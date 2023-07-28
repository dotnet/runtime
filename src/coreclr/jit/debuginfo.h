// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _DEBUGINFO_H_
#define _DEBUGINFO_H_

#include "jit.h"

class InlineContext;

// Represents information about the location of an IL instruction.
class ILLocation
{
public:
    ILLocation() : m_offset(BAD_IL_OFFSET), m_isStackEmpty(false), m_isCall(false)
    {
    }

    ILLocation(IL_OFFSET offset, bool isStackEmpty, bool isCall)
        : m_offset(offset), m_isStackEmpty(isStackEmpty), m_isCall(isCall)
    {
    }

    IL_OFFSET GetOffset() const
    {
        return m_offset;
    }

    // Is this source location at a stack empty point? We need to be able to
    // report this information back to the debugger since we only allow EnC
    // transitions at stack empty points.
    bool IsStackEmpty() const
    {
        return m_isStackEmpty;
    }

    // Is this a call instruction? Used for managed return values.
    bool IsCall() const
    {
        return m_isCall;
    }

    bool IsValid() const
    {
        return m_offset != BAD_IL_OFFSET;
    }

    inline bool operator==(const ILLocation& other) const
    {
        return (m_offset == other.m_offset) && (m_isStackEmpty == other.m_isStackEmpty) && (m_isCall == other.m_isCall);
    }

    inline bool operator!=(const ILLocation& other) const
    {
        return !(*this == other);
    }

    ICorDebugInfo::SourceTypes EncodeSourceTypes() const;

#ifdef DEBUG
    // Dump textual representation of this ILLocation to jitstdout.
    void Dump() const;
#endif

private:
    IL_OFFSET m_offset;
    bool      m_isStackEmpty : 1;
    bool      m_isCall : 1;
};

// Represents debug information about a statement.
class DebugInfo
{
public:
    DebugInfo() : m_inlineContext(nullptr)
    {
    }

    DebugInfo(InlineContext* inlineContext, ILLocation loc) : m_inlineContext(inlineContext), m_location(loc)
    {
    }

    InlineContext* GetInlineContext() const
    {
        return m_inlineContext;
    }

    ILLocation GetLocation() const
    {
        return m_location;
    }

    // Retrieve information about the location that inlined this statement.
    // Note that there can be associated parent information even when IsValid
    // below returns false.
    bool GetParent(DebugInfo* parent) const;

    // Get debug info in the root. If this debug info is in the root, then
    // returns *this. Otherwise returns information of the call in the root
    // that eventually produced this statement through inlines.
    DebugInfo GetRoot() const;

#ifdef DEBUG
    void Validate() const;
#else
    void Validate() const
    {
    }
#endif

#ifdef DEBUG
    // Dump textual representation of this DebugInfo to jitstdout.
    void Dump(bool recurse) const;
#endif

    // Check if this debug info has both a valid inline context and valid
    // location.
    bool IsValid() const
    {
        return m_inlineContext != nullptr && m_location.IsValid();
    }

    inline bool operator==(const DebugInfo& other) const
    {
        return (m_inlineContext == other.m_inlineContext) && (m_location == other.m_location);
    }

    inline bool operator!=(const DebugInfo& other) const
    {
        return !(*this == other);
    }

private:
    InlineContext* m_inlineContext;
    ILLocation     m_location;
};

#endif
