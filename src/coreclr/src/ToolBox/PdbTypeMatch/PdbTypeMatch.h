// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "dia2.h"
#include <set>

extern const wchar_t *g_szFilename;
extern IDiaDataSource *g_pDiaDataSource;
extern IDiaSession *g_pDiaSession1, *g_pDiaSession2;
extern IDiaSymbol *g_pGlobalSymbol1, *g_pGlobalSymbol2;
extern DWORD g_dwMachineType;

typedef std::set<std::wstring> IDiaSymbolSet;


void PrintHelpOptions();
bool ParseArg(int , wchar_t *[]);

bool InitDiaSource(IDiaDataSource  **ppSource);
void Cleanup();
bool LoadDataFromPdb(const wchar_t *, IDiaDataSource *, IDiaSession **, IDiaSymbol **);


bool EnumTypesInPdb(IDiaSymbolSet* types, IDiaSession *pSession, IDiaSymbol *pGlobal);
bool LayoutMatches(IDiaSymbol* pSymbol1, IDiaSymbol* pSymbol2);

LPSTR UnicodeToAnsi(LPCWSTR s);
void DumpAllPdbInfo(IDiaSession *, IDiaSymbol *);
bool DumpAllMods(IDiaSymbol *);
bool DumpAllPublics(IDiaSymbol *);
bool DumpCompiland(IDiaSymbol *, const wchar_t *);
bool DumpAllSymbols(IDiaSymbol *);
bool DumpAllGlobals(IDiaSymbol *);
bool DumpAllTypes(IDiaSymbol *);
bool DumpAllUDTs(IDiaSymbol *);
bool DumpAllEnums(IDiaSymbol *);
bool DumpAllTypedefs(IDiaSymbol *);
bool DumpAllOEMs(IDiaSymbol *);
bool DumpAllFiles(IDiaSession *, IDiaSymbol *);
bool DumpAllLines(IDiaSession *, IDiaSymbol *);
bool DumpAllLines(IDiaSession *, DWORD, DWORD);
bool DumpAllSecContribs(IDiaSession *);
bool DumpAllDebugStreams(IDiaSession *);
bool DumpAllInjectedSources(IDiaSession *);
bool DumpInjectedSource(IDiaSession *, const wchar_t *);
bool DumpAllSourceFiles(IDiaSession *, IDiaSymbol *);
bool DumpAllFPO(IDiaSession *);
bool DumpFPO(IDiaSession *, DWORD);
bool DumpFPO(IDiaSession *, IDiaSymbol *, const wchar_t *);
bool DumpSymbolWithRVA(IDiaSession *, DWORD, const wchar_t *);
bool DumpSymbolsWithRegEx(IDiaSymbol *, const wchar_t *, const wchar_t *);
bool DumpSymbolWithChildren(IDiaSymbol *, const wchar_t *);
bool DumpLines(IDiaSession *, DWORD);
bool DumpLines(IDiaSession *, IDiaSymbol *, const wchar_t *);
bool DumpType(IDiaSymbol *, const wchar_t *);
bool DumpLinesForSourceFile(IDiaSession *, const wchar_t *, DWORD);
bool DumpPublicSymbolsSorted(IDiaSession *, DWORD, DWORD, bool);
bool DumpLabel(IDiaSession *, DWORD);
bool DumpAnnotations(IDiaSession *, DWORD);
bool DumpMapToSrc(IDiaSession *, DWORD);
bool DumpMapFromSrc(IDiaSession *, DWORD);

HRESULT GetTable(IDiaSession *, REFIID, void **);

///////////////////////////////////////////////////////////////////
// Functions defined in regs.cpp
const wchar_t *SzNameC7Reg(USHORT, DWORD);
const wchar_t *SzNameC7Reg(USHORT);
