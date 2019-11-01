// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

template <class T>
class ArrayHolder    
{
public:
    ArrayHolder(T *ptr)
        : m_ptr(ptr)
    {
    }

    ~ArrayHolder()
    {
        Clear();
    }
    
    ArrayHolder(const ArrayHolder &rhs)
    {
        m_ptr = const_cast<ArrayHolder *>(&rhs)->Detach();
    }

    ArrayHolder &operator=(T *ptr)
    {
        Clear();
        m_ptr = ptr;
        return *this;
    }

    const T &operator[](int i) const
    {
        return m_ptr[i];
    }

    T &operator[](int i)
    {
        return m_ptr[i];
    }

    operator const T *() const
    {
        return m_ptr;
    }

    operator T *()
    {
        return m_ptr;
    }

    T **operator&()
    {
        return &m_ptr;
    }

    T *GetPtr()
    {
        return m_ptr;
    }

    T *Detach()
    {
        T *ret = m_ptr;
        m_ptr = NULL;
        return ret;
    }

private:
    void Clear()
    {
        if (m_ptr)
        {
            delete [] m_ptr;
            m_ptr = NULL;
        }
    }

private:
    T *m_ptr;
};
