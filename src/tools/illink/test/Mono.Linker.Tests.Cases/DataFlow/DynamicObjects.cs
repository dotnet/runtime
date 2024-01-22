// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	[Reference ("Microsoft.CSharp.dll")]
	public class DynamicObjects
	{
		// Note on discrepancies between analyzer and NativeAot:
		// Analyzer doesn't produce RequiresDynamicCode warnings for dynamic invocations.
		// Tracked by https://github.com/dotnet/runtime/issues/94427.
		public static void Main ()
		{
			InvocationOnDynamicType.Test ();
			DynamicMemberReference.Test ();
			DynamicIndexerAccess.Test ();
			DynamicInRequiresUnreferencedCodeClass.Test ();
			InvocationOnDynamicTypeInMethodWithRUCDoesNotWarnTwoTimes.Test ();
		}

		class InvocationOnDynamicType
		{
			[ExpectedWarning ("IL2026", "Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember")]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)] // https://github.com/dotnet/runtime/issues/94427
			static void DynamicArgument ()
			{
				dynamic dynamicObject = "Some string";
				Console.WriteLine (dynamicObject);
			}

			static void DynamicParameter ()
			{
				MethodWithDynamicParameterDoNothing (0);
				MethodWithDynamicParameterDoNothing ("Some string");
				MethodWithDynamicParameter(-1);
			}

			static void MethodWithDynamicParameterDoNothing (dynamic arg)
			{
			}

			[ExpectedWarning ("IL2026", "Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember")]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)] // https://github.com/dotnet/runtime/issues/94427
			static void MethodWithDynamicParameter (dynamic arg)
			{
				arg.MethodWithDynamicParameter (arg);
			}

			[ExpectedWarning ("IL2026", "Microsoft.CSharp.RuntimeBinder.Binder.InvokeConstructor")]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)] // https://github.com/dotnet/runtime/issues/94427
			static void ObjectCreationDynamicArgument ()
			{
				dynamic dynamicObject = "Some string";
				var x = new ClassWithDynamicCtor (dynamicObject);
			}

			class ClassWithDynamicCtor
			{
				public ClassWithDynamicCtor (dynamic arg)
				{
				}
			}

			public static void Test ()
			{
				DynamicArgument ();
				DynamicParameter ();
				ObjectCreationDynamicArgument ();
			}
		}

		class DynamicMemberReference
		{
			[ExpectedWarning ("IL2026", "Microsoft.CSharp.RuntimeBinder.Binder.GetMember")]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)] // https://github.com/dotnet/runtime/issues/94427
			static void Read (dynamic d)
			{
				var x = d.Member;
			}

			[ExpectedWarning ("IL2026", "Microsoft.CSharp.RuntimeBinder.Binder.SetMember")]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)] // https://github.com/dotnet/runtime/issues/94427
			static void Write (dynamic d)
			{
				d.Member = 0;
			}

			public static void Test ()
			{
				Read (null);
				Write (null);
			}
		}

		class DynamicIndexerAccess
		{
			[ExpectedWarning ("IL2026", "Microsoft.CSharp.RuntimeBinder.Binder.GetIndex")]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)] // https://github.com/dotnet/runtime/issues/94427
			static void Read (dynamic d)
			{
				var x = d[0];
			}

			[ExpectedWarning ("IL2026", "Microsoft.CSharp.RuntimeBinder.Binder.SetIndex")]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)] // https://github.com/dotnet/runtime/issues/94427
			static void Write (dynamic d)
			{
				d[0] = 0;
			}

			public static void Test ()
			{
				Read (null);
				Write (null);
			}
		}

		class DynamicInRequiresUnreferencedCodeClass
		{
			[RequiresUnreferencedCode("message")]
			class ClassWithRequires
			{
				[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)] // https://github.com/dotnet/runtime/issues/94427
				public static void MethodWithDynamicArg (dynamic arg)
				{
					arg.DynamicInvocation ();
				}
			}

			[ExpectedWarning ("IL2026", nameof (ClassWithRequires))]
			public static void Test ()
			{
				ClassWithRequires.MethodWithDynamicArg (null);
			}
		}

		class InvocationOnDynamicTypeInMethodWithRUCDoesNotWarnTwoTimes ()
		{
			[RequiresUnreferencedCode ("We should only see the warning related to this annotation, and none about the dynamic type.")]
			[RequiresDynamicCode ("We should only see the warning related to this annotation, and none about the dynamic type.")]
			static void MethodWithRequires ()
			{
				dynamic dynamicField = "Some string";
				Console.WriteLine (dynamicField);
			}

			[ExpectedWarning ("IL2026", nameof (MethodWithRequires))]
			[ExpectedWarning ("IL3050", nameof (MethodWithRequires), ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			public static void Test ()
			{
				MethodWithRequires ();
			}
		}
	}
}
