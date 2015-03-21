// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Cast
{
    public static unsafe class CastTests
    {
        #region Test methods

        [Fact]
        public static void CheckCustomCastCustom2Test()
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomCastCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomCastInterfaceTest()
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomCastInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomCastIEquatableOfCustomTest()
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomCastIEquatableOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomCastIEquatableOfCustom2Test()
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomCastIEquatableOfCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomCastObjectTest()
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomCastObject(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayCastCustom2ArrayTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastCustom2Array(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayCastIEnumerableOfCustomTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIEnumerableOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayCastIEnumerableOfCustom2Test()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIEnumerableOfCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayCastIEnumerableOfInterfaceTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIEnumerableOfInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayCastIEnumerableOfObjectTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIEnumerableOfObject(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayCastIListOfCustomTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIListOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayCastIListOfCustom2Test()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIListOfCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayCastIListOfInterfaceTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIListOfInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayCastIListOfObjectTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastIListOfObject(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayCastObjectArrayTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayCastObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2CastCustomTest()
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2CastCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2CastInterfaceTest()
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2CastInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2CastIEquatableOfCustomTest()
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2CastIEquatableOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2CastIEquatableOfCustom2Test()
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2CastIEquatableOfCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2CastObjectTest()
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2CastObject(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2ArrayCastCustomArrayTest()
        {
            D[][] array = new D[][] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2ArrayCastCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckDelegateCastFuncOfObjectTest()
        {
            Delegate[] array = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyDelegateCastFuncOfObject(array[i]);
            }
        }

        [Fact]
        public static void CheckDelegateCastObjectTest()
        {
            Delegate[] array = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyDelegateCastObject(array[i]);
            }
        }

        [Fact]
        public static void CheckEnumCastEnumTypeTest()
        {
            E[] array = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumCastEnumType(array[i]);
            }
        }

        [Fact]
        public static void CheckEnumCastObjectTest()
        {
            E[] array = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumCastObject(array[i]);
            }
        }

        [Fact]
        public static void CheckEnumTypeCastEnumTest()
        {
            Enum[] array = new Enum[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumTypeCastEnum(array[i]);
            }
        }

        [Fact]
        public static void CheckEnumTypeCastObjectTest()
        {
            Enum[] array = new Enum[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumTypeCastObject(array[i]);
            }
        }

        [Fact]
        public static void CheckFuncOfObjectCastDelegateTest()
        {
            Func<object>[] array = new Func<object>[] { null, (Func<object>)delegate () { return null; } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyFuncOfObjectCastDelegate(array[i]);
            }
        }

        [Fact]
        public static void CheckInterfaceCastCustomTest()
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceCastCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckInterfaceCastCustom2Test()
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceCastCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckInterfaceCastObjectTest()
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceCastObject(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfCustomCastCustomArrayTest()
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomCastCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfCustomCastObjectArrayTest()
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomCastObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfCustom2CastCustomArrayTest()
        {
            IEnumerable<D>[] array = new IEnumerable<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustom2CastCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfInterfaceCastCustomArrayTest()
        {
            IEnumerable<I>[] array = new IEnumerable<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfInterfaceCastCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfInterfaceCastObjectArrayTest()
        {
            IEnumerable<I>[] array = new IEnumerable<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfInterfaceCastObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfObjectCastCustomArrayTest()
        {
            IEnumerable<object>[] array = new IEnumerable<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfObjectCastCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfObjectCastObjectArrayTest()
        {
            IEnumerable<object>[] array = new IEnumerable<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfObjectCastObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfStructCastStructArrayTest()
        {
            IEnumerable<S>[] array = new IEnumerable<S>[] { null, new S[] { default(S), new S() }, new S[10], new List<S>(), new List<S>(new S[] { default(S), new S() }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfStructCastStructArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableOfCustomCastCustomTest()
        {
            IEquatable<C>[] array = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustomCastCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableOfCustomCastCustom2Test()
        {
            IEquatable<C>[] array = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustomCastCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableOfCustomCastObjectTest()
        {
            IEquatable<C>[] array = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustomCastObject(array[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableOfCustom2CastCustomTest()
        {
            IEquatable<D>[] array = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustom2CastCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableOfCustom2CastCustom2Test()
        {
            IEquatable<D>[] array = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustom2CastCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableOfCustom2CastObjectTest()
        {
            IEquatable<D>[] array = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustom2CastObject(array[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableOfStructCastStructTest()
        {
            IEquatable<S>[] array = new IEquatable<S>[] { null };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfStructCastStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfCustomCastCustomArrayTest()
        {
            IList<C>[] array = new IList<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustomCastCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfCustomCastObjectArrayTest()
        {
            IList<C>[] array = new IList<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustomCastObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfCustom2CastCustomArrayTest()
        {
            IList<D>[] array = new IList<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustom2CastCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfInterfaceCastCustomArrayTest()
        {
            IList<I>[] array = new IList<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfInterfaceCastCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfInterfaceCastObjectArrayTest()
        {
            IList<I>[] array = new IList<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfInterfaceCastObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfObjectCastCustomArrayTest()
        {
            IList<object>[] array = new IList<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfObjectCastCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfObjectCastObjectArrayTest()
        {
            IList<object>[] array = new IList<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfObjectCastObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfStructCastStructArrayTest()
        {
            IList<S>[] array = new IList<S>[] { null, new S[] { default(S), new S() }, new S[10], new List<S>(), new List<S>(new S[] { default(S), new S() }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfStructCastStructArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIntCastObjectTest()
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIntCastObject(array[i]);
            }
        }

        [Fact]
        public static void CheckIntCastValueTypeTest()
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIntCastValueType(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectCastCustomTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectCastCustom2Test()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectCastDelegateTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastDelegate(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectCastEnumTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastEnum(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectCastEnumTypeTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastEnumType(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectCastInterfaceTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectCastIEquatableOfCustomTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastIEquatableOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectCastIEquatableOfCustom2Test()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastIEquatableOfCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectCastIntTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastInt(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectCastStructTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectCastValueTypeTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastValueType(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayCastCustomArrayTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayCastCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayCastIEnumerableOfCustomTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayCastIEnumerableOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayCastIEnumerableOfInterfaceTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayCastIEnumerableOfInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayCastIEnumerableOfObjectTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayCastIEnumerableOfObject(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayCastIListOfCustomTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayCastIListOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayCastIListOfInterfaceTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayCastIListOfInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayCastIListOfObjectTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayCastIListOfObject(array[i]);
            }
        }

        [Fact]
        public static void CheckStructCastIEquatableOfStructTest()
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructCastIEquatableOfStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckStructCastObjectTest()
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructCastObject(array[i]);
            }
        }

        [Fact]
        public static void CheckStructCastValueTypeTest()
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructCastValueType(array[i]);
            }
        }

        [Fact]
        public static void CheckStructArrayCastIEnumerableOfStructTest()
        {
            S[][] array = new S[][] { null, new S[] { default(S), new S() }, new S[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructArrayCastIEnumerableOfStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckStructArrayCastIListOfStructTest()
        {
            S[][] array = new S[][] { null, new S[] { default(S), new S() }, new S[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructArrayCastIListOfStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckValueTypeCastIntTest()
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeCastInt(array[i]);
            }
        }

        [Fact]
        public static void CheckValueTypeCastObjectTest()
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeCastObject(array[i]);
            }
        }

        [Fact]
        public static void CheckValueTypeCastStructTest()
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeCastStruct(array[i]);
            }
        }

        [Fact]
        public static void ConvertObjectCastGenericAsCustom()
        {
            CheckObjectCastGenericHelper<C>();
        }

        [Fact]
        public static void ConvertObjectCastGenericAsEnum()
        {
            CheckObjectCastGenericHelper<E>();
        }

        [Fact]
        public static void ConvertObjectCastGenericAsObject()
        {
            CheckObjectCastGenericHelper<object>();
        }

        [Fact]
        public static void ConvertObjectCastGenericAsStruct()
        {
            CheckObjectCastGenericHelper<S>();
        }

        [Fact]
        public static void ConvertObjectCastGenericAsStructWithStringAndField()
        {
            CheckObjectCastGenericHelper<Scs>();
        }

        [Fact]
        public static void ConvertObjectCastGenericWithClassRestrictionAsCustom()
        {
            CheckObjectCastGenericWithClassRestrictionHelper<C>();
        }

        [Fact]
        public static void ConvertObjectCastGenericWithClassRestrictionAsObject()
        {
            CheckObjectCastGenericWithClassRestrictionHelper<object>();
        }

        [Fact]
        public static void ConvertObjectCastGenericWithStructRestrictionAsEnum()
        {
            CheckObjectCastGenericWithStructRestrictionHelper<E>();
        }

        [Fact]
        public static void ConvertObjectCastGenericWithStructRestrictionAsStruct()
        {
            CheckObjectCastGenericWithStructRestrictionHelper<S>();
        }

        [Fact]
        public static void ConvertObjectCastGenericWithStructRestrictionAsStructWithStringAndField()
        {
            CheckObjectCastGenericWithStructRestrictionHelper<Scs>();
        }

        [Fact]
        public static void ConvertGenericCastObjectAsCustom()
        {
            CheckGenericCastObjectHelper<C>();
        }

        [Fact]
        public static void ConvertGenericCastObjectAsEnum()
        {
            CheckGenericCastObjectHelper<E>();
        }

        [Fact]
        public static void ConvertGenericCastObjectAsObject()
        {
            CheckGenericCastObjectHelper<object>();
        }

        [Fact]
        public static void ConvertGenericCastObjectAsStruct()
        {
            CheckGenericCastObjectHelper<S>();
        }

        [Fact]
        public static void ConvertGenericCastObjectAsStructWithStringAndField()
        {
            CheckGenericCastObjectHelper<Scs>();
        }

        [Fact]
        public static void ConvertGenericWithClassRestrictionCastObjectAsCustom()
        {
            CheckGenericWithClassRestrictionCastObjectHelper<C>();
        }

        [Fact]
        public static void ConvertGenericWithClassRestrictionCastObjectAsObject()
        {
            CheckGenericWithClassRestrictionCastObjectHelper<object>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastObjectAsEnum()
        {
            CheckGenericWithStructRestrictionCastObjectHelper<E>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStruct()
        {
            CheckGenericWithStructRestrictionCastObjectHelper<S>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStructWithStringAndField()
        {
            CheckGenericWithStructRestrictionCastObjectHelper<Scs>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsEnum()
        {
            CheckGenericWithStructRestrictionCastValueTypeHelper<E>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStruct()
        {
            CheckGenericWithStructRestrictionCastValueTypeHelper<S>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStructWithStringAndField()
        {
            CheckGenericWithStructRestrictionCastValueTypeHelper<Scs>();
        }

        [Fact]
        public static void ConvertValueTypeCastGenericWithStructRestrictionAsEnum()
        {
            CheckValueTypeCastGenericWithStructRestrictionHelper<E>();
        }

        [Fact]
        public static void ConvertValueTypeCastGenericWithStructRestrictionAsStruct()
        {
            CheckValueTypeCastGenericWithStructRestrictionHelper<S>();
        }

        [Fact]
        public static void ConvertValueTypeCastGenericWithStructRestrictionAsStructWithStringAndField()
        {
            CheckValueTypeCastGenericWithStructRestrictionHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        private static void CheckObjectCastGenericHelper<T>()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastGeneric<T>(array[i]);
            }
        }

        private static void CheckObjectCastGenericWithClassRestrictionHelper<Tc>() where Tc : class
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastGenericWithClassRestriction<Tc>(array[i]);
            }
        }

        private static void CheckObjectCastGenericWithStructRestrictionHelper<Ts>() where Ts : struct
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectCastGenericWithStructRestriction<Ts>(array[i]);
            }
        }

        private static void CheckGenericCastObjectHelper<T>()
        {
            T[] array = new T[] { default(T) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericCastObject<T>(array[i]);
            }
        }

        private static void CheckGenericWithClassRestrictionCastObjectHelper<Tc>() where Tc : class
        {
            Tc[] array = new Tc[] { null, default(Tc) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithClassRestrictionCastObject<Tc>(array[i]);
            }
        }

        private static void CheckGenericWithStructRestrictionCastObjectHelper<Ts>() where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionCastObject<Ts>(array[i]);
            }
        }

        private static void CheckGenericWithStructRestrictionCastValueTypeHelper<Ts>() where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionCastValueType<Ts>(array[i]);
            }
        }

        private static void CheckValueTypeCastGenericWithStructRestrictionHelper<Ts>() where Ts : struct
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeCastGenericWithStructRestriction<Ts>(array[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyCustomCastCustom2(C value)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Convert(Expression.Constant(value, typeof(C)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile();

            // compute the value with the expression tree
            D etResult = default(D);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            D csResult = default(D);
            Exception csException = null;
            try
            {
                csResult = (D)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustomCastInterface(C value)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.Convert(Expression.Constant(value, typeof(C)), typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile();

            // compute the value with the expression tree
            I etResult = default(I);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            I csResult = default(I);
            Exception csException = null;
            try
            {
                csResult = (I)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustomCastIEquatableOfCustom(C value)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C)), typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile();

            // compute the value with the expression tree
            IEquatable<C> etResult = default(IEquatable<C>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEquatable<C> csResult = default(IEquatable<C>);
            Exception csException = null;
            try
            {
                csResult = (IEquatable<C>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustomCastIEquatableOfCustom2(C value)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C)), typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile();

            // compute the value with the expression tree
            IEquatable<D> etResult = default(IEquatable<D>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEquatable<D> csResult = default(IEquatable<D>);
            Exception csException = null;
            try
            {
                csResult = (IEquatable<D>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustomCastObject(C value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(C)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = (object)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustomArrayCastCustom2Array(C[] value)
        {
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(D[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile();

            // compute the value with the expression tree
            D[] etResult = default(D[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            D[] csResult = default(D[]);
            Exception csException = null;
            try
            {
                csResult = (D[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustomArrayCastIEnumerableOfCustom(C[] value)
        {
            Expression<Func<IEnumerable<C>>> e =
                Expression.Lambda<Func<IEnumerable<C>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<C>> f = e.Compile();

            // compute the value with the expression tree
            IEnumerable<C> etResult = default(IEnumerable<C>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEnumerable<C> csResult = default(IEnumerable<C>);
            Exception csException = null;
            try
            {
                csResult = (IEnumerable<C>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustomArrayCastIEnumerableOfCustom2(C[] value)
        {
            Expression<Func<IEnumerable<D>>> e =
                Expression.Lambda<Func<IEnumerable<D>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<D>> f = e.Compile();

            // compute the value with the expression tree
            IEnumerable<D> etResult = default(IEnumerable<D>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEnumerable<D> csResult = default(IEnumerable<D>);
            Exception csException = null;
            try
            {
                csResult = (IEnumerable<D>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustomArrayCastIEnumerableOfInterface(C[] value)
        {
            Expression<Func<IEnumerable<I>>> e =
                Expression.Lambda<Func<IEnumerable<I>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<I>> f = e.Compile();

            // compute the value with the expression tree
            IEnumerable<I> etResult = default(IEnumerable<I>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEnumerable<I> csResult = default(IEnumerable<I>);
            Exception csException = null;
            try
            {
                csResult = (IEnumerable<I>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustomArrayCastIEnumerableOfObject(C[] value)
        {
            Expression<Func<IEnumerable<object>>> e =
                Expression.Lambda<Func<IEnumerable<object>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<object>> f = e.Compile();

            // compute the value with the expression tree
            IEnumerable<object> etResult = default(IEnumerable<object>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEnumerable<object> csResult = default(IEnumerable<object>);
            Exception csException = null;
            try
            {
                csResult = (IEnumerable<object>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustomArrayCastIListOfCustom(C[] value)
        {
            Expression<Func<IList<C>>> e =
                Expression.Lambda<Func<IList<C>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IList<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<C>> f = e.Compile();

            // compute the value with the expression tree
            IList<C> etResult = default(IList<C>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IList<C> csResult = default(IList<C>);
            Exception csException = null;
            try
            {
                csResult = (IList<C>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustomArrayCastIListOfCustom2(C[] value)
        {
            Expression<Func<IList<D>>> e =
                Expression.Lambda<Func<IList<D>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IList<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<D>> f = e.Compile();

            // compute the value with the expression tree
            IList<D> etResult = default(IList<D>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IList<D> csResult = default(IList<D>);
            Exception csException = null;
            try
            {
                csResult = (IList<D>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustomArrayCastIListOfInterface(C[] value)
        {
            Expression<Func<IList<I>>> e =
                Expression.Lambda<Func<IList<I>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IList<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<I>> f = e.Compile();

            // compute the value with the expression tree
            IList<I> etResult = default(IList<I>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IList<I> csResult = default(IList<I>);
            Exception csException = null;
            try
            {
                csResult = (IList<I>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustomArrayCastIListOfObject(C[] value)
        {
            Expression<Func<IList<object>>> e =
                Expression.Lambda<Func<IList<object>>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(IList<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<object>> f = e.Compile();

            // compute the value with the expression tree
            IList<object> etResult = default(IList<object>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IList<object> csResult = default(IList<object>);
            Exception csException = null;
            try
            {
                csResult = (IList<object>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustomArrayCastObjectArray(C[] value)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(C[])), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            // compute the value with the expression tree
            object[] etResult = default(object[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object[] csResult = default(object[]);
            Exception csException = null;
            try
            {
                csResult = (object[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustom2CastCustom(D value)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Convert(Expression.Constant(value, typeof(D)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile();

            // compute the value with the expression tree
            C etResult = default(C);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            C csResult = default(C);
            Exception csException = null;
            try
            {
                csResult = (C)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustom2CastInterface(D value)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.Convert(Expression.Constant(value, typeof(D)), typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile();

            // compute the value with the expression tree
            I etResult = default(I);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            I csResult = default(I);
            Exception csException = null;
            try
            {
                csResult = (I)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustom2CastIEquatableOfCustom(D value)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.Convert(Expression.Constant(value, typeof(D)), typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile();

            // compute the value with the expression tree
            IEquatable<C> etResult = default(IEquatable<C>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEquatable<C> csResult = default(IEquatable<C>);
            Exception csException = null;
            try
            {
                csResult = (IEquatable<C>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustom2CastIEquatableOfCustom2(D value)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.Convert(Expression.Constant(value, typeof(D)), typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile();

            // compute the value with the expression tree
            IEquatable<D> etResult = default(IEquatable<D>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEquatable<D> csResult = default(IEquatable<D>);
            Exception csException = null;
            try
            {
                csResult = (IEquatable<D>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustom2CastObject(D value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(D)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = (object)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustom2ArrayCastCustomArray(D[] value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(D[])), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // compute the value with the expression tree
            C[] etResult = default(C[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            C[] csResult = default(C[]);
            Exception csException = null;
            try
            {
                csResult = (C[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyDelegateCastFuncOfObject(Delegate value)
        {
            Expression<Func<Func<object>>> e =
                Expression.Lambda<Func<Func<object>>>(
                    Expression.Convert(Expression.Constant(value, typeof(Delegate)), typeof(Func<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>> f = e.Compile();

            // compute the value with the expression tree
            Func<object> etResult = default(Func<object>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            Func<object> csResult = default(Func<object>);
            Exception csException = null;
            try
            {
                csResult = (Func<object>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyDelegateCastObject(Delegate value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(Delegate)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = (object)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyEnumCastEnumType(E value)
        {
            Expression<Func<Enum>> e =
                Expression.Lambda<Func<Enum>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(Enum)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Enum> f = e.Compile();

            // compute the value with the expression tree
            Enum etResult = default(Enum);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            Enum csResult = default(Enum);
            Exception csException = null;
            try
            {
                csResult = (Enum)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyEnumCastObject(E value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = (object)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyEnumTypeCastEnum(Enum value)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(Enum)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile();

            // compute the value with the expression tree
            E etResult = default(E);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            E csResult = default(E);
            Exception csException = null;
            try
            {
                csResult = (E)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyEnumTypeCastObject(Enum value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(Enum)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = (object)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyFuncOfObjectCastDelegate(Func<object> value)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.Convert(Expression.Constant(value, typeof(Func<object>)), typeof(Delegate)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate> f = e.Compile();

            // compute the value with the expression tree
            Delegate etResult = default(Delegate);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            Delegate csResult = default(Delegate);
            Exception csException = null;
            try
            {
                csResult = (Delegate)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyInterfaceCastCustom(I value)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Convert(Expression.Constant(value, typeof(I)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile();

            // compute the value with the expression tree
            C etResult = default(C);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            C csResult = default(C);
            Exception csException = null;
            try
            {
                csResult = (C)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyInterfaceCastCustom2(I value)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Convert(Expression.Constant(value, typeof(I)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile();

            // compute the value with the expression tree
            D etResult = default(D);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            D csResult = default(D);
            Exception csException = null;
            try
            {
                csResult = (D)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyInterfaceCastObject(I value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(I)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = (object)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEnumerableOfCustomCastCustomArray(IEnumerable<C> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // compute the value with the expression tree
            C[] etResult = default(C[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            C[] csResult = default(C[]);
            Exception csException = null;
            try
            {
                csResult = (C[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEnumerableOfCustomCastObjectArray(IEnumerable<C> value)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            // compute the value with the expression tree
            object[] etResult = default(object[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object[] csResult = default(object[]);
            Exception csException = null;
            try
            {
                csResult = (object[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEnumerableOfCustom2CastCustomArray(IEnumerable<D> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<D>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // compute the value with the expression tree
            C[] etResult = default(C[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            C[] csResult = default(C[]);
            Exception csException = null;
            try
            {
                csResult = (C[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEnumerableOfInterfaceCastCustomArray(IEnumerable<I> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<I>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // compute the value with the expression tree
            C[] etResult = default(C[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            C[] csResult = default(C[]);
            Exception csException = null;
            try
            {
                csResult = (C[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEnumerableOfInterfaceCastObjectArray(IEnumerable<I> value)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<I>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            // compute the value with the expression tree
            object[] etResult = default(object[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object[] csResult = default(object[]);
            Exception csException = null;
            try
            {
                csResult = (object[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEnumerableOfObjectCastCustomArray(IEnumerable<object> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<object>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // compute the value with the expression tree
            C[] etResult = default(C[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            C[] csResult = default(C[]);
            Exception csException = null;
            try
            {
                csResult = (C[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEnumerableOfObjectCastObjectArray(IEnumerable<object> value)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<object>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            // compute the value with the expression tree
            object[] etResult = default(object[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object[] csResult = default(object[]);
            Exception csException = null;
            try
            {
                csResult = (object[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEnumerableOfStructCastStructArray(IEnumerable<S> value)
        {
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEnumerable<S>)), typeof(S[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile();

            // compute the value with the expression tree
            S[] etResult = default(S[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            S[] csResult = default(S[]);
            Exception csException = null;
            try
            {
                csResult = (S[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEquatableOfCustomCastCustom(IEquatable<C> value)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEquatable<C>)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile();

            // compute the value with the expression tree
            C etResult = default(C);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            C csResult = default(C);
            Exception csException = null;
            try
            {
                csResult = (C)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEquatableOfCustomCastCustom2(IEquatable<C> value)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEquatable<C>)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile();

            // compute the value with the expression tree
            D etResult = default(D);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            D csResult = default(D);
            Exception csException = null;
            try
            {
                csResult = (D)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEquatableOfCustomCastObject(IEquatable<C> value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEquatable<C>)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = (object)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEquatableOfCustom2CastCustom(IEquatable<D> value)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEquatable<D>)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile();

            // compute the value with the expression tree
            C etResult = default(C);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            C csResult = default(C);
            Exception csException = null;
            try
            {
                csResult = (C)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEquatableOfCustom2CastCustom2(IEquatable<D> value)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEquatable<D>)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile();

            // compute the value with the expression tree
            D etResult = default(D);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            D csResult = default(D);
            Exception csException = null;
            try
            {
                csResult = (D)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEquatableOfCustom2CastObject(IEquatable<D> value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEquatable<D>)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = (object)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEquatableOfStructCastStruct(IEquatable<S> value)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.Convert(Expression.Constant(value, typeof(IEquatable<S>)), typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile();

            // compute the value with the expression tree
            S etResult = default(S);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            S csResult = default(S);
            Exception csException = null;
            try
            {
                csResult = (S)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIListOfCustomCastCustomArray(IList<C> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<C>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // compute the value with the expression tree
            C[] etResult = default(C[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            C[] csResult = default(C[]);
            Exception csException = null;
            try
            {
                csResult = (C[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIListOfCustomCastObjectArray(IList<C> value)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<C>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            // compute the value with the expression tree
            object[] etResult = default(object[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object[] csResult = default(object[]);
            Exception csException = null;
            try
            {
                csResult = (object[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIListOfCustom2CastCustomArray(IList<D> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<D>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // compute the value with the expression tree
            C[] etResult = default(C[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            C[] csResult = default(C[]);
            Exception csException = null;
            try
            {
                csResult = (C[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIListOfInterfaceCastCustomArray(IList<I> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<I>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // compute the value with the expression tree
            C[] etResult = default(C[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            C[] csResult = default(C[]);
            Exception csException = null;
            try
            {
                csResult = (C[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIListOfInterfaceCastObjectArray(IList<I> value)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<I>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            // compute the value with the expression tree
            object[] etResult = default(object[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object[] csResult = default(object[]);
            Exception csException = null;
            try
            {
                csResult = (object[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIListOfObjectCastCustomArray(IList<object> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<object>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // compute the value with the expression tree
            C[] etResult = default(C[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            C[] csResult = default(C[]);
            Exception csException = null;
            try
            {
                csResult = (C[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIListOfObjectCastObjectArray(IList<object> value)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<object>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            // compute the value with the expression tree
            object[] etResult = default(object[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object[] csResult = default(object[]);
            Exception csException = null;
            try
            {
                csResult = (object[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIListOfStructCastStructArray(IList<S> value)
        {
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(IList<S>)), typeof(S[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile();

            // compute the value with the expression tree
            S[] etResult = default(S[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            S[] csResult = default(S[]);
            Exception csException = null;
            try
            {
                csResult = (S[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIntCastObject(int value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = (object)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIntCastValueType(int value)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile();

            // compute the value with the expression tree
            ValueType etResult = default(ValueType);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            ValueType csResult = default(ValueType);
            Exception csException = null;
            try
            {
                csResult = (ValueType)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectCastCustom(object value)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile();

            // compute the value with the expression tree
            C etResult = default(C);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            C csResult = default(C);
            Exception csException = null;
            try
            {
                csResult = (C)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectCastCustom2(object value)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile();

            // compute the value with the expression tree
            D etResult = default(D);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            D csResult = default(D);
            Exception csException = null;
            try
            {
                csResult = (D)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectCastDelegate(object value)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(Delegate)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate> f = e.Compile();

            // compute the value with the expression tree
            Delegate etResult = default(Delegate);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            Delegate csResult = default(Delegate);
            Exception csException = null;
            try
            {
                csResult = (Delegate)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectCastEnum(object value)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile();

            // compute the value with the expression tree
            E etResult = default(E);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            E csResult = default(E);
            Exception csException = null;
            try
            {
                csResult = (E)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectCastEnumType(object value)
        {
            Expression<Func<Enum>> e =
                Expression.Lambda<Func<Enum>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(Enum)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Enum> f = e.Compile();

            // compute the value with the expression tree
            Enum etResult = default(Enum);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            Enum csResult = default(Enum);
            Exception csException = null;
            try
            {
                csResult = (Enum)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectCastInterface(object value)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile();

            // compute the value with the expression tree
            I etResult = default(I);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            I csResult = default(I);
            Exception csException = null;
            try
            {
                csResult = (I)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectCastIEquatableOfCustom(object value)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile();

            // compute the value with the expression tree
            IEquatable<C> etResult = default(IEquatable<C>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEquatable<C> csResult = default(IEquatable<C>);
            Exception csException = null;
            try
            {
                csResult = (IEquatable<C>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectCastIEquatableOfCustom2(object value)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile();

            // compute the value with the expression tree
            IEquatable<D> etResult = default(IEquatable<D>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEquatable<D> csResult = default(IEquatable<D>);
            Exception csException = null;
            try
            {
                csResult = (IEquatable<D>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectCastInt(object value)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();

            // compute the value with the expression tree
            int etResult = default(int);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            int csResult = default(int);
            Exception csException = null;
            try
            {
                csResult = (int)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectCastStruct(object value)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile();

            // compute the value with the expression tree
            S etResult = default(S);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            S csResult = default(S);
            Exception csException = null;
            try
            {
                csResult = (S)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectCastValueType(object value)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile();

            // compute the value with the expression tree
            ValueType etResult = default(ValueType);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            ValueType csResult = default(ValueType);
            Exception csException = null;
            try
            {
                csResult = (ValueType)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectArrayCastCustomArray(object[] value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Convert(Expression.Constant(value, typeof(object[])), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            // compute the value with the expression tree
            C[] etResult = default(C[]);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            C[] csResult = default(C[]);
            Exception csException = null;
            try
            {
                csResult = (C[])value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectArrayCastIEnumerableOfCustom(object[] value)
        {
            Expression<Func<IEnumerable<C>>> e =
                Expression.Lambda<Func<IEnumerable<C>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<C>> f = e.Compile();

            // compute the value with the expression tree
            IEnumerable<C> etResult = default(IEnumerable<C>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEnumerable<C> csResult = default(IEnumerable<C>);
            Exception csException = null;
            try
            {
                csResult = (IEnumerable<C>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectArrayCastIEnumerableOfInterface(object[] value)
        {
            Expression<Func<IEnumerable<I>>> e =
                Expression.Lambda<Func<IEnumerable<I>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<I>> f = e.Compile();

            // compute the value with the expression tree
            IEnumerable<I> etResult = default(IEnumerable<I>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEnumerable<I> csResult = default(IEnumerable<I>);
            Exception csException = null;
            try
            {
                csResult = (IEnumerable<I>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectArrayCastIEnumerableOfObject(object[] value)
        {
            Expression<Func<IEnumerable<object>>> e =
                Expression.Lambda<Func<IEnumerable<object>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<object>> f = e.Compile();

            // compute the value with the expression tree
            IEnumerable<object> etResult = default(IEnumerable<object>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEnumerable<object> csResult = default(IEnumerable<object>);
            Exception csException = null;
            try
            {
                csResult = (IEnumerable<object>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectArrayCastIListOfCustom(object[] value)
        {
            Expression<Func<IList<C>>> e =
                Expression.Lambda<Func<IList<C>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object[])), typeof(IList<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<C>> f = e.Compile();

            // compute the value with the expression tree
            IList<C> etResult = default(IList<C>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IList<C> csResult = default(IList<C>);
            Exception csException = null;
            try
            {
                csResult = (IList<C>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectArrayCastIListOfInterface(object[] value)
        {
            Expression<Func<IList<I>>> e =
                Expression.Lambda<Func<IList<I>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object[])), typeof(IList<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<I>> f = e.Compile();

            // compute the value with the expression tree
            IList<I> etResult = default(IList<I>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IList<I> csResult = default(IList<I>);
            Exception csException = null;
            try
            {
                csResult = (IList<I>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectArrayCastIListOfObject(object[] value)
        {
            Expression<Func<IList<object>>> e =
                Expression.Lambda<Func<IList<object>>>(
                    Expression.Convert(Expression.Constant(value, typeof(object[])), typeof(IList<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<object>> f = e.Compile();

            // compute the value with the expression tree
            IList<object> etResult = default(IList<object>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IList<object> csResult = default(IList<object>);
            Exception csException = null;
            try
            {
                csResult = (IList<object>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyStructCastIEquatableOfStruct(S value)
        {
            Expression<Func<IEquatable<S>>> e =
                Expression.Lambda<Func<IEquatable<S>>>(
                    Expression.Convert(Expression.Constant(value, typeof(S)), typeof(IEquatable<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<S>> f = e.Compile();

            // compute the value with the expression tree
            IEquatable<S> etResult = default(IEquatable<S>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEquatable<S> csResult = default(IEquatable<S>);
            Exception csException = null;
            try
            {
                csResult = (IEquatable<S>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyStructCastObject(S value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(S)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = (object)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyStructCastValueType(S value)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.Convert(Expression.Constant(value, typeof(S)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile();

            // compute the value with the expression tree
            ValueType etResult = default(ValueType);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            ValueType csResult = default(ValueType);
            Exception csException = null;
            try
            {
                csResult = (ValueType)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyStructArrayCastIEnumerableOfStruct(S[] value)
        {
            Expression<Func<IEnumerable<S>>> e =
                Expression.Lambda<Func<IEnumerable<S>>>(
                    Expression.Convert(Expression.Constant(value, typeof(S[])), typeof(IEnumerable<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<S>> f = e.Compile();

            // compute the value with the expression tree
            IEnumerable<S> etResult = default(IEnumerable<S>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEnumerable<S> csResult = default(IEnumerable<S>);
            Exception csException = null;
            try
            {
                csResult = (IEnumerable<S>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyStructArrayCastIListOfStruct(S[] value)
        {
            Expression<Func<IList<S>>> e =
                Expression.Lambda<Func<IList<S>>>(
                    Expression.Convert(Expression.Constant(value, typeof(S[])), typeof(IList<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<S>> f = e.Compile();

            // compute the value with the expression tree
            IList<S> etResult = default(IList<S>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IList<S> csResult = default(IList<S>);
            Exception csException = null;
            try
            {
                csResult = (IList<S>)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyValueTypeCastInt(ValueType value)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(ValueType)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();

            // compute the value with the expression tree
            int etResult = default(int);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            int csResult = default(int);
            Exception csException = null;
            try
            {
                csResult = (int)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyValueTypeCastObject(ValueType value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(ValueType)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = (object)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyValueTypeCastStruct(ValueType value)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.Convert(Expression.Constant(value, typeof(ValueType)), typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile();

            // compute the value with the expression tree
            S etResult = default(S);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            S csResult = default(S);
            Exception csException = null;
            try
            {
                csResult = (S)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectCastGeneric<T>(object value)
        {
            Expression<Func<T>> e =
                Expression.Lambda<Func<T>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(T)),
                    Enumerable.Empty<ParameterExpression>());
            Func<T> f = e.Compile();

            // compute the value with the expression tree
            T etResult = default(T);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            T csResult = default(T);
            Exception csException = null;
            try
            {
                csResult = (T)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectCastGenericWithClassRestriction<Tc>(object value) where Tc : class
        {
            Expression<Func<Tc>> e =
                Expression.Lambda<Func<Tc>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(Tc)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc> f = e.Compile();

            // compute the value with the expression tree
            Tc etResult = default(Tc);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            Tc csResult = default(Tc);
            Exception csException = null;
            try
            {
                csResult = (Tc)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectCastGenericWithStructRestriction<Ts>(object value) where Ts : struct
        {
            Expression<Func<Ts>> e =
                Expression.Lambda<Func<Ts>>(
                    Expression.Convert(Expression.Constant(value, typeof(object)), typeof(Ts)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts> f = e.Compile();

            // compute the value with the expression tree
            Ts etResult = default(Ts);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            Ts csResult = default(Ts);
            Exception csException = null;
            try
            {
                csResult = (Ts)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyGenericCastObject<T>(T value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(T)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = (object)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyGenericWithClassRestrictionCastObject<Tc>(Tc value) where Tc : class
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(Tc)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = (object)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyGenericWithStructRestrictionCastObject<Ts>(Ts value) where Ts : struct
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(Ts)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = (object)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyGenericWithStructRestrictionCastValueType<Ts>(Ts value) where Ts : struct
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.Convert(Expression.Constant(value, typeof(Ts)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile();

            // compute the value with the expression tree
            ValueType etResult = default(ValueType);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            ValueType csResult = default(ValueType);
            Exception csException = null;
            try
            {
                csResult = (ValueType)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyValueTypeCastGenericWithStructRestriction<Ts>(ValueType value) where Ts : struct
        {
            Expression<Func<Ts>> e =
                Expression.Lambda<Func<Ts>>(
                    Expression.Convert(Expression.Constant(value, typeof(ValueType)), typeof(Ts)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts> f = e.Compile();

            // compute the value with the expression tree
            Ts etResult = default(Ts);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            Ts csResult = default(Ts);
            Exception csException = null;
            try
            {
                csResult = (Ts)value;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        #endregion
    }
}
