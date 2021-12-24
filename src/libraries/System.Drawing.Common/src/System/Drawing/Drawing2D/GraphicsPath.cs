// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.Drawing.Drawing2D
{
    public partial class GraphicsPath
    {
        private readonly SafeGraphicsPathHandle _nativePath;

        internal SafeGraphicsPathHandle SafeGraphicsPathHandle => _nativePath;

        private GraphicsPath(SafeGraphicsPathHandle nativePath)
        {
            _nativePath = nativePath;
        }

        public void Dispose()
        {
            _nativePath.Dispose();
        }
    }
}
