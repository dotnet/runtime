// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Header: Registration.h
**
**
** Purpose: Native methods on System.Runtime.InteropServices.RegistrationServices
**

** 
===========================================================*/
#ifndef __REGISTRATION_H
#define __REGISTRATION_H

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

FCDECL2(VOID, RegisterTypeForComClientsNative, ReflectClassBaseObject* pTypeUNSAFE, GUID* pGuid);
FCDECL3(DWORD, RegisterTypeForComClientsExNative, ReflectClassBaseObject* pTypeUNSAFE, CLSCTX clsContext, REGCLS flags);

#endif
