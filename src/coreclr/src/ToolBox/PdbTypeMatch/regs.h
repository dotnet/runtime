//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

extern const wchar_t * const rgRegX86[];
extern const wchar_t * const rgRegAMD64[];
extern const wchar_t * const rgRegMips[];
extern const wchar_t * const rgReg68k[];
extern const wchar_t * const rgRegAlpha[];
extern const wchar_t * const rgRegPpc[];
extern const wchar_t * const rgRegSh[];
extern const wchar_t * const rgRegArm[];

typedef struct MapIa64Reg{
    CV_HREG_e  iCvReg;
    const wchar_t* wszRegName;
}MapIa64Reg;
extern const MapIa64Reg mpIa64regSz[];
int __cdecl cmpIa64regSz( const void* , const void* );

extern DWORD g_dwMachineType;
const wchar_t* SzNameC7Reg( USHORT , DWORD );
const wchar_t* SzNameC7Reg( USHORT );
