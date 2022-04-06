// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace ILLink.Shared
{
    internal static class MessageFormat
    {
        public static string FormatRequiresAttributeMessageArg(string? message)
        {
            if (!string.IsNullOrEmpty(message))
                return $" {message}{(message!.TrimEnd().EndsWith(".") ? "" : ".")}";

            return string.Empty;
        }

        public static string FormatRequiresAttributeUrlArg(string? url)
        {
            if (!string.IsNullOrEmpty(url))
                return $" {url}";

            return string.Empty;
        }

        public static string FormatRequiresAttributeMismatch(bool memberHasAttribute, bool isInterface, params object[] args)
        {
            string format = (memberHasAttribute, isInterface) switch
            {
                (false, true) => SharedStrings.InterfaceRequiresMismatchMessage,
                (true, true) => SharedStrings.ImplementationRequiresMismatchMessage,
                (false, false) => SharedStrings.BaseRequiresMismatchMessage,
                (true, false) => SharedStrings.DerivedRequiresMismatchMessage
            };

            return string.Format(format, args);
        }
    }
}
