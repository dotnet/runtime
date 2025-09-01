// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "alloc.h"
#include "jitstd/vector.h"

// Represents a list of segments.
//
// Essentially a segment tree (but not stored as a tree) that supports boolean
// Add/Subtract operations of segments. Used to compute the remainder after
// replacements have been handled as part of a decomposed block operation in
// physical promotion. Also used to store non-padding of class layouts.
//
class SegmentList
{
public:
    struct Segment
    {
        unsigned Start = 0;
        unsigned End   = 0;

        Segment()
        {
        }

        Segment(unsigned start, unsigned end)
            : Start(start)
            , End(end)
        {
        }

        bool IntersectsOrAdjacent(const Segment& other) const;
        bool Intersects(const Segment& other) const;
        bool Contains(const Segment& other) const;
        void Merge(const Segment& other);
    };

private:
    jitstd::vector<Segment> m_segments;

    size_t BinarySearchEnd(unsigned offset) const;
public:
    explicit SegmentList(CompAllocator allocator)
        : m_segments(allocator)
    {
    }

    void Add(const Segment& segment);
    void Subtract(const Segment& segment);
    bool IsEmpty() const;
    bool CoveringSegment(Segment* result) const;
    bool Intersects(const Segment& segment) const;

    jitstd::vector<Segment>::iterator begin()
    {
        return m_segments.begin();
    }

    jitstd::vector<Segment>::iterator end()
    {
        return m_segments.end();
    }

    jitstd::vector<Segment>::const_iterator begin() const
    {
        return m_segments.begin();
    }

    jitstd::vector<Segment>::const_iterator end() const
    {
        return m_segments.end();
    }

#ifdef DEBUG
    void Dump();
#endif
};
