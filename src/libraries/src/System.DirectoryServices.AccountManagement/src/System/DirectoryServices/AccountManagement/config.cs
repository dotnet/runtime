// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// This file controls whether we're building for Longhorn or Whidbey.
// There are four #define constants which control which protocol stacks will be included
// in the API:
//
// PAPI_AD          Active Directory
// PAPI_REGSAM      Registry-SAM
// PAPI_MSAM        Machine SAM (implies PAPI_REGSAM as well)
//
// The desired combination of these, in turn, is selected by specifying either FLAVOR_LONGHORN
// or FLAVOR_WHIDBEY.
//

// Given the flavor of the API we're building, select the appropriate protocol stacks to include
#if FLAVOR_LONGHORN

    #define PAPI_AD
    #define PAPI_MSAM

#elif FLAVOR_WHIDBEY

    #define PAPI_AD
    #define PAPI_REGSAM

#else

    #error "Must define either FLAVOR_LONGHORN or FLAVOR_WHIDBEY"

#endif

// MSAM is built on top of Reg-SAM.  If we're building PAPI_MSAM, need to enable PAPI_REGSAM as well.
#if PAPI_MSAM
    #define PAPI_REGSAM
#endif

