using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
    public class NewConstrainedGenericAttributeArgumentConstructorIsKept
    {
        public static void Main()
        {
            // Accessing the attributes via reflection forces the attribute (and its
            // generic instantiation) to be kept. The new() constraint on the attribute's
            // type parameter requires the public parameterless constructor of the
            // generic argument to be preserved, otherwise the runtime throws a
            // TypeLoadException when materializing the attribute.
            typeof(CheckBox).GetCustomAttributes(false);
        }

        [Kept]
        public class Handler
        {
            [Kept]
            public Handler() { }
        }

        [Kept]
        [KeptBaseType(typeof(Attribute))]
        class MyAttribute<T> : Attribute where T : new()
        {
            [Kept]
            public MyAttribute() { }
        }

        [Kept]
        [KeptAttributeAttribute(typeof(MyAttribute<Handler>))]
        [My<Handler>]
        class CheckBox
        {
        }
    }
}
