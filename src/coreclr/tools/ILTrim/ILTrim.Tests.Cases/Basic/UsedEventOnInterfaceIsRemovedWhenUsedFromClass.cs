using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

#pragma warning disable 67

namespace Mono.Linker.Tests.Cases.Basic
{
    [IgnoreTestCase("There is no way to add the event as a root yet")]
    [KeptDelegateCacheField("0")]
    class UsedEventOnInterfaceIsRemovedWhenUsedFromClass
    {
        static void Main()
        {
            var bar = new Bar();
            var jar = new Jar();

            bar.Ping += Bar_Ping;
        }

        [Kept]
        private static void Bar_Ping(object sender, EventArgs e)
        {
        }

        interface IFoo
        {
            event EventHandler Ping;
        }

        [KeptMember(".ctor()")]
        class Bar : IFoo
        {
            [Kept]
            [KeptBackingField]
            [KeptEventAddMethod]
            [KeptEventRemoveMethod]
            public event EventHandler Ping;
        }

        [KeptMember(".ctor()")]
        class Jar : IFoo
        {
            public event EventHandler Ping;
        }
    }
}
