// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __NUMASUPPORT_H__
#define __NUMASUPPORT_H__

#include "config.gc.h"

#if HAVE_NUMA_H

#include <numa.h>
#include <numaif.h>

#endif // HAVE_NUMA_H

void NUMASupportInitialize();
void NUMASupportCleanup();

#if HAVE_NUMA_H

// List of all functions from the numa library that are used
#define FOR_ALL_NUMA_FUNCTIONS \
    PER_FUNCTION_BLOCK(mbind) \
    PER_FUNCTION_BLOCK(numa_available) \
    PER_FUNCTION_BLOCK(numa_max_node) \
    PER_FUNCTION_BLOCK(numa_node_of_cpu)

// Declare pointers to all the used numa functions
#define PER_FUNCTION_BLOCK(fn) extern decltype(fn)* fn##_ptr;
FOR_ALL_NUMA_FUNCTIONS
#undef PER_FUNCTION_BLOCK

// Redefine all calls to numa functions as calls through pointers that are set
// to the functions of libnuma in the initialization.
#define mbind(...) mbind_ptr(__VA_ARGS__)
#define numa_available() numa_available_ptr()
#define numa_max_node() numa_max_node_ptr()
#define numa_node_of_cpu(...) numa_node_of_cpu_ptr(__VA_ARGS__)

#endif // HAVE_NUMA_H

#endif // __NUMASUPPORT_H__
