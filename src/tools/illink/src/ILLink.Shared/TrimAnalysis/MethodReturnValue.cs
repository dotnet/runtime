// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	internal sealed partial record class MethodReturnValue : ValueWithDynamicallyAccessedMembers, IValueWithStaticType
	{
		public TypeProxy? StaticType { get; }
	}
}
