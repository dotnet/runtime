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
    ILLocation()
    {
    }

    ILLocation(IL_OFFSET offset, ICorDebugInfo::SourceTypes sourceTypes)
        : m_offset(offset)
        , m_sourceTypes(sourceTypes)
    {
    }

    IL_OFFSET GetOffset() const
    {
        return m_offset;
    }

    ICorDebugInfo::SourceTypes GetSourceTypes() const
    {
        return m_sourceTypes;
    }

    bool IsCallInstruction() const
    {
        return (m_sourceTypes & ICorDebugInfo::CALL_INSTRUCTION) != 0;
    }

    bool IsAsync() const
    {
        return (m_sourceTypes & ICorDebugInfo::ASYNC) != 0;
    }

    bool IsValid() const
    {
        return m_offset != BAD_IL_OFFSET;
    }

    inline bool operator==(const ILLocation& other) const
    {
        return (m_offset == other.m_offset) && (m_sourceTypes == other.m_sourceTypes);
    }

    inline bool operator!=(const ILLocation& other) const
    {
        return !(*this == other);
    }

#ifdef DEBUG
    // Dump textual representation of this ILLocation to jitstdout.
    void Dump() const;
#endif

private:
    IL_OFFSET                  m_offset      = BAD_IL_OFFSET;
    ICorDebugInfo::SourceTypes m_sourceTypes = ICorDebugInfo::SOURCE_TYPE_INVALID;
};

// Represents debug information about a statement.
class DebugInfo
{
public:
    DebugInfo()
        : m_inlineContext(nullptr)
    {
    }

    DebugInfo(InlineContext* inlineContext, ILLocation loc)
        : m_inlineContext(inlineContext)
        , m_location(loc)
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
