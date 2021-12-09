// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// type_traits.hpp
//
// Type trait metaprogramming utilities.
//

#ifndef __TYPE_TRAITS_HPP__
#define __TYPE_TRAITS_HPP__

#include "CommonTypes.h"

namespace type_traits
{

namespace imp
{

struct true_type { static const bool value = true; };
struct false_type { static const bool value = false; };

////////////////////////////////////////////////////////////////////////////////
// Helper types Small and Big - guarantee that sizeof(Small) < sizeof(Big)
//

template <class T, class U>
struct conversion_helper
{
    typedef char Small;
    struct Big { char dummy[2]; };
    static Big   Test(...);
    static Small Test(U);
    static T MakeT();
};

////////////////////////////////////////////////////////////////////////////////
// class template conversion
// Figures out the conversion relationships between two types
// Invocations (T and U are types):
// a) conversion<T, U>::exists
// returns (at compile time) true if there is an implicit conversion from T
// to U (example: Derived to Base)
// b) conversion<T, U>::exists2Way
// returns (at compile time) true if there are both conversions from T
// to U and from U to T (example: int to char and back)
// c) conversion<T, U>::sameType
// returns (at compile time) true if T and U represent the same type
//
// NOTE: might not work if T and U are in a private inheritance hierarchy.
//

template <class T, class U>
struct conversion
{
    typedef imp::conversion_helper<T, U> H;
    static const bool exists = sizeof(typename H::Small) == sizeof((H::Test(H::MakeT())));
    static const bool exists2Way = exists && conversion<U, T>::exists;
    static const bool sameType = false;
};

template <class T>
struct conversion<T, T>
{
    static const bool exists = true;
    static const bool exists2Way = true;
    static const bool sameType = true;
};

template <class T>
struct conversion<void, T>
{
    static const bool exists = false;
    static const bool exists2Way = false;
    static const bool sameType = false;
};

template <class T>
struct conversion<T, void>
{
    static const bool exists = false;
    static const bool exists2Way = false;
    static const bool sameType = false;
};

template <>
struct conversion<void, void>
{
    static const bool exists = true;
    static const bool exists2Way = true;
    static const bool sameType = true;
};

template <bool>
struct is_base_of_helper;

template <>
struct is_base_of_helper<true> : public true_type {} ;

template <>
struct is_base_of_helper<false> : public false_type {} ;

}// imp

////////////////////////////////////////////////////////////////////////////////
// is_base_of::value is typedefed to be true if TDerived derives from TBase
// and false otherwise.
//
//
// NOTE: use TR1 type_traits::is_base_of when available.
//
#ifdef _MSC_VER

template <typename TBase, typename TDerived>
struct is_base_of : public imp::is_base_of_helper<__is_base_of( TBase, TDerived)> {};

#else

// Note that we need to compare pointer types here, since conversion of types by-value
// just tells us whether or not an implicit conversion constructor exists. We handle
// type parameters that are already pointers specially; see below.
template <typename TBase, typename TDerived>
struct is_base_of : public imp::is_base_of_helper<imp::conversion<TDerived *, TBase *>::exists> {};

// Specialization to handle type parameters that are already pointers.
template <typename TBase, typename TDerived>
struct is_base_of<TBase *, TDerived *> : public imp::is_base_of_helper<imp::conversion<TDerived *, TBase *>::exists> {};

// Specialization to handle invalid mixing of pointer types.
template <typename TBase, typename TDerived>
struct is_base_of<TBase *, TDerived> : public imp::false_type {};

// Specialization to handle invalid mixing of pointer types.
template <typename TBase, typename TDerived>
struct is_base_of<TBase, TDerived *> : public imp::false_type {};

#endif

////////////////////////////////////////////////////////////////////////////////
// Remove const qualifications, if any. Access using remove_const::type
//
template <typename T> struct remove_const { typedef T type; };
template <typename T> struct remove_const<T const> { typedef T type; };

////////////////////////////////////////////////////////////////////////////////
// is_signed::value is true if T is a signed integral type, false otherwise.
//
template <typename T>
struct is_signed { static const bool value = (static_cast<T>(-1) < 0); };

}

////////////////////////////////////////////////////////////////////////////////
// These are related to type traits, but they are more like asserts of type
// traits in that the result is that either the compiler does or does not
// produce an error.
//
namespace type_constraints
{

////////////////////////////////////////////////////////////////////////////////
// derived_from will produce a compiler error if TDerived does not
// derive from TBase.
//
// NOTE: use TR1 type_traits::is_base_of when available.
//

template<class TBase, class TDerived> struct is_base_of
{
    is_base_of()
    {
        static_assert((type_traits::is_base_of<TBase, TDerived>::value),
                      "is_base_of() constraint violation: TDerived does not derive from TBase");
    }
};

}; // namespace type_constraints

namespace rh { namespace std
{
    // Import some select components of the STL

    // TEMPLATE FUNCTION for_each
    template<class _InIt, class _Fn1>
    inline
    _Fn1 for_each(_InIt _First, _InIt _Last, _Fn1 _Func)
    {   // perform function for each element
        for (; _First != _Last; ++_First)
            _Func(*_First);
        return (_Func);
    }

    template<class _InIt, class _Ty>
    inline
    _InIt find(_InIt _First, _InIt _Last, const _Ty& _Val)
    {   // find first matching _Val
        for (; _First != _Last; ++_First)
            if (*_First == _Val)
                break;
        return (_First);
    }

    template<class _InIt, class _Pr>
    inline
    _InIt find_if(_InIt _First, _InIt _Last, _Pr _Pred)
    {   // find first satisfying _Pred
        for (; _First != _Last; ++_First)
            if (_Pred(*_First))
                break;
        return (_First);
    }

    template<class _InIt, class _Ty>
    inline
    bool exists(_InIt _First, _InIt _Last, const _Ty& _Val)
    {
        return find(_First, _Last, _Val) != _Last;
    }

    template<class _InIt, class _Pr>
    inline
    bool exists_if(_InIt _First, _InIt _Last, _Pr _Pred)
    {
        return find_if(_First, _Last, _Pred) != _Last;
    }

    template<class _InIt, class _Ty>
    inline
    uintptr_t count(_InIt _First, _InIt _Last, const _Ty& _Val)
    {
        uintptr_t _Ret = 0;
        for (; _First != _Last; _First++)
            if (*_First == _Val)
                ++_Ret;
        return _Ret;
    }

    template<class _InIt, class _Pr>
    inline
    uintptr_t count_if(_InIt _First, _InIt _Last, _Pr _Pred)
    {
        uintptr_t _Ret = 0;
        for (; _First != _Last; _First++)
            if (_Pred(*_First))
                ++_Ret;
        return _Ret;
    }

    // Forward declaration, each collection requires specialization
    template<class _FwdIt, class _Ty>
    inline
    _FwdIt remove(_FwdIt _First, _FwdIt _Last, const _Ty& _Val);
} // namespace std
} // namespace rh

#if 0

// -----------------------------------------------------------------
// Holding place for unused-but-possibly-useful-in-the-future code.

// -------------------------------------------------
// This belongs in type_traits.hpp

//
// is_pointer::value is true if the type is a pointer, false otherwise
//
template <typename T> struct is_pointer : public false_type {};
template <typename T> struct is_pointer<T *> : public true_type {};

//
// Remove pointer from type, if it has one. Use remove_pointer::type
// Further specialized in daccess.h
//
template <typename T> struct remove_pointer { typedef T type; };
template <typename T> struct remove_pointer<T *> { typedef T type; };

// -------------------------------------------------
// This belongs in daccess.h

namespace type_traits
{

//
// is_pointer::value is true if the type is a pointer, false otherwise
// specialized from type_traits.hpp
//
template <typename T> struct is_pointer<typename __DPtr<T> > : public type_traits::true_type {};

//
// remove_pointer::type is T with one less pointer qualification, if it had one.
// specialized from type_traits.hpp
//
template <typename T> struct remove_pointer<typename __DPtr<T> > { typedef T type; };

} // type_traits

namespace dac
{

//
// is_dptr::value is true if T is a __DPtr, false otherwise.
// This is a partial specialization case for the positive case.
//
//template <typename T> struct is_dptr<typename __DPtr<T> > : public type_traits::true_type {};

}

#endif

#endif

