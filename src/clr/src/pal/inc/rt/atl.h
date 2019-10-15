// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// ===========================================================================
// File:  atl.h
// 
// ===========================================================================

#ifndef __ATL_H__
#define __ATL_H__

#include "ole2.h"
/////////////////////////////////////////////////////////////////////////////
// COM Smart pointers

template <class T>
class _NoAddRefReleaseOnCComPtr : public T
{
	private:
		STDMETHOD_(ULONG, AddRef)()=0;
		STDMETHOD_(ULONG, Release)()=0;
};

//CComPtrBase provides the basis for all other smart pointers
//The other smartpointers add their own constructors and operators
template <class T>
class CComPtrBase
{
protected:
	CComPtrBase()
	{
		p = NULL;
	}
	CComPtrBase(int nNull)
	{
		(void)nNull;
		p = NULL;
	}
	CComPtrBase(T* lp)
	{
		p = lp;
		if (p != NULL)
			p->AddRef();
	}
public:
	typedef T _PtrClass;
	~CComPtrBase()
	{
		if (p)
			p->Release();
	}
	operator T*() const
	{
		return p;
	}
	T& operator*() const
	{
		return *p;
	}
	T** operator&()
	{
		return &p;
	}
	_NoAddRefReleaseOnCComPtr<T>* operator->() const
	{
		return (_NoAddRefReleaseOnCComPtr<T>*)p;
	}
	bool operator!() const
	{
		return (p == NULL);
	}
	bool operator<(T* pT) const
	{
		return p < pT;
	}
	bool operator==(T* pT) const
	{
		return p == pT;
	}

	// Release the interface and set to NULL
	void Release()
	{
		T* pTemp = p;
		if (pTemp)
		{
			p = NULL;
			pTemp->Release();
		}
	}
	// Attach to an existing interface (does not AddRef)
	void Attach(T* p2)
	{
		if (p)
			p->Release();
		p = p2;
	}
	// Detach the interface (does not Release)
	T* Detach()
	{
		T* pt = p;
		p = NULL;
		return pt;
	}
	HRESULT CopyTo(T** ppT)
	{
		if (ppT == NULL)
			return E_POINTER;
		*ppT = p;
		if (p)
			p->AddRef();
		return S_OK;
	}

    T* p;
};

template <class T>
class CComPtr : public CComPtrBase<T>
{
public:
	CComPtr()
	{
	}
	CComPtr(int nNull) :
		CComPtrBase<T>(nNull)
	{
	}
	CComPtr(T* lp) :
		CComPtrBase<T>(lp)

	{
	}
	CComPtr(const CComPtr<T>& lp) :
		CComPtrBase<T>(lp.p)
	{
	}
	T* operator=(T* lp)
	{
		return static_cast<T*>(AtlComPtrAssign((IUnknown**)&this->p, lp));
	}
    T* operator=(const CComPtr<T>& lp)
	{
		return static_cast<T*>(AtlComPtrAssign((IUnknown**)&this->p, lp));
	}
};

#define IUNKNOWN_METHODS \
private: ULONG m_dwRef; \
public: \
    virtual ULONG STDMETHODCALLTYPE AddRef( void) { \
	return (ULONG)InterlockedIncrement((LONG*)&m_dwRef); } \
    virtual ULONG STDMETHODCALLTYPE Release( void) { \
	ULONG new_ref = (ULONG)InterlockedDecrement((LONG*)&m_dwRef); \
	if (new_ref == 0) { delete this; return 0; } return new_ref; } \


#define BEGIN_COM_MAP(t) \
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppvObject) \
	{ \
		if (ppvObject == NULL) \
		{ \
			return E_POINTER; \
		}

#define COM_INTERFACE_ENTRY(i) \
		if (riid == IID_##i) \
		{ \
			*ppvObject = (i*)this; \
                        this->AddRef(); \
			return S_OK; \
		}

#define END_COM_MAP() \
		return E_NOINTERFACE; \
	} \
	virtual ULONG STDMETHODCALLTYPE AddRef( void) = 0; \
	virtual ULONG STDMETHODCALLTYPE Release( void) = 0;



template <const IID* piid>
class ISupportErrorInfoImpl : public ISupportErrorInfo
{
public:
	STDMETHOD(InterfaceSupportsErrorInfo)(REFIID riid)
	{
		return (riid == *piid) ? S_OK : S_FALSE;
	}
};

inline IUnknown* AtlComPtrAssign(IUnknown** pp, IUnknown* lp)
{
	if (lp != NULL)
		lp->AddRef();
	if (*pp)
		(*pp)->Release();
	*pp = lp;
	return lp;
}

inline IUnknown* AtlComQIPtrAssign(IUnknown** pp, IUnknown* lp, REFIID riid)
{
	IUnknown* pTemp = *pp;
	*pp = NULL;
	if (lp != NULL)
		lp->QueryInterface(riid, (void**)pp);
	if (pTemp)
		pTemp->Release();
	return *pp;
}


class CComMultiThreadModelNoCS
{
public:
	static ULONG WINAPI Increment(LONG *p) {return InterlockedIncrement(p);}
	static ULONG WINAPI Decrement(LONG *p) {return InterlockedDecrement(p);}
};

//Base is the user's class that derives from CComObjectRoot and whatever
//interfaces the user wants to support on the object
template <class Base>
class CComObject : public Base
{
public:
	typedef Base _BaseClass;

	// Set refcount to -(LONG_MAX/2) to protect destruction and 
	// also catch mismatched Release in debug builds
	~CComObject()
	{
		this->m_dwRef = -(LONG_MAX/2);
	}
	//If InternalAddRef or InternalRelease is undefined then your class
	//doesn't derive from CComObjectRoot
	STDMETHOD_(ULONG, AddRef)() {return this->InternalAddRef();}
	STDMETHOD_(ULONG, Release)()
	{
		ULONG l = this->InternalRelease();
		if (l == 0)
			delete this;
		return l;
	}

	static HRESULT WINAPI CreateInstance(CComObject<Base>** pp);
};

template <class Base>
HRESULT WINAPI CComObject<Base>::CreateInstance(CComObject<Base>** pp)
{
	ATLASSERT(pp != NULL);
	if (pp == NULL)
		return E_POINTER;
	*pp = NULL;

	HRESULT hRes = E_OUTOFMEMORY;
	CComObject<Base>* p = NULL;
	p = new CComObject<Base>();
	if (p != NULL)
	{
        hRes = NOERROR;
	}
	*pp = p;
	return hRes;
}


// the functions in this class don't need to be virtual because
// they are called from CComObject
class CComObjectRootBase
{
public:
	CComObjectRootBase()
	{
		m_dwRef = 0L;
	}
public:
    LONG m_dwRef;
}; // CComObjectRootBase

template <class ThreadModel>
class CComObjectRootEx : public CComObjectRootBase
{
public:
	typedef ThreadModel _ThreadModel;

    ULONG InternalAddRef()
	{
		ATLASSERT(m_dwRef != -1L);
		return _ThreadModel::Increment(&m_dwRef);
	}
	ULONG InternalRelease()
	{
#ifdef _DEBUG
		LONG nRef = _ThreadModel::Decrement(&m_dwRef);
		if (nRef < -(LONG_MAX / 2))
		{
			ATLASSERT(0);
		}
		return nRef;
#else
		return _ThreadModel::Decrement(&m_dwRef);
#endif
	}
}; // CComObjectRootEx

typedef CComMultiThreadModelNoCS CComObjectThreadModel;

typedef CComObjectRootEx<CComObjectThreadModel> CComObjectRoot;

// dummy definitions for the ATL COM goo
#define DECLARE_NO_REGISTRY()

#define BEGIN_OBJECT_MAP(x) static const int x = 0;
#define OBJECT_ENTRY(clsid, class)
#define END_OBJECT_MAP()

template <class T, const CLSID* pclsid>
class CComCoClass {
};

class CComModule {
public:
    HINSTANCE m_hInst;
    HINSTANCE m_hInstResource;

    HRESULT Init(int objmap, HINSTANCE h)
    {
        m_hInst = h;
        return S_OK;
    }

    void Term()
    {
        m_hInst = NULL;
        m_hInstResource = NULL;
    }

    HINSTANCE GetModuleInstance()
    {
        return m_hInst;
    }

    HINSTANCE GetResourceInstance()
    {
        return m_hInstResource;
    }
};

template <class E>
class CAtlArray
{
private:    
    E     * m_pData;      // Elements of the array.
    size_t  m_nSize;      // Number of valid elements in the array.
    size_t  m_nMaxSize;   // Total number of elements m_pData buffer can hold.

    // Call the constructors for the nElements elements starting from pBeggingElement
    void CallConstructors( E* pBeginningElement, size_t nElements ) 
    {
        for( size_t iElement = 0; iElement < nElements; iElement++ )
        {
            ::new( this->pElements+iElement ) E;
        }
    }

    // Call the destructor for the nElements elements starting from pBeggingElement
    void CallDestructors( E* pBeginningElement, size_t nElements ) 
    {
        ATLASSERT(nElements == 0 ||
                  pBeginningElement + (nElements-1) < m_pData + m_nSize // Should not go beyond the valid element range.
                 );
        
        for( size_t iElement = 0; iElement < nElements; iElement++ )
        {
            pBeginningElement[iElement].~E();
#ifdef DEBUG
            // Put some garbage there. 
            // It would be 0xcccccccc if the element is a pointer. For easy debugging.
            memset(&pBeginningElement[iElement], 0xcc, sizeof(E));
#endif
        }
    }

    
public:
    CAtlArray() : m_pData(NULL), m_nSize(0), m_nMaxSize(0) {}
    ~CAtlArray() { RemoveAll(); }

    size_t GetCount() const
    { 
        return m_nSize; 
    }

    bool IsEmpty()
    {
        return m_nSize == 0;
    }

    void RemoveAll() 
    { 
        if (m_pData) 
        {
            CallDestructors( m_pData, m_nSize );
    		free( m_pData ); 
            m_pData    = NULL; 
            m_nSize    = 0; 
            m_nMaxSize = 0;             
        }

        ATLASSERT(m_pData    == NULL);
        ATLASSERT(m_nSize    == 0);
        ATLASSERT(m_nMaxSize == 0);
    }
    
    E& GetAt( size_t iElement ) 
    {
        ATLASSERT(iElement < m_nSize); 
        if (iElement >= m_nSize)
            AtlThrow(E_INVALIDARG);
        return (m_pData[iElement]);
    }

    E& operator[]( size_t iElement )
    {
        return GetAt(iElement);
    }

    E* GetData()
    {
        return (m_pData);
    }

    void SetCount( size_t nNewSize )
    {
        if ( nNewSize == 0 )
        {
            RemoveAll();
        }
        else
        if ( nNewSize <= m_nSize )
        {
            CallDestructors( m_pData+nNewSize, m_nSize-nNewSize );
            m_nSize = nNewSize;
        }
        else
        if ( nNewSize > m_nSize )
        {
    		bool bSuccess = GrowBuffer( nNewSize );
    		if( !bSuccess )
    		{
    			AtlThrow( E_OUTOFMEMORY );
    		}

            CallDestructors( m_pData+m_nSize, nNewSize-m_nSize );
            m_nSize = nNewSize;
        }
    }

    bool GrowBuffer( size_t nNewMaxSize )
    {
        if( nNewMaxSize > m_nMaxSize )
        {                
            E* pNewData = static_cast< E* >( malloc( nNewMaxSize*sizeof( E ) ) );
            if( pNewData == NULL )
            {
                return false;
            }

            // Ok, allocation succeeded.

            if (m_pData == NULL)
            {
                // First time allocation. Simply return the newly allocated buffer.
                goto DoneNewBuffer;
                
            }

            // copy new data from old
            memmove( pNewData, m_pData, m_nSize*sizeof( E ));
            
            // get rid of old stuff 
            // (note: no need to call the destructors, because the elements are still alive
            //  in the new array.)
			free( m_pData );
            
DoneNewBuffer:            
            m_pData = pNewData;
            m_nMaxSize = nNewMaxSize;
        }
    
        return true;
    }   

    size_t Add( E element )
    {
    	size_t iElement = m_nSize;
        
    	if( iElement >= m_nMaxSize )
    	{
        	size_t nNewMaxSize = m_nMaxSize * 2;
            nNewMaxSize = (nNewMaxSize >= 16)?nNewMaxSize:16;  // Let's allocate at least 16 elements.
            if (nNewMaxSize < m_nMaxSize)
                AtlThrow( E_OUTOFMEMORY );  // Integer overflow
                
            if (iElement >= nNewMaxSize)
            {
                nNewMaxSize = iElement + 1;    
                if (nNewMaxSize<iElement)
                    AtlThrow( E_OUTOFMEMORY );  // Integer overflow
            }
                
    		bool bSuccess = GrowBuffer( nNewMaxSize );
    		if( !bSuccess )
    		{
    			AtlThrow( E_OUTOFMEMORY );
    		}
    	}

        ATLASSERT(m_pData);
        ATLASSERT(iElement < m_nMaxSize); 
        new( m_pData+iElement ) E( element );

    	m_nSize++;

    	return( iElement );
    }
        
    // Remove "nElements" elements starting from the element at index "iElement"
    void RemoveAt( size_t iElement, size_t nElements = 1 )
    {    
    	ATLASSERT( (iElement+nElements) <= m_nSize );

    	if( (iElement+nElements) > m_nSize )
    		AtlThrow(E_INVALIDARG);		
    		
    	// just remove a range
    	size_t nMoveCount = m_nSize-(iElement+nElements);
    	CallDestructors( m_pData+iElement, nElements );
    	if( nMoveCount > 0 )
    	{
            memmove_s( m_pData+iElement, 
                       nMoveCount * sizeof( E ),
                       m_pData+(iElement+nElements),
    			       nMoveCount * sizeof( E )
    			     );
    	}
    	m_nSize -= nElements;
    }

}; // CAtlArray

#endif // __ATL_H__
