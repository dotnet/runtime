// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared
{
	public static class MessageSubCategory
	{
		public const string None = "";
		public const string TrimAnalysis = "Trim analysis";
		public const string UnresolvedAssembly = "Unresolved assembly";
		public const string AotAnalysis = "AOT analysis";
	}
}
