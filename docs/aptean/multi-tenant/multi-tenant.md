# Adjust nopCommerce for Multi-Tenant

In this document we described what should be changed in nopCommerce source code to support multi-tenant, so later when we merge latest changes from nopCommerce, we can understand which changes are needed for nopCommerce new version.

To support multi-tenant with one nopCommerce service instance and nopCommerce database, we will segregate company data by schema, so every company will have a standalone schema and a standalone user account in SQL Server database (for now Postgres is not supported).

## Shared

1. Current company accessor: `ICurrentCompanyAccessor` and `CurrentCompanyAccessor`

   Added an interface for get current company and an implementation `CurrentCompanyAccessor` that will find out current company code, and it's corresponding database schema based on current HTTP request. For now, this is based on port number, this should be changed based on HTTP headers, that is, the frontend will send a HTTP header to identify current company, for example nop-commerce-company-id=abc.

2. `CacheKeyService`.cs When get key for a store, company name/url will also be a part of the key, because the store.Id can be same for different company, a best way is to use company.id.
3. `MemoryCacheManager`.cs is changed to injected the current company accessor, and when query for a key for some record, current company code will also be a part of the key, so same record from different company will use different key.
4. `SynchronizedMemoryCacheManager` is changed to injected the current company accessor.

## Data layer

### Database changes

Schema dbo will not be used (or used as place where we share data between multiple companies, store hosted company info for example, this will need to be discussed later).

Each company will have a unique short name or id, schema and a user will be created for each company, password will be generated based on company short name (this is to be defined later), for example:

    | company name      | id  | schema  | user         | password               |
    | ----------------- | --- | ------- | ------------ | ---------------------- |
    | ABC Company       | abc | nop-abc | commerce-abc | prefix-{sha256 of abc} |
    | Walnut Limited    | wal | nop-wal | commerce-wal | prefix-{sha256 of wal} |
    | TESCO Supermarket | tsc | nop-tsc | commerce-tsc | prefix-{sha256 of tsc} |

### Access to correct schema

   1. `MultiTenantConnectionStringAccessor` is implemented to return connection string based on current company.
   2. DataProviderManager.cs is changed to inject `IConnectionStringAccessor` so it can pass this when create database provider instance.
   3. `BaseDataProvider`.cs and `IDataProviderManager`.cs is changed to inject `IConnectionStringAccessor` so it can build connection string based on current company.
   4. Database providers (`BaseDataProvider`.cs, `MsSqlDataProvider`.cs, `MySqlDataProvider`.cs, `PostgreSqlDataProvider`.cs) are changed to inject connection string accessor so it will use correct user account for current company.

### Database migration

1. Migration for multiple schema

    `MultiTenantMigrationVersionInfo`.cs is created based on MigrationVersionInfo so when access for SchemaName property, it can return schema for current company instead of empty string.

    `MultiTenantConventionSet`.cs is created so when config for index, foreign keys, correct schema can be used.

    `MultiTenantMigrationManager`.cs is created based on MigrationManager so when get migration info, correct company name/schema name can be set for these migrations, and it can run on specific schema instead of default schema.

2. Problem with MigrationBase.Schema

    By default when you access base.Schema in migration script, it mean the default schema, for SQL server, this is dbo, for Postgres this is public, and there is no way to override it. To solve this issue, `NopMigrationBase`.cs is created to override the base.Schema so we will able to access schema for current company.

    Also, database migration scripts and Plugin Migration scripts is changed to inherit from `NopMigrationBase` so the base.`Schema` can be interpret with schema for current company instead of database default schema, usually `dbo`.

    Changed migration scripts:

    | Migration script file name                                                   |
    | ---------------------------------------------------------------------------- |
    | Nop.Data/Migrations/UpgradeTo440/DataMigration.cs                            |
    | Nop.Data/Migrations/UpgradeTo440/SpecificationAttributeGroupingMigration.cs  |
    | Nop.Data/Migrations/UpgradeTo450/DataMigration.cs                            |
    | Nop.Data/Migrations/UpgradeTo460/MySqlDateTimeWithPrecisionMigration.cs      |
    |   `MigrationVersionInfo` is changed to `MultiTenantMigrationVersionInfo`.    |
    | Nop.Data/Migrations/UpgradeTo460/SchemaMigration.cs                          |
    | Nop.Data/Migrations/UpgradeTo460/StoreMigration.cs                           |
    | Nop.Data/Migrations/UpgradeTo460/VideoMigration.cs                           |
    | Nop.Data/Migrations/UpgradeTo470/AddIndexesMigration.cs                      |
    | Nop.Data/Migrations/UpgradeTo470/DataMigration.cs                            |
    | Nop.Data/Migrations/UpgradeTo470/SchemaMigration.cs                          |
    | Nop.Plugin.Misc.Zettle/Data/InventoryBalanceMigration.cs                     |
    | Nop.Plugin.Shipping.FixedByWeightByTotal/Migrations/UpgradeTo450.cs          |
    | Nop.Plugin.Tax.Avalara/Data/ItemClassificationMigration.cs                   |
    | Nop.Plugin.Tax.Avalara/Data/ScheduleTaskMigration.cs                         |
    | Nop.Plugin.Widgets.FacebookPixel/Data/ConversionsApiMigration.cs             |

3. Startup routing for Data layer

    `NopDbStartup`.cs will be replaced by `MultiTenantNopDbStartup`.cs so we can inject correct implementations, `MultiTenantConnectionStringAccessor`, `MultiTenantMigrationManager`, `CurrentCompanyAccessor`, `MultiTenantConventionSet`, `MultiTenantMigrationVersionInfo`.

4. Tools to run migration script for each company.

This is not implemented yet, we should create some tool, so when we add a new customer (company), we will create a schema, a database user, and also run all migration scripts for this company.

Also, the tool will need a feature to run migration scripts for existing schemas (if there is new changes).

## Schedule Tasks

This is how current task schedule is working:

`ITaskSchedule` (`TaskScheduler`) will be initialized and start when application start. All tasks will be load from `IScheduleTaskService` (`ScheduleTaskService`, ScheduleTask table), since it run only for one company. Store record will be load from database Store table, and a TaskTread will be created for each task, since they have different schedule time.

Each TaskThread will have a timer (System.Threading.Timer), and when the time due, it will execute a callback (TimerHandler), in this callback, it will call a predefined web API. This will be a problem if we have many companies and each company have multiple tasks.

We can create a standalone service, that will be wake up on scheduled time, and then call these web APIs.
