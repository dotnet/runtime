using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    [IgnoreTestCase("There is no way to add the property as a root yet")]
    class UnusedPropertySetterRemoved
    {
        public static void Main()
        {
            var val = new UnusedPropertySetterRemoved.B().PartiallyUsed;
        }

        [KeptMember(".ctor()")]
        class B
        {
            [Kept] // FIXME: Should be removed
            [KeptBackingField]
            public int PartiallyUsed { [Kept] get; set; }
        }
    }
}
