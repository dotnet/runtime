using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
    class MultiDimArraySignature
    {
        [Kept]
        static void Main()
        {
            SomeOtherType[,] multiDimArray = new SomeOtherType[4, 5];
        }

        [Kept]
        class SomeOtherType
        {
        }

    }
}
