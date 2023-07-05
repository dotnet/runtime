// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using ILCompiler.Logging;
using Internal.TypeSystem;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Extensions;
using Xunit;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class ResultChecker
	{
		private readonly BaseAssemblyResolver _originalsResolver;
		private readonly ReaderParameters _originalReaderParameters;
		private readonly ReaderParameters _linkedReaderParameters;

		public ResultChecker ()
			: this (new TestCaseAssemblyResolver (),
				new ReaderParameters {
					SymbolReaderProvider = new DefaultSymbolReaderProvider (false)
				},
				new ReaderParameters {
					SymbolReaderProvider = new DefaultSymbolReaderProvider (false)
				})
		{
		}

		public ResultChecker (BaseAssemblyResolver originalsResolver,
			ReaderParameters originalReaderParameters, ReaderParameters linkedReaderParameters)
		{
			_originalsResolver = originalsResolver;
			_originalReaderParameters = originalReaderParameters;
			_linkedReaderParameters = linkedReaderParameters;
		}

		private static bool ShouldValidateIL (AssemblyDefinition inputAssembly)
		{
			if (HasAttribute (inputAssembly, nameof (SkipPeVerifyAttribute)))
				return false;

			var caaIsUnsafeFlag = (CustomAttributeArgument caa) =>
				(caa.Type.Name == "String" && caa.Type.Namespace == "System")
				&& (string) caa.Value == "/unsafe";
			var customAttributeHasUnsafeFlag = (CustomAttribute ca) => ca.ConstructorArguments.Any (caaIsUnsafeFlag);
			if (GetCustomAttributes (inputAssembly, nameof (SetupCompileArgumentAttribute))
				.Any (customAttributeHasUnsafeFlag))
				return false;

			return true;
		}

		public virtual void Check (ILCompilerTestCaseResult testResult)
		{
			InitializeResolvers (testResult);

			try {
				var original = ResolveOriginalsAssembly (testResult.ExpectationsAssemblyPath.FileNameWithoutExtension);

				if (!HasAttribute (original, nameof (NoLinkedOutputAttribute))) {
					// TODO Validate presence of the main assembly - if it makes sense (reflection only somehow)

					// IL verification is impossible for NativeAOT since there's no IL output
					// if (ShouldValidateIL (original))
					//   VerifyIL ();

					InitialChecking (testResult, original);

					PerformOutputAssemblyChecks (original, testResult);
					PerformOutputSymbolChecks (original, testResult);

					if (!HasActiveSkipKeptItemsValidationAttribute (original.MainModule.GetType (testResult.TestCase.ReconstructedFullTypeName))) {
						CreateAssemblyChecker (original, testResult).Verify ();
					}
				}

				AdditionalChecking (testResult, original);
			} finally {
				_originalsResolver.Dispose ();
			}

			bool HasActiveSkipKeptItemsValidationAttribute(ICustomAttributeProvider provider)
			{
				if (TryGetCustomAttribute(provider, nameof(SkipKeptItemsValidationAttribute), out var attribute)) {
					object? by = attribute.GetPropertyValue (nameof (SkipKeptItemsValidationAttribute.By));
					return by is null ? true : ((Tool) by).HasFlag (Tool.NativeAot);
				}

				return false;
			}
		}

		protected virtual AssemblyChecker CreateAssemblyChecker (AssemblyDefinition original, ILCompilerTestCaseResult testResult)
		{
			return new AssemblyChecker (_originalsResolver, _originalReaderParameters, original, testResult);
		}

		private void InitializeResolvers (ILCompilerTestCaseResult linkedResult)
		{
			_originalsResolver.AddSearchDirectory (linkedResult.ExpectationsAssemblyPath.Parent.ToString ());
		}

		protected AssemblyDefinition ResolveOriginalsAssembly (string assemblyName)
		{
			var cleanAssemblyName = assemblyName;
			if (assemblyName.EndsWith (".exe") || assemblyName.EndsWith (".dll"))
				cleanAssemblyName = Path.GetFileNameWithoutExtension (assemblyName);
			return _originalsResolver.Resolve (new AssemblyNameReference (cleanAssemblyName, null), _originalReaderParameters);
		}

		private static void PerformOutputAssemblyChecks (AssemblyDefinition original, ILCompilerTestCaseResult testResult)
		{
			var assembliesToCheck = original.MainModule.Types.SelectMany (t => t.CustomAttributes).Where (ExpectationsProvider.IsAssemblyAssertion);
			var actionAssemblies = new HashSet<string> ();
			//bool trimModeIsCopy = false;

			foreach (var assemblyAttr in assembliesToCheck) {
				var name = (string) assemblyAttr.ConstructorArguments.First ().Value;
				name = Path.GetFileNameWithoutExtension (name);

#if false
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
#endif
			}

#if false
			if (trimModeIsCopy) {
				foreach (string assemblyName in Directory.GetFiles (Directory.GetParent (outputDirectory).ToString (), "input")) {
					var fileInfo = new FileInfo (assemblyName);
					if (fileInfo.Extension == ".dll" && !actionAssemblies.Contains (assemblyName))
						VerifyCopyAssemblyIsKeptUnmodified (outputDirectory, assemblyName + (assemblyName == "test" ? ".exe" : ".dll"));
				}
			}
#endif
		}

#pragma warning disable IDE0060 // Remove unused parameter
		private static void PerformOutputSymbolChecks (AssemblyDefinition original, ILCompilerTestCaseResult testResult)
#pragma warning restore IDE0060 // Remove unused parameter
		{
			// While NativeAOT has symbols, verifying them is rather difficult
		}

		protected virtual void AdditionalChecking (ILCompilerTestCaseResult linkResult, AssemblyDefinition original)
		{
			bool checkRemainingErrors = !HasAttribute (original.MainModule.GetType (linkResult.TestCase.ReconstructedFullTypeName), nameof (SkipRemainingErrorsValidationAttribute));
			VerifyLoggedMessages (original, linkResult.LogWriter, checkRemainingErrors);
		}

		private static bool IsProducedByNativeAOT (CustomAttribute attr)
		{
			var producedBy = attr.GetPropertyValue ("ProducedBy");
			return producedBy is null ? true : ((Tool) producedBy).HasFlag (Tool.NativeAot);
		}

		private static IEnumerable<ICustomAttributeProvider> GetAttributeProviders (AssemblyDefinition assembly)
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

		protected virtual void InitialChecking (ILCompilerTestCaseResult testResult, AssemblyDefinition original)
		{
			// PE verifier is done here in ILLinker, but that's not possible with NativeAOT
		}

		private void VerifyLoggedMessages (AssemblyDefinition original, TestLogWriter logger, bool checkRemainingErrors)
		{
			List<MessageContainer> loggedMessages = logger.GetLoggedMessages ();
			List<(IMemberDefinition, CustomAttribute)> expectedNoWarningsAttributes = new List<(IMemberDefinition, CustomAttribute)> ();
			foreach (var attrProvider in GetAttributeProviders (original)) {
				foreach (var attr in attrProvider.CustomAttributes) {
					if (!IsProducedByNativeAOT (attr))
						continue;

					switch (attr.AttributeType.Name) {

					case nameof (LogContainsAttribute): {
							var expectedMessage = (string) attr.ConstructorArguments[0].Value;

							List<MessageContainer> matchedMessages;
							if ((bool) attr.ConstructorArguments[1].Value)
								matchedMessages = loggedMessages.Where (m => Regex.IsMatch (m.ToString (), expectedMessage)).ToList ();
							else
								matchedMessages = loggedMessages.Where (m => MessageTextContains (m.ToString (), expectedMessage)).ToList ();
							Assert.True (
								matchedMessages.Count > 0,
								$"Expected to find logged message matching `{expectedMessage}`, but no such message was found.{Environment.NewLine}Logged messages:{Environment.NewLine}{string.Join (Environment.NewLine, loggedMessages)}");

							foreach (var matchedMessage in matchedMessages)
								loggedMessages.Remove (matchedMessage);
						}
						break;

					case nameof (LogDoesNotContainAttribute): {
							var unexpectedMessage = (string) attr.ConstructorArguments[0].Value;
							foreach (var loggedMessage in loggedMessages) {
								var isLogged = () => {
									if ((bool) attr.ConstructorArguments[1].Value)
										return !Regex.IsMatch (loggedMessage.ToString (), unexpectedMessage);
									return !MessageTextContains (loggedMessage.ToString (), unexpectedMessage);
								};

								Assert.True (
									isLogged (),
									$"Expected to not find logged message matching `{unexpectedMessage}`, but found:{Environment.NewLine}{loggedMessage}{Environment.NewLine}Logged messages:{Environment.NewLine}{string.Join (Environment.NewLine, loggedMessages)}");
							}
						}
						break;

					case nameof (ExpectedWarningAttribute): {
							var expectedWarningCode = (string) attr.GetConstructorArgumentValue (0);
							if (!expectedWarningCode.StartsWith ("IL")) {
								Assert.Fail ($"The warning code specified in {nameof (ExpectedWarningAttribute)} must start with the 'IL' prefix. Specified value: '{expectedWarningCode}'.");
							}
							var expectedMessageContains = ((CustomAttributeArgument[]) attr.GetConstructorArgumentValue (1)).Select (a => (string) a.Value).ToArray ();
							string fileName = (string) attr.GetPropertyValue ("FileName")!;
							int? sourceLine = (int?) attr.GetPropertyValue ("SourceLine");
							int? sourceColumn = (int?) attr.GetPropertyValue ("SourceColumn");
							bool? isCompilerGeneratedCode = (bool?) attr.GetPropertyValue ("CompilerGeneratedCode");

							int expectedWarningCodeNumber = int.Parse (expectedWarningCode.Substring (2));
							string? expectedOrigin = null;
							bool expectedWarningFound = false;

							foreach (var loggedMessage in loggedMessages) {
								if (loggedMessage.Category != MessageCategory.Warning || loggedMessage.Code != expectedWarningCodeNumber)
									continue;

								bool messageNotFound = false;
								foreach (var expectedMessage in expectedMessageContains) {
									if (!MessageTextContains (loggedMessage.Text, expectedMessage)) {
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
									if (loggedMessage.Origin?.MemberDefinition is MethodDesc methodDesc) {
										if (attrProvider is not IMemberDefinition expectedMember)
											continue;

										string? actualName = NameUtils.GetActualOriginDisplayName (methodDesc);
										string expectedTypeName = NameUtils.GetExpectedOriginDisplayName (expectedMember.DeclaringType);
										if (actualName?.Contains (expectedTypeName) == true &&
											actualName?.Contains ("<" + expectedMember.Name + ">") == true) {
											expectedWarningFound = true;
											loggedMessages.Remove (loggedMessage);
											break;
										}
										if (actualName?.StartsWith (expectedTypeName) == true &&
											actualName?.Contains (".cctor") == true &&
											(expectedMember is FieldDefinition || expectedMember is PropertyDefinition)) {
											expectedWarningFound = true;
											loggedMessages.Remove (loggedMessage);
											break;
										}
										if (methodDesc.IsConstructor &&
											new AssemblyQualifiedToken (methodDesc.OwningType).Equals(new AssemblyQualifiedToken (expectedMember))) {
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
								? NameUtils.GetExpectedOriginDisplayName (attrProvider) + ": "
								: "";

							Assert.True (expectedWarningFound,
								$"Expected to find warning: {(fileName != null ? fileName + (sourceLine != null ? $"({sourceLine},{sourceColumn})" : "") + ": " : "")}" +
								$"warning {expectedWarningCode}: {expectedOriginString}" +
								$"and message containing {string.Join (" ", expectedMessageContains.Select (m => "'" + m + "'"))}, " +
								$"but no such message was found.{Environment.NewLine}Logged messages:{Environment.NewLine}{string.Join (Environment.NewLine, loggedMessages)}");
						}
						break;

					case nameof (ExpectedNoWarningsAttribute):
						// Postpone processing of negative checks, to make it possible to mark some warnings as expected (will be removed from the list above)
						// and then do the negative check on the rest.
						var memberDefinition = attrProvider as IMemberDefinition;
						Assert.NotNull (memberDefinition);
						expectedNoWarningsAttributes.Add ((memberDefinition, attr));
						break;
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
					if ((mc.Origin?.MemberDefinition is TypeSystemEntity member) && member.ToString ()?.Contains (attrProvider.FullName) != true)
						continue;

					unexpectedWarningMessage = mc;
					break;
				}

				Assert.False (unexpectedWarningMessage.HasValue,
					$"Unexpected warning found: {unexpectedWarningMessage}");
			}

			if (checkRemainingErrors) {
				var remainingErrors = loggedMessages.Where (m => Regex.IsMatch (m.ToString (), @".*(error | warning): \d{4}.*"));
				Assert.False (remainingErrors.Any (), $"Found unexpected errors:{Environment.NewLine}{string.Join (Environment.NewLine, remainingErrors)}");
			}

			static bool LogMessageHasSameOriginMember (MessageContainer mc, ICustomAttributeProvider expectedOriginProvider)
			{
				var origin = mc.Origin;
				Debug.Assert (origin != null);
				if (origin?.MemberDefinition == null)
					return false;
				if (origin?.MemberDefinition is IAssemblyDesc asm)
					return expectedOriginProvider is AssemblyDefinition expectedAsm && asm.GetName().Name == expectedAsm.Name.Name;

				if (expectedOriginProvider is not IMemberDefinition expectedOriginMember)
					return false;

				var actualOriginToken = new AssemblyQualifiedToken (origin!.Value.MemberDefinition);
				var expectedOriginToken = new AssemblyQualifiedToken (expectedOriginMember);
				if (actualOriginToken.Equals (expectedOriginToken))
					return true;

				var actualMember = origin.Value.MemberDefinition;
				// Compensate for cases where for some reason the OM doesn't preserve the declaring types
				// on certain things after trimming.
				if (actualMember != null && GetOwningType (actualMember) == null &&
					GetMemberName (actualMember) == (expectedOriginProvider as IMemberDefinition)?.Name)
					return true;

				return false;
			}

			static TypeDesc? GetOwningType (TypeSystemEntity entity) => entity switch {
				DefType defType => defType.ContainingType,
				MethodDesc method => method.OwningType,
				FieldDesc field => field.OwningType,
				_ => null
			};

			static string? GetMemberName (TypeSystemEntity? entity) => entity switch {
				DefType defType => defType.Name,
				MethodDesc method => method.Name,
				FieldDesc field => field.Name,
				_ => null
			};

			static bool MessageTextContains (string message, string value)
			{
				// This is a workaround for different formatting of methods between ilc and illink/analyzer
				// Sometimes they're written with a space after comma and sometimes without
				//    Method(String,String)   - ilc
				//    Method(String, String)  - illink/analyzer
				return message.Contains (value) || message.Contains (NameUtils.ConvertSignatureToIlcFormat (value));
			}
		}

		private static bool HasAttribute (ICustomAttributeProvider caProvider, string attributeName)
		{
			return TryGetCustomAttribute (caProvider, attributeName, out var _);
		}

#nullable enable
		private static bool TryGetCustomAttribute (ICustomAttributeProvider caProvider, string attributeName, [NotNullWhen (true)] out CustomAttribute? customAttribute)
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

		private static IEnumerable<CustomAttribute> GetCustomAttributes (ICustomAttributeProvider caProvider, string attributeName)
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
