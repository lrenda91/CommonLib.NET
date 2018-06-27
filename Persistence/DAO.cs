using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace org.commitworld.web.persistence
{
    public class DAOEventArgs : EventArgs
    {
        public IDictionary<string, object> Input { get; set; }
        public object Result { get; set; }
        public Exception Error { get; set; }
    }

    public abstract class DAO
    {
        private IDBConnectionManager dbManager;
        private QueryLoader qLoader;

        public event EventHandler<DAOEventArgs> Done, Error;

        internal void OnDone(DAOEventArgs e)
        {
            if (Done != null) Done(this, e);
        }

        internal void OnError(DAOEventArgs e)
        {
            if (Error != null) Error(this, e);
        }

        public IDBConnectionManager DBConnectionManager
        {
            get { return dbManager; }
            set { dbManager = value; }
        }

        public QueryLoader SqlQueryLoader
        {
            get { return qLoader; }
            set { qLoader = value; }
        }
    }

    public class DaoFactory
    {
        private DaoFactory() { }

        public static TInterface CreateInstance<TInterface>(IDBConnectionManager dbMngr, QueryLoader qL)
        {
            var DaoInterfaceType = typeof(TInterface);
            if (!DaoInterfaceType.IsInterface)
            {
                throw new ArgumentException(string.Format("{0} deve essese una interface!", DaoInterfaceType.Name));
            }
            var types = Assembly.GetCallingAssembly().GetTypes().AsEnumerable();
            types = types.Where((t) =>
                t.IsClass &&
                !t.IsAbstract &&
                t.IsSubclassOf(typeof(DAO)) &&
                string.Equals(t.Namespace, DaoInterfaceType.Namespace)
            );
            if (types.Count() == 0)
            {
                throw new ArgumentException("Nessuna sottoclasse concreta di DAO sotto " + DaoInterfaceType.Namespace);
            }
            var tuple = new Tuple<string, string>(DaoInterfaceType.Namespace, DaoInterfaceType.Name);
            types = types.Where((t) =>
            {
                var s = (t.GetInterfaces().Select<Type, Tuple<string, string>>((implemented) =>
                    new Tuple<string, string>(implemented.Namespace, implemented.Name))
                );
                return s.Contains(tuple);
            });
            if (types.Count() == 0)
            {
                throw new ArgumentException(string.Format("Nessuna sottoclasse di DAO sotto {0} implementa {1}", DaoInterfaceType.Namespace, DaoInterfaceType.Name));
            }

            var list = types.ToList();
            if (list.Count > 1)
            {
                throw new ArgumentException("Ambiguità");
            }

            Type subclassType = list.First();
            if (DaoInterfaceType.IsGenericType)
            {
                subclassType = subclassType.MakeGenericType(DaoInterfaceType.GetGenericArguments());
            }

            DAO target = (DAO)Activator.CreateInstance(subclassType);
            target.DBConnectionManager = dbMngr;
            target.SqlQueryLoader = qL;
            
            return InterceptorUtil.GetWrappedInstance<TInterface>(target);
        }

        public static TInterface CreateInstance<TInterface, TClass>(IDBConnectionManager dbMngr, QueryLoader qL) where TClass : DAO, TInterface
        {
            DAO target = (DAO)Activator.CreateInstance(typeof(TClass), new object[] { });
            target.DBConnectionManager = dbMngr;
            target.SqlQueryLoader = qL;

            return InterceptorUtil.GetWrappedInstance<TInterface>(target);
        }


    }

}
