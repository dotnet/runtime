//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source: test2.h
**
** 

**
**=========================================================*/


const WCHAR szChildFileW[] = u"paltest_createprocessw_test2_child";
const WCHAR szArgs[] = {' ',0x41,' ','B',' ','C','\0'};
const WCHAR szArg1[] = {0x41,'\0'};
const WCHAR szArg2[] = {'B','\0'};
const WCHAR szArg3[] = {'C','\0'};

const char *szTestString = "An uninteresting test string (it works though)";

const DWORD EXIT_OK_CODE   = 100;
const DWORD EXIT_ERR_CODE1 = 101;
const DWORD EXIT_ERR_CODE2 = 102;
const DWORD EXIT_ERR_CODE3 = 103;
const DWORD EXIT_ERR_CODE4 = 104;
const DWORD EXIT_ERR_CODE5 = 105;

#define BUF_LEN  128

