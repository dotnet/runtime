// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.DotNet.ILAssembler;
using Xunit;

namespace ILDisassembler.Tests;

// Assembles IL with the managed ilasm, disassembles it, reassembles the disassembly and disassembles that again:
// the two disassemblies must be identical, and the first one must contain the expected lines.
public class RoundTripTests
{
    private const string Prolog = """
        .assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) .ver 10:0:0:0 }
        .assembly test { .ver 1:2:3:4 }
        .module test.dll

        """;

    internal static byte[] Assemble(string source)
    {
        DocumentCompiler compiler = new();
        (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = compiler.Compile(
            new SourceText(source, "test.il"),
            _ => throw new InvalidOperationException("no includes"),
            _ => null,
            new Microsoft.DotNet.ILAssembler.Options { Dll = true, Deterministic = true });
        Assert.True(result is not null && diagnostics.All(d => d.Severity != DiagnosticSeverity.Error),
            "Assembly failed:\n" + string.Join("\n", diagnostics.Select(d => d.ToString())) + "\n--- source ---\n" + source);
        BlobBuilder builder = new();
        result!.Serialize(builder);
        return builder.ToArray();
    }

    internal static string Disassemble(byte[] image, Options? options = null)
    {
        using Disassembler disassembler = new(new MemoryStream(image), options);
        StringWriter writer = new();
        Assert.True(disassembler.Disassemble(writer, TextWriter.Null, outputFilePath: null));
        return writer.ToString();
    }

    // The MVID is derived from the image contents, which may legitimately differ between the two assemblies.
    private static string Normalize(string il)
        => string.Join("\n", il.Split('\n').Where(l => !l.TrimStart().StartsWith("// MVID:", StringComparison.Ordinal)));

    public static TheoryData<string, string, string[]> Cases { get; } = new()
    {
        {
            "methods and locals",
            """
            .class public auto ansi beforefieldinit C extends [System.Runtime]System.Object
            {
              .method public hidebysig static int32 Add(int32 a, int32 b) cil managed
              {
                .maxstack 2
                .locals init (int32 V_0, string V_1)
                ldarg.0
                ldarg.1
                add
                stloc.0
                ldloc.0
                ret
              }
              .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
              {
                ldarg.0
                call instance void [System.Runtime]System.Object::.ctor()
                ret
              }
            }
            """,
            [
                ".method public hidebysig static int32  Add(int32 a,",
                "                                          int32 b) cil managed",
                "    .locals init (int32 V_0,",
                "             string V_1)",
                "    IL_0000:  ldarg.0",
                "    IL_0002:  add",
                "    IL_0005:  ret",
                "    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()",
                "  } // end of method C::Add",
                "} // end of class C",
            ]
        },
        {
            "generics, constraints and variance",
            """
            .class interface public abstract auto ansi I`1<+ T> { }
            .class public auto ansi beforefieldinit G`2<class (class I`1<!U>) T, valuetype .ctor U> extends [System.Runtime]System.Object
            {
              .field private !0 f
              .method public hidebysig instance !!M M<M>(!T t, !!M m) cil managed
              {
                ldarg.2
                ret
              }
            }
            """,
            [
                ".class interface public abstract auto ansi I`1<+ T>",
                ".class public auto ansi beforefieldinit G`2<class (class I`1<!U>) T,valuetype .ctor U>",
                "  .field private !T f",
                ".method public hidebysig instance !!M  M<M>(!T t,",
            ]
        },
        {
            "structured exception handling",
            """
            .class public auto ansi C extends [System.Runtime]System.Object
            {
              .method public static void M() cil managed
              {
                .try
                {
                  .try
                  {
                    nop
                    leave.s L1
                  }
                  catch [System.Runtime]System.Exception
                  {
                    pop
                    leave.s L1
                  }
                }
                finally
                {
                  endfinally
                }
              L1:
                .try
                {
                  leave.s L2
                }
                filter
                {
                  pop
                  ldc.i4.1
                  endfilter
                }
                {
                  pop
                  leave.s L2
                }
              L2:
                .try
                {
                  leave.s L3
                }
                fault
                {
                  endfinally
                }
              L3:
                ret
              }
            }
            """,
            [
                "    .try",
                "      catch [System.Runtime]System.Exception ",
                "      }  // end handler",
                "    finally",
                "    filter",
                "    }  // end filter",
                "    {  // handler",
                "    fault",
            ]
        },
        {
            "constants, strings and floating point",
            """
            .class public auto ansi C extends [System.Runtime]System.Object
            {
              .field public static literal int32 I = int32(0x0000002A)
              .field public static literal float64 D = float64(0.5)
              .field public static literal float32 F = float32(1.5)
              .field public static literal string S = "a\"b"
              .field public static literal bool B = bool(true)
              .field public static literal char Ch = char(0x0041)
              .field public static literal object O = nullref
              .method public static void M() cil managed
              {
                ldstr "hello\tworld"
                pop
                ldstr bytearray (E9 00)
                pop
                ldc.r4 1.5
                pop
                ldc.r8 0.1
                pop
                ldc.r8 (00 00 00 00 00 00 F8 7F)
                pop
                ldc.i8 0x123456789
                pop
                ldc.i4 -1
                pop
                ldc.i4.s -2
                pop
                ret
              }
            }
            """,
            [
                ".field public static literal int32 I = int32(0x0000002A)",
                ".field public static literal float64 D = float64(0.50000000000000000)",
                ".field public static literal float32 F = float32(1.5000000)",
                ".field public static literal string S = \"a\\\"b\"",
                ".field public static literal bool B = bool(true)",
                ".field public static literal char Ch = char(0x0041)",
                ".field public static literal object O = nullref",
                "ldstr      \"hello\\tworld\"",
                "ldstr      bytearray (E9 00 )",
                "ldc.r4     1.5",
                "ldc.r8     0.10000000000000001",
                "ldc.r8     (00 00 00 00 00 00 F8 7F)",
                "ldc.i8     0x123456789",
                "ldc.i4     0xffffffff",
                "ldc.i4.s   -2",
            ]
        },
        {
            "branches and switch",
            """
            .class public auto ansi C extends [System.Runtime]System.Object
            {
              .method public static int32 M(int32 x) cil managed
              {
                ldarg.0
                switch (L0, L1)
                br L2
              L0:
                ldc.i4.0
                ret
              L1:
                ldc.i4.1
                ret
              L2:
                ldc.i4.2
                ret
              }
            }
            """,
            [
                "    IL_0001:  switch     ( ",
                "                          IL_0013,",
                "                          IL_0015)",
                "    IL_000e:  br         IL_0017",
            ]
        },
        {
            "properties, events and overrides",
            """
            .class public auto ansi C extends [System.Runtime]System.Object implements [System.Runtime]System.IDisposable
            {
              .method public hidebysig specialname instance int32 get_P() cil managed { ldc.i4.0 ret }
              .method public hidebysig specialname instance void set_P(int32 v) cil managed { ret }
              .method public hidebysig specialname instance void add_E(class [System.Runtime]System.EventHandler h) cil managed { ret }
              .method public hidebysig specialname instance void remove_E(class [System.Runtime]System.EventHandler h) cil managed { ret }
              .method private hidebysig newslot virtual final instance void Dispose() cil managed
              {
                .override [System.Runtime]System.IDisposable::Dispose
                ret
              }
              .property instance int32 P()
              {
                .get instance int32 C::get_P()
                .set instance void C::set_P(int32)
              }
              .event [System.Runtime]System.EventHandler E
              {
                .addon instance void C::add_E(class [System.Runtime]System.EventHandler)
                .removeon instance void C::remove_E(class [System.Runtime]System.EventHandler)
              }
            }
            """,
            [
                "       implements [System.Runtime]System.IDisposable",
                "    .override [System.Runtime]System.IDisposable::Dispose",
                "  .property instance callconv(8) int32 P()",
                "    .get instance int32 C::get_P()",
                "  } // end of property C::P",
                "  .event [System.Runtime]System.EventHandler E",
                "    .addon instance void C::add_E(class [System.Runtime]System.EventHandler)",
                "  } // end of event C::E",
            ]
        },
        {
            "nested types and quoted names",
            """
            .class public auto ansi Outer extends [System.Runtime]System.Object
            {
              .class nested public auto ansi 'Inner Class' extends [System.Runtime]System.Object
              {
                .field public static int32 'add'
                .field public static int32 '.hidden'
              }
            }
            """,
            [
                "  .class auto ansi nested public 'Inner Class'",
                "    .field public static int32 'add'",
                "    .field public static int32 '.hidden'",
                "  } // end of class 'Inner Class'",
            ]
        },
        {
            "custom attributes, params and marshaling",
            """
            .class public auto ansi C extends [System.Runtime]System.Object
            {
              .custom instance void [System.Runtime]System.ObsoleteAttribute::.ctor(string) = ( 01 00 02 68 69 00 00 )
              .method public static pinvokeimpl("lib.so" as "entry" winapi lasterr)
                      int32 marshal(bool) P([in] string marshal(lpwstr) s, [opt] int32 x) cil managed preservesig
              {
                .param [2] = int32(0x00000005)
              }
            }
            """,
            [
                ".custom instance void [System.Runtime]System.ObsoleteAttribute::.ctor(string) = ( 01 00 02 68 69 00 00 ) ",
                "pinvokeimpl(\"lib.so\" as \"entry\" lasterr winapi)",
                "int32  marshal( bool)  P([in] string  marshal( lpwstr) s,",
                "    .param [2] = int32(0x00000005)",
            ]
        },
        {
            "explicit layout and field data",
            """
            .class public explicit ansi sealed S extends [System.Runtime]System.ValueType
            {
              .pack 1
              .size 8
              .field [0] public int32 a
              .field [4] public int32 b
            }
            .class public auto ansi C extends [System.Runtime]System.Object
            {
              .field public static int32 D at D_1
            }
            .data D_1 = int32(42)
            """,
            [
                "  .pack 1",
                "  .size 8",
                "  .field [0] public int32 a",
                "  .field public static int32 D at D_",
                ".data D_",
            ]
        },
        {
            "calli, function pointers and tokens",
            """
            .class public auto ansi C extends [System.Runtime]System.Object
            {
              .field public static method int32 *(int32) fp
              .method public static void M() cil managed
              {
                ldsfld method int32 *(int32) C::fp
                ldc.i4.0
                calli int32(int32)
                pop
                ldtoken method void C::M()
                pop
                ldtoken field method int32 *(int32) C::fp
                pop
                ldtoken [System.Runtime]System.String
                pop
                ret
              }
            }
            """,
            [
                ".field public static method int32 *(int32) fp",
                "calli      int32(int32)",
                "ldtoken    method void C::M()",
                "ldtoken    field method int32 *(int32) C::fp",
                "ldtoken    [System.Runtime]System.String",
            ]
        },
        {
            "global members and module references",
            """
            .module extern native.dll
            .field static assembly int32 g
            .method public static void Global() cil managed
            {
              ldsfld int32 g
              pop
              ret
            }
            """,
            [
                ".module extern native.dll",
                "// ================== GLOBAL FIELDS ==========================",
                ".field static assembly int32 g",
                "// ================== GLOBAL METHODS =========================",
                "} // end of global method Global",
            ]
        },
        {
            "arrays and generic instantiations",
            """
            .class public auto ansi C extends [System.Runtime]System.Object
            {
              .method public static void M() cil managed
              {
                .locals init (int32[0...,0...] V_0, class [System.Runtime]System.Collections.Generic.List`1<int32[]> V_1, int32& V_2, int32* V_3)
                ldc.i4.1
                ldc.i4.1
                newobj instance void int32[0...,0...]::.ctor(int32, int32)
                stloc.0
                ret
              }
            }
            """,
            [
                ".locals init (int32[0...,0...] V_0,",
                "             class [System.Runtime]System.Collections.Generic.List`1<int32[]> V_1,",
                "             int32& V_2,",
                "             int32* V_3)",
                "newobj     instance void int32[0...,0...]::.ctor(int32,",
            ]
        },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void DisassemblyRoundTrips(string name, string body, string[] expectedLines)
    {
        _ = name;
        string first = Disassemble(Assemble(Prolog + body));
        foreach (string expected in expectedLines)
        {
            Assert.True(first.Contains(expected, StringComparison.Ordinal), $"Missing:\n{expected}\n--- disassembly ---\n{first}");
        }

        string second = Disassemble(Assemble(first));
        Assert.Equal(Normalize(first), Normalize(second));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void RawExceptionHandlingRoundTrips(string name, string body, string[] expectedLines)
    {
        _ = name;
        _ = expectedLines;
        Options options = new() { RawExceptionHandling = true, ShowBytes = true, ShowTokens = true, CustomAttributesVerbal = true };
        string first = Disassemble(Assemble(Prolog + body), options);
        string second = Disassemble(Assemble(first), options);
        Assert.Equal(Normalize(first), Normalize(second));
    }

    [Fact]
    public void RawExceptionHandlingListsClausesAfterTheCode()
    {
        string il = Disassemble(Assemble(Prolog + (string)Cases.First(c => (string)c[0] == "structured exception handling")[1]),
            new Options { RawExceptionHandling = true });
        Assert.Contains("    // Exception count 4", il);
        Assert.Contains(".try IL_0000 to IL_0003 catch [System.Runtime]System.Exception handler IL_0003 to IL_0006", il);
        Assert.Contains(" filter IL_", il);
        Assert.Contains(" fault handler IL_", il);
    }

    [Fact]
    public void SelfRoundTrips()
    {
        // A real compiler-produced assembly: this test assembly. The original carries a DebuggableAttribute, which the
        // disassembly comments out (ilasm adds its own), so the comparison is between the first and second reassembly.
        byte[] image = File.ReadAllBytes(typeof(RoundTripTests).Assembly.Location);
        string first = Disassemble(image);
        Assert.Contains("// --- The following custom attribute is added automatically, do not uncomment -------", first);
        string second = Disassemble(Assemble(first));
        string third = Disassemble(Assemble(second));
        Assert.Equal(Normalize(second), Normalize(third));
    }
}
