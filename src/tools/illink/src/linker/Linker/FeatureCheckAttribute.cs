// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Diagnostics.CodeAnalysis
{
	sealed class FeatureGuardAttribute : Attribute
	{
		public Type RequiresAttributeType { get; }

		public FeatureGuardAttribute (Type requiresAttributeType)
		{
			RequiresAttributeType = requiresAttributeType;
		}
	}
}
