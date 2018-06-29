using org.commitworld.web.persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{

    public interface IMyDao
    {
        void foo(int a);
    }

    public class MyDao : DAO, IMyDao
    {
        public void foo(int a)
        {
            Console.WriteLine("body: " + a);
        }
    }

    public static class DaoFactoryProgetto
    {
        public static TInterface Get<TInterface, TClass>(IDBConnectionManager dbMngr, IQueryLoader qL) where TClass : DAO, TInterface
        {
            TInterface proxy;
            DAO target = DaoFactory.CreateInstance<TInterface>(null, null, out proxy);
            target.Done += (sender, eventArgs) =>
            {
                Console.WriteLine("FINE JOJOO ");
                foreach (var pair in eventArgs.Input)
                {
                    Console.WriteLine(pair.Key + " -> " + pair.Value);
                }
            };
            target.Error += (sender, eventArgs) =>
            {
                Console.WriteLine("Error");
            };
            return proxy;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            IMyDao proxy = DaoFactoryProgetto.Get<IMyDao, MyDao>(null, null);
            proxy.foo(3);

            Console.Read();
        }
    }
}
