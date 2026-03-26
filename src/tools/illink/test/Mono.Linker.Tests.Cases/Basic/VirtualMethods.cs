using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
#pragma warning disable 169

    [Kept]
    public class VirtualMethods
    {
        [Kept]
        static void Main()
        {
            BaseType b = new DerivedType();
            b.Method1();

            // TODO: uncomment once we're okay with ToString bringing the whole world into closure
            //((object)default(MyValueType)).ToString();
        }
    }

    [Kept]
    class BaseType
    {
        [Kept]
        public BaseType() { }
        [Kept]
        public virtual void Method1() { }
        public virtual void Method2() { }
    }

    [Kept]
    [KeptBaseType(typeof(BaseType))]
    class DerivedType : BaseType
    {
        [Kept]
        public DerivedType() { }
        [Kept]
        public override void Method1() { }
        public override void Method2() { }
    }

    //[Kept]
    //struct MyValueType
    //{
    //    [Kept]
    //    public override string ToString() => "";
    //}
}
