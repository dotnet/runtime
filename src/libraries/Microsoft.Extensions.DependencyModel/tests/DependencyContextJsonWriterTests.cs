// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                        using (var reader = new JsonTextReader(textReader))
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
                .Which.Values<string>().ShouldBeEquivalentTo(new[] { "win6", "win5" });

            rids.Should().HaveProperty("win8-x64")
                .Subject.Should().BeOfType<JArray>()
                .Which.Values<string>().ShouldBeEquivalentTo(new[] { "win7-x64" });
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
                                        "HASH+/==", // verify that '+' and '/' is not getting escaped to workaround bug in older xunit https://github.com/dotnet/core-setup/issues/7137
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
        public void WritesRuntimeLibrariesToRuntimeTarget()
        {
            var group = new RuntimeAssetGroup("win7-x64", "Banana.Win7-x64.dll");
            WritesRuntimeLibrariesToRuntimeTargetCore(group);
        }

        [Fact]
        public void WritesRuntimeLibrariesToRuntimeTargetWithAssemblyVersions()
        {
            RuntimeFile[] runtimeFile = { new RuntimeFile("Banana.Win7-x64.dll", "1.2.3", "7.8.9") };
            var group = new RuntimeAssetGroup("win7-x64", runtimeFile);

            var runtimeAssembly = WritesRuntimeLibrariesToRuntimeTargetCore(group);
            runtimeAssembly.Should().HavePropertyValue("assemblyVersion", "1.2.3");
            runtimeAssembly.Should().HavePropertyValue("fileVersion", "7.8.9");
        }

        private JObject WritesRuntimeLibrariesToRuntimeTargetCore(RuntimeAssetGroup group)
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
                                            new RuntimeAssetGroup(string.Empty, "Banana.dll"),
                                            group
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
                .Subject.Should().HaveProperty("runtimes/linux/native/native.so");

            var runtimeTargets = library.Should().HavePropertyAsObject("runtimeTargets").Subject;

            var runtimeAssembly = runtimeTargets.Should().HavePropertyAsObject("Banana.Win7-x64.dll").Subject;
            runtimeAssembly.Should().HavePropertyValue("rid", "win7-x64");
            runtimeAssembly.Should().HavePropertyValue("assetType", "runtime");

            var nativeLibrary = runtimeTargets.Should().HavePropertyAsObject("native/Banana.Win7-x64.so").Subject;
            nativeLibrary.Should().HavePropertyValue("rid", "win7-x64");
            nativeLibrary.Should().HavePropertyValue("assetType", "native");

            var resourceAssemblies = library.Should().HavePropertyAsObject("resources").Subject;
            var resourceAssembly = resourceAssemblies.Should().HavePropertyAsObject("en-US/Banana.Resource.dll").Subject;
            resourceAssembly.Should().HavePropertyValue("locale", "en-US");

            //libraries
            var libraries = result.Should().HavePropertyAsObject("libraries").Subject;
            library = libraries.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;
            library.Should().HavePropertyValue("sha512", "HASH");
            library.Should().HavePropertyValue("type", "package");
            library.Should().HavePropertyValue("serviceable", true);
            library.Should().HavePropertyValue("path", "PackagePath");
            library.Should().HavePropertyValue("hashPath", "PackageHashPath");
            library.Should().HavePropertyValue("runtimeStoreManifestName", "placeHolderManifest.xml");

            return runtimeAssembly;
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

        [Fact]
        public void WritesRuntimeTargetForNonPortableLegacy()
        {
            var group = new RuntimeAssetGroup(string.Empty, "Banana.dll");
            var assetGroup = WritesRuntimeTarget(group);

            var files = assetGroup.Should().HavePropertyAsObject("runtime").Subject;
            files.Should().HaveProperty("Banana.dll");
        }

        [Fact]
        public void WritesRuntimeTargetForNonPortable()
        {
            RuntimeFile[] runtimeFiles = { new RuntimeFile("Banana.dll", "1.2.3", "7.8.9") };
            var group = new RuntimeAssetGroup(string.Empty, runtimeFiles);
            var assetGroup = WritesRuntimeTarget(group);

            var files = assetGroup.Should().HavePropertyAsObject("runtime").Subject;
            var file = files.Should().HavePropertyAsObject("Banana.dll").Subject;
            file.Should().HavePropertyValue("assemblyVersion", "1.2.3");
            file.Should().HavePropertyValue("fileVersion", "7.8.9");
        }

        private JObject WritesRuntimeTarget(RuntimeAssetGroup group)
        {
            var result = Save(Create(
                            "Target",
                            "runtime",
                            false,
                            runtimeLibraries: new[]
                            {
                                new RuntimeLibrary(
                                        "package",
                                        "PackageName",
                                        "1.2.3",
                                        "HASH",
                                        new [] {
                                            group
                                        },
                                        new [] {
                                            new RuntimeAssetGroup(string.Empty, "runtimes\\osx\\native\\native.dylib")
                                        },
                                        new ResourceAssembly[] {},
                                        new [] {
                                            new Dependency("Fruits.Abstract.dll","2.0.0")
                                        },
                                        true,
                                        "PackagePath",
                                        "PackageHashPath"
                                    ),
                            }));

            // targets
            var targets = result.Should().HavePropertyAsObject("targets").Subject;
            var target = targets.Should().HavePropertyAsObject("Target/runtime").Subject;
            var assetGroup = target.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;
            var dependencies = assetGroup.Should().HavePropertyAsObject("dependencies").Subject;
            dependencies.Should().HavePropertyValue("Fruits.Abstract.dll", "2.0.0");
            assetGroup.Should().HavePropertyAsObject("native")
                .Subject.Should().HaveProperty("runtimes/osx/native/native.dylib");

            //libraries
            var libraries = result.Should().HavePropertyAsObject("libraries").Subject;
            var library = libraries.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;
            library.Should().HavePropertyValue("sha512", "HASH");
            library.Should().HavePropertyValue("type", "package");
            library.Should().HavePropertyValue("serviceable", true);
            library.Should().HavePropertyValue("path", "PackagePath");
            library.Should().HavePropertyValue("hashPath", "PackageHashPath");

            return assetGroup;
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
        public void WritesResourceAssembliesForNonPortable()
        {
            var result = Save(Create(
                            "Target",
                            "runtime",
                            false,
                            runtimeLibraries: new[]
                            {
                                new RuntimeLibrary(
                                        "package",
                                        "PackageName",
                                        "1.2.3",
                                        "HASH",
                                        new RuntimeAssetGroup[] { },
                                        new RuntimeAssetGroup[] { },
                                        new []
                                        {
                                            new ResourceAssembly("en-US/Fruits.resources.dll", "en-US")
                                        },
                                        new Dependency[] { },
                                        true,
                                        "PackagePath",
                                        "PackageHashPath"
                                    ),
                            }));

            var targets = result.Should().HavePropertyAsObject("targets").Subject;
            var target = targets.Should().HavePropertyAsObject("Target/runtime").Subject;
            var library = target.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;
            var resources = library.Should().HavePropertyAsObject("resources").Subject;
            var resource = resources.Should().HavePropertyAsObject("en-US/Fruits.resources.dll").Subject;
            resource.Should().HavePropertyValue("locale", "en-US");
        }


        [Fact]
        public void WritesResourceAssembliesForPortable()
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
                                        new RuntimeAssetGroup[] { },
                                        new RuntimeAssetGroup[] { },
                                        new []
                                        {
                                            new ResourceAssembly("en-US/Fruits.resources.dll", "en-US")
                                        },
                                        new Dependency[] { },
                                        true,
                                        "PackagePath",
                                        "PackageHashPath"
                                    ),
                            }));

            var targets = result.Should().HavePropertyAsObject("targets").Subject;
            var target = targets.Should().HavePropertyAsObject("Target").Subject;
            var library = target.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;
            var resources = library.Should().HavePropertyAsObject("resources").Subject;
            var resource = resources.Should().HavePropertyAsObject("en-US/Fruits.resources.dll").Subject;
            resource.Should().HavePropertyValue("locale", "en-US");
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
    }
}
