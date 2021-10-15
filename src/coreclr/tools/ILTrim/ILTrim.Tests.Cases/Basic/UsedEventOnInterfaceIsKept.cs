using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

#pragma warning disable 67

namespace Mono.Linker.Tests.Cases.Basic
{
    [IgnoreTestCase("There is no way to add the event as a root yet")]
    [KeptDelegateCacheField("0")]
    class UsedEventOnInterfaceIsKept
    {
        static void Main()
        {
            IFoo bar = new Bar();
            IFoo jar = new Jar();

            bar.Ping += Bar_Ping;
        }

        [Kept]
        private static void Bar_Ping(object sender, EventArgs e)
        {
        }

        [Kept]
        interface IFoo
        {
            [Kept]
            [KeptEventAddMethod]
            [KeptEventRemoveMethod]
            event EventHandler Ping;
        }

        [KeptMember(".ctor()")]
        [KeptInterface(typeof(IFoo))]
        class Bar : IFoo
        {
            [Kept]
            [KeptBackingField]
            [KeptEventAddMethod]
            [KeptEventRemoveMethod]
            public event EventHandler Ping;
        }

        [KeptMember(".ctor()")]
        [KeptInterface(typeof(IFoo))]
        class Jar : IFoo
        {
            [Kept]
            [KeptBackingField]
            [KeptEventAddMethod]
            [KeptEventRemoveMethod]
            public event EventHandler Ping;
        }
    }
}
