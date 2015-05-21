// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Internal.Cryptography
{
    internal abstract partial class AsnFormatter
    {
        private static readonly AsnFormatter s_instance = new CngAsnFormatter();

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------
    }
}