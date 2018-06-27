using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.commitworld.web.utility.logging
{

    public interface LoggerFactory
    {
        BaseLogger BuildLogger();
    }

    public abstract class BaseLogger
    {
        protected enum LogLevel
        {
            DEBUG = 0,
            INFORMATION = 1,
            WARNING = 2,
            ERROR = 3
        }

        protected abstract bool IsEnabled(LogLevel type);
        protected abstract void Write(LogLevel type, string msg);
        protected abstract void Write(LogLevel type, string msg, Dictionary<string, object> args);
        protected void Write(LogLevel type, string format, params object[] args)
        {
            Write(type, string.Format(format, args));
        }

        public void WriteI(string msg)
        {
            if (IsEnabled(LogLevel.INFORMATION)) Write(LogLevel.INFORMATION, msg);
        }
        public void WriteI(string msg, params object[] args)
        {
            if (IsEnabled(LogLevel.INFORMATION)) Write(LogLevel.INFORMATION, msg, args);
        }
        public void WriteI(string msg, Dictionary<string, object> args)
        {
            if (IsEnabled(LogLevel.INFORMATION)) Write(LogLevel.INFORMATION, msg, args);
        }
        public void WriteD(string msg)
        {
            if (IsEnabled(LogLevel.DEBUG)) Write(LogLevel.DEBUG, msg);
        }
        public void WriteD(string msg, params object[] args)
        {
            if (IsEnabled(LogLevel.DEBUG)) Write(LogLevel.DEBUG, msg, args);
        }
        public void WriteD(string msg, Dictionary<string, object> args)
        {
            if (IsEnabled(LogLevel.DEBUG)) Write(LogLevel.DEBUG, msg, args);
        }
        public void WriteW(string msg)
        {
            if (IsEnabled(LogLevel.WARNING)) Write(LogLevel.WARNING, msg);
        }
        public void WriteW(string msg, params object[] args)
        {
            if (IsEnabled(LogLevel.WARNING)) Write(LogLevel.WARNING, msg, args);
        }
        public void WriteW(string msg, Dictionary<string, object> args)
        {
            if (IsEnabled(LogLevel.WARNING)) Write(LogLevel.WARNING, msg, args);
        }
        public void WriteE(string msg)
        {
            if (IsEnabled(LogLevel.ERROR)) Write(LogLevel.ERROR, msg);
        }
        public void WriteE(string msg, params object[] args)
        {
            if (IsEnabled(LogLevel.ERROR)) Write(LogLevel.ERROR, msg, args);
        }
        public void WriteE(string msg, Dictionary<string, object> args)
        {
            if (IsEnabled(LogLevel.ERROR)) Write(LogLevel.ERROR, msg, args);
        }
        public void WriteEx(Exception ex)
        {
            WriteE(string.Format("Exception: {0} ({1}.{2})", ex.Message, ex.TargetSite.ReflectedType.Name, ex.TargetSite.Name));
            WriteE(ex.StackTrace);
        }
    }

    public class ConsoleLogger : BaseLogger
    {
        protected override bool IsEnabled(LogLevel type)
        {
            return true;
        }
        protected override void Write(LogLevel type, string msg)
        {
            string formatType = string.Format("[{0}]", type).PadRight(13, ' ');
            Console.WriteLine("{0} - {1}", formatType, msg);
        }
        protected override void Write(LogLevel type, string msg, Dictionary<string, object> args)
        {
            Console.WriteLine("{0} - {1} - {2}", type.ToString(), msg, args.ToString());
        }
    }


    public abstract class FileLogger : BaseLogger
    {
        protected abstract string GetFilePath();
    }
}
