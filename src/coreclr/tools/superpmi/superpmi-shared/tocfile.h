// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// TOCFile.h - Abstraction for reading a TOC file
//----------------------------------------------------------
#ifndef _TOCFile
#define _TOCFile

#include "methodcontext.h"

class TOCElement
{
public:
    int64_t Offset;
    int     Number;
    char    Hash[MM3_HASH_BUFFER_SIZE];

    TOCElement()
    {
    }

    TOCElement(int number, int64_t offset) : Offset(offset), Number(number)
    {
    }
};

class TOCFile
{
private:
    std::vector<TOCElement> m_tocArray;

public:
    TOCFile()
    {
    }

    void LoadToc(const char* inputFileName, bool validate = true);

    size_t GetTocCount()
    {
        return m_tocArray.size();
    }

    const TOCElement* GetElementPtr(size_t i)
    {
        if (i >= m_tocArray.size())
        {
            // error!
            return nullptr;
        }
        return &m_tocArray[i];
    }
};

#endif
