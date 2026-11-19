// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
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
}
