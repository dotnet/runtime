using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    [IgnoreTestCase("There is no way to add the property as a root yet")]
    class UsedPropertyIsKept
    {
        public static void Main()
        {
            var obj = new B();
            obj.Prop = 1;
            var val = obj.Prop;
        }

        [KeptMember(".ctor()")]
        class B
        {
            [Kept]
            [KeptBackingField]
            public int Prop { [Kept] get; [Kept] set; }
        }
    }
}
