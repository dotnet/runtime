// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// TODO: share? Or find a better home?

namespace System.Diagnostics.CodeAnalysis
{
	class FeatureGuardAttribute : Attribute
	{
		public FeatureGuardAttribute (Type requiresAttributeType)
		{
		}
	}

	sealed class FeatureGuardAttribute<T> : FeatureGuardAttribute
		where T : Attribute
	{
		public FeatureGuardAttribute () : base (typeof (T))
		{
		}
	}
}
