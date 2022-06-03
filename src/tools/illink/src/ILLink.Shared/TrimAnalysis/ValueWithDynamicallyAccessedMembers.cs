// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.DataFlow;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
	public abstract record ValueWithDynamicallyAccessedMembers : SingleValue
	{
		public abstract DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public abstract IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ();
	}
}
