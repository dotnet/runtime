using System;

namespace TestCase
{
    class Program
    {
        public static void Main()
        {
            new Foo<string>().DoSomething();
        }
    }

    public class Foo<T> : Bar<Foo<T>> 
    {
    }

    public class Bar<T> : Baz
    {
        protected override void DoSomethingElse()
        {
            try
            {
                throw new Exception();
            }
            catch
            {
            }
        }
    }

    public abstract class Baz
    {
        protected abstract void DoSomethingElse();

        public virtual void DoSomething()
        {
            DoSomethingElse();
        }
    }
}
