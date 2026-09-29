// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Interop;
using Xunit;

namespace Microsoft.Interop.SourceGeneration.UnitTests;

public class SourceGenerationTests
{
    [Fact]
    public void CoreAndTestAssemblyDoNotReferenceRoslyn()
    {
        foreach (Assembly assembly in new[] { typeof(TypePositionInfo).Assembly, typeof(SourceGenerationTests).Assembly })
        {
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), static reference =>
                reference.Name!.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)
                || reference.Name == "Microsoft.Interop.SourceGeneration.Roslyn");
        }
    }

    [Theory]
    [InlineData(MarshalDirection.ManagedToUnmanaged)]
    [InlineData(MarshalDirection.UnmanagedToManaged)]
    public void GeneratesStubFromModelsWithoutCompilerServices(MarshalDirection direction)
    {
        ImmutableArray<TypePositionInfo> elements = CreateSignature();
        var diagnostics = new RecordingDiagnosticsBag();
        IMarshallingGeneratorResolver resolver = CreateResolver(direction);
        string body;
        GeneratedMethodSignature signature;

        if (direction == MarshalDirection.ManagedToUnmanaged)
        {
            var generator = new ManagedToNativeStubGenerator(elements, setLastError: false, diagnostics, resolver, new CodeEmitOptions(SkipInit: false));
            body = generator.GenerateStubBody("Target");
            signature = generator.GenerateTargetMethodSignatureData();
            Assert.True(generator.NoMarshallingRequired);
            Assert.Equal("{\r\n    {\r\n        Target(value);\r\n    }\r\n}\r\n", body);
        }
        else
        {
            var generator = new UnmanagedToManagedStubGenerator(elements, diagnostics, resolver);
            body = generator.GenerateStubBodyForMethod("Target");
            signature = generator.GenerateAbiMethodSignatureData();
            Assert.Equal("{\r\n    Target(value);\r\n}\r\n", body);
        }

        Assert.Equal(direction == MarshalDirection.ManagedToUnmanaged ? "(int __value_native)" : "(int value)", signature.ParameterList);
        Assert.Equal("void", signature.ReturnType);
        Assert.Empty(diagnostics.Diagnostics);
        Assert.Equal(1, diagnostics.BatchReports);
    }

    [Theory]
    [InlineData(MarshalDirection.ManagedToUnmanaged)]
    [InlineData(MarshalDirection.UnmanagedToManaged)]
    public void StubGeneratorsReportBindingDiagnosticsThroughTheBag(MarshalDirection direction)
    {
        ImmutableArray<TypePositionInfo> elements = CreateSignature();
        var diagnostics = new RecordingDiagnosticsBag();
        var resolver = new DiagnosticResolver(CreateResolver(direction));

        if (direction == MarshalDirection.ManagedToUnmanaged)
        {
            _ = new ManagedToNativeStubGenerator(elements, setLastError: false, diagnostics, resolver, new CodeEmitOptions(SkipInit: false));
        }
        else
        {
            _ = new UnmanagedToManagedStubGenerator(elements, diagnostics, resolver);
        }

        Assert.Equal(1, diagnostics.BatchReports);
        Assert.Collection(diagnostics.Diagnostics,
            diagnostic => Assert.Equal(elements[0], Assert.IsType<GeneratorDiagnostic.NotRecommended>(diagnostic).TypePositionInfo),
            diagnostic => Assert.Equal(elements[1], Assert.IsType<GeneratorDiagnostic.NotRecommended>(diagnostic).TypePositionInfo));
    }

    [Theory]
    [InlineData(RefKind.None, ScopedKind.None, "", "")]
    [InlineData(RefKind.Ref, ScopedKind.ScopedRef, "scoped ref", "ref")]
    [InlineData(RefKind.In, ScopedKind.ScopedRef, "scoped in", "in")]
    [InlineData(RefKind.Out, ScopedKind.ScopedRef, "out", "out")]
    [InlineData(RefKind.RefReadOnlyParameter, ScopedKind.ScopedRef, "scoped ref readonly", "in")]
    [InlineData(RefKind.None, ScopedKind.ScopedValue, "scoped", "")]
    public void ParameterModifiersAreDerivedFromModelEnums(RefKind refKind, ScopedKind scopedKind, string modifiers, string argumentKeyword)
    {
        TypePositionInfo parameter = CreateSignature()[0] with { RefKind = refKind, ScopedKind = scopedKind };

        Assert.Equal(modifiers, MarshallerHelpers.GetManagedParameterModifiers(parameter));
        Assert.Equal(argumentKeyword, MarshallerHelpers.GetManagedArgumentRefKindKeyword(parameter));
        Assert.Equal(refKind != RefKind.None, parameter.IsByRef);
    }

    [Fact]
    public void SignatureCanBeConstructedFromData()
    {
        ImmutableArray<TypePositionInfo> elements = CreateSignature();
        var signature = new SignatureContext(elements, "void", ["Example"]);
        var equivalent = new SignatureContext([.. elements], "void", ["Example"]);

        Assert.Equal(signature, equivalent);
        Assert.Equal("int value", Assert.Single(signature.StubParameters).Declaration);
        Assert.Equal(elements[0], Assert.Single(signature.ManagedParameters));
        Assert.Equal("Example", Assert.Single(signature.AdditionalAttributes));
    }

    [Theory]
    [InlineData(SpecialType.System_Int32, true, true)]
    [InlineData(SpecialType.System_UIntPtr, true, true)]
    [InlineData(SpecialType.System_Void, false, true)]
    [InlineData(SpecialType.System_Double, false, true)]
    [InlineData(SpecialType.System_Boolean, false, false)]
    [InlineData(SpecialType.System_Char, false, false)]
    [InlineData(SpecialType.System_String, false, false)]
    [InlineData(SpecialType.System_Enum, false, false)]
    [InlineData(SpecialType.None, false, false)]
    public void SpecialTypeClassificationUsesOnlyMarshallingSemantics(SpecialType type, bool integral, bool blittable)
    {
        Assert.Equal(integral, type.IsIntegralType());
        Assert.Equal(blittable, type.IsAlwaysBlittable());
    }

    private static ImmutableArray<TypePositionInfo> CreateSignature() =>
    [
        new(SpecialTypeInfo.Int32, NoMarshallingInfo.Instance)
        {
            ManagedIndex = 0,
            NativeIndex = 0,
            InstanceIdentifier = "value"
        },
        new(SpecialTypeInfo.Void, NoMarshallingInfo.Instance)
        {
            ManagedIndex = TypePositionInfo.ReturnIndex,
            NativeIndex = TypePositionInfo.ReturnIndex
        }
    ];

    private static IMarshallingGeneratorResolver CreateResolver(MarshalDirection direction) =>
        DefaultMarshallingGeneratorResolver.Create(EnvironmentFlags.None, direction, "StringMarshalling", []);

    private sealed class RecordingDiagnosticsBag : IGeneratorDiagnosticsBag
    {
        public List<GeneratorDiagnostic> Diagnostics { get; } = [];
        public int BatchReports { get; private set; }

        public void ReportGeneratorDiagnostic(GeneratorDiagnostic diagnostic) => Diagnostics.Add(diagnostic);

        public void ReportGeneratorDiagnostics(ImmutableArray<GeneratorDiagnostic> diagnostics)
        {
            BatchReports++;
            foreach (GeneratorDiagnostic diagnostic in diagnostics)
            {
                ReportGeneratorDiagnostic(diagnostic);
            }
        }
    }

    private sealed class DiagnosticResolver(IMarshallingGeneratorResolver inner) : IMarshallingGeneratorResolver
    {
        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            ResolvedGenerator result = inner.Create(info, context);
            Assert.True(result.IsResolvedWithoutErrors);
            return result with { Diagnostics = [new GeneratorDiagnostic.NotRecommended(info) { Details = info.InstanceIdentifier }] };
        }
    }
}
