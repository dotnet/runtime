// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.checked001.checked001
{
    // <Title>Tests checked block</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myClass
    {
        public int GetMaxInt()
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
            dynamic d = new myClass();
            int x = 0;
            try
            {
                checked
                {
                    x = (int)d.GetMaxInt() + 1;
                }
            }
            catch (System.OverflowException)
            {
                return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.checked002.checked002
{
    // <Title>Tests checked block</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myClass
    {
        public int GetMaxInt
        {
            get
            {
                return int.MaxValue;
            }

            set
            {
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
            dynamic d = new myClass();
            int x = 0;
            try
            {
                checked
                {
                    x = (int)d.GetMaxInt++;
                }
            }
            catch (System.OverflowException)
            {
                if (x == 0)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.checked003.checked003
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = int.MaxValue;
            byte x = 0;
            try
            {
                checked
                {
                    //this will not throw, because we don't pass the fact that we are in a checked context
                    x = (byte)d;
                }
            }
            catch (System.OverflowException)
            {
                if (x == 0)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.checked004.checked004
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = int.MaxValue;
            int x = 0;
            try
            {
                checked
                {
                    //This will not work by our current design because we will introduce an implicit conversion to byte
                    x = d + 1;
                }
            }
            catch (System.OverflowException)
            {
                if (x == 0)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.checked005.checked005
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int rez = 0, tests = 0;
            bool exception = false; //signals if the exception is thrown
            dynamic d = null;
            dynamic d2 = null;
            // ++ on byte in checked context
            tests++;
            exception = false;
            d = byte.MaxValue;
            try
            {
                checked
                {
                    d++;
                }
            }
            catch (System.OverflowException)
            {
                exception = true;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // -- on char in checked context
            tests++;
            exception = false;
            d = char.MinValue;
            try
            {
                char rchar = checked(d--);
            }
            catch (System.OverflowException)
            {
                exception = true;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // - on ushort in checked context
            tests++;
            exception = false;
            d = ushort.MaxValue;
            try
            {
                checked
                {
                    ushort rez2 = (ushort)-d;
                }
            }
            catch (System.OverflowException)
            {
                exception = true;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // + on sbyte and int
            tests++;
            exception = false;
            d = sbyte.MaxValue;
            d2 = int.MaxValue;
            try
            {
                checked
                {
                    int rez2 = d + d2;
                }
            }
            catch (System.OverflowException)
            {
                exception = true;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // - on uint and ushort
            tests++;
            exception = false;
            d = uint.MaxValue;
            d2 = ushort.MinValue;
            try
            {
                checked
                {
                    uint rez3 = d2 - d;
                }
            }
            catch (System.OverflowException)
            {
                exception = true;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // -= on ushort and ushort     
            tests++;
            exception = false;
            d = ushort.MaxValue;
            d2 = ushort.MinValue;
            try
            {
                checked
                {
                    d2 -= d;
                    exception = true;
                }
            }
            catch (System.OverflowException)
            {
                exception = false;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // * on char and long
            tests++;
            exception = false;
            d = char.MaxValue;
            d2 = long.MinValue;
            try
            {
                checked
                {
                    long rez3 = d2 * d;
                }
            }
            catch (System.OverflowException)
            {
                exception = true;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // / on char and long
            tests++;
            exception = false;
            d = int.MinValue;
            d2 = -1;
            try
            {
                long rez4 = checked(d / d2);
            }
            catch (System.OverflowException)
            {
                exception = true;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // explicit numeric conversion from int to byte
            tests++;
            exception = false;
            d = int.MaxValue;
            try
            {
                checked
                {
                    byte b = (byte)d;
                }
            }
            catch (System.OverflowException)
            {
                exception = true;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // explicit numeric conversion from float to char
            tests++;
            exception = false;
            d = float.MaxValue;
            try
            {
                checked
                {
                    char b = (char)d;
                }
            }
            catch (System.OverflowException)
            {
                exception = true;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // explicit numeric conversion from float to char
            tests++;
            exception = false;
            d = double.MaxValue;
            try
            {
                checked
                {
                    ushort b = (ushort)d;
                }
            }
            catch (System.OverflowException)
            {
                exception = true;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            return rez == tests ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.checked006.checked006
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int rez = 0, tests = 0;
            bool exception = true; //signals if the exception is thrown
            dynamic d = null;
            dynamic d2 = null;
            // ++ on byte in unchecked context
            tests++;
            exception = true;
            d = byte.MaxValue;
            try
            {
                unchecked
                {
                    d++;
                }
            }
            catch (System.OverflowException)
            {
                exception = false;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // -- on char in unchecked context
            tests++;
            exception = true;
            d = char.MinValue;
            try
            {
                char rchar = unchecked(d--);
            }
            catch (System.OverflowException)
            {
                exception = false;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // - on ushort in unchecked context
            tests++;
            exception = true;
            d = ushort.MaxValue;
            try
            {
                unchecked
                {
                    ushort rez2 = (ushort)-d;
                }
            }
            catch (System.OverflowException)
            {
                exception = false;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // + on sbyte and int
            tests++;
            exception = true;
            d = sbyte.MaxValue;
            d2 = int.MaxValue;
            try
            {
                unchecked
                {
                    int rez2 = d + d2;
                }
            }
            catch (System.OverflowException)
            {
                exception = false;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // - on uint and ushort
            tests++;
            exception = true;
            d = uint.MaxValue;
            d2 = ushort.MinValue;
            try
            {
                unchecked
                {
                    uint rez3 = d2 - d;
                }
            }
            catch (System.OverflowException)
            {
                exception = false;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // -= on uint and ushort
            tests++;
            exception = true;
            d = ushort.MaxValue;
            d2 = ushort.MinValue;
            try
            {
                unchecked
                {
                    d2 -= d;
                }
            }
            catch (System.OverflowException)
            {
                exception = false;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // * on char and long
            tests++;
            exception = true;
            d = char.MaxValue;
            d2 = long.MinValue;
            try
            {
                unchecked
                {
                    long rez3 = d2 * d;
                }
            }
            catch (System.OverflowException)
            {
                exception = false;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // / on char and long -> it is implemented such that this will throw even in an unchecked context.
            tests++;
            exception = false;
            d = int.MinValue;
            d2 = -1;
            try
            {
                long rez4 = unchecked(d / d2);
            }
            catch (System.OverflowException)
            {
                exception = true;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // explicit numeric conversion from int to byte
            tests++;
            exception = true;
            d = int.MaxValue;
            try
            {
                unchecked
                {
                    byte b = (byte)d;
                }
            }
            catch (System.OverflowException)
            {
                exception = false;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // explicit numeric conversion from float to char
            tests++;
            exception = true;
            d = float.MaxValue;
            try
            {
                unchecked
                {
                    char b = (char)d;
                }
            }
            catch (System.OverflowException)
            {
                exception = false;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            // explicit numeric conversion from float to char
            tests++;
            exception = true;
            d = double.MaxValue;
            try
            {
                unchecked
                {
                    ushort b = (ushort)d;
                }
            }
            catch (System.OverflowException)
            {
                exception = false;
            }
            finally
            {
                if (exception)
                    rez++;
                else
                    System.Console.WriteLine("Test {0} failed", tests);
            }

            return rez == tests ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.checked008.checked008
{
    // <Title>Tests checked block</Title>
    // <Description> Compiler not passing checked flag in complex operators
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=warning>\(16,17\).*CS0649</Expects>
    //<Expects Status=warning>\(17,11\).*CS0414</Expects>
    //<Expects Status=warning>\(18,20\).*CS0414</Expects>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public byte X;
        private sbyte _X1 = sbyte.MinValue;
        private ushort _Y = ushort.MinValue;
        protected short Y1 = short.MaxValue;
        internal uint Z = 0;
        protected internal ulong Q = ulong.MaxValue;
        protected long C = long.MinValue;

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int ret = 0;
            dynamic a = new Test();
            try
            {
                var v0 = checked(a.X1 -= 1);
                ret++;
            }
            catch (System.OverflowException)
            {
                System.Console.WriteLine("byte");
                // expected
            }

            try
            {
                var v1 = checked(a.X1 -= 1);
                ret++;
            }
            catch (System.OverflowException)
            {
                System.Console.WriteLine("sbyte");
                // expected
            }

            try
            {
                var v2 = checked(a.Y -= 1);
                ret++;
            }
            catch (System.OverflowException)
            {
                System.Console.WriteLine("ushort");
                // expected
            }

            try
            {
                var v2 = checked(a.Y1 += 1);
                ret++;
            }
            catch (System.OverflowException)
            {
                System.Console.WriteLine("short");
                // expected
            }

            try
            {
                var v3 = checked(a.Z -= 1);
                ret++;
            }
            catch (System.OverflowException)
            {
                System.Console.WriteLine("uint");
                // expected
            }

            try
            {
                var v4 = checked(a.Q += 1);
                ret++;
            }
            catch (System.OverflowException)
            {
                System.Console.WriteLine("ulong max");
                // expected
            }

            try
            {
                var v5 = checked(a.C -= 1);
                ret++;
            }
            catch (System.OverflowException)
            {
                System.Console.WriteLine("long min");
                // expected
            }

            System.Console.WriteLine(ret);
            return ret == 0 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.do001.do001
{
    // <Title>Tests do statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool value = true;
        public static explicit operator bool (myIf f)
        {
            return f.value;
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
            dynamic d = new myIf();
            int x = 0;
            do
            {
                x++;
                if (x > 2)
                    return 0;
            }
            while ((bool)d);
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.do002.do002
{
    // <Title>Tests do statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public static bool operator true(myIf f)
        {
            return true;
        }

        public static bool operator false(myIf f)
        {
            return false;
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
            dynamic d = new myIf();
            int x = 0;
            do
            {
                x++;
                if (x > 2)
                    return 0;
            }
            while (d);
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.do003.do003
{
    // <Title>Tests do statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool MyMethod()
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
            dynamic d = new myIf();
            int x = 0;
            do
            {
                x++;
                if (x > 2)
                    return 0;
            }
            while (d.MyMethod());
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.if001.if001
{
    // <Title>Tests if statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool value = true;
        public static explicit operator bool (myIf f)
        {
            return f.value;
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
            dynamic d = new myIf();
            if ((bool)d)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.if002.if002
{
    // <Title>Tests if statements</Title>
    // <Description>
    // Remove the comments when the exception is known
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool value = true;
        public static implicit operator bool (myIf f)
        {
            return f.value;
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
            dynamic d = new myIf();
            if (d)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.if003.if003
{
    // <Title>Tests if statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public static bool operator true(myIf f)
        {
            return true;
        }

        public static bool operator false(myIf f)
        {
            return false;
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
            dynamic d = new myIf();
            if (d)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.if004.if004
{
    // <Title>Tests if statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool value = true;
        public static explicit operator bool (myIf f)
        {
            return f.value;
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
            dynamic d = new myIf();
            if ((bool)d)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.if005.if005
{
    // <Title>Tests if statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool value = true;
        public static explicit operator bool (myIf f)
        {
            return f.value;
        }

        public static bool operator true(myIf f)
        {
            return false;
        }

        public static bool operator false(myIf f)
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
            dynamic d = new myIf();
            if ((bool)d)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.if006.if006
{
    // <Title>Tests if statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool value = true;
        public bool MyMethod()
        {
            return this.value;
        }

        public static explicit operator bool (myIf f)
        {
            return f.value;
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
            dynamic d = new myIf();
            if (d.MyMethod())
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.if007.if007
{
    // <Title>Tests if statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool value = true;
        public object MyMethod()
        {
            return this.value;
        }

        public static explicit operator bool (myIf f)
        {
            return f.value;
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
            dynamic d = new myIf();
            if (d.MyMethod())
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.if008.if008
{
    // <Title>Tests if statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool value = true;
        public object MyMethod()
        {
            Test.Status = 1;
            return !this.value;
        }

        public static explicit operator bool (myIf f)
        {
            return f.value;
        }
    }

    public class Test
    {
        public static int Status = 0;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new myIf();
            if ((bool)d || (bool)d.MyMethod())
            {
                if (Test.Status == 0) //We should have short-circuited the second call
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.if009.if009
{
    // <Title>Tests if statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool value = true;
        public object MyMethod()
        {
            Test.Status = 1;
            return !this.value;
        }

        public static explicit operator bool (myIf f)
        {
            return f.value;
        }
    }

    public class Test
    {
        public static int Status = 0;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new myIf();
            if ((bool)d | (bool)d.MyMethod())
            {
                if (Test.Status == 1) //We should have short-circuited the second call
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.if010.if010
{
    // <Title>Tests if statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool value = true;
        public static explicit operator bool (myIf f)
        {
            return f.value;
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
            dynamic d = new myIf();
            bool boo = true;
            if ((bool)d && (bool)boo)
            {
                return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.switch001.switch001
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = "foo";
            switch ((string)d)
            {
                case "foo":
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.switch002.switch002
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = 1;
            switch ((int?)d)
            {
                case 1:
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.ternary001.ternary001
{
    // <Title>Tests ternary operator statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool value = true;
        public static explicit operator bool (myIf f)
        {
            return f.value;
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
            dynamic d = new myIf();
            return (bool)d ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.ternary002.ternary002
{
    // <Title>Tests if statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public static bool operator true(myIf f)
        {
            return true;
        }

        public static bool operator false(myIf f)
        {
            return false;
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
            dynamic d = new myIf();
            return d ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.ternary003.ternary003
{
    // <Title>Tests if statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool value = true;
        public static implicit operator bool (myIf f)
        {
            return f.value;
        }

        public static bool operator true(myIf f)
        {
            return false;
        }

        public static bool operator false(myIf f)
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
            dynamic d = new myIf();
            return d ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.ternary004.ternary004
{
    // <Title>Tests if statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool value = true;
        public bool MyMethod()
        {
            return this.value;
        }

        public static explicit operator bool (myIf f)
        {
            return f.value;
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
            dynamic d = new myIf();
            return d.MyMethod() ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.ternary005.ternary005
{
    // <Title>Tests if statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool value = true;
        public static explicit operator bool (myIf f)
        {
            return f.value;
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
            dynamic d = new myIf();
            bool boo = true;
            return ((bool)d && (bool)boo) ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.unchecked001.unchecked001
{
    // <Title>Tests checked block</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myClass
    {
        public int GetMaxInt()
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
            dynamic d = new myClass();
            int x = 0;
            try
            {
                unchecked
                {
                    x = (int)d.GetMaxInt() + 1;
                }
            }
            catch (System.OverflowException)
            {
                return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.unchecked002.unchecked002
{
    // <Title>Tests checked block</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myClass
    {
        public int GetMaxInt()
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
            dynamic d = new myClass();
            int x = 0;
            try
            {
                unchecked
                {
                    x = (int)d.GetMaxInt() * 3;
                }
            }
            catch (System.OverflowException)
            {
                return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.unchecked003.unchecked003
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            unchecked
            {
                dynamic c_byte = (byte?)(-123);
                var rez = c_byte == (byte?)-123;
                return rez ? 0 : 1;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.unsfe001.unsfe001
{
    // <Title>If def</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public class Foo
    {
        public void Set()
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
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            {
                dynamic d = new Foo();
                d.Set();
            }

            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.while001.while001
{
    // <Title>Tests do statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool value = true;
        public static implicit operator bool (myIf f)
        {
            return f.value;
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
            dynamic d = new myIf();
            int x = 0;
            while (d)
            {
                x++;
                if (x > 2)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.while002.while002
{
    // <Title>Tests do statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public static bool operator true(myIf f)
        {
            return true;
        }

        public static bool operator false(myIf f)
        {
            return false;
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
            dynamic d = new myIf();
            int x = 0;
            while (d)
            {
                x++;
                if (x > 2)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.while003.while003
{
    // <Title>Tests do statements</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class myIf
    {
        public bool MyMethod()
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
            dynamic d = new myIf();
            int x = 0;
            while (d.MyMethod())
            {
                x++;
                if (x > 2)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.yield001.yield001
{
    // <Title>Tests checked block</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class MyClass
    {
        public IEnumerable<int> Foo()
        {
            dynamic d = 1;
            yield return d;
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
            MyClass d = new MyClass();
            foreach (var item in d.Foo())
            {
                if (item != 1)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.yield002.yield002
{
    // <Title>Tests checked block</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class MyClass
    {
        public IEnumerable<dynamic> Foo()
        {
            dynamic d = 1;
            yield return d;
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
            MyClass d = new MyClass();
            foreach (var item in d.Foo())
            {
                if ((int)item != 1)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.yield003.yield003
{
    // <Title>Tests checked block</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class MyClass
    {
        public IEnumerable<dynamic> Foo()
        {
            object d = 1;
            yield return d;
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
            MyClass d = new MyClass();
            foreach (var item in d.Foo())
            {
                if ((int)item != 1)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.statements.yield004.yield004
{
    // <Title>Tests checked block</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class MyClass
    {
        public IEnumerable<object> Foo()
        {
            dynamic d = 1;
            yield return d;
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
            MyClass d = new MyClass();
            foreach (var item in d.Foo())
            {
                if ((int)item != 1)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}
