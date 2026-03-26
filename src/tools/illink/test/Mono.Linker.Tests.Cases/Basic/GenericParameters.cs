using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
	class GenericParameters
	{
        [Kept]
        public static void Main()
        {
            var t = typeof(A<>).ToString();
        }

        [Kept]
        class A<T>
        {
        }
    }
}
