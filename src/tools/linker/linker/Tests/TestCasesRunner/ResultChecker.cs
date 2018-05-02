﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class ResultChecker
	{
		readonly BaseAssemblyResolver _originalsResolver;
		readonly BaseAssemblyResolver _linkedResolver;
		readonly ReaderParameters _readerParameters;
		readonly PeVerifier _peVerifier;

		public ResultChecker ()
			: this(new TestCaseAssemblyResolver (), new TestCaseAssemblyResolver (), new PeVerifier (),
					new ReaderParameters
					{
						SymbolReaderProvider = new DefaultSymbolReaderProvider (false)
					})
		{
		}

		public ResultChecker (BaseAssemblyResolver originalsResolver, BaseAssemblyResolver linkedResolver, PeVerifier peVerifier, ReaderParameters readerParameters)
		{
			_originalsResolver = originalsResolver;
			_linkedResolver = linkedResolver;
			_peVerifier = peVerifier;
			_readerParameters = readerParameters;
		}

		public virtual void Check (LinkedTestCaseResult linkResult)
		{
			Assert.IsTrue (linkResult.OutputAssemblyPath.FileExists (), $"The linked output assembly was not found.  Expected at {linkResult.OutputAssemblyPath}");

			InitializeResolvers (linkResult);

			try
			{
				var original = ResolveOriginalsAssembly (linkResult.ExpectationsAssemblyPath.FileNameWithoutExtension);
				PerformOutputAssemblyChecks (original, linkResult.OutputAssemblyPath.Parent);
				PerformOutputSymbolChecks (original, linkResult.OutputAssemblyPath.Parent);

				var linked = ResolveLinkedAssembly (linkResult.OutputAssemblyPath.FileNameWithoutExtension);

				CreateAssemblyChecker (original, linked).Verify ();

				VerifyLinkingOfOtherAssemblies (original);

				_peVerifier.Check (linkResult, original);

				AdditionalChecking (linkResult, original, linked);
			}
			finally
			{
				_originalsResolver.Dispose ();
				_linkedResolver.Dispose ();
			}
		}

		protected virtual AssemblyChecker CreateAssemblyChecker (AssemblyDefinition original, AssemblyDefinition linked)
		{
			return new AssemblyChecker (original, linked);
		}

		void InitializeResolvers (LinkedTestCaseResult linkedResult)
		{
			_originalsResolver.AddSearchDirectory (linkedResult.ExpectationsAssemblyPath.Parent.ToString ());
			_linkedResolver.AddSearchDirectory (linkedResult.OutputAssemblyPath.Parent.ToString ());
		}

		AssemblyDefinition ResolveLinkedAssembly (string assemblyName)
		{
			var cleanAssemblyName = assemblyName;
			if (assemblyName.EndsWith(".exe") || assemblyName.EndsWith(".dll"))
				cleanAssemblyName = System.IO.Path.GetFileNameWithoutExtension (assemblyName);
			return _linkedResolver.Resolve (new AssemblyNameReference (cleanAssemblyName, null), _readerParameters);
		}

		AssemblyDefinition ResolveOriginalsAssembly (string assemblyName)
		{
			var cleanAssemblyName = assemblyName;
			if (assemblyName.EndsWith (".exe") || assemblyName.EndsWith (".dll"))
				cleanAssemblyName = Path.GetFileNameWithoutExtension (assemblyName);
			return _originalsResolver.Resolve (new AssemblyNameReference (cleanAssemblyName, null), _readerParameters);
		}

		void PerformOutputAssemblyChecks (AssemblyDefinition original, NPath outputDirectory)
		{
			var assembliesToCheck = original.MainModule.Types.SelectMany (t => t.CustomAttributes).Where (attr => ExpectationsProvider.IsAssemblyAssertion(attr));

			foreach (var assemblyAttr in assembliesToCheck) {
				var name = (string) assemblyAttr.ConstructorArguments.First ().Value;
				var expectedPath = outputDirectory.Combine (name);

				if (assemblyAttr.AttributeType.Name == nameof (RemovedAssemblyAttribute))
					Assert.IsFalse (expectedPath.FileExists (), $"Expected the assembly {name} to not exist in {outputDirectory}, but it did");
				else if (assemblyAttr.AttributeType.Name == nameof (KeptAssemblyAttribute))
					Assert.IsTrue (expectedPath.FileExists (), $"Expected the assembly {name} to exist in {outputDirectory}, but it did not");
				else
					throw new NotImplementedException($"Unknown assembly assertion of type {assemblyAttr.AttributeType}");
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
					throw new NotImplementedException($"Unknown symbol file assertion of type {symbolAttr.AttributeType}");
			}
		}

		void VerifyKeptSymbols (CustomAttribute symbolsAttribute)
		{
			var assemblyName = (string) symbolsAttribute.ConstructorArguments [0].Value;
			var originalAssembly = ResolveOriginalsAssembly (assemblyName);
			var linkedAssembly = ResolveLinkedAssembly (assemblyName);

			if (linkedAssembly.MainModule.SymbolReader == null)
				Assert.Fail ($"Missing symbols for assembly `{linkedAssembly.MainModule.FileName}`");

			if (linkedAssembly.MainModule.SymbolReader.GetType () != originalAssembly.MainModule.SymbolReader.GetType ())
				Assert.Fail ($"Expected symbol provider of type `{originalAssembly.MainModule.SymbolReader}`, but was `{linkedAssembly.MainModule.SymbolReader}`");
		}

		void VerifyRemovedSymbols (CustomAttribute symbolsAttribute, NPath outputDirectory)
		{
			var assemblyName = (string) symbolsAttribute.ConstructorArguments [0].Value;
			try
			{
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

		protected virtual void AdditionalChecking (LinkedTestCaseResult linkResult, AssemblyDefinition original, AssemblyDefinition linked)
		{
		}

		void VerifyLinkingOfOtherAssemblies (AssemblyDefinition original)
		{
			var checks = BuildOtherAssemblyCheckTable (original);

			try {
				foreach (var assemblyName in checks.Keys) {
					using (var linkedAssembly = ResolveLinkedAssembly (assemblyName)) {
						foreach (var checkAttrInAssembly in checks [assemblyName]) {
							var expectedTypeName = checkAttrInAssembly.ConstructorArguments [1].Value.ToString ();
							var linkedType = linkedAssembly.MainModule.GetType (expectedTypeName);

							switch (checkAttrInAssembly.AttributeType.Name) {
							case nameof (RemovedTypeInAssemblyAttribute):
								if (linkedType != null)
									Assert.Fail ($"Type `{expectedTypeName}' should have been removed");
								GetOriginalTypeFromInAssemblyAttribute (checkAttrInAssembly);
								break;
							case nameof (KeptTypeInAssemblyAttribute):
								if (linkedType == null)
									Assert.Fail ($"Type `{expectedTypeName}' should have been kept");
								break;
							case nameof (RemovedMemberInAssemblyAttribute):
								if (linkedType == null)
									continue;

								VerifyRemovedMemberInAssembly (checkAttrInAssembly, linkedType);
								break;
							case nameof (KeptMemberInAssemblyAttribute):
								if (linkedType == null)
									Assert.Fail ($"Type `{expectedTypeName}' should have been kept");

								VerifyKeptMemberInAssembly (checkAttrInAssembly, linkedType);
								break;
							case nameof (RemovedForwarderAttribute):
								if (linkedAssembly.MainModule.ExportedTypes.Any (l => l.Name == expectedTypeName))
									Assert.Fail ($"Forwarder `{expectedTypeName}' should have been removed");
							
								break;
							case nameof (KeptResourceInAssemblyAttribute):
								VerifyKeptResourceInAssembly (checkAttrInAssembly);
								break;
							case nameof (RemovedResourceInAssemblyAttribute):
								VerifyRemovedResourceInAssembly (checkAttrInAssembly);
								break;
							default:
								UnhandledOtherAssemblyAssertion (expectedTypeName, checkAttrInAssembly, linkedType);
								break;
							}
						}
					}
				}
			} catch (AssemblyResolutionException e) {
				Assert.Fail ($"Failed to resolve linked assembly `{e.AssemblyReference.Name}`.  It must not exist in any of the output directories:\n\t{_linkedResolver.GetSearchDirectories ().Aggregate ((buff, s) => $"{buff}\n\t{s}")}\n");
			}
		}

		void VerifyRemovedMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
		{
			var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
			foreach (var memberNameAttr in (CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments [2].Value) {
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
			foreach (var memberNameAttr in (CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments [2].Value) {
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
			var originalMethodMember = originalType.Methods.FirstOrDefault (m => m.GetSignature() == memberName);
			if (originalMethodMember != null) {
				var linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature() == memberName);
				if (linkedMethod == null)
					Assert.Fail ($"Method `{memberName}` on Type `{originalType}` should have been kept");

				return true;
			}

			return false;
		}

		void VerifyKeptResourceInAssembly (CustomAttribute inAssemblyAttribute)
		{
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments [0].Value.ToString ());
			var resourceName = inAssemblyAttribute.ConstructorArguments [1].Value.ToString ();

			Assert.That (assembly.MainModule.Resources.Select (r => r.Name), Has.Member (resourceName));
		}

		void VerifyRemovedResourceInAssembly (CustomAttribute inAssemblyAttribute)
		{
			var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments [0].Value.ToString ());
			var resourceName = inAssemblyAttribute.ConstructorArguments [1].Value.ToString ();

			Assert.That (assembly.MainModule.Resources.Select (r => r.Name), Has.No.Member (resourceName));
		}

		protected TypeDefinition GetOriginalTypeFromInAssemblyAttribute (CustomAttribute inAssemblyAttribute)
		{
			var attributeValueAsTypeReference = inAssemblyAttribute.ConstructorArguments [1].Value as TypeReference;
			if (attributeValueAsTypeReference != null)
				return attributeValueAsTypeReference.Resolve ();

			var assembly = ResolveOriginalsAssembly (inAssemblyAttribute.ConstructorArguments [0].Value.ToString ());

			var expectedTypeName = inAssemblyAttribute.ConstructorArguments [1].Value.ToString ();
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
					var assemblyName = (string) attr.ConstructorArguments [0].Value;
					List<CustomAttribute> checksForAssembly;
					if (!checks.TryGetValue (assemblyName, out checksForAssembly))
						checks [assemblyName] = checksForAssembly = new List<CustomAttribute> ();

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
			return attr.AttributeType.Resolve ().DerivesFrom (nameof (BaseInAssemblyAttribute));
		}
	}
}