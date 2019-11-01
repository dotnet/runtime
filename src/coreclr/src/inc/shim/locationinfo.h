// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// locationinfo.h
//
// Enum describing different types of locations for coreCLR
// 
// Note: must be platform independent
//
// ======================================================================================


#ifndef LOCATIONINFO_H
#define LOCATIONINFO_H


// in order of preference, smaller is better
enum LocationInfo
{
    Loc_System=1,
    Loc_Machine=2,
    Loc_User=3,
    Loc_Network=4,
    Loc_Undefined =0xffff
};

// Returns the more preferred of two locations
//
// Assumptions: LocationInfo is defined in a manner that a smaller value is better
//
// Input:
// locations to compare
//
// Output: 
// the preferred location
inline LocationInfo BetterLocation(LocationInfo l1, LocationInfo l2)
{
    return l1<l2?l1:l2;
};

#endif //  LOCATIONINFO_H
