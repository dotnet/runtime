using System;

namespace DebuggerTests
{
    public class NormalClass
    {
        public int myField2;
    }
    
    [System.Diagnostics.DebuggerNonUserCode]
    public class ClassNonUserCodeToInheritThatInheritsFromNormalClass : NormalClass
    {
        private int propA {get;}
        public int propB {get;}
        protected int propC {get;}
        private int d;
        public int e;
        protected int f;
        private int G
        {
            get {return f + 1;}
        }
        private int H => f;

        public ClassNonUserCodeToInheritThatInheritsFromNormalClass()
        {
            propA = 10;
            propB = 20;
            propC = 30;
            d = 40;
            e = 50;
            f = 60;
            Console.WriteLine(propA);
            Console.WriteLine(propB);
            Console.WriteLine(propC);
            Console.WriteLine(d);
            Console.WriteLine(e);
            Console.WriteLine(f);
        }
    }
}
