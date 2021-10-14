using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[module:KeptAttributeAttribute(typeof(System.Security.UnverifiableCodeAttribute))]

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
    [SetupCompileArgument("/unsafe")]
    unsafe class FunctionPointer
    {
        [Kept]
        static void Main()
        {
            MethodTakingFunctionPointer(null);
        }

        [Kept]
        static void MethodTakingFunctionPointer(delegate*<OneType, OtherType> del) { }

        [Kept]
        class OneType { }

        [Kept]
        class OtherType { }
    }
}
