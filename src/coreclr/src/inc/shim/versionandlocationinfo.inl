//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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

