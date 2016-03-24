﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using FluentAssertions;

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
                                        true
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
        }

        [Fact]
        public void WritesRuntimeLibrariesToRuntimeTarget()
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
                                        true
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
                                        true
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
                                        true
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
        }

        [Fact]
        public void WritesRuntimeTargetForNonPortable()
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
                                            new RuntimeAssetGroup(string.Empty, "Banana.dll")
                                        },
                                        new [] {
                                            new RuntimeAssetGroup(string.Empty, "runtimes\\osx\\native\\native.dylib")
                                        },
                                        new ResourceAssembly[] {},
                                        new [] {
                                            new Dependency("Fruits.Abstract.dll","2.0.0")
                                        },
                                        true
                                    ),
                            }));

            // targets
            var targets = result.Should().HavePropertyAsObject("targets").Subject;
            var target = targets.Should().HavePropertyAsObject("Target/runtime").Subject;
            var library = target.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;
            var dependencies = library.Should().HavePropertyAsObject("dependencies").Subject;
            dependencies.Should().HavePropertyValue("Fruits.Abstract.dll", "2.0.0");
            library.Should().HavePropertyAsObject("runtime")
                .Subject.Should().HaveProperty("Banana.dll");
            library.Should().HavePropertyAsObject("native")
                .Subject.Should().HaveProperty("runtimes/osx/native/native.dylib");

            //libraries
            var libraries = result.Should().HavePropertyAsObject("libraries").Subject;
            library = libraries.Should().HavePropertyAsObject("PackageName/1.2.3").Subject;
            library.Should().HavePropertyValue("sha512", "HASH");
            library.Should().HavePropertyValue("type", "package");
            library.Should().HavePropertyValue("serviceable", true);
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
                                        true
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
                                        true
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
                                        true
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