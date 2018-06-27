using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace org.commitworld.web.persistence
{
    public interface IDBConnectionManager
    {
        DataTable GetDataFromQuery(string query, ICollection<SqlParameter> sqlParams = null, bool transactional = false);
        Int32 ExecuteScalarParam(string query, ICollection<SqlParameter> sqlParams = null, bool transactional = false);
        bool SqlBulk(DataTable table, string destTable);
    }

    public class DBManagerEventArgs : EventArgs
    {
        public string Command { get; set; }
        public object Result { get; set; }
        public Exception Error { get; set; }
    }

    public abstract class DBManager : IDBConnectionManager
    {
        public event EventHandler<DBManagerEventArgs> QueryCompleted, Error;

        protected void OnQueryCompleted(DBManagerEventArgs e)
        {
            if (QueryCompleted != null) QueryCompleted(this, e);
        }

        protected void OnError(DBManagerEventArgs e)
        {
            if (Error != null) Error(this, e);
        }

        private SqlConnection OpenNewConnection()
        {
            SqlConnection sqlConnection = new SqlConnection(ConnectionString);
            sqlConnection.Open();
            return sqlConnection;
        }

        protected abstract string ConnectionString
        {
            get;
        }

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
