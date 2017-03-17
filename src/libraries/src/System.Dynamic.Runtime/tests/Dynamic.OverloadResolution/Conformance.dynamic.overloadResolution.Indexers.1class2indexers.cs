// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.diffnumberprms001.diffnumberprms001
{
    // <Title> Tests overload resolution for 1 class and 2 indexers</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public short this[int x]
        {
            get
            {
                Target.Status = 1;
                return 2;
            }

            set
            {
                if (value == 5)
                    Target.Status = 2;
                else
                    Target.Status = 3;
            }
        }

        public short this[int x, int y]
        {
            get
            {
                Target.Status = 5;
                return 2;
            }

            set
            {
                if (value == 5)
                    Target.Status = 6;
                else
                    Target.Status = 7;
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
            dynamic d = new Target();
            int x = 3;
            short s = d[x];
            if (s != 2 || Target.Status != 1)
                return 1;
            d[x] = 5;
            if (Target.Status != 2)
                return 1;
            s = d[x, x];
            if (s != 2 || Target.Status != 5)
                return 1;
            d[x, x] = 5;
            if (Target.Status != 6)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.diffnumberprms002.diffnumberprms002
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public float this[int x]
        {
            get
            {
                Target.Status = 1;
                return 2f;
            }

            set
            {
                if (value == 5f)
                    Target.Status = 2;
                else
                    Target.Status = 3;
            }
        }

        public float this[int x, params int[] xx]
        {
            get
            {
                Target.Status = 5;
                return 2f;
            }

            set
            {
                if (value == 5f)
                    Target.Status = 6;
                else
                    Target.Status = 7;
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
            dynamic d = new Target();
            int x = 3;
            float s = d[x];
            if (s != 2f || Target.Status != 1)
                return 1;
            d[x] = 5;
            if (Target.Status != 2)
                return 1;
            s = d[x, x];
            if (s != 2f || Target.Status != 5)
                return 1;
            d[x, x] = 5;
            if (Target.Status != 6)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.moreprms001.moreprms001
{
    // <Title> Tests overload resolution for 1 class and 2 indexers</Title>
    // <Description>
    // This tests overload resolution for more than 2 params at a sanity level
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public decimal this[int x, float f, string s, decimal d]
        {
            get
            {
                Target.Status = 1;
                return 1m;
            }

            set
            {
                if (value == 5m)
                    Target.Status = 2;
                else
                    Target.Status = 3;
            }
        }

        public decimal this[int x, float f, decimal d, string s]
        {
            get
            {
                Target.Status = 5;
                return 2m;
            }

            set
            {
                if (value == 5m)
                    Target.Status = 6;
                else
                    Target.Status = 7;
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
            dynamic d = new Target();
            int x = 3;
            decimal s = d[x, 3f, 54m, string.Empty];
            if (s != 2m || Target.Status != 5)
                return 1;
            d[x, 3f, 54m, string.Empty] = 5m;
            if (Target.Status != 6)
                return 1;
            s = d[x, 3f, string.Empty, 54m];
            if (s != 1m || Target.Status != 1)
                return 1;
            d[x, 3f, string.Empty, 54m] = 5m;
            if (Target.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.negtwoprms001.negtwoprms001
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // This will have an ambiguity at runtime
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public char this[int x, float f]
        {
            get
            {
                Target.Status = 1;
                return 'b';
            }

            set
            {
                if (value == 'a')
                    Target.Status = 2;
                else
                    Target.Status = 3;
            }
        }

        public char this[float f, int x]
        {
            get
            {
                Target.Status = 5;
                return 'b';
            }

            set
            {
                if (value == 'a')
                    Target.Status = 6;
                else
                    Target.Status = 7;
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
            dynamic d = new Target();
            int x = 3;
            bool ret = true;
            try
            {
                char s = d[x, x];
                d[x, x] = 'a';
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "Target.this[int, float]", "Target.this[float, int]");
                if (ret)
                    return 0;
                System.Console.WriteLine("Unexpected error message - " + ex);
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.negtwoprms002.negtwoprms002
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // This will have an ambiguity at runtime
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public char this[int x, dynamic f]
        {
            get
            {
                Target.Status = 1;
                return 'b';
            }

            set
            {
                if (value == 'a')
                    Target.Status = 2;
                else
                    Target.Status = 3;
            }
        }

        public char this[dynamic f, int x]
        {
            get
            {
                Target.Status = 5;
                return 'b';
            }

            set
            {
                if (value == 'a')
                    Target.Status = 6;
                else
                    Target.Status = 7;
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
            dynamic d = new Target();
            int x = 3;
            object o = 5;
            char s = d[x, o];
            if (s != 'b' || Target.Status != 1)
                return 1;
            int ret = 0;
            bool b = true;
            try
            {
                d[x, x] = 'a';
                ret = 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                b = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "Target.this[int, object]", "Target.this[object, int]");
                if (!b)
                {
                    ret = 1;
                    System.Console.WriteLine("Unexpected error message - " + ex);
                }
            }

            s = d[o, x];
            if (s != 'b' || Target.Status != 5)
                ret = 1;
            try
            {
                d[x, x] = 'a';
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                b = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "Target.this[int, object]", "Target.this[object, int]");
                if (!b)
                {
                    ret = 1;
                    System.Console.WriteLine("Unexpected error message - " + ex);
                }
            }

            return ret;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.onedynamicparam001.onedynamicparam001
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public object this[int x]
        {
            get
            {
                Target.Status = 1;
                return null;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public object this[string x]
        {
            get
            {
                Target.Status = 3;
                return null;
            }

            set
            {
                Target.Status = 4;
            }
        }

        public object this[object x]
        {
            get
            {
                Target.Status = 5;
                return null;
            }

            set
            {
                Target.Status = 6;
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
            Target t = new Target();
            dynamic d = 4;
            dynamic dd = t[d];
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.onedynamicparam002.onedynamicparam002
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public int this[int x]
        {
            get
            {
                Target.Status = 1;
                return 1;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public int this[string x]
        {
            get
            {
                Target.Status = 3;
                return 1;
            }

            set
            {
                Target.Status = 4;
            }
        }

        public int this[object x]
        {
            get
            {
                Target.Status = 5;
                return 1;
            }

            set
            {
                Target.Status = 6;
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
            Target t = new Target();
            dynamic d = 4;
            int x = t[d];
            if (x == 1 && Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.onedynamicparam003.onedynamicparam003
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myClass
    {
        public int Foo = 2;
    }

    public class Target
    {
        public static int Status;
        public myClass this[int x]
        {
            get
            {
                Target.Status = 1;
                return new myClass();
            }

            set
            {
                Target.Status = 2;
            }
        }

        public object this[string x]
        {
            get
            {
                Target.Status = 3;
                return new myClass();
            }

            set
            {
                Target.Status = 4;
            }
        }

        public object this[object x]
        {
            get
            {
                Target.Status = 5;
                return new myClass();
            }

            set
            {
                Target.Status = 6;
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
            Target t = new Target();
            dynamic d = 4;
            object o = t[d];
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.onedynamicparam004.onedynamicparam004
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public dynamic this[int x]
        {
            get
            {
                Target.Status = 1;
                return 2;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public object this[string x]
        {
            get
            {
                Target.Status = 3;
                return 3;
            }

            set
            {
                Target.Status = 4;
            }
        }

        public object this[object x]
        {
            get
            {
                Target.Status = 5;
                return 4;
            }

            set
            {
                Target.Status = 6;
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
            Target t = new Target();
            dynamic d = t[4];
            if (Target.Status == 1 && t[d].ToString() == "2")
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparam005.oneparam005
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public byte this[int x]
        {
            get
            {
                Target.Status = 1;
                return byte.MinValue;
            }

            set
            {
                if (value == byte.MinValue)
                    Target.Status = 2;
            }
        }

        public byte this[params int[] x]
        {
            get
            {
                Target.Status = 3;
                return byte.MaxValue;
            }

            set
            {
                Target.Status = 4;
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
            dynamic d = new Target();
            int x = 3;
            byte b = d[x];
            if (b != byte.MinValue || Target.Status != 1)
                return 1;
            d[x] = b;
            if (Target.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparam006.oneparam006
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public int this[int x]
        {
            get
            {
                Target.Status = 1;
                return int.MinValue;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public int this[params int[] x]
        {
            get
            {
                Target.Status = 3;
                return int.MaxValue;
            }

            set
            {
                if (value == int.MaxValue)
                    Target.Status = 4;
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
            dynamic d = new Target();
            int x = 3;
            int i = d[x, x];
            if (i != int.MaxValue || Target.Status != 3)
                return 1;
            d[x, x] = i;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparam013.oneparam013
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public string this[dynamic x]
        {
            get
            {
                Target.Status = 1;
                return string.Empty;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public string this[int x]
        {
            get
            {
                Target.Status = 3;
                return "foo";
            }

            set
            {
                if (value == "foo")
                    Target.Status = 4;
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
            dynamic d = new Target();
            int x = 2;
            string s = d[x];
            if (Target.Status != 3 || s != "foo")
                return 1;
            d[x] = s;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparam014.oneparam014
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public float? this[dynamic x]
        {
            get
            {
                Target.Status = 1;
                return float.Epsilon;
            }

            set
            {
                if (value == float.Epsilon)
                    Target.Status = 2;
            }
        }

        public float? this[int x]
        {
            get
            {
                Target.Status = 3;
                return float.NegativeInfinity;
            }

            set
            {
                Target.Status = 4;
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
            dynamic d = new Target();
            object x = 2;
            float? f = d[x];
            if (Target.Status != 1 || f != float.Epsilon)
                return 1;
            d[x] = f;
            if (Target.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparam015.oneparam015
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public int[] this[int x]
        {
            get
            {
                Target.Status = 1;
                return new int[]
                {
                1, 2, 3
                }

                ;
            }

            set
            {
                if (value[0] == 1 && value[1] == 2 && value[2] == 3 && value.Length == 3)
                    Target.Status = 2;
            }
        }

        public int[] this[dynamic x]
        {
            get
            {
                Target.Status = 3;
                return new int[]
                {
                4, 5
                }

                ;
            }

            set
            {
                Target.Status = 4;
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
            dynamic d = new Target();
            dynamic x = 2;
            // should call this[int]
            int[] v = d[x];
            if (Target.Status != 1 || v.Length != 3 || v[1] != 2)
                return 1;
            d[x] = v;
            if (Target.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparam016.oneparam016
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public int Foo;
    }

    public class Target
    {
        public static int Status;
        public MyClass this[dynamic x]
        {
            get
            {
                Target.Status = 1;
                return new MyClass()
                {
                    Foo = 2
                }

                ;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public MyClass this[int x]
        {
            get
            {
                Target.Status = 3;
                return new MyClass()
                {
                    Foo = 3
                }

                ;
            }

            set
            {
                if (value.Foo == 3)
                    Target.Status = 4;
                else
                    Target.Status = 9;
            }
        }

        public MyClass this[string x]
        {
            get
            {
                Target.Status = 5;
                return new MyClass()
                {
                    Foo = 4
                }

                ;
            }

            set
            {
                Target.Status = 6;
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
            dynamic d = new Target();
            dynamic x = 2;
            MyClass s = d[x];
            if (s.Foo != 3 || Target.Status != 3)
                return 1;
            d[x] = s;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv001.oneparamdifftypesconv001
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public int this[int x]
        {
            get
            {
                Target.Status = 1;
                return int.MinValue;
            }

            set
            {
                if (value == int.MinValue)
                    Target.Status = 2;
            }
        }

        public int this[long x]
        {
            get
            {
                Target.Status = 3;
                return int.MaxValue;
            }

            set
            {
                Target.Status = 4;
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
            dynamic d = new Target();
            short x = 3;
            int i = d[x];
            if (i != int.MinValue || Target.Status != 1)
                return 1;
            d[x] = i;
            if (Target.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv002.oneparamdifftypesconv002
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public float this[int x]
        {
            get
            {
                Target.Status = 1;
                return float.Epsilon;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public float this[uint x]
        {
            get
            {
                Target.Status = 3;
                return float.NegativeInfinity;
            }

            set
            {
                if (value == float.NegativeInfinity)
                    Target.Status = 4;
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
            dynamic d = new Target();
            uint x = 3;
            // should call this[uint]
            float f = d[x];
            if (f != float.NegativeInfinity || Target.Status != 3)
                return 1;
            d[x] = f;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv003.oneparamdifftypesconv003
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public double this[float x]
        {
            get
            {
                Target.Status = 1;
                return double.PositiveInfinity;
            }

            set
            {
                if (value == double.PositiveInfinity)
                    Target.Status = 2;
            }
        }

        public double this[float? x]
        {
            get
            {
                Target.Status = 3;
                return double.NegativeInfinity;
            }

            set
            {
                Target.Status = 4;
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
            dynamic d = new Target();
            float x = 3.1f;
            double dd = d[x];
            if (dd != double.PositiveInfinity || Target.Status != 1)
                return 1;
            d[x] = dd;
            if (Target.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv004.oneparamdifftypesconv004
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public float?[] this[float x]
        {
            get
            {
                Target.Status = 1;
                return new float?[]
                {
                1, 2, null
                }

                ;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public float?[] this[float? x]
        {
            get
            {
                Target.Status = 3;
                return new float?[]
                {
                null, float.Epsilon
                }

                ;
            }

            set
            {
                if (value.Length == 2 && value[0] == null && value[1] == float.Epsilon)
                    Target.Status = 4;
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
            dynamic d = new Target();
            float?[] f = d[null];
            if (f.Length != 2 || f[0] != null || Target.Status != 3)
                return 1;
            d[null] = f;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv005.oneparamdifftypesconv005
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public ulong this[int? x]
        {
            get
            {
                Target.Status = 1;
                return ulong.MaxValue;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public ulong this[int x]
        {
            get
            {
                Target.Status = 3;
                return ulong.MaxValue;
            }

            set
            {
                Target.Status = 4;
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
            dynamic d = new Target();
            short s = 3;
            ulong ul = d[s];
            if (ul != ulong.MaxValue || Target.Status != 3)
                return 1;
            d[s] = ul;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv006.oneparamdifftypesconv006
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public enum myEnum
    {
        First,
        Second,
        Third
    }

    public class Target
    {
        public static int Status;
        public bool this[int x]
        {
            get
            {
                Target.Status = 1;
                return false;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public bool this[myEnum x]
        {
            get
            {
                Target.Status = 3;
                return true;
            }

            set
            {
                if (value)
                    Target.Status = 4;
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
            dynamic d = new Target();
            myEnum e = myEnum.First;
            bool b = d[e];
            if (Target.Status != 3 || b == false)
                return 1;
            d[e] = b;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv007.oneparamdifftypesconv007
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public enum myEnum
    {
        First,
        Second,
        Third
    }

    public class Target
    {
        public static int Status;
        public char this[int x]
        {
            get
            {
                Target.Status = 1;
                return char.MinValue;
            }

            set
            {
                if (value == char.MinValue)
                    Target.Status = 2;
            }
        }

        public char this[myEnum x]
        {
            get
            {
                Target.Status = 3;
                return char.MaxValue;
            }

            set
            {
                Target.Status = 4;
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
            dynamic d = new Target();
            int x = 1;
            char c = d[x];
            if (Target.Status != 1 || c != char.MinValue)
                return 1;
            d[x] = c;
            if (Target.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv008.oneparamdifftypesconv008
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public enum myEnum
    {
        First,
        Second,
        Third
    }

    public class Target
    {
        public static int Status;
        public int this[int x]
        {
            get
            {
                Target.Status = 1;
                return 1;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public int this[myEnum x]
        {
            get
            {
                Target.Status = 3;
                return 2;
            }

            set
            {
                Target.Status = 4;
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
            dynamic d = new Target();
            int x = d[0];
            if (Target.Status != 1)
                return 1;
            d[0] = x;
            if (Target.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv009.oneparamdifftypesconv009
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public struct myStruct
    {
        public bool Ok
        {
            get;
            set;
        }
    }

    public class Target
    {
        public static int Status;
        public float? this[ValueType x]
        {
            get
            {
                Target.Status = 1;
                return 1f;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public float? this[myStruct x]
        {
            get
            {
                Target.Status = 3;
                return float.PositiveInfinity;
            }

            set
            {
                if (value == null)
                    Target.Status = 4;
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
            dynamic d = new Target();
            myStruct s = new myStruct()
            {
                Ok = false
            }

            ;
            float? nf = d[s];
            if (nf != float.PositiveInfinity || Target.Status != 3)
                return 1;
            d[s] = null;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv010.oneparamdifftypesconv010
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public struct myStruct
    {
        public bool Ok
        {
            get;
            set;
        }
    }

    public class Target
    {
        public static int Status;
        public decimal this[ValueType x]
        {
            get
            {
                Target.Status = 1;
                return decimal.One;
            }

            set
            {
                if (((myStruct)x).Ok == false)
                    Target.Status = 2;
            }
        }

        public decimal this[object x]
        {
            get
            {
                Target.Status = 3;
                return decimal.MinValue;
            }

            set
            {
                Target.Status = 3;
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
            dynamic d = new Target();
            myStruct s = new myStruct()
            {
                Ok = false
            }

            ;
            decimal dd = d[s];
            if (Target.Status != 1 || dd != decimal.One)
                return 1;
            d[s] = dd;
            if (Target.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv011.oneparamdifftypesconv011
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public struct myStruct
    {
        public bool Ok
        {
            get;
            set;
        }
    }

    public class Target
    {
        public static int Status;
        public string[] this[myStruct x]
        {
            get
            {
                Target.Status = 1;
                return new string[]
                {
                "foo"
                }

                ;
            }

            set
            {
                if (value[0] == "foo")
                    Target.Status = 2;
            }
        }

        public string[] this[myStruct? x]
        {
            get
            {
                Target.Status = 3;
                return new string[]
                {
                }

                ;
            }

            set
            {
                Target.Status = 4;
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
            dynamic d = new Target();
            myStruct s = new myStruct()
            {
                Ok = false
            }

            ;
            string[] ss = d[s];
            if (ss[0] != "foo" || Target.Status != 1)
                return 1;
            d[s] = ss;
            if (Target.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv012.oneparamdifftypesconv012
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public struct myStruct
    {
        public bool Ok
        {
            get;
            set;
        }
    }

    public enum MyEnum
    {
        First,
        Second
    }

    public class Target
    {
        public static int Status;
        public MyEnum? this[myStruct x]
        {
            get
            {
                Target.Status = 1;
                return MyEnum.Second;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public MyEnum? this[myStruct? x]
        {
            get
            {
                Target.Status = 3;
                return null;
            }

            set
            {
                if (value == MyEnum.First)
                    Target.Status = 4;
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
            dynamic d = new Target();
            MyEnum? me = d[null];
            if (me != null || Target.Status != 3)
                return 1;
            d[null] = MyEnum.First;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv013.oneparamdifftypesconv013
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Derived : Base
    {
    }

    public class Target
    {
        public static int Status;
        public byte? this[Base x]
        {
            get
            {
                Target.Status = 1;
                return null;
            }

            set
            {
                if (value == null)
                    Target.Status = 2;
            }
        }

        public byte? this[Derived x]
        {
            get
            {
                Target.Status = 3;
                return 12;
            }

            set
            {
                Target.Status = 4;
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
            dynamic d = new Target();
            Base b = new Base();
            byte? by = d[b];
            if (by != null || Target.Status != 1)
                return 1;
            d[b] = by;
            if (Target.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv014.oneparamdifftypesconv014
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Derived : Base
    {
    }

    public class MyClass
    {
        public int Foo;
    }

    public class Target
    {
        public static int Status;
        public MyClass this[Base x]
        {
            get
            {
                Target.Status = 1;
                return new MyClass()
                {
                    Foo = 4
                }

                ;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public MyClass this[Derived x]
        {
            get
            {
                Target.Status = 3;
                return new MyClass()
                {
                    Foo = int.MinValue
                }

                ;
            }

            set
            {
                if (value.Foo == int.MinValue)
                    Target.Status = 4;
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
            dynamic d = new Target();
            Derived b = new Derived();
            MyClass c = d[b];
            if (Target.Status != 3 || c.Foo != int.MinValue)
                return 1;
            d[b] = c;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv015.oneparamdifftypesconv015
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Derived : Base
    {
    }

    public class Target
    {
        public static int Status;
        public string this[Derived x]
        {
            get
            {
                Target.Status = 1;
                return string.Empty;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public string this[Base x]
        {
            get
            {
                Target.Status = 3;
                return "Foo";
            }

            set
            {
                if (value is string)
                    Target.Status = 4;
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
            dynamic d = new Target();
            Base b = new Derived();
            // should pick this[Base]
            string s = d[b];
            if (s != "Foo" || Target.Status != 3)
                return 1;
            d[b] = s;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv016.oneparamdifftypesconv016
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public interface I
    {
    }

    public class Base : I
    {
    }

    public class Target
    {
        public static int Status;
        public float this[I x]
        {
            get
            {
                Target.Status = 1;
                return float.Epsilon;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public float this[Base x]
        {
            get
            {
                Target.Status = 3;
                return float.PositiveInfinity;
            }

            set
            {
                if (value == float.PositiveInfinity)
                    Target.Status = 4;
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
            dynamic d = new Target();
            Base b = new Base();
            float f = d[b];
            if (Target.Status != 3 || f != float.PositiveInfinity)
                return 1;
            d[b] = f;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv017.oneparamdifftypesconv017
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public interface I
    {
    }

    public class Base : I
    {
    }

    public class Target
    {
        public static int Status;
        public char this[Base x]
        {
            get
            {
                Target.Status = 1;
                return 'a';
            }

            set
            {
                Target.Status = 2;
            }
        }

        public char this[I x]
        {
            get
            {
                Target.Status = 3;
                return 'b';
            }

            set
            {
                if (value == 'b')
                    Target.Status = 4;
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
            dynamic d = new Target();
            I b = new Base();
            // should pick this[I]
            char c = d[b];
            if (c != 'b' || Target.Status != 3)
                return 1;
            d[b] = c;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesconv018.oneparamdifftypesconv018
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public interface I
    {
    }

    public class Base : I
    {
    }

    public class Target
    {
        public static int Status;
        public int this[Base x]
        {
            get
            {
                Target.Status = 1;
                return 1;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public int this[I x]
        {
            get
            {
                Target.Status = 3;
                return 3;
            }

            set
            {
                if (value == 3)
                    Target.Status = 4;
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
            dynamic d = new Target();
            Base b = new Base();
            // should pick this[I]
            int x = d[(I)b];
            if (Target.Status != 3 || x != 3)
                return 1;
            d[(I)b] = x;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesnoconv001.oneparamdifftypesnoconv001
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public double this[bool x]
        {
            get
            {
                Target.Status = 1;
                return double.Epsilon;
            }

            set
            {
                if (value == double.Epsilon)
                    Target.Status = 2;
            }
        }

        public double this[string x]
        {
            get
            {
                Target.Status = 3;
                return double.MaxValue;
            }

            set
            {
                Target.Status = 4;
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
            dynamic d = new Target();
            double dd = d[true];
            if (dd != double.Epsilon || Target.Status != 1)
                return 1;
            d[true] = dd;
            if (Target.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesnoconv002.oneparamdifftypesnoconv002
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public bool this[bool x]
        {
            get
            {
                Target.Status = 1;
                return false;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public bool this[string x]
        {
            get
            {
                Target.Status = 3;
                return true;
            }

            set
            {
                if (value)
                    Target.Status = 4;
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
            dynamic d = new Target();
            bool b = d["Foo"];
            if (Target.Status != 3 || b != true)
                return 1;
            d["Foo"] = b;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesuserconv001.oneparamdifftypesuserconv001
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static implicit operator decimal (Base b)
        {
            return 1m;
        }
    }

    public class Target
    {
        public static int Status;
        public int this[decimal x]
        {
            get
            {
                Target.Status = 1;
                return int.MaxValue;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public int this[Base x]
        {
            get
            {
                Target.Status = 3;
                return int.MinValue;
            }

            set
            {
                if (value == int.MinValue)
                    Target.Status = 4;
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
            dynamic d = new Target();
            Base b = new Base();
            int x = d[b];
            if (x != int.MinValue || Target.Status != 3)
                return 1;
            d[b] = x;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesuserconv002.oneparamdifftypesuserconv002
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
        public static implicit operator int (Base b)
        {
            return 1;
        }

        public static implicit operator uint (Base b)
        {
            return 2;
        }
    }

    public class Target
    {
        public static int Status;
        public uint this[int x]
        {
            get
            {
                Target.Status = 1;
                return uint.MaxValue;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public uint this[uint x]
        {
            get
            {
                Target.Status = 3;
                return uint.MinValue;
            }

            set
            {
                if (value == uint.MinValue)
                    Target.Status = 4;
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
            dynamic d = new Target();
            Base b = new Base();
            uint ui = b;
            uint u = d[ui];
            if (u != uint.MinValue || Target.Status != 3)
                return 1;
            d[ui] = u;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.oneparamdifftypesuserconv003.oneparamdifftypesuserconv003
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Derived : Base
    {
        public static implicit operator decimal (Derived b)
        {
            return 1m;
        }
    }

    public class Target
    {
        public static int Status;
        public int this[decimal x]
        {
            get
            {
                Target.Status = 1;
                return int.MaxValue;
            }

            set
            {
                if (value == int.MaxValue)
                    Target.Status = 2;
            }
        }

        public int this[Base x]
        {
            get
            {
                Target.Status = 3;
                return int.MinValue;
            }

            set
            {
                if (value == int.MinValue)
                    Target.Status = 4;
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
            dynamic d = new Target();
            Base b = new Derived();
            // should cann this[Base]
            int x = d[b];
            if (x != int.MinValue || Target.Status != 3)
                return 1;
            d[b] = x;
            if (Target.Status != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.twoprms001.twoprms001
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // This will have an ambiguity at runtime
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public int this[int x, dynamic f]
        {
            get
            {
                Target.Status = 1;
                return int.MaxValue;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public int this[float f, int x]
        {
            get
            {
                Target.Status = 3;
                return int.MinValue;
            }

            set
            {
                if (value == int.MinValue)
                    Target.Status = 4;
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
            dynamic d = new Target();
            int x = 3;
            int hitValue = 0;
            int xx = 10;
            bool ret = true;
            try
            {
                xx = d[x, x];
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "Target.this[int, object]", "Target.this[float, int]");
                if (ret)
                    hitValue++;
            }

            try
            {
                d[x, x] = xx;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "Target.this[int, object]", "Target.this[float, int]");
                if (ret)
                    hitValue++;
            }

            if (hitValue != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.twoprms002.twoprms002
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public float this[int x, dynamic f]
        {
            get
            {
                Target.Status = 1;
                return float.Epsilon;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public float this[float f, int x]
        {
            get
            {
                Target.Status = 3;
                return float.MaxValue;
            }

            set
            {
                Target.Status = 4;
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
            dynamic d = new Target();
            int x = 3;
            object o = 5;
            float f = d[x, o];
            return (f == float.Epsilon) ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.twoprms003.twoprms003
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // This will have an ambiguity at runtime
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public int this[int x, dynamic f]
        {
            get
            {
                Target.Status = 1;
                return int.MinValue;
            }

            set
            {
                Target.Status = 2;
            }
        }

        public int this[float f, int x]
        {
            get
            {
                Target.Status = 3;
                return int.MaxValue;
            }

            set
            {
                if (value == int.MaxValue)
                    Target.Status = 4;
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
            dynamic d = new Target();
            int x = 3;
            dynamic dd = 5;
            int hitValue = 0;
            int xx = 10;
            bool ret = true;
            try
            {
                xx = d[x, dd];
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "Target.this[int, object]", "Target.this[float, int]");
                if (ret)
                    hitValue++;
            }

            try
            {
                d[x, dd] = xx;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "Target.this[int, object]", "Target.this[float, int]");
                if (ret)
                    hitValue++;
            }

            if (hitValue != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.twoprms005.twoprms005
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // This will have an ambiguity at runtime
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public string this[int x, dynamic f]
        {
            get
            {
                Target.Status = 1;
                return "foo";
            }

            set
            {
                Target.Status = 2;
            }
        }

        public string this[int f, dynamic[] x]
        {
            get
            {
                Target.Status = 3;
                return string.Empty;
            }

            set
            {
                Target.Status = 4;
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
            dynamic d = new Target();
            int x = 3;
            dynamic dd = 5;
            string ss = d[x, dd];
            if (ss != "foo" || Target.Status != 1)
                return 1;
            d[x, dd] = ss;
            if (Target.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Indexers.Oneclass2indexers.twoprms006.twoprms006
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // This will have an ambiguity at runtime
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public int this[int x, dynamic f]
        {
            get
            {
                Target.Status = 1;
                return int.MaxValue;
            }

            set
            {
                if (value == int.MaxValue)
                    Target.Status = 2;
            }
        }

        public int this[int f, params dynamic[] x]
        {
            get
            {
                Target.Status = 3;
                return int.MinValue;
            }

            set
            {
                Target.Status = 4;
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
            dynamic d = new Target();
            int x = 3;
            dynamic dd = 5;
            int foo = d[x, dd];
            if (foo != int.MaxValue || Target.Status != 1)
                return 1;
            d[x, dd] = foo;
            if (Target.Status != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}
