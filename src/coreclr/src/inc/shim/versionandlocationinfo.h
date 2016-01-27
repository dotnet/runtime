// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// versionandlocationinfo.h
//
// a simple struct encapsulating version# and location code
// 
// Note: must be platform independent
//
// ======================================================================================


#ifndef VERSIONANDLOCATIONINFO_H
#define VERSIONANDLOCATIONINFO_H

#include "versioninfo.h"
#include "locationinfo.h"

struct VersionAndLocationInfo
{
protected:
    VersionInfo m_Version;
    LocationInfo m_Location;
public:
    VersionAndLocationInfo();
    VersionAndLocationInfo(const VersionInfo& version, const LocationInfo location);
    VersionInfo Version()  const;
    LocationInfo Location() const ;
};


#include "versionandlocationinfo.inl"

#endif // VERSIONANDLOCATIONINFO_H

