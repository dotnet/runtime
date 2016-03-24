﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using System.Diagnostics;

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
        ""name"":"".NETStandardApp,Version=v1.5/osx.10.10-x64""
    },
    ""targets"": {
        "".NETStandardApp,Version=v1.5/osx.10.10-x64"": {},
    }
}");
            context.Target.IsPortable.Should().BeFalse();
            context.Target.Framework.Should().Be(".NETStandardApp,Version=v1.5");
            context.Target.Runtime.Should().Be("osx.10.10-x64");
        }

        [Fact]
        public void SetsPortableIfRuntimeTargetHasNoRid()
        {
            var context = Read(
@"{
    ""targets"": {
        "".NETStandardApp,Version=v1.5"": {}
    }
}");
            context.Target.IsPortable.Should().BeTrue();
        }

        [Fact]
        public void SetsNotPortableIfRuntimeTargetHasRid()
        {
            var context = Read(
@"{
    ""runtimeTarget"": {
        ""name"": "".NETStandardApp,Version=v1.5/osx.10.10-x64""
    },
    ""targets"": {
        "".NETStandardApp,Version=v1.5/osx.10.10-x64"": {}
    }
}");
            context.Target.IsPortable.Should().BeFalse();
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
            context.Target.Framework.Should().Be(".NETStandardApp,Version=v1.5");
        }

        [Fact]
        public void ReadsRuntimeGraph()
        {
            var context = Read(
@"{
    ""targets"": {
        "".NETStandardApp,Version=v1.5/osx.10.10-x64"": {},
    },
    ""runtimes"": {
        ""osx.10.10-x64"": [ ],
        ""osx.10.11-x64"": [ ""osx"" ],
        ""rhel.7-x64"": [ ""linux-x64"", ""unix"" ]
    }
}");
            context.RuntimeGraph.Should().Contain(p => p.Runtime == "osx.10.10-x64").Which
                .Fallbacks.Should().BeEquivalentTo();

            context.RuntimeGraph.Should().Contain(p => p.Runtime == "osx.10.11-x64").Which
                .Fallbacks.Should().BeEquivalentTo("osx");

            context.RuntimeGraph.Should().Contain(p => p.Runtime == "rhel.7-x64").Which
                .Fallbacks.Should().BeEquivalentTo("linux-x64", "unix");
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
            var project = context.CompileLibraries.Should().Contain(l => l.Name == "MyApp").Subject;
            project.Version.Should().Be("1.0.1");
            project.Assemblies.Should().BeEquivalentTo("MyApp.dll");
            project.Type.Should().Be("project");

            var package = context.CompileLibraries.Should().Contain(l => l.Name == "System.Banana").Subject;
            package.Version.Should().Be("1.0.0");
            package.Assemblies.Should().BeEquivalentTo("ref/dotnet5.4/System.Banana.dll");
            package.Hash.Should().Be("HASH-System.Banana");
            package.Type.Should().Be("package");
            package.Serviceable.Should().Be(false);
        }


        [Fact]
        public void ReadsRuntimeLibrariesWithSubtargetsFromMainTargetForPortable()
        {
            var context = Read(
@"{
    ""runtimeTarget"": {
        ""name"": "".NETStandardApp,Version=v1.5""
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
                },
                ""resources"": {
                    ""System.Banana.resources.dll"": { ""locale"": ""en-US"" }
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
            var project = context.RuntimeLibraries.Should().Contain(l => l.Name == "MyApp").Subject;
            project.Version.Should().Be("1.0.1");
            project.RuntimeAssemblyGroups.GetDefaultAssets().Should().Contain("MyApp.dll");
            project.Type.Should().Be("project");


            var package = context.RuntimeLibraries.Should().Contain(l => l.Name == "System.Banana").Subject;
            package.Version.Should().Be("1.0.0");
            package.Hash.Should().Be("HASH-System.Banana");
            package.Type.Should().Be("package");
            package.Serviceable.Should().Be(false);
            package.ResourceAssemblies.Should().Contain(a => a.Path == "System.Banana.resources.dll")
                .Subject.Locale.Should().Be("en-US");

            package.RuntimeAssemblyGroups.GetDefaultAssets().Should().Contain("lib/dotnet5.4/System.Banana.dll");
            package.RuntimeAssemblyGroups.GetRuntimeAssets("win7-x64").Should().Contain("lib/win7/System.Banana.dll");
            package.NativeLibraryGroups.GetRuntimeAssets("win7-x64").Should().Contain("lib/win7/Banana.dll");
        }

        [Fact]
        public void ReadsRuntimeTargetPlaceholdersAsEmptyGroups()
        {
            var context = Read(
@"{
    ""runtimeTarget"": "".NETStandardApp,Version=v1.5"",
    ""targets"": {
        "".NETStandardApp,Version=v1.5"": {
            ""System.Banana/1.0.0"": {
                ""runtimeTargets"": {
                    ""runtime/win7-x64/lib/_._"": { ""assetType"": ""runtime"", ""rid"": ""win7-x64""},
                    ""runtime/linux-x64/native/_._"": { ""assetType"": ""native"", ""rid"": ""linux-x64""},
                },
            }
        }
    },
    ""libraries"":{
        ""System.Banana/1.0.0"": {
            ""type"": ""package"",
            ""serviceable"": false,
            ""sha512"": ""HASH-System.Banana""
        },
    }
}");
            context.CompileLibraries.Should().HaveCount(1);

            var package = context.RuntimeLibraries.Should().Contain(l => l.Name == "System.Banana").Subject;

            package.RuntimeAssemblyGroups.Should().Contain(g => g.Runtime == "win7-x64")
                .Which.AssetPaths.Should().BeEmpty();
            package.NativeLibraryGroups.Should().Contain(g => g.Runtime == "linux-x64")
                .Which.AssetPaths.Should().BeEmpty();
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
    },
    ""targets"": {
        "".NETStandardApp,Version=v1.5/osx.10.10-x64"": {},
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
