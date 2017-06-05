// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Internal
{
    /// <devdoc>
    ///    Specifies the graphics mode of a device context.
    /// </devdoc>
    internal enum DeviceContextGraphicsMode
    {
        /*
        Sets the graphics mode that is compatible with 16-bit Windows. This is the default mode. 
        If this value is specified, the application can only modify the world-to-device transform 
        by calling functions that set window and viewport extents and origins, but not by using 
        SetWorldTransform or ModifyWorldTransform; calls to those functions will fail. 
        Examples of functions that set window and viewport extents and origins are SetViewportExtEx 
        and SetWindowExtEx.
        */
        Compatible = 1,

        /*
        Windows NT/2000/XP: Sets the advanced graphics mode that allows world transformations. This value 
        must be specified if the application will set or modify the world transformation for the specified 
        device context. In this mode all graphics, including text output, fully conform to the world-to-device 
        transformation specified in the device context. 
        Windows 95/98/Me:The GM_ADVANCED value is not supported. When playing enhanced metafiles, the system 
        attempts to make enhanced metafiles look the same as they do on Windows NT/2000/XP. To accomplish this, 
        the system may simulate GM_ADVANCED mode when playing specific enhanced metafile records. 
        */
        Advanced = 2,

        /*
        Resets the current world transformation by using the identity matrix. If this mode is specified, 
        the XFORM structure pointed to by lpXform is ignored. 
        */
        ModifyWorldIdentity = 1

        /*
        GM_COMPATIBLE       = 1,
        GM_ADVANCED         = 2,
        
        MWT_IDENTITY        = 1,
        MWT_LEFTMULTIPLY  // Multiplies the current transformation by the data in the XFORM structure. (The data in the XFORM structure becomes the left multiplicand, and the data for the current transformation becomes the right multiplicand.) 
        MWT_RIGHTMULTIPLY // Multiplies the current transformation by the data in the XFORM structure. (The data in the XFORM structure becomes the right multiplicand, and the data for the current transformation becomes the left multiplicand.) 
        */
    }
}
