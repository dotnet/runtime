// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
	ILLink.RoslynAnalyzer.DynamicallyAccessedMembersAnalyzer,
	ILLink.CodeFix.DynamicallyAccessedMembersCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class DynamicallyAccessedMembersCodeFixTests
	{
		static Task VerifyDynamicallyAccessedMembersCodeFix (
			string source,
			string fixedSource,
			DiagnosticResult[] baselineExpected,
			DiagnosticResult[] fixedExpected,
			int? numberOfIterations = null)
		{
			var test = new VerifyCS.Test {
				TestCode = source,
				FixedCode = fixedSource
			};
			test.ExpectedDiagnostics.AddRange (baselineExpected);
			test.TestState.AnalyzerConfigFiles.Add (
						("/.editorconfig", SourceText.From (@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableTrimAnalyzer} = true")));
			if (numberOfIterations != null) {
				test.NumberOfIncrementalIterations = numberOfIterations;
				test.NumberOfFixAllIterations = numberOfIterations;
			}
			test.FixedState.ExpectedDiagnostics.AddRange (fixedExpected);
			return test.RunAsync ();
		}

		[Fact]
		public async Task CodeFix_IL2067_MismatchParamTargetsParam ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				static void M(Type t) {
					M2(t);
				}

				static void M2([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t) {}
			}
			""";

			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				static void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t) {
					M2(t);
				}

				static void M2([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t) {}
			}
			""";

			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(7,3): warning IL2067: 't' argument does not satisfy 'DynamicallyAccessedMemberTypes.All' in call to 'C.M2(Type)'.
					// The parameter 't' of method 'C.M(Type)' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter)
						.WithSpan(7, 3, 7, 8)
						.WithSpan(6, 16, 6, 22)
						.WithArguments("t",
							"C.M2(Type)",
							"t",
							"C.M(Type)",
							"'DynamicallyAccessedMemberTypes.All'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2067_MismatchParamTargetsParam_WithReturn ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
				static string M(Type t) {
					M2(t);
					return "Foo";
				}

				static void M2([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) {}
			}
			""";

			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
				static string M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) {
					M2(t);
					return "Foo";
				}

				static void M2([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) {}
			}
			""";

			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(7,3): warning IL2067: 't' argument does not satisfy 'DynamicallyAccessedMemberTypes.All' in call to 'C.M2(Type)'.
					// The parameter 't' of method 'C.M(Type)' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter)
						.WithSpan(8, 3, 8, 8)
						.WithSpan(7, 18, 7, 24)
						.WithArguments("t",
							"C.M2(Type)",
							"t",
							"C.M(Type)",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2067_TwoAttributesTurnsOffDiagnostic ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				static void M(Type t) {
					M2(t);
				}

				static void M2([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields)] Type t) {}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(7,3): warning IL2067: 't' argument does not satisfy 'DynamicallyAccessedMemberTypes.All' in call to 'C.M2(Type)'.
				// The parameter 't' of method 'C.M(Type)' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter)
					.WithSpan(7, 3, 7, 8)
					.WithSpan(6, 16, 6, 22)
					.WithArguments("t",
						"C.M2(Type)",
						"t",
						"C.M(Type)",
						"'DynamicallyAccessedMemberTypes.PublicMethods', 'DynamicallyAccessedMemberTypes.PublicFields'")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2067_AttributeTurnsOffCodeFix ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
				static string M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {
					M2(t);
					return "Foo";
				}

				static void M2([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) {}
			}
			""";

			var diag = new[] {
				// /0/Test0.cs(7,3): warning IL2067: 't' argument does not satisfy 'DynamicallyAccessedMemberTypes.All' in call to 'C.M2(Type)'.
				// The parameter 't' of method 'C.M(Type)' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter)
					.WithSpan(8, 3, 8, 8)
					.WithArguments("t",
						"C.M2(Type)",
						"t",
						"C.M(Type)",
						"'DynamicallyAccessedMemberTypes.PublicMethods'")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2068_MismatchParamTargetsMethodReturn ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
				Type M(Type t) {
					return t;
				}
			}
			""";

			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
				Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t) {
					return t;
				}
			}
			""";

			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(8,10): warning IL2068: 'C.M(Type)' method return value does not satisfy 'DynamicallyAccessedMemberTypes.All' requirements. The parameter 't' of method 'C.M(Type)' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType)
					.WithSpan (8, 10, 8, 11)
					.WithSpan (7, 9, 7, 15)
					.WithArguments ("C.M(Type)",
							"t",
							"C.M(Type)",
							"'DynamicallyAccessedMemberTypes.All'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2068_ArgumentTurnsOffCodeFix_None ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
				Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.None)] Type t) {
					return t;
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(8,10): warning IL2068: 'C.M(Type)' method return value does not satisfy 'DynamicallyAccessedMemberTypes.All' requirements. The parameter 't' of method 'C.M(Type)' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType)
				.WithSpan (8, 10, 8, 11)
				.WithArguments ("C.M(Type)",
						"t",
						"C.M(Type)",
						"'DynamicallyAccessedMemberTypes.All'")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2069_MismatchParamTargetsField_PublicMethods ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					M(typeof(C));
				}

				private static void M(Type type)
				{
					f = type;
				}

				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				private static Type f = typeof(C);
			}
			""";

			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					M(typeof(C));
				}

				private static void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{
					f = type;
				}

				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				private static Type f = typeof(C);
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(13,3): warning IL2069: value stored in field 'C.f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements. 
					//The parameter 'type' of method 'C.M(Type)' does not have matching annotations. 
					//The source value must declare at least the same requirements as those declared on the target location it is assigned to.
						VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField)
						.WithSpan(13, 3, 13, 11)
						.WithSpan(11, 24, 11, 33)
						.WithArguments ("C.f",
							"type",
							"C.M(Type)",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2070_MismatchParamTargetsThisParam_PublicMethods ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					M(typeof(C));
				}
				static void M(Type t)
				{
					t.GetMethods();
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					M(typeof(C));
				}
				static void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
				{
					t.GetMethods();
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(12,3): warning IL2070: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods()'. The parameter 't' of method 'C.M(Type)' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter)
					.WithSpan(12, 3, 12, 17)
					.WithSpan(10, 16, 10, 22)
					.WithArguments("System.Type.GetMethods()",
						"t",
						"C.M(Type)",
						"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2070_NonPublicMethods ()
		{
			var test = $$"""
			using System;
			using System.Reflection;
			using System.Diagnostics.CodeAnalysis;
			
			class C
			{
				public static void Main()
				{
					M(typeof(C));
				}
				static void M(Type t)
				{
					t.GetMethods(BindingFlags.NonPublic);
				}
			}
			""";

			var fixtest = $$"""
			using System;
			using System.Reflection;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					M(typeof(C));
				}
				static void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t)
				{
					t.GetMethods(BindingFlags.NonPublic);
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(13,3): warning IL2070: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.NonPublicMethods' in call to 'System.Type.GetMethods(BindingFlags)'.
					// The parameter 't' of method 'C.M(Type)' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter)
						.WithSpan(13, 3, 13, 39)
						.WithSpan(11, 16, 11, 22)
						.WithArguments("System.Type.GetMethods(BindingFlags)",
							"t",
							"C.M(Type)",
							"'DynamicallyAccessedMemberTypes.NonPublicMethods'")
				},
				fixedExpected: new[] { 
					// /0/Test0.cs(9,3): warning IL2111: Method 'C.M(Type)' with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection.
					// Trimmer can't guarantee availability of the requirements of the method.
					VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection)
						.WithSpan (9, 3, 9, 15)
						.WithArguments ("C.M(Type)")
				});
		}

		[Fact]
		public async Task CodeFix_IL2070_GetMethodsInArg ()
		{
			var test = $$"""
			using System.Reflection;
			using System.Diagnostics.CodeAnalysis;

			namespace System
			{
				static class C 
				{
					static void Main(Type t)
					{
						DoSomethingWithMethods(t.GetMethods());
					}

					static void DoSomethingWithMethods(MethodInfo[] m)
					{
					}
				}
			}
			""";

			var fixtest = """
			using System.Reflection;
			using System.Diagnostics.CodeAnalysis;

			namespace System
			{
				static class C 
				{
					static void Main([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
					{
						DoSomethingWithMethods(t.GetMethods());
					}

					static void DoSomethingWithMethods(MethodInfo[] m)
					{
					}
				}
			}
			""";

			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(10,27): warning IL2070: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods()'.
					// The parameter 't' of method 'System.C.Main(Type)' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter)
					.WithSpan(10, 27, 10, 41)
					.WithSpan(8, 20, 8, 26)
					.WithArguments("System.Type.GetMethods()",
						"t",
						"System.C.Main(Type)",
						"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2072_MismatchMethodReturnTargetsParam ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					NeedsPublicMethodsOnParameter(GetC());
				}

				private static void NeedsPublicMethodsOnParameter(
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{
				}

				private static Type GetC()
				{
					return typeof(C);
				}
			}
			""";

			var fixtest = """
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					NeedsPublicMethodsOnParameter(GetC());
				}

				private static void NeedsPublicMethodsOnParameter(
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{
				}

			    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			    private static Type GetC()
				{
					return typeof(C);
				}
			}
			""";

			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(8,3): warning IL2072: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.NeedsPublicMethodsOnParameter(Type)'.
					// The return value of method 'C.GetT()' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter)
					.WithSpan(8, 3, 8, 40)
					.WithSpan(16, 2, 19, 3)
					.WithArguments("type",
						"C.NeedsPublicMethodsOnParameter(Type)",
						"C.GetC()",
						"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2072_MismatchMethodReturnTargetsParam_WithAttributes ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					NeedsPublicMethodsOnParameter(GetC(typeof(C)));
				}

				private static void NeedsPublicMethodsOnParameter(
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
				{
				}

				private static Type GetC([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
				{
					return t;
				}
			}
			""";

			var fixtest = """
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					NeedsPublicMethodsOnParameter(GetC(typeof(C)));
				}

				private static void NeedsPublicMethodsOnParameter(
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
				{
				}

			    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			    private static Type GetC([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
				{
					return t;
				}
			}
			""";

			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(8,3): warning IL2072: 't' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.NeedsPublicMethodsOnParameter(Type)'.
					// The return value of method 'C.GetC(Type)' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter)
						.WithSpan(8, 3, 8, 49)
						.WithSpan(16, 2, 19, 3)
						.WithArguments("t",
							"C.NeedsPublicMethodsOnParameter(Type)",
							"C.GetC(Type)",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2072_AttributeTurnsOffCodeFix_None ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					NeedsPublicMethodsOnParameter(GetC());
				}

				private static void NeedsPublicMethodsOnParameter(
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{
				}

				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.None)]
				private static Type GetC()
				{
					return typeof(C);
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(8,3): warning IL2072: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.NeedsPublicMethodsOnParameter(Type)'.
				// The return value of method 'C.GetT()' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter)
				.WithSpan(8, 3, 8, 40)
				.WithArguments("type",
					"C.NeedsPublicMethodsOnParameter(Type)",
					"C.GetC()",
					"'DynamicallyAccessedMemberTypes.PublicMethods'")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2072_AttributeTurnsOffCodeFix ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					NeedsPublicMethodsOnParameter(GetT());
				}

				private static void NeedsPublicMethodsOnParameter(
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{
				}

				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
				private static Type GetT()
				{
					return typeof(C);
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(8,3): warning IL2072: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.NeedsPublicMethodsOnParameter(Type)'.
				// The return value of method 'C.GetT()' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter)
				.WithSpan(8, 3, 8, 40)
				.WithArguments("type",
					"C.NeedsPublicMethodsOnParameter(Type)",
					"C.GetT()",
					"'DynamicallyAccessedMemberTypes.PublicMethods'")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2073_MismatchMethodReturnTargetsMethodReturn ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C {
				Type Main(Type t) {
					return t;
				}

				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				Type M() {
					return Main(typeof(C));
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C {
			    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			    Type Main([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) {
					return t;
				}

				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				Type M() {
					return Main(typeof(C));
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(11,10): warning IL2073: 'C.M()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements. 
					// The return value of method 'C.Main(Type)' does not have matching annotations. 
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType)
						.WithSpan(11, 10, 11, 25)
						.WithSpan(5, 2, 7, 3)
						.WithArguments("C.M()",
							"C.Main(Type)",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> (), 2);
		}

		[Fact]
		public async Task CodeFix_IL2073_MismatchMethodReturnTargetsMethodReturn_WithAttribute ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C {
				Type Main([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) {
					return t;
				}

				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) {
					return Main(t);
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C {
			    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			    Type Main([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) {
					return t;
				}

				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) {
					return Main(t);
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(11,10): warning IL2073: 'C.M(Type)' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
					// The return value of method 'C.Main(Type)' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType)
						.WithSpan(11, 10, 11, 17)
						.WithSpan(5, 2, 7, 3)
						.WithArguments("C.M(Type)",
							"C.Main(Type)",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2074_MismatchMethodReturnTargetsField ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					f = M();
				}

				private static Type M()
				{
					return typeof(C);
				}

				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				private static Type f;
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					f = M();
				}

			    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			    private static Type M()
				{
					return typeof(C);
				}

				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				private static Type f;
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(8,3): warning IL2074: value stored in field 'C.f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
					// The return value of method 'C.M()' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField)
						.WithSpan(8, 3, 8, 10)
						.WithSpan(11, 2, 14, 3)
						.WithArguments("C.f",
							"C.M()",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> (), 1);

		}

		[Fact]
		public async Task CodeFix_IL2075_MethodReturnTargetsParam_PublicMethods ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					GetC().GetMethod("Foo");
				}

				private static Type GetC ()
				{
					return typeof (C);
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;
			
			class C
			{
				public static void Main()
				{
					GetC().GetMethod("Foo");
				}
			
			    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			    private static Type GetC ()
				{
					return typeof (C);
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(8,3): warning IL2075: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
					//The return value of method 'C.GetFoo()' does not have matching annotations.
					//The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter)
						.WithSpan(8, 3, 8, 26)
						.WithSpan(11, 2, 14, 3)
						.WithArguments ("System.Type.GetMethod(String)",
							"C.GetC()",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2075_MethodAttributeLeavesOnCodeFix ()
		{
			var test = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
					public static void Main()
					{
						GetC().GetMethod("Foo");
					}

					private static Type GetC ()
					{
						return typeof(int);
					}
				}
			}
			""";
			var fixtest = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
					public static void Main()
					{
						GetC().GetMethod("Foo");
					}

			        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			        private static Type GetC ()
					{
						return typeof(int);
					}
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test),
				fixedSource: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), fixtest),
				baselineExpected: new[] {
					// /0/Test0.cs(8,3): warning IL2075: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
					//The return value of method 'C.GetFoo()' does not have matching annotations.
					//The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter)
						.WithSpan(194, 4, 194, 27)
						.WithSpan(197, 3, 200, 4)
						.WithArguments("System.Type.GetMethod(String)",
							"System.C.GetC()",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2075_MethodAttributeLeavesOnCodeFix_Reverse ()
		{
			var test = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						GetC().GetMethod("Foo");
					}

					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
					private static Type GetC ()
					{
						return typeof(int);
					}
				}
			}
			""";
			var fixtest = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						GetC().GetMethod("Foo");
					}

					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			        private static Type GetC ()
					{
						return typeof(int);
					}
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test),
				fixedSource: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), fixtest),
				baselineExpected: new[] {
					// /0/Test0.cs(8,3): warning IL2075: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
					//The return value of method 'C.GetFoo()' does not have matching annotations.
					//The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter)
						.WithSpan(193, 4, 193, 27)
						.WithSpan(196, 3, 200, 4)
						.WithArguments("System.Type.GetMethod(String)",
							"System.C.GetC()",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2075_ReutrnAttributeLeavesOnCodeFix ()
		{
			var test = $$$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
					public static string Main()
					{
						GetC().GetMethod("Foo");
						return "Foo";
					}

					private static Type GetC ()
					{
						return typeof(int);
					}
				}
			}
			""";
			var fixtest = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
					public static string Main()
					{
						GetC().GetMethod("Foo");
						return "Foo";
					}

			        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			        private static Type GetC ()
					{
						return typeof(int);
					}
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test),
				fixedSource: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), fixtest),
				baselineExpected: new[] {
					// /0/Test0.cs(8,3): warning IL2075: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
					//The return value of method 'C.GetFoo()' does not have matching annotations.
					//The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter)
						.WithSpan(194, 4, 194, 27)
						.WithSpan(198, 3, 201, 4)
						.WithArguments("System.Type.GetMethod(String)",
							"System.C.GetC()",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2077_MismatchFieldTargetsParam ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				private static Type f = typeof(C);

				public static void Main()
				{
					NeedsPublicMethods(f);
				}

				private static void NeedsPublicMethods(
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
			    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			    private static Type f = typeof(C);

			    public static void Main()
				{
					NeedsPublicMethods(f);
				}

				private static void NeedsPublicMethods(
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(10,3): warning IL2077: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.NeedsPublicMethods(Type)'.
					// The field 'C.f' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsParameter)
						.WithSpan(10, 3, 10, 24)
						.WithSpan(6, 22, 6, 35)
						.WithArguments("type",
							"C.NeedsPublicMethods(Type)",
							"C.f",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}


		[Fact]
		public async Task CodeFix_IL2077_AttributeTurnsOffCodeFix ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
				private static Type f = typeof(C);

				public static void Main()
				{
					NeedsPublicMethods(f);
				}

				private static void NeedsPublicMethods(
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(11,3): warning IL2077: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.NeedsPublicMethods(Type)'.
				// The field 'C.f' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsParameter)
					.WithSpan(11, 3, 11, 24)
					.WithArguments("type",
						"C.NeedsPublicMethods(Type)",
						"C.f",
						"'DynamicallyAccessedMemberTypes.PublicMethods'")};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}
		[Fact]
		public async Task CodeFix_IL2078_MismatchFieldTargetsMethodReturn ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				public static Type Main()
				{
					return f;
				}

				private static Type f;
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				public static Type Main()
				{
					return f;
				}

			    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			    private static Type f;
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(9,10): warning IL2078: 'C.Main()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
					// The field 'C.f' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType)
						.WithSpan(9, 10, 9, 11)
						.WithSpan(12, 22, 12, 23)
						.WithArguments("C.Main()",
							"C.f",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2078_AttributeTurnsOffCodeFix ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				public static Type Main()
				{
					return f;
				}

				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
				private static Type f;
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(9,10): warning IL2078: 'C.Main()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
				// The field 'C.f' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType)
					.WithSpan(9, 10, 9, 11)
					.WithArguments("C.Main()",
						"C.f",
						"'DynamicallyAccessedMemberTypes.PublicMethods'")};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2079_MismatchFieldTargetsField ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				private static Type f1 = typeof(C);

				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				private static Type f2 = typeof(C);

				public static void Main()
				{
					f2 = f1;
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
			    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			    private static Type f1 = typeof(C);
			    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				private static Type f2 = typeof(C);

				public static void Main()
				{
					f2 = f1;
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(13,3): warning IL2079: value stored in field 'C.f2' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
					// The field 'C.f1' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsField)
						.WithSpan(13, 3, 13, 10)
						.WithSpan(6, 22, 6, 36)
						.WithArguments("C.f2",
							"C.f1",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2079_AttributeTurnsOffCodeFix ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
				private static Type f1 = typeof(C);

				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				private static Type f2 = typeof(C);

				public static void Main()
				{
					f2 = f1;
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(14,3): warning IL2079: value stored in field 'C.f2' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
				// The field 'C.f1' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsField)
					.WithSpan(14, 3, 14, 10)
					.WithArguments("C.f2",
						"C.f1",
						"'DynamicallyAccessedMemberTypes.PublicMethods'")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2080_MismatchFieldTargetsPrivateParam_PublicMethods ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				private static Type f = typeof(C);

				public static void Main()
				{
					f.GetMethod("Foo");
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
			    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			    private static Type f = typeof(C);

			    public static void Main()
				{
					f.GetMethod("Foo");
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(10,3): warning IL2080: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
					// The field 'C.f' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter)
						.WithSpan(10, 3, 10, 21)
						.WithSpan(6, 22, 6, 35)
						.WithArguments("System.Type.GetMethod(String)",
							"C.f",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2080_MismatchFieldTargetsPublicParam_PublicMethods ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static Type f = typeof(C);

				public static void Main()
				{
					f.GetMethod("Foo");
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
			    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			    public static Type f = typeof(C);

			    public static void Main()
				{
					f.GetMethod("Foo");
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(10,3): warning IL2080: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
					// The field 'C.f' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter)
						.WithSpan(10, 3, 10, 21)
						.WithSpan(6, 21, 6, 34)
						.WithArguments("System.Type.GetMethod(String)",
							"C.f",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2080_AttributeTurnsOffCodeFix ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
				public static Type f = typeof(C);

				public static void Main()
				{
					f.GetMethod("Foo");
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(11,3): warning IL2080: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
				// The field 'C.f' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter)
					.WithSpan(11, 3, 11, 21)
					.WithArguments("System.Type.GetMethod(String)",
						"C.f",
						"'DynamicallyAccessedMemberTypes.PublicMethods'")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2082_MismatchThisParamTargetsParam ()
		{
			var test = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M1();
					}

					private void M1()
					{
						M2(this);
					}

					private static void M2([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
					{
					}
				}
			}
			""";

			var fixtest = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M1();
					}

			        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			        private void M1()
					{
						M2(this);
					}

					private static void M2([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
					{
					}
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test),
				fixedSource: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), fixtest),
				baselineExpected: new[] {
					// /0/Test0.cs(198,4): warning IL2082: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.C.M2(Type)'.
					// The implicit 'this' argument of method 'System.C.M1()' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsParameter)
						.WithSpan(198, 4, 198, 12)
						.WithSpan(196, 3, 199, 4)
						.WithArguments("t",
							"System.C.M2(Type)",
							"System.C.M1()",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(193,4): warning IL2065: Value passed to implicit 'this' parameter of method 'System.C.M1()' can not be statically determined
					// and may not meet 'DynamicallyAccessedMembersAttribute' requirements.
					VerifyCS.Diagnostic(DiagnosticId.ImplicitThisCannotBeStaticallyDetermined)
						.WithSpan(193, 4, 193, 16)
						.WithArguments("System.C.M1()")
				});
		}

		[Fact]
		public async Task CodeFix_IL2082_ReturnKeepsOnCodeFix ()
		{
			var test = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M1();
					}

					[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
					private string M1()
					{
						M2(this);
						return "Foo";
					}

					private static void M2([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
					{
					}
				}
			}
			""";

			var fixtest = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M1();
					}

					[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			        private string M1()
					{
						M2(this);
						return "Foo";
					}

					private static void M2([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
					{
					}
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test),
				fixedSource: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), fixtest),
				baselineExpected: new[] {
					// /0/Test0.cs(198,4): warning IL2082: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.C.M2(Type)'.
					// The implicit 'this' argument of method 'System.C.M1()' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsParameter)
						.WithSpan(199, 4, 199, 12)
						.WithSpan(196, 3, 201, 4)
						.WithArguments("t",
							"System.C.M2(Type)",
							"System.C.M1()",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(193,4): warning IL2065: Value passed to implicit 'this' parameter of method 'System.C.M1()' can not be statically determined
					// and may not meet 'DynamicallyAccessedMembersAttribute' requirements.
					VerifyCS.Diagnostic(DiagnosticId.ImplicitThisCannotBeStaticallyDetermined)
						.WithSpan(193, 4, 193, 16)
						.WithArguments("System.C.M1()")
				});
		}

		[Fact]
		public async Task CodeFix_IL2082_ParamAttributeKeepsOnCodeFix ()
		{
			var test = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M1("Foo");
					}

					[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
					private string M1([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] string s)
					{
						M2(this);
						return s;
					}

					private static void M2([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
					{
					}
				}
			}
			""";

			var fixtest = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M1("Foo");
					}

					[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
			        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			        private string M1([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] string s)
					{
						M2(this);
						return s;
					}

					private static void M2([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
					{
					}
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test),
				fixedSource: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), fixtest),
				baselineExpected: new[] {
					// /0/Test0.cs(198,4): warning IL2082: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.C.M2(Type)'.
					// The implicit 'this' argument of method 'System.C.M1()' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsParameter)
						.WithSpan(199, 4, 199, 12)
						.WithSpan(196, 3, 201, 4)
						.WithArguments("t",
							"System.C.M2(Type)",
							"System.C.M1(String)",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(193,4): warning IL2065: Value passed to implicit 'this' parameter of method 'System.C.M1()' can not be statically determined
					// and may not meet 'DynamicallyAccessedMembersAttribute' requirements.
					VerifyCS.Diagnostic(DiagnosticId.ImplicitThisCannotBeStaticallyDetermined)
						.WithSpan(193, 4, 193, 21)
						.WithArguments("System.C.M1(String)")
				});
		}

		[Fact]
		public async Task CodeFix_IL2082_AttributeTurnsOffCodeFix ()
		{
			var test = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M1();
					}

					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
					private void M1()
					{
						M2(this);
					}

					private static void M2([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
					{
					}
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(199,4): warning IL2082: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.C.M2(Type)'.
				// The implicit 'this' argument of method 'System.C.M1()' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsParameter)
					.WithSpan(199, 4, 199, 12)
					.WithArguments("t",
						"System.C.M2(Type)",
						"System.C.M1()",
						"'DynamicallyAccessedMemberTypes.PublicMethods'"),
				// /0/Test0.cs(193,4): warning IL2065: Value passed to implicit 'this' parameter of method 'System.C.M1()' can not be statically determined
				// and may not meet 'DynamicallyAccessedMembersAttribute' requirements.
				VerifyCS.Diagnostic(DiagnosticId.ImplicitThisCannotBeStaticallyDetermined)
					.WithSpan(193, 4, 193, 16)
					.WithArguments("System.C.M1()")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test),
				string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test), diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2083_MismatchThisParamTargetsMethodReturn ()
		{
			var test = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M1();
					}

					[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
					private Type M1()
					{
						return this;
					}
				}
			}
			""";

			var fixtest = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M1();
					}

					[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			        private Type M1()
					{
						return this;
					}
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test),
				fixedSource: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), fixtest),
				baselineExpected: new[] {
					// /0/Test0.cs(199,11): warning IL2083: 'System.C.M1()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements. The implicit 'this' argument of method 'System.C.M1()' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsMethodReturnType)
						.WithSpan(199, 11, 199, 15)
						.WithSpan(196, 3, 200, 4)
						.WithArguments("System.C.M1()",
							"System.C.M1()",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(193,4): warning IL2065: Value passed to implicit 'this' parameter of method 'System.C.M1()' can not be statically determined
					// and may not meet 'DynamicallyAccessedMembersAttribute' requirements.
					VerifyCS.Diagnostic(DiagnosticId.ImplicitThisCannotBeStaticallyDetermined)
						.WithSpan(193, 4, 193, 16)
						.WithArguments("System.C.M1()")
				});
		}

		[Fact]
		public async Task CodeFix_IL2083_ParamAttributeKeepsCodeFix ()
		{
			var test = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M1("Foo");
					}

					[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
					private Type M1([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] string s)
					{
						s.AsSpan();
						return this;
					}
				}
			}
			""";

			var fixtest = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M1("Foo");
					}

					[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			        private Type M1([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] string s)
					{
						s.AsSpan();
						return this;
					}
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test),
				fixedSource: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), fixtest),
				baselineExpected: new[] {
					// /0/Test0.cs(199,11): warning IL2083: 'System.C.M1()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements. The implicit 'this' argument of method 'System.C.M1()' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsMethodReturnType)
						.WithSpan(200, 11, 200, 15)
						.WithSpan(196, 3, 201, 4)
						.WithArguments("System.C.M1(String)",
							"System.C.M1(String)",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(193,4): warning IL2065: Value passed to implicit 'this' parameter of method 'System.C.M1()' can not be statically determined
					// and may not meet 'DynamicallyAccessedMembersAttribute' requirements.
					VerifyCS.Diagnostic(DiagnosticId.ImplicitThisCannotBeStaticallyDetermined)
						.WithSpan(193, 4, 193, 21)
						.WithArguments("System.C.M1(String)")
				});
		}

		[Fact]
		public async Task CodeFix_IL2083_AttributeTurnsOffCodeFix ()
		{
			var test = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M1();
					}

					[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
					private Type M1()
					{
						return this;
					}
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(200,11): warning IL2083: 'System.C.M1()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements. The implicit 'this' argument of method 'System.C.M1()' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsMethodReturnType)
					.WithSpan(200, 11, 200, 15)
					.WithArguments("System.C.M1()",
						"System.C.M1()",
						"'DynamicallyAccessedMemberTypes.PublicMethods'"),
				// /0/Test0.cs(193,4): warning IL2065: Value passed to implicit 'this' parameter of method 'System.C.M1()' can not be statically determined
				// and may not meet 'DynamicallyAccessedMembersAttribute' requirements.
				VerifyCS.Diagnostic(DiagnosticId.ImplicitThisCannotBeStaticallyDetermined)
					.WithSpan(193, 4, 193, 16)
					.WithArguments("System.C.M1()")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test),
				string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test), diag, diag);
		}


		[Fact]
		public async Task CodeFix_IL2084_MismatchThisParamTargetsField ()
		{
			var test = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M();
					}
			
					private void M()
					{
						f = this;
					}
			
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
					private static Type f;
				}
			}
			""";

			var fixtest = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M();
					}

			        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			        private void M()
					{
						f = this;
					}

					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
					private static Type f;
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test),
				fixedSource: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), fixtest),
				baselineExpected: new[] {
					// /0/Test0.cs(198,4): warning IL2084: value stored in field 'System.C.f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
					// The implicit 'this' argument of method 'System.C.M()' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsField)
						.WithSpan(198, 4, 198, 12)
						.WithSpan(196, 3, 199, 4)
						.WithArguments("System.C.f",
							"System.C.M()",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(193,4): warning IL2065: Value passed to implicit 'this' parameter of method 'System.C.M()' can not be statically determined
					// and may not meet 'DynamicallyAccessedMembersAttribute' requirements.
					VerifyCS.Diagnostic(DiagnosticId.ImplicitThisCannotBeStaticallyDetermined)
						.WithSpan(193, 4, 193, 15)
						.WithArguments("System.C.M()")
				});
		}

		[Fact]
		public async Task CodeFix_IL2085_MismatchThisParamTargetsThisParam ()
		{
			var test = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M1();
					}

					private void M1()
					{
						this.M2();
					}

					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
					private void M2()
					{
					}
				}
			}
			""";

			var fixtest = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M1();
					}

			        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			        private void M1()
					{
						this.M2();
					}

					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
					private void M2()
					{
					}
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test),
				fixedSource: string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), fixtest),
				baselineExpected: new[] {
					// /0/Test0.cs(198,4): warning IL2085: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.C.M2()'.
					// The implicit 'this' argument of method 'System.C.M1()' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsThisParameter)
						.WithSpan(198, 4, 198, 13)
						.WithSpan(196, 3, 199, 4)
						.WithArguments("System.C.M2()",
							"System.C.M1()",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(193,4): warning IL2065: Value passed to implicit 'this' parameter of method 'System.C.M1()' can not be statically determined
					// and may not meet 'DynamicallyAccessedMembersAttribute' requirements.
					VerifyCS.Diagnostic(DiagnosticId.ImplicitThisCannotBeStaticallyDetermined)
						.WithSpan(193, 4, 193, 16)
						.WithArguments("System.C.M1()")
				});
		}

		[Fact]
		public async Task CodeFix_IL2085_AttributeTurnsOffCodeFix ()
		{
			var test = $$"""
			namespace System
			{
				class C : TestSystemTypeBase
				{
					public static void Main()
					{
						new C().M1();
					}

					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
					private void M1()
					{
						this.M2();
					}

					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
					private void M2()
					{
					}
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(199,4): warning IL2085: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.C.M2()'.
				// The implicit 'this' argument of method 'System.C.M1()' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsThisParameter)
					.WithSpan(199, 4, 199, 13)
					.WithArguments("System.C.M2()",
						"System.C.M1()",
						"'DynamicallyAccessedMemberTypes.PublicMethods'"),
				// /0/Test0.cs(193,4): warning IL2065: Value passed to implicit 'this' parameter of method 'System.C.M1()' can not be statically determined
				// and may not meet 'DynamicallyAccessedMembersAttribute' requirements.
				VerifyCS.Diagnostic(DiagnosticId.ImplicitThisCannotBeStaticallyDetermined)
					.WithSpan(193, 4, 193, 16)
					.WithArguments("System.C.M1()")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test),
				string.Concat (DynamicallyAccessedMembersAnalyzerTests.GetSystemTypeBase (), test), diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2087_MismatchTypeArgumentTargetsParameter ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					M2<int>();
				}

				private static void M1(
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{
				}

				private static void M2<T>()
				{
					M1(typeof(T));
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					M2<int>();
				}

				private static void M1(
					[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{
				}

				private static void M2<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>()
				{
					M1(typeof(T));
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(18,3): warning IL2087: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.M1(Type)'.
					// The generic parameter 'T' of 'C.M2<T>()' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter)
						.WithSpan(18, 3, 18, 16)
						.WithSpan(16, 25, 16, 26)
						.WithArguments("type",
							"C.M1(Type)",
							"T",
							"C.M2<T>()",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2088_MismatchTypeArgumentTargetsMethodReturnType ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;
			
			class C
			{
				public static void Main()
				{
					M<int>();
				}
			
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
				private static Type M<T>()
				{
					return typeof(T);
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					M<int>();
				}

				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
				private static Type M<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
				{
					return typeof(T);
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(14,10): warning IL2088: 'C.M<T>()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicConstructors' requirements.
					// The generic parameter 'T' of 'C.M<T>()' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType)
						.WithSpan(14, 10, 14, 19)
						.WithSpan(12, 24, 12, 25)
						.WithArguments("C.M<T>()",
							"T",
							"C.M<T>()",
							"'DynamicallyAccessedMemberTypes.PublicConstructors'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2088_AttributeTurnsOffCodeFix ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					M<int>();
				}

				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
				private static Type M<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>()
				{
					return typeof(T);
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(14,10): warning IL2088: 'C.M<T>()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicConstructors' requirements.
				// The generic parameter 'T' of 'C.M<T>()' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType)
					.WithSpan(14, 10, 14, 19)
					.WithArguments("C.M<T>()",
						"T",
						"C.M<T>()",
						"'DynamicallyAccessedMemberTypes.PublicConstructors'")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2089_MismatchTypeArgumentTargetsField ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main<T>()
				{
					f = typeof(T);
				}

				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				private static Type f;
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>()
				{
					f = typeof(T);
				}

				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				private static Type f;
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(8,3): warning IL2089: value stored in field 'C.f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements. The generic parameter 'T' of 'C.Main<T>()' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsField)
						.WithSpan(8, 3, 8, 16)
						.WithSpan(6, 26, 6, 27)
						.WithArguments("C.f",
							"T",
							"C.Main<T>()",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2089_AttributeTurnsOffCodeFix ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] T>()
				{
					f = typeof(T);
				}

				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				private static Type f;
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(8,3): warning IL2089: value stored in field 'C.f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements. The generic parameter 'T' of 'C.Main<T>()' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsField)
					.WithSpan(8, 3, 8, 16)
					.WithArguments("C.f",
						"T",
						"C.Main<T>()",
						"'DynamicallyAccessedMemberTypes.PublicMethods'")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}


		[Fact]
		public async Task CodeFix_IL2090_MismatchTypeArgumentTargetsThisParameter ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;
			
			class C<T> {

				void M() 
				{
					typeof(T).GetMethods();
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;
			
			class C<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T> {

				void M() 
				{
					typeof(T).GetMethods();
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(8,3): warning IL2090: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods()'.
					// The generic parameter 'T' of 'C<T>' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsThisParameter)
						.WithSpan(8, 3, 8, 25)
						.WithSpan(4, 9, 4, 10)
						.WithArguments("System.Type.GetMethods()",
							"T",
							"C<T>",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2090_AttributeTurnsOffCodeFix ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T> {

				void M() 
				{
					typeof(T).GetMethods();
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(8,3): warning IL2090: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods()'.
				// The generic parameter 'T' of 'C<T>' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsThisParameter)
					.WithSpan(8, 3, 8, 25)
					.WithArguments("System.Type.GetMethods()",
						"T",
						"C<T>",
						"'DynamicallyAccessedMemberTypes.PublicMethods'")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2090_AttributeTurnsOffCodeFix_None ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.None)] T> {

				void M() 
				{
					typeof(T).GetMethods();
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(8,3): warning IL2090: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods()'.
				// The generic parameter 'T' of 'C<T>' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsThisParameter)
					.WithSpan(8, 3, 8, 25)
					.WithArguments("System.Type.GetMethods()",
						"T",
						"C<T>",
						"'DynamicallyAccessedMemberTypes.PublicMethods'")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2091_MismatchTypeTargetsGenericParameter ()
		{
			var test = $$"""
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					M2<int>();
				}

				private static void M1<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>()
				{
				}

				private static void M2<S>()
				{
					M1<S>();
				}
			}
			""";
			var fixtest = $$"""
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					M2<int>();
				}

				private static void M1<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>()
				{
				}

				private static void M2<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] S>()
				{
					M1<S>();
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(16,3): warning IL2091: 'T' generic argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in 'C.M1<T>()'.
					// The generic parameter 'S' of 'C.M2<S>()' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter)
						.WithSpan(16, 3, 16, 10)
						.WithSpan(14, 25, 14, 26)
						.WithArguments("T",
							"C.M1<T>()",
							"S",
							"C.M2<S>()",
							"'DynamicallyAccessedMemberTypes.PublicMethods'")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2091_AttributeTurnsOffCodeFix ()
		{
			var test = $$"""
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					M2<int>();
				}

				private static void M1<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>()
				{
				}

				private static void M2<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.None)] S>()
				{
					M1<S>();
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(16,3): warning IL2091: 'T' generic argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in 'C.M1<T>()'.
				// The generic parameter 'S' of 'C.M2<S>()' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter)
					.WithSpan(16, 3, 16, 10)
					.WithArguments("T",
						"C.M1<T>()",
						"S",
						"C.M2<S>()",
						"'DynamicallyAccessedMemberTypes.PublicMethods'")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2092_MismatchMethodParamBtOverride_NonPublicMethods ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class Base
			{
				public virtual void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}
			}

			public class C : Base
			{
				public override void M(Type t) {}

				public static void Main() {
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class Base
			{
				public virtual void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}
			}

			public class C : Base
			{
				public override void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}

				public static void Main() {
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new[] {
				// /0/Test0.cs(11,30): warning IL2092: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the parameter 't' of method 'C.M(Type)'
				// don't match overridden parameter 't' of method 'Base.M(Type)'.
				// All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides)
				.WithSpan(11, 30, 11, 31)
				.WithSpan(11, 30, 11, 31)
				.WithArguments("t",
					"C.M(Type)",
					"t",
					"Base.M(Type)") },
				fixedExpected: Array.Empty<DiagnosticResult> (), 1);
		}

		[Fact]
		public async Task CodeFix_IL2092_MismatchMethodParamBtOverride_NonPublicMethods_Reverse ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class Base
			{
				public virtual void M(Type t) {}
			}

			public class C : Base
			{
				public override void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}

				public static void Main() {

				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class Base
			{
				public virtual void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}
			}

			public class C : Base
			{
				public override void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}

				public static void Main() {

				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(11,108): warning IL2092: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the parameter 't' of method 'C.M(Type)'
					// don't match overridden parameter 't' of method 'Base.M(Type)'.
					// All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides)
					.WithSpan(11, 108, 11, 109)
					.WithSpan(6, 29, 6, 30)
					.WithArguments("t",
						"C.M(Type)",
						"t",
						"Base.M(Type)")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2092_BothAttributesTurnOffCodeFix ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class Base
			{
				public virtual void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) {}
			}

			public class C : Base
			{
				public override void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}

				public static void Main() {

				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(11,108): warning IL2092: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the parameter 't' of method 'C.M(Type)'
				// don't match overridden parameter 't' of method 'Base.M(Type)'.
				// All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides)
				.WithSpan(11, 108, 11, 109)
				.WithArguments("t",
					"C.M(Type)",
					"t",
					"Base.M(Type)")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2092_TwoAttributesTurnOffCodeFix ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class Base
			{
				public virtual void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields)] Type t) {}
			}

			public class C : Base
			{
				public override void M(Type t) {}

				public static void Main() {

				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(11,108): warning IL2092: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the parameter 't' of method 'C.M(Type)'
				// don't match overridden parameter 't' of method 'Base.M(Type)'.
				// All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides)
				.WithSpan(11, 30, 11, 31)
				.WithSpan(11, 30, 11, 31)
				.WithArguments("t",
					"C.M(Type)",
					"t",
					"Base.M(Type)")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2092_BothAttributesTurnOffCodeFix_None ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class Base
			{
				public virtual void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.None)] Type t) {}
			}

			public class C : Base
			{
				public override void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}

				public static void Main() {

				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(11,108): warning IL2092: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the parameter 't' of method 'C.M(Type)'
				// don't match overridden parameter 't' of method 'Base.M(Type)'.
				// All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides)
				.WithSpan(11, 108, 11, 109)
				.WithArguments("t",
					"C.M(Type)",
					"t",
					"Base.M(Type)")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2093_MismatchOnMethodReturnValueBetweenOverrides ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class Base
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
				public virtual Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {
					return t;
				}
			}

			public class C : Base
			{
				public override Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {
					return t;
				}

				public static void Main() {
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class Base
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
				public virtual Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {
					return t;
				}
			}

			public class C : Base
			{
			    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
			    public override Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {
					return t;
				}

				public static void Main() {
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(14,23): warning IL2093: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the return value of method 'C.M(Type)'
					// don't match overridden return value of method 'Base.M(Type)'.
					// All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides)
						.WithSpan(14, 23, 14, 24)
						.WithSpan(14, 23, 14, 24)
						.WithArguments("C.M(Type)",
							"Base.M(Type)")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2093_MismatchOnMethodReturnValueBetweenOverrides_Reversed ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class Base
			{
				public virtual Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {
					return t;
				}
			}

			public class C : Base
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
				public override Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {
					return t;
				}

				public static void Main() {
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class Base
			{
			    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
			    public virtual Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {
					return t;
				}
			}

			public class C : Base
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
				public override Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {
					return t;
				}

				public static void Main() {
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(14,23): warning IL2093: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the return value of method 'C.M(Type)'
					// don't match overridden return value of method 'Base.M(Type)'.
					// All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides)
						.WithSpan(14, 23, 14, 24)
						.WithSpan(6, 22, 6, 23)
						.WithArguments("C.M(Type)",
						"Base.M(Type)")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2093_BothAttributesTurnOffCodeFix ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class Base
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				public virtual Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {
					return t;
				}
			}

			public class C : Base
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
				public override Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {
					return t;
				}

				public static void Main() {
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(15,23): warning IL2093: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the return value of method 'C.M(Type)'
				// don't match overridden return value of method 'Base.M(Type)'.
				// All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides)
					.WithSpan(15, 23, 15, 24)
					.WithArguments("C.M(Type)",
					"Base.M(Type)")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2093_AttributesTurnOffCodeFix_None ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class Base
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.None)]
				public virtual Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {
					return t;
				}
			}

			public class C : Base
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
				public override Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {
					return t;
				}

				public static void Main() {
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(15,23): warning IL2093: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the return value of method 'C.M(Type)'
				// don't match overridden return value of method 'Base.M(Type)'.
				// All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides)
					.WithSpan(15, 23, 15, 24)
					.WithArguments("C.M(Type)",
					"Base.M(Type)")
			};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}
	}
}
