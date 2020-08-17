// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Mono.Linker
{
	public readonly struct MessageContainer
	{
		public static MessageContainer CreateErrorMessage (string text, int code, string subcategory = "", MessageOrigin? origin = null) { throw null; }
		public static MessageContainer CreateWarningMessage (LinkContext context, string text, int code, MessageOrigin origin, WarnVersion version, string subcategory = "") { throw null; }
		public static MessageContainer CreateInfoMessage (string text) { throw null; }
		public static MessageContainer CreateDiagnosticMessage (string text) { throw null; }
	}
}
