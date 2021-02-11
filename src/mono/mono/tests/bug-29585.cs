namespace TestCase
{
    using System;
    using System.Linq;
    using System.Reflection;


    public class MainClass
    {
        public static int Main()
        {
            return new GenericDerived<Param>().FindMethod();
        }
    }


    interface Param
    {
    }


    class GenericDerived<T> :
        Abstract<GenericDerived<T>>
    {
        public int FindMethod()
        {
            return FindGenericMethod<T>();
        }
    }


    abstract class Abstract<TDerived>
        where TDerived : Abstract<TDerived>
    {
        protected virtual int FindGenericMethod<T>()
        {
            var method = typeof(TDerived)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(x => x.Name == "FindGenericMethod" && x.IsGenericMethod);

            Console.WriteLine("TDerived = {0}", typeof(TDerived));
            Console.WriteLine("method = {0}", method);
            Console.WriteLine("method.DeclaringType = {0}", method.DeclaringType);
            Console.WriteLine("method.IsGenericMethod = {0}", method.IsGenericMethod);
            Console.WriteLine("method.IsGenericMethodDefinition = {0}", method.IsGenericMethodDefinition);
			
			if (!method.IsGenericMethod)
				return 1;
			if (!method.IsGenericMethodDefinition)
				return 2;
			return 0;
        }
    }
}