// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains the code that generates "longmul.il", on stdout.
// It can be executed via the global "dotnet-script" tool (dotnet tool install -g dotnet-script).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using static TestsGen;

var testName = "LongMulOn32BitTest";

OutLicenseHeader();
Out(".assembly extern System.Console { auto }");
Out(".assembly extern System.Runtime { auto }");
Out($".assembly {testName} {{ }}");
Out();

OpenScope($".class auto {testName} extends [System.Runtime]System.Object");

var inputConsts = new int[]
{
    int.MinValue,
    -1,
    0,
    int.MaxValue
};

var inputValues = new List<string>();
foreach (var inputConst in inputConsts)
{
    inputValues.Add($"ldc.i4 {inputConst}");
}
inputValues.Add("ldarg");

var casts = new[] { "conv.i8", "conv.u8" };
var inputs = new List<InputKind>();
foreach (var cast in casts)
{
    foreach (var value in inputValues)
    {
        inputs.Add(new InputKind { Cast = cast, Value = value });
    }
}

var muls = new HashSet<LongMul>();
foreach (var left in inputs)
{
    foreach (var right in inputs)
    {
        // Don't include folded cases to keep the size of the test down.
        if (left.IsConst && right.IsConst)
        {
            break;
        }

        var leftInput = left;
        var rightInput = right;
        if (left.IsArg)
        {
            leftInput = new InputKind { Cast = left.Cast, Value = "ldarg left" };
        }
        if (right.IsArg)
        {
            rightInput = new InputKind { Cast = right.Cast, Value = "ldarg right" };
        }

        var index = muls.Count;
        var mul = new LongMul { Left = leftInput, Right = rightInput };

        muls.Add(new LongMul { Left = mul.Left, Right = mul.Right, MethodName = $"LongMul_{++index}" });
        muls.Add(new LongMul { Left = mul.Left, Right = mul.Right, MethodName = $"LongMul_{++index}", IsOverflow = true });
        muls.Add(new LongMul { Left = mul.Left, Right = mul.Right, MethodName = $"LongMul_{++index}", IsOverflow = true, IsUnsigned = true });
    }
}

OpenScope(".method private hidebysig static void PrintErrorWithResult(string message, int64 result) cil managed");
Out("call class [System.Runtime]System.IO.TextWriter [System.Console]System.Console::get_Error()");
Out("ldarg message");
Out("ldarg result");
Out("box [System.Runtime]System.Int64");
Out("callvirt instance void [System.Runtime]System.IO.TextWriter::WriteLine(string, object)");
Out("ret");
CloseScope();

Out();
OpenScope(".method private hidebysig static void PrintError(string message) cil managed");
Out($"call class [System.Runtime]System.IO.TextWriter [System.Console]System.Console::get_Error()");
Out($"ldarg message");
Out($"callvirt instance void [System.Runtime]System.IO.TextWriter::WriteLine(string)");
Out("ret");
CloseScope();

void Print(string msg)
{
    Out($@"ldstr ""{msg}""");
    Out($"call void [System.Console]System.Console::WriteLine(string)");
}

void PrintError(string msg)
{
    Out($@"ldstr ""{msg}""");
    Out($"call void {testName}::PrintError(string)");
}

void PrintErrorWithResult(string fmt)
{
    Out($@"ldstr ""{fmt}""");
    Out("ldloc result");
    Out($"call void {testName}::PrintErrorWithResult(string, int64)");
}

Out();
OpenScope(".method private hidebysig static int32 Main() cil managed");
Out(".entrypoint");
Out(".locals ( int64 result )");

var nextBranchIndex = 0;
foreach (var mul in muls)
{
    var leftValues = mul.Left.IsConst ? new[] { mul.Left.Const } : inputConsts;
    var rightValues = mul.Right.IsConst ? new[] { mul.Right.Const } : inputConsts;
    var scenarios = new List<(bool EmitLeft, bool EmitRight, int LeftValue, int RightValue, long? Expected)>();

    foreach (var leftValue in leftValues)
    {
        foreach (var rightValue in rightValues)
        {
            var longLeftValue = mul.Left.SignExtends ? SignExtend(leftValue) : ZeroExtend(leftValue);
            var longRightValue = mul.Right.SignExtends ? SignExtend(rightValue) : ZeroExtend(rightValue);

            long? expected = longLeftValue * longRightValue;
            var emitLeft = mul.Left.IsArg;
            var emitRight = mul.Right.IsArg;

            if (mul.IsOverflow)
            {
                bool overflow = mul.IsUnsigned ?
                    (ulong)expected != new BigInteger((ulong)longLeftValue) * new BigInteger((ulong)longRightValue) :
                    expected != new BigInteger(longLeftValue) * new BigInteger(longRightValue);

                if (overflow)
                {
                    expected = null;
                }
            }

            scenarios.Add((emitLeft, emitRight, leftValue, rightValue, expected));
        }
    }

    foreach (var (emitLeft, emitRight, leftValue, rightValue, expected) in scenarios)
    {
        Out();

        var expectOverflow = expected is null;
        if (expectOverflow)
        {
            OpenScope(".try");
        }

        if (emitLeft)
        {
            Out($"ldc.i4 {leftValue}");
        }
        if (emitRight)
        {
            Out($"ldc.i4 {rightValue}");
        }
        var fullMethodName = $"{testName}::{mul.MethodName}";
        Out($"call int64 {fullMethodName}({string.Join(", ", Enumerable.Repeat("int32", mul.ArgCount))})");

        if (expectOverflow)
        {
            PrintError($"'{fullMethodName}' failed to throw OverflowException");
            Out("leave FAIL");
            CloseScope();
            OpenScope("catch [System.Runtime]System.OverflowException");
            Out($"leave NEXT{++nextBranchIndex}");
            CloseScope();
        }
        else
        {
            Out("stloc result");
            Out("ldloc result");
            Out($"ldc.i8 {expected}");
            Out($"ceq");
            Out($"brtrue NEXT{++nextBranchIndex}");
            PrintErrorWithResult($"'{fullMethodName}' returned: '{{0}}'. Expected: '{expected}'");
            Out("br FAIL");
        }

        Out($"NEXT{nextBranchIndex}:", $"{TestsGen.LastIndent}");
    }
}

Out();
Print("SUCCESS");
Out("ldc.i4 100");
Out("ret");

Out("FAIL:", "");
PrintError("FAILED");
Out("ldc.i4 1");
Out("ret");

CloseScope();

foreach (var mul in muls)
{
    var parameters = new List<string>();
    if (mul.Left.IsArg)
    {
        parameters.Add("int32 left");
    }
    if (mul.Right.IsArg)
    {
        parameters.Add("int32 right");
    }

    Out();
    OpenScope($".method private hidebysig static int64 {mul.MethodName}({string.Join(", ", parameters)}) cil managed noinlining");
    Out(mul.Left.Value);
    Out(mul.Left.Cast);
    Out(mul.Right.Value);
    Out(mul.Right.Cast);
    Out(mul.Instruction());
    Out("ret");
    CloseScope();
}

CloseScope();

[MethodImpl(MethodImplOptions.NoInlining)]
static long SignExtend(int value) => value;

[MethodImpl(MethodImplOptions.NoInlining)]
static long ZeroExtend(int value) => (uint)value;

public sealed class LongMul
{
    public InputKind Left { get; set; }
    public InputKind Right { get; set; }
    public bool IsOverflow { get; set; }
    public bool IsUnsigned { get; set; }

    public string MethodName { get; set; }

    public int ArgCount => (Left.IsArg, Right.IsArg) switch
    {
        (true, true) => 2,
        (false, false) => 0,
        _ => 1
    };

    public string Instruction() => $"mul{(IsOverflow ? ".ovf" : "")}{(IsUnsigned ? ".un" : "")}";

    public override int GetHashCode() => HashCode.Combine(Left, Right, IsOverflow, IsUnsigned);

    public bool Equals(LongMul other) =>
        other != null &&
        Left.Equals(other.Left) &&
        Right.Equals(other.Right) &&
        IsOverflow == other.IsOverflow &&
        IsUnsigned == other.IsUnsigned;
}

public sealed class InputKind : IEquatable<InputKind>
{
    public string Value { get; set; }
    public string Cast { get; set; }

    public bool SignExtends => Cast.Contains("i8");
    public bool IsArg => Value.Contains("ldarg");
    public bool IsConst => Value.Contains("ldc");

    public int Const => int.Parse(Value.Split()[1]);

    public override bool Equals(object obj) => Equals(obj as InputKind);
    public bool Equals(InputKind other) => other != null && Value == other.Value && Cast == other.Cast;
    public override int GetHashCode() => HashCode.Combine(Value, Cast);
}

public static class TestsGen
{
    public static string IndentBase { get; set; } = "  ";
    public static string Indent { get; private set; }
    public static string LastIndent => Indent.Substring(0, Indent.Length - IndentBase.Length);

    public static void OpenScope(string header)
    {
        Out(header);
        Out("{");
        Indent += IndentBase;
    }

    public static void CloseScope()
    {
        Indent = Indent.Substring(0, Indent.Length - IndentBase.Length);
        Out("}");
    }

    public static void Out(string line = "", string indent = null)
    {
        if (indent is null)
        {
            indent = Indent;
        }

        line = string.IsNullOrWhiteSpace(line) ? "" : indent + line;

        Console.WriteLine(line);
    }

    public static void OutLicenseHeader()
    {
        Out("// Licensed to the .NET Foundation under one or more agreements.");
        Out("// The .NET Foundation licenses this file to you under the MIT license.");
        Out();
    }
}
