// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: SecurityConfig.cpp
//

//
//   Native implementation for security config access and manipulation
//


// #SecurityConfigFormat
// 
// The security config system resides outside of the rest
// of the config system since our needs are different.  The
// unmanaged portion of the security config system is only
// concerned with data file/cache file pairs, not what they
// are used for.  It performs all the duties of reading data
// from the disk, saving data back to the disk, and maintaining
// the policy and quick cache data structures.
//
// FILE FORMAT
//
// The data file is a purely opaque blob for the unmanaged
// code; however, the cache file is constructed and maintained
// completely in the unmanaged code.  It's format is as follows:
//
// CacheHeader
//  |
//  +-- dummyFileTime (FILETIME, 8 bytes) = this exists to make sure we don't read old format cache files. Must be set to {1, 0}.
//  |
//  +-- version (DWORD) = The version of this config file.
//  |
//  +-- configFileTime (FILETIME, 8 bytes) = The file time of the config file associated with this cache file.
//  |
//  +-- isSecurityOn (DWORD, 4 bytes) = This is currently not used.
//  |
//  +-- quickCache (DWORD, 4 bytes) = Used as a bitfield to maintain the information for the QuickCache.  See the QuickCache section for more details.
//  |
//  +-- registryExtensionsInfo (struct RegistryExtensionsInfo) = Indicates whether this cache file was generated in the presence of registry extensions.
//  |
//  +-- numEntries (DWORD, 4 bytes) = The number of policy cache entries in the latter portion of this cache file.
//  |
//  +-- sizeConfig (DWORD, 4 bytes) = The size of the config information stored in the latter portion of this cache file.
//
// Config Data (if any)
//     The cache file can include an entire copy of this
//     information in the adjoining config file.  This is
//     necessary since the cache often allows us to make
//     policy decisions without having parsed the data in 
//     the config file.  In order to guarantee that the config
//     data used by this process is not altered in the
//     meantime, we need to store the data in a readonly
//     location.  Due to the design of the caching system
//     the cache file is locked when it is opened and therefore
//     is the perfect place to store this information.  The
//     other alternative is to hold it in memory, but since
//     this can amount to many kilobytes of data we decided
//     on this design.
//
// List of CacheEntries
//  |
//  +-- CacheEntry
//  |    |
//  |    +-- numItemsInKey (DWORD, 4 bytes) = The number of evidence objects serialized in the key blob
//  |    |
//  |    +-- keySize (DWORD, 4 bytes) = The number of bytes in the key blob.
//  |    |
//  |    +-- dataSize (DWORD, 4 bytes) = The number of bytes in the data blob.
//  |    |     
//  |    +-- keyBlob (raw) = A raw blob representing the serialized evidence.
//  |    |
//  |    +-- dataBlob (raw) = A raw blob representing an XML serialized PolicyStatement
//  |
//  +-- ...

#include "common.h"

