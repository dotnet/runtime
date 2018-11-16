// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

struct StructWithSHFld
{
    HANDLE hnd;
};

struct StructNestedOneDeep
{
    StructWithSHFld s;
};

struct StructNestedParent
{
    StructNestedOneDeep snOneDeep;
};

struct StructWithSHArrayFld
{
    HANDLE* sharr;
};

struct StructWithManySHFlds
{
    HANDLE hnd1;
    HANDLE hnd2;
    HANDLE hnd3;

    HANDLE hnd4;
    HANDLE hnd5;
    HANDLE hnd6;

    HANDLE hnd7;
    HANDLE hnd8;
    HANDLE hnd9;

    HANDLE hnd10;
    HANDLE hnd11;
    HANDLE hnd12;

    HANDLE hnd13;
    HANDLE hnd14;
    HANDLE hnd15;
};

struct StructMAIntf
{
    IDispatch* ptoIntf;
};

struct StructWithVARIANTFld
{
    VARIANT v;
};