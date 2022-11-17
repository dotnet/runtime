// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using ILCompiler;
using ILCompiler.Logging;
using Internal.TypeSystem;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
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

		public virtual void Check (ILCompilerTestCaseResult trimmedResult)
		{
			InitializeResolvers (trimmedResult);

			try {
				var original = ResolveOriginalsAssembly (trimmedResult.ExpectationsAssemblyPath.FileNameWithoutExtension);
				AdditionalChecking (trimmedResult, original);
			} finally {
				_originalsResolver.Dispose ();
			}
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

		protected virtual void AdditionalChecking (ILCompilerTestCaseResult linkResult, AssemblyDefinition original)
		{
			bool checkRemainingErrors = !HasAttribute (original.MainModule.GetType (linkResult.TestCase.ReconstructedFullTypeName), nameof (SkipRemainingErrorsValidationAttribute));
			VerifyLoggedMessages (original, linkResult.LogWriter, checkRemainingErrors);
		}

		private static bool IsProducedByNativeAOT (CustomAttribute attr)
		{
			var producedBy = attr.GetPropertyValue ("ProducedBy");
			return producedBy is null ? true : ((ProducedBy) producedBy).HasFlag (ProducedBy.NativeAot);
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

		private void VerifyLoggedMessages (AssemblyDefinition original, TestLogWriter logger, bool checkRemainingErrors)
		{
			List<MessageContainer> loggedMessages = logger.GetLoggedMessages ();
			List<(IMemberDefinition, CustomAttribute)> expectedNoWarningsAttributes = new List<(IMemberDefinition, CustomAttribute)> ();
			foreach (var attrProvider in GetAttributeProviders (original)) {
				if (attrProvider.ToString () is string mystring && mystring.Contains ("RequiresInCompilerGeneratedCode/SuppressInLambda"))
					Debug.WriteLine ("Print");
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
								matchedMessages = loggedMessages.Where (m => MessageTextContains (m.ToString (), expectedMessage)).ToList (); ;
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
								if (loggedMessage.ToString ().Contains ("RequiresInCompilerGeneratedCode.SuppressInLambda")) {
									Debug.WriteLine ("Print 2");
								}

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

										string actualName = methodDesc.OwningType.ToString ().Replace ("+", ".") + "." + methodDesc.Name;
										if (actualName.Contains (expectedMember.DeclaringType.FullName.Replace ("/", ".")) &&
											actualName.Contains ("<" + expectedMember.Name + ">")) {
											expectedWarningFound = true;
											loggedMessages.Remove (loggedMessage);
											break;
										}
										if (actualName.StartsWith (expectedMember.DeclaringType.FullName) &&
											actualName.Contains (".cctor") && (expectedMember is FieldDefinition || expectedMember is PropertyDefinition)) {
											expectedWarningFound = true;
											loggedMessages.Remove (loggedMessage);
											break;
										}
										if (methodDesc.Name == ".ctor" &&
										methodDesc.OwningType.ToString () == expectedMember.FullName) {
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
								? GetExpectedOriginDisplayName (attrProvider) + ": "
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
				if (GetActualOriginDisplayName (origin?.MemberDefinition) == ConvertSignatureToIlcFormat (GetExpectedOriginDisplayName (expectedOriginProvider)))
					return true;

				var actualMember = origin!.Value.MemberDefinition;
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

			static string? GetActualOriginDisplayName (TypeSystemEntity? entity) => entity switch {
				DefType defType => TrimAssemblyNamePrefix (defType.ToString ()),
				MethodDesc method => TrimAssemblyNamePrefix (method.GetDisplayName ()),
				FieldDesc field => TrimAssemblyNamePrefix (field.ToString ()),
				ModuleDesc module => module.Assembly.GetName ().Name,
				_ => null
			};

			static string TrimAssemblyNamePrefix (string name)
			{
				if (name.StartsWith ('[')) {
					int i = name.IndexOf (']');
					if (i > 0) {
						return name.Substring (i + 1);
					}
				}

				return name;
			}

			static string GetExpectedOriginDisplayName (ICustomAttributeProvider provider) =>
				provider switch {
					MethodDefinition method => method.GetDisplayName (),
					FieldDefinition field => field.GetDisplayName (),
					IMemberDefinition member => member.FullName,
					AssemblyDefinition asm => asm.Name.Name,
					_ => throw new NotImplementedException ()
				};

			static bool MessageTextContains (string message, string value)
			{
				// This is a workaround for different formatting of methods between ilc and linker/analyzer
				// Sometimes they're written with a space after comma and sometimes without
				//    Method(String,String)   - ilc
				//    Method(String, String)  - linker/analyzer
				return message.Contains (value) || message.Contains (ConvertSignatureToIlcFormat (value));
			}

			static string ConvertSignatureToIlcFormat (string value)
			{
				if (value.Contains ('(') || value.Contains ('<')) {
					value = value.Replace (", ", ",");
				}

				// Split it into . separated parts and if one is ending with > rewrite it to `1 format
				// ILC folows the reflection format which doesn't actually use generic instantiations on anything but the last type
				// in nested hierarchy - it's difficult to replicate this with Cecil as it has different representation so just strip that info
				var parts = value.Split ('.');
				StringBuilder sb = new StringBuilder ();
				foreach (var part in parts) {
					if (sb.Length > 0)
						sb.Append ('.');

					if (part.EndsWith ('>')) {
						int i = part.LastIndexOf ('<');
						if (i >= 0) {
							sb.Append (part.AsSpan (0, i));
							sb.Append ('`');
							sb.Append (part.Substring (i + 1).Where (c => c == ',').Count () + 1);
							continue;
						}
					}

					sb.Append (part);
				}

				return sb.ToString ();
			}
		}

		private static bool HasAttribute (ICustomAttributeProvider caProvider, string attributeName)
		{
			if (caProvider is AssemblyDefinition assembly && assembly.EntryPoint != null)
				return assembly.EntryPoint.DeclaringType.CustomAttributes
					.Any (attr => attr.AttributeType.Name == attributeName);

			if (caProvider is TypeDefinition type)
				return type.CustomAttributes.Any (attr => attr.AttributeType.Name == attributeName);

			return false;
		}
	}
}
