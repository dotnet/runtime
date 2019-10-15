// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.opOverload.explicit01.explicit01
{
    // <Area>Declaration of Optional Params</Area>
    // <Title> Explicit User defined conversions</Title>
    // <Description>User-defined conversions with defaults</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(10,39\).*CS1066</Expects>
    public class Derived
    {
        public static explicit operator int (Derived d = null)
        {
            if (d != null)
                return 0;
            return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            int result = (int)tf;
            return result;
        }
    }
    //</code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.opOverload.explicit02.explicit02
{
    // <Area>Declaration of Optional Params</Area>
    // <Title> Explicit User defined conversions</Title>
    // <Description>User-defined conversions with defaults</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(12,41\).*CS1066</Expects>
    public class Derived
    {
        public static explicit operator int (Derived d = default(Derived))
        {
            if (d == null)
                return 0;
            return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived p = null;
            dynamic tf = p;
            try
            {
                int result = (int)tf;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.ValueCantBeNull, e.Message, "int");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.opOverload.explicit04.explicit04
{
    // <Area>Declaration of Optional Params</Area>
    // <Title> Explicit User defined conversions</Title>
    // <Description>User-defined conversions with defaults</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,39\).*CS1066</Expects>
    public class Derived
    {
        private const Derived x = null;
        public static explicit operator int (Derived d = x)
        {
            if (d != null)
                return 0;
            return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            int result = (int)tf;
            return result;
        }
    }
    //</code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.opOverload.explicit05.explicit05
{
    // <Area>Declaration of Optional Params</Area>
    // <Title> Explicit User defined conversions</Title>
    // <Description>User-defined conversions with defaults</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,39\).*CS1066</Expects>
    public class Derived
    {
        private const Derived x = null;
        public static explicit operator int (Derived d = true ? x : x)
        {
            if (d != null)
                return 0;
            return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            int result = (int)tf;
            return result;
        }
    }
    //</code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.opOverload.implicit01.implicit01
{
    // <Area>Declaration of Optional Params</Area>
    // <Title> Explicit User defined conversions</Title>
    // <Description>User-defined conversions with defaults</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(10,39\).*CS1066</Expects>
    public class Derived
    {
        public static implicit operator int (Derived d = null)
        {
            if (d != null)
                return 0;
            return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            int result = tf;
            return result;
        }
    }
    //</code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.opOverload.implicit02.implicit02
{
    // <Area>Declaration of Optional Params</Area>
    // <Title> Explicit User defined conversions</Title>
    // <Description>User-defined conversions with defaults</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,39\).*CS1066</Expects>
    public class Derived
    {
        public static implicit operator int (Derived d = default(Derived))
        {
            if (d == null)
                return 0;
            return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived p = null;
            dynamic tf = p;
            try
            {
                int result = tf;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.ValueCantBeNull, e.Message, "int");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.opOverload.implicit04.implicit04
{
    // <Area>Declaration of Optional Params</Area>
    // <Title> Explicit User defined conversions</Title>
    // <Description>User-defined conversions with defaults</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,39\).*CS1066</Expects>
    public class Derived
    {
        private const Derived x = null;
        public static implicit operator int (Derived d = x)
        {
            if (d != null)
                return 0;
            return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            int result = tf;
            return result;
        }
    }
    //</code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.opOverload.implicit05.implicit05
{
    // <Area>Declaration of Optional Params</Area>
    // <Title> Explicit User defined conversions</Title>
    // <Description>User-defined conversions with defaults</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,39\).*CS1066</Expects>
    public class Derived
    {
        private const Derived x = null;
        public static implicit operator int (Derived d = true ? x : x)
        {
            if (d != null)
                return 0;
            return 1;
        }
    }

    public class TestFunction
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic tf = new Derived();
            int result = tf;
            return result;
        }
    }
    //</code>
}
