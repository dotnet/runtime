// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public class LateBindingTests
    {
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser))]
        [MemberData(nameof(LateCall_TestData))]
        public void LateCall(object obj, Type objType, string name, object[] args, string[] paramNames, bool[] copyBack, Func<object, object> getResult, object expected)
        {
            LateBinding.LateCall(obj, objType, name, args, paramNames, copyBack);
            Assert.Equal(expected, getResult(obj));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser))]
        [MemberData(nameof(LateGet_TestData))]
        public void LateGet(object obj, Type objType, string name, object[] args, string[] paramNames, bool[] copyBack, object expected)
        {
            Assert.Equal(expected, LateBinding.LateGet(obj, objType, name, args, paramNames, copyBack));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser))]
        [MemberData(nameof(LateSet_TestData))]
        public void LateSet(object obj, Type objType, string name, object[] args, string[] paramNames, Func<object, object> getResult, object expected)
        {
            LateBinding.LateSet(obj, objType, name, args, paramNames);
            Assert.Equal(expected, getResult(obj));
        }

        [Theory]
        [MemberData(nameof(LateSetComplex_TestData))]
        public void LateSetComplex(object obj, Type objType, string name, object[] args, string[] paramNames, bool missing, bool valueType)
        {
            if (missing)
            {
                Assert.Throws<MissingMemberException>(() => LateBinding.LateSetComplex(obj, objType, name, args, paramNames, false, false));
                Assert.Throws<MissingMemberException>(() => LateBinding.LateSetComplex(obj, objType, name, args, paramNames, false, true));
                LateBinding.LateSetComplex(obj, objType, name, args, paramNames, true, false);
                LateBinding.LateSetComplex(obj, objType, name, args, paramNames, true, true);
            }
            else if (valueType)
            {
                LateBinding.LateSetComplex(obj, objType, name, args, paramNames, false, false);
                Assert.Throws<Exception>(() => LateBinding.LateSetComplex(obj, objType, name, args, paramNames, false, true));
                LateBinding.LateSetComplex(obj, objType, name, args, paramNames, true, false);
                Assert.Throws<Exception>(() => LateBinding.LateSetComplex(obj, objType, name, args, paramNames, true, true));
            }
            else
            {
                LateBinding.LateSetComplex(obj, objType, name, args, paramNames, false, false);
                LateBinding.LateSetComplex(obj, objType, name, args, paramNames, false, true);
                LateBinding.LateSetComplex(obj, objType, name, args, paramNames, true, false);
                LateBinding.LateSetComplex(obj, objType, name, args, paramNames, true, true);
            }
        }

        [Theory]
        [MemberData(nameof(LateIndexGet_TestData))]
        public void LateIndexGet(object obj, object[] args, string[] paramNames, object expected)
        {
            Assert.Equal(expected, LateBinding.LateIndexGet(obj, args, paramNames));
        }

        [Theory]
        [MemberData(nameof(LateIndexSet_TestData))]
        public void LateIndexSet(object obj, object[] args, string[] paramNames, Func<object, object> getResult, object expected)
        {
            LateBinding.LateIndexSet(obj, args, paramNames);
            Assert.Equal(expected, getResult(obj));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser))]
        [MemberData(nameof(LateIndexSet_MissingMember_TestData))]
        public void LateIndexSet_MissingMember(object obj, object[] args, string[] paramNames)
        {
            Assert.Throws<MissingMemberException>(() => LateBinding.LateIndexSet(obj, args, paramNames));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser))]
        [MemberData(nameof(LateIndexSetComplex_TestData))]
        public void LateIndexSetComplex(object obj, object[] args, string[] paramNames, bool missing, bool valueType)
        {
            if (missing)
            {
                Assert.Throws<MissingMemberException>(() => LateBinding.LateIndexSetComplex(obj, args, paramNames, false, false));
                Assert.Throws<MissingMemberException>(() => LateBinding.LateIndexSetComplex(obj, args, paramNames, false, true));
                LateBinding.LateIndexSetComplex(obj, args, paramNames, true, false);
                LateBinding.LateIndexSetComplex(obj, args, paramNames, true, true);
            }
            else if (valueType)
            {
                LateBinding.LateIndexSetComplex(obj, args, paramNames, false, false);
                Assert.Throws<Exception>(() => LateBinding.LateIndexSetComplex(obj, args, paramNames, false, true));
                LateBinding.LateIndexSetComplex(obj, args, paramNames, true, false);
                Assert.Throws<Exception>(() => LateBinding.LateIndexSetComplex(obj, args, paramNames, true, true));
            }
            else
            {
                LateBinding.LateIndexSetComplex(obj, args, paramNames, false, false);
                LateBinding.LateIndexSetComplex(obj, args, paramNames, false, true);
                LateBinding.LateIndexSetComplex(obj, args, paramNames, true, false);
                LateBinding.LateIndexSetComplex(obj, args, paramNames, true, true);
            }
        }

        public static IEnumerable<object[]> LateCall_TestData()
        {
            yield return new object[] { null, typeof(StaticClass), "M", new object[] { }, new string[] { }, new bool[] { }, new Func<object, object>(_ => StaticClass.Value), 1 };
            yield return new object[] { null, typeof(StaticClass), "M", new object[] { 2, 3 }, new string[] { }, new bool[] { }, new Func<object, object>(_ => StaticClass.Value), 2 };
            yield return new object[] { null, typeof(StaticClass), "M", new object[] { 4, 5 }, new string[] { "a", "b" }, new bool[] { }, new Func<object, object>(_ => StaticClass.Value), 5 };
            yield return new object[] { new InstanceClass(), typeof(InstanceClass), "M", new object[] { }, new string[] { }, new bool[] { }, new Func<object, object>(obj => ((InstanceClass)obj).Value), 1 };
            yield return new object[] { new InstanceClass(), typeof(InstanceClass), "M", new object[] { 2, 3 }, new string[] { }, new bool[] { }, new Func<object, object>(obj => ((InstanceClass)obj).Value), 2 };
            yield return new object[] { new InstanceClass(), typeof(InstanceClass), "M", new object[] { 4, 5 }, new string[] { "a", "b" }, new bool[] { }, new Func<object, object>(obj => ((InstanceClass)obj).Value), 5 };
        }

        public static IEnumerable<object[]> LateGet_TestData()
        {
            yield return new object[] { null, typeof(StaticClass), "M", new object[] { }, new string[] { }, new bool[] { }, 1 };
            yield return new object[] { null, typeof(StaticClass), "M", new object[] { 2, 3 }, new string[] { }, new bool[] { }, 2 };
            yield return new object[] { null, typeof(StaticClass), "M", new object[] { 4, 5 }, new string[] { "a", "b" }, new bool[] { }, 5 };
            yield return new object[] { new InstanceClass(), typeof(InstanceClass), "M", new object[] { }, new string[] { }, new bool[] { }, 1 };
            yield return new object[] { new InstanceClass(), typeof(InstanceClass), "M", new object[] { 2, 3 }, new string[] { }, new bool[] { }, 2 };
            yield return new object[] { new InstanceClass(), typeof(InstanceClass), "M", new object[] { 4, 5 }, new string[] { "a", "b" }, new bool[] { }, 5 };
        }

        public static IEnumerable<object[]> LateSet_TestData()
        {
            yield return new object[] { null, typeof(StaticClass), "M", new object[] { 2, 3 }, new string[] { }, new Func<object, object>(_ => StaticClass.Value), 2 };
            yield return new object[] { null, typeof(StaticClass), "M", new object[] { 4, 5 }, new string[] { "a" }, new Func<object, object>(_ => StaticClass.Value), 5 };
            yield return new object[] { new InstanceClass(), typeof(InstanceClass), "M", new object[] { 2, 3 }, new string[] { }, new Func<object, object>(obj => ((InstanceClass)obj).Value), 2 };
            yield return new object[] { new InstanceClass(), typeof(InstanceClass), "M", new object[] { 4, 5 }, new string[] { "a" }, new Func<object, object>(obj => ((InstanceClass)obj).Value), 5 };
            yield return new object[] { new InstanceClass(), typeof(InstanceClass), "P", new object[] { 6 }, new string[] { }, new Func<object, object>(obj => ((InstanceClass)obj).Value), 6 };
        }

        public static IEnumerable<object[]> LateSetComplex_TestData()
        {
            yield return new object[] { new InstanceClass(), typeof(InstanceClass), "P", new object[] { 1 }, new string[] { }, false, false };
            yield return new object[] { new InstanceClass(), typeof(InstanceClass), "Q", new object[] { 2 }, new string[] { }, true, false };
            yield return new object[] { new InstanceStruct(), typeof(InstanceStruct), "P", new object[] { 3 }, new string[] { }, false, true };
            yield return new object[] { new InstanceStruct(), typeof(InstanceStruct), "Q", new object[] { 4 }, new string[] { }, true, true };
        }

        public static IEnumerable<object[]> LateIndexGet_TestData()
        {
            yield return new object[] { new InstanceClass(), new object[] { 1 }, new string[] { }, 2 };
            yield return new object[] { new InstanceClass(), new object[] { 2, 3 }, new string[] { }, 5 };
            yield return new object[] { new InstanceStruct(), new object[] { 4 }, new string[] { }, 8 };
            yield return new object[] { new InstanceStruct(), new object[] { 5, 6 }, new string[] { }, 11 };
        }

        public static IEnumerable<object[]> LateIndexSet_TestData()
        {
            yield return new object[] { new InstanceClass(), new object[] { 1, 2, 3 }, new string[] { }, new Func<object, object>(obj => ((InstanceClass)obj).Value), 6 };
            yield return new object[] { new InstanceStruct(), new object[] { 4, 5, 6 }, new string[] { }, new Func<object, object>(obj => ((InstanceStruct)obj).Value), 15 };
        }

        public static IEnumerable<object[]> LateIndexSet_MissingMember_TestData()
        {
            yield return new object[] { new StaticClass(), new object[] { 1 }, new string[] { } };
            yield return new object[] { new InstanceClass(), new object[] { 2, 3 }, new string[] { } };
            yield return new object[] { new InstanceStruct(), new object[] { 5, 6 }, new string[] { } };
        }

        public static IEnumerable<object[]> LateIndexSetComplex_TestData()
        {
            yield return new object[] { new InstanceClass(), new object[] { 1, 2, 3 }, new string[] { "x", "y" }, false, false };
            yield return new object[] { new InstanceClass(), new object[] { 4, 5 }, new string[] { "i" }, true, false };
            yield return new object[] { new InstanceStruct(), new object[] { 6, 7, 8 }, new string[] { "x", "y" }, false, true };
            yield return new object[] { new InstanceStruct(), new object[] { 9, 10 }, new string[] { "i" }, true, true };
        }

        private sealed class StaticClass
        {
            public static int Value;
            public static int M() => Value = 1;
            public static object M(int x, int y) => Value = x;
            public static object M(object a, object b) => Value = (int)b;
        }

        private sealed class InstanceClass
        {
            public int Value;
            public int P { get { return Value; } set { Value = value; } }
            public int Q => Value;
            public int M() => Value = 1;
            public object M(int x, int y) => Value = x;
            public object M(object a, object b) => Value = (int)b;
            public int this[int i] => i * 2;
            public int this[object x, object y]
            {
                get { return (int)x + (int)y; }
                set { Value = (int)x + (int)y + value; }
            }
        }

        private struct InstanceStruct
        {
            public int Value;
            public int P { get { return Value; } set { Value = value; } }
            public int Q => Value;
            public int this[int i] => i * 2;
            public int this[object x, object y]
            {
                get { return (int)x + (int)y; }
                set { Value = (int)x + (int)y + value; }
            }
        }
    }
}
