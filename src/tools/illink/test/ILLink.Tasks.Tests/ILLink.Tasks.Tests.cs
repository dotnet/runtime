using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ILLink.Tasks;
using Mono.Linker;
using Mono.Linker.Steps;

namespace ILLink.Tasks.Tests
{
	// These tests ensure that the task options correctly flow from
	// the task -> response file -> parsed arguments -> options on LinkContext
	public class TaskArgumentTests
	{
		public static IEnumerable<object []> AssemblyPathsCases => new List<object []> {
			new object [] {
				new ITaskItem [] {
					new TaskItem ("Assembly.dll", new Dictionary<string, string> { { "action", "copy" } })
				}
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem ("Assembly.dll", new Dictionary<string, string> { { "Action", "Copy" } })
				}
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem ("path with/spaces/Assembly.dll")
				}
			},
			new object [] {
				new ITaskItem [] {
					// same path
					new TaskItem ("path/to/Assembly1.dll"),
					new TaskItem ("path/to/Assembly2.dll")
				}
			},
			new object [] {
				new ITaskItem [] {
					// same assembly
					new TaskItem ("path/to/Assembly.dll"),
					new TaskItem ("path/to/Assembly.dll")
				}
			},
			new object [] {
				new ITaskItem [] {
					// same assembly name, different paths
					new TaskItem ("path1/Assembly.dll"),
					new TaskItem ("path2/Assembly.dll")
				}
			}
		};

		[Theory]
		[MemberData (nameof (AssemblyPathsCases))]
		public void TestAssemblyPaths (ITaskItem [] assemblyPaths)
		{
			var task = new MockTask () {
				AssemblyPaths = assemblyPaths
			};
			using (var driver = task.CreateDriver ()) {
				var context = driver.Context;

				var expectedReferences = assemblyPaths.Select (p => p.ItemSpec)
					.GroupBy (p => Path.GetFileNameWithoutExtension (p))
					.Select (g => g.First ());
				var actualReferences = driver.GetReferenceAssemblies ();
				Assert.Equal (expectedReferences.OrderBy (a => a), actualReferences.OrderBy (a => a));

				foreach (var item in assemblyPaths) {
					var assemblyPath = item.ItemSpec;
					var action = item.GetMetadata ("action");
					if (String.IsNullOrEmpty (action))
						continue;
					AssemblyAction expectedAction = (AssemblyAction)Enum.Parse (typeof(AssemblyAction), action, ignoreCase: true);
					AssemblyAction actualAction = (AssemblyAction) context.Actions [Path.GetFileNameWithoutExtension (assemblyPath)];
					Assert.Equal (expectedAction, actualAction);
				}
			}
		}

		[Fact]
		public void TestAssemblyPathsWithInvalidAction ()
		{
			var task = new MockTask () {
				AssemblyPaths = new ITaskItem [] { new TaskItem ("Assembly.dll", new Dictionary<string, string> { { "action", "invalid" } }) }
			};
			Assert.Throws <ArgumentException> (() => task.CreateDriver ());
		}

		// the InlineData string [] parameters are wrapped in object [] as described in https://github.com/xunit/xunit/issues/2060

		[Theory]
		[InlineData (new object [] { new string [] { "path/to/Assembly.dll" } })]
		[InlineData (new object [] { new string [] { "path with/spaces/Assembly.dll" } })]
		[InlineData (new object [] { new string [] { "path/to/Assembly With Spaces.dll" } })]
		public void TestReferenceAssemblyPaths (string [] referenceAssemblyPaths)
		{
			var task = new MockTask () {
				ReferenceAssemblyPaths = referenceAssemblyPaths.Select (p => new TaskItem (p)).ToArray ()
			};
			using (var driver = task.CreateDriver ()) {
				var expectedReferences = referenceAssemblyPaths;
				var actualReferences = driver.GetReferenceAssemblies ();
				Assert.Equal (expectedReferences.OrderBy (a => a), actualReferences.OrderBy (a => a));
				foreach (var reference in expectedReferences) {
					var referenceName = Path.GetFileNameWithoutExtension (reference);
					var actualAction = driver.Context.Actions [referenceName];
					Assert.Equal (AssemblyAction.Skip, actualAction);
				}
			}
		}

		[Theory]
		[InlineData (new object [] { new string [] { "AssemblyName" } })]
		public void TestRootAssemblyNames (string [] rootAssemblyNames)
		{
			var task = new MockTask () {
				RootAssemblyNames = rootAssemblyNames.Select (a => new TaskItem (a)).ToArray ()
			};
			using (var driver = task.CreateDriver ()) {
				var expectedRoots = rootAssemblyNames;
				var actualRoots = driver.GetRootAssemblies ();
				Assert.Equal (rootAssemblyNames.OrderBy (r => r), actualRoots.OrderBy (r => r));
			}
		}

		[Theory]
		[InlineData ("path/to/directory")]
		[InlineData ("path with/spaces")]
		public void TestOutputDirectory (string outputDirectory)
		{
			var task = new MockTask () {
				OutputDirectory = new TaskItem (outputDirectory)
			};
			using (var driver = task.CreateDriver ()) {
				var actualOutputDirectory = driver.Context.OutputDirectory;
				Assert.Equal (outputDirectory, actualOutputDirectory);
			}
		}

		[Theory]
		[InlineData (new object [] { new string [] { "path/to/descriptor.xml" } })]
		[InlineData (new object [] { new string [] { "path with/spaces/descriptor.xml" } })]
		[InlineData (new object [] { new string [] { "descriptor with spaces.xml" } })]
		[InlineData (new object [] { new string [] { "descriptor1.xml", "descriptor2.xml" } })]
		public void TestRootDescriptorFiles (string [] rootDescriptorFiles)
		{
			var task = new MockTask () {
				RootDescriptorFiles = rootDescriptorFiles.Select (f => new TaskItem (f)).ToArray ()
			};
			using (var driver = task.CreateDriver ()) {
				var actualDescriptors = driver.GetRootDescriptors ();
				Assert.Equal (rootDescriptorFiles.OrderBy (f => f), actualDescriptors.OrderBy (f => f));
			}
		}

		public static IEnumerable<object []> OptimizationsCases () {
			foreach (var optimization in MockTask.OptimizationNames) {
				yield return new object [] { optimization, true };
				yield return new object [] { optimization, false };
			}
		}

		[Theory]
		[MemberData (nameof (OptimizationsCases))]
		public void TestGlobalOptimizations (string optimization, bool enabled)
		{
			var task = new MockTask ();
			task.SetOptimization (optimization, enabled);
			// get the corresponding CodeOptimizations value
			Assert.True (MockDriver.GetOptimizationName (optimization, out CodeOptimizations codeOptimizations));
			using (var driver = task.CreateDriver ()) {
				var actualValue = driver.Context.Optimizations.IsEnabled (codeOptimizations, assemblyName: null);
				Assert.Equal (enabled, actualValue);
			}
		}

		public static IEnumerable<object []> PerAssemblyOptimizationsCases () {
			// test that we can individually enable/disable each optimization
			foreach (var optimization in MockTask.OptimizationNames) {
				yield return new object [] {
					new ITaskItem [] {
						new TaskItem ("path/to/Assembly.dll", new Dictionary<string, string> {
							{ optimization, "True" }
						})
					}
				};
				yield return new object [] {
					new ITaskItem [] {
						new TaskItem ("path/to/Assembly.dll", new Dictionary<string, string> {
							{ optimization, "False" }
						})
					}
				};
			}
			// complex case with multiple optimizations, assemblies
			yield return new object [] {
				new ITaskItem [] {
					new TaskItem ("path/to/Assembly1.dll", new Dictionary<string, string> {
						{ "ClearInitLocals", "True" },
						{ "BeforeFieldInit", "False" }
					}),
					new TaskItem ("path/to/Assembly2.dll", new Dictionary<string, string> {
						{ "ClearInitLocals", "False" },
						{ "Sealer", "True" }
					})
				}
			};
		}

		[Theory]
		[MemberData (nameof (PerAssemblyOptimizationsCases))]
		public void TestPerAssemblyOptimizations (ITaskItem [] assemblyPaths)
		{
			var task = new MockTask () {
				AssemblyPaths = assemblyPaths
			};
			using (var driver = task.CreateDriver ()) {
				foreach (var item in assemblyPaths) {
					var assemblyName = Path.GetFileNameWithoutExtension (item.ItemSpec);
					foreach (var optimization in MockTask.OptimizationNames) {
						Assert.True (MockDriver.GetOptimizationName (optimization, out CodeOptimizations codeOptimizations));
						var optimizationValue = item.GetMetadata (optimization);
						if (String.IsNullOrEmpty (optimizationValue))
							continue;
						var enabled = Boolean.Parse (optimizationValue);
						var actualValue = driver.Context.Optimizations.IsEnabled (codeOptimizations, assemblyName: assemblyName);
						Assert.Equal (enabled, actualValue);
					}
				}
			}
		}

		[Fact]
		public void TestInvalidPerAssemblyOptimizations ()
		{
			var task = new MockTask () {
				AssemblyPaths = new ITaskItem [] {
					new TaskItem ("path/to/Assembly.dll", new Dictionary<string, string> {
						{ "ClearInitLocals", "invalid" }
					})
				}
			};
			Assert.Throws<ArgumentException> (() => task.CreateDriver ());
		}

		[Fact]
		public void TestOptimizationsDefaults ()
		{
			var task = new MockTask ();
			using (var driver = task.CreateDriver ()) {
				var expectedOptimizations = driver.GetDefaultOptimizations ();
				var actualOptimizations = driver.Context.Optimizations.Global;
				Assert.Equal (expectedOptimizations, actualOptimizations);
			}
		}

		[Fact]
		public void CheckGlobalOptimizationsMatchPerAssemblyOptimizations ()
		{
			var task = new MockTask ();
			var optimizationMetadataNames = MockTask.OptimizationNames;
			var optimizationPropertyNames = MockTask.GetOptimizationPropertyNames ();
			Assert.Equal (optimizationMetadataNames.OrderBy (o => o), optimizationPropertyNames.OrderBy (o => o));
		}

		[Theory]
		[InlineData (true)]
		[InlineData (false)]
		public void TestClearInitLocals (bool clearInitLocals)
		{
			var task = new MockTask () {
				ClearInitLocals = clearInitLocals
			};
			using (var driver = task.CreateDriver ()) {
				var actualClearInitLocals = driver.Context.Optimizations.IsEnabled (CodeOptimizations.ClearInitLocals, assemblyName: null);
				Assert.Equal (clearInitLocals, actualClearInitLocals);
			}
		}

		[Theory]
		[InlineData ("Assembly1,Assembly2,Assembly3")]
		public void TestClearInitLocalsAssemblies (string assemblyNames)
		{
			var task = new MockTask () {
				ClearInitLocalsAssemblies = assemblyNames,
				// This task parameter ClearInitLocalsAssemblies currently only is applied if ClearInitLocals is also true,
				// which turns the optimization on globally. This is redundant behavior that we might want to fix.
				// For now, at least test the existing behavior.
				ClearInitLocals = true
			};
			using (var driver = task.CreateDriver ()) {
				var expectedAssemblies = assemblyNames.Split (',');
				Assert.True (driver.Context.TryGetCustomData ("ClearInitLocalsAssemblies", out var value));
				var actualAssemblies = value.Split (',');
				Assert.Equal (expectedAssemblies.OrderBy (a => a), actualAssemblies.OrderBy (a => a));
			}
		}

		[Fact]
		public void TestExtraArgs ()
		{
			var task = new MockTask () {
				DefaultAction = "copy",
				ExtraArgs = "-c link"
			};
			using (var driver = task.CreateDriver ()) {
				Assert.Equal (AssemblyAction.Copy, driver.Context.UserAction);
				// Check that ExtraArgs can override DefaultAction
				Assert.Equal (AssemblyAction.Link, driver.Context.CoreAction);
			}
		}

		[Theory]
		[InlineData (true)]
		[InlineData (false)]
		public void TestDumpDependencies (bool dumpDependencies)
		{
			var task = new MockTask () {
				DumpDependencies = dumpDependencies
			};
			using (var driver = task.CreateDriver ()) {
				Assert.Equal (dumpDependencies, driver.GetDependencyRecorders ()?.Single () == MockXmlDependencyRecorder.Singleton);
			}
		}		

		[Theory]
		[InlineData (true)]
		[InlineData (false)]
		public void TestLinkSymbols (bool linkSymbols)
		{
			var task = new MockTask () {
				LinkSymbols = linkSymbols
			};
			using (var driver = task.CreateDriver ()) {
				Assert.Equal (linkSymbols, driver.Context.LinkSymbols);
			}
		}

		[Theory]
		[InlineData ("copy")]
		[InlineData ("link")]
		[InlineData ("copyused")]
		public void TestDefaultAction (string defaultAction)
		{
			var task = new MockTask () {
				DefaultAction = defaultAction
			};
			using (var driver = task.CreateDriver ()) {
				var expectedAction = (AssemblyAction)Enum.Parse (typeof(AssemblyAction), defaultAction, ignoreCase: true);
				Assert.Equal (expectedAction, driver.Context.CoreAction);
				Assert.Equal (expectedAction, driver.Context.UserAction);
			}
		}

		[Fact]
		public void TestInvalidDefaultAction ()
		{
			var task = new MockTask () {
				DefaultAction = "invalid"
			};
			Assert.Throws <ArgumentException> (() => task.CreateDriver ());
		}

		public static IEnumerable<object []> CustomStepsCases => new List<object []> {
			new object [] {
				new ITaskItem [] {
					new TaskItem (Assembly.GetExecutingAssembly ().Location, new Dictionary<string, string> {
						{ "Type", "ILLink.Tasks.Tests.MockCustomStep" }
					})
				},
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem (Assembly.GetExecutingAssembly ().Location, new Dictionary<string, string> {
						{ "Type", "ILLink.Tasks.Tests.MockCustomStep" },
						{ "BeforeStep", "MarkStep" }
					})
				},
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem (Assembly.GetExecutingAssembly ().Location, new Dictionary<string, string> {
						{ "type", "ILLink.Tasks.Tests.MockCustomStep" },
						{ "beforebtep", "MarkStep" }
					})
				},
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem (Assembly.GetExecutingAssembly ().Location, new Dictionary<string, string> {
						{ "Type", "ILLink.Tasks.Tests.MockCustomStep" },
						{ "AfterStep", "MarkStep" }
					})
				},
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem (Assembly.GetExecutingAssembly ().Location, new Dictionary<string, string> {
						{ "Type", "ILLink.Tasks.Tests.MockCustomStep" },
						{ "BeforeStep", "MarkStep" }
					}),
					new TaskItem (Assembly.GetExecutingAssembly ().Location, new Dictionary<string, string> {
						{ "Type", "ILLink.Tasks.Tests.MockCustomStep" },
						{ "AfterStep", "MarkStep" }
					})
				},
			}
		};

		[Theory]
		[MemberData (nameof (CustomStepsCases))]
		public void TestCustomSteps (ITaskItem [] customSteps)
		{
			var task = new MockTask () {
				CustomSteps = customSteps
			};
			using (var driver = task.CreateDriver ()) {
				foreach (var customStep in customSteps) {
					var stepType = customStep.GetMetadata ("Type");
					var stepName = stepType.Substring (stepType.LastIndexOf (Type.Delimiter) + 1);
					var beforeStepName = customStep.GetMetadata ("BeforeStep");
					var afterStepName = customStep.GetMetadata ("AfterStep");
					Assert.True (String.IsNullOrEmpty (beforeStepName) || String.IsNullOrEmpty (afterStepName));

					var actualStepNames = driver.Context.Pipeline.GetSteps ().Select (s => s.GetType ().Name);
					if (!String.IsNullOrEmpty (beforeStepName)) {
						Assert.Contains (beforeStepName, actualStepNames);
						Assert.Equal (stepName, actualStepNames.TakeWhile (s => s != beforeStepName).Last ());
					} else if (!String.IsNullOrEmpty (afterStepName)) {
						Assert.Contains (afterStepName, actualStepNames);
						Assert.Equal (stepName, actualStepNames.SkipWhile (s => s != afterStepName).ElementAt (1));
					} else {
						Assert.Equal (stepName, actualStepNames.Last ());
					}
				}
			}
		}

		[Fact]
		public void TestCustomStepsWithBeforeAndAfterSteps ()
		{
			var customSteps = new ITaskItem [] {
				new TaskItem (Assembly.GetExecutingAssembly ().Location, new Dictionary<string, string> {
					{ "Type", "ILLink.Tasks.Tests.MockCustomStep" },
					{ "BeforeStep", "MarkStep" },
					{ "AfterStep", "MarkStep" }
				})
			};
			var task = new MockTask () {
				CustomSteps = customSteps
			};
			Assert.Throws <ArgumentException> (() => task.CreateDriver ());
		}

		[Fact]
		public void TestCustomStepsMissingType ()
		{
			var customSteps = new ITaskItem [] {
				new TaskItem (Assembly.GetExecutingAssembly ().Location)
			};
			var task = new MockTask () {
				CustomSteps = customSteps
			};
			Assert.Throws <ArgumentException> (() => task.CreateDriver ());
		}
	}
}
