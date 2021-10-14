using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[module: KeptAttributeAttribute(typeof(System.Security.UnverifiableCodeAttribute))]

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
    [SetupCompileArgument("/unsafe")]
    unsafe class TypeSpecSignature
    {
        [Kept]
        static void Main()
        {
            var reflectedType = typeof(SomeType<SomeOtherType>);
            TestUnsafe();
        }

        [Kept]
        unsafe static void TestUnsafe()
        {
            void* p = null;
        }

        [Kept]
        class SomeType<T> { }

        [Kept]
        class SomeOtherType { }
    }
}
