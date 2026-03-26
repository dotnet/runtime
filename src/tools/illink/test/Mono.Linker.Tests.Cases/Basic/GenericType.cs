using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
    class GenericType
    {
        [Kept]
        public static void Main()
        {
            var c = new C();
        }
    }

    [Kept]
    [KeptMember(".ctor()")]
    class A<T>
    {
    }

    [Kept]
    [KeptMember(".ctor()")]
    [KeptBaseType(typeof(A<string>))]
    class B<T> : A<string>
    {
    }

    [Kept]
    [KeptMember(".ctor()")]
    [KeptBaseType(typeof(B<int>))]
    class C : B<int>
    {
    }
}