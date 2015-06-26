// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Cast
{
    public static unsafe class IsTests
    {
        #region Test methods

        [Fact]
        public static void CheckCustomIsCustom2Test()
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomIsCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomIsInterfaceTest()
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomIsInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomIsIEquatableOfCustomTest()
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomIsIEquatableOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomIsIEquatableOfCustom2Test()
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomIsIEquatableOfCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomIsObjectTest()
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomIsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayIsCustom2ArrayTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsCustom2Array(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayIsIEnumerableOfCustomTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIEnumerableOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayIsIEnumerableOfCustom2Test()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIEnumerableOfCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayIsIEnumerableOfInterfaceTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIEnumerableOfInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayIsIEnumerableOfObjectTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIEnumerableOfObject(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayIsIListOfCustomTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIListOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayIsIListOfCustom2Test()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIListOfCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayIsIListOfInterfaceTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIListOfInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayIsIListOfObjectTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIListOfObject(array[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayIsObjectArrayTest()
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2IsCustomTest()
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2IsCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2IsInterfaceTest()
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2IsInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2IsIEquatableOfCustomTest()
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2IsIEquatableOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2IsIEquatableOfCustom2Test()
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2IsIEquatableOfCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2IsObjectTest()
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2IsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckCustom2ArrayIsCustomArrayTest()
        {
            D[][] array = new D[][] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2ArrayIsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckDelegateIsFuncOfObjectTest()
        {
            Delegate[] array = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyDelegateIsFuncOfObject(array[i]);
            }
        }

        [Fact]
        public static void CheckDelegateIsObjectTest()
        {
            Delegate[] array = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyDelegateIsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckEnumIsEnumTypeTest()
        {
            E[] array = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumIsEnumType(array[i]);
            }
        }

        [Fact]
        public static void CheckEnumIsObjectTest()
        {
            E[] array = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumIsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckEnumTypeIsEnumTest()
        {
            Enum[] array = new Enum[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumTypeIsEnum(array[i]);
            }
        }

        [Fact]
        public static void CheckEnumTypeIsObjectTest()
        {
            Enum[] array = new Enum[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumTypeIsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckFuncOfObjectIsDelegateTest()
        {
            Func<object>[] array = new Func<object>[] { null, (Func<object>)delegate () { return null; } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyFuncOfObjectIsDelegate(array[i]);
            }
        }

        [Fact]
        public static void CheckInterfaceIsCustomTest()
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceIsCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckInterfaceIsCustom2Test()
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceIsCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckInterfaceIsObjectTest()
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceIsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfCustomIsCustomArrayTest()
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomIsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfCustomIsObjectArrayTest()
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomIsObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfCustom2IsCustomArrayTest()
        {
            IEnumerable<D>[] array = new IEnumerable<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustom2IsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfInterfaceIsCustomArrayTest()
        {
            IEnumerable<I>[] array = new IEnumerable<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfInterfaceIsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfInterfaceIsObjectArrayTest()
        {
            IEnumerable<I>[] array = new IEnumerable<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfInterfaceIsObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfObjectIsCustomArrayTest()
        {
            IEnumerable<object>[] array = new IEnumerable<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfObjectIsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfObjectIsObjectArrayTest()
        {
            IEnumerable<object>[] array = new IEnumerable<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfObjectIsObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEnumerableOfStructIsStructArrayTest()
        {
            IEnumerable<S>[] array = new IEnumerable<S>[] { null, new S[] { default(S), new S() }, new S[10], new List<S>(), new List<S>(new S[] { default(S), new S() }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfStructIsStructArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableOfCustomIsCustomTest()
        {
            IEquatable<C>[] array = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustomIsCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableOfCustomIsCustom2Test()
        {
            IEquatable<C>[] array = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustomIsCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableOfCustomIsObjectTest()
        {
            IEquatable<C>[] array = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustomIsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableOfCustom2IsCustomTest()
        {
            IEquatable<D>[] array = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustom2IsCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableOfCustom2IsCustom2Test()
        {
            IEquatable<D>[] array = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustom2IsCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableOfCustom2IsObjectTest()
        {
            IEquatable<D>[] array = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustom2IsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableOfStructIsStructTest()
        {
            IEquatable<S>[] array = new IEquatable<S>[] { null };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfStructIsStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfCustomIsCustomArrayTest()
        {
            IList<C>[] array = new IList<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustomIsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfCustomIsObjectArrayTest()
        {
            IList<C>[] array = new IList<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustomIsObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfCustom2IsCustomArrayTest()
        {
            IList<D>[] array = new IList<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustom2IsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfInterfaceIsCustomArrayTest()
        {
            IList<I>[] array = new IList<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfInterfaceIsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfInterfaceIsObjectArrayTest()
        {
            IList<I>[] array = new IList<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfInterfaceIsObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfObjectIsCustomArrayTest()
        {
            IList<object>[] array = new IList<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfObjectIsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfObjectIsObjectArrayTest()
        {
            IList<object>[] array = new IList<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfObjectIsObjectArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIListOfStructIsStructArrayTest()
        {
            IList<S>[] array = new IList<S>[] { null, new S[] { default(S), new S() }, new S[10], new List<S>(), new List<S>(new S[] { default(S), new S() }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfStructIsStructArray(array[i]);
            }
        }

        [Fact]
        public static void CheckIntIsObjectTest()
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIntIsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckIntIsValueTypeTest()
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIntIsValueType(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectIsCustomTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectIsCustom2Test()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectIsDelegateTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsDelegate(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectIsEnumTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsEnum(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectIsEnumTypeTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsEnumType(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectIsInterfaceTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectIsIEquatableOfCustomTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsIEquatableOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectIsIEquatableOfCustom2Test()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsIEquatableOfCustom2(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectIsIntTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsInt(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectIsStructTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectIsValueTypeTest()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsValueType(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayIsCustomArrayTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayIsCustomArray(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayIsIEnumerableOfCustomTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayIsIEnumerableOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayIsIEnumerableOfInterfaceTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayIsIEnumerableOfInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayIsIEnumerableOfObjectTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayIsIEnumerableOfObject(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayIsIListOfCustomTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayIsIListOfCustom(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayIsIListOfInterfaceTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayIsIListOfInterface(array[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayIsIListOfObjectTest()
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayIsIListOfObject(array[i]);
            }
        }

        [Fact]
        public static void CheckStructIsIEquatableOfStructTest()
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructIsIEquatableOfStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckStructIsObjectTest()
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructIsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckStructIsValueTypeTest()
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructIsValueType(array[i]);
            }
        }

        [Fact]
        public static void CheckStructArrayIsIEnumerableOfStructTest()
        {
            S[][] array = new S[][] { null, new S[] { default(S), new S() }, new S[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructArrayIsIEnumerableOfStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckStructArrayIsIListOfStructTest()
        {
            S[][] array = new S[][] { null, new S[] { default(S), new S() }, new S[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructArrayIsIListOfStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckValueTypeIsIntTest()
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeIsInt(array[i]);
            }
        }

        [Fact]
        public static void CheckValueTypeIsObjectTest()
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeIsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckValueTypeIsStructTest()
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeIsStruct(array[i]);
            }
        }

        [Fact]
        public static void ConvertObjectCastGenericAsCustom()
        {
            CheckObjectIsGenericHelper<C>();
        }

        [Fact]
        public static void ConvertObjectCastGenericAsEnum()
        {
            CheckObjectIsGenericHelper<E>();
        }

        [Fact]
        public static void ConvertObjectCastGenericAsObject()
        {
            CheckObjectIsGenericHelper<object>();
        }

        [Fact]
        public static void ConvertObjectCastGenericAsStruct()
        {
            CheckObjectIsGenericHelper<S>();
        }

        [Fact]
        public static void ConvertObjectCastGenericAsStructWithStringAndField()
        {
            CheckObjectIsGenericHelper<Scs>();
        }

        [Fact]
        public static void ConvertObjectCastGenericWithClassRestrictionAsCustom()
        {
            CheckObjectIsGenericWithClassRestrictionHelper<C>();
        }

        [Fact]
        public static void ConvertObjectCastGenericWithClassRestrictionAsObject()
        {
            CheckObjectIsGenericWithClassRestrictionHelper<object>();
        }

        [Fact]
        public static void ConvertObjectCastGenericWithStructRestrictionAsEnum()
        {
            CheckObjectIsGenericWithStructRestrictionHelper<E>();
        }

        [Fact]
        public static void ConvertObjectCastGenericWithStructRestrictionAsStruct()
        {
            CheckObjectIsGenericWithStructRestrictionHelper<S>();
        }

        [Fact]
        public static void ConvertObjectCastGenericWithStructRestrictionAsStructWithStringAndField()
        {
            CheckObjectIsGenericWithStructRestrictionHelper<Scs>();
        }

        [Fact]
        public static void ConvertGenericCastObjectAsCustom()
        {
            CheckGenericIsObjectHelper<C>();
        }

        [Fact]
        public static void ConvertGenericCastObjectAsEnum()
        {
            CheckGenericIsObjectHelper<E>();
        }

        [Fact]
        public static void ConvertGenericCastObjectAsObject()
        {
            CheckGenericIsObjectHelper<object>();
        }

        [Fact]
        public static void ConvertGenericCastObjectAsStruct()
        {
            CheckGenericIsObjectHelper<S>();
        }

        [Fact]
        public static void ConvertGenericCastObjectAsStructWithStringAndField()
        {
            CheckGenericIsObjectHelper<Scs>();
        }

        [Fact]
        public static void ConvertGenericWithClassRestrictionCastObjectAsCustom()
        {
            CheckGenericWithClassRestrictionIsObjectHelper<C>();
        }

        [Fact]
        public static void ConvertGenericWithClassRestrictionCastObjectAsObject()
        {
            CheckGenericWithClassRestrictionIsObjectHelper<object>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastObjectAsEnum()
        {
            CheckGenericWithStructRestrictionIsObjectHelper<E>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStruct()
        {
            CheckGenericWithStructRestrictionIsObjectHelper<S>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStructWithStringAndField()
        {
            CheckGenericWithStructRestrictionIsObjectHelper<Scs>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsEnum()
        {
            CheckGenericWithStructRestrictionIsValueTypeHelper<E>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStruct()
        {
            CheckGenericWithStructRestrictionIsValueTypeHelper<S>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStructWithStringAndField()
        {
            CheckGenericWithStructRestrictionIsValueTypeHelper<Scs>();
        }

        [Fact]
        public static void ConvertValueTypeCastGenericWithStructRestrictionAsEnum()
        {
            CheckValueTypeIsGenericWithStructRestrictionHelper<E>();
        }

        [Fact]
        public static void ConvertValueTypeCastGenericWithStructRestrictionAsStruct()
        {
            CheckValueTypeIsGenericWithStructRestrictionHelper<S>();
        }

        [Fact]
        public static void ConvertValueTypeCastGenericWithStructRestrictionAsStructWithStringAndField()
        {
            CheckValueTypeIsGenericWithStructRestrictionHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        private static void CheckObjectIsGenericHelper<T>()
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsGeneric<T>(array[i]);
            }
        }

        private static void CheckObjectIsGenericWithClassRestrictionHelper<Tc>() where Tc : class
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsGenericWithClassRestriction<Tc>(array[i]);
            }
        }

        private static void CheckObjectIsGenericWithStructRestrictionHelper<Ts>() where Ts : struct
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsGenericWithStructRestriction<Ts>(array[i]);
            }
        }

        private static void CheckGenericIsObjectHelper<T>()
        {
            T[] array = new T[] { default(T) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericIsObject<T>(array[i]);
            }
        }

        private static void CheckGenericWithClassRestrictionIsObjectHelper<Tc>() where Tc : class
        {
            Tc[] array = new Tc[] { null, default(Tc) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithClassRestrictionIsObject<Tc>(array[i]);
            }
        }

        private static void CheckGenericWithStructRestrictionIsObjectHelper<Ts>() where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionIsObject<Ts>(array[i]);
            }
        }

        private static void CheckGenericWithStructRestrictionIsValueTypeHelper<Ts>() where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionIsValueType<Ts>(array[i]);
            }
        }

        private static void CheckValueTypeIsGenericWithStructRestrictionHelper<Ts>() where Ts : struct
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeIsGenericWithStructRestriction<Ts>(array[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyCustomIsCustom2(C value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is D;
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

        private static void VerifyCustomIsInterface(C value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C)), typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is I;
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

        private static void VerifyCustomIsIEquatableOfCustom(C value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C)), typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IEquatable<C>;
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

        private static void VerifyCustomIsIEquatableOfCustom2(C value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C)), typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IEquatable<D>;
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

        private static void VerifyCustomIsObject(C value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object;
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

        private static void VerifyCustomArrayIsCustom2Array(C[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(D[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is D[];
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

        private static void VerifyCustomArrayIsIEnumerableOfCustom(C[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IEnumerable<C>;
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

        private static void VerifyCustomArrayIsIEnumerableOfCustom2(C[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IEnumerable<D>;
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

        private static void VerifyCustomArrayIsIEnumerableOfInterface(C[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IEnumerable<I>;
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

        private static void VerifyCustomArrayIsIEnumerableOfObject(C[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IEnumerable<object>;
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

        private static void VerifyCustomArrayIsIListOfCustom(C[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IList<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IList<C>;
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

        private static void VerifyCustomArrayIsIListOfCustom2(C[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IList<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IList<D>;
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

        private static void VerifyCustomArrayIsIListOfInterface(C[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IList<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IList<I>;
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

        private static void VerifyCustomArrayIsIListOfObject(C[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IList<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IList<object>;
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

        private static void VerifyCustomArrayIsObjectArray(C[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object[];
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

        private static void VerifyCustom2IsCustom(D value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(D)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is C;
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

        private static void VerifyCustom2IsInterface(D value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(D)), typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is I;
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

        private static void VerifyCustom2IsIEquatableOfCustom(D value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(D)), typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IEquatable<C>;
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

        private static void VerifyCustom2IsIEquatableOfCustom2(D value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(D)), typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IEquatable<D>;
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

        private static void VerifyCustom2IsObject(D value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(D)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object;
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

        private static void VerifyCustom2ArrayIsCustomArray(D[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(D[])), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is C[];
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

        private static void VerifyDelegateIsFuncOfObject(Delegate value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Delegate)), typeof(Func<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is Func<object>;
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

        private static void VerifyDelegateIsObject(Delegate value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Delegate)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object;
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

        private static void VerifyEnumIsEnumType(E value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(E)), typeof(Enum)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();
            Assert.True(f());
        }

        private static void VerifyEnumIsObject(E value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(E)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();
            Assert.True(f());
        }

        private static void VerifyEnumTypeIsEnum(Enum value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Enum)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is E;
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

        private static void VerifyEnumTypeIsObject(Enum value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Enum)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object;
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

        private static void VerifyFuncOfObjectIsDelegate(Func<object> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Func<object>)), typeof(Delegate)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is Delegate;
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

        private static void VerifyInterfaceIsCustom(I value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(I)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is C;
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

        private static void VerifyInterfaceIsCustom2(I value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(I)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is D;
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

        private static void VerifyInterfaceIsObject(I value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(I)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object;
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

        private static void VerifyIEnumerableOfCustomIsCustomArray(IEnumerable<C> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is C[];
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

        private static void VerifyIEnumerableOfCustomIsObjectArray(IEnumerable<C> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object[];
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

        private static void VerifyIEnumerableOfCustom2IsCustomArray(IEnumerable<D> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<D>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is C[];
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

        private static void VerifyIEnumerableOfInterfaceIsCustomArray(IEnumerable<I> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<I>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is C[];
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

        private static void VerifyIEnumerableOfInterfaceIsObjectArray(IEnumerable<I> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<I>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object[];
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

        private static void VerifyIEnumerableOfObjectIsCustomArray(IEnumerable<object> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<object>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is C[];
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

        private static void VerifyIEnumerableOfObjectIsObjectArray(IEnumerable<object> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<object>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object[];
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

        private static void VerifyIEnumerableOfStructIsStructArray(IEnumerable<S> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<S>)), typeof(S[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is S[];
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

        private static void VerifyIEquatableOfCustomIsCustom(IEquatable<C> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEquatable<C>)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is C;
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

        private static void VerifyIEquatableOfCustomIsCustom2(IEquatable<C> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEquatable<C>)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is D;
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

        private static void VerifyIEquatableOfCustomIsObject(IEquatable<C> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEquatable<C>)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object;
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

        private static void VerifyIEquatableOfCustom2IsCustom(IEquatable<D> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEquatable<D>)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is C;
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

        private static void VerifyIEquatableOfCustom2IsCustom2(IEquatable<D> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEquatable<D>)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is D;
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

        private static void VerifyIEquatableOfCustom2IsObject(IEquatable<D> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEquatable<D>)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object;
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

        private static void VerifyIEquatableOfStructIsStruct(IEquatable<S> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEquatable<S>)), typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is S;
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

        private static void VerifyIListOfCustomIsCustomArray(IList<C> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<C>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is C[];
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

        private static void VerifyIListOfCustomIsObjectArray(IList<C> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<C>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object[];
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

        private static void VerifyIListOfCustom2IsCustomArray(IList<D> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<D>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is C[];
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

        private static void VerifyIListOfInterfaceIsCustomArray(IList<I> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<I>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is C[];
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

        private static void VerifyIListOfInterfaceIsObjectArray(IList<I> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<I>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object[];
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

        private static void VerifyIListOfObjectIsCustomArray(IList<object> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<object>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is C[];
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

        private static void VerifyIListOfObjectIsObjectArray(IList<object> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<object>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object[];
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

        private static void VerifyIListOfStructIsStructArray(IList<S> value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<S>)), typeof(S[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is S[];
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

        private static void VerifyIntIsObject(int value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(int)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();
            Assert.True(f());
        }

        private static void VerifyIntIsValueType(int value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(int)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();
            Assert.True(f());
        }

        private static void VerifyObjectIsCustom(object value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is C;
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

        private static void VerifyObjectIsCustom2(object value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is D;
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

        private static void VerifyObjectIsDelegate(object value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(Delegate)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is Delegate;
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

        private static void VerifyObjectIsEnum(object value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is E;
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

        private static void VerifyObjectIsEnumType(object value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(Enum)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is Enum;
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

        private static void VerifyObjectIsInterface(object value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is I;
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

        private static void VerifyObjectIsIEquatableOfCustom(object value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IEquatable<C>;
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

        private static void VerifyObjectIsIEquatableOfCustom2(object value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IEquatable<D>;
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

        private static void VerifyObjectIsInt(object value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is int;
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

        private static void VerifyObjectIsStruct(object value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is S;
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

        private static void VerifyObjectIsValueType(object value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is ValueType;
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

        private static void VerifyObjectArrayIsCustomArray(object[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object[])), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is C[];
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

        private static void VerifyObjectArrayIsIEnumerableOfCustom(object[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IEnumerable<C>;
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

        private static void VerifyObjectArrayIsIEnumerableOfInterface(object[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IEnumerable<I>;
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

        private static void VerifyObjectArrayIsIEnumerableOfObject(object[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IEnumerable<object>;
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

        private static void VerifyObjectArrayIsIListOfCustom(object[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object[])), typeof(IList<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IList<C>;
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

        private static void VerifyObjectArrayIsIListOfInterface(object[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object[])), typeof(IList<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IList<I>;
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

        private static void VerifyObjectArrayIsIListOfObject(object[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object[])), typeof(IList<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IList<object>;
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

        private static void VerifyStructIsIEquatableOfStruct(S value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(S)), typeof(IEquatable<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();
            Assert.True(f());
        }

        private static void VerifyStructIsObject(S value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(S)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();
            Assert.True(f());
        }

        private static void VerifyStructIsValueType(S value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(S)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();
            Assert.True(f());
        }

        private static void VerifyStructArrayIsIEnumerableOfStruct(S[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(S[])), typeof(IEnumerable<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IEnumerable<S>;
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

        private static void VerifyStructArrayIsIListOfStruct(S[] value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(S[])), typeof(IList<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IList<S>;
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

        private static void VerifyValueTypeIsInt(ValueType value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(ValueType)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is int;
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

        private static void VerifyValueTypeIsObject(ValueType value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(ValueType)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object;
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

        private static void VerifyValueTypeIsStruct(ValueType value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(ValueType)), typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is S;
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

        private static void VerifyObjectIsGeneric<T>(object value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(T)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is T;
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

        private static void VerifyObjectIsGenericWithClassRestriction<Tc>(object value) where Tc : class
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(Tc)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is Tc;
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

        private static void VerifyObjectIsGenericWithStructRestriction<Ts>(object value) where Ts : struct
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(Ts)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is Ts;
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

        private static void VerifyGenericIsObject<T>(T value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(T)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object;
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

        private static void VerifyGenericWithClassRestrictionIsObject<Tc>(Tc value) where Tc : class
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Tc)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object;
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

        private static void VerifyGenericWithStructRestrictionIsObject<Ts>(Ts value) where Ts : struct
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Ts)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();
            Assert.True(f());
        }

        private static void VerifyGenericWithStructRestrictionIsValueType<Ts>(Ts value) where Ts : struct
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Ts)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();
            Assert.True(f());
        }

        private static void VerifyValueTypeIsGenericWithStructRestriction<Ts>(ValueType value) where Ts : struct
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(ValueType)), typeof(Ts)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is Ts;
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
