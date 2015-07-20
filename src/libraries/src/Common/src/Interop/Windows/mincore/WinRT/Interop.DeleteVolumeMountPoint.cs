// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

internal partial class Interop
{
    internal partial class mincore
    {
        internal static bool DeleteVolumeMountPoint(string mountPoint) 
        { 
            // DeleteVolumeMountPointW is not available to store apps. 
            // The expectation is that no store app would even have permission 
            // to call this from the app container 
            throw new UnauthorizedAccessException(); 
        } 
    }
}
