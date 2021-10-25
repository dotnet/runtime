// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace XUnitWrapperGenerator;

[Generator]
public sealed class XUnitWrapperGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterImplementationSourceOutput(
            context.SyntaxProvider.CreateSyntaxProvider(
                static (node, ct) =>
                    node.IsKind(SyntaxKind.MethodDeclaration)
                        && node is MethodDeclarationSyntax method
                        && method.AttributeLists.Count > 0,
                static (context, ct) => (IMethodSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node)!)
            .Combine(context.AnalyzerConfigOptionsProvider)
            .SelectMany((data, ct) => ImmutableArray.CreateRange(GetTestMethodInfosForMethod(data.Left, data.Right)))
            .Collect(),
            static (context, methods) =>
            {
                // For simplicity, we'll use top-level statements for the generated Main method.
                StringBuilder builder = new();
                builder.AppendLine("try {");
                builder.AppendLine(string.Join("\n", methods.Select(m => m.ExecutionStatement)));
                builder.AppendLine("} catch(System.Exception) { return 101; }");
                builder.AppendLine("return 100;");
                context.AddSource("Main.g.cs", builder.ToString());
            });
    }

    private static IEnumerable<ITestInfo> GetTestMethodInfosForMethod(IMethodSymbol method, AnalyzerConfigOptionsProvider options)
    {
        bool factAttribute = false;
        bool theoryAttribute = false;
        List<AttributeData> theoryDataAttributes = new();
        List<AttributeData> filterAttributes = new();
        foreach (var attr in method.GetAttributes())
        {
            switch (attr.AttributeClass?.ToDisplayString())
            {
                case "Xunit.ConditionalFactAttribute":
                    filterAttributes.Add(attr);
                    factAttribute = true;
                    break;
                case "Xunit.FactAttribute":
                    factAttribute = true;
                    break;
                case "Xunit.ConditionalTheoryAttribute":
                    filterAttributes.Add(attr);
                    theoryAttribute = true;
                    break;
                case "Xunit.TheoryAttribute":
                    theoryAttribute = true;
                    break;
                case "Xunit.SkipOnPlatformAttribute":
                case "Xunit.ActiveIssueAttribute":
                    filterAttributes.Add(attr);
                    break;
            }
        }

        ImmutableArray<ITestInfo> testInfos = ImmutableArray<ITestInfo>.Empty;

        if (factAttribute)
        {
            if (!method.Parameters.IsEmpty)
            {
                // emit diagnostic
            }
            else
            {
                testInfos = ImmutableArray.Create(method.IsStatic ? (ITestInfo)new StaticFactMethod(method) : new InstanceFactMethod(method));
            }
        }

        foreach (var filterAttribute in filterAttributes)
        {
            switch (filterAttribute.AttributeClass!.ToDisplayString())
            {
                case "Xunit.ConditionalFactAttribute":
                case "Xunit.ConditionalTheoryAttribute":
                    string conditionTypeName = ((ITypeSymbol)filterAttribute.ConstructorArguments[0].Value!).ToDisplayString();
                    testInfos = DecorateWithUserDefinedCondition(testInfos, (ITypeSymbol)filterAttribute.ConstructorArguments[0].Value!, filterAttribute.ConstructorArguments[1].Values);
                    break;
                case "Xunit.ActiveIssueAttribute":
                    if (filterAttribute.AttributeConstructor!.Parameters.Length == 3)
                    {
                        testInfos = DecorateWithUserDefinedCondition(testInfos, (ITypeSymbol)filterAttribute.ConstructorArguments[1].Value!, filterAttribute.ConstructorArguments[2].Values);
                    }
                    switch (filterAttribute.AttributeConstructor.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    {
                        case "global::Xunit.TestPlatforms":
                            testInfos = DecorateWithSkipOnPlatform(testInfos, (int)filterAttribute.ConstructorArguments[1].Value!, options);
                            break;
                        case "global::Xunit.TestRuntimes":
                            testInfos = FilterForRuntimeFlavor(testInfos, (int)filterAttribute.ConstructorArguments[1].Value!, options);
                            break;
                        default:
                            break;
                    }
                    break;
                case "Xunit.SkipOnPlatformAttribute":
                    testInfos = DecorateWithSkipOnPlatform(testInfos, (int)filterAttribute.ConstructorArguments[0].Value!, options);
                    break;
            }
        }

        return testInfos;
    }

    private static ImmutableArray<ITestInfo> FilterForRuntimeFlavor(ImmutableArray<ITestInfo> testInfos, int v, AnalyzerConfigOptionsProvider options)
    {
        Xunit.TestRuntimes runtime = (Xunit.TestRuntimes)v;
        options.GlobalOptions.TryGetValue("build_property.RuntimeFlavor", out string? runtimeFlavor);
        if (runtimeFlavor == "Mono" && runtime.HasFlag(Xunit.TestRuntimes.Mono))
        {
            return ImmutableArray<ITestInfo>.Empty;
        }
        else if (runtime.HasFlag(Xunit.TestRuntimes.CoreCLR))
        {
            return ImmutableArray<ITestInfo>.Empty;
        }
        return testInfos;
    }

    private static ImmutableArray<ITestInfo> DecorateWithSkipOnPlatform(ImmutableArray<ITestInfo> testInfos, int v, AnalyzerConfigOptionsProvider options)
    {
        Xunit.TestPlatforms platformsToSkip = (Xunit.TestPlatforms)v;
        options.GlobalOptions.TryGetValue("build_property.TargetOS", out string? targetOS);
        Xunit.TestPlatforms targetPlatform = GetPlatformForTargetOS(targetOS);

        if (platformsToSkip.HasFlag(targetPlatform))
        {
            // If the target platform is skipped, then we don't have any tests to emit.
            return ImmutableArray<ITestInfo>.Empty;
        }
        else if (targetPlatform.HasFlag(platformsToSkip))
        {
            // If our target platform encompases one or more of the skipped platforms,
            // emit a runtime platform check here.
            Xunit.TestPlatforms platformsToEnableTest = targetPlatform & ~platformsToSkip;
            return ImmutableArray.CreateRange(testInfos.Select(t => (ITestInfo)new PlatformSpecificTest(t, platformsToEnableTest)));
        }
        else
        {
            // The target platform is not mentioned in the attribute, just run it as-is.
            return testInfos;
        }

        static Xunit.TestPlatforms GetPlatformForTargetOS(string? targetOS)
        {
            return targetOS switch
            {
                "windows" => Xunit.TestPlatforms.Windows,
                "Linux" => Xunit.TestPlatforms.Linux,
                "OSX" => Xunit.TestPlatforms.OSX,
                "illumos" => Xunit.TestPlatforms.illumos,
                "Solaris" => Xunit.TestPlatforms.Solaris,
                "Android" => Xunit.TestPlatforms.Android,
                "iOS" => Xunit.TestPlatforms.iOS,
                "tvOS" => Xunit.TestPlatforms.tvOS,
                "macCatalyst" => Xunit.TestPlatforms.MacCatalyst,
                "Browser" => Xunit.TestPlatforms.Browser,
                "FreeBSD" => Xunit.TestPlatforms.FreeBSD,
                "NetBSD" => Xunit.TestPlatforms.NetBSD,
                null or "" or "AnyOS" => Xunit.TestPlatforms.Any,
                _ => 0
            };
        }
    }

    private static ImmutableArray<ITestInfo> DecorateWithUserDefinedCondition(
        ImmutableArray<ITestInfo> testInfos,
        ITypeSymbol conditionType,
        ImmutableArray<TypedConstant> values)
    {
        string condition = string.Join("&&", values.Select(v => $"{conditionType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{v.Value}"));
        return ImmutableArray.CreateRange<ITestInfo>(testInfos.Select(m => new ConditionalTest(m, condition)));
    }
}
