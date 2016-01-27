// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _SNCORECLR_H
#define _SNCORECLR_H

#if !defined(FEATURE_CORECLR)
#error sncoreclr.h should only be used on CoreCLR builds
#endif // !FEATURE_CORECLR

void InitUtilcode();

#endif // _SNCORECLR_H
