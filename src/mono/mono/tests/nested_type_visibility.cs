namespace ConsoleApplication1
{
    public abstract class SuperClass
    {
        protected abstract class SuperInnerAbstractClass
        {
            protected class SuperInnerInnerClass
            {
            }
        }
    }

    public class ChildClass : SuperClass
    {
        private class ChildInnerClass : SuperInnerAbstractClass
        {
            private readonly SuperInnerInnerClass s_class = new SuperInnerInnerClass();
        }

        public ChildClass()
        {
            var childInnerClass = new ChildInnerClass();
        }
    }
    
    internal class Program
    {
        public static int Main(string[] args)
        {
            new ChildClass();

			return 0;
        }
    }
}

