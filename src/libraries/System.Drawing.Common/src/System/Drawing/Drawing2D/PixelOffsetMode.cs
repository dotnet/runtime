// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing.Drawing2D
{
    public enum PixelOffsetMode
    {
        Invalid = QualityMode.Invalid,
        Default = QualityMode.Default,
        HighSpeed = QualityMode.Low,
        HighQuality = QualityMode.High,
        None,
        Half // offset by -0.5, -0.5 for fast anti-alias perf
    }
}
