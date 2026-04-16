// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Include the shared SList .inl for InsertHeadInterlocked implementation.
#include "../../inc/slist.inl"

// Utility algorithms in the rh::std namespace, used by NativeAOT Runtime code.

//-------------------------------------------------------------------------------------------------
namespace rh { namespace std
{
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

    template<class _InIt, class _Ty>
    inline
    _InIt find(_InIt _First, _InIt _Last, const _Ty& _Val)
    {   // find first matching _Val
        for (; _First != _Last; ++_First)
            if (*_First == _Val)
                break;
        return (_First);
    }

    // Specialize rh::std::find for SList iterators so that it will use _Traits::Equals.
    template<class _Tx, class _Traits, class _Ty>
    inline
    typename SList<_Tx, _Traits>::Iterator find(
        typename SList<_Tx, _Traits>::Iterator _First,
        typename SList<_Tx, _Traits>::Iterator _Last,
        const _Ty& _Val)
    {   // find first matching _Val
        for (; _First != _Last; ++_First)
            if (_Traits::Equals(*_First, _Val))
                break;
        return (_First);
    }
} // namespace std
} // namespace rh