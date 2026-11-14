using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
    [SetupLinkerDescriptorFile("UnusedInterfaceTypeOnTypeWithPreserveAllIsKept.xml")]
    public class UnusedInterfaceTypeOnTypeWithPreserveAllIsKept
    {
        public static void Main()
        {
        }

        [Kept]
        interface IBase
        {
            public static void UnusedMethod() { }
        }

        [Kept]
        [KeptInterface(typeof(IBase))]
        interface IFoo : IBase
        {
            public static void UnusedMethod() { }

            class Nested
            {
                public void Method() { }
            }
        }

        [Kept]
        [KeptMember(".ctor()")]
        [KeptInterface(typeof(IBase))]
        [KeptInterface(typeof(IFoo))]
        class Bar : IFoo
        {
        }
    }
}
