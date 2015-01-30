// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//=============================================================================
//
// 
//
//
// Purpose: Define HResult constants. Every exception has one of these.
//
//
//===========================================================================*/
namespace System.IO {
    using System;
    // Only static data no need to serialize
    internal static class __HResults
    {
        // These use an error code from WinError.h
        public const int COR_E_ENDOFSTREAM = unchecked((int)0x80070026);  // OS defined
        public const int COR_E_FILELOAD = unchecked((int)0x80131621);
        public const int COR_E_FILENOTFOUND = unchecked((int)0x80070002);
        public const int COR_E_DIRECTORYNOTFOUND = unchecked((int)0x80070003);
        public const int COR_E_PATHTOOLONG = unchecked((int)0x800700CE);

        public const int COR_E_IO = unchecked((int)0x80131620);
    }
}
