// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __CACHEDINTERFACEDISPATCHPAL_H__
#define __CACHEDINTERFACEDISPATCHPAL_H__

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

extern "C" void RhpInterfaceDispatch();

// One of these is allocated per interface call site. Call sites pass its address to RhpInterfaceDispatch.
//
// The first two fields are a monomorphic cache read by RhpInterfaceDispatch: if the MethodTable of the
// object matches m_pMethodTable, the call goes straight to m_pCode. The writer stores m_pCode first and
// publishes m_pMethodTable with release semantics, so a reader that observes m_pMethodTable also observes
// the matching m_pCode. All other (cell, MethodTable) pairs are cached in a global hashtable.
//
// m_token holds the interface contract (the interface type and slot being called), encoded with
// DispatchToken::ToCachedInterfaceDispatchToken so that it is never zero. Cells in ReadyToRun images
// start out with m_token == 0, the token is decoded from the import signature on the first call.
struct InterfaceDispatchCell
{
    Volatile<TADDR>  m_pMethodTable;
    PCODE            m_pCode;
    Volatile<TADDR>  m_token;

    void Initialize(DispatchToken token)
    {
        LIMITED_METHOD_CONTRACT;
        m_pMethodTable = 0;
        m_pCode = 0;
        m_token = DispatchToken::ToCachedInterfaceDispatchToken(token);
    }

    bool HasToken() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_token != 0;
    }

    DispatchToken GetToken() const
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasToken());
        return DispatchToken::FromCachedInterfaceDispatchToken(m_token);
    }

    void SetToken(DispatchToken token)
    {
        LIMITED_METHOD_CONTRACT;
        m_token = DispatchToken::ToCachedInterfaceDispatchToken(token);
    }
};

#ifndef DACCESS_COMPILE
// Decodes the method called through a dispatch cell in a ReadyToRun image from the import signature of the cell.
// Returns false if the method is not a vtable method in the version of the code we are running against, in which
// case *ppNonVirtualMD is set instead of *pToken.
bool DecodeReadyToRunDispatchCell(TADDR pCell, DispatchToken * pToken, MethodDesc ** ppNonVirtualMD);
#endif // !DACCESS_COMPILE

#endif // FEATURE_CACHED_INTERFACE_DISPATCH

#endif // __CACHEDINTERFACEDISPATCHPAL_H__
