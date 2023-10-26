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
			// Analyzer hole: https://github.com/dotnet/runtime/issues/94057
			[ExpectedWarning ("IL2026", "Invoking members on dynamic types is not trimming-compatible.", ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL2026", "Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)]
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

			// Analyzer hole: https://github.com/dotnet/runtime/issues/94057
			[ExpectedWarning ("IL2026", "Invoking members on dynamic types is not trimming-compatible.", ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL2026", "Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)]
			static void MethodWithDynamicParameter (dynamic arg)
			{
				arg.MethodWithDynamicParameter (arg);
			}

			[ExpectedWarning ("IL2026", "Microsoft.CSharp.RuntimeBinder.Binder.InvokeConstructor", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)]
			// TODO: analyzer hole!
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
			// Analyzer hole: https://github.com/dotnet/runtime/issues/94057
			[ExpectedWarning ("IL2026", "Microsoft.CSharp.RuntimeBinder.Binder.GetMember", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)]
			static void Read (dynamic d)
			{
				var x = d.Member;
			}

			// Analyzer hole: https://github.com/dotnet/runtime/issues/94057
			[ExpectedWarning ("IL2026", "Microsoft.CSharp.RuntimeBinder.Binder.SetMember", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)]
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
			// Analyzer hole: https://github.com/dotnet/runtime/issues/94057
			[ExpectedWarning ("IL2026", "Microsoft.CSharp.RuntimeBinder.Binder.GetIndex", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)]
			static void Read (dynamic d)
			{
				var x = d[0];
			}

			// Analyzer hole: https://github.com/dotnet/runtime/issues/94057
			[ExpectedWarning ("IL2026", "Microsoft.CSharp.RuntimeBinder.Binder.SetIndex", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)]
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
				// Analyzer hole: https://github.com/dotnet/runtime/issues/94057
				[ExpectedWarning ("IL3050", ProducedBy = Tool.NativeAot)]
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
			// Analyzer hole: https://github.com/dotnet/runtime/issues/94057
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
