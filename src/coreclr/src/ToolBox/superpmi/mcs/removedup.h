//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

    RemoveDup(bool cleanup = true)
        : m_inFile(nullptr)
        , m_inFileLegacy(nullptr)
        , m_cleanup(cleanup)
    {}

    ~RemoveDup();

    bool CopyAndRemoveDups(const char* nameOfInput, HANDLE hFileOut, bool stripCR, bool legacyCompare);

private:

    // We use a hash to limit the number of comparisons we need to do.
    // The first level key to our hash map is ILCodeSize and the second
    // level map key is just an index and the value is an existing MC Hash.

    LightWeightMap<int, DenseLightWeightMap<char*>*>*          m_inFile;
    LightWeightMap<int, DenseLightWeightMap<MethodContext*>*>* m_inFileLegacy;

    // If false, we don't spend time cleaning up the `m_inFile` and `m_inFileLegacy`
    // data structures. Only set it to `false` if you're ok with memory leaks, e.g.,
    // if the process will exit soon afterwards.
    bool m_cleanup;

    bool unique(MethodContext* mc);
    bool uniqueLegacy(MethodContext* mc);
};

#endif
