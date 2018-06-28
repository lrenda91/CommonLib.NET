using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace org.commitworld.web.persistence
{
    /// <summary>
    /// Models an SQL executor
    /// </summary>
    public interface IDBConnectionManager
    {
        /// <summary>
        /// Executes a prepared statement and gets a DataTable as the result
        /// </summary>
        /// <param name="query">The prepared statement</param>
        /// <param name="sqlParams">The list of parameter. <see cref="SqlParamsBuilder"/> can be useful to build this list</param>
        /// <param name="transactional">Indicates whether or not to open a new transaction</param>
        /// <returns>The result data as a DataTable</returns>
        DataTable GetDataFromQuery(string query, ICollection<SqlParameter> sqlParams = null, bool transactional = false);

        /// <summary>
        /// Executes a prepared statement with scalar result and gets it as an integer
        /// </summary>
        /// <param name="query">The prepared statement</param>
        /// <param name="sqlParams">The list of parameter. <see cref="SqlParamsBuilder"/> can be useful to build this list</param>
        /// <param name="transactional"></param>
        /// <returns>The integer result of the scalar query</returns>
        Int32 ExecuteScalarParam(string query, ICollection<SqlParameter> sqlParams = null, bool transactional = false);
        /// <summary>
        /// Performs bulk of a set of data into a destination table
        /// </summary>
        /// <param name="table">The set of source data</param>
        /// <param name="destTable">The destination table</param>
        /// <returns>True if the bulk operation was successfully executed, False otherwise</returns>
        bool SqlBulk(DataTable table, string destTable);
    }

    /// <summary>
    /// Args of <see cref="DBManager"/> events
    /// </summary>
    public class DBManagerEventArgs : EventArgs
    {
        /// <summary>
        /// The SQL prepared statement that was just executed
        /// </summary>
        public string Command { get; set; }
        /// <summary>
        /// The result of the query execution
        /// </summary>
        public object Result { get; set; }
        /// <summary>
        /// The eventual exception that was risen due to any kind of problems
        /// </summary>
        public Exception Error { get; set; }
    }

    /// <summary>
    /// Abstract implementation of <see cref="IDBConnectionManager"/>  
    /// It also manages DB access and events notification, delegating them to subclasses to reach customization. 
    /// So, you should extend this class to get a concrete DB manager
    /// </summary>
    public abstract class DBManager : IDBConnectionManager
    {
        public event EventHandler<DBManagerEventArgs> QueryCompleted, Error;

        /// <summary>
        /// Notifies a query execution has completed successfully
        /// </summary>
        /// <param name="e">The event args</param>
        protected void OnQueryCompleted(DBManagerEventArgs e)
        {
            if (QueryCompleted != null) QueryCompleted(this, e);
        }

        /// <summary>
        /// Notifies a query execution has completed with errors
        /// </summary>
        /// <param name="e">The event args</param>
        protected void OnError(DBManagerEventArgs e)
        {
            if (Error != null) Error(this, e);
        }

        /// <summary>
        /// Opens a new SQL connection
        /// </summary>
        /// <returns>The new SQl connection</returns>
        private SqlConnection OpenNewConnection()
        {
            SqlConnection sqlConnection = new SqlConnection(ConnectionString);
            sqlConnection.Open();
            return sqlConnection;
        }

        /// <summary>
        /// Gets the connection string
        /// </summary>
        protected abstract string ConnectionString
        {
            get;
        }

        /// <summary>
        /// Gets the connection timeout
        /// </summary>
        protected abstract int ConnectionTimeout
        {
            get;
        }

        public DataTable GetDataFromQuery(string query, ICollection<SqlParameter> sqlParams = null, bool transactional = false)
        {
            DataTable dataTable = null;
            using (SqlConnection sqlConnection = OpenNewConnection())
            {
                SqlCommand sqlCommand = sqlConnection.CreateCommand();
                sqlCommand.CommandTimeout = ConnectionTimeout;
                sqlCommand.CommandType = CommandType.Text;
                sqlCommand.CommandText = query;
                if (sqlParams != null)
                {
                    foreach (SqlParameter param in sqlParams)
                    {
                        sqlCommand.Parameters.Add(param);
                    }
                }
                try
                {
                    if (transactional)
                    {
                        sqlCommand.Transaction = sqlConnection.BeginTransaction("Transaction");
                    }
                    dataTable = new DataTable();
                    new SqlDataAdapter(sqlCommand).Fill(dataTable);
                    if (transactional)
                    {
                        sqlCommand.Transaction.Commit();
                    }
                    OnQueryCompleted(new DBManagerEventArgs() { Command = query, Result = dataTable });
                }
                catch (Exception ex)
                {
                    if (transactional)
                    {
                        sqlCommand.Transaction.Rollback();
                    }
                    OnError(new DBManagerEventArgs() { Command = query, Error = ex });
                    throw ex;
                }
                finally
                {
                    sqlCommand.Parameters.Clear();
                    sqlConnection.Close();
                }
            }
            return dataTable;
        }

        public int ExecuteScalarParam(string query, ICollection<SqlParameter> sqlParams = null, bool transactional = false)
        {
            Int32 val = -1;
            using (SqlConnection sqlConnection = OpenNewConnection())
            {
                SqlCommand sqlCommand = sqlConnection.CreateCommand();
                sqlCommand.CommandTimeout = ConnectionTimeout;
                sqlCommand.CommandType = CommandType.Text;
                sqlCommand.CommandText = query;
                if (sqlParams != null)
                {
                    foreach (SqlParameter param in sqlParams)
                    {
                        sqlCommand.Parameters.Add(param);
                    }
                }
                try
                {
                    if (transactional)
                    {
                        sqlCommand.Transaction = sqlConnection.BeginTransaction("Transaction");
                    }
                    object result = sqlCommand.ExecuteScalar();
                    if (transactional)
                    {
                        sqlCommand.Transaction.Commit();
                    }
                    if (result == DBNull.Value)
                    {
                        return -1;
                    }
                    val = Convert.ToInt32(result);
                    OnQueryCompleted(new DBManagerEventArgs() { Command = query, Result = val });
                }
                catch (Exception ex)
                {
                    if (transactional)
                    {
                        sqlCommand.Transaction.Rollback();
                    }
                    OnError(new DBManagerEventArgs() { Command = query, Error = ex });
                    throw ex;
                }
                finally
                {
                    sqlCommand.Parameters.Clear();
                    sqlConnection.Close();
                }
            }
            return val;
        }

        public bool SqlBulk(DataTable table, string destTable)
        {
            using (SqlConnection sqlConnection = OpenNewConnection())
            {
                SqlTransaction transaction = null;
                try
                {
                    transaction = sqlConnection.BeginTransaction("SqlBulkTransaction");
                    SqlBulkCopy bulkCopy = new SqlBulkCopy(
                           sqlConnection,
                           SqlBulkCopyOptions.KeepNulls,
                           transaction);
                    bulkCopy.BatchSize = 10;
                    bulkCopy.DestinationTableName = destTable;
                    bulkCopy.WriteToServer(table);
                    transaction.Commit();
                    OnQueryCompleted(new DBManagerEventArgs());
                }
                catch (Exception ex)
                {
                    if (transaction != null)
                    {
                        transaction.Rollback();
                    }
                    OnError(new DBManagerEventArgs() { Error = ex });
                    throw ex;
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
            return true;
        }

    }
}
