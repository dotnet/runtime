using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class DependencyContextJsonReaderTest
    {
        private DependencyContext Read(string text)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                return new DependencyContextJsonReader().Read(stream);
            }
        }

        [Fact]
        public void ReadsRuntimeTargetInfo()
        {
            var context = Read(
@"{
    ""runtimeTarget"": {
        ""portable"": false,
        ""name"": "".NETStandardApp,Version=v1.5/osx.10.10-x64"",
    },
    ""targets"": {
        "".NETStandardApp,Version=v1.5"": {},
        "".NETStandardApp,Version=v1.5/osx.10.10-x64"": {},
    }
}");
            context.IsPortable.Should().BeFalse();
            context.Target.Should().Be(".NETStandardApp,Version=v1.5");
            context.Runtime.Should().Be("osx.10.10-x64");
        }

        [Fact]
        public void DefaultsToPortable()
        {
            var context = Read(
@"{
}");
            context.IsPortable.Should().BeTrue();
        }

        [Fact]
        public void ReadsMainTarget()
        {
            var context = Read(
@"{
    ""targets"": {
        "".NETStandardApp,Version=v1.5"": {}
    }
}");
            context.Target.Should().Be(".NETStandardApp,Version=v1.5");
        }

        [Fact]
        public void ReadsRuntimeGraph()
        {
            var context = Read(
@"{
    ""runtimes"": {
        "".NETStandardApp,Version=v1.5"": {
            ""osx.10.10-x64"": [ ],
            ""osx.10.11-x64"": [ ""osx"" ],
            ""rhel.7-x64"": [ ""linux-x64"", ""unix"" ]
        }
    }
}");
            context.RuntimeGraph.Should().Contain(p => p.Key == "osx.10.10-x64").Which
                .Value.Should().BeEquivalentTo();

            context.RuntimeGraph.Should().Contain(p => p.Key == "osx.10.11-x64").Which
                .Value.Should().BeEquivalentTo("osx");

            context.RuntimeGraph.Should().Contain(p => p.Key == "rhel.7-x64").Which
                .Value.Should().BeEquivalentTo("linux-x64", "unix");
        }

        [Fact]
        public void ReadsCompilationTarget()
        {
            var context = Read(
@"{
    ""targets"": {
        "".NETStandardApp,Version=v1.5"": {
            ""MyApp/1.0.1"": {
                ""dependencies"": {
                    ""AspNet.Mvc"": ""1.0.0""
                },
                ""compile"": {
                    ""MyApp.dll"": { }
                }
            },
            ""System.Banana/1.0.0"": {
                ""dependencies"": {
                    ""System.Foo"": ""1.0.0""
                },
                ""compile"": {
                    ""ref/dotnet5.4/System.Banana.dll"": { }
                }
            }
        }
    },
    ""libraries"":{
        ""MyApp/1.0.1"": {
            ""type"": ""project""
        },
        ""System.Banana/1.0.0"": {
            ""type"": ""package"",
            ""serviceable"": false,
            ""sha512"": ""HASH-System.Banana""
        },
    }
}");
            context.CompileLibraries.Should().HaveCount(2);
            var project = context.CompileLibraries.Should().Contain(l => l.PackageName == "MyApp").Subject;
            project.Version.Should().Be("1.0.1");
            project.Assemblies.Should().BeEquivalentTo("MyApp.dll");
            project.LibraryType.Should().Be("project");

            var package = context.CompileLibraries.Should().Contain(l => l.PackageName == "System.Banana").Subject;
            package.Version.Should().Be("1.0.0");
            package.Assemblies.Should().BeEquivalentTo("ref/dotnet5.4/System.Banana.dll");
            package.Hash.Should().Be("HASH-System.Banana");
            package.LibraryType.Should().Be("package");
            package.Serviceable.Should().Be(false);
        }


        [Fact]
        public void ReadsRuntimeLibrariesWithSubtargetsFromMainTargetForPortable()
        {
            var context = Read(
@"{
    ""runtimeTarget"": {
        ""portable"": true,
        ""name"": "".NETStandardApp,Version=v1.5"",
    },
    ""targets"": {
        "".NETStandardApp,Version=v1.5"": {
            ""MyApp/1.0.1"": {
                ""dependencies"": {
                    ""AspNet.Mvc"": ""1.0.0""
                },
                ""runtime"": {
                    ""MyApp.dll"": { }
                }
            },
            ""System.Banana/1.0.0"": {
                ""dependencies"": {
                    ""System.Foo"": ""1.0.0""
                },
                ""runtime"": {
                    ""lib/dotnet5.4/System.Banana.dll"": { }
                },
                ""runtimeTargets"": {
                    ""lib/win7/System.Banana.dll"": { ""assetType"": ""runtime"", ""rid"": ""win7-x64""},
                    ""lib/win7/Banana.dll"": { ""assetType"": ""native"", ""rid"": ""win7-x64""}
                }
            }
        }
    },
    ""libraries"":{
        ""MyApp/1.0.1"": {
            ""type"": ""project"",
        },
        ""System.Banana/1.0.0"": {
            ""type"": ""package"",
            ""serviceable"": false,
            ""sha512"": ""HASH-System.Banana""
        },
    }
}");
            context.CompileLibraries.Should().HaveCount(2);
            var project = context.RuntimeLibraries.Should().Contain(l => l.PackageName == "MyApp").Subject;
            project.Version.Should().Be("1.0.1");
            project.Assemblies.Should().Contain(a => a.Path == "MyApp.dll");
            project.LibraryType.Should().Be("project");


            var package = context.RuntimeLibraries.Should().Contain(l => l.PackageName == "System.Banana").Subject;
            package.Version.Should().Be("1.0.0");
            package.Hash.Should().Be("HASH-System.Banana");
            package.LibraryType.Should().Be("package");
            package.Serviceable.Should().Be(false);
            package.Assemblies.Should().Contain(a => a.Path == "lib/dotnet5.4/System.Banana.dll");

            var target = package.SubTargets.Should().Contain(t => t.Runtime == "win7-x64").Subject;
            target.Assemblies.Should().Contain(a => a.Path == "lib/win7/System.Banana.dll");
            target.NativeLibraries.Should().Contain("lib/win7/Banana.dll");
        }

        [Fact]
        public void ReadsCompilationOptions()
        {
            var context = Read(
@"{
    ""compilationOptions"": {
        ""allowUnsafe"": true,
        ""defines"": [""MY"", ""DEFINES""],
        ""delaySign"": true,
        ""emitEntryPoint"": true,
        ""xmlDoc"": true,
        ""keyFile"": ""Key.snk"",
        ""languageVersion"": ""C#8"",
        ""platform"": ""Platform"",
        ""publicSign"": true,
        ""warningsAsErrors"": true,
        ""optimize"": true
    }
}");
            context.CompilationOptions.AllowUnsafe.Should().Be(true);
            context.CompilationOptions.Defines.Should().BeEquivalentTo(new [] {"MY", "DEFINES"});
            context.CompilationOptions.DelaySign.Should().Be(true);
            context.CompilationOptions.EmitEntryPoint.Should().Be(true);
            context.CompilationOptions.GenerateXmlDocumentation.Should().Be(true);
            context.CompilationOptions.KeyFile.Should().Be("Key.snk");
            context.CompilationOptions.LanguageVersion.Should().Be("C#8");
            context.CompilationOptions.Optimize.Should().Be(true);
            context.CompilationOptions.Platform.Should().Be("Platform");
            context.CompilationOptions.PublicSign.Should().Be(true);
            context.CompilationOptions.WarningsAsErrors.Should().Be(true);
        }
    }
}
