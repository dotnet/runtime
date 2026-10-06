// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Reflection.Individual;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCasesRunner;
using Xunit;
using TypeName = System.Reflection.Metadata.TypeName;

#nullable enable

namespace Mono.Linker.Tests.TestCases;

internal static class TypeMapOutputTests
{
    private const string Prefix = "Mono.Linker.Tests.Cases.Reflection.Individual.CanOutputTypeMaps+";
    private const string LibraryPrefix = "Mono.Linker.Tests.Cases.Reflection.Dependencies.";
    private const string Target = Prefix + @"Nested+Target\+With\,Reserved=""value""";

    private sealed class Map
    {
        public List<(string Key, string Target)> Entries { get; } = [];
    }

    public static async Task<(int ExitCode, string Diagnostics)> RunTool(string tool, string workingDirectory, IEnumerable<string> arguments)
    {
        string dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")
            ?? Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..",
                OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"));
        Assert.True(File.Exists(dotnet), $"The dotnet host was not found at '{dotnet}'.");
        var startInfo = new ProcessStartInfo(dotnet)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add("--runtimeconfig");
        startInfo.ArgumentList.Add(Path.ChangeExtension(typeof(TypeMapOutputTests).Assembly.Location, ".runtimeconfig.json"));
        startInfo.ArgumentList.Add(tool);
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        Assert.True(process.Start());
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            Assert.Fail($"Tool timed out.\n{await stdout}\n{await stderr}");
        }

        return (process.ExitCode, await stdout + await stderr);
    }

    public static TrimmedTestCaseResult CreateFixture(TestRunner runner)
    {
        NPath casesRoot = TestDatabase.TestCasesRootDirectory;
        var testCase = new TestCase(casesRoot.Combine("Reflection/Individual/CanOutputTypeMaps.cs"), casesRoot,
            typeof(CanOutputTypeMaps).Assembly.Location.ToNPath());
        TrimmedTestCaseResult? result = runner.Run(testCase);
        Assert.NotNull(result);

        // C# cannot declare identifiers containing these legal metadata characters.
        using var assembly = AssemblyDefinition.ReadAssembly(result.InputAssemblyPath.ToString(), new ReaderParameters { InMemory = true });
        TypeDefinition declaringType = assembly.MainModule.GetType(typeof(CanOutputTypeMaps).FullName);
        TypeDefinition nestedType = Assert.Single(declaringType.NestedTypes, type => type.Name == "Nested");
        TypeDefinition target = Assert.Single(nestedType.NestedTypes, type => type.Name == "Target");
        string originalName = target.FullName;
        target.Name = "Target+With,Reserved=\"value\"";
        foreach (CustomAttribute attribute in assembly.CustomAttributes)
        {
            // A byref typeof expression also requires constructing metadata rather than C# syntax.
            if (attribute.AttributeType.Name == "TypeMapAttribute`1" &&
                attribute.ConstructorArguments[0].Value is string key && key == "byref")
            {
                CustomAttributeArgument argument = attribute.ConstructorArguments[1];
                attribute.ConstructorArguments[1] = new CustomAttributeArgument(argument.Type, new ByReferenceType((TypeReference)argument.Value));
            }
            for (int i = 0; i < attribute.ConstructorArguments.Count; i++)
            {
                CustomAttributeArgument argument = attribute.ConstructorArguments[i];
                if (argument.Value is TypeReference reference && reference.FullName == originalName)
                    attribute.ConstructorArguments[i] = new CustomAttributeArgument(argument.Type, target);
                else if (argument.Value is ArrayType array && array.ElementType.FullName == originalName)
                    attribute.ConstructorArguments[i] = new CustomAttributeArgument(argument.Type, new ArrayType(target));
            }
        }
        assembly.Write(result.InputAssemblyPath.ToString());

        return result;
    }

    public static void UseUnrepresentableKey(TrimmedTestCaseResult result)
    {
        using var assembly = AssemblyDefinition.ReadAssembly(result.InputAssemblyPath.ToString(), new ReaderParameters { InMemory = true });
        CustomAttribute attribute = Assert.Single(assembly.CustomAttributes, attribute =>
            attribute.AttributeType.Name == "TypeMapAttribute`1" &&
            attribute.ConstructorArguments[0].Value is string key && key == "conditional");
        CustomAttributeArgument keyArgument = attribute.ConstructorArguments[0];
        attribute.ConstructorArguments[0] = new CustomAttributeArgument(keyArgument.Type, "invalid\0key");
        assembly.Write(result.InputAssemblyPath.ToString());
    }

    public static void CheckArtifact(string path, bool nativeAot)
    {
        Dictionary<string, Dictionary<string, Map>> groups = ReadArtifact(path, nativeAot);
        string target = nativeAot ? Prefix + "Nested+Target+With,Reserved=\"value\"" : Target;
        Assert.Equal(new[]
        {
            LibraryPrefix + "TypeMapOutputGroup,Maps+Library",
            Prefix + "Empty,test",
            Prefix + "ExternalOnly,test",
            Prefix + "GenericUniverse`1[[System.Int32[],System.Private.CoreLib]],test",
            Prefix + "Group,test",
            Prefix + "Invalid,test",
            Prefix + "ProxyOnly,test"
        }.Select(FormatName), groups.Keys);
        Dictionary<string, Map> group = groups[FormatName(Prefix + "Group,test")];
        Assert.Equal(new[] { "external", "proxy" }, group.Keys);
        Assert.Equal(new[]
        {
            ("conditional", target + ",test"),
            ("keep<&\"\t\r\n", target + ",test")
        }.Select(entry => (entry.Item1, FormatName(entry.Item2))), group["external"].Entries);
        Assert.Equal(new[]
        {
            (Prefix + "Source,test", Prefix + "Nested+Generic`1[[System.Int32[],System.Private.CoreLib]],test")
        }.Select(entry => (FormatName(entry.Item1), FormatName(entry.Item2))), group["proxy"].Entries);

        Dictionary<string, Map> externalOnly = groups[FormatName(Prefix + "ExternalOnly,test")];
        Assert.Equal(new[] { "external" }, externalOnly.Keys);
        Assert.Equal(new[]
        {
            ("", Prefix + "Nested+Generic`1[[System.Int32[],System.Private.CoreLib]],test"),
            ("[Lexample/Outer$Inner;", target + "[],test"),
            ("array", "System.Int32[,],System.Private.CoreLib"),
            ("byref", "System.Int32&,System.Private.CoreLib"),
            ("example/Outer$Inner[0]", target + ",test"),
            ("pointer", "System.Int32*,System.Private.CoreLib"),
            ("unicode/\u0130\u4e2d\U0001f600", target + ",test")
        }.Select(entry => (entry.Item1, FormatName(entry.Item2))), externalOnly["external"].Entries);

        Dictionary<string, Map> proxyOnly = groups[FormatName(Prefix + "ProxyOnly,test")];
        Assert.Equal(new[] { "proxy" }, proxyOnly.Keys);
        Assert.Equal(new[]
        {
            (Prefix + "Nested+Generic`1[[System.Int32[],System.Private.CoreLib]],test", target + ",test"),
            (Prefix + "Source,test", "System.Object,System.Private.CoreLib")
        }.Select(entry => (FormatName(entry.Item1), FormatName(entry.Item2))), proxyOnly["proxy"].Entries);

        Dictionary<string, Map> genericUniverse = groups[FormatName(Prefix + "GenericUniverse`1[[System.Int32[],System.Private.CoreLib]],test")];
        Assert.Equal(new[] { "external" }, genericUniverse.Keys);
        Assert.Equal(new[]
        {
            ("generic-universe", Prefix + "Nested+Generic`1[[System.String[,],System.Private.CoreLib]],test")
        }.Select(entry => (entry.Item1, FormatName(entry.Item2))), genericUniverse["external"].Entries);

        Dictionary<string, Map> empty = groups[FormatName(Prefix + "Empty,test")];
        Assert.Equal(new[] { "external", "proxy" }, empty.Keys);
        Assert.All(empty.Values, map => Assert.Empty(map.Entries));

        Dictionary<string, Map> invalid = groups[FormatName(Prefix + "Invalid,test")];
        Assert.Equal(new[] { "external", "proxy" }, invalid.Keys);
        Assert.Equal(nativeAot ? [] : new[]
        {
            ("duplicate", "System.Object,System.Private.CoreLib"),
            ("duplicate", "System.String,System.Private.CoreLib")
        }.Select(entry => (entry.Item1, FormatName(entry.Item2))), invalid["external"].Entries);
        Assert.Equal(nativeAot ? [] : new[]
        {
            (Prefix + "Source,test", "System.Object,System.Private.CoreLib"),
            (Prefix + "Source,test", "System.String,System.Private.CoreLib")
        }.Select(entry => (FormatName(entry.Item1), FormatName(entry.Item2))), invalid["proxy"].Entries);

        Dictionary<string, Map> copied = groups[FormatName(LibraryPrefix + "TypeMapOutputGroup,Maps+Library")];
        Assert.Equal(new[] { "external" }, copied.Keys);
        Assert.Equal(new[]
        {
            ("copied", LibraryPrefix + "TypeMapOutputTarget,Maps+Library")
        }.Select(entry => (entry.Item1, FormatName(entry.Item2))), copied["external"].Entries);

        string FormatName(string name) => nativeAot ? name : CanonicalName(name);
    }

    private static string CanonicalName(string name) => TypeName.Parse(name).AssemblyQualifiedName;

    private static Dictionary<string, Dictionary<string, Map>> ReadArtifact(string path, bool nativeAot)
    {
        using XmlReader reader = XmlReader.Create(path, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreWhitespace = true
        });
        Assert.Equal(XmlNodeType.Element, reader.MoveToContent());
        Assert.Equal("typemaps", reader.Name);
        Assert.Equal("1", reader.GetAttribute("version"));
        var groups = new Dictionary<string, Dictionary<string, Map>>(StringComparer.Ordinal);
        Dictionary<string, Map>? maps = null;
        Map? map = null;
        string? mapKind = null;
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
                continue;

            switch (reader.Name)
            {
                case "group":
                    Assert.Equal(1, reader.Depth);
                    string? groupType = reader.GetAttribute("type");
                    Assert.NotNull(groupType);
                    maps = new Dictionary<string, Map>(StringComparer.Ordinal);
                    map = null;
                    mapKind = null;
                    Assert.True(groups.TryAdd(FormatName(groupType), maps), $"Duplicate universe '{groupType}'.");
                    break;
                case "external":
                case "proxy":
                    Assert.Equal(2, reader.Depth);
                    Assert.Equal(0, reader.AttributeCount);
                    Assert.NotNull(maps);
                    map = new Map();
                    mapKind = reader.Name;
                    Assert.True(maps.TryAdd(reader.Name, map), $"Duplicate '{reader.Name}' section.");
                    break;
                case "entry":
                    Assert.Equal(3, reader.Depth);
                    Assert.Equal(2, reader.AttributeCount);
                    Assert.NotNull(map);
                    string? key = reader.GetAttribute("key");
                    string? target = reader.GetAttribute("value");
                    Assert.NotNull(key);
                    Assert.NotNull(target);
                    map.Entries.Add((mapKind == "proxy" ? FormatName(key) : key, FormatName(target)));
                    break;
                default:
                    Assert.Fail($"Unexpected element '{reader.Name}'.");
                    break;
            }
        }

        return groups;

        string FormatName(string name) => nativeAot ? name : CanonicalName(name);
    }
}
