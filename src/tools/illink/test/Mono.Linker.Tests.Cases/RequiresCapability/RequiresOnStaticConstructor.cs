// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class RequiresOnStaticConstructor
	{
		public static void Main ()
		{
			TestStaticCctorRequires ();
			TestStaticCtorMarkingIsTriggeredByFieldAccess ();
			TestStaticCtorMarkingIsTriggeredByFieldAccessOnExplicitLayout ();
			TestStaticCtorTriggeredByMethodCall ();
			TestTypeIsBeforeFieldInit ();
			TestStaticCtorOnTypeWithRequires ();
			TestRunClassConstructorOnTypeWithRequires ();
			TestRunClassConstructorOnConstructorWithRequires ();
			typeof (StaticCtor).RequiresNonPublicConstructors ();
		}

		class StaticCtor
		{
			[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL2116", "StaticCtor..cctor()")]
			[RequiresUnreferencedCode ("Message for --TestStaticCtor--")]
			static StaticCtor ()
			{
				MethodWithRequires ();
			}
		}

		static void TestStaticCctorRequires ()
		{
			_ = new StaticCtor ();
		}

		[RequiresUnreferencedCode ("Message for --StaticCtorOnTypeWithRequires--")]
		class StaticCtorOnTypeWithRequires
		{
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static StaticCtorOnTypeWithRequires () => MethodWithRequires ();
		}

		[ExpectedWarning ("IL2026", "Message for --StaticCtorOnTypeWithRequires--")]
		static void TestStaticCtorOnTypeWithRequires ()
		{
			var cctor = typeof (StaticCtorOnTypeWithRequires).GetConstructor (BindingFlags.Static | BindingFlags.NonPublic, new Type[0]);
			cctor.Invoke (null, null);
		}

		[ExpectedWarning ("IL2026", "Message for --StaticCtorOnTypeWithRequires--")]
		static void TestRunClassConstructorOnTypeWithRequires ()
		{
			var typeHandle = typeof (StaticCtorOnTypeWithRequires).TypeHandle;
			RuntimeHelpers.RunClassConstructor (typeHandle);
		}

		class StaticCtorForRunClassConstructorWithRequires
		{
			[ExpectedWarning ("IL2116")]
			[ExpectedWarning ("IL3004", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3056", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[RequiresUnreferencedCode ("Message for --StaticCtorOnTypeWithRequires--")]
			[RequiresAssemblyFiles ("Message for --StaticCtorOnTypeWithRequires--")]
			[RequiresDynamicCode ("Message for --StaticCtorOnTypeWithRequires--")]
			static StaticCtorForRunClassConstructorWithRequires () { }
		}

		static void TestRunClassConstructorOnConstructorWithRequires ()
		{
			// No warnings generated here - we rely on IL2116/IL3004/IL3056 in this case and avoid duplicate warnings
			RuntimeHelpers.RunClassConstructor (typeof (StaticCtorForRunClassConstructorWithRequires).TypeHandle);
		}

		class StaticCtorTriggeredByFieldAccess
		{
			[ExpectedWarning ("IL2116", "StaticCtorTriggeredByFieldAccess..cctor()")]
			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByFieldAccess.Cctor--")]
			static StaticCtorTriggeredByFieldAccess ()
			{
				field = 0;
			}

			public static int field;
		}

		static void TestStaticCtorMarkingIsTriggeredByFieldAccess ()
		{
			var x = StaticCtorTriggeredByFieldAccess.field + 1;
		}

		struct StaticCCtorForFieldAccess
		{
			[ExpectedWarning ("IL2116", "StaticCCtorForFieldAccess..cctor()")]
			[RequiresUnreferencedCode ("Message for --StaticCCtorForFieldAccess.cctor--")]
			static StaticCCtorForFieldAccess () { }

			public static int field;
		}

		static void TestStaticCtorMarkingIsTriggeredByFieldAccessOnExplicitLayout ()
		{
			StaticCCtorForFieldAccess.field = 0;
		}

		class StaticCtorTriggeredByMethodCall
		{
			[ExpectedWarning ("IL2116", "StaticCtorTriggeredByMethodCall..cctor()")]
			[ExpectedWarning ("IL3004", "StaticCtorTriggeredByMethodCall..cctor()", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3056", "StaticCtorTriggeredByMethodCall..cctor()", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByMethodCall.Cctor--")]
			[RequiresAssemblyFiles ("Message for --StaticCtorTriggeredByMethodCall.Cctor--")]
			[RequiresDynamicCode ("Message for --StaticCtorTriggeredByMethodCall.Cctor--")]
			static StaticCtorTriggeredByMethodCall ()
			{
			}

			[RequiresUnreferencedCode ("Message for --StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--")]
			[RequiresAssemblyFiles ("Message for --StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--")]
			[RequiresDynamicCode ("Message for --StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--")]
			public void TriggerStaticCtorMarking ()
			{
			}
		}


		[ExpectedWarning ("IL2026", "--StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--")]
		[ExpectedWarning ("IL3002", "--StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "--StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		static void TestStaticCtorTriggeredByMethodCall ()
		{
			new StaticCtorTriggeredByMethodCall ().TriggerStaticCtorMarking ();
		}

		class TypeIsBeforeFieldInit
		{
			[ExpectedWarning ("IL2026", "Message from --TypeIsBeforeFieldInit.AnnotatedMethod--", ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL3002", "Message from --TypeIsBeforeFieldInit.AnnotatedMethod--", ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL3050", "Message from --TypeIsBeforeFieldInit.AnnotatedMethod--", ProducedBy = Tool.Analyzer)]
			public static int field = AnnotatedMethod ();

			[RequiresUnreferencedCode ("Message from --TypeIsBeforeFieldInit.AnnotatedMethod--")]
			[RequiresAssemblyFiles ("Message from --TypeIsBeforeFieldInit.AnnotatedMethod--")]
			[RequiresDynamicCode ("Message from --TypeIsBeforeFieldInit.AnnotatedMethod--")]
			public static int AnnotatedMethod () => 42;
		}

		// ILLink sees the call to AnnotatedMethod in the static .ctor, but analyzer doesn't see the static .ctor at all
		// since it's fully compiler generated, instead it sees the call on the field initialization itself.
		[LogContains ("IL2026: Mono.Linker.Tests.Cases.RequiresCapability.RequiresOnStaticConstructor.TypeIsBeforeFieldInit..cctor():" +
			" Using member 'Mono.Linker.Tests.Cases.RequiresCapability.RequiresOnStaticConstructor.TypeIsBeforeFieldInit.AnnotatedMethod()'" +
			" which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code." +
			" Message from --TypeIsBeforeFieldInit.AnnotatedMethod--.", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
		[LogContains ("IL3002: Mono.Linker.Tests.Cases.RequiresCapability.RequiresOnStaticConstructor.TypeIsBeforeFieldInit..cctor():", ProducedBy = Tool.NativeAot)]
		[LogContains ("IL3050: Mono.Linker.Tests.Cases.RequiresCapability.RequiresOnStaticConstructor.TypeIsBeforeFieldInit..cctor():", ProducedBy = Tool.NativeAot)]
		static void TestTypeIsBeforeFieldInit ()
		{
			var x = TypeIsBeforeFieldInit.field + 42;
		}

		[RequiresUnreferencedCode ("--MethodWithRequires--")]
		[RequiresAssemblyFiles ("--MethodWithRequires--")]
		[RequiresDynamicCode ("--MethodWithRequires--")]
		static void MethodWithRequires ()
		{
		}
	}
}
