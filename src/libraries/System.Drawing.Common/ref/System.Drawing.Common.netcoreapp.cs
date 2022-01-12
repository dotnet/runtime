// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Drawing
{
    public sealed partial class Graphics
    {
        public System.Numerics.Matrix3x2 TransformElements { get { throw null; } set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        [System.ObsoleteAttribute("Use the Graphics.GetContextInfo overloads that accept arguments for better performance and fewer allocations.", DiagnosticId = "SYSLIB0016", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public object GetContextInfo() { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public void GetContextInfo(out PointF offset) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public void GetContextInfo(out PointF offset, out Region? clip) { throw null; }
    }
}
namespace System.Drawing.Drawing2D
{
    public sealed partial class Matrix
    {
        public Matrix(System.Numerics.Matrix3x2 matrix) { }
        public System.Numerics.Matrix3x2 MatrixElements { get { throw null; } set { } }
    }
}
