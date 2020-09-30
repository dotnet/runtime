// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _DYNAMICINTERFACECASTABLE_H_
#define _DYNAMICINTERFACECASTABLE_H_

namespace DynamicInterfaceCastable
{
    BOOL IsInstanceOf(OBJECTREF *objPROTECTED, const TypeHandle &interfaceTypeHandle, BOOL throwIfNotImplemented);

    OBJECTREF GetInterfaceImplementation(OBJECTREF *objPROTECTED, const TypeHandle &interfaceTypeHandle);
}

#endif // _DYNAMICINTERFACECASTABLE_H_
