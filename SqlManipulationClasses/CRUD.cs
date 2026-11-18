using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics.Metrics;
using System.Reflection.PortableExecutable;

namespace SqlManipulationClasses
    {

    public class CRUD
        {
        private readonly SQLPersonalConnection connection;
        public CRUD(SQLPersonalConnection connection)
            {
            this.connection = connection;
            }


        //Read all table names from the database
        public List<(string Schema, string TableName)> readAllTableNames()
            {
            /*declaring a new variable that will hold the a refference to sqlConnectionReturnedValue object of type SqlConnection 
			 *assign it the value returned by the CreateConnection method of the connection object
			 */
            using SqlConnection sqlConnectionReturnedValue = connection.CreateConnection();
            string query = @"
						   SELECT TABLE_SCHEMA, TABLE_NAME
						   FROM INFORMATION_SCHEMA.TABLES
						   WHERE TABLE_TYPE = 'BASE TABLE';"
                           ;

            //create a new SqlCommand object by passing the query and the
            //sqlConnectionReturnedValue as parameters to the constructor of the SqlCommand class
            using SqlCommand command = new SqlCommand(query, sqlConnectionReturnedValue);
            sqlConnectionReturnedValue.Open();

            //using SqlDataReader, the MS type, to create an object of it called reader that contains a refference to what command returns
            using SqlDataReader reader = command.ExecuteReader();

           //create a new list object containing strings by the name tableNames inside readAllTableNames
            List<(string Schema, string TableName)> tables = new List<(string Schema, string TableName)>();
            while (reader.Read())
                {
                string Schema = reader.GetString(0);
                string TableName = reader.GetString(1);

                tables.Add((Schema, TableName));
                }

            return tables;
            }

        //Read all column names from a specific table in the database
        public List<string> readAllColumnNames(string Schema, string TableName)
        {
            List<(string Schema, string TableName)> validTables = readAllTableNames();
            List<string> columnNames = new List<string>();

            if (!validTables.Contains((Schema, TableName)))
                {
                    throw new ArgumentException("Table name not valid.");
                }

            using SqlConnection sqlConnectionReturnedValue = connection.CreateConnection();

            string query = @"
						   SELECT COLUMN_NAME
                           FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_SCHEMA = @schema
                           AND TABLE_NAME = @tableName
                           ORDER BY ORDINAL_POSITION;"
                           ;

            using SqlCommand command = new SqlCommand(query, sqlConnectionReturnedValue);

            command.Parameters.AddWithValue("@schema", Schema);
            command.Parameters.AddWithValue("@tableName", TableName);

            sqlConnectionReturnedValue.Open();

            using SqlDataReader reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                string ColumnName = reader.GetString(0);
                columnNames.Add(ColumnName);
            }

            return columnNames;

        }


        //Select SQL query
        public DataTable read(string Schema, string TableName)
        {
            List<(string Schema, string TableName)> validTables = readAllTableNames();

            using SqlConnection sqlConnectionReturnedValue = connection.CreateConnection();
            DataTable tableData = new DataTable();
                        
            if (!validTables.Contains((Schema, TableName)))
            {
                throw new ArgumentException("Table name not valid.");
            }

            string schema = Schema.Replace("]", "]]");
            string tableName = TableName.Replace("]", "]]");

             string query = $@"
							SELECT *
							FROM [{schema}].[{tableName}];
							";

            using SqlCommand command = new SqlCommand(query, sqlConnectionReturnedValue);
            sqlConnectionReturnedValue.Open();

            using SqlDataReader reader = command.ExecuteReader();
            tableData.Load(reader);

            return tableData;
            }


        //Insert SQL query
        public int insertInto(string Schema, string TableName, Dictionary<string, object?> values)
            {

            List<string> columnNames = new List<string>(values.Keys);
            List<object?> columnValues = new List<object?>(values.Values);
            List<string> parameterNames = new List<string>();
            List<(string Schema, string TableName)> validTables = readAllTableNames();

            using SqlConnection sqlConnectionReturnedValue = connection.CreateConnection();



            string parameter = "@p0";
            foreach (var entry in values)
                {
                    parameterNames.Add(parameter);
                    parameter = $"@p{parameterNames.Count}";
                }

            
            string columnList = string.Join(", ", columnNames);
            string valueList = string.Join(", ", parameterNames);

            if (!validTables.Contains((Schema, TableName)))
            {
                throw new ArgumentException("Table name not valid.");
            }

            string schema = Schema.Replace("]", "]]");
            string tableName = TableName.Replace("]", "]]");
            string columnListEscaped = string.Join(", ", columnNames.Select(c => $"[{c.Replace("]", "]]")}]"));
            string valueListEscaped = string.Join(", ", parameterNames.Select(p => $"{p}"));

            string query = $@"
							INSERT INTO [{schema}].[{tableName}]
							({columnListEscaped})
							VALUES
							({valueListEscaped})
                            ;"
                            ;




            using SqlCommand command = new SqlCommand(query, sqlConnectionReturnedValue);

            for (int i = 0; i < parameterNames.Count; i++)
            {
                command.Parameters.AddWithValue(parameterNames[i], columnValues[i] ?? DBNull.Value);
            }

            sqlConnectionReturnedValue.Open();


        }
    }
    }
