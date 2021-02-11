// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.integeregereractionDynamicObject.integeregereraction001.integeregereraction001
{
    public class Test
    {
        public class Base
        {
            public virtual int Foo(object o)
            {
                return 1;
            }
        }

        public class Derived : Base
        {
            public override int Foo(dynamic o)
            {
                return 2;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object o = 3;
            Base der = new Derived();
            int rez = der.Foo(o);
            if (rez != 2)
                return 1;
            dynamic d = 3;
            rez = der.Foo(d);
            if (rez != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.integeregereractionDynamicObject.integeregereraction002.integeregereraction002
{
    public class Test
    {
        public class Base
        {
            public virtual int Foo(dynamic o)
            {
                return 1;
            }
        }

        public class Derived : Base
        {
            public override int Foo(object o)
            {
                return 2;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object o = 3;
            Base der = new Derived();
            int rez = der.Foo(o);
            if (rez != 2)
                return 1;
            dynamic d = 3;
            rez = der.Foo(d);
            if (rez != 2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.integeregereractionDynamicObject.integeregereraction003.integeregereraction003
{
    public class Test
    {
        public interface I
        {
            int Foo(object o);
        }

        public class C : I
        {
            public int Foo(dynamic o)
            {
                bool ret = true;
                try
                {
                    o.Bar(); //This should fail at runtime
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
                {
                    ret = ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "object", "Bar");
                    if (ret)
                        return 0; //it means we did the right thing
                }

                return 1;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            I cl = new C();
            dynamic d = new object();
            return cl.Foo(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.overloadResolution.integeregereractionDynamicObject.integeregereraction006.integeregereraction006
{
    public class Test
    {
        public interface I
        {
            int Foo(object o);
        }

        public class C : I
        {
            int I.Foo(dynamic o)
            {
                bool ret = true;
                try
                {
                    o.Bar(); //This should fail at runtime
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
                {
                    ret = ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "object", "Bar");
                    if (ret)
                        return 0; //it means we did the right thing
                }

                return 1;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            I cl = new C();
            dynamic d = new object();
            return cl.Foo(d);
        }
    }
    // </Code>
}
