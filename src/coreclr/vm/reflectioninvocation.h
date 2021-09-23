// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#ifndef _REFLECTIONINVOCATION_H_
#define _REFLECTIONINVOCATION_H_

#include "object.h"
#include "fcall.h"
#include "field.h"
#include "stackwalktypes.h"
#include "runtimehandles.h"
#include "invokeutil.h"

// NOTE: The following constants are defined in BindingFlags.cs
#define BINDER_IgnoreCase           0x01
#define BINDER_DeclaredOnly         0x02
#define BINDER_Instance             0x04
#define BINDER_Static               0x08
#define BINDER_Public               0x10
#define BINDER_NonPublic            0x20
#define BINDER_FlattenHierarchy     0x40

#define BINDER_InvokeMethod         0x00100
#define BINDER_CreateInstance       0x00200
#define BINDER_GetField             0x00400
#define BINDER_SetField             0x00800
#define BINDER_GetProperty          0x01000
#define BINDER_SetProperty          0x02000
#define BINDER_PutDispProperty      0x04000
#define BINDER_PutRefDispProperty   0x08000

#define BINDER_ExactBinding         0x010000
#define BINDER_SuppressChangeType   0x020000
#define BINDER_OptionalParamBinding 0x040000

#define BINDER_IgnoreReturn         0x1000000
#define BINDER_DoNotWrapExceptions  0x2000000

#define BINDER_DefaultLookup        (BINDER_Instance | BINDER_Static | BINDER_Public)
#define BINDER_AllLookup            (BINDER_Instance | BINDER_Static | BINDER_Public | BINDER_Instance)

class ReflectionInvocation {

public:
    static
    void QCALLTYPE CompileMethod(MethodDesc * pMD);

    static
    void QCALLTYPE RunClassConstructor(QCall::TypeHandle pType);

    static
    void QCALLTYPE RunModuleConstructor(QCall::ModuleHandle pModule);

    static
    void QCALLTYPE PrepareMethod(MethodDesc* pMD, TypeHandle *pInstantiation, UINT32 cInstantiation);

    static FCDECL1(void, PrepareDelegate, Object* delegateUNSAFE);
    static FCDECL0(void, EnsureSufficientExecutionStack);
    static FCDECL0(FC_BOOL_RET, TryEnsureSufficientExecutionStack);

    // TypedReference functions, should go somewhere else
    static FCDECL4(void, MakeTypedReference, TypedByRef * value, Object* targetUNSAFE, ArrayBase* fldsUNSAFE, ReflectClassBaseObject *pFieldType);
    static FCDECL1(Object*, TypedReferenceToObject, TypedByRef * value);

#ifdef FEATURE_COMINTEROP
    static FCDECL8(Object*, InvokeDispMethod, ReflectClassBaseObject* refThisUNSAFE, StringObject* nameUNSAFE, INT32 invokeAttr, Object* targetUNSAFE, PTRArray* argsUNSAFE, PTRArray* byrefModifiersUNSAFE, LCID lcid, PTRArray* namedParametersUNSAFE);
#endif  // FEATURE_COMINTEROP
    static FCDECL2(void, GetGUID, ReflectClassBaseObject* refThisUNSAFE, GUID * result);
    static FCDECL2_IV(Object*, CreateEnum, ReflectClassBaseObject *pTypeUNSAFE, INT64 value);

    // helper fcalls for invocation
    static FCDECL2(FC_BOOL_RET, CanValueSpecialCast, ReflectClassBaseObject *valueType, ReflectClassBaseObject *targetType);
    static FCDECL3(Object*, AllocateValueType, ReflectClassBaseObject *targetType, Object *valueUNSAFE, CLR_BOOL fForceTypeChange);
};

class ReflectionSerialization {
public:
    static
    void QCALLTYPE GetUninitializedObject(QCall::TypeHandle pType, QCall::ObjectHandleOnStack retObject);
};

class ReflectionEnum {
public:
    static FCDECL1(Object *, InternalGetEnumUnderlyingType, ReflectClassBaseObject *target);
    static FCDECL1(INT32, InternalGetCorElementType, Object *pRefThis);

    static
    void QCALLTYPE GetEnumValuesAndNames(QCall::TypeHandle pEnumType, QCall::ObjectHandleOnStack pReturnValues, QCall::ObjectHandleOnStack pReturnNames, BOOL fGetNames);

    static FCDECL2_IV(Object*, InternalBoxEnum, ReflectClassBaseObject* pEnumType, INT64 value);
    static FCDECL2(FC_BOOL_RET, InternalEquals, Object *pRefThis, Object* pRefTarget);
    static FCDECL2(FC_BOOL_RET, InternalHasFlag, Object *pRefThis, Object* pRefFlags);
};

#endif // _REFLECTIONINVOCATION_H_
