// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// runtimeselector.h
//
// Class that select the best runtime
//
// Note: very similar to ShimSelector but is so simple that adding a common template parent class is not worth it
//
// Note: must be platform independent
//
// ======================================================================================


#ifndef RUNTIMESELECTOR_H
#define RUNTIMESELECTOR_H

// does not currently need to add anything to VersionAndLocationInfo
#include "versionandlocationinfo.h"
typedef VersionAndLocationInfo RuntimeInfo;

class RuntimeSelector
{
protected:
    VersionInfo m_RequestedVersion; // requested version
    RuntimeInfo m_Best;       // the best option so far
    bool m_bHasSomething;             // has any data
public:
    //constructor
    RuntimeSelector();

    // whether the given info compatible with the request
    bool IsAcceptable(const RuntimeInfo& runtimeInfo)  const;

    // add runtime info
    HRESULT Add(const RuntimeInfo& runtimeInfo);

    // set the version requested
    void SetRequestedVersion(const VersionInfo& version);

    // get the best found
    RuntimeInfo GetBest();

    // has any useful data
    bool HasUsefulRuntimeInfo();
    
    // is 1st better than 2nd
    static bool IsBetter(const RuntimeInfo& ri1, const RuntimeInfo& ri2);
};

#include "runtimeselector.inl"

#endif // RUNTIMESELECTOR_H

