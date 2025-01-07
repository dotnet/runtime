// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class EventDataFlow
	{
		public static void Main ()
		{
			AssignToEvent.Test ();
		}

		class AssignToEvent
		{
			static event EventHandler MyEvent;

			static void HandleMyEvent (object sender, EventArgs args) => throw null;

			static void HandleMyEvent2 (object sender, EventArgs args) => throw null;

			public static void TestAssignEvent ()
			{
				MyEvent = HandleMyEvent;
			}

			public static void TestAssignCapturedEvent (bool b = false)
			{
				MyEvent = b ? HandleMyEvent : HandleMyEvent2;
			}

			delegate void TypeEventHandler(Type t);

			static event TypeEventHandler TypeEvent;

			[ExpectedWarning ("IL2111", nameof (DynamicallyAccessedMembersAttribute))]
			public static void TestAssignEventMismatchingAnnotations ()
			{
				TypeEvent =
					([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
						=> throw new Exception ();
			}

			delegate void AnnotatedTypeEventHandler([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type t);

			static event AnnotatedTypeEventHandler AnnotatedTypeEvent;

			// It doesn't matter whether the assigned lambda has annotations that match the
			// delegate type - we treat the delegate creation as reflection access to the
			// lambda method, and warn that it has annotations.
			[ExpectedWarning ("IL2111", nameof (DynamicallyAccessedMembersAttribute))]
			public static void TestAssignEventMatchingAnnotations ()
			{
				AnnotatedTypeEvent =
					([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type t)
						=> throw new Exception ();
			}

			// No dataflow warnings are involved, but the event assignment should not
			// crash the analyzer.
			public static void Test ()
			{
				TestAssignEvent ();
				TestAssignCapturedEvent ();
				TestAssignEventMismatchingAnnotations ();
				TestAssignEventMatchingAnnotations ();
			}
		}
	}
}