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
class SignatureNative;

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

class RuntimeExceptionHandlingClause : Object
{
private:
    // Disallow creation and copy construction of these.
    RuntimeExceptionHandlingClause() { }
    RuntimeExceptionHandlingClause(RuntimeExceptionHandlingClause &r) { }

public:
    RUNTIMEMETHODBODYREF _methodBody;
    CorExceptionFlag _flags;
    INT32 _tryOffset;
    INT32 _tryLength;
    INT32 _handlerOffset;
    INT32 _handlerLength;
    mdTypeDef _catchToken;
    INT32 _filterOffset;
};

class RuntimeMethodBody : Object
{
private:
    // Disallow creation and copy construction of these.
    RuntimeMethodBody() { }
    RuntimeMethodBody(RuntimeMethodBody &r) { }

public:
    U1ARRAYREF _IL;
    PTRARRAYREF _exceptionClauses;
    PTRARRAYREF _localVariables;
    OBJECTREF _methodBase;

    INT32 _localVarSigToken;
    INT32 _maxStackSize;
    CLR_BOOL _initLocals;
};

class RuntimeLocalVariableInfo : Object
{
private:
    // Disallow creation and copy construction of these.
    RuntimeLocalVariableInfo() { }
    RuntimeLocalVariableInfo(RuntimeLocalVariableInfo &r) { }

public:

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

extern "C" BOOL QCALLTYPE MdUtf8String_EqualsCaseInsensitive(LPCUTF8 szLhs, LPCUTF8 szRhs, INT32 stringNumBytes);

class RuntimeTypeHandle;

typedef RuntimeTypeHandle FCALLRuntimeTypeHandle;
#define FCALL_RTH_TO_REFLECTCLASS(x) (x).pRuntimeTypeDONOTUSEDIRECTLY

class RuntimeTypeHandle {
public:
    ReflectClassBaseObject *pRuntimeTypeDONOTUSEDIRECTLY;

    // Static method on RuntimeTypeHandle
    static FCDECL1(Object*, AllocateComObject, void* pClassFactory);

    static FCDECL1(ReflectClassBaseObject*, GetRuntimeType, void *th);

    static FCDECL2(FC_BOOL_RET, IsEquivalentTo, ReflectClassBaseObject *rtType1UNSAFE, ReflectClassBaseObject *rtType2UNSAFE);

    static FCDECL1(AssemblyBaseObject*, GetAssembly, ReflectClassBaseObject *pType);
    static FCDECL1(ReflectClassBaseObject*, GetBaseType, ReflectClassBaseObject* pType);
    static FCDECL1(ReflectModuleBaseObject*, GetModule, ReflectClassBaseObject* pType);
    static FCDECL1(INT32, GetAttributes, ReflectClassBaseObject* pType);
    static FCDECL1(INT32, GetToken, ReflectClassBaseObject* pType);
    static FCDECL1(LPCUTF8, GetUtf8Name, ReflectClassBaseObject* pType);
    static FCDECL1(INT32, GetArrayRank, ReflectClassBaseObject* pType);

    static FCDECL1(ReflectMethodObject*, GetDeclaringMethod, ReflectClassBaseObject *pType);

    static FCDECL1(ReflectClassBaseObject*, GetDeclaringType, ReflectClassBaseObject* pType);
    static FCDECL1(FC_BOOL_RET, IsValueType, ReflectClassBaseObject* pType);
    static FCDECL1(FC_BOOL_RET, IsInterface, ReflectClassBaseObject* pType);
    static FCDECL1(FC_BOOL_RET, IsByRefLike, ReflectClassBaseObject* pType);

    static FCDECL1(Object *, GetArgumentTypesFromFunctionPointer, ReflectClassBaseObject *pTypeUNSAFE);
    static FCDECL1(FC_BOOL_RET, IsUnmanagedFunctionPointer, ReflectClassBaseObject *pTypeUNSAFE);

    static FCDECL2(FC_BOOL_RET, CanCastTo, ReflectClassBaseObject *pType, ReflectClassBaseObject *pTarget);
    static FCDECL2(FC_BOOL_RET, IsInstanceOfType, ReflectClassBaseObject *pType, Object *object);

    static FCDECL6(FC_BOOL_RET, SatisfiesConstraints, PTR_ReflectClassBaseObject pGenericParameter, TypeHandle *typeContextArgs, INT32 typeContextCount, TypeHandle *methodContextArgs, INT32 methodContextCount, PTR_ReflectClassBaseObject pGenericArgument);

    static
    FCDECL1(FC_BOOL_RET, IsGenericVariable, PTR_ReflectClassBaseObject pType);

    static
    FCDECL1(INT32, GetGenericVariableIndex, PTR_ReflectClassBaseObject pType);

    static
    FCDECL1(FC_BOOL_RET, ContainsGenericVariables, PTR_ReflectClassBaseObject pType);

    static FCDECL2(FC_BOOL_RET, CompareCanonicalHandles, PTR_ReflectClassBaseObject pLeft, PTR_ReflectClassBaseObject pRight);

    static FCDECL1(PtrArray*, GetInterfaces, ReflectClassBaseObject *pType);

    static FCDECL1(INT32, GetCorElementType, PTR_ReflectClassBaseObject pType);
    static FCDECL1(ReflectClassBaseObject*, GetElementType, ReflectClassBaseObject* pType);

    static FCDECL2(MethodDesc*, GetMethodAt, PTR_ReflectClassBaseObject pType, INT32 slot);
    static FCDECL1(INT32, GetNumVirtuals, ReflectClassBaseObject *pType);
    static FCDECL1(INT32, GetNumVirtualsAndStaticVirtuals, ReflectClassBaseObject *pType);
    static FCDECL3(FC_BOOL_RET, GetFields, ReflectClassBaseObject *pType, INT32 **result, INT32 *pCount);

    static FCDECL1(MethodDesc *, GetFirstIntroducedMethod, ReflectClassBaseObject* pType);
    static FCDECL1(void, GetNextIntroducedMethod, MethodDesc **ppMethod);

    static FCDECL1(IMDInternalImport*, GetMetadataImport, ReflectClassBaseObject * pModuleUNSAFE);

    // Helper methods not called by managed code

    static void ValidateTypeAbleToBeInstantiated(TypeHandle typeHandle, bool fGetUninitializedObject);
};

extern "C" void QCALLTYPE RuntimeTypeHandle_CreateInstanceForAnotherGenericParameter(QCall::TypeHandle pTypeHandle, TypeHandle *pInstArray, INT32 cInstArray, QCall::ObjectHandleOnStack pInstantiatedObject);
extern "C" void* QCALLTYPE RuntimeTypeHandle_AllocateTypeAssociatedMemory(QCall::TypeHandle type, uint32_t size);

extern "C" PVOID QCALLTYPE QCall_GetGCHandleForTypeHandle(QCall::TypeHandle pTypeHandle, INT32 handleType);
extern "C" void QCALLTYPE QCall_FreeGCHandleForTypeHandle(QCall::TypeHandle pTypeHandle, OBJECTHANDLE objHandle);

extern "C" void QCALLTYPE RuntimeTypeHandle_GetActivationInfo(
    QCall::ObjectHandleOnStack pRuntimeType,
    PCODE* ppfnAllocator,
    void** pvAllocatorFirstArg,
    PCODE* ppfnCtor,
    BOOL* pfCtorIsPublic);
extern "C" void QCALLTYPE RuntimeTypeHandle_MakeByRef(QCall::TypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType);
extern "C" void QCALLTYPE RuntimeTypeHandle_MakePointer(QCall::TypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType);
extern "C" void QCALLTYPE RuntimeTypeHandle_MakeSZArray(QCall::TypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType);
extern "C" void QCALLTYPE RuntimeTypeHandle_MakeArray(QCall::TypeHandle pTypeHandle, INT32 rank, QCall::ObjectHandleOnStack retType);
extern "C" BOOL QCALLTYPE RuntimeTypeHandle_IsCollectible(QCall::TypeHandle pTypeHandle);
extern "C" void QCALLTYPE RuntimeTypeHandle_PrepareMemberInfoCache(QCall::TypeHandle pMemberInfoCache);
extern "C" void QCALLTYPE RuntimeTypeHandle_ConstructName(QCall::TypeHandle pTypeHandle, DWORD format, QCall::StringHandleOnStack retString);
extern "C" BOOL QCALLTYPE RuntimeTypeHandle_IsVisible(QCall::TypeHandle pTypeHandle);
extern "C" void QCALLTYPE RuntimeTypeHandle_GetInstantiation(QCall::TypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType, BOOL fAsRuntimeTypeArray);
extern "C" void QCALLTYPE RuntimeTypeHandle_Instantiate(QCall::TypeHandle pTypeHandle, TypeHandle * pInstArray, INT32 cInstArray, QCall::ObjectHandleOnStack retType);
extern "C" void QCALLTYPE RuntimeTypeHandle_GetGenericTypeDefinition(QCall::TypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType);
extern "C" void QCALLTYPE RuntimeTypeHandle_GetConstraints(QCall::TypeHandle pTypeHandle, QCall::ObjectHandleOnStack retTypes);
extern "C" void QCALLTYPE RuntimeTypeHandle_VerifyInterfaceIsImplemented(QCall::TypeHandle pTypeHandle, QCall::TypeHandle pIFaceHandle);
extern "C" MethodDesc* QCALLTYPE RuntimeTypeHandle_GetInterfaceMethodImplementation(QCall::TypeHandle pTypeHandle, QCall::TypeHandle pOwner, MethodDesc * pMD);
extern "C" void QCALLTYPE RuntimeTypeHandle_RegisterCollectibleTypeDependency(QCall::TypeHandle pTypeHandle, QCall::AssemblyHandle pAssembly);

class RuntimeMethodHandle {

public:
    static FCDECL1(ReflectMethodObject*, GetCurrentMethod, StackCrawlMark* stackMark);

    static FCDECL4(Object*, InvokeMethod, Object *target, PVOID* args, SignatureNative* pSig, CLR_BOOL fConstructor);

    static FCDECL2(Object*, ReboxToNullable, Object *pBoxedValUNSAFE, ReflectClassBaseObject *pDestUNSAFE);
    static FCDECL1(Object*, ReboxFromNullable, Object *pBoxedValUNSAFE);

    struct StreamingContextData {
        Object * additionalContext;  // additionalContex was changed from OBJECTREF to Object to avoid having a
        INT32 contextStates;         // constructor in this struct. GCC doesn't allow structs with constructors to be
    };

    // *******************************************************************************************
    // Keep these in sync with the version in bcl\system\runtime\serialization\streamingcontext.cs
    // *******************************************************************************************
    enum StreamingContextStates
    {
        CONTEXTSTATE_CrossProcess   = 0x01,
        CONTEXTSTATE_CrossMachine   = 0x02,
        CONTEXTSTATE_File           = 0x04,
        CONTEXTSTATE_Persistence    = 0x08,
        CONTEXTSTATE_Remoting       = 0x10,
        CONTEXTSTATE_Other          = 0x20,
        CONTEXTSTATE_Clone          = 0x40,
        CONTEXTSTATE_CrossAppDomain = 0x80,
        CONTEXTSTATE_All            = 0xFF
    };

    // passed by value
    // STATIC IMPLEMENTATION
    static OBJECTREF InvokeMethod_Internal(
        MethodDesc *pMethod, OBJECTREF targetUNSAFE, INT32 attrs, OBJECTREF binderUNSAFE, PTRARRAYREF objsUNSAFE, OBJECTREF localeUNSAFE,
        BOOL isBinderDefault, Assembly *caller, Assembly *reflectedClassAssembly, TypeHandle declaringType, SignatureNative* pSig, BOOL verifyAccess);

    static FCDECL4(void, SerializationInvoke, ReflectMethodObject *pMethodUNSAFE, Object* targetUNSAFE,
        Object* serializationInfoUNSAFE, struct StreamingContextData * pContext);

    static FCDECL1(INT32, GetAttributes, MethodDesc *pMethod);
    static FCDECL1(INT32, GetImplAttributes, ReflectMethodObject *pMethodUNSAFE);
    static FCDECL1(ReflectClassBaseObject*, GetDeclaringType, MethodDesc *pMethod);
    static FCDECL1(INT32, GetSlot, MethodDesc *pMethod);
    static FCDECL1(INT32, GetMethodDef, ReflectMethodObject *pMethodUNSAFE);
    static FCDECL1(StringObject*, GetName, MethodDesc *pMethod);
    static FCDECL1(LPCUTF8, GetUtf8Name, MethodDesc *pMethod);
    static
    FCDECL1(FC_BOOL_RET, HasMethodInstantiation, MethodDesc *pMethod);

    static
    FCDECL1(FC_BOOL_RET, IsGenericMethodDefinition, MethodDesc *pMethod);

    static
    FCDECL1(FC_BOOL_RET, IsTypicalMethodDefinition, ReflectMethodObject *pMethodUNSAFE);

    static
    FCDECL1(INT32, GetGenericParameterCount, MethodDesc * pMethod);

    // see comment in the cpp file
    static FCDECL3(MethodDesc*, GetStubIfNeeded, MethodDesc *pMethod, ReflectClassBaseObject *pType, PtrArray* instArray);
    static FCDECL2(MethodDesc*, GetMethodFromCanonical, MethodDesc *pMethod, PTR_ReflectClassBaseObject pType);

    static
    FCDECL1(FC_BOOL_RET, IsDynamicMethod, MethodDesc * pMethod);

    static
    FCDECL1(Object*, GetResolver, MethodDesc * pMethod);


    static FCDECL2(RuntimeMethodBody*, GetMethodBody, ReflectMethodObject *pMethodUNSAFE, PTR_ReflectClassBaseObject pDeclaringType);

    static FCDECL1(FC_BOOL_RET, IsConstructor, MethodDesc *pMethod);

    static FCDECL1(Object*, GetLoaderAllocator, MethodDesc *pMethod);
};

extern "C" BOOL QCALLTYPE RuntimeMethodHandle_IsCAVisibleFromDecoratedType(
        QCall::TypeHandle targetTypeHandle,
        MethodDesc * pTargetCtor,
        QCall::TypeHandle sourceTypeHandle,
        QCall::ModuleHandle sourceModuleHandle);

extern "C" void QCALLTYPE RuntimeMethodHandle_GetMethodInstantiation(MethodDesc * pMethod, QCall::ObjectHandleOnStack retTypes, BOOL fAsRuntimeTypeArray);

extern "C" void QCALLTYPE RuntimeMethodHandle_ConstructInstantiation(MethodDesc * pMethod, DWORD format, QCall::StringHandleOnStack retString);
extern "C" void * QCALLTYPE RuntimeMethodHandle_GetFunctionPointer(MethodDesc * pMethod);
extern "C" BOOL QCALLTYPE RuntimeMethodHandle_GetIsCollectible(MethodDesc * pMethod);
extern "C" void QCALLTYPE RuntimeMethodHandle_GetTypicalMethodDefinition(MethodDesc * pMethod, QCall::ObjectHandleOnStack refMethod);
extern "C" void QCALLTYPE RuntimeMethodHandle_StripMethodInstantiation(MethodDesc * pMethod, QCall::ObjectHandleOnStack refMethod);
extern "C" void QCALLTYPE RuntimeMethodHandle_Destroy(MethodDesc * pMethod);
class RuntimeFieldHandle {

public:
    static FCDECL5(Object*, GetValue, ReflectFieldObject *pFieldUNSAFE, Object *instanceUNSAFE, ReflectClassBaseObject *pFieldType, ReflectClassBaseObject *pDeclaringType, CLR_BOOL *pDomainInitialized);
    static FCDECL7(void, SetValue, ReflectFieldObject *pFieldUNSAFE, Object *targetUNSAFE, Object *valueUNSAFE, ReflectClassBaseObject *pFieldType, DWORD attr, ReflectClassBaseObject *pDeclaringType, CLR_BOOL *pDomainInitialized);
    static FCDECL4(Object*, GetValueDirect, ReflectFieldObject *pFieldUNSAFE, ReflectClassBaseObject *pFieldType, TypedByRef *pTarget, ReflectClassBaseObject *pDeclaringType);
    static FCDECL5(void, SetValueDirect, ReflectFieldObject *pFieldUNSAFE, ReflectClassBaseObject *pFieldType, TypedByRef *pTarget, Object *valueUNSAFE, ReflectClassBaseObject *pContextType);
    static FCDECL1(StringObject*, GetName, ReflectFieldObject *pFieldUNSAFE);
    static FCDECL1(LPCUTF8, GetUtf8Name, FieldDesc *pField);

    static FCDECL1(INT32, GetAttributes, FieldDesc *pField);
    static FCDECL1(ReflectClassBaseObject*, GetApproxDeclaringType, FieldDesc *pField);
    static FCDECL1(INT32, GetToken, ReflectFieldObject *pFieldUNSAFE);
    static FCDECL2(FieldDesc*, GetStaticFieldForGenericType, FieldDesc *pField, ReflectClassBaseObject *pDeclaringType);
    static FCDECL1(FC_BOOL_RET, AcquiresContextFromThis, FieldDesc *pField);
    static FCDECL1(Object*, GetLoaderAllocator, FieldDesc *pField);
};

class ModuleHandle {

public:
    static FCDECL5(ReflectMethodObject*, GetDynamicMethod, ReflectMethodObject *pMethodUNSAFE, ReflectModuleBaseObject *pModuleUNSAFE, StringObject *name, U1Array *sig, Object *resolver);
    static FCDECL1(INT32, GetToken, ReflectModuleBaseObject *pModuleUNSAFE);

    static
    FCDECL1(IMDInternalImport*, GetMetadataImport, ReflectModuleBaseObject * pModuleUNSAFE);

    static
    FCDECL1(INT32, GetMDStreamVersion, ReflectModuleBaseObject * pModuleUNSAFE);

};

extern "C" void QCALLTYPE ModuleHandle_GetModuleType(QCall::ModuleHandle pModule, QCall::ObjectHandleOnStack retType);

extern "C" void QCALLTYPE ModuleHandle_ResolveType(QCall::ModuleHandle pModule, INT32 tkType, TypeHandle *typeArgs, INT32 typeArgsCount, TypeHandle *methodArgs, INT32 methodArgsCount, QCall::ObjectHandleOnStack retType);

extern "C" MethodDesc * QCALLTYPE ModuleHandle_ResolveMethod(QCall::ModuleHandle pModule, INT32 tkMemberRef, TypeHandle *typeArgs, INT32 typeArgsCount, TypeHandle *methodArgs, INT32 methodArgsCount);

extern "C" void QCALLTYPE ModuleHandle_ResolveField(QCall::ModuleHandle pModule, INT32 tkMemberRef, TypeHandle *typeArgs, INT32 typeArgsCount, TypeHandle *methodArgs, INT32 methodArgsCount, QCall::ObjectHandleOnStack retField);

extern "C" void QCALLTYPE ModuleHandle_GetAssembly(QCall::ModuleHandle pModule, QCall::ObjectHandleOnStack retAssembly);

extern "C" void QCALLTYPE ModuleHandle_GetPEKind(QCall::ModuleHandle pModule, DWORD* pdwPEKind, DWORD* pdwMachine);

class AssemblyHandle {

public:
    static FCDECL1(ReflectModuleBaseObject*, GetManifestModule, AssemblyBaseObject *pAssemblyUNSAFE);

    static FCDECL1(INT32, GetToken, AssemblyBaseObject *pAssemblyUNSAFE);
};

class SignatureNative;

typedef DPTR(SignatureNative) PTR_SignatureNative;

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<SignatureNative> SIGNATURENATIVEREF;
#else
typedef PTR_SignatureNative SIGNATURENATIVEREF;
#endif

class SignatureNative : public Object
{
    friend class RuntimeMethodHandle;
    friend class ArgIteratorForMethodInvoke;

public:
    static FCDECL6(void, GetSignature,
        SignatureNative* pSignatureNative,
        PCCOR_SIGNATURE pCorSig, DWORD cCorSig,
        FieldDesc *pFieldDesc, ReflectMethodObject *pMethodUNSAFE,
        ReflectClassBaseObject *pDeclaringType);

    static FCDECL2(FC_BOOL_RET, CompareSig, SignatureNative* pLhs, SignatureNative* pRhs);

    static FCDECL2(INT32, GetParameterOffset, SignatureNative* pSig, INT32 parameterIndex);

    static FCDECL3(INT32, GetTypeParameterOffset, SignatureNative* pSig, INT32 offset, INT32 index);

    static FCDECL2(FC_INT8_RET, GetCallingConventionFromFunctionPointerAtOffset, SignatureNative* pSig, INT32 offset);

    static FCDECL3(Object *, GetCustomModifiersAtOffset, SignatureNative* pSig, INT32 offset, CLR_BOOL fRequired);

    BOOL HasThis() { LIMITED_METHOD_CONTRACT; return (m_managedCallingConvention & CALLCONV_HasThis); }
    INT32 NumFixedArgs() { WRAPPER_NO_CONTRACT; return m_PtrArrayarguments->GetNumComponents(); }
    TypeHandle GetReturnTypeHandle()
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        return ((REFLECTCLASSBASEREF)m_returnType)->GetType();
    }

    PCCOR_SIGNATURE GetCorSig() { LIMITED_METHOD_CONTRACT; return m_sig; }
    DWORD GetCorSigSize() { LIMITED_METHOD_CONTRACT; return m_cSig; }
    Module* GetModule() { WRAPPER_NO_CONTRACT; return GetDeclaringType().GetModule(); }

    TypeHandle GetArgumentAt(INT32 position)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        REFLECTCLASSBASEREF refArgument = (REFLECTCLASSBASEREF)m_PtrArrayarguments->GetAt(position);
        return refArgument->GetType();
    }

    DWORD GetArgIteratorFlags()
    {
        LIMITED_METHOD_CONTRACT;
        return VolatileLoad(&m_managedCallingConvention) >> CALLCONV_ArgIteratorFlags_Shift;
    }

    INT32 GetSizeOfArgStack()
    {
        LIMITED_METHOD_CONTRACT;
        return m_nSizeOfArgStack;
    }

    TypeHandle GetDeclaringType()
    {
        LIMITED_METHOD_CONTRACT;
        return m_declaringType->GetType();
    }
    MethodDesc* GetMethod()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pMethod;
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

       _ASSERTE(m_pMethod || !GetDeclaringType().IsNull());
        if (m_pMethod)
            return SigTypeContext::GetOptionalTypeContext(m_pMethod, GetDeclaringType(), pTypeContext);
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
        SetObjectReference(&m_returnType, returnType);
    }

    void SetKeepAlive(OBJECTREF keepAlive)
    {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        SetObjectReference(&m_keepalive, keepAlive);
    }

    void SetDeclaringType(REFLECTCLASSBASEREF declaringType)
    {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        SetObjectReference((OBJECTREF*)&m_declaringType, (OBJECTREF)declaringType);
    }

    void SetArgumentArray(PTRARRAYREF ptrArrayarguments)
    {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        SetObjectReference((OBJECTREF*)&m_PtrArrayarguments, (OBJECTREF)ptrArrayarguments);
    }

    void SetArgument(INT32 argument, OBJECTREF argumentType)
    {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        m_PtrArrayarguments->SetAt(argument, argumentType);
    }

    void SetArgIteratorFlags(DWORD flags)
    {
        LIMITED_METHOD_CONTRACT;
        return VolatileStore(&m_managedCallingConvention, (INT32)(m_managedCallingConvention | (flags << CALLCONV_ArgIteratorFlags_Shift)));
    }

    void SetSizeOfArgStack(INT32 nSizeOfArgStack)
    {
        LIMITED_METHOD_CONTRACT;
        m_nSizeOfArgStack = nSizeOfArgStack;
    }

    void SetCallingConvention(INT32 mdCallingConvention)
    {
        LIMITED_METHOD_CONTRACT;

        if ((mdCallingConvention & IMAGE_CEE_CS_CALLCONV_MASK) == IMAGE_CEE_CS_CALLCONV_VARARG)
            m_managedCallingConvention = CALLCONV_VarArgs;
        else
            m_managedCallingConvention = CALLCONV_Standard;

        if ((mdCallingConvention & IMAGE_CEE_CS_CALLCONV_HASTHIS) != 0)
            m_managedCallingConvention |= CALLCONV_HasThis;

        if ((mdCallingConvention & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS) != 0)
            m_managedCallingConvention |= CALLCONV_ExplicitThis;
    }

    // Mirrored in the managed world (System.Signature)
    //
    // this is the layout the classloader chooses by default for the managed struct.
    //
    PTRARRAYREF m_PtrArrayarguments;
    REFLECTCLASSBASEREF m_declaringType;
    OBJECTREF m_returnType;
    OBJECTREF m_keepalive;
    PCCOR_SIGNATURE m_sig;
    INT32 m_managedCallingConvention;
    INT32 m_nSizeOfArgStack;
    DWORD m_cSig;
    MethodDesc* m_pMethod;
};

class ReflectionPointer : public Object
{
public:
    OBJECTREF _ptrType;
    void * _ptr;
};

#endif
