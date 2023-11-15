// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Diagnostics.CodeAnalysis
{
	[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	public sealed class FeatureGuardAttribute<T> : Attribute
		where T : Attribute
	{
		public FeatureGuardAttribute()
		{
		}
	}
}
