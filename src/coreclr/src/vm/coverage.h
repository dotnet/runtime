// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _COVERAGE_H_
#define _COVERAGE_H_

// Please see coverage.cpp for info on this file
class COMCoverage 
{
public:
    //typedef struct 
    //{
    //    DECLARE_ECALL_I4_ARG(INT32, id);
    //} _CoverageArgs;
    static FCDECL1(unsigned __int64, nativeCoverBlock, INT32 id);
};
#endif // _COVERAGE_H_
