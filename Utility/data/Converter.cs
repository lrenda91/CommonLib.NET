using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.commitworld.web.utility.data
{
    public class Converter
    {

        private Converter() { }

        public static Nullable<DateTime> getDateTimeNullableValue(object param)
        {
            if (param == null) return null;
            if (param is DBNull) return null;
            if (!(param is DateTime)) throw new ArgumentException();
            return param as DateTime?;
        }


        public static Nullable<DateTime> getDateTimeNullableValue(String param)
        {
            if (param != null && !(string.Empty.Equals(param)))
            {
                try
                {
                    return Convert.ToDateTime(param);

                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static Nullable<Int32> getInt32NullableValue(String param)
        {
            if (param != null && !(string.Empty.Equals(param)))
            {
                try
                {
                    return Convert.ToInt32(param);

                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                return null;

            }
        }

        public static Nullable<Decimal> getDecimalNullableValue(String param)
        {
            if (param != null && !(string.Empty.Equals(param)))
            {
                try
                {
                    param = param.Replace(",", ".");
                    return decimal.Parse(param,
                    System.Globalization.NumberStyles.AllowParentheses |
                    System.Globalization.NumberStyles.AllowLeadingWhite |
                    System.Globalization.NumberStyles.AllowTrailingWhite |
                    System.Globalization.NumberStyles.AllowThousands |
                    System.Globalization.NumberStyles.AllowDecimalPoint |
                    System.Globalization.NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                return null;

            }
        }

        public static Nullable<long> getLongNullableValue(String param)
        {
            if (param != null && !(string.Empty.Equals(param)))
            {
                try
                {
                    param = param.Replace(",", ".");
                    return long.Parse(param,
                    System.Globalization.NumberStyles.AllowParentheses |
                    System.Globalization.NumberStyles.AllowLeadingWhite |
                    System.Globalization.NumberStyles.AllowTrailingWhite |
                    System.Globalization.NumberStyles.AllowThousands |
                    System.Globalization.NumberStyles.AllowDecimalPoint |
                    System.Globalization.NumberStyles.AllowLeadingSign,
                                        CultureInfo.InvariantCulture);

                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                return null;

            }
        }

        public static object setDateTimeNullableValue(object param)
        {
            try
            {
                if (param == null || string.Empty.Equals(param))
                    return DBNull.Value;

                Convert.ToDateTime(param);
            }
            catch
            {
                return DBNull.Value;
            }

            return param;
        }

        public static object setInt32NullableValue(object param)
        {
            try
            {
                if (param == null || string.Empty.Equals(param))
                    return DBNull.Value;
                Convert.ToInt32(param);
            }
            catch
            {
                return DBNull.Value;
            }

            return param;
        }

        public static object setDecimalNullableValue(object param)
        {
            try
            {
                if (param == null || string.Empty.Equals(param))
                    return DBNull.Value;
                Convert.ToDecimal(param);
            }
            catch
            {
                return DBNull.Value;
            }

            return param;
        }
        public static object setTextNullableValue(object param)
        {
            try
            {
                if (param == null || string.Empty.Equals(param))
                    return DBNull.Value;
                else
                    return param.ToString();
            }
            catch
            {
                return DBNull.Value;
            }
        }

    }
}
