// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;
using TypeEquivalenceTypes;

[TypeIdentifier("MyScope", "MyTypeId")]
public struct EquivalentValueType
{
    public int A;
}

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
            Assert.NotEqual(a, b);
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
        Assert.True(inAsmInterfaceType == inAsmInterfaceType);
        Assert.True(inAsmInterfaceType.IsEquivalentTo(inAsmInterfaceType));
        Assert.False(inAsmInterfaceType.IsEquivalentTo(inAsm.GetType()));
        Assert.True(otherAsmInterfaceType == otherAsmInterfaceType);
        Assert.True(otherAsmInterfaceType.IsEquivalentTo(otherAsmInterfaceType));
        Assert.False(otherAsmInterfaceType.IsEquivalentTo(otherAsm.GetType()));

        // The intrinsic equality operations should fail
        Assert.False(inAsmInterfaceType == otherAsmInterfaceType);
        Assert.False(inAsmInterfaceType.Equals(otherAsmInterfaceType));
        Assert.False(otherAsmInterfaceType == inAsmInterfaceType);
        Assert.False(otherAsmInterfaceType.Equals(inAsmInterfaceType));

        // Determination of equal types requires API call
        Assert.True(inAsmInterfaceType.IsEquivalentTo(otherAsmInterfaceType));
        Assert.True(otherAsmInterfaceType.IsEquivalentTo(inAsmInterfaceType));
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

            Assert.Equal(expectedBaseValue, baseInterface.ScaleInt(input));
            Assert.Equal(expectedDerivedValue, derivedBase.ScaleInt(input));
        }

        {
            string input = "stringToScale";
            string expectedBaseValue = string.Concat(Enumerable.Repeat(input, baseScale));
            string expectedDerivedValue = string.Concat(Enumerable.Repeat(expectedBaseValue, derivedScale));

            Assert.Equal(expectedBaseValue, baseInterface.ScaleString(input));
            Assert.Equal(expectedDerivedValue, derivedBase.ScaleString(input));
        }
    }

    private static void CallSparseInterface()
    {
        Console.WriteLine($"{nameof(CallSparseInterface)}");

        int sparseTypeMethodCount = typeof(ISparseType).GetMethods(BindingFlags.Public | BindingFlags.Instance).Length;
        Assert.Equal(2, sparseTypeMethodCount);

        var sparseType = (ISparseType)SparseTest.Create();
        Assert.Equal(20, SparseTest.GetSparseInterfaceMethodCount());

        int input = 63;
        Assert.Equal(input * 7, sparseType.MultiplyBy7(input));
        Assert.Equal(input * 18, sparseType.MultiplyBy18(input));
    }

    private static void TestArrayEquivalence()
    {
        Console.WriteLine($"{nameof(TestArrayEquivalence)}");
        var inAsm = EmptyType.Create();
        var otherAsm = EmptyType2.Create();

        Type inAsmInterfaceType = inAsm.GetType().GetInterface(nameof(IEmptyType));
        Type otherAsmInterfaceType = otherAsm.GetType().GetInterface(nameof(IEmptyType));

        Assert.True(inAsmInterfaceType.MakeArrayType().IsEquivalentTo(otherAsmInterfaceType.MakeArrayType()));
        Assert.True(inAsmInterfaceType.MakeArrayType(1).IsEquivalentTo(otherAsmInterfaceType.MakeArrayType(1)));
        Assert.True(inAsmInterfaceType.MakeArrayType(2).IsEquivalentTo(otherAsmInterfaceType.MakeArrayType(2)));

        Assert.False(inAsmInterfaceType.MakeArrayType().IsEquivalentTo(otherAsmInterfaceType.MakeArrayType(1)));
        Assert.False(inAsmInterfaceType.MakeArrayType(1).IsEquivalentTo(otherAsmInterfaceType.MakeArrayType(2)));
    }

    private static void TestByRefEquivalence()
    {
        Console.WriteLine($"{nameof(TestByRefEquivalence)}");
        var inAsm = EmptyType.Create();
        var otherAsm = EmptyType2.Create();

        Type inAsmInterfaceType = inAsm.GetType().GetInterface(nameof(IEmptyType));
        Type otherAsmInterfaceType = otherAsm.GetType().GetInterface(nameof(IEmptyType));

        Assert.True(inAsmInterfaceType.MakeByRefType().IsEquivalentTo(otherAsmInterfaceType.MakeByRefType()));
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

        Assert.False(typeof(Generic<>).MakeGenericType(inAsmInterfaceType).IsEquivalentTo(typeof(Generic<>).MakeGenericType(otherAsmInterfaceType)));
    }

    private static void TestGenericInterfaceEquivalence()
    {
        Console.WriteLine($"{nameof(TestGenericInterfaceEquivalence)}");
        var inAsm = EmptyType.Create();
        var otherAsm = EmptyType2.Create();

        Type inAsmInterfaceType = inAsm.GetType().GetInterface(nameof(IEmptyType));
        Type otherAsmInterfaceType = otherAsm.GetType().GetInterface(nameof(IEmptyType));

        Assert.True(typeof(IGeneric<>).MakeGenericType(inAsmInterfaceType).IsEquivalentTo(typeof(IGeneric<>).MakeGenericType(otherAsmInterfaceType)));
    }

    private static unsafe void TestTypeEquivalenceWithTypePunning()
    {
        Console.WriteLine($"{nameof(TestTypeEquivalenceWithTypePunning)}");

        {
            Console.WriteLine($"-- GetFunctionPointer()");
            IntPtr fptr = typeof(CreateFunctionPointer).GetMethod("For_1").MethodHandle.GetFunctionPointer();
            Assert.NotEqual(IntPtr.Zero, fptr);
            var s = new OnlyLoadOnce_1()
            {
                Field = 0x11
            };
            int res = ((delegate* <OnlyLoadOnce_1, int>)fptr)(s);
            Assert.Equal(s.Field, res);
        }
        {
            Console.WriteLine($"-- Ldftn");
            IntPtr fptr = CreateFunctionPointer.For_2_Ldftn();
            Assert.NotEqual(IntPtr.Zero, fptr);
            var s = new OnlyLoadOnce_2()
            {
                Field = 0x22
            };
            int res = ((delegate* <OnlyLoadOnce_2, int>)fptr)(s);
            Assert.Equal(s.Field, res);
        }
        {
            Console.WriteLine($"-- Ldvirtftn");
            IntPtr fptr = CreateFunctionPointer.For_3_Ldvirtftn(out object inst);
            Assert.NotEqual(IntPtr.Zero, fptr);
            var s = new OnlyLoadOnce_3()
            {
                Field = 0x33
            };
            int res = ((delegate* <object, OnlyLoadOnce_3, int>)fptr)(inst, s);
            Assert.Equal(s.Field, res);
        }
    }

    [MethodImpl (MethodImplOptions.NoInlining)]
    private static void TestLoadingValueTypesWithMethod() 
    {
        Console.WriteLine($"{nameof(TestLoadingValueTypesWithMethod)}");
        Console.WriteLine($"-- {typeof(ValueTypeWithStaticMethod).Name}");
        Assert.Throws<TypeLoadException>(() => LoadInvalidType());
    }

    [MethodImpl (MethodImplOptions.NoInlining)]
    private static void LoadInvalidType()
    {
        Console.WriteLine($"-- {typeof(ValueTypeWithInstanceMethod).Name}");
    }

    private static void TestCastsOptimizations()
    {
        string otherTypeName = $"{typeof(EquivalentValueType).FullName},{typeof(EmptyType).Assembly.GetName().Name}";
        Type otherEquivalentValueType = Type.GetType(otherTypeName);

        // ensure that an instance of otherEquivalentValueType can cast to EquivalentValueType
        object otherEquivalentValueTypeInstance = Activator.CreateInstance(otherEquivalentValueType);
        Assert.True(otherEquivalentValueTypeInstance is EquivalentValueType);
        EquivalentValueType inst = (EquivalentValueType)otherEquivalentValueTypeInstance;
    }

    public static int Main()
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
            TestTypeEquivalenceWithTypePunning();
            TestLoadingValueTypesWithMethod();
            TestCastsOptimizations();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }
}
