// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _RUNTIMEHANDLES_H_
#define _RUNTIMEHANDLES_H_

#include "object.h"
#include "typehandle.h"
#include "fcall.h"
#include "field.h"
#include "typectxt.h"

typedef void* EnregisteredTypeHandle;

// NOTE: These are defined in CallingConventions.cs.
typedef enum ReflectionCallConv {
    CALLCONV_Standard       = 0x0001,
    CALLCONV_VarArgs        = 0x0002,
    CALLCONV_Any            = CALLCONV_Standard | CALLCONV_VarArgs,
    CALLCONV_HasThis        = 0x0020,
    CALLCONV_ExplicitThis   = 0x0040,
    CALLCONV_ArgIteratorFlags = 0xFFFFFF00, // PRIVATE member -- cached ArgIterator flags -- Not exposed in CallingConventions.cs
    CALLCONV_ArgIteratorFlags_Shift = 8,
} ReflectionCallConv;


// Types used to expose method bodies via reflection.

class RuntimeExceptionHandlingClause;
class RuntimeMethodBody;
class RuntimeLocalVariableInfo;

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<RuntimeExceptionHandlingClause> RUNTIMEEXCEPTIONHANDLINGCLAUSEREF;
typedef REF<RuntimeMethodBody> RUNTIMEMETHODBODYREF;
typedef REF<RuntimeLocalVariableInfo> RUNTIMELOCALVARIABLEINFOREF;
#else
typedef DPTR(RuntimeExceptionHandlingClause) RUNTIMEEXCEPTIONHANDLINGCLAUSEREF;
typedef DPTR(RuntimeMethodBody) RUNTIMEMETHODBODYREF;
typedef DPTR(RuntimeLocalVariableInfo) RUNTIMELOCALVARIABLEINFOREF;
#endif

class RuntimeExceptionHandlingClause : public Object
{
public:
    // Disallow creation and copy construction of these.
    RuntimeExceptionHandlingClause() = delete;
    RuntimeExceptionHandlingClause(const RuntimeExceptionHandlingClause&)  = delete;

    RUNTIMEMETHODBODYREF _methodBody;
    CorExceptionFlag _flags;
    INT32 _tryOffset;
    INT32 _tryLength;
    INT32 _handlerOffset;
    INT32 _handlerLength;
    mdTypeDef _catchToken;
    INT32 _filterOffset;
};

class RuntimeMethodBody : public Object
{
public:
    // Disallow creation and copy construction of these.
    RuntimeMethodBody() = delete;
    RuntimeMethodBody(const RuntimeMethodBody&) = delete;

    U1ARRAYREF _IL;
    PTRARRAYREF _exceptionClauses;
    PTRARRAYREF _localVariables;
    OBJECTREF _methodBase;

    INT32 _localVarSigToken;
    INT32 _maxStackSize;
    CLR_BOOL _initLocals;
};

class RuntimeLocalVariableInfo : public Object
{
public:
    // Disallow creation and copy construction of these.
    RuntimeLocalVariableInfo() = delete;
    RuntimeLocalVariableInfo(const RuntimeLocalVariableInfo&) = delete;

    REFLECTCLASSBASEREF GetType()
    {
        return (REFLECTCLASSBASEREF)_type;
    }

    void SetType(OBJECTREF type)
    {
        SetObjectReference(&_type, type);
    }

    OBJECTREF _type;
    INT32 _localIndex;
    CLR_BOOL _isPinned;
};

extern "C" BOOL QCALLTYPE MdUtf8String_EqualsCaseInsensitive(LPCUTF8 szLhs, LPCUTF8 szRhs, INT32 stringNumBytes, QCallException* qcallError);

class RuntimeTypeHandle
{
public:
    ReflectClassBaseObject *pRuntimeTypeDONOTUSEDIRECTLY;

    // Static method on RuntimeTypeHandle
    FCDECL1(static AssemblyBaseObject*, GetAssemblyIfExists, ReflectClassBaseObject *pType);
    FCDECL1(static ReflectModuleBaseObject*, GetModuleIfExists, ReflectClassBaseObject* pType);
    FCDECL1(static INT32, GetAttributes, ReflectClassBaseObject* pType);
    FCDECL1(static INT32, GetToken, ReflectClassBaseObject* pType);
    FCDECL1(static LPCUTF8, GetUtf8Name, MethodTable* pMT);
    FCDECL1(static INT32, GetArrayRank, ReflectClassBaseObject* pType);

    FCDECL1(static FC_BOOL_RET, IsUnmanagedFunctionPointer, ReflectClassBaseObject *pTypeUNSAFE);

    FCDECL1(static FC_BOOL_RET, IsGenericVariable, PTR_ReflectClassBaseObject pType);

    FCDECL1(static INT32, GetGenericVariableIndex, PTR_ReflectClassBaseObject pType);

    FCDECL1(static FC_BOOL_RET, ContainsGenericVariables, PTR_ReflectClassBaseObject pType);

    FCDECL2(static FC_BOOL_RET, CompareCanonicalHandles, PTR_ReflectClassBaseObject pLeft, PTR_ReflectClassBaseObject pRight);
    FCDECL1(static Object*, InternalAllocNoChecks_FastPath, MethodTable* pMT);

    FCDECL1(static EnregisteredTypeHandle, GetElementTypeHandle, EnregisteredTypeHandle th);
    FCDECL1(static INT32, GetNumVirtuals, ReflectClassBaseObject *pType);

    FCDECL1(static MethodDesc *, GetFirstIntroducedMethod, ReflectClassBaseObject* pType);
    FCDECL1(static void, GetNextIntroducedMethod, MethodDesc **ppMethod);
};

extern "C" void QCALLTYPE RuntimeTypeHandle_GetRuntimeTypeFromHandleSlow(void* typeHandleRaw, QCall::ObjectHandleOnStack result, QCallException* qcallError);
#ifdef FEATURE_TYPEEQUIVALENCE
extern "C" BOOL QCALLTYPE RuntimeTypeHandle_IsEquivalentTo(QCall::TypeHandle rtType1, QCall::TypeHandle rtType2, QCallException* qcallError);
#endif // FEATURE_TYPEEQUIVALENCE
extern "C" void QCALLTYPE RuntimeTypeHandle_CreateInstanceForAnotherGenericParameter(QCall::TypeHandle pTypeHandle, TypeHandle *pInstArray, INT32 cInstArray, QCall::ObjectHandleOnStack pInstantiatedObject, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_InternalAlloc(MethodTable* pMT, QCall::ObjectHandleOnStack allocated, QCallException* qcallError);

extern "C" void QCALLTYPE RuntimeTypeHandle_InternalAllocNoChecks(MethodTable* pMT, QCall::ObjectHandleOnStack allocated, QCallException* qcallError);
extern "C" void* QCALLTYPE RuntimeTypeHandle_AllocateTypeAssociatedMemory(QCall::TypeHandle type, uint32_t size, QCallException* qcallError);
extern "C" void* QCALLTYPE RuntimeTypeHandle_AllocateTypeAssociatedMemoryAligned(QCall::TypeHandle type, uint32_t size, uint32_t alignment, QCallException* qcallError);

extern "C" PVOID QCALLTYPE QCall_GetGCHandleForTypeHandle(QCall::TypeHandle pTypeHandle, INT32 handleType, QCallException* qcallError);
extern "C" void QCALLTYPE QCall_FreeGCHandleForTypeHandle(QCall::TypeHandle pTypeHandle, OBJECTHANDLE objHandle, QCallException* qcallError);

extern "C" void QCALLTYPE RuntimeTypeHandle_GetActivationInfo(
    QCall::ObjectHandleOnStack pRuntimeType,
    PCODE* ppfnAllocator,
    void** pvAllocatorFirstArg,
    PCODE* ppfnCtor,
    PCODE* ppfnValueCtor,
    BOOL* pfCtorIsPublic,
    QCallException* qcallError);
#ifdef FEATURE_COMINTEROP
extern "C" void QCALLTYPE RuntimeTypeHandle_AllocateComObject(void* pClassFactory, QCall::ObjectHandleOnStack result, QCallException* qcallError);
#endif // FEATURE_COMINTEROP
extern "C" void QCALLTYPE RuntimeTypeHandle_MakeByRef(QCall::TypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_MakePointer(QCall::TypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_MakeSZArray(QCall::TypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_MakeArray(QCall::TypeHandle pTypeHandle, INT32 rank, QCall::ObjectHandleOnStack retType, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_MakeFunctionPointer(TypeHandle* pRetAndArgTypes, INT32 numArgs, BOOL isUnmanaged, QCall::ObjectHandleOnStack retType, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_PrepareMemberInfoCache(QCall::TypeHandle pMemberInfoCache);
extern "C" void QCALLTYPE RuntimeTypeHandle_ConstructName(QCall::TypeHandle pTypeHandle, DWORD format, QCall::StringHandleOnStack retString, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_GetInterfaces(MethodTable* pMT, QCall::ObjectHandleOnStack result, QCallException* qcallError);
extern "C" BOOL QCALLTYPE RuntimeTypeHandle_IsVisible(QCall::TypeHandle pTypeHandle, QCallException* qcallError);
extern "C" BOOL QCALLTYPE RuntimeTypeHandle_SatisfiesConstraints(QCall::TypeHandle paramType, QCall::TypeHandle typeContext, MethodDesc* methodContext, QCall::TypeHandle toType, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_GetInstantiation(QCall::TypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType, BOOL fAsRuntimeTypeArray, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_Instantiate(QCall::TypeHandle pTypeHandle, TypeHandle * pInstArray, INT32 cInstArray, QCall::ObjectHandleOnStack retType, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_GetGenericTypeDefinition(QCall::TypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_GetConstraints(QCall::TypeHandle pTypeHandle, QCall::ObjectHandleOnStack retTypes, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_GetArgumentTypesFromFunctionPointer(QCall::TypeHandle pTypeHandle, QCall::ObjectHandleOnStack argTypes, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_GetAssemblySlow(QCall::ObjectHandleOnStack type, QCall::ObjectHandleOnStack assembly, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_GetModuleSlow(QCall::ObjectHandleOnStack type, QCall::ObjectHandleOnStack module, QCallException* qcallError);
extern "C" INT32 QCALLTYPE RuntimeTypeHandle_GetNumVirtualsAndStaticVirtuals(QCall::TypeHandle pTypeHandle, QCallException* qcallError);
extern "C" MethodDesc* QCALLTYPE RuntimeTypeHandle_GetMethodAt(MethodTable* pMT, INT32 slot, QCallException* qcallError);
extern "C" BOOL QCALLTYPE RuntimeTypeHandle_GetFields(MethodTable* pMT, intptr_t* result, INT32* pCount, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_VerifyInterfaceIsImplemented(QCall::TypeHandle pTypeHandle, QCall::TypeHandle pIFaceHandle, QCallException* qcallError);
extern "C" MethodDesc* QCALLTYPE RuntimeTypeHandle_GetInterfaceMethodImplementation(QCall::TypeHandle pTypeHandle, QCall::TypeHandle pOwner, MethodDesc * pMD, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_GetDeclaringMethodForGenericParameter(QCall::TypeHandle pTypeHandle, QCall::ObjectHandleOnStack result, QCallException* qcallError);
extern "C" EnregisteredTypeHandle QCALLTYPE RuntimeTypeHandle_GetDeclaringTypeHandleForGenericVariable(EnregisteredTypeHandle pTypeHandle, QCallException* qcallError);
extern "C" EnregisteredTypeHandle QCALLTYPE RuntimeTypeHandle_GetDeclaringTypeHandle(EnregisteredTypeHandle pTypeHandle, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeTypeHandle_RegisterCollectibleTypeDependency(QCall::TypeHandle pTypeHandle, QCall::AssemblyHandle pAssembly, QCallException* qcallError);

class RuntimeMethodHandle
{
public:
    FCDECL1(static FC_BOOL_RET, IsCollectible, MethodDesc *pMethod);
    FCDECL1(static INT32, GetAttributes, MethodDesc *pMethod);
    FCDECL1(static INT32, GetImplAttributes, ReflectMethodObject *pMethodUNSAFE);
    FCDECL1(static MethodTable*, GetMethodTable, MethodDesc *pMethod);
    FCDECL1(static INT32, GetSlot, MethodDesc *pMethod);
    FCDECL1(static INT32, GetMethodDef, MethodDesc *pMethod);
    FCDECL1(static LPCUTF8, GetUtf8Name, MethodDesc *pMethod);
    FCDECL1(static FC_BOOL_RET, HasMethodInstantiation, MethodDesc *pMethod);

    FCDECL1(static FC_BOOL_RET, IsGenericMethodDefinition, MethodDesc *pMethod);

    FCDECL1(static FC_BOOL_RET, IsTypicalMethodDefinition, ReflectMethodObject *pMethodUNSAFE);

    FCDECL1(static INT32, GetGenericParameterCount, MethodDesc * pMethod);

    // see comment in the cpp file
    FCDECL2(static MethodDesc*, GetStubIfNeededInternal, MethodDesc *pMethod, ReflectClassBaseObject *pType);
    FCDECL2(static MethodDesc*, GetMethodFromCanonical, MethodDesc *pMethod, PTR_ReflectClassBaseObject pType);

    FCDECL1(static FC_BOOL_RET, IsDynamicMethod, MethodDesc * pMethod);

    FCDECL1(static Object*, GetResolver, MethodDesc * pMethod);

    FCDECL1(static FC_BOOL_RET, IsConstructor, MethodDesc *pMethod);

    FCDECL1(static FC_BOOL_RET, IsAsyncMethod, MethodDesc *pMethod);

    FCDECL1(static Object*, GetLoaderAllocatorInternal, MethodDesc *pMethod);
};

extern "C" MethodDesc* QCALLTYPE MethodBase_GetCurrentMethod(QCall::StackCrawlMarkHandle stackMark, QCallException* qcallError);

extern "C" BOOL QCALLTYPE RuntimeMethodHandle_IsCAVisibleFromDecoratedType(
        QCall::TypeHandle targetTypeHandle,
        MethodDesc * pTargetCtor,
        QCall::TypeHandle sourceTypeHandle,
        QCall::ModuleHandle sourceModuleHandle,
        QCallException* qcallError);

extern "C" void QCALLTYPE RuntimeMethodHandle_GetMethodInstantiation(MethodDesc * pMethod, QCall::ObjectHandleOnStack retTypes, BOOL fAsRuntimeTypeArray, QCallException* qcallError);

extern "C" void QCALLTYPE RuntimeMethodHandle_InvokeMethod(
    QCall::ObjectHandleOnStack target,
    PVOID* args,
    QCall::ObjectHandleOnStack pSigUNSAFE,
    BOOL fConstructor,
    QCall::ObjectHandleOnStack result,
    QCallException* qcallError);

extern "C" void QCALLTYPE RuntimeMethodHandle_ConstructInstantiation(MethodDesc * pMethod, DWORD format, QCall::StringHandleOnStack retString, QCallException* qcallError);
extern "C" void* QCALLTYPE RuntimeMethodHandle_GetFunctionPointer(MethodDesc * pMethod, QCallException* qcallError);
extern "C" BOOL QCALLTYPE RuntimeMethodHandle_GetIsCollectible(MethodDesc * pMethod);
extern "C" void QCALLTYPE RuntimeMethodHandle_GetTypicalMethodDefinition(MethodDesc * pMethod, QCall::ObjectHandleOnStack refMethod, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeMethodHandle_StripMethodInstantiation(MethodDesc * pMethod, QCall::ObjectHandleOnStack refMethod, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeMethodHandle_Destroy(MethodDesc * pMethod, QCallException* qcallError);
extern "C" MethodDesc* QCALLTYPE RuntimeMethodHandle_GetStubIfNeededSlow(MethodDesc* pMethod, QCall::TypeHandle declaringTypeHandle, QCall::ObjectHandleOnStack methodInstantiation, QCallException* qcallError);
extern "C" PCODE QCALLTYPE RuntimeMethodHandle_GetNativeCode(MethodDesc* pMethod, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeMethodHandle_GetMethodBody(MethodDesc* pMethod, QCall::TypeHandle pDeclaringType, QCall::ObjectHandleOnStack result, QCallException* qcallError);

class RuntimeFieldHandle
{
public:
    FCDECL1(static FC_BOOL_RET, IsFastPathSupported, ReflectFieldObject *pField);
    FCDECL1(static INT32, GetInstanceFieldOffset, ReflectFieldObject *pField);
    FCDECL1(static void*, GetStaticFieldAddress, ReflectFieldObject *pField);
    FCDECL1(static LPCUTF8, GetUtf8Name, FieldDesc *pField);

    FCDECL1(static INT32, GetAttributes, FieldDesc *pField);
    FCDECL1(static MethodTable*, GetApproxDeclaringMethodTable, FieldDesc *pField);
    FCDECL1(static INT32, GetToken, FieldDesc* pField);
    FCDECL2(static FieldDesc*, GetStaticFieldForGenericType, FieldDesc *pField, MethodTable *pMT);
    FCDECL1(static FC_BOOL_RET, AcquiresContextFromThis, FieldDesc *pField);
    FCDECL1(static Object*, GetLoaderAllocatorInternal, FieldDesc *pField);
};

extern "C" void QCALLTYPE RuntimeFieldHandle_GetValue(FieldDesc* fieldDesc, QCall::ObjectHandleOnStack instance, QCall::TypeHandle fieldType, QCall::TypeHandle declaringType, BOOL *pIsClassInitialized, QCall::ObjectHandleOnStack result, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeFieldHandle_SetValue(FieldDesc* fieldDesc, QCall::ObjectHandleOnStack instance, QCall::ObjectHandleOnStack value, QCall::TypeHandle fieldType, QCall::TypeHandle declaringType, BOOL* pIsClassInitialized, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeFieldHandle_GetValueDirect(FieldDesc* fieldDesc, TypedByRef *pTarget, QCall::TypeHandle fieldTypeHandle, QCall::TypeHandle declaringTypeHandle, QCall::ObjectHandleOnStack result, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeFieldHandle_SetValueDirect(FieldDesc* fieldDesc, TypedByRef *pTarget, QCall::ObjectHandleOnStack newValue, QCall::TypeHandle fieldType, QCall::TypeHandle declaringType, QCallException* qcallError);
extern "C" void* QCALLTYPE RuntimeFieldHandle_GetEnCFieldAddr(QCall::ObjectHandleOnStack target, FieldDesc* pFD, QCallException* qcallError);
extern "C" BOOL QCALLTYPE RuntimeFieldHandle_GetRVAFieldInfo(FieldDesc* pField, void** address, UINT* size, QCallException* qcallError);
extern "C" void QCALLTYPE RuntimeFieldHandle_GetFieldDataReference(FieldDesc* pField, QCall::ObjectHandleOnStack instance, QCall::ByteRefOnStack offset, QCallException* qcallError);

extern "C" INT32 QCALLTYPE ModuleHandle_GetMDStreamVersion(QCall::ModuleHandle pModule);
extern "C" void QCALLTYPE ModuleHandle_GetModuleType(QCall::ModuleHandle pModule, QCall::ObjectHandleOnStack retType, QCallException* qcallError);

extern "C" INT32 QCALLTYPE ModuleHandle_GetToken(QCall::ModuleHandle pModule);

extern "C" void QCALLTYPE ModuleHandle_ResolveType(QCall::ModuleHandle pModule, INT32 tkType, TypeHandle *typeArgs, INT32 typeArgsCount, TypeHandle *methodArgs, INT32 methodArgsCount, QCall::ObjectHandleOnStack retType, QCallException* qcallError);

extern "C" MethodDesc * QCALLTYPE ModuleHandle_ResolveMethod(QCall::ModuleHandle pModule, INT32 tkMemberRef, TypeHandle *typeArgs, INT32 typeArgsCount, TypeHandle *methodArgs, INT32 methodArgsCount, QCallException* qcallError);

extern "C" void QCALLTYPE ModuleHandle_ResolveField(QCall::ModuleHandle pModule, INT32 tkMemberRef, TypeHandle *typeArgs, INT32 typeArgsCount, TypeHandle *methodArgs, INT32 methodArgsCount, QCall::ObjectHandleOnStack retField, QCallException* qcallError);

extern "C" void QCALLTYPE ModuleHandle_GetPEKind(QCall::ModuleHandle pModule, DWORD* pdwPEKind, DWORD* pdwMachine, QCallException* qcallError);

extern "C" void QCALLTYPE ModuleHandle_GetDynamicMethod(QCall::ModuleHandle pModule, const char* name, byte* sig, INT32 sigLen, QCall::ObjectHandleOnStack resolver, QCall::ObjectHandleOnStack result, QCallException* qcallError);

class AssemblyHandle
{
public:
    FCDECL1(static ReflectModuleBaseObject*, GetManifestModule, AssemblyBaseObject *pAssemblyUNSAFE);
    FCDECL1(static INT32, GetTokenInternal, AssemblyBaseObject *pAssemblyUNSAFE);
};

extern "C" void QCALLTYPE AssemblyHandle_GetManifestModuleSlow(QCall::ObjectHandleOnStack assembly, QCall::ObjectHandleOnStack module, QCallException* qcallError);

extern "C" void QCALLTYPE Signature_Init(
    QCall::ObjectHandleOnStack sigObj,
    PCCOR_SIGNATURE pCorSig, DWORD cCorSig,
    FieldDesc* pFieldDesc,
    MethodDesc* pMethodDesc,
    QCallException* qcallError);

extern "C" BOOL QCALLTYPE Signature_AreEqual(
    PCCOR_SIGNATURE sig1, INT32 cSig1, QCall::TypeHandle handle1,
    PCCOR_SIGNATURE sig2, INT32 cSig2, QCall::TypeHandle handle2,
    QCallException* qcallError);

extern "C" void QCALLTYPE Signature_GetCustomModifiersAtOffset(
    QCall::ObjectHandleOnStack sigObj,
    INT32 offset,
    BOOL fRequired,
    QCall::ObjectHandleOnStack result,
    QCallException* qcallError);

class SignatureNative : public Object
{
    friend void QCALLTYPE Signature_Init(
        QCall::ObjectHandleOnStack sigObj,
        PCCOR_SIGNATURE pCorSig, DWORD cCorSig,
        FieldDesc* pFieldDesc,
        MethodDesc* pMethodDesc,
        QCallException* qcallError);
    friend class ArgIteratorForMethodInvoke;

public:
    FCDECL3(static INT32, GetParameterOffsetInternal, PCCOR_SIGNATURE sig, DWORD csig, INT32 parameterIndex);

    FCDECL4(static INT32, GetTypeParameterOffsetInternal, PCCOR_SIGNATURE sig, DWORD csig, INT32 offset, INT32 index);

    FCDECL3(static INT32, GetCallingConventionFromFunctionPointerAtOffsetInternal, PCCOR_SIGNATURE sig, DWORD csig, INT32 offset);

    BOOL HasThis() { LIMITED_METHOD_CONTRACT; return (_managedCallingConventionAndArgIteratorFlags & CALLCONV_HasThis); }
    INT32 NumFixedArgs() { WRAPPER_NO_CONTRACT; return _arguments->GetNumComponents(); }
    TypeHandle GetReturnTypeHandle()
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        return ((REFLECTCLASSBASEREF)_returnTypeORfieldType)->GetType();
    }

    PCCOR_SIGNATURE GetCorSig() { LIMITED_METHOD_CONTRACT; return _sig; }
    DWORD GetCorSigSize() { LIMITED_METHOD_CONTRACT; return _csig; }
    Module* GetModule() { WRAPPER_NO_CONTRACT; return GetDeclaringType().GetModule(); }

    TypeHandle GetArgumentAt(INT32 position)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        REFLECTCLASSBASEREF refArgument = (REFLECTCLASSBASEREF)_arguments->GetAt(position);
        return refArgument->GetType();
    }

    DWORD GetArgIteratorFlags()
    {
        LIMITED_METHOD_CONTRACT;
        return VolatileLoad(&_managedCallingConventionAndArgIteratorFlags) >> CALLCONV_ArgIteratorFlags_Shift;
    }

    INT32 GetSizeOfArgStack()
    {
        LIMITED_METHOD_CONTRACT;
        return _nSizeOfArgStack;
    }

    TypeHandle GetDeclaringType()
    {
        LIMITED_METHOD_CONTRACT;
        return _declaringType->GetType();
    }
    MethodDesc* GetMethod()
    {
        LIMITED_METHOD_CONTRACT;
        return _pMethod;
    }

    const SigTypeContext * GetTypeContext(SigTypeContext *pTypeContext)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

       _ASSERTE(_pMethod || !GetDeclaringType().IsNull());
        if (_pMethod)
            return SigTypeContext::GetOptionalTypeContext(_pMethod, GetDeclaringType(), pTypeContext);
        else
            return SigTypeContext::GetOptionalTypeContext(GetDeclaringType(), pTypeContext);
    }

private:
    void SetReturnType(OBJECTREF returnType)
    {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        SetObjectReference(&_returnTypeORfieldType, returnType);
    }

    void SetKeepAlive(OBJECTREF keepAlive)
    {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        SetObjectReference(&_keepAlive, keepAlive);
    }

    void SetArgumentArray(PTRARRAYREF ptrArrayarguments)
    {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        SetObjectReference((OBJECTREF*)&_arguments, (OBJECTREF)ptrArrayarguments);
    }

    void SetArgument(INT32 argument, OBJECTREF argumentType)
    {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        _arguments->SetAt(argument, argumentType);
    }

    void SetArgIteratorFlags(DWORD flags)
    {
        LIMITED_METHOD_CONTRACT;
        return VolatileStore(&_managedCallingConventionAndArgIteratorFlags, (INT32)(_managedCallingConventionAndArgIteratorFlags | (flags << CALLCONV_ArgIteratorFlags_Shift)));
    }

    void SetSizeOfArgStack(INT32 nSizeOfArgStack)
    {
        LIMITED_METHOD_CONTRACT;
        _nSizeOfArgStack = nSizeOfArgStack;
    }

    void SetCallingConvention(INT32 mdCallingConvention)
    {
        LIMITED_METHOD_CONTRACT;

        if ((mdCallingConvention & IMAGE_CEE_CS_CALLCONV_MASK) == IMAGE_CEE_CS_CALLCONV_VARARG)
            _managedCallingConventionAndArgIteratorFlags = CALLCONV_VarArgs;
        else
            _managedCallingConventionAndArgIteratorFlags = CALLCONV_Standard;

        if ((mdCallingConvention & IMAGE_CEE_CS_CALLCONV_HASTHIS) != 0)
            _managedCallingConventionAndArgIteratorFlags |= CALLCONV_HasThis;

        if ((mdCallingConvention & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS) != 0)
            _managedCallingConventionAndArgIteratorFlags |= CALLCONV_ExplicitThis;
    }

    // Mirrored in the managed world (System.Signature)
    //
    // this is the layout the classloader chooses by default for the managed struct.
    //
    PTRARRAYREF _arguments;
    REFLECTCLASSBASEREF _declaringType;
    OBJECTREF _returnTypeORfieldType;
    OBJECTREF _keepAlive;
    PCCOR_SIGNATURE _sig;
    DWORD _csig;
    INT32 _managedCallingConventionAndArgIteratorFlags;
    INT32 _nSizeOfArgStack;
    MethodDesc* _pMethod;
};

typedef DPTR(SignatureNative) PTR_SignatureNative;

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<SignatureNative> SIGNATURENATIVEREF;
#else
typedef PTR_SignatureNative SIGNATURENATIVEREF;
#endif

class ReflectionPointer : public Object
{
public:
    OBJECTREF _ptrType;
    void * _ptr;
};

#endif
