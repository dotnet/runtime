//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef __RESOURCE_STRING_H_
#define __RESOURCE_STRING_H_

// Struct to contain a resource ID and its corresponding 
// English language string.
struct NativeStringResource
{
    unsigned int resourceId;
    const char* resourceString;
};

// Sorted array of all native string resources
extern const NativeStringResource nativeStringResources[];

// Number of entries in nativeStringResources
extern const int NUMBER_OF_NATIVE_STRING_RESOURCES;

#endif // __RESOURCE_STRING_H_
