using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
    class MethodSpecSignature
    {
        [Kept]
        static void Main()
        {
            SomeType.SomeMethod<SomeOtherType>();
        }

        [Kept]
        class SomeType
        {
            [Kept]
            public static void SomeMethod<T>() { }
        }

        [Kept]
        class SomeOtherType
        {
        }

    }
}
