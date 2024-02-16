// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
MSVC_SAVE_WARNING_STATE()
MSVC_DISABLE_WARNING(4127)  // conditional expression is constant --
                            // while (true) loops and compile time template constants cause this.


//-------------------------------------------------------------------------------------------------
namespace rh { namespace std
{
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

//-------------------------------------------------------------------------------------------------
inline
void DoNothingFailFastPolicy::FailFast()
{
    // Intentionally a no-op.
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename FailFastPolicy>
inline
typename DefaultSListTraits<T, FailFastPolicy>::PTR_PTR_T DefaultSListTraits<T, FailFastPolicy>::GetNextPtr(
    PTR_T pT)
{
    ASSERT(pT != NULL);
    return dac_cast<PTR_PTR_T>(dac_cast<TADDR>(pT) + offsetof(T, m_pNext));
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename FailFastPolicy>
inline
bool DefaultSListTraits<T, FailFastPolicy>::Equals(
    PTR_T pA,
    PTR_T pB)
{   // Default is pointer comparison
    return pA == pB;
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
SList<T, Traits>::SList()
    : m_pHead(NULL)
{
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
bool SList<T, Traits>::IsEmpty()
{
    return Begin() == End();
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
typename SList<T, Traits>::PTR_T SList<T, Traits>::GetHead()
{
    return m_pHead;
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
void SList<T, Traits>::PushHead(
    PTR_T pItem)
{
    Begin().Insert(pItem);
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
void SList<T, Traits>::PushHeadInterlocked(
    PTR_T pItem)
{
    ASSERT(pItem != NULL);
    ASSERT(IS_ALIGNED(&m_pHead, sizeof(void*)));

    while (true)
    {
        *Traits::GetNextPtr(pItem) = *reinterpret_cast<T * volatile *>(&m_pHead);
        if (PalInterlockedCompareExchangePointer(
                reinterpret_cast<void * volatile *>(&m_pHead),
                reinterpret_cast<void *>(pItem),
                reinterpret_cast<void *>(*Traits::GetNextPtr(pItem))) == reinterpret_cast<void *>(*Traits::GetNextPtr(pItem)))
        {
            break;
        }
    }
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
typename SList<T, Traits>::PTR_T SList<T, Traits>::PopHead()
{
    PTR_T pRet = *Begin();
    Begin().Remove();
    return pRet;
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
SList<T, Traits>::Iterator::Iterator(
    Iterator const &it)
    : m_ppCur(it.m_ppCur)
#ifdef _DEBUG
      , m_fIsValid(it.m_fIsValid)
#endif
{
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
SList<T, Traits>::Iterator::Iterator(
    PTR_PTR_T ppItem)
    : m_ppCur(ppItem)
#ifdef _DEBUG
      , m_fIsValid(true)
#endif
{
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
typename SList<T, Traits>::Iterator& SList<T, Traits>::Iterator::operator=(
    Iterator const &it)
{
    m_ppCur = it.m_ppCur;
#ifdef _DEBUG
    m_fIsValid = it.m_fIsValid;
#endif
    return *this;
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
typename SList<T, Traits>::PTR_T SList<T, Traits>::Iterator::operator->()
{
    _Validate(e_HasValue);
    return _Value();
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
typename SList<T, Traits>::PTR_T SList<T, Traits>::Iterator::operator*()
{
    _Validate(e_HasValue);
    return _Value();
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
typename SList<T, Traits>::Iterator & SList<T, Traits>::Iterator::operator++()
{
    _Validate(e_HasValue); // Having a value means we're not at the end.
    m_ppCur = Traits::GetNextPtr(_Value());
    return *this;
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
typename SList<T, Traits>::Iterator SList<T, Traits>::Iterator::operator++(
    int)
{
    _Validate(e_HasValue); // Having a value means we're not at the end.
    PTR_PTR_T ppRet = m_ppCur;
    ++(*this);
    return Iterator(ppRet);
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
bool SList<T, Traits>::Iterator::operator==(
    Iterator const &rhs)
{
    _Validate(e_CanCompare);
    rhs._Validate(e_CanCompare);
    return Traits::Equals(_Value(), rhs._Value());
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
bool SList<T, Traits>::Iterator::operator==(
    PTR_T pT)
{
    _Validate(e_CanCompare);
    return Traits::Equals(_Value(), pT);
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
bool SList<T, Traits>::Iterator::operator!=(
    Iterator const &rhs)
{
    return !operator==(rhs);
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline /*static*/
typename SList<T, Traits>::Iterator SList<T, Traits>::Iterator::End()
{
    return Iterator(NULL);
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
typename SList<T, Traits>::Iterator SList<T, Traits>::Iterator::Insert(
    PTR_T pItem)
{
    _Validate(e_CanInsert);
    *Traits::GetNextPtr(pItem) = *m_ppCur;
    *m_ppCur = pItem;
    Iterator itRet(m_ppCur);
    ++(*this);
    return itRet;
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
typename SList<T, Traits>::Iterator SList<T, Traits>::Iterator::Remove()
{
    _Validate(e_HasValue);
    *m_ppCur = *Traits::GetNextPtr(*m_ppCur);
    PTR_PTR_T ppRet = m_ppCur;
    // Set it to End, so that subsequent misuse of this iterator will
    // result in an AV rather than possible memory corruption.
    *this = End();
    return Iterator(ppRet);
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
typename SList<T, Traits>::PTR_T SList<T, Traits>::Iterator::_Value() const
{
    ASSERT(m_fIsValid);
    return dac_cast<PTR_T>(m_ppCur == NULL ? NULL : *m_ppCur);
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
void SList<T, Traits>::Iterator::_Validate(e_ValidateOperation op) const
{
    ASSERT(m_fIsValid);
    ASSERT(op == e_CanCompare || op == e_CanInsert || op == e_HasValue);

    if ((op != e_CanCompare && m_ppCur == NULL) ||
        (op == e_HasValue && *m_ppCur == NULL))
    {
        // NOTE: Default of DoNothingFailFastPolicy is a no-op, and so this function will be
        // eliminated in retail builds. This is ok, as the subsequent operation will cause
        // an AV, which will itself trigger a FailFast. Provide a different policy to get
        // different behavior.
        ASSERT_MSG(false, "Invalid SList::Iterator use.");
        Traits::FailFast();
#ifdef _DEBUG
        m_fIsValid = false;
#endif
    }
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
typename SList<T, Traits>::Iterator SList<T, Traits>::Begin()
{
    typedef SList<T, Traits> T_THIS;
    return Iterator(dac_cast<PTR_PTR_T>(
        dac_cast<TADDR>(this) + offsetof(T_THIS, m_pHead)));
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
typename SList<T, Traits>::Iterator SList<T, Traits>::End()
{
    return Iterator::End();
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
typename SList<T, Traits>::Iterator SList<T, Traits>::FindFirst(PTR_T pItem)
{
    return rh::std::find(Begin(), End(), pItem);
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
bool SList<T, Traits>::RemoveFirst(PTR_T pItem)
{
    Iterator it = FindFirst(pItem);
    if (it != End())
    {
        it.Remove();
        return true;
    }
    else
    {
        return false;
    }
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
typename SList<T, Traits>::Iterator SList<T, Traits>::Insert(Iterator & it, PTR_T pItem)
{
    return it.Insert(pItem);
}

//-------------------------------------------------------------------------------------------------
template <typename T, typename Traits>
inline
typename SList<T, Traits>::Iterator SList<T, Traits>::Remove(Iterator & it)
{
    return it.Remove();
}


MSVC_RESTORE_WARNING_STATE()

