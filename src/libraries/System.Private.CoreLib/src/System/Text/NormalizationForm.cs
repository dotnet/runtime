// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Text
{
    public enum NormalizationForm
    {
        FormC = 1,
        FormD = 2,
        [UnsupportedOSPlatform("browser")]
        FormKC = 5,
        [UnsupportedOSPlatform("browser")]
        FormKD = 6
    }
}
