// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//=============================================================================
//
// 
//
// Purpose: Define HResult constants returned by the Windows Modern Resource Manager
// and consumed by System.Resources.ResourceManager.
//
//===========================================================================*/
#if FEATURE_APPX
namespace System.Resources {
    using System;
    // Only static data no need to serialize
    internal static class __HResults
    {
        // From WinError.h
        public const int ERROR_MRM_MAP_NOT_FOUND = unchecked((int)0x80073B1F);
    }
}
#endif
