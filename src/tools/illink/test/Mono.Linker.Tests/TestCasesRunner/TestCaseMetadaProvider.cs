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
	public class TestCaseMetadaProvider
	{
		protected readonly TestCase _testCase;
		protected readonly AssemblyDefinition _fullTestCaseAssemblyDefinition;
		protected readonly TypeDefinition _testCaseTypeDefinition;

		public TestCaseMetadaProvider (TestCase testCase, AssemblyDefinition fullTestCaseAssemblyDefinition)
		{
			_testCase = testCase;
			_fullTestCaseAssemblyDefinition = fullTestCaseAssemblyDefinition;
			// The test case types are never nested so we don't need to worry about that
			_testCaseTypeDefinition = fullTestCaseAssemblyDefinition.MainModule.GetType (_testCase.ReconstructedFullTypeName);

			if (_testCaseTypeDefinition == null)
				throw new InvalidOperationException ($"Could not find the type definition for {_testCase.Name} in {_testCase.SourceFile}");
		}

		public virtual TestCaseLinkerOptions GetLinkerOptions (NPath inputPath)
		{
			var tclo = new TestCaseLinkerOptions {
				Il8n = GetOptionAttributeValue (nameof (Il8nAttribute), "none"),
				IgnoreDescriptors = GetOptionAttributeValue (nameof (IgnoreDescriptorsAttribute), true),
				IgnoreSubstitutions = GetOptionAttributeValue (nameof (IgnoreSubstitutionsAttribute), true),
				IgnoreLinkAttributes = GetOptionAttributeValue (nameof (IgnoreLinkAttributesAttribute), true),
				KeepTypeForwarderOnlyAssemblies = GetOptionAttributeValue (nameof (KeepTypeForwarderOnlyAssembliesAttribute), string.Empty),
				KeepDebugMembers = GetOptionAttributeValue (nameof (SetupLinkerKeepDebugMembersAttribute), string.Empty),
				LinkSymbols = GetOptionAttributeValue (nameof (SetupLinkerLinkSymbolsAttribute), string.Empty),
				TrimMode = GetOptionAttributeValue<string> (nameof (SetupLinkerTrimModeAttribute), null),
				DefaultAssembliesAction = GetOptionAttributeValue<string> (nameof (SetupLinkerDefaultActionAttribute), null),
				SkipUnresolved = GetOptionAttributeValue (nameof (SkipUnresolvedAttribute), false),
				StripDescriptors = GetOptionAttributeValue (nameof (StripDescriptorsAttribute), true),
				StripSubstitutions = GetOptionAttributeValue (nameof (StripSubstitutionsAttribute), true),
				StripLinkAttributes = GetOptionAttributeValue (nameof (StripLinkAttributesAttribute), true),
			};

			foreach (var assemblyAction in _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (SetupLinkerActionAttribute))) {
				var ca = assemblyAction.ConstructorArguments;
				tclo.AssembliesAction.Add (new KeyValuePair<string, string> ((string) ca[0].Value, (string) ca[1].Value));
			}

			foreach (var descFile in _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (SetupLinkerDescriptorFile))) {
				var ca = descFile.ConstructorArguments;
				var file = (string) ca[0].Value;
				tclo.Descriptors.Add (Path.Combine (inputPath, file));
			}

			foreach (var subsFile in _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (SetupLinkerSubstitutionFileAttribute))) {
				var ca = subsFile.ConstructorArguments;
				var file = (string) ca[0].Value;
				tclo.Substitutions.Add (Path.Combine (inputPath, file));
			}

			foreach (var linkAttrFile in _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (SetupLinkAttributesFile))) {
				var ca = linkAttrFile.ConstructorArguments;
				var file = (string) ca[0].Value;
				tclo.LinkAttributes.Add (Path.Combine (inputPath, file));
			}

			foreach (var additionalArgumentAttr in _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (SetupLinkerArgumentAttribute))) {
				var ca = additionalArgumentAttr.ConstructorArguments;
				var values = ((CustomAttributeArgument[]) ca[1].Value)?.Select (arg => arg.Value.ToString ()).ToArray ();
				// Since custom attribute arguments need to be constant expressions, we need to add
				// the path to the temp directory (where the custom assembly is located) here.
				switch ((string) ca[0].Value) {
				case "--custom-step":
					int pos = values[0].IndexOf (",");
					if (pos != -1) {
						string custom_assembly_path = values[0].Substring (pos + 1);
						if (!Path.IsPathRooted (custom_assembly_path))
							values[0] = values[0].Substring (0, pos + 1) + Path.Combine (inputPath, custom_assembly_path);
					}
					break;
				case "-a":
					if (!Path.IsPathRooted (values[0]))
						values[0] = Path.Combine (inputPath, values[0]);

					break;
				}

				tclo.AdditionalArguments.Add (new KeyValuePair<string, string[]> ((string) ca[0].Value, values));
			}

			return tclo;
		}

		public virtual void CustomizeLinker (LinkerDriver linker, LinkerCustomizations customizations)
		{
			if (_testCaseTypeDefinition.CustomAttributes.Any (attr =>
				attr.AttributeType.Name == nameof (DependencyRecordedAttribute))) {
				customizations.DependencyRecorder = new TestDependencyRecorder ();
				customizations.CustomizeContext += context => {
					context.Tracer.AddRecorder (customizations.DependencyRecorder);
				};
			}

			if (ValidatesReflectionAccessPatterns (_testCaseTypeDefinition)) {
				customizations.ReflectionPatternRecorder = new TestReflectionPatternRecorder ();
				customizations.CustomizeContext += context => {
					customizations.ReflectionPatternRecorder.PreviousRecorder = context.ReflectionPatternRecorder;
					context.ReflectionPatternRecorder = customizations.ReflectionPatternRecorder;
					context.LogMessages = true;
				};
			}
		}

		bool ValidatesReflectionAccessPatterns (TypeDefinition testCaseTypeDefinition)
		{
			if (testCaseTypeDefinition.HasNestedTypes) {
				var nestedTypes = new Queue<TypeDefinition> (testCaseTypeDefinition.NestedTypes.ToList ());
				while (nestedTypes.Count > 0) {
					if (ValidatesReflectionAccessPatterns (nestedTypes.Dequeue ()))
						return true;
				}
			}

			if (testCaseTypeDefinition.CustomAttributes.Any (attr =>
					attr.AttributeType.Name == nameof (VerifyAllReflectionAccessPatternsAreValidatedAttribute))
				|| testCaseTypeDefinition.AllMembers ().Concat (testCaseTypeDefinition.AllDefinedTypes ()).Any (m => m.CustomAttributes.Any (attr =>
					  attr.AttributeType.Name == nameof (RecognizedReflectionAccessPatternAttribute) ||
					  attr.AttributeType.Name == nameof (UnrecognizedReflectionAccessPatternAttribute))))
				return true;

			return false;
		}

		public virtual IEnumerable<string> GetCommonReferencedAssemblies (NPath workingDirectory)
		{
			yield return workingDirectory.Combine ("Mono.Linker.Tests.Cases.Expectations.dll").ToString ();
#if NETCOREAPP
			string frameworkDir = Path.GetDirectoryName (typeof (object).Assembly.Location);

			yield return typeof (object).Assembly.Location;
			yield return Path.Combine (frameworkDir, "System.Runtime.dll");
			yield return Path.Combine (frameworkDir, "System.Linq.Expressions.dll");
			yield return Path.Combine (frameworkDir, "System.ComponentModel.TypeConverter.dll");
			yield return Path.Combine (frameworkDir, "System.Console.dll");
			yield return Path.Combine (frameworkDir, "mscorlib.dll");
			yield return Path.Combine (frameworkDir, "System.ObjectModel.dll");
			yield return Path.Combine (frameworkDir, "System.Runtime.Extensions.dll");
#else
			yield return "mscorlib.dll";
#endif
		}

		public virtual IEnumerable<string> GetReferencedAssemblies (NPath workingDirectory)
		{
			foreach (var fileName in GetReferenceValues ()) {

				if (fileName.StartsWith ("System.", StringComparison.Ordinal) || fileName.StartsWith ("Mono.", StringComparison.Ordinal) || fileName.StartsWith ("Microsoft.", StringComparison.Ordinal)) {
#if NETCOREAPP
					// Try to find the assembly alongside the host's framework dependencies
					var frameworkDir = Path.GetFullPath (Path.GetDirectoryName (typeof (object).Assembly.Location));
					var filePath = Path.Combine (frameworkDir, fileName);

					if (File.Exists (filePath)) {
						yield return filePath;
					} else {
						yield return fileName;
					}
#else
					yield return fileName;
#endif
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

		public virtual IEnumerable<SourceAndDestinationPair> GetResponseFiles ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (SetupLinkerResponseFileAttribute))
				.Select (GetSourceAndRelativeDestinationValue);
		}

		public virtual IEnumerable<SourceAndDestinationPair> GetDescriptorFiles ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (SetupLinkerDescriptorFile))
				.Select (GetSourceAndRelativeDestinationValue);
		}

		public virtual IEnumerable<SourceAndDestinationPair> GetSubstitutionFiles ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (SetupLinkerSubstitutionFileAttribute))
				.Select (GetSourceAndRelativeDestinationValue);
		}

		public virtual IEnumerable<SourceAndDestinationPair> GetLinkAttributesFiles ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (SetupLinkAttributesFile))
				.Select (GetSourceAndRelativeDestinationValue);
		}

		public virtual IEnumerable<NPath> GetExtraLinkerReferences ()
		{
			var netcoreappDir = Path.GetDirectoryName (typeof (object).Assembly.Location);
			foreach (var assembly in Directory.EnumerateFiles (netcoreappDir)) {
				if (Path.GetExtension (assembly) != ".dll")
					continue;
				var assemblyName = Path.GetFileNameWithoutExtension (assembly);
				if (assemblyName.Contains ("Native"))
					continue;
				if (assemblyName.StartsWith ("Microsoft") ||
					assemblyName.StartsWith ("System") ||
					assemblyName == "mscorlib" || assemblyName == "netstandard")
					yield return assembly.ToNPath ();
			}
		}

		public virtual bool IsIgnored (out string reason)
		{
			var ignoreAttribute = _testCaseTypeDefinition.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.Name == nameof (IgnoreTestCaseAttribute));
			if (ignoreAttribute != null) {
				reason = (string) ignoreAttribute.ConstructorArguments.First ().Value;
				return true;
			}

			reason = null;
			return false;
		}

		public virtual IEnumerable<SourceAndDestinationPair> AdditionalFilesToSandbox ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (SandboxDependencyAttribute))
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

		public virtual IEnumerable<string> GetDefines ()
		{
			// There are a few tests related to native pdbs where the assertions are different between windows and non-windows
			// To enable test cases to define different expected behavior we set this special define
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				yield return "WIN32";

#if NETCOREAPP
			yield return "NETCOREAPP";
#endif

			foreach (var attr in _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (DefineAttribute)))
				yield return (string) attr.ConstructorArguments.First ().Value;
		}

		public virtual bool LinkPublicAndFamily ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.FirstOrDefault (attr => attr.AttributeType.Name == nameof (SetupLinkerLinkPublicAndFamilyAttribute)) != null;
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

		T GetOptionAttributeValue<T> (string attributeName, T defaultValue)
		{
			var attribute = _testCaseTypeDefinition.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.Name == attributeName);
			if (attribute != null)
				return (T) attribute.ConstructorArguments.First ().Value;

			return defaultValue;
		}

		SourceAndDestinationPair GetSourceAndRelativeDestinationValue (CustomAttribute attribute)
		{
			var fullSource = SourceFileForAttributeArgumentValue (attribute.ConstructorArguments.First ().Value);
			var destinationFileName = (string) attribute.ConstructorArguments[1].Value;
			return new SourceAndDestinationPair {
				Source = fullSource,
				DestinationFileName = string.IsNullOrEmpty (destinationFileName) ? fullSource.FileName : destinationFileName
			};
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
				AdditionalArguments = (string) ctorArguments[5].Value,
				CompilerToUse = (string) ctorArguments[6].Value,
				AddAsReference = ctorArguments.Count >= 8 ? (bool) ctorArguments[7].Value : true,
				RemoveFromLinkerInput = ctorArguments.Count >= 9 ? (bool) ctorArguments[8].Value : false,
				OutputSubFolder = ctorArguments.Count >= 10 ? (string) ctorArguments[9].Value : null
			};
		}

		protected NPath MakeSourceTreeFilePathAbsolute (string value)
		{
			return _testCase.SourceFile.Parent.Combine (value);
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

		protected virtual NPath SourceFileForAttributeArgumentValue (object value)
		{
			if (value is TypeReference valueAsTypeRef) {
				// Use the parent type for locating the source file
				var parentType = ParentMostType (valueAsTypeRef);
				var pathRelativeToAssembly = $"{parentType.FullName.Substring (parentType.Module.Name.Length - 3).Replace ('.', '/')}.cs".ToNPath ();
				var pathElements = pathRelativeToAssembly.Elements.ToArray ();
				var topMostDirectoryName = pathElements[0];
				var topMostDirectory = _testCase.SourceFile.RecursiveParents.Reverse ().FirstOrDefault (d => !d.IsRoot && d.FileName == topMostDirectoryName);

				if (topMostDirectory == null) {
					// Before giving up, try and detect the naming scheme for tests that use a dot in the top level directory name.
					// Ex:
					// Attributes.Debugger
					// + 1 because the file name is one of the elements
					if (pathElements.Length >= 3) {
						topMostDirectoryName = $"{pathElements[0]}.{pathElements[1]}";
						topMostDirectory = _testCase.SourceFile.RecursiveParents.Reverse ().FirstOrDefault (d => !d.IsRoot && d.FileName == topMostDirectoryName);
						pathRelativeToAssembly = topMostDirectoryName.ToNPath ().Combine (pathElements.Skip (2).Aggregate (new NPath (string.Empty), (path, s) => path.Combine (s)));
					}

					if (topMostDirectory == null)
						throw new ArgumentException ($"Unable to locate the source file for type {valueAsTypeRef}.  Could not locate directory {topMostDirectoryName}.  Ensure the type name matches the file name.  And the namespace match the directory structure on disk");
				}

				var fullPath = topMostDirectory.Parent.Combine (pathRelativeToAssembly);

				if (!fullPath.Exists ())
					throw new ArgumentException ($"Unable to locate the source file for type {valueAsTypeRef}.  Expected {fullPath}.  Ensure the type name matches the file name.  And the namespace match the directory structure on disk");

				return fullPath;
			}

			return MakeSourceTreeFilePathAbsolute (value.ToString ());
		}

		static TypeReference ParentMostType (TypeReference type)
		{
			if (!type.IsNested)
				return type;

			return ParentMostType (type.DeclaringType);
		}
	}
}