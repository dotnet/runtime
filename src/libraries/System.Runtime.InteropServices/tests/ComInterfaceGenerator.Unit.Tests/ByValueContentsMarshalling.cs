// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Xunit;
using static Microsoft.Interop.UnitTests.TestUtils;
using StringMarshalling = System.Runtime.InteropServices.StringMarshalling;
using VerifyComInterfaceGenerator = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.ComInterfaceGenerator>;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class ByValueContentsMarshalling
    {
        private static IComInterfaceAttributeProvider GetAttributeProvider(GeneratorKind generator)
            => generator switch
            {
                GeneratorKind.VTableIndexStubGenerator => new VirtualMethodIndexAttributeProvider(),
                GeneratorKind.ComInterfaceGeneratorManagedObjectWrapper => new GeneratedComInterfaceAttributeProvider(System.Runtime.InteropServices.Marshalling.ComInterfaceOptions.ManagedObjectWrapper),
                GeneratorKind.ComInterfaceGeneratorComObjectWrapper => new GeneratedComInterfaceAttributeProvider(System.Runtime.InteropServices.Marshalling.ComInterfaceOptions.ComObjectWrapper),
                GeneratorKind.ComInterfaceGenerator => new GeneratedComInterfaceAttributeProvider(),
                _ => throw new UnreachableException(),
            };

        public static IEnumerable<object[]> ByValueMarshalAttributeOnValueTypes()
        {
            var codeSnippets = new CodeSnippets(GetAttributeProvider(GeneratorKind.ComInterfaceGenerator));
            const string In = "[{|#1:InAttribute|}]";
            const string Out = "[{|#2:OutAttribute|}]";
            const string paramName = "p";
            const string MarshalAsU4 = "[MarshalAs(UnmanagedType.U4)]";
            const string MarshalAsU2 = "[MarshalAs(UnmanagedType.U2)]";

            string p = $$"""{|#0:{{paramName}}|}""";
            var diagnostic = new DiagnosticResult(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails);
            var outAttributeNotSupported = diagnostic
                    .WithLocation(0)
                    .WithArguments(SR.OutAttributeNotSupportedOnByValueParameters, paramName);
            var inAttributeIsNotSupported = diagnostic
                    .WithLocation(0)
                    .WithArguments(SR.InAttributeNotSupportedOnByValueParameters, paramName);
            var inOutAttributeNotSupported = diagnostic
                    .WithLocation(0)
                    .WithArguments(SR.InOutAttributeNotSupportedOnByValueParameters, paramName);

            DiagnosticResult[] InIsNotSupported = [inAttributeIsNotSupported];
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In, "int", p), InIsNotSupported };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In, "byte", p), InIsNotSupported };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + MarshalAsU4, "bool", p), InIsNotSupported };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + MarshalAsU2, "char", p), InIsNotSupported };

            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In, "string", p, (StringMarshalling.Utf8, null)), InIsNotSupported };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In, "IntClass", p) + CodeSnippets.IntClassAndMarshaller, InIsNotSupported };

            DiagnosticResult[] OutIsNotSupported = [outAttributeNotSupported];
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Out, "int", p), OutIsNotSupported };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Out, "IntStruct", p) + CodeSnippets.IntStructAndMarshaller, OutIsNotSupported };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Out + MarshalAsU4, "bool", p), OutIsNotSupported };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Out + MarshalAsU2, "char", p), OutIsNotSupported };

            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Out, "string", p, (StringMarshalling.Utf8, null)), OutIsNotSupported };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Out, "IntClass", p) + CodeSnippets.IntClassAndMarshaller, OutIsNotSupported };

            DiagnosticResult[] InOutIsNotSupported = [inOutAttributeNotSupported];
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Out, "int", p), InOutIsNotSupported };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Out, "IntStruct", p) + CodeSnippets.IntStructAndMarshaller, InOutIsNotSupported };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Out + MarshalAsU4, "bool", p), InOutIsNotSupported };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Out + MarshalAsU2, "char", p), InOutIsNotSupported };

            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Out, "string", p, (StringMarshalling.Utf8, null)), InOutIsNotSupported };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Out, "IntClass", p) + CodeSnippets.IntClassAndMarshaller, InOutIsNotSupported };

            // Any ref keyword is okay for non-collection types
            DiagnosticResult[] None = [];
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType("out", "IntStruct", p) + CodeSnippets.IntStructAndMarshaller, None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType("out", "byte", p), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(MarshalAsU4 + "out", "bool", p), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(MarshalAsU2 + "out", "char", p), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType("out", "string", p, (StringMarshalling.Utf8, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType("out", "IntClass", p) + CodeSnippets.IntClassAndMarshaller, None };

            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType("in", "IntStruct", p) + CodeSnippets.IntStructAndMarshaller, None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType("in", "byte", p), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(MarshalAsU4 + "in", "bool", p), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(MarshalAsU2 + "in", "char", p), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType("in", "string", p, (StringMarshalling.Utf8, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType("in", "IntClass", p) + CodeSnippets.IntClassAndMarshaller, None };

            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType("ref", "IntStruct", p) + CodeSnippets.IntStructAndMarshaller, None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType("ref", "byte", p), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(MarshalAsU4 + "ref", "bool", p), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(MarshalAsU2 + "ref", "char", p), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType("ref", "string", p, (StringMarshalling.Utf8, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType("ref", "IntClass", p) + CodeSnippets.IntClassAndMarshaller, None };
        }

        public static IEnumerable<object[]> ByValueMarshalAttributeOnPinnedMarshalledTypes()
        {
            var codeSnippets = new CodeSnippets(GetAttributeProvider(GeneratorKind.ComInterfaceGenerator));
            const string In = "[{|#1:InAttribute|}]";
            const string Out = "[{|#2:OutAttribute|}]";
            const string paramName = "p";
            string p = $$"""{|#0:{{paramName}}|}""";
            const string Count = @"[MarshalUsing(ConstantElementCount = 10)]";
            const string MarshalAsBoolArray = "[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeConst = 10)]";
            const string MarshalUsingIntMarshaller = "[MarshalUsing(typeof(IntMarshaller), ElementIndirectionDepth = 1)]";
            const string MarshalUsingIntStructMarshaller = "[MarshalUsing(typeof(IntStructMarshaller), ElementIndirectionDepth = 1)]";
            const string MarshalUsingIntClassMarshaller = "[MarshalUsing(typeof(IntClassMarshaller), ElementIndirectionDepth = 1)]";

            // Any explicit [In] or [Out] on an array is preferred and should not warn
            DiagnosticResult[] None = [];
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Count, "int[]", p), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Count, "char[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + MarshalAsBoolArray, "bool[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + MarshalUsingIntMarshaller + Count, "int[]", p) + CodeSnippets.IntMarshaller, None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Count, "string[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Count, "string[]", p, (StringMarshalling.Utf8, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Count + MarshalUsingIntStructMarshaller, "IntStruct[]", p) + CodeSnippets.IntStructAndMarshaller, None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Count + MarshalUsingIntClassMarshaller, "IntClass[]", p) + CodeSnippets.IntClassAndMarshaller, None };

            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Out + Count, "int[]", p), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Out + Count, "char[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Out + MarshalAsBoolArray, "bool[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Out + MarshalUsingIntMarshaller + Count, "int[]", p) + CodeSnippets.IntMarshaller, None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Out + Count, "string[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Out + Count, "string[]", p, (StringMarshalling.Utf8, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Out + Count + MarshalUsingIntStructMarshaller, "IntStruct[]", p) + CodeSnippets.IntStructAndMarshaller, None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(In + Out + Count + MarshalUsingIntClassMarshaller, "IntClass[]", p) + CodeSnippets.IntClassAndMarshaller, None };

            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Out + Count, "int[]", p), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Out + Count, "char[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Out + MarshalAsBoolArray, "bool[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Out + MarshalUsingIntMarshaller + Count, "int[]", p) + CodeSnippets.IntMarshaller, None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Out + Count, "string[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Out + Count, "string[]", p, (StringMarshalling.Utf8, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Out + Count + MarshalUsingIntStructMarshaller, "IntStruct[]", p) + CodeSnippets.IntStructAndMarshaller, None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Out + Count + MarshalUsingIntClassMarshaller, "IntClass[]", p) + CodeSnippets.IntClassAndMarshaller, None };

            // Array parameters without [In] or [Out] should provide an Info diagnostic
            var preferExplicitAttributesDiagnostic = new DiagnosticResult(GeneratorDiagnostics.GeneratedComInterfaceUsageDoesNotFollowBestPractices)
                    .WithLocation(0)
                    .WithArguments(SR.PreferExplicitInOutAttributesOnArrays);
            DiagnosticResult[] PreferInOutAttributes = [preferExplicitAttributesDiagnostic];
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "int[]", p), PreferInOutAttributes };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "char[]", p, (StringMarshalling.Utf16, null)), PreferInOutAttributes };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(MarshalAsBoolArray, "bool[]", p, (StringMarshalling.Utf16, null)), PreferInOutAttributes };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(MarshalUsingIntMarshaller + Count, "int[]", p) + CodeSnippets.IntMarshaller, PreferInOutAttributes };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "string[]", p, (StringMarshalling.Utf16, null)), PreferInOutAttributes };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "string[]", p, (StringMarshalling.Utf8, null)), PreferInOutAttributes };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count + MarshalUsingIntStructMarshaller, "IntStruct[]", p) + CodeSnippets.IntStructAndMarshaller, PreferInOutAttributes };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count + MarshalUsingIntClassMarshaller, "IntClass[]", p) + CodeSnippets.IntClassAndMarshaller, PreferInOutAttributes };

            // Ref Kinds shouldn't warn
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "in int[]", p), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "in char[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(MarshalAsBoolArray, "in bool[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(MarshalUsingIntMarshaller + Count, "in int[]", p) + CodeSnippets.IntMarshaller, None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "in string[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "in string[]", p, (StringMarshalling.Utf8, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count + MarshalUsingIntStructMarshaller, "in IntStruct[]", p) + CodeSnippets.IntStructAndMarshaller, None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count + MarshalUsingIntClassMarshaller, "in IntClass[]", p) + CodeSnippets.IntClassAndMarshaller, None };

            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "out int[]", p), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "out char[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(MarshalAsBoolArray, "out bool[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(MarshalUsingIntMarshaller + Count, "out int[]", p) + CodeSnippets.IntMarshaller, None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "out string[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "out string[]", p, (StringMarshalling.Utf8, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count + MarshalUsingIntStructMarshaller, "out IntStruct[]", p) + CodeSnippets.IntStructAndMarshaller, None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count + MarshalUsingIntClassMarshaller, "out IntClass[]", p) + CodeSnippets.IntClassAndMarshaller, None };

            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "ref int[]", p), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "ref char[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(MarshalAsBoolArray, "ref bool[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(MarshalUsingIntMarshaller + Count, "ref int[]", p) + CodeSnippets.IntMarshaller, None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "ref string[]", p, (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count, "ref string[]", p, (StringMarshalling.Utf8, null)), None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count + MarshalUsingIntStructMarshaller, "ref IntStruct[]", p) + CodeSnippets.IntStructAndMarshaller, None };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(Count + MarshalUsingIntClassMarshaller, "ref IntClass[]", p) + CodeSnippets.IntClassAndMarshaller, None };
        }

        public static IEnumerable<object[]> ByValueMarshalAttributeOnCustomCollections()
        {
            var codeSnippets = new CodeSnippets(GetAttributeProvider(GeneratorKind.ComInterfaceGenerator));
            const string In = "[{|#1:InAttribute|}]";
            const string Out = "[{|#2:OutAttribute|}]";
            const string paramName = "p";
            string p = $$"""{|#0:{{paramName}}|}""";
            const string CollectionMarshaller = "StatelessCollectionAllShapesMarshaller<,>";

            var diagnostic = new DiagnosticResult(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails);
            var outAttributeNotSupported = diagnostic
                    .WithLocation(0)
                    .WithArguments(SR.OutAttributeNotSupportedOnByValueParameters, paramName);
            var inAttributeIsNotSupported = diagnostic
                    .WithLocation(0)
                    .WithArguments(SR.InAttributeNotSupportedOnByValueParameters, paramName);
            var inOutAttributeNotSupported = diagnostic
                    .WithLocation(0)
                    .WithArguments(SR.InOutAttributeNotSupportedOnByValueParameters, paramName);

            DiagnosticResult[] InIsNotSupported = [inAttributeIsNotSupported];
            DiagnosticResult[] OutIsNotSupported = [outAttributeNotSupported];
            DiagnosticResult[] InOutIsNotSupported = [inOutAttributeNotSupported];
            DiagnosticResult[] None = [];

            yield return new object[] { ID(), Source("", "int"), None };
            yield return new object[] { ID(), Source("", "byte"), None };
            yield return new object[] { ID(), Source(MarshalUsing("IntClassMarshaller", 1), "IntClass") + CodeSnippets.IntClassAndMarshaller, None };
            yield return new object[] { ID(), Source(MarshalUsing("IntStructMarshaller", 1), "IntStruct") + CodeSnippets.IntStructAndMarshaller, None };
            yield return new object[] { ID(), Source("", "string", (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), Source("", "string", (StringMarshalling.Utf8, null)), None };
            yield return new object[] { ID(), Source(MarshalCollection(1), CodeSnippets.GetCustomCollectionType("int")), None };

            // [In] and [Out] are not allowed on custom collections
            yield return new object[] { ID(), Source(In, "int"), InIsNotSupported };
            yield return new object[] { ID(), Source(In, "byte"), InIsNotSupported };
            yield return new object[] { ID(), Source(In + MarshalUsing("IntClassMarshaller", 1), "IntClass") + CodeSnippets.IntClassAndMarshaller, InIsNotSupported };
            yield return new object[] { ID(), Source(In + MarshalUsing("IntStructMarshaller", 1), "IntStruct") + CodeSnippets.IntStructAndMarshaller, InIsNotSupported };
            yield return new object[] { ID(), Source(In, "string", (StringMarshalling.Utf16, null)), InIsNotSupported };
            yield return new object[] { ID(), Source(In, "string", (StringMarshalling.Utf8, null)), InIsNotSupported };
            yield return new object[] { ID(), Source(In + MarshalCollection(1), CodeSnippets.GetCustomCollectionType("int")), InIsNotSupported };

            yield return new object[] { ID(), Source(Out, "int"), OutIsNotSupported };
            yield return new object[] { ID(), Source(Out, "byte"), OutIsNotSupported };
            yield return new object[] { ID(), Source(Out + MarshalUsing("IntClassMarshaller", 1), "IntClass") + CodeSnippets.IntClassAndMarshaller, OutIsNotSupported };
            yield return new object[] { ID(), Source(Out + MarshalUsing("IntStructMarshaller", 1), "IntStruct") + CodeSnippets.IntStructAndMarshaller, OutIsNotSupported };
            yield return new object[] { ID(), Source(Out, "string", (StringMarshalling.Utf16, null)), OutIsNotSupported };
            yield return new object[] { ID(), Source(Out, "string", (StringMarshalling.Utf8, null)), OutIsNotSupported };
            yield return new object[] { ID(), Source(Out + MarshalCollection(1), CodeSnippets.GetCustomCollectionType("int")), OutIsNotSupported };

            yield return new object[] { ID(), Source(In + Out, "int"), InOutIsNotSupported };
            yield return new object[] { ID(), Source(In + Out, "byte"), InOutIsNotSupported };
            yield return new object[] { ID(), Source(In + Out + MarshalUsing("IntClassMarshaller", 1), "IntClass") + CodeSnippets.IntClassAndMarshaller, InOutIsNotSupported };
            yield return new object[] { ID(), Source(In + Out + MarshalUsing("IntStructMarshaller", 1), "IntStruct") + CodeSnippets.IntStructAndMarshaller, InOutIsNotSupported };
            yield return new object[] { ID(), Source(In + Out, "string", (StringMarshalling.Utf16, null)), InOutIsNotSupported };
            yield return new object[] { ID(), Source(In + Out, "string", (StringMarshalling.Utf8, null)), InOutIsNotSupported };
            yield return new object[] { ID(), Source(In + Out + MarshalCollection(1), CodeSnippets.GetCustomCollectionType("int")), InOutIsNotSupported };

            // RefKind modifiers are okay
            yield return new object[] { ID(), SourceWithRefKind("in", "", "int"), None };
            yield return new object[] { ID(), SourceWithRefKind("in", "", "byte"), None };
            yield return new object[] { ID(), SourceWithRefKind("in", MarshalUsing("IntClassMarshaller", 1), "IntClass") + CodeSnippets.IntClassAndMarshaller, None };
            yield return new object[] { ID(), SourceWithRefKind("in", MarshalUsing("IntStructMarshaller", 1), "IntStruct") + CodeSnippets.IntStructAndMarshaller, None };
            yield return new object[] { ID(), SourceWithRefKind("in", "", "string", (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), SourceWithRefKind("in", "", "string", (StringMarshalling.Utf8, null)), None };
            yield return new object[] { ID(), SourceWithRefKind("in", MarshalCollection(1), CodeSnippets.GetCustomCollectionType("int")), None };

            yield return new object[] { ID(), SourceWithRefKind("out", "", "int"), None };
            yield return new object[] { ID(), SourceWithRefKind("out", "", "byte"), None };
            yield return new object[] { ID(), SourceWithRefKind("out", MarshalUsing("IntClassMarshaller", 1), "IntClass") + CodeSnippets.IntClassAndMarshaller, None };
            yield return new object[] { ID(), SourceWithRefKind("out", MarshalUsing("IntStructMarshaller", 1), "IntStruct") + CodeSnippets.IntStructAndMarshaller, None };
            yield return new object[] { ID(), SourceWithRefKind("out", "", "string", (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), SourceWithRefKind("out", "", "string", (StringMarshalling.Utf8, null)), None };
            yield return new object[] { ID(), SourceWithRefKind("out", MarshalCollection(1), CodeSnippets.GetCustomCollectionType("int")), None };

            yield return new object[] { ID(), SourceWithRefKind("ref", "", "int"), None };
            yield return new object[] { ID(), SourceWithRefKind("ref", "", "byte"), None };
            yield return new object[] { ID(), SourceWithRefKind("ref", MarshalUsing("IntClassMarshaller", 1), "IntClass") + CodeSnippets.IntClassAndMarshaller, None };
            yield return new object[] { ID(), SourceWithRefKind("ref", MarshalUsing("IntStructMarshaller", 1), "IntStruct") + CodeSnippets.IntStructAndMarshaller, None };
            yield return new object[] { ID(), SourceWithRefKind("ref", "", "string", (StringMarshalling.Utf16, null)), None };
            yield return new object[] { ID(), SourceWithRefKind("ref", "", "string", (StringMarshalling.Utf8, null)), None };
            yield return new object[] { ID(), SourceWithRefKind("ref", MarshalCollection(1), CodeSnippets.GetCustomCollectionType("int")), None };

            string Source(string Attributes, string type, (StringMarshalling StringMarshalling, Type? StringMarshallingCustomType)? stringMarshalling = null)
                => SourceWithRefKind("", Attributes, type, stringMarshalling);

            string SourceWithRefKind(string refKind, string Attributes, string type, (StringMarshalling StringMarshalling, Type? StringMarshallingCustomType)? stringMarshalling = null)
            {
                return codeSnippets.ByValueMarshallingOfType(Attributes + MarshalCollection(), CodeSnippets.GetCustomCollectionType(type), p, stringMarshalling) + CodeSnippets.CustomCollectionAndMarshaller;
            }
            static string MarshalUsing(string marshaller = CollectionMarshaller, int depth = 0)
                => $"[MarshalUsing(typeof({marshaller}), ElementIndirectionDepth = {depth})]";
            static string MarshalCollection(int depth = 0)
                 => $"[MarshalUsing(typeof({CollectionMarshaller}), ElementIndirectionDepth = {depth}, ConstantElementCount = 10)]";
        }



        [Theory]
        [MemberData(nameof(ByValueMarshalAttributeOnPinnedMarshalledTypes))]
        [MemberData(nameof(ByValueMarshalAttributeOnValueTypes))]
        [MemberData(nameof(ByValueMarshalAttributeOnCustomCollections))]
        public async Task VerifyByValueMarshallingAttributeUsageInfoMessages(string id, string source, DiagnosticResult[] diagnostics)
        {
            _ = id;
            VerifyComInterfaceGenerator.Test test = new(referenceAncillaryInterop: false)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
            };
            test.DisabledDiagnostics.Remove(GeneratorDiagnostics.Ids.NotRecommendedGeneratedComInterfaceUsage);
            test.ExpectedDiagnostics.AddRange(diagnostics);
            await test.RunAsync();
        }
    }
}
