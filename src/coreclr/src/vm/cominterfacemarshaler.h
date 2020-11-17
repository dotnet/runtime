// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: ComInterfaceMarshaler.h
//

//


#ifndef _H_COMInterfaceMarshaler_
#define _H_COMInterfaceMarshaler_

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

//--------------------------------------------------------------------------------
//  class ComInterfaceMarshaler
//--------------------------------------------------------------------------------
class COMInterfaceMarshaler
{
public:
    COMInterfaceMarshaler();
    virtual ~COMInterfaceMarshaler();

    VOID Init(IUnknown* pUnk, MethodTable* pClassMT, Thread *pThread, DWORD flags = 0); // see RCW::CreationFlags

    OBJECTREF FindOrCreateObjectRef(IUnknown **ppIncomingIP, MethodTable *pIncomingItfMT = NULL);
    OBJECTREF FindOrCreateObjectRef(IUnknown *pIncomingIP, MethodTable *pIncomingItfMT = NULL);

    VOID InitializeExistingComObject(OBJECTREF *pComObj, IUnknown **ppIncomingIP);

private:
    VOID InitializeObjectClass(IUnknown *pIncomingIP);
    OBJECTREF FindOrCreateObjectRefInternal(IUnknown **ppIncomingIP, MethodTable *pIncomingItfMT, bool bIncomingIPAddRefed);
    VOID      CreateObjectRef(BOOL fDuplicate, OBJECTREF *pComObj, IUnknown **ppIncomingIP, MethodTable *pIncomingItfMT, bool bIncomingIPAddRefed);
    static VOID      EnsureCOMInterfacesSupported(OBJECTREF oref, MethodTable* pClassMT);

    inline bool NeedUniqueObject();

    RCWCache*               m_pWrapperCache;    // initialization info
    IUnknown*               m_pUnknown;         // NOT AddRef'ed
    IUnknown*               m_pIdentity;        // NOT AddRef'ed
    TypeHandle              m_typeHandle;       // inited and computed if inited value is NULL.  Need to represent all array information too.
    TypeHandle              m_itfTypeHandle;    // an interface supported by the object as returned from GetRuntimeClassName
    Thread*                 m_pThread;          // Current thread - avoid calling GetThread multiple times
    DWORD                   m_flags;
};


#endif // #ifndef _H_COMInterfaceMarshaler_
