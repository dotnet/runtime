// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker.Tests.Cases.Expectations.Helpers
{
	public static class DataFlowTypeExtensions
	{
		public static void RequiresAll ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] this Type type) { }

		public static void RequiresPublicConstructors ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] this Type type) { }

		public static void RequiresPublicEvents ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)] this Type type) { }

		public static void RequiresPublicFields ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] this Type type) { }

		public static void RequiresPublicMethods ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] this Type type) { }

		public static void RequiresPublicNestedTypes ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)] this Type type) { }

		public static void RequiresPublicParameterlessConstructor ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] this Type type) { }

		public static void RequiresPublicProperties ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] this Type type) { }

		public static void RequiresNonPublicEvents ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicEvents)] this Type type) { }

		public static void RequiresNonPublicFields ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicFields)] this Type type) { }

		public static void RequiresNonPublicMethods ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)] this Type type) { }

		public static void RequiresNonPublicNestedTypes ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicNestedTypes)] this Type type) { }

		public static void RequiresNonPublicConstructors ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)] this Type type) { }

		public static void RequiresNonPublicProperties ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicProperties)] this Type type) { }

		public static void RequiresInterfaces ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)] this Type type) { }

		public static void RequiresNone (this Type type) { }
	}
}
