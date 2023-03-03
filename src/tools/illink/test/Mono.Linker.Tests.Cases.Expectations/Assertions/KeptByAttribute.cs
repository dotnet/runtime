// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.All, Inherited = false)]
	public class KeptByAttribute : KeptAttribute
	{
		private KeptByAttribute () { }

		/// <summary>
		/// Place on an type member to indicate that the linker should log that the member is kept as a depenendency of <paramref name="dependencyProvider"/> with reason <paramref name="reason"/>.
		/// </summary>
		/// <param name="dependencyProvider">Cecil's FullName property of the item that provides the dependency that keeps the item</param>
		/// <param name="reason">The string representation of the DependencyKind that is recorded as the reason for the dependency</param>
		public KeptByAttribute (string dependencyProvider, string reason) { }

		/// <summary>
		/// Place on an type member to indicate that the linker should log that the member is kept as a depenendency of <paramref name="dependencyProviderType"/> with reason <paramref name="reason"/>.
		/// </summary>
		/// <param name="dependencyProviderType">The type that is providing the dependency that keeps the item</param>
		/// <param name="reason">The string representation of the DependencyKind that is recorded as the reason for the dependency</param>
		public KeptByAttribute (Type dependencyProviderType, string reason) { }

		/// <summary>
		/// Place on an type member to indicate that the linker should log that the member is kept as a depenendency of <paramref name="dependencyProviderType"/>.<paramref name="memberName"/> with reason <paramref name="reason"/>.
		/// </summary>
		/// <param name="dependencyProviderType">The declaring type of the member that is providing the dependency that keeps the item</param>
		/// <param name="memberName">Cecil's 'Name' property of the member that provides the dependency that keeps the item</param>
		/// <param name="reason">The string representation of the DependencyKind that is recorded as the reason for the dependency</param>
		public KeptByAttribute (Type dependencyProviderType, string memberName, string reason) { }
	}
}
