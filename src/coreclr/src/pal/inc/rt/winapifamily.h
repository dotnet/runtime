//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

//
// ===========================================================================
// File: windows.h
// 
// ===========================================================================
// dummy winapifamily.h for PAL

#ifndef _INC_WINAPIFAMILY
#define _INC_WINAPIFAMILY

//
// Windows APIs can be placed in a partition represented by one of the below bits.   The 
// WINAPI_FAMILY value determines which partitions are available to the client code.
//

#define WINAPI_PARTITION_DESKTOP   0x00000001
#define WINAPI_PARTITION_APP       0x00000002    

// A family may be defined as the union of multiple families. WINAPI_FAMILY should be set
// to one of these values.
#define WINAPI_FAMILY_APP          WINAPI_PARTITION_APP
#define WINAPI_FAMILY_DESKTOP_APP  (WINAPI_PARTITION_DESKTOP | WINAPI_PARTITION_APP)    

// Provide a default for WINAPI_FAMILY if needed.  
#ifndef WINAPI_FAMILY
#define WINAPI_FAMILY WINAPI_FAMILY_DESKTOP_APP
#endif

// Macro to determine if a partition is enabled
#define WINAPI_FAMILY_PARTITION(Partition)	((WINAPI_FAMILY & Partition) == Partition)

// Macro to determine if only one partition is enabled from a set
#define WINAPI_FAMILY_ONE_PARTITION(PartitionSet, Partition) ((WINAPI_FAMILY & PartitionSet) == Partition)

#endif
