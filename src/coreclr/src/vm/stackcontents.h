// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __STACKCONTENTS_H__
#define __STACKCONTENTS_H__

#ifdef FEATURE_PERFTRACING
#include "common.h"

class MethodDesc;

class StackContents
{
private:
    const static unsigned int MAX_STACK_DEPTH = 100;

    // Array of IP values from a stack crawl.
    // Top of stack is at index 0.
    UINT_PTR m_stackFrames[MAX_STACK_DEPTH];

#ifdef _DEBUG
    // Parallel array of MethodDesc pointers.
    // Used for debug-only stack printing.
    MethodDesc *m_methods[MAX_STACK_DEPTH];
#endif // _DEBUG

    // The next available slot in StackFrames.
    unsigned int m_nextAvailableFrame;

public:
    StackContents()
    {
        LIMITED_METHOD_CONTRACT;
        Reset();
    }

    void CopyTo(StackContents *pDest)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pDest != NULL);

        memcpy_s(pDest->m_stackFrames, MAX_STACK_DEPTH * sizeof(UINT_PTR), m_stackFrames, sizeof(UINT_PTR) * m_nextAvailableFrame);
#ifdef _DEBUG
        memcpy_s(pDest->m_methods, MAX_STACK_DEPTH * sizeof(MethodDesc *), m_methods, sizeof(MethodDesc *) * m_nextAvailableFrame);
#endif
        pDest->m_nextAvailableFrame = m_nextAvailableFrame;
    }

    void Reset()
    {
        LIMITED_METHOD_CONTRACT;
        m_nextAvailableFrame = 0;
    }

    bool IsEmpty()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_nextAvailableFrame == 0);
    }

    unsigned int GetLength()
    {
        LIMITED_METHOD_CONTRACT;
        return m_nextAvailableFrame;
    }

    UINT_PTR GetIP(unsigned int frameIndex)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(frameIndex < MAX_STACK_DEPTH);

        if (frameIndex >= MAX_STACK_DEPTH)
        {
            return 0;
        }

        return m_stackFrames[frameIndex];
    }

#ifdef _DEBUG
    MethodDesc *GetMethod(unsigned int frameIndex)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(frameIndex < MAX_STACK_DEPTH);

        if (frameIndex >= MAX_STACK_DEPTH)
        {
            return NULL;
        }

        return m_methods[frameIndex];
    }
#endif // _DEBUG

    void Append(UINT_PTR controlPC, MethodDesc *pMethod)
    {
        LIMITED_METHOD_CONTRACT;

        if (m_nextAvailableFrame < MAX_STACK_DEPTH)
        {
            m_stackFrames[m_nextAvailableFrame] = controlPC;
#ifdef _DEBUG
            m_methods[m_nextAvailableFrame] = pMethod;
#endif
            m_nextAvailableFrame++;
        }
    }

    BYTE *GetPointer() const
    {
        LIMITED_METHOD_CONTRACT;
        return (BYTE *)m_stackFrames;
    }

    unsigned int GetSize() const
    {
        LIMITED_METHOD_CONTRACT;
        return (m_nextAvailableFrame * sizeof(UINT_PTR));
    }
};

#endif // FEATURE_PERFTRACING

#endif // __STACKCONTENTS_H__
