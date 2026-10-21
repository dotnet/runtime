// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef ALLOCATION_NOTIFICATION_TEST_H
#define ALLOCATION_NOTIFICATION_TEST_H

#ifdef FEATURE_ALLOCATION_NOTIFICATION_TEST

AllocationCompleteCallback GetAllocationNotificationTestCallback();
void* GetAllocationNotificationTestContext();

#endif // FEATURE_ALLOCATION_NOTIFICATION_TEST

#endif // ALLOCATION_NOTIFICATION_TEST_H
