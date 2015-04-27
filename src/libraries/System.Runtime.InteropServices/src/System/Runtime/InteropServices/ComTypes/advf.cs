// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.InteropServices.ComTypes
{
    [Flags]
    public enum ADVF
    {
        ADVF_NODATA = 1,
        ADVF_PRIMEFIRST = 2,
        ADVF_ONLYONCE = 4,
        ADVF_DATAONSTOP = 64,
        ADVFCACHE_NOHANDLER = 8,
        ADVFCACHE_FORCEBUILTIN = 16,
        ADVFCACHE_ONSAVE = 32
    }
    // Note: ADVF_ONLYONCE and ADVF_PRIMEFIRST values conform with objidl.dll but are backwards from 
    // the Platform SDK documentation as of 07/21/2003.
    // http://msdn.microsoft.com/library/default.asp?url=/library/en-us/com/htm/oen_a2z_8jxi.asp.
}
