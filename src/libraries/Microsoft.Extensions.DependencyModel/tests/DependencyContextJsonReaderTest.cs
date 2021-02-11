// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class DependencyContextJsonReaderTest
    {
        // Same as the default for StreamWriter
        private static readonly Encoding s_utf8NoPreamble =
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private DependencyContext Read(string text, bool withPreamble = false)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetMaxByteCount(text.Length)))
            using (var writer = new StreamWriter(stream, withPreamble ? Encoding.UTF8 : s_utf8NoPreamble))
            {
                writer.Write(text);
                writer.Flush();
                stream.Seek(0, SeekOrigin.Begin);

                return new DependencyContextJsonReader().Read(stream);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReadsRuntimeTargetInfo(bool withPreamble)
        {
            var context = Read(
@"{
    ""runtimeTarget"": {
        ""name"":"".NETCoreApp,Version=v1.0/osx.10.10-x64"",
        ""signature"":""target-signature""
    },
    ""targets"": {
        "".NETCoreApp,Version=v1.0/osx.10.10-x64"": {}
    }
}",
                withPreamble);

            context.Target.IsPortable.Should().BeFalse();
            context.Target.Framework.Should().Be(".NETCoreApp,Version=v1.0");
            context.Target.Runtime.Should().Be("osx.10.10-x64");
            context.Target.RuntimeSignature.Should().Be("target-signature");
        }

        [Fact]
        public void ReadsRuntimeTargetInfoWithCommentsIsInvalid()
        {
            var exception = Assert.ThrowsAny<JsonException>(() => Read(
@"{
    ""runtimeTarget"": {
        ""name"":"".NETCoreApp,Version=v1.0/osx.10.10-x64"",
        ""signature"":""target-signature""
    },
    ""targets"": {
        // Ignore comments
        "".NETCoreApp,Version=v1.0/osx.10.10-x64"": {}
        /*
         * Ignore multi-line comments
        */
    }
}"));

            Assert.Equal("'/' is invalid after a value. Expected either ',', '}', or ']'. LineNumber: 6 | BytePositionInLine: 8.", exception.Message);
        }

        [Fact]
        public void IgnoredUnknownPropertiesInRuntimeTarget()
        {
            var context = Read(
@"{
    ""runtimeTarget"": {
        ""shouldIgnoreString"": ""this-will-never-exist"",
        ""shouldIgnoreObject"": {""inner"": [1, 2]},
        ""name"":"".NETCoreApp,Version=v1.0/osx.10.10-x64"",
        ""signature"":""target-signature""
    },
    ""targets"": {
        "".NETCoreApp,Version=v1.0/osx.10.10-x64"": {}
    }
}");
            context.Target.IsPortable.Should().BeFalse();
            context.Target.Framework.Should().Be(".NETCoreApp,Version=v1.0");
            context.Target.Runtime.Should().Be("osx.10.10-x64");
            context.Target.RuntimeSignature.Should().Be("target-signature");
        }

        [Fact]
        public void GroupsRuntimeAssets()
        {
            string json = @"
 {
     ""targets"": {
         "".NETStandard,Version=v1.5"": {
             ""System.Banana/1.0.0"": {
                 ""runtimeTargets"": {
                     ""runtimes/unix/Banana.dll"": { ""rid"": ""unix"", ""assetType"": ""runtime"" },
                     ""runtimes/win7/Banana.dll"": { ""rid"": ""win7"",  ""assetType"": ""runtime"" },

                     ""runtimes/native/win7/Apple.dll"": { ""rid"": ""win7"",  ""assetType"": ""native"" },
                     ""runtimes/native/unix/libapple.so"": { ""rid"": ""unix"",  ""assetType"": ""native"" }
                 }
             }
         }
     },
     ""libraries"": {
         ""System.Banana/1.0.0"": {
             ""type"": ""package"",
             ""serviceable"": false,
             ""sha512"": ""HASH-System.Banana""
         }
     }
 }";

            ReadGroupsRuntimeAssets(json);
        }

        [Fact]
        public void GroupsRuntimeAssetsWithAssemblyVersions()
        {
            string json = @"
 {
     ""targets"": {
         "".NETStandard,Version=v1.5"": {
             ""System.Banana/1.0.0"": {
                 ""runtimeTargets"": {
                     ""runtimes/unix/Banana.dll"": { ""rid"": ""unix"", ""assetType"": ""runtime"", ""assemblyVersion"": ""1.2.3"", ""fileVersion"": ""4.5.6"" },
                     ""runtimes/win7/Banana.dll"": { ""rid"": ""win7"",  ""assetType"": ""runtime"", ""fileVersion"": ""1.2.3""},

                     ""runtimes/native/win7/Apple.dll"": { ""rid"": ""win7"",  ""assetType"": ""native"" },
                     ""runtimes/native/unix/libapple.so"": { ""rid"": ""unix"",  ""assetType"": ""native"" }
                 }
             }
         }
     },
     ""libraries"": {
         ""System.Banana/1.0.0"": {
             ""type"": ""package"",
             ""serviceable"": false,
             ""sha512"": ""HASH-System.Banana""
         }
     }
 }";

            RuntimeLibrary runtimeLib = ReadGroupsRuntimeAssets(json);
            runtimeLib.RuntimeAssemblyGroups.GetRuntimeFileAssets("unix").Single().AssemblyVersion.Should().Be("1.2.3");
            runtimeLib.RuntimeAssemblyGroups.GetRuntimeFileAssets("unix").Single().FileVersion.Should().Be("4.5.6");
            runtimeLib.RuntimeAssemblyGroups.GetRuntimeFileAssets("win7").Single().AssemblyVersion.Should().BeNull();
            runtimeLib.RuntimeAssemblyGroups.GetRuntimeFileAssets("win7").Single().FileVersion.Should().Be("1.2.3");
        }

        private RuntimeLibrary ReadGroupsRuntimeAssets(string json)
        {
            var context = Read(json);

            context.RuntimeLibraries.Should().HaveCount(1);
            RuntimeLibrary runtimeLib = context.RuntimeLibraries.Single();
            runtimeLib.RuntimeAssemblyGroups.Should().HaveCount(2);
            runtimeLib.RuntimeAssemblyGroups.All(g => g.AssetPaths.Count == 1).Should().BeTrue();

            runtimeLib.NativeLibraryGroups.Should().HaveCount(2);
            runtimeLib.NativeLibraryGroups.All(g => g.AssetPaths.Count == 1).Should().BeTrue();

            return runtimeLib;
        }

        [Fact]
        public void SetsPortableIfRuntimeTargetHasNoRid()
        {
            var context = Read(
@"{
    ""targets"": {
        "".NETCoreApp,Version=v1.0"": {}
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
        ""name"": "".NETCoreApp,Version=v1.0/osx.10.10-x64""
    },
    ""targets"": {
        "".NETCoreApp,Version=v1.0/osx.10.10-x64"": {}
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
        "".NETCoreApp,Version=v1.0"": {}
    }
}");
            context.Target.Framework.Should().Be(".NETCoreApp,Version=v1.0");
        }

        [Fact]
        public void IgnoresExtraTopLevelNodes()
        {
            var context = Read(
@"{
    ""shouldIgnoreObject"": {""inner"": [1, 2]},
    ""targets"": {
        "".NETCoreApp,Version=v1.0"": {}
    },
    ""shouldIgnoreString"": ""this-will-never-exist""
}");
            context.Target.Framework.Should().Be(".NETCoreApp,Version=v1.0");
        }

        [Fact]
        public void ReadsRuntimeGraph()
        {
            var context = Read(
@"{
    ""targets"": {
        "".NETCoreApp,Version=v1.0/osx.10.10-x64"": {}
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
        "".NETCoreApp,Version=v1.0"": {
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
            ""sha512"": ""HASH-System.Banana"",
            ""path"": ""PackagePath"",
            ""hashPath"": ""PachageHashPath""
        }
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
            package.Path.Should().Be("PackagePath");
            package.HashPath.Should().Be("PachageHashPath");
        }

        [Fact]
        public void RejectsMissingLibrary()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => Read(
@"{
    ""targets"": {
        "".NETCoreApp,Version=v1.0"": {
            ""System.Banana/1.0.0"": {}
        }
    },
    ""libraries"": {}
}"));

            Assert.Equal($"Cannot find library information for System.Banana/1.0.0", exception.Message);
        }

        [Fact]
        public void IgnoresUnknownPropertiesInLibrary()
        {
            var context = Read(
@"{
    ""targets"": {
        "".NETCoreApp,Version=v1.0"": {
            ""System.Banana/1.0.0"": {
                ""shouldIgnoreString"": ""this-will-never-exist"",
                ""shouldIgnoreObject"": {""inner"": [1, 2]},
                ""compile"": {
                    ""ref/dotnet5.4/System.Banana.dll"": { }
                }
            }
        }
    },
    ""libraries"": {
        ""System.Banana/1.0.0"": {
            ""shouldIgnoreString"": ""this-will-never-exist"",
            ""shouldIgnoreObject"": {""inner"": [1, 2]},
            ""type"": ""package"",
            ""serviceable"": false,
            ""sha512"": ""HASH-System.Banana""
        }
    }
}");
            context.CompileLibraries.Should().HaveCount(1);

            var package = context.CompileLibraries.Should().Contain(l => l.Name == "System.Banana").Subject;
            package.Version.Should().Be("1.0.0");
            package.Assemblies.Should().BeEquivalentTo("ref/dotnet5.4/System.Banana.dll");
            package.Hash.Should().Be("HASH-System.Banana");
            package.Type.Should().Be("package");
            package.Serviceable.Should().Be(false);
            package.Path.Should().BeNull();
            package.HashPath.Should().BeNull();
        }

        [Fact]
        public void ReadsCompilationTargetWithNullPathAndHashPath()
        {
            var context = Read(
@"{
    ""targets"": {
        "".NETCoreApp,Version=v1.0"": {
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
        ""System.Banana/1.0.0"": {
            ""type"": ""package"",
            ""serviceable"": false,
            ""sha512"": ""HASH-System.Banana"",
            ""path"": null,
            ""hashPath"": null
        }
    }
}");
            context.CompileLibraries.Should().HaveCount(1);

            var package = context.CompileLibraries.Should().Contain(l => l.Name == "System.Banana").Subject;
            package.Version.Should().Be("1.0.0");
            package.Assemblies.Should().BeEquivalentTo("ref/dotnet5.4/System.Banana.dll");
            package.Hash.Should().Be("HASH-System.Banana");
            package.Type.Should().Be("package");
            package.Serviceable.Should().Be(false);
            package.Path.Should().BeNull();
            package.HashPath.Should().BeNull();
        }

        [Fact]
        public void ReadsCompilationTargetWithMissingPathAndHashPath()
        {
            var context = Read(
@"{
    ""targets"": {
        "".NETCoreApp,Version=v1.0"": {
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
        ""System.Banana/1.0.0"": {
            ""type"": ""package"",
            ""serviceable"": false,
            ""sha512"": ""HASH-System.Banana""
        }
    }
}");
            context.CompileLibraries.Should().HaveCount(1);

            var package = context.CompileLibraries.Should().Contain(l => l.Name == "System.Banana").Subject;
            package.Version.Should().Be("1.0.0");
            package.Assemblies.Should().BeEquivalentTo("ref/dotnet5.4/System.Banana.dll");
            package.Hash.Should().Be("HASH-System.Banana");
            package.Type.Should().Be("package");
            package.Serviceable.Should().Be(false);
            package.Path.Should().BeNull();
            package.HashPath.Should().BeNull();
        }

        [Fact]
        public void DoesNotReadRuntimeLibraryFromCompilationOnlyEntries()
        {
            var context = Read(
@"{
    ""targets"": {
        "".NETCoreApp,Version=v1.0"": {
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
                ""compileOnly"": true,
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
        }
    }
}");
            context.CompileLibraries.Should().HaveCount(2);
            context.RuntimeLibraries.Should().HaveCount(1);
            context.RuntimeLibraries[0].Name.Should().Be("MyApp");
        }


        [Fact]
        public void ReadsRuntimeLibrariesWithSubtargetsFromMainTargetForPortable()
        {
            ReadsRuntimeLibrariesWithSubtargetsFromMainTargetForPortableCore(false);
        }

        [Fact]
        public void ReadsRuntimeLibrariesWithSubtargetsFromMainTargetForPortableWithAssemblyVersions()
        {
            ReadsRuntimeLibrariesWithSubtargetsFromMainTargetForPortableCore(true);
        }

        private void ReadsRuntimeLibrariesWithSubtargetsFromMainTargetForPortableCore(bool useAssemblyVersions)
        {
            string json =
@"{
    ""runtimeTarget"": {
        ""name"": "".NETCoreApp,Version=v1.0""
    },
    ""targets"": {
        "".NETCoreApp,Version=v1.0"": {
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
                    ""lib/dotnet5.4/System.Banana.dll"": {";

            if (useAssemblyVersions)
            {
                json +=
@"                            ""assemblyVersion"": ""1.2.3"",
                            ""fileVersion"": ""7.8.9""
                    }";
            }
            else
            {
                json += " }";
            }

            json +=
@"                },
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
            ""type"": ""project""
        },
        ""System.Banana/1.0.0"": {
            ""type"": ""package"",
            ""serviceable"": false,
            ""sha512"": ""HASH-System.Banana"",
            ""path"": ""PackagePath"",
            ""hashPath"": ""PackageHashPath"",
            ""runtimeStoreManifestName"": ""placeHolderManifest.xml""
        }
    }
}";
            var context = Read(json);

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
            package.Path.Should().Be("PackagePath");
            package.HashPath.Should().Be("PackageHashPath");
            package.RuntimeStoreManifestName.Should().Be("placeHolderManifest.xml");
            package.ResourceAssemblies.Should().Contain(a => a.Path == "System.Banana.resources.dll")
                .Subject.Locale.Should().Be("en-US");

            if (useAssemblyVersions)
            {
                var assets = package.RuntimeAssemblyGroups.GetDefaultRuntimeFileAssets();
                assets.Should().HaveCount(1);
                var runtimeFile = assets.First();
                runtimeFile.Path.Should().Be("lib/dotnet5.4/System.Banana.dll");
                runtimeFile.AssemblyVersion.Should().Be("1.2.3");
                runtimeFile.FileVersion.Should().Be("7.8.9");
            }
            else
            {
                package.RuntimeAssemblyGroups.GetDefaultAssets().Should().Contain("lib/dotnet5.4/System.Banana.dll");
            }

            package.RuntimeAssemblyGroups.GetRuntimeAssets("win7-x64").Should().Contain("lib/win7/System.Banana.dll");
            package.NativeLibraryGroups.GetRuntimeAssets("win7-x64").Should().Contain("lib/win7/Banana.dll");
        }

        [Fact]
        public void ReadsRuntimeTargetPlaceholdersAsEmptyGroups()
        {
            var context = Read(
@"{
    ""runtimeTarget"": {
        ""name"": "".NETCoreApp,Version=v1.0""
    },
    ""targets"": {
        "".NETCoreApp,Version=v1.0"": {
            ""System.Banana/1.0.0"": {
                ""runtimeTargets"": {
                    ""runtime/win7-x64/lib/_._"": { ""assetType"": ""runtime"", ""rid"": ""win7-x64""},
                    ""runtime/linux-x64/native/_._"": { ""assetType"": ""native"", ""rid"": ""linux-x64""}
                }
            }
        }
    },
    ""libraries"":{
        ""System.Banana/1.0.0"": {
            ""type"": ""package"",
            ""serviceable"": false,
            ""sha512"": ""HASH-System.Banana""
        }
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
        public void IgnoresUnknownPropertiesInRuntimeTargets()
        {
            var context = Read(
@"{
    ""runtimeTarget"": {
        ""name"": "".NETCoreApp,Version=v1.0""
    },
    ""targets"": {
        "".NETCoreApp,Version=v1.0"": {
            ""System.Banana/1.0.0"": {
                ""runtimeTargets"": {
                    ""runtime/win7-x64/lib/_._"": {
                        ""shouldIgnoreString"": ""this-will-never-exist"",
                        ""shouldIgnoreObject"": {""inner"": [1, 2]},
                        ""assetType"": ""runtime"",
                        ""rid"": ""win7-x64""
                    },
                    ""runtime/linux-x64/native/_._"": { ""assetType"": ""native"", ""rid"": ""linux-x64""}
                }
            }
        }
    },
    ""libraries"":{
        ""System.Banana/1.0.0"": {
            ""type"": ""package"",
            ""serviceable"": false,
            ""sha512"": ""HASH-System.Banana""
        }
    }
}");
            var package = context.RuntimeLibraries.Should().Contain(l => l.Name == "System.Banana").Subject;

            package.RuntimeAssemblyGroups.Should().Contain(g => g.Runtime == "win7-x64")
                .Which.AssetPaths.Should().BeEmpty();
            package.NativeLibraryGroups.Should().Contain(g => g.Runtime == "linux-x64")
                .Which.AssetPaths.Should().BeEmpty();
        }

        [Fact]
        public void ReadsRuntimePackLibrary()
        {
            var context = Read(
@"{
    ""runtimeTarget"": {
        ""name"": "".NETCoreApp,Version=v5.0/win-x86"",
        ""signature"": """"
    },
    ""targets"": {
        "".NETCoreApp,Version=v5.0/win-x86"": {
            ""runtimepack.Microsoft.NETCore.App.Runtime.win-x86/5.0.0-preview.5.20251.1"": {
                ""runtime"": {
                    ""System.Private.CoreLib.dll"": {
                        ""assemblyVersion"": ""5.0.0.0"",
                        ""fileVersion"": ""5.0.20.25101""
                    }
                },
                ""native"": {
                    ""coreclr.dll"": {
                        ""fileVersion"": ""5.0.20.25101""
                    }
                }
            }
        }
    },
    ""libraries"": {
        ""runtimepack.Microsoft.NETCore.App.Runtime.win-x86/5.0.0-preview.5.20251.1"": {
            ""type"": ""runtimepack"",
            ""serviceable"": false,
            ""sha512"": """"
        }
    }
}");

            var runtimeLibrary = context.RuntimeLibraries.Should().ContainSingle().Subject;
            runtimeLibrary.Type.Should().Be("runtimepack");
            runtimeLibrary.Serviceable.Should().Be(false);
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
        "".NETCoreApp,Version=v1.0/osx.10.10-x64"": {}
    }
}");
            context.CompilationOptions.AllowUnsafe.Should().Be(true);
            context.CompilationOptions.Defines.Should().BeEquivalentTo(new[] { "MY", "DEFINES" });
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

        [Fact]
        public void FailsToReadInvalidDefines()
        {
            Assert.Throws<FormatException>(() => Read(
@"{
    ""compilationOptions"": {
        ""defines"": ""MY"",
    },
    ""targets"": {
        "".NETCoreApp,Version=v1.0/osx.10.10-x64"": {}
    }
}")).Message.Should().Contain("line 2 position 23");
        }

        [Fact]
        public void IgnoresUnknownPropertiesInCompilationOptions()
        {
            var context = Read(
@"{
    ""compilationOptions"": {
        ""shouldIgnoreString"": ""this-will-never-exist"",
        ""shouldIgnoreObject"": {""inner"": [1, 2]},
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
        "".NETCoreApp,Version=v1.0/osx.10.10-x64"": {}
    }
}");
            context.CompilationOptions.AllowUnsafe.Should().Be(true);
            context.CompilationOptions.Defines.Should().BeEquivalentTo(new[] { "MY", "DEFINES" });
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
