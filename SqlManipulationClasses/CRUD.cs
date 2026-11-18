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
        public List<string> readAllTableNames()
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
            List<string> tableNames = new List<string>();
            while (reader.Read())
                {
                string schema = reader.GetString(0);
                string name = reader.GetString(1);

                schema = schema.Replace("]", "]]");
                name = name.Replace("]", "]]");

                tableNames.Add($"[{schema}].[{name}]");
                }

            return tableNames;
            }

        //Select SQL query
        public DataTable read(string tableName)
            {
            List<string> validTables = readAllTableNames();


            using SqlConnection sqlConnectionReturnedValue = connection.CreateConnection();
            DataTable tableData = new DataTable();

            string query = $@"
							SELECT *
							FROM {tableName};
							";

            if (!validTables.Contains(tableName))
                {
                throw new ArgumentException("Table name not valid.");
                }

            using SqlCommand command = new SqlCommand(query, sqlConnectionReturnedValue);
            sqlConnectionReturnedValue.Open();

            using SqlDataReader reader = command.ExecuteReader();
            tableData.Load(reader);

            return tableData;
            }


        //Insert SQL query
        public int insertInto(string tableName, Dictionary<string, object?> values)
            {

            List<string> columnNames = new List<string>(values.Keys);
            List<object?> columnValues = new List<object?>(values.Values);
            List<string> parameterNames = new List<string>();
            List<string> validTables = readAllTableNames();

            using SqlConnection sqlConnectionReturnedValue = connection.CreateConnection();



            string parameter = "@p0";
            foreach (var entry in values)
                {
                    parameterNames.Add(parameter);
                    parameter = $"@p{parameterNames.Count}";
                }

            
            string columnList = string.Join(", ", columnNames);
            string valueList = string.Join(", ", parameterNames);

            string query = $@"
							INSERT INTO {tableName}
							({columnList})
							VALUES
							({valueList})
                            ;"
                            ;

            if (!validTables.Contains(tableName))
            {
                throw new ArgumentException("Table name not valid.");
            }


            using SqlCommand command = new SqlCommand(query, sqlConnectionReturnedValue);

            for (int i = 0; i < parameterNames.Count; i++)
            {
                command.Parameters.AddWithValue(parameterNames[i], columnValues[i] ?? DBNull.Value);
            }

            sqlConnectionReturnedValue.Open();


        }
    }
    }
