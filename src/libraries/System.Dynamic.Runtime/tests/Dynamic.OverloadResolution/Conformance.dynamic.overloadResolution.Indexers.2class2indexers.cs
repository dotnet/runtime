// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.hide001.hide001
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(28,16\).*CS0108</Expects>
    public class Base
    {
        public static int Status;
        public int this[int x]
        {
            get
            {
                Base.Status = 1;
                return int.MinValue;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public int this[int x]
        {
            get
            {
                Base.Status = 3;
                return int.MaxValue;
            }

            set
            {
                if (value == int.MaxValue)
                    Base.Status = 4;
            }
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
            int xx = d[x];
            if (xx != int.MaxValue || Base.Status != 3)
                return 1;
            d[x] = xx;
            if (Base.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.hide002.hide002
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(29,18\).*CS0108</Expects>
    public class C
    {
    }

    public class Base
    {
        public static int Status;
        public float this[C x]
        {
            get
            {
                Base.Status = 1;
                return float.NaN;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public float this[C x]
        {
            get
            {
                Base.Status = 3;
                return float.NegativeInfinity;
            }

            set
            {
                if (value == float.NegativeInfinity)
                    Base.Status = 4;
            }
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
            C x = new C();
            float f = d[x];
            if (f != float.NegativeInfinity || Base.Status != 3)
                return 1;
            d[x] = f;
            if (Base.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.hide003.hide003
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
    }

    public class Base
    {
        public static int Status;
        public decimal this[C x]
        {
            get
            {
                Base.Status = 1;
                return decimal.One;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public new decimal this[C x]
        {
            get
            {
                Base.Status = 3;
                return decimal.Zero;
            }

            set
            {
                Base.Status = 4;
            }
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
            C x = new C();
            decimal dd = d[x];
            if (dd != decimal.Zero || Base.Status != 3)
                return 1;
            d[x] = dd;
            if (Base.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.overload001.overload001
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public decimal this[short x]
        {
            get
            {
                Base.Status = 1;
                return decimal.MinValue;
            }

            set
            {
                if (value == decimal.MinValue)
                    Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public decimal this[int x]
        {
            get
            {
                Base.Status = 3;
                return decimal.MaxValue;
            }

            set
            {
                Base.Status = 4;
            }
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
            // should call this[Derived]
            decimal dd = d[x];
            if (dd != decimal.MaxValue || Base.Status != 3)
                return 1;
            d[x] = dd;
            if (Base.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.overload002.overload002
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public char this[short x]
        {
            get
            {
                Base.Status = 1;
                return 'a';
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public char this[int x]
        {
            get
            {
                Base.Status = 3;
                return 'b';
            }

            set
            {
                if (value == 'b')
                    Base.Status = 4;
            }
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
            char c = d[x];
            if (c != 'b' || Base.Status != 3)
                return 1;
            d[x] = c;
            if (Base.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.overload003.overload003
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
        public static int Status;
        protected string[] this[short x]
        {
            get
            {
                Base.Status = 1;
                return new string[]
                {
                "foo"
                }

                ;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public string[] this[int x]
        {
            get
            {
                Base.Status = 3;
                return new string[]
                {
                string.Empty
                }

                ;
            }

            set
            {
                if (value.Length == 1 && value[0] == string.Empty)
                    Base.Status = 4;
            }
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
            string[] ss = d[x];
            if (ss.Length != 1 || ss[0] != string.Empty || Base.Status != 3)
                return 1;
            d[x] = ss;
            if (Base.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.overload004.overload004
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
        public static int Status;
        public byte this[short x]
        {
            get
            {
                Base.Status = 1;
                return byte.MinValue;
            }

            set
            {
                if (value == byte.MinValue)
                    Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        protected byte this[int x]
        {
            get
            {
                Base.Status = 3;
                return byte.MaxValue;
            }

            set
            {
                Base.Status = 4;
            }
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
            var x = 3;
            byte bb = d[(short)x];
            if (bb != byte.MinValue || Base.Status != 1)
                return 1;
            d[(short)x] = bb;
            if (Base.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.overload005.overload005
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
        public static int Status;
        public string this[short x]
        {
            get
            {
                Base.Status = 1;
                return "foo";
            }

            set
            {
                if (value == "foo")
                    Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        private string this[int x]
        {
            get
            {
                Base.Status = 3;
                return string.Empty;
            }

            set
            {
                Base.Status = 4;
            }
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
            byte x = 3;
            string s = d[x];
            if (s != "foo" || Base.Status != 1)
                return 1;
            d[x] = s;
            if (Base.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.overload006.overload006
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
        public static int Status;
        internal float this[short x]
        {
            get
            {
                Base.Status = 1;
                return float.NegativeInfinity;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public float this[int x]
        {
            get
            {
                Base.Status = 3;
                return float.NaN;
            }

            set
            {
                if (float.IsNaN(value))
                    Base.Status = 4;
            }
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
            float f = d[x];
            if (!float.IsNaN(f) || Base.Status != 3)
                return 1;
            d[x] = f;
            if (Base.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.overload007.overload007
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
        public static int Status;
        protected internal char this[short x]
        {
            get
            {
                Base.Status = 1;
                return 'a';
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public char this[int x]
        {
            get
            {
                Base.Status = 3;
                return 'b';
            }

            set
            {
                Base.Status = 4;
            }
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
            char c = d[x];
            if (c != 'b' || Base.Status != 3)
                return 1;
            d[x] = c;
            if (Base.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.overload008.overload008
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
        public int this[dynamic x]
        {
            get
            {
                Base.Status = 1;
                return 0;
            }

            set
            {
                Base.Status = 2;
            }
        }

        public int this[short x]
        {
            get
            {
                Base.Status = 3;
                return int.MinValue;
            }

            set
            {
                Base.Status = 4;
            }
        }
    }

    public class Derived : Base
    {
        public int this[int x]
        {
            get
            {
                Base.Status = 5;
                return int.MaxValue;
            }

            set
            {
                if (value == int.MaxValue)
                    Base.Status = 6;
            }
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
            int xx = d[x];
            if (xx != int.MaxValue || Base.Status != 5)
                return 1;
            d[x] = xx;
            if (Base.Status != 6)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.ovr001.ovr001
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public virtual string this[int x]
        {
            get
            {
                Base.Status = 1;
                return string.Empty;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public override string this[int x]
        {
            get
            {
                Base.Status = 3;
                return "foo";
            }

            set
            {
                Base.Status = 4;
            }
        }

        public string this[long l]
        {
            get
            {
                Base.Status = 5;
                return "bar";
            }

            set
            {
                Base.Status = 6;
            }
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
            string ss = d[x];
            if (ss != "bar" || Base.Status != 5)
                return 1;
            d[x] = ss;
            if (Base.Status != 6)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.ovr002.ovr002
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public virtual int this[int x]
        {
            get
            {
                Base.Status = 1;
                return int.MaxValue;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public override int this[int x]
        {
            get
            {
                Base.Status = 3;
                return int.MinValue;
            }

            set
            {
                Base.Status = 4;
            }
        }

        public int this[long l]
        {
            get
            {
                Base.Status = 5;
                return 0;
            }

            set
            {
                Base.Status = 6;
            }
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
            int xx = d[x];
            if (xx != 0 || Base.Status != 5)
                return 1;
            d[x] = xx;
            if (Base.Status != 6)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.ovr003.ovr003
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public virtual string this[string x]
        {
            get
            {
                Base.Status = 1;
                return string.Empty;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public override string this[string x]
        {
            get
            {
                Base.Status = 3;
                return "foo";
            }

            set
            {
                Base.Status = 4;
            }
        }

        public string this[object o]
        {
            get
            {
                Base.Status = 5;
                return "bar";
            }

            set
            {
                Base.Status = 6;
            }
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
            string x = "3";
            string ss = d[x];
            if (ss != "bar" || Base.Status != 5)
                return 1;
            d[x] = ss;
            if (Base.Status != 6)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.ovr004.ovr004
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
    }

    public class D : C
    {
    }

    public class Base
    {
        public static int Status;
        public virtual int this[D x]
        {
            get
            {
                Base.Status = 1;
                return int.MaxValue;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public override int this[D x]
        {
            get
            {
                Base.Status = 3;
                return int.MinValue;
            }

            set
            {
                Base.Status = 4;
            }
        }

        public int this[C o]
        {
            get
            {
                Base.Status = 5;
                return 0;
            }

            set
            {
                Base.Status = 6;
            }
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
            C x = new C();
            int xx = d[x];
            if (xx != 0 || Base.Status != 5)
                return 1;
            d[x] = xx;
            if (Base.Status != 6)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.ovr005.ovr005
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
    }

    public class D : C
    {
    }

    public class Base
    {
        public static int Status;
        public virtual int this[D x]
        {
            get
            {
                Base.Status = 1;
                return int.MaxValue;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public override int this[D x]
        {
            get
            {
                Base.Status = 3;
                return int.MinValue;
            }

            set
            {
                Base.Status = 4;
            }
        }

        public int this[C o]
        {
            get
            {
                Base.Status = 5;
                return 0;
            }

            set
            {
                Base.Status = 6;
            }
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
            D x = new D();
            int xx = d[x];
            if (xx != 0 || Base.Status != 5)
                return 1;
            d[x] = xx;
            if (Base.Status != 6)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.ovr006.ovr006
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
    }

    public class D : C
    {
    }

    public class Base
    {
        public static int Status;
        public virtual int this[D x]
        {
            get
            {
                Base.Status = 1;
                return 1;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public override int this[D x]
        {
            get
            {
                Base.Status = 3;
                return 3;
            }

            set
            {
                Base.Status = 4;
            }
        }

        public int this[C o]
        {
            get
            {
                Base.Status = 5;
                return 5;
            }

            set
            {
                Base.Status = 6;
            }
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
            C x = new D();
            int xx = d[x];
            if (xx != 5 || Base.Status != 5)
                return 1;
            d[x] = xx;
            if (Base.Status != 6)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.ovr007.ovr007
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
    }

    public class D : C
    {
    }

    public class Base
    {
        public static int Status;
        public virtual int this[int x]
        {
            get
            {
                Base.Status = 1;
                return 1;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public override int this[int x]
        {
            get
            {
                Base.Status = 3;
                return 3;
            }

            set
            {
                Base.Status = 4;
            }
        }

        public int this[string o]
        {
            get
            {
                Base.Status = 5;
                return 5;
            }

            set
            {
                Base.Status = 6;
            }
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
            int xx = d[x];
            if (xx != 3 || Base.Status != 3)
                return 1;
            d[x] = xx;
            if (Base.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.ovr008.ovr008
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
    }

    public class Base
    {
        public static int Status;
        public virtual int this[int x]
        {
            get
            {
                Base.Status = 1;
                return 1;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public override int this[int x]
        {
            get
            {
                Base.Status = 3;
                return 3;
            }

            set
            {
                Base.Status = 4;
            }
        }

        public int this[C o]
        {
            get
            {
                Base.Status = 5;
                return 5;
            }

            set
            {
                Base.Status = 6;
            }
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
            int xx = d[x];
            if (xx != 3 || Base.Status != 3)
                return 1;
            d[x] = xx;
            if (Base.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.ovr009.ovr009
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public enum E
    {
        first,
        second
    }

    public class Base
    {
        public static int Status;
        public virtual int this[string x]
        {
            get
            {
                Base.Status = 1;
                return 1;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public override int this[string x]
        {
            get
            {
                Base.Status = 3;
                return 3;
            }

            set
            {
                Base.Status = 4;
            }
        }

        public int this[E o]
        {
            get
            {
                Base.Status = 5;
                return 5;
            }

            set
            {
                Base.Status = 6;
            }
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
            string x = "adfs";
            int xx = d[x];
            if (xx != 3 || Base.Status != 3)
                return 1;
            d[x] = xx;
            if (Base.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.ovr010.ovr010
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
        public static implicit operator int (C c)
        {
            return 0;
        }
    }

    public class Base
    {
        public static int Status;
        public virtual int this[C x]
        {
            get
            {
                Base.Status = 1;
                return 1;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public override int this[C x]
        {
            get
            {
                Base.Status = 3;
                return 3;
            }

            set
            {
                Base.Status = 4;
            }
        }

        public int this[int o]
        {
            get
            {
                Base.Status = 5;
                return 5;
            }

            set
            {
                Base.Status = 6;
            }
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
            C x = new C(); //This should go to Base.Status = 3
            int xx = d[x];
            if (xx != 5 || Base.Status != 5)
                return 1;
            d[x] = xx;
            if (Base.Status != 6)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.ovr011.ovr011
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
        public static implicit operator int (C c)
        {
            return 0;
        }
    }

    public class Base
    {
        public static int Status;
        public virtual int this[C x]
        {
            get
            {
                Base.Status = 1;
                return 1;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public override int this[C x]
        {
            get
            {
                Base.Status = 3;
                return 3;
            }

            set
            {
                Base.Status = 4;
            }
        }

        public int this[long o]
        {
            get
            {
                Base.Status = 5;
                return 5;
            }

            set
            {
                Base.Status = 6;
            }
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
            C x = new C(); //This should go to Base.Status = 3
            int xx = d[x];
            if (xx != 5 || Base.Status != 5)
                return 1;
            d[x] = xx;
            if (Base.Status != 6)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.ovr012.ovr012
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public virtual int this[int x]
        {
            get
            {
                Base.Status = 1;
                return 1;
            }

            set
            {
                Base.Status = 2;
            }
        }

        public virtual int this[long y]
        {
            get
            {
                Base.Status = 3;
                return 3;
            }

            set
            {
                Base.Status = 4;
            }
        }
    }

    public class Derived : Base
    {
        public override int this[long x]
        {
            get
            {
                Base.Status = 5;
                return 5;
            }

            set
            {
                Base.Status = 6;
            }
        }
    }

    public class FurtherDerived : Derived
    {
        public override int this[int y]
        {
            get
            {
                Base.Status = 7;
                return 7;
            }

            set
            {
                Base.Status = 8;
            }
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
            dynamic d = new FurtherDerived();
            long x = 3;
            int xx = d[x];
            if (xx != 5 || Base.Status != 5)
                return 1;
            d[x] = xx;
            if (Base.Status != 6)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.ovrdynamic001.ovrdynamic001
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public virtual int this[object x]
        {
            get
            {
                Base.Status = 1;
                return 1;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public override int this[dynamic x]
        {
            get
            {
                Base.Status = 3;
                return 2;
            }

            set
            {
                Base.Status = 4;
            }
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
            Base x = new Base(); //This should go to Base.Status = 3
            int xx = d[x];
            if (xx != 2 || Base.Status != 3)
                return 1;
            d[x] = xx;
            if (Base.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Twoclass2indexers.ovrdynamic002.ovrdynamic002
{
    // <Title> Tests overload resolution for 2 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static int Status;
        public virtual int this[dynamic x]
        {
            get
            {
                Base.Status = 1;
                return 1;
            }

            set
            {
                Base.Status = 2;
            }
        }
    }

    public class Derived : Base
    {
        public override int this[object x]
        {
            get
            {
                Base.Status = 3;
                return 2;
            }

            set
            {
                Base.Status = 4;
            }
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
            Base x = new Base(); //This should go to Base.Status = 3
            int xx = d[x];
            if (xx != 2 || Base.Status != 3)
                return 1;
            d[x] = xx;
            if (Base.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}
