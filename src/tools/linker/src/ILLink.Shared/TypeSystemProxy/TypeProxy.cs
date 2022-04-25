// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct TypeProxy : IMemberProxy
	{
		internal partial ImmutableArray<GenericParameterProxy> GetGenericParameters ();
	}
}
