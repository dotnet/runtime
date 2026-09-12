// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace System.Configuration
{
    internal static class AppleApplication
    {
        internal static unsafe string GetMainBundleIdentifier()
        {
            IntPtr bundle = Interop.CoreFoundation.CFBundleGetMainBundle();
            IntPtr identifier = bundle == IntPtr.Zero
                ? IntPtr.Zero
                : Interop.CoreFoundation.CFBundleGetIdentifier(bundle);
            if (identifier == IntPtr.Zero)
            {
                return null;
            }

            IntPtr length = Interop.CoreFoundation.CFStringGetLength(identifier);
            long maximumByteCount = Interop.CoreFoundation.CFStringGetMaximumSizeForEncoding(
                length,
                Interop.CoreFoundation.kCFStringEncodingUTF8).ToInt64();
            if (maximumByteCount < 0 || maximumByteCount >= int.MaxValue)
            {
                return null;
            }

            byte[] buffer = new byte[(int)maximumByteCount + 1];
            fixed (byte* bufferPtr = buffer)
            {
                if (Interop.CoreFoundation.CFStringGetCString(
                    identifier,
                    bufferPtr,
                    new IntPtr(buffer.Length),
                    Interop.CoreFoundation.kCFStringEncodingUTF8) == 0)
                {
                    return null;
                }
            }

            int terminator = Array.IndexOf(buffer, (byte)0);
            if (terminator < 0)
            {
                return null;
            }

            return Encoding.UTF8.GetString(buffer, 0, terminator);
        }
    }
}
