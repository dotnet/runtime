// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using RuntimeLibrariesTest;
using TypeOfRepo;


public class StaticsTests
{
#if USC
    public struct MyCustomType
    {
        string _field;
        public MyCustomType(String s) { _field = s; }
        public override string ToString() { return _field; }
    }
#else
    public class MyCustomType
    {
        string _field;
        public MyCustomType(String s) { _field = s; }
        public override string ToString() { return _field; }
    }
#endif

    public class GenericTypeWithNonGcStaticField<T>
    {
        protected static int _myField;
        
        public GenericTypeWithNonGcStaticField(int i)
        {
            _myField = i;
        }

        public override string ToString()
        {
            return _myField.ToString();
        }
    } 
 
    public class GenericTypeWithMultipleNonGcStaticFields<T>
    {
        static int _myInt1;
        static bool _myBool1;
        static int _myInt2;

        public GenericTypeWithMultipleNonGcStaticFields(int int1, bool bool1, int int2)
        {
            _myInt1 = int1;
            _myBool1 = bool1;
            _myInt2 = int2;
        }

        public override string ToString()
        {
            return _myInt1.ToString() + " " + _myBool1.ToString() + " " + _myInt2.ToString();
        }
    }

    public class DerivedGenericTypeWithNonGcStaticField<T> : GenericTypeWithNonGcStaticField<T>
    {
        protected static int _mySpecializedField;

        public DerivedGenericTypeWithNonGcStaticField(int myField, int mySpecializedField) : base(myField)
        {
            _myField = myField;
            _mySpecializedField = mySpecializedField;
        }

        public override string ToString()
        {
            return base.ToString() + " " + _mySpecializedField.ToString();
        }
    }

    public class SuperDerivedGeneric<T> : DerivedGenericTypeWithNonGcStaticField<T>
    {
        static int _mySuperDerivedField;

        public SuperDerivedGeneric(int myField, int mySpecializedField, int superDerivedField) : base(myField, mySpecializedField)
        {
            _mySuperDerivedField = superDerivedField;
        }

        public override string ToString()
        {
            return base.ToString() + " " + _mySuperDerivedField.ToString();
        }
    }

    public class GenericTypeWithStaticTimeSpanField<T>
    {
        static TimeSpan s_timespan;

        public GenericTypeWithStaticTimeSpanField(double s)
        {
            s_timespan = TimeSpan.FromSeconds(s);
        }

        static GenericTypeWithStaticTimeSpanField()
        {
            s_timespan = TimeSpan.FromSeconds(42.0);
        }

        public override string ToString()
        {
            return s_timespan.ToString();
        }
    }

    public class GenericTypeWithGcStaticField<T>
    {
        static string _myString;

        public GenericTypeWithGcStaticField(string myString)
        {
            _myString = myString;
        }

        public override string ToString()
        {
            return _myString;
        }

        public void SetMyString(string s)
        {
            _myString = s;
        }
    }

#if USC
    public struct SillyString
#else
    public class SillyString
#endif
    {
        public override string ToString()
        {
            return "SillyString";
        }
    }

    public class GenericTypeWithStaticFieldOfTypeT<T>
    {
        static T _myField;

        public GenericTypeWithStaticFieldOfTypeT(T val)
        {
            _myField = val;
        }

        public T Field
        {
            get
            {
                return _myField;
            }
        }

        public override string ToString()
        {
            return _myField.ToString();
        }
    }

    public class ClassWithStaticConstructor<T>
    {
        static string s_myStaticString;

        static ClassWithStaticConstructor()
        {
            s_myStaticString = typeof(T).ToString();
        }

        public override string ToString()
        {
            return s_myStaticString;
        }
    }

    public class AnotherClassWithStaticConstructor<T> : ClassWithStaticConstructor<T>
    {
        static int s_cctorRunCounter;

        static AnotherClassWithStaticConstructor()
        {
            ++s_cctorRunCounter;
        }

        public override string ToString()
        {
            return base.ToString() + " " + s_cctorRunCounter.ToString();
        }
    }

    [TestMethod]
    public static void TestStatics()
    {
        // Test that different instantiations of the same type get their own static data
        {
            Type stringInstType = TypeOf.ST_GenericTypeWithStaticFieldOfTypeT.MakeGenericType(typeof(MyCustomType));
            Type sillyStringInstType = TypeOf.ST_GenericTypeWithStaticFieldOfTypeT.MakeGenericType(typeof(SillyString));

            var sillyStringInst = Activator.CreateInstance(sillyStringInstType, new object[] { new SillyString() });
            var stringInst = Activator.CreateInstance(stringInstType, new object[] { new MyCustomType("Not a silly string") });

            string result = sillyStringInst.ToString() + " " + stringInst.ToString();

            Assert.AreEqual("SillyString Not a silly string", result);
        }

        // Test that different instantiations of the same type get their own static data
        {
            Type stringInstType = TypeOf.ST_GenericTypeWithNonGcStaticField.MakeGenericType(TypeOf.CommonType1);
            Type objectInstType = TypeOf.ST_GenericTypeWithNonGcStaticField.MakeGenericType(TypeOf.CommonType2);
            Type boolInstType = TypeOf.ST_GenericTypeWithNonGcStaticField.MakeGenericType(typeof(StaticsTests));

            var objectInst = Activator.CreateInstance(objectInstType, new object[] { 123 });
            var stringInst = Activator.CreateInstance(stringInstType, new object[] { 666 });
            var boolInst = Activator.CreateInstance(boolInstType, new object[] { 999 });

            string result = objectInst.ToString() + " " + stringInst.ToString() + " " + boolInst.ToString();

            Assert.AreEqual("123 666 999", result);
        }

        // Validate that multiple static non-GC fields on the generic type work correctly over several instantiations
        {
            Type stringInstType = TypeOf.ST_GenericTypeWithMultipleNonGcStaticFields.MakeGenericType(TypeOf.CommonType1);
            Type objectInstType = TypeOf.ST_GenericTypeWithMultipleNonGcStaticFields.MakeGenericType(TypeOf.CommonType2);
            Type boolInstType = TypeOf.ST_GenericTypeWithMultipleNonGcStaticFields.MakeGenericType(typeof(StaticsTests));

            var objectInst = Activator.CreateInstance(objectInstType, new object[] { 123, true, 321 });
            var stringInst = Activator.CreateInstance(stringInstType, new object[] { 666, false, 777 });
            var boolInst = Activator.CreateInstance(boolInstType, new object[] { 999, true, 111 });

            string result = objectInst.ToString() + " " + stringInst.ToString() + " " + boolInst.ToString();

            Assert.AreEqual("123 True 321 666 False 777 999 True 111", result);
        }

        // Validate statics on several layers of a generic type hierarchy 
        {
            Type stringInstType = TypeOf.ST_SuperDerivedGeneric.MakeGenericType(TypeOf.CommonType1);
            Type objectInstType = TypeOf.ST_SuperDerivedGeneric.MakeGenericType(TypeOf.CommonType2);

            var objectInst = Activator.CreateInstance(objectInstType, new object[] { 123, 321, 456 });
            var stringInst = Activator.CreateInstance(stringInstType, new object[] { 666, 999, 111 });

            string result = objectInst.ToString() + " " + stringInst.ToString();

            Assert.AreEqual("123 321 456 666 999 111", result);
        }

        {
            Type objectInstType = TypeOf.ST_GenericTypeWithStaticTimeSpanField.MakeGenericType(TypeOf.CommonType2);
            Type stringInstType = TypeOf.ST_GenericTypeWithStaticTimeSpanField.MakeGenericType(TypeOf.CommonType1);

            var objectInst = Activator.CreateInstance(objectInstType, new object[] { 123.0 });
            var stringInst = Activator.CreateInstance(stringInstType, new object[] { 456.0 });

            string result = objectInst.ToString() + " " + stringInst.ToString();

            Assert.AreEqual("00:02:03 00:07:36", result);
        }

        // GC statics tests
        {
            Type stringInstType = TypeOf.ST_GenericTypeWithGcStaticField.MakeGenericType(TypeOf.CommonType1);
            Type objectInstType = TypeOf.ST_GenericTypeWithGcStaticField.MakeGenericType(TypeOf.CommonType2);

            var objectInst = Activator.CreateInstance(objectInstType, new object[] { "Hello" });
            var stringInst0 = Activator.CreateInstance(stringInstType, new object[] { "And" });
            var stringInst = Activator.CreateInstance(stringInstType, new object[] { "Bye" });

            string result = objectInst.ToString() + " " + stringInst0.ToString() + " " + stringInst.ToString();

            Assert.AreEqual("Hello Bye Bye", result);
        }

        // Statics keep things alive
        {
            Type stringInstType = TypeOf.ST_GenericTypeWithGcStaticField.MakeGenericType(TypeOf.CommonType1);
            
            var stringInst = Activator.CreateInstance(stringInstType, new object[] { "Bye" });
            var setMyStringMethodInfo = stringInstType.GetTypeInfo().GetDeclaredMethod("SetMyString");
            Console.WriteLine("Setting GC static");

            {
                string newString = "New Value Of The String!";
                string my = newString.Replace("!", "");
                setMyStringMethodInfo.Invoke(stringInst, new object[] {my});
            }

            Console.WriteLine("Calling GC.Collect");
            GC.Collect();

            Console.WriteLine("Verifying GC static wasn't collected erroneously");
            string result = stringInst.ToString();
            Assert.AreEqual("New Value Of The String", result);
        }

        {
            Type stringInstType = TypeOf.ST_ClassWithStaticConstructor.MakeGenericType(TypeOf.CommonType1);
            Type objectInstType = TypeOf.ST_ClassWithStaticConstructor.MakeGenericType(TypeOf.CommonType2);

            var objectInst = Activator.CreateInstance(objectInstType);
            var stringInst = Activator.CreateInstance(stringInstType);

            string result = objectInst.ToString() + " " + stringInst.ToString();
            Assert.AreEqual("CommonType2 CommonType1", result);
        }

        {
            Type stringInstType = TypeOf.ST_AnotherClassWithStaticConstructor.MakeGenericType(TypeOf.CommonType1);
            Type objectInstType = TypeOf.ST_AnotherClassWithStaticConstructor.MakeGenericType(TypeOf.CommonType2);
            Type sbInstType = TypeOf.ST_AnotherClassWithStaticConstructor.MakeGenericType(typeof(StringBuilder));

            var objectInst = Activator.CreateInstance(objectInstType);
            var stringInst = Activator.CreateInstance(stringInstType);
            var sbInst = Activator.CreateInstance(sbInstType);

            // Make sure the class constructor is only run once - the two results should be the same (the static int
            // should only get incremented once per instantiation).
            string result1 = objectInst.ToString() + " " + stringInst.ToString() + " " + sbInst.ToString();
            string result2 = objectInst.ToString() + " " + stringInst.ToString() + " " + sbInst.ToString();
            Assert.AreEqual(result1, result2);
            Assert.AreEqual("CommonType2 1 CommonType1 1 System.Text.StringBuilder 1", result1);
        }
    }
}
