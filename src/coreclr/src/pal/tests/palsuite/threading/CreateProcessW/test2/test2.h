// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test2.h
**
** 

**
**=========================================================*/


const WCHAR szChildFileW[] = u"threading/CreateProcessW/test2/paltest_createprocessw_test2_child";
const WCHAR szArgs[] = {' ',0x41,' ','B',' ','C','\0'};
const WCHAR szArg1[] = {0x41,'\0'};
const WCHAR szArg2[] = {'B','\0'};
const WCHAR szArg3[] = {'C','\0'};

#define szTestString "An uninteresting test string (it works though)"

const DWORD EXIT_OK_CODE   = 100;
const DWORD EXIT_ERR_CODE1 = 101;
const DWORD EXIT_ERR_CODE2 = 102;
const DWORD EXIT_ERR_CODE3 = 103;
const DWORD EXIT_ERR_CODE4 = 104;
const DWORD EXIT_ERR_CODE5 = 105;

#define BUF_LEN  128

