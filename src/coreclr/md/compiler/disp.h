// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// Disp.h
//

//
// Class factories are used by the pluming in COM to activate new objects.
// This module contains the class factory code to instantiate the debugger
// objects described in <cordb.h>.
//
//*****************************************************************************
#ifndef __Disp__h__
#define __Disp__h__


class Disp :
#ifndef FEATURE_METADATA_EMIT_PORTABLE_PDB
    public IMetaDataDispenserEx
#else
    public IMetaDataDispenserEx2
#endif
#ifdef FEATURE_METADATA_CUSTOM_DATA_SOURCE
    , IMetaDataDispenserCustom
#endif
{
public:
    Disp();
    virtual ~Disp();

    // *** IUnknown methods ***
    STDMETHODIMP    QueryInterface(REFIID riid, void** ppv);
    STDMETHODIMP_(ULONG) AddRef(void);
    STDMETHODIMP_(ULONG) Release(void);

    // *** IMetaDataDispenser methods ***
    STDMETHODIMP DefineScope(               // Return code.
        REFCLSID    rclsid,                 // [in] What version to create.
        DWORD       dwCreateFlags,          // [in] Flags on the create.
        REFIID      riid,                   // [in] The interface desired.
        IUnknown    **ppIUnk);              // [out] Return interface on success.

    STDMETHODIMP OpenScope(                 // Return code.
        LPCWSTR     szScope,                // [in] The scope to open.
        DWORD       dwOpenFlags,            // [in] Open mode flags.
        REFIID      riid,                   // [in] The interface desired.
        IUnknown    **ppIUnk);              // [out] Return interface on success.

    STDMETHODIMP OpenScopeOnMemory(         // Return code.
        LPCVOID     pData,                  // [in] Location of scope data.
        ULONG       cbData,                 // [in] Size of the data pointed to by pData.
        DWORD       dwOpenFlags,            // [in] Open mode flags.
        REFIID      riid,                   // [in] The interface desired.
        IUnknown    **ppIUnk);              // [out] Return interface on success.

    // *** IMetaDataDispenserEx methods ***
    STDMETHODIMP SetOption(                 // Return code.
        REFGUID     optionid,               // [in] GUID for the option to be set.
        const VARIANT *pvalue);             // [in] Value to which the option is to be set.

    STDMETHODIMP GetOption(                 // Return code.
        REFGUID     optionid,               // [in] GUID for the option to be set.
        VARIANT *pvalue);                   // [out] Value to which the option is currently set.

    STDMETHODIMP OpenScopeOnITypeInfo(      // Return code.
        ITypeInfo   *pITI,                  // [in] ITypeInfo to open.
        DWORD       dwOpenFlags,            // [in] Open mode flags.
        REFIID      riid,                   // [in] The interface desired.
        IUnknown    **ppIUnk);              // [out] Return interface on success.

    STDMETHODIMP GetCORSystemDirectory(     // Return code.
         _Out_writes_ (cchBuffer) LPWSTR szBuffer,  // [out] Buffer for the directory name
         DWORD       cchBuffer,             // [in] Size of the buffer
         DWORD*      pchBuffer);            // [OUT] Number of characters returned

    STDMETHODIMP FindAssembly(              // S_OK or error
        LPCWSTR  szAppBase,                 // [IN] optional - can be NULL
        LPCWSTR  szPrivateBin,              // [IN] optional - can be NULL
        LPCWSTR  szGlobalBin,               // [IN] optional - can be NULL
        LPCWSTR  szAssemblyName,            // [IN] required - this is the assembly you are requesting
        LPCWSTR  szName,                    // [OUT] buffer - to hold name
        ULONG    cchName,                   // [IN] the name buffer's size
        ULONG    *pcName);                  // [OUT] the number of characters returned in the buffer

    STDMETHODIMP FindAssemblyModule(        // S_OK or error
        LPCWSTR  szAppBase,                 // [IN] optional - can be NULL
        LPCWSTR  szPrivateBin,              // [IN] optional - can be NULL
        LPCWSTR  szGlobalBin,               // [IN] optional - can be NULL
        LPCWSTR  szAssemblyName,            // [IN] required - this is the assembly you are requesting
        LPCWSTR  szModuleName,              // [IN] required - the name of the module
        _Out_writes_ (cchName)LPWSTR szName,// [OUT] buffer - to hold name
        ULONG    cchName,                   // [IN]  the name buffer's size
        ULONG    *pcName);                  // [OUT] the number of characters returned in the buffer

#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
    STDMETHODIMP DefinePortablePdbScope(    // Return code.
        REFCLSID    rclsid,                 // [in] What version to create.
        DWORD       dwCreateFlags,          // [in] Flags on the create.
        REFIID      riid,                   // [in] The interface desired.
        IUnknown** ppIUnk);                 // [out] Return interface on success.
#endif

#ifdef FEATURE_METADATA_CUSTOM_DATA_SOURCE
    // *** IMetaDataDispenserCustom methods ***
    STDMETHODIMP OpenScopeOnCustomDataSource(  // S_OK or error
        IMDCustomDataSource  *pCustomSource, // [in] The scope to open.
        DWORD                dwOpenFlags,    // [in] Open mode flags.
        REFIID               riid,           // [in] The interface desired.
        IUnknown             **ppIUnk);      // [out] Return interface on success.
#endif

    // Class factory hook-up.
    static HRESULT CreateObject(REFIID riid, void **ppUnk);

private:
    HRESULT OpenRawScope(                   // Return code.
        LPCWSTR     szScope,                // [in] The scope to open.
        DWORD       dwOpenFlags,            // [in] Open mode flags.
        REFIID      riid,                   // [in] The interface desired.
        IUnknown    **ppIUnk);              // [out] Return interface on success.

    HRESULT OpenRawScopeOnMemory(           // Return code.
        LPCVOID     pData,                  // [in] Location of scope data.
        ULONG       cbData,                 // [in] Size of the data pointed to by pData.
        DWORD       dwOpenFlags,            // [in] Open mode flags.
        REFIID      riid,                   // [in] The interface desired.
        IUnknown    **ppIUnk);              // [out] Return interface on success.

#ifdef FEATURE_METADATA_CUSTOM_DATA_SOURCE
    HRESULT OpenRawScopeOnCustomDataSource( // Return code.
        IMDCustomDataSource*  pDataSource,  // [in] scope data.
        DWORD       dwOpenFlags,            // [in] Open mode flags.
        REFIID      riid,                   // [in] The interface desired.
        IUnknown    **ppIUnk);              // [out] Return interface on success.
#endif


private:
    LONG        m_cRef;                 // Ref count
    OptionValue m_OptionValue;          // values can be set by using SetOption
};

#endif // __Disp__h__
