// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#ifndef _RUNTIMEHANDLES_H_
#define _RUNTIMEHANDLES_H_

#include "object.h"
#include "typehandle.h"
#include "fcall.h"
#include "field.h"
#include "typectxt.h"
#include "constrainedexecutionregion.h"

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

class ExceptionHandlingClause;
class MethodBody;
class LocalVariableInfo;

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<ExceptionHandlingClause> EXCEPTIONHANDLINGCLAUSEREF;
typedef REF<MethodBody> METHODBODYREF;
typedef REF<LocalVariableInfo> LOCALVARIABLEINFOREF;
#else
typedef DPTR(ExceptionHandlingClause) EXCEPTIONHANDLINGCLAUSEREF;
typedef DPTR(MethodBody) METHODBODYREF;
typedef DPTR(LocalVariableInfo) LOCALVARIABLEINFOREF;
#endif

class ExceptionHandlingClause : Object 
{
private:
    // Disallow creation and copy construction of these.
    ExceptionHandlingClause() { }
    ExceptionHandlingClause(ExceptionHandlingClause &r) { }    

public:
    METHODBODYREF m_methodBody;
    CorExceptionFlag m_flags;
    INT32 m_tryOffset;
    INT32 m_tryLength;
    INT32 m_handlerOffset;
    INT32 m_handlerLength;
    mdTypeDef m_catchToken;
    INT32 m_filterOffset;
};

class MethodBody : Object 
{     
private:
    // Disallow creation and copy construction of these.
    MethodBody() { }
    MethodBody(MethodBody &r) { }    

public:
    U1ARRAYREF m_IL;
    PTRARRAYREF m_exceptionClauses;
    PTRARRAYREF m_localVariables;
    OBJECTREF m_methodBase;

    INT32 m_localVarSigToken;
    INT32 m_maxStackSize;
    CLR_BOOL m_initLocals;    
};

class LocalVariableInfo : Object
{
private:
    // Disallow creation and copy construction of these.
    LocalVariableInfo() { }
    LocalVariableInfo(LocalVariableInfo &r) { }    

public:

    REFLECTCLASSBASEREF GetType()
    {
        return (REFLECTCLASSBASEREF)m_type;
    }

    void SetType(OBJECTREF type)
    {
        SetObjectReference(&m_type, type, GetAppDomain());
    }

    OBJECTREF m_type;
    INT32 m_bIsPinned;
    INT32 m_localIndex;
};

class Utf8String {
public:
    static FCDECL3(FC_BOOL_RET, EqualsCaseSensitive, LPCUTF8 szLhs, LPCUTF8 szRhs, INT32 stringNumBytes);

    static
    BOOL QCALLTYPE EqualsCaseInsensitive(LPCUTF8 szLhs, LPCUTF8 szRhs, INT32 stringNumBytes);

    static
    ULONG QCALLTYPE HashCaseInsensitive(LPCUTF8 sz, INT32 stringNumBytes);
};

class RuntimeTypeHandle;

typedef RuntimeTypeHandle FCALLRuntimeTypeHandle;
#define FCALL_RTH_TO_REFLECTCLASS(x) (x).pRuntimeTypeDONOTUSEDIRECTLY

class RuntimeTypeHandle {
    ReflectClassBaseObject *pRuntimeTypeDONOTUSEDIRECTLY;
public:

    // Static method on RuntimeTypeHandle
    static FCDECL1(Object*, Allocate, ReflectClassBaseObject *refType) ; //A.CI work	
    static FCDECL6(Object*, CreateInstance, ReflectClassBaseObject* refThisUNSAFE,
                                            CLR_BOOL publicOnly,
                                            CLR_BOOL securityOff,
                                            CLR_BOOL *pbCanBeCached,
                                            MethodDesc** pConstructor,
                                            CLR_BOOL *pbNeedSecurityCheck);

    static FCDECL2(Object*, CreateCaInstance, ReflectClassBaseObject* refCaType, ReflectMethodObject* pCtorUNSAFE);

    static
    void QCALLTYPE MakeByRef(EnregisteredTypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType);

    static
    void QCALLTYPE MakePointer(EnregisteredTypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType);

    static
    void QCALLTYPE MakeSZArray(EnregisteredTypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType);

    static
    void QCALLTYPE MakeArray(EnregisteredTypeHandle pTypeHandle, INT32 rank, QCall::ObjectHandleOnStack retType);

    static BOOL QCALLTYPE IsCollectible(EnregisteredTypeHandle pTypeHandle);

    static FCDECL1(ReflectClassBaseObject*, GetRuntimeType, void *th);

    static FCDECL1_V(ReflectClassBaseObject*, GetTypeFromHandle, FCALLRuntimeTypeHandle th);
    static FCDECL1_V(EnregisteredTypeHandle, GetValueInternal, FCALLRuntimeTypeHandle RTH);

    static FCDECL2(FC_BOOL_RET, TypeEQ, Object* left, Object* right);
    static FCDECL2(FC_BOOL_RET, TypeNEQ, Object* left, Object* right);

#ifndef FEATURE_CORECLR
    static FCDECL2(FC_BOOL_RET, IsEquivalentTo, ReflectClassBaseObject *rtType1UNSAFE, ReflectClassBaseObject *rtType2UNSAFE);
    static FCDECL1(FC_BOOL_RET, IsEquivalentType, ReflectClassBaseObject *rtTypeUNSAFE);
#endif // !FEATURE_CORECLR

#ifdef FEATURE_COMINTEROP
    static FCDECL1(FC_BOOL_RET, IsWindowsRuntimeObjectType, ReflectClassBaseObject *rtTypeUNSAFE);
    static FCDECL1(FC_BOOL_RET, IsTypeExportedToWindowsRuntime, ReflectClassBaseObject *rtTypeUNSAFE);
#endif // FEATURE_COMINTEROP

    static
    void QCALLTYPE PrepareMemberInfoCache(EnregisteredTypeHandle pMemberInfoCache);

    static
    void QCALLTYPE ConstructName(EnregisteredTypeHandle pTypeHandle, DWORD format, QCall::StringHandleOnStack retString);

    static
    void QCALLTYPE GetTypeByNameUsingCARules(LPCWSTR pwzClassName, QCall::ModuleHandle pModule, QCall::ObjectHandleOnStack retType);

    static
    void QCALLTYPE GetTypeByName(LPCWSTR pwzClassName, BOOL bThrowOnError, BOOL bIgnoreCase, BOOL bReflectionOnly,
                                 QCall::StackCrawlMarkHandle pStackMark, 
#ifdef FEATURE_HOSTED_BINDER
                                 ICLRPrivBinder * pPrivHostBinder,
#endif
                                 BOOL bLoadTypeFromPartialNameHack, QCall::ObjectHandleOnStack retType,
                                 QCall::ObjectHandleOnStack keepAlive);

    static FCDECL1(AssemblyBaseObject*, GetAssembly, ReflectClassBaseObject *pType);
    static FCDECL1(ReflectClassBaseObject*, GetBaseType, ReflectClassBaseObject* pType);
    static FCDECL1(ReflectModuleBaseObject*, GetModule, ReflectClassBaseObject* pType);
    static FCDECL1(INT32, GetAttributes, ReflectClassBaseObject* pType);
    static FCDECL1(INT32, GetToken, ReflectClassBaseObject* pType);
    static FCDECL1(LPCUTF8, GetUtf8Name, ReflectClassBaseObject* pType);
    static FCDECL1(INT32, GetArrayRank, ReflectClassBaseObject* pType);

    static FCDECL1(ReflectMethodObject*, GetDeclaringMethod, ReflectClassBaseObject *pType);

    static
    void QCALLTYPE GetDefaultConstructor(EnregisteredTypeHandle pTypeHandle, QCall::ObjectHandleOnStack retMethod);

    static FCDECL1(ReflectClassBaseObject*, GetDeclaringType, ReflectClassBaseObject* pType);
#ifdef FEATURE_REMOTING	
    static FCDECL1(FC_BOOL_RET, IsContextful, ReflectClassBaseObject* pType);
#endif
    static FCDECL1(FC_BOOL_RET, IsValueType, ReflectClassBaseObject* pType);
    static FCDECL1(FC_BOOL_RET, IsInterface, ReflectClassBaseObject* pType);
    
    static 
    BOOL QCALLTYPE IsVisible(EnregisteredTypeHandle pTypeHandle);
    
    static
    BOOL QCALLTYPE IsSecurityCritical(EnregisteredTypeHandle pTypeHandle);

    static
    BOOL QCALLTYPE IsSecuritySafeCritical(EnregisteredTypeHandle pTypeHandle);

    static
    BOOL QCALLTYPE IsSecurityTransparent(EnregisteredTypeHandle pTypeHandle);

    static FCDECL1(FC_BOOL_RET, HasProxyAttribute, ReflectClassBaseObject *pType);
    static FCDECL2(FC_BOOL_RET, IsComObject, ReflectClassBaseObject *pType, CLR_BOOL isGenericCOM);
    static FCDECL2(FC_BOOL_RET, CanCastTo, ReflectClassBaseObject *pType, ReflectClassBaseObject *pTarget);
    static FCDECL2(FC_BOOL_RET, IsInstanceOfType, ReflectClassBaseObject *pType, Object *object);

    static FCDECL6(FC_BOOL_RET, SatisfiesConstraints, PTR_ReflectClassBaseObject pGenericParameter, TypeHandle *typeContextArgs, INT32 typeContextCount, TypeHandle *methodContextArgs, INT32 methodContextCount, PTR_ReflectClassBaseObject pGenericArgument);
    static
    FCDECL1(FC_BOOL_RET, HasInstantiation, PTR_ReflectClassBaseObject pType);

    static
    FCDECL1(FC_BOOL_RET, IsGenericTypeDefinition, PTR_ReflectClassBaseObject pType);

    static
    FCDECL1(FC_BOOL_RET, IsGenericVariable, PTR_ReflectClassBaseObject pType);

    static
    FCDECL1(INT32, GetGenericVariableIndex, PTR_ReflectClassBaseObject pType);

    static
    FCDECL1(FC_BOOL_RET, ContainsGenericVariables, PTR_ReflectClassBaseObject pType);

    static
    void QCALLTYPE GetInstantiation(EnregisteredTypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType, BOOL fAsRuntimeTypeArray);

    static
    void QCALLTYPE Instantiate(EnregisteredTypeHandle pTypeHandle, TypeHandle * pInstArray, INT32 cInstArray, QCall::ObjectHandleOnStack retType);

    static
    void QCALLTYPE GetGenericTypeDefinition(EnregisteredTypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType);

    static FCDECL2(FC_BOOL_RET, CompareCanonicalHandles, PTR_ReflectClassBaseObject pLeft, PTR_ReflectClassBaseObject pRight);

    static FCDECL1(PtrArray*, GetInterfaces, ReflectClassBaseObject *pType);

    static
    void QCALLTYPE GetConstraints(EnregisteredTypeHandle pTypeHandle, QCall::ObjectHandleOnStack retTypes);

    static
    PVOID QCALLTYPE GetGCHandle(EnregisteredTypeHandle pTypeHandle, INT32 handleType);

    static FCDECL1(INT32, GetCorElementType, PTR_ReflectClassBaseObject pType);
    static FCDECL1(ReflectClassBaseObject*, GetElementType, ReflectClassBaseObject* pType);

    static FCDECL2(MethodDesc*, GetMethodAt, PTR_ReflectClassBaseObject pType, INT32 slot);
    static FCDECL1(INT32, GetNumVirtuals, ReflectClassBaseObject *pType);
    
    static 
    void QCALLTYPE VerifyInterfaceIsImplemented(EnregisteredTypeHandle pTypeHandle, EnregisteredTypeHandle pIFaceHandle);

    static
    INT32 QCALLTYPE GetInterfaceMethodImplementationSlot(EnregisteredTypeHandle pTypeHandle, EnregisteredTypeHandle pOwner, MethodDesc * pMD);

    static FCDECL3(FC_BOOL_RET, GetFields, ReflectClassBaseObject *pType, INT32 **result, INT32 *pCount);

    static FCDECL1(MethodDesc *, GetFirstIntroducedMethod, ReflectClassBaseObject* pType);
    static FCDECL1(void, GetNextIntroducedMethod, MethodDesc **ppMethod);
    
    static FCDECL2(Object*, CreateInstanceForGenericType, ReflectClassBaseObject* pType
        , ReflectClassBaseObject* parameterType );	

    static
    FCDECL1(IMDInternalImport*, GetMetadataImport, ReflectClassBaseObject * pModuleUNSAFE);
};

class RuntimeMethodHandle {
    
public:  
    static FCDECL1(ReflectMethodObject*, GetCurrentMethod, StackCrawlMark* stackMark);

    static FCDECL4(Object*, InvokeMethod, Object *target, PTRArray *objs, SignatureNative* pSig, CLR_BOOL fConstructor);
	
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

    static
    BOOL QCALLTYPE IsSecurityCritical(MethodDesc *pMD);

    static
    BOOL QCALLTYPE IsSecuritySafeCritical(MethodDesc *pMD);

    static
    BOOL QCALLTYPE IsSecurityTransparent(MethodDesc *pMD);

    static FCDECL2(FC_BOOL_RET, IsTokenSecurityTransparent, ReflectModuleBaseObject *pModuleUNSAFE, INT32 tkToken);

    static
    BOOL QCALLTYPE IsCAVisibleFromDecoratedType(
        EnregisteredTypeHandle targetTypeHandle,
        MethodDesc * pTargetCtor,
        EnregisteredTypeHandle sourceTypeHandle,
        QCall::ModuleHandle sourceModuleHandle);

    static FCDECL3(void, CheckLinktimeDemands, ReflectMethodObject *pMethodUNSAFE, ReflectModuleBaseObject *pModuleUNSAFE, CLR_BOOL isDecoratedTargetSecurityTransparent);
    static FCDECL4(void, SerializationInvoke, ReflectMethodObject *pMethodUNSAFE, Object* targetUNSAFE,
        Object* serializationInfoUNSAFE, struct StreamingContextData * pContext);

    static
    void QCALLTYPE ConstructInstantiation(MethodDesc * pMethod, DWORD format, QCall::StringHandleOnStack retString);
    
    static
    void * QCALLTYPE GetFunctionPointer(MethodDesc * pMethod);

    static FCDECL1(INT32, GetAttributes, MethodDesc *pMethod);
    static FCDECL1(INT32, GetImplAttributes, ReflectMethodObject *pMethodUNSAFE);
    static FCDECL1(ReflectClassBaseObject*, GetDeclaringType, MethodDesc *pMethod);
    static FCDECL1(INT32, GetSlot, MethodDesc *pMethod);
    static FCDECL1(INT32, GetMethodDef, ReflectMethodObject *pMethodUNSAFE);
    static FCDECL1(StringObject*, GetName, MethodDesc *pMethod);
    static FCDECL1(LPCUTF8, GetUtf8Name, MethodDesc *pMethod);
    static FCDECL2(FC_BOOL_RET, MatchesNameHash, MethodDesc * pMethod, ULONG hash);

    static
    void QCALLTYPE GetMethodInstantiation(MethodDesc * pMethod, QCall::ObjectHandleOnStack retTypes, BOOL fAsRuntimeTypeArray);

    static
    FCDECL1(FC_BOOL_RET, HasMethodInstantiation, MethodDesc *pMethod);

    static
    FCDECL1(FC_BOOL_RET, IsGenericMethodDefinition, MethodDesc *pMethod);

    static
    FCDECL1(FC_BOOL_RET, IsTypicalMethodDefinition, ReflectMethodObject *pMethodUNSAFE);

    static
    void QCALLTYPE GetTypicalMethodDefinition(MethodDesc * pMethod, QCall::ObjectHandleOnStack refMethod);

    static
    void QCALLTYPE StripMethodInstantiation(MethodDesc * pMethod, QCall::ObjectHandleOnStack refMethod);

    // see comment in the cpp file
    static FCDECL3(MethodDesc*, GetStubIfNeeded, MethodDesc *pMethod, ReflectClassBaseObject *pType, PtrArray* instArray);
    static FCDECL2(MethodDesc*, GetMethodFromCanonical, MethodDesc *pMethod, PTR_ReflectClassBaseObject pType);

    static
    FCDECL1(FC_BOOL_RET, IsDynamicMethod, MethodDesc * pMethod);

    static
    FCDECL1(Object*, GetResolver, MethodDesc * pMethod);

    static 
    void QCALLTYPE Destroy(MethodDesc * pMethod);

    static
    void QCALLTYPE GetCallerType(QCall::StackCrawlMarkHandle pStackMark, QCall::ObjectHandleOnStack retType);

    static FCDECL2(MethodBody*, GetMethodBody, ReflectMethodObject *pMethodUNSAFE, PTR_ReflectClassBaseObject pDeclaringType);

    static FCDECL1(FC_BOOL_RET, IsConstructor, MethodDesc *pMethod);

    static FCDECL1(Object*, GetLoaderAllocator, MethodDesc *pMethod);
};

class RuntimeFieldHandle {
    
public:
    static FCDECL5(Object*, GetValue, ReflectFieldObject *pFieldUNSAFE, Object *instanceUNSAFE, ReflectClassBaseObject *pFieldType, ReflectClassBaseObject *pDeclaringType, CLR_BOOL *pDomainInitialized);
    static FCDECL7(void, SetValue, ReflectFieldObject *pFieldUNSAFE, Object *targetUNSAFE, Object *valueUNSAFE, ReflectClassBaseObject *pFieldType, DWORD attr, ReflectClassBaseObject *pDeclaringType, CLR_BOOL *pDomainInitialized);
    static FCDECL4(Object*, GetValueDirect, ReflectFieldObject *pFieldUNSAFE, ReflectClassBaseObject *pFieldType, TypedByRef *pTarget, ReflectClassBaseObject *pDeclaringType);
    static FCDECL5(void, SetValueDirect, ReflectFieldObject *pFieldUNSAFE, ReflectClassBaseObject *pFieldType, TypedByRef *pTarget, Object *valueUNSAFE, ReflectClassBaseObject *pContextType);
    static FCDECL1(StringObject*, GetName, ReflectFieldObject *pFieldUNSAFE);
    static FCDECL1(LPCUTF8, GetUtf8Name, FieldDesc *pField);
    static FCDECL2(FC_BOOL_RET, MatchesNameHash, FieldDesc * pField, ULONG hash);

    static FCDECL1(INT32, GetAttributes, FieldDesc *pField);
    static FCDECL1(ReflectClassBaseObject*, GetApproxDeclaringType, FieldDesc *pField);
    static FCDECL1(INT32, GetToken, ReflectFieldObject *pFieldUNSAFE);
    static FCDECL2(FieldDesc*, GetStaticFieldForGenericType, FieldDesc *pField, ReflectClassBaseObject *pDeclaringType);
    static FCDECL1(FC_BOOL_RET, AcquiresContextFromThis, FieldDesc *pField);

    static
    BOOL QCALLTYPE IsSecurityCritical(FieldDesc *pFD);

    static
    BOOL QCALLTYPE IsSecuritySafeCritical(FieldDesc *pFD);

    static
    BOOL QCALLTYPE IsSecurityTransparent(FieldDesc *pFD);

    static
    void QCALLTYPE CheckAttributeAccess(FieldDesc *pFD, QCall::ModuleHandle pModule);
};

class ModuleHandle {
    
public:
    static FCDECL5(ReflectMethodObject*, GetDynamicMethod, ReflectMethodObject *pMethodUNSAFE, ReflectModuleBaseObject *pModuleUNSAFE, StringObject *name, U1Array *sig, Object *resolver);
    static FCDECL1(INT32, GetToken, ReflectModuleBaseObject *pModuleUNSAFE);
    
    static
    void QCALLTYPE GetModuleType(QCall::ModuleHandle pModule, QCall::ObjectHandleOnStack retType);

    static
    FCDECL1(IMDInternalImport*, GetMetadataImport, ReflectModuleBaseObject * pModuleUNSAFE);

    static
    BOOL QCALLTYPE ContainsPropertyMatchingHash(QCall::ModuleHandle pModule, INT32 tkProperty, ULONG hash);

    static
    void QCALLTYPE ResolveType(QCall::ModuleHandle pModule, INT32 tkType, TypeHandle *typeArgs, INT32 typeArgsCount, TypeHandle *methodArgs, INT32 methodArgsCount, QCall::ObjectHandleOnStack retType);
    
    static
    MethodDesc * QCALLTYPE ResolveMethod(QCall::ModuleHandle pModule, INT32 tkMemberRef, TypeHandle *typeArgs, INT32 typeArgsCount, TypeHandle *methodArgs, INT32 methodArgsCount);

    static
    void QCALLTYPE ResolveField(QCall::ModuleHandle pModule, INT32 tkMemberRef, TypeHandle *typeArgs, INT32 typeArgsCount, TypeHandle *methodArgs, INT32 methodArgsCount, QCall::ObjectHandleOnStack retField);

    static
    void QCALLTYPE GetAssembly(QCall::ModuleHandle pModule, QCall::ObjectHandleOnStack retAssembly);

    static
    void QCALLTYPE GetPEKind(QCall::ModuleHandle pModule, DWORD* pdwPEKind, DWORD* pdwMachine);

    static
    FCDECL1(INT32, GetMDStreamVersion, ReflectModuleBaseObject * pModuleUNSAFE);
};

class AssemblyHandle {

public:
    static FCDECL1(ReflectModuleBaseObject*, GetManifestModule, AssemblyBaseObject *pAssemblyUNSAFE);

    static FCDECL1(INT32, GetToken, AssemblyBaseObject *pAssemblyUNSAFE);   
#ifdef FEATURE_APTCA
    static FCDECL2(FC_BOOL_RET, AptcaCheck, AssemblyBaseObject *pTargetAssemblyUNSAFE, AssemblyBaseObject *pSourceAssemblyUNSAFE);
#endif // FEATURE_APTCA
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
    static FCDECL3(Object *, GetCustomModifiers, SignatureNative* pSig, INT32 parameter, CLR_BOOL fRequired);
    static FCDECL2(FC_BOOL_RET, CompareSig, SignatureNative* pLhs, SignatureNative* pRhs);
    static FCDECL4(FC_BOOL_RET, CompareSigForAppCompat, SignatureNative* pLhs, ReflectClassBaseObject * pTypeLhs, SignatureNative* pRhs, ReflectClassBaseObject * pTypeRhs);


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
        SetObjectReference(&m_returnType, returnType, GetAppDomain());
    }

    void SetKeepAlive(OBJECTREF keepAlive)
    {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        SetObjectReference(&m_keepalive, keepAlive, GetAppDomain());
    }

    void SetDeclaringType(REFLECTCLASSBASEREF declaringType)
    {  
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        SetObjectReference((OBJECTREF*)&m_declaringType, (OBJECTREF)declaringType, GetAppDomain());
    }

    void SetArgumentArray(PTRARRAYREF ptrArrayarguments)
    {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        SetObjectReference((OBJECTREF*)&m_PtrArrayarguments, (OBJECTREF)ptrArrayarguments, GetAppDomain());
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

