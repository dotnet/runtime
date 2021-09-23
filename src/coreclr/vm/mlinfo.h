// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: mlinfo.h
//

//


#include "stubgen.h"
#include "custommarshalerinfo.h"

#ifndef _MLINFO_H_
#define _MLINFO_H_

#define NATIVE_TYPE_DEFAULT NATIVE_TYPE_MAX
#define VARIABLESIZE ((BYTE)(-1))


#ifdef FEATURE_COMINTEROP
class DispParamMarshaler;
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP
enum DispatchWrapperType
{
    DispatchWrapperType_Unknown         = 0x00000001,
    DispatchWrapperType_Dispatch        = 0x00000002,
    //DispatchWrapperType_Record          = 0x00000004,
    DispatchWrapperType_Error           = 0x00000008,
    DispatchWrapperType_Currency        = 0x00000010,
    DispatchWrapperType_BStr            = 0x00000020,
    DispatchWrapperType_SafeArray       = 0x00010000
};
#endif // FEATURE_COMINTEROP

typedef enum
{
    HANDLEASNORMAL  = 0,
    OVERRIDDEN      = 1,
    DISALLOWED      = 2,
} MarshalerOverrideStatus;


enum MarshalFlags
{
    MARSHAL_FLAG_CLR_TO_NATIVE      = 0x001,
    MARSHAL_FLAG_IN                 = 0x002,
    MARSHAL_FLAG_OUT                = 0x004,
    MARSHAL_FLAG_BYREF              = 0x008,
    MARSHAL_FLAG_HRESULT_SWAP       = 0x010,
    MARSHAL_FLAG_RETVAL             = 0x020,
    // unused                       = 0x040,
    MARSHAL_FLAG_FIELD              = 0x080
};

#include <pshpack1.h>
// Captures arguments for C array marshaling.
struct CREATE_MARSHALER_CARRAY_OPERANDS
{
    MethodTable*    methodTable;
    UINT32          multiplier;
    UINT32          additive;
    VARTYPE         elementType;
    UINT16          countParamIdx;
    BYTE            bestfitmapping;
    BYTE            throwonunmappablechar;
};
#include <poppack.h>

struct OverrideProcArgs
{
    class MarshalInfo*  m_pMarshalInfo;

    union
    {
        MethodTable*        m_pMT;

        struct
        {
            MethodTable*    m_pArrayMT;
            VARTYPE         m_vt;
#ifdef FEATURE_COMINTEROP
            SIZE_T          m_cbElementSize;
#endif // FEATURE_COMINTEROP
        } na;

        struct
        {
            MethodTable* m_pMT;
            MethodDesc*  m_pCopyCtor;
            MethodDesc*  m_pDtor;
        } mm;

        struct
        {
            MethodDesc* m_pMD;
            mdToken     m_paramToken;
            void*       m_hndManagedType; // TypeHandle cannot be a union member
        } rcm;  // MARSHAL_TYPE_REFERENCECUSTOMMARSHALER

        struct
        {
            UINT32 fixedStringLength;
        } fs;
    };
};

typedef MarshalerOverrideStatus (*OVERRIDEPROC)(NDirectStubLinker*    psl,
                                                BOOL                  byref,
                                                BOOL                  fin,
                                                BOOL                  fout,
                                                BOOL                  fManagedToNative,
                                                OverrideProcArgs*     pargs,
                                                UINT*                 pResID,
                                                UINT                  argidx,
                                                UINT                  nativeStackOffset);

typedef MarshalerOverrideStatus (*RETURNOVERRIDEPROC)(NDirectStubLinker*  psl,
                                                      BOOL                fManagedToNative,
                                                      BOOL                fHresultSwap,
                                                      OverrideProcArgs*   pargs,
                                                      UINT*               pResID);

//==========================================================================
// This structure contains the native type information for a given
// parameter.
//==========================================================================
struct NativeTypeParamInfo
{
    NativeTypeParamInfo()
    : m_NativeType(NATIVE_TYPE_DEFAULT)
    , m_ArrayElementType(NATIVE_TYPE_DEFAULT)
    , m_SizeIsSpecified(FALSE)
    , m_CountParamIdx(0)
    , m_Multiplier(0)
    , m_Additive(1)
    , m_strCMMarshalerTypeName(NULL)
    , m_cCMMarshalerTypeNameBytes(0)
    , m_strCMCookie(NULL)
    , m_cCMCookieStrBytes(0)
#ifdef FEATURE_COMINTEROP
    , m_SafeArrayElementVT(VT_EMPTY)
    , m_strSafeArrayUserDefTypeName(NULL)
    , m_cSafeArrayUserDefTypeNameBytes(0)
    , m_IidParamIndex(-1)
    , m_strInterfaceTypeName(NULL)
    , m_cInterfaceTypeNameBytes(0)
#endif // FEATURE_COMINTEROP
    {
        LIMITED_METHOD_CONTRACT;
    }

    // The native type of the parameter.
    CorNativeType           m_NativeType;

    // for NT_ARRAY only
    CorNativeType           m_ArrayElementType; // The array element type.

    BOOL                    m_SizeIsSpecified;  // used to do some validation
    UINT16                  m_CountParamIdx;    // index of "sizeis" parameter
    UINT32                  m_Multiplier;       // multipler for "sizeis"
    UINT32                  m_Additive;         // additive for 'sizeis"

    // For NT_CUSTOMMARSHALER only.
    LPUTF8                  m_strCMMarshalerTypeName;
    DWORD                   m_cCMMarshalerTypeNameBytes;
    LPUTF8                  m_strCMCookie;
    DWORD                   m_cCMCookieStrBytes;

#ifdef FEATURE_COMINTEROP
    // For NT_SAFEARRAY only.
    VARTYPE                 m_SafeArrayElementVT;
    LPUTF8                  m_strSafeArrayUserDefTypeName;
    DWORD                   m_cSafeArrayUserDefTypeNameBytes;

    DWORD                   m_IidParamIndex;    // Capture iid_is syntax from IDL.

    // for NATIVE_TYPE_SPECIFIED_INTERFACE
    LPUTF8                  m_strInterfaceTypeName;
    DWORD                   m_cInterfaceTypeNameBytes;
#endif // FEATURE_COMINTEROP
};

BOOL ParseNativeTypeInfo(mdToken                    token,
                         IMDInternalImport*         pScope,
                         NativeTypeParamInfo*       pParamInfo);

#ifdef _DEBUG
BOOL IsFixedBuffer(mdFieldDef field, IMDInternalImport* pInternalImport);
#endif

#ifdef FEATURE_COMINTEROP
class OleColorMarshalingInfo
{
public:
    // Constructor.
    OleColorMarshalingInfo();

    // OleColorMarshalingInfo's are always allocated on the loader heap so we need to redefine
    // the new and delete operators to ensure this.
    void *operator new(size_t size, LoaderHeap *pHeap);
    void operator delete(void *pMem);

    // Accessors.
    TypeHandle GetColorType()
    {
        LIMITED_METHOD_CONTRACT;
        return m_hndColorType;
    }
    MethodDesc *GetOleColorToSystemColorMD()
    {
        LIMITED_METHOD_CONTRACT;
        return m_OleColorToSystemColorMD;
    }
    MethodDesc *GetSystemColorToOleColorMD()
    {
        LIMITED_METHOD_CONTRACT;
        return m_SystemColorToOleColorMD;
    }


private:
    TypeHandle  m_hndColorType;
    MethodDesc* m_OleColorToSystemColorMD;
    MethodDesc* m_SystemColorToOleColorMD;
};

#endif // FEATURE_COMINTEROP


class EEMarshalingData
{
public:
    EEMarshalingData(LoaderAllocator *pAllocator, CrstBase *pCrst);
    ~EEMarshalingData();

    // EEMarshalingData's are always allocated on the loader heap so we need to redefine
    // the new and delete operators to ensure this.
    void *operator new(size_t size, LoaderHeap *pHeap);
    void operator delete(void *pMem);

    // This method returns the custom marshaling helper associated with the name cookie pair. If the
    // CM info has not been created yet for this pair then it will be created and returned.
    CustomMarshalerHelper *GetCustomMarshalerHelper(Assembly *pAssembly, TypeHandle hndManagedType, LPCUTF8 strMarshalerTypeName, DWORD cMarshalerTypeNameBytes, LPCUTF8 strCookie, DWORD cCookieStrBytes);

    // This method returns the custom marshaling info associated with shared CM helper.
    CustomMarshalerInfo *GetCustomMarshalerInfo(SharedCustomMarshalerHelper *pSharedCMHelper);

#ifdef FEATURE_COMINTEROP
    // This method retrieves OLE_COLOR marshaling info.
    OleColorMarshalingInfo *GetOleColorMarshalingInfo();


#endif // FEATURE_COMINTEROP

private:
    EECMHelperHashTable                 m_CMHelperHashtable;
    EEPtrHashTable                      m_SharedCMHelperToCMInfoMap;
    LoaderAllocator*                    m_pAllocator;
    LoaderHeap*                         m_pHeap;
    CMINFOLIST                          m_pCMInfoList;
#ifdef FEATURE_COMINTEROP
    OleColorMarshalingInfo*             m_pOleColorInfo;
#endif // FEATURE_COMINTEROP
    CrstBase*                           m_lock;
};

struct ItfMarshalInfo;

class MarshalInfo
{
public:
    enum MarshalType
    {
#define DEFINE_MARSHALER_TYPE(mtype, mclass) mtype,
#include "mtypes.h"
        MARSHAL_TYPE_UNKNOWN
    };

    enum MarshalScenario
    {
        MARSHAL_SCENARIO_NDIRECT,
#ifdef FEATURE_COMINTEROP
        MARSHAL_SCENARIO_COMINTEROP,
#endif // FEATURE_COMINTEROP
        MARSHAL_SCENARIO_FIELD
    };

private:

public:
    void *operator new(size_t size, void *pInPlace)
    {
        LIMITED_METHOD_CONTRACT;
        return pInPlace;
    }

    MarshalInfo(Module* pModule,
                SigPointer sig,
                const SigTypeContext *pTypeContext,
                mdToken token,
                MarshalScenario ms,
                CorNativeLinkType nlType,
                CorNativeLinkFlags nlFlags,
                BOOL isParam,
                UINT paramidx,          // parameter # for use in error messages (ignored if not parameter)
                UINT numArgs,           // number of arguments. used to check SizeParamIndex is within valid range
                BOOL BestFit,
                BOOL ThrowOnUnmappableChar,
                BOOL fEmitsIL,
                MethodDesc* pMD = NULL,
                BOOL fUseCustomMarshal = TRUE
#ifdef _DEBUG
                ,
                LPCUTF8 pDebugName = NULL,
                LPCUTF8 pDebugClassName = NULL,
                UINT    argidx = 0  // 0 for return value, -1 for field
#endif

                );

    VOID EmitOrThrowInteropParamException(NDirectStubLinker* psl, BOOL fMngToNative, UINT resID, UINT paramIdx);

    void ThrowTypeLoadExceptionForInvalidFieldMarshal(FieldDesc* pFieldDesc, UINT resID);

    // These methods retrieve the information for different element types.
    HRESULT HandleArrayElemType(NativeTypeParamInfo *pParamInfo,
                                TypeHandle elemTypeHnd,
                                int iRank,
                                BOOL fNoLowerBounds,
                                BOOL isParam,
                                Assembly *pAssembly,
                                BOOL isArrayClass = FALSE);

    void GenerateArgumentIL(NDirectStubLinker* psl,
                            int argOffset, // the argument's index is m_paramidx + argOffset
                            UINT nativeStackOffset, // offset of the argument on the native stack
                            BOOL fMngToNative);

    void GenerateReturnIL(NDirectStubLinker* psl,
                          int argOffset, // the argument's index is m_paramidx + argOffset
                          BOOL fMngToNative,
                          BOOL fieldGetter,
                          BOOL retval);

    void GenerateFieldIL(NDirectStubLinker* psl,
                        UINT32 managedOffset, // the field's byte offset into the managed object
                        UINT32 nativeOffset, // the field's byte offset into the native object
                        FieldDesc* pFieldDesc); // The field descriptor for reporting errors

    OverrideProcArgs const* GetOverrideProcArgs()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_args;
    }

    void SetupArgumentSizes();

    UINT16 GetNativeArgSize()
    {
        LIMITED_METHOD_CONTRACT;
        return m_nativeArgSize;
    }

    MarshalType GetMarshalType()
    {
        LIMITED_METHOD_CONTRACT;
        return m_type;
    }

    BYTE    GetBestFitMapping()
    {
        LIMITED_METHOD_CONTRACT;
        return ((m_BestFit == 0) ? 0 : 1);
    }

    BYTE    GetThrowOnUnmappableChar()
    {
        LIMITED_METHOD_CONTRACT;
        return ((m_ThrowOnUnmappableChar == 0) ? 0 : 1);
    }

    BOOL   IsFpuReturn()
    {
        LIMITED_METHOD_CONTRACT;
        return m_type == MARSHAL_TYPE_FLOAT || m_type == MARSHAL_TYPE_DOUBLE;
    }

    BOOL   IsIn()
    {
        LIMITED_METHOD_CONTRACT;
        return m_in;
    }

    BOOL   IsOut()
    {
        LIMITED_METHOD_CONTRACT;
        return m_out;
    }

    BOOL   IsByRef()
    {
        LIMITED_METHOD_CONTRACT;
        return m_byref;
    }

    Module* GetModule()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pModule;
    }

    int GetArrayRank()
    {
        LIMITED_METHOD_CONTRACT;
        return m_iArrayRank;
    }

    BOOL GetNoLowerBounds()
    {
        LIMITED_METHOD_CONTRACT;
        return m_nolowerbounds;
    }

    // used the same logic of tlbexp to check whether the argument of the method is a VarArg
    BOOL IsOleVarArgCandidate()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fOleVarArgCandidate; // m_fOleVarArgCandidate is set in the constructor method
    }

    void GetMops(CREATE_MARSHALER_CARRAY_OPERANDS* pMopsOut)
    {
        WRAPPER_NO_CONTRACT;
        pMopsOut->methodTable = m_hndArrayElemType.AsMethodTable();
        pMopsOut->elementType = m_arrayElementType;
        pMopsOut->countParamIdx = m_countParamIdx;
        pMopsOut->multiplier  = m_multiplier;
        pMopsOut->additive    = m_additive;
        pMopsOut->bestfitmapping = GetBestFitMapping();
        pMopsOut->throwonunmappablechar = GetThrowOnUnmappableChar();
    }

    TypeHandle GetArrayElementTypeHandle()
    {
        return m_hndArrayElemType;
    }

#ifdef FEATURE_COMINTEROP
    DispParamMarshaler *GenerateDispParamMarshaler();
    DispatchWrapperType GetDispWrapperType();
#endif // FEATURE_COMINTEROP

    void GetItfMarshalInfo(ItfMarshalInfo* pInfo);

    // Helper functions used to map the specified type to its interface marshalling info.
    static void GetItfMarshalInfo(TypeHandle th, BOOL fDispItf, MarshalScenario ms, ItfMarshalInfo *pInfo);

    // Returns true if the specified marshaler requires COM to have been started.
    bool MarshalerRequiresCOM();

    MethodDesc *GetMethodDesc()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pMD;
    }

    UINT GetParamIndex()
    {
        LIMITED_METHOD_CONTRACT;
        return m_paramidx;
    }

    BOOL IsFieldScenario()
    {
        LIMITED_METHOD_CONTRACT;
        return m_ms == MarshalInfo::MARSHAL_SCENARIO_FIELD;
    }

    UINT GetErrorResourceId()
    {
        LIMITED_METHOD_CONTRACT;
        return m_resID;
    }

private:

    UINT16                      GetNativeSize(MarshalType mtype);
    static bool                 IsInOnly(MarshalType mtype);
    static bool                 IsValueClass(MarshalType mtype);

    static OVERRIDEPROC         GetArgumentOverrideProc(MarshalType mtype);
    static RETURNOVERRIDEPROC   GetReturnOverrideProc(MarshalType mtype);

#ifdef _DEBUG
    VOID DumpMarshalInfo(Module* pModule, SigPointer sig, const SigTypeContext *pTypeContext, mdToken token,
                         MarshalScenario ms, CorNativeLinkType nlType, CorNativeLinkFlags nlFlags);
#endif

private:
    MarshalType     m_type;
    BOOL            m_byref;
    BOOL            m_in;
    BOOL            m_out;
    MethodTable*    m_pMT;  // Used if this is a true value type
    MethodDesc*     m_pMD;  // Save MethodDesc for later inspection so that we can pass SizeParamIndex by ref
    TypeHandle      m_hndArrayElemType;
    VARTYPE         m_arrayElementType;
    int             m_iArrayRank;
    BOOL            m_nolowerbounds;  // if managed type is SZARRAY, don't allow lower bounds

    // for NT_ARRAY only
    UINT32          m_multiplier;     // multipler for "sizeis"
    UINT32          m_additive;       // additive for 'sizeis"
    UINT16          m_countParamIdx;  // index of "sizeis" parameter

    UINT16          m_nativeArgSize;

    MarshalScenario m_ms;
    BOOL            m_fAnsi;
    BOOL            m_fDispItf;
#ifdef FEATURE_COMINTEROP
    BOOL            m_fErrorNativeType;
#endif // FEATURE_COMINTEROP

    // Information used by NT_CUSTOMMARSHALER.
    CustomMarshalerHelper* m_pCMHelper;
    VARTYPE         m_CMVt;

    OverrideProcArgs  m_args;

    UINT            m_paramidx;
    UINT            m_resID;     // resource ID for error message (if any)
    BOOL            m_BestFit;
    BOOL            m_ThrowOnUnmappableChar;

    BOOL            m_fOleVarArgCandidate; // indicate whether the arg is a candidate for vararg or not

#if defined(_DEBUG)
    LPCUTF8         m_strDebugMethName;
    LPCUTF8         m_strDebugClassName;
    UINT            m_iArg;  // 0 for return value, -1 for field
#endif

    Module*         m_pModule;
    mdToken         m_token;
};



//
// Flags used to control the behavior of the ArrayMarshalInfo class.
//

enum ArrayMarshalInfoFlags
{
    amiRuntime                                  = 0x0001,
    amiIsPtr                                    = 0x0008,
    amiSafeArraySubTypeExplicitlySpecified      = 0x0010
};

#define IsAMIRuntime(flags) ((flags) & amiRuntime)
#define IsAMIPtr(flags) ((flags) & amiIsPtr)
#define IsAMISafeArraySubTypeExplicitlySpecified(flags) ((flags) & amiSafeArraySubTypeExplicitlySpecified)
//
// Helper classes to determine the marshalling information for arrays.
//

class ArrayMarshalInfo
{
public:
    ArrayMarshalInfo(ArrayMarshalInfoFlags flags)
    : m_vtElement(VT_EMPTY)
    , m_errorResourceId(0)
    , m_flags(flags)
#ifdef FEATURE_COMINTEROP
    , m_cbElementSize(0)
#endif // FEATURE_COMINTEROP
    {
        WRAPPER_NO_CONTRACT;
    }

    void InitForNativeArray(MarshalInfo::MarshalScenario ms, TypeHandle elemTypeHnd, CorNativeType elementNativeType, BOOL isAnsi);
    void InitForFixedArray(TypeHandle elemTypeHnd, CorNativeType elementNativeType, BOOL isAnsi);

#ifdef FEATURE_COMINTEROP
    void InitForSafeArray(MarshalInfo::MarshalScenario ms, TypeHandle elemTypeHnd, VARTYPE elementVT, BOOL isAnsi);
#endif // FEATURE_COMINTEROP

    TypeHandle GetElementTypeHandle()
    {
        LIMITED_METHOD_CONTRACT;
        return m_thElement;
    }

    BOOL IsPtr()
    {
        LIMITED_METHOD_CONTRACT;
        return IsAMIPtr(m_flags);
    }

    VARTYPE GetElementVT()
    {
        LIMITED_METHOD_CONTRACT;
        if ((IsAMIRuntime(m_flags) && IsAMIPtr(m_flags)) != 0)
        {
            // for the purpose of marshaling, we don't care about the inner
            // type - we just marshal pointer-sized values
#ifdef TARGET_64BIT
            return VT_I8;
#else
            return VT_I4;
#endif
        }
        else
        {
            return m_vtElement;
        }
    }

    BOOL IsValid()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        return m_vtElement != VT_EMPTY;
    }

    BOOL IsSafeArraySubTypeExplicitlySpecified()
    {
        LIMITED_METHOD_CONTRACT;

        return IsAMISafeArraySubTypeExplicitlySpecified(m_flags);
    }

    DWORD GetErrorResourceId()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(!IsValid());
        }
        CONTRACTL_END;

        return m_errorResourceId;
    }

#ifdef FEATURE_COMINTEROP

    SIZE_T GetElementSize()
    {
        LIMITED_METHOD_CONTRACT;
        return m_cbElementSize;
    }
#endif // FEATURE_COMINTEROP

protected:
    // Helper function that does the actual work to figure out the element type handle and var type.
    void InitElementInfo(CorNativeType arrayNativeType, MarshalInfo::MarshalScenario ms, TypeHandle elemTypeHnd, CorNativeType elementNativeType, BOOL isAnsi);

    VARTYPE GetPointerSize()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef TARGET_64BIT
            return 8;
#else
            return 4;
#endif
    }

protected:
    TypeHandle m_thElement;
    TypeHandle m_thInterfaceArrayElementClass;
    VARTYPE m_vtElement;
    DWORD m_errorResourceId;
    ArrayMarshalInfoFlags m_flags;

#ifdef FEATURE_COMINTEROP
    SIZE_T m_cbElementSize;
#endif // FEATURE_COMINTEROP
};


//===================================================================================
// Throws an exception indicating a param has invalid element type / native type
// information.
//===================================================================================
VOID ThrowInteropParamException(UINT resID, UINT paramIdx);

VOID CollateParamTokens(IMDInternalImport *pInternalImport, mdMethodDef md, ULONG numargs, mdParamDef *aParams);
bool IsUnsupportedTypedrefReturn(MetaSig& msig);

// We'll cap the total native size at a (somewhat) arbitrary limit to ensure
// that we don't expose some overflow bug later on.
#define MAX_SIZE_FOR_INTEROP    0x7ffffff0

#endif // _MLINFO_H_
