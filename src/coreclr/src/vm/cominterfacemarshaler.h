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

class ICOMInterfaceMarshalerCallback
{
public :
    // Callback to be called when we created a RCW and that RCW is inserted to cache
    virtual void OnRCWCreated(RCW *pRCW) = 0;

    // Callback to be called when we got back a RCW from the cache
    virtual void OnRCWCacheHit(RCW *pRCW) = 0;

    // Callback to be called to determine whether we should use this RCW
    // Return true if ComInterfaceMarshaler should use this RCW
    // Return false if ComInterfaceMarshaler should just skip this RCW and proceed
    // to create a duplicate one instead
    virtual bool ShouldUseThisRCW(RCW *pRCW) = 0;
};

//--------------------------------------------------------------------------------
//  class ComInterfaceMarshaler
//--------------------------------------------------------------------------------
class COMInterfaceMarshaler
{
public:
    COMInterfaceMarshaler();
    virtual ~COMInterfaceMarshaler();

    VOID Init(IUnknown* pUnk, MethodTable* pClassMT, Thread *pThread, DWORD flags = 0); // see RCW::CreationFlags

    // Sets a ICOMInterfaceMarshalerCallback pointer to be called when RCW is created or got back from cache
    // Note that caller owns the lifetime of this callback object, and needs to make sure this callback is
    // alive until the last time you call any function on COMInterfaceMarshaler
    VOID SetCallback(ICOMInterfaceMarshalerCallback *pCallback)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pCallback != NULL);
        m_pCallback = pCallback;
    }

    VOID InitializeObjectClass(IUnknown *pIncomingIP);

    OBJECTREF FindOrCreateObjectRef(IUnknown **ppIncomingIP, MethodTable *pIncomingItfMT = NULL);
    OBJECTREF FindOrCreateObjectRef(IUnknown *pIncomingIP, MethodTable *pIncomingItfMT = NULL);

    OBJECTREF WrapWithComObject();

    VOID InitializeExistingComObject(OBJECTREF *pComObj, IUnknown **ppIncomingIP);

private:
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

    ICOMInterfaceMarshalerCallback  *m_pCallback;        // Callback to call when we created a RCW or got back RCW from cache
};


#endif // #ifndef _H_COMInterfaceMarshaler_
