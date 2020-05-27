// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "common.h"
#include "castableobject.h"

namespace
{
    OBJECTREF CallGetInterfaceImplementation(OBJECTREF *objPROTECTED, TypeHandle interfaceTypeHandle, BOOL throwIfNotFound)
    {
        PREPARE_NONVIRTUAL_CALLSITE(METHOD__ICASTABLEHELPERS__GET_INTERFACE_IMPLEMENTATION);

        OBJECTREF managedType = interfaceTypeHandle.GetManagedClassObject(); // GC triggers

        DECLARE_ARGHOLDER_ARRAY(args, 3);
        args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(*objPROTECTED);
        args[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(managedType);
        args[ARGNUM_2] = BOOL_TO_ARGHOLDER(throwIfNotFound);

        OBJECTREF implTypeRef;
        CALL_MANAGED_METHOD_RETREF(implTypeRef, OBJECTREF, args);
        INDEBUG(managedType = NULL); // managedType wasn't protected during the call

        return implTypeRef;
    }
}

bool CastableObject::IsInstanceOf(OBJECTREF *objPROTECTED, TypeHandle typeHandle, BOOL throwIfNotFound)
{
    CONTRACT(bool) {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(objPROTECTED != NULL);
        PRECONDITION(typeHandle.IsInterface());
    } CONTRACT_END;

    OBJECTREF implTypeObj = CallGetInterfaceImplementation(objPROTECTED, typeHandle, throwIfNotFound);
    RETURN (implTypeObj != NULL);
}

OBJECTREF CastableObject::GetInterfaceImplementation(OBJECTREF *objPROTECTED, TypeHandle typeHandle, BOOL throwIfNotFound)
{
    CONTRACT(OBJECTREF) {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(objPROTECTED != NULL);
        PRECONDITION(typeHandle.IsInterface());
    } CONTRACT_END;

    RETURN CallGetInterfaceImplementation(objPROTECTED, typeHandle, throwIfNotFound);
}
