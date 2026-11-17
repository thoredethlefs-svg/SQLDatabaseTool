using Microsoft.Data.SqlClient;
using System.Data;

namespace SqlManipulationClasses
{
    
    public class CRUD
    {
        private readonly SQLPersonalConnection connection;
        public CRUD(SQLPersonalConnection connection)
        { 
        this.connection = connection;
        }

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
                tableNames.Add($"[{schema}].[{name}]");              
                }
                
            return tableNames;
        }
        public DataTable read(string tableName)
        {
            DataTable tabledata = new DataTable ();

            string query = $@"
                            SELECT *
                            FROM {tableName};
                            ";
        }
    }
}
