// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//
 
//
// ==--==
//*****************************************************************************
// File: IPCEnums.h
//
// Define various enums used by IPCMan
//
//*****************************************************************************

#ifndef _IPCEnums_h_
#define _IPCEnums_h_



//enum for factoring security descriptor stuff...
enum EDescriptorType
{
    eDescriptor_Private = 0,
    eDescriptor_Public,

// MAX used for arrays, insert above this.
    eDescriptor_MAX
};


//-----------------------------------------------------------------------------
// Each IPC client for an IPC block has one entry.
// IMPORTANT: Do not remove any entries from this enumeration as
// the directory indexing cannot change from version to version
// in order to preserve compatibility.  The actual directory
// entry for an obsolete index can be zeroed (which all users
// should be written to handle for forward compatibility) but
// this enumeration can only be appended to.
//-----------------------------------------------------------------------------
enum EIPCClient
{
    eIPC_PerfCounters = 0,

// MAX used for arrays, insert above this.
    eIPC_MAX
};

//-----------------------------------------------------------------------------
// Each IPC client for a LegacyPrivate block (debugging, perf counters, etc)
// has one entry.
// IMPORTANT: Do not remove any entries from this enumeration as
// the directory indexing cannot change from version to version
// in order to preserve compatibility.  The actual directory
// entry for an obsolete index can be zeroed (which all users
// should be written to handle for forward compatibility) but
// this enumeration can only be appended to.
//-----------------------------------------------------------------------------
enum ELegacyPrivateIPCClient
{
    eLegacyPrivateIPC_PerfCounters = 0,
    eLegacyPrivateIPC_Obsolete_Debugger,
    eLegacyPrivateIPC_AppDomain,
    eLegacyPrivateIPC_Obsolete_Service,
    eLegacyPrivateIPC_Obsolete_ClassDump,
    eLegacyPrivateIPC_Obsolete_MiniDump,
    eLegacyPrivateIPC_InstancePath,

// MAX used for arrays, insert above this.
    eLegacyPrivateIPC_MAX
};

//-----------------------------------------------------------------------------
// Each IPC client for a LegacyPublic block has one entry.
// IMPORTANT: Do not remove any entries from this enumeration as
// the directory indexing cannot change from version to version
// in order to preserve compatibility.  The actual directory
// entry for an obsolete index can be zeroed (which all users
// should be written to handle for forward compatibility) but
// this enumeration can only be appended to.
//-----------------------------------------------------------------------------
enum ELegacyPublicIPCClient
{
    eLegacyPublicIPC_PerfCounters = 0,

// MAX used for arrays, insert above this.
    eLegacyPublicIPC_MAX
};

#endif // _IPCEnums_h_

