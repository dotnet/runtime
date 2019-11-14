// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class AsTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomAsCustom2Test(bool useInterpreter)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomAsCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomAsInterfaceTest(bool useInterpreter)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomAsInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomAsIEquatableOfCustomTest(bool useInterpreter)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomAsIEquatableOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomAsIEquatableOfCustom2Test(bool useInterpreter)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomAsIEquatableOfCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomAsObjectTest(bool useInterpreter)
        {
            C[] array = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomAsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayAsCustom2ArrayTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsCustom2Array(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayAsIEnumerableOfCustomTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIEnumerableOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayAsIEnumerableOfCustom2Test(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIEnumerableOfCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayAsIEnumerableOfInterfaceTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIEnumerableOfInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayAsIEnumerableOfObjectTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIEnumerableOfObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayAsIListOfCustomTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIListOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayAsIListOfCustom2Test(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIListOfCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayAsIListOfInterfaceTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIListOfInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayAsIListOfObjectTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsIListOfObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayAsObjectArrayTest(bool useInterpreter)
        {
            C[][] array = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayAsObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2AsCustomTest(bool useInterpreter)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2AsCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2AsInterfaceTest(bool useInterpreter)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2AsInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2AsIEquatableOfCustomTest(bool useInterpreter)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2AsIEquatableOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2AsIEquatableOfCustom2Test(bool useInterpreter)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2AsIEquatableOfCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2AsObjectTest(bool useInterpreter)
        {
            D[] array = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2AsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayAsCustomArrayTest(bool useInterpreter)
        {
            D[][] array = new D[][] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2ArrayAsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateAsFuncOfObjectTest(bool useInterpreter)
        {
            Delegate[] array = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyDelegateAsFuncOfObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateAsObjectTest(bool useInterpreter)
        {
            Delegate[] array = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyDelegateAsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumAsEnumTypeTest(bool useInterpreter)
        {
            E[] array = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumAsEnumType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumAsObjectTest(bool useInterpreter)
        {
            E[] array = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumAsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumTypeAsObjectTest(bool useInterpreter)
        {
            Enum[] array = new Enum[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumTypeAsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncOfObjectAsDelegateTest(bool useInterpreter)
        {
            Func<object>[] array = new Func<object>[] { null, (Func<object>)delegate () { return null; } };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyFuncOfObjectAsDelegate(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceAsCustomTest(bool useInterpreter)
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceAsCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceAsCustom2Test(bool useInterpreter)
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceAsCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceAsObjectTest(bool useInterpreter)
        {
            I[] array = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceAsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfCustomAsCustomArrayTest(bool useInterpreter)
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomAsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfCustomAsObjectArrayTest(bool useInterpreter)
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomAsObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfCustomAsCustomTest(bool useInterpreter)
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomAsCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfCustomAsCustom2Test(bool useInterpreter)
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomAsCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfCustomAsObjectTest(bool useInterpreter)
        {
            IEnumerable<C>[] array = new IEnumerable<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustomAsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfCustom2AsCustomArrayTest(bool useInterpreter)
        {
            IEnumerable<D>[] array = new IEnumerable<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustom2AsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfCustom2AsCustomTest(bool useInterpreter)
        {
            IEnumerable<D>[] array = new IEnumerable<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustom2AsCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfCustom2AsCustom2Test(bool useInterpreter)
        {
            IEnumerable<D>[] array = new IEnumerable<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustom2AsCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfCustom2AsObjectTest(bool useInterpreter)
        {
            IEnumerable<D>[] array = new IEnumerable<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfCustom2AsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfInterfaceAsCustomArrayTest(bool useInterpreter)
        {
            IEnumerable<I>[] array = new IEnumerable<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfInterfaceAsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfInterfaceAsObjectArrayTest(bool useInterpreter)
        {
            IEnumerable<I>[] array = new IEnumerable<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfInterfaceAsObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfObjectAsCustomArrayTest(bool useInterpreter)
        {
            IEnumerable<object>[] array = new IEnumerable<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfObjectAsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfObjectAsObjectArrayTest(bool useInterpreter)
        {
            IEnumerable<object>[] array = new IEnumerable<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfObjectAsObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEnumerableOfStructAsStructArrayTest(bool useInterpreter)
        {
            IEnumerable<S>[] array = new IEnumerable<S>[] { null, new S[] { default(S), new S() }, new S[10], new List<S>(), new List<S>(new S[] { default(S), new S() }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEnumerableOfStructAsStructArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfCustomAsCustomArrayTest(bool useInterpreter)
        {
            IList<C>[] array = new IList<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustomAsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfCustomAsObjectArrayTest(bool useInterpreter)
        {
            IList<C>[] array = new IList<C>[] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10], new List<C>(), new List<C>(new C[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustomAsObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfCustom2AsCustomArrayTest(bool useInterpreter)
        {
            IList<D>[] array = new IList<D>[] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10], new List<D>(), new List<D>(new D[] { null, new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfCustom2AsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfInterfaceAsCustomArrayTest(bool useInterpreter)
        {
            IList<I>[] array = new IList<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfInterfaceAsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfInterfaceAsObjectArrayTest(bool useInterpreter)
        {
            IList<I>[] array = new IList<I>[] { null, new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[10], new List<I>(), new List<I>(new I[] { null, new C(), new D(), new D(0), new D(5) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfInterfaceAsObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfObjectAsCustomArrayTest(bool useInterpreter)
        {
            IList<object>[] array = new IList<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfObjectAsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfObjectAsObjectArrayTest(bool useInterpreter)
        {
            IList<object>[] array = new IList<object>[] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10], new List<object>(), new List<object>(new object[] { null, new object(), new C(), new D(3) }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfObjectAsObjectArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIListOfStructAsStructArrayTest(bool useInterpreter)
        {
            IList<S>[] array = new IList<S>[] { null, new S[] { default(S), new S() }, new S[10], new List<S>(), new List<S>(new S[] { default(S), new S() }) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIListOfStructAsStructArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntAsObjectTest(bool useInterpreter)
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIntAsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntAsValueTypeTest(bool useInterpreter)
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIntAsValueType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectAsCustomTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectAsCustom2Test(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectAsDelegateTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsDelegate(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectAsEnumTypeTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsEnumType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectAsInterfaceTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectAsIEquatableOfCustomTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsIEquatableOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectAsIEquatableOfCustom2Test(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsIEquatableOfCustom2(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectAsValueTypeTest(bool useInterpreter)
        {
            object[] array = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectAsValueType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayAsCustomArrayTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayAsCustomArray(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayAsIEnumerableOfCustomTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayAsIEnumerableOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayAsIEnumerableOfInterfaceTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayAsIEnumerableOfInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayAsIEnumerableOfObjectTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayAsIEnumerableOfObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayAsIListOfCustomTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayAsIListOfCustom(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayAsIListOfInterfaceTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayAsIListOfInterface(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayAsIListOfObjectTest(bool useInterpreter)
        {
            object[][] array = new object[][] { null, new object[] { null, new object(), new C(), new D(3) }, new object[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayAsIListOfObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructAsIEquatableOfStructTest(bool useInterpreter)
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructAsIEquatableOfStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructAsObjectTest(bool useInterpreter)
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructAsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructAsValueTypeTest(bool useInterpreter)
        {
            S[] array = new S[] { default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructAsValueType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayAsIEnumerableOfStructTest(bool useInterpreter)
        {
            S[][] array = new S[][] { null, new S[] { default(S), new S() }, new S[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructArrayAsIEnumerableOfStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayAsIListOfStructTest(bool useInterpreter)
        {
            S[][] array = new S[][] { null, new S[] { default(S), new S() }, new S[10] };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructArrayAsIListOfStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckValueTypeAsObjectTest(bool useInterpreter)
        {
            ValueType[] array = new ValueType[] { null, default(S), new Scs(null, new S()), E.A, El.B };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyValueTypeAsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericAsObjectCustomTest(bool useInterpreter)
        {
            CheckGenericAsObjectHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericAsObjectEnumTest(bool useInterpreter)
        {
            CheckGenericAsObjectHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericAsObjectObjectTest(bool useInterpreter)
        {
            CheckGenericAsObjectHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericAsObjectStructTest(bool useInterpreter)
        {
            CheckGenericAsObjectHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericAsObjectStructWithStringAndValueTest(bool useInterpreter)
        {
            CheckGenericAsObjectHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionAsCustomTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionAsObjectHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionAsObjectTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionAsObjectHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionAsEnumTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionAsObjectHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionAsStructTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionAsObjectHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionAsStructWithStringAndValueTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionAsObjectHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionAsValueTypeAsEnumTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionAsValueTypeHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionAsValueTypeAsStructTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionAsValueTypeHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionAsValueTypeAsStructWithStringAndValueTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionAsValueTypeHelper<Scs>(useInterpreter);
        }

        [Fact]
        public static void ToStringTest()
        {
            UnaryExpression e = Expression.TypeAs(Expression.Parameter(typeof(object), "o"), typeof(string));
            Assert.Equal("(o As String)", e.ToString());
        }

        [Fact]
        public static void NonNullableValueType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.TypeAs(Expression.Constant(0), typeof(int)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.TypeAs(Expression.Constant(null), typeof(DateTime)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.TypeAs(Expression.Constant(DateTime.MinValue), typeof(DateTime)));
        }

        [Fact]
        public static void NullType() =>
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.TypeAs(Expression.Constant(""), null));

        [Fact]
        public static void NullExpression() =>
            AssertExtensions.Throws<ArgumentNullException>("expression", () => Expression.TypeAs(null, typeof(string)));

        [Fact]
        public static void AsOpenGeneric()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.TypeAs(Expression.Constant(""), typeof(List<>)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.TypeAs(Expression.Constant(""), typeof(List<>).MakeGenericType(typeof(List<>))));
        }

        [Fact]
        public static void PointerType()
        {
            AssertExtensions.Throws<ArgumentException>(
                "type", () => Expression.TypeAs(Expression.Constant(""), typeof(int).MakePointerType()));
        }

        [Fact]
        public static void ByRefType()
        {
            AssertExtensions.Throws<ArgumentException>(
                "type", () => Expression.TypeAs(Expression.Constant(""), typeof(string).MakeByRefType()));
        }

        [Fact]
        public static void UnreadableTypeAs()
        {
            MemberExpression unreadable = Expression.Property(
                null, typeof(Unreadable<string>), nameof(Unreadable<string>.WriteOnly));
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.TypeAs(unreadable, typeof(string)));
        }

        #endregion

        #region Generic helpers

        private static void CheckGenericAsObjectHelper<T>(bool useInterpreter)
        {
            T[] array = new T[] { default(T) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericAsObject<T>(array[i], useInterpreter);
            }
        }

        private static void CheckGenericWithClassRestrictionAsObjectHelper<Tc>(bool useInterpreter) where Tc : class
        {
            Tc[] array = new Tc[] { null, default(Tc) };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithClassRestrictionAsObject<Tc>(array[i], useInterpreter);
            }
        }

        private static void CheckGenericWithStructRestrictionAsObjectHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionAsObject<Ts>(array[i], useInterpreter);
            }
        }

        private static void CheckGenericWithStructRestrictionAsValueTypeHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionAsValueType<Ts>(array[i], useInterpreter);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyCustomAsCustom2(C value, bool useInterpreter)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile(useInterpreter);

            Assert.Equal(value as D, f());
        }

        private static void VerifyCustomAsInterface(C value, bool useInterpreter)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C)), typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile(useInterpreter);

            Assert.Equal(value as I, f());
        }

        private static void VerifyCustomAsIEquatableOfCustom(C value, bool useInterpreter)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C)), typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEquatable<C>, f());
        }

        private static void VerifyCustomAsIEquatableOfCustom2(C value, bool useInterpreter)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C)), typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEquatable<D>, f());
        }

        private static void VerifyCustomAsObject(C value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyCustomArrayAsCustom2Array(C[] value, bool useInterpreter)
        {
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(D[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as D[], f());
        }

        private static void VerifyCustomArrayAsIEnumerableOfCustom(C[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<C>>> e =
                Expression.Lambda<Func<IEnumerable<C>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<C>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEnumerable<C>, f());
        }

        private static void VerifyCustomArrayAsIEnumerableOfCustom2(C[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<D>>> e =
                Expression.Lambda<Func<IEnumerable<D>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<D>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEnumerable<D>, f());
        }

        private static void VerifyCustomArrayAsIEnumerableOfInterface(C[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<I>>> e =
                Expression.Lambda<Func<IEnumerable<I>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<I>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEnumerable<I>, f());
        }

        private static void VerifyCustomArrayAsIEnumerableOfObject(C[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<object>>> e =
                Expression.Lambda<Func<IEnumerable<object>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IEnumerable<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<object>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEnumerable<object>, f());
        }

        private static void VerifyCustomArrayAsIListOfCustom(C[] value, bool useInterpreter)
        {
            Expression<Func<IList<C>>> e =
                Expression.Lambda<Func<IList<C>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IList<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<C>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IList<C>, f());
        }

        private static void VerifyCustomArrayAsIListOfCustom2(C[] value, bool useInterpreter)
        {
            Expression<Func<IList<D>>> e =
                Expression.Lambda<Func<IList<D>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IList<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<D>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IList<D>, f());

        }

        private static void VerifyCustomArrayAsIListOfInterface(C[] value, bool useInterpreter)
        {
            Expression<Func<IList<I>>> e =
                Expression.Lambda<Func<IList<I>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IList<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<I>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IList<I>, f());
        }

        private static void VerifyCustomArrayAsIListOfObject(C[] value, bool useInterpreter)
        {
            Expression<Func<IList<object>>> e =
                Expression.Lambda<Func<IList<object>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(IList<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<object>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IList<object>, f());
        }

        private static void VerifyCustomArrayAsObjectArray(C[] value, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(C[])), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as object[], f());
        }

        private static void VerifyCustom2AsCustom(D value, bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(D)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);

            Assert.Equal(value as C, f());

        }

        private static void VerifyCustom2AsInterface(D value, bool useInterpreter)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(D)), typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile(useInterpreter);

            Assert.Equal(value as I, f());
        }

        private static void VerifyCustom2AsIEquatableOfCustom(D value, bool useInterpreter)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(D)), typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEquatable<C>, f());
        }

        private static void VerifyCustom2AsIEquatableOfCustom2(D value, bool useInterpreter)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(D)), typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEquatable<D>, f());
        }

        private static void VerifyCustom2AsObject(D value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(D)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyCustom2ArrayAsCustomArray(D[] value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(D[])), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as C[], f());
        }

        private static void VerifyDelegateAsFuncOfObject(Delegate value, bool useInterpreter)
        {
            Expression<Func<Func<object>>> e =
                Expression.Lambda<Func<Func<object>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Delegate)), typeof(Func<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>> f = e.Compile(useInterpreter);

            Assert.Equal(value as Func<object>, f());
        }

        private static void VerifyDelegateAsObject(Delegate value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Delegate)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyEnumAsEnumType(E value, bool useInterpreter)
        {
            Expression<Func<Enum>> e =
                Expression.Lambda<Func<Enum>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(E)), typeof(Enum)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Enum> f = e.Compile(useInterpreter);

            Assert.Equal(value as Enum, f());
        }

        private static void VerifyEnumAsObject(E value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(E)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyEnumTypeAsObject(Enum value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Enum)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyFuncOfObjectAsDelegate(Func<object> value, bool useInterpreter)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Func<object>)), typeof(Delegate)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate> f = e.Compile(useInterpreter);

            Assert.Equal(value as Delegate, f());
        }

        private static void VerifyInterfaceAsCustom(I value, bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(I)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);

            Assert.Equal(value as C, f());
        }

        private static void VerifyInterfaceAsCustom2(I value, bool useInterpreter)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(I)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile(useInterpreter);

            Assert.Equal(value as D, f());
        }

        private static void VerifyInterfaceAsObject(I value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(I)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyIEnumerableOfCustomAsCustomArray(IEnumerable<C> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as C[], f());
        }

        private static void VerifyIEnumerableOfCustomAsObjectArray(IEnumerable<C> value, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as object[], f());
        }

        private static void VerifyIEnumerableOfCustomAsCustom(IEnumerable<C> value, bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);

            Assert.Equal(value as C, f());
        }

        private static void VerifyIEnumerableOfCustomAsCustom2(IEnumerable<C> value, bool useInterpreter)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile(useInterpreter);

            Assert.Equal(value as D, f());
        }

        private static void VerifyIEnumerableOfCustomAsObject(IEnumerable<C> value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<C>)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyIEnumerableOfCustom2AsCustomArray(IEnumerable<D> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<D>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as C[], f());
        }

        private static void VerifyIEnumerableOfCustom2AsCustom(IEnumerable<D> value, bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<D>)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);

            Assert.Equal(value as C, f());
        }

        private static void VerifyIEnumerableOfCustom2AsCustom2(IEnumerable<D> value, bool useInterpreter)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<D>)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile(useInterpreter);

            Assert.Equal(value as D, f());
        }

        private static void VerifyIEnumerableOfCustom2AsObject(IEnumerable<D> value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<D>)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyIEnumerableOfInterfaceAsCustomArray(IEnumerable<I> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<I>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as C[], f());
        }

        private static void VerifyIEnumerableOfInterfaceAsObjectArray(IEnumerable<I> value, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<I>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as object[], f());
        }

        private static void VerifyIEnumerableOfObjectAsCustomArray(IEnumerable<object> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<object>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as C[], f());
        }

        private static void VerifyIEnumerableOfObjectAsObjectArray(IEnumerable<object> value, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<object>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as object[], f());
        }

        private static void VerifyIEnumerableOfStructAsStructArray(IEnumerable<S> value, bool useInterpreter)
        {
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IEnumerable<S>)), typeof(S[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as S[], f());
        }

        private static void VerifyIListOfCustomAsCustomArray(IList<C> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<C>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as C[], f());
        }

        private static void VerifyIListOfCustomAsObjectArray(IList<C> value, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<C>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as object[], f());
        }

        private static void VerifyIListOfCustom2AsCustomArray(IList<D> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<D>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as C[], f());
        }

        private static void VerifyIListOfInterfaceAsCustomArray(IList<I> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<I>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as C[], f());
        }

        private static void VerifyIListOfInterfaceAsObjectArray(IList<I> value, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<I>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as object[], f());
        }

        private static void VerifyIListOfObjectAsCustomArray(IList<object> value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<object>)), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as C[], f());
        }

        private static void VerifyIListOfObjectAsObjectArray(IList<object> value, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<object>)), typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as object[], f());
        }

        private static void VerifyIListOfStructAsStructArray(IList<S> value, bool useInterpreter)
        {
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(IList<S>)), typeof(S[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as S[], f());
        }

        private static void VerifyIntAsObject(int value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(int)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyIntAsValueType(int value, bool useInterpreter)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(int)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile(useInterpreter);

            Assert.Equal(value as ValueType, f());
        }

        private static void VerifyObjectAsCustom(object value, bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);

            Assert.Equal(value as C, f());
        }

        private static void VerifyObjectAsCustom2(object value, bool useInterpreter)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile(useInterpreter);

            Assert.Equal(value as D, f());
        }

        private static void VerifyObjectAsDelegate(object value, bool useInterpreter)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(Delegate)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate> f = e.Compile(useInterpreter);

            Assert.Equal(value as Delegate, f());
        }

        private static void VerifyObjectAsEnumType(object value, bool useInterpreter)
        {
            Expression<Func<Enum>> e =
                Expression.Lambda<Func<Enum>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(Enum)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Enum> f = e.Compile(useInterpreter);

            Assert.Equal(value as Enum, f());
        }

        private static void VerifyObjectAsInterface(object value, bool useInterpreter)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile(useInterpreter);

            Assert.Equal(value as I, f());
        }

        private static void VerifyObjectAsIEquatableOfCustom(object value, bool useInterpreter)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEquatable<C>, f());
        }

        private static void VerifyObjectAsIEquatableOfCustom2(object value, bool useInterpreter)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEquatable<D>, f());
        }

        private static void VerifyObjectAsValueType(object value, bool useInterpreter)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile(useInterpreter);

            Assert.Equal(value as ValueType, f());
        }

        private static void VerifyObjectArrayAsCustomArray(object[] value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object[])), typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);

            Assert.Equal(value as C[], f());
        }

        private static void VerifyObjectArrayAsIEnumerableOfCustom(object[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<C>>> e =
                Expression.Lambda<Func<IEnumerable<C>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<C>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEnumerable<C>, f());
        }

        private static void VerifyObjectArrayAsIEnumerableOfInterface(object[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<I>>> e =
                Expression.Lambda<Func<IEnumerable<I>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<I>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEnumerable<I>, f());
        }

        private static void VerifyObjectArrayAsIEnumerableOfObject(object[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<object>>> e =
                Expression.Lambda<Func<IEnumerable<object>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object[])), typeof(IEnumerable<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<object>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEnumerable<object>, f());
        }

        private static void VerifyObjectArrayAsIListOfCustom(object[] value, bool useInterpreter)
        {
            Expression<Func<IList<C>>> e =
                Expression.Lambda<Func<IList<C>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object[])), typeof(IList<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<C>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IList<C>, f());
        }

        private static void VerifyObjectArrayAsIListOfInterface(object[] value, bool useInterpreter)
        {
            Expression<Func<IList<I>>> e =
                Expression.Lambda<Func<IList<I>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object[])), typeof(IList<I>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<I>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IList<I>, f());
        }

        private static void VerifyObjectArrayAsIListOfObject(object[] value, bool useInterpreter)
        {
            Expression<Func<IList<object>>> e =
                Expression.Lambda<Func<IList<object>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(object[])), typeof(IList<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<object>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IList<object>, f());
        }

        private static void VerifyStructAsIEquatableOfStruct(S value, bool useInterpreter)
        {
            Expression<Func<IEquatable<S>>> e =
                Expression.Lambda<Func<IEquatable<S>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S)), typeof(IEquatable<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<S>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEquatable<S>, f());
        }

        private static void VerifyStructAsObject(S value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyStructAsValueType(S value, bool useInterpreter)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile(useInterpreter);

            Assert.Equal(value as ValueType, f());
        }

        private static void VerifyStructArrayAsIEnumerableOfStruct(S[] value, bool useInterpreter)
        {
            Expression<Func<IEnumerable<S>>> e =
                Expression.Lambda<Func<IEnumerable<S>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S[])), typeof(IEnumerable<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEnumerable<S>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEnumerable<S>, f());
        }

        private static void VerifyStructArrayAsIListOfStruct(S[] value, bool useInterpreter)
        {
            Expression<Func<IList<S>>> e =
                Expression.Lambda<Func<IList<S>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S[])), typeof(IList<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IList<S>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IList<S>, f());
        }

        private static void VerifyValueTypeAsObject(ValueType value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(ValueType)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyGenericAsObject<T>(T value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(T)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyGenericWithClassRestrictionAsObject<Tc>(Tc value, bool useInterpreter) where Tc : class
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Tc)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyGenericWithStructRestrictionAsObject<Ts>(Ts value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Ts)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyGenericWithStructRestrictionAsValueType<Ts>(Ts value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Ts)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile(useInterpreter);

            Assert.Equal(value as ValueType, f());
        }

        #endregion

        public static IEnumerable<object[]> ObjectsAndTypes()
        {
            yield return new object[] { 3, typeof(int), typeof(int?) };
            yield return new object[] { 3, typeof(int), typeof(long?) };
            yield return new object[] { "3", typeof(IEnumerable<char>), typeof(string) };
            yield return new object[] { Expression.Constant(3), typeof(Expression), typeof(ConstantExpression) };
            yield return new object[] { Expression.Constant(3), typeof(Expression), typeof(BlockExpression) };
        }

        [Theory, PerCompilationType(nameof(ObjectsAndTypes))]
        public static void MakeUnaryTypeAs(object value, Type sourceType, Type resultType, bool useInterpreter)
        {
            var instance = Expression.Constant(value, sourceType);
            var lambda = Expression.Lambda<Func<object>>(
                Expression.Convert(
                    Expression.MakeUnary(ExpressionType.TypeAs, instance, resultType),
                    typeof(object))
                );
            var func = lambda.Compile(useInterpreter);
            if (resultType.IsInstanceOfType(value))
            {
                Assert.Equal(value, func());
            }
            else
            {
                Assert.Null(func());
            }
        }
    }
}
