//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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

