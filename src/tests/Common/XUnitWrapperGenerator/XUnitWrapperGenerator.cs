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
        var methodsInSource = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, ct) =>
                    node.IsKind(SyntaxKind.MethodDeclaration)
                        && node is MethodDeclarationSyntax method
                        && method.AttributeLists.Count > 0,
                static (context, ct) => (IMethodSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node)!);

        var methodsInReferencedAssemblies = context.MetadataReferencesProvider.Combine(context.CompilationProvider).SelectMany((data, ct) =>
        {
            ExternallyReferencedTestMethodsVisitor visitor = new();
            return visitor.Visit(data.Right.GetAssemblyOrModuleSymbol(data.Left))!;
        });

        var assemblyName = context.CompilationProvider.Select((comp, ct) => comp.Assembly.MetadataName);

        var allMethods = methodsInSource.Collect().Combine(methodsInReferencedAssemblies.Collect()).SelectMany((methods, ct) => methods.Left.AddRange(methods.Right));

        var aliasMap = context.CompilationProvider.Select((comp, ct) =>
        {
            var aliasMap = ImmutableDictionary.CreateBuilder<string, string>();
            aliasMap.Add(comp.Assembly.MetadataName, "global");
            foreach (var reference in comp.References)
            {
                aliasMap.Add(comp.GetAssemblyOrModuleSymbol(reference)!.MetadataName, reference.Properties.Aliases.FirstOrDefault() ?? "global");
            }

            return aliasMap.ToImmutable();
        }).WithComparer(new ImmutableDictionaryValueComparer<string, string>(EqualityComparer<string>.Default));

        context.RegisterImplementationSourceOutput(
            allMethods
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Combine(aliasMap)
            .SelectMany((data, ct) => ImmutableArray.CreateRange(GetTestMethodInfosForMethod(data.Left.Left, data.Left.Right, data.Right)))
            .Collect()
            .Combine(aliasMap)
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Combine(assemblyName),
            static (context, data) =>
            {
                var (((methods, aliasMap), configOptions), assemblyName) = data;

                bool referenceCoreLib = configOptions.GlobalOptions.ReferenceSystemPrivateCoreLib();

                bool isMergedTestRunnerAssembly = configOptions.GlobalOptions.IsMergedTestRunnerAssembly();

                // TODO: add error (maybe in MSBuild that referencing CoreLib directly from a merged test runner is not supported)
                if (isMergedTestRunnerAssembly && !referenceCoreLib)
                {
                    context.AddSource("FullRunner.g.cs", GenerateFullTestRunner(methods, aliasMap, assemblyName));
                }
                else
                {
                    string consoleType = referenceCoreLib ? "Internal.Console" : "System.Console";
                    context.AddSource("SimpleRunner.g.cs", GenerateStandaloneSimpleTestRunner(methods, aliasMap, consoleType));
                }
            });
    }

    private static string GenerateFullTestRunner(ImmutableArray<ITestInfo> testInfos, ImmutableDictionary<string, string> aliasMap, string assemblyName)
    {
        // For simplicity, we'll use top-level statements for the generated Main method.
        StringBuilder builder = new();
        ITestReporterWrapper reporter = new WrapperLibraryTestSummaryReporting("summary");
        builder.AppendLine(string.Join("\n", aliasMap.Values.Where(alias => alias != "global").Select(alias => $"extern alias {alias};")));

        builder.AppendLine("XUnitWrapperLibrary.TestSummary summary = new();");
        builder.AppendLine("System.Diagnostics.Stopwatch stopwatch = new();");

        foreach (ITestInfo test in testInfos)
        {
            builder.AppendLine(test.GenerateTestExecution(reporter));
        }

        builder.AppendLine($@"System.IO.File.WriteAllText(""{assemblyName}.testResults.xml"", summary.GetTestResultOutput());");

        return builder.ToString();
    }

    private static string GenerateStandaloneSimpleTestRunner(ImmutableArray<ITestInfo> testInfos, ImmutableDictionary<string, string> aliasMap, string consoleType)
    {
        // For simplicity, we'll use top-level statements for the generated Main method.
        ITestReporterWrapper reporter = new NoTestReporting();
        StringBuilder builder = new();
        builder.AppendLine(string.Join("\n", aliasMap.Values.Where(alias => alias != "global").Select(alias => $"extern alias {alias};")));
        builder.AppendLine("try {");
        builder.AppendLine(string.Join("\n", testInfos.Select(m => m.GenerateTestExecution(reporter))));
        builder.AppendLine($"}} catch(System.Exception ex) {{ {consoleType}.WriteLine(ex.ToString()); return 101; }}");
        builder.AppendLine("return 100;");
        return builder.ToString();
    }

    private class ExternallyReferencedTestMethodsVisitor : SymbolVisitor<IEnumerable<IMethodSymbol>>
    {
        public override IEnumerable<IMethodSymbol>? VisitAssembly(IAssemblySymbol symbol)
        {
            return Visit(symbol.GlobalNamespace);
        }

        public override IEnumerable<IMethodSymbol>? VisitModule(IModuleSymbol symbol)
        {
            return Visit(symbol.GlobalNamespace);
        }

        public override IEnumerable<IMethodSymbol>? VisitNamespace(INamespaceSymbol symbol)
        {
            foreach (var type in symbol.GetMembers())
            {
                foreach (var result in Visit(type) ?? Array.Empty<IMethodSymbol>())
                {
                    yield return result;
                }
            }
        }

        public override IEnumerable<IMethodSymbol>? VisitNamedType(INamedTypeSymbol symbol)
        {
            if (symbol.DeclaredAccessibility == Accessibility.Public)
            {
                foreach (var type in symbol.GetMembers())
                {
                    foreach (var result in Visit(type) ?? Array.Empty<IMethodSymbol>())
                    {
                        yield return result;
                    }
                }
            }
        }

        public override IEnumerable<IMethodSymbol>? VisitMethod(IMethodSymbol symbol)
        {
            if (symbol.DeclaredAccessibility == Accessibility.Public
                && symbol.GetAttributes().Any(attr => attr.AttributeClass?.ContainingNamespace.Name == "Xunit"))
            {
                return new[] { symbol };
            }
            return Array.Empty<IMethodSymbol>();
        }
    }

    private static IEnumerable<ITestInfo> GetTestMethodInfosForMethod(IMethodSymbol method, AnalyzerConfigOptionsProvider options, ImmutableDictionary<string, string> aliasMap)
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
                case "Xunit.InlineDataAttribute":
                case "Xunit.MemberDataAttribute":
                    theoryDataAttributes.Add(attr);
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
                testInfos = ImmutableArray.Create((ITestInfo)new BasicTestMethod(method, aliasMap[method.ContainingAssembly.MetadataName]));
            }
        }
        else if (theoryAttribute)
        {
            testInfos = CreateTestCases(method, theoryDataAttributes, aliasMap[method.ContainingAssembly.MetadataName]);
        }

        foreach (var filterAttribute in filterAttributes)
        {
            switch (filterAttribute.AttributeClass!.ToDisplayString())
            {
                case "Xunit.ConditionalFactAttribute":
                case "Xunit.ConditionalTheoryAttribute":
                    {
                        ITypeSymbol conditionType = (ITypeSymbol)filterAttribute.ConstructorArguments[0].Value!;
                        testInfos = DecorateWithUserDefinedCondition(
                            testInfos,
                            conditionType,
                            filterAttribute.ConstructorArguments[1].Values,
                            aliasMap[conditionType.ContainingAssembly.MetadataName]);
                        break;
                    }
                case "Xunit.ActiveIssueAttribute":
                    if (filterAttribute.AttributeConstructor!.Parameters.Length == 3)
                    {
                        ITypeSymbol conditionType = (ITypeSymbol)filterAttribute.ConstructorArguments[1].Value!;
                        testInfos = DecorateWithUserDefinedCondition(
                            testInfos,
                            conditionType,
                            filterAttribute.ConstructorArguments[2].Values,
                            aliasMap[conditionType.ContainingAssembly.MetadataName]);
                        break;
                    }
                    else if (filterAttribute.AttributeConstructor.Parameters.Length == 4)
                    {
                        testInfos = FilterForSkippedRuntime(
                            FilterForSkippedTargetFrameworkMonikers(
                                DecorateWithSkipOnPlatform(testInfos, (int)filterAttribute.ConstructorArguments[1].Value!, options),
                                (int)filterAttribute.ConstructorArguments[2].Value!),
                            (int)filterAttribute.ConstructorArguments[3].Value!, options);
                    }
                    else
                    {
                        switch (filterAttribute.AttributeConstructor.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                        {
                            case "global::Xunit.TestPlatforms":
                                testInfos = DecorateWithSkipOnPlatform(testInfos, (int)filterAttribute.ConstructorArguments[1].Value!, options);
                                break;
                            case "global::Xunit.TestRuntimes":
                                testInfos = FilterForSkippedRuntime(testInfos, (int)filterAttribute.ConstructorArguments[1].Value!, options);
                                break;
                            case "global::Xunit.TargetFrameworkMonikers":
                                testInfos = FilterForSkippedTargetFrameworkMonikers(testInfos, (int)filterAttribute.ConstructorArguments[1].Value!);
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case "Xunit.SkipOnPlatformAttribute":
                    testInfos = DecorateWithSkipOnPlatform(testInfos, (int)filterAttribute.ConstructorArguments[0].Value!, options);
                    break;
            }
        }

        return testInfos;
    }

    private static ImmutableArray<ITestInfo> FilterForSkippedTargetFrameworkMonikers(ImmutableArray<ITestInfo> testInfos, int v)
    {
        var tfm = (Xunit.TargetFrameworkMonikers)v;

        if (tfm.HasFlag(Xunit.TargetFrameworkMonikers.Netcoreapp))
        {
            return ImmutableArray<ITestInfo>.Empty;
        }
        else
        {
            return testInfos;
        }
    }

    private static ImmutableArray<ITestInfo> CreateTestCases(IMethodSymbol method, List<AttributeData> theoryDataAttributes, string alias)
    {
        var testCasesBuilder = ImmutableArray.CreateBuilder<ITestInfo>();
        foreach (var attr in theoryDataAttributes)
        {
            switch (attr.AttributeClass!.ToDisplayString())
            {
                case "Xunit.InlineDataAttribute":
                    {
                        var args = attr.ConstructorArguments[0].Values;
                        if (method.Parameters.Length != args.Length)
                        {
                            // Emit diagnostic
                            continue;
                        }
                        var argsAsCode = ImmutableArray.CreateRange(args.Select(a => a.ToCSharpString()));
                        testCasesBuilder.Add(new BasicTestMethod(method, alias, arguments: argsAsCode));
                        break;
                    }
                case "Xunit.MemberDataAttribute":
                    {
                        string? memberName = (string?)attr.ConstructorArguments[0].Value;
                        if (string.IsNullOrEmpty(memberName))
                        {
                            // Emit diagnostic
                            continue;
                        }
                        var membersByName = method.ContainingType.GetMembers(memberName!);
                        if (membersByName.Length != 1)
                        {
                            // Emit diagnostic
                            continue;
                        }
                        const string argumentVariableIdentifier = "testArguments";
                        // The display name for the test is an interpolated string that includes the arguments.
                        string displayNameOverride = $@"$""{alias}::{method.ContainingType.ToDisplayString(FullyQualifiedWithoutGlobalNamespace)}.{method.Name}({{string.Join("","", {argumentVariableIdentifier})}})""";
                        var argsAsCode = method.Parameters.Select((p, i) => $"({p.Type.ToDisplayString()}){argumentVariableIdentifier}[{i}]").ToImmutableArray();
                        testCasesBuilder.Add(new MemberDataTest(membersByName[0], new BasicTestMethod(method, alias, argsAsCode, displayNameOverride), alias, argumentVariableIdentifier));
                        break;
                    }
                default:
                    break;
            }
        }
        return testCasesBuilder.ToImmutable();
    }

    private static ImmutableArray<ITestInfo> FilterForSkippedRuntime(ImmutableArray<ITestInfo> testInfos, int v, AnalyzerConfigOptionsProvider options)
    {
        Xunit.TestRuntimes skippedRuntimes = (Xunit.TestRuntimes)v;
        options.GlobalOptions.TryGetValue("build_property.RuntimeFlavor", out string? runtimeFlavor);
        if (runtimeFlavor == "Mono" && skippedRuntimes.HasFlag(Xunit.TestRuntimes.Mono))
        {
            return ImmutableArray<ITestInfo>.Empty;
        }
        else if ((string.IsNullOrEmpty(runtimeFlavor) || runtimeFlavor == "CoreCLR") && skippedRuntimes.HasFlag(Xunit.TestRuntimes.CoreCLR))
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
            return ImmutableArray.CreateRange(testInfos.Select(t => (ITestInfo)new ConditionalTest(t, platformsToEnableTest)));
        }
        else
        {
            // The target platform is not mentioned in the attribute, just run it as-is.
            return testInfos;
        }

        static Xunit.TestPlatforms GetPlatformForTargetOS(string? targetOS)
        {
            return targetOS?.ToLowerInvariant() switch
            {
                "windows" => Xunit.TestPlatforms.Windows,
                "linux" => Xunit.TestPlatforms.Linux,
                "osx" => Xunit.TestPlatforms.OSX,
                "illumos" => Xunit.TestPlatforms.illumos,
                "solaris" => Xunit.TestPlatforms.Solaris,
                "android" => Xunit.TestPlatforms.Android,
                "ios" => Xunit.TestPlatforms.iOS,
                "tvos" => Xunit.TestPlatforms.tvOS,
                "maccatalyst" => Xunit.TestPlatforms.MacCatalyst,
                "browser" => Xunit.TestPlatforms.Browser,
                "freebsd" => Xunit.TestPlatforms.FreeBSD,
                "netbsd" => Xunit.TestPlatforms.NetBSD,
                null or "" or "anyos" => Xunit.TestPlatforms.Any,
                _ => 0
            };
        }
    }

    private static ImmutableArray<ITestInfo> DecorateWithUserDefinedCondition(
        ImmutableArray<ITestInfo> testInfos,
        ITypeSymbol conditionType,
        ImmutableArray<TypedConstant> values,
        string externAlias)
    {
        string condition = string.Join("&&", values.Select(v => $"{externAlias}::{conditionType.ToDisplayString(FullyQualifiedWithoutGlobalNamespace)}.{v.Value}"));
        return ImmutableArray.CreateRange<ITestInfo>(testInfos.Select(m => new ConditionalTest(m, condition)));
    }

    public static readonly SymbolDisplayFormat FullyQualifiedWithoutGlobalNamespace = SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);
}
