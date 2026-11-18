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

Add a dated entry when a milestone is reviewed or pushed. Include the GitHub commit for pushed work and record any local changes and database test results at that point.
