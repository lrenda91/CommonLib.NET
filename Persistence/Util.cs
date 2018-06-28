using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace org.commitworld.web.persistence
{
    /// <summary>
    /// Dynamically builds a list of SqlParameter
    /// </summary>
    public class SqlParamsBuilder
    {
        private List<SqlParameter> paramsCollection = new List<SqlParameter>();

        /// <summary>
        /// Adds an input SqlParameter with provided data
        /// </summary>
        /// <param name="name">The parameter name</param>
        /// <param name="type">The parameter type on the DB</param>
        /// <param name="value">The parameter value</param>
        /// <returns>The params builder itself, so that you can chain calls</returns>
        public SqlParamsBuilder AddInputParameter(string name, SqlDbType type, object value)
        {
            return AddParameter(name, type, value, ParameterDirection.Input);
        }

        /// <summary>
        /// Adds a generic SqlParameter with provided data
        /// </summary>
        /// <param name="name">The parameter name</param>
        /// <param name="type">The parameter type on the DB</param>
        /// <param name="value">The parameter value</param>
        /// <param name="direction">The parameter input/output direction</param>
        /// <returns>The params builder itself, so that you can chain calls</returns>
        public SqlParamsBuilder AddParameter(string name, SqlDbType type, object value, ParameterDirection direction)
        {
            SqlParameter parameter = new SqlParameter()
            {
                ParameterName = string.Concat("@", name),
                SqlDbType = type,
                Direction = direction,
                Value = DBUtil.GetValue(value)
            };
            paramsCollection.Add(parameter);
            return this;
        }

        /// <summary>
        /// Builds the params list
        /// </summary>
        /// <returns>A list of build SqlParameter objects</returns>
        public List<SqlParameter> Build()
        {
            return paramsCollection;
        }

    }

    /// <summary>
    /// Utility class to get DBNull value whenever a non valued object must be stored onto the DB
    /// </summary>
    internal class DBUtil
    {
        private DBUtil() { }

        /// <summary>
        /// Gets DBNull on null or non valued Nullable objects
        /// </summary>
        /// <typeparam name="T">The generic struct wrapped by the Nullable object</typeparam>
        /// <param name="val">The Nullable object</param>
        /// <returns>The Nullable value, if existent, DBNull otherwise</returns>
        public static object GetNullableValue<T>(T? val) where T : struct
        {
            if (val == null || !val.HasValue) return DBNull.Value as object;
            return val.Value;
        }

        /// <summary>
        /// Gets DBNull on null objects
        /// </summary>
        /// <param name="val">The object to store onto the DB</param>
        /// <returns>The object itself, if not null, DBNull otherwise</returns>
        public static object GetValue(object val)
        {
            return (val != null) ? val : DBNull.Value;
        }

        /// <summary>
        /// Gets DBNull when, given a dictionary and a key, the corresponding value is not found.
        /// The method is parametric on the key type
        /// </summary>
        /// <typeparam name="T">The dictionary key type</typeparam>
        /// <param name="dic">The dictionary</param>
        /// <param name="key">The key that must be tested in the dictionary</param>
        /// <returns>The value that is associated with the key, if existent, DBNull otherwise</returns>
        public static object GetDictValue<T>(IDictionary<T, dynamic> dic, T key)
        {
            return dic.ContainsKey(key) ? GetValue(dic[key]) : DBNull.Value;
        }
    }

}
