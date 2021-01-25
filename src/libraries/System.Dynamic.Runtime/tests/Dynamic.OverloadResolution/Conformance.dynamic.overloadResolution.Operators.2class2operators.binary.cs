// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.binary.overload001.overload001
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(11,23\).*CS0649</Expects>
    public class Base
    {
        public static int Status;
        public static int operator +(short x, Base b)
        {
            return int.MinValue;
        }
    }

    public class Derived : Base
    {
        public static int operator +(int x, Derived d)
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
            short x = 3;
            int xx = x + d;
            if (xx == int.MaxValue)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.binary.overload002.overload002
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static float operator -(short x, Base b)
        {
            return float.Epsilon;
        }
    }

    public class Derived : Base
    {
        public static float operator -(int x, Derived d)
        {
            return float.NegativeInfinity;
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
            int x = 3;
            float f = x - d;
            if (float.IsNegativeInfinity(f))
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.binary.overload003.overload003
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
        public static double operator *(Base b, short x)
        {
            return double.MinValue;
        }
    }

    public class Derived : Base
    {
        public static double operator *(Derived d, int x)
        {
            return double.MaxValue;
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
            int x = 3;
            double dd = d * x;
            if (dd == double.MaxValue)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.binary.overload004.overload004
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
        public static short operator /(Base x, short d)
        {
            return short.MinValue;
        }
    }

    public class Derived : Base
    {
        public static short operator /(Derived x, int d)
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
            int x = 3;
            short s = d / x;
            if (s == short.MaxValue)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.binary.overload005.overload005
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
        public static int?[] operator %(short x, Base b)
        {
            return new int?[]
            {
            1, 2, null
            }

            ;
        }
    }

    public class Derived : Base
    {
        public static int?[] operator %(int x, Derived d)
        {
            return new int?[]
            {
            null, null
            }

            ;
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
            int x = 3;
            int?[] xx = x % d;
            if (xx.Length == 2)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.binary.overload006.overload006
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
        public static string operator &(Base b, short x)
        {
            return string.Empty;
        }
    }

    public class Derived : Base
    {
        public static string operator &(Derived d, int x)
        {
            return "foo";
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
            int x = 3;
            string s = d & x;
            if (s == "foo")
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.binary.overload007.overload007
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
        public static byte[] operator |(short x, Base b)
        {
            return new byte[]
            {
            1, 2, 3
            }

            ;
        }
    }

    public class Derived : Base
    {
        public static byte[] operator |(int x, Derived d)
        {
            return new byte[]
            {
            1, 2
            }

            ;
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
            int x = 3;
            byte[] b = x | d;
            if (b.Length == 2)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.binary.overload008.overload008
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
        public static decimal operator ^(dynamic x, Base b)
        {
            return decimal.Zero;
        }

        public static decimal operator ^(short x, Base b)
        {
            return decimal.One;
        }
    }

    public class Derived : Base
    {
        public static decimal operator ^(int x, Derived d)
        {
            return decimal.MinusOne;
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
            decimal dec = x ^ d;
            if (dec == decimal.MinusOne)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Operators.Twoclass2operates.binary.overload009.overload009
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
        public static decimal operator ^(dynamic x, Base b)
        {
            return decimal.Zero;
        }

        public static decimal operator ^(short x, Base b)
        {
            return decimal.One;
        }
    }

    public class Derived : Base
    {
        public static decimal operator ^(int x, Derived d)
        {
            return decimal.MinusOne;
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
            decimal dec = x ^ d;
            if (dec == decimal.Zero)
                return 0;
            return 1;
        }
    }
    // </Code>
}
