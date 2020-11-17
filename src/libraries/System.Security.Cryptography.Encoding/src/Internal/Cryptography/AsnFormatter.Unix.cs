// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.Cryptography
{
    internal abstract partial class AsnFormatter
    {
        private static readonly AsnFormatter s_instance = new OpenSslAsnFormatter();
    }
}
