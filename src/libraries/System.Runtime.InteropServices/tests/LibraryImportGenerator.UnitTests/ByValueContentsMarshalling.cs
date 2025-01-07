// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Xunit;
using static Microsoft.Interop.UnitTests.TestUtils;
using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.LibraryImportGenerator>;

namespace LibraryImportGenerator.UnitTests
{
    public class ByValueContentsMarshalling
    {
        public static IEnumerable<object[]> ByValueMarshalAttributeOnValueTypes()
        {
            CodeSnippets.ByValueParameterWithModifier("int[]", "In");

            const string In = "InAttribute";
            const string Out = "OutAttribute";
            const string InOut = "InAttribute, OutAttribute";
            const string paramName = "p";
            const string MarshalUsingUtf16 = "MarshalUsing(typeof(Utf16StringMarshaller))";

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
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("int", In), InIsNotSupported };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("byte", In), InIsNotSupported };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("string", In + " , " + MarshalUsingUtf16), InIsNotSupported };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("IntStruct", In, CodeSnippets.IntStructAndMarshaller), InIsNotSupported };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("IntClass", In, CodeSnippets.IntClassAndMarshaller), InIsNotSupported };

            DiagnosticResult[] OutIsNotSupported = [outAttributeNotSupported];
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("int", Out), OutIsNotSupported };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("byte", Out), OutIsNotSupported };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("string", Out + " , " + MarshalUsingUtf16), OutIsNotSupported };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("IntStruct", Out, CodeSnippets.IntStructAndMarshaller), OutIsNotSupported };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("IntClass", Out, CodeSnippets.IntClassAndMarshaller), OutIsNotSupported };

            DiagnosticResult[] InOutIsNotSupported = [inOutAttributeNotSupported];
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("int", InOut), InOutIsNotSupported };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("byte", InOut), InOutIsNotSupported };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("string", InOut + " , " + MarshalUsingUtf16), InOutIsNotSupported };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("IntStruct", InOut, CodeSnippets.IntStructAndMarshaller), InOutIsNotSupported };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("IntClass", InOut, CodeSnippets.IntClassAndMarshaller), InOutIsNotSupported };

            var inAndOutNotAllowedWithRefKind = new DiagnosticResult(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(0)
                .WithArguments("The '[In]' and '[Out]' attributes are unsupported on parameters passed by reference. Use the 'in', 'ref', or 'out' keywords instead.", "p");
            DiagnosticResult[] InAndOutNotAllowedWithRefKind = [inAndOutNotAllowedWithRefKind];
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("in int", In), InAndOutNotAllowedWithRefKind };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("ref int", In), InAndOutNotAllowedWithRefKind };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("ref int", InOut), InAndOutNotAllowedWithRefKind };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("out int", Out), InAndOutNotAllowedWithRefKind };
        }

        public static IEnumerable<object[]> ByValueMarshalAttributeOnArrays()
        {
            CodeSnippets.ByValueParameterWithModifier("int[]", "In");

            const string In = "InAttribute";
            const string Out = "OutAttribute";
            const string InOut = "InAttribute, OutAttribute";
            const string MarshalUsingUtf16 = "MarshalUsing(typeof(Utf16StringMarshaller), ElementIndirectionDepth = 1)";
            const string Count = "MarshalUsing(ConstantElementCount = 10)";
            DiagnosticResult[] None = [];

            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("int[]", string.Join(',', Count, In)), None };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("byte[]", string.Join(',', Count, In)), None };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("string[]", string.Join(',', Count, In, MarshalUsingUtf16)), None };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("IntStruct[]", string.Join(',', Count, In), CodeSnippets.IntStructAndMarshaller), None };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("IntClass[]", string.Join(',', Count, In), CodeSnippets.IntClassAndMarshaller), None };

            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("int[]", string.Join(',', Count, Out)), None };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("byte[]", string.Join(',', Count, Out)), None };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("string[]", string.Join(',', Count, Out, MarshalUsingUtf16)), None };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("IntStruct[]", string.Join(',', Count, Out), CodeSnippets.IntStructAndMarshaller), None };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("IntClass[]", string.Join(',', Count, Out), CodeSnippets.IntClassAndMarshaller), None };

            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("int[]", string.Join(',', Count, InOut)), None };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("byte[]", string.Join(',', Count, InOut)), None };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("string[]", string.Join(',', Count, InOut, MarshalUsingUtf16)), None };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("IntStruct[]", string.Join(',', Count, InOut), CodeSnippets.IntStructAndMarshaller), None };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("IntClass[]", string.Join(',', Count, InOut), CodeSnippets.IntClassAndMarshaller), None };

            DiagnosticResult preferAttributes = new DiagnosticResult(GeneratorDiagnostics.LibraryImportUsageDoesNotFollowBestPractices)
                .WithArguments(SR.PreferExplicitInOutAttributesOnArrays)
                .WithLocation(0);
            DiagnosticResult[] PreferAttributes = [preferAttributes];
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("int[]", Count), PreferAttributes };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("byte[]", Count), PreferAttributes };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("string[]", string.Join(',', Count, MarshalUsingUtf16)), PreferAttributes };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("IntStruct[]", Count, CodeSnippets.IntStructAndMarshaller), PreferAttributes };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("IntClass[]", Count, CodeSnippets.IntClassAndMarshaller), PreferAttributes };

            var inAndOutNotAllowedWithRefKind = new DiagnosticResult(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(0)
                .WithArguments("The '[In]' and '[Out]' attributes are unsupported on parameters passed by reference. Use the 'in', 'ref', or 'out' keywords instead.", "p");
            DiagnosticResult[] InAndOutNotAllowedWithRefKind = [inAndOutNotAllowedWithRefKind];
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("in int[]", string.Join(',', Count, In)), InAndOutNotAllowedWithRefKind };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("ref int[]", string.Join(',', Count, In)), InAndOutNotAllowedWithRefKind };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("ref int[]", string.Join(',', Count, In, Out)), InAndOutNotAllowedWithRefKind };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("out int[]", string.Join(',', Count, Out)), InAndOutNotAllowedWithRefKind };
        }


        [Theory]
        [MemberData(nameof(ByValueMarshalAttributeOnValueTypes))]
        [MemberData(nameof(ByValueMarshalAttributeOnArrays))]
        public async Task VerifyByValueMarshallingAttributeUsageInfoMessages(string id, string source, DiagnosticResult[] diagnostics)
        {
            _ = id;
            VerifyCS.Test test = new(referenceAncillaryInterop: false)
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
