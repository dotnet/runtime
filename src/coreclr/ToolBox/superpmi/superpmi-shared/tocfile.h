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
    __int64 Offset;
    int     Number;
    char    Hash[MD5_HASH_BUFFER_SIZE];

    TOCElement()
    {
    }

    TOCElement(int number, __int64 offset) : Offset(offset), Number(number)
    {
    }
};

class TOCFile
{
private:
    TOCElement* m_tocArray;
    size_t      m_tocCount;

public:
    TOCFile() : m_tocArray(nullptr), m_tocCount(0)
    {
    }

    ~TOCFile()
    {
        Clear();
    }

    void Clear()
    {
        delete[] m_tocArray;
        m_tocArray = nullptr;
        m_tocCount = 0;
    }

    void LoadToc(const char* inputFileName, bool validate = true);

    size_t GetTocCount()
    {
        return m_tocCount;
    }

    const TOCElement* GetElementPtr(size_t i)
    {
        if (i >= m_tocCount)
        {
            // error!
            return nullptr;
        }
        return &m_tocArray[i];
    }
};

#endif
