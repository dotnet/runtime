// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _CASTABLEOBJECT_H_
#define _CASTABLEOBJECT_H_

namespace CastableObject
{
    bool IsInstanceOf(OBJECTREF *objPROTECTED, const TypeHandle &interfaceTypeHandle, BOOL throwIfNotFound);

    OBJECTREF GetInterfaceImplementation(OBJECTREF *objPROTECTED, const TypeHandle &interfaceTypeHandle, BOOL throwIfNotFound);
}

#endif // _CASTABLEOBJECT_H_
