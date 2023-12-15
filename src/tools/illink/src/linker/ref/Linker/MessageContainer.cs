// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
