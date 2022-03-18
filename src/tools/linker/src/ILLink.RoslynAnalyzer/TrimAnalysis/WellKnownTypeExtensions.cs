// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TypeSystemProxy
{
	public static partial class WellKnownTypeExtensions
	{
		public static bool TryGetSpecialType (this WellKnownType wellKnownType, [NotNullWhen (true)] out SpecialType? specialType)
		{
			specialType = wellKnownType switch {
				WellKnownType.System_String => SpecialType.System_String,
				WellKnownType.System_Nullable_T => SpecialType.System_Nullable_T,
				WellKnownType.System_Array => SpecialType.System_Array,
				WellKnownType.System_Object => SpecialType.System_Object,
				_ => null,
			};
			return specialType is not null;
		}
	}
}