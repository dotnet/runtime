// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <windows.h>

typedef void* TADDR;
extern BOOL g_bDacBroken;

#define IsMethodDesc(m) FALSE
#define IsMethodTable(mt) FALSE
#define IsInterrupt() FALSE

#define NameForMT_s(a,b,c)
#define DEBUG_OUTPUT_NORMAL            0x00000001

extern WCHAR g_mdName[1];

struct SOS
{
    HRESULT GetMethodDescName(DWORD_PTR arg, size_t bufferSize, WCHAR* buffer, void*)
    {
        return S_FALSE;
    }
};

extern SOS* g_sos;

#define TO_CDADDR(a) a
#define UL64_TO_CDA(a) ((void*)a)
#define SOS_PTR(a) a
#define TO_TADDR(a) ((char *)a)

struct SYMBOLS
{
    HRESULT GetNameByOffset(DWORD_PTR arg, char *buffer, size_t bufferSize, void*, ULONG64 *displacement)
    {
        return S_FALSE;
    }
};

extern SYMBOLS* g_ExtSymbols;

typedef void* CLRDATA_ADDRESS;

struct DacpMethodDescData
{
    int whatever;
    void Request(void*, CLRDATA_ADDRESS a)
    {
    }
};

struct IDebugDataSpaces
{
    virtual HRESULT ReadVirtual(void* src, void* dest, size_t size, int) = 0;
};

HRESULT OutputVaList(ULONG mask, PCSTR format, va_list args);
void ExtOut(PCSTR format, ...);
#define ___in
void formatOutput(struct IDebugDataSpaces* memCallBack, ___in FILE* file, __inout __inout_z char* format, uint64_t threadId, double timeStamp, DWORD_PTR facility, ___in void** args, bool fPrintFormatString = false);
