// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Asn1
{
    internal abstract class RestrictedAsciiRangeEncoding : RestrictedAsciiStringEncoding
    {
        protected RestrictedAsciiRangeEncoding(byte minCharAllowed, byte maxCharAllowed)
            : base(minCharAllowed, maxCharAllowed)
        {
        }
    }
}
