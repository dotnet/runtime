// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared
{
	internal static class DiagnosticCategory
	{
		public const string SingleFile = nameof (SingleFile);
		public const string Trimming = nameof (Trimming);
		public const string AOT = nameof (AOT);
	}
}
