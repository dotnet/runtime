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

        struct
        {
            OBJECTREF managedType;
        } gc;
        gc.managedType = NULL;
        CLR_BOOL isImplemented = FALSE;
        GCPROTECT_BEGIN(gc);
        gc.managedType = interfaceTypeHandle.GetManagedClassObject(); // GC triggers

        UnmanagedCallersOnlyCaller isInterfaceImplemented(METHOD__DYNAMICINTERFACECASTABLEHELPERS__IS_INTERFACE_IMPLEMENTED);
        isInterfaceImplemented.InvokeThrowing(objPROTECTED, &gc.managedType, CLR_BOOL_ARG(throwIfNotImplemented), &isImplemented);
        GCPROTECT_END();

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

        struct
        {
            OBJECTREF managedType;
            OBJECTREF result;
        } gc;
        gc.managedType = NULL;
        gc.result = NULL;
        GCPROTECT_BEGIN(gc);
        gc.managedType = interfaceTypeHandle.GetManagedClassObject(); // GC triggers

        UnmanagedCallersOnlyCaller getInterfaceImplementation(METHOD__DYNAMICINTERFACECASTABLEHELPERS__GET_INTERFACE_IMPLEMENTATION);
        getInterfaceImplementation.InvokeThrowing(objPROTECTED, &gc.managedType, &gc.result);
        GCPROTECT_END();

        RETURN gc.result;
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
