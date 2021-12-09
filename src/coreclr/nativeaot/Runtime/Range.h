// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma once

namespace rh { namespace util
{
    //---------------------------------------------------------------------------------------------
    // Represents value range [a,b), and provides various convenience methods.

    template <typename VALUE_TYPE, typename LENGTH_TYPE = VALUE_TYPE>
    class Range
    {
        typedef Range<VALUE_TYPE, LENGTH_TYPE> THIS_T;

    public:
        //-----------------------------------------------------------------------------------------
        // Construction

        Range()
            : m_start(0),
              m_end(0)
            {}

        Range(Range const & range)
            : m_start(range.m_start),
              m_end(range.m_end)
            {}

        static Range<VALUE_TYPE> CreateWithEndpoint(VALUE_TYPE start,
                                                    VALUE_TYPE end)
            { return Range<VALUE_TYPE>(start, end); }

        static Range<VALUE_TYPE> CreateWithLength(VALUE_TYPE start, LENGTH_TYPE len)
            { return Range<VALUE_TYPE>(start, start + len); }

        //-----------------------------------------------------------------------------------------
        // Operations

        THIS_T& operator=(THIS_T const & range)
            { m_start = range.m_start; m_end = range.m_end; return *this; }

        bool Equals(THIS_T const & range) const
            { return GetStart() == range.GetStart() && GetEnd() == range.GetEnd(); }

        bool operator==(THIS_T const & range) const
            { return Equals(range); }

        bool operator!=(THIS_T const & range) const
            { return !Equals(range); }

        VALUE_TYPE GetStart() const
            { return m_start; }

        VALUE_TYPE GetEnd() const
            { return m_end; }

        LENGTH_TYPE GetLength() const
            { return m_end - m_start; }

        bool IntersectsWith(THIS_T const &range) const
            { return range.GetStart() < GetEnd() && range.GetEnd() > GetStart(); }

        bool IntersectsWith(VALUE_TYPE start,
                            VALUE_TYPE end) const
            { return IntersectsWith(THIS_T(start, end)); }

        bool Contains(THIS_T const &range) const
            { return GetStart() <= range.GetStart() && range.GetEnd() <= GetEnd(); }

        bool IsAdjacentTo(THIS_T const &range) const
            { return GetEnd() == range.GetStart() || range.GetEnd() == GetStart(); }

    protected:
        Range(VALUE_TYPE start, VALUE_TYPE end)
            : m_start(start),
              m_end(end)
            { ASSERT(start <= end); }

        VALUE_TYPE m_start;
        VALUE_TYPE m_end;
    };

    //---------------------------------------------------------------------------------------------
    // Represents address range [a,b), and provides various convenience methods.

    class MemRange : public Range<uint8_t*, uintptr_t>
    {
        typedef Range<uint8_t*, uintptr_t> BASE_T;

    public:
        //-----------------------------------------------------------------------------------------
        // Construction

        MemRange()
            : BASE_T()
            {}

        MemRange(void* pvMemStart,
                 uintptr_t cbMemLen)
            : BASE_T(reinterpret_cast<uint8_t*>(pvMemStart), reinterpret_cast<uint8_t*>(pvMemStart) + cbMemLen)
            {}

        MemRange(void* pvMemStart,
                 void* pvMemEnd)
            : BASE_T(reinterpret_cast<uint8_t*>(pvMemStart), reinterpret_cast<uint8_t*>(pvMemEnd))
            {}

        MemRange(MemRange const & range)
            : BASE_T(range)
            { }

        //-----------------------------------------------------------------------------------------
        // Operations

        MemRange& operator=(MemRange const & range)
            { BASE_T::operator=(range); return *this; }

        uintptr_t GetPageCount() const
        {
            uint8_t *pCurPage = ALIGN_DOWN(GetStart(), OS_PAGE_SIZE);
            uint8_t *pEndPage = ALIGN_UP(GetEnd(), OS_PAGE_SIZE);
            return (pEndPage - pCurPage) / OS_PAGE_SIZE;
        }

        uint8_t* GetStartPage() const
            { return ALIGN_DOWN(GetStart(), OS_PAGE_SIZE); }

        // The page immediately following the last page contained by this range.
        uint8_t* GetEndPage() const
            { return ALIGN_UP(GetEnd(), OS_PAGE_SIZE); }

        MemRange GetPageRange() const
            { return MemRange(GetStartPage(), GetEndPage()); }
    };
}// namespace util
}// namespace rh

