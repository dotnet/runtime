// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing.Drawing2D
{
    public enum InterpolationMode
    {
        Invalid = QualityMode.Invalid,
        Default = QualityMode.Default,
        Low = QualityMode.Low,
        High = QualityMode.High,
        Bilinear,
        Bicubic,
        NearestNeighbor,
        HighQualityBilinear,
        HighQualityBicubic
    }
}
