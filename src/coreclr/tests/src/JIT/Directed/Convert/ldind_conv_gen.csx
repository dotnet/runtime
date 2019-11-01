// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This C# script can be executed using the csi
// tool found in Tools\net46\roslyn\tools.
//
// It produces an IL file (on stdout) containing tests
// for all possible integral ldind/conv combinations.

using System;
using System.Collections.Generic;
using System.IO;

class ILType
{
    public static readonly ILType I1 = new ILType(byteSize: 1, unsigned: false, min: 0xFFFF_FFFF_FFFF_FF80, max: 0x7F);
    public static readonly ILType U1 = new ILType(byteSize: 1, unsigned: true, min: 0, max: 0xFF);
    public static readonly ILType I2 = new ILType(byteSize: 2, unsigned: false, min: 0xFFFF_FFFF_FFFF_8000, max: 0x7FFF);
    public static readonly ILType U2 = new ILType(byteSize: 2, unsigned: true, min: 0, max: 0xFFFF);
    public static readonly ILType I4 = new ILType(byteSize: 4, unsigned: false, min: 0xFFFF_FFFF_8000_0000, max: 0x7FFF_FFFF);
    public static readonly ILType U4 = new ILType(byteSize: 4, unsigned: true, min: 0, max: 0xFFFF_FFFF);
    public static readonly ILType I8 = new ILType(byteSize: 8, unsigned: false, min: 0x8000_0000_0000_0000, max: 0x7FFF_FFFF_FFFF_FFFF);
    public static readonly ILType U8 = new ILType(byteSize: 8, unsigned: true, min: 0, max: 0xFFFF_FFFF_FFFF_FFFF);

    public static readonly IEnumerable<ILType> Types = new[] { I1, U1, I2, U2, I4, U4, I8, U8 };

    public readonly int ByteSize;
    public readonly bool IsUnsigned;
    public readonly ulong Min;
    public readonly ulong Max;

    ILType(int byteSize, bool unsigned, ulong min, ulong max)
    {
        ByteSize = byteSize;
        IsUnsigned = unsigned;
        Min = min;
        Max = max;
    }

    public int BitSize => ByteSize * 8;
    public bool IsSigned => !IsUnsigned;
    public bool IsSmall => ByteSize < 4;
    public bool IsSmallUnsigned => IsSmall && IsUnsigned;
    public bool IsSmallSigned => IsSmall && IsSigned;

    public string Name => $"{(IsUnsigned ? "u" : "")}int{BitSize}";
    public string ShortName => $"{(IsUnsigned ? "u" : "i")}{ByteSize}";

    public ILType ActualType => ByteSize <= 4 ? I4 : I8;

    public ILType UnsignedType
    {
        get
        {
            switch (ByteSize)
            {
            case 1:
                return U1;
            case 2:
                return U2;
            case 4:
                return U4;
            case 8:
                return U8;
            default:
                throw new Exception();
            }
        }
    }

    public ILType SignedType
    {
        get
        {
            switch (ByteSize)
            {
            case 1:
                return I1;
            case 2:
                return I2;
            case 4:
                return I4;
            case 8:
                return I8;
            default:
                throw new Exception();
            }
        }
    }

    public ulong AllOnes => UnsignedType.Max;

    public string ILConst(ulong value) => $"ldc.i{ActualType.ByteSize} {Hex(value)}";

    public string Hex(ulong value)
    {
        string hex = value.ToString("X16");

        if (hex.Length > ByteSize * 2)
        {
            hex = hex.Substring(hex.Length - ByteSize * 2);
        }

        return "0x" + hex;
    }
}

class ILConv
{
    public static IEnumerable<ILConv> Conversions
    {
        get
        {
            foreach (ILType type in ILType.Types)
            {
                yield return new ILConv(type, false, false);
                yield return new ILConv(type, true, false);
                yield return new ILConv(type, true, true);
            }
        }
    }

    public readonly ILType Type;
    public readonly bool Ovf;
    public readonly bool Un;

    ILConv(ILType type, bool overflow, bool unsigned)
    {
        Type = type;
        Ovf = overflow;
        Un = unsigned;
    }

    public string IL => $"conv.{(Ovf ? "ovf." : "")}{Type.ShortName}{(Ovf & Un ? ".un" : "")}";

    static ulong SignExtend(ulong value, ILType valueType)
    {
        if ((value & valueType.Min) != 0)
            value |= ~valueType.AllOnes;
        return value;
    }

    static ulong ZeroExtend(ulong value, ILType valueType)
    {
        return value & valueType.AllOnes;
    }

    static ulong Widen(ulong value, ILType valueType)
    {
        return valueType.IsSigned ? SignExtend(value, valueType) : ZeroExtend(value, valueType);
    }

    public ulong? Eval(ulong value, ILType valueType)
    {
        valueType = valueType.ActualType;

        if (Ovf)
        {
            if (Un)
            {
                valueType = valueType.UnsignedType;
            }

            value = Widen(value, valueType);

            if (valueType.IsSigned)
            {
                if ((long)value < (long)Type.Min)
                    return null;
            }

            if (Type.IsUnsigned)
            {
                if (value > Type.Max)
                    return null;
            }
            else
            {
                if (valueType.IsSigned)
                {
                    if ((long)value > (long)Type.Max)
                        return null;
                }
                else
                {
                    if (value > Type.Max)
                        return null;
                }
            }
        }

        if (Type.ByteSize <= 4)
        {
            value &= Type.AllOnes;

            if (Type.IsSmallSigned)
            {
                value = SignExtend(value, Type);
            }
        }
        else
        {
            value &= valueType.AllOnes;

            if (valueType == ILType.I4 && Type == ILType.I8)
            {
                if ((value & valueType.Min) != 0)
                {
                    value |= ~valueType.AllOnes;
                }
            }
        }

        return value;
    }
}

class ILLdInd
{
    public readonly ILType Type;

    public ILLdInd(ILType type)
    {
        Type = type;
    }

    public ulong Eval(ulong bits)
    {
        bits &= Type.AllOnes;

        if (Type.IsSmallSigned && ((bits & Type.Min) != 0))
        {
            bits |= ~Type.AllOnes;
        }

        return bits;
    }

    public string IL => $"ldind.{Type.ShortName}";
}

class ILStInd
{
    public readonly ILType Type;

    public ILStInd(ILType type)
    {
        Type = type;
    }

    public string IL => $"stind.{Type.ShortName}";
}

class Test
{
    public readonly ILLdInd Load;
    public readonly ILConv Conv;
    public readonly ILStInd Store;

    public string Name => $"{Load.IL}_{Conv.IL}".Replace('.', '_');

    public Test(ILLdInd load, ILConv conv)
    {
        Load = load;
        Conv = conv;
        Store = new ILStInd(conv.Type.ActualType);
    }

    public void WriteConvMethod(TextWriter writer)
    {
        writer.WriteLine();
        writer.WriteLine($"  .method private hidebysig static void Test_{Name}({Load.Type.Name}& src, {Store.Type.Name}& dst) cil managed noinlining");
        writer.WriteLine($"  {{");
        writer.WriteLine($"    .maxstack 4");
        writer.WriteLine($"    ldarg.1");
        writer.WriteLine($"    ldarg.0");
        writer.WriteLine($"    {Load.IL}");
        writer.WriteLine($"    {Conv.IL}");
        writer.WriteLine($"    {Store.IL}");
        writer.WriteLine($"    ret");
        writer.WriteLine($"  }}");
    }

    public void WriteCheckMethod(TextWriter writer)
    {
        writer.WriteLine();
        writer.WriteLine($"  .method private hidebysig static int32 Check_{Name}({Load.Type.ActualType.Name} input, {Store.Type.Name} expected, class [System.Private.CoreLib]System.String desc) cil managed noinlining");
        writer.WriteLine($"  {{");
        writer.WriteLine($"    .maxstack 4");
        writer.WriteLine($"    .locals init({Load.Type.Name} src, {Store.Type.Name} dst)");
        writer.WriteLine($"    ldarg.2");
        writer.WriteLine($"    call void Program::print(class [System.Private.CoreLib]System.String)");
        writer.WriteLine($"    ldarg.0");
        writer.WriteLine($"    stloc 0");
        writer.WriteLine($"    ldloca 0");
        writer.WriteLine($"    ldloca 1");
        writer.WriteLine($"    call void Program::Test_{Name}({Load.Type.Name}&, {Store.Type.Name}&)");
        writer.WriteLine($"    ldloc.1");
        writer.WriteLine($"    ldarg.1");
        writer.WriteLine($"    ceq");
        writer.WriteLine($"    ret");
        writer.WriteLine($"  }}");
    }

    public void WriteCheckOverflowMethod(TextWriter writer)
    {
        writer.WriteLine();
        writer.WriteLine($"  .method private hidebysig static int32 CheckOvf_{Name}({Load.Type.ActualType.Name} input, class [System.Private.CoreLib]System.String desc) cil managed noinlining");
        writer.WriteLine($"  {{");
        writer.WriteLine($"    .maxstack 4");
        writer.WriteLine($"    .locals init({Load.Type.Name} src, {Store.Type.Name} dst, int32 ovf)");
        writer.WriteLine($"    ldarg.1");
        writer.WriteLine($"    call void Program::print(class [System.Private.CoreLib]System.String)");
        writer.WriteLine($"    ldarg.0");
        writer.WriteLine($"    stloc 0");
        writer.WriteLine($"    .try {{");
        writer.WriteLine($"    ldloca 0");
        writer.WriteLine($"    ldloca 1");
        writer.WriteLine($"    call void Program::Test_{Name}({Load.Type.Name}&, {Store.Type.Name}&)");
        writer.WriteLine($"    leave END");
        writer.WriteLine($"    }} catch [System.Private.CoreLib]System.OverflowException {{");
        writer.WriteLine($"    ldc.i4 1");
        writer.WriteLine($"    stloc 2");
        writer.WriteLine($"    leave END");
        writer.WriteLine($"    }}");
        writer.WriteLine($"    END: ldloc.2");
        writer.WriteLine($"    ret");
        writer.WriteLine($"  }}");
    }

    public ulong? Eval(ulong value) => Conv.Eval(Load.Eval(value), Load.Type);
}

class TestInput
{
    public readonly ulong Input;
    public readonly ulong? Expected;

    public TestInput(Test test, ulong input)
    {
        Input = input;
        Expected = test.Eval(input);
    }
}

const string FileBeginIL = @"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Private.CoreLib { auto }
.assembly test { }

.class auto Program extends [System.Private.CoreLib]System.Object
{
  .method private static void print(class [System.Private.CoreLib]System.String) cil managed
  {
    .maxstack 1
    ldarg 0
    call void [System.Private.CoreLib]Internal.Console::WriteLine(class [System.Private.CoreLib]System.String)
    ret
  }";

const string MainMethodBeginIL = @"
  .method private hidebysig static int32 Main() cil managed
  {
    .entrypoint
    .maxstack 8";

const string MainMethodEndIL = @"
    ldc.i4 100
    ret

FAIL:
    ldstr ""FAILED""
    call void Program::print(class [System.Private.CoreLib]System.String)    
    ldc.i4 1
    ret
  }";

IEnumerable<Test> GenerateTests()
{
    var tests = new List<Test>();

    foreach (ILType loadType in ILType.Types)
    {
        foreach (ILConv conv in ILConv.Conversions)
        {
            tests.Add(new Test(new ILLdInd(loadType), conv));
        }
    }

    return tests;
}

IEnumerable<TestInput> GenerateTestInputs(Test test)
{
    var inputs = new List<TestInput>();

    inputs.Add(new TestInput(test, test.Load.Type.Max));

    if (test.Load.Type.IsSigned)
    {
        inputs.Add(new TestInput(test, test.Load.Type.Min));
        inputs.Add(new TestInput(test, test.Load.Type.AllOnes));
    }

    if (test.Conv.Type.ByteSize < test.Load.Type.ByteSize)
    {
        inputs.Add(new TestInput(test, test.Conv.Type.Max));

        if (test.Conv.Type.IsSigned)
        {
            inputs.Add(new TestInput(test, test.Conv.Type.Min));
            inputs.Add(new TestInput(test, test.Conv.Type.AllOnes));
        }
    }

    return inputs;
}

void WriteMainMethod(TextWriter write, IEnumerable<Test> tests)
{
    writer.WriteLine(MainMethodBeginIL);

    foreach (Test t in tests)
    {
        foreach (TestInput i in GenerateTestInputs(t))
        {
            writer.WriteLine();
            writer.WriteLine($"    {t.Load.Type.ActualType.ILConst(i.Input)}");

            if (i.Expected == null)
            {
                writer.WriteLine($"    ldstr \"Checking {t.Name}({t.Load.Type.Hex(i.Input)}) == OverflowException\"");
                writer.WriteLine($"    call int32 Program::CheckOvf_{t.Name}({t.Load.Type.ActualType.Name}, class [System.Private.CoreLib]System.String)");
            }
            else
            {
                writer.WriteLine($"    {t.Store.Type.ILConst(i.Expected.Value)}");
                writer.WriteLine($"    ldstr \"Checking {t.Name}({t.Load.Type.Hex(i.Input)}) == {t.Store.Type.Hex(i.Expected.Value)}\"");
                writer.WriteLine($"    call int32 Program::Check_{t.Name}({t.Load.Type.ActualType.Name}, {t.Store.Type.Name}, class [System.Private.CoreLib]System.String)");
            }

            writer.WriteLine($"    brfalse FAIL");
        }
    }

    writer.WriteLine(MainMethodEndIL);
}

void WriteTestMethods(TextWriter writer, IEnumerable<Test> tests)
{
    foreach (Test t in tests)
    {
        t.WriteConvMethod(writer);
        t.WriteCheckMethod(writer);

        if (t.Conv.Ovf)
        {
            t.WriteCheckOverflowMethod(writer);
        }
    }
}

var tests = GenerateTests();
var writer = Console.Out;
writer.WriteLine(FileBeginIL);
WriteMainMethod(writer, tests);
WriteTestMethods(writer, tests);
writer.WriteLine("}");
