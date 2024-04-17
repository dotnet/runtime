﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class DetectRedundantSuppressionsInMembersAndTypes
	{
		public static void Main ()
		{
			RedundantSuppressionOnType.Test ();
			RedundantSuppressionOnMethod.Test ();
			RedundantSuppressionOnNestedType.Test ();

			RedundantSuppressionOnPropertyGet.Test ();
			RedundantSuppressionOnProperty.Test ();
			RedundantSuppressionOnPropertyWithOnlyGet.Test ();
			RedundantSuppressionOnPropertyWithOnlySet.Test ();
			RedundantSuppressionOnPropertyAccessedByReflection.Test ();

			RedundantSuppressionOnEventAdd.Test ();
			RedundantSuppressionOnEvent.Test ();
			RedundantSuppressionOnEventAccessedByReflection.Test ();

			MultipleRedundantSuppressions.Test ();
			RedundantAndUsedSuppressions.Test ();

			DoNotReportNonLinkerSuppressions.Test ();
			DoNotReportSuppressionsOnMethodsConvertedToThrow.Test ();

			SuppressRedundantSuppressionWarning.Test ();
			DoNotReportUnnecessaryRedundantWarningSuppressions.Test ();

			RedundantSuppressionWithRUC.Test ();
		}

		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (DetectRedundantSuppressionsInMembersAndTypes);
		}

		public static string TrimmerCompatibleMethod ()
		{
			return "test";
		}

		[ExpectedMissingWarning ("IL2121", "IL2071", ProducedBy = Tool.Trimmer)]
		[UnconditionalSuppressMessage ("Test", "IL2071")]
		public class RedundantSuppressionOnType
		{
			public static void Test ()
			{
				TrimmerCompatibleMethod ();
			}
		}

		public class RedundantSuppressionOnMethod
		{
			[ExpectedMissingWarning ("IL2121", "IL2071", ProducedBy = Tool.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2071")]
			public static void Test ()
			{
				TrimmerCompatibleMethod ();
			}
		}

		public class RedundantSuppressionOnNestedType
		{
			public static void Test ()
			{
				NestedType.TrimmerCompatibleMethod ();
			}

			[ExpectedMissingWarning ("IL2121", "IL2071", ProducedBy = Tool.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2071")]
			public class NestedType
			{
				public static void TrimmerCompatibleMethod ()
				{
					DetectRedundantSuppressionsInMembersAndTypes.TrimmerCompatibleMethod ();
				}
			}
		}

		public class RedundantSuppressionOnPropertyGet
		{
			public static void Test ()
			{
				var property = TrimmerCompatibleProperty;
			}

			public static string TrimmerCompatibleProperty {
				[ExpectedMissingWarning ("IL2121", "IL2071", ProducedBy = Tool.Trimmer)]
				[UnconditionalSuppressMessage ("Test", "IL2071")]
				get {
					return TrimmerCompatibleMethod ();
				}
			}
		}

		public class RedundantSuppressionOnProperty
		{
			public static void Test ()
			{
				var property = TrimmerCompatibleProperty;
				TrimmerCompatibleProperty = "test";
			}

			[ExpectedMissingWarning ("IL2121", "IL2071", ProducedBy = Tool.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2071")]
			public static string TrimmerCompatibleProperty {
				get {
					return TrimmerCompatibleMethod ();
				}
				set {
					value = TrimmerCompatibleMethod ();
				}
			}
		}

		public class RedundantSuppressionOnPropertyWithOnlyGet
		{
			public static void Test ()
			{
				var property = TrimmerCompatibleProperty;
			}

			[ExpectedMissingWarning ("IL2121", "IL2071", ProducedBy = Tool.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2071")]
			public static string TrimmerCompatibleProperty {
				get {
					return TrimmerCompatibleMethod ();
				}
			}
		}

		public class RedundantSuppressionOnPropertyWithOnlySet
		{
			public static void Test ()
			{
				TrimmerCompatibleProperty = "test";
			}

			[ExpectedMissingWarning ("IL2121", "IL2071", ProducedBy = Tool.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2071")]
			public static string TrimmerCompatibleProperty {
				set {
					value = TrimmerCompatibleMethod ();
				}
			}
		}

		public class RedundantSuppressionOnPropertyAccessedByReflection
		{
			public static void Test ()
			{
				typeof (RedundantSuppressionOnPropertyAccessedByReflection).GetProperty ("TrimmerCompatibleProperty");
			}

			[ExpectedMissingWarning ("IL2121", "IL2071", ProducedBy = Tool.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2071")]
			public static string TrimmerCompatibleProperty {
				get {
					return TrimmerCompatibleMethod ();
				}
			}
		}

		public class RedundantSuppressionOnEventAdd
		{
			public static void Test ()
			{
				TrimmerCompatibleEvent += EventSubscriber;
			}

			static void EventSubscriber (object sender, EventArgs e)
			{

			}

			public static event EventHandler<EventArgs> TrimmerCompatibleEvent {
				[ExpectedMissingWarning ("IL2121", "IL2072", ProducedBy = Tool.Trimmer)]
				[UnconditionalSuppressMessage ("Test", "IL2072")]
				add { TrimmerCompatibleMethod (); }
				remove { }
			}
		}

		public class RedundantSuppressionOnEvent
		{
			public static void Test ()
			{
				TrimmerCompatibleEvent += EventSubscriber;
			}

			static void EventSubscriber (object sender, EventArgs e)
			{

			}

			[ExpectedMissingWarning ("IL2121", "IL2072", ProducedBy = Tool.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2072")]
			public static event EventHandler<EventArgs> TrimmerCompatibleEvent {
				add { TrimmerCompatibleMethod (); }
				remove { }
			}
		}

		public class RedundantSuppressionOnEventAccessedByReflection
		{
			public static void Test ()
			{
				typeof (RedundantSuppressionOnEventAccessedByReflection).GetEvent ("TrimmerCompatibleEvent");
			}

			[ExpectedMissingWarning ("IL2121", "IL2072", ProducedBy = Tool.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2072")]
			public static event EventHandler<EventArgs> TrimmerCompatibleEvent {
				add { TrimmerCompatibleMethod (); }
				remove { }
			}
		}

		[ExpectedMissingWarning ("IL2121", "IL2072", ProducedBy = Tool.Trimmer)]
		[UnconditionalSuppressMessage ("Test", "IL2072")]
		[ExpectedMissingWarning ("IL2121", "IL2071", ProducedBy = Tool.Trimmer)]
		[UnconditionalSuppressMessage ("Test", "IL2071")]
		public class MultipleRedundantSuppressions
		{
			[ExpectedMissingWarning ("IL2121", "IL2072", ProducedBy = Tool.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2072")]
			[ExpectedMissingWarning ("IL2121", "IL2071", ProducedBy = Tool.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2071")]
			public static void Test ()
			{
				TrimmerCompatibleMethod ();
			}
		}

		public class RedundantAndUsedSuppressions
		{
			[ExpectedMissingWarning ("IL2121", "IL2071", ProducedBy = Tool.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2071")]
			[UnconditionalSuppressMessage ("Test", "IL2072")]
			public static void Test ()
			{
				Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			}
		}

		public class DoNotReportNonLinkerSuppressions
		{
			[UnconditionalSuppressMessage ("Test", "IL3052")]
			public static void Test ()
			{
				TrimmerCompatibleMethod ();
			}
		}

		public class DoNotReportSuppressionsOnMethodsConvertedToThrow
		{
			// The tool is unable to determine whether a suppression is redundant when it is placed on a method with unreachable body.
			// Currently suppressions on methods with unreachable bodies should never be reported as redundant.
			// https://github.com/dotnet/linker/issues/2920
			public static void Test ()
			{
				UsedToMarkMethod (null);
			}

			static void UsedToMarkMethod (TypeWithMethodConvertedToThrow t)
			{
				t.MethodConvertedToThrow ();
			}

			class TypeWithMethodConvertedToThrow
			{
				// The suppression is redundant, but it should not be reported.
				[UnconditionalSuppressMessage ("Test", "IL2072")]
				public void MethodConvertedToThrow ()
				{
					TrimmerCompatibleMethod ();
				}
			}
		}

		public class SuppressRedundantSuppressionWarning
		{
			[UnconditionalSuppressMessage ("Test", "IL2121")]
			[UnconditionalSuppressMessage ("Test", "IL2072")]
			public static void Test ()
			{
				TrimmerCompatibleMethod ();
			}
		}

		public class DoNotReportUnnecessaryRedundantWarningSuppressions
		{
			[UnconditionalSuppressMessage ("Test", "IL2121")]
			[UnconditionalSuppressMessage ("Test", "IL2072")]
			public static void Test ()
			{
				Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			}
		}

		public class RedundantSuppressionWithRUC
		{
			[ExpectedWarning ("IL2026")]
			public static void Test ()
			{
				MethodMarkedRUC ();
			}

			[ExpectedMissingWarning ("IL2121", "IL2072", ProducedBy = Tool.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2072")]
			[RequiresUnreferencedCode ("Test")]
			public static void MethodMarkedRUC ()
			{
				Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			}
		}
	}
}
