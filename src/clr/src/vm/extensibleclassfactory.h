// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Header: ExtensibleClassFactory.h
**
**
** Purpose: Native methods on System.Runtime.InteropServices.ExtensibleClassFactory
**

** 
===========================================================*/

#ifndef _EXTENSIBLECLASSFACTORY_H
#define _EXTENSIBLECLASSFACTORY_H

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

// Register a delegate that will be called whenever an instance of a
// managed type that extends from an unmanaged type needs to allocate
// the aggregated unmanaged object. This delegate is expected to
// allocate and aggregate the unmanaged object and is called in place
// of a CoCreateInstance. This routine must be called in the context
// of the static initializer for the class for which the callbacks
// will be made.
// It is not legal to register this callback from a class that has any
// parents that have already registered a callback.
FCDECL1(void, RegisterObjectCreationCallback, Object* pDelegateUNSAFE);


#endif
