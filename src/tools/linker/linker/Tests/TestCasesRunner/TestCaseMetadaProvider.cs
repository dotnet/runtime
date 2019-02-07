﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class TestCaseMetadaProvider {
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

		public virtual TestCaseLinkerOptions GetLinkerOptions ()
		{
			var tclo = new TestCaseLinkerOptions {
				Il8n = GetOptionAttributeValue (nameof (Il8nAttribute), "none"),
				IncludeBlacklistStep = GetOptionAttributeValue (nameof (IncludeBlacklistStepAttribute), false),
				KeepTypeForwarderOnlyAssemblies = GetOptionAttributeValue (nameof (KeepTypeForwarderOnlyAssembliesAttribute), string.Empty),
				KeepDebugMembers = GetOptionAttributeValue (nameof (SetupLinkerKeepDebugMembersAttribute), string.Empty),
				LinkSymbols = GetOptionAttributeValue (nameof (SetupLinkerLinkSymbolsAttribute), string.Empty),
				CoreAssembliesAction = GetOptionAttributeValue<string> (nameof (SetupLinkerCoreActionAttribute), null),
				UserAssembliesAction = GetOptionAttributeValue<string> (nameof (SetupLinkerUserActionAttribute), null),
				SkipUnresolved = GetOptionAttributeValue (nameof (SkipUnresolvedAttribute), false),
				StripResources = GetOptionAttributeValue (nameof (StripResourcesAttribute), true)
			};

			foreach (var assemblyAction in _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (SetupLinkerActionAttribute)))
			{
				var ca = assemblyAction.ConstructorArguments;
				tclo.AssembliesAction.Add (new KeyValuePair<string, string> ((string)ca [0].Value, (string)ca [1].Value));
			}

			foreach (var additionalArgumentAttr in _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (SetupLinkerArgumentAttribute)))
			{
				var ca = additionalArgumentAttr.ConstructorArguments;
				var values = ((CustomAttributeArgument [])ca [1].Value)?.Select (arg => arg.Value.ToString ()).ToArray ();
				tclo.AdditionalArguments.Add (new KeyValuePair<string, string []> ((string)ca [0].Value, values));
			}

			return tclo;
		}

		public virtual IEnumerable<string> GetCommonReferencedAssemblies (NPath workingDirectory)
		{
			yield return workingDirectory.Combine ("Mono.Linker.Tests.Cases.Expectations.dll").ToString ();
			yield return "mscorlib.dll";
		}

		public virtual IEnumerable<string> GetReferencedAssemblies (NPath workingDirectory)
		{
			foreach (var fileName in GetReferenceValues ()) {
				if (fileName.StartsWith ("System.", StringComparison.Ordinal) || fileName.StartsWith ("Mono.", StringComparison.Ordinal) || fileName.StartsWith ("Microsoft.", StringComparison.Ordinal))
					yield return fileName;
				else
					// Drop any relative path information.  Sandboxing will have taken care of copying the reference to the directory
					yield return workingDirectory.Combine (Path.GetFileName (fileName));
			}
		}
		
		public virtual IEnumerable<string> GetReferenceDependencies ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (ReferenceDependencyAttribute))
				.Select (attr => (string) attr.ConstructorArguments [0].Value);
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

		public virtual IEnumerable<NPath> GetExtraLinkerSearchDirectories ()
		{
			yield break;
		}

		public virtual bool IsIgnored (out string reason)
		{
			var ignoreAttribute = _testCaseTypeDefinition.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.Name == nameof (IgnoreTestCaseAttribute));
			if (ignoreAttribute != null) {
				reason = (string)ignoreAttribute.ConstructorArguments.First ().Value;
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

			foreach (var attr in  _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (DefineAttribute)))
				yield return (string) attr.ConstructorArguments.First ().Value;
		}

		public virtual bool LinkPublicAndFamily()
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
			var destinationFileName = (string) attribute.ConstructorArguments [1].Value;
			return new SourceAndDestinationPair
			{
				Source = fullSource,
				DestinationFileName = string.IsNullOrEmpty (destinationFileName) ? fullSource.FileName : destinationFileName
			};
		}

		private SetupCompileInfo CreateSetupCompileAssemblyInfo (CustomAttribute attribute)
		{
			var ctorArguments = attribute.ConstructorArguments;
			return new SetupCompileInfo
			{
				OutputName = (string) ctorArguments [0].Value,
				SourceFiles = SourceFilesForAttributeArgument (ctorArguments [1]), 
				References = ((CustomAttributeArgument []) ctorArguments [2].Value)?.Select (arg => arg.Value.ToString ()).ToArray (),
				Defines = ((CustomAttributeArgument []) ctorArguments [3].Value)?.Select (arg => arg.Value.ToString ()).ToArray (),
				Resources = ((CustomAttributeArgument []) ctorArguments [4].Value)?.Select (arg => MakeSourceTreeFilePathAbsolute (arg.Value.ToString ())).ToArray (),
				AdditionalArguments = (string) ctorArguments [5].Value,
				CompilerToUse = (string) ctorArguments [6].Value,
				AddAsReference = ctorArguments.Count >= 8 ? (bool) ctorArguments [7].Value : true
			};
		}

		protected NPath MakeSourceTreeFilePathAbsolute (string value)
		{
			return _testCase.SourceFile.Parent.Combine (value);
		}

		protected NPath[] SourceFilesForAttributeArgument (CustomAttributeArgument attributeArgument)
		{
			return ((CustomAttributeArgument []) attributeArgument.Value)
				.Select (attributeArg => SourceFileForAttributeArgumentValue (attributeArg.Value))
				.Distinct ()
				.ToArray ();
		}

		protected virtual NPath SourceFileForAttributeArgumentValue (object value)
		{
			var valueAsTypeRef = value as TypeReference;
			if (valueAsTypeRef != null) {
				// Use the parent type for locating the source file
				var parentType = ParentMostType (valueAsTypeRef);
				var pathRelativeToAssembly = $"{parentType.FullName.Substring (parentType.Module.Name.Length - 3).Replace ('.', '/')}.cs".ToNPath ();
				var pathElements = pathRelativeToAssembly.Elements.ToArray ();
				var topMostDirectoryName = pathElements [0];
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