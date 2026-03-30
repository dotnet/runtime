using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
    class InterfaceCalls
    {
        [Kept]
        static void Main()
        {
            ISimpleInterface simpleInterface = new SimpleType();
            simpleInterface.InterfaceMethod();
        }

        [Kept]
        interface ISimpleInterface
        {
            [Kept]
            void InterfaceMethod();
            void UnusedMethod();
        }

        interface ISimpleUnusedInterface { }

        [Kept]
        [KeptMember(".ctor()")]
        [KeptInterface(typeof(ISimpleInterface))]
        class SimpleType : ISimpleInterface, ISimpleUnusedInterface
        {
            [Kept]
            public virtual void InterfaceMethod() { }
            public virtual void UnusedMethod() { }
        }
    }
}
