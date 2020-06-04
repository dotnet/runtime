// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// =======================================================================================================
// Defines the surface area between md\WinMD and md\everything-else.
//
// md\WinMD contains adapter importers that wrap RegMeta and MDInternalRO to make .winmd files look like
// regular .NET assemblies.
// =======================================================================================================

#ifndef __WINMDINTERFACES_H__
#define __WINMDINTERFACES_H__

#include "metamodel.h"


//-----------------------------------------------------------------------------------------------------
// A common interface unifying RegMeta and MDInternalRO, giving the adapter a common interface to
// access the raw metadata.
//-----------------------------------------------------------------------------------------------------

// {4F8EE8A3-24F8-4241-BC75-C8CAEC0255B5}
EXTERN_GUID(IID_IMDCommon, 0x4f8ee8a3, 0x24f8, 0x4241, 0xbc, 0x75, 0xc8, 0xca, 0xec, 0x2, 0x55, 0xb5);

#undef  INTERFACE
#define INTERFACE IID_IMDCommon
DECLARE_INTERFACE_(IMDCommon, IUnknown)
{
    STDMETHOD_(IMetaModelCommon*, GetMetaModelCommon)() PURE;
    STDMETHOD_(IMetaModelCommonRO*, GetMetaModelCommonRO)() PURE;
    STDMETHOD(GetVersionString)(LPCSTR *pszVersionString) PURE;
};


//-----------------------------------------------------------------------------------------------------
// Returns:
//    S_OK:    if WinMD adapter should be used.
//    S_FALSE: if not
//-----------------------------------------------------------------------------------------------------
HRESULT CheckIfWinMDAdapterNeeded(IMDCommon *pRawMDCommon);



//-----------------------------------------------------------------------------------------------------
// Factory method that creates an WinMD adapter that implements:
//
//     IMetaDataImport2
//     IMetaDataAssemblyImport
//     IMetaDataValidate
//     IMarshal
//     IMDCommon (subset)
//
// IMDCommon is included as a concession to the fact that certain IMetaDataEmit apis have
// an (apparently undocumented) dependency on their importer arguments supporting this.
//
// You must provide a regular MD importer that implements:
//
//     IMDCommon
//     IMetaDataImport2
//     IMetaDataAssemblyImport
//     IMetaDataValidate
//
// The underlying metadata file must follow these restrictions:
//
//     - Have an existing assemblyRef to "mscorlib"
//
//-----------------------------------------------------------------------------------------------------
HRESULT CreateWinMDImport(IMDCommon * pRawMDCommon, REFIID riid, /*[out]*/ void **ppWinMDImport);


//-----------------------------------------------------------------------------------------------------
// Factory method that creates an WinMD adapter that implements IMDInternalImport.
// You must provide a regular MD importer that implements:
//
//     IMDCommon
//     IMDInternalImport
//
// The underlying metadata file must follow these restrictions:
//
//     - Have an existing assemblyRef to "mscorlib"
//
//-----------------------------------------------------------------------------------------------------
#ifdef FEATURE_METADATA_INTERNAL_APIS
HRESULT CreateWinMDInternalImportRO(IMDCommon * pRawMDCommon, REFIID riid, /*[out]*/ void **ppWinMDInternalImport);

#endif // FEATURE_METADATA_INTERNAL_APIS
//-----------------------------------------------------------------------------------------------------
// S_OK if pUnknown is really a WinMD wrapper. This is just a polite way of asking "is it bad to
//   to static cast pUnknown to RegMeta/MDInternalRO."
//-----------------------------------------------------------------------------------------------------
HRESULT CheckIfImportingWinMD(IUnknown *pUnknown);


//-----------------------------------------------------------------------------------------------------
// E_NOTIMPL if pUnknown is really a WinMD wrapper.
//-----------------------------------------------------------------------------------------------------
HRESULT VerifyNotWinMDHelper(IUnknown *pUnknown
#ifdef _DEBUG
                            ,LPCSTR    assertMsg
                            ,LPCSTR    file
                            ,int       line
#endif //_DEBUG
                            );
#define VerifyNotWinMD(pUnknown, assertMsg) S_OK


#endif //__WINMDINTERFACES_H__



