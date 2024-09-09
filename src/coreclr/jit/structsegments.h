// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "alloc.h"
#include "jitstd/vector.h"

// Represents significant segments of a struct operation.
//
// Essentially a segment tree (but not stored as a tree) that supports boolean
// Add/Subtract operations of segments. Used to compute the remainder after
// replacements have been handled as part of a decomposed block operation.
class StructSegments
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

public:
    explicit StructSegments(CompAllocator allocator)
        : m_segments(allocator)
    {
    }

    void Add(const Segment& segment);
    void Subtract(const Segment& segment);
    bool IsEmpty() const;
    bool CoveringSegment(Segment* result) const;
    bool Intersects(const Segment& segment) const;

#ifdef DEBUG
    void Dump();
#endif
};
