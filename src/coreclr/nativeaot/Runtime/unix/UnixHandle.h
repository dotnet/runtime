// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __UNIX_HANDLE_H__
#define __UNIX_HANDLE_H__

enum class UnixHandleType
{
    Thread,
    Event
};

// TODO: add validity check for usage / closing?
class UnixHandleBase
{
    UnixHandleType m_type;
protected:
    UnixHandleBase(UnixHandleType type)
    : m_type(type)
    {
    }

public:

    virtual ~UnixHandleBase()
    {
    }

    virtual bool Destroy()
    {
        return true;
    }

    UnixHandleType GetType()
    {
        return m_type;
    }
};

template<UnixHandleType HT, typename T>
class UnixHandle : UnixHandleBase
{
protected:
    T m_object;
public:

    UnixHandle(T object)
    : UnixHandleBase(HT),
      m_object(object)
    {
    }

    T* GetObject()
    {
        return &m_object;
    }
};

#endif // __UNIX_HANDLE_H__
