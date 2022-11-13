#include <cassert>

#include "impl.hpp"

#define RETURN_IF_FAILED(exp) \
{ \
    hr = (exp); \
    if (FAILED(hr)) \
    { \
        return hr; \
    } \
}

HRESULT HCORENUMImpl::CreateTableEnum(_In_ uint32_t count, _Out_ HCORENUMImpl** impl) noexcept
{
    assert(impl != nullptr);

    HCORENUMImpl* enumImpl;
    enumImpl = (HCORENUMImpl*)::malloc(sizeof(*enumImpl) * count);
    if (enumImpl == nullptr)
        return E_OUTOFMEMORY;

    // Immediately set the return.
    *impl = enumImpl;

    HCORENUMImpl* prev = enumImpl;
    prev->_type = HCORENUMType::Table;
    prev->_next = nullptr;

    for (size_t i = 1; i < count; ++i)
    {
        prev->_next = &enumImpl[i];
        prev = prev->_next;

        prev->_type = HCORENUMType::Table;
        prev->_next = nullptr;
    }

    return S_OK;
}

void HCORENUMImpl::InitTableEnum(_Inout_ HCORENUMImpl& impl, _In_ mdcursor_t cursor, _In_ uint32_t rows) noexcept
{
    assert(impl._type == HCORENUMType::Table);
    impl._table.Current = cursor;
    impl._table.Start = cursor;
    impl._readIn = 0;
    impl._total = rows;
}

HRESULT HCORENUMImpl::CreateDynamicEnum(_Out_ HCORENUMImpl** impl) noexcept
{
    assert(impl != nullptr);

    HCORENUMImpl* enumImpl;
    enumImpl = (HCORENUMImpl*)::malloc(sizeof(*enumImpl));
    if (enumImpl == nullptr)
        return E_OUTOFMEMORY;

    // Immediately set the return.
    *impl = enumImpl;

    ::memset(enumImpl, 0, sizeof(*enumImpl));
    enumImpl->_type = HCORENUMType::Dynamic;
    return S_OK;
}

HRESULT HCORENUMImpl::AddToDynamicEnum(_Inout_ HCORENUMImpl& impl, uint32_t value) noexcept
{
    assert(impl._type == HCORENUMType::Dynamic);

    HRESULT hr;
    HCORENUMImpl* currImpl = &impl;

    uint32_t next = currImpl->_total;
    while (next >= ARRAYSIZE(currImpl->_dynamic.Page))
    {
        if (currImpl->_next == nullptr)
            RETURN_IF_FAILED(CreateDynamicEnum(&currImpl->_next));
        currImpl = currImpl->_next;
        next = currImpl->_total;
    }
    currImpl->_dynamic.Page[next] = value;
    currImpl->_total++;
    return S_OK;
}

void HCORENUMImpl::Destroy(_In_ HCORENUMImpl* impl) noexcept
{
    assert(impl != nullptr);
    if (impl->_type == HCORENUMType::Table)
    {
        ::free(impl);
    }
    else
    {
        assert(impl->_type == HCORENUMType::Dynamic);

        HCORENUMImpl* tmp;
        do
        {
            tmp = impl->_next;
            ::free(impl);
            impl = tmp;
        }
        while (impl != nullptr);
    }
}

uint32_t HCORENUMImpl::Count() const noexcept
{
    // Accumulate all tables in the enumerator
    uint32_t count = 0;
    HCORENUMImpl const* curr = this;
    while (curr != nullptr)
    {
        count += curr->_total;
        curr = curr->_next;
    }

    return count;
}

HRESULT HCORENUMImpl::ReadTokens(
    mdToken rTokens[],
    ULONG cMax,
    ULONG* pcTokens) noexcept
{
    HRESULT hr;
    uint32_t tokenCount = 0;
    if (cMax == 1)
    {
        hr = ReadOneToken(rTokens[0]);
        if (hr == S_OK)
            tokenCount = 1;
    }
    else
    {
        hr = (_type == HCORENUMType::Table)
            ? ReadTableTokens(rTokens, cMax, tokenCount)
            : ReadDynamicTokens(rTokens, cMax, tokenCount);
    }

    if (pcTokens != nullptr)
        *pcTokens = tokenCount;

    return hr;
}

HRESULT HCORENUMImpl::Reset(_In_ ULONG position) noexcept
{
    return (_type == HCORENUMType::Table)
        ? ResetTableEnum(position)
        : ResetDynamicEnum(position);
}

HRESULT HCORENUMImpl::ReadOneToken(mdToken& rToken) noexcept
{
    HCORENUMImpl* enumImpl = this;

    // Find the current enumerator
    while (enumImpl->_readIn == enumImpl->_total)
    {
        enumImpl = enumImpl->_next;
        // Check next link in enumerator list
        if (enumImpl == nullptr)
            return S_FALSE;
    }

    if (_type == HCORENUMType::Table)
    {
        if (!md_cursor_to_token(enumImpl->_table.Current, &rToken))
            return S_FALSE;
        (void)md_cursor_next(&enumImpl->_table.Current);
    }
    else
    {
        rToken = enumImpl->_dynamic.Page[enumImpl->_readIn];
    }
    enumImpl->_readIn++;
    return S_OK;
}

HRESULT HCORENUMImpl::ReadTableTokens(
    mdToken rTokens[],
    uint32_t cMax,
    uint32_t& tokenCount) noexcept
{
    assert(_type == HCORENUMType::Table);
    assert(rTokens != nullptr);

    HCORENUMImpl* enumImpl = this;
    uint32_t count = 0;
    for (uint32_t i = 0; i < cMax; ++i)
    {
        // Check if all values have been read.
        while (enumImpl->_readIn == enumImpl->_total)
        {
            enumImpl = enumImpl->_next;
            // Check next link in enumerator list
            if (enumImpl == nullptr)
                goto Done;
        }

        if (!md_cursor_to_token(enumImpl->_table.Current, &rTokens[count]))
            break;
        count++;

        if (!md_cursor_next(&enumImpl->_table.Current))
            break;
        enumImpl->_readIn++;
    }
Done:
    tokenCount = count;
    return S_OK;
}

HRESULT HCORENUMImpl::ReadDynamicTokens(
    mdToken rTokens[],
    uint32_t cMax,
    uint32_t& tokenCount) noexcept
{
    assert(_type == HCORENUMType::Dynamic);
    assert(rTokens != nullptr);

    HCORENUMImpl* enumImpl = this;
    uint32_t count = 0;
    for (uint32_t i = 0; i < cMax; ++i)
    {
        // Check if all values have been read.
        while (enumImpl->_readIn == enumImpl->_total)
        {
            enumImpl = enumImpl->_next;
            // Check next link in enumerator list
            if (enumImpl == nullptr)
                goto Done;
        }

        rTokens[count] = enumImpl->_dynamic.Page[enumImpl->_readIn];
        enumImpl->_readIn++;
        count++;
    }
Done:
    tokenCount = count;
    return S_OK;
}

HRESULT HCORENUMImpl::ResetTableEnum(_In_ uint32_t position) noexcept
{
    assert(_type == HCORENUMType::Table);

    mdcursor_t newStart;
    uint32_t newReadIn;
    bool reset = false;
    HCORENUMImpl* enumImpl = this;
    while (enumImpl != nullptr)
    {
        newStart = enumImpl->_table.Start;
        if (reset)
        {
            // Reset the enumerator state
            newReadIn = 0;
        }
        else if (position < enumImpl->_total)
        {
            // The current enumerator contains the position
            if (!md_cursor_move(&newStart, position))
                return E_INVALIDARG;
            newReadIn = position;
            reset = true;
        }
        else
        {
            // The current enumerator is consumed based on position
            position -= enumImpl->_total;
            if (!md_cursor_move(&newStart, enumImpl->_total))
                return E_INVALIDARG;
            newReadIn = enumImpl->_total;
        }

        enumImpl->_table.Current = newStart;
        enumImpl->_readIn = newReadIn;
        enumImpl = enumImpl->_next;
    }

    return S_OK;
}

HRESULT HCORENUMImpl::ResetDynamicEnum(_In_ uint32_t position) noexcept
{
    assert(_type == HCORENUMType::Dynamic);

    uint32_t newReadIn;
    bool reset = false;
    HCORENUMImpl* enumImpl = this;
    while (enumImpl != nullptr)
    {
        if (reset)
        {
            // Reset the enumerator state
            newReadIn = 0;
        }
        else if (position < enumImpl->_total)
        {
            newReadIn = position;
            reset = true;
        }
        else
        {
            // The current enumerator is consumed based on position
            position -= enumImpl->_total;
            newReadIn = enumImpl->_total;
        }

        enumImpl->_readIn = newReadIn;
        enumImpl = enumImpl->_next;
    }

    return S_OK;
}
