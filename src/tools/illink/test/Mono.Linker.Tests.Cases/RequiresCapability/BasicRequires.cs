// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

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
			AssemblyFilesOnly.Test ();
			DynamicCodeOnly.Test ();
		}

		[ExpectedWarning ("IL2026", "Message for --RequiresWithMessageOnly--.")]
		[ExpectedWarning ("IL3002", "Message for --RequiresWithMessageOnly--.", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "Message for --RequiresWithMessageOnly--.", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
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
		[ExpectedWarning ("IL3002", "Message for --RequiresWithMessageAndUrl--.", "https://helpurl", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "Message for --RequiresWithMessageAndUrl--.", "https://helpurl", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
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
		[ExpectedWarning ("IL3002", "Message for --ConstructorRequires--.", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "Message for --ConstructorRequires--.", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
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
		[ExpectedWarning ("IL3002", "Message for --getter PropertyRequires--.", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3002", "Message for --setter PropertyRequires--.", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "Message for --getter PropertyRequires--.", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "Message for --setter PropertyRequires--.", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
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

		[RequiresUnreferencedCode ("Adds a trailing period to this message")]
		[RequiresAssemblyFiles ("Adds a trailing period to this message")]
		[RequiresDynamicCode ("Adds a trailing period to this message")]
		static void WarningMessageWithoutEndingPeriod ()
		{
		}

		[ExpectedWarning ("IL2026", "Adds a trailing period to this message.")]
		[ExpectedWarning ("IL3002", "Adds a trailing period to this message.", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "Adds a trailing period to this message.", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		static void TestThatTrailingPeriodIsAddedToMessage ()
		{
			WarningMessageWithoutEndingPeriod ();
		}

		[RequiresUnreferencedCode ("Does not add a period to this message.")]
		[RequiresAssemblyFiles ("Does not add a period to this message.")]
		[RequiresDynamicCode ("Does not add a period to this message.")]
		static void WarningMessageEndsWithPeriod ()
		{
		}

		[LogDoesNotContain ("Does not add a period to this message..")]
		[ExpectedWarning ("IL2026", "Does not add a period to this message.")]
		[ExpectedWarning ("IL3002", "Does not add a period to this message.", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		[ExpectedWarning ("IL3050", "Does not add a period to this message.", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
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
			[ExpectedWarning ("IL2026", "--EventToTestRemove.remove--", ProducedBy = Tool.Trimmer)]
			static event EventHandler EventToTestRemove {
				add { }
				[RequiresUnreferencedCode ("Message for --EventToTestRemove.remove--")]
				[RequiresAssemblyFiles ("Message for --EventToTestRemove.remove--")]
				[RequiresDynamicCode ("Message for --EventToTestRemove.remove--")]
				remove { }
			}

			[ExpectedWarning ("IL2026", "--EventToTestAdd.add--", ProducedBy = Tool.Trimmer)]
			static event EventHandler EventToTestAdd {
				[RequiresUnreferencedCode ("Message for --EventToTestAdd.add--")]
				[RequiresAssemblyFiles ("Message for --EventToTestAdd.add--")]
				[RequiresDynamicCode ("Message for --EventToTestAdd.add--")]
				add { }
				remove { }
			}

			[RequiresAssemblyFiles ("Message for --AnnotatedEvent--")]
			static event EventHandler AnnotatedEvent;

			[ExpectedWarning ("IL2026", "--EventToTestRemove.remove--")]
			[ExpectedWarning ("IL3002", "--EventToTestRemove.remove--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--EventToTestRemove.remove--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL2026", "--EventToTestAdd.add--")]
			[ExpectedWarning ("IL3002", "--EventToTestAdd.add--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--EventToTestAdd.add--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			public static void Test ()
			{
				EventToTestRemove -= (sender, e) => { };
				EventToTestAdd += (sender, e) => { };
				var evt = AnnotatedEvent;
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

			[ExpectedWarning ("IL2026", "--GenericTypeWithStaticMethodWhichRequires--")]
			[ExpectedWarning ("IL3002", "--GenericTypeWithStaticMethodWhichRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--GenericTypeWithStaticMethodWhichRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
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

		class AssemblyFilesOnly
		{
			[RequiresAssemblyFiles ("--Requires--")]
			static void Requires () { }

			[RequiresAssemblyFiles ("--PropertyRequires--")]
			static int PropertyRequires { get; set; }

			[ExpectedWarning ("IL3002", "--PropertyRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3002", "--PropertyRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestProperty ()
			{
				var a = PropertyRequires;
				PropertyRequires = 0;
			}

			[RequiresAssemblyFiles ("--EventRequires--")]
			static event EventHandler EventRequires;

			[ExpectedWarning ("IL3002", "--EventRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3002", "--EventRequires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL2026", "--RequiresOnEventLambda--")]
			[ExpectedWarning ("IL3002", "--RequiresOnEventLambda--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--RequiresOnEventLambda--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			static void TestEvent ()
			{
				EventRequires += (object sender, EventArgs e) => throw new NotImplementedException ();
				EventRequires -= (object sender, EventArgs e) => throw new NotImplementedException ();
				EventRequires = (object sender, EventArgs e) => throw new NotImplementedException (); // no warning on field assignment
				EventRequires =
					[RequiresUnreferencedCode ("--RequiresOnEventLambda--")]
					[RequiresAssemblyFiles ("--RequiresOnEventLambda--")]
					[RequiresDynamicCode ("--RequiresOnEventLambda--")]
					(object sender, EventArgs e) => throw new NotImplementedException ();
				EventRequires (null, null); // no warning on invocation
			}

			[ExpectedWarning("IL3002", "--Requires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			public static void Test()
			{
				Requires ();
				TestProperty ();
				TestEvent ();
			}
		}

		class DynamicCodeOnly
		{
			[RequiresDynamicCode ("--Requires--")]
			static void Requires () { }

			[ExpectedWarning ("IL3050", "--Requires--", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
			public static void Test ()
			{
				Requires ();
			}
		}
	}
}
