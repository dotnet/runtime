// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// activitytracker.h
//

#ifndef __ACTIVITY_TRACKER_H__
#define __ACTIVITY_TRACKER_H__

namespace ActivityTracker
{
    void Start(/*out*/ GUID *activityId, /*out*/ GUID *relatedActivityId);
    void Stop(/*out*/ GUID *activityId);
};

#endif // __ACTIVITY_TRACKER_H__
