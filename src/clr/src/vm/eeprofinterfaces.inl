//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 
// EEProfInterfaces.inl
// 

//
// Inline function implementations for common types used internally in the EE to support
// issuing profiling API callbacks
// 

// ======================================================================================

#ifndef DACCESS_COMPILE

FORCEINLINE BOOL TrackAllocations()
{
#ifdef PROFILING_SUPPORTED
    return CORProfilerTrackAllocations();
#else
    return FALSE;
#endif // PROFILING_SUPPORTED
}


#endif
