using System;
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
            IReadOnlyList<RuntimeFallbacks> runtimeGraph = null)
        {
            return new DependencyContext(
                            target ?? string.Empty,
                            runtime ?? string.Empty,
                            isPortable ?? false,
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
                            false)
                            );

            result.Should().HavePropertyValue("runtimeTarget", "Target/runtime");
        }

        [Fact]
        public void WritesMainTargetNameToRuntimeTargetIfPortable()
        {
            var result = Save(Create(
                            "Target",
                            "runtime",
                            true)
                            );
            result.Should().HavePropertyValue("runtimeTarget", "Target");
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
                                        new [] { RuntimeAssembly.Create("Banana.dll")},
                                        new [] { "runtimes\\linux\\native\\native.so" },
                                        new [] { new ResourceAssembly("en-US\\Banana.Resource.dll", "en-US")},
                                        new []
                                        {
                                            new RuntimeTarget("win7-x64",
                                                new [] { RuntimeAssembly.Create("Banana.Win7-x64.dll") },
                                                new [] { "native\\Banana.Win7-x64.so" }
                                            )
                                        },
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
                                        new [] { RuntimeAssembly.Create("Banana.dll")},
                                        new [] { "native.dll" },
                                        new ResourceAssembly[] {},
                                        new []
                                        {
                                            new RuntimeTarget("win7-x64",
                                                new [] { RuntimeAssembly.Create("Banana.Win7-x64.dll") },
                                                new [] { "Banana.Win7-x64.so" }
                                            )
                                        },
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
                                        new [] { RuntimeAssembly.Create("Banana.dll")},
                                        new [] { "runtimes\\osx\\native\\native.dylib" },
                                        new ResourceAssembly[] {},
                                        new RuntimeTarget[] {},
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
                                        new RuntimeAssembly[] { },
                                        new string[] { },
                                        new []
                                        {
                                            new ResourceAssembly("en-US/Fruits.resources.dll", "en-US")
                                        },
                                        new RuntimeTarget[] { },
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
                                        new RuntimeAssembly[] { },
                                        new string[] { },
                                        new []
                                        {
                                            new ResourceAssembly("en-US/Fruits.resources.dll", "en-US")
                                        },
                                        new RuntimeTarget[] { },
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
                defines: new[] {"MY", "DEFINES"},
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
                .Subject.Values<string>().Should().BeEquivalentTo(new [] {"MY", "DEFINES" });
        }
    }
}