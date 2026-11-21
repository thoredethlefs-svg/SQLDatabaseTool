# SQLDatabaseTool development notes

SQLDatabaseTool is a C# WinForms learning project that aims to make SQL Server table creation and CRUD operations easier through a graphical interface. The [README](README.md) describes the project goals, including future work on joins and fuller data output.

## 2026-11-18

### Pushed to GitHub

Source reviewed on `main` at [`1c86a28`](https://github.com/thoredethlefs-svg/SQLDatabaseTool/commit/1c86a28a3b7bf851a58839fdd5c2d0299eda50ea).

[`CRUD.cs`](https://github.com/thoredethlefs-svg/SQLDatabaseTool/blob/1c86a28a3b7bf851a58839fdd5c2d0299eda50ea/SqlManipulationClasses/CRUD.cs) is the data access code reviewed for this entry. Its constructor takes a `SQLPersonalConnection`, and the methods use `CreateConnection()` and `Microsoft.Data.SqlClient` to work with SQL Server. The repository snapshot has no source file for `SQLPersonalConnection`.

| Method | State on `main` |
| --- | --- |
| `readAllTableNames()` | Reads base-table schemas and names from `INFORMATION_SCHEMA.TABLES`. Returns a `List<string>` of escaped, SQL-ready names such as `[dbo].[Customers]`. |
| `read(string tableName)` | Checks the supplied name against that list before executing `SELECT *`, loads the `SqlDataReader` into a `DataTable`, and returns the result. |
| `insertInto(string tableName, Dictionary<string, object?> values)` | Checks the table name and creates value parameters such as `@p0`. Maps C# `null` to `DBNull.Value`. Column keys still go directly into the SQL text. The method opens the connection, then ends without executing the command or returning the declared `int`. |
| `readAllColumnNames()`, table creation, `update()` and `delete()` | No implementations in the reviewed `CRUD.cs`. |

### Local Visual Studio changes discussed in chat

- `readAllTableNames()` was reported to return `List<(string Schema, string TableName)>`, keeping the original schema and table names as separate values.
- `readAllColumnNames()` was reported to accept the schema and table name and validate their tuple against the discovered tables. The planned use is to check INSERT column names against the selected table's metadata.
- The local `read()` was reviewed with separate schema and table arguments. It checks `Contains((Schema, TableName))`, doubles any `]` in both identifiers, quotes each part in brackets, and loads the query results into a `DataTable`.
- Updating `insertInto()` for the new tuple-based table list was the next step in the chat. The local changes above were absent from the reviewed GitHub snapshot. The `read()` review covered source structure, and a SQL Server run is still pending.

### SQL and C# decisions

- Validate table and column identifiers against database metadata before using them in SQL text. After validation, quote each identifier separately and double embedded closing brackets. For example, the table `Customer]Archive` in schema `dbo` becomes `[dbo].[Customer]]Archive]`.
- Use SQL parameters for row values. SQL parameters cannot stand in for schema, table, or column names. Use `DBNull.Value` when a parameter should represent SQL `NULL`.
- Named tuples keep schema and table together for `Contains()` checks. C# strings are immutable, so keep the result of `Replace()` for SQL quoting and use the original names for metadata checks. `using` declarations dispose of connections, commands, and readers when they go out of scope.

### Next development steps

1. Give `insertInto()` separate schema and table arguments and update it to validate the table tuple. Use `readAllColumnNames()` to validate dictionary keys, then quote the accepted identifiers and keep row values parameterized.
2. Execute the INSERT with `ExecuteNonQuery()` and return its affected-row count.
3. Complete the connection setup discussed in chat and run the read and insert paths against SQL Server. Check that the selected schema is respected, invalid identifiers are rejected, and quoted identifiers and nullable values work.

### Local `insertInto()` follow-up

The Visual Studio screenshot shows `insertInto(string Schema, string TableName, Dictionary<string, object?> values)` using a tuple list and checking `Contains((Schema, TableName))`. Parameter generation and `DBNull.Value` handling are still in place. The `CRUD.cs` on `main` still matches the pushed snapshot above.

- The SQL string references lowercase `schema` and `tableName`, which are undefined in the method shown. Validate the original pair first, then create the lowercase variables by doubling `]` in `Schema` and `TableName`. The SQL string already surrounds each interpolated identifier with brackets.
- Validate the dictionary keys with `readAllColumnNames()` and quote each accepted column name before joining the list.
- The method still ends after opening the connection. Once validation and quoting are in place, return `command.ExecuteNonQuery()` and test against SQL Server.

## 2026-11-19

### Schema-aware reads and inserts on `main`

The schema-aware CRUD work previously recorded as local is now in [the pushed `CRUD.cs` source](https://github.com/thoredethlefs-svg/SQLDatabaseTool/blob/73ba10178702aa9020d34d6e167ec4e9793dd694/SqlManipulationClasses/CRUD.cs).

- `readAllTableNames()` returns `(Schema, TableName)` tuples. `read()` validates the pair, quotes both identifiers, and loads the selected table into a `DataTable`.
- `readAllColumnNames()` validates the table pair and queries `INFORMATION_SCHEMA.COLUMNS` with schema and table parameters. It returns column names in ordinal order.
- `insertInto()` rejects empty input, validates the keys against the selected table's columns, and quotes the schema, table, and column identifiers. It binds values as SQL parameters, maps `null` to `DBNull.Value`, and returns the affected-row count from `ExecuteNonQuery()`.

The pushed source was reviewed. Compilation and SQL Server behavior have yet to be verified. Connection setup and the UPDATE and DELETE paths remain open.

Reviewed through [`73ba10178702aa9020d34d6e167ec4e9793dd694`](https://github.com/thoredethlefs-svg/SQLDatabaseTool/commit/73ba10178702aa9020d34d6e167ec4e9793dd694).


## 2026-11-21 — Product vision and modular architecture

### Long-term vision

SQLDatabaseTool began as a C# WinForms learning project, but the developer intends to develop it into a reusable, potentially commercial, production-ready tool. This is a **future ambition**, not a claim that the current software is production-ready. It should make routine SQL Server operations accessible without requiring users to write T-SQL, while keeping the underlying code reusable across database schemas and deployments.

The learning objectives remain C#, OOP, SQL, system architecture, application security and possible REST API work. Researching MySQL and other SQL documentation is a valid way to understand concepts and limitations; implementation for Microsoft SQL Server must use SQL Server-compatible T-SQL and be verified there.

### Two potential commercial offerings (proposals)

| Offering | Intended users | Main focus |
| --- | --- | --- |
| **General SQLDatabaseTool** | Developers, administrators and others explicitly authorized to use direct database operations | A database-aware graphical tool for discovery, reading, editing, table management, and optional procedure tools |
| **Business / operational application** | Shops, retail workers, warehouses and similar organizations | Focused workflows such as receiving stock, managing products and processing orders without giving employees administrative database tooling |

These are *potential* software offerings, not released or finalized editions. Reuse the underlying database-access components in both rather than duplicating them.

The business application is inspired by the general operating model of systems such as Microsoft Dynamics 365 Business Central: employees interact with application functions, not raw SQL or the underlying database. Business Central is **an example, not a planned integration or a claim of replacement**. Employees may have no direct database login or access to SQL Server Management Studio; authorized IT staff or database administrators may still configure the database and the application.

### Architectural decision: preserve an interchangeable CRUD class

**Do not replace the existing direct-SQL CRUD implementation with stored procedures.** The CRUD class must stay useful when a customer's environment does not permit deployment of custom stored procedures. Stored procedures are an **optional, separate capability**, not a dependency for general table discovery or CRUD.

- `CRUD.cs` handles generic INSERT, READ, UPDATE and DELETE operations, identifier validation, parameterized values and returned results; no WinForms display code belongs in the class.
- `SQLPersonalConnection` is the separate connection-management abstraction, passed into CRUD through its constructor (composition, not inheritance). The connection string and credentials should not be exposed as public fields.
- WinForms handles user interaction, ComboBox/table selection, asynchronous loading so the UI stays responsive, rendering results, and dynamically displaying suitable input controls.
- Database metadata can be obtained via database-facing methods and supplied to WinForms; rendering controls is a UI responsibility.
- Validate schema and table identifiers against metadata before dynamic SQL execution; safely bracket-quote each identifier and double embedded closing brackets. Validate column identifiers separately, and use SQL parameters for data values. SQL parameters cannot substitute for table or column names.
- Retain the existing direct `readAllTableNames()` and `read()` approach. Moving their queries into stored procedures is not necessary for background table loading. Whether called directly or through a procedure, the database work still requires a network request and appropriate permissions.

The CRUD library should be reusable across different SQL Server databases, subject to actual table structure and database permissions. General-purpose CRUD does not by itself confer rights to modify arbitrary data or schemas.

### New separate module: StoredProcedureManager (planned)

**New task:** Design a dedicated `StoredProcedureManager.cs` class alongside CRUD, sharing the connection abstraction, rather than mixing stored-procedure logic into CRUD.

Potential features, to be designed and implemented later:

1. Discover available stored procedures and their metadata/parameters.
2. Execute existing procedures using parameters; handle result sets, return codes, output parameters and failures.
3. Create, alter or delete procedures **only** through separately authorized administrative functions.
4. Report lack of permissions or unavailable procedures clearly without disabling unrelated CRUD functionality.

Good candidates for stored procedures include multi-step business operations: goods receipt, order processing with inventory updates, stock movement, validations, or coordinated transactions. Procedures are not automatically more performant or secure than parameterized direct SQL; design, permissions and transactions determine the outcome.

### Stored procedure source and installation documentation (planned)

**New task:** Include the T-SQL code for supported procedures in a distributable package, so a hypothetical buyer with appropriate SQL Server administrator access can review and install them without needing to author complex SQL independently.

The original idea was a documentation text file containing the procedure code. A maintainable packaging option is separate executable `.sql` files together with human-readable `.md` or `.txt` documentation. Neither format nor layout is final.

Proposed future structure (NOT existing files):

```text
SQLDatabaseTool/
├── SqlManipulationClasses/
│   ├── CRUD.cs
│   ├── SQLPersonalConnection.cs          (planned / not verified in repository)
│   └── StoredProcedureManager.cs         (planned)
├── SQLScripts/                           (planned)
│   ├── Procedures/
│   │   └── ExampleProcedure.sql
│   └── InstallProcedures.sql
├── Documentation/                        (planned)
│   ├── StoredProcedures.md
│   └── InstallationGuide.md
├── DEVELOPMENT.md
└── README.md
```

Documentation for each distributed procedure should specify its purpose, target schema and table dependencies, input/output parameters, required rights, expected outcome, error handling, version compatibility, installation/update instructions and rollback considerations. A customer's authorized database administrator should review compatibility with their actual database schema and perform installation. Employees should not be expected or allowed to create database procedures themselves.

The core application should work through its generic CRUD path where permitted **without** forcing customers to install procedures. A procedure-enabled deployment is an optional offering when installation rights and business requirements justify it. Procedures written for a particular schema will not necessarily work against another customer's schema without adaptation.

### Application access, database rights and security

There is a difference between **users not having an SSMS interface** and **users lacking direct database credentials or privileges**. Hiding administration screens alone is not security. SQL Server permissions and server-side application authorization must enforce operations.

For a business setting, evaluate an API/backend deployment such as:

```text
Warehouse / shop WinForms client
    -> authenticated and authorized application service / REST API
    -> reusable CRUD and/or StoredProcedureManager
    -> SQLPersonalConnection
    -> SQL Server
```

This is a **possible future production architecture**, not the current implementation. The direct desktop-to-SQL design can remain appropriate for an administrator/developer-oriented tool when users are authorized to connect directly. The business edition should avoid placing broadly privileged database credentials on employee endpoints. Stored procedures might be executed through narrowly granted permissions where designed and configured appropriately.

Production-readiness areas still to assess and implement include access control, least privilege, secure credential management, encryption in transit, parameterization, safe identifier handling, meaningful transaction boundaries, audit trails, error handling without leaking secrets, confirmation of destructive operations, deployment/version upgrades, regression tests and automated testing.

### Current implementation versus planned work

Current repository source reviewed on `main` on 2026-11-21:

| Component | Observed state |
| --- | --- |
| `readAllTableNames()` | Implemented in source; returns `(Schema, TableName)` tuples |
| `readAllColumnNames(Schema, TableName)` | Implemented in source with metadata query and table validation |
| `read(Schema, TableName)` | Implemented in source; validates the pair, reads into a `DataTable` |
| `insertInto(Schema, TableName, values)` | Implemented in source; checks columns, escapes identifiers, parameterizes values, calls `ExecuteNonQuery()` |
| `update()` / `delete()` | Not present in reviewed CRUD source |
| `SQLPersonalConnection.cs` | Referenced but implementation not verified in the repository |
| WinForms business UI / stored procedure manager / script package | Future work, not verified as implemented |
| Successful compilation, live SQL Server tests, production deployment | Not verified |

The earlier 2026-11-18 notes intentionally describe the source *as it existed then*, including an incomplete INSERT; 2026-11-19 documents that the later pushed source has a completed INSERT implementation. Preserve both historical entries; do not interpret old snapshot limitations as today's status.

### Development workflow and learning boundary

The developer writes the C# code. ChatGPT provides explanations, code review, architectural guidance and help identifying/fixing bugs, rather than producing whole classes unasked.

Maintain the agreed order: finish CRUD (including UPDATE and DELETE), create the connection class, build WinForms and its event handlers, integrate existing classes, test, fix bugs, retest and iterate through new functionality. Add `StoredProcedureManager`, SQL scripts and business/API deployment work as future modular tasks; do not replace the existing CRUD implementation.

**Scope of this entry:** documentation and design decisions only. No source code, SQL procedures, database tables or application functionality have been changed by recording it.

---

Add a dated entry when a milestone is reviewed or pushed. Include the GitHub commit for pushed work and record any local changes and database test results at that point.
