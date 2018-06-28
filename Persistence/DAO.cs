using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace org.commitworld.web.persistence
{
    /// <summary>
    /// Args of <see cref="DAO"/> events
    /// </summary>
    public class DAOEventArgs : EventArgs
    {
        /// <summary>
        /// The name-value dictionary of actual parameters for the current method
        /// </summary>
        public IDictionary<string, object> Input { get; set; }
        /// <summary>
        /// The result of current method execution on a <see cref="DAO"/> when completed successfully
        /// </summary>
        public object Result { get; set; }
        /// <summary>
        /// The exception that was risen by current method on a <see cref="DAO"/> when completed with errors
        /// </summary>
        public Exception Error { get; set; }
    }

    /// <summary>
    /// A container for a <see cref="IDBConnectionManager"/> and a <see cref="IQueryLoader"/> to get and execute queries
    /// </summary>
    public abstract class DAO
    {
        private IDBConnectionManager dbManager;
        private IQueryLoader qLoader;

        public event EventHandler<DAOEventArgs> Done, Error;

        /// <summary>
        /// Notifies a method was executed successfully 
        /// </summary>
        /// <param name="e">The event args</param>
        internal void OnDone(DAOEventArgs e)
        {
            if (Done != null) Done(this, e);
        }

        /// <summary>
        /// Notifies a method was executed with errors 
        /// </summary>
        /// <param name="e">The event args</param>
        internal void OnError(DAOEventArgs e)
        {
            if (Error != null) Error(this, e);
        }

        /// <summary>
        /// Gets and sets the <see cref="IDBConnectionManager"/> to execute loaded SQL
        /// </summary>
        public IDBConnectionManager DBConnectionManager
        {
            get { return dbManager; }
            set { dbManager = value; }
        }

        /// <summary>
        /// Gets and sets the <see cref="IQueryLoader"/> to load queries to execute
        /// </summary>
        public IQueryLoader SqlQueryLoader
        {
            get { return qLoader; }
            set { qLoader = value; }
        }
    }

    /// <summary>
    /// Class to help creating a <see cref="DAO"/> instance
    /// </summary>
    public class DaoFactory
    {
        private DaoFactory() { }

        /// <summary>
        /// Creates a <see cref="DAO"/> instance from the specified query loader and executor. 
        /// It finds the candidate concrete class looking for a subclass of DAO also implementing <typeparamref name="TInterface"/>, 
        /// instantiates it and returns the created instance.
        /// </summary>
        /// <typeparam name="TInterface">The base interface the DAO must implement</typeparam>
        /// <param name="dbMngr">The db executor to assign to the DAO instance</param>
        /// <param name="qL">The query loader to assign to the DAO instance</param>
        /// <returns>The DAO instance</returns>
        public static TInterface CreateInstance<TInterface>(IDBConnectionManager dbMngr, IQueryLoader qL)
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

            int typesFound = types.Count();
            if (typesFound == 0)
            {
                throw new ArgumentException(string.Format("Nessuna sottoclasse di DAO sotto {0} implementa {1}", DaoInterfaceType.Namespace, DaoInterfaceType.Name));
            }
            else if (typesFound > 1)
            {
                var selected = types.Select<Type, string>((t) => string.Format("{0}.{1}", t.Namespace, t.Name));
                string msg = string.Empty;
                foreach (string s in selected) msg += (" " + s);
                throw new ArgumentException(string.Format("Trovate più sottoclassi di DAO sotto {0} che implementa {1}:\n[{2}]", DaoInterfaceType.Namespace, DaoInterfaceType.Name, msg));
            }

            Type subclassType = types.ToList().First();
            if (DaoInterfaceType.IsGenericType)
            {
                subclassType = subclassType.MakeGenericType(DaoInterfaceType.GetGenericArguments());
            }

            DAO target = (DAO)Activator.CreateInstance(subclassType);
            target.DBConnectionManager = dbMngr;
            target.SqlQueryLoader = qL;
            
            return InterceptorUtil.GetWrappedInstance<TInterface>(target);
        }

        /// <summary>
        /// Creates a <see cref="DAO"/> instance from the specified query loader and executor. 
        /// The implemented interface and the target subclass of <see cref="DAO"/> also implementing the interface are provided as generic arguments
        /// </summary>
        /// <typeparam name="TInterface">The base interface the DAO must implement</typeparam>
        /// <typeparam name="TClass">The concrete class to instantiate</typeparam>
        /// <param name="dbMngr">The db executor to assign to the DAO instance</param>
        /// <param name="qL">The query loader to assign to the DAO instance</param>
        /// <returns>The DAO instance</returns>
        public static TInterface CreateInstance<TInterface, TClass>(IDBConnectionManager dbMngr, IQueryLoader qL) where TClass : DAO, TInterface
        {
            DAO target = (DAO)Activator.CreateInstance(typeof(TClass), new object[] { });
            target.DBConnectionManager = dbMngr;
            target.SqlQueryLoader = qL;

            return InterceptorUtil.GetWrappedInstance<TInterface>(target);
        }


    }

}
