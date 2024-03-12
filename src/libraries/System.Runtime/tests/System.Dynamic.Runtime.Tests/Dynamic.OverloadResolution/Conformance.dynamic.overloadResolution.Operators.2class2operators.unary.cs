// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.unary.overload001.overload001
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static short operator +(Base x)
        {
            return short.MinValue;
        }
    }

    public class Derived : Base
    {
        public static int operator +(Derived x)
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
            int s = +d;
            if (s == short.MaxValue)
                return 0;
            System.Console.WriteLine("Failed!");
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.unary.overload002.overload002
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static short operator +(Base x)
        {
            return short.MinValue;
        }
    }

    public class Derived : Base
    {
        public static short operator +(Derived x)
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
            short s = +d;
            if (s == short.MaxValue)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.unary.overload003.overload003
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // Select the best method to call.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public int Field;
        public static Base operator ++(Base x)
        {
            x.Field = 3;
            return x;
        }
    }

    public class Derived : Base
    {
        public static Derived operator ++(Derived x)
        {
            x.Field = 4;
            return x;
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
            dynamic x = 3;
            dynamic dd = ++d;
            if (dd.Field == 4)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.unary.overload004.overload004
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // Select the best method to call.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public int Field;
        public static Base operator --(Base x)
        {
            x.Field = 3;
            return x;
        }
    }

    public class Derived : Base
    {
        public static Derived operator --(Derived x)
        {
            x.Field = 4;
            return x;
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
            dynamic x = 3;
            dynamic dd = --d;
            if (dd.Field == 4)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.unary.overload005.overload005
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // Select the best method to call.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public static bool operator true(Base x)
        {
            Base.Status = 1;
            return true;
        }

        public static bool operator false(Base x)
        {
            Base.Status = 2;
            return false;
        }
    }

    public class Derived : Base
    {
        public static bool operator true(Derived x)
        {
            return false;
        }

        public static bool operator false(Derived x)
        {
            return true;
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
            // Derived op 'true' invoked -> false
            if (d)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.unary.overload008.overload008
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // Select the best method to call.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static dynamic operator -(Base x)
        {
            return 1;
        }
    }

    public class Derived : Base
    {
        public static short operator -(Derived x)
        {
            return 2;
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
            dynamic x = 3;
            dynamic dd = -d;
            if (dd == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.unary.overload009.overload009
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // Select the best method to call.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static dynamic operator -(Base x)
        {
            return 1;
        }
    }

    public class Derived : Base
    {
        public static short operator -(Derived x)
        {
            return 2;
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
            dynamic x = 3;
            dynamic dd = -d;
            if (dd == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}
