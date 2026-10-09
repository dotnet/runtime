// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;

using Microsoft.Diagnostics.Tools.Pgo;

using Xunit;

namespace DotNetPgo.Tests
{
    public sealed class MethodListMibcTests
    {
        private static readonly string s_dotnetPgoPath = typeof(PgoFileType).Assembly.Location;
        private static readonly string[] s_platformReferences =
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);
        private static readonly string s_testAssemblyPath = typeof(MethodListMibcTests).Assembly.Location;
        private static readonly string s_testAssemblyName = typeof(MethodListMibcTests).Assembly.GetName().Name;

        [Fact]
        public void CreatesDeterministicMibcAndRoundTripsThroughDump()
        {
            using var directory = new TemporaryDirectory();
            string firstMethodList = directory.WriteJson(
                "first.json",
                CreateDocument(
                    Method(nameof(MethodListTargets.Generic), [QualifiedName(typeof(int))], [QualifiedName(typeof(int))]),
                    Method("Hidden"),
                    Method(nameof(MethodListTargets.Overloaded), parameterTypes: [QualifiedName(typeof(string))])));
            string secondMethodList = directory.WriteJson(
                "second.json",
                CreateDocument(
                    Method(nameof(MethodListTargets.Overloaded), parameterTypes: [QualifiedName(typeof(string))]),
                    Method("Hidden"),
                    Method(nameof(MethodListTargets.Generic), [QualifiedName(typeof(int))], [QualifiedName(typeof(int))])));

            string mibcPath = Path.Combine(directory.Path, "methods.mibc");

            CommandResult firstCreate = RunCreate(firstMethodList, mibcPath);
            AssertSuccess(firstCreate);
            Assert.Contains($"Validated {mibcPath}", firstCreate.StandardOutput, StringComparison.Ordinal);
            byte[] firstMibc = File.ReadAllBytes(mibcPath);
            AssertSuccess(RunCreate(secondMethodList, mibcPath));
            Assert.Equal(firstMibc, File.ReadAllBytes(mibcPath));

            string dumpPath = Path.Combine(directory.Path, "dump.json");
            AssertSuccess(Run("dump", "--input", mibcPath, "--output", dumpPath));

            using JsonDocument dump = JsonDocument.Parse(File.ReadAllText(dumpPath));
            JsonElement methods = dump.RootElement.GetProperty("Methods");
            Assert.Equal(3, methods.GetArrayLength());
            Assert.All(methods.EnumerateArray(), method =>
            {
                Assert.False(method.TryGetProperty("CallWeights", out _));
                Assert.False(method.TryGetProperty("ExclusiveWeight", out _));
                Assert.False(method.TryGetProperty("InstrumentationData", out _));
            });
        }

        [Fact]
        public void RejectsAmbiguousMethod()
        {
            using var directory = new TemporaryDirectory();
            string methodList = directory.WriteJson(
                "ambiguous.json",
                CreateDocument(Method(nameof(MethodListTargets.Overloaded))));

            CommandResult result = RunCreate(methodList, Path.Combine(directory.Path, "ambiguous.mibc"));

            AssertFailure(result, "is ambiguous");
        }

        [Fact]
        public void RejectsDuplicateMethod()
        {
            using var directory = new TemporaryDirectory();
            Dictionary<string, object> method = Method("Hidden");
            string methodList = directory.WriteJson("duplicate.json", CreateDocument(method, method));

            CommandResult result = RunCreate(methodList, Path.Combine(directory.Path, "duplicate.mibc"));

            AssertFailure(result, "duplicate entry");
        }

        [Fact]
        public void RejectsInvalidMethodAndGenericArity()
        {
            using var directory = new TemporaryDirectory();
            string missingMethodList = directory.WriteJson(
                "missing-method.json",
                CreateDocument(Method("Missing")));
            string wrongArityMethodList = directory.WriteJson(
                "wrong-arity.json",
                CreateDocument(Method(nameof(MethodListTargets.Generic))));

            AssertFailure(
                RunCreate(missingMethodList, Path.Combine(directory.Path, "missing-method.mibc")),
                "Unable to resolve method");
            AssertFailure(
                RunCreate(wrongArityMethodList, Path.Combine(directory.Path, "wrong-arity.mibc")),
                "generic arity 0");
        }

        [Fact]
        public void RejectsInvalidJsonMissingTypeAndReference()
        {
            using var directory = new TemporaryDirectory();
            string invalidJson = Path.Combine(directory.Path, "invalid.json");
            File.WriteAllText(invalidJson, "{");
            string missingType = directory.WriteJson(
                "missing-type.json",
                CreateDocument(
                    new Dictionary<string, object>
                    {
                        ["type"] = $"Missing.Type, {s_testAssemblyName}",
                        ["name"] = "Method",
                    }));

            AssertFailure(RunCreate(invalidJson, Path.Combine(directory.Path, "invalid.mibc")), "JSON");
            AssertFailure(RunCreate(missingType, Path.Combine(directory.Path, "missing-type.mibc")), "Unable to resolve type");

            string validMethod = directory.WriteJson(
                "missing-reference.json",
                CreateDocument(Method("Hidden")));
            var arguments = new List<string>
            {
                "create-mibc-from-method-list",
                "--method-list", validMethod,
            };
            AddReferences(arguments, includeTestAssembly: false);
            arguments.AddRange(
            [
                "--output", Path.Combine(directory.Path, "missing-reference.mibc"),
                "--compressed", "false",
            ]);
            AssertFailure(Run(arguments.ToArray()), $"No reference was supplied for assembly '{s_testAssemblyName}'");
        }

        [Fact]
        public void RejectsNonMibcOutput()
        {
            using var directory = new TemporaryDirectory();
            string methodList = directory.WriteJson(
                "method.json",
                CreateDocument(Method("Hidden")));

            AssertFailure(RunCreate(methodList, Path.Combine(directory.Path, "output.bin")), "must end with '.mibc'");
        }

        [Fact]
        public void RejectsUnqualifiedConstructedGenericArgument()
        {
            using var directory = new TemporaryDirectory();
            string methodList = directory.WriteJson(
                "unqualified-generic-argument.json",
                CreateDocument(
                    new Dictionary<string, object>
                    {
                        ["type"] = "System.Collections.Generic.List`1[[System.String]], System.Private.CoreLib",
                        ["name"] = "Add",
                        ["parameterTypes"] = new[] { QualifiedName(typeof(string)) },
                    }));

            AssertFailure(
                RunCreate(methodList, Path.Combine(directory.Path, "unqualified-generic-argument.mibc")),
                "must be assembly-qualified");
        }

        private static void AssertFailure(CommandResult result, string expectedError)
        {
            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains(expectedError, result.StandardError, StringComparison.OrdinalIgnoreCase);
        }

        private static void AssertSuccess(CommandResult result)
        {
            Assert.True(
                result.ExitCode == 0,
                $"Command failed with exit code {result.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{result.StandardOutput}{Environment.NewLine}stderr:{Environment.NewLine}{result.StandardError}");
        }

        private static Dictionary<string, object> CreateDocument(params Dictionary<string, object>[] methods)
        {
            return new Dictionary<string, object>
            {
                ["runtime"] = "CoreCLR",
                ["os"] = "test",
                ["architecture"] = "test",
                ["methods"] = methods,
            };
        }

        private static Dictionary<string, object> Method(
            string name,
            string[] genericArguments = null,
            string[] parameterTypes = null)
        {
            var method = new Dictionary<string, object>
            {
                ["type"] = QualifiedName(typeof(MethodListTargets)),
                ["name"] = name,
            };

            if (genericArguments is not null)
            {
                method["genericArguments"] = genericArguments;
            }

            if (parameterTypes is not null)
            {
                method["parameterTypes"] = parameterTypes;
            }

            return method;
        }

        private static string QualifiedName(Type type)
        {
            return type.AssemblyQualifiedName;
        }

        private static CommandResult RunCreate(string methodList, string output)
        {
            var arguments = new List<string>
            {
                "create-mibc-from-method-list",
                "--method-list", methodList,
            };
            AddReferences(arguments, includeTestAssembly: true);
            arguments.AddRange(
            [
                "--output", output,
            ]);

            return Run(arguments.ToArray());
        }

        private static void AddReferences(List<string> arguments, bool includeTestAssembly)
        {
            var assemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string reference in s_platformReferences)
            {
                string assemblyName = AssemblyName.GetAssemblyName(reference).Name;
                if ((!includeTestAssembly && assemblyName == s_testAssemblyName) || !assemblyNames.Add(assemblyName))
                {
                    continue;
                }

                arguments.Add("--reference");
                arguments.Add(reference);
            }

            if (includeTestAssembly && assemblyNames.Add(s_testAssemblyName))
            {
                arguments.Add("--reference");
                arguments.Add(s_testAssemblyPath);
            }
        }

        private static CommandResult Run(params string[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add(s_dotnetPgoPath);
            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using Process process = Process.Start(startInfo);
            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return new CommandResult(process.ExitCode, standardOutput, standardError);
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            public TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public void Dispose()
            {
                Directory.Delete(Path, recursive: true);
            }

            public string WriteJson(string fileName, object value)
            {
                string path = System.IO.Path.Combine(Path, fileName);
                File.WriteAllText(path, JsonSerializer.Serialize(value));
                return path;
            }
        }

        private sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);
    }

    internal static class MethodListTargets
    {
        internal static T Generic<T>(T value) => value;

        private static void Hidden()
        {
        }

        internal static void Overloaded(int value)
        {
        }

        internal static void Overloaded(string value)
        {
        }
    }
}
