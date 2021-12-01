// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// RemoveDup.h - Functions to remove dups from a method context hive (MCH)
//----------------------------------------------------------
#ifndef _RemoveDup
#define _RemoveDup

#include "methodcontext.h"
#include "lightweightmap.h"

class RemoveDup
{
public:

    RemoveDup()
        : m_stripCR(false)
        , m_legacyCompare(false)
        , m_cleanup(false)
        , m_inFile(nullptr)
        , m_inFileLegacy(nullptr)
    {}

    bool Initialize(bool stripCR = false, bool legacyCompare = false, bool cleanup = true)
    {
        m_stripCR       = stripCR;
        m_legacyCompare = legacyCompare;
        m_cleanup       = cleanup;
        m_inFile        = nullptr;
        m_inFileLegacy  = nullptr;

        return true;
    }

    ~RemoveDup();

    bool CopyAndRemoveDups(const char* nameOfInput, HANDLE hFileOut);

private:

    bool m_stripCR;       // 'true' if we remove CompileResults when removing duplicates.
    bool m_legacyCompare; // 'true' to use the legacy comparer.

    // If false, we don't spend time cleaning up the `m_inFile` and `m_inFileLegacy`
    // data structures. Only set it to `false` if you're ok with memory leaks, e.g.,
    // if the process will exit soon afterwards.
    bool m_cleanup;

    // We use a hash to limit the number of comparisons we need to do.
    // The first level key to our hash map is ILCodeSize and the second
    // level map key is just an index and the value is an existing MC Hash.

    LightWeightMap<int, DenseLightWeightMap<char*>*>*          m_inFile;
    LightWeightMap<int, DenseLightWeightMap<MethodContext*>*>* m_inFileLegacy;

    bool unique(MethodContext* mc);
    bool uniqueLegacy(MethodContext* mc);
};

#endif
