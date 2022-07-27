// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CoreFXTestLibrary;
using TypeOfRepo;

namespace UniversalGen
{
    public enum MyEnum { ME_1, ME_2 }

    public class MyGen<T>
    {
#pragma warning disable 0414            // The field '...blah...' is assigned but its value is never used
        int a;
        long b;
        char c;
        double d;
        MyEnum e;
        T f;
        object g;
        MyGenStruct<T> h;
        MyGen<T> i;
        MyGenStruct<T> j;
        MyGenStruct<float> k;
        MyStruct l;
        MyGen<Type> m;

        int[] a_array;
        long[] b_array;
        char[] c_array;
        double[] d_array;
        MyEnum[] e_array;
        T[] f_array;                  // Universal canonical arrays NYI
        object[] g_array;
        MyGenStruct<T>[] h_array;     // Universal canonical arrays NYI
        MyGen<T>[] i_array;           // Universal canonical arrays NYI
        MyGenStruct<T>[] j_array;     // Universal canonical arrays NYI
        MyGenStruct<float>[] k_array; // Universal canonical arrays NYI
        MyStruct[] l_array;           // Universal canonical arrays NYI
        MyGen<Type>[] m_array;        // Universal canonical arrays NYI

        static int a_static;
        static long b_static;
        static char c_static;
        static double d_static;
        static MyEnum e_static;
        static T f_static;
        static object g_static;
        static MyGenStruct<T> h_static;
        static MyGen<T> i_static;
        static MyGenStruct<T> j_static;
        static MyGenStruct<float> k_static;
        static MyStruct l_static;
        static MyGen<string> m_static;

        static int[] a_array_static;
        static long[] b_array_static;
        static char[] c_array_static;
        static double[] d_array_static;
        static MyEnum[] e_array_static;
        static T[] f_array_static;
        static object[] g_array_static;
        static MyGenStruct<T>[] h_array_static;
        static MyGen<T>[] i_array_static;
        static MyGenStruct<T>[] j_array_static;
        static MyGenStruct<float>[] k_array_static;
        static MyStruct[] l_array_static;
        static MyGen<string>[] m_array_static;

        public MyGen()
        {
            a = 1;
            b = 2;
            c = 'c';
            d = 0.3;
            e = MyEnum.ME_1;
            f = default(T);
            g = new object();
            h = default(MyGenStruct<T>);
            i = null;
            j = default(MyGenStruct<T>);
            k = default(MyGenStruct<float>);
            l = default(MyStruct);
            m = null;

            a_array = null;
            b_array = null;
            c_array = null;
            d_array = null;
            e_array = null;
            f_array = null;
            g_array = null;
            h_array = null;
            i_array = null;
            j_array = null;
            k_array = null;
            l_array = null;
            m_array = null;

            a_static = 2;
            b_static = 3;
            c_static = 'd';
            d_static = 0.5;
            e_static = MyEnum.ME_2;
            f_static = default(T);
            g_static = null;
            h_static = default(MyGenStruct<T>);
            i_static = null;
            j_static = default(MyGenStruct<T>);
            k_static = default(MyGenStruct<float>);
            l_static = default(MyStruct);
            m_static = null;

            a_array_static = null;
            b_array_static = null;
            c_array_static = null;
            d_array_static = null;
            e_array_static = null;
            f_array_static = null;
            g_array_static = null;
            h_array_static = null;
            i_array_static = null;
            j_array_static = null;
            k_array_static = null;
            l_array_static = null;
            m_array_static = null;
        }
#pragma warning restore 0414
    }

    public struct MyStruct { }

    public struct MyGenStruct<T>
    {
#pragma warning disable 0414            // The field '...blah...' is assigned but its value is never used

        int a;
        T b;
        MyGen<T> c;
        string d;

        public MyGenStruct(object o)
        {
            a = 2;
            b = default(T);
            c = default(MyGen<T>);
            d = "asd";
        }

#pragma warning restore 0414
    }

    public class MyDerivedList<T> : List<T>
    {
    }

    public struct MyListItem
    {
        string _id;
        public MyListItem(string id) { _id = id; }
        public override string ToString() { return "MyListItem(" + _id + ")"; }
    }

    public class StringWrapper
    {
        string _f;
        public StringWrapper(string s) { _f = s; }
        public override string ToString() { return _f; }
    }

    public class TestFieldsBase
    {
        public virtual void SetVal1(object val) { }
        public virtual void SetVal2(object val) { }
        public virtual void SetVal3(object val) { }
        public virtual void SetVal4(object val) { }
        public virtual void SetVal5(object val) { }
        public virtual void SetVal6(object val) { }
        public virtual object GetVal1() { return null; }
        public virtual object GetVal2() { return null; }
        public virtual object GetVal3() { return null; }
        public virtual object GetVal4() { return null; }
        public virtual object GetVal5() { return null; }
        public virtual object GetVal6() { return null; }
    }

    public class UCGInstanceFields<T, U> : TestFieldsBase
    {
        protected T _1;
        protected U _2;
        protected int _3; // Test field of known type at unknown offset

        public override void SetVal1(object val) { _1 = (T)val; }
        public override void SetVal2(object val) { _2 = (U)val; }
        public override void SetVal3(object val) { _3 = (int)val; }
        public override object GetVal1() { return _1; }
        public override object GetVal2() { return _2; }
        public override object GetVal3() { return _3; }
    }

    public class UCGInstanceFieldsDerived<T, U> : UCGInstanceFields<T, T>
    {
        T _4;
        double _5; // Test field of known type at unknown offset
        U _6;

        public override void SetVal1(object val) { _1 = (T)val; }
        public override void SetVal4(object val) { _4 = (T)val; }
        public override void SetVal5(object val) { _5 = (double)val; }
        public override void SetVal6(object val) { _6 = (U)val; }
        public override object GetVal3() { return _3; }
        public override object GetVal4() { return _4; }
        public override object GetVal5() { return _5; }
        public override object GetVal6() { return _6; }
    }

    public class UCGInstanceFieldsDerived2<T> : UCGInstanceFields<float, T>
    {
        T _4;

        public override void SetVal4(object val) { _4 = (T)val; }
        public override object GetVal4() { return _4; }
    }

    public class UCGInstanceFieldsMostDerived<T> : UCGInstanceFieldsDerived2<float>
    {
        double _5; // Test field of known type at unknown offset
        T _6;

        public override void SetVal5(object val) { _5 = (double)val; }
        public override void SetVal6(object val) { _6 = (T)val; }
        public override object GetVal5() { return _5; }
        public override object GetVal6() { return _6; }
    }

    public class UCGStaticFields<T, U> : TestFieldsBase
    {
        static T _1;
        static U _2;
        static int _3; // Test field of known type at unknown offset

        public override void SetVal1(object val) { _1 = (T)val; }
        public override void SetVal2(object val) { _2 = (U)val; }
        public override void SetVal3(object val) { _3 = (int)val; }
        public override object GetVal1() { return _1; }
        public override object GetVal2() { return _2; }
        public override object GetVal3() { return _3; }
    }

    public class UCGThreadStaticFields<T, U> : TestFieldsBase
    {
        [ThreadStatic]
        static T _1;
        [ThreadStatic]
        static U _2;
        [ThreadStatic]
        static int _3; // Test field of known type at unknown offset

        public override void SetVal1(object val) { _1 = (T)val; }
        public override void SetVal2(object val) { _2 = (U)val; }
        public override void SetVal3(object val) { _3 = (int)val; }
        public override object GetVal1() { return _1; }
        public override object GetVal2() { return _2; }
        public override object GetVal3() { return _3; }
    }

    public class UCGStaticFieldsLayoutCompatStatic<T, U> : TestFieldsBase
    {
        public static T _1;
        public static U _2;
        public static int _3; // Test field of known type at unknown offset

        public override void SetVal1(object val) { _1 = (T)val; }
        public override void SetVal2(object val) { _2 = (U)val; }
        public override void SetVal3(object val) { _3 = (int)val; }
        public override object GetVal1() { return _1; }
        public override object GetVal2() { return _2; }
        public override object GetVal3() { return _3; }
    }

    public class UCGStaticFieldsLayoutCompatDynamic<T, U> : TestFieldsBase
    {
        public override void SetVal1(object val) { UCGStaticFieldsLayoutCompatStatic<T,U>._1 = (T)val; }
        public override void SetVal2(object val) { UCGStaticFieldsLayoutCompatStatic<T, U>._2 = (U)val; }
        public override void SetVal3(object val) { UCGStaticFieldsLayoutCompatStatic<T, U>._3 = (int)val; }
        public override object GetVal1() { return UCGStaticFieldsLayoutCompatStatic<T, U>._1; }
        public override object GetVal2() { return UCGStaticFieldsLayoutCompatStatic<T, U>._2; }
        public override object GetVal3() { return UCGStaticFieldsLayoutCompatStatic<T, U>._3; }
    }

    #region Test case taken from a real app (minimal repro for a field layout bug)
    public class GenBaseType<T> where T : IComparable<T>
    {
        protected String _myString = new String('c', 3);
        protected T _tValue;
        protected IComparer<T> _comparer;

        protected GenBaseType(T tValue, IComparer<T> comparer)
        {
            if (comparer == null)
            {
                throw new System.ArgumentNullException("comparer");
            }
            this._tValue = tValue;
            this._comparer = comparer;
        }
    }
    public class GenDerivedType<T, U> : GenBaseType<T> where T : IComparable<T>
    {
        IList _iListObject;
        U _uValue;
        Func<object, IList, T, U, IComparer<T>, IDisposable> _action;

        public GenDerivedType(IList iListObject, U uValue, Func<object, IList, T, U, IComparer<T>, IDisposable> action, T tValue, IComparer<T> comparer)
            : base(tValue, comparer)
        {
            if (iListObject == null)
            {
                throw new System.ArgumentNullException("iListObject");
            }
            if (action == null)
            {
                throw new System.ArgumentNullException("action");
            }
            this._iListObject = iListObject;
            this._uValue = uValue;
            this._action = action;
        }
        public GenDerivedType(IList iListObject, U uValue, Func<object, IList, T, U, IComparer<T>, IDisposable> action, T tValue)
            : this(iListObject, uValue, action, tValue, Comparer<T>.Default)
        {
            action(this._myString, this._iListObject, this._tValue, this._uValue, this._comparer);
        }
    }
    public class GenDerivedType_Activator<T> where T : new()
    {
        static List<T> _listToUseAsParam = new List<T>(new T[] { default(T), new T() });
        static TimeSpan _timeSpanToUseAsParam = TimeSpan.FromSeconds(123456.0);
        static T _tToUseAsParam = new T();
        static GenDerivedType<TimeSpan, T> _instance;
        static String _delResult;

        public GenDerivedType_Activator()
        {
            _instance = new GenDerivedType<TimeSpan, T>(_listToUseAsParam, _tToUseAsParam, new Func<object, IList, TimeSpan, T, IComparer<TimeSpan>, IDisposable>(FuncForDelegate), _timeSpanToUseAsParam);
        }
        public static IDisposable FuncForDelegate(object stringObj, IList iListObject, TimeSpan ts, T tValue, IComparer<TimeSpan> comparer)
        {
            _delResult = "GenDerivedType_Activator<" + typeof(T) + ">.FuncForDelegate";
            Assert.AreEqual(stringObj, "ccc");
            Assert.AreEqual(_listToUseAsParam, iListObject);
            Assert.AreEqual(_timeSpanToUseAsParam, ts);
            Assert.AreEqual(_tToUseAsParam, tValue);
            Assert.AreEqual(comparer, Comparer<TimeSpan>.Default);
            return null;
        }
        public override string ToString() { return _delResult; }
    }
    #endregion

    public class TestClassConstructorBase
    {
        public static Type s_cctorOutput = null;
        public virtual bool QueryStatic() { return false; }
    }

    public class UCGClassConstructorType<T> : TestClassConstructorBase
    {
        private static bool s_cctorRun = RunInCCtor();

        private static bool RunInCCtor()
        {
            TestClassConstructorBase.s_cctorOutput = typeof(UCGClassConstructorType<T>);
            return true;
        }

        public override bool QueryStatic() { return s_cctorRun; }
    }

    public interface IGetValue
    {
        int GetValue();
    }

    public struct UCGWrapperStruct : IGetValue
    {
        public UCGWrapperStruct(int wrapValue)
        {
            _WrappedValue = wrapValue;
        }
        public int _WrappedValue;

        public int GetValue()
        {
            return _WrappedValue;
        }
    }

    public interface IGVMTest
    {
        string GVMMethod<T>();
    }

    public class MakeGVMCallBase
    {
        public virtual string CallGvm(IGVMTest obj) { return null; }
    }

    public class MakeGVMCall<T> : MakeGVMCallBase
    {
        public override string CallGvm(IGVMTest obj)
        {
            return obj.GVMMethod<T>();
        }
    }
    public class GVMTestClass<T> : IGVMTest
    {
        public string GVMMethod<U>()
        {
            return typeof(T).ToString() + typeof(U).ToString();
        }
    }

    public struct GVMTestStruct<T> : IGVMTest
    {
        public string GVMMethod<U>()
        {
            return typeof(T).ToString() + typeof(U).ToString();
        }
    }

    public class Base
    {
        public virtual object GetElementAt(int index) { return null; }
        public virtual object this[int index] { get { return null; } set { } }
        public virtual bool EmptyMethodTest(object param) { return true; }
        public virtual object dupTest(object o1, object o2) { return null; }
        public virtual bool FunctionCallTestsSetMember(object o1) { return false; }
        public virtual bool FunctionCallTestsSetMemberByRef(object o1) { return false; }
        public virtual bool FunctionCallTestsByRefGC(object o1) { return false; }
        public virtual bool FunctionCallTestsSetByValue(object o1) { return false; }
        public virtual bool FunctionCallTestsSetLocalByRef(object o1) { return false; }
        public virtual bool FunctionCallTestsSetByValue2(object o1) { return false; }
        public virtual void InterlockedTests(object o1, object o2) { }
        public virtual void nestedTest() {}
    }

    public class UnmanagedByRef<T> : Base where T : struct, IGetValue
    {
        public T refVal;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void TestAsPointer(T x)
        {
            IntPtr unsafeValuePtr = (IntPtr)Unsafe.AsPointer(ref x);
            GC.Collect();
            GC.Collect();
            GC.Collect();
            GC.Collect();
            GC.Collect();
            var res = Unsafe.Read<T>(unsafeValuePtr.ToPointer());
            Assert.IsTrue(this.refVal.Equals(res));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void TestGeneralFunction(T x)
        {
            IntPtr unsafeValuePtr = someFuncWithByRefArgs(ref x);
            GC.Collect();
            GC.Collect();
            GC.Collect();
            GC.Collect();
            GC.Collect();
            T res = Unsafe.Read<T>(unsafeValuePtr.ToPointer());
            Assert.IsTrue(this.refVal.Equals(res));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe IntPtr someFuncWithByRefArgs(ref T x)
        {
            return (IntPtr)Unsafe.AsPointer(ref x);
        }

        public override unsafe bool FunctionCallTestsByRefGC(object o1)
        {
            var x = (T)o1;
            this.refVal = x;
            TestAsPointer(x);
            TestGeneralFunction(x);
            return true;
        }
    }

    public class UCGSamples<T, U> : Base
    {
        public T[] _elements = new T[10];

        public T member;

        public T getMember() { return this.member; }

        public override object GetElementAt(int index)
        {
            return _elements[index];
        }

        public override object this[int index]
        {
            get
            {
                return _elements[index];
            }
            set
            {
                _elements[index] = (T)value;
            }
        }

        private void Empty(T t, T u)
        {
        }

        public override void nestedTest()
        {
            testMethodInner();
            //testMethodInner2();
            testMethodInner3();
        }

        private MyGenStruct<UCGSamples<T,U>> testMethodInner()
        {
            return default(MyGenStruct<UCGSamples<T,U>>);
        }
        private MyGenStruct<MyGenStruct<UCGSamples<T,U>>> testMethodInner3()
        {
            return default(MyGenStruct<MyGenStruct<UCGSamples<T,U>>>);
        }

        public override bool EmptyMethodTest(object param)
        {
            T t = (T) param;
            Empty(t, t);
            return true;
        }

        private T dupTestInternal(T t1, T t2)
        {
            // IL for this method uses a 'dup' opcode
            T local = default(T);
            if ((local = t1).Equals(t2))
            {
                local = t2;
            }
            return local;
        }

        public override object dupTest(object o1, object o2)
        {
            return (object) dupTestInternal((T) o1, (T) o2);
        }

        private void set(T t)
        {
            member = t;
        }

        private void setEQ(T t1, T t2)
        {
            t2 = t1;
        }

        private void setByRefInner(T t, ref T tRef)
        {
            tRef = t;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void setInner(T t1, T t2)
        {
            t1 = t2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void setOuter(T t1, T t2)
        {
            setInner(t1, t2);
        }

        private void setByRef(T t, ref T tRef)
        {
            setByRefInner(t, ref tRef);
        }

        public override bool FunctionCallTestsSetMember(object o1)
        {
            // eventually calls this.set(T) which sets this.member
            T t = (T) o1;
            set(t);
            return this.member.Equals(t) && t.Equals(this.getMember());
        }

        public override bool FunctionCallTestsSetMemberByRef(object o1)
        {
            // same as 'FunctionCallTestsSetMember', but sets this.member via passing it byref
            T t = (T) o1;
            Assert.IsFalse(this.member.Equals(t));
            setByRef(t, ref this.member);
            return this.member.Equals(t);
        }

        public override bool FunctionCallTestsSetByValue(object o1)
        {
            // Calls setEQ, which sets second arg equal to first
            // shouldn't change value of args since we're not passing byref
            T t = (T) o1;
            setEQ(t, this.member);
            return !this.member.Equals(t);
        }

        public override bool FunctionCallTestsSetLocalByRef(object o1)
        {
            // same as 'FunctionCallTests', but sets a local via passing it byref
            T t = (T) o1;
            T t2 = default(T);
            Assert.IsFalse(t2.Equals(t));
            setByRef(t, ref t2);
            return t2.Equals(t);
        }

        public override bool FunctionCallTestsSetByValue2(object o1)
        {
            T t = (T) o1;
            Assert.IsTrue(!this.member.Equals(t));
            setOuter(t, this.member);
            return !this.member.Equals(t);
        }
    }

    public class InterlockedClass<T, U> : Base where T : class
    {
        T member= default(T);
        /*public InterlockedClass()
        {
            member = default(T);
        }*/
        public void setMember(T t)
        {
            this.member = t;
        }
        public T exchangeTest(T val)
        {
            T ret;
            ret = System.Threading.Interlocked.Exchange<T>(ref this.member, val);
            Assert.IsTrue(this.member.Equals(val));
            return ret;
        }
        public T compareExchangeTest(T val)
        {
            T ret;
            ret = System.Threading.Interlocked.CompareExchange<T>(ref this.member, val, this.member);
            Assert.IsTrue(this.member.Equals(val));
            return ret;
        }
        public override void InterlockedTests(object o1, object o2)
        {
            this.setMember((T)o1);
            T ret = this.exchangeTest((T)o2);
            Assert.IsTrue(ret.Equals(o1));

            this.setMember((T)o1);
            Assert.IsTrue(this.member.Equals((T) o1));
            ret = this.compareExchangeTest((T)o2);
            Assert.IsTrue(ret.Equals(o1));
        }
    }

    public class Test
    {
        [TestMethod]
        public static void TestInterlockedPrimitives()
        {
            var t = TypeOf.UG_InterlockedClass.MakeGenericType(TypeOf.String, TypeOf.Short);
            Base o = (Base)Activator.CreateInstance(t);
            o.InterlockedTests((object)"abc", (object)"def");
        }

        [TestMethod]
        public static void TestArraysAndGC()
        {
            var t = TypeOf.UG_UCGSamples.MakeGenericType(TypeOf.Short, TypeOf.Short);
            Base o = (Base)Activator.CreateInstance(t);
            for (int i = 0; i < 10; i++)
                o[i] = (short)i;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            for (int i = 0; i < 10; i++)
                Assert.IsTrue((short)o[i] == (short)i);

            t = TypeOf.UG_UCGSamples.MakeGenericType(typeof(StringWrapper), TypeOf.Short);
            o = (Base)Activator.CreateInstance(t);
            for (int i = 0; i < 10; i++)
                o[i] = new StringWrapper("teststring" + i);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            for (int i = 0; i < 10; i++)
                Assert.IsTrue(((StringWrapper)o[i]).ToString() == "teststring" + i);
        }

        [TestMethod]
        public static void TestUSGByRefFunctionCalls()
        {
            var t = TypeOf.UG_UnmanagedByRef.MakeGenericType(typeof(UCGWrapperStruct));
            Base o = (Base)Activator.CreateInstance(t);
            Assert.IsTrue(o.FunctionCallTestsByRefGC(new UCGWrapperStruct(2)));
        }

        [TestMethod]
        public static void TestUSGSamples()
        {
            var t = TypeOf.UG_UCGSamples.MakeGenericType(TypeOf.Short, TypeOf.Short);
            Base o = (Base)Activator.CreateInstance(t);

            var result = o.GetElementAt(5);
            Assert.AreEqual(result.ToString(), "0");
            Assert.AreEqual(result.GetType(), TypeOf.Short);

            Assert.IsTrue(o.EmptyMethodTest((short) 45));
            Assert.AreEqual((short)o.dupTest((short)12, (short)-453),
                            (short)12);

            o.nestedTest();

            Assert.IsTrue(o.FunctionCallTestsSetMember((short) 79));
            Assert.IsTrue(o.FunctionCallTestsSetMemberByRef((short) 85));
            Assert.IsTrue(o.FunctionCallTestsSetByValue((short) 138));
            Assert.IsTrue(o.FunctionCallTestsSetLocalByRef((short) 19));
            Assert.IsTrue(o.FunctionCallTestsSetByValue2((short) 99));

            for (int i = 0; i < 10; i++)
            {
                // No explicit typecasts
                int val = 123 + i;

                try
                {
                    // This assignment should throw an InvalidCastException
                    // We have to explicitly cast "val" to "short"
                    o[i] = val;
                    Assert.IsTrue(false);
                }
                catch (InvalidCastException) { }

                o[i] = (short)val;
                Assert.AreEqual(o.GetElementAt(i), o[i]);
                Assert.AreEqual(o[i], (short)val);
                Assert.AreEqual(o.GetElementAt(i).ToString(), val.ToString());

                // Explicit typecast to the correct type
                val = 456 + i * 10;
                o[i] = (short)val;
                Assert.AreEqual((short)o.GetElementAt(i), (short)o[i]);
                Assert.AreEqual((short)o[i], (short)val);
                Assert.AreEqual(((short)o.GetElementAt(i)).ToString(), val.ToString());
            }
        }

        [TestMethod]
        public static void TestMakeGenericType()
        {
            new MyGenStruct<Type>(null);

            var t = TypeOf.UG_MyGen.MakeGenericType(TypeOf.Object);
            Assert.AreEqual(t.ToString(), "UniversalGen.MyGen`1[System.Object]");

            t = TypeOf.UG_MyGen.MakeGenericType(TypeOf.String);
            Assert.AreEqual(t.ToString(), "UniversalGen.MyGen`1[System.String]");

            t = TypeOf.UG_MyGen.MakeGenericType(TypeOf.Int32);
            Assert.AreEqual(t.ToString(), "UniversalGen.MyGen`1[System.Int32]");

            t = TypeOf.UG_MyGen.MakeGenericType(TypeOf.CommonType2);
            Assert.AreEqual(t.ToString(), "UniversalGen.MyGen`1[CommonType2]");

            t = TypeOf.UG_MyGen.MakeGenericType(typeof(KeyValuePair<string, object>));
            Assert.AreEqual(t.ToString(), "UniversalGen.MyGen`1[System.Collections.Generic.KeyValuePair`2[System.String,System.Object]]");

            // List test
            {
                t = typeof(MyDerivedList<>).MakeGenericType(TypeOf.UG_MyListItem);
                Assert.AreEqual(t.ToString(), "UniversalGen.MyDerivedList`1[UniversalGen.MyListItem]");

                var l = Activator.CreateInstance(t);

                MethodInfo ListTest = typeof(Test).GetTypeInfo().GetDeclaredMethod("ListTest").MakeGenericMethod(TypeOf.UG_MyListItem);
                ListTest.Invoke(null, new object[] { l, new MyListItem("a"), new MyListItem("b") });
            }
        }

        public static void ListTest<T>(MyDerivedList<T> l, T t1, T t2)
        {
            l.Add(t1);
            l.Add(t2);
            l.Add(t1);
            Assert.AreEqual(3, l.Count);
            Assert.AreEqual("MyListItem(a)", l[0].ToString());
            Assert.AreEqual("MyListItem(b)", l[1].ToString());
            Assert.AreEqual("MyListItem(a)", l[2].ToString());
        }

        [TestMethod]
        public static void TestUSCInstanceFieldUsage()
        {
            var t = TypeOf.UG_UCGInstanceFields.MakeGenericType(TypeOf.Int32, TypeOf.Int32);
            TestFieldsBase o = (TestFieldsBase)Activator.CreateInstance(t);
            o.SetVal1(1);
            o.SetVal2(2);
            o.SetVal3(3);
            Assert.AreEqual(o.GetVal1(), 1);
            Assert.AreEqual(o.GetVal2(), 2);
            Assert.AreEqual(o.GetVal3(), 3);

            t = TypeOf.UG_UCGInstanceFieldsDerived.MakeGenericType(TypeOf.String, TypeOf.Int32);
            o = (TestFieldsBase)Activator.CreateInstance(t);
            o.SetVal1("11");
            o.SetVal2("22");
            o.SetVal3(33);
            o.SetVal4("44");
            o.SetVal5(55.55);
            o.SetVal6(66);
            Assert.AreEqual(o.GetVal1(), "11");
            Assert.AreEqual(o.GetVal2(), "22");
            Assert.AreEqual(o.GetVal3(), 33);
            Assert.AreEqual(o.GetVal4(), "44");
            Assert.AreEqual(o.GetVal5(), 55.55);
            Assert.AreEqual(o.GetVal6(), 66);

            t = TypeOf.UG_UCGInstanceFieldsMostDerived.MakeGenericType(TypeOf.Int32);
            o = (TestFieldsBase)Activator.CreateInstance(t);
            o.SetVal1(11.11f);
            o.SetVal2(22.22f);
            o.SetVal3(333);
            o.SetVal4(44.44f);
            o.SetVal5(555.555);
            o.SetVal6(666);
            Assert.AreEqual(o.GetVal1(), 11.11f);
            Assert.AreEqual(o.GetVal2(), 22.22f);
            Assert.AreEqual(o.GetVal3(), 333);
            Assert.AreEqual(o.GetVal4(), 44.44f);
            Assert.AreEqual(o.GetVal5(), 555.555);
            Assert.AreEqual(o.GetVal6(), 666);

            t = TypeOf.UG_UCGGenDerivedTypeActivator.MakeGenericType(typeof(MyGenStruct<double>));
            object o2 = Activator.CreateInstance(t);
            Assert.AreEqual(o2.ToString(), "GenDerivedType_Activator<UniversalGen.MyGenStruct`1[System.Double]>.FuncForDelegate");
        }

        [TestMethod]
        public static void TestUSCStaticFieldUsage()
        {
            // Test with primitive types as field types
            var t = TypeOf.UG_UCGStaticFields.MakeGenericType(TypeOf.Int32, TypeOf.Int32);
            TestFieldsBase o = (TestFieldsBase)Activator.CreateInstance(t);
            o.SetVal1(4);
            o.SetVal2(5);
            o.SetVal3(6);
            Assert.AreEqual(o.GetVal1(), 4);
            Assert.AreEqual(o.GetVal2(), 5);
            Assert.AreEqual(o.GetVal3(), 6);

            // Test with valuetypes as field types
            t = TypeOf.UG_UCGStaticFields.MakeGenericType(TypeOf.UG_UCGWrapperStruct, TypeOf.UG_UCGWrapperStruct);
            o = (TestFieldsBase)Activator.CreateInstance(t);
            o.SetVal1(new UCGWrapperStruct(7));
            o.SetVal2(new UCGWrapperStruct(8));
            o.SetVal3(9);
            Assert.AreEqual(((UCGWrapperStruct)o.GetVal1())._WrappedValue, 7);
            Assert.AreEqual(((UCGWrapperStruct)o.GetVal2())._WrappedValue, 8);
            Assert.AreEqual(o.GetVal3(), 9);
        }

        [TestMethod]
        public static void TestUSCThreadStaticFieldUsage()
        {
            // Test with primitive types as field types
            var t = TypeOf.UG_UCGThreadStaticFields.MakeGenericType(TypeOf.Int32, TypeOf.Int32);
            TestFieldsBase o = (TestFieldsBase)Activator.CreateInstance(t);
            o.SetVal1(16);
            o.SetVal2(17);
            o.SetVal3(18);
            Assert.AreEqual(o.GetVal1(), 16);
            Assert.AreEqual(o.GetVal2(), 17);
            Assert.AreEqual(o.GetVal3(), 18);

            // Test with valuetypes as field types
            t = TypeOf.UG_UCGThreadStaticFields.MakeGenericType(TypeOf.UG_UCGWrapperStruct, TypeOf.UG_UCGWrapperStruct);
            o = (TestFieldsBase)Activator.CreateInstance(t);
            o.SetVal1(new UCGWrapperStruct(19));
            o.SetVal2(new UCGWrapperStruct(20));
            o.SetVal3(21);
            Assert.AreEqual(((UCGWrapperStruct)o.GetVal1())._WrappedValue, 19);
            Assert.AreEqual(((UCGWrapperStruct)o.GetVal2())._WrappedValue, 20);
            Assert.AreEqual(o.GetVal3(), 21);
        }

        // Test that static layout is compatible between universal shared generics, and normal generics
        [TestMethod]
        public static void TestUSCStaticFieldLayoutCompat()
        {
            // In this test, we set values using static code, and the universal shared generic is supposed to read from
            // the same static variables

            // Test with primitive types as field types
            TestFieldsBase o = new UCGStaticFieldsLayoutCompatStatic<int, int>();
            o.SetVal1(10);
            o.SetVal2(11);
            o.SetVal3(12);
            var t = TypeOf.UG_UCGStaticFieldsLayoutCompatDynamic.MakeGenericType(TypeOf.Int32, TypeOf.Int32);
            o = (TestFieldsBase)Activator.CreateInstance(t);
            Assert.AreEqual(o.GetVal1(), 10);
            Assert.AreEqual(o.GetVal2(), 11);
            Assert.AreEqual(o.GetVal3(), 12);

            // Test with valuetypes as field types
            o = new UCGStaticFieldsLayoutCompatStatic<UCGWrapperStruct, UCGWrapperStruct>();
            o.SetVal1(new UCGWrapperStruct(13));
            o.SetVal2(new UCGWrapperStruct(14));
            o.SetVal3(15);
            t = TypeOf.UG_UCGStaticFieldsLayoutCompatDynamic.MakeGenericType(TypeOf.UG_UCGWrapperStruct, TypeOf.UG_UCGWrapperStruct);
            o = (TestFieldsBase)Activator.CreateInstance(t);
            Assert.AreEqual(((UCGWrapperStruct)o.GetVal1())._WrappedValue, 13);
            Assert.AreEqual(((UCGWrapperStruct)o.GetVal2())._WrappedValue, 14);
            Assert.AreEqual(o.GetVal3(), 15);
        }

        // Test class constructor implicit call
        [TestMethod]
        public static void TestUSCClassConstructorImplicit()
        {
            try
            {
                Assert.AreEqual(null, TestClassConstructorBase.s_cctorOutput);
                var t = TypeOf.UG_UCGClassConstructorType.MakeGenericType(TypeOf.Int16);
                var o = (TestClassConstructorBase)Activator.CreateInstance(t);
                Assert.AreEqual(true, o.QueryStatic());
                Assert.AreEqual(t, TestClassConstructorBase.s_cctorOutput);
            }
            finally
            {
                TestClassConstructorBase.s_cctorOutput = null;
            }
        }

        // Test class constructor explicit call
        [TestMethod]
        public static void TestUSCClassConstructorExplicit()
        {
            try
            {
                Assert.AreEqual(null, TestClassConstructorBase.s_cctorOutput);
                var t = TypeOf.UG_UCGClassConstructorType.MakeGenericType(TypeOf.Int32);
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);
                Assert.AreEqual(t, TestClassConstructorBase.s_cctorOutput);
            }
            finally
            {
                TestClassConstructorBase.s_cctorOutput = null;
            }
        }

        [TestMethod]
        public static void TestUniversalGenericsGvmCall()
        {
            var t = TypeOf.UG_MakeGVMCall.MakeGenericType(TypeOf.Int16);
            var o = (MakeGVMCallBase)Activator.CreateInstance(t);

            var tTestClassType = TypeOf.UG_GVMTestClass.MakeGenericType(TypeOf.Int32);
            IGVMTest testClass = (IGVMTest)Activator.CreateInstance(tTestClassType);

            Assert.AreEqual(TypeOf.Int32.ToString() + TypeOf.Int16.ToString(), o.CallGvm(testClass));

            var tTestStructType = TypeOf.UG_GVMTestClass.MakeGenericType(TypeOf.Double);
            IGVMTest testStruct = (IGVMTest)Activator.CreateInstance(tTestStructType);

            Assert.AreEqual(TypeOf.Double.ToString() + TypeOf.Int16.ToString(), o.CallGvm(testStruct));
        }
    }
}

namespace PartialUSC
{
    public class TestVirtualCallsBase
    {
        public virtual void TestVirtualCall0(object o) { }
        public virtual void TestVirtualCall1(object o, string instTypeName) { }
        public virtual void TestVirtualCall2(object o, string instTypeName) { }
        public virtual void TestVirtualCall3(object o, bool TAndUAreTheSame, string instTypeName) { }
        public virtual void TestVirtualCall4(object o, string instTypeName) { }
    }

    public class UCGTestVirtualCalls<T, U> : TestVirtualCallsBase
    {
        public override void TestVirtualCall0(object o)
        {
            VerifyCallResult(((Base<T>)o).Method1(), "Base<" + typeof(T) + ">.Method1");
            VerifyCallResult(((Base<T>)o).Method2(), "Base<" + typeof(T) + ">.Method2");
            VerifyCallResult(((Base<T>)o).Method3(), "Base<" + typeof(T) + ">.Method3");
        }
        public override void TestVirtualCall1(object o, string instTypeName)
        {
            VerifyCallResult(((IBase<T, U>)o).Method4(), instTypeName + ".Method4");
            VerifyCallResult(((IBase<T, U>)o).Method5(), instTypeName + ".Method5");
            VerifyCallResult(((IBase<T, U>)o).Method6(), instTypeName + ".Method6");
        }
        public override void TestVirtualCall2(object o, string instTypeName)
        {
            VerifyCallResult(((Derived<T, U>)o).Method1(), instTypeName + ".Method1");
            VerifyCallResult(((Derived<T, U>)o).Method2(), instTypeName + ".Method2");
            VerifyCallResult(((Derived<T, U>)o).Method3(), instTypeName + ".Method3");
            VerifyCallResult(((Derived<T, U>)o).Method4(), instTypeName + ".Method4");
            VerifyCallResult(((Derived<T, U>)o).Method5(), instTypeName + ".Method5");
            VerifyCallResult(((Derived<T, U>)o).Method6(), instTypeName + ".Method6");
        }
        public override void TestVirtualCall3(object o, bool TAndUAreTheSame, string instTypeName)
        {
            if (TAndUAreTheSame)
            {
                VerifyCallResult(((Derived2<T, T>)o).Method1(), instTypeName + ".Method1");
                VerifyCallResult(((Derived2<T, T>)o).Method2(), instTypeName + ".Method2");
                VerifyCallResult(((Derived2<T, T>)o).Method3(), instTypeName + ".Method3");
                VerifyCallResult(((Derived2<T, T>)o).Method4(), instTypeName + ".Method4");
                VerifyCallResult(((Derived2<T, T>)o).Method5(), instTypeName + ".Method5");
                VerifyCallResult(((Derived2<T, T>)o).Method6(), instTypeName + ".Method6");
            }
            else
            {
                VerifyCallResult(((Derived2<T, U>)o).Method1(), instTypeName + ".Method1");
                VerifyCallResult(((Derived2<T, U>)o).Method2(), instTypeName + ".Method2");
                VerifyCallResult(((Derived2<T, U>)o).Method3(), instTypeName + ".Method3");
                VerifyCallResult(((Derived2<T, U>)o).Method4(), instTypeName + ".Method4");
                VerifyCallResult(((Derived2<T, U>)o).Method5(), instTypeName + ".Method5");
                VerifyCallResult(((Derived2<T, U>)o).Method6(), instTypeName + ".Method6");
            }
        }
        public override void TestVirtualCall4(object o, string instTypeName)
        {
            ((Derived3<T, T>)o).Method1();
            ((Derived3<T, T>)o).Method2();
            ((Derived3<T, T>)o).Method3();
            ((Derived3<T, T>)o).Method4();
            ((Derived3<T, T>)o).Method5();
            ((Derived3<T, T>)o).Method6();
        }

        private void VerifyCallResult(string result, string expected)
        {
            Assert.AreEqual(result, expected);
        }
    }

#pragma warning disable 0114
    public class Base<T>
    {
        public virtual string Method1() { return "Base<" + typeof(T) + ">.Method1"; }
        public virtual string Method2() { return "Base<" + typeof(T) + ">.Method2"; }
        public virtual string Method3() { return "Base<" + typeof(T) + ">.Method3"; }
    }
    public interface IBase<T, U>
    {
        string Method4();
        string Method5();
        string Method6();
    }
    public class Derived<T, U> : Base<T>, IBase<T, U>
    {
        public virtual string Method1() { return "Derived<" + typeof(T) + "," + typeof(U) + ">.Method1"; }
        public virtual string Method2() { return "Derived<" + typeof(T) + "," + typeof(U) + ">.Method2"; }
        public virtual string Method3() { return "Derived<" + typeof(T) + "," + typeof(U) + ">.Method3"; }
        public virtual string Method4() { return "Derived<" + typeof(T) + "," + typeof(U) + ">.Method4"; }
        public virtual string Method5() { return "Derived<" + typeof(T) + "," + typeof(U) + ">.Method5"; }
        public virtual string Method6() { return "Derived<" + typeof(T) + "," + typeof(U) + ">.Method6"; }
    }
    public class Derived2<T, U> : Derived<T, int>
    {
        public override string Method1() { return "Derived2<" + typeof(T) + "," + typeof(U) + ">.Method1"; }
        public override string Method2() { return "Derived2<" + typeof(T) + "," + typeof(U) + ">.Method2"; }
        public override string Method3() { return "Derived2<" + typeof(T) + "," + typeof(U) + ">.Method3"; }
        public override string Method4() { return "Derived2<" + typeof(T) + "," + typeof(U) + ">.Method4"; }
        public override string Method5() { return "Derived2<" + typeof(T) + "," + typeof(U) + ">.Method5"; }
        public override string Method6() { return "Derived2<" + typeof(T) + "," + typeof(U) + ">.Method6"; }
    }
    public class Derived3<T, U> : Derived2<T, Type>
    {
        public virtual string Method1() { return "Derived3<" + typeof(T) + "," + typeof(U) + ">.Method1"; }
        public virtual string Method2() { return "Derived3<" + typeof(T) + "," + typeof(U) + ">.Method2"; }
        public virtual string Method3() { return "Derived3<" + typeof(T) + "," + typeof(U) + ">.Method3"; }
        public virtual string Method4() { return "Derived3<" + typeof(T) + "," + typeof(U) + ">.Method4"; }
        public virtual string Method5() { return "Derived3<" + typeof(T) + "," + typeof(U) + ">.Method5"; }
        public virtual string Method6() { return "Derived3<" + typeof(T) + "," + typeof(U) + ">.Method6"; }
    }
#pragma warning restore 114

    public struct MyStruct<T, U> { }

    public class TestRelatedTypeCases { public virtual void DoTest() { } }

    public class NullableCaseTest<T> : TestRelatedTypeCases
    {
        public override void DoTest()
        {
            MyStruct<T, T>? nullable = null;
            Assert.IsFalse(nullable.HasValue);
            nullable = new MyStruct<T, T>();
            Assert.IsTrue(nullable.Value.Equals(default(MyStruct<T, T>)));
            Assert.IsTrue(nullable.GetType().ToString() == "Nullable<MyStruct<" + typeof(T) + "," + typeof(T) + ">>");

            MyStruct<T, int>? nullable2 = null;
            Assert.IsFalse(nullable2.HasValue);
            nullable2 = new MyStruct<T, int>();
            Assert.IsTrue(nullable2.Value.Equals(default(MyStruct<T, int>)));
            Assert.IsTrue(nullable2.GetType().ToString() == "Nullable<MyStruct<" + typeof(T) + ",System.Int32>>");
        }
    }
    public class ArrayCaseTest<T> : TestRelatedTypeCases
    {
        public override void DoTest()
        {
            MyStruct<T, T>[] arr1 = new MyStruct<T, T>[] { };
            MyStruct<T, long>[] arr2 = new MyStruct<T, long>[] { };
            MyStruct<object, T>[] arr3 = new MyStruct<object, T>[] { };
        }
    }

    public class CustomCollection<V> : System.Collections.ObjectModel.KeyedCollection<V, System.Collections.DictionaryEntry>
    {
        public CustomCollection()
        {
        }
        public CustomCollection(System.Collections.Generic.IEqualityComparer<V> comparer)
            : base(comparer)
        {
        }
        protected override V GetKeyForItem(System.Collections.DictionaryEntry entry)
        {
            return (V)((object)entry.Key);
        }
    }

    public class Test
    {
        [TestMethod]
        public static void TestVirtualCallsPartialUSGVTableMismatch()
        {
            Type collectionType = typeof(CustomCollection<>).MakeGenericType(TypeOf.Short);
            var collection = Activator.CreateInstance(collectionType);

            MethodInfo GetKeyForItem = collectionType.GetTypeInfo().GetDeclaredMethod("GetKeyForItem");
            short result = (short)GetKeyForItem.Invoke(collection, new object[] { new DictionaryEntry((short)123, "abc") });

            Assert.IsTrue(result == (short)123);
        }

        [TestMethod]
        public static void TestVirtualCalls()
        {
            for (int i = 0; i < 10; i++)
            {
                foreach (var typeArg in new Type[] { TypeOf.Short, TypeOf.String, TypeOf.CommonType1, typeof(List<int>) })
                {
                    {
                        var t = TypeOf.PCT_UCGTestVirtualCalls.MakeGenericType(typeArg, typeArg);
                        TestVirtualCallsBase caller = (TestVirtualCallsBase)Activator.CreateInstance(t);

                        var obj = Activator.CreateInstance(TypeOf.PCT_Derived.MakeGenericType(typeArg, typeArg));
                        caller.TestVirtualCall0(obj);
                        caller.TestVirtualCall1(obj, "Derived<" + typeArg + "," + typeArg + ">");
                        caller.TestVirtualCall2(obj, "Derived<" + typeArg + "," + typeArg + ">");

                        try { caller.TestVirtualCall3(obj, true, ""); Assert.IsTrue(false); }
                        catch (InvalidCastException) { }
                        try { caller.TestVirtualCall3(obj, false, ""); Assert.IsTrue(false); }
                        catch (InvalidCastException) { }

                        try { caller.TestVirtualCall4(obj, ""); Assert.IsTrue(false); }
                        catch (InvalidCastException) { }
                    }

                    {
                        var t = TypeOf.PCT_UCGTestVirtualCalls.MakeGenericType(typeArg, TypeOf.Int32);
                        TestVirtualCallsBase caller = (TestVirtualCallsBase)Activator.CreateInstance(t);

                        var obj = Activator.CreateInstance(TypeOf.PCT_Derived2.MakeGenericType(typeArg, typeArg));
                        caller.TestVirtualCall0(obj);
                        caller.TestVirtualCall1(obj, "Derived2<" + typeArg + "," + typeArg + ">");
                        caller.TestVirtualCall2(obj, "Derived2<" + typeArg + "," + typeArg + ">");

                        caller.TestVirtualCall3(obj, true, "Derived2<" + typeArg + "," + typeArg + ">");
                        try { caller.TestVirtualCall3(obj, false, ""); Assert.IsTrue(false); }
                        catch (InvalidCastException) { }

                        try { caller.TestVirtualCall4(obj, ""); Assert.IsTrue(false); }
                        catch (InvalidCastException) { }
                    }

                    {
                        var t_int = TypeOf.PCT_UCGTestVirtualCalls.MakeGenericType(typeArg, TypeOf.Int32);
                        var t_type = TypeOf.PCT_UCGTestVirtualCalls.MakeGenericType(typeArg, TypeOf.Type);
                        TestVirtualCallsBase caller_int = (TestVirtualCallsBase)Activator.CreateInstance(t_int);
                        TestVirtualCallsBase caller_type = (TestVirtualCallsBase)Activator.CreateInstance(t_type);

                        var obj = Activator.CreateInstance(TypeOf.PCT_Derived3.MakeGenericType(typeArg, typeArg));
                        caller_int.TestVirtualCall0(obj);
                        caller_type.TestVirtualCall0(obj);

                        caller_int.TestVirtualCall1(obj, "Derived2<" + typeArg + "," + TypeOf.Type + ">");
                        try { caller_type.TestVirtualCall1(obj, ""); Assert.IsTrue(false); }
                        catch (InvalidCastException) { }

                        caller_int.TestVirtualCall2(obj, "Derived2<" + typeArg + "," + TypeOf.Type + ">");
                        try { caller_type.TestVirtualCall2(obj, ""); Assert.IsTrue(false); }
                        catch (InvalidCastException) { }

                        caller_type.TestVirtualCall3(obj, false, "Derived2<" + typeArg + "," + TypeOf.Type + ">");
                        try { caller_type.TestVirtualCall3(obj, true, ""); Assert.IsTrue(false); }
                        catch (InvalidCastException) { }
                        try { caller_int.TestVirtualCall3(obj, false, ""); Assert.IsTrue(false); }
                        catch (InvalidCastException) { }
                        try { caller_int.TestVirtualCall3(obj, true, ""); Assert.IsTrue(false); }
                        catch (InvalidCastException) { }

                        caller_int.TestVirtualCall4(obj, "Derived3<" + typeArg + "," + typeArg + ">");
                        caller_type.TestVirtualCall4(obj, "Derived3<" + typeArg + "," + typeArg + ">");
                    }

#if false
                    // Bug with callsite address with the nullable test case...
                    {
                        var t = TypeOf.PCT_NullableCaseTest.MakeGenericType(typeArg);
                        TestRelatedTypeCases caller = (TestRelatedTypeCases)Activator.CreateInstance(t);
                        caller.DoTest();

                        t = TypeOf.PCT_ArrayCaseTest.MakeGenericType(typeArg);
                        caller = (TestRelatedTypeCases)Activator.CreateInstance(t);
                        caller.DoTest();
                    }
#endif
                }
            }
        }
    }
}

namespace VirtualCalls
{
    public class TestClass { }
    public struct TestStruct { }

    public class TestVirtualCallsBase
    {
        public virtual void TestVirtualCallNonGenericInstance(object o) { }
        public virtual void TestVirtualCallStaticallyCompiledGenericInstance(object o1, object o2) { }
        public virtual void TestUniversalGenericMethodCallOnNonUniversalContainingType() { }
        public virtual void TestVirtualCall(object o, object value) { }
        public virtual void TestVirtualCallUsingDelegates(object o, object value)
        {
            // TODO
            // Not working today. Requires the following:
            //  1)  Fix instantiation problem with universal canonical types. Example: Foo<T, string>
            //      could create types like Foo<__UniversalCanon, string>, which are incorrect conceptually
            //  2)  Fix NUTC to emit the LOAD_VIRTUAL_FUNCTION opcode for delegates using runtime determined tokens
            //      like Foo<!0>.Method instead of Foo<__UC>.Method (otherwise binder fails with an assert)
        }
    }

    public class UCGTestVirtualCalls<T> : TestVirtualCallsBase
    {
        public override void TestVirtualCallNonGenericInstance(object o)
        {
            NonGenBase obj = (NonGenBase)o;
            Func<string> del1 = obj.Method1;
            Func<string> del2 = obj.Method2;
            Func<string> del3 = obj.Method3;

            if (o.GetType() == typeof(NonGenBase))
            {
                Assert.AreEqual(obj.Method1(), "NonGenBase.Method1");
                Assert.AreEqual(obj.Method2(), "NonGenBase.Method2");
                Assert.AreEqual(obj.Method3(), "NonGenBase.Method3");

                Assert.AreEqual(del1(), "NonGenBase.Method1");
                Assert.AreEqual(del2(), "NonGenBase.Method2");
                Assert.AreEqual(del3(), "NonGenBase.Method3");
            }
            else
            {
                Assert.AreEqual(obj.Method1(), "NonGenDerived.Method1");
                Assert.AreEqual(obj.Method2(), "NonGenDerived.Method2");
                Assert.AreEqual(obj.Method3(), "NonGenDerived.Method3");

                Assert.AreEqual(del1(), "NonGenDerived.Method1");
                Assert.AreEqual(del2(), "NonGenDerived.Method2");
                Assert.AreEqual(del3(), "NonGenDerived.Method3");
            }
        }

        public override void TestVirtualCallStaticallyCompiledGenericInstance(object o1, object o2)
        {
            string result;

            // Static non-shareable instantiation
            result = ((Base<double>)o1).Method1(1.23);
            VerifyCallResult<double>(result, "Method1", o1);
            result = ((Base<double>)o1).Method2(1.23);
            VerifyCallResult<double>(result, "Method2", o1);
            result = ((Base<double>)o1).Method3(1.23);
            VerifyCallResult<double>(result, "Method3", o1);
            result = ((Derived<double>)o1).Method1(1.23);
            VerifyCallResult<double>(result, "Method1", o1);
            result = ((Derived<double>)o1).Method2(1.23);
            VerifyCallResult<double>(result, "Method2", o1);
            result = ((Derived<double>)o1).Method3(1.23);
            VerifyCallResult<double>(result, "Method3", o1);
            result = ((Derived<double>)o1).Method4(1.23);
            VerifyCallResult<double>(result, "Method4", o1);
            result = ((Derived<double>)o1).Method5(1.23);
            VerifyCallResult<double>(result, "Method5", o1);
            result = ((Derived<double>)o1).Method6(1.23);
            VerifyCallResult<double>(result, "Method6", o1);

            // Static normal canonical-shareable instantiation
            result = ((Base<Type>)o2).Method1(this.GetType());
            VerifyCallResult<Type>(result, "Method1", o2);
            result = ((Base<Type>)o2).Method2(this.GetType());
            VerifyCallResult<Type>(result, "Method2", o2);
            result = ((Base<Type>)o2).Method3(this.GetType());
            VerifyCallResult<Type>(result, "Method3", o2);
            result = ((Derived<Type>)o2).Method1(this.GetType());
            VerifyCallResult<Type>(result, "Method1", o2);
            result = ((Derived<Type>)o2).Method2(this.GetType());
            VerifyCallResult<Type>(result, "Method2", o2);
            result = ((Derived<Type>)o2).Method3(this.GetType());
            VerifyCallResult<Type>(result, "Method3", o2);
            result = ((Derived<Type>)o2).Method4(this.GetType());
            VerifyCallResult<Type>(result, "Method4", o2);
            result = ((Derived<Type>)o2).Method5(this.GetType());
            VerifyCallResult<Type>(result, "Method5", o2);
            result = ((Derived<Type>)o2).Method6(this.GetType());
            VerifyCallResult<Type>(result, "Method6", o2);
        }

        public override void TestUniversalGenericMethodCallOnNonUniversalContainingType()
        {
            string result;

            var obj1 = new UCGTestVirtualCalls<TestClass>();
            result = obj1.GenericMethod<T>();
            Assert.AreEqual(result, "UCGTestVirtualCalls<TestClass>.GenericMethod<" + typeof(T).Name + ">");
            result = UCGTestVirtualCalls<TestClass>.StaticGenericMethod<T>();
            Assert.AreEqual(result, "UCGTestVirtualCalls<TestClass>.StaticGenericMethod<" + typeof(T).Name + ">");

            var obj2 = new UCGTestVirtualCalls<TestStruct>();
            result = obj2.GenericMethod<T>();
            Assert.AreEqual(result, "UCGTestVirtualCalls<TestStruct>.GenericMethod<" + typeof(T).Name + ">");
            result = UCGTestVirtualCalls<TestStruct>.StaticGenericMethod<T>();
            Assert.AreEqual(result, "UCGTestVirtualCalls<TestStruct>.StaticGenericMethod<" + typeof(T).Name + ">");
        }
        string GenericMethod<ARG>() { return "UCGTestVirtualCalls<" + typeof(T).Name + ">.GenericMethod<" + typeof(ARG).Name + ">"; }
        static string StaticGenericMethod<ARG>() { return "UCGTestVirtualCalls<" + typeof(T).Name + ">.StaticGenericMethod<" + typeof(ARG).Name + ">"; }

        public override void TestVirtualCall(object o, object value)
        {
            string result;

            result = ((Base<T>)o).Method1((T)value);
            VerifyCallResult<T>(result, "Method1", o);
            result = ((Base<T>)o).Method2((T)value);
            VerifyCallResult<T>(result, "Method2", o);
            result = ((Base<T>)o).Method3((T)value);
            VerifyCallResult<T>(result, "Method3", o);
            result = ((Derived<T>)o).Method1((T)value);
            VerifyCallResult<T>(result, "Method1", o);
            result = ((Derived<T>)o).Method2((T)value);
            VerifyCallResult<T>(result, "Method2", o);
            result = ((Derived<T>)o).Method3((T)value);
            VerifyCallResult<T>(result, "Method3", o);
            result = ((Derived<T>)o).Method4((T)value);
            VerifyCallResult<T>(result, "Method4", o);
            result = ((Derived<T>)o).Method5((T)value);
            VerifyCallResult<T>(result, "Method5", o);
            result = ((Derived<T>)o).Method6((T)value);
            VerifyCallResult<T>(result, "Method6", o);
            result = ((Base<T>)o).Method1(default(T));
            VerifyCallResult<T>(result, "Method1", o);
            result = ((Base<T>)o).Method2(default(T));
            VerifyCallResult<T>(result, "Method2", o);
            result = ((Base<T>)o).Method3(default(T));
            VerifyCallResult<T>(result, "Method3", o);
            result = ((Derived<T>)o).Method1(default(T));
            VerifyCallResult<T>(result, "Method1", o);
            result = ((Derived<T>)o).Method2(default(T));
            VerifyCallResult<T>(result, "Method2", o);
            result = ((Derived<T>)o).Method3(default(T));
            VerifyCallResult<T>(result, "Method3", o);
            result = ((Derived<T>)o).Method4(default(T));
            VerifyCallResult<T>(result, "Method4", o);
            result = ((Derived<T>)o).Method5(default(T));
            VerifyCallResult<T>(result, "Method5", o);
            result = ((Derived<T>)o).Method6(default(T));
            VerifyCallResult<T>(result, "Method6", o);
        }

        void VerifyCallResult<ARG>(string result, string methodName, object o)
        {
            string expected = "";
            if (o.GetType().ToString().Contains("Derived_NoOverride"))
            {
                if (methodName == "Method1" || methodName == "Method2" || methodName == "Method3")
                    expected = "Base<" + typeof(ARG) + ">." + methodName;
                else
                    expected = "Derived<" + typeof(ARG) + ">." + methodName;
            }
            else expected = "Derived_WithOverride<" + typeof(ARG) + ">." + methodName;

            Assert.AreEqual(result, expected);
        }
    }

    public class NonGenBase
    {
        public virtual string Method1() { return "NonGenBase.Method1"; }
        public virtual string Method2() { return "NonGenBase.Method2"; }
        public virtual string Method3() { return "NonGenBase.Method3"; }
    }
    public class NonGenDerived : NonGenBase
    {
        public override string Method1() { return "NonGenDerived.Method1"; }
        public override string Method2() { return "NonGenDerived.Method2"; }
        public override string Method3() { return "NonGenDerived.Method3"; }
    }

    public class Base<T>
    {
        public virtual string Method1(T t) { return "Base<" + typeof(T) + ">.Method1"; }
        public virtual string Method2(T t) { return "Base<" + typeof(T) + ">.Method2"; }
        public virtual string Method3(T t) { return "Base<" + typeof(T) + ">.Method3"; }
    }
    public class Derived<T> : Base<T>
    {
        public virtual string Method4(T t) { return "Derived<" + typeof(T) + ">.Method4"; }
        public virtual string Method5(T t) { return "Derived<" + typeof(T) + ">.Method5"; }
        public virtual string Method6(T t) { return "Derived<" + typeof(T) + ">.Method6"; }
    }
#pragma warning disable 114     // 'method' hides inherited member 'method'
    public class Derived_NoOverride<T> : Derived<T>
    {
        public virtual string Method1(T t) { return "Derived_NoOverride<" + typeof(T) + ">.Method1"; }
        public virtual string Method2(T t) { return "Derived_NoOverride<" + typeof(T) + ">.Method2"; }
        public virtual string Method3(T t) { return "Derived_NoOverride<" + typeof(T) + ">.Method3"; }
        public virtual string Method4(T t) { return "Derived_NoOverride<" + typeof(T) + ">.Method4"; }
        public virtual string Method5(T t) { return "Derived_NoOverride<" + typeof(T) + ">.Method5"; }
        public virtual string Method6(T t) { return "Derived_NoOverride<" + typeof(T) + ">.Method6"; }
    }
#pragma warning restore 114
    public class Derived_WithOverride<T> : Derived<T>
    {
        public override string Method1(T t) { return "Derived_WithOverride<" + typeof(T) + ">.Method1"; }
        public override string Method2(T t) { return "Derived_WithOverride<" + typeof(T) + ">.Method2"; }
        public override string Method3(T t) { return "Derived_WithOverride<" + typeof(T) + ">.Method3"; }
        public override string Method4(T t) { return "Derived_WithOverride<" + typeof(T) + ">.Method4"; }
        public override string Method5(T t) { return "Derived_WithOverride<" + typeof(T) + ">.Method5"; }
        public override string Method6(T t) { return "Derived_WithOverride<" + typeof(T) + ">.Method6"; }
    }

    public class Test
    {
        [TestMethod]
        public static void TestVirtualCalls()
        {
            var rootingStaticInstantiation1 = new UCGTestVirtualCalls<long>();
            var rootingStaticInstantiation2 = new UCGTestVirtualCalls<List<int>>();

            for (int i = 0; i < 10; i++)
            {
                foreach (var typeArg in new Type[] { TypeOf.Short, TypeOf.String, TypeOf.Long, typeof(List<int>) })
                {
                    object value = null;
                    if (typeArg == TypeOf.Short)
                        value = (short)123;
                    else if (typeArg == TypeOf.String)
                        value = "456";
                    else if (typeArg == TypeOf.Long)
                        value = (long)789;
                    else if (typeArg == typeof(List<int>))
                        value = new List<int>();

                    var t = TypeOf.VCT_UCGTestVirtualCalls.MakeGenericType(typeArg);
                    TestVirtualCallsBase caller = (TestVirtualCallsBase)Activator.CreateInstance(t);

                    var obj = Activator.CreateInstance(TypeOf.VCT_Derived_NoOverride.MakeGenericType(typeArg));
                    caller.TestVirtualCall(obj, value);
                    caller.TestVirtualCallUsingDelegates(obj, value);
                    caller.TestVirtualCallNonGenericInstance(new NonGenBase());
                    caller.TestVirtualCallStaticallyCompiledGenericInstance(new Derived_NoOverride<double>(), new Derived_NoOverride<Type>());
                    caller.TestUniversalGenericMethodCallOnNonUniversalContainingType();

                    obj = Activator.CreateInstance(TypeOf.VCT_Derived_WithOverride.MakeGenericType(typeArg));
                    caller.TestVirtualCall(obj, value);
                    caller.TestVirtualCallUsingDelegates(obj, value);
                    caller.TestVirtualCallNonGenericInstance(new NonGenDerived());
                    caller.TestVirtualCallStaticallyCompiledGenericInstance(new Derived_WithOverride<double>(), new Derived_WithOverride<Type>());
                    caller.TestUniversalGenericMethodCallOnNonUniversalContainingType();
                }
            }
        }
    }
}

namespace CallingConvention
{
    public class Foo<T>
    {
        public int _value;
    }
    public struct Bar<T>
    {
        public int _value;
    }

    public unsafe interface IBase<T>
    {
        void SimpleFunc(T tval);
        T VirtualCoolFunc(object o, Bar<T> b, Foo<T> f, T[] a, T t_val, ref T brt_val, ref T brt_default, int index, int* ptr, int** ptrptr, ref int* brptr);
    }
    public unsafe class CCTester<T> : IBase<T>
    {
        public virtual void SimpleFunc(T tval)
        {
            Assert.AreEqual(tval.ToString(), Test.s_expectedT.ToString());
        }

        public virtual T VirtualCoolFunc(object o, Bar<T> b, Foo<T> f, T[] a, T t_val, ref T brt_val, ref T brt_default, int index, int* ptr, int** ptrptr, ref int* brptr)
        {
            Assert.AreEqual(o, "Hello");
            Assert.AreEqual(b._value, 1);
            Assert.AreEqual(f._value, 2);
            Assert.AreEqual(a[0], brt_val);
            Assert.AreEqual(brt_default, default(T));
            Assert.AreEqual(index, 123);
            Assert.IsTrue(ptr == ((int*)456));
            Assert.IsTrue(ptrptr == ((int**)789));
            Assert.IsTrue(brptr == ((int*)159));

            brt_val = brt_default;
            brt_default = t_val;
            brptr = ptr;

            return t_val;
        }
    }

    public interface ITestCallInterface<T>
    {
        void M(T t);
    }

    public class TestCallInterfaceImpl : ITestCallInterface<short>
    {
        public void M(short s)
        {
            TestInterfaceUseBase.ShortValue = s;
            Console.WriteLine("In M(" + s + ")");
        }
    }

    public class TestInterfaceUseBase
    {
        public static short ShortValue;

        public virtual void TestUseInterface(object o, object value) { }
    }

    public class UCGTestUseInterface<T> : TestInterfaceUseBase
    {
        public override void TestUseInterface(object o, object value)
        {
            for (int i = 0; i < 10; i++)
            {
                ((ITestCallInterface<T>)o).M((T)value);
            }
        }
    }

    public class TestNonVirtualFunctionCallUseBase
    {
        public virtual object TestNonVirtualInstanceFunction(object o, object value) { return null; }
    }

    public class UCGSeparateClass<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public object BoxParam(T t)
        {
            return (object)t;
        }
    }

    public class UCGTestNonVirtualFunctionCallUse<T> : TestNonVirtualFunctionCallUseBase
    {
        public override object TestNonVirtualInstanceFunction(object o, object value)
        {
            return ((UCGSeparateClass<T>)o).BoxParam((T)value);
        }
    }

    #region Calling convention converter test with structs of known/unknown sizes
    public class EmptyClass<T>
    {
        public override string ToString() { return "EmptyClass<" + typeof(T).Name + ">"; }
    }

    public class ClassWithTField<T>
    {
        T _field1;

        public ClassWithTField(T f) { _field1 = f; }
        public override string ToString() { return "ClassWithTField<" + typeof(T).Name + ">. _field1 = {" + _field1.ToString() + "}"; }
    }
    public struct StructWithTField<T>
    {
        T _field1;

        public StructWithTField(T f) { _field1 = f; }
        public override string ToString() { return "StructWithTField<" + typeof(T).Name + ">. _field1 = {" + _field1.ToString() + "}"; }
    }

    public struct TestStructKnownSize<T>
    {
        EmptyClass<T> _field1;
        ClassWithTField<T> _field2;

        internal TestStructKnownSize(EmptyClass<T> l, T tValue) { _field1 = l; _field2 = new ClassWithTField<T>(tValue); }
        public override string ToString() { return "TestStructKnownSize<" + typeof(T).Name + ">. _field1 = {" + _field1.ToString() + "}. _field2 = {" + _field2.ToString() + "}"; }
    }
    public struct TestStructIndeterminateSize<T>
    {
        EmptyClass<T> _field1;
        StructWithTField<T> _field2;

        internal TestStructIndeterminateSize(EmptyClass<T> l, T tValue) { _field1 = l; _field2 = new StructWithTField<T>(tValue); }
        public override string ToString() { return "TestStructIndeterminateSize<" + typeof(T).Name + ">. _field1 = {" + _field1.ToString() + "}. _field2 = {" + _field2.ToString() + "}"; }
    }

    public struct StructWithOneField1<T>
    {
        MainClass<T, T> _field1;
        internal StructWithOneField1(MainClass<T, T> f) { _field1 = f; }
        public override string ToString() { return "StructWithOneField1<" + typeof(T).Name + ">. _field1 = {" + _field1.ToString() + "}"; }
    }
    public struct StructWithOneField2<T>
    {
        ClassWithTField<T> _field1;
        internal StructWithOneField2(ClassWithTField<T> f) { _field1 = f; }
        public override string ToString() { return "StructWithOneField2<" + typeof(T).Name + ">. _field1 = {" + _field1.ToString() + "}"; }
    }
    public struct StructWithOneField3<T>
    {
        StructWithTField<T> _field1;
        internal StructWithOneField3(StructWithTField<T> f) { _field1 = f; }
        public override string ToString() { return "StructWithOneField3<" + typeof(T).Name + ">. _field1 = {" + _field1.ToString() + "}"; }
    }
    public struct StructWithTwoFields1<T>
    {
        MainClass<T, T> _field1;
        TestStructKnownSize<T> _field2;

        internal StructWithTwoFields1(MainClass<T, T> f, T tValue) { _field1 = f; _field2 = new TestStructKnownSize<T>(new EmptyClass<T>(), tValue); }
        public override string ToString() { return "StructWithTwoFields1<" + typeof(T).Name + ">. _field1 = {" + _field1.ToString() + "}. _field2 = {" + _field2.ToString() + "}"; }
    }
    public struct StructWithTwoFields2<T>
    {
        MainClass<T, T> _field1;
        TestStructIndeterminateSize<T> _field2;

        internal StructWithTwoFields2(MainClass<T, T> f, T tValue) { _field1 = f; _field2 = new TestStructIndeterminateSize<T>(new EmptyClass<T>(), tValue); }
        public override string ToString() { return "StructWithTwoFields2<" + typeof(T).Name + ">. _field1 = {" + _field1.ToString() + "}. _field2 = {" + _field2.ToString() + "}"; }
    }

    public struct StructWithTwoGenParams<T, U>
    {
        T _field1;
        public StructWithTwoGenParams(T t) { _field1 = t; }
        public override string ToString() { return "StructWithTwoGenParams<" + typeof(T) + "," + typeof(U) + ">. _field1 = {" + _field1.ToString() + "}"; }
    }

    // No fields depending on type U in the following structs (on purpose)
    public struct StructWithOneFieldTwoGenParams1<T, U>
    {
        MainClass<T, T> _field1;
        internal StructWithOneFieldTwoGenParams1(MainClass<T, T> f) { _field1 = f; }
        public override string ToString() { return "StructWithOneFieldTwoGenParams1<" + typeof(T) + "," + typeof(U) +">. _field1 = {" + _field1.ToString() + "}"; }
    }
    public struct StructWithOneFieldTwoGenParams2<T, U>
    {
        ClassWithTField<T> _field1;
        internal StructWithOneFieldTwoGenParams2(ClassWithTField<T> f) { _field1 = f; }
        public override string ToString() { return "StructWithOneFieldTwoGenParams2<" + typeof(T) + "," + typeof(U) + ">. _field1 = {" + _field1.ToString() + "}"; }
    }
    public struct StructWithOneFieldTwoGenParams3<T, U>
    {
        StructWithTField<T> _field1;
        internal StructWithOneFieldTwoGenParams3(StructWithTField<T> f) { _field1 = f; }
        public override string ToString() { return "StructWithOneFieldTwoGenParams3<" + typeof(T) + "," + typeof(U) + ">. _field1 = {" + _field1.ToString() + "}"; }
    }
    public struct StructWithTwoFieldsTwoGenParams1<T, U>
    {
        MainClass<T, T> _field1;
        TestStructKnownSize<T> _field2;

        internal StructWithTwoFieldsTwoGenParams1(MainClass<T, T> f, T tValue) { _field1 = f; _field2 = new TestStructKnownSize<T>(new EmptyClass<T>(), tValue); }
        public override string ToString() { return "StructWithTwoFieldsTwoGenParams1<" + typeof(T) + "," + typeof(U) + ">. _field1 = {" + _field1.ToString() + "}. _field2 = {" + _field2.ToString() + "}"; }
    }
    public struct StructWithTwoFieldsTwoGenParams2<T, U>
    {
        MainClass<T, T> _field1;
        TestStructIndeterminateSize<T> _field2;

        internal StructWithTwoFieldsTwoGenParams2(MainClass<T, T> f, T tValue) { _field1 = f; _field2 = new TestStructIndeterminateSize<T>(new EmptyClass<T>(), tValue); }
        public override string ToString() { return "StructWithTwoFieldsTwoGenParams2<" + typeof(T) + "," + typeof(U) + ">. _field1 = {" + _field1.ToString() + "}. _field2 = {" + _field2.ToString() + "}"; }
    }

    public class MainClass<T, U>
    {
        string _id = null;
        T _tValue = default(T);
        static MainClass<T, T> _mainTT = null;

        public MainClass() { }
        private MainClass(string id, T tValue)
        {
            _id = id;
            _tValue = tValue;
        }

        private MainClass<T, T> GetMainTT()
        {
            if (_mainTT == null)
            {
                _mainTT = new MainClass<T, T>();
                _mainTT._id = this._id;
                _mainTT._tValue = this._tValue;
            }
            return _mainTT;
        }

        public void SetID(string id) { _id = id; }
        public void SetT(T tValue) { _tValue = tValue; }
        public override string ToString() { return "MainClass<" + typeof(T).Name + ">. _id = {" + _id.ToString() + "}. _tValue = {" + _tValue.ToString() + "}"; }

        public ClassWithTField<T> GetClassWithTField() { return new ClassWithTField<T>(_tValue); }
        public ClassWithTField<T> PassthruClassWithTField(ClassWithTField<T> value) { return value; }
        public delegate ClassWithTField<T> PassthruClassWithTFieldDelegate(MainClass<T, U> obj, ClassWithTField<T> value);
        public object PassthruClassWithTFieldDelegateDirectCall(MainClass<T, U> obj, ClassWithTField<T> value, PassthruClassWithTFieldDelegate del) { return del(obj, value); }

        public StructWithTField<T> GetStructWithTField() { return new StructWithTField<T>(_tValue); }
        public StructWithTField<T> PassthruStructWithTField(StructWithTField<T> value) { return value; }
        public delegate StructWithTField<T> PassthruStructWithTFieldDelegate(MainClass<T, U> obj, StructWithTField<T> value);
        public object PassthruStructWithTFieldDelegateDirectCall(MainClass<T, U> obj, StructWithTField<T> value, PassthruStructWithTFieldDelegate del) { return del(obj, value); }

        public TestStructKnownSize<T> GetTestStructKnownSize() { return new TestStructKnownSize<T>(new EmptyClass<T>(), _tValue); }
        public TestStructKnownSize<T> PassthruTestStructKnownSize(TestStructKnownSize<T> value) { return value; }
        public delegate TestStructKnownSize<T> PassthruTestStructKnownSizeDelegate(MainClass<T, U> obj, TestStructKnownSize<T> value);
        public object PassthruTestStructKnownSizeDelegateDirectCall(MainClass<T, U> obj, TestStructKnownSize<T> value, PassthruTestStructKnownSizeDelegate del) { return del(obj, value); }

        public TestStructIndeterminateSize<T> GetTestStructIndeterminateSize() { return new TestStructIndeterminateSize<T>(new EmptyClass<T>(), _tValue); }
        public TestStructIndeterminateSize<T> PassthruTestStructIndeterminateSize(TestStructIndeterminateSize<T> value) { return value; }
        public delegate TestStructIndeterminateSize<T> PassthruTestStructIndeterminateSizeDelegate(MainClass<T, U> obj, TestStructIndeterminateSize<T> value);
        public object PassthruTestStructIndeterminateSizeDelegateDirectCall(MainClass<T, U> obj, TestStructIndeterminateSize<T> value, PassthruTestStructIndeterminateSizeDelegate del) { return del(obj, value); }

        public TestStructKnownSize<TestStructIndeterminateSize<T>> GetTestStructKnownSizeWrappingTestStructIndeterminateSize() { return new TestStructKnownSize<TestStructIndeterminateSize<T>>(new EmptyClass<TestStructIndeterminateSize<T>>(), new TestStructIndeterminateSize<T>(new EmptyClass<T>(), _tValue)); }
        public TestStructKnownSize<TestStructIndeterminateSize<T>> PassthruTestStructKnownSizeWrappingTestStructIndeterminateSize(TestStructKnownSize<TestStructIndeterminateSize<T>> value) { return value; }
        public delegate TestStructKnownSize<TestStructIndeterminateSize<T>> PassthruTestStructKnownSizeWrappingTestStructIndeterminateSizeDelegate(MainClass<T, U> obj, TestStructKnownSize<TestStructIndeterminateSize<T>> value);
        public object PassthruTestStructKnownSizeWrappingTestStructIndeterminateSizeDelegateDirectCall(MainClass<T, U> obj, TestStructKnownSize<TestStructIndeterminateSize<T>> value, PassthruTestStructKnownSizeWrappingTestStructIndeterminateSizeDelegate del) { return del(obj, value); }

        public TestStructIndeterminateSize<TestStructKnownSize<T>> GetTestStructIndeterminateSizeWrappingTestStructKnownSize() { return new TestStructIndeterminateSize<TestStructKnownSize<T>>(new EmptyClass<TestStructKnownSize<T>>(), new TestStructKnownSize<T>(new EmptyClass<T>(), _tValue)); }
        public TestStructIndeterminateSize<TestStructKnownSize<T>> PassthruTestStructIndeterminateSizeWrappingTestStructKnownSize(TestStructIndeterminateSize<TestStructKnownSize<T>> value) { return value; }
        public delegate TestStructIndeterminateSize<TestStructKnownSize<T>> PassthruTestStructIndeterminateSizeWrappingTestStructKnownSizeDelegate(MainClass<T, U> obj, TestStructIndeterminateSize<TestStructKnownSize<T>> value);
        public object PassthruTestStructIndeterminateSizeWrappingTestStructKnownSizeDelegateDirectCall(MainClass<T, U> obj, TestStructIndeterminateSize<TestStructKnownSize<T>> value, PassthruTestStructIndeterminateSizeWrappingTestStructKnownSizeDelegate del) { return del(obj, value); }

        public TestStructIndeterminateSize<TestStructKnownSize<ClassWithTField<T>>> GetTestStructIndeterminateSizeWrappingTestStructKnownSizeWrappingReferenceType() { return new TestStructIndeterminateSize<TestStructKnownSize<ClassWithTField<T>>>(new EmptyClass<TestStructKnownSize<ClassWithTField<T>>>(), new TestStructKnownSize<ClassWithTField<T>>(new EmptyClass<ClassWithTField<T>>(), new ClassWithTField<T>(_tValue))); }
        public TestStructIndeterminateSize<TestStructKnownSize<ClassWithTField<T>>> PassthruTestStructIndeterminateSizeWrappingTestStructKnownSizeWrappingReferenceType(TestStructIndeterminateSize<TestStructKnownSize<ClassWithTField<T>>> value) { return value; }
        public delegate TestStructIndeterminateSize<TestStructKnownSize<ClassWithTField<T>>> PassthruTestStructIndeterminateSizeWrappingTestStructKnownSizeWrappingReferenceTypeDelegate(MainClass<T, U> obj, TestStructIndeterminateSize<TestStructKnownSize<ClassWithTField<T>>> value);
        public object PassthruTestStructIndeterminateSizeWrappingTestStructKnownSizeWrappingReferenceTypeDelegateDirectCall(MainClass<T, U> obj, TestStructIndeterminateSize<TestStructKnownSize<ClassWithTField<T>>> value, PassthruTestStructIndeterminateSizeWrappingTestStructKnownSizeWrappingReferenceTypeDelegate del) { return del(obj, value); }

        public TestStructIndeterminateSize<TestStructIndeterminateSize<ClassWithTField<T>>> GetTestStructIndeterminateSizeWrappingTestStructIndeterminateSizeWrappingReferenceType() { return new TestStructIndeterminateSize<TestStructIndeterminateSize<ClassWithTField<T>>>(new EmptyClass<TestStructIndeterminateSize<ClassWithTField<T>>>(), new TestStructIndeterminateSize<ClassWithTField<T>>(new EmptyClass<ClassWithTField<T>>(), new ClassWithTField<T>(_tValue))); }
        public TestStructIndeterminateSize<TestStructIndeterminateSize<ClassWithTField<T>>> PassthruTestStructIndeterminateSizeWrappingTestStructIndeterminateSizeWrappingReferenceType(TestStructIndeterminateSize<TestStructIndeterminateSize<ClassWithTField<T>>> value) { return value; }
        public delegate TestStructIndeterminateSize<TestStructIndeterminateSize<ClassWithTField<T>>> PassthruTestStructIndeterminateSizeWrappingTestStructIndeterminateSizeWrappingReferenceTypeDelegate(MainClass<T, U> obj, TestStructIndeterminateSize<TestStructIndeterminateSize<ClassWithTField<T>>> value);
        public object PassthruTestStructIndeterminateSizeWrappingTestStructIndeterminateSizeWrappingReferenceTypeDelegateDirectCall(MainClass<T, U> obj, TestStructIndeterminateSize<TestStructIndeterminateSize<ClassWithTField<T>>> value, PassthruTestStructIndeterminateSizeWrappingTestStructIndeterminateSizeWrappingReferenceTypeDelegate del) { return del(obj, value); }


        public StructWithOneField1<T> GetStructWithOneField1() { return new StructWithOneField1<T>(GetMainTT()); }
        public StructWithOneField1<T> PassthruStructWithOneField1(StructWithOneField1<T> value) { return value; }
        public delegate StructWithOneField1<T> PassthruStructWithOneField1Delegate(MainClass<T, U> obj, StructWithOneField1<T> value);
        public object PassthruStructWithOneField1DelegateDirectCall(MainClass<T, U> obj, StructWithOneField1<T> value, PassthruStructWithOneField1Delegate del) { return del(obj, value); }

        public StructWithOneField2<T> GetStructWithOneField2() { return new StructWithOneField2<T>(new ClassWithTField<T>(_tValue)); }
        public StructWithOneField2<T> PassthruStructWithOneField2(StructWithOneField2<T> value) { return value; }
        public delegate StructWithOneField2<T> PassthruStructWithOneField2Delegate(MainClass<T, U> obj, StructWithOneField2<T> value);
        public object PassthruStructWithOneField2DelegateDirectCall(MainClass<T, U> obj, StructWithOneField2<T> value, PassthruStructWithOneField2Delegate del) { return del(obj, value); }

        public StructWithOneField3<T> GetStructWithOneField3() { return new StructWithOneField3<T>(new StructWithTField<T>(_tValue)); }
        public StructWithOneField3<T> PassthruStructWithOneField3(StructWithOneField3<T> value) { return value; }
        public delegate StructWithOneField3<T> PassthruStructWithOneField3Delegate(MainClass<T, U> obj, StructWithOneField3<T> value);
        public object PassthruStructWithOneField3DelegateDirectCall(MainClass<T, U> obj, StructWithOneField3<T> value, PassthruStructWithOneField3Delegate del) { return del(obj, value); }

        public StructWithTwoFields1<T> GetStructWithTwoFields1() { return new StructWithTwoFields1<T>(GetMainTT(), _tValue); }
        public StructWithTwoFields1<T> PassthruStructWithTwoFields1(StructWithTwoFields1<T> value) { return value; }
        public delegate StructWithTwoFields1<T> PassthruStructWithTwoFields1Delegate(MainClass<T, U> obj, StructWithTwoFields1<T> value);
        public object PassthruStructWithTwoFields1DelegateDirectCall(MainClass<T, U> obj, StructWithTwoFields1<T> value, PassthruStructWithTwoFields1Delegate del) { return del(obj, value); }

        public StructWithTwoFields2<T> GetStructWithTwoFields2() { return new StructWithTwoFields2<T>(GetMainTT(), _tValue); }
        public StructWithTwoFields2<T> PassthruStructWithTwoFields2(StructWithTwoFields2<T> value) { return value; }
        public delegate StructWithTwoFields2<T> PassthruStructWithTwoFields2Delegate(MainClass<T, U> obj, StructWithTwoFields2<T> value);
        public object PassthruStructWithTwoFields2DelegateDirectCall(MainClass<T, U> obj, StructWithTwoFields2<T> value, PassthruStructWithTwoFields2Delegate del) { return del(obj, value); }


        public StructWithTwoGenParams<int, T> GetStructWithTwoGenParams1() { return new StructWithTwoGenParams<int, T>(int.Parse(_id)); }
        public StructWithTwoGenParams<int, T> PassthruStructWithTwoGenParams1(StructWithTwoGenParams<int, T> value) { return value; }
        public delegate StructWithTwoGenParams<int, T> PassthruStructWithTwoGenParams1Delegate(MainClass<T, U> obj, StructWithTwoGenParams<int, T> value);
        public object PassthruStructWithTwoGenParams1DelegateDirectCall(MainClass<T, U> obj, StructWithTwoGenParams<int, T> value, PassthruStructWithTwoGenParams1Delegate del) { return del(obj, value); }

        public StructWithTwoGenParams<int, KeyValuePair<int, StructWithTwoGenParams<List<T>, T>>> GetStructWithTwoGenParams2() { return new StructWithTwoGenParams<int, KeyValuePair<int, StructWithTwoGenParams<List<T>, T>>>(int.Parse(_id)); }
        public StructWithTwoGenParams<int, KeyValuePair<int, StructWithTwoGenParams<List<T>, T>>> PassthruStructWithTwoGenParams2(StructWithTwoGenParams<int, KeyValuePair<int, StructWithTwoGenParams<List<T>, T>>> value) { return value; }
        public delegate StructWithTwoGenParams<int, KeyValuePair<int, StructWithTwoGenParams<List<T>, T>>> PassthruStructWithTwoGenParams2Delegate(MainClass<T, U> obj, StructWithTwoGenParams<int, KeyValuePair<int, StructWithTwoGenParams<List<T>, T>>> value);
        public object PassthruStructWithTwoGenParams2DelegateDirectCall(MainClass<T, U> obj, StructWithTwoGenParams<int, KeyValuePair<int, StructWithTwoGenParams<List<T>, T>>> value, PassthruStructWithTwoGenParams2Delegate del) { return del(obj, value); }

        public StructWithTwoGenParams<int, StructWithTwoGenParams<List<T>, KeyValuePair<T, T>>> GetStructWithTwoGenParams3() { return new StructWithTwoGenParams<int, StructWithTwoGenParams<List<T>, KeyValuePair<T, T>>>(int.Parse(_id)); }
        public StructWithTwoGenParams<int, StructWithTwoGenParams<List<T>, KeyValuePair<T, T>>> PassthruStructWithTwoGenParams3(StructWithTwoGenParams<int, StructWithTwoGenParams<List<T>, KeyValuePair<T, T>>> value) { return value; }
        public delegate StructWithTwoGenParams<int, StructWithTwoGenParams<List<T>, KeyValuePair<T, T>>> PassthruStructWithTwoGenParams3Delegate(MainClass<T, U> obj, StructWithTwoGenParams<int, StructWithTwoGenParams<List<T>, KeyValuePair<T, T>>> value);
        public object PassthruStructWithTwoGenParams3DelegateDirectCall(MainClass<T, U> obj, StructWithTwoGenParams<int, StructWithTwoGenParams<List<T>, KeyValuePair<T, T>>> value, PassthruStructWithTwoGenParams3Delegate del) { return del(obj, value); }

        public StructWithTwoGenParams<int, StructWithTwoGenParams<List<int>, KeyValuePair<int, string>>> GetStructWithTwoGenParams4() { return new StructWithTwoGenParams<int, StructWithTwoGenParams<List<int>, KeyValuePair<int, string>>>(int.Parse(_id)); }
        public StructWithTwoGenParams<int, StructWithTwoGenParams<List<int>, KeyValuePair<int, string>>> PassthruStructWithTwoGenParams4(StructWithTwoGenParams<int, StructWithTwoGenParams<List<int>, KeyValuePair<int, string>>> value) { return value; }
        public delegate StructWithTwoGenParams<int, StructWithTwoGenParams<List<int>, KeyValuePair<int, string>>> PassthruStructWithTwoGenParams4Delegate(MainClass<T, U> obj, StructWithTwoGenParams<int, StructWithTwoGenParams<List<int>, KeyValuePair<int, string>>> value);
        public object PassthruStructWithTwoGenParams4DelegateDirectCall(MainClass<T, U> obj, StructWithTwoGenParams<int, StructWithTwoGenParams<List<int>, KeyValuePair<int, string>>> value, PassthruStructWithTwoGenParams4Delegate del) { return del(obj, value); }


        public StructWithOneFieldTwoGenParams1<T, T> GetStructWithOneFieldTwoGenParams1() { return new StructWithOneFieldTwoGenParams1<T, T>(GetMainTT()); }
        public StructWithOneFieldTwoGenParams1<T, T> PassthruStructWithOneFieldTwoGenParams1(StructWithOneFieldTwoGenParams1<T, T> value) { return value; }
        public delegate StructWithOneFieldTwoGenParams1<T, T> PassthruStructWithOneFieldTwoGenParams1Delegate(MainClass<T, U> obj, StructWithOneFieldTwoGenParams1<T, T> value);
        public object PassthruStructWithOneFieldTwoGenParams1DelegateDirectCall(MainClass<T, U> obj, StructWithOneFieldTwoGenParams1<T, T> value, PassthruStructWithOneFieldTwoGenParams1Delegate del) { return del(obj, value); }

        public StructWithOneFieldTwoGenParams2<T, T> GetStructWithOneFieldTwoGenParams2() { return new StructWithOneFieldTwoGenParams2<T, T>(new ClassWithTField<T>(_tValue)); }
        public StructWithOneFieldTwoGenParams2<T, T> PassthruStructWithOneFieldTwoGenParams2(StructWithOneFieldTwoGenParams2<T, T> value) { return value; }
        public delegate StructWithOneFieldTwoGenParams2<T, T> PassthruStructWithOneFieldTwoGenParams2Delegate(MainClass<T, U> obj, StructWithOneFieldTwoGenParams2<T, T> value);
        public object PassthruStructWithOneFieldTwoGenParams2DelegateDirectCall(MainClass<T, U> obj, StructWithOneFieldTwoGenParams2<T, T> value, PassthruStructWithOneFieldTwoGenParams2Delegate del) { return del(obj, value); }

        public StructWithOneFieldTwoGenParams3<T, T> GetStructWithOneFieldTwoGenParams3() { return new StructWithOneFieldTwoGenParams3<T, T>(new StructWithTField<T>(_tValue)); }
        public StructWithOneFieldTwoGenParams3<T, T> PassthruStructWithOneFieldTwoGenParams3(StructWithOneFieldTwoGenParams3<T, T> value) { return value; }
        public delegate StructWithOneFieldTwoGenParams3<T, T> PassthruStructWithOneFieldTwoGenParams3Delegate(MainClass<T, U> obj, StructWithOneFieldTwoGenParams3<T, T> value);
        public object PassthruStructWithOneFieldTwoGenParams3DelegateDirectCall(MainClass<T, U> obj, StructWithOneFieldTwoGenParams3<T, T> value, PassthruStructWithOneFieldTwoGenParams3Delegate del) { return del(obj, value); }

        public StructWithTwoFieldsTwoGenParams1<T, T> GetStructWithTwoFieldsTwoGenParams1() { return new StructWithTwoFieldsTwoGenParams1<T, T>(GetMainTT(), _tValue); }
        public StructWithTwoFieldsTwoGenParams1<T, T> PassthruStructWithTwoFieldsTwoGenParams1(StructWithTwoFieldsTwoGenParams1<T, T> value) { return value; }
        public delegate StructWithTwoFieldsTwoGenParams1<T, T> PassthruStructWithTwoFieldsTwoGenParams1Delegate(MainClass<T, U> obj, StructWithTwoFieldsTwoGenParams1<T, T> value);
        public object PassthruStructWithTwoFieldsTwoGenParams1DelegateDirectCall(MainClass<T, U> obj, StructWithTwoFieldsTwoGenParams1<T, T> value, PassthruStructWithTwoFieldsTwoGenParams1Delegate del) { return del(obj, value); }

        public StructWithTwoFieldsTwoGenParams2<T, T> GetStructWithTwoFieldsTwoGenParams2() { return new StructWithTwoFieldsTwoGenParams2<T, T>(GetMainTT(), _tValue); }
        public StructWithTwoFieldsTwoGenParams2<T, T> PassthruStructWithTwoFieldsTwoGenParams2(StructWithTwoFieldsTwoGenParams2<T, T> value) { return value; }
        public delegate StructWithTwoFieldsTwoGenParams2<T, T> PassthruStructWithTwoFieldsTwoGenParams2Delegate(MainClass<T, U> obj, StructWithTwoFieldsTwoGenParams2<T, T> value);
        public object PassthruStructWithTwoFieldsTwoGenParams2DelegateDirectCall(MainClass<T, U> obj, StructWithTwoFieldsTwoGenParams2<T, T> value, PassthruStructWithTwoFieldsTwoGenParams2Delegate del) { return del(obj, value); }

        public StructWithOneFieldTwoGenParams1<T, float> GetStructWithOneFieldTwoGenParams1_KnownU() { return new StructWithOneFieldTwoGenParams1<T, float>(GetMainTT()); }
        public StructWithOneFieldTwoGenParams1<T, float> PassthruStructWithOneFieldTwoGenParams1_KnownU(StructWithOneFieldTwoGenParams1<T, float> value) { return value; }
        public delegate StructWithOneFieldTwoGenParams1<T, float> PassthruStructWithOneFieldTwoGenParams1_KnownUDelegate(MainClass<T, U> obj, StructWithOneFieldTwoGenParams1<T, float> value);
        public object PassthruStructWithOneFieldTwoGenParams1_KnownUDelegateDirectCall(MainClass<T, U> obj, StructWithOneFieldTwoGenParams1<T, float> value, PassthruStructWithOneFieldTwoGenParams1_KnownUDelegate del) { return del(obj, value); }

        public StructWithOneFieldTwoGenParams2<T, float> GetStructWithOneFieldTwoGenParams2_KnownU() { return new StructWithOneFieldTwoGenParams2<T, float>(new ClassWithTField<T>(_tValue)); }
        public StructWithOneFieldTwoGenParams2<T, float> PassthruStructWithOneFieldTwoGenParams2_KnownU(StructWithOneFieldTwoGenParams2<T, float> value) { return value; }
        public delegate StructWithOneFieldTwoGenParams2<T, float> PassthruStructWithOneFieldTwoGenParams2_KnownUDelegate(MainClass<T, U> obj, StructWithOneFieldTwoGenParams2<T, float> value);
        public object PassthruStructWithOneFieldTwoGenParams2_KnownUDelegateDirectCall(MainClass<T, U> obj, StructWithOneFieldTwoGenParams2<T, float> value, PassthruStructWithOneFieldTwoGenParams2_KnownUDelegate del) { return del(obj, value); }

        public StructWithOneFieldTwoGenParams3<T, float> GetStructWithOneFieldTwoGenParams3_KnownU() { return new StructWithOneFieldTwoGenParams3<T, float>(new StructWithTField<T>(_tValue)); }
        public StructWithOneFieldTwoGenParams3<T, float> PassthruStructWithOneFieldTwoGenParams3_KnownU(StructWithOneFieldTwoGenParams3<T, float> value) { return value; }
        public delegate StructWithOneFieldTwoGenParams3<T, float> PassthruStructWithOneFieldTwoGenParams3_KnownUDelegate(MainClass<T, U> obj, StructWithOneFieldTwoGenParams3<T, float> value);
        public object PassthruStructWithOneFieldTwoGenParams3_KnownUDelegateDirectCall(MainClass<T, U> obj, StructWithOneFieldTwoGenParams3<T, float> value, PassthruStructWithOneFieldTwoGenParams3_KnownUDelegate del) { return del(obj, value); }

        public StructWithTwoFieldsTwoGenParams1<T, float> GetStructWithTwoFieldsTwoGenParams1_KnownU() { return new StructWithTwoFieldsTwoGenParams1<T, float>(GetMainTT(), _tValue); }
        public StructWithTwoFieldsTwoGenParams1<T, float> PassthruStructWithTwoFieldsTwoGenParams1_KnownU(StructWithTwoFieldsTwoGenParams1<T, float> value) { return value; }
        public delegate StructWithTwoFieldsTwoGenParams1<T, float> PassthruStructWithTwoFieldsTwoGenParams1_KnownUDelegate(MainClass<T, U> obj, StructWithTwoFieldsTwoGenParams1<T, float> value);
        public object PassthruStructWithTwoFieldsTwoGenParams1_KnownUDelegateDirectCall(MainClass<T, U> obj, StructWithTwoFieldsTwoGenParams1<T, float> value, PassthruStructWithTwoFieldsTwoGenParams1_KnownUDelegate del) { return del(obj, value); }

        public StructWithTwoFieldsTwoGenParams2<T, float> GetStructWithTwoFieldsTwoGenParams2_KnownU() { return new StructWithTwoFieldsTwoGenParams2<T, float>(GetMainTT(), _tValue); }
        public StructWithTwoFieldsTwoGenParams2<T, float> PassthruStructWithTwoFieldsTwoGenParams2_KnownU(StructWithTwoFieldsTwoGenParams2<T, float> value) { return value; }
        public delegate StructWithTwoFieldsTwoGenParams2<T, float> PassthruStructWithTwoFieldsTwoGenParams2_KnownUDelegate(MainClass<T, U> obj, StructWithTwoFieldsTwoGenParams2<T, float> value);
        public object PassthruStructWithTwoFieldsTwoGenParams2_KnownUDelegateDirectCall(MainClass<T, U> obj, StructWithTwoFieldsTwoGenParams2<T, float> value, PassthruStructWithTwoFieldsTwoGenParams2_KnownUDelegate del) { return del(obj, value); }


        public StructWithOneFieldTwoGenParams1<long, T> GetStructWithOneFieldTwoGenParams1_KnownT() { return new StructWithOneFieldTwoGenParams1<long, T>(new MainClass<long, long>("123", long.Parse(_id))); }
        public StructWithOneFieldTwoGenParams1<long, T> PassthruStructWithOneFieldTwoGenParams1_KnownT(StructWithOneFieldTwoGenParams1<long, T> value) { return value; }
        public delegate StructWithOneFieldTwoGenParams1<long, T> PassthruStructWithOneFieldTwoGenParams1_KnownTDelegate(MainClass<T, U> obj, StructWithOneFieldTwoGenParams1<long, T> value);
        public object PassthruStructWithOneFieldTwoGenParams1_KnownTDelegateDirectCall(MainClass<T, U> obj, StructWithOneFieldTwoGenParams1<long, T> value, PassthruStructWithOneFieldTwoGenParams1_KnownTDelegate del) { return del(obj, value); }

        public StructWithOneFieldTwoGenParams2<long, T> GetStructWithOneFieldTwoGenParams2_KnownT() { return new StructWithOneFieldTwoGenParams2<long, T>(new ClassWithTField<long>(long.Parse(_id))); }
        public StructWithOneFieldTwoGenParams2<long, T> PassthruStructWithOneFieldTwoGenParams2_KnownT(StructWithOneFieldTwoGenParams2<long, T> value) { return value; }
        public delegate StructWithOneFieldTwoGenParams2<long, T> PassthruStructWithOneFieldTwoGenParams2_KnownTDelegate(MainClass<T, U> obj, StructWithOneFieldTwoGenParams2<long, T> value);
        public object PassthruStructWithOneFieldTwoGenParams2_KnownTDelegateDirectCall(MainClass<T, U> obj, StructWithOneFieldTwoGenParams2<long, T> value, PassthruStructWithOneFieldTwoGenParams2_KnownTDelegate del) { return del(obj, value); }

        public StructWithOneFieldTwoGenParams3<long, T> GetStructWithOneFieldTwoGenParams3_KnownT() { return new StructWithOneFieldTwoGenParams3<long, T>(new StructWithTField<long>(long.Parse(_id))); }
        public StructWithOneFieldTwoGenParams3<long, T> PassthruStructWithOneFieldTwoGenParams3_KnownT(StructWithOneFieldTwoGenParams3<long, T> value) { return value; }
        public delegate StructWithOneFieldTwoGenParams3<long, T> PassthruStructWithOneFieldTwoGenParams3_KnownTDelegate(MainClass<T, U> obj, StructWithOneFieldTwoGenParams3<long, T> value);
        public object PassthruStructWithOneFieldTwoGenParams3_KnownTDelegateDirectCall(MainClass<T, U> obj, StructWithOneFieldTwoGenParams3<long, T> value, PassthruStructWithOneFieldTwoGenParams3_KnownTDelegate del) { return del(obj, value); }

        public StructWithTwoFieldsTwoGenParams1<long, T> GetStructWithTwoFieldsTwoGenParams1_KnownT() { return new StructWithTwoFieldsTwoGenParams1<long, T>(new MainClass<long, long>("456", long.Parse(_id)), long.Parse(_id)); }
        public StructWithTwoFieldsTwoGenParams1<long, T> PassthruStructWithTwoFieldsTwoGenParams1_KnownT(StructWithTwoFieldsTwoGenParams1<long, T> value) { return value; }
        public delegate StructWithTwoFieldsTwoGenParams1<long, T> PassthruStructWithTwoFieldsTwoGenParams1_KnownTDelegate(MainClass<T, U> obj, StructWithTwoFieldsTwoGenParams1<long, T> value);
        public object PassthruStructWithTwoFieldsTwoGenParams1_KnownTDelegateDirectCall(MainClass<T, U> obj, StructWithTwoFieldsTwoGenParams1<long, T> value, PassthruStructWithTwoFieldsTwoGenParams1_KnownTDelegate del) { return del(obj, value); }

        public StructWithTwoFieldsTwoGenParams2<long, T> GetStructWithTwoFieldsTwoGenParams2_KnownT() { return new StructWithTwoFieldsTwoGenParams2<long, T>(new MainClass<long, long>("789", long.Parse(_id)), long.Parse(_id)); }
        public StructWithTwoFieldsTwoGenParams2<long, T> PassthruStructWithTwoFieldsTwoGenParams2_KnownT(StructWithTwoFieldsTwoGenParams2<long, T> value) { return value; }
        public delegate StructWithTwoFieldsTwoGenParams2<long, T> PassthruStructWithTwoFieldsTwoGenParams2_KnownTDelegate(MainClass<T, U> obj, StructWithTwoFieldsTwoGenParams2<long, T> value);
        public object PassthruStructWithTwoFieldsTwoGenParams2_KnownTDelegateDirectCall(MainClass<T, U> obj, StructWithTwoFieldsTwoGenParams2<long, T> value, PassthruStructWithTwoFieldsTwoGenParams2_KnownTDelegate del) { return del(obj, value); }

        public string ObjectToString<Y>(Y x) { return x.ToString(); }
    }
    #endregion

    public class Test
    {
        [TestMethod]
        public static void TestInstancesOfKnownAndUnknownSizes()
        {
            foreach (Type genArg in new Type[] { TypeOf.Double, TypeOf.String })
            {
                Type t = typeof(MainClass<,>).MakeGenericType(genArg, TypeOf.Double);
                object o = Activator.CreateInstance(t);
                object genArgInst = genArg == TypeOf.Double ? (object)12.43 : "56.78";

                MethodInfo SetID = t.GetTypeInfo().GetDeclaredMethod("SetID");
                SetID.Invoke(o, new object[] { "abc" });
                MethodInfo SetT = t.GetTypeInfo().GetDeclaredMethod("SetT");
                SetT.Invoke(o, new object[] { genArgInst });

                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetClassWithTField", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithTField", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetTestStructKnownSize", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetTestStructIndeterminateSize", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetTestStructKnownSizeWrappingTestStructIndeterminateSize", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetTestStructIndeterminateSizeWrappingTestStructKnownSize", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetTestStructIndeterminateSizeWrappingTestStructKnownSizeWrappingReferenceType", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetTestStructIndeterminateSizeWrappingTestStructIndeterminateSizeWrappingReferenceType", "abc", genArgInst);

                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithOneField1", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithOneField2", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithOneField3", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithTwoFields1", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithTwoFields2", "abc", genArgInst);

                SetID.Invoke(o, new object[] { "11" });
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithTwoGenParams1", "11", null);
                SetID.Invoke(o, new object[] { "22" });
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithTwoGenParams2", "22", null);
                SetID.Invoke(o, new object[] { "33" });
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithTwoGenParams3", "33", null);
                SetID.Invoke(o, new object[] { "44" });
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithTwoGenParams4", "44", null);

                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithOneFieldTwoGenParams1", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithOneFieldTwoGenParams2", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithOneFieldTwoGenParams3", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithTwoFieldsTwoGenParams1", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithTwoFieldsTwoGenParams2", "abc", genArgInst);

                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithOneFieldTwoGenParams1_KnownU", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithOneFieldTwoGenParams2_KnownU", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithOneFieldTwoGenParams3_KnownU", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithTwoFieldsTwoGenParams1_KnownU", "abc", genArgInst);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithTwoFieldsTwoGenParams2_KnownU", "abc", genArgInst);

                SetID.Invoke(o, new object[] { "55" });
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithOneFieldTwoGenParams1_KnownT", "123", "55");
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithOneFieldTwoGenParams2_KnownT", "55", null);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithOneFieldTwoGenParams3_KnownT", "55", null);
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithTwoFieldsTwoGenParams1_KnownT", "456", "55");
                TestInstancesOfKnownAndUnknownSizes_Inner(t, o, "GetStructWithTwoFieldsTwoGenParams2_KnownT", "789", "55");
            }
        }
        static string TestInstancesOfKnownAndUnknownSizes_GetExpectedResult(Type t, object a, object b, string getterFunc)
        {
            switch (getterFunc)
            {
                case "GetClassWithTField": return "ClassWithTField<" + t.Name + ">. _field1 = {" + b + "}";
                case "GetStructWithTField": return "StructWithTField<" + t.Name + ">. _field1 = {" + b + "}";
                case "GetTestStructKnownSize": return "TestStructKnownSize<" + t.Name + ">. _field1 = {EmptyClass<" + t.Name + ">}. _field2 = {ClassWithTField<" + t.Name + ">. _field1 = {" + b + "}}";
                case "GetTestStructIndeterminateSize": return "TestStructIndeterminateSize<" + t.Name + ">. _field1 = {EmptyClass<" + t.Name + ">}. _field2 = {StructWithTField<" + t.Name + ">. _field1 = {" + b + "}}";
                case "GetTestStructKnownSizeWrappingTestStructIndeterminateSize": return "TestStructKnownSize<TestStructIndeterminateSize`1>. _field1 = {EmptyClass<TestStructIndeterminateSize`1>}. _field2 = {ClassWithTField<TestStructIndeterminateSize`1>. _field1 = {TestStructIndeterminateSize<" + t.Name + ">. _field1 = {EmptyClass<" + t.Name + ">}. _field2 = {StructWithTField<" + t.Name + ">. _field1 = {" + b + "}}}}";
                case "GetTestStructIndeterminateSizeWrappingTestStructKnownSize": return "TestStructIndeterminateSize<TestStructKnownSize`1>. _field1 = {EmptyClass<TestStructKnownSize`1>}. _field2 = {StructWithTField<TestStructKnownSize`1>. _field1 = {TestStructKnownSize<" + t.Name + ">. _field1 = {EmptyClass<" + t.Name + ">}. _field2 = {ClassWithTField<" + t.Name + ">. _field1 = {" + b + "}}}}";
                case "GetTestStructIndeterminateSizeWrappingTestStructKnownSizeWrappingReferenceType": return "TestStructIndeterminateSize<TestStructKnownSize`1>. _field1 = {EmptyClass<TestStructKnownSize`1>}. _field2 = {StructWithTField<TestStructKnownSize`1>. _field1 = {TestStructKnownSize<ClassWithTField`1>. _field1 = {EmptyClass<ClassWithTField`1>}. _field2 = {ClassWithTField<ClassWithTField`1>. _field1 = {ClassWithTField<" + t.Name + ">. _field1 = {" + b + "}}}}}";
                case "GetTestStructIndeterminateSizeWrappingTestStructIndeterminateSizeWrappingReferenceType": return "TestStructIndeterminateSize<TestStructIndeterminateSize`1>. _field1 = {EmptyClass<TestStructIndeterminateSize`1>}. _field2 = {StructWithTField<TestStructIndeterminateSize`1>. _field1 = {TestStructIndeterminateSize<ClassWithTField`1>. _field1 = {EmptyClass<ClassWithTField`1>}. _field2 = {StructWithTField<ClassWithTField`1>. _field1 = {ClassWithTField<" + t.Name + ">. _field1 = {" + b + "}}}}}";

                case "GetStructWithOneField1": return "StructWithOneField1<" + t.Name + ">. _field1 = {MainClass<" + t.Name + ">. _id = {" + a + "}. _tValue = {" + b + "}}";
                case "GetStructWithOneField2": return "StructWithOneField2<" + t.Name + ">. _field1 = {ClassWithTField<" + t.Name + ">. _field1 = {" + b + "}}";
                case "GetStructWithOneField3": return "StructWithOneField3<" + t.Name + ">. _field1 = {StructWithTField<" + t.Name + ">. _field1 = {" + b + "}}";
                case "GetStructWithTwoFields1": return "StructWithTwoFields1<" + t.Name + ">. _field1 = {MainClass<" + t.Name + ">. _id = {" + a + "}. _tValue = {" + b + "}}. _field2 = {TestStructKnownSize<" + t.Name + ">. _field1 = {EmptyClass<" + t.Name + ">}. _field2 = {ClassWithTField<" + t.Name + ">. _field1 = {" + b + "}}}";
                case "GetStructWithTwoFields2": return "StructWithTwoFields2<" + t.Name + ">. _field1 = {MainClass<" + t.Name + ">. _id = {" + a + "}. _tValue = {" + b + "}}. _field2 = {TestStructIndeterminateSize<" + t.Name + ">. _field1 = {EmptyClass<" + t.Name + ">}. _field2 = {StructWithTField<" + t.Name + ">. _field1 = {" + b + "}}}";

                case "GetStructWithTwoGenParams1": return "StructWithTwoGenParams<System.Int32," + t + ">. _field1 = {" + a + "}";
                case "GetStructWithTwoGenParams2": return "StructWithTwoGenParams<System.Int32,System.Collections.Generic.KeyValuePair`2[System.Int32,CallingConvention.StructWithTwoGenParams`2[System.Collections.Generic.List`1[" + t + "]," + t + "]]>. _field1 = {" + a + "}";
                case "GetStructWithTwoGenParams3": return "StructWithTwoGenParams<System.Int32,CallingConvention.StructWithTwoGenParams`2[System.Collections.Generic.List`1[" + t + "],System.Collections.Generic.KeyValuePair`2[" + t + "," + t + "]]>. _field1 = {" + a + "}";
                case "GetStructWithTwoGenParams4": return "StructWithTwoGenParams<System.Int32,CallingConvention.StructWithTwoGenParams`2[System.Collections.Generic.List`1[System.Int32],System.Collections.Generic.KeyValuePair`2[System.Int32,System.String]]>. _field1 = {" + a + "}";

                case "GetStructWithOneFieldTwoGenParams1": return "StructWithOneFieldTwoGenParams1<" + t + "," + t + ">. _field1 = {MainClass<" + t.Name + ">. _id = {" + a + "}. _tValue = {" + b + "}}";
                case "GetStructWithOneFieldTwoGenParams2": return "StructWithOneFieldTwoGenParams2<" + t + "," + t + ">. _field1 = {ClassWithTField<" + t.Name + ">. _field1 = {" + b + "}}";
                case "GetStructWithOneFieldTwoGenParams3": return "StructWithOneFieldTwoGenParams3<" + t + "," + t + ">. _field1 = {StructWithTField<" + t.Name + ">. _field1 = {" + b + "}}";
                case "GetStructWithTwoFieldsTwoGenParams1": return "StructWithTwoFieldsTwoGenParams1<" + t + "," + t + ">. _field1 = {MainClass<" + t.Name + ">. _id = {" + a + "}. _tValue = {" + b + "}}. _field2 = {TestStructKnownSize<" + t.Name + ">. _field1 = {EmptyClass<" + t.Name + ">}. _field2 = {ClassWithTField<" + t.Name + ">. _field1 = {" + b + "}}}";
                case "GetStructWithTwoFieldsTwoGenParams2": return "StructWithTwoFieldsTwoGenParams2<" + t + "," + t + ">. _field1 = {MainClass<" + t.Name + ">. _id = {" + a + "}. _tValue = {" + b + "}}. _field2 = {TestStructIndeterminateSize<" + t.Name + ">. _field1 = {EmptyClass<" + t.Name + ">}. _field2 = {StructWithTField<" + t.Name + ">. _field1 = {" + b + "}}}";

                case "GetStructWithOneFieldTwoGenParams1_KnownU": return "StructWithOneFieldTwoGenParams1<" + t + ",System.Single>. _field1 = {MainClass<" + t.Name + ">. _id = {" + a + "}. _tValue = {" + b + "}}";
                case "GetStructWithOneFieldTwoGenParams2_KnownU": return "StructWithOneFieldTwoGenParams2<" + t + ",System.Single>. _field1 = {ClassWithTField<" + t.Name + ">. _field1 = {" + b + "}}";
                case "GetStructWithOneFieldTwoGenParams3_KnownU": return "StructWithOneFieldTwoGenParams3<" + t + ",System.Single>. _field1 = {StructWithTField<" + t.Name + ">. _field1 = {" + b + "}}";
                case "GetStructWithTwoFieldsTwoGenParams1_KnownU": return "StructWithTwoFieldsTwoGenParams1<" + t + ",System.Single>. _field1 = {MainClass<" + t.Name + ">. _id = {" + a + "}. _tValue = {" + b + "}}. _field2 = {TestStructKnownSize<" + t.Name + ">. _field1 = {EmptyClass<" + t.Name + ">}. _field2 = {ClassWithTField<" + t.Name + ">. _field1 = {" + b + "}}}";
                case "GetStructWithTwoFieldsTwoGenParams2_KnownU": return "StructWithTwoFieldsTwoGenParams2<" + t + ",System.Single>. _field1 = {MainClass<" + t.Name + ">. _id = {" + a + "}. _tValue = {" + b + "}}. _field2 = {TestStructIndeterminateSize<" + t.Name + ">. _field1 = {EmptyClass<" + t.Name + ">}. _field2 = {StructWithTField<" + t.Name + ">. _field1 = {" + b + "}}}";

                case "GetStructWithOneFieldTwoGenParams1_KnownT": return "StructWithOneFieldTwoGenParams1<System.Int64," + t + ">. _field1 = {MainClass<Int64>. _id = {" + a + "}. _tValue = {" + b + "}}";
                case "GetStructWithOneFieldTwoGenParams2_KnownT": return "StructWithOneFieldTwoGenParams2<System.Int64," + t + ">. _field1 = {ClassWithTField<Int64>. _field1 = {" + a + "}}";
                case "GetStructWithOneFieldTwoGenParams3_KnownT": return "StructWithOneFieldTwoGenParams3<System.Int64," + t + ">. _field1 = {StructWithTField<Int64>. _field1 = {" + a + "}}";
                case "GetStructWithTwoFieldsTwoGenParams1_KnownT": return "StructWithTwoFieldsTwoGenParams1<System.Int64," + t + ">. _field1 = {MainClass<Int64>. _id = {" + a + "}. _tValue = {" + b + "}}. _field2 = {TestStructKnownSize<Int64>. _field1 = {EmptyClass<Int64>}. _field2 = {ClassWithTField<Int64>. _field1 = {" + b + "}}}";
                case "GetStructWithTwoFieldsTwoGenParams2_KnownT": return "StructWithTwoFieldsTwoGenParams2<System.Int64," + t + ">. _field1 = {MainClass<Int64>. _id = {" + a + "}. _tValue = {" + b + "}}. _field2 = {TestStructIndeterminateSize<Int64>. _field1 = {EmptyClass<Int64>}. _field2 = {StructWithTField<Int64>. _field1 = {" + b + "}}}";

                default: return null;
            }
        }
        static void TestInstancesOfKnownAndUnknownSizes_Inner(Type t, object o, string getterFunc, object a, object b)
        {
            string expectedResult = TestInstancesOfKnownAndUnknownSizes_GetExpectedResult(t.GenericTypeArguments[0], a, b, getterFunc);

            // Test that the abi handling for a return value works
            MethodInfo getter = t.GetTypeInfo().GetDeclaredMethod(getterFunc);
            object retVal = getter.Invoke(o, null);
            Assert.AreEqual(expectedResult, retVal.ToString());

            MethodInfo toString = t.GetTypeInfo().GetDeclaredMethod("ObjectToString").MakeGenericMethod(retVal.GetType());
            string res = (string)toString.Invoke(o, new object[] { retVal });
            Assert.AreEqual(expectedResult, res);

            // Test that the abi handling for a parameter
            string passthruName = "Passthru" + getterFunc.Substring("Get".Length);
            MethodInfo passthru = t.GetTypeInfo().GetDeclaredMethod(passthruName);
            object passthruRetVal = passthru.Invoke(o, new object[] { retVal });

            // Passthru return value testing
            res = (string)toString.Invoke(o, new object[] { passthruRetVal });
            Assert.AreEqual(expectedResult, res);

            // Test that the abi handling for a parameter and return value, through the constructed delegate invoke path
            Type delegateType = t.GetTypeInfo().GetDeclaredNestedType(passthruName + "Delegate").AsType();
            Type funcType = delegateType.MakeGenericType(t.GenericTypeArguments);
            Delegate passthruDelegate = passthru.CreateDelegate(funcType);
            object passthruDelegateRetVal = passthruDelegate.DynamicInvoke(o, passthruRetVal);

            // Passthru delegate return value testing
            res = (string)toString.Invoke(o, new object[] { passthruDelegateRetVal });
            Assert.AreEqual(expectedResult, res);

            // PassthurDelegate direct call testing
            MethodInfo delegateDirectCall = delegateDirectCall = t.GetTypeInfo().GetDeclaredMethod(passthruName + "DelegateDirectCall");

            if (delegateDirectCall != null)
            {
                object passthruDelegateDirectCallRetVal = delegateDirectCall.Invoke(o, new object[] { o, passthruDelegateRetVal, passthruDelegate });

                // Passthru delegate return value testing
                res = (string)toString.Invoke(o, new object[] { passthruDelegateDirectCallRetVal });
                Assert.AreEqual(expectedResult, res);
            }
        }


        [TestMethod]
        public static void TestCallInstanceFunction()
        {
            var t = TypeOf.CCT_UCGTestNonVirtualFunctionCallUse.MakeGenericType(TypeOf.Short);
            TestNonVirtualFunctionCallUseBase o = (TestNonVirtualFunctionCallUseBase)Activator.CreateInstance(t);

            new UCGSeparateClass<short>().BoxParam(4);

            short testValue = (short)3817;
            object returnValue = o.TestNonVirtualInstanceFunction(new UCGSeparateClass<short>(), (object)testValue);
            Assert.AreEqual(testValue, (short)returnValue);
        }


        public static object s_expectedT;

        [TestMethod]
        public static void TestCallInterface()
        {
            var t = TypeOf.CCT_UCGTestUseInterface.MakeGenericType(TypeOf.Short);
            TestInterfaceUseBase o = (TestInterfaceUseBase)Activator.CreateInstance(t);

            ITestCallInterface<short> temp = (ITestCallInterface<short>)new TestCallInterfaceImpl();
            TestInterfaceUseBase.ShortValue = 0;
            short testValue = (short)3817;
            o.TestUseInterface(new TestCallInterfaceImpl(), (object)testValue);
            Assert.AreEqual(testValue, TestInterfaceUseBase.ShortValue);
        }

        [TestMethod]
        public static void CallingConventionTest()
        {
            // Using the normal canonical template for the instantiation
            {
                s_expectedT = "Hello";
                CallingConventionTest_Inner<string>("Hello");
            }

            // Using the universal canonical template for the instantiation
            {
                s_expectedT = 443;
                CallingConventionTest_Inner<short>(443);
            }
        }
        static void CallingConventionTest_Inner<ARG>(ARG val)
        {
            var t = TypeOf.CCT_CCTester.MakeGenericType(typeof(ARG));
            IBase<ARG> o = (IBase<ARG>)Activator.CreateInstance(t);
            SimpleFuncCaller<ARG>(o, val);
            CoolFuncCaller<ARG>(o, val);
        }
        static unsafe void SimpleFuncCaller<T>(IBase<T> obj, T tval)
        {
            obj.SimpleFunc(tval);
        }
        static unsafe void CoolFuncCaller<T>(IBase<T> obj, T tval)
        {
            T brt_default = default(T);
            int* somePtr = (int*)159;
            T t_copy = tval;

            T retVal = obj.VirtualCoolFunc("Hello", new Bar<T> { _value = 1 }, new Foo<T> { _value = 2 }, new T[] { tval }, t_copy, ref tval, ref brt_default, 123, (int*)456, (int**)789, ref somePtr);
            Assert.AreEqual(tval, default(T));
            Assert.AreEqual(brt_default, t_copy);
            Assert.AreEqual(retVal, t_copy);
            Assert.IsTrue(somePtr == (int*)456);
        }
    }
}

namespace DynamicInvoke
{
    public class Type1<T>
    {
        T _myField;
        public Type1(T t) { _myField = t; }
        public T GetMyField() { return _myField; }
        public override string ToString() { return "Type1<" + typeof(T) + ">._myField = " + _myField.ToString(); }
    }
    public class Type2<T>
    {
        T _myField;
        public Type2(T t) { _myField = t; }
        public override string ToString() { return "Type2<" + typeof(T) + ">._myField = " + _myField.ToString(); }
    }
    public class Type3<T>
    {
        T[] _myField;
        public Type3(T t1, T t2, T t3) { _myField = new T[] { t1, t2, t3 }; }
        public T Get_A_T(int index) { return _myField[index]; }
    }
    public class TestType<T, U, V>
    {
        public string SimpleMethod1()
        {
            return "SimpleMethod1";
        }
        public string SimpleMethod2(int a, string b, object c, List<float> d)
        {
            string result = "SimpleMethod2(" + a + "," + b + "," + c;
            foreach (float f in d) result += "," + f;
            return result + ")";
        }
        public T Method0(T t, ref string resultStr)
        {
            resultStr = "Method0-" + t.ToString();
            return t;
        }
        public void Method1(T t1, ref T t2)
        {
            t2 = t1;
        }

        public Type1<T> Method2(Type2<T> l_t, T t, ref string resultStr)
        {
            resultStr = l_t.ToString();
            return new Type1<T>(t);
        }
        public Type1<U> Method3(Type2<U> l_u, U u, ref string resultStr)
        {
            resultStr = l_u.ToString();
            return new Type1<U>(u);
        }

        public KeyValuePair<Type3<T>, U> ComplexMethod(KeyValuePair<Type1<T[]>, U> kvp)
        {
            Type1<T[]> key = kvp.Key;
            T[] array = key.GetMyField();
            return new KeyValuePair<Type3<T>, U>(new Type3<T>(array[0], array[1], array[2]), kvp.Value);
        }
    }

    public class Test
    {
        [TestMethod]
        public static void TestDynamicInvoke()
        {
            TestDynamicInvoke_Inner<string>("123", "456");
            TestDynamicInvoke_Inner<char>('a', 'b');
            TestDynamicInvoke_Inner<short>((short)111, (short)222);

            TestDynamicInvoke_Inner<float>((float)3.3f, (float)4.4f);
            TestDynamicInvoke_Inner<double>((double)5.55, (double)6.66);
        }
        public static void TestDynamicInvoke_Inner<T>(T argParam1, T argParam2)
        {
            string argParam1Str = argParam1.ToString();
            string argParam2Str = argParam2.ToString();

            var t = TypeOf.DI_TestType.MakeGenericType(typeof(T), TypeOf.String, /* Use int32 here to force usage of the universal template*/ TypeOf.Int32);
            var o = Activator.CreateInstance(t);

            // SimpleMethod1
            {
                MethodInfo simpleMethod1 = t.GetTypeInfo().GetDeclaredMethod("SimpleMethod1");
                string result = (string)simpleMethod1.Invoke(o, null);
                Assert.AreEqual(result, "SimpleMethod1");

                Delegate simpleMethod1Del = simpleMethod1.CreateDelegate(typeof(Func<string>), o);
                result = (string)simpleMethod1Del.DynamicInvoke(null);
                Assert.AreEqual(result, "SimpleMethod1");
            }

            // SimpleMethod2
            {
                MethodInfo simpleMethod2 = t.GetTypeInfo().GetDeclaredMethod("SimpleMethod2");
                object[] args = new object[] {
                    123,
                    "456",
                    new Dictionary<object, string>(),
                    new List<float>(new float[]{1.2f, 3.4f, 5.6f})
                };

                string result = (string)simpleMethod2.Invoke(o, args);
                Assert.AreEqual(result, "SimpleMethod2(123,456,System.Collections.Generic.Dictionary`2[System.Object,System.String],1.2,3.4,5.6)");

                Delegate simpleMethod2Del = simpleMethod2.CreateDelegate(typeof(Func<int, string, Dictionary<object, string>, List<float>, string>), o);
                result = (string)simpleMethod2Del.DynamicInvoke(args);
                Assert.AreEqual(result, "SimpleMethod2(123,456,System.Collections.Generic.Dictionary`2[System.Object,System.String],1.2,3.4,5.6)");
            }

            // Method0
            {
                MethodInfo method0 = t.GetTypeInfo().GetDeclaredMethod("Method0");
                string resultStr = null;
                object[] args = new object[] { argParam1, resultStr };

                string result = method0.Invoke(o, args).ToString();
                resultStr = (string)args[1];
                Assert.AreEqual(resultStr, "Method0-" + argParam1Str);
                Assert.AreEqual(result, argParam1Str);
            }

            // Method1
            {
                MethodInfo method1 = t.GetTypeInfo().GetDeclaredMethod("Method1");
                object[] args = new object[] { argParam1, argParam2 };

                method1.Invoke(o, args);
                Assert.AreEqual(args[0].ToString(), args[1].ToString());
                Assert.AreEqual(args[0], args[1]);
                Assert.AreEqual(args[1], argParam1);
            }

            // Method2 and Method3
            {
                MethodInfo method2 = t.GetTypeInfo().GetDeclaredMethod("Method2");
                MethodInfo method3 = t.GetTypeInfo().GetDeclaredMethod("Method3");

                var args_for_method2 = new object[] { new Type2<T>(argParam1), argParam2, "" };
                var args_for_method3 = new object[] { new Type2<string>("hello"), "myTest", "" };
                string result_of_method2 = method2.Invoke(o, args_for_method2).ToString();
                string result_of_method3 = method3.Invoke(o, args_for_method3).ToString();

                string resultStr_method2 = args_for_method2[2].ToString();
                string resultStr_method3 = args_for_method3[2].ToString();
                Assert.AreEqual(resultStr_method2, "Type2<" + typeof(T) + ">._myField = " + argParam1.ToString());
                Assert.AreEqual(resultStr_method3, "Type2<System.String>._myField = hello");

                Assert.AreEqual(result_of_method2, "Type1<" + typeof(T) + ">._myField = " + argParam2.ToString());
                Assert.AreEqual(result_of_method3, "Type1<System.String>._myField = myTest");
            }

            // ComplexMethod
            {
                MethodInfo complex_method = t.GetTypeInfo().GetDeclaredMethod("ComplexMethod");

                Type1<T[]> myType1 = new Type1<T[]>(new T[] { argParam1, argParam2, argParam1 });
                object result = complex_method.Invoke(o, new object[] { new KeyValuePair<Type1<T[]>, string>(myType1, "hello") });

                KeyValuePair<Type3<T>, String> resultAsKvp = (KeyValuePair<Type3<T>, String>) result;

                Assert.AreEqual(resultAsKvp.Key.Get_A_T(0), argParam1);
                Assert.AreEqual(resultAsKvp.Key.Get_A_T(1), argParam2);
                Assert.AreEqual(resultAsKvp.Key.Get_A_T(2), argParam1);
                Assert.AreEqual(resultAsKvp.Value, "hello");
            }
        }
    }
}

namespace TypeLayout
{
    public struct GenStructStatic<X, Y, Z>
    {
        public X x;
        public Y y;
        public Z z;
    }

    public struct GenStructDynamic<X, Y, Z>
    {
        public X x;
        public Y y;
        public Z z;

        // This forces recursive type layout to ensure that we come up with a sensible result.
        public static GenStructDynamic<X, Y, Z> test;
    }

    public class BaseType
    {
        public float _f1;
        public string _f2;
    }

    public class GenClassStatic<X, Y, Z> : BaseType
    {
        public X x;
        public Y y;
        public Z z;
    }

    public class GenClassDynamic<X, Y, Z> : BaseType
    {
        public X x;
        public Y y;
        public Z z;
    }

    public abstract class Base
    {
        public abstract Type GetTypeOfArray();
    }

    public class MyArray<T> : Base
    {
        public T[] t = new T[10];

        public override Type GetTypeOfArray()
        {
            return t.GetType();
        }
    }


    public class Test
    {
        public static GenStructStatic<sbyte, sbyte, sbyte> s_staticStruct;
        public static GenStructDynamic<sbyte, sbyte, sbyte> s_dynamicStruct;

        public static GenClassStatic<sbyte, sbyte, sbyte> s_staticClass = new GenClassStatic<sbyte,sbyte,sbyte>();
        public static GenClassDynamic<sbyte, sbyte, sbyte> s_dynamicClass = new GenClassDynamic<sbyte,sbyte,sbyte>();

        public static MyArray<sbyte> s_test = new MyArray<sbyte>();

        public static void AssertTypesSimilar(Type left, Type right)
        {
#if INTERNAL_CONTRACTS
            int sizeLeft, sizeRight, alignmentLeft, alignmentRight;
            RuntimeTypeHandle rthLeft = left.TypeHandle;
            RuntimeTypeHandle rthRight = right.TypeHandle;
            Internal.Runtime.TypeLoader.TypeLoaderEnvironment.GetFieldAlignmentAndSize(rthLeft, out alignmentLeft, out sizeLeft);
            Internal.Runtime.TypeLoader.TypeLoaderEnvironment.GetFieldAlignmentAndSize(rthRight, out alignmentRight, out sizeRight);
            Assert.AreEqual(sizeLeft, sizeRight);
            Assert.AreEqual(alignmentLeft, alignmentRight);
#endif
        }

        public unsafe static void AssertSameGCDesc(Type left, Type right)
        {
            RuntimeTypeHandle rthLeft = left.TypeHandle;
            RuntimeTypeHandle rthRight = right.TypeHandle;

            void** ptrLeft = *(void***)&rthLeft - 1;
            void** ptrRight = *(void***)&rthRight - 1;

            long leftVal = (long)*ptrLeft--;
            long rightVal = (long)*ptrRight--;

            Assert.AreEqual(leftVal, rightVal);

            int count = leftVal > 0 ? (int)leftVal * 2 : -(int)leftVal * 2 - 1;

            for (int i = 0; i < count; i++)
                Assert.AreEqual(new IntPtr(*ptrLeft--), new IntPtr(*ptrRight--));
        }

        [TestMethod]
        public static void TestTypeGCDescs()
        {
            BaseType bt = new BaseType();
            bt._f1 = 1;
            bt._f2 = new String('c', 2);

            s_test.t = null;

            s_staticClass.x = s_dynamicClass.x;
            s_staticClass.y = s_dynamicClass.y;
            s_staticClass.z = s_dynamicClass.z;

            GenStructDynamic<sbyte, sbyte, sbyte>.test.x = 0;

            Type staticType = null;
            Type staticArrayType = null;
            Type dynamicType = null;
            Type innerType = null;
            Type arrayType = null;
            Base o = null;

            staticType = typeof(GenStructStatic<bool, GenStructStatic<object, object, bool>, object>);
            staticArrayType = typeof(GenStructStatic<bool, GenStructStatic<object, object, bool>, object>[]);

            innerType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Object, TypeOf.Object, TypeOf.Bool);

            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, innerType, TypeOf.Object);

            arrayType = typeof(MyArray<>).MakeGenericType(dynamicType);
            o = (Base)Activator.CreateInstance(arrayType);

            AssertSameGCDesc(staticType, dynamicType);
            AssertSameGCDesc(staticArrayType, o.GetTypeOfArray());


            staticType = typeof(GenStructStatic<bool, object, bool>);
            staticArrayType = typeof(GenStructStatic<bool, object, bool>[]);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Object, TypeOf.Bool);

            arrayType = typeof(MyArray<>).MakeGenericType(dynamicType);
            o = (Base)Activator.CreateInstance(arrayType);

            AssertSameGCDesc(staticType, dynamicType);
            AssertSameGCDesc(staticArrayType, o.GetTypeOfArray());


            staticType = typeof(GenStructStatic<object, bool, short>);
            staticArrayType = typeof(GenStructStatic<object, bool, short>[]);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Object, TypeOf.Bool, TypeOf.Int16);

            arrayType = typeof(MyArray<>).MakeGenericType(dynamicType);
            o = (Base)Activator.CreateInstance(arrayType);

            AssertSameGCDesc(staticType, dynamicType);
            AssertSameGCDesc(staticArrayType, o.GetTypeOfArray());

            staticType = typeof(GenStructStatic<bool, bool, object>);
            staticArrayType = typeof(GenStructStatic<bool, bool, object>[]);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Bool, TypeOf.Object);


            arrayType = typeof(MyArray<>).MakeGenericType(dynamicType);
            o = (Base)Activator.CreateInstance(arrayType);

            AssertSameGCDesc(staticType, dynamicType);
            AssertSameGCDesc(staticArrayType, o.GetTypeOfArray());




            staticType = typeof(GenClassStatic<bool, object, bool>);
            staticArrayType = typeof(GenClassStatic<bool, object, bool>[]);
            dynamicType = typeof(GenClassDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Object, TypeOf.Bool);

            arrayType = typeof(MyArray<>).MakeGenericType(dynamicType);
            o = (Base)Activator.CreateInstance(arrayType);

            AssertSameGCDesc(staticType, dynamicType);
            AssertSameGCDesc(staticArrayType, o.GetTypeOfArray());

            staticType = typeof(GenClassStatic<object, bool, short>);
            staticArrayType = typeof(GenClassStatic<object, bool, short>[]);
            dynamicType = typeof(GenClassDynamic<,,>).MakeGenericType(TypeOf.Object, TypeOf.Bool, TypeOf.Int16);
            arrayType = typeof(MyArray<>).MakeGenericType(dynamicType);
            o = (Base)Activator.CreateInstance(arrayType);

            AssertSameGCDesc(staticType, dynamicType);
            AssertSameGCDesc(staticArrayType, o.GetTypeOfArray());

            staticType = typeof(GenClassStatic<bool, bool, object>);
            staticArrayType = typeof(GenClassStatic<bool, bool, object>[]);
            dynamicType = typeof(GenClassDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Bool, TypeOf.Object);

            arrayType = typeof(MyArray<>).MakeGenericType(dynamicType);
            o = (Base)Activator.CreateInstance(arrayType);

            AssertSameGCDesc(staticType, dynamicType);
            AssertSameGCDesc(staticArrayType, o.GetTypeOfArray());

        }


        [TestMethod]
        public static void StructsOfPrimitives()
        {
            // Test type sizes for structs of primitive types

            // Ensure the reducer can't get rid of the x,y,z fields from these types.
            s_dynamicStruct.x = s_staticStruct.x;
            s_dynamicStruct.y = s_staticStruct.y;
            s_dynamicStruct.z = s_staticStruct.z;


            Type staticType = null;
            Type dynamicType = null;

            // All permutation of bool, short, int and double across 3 fields

            // top level bool
            // mid level bool
            staticType = typeof(GenStructStatic<bool, bool, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Bool, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<bool, bool, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Bool, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<bool, bool, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Bool, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<bool, bool, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Bool, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);

            // mid level short
            staticType = typeof(GenStructStatic<bool, short, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Int16, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<bool, short, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Int16, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<bool, short, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Int16, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<bool, short, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Int16, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);

            // mid level int
            staticType = typeof(GenStructStatic<bool, int, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Int32, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<bool, int, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Int32, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<bool, int, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Int32, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<bool, int, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Int32, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);

            // mid level double
            staticType = typeof(GenStructStatic<bool, double, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Double, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<bool, double, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Double, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<bool, double, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Double, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<bool, double, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Bool, TypeOf.Double, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);

            // top level short
            // mid level bool
            staticType = typeof(GenStructStatic<short, bool, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Bool, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<short, bool, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Bool, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<short, bool, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Bool, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<short, bool, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Bool, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);

            // mid level short
            staticType = typeof(GenStructStatic<short, short, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Int16, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<short, short, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Int16, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<short, short, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Int16, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<short, short, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Int16, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);

            // mid level int
            staticType = typeof(GenStructStatic<short, int, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Int32, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<short, int, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Int32, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<short, int, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Int32, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<short, int, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Int32, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);

            // mid level double
            staticType = typeof(GenStructStatic<short, double, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Double, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<short, double, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Double, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<short, double, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Double, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<short, double, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int16, TypeOf.Double, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);

            // top level int
            // mid level bool
            staticType = typeof(GenStructStatic<int, bool, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Bool, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<int, bool, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Bool, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<int, bool, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Bool, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<int, bool, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Bool, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);

            // mid level short
            staticType = typeof(GenStructStatic<int, short, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Int16, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<int, short, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Int16, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<int, short, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Int16, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<int, short, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Int16, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);

            // mid level int
            staticType = typeof(GenStructStatic<int, int, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Int32, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<int, int, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Int32, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<int, int, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Int32, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<int, int, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Int32, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);

            // mid level double
            staticType = typeof(GenStructStatic<int, double, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Double, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<int, double, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Double, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<int, double, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Double, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<int, double, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Double, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);

            // top level double
            // mid level bool
            staticType = typeof(GenStructStatic<double, bool, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Bool, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<double, bool, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Bool, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<double, bool, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Bool, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<double, bool, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Bool, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);

            // mid level short
            staticType = typeof(GenStructStatic<double, short, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Int16, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<double, short, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Int16, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<double, short, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Int16, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<double, short, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Int16, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);

            // mid level int
            staticType = typeof(GenStructStatic<double, int, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Int32, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<double, int, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Int32, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<double, int, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Int32, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<double, int, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Int32, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);

            // mid level double
            staticType = typeof(GenStructStatic<double, double, bool>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Double, TypeOf.Bool);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<double, double, short>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Double, TypeOf.Int16);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<double, double, int>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Double, TypeOf.Int32);
            AssertTypesSimilar(staticType, dynamicType);
            staticType = typeof(GenStructStatic<double, double, double>);
            dynamicType = typeof(GenStructDynamic<,,>).MakeGenericType(TypeOf.Double, TypeOf.Double, TypeOf.Double);
            AssertTypesSimilar(staticType, dynamicType);
        }
    }
}

namespace ActivatorCreateInstance
{
    public class ReferenceType
    {
        string _field;
        public ReferenceType() { _field = "ReferenceType.ctor"; }
        public override string ToString() { return _field; }
    }
    public class GenReferenceType<T>
    {
        string _field;
        public GenReferenceType() { _field = "GenReferenceType<" + typeof(T) + ">.ctor"; }
        public override string ToString() { return _field; }
    }
    public class ReferenceTypeNoDefaultCtor
    {
        string _field;
        public ReferenceTypeNoDefaultCtor(int param1) { _field = "ReferenceTypeNoDefaultCtor.ctor"; }
        public override string ToString() { return _field; }
    }
    public class GenReferenceTypeNoDefaultCtor<T>
    {
        string _field;
        public GenReferenceTypeNoDefaultCtor(bool param1) { _field = "GenReferenceTypeNoDefaultCtor<" + typeof(T) + ">.ctor"; }
        public override string ToString() { return _field; }
    }
    public struct AValueType
    {
        int _a;
        double _b;
        object _c;
        public AValueType(int param) { _a = 1; _b = 2.0; _c = new object(); }
        public override string ToString() { return "AValueType.ctor" + _a + _b + _c; }
    }
    public struct AGenValueType<T>
    {
        T _a;
        object _c;
        public AGenValueType(double param) { _a = default(T); _c = "3"; }
        public override string ToString() { return "AGenValueType<" + typeof(T) + ">.ctor" + _a + _c; }
    }


    public class Base
    {
        public virtual string Func() { return null; }
    }
    public class ACI_Instantiator<T, U> : Base
    {
        public override string Func()
        {
            T t = Activator.CreateInstance<T>();
            t = Activator.CreateInstance<T>();
            return "ACI_Instantiator: " + typeof(T) + " = " + t.ToString();
        }
    }
    public class NEW_Instantiator<T, U> : Base
        where T : new()
    {
        public override string Func()
        {
            T t = new T();
            t = new T();
            return "NEW_Instantiator: " + typeof(T) + " = " + t.ToString();
        }
    }

    public class Test
    {
        [TestMethod]
        public static void TestCreateInstance()
        {
            TestActivatorCreateInstance_Inner(TypeOf.Short, "0");
            TestActivatorCreateInstance_Inner(TypeOf.Int32, "0");
            TestActivatorCreateInstance_Inner(TypeOf.Long, "0");
            TestActivatorCreateInstance_Inner(TypeOf.Float, "0");
            TestActivatorCreateInstance_Inner(TypeOf.Double, "0");

            TestActivatorCreateInstance_Inner(typeof(ReferenceType), "ReferenceType.ctor");
            TestActivatorCreateInstance_Inner(TypeOf.ACI_GenReferenceType.MakeGenericType(TypeOf.String), "GenReferenceType<System.String>.ctor");
            TestActivatorCreateInstance_Inner(TypeOf.ACI_GenReferenceType.MakeGenericType(TypeOf.Double), "GenReferenceType<System.Double>.ctor");
            TestActivatorCreateInstance_Inner(typeof(GenReferenceType<CommonType1>), "GenReferenceType<CommonType1>.ctor");

            TestActivatorCreateInstance_Inner(typeof(ReferenceTypeNoDefaultCtor), null, true);
            TestActivatorCreateInstance_Inner(TypeOf.ACI_GenReferenceTypeNoDefaultCtor.MakeGenericType(TypeOf.String), null, true);
            TestActivatorCreateInstance_Inner(TypeOf.ACI_GenReferenceTypeNoDefaultCtor.MakeGenericType(TypeOf.Double), null, true);
            TestActivatorCreateInstance_Inner(typeof(GenReferenceTypeNoDefaultCtor<CommonType1>), null, true);

            TestActivatorCreateInstance_Inner(typeof(AValueType), "AValueType.ctor00");
            TestActivatorCreateInstance_Inner(TypeOf.ACI_AGenValueType.MakeGenericType(TypeOf.String), "AGenValueType<System.String>.ctor");
            TestActivatorCreateInstance_Inner(TypeOf.ACI_AGenValueType.MakeGenericType(TypeOf.Double), "AGenValueType<System.Double>.ctor0");
#if USC
            TestActivatorCreateInstance_Inner(typeof(AGenValueType<CommonType1>), "AGenValueType<CommonType1>.ctorCommonType1");
#else
            TestActivatorCreateInstance_Inner(typeof(AGenValueType<CommonType1>), "AGenValueType<CommonType1>.ctor");
#endif
        }
        static void TestActivatorCreateInstance_Inner(Type typeArg, string toStrVal, bool expectMissingMemberException = false)
        {
            Type t;
            Base o;
            string result1, result2;

            string expectedResult1 = "ACI_Instantiator: " + typeArg.ToString() + " = " + toStrVal;
            string expectedResult2 = "NEW_Instantiator: " + typeArg.ToString() + " = " + toStrVal;

            try
            {
                t = TypeOf.ACI_ACI_Instantiator.MakeGenericType(typeArg, TypeOf.Short);
                o = (Base)Activator.CreateInstance(t);
                result1 = o.Func();
                Assert.AreEqual(expectedResult1, result1);

                Assert.IsFalse(expectMissingMemberException);
            }
            catch (System.MissingMemberException)
            {
                Assert.IsTrue(expectMissingMemberException);
            }

            if (expectMissingMemberException)
            {
                // Types with no default constructor will violate the constraint on "T".
                return;
            }

            t = TypeOf.ACI_NEW_Instantiator.MakeGenericType(typeArg, TypeOf.Short);
            o = (Base)Activator.CreateInstance(t);
            result2 = o.Func();
            Assert.AreEqual(expectedResult2, result2);
        }
    }
}

namespace MultiThreadUSCCall
{
    public class TestType<T>
    {
        public string Func(T t)
        {
            return "Func(" + typeof(T) + ")";
        }
    }

    public class Test
    {
        static void DoTest()
        {
            Task[] tasks = new Task[50];
            for (int i = 0; i < 50; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < 5; j++)
                    {
                        var t = typeof(TestType<>).MakeGenericType(TypeOf.Short);
                        object o = Activator.CreateInstance(t);
                        MethodInfo mi = t.GetTypeInfo().GetDeclaredMethod("Func");
                        string s = (string)mi.Invoke(o, new object[] { null });
                        Assert.AreEqual("Func(System.Int16)", s);

                        t = null;
                        o = null;
                        mi = null;
                        s = null;
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                    }
                });
            }
            Task.WaitAll(tasks);
        }

        [TestMethod]
        public static void CallsWithGCCollects()
        {
            for (int i = 0; i < 5; i++)
            {
                DoTest();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
    }
}

namespace Heuristics
{
    public struct MyStruct<T>
    {
        public override string ToString()
        {
            return typeof(T).ToString();
        }
    }

    //
    // Test USG reflection heuristics by using an rd.xml entry to root the type.
    // Only look up the type with Type.GetType(string) so it is never statically referenced.
    //
    public struct OnlyUseViaReflection<T>
    {
        T _a;

        public OnlyUseViaReflection(int dummyToMakeCscPass) { _a = default(T); }
        public override string ToString() { return "OnlyUseViaReflection<" + typeof(T) + ">.ctor" + _a; }
        public string GenericMethodNotCalledStatically<U>(U u)
        {
            return typeof(U).ToString();
        }
    }

    public class OnlyUseViaReflectionGenMethod
    {
        public string GenericMethodNotCalledStatically<T>(T t)
        {
            return typeof(T).ToString();
        }
    }

    public class TestHeuristics
    {
        [TestMethod]
        public static void TestReflectionHeuristics()
        {
            Type t;
            t = TypeOf.OnlyUseViaReflection.MakeGenericType(TypeOf.Double);
            Object o = Activator.CreateInstance(t);
            Assert.IsTrue(o != null);

            t = TypeOf.OnlyUseViaReflectionGenMethod;
            Object obj = Activator.CreateInstance(t);
            Assert.IsTrue(obj != null);
        }

        //
        // Try instantiating all reflectable generics in this test app over a specific value type to ensure
        // everything marked reflectable works with USG
        //
        //[TestMethod]
#if false
        public static void TestReflectionHeuristicsAllGenerics()
        {
            var assembly = typeof(TestHeuristics).GetTypeInfo().Assembly;

            foreach (var t in assembly.DefinedTypes)
            {
                if (t.IsGenericType)
                {
                    Assert.IsTrue(t.IsGenericTypeDefinition);
                    int arity = t.GenericTypeParameters.Length;
                    Console.WriteLine("Type: {0}  Arity: {1}", t.ToString(), arity);

                    bool hasTypeConstraints = false;

                    foreach (var tp in t.GenericTypeParameters)
                    {
                        if (tp.GetTypeInfo().GetGenericParameterConstraints().Length > 0)
                            hasTypeConstraints = true;
                    }

                    if (hasTypeConstraints)
                    {
                        Console.WriteLine("Skipping type - it has at least one type parameter constraint (that forces a specific base type)");
                        continue;
                    }

                    Type[] args = new Type[arity];

                    for (int i = 0; i < args.Length; i++)
                    {
                        args[i] = typeof(MyStruct<int>);
                    }
                    Type instantiated = t.MakeGenericType(args);

                    Assert.IsTrue(instantiated != null);
                }
            }
        }
#endif
    }
}


namespace ArrayVarianceTest
{
    public class GenType<T, U>
    {
        public string RunTest(object input_obj, int testId)
        {
            // These typecases will cause RhTypeCast_IsInstanceOfInterface to execute,
            // which will check for variance equalities between types.
            IEnumerable<T> source = input_obj as IEnumerable<T>;
            ICollection<T> collection = source as ICollection<T>;

            switch (testId)
            {
                case 0:
                    {
                        return collection == null ? "NULL" : (collection.Count + " items in ICollection<" + typeof(T).Name + ">");
                    }

                case 1:
                    {
                        if (source == null) return "NULL";
                        int count = 0;
                        foreach (T item in source)
                            count++;
                        return (count + " items in IEnumerable<" + typeof(T).Name + ">");
                    }
            }

            return null;
        }
    }
    public enum MyTestEnum : int
    {
        MTE_1, MTE_2, MTE_3,
    }

    public class Test
    {
        [TestMethod]
        public static void RunTest()
        {
            ICollection<string> coll_str = new string[] { "abc", "def" };
            int[] int_array = new int[] { 1, 2, 3 };
            MyTestEnum[] enum_array = new MyTestEnum[] { MyTestEnum.MTE_1, MyTestEnum.MTE_1, MyTestEnum.MTE_2, MyTestEnum.MTE_2, MyTestEnum.MTE_3, MyTestEnum.MTE_3, };

            Type t = TypeOf.AVT_GenType.MakeGenericType(TypeOf.CommonType4, TypeOf.Short);
            object o = Activator.CreateInstance(t);
            MethodInfo mi = t.GetTypeInfo().GetDeclaredMethod("RunTest");
            Assert.AreEqual("NULL", mi.Invoke(o, new object[] { coll_str, 0 }));
            Assert.AreEqual("NULL", mi.Invoke(o, new object[] { int_array, 0 }));
            Assert.AreEqual("NULL", mi.Invoke(o, new object[] { coll_str, 1 }));
            Assert.AreEqual("NULL", mi.Invoke(o, new object[] { int_array, 1 }));

            t = TypeOf.AVT_GenType.MakeGenericType(TypeOf.String, TypeOf.Short);
            o = Activator.CreateInstance(t);
            mi = t.GetTypeInfo().GetDeclaredMethod("RunTest");
            Assert.AreEqual("2 items in ICollection<String>", mi.Invoke(o, new object[] { coll_str, 0 }));
            Assert.AreEqual("NULL", mi.Invoke(o, new object[] { int_array, 0 }));
            Assert.AreEqual("2 items in IEnumerable<String>", mi.Invoke(o, new object[] { coll_str, 1 }));
            Assert.AreEqual("NULL", mi.Invoke(o, new object[] { int_array, 1 }));

            t = TypeOf.AVT_GenType.MakeGenericType(TypeOf.Object, TypeOf.Short);
            o = Activator.CreateInstance(t);
            mi = t.GetTypeInfo().GetDeclaredMethod("RunTest");
            Assert.AreEqual("2 items in ICollection<Object>", mi.Invoke(o, new object[] { coll_str, 0 }));
            Assert.AreEqual("NULL", mi.Invoke(o, new object[] { int_array, 0 }));
            Assert.AreEqual("2 items in IEnumerable<Object>", mi.Invoke(o, new object[] { coll_str, 1 }));
            Assert.AreEqual("NULL", mi.Invoke(o, new object[] { int_array, 1 }));

            t = TypeOf.AVT_GenType.MakeGenericType(TypeOf.Int32, TypeOf.Short);
            o = Activator.CreateInstance(t);
            mi = t.GetTypeInfo().GetDeclaredMethod("RunTest");
            Assert.AreEqual("NULL", mi.Invoke(o, new object[] { coll_str, 0 }));
            Assert.AreEqual("3 items in ICollection<Int32>", mi.Invoke(o, new object[] { int_array, 0 }));
            Assert.AreEqual("6 items in ICollection<Int32>", mi.Invoke(o, new object[] { enum_array, 0 }));
            Assert.AreEqual("NULL", mi.Invoke(o, new object[] { coll_str, 1 }));
            Assert.AreEqual("3 items in IEnumerable<Int32>", mi.Invoke(o, new object[] { int_array, 1 }));
            Assert.AreEqual("6 items in IEnumerable<Int32>", mi.Invoke(o, new object[] { enum_array, 1 }));

            t = TypeOf.AVT_GenType.MakeGenericType(typeof(MyTestEnum), TypeOf.Short);
            o = Activator.CreateInstance(t);
            mi = t.GetTypeInfo().GetDeclaredMethod("RunTest");
            Assert.AreEqual("NULL", mi.Invoke(o, new object[] { coll_str, 0 }));
            Assert.AreEqual("3 items in ICollection<MyTestEnum>", mi.Invoke(o, new object[] { int_array, 0 }));
            Assert.AreEqual("6 items in ICollection<MyTestEnum>", mi.Invoke(o, new object[] { enum_array, 0 }));
            Assert.AreEqual("NULL", mi.Invoke(o, new object[] { coll_str, 1 }));
            Assert.AreEqual("3 items in IEnumerable<MyTestEnum>", mi.Invoke(o, new object[] { int_array, 1 }));
            Assert.AreEqual("6 items in IEnumerable<MyTestEnum>", mi.Invoke(o, new object[] { enum_array, 1 }));
        }
    }
}

namespace IsInstTest
{
    public interface IBase { }
    public interface IObject : IBase { string Func(); }
    public class MyObject : IObject
    {
        string _id;

        public MyObject(string id) { _id = id; }
        public string Func() { return this.ToString(); }
        public override string ToString() { return "MyObject{" + _id + "}"; }
    }
    public class MyStruct : IObject
    {
        string _id;

        public MyStruct(string id) { _id = id; }
        public string Func() { return this.ToString(); }
        public override string ToString() { return "MyStruct{" + _id + "}"; }
    }

    public class ObjectActivator
    {
        public object Activate(string id, bool activateTheStruct)
        {
            return activateTheStruct ? (object)new MyStruct(id) : (object)new MyObject(id);
        }
    }

    public class TestType
    {
        public T ActivateObject_IsInstT<T, U>(string id, bool activateTheStruct) where T : class
        {
            object o = new ObjectActivator().Activate(id, activateTheStruct);
            T t = o as T;
            return t;
        }
        public T ActivateArray_IsInstT<T, U>(string id, bool activateTheStruct) where T : class
        {
            IObject[] array = new IObject[] {
                (IObject)new ObjectActivator().Activate(id, activateTheStruct),
                (IObject)new ObjectActivator().Activate(id, activateTheStruct),
                (IObject)new ObjectActivator().Activate(id, activateTheStruct)
            };
            T t = array as T;
            return t;
        }
    }

    public class TestRunner
    {
        [TestMethod]
        public static unsafe void RunIsInstAndCheckCastTest()
        {
            // Test isinst of an interface
            MethodInfo ActivateObject_IsInstT = typeof(TestType).GetTypeInfo().GetDeclaredMethod("ActivateObject_IsInstT").MakeGenericMethod(typeof(IObject), TypeOf.Double);
            IObject myObject1 = (IObject)ActivateObject_IsInstT.Invoke(new TestType(), new object[] { "1", false });
            Assert.AreEqual(myObject1.Func(), "MyObject{1}");
            IObject myObject2 = (IObject)ActivateObject_IsInstT.Invoke(new TestType(), new object[] { "2", true });
            Assert.AreEqual(myObject2.Func(), "MyStruct{2}");

            // Test isinst of a class
            ActivateObject_IsInstT = typeof(TestType).GetTypeInfo().GetDeclaredMethod("ActivateObject_IsInstT").MakeGenericMethod(typeof(MyObject), TypeOf.Double);
            IObject myObject3 = (IObject)ActivateObject_IsInstT.Invoke(new TestType(), new object[] { "3", false });
            Assert.AreEqual(myObject3.Func(), "MyObject{3}");
            IObject myObject4 = (IObject)ActivateObject_IsInstT.Invoke(new TestType(), new object[] { "4", true });
            Assert.IsTrue(myObject4 == null);

            // Test isinst of an array
            MethodInfo ActivateArray_IsInstT = typeof(TestType).GetTypeInfo().GetDeclaredMethod("ActivateArray_IsInstT").MakeGenericMethod(typeof(IBase[]), TypeOf.Double);
            IBase[] myArray1 = (IBase[])ActivateArray_IsInstT.Invoke(new TestType(), new object[] { "5", false });
            Assert.IsTrue(myArray1.Length == 3);
            Assert.AreEqual(((IObject)myArray1[0]).Func(), "MyObject{5}");
            Assert.AreEqual(((IObject)myArray1[1]).Func(), "MyObject{5}");
            Assert.AreEqual(((IObject)myArray1[2]).Func(), "MyObject{5}");
            IBase[] myArray2 = (IBase[])ActivateArray_IsInstT.Invoke(new TestType(), new object[] { "6", true });
            Assert.IsTrue(myArray2.Length == 3);
            Assert.AreEqual(((IObject)myArray2[0]).Func(), "MyStruct{6}");
            Assert.AreEqual(((IObject)myArray2[1]).Func(), "MyStruct{6}");
            Assert.AreEqual(((IObject)myArray2[2]).Func(), "MyStruct{6}");
        }
    }
}

namespace DelegateCallTest
{
    public interface IBar { }
    public class Bar : IBar
    {
        public Bar() { Console.WriteLine("BarCtor"); }
        public override string ToString() { return "BarInstance"; }
    }
    public class Foo
    {
        public string CallMethodThroughDelegate<T>(T tValue, Func<IBar, T, string> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            return action(new Bar(), tValue);
        }
        public string Method<T>(IBar i, T tValue)
        {
            string res = "Method<" + typeof(T) + ">, " + i.ToString() + ", " + tValue.ToString();
            return res;
        }
    }
    public class TestRunner
    {
        [TestMethod]
        public static unsafe void TestCallMethodThroughUsgDelegate()
        {
            Foo o = new Foo();

            MethodInfo Method = typeof(Foo).GetTypeInfo().GetDeclaredMethod("Method").MakeGenericMethod(TypeOf.Double);
            Type delType = typeof(Func<,,>).MakeGenericType(typeof(IBar), TypeOf.Double, TypeOf.String);
            Delegate d = Method.CreateDelegate(delType, o);

            MethodInfo CallMethodThroughDelegate = typeof(Foo).GetTypeInfo().GetDeclaredMethod("CallMethodThroughDelegate").MakeGenericMethod(TypeOf.Double);
            string res = (string)CallMethodThroughDelegate.Invoke(o, new object[] { 1.2, d });
            Assert.AreEqual(res, "Method<System.Double>, BarInstance, 1.2");
        }
    }
}

// Repro taken from a real app (System.Reactive framework). Bug in field layout caused crashes on x86.
namespace FieldLayoutBugRepro
{
    public abstract class CallerBase
    {
        public abstract string DoCall(object state, TimeSpan dueTime, object action);
    }
    public class Caller<TState> : CallerBase
    {
        public override string DoCall(object state, TimeSpan dueTime, object action)
        {
            BaseType obj = new DerivedType();
            return obj.Schedule<TState>((TState)state, dueTime, (Func<IInterface, TState, string>)action);
        }
    }

    public interface IInterface { }
    public class BaseType : IInterface
    {
        public virtual string Schedule<TState>(TState state, TimeSpan dueTime, Func<IInterface, TState, string> action) { return null; }
        public override string ToString() { return "BaseType"; }
    }
    public class DerivedType : BaseType
    {
        public override string Schedule<TState>(TState state, TimeSpan dueTime, Func<IInterface, TState, string> action)
        {
            // Root static typespecs:
            var t1 = typeof(ScheduledItem<TimeSpan>);
            var t2 = typeof(ScheduledItem<TimeSpan, StateProducer<EventPattern<string>>.State>);

            ScheduledItem<TimeSpan, TState> scheduledItem = new ScheduledItem<TimeSpan, TState>(this, state, action, dueTime);
            return ((ScheduledItem<TimeSpan>)scheduledItem).Execute();
        }
        public override string ToString()
        {
            return "DerivedType";
        }
    }

    public interface IMyComparer<T> { }
    public class MyComparer<T> : IMyComparer<T>
    {
        public override string ToString() { return "MyComparer<" + typeof(T) + ">"; }
    }

    public interface IScheduledItem<TAbsolute> { }
    public abstract class ScheduledItem<TAbsolute> : IScheduledItem<TAbsolute>, IComparable<ScheduledItem<TAbsolute>> where TAbsolute : IComparable<TAbsolute>
    {
        private readonly string _disposable = new String('c', 3);
        protected readonly TAbsolute _dueTime;
        protected readonly IMyComparer<TAbsolute> _comparer;

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected ScheduledItem(TAbsolute dueTime, IMyComparer<TAbsolute> comparer)
        {
            this._dueTime = dueTime;
            this._comparer = comparer;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int CompareTo(ScheduledItem<TAbsolute> other) { return 1; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string Execute() { return ExecuteCore(); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public abstract string ExecuteCore();
    }
    public class ScheduledItem<TAbsolute, TValue> : ScheduledItem<TAbsolute> where TAbsolute : IComparable<TAbsolute>
    {
        private readonly IInterface _scheduler;
        private readonly TValue _state;
        private readonly Func<IInterface, TValue, string> _action;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ScheduledItem(IInterface scheduler, TValue state, Func<IInterface, TValue, string> action, TAbsolute dueTime)
            : this(scheduler, state, action, dueTime, new MyComparer<TAbsolute>())
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ScheduledItem(IInterface scheduler, TValue state, Func<IInterface, TValue, string> action, TAbsolute dueTime, IMyComparer<TAbsolute> comparer)
            : base(dueTime, comparer)
        {
            this._scheduler = scheduler;
            this._state = state;
            this._action = action;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string ExecuteCore() { return this.ToString() + "=" + _action(_scheduler, _state); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string ToString() { return "ScheduledItem<" + typeof(TAbsolute) + "," + typeof(TValue) + ">{_dueTime=" + _dueTime + ",_comparer=" + _comparer + "}"; }
    }

    public abstract class StateProducerBase
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public abstract object GetState(string s1, string s2);
    }
    public class StateProducer<TSource> : StateProducerBase
    {
        public struct State
        {
            public string _s1;
            public string _s2;
            public State(string s1, string s2)
            {
                _s1 = s1;
                _s2 = s2;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public override string ToString() { return "StateProducer<" + typeof(TSource) + ">/State[" + _s1 + _s2 + "]"; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override object GetState(string s1, string s2) { return new State(s1, s2); }
    }
    public sealed class EventPattern<TEventArgs> { }

    public partial class Runner
    {
        [TestMethod]
        public static unsafe void EntryPoint()
        {
            Type targ1 = typeof(EventPattern<>).MakeGenericType(TypeOf.String);
            Type targ2 = typeof(StateProducer<>).MakeGenericType(targ1);
            Type targ3 = typeof(StateProducer<>).GetTypeInfo().GetDeclaredNestedType("State").MakeGenericType(targ1);
            Type delType = typeof(Func<,,>).MakeGenericType(typeof(IInterface), targ3, TypeOf.String);

            var state = ((StateProducerBase)Activator.CreateInstance(targ2)).GetState("abc", "def");
            TimeSpan dueTime = new TimeSpan(0x123);
            Delegate action = typeof(Runner).GetTypeInfo().GetDeclaredMethod("MyDelTarget").MakeGenericMethod(targ3).CreateDelegate(delType);

            Type callerType = typeof(Caller<>).MakeGenericType(targ3);
            CallerBase caller = (CallerBase)Activator.CreateInstance(callerType);

            string result = caller.DoCall(state, dueTime, action);
            Assert.AreEqual("ScheduledItem<System.TimeSpan,FieldLayoutBugRepro.StateProducer`1+State[FieldLayoutBugRepro.EventPattern`1[System.String]]>{_dueTime=00:00:00.0000291,_comparer=MyComparer<System.TimeSpan>}={scheduler=DerivedType,state=StateProducer<FieldLayoutBugRepro.EventPattern`1[System.String]>/State[abcdef]}", result);
        }

        public static string MyDelTarget<TState>(IInterface scheduler, TState state)
        {
            return "{scheduler=" + scheduler + ",state=" + state + "}";
        }
    }
}

namespace DelegateTest
{
    public class BaseType
    {
        public virtual Func<T, string> GVMethod1<T>(T t) { return null; }
        public virtual Func<T, string> GVMethod2<T>(T t) { return null; }
    }
    public class DerivedType : BaseType
    {
        public override Func<T, string> GVMethod1<T>(T t)
        {
            return new Func<T, string>((new GenType<T, string>().Method));
        }
        public override Func<T, string> GVMethod2<T>(T t)
        {
            return new Func<T, string>((new NonGenType().GenMethod<T, string>));
        }
    }
    public class GenType<T, U>
    {
        public string Method(T t)
        {
            return "GenType<" + t.GetType() + "," + typeof(U) + ">.Method{" + t + "," + default(U) + "}";
        }
    }
    public class NonGenType
    {
        public string GenMethod<T, U>(T t)
        {
            return "NonGenType.GenMethod<" + t.GetType() + "," + typeof(U) + ">{" + t + "," + default(U) + "}";
        }
    }


    public partial class TestRunner
    {
        [TestMethod]
        public static unsafe void TestMethodCellsWithUSGTargetsUsedOnNonUSGInstantiations()
        {
            // Root compatible normal canonical instantiations
            new GenType<double, object>();
            new NonGenType().GenMethod<double, object>(0);

            MethodInfo GVMethod1 = typeof(BaseType).GetTypeInfo().GetDeclaredMethod("GVMethod1").MakeGenericMethod(TypeOf.Double);
            Delegate del = (Delegate)GVMethod1.Invoke(new DerivedType(), new object[] { 12.34 });
            string result = (string)del.DynamicInvoke(new object[] { 56.79 });
            Assert.AreEqual("GenType<System.Double,System.String>.Method{56.79,}", result);

            MethodInfo GVMethod2 = typeof(BaseType).GetTypeInfo().GetDeclaredMethod("GVMethod2").MakeGenericMethod(TypeOf.Double);
            del = (Delegate)GVMethod2.Invoke(new DerivedType(), new object[] { 11.22 });
            result = (string)del.DynamicInvoke(new object[] { 88.99 });
            Assert.AreEqual("NonGenType.GenMethod<System.Double,System.String>{88.99,}", result);
        }
    }
}

namespace ArrayExceptionsTest
{
    public enum IntBasedEnum
    {

    }

    public enum ShortBasedEnum : short
    {

    }

    public abstract class BaseType
    {
        public abstract void TestSetExceptionRank1(object valToSet);
        public abstract void TestAddressOfExceptionRank1();
        public abstract void TestSetExceptionRank2(object valToSet);
        public abstract void TestAddressOfExceptionRank2();
        public abstract void TestSetExceptionRank3(object valToSet);
        public abstract void TestAddressOfExceptionRank3();
        public abstract void TestSetExceptionRank4(object valToSet);
        public abstract void TestAddressOfExceptionRank4();
    }

    public class DerivedType<T,U,V> : BaseType
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        void Func(ref U t)
        { }

        public override void TestSetExceptionRank1(object valToSet)
        {
            T[] tArray = new T[1];
            U[] uArray = (U[])(object)tArray;
            uArray[0] = (U)valToSet;
        }

        public override void TestAddressOfExceptionRank1()
        {
            T[] tArray = new T[1];
            U[] uArray = (U[])(object)tArray;
            Func(ref uArray[0]);
        }

        public override void TestSetExceptionRank2(object valToSet)
        {
            T[,] tArray = new T[1, 1];
            U[,] uArray = (U[,])(object)tArray;
            uArray[0, 0] = (U)valToSet;
        }

        public override void TestAddressOfExceptionRank2()
        {
            T[,] tArray = new T[1, 1];
            U[,] uArray = (U[,])(object)tArray;
            Func(ref uArray[0, 0]);
        }

        public override void TestSetExceptionRank3(object valToSet)
        {
            T[,,] tArray = new T[1, 1, 1];
            U[,,] uArray = (U[,,])(object)tArray;
            uArray[0, 0, 0] = (U)valToSet;
        }

        public override void TestAddressOfExceptionRank3()
        {
            T[,,] tArray = new T[1, 1, 1];
            U[,,] uArray = (U[,,])(object)tArray;
            Func(ref uArray[0, 0, 0]);
        }

        public override void TestSetExceptionRank4(object valToSet)
        {
            T[, ,,] tArray = new T[1, 1, 1, 1];
            U[, ,,] uArray = (U[, ,,])(object)tArray;
            uArray[0, 0, 0, 0] = (U)valToSet;
        }

        public override void TestAddressOfExceptionRank4()
        {
            T[, ,,] tArray = new T[1, 1, 1, 1];
            U[, ,,] uArray = (U[, ,,])(object)tArray;
            Func(ref uArray[0, 0, 0, 0]);
        }
    }

    public class Runner
    {
        static void RunIndividualTests(BaseType o, object setObject, bool expectedToThrow)
        {
            if (expectedToThrow)
            {
                Assert.Throws<ArrayTypeMismatchException>(() => { o.TestSetExceptionRank1(setObject); });
                Assert.Throws<ArrayTypeMismatchException>(() => { o.TestAddressOfExceptionRank1(); });
                Assert.Throws<ArrayTypeMismatchException>(() => { o.TestSetExceptionRank2(setObject); });
                Assert.Throws<ArrayTypeMismatchException>(() => { o.TestAddressOfExceptionRank2(); });
                Assert.Throws<ArrayTypeMismatchException>(() => { o.TestSetExceptionRank3(setObject); });
                Assert.Throws<ArrayTypeMismatchException>(() => { o.TestAddressOfExceptionRank3(); });
                Assert.Throws<ArrayTypeMismatchException>(() => { o.TestSetExceptionRank4(setObject); });
                Assert.Throws<ArrayTypeMismatchException>(() => { o.TestAddressOfExceptionRank4(); });
            }
            else
            {
                o.TestSetExceptionRank1(setObject);
                o.TestAddressOfExceptionRank1();
                o.TestSetExceptionRank2(setObject);
                o.TestAddressOfExceptionRank2();
                o.TestSetExceptionRank3(setObject);
                o.TestAddressOfExceptionRank3();
                o.TestSetExceptionRank4(setObject);
                o.TestAddressOfExceptionRank4();
            }
        }

        [TestMethod]
        public static unsafe void ArrayExceptionsTest_String_Object()
        {
            Type t = typeof(DerivedType<,,>).MakeGenericType(TypeOf.String, TypeOf.Object, TypeOf.Short);

            RunIndividualTests((BaseType)Activator.CreateInstance(t), new object(), true);
        }

        [TestMethod]
        public static unsafe void ArrayExceptionsTest_Int32_Int32()
        {
            Type t = typeof(DerivedType<,,>).MakeGenericType(TypeOf.Int32, TypeOf.Int32, TypeOf.Short);

            RunIndividualTests((BaseType)Activator.CreateInstance(t), (object)1024, false);
        }

        [TestMethod]
        public static unsafe void ArrayExceptionsTest_Int32_IntBasedEnum()
        {
            Type t = typeof(DerivedType<,,>).MakeGenericType(TypeOf.Int32, typeof(IntBasedEnum), TypeOf.Short);

            RunIndividualTests((BaseType)Activator.CreateInstance(t), (object)1024, false);
        }

        [TestMethod]
        public static unsafe void ArrayExceptionsTest_UInt32_Int32()
        {
            Type t = typeof(DerivedType<,,>).MakeGenericType(typeof(uint), TypeOf.Int32, TypeOf.Short);

            RunIndividualTests((BaseType)Activator.CreateInstance(t), (object)1024, false);
        }
    }
}

namespace UnboxAnyTests
{
    public enum IntBasedEnum
    {
        Val = 3
    }

    public enum ShortBasedEnum : short
    {
        Val = 17
    }

    public abstract class BaseType
    {
        public abstract object TestUnboxAnyAndRebox(object valToSet);
    }

    public class DerivedType<T,V> : BaseType
    {
        public static T m_t;

        public override object TestUnboxAnyAndRebox(object valToSet)
        {
            m_t = (T)valToSet;
            return (object)m_t;
        }
    }

    public class Runner
    {
        [TestMethod]
        public static unsafe void TestUnboxAnyToString()
        {
            Type t = typeof(DerivedType<,>).MakeGenericType(TypeOf.String, TypeOf.Short);
            BaseType o = (BaseType)Activator.CreateInstance(t);

            object tempObj = "TestString";

            Assert.AreEqual(tempObj, o.TestUnboxAnyAndRebox(tempObj));
            Assert.AreEqual(null, o.TestUnboxAnyAndRebox(null));
            Assert.Throws<InvalidCastException>(() => { o.TestUnboxAnyAndRebox(new object()); });
        }

        [TestMethod]
        public static unsafe void TestUnboxAnyToInt()
        {
            Type t = typeof(DerivedType<,>).MakeGenericType(TypeOf.Int32, TypeOf.Short);
            BaseType o = (BaseType)Activator.CreateInstance(t);

            Assert.AreEqual(43, (int)o.TestUnboxAnyAndRebox(43));
            Assert.AreEqual(IntBasedEnum.Val, (IntBasedEnum)o.TestUnboxAnyAndRebox(IntBasedEnum.Val));
            Assert.Throws<InvalidCastException>(() => { o.TestUnboxAnyAndRebox(new object()); });
            Assert.Throws<NullReferenceException>(() => { o.TestUnboxAnyAndRebox(null); });
        }

        [TestMethod]
        public static unsafe void TestUnboxAnyToIntBasedEnum()
        {
            Type t = typeof(DerivedType<,>).MakeGenericType(typeof(IntBasedEnum), TypeOf.Short);
            BaseType o = (BaseType)Activator.CreateInstance(t);

            Assert.AreEqual(43, (int)o.TestUnboxAnyAndRebox(43));
            Assert.AreEqual(IntBasedEnum.Val, (IntBasedEnum)o.TestUnboxAnyAndRebox(IntBasedEnum.Val));
            Assert.Throws<InvalidCastException>(() => { o.TestUnboxAnyAndRebox(new object()); });
            Assert.Throws<NullReferenceException>(() => { o.TestUnboxAnyAndRebox(null); });
        }

        [TestMethod]
        public static unsafe void TestUnboxAnyToNullableInt()
        {
            Type t = typeof(DerivedType<,>).MakeGenericType(typeof(int?), TypeOf.Short);
            BaseType o = (BaseType)Activator.CreateInstance(t);

            Assert.AreEqual(14, (int)o.TestUnboxAnyAndRebox(14));
            Assert.Throws<InvalidCastException>(() => { o.TestUnboxAnyAndRebox(IntBasedEnum.Val); });
            Assert.Throws<InvalidCastException>(() => { o.TestUnboxAnyAndRebox(new object()); });
            Assert.AreEqual(null, o.TestUnboxAnyAndRebox(null));
        }

        [TestMethod]
        public static unsafe void TestUnboxAnyToNullableIntBasedEnum()
        {
            Type t = typeof(DerivedType<,>).MakeGenericType(typeof(IntBasedEnum?), TypeOf.Short);
            BaseType o = (BaseType)Activator.CreateInstance(t);

            Assert.Throws<InvalidCastException>(() => { o.TestUnboxAnyAndRebox(14); });
            Assert.AreEqual(IntBasedEnum.Val, (IntBasedEnum)o.TestUnboxAnyAndRebox(IntBasedEnum.Val));
            Assert.Throws<InvalidCastException>(() => { o.TestUnboxAnyAndRebox(new object()); });
            Assert.AreEqual(null, o.TestUnboxAnyAndRebox(null));
        }

        [TestMethod]
        public static unsafe void TestUnboxAnyToShort_NonUSG()
        {
            // Test non-usg case for parallel verification
            BaseType o = new DerivedType<short,sbyte>();

            Assert.AreEqual(43, (short)o.TestUnboxAnyAndRebox((object)(short)43));
            Assert.AreEqual(ShortBasedEnum.Val, (ShortBasedEnum)o.TestUnboxAnyAndRebox(ShortBasedEnum.Val));
            Assert.Throws<InvalidCastException>(() => { o.TestUnboxAnyAndRebox(new object()); });
            Assert.Throws<NullReferenceException>(() => { o.TestUnboxAnyAndRebox(null); });
        }

        [TestMethod]
        public static unsafe void TestUnboxAnyToShortBasedEnum_NonUSG()
        {
            // Test non-usg case for parallel verification
            BaseType o = new DerivedType<ShortBasedEnum,sbyte>();

            Assert.AreEqual(14, (short)o.TestUnboxAnyAndRebox((object)(short)14));
            Assert.AreEqual(ShortBasedEnum.Val, (ShortBasedEnum)o.TestUnboxAnyAndRebox(ShortBasedEnum.Val));
            Assert.Throws<InvalidCastException>(() => { o.TestUnboxAnyAndRebox(new object()); });
            Assert.Throws<NullReferenceException>(() => { o.TestUnboxAnyAndRebox(null); });
        }

        [TestMethod]
        public static unsafe void TestUnboxAnyToNullableShort_NonUSG()
        {
            // Test non-usg case for parallel verification
            BaseType o = new DerivedType<short?,sbyte>();

            Assert.AreEqual(14, (short)o.TestUnboxAnyAndRebox((object)(short)14));
            Assert.Throws<InvalidCastException>(() => { o.TestUnboxAnyAndRebox(ShortBasedEnum.Val); });
            Assert.Throws<InvalidCastException>(() => { o.TestUnboxAnyAndRebox(new object()); });
            Assert.AreEqual(null, o.TestUnboxAnyAndRebox(null));
        }

        [TestMethod]
        public static unsafe void TestUnboxAnyToNullableShortBasedEnum_NonUSG()
        {
            // Test non-usg case for parallel verification
            BaseType o = new DerivedType<ShortBasedEnum?,sbyte>();

            Assert.Throws<InvalidCastException>(() => { o.TestUnboxAnyAndRebox((object)(short)14); });
            Assert.AreEqual(ShortBasedEnum.Val, (ShortBasedEnum)o.TestUnboxAnyAndRebox(ShortBasedEnum.Val));
            Assert.Throws<InvalidCastException>(() => { o.TestUnboxAnyAndRebox(new object()); });
            Assert.AreEqual(null, o.TestUnboxAnyAndRebox(null));
        }
    }
}

namespace HFATest
{
    public struct Struct00<F, D, I, L> { public D _f1; }
    public struct Struct01<F, D, I, L> { public F _f1; }
    public struct Struct02<F, D, I, L> { public I _f1; }
    public struct Struct03<F, D, I, L> { public L _f1; }

    public struct Struct10<F, D, I, L> { public F _f1; public D _f2; }
    public struct Struct11<F, D, I, L> { public F _f1; public F _f2; }
    public struct Struct12<F, D, I, L> { public F _f1; public I _f2; }
    public struct Struct13<F, D, I, L> { public F _f1; public L _f2; }
    public struct Struct14<F, D, I, L> { public D _f1; public D _f2; }
    public struct Struct15<F, D, I, L> { public D _f1; public F _f2; }
    public struct Struct16<F, D, I, L> { public D _f1; public I _f2; }
    public struct Struct17<F, D, I, L> { public D _f1; public L _f2; }

    public struct Struct20<F, D, I, L> { public F _f1; public D _f2; public F _f3; }
    public struct Struct21<F, D, I, L> { public F _f1; public F _f2; public F _f3; }
    public struct Struct22<F, D, I, L> { public F _f1; public I _f2; public F _f3; }
    public struct Struct23<F, D, I, L> { public F _f1; public L _f2; public F _f3; }
    public struct Struct24<F, D, I, L> { public D _f1; public D _f2; public D _f3; }
    public struct Struct25<F, D, I, L> { public D _f1; public F _f2; public D _f3; }
    public struct Struct26<F, D, I, L> { public D _f1; public I _f2; public D _f3; }
    public struct Struct27<F, D, I, L> { public D _f1; public L _f2; public D _f3; }

    public struct Struct30<F, D, I, L> { public F _f1; public D _f2; public F _f3; public F _f4; }
    public struct Struct31<F, D, I, L> { public F _f1; public F _f2; public F _f3; public F _f4; }
    public struct Struct32<F, D, I, L> { public F _f1; public I _f2; public F _f3; public F _f4; }
    public struct Struct33<F, D, I, L> { public F _f1; public L _f2; public F _f3; public F _f4; }
    public struct Struct34<F, D, I, L> { public D _f1; public D _f2; public D _f3; public D _f4; }
    public struct Struct35<F, D, I, L> { public D _f1; public F _f2; public D _f3; public D _f4; }
    public struct Struct36<F, D, I, L> { public D _f1; public I _f2; public D _f3; public D _f4; }
    public struct Struct37<F, D, I, L> { public D _f1; public L _f2; public D _f3; public D _f4; }

    public struct Struct40<F, D, I, L> { public F _f1; public F _f2; public F _f3; public F _f4; public D _f5; }
    public struct Struct41<F, D, I, L> { public F _f1; public F _f2; public F _f3; public F _f4; public F _f5; }
    public struct Struct42<F, D, I, L> { public F _f1; public F _f2; public F _f3; public F _f4; public I _f5; }
    public struct Struct43<F, D, I, L> { public F _f1; public F _f2; public F _f3; public F _f4; public L _f5; }
    public struct Struct44<F, D, I, L> { public D _f1; public D _f2; public D _f3; public D _f4; public D _f5; }
    public struct Struct45<F, D, I, L> { public D _f1; public D _f2; public D _f3; public D _f4; public F _f5; }
    public struct Struct46<F, D, I, L> { public D _f1; public D _f2; public D _f3; public D _f4; public I _f5; }
    public struct Struct47<F, D, I, L> { public D _f1; public D _f2; public D _f3; public D _f4; public L _f5; }

    public struct FComplex00<F, D, I, L> { public F _f1; public Struct00<F, D, I, L> _f2; }
    public struct FComplex01<F, D, I, L> { public F _f1; public Struct01<F, D, I, L> _f2; }
    public struct FComplex02<F, D, I, L> { public F _f1; public Struct02<F, D, I, L> _f2; }
    public struct FComplex03<F, D, I, L> { public F _f1; public Struct03<F, D, I, L> _f2; }

    public struct FComplex10<F, D, I, L> { public F _f1; public Struct20<F, D, I, L> _f2; }
    public struct FComplex11<F, D, I, L> { public F _f1; public Struct21<F, D, I, L> _f2; }
    public struct FComplex12<F, D, I, L> { public F _f1; public Struct22<F, D, I, L> _f2; }
    public struct FComplex13<F, D, I, L> { public F _f1; public Struct23<F, D, I, L> _f2; }
    public struct FComplex14<F, D, I, L> { public F _f1; public Struct24<F, D, I, L> _f2; }
    public struct FComplex15<F, D, I, L> { public F _f1; public Struct25<F, D, I, L> _f2; }
    public struct FComplex16<F, D, I, L> { public F _f1; public Struct26<F, D, I, L> _f2; }
    public struct FComplex17<F, D, I, L> { public F _f1; public Struct27<F, D, I, L> _f2; }

    public struct DComplex00<F, D, I, L> { public D _f1; public Struct00<F, D, I, L> _f2; }
    public struct DComplex01<F, D, I, L> { public D _f1; public Struct01<F, D, I, L> _f2; }
    public struct DComplex02<F, D, I, L> { public D _f1; public Struct02<F, D, I, L> _f2; }
    public struct DComplex03<F, D, I, L> { public D _f1; public Struct03<F, D, I, L> _f2; }

    public struct DComplex10<F, D, I, L> { public D _f1; public Struct20<F, D, I, L> _f2; }
    public struct DComplex11<F, D, I, L> { public D _f1; public Struct21<F, D, I, L> _f2; }
    public struct DComplex12<F, D, I, L> { public D _f1; public Struct22<F, D, I, L> _f2; }
    public struct DComplex13<F, D, I, L> { public D _f1; public Struct23<F, D, I, L> _f2; }
    public struct DComplex14<F, D, I, L> { public D _f1; public Struct24<F, D, I, L> _f2; }
    public struct DComplex15<F, D, I, L> { public D _f1; public Struct25<F, D, I, L> _f2; }
    public struct DComplex16<F, D, I, L> { public D _f1; public Struct26<F, D, I, L> _f2; }
    public struct DComplex17<F, D, I, L> { public D _f1; public Struct27<F, D, I, L> _f2; }

    public struct Floats3<F, D, I, L> { public F _f1; public F _f2; public F _f3; }
    public struct Floats4<F, D, I, L> { public F _f1; public F _f2; public F _f3; public F _f4; }
    public struct Floats3Complex<F, D, I, L> { public Floats3<F, D, I, L> _f1; }
    public struct Floats4Complex1<F, D, I, L> { public Floats3<F, D, I, L> _f1; public F _f2; }
    public struct Floats4Complex2<F, D, I, L> { public Floats4<F, D, I, L> _f1; }
    public struct Floats4Complex3<F, D, I, L> { public Floats4<F, D, I, L> _f1; public F _f2; }

    public struct Doubles3<F, D, I, L> { public D _f1; public D _f2; public D _f3; }
    public struct Doubles4<F, D, I, L> { public D _f1; public D _f2; public D _f3; public D _f4; }
    public struct Doubles3Complex<F, D, I, L> { public Doubles3<F, D, I, L> _f1; }
    public struct Doubles4Complex1<F, D, I, L> { public Doubles3<F, D, I, L> _f1; public D _f2; }
    public struct Doubles4Complex2<F, D, I, L> { public Doubles4<F, D, I, L> _f1; }
    public struct Doubles4Complex3<F, D, I, L> { public Doubles4<F, D, I, L> _f1; public D _f2; }

    public struct GenStructWrapper<T> { public T _f1; }

    public class TestClass<T>
    {
        public static void TestStruct(T t, object[] values)
        {
            TestStruct_Inner1(TestStruct_Inner1(t, values), values);
            TestStruct_Inner2("abc", TestStruct_Inner2("abc", t, values), values);
            TestStruct_Inner3("abc", "def", TestStruct_Inner3("abc", "def", t, values), values);
        }
        public static T TestStruct_Inner1(T t, object[] values)
        {
            CheckFieldValues(t, typeof(T).GetTypeInfo(), values, 0);
            T copy = t;
            return copy;
        }
        public static T TestStruct_Inner2(string param1, T t, object[] values)
        {
            CheckFieldValues(t, typeof(T).GetTypeInfo(), values, 0);
            T copy = t;
            return copy;
        }
        public static T TestStruct_Inner3(string param1, string param2, T t, object[] values)
        {
            CheckFieldValues(t, typeof(T).GetTypeInfo(), values, 0);
            T copy = t;
            return copy;
        }

        static void CheckFieldValues(object obj, TypeInfo ti, object[] values, int index)
        {
            for (int i = 1; i <= 5; i++)
            {
                FieldInfo fi = ti.GetDeclaredField("_f" + i);
                if (fi == null) break;

                if (fi.FieldType.Name.Contains("Struct") || fi.FieldType.Name.Contains("Floats") || fi.FieldType.Name.Contains("Doubles") || fi.DeclaringType.Name.Contains("GenStructWrapper"))
                {
                    TypeInfo complexti = fi.FieldType.GetTypeInfo();
                    object complexObj = fi.GetValue(obj);
                    CheckFieldValues(complexObj, complexti, values, index);
                }
                else
                {
                    //Console.WriteLine(obj.GetType() + "._f" + i + " == " + values[index] + " ? " + (fi.GetValue(obj).Equals(values[index])));
                    Assert.AreEqual(fi.GetValue(obj), values[index]);
                    index++;
                }
            }
        }
    }

    public class Runner
    {
        [TestMethod]
        public static unsafe void HFATestEntryPoint()
        {
            // suppress stupid warning about field not being used in code...
            new GenStructWrapper<string> { _f1 = "abc" };

            HFATestEntryPoint_Inner(typeof(Struct00<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct01<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct02<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct03<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct10<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct11<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct12<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct13<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct14<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct15<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct16<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct17<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct20<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct21<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct22<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct23<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct24<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct25<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct26<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct27<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct30<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct31<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct32<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct33<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct34<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct35<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct36<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct37<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct40<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct41<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct42<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct43<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct44<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct45<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct46<,,,>));
            HFATestEntryPoint_Inner(typeof(Struct47<,,,>));
            HFATestEntryPoint_Inner(typeof(FComplex00<,,,>));
            HFATestEntryPoint_Inner(typeof(FComplex01<,,,>));
            HFATestEntryPoint_Inner(typeof(FComplex02<,,,>));
            HFATestEntryPoint_Inner(typeof(FComplex03<,,,>));
            HFATestEntryPoint_Inner(typeof(FComplex10<,,,>));
            HFATestEntryPoint_Inner(typeof(FComplex11<,,,>));
            HFATestEntryPoint_Inner(typeof(FComplex12<,,,>));
            HFATestEntryPoint_Inner(typeof(FComplex13<,,,>));
            HFATestEntryPoint_Inner(typeof(FComplex14<,,,>));
            HFATestEntryPoint_Inner(typeof(FComplex15<,,,>));
            HFATestEntryPoint_Inner(typeof(FComplex16<,,,>));
            HFATestEntryPoint_Inner(typeof(FComplex17<,,,>));
            HFATestEntryPoint_Inner(typeof(DComplex00<,,,>));
            HFATestEntryPoint_Inner(typeof(DComplex01<,,,>));
            HFATestEntryPoint_Inner(typeof(DComplex02<,,,>));
            HFATestEntryPoint_Inner(typeof(DComplex03<,,,>));
            HFATestEntryPoint_Inner(typeof(DComplex10<,,,>));
            HFATestEntryPoint_Inner(typeof(DComplex11<,,,>));
            HFATestEntryPoint_Inner(typeof(DComplex12<,,,>));
            HFATestEntryPoint_Inner(typeof(DComplex13<,,,>));
            HFATestEntryPoint_Inner(typeof(DComplex14<,,,>));
            HFATestEntryPoint_Inner(typeof(DComplex15<,,,>));
            HFATestEntryPoint_Inner(typeof(DComplex16<,,,>));
            HFATestEntryPoint_Inner(typeof(DComplex17<,,,>));

            HFATestEntryPoint_Inner(typeof(Floats3<,,,>));
            HFATestEntryPoint_Inner(typeof(Floats4<,,,>));
            HFATestEntryPoint_Inner(typeof(Floats3Complex<,,,>));
            HFATestEntryPoint_Inner(typeof(Floats4Complex1<,,,>));
            HFATestEntryPoint_Inner(typeof(Floats4Complex2<,,,>));
            HFATestEntryPoint_Inner(typeof(Floats4Complex3<,,,>));

            HFATestEntryPoint_Inner(typeof(Doubles3<,,,>));
            HFATestEntryPoint_Inner(typeof(Doubles4<,,,>));
            HFATestEntryPoint_Inner(typeof(Doubles3Complex<,,,>));
            HFATestEntryPoint_Inner(typeof(Doubles4Complex1<,,,>));
            HFATestEntryPoint_Inner(typeof(Doubles4Complex2<,,,>));
            HFATestEntryPoint_Inner(typeof(Doubles4Complex3<,,,>));
        }

        static void HFATestEntryPoint_Inner(Type structType)
        {
            Type genStructInst = structType.MakeGenericType(TypeOf.Float, TypeOf.Double, TypeOf.Int32, TypeOf.Long);

            {
                TypeOf.HFA_TestClass.MakeGenericType(genStructInst).GetTypeInfo().GetDeclaredMethod("TestStruct").Invoke(null, GetInstanceAndValuesArray(genStructInst));
            }

            {
                Type genStructWrapper = TypeOf.HFA_GenStructWrapper.MakeGenericType(genStructInst);
                TypeOf.HFA_TestClass.MakeGenericType(genStructWrapper).GetTypeInfo().GetDeclaredMethod("TestStruct").Invoke(null, GetInstanceAndValuesArray(genStructWrapper));
            }
        }

        public static unsafe object[] GetInstanceAndValuesArray(Type genStructInst)
        {
            object obj = Activator.CreateInstance(genStructInst);
            List<object>  values = new List<object>();
            FillFieldValues(obj, 1, values);
            return new object[] { obj, values.ToArray() };
        }

        static void FillFieldValues(object obj, int multiplier, List<object> values)
        {
            for (int i = 1; i <= 5; i++)
            {
                float fvalue = 1.1f * i * multiplier;
                double dvalue = 11.11 * i * multiplier;
                int ivalue = 10 * i * multiplier;
                long lvalue = 123000 * i * multiplier;

                FieldInfo fi = obj.GetType().GetTypeInfo().GetDeclaredField("_f" + i);
                if (fi == null) return;

                if (fi.FieldType == typeof(float))
                {
                    fi.SetValue(obj, fvalue);
                    values.Add(fvalue);
                }
                else if (fi.FieldType == typeof(double))
                {
                    fi.SetValue(obj, dvalue);
                    values.Add(dvalue);
                }
                else if (fi.FieldType == typeof(int))
                {
                    fi.SetValue(obj, ivalue);
                    values.Add(ivalue);
                }
                else if (fi.FieldType == typeof(long))
                {
                    fi.SetValue(obj, lvalue);
                    values.Add(lvalue);
                }
                else
                {
                    object complexObj = fi.GetValue(obj);
                    FillFieldValues(complexObj, multiplier * 2, values);
                    fi.SetValue(obj, complexObj);
                }
            }
        }
    }
}
namespace ComparerOfTTests
{
    struct BoringStruct
    {
    }

    struct StructThatImplementsIComparable : IComparable<StructThatImplementsIComparable>
    {
        public StructThatImplementsIComparable(int x)
        {
            _x = x;
        }

        int IComparable<StructThatImplementsIComparable>.CompareTo(StructThatImplementsIComparable other)
        {
            if (_x == other._x) return 0;
            if (_x < other._x) return -1;
            return 1;
        }

        private int _x;
    }

    struct StructThatImplementsIComparableOfObject : IComparable<object>
    {
        public StructThatImplementsIComparableOfObject(int x)
        {
            _x = x;
        }

        int IComparable<object>.CompareTo(object other)
        {
            return 1;
        }

        private int _x;
    }

    public abstract class BaseType
    {
        public abstract void TestCompare(object x, object y);
    }

    public class DerivedType<T,V> : BaseType
    {
        private static void TestC(T x, T y)
        {
            Comparer<T> e = Comparer<T>.Default;
            bool expectThrow = false;

            int expectedResult;
            if (x is IComparable<T>)
            {
                expectedResult = ((IComparable<T>)x).CompareTo(y);
            }
            else if (x is StructThatImplementsIComparable?)
            {
                // This logic really applies to all Nullable types but it's a pain to write this for general nullable types without falling back to Reflection

                StructThatImplementsIComparable? xn = (StructThatImplementsIComparable?)(object)x;
                StructThatImplementsIComparable? yn = (StructThatImplementsIComparable?)(object)y;

                if (xn.HasValue && yn.HasValue)
                {
                    IComparable<StructThatImplementsIComparable> xv = ((StructThatImplementsIComparable?)(object)x).Value;
                    StructThatImplementsIComparable yv = ((StructThatImplementsIComparable?)(object)y).Value;
                    expectedResult = xv.CompareTo(yv);
                }
                else if (xn.HasValue)
                {
                    expectedResult = 1;
                }
                else if (yn.HasValue)
                {
                    expectedResult = -1;
                }
                else
                {
                    expectedResult = 0;
                }
            }
            else if (x is int?)
            {
                // This logic really applies to all Nullable types but it's a pain to write this for general nullable types without falling back to Reflection

                int? xn = (int?)(object)x;
                int? yn = (int?)(object)y;

                if (xn.HasValue && yn.HasValue)
                {
                    IComparable<int> xv = ((int?)(object)x).Value;
                    int yv = ((int?)(object)y).Value;
                    expectedResult = xv.CompareTo(yv);
                }
                else if (xn.HasValue)
                {
                    expectedResult = 1;
                }
                else if (yn.HasValue)
                {
                    expectedResult = -1;
                }
                else
                {
                    expectedResult = 0;
                }
            }
            else
            {
                expectedResult = 0;
                try
                {
                    expectedResult = System.Collections.Comparer.Default.Compare(x,y);
                }
                catch
                {
                    expectThrow = true;
                }
            }

            int actualResult = 0;
            bool actualThrow = false;

            try
            {
                actualResult = e.Compare(x,y);
            }
            catch
            {
                actualThrow = true;
            }

            Assert.AreEqual(expectedResult, actualResult);
            Assert.AreEqual(expectThrow, actualThrow);
        }

        public override void TestCompare(object x, object y)
        {
            TestC((T)x, (T)y);
        }
    }

    public class Runner
    {
        [TestMethod]
        public static unsafe void TestStructThatImplementsIComparable()
        {
            Type t = typeof(DerivedType<,>).MakeGenericType(typeof(StructThatImplementsIComparable), TypeOf.Short);
            BaseType o = (BaseType)Activator.CreateInstance(t);

            object o1 = new StructThatImplementsIComparable(1);
            object o2 = new StructThatImplementsIComparable(2);
            o.TestCompare(o2, o1);
            o.TestCompare(o1, o2);
            o.TestCompare(o1, o1);
        }

        [TestMethod]
        public static unsafe void TestStructThatImplementsIComparableOfObject()
        {
            Type t = typeof(DerivedType<,>).MakeGenericType(typeof(StructThatImplementsIComparableOfObject), TypeOf.Short);
            BaseType o = (BaseType)Activator.CreateInstance(t);

            object o1 = new StructThatImplementsIComparableOfObject(1);
            object o2 = new StructThatImplementsIComparableOfObject(2);
            o.TestCompare(o2, o1);
            o.TestCompare(o1, o2);
            o.TestCompare(o1, o1);
        }

        [TestMethod]
        public static unsafe void TestBoringStruct()
        {
            Type t = typeof(DerivedType<,>).MakeGenericType(typeof(BoringStruct), TypeOf.Short);
            BaseType o = (BaseType)Activator.CreateInstance(t);

            object o1 = new BoringStruct();
            object o2 = new BoringStruct();

            o.TestCompare(o2, o1);
            o.TestCompare(o1, o2);
            o.TestCompare(o1, o1);
        }
    }
}

namespace DefaultValueDelegateParameterTests
{
    public delegate int DelegateWithDefaultValue<T>(T val, int defaultParam = 2);

    public abstract class BaseType
    {
        public abstract Delegate GetDefaultValueDelegate();
        public abstract void SetExpectedValParameter(object expected);
    }

    public class TestType<T> : BaseType
    {
        T s_expected;

        public override void SetExpectedValParameter(object expected)
        {
            s_expected = (T)expected;
        }

        public override Delegate GetDefaultValueDelegate()
        {
            return (DelegateWithDefaultValue<T>)DelegateTarget;
        }

        private int DelegateTarget(T val, int defaultValueParam)
        {
            Assert.AreEqual(s_expected, val);
            return defaultValueParam;
        }
    }

    public class Runner
    {
        [TestMethod]
        public static unsafe void TestCallUniversalGenericDelegate()
        {
            Type t = typeof(TestType<>).MakeGenericType(TypeOf.Short);
            BaseType targetObject = (BaseType)Activator.CreateInstance(t);

            targetObject.SetExpectedValParameter((short)3);
            Delegate del = targetObject.GetDefaultValueDelegate();
            object result;

            // Test using default parameter
            result = del.DynamicInvoke(new object[]{ (object)(short)3, Type.Missing});
            Assert.AreEqual(result, 2);

            // Test not using default parameter
            result = del.DynamicInvoke(new object[]{ (object)(short)3, 5});
            Assert.AreEqual(result, 5);
        }
    }
}

namespace ArrayOfGenericStructGCTests
{
    struct StructWithGCReference
    {
        public object o;
        public object o2;
    }

    public struct StructWithoutGCReference
    {
        public IntPtr _value;
        public IntPtr _value2;
        public IntPtr _value3;
    }

    public struct GenericStruct<X,Y,Z>
    {
        public X _x;
        public Y _y;
    }

    public abstract class Base
    {
        public abstract void SetValues(int index, object x, object y);
    }

    public class Derived<X, Y, Z> : Base
    {
        GenericStruct<X, Y, Z>[] _array = new GenericStruct<X, Y, Z>[100];
        public override void SetValues(int index, object x, object y)
        {
            _array[index]._x = (X)x;
            _array[index]._y = (Y)y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ClassWithNonPointerSizedFinalFieldBase<T>
    {
        public object o1;
        public object o2;
        public byte b1;

        // Ensure the toolchain doesn't DR any part of this type
        public override string ToString()
        {
            if (o1 != null) return o1.ToString();
            if (o2 != null) return o1.ToString();
            return b1.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ClassWithNonPointerSizedFinalFieldBase2<T> : ClassWithNonPointerSizedFinalFieldBase<T>
    {
        public object o3;
        public object o4;
        public byte b2;

        // Ensure the toolchain doesn't DR any part of this type
        public override string ToString()
        {
            if (o1 != null) return o1.ToString();
            if (o2 != null) return o1.ToString();
            if (o3 != null) return o1.ToString();
            if (o4 != null) return o1.ToString();
            return b1.ToString() + b2.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ClassWithNonPointerSizedFinalField<T> : ClassWithNonPointerSizedFinalFieldBase2<T>
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructWithNonPointerSizedFinalField<T>
    {
        public object o1;
        public object o2;
        public byte b1;
        public object o3;
        public object o4;
        public byte b2;

        // Ensure the toolchain doesn't DR any part of this type
        public override string ToString()
        {
            if (o1 != null) return o1.ToString();
            if (o2 != null) return o1.ToString();
            if (o3 != null) return o1.ToString();
            if (o4 != null) return o1.ToString();
            return b1.ToString() + b2.ToString();
        }
    }

    public class Runner
    {
        static object s_o = new Derived<int,int,int>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static unsafe IntPtr * decrement(IntPtr * pIntPtr)
        {
            // This function is a workaround for 469350
            // If this is inline in TestArrayOfGenericStructGCTests, nutc
            // gc tracker can become confused
            return pIntPtr - 1;
        }

        // This test constructs a valuetype array full of GC pointers and
        // non-pointers, and attempts to validate that it is reported correctly, by
        // having the non-pointers be close enough to gc pointers that the GC will AV
        // if it does the wrong thing
        [TestMethod]
        public static unsafe void TestArrayOfGenericStructGCTests()
        {
            Type t = typeof(Derived<,,>).MakeGenericType(typeof(StructWithGCReference), typeof(StructWithoutGCReference), TypeOf.Short);
            Base o = (Base)Activator.CreateInstance(t);

            StructWithGCReference swgr = new StructWithGCReference();
            swgr.o = new object();
            swgr.o2 = new object();
            StructWithoutGCReference swogr = new StructWithoutGCReference();

            GenericStruct<object, IntPtr, bool> tempStruct = new GenericStruct<object, IntPtr, bool>();
            tempStruct._x = new object();
            IntPtr *pIntPtr = &tempStruct._y;
            pIntPtr = decrement(pIntPtr);

            long ptrInGCHeapThatIsLikelyGCObjectAddress = (*pIntPtr).ToInt64();
            long ptrInGCHeapThatIsNotGCObjectAddress = ptrInGCHeapThatIsLikelyGCObjectAddress + 1;

            swogr._value = new IntPtr(ptrInGCHeapThatIsNotGCObjectAddress);
            swogr._value2 = new IntPtr(ptrInGCHeapThatIsNotGCObjectAddress+1);
            swogr._value3 = new IntPtr(ptrInGCHeapThatIsNotGCObjectAddress+2);

            for (int i = 0; i < 100; i++)
                o.SetValues(i, swgr, swogr);

            GC.Collect(2);
            GC.Collect(2);
            GC.Collect(2);
        }

        static string s_str;

        // This test creates a type that have both GC pointers and lack a final field which is aligned on a GC boundary
        [TestMethod]
        public static unsafe void TestNonPointerSizedFinalField()
        {
            Type t;
            object o;

            t = typeof(ClassWithNonPointerSizedFinalFieldBase2<>).MakeGenericType(TypeOf.Int32);
            o = Activator.CreateInstance(t);
            s_str = o.ToString();

            t = typeof(ClassWithNonPointerSizedFinalField<>).MakeGenericType(TypeOf.Int32);
            o = Activator.CreateInstance(t);
            s_str = o.ToString();

            t = typeof(ClassWithNonPointerSizedFinalField<>).MakeGenericType(TypeOf.Short);
            o = Activator.CreateInstance(t);
            s_str = o.ToString();

            Type t2 = typeof(StructWithNonPointerSizedFinalField<>).MakeGenericType(TypeOf.Short);
            object o2 = Activator.CreateInstance(t2);
            s_str = o.ToString();
        }
    }
}

namespace DelegatesToStructMethods
{
    public struct MySpecialStruct<T>
    {
        T _val;
        public MySpecialStruct(T val) { _val = val; }

        public Func<short, string> SimpleDelegateCreator()
        {
            return new Func<short, string>(StructFunc);
        }
        public Func<short, string> SimpleGenDelegateCreator()
        {
            return new Func<short, string>(GenStructFunc<T>);
        }
        public Func<T, string> ComplexDelegateCreator()
        {
            return new Func<T, string>(StructFuncNeedingCCC);
        }
        public Func<T, string> ComplexGenDelegateCreator()
        {
            return new Func<T, string>(GenStructFuncNeedingCCC<T>);
        }


        public string StructFunc(short s)
        {
            return typeof(T).Name + "-" + _val.ToString() + "-" + s;
        }
        public string GenStructFunc<U>(short s)
        {
            return typeof(T).Name + "-" + typeof(U).Name + "-" + _val.ToString() + "-" + s;
        }
        public string StructFuncNeedingCCC(T s)
        {
            return typeof(T).Name + "-" + _val.ToString() + "-" + s;
        }
        public string GenStructFuncNeedingCCC<U>(T s)
        {
            return typeof(T).Name + "-" + typeof(U).Name + "-" + _val.ToString() + "-" + s;
        }
    }

    public class MySpecialClass<T>
    {
        T _val;
        public MySpecialClass(T val) { _val = val; }

        public Func<short, string> SimpleDelegateCreator()
        {
            return new Func<short, string>(ClassFunc);
        }
        public Func<short, string> SimpleGenDelegateCreator()
        {
            return new Func<short, string>(GenClassFunc<T>);
        }
        public Func<T, string> ComplexDelegateCreator()
        {
            return new Func<T, string>(ClassFuncNeedingCCC);
        }
        public Func<T, string> ComplexGenDelegateCreator()
        {
            return new Func<T, string>(GenClassFuncNeedingCCC<T>);
        }


        public string ClassFunc(short s)
        {
            return typeof(T).Name + "-" + _val.ToString() + "-" + s;
        }
        public string GenClassFunc<U>(short s)
        {
            return typeof(T).Name + "-" + typeof(U).Name + "-" + _val.ToString() + "-" + s;
        }
        public string ClassFuncNeedingCCC(T s)
        {
            return typeof(T).Name + "-" + _val.ToString() + "-" + s;
        }
        public string GenClassFuncNeedingCCC<U>(T s)
        {
            return typeof(T).Name + "-" + typeof(U).Name + "-" + _val.ToString() + "-" + s;
        }
    }

    public class Runner
    {
        [TestMethod]
        public static void TestDelegateInvokeToMethods()
        {
            MethodInfo testMi = typeof(Runner).GetTypeInfo().GetDeclaredMethod("TestDelegateInvokeToMethods_Inner").MakeGenericMethod(TypeOf.Short);

            testMi.Invoke(null, new object[] { (short)44, (short)55, "StructFunc", "GenStructFunc", true });
            testMi.Invoke(null, new object[] { (short)44, (short)55, "ClassFunc", "GenClassFunc", false });
        }
        static string CallDelegateFromNonUSGContext(Delegate d, short val)
        {
            Func<short, string> del = (Func<short, string>)d;
            return del(val);
        }
        public static void TestDelegateInvokeToMethods_Inner<T>(T tval1, T tval2, string funcName, string genFuncName, bool isTestOnStruct)
        {
            // USG case
            {
                Type t = isTestOnStruct ?
                    typeof(MySpecialStruct<>).MakeGenericType(TypeOf.Short) :
                    typeof(MySpecialClass<>).MakeGenericType(TypeOf.Short);

                object o = Activator.CreateInstance(t, new object[] { (short)123 });

                // Simple method signature case
                {
                    MethodInfo delCreator = t.GetTypeInfo().GetDeclaredMethod("SimpleDelegateCreator");
                    MethodInfo genDelCreator = t.GetTypeInfo().GetDeclaredMethod("SimpleGenDelegateCreator");

                    MethodInfo mi = t.GetTypeInfo().GetDeclaredMethod(funcName);
                    Func<short, string> del1 = (Func<short, string>)mi.CreateDelegate(typeof(Func<short, string>), o);
                    Func<short, string> del2 = (Func<short, string>)delCreator.Invoke(o, null);

                    string res1 = del1(11);
                    string res2 = del2(11);
                    Assert.AreEqual(res1, "Int16-123-11");
                    Assert.AreEqual(res1, res2);

                    MethodInfo miGen = t.GetTypeInfo().GetDeclaredMethod(genFuncName).MakeGenericMethod(TypeOf.Short);
                    Func<short, string> genDel1 = (Func<short, string>)miGen.CreateDelegate(typeof(Func<short, string>), o);
                    Func<short, string> genDel2 = (Func<short, string>)genDelCreator.Invoke(o, null);

                    string genRes1 = genDel1(22);
                    string genRes2 = genDel2(22);
                    Assert.AreEqual(genRes1, "Int16-Int16-123-22");
                    Assert.AreEqual(genRes1, genRes2);
                }

                // Complex method signature case
                {
                    MethodInfo delCreator = t.GetTypeInfo().GetDeclaredMethod("ComplexDelegateCreator");
                    MethodInfo genDelCreator = t.GetTypeInfo().GetDeclaredMethod("ComplexGenDelegateCreator");

                    MethodInfo mi = t.GetTypeInfo().GetDeclaredMethod(funcName + "NeedingCCC");
                    Func<T, string> del1 = (Func<T, string>)mi.CreateDelegate(typeof(Func<T, string>), o);
                    Func<T, string> del2 = (Func<T, string>)delCreator.Invoke(o, null);

                    string res1 = del1(tval1);
                    string res2 = del2(tval1);
                    Assert.AreEqual(res1, "Int16-123-44");
                    Assert.AreEqual(res1, res2);

                    res1 = CallDelegateFromNonUSGContext(del1, 44);
                    res2 = CallDelegateFromNonUSGContext(del2, 44);
                    Assert.AreEqual(res1, "Int16-123-44");
                    Assert.AreEqual(res1, res2);

                    MethodInfo miGen = t.GetTypeInfo().GetDeclaredMethod(genFuncName + "NeedingCCC").MakeGenericMethod(TypeOf.Short);
                    Func<T, string> genDel1 = (Func<T, string>)miGen.CreateDelegate(typeof(Func<T, string>), o);
                    Func<T, string> genDel2 = (Func<T, string>)genDelCreator.Invoke(o, null);

                    string genRes1 = genDel1(tval2);
                    string genRes2 = genDel2(tval2);
                    Assert.AreEqual(genRes1, "Int16-Int16-123-55");
                    Assert.AreEqual(genRes1, genRes2);

                    genRes1 = CallDelegateFromNonUSGContext(genDel1, 55);
                    genRes2 = CallDelegateFromNonUSGContext(genDel2, 55);
                    Assert.AreEqual(genRes1, "Int16-Int16-123-55");
                    Assert.AreEqual(genRes1, genRes2);
                }
            }

            // Normal Canonical case
            {
                Type t = isTestOnStruct ?
                    typeof(MySpecialStruct<>).MakeGenericType(TypeOf.String) :
                    typeof(MySpecialClass<>).MakeGenericType(TypeOf.String);
                object o = Activator.CreateInstance(t, new object[] { "abc" });

                MethodInfo delCreator = t.GetTypeInfo().GetDeclaredMethod("SimpleDelegateCreator");
                MethodInfo genDelCreator = t.GetTypeInfo().GetDeclaredMethod("SimpleGenDelegateCreator");

                MethodInfo mi = t.GetTypeInfo().GetDeclaredMethod(funcName);
                Func<short, string> del1 = (Func<short, string>)mi.CreateDelegate(typeof(Func<short, string>), o);
                Func<short, string> del2 = (Func<short, string>)delCreator.Invoke(o, null);

                string res1 = del1(66);
                string res2 = del2(66);
                Assert.AreEqual(res1, "String-abc-66");
                Assert.AreEqual(res1, res2);

                MethodInfo miGen = t.GetTypeInfo().GetDeclaredMethod(genFuncName).MakeGenericMethod(TypeOf.String);
                Func<short, string> genDel1 = (Func<short, string>)miGen.CreateDelegate(typeof(Func<short, string>), o);
                Func<short, string> genDel2 = (Func<short, string>)genDelCreator.Invoke(o, null);

                string genRes1 = genDel1(77);
                string genRes2 = genDel2(77);
                Assert.AreEqual(genRes1, "String-String-abc-77");
                Assert.AreEqual(genRes1, genRes2);
            }
        }
    }
}
