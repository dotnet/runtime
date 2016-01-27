// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: ClassFactory.h
//

// ===========================================================================

//*****************************************************************************
// ClassFactory.h
//
// Class factories are used by the pluming in COM to activate new objects.  
// This module contains the class factory code to instantiate the
// ISymUnmanaged Reader,Writer and Binder
//*****************************************************************************
#ifndef __ILDBClassFactory__h__
#define __ILDBClassFactory__h__

// This typedef is for a function which will create a new instance of an object.
typedef HRESULT (* PFN_CREATE_OBJ)(REFIID riid, void**ppvObject);

//*****************************************************************************
// This structure is used to declare a global list of coclasses.  The class
// factory object is created with a pointer to the correct one of these, so
// that when create instance is called, it can be created.
//*****************************************************************************
struct COCLASS_REGISTER
{
    const GUID *pClsid;                 // Class ID of the coclass.
    LPCWSTR     szProgID;               // Prog ID of the class.
    PFN_CREATE_OBJ pfnCreateObject;     // Creation function for an instance.
};



//*****************************************************************************
// One class factory object satifies all of our clsid's, to reduce overall 
// code bloat.
//*****************************************************************************
class CIldbClassFactory :
    public IClassFactory
{
    CIldbClassFactory() { }                     // Can't use without data.
    
public:
    CIldbClassFactory(const COCLASS_REGISTER *pCoClass)
        : m_cRef(1), m_pCoClass(pCoClass)
    { }

    virtual ~CIldbClassFactory() {}
    
    //
    // IUnknown methods.
    //

    virtual HRESULT STDMETHODCALLTYPE QueryInterface( 
        REFIID      riid,
        void        **ppvObject);
    
    virtual ULONG STDMETHODCALLTYPE AddRef()
    {
        return InterlockedIncrement(&m_cRef);
    }
    
    virtual ULONG STDMETHODCALLTYPE Release()
    {
        LONG        cRef = InterlockedDecrement(&m_cRef);
        if (cRef <= 0)
            DELETE(this);
        return (cRef);
    }


    //
    // IClassFactory methods.
    //

    virtual HRESULT STDMETHODCALLTYPE CreateInstance( 
        IUnknown    *pUnkOuter,
        REFIID      riid,
        void        **ppvObject);
    
    virtual HRESULT STDMETHODCALLTYPE LockServer( 
        BOOL        fLock);


private:
    LONG        m_cRef;                     // Reference count.
    const COCLASS_REGISTER *m_pCoClass;     // The class we belong to.
};



#endif // __ClassFactory__h__
