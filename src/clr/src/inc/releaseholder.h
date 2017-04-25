// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This class acts a smart pointer which calls the Release method on any object
// you place in it when the ReleaseHolder class falls out of scope.  You may use it
// just like you would a standard pointer to a COM object (including if (foo),
// if (!foo), if (foo == 0), etc) except for two caveats:
//     1. This class never calls AddRef and it always calls Release when it
//        goes out of scope.
//     2. You should never use & to try to get a pointer to a pointer unless
//        you call Release first, or you will leak whatever this object contains
//        prior to updating its internal pointer.
template<class T>
class ReleaseHolder
{
public:
    ReleaseHolder()
        : m_ptr(NULL)
    {}
    
    ReleaseHolder(T* ptr)
        : m_ptr(ptr)
    {}
    
    ~ReleaseHolder()
    {
        Release();
    }

    void operator=(T *ptr)
    {
        Release();

        m_ptr = ptr;
    }

    T* operator->()
    {
        return m_ptr;
    }

    operator T*()
    {
        return m_ptr;
    }

    T** operator&()
    {
        return &m_ptr;
    }

    T* GetPtr() const
    {
        return m_ptr;
    }

    T* Detach()
    {
        T* pT = m_ptr;
        m_ptr = NULL;
        return pT;
    }
    
    void Release()
    {
        if (m_ptr != NULL)
        {
            m_ptr->Release();
            m_ptr = NULL;
        }
    }

private:
    T* m_ptr;    
};

