// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Mono.Linker
{
	public readonly struct MessageContainer
	{
		public static MessageContainer CreateCustomErrorMessage (string text, int code, string subcategory = "", MessageOrigin? origin = null) { throw null; }
		public static MessageContainer CreateCustomWarningMessage (LinkContext context, string text, int code, MessageOrigin origin, WarnVersion version, string subcategory = "") { throw null; }
		public static MessageContainer CreateInfoMessage (string text) { throw null; }
		public static MessageContainer CreateDiagnosticMessage (string text) { throw null; }
	}
}
