// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.DataFlow;

namespace ILLink.Shared.TrimAnalysis
{
	public abstract record ValueWithDynamicallyAccessedMembers : SingleValue
	{
		public abstract DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }
	}
}