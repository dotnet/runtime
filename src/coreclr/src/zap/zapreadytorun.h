// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ZapReadyToRun.h
//

//
// Zapping of ready-to-run specific structures
// 
// ======================================================================================

#ifndef __ZAPREADYTORUN_H__
#define __ZAPREADYTORUN_H__

#include "readytorun.h"

class ZapReadyToRunHeader : public ZapNode
{
    struct Section
    {
        DWORD       type;
        ZapNode *   pSection;
    };

    SArray<Section> m_Sections;

    static int __cdecl SectionCmp(const void* a_, const void* b_)
    {
        return ((Section*)a_)->type - ((Section*)b_)->type;
    }

public:
    ZapReadyToRunHeader(ZapImage * pImage)
    {
    }

    void RegisterSection(DWORD type, ZapNode * pSection)
    {
        Section section;
        section.type = type;
        section.pSection = pSection;
        m_Sections.Append(section);
    }

    virtual DWORD GetSize()
    {
        return sizeof(READYTORUN_HEADER) + sizeof(READYTORUN_SECTION) * m_Sections.GetCount();
    }

    virtual UINT GetAlignment()
    {
        return sizeof(DWORD);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_NativeHeader;
    }

    virtual void Save(ZapWriter * pZapWriter);
};

#endif // __ZAPREADYTORUN_H__
