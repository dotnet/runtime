// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;

using TestLibrary;
using TypeEquivalenceTypes;

public class Simple
{
    private class EmptyType2 : IEmptyType
    {
        /// <summary>
        /// Create an instance of <see cref="EmptyType" />
        /// </summary>
        public static object Create()
        {
            return new EmptyType2();
        }
    }

    private static void InterfaceTypesFromDifferentAssembliesAreEquivalent()
    {
        Console.WriteLine($"{nameof(InterfaceTypesFromDifferentAssembliesAreEquivalent)}");
        var inAsm = EmptyType.Create();
        var otherAsm = EmptyType2.Create();

        AreNotSameObject((IEmptyType)inAsm, (IEmptyType)otherAsm);

        void AreNotSameObject(IEmptyType a, IEmptyType b)
        {
            Assert.AreNotEqual(a, b);
        }
    }

    private static void ValidateTypeInstanceEquality()
    {
        Console.WriteLine($"{nameof(ValidateTypeInstanceEquality)}");
        var inAsm = EmptyType.Create();
        var otherAsm = EmptyType2.Create();

        Type inAsmInterfaceType = inAsm.GetType().GetInterface(nameof(IEmptyType));
        Type otherAsmInterfaceType = otherAsm.GetType().GetInterface(nameof(IEmptyType));

        // Sanity checks
        Assert.IsTrue(inAsmInterfaceType == inAsmInterfaceType);
        Assert.IsTrue(inAsmInterfaceType.IsEquivalentTo(inAsmInterfaceType));
        Assert.IsFalse(inAsmInterfaceType.IsEquivalentTo(inAsm.GetType()));
        Assert.IsTrue(otherAsmInterfaceType == otherAsmInterfaceType);
        Assert.IsTrue(otherAsmInterfaceType.IsEquivalentTo(otherAsmInterfaceType));
        Assert.IsFalse(otherAsmInterfaceType.IsEquivalentTo(otherAsm.GetType()));

        // The intrinsic equality operations should fail
        Assert.IsFalse(inAsmInterfaceType == otherAsmInterfaceType);
        Assert.IsFalse(inAsmInterfaceType.Equals(otherAsmInterfaceType));
        Assert.IsFalse(otherAsmInterfaceType == inAsmInterfaceType);
        Assert.IsFalse(otherAsmInterfaceType.Equals(inAsmInterfaceType));

        // Determination of equal types requires API call
        Assert.IsTrue(inAsmInterfaceType.IsEquivalentTo(otherAsmInterfaceType));
        Assert.IsTrue(otherAsmInterfaceType.IsEquivalentTo(inAsmInterfaceType));
    }

    private class MethodTestDerived : MethodTestBase
    {
        private readonly int scaleValue;

        private IMethodTestType inner;

        /// <summary>
        /// Create an instance of <see cref="MethodTestDerived" />
        /// </summary>
        public static object Create(int scaleValue, int baseScaleValue)
        {
            return new MethodTestDerived(scaleValue, baseScaleValue);
        }

        private MethodTestDerived(int scaleValue, int baseScaleValue)
            : base(baseScaleValue)
        {
            this.scaleValue = scaleValue;
        }

        public override int ScaleInt(int i)
        {
            return base.ScaleInt(i) * this.scaleValue;
        }

        public override string ScaleString(string s)
        {
            string baseValue = base.ScaleString(s);
            var sb = new StringBuilder(this.scaleValue * baseValue.Length);
            for (int i = 0; i < this.scaleValue; ++i)
            {
                sb.Append(baseValue);
            }

            return sb.ToString();
        }
    }

    private static void InterfaceTypesMethodOperations()
    {
        Console.WriteLine($"{nameof(InterfaceTypesMethodOperations)}");

        int baseScale = 2;
        int derivedScale = 3;
        object baseInst = MethodTestBase.Create(baseScale);
        object derivedInst = MethodTestDerived.Create(derivedScale, baseScaleValue: baseScale);

        var baseInterface = (IMethodTestType)baseInst;
        var derivedBase = (MethodTestBase)derivedInst;

        {
            int input = 67;
            int expectedBaseValue = input * baseScale;
            int expectedDerivedValue = expectedBaseValue * derivedScale;

            Assert.AreEqual(expectedBaseValue, baseInterface.ScaleInt(input));
            Assert.AreEqual(expectedDerivedValue, derivedBase.ScaleInt(input));
        }

        {
            string input = "stringToScale";
            string expectedBaseValue = string.Concat(Enumerable.Repeat(input, baseScale));
            string expectedDerivedValue = string.Concat(Enumerable.Repeat(expectedBaseValue, derivedScale));

            Assert.AreEqual(expectedBaseValue, baseInterface.ScaleString(input));
            Assert.AreEqual(expectedDerivedValue, derivedBase.ScaleString(input));
        }
    }

    private static void CallSparseInterface()
    {
        Console.WriteLine($"{nameof(CallSparseInterface)}");

        int sparseTypeMethodCount = typeof(ISparseType).GetMethods(BindingFlags.Public | BindingFlags.Instance).Length;
        Assert.AreEqual(2, sparseTypeMethodCount, "Should have limited method metadata");

        var sparseType = (ISparseType)SparseTest.Create();
        Assert.AreEqual(20, SparseTest.GetSparseInterfaceMethodCount(), "Should have all method metadata");

        int input = 63;
        Assert.AreEqual(input * 7, sparseType.MultiplyBy7(input));
        Assert.AreEqual(input * 18, sparseType.MultiplyBy18(input));
    }

    private static void TestArrayEquivalence()
    {
        Console.WriteLine($"{nameof(TestArrayEquivalence)}");
        var inAsm = EmptyType.Create();
        var otherAsm = EmptyType2.Create();

        Type inAsmInterfaceType = inAsm.GetType().GetInterface(nameof(IEmptyType));
        Type otherAsmInterfaceType = otherAsm.GetType().GetInterface(nameof(IEmptyType));

        Assert.IsTrue(inAsmInterfaceType.MakeArrayType().IsEquivalentTo(otherAsmInterfaceType.MakeArrayType()));
        Assert.IsTrue(inAsmInterfaceType.MakeArrayType(1).IsEquivalentTo(otherAsmInterfaceType.MakeArrayType(1)));
        Assert.IsTrue(inAsmInterfaceType.MakeArrayType(2).IsEquivalentTo(otherAsmInterfaceType.MakeArrayType(2)));

        Assert.IsFalse(inAsmInterfaceType.MakeArrayType().IsEquivalentTo(otherAsmInterfaceType.MakeArrayType(1)));
        Assert.IsFalse(inAsmInterfaceType.MakeArrayType(1).IsEquivalentTo(otherAsmInterfaceType.MakeArrayType(2)));
    }

    private static void TestByRefEquivalence()
    {
        Console.WriteLine($"{nameof(TestByRefEquivalence)}");
        var inAsm = EmptyType.Create();
        var otherAsm = EmptyType2.Create();

        Type inAsmInterfaceType = inAsm.GetType().GetInterface(nameof(IEmptyType));
        Type otherAsmInterfaceType = otherAsm.GetType().GetInterface(nameof(IEmptyType));

        Assert.IsTrue(inAsmInterfaceType.MakeByRefType().IsEquivalentTo(otherAsmInterfaceType.MakeByRefType()));
    }

    interface IGeneric<in T>
    {
        void Method(T input);
    }

    class Generic<V> : IGeneric<V>
    {
        public void Method(V input)
        {
        }
    }

    private static void TestGenericClassNonEquivalence()
    {
        Console.WriteLine($"{nameof(TestGenericClassNonEquivalence)}");
        var inAsm = EmptyType.Create();
        var otherAsm = EmptyType2.Create();

        Type inAsmInterfaceType = inAsm.GetType().GetInterface(nameof(IEmptyType));
        Type otherAsmInterfaceType = otherAsm.GetType().GetInterface(nameof(IEmptyType));

        Assert.IsFalse(typeof(Generic<>).MakeGenericType(inAsmInterfaceType).IsEquivalentTo(typeof(Generic<>).MakeGenericType(otherAsmInterfaceType)));
    }

    private static void TestGenericInterfaceEquivalence()
    {
        Console.WriteLine($"{nameof(TestGenericInterfaceEquivalence)}");
        var inAsm = EmptyType.Create();
        var otherAsm = EmptyType2.Create();

        Type inAsmInterfaceType = inAsm.GetType().GetInterface(nameof(IEmptyType));
        Type otherAsmInterfaceType = otherAsm.GetType().GetInterface(nameof(IEmptyType));

        Assert.IsTrue(typeof(IGeneric<>).MakeGenericType(inAsmInterfaceType).IsEquivalentTo(typeof(IGeneric<>).MakeGenericType(otherAsmInterfaceType)));
    }

    public static int Main(string[] noArgs)
    {
        if (!OperatingSystem.IsWindows())
        {
            return 100;
        }
        try
        {
            InterfaceTypesFromDifferentAssembliesAreEquivalent();
            ValidateTypeInstanceEquality();
            InterfaceTypesMethodOperations();
            CallSparseInterface();
            TestByRefEquivalence();
            TestArrayEquivalence();
            TestGenericClassNonEquivalence();
            TestGenericInterfaceEquivalence();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }
}
