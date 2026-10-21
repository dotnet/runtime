// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _GCREF_H_
#define _GCREF_H_

#include "gcdesc.h"

#ifndef DACCESS_COMPILE
struct GCReferenceRange
{
    Object** start;
    size_t count;
};

struct GCReferenceObjectLayout
{
    size_t objectSize;
    uint32_t componentCount;
};

FORCEINLINE GCReferenceObjectLayout GetGCReferenceObjectLayout(
    Object* object,
    MethodTable* methodTable)
{
    GCReferenceObjectLayout layout = { methodTable->GetBaseSize(), 0 };
    if (methodTable->HasComponentSize())
    {
        layout.componentCount =
            reinterpret_cast<ArrayBase*>(object)->GetNumComponents();
        size_t componentSize = methodTable->RawGetComponentSize();
        _ASSERTE(
            (componentSize == 0) ||
            (layout.componentCount <=
             ((static_cast<size_t>(-1) - layout.objectSize) / componentSize)));
        layout.objectSize +=
            static_cast<size_t>(layout.componentCount) * componentSize;
    }

    return layout;
}

class GCReferenceRanges
{
public:
    template <typename TVisitor>
    static FORCEINLINE bool Enumerate(Object* object, TVisitor&& visitor)
    {
        _ASSERTE(object != nullptr);

        MethodTable* methodTable = object->GetGCSafeMethodTable();
        if (!methodTable->ContainsGCPointers())
        {
            return true;
        }

        GCReferenceObjectLayout objectLayout =
            GetGCReferenceObjectLayout(object, methodTable);
        CGCDesc* map = CGCDesc::GetCGCDescFromMT(methodTable);
        CGCDescSeries* current = map->GetHighestSeries();
        ptrdiff_t seriesCount = static_cast<ptrdiff_t>(map->GetNumSeries());
        _ASSERTE(seriesCount != 0);

        if (seriesCount > 0)
        {
            CGCDescSeries* last = map->GetLowestSeries();
            while (true)
            {
                Object** rangeStart = reinterpret_cast<Object**>(
                    reinterpret_cast<uint8_t*>(object) +
                    current->GetSeriesOffset());
                size_t rangeSize =
                    current->GetSeriesSize() + objectLayout.objectSize;
                size_t rangeCount = rangeSize / sizeof(Object*);
                if ((rangeCount != 0) && !visitor(rangeStart, rangeCount))
                {
                    return false;
                }

                if (current == last)
                {
                    return true;
                }
                current--;
            }
        }

        Object** rangeStart = reinterpret_cast<Object**>(
            reinterpret_cast<uint8_t*>(object) +
            current->GetSeriesOffset());
        for (uint32_t component = 0;
             component < objectLayout.componentCount;
             component++)
        {
            for (ptrdiff_t seriesIndex = 0;
                 seriesIndex > seriesCount;
                 seriesIndex--)
            {
                val_serie_item* item = current->val_serie + seriesIndex;
                if (!visitor(rangeStart, item->nptrs))
                {
                    return false;
                }
                rangeStart = reinterpret_cast<Object**>(
                    reinterpret_cast<uint8_t*>(rangeStart + item->nptrs) +
                    item->skip);
            }
        }

        return true;
    }
};

class GCReferenceRangeIterator
{
public:
    explicit GCReferenceRangeIterator(Object* object)
        : m_object(reinterpret_cast<uint8_t*>(object))
        , m_objectSize(0)
#ifdef _DEBUG
        , m_objectEnd(nullptr)
#endif // _DEBUG
        , m_currentSeries(nullptr)
        , m_lastSeries(nullptr)
        , m_seriesCount(0)
        , m_repeatingSeriesIndex(0)
        , m_repeatingCursor(nullptr)
        , m_remainingComponents(0)
    {
        _ASSERTE(object != nullptr);

        MethodTable* methodTable = object->GetGCSafeMethodTable();
        if (!methodTable->ContainsGCPointers())
        {
            return;
        }

        GCReferenceObjectLayout objectLayout =
            GetGCReferenceObjectLayout(object, methodTable);
        m_objectSize = objectLayout.objectSize;
#ifdef _DEBUG
        _ASSERTE(m_objectSize >= sizeof(ObjHeader));
        m_objectEnd = m_object + m_objectSize - sizeof(ObjHeader);
#endif // _DEBUG

        CGCDesc* map = CGCDesc::GetCGCDescFromMT(methodTable);
        m_currentSeries = map->GetHighestSeries();
        m_seriesCount = static_cast<ptrdiff_t>(map->GetNumSeries());
        _ASSERTE(m_seriesCount != 0);

        if (m_seriesCount > 0)
        {
            m_lastSeries = map->GetLowestSeries();
        }
        else
        {
            m_repeatingCursor = m_object + m_currentSeries->GetSeriesOffset();
            m_remainingComponents = objectLayout.componentCount;
        }
    }

    void Relocate(Object* object)
    {
        uint8_t* destination = reinterpret_cast<uint8_t*>(object);
        if (m_repeatingCursor != nullptr)
        {
            m_repeatingCursor = destination + (m_repeatingCursor - m_object);
        }
#ifdef _DEBUG
        if (m_objectEnd != nullptr)
        {
            m_objectEnd = destination + (m_objectEnd - m_object);
        }
#endif // _DEBUG
        m_object = destination;
    }

    FORCEINLINE bool Next(GCReferenceRange* range)
    {
        _ASSERTE(range != nullptr);

        if (m_seriesCount > 0)
        {
            while (m_currentSeries != nullptr)
            {
                CGCDescSeries* series = m_currentSeries;
                m_currentSeries =
                    (series == m_lastSeries) ? nullptr : series - 1;

                uint8_t* rangeStart = m_object + series->GetSeriesOffset();
                size_t rangeSize = series->GetSeriesSize() + m_objectSize;
#ifdef _DEBUG
                _ASSERTE((series->GetSeriesOffset() % sizeof(Object*)) == 0);
                _ASSERTE((rangeSize % sizeof(Object*)) == 0);
                _ASSERTE(rangeStart <= m_objectEnd);
                _ASSERTE(rangeSize <= static_cast<size_t>(m_objectEnd - rangeStart));
#endif // _DEBUG

                size_t count = rangeSize / sizeof(Object*);
                if (count == 0)
                {
                    continue;
                }

                range->start = reinterpret_cast<Object**>(rangeStart);
                range->count = count;
                return true;
            }

            range->start = nullptr;
            range->count = 0;
            return false;
        }

        while (m_remainingComponents != 0)
        {
            val_serie_item* item =
                m_currentSeries->val_serie + m_repeatingSeriesIndex;
            size_t count = item->nptrs;
            size_t rangeSize = count * sizeof(Object*);
            uint8_t* rangeStart = m_repeatingCursor;
            m_repeatingCursor += rangeSize + item->skip;

            m_repeatingSeriesIndex--;
            if (m_repeatingSeriesIndex == m_seriesCount)
            {
                m_repeatingSeriesIndex = 0;
                m_remainingComponents--;
            }

#ifdef _DEBUG
            _ASSERTE(count != 0);
            _ASSERTE((reinterpret_cast<uintptr_t>(rangeStart) % sizeof(Object*)) == 0);
            _ASSERTE(rangeStart < m_objectEnd);
            _ASSERTE(rangeSize <= static_cast<size_t>(m_objectEnd - rangeStart));
#endif // _DEBUG

            range->start = reinterpret_cast<Object**>(rangeStart);
            range->count = count;
            return true;
        }

        range->start = nullptr;
        range->count = 0;
        return false;
    }

private:
    uint8_t* m_object;
    size_t m_objectSize;
#ifdef _DEBUG
    uint8_t* m_objectEnd;
#endif // _DEBUG
    CGCDescSeries* m_currentSeries;
    CGCDescSeries* m_lastSeries;
    ptrdiff_t m_seriesCount;
    ptrdiff_t m_repeatingSeriesIndex;
    uint8_t* m_repeatingCursor;
    uint32_t m_remainingComponents;
};
#endif // !DACCESS_COMPILE

#endif // _GCREF_H_
