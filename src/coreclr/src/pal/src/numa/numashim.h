// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Enable calling numa functions through shims to make it a soft
// runtime dependency.

#ifndef __NUMASHIM_H__
#define __NUMASHIM_H__

#if HAVE_NUMA_H

#include <numa.h>
#include <numaif.h>

#define numa_free_cpumask numa_bitmask_free

// List of all functions from the numa library that are used
#define FOR_ALL_NUMA_FUNCTIONS \
    PER_FUNCTION_BLOCK(numa_available) \
    PER_FUNCTION_BLOCK(mbind) \
    PER_FUNCTION_BLOCK(numa_num_possible_cpus) \
    PER_FUNCTION_BLOCK(numa_max_node) \
    PER_FUNCTION_BLOCK(numa_allocate_cpumask) \
    PER_FUNCTION_BLOCK(numa_node_to_cpus) \
    PER_FUNCTION_BLOCK(numa_bitmask_weight) \
    PER_FUNCTION_BLOCK(numa_bitmask_isbitset) \
    PER_FUNCTION_BLOCK(numa_bitmask_free)

// Declare pointers to all the used numa functions
#define PER_FUNCTION_BLOCK(fn) extern decltype(fn)* fn##_ptr;
FOR_ALL_NUMA_FUNCTIONS
#undef PER_FUNCTION_BLOCK

// Redefine all calls to numa functions as calls through pointers that are set
// to the functions of libnuma in the initialization.
#define numa_available() numa_available_ptr()
#define mbind(...) mbind_ptr(__VA_ARGS__)
#define numa_num_possible_cpus() numa_num_possible_cpus_ptr()
#define numa_max_node() numa_max_node_ptr()
#define numa_allocate_cpumask() numa_allocate_cpumask_ptr()
#define numa_node_to_cpus(...) numa_node_to_cpus_ptr(__VA_ARGS__)
#define numa_bitmask_weight(...) numa_bitmask_weight_ptr(__VA_ARGS__)
#define numa_bitmask_isbitset(...) numa_bitmask_isbitset_ptr(__VA_ARGS__)
#define numa_bitmask_free(...) numa_bitmask_free_ptr(__VA_ARGS__)

#endif // HAVE_NUMA_H

#endif // __NUMASHIM_H__
