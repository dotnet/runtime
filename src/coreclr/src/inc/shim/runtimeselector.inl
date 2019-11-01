// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// runtimeselector.inl
//
// implementation that select the best runtime
// 
// Note: must be platform independent
//
// ======================================================================================

// Constructor
//
// Simply marks the fact that no data is set
inline RuntimeSelector::RuntimeSelector()
{
    m_bHasSomething=false;
}

// Sets the runtime version requested
//
// Input:
// version - version to set
inline void RuntimeSelector::SetRequestedVersion(const VersionInfo& version)
{
    m_RequestedVersion=version;
};


// Returns whether the given runtime can be used
//
// Input:
// runtimeInfo - the runtime info
//
// Output: 
// return value - true: can be used, false: cannot be used
inline bool RuntimeSelector::IsAcceptable(const  RuntimeInfo& runtimeInfo)  const 
{
    return ( m_RequestedVersion.Major() ==  runtimeInfo.Version().Major() &&
                  m_RequestedVersion.Minor() ==  runtimeInfo.Version().Minor());
}


// Returns add the given runtime to the potentially used runtimes list
//
// Input:
// runtimeInfo - the runtime info
//
// Output: 
// return value - S_OK added, S_FALSE not added (not a failure) or a failure code
inline HRESULT RuntimeSelector::Add(const RuntimeInfo& runtimeInfo)
{
    HRESULT hr = S_FALSE;
    if(!IsAcceptable(runtimeInfo))
        return S_FALSE;

    if(!m_bHasSomething || IsBetter(runtimeInfo,m_Best))
    {
        m_Best=runtimeInfo;
        hr=S_OK;        
    }

    m_bHasSomething=true;
    return hr;
};

// Returns add the best runtime of the options given (see Add)
//
// Output: 
// return value - the best option
inline RuntimeInfo RuntimeSelector::GetBest()
{
    return m_Best;
};

// Returns whether we have any usable choices
//
// Output: 
// return value - true if the has something usable 
inline bool RuntimeSelector::HasUsefulRuntimeInfo()
{
    return m_bHasSomething;
};

// Compares two given options
//
// Input: 
// ri1, ri2 - runtimes to compare
//
// Output: 
// return value - true if ri1 is better than ri2
inline bool RuntimeSelector::IsBetter(const RuntimeInfo& ri1, const RuntimeInfo& ri2) 
{
    switch(ri1.Version().Compare(ri2.Version()))
    {
        case -1: return false;
        case 1: return true;
        default:
        return (BetterLocation(ri1.Location(),ri2.Location())==ri1.Location());
    }
};



