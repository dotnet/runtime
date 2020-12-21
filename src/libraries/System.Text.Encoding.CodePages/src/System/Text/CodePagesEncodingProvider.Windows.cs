// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text
{
    public sealed partial class CodePagesEncodingProvider : EncodingProvider
    {
        /// <summary>Get the system default non-unicode code page, or 0 if not available.</summary>
        private static int SystemDefaultCodePage
        {
            get
            {
                int codePage;
                return Interop.Kernel32.TryGetACPCodePage(out codePage) ? codePage : 0;
            }
        }
    }
}
