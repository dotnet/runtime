// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/51952", TestPlatforms.tvOS)]
    public static class CastTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomCastCustom2Test(bool useInterpreter)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomCastCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomCastInterfaceTest(bool useInterpreter)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomCastInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomCastIEquatableOfCustomTest(bool useInterpreter)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomCastIEquatableOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomCastIEquatableOfCustom2Test(bool useInterpreter)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomCastIEquatableOfCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomCastObjectTest(bool useInterpreter)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomCastObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayCastCustom2ArrayTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastCustom2Array(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayCastIEnumerableOfCustomTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIEnumerableOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayCastIEnumerableOfCustom2Test(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIEnumerableOfCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayCastIEnumerableOfInterfaceTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIEnumerableOfInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayCastIEnumerableOfObjectTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIEnumerableOfObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayCastIListOfCustomTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIListOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayCastIListOfCustom2Test(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIListOfCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayCastIListOfInterfaceTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIListOfInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayCastIListOfObjectTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIListOfObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayCastObjectArrayTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2CastCustomTest(bool useInterpreter)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2CastCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2CastInterfaceTest(bool useInterpreter)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2CastInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2CastIEquatableOfCustomTest(bool useInterpreter)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2CastIEquatableOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2CastIEquatableOfCustom2Test(bool useInterpreter)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2CastIEquatableOfCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2CastObjectTest(bool useInterpreter)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2CastObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayCastCustomArrayTest(bool useInterpreter)
        {
            D[][] array = new D[][] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2ArrayCastCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateCastFuncOfObjectTest(bool useInterpreter)
        {
            Delegate[] array = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyDelegateCastFuncOfObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateCastObjectTest(bool useInterpreter)
        {
            Delegate[] array = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyDelegateCastObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumCastEnumTypeTest(bool useInterpreter)
        {
            E[] array = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumCastEnumType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumCastObjectTest(bool useInterpreter)
        {
            E[] array = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumCastObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumTypeCastEnumTest(bool useInterpreter)
        {
            Enum[] array = new Enum[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumTypeCastEnum(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnsignedEnumInObjectCastEnum(bool useInterpreter)
        {
            foreach (Eu value in new[] { Eu.Foo, Eu.Bar, Eu.Baz, (Eu)uint.MaxValue })
                VerifyUnsignedEnumInObjectCastEnum(value, useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnsignedEnumCastEnum(bool useInterpreter)
        {
            foreach (Eu value in new[] { Eu.Foo, Eu.Bar, Eu.Baz, (Eu)uint.MaxValue })
                VerifyUnsignedEnumCastEnum(value, useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumCastLongEnum(bool useInterpreter)
        {
            foreach (E value in new[] { E.A, E.B, (E)int.MinValue, (E)int.MaxValue })
                VerifyEnumCastLongEnum(value, useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongEnumCastEnum(bool useInterpreter)
        {
            foreach (El value in new[] { El.A, El.B, (El)int.MaxValue, (El)long.MaxValue, (El)long.MinValue })
                VerifyLongEnumCastEnum(value, useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumTypeCastObjectTest(bool useInterpreter)
        {
            Enum[] array = new Enum[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumTypeCastObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncOfObjectCastDelegateTest(bool useInterpreter)
        {
            Func<object>[] array = new Func<object>[] { null, (Func<object>)delegate () { return null; } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyFuncOfObjectCastDelegate(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceCastCustomTest(bool useInterpreter)
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceCastCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceCastCustom2Test(bool useInterpreter)
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceCastCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceCastObjectTest(bool useInterpreter)
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceCastObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfCustomCastCustomArrayTest(bool useInterpreter)
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomCastCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfCustomCastObjectArrayTest(bool useInterpreter)
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomCastObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfCustom2CastCustomArrayTest(bool useInterpreter)
        {
            IEnumerable<D>[] array = new IEnumerable<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustom2CastCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfInterfaceCastCustomArrayTest(bool useInterpreter)
        {
            IEnumerable<I>[] array = new IEnumerable<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfInterfaceCastCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfInterfaceCastObjectArrayTest(bool useInterpreter)
        {
            IEnumerable<I>[] array = new IEnumerable<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfInterfaceCastObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfObjectCastCustomArrayTest(bool useInterpreter)
        {
            IEnumerable<object>[] array = new IEnumerable<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfObjectCastCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfObjectCastObjectArrayTest(bool useInterpreter)
        {
            IEnumerable<object>[] array = new IEnumerable<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfObjectCastObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfStructCastStructArrayTest(bool useInterpreter)
        {
            IEnumerable<S>[] array = new IEnumerable<S>[] { null, new S[] { default(S), new S() }, new S[10], new List<S>(), new List<S>(new S[] { default(S), new S() }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfStructCastStructArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustomCastCustomTest(bool useInterpreter)
        {
            IEquatable<C>[] array = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustomCastCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustomCastCustom2Test(bool useInterpreter)
        {
            IEquatable<C>[] array = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustomCastCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustomCastObjectTest(bool useInterpreter)
        {
            IEquatable<C>[] array = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustomCastObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustom2CastCustomTest(bool useInterpreter)
        {
            IEquatable<D>[] array = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustom2CastCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustom2CastCustom2Test(bool useInterpreter)
        {
            IEquatable<D>[] array = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustom2CastCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustom2CastObjectTest(bool useInterpreter)
        {
            IEquatable<D>[] array = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustom2CastObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfStructCastStructTest(bool useInterpreter)
        {
            IEquatable<S>[] array = new IEquatable<S>[] { null };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfStructCastStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfCustomCastCustomArrayTest(bool useInterpreter)
        {
            IList<C>[] array = new IList<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustomCastCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfCustomCastObjectArrayTest(bool useInterpreter)
        {
            IList<C>[] array = new IList<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustomCastObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfCustom2CastCustomArrayTest(bool useInterpreter)
        {
            IList<D>[] array = new IList<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustom2CastCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfInterfaceCastCustomArrayTest(bool useInterpreter)
        {
            IList<I>[] array = new IList<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfInterfaceCastCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfInterfaceCastObjectArrayTest(bool useInterpreter)
        {
            IList<I>[] array = new IList<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfInterfaceCastObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfObjectCastCustomArrayTest(bool useInterpreter)
        {
            IList<object>[] array = new IList<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfObjectCastCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfObjectCastObjectArrayTest(bool useInterpreter)
        {
            IList<object>[] array = new IList<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfObjectCastObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfStructCastStructArrayTest(bool useInterpreter)
        {
            IList<S>[] array = new IList<S>[] { null, new S[] { default(S), new S() }, new S[10], new List<S>(), new List<S>(new S[] { default(S), new S() }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfStructCastStructArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntCastObjectTest(bool useInterpreter)
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIntCastObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntCastValueTypeTest(bool useInterpreter)
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIntCastValueType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectCastCustomTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectCastCustom2Test(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectCastDelegateTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastDelegate(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectCastEnumTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastEnum(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectCastEnumTypeTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastEnumType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectCastInterfaceTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectCastIEquatableOfCustomTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastIEquatableOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectCastIEquatableOfCustom2Test(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastIEquatableOfCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectCastIntTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastInt(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectCastStructTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectCastValueTypeTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastValueType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayCastCustomArrayTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayCastCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayCastIEnumerableOfCustomTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayCastIEnumerableOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayCastIEnumerableOfInterfaceTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayCastIEnumerableOfInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayCastIEnumerableOfObjectTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayCastIEnumerableOfObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayCastIListOfCustomTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayCastIListOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayCastIListOfInterfaceTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayCastIListOfInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayCastIListOfObjectTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayCastIListOfObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructCastIEquatableOfStructTest(bool useInterpreter)
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructCastIEquatableOfStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructCastObjectTest(bool useInterpreter)
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructCastObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructCastValueTypeTest(bool useInterpreter)
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructCastValueType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayCastIEnumerableOfStructTest(bool useInterpreter)
        {
            S[][] array = new S[][] { null, new S[] { default(S), new S() }, new S[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructArrayCastIEnumerableOfStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayCastIListOfStructTest(bool useInterpreter)
        {
            S[][] array = new S[][] { null, new S[] { default(S), new S() }, new S[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructArrayCastIListOfStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckValueTypeCastIntTest(bool useInterpreter)
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeCastInt(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckValueTypeCastObjectTest(bool useInterpreter)
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeCastObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckValueTypeCastStructTest(bool useInterpreter)
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeCastStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericAsCustom(bool useInterpreter)
        {
            CheckObjectCastGenericHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericAsEnum(bool useInterpreter)
        {
            CheckObjectCastGenericHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericAsObject(bool useInterpreter)
        {
            CheckObjectCastGenericHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericAsStruct(bool useInterpreter)
        {
            CheckObjectCastGenericHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericAsStructWithStringAndField(bool useInterpreter)
        {
            CheckObjectCastGenericHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericWithClassRestrictionAsCustom(bool useInterpreter)
        {
            CheckObjectCastGenericWithClassRestrictionHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericWithClassRestrictionAsObject(bool useInterpreter)
        {
            CheckObjectCastGenericWithClassRestrictionHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericWithStructRestrictionAsEnum(bool useInterpreter)
        {
            CheckObjectCastGenericWithStructRestrictionHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericWithStructRestrictionAsStruct(bool useInterpreter)
        {
            CheckObjectCastGenericWithStructRestrictionHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericWithStructRestrictionAsStructWithStringAndField(bool useInterpreter)
        {
            CheckObjectCastGenericWithStructRestrictionHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericCastObjectAsCustom(bool useInterpreter)
        {
            CheckGenericCastObjectHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericCastObjectAsEnum(bool useInterpreter)
        {
            CheckGenericCastObjectHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericCastObjectAsObject(bool useInterpreter)
        {
            CheckGenericCastObjectHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericCastObjectAsStruct(bool useInterpreter)
        {
            CheckGenericCastObjectHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericCastObjectAsStructWithStringAndField(bool useInterpreter)
        {
            CheckGenericCastObjectHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithClassRestrictionCastObjectAsCustom(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionCastObjectHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithClassRestrictionCastObjectAsObject(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionCastObjectHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastObjectAsEnum(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCastObjectHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStruct(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCastObjectHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStructWithStringAndField(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCastObjectHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsEnum(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCastValueTypeHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStruct(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCastValueTypeHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStructWithStringAndField(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCastValueTypeHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsDateTime(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCastValueTypeHelper<DateTime>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertValueTypeCastGenericWithStructRestrictionAsEnum(bool useInterpreter)
        {
            CheckValueTypeCastGenericWithStructRestrictionHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertValueTypeCastGenericWithStructRestrictionAsStruct(bool useInterpreter)
        {
            CheckValueTypeCastGenericWithStructRestrictionHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertValueTypeCastGenericWithDateTime(bool useInterpreter)
        {
            CheckValueTypeCastGenericWithStructRestrictionHelper<DateTime>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertValueTypeCastGenericWithStructRestrictionAsStructWithStringAndField(bool useInterpreter)
        {
            CheckValueTypeCastGenericWithStructRestrictionHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        private static void CheckObjectCastGenericHelper<T>(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastGeneric<T>(array[i], useInterpreter);
            }
        }

        private static void CheckObjectCastGenericWithClassRestrictionHelper<Tc>(bool useInterpreter) where Tc : class
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastGenericWithClassRestriction<Tc>(array[i], useInterpreter);
            }
        }

        private static void CheckObjectCastGenericWithStructRestrictionHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastGenericWithStructRestriction<Ts>(array[i], useInterpreter);
            }
        }

        private static void CheckGenericCastObjectHelper<T>(bool useInterpreter)
        {
            T[] array = new T[] { default(T) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericCastObject<T>(array[i], useInterpreter);
            }
        }

        private static void CheckGenericWithClassRestrictionCastObjectHelper<Tc>(bool useInterpreter) where Tc : class
        {
            Tc[] array = new Tc[] { null, default(Tc) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithClassRestrictionCastObject<Tc>(array[i], useInterpreter);
            }
        }

        private static void CheckGenericWithStructRestrictionCastObjectHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionCastObject<Ts>(array[i], useInterpreter);
            }
        }

        private static void CheckGenericWithStructRestrictionCastValueTypeHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionCastValueType<Ts>(array[i], useInterpreter);
            }
        }

        private static void CheckValueTypeCastGenericWithStructRestrictionHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeCastGenericWithStructRestriction<Ts>(array[i], useInterpreter);
            }
        }

        #endregion

        #region Test verifiers

        private static bool CanBeNull(Type type)
        {
            return !type.IsValueType || (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        private static void VerifyCustomCastCustom2(C value, bool useInterpreter)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Convert(Expression.Constant(value, typeof(C)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile(useInterpreter);

            if (value == null || value is D)
                Assert.Equal((D)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyCustomCastInterface(C value, bool useInterpreter)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.Convert(Expression.Constant(value, typeof(C)), typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCustomCastIEquatableOfCustom(C value, bool useInterpreter)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C)), typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCustomCastIEquatableOfCustom2(C value, bool useInterpreter)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C)), typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile(useInterpreter);

            if (value == null || value is IEquatable<D>)
                Assert.Equal((IEquatable<D>)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyCustomCastObject(C value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(C)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCustomArrayCastCustom2Array(C[] value, bool useInterpreter)
        {
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(D[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            if (value == null || value is D[])
                Assert.Equal((D[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyCustomArrayCastIEnumerableOfCustom(C[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<C>>> e =
                Expression.Lambda<Func<IEnumerable<C>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<C>> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCustomArrayCastIEnumerableOfCustom2(C[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<D>>> e =
                Expression.Lambda<Func<IEnumerable<D>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<D>> f = e.Compile(useInterpreter);

            if (value == null || value is IEnumerable<D>)
                Assert.Equal((IEnumerable<D>)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyCustomArrayCastIEnumerableOfInterface(C[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<I>>> e =
                Expression.Lambda<Func<IEnumerable<I>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<I>> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCustomArrayCastIEnumerableOfObject(C[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<object>>> e =
                Expression.Lambda<Func<IEnumerable<object>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<object>> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCustomArrayCastIListOfCustom(C[] value, bool useInterpreter)
        {
            Expression<Func<IList<C>>> e =
                Expression.Lambda<Func<IList<C>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IList<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<C>> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCustomArrayCastIListOfCustom2(C[] value, bool useInterpreter)
        {
            Expression<Func<IList<D>>> e =
                Expression.Lambda<Func<IList<D>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IList<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<D>> f = e.Compile(useInterpreter);

            if (value == null || value is IList<D>)
                Assert.Equal((IList<D>)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyCustomArrayCastIListOfInterface(C[] value, bool useInterpreter)
        {
            Expression<Func<IList<I>>> e =
                Expression.Lambda<Func<IList<I>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IList<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<I>> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCustomArrayCastIListOfObject(C[] value, bool useInterpreter)
        {
            Expression<Func<IList<object>>> e =
                Expression.Lambda<Func<IList<object>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IList<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<object>> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCustomArrayCastObjectArray(C[] value, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCustom2CastCustom(D value, bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Convert(Expression.Constant(value, typeof(D)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCustom2CastInterface(D value, bool useInterpreter)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.Convert(Expression.Constant(value, typeof(D)), typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCustom2CastIEquatableOfCustom(D value, bool useInterpreter)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.Convert(Expression.Constant(value, typeof(D)), typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCustom2CastIEquatableOfCustom2(D value, bool useInterpreter)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.Convert(Expression.Constant(value, typeof(D)), typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCustom2CastObject(D value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(D)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCustom2ArrayCastCustomArray(D[] value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(D[])), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            if (value == null || value is C[])
                Assert.Equal((C[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyDelegateCastFuncOfObject(Delegate value, bool useInterpreter)
        {
            Expression<Func<Func<object>>> e =
                Expression.Lambda<Func<Func<object>>>(
                    Expression.Convert(Expression.Constant(value, typeof(Delegate)), typeof(Func<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>> f = e.Compile(useInterpreter);

            if (value == null || value is Func<object>)
                Assert.Equal((Func<object>)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyDelegateCastObject(Delegate value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(Delegate)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyEnumCastEnumType(E value, bool useInterpreter)
        {
            Expression<Func<Enum>> e =
                Expression.Lambda<Func<Enum>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(Enum)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Enum> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyEnumCastObject(E value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyEnumTypeCastEnum(Enum value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(Enum)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value == null)
                Assert.Throws<NullReferenceException>(() => f());
            else
            {
                E expected = default(E);
                try
                {
                    expected = (E)value;
                }
                catch (InvalidCastException)
                {
                    Assert.Throws<InvalidCastException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
        }

        private static void VerifyEnumTypeCastObject(Enum value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(Enum)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyFuncOfObjectCastDelegate(Func<object> value, bool useInterpreter)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.Convert(Expression.Constant(value, typeof(Func<object>)), typeof(Delegate)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyInterfaceCastCustom(I value, bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Convert(Expression.Constant(value, typeof(I)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);

            if (value == null || value is C)
                Assert.Equal((C)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyInterfaceCastCustom2(I value, bool useInterpreter)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Convert(Expression.Constant(value, typeof(I)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile(useInterpreter);

            if (value == null || value is D)
                Assert.Equal((D)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyInterfaceCastObject(I value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(I)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyIEnumerableOfCustomCastCustomArray(IEnumerable<C> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            if (value == null || value is C[])
                Assert.Equal((C[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIEnumerableOfCustomCastObjectArray(IEnumerable<C> value, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            if (value == null || value is object[])
                Assert.Equal((object[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIEnumerableOfCustom2CastCustomArray(IEnumerable<D> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<D>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            if (value == null || value is C[])
                Assert.Equal((C[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIEnumerableOfInterfaceCastCustomArray(IEnumerable<I> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<I>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            if (value == null || value is C[])
                Assert.Equal((C[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIEnumerableOfInterfaceCastObjectArray(IEnumerable<I> value, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<I>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            if (value == null || value is object[])
                Assert.Equal((object[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIEnumerableOfObjectCastCustomArray(IEnumerable<object> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<object>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            if (value == null || value is C[])
                Assert.Equal((C[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIEnumerableOfObjectCastObjectArray(IEnumerable<object> value, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<object>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            if (value == null || value is object[])
                Assert.Equal((object[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIEnumerableOfStructCastStructArray(IEnumerable<S> value, bool useInterpreter)
        {
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<S>)), typeof(S[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            if (value == null || value is S[])
                Assert.Equal((S[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIEquatableOfCustomCastCustom(IEquatable<C> value, bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEquatable<C>)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);

            if (value == null || value is C)
                Assert.Equal((C)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIEquatableOfCustomCastCustom2(IEquatable<C> value, bool useInterpreter)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEquatable<C>)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile(useInterpreter);

            if (value == null || value is D)
                Assert.Equal((D)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIEquatableOfCustomCastObject(IEquatable<C> value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEquatable<C>)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyIEquatableOfCustom2CastCustom(IEquatable<D> value, bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEquatable<D>)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);

            if (value == null || value is C)
                Assert.Equal((C)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIEquatableOfCustom2CastCustom2(IEquatable<D> value, bool useInterpreter)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEquatable<D>)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile(useInterpreter);

            if (value == null || value is D)
                Assert.Equal((D)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIEquatableOfCustom2CastObject(IEquatable<D> value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEquatable<D>)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyIEquatableOfStructCastStruct(IEquatable<S> value, bool useInterpreter)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEquatable<S>)), typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile(useInterpreter);

            if (value == null)
                Assert.Throws<NullReferenceException>(() => f());
            else if (value is S)
                Assert.Equal((S)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIListOfCustomCastCustomArray(IList<C> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<C>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            if (value == null || value is C[])
                Assert.Equal((C[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIListOfCustomCastObjectArray(IList<C> value, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<C>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            if (value == null || value is object[])
                Assert.Equal((object[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIListOfCustom2CastCustomArray(IList<D> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<D>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            if (value == null || value is C[])
                Assert.Equal((C[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIListOfInterfaceCastCustomArray(IList<I> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<I>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            if (value == null || value is C[])
                Assert.Equal((C[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIListOfInterfaceCastObjectArray(IList<I> value, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<I>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            if (value == null || value is object[])
                Assert.Equal((object[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIListOfObjectCastCustomArray(IList<object> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<object>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            if (value == null || value is C[])
                Assert.Equal((C[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIListOfObjectCastObjectArray(IList<object> value, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<object>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            if (value == null || value is Array)
                Assert.Equal(value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIListOfStructCastStructArray(IList<S> value, bool useInterpreter)
        {
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<S>)), typeof(S[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            if (value == null || value is Array)
                Assert.Equal((S[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyIntCastObject(int value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyIntCastValueType(int value, bool useInterpreter)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyObjectCastCustom(object value, bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);

            if (value == null || value is C)
                Assert.Equal((C)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectCastCustom2(object value, bool useInterpreter)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile(useInterpreter);

            if (value == null || value is D)
                Assert.Equal((D)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectCastDelegate(object value, bool useInterpreter)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(Delegate)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate> f = e.Compile(useInterpreter);

            if (value == null || value is Delegate)
                Assert.Equal((Delegate)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectCastEnum(object value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value == null)
                Assert.Throws<NullReferenceException>(() => f());
            else
            {
                E expected = default(E);
                try
                {
                    expected = (E)value;
                }
                catch (InvalidCastException)
                {
                    Assert.Throws<InvalidCastException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
        }

        private static void VerifyObjectCastEnumType(object value, bool useInterpreter)
        {
            Expression<Func<Enum>> e =
                Expression.Lambda<Func<Enum>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(Enum)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Enum> f = e.Compile(useInterpreter);

            if (value == null || value is Enum)
                Assert.Equal((Enum)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectCastInterface(object value, bool useInterpreter)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile(useInterpreter);

            if (value == null || value is I)
                Assert.Equal((I)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectCastIEquatableOfCustom(object value, bool useInterpreter)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile(useInterpreter);

            if (value == null || value is IEquatable<C>)
                Assert.Equal((IEquatable<C>)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectCastIEquatableOfCustom2(object value, bool useInterpreter)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile(useInterpreter);

            if (value == null || value is IEquatable<D>)
                Assert.Equal((IEquatable<D>)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectCastInt(object value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value == null)
                Assert.Throws<NullReferenceException>(() => f());
            else if (value is int)
                Assert.Equal((int)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectCastStruct(object value, bool useInterpreter)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile(useInterpreter);

            if (value == null)
                Assert.Throws<NullReferenceException>(() => f());
            else if (value is S)
                Assert.Equal((S)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectCastValueType(object value, bool useInterpreter)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile(useInterpreter);

            if (value == null || value is ValueType)
                Assert.Equal((ValueType)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectArrayCastCustomArray(object[] value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(object[])), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            if (value == null || value is C[])
                Assert.Equal((C[])value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectArrayCastIEnumerableOfCustom(object[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<C>>> e =
                Expression.Lambda<Func<IEnumerable<C>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<C>> f = e.Compile(useInterpreter);

            if (value == null || value is IEnumerable<C>)
                Assert.Equal((IEnumerable<C>)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectArrayCastIEnumerableOfInterface(object[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<I>>> e =
                Expression.Lambda<Func<IEnumerable<I>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<I>> f = e.Compile(useInterpreter);

            if (value == null || value is IEnumerable<I>)
                Assert.Equal((IEnumerable<I>)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectArrayCastIEnumerableOfObject(object[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<object>>> e =
                Expression.Lambda<Func<IEnumerable<object>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<object>> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyObjectArrayCastIListOfCustom(object[] value, bool useInterpreter)
        {
            Expression<Func<IList<C>>> e =
                Expression.Lambda<Func<IList<C>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object[])), typeof(IList<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<C>> f = e.Compile(useInterpreter);

            if (value == null || value is IList<C>)
                Assert.Equal((IList<C>)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectArrayCastIListOfInterface(object[] value, bool useInterpreter)
        {
            Expression<Func<IList<I>>> e =
                Expression.Lambda<Func<IList<I>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object[])), typeof(IList<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<I>> f = e.Compile(useInterpreter);

            if (value == null || value is IList<I>)
                Assert.Equal((IList<I>)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectArrayCastIListOfObject(object[] value, bool useInterpreter)
        {
            Expression<Func<IList<object>>> e =
                Expression.Lambda<Func<IList<object>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object[])), typeof(IList<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<object>> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyStructCastIEquatableOfStruct(S value, bool useInterpreter)
        {
            Expression<Func<IEquatable<S>>> e =
                Expression.Lambda<Func<IEquatable<S>>>(
                    Expression.Convert(Expression.Constant(value, typeof(S)), typeof(IEquatable<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<S>> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyStructCastObject(S value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(S)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyStructCastValueType(S value, bool useInterpreter)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.Convert(Expression.Constant(value, typeof(S)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyStructArrayCastIEnumerableOfStruct(S[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<S>>> e =
                Expression.Lambda<Func<IEnumerable<S>>>(
                    Expression.Convert(Expression.Constant(value, typeof(S[])), typeof(IEnumerable<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<S>> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyStructArrayCastIListOfStruct(S[] value, bool useInterpreter)
        {
            Expression<Func<IList<S>>> e =
                Expression.Lambda<Func<IList<S>>>(
                    Expression.Convert(Expression.Constant(value, typeof(S[])), typeof(IList<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<S>> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyValueTypeCastInt(ValueType value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(ValueType)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value == null)
                Assert.Throws<NullReferenceException>(() => f());
            else
            {
                int expected;
                try
                {
                    expected = (int)value;
                }
                catch (InvalidCastException)
                {
                    Assert.Throws<InvalidCastException>(() => f());
                    return;
                }
                Assert.Equal(expected, f());
            }
        }

        private static void VerifyValueTypeCastObject(ValueType value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(ValueType)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyValueTypeCastStruct(ValueType value, bool useInterpreter)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.Convert(Expression.Constant(value, typeof(ValueType)), typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile(useInterpreter);

            if (value == null)
                Assert.Throws<NullReferenceException>(() => f());
            else if (value is S)
                Assert.Equal((S)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectCastGeneric<T>(object value, bool useInterpreter)
        {
            Expression<Func<T>> e =
                Expression.Lambda<Func<T>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(T)),
                    Enumerable.Empty<ParameterExpression>());
            Func<T> f = e.Compile(useInterpreter);

            if (value == null && !(CanBeNull(typeof(T))))
                Assert.Throws<NullReferenceException>(() => f());
            else if (value == null || value is T)
                Assert.Equal((T)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyLongEnumCastEnum(El value, bool useInterpreter)
        {
            Expression<Func<E>> e = Expression.Lambda<Func<E>>(
                Expression.Convert(Expression.Constant(value, typeof(El)), typeof(E)));
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E)value), f());
        }

        private static void VerifyEnumCastLongEnum(E value, bool useInterpreter)
        {
            Expression<Func<El>> e = Expression.Lambda<Func<El>>(
                Expression.Convert(Expression.Constant(value, typeof(E)), typeof(El)));
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyUnsignedEnumCastEnum(Eu value, bool useInterpreter)
        {
            Expression<Func<E>> e = Expression.Lambda<Func<E>>(
                Expression.Convert(Expression.Constant(value, typeof(Eu)), typeof(E)));
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E)value), f());
        }

        private static void VerifyUnsignedEnumInObjectCastEnum(Eu value, bool useInterpreter)
        {
            Expression<Func<E>> e = Expression.Lambda<Func<E>>(
                Expression.Convert(Expression.Constant(value, typeof(object)), typeof(E)));
            Func<E> f = e.Compile(useInterpreter);

            Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectCastGenericWithClassRestriction<Tc>(object value, bool useInterpreter) where Tc : class
        {
            Expression<Func<Tc>> e =
                Expression.Lambda<Func<Tc>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(Tc)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc> f = e.Compile(useInterpreter);

            if (value == null || value is Tc)
                Assert.Equal((Tc)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyObjectCastGenericWithStructRestriction<Ts>(object value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<Ts>> e =
                Expression.Lambda<Func<Ts>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(Ts)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts> f = e.Compile(useInterpreter);

            if (value == null)
                Assert.Throws<NullReferenceException>(() => f());
            else if (value is Ts)
                Assert.Equal((Ts)value, f());
            else
                Assert.Throws<InvalidCastException>(() => f());
        }

        private static void VerifyGenericCastObject<T>(T value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(T)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithClassRestrictionCastObject<Tc>(Tc value, bool useInterpreter) where Tc : class
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(Tc)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithStructRestrictionCastObject<Ts>(Ts value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(Ts)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithStructRestrictionCastValueType<Ts>(Ts value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.Convert(Expression.Constant(value, typeof(Ts)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyValueTypeCastGenericWithStructRestriction<Ts>(ValueType value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<Ts>> e =
                Expression.Lambda<Func<Ts>>(
                    Expression.Convert(Expression.Constant(value, typeof(ValueType)), typeof(Ts)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts> f = e.Compile(useInterpreter);

            if (value == null)
                Assert.Throws<NullReferenceException>(() => f());
            else
            {
                Ts expected;
                try
                {
                    expected = (Ts)value;
                }
                catch (InvalidCastException)
                {
                    Assert.Throws<InvalidCastException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
        }

        #endregion

        public static IEnumerable<Type> EnumerableTypes()
        {
            yield return typeof(ByteEnum);
            yield return typeof(SByteEnum);
            yield return typeof(Int16Enum);
            yield return typeof(UInt16Enum);
            yield return typeof(Int32Enum);
            yield return typeof(UInt32Enum);
            yield return typeof(Int64Enum);
            yield return typeof(UInt64Enum);
#if FEATURE_COMPILE
            yield return NonCSharpTypes.CharEnumType;
            yield return NonCSharpTypes.BoolEnumType;
#endif
        }

        public static IEnumerable<object[]> EnumerableTypeArgs() => EnumerableTypes().Select(t => new object[] { t });

        public static IEnumerable<object[]> EnumerableTypesAndIncompatibleObjects()
            => from value in EnumerableTypes().Select(Activator.CreateInstance)
               from type in EnumerableTypes()
               where type != value.GetType()
               select new[] { type, value };

        public static IEnumerable<object[]> EnumerableTypesAndIncompatibleUnderlyingObjects()
            => from value in EnumerableTypes().Select(t => Activator.CreateInstance(Enum.GetUnderlyingType(t)))
               from type in EnumerableTypes()
               where Enum.GetUnderlyingType(type) != value.GetType()
               select new[] { type, value };

        [Theory, PerCompilationType(nameof(EnumerableTypeArgs))]
        public static void CanCastReferenceToUnderlyingTypeToEnumType(Type type, bool useInterpreter)
        {
            object value = Activator.CreateInstance(type);
            Expression<Func<bool>> exp = Expression.Lambda<Func<bool>>(
                Expression.Equal(
                    Expression.Default(type),
                    Expression.Convert(Expression.Constant(value, typeof(object)), type)));
            Func<bool> func = exp.Compile(useInterpreter);
            Assert.True(func());
        }

        [Theory, PerCompilationType(nameof(EnumerableTypeArgs))]
        public static void CanCastReferenceToUnderlyingTypeToEnumTypeChecked(Type type, bool useInterpreter)
        {
            object value = Activator.CreateInstance(type);
            Expression<Func<bool>> exp = Expression.Lambda<Func<bool>>(
                Expression.Equal(
                    Expression.Default(type),
                    Expression.ConvertChecked(Expression.Constant(value, typeof(object)), type)));
            Func<bool> func = exp.Compile(useInterpreter);
            Assert.True(func());
        }

        [Theory, PerCompilationType(nameof(EnumerableTypesAndIncompatibleObjects))]
        public static void CannotCastReferenceToWrongUnderlyingTypeEnum(Type type, object value, bool useInterpreter)
        {
            Expression<Action> exp = Expression.Lambda<Action>(
                Expression.Block(
                    Expression.Convert(Expression.Constant(value, typeof(object)), type),
                    Expression.Empty()));
            Action act = exp.Compile(useInterpreter);
            Assert.Throws<InvalidCastException>(act);
        }

        [Theory, PerCompilationType(nameof(EnumerableTypesAndIncompatibleObjects))]
        public static void CannotCastReferenceToWrongUnderlyingTypeEnumChecked(Type type, object value, bool useInterpreter)
        {
            Expression<Action> exp = Expression.Lambda<Action>(
                Expression.Block(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(object)), type),
                    Expression.Empty()));
            Action act = exp.Compile(useInterpreter);
            Assert.Throws<InvalidCastException>(act);
        }

        [Theory, PerCompilationType(nameof(EnumerableTypeArgs))]
        public static void CanCastUnderlyingTypeToEnumType(Type type, bool useInterpreter)
        {
            Type underlying = Enum.GetUnderlyingType(type);
            object value = Activator.CreateInstance(underlying);
            Expression<Func<bool>> exp = Expression.Lambda<Func<bool>>(
                Expression.Equal(
                    Expression.Default(type),
                    Expression.Convert(Expression.Constant(value, underlying), type)));
            Func<bool> func = exp.Compile(useInterpreter);
            Assert.True(func());
        }

        [Theory, PerCompilationType(nameof(EnumerableTypeArgs))]
        public static void CanCastUnderlyingTypeToEnumTypeChecked(Type type, bool useInterpreter)
        {
            Type underlying = Enum.GetUnderlyingType(type);
            object value = Activator.CreateInstance(underlying);
            Expression<Func<bool>> exp = Expression.Lambda<Func<bool>>(
                Expression.Equal(
                    Expression.Default(type),
                    Expression.ConvertChecked(Expression.Constant(value, underlying), type)));
            Func<bool> func = exp.Compile(useInterpreter);
            Assert.True(func());
        }

        [Theory, PerCompilationType(nameof(EnumerableTypeArgs))]
        public static void CanCastEnumTypeToUnderlyingType(Type type, bool useInterpreter)
        {
            Type underlying = Enum.GetUnderlyingType(type);
            object value = Activator.CreateInstance(type);
            Expression<Func<bool>> exp = Expression.Lambda<Func<bool>>(
                Expression.Equal(
                    Expression.Default(underlying),
                    Expression.Convert(Expression.Constant(value, type), underlying)));
            Func<bool> func = exp.Compile(useInterpreter);
            Assert.True(func());
        }

        [Theory, PerCompilationType(nameof(EnumerableTypeArgs))]
        public static void CanCastEnumTypeToUnderlyingTypeChecked(Type type, bool useInterpreter)
        {
            Type underlying = Enum.GetUnderlyingType(type);
            object value = Activator.CreateInstance(type);
            Expression<Func<bool>> exp = Expression.Lambda<Func<bool>>(
                Expression.Equal(
                    Expression.Default(underlying),
                    Expression.ConvertChecked(Expression.Constant(value, type), underlying)));
            Func<bool> func = exp.Compile(useInterpreter);
            Assert.True(func());
        }

        [Theory, PerCompilationType(nameof(EnumerableTypesAndIncompatibleUnderlyingObjects))]
        public static void CannotCastWrongUnderlyingTypeEnum(Type type, object value, bool useInterpreter)
        {
            Expression<Action> exp = Expression.Lambda<Action>(
                Expression.Block(
                    Expression.Convert(Expression.Constant(value, typeof(object)), type),
                    Expression.Empty()));
            Action act = exp.Compile(useInterpreter);
            Assert.Throws<InvalidCastException>(act);
        }

        [Theory, PerCompilationType(nameof(EnumerableTypesAndIncompatibleUnderlyingObjects))]
        public static void CannotCastWrongUnderlyingTypeEnumChecked(Type type, object value, bool useInterpreter)
        {
            Expression<Action> exp = Expression.Lambda<Action>(
                Expression.Block(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(object)), type),
                    Expression.Empty()));
            Action act = exp.Compile(useInterpreter);
            Assert.Throws<InvalidCastException>(act);
        }

        public static IEnumerable<object[]> GreaterThanOrEqualToOneArguments => new[]
        {
            new object[] { typeof(decimal?), 1m },
            new object[] { typeof(decimal), 1m },
            new object[] { typeof(int?), 1 },
            new object[] { typeof(int), 1 },
            new object[] { typeof(long?), 1L },
            new object[] { typeof(long), 1L },
            new object[] { typeof(double?), 1.0 },
            new object[] { typeof(double), 1.0 }
        };

        [Theory]
        [MemberData(nameof(GreaterThanOrEqualToOneArguments))]
        public static void GreaterThanOrEqualToOne(Type type, object value)
        {
            // two constant of different types that you want to compare
            Expression x = Expression.Constant(value, type);
            Expression y = Expression.Constant(1.0f, typeof(float));

            // (T)y -- cast to a common type to allow the comparison later
            y = Expression.Convert(y, type);

            // (T)y == default(T)
            Expression guard = Expression.Equal(y, Expression.Default(y.Type));

            // x >= (T)y
            Expression predicate = Expression.GreaterThanOrEqual(x, y);

            // (T)y == default(T) || x >= (T)y
            Expression overallExpression = Expression.OrElse(guard, predicate);

            Expression<Func<bool>> lambda = Expression.Lambda<Func<bool>>(overallExpression);

            Assert.True(lambda.Compile(preferInterpretation: false).Invoke());
            Assert.True(lambda.Compile(preferInterpretation: true).Invoke());
        }
    }
}
