// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;

namespace System.Text
{
    // Shared implementations for commonly overriden Encoding methods

    internal static class EncodingForwarder
    {
        // We normally have to duplicate a lot of code between UTF8Encoding,
        // UTF7Encoding, EncodingNLS, etc. because we want to override many
        // of the methods in all of those classes to just forward to the unsafe
        // version. (e.g. GetBytes(char[]))
        // Ideally, everything would just derive from EncodingNLS, but that's
        // not exposed in the public API, and C# prohibits a public class from
        // inheriting from an internal one. So, we have to override each of the
        // methods in question and repeat the argument validation/logic.

        // These set of methods exist so instead of duplicating code, we can
        // simply have those overriden methods call here to do the actual work.

        public unsafe static int GetByteCount(Encoding encoding, char[] chars, int index, int count)
        {
            // Validate parameters

            Contract.Assert(encoding != null); // this parameter should only be affected internally, so just do a debug check here
            if (chars == null)
            {
                throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
            }
            if (index < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException(index < 0 ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (chars.Length - index < count)
            {
                throw new ArgumentOutOfRangeException("chars", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            }
            Contract.EndContractBlock();

            // If no input, return 0, avoid fixed empty array problem
            if (count == 0)
                return 0;

            // Just call the pointer version
            fixed (char* pChars = chars)
                return encoding.GetByteCount(pChars + index, count, null);
        }
    }
}
