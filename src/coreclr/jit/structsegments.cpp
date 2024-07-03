// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "structsegments.h"
#include "promotion.h"

//------------------------------------------------------------------------
// IntersectsOrAdjacent:
//   Check if this segment intersects or is adjacent to another segment.
//
// Parameters:
//   other - The other segment.
//
// Returns:
//    True if so.
//
bool StructSegments::Segment::IntersectsOrAdjacent(const Segment& other) const
{
    if (End < other.Start)
    {
        return false;
    }

    if (other.End < Start)
    {
        return false;
    }

    return true;
}

//------------------------------------------------------------------------
// Intersects:
//   Check if this segment intersects another segment.
//
// Parameters:
//   other - The other segment.
//
// Returns:
//    True if so.
//
bool StructSegments::Segment::Intersects(const Segment& other) const
{
    if (End <= other.Start)
    {
        return false;
    }

    if (other.End <= Start)
    {
        return false;
    }

    return true;
}

//------------------------------------------------------------------------
// Contains:
//   Check if this segment contains another segment.
//
// Parameters:
//   other - The other segment.
//
// Returns:
//    True if so.
//
bool StructSegments::Segment::Contains(const Segment& other) const
{
    return (other.Start >= Start) && (other.End <= End);
}

//------------------------------------------------------------------------
// Merge:
//   Update this segment to also contain another segment.
//
// Parameters:
//   other - The other segment.
//
void StructSegments::Segment::Merge(const Segment& other)
{
    Start = min(Start, other.Start);
    End   = max(End, other.End);
}

//------------------------------------------------------------------------
// Add:
//   Add a segment to the data structure.
//
// Parameters:
//   segment - The segment to add.
//
void StructSegments::Add(const Segment& segment)
{
    size_t index = Promotion::BinarySearch<Segment, &Segment::End>(m_segments, segment.Start);

    if ((ssize_t)index < 0)
    {
        index = ~index;
    }

    m_segments.insert(m_segments.begin() + index, segment);
    size_t endIndex;
    for (endIndex = index + 1; endIndex < m_segments.size(); endIndex++)
    {
        if (!m_segments[index].IntersectsOrAdjacent(m_segments[endIndex]))
        {
            break;
        }

        m_segments[index].Merge(m_segments[endIndex]);
    }

    m_segments.erase(m_segments.begin() + index + 1, m_segments.begin() + endIndex);
}

//------------------------------------------------------------------------
// Subtract:
//   Subtract a segment from the data structure.
//
// Parameters:
//   segment - The segment to subtract.
//
void StructSegments::Subtract(const Segment& segment)
{
    size_t index = Promotion::BinarySearch<Segment, &Segment::End>(m_segments, segment.Start);
    if ((ssize_t)index < 0)
    {
        index = ~index;
    }
    else
    {
        // Start == segment[index].End, which makes it non-interesting.
        index++;
    }

    if (index >= m_segments.size())
    {
        return;
    }

    // Here we know Start < segment[index].End. Do they not intersect at all?
    if (m_segments[index].Start >= segment.End)
    {
        // Does not intersect any segment.
        return;
    }

    assert(m_segments[index].Intersects(segment));

    if (m_segments[index].Contains(segment))
    {
        if (segment.Start > m_segments[index].Start)
        {
            // New segment (existing.Start, segment.Start)
            if (segment.End < m_segments[index].End)
            {
                m_segments.insert(m_segments.begin() + index, Segment(m_segments[index].Start, segment.Start));

                // And new segment (segment.End, existing.End)
                m_segments[index + 1].Start = segment.End;
                return;
            }

            m_segments[index].End = segment.Start;
            return;
        }
        if (segment.End < m_segments[index].End)
        {
            // New segment (segment.End, existing.End)
            m_segments[index].Start = segment.End;
            return;
        }

        // Full segment is being removed
        m_segments.erase(m_segments.begin() + index);
        return;
    }

    if (segment.Start > m_segments[index].Start)
    {
        m_segments[index].End = segment.Start;
        index++;
    }

    size_t endIndex = Promotion::BinarySearch<Segment, &Segment::End>(m_segments, segment.End);
    if ((ssize_t)endIndex >= 0)
    {
        m_segments.erase(m_segments.begin() + index, m_segments.begin() + endIndex + 1);
        return;
    }

    endIndex = ~endIndex;
    if (endIndex == m_segments.size())
    {
        m_segments.erase(m_segments.begin() + index, m_segments.end());
        return;
    }

    if (segment.End > m_segments[endIndex].Start)
    {
        m_segments[endIndex].Start = segment.End;
    }

    m_segments.erase(m_segments.begin() + index, m_segments.begin() + endIndex);
}

//------------------------------------------------------------------------
// IsEmpty:
//   Check if the segment tree is empty.
//
// Returns:
//   True if so.
//
bool StructSegments::IsEmpty() const
{
    return m_segments.size() == 0;
}

//------------------------------------------------------------------------
// CoveringSegment:
//   Compute a segment that covers all contained segments in this segment tree.
//
// Parameters:
//   result - [out] The single segment. Only valid if the method returns true.
//
// Returns:
//   True if this segment tree was non-empty; otherwise false.
//
bool StructSegments::CoveringSegment(Segment* result) const
{
    if (m_segments.size() == 0)
    {
        return false;
    }

    result->Start = m_segments[0].Start;
    result->End   = m_segments[m_segments.size() - 1].End;
    return true;
}

//------------------------------------------------------------------------
// Intersects:
//   Check if a segment intersects with any segment in this segment tree.
//
// Parameters:
//   segment - The segment.
//
// Returns:
//   True if the input segment intersects with any segment in the tree;
//   otherwise false.
//
bool StructSegments::Intersects(const Segment& segment) const
{
    size_t index = Promotion::BinarySearch<Segment, &Segment::End>(m_segments, segment.Start);
    if ((ssize_t)index < 0)
    {
        index = ~index;
    }
    else
    {
        // Start == segment[index].End, which makes it non-interesting.
        index++;
    }

    if (index >= m_segments.size())
    {
        return false;
    }

    // Here we know Start < segment[index].End. Do they not intersect at all?
    if (m_segments[index].Start >= segment.End)
    {
        // Does not intersect any segment.
        return false;
    }

    assert(m_segments[index].Intersects(segment));
    return true;
}

#ifdef DEBUG
//------------------------------------------------------------------------
// Dump:
//   Dump a string representation of the segment tree to stdout.
//
void StructSegments::Dump()
{
    if (m_segments.size() == 0)
    {
        printf("<empty>");
    }
    else
    {
        const char* sep = "";
        for (const Segment& segment : m_segments)
        {
            printf("%s[%03u..%03u)", sep, segment.Start, segment.End);
            sep = " ";
        }
    }
}
#endif

