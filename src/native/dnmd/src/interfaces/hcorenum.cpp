#include "hcorenum.hpp"
#include <cassert>
#include <cstring>

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
    assert(impl != nullptr && count > 0);

    HCORENUMImpl* enumImpl;
    enumImpl = (HCORENUMImpl*)::malloc(sizeof(*enumImpl) + (sizeof(enumImpl->_data) * (count - 1)));
    if (enumImpl == nullptr)
        return E_OUTOFMEMORY;

    // Immediately set the return.
    *impl = enumImpl;

    enumImpl->_type = HCORENUMType::Table;
    enumImpl->_entrySpan = 1;
    enumImpl->_curr = &enumImpl->_data;
    enumImpl->_last = enumImpl->_curr;

    // Initialize the linked list of EnumData.
    EnumData* currInit = enumImpl->_curr;
    currInit->Next = nullptr;

    // -1 because the initial impl contains one.
    EnumData* nextMaybe = (EnumData*)&enumImpl[1];
    for (size_t i = 0; i < (count - 1); ++i)
    {
        currInit->Next = nextMaybe;
        currInit = nextMaybe;
        currInit->Next = nullptr;
        enumImpl->_last = currInit;
        nextMaybe = nextMaybe + 1;
    }

    return S_OK;
}

void HCORENUMImpl::InitTableEnum(_Inout_ HCORENUMImpl& impl, _In_ uint32_t index, _In_ mdcursor_t cursor, _In_ uint32_t rows) noexcept
{
    assert(impl._type == HCORENUMType::Table);
    EnumData* currInit = impl._curr;

    // See CreateTableEnum for allocation layout.
    if (index > 0)
    {
        HCORENUMImpl* pImpl = &impl;
        EnumData* dataBegin = (EnumData*)&pImpl[1]; // Data starts immediately after impl.
        currInit = (EnumData*)&dataBegin[index - 1];
    }

    currInit->Table.Current = cursor;
    currInit->Table.Start = cursor;
    currInit->ReadIn = 0;
    currInit->Total = rows;
}

HRESULT HCORENUMImpl::CreateDynamicEnum(_Out_ HCORENUMImpl** impl, _In_ uint32_t entrySpan) noexcept
{
    assert(impl != nullptr && entrySpan > 0);

    HCORENUMImpl* enumImpl;
    enumImpl = (HCORENUMImpl*)::malloc(sizeof(*enumImpl));
    if (enumImpl == nullptr)
        return E_OUTOFMEMORY;

    // Immediately set the return.
    *impl = enumImpl;

    enumImpl->_type = HCORENUMType::Dynamic;
    // The page must be a multiple of the entrySpan for reading to be efficient.
    assert(ARRAY_SIZE(enumImpl->_data.Dynamic.Page) % entrySpan == 0);
    enumImpl->_entrySpan = entrySpan;
    ::memset(&enumImpl->_data, 0, sizeof(enumImpl->_data));
    enumImpl->_curr = &enumImpl->_data;
    enumImpl->_last = enumImpl->_curr;
    return S_OK;
}

HRESULT HCORENUMImpl::AddToDynamicEnum(_Inout_ HCORENUMImpl& impl, uint32_t value) noexcept
{
    assert(impl._type == HCORENUMType::Dynamic);

    // Check if we have exhausted the last page
    EnumData* currData = impl._last;
    if (currData->Total >= ARRAY_SIZE(currData->Dynamic.Page))
    {
        EnumData* newData = (EnumData*)::malloc(sizeof(EnumData));
        if (newData == nullptr)
            return E_OUTOFMEMORY;

        ::memset(newData, 0, sizeof(*newData));
        assert(currData->Next == nullptr);
        currData->Next = newData;
        impl._last = newData;
        currData = impl._last;
    }
    currData->Dynamic.Page[currData->Total] = value;
    currData->Total++;
    return S_OK;
}

void HCORENUMImpl::Destroy(_In_ HCORENUMImpl* impl) noexcept
{
    assert(impl != nullptr);
    if (impl->_type == HCORENUMType::Dynamic)
    {
        // Delete all allocated pages.
        EnumData* tmp;
        EnumData* toDelete = impl->_data.Next;
        while (toDelete != nullptr)
        {
            tmp = toDelete->Next;
            ::free(toDelete);
            toDelete = tmp;
        }
    }

    ::free(impl);
}

uint32_t HCORENUMImpl::Count() const noexcept
{
    // Accumulate all tables in the enumerator
    uint32_t count = 0;
    EnumData const* curr = &_data;
    do
    {
        count += curr->Total;
        curr = curr->Next;
    }
    while (curr != nullptr);

    return count / _entrySpan;
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
        hr = ReadOneToken(rTokens[0], tokenCount);
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

HRESULT HCORENUMImpl::ReadTokenPairs(
    mdToken rTokens1[],
    mdToken rTokens2[],
    ULONG cMax,
    ULONG* pcTokens) noexcept
{
    assert(_type == HCORENUMType::Dynamic);
    assert(rTokens1 != nullptr && rTokens2 != nullptr && pcTokens != nullptr);
    assert(_entrySpan == 2);

    EnumData* currData = _curr;
    if (currData == nullptr)
        return S_FALSE;

    uint32_t count = 0;
    for (uint32_t i = 0; i < cMax; ++i)
    {
        // Check if all values have been read.
        while (currData->ReadIn == currData->Total)
        {
            currData = currData->Next;
            // Check next link in enumerator list
            if (currData == nullptr)
                goto Done;
            _curr = currData;
        }

        assert(((currData->Total - currData->ReadIn) % 2) == 0);
        rTokens1[count] = currData->Dynamic.Page[currData->ReadIn++];
        rTokens2[count] = currData->Dynamic.Page[currData->ReadIn++];
        count++;
    }
Done:
    *pcTokens = count;
    return S_OK;
}

HRESULT HCORENUMImpl::Reset(_In_ ULONG position) noexcept
{
    return (_type == HCORENUMType::Table)
        ? ResetTableEnum(position)
        : ResetDynamicEnum(position);
}

HRESULT HCORENUMImpl::ReadOneToken(mdToken& rToken, uint32_t& count) noexcept
{
    EnumData* currData = _curr;
    while (currData->ReadIn == currData->Total)
    {
        currData = currData->Next;
        // Check next link in enumerator list
        if (currData == nullptr)
            return S_FALSE;
        _curr = currData;
    }

    if (_type == HCORENUMType::Table)
    {
        if (!md_cursor_to_token(currData->Table.Current, &rToken))
            return S_FALSE;
        (void)md_cursor_next(&currData->Table.Current);
    }
    else
    {
        rToken = currData->Dynamic.Page[currData->ReadIn];
    }

    currData->ReadIn++;
    count = 1;
    return S_OK;
}

HRESULT HCORENUMImpl::ReadTableTokens(
    mdToken rTokens[],
    uint32_t cMax,
    uint32_t& tokenCount) noexcept
{
    assert(_type == HCORENUMType::Table);
    assert(rTokens != nullptr);

    EnumData* currData = _curr;
    if (currData == nullptr)
        return S_FALSE;

    uint32_t count = 0;
    for (uint32_t i = 0; i < cMax; ++i)
    {
        // Check if all values have been read.
        while (currData->ReadIn == currData->Total)
        {
            currData = currData->Next;
            // Check next link in enumerator list
            if (currData == nullptr)
                goto Done;
            _curr = currData;
        }

        mdcursor_t current;
        if (!md_resolve_indirect_cursor(currData->Table.Current, &current))
            return CLDB_E_FILE_CORRUPT;

        if (!md_cursor_to_token(current, &rTokens[count]))
            break;
        count++;

        if (!md_cursor_next(&currData->Table.Current))
            break;
        currData->ReadIn++;
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

    EnumData* currData = _curr;
    if (currData == nullptr)
        return S_FALSE;

    uint32_t count = 0;
    for (uint32_t i = 0; i < cMax; ++i)
    {
        // Check if all values have been read.
        while (currData->ReadIn == currData->Total)
        {
            currData = currData->Next;
            // Check next link in enumerator list
            if (currData == nullptr)
                goto Done;
            _curr = currData;
        }

        rTokens[count] = currData->Dynamic.Page[currData->ReadIn];
        currData->ReadIn++;
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
    EnumData* currData = &_data;
    while (currData != nullptr)
    {
        newStart = currData->Table.Start;
        if (reset)
        {
            // Reset the enumerator state
            newReadIn = 0;
        }
        else if (position < currData->Total)
        {
            // The current enumerator contains the position
            if (!md_cursor_move(&newStart, position))
                return E_INVALIDARG;
            newReadIn = position;
            reset = true;

            // Update the current state of the enumerator
            _curr = currData;
        }
        else
        {
            // The current enumerator is consumed based on position
            position -= currData->Total;
            if (!md_cursor_move(&newStart, currData->Total))
                return E_INVALIDARG;
            newReadIn = currData->Total;
        }

        currData->Table.Current = newStart;
        currData->ReadIn = newReadIn;
        currData = currData->Next;
    }

    return S_OK;
}

HRESULT HCORENUMImpl::ResetDynamicEnum(_In_ uint32_t position) noexcept
{
    assert(_type == HCORENUMType::Dynamic);

    uint32_t newReadIn;
    bool reset = false;
    EnumData* currData = &_data;
    while (currData != nullptr)
    {
        if (reset)
        {
            // Reset the enumerator state
            newReadIn = 0;
        }
        else if (position < currData->Total)
        {
            newReadIn = position;
            reset = true;

            // Update the current state of the enumerator
            _curr = currData;
        }
        else
        {
            // The current enumerator is consumed based on position
            position -= currData->Total;
            newReadIn = currData->Total;
        }

        currData->ReadIn = newReadIn;
        currData = currData->Next;
    }

    return S_OK;
}
