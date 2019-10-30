// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//-----------------------------------------------------------------------------
// Entrypoint markers
// Used to identify all external entrypoints into the CLR (via COM, exports, etc)
// and perform various tasks on all of them
//-----------------------------------------------------------------------------


#ifndef __ENTRYPOINTS_h__
#define __ENTRYPOINTS_h__

#define BEGIN_ENTRYPOINT_THROWS
#define END_ENTRYPOINT_THROWS
#define BEGIN_ENTRYPOINT_THROWS_WITH_THREAD(____thread)
#define END_ENTRYPOINT_THROWS_WITH_THREAD
#define BEGIN_ENTRYPOINT_NOTHROW_WITH_THREAD(___thread)
#define END_ENTRYPOINT_NOTHROW_WITH_THREAD
#define BEGIN_ENTRYPOINT_NOTHROW
#define END_ENTRYPOINT_NOTHROW
#define BEGIN_ENTRYPOINT_VOIDRET
#define END_ENTRYPOINT_VOIDRET
#define BEGIN_CLEANUP_ENTRYPOINT
#define END_CLEANUP_ENTRYPOINT

#endif  // __ENTRYPOINTS_h__


