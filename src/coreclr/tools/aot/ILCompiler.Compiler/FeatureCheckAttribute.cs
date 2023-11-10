// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// TODO: share? Or find a better home?

namespace System.Diagnostics.CodeAnalysis
{
	class FeatureCheckAttribute : Attribute
	{
		public FeatureCheckAttribute (Type requiresAttributeType)
		{
		}
	}

	sealed class FeatureCheckAttribute<T> : FeatureCheckAttribute
		where T : Attribute
	{
		public FeatureCheckAttribute () : base (typeof (T))
		{
		}
	}
}
