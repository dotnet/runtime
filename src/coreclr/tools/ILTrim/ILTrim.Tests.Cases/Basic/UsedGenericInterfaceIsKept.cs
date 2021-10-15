using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    [IgnoreTestCase("There is no way to add the event as a root yet")]
    class UsedGenericInterfaceIsKept
    {
        public static void Main()
        {
            A<int> a = new A<int>();
            var t = typeof(I<>).ToString();
        }

        [Kept]
        [KeptInterface(typeof(I<>))]
        [KeptMember(".ctor()")]
        class A<T> : I<T>
        {
        }

        [Kept]
        interface I<T>
        {
        }
    }
}
