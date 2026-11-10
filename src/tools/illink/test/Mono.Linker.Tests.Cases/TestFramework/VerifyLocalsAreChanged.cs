using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TestFramework
{
    [SetupLinkerSubstitutionFile("VerifyLocalsAreChanged.xml")]
    public class VerifyLocalsAreChanged
    {
        public static void Main()
        {
            TestMethod_1();

            TestMethod_2();
        }

        [Kept]
        struct NestedType
        {
            public NestedType(int arg)
            {
                throw new NotImplementedException();
            }
        }

        [Kept]
        [ExpectBodyModified]
        [ExpectLocalsModified]
        static NestedType TestMethod_1()
        {
            var value = new NestedType(42);
            return value;
        }

        [Kept]
        [ExpectedLocalsSequence(["Mono.Linker.Tests.Cases.TestFramework.VerifyLocalsAreChanged/NestedType"])]
        [ExpectBodyModified]
        static NestedType TestMethod_2()
        {
            var value = new NestedType(2);
            return value;
        }
    }
}
