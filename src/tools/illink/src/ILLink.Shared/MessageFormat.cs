// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

using System;

namespace ILLink.Shared
{
	internal static class MessageFormat
	{
		public static string FormatRequiresAttributeMessageArg (string? message)
		{
			if (!string.IsNullOrEmpty (message))
				return $" {message}{(message!.TrimEnd ().EndsWith (".") ? "" : ".")}";

			return string.Empty;
		}

		public static string FormatRequiresAttributeUrlArg (string? url)
		{
			if (!string.IsNullOrEmpty (url))
				return $" {url}";

			return string.Empty;
		}

		public static string FormatRequiresAttributeMismatch (bool memberHasAttribute, bool isInterface, params object[] args)
		{
			string format = (memberHasAttribute, isInterface) switch {
				(false, true) => SharedStrings.InterfaceRequiresMismatchMessage,
				(true, true) => SharedStrings.ImplementationRequiresMismatchMessage,
				(false, false) => SharedStrings.BaseRequiresMismatchMessage,
				(true, false) => SharedStrings.DerivedRequiresMismatchMessage
			};

			return TryFormat (format, args);
		}

		public static string TryFormat (string format, params object[] args)
		{
			string formattedString;
			try
			{
				formattedString = string.Format(format, args);
			}
			catch (FormatException)
			{
#pragma warning disable RS1035 // Do not use APIs banned for analyzers - We just need a newline
				formattedString = "Internal error formatting diagnostic. Please report the issue at https://aka.ms/report-illink" + Environment.NewLine
					+ $"'{format}', " + $"[{string.Join(", ", args)}]";
#pragma warning restore RS1035 // Do not use APIs banned for analyzers
			}
			return formattedString;
		}
	}
}
