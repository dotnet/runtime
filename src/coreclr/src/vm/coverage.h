//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


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
