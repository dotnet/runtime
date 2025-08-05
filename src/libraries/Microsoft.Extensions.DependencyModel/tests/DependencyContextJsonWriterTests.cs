// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class DependencyContextJsonWriterTests
    {
        public JObject Save(DependencyContext dependencyContext)
        {
            using (var memoryStream = new MemoryStream())
            {
                new DependencyContextWriter().Write(dependencyContext, memoryStream);
                using (var readStream = new MemoryStream(memoryStream.ToArray()))
                {
                    using (var textReader = new StreamReader(readStream))
                    {
                        using (var reader = new JsonTextReader(textReader) { MaxDepth = null })
                        {
                            return JObject.Load(reader);
                        }
                    }
                }
            }
        }

        public DependencyContext Create(
            string target = null,
            string runtime = null,
            bool? isPortable = null,
            CompilationOptions compilationOptions = null,
            CompilationLibrary[] compileLibraries = null,
            RuntimeLibrary[] runtimeLibraries = null,
            IReadOnlyList<RuntimeFallbacks> runtimeGraph = null,
            string runtimeSignature = null)
        {
            return new DependencyContext(new TargetInfo(
                            target ?? "DefaultTarget",
                            runtime ?? string.Empty,
                            runtimeSignature ?? string.Empty,
                            isPortable ?? false),
                            compilationOptions ?? CompilationOptions.Default,
                            compileLibraries ?? new CompilationLibrary[0],
                            runtimeLibraries ?? new RuntimeLibrary[0],
                            runtimeGraph ?? new RuntimeFallbacks[0]
                            );
        }

        [Fact]
        public void DuplicateEntriesThrowArgumentException()
        {
            var context = Create(
                            "Target",
                            "Target/runtime",
                            true,
                            null,
                            runtimeLibraries: new[]
                            {
                                new RuntimeLibrary(
                                        "package",
                                        "DuplicatePackageName",
                                        "1.2.3",
                                        "HASH",
                                        new [] {
                                            new RuntimeAssetGroup(string.Empty, "Banana.dll"),
                                            new RuntimeAssetGroup("win7-x64", "Banana.Win7-x64.dll")
                                        },
                                        new [] {
                                            new RuntimeAssetGroup(string.Empty, "runtimes\\linux\\native\\native.so"),
                                            new RuntimeAssetGroup("win7-x64", "native\\Banana.Win7-x64.so")
                                        },
                                        new [] { new ResourceAssembly("en-US\\Banana.Resource.dll", "en-US")},
                                        new [] {
                                            new Dependency("Fruits.Abstract.dll","2.0.0")
                                        },
                                        true,
                                        "PackagePath",
                                        "PackageHashPath",
                                        "placeHolderManifest.xml"
                                    ),

                                new RuntimeLibrary(
                                        "package",
                                        "DuplicatePackageName",
                                        "1.2.3",
                                        "HASH",
                                        new [] {
                                            new RuntimeAssetGroup(string.Empty, "Banana.dll"),
                                            new RuntimeAssetGroup("win7-x64", "Banana.Win7-x64.dll")
                                        },
                                        new [] {
                                            new RuntimeAssetGroup(string.Empty, "runtimes\\linux\\native\\native.so"),
                                            new RuntimeAssetGroup("win7-x64", "native\\Banana.Win7-x64.so")
                                        },
                                        new [] { new ResourceAssembly("en-US\\Banana.Resource.dll", "en-US")},
                                        new [] {
                                            new Dependency("Fruits.Abstract.dll","2.0.0")
                                        },
                                        true,
                                        "PackagePath",
                                        "PackageHashPath",
                                        "placeHolderManifest.xml"
                                    ),
                            },
                            runtimeGraph: new[]
                            {
                                new RuntimeFallbacks("win7-x64", new [] { "win6", "win5"}),
                                new RuntimeFallbacks("win8-x64", new [] { "win7-x64"}),
                            });

            ArgumentException ex = Assert.Throws<ArgumentException>(() => Save(context));
            Assert.Contains("DuplicatePackageName", ex.Message);
        }

        [Fact]
        public void SavesRuntimeGraph()
        {
            var result = Save(Create(
                            "Target",
                            "Target/runtime",
                            runtimeGraph: new[]
                            {
                                new RuntimeFallbacks("win7-x64", new [] { "win6", "win5"}),
                                new RuntimeFallbacks("win8-x64", new [] { "win7-x64"}),
                            }));

            var rids = result.Should().HaveProperty("runtimes")
                .Subject.Should().BeOfType<JObject>().Subject;

            rids.Should().HaveProperty("win7-x64")
                .Subject.Should().BeOfType<JArray>()
                .Which.Values<string>().Should().BeEquivalentTo(new[] { "win6", "win5" });

            rids.Should().HaveProperty("win8-x64")
                .Subject.Should().BeOfType<JArray>()
                .Which.Values<string>().Should().BeEquivalentTo(new[] { "win7-x64" });
        }

        [Fact]
        public void WritesRuntimeTargetPropertyIfNotPortable()
        {
            var result = Save(Create(
                            "Target",
                            "runtime",
                            false,
                            runtimeSignature: "runtimeSignature")
                            );
            result.Should().HavePropertyAsObject("runtimeTarget")
                .Which.Should().HavePropertyValue("name", "Target/runtime");
            result.Should().HavePropertyAsObject("runtimeTarget")
                .Which.Should().HavePropertyValue("signature", "runtimeSignature");
        }

        [Fact]
        public void WritesMainTargetNameToRuntimeTargetIfPortable()
        {
            var result = Save(Create(
                            "Target",
                            "runtime",
                            true,
                            runtimeSignature: "runtimeSignature")
                            );
            result.Should().HavePropertyAsObject("runtimeTarget")
                .Which.Should().HavePropertyValue("name", "Target");
            result.Should().HavePropertyAsObject("runtimeTarget")
                .Which.Should().HavePropertyValue("signature", "runtimeSignature");
        }

        [Fact]
        public void WritesCompilationLibraries()
        {
            DependencyContext dependencyContext = Create(
                            "Target",
                            "runtime",
                            true,
                            compileLibraries: new[]
                            {
                                new CompilationLibrary(
                                        "package",
                                        "PackageName",
                                        "1.2.3",
                                        "HASH+/==", // verify that '+' and '/' is not getting escaped to workaround bug in older xunit https://github.com/dotnet/runtime/issues/3678
                                        new [] {"Banana.dll"},
                                        new [] {
                                            new Dependency("Fruits.Abstract.dll","2.0.0")
                                        },
                                        true,
                                        "PackagePath",
                                        "PackageHashPath"
                                    )
                            });

            using (var memoryStream = new MemoryStream())
            {
                new DependencyContextWriter().Write(dependencyContext, memoryStream);
                var reader = new Utf8JsonReader(memoryStream.ToArray());
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("sha512"))
                    {
                        Assert.True(reader.Read());
                        Assert.Equal("HASH+/==", Encoding.UTF8.GetString(reader.ValueSpan.ToArray()));
                    }
                }
            }

            JObject result = Save(dependencyContext);

            // targets
            var targets = result.Should().HavePropertyAsObject("targets").Subject;
            var target = targets.Should().HavePropertyAsObject("Target").Subject;
            var library = target.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;
            var dependencies = library.Should().HavePropertyAsObject("dependencies").Subject;
            dependencies.Should().HavePropertyValue("Fruits.Abstract.dll", "2.0.0");
            library.Should().HavePropertyAsObject("compile")
                .Subject.Should().HaveProperty("Banana.dll");

            //libraries
            var libraries = result.Should().HavePropertyAsObject("libraries").Subject;
            library = libraries.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;
            library.Should().HavePropertyValue("sha512", "HASH+/==");
            library.Should().HavePropertyValue("type", "package");
            library.Should().HavePropertyValue("serviceable", true);
            library.Should().HavePropertyValue("path", "PackagePath");
            library.Should().HavePropertyValue("hashPath", "PackageHashPath");
        }

        [Fact]
        public void ExcludesPathAndHashPath()
        {
            var result = Save(Create(
                            "Target",
                            "runtime",
                            true,
                            compileLibraries: new[]
                            {
                                new CompilationLibrary(
                                        "package",
                                        "PackageName",
                                        "1.2.3",
                                        "HASH",
                                        new [] {"Banana.dll"},
                                        new [] {
                                            new Dependency("Fruits.Abstract.dll","2.0.0")
                                        },
                                        true,
                                        path: null,
                                        hashPath: null
                                    )
                            }));

            // targets
            var targets = result.Should().HavePropertyAsObject("targets").Subject;
            var target = targets.Should().HavePropertyAsObject("Target").Subject;
            var library = target.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;
            var dependencies = library.Should().HavePropertyAsObject("dependencies").Subject;
            dependencies.Should().HavePropertyValue("Fruits.Abstract.dll", "2.0.0");
            library.Should().HavePropertyAsObject("compile")
                .Subject.Should().HaveProperty("Banana.dll");

            //libraries
            var libraries = result.Should().HavePropertyAsObject("libraries").Subject;
            library = libraries.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;
            library.Should().HavePropertyValue("sha512", "HASH");
            library.Should().HavePropertyValue("type", "package");
            library.Should().HavePropertyValue("serviceable", true);
            library.Should().NotHaveProperty("path");
            library.Should().NotHaveProperty("hashPath");
        }

        [Fact]
        public void WritesRuntimePackLibrariesWithFrameworkName()
        {
            var result = Save(Create(
                "Target",
                "win-x86",
                false,
                runtimeLibraries: new[]
                {
                    new RuntimeLibrary(
                        "runtimepack",
                        "RuntimePackName",
                        "1.2.3",
                        "HASH",
                        new [] {
                            new RuntimeAssetGroup(
                                string.Empty,
                                new []
                                {
                                    new RuntimeFile("System.Private.CoreLib.dll", "2.3.4", "3.4.5"),
                                }),
                        },
                        new [] {
                            new RuntimeAssetGroup(
                                string.Empty,
                                new []
                                {
                                    new RuntimeFile("coreclr.dll", "4.5.6", "5.6.7"),
                                }),
                        },
                        new ResourceAssembly[0],
                        new Dependency[0],
                        false,
                        "PackagePath",
                        "PackageHashPath",
                        "placeHolderManifest.xml"
                    ),
                }));

            // targets
            var targets = result.Should().HavePropertyAsObject("targets").Subject;
            var target = targets.Should().HavePropertyAsObject("Target/win-x86").Subject;
            var library = target.Should().HavePropertyAsObject("RuntimePackName/1.2.3").Subject;
            library.Should().NotHaveProperty("dependencies");
            library.Should().NotHaveProperty("resources");

            library.Should().HavePropertyAsObject("runtime")
                .Subject.Should().HaveProperty("System.Private.CoreLib.dll");
            library.Should().HavePropertyAsObject("native")
                .Subject.Should().HaveProperty("coreclr.dll");

            //libraries
            var libraries = result.Should().HavePropertyAsObject("libraries").Subject;
            library = libraries.Should().HavePropertyAsObject("RuntimePackName/1.2.3").Subject;
            library.Should().HavePropertyValue("sha512", "HASH");
            library.Should().HavePropertyValue("type", "runtimepack");
            library.Should().HavePropertyValue("serviceable", false);
            library.Should().HavePropertyValue("path", "PackagePath");
            library.Should().HavePropertyValue("hashPath", "PackageHashPath");
            library.Should().HavePropertyValue("runtimeStoreManifestName", "placeHolderManifest.xml");
        }

        [Fact]
        public void MergesRuntimeAndCompileLibrariesForPortable()
        {
            var result = Save(Create(
                            "Target",
                            "runtime",
                            true,
                            compileLibraries: new[]
                            {
                                 new CompilationLibrary(
                                        "package",
                                        "PackageName",
                                        "1.2.3",
                                        "HASH",
                                        new [] { "ref/Banana.dll" },
                                        new [] {
                                            new Dependency("Fruits.Abstract.dll","2.0.0")
                                        },
                                        true,
                                        "PackagePath",
                                        "PackageHashPath"
                                    )
                            },
                            runtimeLibraries: new[]
                            {
                                new RuntimeLibrary(
                                        "package",
                                        "PackageName",
                                        "1.2.3",
                                        "HASH",
                                        new [] {
                                            new RuntimeAssetGroup(string.Empty, "Banana.dll"),
                                            new RuntimeAssetGroup("win7-x64", "Banana.Win7-x64.dll")
                                        },
                                        new [] {
                                            new RuntimeAssetGroup(string.Empty, "native.dll"),
                                            new RuntimeAssetGroup("win7-x64", "Banana.Win7-x64.so")
                                        },
                                        new ResourceAssembly[] {},
                                        new [] {
                                            new Dependency("Fruits.Abstract.dll","2.0.0")
                                        },
                                        true,
                                        "PackagePath",
                                        "PackageHashPath",
                                        "placeHolderManifest.xml"
                                    ),
                            }));

            // targets
            var targets = result.Should().HavePropertyAsObject("targets").Subject;
            var target = targets.Should().HavePropertyAsObject("Target").Subject;
            var library = target.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;
            var dependencies = library.Should().HavePropertyAsObject("dependencies").Subject;
            dependencies.Should().HavePropertyValue("Fruits.Abstract.dll", "2.0.0");

            library.Should().HavePropertyAsObject("runtime")
                .Subject.Should().HaveProperty("Banana.dll");
            library.Should().HavePropertyAsObject("native")
                .Subject.Should().HaveProperty("native.dll");

            library.Should().HavePropertyAsObject("compile")
              .Subject.Should().HaveProperty("ref/Banana.dll");

            var runtimeTargets = library.Should().HavePropertyAsObject("runtimeTargets").Subject;

            var runtimeAssembly = runtimeTargets.Should().HavePropertyAsObject("Banana.Win7-x64.dll").Subject;
            runtimeAssembly.Should().HavePropertyValue("rid", "win7-x64");
            runtimeAssembly.Should().HavePropertyValue("assetType", "runtime");

            var nativeLibrary = runtimeTargets.Should().HavePropertyAsObject("Banana.Win7-x64.so").Subject;
            nativeLibrary.Should().HavePropertyValue("rid", "win7-x64");
            nativeLibrary.Should().HavePropertyValue("assetType", "native");

            //libraries
            var libraries = result.Should().HavePropertyAsObject("libraries").Subject;
            library = libraries.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;
            library.Should().HavePropertyValue("sha512", "HASH");
            library.Should().HavePropertyValue("type", "package");
            library.Should().HavePropertyValue("serviceable", true);
            library.Should().HavePropertyValue("path", "PackagePath");
            library.Should().HavePropertyValue("hashPath", "PackageHashPath");
            library.Should().HavePropertyValue("runtimeStoreManifestName", "placeHolderManifest.xml");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WritesRuntimeAssembly_PathOnly(bool isPortable)
        {
            List<RuntimeAssetGroup> groups = [ new RuntimeAssetGroup(string.Empty, "Banana.dll") ];
            if (isPortable)
                groups.Add(new RuntimeAssetGroup("win-x64", "Banana.win-x64.dll"));

            WritesRuntimeAssembly(isPortable, groups);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WritesRuntimeAssembly_Versions(bool isPortable)
        {
            List<RuntimeAssetGroup> groups = [
                new RuntimeAssetGroup(string.Empty, [new RuntimeFile("Banana.dll", "1.2.3", "7.8.9")])
            ];
            if (isPortable)
                groups.Add(new RuntimeAssetGroup("win-x64", [ new RuntimeFile("Banana.win-x64.dll", "1.2.3", "7.8.9") ]));

            WritesRuntimeAssembly(isPortable, groups);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WritesRuntimeAssembly_LocalPath(bool isPortable)
        {
            List<RuntimeAssetGroup> groups = [
                new RuntimeAssetGroup(string.Empty, [ new RuntimeFile("Banana.dll", "1.2.3", "7.8.9", "local/path/Banana.dll")])
            ];
            if (isPortable)
                groups.Add(new RuntimeAssetGroup("win-x64", [new RuntimeFile("Banana.win-x64.dll", "1.2.3", "7.8.9", "local/path/Banana.win-x64.dll")]));

            WritesRuntimeAssembly(isPortable, groups);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WritesNativeLibrary_PathOnly(bool isPortable)
        {
            List<RuntimeAssetGroup> groups = [new RuntimeAssetGroup(string.Empty, "native.so")];
            if (isPortable)
                groups.Add(new RuntimeAssetGroup("linux-x64", "runtimes/linux-x64/native/native.linux-64.so"));

            WritesNativeLibrary(isPortable, groups);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WritesNativeLibrary_LocalPath(bool isPortable)
        {
            List<RuntimeAssetGroup> groups = [
                new RuntimeAssetGroup(string.Empty, [new RuntimeFile("native.so", null, null, "local/path/native.so")])
            ];
            if (isPortable)
                groups.Add(new RuntimeAssetGroup("linux-x64", [new RuntimeFile("runtimes/linux-x64/native/native.linux-64.so", null, null, "local/path/native.so")]));

            WritesNativeLibrary(isPortable, groups);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WritesResourceAssembly_PathOnly(bool isPortable)
        {
            ResourceAssembly resourceAssembly = new ResourceAssembly("fr/Fruits.resources.dll", "fr");
            WritesResourceAssembly(isPortable, [resourceAssembly]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WritesResourceAssembly_LocalPath(bool isPortable)
        {
            ResourceAssembly resourceAssembly = new ResourceAssembly("fr/Fruits.resources.dll", "fr", "local/path/fr/Fruits.resources.dll");
            WritesResourceAssembly(isPortable, [resourceAssembly]);
        }

        private void WritesRuntimeAssembly(bool isPortable, IReadOnlyList<RuntimeAssetGroup> runtimeAssemblies)
            => WritesRuntimeLibrary(isPortable, runtimeAssemblies, [], []);

        private void WritesNativeLibrary(bool isPortable, IReadOnlyList<RuntimeAssetGroup> nativeLibraries)
            => WritesRuntimeLibrary(isPortable, [], nativeLibraries, []);

        private void WritesResourceAssembly(bool isPortable, ResourceAssembly[] resourceAssemblies)
            => WritesRuntimeLibrary(isPortable, [], [], resourceAssemblies);

        private void WritesRuntimeLibrary(bool isPortable, IReadOnlyList<RuntimeAssetGroup> runtimeAssemblies, IReadOnlyList<RuntimeAssetGroup> nativeLibraries, ResourceAssembly[] resourceAssemblies)
        {
            var result = Save(Create(
                            "Target",
                            "runtime",
                            isPortable,
                            runtimeLibraries: new[]
                            {
                                new RuntimeLibrary(
                                        "package",
                                        "PackageName",
                                        "1.2.3",
                                        "HASH",
                                        runtimeAssemblies,
                                        nativeLibraries,
                                        resourceAssemblies,
                                        dependencies: [],
                                        serviceable: true,
                                        "PackagePath",
                                        "PackageHashPath"
                                    ),
                            }));

            // targets
            var targets = result.Should().HavePropertyAsObject("targets").Subject;
            var target = targets.Should().HavePropertyAsObject($"Target{(isPortable ? "" : "/runtime")}").Subject;
            var library = target.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;

            ValidateRuntimeAssetGroups(library, runtimeAssemblies, DependencyContextStrings.RuntimeAssetType);
            ValidateRuntimeAssetGroups(library, nativeLibraries, DependencyContextStrings.NativeAssetType);

            if (resourceAssemblies.Length == 0)
            {
                library.Should().NotHaveProperty(DependencyContextStrings.ResourceAssembliesPropertyName);
            }
            else
            {
                var resources = library.Should().HavePropertyAsObject(DependencyContextStrings.ResourceAssembliesPropertyName).Subject;
                foreach (var resource in resourceAssemblies)
                {
                    var resourceJson = resources.Should().HavePropertyAsObject(resource.Path).Subject;
                    resourceJson.Should().HavePropertyValue(DependencyContextStrings.LocalePropertyName, resource.Locale);
                    if (string.IsNullOrEmpty(resource.LocalPath))
                    {
                        resourceJson.Should().NotHaveProperty(DependencyContextStrings.LocalPathPropertyName);
                    }
                    else
                    {
                        resourceJson.Should().HavePropertyValue(DependencyContextStrings.LocalPathPropertyName, resource.LocalPath);
                    }
                }
            }
        }

        private void ValidateRuntimeAssetGroups(JObject library, IReadOnlyList<RuntimeAssetGroup> groups, string assetType)
        {
            if (groups.Count == 0)
                library.Should().NotHaveProperty(assetType);

            foreach (var group in groups)
            {
                bool hasRuntimeId = !string.IsNullOrEmpty(group.Runtime);
                var files = library.Should().HavePropertyAsObject(hasRuntimeId ? DependencyContextStrings.RuntimeTargetsPropertyName : assetType).Subject;
                foreach (var file in group.RuntimeFiles)
                {
                    var fileJson = files.Should().HavePropertyAsObject(file.Path).Subject;
                    ValidateRuntimeFile(file, fileJson);
                    if (hasRuntimeId)
                    {
                        fileJson.Should().HavePropertyValue(DependencyContextStrings.RidPropertyName, group.Runtime);
                        fileJson.Should().HavePropertyValue(DependencyContextStrings.AssetTypePropertyName, assetType);
                    }
                    else
                    {
                        fileJson.Should().NotHaveProperty(DependencyContextStrings.RidPropertyName);
                        fileJson.Should().NotHaveProperty(DependencyContextStrings.AssetTypePropertyName);
                    }
                }
            }
        }

        private void ValidateRuntimeFile(RuntimeFile file, JObject fileJson)
        {
            if (string.IsNullOrEmpty(file.AssemblyVersion))
            {
                fileJson.Should().NotHaveProperty(DependencyContextStrings.AssemblyVersionPropertyName);
            }
            else
            {
                fileJson.Should().HavePropertyValue(DependencyContextStrings.AssemblyVersionPropertyName, file.AssemblyVersion);
            }

            if (string.IsNullOrEmpty(file.FileVersion))
            {
                fileJson.Should().NotHaveProperty(DependencyContextStrings.FileVersionPropertyName);
            }
            else
            {
                fileJson.Should().HavePropertyValue(DependencyContextStrings.FileVersionPropertyName, file.FileVersion);
            }

            if (string.IsNullOrEmpty(file.LocalPath))
            {
                fileJson.Should().NotHaveProperty(DependencyContextStrings.LocalPathPropertyName);
            }
            else
            {
                fileJson.Should().HavePropertyValue(DependencyContextStrings.LocalPathPropertyName, file.LocalPath);
            }
        }

        [Fact]
        public void WritesPlaceholderRuntimeTargetsForEmptyGroups()
        {
            var result = Save(Create(
                            "Target",
                            "runtime",
                            true,
                            runtimeLibraries: new[]
                            {
                                new RuntimeLibrary(
                                        "package",
                                        "PackageName",
                                        "1.2.3",
                                        "HASH",
                                        new [] {
                                            new RuntimeAssetGroup("win7-x64"),
                                            new RuntimeAssetGroup("win7-x86", "lib\\x86Support.dll")
                                        },
                                        new [] {
                                            new RuntimeAssetGroup("linux-x64"),
                                            new RuntimeAssetGroup("osx", "native\\OSXSupport.dylib")
                                        },
                                        new ResourceAssembly[] { },
                                        new Dependency[] { },
                                        true,
                                        "PackagePath",
                                        "PackageHashPath"
                                    ),
                            }));

            // targets
            var targets = result.Should().HavePropertyAsObject("targets").Subject;
            var target = targets.Should().HavePropertyAsObject("Target").Subject;
            var library = target.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;

            var runtimeTargets = library.Should().HavePropertyAsObject("runtimeTargets").Subject;

            var winPlaceholder = runtimeTargets.Should().HavePropertyAsObject("runtime/win7-x64/lib/_._").Subject;
            winPlaceholder.Should().HavePropertyValue("rid", "win7-x64");
            winPlaceholder.Should().HavePropertyValue("assetType", "runtime");

            var winRuntime = runtimeTargets.Should().HavePropertyAsObject("lib/x86Support.dll").Subject;
            winPlaceholder.Should().HavePropertyValue("rid", "win7-x64");
            winPlaceholder.Should().HavePropertyValue("assetType", "runtime");

            var linuxPlaceholder = runtimeTargets.Should().HavePropertyAsObject("runtime/linux-x64/native/_._").Subject;
            linuxPlaceholder.Should().HavePropertyValue("rid", "linux-x64");
            linuxPlaceholder.Should().HavePropertyValue("assetType", "native");

            var osxNative = runtimeTargets.Should().HavePropertyAsObject("native/OSXSupport.dylib").Subject;
            osxNative.Should().HavePropertyValue("rid", "osx");
            osxNative.Should().HavePropertyValue("assetType", "native");
        }

        [Fact]
        public void WriteCompilationOnlyAttributeIfOnlyCompilationLibraryProvided()
        {
            var result = Save(Create(
                            "Target",
                            "runtime",
                            true,
                            compileLibraries: new[]
                            {
                                 new CompilationLibrary(
                                        "package",
                                        "PackageName",
                                        "1.2.3",
                                        "HASH",
                                        new [] { "ref/Banana.dll" },
                                        new [] {
                                            new Dependency("Fruits.Abstract.dll","2.0.0")
                                        },
                                        true,
                                        "PackagePath",
                                        "PackageHashPath"
                                    )
                            }));

            // targets
            var targets = result.Should().HavePropertyAsObject("targets").Subject;
            var target = targets.Should().HavePropertyAsObject("Target").Subject;
            var library = target.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;
            library.Should().HavePropertyValue("compileOnly", true);
        }

        [Fact]
        public void WritesCompilationOptions()
        {
            var result = Save(Create(compilationOptions: new CompilationOptions(
                defines: new[] { "MY", "DEFINES" },
                languageVersion: "C#8",
                platform: "Platform",
                allowUnsafe: true,
                warningsAsErrors: true,
                optimize: true,
                keyFile: "Key.snk",
                delaySign: true,
                debugType: null,
                publicSign: true,
                emitEntryPoint: true,
                generateXmlDocumentation: true)));

            var options = result.Should().HavePropertyAsObject("compilationOptions").Subject;
            options.Should().HavePropertyValue("allowUnsafe", true);
            options.Should().HavePropertyValue("delaySign", true);
            options.Should().HavePropertyValue("emitEntryPoint", true);
            options.Should().HavePropertyValue("xmlDoc", true);
            options.Should().HavePropertyValue("publicSign", true);
            options.Should().HavePropertyValue("optimize", true);
            options.Should().HavePropertyValue("warningsAsErrors", true);
            options.Should().HavePropertyValue("allowUnsafe", true);
            options.Should().HavePropertyValue("languageVersion", "C#8");
            options.Should().HavePropertyValue("keyFile", "Key.snk");
            options.Should().HaveProperty("defines")
                .Subject.Values<string>().Should().BeEquivalentTo(new[] { "MY", "DEFINES" });
        }

        [Fact]
        public void WriteDoesNotDisposeStream()
        {
            DependencyContext context = Create(
                "Target",
                "Target/runtime",
                runtimeGraph: new[]
                {
                    new RuntimeFallbacks("win7-x64", new [] { "win6", "win5"}),
                    new RuntimeFallbacks("win8-x64", new [] { "win7-x64"}),
                });

            DisposeAwareMemoryStream stream = new DisposeAwareMemoryStream();
            using (stream)
            {
                new DependencyContextWriter().Write(context, stream);
                Assert.False(stream.IsDisposed);
            }

            Assert.True(stream.IsDisposed);
        }

        private class DisposeAwareMemoryStream : MemoryStream
        {
            public bool IsDisposed { get; set; }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;

                base.Dispose(disposing);
            }
        }
    }
}
