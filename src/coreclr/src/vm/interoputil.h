// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _H_INTEROP_UTIL
#define _H_INTEROP_UTIL

#include "debugmacros.h"
#include "interopconverter.h"

struct VariantData;

// Out of memory helper.
#define IfNullThrow(EXPR) \
do {if ((EXPR) == 0) {ThrowOutOfMemory();} } while (0)


// Helper to determine the version number from an int.
#define GET_VERSION_USHORT_FROM_INT(x) ((x < 0) || (x > (INT)((USHORT)-1))) ? 0 : static_cast<USHORT>(x)

#ifdef FEATURE_COMINTEROP
#include "winrttypenameconverter.h"
#include "roparameterizediid.h"
#include "../md/winmd/inc/adapter.h"
#include <windows.foundation.h>

// The format string to use to format unknown members to be passed to
// invoke member
#define DISPID_NAME_FORMAT_STRING                       W("[DISPID=%i]")

//---------------------------------------------------------------------------
// This method returns the default interface for the class as well as the 
// type of default interface we are dealing with.
enum DefaultInterfaceType
{
    DefaultInterfaceType_Explicit       = 0,
    DefaultInterfaceType_IUnknown       = 1,
    DefaultInterfaceType_AutoDual       = 2,
    DefaultInterfaceType_AutoDispatch   = 3,
    DefaultInterfaceType_BaseComClass   = 4
};

// System.Drawing.Color struct definition.

struct SYSTEMCOLOR
{
#ifdef _WIN64    
    STRINGREF name;
    INT64     value;
#else
    INT64     value;
    STRINGREF name;
#endif
    short     knownColor;
    short     state;
};

struct ComMethodTable;
struct IUnkEntry;
interface IStream;
class ComCallWrapper;
class InteropSyncBlockInfo;

#endif //FEATURE_COMINTEROP

#if FEATURE_COMINTEROP
#include <restrictederrorInfo.h>
#endif
#ifndef __IRestrictedErrorInfo_INTERFACE_DEFINED__
DEFINE_GUID(IID_IRestrictedErrorInfo, 0x82BA7092,0x4C88,0x427D,0xA7,0xBC,0x16,0xDD,0x93,0xFE,0xB6,0x7E);
MIDL_INTERFACE("82BA7092-4C88-427D-A7BC-16DD93FEB67E")
IRestrictedErrorInfo : public IUnknown
{
public:
};
#endif // !__IRestrictedErrorInfo_INTERFACE_DEFINED__

class FieldDesc;
struct ExceptionData;

//------------------------------------------------------------------
 // setup error info for exception object
//
#ifdef FEATURE_COMINTEROP
HRESULT SetupErrorInfo(OBJECTREF pThrownObject, ComCallMethodDesc *pCMD);
HRESULT SafeGetRestrictedErrorInfo(IRestrictedErrorInfo **ppIErrInfo);
BOOL IsManagedObject(IUnknown *pErrInfo);
IErrorInfo *GetCorrepondingErrorInfo_WinRT(HRESULT hr, IRestrictedErrorInfo *pResErrInfo, BOOL* bHasLangRestrictedErrInfo);
HRESULT GetRestrictedErrorDetails(IRestrictedErrorInfo *pRestrictedErrorInfo, BSTR *perrorDescription, BSTR *pErrorRestrictedDescription, HRESULT *hr, BSTR *pErrorCapabilitySid);

#endif // FEATURE_COMINTEROP

HRESULT SetupErrorInfo(OBJECTREF pThrownObject, BOOL bIsWinRTScenario = FALSE);

//--------------------------------------------------------------------------------
 // Release helper, enables and disables GC during call-outs
ULONG SafeRelease(IUnknown* pUnk, RCW* pRCW = NULL);

//--------------------------------------------------------------------------------
// Release helper, must be called in preemptive mode.  Only use this variant if
// you already know you're in preemptive mode for other reasons.  
ULONG SafeReleasePreemp(IUnknown* pUnk, RCW* pRCW = NULL);

//--------------------------------------------------------------------------------
// Determines if a COM object can be cast to the specified type.
BOOL CanCastComObject(OBJECTREF obj, MethodTable * pTargetMT);

// includes Types which hold a "ComObject" class
// and types which are imported through typelib
BOOL IsComWrapperClass(TypeHandle type);

// includes Type which hold a "__ComObject" class
BOOL IsComObjectClass(TypeHandle type);

//---------------------------------------------------------
// Read the BestFit custom attribute info from 
// both assembly level and interface level
//---------------------------------------------------------
VOID ReadBestFitCustomAttribute(MethodDesc* pMD, BOOL* BestFit, BOOL* ThrowOnUnmappableChar);
VOID ReadBestFitCustomAttribute(Module* pModule, mdTypeDef cl, BOOL* BestFit, BOOL* ThrowOnUnmappableChar);
int  InternalWideToAnsi(__in_ecount(iNumWideChars) LPCWSTR szWideString, int iNumWideChars, __out_ecount_opt(cbAnsiBufferSize) LPSTR szAnsiString, int cbAnsiBufferSize, BOOL fBestFit, BOOL fThrowOnUnmappableChar);

//---------------------------------------------------------
// Read the ClassInterfaceType custom attribute info from 
// both assembly level and interface level
//---------------------------------------------------------
CorClassIfaceAttr ReadClassInterfaceTypeCustomAttribute(TypeHandle type);

//-------------------------------------------------------------------
 // Used to populate ExceptionData with COM data
//-------------------------------------------------------------------
void FillExceptionData(
    _Inout_ ExceptionData* pedata,
    _In_ IErrorInfo* pErrInfo,
    _In_opt_ IRestrictedErrorInfo* pRestrictedErrorInfo);

//---------------------------------------------------------------------------
// If pImport has the DefaultDllImportSearchPathsAttribute, 
// set the value of the attribute in pDlImportSearchPathFlags and return true.
BOOL GetDefaultDllImportSearchPathsAttributeValue(Module *pModule, mdToken token, DWORD * pDlImportSearchPathFlags);

//---------------------------------------------------------------------------
// Returns the index of the LCID parameter if one exists and -1 otherwise.
int GetLCIDParameterIndex(MethodDesc *pMD);

//---------------------------------------------------------------------------
// Transforms an LCID into a CultureInfo.
void GetCultureInfoForLCID(LCID lcid, OBJECTREF *pCultureObj);

//---------------------------------------------------------------------------
// This method determines if a member is visible from COM.
BOOL IsMemberVisibleFromCom(MethodTable *pDeclaringMT, mdToken tk, mdMethodDef mdAssociate);

//--------------------------------------------------------------------------------
// This method generates a stringized version of an interface that contains the
// name of the interface along with the signature of all the methods.
SIZE_T GetStringizedItfDef(TypeHandle InterfaceType, CQuickArray<BYTE> &rDef);

//--------------------------------------------------------------------------------
// Helper to get the stringized form of typelib guid.
HRESULT GetStringizedTypeLibGuidForAssembly(Assembly *pAssembly, CQuickArray<BYTE> &rDef, ULONG cbCur, ULONG *pcbFetched);

//--------------------------------------------------------------------------------
// GetErrorInfo helper, enables and disables GC during call-outs
HRESULT SafeGetErrorInfo(_Outptr_ IErrorInfo **ppIErrInfo);

//--------------------------------------------------------------------------------
// QI helper, enables and disables GC during call-outs
HRESULT SafeQueryInterface(IUnknown* pUnk, REFIID riid, IUnknown** pResUnk);

//--------------------------------------------------------------------------------
// QI helper, must be called in preemptive mode.  Faster than the MODE_ANY version 
// because it doesn't need to toggle the mode.  Use this version only if you already
// know that you're in preemptive mode for other reasons.
HRESULT SafeQueryInterfacePreemp(IUnknown* pUnk, REFIID riid, IUnknown** pResUnk);

#ifdef FEATURE_COMINTEROP

// Convert an IUnknown to CCW, does not handle aggregation and ICustomQI.
ComCallWrapper* MapIUnknownToWrapper(IUnknown* pUnk);

// Convert an IUnknown to CCW, returns NULL if the pUnk is not on
// a managed tear-off (OR) if the pUnk is to a managed tear-off that
// has been aggregated
ComCallWrapper* GetCCWFromIUnknown(IUnknown* pUnk, BOOL bEnableCustomization = TRUE);

// A version of LoadRegTypeLib that loads based on bitness and platform support
//  and loads with LCID == LOCALE_USER_DEFAULT
HRESULT LoadRegTypeLib(_In_ REFGUID guid,
                       _In_ unsigned short wVerMajor,
                       _In_ unsigned short wVerMinor,
                       _Outptr_ ITypeLib **pptlib);

//--------------------------------------------------------------------------------
// Called from EEStartup, to initialize com Interop specific data structures.
void InitializeComInterop();

//--------------------------------------------------------------------------------
// Clean up Helpers
//--------------------------------------------------------------------------------
// called by syncblock, on the finalizer thread to do major cleanup
void CleanupSyncBlockComData(InteropSyncBlockInfo* pInteropInfo);

// called by syncblock, during GC, do only minimal work
void MinorCleanupSyncBlockComData(InteropSyncBlockInfo* pInteropInfo);

// Helper to release all of the RCWs in the specified context, across all caches.
// If context is null, release all RCWs, otherwise release RCWs created in the
// given context, including Jupiter RCWs
void ReleaseRCWsInCaches(LPVOID pCtxCookie);

// A wrapper that catches all exceptions - used in the OnThreadTerminate case.
void ReleaseRCWsInCachesNoThrow(LPVOID pCtxCookie);


//--------------------------------------------------------------------------------
// AddRef helper, enables and disables GC during call-outs
ULONG SafeAddRef(IUnknown* pUnk);
//--------------------------------------------------------------------------------
// AddRef helper, must be called in preemptive mode.  Only use this variant if
// you already know you're in preemptive mode for other reasons.  
ULONG SafeAddRefPreemp(IUnknown* pUnk);

//--------------------------------------------------------------------------------
// Release helper, enables and disables GC during call-outs
HRESULT SafeVariantChangeType(_Inout_ VARIANT* pVarRes, _In_ VARIANT* pVarSrc,
                              unsigned short wFlags, VARTYPE vt);

//--------------------------------------------------------------------------------
// Release helper, enables and disables GC during call-outs
HRESULT SafeVariantChangeTypeEx(_Inout_ VARIANT* pVarRes, _In_ VARIANT* pVarSrc,
                          LCID lcid, unsigned short wFlags, VARTYPE vt);

//--------------------------------------------------------------------------------
// Init helper, enables and disables GC during call-outs
void SafeVariantInit(VARIANT* pVar);

//--------------------------------------------------------------------------------
// Releases the data in the stream and then releases the stream itself.
void SafeReleaseStream(IStream *pStream);

//--------------------------------------------------------------------------------
// Ole RPC seems to return an inconsistent SafeArray for arrays created with
// SafeArrayVector(VT_BSTR). OleAut's SafeArrayGetVartype() doesn't notice
// the inconsistency and returns a valid-seeming (but wrong vartype.)
// Our version is more discriminating. This should only be used for
// marshaling scenarios where we can assume unmanaged code permissions
// (and hence are already in a position of trusting unmanaged data.)
HRESULT ClrSafeArrayGetVartype(_In_ SAFEARRAY *psa, _Out_ VARTYPE *pvt);

//Helpers

//
// Macros that defines how to recognize tear off
//
#define TEAR_OFF_SLOT           1
#define TEAR_OFF_STANDARD       Unknown_AddRef
#define TEAR_OFF_SIMPLE_INNER   Unknown_AddRefInner
#define TEAR_OFF_SIMPLE         Unknown_AddRefSpecial

BOOL ComInterfaceSlotIs(IUnknown* pUnk, int slot, LPVOID pvFunction);

// Is the tear-off a CLR created tear-off
BOOL IsInProcCCWTearOff(IUnknown* pUnk);
    
// is the tear-off represent one of the standard interfaces such as IProvideClassInfo, IErrorInfo etc.
BOOL IsSimpleTearOff(IUnknown* pUnk);

// Is the tear-off represent the inner unknown or the original unknown for the object
BOOL IsInnerUnknown(IUnknown* pUnk);

// Is this one of our "standard" ComCallWrappers
BOOL IsStandardTearOff(IUnknown* pUnk);

//---------------------------------------------------------------------------
 //  is the iid represent an IClassX for this class
BOOL IsIClassX(MethodTable *pMT, REFIID riid, ComMethodTable **ppComMT);

// Returns TRUE if we support IClassX for the given class.
BOOL ClassSupportsIClassX(MethodTable *pMT);

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
//---------------------------------------------------------------------------
 //  Calls COM class factory and instantiates a new RCW.
OBJECTREF AllocateComObject_ForManaged(MethodTable* pMT);
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
 
//---------------------------------------------------------------------------
 //  get/load data for a given clsid
MethodTable* GetTypeForCLSID(REFCLSID rclsid, BOOL* pfAssemblyInReg = NULL);


//---------------------------------------------------------------------------
 //  get/load a value class for a given guid
#ifdef FEATURE_CLASSIC_COMINTEROP
MethodTable* GetValueTypeForGUID(REFCLSID guid);
#endif

DefaultInterfaceType GetDefaultInterfaceForClassInternal(TypeHandle hndClass, TypeHandle *pHndDefClass);
DefaultInterfaceType GetDefaultInterfaceForClassWrapper(TypeHandle hndClass, TypeHandle *pHndDefClass);

HRESULT TryGetDefaultInterfaceForClass(TypeHandle hndClass, TypeHandle *pHndDefClass, DefaultInterfaceType *pDefItfType);

MethodTable *GetDefaultInterfaceMTForClass(MethodTable *pMT, BOOL *pbDispatch);

//---------------------------------------------------------------------------
// This method retrieves the list of source interfaces for a given class.
void GetComSourceInterfacesForClass(MethodTable *pClassMT, CQuickArray<MethodTable *> &rItfList);

//--------------------------------------------------------------------------------
// This methods converts an IEnumVARIANT to a managed IEnumerator.
OBJECTREF ConvertEnumVariantToMngEnum(IEnumVARIANT *pNativeEnum);

//--------------------------------------------------------------------------------
// These methods convert an OLE_COLOR to a System.Color and vice versa.
void ConvertOleColorToSystemColor(OLE_COLOR SrcOleColor, SYSTEMCOLOR *pDestSysColor);
OLE_COLOR ConvertSystemColorToOleColor(SYSTEMCOLOR *pSrcSysColor);
OLE_COLOR ConvertSystemColorToOleColor(OBJECTREF *pSrcObj);

//--------------------------------------------------------------------------------
// This method generates a stringized version of a class interface that contains 
// the signatures of all the methods and fields.
ULONG GetStringizedClassItfDef(TypeHandle InterfaceType, CQuickArray<BYTE> &rDef);

//--------------------------------------------------------------------------------
// Helper to get the GUID of a class interface.
void GenerateClassItfGuid(TypeHandle InterfaceType, GUID *pGuid);

// Try/Catch wrapped version of the method.
HRESULT TryGenerateClassItfGuid(TypeHandle InterfaceType, GUID *pGuid);

//--------------------------------------------------------------------------------
// Helper to get the GUID of the typelib that is created from an assembly.
HRESULT GetTypeLibGuidForAssembly(Assembly *pAssembly, GUID *pGuid);

//--------------------------------------------------------------------------------
// Helper to get the version of the typelib that is created from an assembly.
HRESULT GetTypeLibVersionForAssembly(
    _In_ Assembly *pAssembly,
    _Out_ USHORT *pMajorVersion,
    _Out_ USHORT *pMinorVersion);

//---------------------------------------------------------------------------
// This method determines if a member is visible from COM.
BOOL IsMethodVisibleFromCom(MethodDesc *pMD);

//---------------------------------------------------------------------------
// This method determines if a type is visible from COM or not based on 
// its visibility. This version of the method works with a type handle.
BOOL IsTypeVisibleFromCom(TypeHandle hndType);

//---------------------------------------------------------------------------
// Determines if a method is likely to be used for forward COM/WinRT interop.
BOOL MethodNeedsForwardComStub(MethodDesc *pMD, DataImage *pImage);

//---------------------------------------------------------------------------
// Determines if a method is visible from COM in a way that requires a marshaling stub.
BOOL MethodNeedsReverseComStub(MethodDesc *pMD);

//--------------------------------------------------------------------------------
// InvokeDispMethod will convert a set of managed objects and call IDispatch.  The
// result will be returned as a COM+ Variant pointed to by pRetVal.
void IUInvokeDispMethod(
    REFLECTCLASSBASEREF* pRefClassObj,
    OBJECTREF* pTarget,
    OBJECTREF* pName,
    DISPID *pMemberID,
    OBJECTREF* pArgs,
    OBJECTREF* pModifiers,
    OBJECTREF* pNamedArgs,
    OBJECTREF* pRetVal,
    LCID lcid,
    WORD flags,
    BOOL bIgnoreReturn,
    BOOL bIgnoreCase);

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
// Class Factory helpers

//--------------------------------------------------------------------------
// GetComClassFromProgID used by reflection class to setup a Class based on ProgID
void GetComClassFromProgID(STRINGREF srefProgID, STRINGREF srefServer, OBJECTREF* pRef);

//--------------------------------------------------------------------------
// GetComClassFromCLSID used by reflection class to setup a Class based on CLSID
void GetComClassFromCLSID(REFCLSID clsid, STRINGREF srefServer, OBJECTREF* pRef);

//-------------------------------------------------------------
// check if a ComClassFactory/WinRTClassFactory has been setup for this class
// if not set one up
ClassFactoryBase *GetComClassFactory(MethodTable* pClassMT);
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION


// logging APIs

#ifdef _DEBUG

VOID LogInterop(__in_z LPCSTR szMsg);
VOID LogInterop(__in_z LPCWSTR szMsg);

VOID LogInteropLeak(IUnkEntry * pEntry);
VOID LogInteropLeak(IUnknown* pItf);
VOID LogInteropQI(IUnknown* pItf, REFIID riid, HRESULT hr, __in_z LPCSTR szMsg);
VOID LogInteropAddRef(IUnknown* pItf, ULONG cbRef, __in_z LPCSTR szMsg);
VOID LogInteropRelease(IUnknown* pItf, ULONG cbRef, __in_z LPCSTR szMsg);

VOID LogRCWCreate(RCW* pWrap, IUnknown* pUnk);
VOID LogRCWMinorCleanup(RCW* pWrap);
VOID LogRCWDestroy(RCW* pWrap);

#else

#define LogInterop(x)
#define LogInteropLeak(x)
#define LogInteropQI(x, y, z, w)
#define LogInteropAddRef(x, y, z)
#define LogInteropRelease(x, y, z)
#define LogRCWCreate(x, y)
#define LogRCWMinorCleanup(x)
#define LogRCWDestroy(x)

#endif

//--------------------------------------------------------------------------------
// Ensure COM is started up.
HRESULT EnsureComStartedNoThrow(BOOL fCoInitCurrentThread = TRUE);
VOID EnsureComStarted(BOOL fCoInitCurrentThread = TRUE);

//--------------------------------------------------------------------------------
// check if the class is OR extends a COM Imported class
BOOL ExtendsComImport(MethodTable* pMT);

//--------------------------------------------------------------------------------
// Gets the CLSID from the specified Prog ID.
HRESULT GetCLSIDFromProgID(__in_z WCHAR *strProgId, GUID *pGuid);

//--------------------------------------------------------------------------------
// Check if the pUnk implements IProvideClassInfo and try to figure
// out the class from there
MethodTable* GetClassFromIProvideClassInfo(IUnknown* pUnk);

//--------------------------------------------------------------------------------
// Try to load a WinRT type.
TypeHandle LoadWinRTType(SString* ssTypeName, BOOL bThrowIfNotFound, ICLRPrivBinder* loadBinder = nullptr);

//--------------------------------------------------------------------------------
// Try to get the class from IInspectable.
TypeHandle GetClassFromIInspectable(IUnknown* pUnk, bool *pfSupportsIInspectable, bool *pfSupportsIReference, bool *pfSupportsIReferenceArray);

//--------------------------------------------------------------------------------
// Build a WinRT URI for a given raw URI
ABI::Windows::Foundation::IUriRuntimeClass *CreateWinRTUri(LPCWSTR wszUri, INT32 cchUri);

// Generates GUIDs for parameterized WinRT types.
class WinRTGuidGenerator
{
    class MetaDataLocator : public IRoMetaDataLocator
    {
        // IRoMetaDataLocator implementation:
        STDMETHOD(Locate)(PCWSTR nameElement, IRoSimpleMetaDataBuilder &metaDataDestination) const;

        // helper methods:
        static HRESULT LocateTypeWithDefaultInterface(MethodTable *pMT, LPCWSTR pszName, IRoSimpleMetaDataBuilder &metaDataDestination);
        static HRESULT LocateStructure(MethodTable *pMT, LPCWSTR pszName, IRoSimpleMetaDataBuilder &metaDataDestination);
        static HRESULT LocateRedirectedType(MethodTable *pMT, IRoSimpleMetaDataBuilder &metaDataDestination);
    };

    static void PopulateNames(MethodTable *pMT, SArray<BYTE> &namesBuf, PCWSTR* &pszNames, COUNT_T &cNames);
    static void PopulateNamesAppendNamePointers(MethodTable *pMT, SArray<BYTE> &namesBuf, PCWSTR* &pszNames, COUNT_T cNames);
    static void PopulateNamesAppendTypeName(MethodTable *pMT, SArray<BYTE> &namesBuf, COUNT_T &cNames);
public:
    //--------------------------------------------------------------------------
    // pGuid is filled with the constructed IID by the function.
    static void ComputeGuidForGenericType(MethodTable *pMT, GUID *pGuid);
};  // class WinRTGuidGenerator

IUnknown* MarshalObjectToInterface(OBJECTREF* ppObject, MethodTable* pItfMT, MethodTable* pClassMT, DWORD dwFlags);
void UnmarshalObjectFromInterface(OBJECTREF *ppObjectDest, IUnknown **ppUnkSrc, MethodTable *pItfMT, MethodTable *pClassMT, DWORD dwFlags);

#define DEFINE_ASM_QUAL_TYPE_NAME(varname, typename, asmname, version, publickeytoken)          static const char varname##[] = { typename##", "##asmname##", Culture=neutral, PublicKeyToken="##publickeytoken##", Version="##version };

class ICOMInterfaceMarshalerCallback;
void GetNativeWinRTFactoryObject(MethodTable *pMT, Thread *pThread, MethodTable *pFactoryIntfMT, BOOL bNeedUniqueRCW, ICOMInterfaceMarshalerCallback *pCallback, OBJECTREF *prefFactory);

#else // FEATURE_COMINTEROP
inline HRESULT EnsureComStartedNoThrow()
{
    LIMITED_METHOD_CONTRACT;
    
    return S_OK;
}

inline VOID EnsureComStarted()
{
    LIMITED_METHOD_CONTRACT;
}
    
#define LogInteropRelease(x, y, z)

#endif // FEATURE_COMINTEROP

#endif // _H_INTEROP_UTIL
