// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Text
{
    public sealed partial class CodePagesEncodingProvider : EncodingProvider
    {
        /// <summary>Get the system default non-unicode code page, or 0 if not available.</summary>
        private static int SystemDefaultCodePage
        {
            get { return 0; }
        }
    }
}
