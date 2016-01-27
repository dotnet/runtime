// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// MDValidator.cpp
// 

//
// Implementation for the MetaData validator.
// Only supported for full mscorwks version.
//
//*****************************************************************************
#include "stdafx.h"

#ifdef FEATURE_METADATA_VALIDATOR

#include "regmeta.h"
#include "importhelper.h"
#include "pedecoder.h"
#include "stgio.h"
#include "corhost.h"
#ifdef FEATURE_FUSION
#include "fusion.h"
#endif
#include "sstring.h"
#include "nsutilpriv.h"
#include "holder.h"
#include "vererror.h"

#include "mdsighelper.h"

#ifdef DACCESS_COMPILE
#error Dac should be using standalone version of metadata, not Wks version.
#endif

//-----------------------------------------------------------------------------
// Application specific debug macro.
#define IfBreakGo(EXPR) \
do {if ((EXPR) != S_OK) IfFailGo(VLDTR_E_INTERRUPTED); } while (0)

//-----------------------------------------------------------------------------

//#define CACHE_IMPLMAP_VALIDATION_RESULT
#ifdef CACHE_IMPLMAP_VALIDATION_RESULT
// To avoid multiple validation of the same thing:
struct ValidationResult
{
    mdToken     tok;
    HRESULT     hr;
};
ValidationResult*               g_rValidated=NULL; // allocated in ValidateMetaData
unsigned                        g_nValidated=0;
#endif

//-----------------------------------------------------------------------------

#define BASE_OBJECT_CLASSNAME   "Object"
#define BASE_NAMESPACE          "System"
#define BASE_VTYPE_CLASSNAME    "ValueType"
#define BASE_ENUM_CLASSNAME     "Enum"
#define BASE_VALUE_FIELDNAME    "value__"
#define BASE_CTOR_NAME          ".ctor"
#define BASE_CCTOR_NAME         ".cctor"
#define BASE_MCDELEGATE_CLASSNAME "MulticastDelegate"

#define SYSTEM_OBJECT_TOSTRING_METHODNAME    "ToString"
#define SYSTEM_OBJECT_GETHASHCODE_METHODNAME "GetHashCode"
#define SYSTEM_OBJECT_EQUALS_METHODNAME      "Equals"

// string ToString()
static const BYTE g_sigSystemObject_ToString[] = 
{ 
    IMAGE_CEE_CS_CALLCONV_HASTHIS,  // 0x20
    0,                              // 0x00 ... Param Count
    ELEMENT_TYPE_STRING             // 0x0e ... Return Type - string
};

// int GetHashCode()
static const BYTE g_sigSystemObject_GetHashCode[] = 
{ 
    IMAGE_CEE_CS_CALLCONV_HASTHIS,  // 0x20
    0,                              // 0x00 ... Param Count
    ELEMENT_TYPE_I4                 // 0x08 ... Return Type - I4
};

// bool Equals(object)
static const BYTE g_sigSystemObject_Equals[] = 
{ 
    IMAGE_CEE_CS_CALLCONV_HASTHIS,  // 0x20
    1,                              // 0x01 ... Param Count
    ELEMENT_TYPE_BOOLEAN,           // 0x02 ... Return Type - bool
    ELEMENT_TYPE_OBJECT             // 0x1c ... Param #1 - object
};

// as defined in src\vm\vars.hpp
#define MAX_CLASSNAME_LENGTH 1024
//-----------------------------------------------------------------------------
// Class names used in long form signatures (namespace is always "System")
unsigned g_NumSigLongForms = 19;
static const LPCSTR g_SigLongFormName[] = {
    "String",
    "______", // "Object", <REVISIT_TODO>// uncomment when EE handles ELEMENT_TYPE_OBJECT</REVISIT_TODO>
    "Boolean",
    "Char",
    "Byte",
    "SByte",
    "UInt16",
    "Int16",
    "UInt32",
    "Int32",
    "UInt64",
    "Int64",
    "Single",
    "Double",
    "SysInt",  // Review this.
    "SysUInt", // Review this.
    "SingleResult",
    "Void",
    "IntPtr"
};

// <REVISIT_TODO>: Why are these global variables?</REVISIT_TODO>
mdToken g_tkEntryPoint;
bool    g_fValidatingMscorlib;
bool    g_fIsDLL;

//-----------------------------------------------------------------------------

static HRESULT _FindClassLayout(
    CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
    mdTypeDef   tkParent,               // [IN] the parent that ClassLayout is associated with
    RID         *clRid,                 // [OUT] rid for the ClassLayout.
    RID         rid);                   // [IN] rid to be ignored.

static HRESULT _FindFieldLayout(
    CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
    mdFieldDef  tkParent,               // [IN] the parent that FieldLayout is associated with
    RID         *flRid,                 // [OUT] rid for the FieldLayout record.
    RID         rid);                   // [IN] rid to be ignored.

static BOOL _IsValidLocale(LPCUTF8 szLocale,
                           BOOL    fIsV2Assembly);


#define REPORT_ERROR0(_VECode)                                  \
    IfFailGo(_ValidateErrorHelper(_VECode, veCtxt))
#define REPORT_ERROR1(_VECode, _Arg0)                           \
    IfFailGo(_ValidateErrorHelper(_VECode, veCtxt, _Arg0))
#define REPORT_ERROR2(_VECode, _Arg0, _Arg1)                    \
    IfFailGo(_ValidateErrorHelper(_VECode, veCtxt, _Arg0, _Arg1))
#define REPORT_ERROR3(_VECode, _Arg0, _Arg1, _Arg2)             \
    IfFailGo(_ValidateErrorHelper(_VECode, veCtxt, _Arg0, _Arg1, _Arg2))

//*****************************************************************************
// Returns true if ixPtrTbl and ixParTbl are a valid parent-child combination
// in the pointer table scheme.
//*****************************************************************************
static inline bool IsTblPtr(ULONG ixPtrTbl, ULONG ixParTbl)
{
    if ((ixPtrTbl == TBL_Field && ixParTbl == TBL_TypeDef) ||
        (ixPtrTbl == TBL_Method && ixParTbl == TBL_TypeDef) ||
        (ixPtrTbl == TBL_Param && ixParTbl == TBL_Method) ||
        (ixPtrTbl == TBL_Property && ixParTbl == TBL_PropertyMap) ||
        (ixPtrTbl == TBL_Event && ixParTbl == TBL_EventMap))
    {
        return true;
    }
        return false;
}   // IsTblPtr()

//*****************************************************************************
// This inline function is used to set the return hr value for the Validate
// functions to one of VLDTR_S_WRN, VLDTR_S_ERR or VLDTR_S_WRNERR based on
// the current hr value and the new success code.
// The general algorithm for error codes from the validation functions is:
//      if (no warnings or errors found)
//          return S_OK or S_FALSE
//      else if (warnings found)
//          return VLDTR_S_WRN
//      else if (errors found)
//          return VLDTR_S_ERR
//      else if (warnings and errors found)
//          return VLDTR_S_WRNERR
//*****************************************************************************
static inline void SetVldtrCode(HRESULT *phr, HRESULT successcode)
{
    _ASSERTE(successcode == S_OK || successcode == S_FALSE ||successcode == VLDTR_S_WRN ||
             successcode == VLDTR_S_ERR || successcode == VLDTR_S_WRNERR);
    _ASSERTE(*phr == S_OK || *phr == VLDTR_S_WRN || *phr == VLDTR_S_ERR ||
             *phr == VLDTR_S_WRNERR);
    if (successcode == S_OK || successcode == S_FALSE ||*phr == VLDTR_S_WRNERR)
        return;
    else if (*phr == S_OK || *phr == S_FALSE)
        *phr = successcode;
    else if (*phr != successcode)
        *phr = VLDTR_S_WRNERR;
}   // SetVldtrCode()

//*****************************************************************************
// Initialize the Validator related structures in RegMeta.
//*****************************************************************************
HRESULT RegMeta::ValidatorInit(         // S_OK or error.
    DWORD       dwModuleType,           // [IN] Specifies whether the module is a PE file or an obj.
    IUnknown    *pUnk)                  // [IN] Validation error handler.
{
    HRESULT     hr = S_OK;              // Return value.

    BEGIN_ENTRYPOINT_NOTHROW;

    int         i = 0;                  // Index into the function pointer table.

    // Initialize the array of function pointers to the validation function on
    // each table.
#undef MiniMdTable
#define MiniMdTable(x) m_ValidateRecordFunctionTable[i++] = &RegMeta::Validate##x;
    MiniMdTables()

    // Verify that the ModuleType passed in is a valid one.
    if (dwModuleType < ValidatorModuleTypeMin ||
        dwModuleType > ValidatorModuleTypeMax)
    {
        IfFailGo(E_INVALIDARG);
    }

    // Verify that the interface passed in supports IID_IVEHandler.
    IfFailGo(pUnk->QueryInterface(IID_IVEHandler, (void **)&m_pVEHandler));

    // Set the ModuleType class member.  Do this last, this is used in
    // ValidateMetaData to see if the validator is correctly initialized.
    m_ModuleType = (CorValidatorModuleType)dwModuleType;
ErrExit:
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // HRESULT RegMeta::ValidatorInit()


//*****************************************************************************
// Public implementation for code:IMetaDataValidate::ValidateMetaData
// 
// Validate the entire MetaData.  Here is the basic algorithm.
//      for each table
//          for each record
//          {
//              Do generic validation - validate that the offsets into the blob
//              pool are good, validate that all the rids are within range,
//              validate that token encodings are consistent.
//          }
//      if (problems found in generic validation)
//          return;
//      for each table
//          for each record
//              Do semantic validation.
//******************************************************************************
HRESULT RegMeta::ValidateMetaData()
{
    HRESULT hr = S_OK;
    
    BEGIN_ENTRYPOINT_NOTHROW;
    
    CMiniMdRW * pMiniMd = &(m_pStgdb->m_MiniMd);
    HRESULT     hrSave = S_OK;      // Saved hr from generic validation.
    ULONG       ulCount;            // Count of records in the current table.
    ULONG       i;                  // Index to iterate over the tables.
    ULONG       j;                  // Index to iterate over the records in a given table.
    IHostTaskManager * pHostTaskManager = NULL;

#ifdef CACHE_IMPLMAP_VALIDATION_RESULT
    ULONG       rValidatedSize=0;   // Size of g_rValidated array
#endif

    // Verify that the validator is initialized correctly
    if (m_ModuleType == ValidatorModuleTypeInvalid)
    {
        _ASSERTE(!"Validator not initialized, initialize with ValidatorInit().");
        IfFailGo(VLDTR_E_NOTINIT);
    }

    // First do a validation pass to do some basic structural checks based on
    // the Meta-Meta data.  This'll validate all the offsets into the pools,
    // rid value and coded token ranges.
    for (i = 0; i < pMiniMd->GetCountTables(); i++)
    {
        ulCount = pMiniMd->GetCountRecs(i);

#ifdef CACHE_IMPLMAP_VALIDATION_RESULT
        switch(i)
        {
            case TBL_ImplMap:
                rValidatedSize += ulCount;
            default:
                ;
        }
#endif
        for (j = 1; j <= ulCount; j++)
        {
            IfFailGo(ValidateRecord(i, j));
            SetVldtrCode(&hrSave, hr);
        }
    }
    // Validate that the size of the Ptr tables matches with the corresponding
    // real tables.

    // Do not do semantic validation if structural validation failed.
    if (hrSave != S_OK)
    {
        hr = hrSave;
        goto ErrExit;
    }

    // Verify the entry point (if any)
    ::g_tkEntryPoint = 0;
    ::g_fIsDLL = false;
    if(m_pStgdb && m_pStgdb->m_pImage)
    {
        NewHolder<PEDecoder> pe;

        EX_TRY
        {
            // We need to use different PEDecoder constructors based on the type of data we give it.
            // We use the one with a 'bool' as the second argument when dealing with a mapped file,
            // and we use the one that takes a COUNT_T as the second argument when dealing with a
            // flat file.

            if (m_pStgdb->m_pStgIO->GetMemoryMappedType() == MTYPE_IMAGE)
                pe = new (nothrow) PEDecoder(m_pStgdb->m_pImage, false);
            else
                pe = new (nothrow) PEDecoder(m_pStgdb->m_pImage, (COUNT_T)(m_pStgdb->m_dwImageSize));

            hr = S_OK;
        }
        EX_CATCH
        {
            hr = COR_E_BADIMAGEFORMAT;
        }
        EX_END_CATCH(SwallowAllExceptions)

        if (SUCCEEDED(hr) && pe == NULL)
            IfFailGo(E_OUTOFMEMORY);

        if(FAILED(hr) || !pe->CheckFormat())
        {
            VEContext   veCtxt;             // Context structure.
            
            memset(&veCtxt, 0, sizeof(VEContext));
            veCtxt.Token = 0;
            veCtxt.uOffset = 0;
            REPORT_ERROR0(COR_E_BADIMAGEFORMAT);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else if(!pe->IsILOnly())
        {
            VEContext   veCtxt;             // Context structure.
            memset(&veCtxt, 0, sizeof(VEContext));
            veCtxt.Token = 0;
            veCtxt.uOffset = 0;
            REPORT_ERROR0(VER_E_BAD_PE);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        if((pe->GetCorHeader()->Flags & COMIMAGE_FLAGS_NATIVE_ENTRYPOINT) == 0)
                g_tkEntryPoint = pe->GetEntryPointToken();
        g_fIsDLL = pe->IsDll() ? true : false;

        if(g_tkEntryPoint)
        {
            RID rid = RidFromToken(g_tkEntryPoint);
            RID maxrid = 0;
            switch(TypeFromToken(g_tkEntryPoint))
            {
                case mdtMethodDef:  maxrid = pMiniMd->getCountMethods(); break;
                case mdtFile:       maxrid = pMiniMd->getCountFiles(); break;
                default:            break;
            }
            if((rid == 0)||(rid > maxrid))
            {
                VEContext   veCtxt;             // Context structure.
                memset(&veCtxt, 0, sizeof(VEContext));
                veCtxt.Token = g_tkEntryPoint;
                veCtxt.uOffset = 0;
                REPORT_ERROR0(VLDTR_E_EP_BADTOKEN);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
        }
        else if(!g_fIsDLL) // exe must have an entry point
        {
            VEContext   veCtxt;             // Context structure.
            memset(&veCtxt, 0, sizeof(VEContext));
            veCtxt.Token = g_tkEntryPoint;
            veCtxt.uOffset = 0;
            REPORT_ERROR0(VLDTR_E_EP_BADTOKEN);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

    g_fValidatingMscorlib = false;
    if(pMiniMd->GetCountRecs(TBL_Assembly))
    {
        AssemblyRec *pRecord;
        IfFailGo(pMiniMd->GetAssemblyRecord(1, &pRecord));
        LPCSTR szName;
        IfFailGo(pMiniMd->getNameOfAssembly(pRecord, &szName));
        g_fValidatingMscorlib = (0 == SString::_stricmp(szName,"mscorlib"));
    }
    // Verify there are no circular class hierarchies.

    // Do per record semantic validation on the MetaData.  The function
    // pointers to the per record validation are stored in the table by the
    // ValidatorInit() function.

#ifdef CACHE_IMPLMAP_VALIDATION_RESULT
    g_rValidated = NULL;
    ::g_nValidated = 0;
    if (rValidatedSize)
    {
        g_rValidated = new(nothrow) ValidationResult[rValidatedSize];
        IfNullGo(g_rValidated);
    }
#endif
    pHostTaskManager = CorHost2::GetHostTaskManager();

#ifdef Sleep
#undef Sleep
#endif
    //DWORD cBegin=0,cEnd=0;
    for (i = 0; i < pMiniMd->GetCountTables(); i++)
    {
        ulCount = pMiniMd->GetCountRecs(i);
        //cBegin = GetTickCount();
        for (j = 1; j <= ulCount; j++)
        {
            IfFailGo((this->*m_ValidateRecordFunctionTable[i])(j));
            SetVldtrCode(&hrSave, hr);
            if(pHostTaskManager)
            {
                // SwitchToTask forces the current thread to give up quantum, while a host can decide what
                // to do with Sleep if the current thread has not run out of quantum yet.
                ClrSleepEx(0, FALSE);
            }
        }
        //cEnd = GetTickCount();
        //printf("Table %d, recs: %d, time: %d\n",i,ulCount,(cEnd-cBegin));
    }
    hr = hrSave;
ErrExit:

#ifdef CACHE_IMPLMAP_VALIDATION_RESULT
    if(g_rValidated) delete [] g_rValidated;
#endif
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateMetaData()

//*****************************************************************************
// Validate the Module record.
//*****************************************************************************
HRESULT RegMeta::ValidateModule(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd for the scope.
    ModuleRec   *pRecord;           // Module record.
    VEContext   veCtxt;             // Context structure.
    HRESULT     hr = S_OK;          // Value returned.
    HRESULT     hrSave = S_OK;      // Save state.
    LPCSTR      szName;
    GUID GuidOfModule;

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));

    // Get the Module record.
    veCtxt.Token = TokenFromRid(rid, mdtModule);
    veCtxt.uOffset = 0;
    IfFailGo(pMiniMd->GetModuleRecord(rid, &pRecord));

    // There can only be one Module record.
    if (rid > 1)
    {
        REPORT_ERROR0(VLDTR_E_MOD_MULTI);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Verify the name
    IfFailGo(pMiniMd->getNameOfModule(pRecord, &szName));
    if(szName && *szName)
    {
        ULONG L = (ULONG)strlen(szName);
        if(L >= MAX_CLASSNAME_LENGTH)
        {
            // Name too long
            REPORT_ERROR2(VLDTR_E_TD_NAMETOOLONG, L, (ULONG)(MAX_CLASSNAME_LENGTH-1));
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        if(strchr(szName,':') || strchr(szName,'\\'))
        {
            REPORT_ERROR0(VLDTR_E_MOD_NAMEFULLQLFD);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    else
    {
        REPORT_ERROR0(VLDTR_E_MOD_NONAME);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // Verify that the MVID is valid.
    IfFailGo(pMiniMd->getMvidOfModule(pRecord, &GuidOfModule));
    if (GuidOfModule == GUID_NULL)
    {
        REPORT_ERROR0(VLDTR_E_MOD_NULLMVID);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateModule()

//*****************************************************************************
// Validate the given TypeRef.
//*****************************************************************************
HRESULT RegMeta::ValidateTypeRef(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    TypeRefRec  *pRecord;               // TypeRef record.
    mdToken     tkRes;                  // Resolution scope.
    LPCSTR      szNamespace;            // TypeRef Namespace.
    LPCSTR      szName;                 // TypeRef Name.
    mdTypeRef   tkTypeRef;              // Duplicate TypeRef.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));
    // Get the TypeRef record.
    veCtxt.Token = TokenFromRid(rid, mdtTypeRef);
    veCtxt.uOffset = 0;

    IfFailGo(pMiniMd->GetTypeRefRecord(rid, &pRecord));

    // Check name is not NULL.
    IfFailGo(pMiniMd->getNamespaceOfTypeRef(pRecord, &szNamespace));
    IfFailGo(pMiniMd->getNameOfTypeRef(pRecord, &szName));
    if (!*szName)
    {
        REPORT_ERROR0(VLDTR_E_TR_NAMENULL);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else
    {
        RID ridScope;
        // Look for a Duplicate, this function reports only one duplicate.
        tkRes = pMiniMd->getResolutionScopeOfTypeRef(pRecord);
        hr = ImportHelper::FindTypeRefByName(pMiniMd, tkRes, szNamespace, szName, &tkTypeRef, rid);
        if (hr == S_OK)
        {
            REPORT_ERROR1(VLDTR_E_TR_DUP, tkTypeRef);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else if (hr == CLDB_E_RECORD_NOTFOUND)
            hr = S_OK;
        else
            IfFailGo(hr);
        ULONG L = (ULONG)(strlen(szName)+strlen(szNamespace));
        if(L >= MAX_CLASSNAME_LENGTH)
        {
            REPORT_ERROR2(VLDTR_E_TD_NAMETOOLONG, L, (ULONG)(MAX_CLASSNAME_LENGTH-1));
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        ridScope = RidFromToken(tkRes);
        if(ridScope)
        {
            bool badscope = true;
            //check if valid scope
            switch(TypeFromToken(tkRes))
            {
                case mdtAssemblyRef:
                case mdtModuleRef:
                case mdtModule:
                case mdtTypeRef:
                    badscope = !IsValidToken(tkRes);
                    break;
                default:
                    break;
            }
            if(badscope)
            {
                REPORT_ERROR1(VLDTR_E_TR_BADSCOPE, tkTypeRef);
                SetVldtrCode(&hrSave, VLDTR_S_WRN);
            }
        }
        else
        {
            // check if there is a ExportedType
            //hr = ImportHelper::FindExportedType(pMiniMd, szNamespace, szName, tkImpl, &tkExportedType, rid);
        }
        // Check if there is TypeDef with the same name
        if(!ridScope)
        {
            if((TypeFromToken(tkRes) != mdtTypeRef) &&
                (S_OK == ImportHelper::FindTypeDefByName(pMiniMd, szNamespace, szName, mdTokenNil,&tkTypeRef, 0)))
            {
                REPORT_ERROR1(VLDTR_E_TR_HASTYPEDEF, tkTypeRef);
                SetVldtrCode(&hrSave, VLDTR_S_WRN);
            }
        }
    }
    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateTypeRef()

//*****************************************************************************
// Validate the given TypeDef.
//*****************************************************************************
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
HRESULT RegMeta::ValidateTypeDef(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    TypeDefRec  *pRecord;               // TypeDef record.
    TypeDefRec  *pExtendsRec = 0;       // TypeDef record for the parent class.
    mdTypeDef   tkTypeDef;              // Duplicate TypeDef token.
    DWORD       dwFlags;                // TypeDef flags.
    DWORD       dwExtendsFlags;         // TypeDef flags of the parent class.
    LPCSTR      szName;                 // TypeDef Name.
    LPCSTR      szNameSpace;            // TypeDef NameSpace.
    LPCSTR      szExtName = NULL;       // Parent Name.
    LPCSTR      szExtNameSpace = NULL;  // Parent NameSpace.
    CQuickBytes qb;                     // QuickBytes for flexible allocation.
    mdToken     tkExtends;              // TypeDef of the parent class.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.
    mdToken     tkEncloser=mdTokenNil;  // Encloser, if any
    BOOL        bIsEnum,bExtendsEnum,bExtendsVType,bIsVType,bExtendsObject,bIsObject,bExtendsMCDelegate;
    BOOL        bHasMethods=FALSE, bHasFields=FALSE;

    BEGIN_ENTRYPOINT_NOTHROW;

    // Skip validating m_tdModule class.
    if (rid == RidFromToken(m_tdModule))
        goto ErrExit;

    memset(&veCtxt, 0, sizeof(VEContext));

    // Get the TypeDef record.
    veCtxt.Token = TokenFromRid(rid, mdtTypeDef);
    veCtxt.uOffset = 0;

    IfFailGo(pMiniMd->GetTypeDefRecord(rid, &pRecord));

    // Do checks for name validity..
    IfFailGo(pMiniMd->getNameOfTypeDef(pRecord, &szName));
    IfFailGo(pMiniMd->getNamespaceOfTypeDef(pRecord, &szNameSpace));
    if (!*szName)
    {
        // TypeDef Name is null.
        REPORT_ERROR0(VLDTR_E_TD_NAMENULL);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else if (!IsDeletedName(szName))
    {
        RID iRecord;
        IfFailGo(pMiniMd->FindNestedClassHelper(TokenFromRid(rid, mdtTypeDef), &iRecord));
        
        if (InvalidRid(iRecord))
        {
            tkEncloser = mdTokenNil;
        }
        else
        {
            NestedClassRec *pNestedClassRec;
            IfFailGo(pMiniMd->GetNestedClassRecord(iRecord, &pNestedClassRec));
            tkEncloser = pMiniMd->getEnclosingClassOfNestedClass(pNestedClassRec);
        }
        
        // Check for duplicates based on Name/NameSpace.  Do not do Dup checks
        // on deleted records.
        hr = ImportHelper::FindTypeDefByName(pMiniMd, szNameSpace, szName, tkEncloser,
                                             &tkTypeDef, rid);
        if (hr == S_OK)
        {
            REPORT_ERROR1(VLDTR_E_TD_DUPNAME, tkTypeDef);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else if (hr == CLDB_E_RECORD_NOTFOUND)
            hr = S_OK;
        else
            IfFailGo(hr);
        ULONG L = (ULONG)(strlen(szName)+strlen(szNameSpace));
        if(L >= MAX_CLASSNAME_LENGTH)
        {
            REPORT_ERROR2(VLDTR_E_TD_NAMETOOLONG, L, (ULONG)(MAX_CLASSNAME_LENGTH-1));
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

    // Get the flag value for the TypeDef.
    dwFlags = pMiniMd->getFlagsOfTypeDef(pRecord);
    // Do semantic checks on the flags.
    // RTSpecialName bit must be set on Deleted records.
    if (IsDeletedName(szName))
    {
        if(!IsTdRTSpecialName(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_TD_DLTNORTSPCL);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        hr = hrSave;
        goto ErrExit;
    }

    // If RTSpecialName bit is set, the record must be a Deleted record.
    if (IsTdRTSpecialName(dwFlags))
    {
        REPORT_ERROR0(VLDTR_E_TD_RTSPCLNOTDLT);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
        if(!IsTdSpecialName(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_TD_RTSPCLNOTSPCL);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

    // Check if flag value is valid
    {
        DWORD dwInvalidMask, dwExtraBits;
        dwInvalidMask = (DWORD)~(tdVisibilityMask | tdLayoutMask | tdClassSemanticsMask | 
                tdAbstract | tdSealed | tdSpecialName | tdImport | tdSerializable | tdWindowsRuntime | 
                tdStringFormatMask | tdBeforeFieldInit | tdReservedMask);
        // check for extra bits
        dwExtraBits = dwFlags & dwInvalidMask;
        if (dwExtraBits == 0)
        {
            // if no extra bits, check layout
            dwExtraBits = dwFlags & tdLayoutMask;
            if (dwExtraBits != tdLayoutMask)
            {
                // layout OK, check string format
                dwExtraBits = dwFlags & tdStringFormatMask;
                if (dwExtraBits != tdStringFormatMask)
                    dwExtraBits = 0;
            }
        }
        if (dwExtraBits != 0)
        {
            REPORT_ERROR1(VLDTR_E_TD_EXTRAFLAGS, dwExtraBits);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }


        // Generic types may be specified to have only AutoLayout or SequentialLayout (never ExplicitLayout).
        if (IsTdExplicitLayout(dwFlags))
        {
            HENUMInternal hEnumTyPars;
            ULONG ulTypeDefArity;
            hr = pMiniMd->FindGenericParamHelper(TokenFromRid(rid, mdtTypeDef), &hEnumTyPars);
            if (SUCCEEDED(hr))
            {
                IfFailGo(HENUMInternal::GetCount(&hEnumTyPars,&ulTypeDefArity));
                HENUMInternal::ClearEnum(&hEnumTyPars);
                if (ulTypeDefArity != 0)
                {
                    REPORT_ERROR0(VLDTR_E_TD_GENERICHASEXPLAYOUT);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
            }
        }
        
    }

    // Get the parent of the TypeDef.
    tkExtends = pMiniMd->getExtendsOfTypeDef(pRecord);

    // Check if TypeDef extends itself
    if (tkExtends == veCtxt.Token)
    {
        REPORT_ERROR0(VLDTR_E_TD_EXTENDSITSELF);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // Check if TypeDef extends one of its children
    if (RidFromToken(tkExtends)&&(TypeFromToken(tkExtends)==mdtTypeDef))
    {
        TypeDefRec *pRec;
        IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkExtends), &pRec));
        mdToken tkExtends2 = pMiniMd->getExtendsOfTypeDef(pRec);
        if( tkExtends2 == veCtxt.Token)
        {
            REPORT_ERROR0(VLDTR_E_TD_EXTENDSCHILD);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    
    
    
    if (IsNilToken(tkEncloser) == IsTdNested(dwFlags))
    {
        REPORT_ERROR0(IsNilToken(tkEncloser) ? VLDTR_E_TD_NESTEDNOENCL : VLDTR_E_TD_ENCLNOTNESTED);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    
    bIsObject = bIsEnum = bIsVType = FALSE;
    if(0 == strcmp(szNameSpace,BASE_NAMESPACE))
    {
        bIsObject = (0 == strcmp(szName,BASE_OBJECT_CLASSNAME));
        if(!bIsObject)
        {
            bIsEnum   = (0 == strcmp(szName,BASE_ENUM_CLASSNAME));
            if(!bIsEnum)
            {
                bIsVType  = (0 == strcmp(szName,BASE_VTYPE_CLASSNAME));
            }
        }
    }

    if (IsNilToken(tkExtends))
    {
        // If the parent token is nil, the class must be marked Interface,
        // unless it's the System.Object class.
        if ( !(bIsObject || IsTdInterface(dwFlags)))
        {
            REPORT_ERROR0(VLDTR_E_TD_NOTIFACEOBJEXTNULL);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        szExtName = "";
        szExtNameSpace = "";
    }
    else
    {

        // If tkExtends is a TypeSpec, extract the generic type and continue
        if (TypeFromToken(tkExtends) == mdtTypeSpec)
        {
            //@GENERICSVER: TODO first validate the spec

            TypeSpecRec *pRec;
            IfFailGo(pMiniMd->GetTypeSpecRecord(RidFromToken(tkExtends), &pRec));
            PCCOR_SIGNATURE pSig;
            ULONG       cSig;

            IfFailGo(pMiniMd->getSignatureOfTypeSpec(pRec, &pSig, &cSig));
           
            switch(CorSigUncompressElementType(pSig))
            { 
                default: 
                {
                    REPORT_ERROR1(VLDTR_E_TD_EXTBADTYPESPEC, tkExtends);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    szExtName = "";
                    szExtNameSpace = "";
                    break;
                }
                case ELEMENT_TYPE_GENERICINST:
                { 
                    switch(CorSigUncompressElementType(pSig)) 
                    { 
                        default:
                        {
                            REPORT_ERROR1(VLDTR_E_TD_EXTBADTYPESPEC, tkExtends);
                            SetVldtrCode(&hrSave, VLDTR_S_ERR);
                            szExtName = "";
                            szExtNameSpace = "";
                            break;
                        }
                        case ELEMENT_TYPE_VALUETYPE:
                        case ELEMENT_TYPE_CLASS: 
                        { 
                            tkExtends = CorSigUncompressToken(pSig);
                            break;
                        }
                    }
                }
            }
        }

        // If tkExtends is a TypeRef try to resolve it to a corresponding
        // TypeDef.  If it resolves successfully, issue a warning.  It means
        // that the Ref to Def optimization didn't happen successfully.
        if (TypeFromToken(tkExtends) == mdtTypeRef)
        {
            TypeRefRec *pTypeRefRec;
            IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(tkExtends), &pTypeRefRec));

            IfFailGo(pMiniMd->getNameOfTypeRef(pTypeRefRec, &szExtName));
            IfFailGo(pMiniMd->getNamespaceOfTypeRef(pTypeRefRec, &szExtNameSpace));

            BOOL fLookForDef = TRUE;
            mdToken tkResScope = pMiniMd->getResolutionScopeOfTypeRef(pTypeRefRec);
            if (TypeFromToken(tkResScope) == mdtAssemblyRef)
            {   // We will look for the TypeDef of the same name, only if the AssemblyRef has the same name as AssemblyDef
                fLookForDef = FALSE;
                RID ridResScope = RidFromToken(tkResScope);
                if ((ridResScope > 0) && (ridResScope <= pMiniMd->GetCountRecs(TBL_AssemblyRef)))
                {
                    if (pMiniMd->GetCountRecs(TBL_Assembly) > 0)
                    {
                        AssemblyRefRec * pAsmRefRec;
                        IfFailGo(pMiniMd->GetAssemblyRefRecord(ridResScope, &pAsmRefRec));
                        AssemblyRec *pAsmRec;
                        IfFailGo(pMiniMd->GetAssemblyRecord(1, &pAsmRec));
                        if ((pAsmRec != NULL) && (pAsmRefRec != NULL))
                        {
                            LPCUTF8 szAsmName;
                            IfFailGo(pMiniMd->getNameOfAssembly(pAsmRec, &szAsmName));
                            LPCUTF8 szAsmRefName;
                            IfFailGo(pMiniMd->getNameOfAssemblyRef(pAsmRefRec, &szAsmRefName));
                            if ((szAsmName != NULL) && (szAsmRefName != NULL))
                                fLookForDef = (strcmp(szAsmName,szAsmRefName) == 0);
                        }
                    }
                }
            }
            
            if (fLookForDef)
            {
                mdTypeDef tkResTd;
    
                if (ImportHelper::FindTypeDefByName(pMiniMd,
                        szExtNameSpace, 
                        szExtName, 
                        tkResScope, 
                        &tkResTd) == S_OK)
                {
                    // Ref to Def optimization is not expected to happen for Obj files.
                    /*
                    if (m_ModuleType != ValidatorModuleTypeObj)
                    {
                        REPORT_ERROR2(VLDTR_E_TD_EXTTRRES, tkExtends, tkResTd);
                        SetVldtrCode(&hrSave, VLDTR_S_WRN);
                    }
                    */
    
                    // Set tkExtends to the new TypeDef, so we can continue
                    // with the validation.
                    tkExtends = tkResTd;
                }
            }
        }

        // Continue validation, even for the case where TypeRef got resolved
        // to a corresponding TypeDef in the same Module.
        if (TypeFromToken(tkExtends) == mdtTypeDef)
        {
            // Extends must not be sealed.
            IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkExtends), &pExtendsRec));
            dwExtendsFlags = pMiniMd->getFlagsOfTypeDef(pExtendsRec);
            IfFailGo(pMiniMd->getNameOfTypeDef(pExtendsRec, &szExtName));
            IfFailGo(pMiniMd->getNamespaceOfTypeDef(pExtendsRec, &szExtNameSpace));
            if (IsTdSealed(dwExtendsFlags))
            {
                REPORT_ERROR1(VLDTR_E_TD_EXTENDSSEALED, tkExtends);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            if (IsTdInterface(dwExtendsFlags))
            {
                REPORT_ERROR1(VLDTR_E_TD_EXTENDSIFACE, tkExtends);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
        }
        else if(TypeFromToken(tkExtends) == mdtTypeSpec)
        {
            //If we got here, the instantiated generic type is itself a type spec, which is illegal
            REPORT_ERROR1(VLDTR_E_TD_EXTBADTYPESPEC, tkExtends);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
            szExtName = "";
            szExtNameSpace = "";

        }
        // If the parent token is non-null, the class must not be System.Object.
        if (bIsObject)
        {
            REPORT_ERROR1(VLDTR_E_TD_OBJEXTENDSNONNULL, tkExtends);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

    bExtendsObject = bExtendsEnum = bExtendsVType = bExtendsMCDelegate = FALSE;
    if(0 == strcmp(szExtNameSpace,BASE_NAMESPACE))
    {
        bExtendsObject = (0 == strcmp(szExtName,BASE_OBJECT_CLASSNAME));
        if(!bExtendsObject)
        {
            bExtendsEnum   = (0 == strcmp(szExtName,BASE_ENUM_CLASSNAME));
            if(!bExtendsEnum)
            {
                bExtendsVType  = (0 == strcmp(szExtName,BASE_VTYPE_CLASSNAME));
                if(!bExtendsVType)
                {
                    bExtendsMCDelegate  = (0 == strcmp(szExtName,BASE_MCDELEGATE_CLASSNAME));
                }
            }
        }
    }

    // System.ValueType must extend System.Object
    if(bIsVType && !bExtendsObject)
    {
        REPORT_ERROR0(VLDTR_E_TD_SYSVTNOTEXTOBJ);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // Validate rules for interface.  Some of the VOS rules are verified as
    // part of the validation for the corresponding Methods, fields etc.
    if (IsTdInterface(dwFlags))
    {
        // Interface type must be marked abstract.
        if (!IsTdAbstract(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_TD_IFACENOTABS);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }

        // Interface must not be sealed
        if(IsTdSealed(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_TD_IFACESEALED);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }

        // Interface must have parent Nil token.
        if (!IsNilToken(tkExtends))
        {
            REPORT_ERROR1(VLDTR_E_TD_IFACEPARNOTNIL, tkExtends);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }

        //Interface must have only static fields -- checked in ValidateField
        //Interface must have only public fields -- checked in ValidateField
        //Interface must have only abstract or static methods -- checked in ValidateMethod
        //Interface must have only public methods -- checked in ValidateMethod

        // Interface must have GUID
        /*
        if (*pGuid == GUID_NULL)
        {
            REPORT_ERROR0(VLDTR_E_TD_IFACEGUIDNULL);
            SetVldtrCode(&hrSave, VLDTR_S_WRN);
        }
        */
    }


    // Class must have valid method and field lists
    {
        ULONG           ridStart,ridEnd;
        ridStart = pMiniMd->getMethodListOfTypeDef(pRecord);
        ridEnd  = pMiniMd->getCountMethods() + 1;
        if(ridStart > ridEnd)
        {
            REPORT_ERROR0(VLDTR_E_TD_BADMETHODLST);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else 
        {
            IfFailGo(pMiniMd->getEndMethodListOfTypeDef(rid, &ridEnd));
            bHasMethods = (ridStart && (ridStart < ridEnd));
        }

        ridStart = pMiniMd->getFieldListOfTypeDef(pRecord);
        ridEnd  = pMiniMd->getCountFields() + 1;
        if(ridStart > ridEnd)
        {
            REPORT_ERROR0(VLDTR_E_TD_BADFIELDLST);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else 
        {
            IfFailGo(pMiniMd->getEndFieldListOfTypeDef(rid, &ridEnd));
            bHasFields = (ridStart && (ridStart < ridEnd));
        }
    }

    // Validate rules for System.Enum
    if(bIsEnum)
    {
        if(!IsTdClass(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_TD_SYSENUMNOTCLASS);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        if(!bExtendsVType)
        {
            REPORT_ERROR0(VLDTR_E_TD_SYSENUMNOTEXTVTYPE);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    else
    {
        if(bExtendsVType || bExtendsEnum)
        {
            // ValueTypes and Enums must be sealed
            if(!IsTdSealed(dwFlags))
            {
                REPORT_ERROR0(VLDTR_E_TD_VTNOTSEAL);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            // Value class must have fields or size
            if(!bHasFields)
            {
                ULONG ulClassSize = 0;
                ClassLayoutRec  *pRec;
                RID ridClassLayout;
                IfFailGo(pMiniMd->FindClassLayoutHelper(TokenFromRid(rid, mdtTypeDef), &ridClassLayout));

                if (!InvalidRid(ridClassLayout))
                {
                    IfFailGo(pMiniMd->GetClassLayoutRecord(RidFromToken(ridClassLayout), &pRec));
                    ulClassSize = pMiniMd->getClassSizeOfClassLayout(pRec);
                }
                if(ulClassSize == 0)
                {
                    REPORT_ERROR0(VLDTR_E_TD_VTNOSIZE);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
            }
        }
        else if(bExtendsMCDelegate)
        {
            // Delegates must be sealed
            if(!IsTdSealed(dwFlags))
            {
                REPORT_ERROR0(VLDTR_E_TD_VTNOTSEAL);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
        }
    }

    // Enum-related checks
    if (bExtendsEnum)
    {
        {
            PCCOR_SIGNATURE pValueSig = NULL;
            ULONG           cbValueSig = 0;
            mdFieldDef      tkValueField=0, tkField, tkValue__Field = 0;
            ULONG           ridStart,ridEnd,index;
            FieldRec        *pFieldRecord;               // Field record.
            DWORD           dwRecordFlags, dwTally, dwValueFlags, dwValue__Flags = 0;
            RID             ridField,ridValue=0,ridValue__ = 0;

            ridStart = pMiniMd->getFieldListOfTypeDef(pRecord);
            IfFailGo(pMiniMd->getEndFieldListOfTypeDef(rid, &ridEnd));
            // check the instance (value__) field(s)
            dwTally = 0;
            for (index = ridStart; index < ridEnd; index++ )
            {
                IfFailGo(pMiniMd->GetFieldRid(index, &ridField));
                IfFailGo(pMiniMd->GetFieldRecord(ridField, &pFieldRecord));
                dwRecordFlags = pFieldRecord->GetFlags();
                if(!IsFdStatic(dwRecordFlags))
                {
                    dwTally++;
                    if(ridValue == 0)
                    {
                        ridValue = ridField;
                        tkValueField = TokenFromRid(ridField, mdtFieldDef);
                        IfFailGo(pMiniMd->getSignatureOfField(pFieldRecord, &pValueSig, &cbValueSig));
                        dwValueFlags = dwRecordFlags;
                    }
                }
                LPCSTR szFieldName;
                IfFailGo(pMiniMd->getNameOfField(pFieldRecord, &szFieldName));
                if(!strcmp(szFieldName, BASE_VALUE_FIELDNAME))
                {
                    ridValue__ = ridField;
                    dwValue__Flags = dwRecordFlags;
                    tkValue__Field = TokenFromRid(ridField, mdtFieldDef);
                }
            }
            // Enum must have one (and only one) inst.field
            if(dwTally == 0)
            {
                REPORT_ERROR0(VLDTR_E_TD_ENUMNOINSTFLD);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            else if(dwTally > 1)
            {
                REPORT_ERROR0(VLDTR_E_TD_ENUMMULINSTFLD);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            
            // inst.field name must be "value__" (CLS)
            if(ridValue__ == 0)
            {
                REPORT_ERROR0(VLDTR_E_TD_ENUMNOVALUE);
                SetVldtrCode(&hrSave, VLDTR_S_WRN);
            }
            else
            {
                // if "value__" field is present ...
                // ... it must be 1st instance field
                if(ridValue__ != ridValue)
                {
                    REPORT_ERROR1(VLDTR_E_TD_ENUMVALNOT1ST, tkValue__Field);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                // ... it must not be static
                if(IsFdStatic(dwValue__Flags))
                {
                    REPORT_ERROR1(VLDTR_E_TD_ENUMVALSTATIC, tkValue__Field);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                // ... it must be fdRTSpecialName
                if(!IsFdRTSpecialName(dwValue__Flags))
                {
                    REPORT_ERROR1(VLDTR_E_TD_ENUMVALNOTSN, tkValueField);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                // ... its type must be integral
                if(cbValueSig && pValueSig)
                {
                    //ULONG ulCurByte = CorSigUncompressedDataSize(pValueSig);
                    //CorSigUncompressData(pValueSig);
                    //ULONG ulElemSize,ulElementType;
                    //ulCurByte += (ulElemSize = CorSigUncompressedDataSize(pValueSig));
                    //ulElementType = CorSigUncompressData(pValueSig);
                    //switch (ulElementType)
                    BYTE* pB = (BYTE*)pValueSig;
                    pB++; // skip the calling convention
                    while((*pB == ELEMENT_TYPE_CMOD_OPT)||
                          (*pB == ELEMENT_TYPE_CMOD_REQD))
                    {
                        mdToken tok;
                        pB++; // move from E_T_... to compressed token
                        pB += CorSigUncompressToken((PCOR_SIGNATURE)pB,&tok);
                    }
                    switch(*pB)
                    {
                        case ELEMENT_TYPE_BOOLEAN:
                        case ELEMENT_TYPE_CHAR:
                        case ELEMENT_TYPE_I1:
                        case ELEMENT_TYPE_U1:
                        case ELEMENT_TYPE_I2:
                        case ELEMENT_TYPE_U2:
                        case ELEMENT_TYPE_I4:
                        case ELEMENT_TYPE_U4:
                        case ELEMENT_TYPE_I8:
                        case ELEMENT_TYPE_U8:
                        case ELEMENT_TYPE_U:
                        case ELEMENT_TYPE_I:
                        case ELEMENT_TYPE_R4:
                        case ELEMENT_TYPE_R8:
                            break;
                        default:
                            REPORT_ERROR1(VLDTR_E_TD_ENUMFLDBADTYPE, tkValue__Field);
                            SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    }
                }
            }
            // check all the fields
            dwTally = 0;
            for (index = ridStart; index < ridEnd; index++ )
            {
                IfFailGo(pMiniMd->GetFieldRid(index, &ridField));
                if(ridField == ridValue) continue; 
                IfFailGo(pMiniMd->GetFieldRecord(ridField, &pFieldRecord));
                LPCSTR szFieldName;
                IfFailGo(pMiniMd->getNameOfField(pFieldRecord, &szFieldName));
                if(IsFdRTSpecialName(pFieldRecord->GetFlags()) 
                    && IsDeletedName(szFieldName)) continue;
                dwTally++;
                tkField = TokenFromRid(ridField, mdtFieldDef);
                if(!IsFdStatic(pFieldRecord->GetFlags()))
                {
                    REPORT_ERROR1(VLDTR_E_TD_ENUMFLDNOTST, tkField);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                if(!IsFdLiteral(pFieldRecord->GetFlags()))
                {
                    REPORT_ERROR1(VLDTR_E_TD_ENUMFLDNOTLIT, tkField);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                /*
                IfFailGo(pMiniMd->getSignatureOfField(pFieldRecord, &pvSigTmp, &cbSig));
                if(!(pvSigTmp && (cbSig==cbValueSig) &&(memcmp(pvSigTmp,pValueSig,cbSig)==0)))
                {
                    REPORT_ERROR1(VLDTR_E_TD_ENUMFLDSIGMISMATCH, tkField);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                */
            }
            if(dwTally == 0)
            {
                REPORT_ERROR0(VLDTR_E_TD_ENUMNOLITFLDS);
                SetVldtrCode(&hrSave, VLDTR_S_WRN);
            }
        }
        // Enum must have no methods
        if (bHasMethods)
        {
            REPORT_ERROR0(VLDTR_E_TD_ENUMHASMETHODS);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        // Enum must implement no interfaces
        {
            ULONG ridStart = 1;
            ULONG ridEnd = pMiniMd->getCountInterfaceImpls() + 1;
            ULONG index;
            for (index = ridStart; index < ridEnd; index ++ )
            {
                InterfaceImplRec *pInterfaceImplRecord;
                IfFailGo(pMiniMd->GetInterfaceImplRecord(index, &pInterfaceImplRecord));
                if (veCtxt.Token == pMiniMd->getClassOfInterfaceImpl(pInterfaceImplRecord))
                {
                    REPORT_ERROR0(VLDTR_E_TD_ENUMIMPLIFACE);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    break;
                }
            }
        }
        // Enum must have no properties
        {
            ULONG ridStart = 1;
            ULONG ridEnd = pMiniMd->getCountPropertys() + 1;
            ULONG index;
            mdToken tkClass;
            for (index = ridStart; index < ridEnd; index ++ )
            {
                IfFailGo(pMiniMd->FindParentOfPropertyHelper(index | mdtProperty, &tkClass));
                if (veCtxt.Token == tkClass)
                {
                    REPORT_ERROR0(VLDTR_E_TD_ENUMHASPROP);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    break;
                }
            }
        }
        // Enum must have no events
        {
            ULONG ridStart = 1;
            ULONG ridEnd = pMiniMd->getCountEvents() + 1;
            ULONG index;
            mdToken tkClass;
            for (index = ridStart; index < ridEnd; index ++ )
            {
                IfFailGo(pMiniMd->FindParentOfEventHelper(index | mdtEvent, &tkClass));
                if (veCtxt.Token == tkClass)
                {
                    REPORT_ERROR0(VLDTR_E_TD_ENUMHASEVENT);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    break;
                }
            }
        }
    } // end if(bExtendsEnum)
    // Class having security must be marked tdHasSecurity and vice versa
    {
        ULONG ridStart = 1;
        ULONG ridEnd = pMiniMd->getCountDeclSecuritys() + 1;
        ULONG index;
        BOOL  bHasSecurity = FALSE;
        for (index = ridStart; index < ridEnd; index ++ )
        {
            DeclSecurityRec *pDeclSecurityRecord;
            IfFailGo(pMiniMd->GetDeclSecurityRecord(index, &pDeclSecurityRecord));
            if (veCtxt.Token == pMiniMd->getParentOfDeclSecurity(pDeclSecurityRecord))
            {
                bHasSecurity = TRUE;
                break;
            }
        }
        if (!bHasSecurity) // No records, check for CA "SuppressUnmanagedCodeSecurityAttribute"
        {
            bHasSecurity = (S_OK == ImportHelper::GetCustomAttributeByName(pMiniMd, veCtxt.Token, 
                "System.Security.SuppressUnmanagedCodeSecurityAttribute", NULL, NULL));
        }
        if(bHasSecurity != (IsTdHasSecurity(pRecord->GetFlags())!=0))
        {
            REPORT_ERROR0(bHasSecurity ? VLDTR_E_TD_SECURNOTMARKED : VLDTR_E_TD_MARKEDNOSECUR);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateTypeDef()
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//*****************************************************************************
// Validate the given FieldPtr.
//*****************************************************************************
HRESULT RegMeta::ValidateFieldPtr(RID rid)
{
    return S_OK;
}   // RegMeta::ValidateFieldPtr()


//*****************************************************************************
// Validate the given Field.
//*****************************************************************************
HRESULT RegMeta::ValidateField(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    FieldRec    *pRecord;               // Field record.
    mdTypeDef   tkTypeDef;              // Parent TypeDef token.
    mdFieldDef  tkFieldDef;             // Duplicate FieldDef token.
    LPCSTR      szName;                 // FieldDef name.
    PCCOR_SIGNATURE pbSig;              // FieldDef signature.
    ULONG       cbSig;                  // Signature size in bytes.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.
    BOOL        bIsValueField;
    BOOL        bIsGlobalField = FALSE;
    BOOL        bHasValidRVA = FALSE;
    DWORD       dwInvalidFlags;
    DWORD       dwFlags;
    RID         tempRid;

    BEGIN_ENTRYPOINT_NOTHROW;
    
    memset(&veCtxt, 0, sizeof(VEContext));

    // Get the FieldDef record.
    veCtxt.Token = TokenFromRid(rid, mdtFieldDef);
    veCtxt.uOffset = 0;

    IfFailGo(pMiniMd->GetFieldRecord(rid, &pRecord));

    // Do checks for name validity.
    IfFailGo(pMiniMd->getNameOfField(pRecord, &szName));
    if (!*szName)
    {
        // Field name is NULL.
        REPORT_ERROR0(VLDTR_E_FD_NAMENULL);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else
    {
        if(!strcmp(szName,COR_DELETED_NAME_A)) goto ErrExit; 
        ULONG L = (ULONG)strlen(szName);
        if(L >= MAX_CLASSNAME_LENGTH)
        {
            REPORT_ERROR2(VLDTR_E_TD_NAMETOOLONG, L, (ULONG)(MAX_CLASSNAME_LENGTH-1));
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    bIsValueField = (strcmp(szName,BASE_VALUE_FIELDNAME)==0);
    // If field is RTSpecialName, its name must be 'value__' and vice versa
    if((IsFdRTSpecialName(pRecord->GetFlags())!=0) != bIsValueField)
    {
        REPORT_ERROR1(bIsValueField ? VLDTR_E_TD_ENUMVALNOTSN : VLDTR_E_FD_NOTVALUERTSN, veCtxt.Token);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Validate flags
    dwFlags = pRecord->GetFlags();
    dwInvalidFlags = ~(fdFieldAccessMask | fdStatic | fdInitOnly | fdLiteral | fdNotSerialized | fdSpecialName
        | fdPinvokeImpl | fdReservedMask);
    if(dwFlags & dwInvalidFlags)
    {
        REPORT_ERROR1(VLDTR_E_TD_EXTRAFLAGS, dwFlags & dwInvalidFlags);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Validate access
    if((dwFlags & fdFieldAccessMask) == fdFieldAccessMask)
    {
        REPORT_ERROR0(VLDTR_E_FMD_BADACCESSFLAG);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // Literal : Static, !InitOnly
    if(IsFdLiteral(dwFlags))
    {
        if(IsFdInitOnly(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_FD_INITONLYANDLITERAL);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        if(!IsFdStatic(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_FD_LITERALNOTSTATIC);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        if(!IsFdHasDefault(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_FD_LITERALNODEFAULT);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    // RTSpecialName => SpecialName
    if(IsFdRTSpecialName(dwFlags) && !IsFdSpecialName(dwFlags))
    {
        REPORT_ERROR0(VLDTR_E_FMD_RTSNNOTSN);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Validate Field signature.
    IfFailGo(pMiniMd->getSignatureOfField(pRecord, &pbSig, &cbSig));
    IfFailGo(ValidateFieldSig(TokenFromRid(rid, mdtFieldDef), pbSig, cbSig));
    if (hr != S_OK)
        SetVldtrCode(&hrSave, hr);

    // Validate Field RVA
    if(IsFdHasFieldRVA(dwFlags))
    {
        ULONG iFieldRVARid;
        IfFailGo(pMiniMd->FindFieldRVAHelper(TokenFromRid(rid, mdtFieldDef), &iFieldRVARid));
        if((iFieldRVARid==0) || (iFieldRVARid > pMiniMd->getCountFieldRVAs()))
        {
            REPORT_ERROR0(VLDTR_E_FD_RVAHASNORVA);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else
        {
            /*
            FieldRVARec *pRVARec;
            IfFailGo(pMiniMd->GetFieldRVARecord(iFieldRVARid, &pRVARec));
            if(pRVARec->GetRVA() == 0)
            {
                REPORT_ERROR0(VLDTR_E_FD_RVAHASZERORVA);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            else
            */ 
                bHasValidRVA = TRUE;
        }
    }

    // Get the parent of the Field.
    IfFailGo(pMiniMd->FindParentOfFieldHelper(TokenFromRid(rid, mdtFieldDef), &tkTypeDef));
    // Validate that the parent is not nil.
    if (IsNilToken(tkTypeDef))
    {
        REPORT_ERROR0(VLDTR_E_FD_PARNIL);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else if (RidFromToken(tkTypeDef) != RidFromToken(m_tdModule))
    {
        if(IsValidToken(tkTypeDef) && (TypeFromToken(tkTypeDef) == mdtTypeDef))
        {
            TypeDefRec *pParentRec;
            IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkTypeDef), &pParentRec));
            // If the name is "value__" ...
            if(bIsValueField)
            {
                // parent must be Enum
                mdToken tkExtends = pMiniMd->getExtendsOfTypeDef(pParentRec);
                RID     ridExtends = RidFromToken(tkExtends);
                LPCSTR  szExtName="",szExtNameSpace="";
                if(ridExtends)
                {
                    if(TypeFromToken(tkExtends) == mdtTypeRef)
                    {
                        TypeRefRec *pExtRec;
                        IfFailGo(pMiniMd->GetTypeRefRecord(ridExtends, &pExtRec));
                        IfFailGo(pMiniMd->getNameOfTypeRef(pExtRec, &szExtName));
                        IfFailGo(pMiniMd->getNamespaceOfTypeRef(pExtRec, &szExtNameSpace));
                    }
                    else if(TypeFromToken(tkExtends) == mdtTypeDef)
                    {
                        TypeDefRec *pExtRec;
                        IfFailGo(pMiniMd->GetTypeDefRecord(ridExtends, &pExtRec));
                        IfFailGo(pMiniMd->getNameOfTypeDef(pExtRec, &szExtName));
                        IfFailGo(pMiniMd->getNamespaceOfTypeDef(pExtRec, &szExtNameSpace));
                    }
                }
                if(strcmp(szExtName,BASE_ENUM_CLASSNAME) || strcmp(szExtNameSpace,BASE_NAMESPACE))
                {
                    REPORT_ERROR0(VLDTR_E_FD_VALUEPARNOTENUM);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }

                // field must be instance - checked in ValidateTypeDef
                // must be no other instance fields - checked in ValidateTypeDef
                // must be first field - checked in ValidateTypeDef
                // must be RTSpecialName -- checked in ValidateTypeDef
            }
            if(IsTdInterface(pMiniMd->getFlagsOfTypeDef(pParentRec)))
            {
                // Fields in interface are not CLS compliant
                REPORT_ERROR0(VLDTR_E_FD_FLDINIFACE);
                SetVldtrCode(&hrSave, VLDTR_S_WRN);

                // If field is not static, verify parent is not interface.
                if(!IsFdStatic(dwFlags))
                {
                    REPORT_ERROR0(VLDTR_E_FD_INSTINIFACE);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    // If field is not public, verify parent is not interface.
                    if(!IsFdPublic(dwFlags))
                    {
                        REPORT_ERROR0(VLDTR_E_FD_NOTPUBINIFACE);
                        SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    }
                }
            }
        } // end if Valid and TypeDef
        else
        {
            REPORT_ERROR1(VLDTR_E_FD_BADPARENT, tkTypeDef);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    else // i.e. if (RidFromToken(tkTypeDef) == RidFromToken(m_tdModule))
    {
        bIsGlobalField = TRUE;
        // Globals are not CLS-compliant
        REPORT_ERROR0(VLDTR_E_FMD_GLOBALITEM);
        SetVldtrCode(&hrSave, VLDTR_S_WRN);
        // Validate global field:
        // Must be static
        if(!IsFdStatic(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_FMD_GLOBALNOTSTATIC);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        // Must have a non-zero RVA
        /*
        if(!bHasValidRVA)
        {
            REPORT_ERROR0(VLDTR_E_FD_GLOBALNORVA);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        */
    }

    // Check for duplicates, except global fields with PrivateScope.
    if (*szName && cbSig && !IsFdPrivateScope(dwFlags))
    {
        hr = ImportHelper::FindField(pMiniMd, tkTypeDef, szName, pbSig, cbSig, &tkFieldDef, rid);
        if (hr == S_OK)
        {
            if(!IsFdPrivateScope(dwFlags))
            {
                REPORT_ERROR1(VLDTR_E_FD_DUP, tkFieldDef);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            else hr = S_OK;
        }
        else if (hr == CLDB_E_RECORD_NOTFOUND)
        {
            hr = S_OK;
        }
        else
        {
            IfFailGo(hr);
        }
    }
    // Field having security must be marked fdHasSecurity and vice versa
    {
        ULONG ridStart = 1;
        ULONG ridEnd = pMiniMd->getCountDeclSecuritys() + 1;
        ULONG index;
        BOOL  bHasSecurity = FALSE;
        for (index = ridStart; index < ridEnd; index ++ )
        {
            DeclSecurityRec *pDeclSecurityRecord;
            IfFailGo(pMiniMd->GetDeclSecurityRecord(index, &pDeclSecurityRecord));
            if ( veCtxt.Token == pMiniMd->getParentOfDeclSecurity(pDeclSecurityRecord))
            {
                bHasSecurity = TRUE;
                break;
            }
        }
        if(!bHasSecurity) // No records, check for CA "SuppressUnmanagedCodeSecurityAttribute"
        {
            bHasSecurity = (S_OK == ImportHelper::GetCustomAttributeByName(pMiniMd, veCtxt.Token, 
                "System.Security.SuppressUnmanagedCodeSecurityAttribute", NULL, NULL));
        }
        if(bHasSecurity)
        {
            REPORT_ERROR0(VLDTR_E_FMD_SECURNOTMARKED);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    // Field having marshaling must be marked fdHasFieldMarshal and vice versa
    IfFailGo(pMiniMd->FindFieldMarshalHelper(veCtxt.Token, &tempRid));
    if (InvalidRid(tempRid) == (IsFdHasFieldMarshal(dwFlags) != 0))
    {
        REPORT_ERROR0(IsFdHasFieldMarshal(dwFlags)? VLDTR_E_FD_MARKEDNOMARSHAL : VLDTR_E_FD_MARSHALNOTMARKED);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // Field having const value must be marked fdHasDefault and vice versa
    IfFailGo(pMiniMd->FindConstantHelper(veCtxt.Token, &tempRid));
    if(InvalidRid(tempRid) == (IsFdHasDefault(dwFlags) != 0))
    {
        REPORT_ERROR0(IsFdHasDefault(dwFlags)? VLDTR_E_FD_MARKEDNODEFLT : VLDTR_E_FD_DEFLTNOTMARKED);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // Check the field's impl.map
    {
        ULONG iRecord;
        IfFailGo(pMiniMd->FindImplMapHelper(veCtxt.Token, &iRecord));
        if(IsFdPinvokeImpl(dwFlags))
        {
            // must be static
            if(!IsFdStatic(dwFlags))
            {
                REPORT_ERROR0(VLDTR_E_FMD_PINVOKENOTSTATIC);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            // must have ImplMap
            if (InvalidRid(iRecord))
            {
                REPORT_ERROR0(VLDTR_E_FMD_MARKEDNOPINVOKE);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
        }
        else
        {
            // must have no ImplMap
            if (!InvalidRid(iRecord))
            {
                REPORT_ERROR0(VLDTR_E_FMD_PINVOKENOTMARKED);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
        }
        if (!InvalidRid(iRecord))
        {
            hr = ValidateImplMap(iRecord);
            if(hr != S_OK)
            {
                REPORT_ERROR0(VLDTR_E_FMD_BADIMPLMAP);
                SetVldtrCode(&hrSave, VLDTR_S_WRN);
            }
        }

    }

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateField()

//*****************************************************************************
// Validate the given MethodPtr.
//*****************************************************************************
HRESULT RegMeta::ValidateMethodPtr(RID rid)
{
    return S_OK;
}   // RegMeta::ValidateMethodPtr()


//*****************************************************************************
// Validate the given Method.
//*****************************************************************************
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
HRESULT RegMeta::ValidateMethod(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    MethodRec   *pRecord = NULL;               // Method record.
    mdTypeDef   tkTypeDef;              // Parent TypeDef token.
    mdMethodDef tkMethodDef;            // Duplicate MethodDef token.
    LPCSTR      szName;                 // MethodDef name.
    DWORD       dwFlags = 0;                // Method flags.
    DWORD       dwImplFlags = 0;            // Method impl.flags.
    PCCOR_SIGNATURE pbSig;              // MethodDef signature.
    ULONG       cbSig;                  // Signature size in bytes.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.
    BOOL        bIsCtor=FALSE;
    BOOL        bIsCctor=FALSE;
    BOOL        bIsGlobal=FALSE;
    BOOL        bIsParentImport = FALSE;
    BOOL        bIsGeneric = FALSE;
    unsigned    retType;

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));

    // Get the MethodDef record.
    veCtxt.Token = TokenFromRid(rid, mdtMethodDef);
    veCtxt.uOffset = 0;

    IfFailGo(pMiniMd->GetMethodRecord(rid, &pRecord));

    // Do checks for name validity.
    IfFailGo(pMiniMd->getNameOfMethod(pRecord, &szName));
    if (!*szName)
    {
        // Method name is NULL.
        REPORT_ERROR0(VLDTR_E_MD_NAMENULL);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else
    {
        if(!strcmp(szName,COR_DELETED_NAME_A)) goto ErrExit; 
        bIsCtor = (0 == strcmp(szName,BASE_CTOR_NAME));
        bIsCctor = (0 == strcmp(szName,BASE_CCTOR_NAME));
        ULONG L = (ULONG)strlen(szName);
        if(L >= MAX_CLASSNAME_LENGTH)
        {
            REPORT_ERROR2(VLDTR_E_TD_NAMETOOLONG, L, (ULONG)(MAX_CLASSNAME_LENGTH-1));
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

    // Get the parent, flags and signature of the Method.
    IfFailGo(pMiniMd->FindParentOfMethodHelper(TokenFromRid(rid, mdtMethodDef), &tkTypeDef));
    dwFlags = pMiniMd->getFlagsOfMethod(pRecord);
    dwImplFlags = pMiniMd->getImplFlagsOfMethod(pRecord);
    IfFailGo(pMiniMd->getSignatureOfMethod(pRecord, &pbSig, &cbSig));

    // Check for duplicates.
    if (*szName && cbSig && !IsNilToken(tkTypeDef) && !IsMdPrivateScope(dwFlags))
    {
        hr = ImportHelper::FindMethod(pMiniMd, tkTypeDef, szName, pbSig, cbSig, &tkMethodDef, rid);
        if (hr == S_OK)
        {
            REPORT_ERROR1(VLDTR_E_MD_DUP, tkMethodDef);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else if (hr == CLDB_E_RECORD_NOTFOUND)
            hr = S_OK;
        else
            IfFailGo(hr);
    }

    // No further error checking for VtblGap methods.
    if (IsVtblGapName(szName))
    {
        hr = hrSave;
        goto ErrExit;
    }

    // Validate Method signature.
    IfFailGo(ValidateMethodSig(TokenFromRid(rid, mdtMethodDef), pbSig, cbSig,
                               dwFlags));
    if (hr != S_OK)
        SetVldtrCode(&hrSave, hr);

    // Validate that the parent is not nil.
    if (IsNilToken(tkTypeDef))
    {
        REPORT_ERROR0(VLDTR_E_MD_PARNIL);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else if (RidFromToken(tkTypeDef) != RidFromToken(m_tdModule))
    {
        if(TypeFromToken(tkTypeDef) == mdtTypeDef)
        {
            TypeDefRec *pTDRec;
            IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkTypeDef), &pTDRec));
            DWORD       dwTDFlags = pTDRec->GetFlags();
            LPCSTR szTDName;
            IfFailGo(pMiniMd->getNameOfTypeDef(pTDRec, &szTDName));
            LPCSTR      szTDNameSpace;
            IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTDRec, &szTDNameSpace));
            BOOL        fIsTdValue=FALSE, fIsTdEnum=FALSE;
            mdToken     tkExtends = pMiniMd->getExtendsOfTypeDef(pTDRec);

            if(0 == strcmp(szTDNameSpace,BASE_NAMESPACE))
            {
                fIsTdEnum   = (0 == strcmp(szTDName,BASE_ENUM_CLASSNAME));
                if(!fIsTdEnum)
                {
                    fIsTdValue  = (0 == strcmp(szTDName,BASE_VTYPE_CLASSNAME));
                }
            }
            if(fIsTdEnum || fIsTdValue)
            {
                fIsTdEnum = fIsTdValue = FALSE; // System.Enum and System.ValueType themselves are classes
            }
            else if(RidFromToken(tkExtends))
            { 
                if(TypeFromToken(tkExtends) == mdtTypeDef)
                {
                    IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkExtends), &pTDRec));
                    IfFailGo(pMiniMd->getNameOfTypeDef(pTDRec, &szTDName));
                    IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTDRec, &szTDNameSpace));
                }
                else if(TypeFromToken(tkExtends) == mdtTypeSpec)
                {
                    fIsTdEnum = fIsTdValue = FALSE; // a type extending a spec cannot be an enum or value type 
                    // the assignments are redundant, but clear.
                }
                else 
                {
                    TypeRefRec *pTRRec;
                    IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(tkExtends), &pTRRec));
                    IfFailGo(pMiniMd->getNameOfTypeRef(pTRRec, &szTDName));
                    IfFailGo(pMiniMd->getNamespaceOfTypeRef(pTRRec, &szTDNameSpace));
                }

                if(0 == strcmp(szTDNameSpace,BASE_NAMESPACE))
                {
                    fIsTdEnum   = (0 == strcmp(szTDName,BASE_ENUM_CLASSNAME));
                    if(!fIsTdEnum)
                    {
                        fIsTdValue  = (0 == strcmp(szTDName,BASE_VTYPE_CLASSNAME));
                    }
                    else fIsTdValue = FALSE;
                }
            }

            // If Method is abstract, verify parent is abstract.
            if(IsMdAbstract(dwFlags) && !IsTdAbstract(dwTDFlags))
            {
                REPORT_ERROR1(VLDTR_E_MD_ABSTPARNOTABST, tkTypeDef);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            // If parent is import, method must have zero RVA, otherwise it depends...
            if(IsTdImport(dwTDFlags)) bIsParentImport = TRUE;
            if(IsTdInterface(dwTDFlags))
            {
                if(!IsMdStatic(dwFlags))
                { 
                    // No non-abstract instance methods in interface.
                    if(!IsMdAbstract(dwFlags))
                    {
                        REPORT_ERROR1(VLDTR_E_MD_NOTSTATABSTININTF, tkTypeDef);
                        SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    }
                    // No non-public instance methods in interface.
                    if(!IsMdPublic(dwFlags))
                    {
                        REPORT_ERROR1(VLDTR_E_MD_NOTPUBININTF, tkTypeDef);
                        SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    }
                }
                // If Method is constructor, verify parent is not interface.
                if(bIsCtor)
                {
                    REPORT_ERROR1(VLDTR_E_MD_CTORININTF, tkTypeDef);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
            }//end if(interface)
            if((fIsTdValue || fIsTdEnum) && IsMiSynchronized(dwImplFlags))
            {
                REPORT_ERROR1(VLDTR_E_MD_SYNCMETHODINVTYPE, tkTypeDef);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            if(bIsCtor)
            {
                // .ctor must be instance
                if(IsMdStatic(dwFlags))
                {
                    REPORT_ERROR1(VLDTR_E_MD_CTORSTATIC, tkTypeDef);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
            }//end if .ctor
            else if(bIsCctor)
            {
                // .cctor must be static
                if(!IsMdStatic(dwFlags))
                {
                    REPORT_ERROR1(VLDTR_E_MD_CCTORNOTSTATIC, tkTypeDef);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                // ..cctor must have default callconv
                IfFailGo(pMiniMd->getSignatureOfMethod(pRecord, &pbSig, &cbSig));
                if(IMAGE_CEE_CS_CALLCONV_DEFAULT != CorSigUncompressData(pbSig))
                {
                    REPORT_ERROR0(VLDTR_E_MD_CCTORCALLCONV);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                // .cctor must have no arguments
                if(0 != CorSigUncompressData(pbSig))
                {
                    REPORT_ERROR0(VLDTR_E_MD_CCTORHASARGS);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }


            }//end if .cctor
            if(bIsCtor || bIsCctor)
            {
                // .ctor, .cctor must be SpecialName and RTSpecialName
                if(!(IsMdSpecialName(dwFlags) && IsMdRTSpecialName(dwFlags)))
                {
                    REPORT_ERROR1(VLDTR_E_MD_CTORNOTSNRTSN, tkTypeDef);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
#ifdef NO_SUCH_CHECKS_NEEDED_SPEC_TO_BE_UODATED
                // .ctor, .cctor must not be virtual
                if(IsMdVirtual(dwFlags))
                {
                    REPORT_ERROR1(VLDTR_E_MD_CTORVIRT, tkTypeDef);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                // .ctor, .cctor must not be abstract
                if(IsMdAbstract(dwFlags))
                {
                    REPORT_ERROR1(VLDTR_E_MD_CTORABST, tkTypeDef);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                // .ctor, .cctor must not be PInvoke
                if(IsMdPinvokeImpl(dwFlags))
                {
                    REPORT_ERROR1(VLDTR_E_MD_CTORPINVOKE, tkTypeDef);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                // .ctor,.cctor must have RVA!=0
                if(pRecord->GetRVA()==0)
                { 
                    REPORT_ERROR0(VLDTR_E_MD_CTORZERORVA);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
#endif
            }//end if .ctor or .cctor
        }// end if(parent == TypeDef)
    }// end if not Module
    else // i.e. if (RidFromToken(tkTypeDef) == RidFromToken(m_tdModule))
    {
        bIsGlobal = TRUE;
        // Globals are not CLS-compliant
        REPORT_ERROR0(VLDTR_E_FMD_GLOBALITEM);
        SetVldtrCode(&hrSave, VLDTR_S_WRN);
        // Validate global method:
        // Must be static
        if(!IsMdStatic(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_FMD_GLOBALNOTSTATIC);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        // Must not be abstract or virtual
        if(IsMdAbstract(dwFlags) || IsMdVirtual(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_MD_GLOBALABSTORVIRT);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        // Must be not .ctor or .cctor
        if(bIsCtor)
        {
            REPORT_ERROR0(VLDTR_E_MD_GLOBALCTORCCTOR);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    } //end if Module

    // Signature specifics: .ctor, .cctor, entrypoint
    if(bIsCtor || bIsCctor)
    {
        // .ctor, .cctor must return void
        IfFailGo(pMiniMd->getSignatureOfMethod(pRecord, &pbSig, &cbSig));
        CorSigUncompressData(pbSig); // get call conv out of the way
        CorSigUncompressData(pbSig); // get num args out of the way
        while (((retType=CorSigUncompressData(pbSig)) == ELEMENT_TYPE_CMOD_OPT) 
            || (retType == ELEMENT_TYPE_CMOD_REQD)) CorSigUncompressToken(pbSig);
        if(retType != ELEMENT_TYPE_VOID)
        {
            REPORT_ERROR0(VLDTR_E_MD_CTORNOTVOID);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    if(g_tkEntryPoint == veCtxt.Token)
    {
        ULONG ulCallConv;
        // EP must be static
        if(!IsMdStatic(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_EP_INSTANCE);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        
        // EP can't belong to generic class or nested in generic class
        mdToken tkTypeDefCur;
        for(tkTypeDefCur = tkTypeDef; tkTypeDefCur != mdTokenNil;)
        {
            HENUMInternal hEnumTyPars;
            ULONG ulTypeDefArity = 0;
            hr = pMiniMd->FindGenericParamHelper(tkTypeDefCur, &hEnumTyPars);
            if (SUCCEEDED(hr))
            {
                IfFailGo(HENUMInternal::GetCount(&hEnumTyPars,&ulTypeDefArity));
                HENUMInternal::ClearEnum(&hEnumTyPars);
                if (ulTypeDefArity != 0)
                {
                    REPORT_ERROR0(VLDTR_E_EP_GENERIC_TYPE);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
            }
            if(ulTypeDefArity == 0)
            {
                // This class is not generic, how about the encloser?
                RID iRecord;
                IfFailGo(pMiniMd->FindNestedClassHelper(tkTypeDefCur, &iRecord));
                
                if (InvalidRid(iRecord))
                {
                    tkTypeDefCur = mdTokenNil;
                }
                else
                {
                    NestedClassRec *pNestedClassRec;
                    IfFailGo(pMiniMd->GetNestedClassRecord(iRecord, &pNestedClassRec));
                    tkTypeDefCur = pMiniMd->getEnclosingClassOfNestedClass(pNestedClassRec);
                }
            }
            else
                tkTypeDefCur = mdTokenNil;
        }

        // EP must have a predetermined signature (different for DLL and EXE
        IfFailGo(pMiniMd->getSignatureOfMethod(pRecord, &pbSig, &cbSig));
        ulCallConv = CorSigUncompressData(pbSig); // get call conv out of the way
        // EP can't be generic
        if (ulCallConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
            // Skip the arity
            CorSigUncompressData(pbSig);
            REPORT_ERROR0(VLDTR_E_EP_GENERIC_METHOD);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }

        // EP must have 0 or 1 argument
        unsigned nArgs = CorSigUncompressData(pbSig);
        if(g_fIsDLL)
        {
            if(nArgs != 3)
            {
                REPORT_ERROR1(VLDTR_E_EP_TOOMANYARGS, 3);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            //EP must return I4
            while (((retType=CorSigUncompressData(pbSig)) == ELEMENT_TYPE_CMOD_OPT) 
                || (retType == ELEMENT_TYPE_CMOD_REQD)) CorSigUncompressToken(pbSig);
    
            if(retType != ELEMENT_TYPE_I4)
            {
                REPORT_ERROR0(VLDTR_E_EP_BADRET);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            // Arguments must be VOID*, U4, VOID*
            if(nArgs)
            {
                unsigned jj;
                bool    badarg;
                for(jj=0; jj<nArgs;jj++)
                {
                    while (((retType=CorSigUncompressData(pbSig)) == ELEMENT_TYPE_CMOD_OPT) 
                        || (retType == ELEMENT_TYPE_CMOD_REQD)) CorSigUncompressToken(pbSig);
        
                    switch(jj)
                    {
                        case 0:
                        case 2:
                            badarg = (retType != ELEMENT_TYPE_PTR)
                                    ||(CorSigUncompressData(pbSig) != ELEMENT_TYPE_VOID);
                            break;
    
                        case 1:
                            badarg = (retType != ELEMENT_TYPE_U4);
                            break;
    
                        default:
                            badarg = true;
                    }
                    if(badarg)
                    {
                        REPORT_ERROR1(VLDTR_E_EP_BADARG, jj+1);
                        SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    }
                }
            }
        }
        else
        {
            if(nArgs > 1)
            {
                REPORT_ERROR1(VLDTR_E_EP_TOOMANYARGS, 1);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            //EP must return VOID, I4 or U4
            while (((retType=CorSigUncompressData(pbSig)) == ELEMENT_TYPE_CMOD_OPT) 
                || (retType == ELEMENT_TYPE_CMOD_REQD)) CorSigUncompressToken(pbSig);
    
            if((retType != ELEMENT_TYPE_VOID)&&(retType != ELEMENT_TYPE_I4)&&(retType != ELEMENT_TYPE_U4))
            {
                REPORT_ERROR0(VLDTR_E_EP_BADRET);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            // Argument (if any) must be vector of strings
            if(nArgs)
            {
                while (((retType=CorSigUncompressData(pbSig)) == ELEMENT_TYPE_CMOD_OPT) 
                    || (retType == ELEMENT_TYPE_CMOD_REQD)) CorSigUncompressToken(pbSig);
    
                if((retType != ELEMENT_TYPE_SZARRAY)||(CorSigUncompressData(pbSig) != ELEMENT_TYPE_STRING))
                {
                    REPORT_ERROR1(VLDTR_E_EP_BADARG, 1);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
            }
        } // end if(IsDll)--else
    }  // end if (IsEntryPoint)


    // Check method RVA
    if(pRecord->GetRVA()==0)
    { 
        if(!(IsMdPinvokeImpl(dwFlags) || IsMdAbstract(dwFlags) 
            || IsMiRuntime(dwImplFlags) || IsMiInternalCall(dwImplFlags)
            || bIsParentImport))
        {
            REPORT_ERROR0(VLDTR_E_MD_ZERORVA);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    else
    {
        if(m_pStgdb && m_pStgdb->m_pImage)
        {
            NewHolder<PEDecoder> pe;

            EX_TRY
            {
                // We need to use different PEDecoder constructors based on the type of data we give it.
                // We use the one with a 'bool' as the second argument when dealing with a mapped file,
                // and we use the one that takes a COUNT_T as the second argument when dealing with a
                // flat file.
            
                if (m_pStgdb->m_pStgIO->GetMemoryMappedType() == MTYPE_IMAGE)
                    pe = new (nothrow) PEDecoder(m_pStgdb->m_pImage, false);
                else
                    pe = new (nothrow) PEDecoder(m_pStgdb->m_pImage, (COUNT_T)(m_pStgdb->m_dwImageSize));

            }
            EX_CATCH
            {
                hr = COR_E_BADIMAGEFORMAT;
            }
            EX_END_CATCH(SwallowAllExceptions)

            IfFailGo(hr);
            IfNullGo(pe);

            if (!pe->CheckRva(pRecord->GetRVA()))
            {
                REPORT_ERROR1(VLDTR_E_MD_BADRVA, pRecord->GetRVA());
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            else
            {
                if(IsMiManaged(dwImplFlags) && (IsMiIL(dwImplFlags) || IsMiOPTIL(dwImplFlags)))
                {
                    HRESULT hrTemp = S_OK;
                    // validate locals signature token
                    EX_TRY
                    {
                        COR_ILMETHOD_DECODER method((COR_ILMETHOD*) pe->GetRvaData(pRecord->GetRVA()));
                        if (method.LocalVarSigTok)
                        {
                            if((TypeFromToken(method.GetLocalVarSigTok()) != mdtSignature) ||
                                (!IsValidToken(method.GetLocalVarSigTok())) || (RidFromToken(method.GetLocalVarSigTok())==0))
                            {
                                hrTemp = _ValidateErrorHelper(VLDTR_E_MD_BADLOCALSIGTOK, veCtxt, method.GetLocalVarSigTok());
                                if (SUCCEEDED(hrTemp))
                                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                            }
                        }
                    } 
                    EX_CATCH
                    {
                        hrTemp = _ValidateErrorHelper(VLDTR_E_MD_BADHEADER, veCtxt);
                        if (SUCCEEDED(hrTemp))
                            SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    }
                    EX_END_CATCH(SwallowAllExceptions)

                    IfFailGo(hrTemp);
                }
            }
        }

        if(IsMdAbstract(dwFlags) || bIsParentImport
            || IsMiRuntime(dwImplFlags) || IsMiInternalCall(dwImplFlags))
        {
            REPORT_ERROR0(VLDTR_E_MD_ZERORVA);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    // Check the method flags
    // Validate access
    if((dwFlags & mdMemberAccessMask) == mdMemberAccessMask)
    {
        REPORT_ERROR0(VLDTR_E_FMD_BADACCESSFLAG);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // Final/NewSlot must be virtual
    if((IsMdFinal(dwFlags)||IsMdNewSlot(dwFlags)||IsMdCheckAccessOnOverride(dwFlags)) 
        && !IsMdVirtual(dwFlags))
    {
        REPORT_ERROR0(VLDTR_E_MD_FINNOTVIRT);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // Static can't be final or virtual
    if(IsMdStatic(dwFlags))
    {
        if(IsMdFinal(dwFlags) || IsMdVirtual(dwFlags) || IsMdNewSlot(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_MD_STATANDFINORVIRT);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    else // non-static can't be an entry point
    {
        if(g_tkEntryPoint == veCtxt.Token)
        {
            REPORT_ERROR0(VLDTR_E_EP_INSTANCE);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    if(IsMdAbstract(dwFlags))
    {
        // Can't be both abstract and final
        if(IsMdFinal(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_MD_ABSTANDFINAL);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        // If abstract, must be not miForwardRef, not Pinvoke, and must be virtual
        if(IsMiForwardRef(dwImplFlags))
        {
            REPORT_ERROR0(VLDTR_E_MD_ABSTANDIMPL);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        if(IsMdPinvokeImpl(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_MD_ABSTANDPINVOKE);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        if(!IsMdVirtual(dwFlags))
        {
            REPORT_ERROR0(VLDTR_E_MD_ABSTNOTVIRT);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    // If PrivateScope, must have RVA!=0
    if(IsMdPrivateScope(dwFlags) && (pRecord->GetRVA() ==0))
    {
        REPORT_ERROR0(VLDTR_E_MD_PRIVSCOPENORVA);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // RTSpecialName => SpecialName
    if(IsMdRTSpecialName(dwFlags) && !IsMdSpecialName(dwFlags))
    {
        REPORT_ERROR0(VLDTR_E_FMD_RTSNNOTSN);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Method having security must be marked mdHasSecurity and vice versa
    {
        ULONG ridStart = 1;
        ULONG ridEnd = pMiniMd->getCountDeclSecuritys() + 1;
        ULONG index;
        BOOL  bHasSecurity = FALSE;
        for (index = ridStart; index < ridEnd; index ++ )
        {
            DeclSecurityRec *pDeclSecurityRecord;
            IfFailGo(pMiniMd->GetDeclSecurityRecord(index, &pDeclSecurityRecord));
            if ( veCtxt.Token == pMiniMd->getParentOfDeclSecurity(pDeclSecurityRecord))
            {
                bHasSecurity = TRUE;
                break;
            }
        }
        if(!bHasSecurity) // No records, check for CA "SuppressUnmanagedCodeSecurityAttribute"
        {
            bHasSecurity = (S_OK == ImportHelper::GetCustomAttributeByName(pMiniMd, veCtxt.Token, 
                "System.Security.SuppressUnmanagedCodeSecurityAttribute", NULL, NULL));
        }
        if(bHasSecurity != (IsMdHasSecurity(dwFlags)!=0))
        {
            REPORT_ERROR0(bHasSecurity ? VLDTR_E_FMD_SECURNOTMARKED : VLDTR_E_FMD_MARKEDNOSECUR);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    // Validate method semantics
    {
        MethodSemanticsRec  *pRec;
        ULONG               ridEnd;
        ULONG               index;
        unsigned            uTally = 0;
        mdToken             tkEventProp;
        ULONG               iCount;
        DWORD               dwSemantic;
        // get the range of method rids given a typedef
        ridEnd = pMiniMd->getCountMethodSemantics();

        for (index = 1; index <= ridEnd; index++ )
        {
            IfFailGo(pMiniMd->GetMethodSemanticsRecord(index, &pRec));
            if ( pMiniMd->getMethodOfMethodSemantics(pRec) ==  veCtxt.Token )
            {
                uTally++;
                if(uTally > 1)
                {
                    REPORT_ERROR0(VLDTR_E_MD_MULTIPLESEMANTICS);
                    SetVldtrCode(&hrSave, VLDTR_S_WRN);
                }
                tkEventProp = pMiniMd->getAssociationOfMethodSemantics(pRec);
                if((TypeFromToken(tkEventProp) == mdtEvent)||(TypeFromToken(tkEventProp) == mdtProperty))
                {
                    iCount = (TypeFromToken(tkEventProp) == mdtEvent) ? pMiniMd->getCountEvents() :
                                                                        pMiniMd->getCountPropertys();
                    if(RidFromToken(tkEventProp) > iCount)
                    {
                        REPORT_ERROR1(VLDTR_E_MD_SEMANTICSNOTEXIST, tkEventProp);
                        SetVldtrCode(&hrSave, VLDTR_S_WRN);
                    }
                }
                else
                {
                    REPORT_ERROR1(VLDTR_E_MD_INVALIDSEMANTICS, tkEventProp);
                    SetVldtrCode(&hrSave, VLDTR_S_WRN);
                }
                // One and only one semantics flag must be set
                iCount = 0;
                dwSemantic = pRec->GetSemantic();
                if(IsMsSetter(dwSemantic)) iCount++;
                if(IsMsGetter(dwSemantic)) iCount++;
                if(IsMsOther(dwSemantic))  iCount++;
                if(IsMsAddOn(dwSemantic))  iCount++;
                if(IsMsRemoveOn(dwSemantic)) iCount++;
                if(IsMsFire(dwSemantic)) iCount++;
                if(iCount != 1)
                {
                    REPORT_ERROR1(iCount ? VLDTR_E_MD_MULTSEMANTICFLAGS : VLDTR_E_MD_NOSEMANTICFLAGS, tkEventProp);
                    SetVldtrCode(&hrSave, VLDTR_S_WRN);
                }
            }
        }// end for(index)
    }
    // Check the method's impl.map
    {
        RID iRecord;
        IfFailGo(pMiniMd->FindImplMapHelper(veCtxt.Token, &iRecord));
        if(IsMdPinvokeImpl(dwFlags))
        {
            // must be static
            if(!IsMdStatic(dwFlags))
            {
                REPORT_ERROR0(VLDTR_E_FMD_PINVOKENOTSTATIC);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            // must have either ImplMap or RVA == 0
            if (InvalidRid(iRecord))
            {
                if(pRecord->GetRVA()==0)
                {
                    REPORT_ERROR0(VLDTR_E_FMD_MARKEDNOPINVOKE);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
            }
            else
            {
                if(pRecord->GetRVA()!=0)
                {
                    // C++ emits ImplMaps for IJW methods,
                    // with resolution=ModuleRef with name ""
                    ImplMapRec  *pIMRecord;
                    mdToken     tkModuleRef;
                    IfFailGo(pMiniMd->GetImplMapRecord(iRecord, &pIMRecord));
                    tkModuleRef = pMiniMd->getImportScopeOfImplMap(pIMRecord);
                    if((TypeFromToken(tkModuleRef) == mdtModuleRef) && (!IsNilToken(tkModuleRef)))
                    {
                        ModuleRefRec *pMRRecord;              // ModuleRef record.
                        LPCUTF8     szMRName;                 // ModuleRef name.
                        // Get the ModuleRef record.
                        IfFailGo(pMiniMd->GetModuleRefRecord(RidFromToken(tkModuleRef), &pMRRecord));
                        // Check ModuleRef name is "".
                        IfFailGo(pMiniMd->getNameOfModuleRef(pMRRecord, &szMRName));
                        if (*szMRName)
                        {
                            REPORT_ERROR0(VLDTR_E_MD_RVAANDIMPLMAP);
                            SetVldtrCode(&hrSave, VLDTR_S_ERR);
                        }
                    }
                }
                else
                {
                    hr = ValidateImplMap(iRecord);
                    if(hr != S_OK)
                    {
                        REPORT_ERROR0(VLDTR_E_FMD_BADIMPLMAP);
                        SetVldtrCode(&hrSave, VLDTR_S_WRN);
                    }
                }
            }

        }
        else
        {
            // must have no ImplMap
            if (!InvalidRid(iRecord))
            {
                REPORT_ERROR0(VLDTR_E_FMD_PINVOKENOTMARKED);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
        }
    }
    // Validate params
    {
        ULONG ridStart = pMiniMd->getParamListOfMethod(pRecord);
        ULONG ridEnd;
        IfFailGo(pMiniMd->getEndParamListOfMethod(rid, &ridEnd));
        ParamRec* pRec;
        ULONG cbSigT;
        PCCOR_SIGNATURE typePtr;
        IfFailGo(pMiniMd->getSignatureOfMethod(pRecord, &typePtr, &cbSigT));
        unsigned  callConv = CorSigUncompressData(typePtr);  // get the calling convention out of the way
        unsigned  numTyArgs = 0;
        if (callConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
        { 
            bIsGeneric = TRUE;
            numTyArgs = CorSigUncompressData(typePtr);
        }

        unsigned  numArgs = CorSigUncompressData(typePtr);
        USHORT    usPrevSeq = 0;

        for(ULONG ridP = ridStart; ridP < ridEnd; ridP++)
        {
            RID tempRid;
            IfFailGo(pMiniMd->GetParamRecord(ridP, &pRec));
            // Sequence order must be ascending
            if(ridP > ridStart)
            {
                if(pRec->GetSequence() <= usPrevSeq)
                {
                    REPORT_ERROR2(VLDTR_E_MD_PARAMOUTOFSEQ, ridP-ridStart,pRec->GetSequence());
                    SetVldtrCode(&hrSave, VLDTR_S_WRN);
                }
            }
            usPrevSeq = pRec->GetSequence();
            // Sequence value must not exceed num of arguments
            if(usPrevSeq > numArgs)
            {
                REPORT_ERROR2(VLDTR_E_MD_PARASEQTOOBIG, ridP-ridStart,usPrevSeq);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }

            // Param having marshaling must be marked pdHasFieldMarshal and vice versa
            IfFailGo(pMiniMd->FindFieldMarshalHelper(TokenFromRid(ridP,mdtParamDef), &tempRid));
            if (InvalidRid(tempRid) == (IsPdHasFieldMarshal(pRec->GetFlags()) != 0))
            {
                REPORT_ERROR1(IsPdHasFieldMarshal(pRec->GetFlags()) ? VLDTR_E_MD_PARMMARKEDNOMARSHAL
                    : VLDTR_E_MD_PARMMARSHALNOTMARKED, ridP-ridStart);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            // Param having const value must be marked pdHasDefault and vice versa
            IfFailGo(pMiniMd->FindConstantHelper(TokenFromRid(ridP,mdtParamDef), &tempRid));
            if (InvalidRid(tempRid) == (IsPdHasDefault(pRec->GetFlags()) != 0))
            {
                REPORT_ERROR1(IsPdHasDefault(pRec->GetFlags()) ? VLDTR_E_MD_PARMMARKEDNODEFLT 
                    : VLDTR_E_MD_PARMDEFLTNOTMARKED, ridP-ridStart);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
        }
    }

    // Generic Method related checks
    if (bIsGeneric)
    {
        if (bIsCctor)
        {
            REPORT_ERROR0(VLDTR_E_MD_GENERIC_CCTOR);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }

        if (bIsCtor)
        {
            REPORT_ERROR0(VLDTR_E_MD_GENERIC_CTOR);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }

        if (bIsParentImport)
        {
            REPORT_ERROR0(VLDTR_E_MD_GENERIC_IMPORT);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }

    }

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateMethod()
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//*****************************************************************************
// Validate the given ParamPtr.
//*****************************************************************************
HRESULT RegMeta::ValidateParamPtr(RID rid)
{
    return S_OK;
}   // RegMeta::ValidateParamPtr()

//*****************************************************************************
// Validate the given Param.
//*****************************************************************************
HRESULT RegMeta::ValidateParam(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    ParamRec    *pRecord;               // Param record
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.
    LPCSTR      szName;                 // Param name.

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));

    // Get the InterfaceImpl record.
    veCtxt.Token = TokenFromRid(rid, mdtParamDef);
    veCtxt.uOffset = 0;

    DWORD   dwBadFlags = 0;
    DWORD   dwFlags = 0;
    IfFailGo(pMiniMd->GetParamRecord(rid, &pRecord));
    // Name, if any, must not exceed MAX_CLASSNAME_LENGTH
    IfFailGo(pMiniMd->getNameOfParam(pRecord, &szName));
    ULONG L = (ULONG)strlen(szName);
    if(L >= MAX_CLASSNAME_LENGTH)
    {
        REPORT_ERROR2(VLDTR_E_TD_NAMETOOLONG, L, (ULONG)(MAX_CLASSNAME_LENGTH-1));
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    
    // Flags must be as defined in CorHdr.h
    dwBadFlags = ~(pdIn | pdOut | pdOptional | pdHasDefault | pdHasFieldMarshal);
    dwFlags = pRecord->GetFlags();
    if(dwFlags & dwBadFlags)
    {
        REPORT_ERROR1(VLDTR_E_PD_BADFLAGS, dwFlags);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    
    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateParam()

//*****************************************************************************
// Helper function for ValidateInterfaceImpl
//*****************************************************************************
int IsMethodImplementedByClass(CMiniMdRW *pMiniMd, 
                                mdToken tkMethod, 
                                LPCUTF8 szName,
                                PCCOR_SIGNATURE pSig,
                                ULONG cbSig,
                                mdToken tkClass)
{
    HRESULT hr;
    int numImpl = 0;
    if(TypeFromToken(tkMethod) == mdtMethodDef)
    {
        if(TypeFromToken(tkClass) == mdtTypeSpec)
        {
            // We are trying to find out if an interface method is implemented in the generic class tkClass.
            // Simple signature comparison doesn't work here, because "int Method()" in the interface might 
            // be implemented by "T Type.Method()" in the generic type.
            // Therefore we assume it is implemented. Atlernatively we could implement better signature 
            // comparison which would match T with any other type, etc.
            numImpl = 1;
        }
        else if(TypeFromToken(tkClass) == mdtTypeDef)
        {
            TypeDefRec *pClass;
            IfFailRet(pMiniMd->GetTypeDefRecord(RidFromToken(tkClass), &pClass));
            RID ridClsStart = pMiniMd->getMethodListOfTypeDef(pClass);
            RID ridClsEnd;
            IfFailRet(pMiniMd->getEndMethodListOfTypeDef(RidFromToken(tkClass), &ridClsEnd));
            mdMethodDef tkFoundMethod = 0;
            DWORD dwFoundMethodFlags = 0;
            // Check among methods
            hr = ImportHelper::FindMethod(pMiniMd, tkClass, szName, pSig, cbSig, &tkFoundMethod, 0);
            if(SUCCEEDED(hr))
            {
                MethodRec * pMethod;
                IfFailRet(pMiniMd->GetMethodRecord(RidFromToken(tkFoundMethod), &pMethod));
                if(pMethod)
                {
                    dwFoundMethodFlags = pMiniMd->getFlagsOfMethod(pMethod);
                    if(IsMdVirtual(dwFoundMethodFlags)) //&&!IsMdNewSlot(dwFoundMethodFlags))
                        numImpl = 1;
                }
            }
            if (numImpl==0) //if(hr == CLDB_E_RECORD_NOTFOUND)
            { // Check among MethodImpls
                RID ridImpl;
                for(RID idxCls = ridClsStart; idxCls < ridClsEnd; idxCls++)
                {
                    RID ridCls;
                    IfFailRet(pMiniMd->GetMethodRid(idxCls, &ridCls));
    
                    hr = ImportHelper::FindMethodImpl(pMiniMd,tkClass,TokenFromRid(ridCls,mdtMethodDef),
                        tkMethod,&ridImpl);
                    if(hr != CLDB_E_RECORD_NOTFOUND)
                    { 
                        if(SUCCEEDED(hr)) numImpl++;
                        break; 
                    }
                }
                if(numImpl == 0)
                {
                    // Check if parent class implements this method
                    mdToken tkParent = pMiniMd->getExtendsOfTypeDef(pClass);
                    if(RidFromToken(tkParent))
                           numImpl = IsMethodImplementedByClass(pMiniMd,tkMethod,szName,pSig,cbSig,tkParent);
                }
            }
        }
        else if (TypeFromToken(tkClass) == mdtTypeRef)
        {
            TypeRefRec  *pRecord;               // TypeRef record.
            LPCSTR      szTRNamespace;          // TypeRef Namespace.
            LPCSTR      szTRName;               // TypeRef Name.

            // Get the TypeRef record.
            IfFailRet(pMiniMd->GetTypeRefRecord(RidFromToken(tkClass), &pRecord));
        
            // Check name is not NULL.
            IfFailRet(pMiniMd->getNamespaceOfTypeRef(pRecord, &szTRNamespace));
            IfFailRet(pMiniMd->getNameOfTypeRef(pRecord, &szTRName));
            
            mdToken tkRefScope = pMiniMd->getResolutionScopeOfTypeRef(pRecord);
            if (tkRefScope == TokenFromRid(1, mdtModule))
            {
                // if the typeref is referring to a type in this module then
                // we should check the type definition it is referring to
                mdTypeDef tkTypeDef;
                hr = ImportHelper::FindTypeDefByName(pMiniMd, szTRNamespace, szTRName, tkRefScope, &tkTypeDef);
                if (SUCCEEDED(hr))
                    numImpl = IsMethodImplementedByClass(pMiniMd, tkMethod, szName, pSig, cbSig, tkTypeDef);
            }
            else if ((strcmp(szTRNamespace, BASE_NAMESPACE) == 0) && 
                      ((strcmp(szTRName, BASE_OBJECT_CLASSNAME) == 0) || 
                       (strcmp(szTRName, BASE_VTYPE_CLASSNAME) == 0) || 
                       (strcmp(szTRName, BASE_ENUM_CLASSNAME) == 0)))
            {
                if (((strcmp(szName, SYSTEM_OBJECT_TOSTRING_METHODNAME) == 0) && 
                     (cbSig == _countof(g_sigSystemObject_ToString)) && 
                     (memcmp(pSig, g_sigSystemObject_ToString, cbSig) == 0)) || 
                    ((strcmp(szName, SYSTEM_OBJECT_GETHASHCODE_METHODNAME) == 0) && 
                     (cbSig == _countof(g_sigSystemObject_GetHashCode)) && 
                     (memcmp(pSig, g_sigSystemObject_GetHashCode, cbSig) == 0)) || 
                    ((strcmp(szName, SYSTEM_OBJECT_EQUALS_METHODNAME) == 0) && 
                     (cbSig == _countof(g_sigSystemObject_Equals)) && 
                     (memcmp(pSig, g_sigSystemObject_Equals, cbSig) == 0)))
                {
                    numImpl = 1; // Method signature matches one of System.Object's virtual methods
                }
                else
                {
                    numImpl = 0; // These classes (System.Object, System.ValueType and System.Enum) don't implement any other virtual methods
                }
            }
            else
            {
                numImpl = -1; // The method is defined in another module, we cannot verify it (no external modules are loaded)
            }
        }
    }
    return numImpl;
}

//*****************************************************************************
// Validate the given InterfaceImpl.
//*****************************************************************************
//@todo GENERICS: complete logic for type specs 
// - for now, we just allow them, but we should be checking more properties
HRESULT RegMeta::ValidateInterfaceImpl(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    InterfaceImplRec *pRecord;          // InterfaceImpl record.
    mdTypeDef   tkClass;                // Class implementing the interface.
    mdToken     tkInterface;            // TypeDef for the interface.
    mdInterfaceImpl tkInterfaceImpl;    // Duplicate InterfaceImpl.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.
    BOOL        fCheckTheMethods=TRUE;

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));

    // Get the InterfaceImpl record.
    veCtxt.Token = TokenFromRid(rid, mdtInterfaceImpl);
    veCtxt.uOffset = 0;

    IfFailGo(pMiniMd->GetInterfaceImplRecord(rid, &pRecord));

    // Get implementing Class and the TypeDef for the interface.
    tkClass = pMiniMd->getClassOfInterfaceImpl(pRecord);

    // No validation needs to be done on deleted records.
    if (IsNilToken(tkClass))
        goto ErrExit;

    tkInterface = pMiniMd->getInterfaceOfInterfaceImpl(pRecord);

    // Validate that the Class is TypeDef.
    if((!IsValidToken(tkClass))||(TypeFromToken(tkClass) != mdtTypeDef)/*&&(TypeFromToken(tkClass) != mdtTypeRef)*/)
    {
        REPORT_ERROR1(VLDTR_E_IFACE_BADIMPL, tkClass);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
        fCheckTheMethods = FALSE;
    }
    // Validate that the Interface is TypeDef or TypeRef or TypeSpec
    if((!IsValidToken(tkInterface))||(TypeFromToken(tkInterface) != mdtTypeDef)&&(TypeFromToken(tkInterface) != mdtTypeRef)
        &&(TypeFromToken(tkInterface) != mdtTypeSpec))
    {
        REPORT_ERROR1(VLDTR_E_IFACE_BADIFACE, tkInterface);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
        fCheckTheMethods = FALSE;
    }
    // Validate that Interface is marked tdInterface.
    else if(TypeFromToken(tkInterface) == mdtTypeDef)
    {
        TypeDefRec *pTDRec;
        IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkInterface), &pTDRec));
        if(!IsTdInterface(pTDRec->GetFlags()))
        {
            REPORT_ERROR1(VLDTR_E_IFACE_NOTIFACE, tkInterface);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        
    }

    // Look for duplicates.
    hr = ImportHelper::FindInterfaceImpl(pMiniMd, tkClass, tkInterface,
                                         &tkInterfaceImpl, rid);
    if (hr == S_OK)
    {
        REPORT_ERROR1(VLDTR_E_IFACE_DUP, tkInterfaceImpl);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else if (hr == CLDB_E_RECORD_NOTFOUND)
        hr = S_OK;
    else
        IfFailGo(hr);

    // Validate that the Class (if not interface or abstract) implements all the methods of Interface
    if((TypeFromToken(tkInterface) == mdtTypeDef) && fCheckTheMethods && (tkInterface != tkClass))
    {
        TypeDefRec *pClass;
        IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkClass), &pClass));
        if(!(IsTdAbstract(pClass->GetFlags())
             ||IsTdImport(pClass->GetFlags())
             ||IsTdInterface(pClass->GetFlags())))
        {
            TypeDefRec *pInterface;
            IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkInterface), &pInterface));
            RID ridIntStart = pMiniMd->getMethodListOfTypeDef(pInterface);
            RID ridIntEnd;
            IfFailGo(pMiniMd->getEndMethodListOfTypeDef(RidFromToken(tkInterface), &ridIntEnd));
            MethodRec*  pIntMethod;
            for(RID idxInt = ridIntStart; idxInt < ridIntEnd; idxInt++)
            {
                RID ridInt;
                IfFailGo(pMiniMd->GetMethodRid(idxInt, &ridInt));
                IfFailGo(pMiniMd->GetMethodRecord(ridInt, &pIntMethod));
                const char* szName;
                IfFailGo(pMiniMd->getNameOfMethod(pIntMethod, &szName));
                if(!IsMdStatic(pIntMethod->GetFlags()) 
                    && !IsDeletedName(szName) 
                    && !IsVtblGapName(szName))
                {
                ULONG       cbSig;
                PCCOR_SIGNATURE pSig;
                IfFailGo(pMiniMd->getSignatureOfMethod(pIntMethod, &pSig, &cbSig));
                if(cbSig)
                {
                        int num = IsMethodImplementedByClass(pMiniMd,TokenFromRid(ridInt,mdtMethodDef),szName,pSig,cbSig,tkClass);
                        if(num == 0) 
                        { // Error: method not implemented
                            REPORT_ERROR3(VLDTR_E_IFACE_METHNOTIMPL, tkClass, tkInterface, TokenFromRid(ridInt,mdtMethodDef));
                            SetVldtrCode(&hrSave, VLDTR_S_ERR);
                        }
                        if(num == -1)
                        {
                            // Traced to a TypeRef, which might implement the method, give warning
                            REPORT_ERROR3(VLDTR_E_IFACE_METHNOTIMPLTHISMOD, tkClass, tkInterface, TokenFromRid(ridInt,mdtMethodDef));
                            SetVldtrCode(&hrSave, VLDTR_S_WRN);
                        }
                        if(num > 1) 
                        { // Error: multiple method implementation
                            REPORT_ERROR3(VLDTR_E_IFACE_METHMULTIMPL, tkClass, tkInterface, TokenFromRid(ridInt,mdtMethodDef));
                            SetVldtrCode(&hrSave, VLDTR_S_ERR);
                        }
                }
            }
        }
    }
    }
    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateInterfaceImpl()

//*****************************************************************************
// Validate the given GenericParam.
//*****************************************************************************
HRESULT RegMeta::ValidateGenericParam(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    GenericParamRec *pRecord;           // GenericParam record.
    LPCSTR      szName;                 // GenericParam name field.
    mdToken     tkOwner;                // GenericParam owner field.
    ULONG       ulNumber;                 // GenericParam number field.
    DWORD       dwFlags;                  // GenericParam flags field

    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));

    // Get the GenericParam record.
    veCtxt.Token = TokenFromRid(rid, mdtGenericParam);
    veCtxt.uOffset = 0;
    IfFailGo(pMiniMd->GetGenericParamRecord(rid, &pRecord));

    // 1. GenericParam may contain zero or more rows.
    // (Nothing to check.)

    tkOwner = pMiniMd->getOwnerOfGenericParam(pRecord);
    // 2. Owner must be a valid token and a type def or method def 
    // (Already checked by ValidateRecord)

    // CLR tolerates Nil owners, ECMA does not
    if(IsNilToken(tkOwner)) 
    {
        REPORT_ERROR0(VLDTR_E_GP_OWNERNIL); 
        SetVldtrCode(&hrSave, VLDTR_S_WRN);
    }
   
    //3. Every generic type shall own one row in the Generic Param table for each of its type parameters. [ERROR]
    // (Nothing to check, as the arity of a generic type is, by definition, the number of generic param entries).

    //4. Every generic method shall own one row in the Generic Param table for each of its type parameters. [ERROR]
    // (This is checked in ValidateMethodSig, error VLDTR_E_MD_GPMISMATCH).

    ulNumber = pMiniMd->getNumberOfGenericParam(pRecord);

    // 5. Flags must be valid
    {
        DWORD dwInvalidMask, dwExtraBits;

        dwFlags = pMiniMd->getFlagsOfGenericParam(pRecord);


        // check for extra bits
        dwInvalidMask = (DWORD)~(gpVarianceMask|gpSpecialConstraintMask); 
        dwExtraBits = dwFlags & dwInvalidMask;
        if(dwExtraBits)
        {
            //@GENERICS: we could use a custom error,
            // but this is one is already used in more than one context.
            REPORT_ERROR1(VLDTR_E_TD_EXTRAFLAGS, dwExtraBits); 
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }

        //Check Variance
        { 
            DWORD dwVariance = dwFlags & gpVarianceMask;
            switch (dwVariance)
            {
                case gpNonVariant: 
                    // always ok
                    break;
                case gpCovariant:
                case gpContravariant:
                    if (TypeFromToken(tkOwner)==mdtTypeDef)
                    {
                        if (IsNilToken(tkOwner))
                            break;
                        TypeDefRec *pTypeDefRec;
                        IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkOwner), &pTypeDefRec));
                        // co-contra variance only legal on interfaces and delegates
                        // If owner is not an interface and does not extend MultiCastDelegate, report an error
                        if(!IsTdInterface(pTypeDefRec->GetFlags()))
                        {
                            // Get the parent of the TypeDef.
                            mdToken tkExtends = pMiniMd->getExtendsOfTypeDef(pTypeDefRec);
                            LPCSTR      szExtName = NULL;       // Parent Name.
                            LPCSTR      szExtNameSpace = NULL;  // Parent NameSpace.
                            BOOL bExtendsMCDelegate = FALSE;
                            
                            // Determine if the parent is MCDelegate
                            if (TypeFromToken(tkExtends) != mdtTypeSpec) 
                            {
                                if (TypeFromToken(tkExtends) == mdtTypeRef)
                                {
                                    TypeRefRec *pExtTypeRefRec;
                                    IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(tkExtends), &pExtTypeRefRec));
                                    mdToken tkResScope = pMiniMd->getResolutionScopeOfTypeRef(pExtTypeRefRec);
                                    if (RidFromToken(tkResScope) && (TypeFromToken(tkResScope) == mdtAssemblyRef))
                                    {
                                        AssemblyRefRec * pARRec;
                                        IfFailGo(pMiniMd->GetAssemblyRefRecord(RidFromToken(tkResScope), &pARRec));
                                        LPCSTR szAssemblyRefName;
                                        IfFailGo(pMiniMd->getNameOfAssemblyRef(pARRec, &szAssemblyRefName));
                                        if ((0 == SString::_stricmp("mscorlib", szAssemblyRefName)) || (0 == SString::_stricmp("System.Runtime", szAssemblyRefName)))
                                        // otherwise don't even bother extracting the name
                                        {
                                            IfFailGo(pMiniMd->getNameOfTypeRef(pExtTypeRefRec, &szExtName));
                                            IfFailGo(pMiniMd->getNamespaceOfTypeRef(pExtTypeRefRec, &szExtNameSpace));
                                        }
                                    }
                                }
                                else if (TypeFromToken(tkExtends) == mdtTypeDef)
                                {
                                    if (g_fValidatingMscorlib) // otherwise don't even bother extracting the name
                                    {
                                        TypeDefRec * pExtTypeRefRec;
                                        IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkExtends), &pExtTypeRefRec));
                                        IfFailGo(pMiniMd->getNameOfTypeDef(pExtTypeRefRec, &szExtName));
                                        IfFailGo(pMiniMd->getNamespaceOfTypeDef(pExtTypeRefRec, &szExtNameSpace));
                                    }
                                }
                                
                                bExtendsMCDelegate  = 
                                    szExtNameSpace && szExtName && 
                                    (0 == strcmp(szExtNameSpace,BASE_NAMESPACE)) && 
                                    (0 == strcmp(szExtName,BASE_MCDELEGATE_CLASSNAME));
                            }
                      
                            // Report any error
                            if (!bExtendsMCDelegate)
                            {
                                REPORT_ERROR1(VLDTR_E_GP_UNEXPECTED_OWNER_FOR_VARIANT_VAR,tkOwner);
                                SetVldtrCode(&hrSave, VLDTR_S_ERR);
                            }
                        }
                    }
                    else
                    {
                        // co-contra variance never legal on MVARs
                        REPORT_ERROR0(VLDTR_E_GP_ILLEGAL_VARIANT_MVAR);
                        SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    }
                    break;
                default:
                    REPORT_ERROR1(VLDTR_E_GP_ILLEGAL_VARIANCE_FLAGS,dwFlags); 
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    break;
            }
        }
        
        // Check special constraints
        { 
            DWORD dwSpecialConstraints = dwFlags & gpSpecialConstraintMask;
            // It is illegal go declare both gpNotNullableValueTypeConstraint
            // and gpReferenceTypeConstraint, but gpDefaultConstructorConstraint
            // is legal with either (or neither).
            if ((dwSpecialConstraints & (gpReferenceTypeConstraint | gpNotNullableValueTypeConstraint)) == 
                (gpReferenceTypeConstraint | gpNotNullableValueTypeConstraint))
            {
                    REPORT_ERROR1(VLDTR_E_GP_REFANDVALUETYPE,dwFlags); 
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
        }
    }

    // 6. Number shall have a value >=0 and < number of type parameters in owner type or method.
    // 7. Successive rows of the GenericParam table that are owned by the same method (sic) (owner?) shall
    //    be ordered by increasing Number value; there can be no gaps in the Number sequence.
    {
        if(ulNumber>0)
        {  if(rid==1)
           {
               REPORT_ERROR0(VLDTR_E_GP_NONSEQ_BY_NUMBER);
               SetVldtrCode(&hrSave, VLDTR_S_ERR);
           }
           else 
           {
               GenericParamRec *pPredRecord;
               IfFailGo(pMiniMd->GetGenericParamRecord(rid-1, &pPredRecord));
               mdToken tkPredOwner = pMiniMd->getOwnerOfGenericParam(pPredRecord);
               ULONG ulPredNumber = pMiniMd->getNumberOfGenericParam(pPredRecord);
               if (tkPredOwner != tkOwner) 
               {
                   REPORT_ERROR0(VLDTR_E_GP_NONSEQ_BY_OWNER);
                   SetVldtrCode(&hrSave, VLDTR_S_ERR);
               }
               if (ulPredNumber != ulNumber-1) 
               {
                   REPORT_ERROR0(VLDTR_E_GP_NONSEQ_BY_NUMBER);
                   SetVldtrCode(&hrSave, VLDTR_S_ERR);
               }
           }
        }
    }

    // 8. Name must be non-null and not too long 
    IfFailGo(pMiniMd->getNameOfGenericParam(pRecord, &szName));
    if (!*szName)
    {
        // name is NULL.
        REPORT_ERROR0(VLDTR_E_GP_NAMENULL);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else
    {
        if(!strcmp(szName,COR_DELETED_NAME_A)) goto ErrExit; //@GENERICS: do we allow parameters to be deleted?
        ULONG L = (ULONG)strlen(szName);
        if(L >= MAX_CLASSNAME_LENGTH)
        {
            REPORT_ERROR2(VLDTR_E_TD_NAMETOOLONG, L, (ULONG)(MAX_CLASSNAME_LENGTH-1));
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

#ifdef THIS_RULE_IS_DISABLED_BECAUSE_CSHARP_EMITS_DUP_NAMES_AND_DOESNT_WANT_TO_STOP
    // 9. There shall be no duplicates based upon Owner and Name
    if (szName)
    {
        mdGenericParam tkDupGenericParam;
        hr = ImportHelper::FindGenericParamByOwner(pMiniMd, tkOwner, szName, NULL, &tkDupGenericParam, rid);
        if (hr == S_OK)
        {
            REPORT_ERROR1(VLDTR_E_GP_DUPNAME, tkDupGenericParam);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else if (hr == CLDB_E_RECORD_NOTFOUND)
            hr = S_OK;
        else
            IfFailGo(hr);
    }
#endif

    // 10. There shall be no duplicates based upon Owner and Number
    {
        mdGenericParam tkDupGenericParam;
        hr = ImportHelper::FindGenericParamByOwner(pMiniMd, tkOwner, NULL, &ulNumber, &tkDupGenericParam, rid);
        if (hr == S_OK)
        {
            REPORT_ERROR1(VLDTR_E_GP_DUPNUMBER, tkDupGenericParam);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else if (hr == CLDB_E_RECORD_NOTFOUND)
            hr = S_OK;
        else
            IfFailGo(hr);
    }

    hr = hrSave;

ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateGenericParam()

//*****************************************************************************
// Validate the given MemberRef.
//*****************************************************************************
HRESULT RegMeta::ValidateMemberRef(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    MemberRefRec *pRecord;              // MemberRef record.
    mdMemberRef tkMemberRef;            // Duplicate MemberRef.
    mdToken     tkClass;                // MemberRef parent.
    LPCSTR      szName;                 // MemberRef name.
    PCCOR_SIGNATURE pbSig;              // MemberRef signature.
    PCCOR_SIGNATURE pbSigTmp;           // Temp copy of pbSig, so that can be changed.
    ULONG       cbSig;                  // Size of sig in bytes.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));

    // Get the MemberRef record.
    veCtxt.Token = TokenFromRid(rid, mdtMemberRef);
    veCtxt.uOffset = 0;

    IfFailGo(pMiniMd->GetMemberRefRecord(rid, &pRecord));

    // Do checks for name validity.
    IfFailGo(pMiniMd->getNameOfMemberRef(pRecord, &szName));
    if (!*szName)
    {
        REPORT_ERROR0(VLDTR_E_MR_NAMENULL);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else 
    {
        if (IsVtblGapName(szName))
        {
            REPORT_ERROR0(VLDTR_E_MR_VTBLNAME);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else if (IsDeletedName(szName))
        {
            REPORT_ERROR0(VLDTR_E_MR_DELNAME);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        ULONG L = (ULONG)strlen(szName);
        if(L >= MAX_CLASSNAME_LENGTH)
        {
            // Name too long
            REPORT_ERROR2(VLDTR_E_TD_NAMETOOLONG, L, (ULONG)(MAX_CLASSNAME_LENGTH-1));
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

    // MemberRef parent should never be nil in a PE file.
    tkClass = pMiniMd->getClassOfMemberRef(pRecord);
    if (m_ModuleType == ValidatorModuleTypePE && IsNilToken(tkClass))
    {
        REPORT_ERROR0(VLDTR_E_MR_PARNIL);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Verify that the signature is a valid signature as per signature spec.
    IfFailGo(pMiniMd->getSignatureOfMemberRef(pRecord, &pbSig, &cbSig));

    // Do some semantic checks based on the signature.
    if (hr == S_OK)
    {
        ULONG   ulCallingConv;
        ULONG   ulArgCount;
        ULONG   ulTyArgCount = 0;
        ULONG   ulCurByte = 0;

        // Extract calling convention.
        pbSigTmp = pbSig;
        ulCurByte += CorSigUncompressedDataSize(pbSigTmp);
        ulCallingConv = CorSigUncompressData(pbSigTmp);

        // Get the type argument count
        if (ulCallingConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
            ulCurByte += CorSigUncompressedDataSize(pbSigTmp);
            ulTyArgCount = CorSigUncompressData(pbSigTmp);
        }

        // Get the argument count.
        ulCurByte += CorSigUncompressedDataSize(pbSigTmp);
        ulArgCount = CorSigUncompressData(pbSigTmp);

        // Calling convention must be one of IMAGE_CEE_CS_CALLCONV_DEFAULT,
        // IMAGE_CEE_CS_CALLCONV_VARARG or IMAGE_CEE_CS_CALLCONV_FIELD.
        if (!isCallConv(ulCallingConv, IMAGE_CEE_CS_CALLCONV_DEFAULT) &&
            !isCallConv(ulCallingConv, IMAGE_CEE_CS_CALLCONV_VARARG) &&
            !isCallConv(ulCallingConv, IMAGE_CEE_CS_CALLCONV_FIELD))
        {
            REPORT_ERROR1(VLDTR_E_MR_BADCALLINGCONV, ulCallingConv);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        // [CLS] Calling convention must not be VARARG
        if(isCallConv(ulCallingConv, IMAGE_CEE_CS_CALLCONV_VARARG))
        {
            REPORT_ERROR0(VLDTR_E_MR_VARARGCALLINGCONV);
            SetVldtrCode(&hrSave, VLDTR_S_WRN);
        }
        
        // If the parent is a MethodDef...
        if (TypeFromToken(tkClass) == mdtMethodDef)
        {
            if (RidFromToken(tkClass) != 0)
            {
                // The MethodDef must be the same name and the fixed part of the
                // vararg signature must be the same.
                MethodRec   *pMethodRecord;     // Method Record.
                LPCSTR      szMethodName;       // Method name.
                PCCOR_SIGNATURE pbMethodSig;    // Method signature.
                ULONG       cbMethodSig;        // Size in bytes of signature.
                
                // Get Method record, name and signature.
                IfFailGo(pMiniMd->GetMethodRecord(RidFromToken(tkClass), &pMethodRecord));
                IfFailGo(pMiniMd->getNameOfMethod(pMethodRecord, &szMethodName));
                IfFailGo(pMiniMd->getSignatureOfMethod(pMethodRecord, &pbMethodSig, &cbMethodSig));
                
                // Verify that the name of the Method is the same as the MemberRef.
                if (strcmp(szName, szMethodName))
                {
                    REPORT_ERROR1(VLDTR_E_MR_NAMEDIFF, tkClass);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                
                if (isCallConv(ulCallingConv, IMAGE_CEE_CS_CALLCONV_VARARG))
                {   // It's VARARG calling convention
                    CQuickBytes qbFixedSig;         // Quick bytes to hold the fixed part of the variable signature.
                    ULONG       cbFixedSig;         // Size in bytes of the fixed part.
                    
                    // Get the fixed part of the vararg signature of the MemberRef.
                    hr = _GetFixedSigOfVarArg(pbSig, cbSig, &qbFixedSig, &cbFixedSig);
                    if (FAILED(hr) || cbFixedSig != cbMethodSig ||
                        memcmp(pbMethodSig, qbFixedSig.Ptr(), cbFixedSig))
                    {
                        UnifiedAssemblySigComparer uasc(*this);
                        MDSigComparer sc(MDSigParser(pbMethodSig, cbMethodSig),
                                         MDSigParser((PCCOR_SIGNATURE)qbFixedSig.Ptr(), cbFixedSig),
                                         uasc);
                        
                        hr = sc.CompareMethodSignature();
                        if (FAILED(hr))
                        {
                            hr = S_OK;
                            REPORT_ERROR1(VLDTR_E_MR_SIGDIFF, tkClass);
                            SetVldtrCode(&hrSave, VLDTR_S_ERR);
                        }
                    }
                }
                else
                {   // It's not VARARG calling convention - a MemberRef is referencing MethodDef (part of 
                    // NoPIA)
                    UnifiedAssemblySigComparer uasc(*this);
                    MDSigComparer sc(MDSigParser(pbMethodSig, cbMethodSig), 
                                     MDSigParser(pbSig, cbSig), 
                                     uasc);
                    
                    // Compare signatures
                    hr = sc.CompareMethodSignature();
                    if (FAILED(hr))
                    {
                        hr = S_OK;
                        REPORT_ERROR1(VLDTR_E_MR_SIGDIFF, tkClass);
                        SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    }
                }
            }
        }
        
        // There should be no duplicate MemberRefs.
        if (*szName && pbSig && cbSig)
        {
            hr = ImportHelper::FindMemberRef(pMiniMd, tkClass, szName, pbSig,
                                             cbSig, &tkMemberRef, rid, 
                                             ImportHelper::CreateHash); // Optimize for multiple calls
            if (hr == S_OK)
            {
                REPORT_ERROR1(VLDTR_E_MR_DUP, tkMemberRef);
                SetVldtrCode(&hrSave, VLDTR_S_WRN);
            }
            else if (hr == CLDB_E_RECORD_NOTFOUND)
            {
                hr = S_OK;
            }
            else
            {
                IfFailGo(hr);
            }
        }

        if (!isCallConv(ulCallingConv, IMAGE_CEE_CS_CALLCONV_FIELD))
        {
            hr = ValidateMethodSig(veCtxt.Token,pbSig, cbSig,0);
            SetVldtrCode(&hrSave,hr);
        }
    }
    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateMemberRef()

//*****************************************************************************
// Validate the given Constant.
//*****************************************************************************
HRESULT RegMeta::ValidateConstant(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    ConstantRec *pRecord;              // Constant record.
    mdToken     tkParent;              // Constant parent.
    const VOID* pbBlob;                 // Constant value blob ptr
    DWORD       cbBlob;                 // Constant value blob size
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));

    // Get the MemberRef record.
    veCtxt.Token = rid;
    veCtxt.uOffset = 0;

    ULONG maxrid = 0;
    ULONG typ = 0;
    IfFailGo(pMiniMd->GetConstantRecord(rid, &pRecord));
    IfFailGo(pMiniMd->getValueOfConstant(pRecord, (const BYTE **)&pbBlob, &cbBlob));
    switch(pRecord->GetType())
    {
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R8:
            if(pbBlob == NULL)
            {
                REPORT_ERROR1(VLDTR_E_CN_BLOBNULL, pRecord->GetType());
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
        case ELEMENT_TYPE_STRING:
            break;

        case ELEMENT_TYPE_CLASS:
            if(GET_UNALIGNED_32(pbBlob) != 0)
            {
                REPORT_ERROR1(VLDTR_E_CN_BLOBNOTNULL, pRecord->GetType());
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            break;

        default:
            REPORT_ERROR1(VLDTR_E_CN_BADTYPE, pRecord->GetType());
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
            break;
    }
    tkParent = pMiniMd->getParentOfConstant(pRecord);
    typ = TypeFromToken(tkParent);
    switch(typ)
    {
        case mdtFieldDef:
            maxrid = pMiniMd->getCountFields();
            break;
        case mdtParamDef:
            maxrid = pMiniMd->getCountParams();
            break;
        case mdtProperty:
            maxrid = pMiniMd->getCountPropertys();
            break;
    }
    switch(typ)
    {
        case mdtFieldDef:
        case mdtParamDef:
        case mdtProperty:
            {
                ULONG rid_p = RidFromToken(tkParent);
                if((0==rid_p)||(rid_p > maxrid))
                {
                    REPORT_ERROR1(VLDTR_E_CN_PARENTRANGE, tkParent);
                    SetVldtrCode(&hrSave, VLDTR_S_WRN);
                }
                break;
            }

        default:
            REPORT_ERROR1(VLDTR_E_CN_PARENTTYPE, tkParent);
            SetVldtrCode(&hrSave, VLDTR_S_WRN);
            break;
    }

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateConstant()

//*****************************************************************************
// Validate the given CustomAttribute.
//*****************************************************************************
HRESULT RegMeta::ValidateCustomAttribute(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.

    BEGIN_ENTRYPOINT_NOTHROW;
    
    CustomAttributeRec *pRecord;
    IfFailGo(pMiniMd->GetCustomAttributeRecord(rid, &pRecord));

    memset(&veCtxt, 0, sizeof(VEContext));

    veCtxt.Token = TokenFromRid(rid,mdtCustomAttribute);
    veCtxt.uOffset = 0;

    if (pRecord != NULL)
    {
        mdToken     tkOwner = pMiniMd->getParentOfCustomAttribute(pRecord);
        if(RidFromToken(tkOwner))
        { // if 0, it's deleted CA, don't pay attention
            mdToken     tkCAType = pMiniMd->getTypeOfCustomAttribute(pRecord);
            DWORD       cbValue=0;
            const BYTE *pbValue;
            IfFailGo(pMiniMd->getValueOfCustomAttribute(pRecord, &pbValue, &cbValue));
            if((TypeFromToken(tkOwner)==mdtCustomAttribute)||(!IsValidToken(tkOwner)))
            {
                REPORT_ERROR1(VLDTR_E_CA_BADPARENT, tkOwner);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            if(((TypeFromToken(tkCAType)!=mdtMethodDef)&&(TypeFromToken(tkCAType)!=mdtMemberRef))
                ||(!IsValidToken(tkCAType))||(RidFromToken(tkCAType)==0))
            {
                REPORT_ERROR1(VLDTR_E_CA_BADTYPE, tkCAType);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            else
            { //i.e. Type is valid MethodDef or MemberRef
                LPCUTF8 szName;
                PCCOR_SIGNATURE pSig=NULL;
                DWORD           cbSig=0;
                DWORD           dwFlags=0;
                if (TypeFromToken(tkCAType) == mdtMethodDef)
                {
                    MethodRec *pTypeRec;
                    IfFailGo(pMiniMd->GetMethodRecord(RidFromToken(tkCAType), &pTypeRec));
                    IfFailGo(pMiniMd->getNameOfMethod(pTypeRec, &szName));
                    IfFailGo(pMiniMd->getSignatureOfMethod(pTypeRec, &pSig, &cbSig));
                    dwFlags = pTypeRec->GetFlags();
                }
                else // it can be only MemberRef, otherwise we wouldn't be here
                {
                    MemberRefRec *pTypeRec;
                    IfFailGo(pMiniMd->GetMemberRefRecord(RidFromToken(tkCAType), &pTypeRec));
                    IfFailGo(pMiniMd->getNameOfMemberRef(pTypeRec, &szName));
                    IfFailGo(pMiniMd->getSignatureOfMemberRef(pTypeRec, &pSig, &cbSig));
                }
                if (strcmp(szName, ".ctor") != 0)
                {
                    REPORT_ERROR1(VLDTR_E_CA_NOTCTOR, tkCAType);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                if ((cbSig > 0) && (pSig != NULL))
                {
                    if(FAILED(ValidateMethodSig(tkCAType, pSig,cbSig,dwFlags))
                        || (!((*pSig) & IMAGE_CEE_CS_CALLCONV_HASTHIS)))
                    {
                        REPORT_ERROR1(VLDTR_E_CA_BADSIG, tkCAType);
                        SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    }
                    else
                    { // sig seems to be OK
                        if ((pbValue != NULL) && (cbValue > 0))
                        {
                            // Check if prolog is OK
                            WORD pW = *((UNALIGNED WORD*)pbValue);
                            if(pW != 0x0001)
                            {
                                REPORT_ERROR1(VLDTR_E_CA_BADPROLOG, pW);
                                SetVldtrCode(&hrSave, VLDTR_S_ERR);
                            }
                            // Check if blob corresponds to the signature
                        }
                    }

                }
                else
                {
                    REPORT_ERROR1(VLDTR_E_CA_NOSIG, tkCAType);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
            } // end if bad Type - else
        } // end if RidFromToken(tkOwner)
    } // end if pRecord

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateCustomAttribute()

//*****************************************************************************
// Validate the given FieldMarshal.
//*****************************************************************************
HRESULT RegMeta::ValidateFieldMarshal(RID rid)
{
    return S_OK;
}   // RegMeta::ValidateFieldMarshal()

//*****************************************************************************
// Validate the given DeclSecurity.
//*****************************************************************************
HRESULT RegMeta::ValidateDeclSecurity(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.
    DeclSecurityRec *pRecord;
    mdToken     tkOwner;                // Owner of the decl security
    DWORD       dwAction;               // action flags
    BOOL        bIsValidOwner = FALSE;

    BEGIN_ENTRYPOINT_NOTHROW;

    IfFailGo(pMiniMd->GetDeclSecurityRecord(rid, &pRecord));
    
    memset(&veCtxt, 0, sizeof(VEContext));
    
    veCtxt.Token = TokenFromRid(rid,mdtPermission);
    veCtxt.uOffset = 0;

    // Must have a valid owner
    tkOwner = pMiniMd->getParentOfDeclSecurity(pRecord);
    if(RidFromToken(tkOwner)==0) goto ErrExit; // deleted record, no need to validate
    switch(TypeFromToken(tkOwner))
    {
        case mdtModule:
        case mdtAssembly:
        case mdtTypeDef:
        case mdtMethodDef:
        case mdtFieldDef:
        case mdtInterfaceImpl:
            bIsValidOwner = IsValidToken(tkOwner);
            break;
        default:
            break;
    }
    if(!bIsValidOwner)
    {
        REPORT_ERROR1(VLDTR_E_DS_BADOWNER, tkOwner);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // Must have one and only one flag set
    dwAction = pRecord->GetAction() & dclActionMask;
    if(dwAction > dclMaximumValue) // the flags are 0,1,2,3,...,dclMaximumValue
    {
        REPORT_ERROR1(VLDTR_E_DS_BADFLAGS, dwAction);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // If field has DeclSecurity, verify its parent is not an interface.-- checked in ValidateField
    // If method has DeclSecurity, verify its parent is not an interface.-- checked in ValidateMethod
    
    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateDeclSecurity()

//*****************************************************************************
// Validate the given ClassLayout.
//*****************************************************************************
HRESULT RegMeta::ValidateClassLayout(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    ClassLayoutRec *pRecord;            // ClassLayout record.
    TypeDefRec  *pTypeDefRec;           // Parent TypeDef record.
    DWORD       dwPackingSize;          // Packing size.
    mdTypeDef   tkParent;               // Parent TypeDef token.
    DWORD       dwTypeDefFlags;         // Parent TypeDef flags.
    RID         clRid;                  // Duplicate ClassLayout rid.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.

    memset(&veCtxt, 0, sizeof(VEContext));

    BEGIN_ENTRYPOINT_NOTHROW;

    // Extract the record.
    veCtxt.Token = rid;
    veCtxt.uOffset = 0;
    IfFailGo(pMiniMd->GetClassLayoutRecord(rid, &pRecord));

    // Get the parent, if parent is nil its a deleted record.  Skip validation.
    tkParent = pMiniMd->getParentOfClassLayout(pRecord);
    if (IsNilToken(tkParent))
        goto ErrExit;

    // Parent should not have AutoLayout set on it.
    IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkParent), &pTypeDefRec));
    dwTypeDefFlags = pMiniMd->getFlagsOfTypeDef(pTypeDefRec);
    if (IsTdAutoLayout(dwTypeDefFlags))
    {
        REPORT_ERROR1(VLDTR_E_CL_TDAUTO, tkParent);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // Parent must not be an Interface
    if(IsTdInterface(dwTypeDefFlags))
    {
        REPORT_ERROR1(VLDTR_E_CL_TDINTF, tkParent);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Validate the PackingSize.
    dwPackingSize = pMiniMd->getPackingSizeOfClassLayout(pRecord);
    if((dwPackingSize > 128)||((dwPackingSize & (dwPackingSize-1)) !=0 ))
    {
        REPORT_ERROR2(VLDTR_E_CL_BADPCKSZ, tkParent, dwPackingSize);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Validate that there are no duplicates.
    hr = _FindClassLayout(pMiniMd, tkParent, &clRid, rid);
    if (hr == S_OK)
    {
        REPORT_ERROR2(VLDTR_E_CL_DUP, tkParent, clRid);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else if (hr == CLDB_E_RECORD_NOTFOUND)
        hr = S_OK;
    else
        IfFailGo(hr);
    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateClassLayout()

//*****************************************************************************
// Validate the given FieldLayout.
//*****************************************************************************
HRESULT RegMeta::ValidateFieldLayout(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    FieldLayoutRec *pRecord;            // FieldLayout record.
    mdFieldDef  tkField;                // Field token.
    ULONG       ulOffset;               // Field offset.
    FieldRec    *pFieldRec;             // Field record.
    TypeDefRec  *pTypeDefRec;           // Parent TypeDef record.
    mdTypeDef   tkTypeDef;              // Parent TypeDef token.
    RID         clRid;                  // Corresponding ClassLayout token.
    RID         flRid = 0;              // Duplicate FieldLayout rid.
    DWORD       dwTypeDefFlags;         // Parent TypeDef flags.
    DWORD       dwFieldFlags;           // Field flags.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));

    // Extract the record.
    veCtxt.Token = rid;
    veCtxt.uOffset = 0;
    IfFailGo(pMiniMd->GetFieldLayoutRecord(rid, &pRecord));

    // Get the field, if it's nil it's a deleted record, so just skip it.
    tkField = pMiniMd->getFieldOfFieldLayout(pRecord);
    if (IsNilToken(tkField))
        goto ErrExit;

    // Validate the Offset value.
    ulOffset = pMiniMd->getOffSetOfFieldLayout(pRecord);
    if (ulOffset == ULONG_MAX)
    {
        REPORT_ERROR2(VLDTR_E_FL_BADOFFSET, tkField, ulOffset);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Get the parent of the Field.
    IfFailGo(pMiniMd->FindParentOfFieldHelper(tkField, &tkTypeDef));
    // Validate that the parent is not nil.
    if (IsNilToken(tkTypeDef))
    {
        REPORT_ERROR1(VLDTR_E_FL_TDNIL, tkField);
        SetVldtrCode(&hr, hrSave);
        goto ErrExit;
    }

    // Validate that there exists a ClassLayout record associated with
    // this TypeDef.
    IfFailGo(pMiniMd->FindClassLayoutHelper(tkTypeDef, &clRid));
    if (InvalidRid(rid))
    {
        REPORT_ERROR2(VLDTR_E_FL_NOCL, tkField, tkTypeDef);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Validate that ExplicitLayout is set on the TypeDef flags.
    IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkTypeDef), &pTypeDefRec));
    dwTypeDefFlags = pMiniMd->getFlagsOfTypeDef(pTypeDefRec);
    if (IsTdAutoLayout(dwTypeDefFlags))
    {
        REPORT_ERROR2(VLDTR_E_FL_TDNOTEXPLCT, tkField, tkTypeDef);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Extract Field record.
    IfFailGo(pMiniMd->GetFieldRecord(RidFromToken(tkField), &pFieldRec));
    // Validate that the field is non-static.
    dwFieldFlags = pMiniMd->getFlagsOfField(pFieldRec);
    if (IsFdStatic(dwFieldFlags))
    {
        REPORT_ERROR1(VLDTR_E_FL_FLDSTATIC, tkField);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    
    // Look for duplicates.
    hr = _FindFieldLayout(pMiniMd, tkField, &flRid, rid);
    if (hr == S_OK)
    {
        REPORT_ERROR1(VLDTR_E_FL_DUP, flRid);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else if (hr == CLDB_E_RECORD_NOTFOUND)
        hr = S_OK;
    else
        IfFailGo(hr);
    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateFieldLayout()

//*****************************************************************************
// Validate the given StandAloneSig.
//*****************************************************************************
HRESULT RegMeta::ValidateStandAloneSig(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    StandAloneSigRec *pRecord;          // FieldLayout record.
    PCCOR_SIGNATURE pbSig;              // Signature.
    ULONG       cbSig;                  // Size in bytes of the signature.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.
    ULONG       ulCurByte = 0;          // Current index into the signature.
    ULONG       ulCallConv;             // Calling convention.
    ULONG       ulArgCount;             // Count of arguments.
    ULONG       ulTyArgCount = 0;       // Count of type arguments.
    ULONG       i;                      // Looping index.
    ULONG       ulNSentinels = 0;       // Number of sentinels in the signature
    BOOL        bNoVoidAllowed=TRUE;

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));

    // Extract the record.
    veCtxt.Token = TokenFromRid(rid,mdtSignature);
    veCtxt.uOffset = 0;
    IfFailGo(pMiniMd->GetStandAloneSigRecord(rid, &pRecord));
    IfFailGo(pMiniMd->getSignatureOfStandAloneSig(pRecord, &pbSig, &cbSig));

    // Validate the signature is well-formed with respect to the compression
    // scheme.  If this fails, no further validation needs to be done.
    if ( (hr = ValidateSigCompression(veCtxt.Token, pbSig, cbSig)) != S_OK)
        goto ErrExit;

    //_ASSERTE((rid != 0x2c2)&&(rid!=0x2c8)&&(rid!=0x2c9)&&(rid!=0x2d6)&&(rid!=0x38b));
    // Validate the calling convention.
    ulCurByte += CorSigUncompressedDataSize(pbSig);
    ulCallConv = CorSigUncompressData(pbSig);
    i = ulCallConv & IMAGE_CEE_CS_CALLCONV_MASK;
    if(i == IMAGE_CEE_CS_CALLCONV_FIELD) // <REVISIT_TODO>it's a temporary bypass (VB bug)</REVISIT_TODO>
        ulArgCount = 1;
    else 
    {
        if(i != IMAGE_CEE_CS_CALLCONV_LOCAL_SIG) // then it is function sig for calli
        {
            if((i >= IMAGE_CEE_CS_CALLCONV_FIELD) 
                ||((ulCallConv & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS)
                &&(!(ulCallConv & IMAGE_CEE_CS_CALLCONV_HASTHIS))))
            {
                REPORT_ERROR1(VLDTR_E_MD_BADCALLINGCONV, ulCallConv);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            bNoVoidAllowed = FALSE;
        }
        // Is there any sig left for arguments?
        _ASSERTE(ulCurByte <= cbSig);
        if (cbSig == ulCurByte)
        {
            REPORT_ERROR1(VLDTR_E_MD_NOARGCNT, ulCurByte+1);
            SetVldtrCode(&hr, hrSave);
            goto ErrExit;
        }

        // Get the type argument count.
        if (ulCallConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
          ulCurByte += CorSigUncompressedDataSize(pbSig);
          ulTyArgCount = CorSigUncompressData(pbSig);
    }

        // Get the argument count.
        ulCurByte += CorSigUncompressedDataSize(pbSig);
        ulArgCount = CorSigUncompressData(pbSig);
    }
    // Validate the the arguments.
    if(ulArgCount)
    {
        for(i=1; ulCurByte < cbSig; i++)
        {
            hr = ValidateOneArg(veCtxt.Token, pbSig, cbSig, &ulCurByte,&ulNSentinels,bNoVoidAllowed);
            if (hr != S_OK)
            {
                if(hr == VLDTR_E_SIG_MISSARG)
                {
                    REPORT_ERROR1(VLDTR_E_SIG_MISSARG, i);
                }
                SetVldtrCode(&hr, hrSave);
                hrSave = hr;
                break;
            }
            bNoVoidAllowed = TRUE; // whatever it was for the 1st arg, it must be TRUE for the rest
        }
        if((ulNSentinels != 0) && (!isCallConv(ulCallConv, IMAGE_CEE_CS_CALLCONV_VARARG )))
        {
            REPORT_ERROR0(VLDTR_E_SIG_SENTMUSTVARARG);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        if(ulNSentinels > 1)
        {
            REPORT_ERROR0(VLDTR_E_SIG_MULTSENTINELS);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateStandAloneSig()

//*****************************************************************************
// Validate the given EventMap.
//*****************************************************************************
HRESULT RegMeta::ValidateEventMap(RID rid)
{
    return S_OK;
}   // RegMeta::ValidateEventMap()

//*****************************************************************************
// Validate the given EventPtr.
//*****************************************************************************
HRESULT RegMeta::ValidateEventPtr(RID rid)
{
    return S_OK;
}   // RegMeta::ValidateEventPtr()

//*****************************************************************************
// Validate the given Event.
//*****************************************************************************
HRESULT RegMeta::ValidateEvent(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.
    mdToken     tkClass;                // Declaring TypeDef
    mdToken     tkEventType;            // Event Type (TypeDef/TypeRef)
    EventRec *pRecord;
    HENUMInternal hEnum;

    BEGIN_ENTRYPOINT_NOTHROW;

    IfFailGo(pMiniMd->GetEventRecord(rid, &pRecord));
    
    memset(&veCtxt, 0, sizeof(VEContext));
    memset(&hEnum, 0, sizeof(HENUMInternal));
    veCtxt.Token = TokenFromRid(rid,mdtEvent);
    veCtxt.uOffset = 0;

    // The scope must be a valid TypeDef
    if (FAILED(pMiniMd->FindParentOfEventHelper(veCtxt.Token, &tkClass)) || 
        (TypeFromToken(tkClass) != mdtTypeDef) || 
        !IsValidToken(tkClass))
    {
        REPORT_ERROR1(VLDTR_E_EV_BADSCOPE, tkClass);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
        tkClass = 0;
    }
    // Must have name
    {
        LPCUTF8 szName;
        IfFailGo(pMiniMd->getNameOfEvent(pRecord, &szName));
        
        if (*szName == 0)
        {
            REPORT_ERROR0(VLDTR_E_EV_NONAME);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else
        {
            if (strcmp(szName, COR_DELETED_NAME_A) == 0)
                goto ErrExit; 
            if (tkClass != 0)    // Must be no duplicates
            {
                RID          ridEventMap;
                EventMapRec *pEventMapRec;
                EventRec    *pRec;
                ULONG        ridStart;
                ULONG        ridEnd;
                ULONG        i;
                
                IfFailGo(pMiniMd->FindEventMapFor(RidFromToken(tkClass), &ridEventMap));
                if (!InvalidRid(ridEventMap))
                {
                    IfFailGo(pMiniMd->GetEventMapRecord(ridEventMap, &pEventMapRec));
                    ridStart = pMiniMd->getEventListOfEventMap(pEventMapRec);
                    IfFailGo(pMiniMd->getEndEventListOfEventMap(ridEventMap, &ridEnd));
                    
                    for (i = ridStart; i < ridEnd; i++)
                    {
                        if (i == rid)
                            continue;
                        IfFailGo(pMiniMd->GetEventRecord(i, &pRec));
                        
                        LPCSTR szEventName;
                        IfFailGo(pMiniMd->getNameOfEvent(pRec, &szEventName));
                        if (strcmp(szName, szEventName) != 0)
                            continue;
                        
                        REPORT_ERROR1(VLDTR_E_EV_DUP, TokenFromRid(i, mdtEvent));
                        SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    }
                }
            }
        }
    }// end of name block
    // EventType must be Nil or valid TypeDef, TypeRef or TypeSpec representing an instantiated generic type
    tkEventType = pMiniMd->getEventTypeOfEvent(pRecord);
    if (!IsNilToken(tkEventType))
    {
        if(IsValidToken(tkEventType) && 
            ((TypeFromToken(tkEventType)==mdtTypeDef)||
             (TypeFromToken(tkEventType)==mdtTypeRef)||
             (TypeFromToken(tkEventType)==mdtTypeSpec)))
        {
            // TypeSpecs can be many things, we only handle instantiated generic types currently.
            if (TypeFromToken(tkEventType)==mdtTypeSpec)
            {
                TypeSpecRec *pRec;
                IfFailGo(pMiniMd->GetTypeSpecRecord(RidFromToken(tkEventType), &pRec));
                PCCOR_SIGNATURE pSig;
                ULONG           cSig;

                IfFailGo(pMiniMd->getSignatureOfTypeSpec(pRec, &pSig, &cSig));
           
                if (CorSigUncompressElementType(pSig) == ELEMENT_TYPE_GENERICINST &&
                    CorSigUncompressElementType(pSig) == ELEMENT_TYPE_CLASS)
                {
                    // Just update the event type token variable and fall through to the validation code below (it doesn't care
                    // whether the type is generic or not).
                    tkEventType = CorSigUncompressToken(pSig);
                }
                else
                {
                    REPORT_ERROR1(VLDTR_E_EV_BADEVTYPE, tkEventType);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
            }

            // EventType must not be Interface or ValueType
            if(TypeFromToken(tkEventType)==mdtTypeDef) // can't say anything about TypeRef: no flags available!
            {
                TypeDefRec *pTypeDefRecord;
                IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkEventType), &pTypeDefRecord));
                DWORD dwFlags = pTypeDefRecord->GetFlags();
                if(!IsTdClass(dwFlags))
                {
                    REPORT_ERROR2(VLDTR_E_EV_EVTYPENOTCLASS, tkEventType, dwFlags);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
            }
        }
        else
        {
            REPORT_ERROR1(VLDTR_E_EV_BADEVTYPE, tkEventType);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    // Validate related methods
    {
        MethodSemanticsRec *pSemantics;
        RID         ridCur;
        ULONG       ulSemantics;
        mdMethodDef tkMethod;
        bool        bHasAddOn = false;
        bool        bHasRemoveOn = false;

        IfFailGo( pMiniMd->FindMethodSemanticsHelper(veCtxt.Token, &hEnum) );
        while (HENUMInternal::EnumNext(&hEnum, (mdToken *)&ridCur))
        {
            IfFailGo(pMiniMd->GetMethodSemanticsRecord(ridCur, &pSemantics));
            ulSemantics = pMiniMd->getSemanticOfMethodSemantics(pSemantics);
            tkMethod = TokenFromRid( pMiniMd->getMethodOfMethodSemantics(pSemantics), mdtMethodDef );
            // Semantics must be Setter, Getter or Other
            switch (ulSemantics)
            {
                case msAddOn:
                    bHasAddOn = true;
                    break;
                case msRemoveOn:
                    bHasRemoveOn = true;
                    break;
                case msFire:
                case msOther:
                    break;
                default:
                    REPORT_ERROR2(VLDTR_E_EV_BADSEMANTICS, tkMethod,ulSemantics);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            // Method must be valid
            if(!IsValidToken(tkMethod))
            {
                REPORT_ERROR1(VLDTR_E_EV_BADMETHOD, tkMethod);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            else
            {
                // Method's parent must be the same
                mdToken tkTypeDef;
                IfFailGo(pMiniMd->FindParentOfMethodHelper(tkMethod, &tkTypeDef));
                if(tkTypeDef != tkClass)
                {
                    REPORT_ERROR2(VLDTR_E_EV_ALIENMETHOD, tkMethod,tkTypeDef);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
            }
        } // end loop over methods
        // AddOn and RemoveOn are a must
        if(!bHasAddOn)
        {
            REPORT_ERROR0(VLDTR_E_EV_NOADDON);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        if(!bHasRemoveOn)
        {
            REPORT_ERROR0(VLDTR_E_EV_NOREMOVEON);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }// end of related method validation block
    
    hr = hrSave;
ErrExit:
    HENUMInternal::ClearEnum(&hEnum);

    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateEvent()


//*****************************************************************************
// Validate the given PropertyMap.
//*****************************************************************************
HRESULT RegMeta::ValidatePropertyMap(RID rid)
{
    return S_OK;
}   // RegMeta::ValidatePropertyMap(0

//*****************************************************************************
// Validate the given PropertyPtr.
//*****************************************************************************
HRESULT RegMeta::ValidatePropertyPtr(RID rid)
{
    return S_OK;
}   // RegMeta::ValidatePropertyPtr()

//*****************************************************************************
// Validate the given Property.
//*****************************************************************************
HRESULT RegMeta::ValidateProperty(RID rid)
{
    CMiniMdRW    *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd for the scope.
    VEContext     veCtxt;                 // Context record.
    HRESULT       hr = S_OK;              // Value returned.
    HRESULT       hrSave = S_OK;          // Save state.
    mdToken       tkClass = mdTokenNil;   // Declaring TypeDef
    PropertyRec  *pRecord;
    HENUMInternal hEnum;
    RID           tempRid;
    
    BEGIN_ENTRYPOINT_NOTHROW;
    
    IfFailGo(pMiniMd->GetPropertyRecord(rid, &pRecord));
    
    memset(&veCtxt, 0, sizeof(VEContext));
    memset(&hEnum, 0, sizeof(HENUMInternal));
    veCtxt.Token = TokenFromRid(rid,mdtProperty);
    veCtxt.uOffset = 0;
    // The scope must be a valid TypeDef
    IfFailGo(pMiniMd->FindParentOfPropertyHelper( veCtxt.Token, &tkClass));
    if ((TypeFromToken(tkClass) != mdtTypeDef) || 
        !IsValidToken(tkClass) || 
        IsNilToken(tkClass))
    {
        REPORT_ERROR1(VLDTR_E_PR_BADSCOPE, tkClass);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // Must have name and signature
    {
        ULONG           cbSig;
        PCCOR_SIGNATURE pvSig;
        IfFailGo(pMiniMd->getTypeOfProperty(pRecord, &pvSig, &cbSig));
        
        LPCUTF8 szName;
        IfFailGo(pMiniMd->getNameOfProperty(pRecord, &szName));
        ULONG ulNameLen = (szName != NULL) ? (ULONG)strlen(szName) : 0;
        
        if (ulNameLen == 0)
        {
            REPORT_ERROR0(VLDTR_E_PR_NONAME);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else
        {
            if(strcmp(szName, COR_DELETED_NAME_A) == 0)
                goto ErrExit; 
        }
        if (cbSig == 0)
        {
            REPORT_ERROR0(VLDTR_E_PR_NOSIG);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        // Must be no duplicates
        if ((ulNameLen != 0) && (cbSig != 0))
        {
            RID             ridPropertyMap;
            PropertyMapRec *pPropertyMapRec;
            PropertyRec    *pRec;
            ULONG           ridStart;
            ULONG           ridEnd;
            ULONG           i;
            ULONG           cbSig1;
            PCCOR_SIGNATURE pvSig1;
            
            IfFailGo(pMiniMd->FindPropertyMapFor(RidFromToken(tkClass), &ridPropertyMap));
            if (!InvalidRid(ridPropertyMap) )
            {
                IfFailGo(pMiniMd->GetPropertyMapRecord(ridPropertyMap, &pPropertyMapRec));
                ridStart = pMiniMd->getPropertyListOfPropertyMap(pPropertyMapRec);
                IfFailGo(pMiniMd->getEndPropertyListOfPropertyMap(ridPropertyMap, &ridEnd));
                
                for (i = ridStart; i < ridEnd; i++)
                {
                    if (i == rid)
                        continue;
                    IfFailGo(pMiniMd->GetPropertyRecord(i, &pRec));
                    IfFailGo(pMiniMd->getTypeOfProperty(pRec, &pvSig1, &cbSig1));
                    
                    if (cbSig != cbSig1)
                        continue;
                    if (memcmp(pvSig,pvSig1,cbSig) != 0)
                        continue;
                    
                    LPCSTR szPropertyName;
                    IfFailGo(pMiniMd->getNameOfProperty(pRec, &szPropertyName));
                    if (strcmp(szName, szPropertyName) != 0)
                        continue;
                    
                    REPORT_ERROR1(VLDTR_E_PR_DUP, TokenFromRid(i,mdtProperty));
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
            }
        }
        // Validate the signature
        if ((pvSig != NULL) && (cbSig != 0))
        {
            ULONG ulCurByte = 0;          // Current index into the signature.
            ULONG ulCallConv;             // Calling convention.
            ULONG ulArgCount;
            ULONG i;
            ULONG ulNSentinels = 0;
            
            // Validate the calling convention.
            ulCurByte += CorSigUncompressedDataSize(pvSig);
            ulCallConv = CorSigUncompressData(pvSig);
            if (!isCallConv(ulCallConv, IMAGE_CEE_CS_CALLCONV_PROPERTY ))
            {
                REPORT_ERROR1(VLDTR_E_PR_BADCALLINGCONV, ulCallConv);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            // Get the argument count.
            ulCurByte += CorSigUncompressedDataSize(pvSig);
            ulArgCount = CorSigUncompressData(pvSig);
            
            // Validate the arguments.
            for (i = 0; i < ulArgCount; i++)
            {
                hr = ValidateOneArg(veCtxt.Token, pvSig, cbSig, &ulCurByte,&ulNSentinels,(i>0));
                if (hr != S_OK)
                {
                    if (hr == VLDTR_E_SIG_MISSARG)
                    {
                        REPORT_ERROR1(VLDTR_E_SIG_MISSARG, i+1);
                    }
                    SetVldtrCode(&hr, hrSave);
                    break;
                }
            }
        }//end if(pvSig && cbSig)
    }// end of name/signature block
    
    // Marked HasDefault <=> has default value
    IfFailGo(pMiniMd->FindConstantHelper(veCtxt.Token, &tempRid));
    if (InvalidRid(tempRid) == IsPrHasDefault(pRecord->GetPropFlags()))
    {
        REPORT_ERROR0(IsPrHasDefault(pRecord->GetPropFlags())? VLDTR_E_PR_MARKEDNODEFLT : VLDTR_E_PR_DEFLTNOTMARKED);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Validate related methods
    {
        MethodSemanticsRec *pSemantics;
        RID         ridCur;
        ULONG       ulSemantics;
        mdMethodDef tkMethod;

        IfFailGo( pMiniMd->FindMethodSemanticsHelper(veCtxt.Token, &hEnum) );
        while (HENUMInternal::EnumNext(&hEnum, (mdToken *) &ridCur))
        {
            IfFailGo(pMiniMd->GetMethodSemanticsRecord(ridCur, &pSemantics));
            ulSemantics = pMiniMd->getSemanticOfMethodSemantics(pSemantics);
            tkMethod = TokenFromRid( pMiniMd->getMethodOfMethodSemantics(pSemantics), mdtMethodDef );
            // Semantics must be Setter, Getter or Other
            switch (ulSemantics)
            {
                case msSetter:
                case msGetter:
                case msOther:
                    break;
                default:
                    REPORT_ERROR2(VLDTR_E_PR_BADSEMANTICS, tkMethod, ulSemantics);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            // Method must be valid
            if(!IsValidToken(tkMethod))
            {
                REPORT_ERROR1(VLDTR_E_PR_BADMETHOD, tkMethod);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            else
            {
                // Method's parent must be the same
                mdToken tkTypeDef;
                IfFailGo(pMiniMd->FindParentOfMethodHelper(tkMethod, &tkTypeDef));
                if(tkTypeDef != tkClass)
                {
                    REPORT_ERROR2(VLDTR_E_PR_ALIENMETHOD, tkMethod, tkTypeDef);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
            }
        } // end loop over methods
    }// end of related method validation block
    
    hr = hrSave;
ErrExit:
    HENUMInternal::ClearEnum(&hEnum);

    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateProperty()

//*****************************************************************************
// Validate the given MethodSemantics.
//*****************************************************************************
HRESULT RegMeta::ValidateMethodSemantics(RID rid)
{
    return S_OK;
}   // RegMeta::ValidateMethodSemantics()

//*****************************************************************************
// Validate the given MethodImpl.
//*****************************************************************************
HRESULT RegMeta::ValidateMethodImpl(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd for the scope.
    MethodImplRec* pRecord;
    MethodImplRec* pRec;
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.
    mdToken     tkClass;                // Declaring TypeDef
    mdToken     tkBody;                 // Implementing method (MethodDef or MemberRef)
    mdToken     tkDecl;                 // Implemented method (MethodDef or MemberRef)
    unsigned    iCount;
    unsigned    index;

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));
    veCtxt.Token = TokenFromRid(rid, mdtMethodImpl);
    veCtxt.uOffset = 0;

    PCCOR_SIGNATURE     pbBodySig = NULL;
    PCCOR_SIGNATURE     pbDeclSig = NULL;

    IfFailGo(pMiniMd->GetMethodImplRecord(rid, &pRecord));
    tkClass = pMiniMd->getClassOfMethodImpl(pRecord);
    // Class must be valid
    if(!IsValidToken(tkClass) || (TypeFromToken(tkClass) != mdtTypeDef))
    {
        REPORT_ERROR1(VLDTR_E_MI_BADCLASS, tkClass);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else
    { // ... and not an Interface
        TypeDefRec *pTypeDefRecord;
        IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkClass), &pTypeDefRecord));
        if(IsTdInterface(pTypeDefRecord->GetFlags()))
        {
            REPORT_ERROR1(VLDTR_E_MI_CLASSISINTF, tkClass);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    // Decl must be valid MethodDef or MemberRef
    tkDecl = pMiniMd->getMethodDeclarationOfMethodImpl(pRecord);
    if(!(IsValidToken(tkDecl) &&
        ((TypeFromToken(tkDecl) == mdtMethodDef) || (TypeFromToken(tkDecl) == mdtMemberRef))))
    {
        REPORT_ERROR1(VLDTR_E_MI_BADDECL, tkDecl);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // Body must be valid MethodDef or MemberRef
    tkBody = pMiniMd->getMethodBodyOfMethodImpl(pRecord);
    if(!(IsValidToken(tkBody) &&
        ((TypeFromToken(tkBody) == mdtMethodDef) || (TypeFromToken(tkBody) == mdtMemberRef))))
    {
        REPORT_ERROR1(VLDTR_E_MI_BADBODY, tkBody);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // No duplicates based on (tkClass,tkDecl)
    iCount = pMiniMd->getCountMethodImpls();
    for(index = rid+1; index <= iCount; index++)
    {
        IfFailGo(pMiniMd->GetMethodImplRecord(index, &pRec));
        if((tkClass == pMiniMd->getClassOfMethodImpl(pRec)) &&
            (tkDecl == pMiniMd->getMethodDeclarationOfMethodImpl(pRec)))
        {
            REPORT_ERROR1(VLDTR_E_MI_DUP, index);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

    mdToken tkBodyParent;
    ULONG               cbBodySig;

    if(TypeFromToken(tkBody) == mdtMethodDef)
    {
        MethodRec *pBodyRec;
        IfFailGo(pMiniMd->GetMethodRecord(RidFromToken(tkBody), &pBodyRec));
        IfFailGo(pMiniMd->getSignatureOfMethod(pBodyRec, &pbBodySig, &cbBodySig));
        IfFailGo(pMiniMd->FindParentOfMethodHelper(tkBody, &tkBodyParent));
        // Body must not be static
        if(IsMdStatic(pBodyRec->GetFlags()))
        {
            REPORT_ERROR1(VLDTR_E_MI_BODYSTATIC, tkBody);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    else if(TypeFromToken(tkBody) == mdtMemberRef)
    {
        MemberRefRec *pBodyRec;
        IfFailGo(pMiniMd->GetMemberRefRecord(RidFromToken(tkBody), &pBodyRec));
        tkBodyParent = pMiniMd->getClassOfMemberRef(pBodyRec);
        IfFailGo(pMiniMd->getSignatureOfMemberRef(pBodyRec, &pbBodySig, &cbBodySig));
    }
    // Body must belong to the same class
    if(tkBodyParent != tkClass)
    {
        REPORT_ERROR1(VLDTR_E_MI_ALIENBODY, tkBodyParent);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    mdToken tkDeclParent;
    ULONG               cbDeclSig;

    if(TypeFromToken(tkDecl) == mdtMethodDef)
    {
        MethodRec *pDeclRec;
        IfFailGo(pMiniMd->GetMethodRecord(RidFromToken(tkDecl), &pDeclRec));
        IfFailGo(pMiniMd->getSignatureOfMethod(pDeclRec, &pbDeclSig, &cbDeclSig));
        IfFailGo(pMiniMd->FindParentOfMethodHelper(tkDecl, &tkDeclParent));
        // Decl must be virtual
        if(!IsMdVirtual(pDeclRec->GetFlags()))
        {
            REPORT_ERROR1(VLDTR_E_MI_DECLNOTVIRT, tkDecl);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        // Decl must not be final
        if(IsMdFinal(pDeclRec->GetFlags()))
        {
            REPORT_ERROR1(VLDTR_E_MI_DECLFINAL, tkDecl);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        // Decl must not be private
        if(IsMdPrivate(pDeclRec->GetFlags()) && IsMdCheckAccessOnOverride(pDeclRec->GetFlags()))
        {
            REPORT_ERROR1(VLDTR_E_MI_DECLPRIV, tkDecl);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    else if(TypeFromToken(tkDecl) == mdtMemberRef)
    {
        MemberRefRec *pDeclRec;
        IfFailGo(pMiniMd->GetMemberRefRecord(RidFromToken(tkDecl), &pDeclRec));
        tkDeclParent = pMiniMd->getClassOfMemberRef(pDeclRec);
        IfFailGo(pMiniMd->getSignatureOfMemberRef(pDeclRec, &pbDeclSig, &cbDeclSig));
    }

    // Compare the signatures as best we can, delegating some comparisons to the loader.
    if (*pbBodySig & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        // decl's callconv must be generic
        if (*pbDeclSig & IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
            // and the arities must match
            ULONG ulBodyArity = CorSigUncompressData(++pbBodySig);
            ULONG ulDeclArity = CorSigUncompressData(++pbDeclSig);
            if(ulBodyArity != ulDeclArity)
            {
                REPORT_ERROR3(VLDTR_E_MI_ARITYMISMATCH,tkDecl,ulDeclArity,ulBodyArity);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
        }
        else
        {
            REPORT_ERROR1(VLDTR_E_MI_DECLNOTGENERIC,tkDecl); 
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        // delegate precise signature checking to the loader, 
        // as this requires signature comparison modulo substitution
    }
    else if (*pbDeclSig & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        REPORT_ERROR1(VLDTR_E_MI_IMPLNOTGENERIC,tkDecl); 
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else if (TypeFromToken(tkDeclParent) == mdtTypeSpec)
    {
        // do nothing for now...
        // delegate precise signature checking to the loader, 
        // as this requires signature comparison modulo substitution
    }
    // Signatures must match (except call conv)
    else if((cbDeclSig != cbBodySig)||(memcmp(pbDeclSig+1,pbBodySig+1,cbDeclSig-1)))
    {
        //@GENERICSVER: todo: 
        /*
          //@TODO: Fix to have peverify resolve assemblies 
          // through the runtime. At that point, use this method instead
          // of the current compare

          // @TODO: check for other bad memcmp sig comparisons in peverify

        // Can't use memcmp because there may be two AssemblyRefs
        // in this scope, pointing to the same assembly, etc.).
        if (!MetaSig::CompareMethodSigs(pbDeclSig,
                                        cbDeclSig,
                                        Module*     pModule1,
                                        pbBodySig,
                                        cbDeclSig,
                                        Module*     pModule2))
        */
        UnifiedAssemblySigComparer uasc(*this);
        MDSigComparer sc(MDSigParser(pbDeclSig, cbDeclSig),
                         MDSigParser(pbBodySig, cbBodySig),
                         uasc);

        hr = sc.CompareMethodSignature();

        if (FAILED(hr))
        {
            REPORT_ERROR2(VLDTR_E_MI_SIGMISMATCH,tkDecl,tkBody);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateMethodImpl()

//*****************************************************************************
// Validate the given ModuleRef.
//*****************************************************************************
HRESULT RegMeta::ValidateModuleRef(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd for the scope.
    ModuleRefRec *pRecord;              // ModuleRef record.
    LPCUTF8     szName;                 // ModuleRef name.
    mdModuleRef tkModuleRef;            // Duplicate ModuleRef.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));
    // Get the ModuleRef record.
    veCtxt.Token = TokenFromRid(rid, mdtModuleRef);
    veCtxt.uOffset = 0;

    IfFailGo(pMiniMd->GetModuleRefRecord(rid, &pRecord));

    // C++ emits IJW methods with ImplMaps
    // which have resolution=ModuleRef with empty name
    IfFailGo(pMiniMd->getNameOfModuleRef(pRecord, &szName));
    if (*szName)
    {
        // Look for a Duplicate, this function reports only one duplicate.
        hr = ImportHelper::FindModuleRef(pMiniMd, szName, &tkModuleRef, rid);
        if (hr == S_OK)
        {
            REPORT_ERROR1(VLDTR_E_MODREF_DUP, tkModuleRef);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else if (hr == CLDB_E_RECORD_NOTFOUND)
            hr = S_OK;
        else
            IfFailGo(hr);
    }
    else
        hrSave = S_FALSE;
    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateModuleRef()

//*****************************************************************************
// Validate the given TypeSpec.
//*****************************************************************************
//@todo GENERICS: reject duplicate specs?
HRESULT RegMeta::ValidateTypeSpec(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    TypeSpecRec *pRecord;               // TypeSpec record.
    PCCOR_SIGNATURE pbSig;              // Signature.
    ULONG       cbSig;                  // Size in bytes of the signature.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.
    ULONG       ulCurByte = 0;          // Current index into the signature.
    ULONG       ulNSentinels = 0;       // Number of sentinels in the signature

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));
    // Extract the record.
    veCtxt.Token = TokenFromRid(rid,mdtTypeSpec);
    veCtxt.uOffset = 0;
    IfFailGo(pMiniMd->GetTypeSpecRecord(rid, &pRecord));
    IfFailGo(pMiniMd->getSignatureOfTypeSpec(pRecord, &pbSig, &cbSig));

    // Validate the signature is well-formed with respect to the compression
    // scheme.  If this fails, no further validation needs to be done.
    if ( (hr = ValidateSigCompression(veCtxt.Token, pbSig, cbSig)) != S_OK)
        goto ErrExit;

    hr = ValidateOneArg(veCtxt.Token, pbSig, cbSig, &ulCurByte,&ulNSentinels,FALSE);
    if (hr != S_OK)
    {
        if(hr == VLDTR_E_SIG_MISSARG)
        {
            REPORT_ERROR0(VLDTR_E_TS_EMPTY);
        }
        SetVldtrCode(&hr, hrSave);
        hrSave = hr;
    }
    if(ulNSentinels != 0)
    {
        REPORT_ERROR0(VLDTR_E_TS_HASSENTINALS);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateTypeSpec()

//*****************************************************************************
// This function validates the given Field signature.  This function works
// with Field signature for both the MemberRef and FieldDef.
//*****************************************************************************
HRESULT RegMeta::ValidateMethodSpecSig(
    mdMethodSpec tk,                     // [IN] Token whose signature needs to be validated.
    PCCOR_SIGNATURE pbSig,              //  [IN] Signature.
    ULONG       cbSig,                  //  [IN] Size in bytes of the signature.
    ULONG       *pArity)                // [Out] Arity of the instantiation
{
    ULONG       ulCurByte = 0;          // Current index into the signature.
    ULONG       ulCallConv;             // Calling convention.
    ULONG       ulArity;                // Arity of instantiation.
    ULONG       ulArgCnt;
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.

    BEGIN_ENTRYPOINT_NOTHROW;
    
    memset(&veCtxt, 0, sizeof(VEContext));
    _ASSERTE(TypeFromToken(tk) == mdtMethodSpec);

    veCtxt.Token = tk;
    veCtxt.uOffset = 0;

    // Validate the calling convention.
    ulCurByte += CorSigUncompressedDataSize(pbSig);
    ulCallConv = CorSigUncompressData(pbSig);
    if (!isCallConv(ulCallConv, IMAGE_CEE_CS_CALLCONV_GENERICINST))
    {
        REPORT_ERROR1(VLDTR_E_MS_BADCALLINGCONV, ulCallConv);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    if (cbSig == ulCurByte)
    {
        REPORT_ERROR1(VLDTR_E_MS_MISSARITY, ulCurByte + 1);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    ulCurByte += CorSigUncompressedDataSize(pbSig);
    ulArity = CorSigUncompressData(pbSig);

    if (ulArity == 0)
    {
        REPORT_ERROR1(VLDTR_E_MS_ARITYZERO, ulCurByte);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    ulArgCnt = ulArity;

    if(pArity != NULL)
    {
        *pArity = ulArity; 
    }

    // Validate and consume the arguments.
    while(ulArgCnt--)
    {

        PCCOR_SIGNATURE pbTypeArg = pbSig;
        ULONG ulTypeArgByte = ulCurByte;

        IfFailGo(ValidateOneArg(tk, pbSig, cbSig, &ulCurByte, NULL, TRUE)); 
        if (hr != S_OK)
        {
            if(hr == VLDTR_E_SIG_MISSARG)
            {
                REPORT_ERROR1(VLDTR_E_MS_MISSARG, ulArity-ulArgCnt);
            }
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
            break;
        }
        // reject byref-like args
        switch (CorSigUncompressData(pbTypeArg))
        {
            case ELEMENT_TYPE_TYPEDBYREF:
            case ELEMENT_TYPE_BYREF:
            {
               REPORT_ERROR1(VLDTR_E_MS_BYREFINST, ulTypeArgByte);
               SetVldtrCode(&hrSave, VLDTR_S_ERR);
               break;
            }
            default: 
              break;
        }
    }

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateMethodSpecSig()


//*****************************************************************************
// Validate the given MethodSpec.
//*****************************************************************************
HRESULT RegMeta::ValidateMethodSpec(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    MethodSpecRec *pRecord;                         // MethodSpec record.
    mdToken     tkMethod;                           // Method field (a MethodDefOrRef)
    PCCOR_SIGNATURE pInstantiation;                 // MethodSpec instantiation (a signature)
    ULONG       cbInstantiation;                    // Size of instantiation.
    ULONG       ulInstantiationArity;               // Arity of the Instantiation

    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));
    // Get the GenericParamConstraint record.
    veCtxt.Token = TokenFromRid(rid, mdtMethodSpec);
    veCtxt.uOffset = 0;
    IfFailGo(pMiniMd->GetMethodSpecRecord(rid, &pRecord));

    // 1. The MethodSpec table may contain zero or more rows.
    // (Nothing to check.)

    // Implicit (missing from  spec): Method is not nil [ERROR]
    tkMethod = pMiniMd->getMethodOfMethodSpec(pRecord);
    if(IsNilToken(tkMethod)) 
    {
        REPORT_ERROR0(VLDTR_E_MS_METHODNIL); 
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Implicit in ValidateRecord: Method is a valid mdMethodDefOrRef.
    
    // 2. One or more rows may refer to the same row in the MethodDef or MethodRef table. 
    //    (There may be more multiple instantions of the same generic method)
    // (nothing to check!)

    // 3. "The signature stored at Instantiation shall be a valid instantiation of the signature of the generic method stored at Method. [ERROR]
    { 
        IfFailGo(pMiniMd->getInstantiationOfMethodSpec(pRecord, &pInstantiation, &cbInstantiation));
        IfFailGo(ValidateMethodSpecSig(TokenFromRid(rid, mdtMethodSpec), pInstantiation, cbInstantiation,&ulInstantiationArity));
        if (hr != S_OK)
            SetVldtrCode(&hrSave, hr);
    }

    IfFailGo(pMiniMd->getInstantiationOfMethodSpec(pRecord, &pInstantiation, &cbInstantiation));
    // 4. There shall be no duplicate rows based upon Method and Instantiation [ERROR]
    {
        mdMethodSpec tkDupMethodSpec;
        hr = ImportHelper::FindMethodSpecByMethodAndInstantiation(pMiniMd, tkMethod, pInstantiation, cbInstantiation, &tkDupMethodSpec, rid);
        if (hr == S_OK)
        {
            REPORT_ERROR1(VLDTR_E_MS_DUP, tkDupMethodSpec); 
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else if (hr == CLDB_E_RECORD_NOTFOUND)
            hr = S_OK;
        else
            IfFailGo(hr);
    }

    // check the method is generic and that the arity of the instantiation is correct
    {
        PCCOR_SIGNATURE pbGenericMethodSig;
        ULONG       cbGenericMethodSig;
        
        if(TypeFromToken(tkMethod) == mdtMethodDef)
        {
           MethodRec *pMethodRec;
           IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(tkMethod), &pMethodRec)); 
           IfFailGo(pMiniMd->getSignatureOfMethod(pMethodRec, &pbGenericMethodSig, &cbGenericMethodSig));
        }
        else
        {
            _ASSERTE(TypeFromToken(tkMethod) == mdtMemberRef);
            MemberRefRec *pMethodRefRec;
            IfFailGo(pMiniMd->GetMemberRefRecord(RidFromToken(tkMethod), &pMethodRefRec));
            IfFailGo(pMiniMd->getSignatureOfMemberRef(pMethodRefRec, &pbGenericMethodSig, &cbGenericMethodSig));
        }
        
        if (*pbGenericMethodSig & IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
            ULONG ulGenericArity = CorSigUncompressData(++pbGenericMethodSig);
            if(ulGenericArity != ulInstantiationArity)
            {
                REPORT_ERROR2(VLDTR_E_MS_ARITYMISMATCH,ulGenericArity,ulInstantiationArity); 
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
        }
        else
        {
            REPORT_ERROR1(VLDTR_E_MS_METHODNOTGENERIC, tkMethod); 
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

    hr = hrSave;

ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}  // RegMeta::ValidateMethodSpec()


//*****************************************************************************
// Validate the given GenericParamConstraint.
//*****************************************************************************
HRESULT RegMeta::ValidateGenericParamConstraint(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd of the scope.
    GenericParamConstraintRec *pRecord;             // GenericParamConstraint record.
    mdGenericParam     tkOwner;                     // GenericParamConstraint owner field.
    mdToken     tkConstraint;                       // GenericParamConstraint constraint field.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));
    // Get the GenericParamConstraint record.
    veCtxt.Token = TokenFromRid(rid, mdtGenericParamConstraint);
    veCtxt.uOffset = 0;
    IfFailGo(pMiniMd->GetGenericParamConstraintRecord(rid, &pRecord));

    // 1. GenericParamConstraint may contain zero or more rows.
    // (Nothing to check.)

    // 2. Each row shall have one, and only one, owner row in the GenericParamTable [ERROR]
    // (Nothing to check except owner not nil)
    tkOwner = pMiniMd->getOwnerOfGenericParamConstraint(pRecord);
    if(IsNilToken(tkOwner)) 
    {
        REPORT_ERROR0(VLDTR_E_GPC_OWNERNIL); 
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    
    // 3. Each row in the GenericParam table shall own a separate row in the GenericParamConstraint table for each constraint that type parameter has [ERROR]
    // (Nothing to check)

    // 4.All of the rows in the GenericParamConstraint table that are owned by a given row in the GenericParamTable
    // shall form a contiguous range of rows [ERROR]
    //@NOTE: this check is (iterated over all rows) is quadratic in the (typically small) number of constraints
    {
        RID curRid = rid;
        GenericParamConstraintRec *pCurRecord;
        mdGenericParam tkCurOwner = tkOwner;
        // find the first preceding row with a distinct owner
        while (curRid > 1 && tkCurOwner == tkOwner) 
        { 
            curRid--;
            IfFailGo(pMiniMd->GetGenericParamConstraintRecord(curRid, &pCurRecord));
            tkCurOwner = pMiniMd->getOwnerOfGenericParamConstraint(pCurRecord);
        };
        // reject this row if there is some row preceding the current row with this owner
        while (curRid > 1) 
        { 
            curRid--;
            IfFailGo(pMiniMd->GetGenericParamConstraintRecord(curRid, &pCurRecord));
            tkCurOwner = pMiniMd->getOwnerOfGenericParamConstraint(pCurRecord);
            if (tkCurOwner == tkOwner) 
            {
                REPORT_ERROR1(VLDTR_E_GPC_NONCONTIGUOUS,tkOwner); 
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
                break;
            }
        };
    }

    // 5. "At most one class constraint per GenericParam" --- no longer required.
    // 6. "Zero or more interface constraints per GenericParam" --- no longer required.
    
    tkConstraint = pMiniMd->getConstraintOfGenericParamConstraint(pRecord);
    // 7. There shall be no duplicates based upon Owner and Constraint
    {
        mdGenericParamConstraint tkDupGenericParamConstraint;
        hr = ImportHelper::FindGenericParamConstraintByOwnerAndConstraint(pMiniMd, tkOwner, tkConstraint, &tkDupGenericParamConstraint, rid);
        if (hr == S_OK)
        {
            REPORT_ERROR1(VLDTR_E_GPC_DUP, tkDupGenericParamConstraint); 
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else if (hr == CLDB_E_RECORD_NOTFOUND)
            hr = S_OK;
        else
            IfFailGo(hr);
    }

    hr = hrSave;

ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateGenericParamConstraint()

//*****************************************************************************
// Validate the given ImplMap.
//*****************************************************************************
HRESULT RegMeta::ValidateImplMap(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd for the scope.
    ImplMapRec  *pRecord;
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.
    HRESULT     hrModuleRef=S_OK;
    mdToken     tkModuleRef;
    mdToken     tkMember;
    USHORT      usFlags;

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));
#ifdef CACHE_IMPLMAP_VALIDATION_RESULT
    for(unsigned jjj=0; jjj<g_nValidated; jjj++) 
    { 
        if(g_rValidated[jjj].tok == (rid | 0x51000000)) return g_rValidated[jjj].hr; 
    }
#endif
    veCtxt.Token = rid;
    veCtxt.uOffset = 0;
    IfFailGo(pMiniMd->GetImplMapRecord(rid, &pRecord));
    if(pRecord == NULL) IfFailGo(E_FAIL);
    // ImplMap must have ModuleRef
    tkModuleRef = pMiniMd->getImportScopeOfImplMap(pRecord);
    if((TypeFromToken(tkModuleRef) != mdtModuleRef) || IsNilToken(tkModuleRef)
        || FAILED(hrModuleRef= ValidateModuleRef(RidFromToken(tkModuleRef))))
    {
        REPORT_ERROR1(VLDTR_E_IMAP_BADMODREF, tkModuleRef);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // ImplMap must belong to FieldDef or MethodDef
    tkMember = pMiniMd->getMemberForwardedOfImplMap(pRecord);
    if((TypeFromToken(tkMember) != mdtFieldDef) && (TypeFromToken(tkMember) != mdtMethodDef))
    {
        REPORT_ERROR1(VLDTR_E_IMAP_BADMEMBER, tkMember);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // ImplMap must have import name, unless ModuleRef has no name
    // (special case for C++ IJW methods)
    if(hrModuleRef != S_FALSE)
    {
        LPCSTR szName;                 // Import name.
        IfFailGo(pMiniMd->getImportNameOfImplMap(pRecord, &szName));
        if((szName==NULL)||(*szName == 0))
        {
            REPORT_ERROR0(VLDTR_E_IMAP_BADIMPORTNAME);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }
    // ImplMap must have valid flags:
        // one value of pmCharSetMask - always so, no check needed (values: 0,2,4,6, mask=6)
        // one value of pmCallConvMask...
        // ...and it's not pmCallConvThiscall
    usFlags = pRecord->GetMappingFlags() & pmCallConvMask;
    if((usFlags < pmCallConvWinapi)||(usFlags > pmCallConvFastcall))
    {
        REPORT_ERROR1(VLDTR_E_IMAP_BADCALLCONV, usFlags);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
ErrExit:

#ifdef CACHE_IMPLMAP_VALIDATION_RESULT
    g_rValidated[g_nValidated].tok = rid | 0x51000000;
    g_rValidated[g_nValidated].hr = hrSave;
    g_nValidated++;
#endif
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateImplMap()

//*****************************************************************************
// Validate the given FieldRVA.
//*****************************************************************************
HRESULT RegMeta::ValidateFieldRVA(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd for the scope.
    FieldRVARec  *pRecord;
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.
    mdToken     tkField;
    ULONG       ulRVA;

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));
    veCtxt.Token = rid;
    veCtxt.uOffset = 0;
    IfFailGo(pMiniMd->GetFieldRVARecord(rid, &pRecord));
    ulRVA = pRecord->GetRVA();
    tkField = pMiniMd->getFieldOfFieldRVA(pRecord);
    /*
    if(ulRVA == 0)
    {
        REPORT_ERROR1(VLDTR_E_FRVA_ZERORVA, tkField);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    */
    if((0==RidFromToken(tkField))||(TypeFromToken(tkField) != mdtFieldDef)||(!IsValidToken(tkField)))
    {
        REPORT_ERROR2(VLDTR_E_FRVA_BADFIELD, tkField, ulRVA);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    {
        RID N = pMiniMd->getCountFieldRVAs();
        RID tmp;
        FieldRVARec* pRecTmp;
        for(tmp = rid+1; tmp <= N; tmp++)
        { 
            IfFailGo(pMiniMd->GetFieldRVARecord(tmp, &pRecTmp));
            if(tkField == pMiniMd->getFieldOfFieldRVA(pRecTmp))
            {
                REPORT_ERROR2(VLDTR_E_FRVA_DUPFIELD, tkField, tmp);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
        }
    }
    
    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateFieldRVA()

//*****************************************************************************
// Validate the given ENCLog.
//*****************************************************************************
HRESULT RegMeta::ValidateENCLog(RID rid)
{
    return S_OK;
}   // RegMeta::ValidateENCLog()

//*****************************************************************************
// Validate the given ENCMap.
//*****************************************************************************
HRESULT RegMeta::ValidateENCMap(RID rid)
{
    return S_OK;
}   // RegMeta::ValidateENCMap()

//*****************************************************************************
// Validate the given Assembly.
//*****************************************************************************
HRESULT RegMeta::ValidateAssembly(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd for the scope.
    AssemblyRec *pRecord;           // Assembly record.
    CorAssemblyFlags   dwFlags;     // Assembly flags.
    LPCSTR      szName;             // Assembly Name.
    VEContext   veCtxt;             // Context structure.
    HRESULT     hr = S_OK;          // Value returned.
    HRESULT     hrSave = S_OK;      // Save state.
    BOOL        invalidAssemblyFlags;  // Whether the CorAssemblyFlags are valid.
    BOOL        fIsV2Assembly = FALSE;

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));
    // Get the Assembly record.
    veCtxt.Token = TokenFromRid(rid, mdtAssembly);
    veCtxt.uOffset = 0;

    IfFailGo(pMiniMd->GetAssemblyRecord(rid, &pRecord));

    // There can only be one Assembly record.
    if (rid > 1)
    {
        REPORT_ERROR0(VLDTR_E_AS_MULTI);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Do checks for name validity..
    IfFailGo(pMiniMd->getNameOfAssembly(pRecord, &szName));
    if (!*szName)
    {
        // Assembly Name is null.
        REPORT_ERROR0(VLDTR_E_AS_NAMENULL);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else
    {
        unsigned L = (unsigned)strlen(szName);
        if((*szName==' ')||strchr(szName,':') || strchr(szName,'\\') || strchr(szName, '/') 
            || strchr(szName, ',') || strchr(szName, '\n') || strchr(szName, '\r')
            || ((L > 4)&&((!SString::_stricmp(&szName[L-4],".exe"))||(!SString::_stricmp(&szName[L-4],".dll")))))
        {
            //Assembly name has path and/or extension
            REPORT_ERROR0(VLDTR_E_AS_BADNAME);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

    // Get the flags value for the Assembly.
    dwFlags = (CorAssemblyFlags) pMiniMd->getFlagsOfAssembly(pRecord);

    // Validate the flags 
    invalidAssemblyFlags = dwFlags & (~(afPublicKey | afRetargetable | afPA_FullMask | afEnableJITcompileTracking | afDisableJITcompileOptimizer | afContentType_Mask));

    // Validate we only set a legal processor architecture flags
    // The processor architecture flags were introduced in CLR v2.0.
    // Note that METAMODEL_MINOR_VER_V2_0 is 0.  GCC points out the comparison 
    // is useless, so that part is commented out.
    fIsV2Assembly = (m_pStgdb->m_MiniMd.m_Schema.m_major >= METAMODEL_MAJOR_VER_V2_0
                     /* && m_pStgdb->m_MiniMd.m_Schema.m_minor >= METAMODEL_MINOR_VER_V2_0*/);
    if (fIsV2Assembly)
    {
        if ((dwFlags & afPA_Mask) > afPA_AMD64 && !IsAfPA_NoPlatform(dwFlags))
            invalidAssemblyFlags = true;
    }
    else {
        if ((dwFlags & afPA_Mask) != 0)
            invalidAssemblyFlags = true;
    }

    if (!IsAfContentType_Default(dwFlags) && !IsAfContentType_WindowsRuntime(dwFlags))
    {   // Unknown ContentType value
        invalidAssemblyFlags = true;
    }

    if (invalidAssemblyFlags)
    {
        REPORT_ERROR1(VLDTR_E_AS_BADFLAGS, dwFlags);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Validate hash algorithm ID
    switch(pRecord->GetHashAlgId())
    {
        case CALG_MD2:
        case CALG_MD4:
        case CALG_MD5:
        case CALG_SHA:
        //case CALG_SHA1: // same as CALG_SHA
        case CALG_MAC:
        case CALG_SSL3_SHAMD5:
        case CALG_HMAC:
        case 0:
            break;
        default:
            REPORT_ERROR1(VLDTR_E_AS_HASHALGID, pRecord->GetHashAlgId());
            SetVldtrCode(&hrSave, VLDTR_S_WRN);
    }
    // Validate locale
    {
        LPCSTR szLocale;
        IfFailGo(pMiniMd->getLocaleOfAssembly(pRecord, &szLocale));
        if(!_IsValidLocale(szLocale, fIsV2Assembly))
        {
            REPORT_ERROR0(VLDTR_E_AS_BADLOCALE);
            SetVldtrCode(&hrSave, VLDTR_S_WRN);
        }
    }

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateAssembly()

//*****************************************************************************
// Validate the given AssemblyProcessor.
//*****************************************************************************
HRESULT RegMeta::ValidateAssemblyProcessor(RID rid)
{
    return S_OK;
}   // RegMeta::ValidateAssemblyProcessor()

//*****************************************************************************
// Validate the given AssemblyOS.
//*****************************************************************************
HRESULT RegMeta::ValidateAssemblyOS(RID rid)
{
    return S_OK;
}   // RegMeta::ValidateAssemblyOS()

//*****************************************************************************
// Validate the given AssemblyRef.
//*****************************************************************************
HRESULT RegMeta::ValidateAssemblyRef(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd for the scope.
    AssemblyRefRec *pRecord;        // Assembly record.
    LPCSTR      szName;             // AssemblyRef Name.
    VEContext   veCtxt;             // Context structure.
    HRESULT     hr = S_OK;          // Value returned.
    HRESULT     hrSave = S_OK;      // Save state.

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));
    veCtxt.Token = TokenFromRid(rid, mdtAssemblyRef);
    veCtxt.uOffset = 0;

    // Get the AssemblyRef record.
    IfFailGo(pMiniMd->GetAssemblyRefRecord(rid, &pRecord));

    // Do checks for name and alias validity.
    IfFailGo(pMiniMd->getNameOfAssemblyRef(pRecord, &szName));
    if (!*szName)
    {
        // AssemblyRef Name is null.
        REPORT_ERROR0(VLDTR_E_AR_NAMENULL);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else
    {
        unsigned L = (unsigned)strlen(szName);
        if((*szName==' ')||strchr(szName,':') || strchr(szName,'\\') || strchr(szName, '/') 
            || strchr(szName, ',') || strchr(szName, '\n') || strchr(szName, '\r')
            || ((L > 4)&&((!SString::_stricmp(&szName[L-4],".exe"))||(!SString::_stricmp(&szName[L-4],".dll")))))
        {
            //Assembly name has path and/or extension
            REPORT_ERROR0(VLDTR_E_AS_BADNAME);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

    // Validate locale
    {
        LPCSTR szLocale;
        IfFailGo(pMiniMd->getLocaleOfAssemblyRef(pRecord, &szLocale));
        BOOL fIsV2Assembly = (m_pStgdb->m_MiniMd.m_Schema.m_major >= METAMODEL_MAJOR_VER_V2_0
                              /* && m_pStgdb->m_MiniMd.m_Schema.m_minor >= METAMODEL_MINOR_VER_V2_0*/);
        if(!_IsValidLocale(szLocale, fIsV2Assembly))
        {
            REPORT_ERROR0(VLDTR_E_AS_BADLOCALE);
            SetVldtrCode(&hrSave, VLDTR_S_WRN);
        }
    }

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateAssemblyRef()

//*****************************************************************************
// Validate the given AssemblyRefProcessor.
//*****************************************************************************
HRESULT RegMeta::ValidateAssemblyRefProcessor(RID rid)
{
    return S_OK;
}   // RegMeta::ValidateAssemblyRefProcessor()

//*****************************************************************************
// Validate the given AssemblyRefOS.
//*****************************************************************************
HRESULT RegMeta::ValidateAssemblyRefOS(RID rid)
{
    return S_OK;
}   // RegMeta::ValidateAssemblyRefOS()

//*****************************************************************************
// Validate the given File.
//*****************************************************************************
HRESULT RegMeta::ValidateFile(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd for the scope.
    FileRec     *pRecord;           // File record.
    mdFile      tkFile;             // Duplicate File token.
    LPCSTR      szName;             // File Name.
    VEContext   veCtxt;             // Context structure.
    HRESULT     hr = S_OK;          // Value returned.
    HRESULT     hrSave = S_OK;      // Save state.

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));
    veCtxt.Token = TokenFromRid(rid, mdtFile);
    veCtxt.uOffset = 0;

    // Get the File record.
    IfFailGo(pMiniMd->GetFileRecord(rid, &pRecord));

    // Do checks for name validity.
    IfFailGo(pMiniMd->getNameOfFile(pRecord, &szName));
    if (!*szName)
    {
        // File Name is null.
        REPORT_ERROR0(VLDTR_E_FILE_NAMENULL);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else
    {
        ULONG L = (ULONG)strlen(szName);
        if(L >= MAX_PATH_FNAME)
        {
            // Name too long
            REPORT_ERROR2(VLDTR_E_TD_NAMETOOLONG, L, (ULONG)(MAX_PATH_FNAME-1));
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        // Check for duplicates based on Name.
        hr = ImportHelper::FindFile(pMiniMd, szName, &tkFile, rid);
        if (hr == S_OK)
        {
            REPORT_ERROR1(VLDTR_E_FILE_DUP, tkFile);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else if (hr == CLDB_E_RECORD_NOTFOUND)
            hr = S_OK;
        else
            IfFailGo(hr);

        // File name must not be fully qualified.
        if(strchr(szName,':') || strchr(szName,'\\') || strchr(szName,'/'))
        {
            REPORT_ERROR0(VLDTR_E_FILE_NAMEFULLQLFD);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }

        // File name must not be one of system names.
        char *sysname[6]={"con","aux","lpt","prn","null","com"};
        char *syssymbol = "0123456789$:";
        for(unsigned i=0; i<6; i++)
        {
            L = (ULONG)strlen(sysname[i]);
            if(!SString::_strnicmp(szName,sysname[i],L))
            {
                if((szName[L]==0)|| strchr(syssymbol,szName[L]))
                {
                    REPORT_ERROR0(VLDTR_E_FILE_SYSNAME);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    break;
                }
            }
        }
    }

    if (pRecord->GetFlags() & (~0x00000003))
    {
        REPORT_ERROR1(VLDTR_E_FILE_BADFLAGS, pRecord->GetFlags());
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Validate hash value
    {
        const BYTE *pbHashValue = NULL;
        ULONG       cbHashValue;
        IfFailGo(m_pStgdb->m_MiniMd.getHashValueOfFile(pRecord, &pbHashValue, &cbHashValue));
        if ((pbHashValue == NULL) || (cbHashValue == 0))
        {
            REPORT_ERROR0(VLDTR_E_FILE_NULLHASH);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

    // Validate that the name is not the same as the file containing
    // the manifest.

    // File name must be a valid file name.

    // Each ModuleRef in the assembly must have a corresponding File table entry.

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateFile()

//*****************************************************************************
// Validate the given ExportedType.
//*****************************************************************************
HRESULT RegMeta::ValidateExportedType(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd for the scope.
    ExportedTypeRec  *pRecord;           // ExportedType record.
    mdExportedType   tkExportedType;          // Duplicate ExportedType.
    mdToken     tkImpl;             // Implementation token
    mdToken     tkTypeDef;          // TypeDef token

    LPCSTR      szName;             // ExportedType Name.
    LPCSTR      szNamespace;        // ExportedType Namespace.
    VEContext   veCtxt;             // Context structure.
    HRESULT     hr = S_OK;          // Value returned.
    HRESULT     hrSave = S_OK;      // Save state.

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));
    veCtxt.Token = TokenFromRid(rid, mdtExportedType);
    veCtxt.uOffset = 0;

    // Get the ExportedType record.
    IfFailGo(pMiniMd->GetExportedTypeRecord(rid, &pRecord));

    tkImpl = pMiniMd->getImplementationOfExportedType(pRecord);
    
    tkTypeDef = pRecord->GetTypeDefId();
    if ((TypeFromToken(tkImpl) == mdtFile) && IsNilToken(tkTypeDef))
    {   // Report 'No TypeDefId' warning only for types exported from other modules (do not report it for 
        // type forwarders)
        REPORT_ERROR0(VLDTR_E_CT_NOTYPEDEFID);
        SetVldtrCode(&hrSave, VLDTR_S_WRN);
    }
    
    // Do checks for name validity.
    IfFailGo(pMiniMd->getTypeNameOfExportedType(pRecord, &szName));
    IfFailGo(pMiniMd->getTypeNamespaceOfExportedType(pRecord, &szNamespace));
    if (!*szName)
    {
        // ExportedType Name is null.
        REPORT_ERROR0(VLDTR_E_CT_NAMENULL);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else
    {
        if(!strcmp(szName,COR_DELETED_NAME_A)) goto ErrExit; 
        ULONG L = (ULONG)(strlen(szName)+strlen(szNamespace));
        if(L >= MAX_CLASSNAME_LENGTH)
        {
            // Name too long
            REPORT_ERROR2(VLDTR_E_TD_NAMETOOLONG, L, (ULONG)(MAX_CLASSNAME_LENGTH-1));
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        // Check for duplicates based on Name and Enclosing ExportedType.
        hr = ImportHelper::FindExportedType(pMiniMd, szNamespace, szName, tkImpl, &tkExportedType, rid);
        if (hr == S_OK)
        {
            REPORT_ERROR1(VLDTR_E_CT_DUP, tkExportedType);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else if (hr == CLDB_E_RECORD_NOTFOUND)
            hr = S_OK;
        else
            IfFailGo(hr);
        // Check for duplicate TypeDef based on Name/NameSpace - only for top-level ExportedTypes.
        if(TypeFromToken(tkImpl)==mdtFile)
        {
            mdToken tkTypeDef2;
            hr = ImportHelper::FindTypeDefByName(pMiniMd, szNamespace, szName, mdTypeDefNil,
                                             &tkTypeDef2, 0);
            if (hr == S_OK)
            {
                REPORT_ERROR1(VLDTR_E_CT_DUPTDNAME, tkTypeDef2);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            else if (hr == CLDB_E_RECORD_NOTFOUND)
                hr = S_OK;
            else
                IfFailGo(hr);
        }
    }
    // Check if flag value is valid
    {
        DWORD dwFlags = pRecord->GetFlags();
        DWORD dwInvalidMask, dwExtraBits;
        dwInvalidMask = (DWORD)~(tdVisibilityMask | tdLayoutMask | tdClassSemanticsMask | 
                tdAbstract | tdSealed | tdSpecialName | tdImport | tdSerializable | tdForwarder |
                tdStringFormatMask | tdBeforeFieldInit | tdReservedMask);
        // check for extra bits
        dwExtraBits = dwFlags & dwInvalidMask;
        if(!dwExtraBits)
        {
            // if no extra bits, check layout
            dwExtraBits = dwFlags & tdLayoutMask;
            if(dwExtraBits != tdLayoutMask)
            {
                // layout OK, check string format
                dwExtraBits = dwFlags & tdStringFormatMask;
                if(dwExtraBits != tdStringFormatMask) dwExtraBits = 0;
            }
        }
        if(dwExtraBits)
        {
            REPORT_ERROR1(VLDTR_E_TD_EXTRAFLAGS, dwExtraBits);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

    if(IsNilToken(tkImpl)
        || ((TypeFromToken(tkImpl) != mdtFile)&&(TypeFromToken(tkImpl) != mdtExportedType)&&(TypeFromToken(tkImpl) != mdtAssemblyRef))
        || (!IsValidToken(tkImpl)))
    {
        REPORT_ERROR1(VLDTR_E_CT_BADIMPL, tkImpl);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateExportedType()

//*****************************************************************************
// Validate the given ManifestResource.
//*****************************************************************************
HRESULT RegMeta::ValidateManifestResource(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd for the scope.
    ManifestResourceRec  *pRecord;  // ManifestResource record.
    LPCSTR      szName;             // ManifestResource Name.
    DWORD       dwFlags;            // ManifestResource flags.
    mdManifestResource tkmar;       // Duplicate ManifestResource.
    VEContext   veCtxt;             // Context structure.
    HRESULT     hr = S_OK;          // Value returned.
    HRESULT     hrSave = S_OK;      // Save state.
    mdToken     tkImplementation;
    BOOL        bIsValidImplementation = TRUE;

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));
    veCtxt.Token = TokenFromRid(rid, mdtManifestResource);
    veCtxt.uOffset = 0;

    // Get the ManifestResource record.
    IfFailGo(pMiniMd->GetManifestResourceRecord(rid, &pRecord));

    // Do checks for name validity.
    IfFailGo(pMiniMd->getNameOfManifestResource(pRecord, &szName));
    if (!*szName)
    {
        // ManifestResource Name is null.
        REPORT_ERROR0(VLDTR_E_MAR_NAMENULL);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    else
    {
        // Check for duplicates based on Name.
        hr = ImportHelper::FindManifestResource(pMiniMd, szName, &tkmar, rid);
        if (hr == S_OK)
        {
            REPORT_ERROR1(VLDTR_E_MAR_DUP, tkmar);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        else if (hr == CLDB_E_RECORD_NOTFOUND)
            hr = S_OK;
        else
            IfFailGo(hr);
    }

    // Get the flags of the ManifestResource.
    dwFlags = pMiniMd->getFlagsOfManifestResource(pRecord);
    if(dwFlags &(~0x00000003))
    {
            REPORT_ERROR1(VLDTR_E_MAR_BADFLAGS, dwFlags);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Visibility of ManifestResource flags must either be public or private.
    if (!IsMrPublic(dwFlags) && !IsMrPrivate(dwFlags))
    {
        REPORT_ERROR0(VLDTR_E_MAR_NOTPUBPRIV);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Implementation must be Nil or valid AssemblyRef or File
    tkImplementation = pMiniMd->getImplementationOfManifestResource(pRecord);
    if(!IsNilToken(tkImplementation))
    {
        switch(TypeFromToken(tkImplementation))
        {
            case mdtAssemblyRef:
                bIsValidImplementation = IsValidToken(tkImplementation);
                break;
            case mdtFile:
                if((bIsValidImplementation = IsValidToken(tkImplementation)))
                {   // if file not PE, offset must be 0
                    FileRec *pFR;
                    IfFailGo(pMiniMd->GetFileRecord(RidFromToken(tkImplementation), &pFR));
                    if(IsFfContainsNoMetaData(pFR->GetFlags()) 
                        && pRecord->GetOffset())
                    {
                        REPORT_ERROR1(VLDTR_E_MAR_BADOFFSET, tkImplementation);
                        SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    }
                }
                break;
            default:
                bIsValidImplementation = FALSE;
        }
    }
    if(!bIsValidImplementation)
    {
        REPORT_ERROR1(VLDTR_E_MAR_BADIMPL, tkImplementation);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Validate the Offset into the PE file.

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateManifestResource()

//*****************************************************************************
// Validate the given NestedClass.
//*****************************************************************************
HRESULT RegMeta::ValidateNestedClass(RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);   // MiniMd for the scope.
    NestedClassRec  *pRecord;  // NestedClass record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save the current state.
    VEContext   veCtxt;             // Context structure.
    mdToken     tkNested;
    mdToken     tkEncloser;

    BEGIN_ENTRYPOINT_NOTHROW;

    memset(&veCtxt, 0, sizeof(VEContext));
    veCtxt.Token = rid;
    veCtxt.uOffset = 0;

    // Get the NestedClass record.
    IfFailGo(pMiniMd->GetNestedClassRecord(rid, &pRecord));
    tkNested = pMiniMd->getNestedClassOfNestedClass(pRecord);
    tkEncloser = pMiniMd->getEnclosingClassOfNestedClass(pRecord);

    // Nested must be valid TypeDef
    if((TypeFromToken(tkNested) != mdtTypeDef) || !IsValidToken(tkNested))
    {
        REPORT_ERROR1(VLDTR_E_NC_BADNESTED, tkNested);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // Encloser must be valid TypeDef
    if((TypeFromToken(tkEncloser) != mdtTypeDef) || !IsValidToken(tkEncloser))
    {
        REPORT_ERROR1(VLDTR_E_NC_BADENCLOSER, tkEncloser);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    // Check for duplicates 
    {
        RID N = pMiniMd->getCountNestedClasss();
        RID tmp;
        NestedClassRec* pRecTmp;
        mdToken tkEncloserTmp;
        for(tmp = rid+1; tmp <= N; tmp++)
        { 
            IfFailGo(pMiniMd->GetNestedClassRecord(tmp, &pRecTmp));
            if(tkNested == pMiniMd->getNestedClassOfNestedClass(pRecTmp))
            {
                if(tkEncloser == (tkEncloserTmp = pMiniMd->getEnclosingClassOfNestedClass(pRecTmp)))
                {
                    REPORT_ERROR1(VLDTR_E_NC_DUP, tmp);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                else
                {
                    REPORT_ERROR3(VLDTR_E_NC_DUPENCLOSER, tkNested, tkEncloser, tkEncloserTmp);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
            }
        }
    }

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateLocalVariable()

//*****************************************************************************
// Given a Table ID and a Row ID, validate all the columns contain meaningful
// values given the column definitions.  Validate that the offsets into the
// different pools are valid, the rids are within range and the coded tokens
// are valid.  Every failure here is considered an error.
//*****************************************************************************
HRESULT RegMeta::ValidateRecord(ULONG ixTbl, RID rid)
{
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save the current state.
    ULONG       ulCount;                // Count of records in the table.
    ULONG       ulRawColVal;            // Raw value of the column.
    void        *pRow;                  // Row with the data.
    CMiniTableDef *pTbl;                // Table definition.
    CMiniColDef *pCol;                  // Column definition.
    const CCodedTokenDef *pCdTkn;       // Coded token definition.
    ULONG       ix;                     // Index into the array of coded tokens.

    BEGIN_ENTRYPOINT_NOTHROW;

    // Get the table definition.
    pTbl = &pMiniMd->m_TableDefs[ixTbl];

    // Get the row.  We may assume that the Row pointer we get back from
    // this call is correct since we do the verification on the Record
    // pools for each table during the open sequence.  The only place
    // this is not valid is for Dynamic IL and we don't do this
    // verification in that case since we go through IMetaData* APIs
    // in that case and it should all be consistent.
    IfFailGo(m_pStgdb->m_MiniMd.getRow(ixTbl, rid, &pRow));

    for (ULONG ixCol = 0; ixCol < pTbl->m_cCols; ixCol++)
    {
        // Get the column definition.
        pCol = &pTbl->m_pColDefs[ixCol];

        // Get the raw value stored in the column.  getIX currently doesn't
        // handle byte sized fields, but there are some BYTE fields in the
        // MetaData.  So using the conditional to access BYTE fields.
        if (pCol->m_cbColumn == 1)
            ulRawColVal = pMiniMd->getI1(pRow, *pCol);
        else
            ulRawColVal = pMiniMd->getIX(pRow, *pCol);

        // Do some basic checks on the non-absurdity of the value stored in the
        // column.
        if (IsRidType(pCol->m_Type))
        {
            // Verify that the RID is within range.
            _ASSERTE(pCol->m_Type < pMiniMd->GetCountTables());
            ulCount = pMiniMd->GetCountRecs(pCol->m_Type);
            // For records storing rids to pointer tables, the stored value may
            // be one beyond the last record.
            if (IsTblPtr(pCol->m_Type, ixTbl))
                ulCount++;
            if (ulRawColVal > ulCount)
            {
                VEContext   veCtxt;
                memset(&veCtxt, 0, sizeof(VEContext));
                veCtxt.Token    = 0;
                veCtxt.uOffset  = 0;
                REPORT_ERROR3(VLDTR_E_RID_OUTOFRANGE, ixTbl, ixCol, rid);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
        }
        else if (IsCodedTokenType(pCol->m_Type))
        {
            // Verify that the Coded token and rid are valid.
            pCdTkn = &g_CodedTokens[pCol->m_Type - iCodedToken];
            ix = ulRawColVal & ~(-1 << CMiniMdRW::m_cb[pCdTkn->m_cTokens]);
            if (ix >= pCdTkn->m_cTokens)
            {
                VEContext   veCtxt;
                memset(&veCtxt, 0, sizeof(VEContext));
                veCtxt.Token    = 0;
                veCtxt.uOffset  = 0;
                REPORT_ERROR3(VLDTR_E_CDTKN_OUTOFRANGE, ixTbl, ixCol, rid);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            ulCount = pMiniMd->GetCountRecs(TypeFromToken(pCdTkn->m_pTokens[ix]) >> 24);
            if ( (ulRawColVal >> CMiniMdRW::m_cb[pCdTkn->m_cTokens]) > ulCount)
            {
                VEContext   veCtxt;
                memset(&veCtxt, 0, sizeof(VEContext));
                veCtxt.Token    = 0;
                veCtxt.uOffset  = 0;
                REPORT_ERROR3(VLDTR_E_CDRID_OUTOFRANGE, ixTbl, ixCol, rid);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
        }
        else if (IsHeapType(pCol->m_Type))
        {
            // Verify that the offsets for the Heap type fields are valid offsets
            // into the heaps.
            switch (pCol->m_Type)
            {
            case iSTRING:
                if (!pMiniMd->m_StringHeap.IsValidIndex(ulRawColVal))
                {
                    VEContext   veCtxt;
                    memset(&veCtxt, 0, sizeof(VEContext));
                    veCtxt.Token    = 0;
                    veCtxt.uOffset  = 0;
                    REPORT_ERROR3(VLDTR_E_STRING_INVALID, ixTbl, ixCol, rid);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                break;
            case iGUID:
                if (ulRawColVal == 0)
                {   // GUID value 0 is valid value, though it's invalid GUID heap index
                    break;
                }
                if (!pMiniMd->m_GuidHeap.IsValidIndex(ulRawColVal))
                {
                    VEContext   veCtxt;
                    memset(&veCtxt, 0, sizeof(VEContext));
                    veCtxt.Token    = 0;
                    veCtxt.uOffset  = 0;
                    REPORT_ERROR3(VLDTR_E_GUID_INVALID, ixTbl, ixCol, rid);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                break;
            case iBLOB:
                if (! pMiniMd->m_BlobHeap.IsValidIndex(ulRawColVal))
                {
                    VEContext   veCtxt;
                    memset(&veCtxt, 0, sizeof(VEContext));
                    veCtxt.Token    = 0;
                    veCtxt.uOffset  = 0;
                    REPORT_ERROR3(VLDTR_E_BLOB_INVALID, ixTbl, ixCol, rid);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                break;
            default:
                _ASSERTE(!"Invalid heap type encountered!");
            }
        }
        else
        {
            // Not much checking that can be done on the fixed type in a generic sense.
            _ASSERTE (IsFixedType(pCol->m_Type));
        }
        hr = hrSave;
    }
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateRecord()

//*****************************************************************************
// This function validates that the given Method signature is consistent as per
// the compression scheme.
//*****************************************************************************
HRESULT RegMeta::ValidateSigCompression(
    mdToken     tk,                     // [IN] Token whose signature needs to be validated.
    PCCOR_SIGNATURE pbSig,              // [IN] Signature.
    ULONG       cbSig)                  // [IN] Size in bytes of the signature.
{
    VEContext   veCtxt;                 // Context record.
    ULONG       ulCurByte = 0;          // Current index into the signature.
    ULONG       ulSize;                 // Size of uncompressed data at each point.
    HRESULT     hr = S_OK;              // Value returned.

    memset(&veCtxt, 0, sizeof(VEContext));
    veCtxt.Token = tk;
    veCtxt.uOffset = 0;

    // Check for NULL signature.
    if (!cbSig)
    {
        REPORT_ERROR0(VLDTR_E_SIGNULL);
        SetVldtrCode(&hr, VLDTR_S_ERR);
        goto ErrExit;
    }

    // Walk through the signature.  At each point make sure there is enough
    // room left in the signature based on the encoding in the current byte.
    while (cbSig - ulCurByte)
    {
        _ASSERTE(ulCurByte <= cbSig);
        // Get next chunk of uncompressed data size.
        if ((ulSize = CorSigUncompressedDataSize(pbSig)) > (cbSig - ulCurByte))
        {
            REPORT_ERROR1(VLDTR_E_SIGNODATA, ulCurByte+1);
            SetVldtrCode(&hr, VLDTR_S_ERR);
            goto ErrExit;
        }
        // Go past this chunk.
        ulCurByte += ulSize;
        CorSigUncompressData(pbSig);
    }
ErrExit:

    return hr;
}   // RegMeta::ValidateSigCompression()

//*****************************************************************************
// This function validates one argument given an offset into the signature
// where the argument begins.  This function assumes that the signature is well
// formed as far as the compression scheme is concerned.
//*****************************************************************************
//@GENERICS: todo: reject uninstantiated generic types used as types.
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
HRESULT RegMeta::ValidateOneArg(
    mdToken     tk,                     // [IN] Token whose signature is being processed.
    PCCOR_SIGNATURE &pbSig,             // [IN] Pointer to the beginning of argument.
    ULONG       cbSig,                  // [IN] Size in bytes of the full signature.
    ULONG       *pulCurByte,            // [IN/OUT] Current offset into the signature..
    ULONG       *pulNSentinels,         // [IN/OUT] Number of sentinels
    BOOL        bNoVoidAllowed)         // [IN] Flag indicating whether "void" is disallowed for this arg 
{
    ULONG       ulElementType;          // Current element type being processed.
    ULONG       ulElemSize;             // Size of the element type.
    mdToken     token;                  // Embedded token.
    ULONG       ulArgCnt;               // Argument count for function pointer.
    ULONG       ulRank;                 // Rank of the array.
    ULONG       ulSizes;                // Count of sized dimensions of the array.
    ULONG       ulLbnds;                // Count of lower bounds of the array.
    ULONG       ulTkSize;               // Token size.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.
    BOOL        bRepeat = TRUE;         // MODOPT and MODREQ belong to the arg after them
    BOOL        bByRefForbidden = FALSE;// ByRef is not allowed for fields

    BEGIN_ENTRYPOINT_NOTHROW;

    switch(TypeFromToken(tk))
    {
    case mdtFieldDef:
        bByRefForbidden = TRUE;
        break;
    case mdtName:
        tk = TokenFromRid(RidFromToken(tk),mdtFieldDef);
        // Field type can be a FNPTR with a sig containing ByRefs.
        // So we change the token type not to be mdtFieldDef and thus allow ByRefs,
        // but the token needs to be restored to its original type for reporting
        break;
    }

    _ASSERTE (pulCurByte);
    memset(&veCtxt, 0, sizeof(VEContext));
    veCtxt.Token = tk;
    veCtxt.uOffset = 0;

    while(bRepeat)
    {
        bRepeat = FALSE;
    // Validate that the argument is not missing.
    _ASSERTE(*pulCurByte <= cbSig);
    if (cbSig == *pulCurByte)
    {
        hr = VLDTR_E_SIG_MISSARG;
        goto ErrExit;
    }

    // Get the element type.
    *pulCurByte += (ulElemSize = CorSigUncompressedDataSize(pbSig));
    ulElementType = CorSigUncompressData(pbSig);

    // Walk past all the modifier types.
    while (ulElementType & ELEMENT_TYPE_MODIFIER)
    {
        _ASSERTE(*pulCurByte <= cbSig);
        if(ulElementType == ELEMENT_TYPE_SENTINEL)
        {
            if(pulNSentinels) *pulNSentinels+=1;
            if(TypeFromToken(tk) == mdtMethodDef)
            {
                REPORT_ERROR0(VLDTR_E_SIG_SENTINMETHODDEF);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            if (cbSig == *pulCurByte)
            {
                REPORT_ERROR0(VLDTR_E_SIG_LASTSENTINEL);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
                goto ErrExit;
            }
        }
        if (cbSig == *pulCurByte)
        {
            REPORT_ERROR2(VLDTR_E_SIG_MISSELTYPE, ulElementType, *pulCurByte + 1);
            SetVldtrCode(&hr, hrSave);
            goto ErrExit;
        }
        *pulCurByte += (ulElemSize = CorSigUncompressedDataSize(pbSig));
        ulElementType = CorSigUncompressData(pbSig);
    }

    switch (ulElementType)
    {
        case ELEMENT_TYPE_VOID:
            if(bNoVoidAllowed)
            {
                IfBreakGo(m_pVEHandler->VEHandler(VLDTR_E_SIG_BADVOID, veCtxt, 0));
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_R8:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_TYPEDBYREF:
        case ELEMENT_TYPE_U:
        case ELEMENT_TYPE_I:
            break;
        case ELEMENT_TYPE_BYREF:  //fallthru
            if(bByRefForbidden)
            {
                IfBreakGo(m_pVEHandler->VEHandler(VLDTR_E_SIG_BYREFINFIELD, veCtxt, 0));
                SetVldtrCode(&hr, hrSave);
            }
        case ELEMENT_TYPE_PTR:
            // Validate the referenced type.
            IfFailGo(ValidateOneArg(tk, pbSig, cbSig, pulCurByte,pulNSentinels,FALSE));
            if (hr != S_OK)
                SetVldtrCode(&hrSave, hr);
            break;
        case ELEMENT_TYPE_PINNED:
        case ELEMENT_TYPE_SZARRAY:
            // Validate the referenced type.
            IfFailGo(ValidateOneArg(tk, pbSig, cbSig, pulCurByte,pulNSentinels,TRUE));
            if (hr != S_OK)
                SetVldtrCode(&hrSave, hr);
            break;
        case ELEMENT_TYPE_VALUETYPE: //fallthru
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_CMOD_OPT:
        case ELEMENT_TYPE_CMOD_REQD:
            // See if the token is missing.
            _ASSERTE(*pulCurByte <= cbSig);
            if (cbSig == *pulCurByte)
            {
                REPORT_ERROR1(VLDTR_E_SIG_MISSTKN, ulElementType);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
                break;
            }
            // See if the token is a valid token.
            ulTkSize = CorSigUncompressedDataSize(pbSig);
            token = CorSigUncompressToken(pbSig);
            if (!IsValidToken(token))
            {
                REPORT_ERROR2(VLDTR_E_SIG_TKNBAD, token, *pulCurByte);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
                *pulCurByte += ulTkSize;
                break;
            }
            *pulCurByte += ulTkSize;
            if ((ulElementType == ELEMENT_TYPE_CLASS) || (ulElementType == ELEMENT_TYPE_VALUETYPE))
            {
                // Check for long-form encoding
                CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);
                LPCSTR      szName = "";                 // token's Name.
                LPCSTR      szNameSpace = "";            // token's NameSpace.


                // Check for TypeDef or TypeRef
                // To prevent cycles in metadata, token must not be a TypeSpec.
                if ((TypeFromToken(token) != mdtTypeRef) && (TypeFromToken(token) != mdtTypeDef))
                {
                    REPORT_ERROR2(VLDTR_E_SIG_BADTOKTYPE, token, *pulCurByte);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }

                if (TypeFromToken(token) == mdtTypeRef)
                {
                    TypeRefRec *pTokenRec;
                    IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(token), &pTokenRec));
                    mdToken tkResScope = pMiniMd->getResolutionScopeOfTypeRef(pTokenRec);
                    if (RidFromToken(tkResScope) && (TypeFromToken(tkResScope) == mdtAssemblyRef))
                    {
                        AssemblyRefRec * pARRec;
                        IfFailGo(pMiniMd->GetAssemblyRefRecord(RidFromToken(tkResScope), &pARRec));
                        LPCSTR szAssemblyRefName;
                        IfFailGo(pMiniMd->getNameOfAssemblyRef(pARRec, &szAssemblyRefName));
                        if((0 == SString::_stricmp("mscorlib", szAssemblyRefName)) || (0 == SString::_stricmp("System.Runtime", szAssemblyRefName)))
                        {
                            IfFailGo(pMiniMd->getNamespaceOfTypeRef(pTokenRec, &szNameSpace));
                            IfFailGo(pMiniMd->getNameOfTypeRef(pTokenRec, &szName));
                        }
                    }
                }
                else if (TypeFromToken(token) == mdtTypeDef)
                {
                    TypeDefRec *pTokenRec;
                    IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(token), &pTokenRec));
                    if(g_fValidatingMscorlib) // otherwise don't even bother checking the name
                    {
                        IfFailGo(pMiniMd->getNameOfTypeDef(pTokenRec, &szName));
                        IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTokenRec, &szNameSpace));
                    }
                    // while at it, check if token is indeed a class (valuetype)
                    BOOL bValueType = FALSE;
                    if(!IsTdInterface(pTokenRec->GetFlags()))
                    {
                        mdToken tkExtends = pMiniMd->getExtendsOfTypeDef(pTokenRec);
                        if(RidFromToken(tkExtends))
                        {
                            LPCSTR      szExtName = "";                 // parent's Name.
                            LPCSTR      szExtNameSpace = "";            // parent's NameSpace.
                            if(TypeFromToken(tkExtends)==mdtTypeRef)
                            {
                                TypeRefRec *pExtRec;
                                IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(tkExtends), &pExtRec));
                                mdToken tkResScope = pMiniMd->getResolutionScopeOfTypeRef(pExtRec);
                                if(RidFromToken(tkResScope) && (TypeFromToken(tkResScope)==mdtAssemblyRef))
                                {
                                    AssemblyRefRec *pARRec;
                                    IfFailGo(pMiniMd->GetAssemblyRefRecord(RidFromToken(tkResScope), &pARRec));
                                    LPCSTR szAssemblyRefName;
                                    IfFailGo(pMiniMd->getNameOfAssemblyRef(pARRec, &szAssemblyRefName));
                                    if((0 == SString::_stricmp("mscorlib", szAssemblyRefName)) || (0 == SString::_stricmp("System.Runtime", szAssemblyRefName)))
                                    {
                                        IfFailGo(pMiniMd->getNamespaceOfTypeRef(pExtRec, &szExtNameSpace));
                                        IfFailGo(pMiniMd->getNameOfTypeRef(pExtRec, &szExtName));
                                    }
                                }
                            }
                            else if(TypeFromToken(tkExtends)==mdtTypeDef)
                            {
                                if(g_fValidatingMscorlib) // otherwise don't even bother checking the name
                                {
                                    TypeDefRec *pExtRec;
                                    IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkExtends), &pExtRec));
                                    IfFailGo(pMiniMd->getNameOfTypeDef(pExtRec, &szExtName));
                                    IfFailGo(pMiniMd->getNamespaceOfTypeDef(pExtRec, &szExtNameSpace));
                                }
                            }
                            if(0 == strcmp(szExtNameSpace,BASE_NAMESPACE))
                            {
                                if(0==strcmp(szExtName,BASE_ENUM_CLASSNAME)) bValueType = TRUE;
                                else if(0==strcmp(szExtName,BASE_VTYPE_CLASSNAME))
                                {
                                    bValueType = (strcmp(szNameSpace,BASE_NAMESPACE) ||
                                                strcmp(szName,BASE_ENUM_CLASSNAME));
                                }
                            }
                        }
                    }
                    if(bValueType != (ulElementType == ELEMENT_TYPE_VALUETYPE))
                    {
                        REPORT_ERROR2(VLDTR_E_SIG_TOKTYPEMISMATCH, token, *pulCurByte);
                        SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    }

                }
                if(0 == strcmp(szNameSpace,BASE_NAMESPACE))
                {
                    for(unsigned jjj = 0; jjj < g_NumSigLongForms; jjj++)
                    {
                        if(0 == strcmp(szName,g_SigLongFormName[jjj]))
                        {
                            REPORT_ERROR2(VLDTR_E_SIG_LONGFORM, token, *pulCurByte);
                            SetVldtrCode(&hrSave, VLDTR_S_ERR);
                            break;
                        }
                    }
                }
            }
            else // i.e. if(ELEMENT_TYPE_CMOD_OPT || ELEMENT_TYPE_CMOD_REQD)
                bRepeat = TRUE; // go on validating, we're not done with this arg
            break;

        case ELEMENT_TYPE_FNPTR: 
            // Validate that calling convention is present.
            _ASSERTE(*pulCurByte <= cbSig);
            if (cbSig == *pulCurByte)
            {
                REPORT_ERROR1(VLDTR_E_SIG_MISSFPTR, *pulCurByte + 1);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
                break;
            }
            // Consume calling convention.
            *pulCurByte += CorSigUncompressedDataSize(pbSig);
            CorSigUncompressData(pbSig);

            // Validate that argument count is present.
            _ASSERTE(*pulCurByte <= cbSig);
            if (cbSig == *pulCurByte)
            {
                REPORT_ERROR1(VLDTR_E_SIG_MISSFPTRARGCNT, *pulCurByte + 1);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
                break;
            }
            // Consume argument count.
            *pulCurByte += CorSigUncompressedDataSize(pbSig);
            ulArgCnt = CorSigUncompressData(pbSig);

            // Checking the signature, ByRefs OK
            if(bByRefForbidden)
                tk = TokenFromRid(RidFromToken(tk),mdtName);

            // Validate and consume return type.
            IfFailGo(ValidateOneArg(tk, pbSig, cbSig, pulCurByte,NULL,FALSE));
            if (hr != S_OK)
            {
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
                break;
            }

            // Validate and consume the arguments.
            while(ulArgCnt--)
            {
                IfFailGo(ValidateOneArg(tk, pbSig, cbSig, pulCurByte,NULL,TRUE));
                if (hr != S_OK)
                {
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    break;
                }
            }
            break;

        case ELEMENT_TYPE_ARRAY:
            // Validate and consume the base type.
            IfFailGo(ValidateOneArg(tk, pbSig, cbSig, pulCurByte,pulNSentinels,TRUE));

            // Validate that the rank is present.
            _ASSERTE(*pulCurByte <= cbSig);
            if (cbSig == *pulCurByte)
            {
                REPORT_ERROR1(VLDTR_E_SIG_MISSRANK, *pulCurByte + 1);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
                break;
            }
            // Consume the rank.
            *pulCurByte += CorSigUncompressedDataSize(pbSig);
            ulRank = CorSigUncompressData(pbSig);

            // Process the sizes.
            if (ulRank)
            {
                // Validate that the count of sized-dimensions is specified.
                _ASSERTE(*pulCurByte <= cbSig);
                if (cbSig == *pulCurByte)
                {
                    REPORT_ERROR1(VLDTR_E_SIG_MISSNSIZE, *pulCurByte + 1);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    break;
                }
                // Consume the count of sized dimensions.
                *pulCurByte += CorSigUncompressedDataSize(pbSig);
                ulSizes = CorSigUncompressData(pbSig);

                // Loop over the sizes.
                while (ulSizes--)
                {
                    // Validate the current size.
                    _ASSERTE(*pulCurByte <= cbSig);
                    if (cbSig == *pulCurByte)
                    {
                        REPORT_ERROR1(VLDTR_E_SIG_MISSSIZE, *pulCurByte + 1);
                        SetVldtrCode(&hrSave, VLDTR_S_ERR);
                        break;
                    }
                    // Consume the current size.
                    *pulCurByte += CorSigUncompressedDataSize(pbSig);
                    CorSigUncompressData(pbSig);
                }

                // Validate that the count of lower bounds is specified.
                _ASSERTE(*pulCurByte <= cbSig);
                if (cbSig == *pulCurByte)
                {
                    REPORT_ERROR1(VLDTR_E_SIG_MISSNLBND, *pulCurByte + 1);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    break;
                }
                // Consume the count of lower bound.
                *pulCurByte += CorSigUncompressedDataSize(pbSig);
                ulLbnds = CorSigUncompressData(pbSig);

                // Loop over the lower bounds.
                while (ulLbnds--)
                {
                    // Validate the current lower bound.
                    _ASSERTE(*pulCurByte <= cbSig);
                    if (cbSig == *pulCurByte)
                    {
                        REPORT_ERROR1(VLDTR_E_SIG_MISSLBND, *pulCurByte + 1);
                        SetVldtrCode(&hrSave, VLDTR_S_ERR);
                        break;
                    }
                    // Consume the current size.
                    *pulCurByte += CorSigUncompressedDataSize(pbSig);
                    CorSigUncompressData(pbSig);
                }
            }
            break;

        case ELEMENT_TYPE_VAR: 
        case ELEMENT_TYPE_MVAR: 
            // Consume index.
            *pulCurByte += CorSigUncompressedDataSize(pbSig);
            CorSigUncompressData(pbSig);
            break;

        case ELEMENT_TYPE_GENERICINST: 
            { 
                PCCOR_SIGNATURE pbGenericTypeSig = pbSig;
                BOOL fCheckArity = FALSE;
                ULONG ulGenericArity = 0;
            
                // Validate and consume the type constructor
                IfFailGo(ValidateOneArg(tk, pbSig, cbSig, pulCurByte, NULL, TRUE));

                // Extract its arity
                {
                    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);
                    switch(CorSigUncompressElementType(pbGenericTypeSig))
                    {
                        case ELEMENT_TYPE_VALUETYPE:
                        case ELEMENT_TYPE_CLASS: 
                            { 
                                mdToken tkGenericType = CorSigUncompressToken(pbGenericTypeSig);
                                if (TypeFromToken(tkGenericType) == mdtTypeDef)
                                {
                                    HENUMInternal hEnumTyPars;
                                    hr = pMiniMd->FindGenericParamHelper(tkGenericType, &hEnumTyPars);
                                    if (SUCCEEDED(hr))
                                    {
                                        IfFailGo(HENUMInternal::GetCount(&hEnumTyPars,&ulGenericArity));
                                        HENUMInternal::ClearEnum(&hEnumTyPars);
                                        fCheckArity = TRUE;
                                    }
                                    ; 
                                }
                                // for a mdtTypeRef, don't check anything until load time
                                break;
                            }
                    default:
                        break;
                    }
                    
                }
                
                // Consume argument count.
                if (cbSig == *pulCurByte)
                {
                    REPORT_ERROR1(VLDTR_E_SIG_MISSARITY, *pulCurByte + 1);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                    break;
                }
                
                *pulCurByte += CorSigUncompressedDataSize(pbSig);
                ulArgCnt = CorSigUncompressData(pbSig);

                if (ulArgCnt == 0)
                {
                    REPORT_ERROR1(VLDTR_E_SIG_ARITYZERO,*pulCurByte);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                
                if (fCheckArity && ulArgCnt != ulGenericArity)
                {
                    REPORT_ERROR3(VLDTR_E_SIG_ARITYMISMATCH,ulGenericArity,ulArgCnt,*pulCurByte);
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
                
                // Validate and consume the arguments.
                while(ulArgCnt--)
                {
                    PCCOR_SIGNATURE pbTypeArg = pbSig;
                    ULONG ulTypeArgByte = *pulCurByte;
                    IfFailGo(ValidateOneArg(tk, pbSig, cbSig, pulCurByte, NULL, TRUE));
                    if (hr != S_OK)
                    {
                        SetVldtrCode(&hrSave, VLDTR_S_ERR);
                        break;
                    }

                    // reject byref-like args
                    switch (CorSigUncompressData(pbTypeArg))
                    {
                       case ELEMENT_TYPE_TYPEDBYREF:
                       case ELEMENT_TYPE_BYREF:
                         {
                            REPORT_ERROR1(VLDTR_E_SIG_BYREFINST, ulTypeArgByte);
                            SetVldtrCode(&hrSave, VLDTR_S_ERR);
                            break;
                         }
                       default: 
                         break;
                    }
                }

                break;
            }
            

        case ELEMENT_TYPE_SENTINEL: // this case never works because all modifiers are skipped before switch
            if(TypeFromToken(tk) == mdtMethodDef)
            {
                REPORT_ERROR0(VLDTR_E_SIG_SENTINMETHODDEF);
                SetVldtrCode(&hrSave, VLDTR_S_ERR);
            }
            break;
        default:
            REPORT_ERROR2(VLDTR_E_SIG_BADELTYPE, ulElementType, *pulCurByte - ulElemSize);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
            break;
    }   // switch (ulElementType)
    } // end while(bRepeat)
    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateOneArg()
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//*****************************************************************************
// This function validates the given Method signature.  This function works
// with Method signature for both the MemberRef and MethodDef.
//*****************************************************************************
HRESULT RegMeta::ValidateMethodSig(
    mdToken     tk,                     // [IN] Token whose signature needs to be validated.
    PCCOR_SIGNATURE pbSig,              // [IN] Signature.
    ULONG       cbSig,                  // [IN] Size in bytes of the signature.
    DWORD       dwFlags)                // [IN] Method flags.
{
    ULONG       ulCurByte = 0;          // Current index into the signature.
    ULONG       ulCallConv;             // Calling convention.
    ULONG       ulArgCount;             // Count of arguments.
    ULONG       ulTyArgCount;           // Count of type arguments.
    ULONG       i;                      // Looping index.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.
    ULONG       ulNSentinels = 0;

    BEGIN_ENTRYPOINT_NOTHROW;

    _ASSERTE(TypeFromToken(tk) == mdtMethodDef ||
             TypeFromToken(tk) == mdtMemberRef);

    memset(&veCtxt, 0, sizeof(VEContext));
    veCtxt.Token = tk;
    veCtxt.uOffset = 0;

    // Validate the signature is well-formed with respect to the compression
    // scheme.  If this fails, no further validation needs to be done.
    if ((hr = ValidateSigCompression(tk, pbSig, cbSig)) != S_OK)
        goto ErrExit;

    // Validate the calling convention.
    ulCurByte += CorSigUncompressedDataSize(pbSig);
    ulCallConv = CorSigUncompressData(pbSig);

    i = ulCallConv & IMAGE_CEE_CS_CALLCONV_MASK;
    if ((i != IMAGE_CEE_CS_CALLCONV_DEFAULT)&&( i != IMAGE_CEE_CS_CALLCONV_VARARG)
        || (ulCallConv & IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS))
    {
        REPORT_ERROR1(VLDTR_E_MD_BADCALLINGCONV, ulCallConv);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    if (TypeFromToken(tk) == mdtMethodDef) // MemberRefs have no flags available
    {
        // If HASTHIS is set on the calling convention, the method should not be static.
        if ((ulCallConv & IMAGE_CEE_CS_CALLCONV_HASTHIS) &&
            IsMdStatic(dwFlags))
        {
            REPORT_ERROR1(VLDTR_E_MD_THISSTATIC, ulCallConv);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }

        // If HASTHIS is not set on the calling convention, the method should be static.
        if (!(ulCallConv & IMAGE_CEE_CS_CALLCONV_HASTHIS) &&
            !IsMdStatic(dwFlags))
        {
            REPORT_ERROR1(VLDTR_E_MD_NOTTHISNOTSTATIC, ulCallConv);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
    }

    // Get the type argument count.
    if (ulCallConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        if (i != IMAGE_CEE_CS_CALLCONV_DEFAULT)
        {
            REPORT_ERROR1(VLDTR_E_MD_GENERIC_BADCALLCONV, ulCallConv);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        
        if (cbSig == ulCurByte)
        {
            REPORT_ERROR1(VLDTR_E_MD_MISSARITY, ulCurByte+1);
            SetVldtrCode(&hr, hrSave);
            goto ErrExit;
        }
        
        ulCurByte += CorSigUncompressedDataSize(pbSig);
        ulTyArgCount = CorSigUncompressData(pbSig);
        
        if (ulTyArgCount == 0)
        {
            REPORT_ERROR1(VLDTR_E_MD_ARITYZERO, ulCurByte);
            SetVldtrCode(&hrSave, VLDTR_S_ERR);
        }
        
        // If this is a def, check the arity against the number of generic params
        if (TypeFromToken(tk) == mdtMethodDef)
        {
            CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);
            ULONG ulGenericParamCount;
            HENUMInternal hEnumTyPars;
            hr = pMiniMd->FindGenericParamHelper(tk, &hEnumTyPars);
            if (SUCCEEDED(hr))
            {
                IfFailGo(HENUMInternal::GetCount(&hEnumTyPars,&ulGenericParamCount));
                HENUMInternal::ClearEnum(&hEnumTyPars);
                if (ulTyArgCount != ulGenericParamCount)
                {
                    REPORT_ERROR2(VLDTR_E_MD_GPMISMATCH,ulTyArgCount,ulGenericParamCount); 
                    SetVldtrCode(&hrSave, VLDTR_S_ERR);
                }
            }
        }
    }


    // Is there any sig left for arguments?
    _ASSERTE(ulCurByte <= cbSig);
    if (cbSig == ulCurByte)
    {
        REPORT_ERROR1(VLDTR_E_MD_NOARGCNT, ulCurByte+1);
        SetVldtrCode(&hr, hrSave);
        goto ErrExit;
    }

    // Get the argument count.
    ulCurByte += CorSigUncompressedDataSize(pbSig);
    ulArgCount = CorSigUncompressData(pbSig);

    // Validate the return type and the arguments.
//    for (i = 0; i < (ulArgCount + 1); i++)
    for(i=1; ulCurByte < cbSig; i++)
    {
        hr = ValidateOneArg(tk, pbSig, cbSig, &ulCurByte,&ulNSentinels,(i > 1));
        if (hr != S_OK)
        {
            if(hr == VLDTR_E_SIG_MISSARG)
            {
                REPORT_ERROR1(VLDTR_E_SIG_MISSARG, i);
            }
            SetVldtrCode(&hr, hrSave);
            hrSave = hr;
            break;
        }
    }
    if((ulNSentinels != 0) && (!isCallConv(ulCallConv, IMAGE_CEE_CS_CALLCONV_VARARG )))
    {
        REPORT_ERROR0(VLDTR_E_SIG_SENTMUSTVARARG);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }
    if(ulNSentinels > 1)
    {
        REPORT_ERROR0(VLDTR_E_SIG_MULTSENTINELS);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateMethodSig()

//*****************************************************************************
// This function validates the given Field signature.  This function works
// with Field signature for both the MemberRef and FieldDef.
//*****************************************************************************
HRESULT RegMeta::ValidateFieldSig(
    mdToken     tk,                     // [IN] Token whose signature needs to be validated.
    PCCOR_SIGNATURE pbSig,              // [IN] Signature.
    ULONG       cbSig)                  // [IN] Size in bytes of the signature.
{
    ULONG       ulCurByte = 0;          // Current index into the signature.
    ULONG       ulCallConv;             // Calling convention.
    VEContext   veCtxt;                 // Context record.
    HRESULT     hr = S_OK;              // Value returned.
    HRESULT     hrSave = S_OK;          // Save state.

    BEGIN_ENTRYPOINT_NOTHROW;

    _ASSERTE(TypeFromToken(tk) == mdtFieldDef ||
             TypeFromToken(tk) == mdtMemberRef);

    memset(&veCtxt, 0, sizeof(VEContext));
    veCtxt.Token = tk;
    veCtxt.uOffset = 0;

    // Validate the calling convention.
    ulCurByte += CorSigUncompressedDataSize(pbSig);
    ulCallConv = CorSigUncompressData(pbSig);
    if (!isCallConv(ulCallConv, IMAGE_CEE_CS_CALLCONV_FIELD ))
    {
        REPORT_ERROR1(VLDTR_E_FD_BADCALLINGCONV, ulCallConv);
        SetVldtrCode(&hrSave, VLDTR_S_ERR);
    }

    // Validate the field.
    IfFailGo(ValidateOneArg(tk, pbSig, cbSig, &ulCurByte,NULL,TRUE));
    SetVldtrCode(&hrSave, hr);

    hr = hrSave;
ErrExit:
    ;
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::ValidateFieldSig()

//*****************************************************************************
// This is a utility function to allocate a one-dimensional zero-based safe
// array of variants.
//*****************************************************************************
static HRESULT _AllocSafeVariantArrayVector( // Return status.
    VARIANT     *rVar,                  // [IN] Variant array.
    int         cElem,                  // [IN] Size of the array.
    SAFEARRAY   **ppArray)              // [OUT] Double pointer to SAFEARRAY.
{
    HRESULT     hr = S_OK;
    LONG        i;

    _ASSERTE(rVar && cElem && ppArray);

    IfNullGo(*ppArray = SafeArrayCreateVector(VT_VARIANT, 0, cElem));
    for (i = 0; i < cElem; i++)
        IfFailGo(SafeArrayPutElement(*ppArray, &i, &rVar[i]));
ErrExit:
    return hr;
}   // _AllocSafeVariantArrayVector()

//*****************************************************************************
// Helper function for reporting error with no arguments
//*****************************************************************************
HRESULT RegMeta::_ValidateErrorHelper(
        HRESULT     VECode,
        VEContext   Context)
{
    HRESULT     hr = S_OK;
    
    //
    // MDValidator does not zero out the Context. Fix it here. This fix relies 
    // on the fact that MDValidator just uses the token  and offset field of the
    // context.
    //

    if (Context.Token != 0) {
        Context.flags = VER_ERR_TOKEN;
    }

    IfBreakGo(m_pVEHandler->VEHandler(VECode, Context, NULL));
ErrExit:

    return hr;
}   // _ValidateErrorHelper()

//*****************************************************************************
// Helper function for reporting error with 1 argument
//*****************************************************************************
HRESULT RegMeta::_ValidateErrorHelper(
        HRESULT     VECode,
        VEContext   Context,
        ULONG       ulVal1)
{
    HRESULT     hr = S_OK;
    SAFEARRAY   *psa = 0;               // The SAFEARRAY.
    VARIANT     rVar[1];                // The VARIANT array

    if (Context.Token != 0) {
        Context.flags = VER_ERR_TOKEN;
    }

    V_VT(&rVar[0]) = VT_UI4;
    V_UI4(&rVar[0]) = ulVal1;
    IfFailGo(_AllocSafeVariantArrayVector(rVar, 1, &psa));
    IfBreakGo(m_pVEHandler->VEHandler(VECode, Context, psa));

ErrExit:
    if (psa)
    {
        HRESULT hrSave = SafeArrayDestroy(psa);
        if (FAILED(hrSave))
            hr = hrSave;
    }
    return hr;
}   // _ValidateErrorHelper()

//*****************************************************************************
// Helper function for reporting error with 2 arguments
//*****************************************************************************
HRESULT RegMeta::_ValidateErrorHelper(
        HRESULT     VECode,
        VEContext   Context,
        ULONG       ulVal1,
        ULONG       ulVal2)
{
    HRESULT     hr = S_OK;
    SAFEARRAY   *psa = 0;               // The SAFEARRAY.
    VARIANT     rVar[2];                // The VARIANT array

    if (Context.Token != 0) {
        Context.flags = VER_ERR_TOKEN;
    }
    
    V_VT(&rVar[0]) = VT_UI4;
    V_UI4(&rVar[0]) = ulVal1;
    V_VT(&rVar[1]) = VT_UI4;
    V_UI4(&rVar[1]) = ulVal2;

    IfFailGo(_AllocSafeVariantArrayVector(rVar, 2, &psa));
    IfBreakGo(m_pVEHandler->VEHandler(VECode, Context, psa));

ErrExit:
    if (psa)
    {
        HRESULT hrSave = SafeArrayDestroy(psa);
        if (FAILED(hrSave))
            hr = hrSave;
    }
    return hr;
}   // _ValidateErrorHelper()

//*****************************************************************************
// Helper function for reporting error with 3 arguments
//*****************************************************************************
HRESULT RegMeta::_ValidateErrorHelper(
        HRESULT     VECode,
        VEContext   Context,
        ULONG       ulVal1,
        ULONG       ulVal2,
        ULONG       ulVal3)
{
    HRESULT     hr = S_OK;
    SAFEARRAY   *psa = 0;               // The SAFEARRAY.
    VARIANT     rVar[3];                // The VARIANT array

    if (Context.Token != 0) {
        Context.flags = VER_ERR_TOKEN;
    }
    
    V_VT(&rVar[0]) = VT_UI4;
    V_UI4(&rVar[0]) = ulVal1;
    V_VT(&rVar[1]) = VT_UI4;
    V_UI4(&rVar[1]) = ulVal2;
    V_VT(&rVar[2]) = VT_UI4;
    V_UI4(&rVar[2]) = ulVal3;

    IfFailGo(_AllocSafeVariantArrayVector(rVar, 3, &psa));
    IfBreakGo(m_pVEHandler->VEHandler(VECode, Context, psa));

ErrExit:
    if (psa)
    {
        HRESULT hrSave = SafeArrayDestroy(psa);
        if (FAILED(hrSave))
            hr = hrSave;
    }
    return hr;
}

//*****************************************************************************
// Helper function to see if there is a duplicate record for ClassLayout.
//*****************************************************************************
static HRESULT _FindClassLayout(
    CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
    mdTypeDef   tkParent,               // [IN] the parent that ClassLayout is associated with
    RID         *pclRid,                // [OUT] rid for the ClassLayout.
    RID         rid)                    // [IN] rid to be ignored.
{
    HRESULT hr;
    ULONG       cClassLayoutRecs;
    ClassLayoutRec *pRecord;
    mdTypeDef   tkParTmp;
    ULONG       i;

    _ASSERTE(pMiniMd && pclRid && rid);
    _ASSERTE(TypeFromToken(tkParent) == mdtTypeDef && RidFromToken(tkParent));

    cClassLayoutRecs = pMiniMd->getCountClassLayouts();

    for (i = 1; i <= cClassLayoutRecs; i++)
    {
        // Ignore the rid to be ignored!
        if (rid == i)
            continue;

        IfFailRet(pMiniMd->GetClassLayoutRecord(i, &pRecord));
        tkParTmp = pMiniMd->getParentOfClassLayout(pRecord);
        if (tkParTmp == tkParent)
        {
            *pclRid = i;
            return S_OK;
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
}   // _FindClassLayout()

//*****************************************************************************
// Helper function to see if there is a duplicate for FieldLayout.
//*****************************************************************************
static HRESULT _FindFieldLayout(
    CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
    mdFieldDef  tkParent,               // [IN] the parent that FieldLayout is associated with
    RID         *pflRid,                // [OUT] rid for the FieldLayout record.
    RID         rid)                    // [IN] rid to be ignored.
{
    HRESULT hr;
    ULONG       cFieldLayoutRecs;
    FieldLayoutRec *pRecord;
    mdFieldDef  tkField;
    ULONG       i;

    _ASSERTE(pMiniMd && pflRid && rid);
    _ASSERTE(TypeFromToken(tkParent) == mdtFieldDef && RidFromToken(tkParent));

    cFieldLayoutRecs = pMiniMd->getCountFieldLayouts();

    for (i = 1; i <= cFieldLayoutRecs; i++)
    {
        // Ignore the rid to be ignored!
        if (rid == i)
            continue;

        IfFailRet(pMiniMd->GetFieldLayoutRecord(i, &pRecord));
        tkField = pMiniMd->getFieldOfFieldLayout(pRecord);
        if (tkField == tkParent)
        {
            *pflRid = i;
            return S_OK;
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
}   // _FindFieldLayout()

//*****************************************************************************
//*****************************************************************************
HRESULT
MDSigComparer::CompareMethodSignature()
{
    HRESULT hr = S_OK;

    EX_TRY
    {
        hr = _CompareMethodSignature();
    }
    EX_CATCH
    {
        hr = E_FAIL;
    }
    EX_END_CATCH(SwallowAllExceptions)

    return hr;
}

//*****************************************************************************
//*****************************************************************************
HRESULT
MDSigComparer::_CompareMethodSignature()
{
    HRESULT hr;

    // Test equivalency of method signature header
    ULONG cArgs;
    IfFailRet(_CompareMethodSignatureHeader(cArgs));

    // Iterate for cArgs + 1 to include the return type
    for (ULONG i = 0; i < cArgs + 1; i++)
    {
        IfFailRet(_CompareExactlyOne());
    }

    return S_OK;
}

//*****************************************************************************
//*****************************************************************************
HRESULT
MDSigComparer::_CompareExactlyOne()
{
    HRESULT hr;

    CorElementType typ1, typ2;
    IfFailRet(m_sig1.GetElemType(&typ1));
    IfFailRet(m_sig2.GetElemType(&typ2));

    if (typ1 != typ2)
    {
        return E_FAIL;
    }

    CorElementType typ = typ1;
    if (!CorIsPrimitiveType((CorElementType)typ))
    {
        switch (typ)
        {
            default:
            {
                // _ASSERT(!"Illegal or unimplement type in COM+ sig.");
                return META_E_BAD_SIGNATURE;
                break;
            }
            case ELEMENT_TYPE_VAR:
            case ELEMENT_TYPE_MVAR:
            {
                IfFailRet(_CompareData(NULL));                  // Skip variable number
                break;
            }
            case ELEMENT_TYPE_OBJECT:
            case ELEMENT_TYPE_STRING:
            case ELEMENT_TYPE_TYPEDBYREF:
            {
                break;
            }

            case ELEMENT_TYPE_BYREF:                            // fallthru
            case ELEMENT_TYPE_PTR:
            case ELEMENT_TYPE_PINNED:
            case ELEMENT_TYPE_SZARRAY:
            {
                IfFailRet(_CompareExactlyOne());                // Compare referenced type
                break;
            }

            case ELEMENT_TYPE_VALUETYPE:                        // fallthru
            case ELEMENT_TYPE_CLASS:
            {
                mdToken tok1, tok2;
                IfFailRet(m_sig1.GetToken(&tok1));
                IfFailRet(m_sig2.GetToken(&tok2));
                IfFailRet(m_comparer.CompareToken(tok1, tok2));
                break;
            }

            case ELEMENT_TYPE_FNPTR:
            {
                IfFailRet(_CompareMethodSignature());
                break;
            }

            case ELEMENT_TYPE_ARRAY:
            {
                IfFailRet(_CompareExactlyOne());                // Compare element type

                ULONG rank;
                IfFailRet(_CompareData(&rank));                 // Compare & get rank

                if (rank)
                {
                    ULONG nsizes;
                    IfFailRet(_CompareData(&nsizes));           // Compare & get # of sizes
                    while (nsizes--)
                    {
                        IfFailRet(_CompareData(NULL));          // Compare size
                    }

                    ULONG nlbounds;
                    IfFailRet(_CompareData(&nlbounds));         // Compare & get # of lower bounds
                    while (nlbounds--)
                    {
                        IfFailRet(_CompareData(NULL));          // Compare lower bounds
                    }
                }

                break;
            }

            case ELEMENT_TYPE_SENTINEL:
            {
                // Should be unreachable since GetElem strips it
                break;
            }

            case ELEMENT_TYPE_INTERNAL:
            {
                // Shouldn't ever get this since it is internal to the runtime,
                // but just in case we know how to compare and skip these.
                PVOID val1 = *((PVOID *)m_sig1.m_ptr);
                PVOID val2 = *((PVOID *)m_sig2.m_ptr);

                if (val1 != val2)
                {
                    return E_FAIL;
                }

                m_sig1.SkipBytes(sizeof(void*));
                m_sig2.SkipBytes(sizeof(void*));
                break;
            }

            case ELEMENT_TYPE_GENERICINST:
            {
                IfFailRet(_CompareExactlyOne());                  // Compare generic type
                ULONG argCnt;
                IfFailRet(_CompareData(&argCnt));               // Compare & get number of parameters
                _ASSERTE(argCnt > 0);
                while (argCnt--)
                {
                    IfFailRet(_CompareExactlyOne());              // Compare the parameters
                }
                break;
            }
        }
    }

    return S_OK;
}

//*****************************************************************************
//*****************************************************************************
HRESULT
MDSigComparer::_CompareData(
    ULONG     *pulData)
{
    ULONG cbCompressedData1, cbCompressedData2;
    ULONG ulData1, ulData2;

    cbCompressedData1 = CorSigUncompressData(m_sig1.m_ptr, &ulData1);
    cbCompressedData2 = CorSigUncompressData(m_sig2.m_ptr, &ulData2);

    if ((cbCompressedData1 == ((ULONG)(-1))) ||
        (cbCompressedData2 == ((ULONG)(-1))) ||
        (cbCompressedData1 != cbCompressedData2) ||
        (ulData1 != ulData2))
    {
        return E_FAIL;
    }

    m_sig1.SkipBytes(cbCompressedData1);
    m_sig2.SkipBytes(cbCompressedData2);

    // Out data
    if (pulData)
        *pulData = ulData1;

    return S_OK;
}

//*****************************************************************************
//*****************************************************************************
HRESULT
MDSigComparer::_CompareMethodSignatureHeader(
    ULONG     &cArgs)
{
    HRESULT hr;

    // Get calling convention information, but only use it to get type param information.
    ULONG uCallConv1, uCallConv2;
    IfFailRet(m_sig1.GetData(&uCallConv1));
    IfFailRet(m_sig2.GetData(&uCallConv2));

    // Check type parameter information
    ULONG uTypeParamCount1 = 0;
    ULONG uTypeParamCount2 = 0;

    if (uCallConv1 & IMAGE_CEE_CS_CALLCONV_GENERIC)
        IfFailRet(m_sig1.GetData(&uTypeParamCount1));

    if (uCallConv2 & IMAGE_CEE_CS_CALLCONV_GENERIC)
        IfFailRet(m_sig2.GetData(&uTypeParamCount2));

    if (uTypeParamCount1 != uTypeParamCount2)
    {
        return E_FAIL;
    }

    // Get arg count
    ULONG cArgs1, cArgs2;
    IfFailRet(m_sig1.GetData(&cArgs1));
    IfFailRet(m_sig2.GetData(&cArgs2));

    if (cArgs1 != cArgs2)
    {
        return E_FAIL;
    }

    // Out parameter
    cArgs = cArgs1;

    return S_OK;
}

//*****************************************************************************
//*****************************************************************************

#ifdef FEATURE_FUSION
HRESULT
UnifiedAssemblySigComparer::_CreateIAssemblyNameFromAssemblyRef(
    mdToken         tkAsmRef,
    IAssemblyName **ppAsmName)
{
    HRESULT hr;

    void const *        pvPublicKey;
    ULONG               cbPublicKey;
    ULONG               cchName;
    ASSEMBLYMETADATA    amd;
    void const *        pvHashValue;
    ULONG               cbHashValue;
    DWORD               dwFlags;

    ZeroMemory(&amd, sizeof(amd));
    
    IfFailRet(m_pRegMeta->GetAssemblyRefProps(tkAsmRef,
                                  NULL,
                                  NULL,
                                  NULL,
                                  0,
                                  &cchName,
                                  &amd,
                                  NULL,
                                  NULL,
                                  NULL));

    StackSString ssName;
    StackSString ssLocale;
    amd.szLocale = ssLocale.OpenUnicodeBuffer(amd.cbLocale);

    IfFailRet(m_pRegMeta->GetAssemblyRefProps(tkAsmRef,
                                  &pvPublicKey,
                                  &cbPublicKey,
                                  ssName.OpenUnicodeBuffer(cchName),
                                  cchName,
                                  &cchName,
                                  &amd,
                                  &pvHashValue,
                                  &cbHashValue,
                                  &dwFlags));

    ssName.CloseBuffer();
    ssLocale.CloseBuffer();

    IAssemblyName *pAsmName = NULL;

    IfFailRet(CreateAssemblyNameObject(&pAsmName,
                                       ssName.GetUnicode(),
                                       CANOF_SET_DEFAULT_VALUES,
                                       NULL));

    // Set the public key token
    IfFailRet(pAsmName->SetProperty(ASM_NAME_PUBLIC_KEY_TOKEN,
                                    (LPVOID)pvPublicKey,
                                    cbPublicKey));

    // Set the culture
    if (amd.cbLocale == 0 || amd.szLocale == NULL)
    {
        IfFailRet(pAsmName->SetProperty(ASM_NAME_CULTURE,
                                        W("Neutral"),
                                        sizeof(W("Neutral"))));
    }
    else
    {
        IfFailRet(pAsmName->SetProperty(ASM_NAME_CULTURE,
                                        amd.szLocale,
                                        amd.cbLocale));
    }

    // Set the major version
    IfFailRet(pAsmName->SetProperty(ASM_NAME_MAJOR_VERSION,
                                    &amd.usMajorVersion,
                                    sizeof(amd.usMajorVersion)));

    // Set the minor version
    IfFailRet(pAsmName->SetProperty(ASM_NAME_MINOR_VERSION,
                                    &amd.usMinorVersion,
                                    sizeof(amd.usMinorVersion)));

    // Set the build number
    IfFailRet(pAsmName->SetProperty(ASM_NAME_BUILD_NUMBER,
                                    &amd.usBuildNumber,
                                    sizeof(amd.usBuildNumber)));

    // Set the revision number
    IfFailRet(pAsmName->SetProperty(ASM_NAME_REVISION_NUMBER,
                                    &amd.usRevisionNumber,
                                    sizeof(amd.usRevisionNumber)));

    *ppAsmName = pAsmName;

    return S_OK;
}

//*****************************************************************************
// Define holder to release IAssemblyName on exception.
//*****************************************************************************
void UnifiedAssemblySigComparer_IAssemblyNameRelease(IAssemblyName *value)
{
    if (value != NULL)
    {
        value->Release();
    }
}

typedef Holder<IAssemblyName*,
               DoNothing<IAssemblyName*>,
               &UnifiedAssemblySigComparer_IAssemblyNameRelease,
               NULL> UnifiedAssemblySigComparer_IAssemblyNameHolder;

#endif // FEATURE_FUSION

#ifndef FEATURE_FUSION
HRESULT UnifiedAssemblySigComparer::_CompareAssemblies(mdToken tkAsmRef1,mdToken tkAsmRef2, BOOL* pfEquivalent)
{

    HRESULT hr;
    void const *        pvPublicKey1;
    ULONG               cbPublicKey1;
    ULONG               cchName1;
    ASSEMBLYMETADATA    amd1;
    void const *        pvHashValue;
    ULONG               cbHashValue;
    DWORD               dwFlags1;

    void const *        pvPublicKey2;
    ULONG               cbPublicKey2;
    ULONG               cchName2;
    ASSEMBLYMETADATA    amd2;
    DWORD               dwFlags2;


    ZeroMemory(&amd1, sizeof(amd1));
    ZeroMemory(&amd2, sizeof(amd2));
    
    IfFailRet(m_pRegMeta->GetAssemblyRefProps(tkAsmRef1,
                                  NULL,
                                  NULL,
                                  NULL,
                                  0,
                                  &cchName1,
                                  &amd1,
                                  NULL,
                                  NULL,
                                  NULL));

    StackSString ssName1;
    StackSString ssLocale1;
    amd1.szLocale = ssLocale1.OpenUnicodeBuffer(amd1.cbLocale);

    IfFailRet(m_pRegMeta->GetAssemblyRefProps(tkAsmRef1,
                                  &pvPublicKey1,
                                  &cbPublicKey1,
                                  ssName1.OpenUnicodeBuffer(cchName1),
                                  cchName1,
                                  &cchName1,
                                  &amd1,
                                  &pvHashValue,
                                  &cbHashValue,
                                  &dwFlags1));

    ssName1.CloseBuffer();
    ssLocale1.CloseBuffer();

    IfFailRet(m_pRegMeta->GetAssemblyRefProps(tkAsmRef2,
                                  NULL,
                                  NULL,
                                  NULL,
                                  0,
                                  &cchName2,
                                  &amd2,
                                  NULL,
                                  NULL,
                                  NULL));

    StackSString ssName2;
    StackSString ssLocale2;
    amd2.szLocale = ssLocale2.OpenUnicodeBuffer(amd2.cbLocale);

    IfFailRet(m_pRegMeta->GetAssemblyRefProps(tkAsmRef2,
                                  &pvPublicKey2,
                                  &cbPublicKey2,
                                  ssName2.OpenUnicodeBuffer(cchName2),
                                  cchName2,
                                  &cchName2,
                                  &amd2,
                                  &pvHashValue,
                                  &cbHashValue,
                                  &dwFlags2));

    ssName2.CloseBuffer();
    ssLocale2.CloseBuffer();

    StackSString sMscorlib(W("mscorlib"));


    if(ssName1.CompareCaseInsensitive(sMscorlib)==0 &&
        ssName2.CompareCaseInsensitive(sMscorlib)==0 ) 
    {
        *pfEquivalent=TRUE;
        return S_OK;
    }

    *pfEquivalent=FALSE;
    
    if (ssName1.CompareCaseInsensitive(ssName2)!=0)
        return S_OK;
    if (ssLocale1.CompareCaseInsensitive(ssLocale2)!=0)
        return S_OK;
    if(cbPublicKey1!=cbPublicKey2)
        return S_OK;
    if(memcmp(pvPublicKey1,pvPublicKey2,cbPublicKey1)!=0)
        return S_OK;
    if(dwFlags1!=dwFlags2)
        return S_OK;
    if(amd1.usMajorVersion!=amd2.usMajorVersion)
        return S_OK;
    if(amd1.usMinorVersion!=amd2.usMinorVersion)
        return S_OK;
    if(amd1.usBuildNumber!=amd2.usBuildNumber)
        return S_OK;
    if(amd1.usRevisionNumber!=amd2.usRevisionNumber)
        return S_OK;
    
    *pfEquivalent=TRUE;
    return S_OK;

};
#endif // FEATURE_FUSION

//*****************************************************************************
//*****************************************************************************
HRESULT
UnifiedAssemblySigComparer::_CreateTypeNameFromTypeRef(
    mdToken tkTypeRef,
    SString &ssName,
    mdToken &tkParent)
{
    HRESULT hr;

    // Get the parent token as well as the name, and return.
    ULONG cchTypeRef;
    IfFailRet(m_pRegMeta->GetTypeRefProps(tkTypeRef, NULL, NULL, 0, &cchTypeRef));
    IfFailRet(m_pRegMeta->GetTypeRefProps(tkTypeRef, &tkParent, ssName.OpenUnicodeBuffer(cchTypeRef), cchTypeRef, NULL));
    ssName.CloseBuffer();

    return S_OK;
}

//*****************************************************************************
//*****************************************************************************
HRESULT
UnifiedAssemblySigComparer::_CreateFullyQualifiedTypeNameFromTypeRef(
    mdToken tkTypeRef,
    SString &ssFullName,
    mdToken &tkParent)
{
    HRESULT hr;

    StackSString ssBuf;
    StackSString ssName;
    mdToken tok = tkTypeRef;
    BOOL fFirstLoop = TRUE;

    // Loop stops at first non-typeref parent token.
    do
    {
        // Get the name for this token, as well as the parent token value.
        IfFailRet(_CreateTypeNameFromTypeRef(tok, ssName, tok));

        // If this is the first time through the loop, just assign values.
        if (fFirstLoop)
        {
            ssFullName = ssName;
            fFirstLoop = FALSE;
        }
        // If this isn't the first time through, make nested type name
        else
        {
            ns::MakeNestedTypeName(ssBuf, ssName, ssFullName);
            ssFullName = ssBuf;
        }
    } while (TypeFromToken(tok) == mdtTypeRef);

    // Assign non-typeref token parent
    tkParent = tok;

    return S_OK;
}



//*****************************************************************************
//*****************************************************************************
HRESULT
UnifiedAssemblySigComparer::CompareToken(
    const mdToken &tok1,
    const mdToken &tok2)
{
    HRESULT hr;

    // Check binary equality
    if (tok1 == tok2)
    {
        return S_OK;
    }

    // Currently only want to do extra checking on TypeRefs
    if (TypeFromToken(tok1) != mdtTypeRef || TypeFromToken(tok2) != mdtTypeRef)
    {
        return E_FAIL;
    }

    // Get the fully qualified type names as well as the non-typeref parents.
    mdToken tkParent1, tkParent2;
    StackSString ssName1, ssName2;

    IfFailRet(_CreateFullyQualifiedTypeNameFromTypeRef(tok1, ssName1, tkParent1));
    IfFailRet(_CreateFullyQualifiedTypeNameFromTypeRef(tok2, ssName2, tkParent2));

    // Currently only want to do extra checking if the parent tokens are AssemblyRefs
    if (TypeFromToken(tkParent1) != mdtAssemblyRef || TypeFromToken(tkParent2) != mdtAssemblyRef)
    {
        return E_FAIL;
    }

    // If the type names are not equal, no need to check the assembly refs for unification since
    // we know the types couldn't possibly match.
    if (!ssName1.Equals(ssName2))
    {
        return E_FAIL;
    }
    BOOL fEquivalent;

#ifdef FEATURE_FUSION //move into _CompareAssemblies
    IAssemblyName *pAsmName1 = NULL;
    IfFailRet(_CreateIAssemblyNameFromAssemblyRef(tkParent1, &pAsmName1));
    UnifiedAssemblySigComparer_IAssemblyNameHolder anh1(pAsmName1);

    IAssemblyName *pAsmName2 = NULL;
    IfFailRet(_CreateIAssemblyNameFromAssemblyRef(tkParent2, &pAsmName2));
    UnifiedAssemblySigComparer_IAssemblyNameHolder anh2(pAsmName2);

    DWORD cchDisplayName = 0;

    StackSString ssDisplayName1;
    pAsmName1->GetDisplayName(NULL, &cchDisplayName, NULL);
    IfFailRet(pAsmName1->GetDisplayName(ssDisplayName1.OpenUnicodeBuffer(cchDisplayName), &cchDisplayName, NULL));
    ssDisplayName1.CloseBuffer();

    StackSString ssDisplayName2;
    pAsmName2->GetDisplayName(NULL, &cchDisplayName, NULL);
    IfFailRet(pAsmName2->GetDisplayName(ssDisplayName2.OpenUnicodeBuffer(cchDisplayName), &cchDisplayName, NULL));
    ssDisplayName2.CloseBuffer();

    AssemblyComparisonResult res;
    IfFailRet(CompareAssemblyIdentity(ssDisplayName1.GetUnicode(),
                                      TRUE,
                                      ssDisplayName2.GetUnicode(),
                                      TRUE,
                                      &fEquivalent,
                                      &res));
#else
    // no redirects supported
    IfFailRet(_CompareAssemblies(tkParent1,tkParent2,&fEquivalent));
#endif

    if (!fEquivalent)
    {
        return E_FAIL;
    }

    return S_OK;
}


//*****************************************************************************
// Helper function to validate a locale.
//*****************************************************************************
static const char* const g_szValidLocale_V1[] = {
"ar","ar-SA","ar-IQ","ar-EG","ar-LY","ar-DZ","ar-MA","ar-TN","ar-OM","ar-YE","ar-SY","ar-JO","ar-LB","ar-KW","ar-AE","ar-BH","ar-QA",
"bg","bg-BG",
"ca","ca-ES",
"zh-CHS","zh-TW","zh-CN","zh-HK","zh-SG","zh-MO","zh-CHT",
"cs","cs-CZ",
"da","da-DK",
"de","de-DE","de-CH","de-AT","de-LU","de-LI",
"el","el-GR",
"en","en-US","en-GB","en-AU","en-CA","en-NZ","en-IE","en-ZA","en-JM","en-CB","en-BZ","en-TT","en-ZW","en-PH",
"es","es-ES-Ts","es-MX","es-ES","es-GT","es-CR","es-PA","es-DO","es-VE","es-CO","es-PE","es-AR","es-EC","es-CL",
"es-UY","es-PY","es-BO","es-SV","es-HN","es-NI","es-PR",
"fi","fi-FI",
"fr","fr-FR","fr-BE","fr-CA","fr-CH","fr-LU","fr-MC",
"he","he-IL",
"hu","hu-HU",
"is","is-IS",
"it","it-IT","it-CH",
"ja","ja-JP",
"ko","ko-KR",
"nl","nl-NL","nl-BE",
"no",
"nb-NO",
"nn-NO",
"pl","pl-PL",
"pt","pt-BR","pt-PT",
"ro","ro-RO",
"ru","ru-RU",
"hr","hr-HR",
"sk","sk-SK",
"sq","sq-AL",
"sv","sv-SE","sv-FI",
"th","th-TH",
"tr","tr-TR",
"ur","ur-PK",
"id","id-ID",
"uk","uk-UA",
"be","be-BY",
"sl","sl-SI",
"et","et-EE",
"lv","lv-LV",
"lt","lt-LT",
"fa","fa-IR",
"vi","vi-VN",
"hy","hy-AM",
"az",
"eu","eu-ES",
"mk","mk-MK",
"af","af-ZA",
"ka","ka-GE",
"fo","fo-FO",
"hi","hi-IN",
"ms","ms-MY","ms-BN",
"kk","kk-KZ",
"ky","ky-KZ",
"sw","sw-KE",
"uz",
"tt","tt-RU",
"pa","pa-IN",
"gu","gu-IN",
"ta","ta-IN",
"te","te-IN",
"kn","kn-IN",
"mr","mr-IN",
"sa","sa-IN",
"mn","mn-MN",
"gl","gl-ES",
"kok","kok-IN",
"syr","syr-SY",
"div"
};

static const char* const g_szValidLocale_V2[] = {
    "bn", "bn-IN",
    "bs-Latn-BA", "bs-Cyrl-BA",
    "hr-BA",
    "fil", "fil-PH",
    "fy", "fy-NL",
    "iu-Latn-CA",
    "ga", "ga-IE",
    "ky-KG",
    "lb", "lb-LU",
    "ml", "ml-IN",
    "mt", "mt-MT",
    "mi", "mi-NZ",
    "arn", "arn-CL",
    "moh", "moh-CA",
    "ne", "ne-NP",
    "ps", "ps-AF",
    "quz", "quz-BO", "quz-EC", "quz-PE",
    "rm", "rm-CH",
    "smn", "smn-FI",
    "smj" , "smj-SE", "smj-NO",
    "se", "se-NO", "se-SE", "se-FI",
    "sms", "sms-FI",
    "sma", "sma-NO", "sma-SE",
    "sr-Latn-BA", "sr-Cyrl-BA",
    "nso", "nso-ZA"
};

// Pre-vista specific cultures (renamed on Vista)
static const char* const g_szValidLocale_PreVista[] = {
    "div-MV",
    "sr-SP-Latn", "sr-SP-Cyrl",
    "az-AZ-Latn", "az-AZ-Cyrl",
    "uz-UZ-Latn", "uz-UZ-Cyrl",
};

// Vista only specific cultures (renamed and freshly introduced)
static const char * const g_szValidLocale_Vista[] = {
    "dv-MV",
    "sr-Latn-CS", "sr-Cyrl-CS",
    "az-Latn-AZ", "az-Cyrl-AZ",
    "uz-Latn-UZ", "uz-Cyrl-UZ",
    "zh-Hant", "zh-Hans",
    "gsw", "gsw-FR",
    "am", "am-ET",
    "as", "as-IN",
    "ba", "ba-RU",
    "br", "br-FR",
    "en-IN",
    "kl", "kl-GL",
    "iu-Cans-CA",
    "km", "km-KH",
    "lo", "lo-LA",
    "dsb", "dsb-DE",
    "mn-Mong-CN",
    "oc", "oc-FR",
    "or", "or-IN"
};

static BOOL FindInArray(LPCUTF8 szLocale, const char * const *cultureArr, const int nCultures)
{
    for (int i = 0; i < nCultures; i++)
    {
        if(!SString::_stricmp(szLocale, cultureArr[i]))
            return TRUE;
    }
    return FALSE;
}

#define LENGTH_OF(x) (sizeof(x) / sizeof(x[0]))

// For Everett assemblies, only the preVista cultures are valid even if running on Vista.
static BOOL _IsValidLocale(LPCUTF8 szLocale,
                           BOOL    fIsV2Assembly)
{
    if (szLocale && *szLocale)
    {
        // Locales valid for Everett and Whidbey
        if (FindInArray(szLocale, g_szValidLocale_V1, LENGTH_OF(g_szValidLocale_V1)))
            return TRUE;
        
        // Locales valid for Whidbey assemblies only
        if (fIsV2Assembly &&
            FindInArray(szLocale, g_szValidLocale_V2, LENGTH_OF(g_szValidLocale_V2)))
            return TRUE;
        
        // Finally search OS specific cultures
        if (fIsV2Assembly)
            return FindInArray(szLocale, g_szValidLocale_Vista, LENGTH_OF(g_szValidLocale_Vista));
        else
            return FindInArray(szLocale, g_szValidLocale_PreVista, LENGTH_OF(g_szValidLocale_PreVista));
    }

    return TRUE;
}

#endif //FEATURE_METADATA_VALIDATOR
