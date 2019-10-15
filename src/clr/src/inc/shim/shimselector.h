// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// shimselector.h
//
// Class that select the best runtime
//
// Note: very similar to RuntimeSelector but is so simple that adding a common template parent class is not worth it
//
// Note: must be platform independent
//
// ======================================================================================

#ifndef SHIMSELECTOR_H
#define SHIMSELECTOR_H

// does not currently need to add anything to VersionAndLocationInfo
#include "versionandlocationinfo.h"
typedef VersionAndLocationInfo ShimInfo;

class ShimSelector
{
protected:
    VersionInfo m_Baseline;   // base line to compare against
    ShimInfo m_Best;             // best found so far
    bool m_bHasSomething;  // has any data
public:

    //constructor
    ShimSelector();

    // whether the given info is better than the base line
    bool IsAcceptable(const ShimInfo& shimInfo)  const;

    // add shim info
    HRESULT Add(const ShimInfo& shimInfo);

    //set the base line
    void SetBaseline(const VersionInfo& version);

    // get the best found
    ShimInfo GetBest();

    // has any useful data
    bool HasUsefulShimInfo();

    // is 1st better than 2nd
    static bool IsBetter(const ShimInfo& ri1, const ShimInfo& ri2);
};


#include "shimselector.inl"

#endif // SHIMSELECTOR_H
