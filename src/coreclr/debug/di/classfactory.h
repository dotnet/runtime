// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// ClassFactory.h
//

//
// Class factories are used by the pluming in COM to activate new objects.
// This module contains the class factory code to instantiate the debugger
// objects described in RSPriv.h.
//
//*****************************************************************************
#ifndef __ClassFactory__h__
#define __ClassFactory__h__

#include "rspriv.h"


// This typedef is for a function which will create a new instance of an object.
typedef HRESULT (STDMETHODCALLTYPE * PFN_CREATE_OBJ)(REFIID riid, void **ppvObject);


//*****************************************************************************
// One class factory object satifies all of our clsid's, to reduce overall
// code bloat.
//*****************************************************************************
class CClassFactory :
	public IClassFactory
{
	CClassFactory() { }						// Can't use without data.

public:
	CClassFactory(PFN_CREATE_OBJ pfnCreateObject)
		: m_cRef(1), m_pfnCreateObject(pfnCreateObject)
	{ }

	virtual ~CClassFactory() {}

	//
	// IUnknown methods.
	//

    virtual HRESULT STDMETHODCALLTYPE QueryInterface(
        REFIID		riid,
        void		**ppvObject);

    virtual ULONG STDMETHODCALLTYPE AddRef()
	{
		return (InterlockedIncrement(&m_cRef));
	}

    virtual ULONG STDMETHODCALLTYPE Release()
	{
		LONG cRef = InterlockedDecrement(&m_cRef);
		if (cRef <= 0)
			delete this;
		return (cRef);
	}


	//
	// IClassFactory methods.
	//

    virtual HRESULT STDMETHODCALLTYPE CreateInstance(
        IUnknown	*pUnkOuter,
        REFIID		riid,
        void		**ppvObject);

    virtual HRESULT STDMETHODCALLTYPE LockServer(
        BOOL		fLock);


private:
    LONG        m_cRef;                     // Reference count.
    PFN_CREATE_OBJ m_pfnCreateObject;       // Creation function for an instance.
};



#endif // __ClassFactory__h__
