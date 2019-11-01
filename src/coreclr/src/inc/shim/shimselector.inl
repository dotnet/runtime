// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// shimselector.inl
//
// implementation that select the best shim
// 
// Note: must be platform independent
//
// ======================================================================================



// Constructor
//
// Simply marks the fact that no data is set
inline ShimSelector::ShimSelector()
{
    m_bHasSomething=false;
}

// Sets the base line to compare against
//
// Input:
// version - the version info
inline void ShimSelector::SetBaseline(const VersionInfo& version)
{
    m_Baseline=version;
}

// Checks whether the shim is better that the base (the current one)
//
// Input:
// shimInfo - the shim info
//
// Output: 
// return value - true: better, false: worse or the same
inline bool ShimSelector::IsAcceptable(const  ShimInfo& shimInfo)  const 
{
    return ( m_Baseline.Compare(shimInfo.Version()) < 0 );
}

// Returns add the given shim to the potentially used shim list
//
// Input:
// shimInfo - the shim info
//
// Output: 
// return value - S_OK added, S_FALSE not added (not a failure) or a failure code
inline HRESULT ShimSelector::Add(const ShimInfo& shimInfo)
{
    HRESULT hr = S_FALSE;
    if(!IsAcceptable(shimInfo))
        return S_FALSE;

    if(!m_bHasSomething || IsBetter(shimInfo,m_Best))
    {
            m_Best=shimInfo;
            hr=S_OK;
    }

    m_bHasSomething=true;
    return hr;
};

// Returns add the best shim of the options given (see Add)
//
// Output: 
// return value - the best option
inline ShimInfo ShimSelector::GetBest()
{
    return m_Best;
};

// Returns whether we have any usable choices
//
// Output: 
// return value - true if the has something usable 
inline bool ShimSelector::HasUsefulShimInfo()
{
    return m_bHasSomething;
};

// Compares two given options
//
// Input: 
// si1, si2 - shims to compare
//
// Output: 
// return value - true if si1 is better than si2
inline bool ShimSelector::IsBetter(const ShimInfo& si1, const ShimInfo& si2) 
{
    switch(si1.Version().Compare(si2.Version()))
    {
        case -1: return false;
        case 1: return true;
        default:
        return (BetterLocation(si1.Location(),si2.Location())==si1.Location());
    }
};



