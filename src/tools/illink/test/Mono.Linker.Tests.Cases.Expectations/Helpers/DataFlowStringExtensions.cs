// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker.Tests.Cases.Expectations.Helpers
{
	public static class DataFlowStringExtensions
	{
		public static void RequiresAll ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] this string str) { }

		public static void RequiresPublicConstructors ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] this string str) { }

		public static void RequiresPublicEvents ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)] this string str) { }

		public static void RequiresPublicFields ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] this string str) { }

		public static void RequiresPublicMethods ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] this string str) { }

		public static void RequiresPublicNestedTypes ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)] this string str) { }

		public static void RequiresPublicParameterlessConstructor ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] this string str) { }

		public static void RequiresPublicProperties ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] this string str) { }

		public static void RequiresNonPublicEvents ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicEvents)] this string str) { }

		public static void RequiresNonPublicFields ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicFields)] this string str) { }

		public static void RequiresNonPublicMethods ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)] this string str) { }

		public static void RequiresNonPublicNestedTypes ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicNestedTypes)] this string str) { }

		public static void RequiresNonPublicConstructors ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)] this string str) { }

		public static void RequiresNonPublicProperties ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicProperties)] this string str) { }

		public static void RequiresInterfaces ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)] this string str) { }

		public static void RequiresNone (this string str) { }
	}
}
