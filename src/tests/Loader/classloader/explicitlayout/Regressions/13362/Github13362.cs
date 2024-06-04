using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace ClrIssueRepro
{
    // If you remove '[StructLayout(LayoutKind.Sequential)]', you get:
    // Unhandled exception. System.TypeLoadException: 
    // Could not load type 'ClrIssueRepro.GenInt' from assembly '...' 
    //    because the format is invalid.
    //  at ClrIssueRepro.Program.Main(String[] args)
    [StructLayout(LayoutKind.Sequential)]
    public class GenBase<T>
    {
        public string _string0 = "string0";
        public string _string1 = "string1";
    }

    [StructLayout(LayoutKind.Explicit)]
    public class GenInt : GenBase<int>
    {
        // Commenting out either one of these fields fixes things!?
        [FieldOffset(0)] public string _sstring0 = "string0";
        [FieldOffset(16)] public string _sstring1 = "string1";
    }

    // This works! (it's GenInt with [StructLayout(LayoutKind.Explicit)] and [FieldOffset(..)] removed)
    public class GenIntNormal : GenBase<int>
    {
        public string _sstring0 = "string0";
        public string _sstring1 = "string1";
    }

    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            // If you comment this line out, you get
            //    Unhandled exception. System.TypeLoadException: 
            //    Could not load type 'ClrIssueRepro.GenInt' from assembly '...'
            //    at ClrIssueRepro.Program.Main(String[] args)
            // in Debug and Release builds?!
            Type type = typeof(GenInt);

            object instance = new GenInt();

            // GenIntNormal has the same fields as GenInt, but has
            // [StructLayout(LayoutKind.Explicit)] and [FieldOffset(..)] REMOVED
            //object instance = new GenIntNormal(); // works fine!!

            string instType = instance.GetType().ToString();
            Console.WriteLine(instType);
            return "ClrIssueRepro.GenInt" == instType ? 100 : 0;
        }
    }
}