// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCasesRunner.ILVerification;
using NUnit.Framework;
using WellKnownType = ILLink.Shared.TypeSystemProxy.WellKnownType;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class ResultChecker
	{
		readonly BaseAssemblyResolver _originalsResolver;
		readonly BaseAssemblyResolver _linkedResolver;
		readonly ReaderParameters _originalReaderParameters;
		readonly ReaderParameters _linkedReaderParameters;

		public ResultChecker ()
			: this (new TestCaseAssemblyResolver (), new TestCaseAssemblyResolver (),
				new ReaderParameters {
					SymbolReaderProvider = new DefaultSymbolReaderProvider (false)
				},
				new ReaderParameters {
					SymbolReaderProvider = new DefaultSymbolReaderProvider (false)
				})
		{
		}

		public ResultChecker (BaseAssemblyResolver originalsResolver, BaseAssemblyResolver linkedResolver,
			ReaderParameters originalReaderParameters, ReaderParameters linkedReaderParameters)
		{
			_originalsResolver = originalsResolver;
			_linkedResolver = linkedResolver;
			_originalReaderParameters = originalReaderParameters;
			_linkedReaderParameters = linkedReaderParameters;
		}

		protected static void ValidateTypeRefsHaveValidAssemblyRefs (AssemblyDefinition linked)
		{
			foreach (var typeRef in linked.MainModule.GetTypeReferences ()) {
				switch (typeRef.Scope) {
				case null:
					// There should be an ExportedType row for this typeref
					var exportedType = linked.MainModule.ExportedTypes.SingleOrDefault (et => et.FullName == typeRef.FullName);
					Assert.IsNotNull (exportedType, $"Type reference '{typeRef.FullName}' with null scope has no ExportedType row");
					// The exported type's Implementation must be an index into the File/ExportedType/AssemblyRef table
					switch (exportedType.Scope) {
					case AssemblyNameReference:
						// There should be an AssemblyRef row for this assembly
						var assemblyRef = linked.MainModule.AssemblyReferences.Single (ar => ar.Name == exportedType.Scope.Name);
						Assert.IsNotNull (assemblyRef, $"Exported type '{exportedType.FullName}' has a reference to assembly '{exportedType.Scope.Name}' which is not a reference of '{linked.FullName}'");
						break;
					default:
						throw new NotImplementedException ($"Unexpected scope type '{exportedType.Scope.GetType ()}' for exported type '{exportedType.FullName}'");
					}
					continue;
				case AssemblyNameReference:
				{
					// There should be an AssemblyRef row for this assembly
					var assemblyRef = linked.MainModule.AssemblyReferences.Single (ar => ar.Name == typeRef.Scope.Name);
					Assert.IsNotNull (assemblyRef, $"Type reference '{typeRef.FullName}' has a reference to assembly '{typeRef.Scope.Name}' which is not a reference of '{linked.FullName}'");
					continue;
				}
				default:
					throw new NotImplementedException ($"Unexpected scope type '{typeRef.Scope.GetType ()}' for type reference '{typeRef.FullName}'");
				}
			}
		}

		public virtual void Check (TrimmedTestCaseResult linkResult)
		{
			InitializeResolvers (linkResult);

			try {
				var original = ResolveOriginalsAssembly (linkResult.ExpectationsAssemblyPath.FileNameWithoutExtension);

				VerifyExitCode (linkResult, original);

				if (!HasAttribute (original, nameof (NoLinkedOutputAttribute))) {
					Assert.IsTrue (linkResult.OutputAssemblyPath.FileExists (), $"The linked output assembly was not found.  Expected at {linkResult.OutputAssemblyPath}");

					var linked = ResolveLinkedAssembly (linkResult.OutputAssemblyPath.FileNameWithoutExtension);

					InitialChecking (linkResult, original, linked);

					PerformOutputAssemblyChecks (original, linkResult.OutputAssemblyPath.Parent);
					PerformOutputSymbolChecks (original, linkResult.OutputAssemblyPath.Parent);

					if (!HasActiveSkipKeptItemsValidationAttribute(linkResult.TestCase.FindTypeDefinition (original))) {
						CreateAssemblyChecker (original, linked, linkResult).Verify ();
					}
				}

				VerifyLinkingOfOtherAssemblies (original);
				VerifyILOfOtherAssemblies (linkResult);
				AdditionalChecking (linkResult, original);
			} finally {
				_originalsResolver.Dispose ();
				_linkedResolver.Dispose ();
			}

			bool HasActiveSkipKeptItemsValidationAttribute (ICustomAttributeProvider provider)
			{
				if (TryGetCustomAttribute (provider, nameof (SkipKeptItemsValidationAttribute), out var attribute)) {
					object by = attribute.GetPropertyValue (nameof (SkipKeptItemsValidationAttribute.By));
					return by is null ? true : ((Tool) by).HasFlag (Tool.Trimmer);
				}

				return false;
			}
		}

		void VerifyILOfOtherAssemblies (TrimmedTestCaseResult linkResult)
		{
			foreach (var linkedAssemblyPath in linkResult.Sandbox.OutputDirectory.Files ("*.dll")) {
				if (linkedAssemblyPath == linkResult.OutputAssemblyPath)
					continue;

				var linked = ResolveLinkedAssembly (linkedAssemblyPath.FileNameWithoutExtension);
				ValidateTypeRefsHaveValidAssemblyRefs (linked);
			}
		}

		protected virtual ILChecker CreateILChecker () => new ();

		protected virtual AssemblyChecker CreateAssemblyChecker (AssemblyDefinition original, AssemblyDefinition linked, TrimmedTestCaseResult linkedTestCase)
		{
			return new AssemblyChecker (original, linked, linkedTestCase);
		}

		void InitializeResolvers (TrimmedTestCaseResult linkedResult)
		{
			_originalsResolver.AddSearchDirectory (linkedResult.ExpectationsAssemblyPath.Parent.ToString ());
			_linkedResolver.AddSearchDirectory (linkedResult.OutputAssemblyPath.Parent.ToString ());
		}

		protected AssemblyDefinition ResolveLinkedAssembly (string assemblyName)
		{
			var cleanAssemblyName = assemblyName;
			if (assemblyName.EndsWith (".exe") || assemblyName.EndsWith (".dll"))
				cleanAssemblyName = System.IO.Path.GetFileNameWithoutExtension (assemblyName);
			return _linkedResolver.Resolve (new AssemblyNameReference (cleanAssemblyName, null), _linkedReaderParameters);
		}

		protected AssemblyDefinition ResolveOriginalsAssembly (string assemblyName)
		{
			var cleanAssemblyName = assemblyName;
			if (assemblyName.EndsWith (".exe") || assemblyName.EndsWith (".dll"))
				cleanAssemblyName = Path.GetFileNameWithoutExtension (assemblyName);
			return _originalsResolver.Resolve (new AssemblyNameReference (cleanAssemblyName, null), _originalReaderParameters);
		}

		void PerformOutputAssemblyChecks (AssemblyDefinition original, NPath outputDirectory)
		{
			var assembliesToCheck = original.MainModule.Types.SelectMany (t => t.CustomAttributes).Where (attr => ExpectationsProvider.IsAssemblyAssertion (attr));
			var actionAssemblies = new HashSet<string> ();
			bool trimModeIsCopy = false;

			foreach (var assemblyAttr in assembliesToCheck) {
				var name = (string) assemblyAttr.ConstructorArguments.First ().Value;
				var expectedPath = outputDirectory.Combine (name);

				if (assemblyAttr.AttributeType.Name == nameof (RemovedAssemblyAttribute))
					Assert.IsFalse (expectedPath.FileExists (), $"Expected the assembly {name} to not exist in {outputDirectory}, but it did");
				else if (assemblyAttr.AttributeType.Name == nameof (KeptAssemblyAttribute))
					Assert.IsTrue (expectedPath.FileExists (), $"Expected the assembly {name} to exist in {outputDirectory}, but it did not");
				else if (assemblyAttr.AttributeType.Name == nameof (SetupLinkerActionAttribute)) {
					string assemblyName = (string) assemblyAttr.ConstructorArguments[1].Value;
					if ((string) assemblyAttr.ConstructorArguments[0].Value == "copy") {
						VerifyCopyAssemblyIsKeptUnmodified (outputDirectory, assemblyName + (assemblyName == "test" ? ".exe" : ".dll"));
					}

					actionAssemblies.Add (assemblyName);
				} else if (assemblyAttr.AttributeType.Name == nameof (SetupLinkerTrimModeAttribute)) {
					// We delay checking that everything was copied after processing all assemblies
					// with a specific action, since assembly action wins over trim mode.
					if ((string) assemblyAttr.ConstructorArguments[0].Value == "copy")
						trimModeIsCopy = true;
				} else
					throw new NotImplementedException ($"Unknown assembly assertion of type {assemblyAttr.AttributeType}");
			}

			if (trimModeIsCopy) {
				foreach (string assemblyName in Directory.GetFiles (Directory.GetParent (outputDirectory).ToString (), "input")) {
					var fileInfo = new FileInfo (assemblyName);
					if (fileInfo.Extension == ".dll" && !actionAssemblies.Contains (assemblyName))
						VerifyCopyAssemblyIsKeptUnmodified (outputDirectory, assemblyName + (assemblyName == "test" ? ".exe" : ".dll"));
				}
			}
		}

		void PerformOutputSymbolChecks (AssemblyDefinition original, NPath outputDirectory)
		{
			var symbolFilesToCheck = original.MainModule.Types.SelectMany (t => t.CustomAttributes).Where (ExpectationsProvider.IsSymbolAssertion);

			foreach (var symbolAttr in symbolFilesToCheck) {
				if (symbolAttr.AttributeType.Name == nameof (RemovedSymbolsAttribute))
					VerifyRemovedSymbols (symbolAttr, outputDirectory);
				else if (symbolAttr.AttributeType.Name == nameof (KeptSymbolsAttribute))
					VerifyKeptSymbols (symbolAttr);
				else
					throw new NotImplementedException ($"Unknown symbol file assertion of type {symbolAttr.AttributeType}");
			}
		}

		void VerifyExitCode (TrimmedTestCaseResult linkResult, AssemblyDefinition original)
		{
			if (TryGetCustomAttribute (original, nameof(ExpectNonZeroExitCodeAttribute), out var attr)) {
				var expectedExitCode = (int) attr.ConstructorArguments[0].Value;
				Assert.AreEqual (expectedExitCode, linkResult.ExitCode, $"Expected exit code {expectedExitCode} but got {linkResult.ExitCode}.  Output was:\n{FormatLinkerOutput()}");
			} else {
				if (linkResult.ExitCode != 0) {
					Assert.Fail($"Linker exited with an unexpected non-zero exit code of {linkResult.ExitCode} and output:\n{FormatLinkerOutput()}");
				}
			}

			string FormatLinkerOutput ()
			{
				var sb = new StringBuilder ();
				foreach (var message in linkResult.Logger.GetLoggedMessages ())
					sb.AppendLine (message.ToString ());
				return sb.ToString ();
			}
		}

		void VerifyKeptSymbols (CustomAttribute symbolsAttribute)
		{
			var assemblyName = (string) symbolsAttribute.ConstructorArguments[0].Value;
			var originalAssembly = ResolveOriginalsAssembly (assemblyName);
			var linkedAssembly = ResolveLinkedAssembly (assemblyName);

			if (linkedAssembly.MainModule.SymbolReader == null)
				Assert.Fail ($"Missing symbols for assembly `{linkedAssembly.MainModule.FileName}`");

			if (linkedAssembly.MainModule.SymbolReader.GetType () != originalAssembly.MainModule.SymbolReader.GetType ())
				Assert.Fail ($"Expected symbol provider of type `{originalAssembly.MainModule.SymbolReader}`, but was `{linkedAssembly.MainModule.SymbolReader}`");
		}

		void VerifyRemovedSymbols (CustomAttribute symbolsAttribute, NPath outputDirectory)
		{
			var assemblyName = (string) symbolsAttribute.ConstructorArguments[0].Value;
			try {
				var linkedAssembly = ResolveLinkedAssembly (assemblyName);

				if (linkedAssembly.MainModule.SymbolReader != null)
					Assert.Fail ($"Expected no symbols to be found for assembly `{linkedAssembly.MainModule.FileName}`, however, symbols were found of type {linkedAssembly.MainModule.SymbolReader}");
			} catch (AssemblyResolutionException) {
				// If we failed to resolve, then the entire assembly may be gone.
				// The assembly being gone confirms that embedded pdbs were removed, but technically, for the other symbol types, the symbol file could still exist on disk
				// let's check to make sure that it does not.
				var possibleSymbolFilePath = outputDirectory.Combine ($"{assemblyName}").ChangeExtension ("pdb");
				if (possibleSymbolFilePath.Exists ())
					Assert.Fail ($"Expected no symbols to be found for assembly `{assemblyName}`, however, a symbol file was found at {possibleSymbolFilePath}");

				possibleSymbolFilePath = outputDirectory.Combine ($"{assemblyName}.mdb");
				if (possibleSymbolFilePath.Exists ())
					Assert.Fail ($"Expected no symbols to be found for assembly `{assemblyName}`, however, a symbol file was found at {possibleSymbolFilePath}");
			}
		}

		protected virtual void AdditionalChecking (TrimmedTestCaseResult linkResult, AssemblyDefinition original)
		{
			bool checkRemainingErrors = !HasAttribute (linkResult.TestCase.FindTypeDefinition (original), nameof (SkipRemainingErrorsValidationAttribute));
			VerifyLoggedMessages (original, linkResult.Logger, checkRemainingErrors);
			VerifyRecordedDependencies (original, linkResult.Customizations.DependencyRecorder);
		}

		protected virtual void InitialChecking (TrimmedTestCaseResult linkResult, AssemblyDefinition original, AssemblyDefinition linked)
		{
			CreateILChecker ().Check(linkResult, original);
			ValidateTypeRefsHaveValidAssemblyRefs (linked);
		}

		void VerifyLinkingOfOtherAssemblies (AssemblyDefinition original)
		{
			var checks = BuildOtherAssemblyCheckTable (original);

			try {
				foreach (var assemblyName in checks.Keys) {
					var linkedAssembly = ResolveLinkedAssembly (assemblyName);
					foreach (var checkAttrInAssembly in checks[assemblyName]) {
						var attributeTypeName = checkAttrInAssembly.AttributeType.Name;

						switch (attributeTypeName) {
						case nameof (KeptAllTypesAndMembersInAssemblyAttribute):
							VerifyKeptAllTypesAndMembersInAssembly (linkedAssembly);
							continue;
						case nameof (KeptAttributeInAssemblyAttribute):
							VerifyKeptAttributeInAssembly (checkAttrInAssembly, linkedAssembly);
							continue;
						case nameof (RemovedAttributeInAssembly):
							VerifyRemovedAttributeInAssembly (checkAttrInAssembly, linkedAssembly);
							continue;
						default:
							break;
						}

						var expectedTypeName = checkAttrInAssembly.ConstructorArguments[1].Value.ToString ();
						TypeDefinition linkedType = linkedAssembly.MainModule.GetType (expectedTypeName);

						if (linkedType == null && linkedAssembly.MainModule.HasExportedTypes) {
							ExportedType exportedType = linkedAssembly.MainModule.ExportedTypes
									.FirstOrDefault (exported => exported.FullName == expectedTypeName);

							// Note that copied assemblies could have dangling references.
							if (exportedType != null && original.EntryPoint.DeclaringType.CustomAttributes.FirstOrDefault (
								ca => ca.AttributeType.Name == nameof (RemovedAssemblyAttribute)
								&& ca.ConstructorArguments[0].Value.ToString () == exportedType.Scope.Name + ".dll") != null)
								continue;

							linkedType = exportedType?.Resolve ();
						}

						switch (attributeTypeName) {
						case nameof (RemovedTypeInAssemblyAttribute):
							if (linkedType != null)
								Assert.Fail ($"Type `{expectedTypeName}' should have been removed from assembly {assemblyName}");
							GetOriginalTypeFromInAssemblyAttribute (checkAttrInAssembly);
							break;
						case nameof (KeptTypeInAssemblyAttribute):
							if (linkedType == null)
								Assert.Fail ($"Type `{expectedTypeName}' should have been kept in assembly {assemblyName}");
							break;
						case nameof (RemovedInterfaceOnTypeInAssemblyAttribute):
							if (linkedType == null)
								Assert.Fail ($"Type `{expectedTypeName}' should have been kept in assembly {assemblyName}");
							VerifyRemovedInterfaceOnTypeInAssembly (checkAttrInAssembly, linkedType);
							break;
						case nameof (KeptInterfaceOnTypeInAssemblyAttribute):
							if (linkedType == null)
								Assert.Fail ($"Type `{expectedTypeName}' should have been kept in assembly {assemblyName}");
							VerifyKeptInterfaceOnTypeInAssembly (checkAttrInAssembly, linkedType);
							break;
						case nameof (RemovedMemberInAssemblyAttribute):
							if (linkedType == null)
								continue;

							VerifyRemovedMemberInAssembly (checkAttrInAssembly, linkedType);
							break;
						case nameof (KeptBaseOnTypeInAssemblyAttribute):
							if (linkedType == null)
								Assert.Fail ($"Type `{expectedTypeName}' should have been kept in assembly {assemblyName}");
							VerifyKeptBaseOnTypeInAssembly (checkAttrInAssembly, linkedType);
							break;
						case nameof (KeptMemberInAssemblyAttribute):
							if (linkedType == null)
								Assert.Fail ($"Type `{expectedTypeName}' should have been kept in assembly {assemblyName}");

							VerifyKeptMemberInAssembly (checkAttrInAssembly, linkedType);
							break;
						case nameof (RemovedForwarderAttribute):
							if (linkedAssembly.MainModule.ExportedTypes.Any (l => l.Name == expectedTypeName))
								Assert.Fail ($"Forwarder `{expectedTypeName}' should have been removed from assembly {assemblyName}");

							break;

						case nameof (RemovedAssemblyReferenceAttribute):
							Assert.False (linkedAssembly.MainModule.AssemblyReferences.Any (l => l.Name == expectedTypeName),
								$"AssemblyRef '{expectedTypeName}' should have been removed from assembly {assemblyName}");
							break;

						case nameof (KeptResourceInAssemblyAttribute):
							VerifyKeptResourceInAssembly (checkAttrInAssembly);
							break;
						case nameof (RemovedResourceInAssemblyAttribute):
							VerifyRemovedResourceInAssembly (checkAttrInAssembly);
							break;
						case nameof (KeptReferencesInAssemblyAttribute):
							VerifyKeptReferencesInAssembly (checkAttrInAssembly);
							break;
						case nameof (ExpectedInstructionSequenceOnMemberInAssemblyAttribute):
							if (linkedType == null)
								Assert.Fail ($"Type `{expectedTypeName}` should have been kept in assembly {assemblyName}");
							VerifyExpectedInstructionSequenceOnMemberInAssembly (checkAttrInAssembly, linkedType);
							break;
						default:
							UnhandledOtherAssemblyAssertion (expectedTypeName, checkAttrInAssembly, linkedType);
							break;
						}
					}
				}
			} catch (AssemblyResolutionException e) {
				Assert.Fail ($"Failed to resolve linked assembly `{e.AssemblyReference.Name}`.  It must not exist in any of the output directories:\n\t{_linkedResolver.GetSearchDirectories ().Aggregate ((buff, s) => $"{buff}\n\t{s}")}\n");
			}
		}

		void VerifyKeptAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly)
		{
			VerifyAttributeInAssembly (inAssemblyAttribute, linkedAssembly, VerifyCustomAttributeKept);
		}

		void VerifyRemovedAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly)
		{
			VerifyAttributeInAssembly (inAssemblyAttribute, linkedAssembly, VerifyCustomAttributeRemoved);
		}

		void VerifyAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly, Action<ICustomAttributeProvider, string> assertExpectedAttribute)
		{
			var assemblyName = (string) inAssemblyAttribute.ConstructorArguments[0].Value;
			string expectedAttributeTypeName;
			var attributeTypeOrTypeName = inAssemblyAttribute.ConstructorArguments[1].Value;
			if (attributeTypeOrTypeName is TypeReference typeReference) {
				expectedAttributeTypeName = typeReference.FullName;
			} else {
				expectedAttributeTypeName = attributeTypeOrTypeName.ToString ();
			}

			if (inAssemblyAttribute.ConstructorArguments.Count == 2) {
				// Assembly
				assertExpectedAttribute (linkedAssembly, expectedAttributeTypeName);
				return;
			}

			// We are asserting on type or member
			var typeOrTypeName = inAssemblyAttribute.ConstructorArguments[2].Value;
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute.ConstructorArguments[0].Value.ToString (), typeOrTypeName);
			if (originalType == null)
				Assert.Fail ($"Invalid test assertion.  The original `{assemblyName}` does not contain a type `{typeOrTypeName}`");

			var linkedType = linkedAssembly.MainModule.GetType (originalType.FullName);
			if (linkedType == null)
				Assert.Fail ($"Missing expected type `{typeOrTypeName}` in `{assemblyName}`");

			if (inAssemblyAttribute.ConstructorArguments.Count == 3) {
				assertExpectedAttribute (linkedType, expectedAttributeTypeName);
				return;
			}

			// we are asserting on a member
			string memberName = (string) inAssemblyAttribute.ConstructorArguments[3].Value;

			// We will find the matching type from the original assembly first that way we can confirm
			// that the name defined in the attribute corresponds to a member that actually existed
			var originalFieldMember = originalType.Fields.FirstOrDefault (m => m.Name == memberName);
			if (originalFieldMember != null) {
				var linkedField = linkedType.Fields.FirstOrDefault (m => m.Name == memberName);
				if (linkedField == null)
					Assert.Fail ($"Field `{memberName}` on Type `{originalType}` should have been kept");

				assertExpectedAttribute (linkedField, expectedAttributeTypeName);
				return;
			}

			var originalPropertyMember = originalType.Properties.FirstOrDefault (m => m.Name == memberName);
			if (originalPropertyMember != null) {
				var linkedProperty = linkedType.Properties.FirstOrDefault (m => m.Name == memberName);
				if (linkedProperty == null)
					Assert.Fail ($"Property `{memberName}` on Type `{originalType}` should have been kept");

				assertExpectedAttribute (linkedProperty, expectedAttributeTypeName);
				return;
			}

			var originalMethodMember = originalType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
			if (originalMethodMember != null) {
				var linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
				if (linkedMethod == null)
					Assert.Fail ($"Method `{memberName}` on Type `{originalType}` should have been kept");

				assertExpectedAttribute (linkedMethod, expectedAttributeTypeName);
				return;
			}

			Assert.Fail ($"Invalid test assertion.  No member named `{memberName}` exists on the original type `{originalType}`");
		}

		static void VerifyCopyAssemblyIsKeptUnmodified (NPath outputDirectory, string assemblyName)
		{
			string inputAssemblyPath = Path.Combine (Directory.GetParent (outputDirectory).ToString (), "input", assemblyName);
			string outputAssemblyPath = Path.Combine (outputDirectory, assemblyName);
			Assert.IsTrue (File.ReadAllBytes (inputAssemblyPath).SequenceEqual (File.ReadAllBytes (outputAssemblyPath)),
				$"Expected assemblies\n" +
				$"\t{inputAssemblyPath}\n" +
				$"\t{outputAssemblyPath}\n" +
				$"binaries to be equal, since the input assembly has copy action.");
		}

		void VerifyCustomAttributeKept (ICustomAttributeProvider provider, string expectedAttributeTypeName)
		{
			var match = provider.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.FullName == expectedAttributeTypeName);
			if (match == null)
				Assert.Fail ($"Expected `{provider}` to have an attribute of type `{expectedAttributeTypeName}`");
		}

		void VerifyCustomAttributeRemoved (ICustomAttributeProvider provider, string expectedAttributeTypeName)
		{
			var match = provider.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.FullName == expectedAttributeTypeName);
			if (match != null)
				Assert.Fail ($"Expected `{provider}` to no longer have an attribute of type `{expectedAttributeTypeName}`");
		}

		void VerifyRemovedInterfaceOnTypeInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);

			var interfaceAssemblyName = inAssemblyAttribute.ConstructorArguments[2].Value.ToString ();
			var interfaceType = inAssemblyAttribute.ConstructorArguments[3].Value;

			var originalInterface = GetOriginalTypeFromInAssemblyAttribute (interfaceAssemblyName, interfaceType);
			if (!originalType.HasInterfaces)
				Assert.Fail ("Invalid assertion.  Original type does not have any interfaces");

			var originalInterfaceImpl = GetMatchingInterfaceImplementationOnType (originalType, originalInterface.FullName);
			if (originalInterfaceImpl == null)
				Assert.Fail ($"Invalid assertion.  Original type never had an interface of type `{originalInterface}`");

			var linkedInterfaceImpl = GetMatchingInterfaceImplementationOnType (linkedType, originalInterface.FullName);
			if (linkedInterfaceImpl != null)
				Assert.Fail ($"Expected `{linkedType}` to no longer have an interface of type {originalInterface.FullName}");
		}

		void VerifyKeptInterfaceOnTypeInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);

			var interfaceAssemblyName = inAssemblyAttribute.ConstructorArguments[2].Value.ToString ();
			var interfaceType = inAssemblyAttribute.ConstructorArguments[3].Value;

			var originalInterface = GetOriginalTypeFromInAssemblyAttribute (interfaceAssemblyName, interfaceType);
			if (!originalType.HasInterfaces)
				Assert.Fail ("Invalid assertion.  Original type does not have any interfaces");

			var originalInterfaceImpl = GetMatchingInterfaceImplementationOnType (originalType, originalInterface.FullName);
			if (originalInterfaceImpl == null)
				Assert.Fail ($"Invalid assertion.  Original type never had an interface of type `{originalInterface}`");

			var linkedInterfaceImpl = GetMatchingInterfaceImplementationOnType (linkedType, originalInterface.FullName);
			if (linkedInterfaceImpl == null)
				Assert.Fail ($"Expected `{linkedType}` to have interface of type {originalInterface.FullName}");
		}

		void VerifyKeptBaseOnTypeInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);

			var baseAssemblyName = inAssemblyAttribute.ConstructorArguments[2].Value.ToString ();
			var baseType = inAssemblyAttribute.ConstructorArguments[3].Value;

			var originalBase = GetOriginalTypeFromInAssemblyAttribute (baseAssemblyName, baseType);
			if (originalType.BaseType.Resolve () != originalBase)
				Assert.Fail ("Invalid assertion.  Original type's base does not match the expected base");

			Assert.That (originalBase.FullName, Is.EqualTo (linkedType.BaseType.FullName),
				$"Incorrect base on `{linkedType.FullName}`.  Expected `{originalBase.FullName}` but was `{linkedType.BaseType.FullName}`");
		}

		protected static InterfaceImplementation GetMatchingInterfaceImplementationOnType (TypeDefinition type, string expectedInterfaceTypeName)
		{
			return type.Interfaces.FirstOrDefault (impl => {
				var resolvedImpl = impl.InterfaceType.Resolve ();

				if (resolvedImpl == null)
					Assert.Fail ($"Failed to resolve interface : `{impl.InterfaceType}` on `{type}`");

				return resolvedImpl.FullName == expectedInterfaceTypeName;
			});
		}

		void VerifyRemovedMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
			foreach (var memberNameAttr in (CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[2].Value) {
				string memberName = (string) memberNameAttr.Value;

				// We will find the matching type from the original assembly first that way we can confirm
				// that the name defined in the attribute corresponds to a member that actually existed
				var originalFieldMember = originalType.Fields.FirstOrDefault (m => m.Name == memberName);
				if (originalFieldMember != null) {
					var linkedField = linkedType.Fields.FirstOrDefault (m => m.Name == memberName);
					if (linkedField != null)
						Assert.Fail ($"Field `{memberName}` on Type `{originalType}` should have been removed");

					continue;
				}

				var originalPropertyMember = originalType.Properties.FirstOrDefault (m => m.Name == memberName);
				if (originalPropertyMember != null) {
					var linkedProperty = linkedType.Properties.FirstOrDefault (m => m.Name == memberName);
					if (linkedProperty != null)
						Assert.Fail ($"Property `{memberName}` on Type `{originalType}` should have been removed");

					continue;
				}

				var originalMethodMember = originalType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
				if (originalMethodMember != null) {
					var linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
					if (linkedMethod != null)
						Assert.Fail ($"Method `{memberName}` on Type `{originalType}` should have been removed");

					continue;
				}

				Assert.Fail ($"Invalid test assertion.  No member named `{memberName}` exists on the original type `{originalType}`");
			}
		}

		void VerifyKeptMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
			var memberNames = (CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[2].Value;
			Assert.IsTrue (memberNames.Length > 0, "Invalid KeptMemberInAssemblyAttribute. Expected member names.");
			foreach (var memberNameAttr in memberNames) {
				string memberName = (string) memberNameAttr.Value;

				// We will find the matching type from the original assembly first that way we can confirm
				// that the name defined in the attribute corresponds to a member that actually existed

				if (TryVerifyKeptMemberInAssemblyAsField (memberName, originalType, linkedType))
					continue;

				if (TryVerifyKeptMemberInAssemblyAsProperty (memberName, originalType, linkedType))
					continue;

				if (TryVerifyKeptMemberInAssemblyAsMethod (memberName, originalType, linkedType))
					continue;

				Assert.Fail ($"Invalid test assertion.  No member named `{memberName}` exists on the original type `{originalType}`");
			}
		}

		protected virtual bool TryVerifyKeptMemberInAssemblyAsField (string memberName, TypeDefinition originalType, TypeDefinition linkedType)
		{
			var originalFieldMember = originalType.Fields.FirstOrDefault (m => m.Name == memberName);
			if (originalFieldMember != null) {
				var linkedField = linkedType.Fields.FirstOrDefault (m => m.Name == memberName);
				if (linkedField == null)
					Assert.Fail ($"Field `{memberName}` on Type `{originalType}` should have been kept");

				return true;
			}

			return false;
		}

		protected virtual bool TryVerifyKeptMemberInAssemblyAsProperty (string memberName, TypeDefinition originalType, TypeDefinition linkedType)
		{
			var originalPropertyMember = originalType.Properties.FirstOrDefault (m => m.Name == memberName);
			if (originalPropertyMember != null) {
				var linkedProperty = linkedType.Properties.FirstOrDefault (m => m.Name == memberName);
				if (linkedProperty == null)
					Assert.Fail ($"Property `{memberName}` on Type `{originalType}` should have been kept");

				return true;
			}

			return false;
		}

		protected virtual bool TryVerifyKeptMemberInAssemblyAsMethod (string memberName, TypeDefinition originalType, TypeDefinition linkedType)
		{
			return TryVerifyKeptMemberInAssemblyAsMethod (memberName, originalType, linkedType, out _, out _);
		}

		protected virtual bool TryVerifyKeptMemberInAssemblyAsMethod (string memberName, TypeDefinition originalType, TypeDefinition linkedType, out MethodDefinition originalMethod, out MethodDefinition linkedMethod)
		{
			originalMethod = originalType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
			if (originalMethod != null) {
				linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
				if (linkedMethod == null)
					Assert.Fail ($"Method `{memberName}` on Type `{originalType}` should have been kept");

				return true;
			}

			linkedMethod = null;
			return false;
		}

		void VerifyKeptReferencesInAssembly (CustomAttribute inAssemblyAttribute)
		{
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ());
			var expectedReferenceNames = ((CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[1].Value).Select (attr => (string) attr.Value).ToList ();
			for (int i = 0; i < expectedReferenceNames.Count; i++)
				if (expectedReferenceNames[i].EndsWith (".dll"))
					expectedReferenceNames[i] = expectedReferenceNames[i].Substring (0, expectedReferenceNames[i].LastIndexOf ("."));

			Assert.That (assembly.MainModule.AssemblyReferences.Select (asm => asm.Name), Is.EquivalentTo (expectedReferenceNames));
		}

		void VerifyKeptResourceInAssembly (CustomAttribute inAssemblyAttribute)
		{
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ());
			var resourceName = inAssemblyAttribute.ConstructorArguments[1].Value.ToString ();

			Assert.That (assembly.MainModule.Resources.Select (r => r.Name), Has.Member (resourceName));
		}

		void VerifyRemovedResourceInAssembly (CustomAttribute inAssemblyAttribute)
		{
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ());
			var resourceName = inAssemblyAttribute.ConstructorArguments[1].Value.ToString ();

			Assert.That (assembly.MainModule.Resources.Select (r => r.Name), Has.No.Member (resourceName));
		}

		void VerifyKeptAllTypesAndMembersInAssembly (AssemblyDefinition linked)
		{
			var original = ResolveOriginalsAssembly (linked.MainModule.Assembly.Name.Name);

			if (original == null)
				Assert.Fail ($"Failed to resolve original assembly {linked.MainModule.Assembly.Name.Name}");

			var originalTypes = original.AllDefinedTypes ().ToDictionary (t => t.FullName);
			var linkedTypes = linked.AllDefinedTypes ().ToDictionary (t => t.FullName);

			var missingInLinked = originalTypes.Keys.Except (linkedTypes.Keys);

			Assert.That (missingInLinked, Is.Empty, $"Expected all types to exist in the linked assembly {linked.Name}, but one or more were missing");

			foreach (var originalKvp in originalTypes) {
				var linkedType = linkedTypes[originalKvp.Key];

				var originalMembers = originalKvp.Value.AllMembers ().Select (m => m.FullName);
				var linkedMembers = linkedType.AllMembers ().Select (m => m.FullName);

				var missingMembersInLinked = originalMembers.Except (linkedMembers);

				Assert.That (missingMembersInLinked, Is.Empty, $"Expected all members of `{originalKvp.Key}`to exist in the linked assembly, but one or more were missing");
			}
		}

		static bool IsProducedByLinker (CustomAttribute attr)
		{
			var producedBy = attr.GetPropertyValue ("ProducedBy");
			return producedBy is null ? true : ((Tool) producedBy).HasFlag (Tool.Trimmer);
		}

		static IEnumerable<ICustomAttributeProvider> GetAttributeProviders (AssemblyDefinition assembly)
		{
			foreach (var testType in assembly.AllDefinedTypes ()) {
				foreach (var provider in testType.AllMembers ())
					yield return provider;

				yield return testType;
			}

			foreach (var module in assembly.Modules)
				yield return module;

			yield return assembly;
		}

		void VerifyLoggedMessages (AssemblyDefinition original, TrimmingTestLogger logger, bool checkRemainingErrors)
		{
			List<MessageContainer> loggedMessages = logger.GetLoggedMessages ();
			List<(ICustomAttributeProvider, CustomAttribute)> expectedNoWarningsAttributes = new ();
			foreach (var attrProvider in GetAttributeProviders (original)) {
				foreach (var attr in attrProvider.CustomAttributes) {
					if (!IsProducedByLinker (attr))
						continue;

					switch (attr.AttributeType.Name) {

					case nameof (LogContainsAttribute): {
							var expectedMessage = (string) attr.ConstructorArguments[0].Value;

							List<MessageContainer> matchedMessages;
							if ((bool) attr.ConstructorArguments[1].Value)
								matchedMessages = loggedMessages.Where (m => Regex.IsMatch (m.ToString (), expectedMessage)).ToList ();
							else
								matchedMessages = loggedMessages.Where (m => m.ToString ().Contains (expectedMessage)).ToList (); ;
							Assert.IsTrue (
								matchedMessages.Count > 0,
								$"Expected to find logged message matching `{expectedMessage}`, but no such message was found.{Environment.NewLine}Logged messages:{Environment.NewLine}{string.Join (Environment.NewLine, loggedMessages)}");

							foreach (var matchedMessage in matchedMessages)
								loggedMessages.Remove (matchedMessage);
						}
						break;

					case nameof (LogDoesNotContainAttribute): {
							var unexpectedMessage = (string) attr.ConstructorArguments[0].Value;
							foreach (var loggedMessage in loggedMessages) {
								Assert.That (() => {
									if ((bool) attr.ConstructorArguments[1].Value)
										return !Regex.IsMatch (loggedMessage.ToString (), unexpectedMessage);
									return !loggedMessage.ToString ().Contains (unexpectedMessage);
								},
								$"Expected to not find logged message matching `{unexpectedMessage}`, but found:{Environment.NewLine}{loggedMessage.ToString ()}{Environment.NewLine}Logged messages:{Environment.NewLine}{string.Join (Environment.NewLine, loggedMessages)}");
							}
						}
						break;

					case nameof (ExpectedWarningAttribute): {
							var expectedWarningCode = (string) attr.GetConstructorArgumentValue (0);
							if (!expectedWarningCode.StartsWith ("IL")) {
								Assert.Fail ($"The warning code specified in {nameof (ExpectedWarningAttribute)} must start with the 'IL' prefix. Specified value: '{expectedWarningCode}'.");
							}
							var expectedMessageContains = ((CustomAttributeArgument[]) attr.GetConstructorArgumentValue (1)).Select (a => (string) a.Value).ToArray ();
							string fileName = (string) attr.GetPropertyValue ("FileName");
							int? sourceLine = (int?) attr.GetPropertyValue ("SourceLine");
							int? sourceColumn = (int?) attr.GetPropertyValue ("SourceColumn");
							bool? isCompilerGeneratedCode = (bool?) attr.GetPropertyValue ("CompilerGeneratedCode");

							int expectedWarningCodeNumber = int.Parse (expectedWarningCode.Substring (2));
							string expectedOrigin = null;
							bool expectedWarningFound = false;

							foreach (var loggedMessage in loggedMessages) {

								if (loggedMessage.Category != MessageCategory.Warning || loggedMessage.Code != expectedWarningCodeNumber)
									continue;

								bool messageNotFound = false;
								foreach (var expectedMessage in expectedMessageContains) {
									if (!loggedMessage.Text.Contains (expectedMessage)) {
										messageNotFound = true;
										break;
									}
								}
								if (messageNotFound)
									continue;

								if (fileName != null) {
									if (loggedMessage.Origin == null)
										continue;

									var actualOrigin = loggedMessage.Origin.Value;
									if (actualOrigin.FileName != null) {
										// Note: string.Compare(string, StringComparison) doesn't exist in .NET Framework API set
										if (actualOrigin.FileName.IndexOf (fileName, StringComparison.OrdinalIgnoreCase) < 0)
											continue;

										if (sourceLine != null && loggedMessage.Origin?.SourceLine != sourceLine.Value)
											continue;

										if (sourceColumn != null && loggedMessage.Origin?.SourceColumn != sourceColumn.Value)
											continue;
									} else {
										// The warning was logged with member/ILoffset, so it didn't have line/column info filled
										// but it will be computed from PDBs, so instead compare it in a string representation
										if (expectedOrigin == null) {
											expectedOrigin = fileName;
											if (sourceLine.HasValue) {
												expectedOrigin += "(" + sourceLine.Value;
												if (sourceColumn.HasValue)
													expectedOrigin += "," + sourceColumn.Value;
												expectedOrigin += ")";
											}
										}

										string actualOriginString = actualOrigin.ToString () ?? "";
										if (!actualOriginString.EndsWith (expectedOrigin, StringComparison.OrdinalIgnoreCase))
											continue;
									}
								} else if (isCompilerGeneratedCode == true) {
									if (loggedMessage.Origin?.Provider is not IMemberDefinition memberDefinition)
										continue;

									if (attrProvider is IMemberDefinition expectedMember) {
										string actualName = memberDefinition.DeclaringType.FullName + "." + memberDefinition.Name;

										if (actualName.StartsWith (expectedMember.DeclaringType.FullName) &&
											(actualName.Contains ("<" + expectedMember.Name + ">") ||
											 actualName.EndsWith ("get_" + expectedMember.Name) ||
											 actualName.EndsWith ("set_" + expectedMember.Name))) {
											expectedWarningFound = true;
											loggedMessages.Remove (loggedMessage);
											break;
										}
										if (memberDefinition is not MethodDefinition)
											continue;
										if (actualName.StartsWith (expectedMember.DeclaringType.FullName)) {
											if (memberDefinition.Name == ".cctor" &&
												(expectedMember is FieldDefinition || expectedMember is PropertyDefinition)) {
												expectedWarningFound = true;
												loggedMessages.Remove (loggedMessage);
												break;
											}
											if (memberDefinition.Name == ".ctor" &&
												(expectedMember is FieldDefinition || expectedMember is PropertyDefinition || memberDefinition.DeclaringType.FullName == expectedMember.FullName)) {
												expectedWarningFound = true;
												loggedMessages.Remove (loggedMessage);
												break;
											}
										}
									} else if (attrProvider is AssemblyDefinition expectedAssembly) {
										// Allow assembly-level attributes to match warnings from compiler-generated Main
										if (memberDefinition.Name == "<Main>$" &&
											memberDefinition.DeclaringType.FullName == "Program" &&
											memberDefinition.DeclaringType.Module.Assembly.Name.Name == expectedAssembly.Name.Name) {
											expectedWarningFound = true;
											loggedMessages.Remove (loggedMessage);
											break;
										}
									}
									continue;
								} else {
									if (LogMessageHasSameOriginMember (loggedMessage, attrProvider)) {
										expectedWarningFound = true;
										loggedMessages.Remove (loggedMessage);
										break;
									}
									continue;
								}

								expectedWarningFound = true;
								loggedMessages.Remove (loggedMessage);
								break;
							}

							var expectedOriginString = fileName == null
								? attrProvider switch {
									MethodDefinition method => method.GetDisplayName (),
									IMemberDefinition member => member.FullName,
									AssemblyDefinition asm => asm.Name.Name,
									_ => throw new NotImplementedException ()
								} + ": "
								: "";

							Assert.IsTrue (expectedWarningFound,
								$"Expected to find warning: {(fileName != null ? fileName + (sourceLine != null ? $"({sourceLine},{sourceColumn})" : "") + ": " : "")}" +
								$"warning {expectedWarningCode}: {expectedOriginString}" +
								$"and message containing {string.Join (" ", expectedMessageContains.Select (m => "'" + m + "'"))}, " +
								$"but no such message was found.{Environment.NewLine}Logged messages:{Environment.NewLine}{string.Join (Environment.NewLine, loggedMessages)}");
						}
						break;

					case nameof (ExpectedNoWarningsAttribute): {
							// Postpone processing of negative checks, to make it possible to mark some warnings as expected (will be removed from the list above)
							// and then do the negative check on the rest.
							expectedNoWarningsAttributes.Add ((attrProvider, attr));
							break;
						}
					}
				}
			}

			foreach ((var attrProvider, var attr) in expectedNoWarningsAttributes) {
				var unexpectedWarningCode = attr.ConstructorArguments.Count == 0 ? null : (string) attr.GetConstructorArgumentValue (0);
				if (unexpectedWarningCode != null && !unexpectedWarningCode.StartsWith ("IL")) {
					Assert.Fail ($"The warning code specified in ExpectedNoWarnings attribute must start with the 'IL' prefix. Specified value: '{unexpectedWarningCode}'.");
				}

				int? unexpectedWarningCodeNumber = unexpectedWarningCode == null ? null : int.Parse (unexpectedWarningCode.Substring (2));

				MessageContainer? unexpectedWarningMessage = null;
				foreach (var mc in logger.GetLoggedMessages ()) {
					if (mc.Category != MessageCategory.Warning)
						continue;

					if (unexpectedWarningCodeNumber != null && unexpectedWarningCodeNumber.Value != mc.Code)
						continue;

					// This is a hacky way to say anything in the "subtree" of the attrProvider
					if (attrProvider is IMemberDefinition attrMember && (mc.Origin?.Provider is IMemberDefinition member) && member.FullName.Contains (attrMember.FullName) != true)
						continue;

					unexpectedWarningMessage = mc;
					break;
				}

				Assert.IsNull (unexpectedWarningMessage,
					$"Unexpected warning found: {unexpectedWarningMessage}");
			}

			if (checkRemainingErrors) {
				var remainingErrors = loggedMessages.Where (m => Regex.IsMatch (m.ToString (), @".*(error | warning): \d{4}.*"));
				Assert.IsEmpty (remainingErrors, $"Found unexpected errors:{Environment.NewLine}{string.Join (Environment.NewLine, remainingErrors)}");
			}

			bool LogMessageHasSameOriginMember (MessageContainer mc, ICustomAttributeProvider expectedOriginProvider)
			{
				var origin = mc.Origin;
				Debug.Assert (origin != null);
				if (origin?.Provider is AssemblyDefinition asm)
					return expectedOriginProvider is AssemblyDefinition expectedAsm && asm.Name.Name == expectedAsm.Name.Name;

				if (origin?.Provider is not IMemberDefinition actualMember)
					return false;

				if (expectedOriginProvider is not IMemberDefinition expectedOriginMember)
					return false;

				return actualMember.FullName == expectedOriginMember.FullName;
			}
		}

		void VerifyRecordedDependencies (AssemblyDefinition original, TestDependencyRecorder dependencyRecorder)
		{
			foreach (var typeWithRemoveInAssembly in original.AllDefinedTypes ()) {
				foreach (var attr in typeWithRemoveInAssembly.CustomAttributes) {
					if (attr.AttributeType.Resolve ()?.Name == nameof (DependencyRecordedAttribute)) {
						var expectedSource = (string) attr.ConstructorArguments[0].Value;
						var expectedTarget = (string) attr.ConstructorArguments[1].Value;
						var expectedMarked = (string) attr.ConstructorArguments[2].Value;

						if (!dependencyRecorder.Dependencies.Any (dependency => {
							if (dependency.Source != expectedSource)
								return false;

							if (dependency.Target != expectedTarget)
								return false;

							return expectedMarked == null || dependency.Marked.ToString () == expectedMarked;
						})) {

							string targetCandidates = string.Join (Environment.NewLine, dependencyRecorder.Dependencies
								.Where (d => d.Target.ToLowerInvariant ().Contains (expectedTarget.ToLowerInvariant ()))
								.Select (d => "\t" + DependencyToString (d)));
							string sourceCandidates = string.Join (Environment.NewLine, dependencyRecorder.Dependencies
								.Where (d => d.Source.ToLowerInvariant ().Contains (expectedSource.ToLowerInvariant ()))
								.Select (d => "\t" + DependencyToString (d)));

							Assert.Fail (
								$"Expected to find recorded dependency '{expectedSource} -> {expectedTarget} {expectedMarked ?? string.Empty}'{Environment.NewLine}" +
								$"Potential dependencies matching the target: {Environment.NewLine}{targetCandidates}{Environment.NewLine}" +
								$"Potential dependencies matching the source: {Environment.NewLine}{sourceCandidates}{Environment.NewLine}" +
								$"If there's no matches, try to specify just a part of the source/target name and rerun the test to get potential matches.");
						}
					}
				}
			}

			static string DependencyToString (TestDependencyRecorder.Dependency dependency)
			{
				return $"{dependency.Source} -> {dependency.Target} Marked: {dependency.Marked}";
			}
		}

		void VerifyExpectedInstructionSequenceOnMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
			var memberName = (string) inAssemblyAttribute.ConstructorArguments[2].Value;

			if (TryVerifyKeptMemberInAssemblyAsMethod (memberName, originalType, linkedType, out MethodDefinition originalMethod, out MethodDefinition linkedMethod)) {
				static string[] valueCollector (MethodDefinition m) => AssemblyChecker.FormatMethodBody (m.Body);
				var linkedValues = valueCollector (linkedMethod);
				var srcValues = valueCollector (originalMethod);

				var expected = ((CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[3].Value)?.Select (arg => arg.Value.ToString ()).ToArray ();
				Assert.That (
					linkedValues,
					Is.EquivalentTo (expected),
					$"Expected method `{originalMethod} to have its {nameof (ExpectedInstructionSequenceOnMemberInAssemblyAttribute)} modified, however, the sequence does not match the expected value\n{FormattingUtils.FormatSequenceCompareFailureMessage2 (linkedValues, expected, srcValues)}");

				return;
			}

			Assert.Fail ($"Invalid test assertion.  No method named `{memberName}` exists on the original type `{originalType}`");
		}

		static string GetFullMemberNameFromDefinition (IMetadataTokenProvider member)
		{
			return GetFullMemberNameFromDefinition (member, out _);
		}

		static string GetFullMemberNameFromDefinition (IMetadataTokenProvider member, out string genericMember)
		{
			// Method which basically returns the same as member.GetDisplayName () for the resolved member,
			// but with the return type if the input is a MethodReturnType.
			// If the input is a generic parameter, the returned string will be the name of the generic
			// parameter, and the full member name of the member which declares the generic parameter will be
			// returned via the out parameter.

			genericMember = null;
			if (member == null)
				return null;
			else if (member is TypeSpecification typeSpecification)
				return typeSpecification.FullName;
			else if (member is MethodSpecification methodSpecification)
				member = methodSpecification.ElementMethod.Resolve ();
			else if (member is GenericParameter genericParameter) {
				var declaringType = genericParameter.DeclaringType?.Resolve ();
				if (declaringType != null) {
					genericMember = declaringType.GetDisplayName ();
					return genericParameter.FullName;
				}

				var declaringMethod = genericParameter.DeclaringMethod?.Resolve ();
				if (declaringMethod != null) {
					genericMember = GetFullMemberNameFromDefinition (declaringMethod);
					return genericParameter.FullName;
				}

				return genericParameter.FullName;
			} else if (member is MemberReference memberReference)
				member = memberReference.Resolve ();

			if (member is MemberReference memberRef)
				return memberRef.GetDisplayName ();

			if (member is MethodReturnType returnType) {
				MethodDefinition method = (MethodDefinition) returnType.Method;
				return method.ReturnType + " " + method.GetDisplayName ();
			}

			if (member is AssemblyDefinition assembly)
				return assembly.Name.Name;

			throw new NotImplementedException ($"Getting the full member name has not been implemented for {member}");
		}

		protected TypeDefinition GetOriginalTypeFromInAssemblyAttribute (CustomAttribute inAssemblyAttribute)
		{
			string assemblyName;
			if (inAssemblyAttribute.HasProperties && inAssemblyAttribute.Properties[0].Name == "ExpectationAssemblyName")
				assemblyName = inAssemblyAttribute.Properties[0].Argument.Value.ToString ();
			else
				assemblyName = inAssemblyAttribute.ConstructorArguments[0].Value.ToString ();

			return GetOriginalTypeFromInAssemblyAttribute (assemblyName, inAssemblyAttribute.ConstructorArguments[1].Value);
		}

		protected TypeDefinition GetOriginalTypeFromInAssemblyAttribute (string assemblyName, object typeOrTypeName)
		{
			if (typeOrTypeName is TypeReference attributeValueAsTypeReference)
				return attributeValueAsTypeReference.Resolve ();

			var assembly = ResolveOriginalsAssembly (assemblyName);

			var expectedTypeName = typeOrTypeName.ToString ();
			var originalType = assembly.MainModule.GetType (expectedTypeName);
			if (originalType == null)
				Assert.Fail ($"Invalid test assertion.  Unable to locate the original type `{expectedTypeName}.`");
			return originalType;
		}

		Dictionary<string, List<CustomAttribute>> BuildOtherAssemblyCheckTable (AssemblyDefinition original)
		{
			var checks = new Dictionary<string, List<CustomAttribute>> ();

			foreach (var typeWithRemoveInAssembly in original.AllDefinedTypes ()) {
				foreach (var attr in typeWithRemoveInAssembly.CustomAttributes.Where (IsTypeInOtherAssemblyAssertion)) {
					var assemblyName = (string) attr.ConstructorArguments[0].Value;

					Tool? toolTarget = (Tool?) (int?) attr.GetPropertyValue ("Tool");
					if (toolTarget is not null && !toolTarget.Value.HasFlag (Tool.Trimmer))
						continue;

					if (!checks.TryGetValue (assemblyName, out List<CustomAttribute> checksForAssembly))
						checks[assemblyName] = checksForAssembly = new List<CustomAttribute> ();

					checksForAssembly.Add (attr);
				}
			}

			return checks;
		}

		protected virtual void UnhandledOtherAssemblyAssertion (string expectedTypeName, CustomAttribute checkAttrInAssembly, TypeDefinition linkedType)
		{
			throw new NotImplementedException ($"Type {expectedTypeName}, has an unknown other assembly attribute of type {checkAttrInAssembly.AttributeType}");
		}

		bool IsTypeInOtherAssemblyAssertion (CustomAttribute attr)
		{
			return attr.AttributeType.Resolve ()?.DerivesFrom (nameof (BaseInAssemblyAttribute)) ?? false;
		}

		static bool HasAttribute (ICustomAttributeProvider caProvider, string attributeName)
		{
			return TryGetCustomAttribute (caProvider, attributeName, out var _);
		}

#nullable enable
		static bool TryGetCustomAttribute (ICustomAttributeProvider caProvider, string attributeName, [NotNullWhen (true)] out CustomAttribute? customAttribute)
		{
			if (caProvider is AssemblyDefinition assembly && assembly.EntryPoint != null) {
				customAttribute = assembly.EntryPoint.DeclaringType.CustomAttributes
					.FirstOrDefault (attr => attr!.AttributeType.Name == attributeName, null);
				return customAttribute is not null;
			}

			if (caProvider is TypeDefinition type) {
				customAttribute = type.CustomAttributes
					.FirstOrDefault (attr => attr!.AttributeType.Name == attributeName, null);
				return customAttribute is not null;
			}
			customAttribute = null;
			return false;
		}

		static IEnumerable<CustomAttribute> GetCustomAttributes (ICustomAttributeProvider caProvider, string attributeName)
		{
			if (caProvider is AssemblyDefinition assembly && assembly.EntryPoint != null)
				return assembly.EntryPoint.DeclaringType.CustomAttributes
					.Where (attr => attr!.AttributeType.Name == attributeName);

			if (caProvider is TypeDefinition type)
				return type.CustomAttributes
					.Where (attr => attr!.AttributeType.Name == attributeName);

			return Enumerable.Empty<CustomAttribute> ();
		}
#nullable restore
	}
}
