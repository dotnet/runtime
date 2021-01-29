// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-SYMBOL.CPP
//

#include <mono/utils/atomic.h>

#include "corerror.h"
#include "mdlog.h"
#include "posterror.h"
#include "rwutil.h"
#include "sstring.h"
#include "stdafx.h"
#include "stgio.h"
#include "switches.h"

#include "importhelper.h"
#include "mdinternalrw.h"

#include <metamodelrw.h>

#include <cordb-assembly.h>
#include <cordb-frame.h>
#include <cordb-function.h>
#include <cordb-process.h>
#include <cordb-stepper.h>
#include <cordb-symbol.h>
#include <cordb-thread.h>
#include <cordb.h>

using namespace std;

RegMeta::RegMeta(CordbAssembly *cordbAssembly, CordbModule *cordbModule) {
  module_id = -1;
  pCordbAssembly = cordbAssembly;
  this->cordbModule = cordbModule;
  parameters = g_hash_table_new(NULL, NULL);
  token_id = 0;

  m_pStgdb = new CLiteWeightStgdbRW();
  ULONG32 pcchName = 0;
  cordbModule->GetName(0, &pcchName, NULL);

  wchar_t *full_path;
  full_path = (wchar_t *)malloc(sizeof(wchar_t) * pcchName);
  cordbModule->GetName(pcchName, &pcchName, full_path);

  m_pStgdb->OpenForRead(full_path, NULL, 0, 0);
}

HRESULT RegMeta::EnumGenericParams(
    HCORENUM *phEnum, // [IN|OUT] Pointer to the enum.
    mdToken tkOwner,  // [IN] TypeDef or MethodDef whose generic parameters are
                      // requested
    mdGenericParam rTokens[], // [OUT] Put GenericParams here.
    ULONG cMaxTokens,         // [IN] Max GenericParams to put.
    ULONG *pcTokens) {
  HRESULT hr = S_OK;

  HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **>(phEnum);
  ULONG ridStart;
  ULONG ridEnd;
  HENUMInternal *pEnum;
  GenericParamRec *pRec;
  ULONG index;
  CMiniMdRW *pMiniMd = NULL;

  pMiniMd = &(m_pStgdb->m_MiniMd);

  // See if this version of the metadata can do Generics
  if (!pMiniMd->SupportsGenerics()) {
    if (pcTokens)
      *pcTokens = 0;
    hr = S_FALSE;
    goto ErrExit;
  }

  _ASSERTE(TypeFromToken(tkOwner) == mdtTypeDef ||
           TypeFromToken(tkOwner) == mdtMethodDef);

  if (*ppmdEnum == 0) {
    // instantiating a new ENUM

    //@todo GENERICS: review this. Are we expecting a sorted table or not?
    if (pMiniMd->IsSorted(TBL_GenericParam)) {
      if (TypeFromToken(tkOwner) == mdtTypeDef) {
        IfFailGo(pMiniMd->getGenericParamsForTypeDef(RidFromToken(tkOwner),
                                                     &ridEnd, &ridStart));
      } else {
        IfFailGo(pMiniMd->getGenericParamsForMethodDef(RidFromToken(tkOwner),
                                                       &ridEnd, &ridStart));
      }

      IfFailGo(HENUMInternal::CreateSimpleEnum(mdtGenericParam, ridStart,
                                               ridEnd, &pEnum));
    } else {
      // table is not sorted so we have to create dynamic array
      // create the dynamic enumerator
      //
      ridStart = 1;
      ridEnd = pMiniMd->getCountGenericParams() + 1;

      IfFailGo(HENUMInternal::CreateDynamicArrayEnum(mdtGenericParam, &pEnum));

      for (index = ridStart; index < ridEnd; index++) {
        IfFailGo(pMiniMd->GetGenericParamRecord(index, &pRec));
        if (tkOwner == pMiniMd->getOwnerOfGenericParam(pRec)) {
          IfFailGo(HENUMInternal::AddElementToEnum(
              pEnum, TokenFromRid(index, mdtGenericParam)));
        }
      }
    }

    // set the output parameter
    *ppmdEnum = pEnum;
  } else {
    pEnum = *ppmdEnum;
  }

  // fill the output token buffer
  hr = HENUMInternal::EnumWithCount(pEnum, cMaxTokens, rTokens, pcTokens);

ErrExit:
  HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);

  return hr;
}

HRESULT RegMeta::GetGenericParamProps( // S_OK or error.
    mdGenericParam rd,                 // [IN] The type parameter
    ULONG *pulSequence,                // [OUT] Parameter sequence number
    DWORD *pdwAttr,   // [OUT] Type parameter flags (for future use)
    mdToken *ptOwner, // [OUT] The owner (TypeDef or MethodDef)
    DWORD *reserved,  // [OUT] The kind (TypeDef/Ref/Spec, for future use)
    __out_ecount_opt(cchName) LPWSTR szName, // [OUT] The name
    ULONG cchName,                           // [IN] Size of name buffer
    ULONG *pchName)                          // [OUT] Actual size of name
{
  HRESULT hr = NOERROR;

  BEGIN_ENTRYPOINT_NOTHROW;

  GenericParamRec *pGenericParamRec;
  CMiniMdRW *pMiniMd = NULL;
  RID ridRD = RidFromToken(rd);

  pMiniMd = &(m_pStgdb->m_MiniMd);

  // See if this version of the metadata can do Generics
  if (!pMiniMd->SupportsGenerics())
    IfFailGo(CLDB_E_INCOMPATIBLE);

  if ((TypeFromToken(rd) == mdtGenericParam) && (ridRD != 0)) {
    IfFailGo(
        pMiniMd->GetGenericParamRecord(RidFromToken(rd), &pGenericParamRec));

    if (pulSequence)
      *pulSequence = pMiniMd->getNumberOfGenericParam(pGenericParamRec);
    if (pdwAttr)
      *pdwAttr = pMiniMd->getFlagsOfGenericParam(pGenericParamRec);
    if (ptOwner)
      *ptOwner = pMiniMd->getOwnerOfGenericParam(pGenericParamRec);
    // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not
    // rewritten with S_OK
    if (pchName || szName)
      IfFailGo(pMiniMd->getNameOfGenericParam(pGenericParamRec, szName, cchName,
                                              pchName));
  } else
    hr = META_E_BAD_INPUT_PARAMETER;

ErrExit:

  return hr;
} // [OUT] Put size of name (wide chars) here.

HRESULT RegMeta::GetMethodSpecProps(
    mdMethodSpec mi,             // [IN] The method instantiation
    mdToken *tkParent,           // [OUT] MethodDef or MemberRef
    PCCOR_SIGNATURE *ppvSigBlob, // [OUT] point to the blob value of meta data
    ULONG *pcbSigBlob) {

  HRESULT hr = NOERROR;

  MethodSpecRec *pMethodSpecRec;
  CMiniMdRW *pMiniMd = NULL;

  pMiniMd = &(m_pStgdb->m_MiniMd);

  // See if this version of the metadata can do Generics
  if (!pMiniMd->SupportsGenerics())
    IfFailGo(CLDB_E_INCOMPATIBLE);

  _ASSERTE(TypeFromToken(mi) == mdtMethodSpec && RidFromToken(mi));

  IfFailGo(pMiniMd->GetMethodSpecRecord(RidFromToken(mi), &pMethodSpecRec));

  if (tkParent)
    *tkParent = pMiniMd->getMethodOfMethodSpec(pMethodSpecRec);

  if (ppvSigBlob || pcbSigBlob) {
    // caller wants signature information
    PCCOR_SIGNATURE pvSigTmp;
    ULONG cbSig;
    IfFailGo(pMiniMd->getInstantiationOfMethodSpec(pMethodSpecRec, &pvSigTmp,
                                                   &cbSig));
    if (ppvSigBlob)
      *ppvSigBlob = pvSigTmp;
    if (pcbSigBlob)
      *pcbSigBlob = cbSig;
  }

ErrExit:

  return hr;
} // [OUT] actual size of signature blob

HRESULT RegMeta::EnumGenericParamConstraints(
    HCORENUM *phEnum,  // [IN|OUT] Pointer to the enum.
    mdGenericParam tk, // [IN] GenericParam whose constraints are requested
    mdGenericParamConstraint
        rGenericParamConstraints[], // [OUT] Put GenericParamConstraints here.
    ULONG cMax,                     // [IN] Max GenericParamConstraints to put.
    ULONG *pcGenericParamConstraints) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumGenericParamConstraints - NOT IMPLEMENTED\n"));
  return S_OK;
} // [OUT] Put # put here.

HRESULT RegMeta::GetGenericParamConstraintProps( // S_OK or error.
    mdGenericParamConstraint gpc,                // [IN] GenericParamConstraint
    mdGenericParam *ptGenericParam, // [OUT] GenericParam that is constrained
    mdToken *ptkConstraintType) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - GetGenericParamConstraintProps - NOT IMPLEMENTED\n"));
  return S_OK;
} // [OUT] TypeDef/Ref/Spec constraint

HRESULT RegMeta::GetPEKind( // S_OK or error.
    DWORD *pdwPEKind,       // [OUT] The kind of PE (0 - not a PE)
    DWORD *pdwMachine) {
  HRESULT hr = NOERROR;
  MAPPINGTYPE mt = MTYPE_NOMAPPING;

  if (m_pStgdb->m_pStgIO != NULL)
    mt = m_pStgdb->m_pStgIO->GetMemoryMappedType();

  hr = m_pStgdb->GetPEKind(mt, pdwPEKind, pdwMachine);
  return hr;
} // [OUT] Machine as defined in NT header

HRESULT RegMeta::GetVersionString( // S_OK or error.
    _Out_writes_to_opt_(ccBufSize, *pccBufSize)
        LPWSTR pwzBuf, // [OUT] Put version string here.
    DWORD cchBufSize,  // [IN] size of the buffer, in wide chars
    DWORD *pchBufSize) {
  HRESULT hr = NOERROR;

  DWORD cch;   // Length of WideChar string.
  LPCSTR pVer; // Pointer to version string.

  if (m_pStgdb->m_pvMd != NULL) {
    // For convenience, get a pointer to the version string.
    // @todo: get from alternate locations when there is no STOREAGESIGNATURE.
    pVer = reinterpret_cast<const char *>(
        reinterpret_cast<const STORAGESIGNATURE *>(m_pStgdb->m_pvMd)->pVersion);
    // Attempt to convert into caller's buffer.
    cch = WszMultiByteToWideChar(CP_UTF8, 0, pVer, -1, pwzBuf, cchBufSize);
    // Did the string fit?
    if (cch == 0) {
      // No, didn't fit.  Find out space required.
      cch = WszMultiByteToWideChar(CP_UTF8, 0, pVer, -1, pwzBuf, 0);
      // NUL terminate string.
      if (cchBufSize > 0)
        pwzBuf[cchBufSize - 1] = W('\0');
      // Truncation return code.
      hr = CLDB_S_TRUNCATION;
    }
  } else {
    // No string.
    if (cchBufSize > 0)
      *pwzBuf = W('\0');
    cch = 0;
  }

  if (pchBufSize)
    *pchBufSize = cch;

  return hr;
} // [OUT] Size of the version string, wide chars, including terminating nul.

HRESULT RegMeta::EnumMethodSpecs(
    HCORENUM *phEnum, // [IN|OUT] Pointer to the enum.
    mdToken tk, // [IN] MethodDef or MemberRef whose MethodSpecs are requested
    mdMethodSpec rMethodSpecs[], // [OUT] Put MethodSpecs here.
    ULONG cMax,                  // [IN] Max tokens to put.
    ULONG *pcMethodSpecs) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumMethodSpecs - NOT IMPLEMENTED\n"));
  return S_OK;
} // [OUT] Put actual count here.

HRESULT RegMeta::GetAssemblyProps( // S_OK or error.
    mdAssembly mda, // [IN] The Assembly for which to get the properties.
    const void **ppbPublicKey, // [OUT] Pointer to the public key.
    ULONG *pcbPublicKey,       // [OUT] Count of bytes in the public key.
    ULONG *pulHashAlgId,       // [OUT] Hash Algorithm.
    _Out_writes_to_opt_(cchName, *pchName) LPWSTR
        szName,     // [OUT] MdbgProtBuffer to fill with assembly's simply name.
    ULONG cchName,  // [IN] Size of buffer in wide chars.
    ULONG *pchName, // [OUT] Actual # of wide chars in name.
    ASSEMBLYMETADATA *pMetaData, // [OUT] Assembly MetaData.
    DWORD *pdwAssemblyFlags) {
  HRESULT hr = S_OK;

  BEGIN_ENTRYPOINT_NOTHROW;

  AssemblyRec *pRecord;
  CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

  _ASSERTE(TypeFromToken(mda) == mdtAssembly && RidFromToken(mda));
  IfFailGo(pMiniMd->GetAssemblyRecord(RidFromToken(mda), &pRecord));

  if (ppbPublicKey != NULL) {
    IfFailGo(pMiniMd->getPublicKeyOfAssembly(
        pRecord, (const BYTE **)ppbPublicKey, pcbPublicKey));
  }
  if (pulHashAlgId)
    *pulHashAlgId = pMiniMd->getHashAlgIdOfAssembly(pRecord);
  if (pMetaData) {
    pMetaData->usMajorVersion = pMiniMd->getMajorVersionOfAssembly(pRecord);
    pMetaData->usMinorVersion = pMiniMd->getMinorVersionOfAssembly(pRecord);
    pMetaData->usBuildNumber = pMiniMd->getBuildNumberOfAssembly(pRecord);
    pMetaData->usRevisionNumber = pMiniMd->getRevisionNumberOfAssembly(pRecord);
    IfFailGo(pMiniMd->getLocaleOfAssembly(pRecord, pMetaData->szLocale,
                                          pMetaData->cbLocale,
                                          &pMetaData->cbLocale));
    pMetaData->ulProcessor = 0;
    pMetaData->ulOS = 0;
  }
  if (pdwAssemblyFlags) {
    *pdwAssemblyFlags = pMiniMd->getFlagsOfAssembly(pRecord);

    // Turn on the afPublicKey if PublicKey blob is not empty
    DWORD cbPublicKey;
    const BYTE *pbPublicKey;
    IfFailGo(
        pMiniMd->getPublicKeyOfAssembly(pRecord, &pbPublicKey, &cbPublicKey));
    if (cbPublicKey != 0)
      *pdwAssemblyFlags |= afPublicKey;
  }
  // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten
  // with S_OK
  if (szName || pchName)
    IfFailGo(pMiniMd->getNameOfAssembly(pRecord, szName, cchName, pchName));
ErrExit:

  return hr;
} // [OUT] Flags.

HRESULT RegMeta::GetAssemblyRefProps( // S_OK or error.
    mdAssemblyRef mdar, // [IN] The AssemblyRef for which to get the properties.
    const void *
        *ppbPublicKeyOrToken, // [OUT] Pointer to the public key or token.
    ULONG *
        pcbPublicKeyOrToken, // [OUT] Count of bytes in the public key or token.
    _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR szName,           // [OUT] MdbgProtBuffer to fill with name.
    ULONG cchName,               // [IN] Size of buffer in wide chars.
    ULONG *pchName,              // [OUT] Actual # of wide chars in name.
    ASSEMBLYMETADATA *pMetaData, // [OUT] Assembly MetaData.
    const void **ppbHashValue,   // [OUT] Hash blob.
    ULONG *pcbHashValue,         // [OUT] Count of bytes in the hash blob.
    DWORD *pdwAssemblyRefFlags) {
  HRESULT hr = S_OK;

  BEGIN_ENTRYPOINT_NOTHROW;

  AssemblyRefRec *pRecord;
  CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

  _ASSERTE(TypeFromToken(mdar) == mdtAssemblyRef && RidFromToken(mdar));
  IfFailGo(pMiniMd->GetAssemblyRefRecord(RidFromToken(mdar), &pRecord));

  if (ppbPublicKeyOrToken != NULL) {
    IfFailGo(pMiniMd->getPublicKeyOrTokenOfAssemblyRef(
        pRecord, (const BYTE **)ppbPublicKeyOrToken, pcbPublicKeyOrToken));
  }
  if (pMetaData) {
    pMetaData->usMajorVersion = pMiniMd->getMajorVersionOfAssemblyRef(pRecord);
    pMetaData->usMinorVersion = pMiniMd->getMinorVersionOfAssemblyRef(pRecord);
    pMetaData->usBuildNumber = pMiniMd->getBuildNumberOfAssemblyRef(pRecord);
    pMetaData->usRevisionNumber =
        pMiniMd->getRevisionNumberOfAssemblyRef(pRecord);
    IfFailGo(pMiniMd->getLocaleOfAssemblyRef(pRecord, pMetaData->szLocale,
                                             pMetaData->cbLocale,
                                             &pMetaData->cbLocale));
    pMetaData->ulProcessor = 0;
    pMetaData->ulOS = 0;
  }
  if (ppbHashValue != NULL) {
    IfFailGo(pMiniMd->getHashValueOfAssemblyRef(
        pRecord, (const BYTE **)ppbHashValue, pcbHashValue));
  }
  if (pdwAssemblyRefFlags)
    *pdwAssemblyRefFlags = pMiniMd->getFlagsOfAssemblyRef(pRecord);
  // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten
  // with S_OK
  if (szName || pchName)
    IfFailGo(pMiniMd->getNameOfAssemblyRef(pRecord, szName, cchName, pchName));
ErrExit:

  return hr;
} // [OUT] Flags.

HRESULT RegMeta::GetFileProps( // S_OK or error.
    mdFile mdf,                // [IN] The File for which to get the properties.
    _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR szName,         // [OUT] MdbgProtBuffer to fill with name.
    ULONG cchName,             // [IN] Size of buffer in wide chars.
    ULONG *pchName,            // [OUT] Actual # of wide chars in name.
    const void **ppbHashValue, // [OUT] Pointer to the Hash Value Blob.
    ULONG *pcbHashValue,       // [OUT] Count of bytes in the Hash Value Blob.
    DWORD *pdwFileFlags) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - GetFileProps - NOT IMPLEMENTED\n"));
  return S_OK;
} // [OUT] Flags.

HRESULT RegMeta::GetExportedTypeProps( // S_OK or error.
    mdExportedType
        mdct, // [IN] The ExportedType for which to get the properties.
    _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR szName, // [OUT] MdbgProtBuffer to fill with name.
    ULONG cchName,     // [IN] Size of buffer in wide chars.
    ULONG *pchName,    // [OUT] Actual # of wide chars in name.
    mdToken
        *ptkImplementation, // [OUT] mdFile or mdAssemblyRef or mdExportedType.
    mdTypeDef *ptkTypeDef,  // [OUT] TypeDef token within the file.
    DWORD *pdwExportedTypeFlags) {
  HRESULT hr = S_OK; // A result.

  ExportedTypeRec *pRecord; // The exported type.
  CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
  int bTruncation = 0; // Was there name truncation?

  _ASSERTE(TypeFromToken(mdct) == mdtExportedType && RidFromToken(mdct));
  IfFailGo(pMiniMd->GetExportedTypeRecord(RidFromToken(mdct), &pRecord));

  if (szName || pchName) {
    LPCSTR szTypeNamespace;
    LPCSTR szTypeName;

    IfFailGo(
        pMiniMd->getTypeNamespaceOfExportedType(pRecord, &szTypeNamespace));
    PREFIX_ASSUME(szTypeNamespace != NULL);
    MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzTypeNamespace, szTypeNamespace);
    IfNullGo(wzTypeNamespace);

    IfFailGo(pMiniMd->getTypeNameOfExportedType(pRecord, &szTypeName));
    _ASSERTE(*szTypeName);
    MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzTypeName, szTypeName);
    IfNullGo(wzTypeName);

    if (szName)
      bTruncation =
          !(ns::MakePath(szName, cchName, wzTypeNamespace, wzTypeName));
    if (pchName) {
      if (bTruncation || !szName)
        *pchName = ns::GetFullLength(wzTypeNamespace, wzTypeName);
      else
        *pchName = (ULONG)(wcslen(szName) + 1);
    }
  }
  if (ptkImplementation)
    *ptkImplementation = pMiniMd->getImplementationOfExportedType(pRecord);
  if (ptkTypeDef)
    *ptkTypeDef = pMiniMd->getTypeDefIdOfExportedType(pRecord);
  if (pdwExportedTypeFlags)
    *pdwExportedTypeFlags = pMiniMd->getFlagsOfExportedType(pRecord);

  if (bTruncation && hr == S_OK) {
    if ((szName != NULL) && (cchName > 0)) {
      // null-terminate the truncated output string
      szName[cchName - 1] = W('\0');
    }
    hr = CLDB_S_TRUNCATION;
  }

ErrExit:

  return hr;
} // [OUT] Flags.

HRESULT RegMeta::GetManifestResourceProps( // S_OK or error.
    mdManifestResource
        mdmr, // [IN] The ManifestResource for which to get the properties.
    _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR szName,          // [OUT] MdbgProtBuffer to fill with name.
    ULONG cchName,              // [IN] Size of buffer in wide chars.
    ULONG *pchName,             // [OUT] Actual # of wide chars in name.
    mdToken *ptkImplementation, // [OUT] mdFile or mdAssemblyRef that provides
                                // the ManifestResource.
    DWORD *pdwOffset, // [OUT] Offset to the beginning of the resource within
                      // the file.
    DWORD *pdwResourceFlags) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - GetManifestResourceProps - NOT IMPLEMENTED\n"));
  return S_OK;
} // [OUT] Flags.

HRESULT RegMeta::EnumAssemblyRefs( // S_OK or error
    HCORENUM *phEnum,              // [IN|OUT] Pointer to the enum.
    mdAssemblyRef rAssemblyRefs[], // [OUT] Put AssemblyRefs here.
    ULONG cMax,                    // [IN] Max AssemblyRefs to put.
    ULONG *pcTokens) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumAssemblyRefs - NOT IMPLEMENTED\n"));
  return S_OK;
} // [OUT] Put # put here.

HRESULT RegMeta::EnumFiles( // S_OK or error
    HCORENUM *phEnum,       // [IN|OUT] Pointer to the enum.
    mdFile rFiles[],        // [OUT] Put Files here.
    ULONG cMax,             // [IN] Max Files to put.
    ULONG *pcTokens) {
  HRESULT hr = NOERROR;

  HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **>(phEnum);
  HENUMInternal *pEnum;

  if (*ppmdEnum == 0) {
    // instantiate a new ENUM.
    CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

    // create the enumerator.
    IfFailGo(HENUMInternal::CreateSimpleEnum(
        mdtFile, 1, pMiniMd->getCountFiles() + 1, &pEnum));

    // set the output parameter.
    *ppmdEnum = pEnum;
  } else
    pEnum = *ppmdEnum;

  // we can only fill the minimum of what the caller asked for or what we have
  // left.
  IfFailGo(HENUMInternal::EnumWithCount(pEnum, cMax, rFiles, pcTokens));
ErrExit:
  HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);

  return hr;
} // [OUT] Put # put here.

HRESULT RegMeta::EnumExportedTypes(  // S_OK or error
    HCORENUM *phEnum,                // [IN|OUT] Pointer to the enum.
    mdExportedType rExportedTypes[], // [OUT] Put ExportedTypes here.
    ULONG cMax,                      // [IN] Max ExportedTypes to put.
    ULONG *pcTokens) {
  HRESULT hr = NOERROR;

  BEGIN_ENTRYPOINT_NOTHROW;

  HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **>(phEnum);
  HENUMInternal *pEnum;

  if (*ppmdEnum == 0) {
    // instantiate a new ENUM.
    CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

    if (pMiniMd->HasDelete()) {
      IfFailGo(HENUMInternal::CreateDynamicArrayEnum(mdtExportedType, &pEnum));

      // add all Types to the dynamic array if name is not _Delete
      for (ULONG index = 1; index <= pMiniMd->getCountExportedTypes();
           index++) {
        ExportedTypeRec *pRec;
        IfFailGo(pMiniMd->GetExportedTypeRecord(index, &pRec));
        LPCSTR szTypeName;
        IfFailGo(pMiniMd->getTypeNameOfExportedType(pRec, &szTypeName));
        if (IsDeletedName(szTypeName)) {
          continue;
        }
        IfFailGo(HENUMInternal::AddElementToEnum(
            pEnum, TokenFromRid(index, mdtExportedType)));
      }
    } else {
      // create the enumerator.
      IfFailGo(HENUMInternal::CreateSimpleEnum(
          mdtExportedType, 1, pMiniMd->getCountExportedTypes() + 1, &pEnum));
    }

    // set the output parameter.
    *ppmdEnum = pEnum;
  } else
    pEnum = *ppmdEnum;

  // we can only fill the minimum of what the caller asked for or what we have
  // left.
  IfFailGo(HENUMInternal::EnumWithCount(pEnum, cMax, rExportedTypes, pcTokens));
ErrExit:
  HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);

  return hr;
} // [OUT] Put # put here.

HRESULT RegMeta::EnumManifestResources( // S_OK or error
    HCORENUM *phEnum,                   // [IN|OUT] Pointer to the enum.
    mdManifestResource
        rManifestResources[], // [OUT] Put ManifestResources here.
    ULONG cMax,               // [IN] Max Resources to put.
    ULONG *pcTokens) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumManifestResources - NOT IMPLEMENTED\n"));
  return S_OK;
} // [OUT] Put # put here.

HRESULT RegMeta::FindExportedTypeByName( // S_OK or error
    LPCWSTR szName,                      // [IN] Name of the ExportedType.
    mdToken mdtExportedType, // [IN] ExportedType for the enclosing class.
    mdExportedType *ptkExportedType) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - FindExportedTypeByName - NOT IMPLEMENTED\n"));
  return S_OK;
} // [OUT] Put the ExportedType token here.

HRESULT RegMeta::FindManifestResourceByName( // S_OK or error
    LPCWSTR szName, // [IN] Name of the ManifestResource.
    mdManifestResource *ptkManifestResource) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - FindManifestResourceByName - NOT IMPLEMENTED\n"));
  return S_OK;
} // [OUT] Put the ManifestResource token here.

HRESULT RegMeta::FindAssembliesByName( // S_OK or error
    LPCWSTR szAppBase,                 // [IN] optional - can be NULL
    LPCWSTR szPrivateBin,              // [IN] optional - can be NULL
    LPCWSTR szAssemblyName, // [IN] required - this is the assembly you are
                            // requesting
    IUnknown *ppIUnk[],     // [OUT] put IMetaDataAssemblyImport pointers here
    ULONG cMax,             // [IN] The max number to put
    ULONG *pcAssemblies) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - FindAssembliesByName - NOT IMPLEMENTED\n"));
  return S_OK;
} // [OUT] The number of assemblies returned.

// IUnknown methods
HRESULT RegMeta::QueryInterface(REFIID riid, LPVOID *ppUnk) {
  HRESULT hr = S_OK;
  int fIsInterfaceRW = false;
  *ppUnk = 0;
  if (riid == IID_IUnknown) {
    *ppUnk = (IUnknown *)(IMetaDataImport2 *)this;
  } else if (riid == IID_IMDCommon) {
    *ppUnk = (IMDCommon *)this;
  } else if (riid == IID_IMetaDataImport) {
    *ppUnk = (IMetaDataImport2 *)this;
  } else if (riid == IID_IMetaDataImport2) {
    *ppUnk = (IMetaDataImport2 *)this;
  } else if (riid == IID_IMetaDataAssemblyImport) {
    *ppUnk = (IMetaDataAssemblyImport *)this;
  } else {
    IfFailGo(E_NOINTERFACE);
  }
ErrExit:
  return hr;
}

ULONG RegMeta::AddRef() { return S_OK; }

ULONG RegMeta::Release() { return S_OK; }

// IMetaDataImport functions
void RegMeta::CloseEnum(HCORENUM hEnum) {
  BEGIN_CLEANUP_ENTRYPOINT;
  // No need to lock this function.
  HENUMInternal *pmdEnum = reinterpret_cast<HENUMInternal *>(hEnum);

  if (pmdEnum == NULL)
    return;

  HENUMInternal::DestroyEnum(pmdEnum);
  END_CLEANUP_ENTRYPOINT;
}

HRESULT RegMeta::CountEnum(HCORENUM hEnum, ULONG *pulCount) {
  HENUMInternal *pmdEnum = reinterpret_cast<HENUMInternal *>(hEnum);
  HRESULT hr = S_OK;

  // No need to lock this function.

  _ASSERTE(pulCount);

  if (pmdEnum == NULL) {
    *pulCount = 0;
    goto ErrExit;
  }

  if (pmdEnum->m_tkKind == (TBL_MethodImpl << 24)) {
    // Number of tokens must always be a multiple of 2.
    _ASSERTE(!(pmdEnum->m_ulCount % 2));
    // There are two entries in the Enumerator for each MethodImpl.
    *pulCount = pmdEnum->m_ulCount / 2;
  } else
    *pulCount = pmdEnum->m_ulCount;
ErrExit:
  return hr;
}

HRESULT RegMeta::ResetEnum(HCORENUM hEnum, ULONG ulPos) {
  LOG((LF_CORDB, LL_INFO100000, "CordbSymbol - ResetEnum - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::EnumTypeDefs(HCORENUM *phEnum, mdTypeDef rTypeDefs[],
                              ULONG cMax, ULONG *pcTypeDefs) {
  HRESULT hr = S_OK;
  HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **>(phEnum);
  HENUMInternal *pEnum;
  if (*phEnum == NULL) {
    CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
    HENUMInternal::CreateDynamicArrayEnum(mdtTypeDef, &pEnum);
    for (ULONG index = 2; index <= pMiniMd->getCountTypeDefs(); index++) {
      TypeDefRec *pRec;
      pMiniMd->GetTypeDefRecord(index, &pRec);
      LPCSTR szTypeDefName;
      pMiniMd->getNameOfTypeDef(pRec, &szTypeDefName);
      if (IsDeletedName(szTypeDefName))
        continue;
      HENUMInternal::AddElementToEnum(pEnum, TokenFromRid(index, mdtTypeDef));
    }
    *ppmdEnum = pEnum;
  } else {
    pEnum = *ppmdEnum;
  }
  hr = HENUMInternal::EnumWithCount(pEnum, cMax, rTypeDefs, pcTypeDefs);
  return hr;
}

HRESULT RegMeta::EnumInterfaceImpls(HCORENUM *phEnum, mdTypeDef td,
                                    mdInterfaceImpl rImpls[], ULONG cMax,
                                    ULONG *pcImpls) {
  HRESULT hr = S_OK;

  HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **>(phEnum);
  ULONG ridStart;
  ULONG ridEnd;
  HENUMInternal *pEnum;
  InterfaceImplRec *pRec;
  ULONG index;

  _ASSERTE(TypeFromToken(td) == mdtTypeDef);

  if (*ppmdEnum == 0) {
    // instantiating a new ENUM
    CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
    if (pMiniMd->IsSorted(TBL_InterfaceImpl)) {
      IfFailGo(pMiniMd->getInterfaceImplsForTypeDef(RidFromToken(td), &ridEnd,
                                                    &ridStart));
      IfFailGo(HENUMInternal::CreateSimpleEnum(mdtInterfaceImpl, ridStart,
                                               ridEnd, &pEnum));
    } else {
      // table is not sorted so we have to create dynmaic array
      // create the dynamic enumerator
      //
      ridStart = 1;
      ridEnd = pMiniMd->getCountInterfaceImpls() + 1;

      IfFailGo(HENUMInternal::CreateDynamicArrayEnum(mdtInterfaceImpl, &pEnum));

      for (index = ridStart; index < ridEnd; index++) {
        IfFailGo(pMiniMd->GetInterfaceImplRecord(index, &pRec));
        if (td == pMiniMd->getClassOfInterfaceImpl(pRec)) {
          IfFailGo(HENUMInternal::AddElementToEnum(
              pEnum, TokenFromRid(index, mdtInterfaceImpl)));
        }
      }
    }

    // set the output parameter
    *ppmdEnum = pEnum;
  } else {
    pEnum = *ppmdEnum;
  }

  // fill the output token buffer
  hr = HENUMInternal::EnumWithCount(pEnum, cMax, rImpls, pcImpls);

ErrExit:
  HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);

  return hr;
}

HRESULT RegMeta::EnumTypeRefs(HCORENUM *phEnum, mdTypeRef rTypeRefs[],
                              ULONG cMax, ULONG *pcTypeRefs) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumTypeRefs - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::FindTypeDefByName( // S_OK or error.
    LPCWSTR wzTypeDef,              // [IN] Name of the Type.
    mdToken tkEnclosingClass,       // [IN] TypeDef/TypeRef for Enclosing class.
    mdTypeDef *ptd)                 // [OUT] Put the TypeDef token here.
{
  HRESULT hr = S_OK;
  if (wzTypeDef == NULL)
    IfFailGo(E_INVALIDARG);
  PREFIX_ASSUME(wzTypeDef != NULL);
  LPSTR szTypeDef;
  UTF8STR(wzTypeDef, szTypeDef);
  LPCSTR szNamespace;
  LPCSTR szName;

  _ASSERTE(ptd);
  _ASSERTE(TypeFromToken(tkEnclosingClass) == mdtTypeDef ||
           TypeFromToken(tkEnclosingClass) == mdtTypeRef ||
           IsNilToken(tkEnclosingClass));

  // initialize output parameter
  *ptd = mdTypeDefNil;

  ns::SplitInline(szTypeDef, szNamespace, szName);
  hr = ImportHelper::FindTypeDefByName(&(m_pStgdb->m_MiniMd), szNamespace,
                                       szName, tkEnclosingClass, ptd);
ErrExit:
  return hr;
}

HRESULT RegMeta::GetScopeProps( // S_OK or error.
    __out_ecount_part_opt(cchName, *pchName)
        LPWSTR szName, // [OUT] Put the name here.
    ULONG cchName,     // [IN] Size of name buffer in wide chars.
    ULONG *pchName,    // [OUT] Put size of name (wide chars) here.
    GUID *pmvid)       // [OUT, OPTIONAL] Put MVID here.
{
  HRESULT hr = S_OK;

  BEGIN_ENTRYPOINT_NOTHROW;

  CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
  ModuleRec *pModuleRec;

  // there is only one module record
  IfFailGo(pMiniMd->GetModuleRecord(1, &pModuleRec));

  if (pmvid != NULL) {
    IfFailGo(pMiniMd->getMvidOfModule(pModuleRec, pmvid));
  }
  // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten
  // with S_OK
  if (szName || pchName)
    IfFailGo(pMiniMd->getNameOfModule(pModuleRec, szName, cchName, pchName));
ErrExit:
  return hr;
}

HRESULT RegMeta::GetModuleFromScope( // S_OK.
    mdModule *pmd)                   // [OUT] Put mdModule token here.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - GetModuleFromScope - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::GetTypeDefProps( // S_OK or error.
    mdTypeDef td,                 // [IN] TypeDef token for inquiry.
    __out_ecount_part_opt(cchTypeDef, *pchTypeDef)
        LPWSTR szTypeDef,   // [OUT] Put name here.
    ULONG cchTypeDef,       // [IN] size of name buffer in wide chars.
    ULONG *pchTypeDef,      // [OUT] put size of name (wide chars) here.
    DWORD *pdwTypeDefFlags, // [OUT] Put flags here.
    mdToken *ptkExtends)    // [OUT] Put base class TypeDef/TypeRef here.
{
  HRESULT hr = S_OK;
  CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
  TypeDefRec *pTypeDefRec;
  BOOL fTruncation = FALSE;

  if (TypeFromToken(td) != mdtTypeDef) {
    hr = S_FALSE;
    goto ErrExit;
  }
  if (td == mdTypeDefNil) {
    // Backward compatibility with CLR 2.0 implementation
    if (pdwTypeDefFlags != NULL)
      *pdwTypeDefFlags = 0;
    if (ptkExtends != NULL)
      *ptkExtends = mdTypeRefNil;
    if (pchTypeDef != NULL)
      *pchTypeDef = 1;
    if ((szTypeDef != NULL) && (cchTypeDef > 0))
      szTypeDef[0] = 0;

    hr = S_OK;
    goto ErrExit;
  }

  pMiniMd->GetTypeDefRecord(RidFromToken(td), &pTypeDefRec);

  if ((szTypeDef != NULL) || (pchTypeDef != NULL)) {
    LPCSTR szNamespace;
    LPCSTR szName;

    pMiniMd->getNamespaceOfTypeDef(pTypeDefRec, &szNamespace);
    MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzNamespace, szNamespace);
    IfNullGo(wzNamespace);

    pMiniMd->getNameOfTypeDef(pTypeDefRec, &szName);
    MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzName, szName);

    if (szTypeDef != NULL) {
      fTruncation = !(ns::MakePath(szTypeDef, cchTypeDef, wzNamespace, wzName));
    }
    if (pchTypeDef != NULL) {
      if (fTruncation || (szTypeDef == NULL)) {
        *pchTypeDef = ns::GetFullLength(wzNamespace, wzName);
      } else {
        *pchTypeDef = (ULONG)(wcslen(szTypeDef) + 1);
      }
    }
  }
  if (pdwTypeDefFlags != NULL) {
    // caller wants type flags
    *pdwTypeDefFlags = pMiniMd->getFlagsOfTypeDef(pTypeDefRec);
  }
  if (ptkExtends != NULL) {
    *ptkExtends = pMiniMd->getExtendsOfTypeDef(pTypeDefRec);

    // take care of the 0 case
    if (RidFromToken(*ptkExtends) == 0) {
      *ptkExtends = mdTypeRefNil;
    }
  }

  if (fTruncation && (hr == S_OK)) {
    if ((szTypeDef != NULL) && (cchTypeDef > 0)) {
      // null-terminate the truncated output string
      szTypeDef[cchTypeDef - 1] = W('\0');
    }
    hr = CLDB_S_TRUNCATION;
  }

ErrExit:
  return hr;
}

HRESULT RegMeta::GetInterfaceImplProps( // S_OK or error.
    mdInterfaceImpl iiImpl,             // [IN] InterfaceImpl token.
    mdTypeDef *pClass, // [OUT] Put implementing class token here.
    mdToken *ptkIface) // [OUT] Put implemented interface token here.
{
  HRESULT hr = S_OK;

  CMiniMdRW *pMiniMd = NULL;
  InterfaceImplRec *pIIRec = NULL;

  _ASSERTE(TypeFromToken(iiImpl) == mdtInterfaceImpl);

  pMiniMd = &(m_pStgdb->m_MiniMd);
  IfFailGo(pMiniMd->GetInterfaceImplRecord(RidFromToken(iiImpl), &pIIRec));

  if (pClass) {
    *pClass = pMiniMd->getClassOfInterfaceImpl(pIIRec);
  }
  if (ptkIface) {
    *ptkIface = pMiniMd->getInterfaceOfInterfaceImpl(pIIRec);
  }

ErrExit:
  return hr;
}

HRESULT RegMeta::GetTypeRefProps( // S_OK or error.
    mdTypeRef tr,                 // [IN] TypeRef token.
    mdToken *
        ptkResolutionScope, // [OUT] Resolution scope, ModuleRef or AssemblyRef.
    __out_ecount_part_opt(cchName, *pchName)
        LPWSTR szTypeRef, // [OUT] Name of the TypeRef.
    ULONG cchTypeRef,     // [IN] Size of buffer.
    ULONG *pchTypeRef)    // [OUT] Size of Name.
{
  HRESULT hr = S_OK;
  CMiniMdRW *pMiniMd;
  TypeRefRec *pTypeRefRec;
  BOOL fTruncation = FALSE; // Was there name truncation?

  if (TypeFromToken(tr) != mdtTypeRef) {
    hr = S_FALSE;
    goto ErrExit;
  }
  if (tr == mdTypeRefNil) {
    // Backward compatibility with CLR 2.0 implementation
    if (ptkResolutionScope != NULL)
      *ptkResolutionScope = mdTokenNil;
    if (pchTypeRef != NULL)
      *pchTypeRef = 1;
    if ((szTypeRef != NULL) && (cchTypeRef > 0))
      szTypeRef[0] = 0;

    hr = S_OK;
    goto ErrExit;
  }

  pMiniMd = &(m_pStgdb->m_MiniMd);
  IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(tr), &pTypeRefRec));

  if (ptkResolutionScope != NULL) {
    *ptkResolutionScope = pMiniMd->getResolutionScopeOfTypeRef(pTypeRefRec);
  }

  if ((szTypeRef != NULL) || (pchTypeRef != NULL)) {
    LPCSTR szNamespace;
    LPCSTR szName;

    IfFailGo(pMiniMd->getNamespaceOfTypeRef(pTypeRefRec, &szNamespace));
    MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzNamespace, szNamespace);
    IfNullGo(wzNamespace);

    IfFailGo(pMiniMd->getNameOfTypeRef(pTypeRefRec, &szName));
    MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzName, szName);
    IfNullGo(wzName);

    if (szTypeRef != NULL) {
      fTruncation = !(ns::MakePath(szTypeRef, cchTypeRef, wzNamespace, wzName));
    }
    if (pchTypeRef != NULL) {
      if (fTruncation || (szTypeRef == NULL)) {
        *pchTypeRef = ns::GetFullLength(wzNamespace, wzName);
      } else {
        *pchTypeRef = (ULONG)(wcslen(szTypeRef) + 1);
      }
    }
  }
  if (fTruncation && (hr == S_OK)) {
    if ((szTypeRef != NULL) && (cchTypeRef > 0)) {
      // null-terminate the truncated output string
      szTypeRef[cchTypeRef - 1] = W('\0');
    }
    hr = CLDB_S_TRUNCATION;
  }

ErrExit:
  return hr;
}

HRESULT RegMeta::ResolveTypeRef(mdTypeRef tr, REFIID riid, IUnknown **ppIScope,
                                mdTypeDef *ptd) {
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - ResolveTypeRef - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::EnumMembers( // S_OK, S_FALSE, or error.
    HCORENUM *phEnum,         // [IN|OUT] Pointer to the enum.
    mdTypeDef cl,             // [IN] TypeDef to scope the enumeration.
    mdToken rMembers[],       // [OUT] Put MemberDefs here.
    ULONG cMax,               // [IN] Max MemberDefs to put.
    ULONG *pcTokens)          // [OUT] Put # put here.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumMembers - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::EnumMembersWithName( // S_OK, S_FALSE, or error.
    HCORENUM *phEnum,                 // [IN|OUT] Pointer to the enum.
    mdTypeDef cl,                     // [IN] TypeDef to scope the enumeration.
    LPCWSTR szName,     // [IN] Limit results to those with this name.
    mdToken rMembers[], // [OUT] Put MemberDefs here.
    ULONG cMax,         // [IN] Max MemberDefs to put.
    ULONG *pcTokens)    // [OUT] Put # put here.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumMembersWithName - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::EnumMethods( // S_OK, S_FALSE, or error.
    HCORENUM *phEnum,         // [IN|OUT] Pointer to the enum.
    mdTypeDef td,             // [IN] TypeDef to scope the enumeration.
    mdMethodDef rMethods[],   // [OUT] Put MethodDefs here.
    ULONG cMax,               // [IN] Max MethodDefs to put.
    ULONG *pcTokens)          // [OUT] Put # put here.
{
  HRESULT hr = NOERROR;
  HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **>(phEnum);
  ULONG ridStart;
  ULONG ridEnd;
  TypeDefRec *pRec;
  HENUMInternal *pEnum = *ppmdEnum;

  if (pEnum == 0) {
    // instantiating a new ENUM
    CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

    // Check for mdTypeDefNil (representing <Module>).
    // If so, this will map it to its token.
    //
    if (IsGlobalMethodParentTk(td)) {
      td = COR_GLOBAL_PARENT_TOKEN;
    }

    IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(td), &pRec));
    ridStart = m_pStgdb->m_MiniMd.getMethodListOfTypeDef(pRec);
    IfFailGo(m_pStgdb->m_MiniMd.getEndMethodListOfTypeDef(RidFromToken(td),
                                                          &ridEnd));

    if (pMiniMd->HasIndirectTable(TBL_Method) || pMiniMd->HasDelete()) {
      IfFailGo(HENUMInternal::CreateDynamicArrayEnum(mdtMethodDef, &pEnum));

      // add all methods to the dynamic array
      for (ULONG index = ridStart; index < ridEnd; index++) {
        if (pMiniMd->HasDelete()) {
          MethodRec *pMethRec;
          RID rid;
          IfFailGo(pMiniMd->GetMethodRid(index, &rid));
          IfFailGo(pMiniMd->GetMethodRecord(rid, &pMethRec));
          LPCSTR szMethodName;
          IfFailGo(pMiniMd->getNameOfMethod(pMethRec, &szMethodName));
          if (IsMdRTSpecialName(pMethRec->GetFlags()) &&
              IsDeletedName(szMethodName)) {
            continue;
          }
        }
        RID rid;
        IfFailGo(pMiniMd->GetMethodRid(index, &rid));
        IfFailGo(HENUMInternal::AddElementToEnum(
            pEnum, TokenFromRid(rid, mdtMethodDef)));
      }
    } else {
      IfFailGo(HENUMInternal::CreateSimpleEnum(mdtMethodDef, ridStart, ridEnd,
                                               &pEnum));
    }

    // set the output parameter
    *ppmdEnum = pEnum;
  }

  // fill the output token buffer
  hr = HENUMInternal::EnumWithCount(pEnum, cMax, rMethods, pcTokens);

ErrExit:
  HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);

  END_ENTRYPOINT_NOTHROW;

  return hr;
}

HRESULT RegMeta::EnumMethodsWithName( // S_OK, S_FALSE, or error.
    HCORENUM *phEnum,                 // [IN|OUT] Pointer to the enum.
    mdTypeDef cl,                     // [IN] TypeDef to scope the enumeration.
    LPCWSTR szName,         // [IN] Limit results to those with this name.
    mdMethodDef rMethods[], // [OU] Put MethodDefs here.
    ULONG cMax,             // [IN] Max MethodDefs to put.
    ULONG *pcTokens)        // [OUT] Put # put here.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumMethodsWithName - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::EnumFields( // S_OK, S_FALSE, or error.
    HCORENUM *phEnum,        // [IN|OUT] Pointer to the enum.
    mdTypeDef td,            // [IN] TypeDef to scope the enumeration.
    mdFieldDef rFields[],    // [OUT] Put FieldDefs here.
    ULONG cMax,              // [IN] Max FieldDefs to put.
    ULONG *pcTokens)         // [OUT] Put # put here.
{
  HRESULT hr = NOERROR;

  HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **>(phEnum);
  ULONG ridStart;
  ULONG ridEnd;
  TypeDefRec *pRec;
  HENUMInternal *pEnum = *ppmdEnum;

  if (pEnum == NULL) {
    // instantiating a new ENUM
    CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

    // Check for mdTypeDefNil (representing <Module>).
    // If so, this will map it to its token.
    //
    if (IsGlobalMethodParentTk(td)) {
      td = COR_GLOBAL_PARENT_TOKEN;
    }

    IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(td), &pRec));
    ridStart = m_pStgdb->m_MiniMd.getFieldListOfTypeDef(pRec);
    IfFailGo(
        m_pStgdb->m_MiniMd.getEndFieldListOfTypeDef(RidFromToken(td), &ridEnd));

    if (pMiniMd->HasIndirectTable(TBL_Field) || pMiniMd->HasDelete()) {
      IfFailGo(HENUMInternal::CreateDynamicArrayEnum(mdtFieldDef, &pEnum));

      // add all methods to the dynamic array
      for (ULONG index = ridStart; index < ridEnd; index++) {
        if (pMiniMd->HasDelete()) {
          FieldRec *pFieldRec;
          RID rid;
          IfFailGo(pMiniMd->GetFieldRid(index, &rid));
          IfFailGo(pMiniMd->GetFieldRecord(rid, &pFieldRec));
          LPCUTF8 szFieldName;
          IfFailGo(pMiniMd->getNameOfField(pFieldRec, &szFieldName));
          if (IsFdRTSpecialName(pFieldRec->GetFlags()) &&
              IsDeletedName(szFieldName)) {
            continue;
          }
        }
        RID rid;
        IfFailGo(pMiniMd->GetFieldRid(index, &rid));
        IfFailGo(HENUMInternal::AddElementToEnum(
            pEnum, TokenFromRid(rid, mdtFieldDef)));
      }
    } else {
      IfFailGo(HENUMInternal::CreateSimpleEnum(mdtFieldDef, ridStart, ridEnd,
                                               &pEnum));
    }

    // set the output parameter
    *ppmdEnum = pEnum;
  }

  // fill the output token buffer
  hr = HENUMInternal::EnumWithCount(pEnum, cMax, rFields, pcTokens);

ErrExit:
  HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);

  END_ENTRYPOINT_NOTHROW;

  return hr;
}

HRESULT RegMeta::EnumFieldsWithName( // S_OK, S_FALSE, or error.
    HCORENUM *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef cl,                    // [IN] TypeDef to scope the enumeration.
    LPCWSTR szName,       // [IN] Limit results to those with this name.
    mdFieldDef rFields[], // [OUT] Put MemberDefs here.
    ULONG cMax,           // [IN] Max MemberDefs to put.
    ULONG *pcTokens)      // [OUT] Put # put here.
{
  HRESULT hr = NOERROR;

  HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **>(phEnum);
  ULONG ridStart;
  ULONG ridEnd;
  ULONG index;
  TypeDefRec *pRec;
  FieldRec *pField;
  HENUMInternal *pEnum = *ppmdEnum;
  LPUTF8 szNameUtf8;
  UTF8STR(szName, szNameUtf8);
  LPCUTF8 szNameUtf8Tmp;

  if (pEnum == 0) {
    // instantiating a new ENUM
    CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

    // Check for mdTypeDefNil (representing <Module>).
    // If so, this will map it to its token.
    //
    if (IsGlobalMethodParentTk(cl)) {
      cl = COR_GLOBAL_PARENT_TOKEN;
    }

    // create the enumerator
    IfFailGo(HENUMInternal::CreateDynamicArrayEnum(mdtMethodDef, &pEnum));

    // get the range of field rids given a typedef
    IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(cl), &pRec));
    ridStart = m_pStgdb->m_MiniMd.getFieldListOfTypeDef(pRec);
    IfFailGo(
        m_pStgdb->m_MiniMd.getEndFieldListOfTypeDef(RidFromToken(cl), &ridEnd));

    for (index = ridStart; index < ridEnd; index++) {
      if (szNameUtf8 == NULL) {
        RID rid;
        IfFailGo(pMiniMd->GetFieldRid(index, &rid));
        IfFailGo(HENUMInternal::AddElementToEnum(
            pEnum, TokenFromRid(rid, mdtFieldDef)));
      } else {
        RID rid;
        IfFailGo(pMiniMd->GetFieldRid(index, &rid));
        IfFailGo(pMiniMd->GetFieldRecord(rid, &pField));
        IfFailGo(pMiniMd->getNameOfField(pField, &szNameUtf8Tmp));
        if (strcmp(szNameUtf8Tmp, szNameUtf8) == 0) {
          IfFailGo(pMiniMd->GetFieldRid(index, &rid));
          IfFailGo(HENUMInternal::AddElementToEnum(
              pEnum, TokenFromRid(rid, mdtFieldDef)));
        }
      }
    }

    // set the output parameter
    *ppmdEnum = pEnum;
  }

  // fill the output token buffer
  hr = HENUMInternal::EnumWithCount(pEnum, cMax, rFields, pcTokens);

ErrExit:
  HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);

  return hr;
}

HRESULT RegMeta::EnumParams( // S_OK, S_FALSE, or error.
    HCORENUM *phEnum,        // [IN|OUT] Pointer to the enum.
    mdMethodDef mb,          // [IN] MethodDef to scope the enumeration.
    mdParamDef rParams[],    // [OUT] Put ParamDefs here.
    ULONG cMax,              // [IN] Max ParamDefs to put.
    ULONG *pcTokens)         // [OUT] Put # put here.
{
  HRESULT hr = NOERROR;

  HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **>(phEnum);
  ULONG ridStart;
  ULONG ridEnd;
  MethodRec *pRec;
  HENUMInternal *pEnum = *ppmdEnum;

  if (pEnum == 0) {
    // instantiating a new ENUM
    CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
    IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(mb), &pRec));
    ridStart = m_pStgdb->m_MiniMd.getParamListOfMethod(pRec);
    IfFailGo(
        m_pStgdb->m_MiniMd.getEndParamListOfMethod(RidFromToken(mb), &ridEnd));

    if (pMiniMd->HasIndirectTable(TBL_Param)) {
      IfFailGo(HENUMInternal::CreateDynamicArrayEnum(mdtParamDef, &pEnum));

      // add all methods to the dynamic array
      for (ULONG index = ridStart; index < ridEnd; index++) {
        RID rid;
        IfFailGo(pMiniMd->GetParamRid(index, &rid));
        IfFailGo(HENUMInternal::AddElementToEnum(
            pEnum, TokenFromRid(rid, mdtParamDef)));
      }
    } else {
      IfFailGo(HENUMInternal::CreateSimpleEnum(mdtParamDef, ridStart, ridEnd,
                                               &pEnum));
    }

    // set the output parameter
    *ppmdEnum = pEnum;
  }

  // fill the output token buffer
  hr = HENUMInternal::EnumWithCount(pEnum, cMax, rParams, pcTokens);

ErrExit:
  HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);

  return hr;
}

HRESULT RegMeta::EnumMemberRefs( // S_OK, S_FALSE, or error.
    HCORENUM *phEnum,            // [IN|OUT] Pointer to the enum.
    mdToken tkParent,            // [IN] Parent token to scope the enumeration.
    mdMemberRef rMemberRefs[],   // [OUT] Put MemberRefs here.
    ULONG cMax,                  // [IN] Max MemberRefs to put.
    ULONG *pcTokens)             // [OUT] Put # put here.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumMemberRefs - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::EnumMethodImpls( // S_OK, S_FALSE, or error
    HCORENUM *phEnum,             // [IN|OUT] Pointer to the enum.
    mdTypeDef td,                 // [IN] TypeDef to scope the enumeration.
    mdToken rMethodBody[],        // [OUT] Put Method Body tokens here.
    mdToken rMethodDecl[],        // [OUT] Put Method Declaration tokens here.
    ULONG cMax,                   // [IN] Max tokens to put.
    ULONG *pcTokens)              // [OUT] Put # put here.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumMethodImpls - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::EnumPermissionSets( // S_OK, S_FALSE, or error.
    HCORENUM *phEnum,                // [IN|OUT] Pointer to the enum.
    mdToken tk,                 // [IN] if !NIL, token to scope the enumeration.
    DWORD dwActions,            // [IN] if !0, return only these actions.
    mdPermission rPermission[], // [OUT] Put Permissions here.
    ULONG cMax,                 // [IN] Max Permissions to put.
    ULONG *pcTokens)            // [OUT] Put # put here.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumPermissionSets - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::FindMember(
    mdTypeDef td,              // [IN] given typedef
    LPCWSTR szName,            // [IN] member name
    PCCOR_SIGNATURE pvSigBlob, // [IN] point to a blob value of CLR signature
    ULONG cbSigBlob,           // [IN] count of bytes in the signature blob
    mdToken *pmb)              // [OUT] matching memberdef
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - FindMember - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::FindMethod(
    mdTypeDef td,              // [IN] given typedef
    LPCWSTR szName,            // [IN] member name
    PCCOR_SIGNATURE pvSigBlob, // [IN] point to a blob value of CLR signature
    ULONG cbSigBlob,           // [IN] count of bytes in the signature blob
    mdMethodDef *pmb)          // [OUT] matching memberdef
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - FindMethod - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::FindField(
    mdTypeDef td,              // [IN] given typedef
    LPCWSTR szName,            // [IN] member name
    PCCOR_SIGNATURE pvSigBlob, // [IN] point to a blob value of CLR signature
    ULONG cbSigBlob,           // [IN] count of bytes in the signature blob
    mdFieldDef *pmb)           // [OUT] matching memberdef
{
  LOG((LF_CORDB, LL_INFO100000, "CordbSymbol - FindField - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::FindMemberRef(
    mdTypeRef td,              // [IN] given typeRef
    LPCWSTR szName,            // [IN] member name
    PCCOR_SIGNATURE pvSigBlob, // [IN] point to a blob value of CLR signature
    ULONG cbSigBlob,           // [IN] count of bytes in the signature blob
    mdMemberRef *pmr)          // [OUT] matching memberref
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - FindMemberRef - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::GetMethodProps(
    mdMethodDef mb,    // The method for which to get props.
    mdTypeDef *pClass, // Put method's class here.
    __out_ecount_part_opt(cchMethod, *pchMethod)
        LPWSTR szMethod,         // Put method's name here.
    ULONG cchMethod,             // Size of szMethod buffer in wide chars.
    ULONG *pchMethod,            // Put actual size here
    DWORD *pdwAttr,              // Put flags here.
    PCCOR_SIGNATURE *ppvSigBlob, // [OUT] point to the blob value of meta data
    ULONG *pcbSigBlob,           // [OUT] actual size of signature blob
    ULONG *pulCodeRVA,           // [OUT] codeRVA
    DWORD *pdwImplFlags)         // [OUT] Impl. Flags
{
  HRESULT hr = NOERROR;
  BEGIN_ENTRYPOINT_NOTHROW;

  MethodRec *pMethodRec;
  CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

  _ASSERTE(TypeFromToken(mb) == mdtMethodDef);

  IfFailGo(pMiniMd->GetMethodRecord(RidFromToken(mb), &pMethodRec));

  if (pClass) {
    // caller wants parent typedef
    IfFailGo(pMiniMd->FindParentOfMethodHelper(mb, pClass));

    if (IsGlobalMethodParentToken(*pClass)) {
      // If the parent of Method is the <Module>, return mdTypeDefNil instead.
      *pClass = mdTypeDefNil;
    }
  }
  if (ppvSigBlob || pcbSigBlob) {
    // caller wants signature information
    PCCOR_SIGNATURE pvSigTmp;
    ULONG cbSig;
    IfFailGo(pMiniMd->getSignatureOfMethod(pMethodRec, &pvSigTmp, &cbSig));
    if (ppvSigBlob)
      *ppvSigBlob = pvSigTmp;
    if (pcbSigBlob)
      *pcbSigBlob = cbSig;
  }
  if (pdwAttr) {
    *pdwAttr = pMiniMd->getFlagsOfMethod(pMethodRec);
  }
  if (pulCodeRVA) {
    *pulCodeRVA = pMiniMd->getRVAOfMethod(pMethodRec);
  }
  if (pdwImplFlags) {
    *pdwImplFlags = (DWORD)pMiniMd->getImplFlagsOfMethod(pMethodRec);
  }
  // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten
  // with S_OK
  if (szMethod || pchMethod) {
    IfFailGo(
        pMiniMd->getNameOfMethod(pMethodRec, szMethod, cchMethod, pchMethod));
  }

ErrExit:

  return hr;
}

HRESULT RegMeta::GetMemberRefProps( // S_OK or error.
    mdMemberRef mr,                 // [IN] given memberref
    mdToken *ptk,                   // [OUT] Put classref or classdef here.
    __out_ecount_part_opt(cchMember, *pchMember)
        LPWSTR szMember,         // [OUT] buffer to fill for member's name
    ULONG cchMember,             // [IN] the count of char of szMember
    ULONG *pchMember,            // [OUT] actual count of char in member name
    PCCOR_SIGNATURE *ppvSigBlob, // [OUT] point to meta data blob value
    ULONG *pbSig)                // [OUT] actual size of signature blob
{
  HRESULT hr = NOERROR;

  CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
  MemberRefRec *pMemberRefRec;

  _ASSERTE(TypeFromToken(mr) == mdtMemberRef);

  IfFailGo(pMiniMd->GetMemberRefRecord(RidFromToken(mr), &pMemberRefRec));

  if (ptk) {
    *ptk = pMiniMd->getClassOfMemberRef(pMemberRefRec);
    if (IsGlobalMethodParentToken(*ptk)) {
      // If the parent of MemberRef is the <Module>, return mdTypeDefNil
      // instead.
      *ptk = mdTypeDefNil;
    }
  }
  if (ppvSigBlob || pbSig) {
    // caller wants signature information
    PCCOR_SIGNATURE pvSigTmp;
    ULONG cbSig;
    IfFailGo(
        pMiniMd->getSignatureOfMemberRef(pMemberRefRec, &pvSigTmp, &cbSig));
    if (ppvSigBlob)
      *ppvSigBlob = pvSigTmp;
    if (pbSig)
      *pbSig = cbSig;
  }
  // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten
  // with S_OK
  if (szMember || pchMember) {
    IfFailGo(pMiniMd->getNameOfMemberRef(pMemberRefRec, szMember, cchMember,
                                         pchMember));
  }

ErrExit:

  return hr;
}

HRESULT RegMeta::EnumProperties( // S_OK, S_FALSE, or error.
    HCORENUM *phEnum,            // [IN|OUT] Pointer to the enum.
    mdTypeDef td,                // [IN] TypeDef to scope the enumeration.
    mdProperty rProperties[],    // [OUT] Put Properties here.
    ULONG cMax,                  // [IN] Max properties to put.
    ULONG *pcProperties)         // [OUT] Put # put here.
{
  HRESULT hr = NOERROR;

  HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **>(phEnum);
  ULONG ridStart = 0;
  ULONG ridEnd = 0;
  ULONG ridMax = 0;
  HENUMInternal *pEnum = *ppmdEnum;

  if (IsNilToken(td)) {
    if (pcProperties)
      *pcProperties = 0;
    hr = S_FALSE;
    goto ErrExit;
  }

  _ASSERTE(TypeFromToken(td) == mdtTypeDef);

  if (pEnum == 0) {
    // instantiating a new ENUM
    CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
    RID ridPropertyMap;
    PropertyMapRec *pPropertyMapRec;

    // get the starting/ending rid of properties of this typedef
    IfFailGo(pMiniMd->FindPropertyMapFor(RidFromToken(td), &ridPropertyMap));
    if (!InvalidRid(ridPropertyMap)) {
      IfFailGo(m_pStgdb->m_MiniMd.GetPropertyMapRecord(ridPropertyMap,
                                                       &pPropertyMapRec));
      ridStart = pMiniMd->getPropertyListOfPropertyMap(pPropertyMapRec);
      IfFailGo(
          pMiniMd->getEndPropertyListOfPropertyMap(ridPropertyMap, &ridEnd));
      ridMax = pMiniMd->getCountPropertys() + 1;
      if (ridStart == 0)
        ridStart = 1;
      if (ridEnd > ridMax)
        ridEnd = ridMax;
      if (ridStart > ridEnd)
        ridStart = ridEnd;
    }

    if (pMiniMd->HasIndirectTable(TBL_Property) || pMiniMd->HasDelete()) {
      IfFailGo(HENUMInternal::CreateDynamicArrayEnum(mdtProperty, &pEnum));

      // add all methods to the dynamic array
      for (ULONG index = ridStart; index < ridEnd; index++) {
        if (pMiniMd->HasDelete()) {
          PropertyRec *pRec;
          RID rid;
          IfFailGo(pMiniMd->GetPropertyRid(index, &rid));
          IfFailGo(pMiniMd->GetPropertyRecord(rid, &pRec));
          LPCUTF8 szPropertyName;
          IfFailGo(pMiniMd->getNameOfProperty(pRec, &szPropertyName));
          if (IsPrRTSpecialName(pRec->GetPropFlags()) &&
              IsDeletedName(szPropertyName)) {
            continue;
          }
        }
        RID rid;
        IfFailGo(pMiniMd->GetPropertyRid(index, &rid));
        IfFailGo(HENUMInternal::AddElementToEnum(
            pEnum, TokenFromRid(rid, mdtProperty)));
      }
    } else {
      IfFailGo(HENUMInternal::CreateSimpleEnum(mdtProperty, ridStart, ridEnd,
                                               &pEnum));
    }

    // set the output parameter
    *ppmdEnum = pEnum;
  }

  // fill the output token buffer
  hr = HENUMInternal::EnumWithCount(pEnum, cMax, rProperties, pcProperties);

ErrExit:
  HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);

  return hr;
}

HRESULT RegMeta::EnumEvents( // S_OK, S_FALSE, or error.
    HCORENUM *phEnum,        // [IN|OUT] Pointer to the enum.
    mdTypeDef td,            // [IN] TypeDef to scope the enumeration.
    mdEvent rEvents[],       // [OUT] Put events here.
    ULONG cMax,              // [IN] Max events to put.
    ULONG *pcEvents)         // [OUT] Put # put here.
{
  HRESULT hr = NOERROR;

  HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **>(phEnum);
  ULONG ridStart = 0;
  ULONG ridEnd = 0;
  ULONG ridMax = 0;
  HENUMInternal *pEnum = *ppmdEnum;

  _ASSERTE(TypeFromToken(td) == mdtTypeDef);

  if (pEnum == 0) {
    // instantiating a new ENUM
    CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
    RID ridEventMap;
    EventMapRec *pEventMapRec;

    // get the starting/ending rid of properties of this typedef
    IfFailGo(pMiniMd->FindEventMapFor(RidFromToken(td), &ridEventMap));
    if (!InvalidRid(ridEventMap)) {
      IfFailGo(pMiniMd->GetEventMapRecord(ridEventMap, &pEventMapRec));
      ridStart = pMiniMd->getEventListOfEventMap(pEventMapRec);
      IfFailGo(pMiniMd->getEndEventListOfEventMap(ridEventMap, &ridEnd));
      ridMax = pMiniMd->getCountEvents() + 1;
      if (ridStart == 0)
        ridStart = 1;
      if (ridEnd > ridMax)
        ridEnd = ridMax;
      if (ridStart > ridEnd)
        ridStart = ridEnd;
    }

    if (pMiniMd->HasIndirectTable(TBL_Event) || pMiniMd->HasDelete()) {
      IfFailGo(HENUMInternal::CreateDynamicArrayEnum(mdtEvent, &pEnum));

      // add all methods to the dynamic array
      for (ULONG index = ridStart; index < ridEnd; index++) {
        if (pMiniMd->HasDelete()) {
          EventRec *pRec;
          RID rid;
          IfFailGo(pMiniMd->GetEventRid(index, &rid));
          IfFailGo(pMiniMd->GetEventRecord(rid, &pRec));
          LPCSTR szEventName;
          IfFailGo(pMiniMd->getNameOfEvent(pRec, &szEventName));
          if (IsEvRTSpecialName(pRec->GetEventFlags()) &&
              IsDeletedName(szEventName)) {
            continue;
          }
        }
        RID rid;
        IfFailGo(pMiniMd->GetEventRid(index, &rid));
        IfFailGo(HENUMInternal::AddElementToEnum(pEnum,
                                                 TokenFromRid(rid, mdtEvent)));
      }
    } else {
      IfFailGo(
          HENUMInternal::CreateSimpleEnum(mdtEvent, ridStart, ridEnd, &pEnum));
    }

    // set the output parameter
    *ppmdEnum = pEnum;
  }

  // fill the output token buffer
  hr = HENUMInternal::EnumWithCount(pEnum, cMax, rEvents, pcEvents);

ErrExit:
  HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);

  return hr;
}

HRESULT RegMeta::GetEventProps( // S_OK, S_FALSE, or error.
    mdEvent ev,                 // [IN] event token
    mdTypeDef *pClass,          // [OUT] typedef containing the event declarion.
    LPCWSTR szEvent,            // [OUT] Event name
    ULONG cchEvent,             // [IN] the count of wchar of szEvent
    ULONG *pchEvent,            // [OUT] actual count of wchar for event's name
    DWORD *pdwEventFlags,       // [OUT] Event flags.
    mdToken *ptkEventType,      // [OUT] EventType class
    mdMethodDef *pmdAddOn,      // [OUT] AddOn method of the event
    mdMethodDef *pmdRemoveOn,   // [OUT] RemoveOn method of the event
    mdMethodDef *pmdFire,       // [OUT] Fire method of the event
    mdMethodDef rmdOtherMethod[], // [OUT] other method of the event
    ULONG cMax,                   // [IN] size of rmdOtherMethod
    ULONG *pcOtherMethod) // [OUT] total number of other method of this event
{
  HRESULT hr = NOERROR;

  CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
  EventRec *pRec;
  HENUMInternal hEnum;

  _ASSERTE(TypeFromToken(ev) == mdtEvent);

  HENUMInternal::ZeroEnum(&hEnum);
  IfFailGo(pMiniMd->GetEventRecord(RidFromToken(ev), &pRec));

  if (pClass) {
    // find the event map entry corresponding to this event
    IfFailGo(pMiniMd->FindParentOfEventHelper(ev, pClass));
  }
  if (pdwEventFlags) {
    *pdwEventFlags = pMiniMd->getEventFlagsOfEvent(pRec);
  }
  if (ptkEventType) {
    *ptkEventType = pMiniMd->getEventTypeOfEvent(pRec);
  }
  {
    MethodSemanticsRec *pSemantics;
    RID ridCur;
    ULONG cCurOtherMethod = 0;
    ULONG ulSemantics;
    mdMethodDef tkMethod;

    // initialize output parameters
    if (pmdAddOn)
      *pmdAddOn = mdMethodDefNil;
    if (pmdRemoveOn)
      *pmdRemoveOn = mdMethodDefNil;
    if (pmdFire)
      *pmdFire = mdMethodDefNil;

    IfFailGo(pMiniMd->FindMethodSemanticsHelper(ev, &hEnum));
    while (HENUMInternal::EnumNext(&hEnum, (mdToken *)&ridCur)) {
      IfFailGo(pMiniMd->GetMethodSemanticsRecord(ridCur, &pSemantics));
      ulSemantics = pMiniMd->getSemanticOfMethodSemantics(pSemantics);
      tkMethod = TokenFromRid(pMiniMd->getMethodOfMethodSemantics(pSemantics),
                              mdtMethodDef);
      switch (ulSemantics) {
      case msAddOn:
        if (pmdAddOn)
          *pmdAddOn = tkMethod;
        break;
      case msRemoveOn:
        if (pmdRemoveOn)
          *pmdRemoveOn = tkMethod;
        break;
      case msFire:
        if (pmdFire)
          *pmdFire = tkMethod;
        break;
      case msOther:
        if (cCurOtherMethod < cMax)
          rmdOtherMethod[cCurOtherMethod] = tkMethod;
        cCurOtherMethod++;
        break;
      default:
        _ASSERTE(!"BadKind!");
      }
    }

    // set the output parameter
    if (pcOtherMethod)
      *pcOtherMethod = cCurOtherMethod;
  }
  // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten
  // with S_OK
  if (szEvent || pchEvent) {
    IfFailGo(
        pMiniMd->getNameOfEvent(pRec, (LPWSTR)szEvent, cchEvent, pchEvent));
  }

ErrExit:
  HENUMInternal::ClearEnum(&hEnum);

  return hr;
}

HRESULT RegMeta::EnumMethodSemantics( // S_OK, S_FALSE, or error.
    HCORENUM *phEnum,                 // [IN|OUT] Pointer to the enum.
    mdMethodDef mb,       // [IN] MethodDef to scope the enumeration.
    mdToken rEventProp[], // [OUT] Put Event/Property here.
    ULONG cMax,           // [IN] Max properties to put.
    ULONG *pcEventProp)   // [OUT] Put # put here.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumMethodSemantics - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::GetMethodSemantics( // S_OK, S_FALSE, or error.
    mdMethodDef mb,                  // [IN] method token
    mdToken tkEventProp,             // [IN] event/property token.
    DWORD *
        pdwSemanticsFlags) // [OUT] the role flags for the method/propevent pair
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - GetMethodSemantics - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::GetClassLayout(
    mdTypeDef td,                    // [IN] give typedef
    DWORD *pdwPackSize,              // [OUT] 1, 2, 4, 8, or 16
    COR_FIELD_OFFSET rFieldOffset[], // [OUT] field offset array
    ULONG cMax,                      // [IN] size of the array
    ULONG *pcFieldOffset,            // [OUT] needed array size
    ULONG *pulClassSize)             // [OUT] the size of the class
{
  HRESULT hr = NOERROR;

  CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
  ClassLayoutRec *pRec;
  RID ridClassLayout;
  int bLayout = 0; // Was any layout information found?

  _ASSERTE(TypeFromToken(td) == mdtTypeDef);

  IfFailGo(pMiniMd->FindClassLayoutHelper(td, &ridClassLayout));

  if (InvalidRid(ridClassLayout)) {
    // Nothing specified - return default values of 0.
    if (pdwPackSize)
      *pdwPackSize = 0;
    if (pulClassSize)
      *pulClassSize = 0;
  } else {
    IfFailGo(
        pMiniMd->GetClassLayoutRecord(RidFromToken(ridClassLayout), &pRec));
    if (pdwPackSize)
      *pdwPackSize = pMiniMd->getPackingSizeOfClassLayout(pRec);
    if (pulClassSize)
      *pulClassSize = pMiniMd->getClassSizeOfClassLayout(pRec);
    bLayout = 1;
  }

  // fill the layout array
  if (rFieldOffset || pcFieldOffset) {
    ULONG iFieldOffset = 0;
    ULONG ridFieldStart;
    ULONG ridFieldEnd;
    ULONG ridFieldLayout;
    ULONG ulOffset;
    TypeDefRec *pTypeDefRec;
    FieldLayoutRec *pLayout2Rec;
    mdFieldDef fd;

    // record for this typedef in TypeDef Table
    IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(td), &pTypeDefRec));

    // find the starting and end field for this typedef
    ridFieldStart = pMiniMd->getFieldListOfTypeDef(pTypeDefRec);
    IfFailGo(pMiniMd->getEndFieldListOfTypeDef(RidFromToken(td), &ridFieldEnd));

    // loop through the field table

    for (; ridFieldStart < ridFieldEnd; ridFieldStart++) {
      // Calculate the field token.
      RID rid;
      IfFailGo(pMiniMd->GetFieldRid(ridFieldStart, &rid));
      fd = TokenFromRid(rid, mdtFieldDef);

      // Calculate the FieldLayout rid for the current field.
      IfFailGo(pMiniMd->FindFieldLayoutHelper(fd, &ridFieldLayout));

      // Calculate the offset.
      if (InvalidRid(ridFieldLayout))
        ulOffset = (ULONG)-1;
      else {
        // get the FieldLayout record.
        IfFailGo(pMiniMd->GetFieldLayoutRecord(ridFieldLayout, &pLayout2Rec));
        ulOffset = pMiniMd->getOffSetOfFieldLayout(pLayout2Rec);
        bLayout = 1;
      }

      // fill in the field layout if output buffer still has space.
      if (cMax > iFieldOffset && rFieldOffset) {
        rFieldOffset[iFieldOffset].ridOfField = fd;
        rFieldOffset[iFieldOffset].ulOffset = ulOffset;
      }

      // advance the index to the buffer.
      iFieldOffset++;
    }

    if (bLayout && pcFieldOffset)
      *pcFieldOffset = iFieldOffset;
  }

  if (!bLayout)
    hr = CLDB_E_RECORD_NOTFOUND;

ErrExit:

  return hr;
}

HRESULT RegMeta::GetFieldMarshal(
    mdToken tk,                     // [IN] given a field's memberdef
    PCCOR_SIGNATURE *ppvNativeType, // [OUT] native type of this field
    ULONG *pcbNativeType) // [OUT] the count of bytes of *ppvNativeType
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - GetFieldMarshal - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::GetRVA( // S_OK or error.
    mdToken tk,          // Member for which to set offset
    ULONG *pulCodeRVA,   // The offset
    DWORD *pdwImplFlags) // the implementation flags
{
  LOG((LF_CORDB, LL_INFO100000, "CordbSymbol - GetRVA - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::GetPermissionSetProps(
    mdPermission pm,            // [IN] the permission token.
    DWORD *pdwAction,           // [OUT] CorDeclSecurity.
    void const **ppvPermission, // [OUT] permission blob.
    ULONG *pcbPermission)       // [OUT] count of bytes of pvPermission.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - GetPermissionSetProps - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::GetSigFromToken( // S_OK or error.
    mdSignature mdSig,            // [IN] Signature token.
    PCCOR_SIGNATURE *ppvSig,      // [OUT] return pointer to token.
    ULONG *pcbSig)                // [OUT] return size of signature.
{
  HRESULT hr = NOERROR;

  CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
  StandAloneSigRec *pRec;

  _ASSERTE(TypeFromToken(mdSig) == mdtSignature);
  _ASSERTE(ppvSig && pcbSig);

  IfFailGo(pMiniMd->GetStandAloneSigRecord(RidFromToken(mdSig), &pRec));
  IfFailGo(pMiniMd->getSignatureOfStandAloneSig(pRec, ppvSig, pcbSig));

ErrExit:

  return hr;
}

HRESULT RegMeta::GetModuleRefProps( // S_OK or error.
    mdModuleRef mur,                // [IN] moduleref token.
    __out_ecount_part_opt(cchName, *pchName)
        LPWSTR szName, // [OUT] buffer to fill with the moduleref name.
    ULONG cchName,     // [IN] size of szName in wide characters.
    ULONG *pchName)    // [OUT] actual count of characters in the name.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - GetModuleRefProps - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::EnumModuleRefs( // S_OK or error.
    HCORENUM *phEnum,            // [IN|OUT] pointer to the enum.
    mdModuleRef rModuleRefs[],   // [OUT] put modulerefs here.
    ULONG cmax,                  // [IN] max memberrefs to put.
    ULONG *pcModuleRefs)         // [OUT] put # put here.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumModuleRefs - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::GetTypeSpecFromToken( // S_OK or error.
    mdTypeSpec typespec,               // [IN] TypeSpec token.
    PCCOR_SIGNATURE *ppvSig, // [OUT] return pointer to TypeSpec signature
    ULONG *pcbSig)           // [OUT] return size of signature.
{
  HRESULT hr = NOERROR;

  CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
  TypeSpecRec *pRec = NULL;

  _ASSERTE(TypeFromToken(typespec) == mdtTypeSpec);
  _ASSERTE(ppvSig && pcbSig);

  IfFailGo(pMiniMd->GetTypeSpecRecord(RidFromToken(typespec), &pRec));
  IfFailGo(pMiniMd->getSignatureOfTypeSpec(pRec, ppvSig, pcbSig));

ErrExit:

  return hr;
}

HRESULT RegMeta::GetNameFromToken( // Not Recommended! May be removed!
    mdToken tk, // [IN] Token to get name from.  Must have a name.
    MDUTF8CSTR *pszUtf8NamePtr) // [OUT] Return pointer to UTF8 name in heap.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - GetNameFromToken - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::EnumUnresolvedMethods( // S_OK, S_FALSE, or error.
    HCORENUM *phEnum,                   // [IN|OUT] Pointer to the enum.
    mdToken rMethods[],                 // [OUT] Put MemberDefs here.
    ULONG cMax,                         // [IN] Max MemberDefs to put.
    ULONG *pcTokens)                    // [OUT] Put # put here.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumUnresolvedMethods - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::GetUserString(                       // S_OK or error.
    mdString stk,                                     // [IN] String token.
    __out_ecount_opt(cchStringSize) LPWSTR wszString, // [OUT] Copy of string.
    ULONG cchStringSize,   // [IN] Max chars of room in szString.
    ULONG *pcchStringSize) // [OUT] How many chars in actual string.
{
  HRESULT hr = S_OK;
  ULONG cchStringSize_Dummy;
  MetaData::DataBlob userString;

  // Get the string data.
  IfFailGo(m_pStgdb->m_MiniMd.GetUserString(RidFromToken(stk), &userString));
  // Want to get whole characters, followed by byte to indicate whether there
  // are extended characters (>= 0x80).
  if ((userString.GetSize() % sizeof(WCHAR)) == 0) {
    Debug_ReportError(
        "User strings should have 1 byte terminator (either 0x00 or 0x80).");
    IfFailGo(CLDB_E_FILE_CORRUPT);
  }

  // Strip off the last byte.
  if (!userString.TruncateBySize(1)) {
    Debug_ReportInternalError(
        "There's a bug, because previous % 2 check didn't return 0.");
    IfFailGo(METADATA_E_INTERNAL_ERROR);
  }

  // Convert bytes to characters.
  if (pcchStringSize == NULL) {
    pcchStringSize = &cchStringSize_Dummy;
  }
  *pcchStringSize = userString.GetSize() / sizeof(WCHAR);

  // Copy the string back to the caller.
  if ((wszString != NULL) && (cchStringSize > 0)) {
    ULONG cbStringSize = cchStringSize * sizeof(WCHAR);
    memcpy(wszString, userString.GetDataPointer(),
           min(userString.GetSize(), cbStringSize));
    if (cbStringSize < userString.GetSize()) {
      if ((wszString != NULL) && (cchStringSize > 0)) {
        // null-terminate the truncated output string
        wszString[cchStringSize - 1] = W('\0');
      }

      hr = CLDB_S_TRUNCATION;
    }
  }

ErrExit:
  return hr;
}

HRESULT RegMeta::GetPinvokeMap( // S_OK or error.
    mdToken tk,                 // [IN] FieldDef or MethodDef.
    DWORD *pdwMappingFlags,     // [OUT] Flags used for mapping.
    __out_ecount_part_opt(cchImportName, *pchImportName)
        LPWSTR szImportName,   // [OUT] Import name.
    ULONG cchImportName,       // [IN] Size of the name buffer.
    ULONG *pchImportName,      // [OUT] Actual number of characters stored.
    mdModuleRef *pmrImportDLL) // [OUT] ModuleRef token for the target DLL.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - GetPinvokeMap - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::EnumSignatures( // S_OK or error.
    HCORENUM *phEnum,            // [IN|OUT] pointer to the enum.
    mdSignature rSignatures[],   // [OUT] put signatures here.
    ULONG cmax,                  // [IN] max signatures to put.
    ULONG *pcSignatures)         // [OUT] put # put here.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumSignatures - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::EnumTypeSpecs( // S_OK or error.
    HCORENUM *phEnum,           // [IN|OUT] pointer to the enum.
    mdTypeSpec rTypeSpecs[],    // [OUT] put TypeSpecs here.
    ULONG cmax,                 // [IN] max TypeSpecs to put.
    ULONG *pcTypeSpecs)         // [OUT] put # put here.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumTypeSpecs - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::EnumUserStrings( // S_OK or error.
    HCORENUM *phEnum,             // [IN/OUT] pointer to the enum.
    mdString rStrings[],          // [OUT] put Strings here.
    ULONG cmax,                   // [IN] max Strings to put.
    ULONG *pcStrings)             // [OUT] put # put here.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - EnumUserStrings - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::GetParamForMethodIndex( // S_OK or error.
    mdMethodDef md,                      // [IN] Method token.
    ULONG ulParamSeq,                    // [IN] Parameter sequence.
    mdParamDef *ppd)                     // [IN] Put Param token here.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - GetParamForMethodIndex - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::EnumCustomAttributes( // S_OK or error.
    HCORENUM *phEnum,                  // [IN, OUT] COR enumerator.
    mdToken tk,     // [IN] Token to scope the enumeration, 0 for all.
    mdToken tkType, // [IN] Type of interest, 0 for all.
    mdCustomAttribute
        rCustomAttributes[], // [OUT] Put custom attribute tokens here.
    ULONG cMax,              // [IN] Size of rCustomAttributes.
    ULONG
        *pcCustomAttributes) // [OUT, OPTIONAL] Put count of token values here.
{
  HRESULT hr = S_OK;

  HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **>(phEnum);
  ULONG ridStart;
  ULONG ridEnd;
  HENUMInternal *pEnum = *ppmdEnum;
  CustomAttributeRec *pRec;
  ULONG index;

  if (pEnum == 0) {
    // instantiating a new ENUM
    CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
    CLookUpHash *pHashTable = pMiniMd->m_pLookUpHashs[TBL_CustomAttribute];

    // Does caller want all custom Values?
    if (IsNilToken(tk)) {
      IfFailGo(HENUMInternal::CreateSimpleEnum(
          mdtCustomAttribute, 1, pMiniMd->getCountCustomAttributes() + 1,
          &pEnum));
    } else {
      // Scope by some object.
      if (pMiniMd->IsSorted(TBL_CustomAttribute)) {
        // Get CustomAttributes for the object.
        IfFailGo(pMiniMd->getCustomAttributeForToken(tk, &ridEnd, &ridStart));

        if (IsNilToken(tkType)) {
          // Simple enumerator for object's entire list.
          IfFailGo(HENUMInternal::CreateSimpleEnum(mdtCustomAttribute, ridStart,
                                                   ridEnd, &pEnum));
        } else {
          // Dynamic enumerator for subsetted list.

          IfFailGo(HENUMInternal::CreateDynamicArrayEnum(mdtCustomAttribute,
                                                         &pEnum));

          for (index = ridStart; index < ridEnd; index++) {
            IfFailGo(pMiniMd->GetCustomAttributeRecord(index, &pRec));
            if (tkType == pMiniMd->getTypeOfCustomAttribute(pRec)) {
              IfFailGo(HENUMInternal::AddElementToEnum(
                  pEnum, TokenFromRid(index, mdtCustomAttribute)));
            }
          }
        }
      } else {

        if (pHashTable) {
          // table is not sorted but hash is built
          // We want to create dynmaic array to hold the dynamic enumerator.
          TOKENHASHENTRY *p;
          ULONG iHash;
          int pos;
          mdToken tkParentTmp;
          mdToken tkTypeTmp;

          // Hash the data.
          iHash = pMiniMd->HashCustomAttribute(tk);

          IfFailGo(HENUMInternal::CreateDynamicArrayEnum(mdtCustomAttribute,
                                                         &pEnum));

          // Go through every entry in the hash chain looking for ours.
          for (p = pHashTable->FindFirst(iHash, pos); p;
               p = pHashTable->FindNext(pos)) {

            CustomAttributeRec *pCustomAttribute;
            IfFailGo(pMiniMd->GetCustomAttributeRecord(RidFromToken(p->tok),
                                                       &pCustomAttribute));
            tkParentTmp = pMiniMd->getParentOfCustomAttribute(pCustomAttribute);
            tkTypeTmp = pMiniMd->getTypeOfCustomAttribute(pCustomAttribute);
            if (tkParentTmp == tk) {
              if (IsNilToken(tkType) || tkType == tkTypeTmp) {
                // compare the blob value
                IfFailGo(HENUMInternal::AddElementToEnum(
                    pEnum, TokenFromRid(p->tok, mdtCustomAttribute)));
              }
            }
          }
        } else {

          // table is not sorted and hash is not built so we have to create
          // dynmaic array create the dynamic enumerator and loop through CA
          // table linearly
          //
          ridStart = 1;
          ridEnd = pMiniMd->getCountCustomAttributes() + 1;

          IfFailGo(HENUMInternal::CreateDynamicArrayEnum(mdtCustomAttribute,
                                                         &pEnum));

          for (index = ridStart; index < ridEnd; index++) {
            IfFailGo(pMiniMd->GetCustomAttributeRecord(index, &pRec));
            if (tk == pMiniMd->getParentOfCustomAttribute(pRec) &&
                (tkType == pMiniMd->getTypeOfCustomAttribute(pRec) ||
                 IsNilToken(tkType))) {
              IfFailGo(HENUMInternal::AddElementToEnum(
                  pEnum, TokenFromRid(index, mdtCustomAttribute)));
            }
          }
        }
      }
    }

    // set the output parameter
    *ppmdEnum = pEnum;
  }

  // fill the output token buffer
  hr = HENUMInternal::EnumWithCount(pEnum, cMax, rCustomAttributes,
                                    pcCustomAttributes);

ErrExit:
  HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);

  return hr;
}

HRESULT RegMeta::GetCustomAttributeProps( // S_OK or error.
    mdCustomAttribute cv,                 // [IN] CustomAttribute token.
    mdToken *ptkObj,     // [OUT, OPTIONAL] Put object token here.
    mdToken *ptkType,    // [OUT, OPTIONAL] Put AttrType token here.
    void const **ppBlob, // [OUT, OPTIONAL] Put pointer to data here.
    ULONG *pcbSize)      // [OUT, OPTIONAL] Put size of date here.
{
  HRESULT hr = S_OK; // A result.

  CMiniMdRW *pMiniMd;

  _ASSERTE(TypeFromToken(cv) == mdtCustomAttribute);

  pMiniMd = &(m_pStgdb->m_MiniMd);
  CustomAttributeRec *pCustomAttributeRec; // The custom value record.

  IfFailGo(pMiniMd->GetCustomAttributeRecord(RidFromToken(cv),
                                             &pCustomAttributeRec));

  if (ptkObj)
    *ptkObj = pMiniMd->getParentOfCustomAttribute(pCustomAttributeRec);

  if (ptkType)
    *ptkType = pMiniMd->getTypeOfCustomAttribute(pCustomAttributeRec);

  if (ppBlob != NULL) {
    IfFailGo(pMiniMd->getValueOfCustomAttribute(
        pCustomAttributeRec, (const BYTE **)ppBlob, pcbSize));
  }

ErrExit:

  return hr;
}

HRESULT RegMeta::FindTypeRef(
    mdToken tkResolutionScope, // [IN] ModuleRef, AssemblyRef or TypeRef.
    LPCWSTR szName,            // [IN] TypeRef Name.
    mdTypeRef *ptr)            // [OUT] matching TypeRef.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - FindTypeRef - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::GetMemberProps(
    mdToken mb,        // The member for which to get props.
    mdTypeDef *pClass, // Put member's class here.
    __out_ecount_part_opt(cchMember, *pchMember)
        LPWSTR szMember,         // Put member's name here.
    ULONG cchMember,             // Size of szMember buffer in wide chars.
    ULONG *pchMember,            // Put actual size here
    DWORD *pdwAttr,              // Put flags here.
    PCCOR_SIGNATURE *ppvSigBlob, // [OUT] point to the blob value of meta data
    ULONG *pcbSigBlob,           // [OUT] actual size of signature blob
    ULONG *pulCodeRVA,           // [OUT] codeRVA
    DWORD *pdwImplFlags,         // [OUT] Impl. Flags
    DWORD
        *pdwCPlusTypeFlag, // [OUT] flag for value type. selected ELEMENT_TYPE_*
    UVCP_CONSTANT *ppValue, // [OUT] constant value
    ULONG *
        pcchValue) // [OUT] size of constant string in chars, 0 for non-strings.
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - GetMemberProps - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::GetFieldProps(
    mdFieldDef fd,     // The field for which to get props.
    mdTypeDef *pClass, // Put field's class here.
    __out_ecount_part_opt(cchField, *pchField)
        LPWSTR szField,          // Put field's name here.
    ULONG cchField,              // Size of szField buffer in wide chars.
    ULONG *pchField,             // Put actual size here
    DWORD *pdwAttr,              // Put flags here.
    PCCOR_SIGNATURE *ppvSigBlob, // [OUT] point to the blob value of meta data
    ULONG *pcbSigBlob,           // [OUT] actual size of signature blob
    DWORD
        *pdwCPlusTypeFlag, // [OUT] flag for value type. selected ELEMENT_TYPE_*
    UVCP_CONSTANT *ppValue, // [OUT] constant value
    ULONG
        *pchValue) // [OUT] size of constant string in chars, 0 for non-strings.
{
  HRESULT hr = NOERROR;

  BEGIN_ENTRYPOINT_NOTHROW;

  FieldRec *pFieldRec;
  CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

  _ASSERTE(TypeFromToken(fd) == mdtFieldDef);

  IfFailGo(pMiniMd->GetFieldRecord(RidFromToken(fd), &pFieldRec));

  if (pClass) {
    // caller wants parent typedef
    IfFailGo(pMiniMd->FindParentOfFieldHelper(fd, pClass));

    if (IsGlobalMethodParentToken(*pClass)) {
      // If the parent of Field is the <Module>, return mdTypeDefNil instead.
      *pClass = mdTypeDefNil;
    }
  }
  if (ppvSigBlob || pcbSigBlob) {
    // caller wants signature information
    PCCOR_SIGNATURE pvSigTmp;
    ULONG cbSig;
    IfFailGo(pMiniMd->getSignatureOfField(pFieldRec, &pvSigTmp, &cbSig));
    if (ppvSigBlob)
      *ppvSigBlob = pvSigTmp;
    if (pcbSigBlob)
      *pcbSigBlob = cbSig;
  }
  if (pdwAttr) {
    *pdwAttr = pMiniMd->getFlagsOfField(pFieldRec);
  }
  if (pdwCPlusTypeFlag || ppValue || pchValue) {
    // get the constant value
    ULONG cbValue;
    RID rid;
    IfFailGo(pMiniMd->FindConstantHelper(fd, &rid));

    if (pchValue)
      *pchValue = 0;

    if (InvalidRid(rid)) {
      // There is no constant value associate with it
      if (pdwCPlusTypeFlag)
        *pdwCPlusTypeFlag = ELEMENT_TYPE_VOID;

      if (ppValue)
        *ppValue = NULL;
    } else {
      ConstantRec *pConstantRec;
      IfFailGo(m_pStgdb->m_MiniMd.GetConstantRecord(rid, &pConstantRec));
      DWORD dwType;

      // get the type of constant value
      dwType = pMiniMd->getTypeOfConstant(pConstantRec);
      if (pdwCPlusTypeFlag)
        *pdwCPlusTypeFlag = dwType;

      // get the value blob
      if (ppValue != NULL) {
        IfFailGo(pMiniMd->getValueOfConstant(pConstantRec,
                                             (const BYTE **)ppValue, &cbValue));
        if (pchValue && dwType == ELEMENT_TYPE_STRING)
          *pchValue = cbValue / sizeof(WCHAR);
      }
    }
  }
  // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten
  // with S_OK
  if (szField || pchField) {
    IfFailGo(pMiniMd->getNameOfField(pFieldRec, szField, cchField, pchField));
  }

ErrExit:

  return hr;
}

HRESULT RegMeta::GetPropertyProps( // S_OK, S_FALSE, or error.
    mdProperty prop,               // [IN] property token
    mdTypeDef *pClass,   // [OUT] typedef containing the property declarion.
    LPCWSTR szProperty,  // [OUT] Property name
    ULONG cchProperty,   // [IN] the count of wchar of szProperty
    ULONG *pchProperty,  // [OUT] actual count of wchar for property name
    DWORD *pdwPropFlags, // [OUT] property flags.
    PCCOR_SIGNATURE
        *ppvSig,  // [OUT] property type. pointing to meta data internal blob
    ULONG *pbSig, // [OUT] count of bytes in *ppvSig
    DWORD
        *pdwCPlusTypeFlag, // [OUT] flag for value type. selected ELEMENT_TYPE_*
    UVCP_CONSTANT *ppDefaultValue, // [OUT] constant value
    ULONG *pchDefaultValue, // [OUT] size of constant string in chars, 0 for
                            // non-strings.
    mdMethodDef *pmdSetter, // [OUT] setter method of the property
    mdMethodDef *pmdGetter, // [OUT] getter method of the property
    mdMethodDef rmdOtherMethod[], // [OUT] other method of the property
    ULONG cMax,                   // [IN] size of rmdOtherMethod
    ULONG *pcOtherMethod) // [OUT] total number of other method of this property
{
  HRESULT hr = NOERROR;

  CMiniMdRW *pMiniMd;
  PropertyRec *pRec;
  HENUMInternal hEnum;

  _ASSERTE(TypeFromToken(prop) == mdtProperty);

  pMiniMd = &(m_pStgdb->m_MiniMd);

  HENUMInternal::ZeroEnum(&hEnum);
  IfFailGo(pMiniMd->GetPropertyRecord(RidFromToken(prop), &pRec));

  if (pClass) {
    // find the property map entry corresponding to this property
    IfFailGo(pMiniMd->FindParentOfPropertyHelper(prop, pClass));
  }
  if (pdwPropFlags) {
    *pdwPropFlags = pMiniMd->getPropFlagsOfProperty(pRec);
  }
  if (ppvSig || pbSig) {
    // caller wants the signature
    //
    ULONG cbSig;
    PCCOR_SIGNATURE pvSig;
    IfFailGo(pMiniMd->getTypeOfProperty(pRec, &pvSig, &cbSig));
    if (ppvSig) {
      *ppvSig = pvSig;
    }
    if (pbSig) {
      *pbSig = cbSig;
    }
  }
  if (pdwCPlusTypeFlag || ppDefaultValue || pchDefaultValue) {
    // get the constant value
    ULONG cbValue;
    RID rid;
    IfFailGo(pMiniMd->FindConstantHelper(prop, &rid));

    if (pchDefaultValue)
      *pchDefaultValue = 0;

    if (InvalidRid(rid)) {
      // There is no constant value associate with it
      if (pdwCPlusTypeFlag)
        *pdwCPlusTypeFlag = ELEMENT_TYPE_VOID;

      if (ppDefaultValue)
        *ppDefaultValue = NULL;
    } else {
      ConstantRec *pConstantRec;
      IfFailGo(m_pStgdb->m_MiniMd.GetConstantRecord(rid, &pConstantRec));
      DWORD dwType;

      // get the type of constant value
      dwType = pMiniMd->getTypeOfConstant(pConstantRec);
      if (pdwCPlusTypeFlag)
        *pdwCPlusTypeFlag = dwType;

      // get the value blob
      if (ppDefaultValue != NULL) {
        IfFailGo(pMiniMd->getValueOfConstant(
            pConstantRec, (const BYTE **)ppDefaultValue, &cbValue));
        if (pchDefaultValue && dwType == ELEMENT_TYPE_STRING)
          *pchDefaultValue = cbValue / sizeof(WCHAR);
      }
    }
  }
  {
    MethodSemanticsRec *pSemantics;
    RID ridCur;
    ULONG cCurOtherMethod = 0;
    ULONG ulSemantics;
    mdMethodDef tkMethod;

    // initialize output parameters
    if (pmdSetter)
      *pmdSetter = mdMethodDefNil;
    if (pmdGetter)
      *pmdGetter = mdMethodDefNil;

    IfFailGo(pMiniMd->FindMethodSemanticsHelper(prop, &hEnum));
    while (HENUMInternal::EnumNext(&hEnum, (mdToken *)&ridCur)) {
      IfFailGo(pMiniMd->GetMethodSemanticsRecord(ridCur, &pSemantics));
      ulSemantics = pMiniMd->getSemanticOfMethodSemantics(pSemantics);
      tkMethod = TokenFromRid(pMiniMd->getMethodOfMethodSemantics(pSemantics),
                              mdtMethodDef);
      switch (ulSemantics) {
      case msSetter:
        if (pmdSetter)
          *pmdSetter = tkMethod;
        break;
      case msGetter:
        if (pmdGetter)
          *pmdGetter = tkMethod;
        break;
      case msOther:
        if (cCurOtherMethod < cMax)
          rmdOtherMethod[cCurOtherMethod] = tkMethod;
        cCurOtherMethod++;
        break;
      default:
        _ASSERTE(!"BadKind!");
      }
    }

    // set the output parameter
    if (pcOtherMethod)
      *pcOtherMethod = cCurOtherMethod;
  }
  // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten
  // with S_OK
  if (szProperty || pchProperty) {
    IfFailGo(pMiniMd->getNameOfProperty(pRec, (LPWSTR)szProperty, cchProperty,
                                        pchProperty));
  }

ErrExit:
  HENUMInternal::ClearEnum(&hEnum);
  return hr;
}

HRESULT RegMeta::GetParamProps( // S_OK or error.
    mdParamDef pd,              // [IN]The Parameter.
    mdMethodDef *pmd,           // [OUT] Parent Method token.
    ULONG *pulSequence,         // [OUT] Parameter sequence.
    __out_ecount_part_opt(cchName, *pchName)
        LPWSTR szName, // [OUT] Put name here.
    ULONG cchName,     // [OUT] Size of name buffer.
    ULONG *pchName,    // [OUT] Put actual size of name here.
    DWORD *pdwAttr,    // [OUT] Put flags here.
    DWORD *
        pdwCPlusTypeFlag, // [OUT] Flag for value type. selected ELEMENT_TYPE_*.
    UVCP_CONSTANT *ppValue, // [OUT] Constant value.
    ULONG
        *pchValue) // [OUT] size of constant string in chars, 0 for non-strings.
{
  HRESULT hr = NOERROR;

  ParamRec *pParamRec;
  CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

  _ASSERTE(TypeFromToken(pd) == mdtParamDef);

  IfFailGo(pMiniMd->GetParamRecord(RidFromToken(pd), &pParamRec));

  if (pmd) {
    IfFailGo(pMiniMd->FindParentOfParamHelper(pd, pmd));
    _ASSERTE(TypeFromToken(*pmd) == mdtMethodDef);
  }
  if (pulSequence)
    *pulSequence = pMiniMd->getSequenceOfParam(pParamRec);
  if (pdwAttr) {
    *pdwAttr = pMiniMd->getFlagsOfParam(pParamRec);
  }
  if (pdwCPlusTypeFlag || ppValue || pchValue) {
    // get the constant value
    ULONG cbValue;
    RID rid;
    IfFailGo(pMiniMd->FindConstantHelper(pd, &rid));

    if (pchValue)
      *pchValue = 0;

    if (InvalidRid(rid)) {
      // There is no constant value associate with it
      if (pdwCPlusTypeFlag)
        *pdwCPlusTypeFlag = ELEMENT_TYPE_VOID;

      if (ppValue)
        *ppValue = NULL;
    } else {
      ConstantRec *pConstantRec;
      IfFailGo(m_pStgdb->m_MiniMd.GetConstantRecord(rid, &pConstantRec));
      DWORD dwType;

      // get the type of constant value
      dwType = pMiniMd->getTypeOfConstant(pConstantRec);
      if (pdwCPlusTypeFlag)
        *pdwCPlusTypeFlag = dwType;

      // get the value blob
      if (ppValue != NULL) {
        IfFailGo(pMiniMd->getValueOfConstant(pConstantRec,
                                             (const BYTE **)ppValue, &cbValue));
        if (pchValue && dwType == ELEMENT_TYPE_STRING)
          *pchValue = cbValue / sizeof(WCHAR);
      }
    }
  }
  // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten
  // with S_OK
  if (szName || pchName)
    IfFailGo(pMiniMd->getNameOfParam(pParamRec, szName, cchName, pchName));

ErrExit:

  return hr;
}

HRESULT RegMeta::GetAssemblyFromScope(mdAssembly *ptkAssembly) {
  HRESULT hr = NOERROR;
  CMiniMdRW *pMiniMd = NULL;

  _ASSERTE(ptkAssembly);

  pMiniMd = &(m_pStgdb->m_MiniMd);
  if (pMiniMd->getCountAssemblys()) {
    *ptkAssembly = TokenFromRid(1, mdtAssembly);
  } else {
    IfFailGo(CLDB_E_RECORD_NOTFOUND);
  }
ErrExit:
  END_ENTRYPOINT_NOTHROW;
  return hr;
}

HRESULT RegMeta::GetCustomAttributeByName( // S_OK or error.
    mdToken tkObj,                         // [IN] Object with Custom Attribute.
    LPCWSTR wzName,      // [IN] Name of desired Custom Attribute.
    const void **ppData, // [OUT] Put pointer to data here.
    ULONG *pcbData)      // [OUT] Put size of data here.
{
  HRESULT hr; // A result.

  LPUTF8 szName; // Name in UFT8.
  int iLen;      // A length.
  CMiniMdRW *pMiniMd = NULL;

  pMiniMd = &(m_pStgdb->m_MiniMd);

  iLen = WszWideCharToMultiByte(CP_UTF8, 0, wzName, -1, NULL, 0, 0, 0);
  szName = (LPUTF8)_alloca(iLen);
  VERIFY(WszWideCharToMultiByte(CP_UTF8, 0, wzName, -1, szName, iLen, 0, 0));

  hr = ImportHelper::GetCustomAttributeByName(pMiniMd, tkObj, szName, ppData,
                                              pcbData);

  return hr;
}

BOOL RegMeta::IsValidToken( // True or False.
    mdToken tk)             // [IN] Given token.
{
  BOOL fRet = FALSE;
  HRESULT hr = S_OK;

  // If acquiring the lock failed...
  IfFailGo(hr);

  fRet = m_pStgdb->m_MiniMd._IsValidToken(tk);

ErrExit:
  return fRet;
}

HRESULT RegMeta::GetNestedClassProps( // S_OK or error.
    mdTypeDef tdNestedClass,          // [IN] NestedClass token.
    mdTypeDef *ptdEnclosingClass)     // [OUT] EnclosingClass token.
{
  HRESULT hr = NOERROR;

  BEGIN_ENTRYPOINT_NOTHROW;

  NestedClassRec *pRecord;
  ULONG iRecord;
  CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

  // If not a typedef -- return error.
  if (TypeFromToken(tdNestedClass) != mdtTypeDef) {
    IfFailGo(META_E_INVALID_TOKEN_TYPE); // PostError(META_E_INVALID_TOKEN_TYPE,
                                         // tdNestedClass));
  }

  _ASSERTE(TypeFromToken(tdNestedClass) && !IsNilToken(tdNestedClass) &&
           ptdEnclosingClass);

  IfFailGo(pMiniMd->FindNestedClassHelper(tdNestedClass, &iRecord));

  if (InvalidRid(iRecord)) {
    hr = CLDB_E_RECORD_NOTFOUND;
    goto ErrExit;
  }

  IfFailGo(pMiniMd->GetNestedClassRecord(iRecord, &pRecord));

  _ASSERTE(tdNestedClass == pMiniMd->getNestedClassOfNestedClass(pRecord));
  *ptdEnclosingClass = pMiniMd->getEnclosingClassOfNestedClass(pRecord);

ErrExit:
  return hr;
}

HRESULT RegMeta::GetNativeCallConvFromSig( // S_OK or error.
    void const *pvSig,                     // [IN] Pointer to signature.
    ULONG cbSig,                           // [IN] Count of signature bytes.
    ULONG *pCallConv) // [OUT] Put calling conv here (see CorPinvokemap).
{
  LOG((LF_CORDB, LL_INFO100000,
       "CordbSymbol - GetNativeCallConvFromSig - NOT IMPLEMENTED\n"));
  return S_OK;
}

HRESULT RegMeta::IsGlobal( // S_OK or error.
    mdToken pd,            // [IN] Type, Field, or Method token.
    int *pbGlobal)         // [OUT] Put 1 if global, 0 otherwise.

{
  LOG((LF_CORDB, LL_INFO100000, "CordbSymbol - IsGlobal - NOT IMPLEMENTED\n"));
  return S_OK;
}
