// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This C# script can be executed using the csi
// tool found in Tools\net46\roslyn\tools.
//
// It produces a C# file (on stdout) containing tests for various
// COMISS/UCOMISS/PTEST/VTESTPS/VTESTPD based intrinsics.

using System;
using System.Collections.Generic;
using System.IO;

[Flags]
enum TestKind
{
    // Use the intrinsic as is
    Normal = 0,
    // Negate the intrinsic result
    LogicalNot = 1,
    // Use a branch to test the intrinsic result
    Branch = 2,
    // Try to cause the intrinsic operands to be
    // swapped by placing the first operand in
    // memory and the second in a register.
    Swap = 4
}

void GenerateCompareTests(List<Test> tests)
{
    var inputs = new (double x, double y)[]
    {
        (42.0, 42.0),
        (41.0, 42.0),
        (42.0, 41.0),
        (42.0, double.NaN),
        (double.NaN, double.NaN)
    };

    bool EQ(double x, double y) => x == y;
    bool NE(double x, double y) => x != y;
    bool LT(double x, double y) => x < y;
    bool LE(double x, double y) => x <= y;
    bool GT(double x, double y) => x > y;
    bool GE(double x, double y) => x >= y;

    foreach (var intrinsic in new (string name, Func<double, double, bool> op, (double x, double y)[] inputs)[]
    {
        ("CompareScalarOrderedEqual", EQ, inputs),
        ("CompareScalarOrderedNotEqual", NE, inputs),
        ("CompareScalarOrderedLessThan", LT, inputs),
        ("CompareScalarOrderedLessThanOrEqual", LE, inputs),
        ("CompareScalarOrderedGreaterThan", GT, inputs),
        ("CompareScalarOrderedGreaterThanOrEqual", GE, inputs),

        ("CompareScalarUnorderedEqual", EQ, inputs),
        ("CompareScalarUnorderedNotEqual", NE, inputs),
        ("CompareScalarUnorderedLessThan", LT, inputs),
        ("CompareScalarUnorderedLessThanOrEqual", LE, inputs),
        ("CompareScalarUnorderedGreaterThan", GT, inputs),
        ("CompareScalarUnorderedGreaterThanOrEqual", GE, inputs)
    })
    {
        foreach ((string isa, int vectorSize, string vectorElementType) in new[]
        {
            ("Sse", 128, "Single"),
            ("Sse2", 128, "Double")
        })
        {
            foreach (TestKind kind in new[]
            {
                TestKind.Normal,
                TestKind.LogicalNot,
                TestKind.Branch,
                TestKind.Swap,
                TestKind.Swap | TestKind.Branch
            })
            {
                tests.Add(new BinaryOpTest<double>(isa, intrinsic.name, vectorSize, vectorElementType, kind, intrinsic.op, intrinsic.inputs));
            }
        }
    }
}

void GeneratePackedIntTestTests(List<Test> tests)
{
    var inputs = new (int x, int y)[]
    {
        (0, 0),
        (1, 2),
        (2, 3),
        (3, 2)
    };

    bool Z(int x, int y) => (x & y) == 0;
    bool C(int x, int y) => (~x & y) == 0;

    foreach (var intrinsic in new (string name, Func<int, int, bool> op, (int x, int y)[] inputs)[]
    {
        ("TestZ", Z, inputs),
        ("TestC", C, inputs),
        ("TestNotZAndNotC", (x, y) => !Z(x, y) & !C(x, y), inputs)
    })
    {
        foreach ((string isa, int vectorSize, string vectorElementType) in new[]
        {
            ("Sse41", 128, "Int32"),
            ("Avx", 128, "Int32"),
            ("Avx", 256, "Int32")
        })
        {
            foreach (TestKind kind in new[]
            {
                TestKind.Normal,
                TestKind.LogicalNot,
                TestKind.Branch,
                TestKind.Swap,
                TestKind.Swap | TestKind.LogicalNot
            })
            {
                tests.Add(new BinaryOpTest<int>(isa, intrinsic.name, vectorSize, vectorElementType, kind, intrinsic.op, intrinsic.inputs));
            }
        }
    }
}

void GeneratePackedDoubleTestTests(List<Test> tests)
{
    var inputs = new (double x, double y)[]
    {
        (1.0, 1.0),
        (1.0, -1.0),
        (-1.0, -1.0)
    };

    bool S(double d) => d < 0.0;
    bool Z(double x, double y) => (S(x) & S(y)) == false;
    bool C(double x, double y) => (!S(x) & S(y)) == false;

    foreach (var intrinsic in new (string name, Func<double, double, bool> op, (double x, double y)[] inputs)[]
    {
        ("TestZ", Z, inputs),
        ("TestC", C, inputs),
        ("TestNotZAndNotC", (x, y) => !Z(x, y) && !C(x, y), inputs)
    })
    {
        foreach ((string isa, int vectorSize, string vectorElementType) in new[]
        {
            ("Avx", 128, "Single"),
            ("Avx", 256, "Single")
        })
        {
            foreach (TestKind kind in new[]
            {
                TestKind.Normal,
                TestKind.LogicalNot,
                TestKind.Branch,
                TestKind.Swap,
                TestKind.Swap | TestKind.Branch | TestKind.LogicalNot
            })
            {
                tests.Add(new BinaryOpTest<double>(isa, intrinsic.name, vectorSize, vectorElementType, kind, intrinsic.op, intrinsic.inputs));
            }
        }
    }
}

static string CreateVector(int vectorSize, string vectorElementType, double value)
{
    if (vectorElementType == "Single")
        return double.IsNaN(value) ? $"Vector{vectorSize}.Create(float.NaN)" : $"Vector{vectorSize}.Create({value:F1}f)";
    if (vectorElementType == "Double")
        return double.IsNaN(value) ? $"Vector{vectorSize}.Create(double.NaN)" : $"Vector{vectorSize}.Create({value:F1})";
    throw new NotSupportedException();
}

static string CreateVector(int vectorSize, int value)
{
    return $"Vector{vectorSize}.Create({value})";
}

static string CreateVector<T>(int vectorSize, string vectorElementType, T value)
{
    if (value is double d)
        return CreateVector(vectorSize, vectorElementType, d);
    if (value is int i)
        return CreateVector(vectorSize, i);
    throw new NotSupportedException();
}

abstract class Test
{
    public readonly string Isa;
    public readonly string Intrinsic;
    public readonly int VectorSize;
    public readonly string VectorElementType;
    public readonly string VectorType;
    public readonly TestKind Kind;

    public Test(string isa, string intrinsic, int vectorSize, string vectorElementType, TestKind kind)
    {
        Isa = isa;
        Intrinsic = intrinsic;
        VectorSize = vectorSize;
        VectorElementType = vectorElementType;
        VectorType = $"Vector{VectorSize}<{VectorElementType}>";
        Kind = kind;
    }

    public string Name => $"Test_{Isa}_{Intrinsic}_{Kind.ToString().Replace(',', '_').Replace(" ", "")}";
    public abstract void WriteTestMethod(TextWriter w);
    public abstract void WriteTestCases(TextWriter w);
}

class UnaryOpTest<T> : Test
{
    Func<T, bool> op;
    T[] inputs;

    public UnaryOpTest(string isa, string intrinsic, int vectorSize, string vectorElementType, TestKind kind, Func<T, bool> op, T[] inputs)
        : base(isa, intrinsic, vectorSize, vectorElementType, kind)
    {
        this.op = op;
        this.inputs = inputs;
    }

    public override void WriteTestMethod(TextWriter w)
    {
        w.WriteLine();
        w.WriteLine("    [MethodImpl(MethodImplOptions.NoInlining)]");
        w.WriteLine($"    static bool {Name}(in {VectorType} x)");
        w.WriteLine("    {");

        w.Write("        return ");

        if (Kind.HasFlag(TestKind.LogicalNot))
            w.Write("!");

        w.Write($"{Isa}.{Intrinsic}(x)");

        if (Kind.HasFlag(TestKind.Branch))
            w.Write(" ? True() : False()");

        w.WriteLine(";");
        w.WriteLine("    }");
    }

    string Check(T x)
    {
        return (Kind.HasFlag(TestKind.LogicalNot) ? !op(x) : op(x)).ToString().ToLowerInvariant();
    }

    public override void WriteTestCases(TextWriter w)
    {
        foreach (var input in inputs)
            w.WriteLine($"        r &= !{Isa}.IsSupported || Check({Check(input)}, {Name}({CreateVector(VectorSize, VectorElementType, input)}));");
    }
}

class BinaryOpTest<T> : Test
{
    Func<T, T, bool> op;
    (T x, T y)[] inputs;

    public BinaryOpTest(string isa, string intrinsic, int vectorSize, string vectorElementType, TestKind kind, Func<T, T, bool> op, (T x, T y)[] inputs)
        : base(isa, intrinsic, vectorSize, vectorElementType, kind)
    {
        this.op = op;
        this.inputs = inputs;
    }

    public override void WriteTestMethod(TextWriter w)
    {
        w.WriteLine();
        w.WriteLine("    [MethodImpl(MethodImplOptions.NoInlining)]");
        // Pass parameters by reference so we get consistency across various ABIs.
        // We get operands in memory and by adding an extra "nop" intrinsic we can
        // force one of the operands in a register, just enough to catch some cases
        // of containment.
        w.WriteLine($"    static bool {Name}(in {VectorType} x, in {VectorType} y)");
        w.WriteLine("    {");
        w.Write("        return ");

        if (Kind.HasFlag(TestKind.LogicalNot))
            w.Write("!");

        if (Kind.HasFlag(TestKind.Swap))
            w.Write($"{Isa}.{Intrinsic}(x, {Isa}.Or(y.AsSingle(), default).As{VectorElementType}())");
        else
            w.Write($"{Isa}.{Intrinsic}(x, y)");

        if (Kind.HasFlag(TestKind.Branch))
            w.Write(" ? True() : False()");

        w.WriteLine(";");
        w.WriteLine("    }");
    }

    string Check((T x, T y) input)
    {
        return (Kind.HasFlag(TestKind.LogicalNot) ? !op(input.x, input.y) : op(input.x, input.y)).ToString().ToLowerInvariant();
    }

    public override void WriteTestCases(TextWriter w)
    {
        foreach (var input in inputs)
            w.WriteLine($"        r &= !{Isa}.IsSupported || Check({Check(input)}, {Name}({CreateVector(VectorSize, VectorElementType, input.x)}, {CreateVector(VectorSize, VectorElementType, input.y)}));");
    }
}

var tests = new List<Test>();
GenerateCompareTests(tests);
GeneratePackedIntTestTests(tests);
GeneratePackedDoubleTestTests(tests);

var w = Console.Out;
w.WriteLine(@"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class GitHub_17073
{
    [MethodImpl(MethodImplOptions.NoInlining)] static bool True() => true;
    [MethodImpl(MethodImplOptions.NoInlining)] static bool False() => false;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Check(bool expected, bool actual, [CallerLineNumber] int line = 0)
    {
        if (expected != actual) Console.WriteLine(""Failed at line {0}"", line);
        return expected == actual;
    }
");

w.WriteLine("    [Fact]");
w.WriteLine("    public static void Test()");
w.WriteLine("    {");
w.WriteLine("        bool r = true;");

foreach (var test in tests)
    test.WriteTestCases(w);

w.WriteLine("        Assert.Equal(true, r);");
w.WriteLine("    }");

foreach (var test in tests)
    test.WriteTestMethod(w);

w.WriteLine("}");
