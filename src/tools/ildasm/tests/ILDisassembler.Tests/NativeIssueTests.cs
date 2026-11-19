// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Xunit;

namespace ILDisassembler.Tests;

// Cases where the native ildasm produces output that does not reassemble to the original; the managed one must.
public class NativeIssueTests
{
    private const string Prolog = """
        .assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) .ver 10:0:0:0 }
        .assembly test { }
        .module test.dll

        """;

    private static string RoundTrip(string body, Options? options = null)
    {
        byte[] original = RoundTripTests.Assemble(Prolog + body);
        string il = RoundTripTests.Disassemble(original, options);
        byte[] reassembled = RoundTripTests.Assemble(il);
        Assert.Equal(DescribeBodies(original), DescribeBodies(reassembled));
        return il;
    }

    // The IL bytes and EH clauses of every method, in order.
    private static string DescribeBodies(byte[] image)
    {
        using PEReader pe = new(new MemoryStream(image));
        MetadataReader md = pe.GetMetadataReader();
        System.Text.StringBuilder sb = new();
        foreach (MethodDefinitionHandle h in md.MethodDefinitions)
        {
            MethodDefinition m = md.GetMethodDefinition(h);
            sb.Append(md.GetString(m.Name)).Append(':');
            if (m.RelativeVirtualAddress != 0)
            {
                MethodBodyBlock body = pe.GetMethodBody(m.RelativeVirtualAddress);
                sb.Append(Convert.ToHexString(body.GetILBytes()!));
                foreach (ExceptionRegion r in body.ExceptionRegions)
                {
                    sb.Append($" [{r.Kind} {r.TryOffset}+{r.TryLength} {r.HandlerOffset}+{r.HandlerLength} {r.FilterOffset} {(r.CatchType.IsNil ? "" : md.GetString(md.GetTypeReference((TypeReferenceHandle)r.CatchType).Name))}]");
                }
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }

    // dotnet/runtime#4873: two catch clauses on one try whose handlers are out of order were swapped.
    [Fact]
    public void CatchClausesKeepTheirOrder()
    {
        string il = RoundTrip("""
            .class public C extends [System.Runtime]System.Object
            {
              .method public static int32 M(int32 a, int32 b) cil managed
              {
                .locals (class [System.Runtime]System.Exception, int32)
                .maxstack 3
              try_start:
                ldarg 0
                ldarg 1
                div
                stloc.1
                leave.s try_end
              try_end:
                ldloc.1
                ret
              arithmetic:
                isinst [System.Runtime]System.ArithmeticException
                stloc 0
                leave AEEnd
              AEEnd:
                ldc.i4 0xAE
                ret
              divbyzero:
                isinst [System.Runtime]System.DivideByZeroException
                stloc 0
                leave DBZEnd
              DBZEnd:
                ldc.i4 0xDB0E
                ret
                .try try_start to try_end catch [System.Runtime]System.DivideByZeroException handler divbyzero to DBZEnd
                .try try_start to try_end catch [System.Runtime]System.ArithmeticException handler arithmetic to AEEnd
              }
            }
            """);
        Assert.Contains("catch [System.Runtime]System.DivideByZeroException handler", il);
    }

    [Fact]
    public void NestedStructuredHandlersStayStructured()
    {
        string il = RoundTrip("""
            .class public C extends [System.Runtime]System.Object
            {
              .method public static void M() cil managed
              {
                .try
                {
                  .try
                  {
                    leave.s L
                  }
                  catch [System.Runtime]System.Exception
                  {
                    pop
                    leave.s L
                  }
                }
                finally
                {
                  endfinally
                }
              L:
                ret
              }
            }
            """);
        Assert.Contains("  // end handler", il);
        Assert.DoesNotContain("// Exception count", il);
    }

    // dotnet/runtime#38506: a branch into the middle of an instruction has no label to refer to.
    [Fact]
    public void BranchIntoAnInstructionUsesAnOffset()
    {
        string il = RoundTrip("""
            .class public C extends [System.Runtime]System.Object
            {
              .method public static void M() cil managed
              {
                ldc.i4 1
                pop
                br -7
                ret
              }
            }
            """);
        Assert.Contains("br         -7", il);
    }

    [Fact]
    public void UndefinedOpcodesAreEmittedAsBytes()
    {
        string il = RoundTrip("""
            .class public C extends [System.Runtime]System.Object
            {
              .method public static void M() cil managed
              {
                .emitbyte 0xee
                .emitbyte 0xfe
                .emitbyte 0x30
                ret
              }
            }
            """);
        Assert.Contains("IL_0000:  .emitbyte 0xee", il);
        Assert.Contains("IL_0001:  .emitbyte 0xfe .emitbyte 0x30", il);
    }

    // dotnet/runtime#72695
    [Fact]
    public void NoPrefixRoundTrips()
    {
        string il = RoundTrip("""
            .class public C extends [System.Runtime]System.Object
            {
              .method public static uint8 M(uint8[] a) cil managed
              {
                ldarg.0
                ldc.i4.0
                no. 6
                ldelem.u1
                ret
              }
            }
            """);
        Assert.Contains("no.        6", il);
    }

    // dotnet/runtime#53822
    [Fact]
    public void LineNumbersComeFromThePortablePdb()
    {
        string path = typeof(NativeIssueTests).Assembly.Location;
        using Disassembler disassembler = new(path, new Options { ShowLineNumbers = true });
        StringWriter writer = new();
        Assert.True(disassembler.Disassemble(writer, TextWriter.Null, outputFilePath: null));
        string il = writer.ToString();
        Assert.Contains(".line ", il);
        Assert.Contains("NativeIssueTests.cs'", il);
    }
    // dotnet/runtime#38507 (JIT/opt/Inline/regression/badcallee): a branch past the end of the code has no label to refer to.
    [Fact]
    public void BranchPastTheEndUsesAnOffset()
    {
        string il = RoundTrip("""
            .class public C extends [System.Runtime]System.Object
            {
              .method public static int32 M() cil managed
              {
                ldc.i4.0
                br.s 100
                ret
              }
            }
            """);
        Assert.Contains("br.s       100", il);
    }

    // JIT/Directed/RVAInit: the native ildasm sizes the data of a struct field by the sum of its fields,
    // dropping the padding and truncating the data.
    [Fact]
    public void FieldDataOfAPaddedStructKeepsItsSize()
    {
        byte[] original = RoundTripTests.Assemble(Prolog + """
            .class public sequential ansi sealed S extends [System.Runtime]System.ValueType
            {
              .field public uint8 a
              .field public int32 b
              .field public int64 c
            }
            .class public auto ansi C extends [System.Runtime]System.Object
            {
              .field public static valuetype S D at D_1
            }
            .data D_1 = bytearray(01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10)
            """);
        string il = RoundTripTests.Disassemble(original);
        Assert.Contains("01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10", il);
        byte[] reassembled = RoundTripTests.Assemble(il);
        Assert.Equal(FieldData(original, 16), FieldData(reassembled, 16));
    }

    // A custom-attribute string with an embedded NUL cannot be written verbally; the raw blob is kept instead.
    [Fact]
    public void VerbalCustomAttributeStringWithNulKeepsTheBlob()
    {
        byte[] original = RoundTripTests.Assemble(Prolog + """
            .class public A extends [System.Runtime]System.Attribute
            {
              .method public specialname rtspecialname instance void .ctor(string s) cil managed { ret }
            }
            .class public C extends [System.Runtime]System.Object
            {
              .custom instance void A::.ctor(string) = ( 01 00 03 61 00 62 00 00 )
            }
            """);
        string il = RoundTripTests.Disassemble(original, new Options { CustomAttributesVerbal = true });
        Assert.DoesNotContain("string('a')", il);
        Assert.Equal(CustomAttributeValues(original), CustomAttributeValues(RoundTripTests.Assemble(il)));
    }

    // A comma in a quoted name after an escaped quote is not a parameter separator for the long-signature split.
    [Fact]
    public void SignatureSplitSkipsEscapesInQuotedNames()
    {
        string il = RoundTrip("""
            .class public C extends [System.Runtime]System.Object
            {
              .method public static void Callee(class [System.Runtime]'it\'s,a' x, int32 y) cil managed { ret }
              .method public static void M() cil managed
              {
                ldnull
                ldc.i4.0
                call void C::Callee(class [System.Runtime]'it\'s,a', int32)
                ret
              }
            }
            """);
        Assert.Contains("'it\\'s,a'", il);
    }

    // A long P/Invoke name is split into chunks on a UTF-8 character boundary.
    [Fact]
    public void LongPInvokeNameIsNotSplitInsideACharacter()
    {
        string name = new string('x', 79) + "\u00e9abc";
        string il = RoundTrip($$"""
            .class public C extends [System.Runtime]System.Object
            {
              .method public static pinvokeimpl("lib" as "{{name}}" winapi) void M() cil managed preservesig { }
            }
            """);
        Assert.DoesNotContain("\uFFFD", il);
        using PEReader pe = new(new MemoryStream(RoundTripTests.Assemble(il)));
        MetadataReader md = pe.GetMetadataReader();
        Assert.Equal(name, md.MethodDefinitions.Select(h => md.GetString(md.GetMethodDefinition(h).GetImport().Name)).Single());
    }

    // Exported types forwarded to a duplicate assembly reference use the alias of that reference.
    [Fact]
    public void ExportedTypeUsesTheAssemblyReferenceAlias()
    {
        byte[] original = RoundTripTests.Assemble(Prolog + """
            .assembly extern Lib { .ver 1:0:0:0 }
            .assembly extern Lib as Lib2 { .ver 2:0:0:0 }
            .class extern forwarder N.T { .assembly extern Lib2 }
            """);
        string il = RoundTripTests.Disassemble(original);
        Assert.Contains(".assembly extern Lib_2", il);
        byte[] reassembled = RoundTripTests.Assemble(il);
        Assert.Equal(ExportedTypeTargets(original), ExportedTypeTargets(reassembled));
    }

    // /HEADERS dumps the headers of a PE file without metadata.
    [Fact]
    public void HeadersOfAFileWithoutMetadata()
    {
        BlobBuilder image = new();
        new NativePEBuilder().Serialize(image);
        using Disassembler disassembler = new(new MemoryStream(image.ToArray()), new Options { Headers = true });
        StringWriter writer = new();
        StringWriter error = new();
        Assert.False(disassembler.Disassemble(writer, error, outputFilePath: null));
        Assert.Contains("// ----- DOS Header:", writer.ToString());
        Assert.Contains("has no valid CLR header", error.ToString());
    }

    private sealed class NativePEBuilder() : PEBuilder(PEHeaderBuilder.CreateLibraryHeader(), deterministicIdProvider: null)
    {
        protected override System.Collections.Immutable.ImmutableArray<Section> CreateSections()
            => [new Section(".text", SectionCharacteristics.ContainsCode | SectionCharacteristics.MemRead | SectionCharacteristics.MemExecute)];

        protected override PEDirectoriesBuilder GetDirectories() => new();

        protected override BlobBuilder SerializeSection(string name, SectionLocation location)
        {
            BlobBuilder b = new();
            b.WriteBytes(0xCC, 16);
            return b;
        }
    }

    private static string FieldData(byte[] image, int size)
    {
        using PEReader pe = new(new MemoryStream(image));
        MetadataReader md = pe.GetMetadataReader();
        FieldDefinition f = md.FieldDefinitions.Select(md.GetFieldDefinition).Single(f => f.GetRelativeVirtualAddress() != 0);
        return Convert.ToHexString(pe.GetSectionData(f.GetRelativeVirtualAddress()).GetContent(0, size).AsSpan());
    }

    private static string[] CustomAttributeValues(byte[] image)
    {
        using PEReader pe = new(new MemoryStream(image));
        MetadataReader md = pe.GetMetadataReader();
        return md.CustomAttributes.Select(h => Convert.ToHexString(md.GetBlobBytes(md.GetCustomAttribute(h).Value))).ToArray();
    }

    private static string[] ExportedTypeTargets(byte[] image)
    {
        using PEReader pe = new(new MemoryStream(image));
        MetadataReader md = pe.GetMetadataReader();
        List<string> targets = new();
        foreach (ExportedTypeHandle h in md.ExportedTypes)
        {
            ExportedType t = md.GetExportedType(h);
            AssemblyReference r = md.GetAssemblyReference((AssemblyReferenceHandle)t.Implementation);
            targets.Add($"{md.GetString(t.Namespace)}.{md.GetString(t.Name)} -> {md.GetString(r.Name)} {r.Version}");
        }

        return targets.ToArray();
    }
}
