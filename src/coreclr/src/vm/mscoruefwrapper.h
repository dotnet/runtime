//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

//
//*****************************************************************************
// MSCorUEFWrapper.h - Wrapper for including the UEF chain manager definition
//                     and the global that references it for VM usage.
//*****************************************************************************

#ifdef FEATURE_UEF_CHAINMANAGER

// This is required to register our UEF callback with the UEF chain manager
#include <mscoruef.h>
// Global reference to IUEFManager that will be used in the VM
extern IUEFManager * g_pUEFManager;

#endif // FEATURE_UEF_CHAINMANAGER
