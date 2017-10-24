﻿using System;
using System.Collections.Generic;
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
				Il8n = GetOptionAttributeValue (nameof (Il8nAttribute), string.Empty),
				IncludeBlacklistStep = GetOptionAttributeValue (nameof (IncludeBlacklistStepAttribute), false),
				KeepTypeForwarderOnlyAssemblies = GetOptionAttributeValue (nameof (KeepTypeForwarderOnlyAssembliesAttribute), string.Empty),
				CoreAssembliesAction = GetOptionAttributeValue<string> (nameof (SetupLinkerCoreActionAttribute), null)
			};

			foreach (var assemblyAction in _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (SetupLinkerActionAttribute)))
			{
				var ca = assemblyAction.ConstructorArguments;
				tclo.AssembliesAction.Add (new KeyValuePair<string, string> ((string)ca [0].Value, (string)ca [1].Value));
			}

			return tclo;
		}

		public virtual IEnumerable<string> GetReferencedAssemblies (NPath workingDirectory)
		{
			yield return workingDirectory.Combine ("Mono.Linker.Tests.Cases.Expectations.dll").ToString ();
			yield return "mscorlib.dll";

			foreach (var referenceAttr in _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (ReferenceAttribute))) {
				yield return (string) referenceAttr.ConstructorArguments.First ().Value;
			}
		}

		public virtual IEnumerable<SourceAndDestinationPair> GetResources ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (SetupCompileResourceAttribute))
				.Select (GetSourceAndRelativeDestinationValue);
		}

		public virtual IEnumerable<NPath> GetExtraLinkerSearchDirectories ()
		{
			yield break;
		}

		public bool IsIgnored (out string reason)
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
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (DefineAttribute))
				.Select (attr => (string) attr.ConstructorArguments.First ().Value);
		}

		public virtual string GetAssemblyName ()
		{
			return GetOptionAttributeValue (nameof (SetupCompileAssemblyNameAttribute), "test.exe");
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
			var relativeSource = (string) attribute.ConstructorArguments.First ().Value;
			var destinationFileName = (string) attribute.ConstructorArguments [1].Value;
			var fullSource = _testCase.SourceFile.Parent.Combine (relativeSource);
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
				SourceFiles = ((CustomAttributeArgument []) ctorArguments [1].Value).Select (arg => _testCase.SourceFile.Parent.Combine (arg.Value.ToString ())).ToArray (),
				References = ((CustomAttributeArgument []) ctorArguments [2].Value)?.Select (arg => arg.Value.ToString ()).ToArray (),
				Defines = ((CustomAttributeArgument []) ctorArguments [3].Value)?.Select (arg => arg.Value.ToString ()).ToArray (),
				AddAsReference = ctorArguments.Count >= 5 ? (bool) ctorArguments [4].Value : true
			};
		}
	}
}