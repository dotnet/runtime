// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"
#include "vars.hpp"
#include "excep.h"
#include "interoputil.h"
#include "cachelinealloc.h"
#include "comutilnative.h"
#include "field.h"
#include "guidfromname.h"
#include "eeconfig.h"
#include "mlinfo.h"
#include "comdelegate.h"
#include "appdomain.hpp"
#include "prettyprintsig.h"
#include "util.hpp"
#include "interopconverter.h"
#include "wrappers.h"
#include "invokeutil.h"
#include "comcallablewrapper.h"
#include "../md/compiler/custattr.h"
#include "siginfo.hpp"
#include "eemessagebox.h"
#include "finalizerthread.h"
#include "interoplibinterface.h"

#ifdef FEATURE_COMINTEROP
#include "cominterfacemarshaler.h"
#endif

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
#include "olecontexthelpers.h"
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

#ifdef FEATURE_COMINTEROP
#include "dispex.h"
#include "runtimecallablewrapper.h"
#include "comtoclrcall.h"
#include "clrtocomcall.h"
#include "comcache.h"
#include "commtmemberinfomap.h"
#include "olevariant.h"
#include "stdinterfaces.h"
#include "notifyexternals.h"
#include "typeparse.h"
#include "interoputil.inl"
#include "typestring.h"

#define STANDARD_DISPID_PREFIX              W("[DISPID")
#define STANDARD_DISPID_PREFIX_LENGTH       7
#define GET_ENUMERATOR_METHOD_NAME          W("GetEnumerator")

#ifdef _DEBUG
    VOID IntializeInteropLogging();
#endif

struct ByrefArgumentInfo
{
    BOOL        m_bByref;
    VARIANT     m_Val;
};

// Flag indicating if COM support has been initialized.
BOOL    g_fComStarted = FALSE;

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
void AllocateComClassObject(ComClassFactory* pComClsFac, OBJECTREF* pComObj);
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

#endif // FEATURE_COMINTEROP


//------------------------------------------------------------------
// setup error info for exception object
//
#ifdef FEATURE_COMINTEROP

// HRESULT for CLR created IErrorInfo pointers are accessible
// from the enclosing simple wrapper
// This is in-proc only.
HRESULT GetHRFromCLRErrorInfo(IErrorInfo* pErr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pErr));
        PRECONDITION(IsInProcCCWTearOff(pErr));
        PRECONDITION(IsSimpleTearOff(pErr));
    }
    CONTRACTL_END;

    SimpleComCallWrapper* pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pErr);
    return pSimpleWrap->IErrorInfo_hr();
}
#endif // FEATURE_COMINTEROP

HRESULT SetupErrorInfo(OBJECTREF pThrownObject)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    HRESULT hr = E_FAIL;

#ifdef FEATURE_COMINTEROP
    Exception* pException = NULL;
#endif

    GCPROTECT_BEGIN(pThrownObject)
    {
        EX_TRY
        {
            // Calls to COM up ahead.
            hr = EnsureComStartedNoThrow();
            if (SUCCEEDED(hr) && pThrownObject != NULL)
            {
#ifdef _DEBUG
                EX_TRY
                {
                    StackSString message;
                    GetExceptionMessage(pThrownObject, message);

                    if (g_pConfig->ShouldExposeExceptionsInCOMToConsole())
                    {
                        PrintToStdOutW(W(".NET exception in COM\n"));
                        if (!message.IsEmpty())
                            PrintToStdOutW(message.GetUnicode());
                        else
                            PrintToStdOutW(W("No exception info available"));
                    }

                    if (g_pConfig->ShouldExposeExceptionsInCOMToMsgBox())
                    {
                        GCX_PREEMP();
                        if (!message.IsEmpty())
                            EEMessageBoxNonLocalizedDebugOnly((LPWSTR)message.GetUnicode(), W(".NET exception in COM"), MB_ICONSTOP | MB_OK);
                        else
                            EEMessageBoxNonLocalizedDebugOnly(W("No exception information available"), W(".NET exception in COM"),MB_ICONSTOP | MB_OK);
                    }
                }
                EX_CATCH
                {
                }
                EX_END_CATCH (SwallowAllExceptions);
#endif

#ifdef FEATURE_COMINTEROP
                IErrorInfo* pErr = NULL;
                EX_TRY
                {
                    // set the error info object for the exception that was thrown.
                    pErr = (IErrorInfo *)GetComIPFromObjectRef(&pThrownObject, IID_IErrorInfo);
                    {
                        GCX_PREEMP();
                        SetErrorInfo(0, pErr);
                    }

                    // Release the pErr in case it exists.
                    if (pErr)
                    {
                        hr = GetHRFromCLRErrorInfo(pErr);
                        ULONG cbRef = SafeRelease(pErr);
                        LogInteropRelease(pErr, cbRef, "IErrorInfo");
                    }
                }
                EX_CATCH
                {
                    hr = GET_EXCEPTION()->GetHR();
                }
                EX_END_CATCH(SwallowAllExceptions);
#endif // FEATURE_COMINTEROP
            }
        }
        EX_CATCH
        {
            if (SUCCEEDED(hr))
                hr = E_FAIL;
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
    GCPROTECT_END();
    return hr;
}

//-------------------------------------------------------------------
 // Used to populate ExceptionData with COM data
//-------------------------------------------------------------------
void FillExceptionData(
    _Inout_ ExceptionData* pedata,
    _In_ IErrorInfo* pErrInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pedata));
    }
    CONTRACTL_END;

    if (pErrInfo != NULL)
    {
        Thread* pThread = GetThreadNULLOk();
        if (pThread != NULL)
        {
            GCX_PREEMP();

            pErrInfo->GetSource (&pedata->bstrSource);
            pErrInfo->GetDescription (&pedata->bstrDescription);
            pErrInfo->GetHelpFile (&pedata->bstrHelpFile);
            pErrInfo->GetHelpContext (&pedata->dwHelpContext );
            pErrInfo->GetGUID(&pedata->guid);
            ULONG cbRef = SafeRelease(pErrInfo); // release the IErrorInfo interface pointer
            LogInteropRelease(pErrInfo, cbRef, "IErrorInfo");
        }
    }
}

//---------------------------------------------------------------------------
// If pImport has the DefaultDllImportSearchPathsAttribute,
// set the value of the attribute in pDlImportSearchPathFlags and return true.
BOOL GetDefaultDllImportSearchPathsAttributeValue(Module *pModule, mdToken token, DWORD * pDllImportSearchPathFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    BYTE* pData = NULL;
    LONG cData = 0;

    HRESULT hr = pModule->GetCustomAttribute(token,
                                            WellKnownAttribute::DefaultDllImportSearchPaths,
                                            (const VOID **)(&pData),
                                            (ULONG *)&cData);

    IfFailThrow(hr);
    if(cData == 0 )
    {
        return FALSE;
    }

    CustomAttributeParser ca(pData, cData);
    CaArg args[1];
    args[0].InitEnum(SERIALIZATION_TYPE_U4, (ULONG)0);

    ParseKnownCaArgs(ca, args, ARRAY_SIZE(args));
    *pDllImportSearchPathFlags = args[0].val.u4;
    return TRUE;
}


//---------------------------------------------------------------------------
// Returns the index of the LCID parameter if one exists and -1 otherwise.
int GetLCIDParameterIndex(MethodDesc *pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    int             iLCIDParam = -1;
    HRESULT         hr;
    const BYTE *    pVal;
    ULONG           cbVal;

    // Check to see if the method has the LCIDConversionAttribute.
    hr = pMD->GetCustomAttribute(WellKnownAttribute::LCIDConversion, (const void**)&pVal, &cbVal);
    if (hr == S_OK)
    {
        CustomAttributeParser caLCID(pVal, cbVal);
        CaArg args[1];
        args[0].Init(SERIALIZATION_TYPE_I4, 0);
        IfFailGo(ParseKnownCaArgs(caLCID, args, ARRAY_SIZE(args)));
        iLCIDParam = args[0].val.i4;
    }

ErrExit:
    return iLCIDParam;
}

//---------------------------------------------------------------------------
// Transforms an LCID into a CultureInfo.
void GetCultureInfoForLCID(LCID lcid, OBJECTREF *pCultureObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pCultureObj));
    }
    CONTRACTL_END;

    OBJECTREF CultureObj = NULL;
    GCPROTECT_BEGIN(CultureObj)
    {
        // Allocate a CultureInfo with the specified LCID.
        CultureObj = AllocateObject(CoreLibBinder::GetClass(CLASS__CULTURE_INFO));

        MethodDescCallSite cultureInfoCtor(METHOD__CULTURE_INFO__INT_CTOR, &CultureObj);

        // Call the CultureInfo(int culture) constructor.
        ARG_SLOT pNewArgs[] = {
            ObjToArgSlot(CultureObj),
            (ARG_SLOT)lcid
        };
        cultureInfoCtor.Call(pNewArgs);

        // Set the returned culture object.
        *pCultureObj = CultureObj;
    }
    GCPROTECT_END();
}


//---------------------------------------------------------------------------
// This method determines if a member is visible from COM.
BOOL IsMemberVisibleFromCom(MethodTable *pDeclaringMT, mdToken tk, mdMethodDef mdAssociate)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDeclaringMT));
    }
    CONTRACTL_END;

    HRESULT                 hr;
    const BYTE *            pVal;
    ULONG                   cbVal;
    DWORD                   dwFlags;

    IMDInternalImport *pInternalImport = pDeclaringMT->GetMDImport();
    Module *pModule = pDeclaringMT->GetModule();

    // Check to see if the member is public.
    switch (TypeFromToken(tk))
    {
        case mdtFieldDef:
            _ASSERTE(IsNilToken(mdAssociate));
            if (FAILED(pInternalImport->GetFieldDefProps(tk, &dwFlags)))
            {
                return FALSE;
            }
            if (!IsFdPublic(dwFlags))
                return FALSE;
            break;

        case mdtMethodDef:
            _ASSERTE(IsNilToken(mdAssociate));
            if (FAILED(pInternalImport->GetMethodDefProps(tk, &dwFlags)))
            {
                return FALSE;
            }
            if (!IsMdPublic(dwFlags))
            {
                return FALSE;
            }
            {
                // Generic Methods are not visible from COM
                MDEnumHolder hEnumTyPars(pInternalImport);
                if (FAILED(pInternalImport->EnumInit(mdtGenericParam, tk, &hEnumTyPars)))
                    return FALSE;

                if (pInternalImport->EnumGetCount(&hEnumTyPars) != 0)
                    return FALSE;
            }
            break;

        case mdtProperty:
            _ASSERTE(!IsNilToken(mdAssociate));
            if (FAILED(pInternalImport->GetMethodDefProps(mdAssociate, &dwFlags)))
            {
                return FALSE;
            }
            if (!IsMdPublic(dwFlags))
                return FALSE;

            // Check to see if the associate has the ComVisible attribute set
            hr = pModule->GetCustomAttribute(mdAssociate, WellKnownAttribute::ComVisible, (const void**)&pVal, &cbVal);
            if (hr == S_OK)
            {
                CustomAttributeParser cap(pVal, cbVal);
                if (FAILED(cap.SkipProlog()))
                    return FALSE;

                UINT8 u1;
                if (FAILED(cap.GetU1(&u1)))
                    return FALSE;

                return (BOOL)u1;
            }
            break;

        default:
            _ASSERTE(!"The type of the specified member is not handled by IsMemberVisibleFromCom");
            break;
    }

    // Check to see if the member has the ComVisible attribute set (non-WinRT members only).
    hr = pModule->GetCustomAttribute(tk, WellKnownAttribute::ComVisible, (const void**)&pVal, &cbVal);
    if (hr == S_OK)
    {
        CustomAttributeParser cap(pVal, cbVal);
        if (FAILED(cap.SkipProlog()))
            return FALSE;

        UINT8 u1;
        if (FAILED(cap.GetU1(&u1)))
            return FALSE;

        return (BOOL)u1;
    }

    // The member is visible.
    return TRUE;
}

// This method checks whether the given IErrorInfo is actually a managed CLR object.
BOOL IsManagedObject(IUnknown *pIUnknown)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pIUnknown));
    }
    CONTRACTL_END;
#if FEATURE_COMINTEROP
    //Check based on IUnknown slots, i.e. we'll see whether the IP maps to a CCW.
    if (MapIUnknownToWrapper(pIUnknown) != NULL)
    {
        // We found an existing CCW hence this is a managed exception.
        return TRUE;
    }
#endif
    return FALSE;
}

ULONG GetStringizedMethodDef(MethodTable *pDeclaringMT, mdToken tkMb, CQuickArray<BYTE> &rDef, ULONG cbCur)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDeclaringMT));
    }
    CONTRACTL_END;

    IMDInternalImport *pMDImport = pDeclaringMT->GetMDImport();
    CQuickBytes     rSig;
    MDEnumHolder    ePm(pMDImport);         // For enumerating  params.
    mdParamDef      tkPm;                   // A param token.
    DWORD           dwFlags;                // Param flags.
    USHORT          usSeq;                  // Sequence of a parameter.
    ULONG           cPm;                    // Count of params.
    PCCOR_SIGNATURE pSig;
    ULONG           cbSig;

    // Don't count invisible members.
    if (!IsMemberVisibleFromCom(pDeclaringMT, tkMb, mdMethodDefNil))
        return cbCur;

    // accumulate the signatures.
    IfFailThrow(pMDImport->GetSigOfMethodDef(tkMb, &cbSig, &pSig));
    IfFailThrow(::PrettyPrintSigInternalLegacy(pSig, cbSig, "", &rSig, pMDImport));

    // Get the parameter flags.
    IfFailThrow(pMDImport->EnumInit(mdtParamDef, tkMb, &ePm));
    cPm = pMDImport->EnumGetCount(&ePm);

    // Resize for sig and params.  Just use 1 byte of param.
    rDef.ReSizeThrows(cbCur + rSig.Size() + cPm);
    memcpy(rDef.Ptr() + cbCur, rSig.Ptr(), rSig.Size());
    cbCur += (ULONG)(rSig.Size()-1);

    // Enumerate through the params and get the flags.
    while (pMDImport->EnumNext(&ePm, &tkPm))
    {
        LPCSTR szParamName_Ignore;
        IfFailThrow(pMDImport->GetParamDefProps(tkPm, &usSeq, &dwFlags, &szParamName_Ignore));
        if (usSeq == 0)     // Skip return type flags.
            continue;
        rDef[cbCur++] = (BYTE)dwFlags;
    }

    // Return the number of bytes.
    return cbCur;
} // void GetStringizedMethodDef()


ULONG GetStringizedFieldDef(MethodTable *pDeclaringMT, mdToken tkMb, CQuickArray<BYTE> &rDef, ULONG cbCur)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDeclaringMT));
    }
    CONTRACTL_END;

    CQuickBytes         rSig;
    PCCOR_SIGNATURE     pSig;
    ULONG               cbSig;

    // Don't count invisible members.
    if (!IsMemberVisibleFromCom(pDeclaringMT, tkMb, mdMethodDefNil))
        return cbCur;

    IMDInternalImport *pMDImport = pDeclaringMT->GetMDImport();

    // accumulate the signatures.
    IfFailThrow(pMDImport->GetSigOfFieldDef(tkMb, &cbSig, &pSig));
    IfFailThrow(::PrettyPrintSigInternalLegacy(pSig, cbSig, "", &rSig, pMDImport));
    rDef.ReSizeThrows(cbCur + rSig.Size());
    memcpy(rDef.Ptr() + cbCur, rSig.Ptr(), rSig.Size());
    cbCur += (ULONG)(rSig.Size()-1);

    // Return the number of bytes.
    return cbCur;
} // void GetStringizedFieldDef()

//--------------------------------------------------------------------------------
// This method generates a stringized version of an interface that contains the
// name of the interface along with the signature of all the methods.
SIZE_T GetStringizedItfDef(TypeHandle InterfaceType, CQuickArray<BYTE> &rDef)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodTable* pIntfMT = InterfaceType.GetMethodTable();
    PREFIX_ASSUME(pIntfMT != NULL);

    IMDInternalImport* pMDImport = pIntfMT->GetMDImport();
    PREFIX_ASSUME(pMDImport != NULL);

    LPCWSTR             szName;
    ULONG               cchName;
    MDEnumHolder        eMb(pMDImport);                         // For enumerating methods and fields.
    mdToken             tkMb;                                   // A method or field token.
    SIZE_T              cbCur;

    // Make sure the specified type is an interface with a valid token.
    _ASSERTE(!IsNilToken(pIntfMT->GetCl()) && pIntfMT->IsInterface());

    // Get the name of the class.
    DefineFullyQualifiedNameForClassW();
    szName = GetFullyQualifiedNameForClassNestedAwareW(pIntfMT);

    cchName = (ULONG)wcslen(szName);

    // Start with the interface name.
    cbCur = cchName * sizeof(WCHAR);
    rDef.ReSizeThrows(cbCur + sizeof(WCHAR));
    wcscpy_s(reinterpret_cast<LPWSTR>(rDef.Ptr()), rDef.Size()/sizeof(WCHAR), szName);

    // Enumerate the methods...
    IfFailThrow(pMDImport->EnumInit(mdtMethodDef, pIntfMT->GetCl(), &eMb));
    while(pMDImport->EnumNext(&eMb, &tkMb))
    {   // accumulate the signatures.
        cbCur = GetStringizedMethodDef(pIntfMT, tkMb, rDef, (ULONG)cbCur);
    }
    pMDImport->EnumClose(&eMb);

    // Enumerate the fields...
    IfFailThrow(pMDImport->EnumInit(mdtFieldDef, pIntfMT->GetCl(), &eMb));
    while(pMDImport->EnumNext(&eMb, &tkMb))
    {   // accumulate the signatures.
        cbCur = GetStringizedFieldDef(pIntfMT, tkMb, rDef, (ULONG)cbCur);
    }

    // Return the number of bytes.
    return cbCur;
} // ULONG GetStringizedItfDef()

//--------------------------------------------------------------------------------
// Helper to get the stringized form of typelib guid.
HRESULT GetStringizedTypeLibGuidForAssembly(Assembly *pAssembly, CQuickArray<BYTE> &rDef, ULONG cbCur, ULONG *pcbFetched)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(pcbFetched));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;              // A result.
    LPCUTF8     pszName = NULL;         // Library name in UTF8.
    ULONG       cbName;                 // Length of name, UTF8 characters.
    LPWSTR      pName;                  // Pointer to library name.
    ULONG       cchName;                // Length of name, wide chars.
    LPWSTR      pch=0;                  // Pointer into lib name.
    const void  *pSN=NULL;              // Pointer to public key.
    DWORD       cbSN=0;                 // Size of public key.
    USHORT      usMajorVersion;         // The major version number.
    USHORT      usMinorVersion;         // The minor version number.
    USHORT      usBuildNumber;          // The build number.
    USHORT      usRevisionNumber;       // The revision number.
    const BYTE  *pbData = NULL;         // Pointer to a custom attribute data.
    ULONG       cbData = 0;             // Size of custom attribute data.
    static char szTypeLibKeyName[] = {"TypeLib"};

    // Get the name, and determine its length.
    pszName = pAssembly->GetSimpleName();
    cbName=(ULONG)strlen(pszName);
    cchName = WszMultiByteToWideChar(CP_ACP,0, pszName,cbName+1, 0,0);

    // See if there is a public key.
    EX_TRY
    {
        pSN = pAssembly->GetPublicKey(&cbSN);
    }
    EX_CATCH
    {
        IfFailGo(COR_E_BADIMAGEFORMAT);
    }
    EX_END_CATCH(RethrowTerminalExceptions)


#ifdef FEATURE_COMINTEROP
    // If the ComCompatibleVersionAttribute is set, then use the version
    // number in the attribute when generating the GUID.
    IfFailGo(pAssembly->GetCustomAttribute(TokenFromRid(1, mdtAssembly), WellKnownAttribute::ComCompatibleVersion, (const void**)&pbData, &cbData));


    if (hr == S_OK && cbData >= (2 + 4 * sizeof(INT32)))
    {
        CustomAttributeParser cap(pbData, cbData);
        IfFailRet(cap.SkipProlog());

        // Retrieve the major and minor version from the attribute.
        UINT32 u4;

        IfFailRet(cap.GetU4(&u4));
        usMajorVersion = GET_VERSION_USHORT_FROM_INT(u4);
        IfFailRet(cap.GetU4(&u4));
        usMinorVersion = GET_VERSION_USHORT_FROM_INT(u4);
        IfFailRet(cap.GetU4(&u4));
        usBuildNumber = GET_VERSION_USHORT_FROM_INT(u4);
        IfFailRet(cap.GetU4(&u4));
        usRevisionNumber = GET_VERSION_USHORT_FROM_INT(u4);
    }
    else
#endif // FEATURE_COMINTEROP
    {
        pAssembly->GetVersion(&usMajorVersion, &usMinorVersion, &usBuildNumber, &usRevisionNumber);
    }

    // Get the version information.
    struct  versioninfo
    {
        USHORT      usMajorVersion;         // Major Version.
        USHORT      usMinorVersion;         // Minor Version.
        USHORT      usBuildNumber;          // Build Number.
        USHORT      usRevisionNumber;       // Revision Number.
    } ver;

    // <REVISIT_TODO> An issue here is that usMajor is used twice and usMinor not at all.
    //  We're not fixing that because everyone has a major version, so all the
    //  generated guids would change, which is breaking.  To compensate, if
    //  the minor is non-zero, we add it separately, below.</REVISIT_TODO>
    ver.usMajorVersion = usMajorVersion;
    ver.usMinorVersion =  usMajorVersion;  // Don't fix this line!
    ver.usBuildNumber =  usBuildNumber;
    ver.usRevisionNumber =  usRevisionNumber;

    // Resize the output buffer.
    IfFailGo(rDef.ReSizeNoThrow(cbCur + cchName*sizeof(WCHAR) + sizeof(szTypeLibKeyName)-1 + cbSN + sizeof(ver)+sizeof(USHORT)));

    // Put it all together.  Name first.
    WszMultiByteToWideChar(CP_ACP,0, pszName,cbName+1, (LPWSTR)(&rDef[cbCur]),cchName);
    pName = (LPWSTR)(&rDef[cbCur]);
    for (pch=pName; *pch; ++pch)
        if (*pch == '.' || *pch == ' ')
            *pch = '_';
    else
        if (iswupper(*pch))
            *pch = towlower(*pch);
    cbCur += (cchName-1)*sizeof(WCHAR);
    memcpy(&rDef[cbCur], szTypeLibKeyName, sizeof(szTypeLibKeyName)-1);
    cbCur += sizeof(szTypeLibKeyName)-1;

    // Version.
    memcpy(&rDef[cbCur], &ver, sizeof(ver));
    cbCur += sizeof(ver);

    // If minor version is non-zero, add it to the hash.  It should have been in the ver struct,
    //  but due to a bug, it was omitted there, and fixing it "right" would have been
    //  breaking.  So if it isn't zero, add it; if it is zero, don't add it.  Any
    //  possible value of minor thus generates a different guid, and a value of 0 still generates
    //  the guid that the original, buggy, code generated.
    if (usMinorVersion != 0)
    {
        SET_UNALIGNED_16(&rDef[cbCur], usMinorVersion);
        cbCur += sizeof(USHORT);
    }

    // Public key.
    memcpy(&rDef[cbCur], pSN, cbSN);
    cbCur += cbSN;

    if (pcbFetched)
        *pcbFetched = cbCur;

ErrExit:
    return hr;
}

#include <optsmallperfcritical.h>
//--------------------------------------------------------------------------------
// Release helper, must be called in preemptive mode.  Only use this variant if
// you already know you're in preemptive mode for other reasons.
ULONG SafeReleasePreemp(IUnknown * pUnk, RCW * pRCW)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk, NULL_OK));
    } CONTRACTL_END;

    if (pUnk == NULL)
        return 0;

    // Message pump could happen, so arbitrary managed code could run.
    CONTRACT_VIOLATION(ThrowsViolation | FaultViolation);

    return pUnk->Release();
}

//--------------------------------------------------------------------------------
// Release helper, enables and disables GC during call-outs
ULONG SafeRelease(IUnknown* pUnk, RCW* pRCW)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pUnk, NULL_OK));
    } CONTRACTL_END;

    if (pUnk == NULL)
        return 0;

    ULONG res = 0;
    Thread * const pThread = GetThreadNULLOk();
    GCX_PREEMP_NO_DTOR_HAVE_THREAD(pThread);

    // Message pump could happen, so arbitrary managed code could run.
    CONTRACT_VIOLATION(ThrowsViolation | FaultViolation);

    res = pUnk->Release();

    GCX_PREEMP_NO_DTOR_END();

    return res;
}

#include <optdefault.h>

//--------------------------------------------------------------------------------
// Determines if a COM object can be cast to the specified type.
BOOL CanCastComObject(OBJECTREF obj, MethodTable * pTargetMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (!obj)
        return TRUE;

    if (pTargetMT->IsInterface())
    {
        return Object::SupportsInterface(obj, pTargetMT);
    }
    else
    {
        return obj->GetMethodTable()->CanCastToClass(pTargetMT);
    }
}

// Returns TRUE iff the argument represents the "__ComObject" type.
BOOL IsComObjectClass(TypeHandle type)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef FEATURE_COMINTEROP
    if (!type.IsTypeDesc())
    {
        MethodTable *pMT = type.AsMethodTable();

        if (pMT->IsComObjectType())
        {
            // May be __ComObject or typed RCW. __ComObject must have already been loaded
            // if we see an MT marked like this so calling the *NoInit method is sufficient.

            return pMT == g_pBaseCOMObject;
        }
    }
#endif

    return FALSE;
}

VOID
ReadBestFitCustomAttribute(MethodDesc* pMD, BOOL* BestFit, BOOL* ThrowOnUnmappableChar)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ReadBestFitCustomAttribute(pMD->GetModule(),
        pMD->GetMethodTable()->GetCl(),
        BestFit, ThrowOnUnmappableChar);
}

VOID
ReadBestFitCustomAttribute(Module* pModule, mdTypeDef cl, BOOL* BestFit, BOOL* ThrowOnUnmappableChar)
{
    // Set the attributes to their defaults, just to be safe.
    *BestFit = TRUE;
    *ThrowOnUnmappableChar = FALSE;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    HRESULT     hr;
    BYTE*       pData;
    ULONG       cbCount;

    // A well-formed BestFitMapping attribute will have at least 5 bytes
    // 1,2 for the prolog (should be 0x1, 0x0)
    // 3 for the BestFitMapping bool
    // 4,5 for the number of named parameters (will be 0 if ThrowOnUnmappableChar doesn't exist)
    // 6 - 29 for the description of ThrowOnUnmappableChar
    // 30 for the ThrowOnUnmappableChar bool

    // Try the assembly first
    hr = pModule->GetCustomAttribute(TokenFromRid(1, mdtAssembly), WellKnownAttribute::BestFitMapping, (const VOID**)(&pData), &cbCount);
    if ((hr == S_OK) && (pData) && (cbCount > 4) && (pData[0] == 1) && (pData[1] == 0))
    {
        _ASSERTE((cbCount == 5) || (cbCount == 30));

        // index to 2 to skip prolog
        *BestFit = pData[2] != 0;

        // If this parameter exists,
        if (cbCount == 30)
            // index to end of data to skip description of named argument
            *ThrowOnUnmappableChar = pData[29] != 0;
    }

    // Now try the interface/class/struct
    if (IsNilToken(cl))
        return;
    hr = pModule->GetCustomAttribute(cl, WellKnownAttribute::BestFitMapping, (const VOID**)(&pData), &cbCount);
    if ((hr == S_OK) && (pData) && (cbCount > 4) && (pData[0] == 1) && (pData[1] == 0))
    {
        _ASSERTE((cbCount == 5) || (cbCount == 30));

        // index to 2 to skip prolog
        *BestFit = pData[2] != 0;

        // If this parameter exists,
        if (cbCount == 30)
            // index to end of data to skip description of named argument
            *ThrowOnUnmappableChar = pData[29] != 0;
    }
}


int InternalWideToAnsi(_In_reads_(iNumWideChars) LPCWSTR szWideString, int iNumWideChars, _Out_writes_bytes_opt_(cbAnsiBufferSize) LPSTR szAnsiString, int cbAnsiBufferSize, BOOL fBestFit, BOOL fThrowOnUnmappableChar)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;


    if ((szWideString == 0) || (iNumWideChars == 0) || (szAnsiString == 0) || (cbAnsiBufferSize == 0))
        return 0;

    DWORD flags = 0;
    int retval;

    if (fBestFit == FALSE)
        flags = WC_NO_BEST_FIT_CHARS;

    if (fThrowOnUnmappableChar)
    {
        BOOL DefaultCharUsed = FALSE;
        retval = WszWideCharToMultiByte(CP_ACP,
                                    flags,
                                    szWideString,
                                    iNumWideChars,
                                    szAnsiString,
                                    cbAnsiBufferSize,
                                    NULL,
                                    &DefaultCharUsed);
        DWORD lastError = GetLastError();

        if (retval == 0)
        {
            INSTALL_UNWIND_AND_CONTINUE_HANDLER;
            COMPlusThrowHR(HRESULT_FROM_WIN32(lastError));
            UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
        }

        if (DefaultCharUsed)
        {
            struct HelperThrow
            {
                static void Throw()
                {
                    COMPlusThrow( kArgumentException, IDS_EE_MARSHAL_UNMAPPABLE_CHAR );
                }
            };

            ENCLOSE_IN_EXCEPTION_HANDLER( HelperThrow::Throw );
        }

    }
    else
    {
        retval = WszWideCharToMultiByte(CP_ACP,
                                    flags,
                                    szWideString,
                                    iNumWideChars,
                                    szAnsiString,
                                    cbAnsiBufferSize,
                                    NULL,
                                    NULL);
        DWORD lastError = GetLastError();

        if (retval == 0)
        {
            INSTALL_UNWIND_AND_CONTINUE_HANDLER;
            COMPlusThrowHR(HRESULT_FROM_WIN32(lastError));
            UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
        }
    }

    return retval;
}

namespace
{
    HRESULT TryParseClassInterfaceAttribute(
        _In_ Module *pModule,
        _In_ mdToken tkObj,
        _Out_ CorClassIfaceAttr *val)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
            PRECONDITION(CheckPointer(pModule));
            PRECONDITION(CheckPointer(val));
        }
        CONTRACTL_END

        const BYTE *pVal = nullptr;
        ULONG cbVal = 0;
        HRESULT hr = pModule->GetCustomAttribute(tkObj, WellKnownAttribute::ClassInterface, (const void**)&pVal, &cbVal);
        if (hr != S_OK)
        {
            *val = clsIfNone;
            return S_FALSE;
        }

        CustomAttributeParser cap(pVal, cbVal);
        if (FAILED(cap.ValidateProlog()))
            return COR_E_BADIMAGEFORMAT;

        U1 u1;
        if (FAILED(cap.GetU1(&u1)))
            return COR_E_BADIMAGEFORMAT;

        *val = (CorClassIfaceAttr)(u1);
        _ASSERTE(*val < clsIfLast);

        return S_OK;
    }
}

//---------------------------------------------------------
// Read the ClassInterfaceType custom attribute info from
// both assembly level and class level
//---------------------------------------------------------
CorClassIfaceAttr ReadClassInterfaceTypeCustomAttribute(TypeHandle type)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!type.IsInterface());
    }
    CONTRACTL_END

    CorClassIfaceAttr attrValueMaybe;

    // First look for the class interface attribute at the class level.
    HRESULT hr = TryParseClassInterfaceAttribute(type.GetModule(), type.GetCl(), &attrValueMaybe);
    if (FAILED(hr))
        ThrowHR(hr, BFA_BAD_CLASS_INT_CA_FORMAT);

    if (hr == S_FALSE)
    {
        // Check the class interface attribute at the assembly level.
        Assembly *pAssembly = type.GetAssembly();
        hr = TryParseClassInterfaceAttribute(pAssembly->GetManifestModule(), pAssembly->GetManifestToken(), &attrValueMaybe);
        if (FAILED(hr))
            ThrowHR(hr, BFA_BAD_CLASS_INT_CA_FORMAT);
    }

    if (hr == S_OK)
        return attrValueMaybe;

    return DEFAULT_CLASS_INTERFACE_TYPE;
}

//--------------------------------------------------------------------------------
// GetErrorInfo helper, enables and disables GC during call-outs
HRESULT SafeGetErrorInfo(IErrorInfo **ppIErrInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(ppIErrInfo));
    }
    CONTRACTL_END;

    *ppIErrInfo = NULL;

#ifdef FEATURE_COMINTEROP
    GCX_PREEMP();

    HRESULT hr = S_OK;
    EX_TRY
    {
        hr = GetErrorInfo(0, ppIErrInfo);
    }
    EX_CATCH
    {
        hr = E_OUTOFMEMORY;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return hr;
#else // FEATURE_COMINTEROP
    // Indicate no error object
    return S_FALSE;
#endif
}


#include <optsmallperfcritical.h>
//--------------------------------------------------------------------------------
// QI helper, enables and disables GC during call-outs
HRESULT SafeQueryInterface(IUnknown* pUnk, REFIID riid, IUnknown** pResUnk)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;
    _ASSERTE(pUnk);
    _ASSERTE(pResUnk);

    Thread * const pThread = GetThreadNULLOk();

    *pResUnk = NULL;
    HRESULT hr = E_FAIL;

    GCX_PREEMP_NO_DTOR_HAVE_THREAD(pThread);

    BEGIN_CONTRACT_VIOLATION(ThrowsViolation); // message pump could happen, so arbitrary managed code could run

    struct Param { HRESULT * const hr; IUnknown** const pUnk; REFIID riid; IUnknown*** const pResUnk; } param = { &hr, &pUnk, riid, &pResUnk };
#define PAL_TRY_ARG(argName) (*(pParam->argName))
#define PAL_TRY_REFARG(argName) (pParam->argName)
    PAL_TRY(Param * const, pParam, &param)
    {
        PAL_TRY_ARG(hr) = PAL_TRY_ARG(pUnk)->QueryInterface(PAL_TRY_REFARG(riid), (void**) PAL_TRY_ARG(pResUnk));
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
#if defined(STACK_GUARDS_DEBUG)
        // Catching and just swallowing an exception means we need to tell
        // the SO code that it should go back to normal operation, as it
        // currently thinks that the exception is still on the fly.
        GetThread()->GetCurrentStackGuard()->RestoreCurrentGuard();
#endif
    }
    PAL_ENDTRY;
#undef PAL_TRY_ARG
#undef PAL_TRY_REFARG

    END_CONTRACT_VIOLATION;

    LOG((LF_INTEROP, LL_EVERYTHING, hr == S_OK ? "QI Succeeded\n" : "QI Failed\n"));

    // Ensure if the QI returned ok that it actually set a pointer.
    if (hr == S_OK)
    {
        if (*pResUnk == NULL)
            hr = E_NOINTERFACE;
    }

    GCX_PREEMP_NO_DTOR_END();

    return hr;
}


//--------------------------------------------------------------------------------
// QI helper, must be called in preemptive mode.  Faster than the MODE_ANY version
// because it doesn't need to toggle the mode.  Use this version only if you already
// know that you're in preemptive mode for other reasons.
HRESULT SafeQueryInterfacePreemp(IUnknown* pUnk, REFIID riid, IUnknown** pResUnk)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;
    _ASSERTE(pUnk);
    _ASSERTE(pResUnk);

    Thread * const pThread = GetThreadNULLOk();

    *pResUnk = NULL;
    HRESULT hr = E_FAIL;

    BEGIN_CONTRACT_VIOLATION(ThrowsViolation); // message pump could happen, so arbitrary managed code could run

    struct Param { HRESULT * const hr; IUnknown** const pUnk; REFIID riid; IUnknown*** const pResUnk; } param = { &hr, &pUnk, riid, &pResUnk };
#define PAL_TRY_ARG(argName) (*(pParam->argName))
#define PAL_TRY_REFARG(argName) (pParam->argName)
    PAL_TRY(Param * const, pParam, &param)
    {
        PAL_TRY_ARG(hr) = PAL_TRY_ARG(pUnk)->QueryInterface(PAL_TRY_REFARG(riid), (void**) PAL_TRY_ARG(pResUnk));
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
#if defined(STACK_GUARDS_DEBUG)
        // Catching and just swallowing an exception means we need to tell
        // the SO code that it should go back to normal operation, as it
        // currently thinks that the exception is still on the fly.
        GetThread()->GetCurrentStackGuard()->RestoreCurrentGuard();
#endif
    }
    PAL_ENDTRY;
#undef PAL_TRY_ARG
#undef PAL_TRY_REFARG

    END_CONTRACT_VIOLATION;

    LOG((LF_INTEROP, LL_EVERYTHING, hr == S_OK ? "QI Succeeded\n" : "QI Failed\n"));

    // Ensure if the QI returned ok that it actually set a pointer.
    if (hr == S_OK)
    {
        if (*pResUnk == NULL)
            hr = E_NOINTERFACE;
    }

    return hr;
}
#include <optdefault.h>

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)

//--------------------------------------------------------------------------------
// Cleanup helpers
//--------------------------------------------------------------------------------
void MinorCleanupSyncBlockComData(InteropSyncBlockInfo* pInteropInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION( GCHeapUtilities::IsGCInProgress() || ( (g_fEEShutDown & ShutDown_SyncBlock) && g_fProcessDetach ) );
    }
    CONTRACTL_END;

#ifdef FEATURE_COMINTEROP
    // No need to notify the thread that the RCW is in use here.
    // This is a privileged function called during GC or shutdown.
    RCW* pRCW = pInteropInfo->GetRawRCW();
    if (pRCW)
        pRCW->MinorCleanup();
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMWRAPPERS
    void* eoc;
    if (pInteropInfo->TryGetExternalComObjectContext(&eoc))
        ComWrappersNative::MarkExternalComObjectContextCollected(eoc);
#endif // FEATURE_COMWRAPPERS
}

void CleanupSyncBlockComData(InteropSyncBlockInfo* pInteropInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if ((g_fEEShutDown & ShutDown_SyncBlock) && g_fProcessDetach )
        MinorCleanupSyncBlockComData(pInteropInfo);

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    ComClassFactory* pComClassFactory = pInteropInfo->GetComClassFactory();
    if (pComClassFactory)
    {
        delete pComClassFactory;
        pInteropInfo->SetComClassFactory(NULL);
    }
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

#ifdef FEATURE_COMINTEROP
    // No need to notify the thread that the RCW is in use here.
    // This is only called during finalization of a __ComObject so no one
    // else could have a reference to this object.
    RCW* pRCW = pInteropInfo->GetRawRCW();
    if (pRCW)
    {
        pInteropInfo->SetRawRCW(NULL);
        pRCW->Cleanup();
    }

    ComCallWrapper* pCCW = pInteropInfo->GetCCW();
    if (pCCW)
    {
        pInteropInfo->SetCCW(NULL);
        pCCW->Cleanup();
    }
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMWRAPPERS
    pInteropInfo->ClearManagedObjectComWrappers(&ComWrappersNative::DestroyManagedObjectComWrapper);

    void* eoc;
    if (pInteropInfo->TryGetExternalComObjectContext(&eoc))
    {
        (void)pInteropInfo->TrySetExternalComObjectContext(NULL, eoc);
        ComWrappersNative::DestroyExternalComObjectContext(eoc);
    }
#endif // FEATURE_COMWRAPPERS
}

#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS

#ifdef FEATURE_COMINTEROP

//--------------------------------------------------------------------------------
//  Helper to release all of the RCWs in the specified context across all caches.
//  If pCtxCookie is NULL, release all RCWs
static void ReleaseRCWsInCaches(LPVOID pCtxCookie)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pCtxCookie, NULL_OK));
    }
    CONTRACTL_END;

    // Go through all the app domains and for each one release all the
    // RCW's that live in the current context.
    AppDomainIterator i(TRUE);
    while (i.Next())
        i.GetDomain()->ReleaseRCWs(pCtxCookie);

    if (!g_fEEShutDown)
    {
        GCX_COOP();

        // If the finalizer thread has sync blocks to clean up or if it is in the process
        // of cleaning up the sync blocks, we need to wait for it to finish.
        if (FinalizerThread::GetFinalizerThread()->RequireSyncBlockCleanup() || SyncBlockCache::GetSyncBlockCache()->IsSyncBlockCleanupInProgress())
            FinalizerThread::FinalizerThreadWait();

        // If more sync blocks were added while the finalizer thread was calling the finalizers
        // or while it was transitioning into a context to clean up the IP's, we need to wake
        // it up again to have it clean up the newly added sync blocks.
        if (FinalizerThread::GetFinalizerThread()->RequireSyncBlockCleanup() || SyncBlockCache::GetSyncBlockCache()->IsSyncBlockCleanupInProgress())
            FinalizerThread::FinalizerThreadWait();
    }
}

void ReleaseRCWsInCachesNoThrow(LPVOID pCtxCookie)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pCtxCookie, NULL_OK));
    }
    CONTRACTL_END;

    EX_TRY
    {
        ReleaseRCWsInCaches(pCtxCookie);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);
}

//--------------------------------------------------------------------------------
// Marshalling Helpers
//--------------------------------------------------------------------------------


// Convert an IUnknown to CCW, returns NULL if the pUnk is not on
// a managed tear-off (OR) if the pUnk is to a managed tear-off that
// has been aggregated
ComCallWrapper* GetCCWFromIUnknown(IUnknown* pUnk, BOOL bEnableCustomization)
{
    CONTRACT (ComCallWrapper*)
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pUnk));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    ComCallWrapper* pWrap = MapIUnknownToWrapper(pUnk);
    if (pWrap != NULL)
    {
        // check if this wrapper is aggregated
        if (pWrap->GetOuter() != NULL)
        {
            pWrap = NULL;
        }
    }

    RETURN pWrap;
}

HRESULT LoadRegTypeLib(_In_ REFGUID guid,
                       _In_ unsigned short wVerMajor,
                       _In_ unsigned short wVerMinor,
                       _Outptr_ ITypeLib **pptlib)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    *pptlib = NULL;

    GCX_PREEMP();

    BSTRHolder wzPath;
    HRESULT hr = S_OK;

    EX_TRY
    {
        hr = QueryPathOfRegTypeLib(guid, wVerMajor, wVerMinor, LOCALE_USER_DEFAULT, &wzPath);
        if (SUCCEEDED(hr))
        {
#ifdef HOST_64BIT
            REGKIND rk = (REGKIND)(REGKIND_NONE | LOAD_TLB_AS_64BIT);
#else
            REGKIND rk = (REGKIND)(REGKIND_NONE | LOAD_TLB_AS_32BIT);
#endif // HOST_64BIT
            hr = LoadTypeLibEx(wzPath, rk, pptlib);
        }
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    return hr;
}

VOID EnsureComStarted(BOOL fCoInitCurrentThread)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(GetThreadNULLOk() || !fCoInitCurrentThread);
        PRECONDITION(g_fEEStarted);
    }
    CONTRACTL_END;

    if (g_fComStarted == FALSE)
    {
        FinalizerThread::GetFinalizerThread()->SetRequiresCoInitialize();

        // Attempt to set the thread's apartment model (to MTA by default). May not
        // succeed (if someone beat us to the punch). That doesn't matter (since
        // COM+ objects are now apartment agile), we only care that a CoInitializeEx
        // has been performed on this thread by us.
        if (fCoInitCurrentThread)
            GetThread()->SetApartment(Thread::AS_InMTA);

        // set the finalizer event
        FinalizerThread::EnableFinalization();

        g_fComStarted = TRUE;
    }
}

HRESULT EnsureComStartedNoThrow(BOOL fCoInitCurrentThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(g_fEEStarted);
        PRECONDITION(GetThreadNULLOk() != NULL);      // Should always be inside BEGIN_EXTERNAL_ENTRYPOINT
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    if (!g_fComStarted)
    {
        GCX_COOP();
        EX_TRY
        {
            EnsureComStarted(fCoInitCurrentThread);
        }
        EX_CATCH_HRESULT(hr);
    }

    return hr;
}

#include <optsmallperfcritical.h>
//--------------------------------------------------------------------------------
// AddRef helper, enables and disables GC during call-outs
ULONG SafeAddRef(IUnknown* pUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ULONG res = ~0;
    if (pUnk == NULL)
        return res;

    GCX_PREEMP_NO_DTOR();

    // @TODO: Consider special-casing this when we know it's one of ours so
    //        that we can avoid having to 'leave' and then 'enter'.

    CONTRACT_VIOLATION(ThrowsViolation); // arbitrary managed code could run

    res = pUnk->AddRef();

    GCX_PREEMP_NO_DTOR_END();

    return res;
}

//--------------------------------------------------------------------------------
// AddRef helper, must be called in preemptive mode.  Only use this variant if
// you already know you're in preemptive mode for other reasons.
ULONG SafeAddRefPreemp(IUnknown* pUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    ULONG res = ~0;
    if (pUnk == NULL)
        return res;

    // @TODO: Consider special-casing this when we know it's one of ours so
    //        that we can avoid having to 'leave' and then 'enter'.

    CONTRACT_VIOLATION(ThrowsViolation); // arbitrary managed code could run

    res = pUnk->AddRef();

    return res;
}
#include <optdefault.h>

//--------------------------------------------------------------------------------
// Ole RPC seems to return an inconsistent SafeArray for arrays created with
// SafeArrayVector(VT_BSTR). OleAut's SafeArrayGetVartype() doesn't notice
// the inconsistency and returns a valid-seeming (but wrong vartype.)
// Our version is more discriminating. This should only be used for
// marshaling scenarios where we can assume unmanaged code permissions
// (and hence are already in a position of trusting unmanaged data.)

HRESULT ClrSafeArrayGetVartype(_In_ SAFEARRAY *psa, _Out_ VARTYPE *pvt)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(psa));
        PRECONDITION(CheckPointer(pvt));
    }
    CONTRACTL_END;

    if (pvt == NULL || psa == NULL)
    {
       // This is the HRESULT returned by OLEAUT if either of the args are null.
       return E_INVALIDARG;
    }

    USHORT fFeatures = psa->fFeatures;
    USHORT hardwiredType = (fFeatures & (FADF_BSTR|FADF_UNKNOWN|FADF_DISPATCH|FADF_VARIANT));

    if (hardwiredType == FADF_BSTR && psa->cbElements == sizeof(BSTR))
    {
        *pvt = VT_BSTR;
        return S_OK;
    }
    else if (hardwiredType == FADF_UNKNOWN && psa->cbElements == sizeof(IUnknown*))
    {
        *pvt = VT_UNKNOWN;
        return S_OK;
    }
    else if (hardwiredType == FADF_DISPATCH && psa->cbElements == sizeof(IDispatch*))
    {
        *pvt = VT_DISPATCH;
        return S_OK;
    }
    else if (hardwiredType == FADF_VARIANT && psa->cbElements == sizeof(VARIANT))
    {
        *pvt = VT_VARIANT;
        return S_OK;
    }
    else
    {
        _ASSERTE(GetModuleHandleA("oleaut32.dll") != NULL);
        // We have got a SAFEARRAY.  Oleaut32.dll should have been loaded.
        CONTRACT_VIOLATION(ThrowsViolation);
        return ::SafeArrayGetVartype(psa, pvt);
    }
}

//--------------------------------------------------------------------------------
// // safe VariantChangeType
// Release helper, enables and disables GC during call-outs
HRESULT SafeVariantChangeType(_Inout_ VARIANT* pVarRes, _In_ VARIANT* pVarSrc,
                              unsigned short wFlags, VARTYPE vt)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pVarRes));
        PRECONDITION(CheckPointer(pVarSrc));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    if (pVarRes)
    {
        GCX_PREEMP();
        EX_TRY
        {
            hr = VariantChangeType(pVarRes, pVarSrc, wFlags, vt);
        }
        EX_CATCH
        {
            hr = GET_EXCEPTION()->GetHR();
        }
        EX_END_CATCH(SwallowAllExceptions);
    }

    return hr;
}

//--------------------------------------------------------------------------------
HRESULT SafeVariantChangeTypeEx(_Inout_ VARIANT* pVarRes, _In_ VARIANT* pVarSrc,
                          LCID lcid, unsigned short wFlags, VARTYPE vt)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pVarRes));
        PRECONDITION(CheckPointer(pVarSrc));
    }
    CONTRACTL_END;

    GCX_PREEMP();
    _ASSERTE(GetModuleHandleA("oleaut32.dll") != NULL);
    CONTRACT_VIOLATION(ThrowsViolation);

    HRESULT hr = VariantChangeTypeEx (pVarRes, pVarSrc,lcid,wFlags,vt);

    return hr;
}

//--------------------------------------------------------------------------------
void SafeVariantInit(VARIANT* pVar)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pVar));
    }
    CONTRACTL_END;

    // From the oa sources
    V_VT(pVar) = VT_EMPTY;
}

//--------------------------------------------------------------------------------
// void SafeReleaseStream(IStream *pStream)
void SafeReleaseStream(IStream *pStream)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pStream));
    }
    CONTRACTL_END;

    GCX_PREEMP();

    {
        HRESULT hr = CoReleaseMarshalData(pStream);

#ifdef _DEBUG
        WCHAR      logStr[200];
        swprintf_s(logStr, ARRAY_SIZE(logStr), W("Object gone: CoReleaseMarshalData returned %x, file %S, line %d\n"), hr, __FILE__, __LINE__);
        LogInterop(logStr);
        if (hr != S_OK)
        {
            // Reset the stream to the beginning
            LARGE_INTEGER li;
            LISet32(li, 0);
            ULARGE_INTEGER li2;
            pStream->Seek(li, STREAM_SEEK_SET, &li2);
            hr = CoReleaseMarshalData(pStream);
            swprintf_s(logStr, ARRAY_SIZE(logStr), W("Object gone: CoReleaseMarshalData returned %x, file %S, line %d\n"), hr, __FILE__, __LINE__);
            LogInterop(logStr);
        }
#endif
    }

    ULONG cbRef = SafeReleasePreemp(pStream);
    LogInteropRelease(pStream, cbRef, "Release marshal Stream");
}

//---------------------------------------------------------------------------
//  is the iid represent an IClassX for this class
BOOL IsIClassX(MethodTable *pMT, REFIID riid, ComMethodTable **ppComMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(ppComMT));
    }
    CONTRACTL_END;

    // Walk up the hierarchy starting at the specified method table and compare
    // the IID's of the IClassX's against the specified IID.
    while (pMT != NULL)
    {
        ComCallWrapperTemplate *pTemplate = ComCallWrapperTemplate::GetTemplate(pMT);
        if (pTemplate->SupportsIClassX())
        {
            ComMethodTable *pComMT =
                ComCallWrapperTemplate::SetupComMethodTableForClass(pMT, FALSE);
            _ASSERTE(pComMT);

            if (IsEqualIID(riid, pComMT->GetIID()))
            {
                *ppComMT = pComMT;
                return TRUE;
            }
        }

        pMT = pMT->GetComPlusParentMethodTable();
    }

    return FALSE;
}



//---------------------------------------------------------------------------
// Returns TRUE if we support IClassX (the auto-generated class interface)
// for the given class.
BOOL ClassSupportsIClassX(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    return TRUE;
}



#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
//---------------------------------------------------------------------------
// OBJECTREF AllocateComObject_ForManaged(MethodTable* pMT)
OBJECTREF AllocateComObject_ForManaged(MethodTable* pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(pMT->IsComObjectType());
    }
    CONTRACTL_END;

    // Calls to COM up ahead.
    HRESULT hr = S_OK;
    EnsureComStarted();

    ComClassFactory *pComClsFac = (ComClassFactory *)GetComClassFactory(pMT);
    return pComClsFac->CreateInstance(pMT, TRUE);
}
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION


//---------------------------------------------------------------------------
// This method returns the default interface for the class.
DefaultInterfaceType GetDefaultInterfaceForClassInternal(TypeHandle hndClass, TypeHandle *pHndDefClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(!hndClass.IsNull());
        PRECONDITION(CheckPointer(pHndDefClass));
        PRECONDITION(!hndClass.GetMethodTable()->IsInterface());
    }
    CONTRACTL_END;

    // Set ppDefComMT to NULL before we start.
    *pHndDefClass = TypeHandle();

    HRESULT      hr       = S_FALSE;
    MethodTable* pClassMT = hndClass.GetMethodTable();
    const void*         pvData;
    ULONG               cbData;
    CorClassIfaceAttr   ClassItfType;
    BOOL                bComVisible;

    PREFIX_ASSUME(pClassMT != NULL);

    if (pClassMT->IsComImport())
    {
        ClassItfType = clsIfNone;
        bComVisible = TRUE;
    }
    else
    {
        ClassItfType = pClassMT->GetComClassInterfaceType();
        bComVisible = IsTypeVisibleFromCom(hndClass);
    }

    // If the class is not COM visible, then its default interface is IUnknown.
    if (!bComVisible)
        return DefaultInterfaceType_IUnknown;

    // Start by checking for the ComDefaultInterface attribute.
    hr = pClassMT->GetCustomAttribute(WellKnownAttribute::ComDefaultInterface, &pvData, &cbData);
    IfFailThrow(hr);
    if (hr == S_OK && cbData > 2)
    {
        TypeHandle DefItfType;
        AppDomain *pCurrDomain = SystemDomain::GetCurrentDomain();

        CustomAttributeParser cap(pvData, cbData);
        IfFailThrow(cap.SkipProlog());

        LPCUTF8 szStr;
        ULONG   cbStr;
        IfFailThrow(cap.GetNonNullString(&szStr, &cbStr));

        // Allocate a new buffer that will contain the name of the default COM interface.
        StackSString defItf(SString::Utf8, szStr, cbStr);

        // Load the default COM interface specified in the CA.
        {
            GCX_COOP();

            DefItfType = TypeName::GetTypeUsingCASearchRules(defItf.GetUnicode(), pClassMT->GetAssembly());

            // If the type handle isn't a named type, then throw an exception using
            // the name of the type obtained from pCurrInterfaces.
            if (!DefItfType.GetMethodTable())
            {
                // This should only occur for TypeDesc's.
                StackSString ssClassName;
                DefineFullyQualifiedNameForClassW()
                COMPlusThrow(kTypeLoadException, IDS_EE_INVALIDCOMDEFITF,
                             GetFullyQualifiedNameForClassW(pClassMT),
                             defItf.GetUnicode());
            }

            // Otherwise, if the type is not an interface thrown an exception using the actual
            // name of the type.
            if (!DefItfType.IsInterface())
            {
                StackSString ssClassName;
                StackSString ssInvalidItfName;
                pClassMT->_GetFullyQualifiedNameForClass(ssClassName);
                DefItfType.GetMethodTable()->_GetFullyQualifiedNameForClass(ssInvalidItfName);
                COMPlusThrow(kTypeLoadException, IDS_EE_INVALIDCOMDEFITF,
                             ssClassName.GetUnicode(), ssInvalidItfName.GetUnicode());
            }

            // Make sure the class implements the interface.
            if (!pClassMT->CanCastToInterface(DefItfType.GetMethodTable()))
            {
                StackSString ssClassName;
                StackSString ssInvalidItfName;
                pClassMT->_GetFullyQualifiedNameForClass(ssClassName);
                DefItfType.GetMethodTable()->_GetFullyQualifiedNameForClass(ssInvalidItfName);
                COMPlusThrow(kTypeLoadException, IDS_EE_COMDEFITFNOTSUPPORTED,
                             ssClassName.GetUnicode(), ssInvalidItfName.GetUnicode());
            }
        }

        // The default interface is valid so return it.
        *pHndDefClass = DefItfType;
        return DefaultInterfaceType_Explicit;
    }

    // If the class's interface type is AutoDispatch or AutoDual then return either the
    // IClassX for the current class or IDispatch.
    if (ClassItfType != clsIfNone)
    {
        *pHndDefClass = hndClass;
        return ClassItfType == clsIfAutoDisp ? DefaultInterfaceType_AutoDispatch : DefaultInterfaceType_AutoDual;
    }

    // The class interface is set to NONE for this level of the hierarchy. So we need to check
    // to see if this class implements an interface.

    // Search for the first COM visible implemented interface. We start with the most
    // derived class and work our way up the hierarchy.
    for (MethodTable *pParentMT = pClassMT->GetParentMethodTable(); pParentMT; pParentMT = pParentMT->GetParentMethodTable())
    {
        MethodTable::InterfaceMapIterator it = pClassMT->IterateInterfaceMap();
        while (it.Next())
        {
            MethodTable *pItfMT = it.GetInterfaceInfo()->GetApproxMethodTable(pClassMT->GetLoaderModule());

            // Skip generic interfaces. Classic COM interop does not support these and we don't
            // use the result of this function in WinRT scenarios. WinRT parameter marshaling
            // doesn't come here at all because the default interface is always specified using
            // the DefaultAttribute. Field marshaling does come here but WinRT does not support
            // fields of reference types other than string.
            if (!pItfMT->HasInstantiation())
            {
                // If the interface is visible from COM and not implemented by our parent,
                // then use it as the default.
                if (IsTypeVisibleFromCom(TypeHandle(pItfMT)) && !pParentMT->ImplementsInterface(pItfMT))
                {
                    *pHndDefClass = TypeHandle(pItfMT);
                    return DefaultInterfaceType_Explicit;
                }
            }
        }
    }

    // If the class is a COM import with no interfaces, then its default interface will
    // be IUnknown.
    if (pClassMT->IsComImport())
        return DefaultInterfaceType_IUnknown;

    // If we have a managed parent class then return its default interface.
    MethodTable *pParentClass = pClassMT->GetComPlusParentMethodTable();
    if (pParentClass)
        return GetDefaultInterfaceForClassWrapper(TypeHandle(pParentClass), pHndDefClass);

    // Check to see if the class is an extensible RCW.
    if (pClassMT->IsComObjectType())
        return DefaultInterfaceType_BaseComClass;

    // The class has no interfaces and is marked as ClassInterfaceType.None.
    return DefaultInterfaceType_IUnknown;
}


DefaultInterfaceType GetDefaultInterfaceForClassWrapper(TypeHandle hndClass, TypeHandle *pHndDefClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!hndClass.IsNull());
    }
    CONTRACTL_END;

    if (!hndClass.IsTypeDesc())
    {
        ComCallWrapperTemplate *pTemplate = hndClass.AsMethodTable()->GetComCallWrapperTemplate();
        if (pTemplate != NULL)
        {
            // if CCW template is available, use its cache
            MethodTable *pDefaultItf;
            DefaultInterfaceType itfType = pTemplate->GetDefaultInterface(&pDefaultItf);

            *pHndDefClass = TypeHandle(pDefaultItf);
            return itfType;
        }
    }

    return GetDefaultInterfaceForClassInternal(hndClass, pHndDefClass);
}



HRESULT TryGetDefaultInterfaceForClass(TypeHandle hndClass, TypeHandle *pHndDefClass, DefaultInterfaceType *pDefItfType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!hndClass.IsNull());
        PRECONDITION(CheckPointer(pHndDefClass));
        PRECONDITION(CheckPointer(pDefItfType));
    }
    CONTRACTL_END;

    GCX_COOP();

    HRESULT hr = S_OK;
    OBJECTREF pThrowable = NULL;

    GCPROTECT_BEGIN(pThrowable)
    {
        EX_TRY
        {
            *pDefItfType = GetDefaultInterfaceForClassWrapper(hndClass, pHndDefClass);
        }
        EX_CATCH
        {
            pThrowable = GET_THROWABLE();
        }
        EX_END_CATCH(SwallowAllExceptions);

        if (pThrowable != NULL)
            hr = SetupErrorInfo(pThrowable);
    }
    GCPROTECT_END();
    return hr;
}

// Returns the default interface for a class if it's an explicit interface or the AutoDual
// class interface. Sets *pbDispatch otherwise. This is the logic used by array marshaling
// in code:OleVariant::MarshalInterfaceArrayComToOleHelper.
MethodTable *GetDefaultInterfaceMTForClass(MethodTable *pMT, BOOL *pbDispatch)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(!pMT->IsInterface());
        PRECONDITION(CheckPointer(pbDispatch));
    }
    CONTRACTL_END;

    TypeHandle hndDefItfClass;
    DefaultInterfaceType DefItfType = GetDefaultInterfaceForClassWrapper(TypeHandle(pMT), &hndDefItfClass);

    switch (DefItfType)
    {
        case DefaultInterfaceType_Explicit:
        case DefaultInterfaceType_AutoDual:
        {
            return hndDefItfClass.GetMethodTable();
        }

        case DefaultInterfaceType_IUnknown:
        case DefaultInterfaceType_BaseComClass:
        {
            *pbDispatch = FALSE;
            return NULL;
        }

        case DefaultInterfaceType_AutoDispatch:
        {
            *pbDispatch = TRUE;
            return NULL;
        }

        default:
        {
            _ASSERTE(!"Invalid default interface type!");
            return NULL;
        }
    }
}

//---------------------------------------------------------------------------
// This method retrieves the list of source interfaces for a given class.
void GetComSourceInterfacesForClass(MethodTable *pMT, CQuickArray<MethodTable *> &rItfList)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    HRESULT             hr          = S_OK;
    const void*         pvData;
    ULONG               cbData;
    CQuickArray<CHAR>   qbCurrInterfaces;

    GCX_COOP();

    // Reset the size of the interface list to 0.
    rItfList.Shrink(0);

    // Starting at the specified class MT retrieve the COM source interfaces
    // of all the striped of the hierarchy.
    for (; pMT != NULL; pMT = pMT->GetParentMethodTable())
    {
        // See if there is any [source] interface at this level of the hierarchy.
        hr = pMT->GetCustomAttribute(WellKnownAttribute::ComSourceInterfaces, &pvData, &cbData);
        IfFailThrow(hr);
        if (hr == S_OK && cbData > 2)
        {
            AppDomain *pCurrDomain = SystemDomain::GetCurrentDomain();

            CustomAttributeParser cap(pvData, cbData);
            IfFailThrow(cap.SkipProlog());

            while (cap.BytesLeft() != 0)
            {
                // Uncompress the current string of source interfaces.
                BYTE const *pbStr;
                ULONG       cbStr;
                IfFailThrow(cap.GetData(&pbStr, &cbStr));

                // Allocate a new buffer that will contain the current list of source interfaces.
                qbCurrInterfaces.ReSizeThrows(cbStr + 1);
                LPUTF8 strCurrInterfaces = qbCurrInterfaces.Ptr();
                memcpyNoGCRefs(strCurrInterfaces, pbStr, cbStr);
                strCurrInterfaces[cbStr] = 0;
                LPUTF8 pCurrInterfaces = strCurrInterfaces;
                LPUTF8 pCurrInterfacesEnd = pCurrInterfaces + cbStr + 1;

                while (pCurrInterfaces < pCurrInterfacesEnd && *pCurrInterfaces != 0)
                {
                    // Load the COM source interface specified in the CA.
                    TypeHandle ItfType;
                    ItfType = TypeName::GetTypeUsingCASearchRules(pCurrInterfaces, pMT->GetAssembly());

                    // If the type handle isn't a named type, then throw an exception using
                    // the name of the type obtained from pCurrInterfaces.
                    if (!ItfType.GetMethodTable())
                    {
                        // This should only occur for TypeDesc's.
                        StackSString ssInvalidItfName(SString::Utf8, pCurrInterfaces);
                        DefineFullyQualifiedNameForClassW()
                        COMPlusThrow(kTypeLoadException, IDS_EE_INVALIDCOMSOURCEITF,
                                     GetFullyQualifiedNameForClassW(pMT),
                                     ssInvalidItfName.GetUnicode());
                    }

                    // Otherwise, if the type is not an interface thrown an exception using the actual
                    // name of the type.
                    if (!ItfType.IsInterface())
                    {
                        StackSString ssClassName;
                        StackSString ssInvalidItfName;
                        pMT->_GetFullyQualifiedNameForClass(ssClassName);
                        ItfType.GetMethodTable()->_GetFullyQualifiedNameForClass(ssInvalidItfName);
                        COMPlusThrow(kTypeLoadException, IDS_EE_INVALIDCOMSOURCEITF,
                                     ssClassName.GetUnicode(), ssInvalidItfName.GetUnicode());
                    }

                    // Ensure the source interface is not generic.
                    if (ItfType.HasInstantiation())
                    {
                        StackSString ssClassName;
                        StackSString ssInvalidItfName;
                        pMT->_GetFullyQualifiedNameForClass(ssClassName);
                        ItfType.GetMethodTable()->_GetFullyQualifiedNameForClass(ssInvalidItfName);
                        COMPlusThrow(kTypeLoadException, IDS_EE_INVALIDCOMSOURCEITF,
                                     ssClassName.GetUnicode(), ssInvalidItfName.GetUnicode());
                    }


                    // Retrieve the IID of the COM source interface.
                    IID ItfIID;
                    ItfType.GetMethodTable()->GetGuid(&ItfIID, TRUE);

                    // Go through the list of source interfaces and check to see if the new one is a duplicate.
                    // It can be a duplicate either if it is the same interface or if it has the same IID.
                    BOOL bItfInList = FALSE;
                    for (UINT i = 0; i < rItfList.Size(); i++)
                    {
                        if (rItfList[i] == ItfType.GetMethodTable())
                        {
                            bItfInList = TRUE;
                            break;
                        }

                        IID ItfIID2;
                        rItfList[i]->GetGuid(&ItfIID2, TRUE);
                        if (IsEqualIID(ItfIID, ItfIID2))
                        {
                            bItfInList = TRUE;
                            break;
                        }
                    }

                    // If the COM source interface is not in the list then add it.
                    if (!bItfInList)
                    {
                        size_t OldSize = rItfList.Size();
                        rItfList.ReSizeThrows(OldSize + 1);
                        rItfList[OldSize] = ItfType.GetMethodTable();
                    }

                    // Process the next COM source interfaces in the CA.
                    pCurrInterfaces += strlen(pCurrInterfaces) + 1;
                }
            }
        }
    }
}


//--------------------------------------------------------------------------------
// These methods convert a native IEnumVARIANT to a managed IEnumerator.
OBJECTREF ConvertEnumVariantToMngEnum(IEnumVARIANT *pNativeEnum)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF MngEnum = NULL;
    OBJECTREF EnumeratorToEnumVariantMarshaler = NULL;
    GCPROTECT_BEGIN(EnumeratorToEnumVariantMarshaler)
    {
        // Retrieve the custom marshaler and the MD to use to convert the IEnumVARIANT.
        StdMngIEnumerator *pStdMngIEnumInfo = SystemDomain::GetCurrentDomain()->GetMngStdInterfacesInfo()->GetStdMngIEnumerator();
        MethodDesc *pEnumNativeToManagedMD = pStdMngIEnumInfo->GetCustomMarshalerMD(CustomMarshalerMethods_MarshalNativeToManaged);
        EnumeratorToEnumVariantMarshaler = pStdMngIEnumInfo->GetCustomMarshaler();
        MethodDescCallSite enumNativeToManaged(pEnumNativeToManagedMD, &EnumeratorToEnumVariantMarshaler);

        // Prepare the arguments that will be passed to MarshalNativeToManaged.
        ARG_SLOT MarshalNativeToManagedArgs[] = {
            ObjToArgSlot(EnumeratorToEnumVariantMarshaler),
            (ARG_SLOT)pNativeEnum
        };

        // Retrieve the managed view for the current native interface pointer.
        MngEnum = enumNativeToManaged.Call_RetOBJECTREF(MarshalNativeToManagedArgs);
    }
    GCPROTECT_END();

    return MngEnum;
}

//--------------------------------------------------------------------------------
// This method converts an OLE_COLOR to a System.Color.
void ConvertOleColorToSystemColor(OLE_COLOR SrcOleColor, SYSTEMCOLOR *pDestSysColor)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Retrieve the method desc to use for the current AD.
    MethodDesc *pOleColorToSystemColorMD =
        GetAppDomain()->GetLoaderAllocator()->GetMarshalingData()->GetOleColorMarshalingInfo()->GetOleColorToSystemColorMD();

    MethodDescCallSite oleColorToSystemColor(pOleColorToSystemColorMD);

    _ASSERTE(pOleColorToSystemColorMD->HasRetBuffArg());

    ARG_SLOT Args[] =
    {
        PtrToArgSlot(pDestSysColor),
        PtrToArgSlot(SrcOleColor)
    };

    oleColorToSystemColor.Call(Args);
}

//--------------------------------------------------------------------------------
// This method converts a System.Color to an OLE_COLOR.
OLE_COLOR ConvertSystemColorToOleColor(OBJECTREF *pSrcObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Retrieve the method desc to use for the current AD.
    MethodDesc *pSystemColorToOleColorMD =
        GetAppDomain()->GetLoaderAllocator()->GetMarshalingData()->GetOleColorMarshalingInfo()->GetSystemColorToOleColorMD();
    MethodDescCallSite systemColorToOleColor(pSystemColorToOleColorMD);

    // Set up the args and call the method.
    SYSTEMCOLOR *pSrcSysColor = (SYSTEMCOLOR *)(*pSrcObj)->UnBox();
    return systemColorToOleColor.CallWithValueTypes_RetOleColor((const ARG_SLOT *)&pSrcSysColor);
}

//--------------------------------------------------------------------------------
// This method generates a stringized version of a class interface that contains
// the signatures of all the methods and fields.
ULONG GetStringizedClassItfDef(TypeHandle InterfaceType, CQuickArray<BYTE> &rDef)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(!InterfaceType.IsNull());
    }
    CONTRACTL_END;

    LPCWSTR             szName;
    ULONG               cchName;
    MethodTable*        pIntfMT = InterfaceType.GetMethodTable();
    PREFIX_ASSUME(pIntfMT != NULL);

    MethodTable*        pDeclaringMT = NULL;
    DWORD               nSlots;                 // Slots on the pseudo interface.
    mdToken             tkMb;                   // A method or field token.
    ULONG               cbCur;
    HRESULT             hr = S_OK;
    ULONG               i;

    // Should be an actual class.
    _ASSERTE(!pIntfMT->IsInterface());

    // See what sort of IClassX this class gets.
    TypeHandle thDefItf;
    BOOL bGenerateMethods = FALSE;
    DefaultInterfaceType DefItfType = GetDefaultInterfaceForClassWrapper(TypeHandle(pIntfMT), &thDefItf);

    // The results apply to this class if the thDefItf is this class itself, not a parent class.
    // A side effect is that [ComVisible(false)] types' guids are generated without members.
    if (thDefItf.GetMethodTable() == pIntfMT && DefItfType == DefaultInterfaceType_AutoDual)
        bGenerateMethods = TRUE;

    // Get the name of the class.
    DefineFullyQualifiedNameForClassW();
    szName = GetFullyQualifiedNameForClassNestedAwareW(pIntfMT);
    cchName = (ULONG)wcslen(szName);

    // Start with the interface name.
    cbCur = cchName * sizeof(WCHAR);
    rDef.ReSizeThrows(cbCur + sizeof(WCHAR));
    wcscpy_s(reinterpret_cast<LPWSTR>(rDef.Ptr()), rDef.Size()/sizeof(WCHAR), szName);

    if (bGenerateMethods)
    {
        ComMTMemberInfoMap MemberMap(pIntfMT); // The map of members.

        // Retrieve the method properties.
        MemberMap.Init(sizeof(void*));

        CQuickArray<ComMTMethodProps> &rProps = MemberMap.GetMethods();
        nSlots = (DWORD)rProps.Size();

        // Now add the methods to the TypeInfo.
        for (i=0; i<nSlots; ++i)
        {
            ComMTMethodProps *pProps = &rProps[i];
            if (pProps->bMemberVisible)
            {
                if (pProps->semantic < FieldSemanticOffset)
                {
                    pDeclaringMT = pProps->pMeth->GetMethodTable();
                    tkMb = pProps->pMeth->GetMemberDef();
                    cbCur = GetStringizedMethodDef(pDeclaringMT, tkMb, rDef, cbCur);
                }
                else
                {
                    ComCallMethodDesc   *pFieldMeth;    // A MethodDesc for a field call.
                    FieldDesc   *pField;                // A FieldDesc.
                    pFieldMeth = reinterpret_cast<ComCallMethodDesc*>(pProps->pMeth);
                    pField = pFieldMeth->GetFieldDesc();
                    pDeclaringMT = pField->GetApproxEnclosingMethodTable();
                    tkMb = pField->GetMemberDef();
                    cbCur = GetStringizedFieldDef(pDeclaringMT, tkMb, rDef, cbCur);
                }
            }
        }
    }

    // Return the number of bytes.
    return cbCur;
} // ULONG GetStringizedClassItfDef()

//--------------------------------------------------------------------------------
// Helper to get the GUID of a class interface.
void GenerateClassItfGuid(TypeHandle InterfaceType, GUID *pGuid)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(!InterfaceType.IsNull());
        PRECONDITION(CheckPointer(pGuid));
    }
    CONTRACTL_END;

    LPWSTR      szName;                 // Name to turn to a guid.
    ULONG       cchName;                // Length of the name (possibly after decoration).
    CQuickArray<BYTE> rName;            // Buffer to accumulate signatures.
    ULONG       cbCur;                  // Current offset.
    HRESULT     hr = S_OK;              // A result.

    cbCur = GetStringizedClassItfDef(InterfaceType, rName);

    // Pad up to a whole WCHAR.
    if (cbCur % sizeof(WCHAR))
    {
        int cbDelta = sizeof(WCHAR) - (cbCur % sizeof(WCHAR));
        rName.ReSizeThrows(cbCur + cbDelta);
        memset(rName.Ptr() + cbCur, 0, cbDelta);
        cbCur += cbDelta;
    }

    // Point to the new buffer.
    cchName = cbCur / sizeof(WCHAR);
    szName = reinterpret_cast<LPWSTR>(rName.Ptr());

    // Generate guid from name.
    CorGuidFromNameW(pGuid, szName, cchName);
} // void GenerateClassItfGuid()

HRESULT TryGenerateClassItfGuid(TypeHandle InterfaceType, GUID *pGuid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!InterfaceType.IsNull());
        PRECONDITION(CheckPointer(pGuid));
    }
    CONTRACTL_END;

    GCX_COOP();

    HRESULT hr = S_OK;
    OBJECTREF pThrowable = NULL;

    GCPROTECT_BEGIN(pThrowable)
    {
        EX_TRY
        {
            GenerateClassItfGuid(InterfaceType, pGuid);
        }
        EX_CATCH
        {
            pThrowable = GET_THROWABLE();
        }
        EX_END_CATCH (SwallowAllExceptions);

        if (pThrowable != NULL)
            hr = SetupErrorInfo(pThrowable);
    }
    GCPROTECT_END();

    return hr;
}

//--------------------------------------------------------------------------------
// Helper to get the GUID of the typelib that is created from an assembly.
HRESULT GetTypeLibGuidForAssembly(Assembly *pAssembly, GUID *pGuid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(pGuid));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;
    CQuickArray<BYTE> rName;            // String for guid.
    ULONG       cbData;                 // Size of the string in bytes.

    // Get GUID from Assembly, else from Manifest Module, else Generate from name.
    hr = pAssembly->GetManifestImport()->GetItemGuid(TokenFromRid(1, mdtAssembly), pGuid);

    if (*pGuid == GUID_NULL)
    {
        // Get the string.
        IfFailGo(GetStringizedTypeLibGuidForAssembly(pAssembly, rName, 0, &cbData));

        // Pad to a whole WCHAR.
        if (cbData % sizeof(WCHAR))
        {
            IfFailGo(rName.ReSizeNoThrow(cbData + sizeof(WCHAR)-(cbData%sizeof(WCHAR))));
            while (cbData % sizeof(WCHAR))
                rName[cbData++] = 0;
        }

        // Turn into guid
        CorGuidFromNameW(pGuid, (LPWSTR)rName.Ptr(), cbData/sizeof(WCHAR));
}

ErrExit:
    return hr;
} // HRESULT GetTypeLibGuidForAssembly()

//--------------------------------------------------------------------------------
// Helper to get the version of the typelib that is created from an assembly.
HRESULT GetTypeLibVersionForAssembly(
    _In_ Assembly *pAssembly,
    _Out_ USHORT *pMajorVersion,
    _Out_ USHORT *pMinorVersion)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(pMajorVersion));
        PRECONDITION(CheckPointer(pMinorVersion));
    }
    CONTRACTL_END;

    HRESULT hr;
    const BYTE *pbData = nullptr;
    ULONG cbData = 0;

    // Check to see if the TypeLibVersionAttribute is set.
    IfFailRet(pAssembly->GetManifestImport()->GetCustomAttributeByName(TokenFromRid(1, mdtAssembly), INTEROP_TYPELIBVERSION_TYPE, (const void**)&pbData, &cbData));

    // For attribute contents, see https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.typelibversionattribute
    if (cbData >= (2 + 2 * sizeof(UINT32)))
    {
        CustomAttributeParser cap(pbData, cbData);
        IfFailRet(cap.SkipProlog());

        // Retrieve the major and minor version from the attribute.
        UINT32 u4;
        IfFailRet(cap.GetU4(&u4));
        *pMajorVersion = GET_VERSION_USHORT_FROM_INT(u4);
        IfFailRet(cap.GetU4(&u4));
        *pMinorVersion = GET_VERSION_USHORT_FROM_INT(u4);
    }
    else
    {
        // Use the assembly's major and minor version number.
        IfFailRet(pAssembly->GetVersion(pMajorVersion, pMinorVersion, nullptr, nullptr));
    }

    // Some system don't handle a typelib with a version of 0.0.
    // When that happens, change it to 1.0.
    if (*pMajorVersion == 0 && *pMinorVersion == 0)
        *pMajorVersion = 1;

    return S_OK;
} // HRESULT TypeLibExporter::GetTypeLibVersionFromAssembly()



//---------------------------------------------------------------------------
// This method determines if a member is visible from COM.
BOOL IsMethodVisibleFromCom(MethodDesc *pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;
    mdProperty  pd;
    LPCUTF8     pPropName;
    ULONG       uSemantic;
    mdMethodDef md = pMD->GetMemberDef();

    // See if there is property information for this member.
    hr = pMD->GetModule()->GetPropertyInfoForMethodDef(md, &pd, &pPropName, &uSemantic);
    IfFailThrow(hr);

    if (hr == S_OK)
    {
        return IsMemberVisibleFromCom(pMD->GetMethodTable(), pd, md);
    }
    else
    {
        return IsMemberVisibleFromCom(pMD->GetMethodTable(), md, mdTokenNil);
    }
}

//---------------------------------------------------------------------------
// This method determines if a type is visible from COM or not based on
// its visibility. This version of the method works with a type handle.
// This version will ignore a type's generic attributes.
//
// This API should *never* be called directly!!!
static BOOL SpecialIsGenericTypeVisibleFromCom(TypeHandle hndType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(!hndType.IsNull());
    }
    CONTRACTL_END;

    DWORD                   dwFlags;
    mdTypeDef               tdEnclosingType;
    HRESULT                 hr;
    const BYTE *            pVal;
    ULONG                   cbVal;
    MethodTable *           pMT = hndType.GetMethodTable();
    _ASSERTE(pMT);

    mdTypeDef               mdType = pMT->GetCl();
    IMDInternalImport *     pInternalImport = pMT->GetMDImport();
    Assembly *              pAssembly = pMT->GetAssembly();
    Module *                pModule = pMT->GetModule();

    // If the type is a COM imported interface then it is visible from COM.
    if (pMT->IsInterface() && pMT->IsComImport())
        return TRUE;

    // If the type is an array, then it is not visible from COM.
    if (pMT->IsArray())
        return FALSE;

    // Retrieve the flags for the current type.
    tdEnclosingType = mdType;
    if (FAILED(pInternalImport->GetTypeDefProps(tdEnclosingType, &dwFlags, 0)))
    {
        return FALSE;
    }

    // Handle nested types.
    while (IsTdNestedPublic(dwFlags))
    {
        hr = pInternalImport->GetNestedClassProps(tdEnclosingType, &tdEnclosingType);
        if (FAILED(hr))
        {
            return FALSE;
        }

        // Retrieve the flags for the enclosing type.
        if (FAILED(pInternalImport->GetTypeDefProps(tdEnclosingType, &dwFlags, 0)))
        {
            return FALSE;
        }
    }

    // If the outermost type is not visible then the specified type is not visible.
    if (!IsTdPublic(dwFlags))
        return FALSE;

    // Check to see if the type has the ComVisible attribute set.
    hr = pModule->GetCustomAttribute(mdType, WellKnownAttribute::ComVisible, (const void**)&pVal, &cbVal);
    if (hr == S_OK)
    {
        CustomAttributeParser cap(pVal, cbVal);
        if (FAILED(cap.SkipProlog()))
            return FALSE;

        UINT8 u1;
        if (FAILED(cap.GetU1(&u1)))
            return FALSE;

        return (BOOL)u1;
    }

    // Check to see if the assembly has the ComVisible attribute set.
    hr = pModule->GetCustomAttribute(pAssembly->GetManifestToken(), WellKnownAttribute::ComVisible, (const void**)&pVal, &cbVal);
    if (hr == S_OK)
    {
        CustomAttributeParser cap(pVal, cbVal);
        if (FAILED(cap.SkipProlog()))
            return FALSE;

        UINT8 u1;
        if (FAILED(cap.GetU1(&u1)))
            return FALSE;

        return (BOOL)u1;
    }

    // The type is visible.
    return TRUE;
}

//---------------------------------------------------------------------------
// This method determines if a type is visible from COM or not based on
// its visibility. This version of the method works with a type handle.
BOOL IsTypeVisibleFromCom(TypeHandle hndType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(!hndType.IsNull());
    }
    CONTRACTL_END;

    // If the type is a generic type, then it is not visible from COM.
    if (hndType.HasInstantiation() || hndType.IsGenericVariable())
        return FALSE;

    return SpecialIsGenericTypeVisibleFromCom(hndType);
}



//--------------------------------------------------------------------------------
// Validate that the given target is valid for the specified type.
BOOL IsComTargetValidForType(REFLECTCLASSBASEREF* pRefClassObj, OBJECTREF* pTarget)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pRefClassObj));
        PRECONDITION(CheckPointer(pTarget));
    }
    CONTRACTL_END;

    MethodTable* pInvokedMT = (*pRefClassObj)->GetType().GetMethodTable();

    MethodTable* pTargetMT = (*pTarget)->GetMethodTable();
    _ASSERTE(pTargetMT);
    PREFIX_ASSUME(pInvokedMT != NULL);

    // If the target class and the invoke class are identical then the invoke is valid.
    if (pTargetMT == pInvokedMT)
        return TRUE;

    // We always allow calling InvokeMember on a __ComObject type regardless of the type
    // of the target object.
    if (IsComObjectClass((*pRefClassObj)->GetType()))
        return TRUE;

    // If the class that is being invoked on is an interface then check to see if the
    // target class supports that interface.
    if (pInvokedMT->IsInterface())
        return Object::SupportsInterface(*pTarget, pInvokedMT);

    // Check to see if the target class inherits from the invoked class.
    while (pTargetMT)
    {
        pTargetMT = pTargetMT->GetParentMethodTable();
        if (pTargetMT == pInvokedMT)
        {
            // The target class inherits from the invoked class.
            return TRUE;
        }
    }

    // There is no valid relationship between the invoked and the target classes.
    return FALSE;
}

DISPID ExtractStandardDispId(_In_z_ LPWSTR strStdDispIdMemberName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Find the first character after the = in the standard DISPID member name.
    LPWSTR strDispId = wcsstr(&strStdDispIdMemberName[STANDARD_DISPID_PREFIX_LENGTH], W("=")) + 1;
    if (!strDispId)
        COMPlusThrow(kArgumentException, IDS_EE_INVALID_STD_DISPID_NAME);

    // Validate that the last character of the standard member name is a ].
    LPWSTR strClosingBracket = wcsstr(strDispId, W("]"));
    if (!strClosingBracket || (strClosingBracket[1] != 0))
        COMPlusThrow(kArgumentException, IDS_EE_INVALID_STD_DISPID_NAME);

    // Extract the number from the standard DISPID member name.
    return _wtoi(strDispId);
}

static HRESULT InvokeExHelper(
    IDispatchEx *       pDispEx,
    DISPID              MemberID,
    LCID                lcid,
    WORD                flags,
    DISPPARAMS *        pDispParams,
    VARIANT*            pVarResult,
    EXCEPINFO *         pExcepInfo,
                              IServiceProvider *pspCaller)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    _ASSERTE(pDispEx != NULL);

    struct Param : CallOutFilterParam {
        HRESULT             hr;
        IDispatchEx *       pDispEx;
        DISPID              MemberID;
        LCID                lcid;
        WORD                flags;
        DISPPARAMS *        pDispParams;
        VARIANT*            pVarResult;
        EXCEPINFO *         pExcepInfo;
        IServiceProvider *  pspCaller;
    }; Param param;

    param.OneShot = TRUE; // Inherited from CallOutFilterParam
    param.hr = S_OK;
    param.pDispEx = pDispEx;
    param.MemberID = MemberID;
    param.lcid = lcid;
    param.flags = flags;
    param.pDispParams = pDispParams;
    param.pVarResult = pVarResult;
    param.pExcepInfo = pExcepInfo;
    param.pspCaller = pspCaller;

    PAL_TRY(Param *, pParam, &param)
    {
        pParam->hr = pParam->pDispEx->InvokeEx(pParam->MemberID,
                                               pParam->lcid,
                                               pParam->flags,
                                               pParam->pDispParams,
                                               pParam->pVarResult,
                                               pParam->pExcepInfo,
                                               pParam->pspCaller);
    }
    PAL_EXCEPT_FILTER(CallOutFilter)
    {
        _ASSERTE(!"CallOutFilter returned EXECUTE_HANDLER.");
    }
    PAL_ENDTRY;

    return param.hr;
}

static HRESULT InvokeHelper(
    IDispatch *     pDisp,
    DISPID          MemberID,
    REFIID          riid,
    LCID            lcid,
    WORD            flags,
    DISPPARAMS *    pDispParams,
    VARIANT*        pVarResult,
    EXCEPINFO *     pExcepInfo,
                            UINT *piArgErr)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    _ASSERTE(pDisp != NULL);

    struct Param : CallOutFilterParam {
        HRESULT             hr;
        IDispatch *         pDisp;
        DISPID              MemberID;
        REFIID              riid;
        LCID                lcid;
        WORD                flags;
        DISPPARAMS *        pDispParams;
        VARIANT *           pVarResult;
        EXCEPINFO *         pExcepInfo;
        UINT *              piArgErr;

        Param(REFIID _riid) : riid(_riid) {}
    }; Param param(riid);

    param.OneShot = TRUE; // Inherited from CallOutFilterParam
    param.hr = S_OK;
    param.pDisp = pDisp;
    param.MemberID = MemberID;
    //param.riid = riid;
    param.lcid = lcid;
    param.flags = flags;
    param.pDispParams = pDispParams;
    param.pVarResult = pVarResult;
    param.pExcepInfo = pExcepInfo;
    param.piArgErr = piArgErr;

    PAL_TRY(Param *, pParam, &param)
    {
        pParam->hr = pParam->pDisp->Invoke(pParam->MemberID,
                                           pParam->riid,
                                           pParam->lcid,
                                           pParam->flags,
                                           pParam->pDispParams,
                                           pParam->pVarResult,
                                           pParam->pExcepInfo,
                                           pParam->piArgErr);
    }
    PAL_EXCEPT_FILTER(CallOutFilter)
    {
        _ASSERTE(!"CallOutFilter returned EXECUTE_HANDLER.");
    }
    PAL_ENDTRY;

    return param.hr;
}


void DispInvokeConvertObjectToVariant(OBJECTREF *pSrcObj, VARIANT *pDestVar, ByrefArgumentInfo *pByrefArgInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pSrcObj));
        PRECONDITION(IsProtectedByGCFrame (pSrcObj));
        PRECONDITION(CheckPointer(pDestVar));
        PRECONDITION(CheckPointer(pByrefArgInfo));
    }
    CONTRACTL_END;

    if (pByrefArgInfo->m_bByref)
    {
        if (*pSrcObj == NULL)
        {
            V_VT(pDestVar) = VT_VARIANT | VT_BYREF;
            pDestVar->pvarVal = &pByrefArgInfo->m_Val;
        }
        else if (CoreLibBinder::IsClass((*pSrcObj)->GetMethodTable(), CLASS__VARIANT_WRAPPER))
        {
            OBJECTREF WrappedObj = (*((VARIANTWRAPPEROBJECTREF*)pSrcObj))->GetWrappedObject();
            GCPROTECT_BEGIN(WrappedObj)
            {
                OleVariant::MarshalOleVariantForObject(&WrappedObj, &pByrefArgInfo->m_Val);
                V_VT(pDestVar) = VT_VARIANT | VT_BYREF;
                pDestVar->pvarVal = &pByrefArgInfo->m_Val;
            }
            GCPROTECT_END();
        }
        else
        {
            OleVariant::MarshalOleVariantForObject(pSrcObj, &pByrefArgInfo->m_Val);
            OleVariant::CreateByrefVariantForVariant(&pByrefArgInfo->m_Val, pDestVar);
        }
    }
    else
    {
        OleVariant::MarshalOleVariantForObject(pSrcObj, pDestVar);
    }
}

static void DoIUInvokeDispMethod(IDispatchEx* pDispEx, IDispatch* pDisp, DISPID MemberID, LCID lcid,
                                 WORD flags, DISPPARAMS* pDispParams, VARIANT* pVarResult)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    UINT        iArgErr;
    EXCEPINFO   ExcepInfo;
    HRESULT     hr;

    memset(&ExcepInfo, 0, sizeof(EXCEPINFO));

    GCX_COOP();
    OBJECTREF pThrowable = NULL;
    GCPROTECT_BEGIN(pThrowable);
    {
        // Call the method
        EX_TRY
        {
            {
            // We are about to make call's to COM so switch to preemptive GC.
            GCX_PREEMP();

                if (pDispEx)
                {
                    hr = InvokeExHelper(pDispEx, MemberID, lcid, flags, pDispParams,
                                        pVarResult, &ExcepInfo, NULL);
                }
                else
                {
                    hr = InvokeHelper(  pDisp, MemberID, IID_NULL, lcid, flags,
                                        pDispParams, pVarResult, &ExcepInfo, &iArgErr);
                }
            }

            // If the invoke call failed then throw an exception based on the EXCEPINFO.
            if (FAILED(hr))
            {
                if (hr == DISP_E_EXCEPTION)
                {
                    // This method will free the BSTR's in the EXCEPINFO.
                    COMPlusThrowHR(&ExcepInfo);
                }
                else
                {
                    COMPlusThrowHR(hr);
                }
            }
        }
        EX_CATCH
        {
            // If we get here we need to throw an TargetInvocationException
            pThrowable = GET_THROWABLE();
            _ASSERTE(pThrowable != NULL);
        }
        EX_END_CATCH(RethrowTerminalExceptions);

        if (pThrowable != NULL)
        {
            COMPlusThrow(InvokeUtil::CreateTargetExcept(&pThrowable));
        }
    }
    GCPROTECT_END();
}


FORCEINLINE void DispParamHolderRelease(VARIANT* value)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (value)
    {
       if (V_VT(value) & VT_BYREF)
       {
           VariantHolder TmpVar;
           OleVariant::ExtractContentsFromByrefVariant(value, &TmpVar);
       }

       SafeVariantClear(value);
    }
}

class DispParamHolder : public Wrapper<VARIANT*, DispParamHolderDoNothing, DispParamHolderRelease, NULL>
{
public:
    DispParamHolder(VARIANT* p = NULL)
        : Wrapper<VARIANT*, DispParamHolderDoNothing, DispParamHolderRelease, NULL>(p)
    {
        WRAPPER_NO_CONTRACT;
    }

    FORCEINLINE void operator=(VARIANT* p)
    {
        WRAPPER_NO_CONTRACT;
        Wrapper<VARIANT*, DispParamHolderDoNothing, DispParamHolderRelease, NULL>::operator=(p);
    }
};



//--------------------------------------------------------------------------------
// InvokeDispMethod will convert a set of managed objects and call IDispatch.  The
// result will be returned as a CLR Variant pointed to by pRetVal.
void IUInvokeDispMethod(
    REFLECTCLASSBASEREF* pRefClassObj,
    OBJECTREF* pTarget,
    OBJECTREF* pName,
    DISPID *pMemberID,
    OBJECTREF* pArgs,
    OBJECTREF* pByrefModifiers,
    OBJECTREF* pNamedArgs,
    OBJECTREF* pRetVal,
    LCID lcid,
    WORD flags,
    BOOL bIgnoreReturn,
    BOOL bIgnoreCase)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pTarget));
    }
    CONTRACTL_END;

    HRESULT             hr;
    UINT                i;
    UINT                iSrcArg;
    UINT                iDestArg;
    VARIANT             VarResult;
    UINT                cArgs               = 0;
    UINT                cNamedArgs          = 0;
    DISPPARAMS          DispParams          = {0};
    DISPID*             aDispID             = NULL;
    DISPID              MemberID            = 0;
    ByrefArgumentInfo*  aByrefArgInfos      = NULL;
    BOOL                bSomeArgsAreByref   = FALSE;
    SafeComHolder<IDispatch> pDisp          = NULL;
    SafeComHolder<IDispatchEx> pDispEx      = NULL;
    VariantPtrHolder    pVarResult          = NULL;
    NewArrayHolder<DispParamHolder> params  = NULL;

    //
    // Function initialization.
    //

    SafeVariantInit(&VarResult);


    // InteropUtil.h does not know about anything other than OBJECTREF so
    // convert the OBJECTREF's to their real type.

    STRINGREF* pStrName = (STRINGREF*) pName;
    PTRARRAYREF* pArrArgs = (PTRARRAYREF*) pArgs;
    PTRARRAYREF* pArrByrefModifiers = (PTRARRAYREF*) pByrefModifiers;
    PTRARRAYREF* pArrNamedArgs = (PTRARRAYREF*) pNamedArgs;
    MethodTable* pInvokedMT = (*pRefClassObj)->GetType().GetMethodTable();
    PREFIX_ASSUME(pInvokedMT != NULL);

    // Retrieve the total count of arguments.
    if (*pArrArgs != NULL)
        cArgs = (*pArrArgs)->GetNumComponents();

    // Retrieve the count of named arguments.
    if (*pArrNamedArgs != NULL)
        cNamedArgs = (*pArrNamedArgs)->GetNumComponents();

    // Validate that the target is valid for the specified type.
    if (!IsComTargetValidForType(pRefClassObj, pTarget))
        COMPlusThrow(kTargetException, W("RFLCT.Targ_ITargMismatch"));

    // If the invoked type is an interface, make sure it is IDispatch based.
    if (pInvokedMT->IsInterface())
    {
        CorIfaceAttr ifaceType = pInvokedMT->GetComInterfaceType();
        if (!IsDispatchBasedItf(ifaceType))
            COMPlusThrow(kTargetInvocationException, IDS_EE_INTERFACE_NOT_DISPATCH_BASED);
    }

    // Validate that the target is a COM object.
    _ASSERTE((*pTarget)->GetMethodTable()->IsComObjectType());

    //
    // Initialize the DISPPARAMS structure.
    //
    if (cArgs > 0)
    {
        UINT cPositionalArgs = cArgs - cNamedArgs;

        DispParams.cArgs = cArgs;
        DispParams.rgvarg = (VARIANTARG *)_alloca(cArgs * sizeof(VARIANTARG));
        params = new DispParamHolder[cArgs];

        // Initialize all the variants.
        GCX_PREEMP();
        for (i = 0; i < cArgs; i++)
        {
            SafeVariantInit(&DispParams.rgvarg[i]);
            params[i] = &DispParams.rgvarg[i];
        }
    }


    //
    // Retrieve the IDispatch interface that will be invoked on.
    //

    if (pInvokedMT->IsInterface())
    {
        // The invoked type is a dispatch or dual interface so we will make the
        // invocation on it.
        pDisp = (IDispatch *)ComObject::GetComIPFromRCWThrowing(pTarget, pInvokedMT);
    }
    else
    {
        // A class was passed in so we will make the invocation on the default
        // IDispatch for the COM component.

        RCWHolder pRCW(GetThread());
        RCWPROTECT_BEGIN(pRCW, *pTarget);

        // Retrieve the IDispath pointer from the wrapper.
        pDisp = (IDispatch*)pRCW->GetIDispatch();
        if (!pDisp)
            COMPlusThrow(kTargetInvocationException, IDS_EE_NO_IDISPATCH_ON_TARGET);

        // If we aren't ignoring case, then we need to try and QI for IDispatchEx to
        // be able to use IDispatchEx::GetDispID() which has a flag to control case
        // sentisitivity.
        if (!bIgnoreCase && cNamedArgs == 0)
        {
            RCW_VTABLEPTR(pRCW);
            hr = SafeQueryInterface(pDisp, IID_IDispatchEx, (IUnknown**)&pDispEx);
            if (FAILED(hr))
                pDispEx = NULL;
        }

        RCWPROTECT_END(pRCW);
    }
    _ASSERTE((IUnknown*)pDisp != NULL);


    //
    // Prepare the DISPID's that will be passed to invoke.
    //

    if (pMemberID && (*pMemberID != DISPID_UNKNOWN) && (cNamedArgs == 0))
    {
        // The caller specified a member ID and we don't have any named arguments so
        // we can simply use the member ID the caller specified.
        MemberID = *pMemberID;
    }
    else
    {
        int strNameLength = (*pStrName)->GetStringLength();

        // Check if we are invoking on the default member.
        if (strNameLength == 0)
        {
            // Set the DISPID to 0 (default member).
            MemberID = 0;

            _ASSERTE(cNamedArgs == 0);
            if (cNamedArgs != 0)
                COMPlusThrow(kNotSupportedException,W("NotSupported_IDispInvokeDefaultMemberWithNamedArgs"));
        }
        else
        {
            //
            // Create an array of strings that will be passed to GetIDsOfNames().
            //

            UINT cNamesToConvert = cNamedArgs + 1;
            LPWSTR strTmpName = NULL;

            // Allocate the array of strings to convert, the array of pinned handles and the
            // array of converted DISPID's.
            size_t allocSize = cNamesToConvert * sizeof(LPWSTR);
            if (allocSize < cNamesToConvert)
                COMPlusThrowArgumentOutOfRange(W("namedParameters"), W("ArgumentOutOfRange_Capacity"));
            LPWSTR *aNamesToConvert = (LPWSTR *)_alloca(allocSize);

            allocSize = cNamesToConvert * sizeof(DISPID);
            if (allocSize < cNamesToConvert)
                COMPlusThrowArgumentOutOfRange(W("namedParameters"), W("ArgumentOutOfRange_Capacity"));
            aDispID = (DISPID *)_alloca(allocSize);

            // The first name to convert is the name of the method itself.
            aNamesToConvert[0] = (*pStrName)->GetBuffer();

            // Check to see if the name is for a standard DISPID.
            if (SString::_wcsnicmp(aNamesToConvert[0], STANDARD_DISPID_PREFIX, STANDARD_DISPID_PREFIX_LENGTH) == 0)
            {
                // The name is for a standard DISPID so extract it from the name.
                MemberID = ExtractStandardDispId(aNamesToConvert[0]);

                // Make sure there are no named arguments to convert.
                if (cNamedArgs > 0)
                {
                    STRINGREF *pNamedArgsData = (STRINGREF *)(*pArrNamedArgs)->GetDataPtr();

                    for (i = 0; i < cNamedArgs; i++)
                    {
                        // The first name to convert is the name of the method itself.
                        strTmpName = pNamedArgsData[i]->GetBuffer();

                        // Check to see if the name is for a standard DISPID.
                        if (SString::_wcsnicmp(strTmpName, STANDARD_DISPID_PREFIX, STANDARD_DISPID_PREFIX_LENGTH) != 0)
                            COMPlusThrow(kArgumentException, IDS_EE_NON_STD_NAME_WITH_STD_DISPID);

                        // The name is for a standard DISPID so extract it from the name.
                        aDispID[i + 1] = ExtractStandardDispId(strTmpName);
                    }
                }
            }
            else
            {
                BOOL fGotIt = FALSE;
                BOOL fIsNonGenericComObject = pInvokedMT->IsInterface() || (pInvokedMT != g_pBaseCOMObject && pInvokedMT->IsComObjectType());
                BOOL fUseCache = fIsNonGenericComObject && !(IUnknown*)pDispEx && strNameLength <= ReflectionMaxCachedNameLength && cNamedArgs == 0;
                DispIDCacheElement vDispIDElement;

                // If the object is not a generic COM object and the member meets the criteria to be
                // in the cache then look up the DISPID in the cache.
                if (fUseCache)
                {
                    vDispIDElement.pMT = pInvokedMT;
                    vDispIDElement.strNameLength = strNameLength;
                    vDispIDElement.lcid = lcid;
                    wcscpy_s(vDispIDElement.strName, COUNTOF(vDispIDElement.strName), aNamesToConvert[0]);

                    // Only look up if the cache has already been created.
                    DispIDCache* pDispIDCache = GetAppDomain()->GetRefDispIDCache();
                    fGotIt = pDispIDCache->GetFromCache (&vDispIDElement, MemberID);
                }

                if (!fGotIt)
                {
                    NewArrayHolder<PinningHandleHolder> ahndPinnedObjs = new PinningHandleHolder[cNamesToConvert];
                    ahndPinnedObjs[0] = GetAppDomain()->CreatePinningHandle((OBJECTREF)*pStrName);

                    // Copy the named arguments into the array of names to convert.
                    if (cNamedArgs > 0)
                    {
                        STRINGREF *pNamedArgsData = (STRINGREF *)(*pArrNamedArgs)->GetDataPtr();

                        for (i = 0; i < cNamedArgs; i++)
                        {
                            // Pin the string object and retrieve a pointer to its data.
                            ahndPinnedObjs[i + 1] = GetAppDomain()->CreatePinningHandle((OBJECTREF)pNamedArgsData[i]);
                            aNamesToConvert[i + 1] = pNamedArgsData[i]->GetBuffer();
                        }
                    }

                    //
                    // Call GetIDsOfNames to convert the names to DISPID's
                    //

                    {
                    // We are about to make call's to COM so switch to preemptive GC.
                    GCX_PREEMP();

                    if ((IUnknown*)pDispEx)
                    {
                        // We should only get here if we are doing a case sensitive lookup and
                        // we don't have any named arguments.
                        _ASSERTE(cNamedArgs == 0);
                        _ASSERTE(!bIgnoreCase);

                        // We managed to retrieve an IDispatchEx IP so we will use it to
                        // retrieve the DISPID.
                        BSTRHolder bstrTmpName = SysAllocString(aNamesToConvert[0]);
                        if (!bstrTmpName)
                            COMPlusThrowOM();

                        hr = pDispEx->GetDispID(bstrTmpName, fdexNameCaseSensitive, aDispID);
                    }
                    else
                    {
                        // Call GetIdsOfNames() to retrieve the DISPID's of the method and of the arguments.
                        hr = pDisp->GetIDsOfNames(
                                                    IID_NULL,
                                                    aNamesToConvert,
                                                    cNamesToConvert,
                                                    lcid,
                                                    aDispID
                                                );
                    }
                    }

                    if (FAILED(hr))
                    {
                        // Check to see if the user wants to invoke the new enum member.
                        if (cNamesToConvert == 1 && SString::_wcsicmp(aNamesToConvert[0], GET_ENUMERATOR_METHOD_NAME) == 0)
                        {
                            // Invoke the new enum member.
                            MemberID = DISPID_NEWENUM;
                        }
                        else
                        {
                            // The name is unknown.
                            COMPlusThrowHR(hr);
                        }
                    }
                    else
                    {
                        // The member ID is the first elements of the array we got back from GetIdsOfNames.
                        MemberID = aDispID[0];
                    }

                    // If the object is not a generic COM object and the member meets the criteria to be
                    // in the cache then insert the member in the cache.
                    if (fUseCache)
                    {
                        DispIDCache *pDispIDCache = GetAppDomain()->GetRefDispIDCache();
                        pDispIDCache->AddToCache (&vDispIDElement, MemberID);
                    }
                }
            }
        }

        // Store the member ID if the caller passed in a place to store it.
        if (pMemberID)
            *pMemberID = MemberID;
    }


    //
    // Fill in the DISPPARAMS structure.
    //

    if (cArgs > 0)
    {
        // Allocate the byref argument information.
        aByrefArgInfos = (ByrefArgumentInfo*)_alloca(cArgs * sizeof(ByrefArgumentInfo));
        memset(aByrefArgInfos, 0, cArgs * sizeof(ByrefArgumentInfo));

        // Set the byref bit on the arguments that have the byref modifier.
        if (*pArrByrefModifiers != NULL)
        {
            BYTE *aByrefModifiers = (BYTE*)(*pArrByrefModifiers)->GetDataPtr();
            for (i = 0; i < cArgs; i++)
            {
                if (aByrefModifiers[i])
                {
                    aByrefArgInfos[i].m_bByref = TRUE;
                    bSomeArgsAreByref = TRUE;
                }
            }
        }

        // We need to protect the temporary object that will be used to convert from
        // the managed objects to OLE variants.
        OBJECTREF TmpObj = NULL;
        GCPROTECT_BEGIN(TmpObj)
        {
            if (!(flags & (DISPATCH_PROPERTYPUT | DISPATCH_PROPERTYPUTREF)))
            {
                // For anything other than a put or a putref we just use the specified
                // named arguments.
                DispParams.cNamedArgs = cNamedArgs;
                DispParams.rgdispidNamedArgs = (cNamedArgs == 0) ? NULL : &aDispID[1];

                // Convert the named arguments from COM+ to OLE. These arguments are in the same order
                // on both sides.
                for (i = 0; i < cNamedArgs; i++)
                {
                    iSrcArg = i;
                    iDestArg = i;
                    TmpObj = ((OBJECTREF*)(*pArrArgs)->GetDataPtr())[iSrcArg];
                    DispInvokeConvertObjectToVariant(&TmpObj, &DispParams.rgvarg[iDestArg], &aByrefArgInfos[iSrcArg]);
                }

                // Convert the unnamed arguments. These need to be presented in reverse order to IDispatch::Invoke().
                for (iSrcArg = cNamedArgs, iDestArg = cArgs - 1; iSrcArg < cArgs; iSrcArg++, iDestArg--)
                {
                    TmpObj = ((OBJECTREF*)(*pArrArgs)->GetDataPtr())[iSrcArg];
                    DispInvokeConvertObjectToVariant(&TmpObj, &DispParams.rgvarg[iDestArg], &aByrefArgInfos[iSrcArg]);
                }
            }
            else
            {
                // If we are doing a property put then we need to set the DISPID of the
                // argument to DISP_PROPERTYPUT if there is at least one argument.
                DispParams.cNamedArgs = cNamedArgs + 1;
                DispParams.rgdispidNamedArgs = (DISPID*)_alloca((cNamedArgs + 1) * sizeof(DISPID));

                // Fill in the array of named arguments.
                DispParams.rgdispidNamedArgs[0] = DISPID_PROPERTYPUT;
                for (i = 1; i < cNamedArgs; i++)
                    DispParams.rgdispidNamedArgs[i] = aDispID[i];

                // The last argument from reflection becomes the first argument that must be passed to IDispatch.
                iSrcArg = cArgs - 1;
                iDestArg = 0;
                TmpObj = ((OBJECTREF*)(*pArrArgs)->GetDataPtr())[iSrcArg];
                DispInvokeConvertObjectToVariant(&TmpObj, &DispParams.rgvarg[iDestArg], &aByrefArgInfos[iSrcArg]);

                // Convert the named arguments from COM+ to OLE. These arguments are in the same order
                // on both sides.
                for (i = 0; i < cNamedArgs; i++)
                {
                    iSrcArg = i;
                    iDestArg = i + 1;
                    TmpObj = ((OBJECTREF*)(*pArrArgs)->GetDataPtr())[iSrcArg];
                    DispInvokeConvertObjectToVariant(&TmpObj, &DispParams.rgvarg[iDestArg], &aByrefArgInfos[iSrcArg]);
                }

                // Convert the unnamed arguments. These need to be presented in reverse order to IDispatch::Invoke().
                for (iSrcArg = cNamedArgs, iDestArg = cArgs - 1; iSrcArg < cArgs - 1; iSrcArg++, iDestArg--)
                {
                    TmpObj = ((OBJECTREF*)(*pArrArgs)->GetDataPtr())[iSrcArg];
                    DispInvokeConvertObjectToVariant(&TmpObj, &DispParams.rgvarg[iDestArg], &aByrefArgInfos[iSrcArg]);
                }
            }
        }
        GCPROTECT_END();
    }
    else
    {
        // There are no arguments.
        DispParams.cArgs = cArgs;
        DispParams.cNamedArgs = 0;
        DispParams.rgdispidNamedArgs = NULL;
        DispParams.rgvarg = NULL;
    }

    // If we're calling on DISPID=-4, then pass both METHOD and PROPERTYGET
    if (MemberID == DISPID_NEWENUM)
    {
        _ASSERTE((flags & DISPATCH_METHOD) && "Expected DISPATCH_METHOD to be set.");
        flags |= DISPATCH_METHOD | DISPATCH_PROPERTYGET;
    }

    //
    // Call invoke on the target's IDispatch.
    //

    if (!bIgnoreReturn)
        pVarResult = &VarResult;

    DoIUInvokeDispMethod(pDispEx, pDisp, MemberID, lcid, flags, &DispParams, pVarResult);


    //
    // Return value handling and cleanup.
    //

    // Back propagate any byref args.
    if (bSomeArgsAreByref)
    {
        OBJECTREF TmpObj = NULL;
        GCPROTECT_BEGIN(TmpObj)
        {
            for (i = 0; i < cArgs; i++)
            {
                if (aByrefArgInfos[i].m_bByref)
                {
                    // Convert the variant back to an object.
                    OleVariant::MarshalObjectForOleVariant(&aByrefArgInfos[i].m_Val, &TmpObj);
                    (*pArrArgs)->SetAt(i, TmpObj);
                }
            }
        }
        GCPROTECT_END();
    }

    if (!bIgnoreReturn)
    {
        if (MemberID == DISPID_NEWENUM)
        {
            //
            // Use a custom marshaler to convert the IEnumVARIANT to an IEnumerator.
            //

            // Start by making sure that the variant we got back contains an IP.
            if ((VarResult.vt != VT_UNKNOWN) || !VarResult.punkVal)
                COMPlusThrow(kInvalidCastException, IDS_EE_INVOKE_NEW_ENUM_INVALID_RETURN);

            // Have the custom marshaler do the conversion.
            *pRetVal = ConvertEnumVariantToMngEnum((IEnumVARIANT *)VarResult.punkVal);
        }
        else
        {
            // Convert the return variant to a COR variant.
            OleVariant::MarshalObjectForOleVariant(&VarResult, pRetVal);
        }
    }
}

#if defined(FEATURE_COMINTEROP_UNMANAGED_ACTIVATION) && defined(FEATURE_COMINTEROP)

static void GetComClassHelper(
    _Out_ OBJECTREF *pRef,
    _In_ EEClassFactoryInfoHashTable *pClassFactHash,
    _In_ ClassFactoryInfo *pClassFactInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(ThrowOutOfMemory());
        PRECONDITION(CheckPointer(pRef));
        PRECONDITION(CheckPointer(pClassFactHash));
        PRECONDITION(CheckPointer(pClassFactInfo));
    }
    CONTRACTL_END;

    OBJECTHANDLE hRef;
    AppDomain *pDomain = GetAppDomain();

    CrstHolder ch(pDomain->GetRefClassFactCrst());

    // Check again.
    if (pClassFactHash->GetValue(pClassFactInfo, (HashDatum *)&hRef))
    {
        *pRef = ObjectFromHandle(hRef);
    }
    else
    {
        //
        // There is no managed class for this CLSID
        // so we will create a ComClassFactory to
        // represent it.
        //

        NewHolder<ComClassFactory> pComClsFac = new ComClassFactory(pClassFactInfo->m_clsid);
        pComClsFac->SetManagedVersion();

        NewArrayHolder<WCHAR> wszRefServer = NULL;
        if (pClassFactInfo->m_strServerName)
        {
            size_t len = wcslen(pClassFactInfo->m_strServerName)+1;
            wszRefServer = new WCHAR[len];
            wcscpy_s(wszRefServer, len, pClassFactInfo->m_strServerName);
        }

        pComClsFac->Init(wszRefServer, NULL);
        AllocateComClassObject(pComClsFac, pRef);

        // Insert to hash.
        hRef = pDomain->CreateHandle(*pRef);
        pClassFactHash->InsertValue(pClassFactInfo, (LPVOID)hRef);

        // Make sure the hash code is working.
        _ASSERTE (pClassFactHash->GetValue(pClassFactInfo, (HashDatum *)&hRef));

        wszRefServer.SuppressRelease();
        pComClsFac.SuppressRelease();
    }
}

//-------------------------------------------------------------
// returns a ComClass reflect class that wraps the IClassFactory
void GetComClassFromCLSID(REFCLSID clsid, _In_opt_z_ PCWSTR wszServer, OBJECTREF *pRef)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(pRef != NULL);
    }
    CONTRACTL_END;

    //
    // See if we can find the well known managed class for this CLSID.
    //

    // Check if we have in the hash.
    OBJECTHANDLE hRef;
    ClassFactoryInfo ClassFactInfo;
    ClassFactInfo.m_clsid = clsid;
    ClassFactInfo.m_strServerName = wszServer;
    EEClassFactoryInfoHashTable *pClassFactHash = GetAppDomain()->GetClassFactHash();

    if (pClassFactHash->GetValue(&ClassFactInfo, (HashDatum*) &hRef))
    {
        *pRef = ObjectFromHandle(hRef);
    }
    else
    {
        GetComClassHelper(pRef, pClassFactHash, &ClassFactInfo);
    }

    // If we made it this far *pRef better be set.
    _ASSERTE(*pRef != NULL);
}

#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION && FEATURE_COMINTEROP



#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
//-------------------------------------------------------------
// check if a ComClassFactory has been setup for this class
// if not set one up
ClassFactoryBase *GetComClassFactory(MethodTable* pClassMT)
{
    CONTRACT (ClassFactoryBase*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(ThrowOutOfMemory());
        PRECONDITION(CheckPointer(pClassMT));
        PRECONDITION(pClassMT->IsComObjectType());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Work our way up the hierachy until we find the first COM import type.
    while (!pClassMT->IsComImport())
    {
        pClassMT = pClassMT->GetParentMethodTable();
        _ASSERTE(pClassMT != NULL);
        _ASSERTE(pClassMT->IsComObjectType());
    }

    // check if com data has been setup for this class
    ClassFactoryBase *pClsFac = pClassMT->GetComClassFactory();

    if (pClsFac == NULL)
    {
        NewHolder<ClassFactoryBase> pNewFactory;

        GUID guid;
        pClassMT->GetGuid(&guid, TRUE);

        ComClassFactory *pComClsFac = new ComClassFactory(guid);

        pNewFactory = pComClsFac;

        pComClsFac->Init(NULL, pClassMT);

        // store the class factory in EE Class
        if (!pClassMT->SetComClassFactory(pNewFactory))
        {
            // another thread beat us to it
            pNewFactory = pClassMT->GetComClassFactory();
        }

        pClsFac = pNewFactory.Extract();
    }

    RETURN pClsFac;
}
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION


//-------------------------------------------------------------------
// void InitializeComInterop()
// Called from EEStartup, to initialize com Interop specific data
// structures.
//-------------------------------------------------------------------
void InitializeComInterop()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    InitializeSListHead(&RCW::s_RCWStandbyList);
    ComCall::Init();
#ifdef TARGET_X86
    ComPlusCall::Init();
#endif
    CtxEntryCache::Init();
    ComCallWrapperTemplate::Init();
#ifdef _DEBUG
    IntializeInteropLogging();
#endif //_DEBUG
}


#ifdef _DEBUG
//-------------------------------------------------------------------
// LOGGING APIS
//-------------------------------------------------------------------

static int g_TraceCount = 0;
static IUnknown* g_pTraceIUnknown = NULL;

VOID IntializeInteropLogging()
{
    WRAPPER_NO_CONTRACT;

    g_TraceCount = g_pConfig->GetTraceWrapper();
}

VOID LogInterop(_In_z_ LPCSTR szMsg)
{
    LIMITED_METHOD_CONTRACT;
    LOG( (LF_INTEROP, LL_INFO10, "%s\n",szMsg) );
}

VOID LogInterop(_In_z_ LPCWSTR wszMsg)
{
    LIMITED_METHOD_CONTRACT;
    LOG( (LF_INTEROP, LL_INFO10, "%S\n", wszMsg) );
}

//-------------------------------------------------------------------
// VOID LogRCWCreate(RCW* pWrap, IUnknown* pUnk)
// log wrapper create
//-------------------------------------------------------------------
VOID LogRCWCreate(RCW* pWrap, IUnknown* pUnk)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    static int count = 0;
    LPVOID pCurrCtx = GetCurrentCtxCookie();

    // pre-increment the count, so it can never be zero
    count++;

    if (count == g_TraceCount)
    {
        g_pTraceIUnknown = pUnk;
    }

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pUnk)
    {
        LOG( (LF_INTEROP,
            LL_INFO10,
            "Create RCW: Wrapper %p #%d IUnknown:%p Context %p\n",
            pWrap, count,
            pUnk,
            pCurrCtx) );
    }
}

//-------------------------------------------------------------------
// VOID LogRCWMinorCleanup(RCW* pWrap)
// log wrapper minor cleanup
//-------------------------------------------------------------------
VOID LogRCWMinorCleanup(RCW* pWrap)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pWrap));
    }
    CONTRACTL_END;

    static int dest_count = 0;
    dest_count++;

    IUnknown *pUnk = pWrap->GetRawIUnknown_NoAddRef_NoThrow();

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pUnk)
    {
        LPVOID pCurrCtx = GetCurrentCtxCookie();
        LOG( (LF_INTEROP,
            LL_INFO10,
            "Minor Cleanup RCW: Wrapper %p #%d IUnknown %p Context: %p\n",
            pWrap, dest_count,
            pUnk,
            pCurrCtx) );
    }
}

//-------------------------------------------------------------------
// VOID LogRCWDestroy(RCW* pWrap, IUnknown* pUnk)
// log wrapper destroy
//-------------------------------------------------------------------
VOID LogRCWDestroy(RCW* pWrap)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pWrap));
    }
    CONTRACTL_END;

    static int dest_count = 0;
    dest_count++;

    IUnknown *pUnk = pWrap->GetRawIUnknown_NoAddRef_NoThrow();

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pUnk)
    {
        LPVOID pCurrCtx = GetCurrentCtxCookie();
        STRESS_LOG4(
            LF_INTEROP,
            LL_INFO10,
            "Destroy RCW: Wrapper %p #%d IUnknown %p Context: %p\n",
            pWrap, dest_count,
            pUnk,
            pCurrCtx);
    }
}

//-------------------------------------------------------------------
// VOID LogInteropLeak(IUnkEntry * pEntry)
//-------------------------------------------------------------------
VOID LogInteropLeak(IUnkEntry * pEntry)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pEntry));
    }
    CONTRACTL_END;

    IUnknown *pUnk = pEntry->GetRawIUnknown_NoAddRef_NoThrow();

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pUnk)
    {
        LOG( (LF_INTEROP,
            LL_INFO10,
            "IUnkEntry Leak: %p Context: %p\n",
            pUnk,
            pEntry->GetCtxCookie()) );
    }
}

//-------------------------------------------------------------------
//  VOID LogInteropLeak(IUnknown* pItf)
//-------------------------------------------------------------------
VOID LogInteropLeak(IUnknown* pItf)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LPVOID              pCurrCtx    = NULL;

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pItf)
    {
        pCurrCtx = GetCurrentCtxCookie();
        LOG((LF_INTEROP,
            LL_EVERYTHING,
            "Leak: Itf = %p, CurrCtx = %p\n",
            pItf, pCurrCtx));
    }
}

//-------------------------------------------------------------------
// VOID LogInteropQI(IUnknown* pItf, REFIID iid, HRESULT hr, LPCSTR szMsg)
//-------------------------------------------------------------------
VOID LogInteropQI(IUnknown* pItf, REFIID iid, HRESULT hrArg, _In_z_ LPCSTR szMsg)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pItf));
    }
    CONTRACTL_END;

    LPVOID              pCurrCtx    = NULL;
    HRESULT             hr          = S_OK;
    SafeComHolder<IUnknown> pUnk        = NULL;
    int                 cch         = 0;
    WCHAR               wszIID[64];

    hr = SafeQueryInterface(pItf, IID_IUnknown, &pUnk);

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pUnk)
    {
        pCurrCtx = GetCurrentCtxCookie();

        cch = StringFromGUID2(iid, wszIID, sizeof(wszIID) / sizeof(WCHAR));
        _ASSERTE(cch > 0);

        if (SUCCEEDED(hrArg))
        {
            LOG((LF_INTEROP,
                LL_EVERYTHING,
                "Succeeded QI: Unk = %p, Itf = %p, CurrCtx = %p, IID = %S, Msg: %s\n",
                (IUnknown*)pUnk, pItf, pCurrCtx, wszIID, szMsg));
        }
        else
        {
            LOG((LF_INTEROP,
                LL_EVERYTHING,
                "Failed QI: Unk = %p, Itf = %p, CurrCtx = %p, IID = %S, HR = %p, Msg: %s\n",
                (IUnknown*)pUnk, pItf, pCurrCtx, wszIID, hrArg, szMsg));
        }
    }
}

//-------------------------------------------------------------------
//  VOID LogInteropAddRef(IUnknown* pItf, ULONG cbRef, LPCSTR szMsg)
//-------------------------------------------------------------------
VOID LogInteropAddRef(IUnknown* pItf, ULONG cbRef, _In_z_ LPCSTR szMsg)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pItf));
    }
    CONTRACTL_END;

    LPVOID              pCurrCtx    = NULL;
    HRESULT             hr          = S_OK;
    SafeComHolder<IUnknown> pUnk        = NULL;

    hr = SafeQueryInterface(pItf, IID_IUnknown, &pUnk);

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pUnk)
    {
        pCurrCtx = GetCurrentCtxCookie();
        LOG((LF_INTEROP,
            LL_EVERYTHING,
            "AddRef: Unk = %p, Itf = %p, CurrCtx = %p, RefCount = %d, Msg: %s\n",
            (IUnknown*)pUnk, pItf, pCurrCtx, cbRef, szMsg));
    }
}

//-------------------------------------------------------------------
//  VOID LogInteropRelease(IUnknown* pItf, ULONG cbRef, LPCSTR szMsg)
//-------------------------------------------------------------------
VOID LogInteropRelease(IUnknown* pItf, ULONG cbRef, _In_z_ LPCSTR szMsg)
{
    if (!LoggingOn(LF_INTEROP, LL_ALWAYS))
        return;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pItf, NULL_OK));
    }
    CONTRACTL_END;

    LPVOID pCurrCtx = NULL;

    if (g_pTraceIUnknown == 0 || g_pTraceIUnknown == pItf)
    {
        pCurrCtx = GetCurrentCtxCookie();
        LOG((LF_INTEROP,
            LL_EVERYTHING,
            "Release: Itf = %p, CurrCtx = %p, RefCount = %d, Msg: %s\n",
            pItf, pCurrCtx, cbRef, szMsg));
    }
}

#endif // _DEBUG

IUnknown* MarshalObjectToInterface(OBJECTREF* ppObject, MethodTable* pItfMT, MethodTable* pClassMT, DWORD dwFlags)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // When an interface method table is specified, fDispIntf must be consistent with the
    // interface type.
    BOOL bDispatch = (dwFlags & ItfMarshalInfo::ITF_MARSHAL_DISP_ITF);
    BOOL bUseBasicItf = (dwFlags & ItfMarshalInfo::ITF_MARSHAL_USE_BASIC_ITF);

    _ASSERTE(!pItfMT || (!pItfMT->IsInterface() && bDispatch) ||
             (!!bDispatch == IsDispatchBasedItf(pItfMT->GetComInterfaceType())));

    if (pItfMT)
    {
        return GetComIPFromObjectRef(ppObject, pItfMT);
    }
    else if (!bUseBasicItf)
    {
        return GetComIPFromObjectRef(ppObject, pClassMT);
    }
    else
    {
        ComIpType ReqIpType = bDispatch ? ComIpType_Dispatch : ComIpType_Unknown;
        return GetComIPFromObjectRef(ppObject, ReqIpType, NULL);
    }
}

void UnmarshalObjectFromInterface(OBJECTREF *ppObjectDest, IUnknown **ppUnkSrc, MethodTable *pItfMT, MethodTable *pClassMT, DWORD dwFlags)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        PRECONDITION(IsProtectedByGCFrame(ppObjectDest));
    }
    CONTRACTL_END;

    _ASSERTE(!pClassMT || !pClassMT->IsInterface());

    DWORD dwObjFromComIPFlags = ObjFromComIP::FromItfMarshalInfoFlags(dwFlags);
    GetObjectRefFromComIP(
        ppObjectDest,                  // Object
        ppUnkSrc,                      // Interface pointer
        pClassMT,                      // Class type
        dwObjFromComIPFlags            // Flags
        );

    // Make sure the interface is supported.
    _ASSERTE(!pItfMT || pItfMT->IsInterface() || pItfMT->GetComClassInterfaceType() != clsIfNone);

    bool fIsInterface = (pItfMT != NULL && pItfMT->IsInterface());
    if (fIsInterface)
    {
        // We only verify that the object supports the interface for non-WinRT scenarios because we
        // believe that the likelihood of improperly constructed programs is significantly lower
        // with WinRT and the Object::SupportsInterface check is very expensive.
        if (!Object::SupportsInterface(*ppObjectDest, pItfMT))
        {
            COMPlusThrowInvalidCastException(ppObjectDest, TypeHandle(pItfMT));
        }
    }
}


#endif // FEATURE_COMINTEROP
