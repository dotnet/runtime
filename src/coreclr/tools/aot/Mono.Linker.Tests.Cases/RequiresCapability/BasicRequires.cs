// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class BasicRequires
	{

		public static void Main ()
		{
			TestRequiresWithMessageOnlyOnMethod ();
			TestRequiresWithMessageAndUrlOnMethod ();
			TestRequiresOnConstructor ();
			TestRequiresOnPropertyGetterAndSetter ();
			TestThatTrailingPeriodIsAddedToMessage ();
			TestThatTrailingPeriodIsNotDuplicatedInWarningMessage ();
			TestRequiresFromNameOf ();
			OnEventMethod.Test ();
			RequiresOnGenerics.Test ();
		}

		[ExpectedWarning ("IL2026", "Message for --RequiresWithMessageOnly--.")]
		[ExpectedWarning ("IL3002", "Message for --RequiresWithMessageOnly--.", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL3050", "Message for --RequiresWithMessageOnly--.", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		static void TestRequiresWithMessageOnlyOnMethod ()
		{
			RequiresWithMessageOnly ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresWithMessageOnly--")]
		[RequiresAssemblyFiles ("Message for --RequiresWithMessageOnly--")]
		[RequiresDynamicCode ("Message for --RequiresWithMessageOnly--")]
		static void RequiresWithMessageOnly ()
		{
		}

		[ExpectedWarning ("IL2026", "Message for --RequiresWithMessageAndUrl--.", "https://helpurl")]
		[ExpectedWarning ("IL3002", "Message for --RequiresWithMessageAndUrl--.", "https://helpurl", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL3050", "Message for --RequiresWithMessageAndUrl--.", "https://helpurl", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		static void TestRequiresWithMessageAndUrlOnMethod ()
		{
			RequiresWithMessageAndUrl ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresWithMessageAndUrl--", Url = "https://helpurl")]
		[RequiresAssemblyFiles ("Message for --RequiresWithMessageAndUrl--", Url = "https://helpurl")]
		[RequiresDynamicCode ("Message for --RequiresWithMessageAndUrl--", Url = "https://helpurl")]
		static void RequiresWithMessageAndUrl ()
		{
		}

		[ExpectedWarning ("IL2026", "Message for --ConstructorRequires--.")]
		[ExpectedWarning ("IL3002", "Message for --ConstructorRequires--.", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL3050", "Message for --ConstructorRequires--.", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		static void TestRequiresOnConstructor ()
		{
			new ConstructorRequires ();
		}

		class ConstructorRequires
		{
			[RequiresUnreferencedCode ("Message for --ConstructorRequires--")]
			[RequiresAssemblyFiles ("Message for --ConstructorRequires--")]
			[RequiresDynamicCode ("Message for --ConstructorRequires--")]
			public ConstructorRequires ()
			{
			}
		}

		[ExpectedWarning ("IL2026", "Message for --getter PropertyRequires--.")]
		[ExpectedWarning ("IL2026", "Message for --setter PropertyRequires--.")]
		[ExpectedWarning ("IL3002", "Message for --getter PropertyRequires--.", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL3002", "Message for --setter PropertyRequires--.", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL3050", "Message for --getter PropertyRequires--.", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL3050", "Message for --setter PropertyRequires--.", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		static void TestRequiresOnPropertyGetterAndSetter ()
		{
			_ = PropertyRequires;
			PropertyRequires = 0;
		}

		static int PropertyRequires {
			[RequiresUnreferencedCode ("Message for --getter PropertyRequires--")]
			[RequiresAssemblyFiles ("Message for --getter PropertyRequires--")]
			[RequiresDynamicCode ("Message for --getter PropertyRequires--")]
			get { return 42; }

			[RequiresUnreferencedCode ("Message for --setter PropertyRequires--")]
			[RequiresAssemblyFiles ("Message for --setter PropertyRequires--")]
			[RequiresDynamicCode ("Message for --setter PropertyRequires--")]
			set { }
		}

		[RequiresUnreferencedCode ("Linker adds a trailing period to this message")]
		[RequiresAssemblyFiles ("Linker adds a trailing period to this message")]
		[RequiresDynamicCode ("Linker adds a trailing period to this message")]
		static void WarningMessageWithoutEndingPeriod ()
		{
		}

		[ExpectedWarning ("IL2026", "Linker adds a trailing period to this message.")]
		[ExpectedWarning ("IL3002", "Linker adds a trailing period to this message.", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL3050", "Linker adds a trailing period to this message.", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		static void TestThatTrailingPeriodIsAddedToMessage ()
		{
			WarningMessageWithoutEndingPeriod ();
		}

		[RequiresUnreferencedCode ("Linker does not add a period to this message.")]
		[RequiresAssemblyFiles ("Linker does not add a period to this message.")]
		[RequiresDynamicCode ("Linker does not add a period to this message.")]
		static void WarningMessageEndsWithPeriod ()
		{
		}

		[LogDoesNotContain ("Linker does not add a period to this message..")]
		[ExpectedWarning ("IL2026", "Linker does not add a period to this message.")]
		[ExpectedWarning ("IL3002", "Linker does not add a period to this message.", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL3050", "Linker does not add a period to this message.", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		static void TestThatTrailingPeriodIsNotDuplicatedInWarningMessage ()
		{
			WarningMessageEndsWithPeriod ();
		}

		static void TestRequiresFromNameOf ()
		{
			_ = nameof (BasicRequires.RequiresWithMessageOnly);
		}

		class OnEventMethod
		{
			[ExpectedWarning ("IL2026", "--EventToTestRemove.remove--", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "--EventToTestRemove.remove--", ProducedBy = ProducedBy.Trimmer)]
			static event EventHandler EventToTestRemove {
				add { }
				[RequiresUnreferencedCode ("Message for --EventToTestRemove.remove--")]
				[RequiresAssemblyFiles ("Message for --EventToTestRemove.remove--")]
				[RequiresDynamicCode ("Message for --EventToTestRemove.remove--")]
				remove { }
			}

			[ExpectedWarning ("IL2026", "--EventToTestAdd.add--", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "--EventToTestAdd.add--", ProducedBy = ProducedBy.Trimmer)]
			static event EventHandler EventToTestAdd {
				[RequiresUnreferencedCode ("Message for --EventToTestAdd.add--")]
				[RequiresAssemblyFiles ("Message for --EventToTestAdd.add--")]
				[RequiresDynamicCode ("Message for --EventToTestAdd.add--")]
				add { }
				remove { }
			}

			[ExpectedWarning ("IL2026", "--EventToTestRemove.remove--")]
			[ExpectedWarning ("IL3002", "--EventToTestRemove.remove--", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3050", "--EventToTestRemove.remove--", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL2026", "--EventToTestAdd.add--")]
			[ExpectedWarning ("IL3002", "--EventToTestAdd.add--", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3050", "--EventToTestAdd.add--", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			public static void Test ()
			{
				EventToTestRemove -= (sender, e) => { };
				EventToTestAdd += (sender, e) => { };
			}
		}

		class RequiresOnGenerics
		{
			class GenericWithStaticMethod<T>
			{
				[RequiresUnreferencedCode ("Message for --GenericTypeWithStaticMethodWhichRequires--")]
				[RequiresAssemblyFiles ("Message for --GenericTypeWithStaticMethodWhichRequires--")]
				[RequiresDynamicCode ("Message for --GenericTypeWithStaticMethodWhichRequires--")]
				public static void GenericTypeWithStaticMethodWhichRequires () { }
			}

			// NativeAOT doesnt produce Requires warnings in Generics https://github.com/dotnet/runtime/issues/68688
			// [ExpectedWarning("IL2026", "--GenericTypeWithStaticMethodWhichRequires--"]
			[ExpectedWarning ("IL2026", "--GenericTypeWithStaticMethodWhichRequires--", ProducedBy = ProducedBy.Analyzer | ProducedBy.Trimmer)]
			// [ExpectedWarning("IL3002", "--GenericTypeWithStaticMethodWhichRequires--", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3002", "--GenericTypeWithStaticMethodWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
			// [ExpectedWarning("IL3050", "--GenericTypeWithStaticMethodWhichRequires--", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3050", "--GenericTypeWithStaticMethodWhichRequires--", ProducedBy = ProducedBy.Analyzer)]
			public static void GenericTypeWithStaticMethodViaLdftn ()
			{
				var _ = new Action (GenericWithStaticMethod<TestType>.GenericTypeWithStaticMethodWhichRequires);
			}

			class TestType { }

			static T MakeNew<T> () where T : new() => new T ();
			static T MakeNew2<T> () where T : new() => MakeNew<T> ();

			public static void Test ()
			{
				GenericTypeWithStaticMethodViaLdftn ();
				MakeNew2<TestType> ();
			}
		}
	}
}
