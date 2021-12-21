using System;

namespace DebuggerTests
{
    public class ClassToInspectWithDebugTypeFull
    {
        int a;
        int b;
        int c;
        public ClassToInspectWithDebugTypeFull()
        {
            a = 10;
            b = 20;
            c = 30;
            Console.WriteLine(a);
            Console.WriteLine(b);
            Console.WriteLine(c);
        }
    }
}
