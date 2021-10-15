using Mono.Linker.Tests.Cases.Expectations.Assertions;

#pragma warning disable 649

namespace Mono.Linker.Tests.Cases.Basic
{
    class UnusedFieldGetsRemoved
    {
        public static void Main()
        {
            new B().Method();
        }

        [KeptMember(".ctor()")]
        class B
        {
            public int _unused;

            [Kept]
            public void Method()
            {
            }
        }
    }
}
