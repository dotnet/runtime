// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// RegMeta.cpp
//

//
// Implementation for meta data public interface methods.
//
//*****************************************************************************
#include "stdafx.h"
#include "regmeta.h"
#include "metadata.h"
#include "corerror.h"
#include "mdutil.h"
#include "rwutil.h"
#include "mdlog.h"
#include "importhelper.h"
#include "filtermanager.h"
#include "switches.h"
#include "posterror.h"
#include "stgio.h"
#include "sstring.h"

#include <metamodelrw.h>

#define DEFINE_CUSTOM_NODUPCHECK    1
#define DEFINE_CUSTOM_DUPCHECK      2
#define SET_CUSTOM                  3

#if defined(_DEBUG) && defined(_TRACE_REMAPS)
#define LOGGING
#endif
#include <log.h>

#ifdef _MSC_VER
#pragma warning(disable: 4102)
#endif

#ifdef FEATURE_METADATA_EMIT

//*****************************************************************************
// Merge the pImport scope to this scope
//*****************************************************************************
STDMETHODIMP RegMeta::Merge(            // S_OK or error.
    IMetaDataImport *pImport,           // [IN] The scope to be merged.
    IMapToken   *pHostMapToken,         // [IN] Host IMapToken interface to receive token remap notification
    IUnknown    *pHandler)              // [IN] An object to receive to receive error notification.
{
    return E_NOTIMPL;
} // RegMeta::Merge


//*****************************************************************************
// real merge takes place here
//*****************************************************************************
STDMETHODIMP RegMeta::MergeEnd()        // S_OK or error.
{
    return E_NOTIMPL;
} // RegMeta::MergeEnd


//*****************************************************************************
// As the Stgdb object to get the save size for the metadata delta.
//*****************************************************************************
STDMETHODIMP RegMeta::GetDeltaSaveSize(      // S_OK or error.
    CorSaveSize fSave,                  // [IN] cssAccurate or cssQuick.
    DWORD       *pdwSaveSize)           // [OUT] Put the size here.
{
#ifdef FEATURE_METADATA_EMIT_ALL
    HRESULT hr = S_OK;

    // Make sure we're in EnC mode
    if (!IsENCOn())
    {
        _ASSERTE(!"Not in EnC mode!");
        IfFailGo(META_E_NOT_IN_ENC_MODE);
    }

    m_pStgdb->m_MiniMd.EnableDeltaMetadataGeneration();
    hr = GetSaveSize(fSave, pdwSaveSize);
    m_pStgdb->m_MiniMd.DisableDeltaMetadataGeneration();

ErrExit:
    return hr;
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::GetDeltaSaveSize

//*****************************************************************************
// Saves a metadata delta to a file of a given name.
//*****************************************************************************
STDMETHODIMP RegMeta::SaveDelta(                     // S_OK or error.
    LPCWSTR     szFile,                 // [IN] The filename to save to.
    DWORD       dwSaveFlags)            // [IN] Flags for the save.
{
#ifdef FEATURE_METADATA_EMIT_ALL
    HRESULT hr = S_OK;

    // Make sure we're in EnC mode
    if (!IsENCOn())
    {
        _ASSERTE(!"Not in EnC mode!");
        IfFailGo(META_E_NOT_IN_ENC_MODE);
    }

    m_pStgdb->m_MiniMd.EnableDeltaMetadataGeneration();
    hr = Save(szFile, dwSaveFlags);
    m_pStgdb->m_MiniMd.DisableDeltaMetadataGeneration();

ErrExit:
    return hr;
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::SaveDelta

//*****************************************************************************
// Saves a metadata delta to a stream.
//*****************************************************************************
STDMETHODIMP RegMeta::SaveDeltaToStream(     // S_OK or error.
    IStream     *pIStream,              // [IN] A writable stream to save to.
    DWORD       dwSaveFlags)            // [IN] Flags for the save.
{
#ifdef FEATURE_METADATA_EMIT_ALL
    HRESULT hr = S_OK;

    // Make sure we're in EnC mode
    if (!IsENCOn())
    {
        _ASSERTE(!"Not in EnC mode!");
        IfFailGo(META_E_NOT_IN_ENC_MODE);
    }

    m_pStgdb->m_MiniMd.EnableDeltaMetadataGeneration();
    hr = SaveToStream(pIStream, dwSaveFlags);
    m_pStgdb->m_MiniMd.DisableDeltaMetadataGeneration();

ErrExit:
    return hr;
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::SaveDeltaToStream

//*****************************************************************************
// Saves a copy of the scope into the memory buffer provided.  The buffer size
// must be at least as large as the GetSaveSize value.
//*****************************************************************************
STDMETHODIMP RegMeta::SaveDeltaToMemory(           // S_OK or error.
    void        *pbData,                // [OUT] Location to write data.
    ULONG       cbData)                 // [IN] Max size of data buffer.
{
#ifdef FEATURE_METADATA_EMIT_ALL
    // Make sure we're in EnC mode
    if (!IsENCOn())
    {
        _ASSERTE(!"Not in EnC mode!");
        return META_E_NOT_IN_ENC_MODE;
    }

    m_pStgdb->m_MiniMd.EnableDeltaMetadataGeneration();
    HRESULT hr = SaveToMemory(pbData, cbData);
    m_pStgdb->m_MiniMd.DisableDeltaMetadataGeneration();

    return hr;
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::SaveDeltaToMemory

//*****************************************************************************
// Resets the current edit and continue session
//
// Implements public API code:IMetaDataEmit2::ResetENCLog.
//*****************************************************************************
STDMETHODIMP
RegMeta::ResetENCLog()
{
#ifdef FEATURE_METADATA_EMIT_ALL
    HRESULT hr = S_OK;

    // Make sure we're in EnC mode
    if (!IsENCOn())
    {
        _ASSERTE(!"Not in EnC mode!");
        IfFailGo(META_E_NOT_IN_ENC_MODE);
    }

    IfFailGo(m_pStgdb->m_MiniMd.ResetENCLog());
ErrExit:
    return hr;
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::ResetENCLog

#endif //FEATURE_METADATA_EMIT
