// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

/*
 * Note: This may be called at any time, but cannot be called concurrently
 * with (during and on a separate thread from) sgen init. Callers are
 * responsible for enforcing this.
 */
MONO_API_FUNCTION(void, mono_gc_register_bridge_callbacks, (MonoGCBridgeCallbacks *callbacks))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_gc_wait_for_bridge_processing, (void))
