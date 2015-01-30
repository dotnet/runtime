//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _SNCORECLR_H
#define _SNCORECLR_H

#if !defined(FEATURE_CORECLR)
#error sncoreclr.h should only be used on CoreCLR builds
#endif // !FEATURE_CORECLR

void InitUtilcode();

#endif // _SNCORECLR_H
