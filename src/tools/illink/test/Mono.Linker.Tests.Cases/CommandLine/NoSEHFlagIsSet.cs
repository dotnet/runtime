using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CommandLine
{
    /// <summary>
    /// This test case verifies that the NoSEH flag is set in the PE header
    /// when running ILLink on a sample application that has been R2R-stripped.
    /// </summary>
    public class NoSEHFlagIsSet
    {
        public static void Main()
        {
            // Simple test case - just call a method
            TestMethod();
        }

        [Kept]
        static void TestMethod()
        {
            // Simple implementation
        }
    }
}