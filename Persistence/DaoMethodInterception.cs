using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AopAlliance.Intercept;
using Spring.Aop;
using Spring.Aop.Framework;

namespace org.commitworld.web.persistence
{

    internal class InterceptorUtil
    {
        private InterceptorUtil() { }
        internal static IDictionary<string, object> GetParamsMap(MethodBase method, object[] args)
        {
            ParameterInfo[] myParameters = method.GetParameters();
            var parameters = new Dictionary<string, object>();
            for (int i = 0; i < args.Length; i++)
            {
                parameters.Add(myParameters[i].Name, args[i]);
            }
            return parameters;
        }

        internal static TInterface GetWrappedInstance<TInterface>(DAO target)
        {
            ProxyFactory factory = new ProxyFactory(target);
            factory.AddAdvice(new DaoInterceptor());
            factory.AddAdvice(new AfterAdvice());
            factory.AddAdvice(new BeforeAdvice());
            return (TInterface)factory.GetProxy();
        }
    }
    public class DaoInterceptor : IMethodInterceptor
    {

        public object Invoke(IMethodInvocation invocation)
        {
            object result = null;
            DAO dao = (DAO)invocation.Target;
            try
            {
                result = invocation.Proceed();
            }
            catch (Exception ex)
            {
                dao.OnError(new DAOEventArgs()
                {
                    Input = InterceptorUtil.GetParamsMap(invocation.Method, invocation.Arguments),
                    Error = ex
                });
                throw ex;
            }
            return result;
        }
    }

    public class BeforeAdvice : IMethodBeforeAdvice
    {

        public void Before(MethodInfo method, object[] args, object target)
        {
            Console.WriteLine("prima");
        }
    }
    public class AfterAdvice : IAfterReturningAdvice
    {
        public void AfterReturning(object returnValue, MethodInfo method, object[] args, object target)
        {
            DAO dao = (DAO)target;
            dao.OnDone(new DAOEventArgs()
            {
                Input = InterceptorUtil.GetParamsMap(method, args),
                Result = returnValue
            });
        }
    }

}
