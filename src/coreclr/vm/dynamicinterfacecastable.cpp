// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"
#include "dynamicinterfacecastable.h"

namespace
{
    BOOL CallIsInterfaceImplemented(OBJECTREF *objPROTECTED, const TypeHandle &interfaceTypeHandle, BOOL throwIfNotImplemented)
    {
        CONTRACT(BOOL) {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            PRECONDITION(objPROTECTED != NULL);
            PRECONDITION(interfaceTypeHandle.IsInterface());
            POSTCONDITION(!throwIfNotImplemented || RETVAL);
        } CONTRACT_END;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__DYNAMICINTERFACECASTABLEHELPERS__IS_INTERFACE_IMPLEMENTED);

        OBJECTREF managedType = interfaceTypeHandle.GetManagedClassObject(); // GC triggers

        DECLARE_ARGHOLDER_ARRAY(args, 3);
        args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(*objPROTECTED);
        args[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(managedType);
        args[ARGNUM_2] = BOOL_TO_ARGHOLDER(throwIfNotImplemented);

        BOOL isImplemented;
        CALL_MANAGED_METHOD(isImplemented, CLR_BOOL, args);
        INDEBUG(managedType = NULL); // managedType wasn't protected during the call

        RETURN isImplemented;
    }

    OBJECTREF CallGetInterfaceImplementation(OBJECTREF *objPROTECTED, const TypeHandle &interfaceTypeHandle)
    {
        CONTRACT(OBJECTREF) {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            PRECONDITION(objPROTECTED != NULL);
            PRECONDITION(interfaceTypeHandle.IsInterface());
            POSTCONDITION(RETVAL != NULL);
        } CONTRACT_END;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__DYNAMICINTERFACECASTABLEHELPERS__GET_INTERFACE_IMPLEMENTATION);

        OBJECTREF managedType = interfaceTypeHandle.GetManagedClassObject(); // GC triggers

        DECLARE_ARGHOLDER_ARRAY(args, 2);
        args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(*objPROTECTED);
        args[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(managedType);

        OBJECTREF implTypeRef;
        CALL_MANAGED_METHOD_RETREF(implTypeRef, OBJECTREF, args);
        INDEBUG(managedType = NULL); // managedType wasn't protected during the call

        RETURN implTypeRef;
    }
}

BOOL DynamicInterfaceCastable::IsInstanceOf(OBJECTREF *objPROTECTED, const TypeHandle &typeHandle, BOOL throwIfNotImplemented)
{
    CONTRACT(BOOL) {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(objPROTECTED != NULL);
        PRECONDITION(typeHandle.IsInterface());
    } CONTRACT_END;

    RETURN CallIsInterfaceImplemented(objPROTECTED, typeHandle, throwIfNotImplemented);
}

OBJECTREF DynamicInterfaceCastable::GetInterfaceImplementation(OBJECTREF *objPROTECTED, const TypeHandle &typeHandle)
{
    CONTRACT(OBJECTREF) {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(objPROTECTED != NULL);
        PRECONDITION(typeHandle.IsInterface());
        POSTCONDITION(RETVAL != NULL);
    } CONTRACT_END;

    RETURN CallGetInterfaceImplementation(objPROTECTED, typeHandle);
}
