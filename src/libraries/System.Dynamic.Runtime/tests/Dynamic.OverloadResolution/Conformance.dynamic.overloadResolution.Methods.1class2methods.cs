// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.diffnumberprms001.diffnumberprms001
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
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(int x, int y)
        {
            Target.Status = 2;
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
            d.Method(x);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.diffnumberprms002.diffnumberprms002
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
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(int x, params int[] xx)
        {
            Target.Status = 2;
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
            d.Method(x);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.diffnumberprms003.diffnumberprms003
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
        public void Method(ref int x)
        {
            Target.Status = 1;
        }

        public void Method(ref int x, params int[] xx)
        {
            Target.Status = 2;
        }
    }

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Target();
            int x = 3;
            d.Method(ref x);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.moreprms001.moreprms001
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // This tests overload resolution for more than 2 params at a sanity level
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status;
        public void Method(int x, float f, string s, decimal d)
        {
            Target.Status = 1;
        }

        public void Method(int x, float f, decimal d, string s)
        {
            Target.Status = 2;
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
            d.Method(x, 3f, 54m, string.Empty);
            if (Target.Status == 2)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.moreprms002.moreprms002
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // This tests overload resolution for more than 2 params at a sanity level
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status = -1;
        public void Method(int x, float f, string s, decimal d, int xx, float ff, string ss, decimal dd, int xxx, float fff, string sss, decimal ddd)
        {
            Target.Status = 2;
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
            d.Method(x, 3f, string.Empty, 54m, x, 3f, string.Empty, 54m, x, 3f, string.Empty, 54m);
            if (Target.Status == 2)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.negtwoprms001.negtwoprms001
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
        public void Method(int x, float f)
        {
            Target.Status = 1;
        }

        public void Method(float f, int x)
        {
            Target.Status = 2;
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
            try
            {
                d.Method(x, x);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "Target.Method(int, float)", "Target.Method(float, int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.negtwoprms002.negtwoprms002
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // This will have an ambiguity at runtime
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Target
    {
        public static int Status;
        public void Method(int x, dynamic f)
        {
            Target.Status = 1;
        }

        public void Method(dynamic f, int x)
        {
            Target.Status = 2;
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
            dynamic x = 3;
            dynamic o = 5;
            try
            {
                d.Method(x, o);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "Target.Method(int, object)", "Target.Method(object, int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.onedynamicparam001.onedynamicparam001
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
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(string x)
        {
            Target.Status = 2;
        }

        public void Method(object x)
        {
            Target.Status = 3;
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
            dynamic t = new Target();
            dynamic d = 4;
            t.Method(d);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.onedynamicparam002.onedynamicparam002
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
        public int Method(int x)
        {
            Target.Status = 1;
            return 1;
        }

        public void Method(string x)
        {
            Target.Status = 2;
        }

        public void Method(object x)
        {
            Target.Status = 3;
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
            dynamic t = new Target();
            dynamic d = 4;
            Target.Status = (int)t.Method(d);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.onedynamicparam003.onedynamicparam003
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
        public myClass Method(int x)
        {
            Target.Status = 1;
            return new myClass();
        }

        public void Method(string x)
        {
            Target.Status = 2;
        }

        public void Method(object x)
        {
            Target.Status = 3;
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
            dynamic t = new Target();
            dynamic d = 4;
            Target.Status = (int)t.Method(d).Foo;
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.onedynamicparam004.onedynamicparam004
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
        public dynamic Method(int x)
        {
            Target.Status = 1;
            return 2;
        }

        public void Method(string x)
        {
            Target.Status = 2;
        }

        public void Method(object x)
        {
            Target.Status = 3;
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
            dynamic t = new Target();
            dynamic d = 4;
            if ((string)t.Method(d).ToString() == "2" && Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.onedynamicparam006.onedynamicparam006
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
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(string x)
        {
            Target.Status = 2;
        }

        public void Method(object x)
        {
            Target.Status = 3;
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
            dynamic t = new Target();
            dynamic d = 4;
            try
            {
                t.Method(d).Foo(); //We call a method on a void return type
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(RuntimeErrorId.BindToVoidMethodButExpectResult, ex.Message);
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam001.oneparam001
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
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(ref int x)
        {
            Target.Status = 2;
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
            d.Method(x);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam002.oneparam002
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
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(ref int x)
        {
            x = x + 1;
            Target.Status = 2;
        }
    }

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Target();
            int x = 3;
            d.Method(ref x);
            System.Console.WriteLine(x);
            if (Target.Status == 2 && x == 4)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam003.oneparam003
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
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(out int x)
        {
            x = 4;
            Target.Status = 2;
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
            d.Method(x);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam004.oneparam004
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
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(out int x)
        {
            x = 3;
            Target.Status = 2;
        }
    }

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Target();
            int x = 3;
            d.Method(out x);
            if (Target.Status == 2 && x == 3)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam005.oneparam005
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
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(params int[] x)
        {
            Target.Status = 2;
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
            d.Method(x);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam006.oneparam006
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
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(params int[] x)
        {
            Target.Status = 2;
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
            d.Method(x, x);
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam007.oneparam007
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
        public void Method(ref int x)
        {
            x = x + 1;
            Target.Status = 1;
        }

        public void Method(out short x)
        {
            x = 1;
            Target.Status = 2;
        }
    }

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Target();
            int x = 3;
            d.Method(ref x);
            if (Target.Status == 1 && x == 4)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam008.oneparam008
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
        public void Method(ref short x)
        {
            Target.Status = 1;
        }

        public void Method(out int x)
        {
            x = 3;
            Target.Status = 2;
        }
    }

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Target();
            int x;
            d.Method(out x);
            if (Target.Status == 2 && x == 3)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam009.oneparam009
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
        public void Method(ref int x)
        {
            x = x + 1;
            Target.Status = 1;
        }

        public void Method(params int[] x)
        {
            Target.Status = 2;
        }
    }

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Target();
            int x = 3;
            d.Method(ref x);
            if (Target.Status == 1 && x == 4)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam010.oneparam010
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
        public void Method(ref int x)
        {
            x = x + 1;
            Target.Status = 1;
        }

        public void Method(params int[] x)
        {
            Target.Status = 2;
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
            d.Method(x);
            if (Target.Status == 2 && x == 3)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam011.oneparam011
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
        public void Method(out int x)
        {
            x = 3;
            Target.Status = 1;
        }

        public void Method(params int[] x)
        {
            Target.Status = 2;
        }
    }

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Target();
            int x;
            d.Method(out x);
            if (Target.Status == 1 && x == 3)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam012.oneparam012
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
        public void Method(out int x)
        {
            x = 3;
            Target.Status = 1;
        }

        public void Method(params int[] x)
        {
            Target.Status = 2;
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
            d.Method(x);
            if (Target.Status == 2 && x == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam013.oneparam013
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
        public void Method(dynamic x)
        {
            Target.Status = 1;
        }

        public void Method(int x)
        {
            Target.Status = 2;
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
            d.Method(x);
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam014.oneparam014
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Target
    {
        public static int Status = -1;
        public void Method(dynamic x)
        {
            Target.Status = 2;
        }

        public void Method(int x)
        {
            Target.Status = 1;
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
            object x = 99;
            d.Method(x);
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam015.oneparam015
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
        public void Method(dynamic x)
        {
            Target.Status = 1;
        }

        public void Method(int x)
        {
            Target.Status = 2;
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
            short x = 2;
            d.Method(x);
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam016.oneparam016
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
        public void Method(dynamic x)
        {
            Target.Status = 1;
        }

        public void Method(int x)
        {
            Target.Status = 2;
        }

        public void Method(string x)
        {
            Target.Status = 3;
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
            d.Method(x);
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparam017.oneparam017
{
    // <Title> Tests overload resolution for 1 class and 2 methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class MemberClass
    {
        public void Method_ReturnsDynamic<U>()
        {
            Test.Status = 2;
        }

        public void Method_ReturnsDynamic<U, V>(V v)
        {
            Test.Status = 1;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClass();
            List<string> list = new List<string>()
            {
            "Test"
            }

            ;
            mc.Method_ReturnsDynamic<int, List<string>>(list);
            if (Test.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv001.oneparamdifftypesconv001
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
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(long x)
        {
            Target.Status = 2;
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
            d.Method(x);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv002.oneparamdifftypesconv002
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
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(long x)
        {
            Target.Status = 2;
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
            d.Method(x);
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv003.oneparamdifftypesconv003
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
        public void Method(float x)
        {
            Target.Status = 1;
        }

        public void Method(float? x)
        {
            Target.Status = 2;
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
            d.Method(x);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv004.oneparamdifftypesconv004
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
        public void Method(float x)
        {
            Target.Status = 1;
        }

        public void Method(float? x)
        {
            Target.Status = 2;
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
            d.Method(null);
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv005.oneparamdifftypesconv005
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
        public void Method(long x)
        {
            Target.Status = 1;
        }

        public void Method(uint? x)
        {
            Target.Status = 2;
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
            d.Method(s);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv006.oneparamdifftypesconv006
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
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(myEnum x)
        {
            Target.Status = 2;
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
            d.Method(e);
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv007.oneparamdifftypesconv007
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
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(myEnum x)
        {
            Target.Status = 2;
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
            d.Method(x);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv008.oneparamdifftypesconv008
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
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(myEnum x)
        {
            Target.Status = 2;
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
            d.Method(0);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv009.oneparamdifftypesconv009
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
        public void Method(ValueType x)
        {
            Target.Status = 1;
        }

        public void Method(myStruct x)
        {
            Target.Status = 2;
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
            d.Method(s);
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv010.oneparamdifftypesconv010
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
        public void Method(ValueType x)
        {
            Target.Status = 1;
        }

        public void Method(object x)
        {
            Target.Status = 2;
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
            d.Method(s);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv011.oneparamdifftypesconv011
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
        public void Method(myStruct x)
        {
            Target.Status = 1;
        }

        public void Method(myStruct? x)
        {
            Target.Status = 2;
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
            d.Method(s);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv012.oneparamdifftypesconv012
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
        public void Method(myStruct x)
        {
            Target.Status = 1;
        }

        public void Method(myStruct? x)
        {
            Target.Status = 2;
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
            d.Method(null);
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv013.oneparamdifftypesconv013
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
        public void Method(Base x)
        {
            Target.Status = 1;
        }

        public void Method(Derived x)
        {
            Target.Status = 2;
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
            d.Method(b);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv014.oneparamdifftypesconv014
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
        public void Method(Base x)
        {
            Target.Status = 1;
        }

        public void Method(Derived x)
        {
            Target.Status = 2;
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
            d.Method(b);
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv015.oneparamdifftypesconv015
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
        public static int Status = -1;
        public void Method(Base x)
        {
            Target.Status = 1;
        }

        public void Method(Derived x)
        {
            Target.Status = 2;
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
            d.Method(b);
            // call base
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv016.oneparamdifftypesconv016
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
        public void Method(I x)
        {
            Target.Status = 1;
        }

        public void Method(Base x)
        {
            Target.Status = 2;
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
            d.Method(b);
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv017.oneparamdifftypesconv017
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
        public static int Status = 1;
        public void Method(I x)
        {
            Target.Status = 1;
        }

        public void Method(Base x)
        {
            Target.Status = 2;
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
            d.Method(b);
            // case to Interface already
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesconv018.oneparamdifftypesconv018
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
        public static int Status = -1;
        public void Method(I x)
        {
            Target.Status = 1;
        }

        public void Method(Base x)
        {
            Target.Status = 2;
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
            d.Method((Base)b);
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesnoconv001.oneparamdifftypesnoconv001
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
        public void Method(bool x)
        {
            Target.Status = 1;
        }

        public void Method(string x)
        {
            Target.Status = 2;
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
            d.Method(true);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesnoconv002.oneparamdifftypesnoconv002
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
        public void Method(bool x)
        {
            Target.Status = 1;
        }

        public void Method(string x)
        {
            Target.Status = 2;
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
            d.Method("Foo");
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesuserconv001.oneparamdifftypesuserconv001
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
        public void Method(decimal x)
        {
            Target.Status = 1;
        }

        public void Method(Base x)
        {
            Target.Status = 2;
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
            d.Method(b);
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesuserconv002.oneparamdifftypesuserconv002
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

        public static implicit operator long (Base b)
        {
            return 2;
        }
    }

    public class Target
    {
        public static int Status;
        public void Method(int x)
        {
            Target.Status = 1;
        }

        public void Method(long x)
        {
            Target.Status = 2;
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
            d.Method(b);
            if (Target.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.oneparamdifftypesuserconv003.oneparamdifftypesuserconv003
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
        public static int Status = -1;
        public void Method(decimal x)
        {
            Target.Status = 1;
        }

        public void Method(Base x)
        {
            Target.Status = 2;
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
            d.Method(b);
            if (Target.Status == 2)
                return 0;
            else
                return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype001.statictype001
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
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

    public class Test
    {
        public static int Status;
        public void Method(Base b)
        {
            Test.Status = 1;
        }

        public void Method(Derived b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Base b = new Derived();
            dynamic d = new Test();
            d.Method(b);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype002.statictype002
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
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

    public class Test
    {
        public static int Status;
        public void Method(Base b)
        {
            Test.Status = 1;
        }

        public void Method(Derived b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived b = new Derived();
            dynamic d = new Test();
            d.Method(b);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype003.statictype003
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Test
    {
        public static int Status;
        public void Method(string b)
        {
            Test.Status = 1;
        }

        public void Method(Base b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            string s = null;
            dynamic d = new Test();
            d.Method(s);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype004.statictype004
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Test
    {
        public static int Status;
        public void Method(string b)
        {
            Test.Status = 1;
        }

        public void Method(Base b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Base s = null;
            dynamic d = new Test();
            d.Method(s);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype005.statictype005
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
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

    public class Test
    {
        public static int Status;
        public void Method(Derived b)
        {
            Test.Status = 1;
        }

        public void Method(Base b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Derived s = null;
            dynamic d = new Test();
            d.Method(s);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype006.statictype006
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
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

    public class Test
    {
        public static int Status;
        public void Method(Derived b)
        {
            Test.Status = 1;
        }

        public void Method(Base b)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Base s = null;
            dynamic d = new Test();
            d.Method(s);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype007.statictype007
{
    public class Test
    {
        public static int Status;
        public void Method(string s)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            string s = null;
            dynamic d = new Test();
            d.Method(s);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype008.statictype008
{
    public class Test
    {
        public static int Status;
        public void Method(short? s)
        {
            Test.Status = 1;
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49568", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            short? s = 2;
            d.Method(s);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype009.statictype009
{
    public class Test
    {
        public static int Status;
        public void Method(short? s)
        {
            Test.Status = 1;
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49568", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            short? s = null;
            d.Method(s);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype010.statictype010
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters (with nullable types)
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public struct S
    {
        public int Field;
    }

    public class Test
    {
        public static int Status;
        public void Method(S? s)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            S? s = new S()
            {
                Field = 3
            }

            ;
            d.Method(s);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype011.statictype011
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters (with nullable types)
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public struct S
    {
        public int Field;
    }

    public class Test
    {
        public static int Status;
        public void Method(S? s)
        {
            Test.Status = 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            S? s = null;
            d.Method(s);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype012.statictype012
{
    public class Test
    {
        public static int Status;
        public void Method(int x)
        {
            Test.Status = 1;
        }

        public void Method(float f)
        {
            Test.Status = 2;
        }

        public void Method(char c)
        {
            Test.Status = 3;
        }

        public void Method(decimal c)
        {
            Test.Status = 4;
        }

        public void Method(System.Guid c)
        {
            Test.Status = 5;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int x = 2;
            float f = float.Epsilon;
            char c = 'a';
            decimal m = 1M;
            System.Guid g = System.Guid.NewGuid();
            dynamic d = new Test();
            d.Method(x);
            if (Test.Status != 1)
                return 1;
            d.Method(f);
            if (Test.Status != 2)
                return 1;
            d.Method(c);
            if (Test.Status != 3)
                return 1;
            d.Method(m);
            if (Test.Status != 4)
                return 1;
            d.Method(g);
            if (Test.Status != 5)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype013.statictype013
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Test
    {
        public static int Status;
        public void Method(int x, string s)
        {
            Test.Status = 1;
        }

        public void Method(int x, Base s)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = 3;
            string s = null;
            dynamic d = new Test();
            d.Method(x, s);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype014.statictype014
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Test
    {
        public static int Status;
        public void Method(int x, string s)
        {
            Test.Status = 1;
        }

        public void Method(int x, Base s)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = 3;
            Base s = null;
            dynamic d = new Test();
            d.Method(x, s);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype015.statictype015
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
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

    public class Test
    {
        public static int Status;
        public void Method(int x, Derived s)
        {
            Test.Status = 1;
        }

        public void Method(int x, Base s)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = 3;
            Derived s = null;
            dynamic d = new Test();
            d.Method(x, s);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype016.statictype016
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
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

    public class Test
    {
        public static int Status;
        public void Method(int x, Derived s)
        {
            Test.Status = 1;
        }

        public void Method(int x, Base s)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = 3;
            Base s = null;
            dynamic d = new Test();
            d.Method(x, s);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype017.statictype017
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Test
    {
        public static int Status;
        public void Method(int x, short? s)
        {
            Test.Status = 1;
        }

        public void Method(int x, Base s)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = 3;
            short? s = null;
            dynamic d = new Test();
            d.Method(x, s);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype018.statictype018
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Test
    {
        public static int Status;
        public void Method(int x, short? s)
        {
            Test.Status = 1;
        }

        public void Method(int x, Base s)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = 3;
            Base s = null;
            dynamic d = new Test();
            d.Method(x, s);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype019.statictype019
{
    public class Test
    {
        public static int Status;
        public void Method(int x, int s)
        {
            Test.Status = 1;
        }

        public void Method(int x, long s)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = 3;
            int s = 3;
            dynamic d = new Test();
            d.Method(x, s);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype020.statictype020
{
    public class Test
    {
        public static int Status;
        public void Method(int x, int s)
        {
            Test.Status = 1;
        }

        public void Method(int x, long s)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = 3;
            long s = 5l;
            dynamic d = new Test();
            d.Method(x, s);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype021.statictype021
{
    public class Test
    {
        public static int Status;
        public static void Method(int x, int s)
        {
            Test.Status = 1;
        }

        public static void Method(int x, long s)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = 3;
            int s = 3;
            Test.Method(x, s);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype022.statictype022
{
    public class Test
    {
        public static int Status;
        public static void Method(int x, int s)
        {
            Test.Status = 1;
        }

        public static void Method(int x, long s)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = 3;
            long s = 5l;
            Test.Method(x, s);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype023.statictype023
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Test
    {
        public static int Status;
        public static void Method(int x, string s)
        {
            Test.Status = 1;
        }

        public static void Method(int x, Base s)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = 3;
            string s = null;
            Test.Method(x, s);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype024.statictype024
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Test
    {
        public static int Status;
        public static void Method(int x, string s)
        {
            Test.Status = 1;
        }

        public static void Method(int x, Base s)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = 3;
            Base s = null;
            Test.Method(x, s);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype025.statictype025
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Test
    {
        public static int Status;
        public static void Method(int x, short? s)
        {
            Test.Status = 1;
        }

        public static void Method(int x, Base s)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = 3;
            short? s = null;
            Test.Method(x, s);
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.statictype026.statictype026
{
    // <Title> Tests overload resolution when the static type of the variable is passed to the binder</Title>
    // <Description>
    // Calls a method dynamically but uses non-dynamic parameters
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Base
    {
    }

    public class Test
    {
        public static int Status;
        public static void Method(int x, short? s)
        {
            Test.Status = 1;
        }

        public static void Method(int x, Base s)
        {
            Test.Status = 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = 3;
            Base s = null;
            Test.Method(x, s);
            if (Test.Status == 2)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.twoprms001.twoprms001
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
        public void Method(int x, dynamic f)
        {
            Target.Status = 1;
        }

        public void Method(float f, int x)
        {
            Target.Status = 2;
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
            try
            {
                d.Method(x, x);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "Target.Method(int, object)", "Target.Method(float, int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.twoprms002.twoprms002
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
        public void Method(int x, dynamic f)
        {
            Target.Status = 1;
        }

        public void Method(float f, int x)
        {
            Target.Status = 2;
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
            dynamic x = 3;
            dynamic o = 5;
            try
            {
                d.Method(x, o);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "Target.Method(int, object)", "Target.Method(float, int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.twoprms003.twoprms003
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
        public void Method(int x, dynamic f)
        {
            Target.Status = 1;
        }

        public void Method(float f, int x)
        {
            Target.Status = 2;
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
            try
            {
                d.Method(x, dd);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.AmbigCall, ex.Message, "Target.Method(int, object)", "Target.Method(float, int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.twoprms004.twoprms004
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
        public void Method(int x, dynamic f)
        {
            Target.Status = 1;
        }

        public void Method(int f, ref object x)
        {
            Target.Status = 2;
        }
    }

    public class Test
    {
        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/14906", TestRuntimes.Mono)]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Target();
            int x = 3;
            dynamic dd = 5;
            d.Method(x, dd);
            if (Target.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.twoprms005.twoprms005
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
        public void Method(int x, dynamic f)
        {
            Target.Status = 1;
        }

        public void Method(int f, dynamic[] x)
        {
            Target.Status = 2;
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
            d.Method(x, dd);
            if (Target.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.twoprms006.twoprms006
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
        public void Method(int x, dynamic f)
        {
            Target.Status = 1;
        }

        public void Method(int f, params dynamic[] x)
        {
            Target.Status = 2;
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
            d.Method(x, dd);
            if (Target.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.generic001.generic001
{
    // <Title> Tests overload resolution - Generic</Title>
    // <Description>Program compiles successfully and runs without exceptions.
    //              Runtime binder should use the same tie-breaking rules.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var a = new A();
            Func<int, Func<int, int>> f = x => y => x;
            a.Bar(f);
            dynamic d = a;
            d.Bar(f);
            return 0;
        }

        public void Bar<T, S>(Func<T, Func<S, int>> x)
        {
        }

        public void Bar<T, S>(Func<T, Func<S, S>> x)
        {
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.generic002.generic002
{
    public class Test
    {
        public class A<T>
        {
            public int Foo(T x)
            {
                return 1;
            }

            public int Foo(int x)
            {
                return 2;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            // used to be AmbiguousMatchException
            var v = new A<int>();
            int r1 = v.Foo(1);
            dynamic d = v;
            int r2 = d.Foo(1);
            return (r1 == r2) ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.literals01.literals01
{
    // <Area>Overload resolution</Area>
    // <Expects status=success></Expects>
    // <Code>

    public class A
    {
        private const byte b = 100;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var res = 0;
            dynamic a = new A();
            res += a.Foo(0 + 1);
            res += a.Foo(-1);
            res += a.Foo(+1);
            res += a.Foo(1 + 1);
            res += a.Foo(1 - 1);
            res += a.Foo(1 * 1);
            res += a.Foo(1 / 1);
            res += a.Foo(true & false);
            res += a.Foo(1 > 2);
            res += a.Foo(1 < 2);
            res += a.Foo(1 <= 2);
            res += a.Foo(1 >= 2);
            res += a.Foo(true && false);
            res += a.Foo(true || false);
            res += a.Foo(b);
            res += a.Foo(b + 2);
            res += a.Foo(b - 2);
            res += a.Foo(+b);
            res += a.Foo(-b);
            res += a.Foo(b > 2);
            res += a.Foo(b < 2);
            res += a.Foo(b > 2);
            res += a.Foo(-b < 2);
            res += a.Foo(x: 0 + 1);
            res += a.Foo(x: -1);
            res += a.Foo(x: +1);
            res += a.Foo(x: 1 + 1);
            res += a.Foo(x: 1 - 1);
            res += a.Foo(x: 1 * 1);
            res += a.Foo(x: 1 / 1);
            res += a.Foo(x: true & false);
            res += a.Foo(x: 1 > 2);
            res += a.Foo(x: 1 < 2);
            res += a.Foo(x: 1 <= 2);
            res += a.Foo(x: 1 >= 2);
            res += a.Foo(x: true && false);
            res += a.Foo(x: true || false);
            res += a.Foo(x: b);
            res += a.Foo(x: b + 2);
            res += a.Foo(x: b - 2);
            res += a.Foo(x: +b);
            res += a.Foo(x: -b);
            res += a.Foo(x: b > 2);
            res += a.Foo(x: b < 2);
            res += a.Foo(x: b > 2);
            res += a.Foo(x: -b < 2);
            return res;
        }

        public int Foo(bool x)
        {
            return 0;
        }

        public int Foo(byte x)
        {
            return 0;
        }

        public int Foo(sbyte x)
        {
            return 0;
        }

        public int Foo(object x)
        {
            return 1;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.bestmeth01.bestmeth01
{
    // <Area>Overload resolution</Area>
    // <Expects status=success></Expects>
    // <Code>
    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = 0L;
            return C.M<Program>(d);
        }
    }

    public class C
    {
        public static int M<T>(int i) where T : C
        {
            return 1;
        }

        public static int M<T>(long l)
        {
            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.cnstraintegereger01.cnstraintegereger01
{
    // <Area>Overload resolution</Area>
    // <Expects status=success></Expects>
    // <Code>
    public class C<T>
        where T : class
    {
    }

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic a = new A();
            return a.Foo(1, null);
        }

        public int Foo(object x, object y)
        {
            return 0;
        }

        public int Foo<T>(T x, C<T> y) where T : class
        {
            return 1;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.cnstraintegereger02.cnstraintegereger02
{
    // <Area>Overload resolution</Area>
    // <Expects status=success></Expects>
    // <Code>
    public interface I
    {
    }

    public class C<T>
        where T : I
    {
    }

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic a = new A();
            return a.Foo(1, null);
        }

        public int Foo(object x, object y)
        {
            return 0;
        }

        public int Foo<T>(T x, C<T> y) where T : I
        {
            return 1;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.cnstraintegereger03.cnstraintegereger03
{
    // <Area>Overload resolution</Area>
    // <Expects status=success></Expects>
    // <Code>
    public class C<T>
        where T : class
    {
    }

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic a = new A();
            return a.Foo(1, null);
        }

        public int Foo(object x, object y)
        {
            return 0;
        }

        public int Foo<T>(T x, T y) where T : class
        {
            return 1;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.cnstraintegereger04.cnstraintegereger04
{
    // <Area>Overload resolution</Area>
    // <Expects status=success></Expects>
    // <Code>
    public class C<T>
        where T : class
    {
        public static implicit operator C<T>(int i)
        {
            return null;
        }
    }

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic a = new A();
            return a.Foo(null, new C<A>());
        }

        public int Foo(object x, object y)
        {
            return 1;
        }

        public int Foo<T>(T x, C<T> y) where T : class
        {
            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.operate001.operate001
{
    // <Title> Tests overload resolution - Generic</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class A
    {
        public static explicit operator string (A x)
        {
            return "A";
        }

        public static string op_Implicit<T>(A x)
        {
            return "B";
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic y = new A();
            string x = (string)y;
            return ("A" == x) ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.arglistprms002.arglistprms002
{
    // <Title>Arglist</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = 1;

            for (int i = 0; i < 4; i++)
            {
                if (((object)d).ToString() != "1")
                {
                    return 1;
                }
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.Methods.Oneclass2methods.regression01.regression01
{
    // <Title>Regression test</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class A
    {
        public static int Foo(object x)
        {
            return 1;
        }

        public static int Foo(params object[] x)
        {
            return 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (Bar1(new string[0]) == 1 && Bar2(new string[0]) == 2)
                return 0;
            return 1;
        }

        private static int Bar1<T>(T[] x)
        {
            return Foo(x); // Prints 1
        }

        private static int Bar2<T>(T[] x)
        {
            return Foo((dynamic)x); // Prints 2
        }
    }
    // </Code>
}
