// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// versionandlocationinfo.inl
//
// simple accessors for the struct encapsulating version# and location code
// 
// Note: must be platform independent
//
// ======================================================================================


inline VersionAndLocationInfo::VersionAndLocationInfo(): 
    m_Version(0,0,0,0),
    m_Location(Loc_Undefined)
{
};

inline VersionAndLocationInfo::VersionAndLocationInfo(const VersionInfo& version, const LocationInfo location): 
    m_Version(version),
    m_Location(location)
{
};

inline VersionInfo VersionAndLocationInfo::Version()  const 
{
    return m_Version;
};

inline LocationInfo VersionAndLocationInfo::Location()  const 
{
    return m_Location;
};

