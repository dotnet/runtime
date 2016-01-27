// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
