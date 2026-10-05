// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.ExceptionServices;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.DotNet.RemoteExecutor;
using Mono.Linker;
using Xunit;

namespace ILLink.Tasks.Tests
{
    // These tests ensure that the task options correctly flow from
    // the task -> response file -> parsed arguments -> options on LinkContext
    public class TaskArgumentTests
    {
        public static IEnumerable<object[]> AssemblyPathsCases => new List<object[]> {
            new object [] {
                new ITaskItem [] {
                    new TaskItem("Assembly.dll", new Dictionary<string, string> { { "trimmode", "copy" } })
                }
            },
            new object [] {
                new ITaskItem [] {
                    new TaskItem("Assembly.dll", new Dictionary<string, string> { { "TrimMode", "Copy" } })
                }
            },
            new object [] {
                new ITaskItem [] {
                    new TaskItem("path with/spaces/Assembly.dll")
                }
            },
            new object [] {
                new ITaskItem [] {
                    // same path
                    new TaskItem("path/to/Assembly1.dll"),
                    new TaskItem("path/to/Assembly2.dll")
                }
            },
            new object [] {
                new ITaskItem [] {
                    // same assembly
                    new TaskItem("path/to/Assembly.dll"),
                    new TaskItem("path/to/Assembly.dll")
                }
            },
            new object [] {
                new ITaskItem [] {
                    // same assembly name, different paths
                    new TaskItem("path1/Assembly.dll"),
                    new TaskItem("path2/Assembly.dll")
                }
            }
        };

        [Theory]
        [InlineData("full", AssemblyAction.Link)]
        [InlineData("partial", AssemblyAction.Copy)]
        public void TrimModeFullAndPartial(string trimMode, AssemblyAction expectedDefaultAction)
        {
            var task = new MockTask()
            {
                TrimMode = trimMode
            };
            using (var driver = task.CreateDriver())
            {
                Assert.Equal(AssemblyAction.Link, driver.Context.TrimAction);
                Assert.Equal(expectedDefaultAction, driver.Context.DefaultAction);
            }
        }

        [Theory]
        [InlineData("full")]
        [InlineData("partial")]
        public void TrimModeAssemblyPaths(string trimMode)
        {
            var assemblyPaths = new ITaskItem[] {
                new TaskItem("Assembly1.dll", new Dictionary<string, string> {{ "IsTrimmable", "true" }}),
                new TaskItem("Assembly2.dll", new Dictionary<string, string>()),
                new TaskItem("Assembly3.dll", new Dictionary<string, string> {{ "IsTrimmable", "false" }}),
            };
            var task = new MockTask()
            {
                TrimMode = trimMode,
                AssemblyPaths = assemblyPaths
            };
            using var driver = task.CreateDriver();
            var context = driver.Context;
            var references = driver.GetReferenceAssemblies();
            Assert.Equal("", assemblyPaths[0].GetMetadata("TrimMode"));
            Assert.Equal(AssemblyAction.Link, context.Actions["Assembly1"]);
            Assert.Equal("", assemblyPaths[1].GetMetadata("TrimMode"));
            Assert.False(context.Actions.ContainsKey("Assembly2"));
            Assert.Equal("", assemblyPaths[2].GetMetadata("TrimMode"));
            Assert.Equal(AssemblyAction.Copy, context.Actions["Assembly3"]);
        }

        [Theory]
        [MemberData(nameof(AssemblyPathsCases))]
        public void TestAssemblyPaths(ITaskItem[] assemblyPaths)
        {
            var task = new MockTask()
            {
                AssemblyPaths = assemblyPaths
            };
            using (var driver = task.CreateDriver())
            {
                var context = driver.Context;

                var expectedReferences = assemblyPaths.Select(p => p.ItemSpec)
                    .GroupBy(p => Path.GetFileNameWithoutExtension(p))
                    .Select(g => g.First());
                var actualReferences = driver.GetReferenceAssemblies();
                Assert.Equal(expectedReferences.OrderBy(a => a), actualReferences.OrderBy(a => a));

                foreach (var item in assemblyPaths)
                {
                    var assemblyPath = item.ItemSpec;
                    var trimMode = item.GetMetadata("TrimMode");
                    if (String.IsNullOrEmpty(trimMode))
                        continue;

                    AssemblyAction expectedAction = (AssemblyAction)Enum.Parse(typeof(AssemblyAction), trimMode, ignoreCase: true);
                    AssemblyAction actualAction = context.Actions[Path.GetFileNameWithoutExtension(assemblyPath)];

                    Assert.Equal(expectedAction, actualAction);
                }
            }
        }

        [Fact]
        public void TestAssemblyPathsWithInvalidAction()
        {
            var task = new MockTask()
            {
                AssemblyPaths = new ITaskItem[] { new TaskItem("Assembly.dll", new Dictionary<string, string> { { "TrimMode", "invalid" } }) }
            };

            using (var driver = task.CreateDriver())
            {
                Assert.Equal(1031, driver.Logger.Messages[0].Code);
            }
        }

        // the InlineData string [] parameters are wrapped in object [] as described in https://github.com/xunit/xunit/issues/2060

        [Theory]
        [InlineData(new object[] { new string[] { "path/to/Assembly.dll" } })]
        [InlineData(new object[] { new string[] { "path with/spaces/Assembly.dll" } })]
        [InlineData(new object[] { new string[] { "path/to/Assembly With Spaces.dll" } })]
        public void TestReferenceAssemblyPaths(string[] referenceAssemblyPaths)
        {
            var task = new MockTask()
            {
                ReferenceAssemblyPaths = referenceAssemblyPaths.Select(p => new TaskItem(p)).ToArray()
            };
            using (var driver = task.CreateDriver())
            {
                var expectedReferences = referenceAssemblyPaths;
                var actualReferences = driver.GetReferenceAssemblies();
                Assert.Equal(expectedReferences.OrderBy(a => a), actualReferences.OrderBy(a => a));
                foreach (var reference in expectedReferences)
                {
                    var referenceName = Path.GetFileNameWithoutExtension(reference);
                    var actualAction = driver.Context.Actions[referenceName];
                    Assert.Equal(AssemblyAction.Skip, actualAction);
                }
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "illink.dll" } })]
        [InlineData(new object[] { new string[] { "illink" } })]
        public void TestRootEntryPointAssemblyNames(string[] rootAssemblyNames)
        {
            var task = new MockTask()
            {
                RootAssemblyNames = rootAssemblyNames.Select(a => new TaskItem(a)).ToArray()
            };

            using (var driver = task.CreateDriver())
            {
                var expectedRoots = rootAssemblyNames;
                var actualRoots = driver.GetRootAssemblies();
                Assert.Equal(rootAssemblyNames.OrderBy(r => r), actualRoots.OrderBy(r => r));
            }
        }

        [Theory]
        [InlineData("path/to/directory")]
        [InlineData("path with/spaces")]
        public void TestOutputDirectory(string outputDirectory)
        {
            var task = new MockTask()
            {
                OutputDirectory = new TaskItem(outputDirectory)
            };
            using (var driver = task.CreateDriver())
            {
                var actualOutputDirectory = driver.Context.OutputDirectory;
                Assert.Equal(outputDirectory, actualOutputDirectory);
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "combined_output.xml" } })]
        public void TestRootDescriptorFiles(string[] rootDescriptorFiles)
        {
            var task = new MockTask()
            {
                RootDescriptorFiles = rootDescriptorFiles.Select(f => new TaskItem(f)).ToArray()
            };
            using (var driver = task.CreateDriver())
            {
                var actualDescriptors = driver.GetRootDescriptors();
                Assert.Equal(rootDescriptorFiles.OrderBy(f => f), actualDescriptors.OrderBy(f => f));
            }
        }

        public static IEnumerable<object[]> OptimizationsCases()
        {
            foreach (var optimization in MockTask.OptimizationNames)
            {
                yield return new object[] { optimization, true };
                yield return new object[] { optimization, false };
            }
        }

        [Theory]
        [MemberData(nameof(OptimizationsCases))]
        public void TestGlobalOptimizations(string optimization, bool enabled)
        {
            var task = new MockTask();
            task.SetOptimization(optimization, enabled);
            // get the corresponding CodeOptimizations value
            using (var driver = task.CreateDriver())
            {
                Assert.True(driver.GetOptimizationName(optimization, out CodeOptimizations codeOptimizations));
                var actualValue = driver.Context.Optimizations.IsEnabled(codeOptimizations, assemblyName: null);
                Assert.Equal(enabled, actualValue);
            }
        }

        public static IEnumerable<object[]> PerAssemblyOptimizationsCases()
        {
            // test that we can individually enable/disable each optimization
            foreach (var optimization in MockTask.OptimizationNames)
            {
                yield return new object[] {
                    new ITaskItem [] {
                        new TaskItem("path/to/Assembly.dll", new Dictionary<string, string> {
                            { optimization, "True" }
                        })
                    }
                };
                yield return new object[] {
                    new ITaskItem [] {
                        new TaskItem("path/to/Assembly.dll", new Dictionary<string, string> {
                            { optimization, "False" }
                        })
                    }
                };
            }
            // complex case with multiple optimizations, assemblies
            yield return new object[] {
                new ITaskItem [] {
                    new TaskItem("path/to/Assembly1.dll", new Dictionary<string, string> {
                        { "Sealer", "True" },
                        { "BeforeFieldInit", "False" }
                    }),
                    new TaskItem("path/to/Assembly2.dll", new Dictionary<string, string> {
                        { "Sealer", "False" },
                        { "BeforeFieldInit", "True" }
                    })
                }
            };
        }

        [Theory]
        [MemberData(nameof(PerAssemblyOptimizationsCases))]
        public void TestPerAssemblyOptimizations(ITaskItem[] assemblyPaths)
        {
            var task = new MockTask()
            {
                AssemblyPaths = assemblyPaths
            };
            using (var driver = task.CreateDriver())
            {
                foreach (var item in assemblyPaths)
                {
                    var assemblyName = Path.GetFileNameWithoutExtension(item.ItemSpec);
                    foreach (var optimization in MockTask.OptimizationNames)
                    {
                        Assert.True(driver.GetOptimizationName(optimization, out CodeOptimizations codeOptimizations));
                        var optimizationValue = item.GetMetadata(optimization);
                        if (String.IsNullOrEmpty(optimizationValue))
                            continue;
                        var enabled = Boolean.Parse(optimizationValue);
                        var actualValue = driver.Context.Optimizations.IsEnabled(codeOptimizations, assemblyName: assemblyName);
                        Assert.Equal(enabled, actualValue);
                    }
                }
            }
        }

        public static IEnumerable<object[]> SingleWarnCases => new List<object[]> {
            new object[] {
                true,
                new ITaskItem [] {
                    new TaskItem("AssemblyTrue.dll", new Dictionary<string, string> { { "TrimmerSingleWarn", "true" } } ),
                    new TaskItem("AssemblyFalse.dll", new Dictionary<string, string> { { "TrimmerSingleWarn", "false" } } )
                },
            },
            new object [] {
                false,
                new ITaskItem [] {
                    new TaskItem("AssemblyTrue.dll", new Dictionary<string, string> { { "TrimmerSingleWarn", "true" } } ),
                    new TaskItem("AssemblyFalse.dll", new Dictionary<string, string> { { "TrimmerSingleWarn", "false" } } )
                }
            }
        };

        [Theory]
        [MemberData(nameof(SingleWarnCases))]
        public void TestSingleWarn(bool singleWarn, ITaskItem[] assemblyPaths)
        {
            var task = new MockTask()
            {
                AssemblyPaths = assemblyPaths,
                SingleWarn = singleWarn
            };
            using (var driver = task.CreateDriver())
            {
                Assert.Equal(singleWarn, driver.Context.GeneralSingleWarn);
                var expectedSingleWarn = assemblyPaths.ToDictionary(
                    p => Path.GetFileNameWithoutExtension(p.ItemSpec),
                    p => bool.Parse(p.GetMetadata("TrimmerSingleWarn"))
                );
                Assert.Equal(expectedSingleWarn, driver.Context.SingleWarn);
            }
        }

        [Fact]
        public void TestInvalidPerAssemblyOptimizations()
        {
            var task = new MockTask()
            {
                AssemblyPaths = new ITaskItem[] {
                    new TaskItem("path/to/Assembly.dll", new Dictionary<string, string> {
                        { "Sealer", "invalid" }
                    })
                }
            };
            Assert.Throws<ArgumentException>(() => task.CreateDriver());
        }

        [Fact]
        public void TestOptimizationsDefaults()
        {
            var task = new MockTask();
            using (var driver = task.CreateDriver())
            {
                var expectedOptimizations = driver.GetDefaultOptimizations();
                var actualOptimizations = driver.Context.Optimizations.Global;
                Assert.Equal(expectedOptimizations, actualOptimizations);
            }
        }

        [Fact]
        public void CheckGlobalOptimizationsMatchPerAssemblyOptimizations()
        {
            var task = new MockTask();
            var optimizationMetadataNames = MockTask.OptimizationNames;
            var optimizationPropertyNames = MockTask.GetOptimizationPropertyNames();
            Assert.Equal(optimizationMetadataNames.OrderBy(o => o), optimizationPropertyNames.OrderBy(o => o));
        }

        [Theory]
        [InlineData("IL2001;IL2002;IL2003;IL2004", 4)]
        [InlineData("IL2001 IL2002 IL2003 IL2004", 4)]
        [InlineData("IL2001,IL2002,IL2003,IL2004", 4)]
        [InlineData("IL2001,IL2002;IL2003 IL2004", 4)]
        [InlineData("IL2001,IL2002,IL8000,IL1003", 4)]
        [InlineData("IL20000,IL02000", 2)]
        [InlineData("   IL2001\n  IL2002;\n \tIL2003", 3)]
        public void TestValidNoWarn(string noWarn, int validNoWarns)
        {
            var task = new MockTask()
            {
                NoWarn = noWarn
            };
            using (var driver = task.CreateDriver())
            {
                var actualUsedNoWarns = driver.Context.NoWarn;
                Assert.Equal(validNoWarns, actualUsedNoWarns.Count);
            }
        }

        [Theory]
        [InlineData("0", WarnVersion.ILLink0)]
        [InlineData("5", WarnVersion.ILLink5)]
        [InlineData("6", (WarnVersion)6)]
        [InlineData("9999", WarnVersion.Latest)]
        public void TestWarn(string warnArg, WarnVersion expectedVersion)
        {
            var task = new MockTask()
            {
                Warn = warnArg
            };
            using (var driver = task.CreateDriver())
            {
                Assert.Equal(expectedVersion, driver.Context.WarnVersion);
            }
        }

#nullable enable
        [Theory]
        [InlineData(true, null, null, new int[] { }, new int[] { })]
        [InlineData(false, "IL1001,IL2000,IL2054,IL2022", null,
            new int[] { 1001, 2000, 2054, 2022 }, new int[] { })]
        [InlineData(false, "IL2023,IL6000;IL5042 IL2040", "IL4000,IL4001;IL4002 IL4003",
            new int[] { 2023, 2040, 5042, 6000 }, new int[] { 4000, 4001, 4002, 4003 })]
        [InlineData(false, "IL3000;IL3000;ABCD", "IL2005 IL3005 IL2005",
            new int[] { 3000 }, new int[] { 2005, 3005 })]
        [InlineData(true, null, "IL2067", new int[] { }, new int[] { 2067 })]
        [InlineData(true, "IL2001", "IL2001", new int[] { }, new int[] { 2001 })]
        [InlineData(false, "IL1001;\n\t IL1002\n\t IL1003", null,
            new int[] { 1001, 1002, 1003 }, new int[] { })]
        public void TestWarningsAsErrors(bool treatWarningsAsErrors, string? warningsAsErrors, string? warningsNotAsErrors, int[] warnAsError, int[] warnNotAsError)
        {
            var task = new MockTask()
            {
                TreatWarningsAsErrors = treatWarningsAsErrors,
                WarningsAsErrors = warningsAsErrors,
                WarningsNotAsErrors = warningsNotAsErrors
            };

            using (var driver = task.CreateDriver())
            {
                var actualWarnAsError = driver.Context.WarnAsError;
                var actualGeneralWarnAsError = driver.Context.GeneralWarnAsError;
                Assert.Equal(warnAsError.Length + warnNotAsError.Length, actualWarnAsError.Count);
                Assert.Equal(treatWarningsAsErrors, actualGeneralWarnAsError);
                if (warnAsError.Length > 0)
                {
                    foreach (var warningCode in warnAsError)
                        Assert.True(actualWarnAsError.ContainsKey(warningCode) && actualWarnAsError[warningCode] == true);
                }

                if (warnNotAsError.Length > 0)
                {
                    foreach (var warningCode in warnNotAsError)
                        Assert.True(actualWarnAsError.ContainsKey(warningCode) && actualWarnAsError[warningCode] == false);
                }
            }
        }
#nullable restore

        public static IEnumerable<object[]> CustomDataCases => new List<object[]> {
            new object [] {
                new ITaskItem [] {
                    new TaskItem("DataName", new Dictionary<string, string> { { "Value", "DataValue" } })
                },
            },
            new object [] {
                new ITaskItem [] {
                    new TaskItem("DataName", new Dictionary<string, string> { { "Value", "DataValue" } }),
                    new TaskItem("DataName", new Dictionary<string, string> { { "Value", "DataValue2" } })
                },
            },
            new object [] {
                new ITaskItem [] {
                    new TaskItem("DataName1", new Dictionary<string, string> { { "Value", "DataValue1" } }),
                    new TaskItem("DataName2", new Dictionary<string, string> { { "Value", "DataValue2" } })
                },
            },
            new object [] {
                new ITaskItem [] {
                    new TaskItem("DataName", new Dictionary<string, string> { { "Value", "data value with spaces" } })
                },
            },
        };

        [Theory]
        [MemberData(nameof(CustomDataCases))]
        public void TestCustomData(ITaskItem[] customData)
        {
            var task = new MockTask()
            {
                CustomData = customData
            };
            using (var driver = task.CreateDriver())
            {
                var expectedCustomData = customData.Select(c => new { Key = c.ItemSpec, Value = c.GetMetadata("Value") })
                    .GroupBy(c => c.Key)
                    .Select(c => c.Last())
                    .ToDictionary(c => c.Key, c => c.Value);
                var actualCustomData = driver.GetCustomData();
                Assert.Equal(expectedCustomData, actualCustomData);
            }
        }

        public static IEnumerable<object[]> FeatureSettingsCases => new List<object[]> {
            new object [] {
                new ITaskItem [] {
                    new TaskItem("FeatureName", new Dictionary<string, string> { { "Value", "true" } })
                },
            },
            new object [] {
                new ITaskItem [] {
                    new TaskItem("FeatureName", new Dictionary<string, string> { { "Value", "true" } }),
                    new TaskItem("FeatureName", new Dictionary<string, string> { { "Value", "false" } })
                },
            },
            new object [] {
                new ITaskItem [] {
                    new TaskItem("FeatureName1", new Dictionary<string, string> { { "value", "true" } }),
                    new TaskItem("FeatureName2", new Dictionary<string, string> { { "value", "false" } }),
                },
            },
        };

        [Theory]
        [MemberData(nameof(FeatureSettingsCases))]
        public void TestFeatureSettings(ITaskItem[] featureSettings)
        {
            var task = new MockTask()
            {
                FeatureSettings = featureSettings
            };
            using (var driver = task.CreateDriver())
            {
                var expectedSettings = featureSettings.Select(f => new { Feature = f.ItemSpec, Value = f.GetMetadata("Value") })
                    .GroupBy(f => f.Feature)
                    .Select(f => f.Last())
                    .ToDictionary(f => f.Feature, f => bool.Parse(f.Value));
                var actualSettings = driver.Context.FeatureSettings;
                Assert.Equal(expectedSettings, actualSettings);
            }
        }

        [Fact]
        public void TestInvalidFeatureSettings()
        {
            var task = new MockTask()
            {
                FeatureSettings = new ITaskItem[] { new TaskItem("FeatureName") }
            };
            Assert.Throws<ArgumentException>(() => task.CreateDriver());
        }

        [Fact]
        public void TestExtraArgs()
        {
            var task = new MockTask()
            {
                TrimMode = "copy",
                ExtraArgs = "--trim-mode copyused"
            };
            using (var driver = task.CreateDriver())
            {
                Assert.Equal(AssemblyAction.Link, driver.Context.DefaultAction);
                // Check that ExtraArgs can override TrimMode
                Assert.Equal(AssemblyAction.CopyUsed, driver.Context.TrimAction);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestDumpDependencies(bool dumpDependencies)
        {
            var task = new MockTask()
            {
                DumpDependencies = dumpDependencies
            };
            using (var driver = task.CreateDriver())
            {
                Assert.Equal(dumpDependencies, driver.GetDependencyRecorders()?.Single() == MockXmlDependencyRecorder.Singleton);
            }
        }

        [Theory]
        [InlineData("Xml")]
        [InlineData("xml")]
        [InlineData("dgml")]
        [InlineData("Txt")]
        public void TestDependenciesFileFormat(string fileFormat)
        {
            var task = new MockTask()
            {
                DumpDependencies = true,
                DependenciesFileFormat = fileFormat
            };
            // translate string to enum
            // check if enum matches output file format of recorder
            using (var driver = task.CreateDriver())
            {
                switch (fileFormat.ToLower())
                {
                    case "xml":
                        Assert.Equal(MockXmlDependencyRecorder.Singleton, driver.GetDependencyRecorders()?.Single());
                        break;
                    case "dgml":
                        Assert.Equal(MockDgmlDependencyRecorder.Singleton, driver.GetDependencyRecorders()?.Single());
                        break;
                    default:
                        Assert.Equal(1047, driver.Logger.Messages[0].Code);
                        break;
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestRemoveSymbols(bool removeSymbols)
        {
            var task = new MockTask()
            {
                RemoveSymbols = removeSymbols
            };
            using (var driver = task.CreateDriver())
            {
                Assert.NotEqual(removeSymbols, driver.Context.LinkSymbols);
            }
        }

        [Fact]
        public void TestRemoveSymbolsDefault()
        {
            var task = new MockTask();
            using (var driver = task.CreateDriver())
            {
                Assert.False(driver.Context.LinkSymbols);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestPreserveSymbolPaths(bool preserveSymbolPaths)
        {
            var task = new MockTask()
            {
                PreserveSymbolPaths = preserveSymbolPaths
            };
            using (var driver = task.CreateDriver())
            {
                Assert.Equal(preserveSymbolPaths, driver.Context.PreserveSymbolPaths);
            }
        }

        [Fact]
        public void TestPreserveSymbolPathsDefault()
        {
            var task = new MockTask();
            using (var driver = task.CreateDriver())
            {
                Assert.False(driver.Context.PreserveSymbolPaths);
            }
        }

        [Fact]
        public void TestKeepCustomMetadata()
        {
            var task = new MockTask()
            {
                KeepMetadata = new ITaskItem[] { new TaskItem("parametername") }
            };

            using (var driver = task.CreateDriver())
            {
                Assert.Equal(MetadataTrimming.None, driver.Context.MetadataTrimming);
            }
        }

        [Theory]
        [InlineData("copy")]
        [InlineData("link")]
        [InlineData("copyused")]
        public void TestGlobalTrimMode(string trimMode)
        {
            var task = new MockTask()
            {
                TrimMode = trimMode
            };
            using (var driver = task.CreateDriver())
            {
                var expectedAction = (AssemblyAction)Enum.Parse(typeof(AssemblyAction), trimMode, ignoreCase: true);
                Assert.Equal(expectedAction, driver.Context.TrimAction);
                Assert.Equal(AssemblyAction.Link, driver.Context.DefaultAction);
            }
        }

        [Theory]
        [InlineData("copy")]
        [InlineData("link")]
        [InlineData("copyused")]
        public void TestDefaultAction(string defaultAction)
        {
            var task = new MockTask()
            {
                DefaultAction = defaultAction
            };
            using (var driver = task.CreateDriver())
            {
                var expectedAction = (AssemblyAction)Enum.Parse(typeof(AssemblyAction), defaultAction, ignoreCase: true);
                Assert.Equal(expectedAction, driver.Context.DefaultAction);
                Assert.Equal(AssemblyAction.Link, driver.Context.TrimAction);
            }
        }

        [Fact]
        public void TestInvalidDefaultAction()
        {
            var task = new MockTask()
            {
                TrimMode = "invalid"
            };

            using (var driver = task.CreateDriver())
            {
                Assert.Equal(1031, driver.Logger.Messages[0].Code);
            }
        }

        public static IEnumerable<object[]> CustomStepsCases => new List<object[]> {
            new object [] {
                new ITaskItem [] {
                    new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                        { "Type", "ILLink.Tasks.Tests.MockCustomStep" }
                    })
                },
            },
            new object [] {
                new ITaskItem [] {
                    new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                        { "Type", "ILLink.Tasks.Tests.MockCustomStep" },
                        { "BeforeStep", "MarkStep" }
                    })
                },
            },
            new object [] {
                new ITaskItem [] {
                    new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                        { "type", "ILLink.Tasks.Tests.MockCustomStep" },
                        { "beforebtep", "MarkStep" }
                    })
                },
            },
            new object [] {
                new ITaskItem [] {
                    new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                        { "Type", "ILLink.Tasks.Tests.MockCustomStep" },
                        { "AfterStep", "MarkStep" }
                    })
                },
            },
            new object [] {
                new ITaskItem [] {
                    new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                        { "Type", "ILLink.Tasks.Tests.MockCustomStep" },
                        { "BeforeStep", "MarkStep" }
                    }),
                    new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                        { "Type", "ILLink.Tasks.Tests.MockCustomStep" },
                        { "AfterStep", "MarkStep" }
                    })
                },
            }
        };

        [Theory]
        [MemberData(nameof(CustomStepsCases))]
        public void TestCustomSteps(ITaskItem[] customSteps)
        {
            var task = new MockTask()
            {
                CustomSteps = customSteps
            };
            using (var driver = task.CreateDriver())
            {
                foreach (var customStep in customSteps)
                {
                    var stepType = customStep.GetMetadata("Type");
                    var stepName = stepType.Substring(stepType.LastIndexOf(Type.Delimiter) + 1);
                    var beforeStepName = customStep.GetMetadata("BeforeStep");
                    var afterStepName = customStep.GetMetadata("AfterStep");
                    Assert.True(String.IsNullOrEmpty(beforeStepName) || String.IsNullOrEmpty(afterStepName));

                    var actualStepNames = driver.Context.Pipeline.GetSteps().Select(s => s.GetType().Name);
                    if (!String.IsNullOrEmpty(beforeStepName))
                    {
                        Assert.Contains(beforeStepName, actualStepNames);
                        Assert.Equal(stepName, actualStepNames.TakeWhile(s => s != beforeStepName).Last());
                    }
                    else if (!String.IsNullOrEmpty(afterStepName))
                    {
                        Assert.Contains(afterStepName, actualStepNames);
                        Assert.Equal(stepName, actualStepNames.SkipWhile(s => s != afterStepName).ElementAt(1));
                    }
                    else
                    {
                        Assert.Equal(stepName, actualStepNames.Last());
                    }
                }
            }
        }

        [Fact]
        public void TestCustomStepsWithBeforeAndAfterSteps()
        {
            var customSteps = new ITaskItem[] {
                new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                    { "Type", "ILLink.Tasks.Tests.MockCustomStep" },
                    { "BeforeStep", "MarkStep" },
                    { "AfterStep", "MarkStep" }
                })
            };
            var task = new MockTask()
            {
                CustomSteps = customSteps
            };
            Assert.Throws<ArgumentException>(() => task.CreateDriver());
        }

        [Fact]
        public void TestCustomStepOrdering()
        {
            var customSteps = new ITaskItem[] {
                new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                    { "Type", "ILLink.Tasks.Tests.MockCustomStep" },
                    { "BeforeStep", "MarkStep" }
                }),
                new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                    { "Type", "ILLink.Tasks.Tests.MockCustomStep2" },
                    { "BeforeStep", "MarkStep" }
                }),
                new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                    { "Type", "ILLink.Tasks.Tests.MockCustomStep3" },
                    { "AfterStep", "MarkStep" }
                }),
                new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                    { "Type", "ILLink.Tasks.Tests.MockCustomStep4" },
                    { "AfterStep", "MarkStep" }
                }),
                new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                    { "Type", "ILLink.Tasks.Tests.MockCustomStep5" },
                }),
                new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                    { "Type", "ILLink.Tasks.Tests.MockCustomStep6" },
                }),
                new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                    { "Type", "ILLink.Tasks.Tests.MockMarkHandler" }
                }),
                new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                    { "Type", "ILLink.Tasks.Tests.MockMarkHandler2" },
                    { "BeforeStep", "MockMarkHandler" }
                }),
                new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                    { "Type", "ILLink.Tasks.Tests.MockMarkHandler3" },
                    { "AfterStep", "MockMarkHandler2" }
                }),
                new TaskItem(Assembly.GetExecutingAssembly().Location, new Dictionary<string, string> {
                    { "Type", "ILLink.Tasks.Tests.MockMarkHandler4" }
                }),
            };
            var task = new MockTask()
            {
                CustomSteps = customSteps
            };
            using (var driver = task.CreateDriver())
            {
                var actualSteps = driver.Context.Pipeline.GetSteps().Select(s => s.GetType().Name).ToList();
                Assert.Equal(new List<string> {
                    "MockCustomStep",
                    "MockCustomStep2",
                    "MarkStep",
                    "MockCustomStep4",
                    "MockCustomStep3",
                }, actualSteps.Skip(actualSteps.IndexOf("MarkStep") - 2).Take(5).ToList());
                Assert.Equal(new List<string> {
                    "MockCustomStep5",
                    "MockCustomStep6"
                }, actualSteps.TakeLast(2).ToList());
                var actualMarkHandlers = driver.Context.Pipeline.MarkHandlers.Select(h => h.GetType().Name).ToList();
                Assert.Equal(new List<string> {
                    "MockMarkHandler2",
                    "MockMarkHandler3",
                    "MockMarkHandler",
                    "MockMarkHandler4"
                }, actualMarkHandlers.TakeLast(4).ToList());
            }
        }

        [Fact]
        public void TestCustomStepsMissingType()
        {
            var customSteps = new ITaskItem[] {
                new TaskItem(Assembly.GetExecutingAssembly().Location)
            };
            var task = new MockTask()
            {
                CustomSteps = customSteps
            };

            Assert.Throws<ArgumentException>(() => task.CreateDriver());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void TaskPreparesEmptyOutputDirectory(bool exists, bool populated)
        {
            using var test = new OutputDirectoryTest();
            if (exists)
                Directory.CreateDirectory(test.Output);
            if (populated)
            {
                Directory.CreateDirectory(Path.Combine(test.Output, "fr"));
                Directory.CreateDirectory(Path.Combine(test.Output, "empty"));
                File.WriteAllText(Path.Combine(test.Output, "old.dll"), "old assembly");
                File.WriteAllText(Path.Combine(test.Output, "old.dll.config"), "old configuration");
                File.WriteAllText(Path.Combine(test.Output, "fr", "old.resources.dll"), "old satellite");
            }
            string sibling = Path.Combine(test.Root, "unrelated.txt");
            File.WriteAllText(sibling, "keep");

            Assert.True(test.Task.Execute());
            Assert.True(Directory.Exists(test.Output));
            Assert.Empty(Directory.EnumerateFileSystemEntries(test.Output));
            Assert.Equal("keep", File.ReadAllText(sibling));
            Assert.Empty(test.BuildEngine.Errors);
        }

        [Fact]
        public void TaskOutputPreparationFailureDoesNotRunTool()
        {
            using var test = new OutputDirectoryTest();
            File.WriteAllText(test.Output, "not a directory");

            Assert.False(test.Task.Execute());
            Assert.Equal("not a directory", File.ReadAllText(test.Output));
            Assert.Contains(test.BuildEngine.Errors, error => error.Message.Contains(test.Output));
            Assert.Empty(test.Task.Messages);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("-o")]
        [InlineData("-out")]
        public void TaskWritesOutputsWithConfiguredOwnership(string outputOption)
        {
            using var test = new OutputDirectoryTest();
            string input = Path.Combine(test.Root, "Input.dll");
            using (var assembly = Mono.Cecil.AssemblyDefinition.CreateAssembly(
                new Mono.Cecil.AssemblyNameDefinition("Input", new Version(1, 0)), "Input", Mono.Cecil.ModuleKind.Dll))
            {
                assembly.Write(input);
            }

            test.Task.AssemblyPaths = new ITaskItem[]
            {
                new TaskItem(input, new Dictionary<string, string> { { "TrimMode", "copy" } })
            };
            test.Task.RootAssemblyNames = new ITaskItem[] { new TaskItem("Input") };
            string alternateOutput = Path.Combine(test.Root, "alternate output");
            Directory.CreateDirectory(alternateOutput);
            string unrelated = Path.Combine(alternateOutput, "unrelated.txt");
            File.WriteAllText(unrelated, "keep");
            test.Task.ExtraArgs = outputOption is null ? null : $"{outputOption} \"{alternateOutput}\"";
            string outputAssembly = Path.Combine(test.Output, "Input.dll");

            Assert.True(test.Task.Execute(), string.Join(Environment.NewLine, test.Task.Messages.Select(message => message.Line)));
            Assert.Equal(File.ReadAllBytes(input), File.ReadAllBytes(outputAssembly));
            Assert.False(File.Exists(Path.Combine(alternateOutput, "Input.dll")));
            Assert.True(Directory.Exists(test.Output));
            string stale = Path.Combine(test.Output, "nested", "stale.txt");
            for (int i = 0; i < 2; i++)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(stale));
                File.WriteAllText(stale, "old output");
                File.WriteAllText(outputAssembly, "replace with current output");
                Assert.True(test.Task.Execute(), string.Join(Environment.NewLine, test.Task.Messages.Select(message => message.Line)));
                Assert.False(File.Exists(stale));
                Assert.Equal(File.ReadAllBytes(input), File.ReadAllBytes(outputAssembly));
                Assert.False(File.Exists(Path.Combine(alternateOutput, "Input.dll")));
                Assert.Equal("keep", File.ReadAllText(unrelated));
            }
        }

        [Fact]
        public void TestErrorHandling()
        {
            using var test = new OutputDirectoryTest();
            Directory.CreateDirectory(test.Output);
            string stale = Path.Combine(test.Output, "stale.dll");
            File.WriteAllText(stale, "old assembly");
            test.Task.ExtraArgs = null;

            Assert.False(test.Task.Execute());
            Assert.False(File.Exists(stale));
            Assert.Contains(test.Task.Messages, message =>
                message.Line.Contains("No input files were specified"));
        }

        [Fact]
        public void CacheHasNoTaskParameters()
        {
            Assert.DoesNotContain(typeof(ILLink).GetProperties(), property => property.Name.Contains("Cache"));
        }

        [Theory]
        [InlineData(null, false, false)]
        [InlineData("", false, false)]
        [InlineData("false", false, false)]
        [InlineData("FALSE", false, false)]
        [InlineData("true", true, false)]
        [InlineData("TrUe", true, false)]
        [InlineData("1", false, true)]
        [InlineData("invalid", false, true)]
        public void CacheEnvironmentConfiguration(string setting, bool enabled, bool invalid)
        {
            RemoteExecutor.Invoke(static (value, expectedEnabled, expectedInvalid) =>
            {
                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE", value == "<unset>" ? null : value);
                string directory = Path.Combine(Path.GetTempPath(), "illink-cache-" + Guid.NewGuid().ToString("N"));
                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE_PATH", directory);
                var buildEngine = new MockBuildEngine();
                var task = new MockTask { BuildEngine = buildEngine };
                ILLinkCache cache = ILLinkCache.TryCreateFromEnvironment(task.Log);

                Assert.Equal(bool.Parse(expectedEnabled), cache is not null);
                if (cache is not null)
                    Assert.Equal(directory, cache.CacheDirectory);
                Assert.Equal(bool.Parse(expectedInvalid), buildEngine.Messages.Any(message =>
                    message.Message.Contains("ILLINK_EXPERIMENTAL_CACHE must be 'true' or 'false'")));
                Assert.False(Directory.Exists(directory));
            }, setting ?? "<unset>", enabled.ToString(), invalid.ToString()).Dispose();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("true")]
        [InlineData("false")]
        [InlineData("cache directory")]
        public void CacheEnvironmentDirectory(string directory)
        {
            RemoteExecutor.Invoke(static value =>
            {
                string path = value == "<unset>" ? null : value;
                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE", "true");
                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE_PATH", path);
                var task = new MockTask { BuildEngine = new MockBuildEngine() };

                ILLinkCache cache = ILLinkCache.TryCreateFromEnvironment(task.Log);

                Assert.NotNull(cache);
                Assert.Equal(ILLinkCache.TryCreate(path, task.Log).CacheDirectory, cache.CacheDirectory);
            }, directory ?? "<unset>").Dispose();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void CacheUsesPlatformDefaultDirectory(string cacheDirectory)
        {
            var task = new MockTask { BuildEngine = new MockBuildEngine() };
            string expectedDirectory;
            if (OperatingSystem.IsWindows())
            {
                expectedDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "illink");
            }
            else if (OperatingSystem.IsMacOS())
            {
                expectedDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Caches", "illink");
            }
            else
            {
                string cacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
                expectedDirectory = !string.IsNullOrEmpty(cacheHome) && Path.IsPathRooted(cacheHome)
                    ? Path.Combine(cacheHome, "illink")
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "illink");
            }

            ILLinkCache cache = ILLinkCache.TryCreate(cacheDirectory, task.Log);

            Assert.NotNull(cache);
            Assert.Equal(Path.GetFullPath(expectedDirectory), cache.CacheDirectory);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CacheDirectoryOverrideDoesNotCreateDirectory(bool absolutePath)
        {
            string directoryName = "illink-cache-" + Guid.NewGuid().ToString("N");
            string cacheDirectory = absolutePath ? Path.Combine(Path.GetTempPath(), directoryName) : directoryName;
            var task = new MockTask { BuildEngine = new MockBuildEngine() };

            ILLinkCache cache = ILLinkCache.TryCreate(cacheDirectory, task.Log);

            Assert.NotNull(cache);
            Assert.Equal(Path.GetFullPath(cacheDirectory), cache.CacheDirectory);
            Assert.False(Directory.Exists(cache.CacheDirectory));
        }

        [Fact]
        public void InvalidCacheDirectoryBypassesCaching()
        {
            var buildEngine = new MockBuildEngine();
            var task = new MockTask { BuildEngine = buildEngine };

            Assert.Null(ILLinkCache.TryCreate("invalid\0path", task.Log));
            Assert.Contains(buildEngine.Messages, message =>
                message.Message.Contains("could not be resolved"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CacheRoundTrip(bool includeSymbols)
        {
            using var test = new CacheTestDirectory();
            var files = new Dictionary<string, string>
            {
                ["app.dll"] = "assembly",
                ["app.dll.config"] = "configuration",
                [Path.Combine("fr", "app.resources.dll")] = "satellite",
                ["Link.semaphore"] = "ordinary file"
            };
            if (includeSymbols)
                files.Add("app.pdb", "symbols");

            foreach (var file in files)
                test.WriteSource(file.Key, file.Value);

            Assert.False(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
            Assert.False(Directory.Exists(test.Cache.CacheDirectory));
            test.Cache.Store(CacheTestDirectory.Key, test.Source);

            Directory.CreateDirectory(test.Output);
            File.WriteAllText(Path.Combine(test.Output, "app.dll"), "old assembly");
            File.WriteAllText(Path.Combine(test.Output, "Link.semaphore"), "existing semaphore");
            Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));

            foreach (var file in files)
            {
                string path = Path.Combine(test.Output, file.Key);
                Assert.Equal(file.Value, File.ReadAllText(path));
                Assert.Equal(CacheTestDirectory.Timestamp, File.GetLastWriteTimeUtc(path));
            }
            Assert.Equal("ordinary file", File.ReadAllText(Path.Combine(test.Output, "Link.semaphore")));
            Assert.Equal(files.Count, Directory.GetFiles(Path.Combine(test.Entry, "outputs"), "*", SearchOption.AllDirectories).Length);
            Assert.Empty(Directory.GetDirectories(Path.GetDirectoryName(test.Entry), "*.tmp"));

            File.WriteAllText(Path.Combine(test.Output, "app.dll"), "modified restored output");
            Assert.Equal("assembly", File.ReadAllText(Path.Combine(test.Entry, "outputs", "app.dll")));
        }

        [Fact]
        public void CacheSupportsEmptyOutput()
        {
            using var test = new CacheTestDirectory();
            test.Cache.Store(CacheTestDirectory.Key, test.Source);
            Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
            Assert.True(Directory.Exists(test.Output));
            Assert.Empty(Directory.GetFileSystemEntries(test.Output));
        }

        [Fact]
        public void CacheRecordsLastUsedOnStoreAndHit()
        {
            using var test = new CacheTestDirectory();
            test.WriteSource("app.dll", "assembly");
            DateTimeOffset beforeStore = DateTimeOffset.UtcNow;
            test.Cache.Store(CacheTestDirectory.Key, test.Source);
            string marker = Path.Combine(test.Entry, ILLinkCacheEntry.LastUsedFileName);
            Assert.InRange(DateTimeOffset.ParseExact(File.ReadAllText(marker), "O", CultureInfo.InvariantCulture),
                beforeStore, DateTimeOffset.UtcNow);
            File.WriteAllText(marker, DateTimeOffset.MinValue.ToString("O", CultureInfo.InvariantCulture));

            DateTimeOffset beforeRestore = DateTimeOffset.UtcNow;
            Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));

            Assert.InRange(DateTimeOffset.ParseExact(File.ReadAllText(marker), "O", CultureInfo.InvariantCulture),
                beforeRestore, DateTimeOffset.UtcNow);
            Assert.False(File.Exists(Path.Combine(test.Output, ILLinkCacheEntry.LastUsedFileName)));
            Assert.Empty(Directory.GetFiles(test.Entry, "*.tmp"));
        }

        [Fact]
        public void CacheMarkerUpdateFailureDoesNotFailRestore()
        {
            using var test = new CacheTestDirectory();
            test.WriteSource("app.dll", "assembly");
            test.Cache.Store(CacheTestDirectory.Key, test.Source);
            string marker = Path.Combine(test.Entry, ILLinkCacheEntry.LastUsedFileName);
            File.Delete(marker);
            Directory.CreateDirectory(marker);

            Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));

            Assert.Equal("assembly", File.ReadAllText(Path.Combine(test.Output, "app.dll")));
            Assert.Contains(test.BuildEngine.Messages, message => message.Message.Contains("last-used update failed"));
            Assert.Empty(Directory.GetFiles(test.Entry, "*.tmp"));
            Assert.Empty(test.BuildEngine.Errors);
        }

        [Fact]
        public void FailedRestoreDoesNotRefreshUsage()
        {
            using var test = new CacheTestDirectory();
            test.WriteSource("app.dll", "assembly");
            test.Cache.Store(CacheTestDirectory.Key, test.Source);
            string marker = Path.Combine(test.Entry, ILLinkCacheEntry.LastUsedFileName);
            string oldTimestamp = DateTimeOffset.MinValue.ToString("O", CultureInfo.InvariantCulture);
            File.WriteAllText(marker, oldTimestamp);
            File.WriteAllText(Path.Combine(test.Entry, "outputs", "app.dll"), "corrupt");

            Assert.False(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));

            Assert.Equal(oldTimestamp, File.ReadAllText(marker));
        }

        [Fact]
        public void ParallelProcessesRefreshUsageWithoutCorruptingMarker()
        {
            using var test = new CacheTestDirectory();
            test.WriteSource("app.dll", "assembly");
            test.Cache.Store(CacheTestDirectory.Key, test.Source);
            DateTimeOffset beforeRestore = DateTimeOffset.UtcNow;
            using (StartReader(test, 0))
            using (StartReader(test, 1))
            {
                Assert.True(SpinWait.SpinUntil(() => Directory.GetFiles(test.Root, "*.ready").Length == 2, TimeSpan.FromSeconds(30)));
                File.WriteAllText(Path.Combine(test.Root, "start"), "");
            }

            string marker = Path.Combine(test.Entry, ILLinkCacheEntry.LastUsedFileName);
            Assert.InRange(DateTimeOffset.ParseExact(File.ReadAllText(marker), "O", CultureInfo.InvariantCulture),
                beforeRestore, DateTimeOffset.UtcNow);
            Assert.Empty(Directory.GetFiles(test.Entry, "*.tmp"));

            static RemoteInvokeHandle StartReader(CacheTestDirectory test, int index) =>
                RemoteExecutor.Invoke(static (cacheDirectory, output, start) =>
                {
                    var engine = new MockBuildEngine();
                    var task = new MockTask { BuildEngine = engine };
                    ILLinkCache cache = ILLinkCache.TryCreate(cacheDirectory, task.Log);
                    File.WriteAllText(output + ".ready", "");
                    Assert.True(SpinWait.SpinUntil(() => File.Exists(start), TimeSpan.FromSeconds(30)));
                    for (int attempt = 0; attempt < 20; attempt++)
                        Assert.True(cache.TryRestore(CacheTestDirectory.Key, output, CacheTestDirectory.Timestamp));
                    Assert.Empty(engine.Errors);
                }, test.Cache.CacheDirectory, Path.Combine(test.Root, "reader-" + index), Path.Combine(test.Root, "start"));
        }

        [Fact]
        public void CacheDoesNotReplaceExistingEntry()
        {
            using var test = new CacheTestDirectory();
            test.WriteSource("app.dll", "first");
            test.Cache.Store(CacheTestDirectory.Key, test.Source);
            test.WriteSource("app.dll", "second");
            test.Cache.Store(CacheTestDirectory.Key, test.Source);
            Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
            Assert.Equal("first", File.ReadAllText(Path.Combine(test.Output, "app.dll")));
            Assert.Contains(test.BuildEngine.Messages, message => message.Message.Contains("already exists"));
            Assert.Empty(Directory.GetDirectories(Path.GetDirectoryName(test.Entry), "*.tmp"));
        }

        [Theory]
        [InlineData("missing-manifest")]
        [InlineData("version")]
        [InlineData("truncated")]
        [InlineData("missing-file")]
        [InlineData("corrupt-file")]
        [InlineData("escaping-path")]
        public void CacheRejectsInvalidEntryBeforeCopying(string damage)
        {
            using var test = new CacheTestDirectory();
            test.WriteSource("a.dll", "first");
            test.WriteSource("b.dll", "second");
            test.Cache.Store(CacheTestDirectory.Key, test.Source);
            string manifest = Path.Combine(test.Entry, "manifest");
            string cachedFile = Path.Combine(test.Entry, "outputs", "b.dll");
            switch (damage)
            {
                case "missing-manifest":
                    File.Delete(manifest);
                    break;
                case "version":
                    using (var writer = new BinaryWriter(File.OpenWrite(manifest)))
                        writer.Write(-1);
                    break;
                case "truncated":
                    File.WriteAllBytes(manifest, new byte[] { 1 });
                    break;
                case "missing-file":
                    File.Delete(cachedFile);
                    break;
                case "corrupt-file":
                    File.WriteAllText(cachedFile, "broken");
                    break;
                case "escaping-path":
                    using (var writer = new BinaryWriter(File.Create(manifest)))
                    {
                        writer.Write(1);
                        writer.Write(1);
                        writer.Write("../escaped.dll");
                    }
                    break;
            }

            Directory.CreateDirectory(test.Output);
            string existingOutput = Path.Combine(test.Output, "a.dll");
            File.WriteAllText(existingOutput, "unchanged");
            Assert.False(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
            Assert.Equal("unchanged", File.ReadAllText(existingOutput));
            Assert.False(File.Exists(Path.Combine(test.Output, "b.dll")));
            Assert.False(File.Exists(Path.Combine(test.Root, "escaped.dll")));
            Assert.True(Directory.Exists(test.Entry));
            Assert.Contains(test.BuildEngine.Messages, message => message.Message.Contains("restore failed"));
        }

        [Fact]
        public void CacheRestoreReplacesExistingDirectory()
        {
            using var test = new CacheTestDirectory();
            test.WriteSource("a.dll", "new assembly");
            test.WriteSource("b.dll", "other assembly");
            test.Cache.Store(CacheTestDirectory.Key, test.Source);
            Directory.CreateDirectory(Path.Combine(test.Output, "b.dll"));
            File.WriteAllText(Path.Combine(test.Output, "a.dll"), "old assembly");

            Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
            Assert.Equal("new assembly", File.ReadAllText(Path.Combine(test.Output, "a.dll")));
            Assert.Equal("other assembly", File.ReadAllText(Path.Combine(test.Output, "b.dll")));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CacheStoreFailureIsBestEffort(bool failPublication)
        {
            using var test = new CacheTestDirectory();
            test.WriteSource("app.dll", "assembly");
            if (failPublication)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(test.Entry));
                File.WriteAllText(test.Entry, "blocks publication");
            }
            else
            {
                File.WriteAllText(test.Cache.CacheDirectory, "blocks cache creation");
            }

            test.Cache.Store(CacheTestDirectory.Key, test.Source);

            Assert.Equal("assembly", File.ReadAllText(Path.Combine(test.Source, "app.dll")));
            Assert.False(Directory.Exists(test.Entry));
            Assert.Contains(test.BuildEngine.Messages, message => message.Message.Contains("store failed"));
            if (failPublication)
            {
                Assert.Equal("blocks publication", File.ReadAllText(test.Entry));
                Assert.Empty(Directory.GetDirectories(Path.GetDirectoryName(test.Entry), "*.tmp"));
            }
        }

        [Fact]
        public void CacheSkipsContendedEntryButNotOtherKeys()
        {
            using var test = new CacheTestDirectory();
            test.WriteSource("app.dll", "assembly");
            using var mutex = new Mutex(false, test.MutexName);
            Assert.True(mutex.WaitOne(0));
            try
            {
                RunOnOtherThread(() => test.Cache.Store(CacheTestDirectory.Key, test.Source));
                Assert.False(Directory.Exists(test.Cache.CacheDirectory));
                Assert.Contains(test.BuildEngine.Messages, message => message.Message.Contains("another writer"));

                string otherKey = new string('b', 64);
                RunOnOtherThread(() => test.Cache.Store(otherKey, test.Source));
                Assert.True(test.Cache.TryRestore(otherKey, test.Output, CacheTestDirectory.Timestamp));
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            test.Cache.Store(CacheTestDirectory.Key, test.Source);
            Assert.True(Directory.Exists(test.Entry));
        }

        [Fact]
        public void CacheReadsPublishedEntryWithoutWriterLock()
        {
            using var test = new CacheTestDirectory();
            test.WriteSource("app.dll", "assembly");
            test.Cache.Store(CacheTestDirectory.Key, test.Source);
            using var mutex = new Mutex(false, test.MutexName);
            Assert.True(mutex.WaitOne(0));
            try
            {
                RunOnOtherThread(() =>
                    Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp)));
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        [Fact]
        public void CacheSupportsConcurrentWritersWithDifferentOutputs()
        {
            using var test = new CacheTestDirectory();
            string contents = new string('a', 1024 * 1024);
            System.Threading.Tasks.Parallel.For(0, 8, index =>
            {
                string source = Path.Combine(test.Root, "source-" + index);
                Directory.CreateDirectory(source);
                File.WriteAllText(Path.Combine(source, "app.dll"), contents);
                File.WriteAllText(Path.Combine(source, "app.pdb"), "symbols");
                var task = new MockTask { BuildEngine = new MockBuildEngine() };
                ILLinkCache cache = ILLinkCache.TryCreate(test.Cache.CacheDirectory, task.Log);

                cache.Store(CacheTestDirectory.Key, source);
            });

            Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
            Assert.Equal(contents, File.ReadAllText(Path.Combine(test.Output, "app.dll")));
            Assert.Equal("symbols", File.ReadAllText(Path.Combine(test.Output, "app.pdb")));
            Assert.Single(Directory.GetDirectories(Path.GetDirectoryName(test.Entry)));
        }

        [Fact]
        public void CacheIgnoresOtherWritersStagingDirectories()
        {
            using var test = new CacheTestDirectory();
            string stagingDirectory = test.Entry + ".orphan.tmp";
            Directory.CreateDirectory(stagingDirectory);
            string marker = Path.Combine(stagingDirectory, "partial");
            File.WriteAllText(marker, "other attempt");

            Assert.False(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
            test.Cache.Store(CacheTestDirectory.Key, test.Source);

            Assert.Equal("other attempt", File.ReadAllText(marker));
            Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CacheRejectsOverlappingDirectories(bool cacheContainsOutput)
        {
            using var test = new CacheTestDirectory();
            string output = cacheContainsOutput ? Path.Combine(test.Cache.CacheDirectory, "output") : test.Root;

            Assert.Throws<ArgumentException>(() => test.Cache.Store(CacheTestDirectory.Key, output));
            Assert.Throws<ArgumentException>(() => test.Cache.TryRestore(CacheTestDirectory.Key, output, CacheTestDirectory.Timestamp));
            Assert.False(Directory.Exists(test.Cache.CacheDirectory));
        }

        [Fact]
        public void CacheRecoversAbandonedWriterLock()
        {
            using var test = new CacheTestDirectory();
            test.WriteSource("app.dll", "assembly");
            using var mutex = new Mutex(false, test.MutexName);
            var owner = new Thread(() => mutex.WaitOne());
            owner.Start();
            owner.Join();

            test.Cache.Store(CacheTestDirectory.Key, test.Source);

            Assert.True(test.Cache.TryRestore(CacheTestDirectory.Key, test.Output, CacheTestDirectory.Timestamp));
            Assert.Contains(test.BuildEngine.Messages, message => message.Message.Contains("abandoned"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("../entry")]
        [InlineData("not-a-hash")]
        public void CacheRejectsInvalidInputHash(string inputHash)
        {
            using var test = new CacheTestDirectory();
            Assert.Throws<ArgumentException>(() => test.Cache.Store(inputHash, test.Source));
            Assert.Throws<ArgumentException>(() => test.Cache.TryRestore(inputHash, test.Output, CacheTestDirectory.Timestamp));
            Assert.False(Directory.Exists(test.Cache.CacheDirectory));
        }

        private static void RunOnOtherThread(Action action)
        {
            ExceptionDispatchInfo exception = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exception = ExceptionDispatchInfo.Capture(ex);
                }
            });
            thread.Start();
            thread.Join();
            exception?.Throw();
        }

        private sealed class CacheTestDirectory : IDisposable
        {
            internal static readonly string Key = new string('a', 64);
            internal static readonly DateTime Timestamp = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            internal string Root { get; } = Path.Combine(Path.GetTempPath(), "illink-cache-tests-" + Guid.NewGuid().ToString("N"));
            internal string Source => Path.Combine(Root, "source");
            internal string Output => Path.Combine(Root, "output");
            internal string Entry => Path.Combine(Cache.CacheDirectory, "v1", Key);
            internal MockBuildEngine BuildEngine { get; } = new();
            internal ILLinkCache Cache { get; }
            internal string MutexName => (string)typeof(ILLinkCache).GetMethod("GetWriterMutexName", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Cache, new object[] { Key });

            internal CacheTestDirectory()
            {
                Directory.CreateDirectory(Source);
                var task = new MockTask { BuildEngine = BuildEngine };
                Cache = ILLinkCache.TryCreate(Path.Combine(Root, "cache"), task.Log);
            }

            internal void WriteSource(string relativePath, string contents)
            {
                string path = Path.Combine(Source, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, contents);
            }

            public void Dispose() => Directory.Delete(Root, recursive: true);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CacheOptionsAreNotLinkerArguments(bool enableCache)
        {
            RemoteExecutor.Invoke(static value =>
            {
                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE", null);
                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE_PATH", null);
                string arguments = new MockTask().GetResponseFileCommands();
                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE", value);
                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE_PATH", "cache directory");

                Assert.Equal(arguments, new MockTask().GetResponseFileCommands());
            }, enableCache.ToString()).Dispose();
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void TaskDoesNotCacheUnsupportedOrFailedInvocations(bool enableCache, bool succeeds)
        {
            RemoteExecutor.Invoke(static (enabled, success) =>
            {
                bool succeeds = bool.Parse(success);
                using var test = new OutputDirectoryTest();
                string cacheDirectory = Path.Combine(test.Root, "cache");
                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE", enabled);
                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE_PATH", cacheDirectory);
                var task = test.Task;
                task.ExtraArgs = succeeds ? "--help" : null;
                Directory.CreateDirectory(test.Output);
                string stale = Path.Combine(test.Output, "stale.dll");
                File.WriteAllText(stale, "old output");

                Assert.Equal(succeeds, task.Execute());
                Assert.False(File.Exists(stale));
                Assert.Equal(succeeds ? 0 : 1, task.ExitCode);
                Assert.Contains(task.Messages, message =>
                    message.Line.Contains(succeeds ? "illink [options]" : "No input files were specified"));
                Assert.Equal(bool.Parse(enabled) && succeeds, test.BuildEngine.Messages.Any(message =>
                    message.Message.StartsWith("ILLink cache bypassed: extra arguments")));
                Assert.False(Directory.Exists(cacheDirectory));
            }, enableCache.ToString(), succeeds.ToString()).Dispose();
        }

        [Theory]
        [InlineData("inputs/Input.dll")]
        [InlineData("inputs/Reference.dll")]
        [InlineData("roots.xml")]
        [InlineData("host/dotnet")]
        public void CacheKeyTracksFileContentsNotTimestamps(string relativePath)
        {
            using var test = new OutputDirectoryTest();
            string host = PrepareCacheKeyTest(test);
            Assert.True(test.Task.TryGetCacheKey(host, out string original));
            Assert.Matches("^[0-9a-f]{64}$", original);

            string file = Path.Combine(test.Root, relativePath);
            DateTime timestamp = File.GetLastWriteTimeUtc(file);
            File.SetLastWriteTimeUtc(file, timestamp.AddHours(-1));
            Assert.True(test.Task.TryGetCacheKey(host, out string afterTouch));
            Assert.Equal(original, afterTouch);

            byte[] contents = File.ReadAllBytes(file);
            contents[contents.Length - 1] ^= 1;
            File.WriteAllBytes(file, contents);
            File.SetLastWriteTimeUtc(file, timestamp);
            Assert.True(test.Task.TryGetCacheKey(host, out string afterChange));
            Assert.NotEqual(original, afterChange);
        }

        [Theory]
        [InlineData("--help")]
        [InlineData("--ignore-link-attributes true")]
        [InlineData("--link-attributes attributes.xml")]
        [InlineData("--ignore-link-attributes true --substitutions substitutions.xml")]
        [InlineData("--ignore-link-attributes true -d assemblies")]
        public void CacheKeyBypassesUnsupportedExtraArgs(string extraArgs)
        {
            using var test = new OutputDirectoryTest();
            string host = PrepareCacheKeyTest(test);
            test.Task.ExtraArgs = extraArgs;

            Assert.False(test.Task.TryGetCacheKey(host, out string key));
            Assert.Empty(key);
            Assert.Contains(test.BuildEngine.Messages, message =>
                message.Message.StartsWith("ILLink cache bypassed: extra arguments are not supported."));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("1.0.0+test")]
        public void CacheKeyUsesLinkerContents(string informationalVersion)
        {
            using var test = new OutputDirectoryTest();
            string host = PrepareCacheKeyTest(test);
            Guid mvid = Guid.NewGuid();
            WriteTestAssembly(test.Task.ILLinkPath, informationalVersion, mvid);
            Assert.True(test.Task.TryGetCacheKey(host, out string original));
            byte[] originalImage = File.ReadAllBytes(test.Task.ILLinkPath);

            WriteTestAssembly(test.Task.ILLinkPath, "1.0.0+changed", mvid);
            Assert.NotEqual(originalImage, File.ReadAllBytes(test.Task.ILLinkPath));
            Assert.True(test.Task.TryGetCacheKey(host, out string sameMvid));
            Assert.NotEqual(original, sameMvid);

            WriteTestAssembly(test.Task.ILLinkPath, "1.0.0+changed", Guid.NewGuid());
            Assert.True(test.Task.TryGetCacheKey(host, out string changedMvid));
            Assert.NotEqual(sameMvid, changedMvid);

            byte[] image = File.ReadAllBytes(test.Task.ILLinkPath);
            DateTime timestamp = File.GetLastWriteTimeUtc(test.Task.ILLinkPath);
            image[image.Length - 1] ^= 1;
            File.WriteAllBytes(test.Task.ILLinkPath, image);
            File.SetLastWriteTimeUtc(test.Task.ILLinkPath, timestamp);
            Assert.True(test.Task.TryGetCacheKey(host, out string sameLengthAndTimestamp));
            Assert.NotEqual(changedMvid, sameLengthAndTimestamp);

            File.SetLastWriteTimeUtc(test.Task.ILLinkPath, timestamp.AddSeconds(1));
            Assert.True(test.Task.TryGetCacheKey(host, out string timestampOnly));
            Assert.Equal(sameLengthAndTimestamp, timestampOnly);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CacheKeyHashesLinkerWithoutMvid(bool hasMetadata)
        {
            using var test = new OutputDirectoryTest();
            string host = PrepareCacheKeyTest(test);
            if (hasMetadata)
                WriteTestAssembly(test.Task.ILLinkPath, mvid: Guid.Empty);
            else
                WriteMetadataLessPE(test.Task.ILLinkPath);

            Assert.True(test.Task.TryGetCacheKey(host, out string original));
            File.AppendAllText(test.Task.ILLinkPath, "changed");
            Assert.True(test.Task.TryGetCacheKey(host, out string changed));
            Assert.NotEqual(original, changed);
        }

        [Theory]
        [InlineData("Mono.Cecil.dll")]
        [InlineData("illink.runtimeconfig.json")]
        [InlineData("illink.deps.json")]
        [InlineData("unrelated/generated.xml")]
        public void CacheKeyIgnoresToolDirectoryFiles(string relativePath)
        {
            using var test = new OutputDirectoryTest();
            string host = PrepareCacheKeyTest(test);
            Assert.True(test.Task.TryGetCacheKey(host, out string original));

            string file = Path.Combine(Path.GetDirectoryName(test.Task.ILLinkPath), relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, "tool directory contents");
            Assert.True(test.Task.TryGetCacheKey(host, out string added));
            Assert.Equal(original, added);

            File.WriteAllText(file, "changed tool directory contents");
            Assert.True(test.Task.TryGetCacheKey(host, out string changed));
            Assert.Equal(original, changed);

            File.Delete(file);
            Assert.True(test.Task.TryGetCacheKey(host, out string removed));
            Assert.Equal(original, removed);
        }

        [Theory]
        [InlineData("inputs/Input.pdb")]
        [InlineData("inputs/Input.dll.mdb")]
        [InlineData("inputs/Input.dll.config")]
        [InlineData("inputs/fr/Input.resources.dll")]
        [InlineData("inputs/Reference.pdb")]
        public void CacheKeyTracksOptionalFilePresence(string relativePath)
        {
            using var test = new OutputDirectoryTest();
            string host = PrepareCacheKeyTest(test);
            test.Task.RemoveSymbols = true;
            Assert.True(test.Task.TryGetCacheKey(host, out string original));

            string file = Path.Combine(test.Root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, "sidecar");
            Assert.True(test.Task.TryGetCacheKey(host, out string added));
            Assert.NotEqual(original, added);

            File.WriteAllText(file, "changed");
            Assert.True(test.Task.TryGetCacheKey(host, out string changed));
            Assert.NotEqual(added, changed);

            File.Delete(file);
            Assert.True(test.Task.TryGetCacheKey(host, out string removed));
            Assert.Equal(original, removed);
        }

        [Theory]
        [InlineData("argument")]
        [InlineData("metadata")]
        [InlineData("output")]
        [InlineData("input-path")]
        public void CacheKeyTracksInvocation(string change)
        {
            using var test = new OutputDirectoryTest();
            string host = PrepareCacheKeyTest(test);
            Assert.True(test.Task.TryGetCacheKey(host, out string original));
            switch (change)
            {
                case "argument":
                    test.Task.NoWarn = "2026";
                    break;
                case "metadata":
                    test.Task.AssemblyPaths[0].SetMetadata("TrimMode", "copy");
                    break;
                case "output":
                    test.Task.OutputDirectory = new TaskItem(test.Output + "-other");
                    break;
                case "input-path":
                    string newPath = Path.Combine(test.Root, "Input.dll");
                    File.Copy(test.Task.AssemblyPaths[0].ItemSpec, newPath);
                    test.Task.AssemblyPaths[0] = new TaskItem(newPath);
                    break;
            }

            Assert.True(test.Task.TryGetCacheKey(host, out string changed));
            Assert.NotEqual(original, changed);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CacheKeyTracksAssemblyFiles(bool containsMetadata)
        {
            using var test = new OutputDirectoryTest();
            string host = PrepareCacheKeyTest(test);
            string input = test.Task.AssemblyPaths[0].ItemSpec;
            string externalFile;
            if (containsMetadata)
            {
                externalFile = Path.Combine(Path.GetDirectoryName(input), "secondary.netmodule");
                using var module = Mono.Cecil.ModuleDefinition.CreateModule("secondary.netmodule", Mono.Cecil.ModuleKind.NetModule);
                module.Write(externalFile);
                var metadata = new MetadataBuilder();
                metadata.AddModule(0, metadata.GetOrAddString("Input.dll"), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
                metadata.AddAssembly(metadata.GetOrAddString("Input"), new Version(1, 0), default, default, 0, AssemblyHashAlgorithm.Sha256);
                metadata.AddAssemblyFile(metadata.GetOrAddString("secondary.netmodule"), default, containsMetadata: true);
                var pe = new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(metadata), new BlobBuilder());
                var image = new BlobBuilder();
                pe.Serialize(image);
                File.WriteAllBytes(input, image.ToArray());
            }
            else
            {
                externalFile = Path.Combine(Path.GetDirectoryName(input), "linked.resources");
                File.WriteAllText(externalFile, "resource contents");
                using var assembly = Mono.Cecil.AssemblyDefinition.CreateAssembly(
                    new Mono.Cecil.AssemblyNameDefinition("Input", new Version(1, 0)), "Input.dll", Mono.Cecil.ModuleKind.Dll);
                assembly.MainModule.Resources.Add(new Mono.Cecil.LinkedResource(
                    "linked", Mono.Cecil.ManifestResourceAttributes.Public, externalFile));
                assembly.Write(input);
            }

            Assert.True(File.Exists(externalFile));
            Assert.True(test.Task.TryGetCacheKey(host, out string original));
            File.AppendAllText(externalFile, "changed");
            Assert.True(test.Task.TryGetCacheKey(host, out string changed));
            Assert.NotEqual(original, changed);
        }

        [Theory]
        [InlineData("host/fxr")]
        [InlineData("shared")]
        public void CacheKeyIgnoresHostDirectories(string relativePath)
        {
            using var test = new OutputDirectoryTest();
            string host = PrepareCacheKeyTest(test);
            Assert.True(test.Task.TryGetCacheKey(host, out string original));
            string directory = Path.Combine(test.Root, "host", relativePath);
            Directory.CreateDirectory(Path.Combine(directory, "another-version"));
            Assert.True(test.Task.TryGetCacheKey(host, out string addedDirectory));
            Assert.Equal(original, addedDirectory);

            string file = Path.Combine(directory, "another-version", "runtime");
            File.WriteAllText(file, "runtime contents");
            Assert.True(test.Task.TryGetCacheKey(host, out string addedFile));
            Assert.Equal(original, addedFile);

            File.WriteAllText(file, "changed runtime contents");
            Assert.True(test.Task.TryGetCacheKey(host, out string changedFile));
            Assert.Equal(original, changedFile);

            Directory.Delete(directory, recursive: true);
            Assert.True(test.Task.TryGetCacheKey(host, out string removedDirectory));
            Assert.Equal(original, removedDirectory);
        }

        [Theory]
        [InlineData("extra-args")]
        [InlineData("custom-step")]
        [InlineData("custom-data")]
        [InlineData("dump-dependencies")]
        [InlineData("dependencies-format")]
        [InlineData("environment")]
        [InlineData("missing-input")]
        [InlineData("invalid-assembly")]
        [InlineData("metadata-less-assembly")]
        [InlineData("metadata-less-reference")]
        [InlineData("missing-linker")]
        public void CacheKeyBypassesUnsupportedInputs(string reason)
        {
            using var test = new OutputDirectoryTest();
            string host = PrepareCacheKeyTest(test);
            switch (reason)
            {
                case "extra-args":
                    test.Task.ExtraArgs = "--help";
                    break;
                case "custom-step":
                    test.Task.CustomSteps = new ITaskItem[]
                    {
                        new TaskItem("step.dll", new Dictionary<string, string> { { "Type", "Step" } })
                    };
                    break;
                case "custom-data":
                    test.Task.CustomData = new ITaskItem[]
                    {
                        new TaskItem("data", new Dictionary<string, string> { { "Value", "input" } })
                    };
                    break;
                case "dump-dependencies":
                    test.Task.DumpDependencies = true;
                    break;
                case "dependencies-format":
                    test.Task.DependenciesFileFormat = "xml";
                    break;
                case "environment":
                    test.Task.EnvironmentVariables = new[] { "CUSTOM_SETTING=value" };
                    break;
                case "missing-input":
                    File.Delete(test.Task.AssemblyPaths[0].ItemSpec);
                    break;
                case "invalid-assembly":
                    File.WriteAllText(test.Task.AssemblyPaths[0].ItemSpec, "invalid");
                    break;
                case "metadata-less-assembly":
                    WriteMetadataLessPE(test.Task.AssemblyPaths[0].ItemSpec);
                    break;
                case "metadata-less-reference":
                    WriteMetadataLessPE(test.Task.ReferenceAssemblyPaths[0].ItemSpec);
                    break;
                case "missing-linker":
                    File.Delete(test.Task.ILLinkPath);
                    break;
            }

            Assert.False(test.Task.TryGetCacheKey(host, out string key));
            Assert.Empty(key);
            Assert.Contains(test.BuildEngine.Messages, message => message.Message.StartsWith("ILLink cache bypassed:"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TaskLinksWithUnusedMetadataLessReference(bool enableCache)
        {
            RemoteExecutor.Invoke(static enabled =>
            {
                using var test = new OutputDirectoryTest();
                string input = Path.Combine(test.Root, "Input.dll");
                string reference = Path.Combine(test.Root, "Native.dll");
                WriteTestAssembly(input);
                WriteMetadataLessPE(reference);
                var task = test.Task;
                task.AssemblyPaths = new ITaskItem[] { new TaskItem(input, new Dictionary<string, string> { { "TrimMode", "copy" } }) };
                task.RootAssemblyNames = new ITaskItem[] { new TaskItem("Input") };
                task.ReferenceAssemblyPaths = new ITaskItem[] { new TaskItem(reference) };
                task.ExtraArgs = null;
                string cacheDirectory = Path.Combine(test.Root, "cache");
                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE", enabled);
                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE_PATH", cacheDirectory);

                Directory.CreateDirectory(test.Output);
                string stale = Path.Combine(test.Output, "stale.txt");
                File.WriteAllText(stale, "remove before linking");

                Assert.True(task.Execute(), string.Join(Environment.NewLine, task.Messages.Select(message => message.Line)));
                Assert.Empty(test.BuildEngine.Errors);
                Assert.False(File.Exists(stale));
                Assert.Equal(File.ReadAllBytes(input), File.ReadAllBytes(Path.Combine(test.Output, "Input.dll")));
                Assert.False(Directory.Exists(cacheDirectory));
                if (bool.Parse(enabled))
                {
                    Assert.Contains(test.BuildEngine.Messages, message =>
                        message.Message.StartsWith("ILLink cache bypassed: input identity could not be computed:") &&
                        message.Message.Contains("no managed metadata"));
                }
                else
                {
                    Assert.DoesNotContain(test.BuildEngine.Messages, message =>
                        message.Message.StartsWith("ILLink caching is disabled") ||
                        message.Message.StartsWith("ILLink cache bypassed:"));
                }
            }, enableCache.ToString()).Dispose();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TaskCachesAndInvalidatesOutputs(bool corruptEntry)
        {
            RemoteExecutor.Invoke(static corrupt =>
            {
                bool corruptEntry = bool.Parse(corrupt);
                using var test = new OutputDirectoryTest();
                string input = Path.Combine(test.Root, "Input.dll");
                WriteTestAssembly(input);
                var task = test.Task;
                task.AssemblyPaths = new ITaskItem[] { new TaskItem(input, new Dictionary<string, string> { { "TrimMode", "copy" } }) };
                task.RootAssemblyNames = new ITaskItem[] { new TaskItem("Input") };
                task.ExtraArgs = null;
                string cacheDirectory = Path.Combine(test.Root, "cache");
                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE", "true");
                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE_PATH", cacheDirectory);

                Assert.True(task.Execute());
                Assert.Contains(test.BuildEngine.Messages, message => message.Message.StartsWith("ILLink cache stored:"));
                string entry = Assert.Single(Directory.GetDirectories(Path.Combine(cacheDirectory, "v1")));
                string stale = Path.Combine(test.Output, "stale.txt");
                File.WriteAllText(stale, "not part of the cached directory");
                File.Delete(Path.Combine(test.Output, "Input.dll"));
                test.BuildEngine.Messages.Clear();

                Assert.True(task.Execute());
                Assert.Contains(test.BuildEngine.Messages, message => message.Message.StartsWith("ILLink cache hit:"));
                Assert.False(File.Exists(stale));
                Assert.Equal(File.ReadAllBytes(input), File.ReadAllBytes(Path.Combine(test.Output, "Input.dll")));

                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE", "false");
                test.BuildEngine.Messages.Clear();
                File.Delete(Path.Combine(test.Output, "Input.dll"));
                Assert.True(task.Execute());
                Assert.DoesNotContain(test.BuildEngine.Messages, message => message.Message.StartsWith("ILLink cache"));
                Assert.Equal(File.ReadAllBytes(input), File.ReadAllBytes(Path.Combine(test.Output, "Input.dll")));
                Environment.SetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE", "true");

                if (corruptEntry)
                    File.WriteAllText(Path.Combine(entry, "outputs", "Input.dll"), "corrupt");
                else
                    File.AppendAllText(input, "changed");
                File.WriteAllText(stale, "remove before fallback execution");
                test.BuildEngine.Messages.Clear();

                Assert.True(task.Execute());
                Assert.DoesNotContain(test.BuildEngine.Messages, message => message.Message.StartsWith("ILLink cache hit:"));
                Assert.Contains(test.BuildEngine.Messages, message =>
                    message.Message.StartsWith(corruptEntry ? "ILLink cache restore failed" : "ILLink cache miss:"));
                Assert.False(File.Exists(stale));
                Assert.Equal(File.ReadAllBytes(input), File.ReadAllBytes(Path.Combine(test.Output, "Input.dll")));
                Assert.Equal(corruptEntry ? 1 : 2, Directory.GetDirectories(Path.Combine(cacheDirectory, "v1")).Length);
            }, corruptEntry.ToString()).Dispose();
        }

        private static void WriteTestAssembly(string path, string informationalVersion = null, Guid? mvid = null)
        {
            using var assembly = Mono.Cecil.AssemblyDefinition.CreateAssembly(
                new Mono.Cecil.AssemblyNameDefinition(Path.GetFileNameWithoutExtension(path), new Version(1, 0)),
                Path.GetFileName(path), Mono.Cecil.ModuleKind.Dll);
            if (mvid is Guid moduleVersionId)
                assembly.MainModule.Mvid = moduleVersionId;
            if (informationalVersion is not null)
            {
                var constructor = typeof(AssemblyInformationalVersionAttribute).GetConstructor(new[] { typeof(string) });
                var attribute = new Mono.Cecil.CustomAttribute(assembly.MainModule.ImportReference(constructor));
                attribute.ConstructorArguments.Add(new Mono.Cecil.CustomAttributeArgument(assembly.MainModule.TypeSystem.String, informationalVersion));
                assembly.CustomAttributes.Add(attribute);
            }
            assembly.Write(path);
        }

        private static void WriteMetadataLessPE(string path)
        {
            WriteTestAssembly(path);
            byte[] image = File.ReadAllBytes(path);
            using (var pe = new PEReader(new MemoryStream(image)))
            {
                // The CLI header directory precedes the final reserved directory in the PE optional header.
                const int DirectoryEntrySize = 2 * sizeof(int);
                int cliHeaderDirectoryOffset = pe.PEHeaders.PEHeaderStartOffset +
                    pe.PEHeaders.CoffHeader.SizeOfOptionalHeader - 2 * DirectoryEntrySize;
                Array.Clear(image, cliHeaderDirectoryOffset, DirectoryEntrySize);
            }
            File.WriteAllBytes(path, image);
            using var reader = new PEReader(File.OpenRead(path));
            Assert.False(reader.HasMetadata);
        }

        private static string PrepareCacheKeyTest(OutputDirectoryTest test)
        {
            string inputs = Path.Combine(test.Root, "inputs");
            Directory.CreateDirectory(inputs);
            string input = Path.Combine(inputs, "Input.dll");
            string reference = Path.Combine(inputs, "Reference.dll");
            WriteTestAssembly(input);
            WriteTestAssembly(reference);
            foreach (string path in new[]
            {
                "roots.xml", "tools/Mono.Cecil.dll", "tools/Mono.Cecil.Mdb.dll",
                "tools/Mono.Cecil.Pdb.dll", "tools/Mono.Cecil.Rocks.dll", "tools/illink.runtimeconfig.json",
                "host/dotnet", "host/host/fxr/version/hostfxr", "host/shared/Microsoft.NETCore.App/version/runtime"
            })
            {
                string file = Path.Combine(test.Root, path);
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllText(file, "contents");
            }

            test.Task.AssemblyPaths = new ITaskItem[] { new TaskItem(input) };
            test.Task.ReferenceAssemblyPaths = new ITaskItem[] { new TaskItem(reference) };
            test.Task.RootDescriptorFiles = new ITaskItem[] { new TaskItem(Path.Combine(test.Root, "roots.xml")) };
            test.Task.ILLinkPath = Path.Combine(test.Root, "tools", "illink.dll");
            WriteTestAssembly(test.Task.ILLinkPath, "1.0.0+test");
            test.Task.ExtraArgs = null;
            return Path.Combine(test.Root, "host", "dotnet");
        }

        private sealed class OutputDirectoryTest : IDisposable
        {
            public string Root { get; } = Path.Combine(Path.GetTempPath(), "illink-task-output-" + Guid.NewGuid().ToString("N"));
            public string Output => Path.Combine(Root, "linked output");
            public MockBuildEngine BuildEngine { get; } = new MockBuildEngine();
            public MockTask Task { get; }

            public OutputDirectoryTest()
            {
                Directory.CreateDirectory(Root);
                string corelibPath = typeof(object).Assembly.Location;
                string dotnetRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(corelibPath))));
                string dotnetName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
                Assert.True(File.Exists(Path.Combine(dotnetRoot, dotnetName)));
                Task = new MockTask
                {
                    BuildEngine = BuildEngine,
                    OutputDirectory = new TaskItem(Output + Path.DirectorySeparatorChar),
                    ExtraArgs = "--help",
                    ToolPath = dotnetRoot,
                    ToolExe = dotnetName
                };
            }

            public void Dispose() => Directory.Delete(Root, recursive: true);
        }
    }
}
