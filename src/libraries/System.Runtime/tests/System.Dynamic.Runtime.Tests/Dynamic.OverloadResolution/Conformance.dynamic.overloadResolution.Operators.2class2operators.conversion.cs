// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.conversion.overload001.overload001
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static implicit operator short (Base b)
        {
            return short.MinValue;
        }
    }

    public class Derived : Base
    {
        public static implicit operator short (Derived x)
        {
            return short.MaxValue;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Derived();
            short x = d;
            if (x == short.MaxValue)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.conversion.overload002.overload002
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static implicit operator short (Base x)
        {
            return short.MinValue;
        }
    }

    public class Derived : Base
    {
        public static implicit operator int (Derived x)
        {
            return int.MaxValue;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Derived();
            int x = d;
            if (x == int.MaxValue)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.conversion.overload004.overload004
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // Call methods with different accessibility level and selecting the right one.:)
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static explicit operator short (Base x)
        {
            return short.MinValue;
        }
    }

    public class Derived : Base
    {
        public static explicit operator int (Derived x)
        {
            return int.MaxValue;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Base();
            short x = (short)d;
            if (x == short.MinValue)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.conversion.overload006.overload006
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // Call methods with different accessibility level and selecting the right one.:)
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static implicit operator short (Base x)
        {
            return short.MinValue;
        }
    }

    public class Derived : Base
    {
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Derived();
            short x = d;
            if (x == short.MinValue)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.conversion.overload007.overload007
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // Call methods with different accessibility level and selecting the right one.:)
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public int Field;
        public static implicit operator Base(short x)
        {
            return new Base()
            {
                Field = x
            }

            ;
        }
    }

    public class Derived
    {
        public static explicit operator short (Derived x)
        {
            return short.MinValue;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Derived();
            Base x = (short)d;
            if (x.Field == short.MinValue)
                return 0;
            return 1;
        }
    }
    // </Code>
}
