// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TestCaseCompilationMetadataProvider : BaseMetadataProvider
	{

		public TestCaseCompilationMetadataProvider (TestCase testCase, AssemblyDefinition fullTestCaseAssemblyDefinition)
			: base (testCase, fullTestCaseAssemblyDefinition)
		{

		}

		public virtual TestRunCharacteristics Characteristics =>
			TestRunCharacteristics.TargetingNetCore | TestRunCharacteristics.SupportsDefaultInterfaceMethods | TestRunCharacteristics.SupportsStaticInterfaceMethods;

		private static bool IsIgnoredByTrimmer (CustomAttribute attr)
		{
			var ignoredBy = attr.GetPropertyValue ("IgnoredBy");
			return ignoredBy is null ? true : ((Tool) ignoredBy).HasFlag (Tool.Trimmer);
		}

		public virtual bool IsIgnored (out string reason)
		{
			var ignoreAttribute = _testCaseTypeDefinition.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.Name == nameof (IgnoreTestCaseAttribute));
			if (ignoreAttribute != null && IsIgnoredByTrimmer (ignoreAttribute)) {
				if (ignoreAttribute.ConstructorArguments.Count == 1) {
					reason = (string) ignoreAttribute.ConstructorArguments.First ().Value;
					return true;
				} else {
					throw new ArgumentException ($"Unhandled {nameof (IgnoreTestCaseAttribute)} constructor with {ignoreAttribute.ConstructorArguments} arguments");
				}
			}

			var requirementsAttribute = _testCaseTypeDefinition.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.Name == nameof (TestCaseRequirementsAttribute));
			if (requirementsAttribute != null) {
				if (requirementsAttribute.ConstructorArguments.Count == 2) {
					var testCaseRequirements = (TestRunCharacteristics) requirementsAttribute.ConstructorArguments[0].Value;

					foreach (var value in Enum.GetValues (typeof (TestRunCharacteristics))) {
						if (IsRequirementMissing ((TestRunCharacteristics) value, testCaseRequirements)) {
							reason = (string) requirementsAttribute.ConstructorArguments[1].Value;
							return true;
						}
					}
				} else {
					throw new ArgumentException ($"Unhandled {nameof (TestCaseRequirementsAttribute)} constructor with {requirementsAttribute.ConstructorArguments} arguments");
				}
			}

			reason = null;
			return false;
		}

		bool IsRequirementMissing (TestRunCharacteristics requirement, TestRunCharacteristics testCaseRequirements)
		{
			return testCaseRequirements.HasFlag (requirement) && !Characteristics.HasFlag (requirement);
		}

		public virtual IEnumerable<string> GetDefines ()
		{
			// There are a few tests related to native pdbs where the assertions are different between windows and non-windows
			// To enable test cases to define different expected behavior we set this special define
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				yield return "WIN32";

			if (Characteristics.HasFlag (TestRunCharacteristics.TargetingNetCore))
				yield return "NETCOREAPP";

			if (Characteristics.HasFlag (TestRunCharacteristics.SupportsDefaultInterfaceMethods))
				yield return "SUPPORTS_DEFAULT_INTERFACE_METHODS";

			foreach (var attr in _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (DefineAttribute)))
				yield return (string) attr.ConstructorArguments.First ().Value;
		}

		public virtual string GetAssemblyName ()
		{
			var asLibraryAttribute = _testCaseTypeDefinition.CustomAttributes
				.FirstOrDefault (attr => attr.AttributeType.Name == nameof (SetupCompileAsLibraryAttribute));
			var defaultName = asLibraryAttribute == null ? "test.exe" : "test.dll";
			return GetOptionAttributeValue (nameof (SetupCompileAssemblyNameAttribute), defaultName);
		}

		public virtual string GetCSharpCompilerToUse ()
		{
			return GetOptionAttributeValue (nameof (SetupCSharpCompilerToUseAttribute), string.Empty).ToLower ();
		}

		public virtual IEnumerable<string> GetSetupCompilerArguments ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (SetupCompileArgumentAttribute))
				.Select (attr => (string) attr.ConstructorArguments.First ().Value);
		}

		public virtual IEnumerable<SourceAndDestinationPair> AdditionalFilesToSandbox ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (SandboxDependencyAttribute))
				.Select (GetSourceAndRelativeDestinationValue);
		}

		static string GetReferenceDir ()
		{
			string runtimeDir = Path.GetDirectoryName (typeof (object).Assembly.Location);
			string ncaVersion = Path.GetFileName (runtimeDir);
			var dotnetDir = Path.GetDirectoryName (Path.GetDirectoryName (Path.GetDirectoryName (runtimeDir)));
			string candidatePath = Path.Combine (dotnetDir, "packs", "Microsoft.NETCore.App.Ref", ncaVersion, "ref", PathUtilities.TFMDirectoryName);
			if (Directory.Exists (candidatePath))
				return candidatePath;

			// There's no rule that runtime version must match the reference pack version exactly. So in this case use the major.minor only
			// and find the highest available patch version (since the runtime should also be the highest available patch version in that range)
			string ncaVersionWithoutPatch = ncaVersion.Substring (0, ncaVersion.LastIndexOf ('.'));
			candidatePath = null;
			foreach (var dir in Directory.GetDirectories (Path.Combine (dotnetDir, "Packs", "Microsoft.NETCore.App.Ref"), ncaVersionWithoutPatch + ".*")) {
				if (candidatePath == null || StringComparer.Ordinal.Compare (dir, candidatePath) > 0)
					candidatePath = dir;
			}

			if (candidatePath == null)
				throw new InvalidOperationException ($"Could not determine ref pack path. Based on runtime directory {runtimeDir}.");

			candidatePath = Path.Combine (candidatePath, "ref", PathUtilities.TFMDirectoryName);
			if (Directory.Exists (candidatePath))
				return candidatePath;

			throw new InvalidOperationException ($"Could not determine ref pack path. Computed path {candidatePath} doesn't exist.");
		}

		public virtual IEnumerable<string> GetCommonReferencedAssemblies (NPath workingDirectory)
		{
			yield return workingDirectory.Combine ("Mono.Linker.Tests.Cases.Expectations.dll").ToString ();
			if (Characteristics.HasFlag (TestRunCharacteristics.TargetingNetCore)) {
				string referenceDir = GetReferenceDir ();

				yield return Path.Combine (referenceDir, "mscorlib.dll");
				yield return Path.Combine (referenceDir, "System.Collections.dll");
				yield return Path.Combine (referenceDir, "System.Collections.Immutable.dll");
				yield return Path.Combine (referenceDir, "System.ComponentModel.TypeConverter.dll");
				yield return Path.Combine (referenceDir, "System.Console.dll");
				yield return Path.Combine (referenceDir, "System.Linq.Expressions.dll");
				yield return Path.Combine (referenceDir, "System.Memory.dll");
				yield return Path.Combine (referenceDir, "System.ObjectModel.dll");
				yield return Path.Combine (referenceDir, "System.Runtime.dll");
				yield return Path.Combine (referenceDir, "System.Runtime.Extensions.dll");
				yield return Path.Combine (referenceDir, "System.Runtime.InteropServices.dll");
				yield return Path.Combine (referenceDir, "System.Threading.dll");
			} else {
				yield return "mscorlib.dll";
			}
		}

		public virtual IEnumerable<string> GetReferencedAssemblies (NPath workingDirectory)
		{
			foreach (var fileName in GetReferenceValues ()) {

				if (fileName.StartsWith ("System.", StringComparison.Ordinal) || fileName.StartsWith ("Mono.", StringComparison.Ordinal) || fileName.StartsWith ("Microsoft.", StringComparison.Ordinal)) {
					if (Characteristics.HasFlag (TestRunCharacteristics.TargetingNetCore)) {
						var referenceDir = GetReferenceDir ();
						var filePath = Path.Combine (referenceDir, fileName);

						if (File.Exists (filePath)) {
							yield return filePath;
						} else {
							yield return fileName;
						}
					} else {
						yield return fileName;
					}
				} else {
					// Drop any relative path information.  Sandboxing will have taken care of copying the reference to the directory
					yield return workingDirectory.Combine (Path.GetFileName (fileName));
				}
			}
		}

		public virtual IEnumerable<string> GetReferenceDependencies ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (ReferenceDependencyAttribute))
				.Select (attr => (string) attr.ConstructorArguments[0].Value);
		}

		public virtual IEnumerable<string> GetReferenceValues ()
		{
			foreach (var referenceAttr in _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (ReferenceAttribute)))
				yield return (string) referenceAttr.ConstructorArguments.First ().Value;
		}

		public virtual IEnumerable<SourceAndDestinationPair> GetResources ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (SetupCompileResourceAttribute))
				.Select (GetSourceAndRelativeDestinationValue);
		}

		public virtual IEnumerable<SetupCompileInfo> GetSetupCompileAssembliesBefore ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (SetupCompileBeforeAttribute))
				.Select (CreateSetupCompileAssemblyInfo);
		}

		public virtual IEnumerable<SetupCompileInfo> GetSetupCompileAssembliesAfter ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (SetupCompileAfterAttribute))
				.Select (CreateSetupCompileAssemblyInfo);
		}

		private SetupCompileInfo CreateSetupCompileAssemblyInfo (CustomAttribute attribute)
		{
			var ctorArguments = attribute.ConstructorArguments;
			return new SetupCompileInfo {
				OutputName = (string) ctorArguments[0].Value,
				SourceFiles = SourceFilesForAttributeArgument (ctorArguments[1]),
				References = ((CustomAttributeArgument[]) ctorArguments[2].Value)?.Select (arg => arg.Value.ToString ()).ToArray (),
				Defines = ((CustomAttributeArgument[]) ctorArguments[3].Value)?.Select (arg => arg.Value.ToString ()).ToArray (),
				Resources = ResourcesForAttributeArgument (ctorArguments[4]),
				AdditionalArguments = ((CustomAttributeArgument[]) ctorArguments[5].Value)?.Select (arg => arg.Value.ToString ()).ToArray (),
				CompilerToUse = (string) ctorArguments[6].Value,
				AddAsReference = ctorArguments.Count >= 8 ? (bool) ctorArguments[7].Value : true,
				RemoveFromLinkerInput = ctorArguments.Count >= 9 ? (bool) ctorArguments[8].Value : false,
				OutputSubFolder = ctorArguments.Count >= 10 ? (string) ctorArguments[9].Value : null
			};
		}

		protected NPath[] SourceFilesForAttributeArgument (CustomAttributeArgument attributeArgument)
		{
			return ((CustomAttributeArgument[]) attributeArgument.Value)
				.Select (attributeArg => SourceFileForAttributeArgumentValue (attributeArg.Value))
				.Distinct ()
				.ToArray ();
		}

		protected SourceAndDestinationPair[] ResourcesForAttributeArgument (CustomAttributeArgument attributeArgument)
		{
			return ((CustomAttributeArgument[]) attributeArgument.Value)
				?.Select (arg => {
					var referenceArg = (CustomAttributeArgument) arg.Value;
					if (referenceArg.Value is string source) {
						var fullSource = MakeSourceTreeFilePathAbsolute (source);
						return new SourceAndDestinationPair {
							Source = fullSource,
							DestinationFileName = fullSource.FileName
						};
					}
					var sourceAndDestination = (CustomAttributeArgument[]) referenceArg.Value;
					return new SourceAndDestinationPair {
						Source = MakeSourceTreeFilePathAbsolute (sourceAndDestination[0].Value.ToString ()),
						DestinationFileName = sourceAndDestination[1].Value.ToString ()
					};
				})
				?.ToArray ();
		}
	}
}
