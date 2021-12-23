// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    public partial class Region
    {
        public void ReleaseHrgn(IntPtr regionHandle)
        {
            if (regionHandle == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(regionHandle));
            }

            // for libgdiplus HRGN == GpRegion*, and we check the return code
            SafeRegionHandle nativeRegion = SafeRegionHandle;
            bool releaseThisRegion = false;
            try
            {
                nativeRegion.DangerousAddRef(ref releaseThisRegion);
                int status = Gdip.GdipDeleteRegion(nativeRegion.DangerousGetHandle());
                Gdip.CheckStatus(status);
            }
            finally
            {
                if (releaseThisRegion)
                {
                    nativeRegion.DangerousRelease();
                }
            }
        }
    }
}
