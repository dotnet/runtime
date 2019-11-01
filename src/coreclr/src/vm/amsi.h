// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: amsi.h
//

#ifndef __AMSI_H__
#define __AMSI_H__

namespace Amsi
{
    bool IsBlockedByAmsiScan(void *flatImageBytes, COUNT_T size);
};

#endif // __AMSI_H__
