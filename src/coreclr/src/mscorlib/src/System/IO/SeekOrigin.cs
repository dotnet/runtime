// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Enum:   SeekOrigin
** 
** 
**
**
** Purpose: Enum describing locations in a stream you could
** seek relative to.
**
**
===========================================================*/

using System;

namespace System.IO {
    // Provides seek reference points.  To seek to the end of a stream,
    // call stream.Seek(0, SeekOrigin.End).
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum SeekOrigin
    {
        // These constants match Win32's FILE_BEGIN, FILE_CURRENT, and FILE_END
        Begin = 0,
        Current = 1,
        End = 2,
    }
}
