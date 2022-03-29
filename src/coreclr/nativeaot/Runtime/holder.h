// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// -----------------------------------------------------------------------------------------------------------
// Cut down versions of the Holder and Wrapper template classes used in the CLR. If this coding pattern is
// also common in the Redhawk code then it might be worth investigating pulling the whole holder.h header file
// over (a quick look indicates it might not drag in too many extra dependencies).
//

// -----------------------------------------------------------------------------------------------------------
// This version of holder does not have a default constructor.

#if defined(_MSC_VER) && (_MSC_VER < 1900)
#define EQUALS_DEFAULT
#else
#define EQUALS_DEFAULT = default
#endif

template <typename TYPE, void (*ACQUIRE_FUNC)(TYPE), void (*RELEASE_FUNC)(TYPE)>
class HolderNoDefaultValue
{
public:
    HolderNoDefaultValue(TYPE value, bool fTake = true) : m_value(value), m_held(false)
        { if (fTake) { ACQUIRE_FUNC(value); m_held = true; } }

    ~HolderNoDefaultValue() { if (m_held) RELEASE_FUNC(m_value); }

    TYPE GetValue() { return m_value; }

    void Acquire() { ACQUIRE_FUNC(m_value); m_held = true; }
    void Release() { if (m_held) { RELEASE_FUNC(m_value); m_held = false; } }
    void SuppressRelease() { m_held = false; }
    TYPE Extract() { m_held = false; return GetValue(); }

    HolderNoDefaultValue(HolderNoDefaultValue && other) EQUALS_DEFAULT;

protected:
    TYPE    m_value;
    bool    m_held;

private:
    // No one should be copying around holder types.
    HolderNoDefaultValue & operator=(const HolderNoDefaultValue & other);
    HolderNoDefaultValue(const HolderNoDefaultValue & other);
};

// -----------------------------------------------------------------------------------------------------------
template <typename TYPE, void (*ACQUIRE_FUNC)(TYPE), void (*RELEASE_FUNC)(TYPE), TYPE DEFAULTVALUE = nullptr>
class Holder : public HolderNoDefaultValue<TYPE, ACQUIRE_FUNC, RELEASE_FUNC>
{
    typedef HolderNoDefaultValue<TYPE, ACQUIRE_FUNC, RELEASE_FUNC> MY_PARENT;
public:
    Holder() : MY_PARENT(DEFAULTVALUE, false) {}
    Holder(TYPE value, bool fTake = true) : MY_PARENT(value, fTake) {}

    Holder(Holder && other) EQUALS_DEFAULT;

private:
    // No one should be copying around holder types.
    Holder & operator=(const Holder & other);
    Holder(const Holder & other);
};

// -----------------------------------------------------------------------------------------------------------
template <typename TYPE, void (*ACQUIRE_FUNC)(TYPE), void (*RELEASE_FUNC)(TYPE), TYPE DEFAULTVALUE = nullptr>
class Wrapper : public Holder<TYPE, ACQUIRE_FUNC, RELEASE_FUNC, DEFAULTVALUE>
{
    typedef Holder<TYPE, ACQUIRE_FUNC, RELEASE_FUNC, DEFAULTVALUE> MY_PARENT;

public:
    Wrapper() : MY_PARENT() {}
    Wrapper(TYPE value, bool fTake = true) : MY_PARENT(value, fTake) {}
    Wrapper(Wrapper && other) EQUALS_DEFAULT;

    FORCEINLINE TYPE& operator=(TYPE const & value)
    {
        MY_PARENT::Release();
        MY_PARENT::m_value = value;
        MY_PARENT::Acquire();
        return MY_PARENT::m_value;
    }

    FORCEINLINE const TYPE &operator->() { return MY_PARENT::m_value; }
    FORCEINLINE const TYPE &operator*() { return MY_PARENT::m_value; }
    FORCEINLINE operator TYPE() { return MY_PARENT::m_value; }

private:
    // No one should be copying around wrapper types.
    Wrapper & operator=(const Wrapper & other);
    Wrapper(const Wrapper & other);
};

// -----------------------------------------------------------------------------------------------------------
template <typename TYPE>
FORCEINLINE void DoNothing(TYPE /*value*/)
{
}

// -----------------------------------------------------------------------------------------------------------
template <typename TYPE>
FORCEINLINE void Delete(TYPE *value)
{
    delete value;
}

// -----------------------------------------------------------------------------------------------------------
template <typename TYPE,
          typename PTR_TYPE = TYPE *,
          void (*ACQUIRE_FUNC)(PTR_TYPE) = DoNothing<PTR_TYPE>,
          void (*RELEASE_FUNC)(PTR_TYPE) = Delete<TYPE>,
          PTR_TYPE NULL_VAL = nullptr,
          typename BASE = Wrapper<PTR_TYPE, ACQUIRE_FUNC, RELEASE_FUNC, NULL_VAL> >
class NewHolder : public BASE
{
public:
    NewHolder(PTR_TYPE p = NULL_VAL) : BASE(p)
        { }

    PTR_TYPE& operator=(PTR_TYPE p)
        { return BASE::operator=(p); }

    bool IsNull()
        { return BASE::GetValue() == NULL_VAL; }
};

//-----------------------------------------------------------------------------
// NewArrayHolder : New []'ed pointer holder
//  {
//      NewArrayHolder<Foo> foo = new (nothrow) Foo [30];
//  } // delete [] foo on out of scope
//-----------------------------------------------------------------------------

template <typename TYPE>
FORCEINLINE void DeleteArray(TYPE *value)
{
    delete [] value;
    value = NULL;
}

template <typename TYPE,
          typename PTR_TYPE = TYPE *,
          void (*ACQUIRE_FUNC)(PTR_TYPE) = DoNothing<PTR_TYPE>,
          void (*RELEASE_FUNC)(PTR_TYPE) = DeleteArray<TYPE>,
          PTR_TYPE NULL_VAL = nullptr,
          typename BASE = Wrapper<PTR_TYPE, ACQUIRE_FUNC, RELEASE_FUNC, NULL_VAL> >
class NewArrayHolder : public BASE
{
public:
    NewArrayHolder(PTR_TYPE p = NULL_VAL) : BASE(p)
        { }

    PTR_TYPE& operator=(PTR_TYPE p)
        { return BASE::operator=(p); }

    bool IsNull()
        { return BASE::GetValue() == NULL_VAL; }
};

// -----------------------------------------------------------------------------------------------------------
template<typename TYPE>
FORCEINLINE void Destroy(TYPE * value)
{
    value->Destroy();
}

// -----------------------------------------------------------------------------------------------------------
template <typename TYPE,
          typename PTR_TYPE = TYPE *,
          void (*ACQUIRE_FUNC)(PTR_TYPE) = DoNothing<PTR_TYPE>,
          void (*RELEASE_FUNC)(PTR_TYPE) = Destroy<TYPE>,
          PTR_TYPE NULL_VAL = nullptr,
          typename BASE = Wrapper<PTR_TYPE, ACQUIRE_FUNC, RELEASE_FUNC, NULL_VAL> >
class CreateHolder : public BASE
{
public:
    CreateHolder(PTR_TYPE p = NULL_VAL) : BASE(p)
        { }

    PTR_TYPE& operator=(PTR_TYPE p)
        { return BASE::operator=(p); }
};


