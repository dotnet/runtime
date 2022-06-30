using System;

namespace DebuggerTests
{
    public class ClassWithPdbDeleted
    {
        private int propA {get;}
        public int propB {get;}
        protected int propC {get;}
        public ClassWithPdbDeleted()
        {
            propA = 10;
            propB = 20;
            propC = 30;
            Console.WriteLine(propA);
            Console.WriteLine(propB);
            Console.WriteLine(propC);
        }
    }
}
