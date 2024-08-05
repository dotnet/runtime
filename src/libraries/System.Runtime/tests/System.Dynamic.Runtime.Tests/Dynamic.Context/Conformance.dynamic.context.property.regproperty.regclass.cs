// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclassregprop.regclassregprop;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclassregprop.regclassregprop
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
        /*
        Example of calling it:
        MemberClass staticMC =new MemberClass();
        dynamic mc = staticMC;
        bool myBool;

        //This test the getter for the property
        myBool = true;
        staticMC.myBool = myBool; //We set the inner field
        myBool = mc.Property_bool; //We use the property to get the field
        if (myBool != true)
        return 1;

        //This tests the setter for the property
        myBool = true;
        mc.Property_bool = myBool; //We set the property
        myBool = statMc.myBool; // We get the inner field
        if (myBool != true)
        return 1;

        */
        public bool myBool = true;
        public bool? myBoolNull = true;
        public bool?[] myBoolNullArr = new bool?[2];
        public bool[] myBoolArr = new bool[2];
        public char myChar = 'a';
        public char? myCharNull = 'a';
        public char?[] myCharNullArr = new char?[2];
        public char[] myCharArr = new char[2];
        public decimal myDecimal = 1m;
        public decimal? myDecimalNull = 1m;
        public decimal?[] myDecimalNullArr = new decimal?[2];
        public decimal[] myDecimalArr = new decimal[2];
        public dynamic myDynamic = new object();
        public float myFloat = 3f;
        public float?[] myFloatNullArr = new float?[]
        {
        }

        ;
        public MyClass myClass = new MyClass()
        {
            Field = 2
        }

        ;
        public MyClass[] myClassArr = new MyClass[3];
        public MyEnum myEnum = MyEnum.First;
        public MyEnum? myEnumNull = MyEnum.First;
        public MyEnum?[] myEnumNullArr = new MyEnum?[3];
        public MyEnum[] myEnumArr = new MyEnum[3];
        public MyStruct myStruct = new MyStruct()
        {
            Number = 3
        }

        ;
        public MyStruct? myStructNull = new MyStruct()
        {
            Number = 3
        }

        ;
        public MyStruct?[] myStructNullArr = new MyStruct?[3];
        public MyStruct[] myStructArr = new MyStruct[3];
        public short myShort = 1;
        public short? myShortNull = 1;
        public short?[] myShortNullArr = new short?[2];
        public short[] myShortArr = new short[2];
        public string myString = string.Empty;
        public string[] myStringArr = new string[2];
        public ulong myUlong = 1;
        public ulong? myUlongNull = 1;
        public ulong?[] myUlongNullArr = new ulong?[2];
        public ulong[] myUlongArr = new ulong[2];
        public bool Property_bool
        {
            protected set
            {
                myBool = value;
            }

            get
            {
                return false;
            }
        }

        public bool? Property_boolNull
        {
            protected set
            {
                myBoolNull = value;
            }

            get
            {
                return null;
            }
        }

        public bool?[] Property_boolNullArr
        {
            protected set
            {
                myBoolNullArr = value;
            }

            get
            {
                return new bool?[]
                {
                true, null, false
                }

                ;
            }
        }

        public bool[] Property_boolArr
        {
            protected set
            {
                myBoolArr = value;
            }

            get
            {
                return new bool[]
                {
                true, false
                }

                ;
            }
        }

        public char Property_char
        {
            private set
            {
                myChar = value;
            }

            get
            {
                return myChar;
            }
        }

        public char? Property_charNull
        {
            private set
            {
                myCharNull = value;
            }

            get
            {
                return myCharNull;
            }
        }

        public char?[] Property_charNullArr
        {
            private set
            {
                myCharNullArr = value;
            }

            get
            {
                return myCharNullArr;
            }
        }

        public char[] Property_charArr
        {
            private set
            {
                myCharArr = value;
            }

            get
            {
                return myCharArr;
            }
        }

        public decimal Property_decimal
        {
            internal set
            {
                myDecimal = value;
            }

            get
            {
                return myDecimal;
            }
        }

        public decimal? Property_decimalNull
        {
            internal set
            {
                myDecimalNull = value;
            }

            get
            {
                return myDecimalNull;
            }
        }

        public decimal?[] Property_decimalNullArr
        {
            protected internal set
            {
                myDecimalNullArr = value;
            }

            get
            {
                return myDecimalNullArr;
            }
        }

        public decimal[] Property_decimalArr
        {
            protected internal set
            {
                myDecimalArr = value;
            }

            get
            {
                return myDecimalArr;
            }
        }

        public dynamic Property_dynamic
        {
            get
            {
                return myDynamic;
            }

            set
            {
                myDynamic = value;
            }
        }

        public float Property_Float
        {
            get
            {
                return myFloat;
            }

            set
            {
                myFloat = value;
            }
        }

        public float?[] Property_FloatNullArr
        {
            get
            {
                return myFloatNullArr;
            }

            set
            {
                myFloatNullArr = value;
            }
        }

        public MyClass Property_MyClass
        {
            set
            {
                myClass = value;
            }
        }

        public MyClass[] Property_MyClassArr
        {
            set
            {
                myClassArr = value;
            }
        }

        public MyEnum Property_MyEnum
        {
            set
            {
                myEnum = value;
            }

            get
            {
                return myEnum;
            }
        }

        public MyEnum? Property_MyEnumNull
        {
            set
            {
                myEnumNull = value;
            }

            private get
            {
                return myEnumNull;
            }
        }

        public MyEnum?[] Property_MyEnumNullArr
        {
            set
            {
                myEnumNullArr = value;
            }

            private get
            {
                return myEnumNullArr;
            }
        }

        public MyEnum[] Property_MyEnumArr
        {
            set
            {
                myEnumArr = value;
            }

            private get
            {
                return myEnumArr;
            }
        }

        public MyStruct Property_MyStruct
        {
            get
            {
                return myStruct;
            }

            set
            {
                myStruct = value;
            }
        }

        public MyStruct? Property_MyStructNull
        {
            get
            {
                return myStructNull;
            }
        }

        public MyStruct?[] Property_MyStructNullArr
        {
            get
            {
                return myStructNullArr;
            }
        }

        public MyStruct[] Property_MyStructArr
        {
            get
            {
                return myStructArr;
            }
        }

        public short Property_short
        {
            set
            {
                myShort = value;
            }

            protected get
            {
                return myShort;
            }
        }

        public short? Property_shortNull
        {
            set
            {
                myShortNull = value;
            }

            protected get
            {
                return myShortNull;
            }
        }

        public short?[] Property_shortNullArr
        {
            set
            {
                myShortNullArr = value;
            }

            protected get
            {
                return myShortNullArr;
            }
        }

        public short[] Property_shortArr
        {
            set
            {
                myShortArr = value;
            }

            protected get
            {
                return myShortArr;
            }
        }

        public string Property_string
        {
            set
            {
                myString = value;
            }

            get
            {
                return myString;
            }
        }

        public string[] Property_stringArr
        {
            set
            {
                myStringArr = value;
            }

            get
            {
                return myStringArr;
            }
        }

        public ulong Property_ulong
        {
            set
            {
                myUlong = value;
            }

            protected internal get
            {
                return myUlong;
            }
        }

        public ulong? Property_ulongNull
        {
            set
            {
                myUlongNull = value;
            }

            protected internal get
            {
                return myUlongNull;
            }
        }

        public ulong?[] Property_ulongNullArr
        {
            set
            {
                myUlongNullArr = value;
            }

            protected internal get
            {
                return myUlongNullArr;
            }
        }

        public ulong[] Property_ulongArr
        {
            set
            {
                myUlongArr = value;
            }

            protected internal get
            {
                return myUlongArr;
            }
        }

        public static bool myBoolStatic;
        public static MyClass myClassStatic = new MyClass();
        public static bool Property_boolStatic
        {
            protected set
            {
                myBoolStatic = value;
            }

            get
            {
                return myBoolStatic;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass001.regclass001
{
    // <Title> Tests regular class regular property used in static method body.</Title>
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
            return Test.TestGetMethod(new MemberClass()) + Test.TestSetMethod(new MemberClass()) == 0 ? 0 : 1;
        }

        public static int TestGetMethod(MemberClass mc)
        {
            dynamic dy = mc;
            dy.myBool = true;
            if (dy.Property_bool) //always return false
                return 1;
            else
                return 0;
        }

        public static int TestSetMethod(MemberClass mc)
        {
            dynamic dy = mc;
            try
            {
                dy.Property_bool = true;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, e.Message, "MemberClass.Property_bool", "set"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass002.regclass002
{
    // <Title> Tests regular class regular property used in arguments of method invocation.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private delegate int TestDec(char[] c);
        private static char[] s_charArray = new char[]
        {
        '0', 'a'
        };

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            TestDec td = t.TestMethod;
            MemberClass mc = new MemberClass();
            mc.myCharArr = s_charArray;
            dynamic dy = mc;
            return td((char[])dy.Property_charArr);
        }

        public int TestMethod(char[] c)
        {
            if (ReferenceEquals(c, s_charArray) && c[0] == '0' && c[1] == 'a')
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass003.regclass003
{
    // <Title> Tests regular class regular property used in property-set body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_mc = new MemberClass();

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Dec = new short?[]
            {
            null, 0, -1
            }

            ;
            if (s_mc.myShortNullArr[0] == null && s_mc.myShortNullArr[1] == 0 && s_mc.myShortNullArr[2] == -1)
                return 0;
            return 1;
        }

        public static short?[] Dec
        {
            set
            {
                s_mc.Property_shortNullArr = value;
            }

            get
            {
                return null;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass004.regclass004
{
    // <Title> Tests regular class regular property used in short-circuit boolean expression and ternary operator expression.</Title>
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
            int loopCount = 0;
            dy.Property_decimal = 0M;
            while (dy.Property_decimal < 10)
            {
                System.Console.WriteLine((object)dy.Property_decimal);
                dy.Property_decimal++;
                loopCount++;
            }

            return (dy.Property_decimal == 10 && loopCount == 10) ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass005.regclass005
{
    // <Title> Tests regular class regular property used in property set.</Title>
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
            try
            {
                t.TestProperty = null; //protected, should have exception
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, e.Message, "MemberClass.Property_boolNull"))
                    return 0;
            }

            return 1;
        }

        public bool? TestProperty
        {
            set
            {
                dynamic dy = new MemberClass();
                dy.Property_boolNull = value;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass006.regclass006
{
    // <Title> Tests regular class regular property used in property get body.</Title>
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
            bool?[] array = t.TestProperty;
            if (array.Length == 3 && array[0] == true && array[1] == null && array[2] == false)
                return 0;
            return 1;
        }

        public bool?[] TestProperty
        {
            get
            {
                dynamic dy = new MemberClass();
                return dy.Property_boolNullArr;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass007.regclass007
{
    // <Title> Tests regular class regular property used in static method body.</Title>
    // <Description>
    // Derived class call protected parent property.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test : MemberClass
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test mc = new Test();
            dynamic dy = mc;
            dy.Property_boolArr = new bool[3];
            bool[] result = dy.Property_boolArr;
            if (result.Length != 2 || result[0] != true || result[1] != false)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass008.regclass008
{
    // <Title> Tests regular class regular property used in indexer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        private Dictionary<string, int> _dic = new Dictionary<string, int>();
        private MemberClass _mc = new MemberClass();

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (TestSet() == 0 && TestGet() == 0)
                return 0;
            else
                return 1;
        }

        private static int TestSet()
        {
            Test t = new Test();
            t["a"] = 10;
            t[string.Empty] = -1;
            if (t._dic["a"] == 10 && t._dic[string.Empty] == -1 && (string)t._mc.Property_string == string.Empty)
                return 0;
            else
                return 1;
        }

        private static int TestGet()
        {
            Test t = new Test();
            t._dic["Test0"] = 2;
            if (t["Test0"] == 2)
                return 0;
            else
                return 1;
        }

        public int this[string i]
        {
            set
            {
                dynamic dy = _mc;
                dy.Property_string = i;
                _dic.Add((string)dy.Property_string, value);
                _mc = dy; //this is to circumvent the boxing of the struct
            }

            get
            {
                _mc.Property_string = i;
                dynamic dy = _mc;
                return _dic[(string)dy.Property_string];
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass010.regclass010
{
    // <Title> Tests regular class regular property used in try/catch/finally.</Title>
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
            dy.myChar = 'a';
            try
            {
                dy.Property_char = 'x'; //private, should have exception.
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, e.Message, "MemberClass.Property_char"))
                    return 1;
                if ((char)dy.Property_char != 'a')
                    return 1;
            }
            finally
            {
                dy.myChar = 'b';
            }

            if ((char)dy.Property_char != 'b')
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass011.regclass011
{
    // <Title> Tests regular class regular property used in anonymous method.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

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
            Func<char, char?> func = delegate (char arg)
          {
              mc.myCharNull = arg;
              dy = mc; // struct need to re-assign the value.
              return dy.Property_charNull;
          }

            ;
            char? result = func('a');
            if (result == 'a')
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass012.regclass012
{
    // <Title> Tests regular class regular property used in lambda expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Linq;

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            return TestGet() + TestSet();
        }

        private static int TestSet()
        {
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            dy.Property_ulongArr = new ulong[]
            {
            1, 2, 3, 4, 3, 4
            }

            ;
            dy.Property_ulong = (ulong)4;
            var list = mc.Property_ulongArr.Where(p => p == (ulong)mc.Property_ulong).ToList();
            return list.Count - 2;
        }

        private static int TestGet()
        {
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            mc.Property_ulongArr = new ulong[]
            {
            1, 2, 3, 4, 3, 4
            }

            ;
            mc.Property_ulong = 4;
            var list = ((ulong[])dy.Property_ulongArr).Where(p => p == (ulong)dy.Property_ulong).ToList();
            return list.Count - 2;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass013.regclass013
{
    // <Title> Tests regular class regular property used in the foreach loop body.</Title>
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
            char result = default(char);
            char[] LoopArray = new char[]
            {
            'a', 'b'
            }

            ;
            foreach (char c in LoopArray)
            {
                mc.myCharNullArr = new char?[]
                {
                c
                }

                ;
                dy = mc;
                result = ((char?[])dy.myCharNullArr)[0].Value;
            }

            if (result == 'b')
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass014.regclass014
{
    // <Title> Tests regular class regular property used in do/while expression.</Title>
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
            mc.Property_decimalNull = -10.1M;
            dynamic dy = mc;
            do
            {
                mc.Property_decimalNull += 1;
                dy = mc; // for struct we should re-assign.
            }
            while ((decimal?)dy.Property_decimalNull < 0M);
            if ((decimal?)mc.Property_decimalNull == 0.9M)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass017.regclass017
{
    // <Title> Tests regular class regular property used in using expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.IO;

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
            mc.myChar = (char)256;
            dynamic dy = mc;
            using (MemoryStream ms = new MemoryStream((int)dy.Property_char))
            {
                if (ms.Capacity != 256)
                    return 1;
            }

            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass018.regclass018
{
    // <Title> Tests regular class regular property used in try/catch/finally.</Title>
    // <Description>
    // try/catch/finally that uses an anonymous method and refer two dynamic parameters.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

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
            mc.Property_decimalNullArr = new decimal?[]
            {
            0M, 1M, 1.3M
            }

            ;
            mc.myStruct = new MyStruct()
            {
                Number = 3
            }

            ;
            dynamic dy = mc;
            int result = -1;
            try
            {
                Func<decimal?[], MyStruct, int> func = delegate (decimal?[] x, MyStruct y)
              {
                  int tmp = 0;
                  foreach (decimal? d in x)
                  {
                      tmp += (int)d.Value;
                  }

                  tmp += y.Number;
                  return tmp;
              }

                ;
                result = func((decimal?[])dy.Property_decimalNullArr, (MyStruct)dy.Property_MyStruct);
            }
            finally
            {
                result += (int)dy.Property_MyStruct.Number;
            }

            if (result != 8)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass019.regclass019
{
    // <Title> Tests regular class regular property used in foreach.</Title>
    // <Description>
    // foreach inside a using statement that uses the dynamic introduced by the using statement.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.IO;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int[] array = new int[]
            {
            1, 2
            }

            ;
            MemoryStream ms = new MemoryStream(new byte[]
            {
            84, 101, 115, 116
            }

            ); //Test
            MemberClass mc = new MemberClass();
            mc.Property_dynamic = true;
            dynamic dy = mc;
            string result = string.Empty;
            using (dynamic sr = new StreamReader(ms, (bool)dy.Property_dynamic))
            {
                foreach (int s in array)
                {
                    ms.Position = 0;
                    string m = ((StreamReader)sr).ReadToEnd();
                    result += m + s.ToString();
                }
            }

            //Test1Test2
            if (result == "Test1Test2")
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass020.regclass020
{
    // <Title> Tests regular class regular property used in iterator that calls to a lambda expression.</Title>
    // <Description>
    // foreach inside a using statement that uses the dynamic introduced by the using statement.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Collections;

    public class Test
    {
        private static MemberClass s_mc;
        private static dynamic s_dy;
        static Test()
        {
            s_mc = new MemberClass();
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            decimal index = 1M;
            s_mc.Property_decimalArr = new decimal[]
            {
            1M, 2M, 3M
            }

            ;
            s_dy = s_mc;
            Test t = new Test();
            foreach (decimal i in t.Increment(0))
            {
                if (i != index)
                    return 1;
                index = index + 1;
            }

            if (index != 4)
                return 1;
            return 0;
        }

        public IEnumerable Increment(int number)
        {
            while (number < s_mc.Property_decimalArr.Length)
            {
                Func<decimal[], decimal> func = (decimal[] x) => x[number++];
                yield return func(s_dy.Property_decimalArr);
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass021.regclass021
{
    // <Title> Tests regular class regular property used in object initializer inside a collection initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        private float _field1;
        private float?[] _field2;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass mc = new MemberClass();
            mc.Property_Float = 1.23f;
            mc.Property_FloatNullArr = new float?[]
            {
            null, 1.33f
            }

            ;
            dynamic dy = mc;
            List<Test> list = new List<Test>()
            {
            new Test()
            {
            _field1 = dy.Property_Float, _field2 = dy.Property_FloatNullArr
            }
            }

            ;
            if (list.Count == 1 && list[0]._field1 == 1.23f && list[0]._field2.Length == 2 && list[0]._field2[0] == null && list[0]._field2[1] == 1.33f)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass022.regclass022
{
    // <Title> Tests regular class regular property used in static method body.</Title>
    // <Description>
    // set only  property access.
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
            dy.Property_MyClass = new MyClass()
            {
                Field = -1
            }

            ;
            mc = dy; //to circumvent the boxing of the struct
            if (mc.myClass.Field == -1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass023.regclass023
{
    // <Title> Tests regular class regular property used in static method body.</Title>
    // <Description>
    // Negative: set only  property access
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            dy.Property_MyClass = new MyClass()
            {
                Field = -1
            }

            ;
            mc = dy; //to circumvent the boxing of the struct
            if (mc.myClass.Field != -1)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass024.regclass024
{
    // <Title> Tests regular class regular property used in throws.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

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
            dy.Property_string = "Test Message";
            try
            {
                throw new ArithmeticException((string)dy.Property_string);
            }
            catch (ArithmeticException ae)
            {
                if (ae.Message == "Test Message")
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass025.regclass025
{
    // <Title> Tests regular class regular property used in field initializer.</Title>
    // <Description>
    // Negative
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static MemberClass s_mc;
        private static dynamic s_dy;
        private MyEnum _me = s_dy.Property_MyEnum;
        static Test()
        {
            s_mc = new MemberClass();
            s_dy = s_mc;
            s_dy.Property_MyEnum = MyEnum.Third;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            if (t._me != MyEnum.Third)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass026.regclass026
{
    // <Title> Tests regular class regular property used in set only property body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private MemberClass _mc;
        private MyEnum? MyProp
        {
            set
            {
                _mc = new MemberClass();
                dynamic dy = _mc;
                dy.Property_MyEnumNull = value;
                _mc = dy; // for struct.
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
            t.MyProp = MyEnum.Second;
            if (t._mc.myEnumNull == MyEnum.Second)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass027.regclass027
{
    // <Title> Tests regular class regular property used in read only property body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private MyEnum MyProp
        {
            get
            {
                dynamic dy = new MemberClass();
                dy.Property_MyEnumArr = new MyEnum[]
                {
                MyEnum.Second, default (MyEnum)}

                ;
                return dy.myEnumArr[0];
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
            if (t.MyProp == MyEnum.Second)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass028.regclass028
{
    // <Title> Tests regular class regular property used in static read only property body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static MyEnum? MyProp
        {
            get
            {
                dynamic dy = new MemberClass();
                dy.Property_MyEnumNullArr = new MyEnum?[]
                {
                null, MyEnum.Second, default (MyEnum)}

                ;
                return dy.myEnumNullArr[0];
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (Test.MyProp == null)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass029.regclass029
{
    // <Title> Tests regular class regular property used in static method body.</Title>
    // <Description>
    // get only  property access.
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
            mc.myStructNull = new MyStruct()
            {
                Number = int.MinValue
            }

            ;
            dynamic dy = mc;
            MyStruct? result = dy.Property_MyStructNull;
            if (result.Value.Number == int.MinValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass030.regclass030
{
    // <Title> Tests regular class regular property used in method call argument.</Title>
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
            mc.myStructArr = new MyStruct[]
            {
            new MyStruct()
            {
            Number = 1
            }

            , new MyStruct()
            {
            Number = -1
            }
            }

            ;
            dynamic dy = mc;
            bool result = TestMethod(1, string.Empty, (MyStruct[])dy.Property_MyStructArr);
            if (result)
                return 0;
            return 1;
        }

        private static bool TestMethod<V, U>(V v, U u, params MyStruct[] ms)
        {
            if (v.GetType() != typeof(int))
                return false;
            if (u.GetType() != typeof(string))
                return false;
            if (ms.Length != 2 || ms[0].Number != 1 || ms[1].Number != -1)
                return false;
            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass031.regclass031
{
    // <Title> Tests regular class regular property used in static method body.</Title>
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
            mc.myStructNullArr = new MyStruct?[]
            {
            null, new MyStruct()
            {
            Number = -1
            }
            }

            ;
            if (((MyStruct?[])dy.Property_MyStructNullArr)[0] == null)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass032.regclass032
{
    // <Title> Tests regular class regular property used in lock expression.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            dy.Property_shortNull = (short)-1;
            try
            {
                lock (dy.Property_shortNull)
                {
                }
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadProtectedAccess, e.Message, "MemberClass.Property_shortNull", "MemberClass", "Test"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass034.regclass034
{
    // <Title> Tests regular class regular property used in foreach loop.</Title>
    // <Description>
    // Negative
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
            mc.Property_shortArr = new short[]
            {
            1, 2, 3, 4, 5
            }

            ;
            dynamic dy = mc;
            short i = 1;
            try
            {
                foreach (var x in dy.Property_shortArr) //protected
                {
                    if (i++ != (short)x)
                        return 1;
                }
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.regclass.regclass035.regclass035
{
    // <Title> Tests regular class regular property used in method body.</Title>
    // <Description>
    // Negative
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
            return Test.TestGetMethod(new MemberClass()) + Test.TestSetMethod(new MemberClass()) == 0 ? 0 : 1;
        }

        public static int TestGetMethod(MemberClass mc)
        {
            dynamic dy = mc;
            mc.Property_ulongNullArr = new ulong?[]
            {
            null, 1
            }

            ;
            if (dy.Property_ulongNullArr.Length == 2 && dy.Property_ulongNullArr[0] == null && dy.Property_ulongNullArr[1] == 1)
                return 0;
            else
                return 1;
        }

        public static int TestSetMethod(MemberClass mc)
        {
            dynamic dy = mc;
            dy.Property_ulongNullArr = new ulong?[]
            {
            null, 1
            }

            ;
            if (mc.Property_ulongNullArr.Length == 2 && mc.Property_ulongNullArr[0] == null && mc.Property_ulongNullArr[1] == 1)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}
