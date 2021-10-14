using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
    class TypeSpecSignature
    {
        [Kept]
        static void Main()
        {
            var reflectedType = typeof(SomeType<SomeOtherType>);
        }

        [Kept]
        class SomeType<T> { }

        [Kept]
        class SomeOtherType { }
    }
}
