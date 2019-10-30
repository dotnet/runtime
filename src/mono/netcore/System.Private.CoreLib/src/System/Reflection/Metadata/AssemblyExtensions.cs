// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata
{
	public static class AssemblyExtensions
	{
		[CLSCompliant(false)]
		public static unsafe bool TryGetRawMetadata (this Assembly assembly, out byte* blob, out int length) => throw new NotImplementedException ();
	}
}
