using System;

namespace DebuggerTests
{
    public static class ClassToBreak
    {
        public static int TestBreakpoint()
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
