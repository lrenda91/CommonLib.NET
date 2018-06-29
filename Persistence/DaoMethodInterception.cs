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

        /// <summary>
        /// Builds the name-value map of parameters of an invocation
        /// </summary>
        /// <param name="invocation">The invocation data</param>
        /// <returns>The parameters dictionary</returns>
        internal static IDictionary<string, object> GetParamsMap(MethodBase method, object[] args)
        {
            var dict = new Dictionary<string, object>();
            ParameterInfo[] myParameters = method.GetParameters();
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    dict.Add(myParameters[i].Name, args[i]);
                }
            }
            return dict;
        }

        /// <summary>
        /// Registers interceptors on each method of a target DAO instance before and after execution, and even if exceptions occurs
        /// </summary>
        /// <typeparam name="TInterface">The interface implemented by the target DAO instance</typeparam>
        /// <param name="target">The target DAO instance</param>
        /// <returns>A new <typeparamref name="TInterface"/> instance, with method interception</returns>
        internal static TInterface GetWrappedInstance<TInterface>(DAO target)
        {
            ProxyFactory factory = new ProxyFactory(target);
            factory.AddAdvice(new DaoInterceptor());
            factory.AddAdvice(new AfterAdvice());
            factory.AddAdvice(new BeforeAdvice());
            return (TInterface)factory.GetProxy();
        }
    }

    /// <summary>
    /// Intercepts DAO method calls by substituting real method
    /// </summary>
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

    /// <summary>
    /// Intercetps DAO methods BEFORE they are executed
    /// </summary>
    public class BeforeAdvice : IMethodBeforeAdvice
    {

        public void Before(MethodInfo method, object[] args, object target)
        {
            
        }
    }

    /// <summary>
    /// Intercetps DAO methods AFTER they are executed
    /// </summary>
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
