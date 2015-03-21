// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Cast
{
    public static unsafe class AsTests
    {
        #region Test methods

        [Fact]
        public static void CheckCustomAsCustom2Test()
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomAsCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomAsInterfaceTest()
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomAsInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomAsIEquatableOfCustomTest()
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomAsIEquatableOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomAsIEquatableOfCustom2Test()
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomAsIEquatableOfCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomAsObjectTest()
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomAsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayAsCustom2ArrayTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsCustom2Array(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayAsIEnumerableOfCustomTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIEnumerableOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayAsIEnumerableOfCustom2Test()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIEnumerableOfCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayAsIEnumerableOfInterfaceTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIEnumerableOfInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayAsIEnumerableOfObjectTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIEnumerableOfObject(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayAsIListOfCustomTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIListOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayAsIListOfCustom2Test()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIListOfCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayAsIListOfInterfaceTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIListOfInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayAsIListOfObjectTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIListOfObject(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayAsObjectArrayTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2AsCustomTest()
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2AsCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2AsInterfaceTest()
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2AsInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2AsIEquatableOfCustomTest()
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2AsIEquatableOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2AsIEquatableOfCustom2Test()
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2AsIEquatableOfCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2AsObjectTest()
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2AsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2ArrayAsCustomArrayTest()
        {
            D[][] array = new D[][] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2ArrayAsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckDelegateAsFuncOfObjectTest()
        {
            Delegate[] array = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyDelegateAsFuncOfObject(array[i]);
            }
        }

        [Fact]
        public static void CheckDelegateAsObjectTest()
        {
            Delegate[] array = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyDelegateAsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckEnumAsEnumTypeTest()
        {
            E[] array = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumAsEnumType(array[i]);
            }
        }

        [Fact]
        public static void CheckEnumAsObjectTest()
        {
            E[] array = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumAsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckEnumTypeAsObjectTest()
        {
            Enum[] array = new Enum[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumTypeAsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckFuncOfObjectAsDelegateTest()
        {
            Func<object>[] array = new Func<object>[] { null, (Func<object>)delegate () { return null; } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyFuncOfObjectAsDelegate(array[i]);
            }
        }

        [Fact]
        public static void CheckInterfaceAsCustomTest()
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceAsCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckInterfaceAsCustom2Test()
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceAsCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckInterfaceAsObjectTest()
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceAsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfCustomAsCustomArrayTest()
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomAsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfCustomAsObjectArrayTest()
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomAsObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfCustomAsCustomTest()
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomAsCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfCustomAsCustom2Test()
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomAsCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfCustomAsObjectTest()
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomAsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfCustom2AsCustomArrayTest()
        {
            IEnumerable<D>[] array = new IEnumerable<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustom2AsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfCustom2AsCustomTest()
        {
            IEnumerable<D>[] array = new IEnumerable<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustom2AsCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfCustom2AsCustom2Test()
        {
            IEnumerable<D>[] array = new IEnumerable<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustom2AsCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfCustom2AsObjectTest()
        {
            IEnumerable<D>[] array = new IEnumerable<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustom2AsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfInterfaceAsCustomArrayTest()
        {
            IEnumerable<I>[] array = new IEnumerable<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfInterfaceAsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfInterfaceAsObjectArrayTest()
        {
            IEnumerable<I>[] array = new IEnumerable<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfInterfaceAsObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfObjectAsCustomArrayTest()
        {
            IEnumerable<object>[] array = new IEnumerable<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfObjectAsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfObjectAsObjectArrayTest()
        {
            IEnumerable<object>[] array = new IEnumerable<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfObjectAsObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfStructAsStructArrayTest()
        {
            IEnumerable<S>[] array = new IEnumerable<S>[] { null, new S[] { default(S), new S() }, new S[10], new List<S>(), new List<S>(new S[] { default(S), new S() }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfStructAsStructArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfCustomAsCustomArrayTest()
        {
            IList<C>[] array = new IList<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustomAsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfCustomAsObjectArrayTest()
        {
            IList<C>[] array = new IList<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustomAsObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfCustom2AsCustomArrayTest()
        {
            IList<D>[] array = new IList<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustom2AsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfInterfaceAsCustomArrayTest()
        {
            IList<I>[] array = new IList<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfInterfaceAsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfInterfaceAsObjectArrayTest()
        {
            IList<I>[] array = new IList<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfInterfaceAsObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfObjectAsCustomArrayTest()
        {
            IList<object>[] array = new IList<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfObjectAsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfObjectAsObjectArrayTest()
        {
            IList<object>[] array = new IList<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfObjectAsObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfStructAsStructArrayTest()
        {
            IList<S>[] array = new IList<S>[] { null, new S[] { default(S), new S() }, new S[10], new List<S>(), new List<S>(new S[] { default(S), new S() }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfStructAsStructArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIntAsObjectTest()
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIntAsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckIntAsValueTypeTest()
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIntAsValueType(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectAsCustomTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectAsCustom2Test()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectAsDelegateTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsDelegate(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectAsEnumTypeTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsEnumType(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectAsInterfaceTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectAsIEquatableOfCustomTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsIEquatableOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectAsIEquatableOfCustom2Test()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsIEquatableOfCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectAsValueTypeTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsValueType(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayAsCustomArrayTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayAsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayAsIEnumerableOfCustomTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayAsIEnumerableOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayAsIEnumerableOfInterfaceTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayAsIEnumerableOfInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayAsIEnumerableOfObjectTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayAsIEnumerableOfObject(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayAsIListOfCustomTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayAsIListOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayAsIListOfInterfaceTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayAsIListOfInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayAsIListOfObjectTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayAsIListOfObject(array[i]);
            }
        }

        [Fact]
        public static void CheckStructAsIEquatableOfStructTest()
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructAsIEquatableOfStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckStructAsObjectTest()
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructAsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckStructAsValueTypeTest()
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructAsValueType(array[i]);
            }
        }

        [Fact]
        public static void CheckStructArrayAsIEnumerableOfStructTest()
        {
            S[][] array = new S[][] { null, new S[] { default(S), new S() }, new S[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructArrayAsIEnumerableOfStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckStructArrayAsIListOfStructTest()
        {
            S[][] array = new S[][] { null, new S[] { default(S), new S() }, new S[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructArrayAsIListOfStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckValueTypeAsObjectTest()
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeAsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckGenericAsObjectCustomTest()
        {
            CheckGenericAsObjectHelper<C>();
        }

        [Fact]
        public static void CheckGenericAsObjectEnumTest()
        {
            CheckGenericAsObjectHelper<E>();
        }

        [Fact]
        public static void CheckGenericAsObjectObjectTest()
        {
            CheckGenericAsObjectHelper<object>();
        }

        [Fact]
        public static void CheckGenericAsObjectStructTest()
        {
            CheckGenericAsObjectHelper<S>();
        }

        [Fact]
        public static void CheckGenericAsObjectStructWithStringAndValueTest()
        {
            CheckGenericAsObjectHelper<Scs>();
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionAsCustomTest()
        {
            CheckGenericWithClassRestrictionAsObjectHelper<C>();
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionAsObjectTest()
        {
            CheckGenericWithClassRestrictionAsObjectHelper<object>();
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionAsEnumTest()
        {
            CheckGenericWithStructRestrictionAsObjectHelper<E>();
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionAsStructTest()
        {
            CheckGenericWithStructRestrictionAsObjectHelper<S>();
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionAsStructWithStringAndValueTest()
        {
            CheckGenericWithStructRestrictionAsObjectHelper<Scs>();
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionAsValueTypeAsEnumTest()
        {
            CheckGenericWithStructRestrictionAsValueTypeHelper<E>();
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionAsValueTypeAsStructTest()
        {
            CheckGenericWithStructRestrictionAsValueTypeHelper<S>();
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionAsValueTypeAsStructWithStringAndValueTest()
        {
            CheckGenericWithStructRestrictionAsValueTypeHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        private static void CheckGenericAsObjectHelper<T>()
        {
            T[] array = new T[] { default(T) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericAsObject<T>(array[i]);
            }
        }

        private static void CheckGenericWithClassRestrictionAsObjectHelper<Tc>() where Tc : class
        {
            Tc[] array = new Tc[] { null, default(Tc) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithClassRestrictionAsObject<Tc>(array[i]);
            }
        }

        private static void CheckGenericWithStructRestrictionAsObjectHelper<Ts>() where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionAsObject<Ts>(array[i]);
            }
        }

        private static void CheckGenericWithStructRestrictionAsValueTypeHelper<Ts>() where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionAsValueType<Ts>(array[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyCustomAsCustom2(C value)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C)), typeof(D)),
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
                csResult = value as D;
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

        private static void VerifyCustomAsInterface(C value)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C)), typeof(I)),
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
                csResult = value as I;
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

        private static void VerifyCustomAsIEquatableOfCustom(C value)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C)), typeof(IEquatable<C>)),
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
                csResult = value as IEquatable<C>;
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

        private static void VerifyCustomAsIEquatableOfCustom2(C value)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C)), typeof(IEquatable<D>)),
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
                csResult = value as IEquatable<D>;
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

        private static void VerifyCustomAsObject(C value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C)), typeof(object)),
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
                csResult = value as object;
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

        private static void VerifyCustomArrayAsCustom2Array(C[] value)
        {
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(D[])),
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
                csResult = value as D[];
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

        private static void VerifyCustomArrayAsIEnumerableOfCustom(C[] value)
        {
            Expression<Func<IEnumerable<C>>> e =
                Expression.Lambda<Func<IEnumerable<C>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<C>)),
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
                csResult = value as IEnumerable<C>;
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

        private static void VerifyCustomArrayAsIEnumerableOfCustom2(C[] value)
        {
            Expression<Func<IEnumerable<D>>> e =
                Expression.Lambda<Func<IEnumerable<D>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<D>)),
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
                csResult = value as IEnumerable<D>;
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

        private static void VerifyCustomArrayAsIEnumerableOfInterface(C[] value)
        {
            Expression<Func<IEnumerable<I>>> e =
                Expression.Lambda<Func<IEnumerable<I>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<I>)),
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
                csResult = value as IEnumerable<I>;
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

        private static void VerifyCustomArrayAsIEnumerableOfObject(C[] value)
        {
            Expression<Func<IEnumerable<object>>> e =
                Expression.Lambda<Func<IEnumerable<object>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<object>)),
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
                csResult = value as IEnumerable<object>;
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

        private static void VerifyCustomArrayAsIListOfCustom(C[] value)
        {
            Expression<Func<IList<C>>> e =
                Expression.Lambda<Func<IList<C>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IList<C>)),
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
                csResult = value as IList<C>;
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

        private static void VerifyCustomArrayAsIListOfCustom2(C[] value)
        {
            Expression<Func<IList<D>>> e =
                Expression.Lambda<Func<IList<D>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IList<D>)),
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
                csResult = value as IList<D>;
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

        private static void VerifyCustomArrayAsIListOfInterface(C[] value)
        {
            Expression<Func<IList<I>>> e =
                Expression.Lambda<Func<IList<I>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IList<I>)),
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
                csResult = value as IList<I>;
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

        private static void VerifyCustomArrayAsIListOfObject(C[] value)
        {
            Expression<Func<IList<object>>> e =
                Expression.Lambda<Func<IList<object>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IList<object>)),
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
                csResult = value as IList<object>;
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

        private static void VerifyCustomArrayAsObjectArray(C[] value)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(object[])),
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
                csResult = value as object[];
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

        private static void VerifyCustom2AsCustom(D value)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(D)), typeof(C)),
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
                csResult = value as C;
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

        private static void VerifyCustom2AsInterface(D value)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(D)), typeof(I)),
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
                csResult = value as I;
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

        private static void VerifyCustom2AsIEquatableOfCustom(D value)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(D)), typeof(IEquatable<C>)),
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
                csResult = value as IEquatable<C>;
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

        private static void VerifyCustom2AsIEquatableOfCustom2(D value)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(D)), typeof(IEquatable<D>)),
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
                csResult = value as IEquatable<D>;
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

        private static void VerifyCustom2AsObject(D value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(D)), typeof(object)),
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
                csResult = value as object;
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

        private static void VerifyCustom2ArrayAsCustomArray(D[] value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(D[])), typeof(C[])),
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
                csResult = value as C[];
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

        private static void VerifyDelegateAsFuncOfObject(Delegate value)
        {
            Expression<Func<Func<object>>> e =
                Expression.Lambda<Func<Func<object>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Delegate)), typeof(Func<object>)),
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
                csResult = value as Func<object>;
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

        private static void VerifyDelegateAsObject(Delegate value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Delegate)), typeof(object)),
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
                csResult = value as object;
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

        private static void VerifyEnumAsEnumType(E value)
        {
            Expression<Func<Enum>> e =
                Expression.Lambda<Func<Enum>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(E)), typeof(Enum)),
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
                csResult = value as Enum;
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

        private static void VerifyEnumAsObject(E value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(E)), typeof(object)),
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
                csResult = value as object;
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

        private static void VerifyEnumTypeAsObject(Enum value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Enum)), typeof(object)),
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
                csResult = value as object;
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

        private static void VerifyFuncOfObjectAsDelegate(Func<object> value)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Func<object>)), typeof(Delegate)),
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
                csResult = value as Delegate;
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

        private static void VerifyInterfaceAsCustom(I value)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(I)), typeof(C)),
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
                csResult = value as C;
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

        private static void VerifyInterfaceAsCustom2(I value)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(I)), typeof(D)),
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
                csResult = value as D;
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

        private static void VerifyInterfaceAsObject(I value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(I)), typeof(object)),
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
                csResult = value as object;
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

        private static void VerifyIEnumerableOfCustomAsCustomArray(IEnumerable<C> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(C[])),
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
                csResult = value as C[];
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

        private static void VerifyIEnumerableOfCustomAsObjectArray(IEnumerable<C> value)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(object[])),
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
                csResult = value as object[];
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

        private static void VerifyIEnumerableOfCustomAsCustom(IEnumerable<C> value)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(C)),
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
                csResult = value as C;
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

        private static void VerifyIEnumerableOfCustomAsCustom2(IEnumerable<C> value)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(D)),
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
                csResult = value as D;
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

        private static void VerifyIEnumerableOfCustomAsObject(IEnumerable<C> value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(object)),
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
                csResult = value as object;
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

        private static void VerifyIEnumerableOfCustom2AsCustomArray(IEnumerable<D> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<D>)), typeof(C[])),
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
                csResult = value as C[];
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

        private static void VerifyIEnumerableOfCustom2AsCustom(IEnumerable<D> value)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<D>)), typeof(C)),
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
                csResult = value as C;
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

        private static void VerifyIEnumerableOfCustom2AsCustom2(IEnumerable<D> value)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<D>)), typeof(D)),
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
                csResult = value as D;
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

        private static void VerifyIEnumerableOfCustom2AsObject(IEnumerable<D> value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<D>)), typeof(object)),
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
                csResult = value as object;
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

        private static void VerifyIEnumerableOfInterfaceAsCustomArray(IEnumerable<I> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<I>)), typeof(C[])),
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
                csResult = value as C[];
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

        private static void VerifyIEnumerableOfInterfaceAsObjectArray(IEnumerable<I> value)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<I>)), typeof(object[])),
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
                csResult = value as object[];
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

        private static void VerifyIEnumerableOfObjectAsCustomArray(IEnumerable<object> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<object>)), typeof(C[])),
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
                csResult = value as C[];
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

        private static void VerifyIEnumerableOfObjectAsObjectArray(IEnumerable<object> value)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<object>)), typeof(object[])),
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
                csResult = value as object[];
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

        private static void VerifyIEnumerableOfStructAsStructArray(IEnumerable<S> value)
        {
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<S>)), typeof(S[])),
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
                csResult = value as S[];
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

        private static void VerifyIListOfCustomAsCustomArray(IList<C> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<C>)), typeof(C[])),
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
                csResult = value as C[];
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

        private static void VerifyIListOfCustomAsObjectArray(IList<C> value)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<C>)), typeof(object[])),
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
                csResult = value as object[];
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

        private static void VerifyIListOfCustom2AsCustomArray(IList<D> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<D>)), typeof(C[])),
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
                csResult = value as C[];
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

        private static void VerifyIListOfInterfaceAsCustomArray(IList<I> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<I>)), typeof(C[])),
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
                csResult = value as C[];
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

        private static void VerifyIListOfInterfaceAsObjectArray(IList<I> value)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<I>)), typeof(object[])),
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
                csResult = value as object[];
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

        private static void VerifyIListOfObjectAsCustomArray(IList<object> value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<object>)), typeof(C[])),
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
                csResult = value as C[];
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

        private static void VerifyIListOfObjectAsObjectArray(IList<object> value)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<object>)), typeof(object[])),
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
                csResult = value as object[];
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

        private static void VerifyIListOfStructAsStructArray(IList<S> value)
        {
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<S>)), typeof(S[])),
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
                csResult = value as S[];
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

        private static void VerifyIntAsObject(int value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(int)), typeof(object)),
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
                csResult = value as object;
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

        private static void VerifyIntAsValueType(int value)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(int)), typeof(ValueType)),
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
                csResult = value as ValueType;
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

        private static void VerifyObjectAsCustom(object value)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(C)),
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
                csResult = value as C;
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

        private static void VerifyObjectAsCustom2(object value)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(D)),
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
                csResult = value as D;
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

        private static void VerifyObjectAsDelegate(object value)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(Delegate)),
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
                csResult = value as Delegate;
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

        private static void VerifyObjectAsEnumType(object value)
        {
            Expression<Func<Enum>> e =
                Expression.Lambda<Func<Enum>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(Enum)),
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
                csResult = value as Enum;
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

        private static void VerifyObjectAsInterface(object value)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(I)),
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
                csResult = value as I;
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

        private static void VerifyObjectAsIEquatableOfCustom(object value)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(IEquatable<C>)),
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
                csResult = value as IEquatable<C>;
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

        private static void VerifyObjectAsIEquatableOfCustom2(object value)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(IEquatable<D>)),
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
                csResult = value as IEquatable<D>;
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

        private static void VerifyObjectAsValueType(object value)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(ValueType)),
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
                csResult = value as ValueType;
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

        private static void VerifyObjectArrayAsCustomArray(object[] value)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object[])), typeof(C[])),
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
                csResult = value as C[];
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

        private static void VerifyObjectArrayAsIEnumerableOfCustom(object[] value)
        {
            Expression<Func<IEnumerable<C>>> e =
                Expression.Lambda<Func<IEnumerable<C>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<C>)),
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
                csResult = value as IEnumerable<C>;
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

        private static void VerifyObjectArrayAsIEnumerableOfInterface(object[] value)
        {
            Expression<Func<IEnumerable<I>>> e =
                Expression.Lambda<Func<IEnumerable<I>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<I>)),
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
                csResult = value as IEnumerable<I>;
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

        private static void VerifyObjectArrayAsIEnumerableOfObject(object[] value)
        {
            Expression<Func<IEnumerable<object>>> e =
                Expression.Lambda<Func<IEnumerable<object>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<object>)),
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
                csResult = value as IEnumerable<object>;
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

        private static void VerifyObjectArrayAsIListOfCustom(object[] value)
        {
            Expression<Func<IList<C>>> e =
                Expression.Lambda<Func<IList<C>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object[])), typeof(IList<C>)),
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
                csResult = value as IList<C>;
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

        private static void VerifyObjectArrayAsIListOfInterface(object[] value)
        {
            Expression<Func<IList<I>>> e =
                Expression.Lambda<Func<IList<I>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object[])), typeof(IList<I>)),
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
                csResult = value as IList<I>;
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

        private static void VerifyObjectArrayAsIListOfObject(object[] value)
        {
            Expression<Func<IList<object>>> e =
                Expression.Lambda<Func<IList<object>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object[])), typeof(IList<object>)),
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
                csResult = value as IList<object>;
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

        private static void VerifyStructAsIEquatableOfStruct(S value)
        {
            Expression<Func<IEquatable<S>>> e =
                Expression.Lambda<Func<IEquatable<S>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S)), typeof(IEquatable<S>)),
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
                csResult = value as IEquatable<S>;
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

        private static void VerifyStructAsObject(S value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S)), typeof(object)),
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
                csResult = value as object;
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

        private static void VerifyStructAsValueType(S value)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S)), typeof(ValueType)),
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
                csResult = value as ValueType;
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

        private static void VerifyStructArrayAsIEnumerableOfStruct(S[] value)
        {
            Expression<Func<IEnumerable<S>>> e =
                Expression.Lambda<Func<IEnumerable<S>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S[])), typeof(IEnumerable<S>)),
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
                csResult = value as IEnumerable<S>;
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

        private static void VerifyStructArrayAsIListOfStruct(S[] value)
        {
            Expression<Func<IList<S>>> e =
                Expression.Lambda<Func<IList<S>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S[])), typeof(IList<S>)),
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
                csResult = value as IList<S>;
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

        private static void VerifyValueTypeAsObject(ValueType value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(ValueType)), typeof(object)),
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
                csResult = value as object;
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

        private static void VerifyGenericAsObject<T>(T value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(T)), typeof(object)),
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
                csResult = value as object;
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

        private static void VerifyGenericWithClassRestrictionAsObject<Tc>(Tc value) where Tc : class
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Tc)), typeof(object)),
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
                csResult = value as object;
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

        private static void VerifyGenericWithStructRestrictionAsObject<Ts>(Ts value) where Ts : struct
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Ts)), typeof(object)),
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
                csResult = value as object;
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

        private static void VerifyGenericWithStructRestrictionAsValueType<Ts>(Ts value) where Ts : struct
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Ts)), typeof(ValueType)),
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
                csResult = value as ValueType;
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
