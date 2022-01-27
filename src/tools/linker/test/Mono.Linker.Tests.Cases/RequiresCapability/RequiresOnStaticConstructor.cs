// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
			typeof (StaticCtor).RequiresNonPublicConstructors ();
		}

		class StaticCtor
		{
			[ExpectedWarning ("IL2116", "StaticCtor..cctor()", ProducedBy = ProducedBy.Trimmer)]
			[RequiresUnreferencedCode ("Message for --TestStaticCtor--")]
			static StaticCtor ()
			{
			}
		}

		static void TestStaticCctorRequires ()
		{
			_ = new StaticCtor ();
		}

		class StaticCtorTriggeredByFieldAccess
		{
			[ExpectedWarning ("IL2116", "StaticCtorTriggeredByFieldAccess..cctor()", ProducedBy = ProducedBy.Trimmer)]
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
			// TODO: Analyzer still allows RUC/RAF on static constructor with no warning
			// https://github.com/dotnet/linker/issues/2347
			[ExpectedWarning ("IL2116", "StaticCCtorForFieldAccess..cctor()", ProducedBy = ProducedBy.Trimmer)]
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
			// TODO: Analyzer still allows RUC/RAF on static constructor with no warning
			// https://github.com/dotnet/linker/issues/2347
			[ExpectedWarning ("IL2116", "StaticCtorTriggeredByMethodCall..cctor()", ProducedBy = ProducedBy.Trimmer)]
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
		[ExpectedWarning ("IL3002", "--StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--", ProducedBy = ProducedBy.Analyzer)]
		static void TestStaticCtorTriggeredByMethodCall ()
		{
			new StaticCtorTriggeredByMethodCall ().TriggerStaticCtorMarking ();
		}

		class TypeIsBeforeFieldInit
		{
			[ExpectedWarning ("IL2026", "Message from --TypeIsBeforeFieldInit.AnnotatedMethod--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3002", "Message from --TypeIsBeforeFieldInit.AnnotatedMethod--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "Message from --TypeIsBeforeFieldInit.AnnotatedMethod--", ProducedBy = ProducedBy.Analyzer)]
			public static int field = AnnotatedMethod ();

			[RequiresUnreferencedCode ("Message from --TypeIsBeforeFieldInit.AnnotatedMethod--")]
			[RequiresAssemblyFiles ("Message from --TypeIsBeforeFieldInit.AnnotatedMethod--")]
			[RequiresDynamicCode ("Message from --TypeIsBeforeFieldInit.AnnotatedMethod--")]
			public static int AnnotatedMethod () => 42;
		}

		// Linker sees the call to AnnotatedMethod in the static .ctor, but analyzer doesn't see the static .ctor at all
		// since it's fully compiler generated, instead it sees the call on the field initialization itself.
		[LogContains ("IL2026: Mono.Linker.Tests.Cases.RequiresCapability.RequiresOnStaticConstructor.TypeIsBeforeFieldInit..cctor():" +
			" Using member 'Mono.Linker.Tests.Cases.RequiresCapability.RequiresOnStaticConstructor.TypeIsBeforeFieldInit.AnnotatedMethod()'" +
			" which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code." +
			" Message from --TypeIsBeforeFieldInit.AnnotatedMethod--.", ProducedBy = ProducedBy.Trimmer)]
		static void TestTypeIsBeforeFieldInit ()
		{
			var x = TypeIsBeforeFieldInit.field + 42;
		}
	}
}
