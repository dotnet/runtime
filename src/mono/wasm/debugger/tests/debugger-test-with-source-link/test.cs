using System;

namespace DebuggerTests
{
    public static partial class ClassToBreak
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int TestBreakpoint()
        {
            return 50;
        }
        public static int valueToCheck = 10;
    }
    public class ClassToCheckFieldValue
    {
        public int valueToCheck;
        public ClassToCheckFieldValue()
        {
            valueToCheck = 20;
        }
    }
}
