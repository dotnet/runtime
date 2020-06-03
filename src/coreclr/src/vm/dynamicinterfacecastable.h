// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _DYNAMICINTERFACECASTABLE_H_
#define _DYNAMICINTERFACECASTABLE_H_

namespace DynamicInterfaceCastable
{
    bool IsInstanceOf(OBJECTREF *objPROTECTED, const TypeHandle &interfaceTypeHandle);

    OBJECTREF GetInterfaceImplementation(OBJECTREF *objPROTECTED, const TypeHandle &interfaceTypeHandle);
}

#endif // _DYNAMICINTERFACECASTABLE_H_
