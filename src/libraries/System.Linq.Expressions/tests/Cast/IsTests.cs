// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class IsTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomIsCustom2Test(bool useInterpreter)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomIsCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomIsInterfaceTest(bool useInterpreter)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomIsInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomIsIEquatableOfCustomTest(bool useInterpreter)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomIsIEquatableOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomIsIEquatableOfCustom2Test(bool useInterpreter)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomIsIEquatableOfCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomIsObjectTest(bool useInterpreter)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomIsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayIsCustom2ArrayTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsCustom2Array(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayIsIEnumerableOfCustomTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIEnumerableOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayIsIEnumerableOfCustom2Test(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIEnumerableOfCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayIsIEnumerableOfInterfaceTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIEnumerableOfInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayIsIEnumerableOfObjectTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIEnumerableOfObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayIsIListOfCustomTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIListOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayIsIListOfCustom2Test(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIListOfCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayIsIListOfInterfaceTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIListOfInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayIsIListOfObjectTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsIListOfObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayIsObjectArrayTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayIsObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2IsCustomTest(bool useInterpreter)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2IsCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2IsInterfaceTest(bool useInterpreter)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2IsInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2IsIEquatableOfCustomTest(bool useInterpreter)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2IsIEquatableOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2IsIEquatableOfCustom2Test(bool useInterpreter)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2IsIEquatableOfCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2IsObjectTest(bool useInterpreter)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2IsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayIsCustomArrayTest(bool useInterpreter)
        {
            D[][] array = new D[][] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2ArrayIsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateIsFuncOfObjectTest(bool useInterpreter)
        {
            Delegate[] array = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyDelegateIsFuncOfObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateIsObjectTest(bool useInterpreter)
        {
            Delegate[] array = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyDelegateIsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumIsEnumTypeTest(bool useInterpreter)
        {
            E[] array = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumIsEnumType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumIsObjectTest(bool useInterpreter)
        {
            E[] array = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumIsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumTypeIsEnumTest(bool useInterpreter)
        {
            Enum[] array = new Enum[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumTypeIsEnum(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumTypeIsObjectTest(bool useInterpreter)
        {
            Enum[] array = new Enum[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumTypeIsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncOfObjectIsDelegateTest(bool useInterpreter)
        {
            Func<object>[] array = new Func<object>[] { null, (Func<object>)delegate () { return null; } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyFuncOfObjectIsDelegate(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceIsCustomTest(bool useInterpreter)
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceIsCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceIsCustom2Test(bool useInterpreter)
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceIsCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceIsObjectTest(bool useInterpreter)
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceIsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfCustomIsCustomArrayTest(bool useInterpreter)
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomIsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfCustomIsObjectArrayTest(bool useInterpreter)
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomIsObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfCustom2IsCustomArrayTest(bool useInterpreter)
        {
            IEnumerable<D>[] array = new IEnumerable<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustom2IsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfInterfaceIsCustomArrayTest(bool useInterpreter)
        {
            IEnumerable<I>[] array = new IEnumerable<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfInterfaceIsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfInterfaceIsObjectArrayTest(bool useInterpreter)
        {
            IEnumerable<I>[] array = new IEnumerable<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfInterfaceIsObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfObjectIsCustomArrayTest(bool useInterpreter)
        {
            IEnumerable<object>[] array = new IEnumerable<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfObjectIsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfObjectIsObjectArrayTest(bool useInterpreter)
        {
            IEnumerable<object>[] array = new IEnumerable<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfObjectIsObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfStructIsStructArrayTest(bool useInterpreter)
        {
            IEnumerable<S>[] array = new IEnumerable<S>[] { null, new S[] { default(S), new S() }, new S[10], new List<S>(), new List<S>(new S[] { default(S), new S() }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfStructIsStructArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustomIsCustomTest(bool useInterpreter)
        {
            IEquatable<C>[] array = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustomIsCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustomIsCustom2Test(bool useInterpreter)
        {
            IEquatable<C>[] array = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustomIsCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustomIsObjectTest(bool useInterpreter)
        {
            IEquatable<C>[] array = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustomIsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustom2IsCustomTest(bool useInterpreter)
        {
            IEquatable<D>[] array = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustom2IsCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustom2IsCustom2Test(bool useInterpreter)
        {
            IEquatable<D>[] array = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustom2IsCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustom2IsObjectTest(bool useInterpreter)
        {
            IEquatable<D>[] array = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfCustom2IsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfStructIsStructTest(bool useInterpreter)
        {
            IEquatable<S>[] array = new IEquatable<S>[] { null };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableOfStructIsStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfCustomIsCustomArrayTest(bool useInterpreter)
        {
            IList<C>[] array = new IList<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustomIsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfCustomIsObjectArrayTest(bool useInterpreter)
        {
            IList<C>[] array = new IList<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustomIsObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfCustom2IsCustomArrayTest(bool useInterpreter)
        {
            IList<D>[] array = new IList<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustom2IsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfInterfaceIsCustomArrayTest(bool useInterpreter)
        {
            IList<I>[] array = new IList<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfInterfaceIsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfInterfaceIsObjectArrayTest(bool useInterpreter)
        {
            IList<I>[] array = new IList<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfInterfaceIsObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfObjectIsCustomArrayTest(bool useInterpreter)
        {
            IList<object>[] array = new IList<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfObjectIsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfObjectIsObjectArrayTest(bool useInterpreter)
        {
            IList<object>[] array = new IList<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfObjectIsObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfStructIsStructArrayTest(bool useInterpreter)
        {
            IList<S>[] array = new IList<S>[] { null, new S[] { default(S), new S() }, new S[10], new List<S>(), new List<S>(new S[] { default(S), new S() }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfStructIsStructArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntIsObjectTest(bool useInterpreter)
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIntIsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntIsValueTypeTest(bool useInterpreter)
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIntIsValueType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectIsCustomTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectIsCustom2Test(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectIsDelegateTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsDelegate(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectIsEnumTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsEnum(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectIsEnumTypeTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsEnumType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectIsInterfaceTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectIsIEquatableOfCustomTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsIEquatableOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectIsIEquatableOfCustom2Test(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsIEquatableOfCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectIsIntTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsInt(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectIsStructTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectIsValueTypeTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsValueType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayIsCustomArrayTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayIsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayIsIEnumerableOfCustomTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayIsIEnumerableOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayIsIEnumerableOfInterfaceTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayIsIEnumerableOfInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayIsIEnumerableOfObjectTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayIsIEnumerableOfObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayIsIListOfCustomTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayIsIListOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayIsIListOfInterfaceTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayIsIListOfInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayIsIListOfObjectTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayIsIListOfObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructIsIEquatableOfStructTest(bool useInterpreter)
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructIsIEquatableOfStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructIsObjectTest(bool useInterpreter)
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructIsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructIsValueTypeTest(bool useInterpreter)
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructIsValueType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayIsIEnumerableOfStructTest(bool useInterpreter)
        {
            S[][] array = new S[][] { null, new S[] { default(S), new S() }, new S[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructArrayIsIEnumerableOfStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayIsIListOfStructTest(bool useInterpreter)
        {
            S[][] array = new S[][] { null, new S[] { default(S), new S() }, new S[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructArrayIsIListOfStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckValueTypeIsIntTest(bool useInterpreter)
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeIsInt(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckValueTypeIsObjectTest(bool useInterpreter)
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeIsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckValueTypeIsStructTest(bool useInterpreter)
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeIsStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericAsCustom(bool useInterpreter)
        {
            CheckObjectIsGenericHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericAsEnum(bool useInterpreter)
        {
            CheckObjectIsGenericHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericAsObject(bool useInterpreter)
        {
            CheckObjectIsGenericHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericAsStruct(bool useInterpreter)
        {
            CheckObjectIsGenericHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericAsStructWithStringAndField(bool useInterpreter)
        {
            CheckObjectIsGenericHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericWithClassRestrictionAsCustom(bool useInterpreter)
        {
            CheckObjectIsGenericWithClassRestrictionHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericWithClassRestrictionAsObject(bool useInterpreter)
        {
            CheckObjectIsGenericWithClassRestrictionHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericWithStructRestrictionAsEnum(bool useInterpreter)
        {
            CheckObjectIsGenericWithStructRestrictionHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericWithStructRestrictionAsStruct(bool useInterpreter)
        {
            CheckObjectIsGenericWithStructRestrictionHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertObjectCastGenericWithStructRestrictionAsStructWithStringAndField(bool useInterpreter)
        {
            CheckObjectIsGenericWithStructRestrictionHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericCastObjectAsCustom(bool useInterpreter)
        {
            CheckGenericIsObjectHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericCastObjectAsEnum(bool useInterpreter)
        {
            CheckGenericIsObjectHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericCastObjectAsObject(bool useInterpreter)
        {
            CheckGenericIsObjectHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericCastObjectAsStruct(bool useInterpreter)
        {
            CheckGenericIsObjectHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericCastObjectAsStructWithStringAndField(bool useInterpreter)
        {
            CheckGenericIsObjectHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithClassRestrictionCastObjectAsCustom(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionIsObjectHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithClassRestrictionCastObjectAsObject(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionIsObjectHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastObjectAsEnum(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionIsObjectHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStruct(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionIsObjectHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStructWithStringAndField(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionIsObjectHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsEnum(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionIsValueTypeHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStruct(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionIsValueTypeHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStructWithStringAndField(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionIsValueTypeHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertValueTypeCastGenericWithStructRestrictionAsEnum(bool useInterpreter)
        {
            CheckValueTypeIsGenericWithStructRestrictionHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertValueTypeCastGenericWithStructRestrictionAsStruct(bool useInterpreter)
        {
            CheckValueTypeIsGenericWithStructRestrictionHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertValueTypeCastGenericWithStructRestrictionAsStructWithStringAndField(bool useInterpreter)
        {
            CheckValueTypeIsGenericWithStructRestrictionHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        private static void CheckObjectIsGenericHelper<T>(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsGeneric<T>(array[i], useInterpreter);
            }
        }

        private static void CheckObjectIsGenericWithClassRestrictionHelper<Tc>(bool useInterpreter) where Tc : class
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsGenericWithClassRestriction<Tc>(array[i], useInterpreter);
            }
        }

        private static void CheckObjectIsGenericWithStructRestrictionHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectIsGenericWithStructRestriction<Ts>(array[i], useInterpreter);
            }
        }

        private static void CheckGenericIsObjectHelper<T>(bool useInterpreter)
        {
            T[] array = new T[] { default(T) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericIsObject<T>(array[i], useInterpreter);
            }
        }

        private static void CheckGenericWithClassRestrictionIsObjectHelper<Tc>(bool useInterpreter) where Tc : class
        {
            Tc[] array = new Tc[] { null, default(Tc) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithClassRestrictionIsObject<Tc>(array[i], useInterpreter);
            }
        }

        private static void CheckGenericWithStructRestrictionIsObjectHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionIsObject<Ts>(array[i], useInterpreter);
            }
        }

        private static void CheckGenericWithStructRestrictionIsValueTypeHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionIsValueType<Ts>(array[i], useInterpreter);
            }
        }

        private static void CheckValueTypeIsGenericWithStructRestrictionHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeIsGenericWithStructRestriction<Ts>(array[i], useInterpreter);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyCustomIsCustom2(C value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is D, f());
        }

        private static void VerifyCustomIsInterface(C value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C)), typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyCustomIsIEquatableOfCustom(C value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C)), typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyCustomIsIEquatableOfCustom2(C value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C)), typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is IEquatable<D>, f());
        }

        private static void VerifyCustomIsObject(C value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyCustomArrayIsCustom2Array(C[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(D[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is D[], f());
        }

        private static void VerifyCustomArrayIsIEnumerableOfCustom(C[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyCustomArrayIsIEnumerableOfCustom2(C[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is IEnumerable<D>, f());
        }

        private static void VerifyCustomArrayIsIEnumerableOfInterface(C[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyCustomArrayIsIEnumerableOfObject(C[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyCustomArrayIsIListOfCustom(C[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IList<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyCustomArrayIsIListOfCustom2(C[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IList<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is IList<D>, f());
        }

        private static void VerifyCustomArrayIsIListOfInterface(C[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IList<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyCustomArrayIsIListOfObject(C[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(IList<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyCustomArrayIsObjectArray(C[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(C[])), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyCustom2IsCustom(D value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(D)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyCustom2IsInterface(D value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(D)), typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyCustom2IsIEquatableOfCustom(D value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(D)), typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyCustom2IsIEquatableOfCustom2(D value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(D)), typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyCustom2IsObject(D value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(D)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyCustom2ArrayIsCustomArray(D[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(D[])), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is C[], f());
        }

        private static void VerifyDelegateIsFuncOfObject(Delegate value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Delegate)), typeof(Func<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is Func<object>, f());
        }

        private static void VerifyDelegateIsObject(Delegate value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Delegate)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyEnumIsEnumType(E value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(E)), typeof(Enum)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);
            Assert.True(f());
        }

        private static void VerifyEnumIsObject(E value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(E)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);
            Assert.True(f());
        }

        private static void VerifyEnumTypeIsEnum(Enum value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Enum)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is E, f());
        }

        private static void VerifyEnumTypeIsObject(Enum value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Enum)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyFuncOfObjectIsDelegate(Func<object> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Func<object>)), typeof(Delegate)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyInterfaceIsCustom(I value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(I)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is C, f());
        }

        private static void VerifyInterfaceIsCustom2(I value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(I)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is D, f());
        }

        private static void VerifyInterfaceIsObject(I value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(I)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyIEnumerableOfCustomIsCustomArray(IEnumerable<C> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is C[], f());
        }

        private static void VerifyIEnumerableOfCustomIsObjectArray(IEnumerable<C> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is object[], f());
        }

        private static void VerifyIEnumerableOfCustom2IsCustomArray(IEnumerable<D> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<D>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is C[], f());
        }

        private static void VerifyIEnumerableOfInterfaceIsCustomArray(IEnumerable<I> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<I>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is C[], f());
        }

        private static void VerifyIEnumerableOfInterfaceIsObjectArray(IEnumerable<I> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<I>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is object[], f());
        }

        private static void VerifyIEnumerableOfObjectIsCustomArray(IEnumerable<object> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<object>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is C[], f());
        }

        private static void VerifyIEnumerableOfObjectIsObjectArray(IEnumerable<object> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<object>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is object[], f());
        }

        private static void VerifyIEnumerableOfStructIsStructArray(IEnumerable<S> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEnumerable<S>)), typeof(S[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is S[], f());
        }

        private static void VerifyIEquatableOfCustomIsCustom(IEquatable<C> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEquatable<C>)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is C, f());
        }

        private static void VerifyIEquatableOfCustomIsCustom2(IEquatable<C> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEquatable<C>)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is D, f());
        }

        private static void VerifyIEquatableOfCustomIsObject(IEquatable<C> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEquatable<C>)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyIEquatableOfCustom2IsCustom(IEquatable<D> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEquatable<D>)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is C, f());
        }

        private static void VerifyIEquatableOfCustom2IsCustom2(IEquatable<D> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEquatable<D>)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is D, f());
        }

        private static void VerifyIEquatableOfCustom2IsObject(IEquatable<D> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEquatable<D>)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyIEquatableOfStructIsStruct(IEquatable<S> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IEquatable<S>)), typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is S, f());
        }

        private static void VerifyIListOfCustomIsCustomArray(IList<C> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<C>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is C[], f());
        }

        private static void VerifyIListOfCustomIsObjectArray(IList<C> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<C>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is object[], f());
        }

        private static void VerifyIListOfCustom2IsCustomArray(IList<D> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<D>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is C[], f());
        }

        private static void VerifyIListOfInterfaceIsCustomArray(IList<I> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<I>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is C[], f());
        }

        private static void VerifyIListOfInterfaceIsObjectArray(IList<I> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<I>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is object[], f());
        }

        private static void VerifyIListOfObjectIsCustomArray(IList<object> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<object>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is C[], f());
        }

        private static void VerifyIListOfObjectIsObjectArray(IList<object> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<object>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is object[], f());
        }

        private static void VerifyIListOfStructIsStructArray(IList<S> value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(IList<S>)), typeof(S[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is S[], f());
        }

        private static void VerifyIntIsObject(int value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(int)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);
            Assert.True(f());
        }

        private static void VerifyIntIsValueType(int value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(int)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);
            Assert.True(f());
        }

        private static void VerifyObjectIsCustom(object value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is C, f());
        }

        private static void VerifyObjectIsCustom2(object value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is D, f());
        }

        private static void VerifyObjectIsDelegate(object value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(Delegate)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is Delegate, f());
        }

        private static void VerifyObjectIsEnum(object value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is E, f());
        }

        private static void VerifyObjectIsEnumType(object value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(Enum)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is Enum, f());
        }

        private static void VerifyObjectIsInterface(object value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is I, f());
        }

        private static void VerifyObjectIsIEquatableOfCustom(object value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is IEquatable<C>, f());
        }

        private static void VerifyObjectIsIEquatableOfCustom2(object value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is IEquatable<D>, f());
        }

        private static void VerifyObjectIsInt(object value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is int, f());
        }

        private static void VerifyObjectIsStruct(object value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is S, f());
        }

        private static void VerifyObjectIsValueType(object value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is ValueType, f());
        }

        private static void VerifyObjectArrayIsCustomArray(object[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object[])), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is C[], f());
        }

        private static void VerifyObjectArrayIsIEnumerableOfCustom(object[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is IEnumerable<C>, f());
        }

        private static void VerifyObjectArrayIsIEnumerableOfInterface(object[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is IEnumerable<I>, f());
        }

        private static void VerifyObjectArrayIsIEnumerableOfObject(object[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyObjectArrayIsIListOfCustom(object[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object[])), typeof(IList<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is IList<C>, f());
        }

        private static void VerifyObjectArrayIsIListOfInterface(object[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object[])), typeof(IList<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is IList<I>, f());
        }

        private static void VerifyObjectArrayIsIListOfObject(object[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object[])), typeof(IList<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyStructIsIEquatableOfStruct(S value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(S)), typeof(IEquatable<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);
            Assert.True(f());
        }

        private static void VerifyStructIsObject(S value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(S)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);
            Assert.True(f());
        }

        private static void VerifyStructIsValueType(S value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(S)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);
            Assert.True(f());
        }

        private static void VerifyStructArrayIsIEnumerableOfStruct(S[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(S[])), typeof(IEnumerable<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyStructArrayIsIListOfStruct(S[] value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(S[])), typeof(IList<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyValueTypeIsInt(ValueType value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(ValueType)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is int, f());
        }

        private static void VerifyValueTypeIsObject(ValueType value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(ValueType)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyValueTypeIsStruct(ValueType value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(ValueType)), typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is S, f());
        }

        private static void VerifyObjectIsGeneric<T>(object value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(T)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is T, f());
        }

        private static void VerifyObjectIsGenericWithClassRestriction<Tc>(object value, bool useInterpreter) where Tc : class
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(Tc)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is Tc, f());
        }

        private static void VerifyObjectIsGenericWithStructRestriction<Ts>(object value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(object)), typeof(Ts)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is Ts, f());
        }

        private static void VerifyGenericIsObject<T>(T value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(T)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyGenericWithClassRestrictionIsObject<Tc>(Tc value, bool useInterpreter) where Tc : class
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Tc)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value != null, f());
        }

        private static void VerifyGenericWithStructRestrictionIsObject<Ts>(Ts value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Ts)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);
            Assert.True(f());
        }

        private static void VerifyGenericWithStructRestrictionIsValueType<Ts>(Ts value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Ts)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);
            Assert.True(f());
        }

        private static void VerifyValueTypeIsGenericWithStructRestriction<Ts>(ValueType value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(ValueType)), typeof(Ts)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(value is Ts, f());
        }

        #endregion
    }
}
