// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

// Well-known GUID sentinel argument used for self-restart createdump.
// When a NativeAOT process crashes and createdump support is linked in,
// the process forks and re-executes itself with this sentinel as argv[1].
// The bootstrap checks for this sentinel before initializing the runtime
// and redirects into the dump-writing code path instead.
#define CREATEDUMP_SENTINEL "--{E89B44A7-9C62-4AB5-B292-FC7E1A25E949}"
