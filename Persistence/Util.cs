using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace org.commitworld.web.persistence
{

    public class SqlParameterBuilder
    {
        private List<SqlParameter> paramsCollection = new List<SqlParameter>();

        public SqlParameterBuilder AddInputParameter(string name, SqlDbType type, object value)
        {
            return AddParameter(name, type, value, ParameterDirection.Input);
        }

        public SqlParameterBuilder AddParameter(string name, SqlDbType type, object value, ParameterDirection direction)
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

    }


    internal class DBUtil
    {
        private DBUtil() { }

        public static object GetNullableValue<T>(T? val) where T : struct
        {
            if (val == null || !val.HasValue) return DBNull.Value as object;
            return val.Value;
        }

        public static object GetValue(object val)
        {
            return (val != null) ? val : DBNull.Value;
        }
        public static object GetDictValue<T>(IDictionary<T, dynamic> dic, T key)
        {
            return dic.ContainsKey(key) ? GetValue(dic[key]) : DBNull.Value;
        }
    }

}
