// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
#include "mdperf.h"
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
#ifdef FEATURE_METADATA_EMIT_ALL
    HRESULT hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    IMetaDataImport2 *pI2=NULL;

    LOG((LOGMD, "RegMeta::Merge(0x%08x, 0x%08x)\n", pImport, pHandler));
    START_MD_PERF();
    LOCKWRITE();

    IfFailGo(VerifyNotWinMD(pImport, "IMetaDataEmit::Merge(): merging with a .winmd file not supported."));

    IfFailGo(pImport->QueryInterface(IID_IMetaDataImport2, (void**)&pI2));
    m_hasOptimizedRefToDef = false;

    // track this import
    IfFailGo(  m_newMerger.AddImport(pI2, pHostMapToken, pHandler) );

ErrExit:
    if (pI2)
        pI2->Release();
    STOP_MD_PERF(Merge);
    END_ENTRYPOINT_NOTHROW;

    return (hr);
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::Merge


//*****************************************************************************
// real merge takes place here
//*****************************************************************************
STDMETHODIMP RegMeta::MergeEnd()        // S_OK or error.
{
#ifdef FEATURE_METADATA_EMIT_ALL
    HRESULT hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    LOG((LOGMD, "RegMeta::MergeEnd()\n"));
    START_MD_PERF();
    LOCKWRITE();
    // Merge happens here!!

    // <REVISIT_TODO>bug 16719.  Merge itself is doing a lots of small changes in literally
    // dozens of places.  It would be to hard to maintain and would cause code
    // bloat to auto-grow the tables.  So instead, we've opted to just expand
    // the world right away and avoid the trouble.</REVISIT_TODO>
    IfFailGo(m_pStgdb->m_MiniMd.ExpandTables());

    IfFailGo(m_newMerger.Merge(m_OptionValue.m_MergeOptions, m_OptionValue.m_RefToDefCheck) );

ErrExit:
    STOP_MD_PERF(MergeEnd);
    END_ENTRYPOINT_NOTHROW;

    return (hr);
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
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

    BEGIN_ENTRYPOINT_NOTHROW;

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
    END_ENTRYPOINT_NOTHROW;
    
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

    BEGIN_ENTRYPOINT_NOTHROW;

    
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

    END_ENTRYPOINT_NOTHROW;

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

    BEGIN_ENTRYPOINT_NOTHROW;

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
    END_ENTRYPOINT_NOTHROW;
    
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
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    // Make sure we're in EnC mode
    if (!IsENCOn())
    {
        _ASSERTE(!"Not in EnC mode!");
        IfFailGo(META_E_NOT_IN_ENC_MODE);
    }


    m_pStgdb->m_MiniMd.EnableDeltaMetadataGeneration();
    hr = SaveToMemory(pbData, cbData);
    m_pStgdb->m_MiniMd.DisableDeltaMetadataGeneration();

ErrExit:

    END_ENTRYPOINT_NOTHROW;
    
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

    BEGIN_ENTRYPOINT_NOTHROW;

    // Make sure we're in EnC mode
    if (!IsENCOn())
    {
        _ASSERTE(!"Not in EnC mode!");
        IfFailGo(META_E_NOT_IN_ENC_MODE);
    }

    IfFailGo(m_pStgdb->m_MiniMd.ResetENCLog());
ErrExit:
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::ResetENCLog

#ifdef FEATURE_METADATA_EMIT_ALL

// Helper for code:RegMeta::ProcessFilter
HRESULT RegMeta::ProcessFilterWorker()
{
    HRESULT hr = S_OK;

    CMiniMdRW       *pMiniMd;               // The MiniMd with the data.
    RegMeta         *pMetaNew = NULL;
    CMapToken       *pMergeMap = NULL;
    IMapToken       *pMapNew = NULL;
    MergeTokenManager *pCompositHandler = NULL;
    IMapToken       *pHostMapToken = NULL;

    // For convenience.
    pMiniMd = &(m_pStgdb->m_MiniMd);
    IfNullGo( pMiniMd->GetFilterTable() );
    _ASSERTE(pMiniMd->GetFilterTable()->Count() != 0); // caller verified this

    // Yes, client has used filter to specify what are the metadata needed.
    // We will create another instance of RegMeta and make this module an imported module
    // to be merged into the new RegMeta. We will provide the handler to track all of the token
    // movements. We will replace the merged light weight stgdb to this RegMeta..
    // Then we will need to fix up the MergeTokenManager with this new movement.
    // The reason that we decide to choose this approach is because it will be more complicated
    // and very likely less efficient to fix up the signature blob pool and then compact all of the pools!
    //

    // Create a new RegMeta.
    pMetaNew = new (nothrow) RegMeta();
    IfNullGo( pMetaNew );
    pMetaNew->AddRef();
    IfFailGo(pMetaNew->SetOption(&m_OptionValue));


    // Remember the open type.
    IfFailGo(pMetaNew->CreateNewMD());
    IfFailGo(pMetaNew->AddToCache());

    // Ignore the error return by setting handler
    hr = pMetaNew->SetHandler(m_pHandler);

    // create the IMapToken to receive token remap information from merge
    pMergeMap = new (nothrow) CMapToken;
    IfNullGo( pMergeMap );

    // use merge to filter out the unneeded data. But we need to keep COMType and also need to drop off the 
    // CustomAttributes that associated with MemberRef with parent MethodDef
    //
    pMetaNew->m_hasOptimizedRefToDef = false;
    IfFailGo( pMetaNew->m_newMerger.AddImport(this, pMergeMap, NULL) );
    IfFailGo( pMetaNew->m_pStgdb->m_MiniMd.ExpandTables());
    IfFailGo( pMetaNew->m_newMerger.Merge((MergeFlags)(MergeManifest | DropMemberRefCAs | NoDupCheck), MDRefToDefDefault) );

    // Now we need to recalculate the token movement
    // 
    if (m_newMerger.m_pImportDataList)
    {

        // This is the case the filter is applied to merged emit scope. We need calculate how this implicit merge
        // affects the original merge remap. Basically we need to walk all the m_pTkMapList in the merger and replace
        // the to token to the most recent to token.
        // 
        MDTOKENMAP          *pMDTokenMapList;

        pMDTokenMapList = m_newMerger.m_pImportDataList->m_pMDTokenMap;

        MDTOKENMAP          *pMap;
        TOKENREC            *pTKRec;
        ULONG               i;
        mdToken             tkFinalTo;
        ModuleRec           *pMod;
        ModuleRec           *pModNew;
        LPCUTF8             szName;

        // update each import map from merge to have the m_tkTo points to the final mapped to token
        for (pMap = pMDTokenMapList; pMap; pMap = pMap->m_pNextMap)
        {
            // update each record
            for (i = 0; i < (ULONG) (pMap->Count()); i++)
            {
                TOKENREC    *pRecTo;
                pTKRec = pMap->Get(i);
                if ( pMergeMap->Find( pTKRec->m_tkTo, &pRecTo ) )
                {
                    // This record is kept by the filter and the tkTo is changed
                    pRecTo->m_isFoundInImport = true;
                    tkFinalTo = pRecTo->m_tkTo;
                    pTKRec->m_tkTo = tkFinalTo;
                    pTKRec->m_isDeleted = false;

                    // send the notification now. Because after merge, we may have everything in order and 
                    // won't send another set of notification.
                    //
                    LOG((LOGMD, "TokenRemap in RegMeta::ProcessFilter (IMapToken 0x%08x): from 0x%08x to 0x%08x\n", pMap->m_pMap, pTKRec->m_tkFrom, pTKRec->m_tkTo));

                    pMap->m_pMap->Map(pTKRec->m_tkFrom, pTKRec->m_tkTo);
                }
                else
                {
                    // This record is pruned by the filter upon save
                    pTKRec->m_isDeleted = true;
                }
            }
        }

        // now walk the pMergeMap and check to see if there is any entry that is not set to true for m_isFoundInImport.
        // These are the records that from calling DefineXXX methods directly on the Emitting scope!
        if (m_pHandler)
            m_pHandler->QueryInterface(IID_IMapToken, (void **)&pHostMapToken);
        if (pHostMapToken)
        {
            for (i = 0; i < (ULONG) (pMergeMap->m_pTKMap->Count()); i++)
            {
                pTKRec = pMergeMap->m_pTKMap->Get(i);
                if (pTKRec->m_isFoundInImport == false)
                {
                    LOG((LOGMD, "TokenRemap in RegMeta::ProcessFilter (default IMapToken 0x%08x): from 0x%08x to 0x%08x\n", pHostMapToken, pTKRec->m_tkFrom, pTKRec->m_tkTo));

                    // send the notification on the IMapToken from SetHandler of this RegMeta
                    pHostMapToken->Map(pTKRec->m_tkFrom, pTKRec->m_tkTo);
                }
            }
        }

        // Preserve module name across merge.
        IfFailGo(m_pStgdb->m_MiniMd.GetModuleRecord(1, &pMod));
        IfFailGo(pMetaNew->m_pStgdb->m_MiniMd.GetModuleRecord(1, &pModNew));
        IfFailGo(m_pStgdb->m_MiniMd.getNameOfModule(pMod, &szName));
        IfFailGo(pMetaNew->m_pStgdb->m_MiniMd.PutString(TBL_Module, ModuleRec::COL_Name, pModNew, szName));

        // now swap the stgdb but keep the merger...
        _ASSERTE( !IsOfExternalStgDB(m_OpenFlags) );
        
        CLiteWeightStgdbRW * pStgdbTmp = m_pStgdb;
        m_pStgdb = pMetaNew->m_pStgdb;
        pMetaNew->m_pStgdb = pStgdbTmp;
        // Update RuntimeVersion string pointers to point to the owning RegMeta string (the strings are 2 copies of the same string content)
        m_pStgdb->m_MiniMd.m_OptionValue.m_RuntimeVersion = m_OptionValue.m_RuntimeVersion;
        pMetaNew->m_pStgdb->m_MiniMd.m_OptionValue.m_RuntimeVersion = pMetaNew->m_OptionValue.m_RuntimeVersion;
    }
    else
    {
        // swap the Stgdb
        CLiteWeightStgdbRW * pStgdbTmp = m_pStgdb;
        m_pStgdb = pMetaNew->m_pStgdb;
        pMetaNew->m_pStgdb = pStgdbTmp;
        // Update RuntimeVersion string pointers to point to the owning RegMeta string (the strings are 2 copies of the same string content)
        m_pStgdb->m_MiniMd.m_OptionValue.m_RuntimeVersion = m_OptionValue.m_RuntimeVersion;
        pMetaNew->m_pStgdb->m_MiniMd.m_OptionValue.m_RuntimeVersion = pMetaNew->m_OptionValue.m_RuntimeVersion;
        
        // Client either open an existing scope and apply the filter mechanism, or client define the scope and then
        // apply the filter mechanism.

        // In this case, host better has supplied the handler!!
        _ASSERTE( m_bRemap && m_pHandler);
        IfFailGo( m_pHandler->QueryInterface(IID_IMapToken, (void **) &pMapNew) );

        
        {
            // Send the notification of token movement now because after merge we may not move tokens again
            // and thus no token notification will be send.
            MDTOKENMAP      *pMap = pMergeMap->m_pTKMap;
            TOKENREC        *pTKRec;
            ULONG           i;

            for (i=0; i < (ULONG) (pMap->Count()); i++)
            {
                pTKRec = pMap->Get(i);
                pMap->m_pMap->Map(pTKRec->m_tkFrom, pTKRec->m_tkTo);
            }

        }


        // What we need to do here is create a IMapToken that will replace the original handler. This new IMapToken 
        // upon called will first map the from token to the most original from token.
        //
        pCompositHandler = new (nothrow) MergeTokenManager(pMergeMap->m_pTKMap, NULL);
        IfNullGo( pCompositHandler );

        // now update the following field to hold on to the real IMapToken supplied by our client by SetHandler
        if (pMergeMap->m_pTKMap->m_pMap)
            pMergeMap->m_pTKMap->m_pMap->Release();
        _ASSERTE(pMapNew);
        pMergeMap->m_pTKMap->m_pMap = pMapNew;

        // ownership transferred
        pMergeMap = NULL;
        pMapNew = NULL;
    
        // now you want to replace all of the IMapToken set by calling SetHandler to this new MergeTokenManager
        IfFailGo( m_pStgdb->m_MiniMd.SetHandler(pCompositHandler) );

        m_pHandler = pCompositHandler;

        // ownership transferred
        pCompositHandler = NULL;
    }

    // Force a ref to def optimization because the remap information was stored in the thrown away CMiniMdRW
    m_hasOptimizedRefToDef = false;
    IfFailGo( RefToDefOptimization() );

ErrExit:
    if (pHostMapToken)
        pHostMapToken->Release();
    if (pMetaNew) 
        pMetaNew->Release();
    if (pMergeMap)
        pMergeMap->Release();
    if (pCompositHandler)
        pCompositHandler->Release();
    if (pMapNew)
        pMapNew->Release();
    
    return hr;
} // RegMeta::ProcessFilter

#endif //FEATURE_METADATA_EMIT_ALL

#endif //FEATURE_METADATA_EMIT
