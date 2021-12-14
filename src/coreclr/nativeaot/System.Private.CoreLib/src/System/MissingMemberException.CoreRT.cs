// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System
{
    public partial class MissingMemberException : MemberAccessException
    {
        internal static string FormatSignature(byte[] signature)
        {
            // This is not the correct implementation, however, it's probably not worth the time to port given that
            //  (1) it's for a diagnostic
            //  (2) Signature is non-null when this exception is created from the native runtime. Which we don't do in .Net Native.
            //  (3) Only other time the signature is non-null is if this exception object is deserialized from a persisted blob from an older runtime.
            return string.Empty;
        }
    }
}
