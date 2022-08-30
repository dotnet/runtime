// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;
using System;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TestCaseMetadataProvider : BaseMetadataProvider
	{
		public TestCaseMetadataProvider (TestCase testCase, AssemblyDefinition fullTestCaseAssemblyDefinition)
			: base (testCase, fullTestCaseAssemblyDefinition)
		{
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
				tclo.AssembliesAction.Add (((string) ca[0].Value, (string) ca[1].Value));
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
				var values = ((CustomAttributeArgument[]) ca[1].Value)!.Select (arg => arg.Value.ToString ()!).ToArray ();
				// Since custom attribute arguments need to be constant expressions, we need to add
				// the path to the temp directory (where the custom assembly is located) here.
				switch ((string) ca[0].Value) {
				case "--custom-step":
					int pos = values[0].IndexOf (",");
					if (pos != -1) {
						string custom_assembly_path = values[0].Substring (pos + 1);
						if (!Path.IsPathRooted (custom_assembly_path))
							values[0] = string.Concat (values[0].AsSpan (0, pos + 1), Path.Combine (inputPath, custom_assembly_path));
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
			var netcoreappDir = Path.GetDirectoryName (typeof (object).Assembly.Location)!;
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

		public virtual bool LinkPublicAndFamily ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.FirstOrDefault (attr => attr.AttributeType.Name == nameof (SetupLinkerLinkPublicAndFamilyAttribute)) != null;
		}
	}
}
