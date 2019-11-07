// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    public enum CompositingQuality
    {
        Invalid = QualityMode.Invalid,
        Default = QualityMode.Default,
        HighSpeed = QualityMode.Low,
        HighQuality = QualityMode.High,
        GammaCorrected,
        AssumeLinear
    }
}
