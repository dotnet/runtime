// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclassautoprop.regclassautoprop;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclassautoprop.regclassautoprop
{
    public class MyClass
    {
        public int Field = 0;
    }

    public struct MyStruct
    {
        public int Number;
    }

    public enum MyEnum
    {
        First = 1,
        Second = 2,
        Third = 3
    }

    public class MemberClass
    {
        public string Property_string
        {
            set;
            get;
        }

        public MyClass Property_MyClass
        {
            get;
            set;
        }

        public MyStruct Property_MyStruct
        {
            set;
            get;
        }

        public MyEnum Property_MyEnum
        {
            set;
            private get;
        }

        public short Property_short
        {
            set;
            protected get;
        }

        public ulong Property_ulong
        {
            set;
            protected internal get;
        }

        public char Property_char
        {
            private set;
            get;
        }

        public bool Property_bool
        {
            protected set;
            get;
        }

        public decimal Property_decimal
        {
            protected internal set;
            get;
        }

        public MyStruct? Property_MyStructNull
        {
            set;
            get;
        }

        public MyEnum? Property_MyEnumNull
        {
            set;
            private get;
        }

        public short? Property_shortNull
        {
            set;
            get;
        }

        public ulong? Property_ulongNull
        {
            set;
            protected internal get;
        }

        public char? Property_charNull
        {
            private set;
            get;
        }

        public bool? Property_boolNull
        {
            protected set;
            get;
        }

        public decimal? Property_decimalNull
        {
            protected internal set;
            get;
        }

        public string[] Property_stringArr
        {
            set;
            get;
        }

        public MyClass[] Property_MyClassArr
        {
            set;
            get;
        }

        public MyStruct[] Property_MyStructArr
        {
            get;
            set;
        }

        public MyEnum[] Property_MyEnumArr
        {
            set;
            private get;
        }

        public short[] Property_shortArr
        {
            set;
            protected get;
        }

        public ulong[] Property_ulongArr
        {
            set;
            protected internal get;
        }

        public char[] Property_charArr
        {
            private set;
            get;
        }

        public bool[] Property_boolArr
        {
            protected set;
            get;
        }

        public decimal[] Property_decimalArr
        {
            protected internal set;
            get;
        }

        public MyStruct?[] Property_MyStructNullArr
        {
            set;
            get;
        }

        public MyEnum?[] Property_MyEnumNullArr
        {
            set;
            private get;
        }

        public short?[] Property_shortNullArr
        {
            set;
            protected get;
        }

        public ulong?[] Property_ulongNullArr
        {
            set;
            protected internal get;
        }

        public char?[] Property_charNullArr
        {
            private set;
            get;
        }

        public bool?[] Property_boolNullArr
        {
            protected set;
            get;
        }

        public decimal?[] Property_decimalNullArr
        {
            protected internal set;
            get;
        }

        public float Property_Float
        {
            get;
            set;
        }

        public float?[] Property_FloatNullArr
        {
            get;
            set;
        }

        public dynamic Property_Dynamic
        {
            get;
            set;
        }

        public static string Property_stringStatic
        {
            set;
            get;
        }
        // Move declarations to the call site
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass001.regclass001
{
    // <Title> Tests regular class auto property used in generic method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t1 = new Test();
            return t1.TestGetMethod<long>(1, new MemberClass()) + t1.TestSetMethod<Test, string>(string.Empty, new MemberClass()) == 0 ? 0 : 1;
        }

        public int TestGetMethod<T>(T t, MemberClass mc)
        {
            mc.Property_string = "Test";
            dynamic dy = mc;
            if ((string)dy.Property_string != "Test")
                return 1;
            else
                return 0;
        }

        public int TestSetMethod<U, V>(V v, MemberClass mc)
        {
            dynamic dy = mc;
            dy.Property_string = "Test";
            mc = dy; //because we might change the property on a boxed version of it if MemberClass is a struct
            if (mc.Property_string != "Test")
                return 1;
            else
                return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass003.regclass003
{
    // <Title> Tests regular class auto property used in variable initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            string[] bas = new string[]
            {
            "Test", string.Empty, null
            }

            ;
            MemberClass mc = new MemberClass();
            mc.Property_stringArr = bas;
            dynamic dy = mc;
            string[] loc = dy.Property_stringArr;
            if (ReferenceEquals(bas, loc) && loc[0] == "Test" && loc[1] == string.Empty && loc[2] == null)
            {
                return 0;
            }
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass004.regclass004
{
    // <Title> Tests regular class auto property used in implicitly-typed array initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass mc1 = new MemberClass();
            MemberClass mc2 = new MemberClass();
            mc1.Property_MyStructNull = null;
            mc2.Property_MyStructNull = new MyStruct()
            {
                Number = 1
            }

            ;
            dynamic dy1 = mc1;
            dynamic dy2 = mc2;
            var loc = new MyStruct?[]
            {
            (MyStruct? )dy1.Property_MyStructNull, (MyStruct? )dy2.Property_MyStructNull
            }

            ;
            if (loc.Length == 2 && loc[0] == null && loc[1].Value.Number == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass005.regclass005
{
    // <Title> Tests regular class auto property used in operator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass mc1 = new MemberClass();
            MemberClass mc2 = new MemberClass();
            mc1.Property_string = "a";
            mc2.Property_string = "b";
            dynamic dy1 = mc1;
            dynamic dy2 = mc2;
            string s = (string)dy1.Property_string + (string)dy2.Property_string;
            if (s == "ab")
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass006.regclass006
{
    // <Title> Tests regular class auto property used in null coalescing operator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass mc = new MemberClass();
            mc.Property_MyStructArr = new MyStruct[]
            {
            new MyStruct()
            {
            Number = 0
            }

            , new MyStruct()
            {
            Number = 1
            }
            }

            ;
            dynamic dy = mc;
            string s1 = ((string)dy.Property_string) ?? string.Empty;
            mc.Property_string = "Test";
            dy = mc;
            MyStruct[] b1 = ((MyStruct[])dy.Property_MyStructArr) ?? (new MyStruct[1]);
            MyStruct[] b2 = ((MyStruct[])dy.Property_MyStructArr) ?? (new MyStruct[1]);
            string s2 = ((string)dy.Property_string) ?? string.Empty;
            if (b1.Length == 2 && s1 == string.Empty && b2.Length == 2 && s2 == "Test")
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass007.regclass007
{
    // <Title> Tests regular class auto property used in destructor.</Title>
    // <Description>
    // On IA64 the GC.WaitForPendingFinalizers() does not actually work...
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Runtime.CompilerServices;

    public class Test
    {
        private static string s_field;
        public static object locker = new object();
        ~Test()
        {
            lock (locker)
            {
                MemberClass mc = new MemberClass();
                mc.Property_string = "Test";
                dynamic dy = mc;
                s_field = dy.Property_string;
            }
        }

        private static int Verify()
        {
            lock (Test.locker)
            {
                if (Test.s_field != "Test")
                {
                    return 1;
                }
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RequireLifetimesEnded()
        {
            Test t = new Test();
            Test.s_field = "Field";
            GC.KeepAlive(t);
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            RequireLifetimesEnded();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            // If move the code in Verify() to here, the finalizer will only be executed after exited Main
            return Verify();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass008.regclass008
{
    // <Title> Tests regular class auto property used in extension method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int a1 = 10;
            MyStruct ms1 = a1.TestSetMyStruct();
            MyStruct ms2 = a1.TestGetMyStruct();
            if (ms1.Number == 10 && ms2.Number == 10)
                return 0;
            return 1;
        }
    }

    public static class Extension
    {
        public static MyStruct TestSetMyStruct(this int i)
        {
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            dy.Property_MyStruct = new MyStruct()
            {
                Number = i
            }

            ;
            mc = dy; //because MC might be a struct
            return mc.Property_MyStruct;
        }

        public static MyStruct TestGetMyStruct(this int i)
        {
            MemberClass mc = new MemberClass();
            mc.Property_MyStruct = new MyStruct()
            {
                Number = i
            }

            ;
            dynamic dy = mc;
            return (MyStruct)dy.Property_MyStruct;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass009.regclass009
{
    // <Title> Tests regular class auto property used in variable initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            char value = (char)dy.Property_char;
            if (value == default(char))
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass010.regclass010
{
    // <Title> Tests regular class auto property used in array initializer list.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            bool[] array = new bool[]
            {
            (bool)dy.Property_bool, true
            }

            ;
            if (array.Length == 2 && array[0] == false && array[1] == true)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass014.regclass014
{
    // <Title> Tests regular class auto property used in for loop body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClass();
            ulong[] array = new ulong[]
            {
            1L, 2L, 3L, ulong.MinValue, ulong.MaxValue
            }

            ;
            for (int i = 0; i < array.Length; i++)
            {
                mc.Property_ulong = array[i];
            }

            ulong x = (ulong)mc.Property_ulong;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass015.regclass015
{
    // <Title> Tests regular class auto property used in foreach expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass mc = new MemberClass();
            mc.Property_MyClassArr = new MyClass[]
            {
            null, new MyClass()
            {
            Field = -1
            }
            }

            ;
            dynamic dy = mc;
            List<MyClass> list = new List<MyClass>();
            foreach (MyClass myclass in dy.Property_MyClassArr)
            {
                list.Add(myclass);
            }

            if (list.Count == 2 && list[0] == null && list[1].Field == -1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass016.regclass016
{
    // <Title> Tests regular class auto property used in while body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test : MemberClass
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test mc = new Test();
            dynamic dy = mc;
            short a = 0;
            short v = 0;
            while (a < 10)
            {
                v = a;
                dy.Property_shortNull = a;
                a = (short)((short)dy.Property_shortNull + 1);
                if (a != v + 1)
                    return 1;
            }

            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass018.regclass018
{
    // <Title> Tests regular class auto property used in uncheck expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            ulong result = 1;
            dy.Property_ulongNull = ulong.MaxValue;
            result = unchecked(dy.Property_ulongNull + 1); //0
            return (int)result;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass019.regclass019
{
    // <Title> Tests regular class auto property used in static constructor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static char? s_charValue = 'a';
        static Test()
        {
            dynamic dy = new MemberClass();
            s_charValue = dy.Property_charNull;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (Test.s_charValue == null)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass020.regclass020
{
    // <Title> Tests regular class auto property used in variable named dynamic.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dynamic = new MemberClass();
            if (dynamic.Property_boolNull == null)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass022.regclass022
{
    // <Title> Tests regular class auto property used in field initailizer outside of constructor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_dy = new MemberClass();
        private char[] _result = s_dy.Property_charArr;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            if (t._result == null)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass023.regclass023
{
    // <Title> Tests regular class auto property used in static generic method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            return TestMethod<Test>();
        }

        private static int TestMethod<T>()
        {
            dynamic dy = new MemberClass();
            dy.Property_MyEnumArr = new MyEnum[0];
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass024.regclass024
{
    // <Title> Tests regular class auto property used in static generic method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            return TestMethod<int>();
        }

        private static int TestMethod<T>()
        {
            dynamic dy = new MemberClass();
            dy.Property_shortArr = new short[2];
            try
            {
                short[] result = dy.Property_shortArr; // protected
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.InaccessibleGetter, e.Message, "MemberClass.Property_shortArr"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass025.regclass025
{
    // <Title> Tests regular class auto property used in inside#if, #else block.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            ulong[] array = null;
            dy.Property_ulongArr = new ulong[]
            {
            0, 1
            }

            ;
#if MS
array = new ulong[] { (ulong)dy.Property_ulong };

 #else
            try
            {
                array = dy.Property_ulongArr;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.InaccessibleGetter, e.Message, "MemberClass.Property_ulongArr"))
                    return 0;
                else
                {
                    System.Console.WriteLine(e);
                    return 1;
                }
            }

#endif

            // different case actually
            if (array.Length == 2 && array[0] == 0 && array[1] == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass026.regclass026
{
    // <Title> Tests regular class auto property used in regular method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (new Test().TestMethod())
                return 0;
            return 1;
        }

        private bool TestMethod()
        {
            dynamic dy = new MemberClass();
            bool[] result = dy.Property_boolArr;
            return result == null;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass027.regclass027
{
    // <Title> Tests regular class auto property used in using block.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.IO;

    public class Test
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass mc = new MemberClass();
            mc.Property_decimalArr = new decimal[]
            {
            1M, 1.1M
            }

            ;
            dynamic dy = mc;
            using (MemoryStream ms = new MemoryStream())
            {
                if (((decimal[])dy.Property_decimalArr)[0] != 1M && ((decimal[])dy.Property_decimalArr)[1] != 1.1M)
                    return 1;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                dy.Property_decimalArr = new decimal[]
                {
                10M
                }

                ;
                ((decimal[])dy.Property_decimalArr)[0] = 10.01M;
            }

            if (mc.Property_decimalArr.Length == 1 && mc.Property_decimalArr[0] == 10.01M)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass028.regclass028
{
    // <Title> Tests regular class auto property used in ternary operator expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            return t.TestGet() + t.TestSet();
        }

        public int TestGet()
        {
            MemberClass mc = new MemberClass();
            mc.Property_MyStructNullArr = new MyStruct?[]
            {
            null, new MyStruct()
            {
            Number = 10
            }
            }

            ;
            dynamic dy = mc;
            return (int)dy.Property_MyStructNullArr.Length == 2 ? 0 : 1;
        }

        public int TestSet()
        {
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            dy.Property_MyStructNullArr = new MyStruct?[]
            {
            null, new MyStruct()
            {
            Number = 10
            }
            }

            ;
            mc = dy;
            return (int)dy.Property_MyStructNullArr.Length == 2 ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass029.regclass029
{
    // <Title> Tests regular class auto property used in null coalescing operator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            try
            {
                MyEnum?[] result = dy.Property_MyEnumNullArr ?? new MyEnum?[1]; //private, should have exception
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.InaccessibleGetter, e.Message, "MemberClass.Property_MyEnumNullArr"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass030.regclass030
{
    // <Title> Tests regular class auto property used in constructor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static int Return;
        public Test()
        {
            dynamic dy = new MemberClass();
            try
            {
                // public for struct
                short?[] result = dy.Property_shortNullArr; //protected, should have exception
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.InaccessibleGetter, e.Message, "MemberClass.Property_shortNullArr"))
                    Test.Return = 0;
                else
                    Test.Return = 1;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            return Test.Return;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass031.regclass031
{
    // <Title> Tests regular class auto property used in null coalescing operator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            ulong?[] result1 = dy.Property_ulongNullArr ?? new ulong?[1];
            if (result1.Length != 1 || dy.Property_ulongNullArr != null)
                return 1;
            dy.Property_ulongNullArr = dy.Property_ulongNullArr ?? new ulong?[0];
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass032.regclass032
{
    // <Title> Tests regular class auto property used in static variable.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_dy = new MemberClass();
        private static char?[] s_result = s_dy.Property_charNullArr;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (s_result == null)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass034.regclass034
{
    // <Title> Tests regular class auto property used in switch section statement.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            int result = int.MaxValue;
            try
            {
                dy.Property_decimalNullArr = new decimal?[]
                {
                int.MinValue
                }

                ;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, e.Message, "MemberClass.Property_decimalNullArr"))
                    result = int.MaxValue;
            }

            switch (result)
            {
                case int.MaxValue:
                    try
                    {
                        result = (int)((decimal?[])dy.Property_decimalNullArr)[0];
                    }
                    catch (System.NullReferenceException)
                    {
                        result = int.MinValue;
                    }

                    break;
                default:
                    break;
            }

            if (result == int.MinValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass035.regclass035
{
    // <Title> Tests regular class auto property used in switch default section statement.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            int result = 4;
            dy.Property_Float = 4;
            switch (result)
            {
                case 4:
                    dy.Property_Float = float.NaN;
                    break;
                default:
                    result = (int)dy.Property_Float;
                    break;
            }

            if (result == 4)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass036.regclass036
{
    // <Title> Tests regular class auto property used in foreach body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            dy.Property_FloatNullArr = new float?[]
            {
            float.Epsilon, float.MaxValue, float.MinValue, float.NaN, float.NegativeInfinity, float.PositiveInfinity
            }

            ;
            if (dy.Property_FloatNullArr.Length == 6 && dy.Property_FloatNullArr[0] == float.Epsilon && dy.Property_FloatNullArr[1] == float.MaxValue && dy.Property_FloatNullArr[2] == float.MinValue && float.IsNaN((float)dy.Property_FloatNullArr[3]) && float.IsNegativeInfinity((float)dy.Property_FloatNullArr[4]) && float.IsPositiveInfinity((float)dy.Property_FloatNullArr[5]))
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.regclass.regclass037.regclass037
{
    // <Title> Tests regular class auto property used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass.Property_stringStatic = "Test";
            dynamic dynamic = MemberClass.Property_stringStatic;
            if ((string)dynamic == "Test")
                return 0;
            return 1;
        }
    }
    //</Code>
}
