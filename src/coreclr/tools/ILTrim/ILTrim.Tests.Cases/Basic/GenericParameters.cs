using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
	class GenericParameters
	{
        [Kept]
        public static void Main()
        {
            // We will generate corrupted IL in the method body until we fix the signature parsing
            //var t = typeof(A<>).ToString();
        }
        /*
        [Kept]
        class A<T>
        {
        }
        */
    }
}
