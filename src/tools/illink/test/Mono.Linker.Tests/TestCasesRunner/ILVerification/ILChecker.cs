// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILVerify;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner.ILVerification;

public class ILChecker
{
	public virtual void Check (TrimmedTestCaseResult linkResult, AssemblyDefinition original)
	{
		ProcessSkipAttributes (linkResult, original, out bool skipCheckEntirely, out HashSet<string> assembliesToSkip);

		if (skipCheckEntirely)
			return;

		using var outputVerifier = CreateILVerifier (linkResult.OutputAssemblyPath.Parent);
		using var inputVerifier = CreateILVerifier (linkResult.InputAssemblyPath.Parent);

		var failureMessages = new StringBuilder ();

		foreach (var file in linkResult.OutputAssemblyPath.Parent.Files ()) {
			if (assembliesToSkip.Contains (file.FileName))
				continue;

			if (!ShouldCheckAssembly (file))
				continue;

			var outputResults = outputVerifier.Verify (file);

			if (outputResults.Length == 0)
				continue;

			if (!DisableILDiffing (linkResult, original)) {
				var inputResults = inputVerifier.VerifyByName (file.FileNameWithoutExtension);

				outputResults = Diff (outputResults, inputResults);

				if (outputResults.Length == 0)
					continue;
			}

			failureMessages.AppendLine ($"IL Verification failed for {file.FileName}:");
			failureMessages.AppendLine ("-----------------------------");

			foreach (var result in outputResults) {
				failureMessages.AppendLine (result.GetErrorMessage ());
			}

			failureMessages.AppendLine ("-----------------------------");
		}

		var failures = failureMessages.ToString ();
		var hasFailures = !string.IsNullOrEmpty (failures);

		ProcessExpectILFailures (linkResult, original, out bool expectILFailures, out List<string> expectedFailureMessages);

		if (expectILFailures) {
			if (expectedFailureMessages.Count == 0 && !hasFailures)
				Assert.Fail ("Expected IL failures, but there were none");

			foreach (var expectedFailureMessage in expectedFailureMessages) {
				if (!failures.Contains (expectedFailureMessage)) {
					Assert.Fail ($"Expected IL failure message '{expectedFailureMessage}' was not found in the actual IL failure messages:\n--------------------\n{failures}");
				}
			}
		} else {
			if (hasFailures)
				Assert.Fail (failures);
		}
	}

	private static bool DisableILDiffing (TrimmedTestCaseResult linkResult, AssemblyDefinition original)
	{
		return linkResult.TestCase.FindTypeDefinition (original)
			.CustomAttributes
			.FirstOrDefault (attr => attr.AttributeType.Name == nameof(DisableILVerifyDiffingAttribute)) != null;
	}

	private static void ProcessExpectILFailures (TrimmedTestCaseResult linkResult, AssemblyDefinition original, out bool expectILFailures, out List<string> failureMessages)
	{
		var attrs = linkResult.TestCase.FindTypeDefinition (original).CustomAttributes.Where (attr => attr.AttributeType.Name == nameof(ExpectILFailureAttribute)).ToArray ();
		expectILFailures = attrs.Length > 0;
		failureMessages = new List<string> ();
		foreach (var attr in attrs) {
			var expectedMessageContains = ((CustomAttributeArgument[]) attr.GetConstructorArgumentValue (0)).Select (a => (string) a.Value).ToArray ();
			failureMessages.AddRange (expectedMessageContains);
		}
	}

	private void ProcessSkipAttributes (TrimmedTestCaseResult linkResult, AssemblyDefinition original, out bool skipCheckEntirely, out HashSet<string> assembliesToSkip)
	{
		var attrs = linkResult.TestCase.FindTypeDefinition (original).CustomAttributes.Where (attr => attr.AttributeType.Name == nameof(SkipILVerifyAttribute));
		skipCheckEntirely = false;
		assembliesToSkip = new HashSet<string> ();
		foreach (var attr in attrs) {
			var ctorArg = attr.ConstructorArguments.FirstOrDefault ();

			if (!attr.HasConstructorArguments) {
				skipCheckEntirely = true;
			} else if (ctorArg.Type.Name == nameof(String)) {
				assembliesToSkip.Add ((string) ctorArg.Value);
			} else {
				throw new ArgumentException ($"Unhandled constructor argument type of {ctorArg.Type} on {nameof(SkipILVerifyAttribute)}");
			}
		}
	}

	public static ILVerifierResult[] Diff (ILVerifierResult[] outputResults, ILVerifierResult[] inputResults)
	{
		var inputErrors = new HashSet<ErrorKey> ();

		foreach (var inputResult in inputResults) {
			inputErrors.Add (CreateKey (inputResult));
		}

		var newErrors = new List<ILVerifierResult> ();

		foreach (var outputResult in outputResults) {
			var key = CreateKey (outputResult);

			// The error existed in the input assembly, filter it out
			if (inputErrors.Contains (key))
				continue;

			newErrors.Add (outputResult);
		}

		return newErrors.ToArray ();
	}

	static ErrorKey CreateKey (ILVerifierResult result)
	{
		return new ErrorKey (result.Result.Code,
			result.TypeFullName,
			result.MethodSignature,
			result.Result.ErrorArguments
				.Aggregate (string.Empty, (accum, error) => $"{accum}, {KeyForArgument (error)}"));

		static string KeyForArgument (ErrorArgument argument)
		{
			// Technically, tokens and offsets could change.  If they did this would mess up the diffing.
			// So far I haven't noticed this happening.  I'm not sure what to do about this case so I'm not going to do anything for the time being
			// we can revisit if it becomes a problem.
			return $"{argument.Name} : {argument.Value}";
		}
	}

	protected virtual ILVerifier CreateILVerifier (NPath directory)
	{
		return new ILVerifier (new []
		{
			directory,
			typeof (object).Assembly.Location.ToNPath ().Parent
		},"System.Private.CoreLib");
	}


	protected bool ShouldCheckAssembly (NPath file) => file.ExtensionWithDot == ".exe" || file.ExtensionWithDot == ".dll" || file.ExtensionWithDot == ".winmd";

	public record class ErrorKey(VerifierError Code, string TypeFullName, string MethodName, string ErrorArguments);
}
