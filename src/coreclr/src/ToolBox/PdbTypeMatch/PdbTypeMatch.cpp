// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "stdafx.h"
#include "PdbTypeMatch.h"
#include "PrintSymbol.h"

#include "Callback.h"

#include <fstream>
#include <iostream>
#include <sstream>
#include <sys/stat.h>
#include <algorithm>
#include "Shlwapi.h"

#define DEBUG_VERBOSE 0

#pragma warning (disable : 4100)

const wchar_t *g_szFilename1, *g_szFilename2;
IDiaDataSource *g_pDiaDataSource1, *g_pDiaDataSource2;
IDiaSession *g_pDiaSession1, *g_pDiaSession2;
IDiaSymbol *g_pGlobalSymbol1, *g_pGlobalSymbol2;
DWORD g_dwMachineType = CV_CFL_80386;
IDiaSymbolSet g_excludedTypes;
IDiaSymbolSet g_excludedTypePatterns;

////////////////////////////////////////////////////////////
//
int __cdecl wmain(int argc, wchar_t *argv[])
{
  FILE *pFile;

  if (argc < 3) {
    PrintHelpOptions();
    return -1;
  }

  if (!_wcsicmp(argv[1], L"-type")) {
    
    // -type <symbolname> <pdbfilename>: dump this type in detail
    
    if (argc < 4) {
      PrintHelpOptions();
      return -1;
    }
    
    if ((argc > 1) && (*argv[2] != L'-')) {
    
        if (_wfopen_s(&pFile, argv[3], L"r") || !pFile) {
        // invalid file name or file does not exist
    
        PrintHelpOptions();
        return -1;
      }
      fclose(pFile);
      // CoCreate() and initialize COM objects
      if (!InitDiaSource(&g_pDiaDataSource1))
      {
          return -1;
      }
      if (!LoadDataFromPdb(argv[3], g_pDiaDataSource1, &g_pDiaSession1, &g_pGlobalSymbol1)) {
        return -1;
      }
  
      DumpType(g_pGlobalSymbol1, argv[2]);
      
      // release COM objects and CoUninitialize()
      Cleanup();
      
      return 0;
    }
  }

  if (argc < 3) {
    PrintHelpOptions();
    return -1;
  }
  
  if (_wfopen_s(&pFile, argv[1], L"r") || !pFile) {
    // invalid file name or file does not exist

    PrintHelpOptions();
    return -1;
  }

  fclose(pFile);

  if (_wfopen_s(&pFile, argv[2], L"r") || !pFile) {
    // invalid file name or file does not exist

    PrintHelpOptions();
    return -1;
  }

  fclose(pFile);

  g_szFilename1 = argv[1];

  // CoCreate() and initialize COM objects
  if (!InitDiaSource(&g_pDiaDataSource1))
  {
      return -1;
  }
  if (!LoadDataFromPdb(g_szFilename1, g_pDiaDataSource1, &g_pDiaSession1, &g_pGlobalSymbol1)) {
    return -1;
  }

  g_szFilename2 = argv[2];

  InitDiaSource(&g_pDiaDataSource2);
  if (!LoadDataFromPdb(g_szFilename2, g_pDiaDataSource2, &g_pDiaSession2, &g_pGlobalSymbol2)) {
    return -1;
  }
  
  // Read exclusion list.
  struct stat fileStatus;  
  if (stat(UnicodeToAnsi(argv[3]), &fileStatus) != 0)
  {
    wprintf(L"Could not open type_exclusion_list file!\n"); 
    return -1;
  }
 
  char linec[2048];  
  FILE *file = fopen(UnicodeToAnsi(argv[3]), "r");
  while (fgets (linec, sizeof(linec), file) != NULL)
  {
      std::string line(linec);
      line.erase(std::remove_if(line.begin(), line.end(), isspace), line.end());
      if (line.empty() || line.length() <= 1)
      {
          continue;
      }
      if (line.front() == '#') continue;
      int len;
      int slength = (int)line.length() + 1;
      len = MultiByteToWideChar(CP_ACP, 0, line.c_str(), slength, 0, 0); 
      wchar_t* buf = new wchar_t[len];
      MultiByteToWideChar(CP_ACP, 0, line.c_str(), slength, buf, len);
      std::wstring wLine(buf);
      delete[] buf;
      
      /// Add *str in the patterns list.
      if (line.front() == '*') 
      {
          g_excludedTypePatterns.insert((std::wstring)(wLine.substr(1, wLine.size()-1)));    
      }
      else
      {
          g_excludedTypes.insert((std::wstring)(wLine));
      }         
  } 
  fclose(file);
  
  IDiaSymbolSet types1;
  IDiaSymbolSet types2;
  if (!EnumTypesInPdb(&types1, g_pDiaSession1, g_pGlobalSymbol1))
  {
      return -1;
  }

  if (!EnumTypesInPdb(&types2, g_pDiaSession2, g_pGlobalSymbol2))
  {
      return -1;
  }

  IDiaSymbolSet commonTypes;

  // Intersect types
  for (IDiaSymbolSet::iterator i = types1.begin(); i != types1.end(); i++)
  {
      std::wstring typeName = *i;

      /// Skip excluded types
      if (g_excludedTypes.find(typeName) != g_excludedTypes.end())
      {
          continue;
      }
      bool skipType = false;
      /// Skip if includes one pattern string.
      for (IDiaSymbolSet::iterator j = g_excludedTypePatterns.begin(); j != g_excludedTypePatterns.end(); j++)
      {
           std::wstring patternStr = *j;
           if (wcsstr(typeName.c_str(), patternStr.c_str()) != NULL)
           {
              skipType = true;
              break;
           }
      }
      
      if (skipType) continue;
      
      if (types2.find(typeName) != types2.end())
      {
          commonTypes.insert(typeName);
      }
  }

  bool matchedSymbols = true;
  ULONG failuresNb = 0;
      
  
  // Compare layout for common types
  for (IDiaSymbolSet::iterator i = commonTypes.begin(); i != commonTypes.end(); i++)
  {
      std::wstring typeName = *i;

      IDiaEnumSymbols *pEnumSymbols1;
      BSTR pwstrTypeName = SysAllocString(typeName.c_str());
      if (FAILED(g_pGlobalSymbol1->findChildren(SymTagUDT, pwstrTypeName, nsNone, &pEnumSymbols1))) {
        SysFreeString(pwstrTypeName);
        return false;
      }

      IDiaEnumSymbols *pEnumSymbols2;
      if (FAILED(g_pGlobalSymbol2->findChildren(SymTagUDT, pwstrTypeName, nsNone, &pEnumSymbols2))) {
        SysFreeString(pwstrTypeName);
        return false;
      }
      
      IDiaSymbol *pSymbol1;
      IDiaSymbol *pSymbol2;
      ULONG celt = 0;      

      
      while (SUCCEEDED(pEnumSymbols1->Next(1, &pSymbol1, &celt)) && (celt == 1)) {
        if (SUCCEEDED(pEnumSymbols2->Next(1, &pSymbol2, &celt)) && (celt == 1))
        {   
           
            BSTR bstrSymbol1Name;
            if (pSymbol1->get_name(&bstrSymbol1Name) != S_OK) 
            {   
                bstrSymbol1Name = NULL; 
                pSymbol2->Release();
                continue;               
            }              
            BSTR bstrSymbol2Name;
            if (pSymbol2->get_name(&bstrSymbol2Name) != S_OK) 
            {
                bstrSymbol2Name = NULL;
                pSymbol2->Release();
                continue;                           
            }            
            if (_wcsicmp(bstrSymbol1Name, bstrSymbol2Name) != 0)
            {
                pSymbol2->Release();
                continue;                
            }
            ULONGLONG sym1Size;
            ULONGLONG sym2Size;
            if (pSymbol1->get_length(&sym1Size) != S_OK)
            {
               wprintf(L"ERROR - can't retrieve the symbol's length\n");           
               pSymbol2->Release();
               continue;
            } 
            //wprintf(L"sym1Size = %x\n", sym1Size);
            if (pSymbol2->get_length(&sym2Size) != S_OK)
            {
               wprintf(L"ERROR - can't retrieve the symbol's length\n");           
               pSymbol2->Release();
               continue;
            }            
            //wprintf(L"sym2Size = %x\n", sym2Size);
            if (sym1Size == 0 || sym2Size == 2)
            {
                pSymbol2->Release();
                continue;
            }
            
            if (!LayoutMatches(pSymbol1, pSymbol2))
            {
                wprintf(L"Type \"%s\" is not matching in %s and %s\n", pwstrTypeName, g_szFilename1, g_szFilename2);                
                pSymbol2->Release();

                matchedSymbols = false;
                failuresNb++;
                // Continue to compare and report all inconsistencies.               
                continue;
            }
            else
            {
#if	DEBUG_VERBOSE
                wprintf(L"Matched type: %s\n", pwstrTypeName);
#endif
            }
            
            pSymbol2->Release();
        }

        pSymbol1->Release();
      }
      
      SysFreeString(pwstrTypeName);
      pEnumSymbols1->Release();
      pEnumSymbols2->Release();
  }
  
  // release COM objects and CoUninitialize()
  Cleanup();
  
  if (matchedSymbols)
  {
      wprintf(L"OK: All %d common types of %s and %s match!\n", commonTypes.size(), g_szFilename1, g_szFilename2);
      return 0;
  }
  else
  {
      wprintf(L"FAIL: Failed to match %d common types of %s and %s!\n", failuresNb, g_szFilename1, g_szFilename2);
      wprintf(L"Matched %d common types!\n", commonTypes.size() - failuresNb);
      return -1;
  }
}

LPSTR UnicodeToAnsi(LPCWSTR s)
{
    if (s==NULL) return NULL;
    int cw=lstrlenW(s);
    if (cw==0) 
    {
        CHAR *psz=new CHAR[1];*psz='\0';return psz;
    }
    int cc=WideCharToMultiByte(CP_ACP,0,s,cw,NULL,0,NULL,NULL);
    if (cc==0) return NULL;
    CHAR *psz=new CHAR[cc+1];
    cc=WideCharToMultiByte(CP_ACP,0,s,cw,psz,cc,NULL,NULL);
    if (cc==0) {delete[] psz;return NULL;}
    psz[cc]='\0';
    return psz;
}


bool InitDiaSource(IDiaDataSource  **ppSource)
{
    HRESULT hr = CoInitialize(NULL);

    // Obtain access to the provider

    hr = CoCreateInstance(__uuidof(DiaSource),
                        NULL,
                        CLSCTX_INPROC_SERVER,
                        __uuidof(IDiaDataSource),
                        (void **) ppSource);

	if (FAILED(hr)) {
		   ACTCTX actCtx;
       memset((void*)&actCtx, 0, sizeof(ACTCTX));
       actCtx.cbSize = sizeof(ACTCTX);
       CHAR   dllPath[MAX_PATH*2];
       GetModuleFileName(NULL, dllPath, _countof(dllPath));
       PathRemoveFileSpec(dllPath);
       strcat(dllPath, "\\msdia100.sxs.manifest");
       actCtx.lpSource = dllPath;
    
       HANDLE hCtx = ::CreateActCtx(&actCtx);
       if (hCtx == INVALID_HANDLE_VALUE)
          wprintf(L"CreateActCtx returned: INVALID_HANDLE_VALUE\n");                 
       else
       {
          ULONG_PTR cookie;
          if (::ActivateActCtx(hCtx, &cookie))
          {
             hr = CoCreateInstance(__uuidof(DiaSource),
                        NULL,
                        CLSCTX_INPROC_SERVER,
                        __uuidof(IDiaDataSource),
                        (void **) ppSource);
             ::DeactivateActCtx(0, cookie);
                 if (FAILED(hr)) {
		          wprintf(L"CoCreateInstance failed - HRESULT = %08X\n", hr);
		          return false;
              }
          }
       }
	  }
    if (FAILED(hr)) {
		wprintf(L"CoCreateInstance failed - HRESULT = %08X\n", hr);

		return false;
    }

    return true;
}

////////////////////////////////////////////////////////////
// Create an IDiaData source and open a PDB file
//
bool LoadDataFromPdb(
    const wchar_t    *szFilename,
    IDiaDataSource  *ppSource,
    IDiaSession     **ppSession,
    IDiaSymbol      **ppGlobal)
{
  wchar_t wszExt[MAX_PATH];
  wchar_t *wszSearchPath = L"SRV**\\\\symbols\\symbols"; // Alternate path to search for debug data
  DWORD dwMachType = 0;
  HRESULT hr;

  _wsplitpath_s(szFilename, NULL, 0, NULL, 0, NULL, 0, wszExt, MAX_PATH);

  if (!_wcsicmp(wszExt, L".pdb")) {
    // Open and prepare a program database (.pdb) file as a debug data source

    hr = (ppSource)->loadDataFromPdb(szFilename);

    if (FAILED(hr)) {
      wprintf(L"loadDataFromPdb failed - HRESULT = %08X\n", hr);

      return false;
    }
  }

  else {
    CCallback callback; // Receives callbacks from the DIA symbol locating procedure,
                        // thus enabling a user interface to report on the progress of
                        // the location attempt. The client application may optionally
                        // provide a reference to its own implementation of this
                        // virtual base class to the IDiaDataSource::loadDataForExe method.
    callback.AddRef();

    // Open and prepare the debug data associated with the executable

    hr = (ppSource)->loadDataForExe(szFilename, wszSearchPath, &callback);

    if (FAILED(hr)) {
      wprintf(L"loadDataForExe failed - HRESULT = %08X\n", hr);

      return false;
    }
  }

  // Open a session for querying symbols

  hr = (ppSource)->openSession(ppSession);

  if (FAILED(hr)) {
    wprintf(L"openSession failed - HRESULT = %08X\n", hr);

    return false;
  }

  // Retrieve a reference to the global scope

  hr = (*ppSession)->get_globalScope(ppGlobal);

  if (hr != S_OK) {
    wprintf(L"get_globalScope failed\n");

    return false;
  }

  // Set Machine type for getting correct register names

  if ((*ppGlobal)->get_machineType(&dwMachType) == S_OK) {
    switch (dwMachType) {
      case IMAGE_FILE_MACHINE_I386 : g_dwMachineType = CV_CFL_80386; break;
      case IMAGE_FILE_MACHINE_IA64 : g_dwMachineType = CV_CFL_IA64; break;
      case IMAGE_FILE_MACHINE_AMD64 : g_dwMachineType = CV_CFL_AMD64; break;
    }
  }

  return true;
}

bool LayoutMatches(IDiaSymbol* pSymbol1, IDiaSymbol* pSymbol2)
{
    DWORD dwTag1, dwTag2;
    DWORD dwLocType1, dwLocType2;
    LONG lOffset1, lOffset2;

    if (pSymbol1->get_symTag(&dwTag1) != S_OK) 
    {
        wprintf(L"ERROR - can't retrieve the symbol's SymTag\n");
        return false;
   }
   if (pSymbol2->get_symTag(&dwTag2) != S_OK) 
   {
        wprintf(L"ERROR - can't retrieve the symbol's SymTag\n");
        return false;
   }

   if (dwTag1 == SymTagUDT)
   {
       if (dwTag2 != SymTagUDT)
       {
           
            wprintf(L"ERROR - symbols don't match\n");
            wprintf(L"Symbol 1:\n");
            PrintTypeInDetail(pSymbol1, 0);
            wprintf(L"Symbol 2:\n");
            PrintTypeInDetail(pSymbol2, 0);
            return false;
       }

       // First check that types size match
       ULONGLONG sym1Size;
       ULONGLONG sym2Size;
       if (pSymbol1->get_length(&sym1Size) != S_OK)
       {
           wprintf(L"ERROR - can't retrieve the symbol's length\n");           
           return false;  
       } 
       if (pSymbol2->get_length(&sym2Size) != S_OK)
       {
           wprintf(L"ERROR - can't retrieve the symbol's length\n");           
           return false;  
       }
       if (sym1Size == 0 || sym2Size == 0)
       {
            return true;
       } 
       if (sym1Size != sym2Size)
       {                
            wprintf(L"Failed to match type size: (sizeof(sym1)=%x) != (sizeof(sym2)=%x)\n", sym1Size, sym2Size);            
            return false;
       }
       IDiaEnumSymbols *pEnumChildren1, *pEnumChildren2;
       IDiaSymbol *pChild1, *pChild2;
       ULONG celt = 0;
       BSTR bstrName1, bstrName2;
       if (SUCCEEDED(pSymbol1->findChildren(SymTagNull, NULL, nsNone, &pEnumChildren1))) 
       {
            while (SUCCEEDED(pEnumChildren1->Next(1, &pChild1, &celt)) && (celt == 1)) 
            {
                if (pChild1->get_symTag(&dwTag1) != S_OK) 
                {
                    wprintf(L"ERROR - can't retrieve the symbol's SymTag\n");
                    pChild1->Release();
                    return false;
                }
                if (dwTag1 != SymTagData) { pChild1->Release(); continue; }
                if (pChild1->get_locationType(&dwLocType1) != S_OK) 
                {
                    wprintf(L"symbol in optmized code");
                    pChild1->Release();
                    return false;
                }
                if (dwLocType1 != LocIsThisRel) { pChild1->Release(); continue; }
                if (pChild1->get_offset(&lOffset1) != S_OK) 
                {
                    wprintf(L"ERROR - geting field offset\n");
                    pChild1->Release();
                    return false;
                }
                if (pChild1->get_name(&bstrName1) != S_OK) 
                {
                    bstrName1 = NULL;
                }
                /// Search in the second symbol the field at lOffset1
                bool fieldMatched = false;
                if  (SUCCEEDED(pSymbol2->findChildren(SymTagNull, NULL, nsNone, &pEnumChildren2)))
                {
                    while (SUCCEEDED(pEnumChildren2->Next(1, &pChild2, &celt)) && (celt == 1)) 
                    {
                        if (pChild2->get_symTag(&dwTag2) != S_OK) 
                        {
                            wprintf(L"ERROR - can't retrieve the symbol's SymTag\n");
                            pChild2->Release();
                            return false;
                        }
                        if (dwTag2 != SymTagData) { pChild2->Release(); continue; }
                        if (pChild2->get_locationType(&dwLocType2) != S_OK) 
                        {
                            wprintf(L"symbol in optmized code");
                            pChild2->Release();
                            return false;
                        }
                        if (dwLocType2 != LocIsThisRel) { pChild2->Release(); continue; }
                        if (pChild2->get_offset(&lOffset2) != S_OK) 
                        {
                            wprintf(L"ERROR - geting field offset\n");
                            pChild2->Release();
                            return false;
                        }
                        if (pChild2->get_name(&bstrName2) != S_OK) 
                        {
                            bstrName2 = NULL;
                        }
                        if (lOffset2 == lOffset1)
                        {
                            if (_wcsicmp(bstrName1, bstrName2) == 0
                            || wcsstr(bstrName1, bstrName2) == bstrName1
                            || wcsstr(bstrName2, bstrName1) == bstrName2)
                            {
                                //wprintf(L"Matched field %s at offset %x\n", bstrName1, lOffset2);
                                fieldMatched = true;
                                pChild2->Release();
                                break;
                            }
                        }
                        pChild2->Release();
                    }
                    pEnumChildren2->Release();
                }
                if (!fieldMatched)
                {
                    BSTR bstrSymbol1Name;
                    if (pSymbol1->get_name(&bstrSymbol1Name) != S_OK) 
                    {
                        bstrSymbol1Name = NULL;
                    }
                    wprintf(L"Failed to match %s field %s at offset %x\n", bstrSymbol1Name, bstrName1, lOffset1);
                    pChild1->Release();
                    return false;
                }
                pChild1->Release();
            }

            pEnumChildren1->Release();
       }
   }

   return true;
}

////////////////////////////////////////////////////////////
// Release DIA objects and CoUninitialize
//
void Cleanup()
{
  if (g_pGlobalSymbol1) {
    g_pGlobalSymbol1->Release();
    g_pGlobalSymbol1 = NULL;
  }

  if (g_pGlobalSymbol2) {
    g_pGlobalSymbol2->Release();
    g_pGlobalSymbol2 = NULL;
  }

  if (g_pDiaSession1) {
    g_pDiaSession1->Release();
    g_pDiaSession1 = NULL;
  }

  if (g_pDiaSession2) {
    g_pDiaSession2->Release();
    g_pDiaSession2 = NULL;
  }

  CoUninitialize();
}


////////////////////////////////////////////////////////////
// Display the usage
//
void PrintHelpOptions()
{
  static const wchar_t * const helpString = L"usage: PdbTypeMatch.exe <pdb_filename_1> <pdb_filename_2> <type_exclusion_list_file> : compare all common types by size and fields\n"
                                            L"       PdbTypeMatch.exe -type <symbolname>  <pdb_filename_1>: dump this type in detail\n";
                                            
  wprintf(helpString);
}

bool EnumTypesInPdb(IDiaSymbolSet* types, IDiaSession *pSession, IDiaSymbol *pGlobal)
{
    IDiaEnumSymbols *pEnumSymbols;

    if (FAILED(pGlobal->findChildren(SymTagUDT, NULL, nsNone, &pEnumSymbols))) 
    {
        wprintf(L"ERROR - EnumTypesInPdb() returned no symbols\n");

        return false;
    }

    IDiaSymbol *pSymbol;
    ULONG celt = 0;

    while (SUCCEEDED(pEnumSymbols->Next(1, &pSymbol, &celt)) && (celt == 1)) 
    {
        std::wstring typeName;
        GetSymbolName(typeName, pSymbol);
        types->insert(std::wstring(typeName));
        pSymbol->Release();
    }

    pEnumSymbols->Release();

    return true;
}

////////////////////////////////////////////////////////////
// Dump all the data stored in a PDB
//
void DumpAllPdbInfo(IDiaSession *pSession, IDiaSymbol *pGlobal)
{
  DumpAllMods(pGlobal);
  DumpAllPublics(pGlobal);
  DumpAllSymbols(pGlobal);
  DumpAllGlobals(pGlobal);
  DumpAllTypes(pGlobal);
  DumpAllFiles(pSession, pGlobal);
  DumpAllLines(pSession, pGlobal);
  DumpAllSecContribs(pSession);
  DumpAllDebugStreams(pSession);
  DumpAllInjectedSources(pSession);
  DumpAllFPO(pSession);
  DumpAllOEMs(pGlobal);
}

////////////////////////////////////////////////////////////
// Dump all the modules information
//
bool DumpAllMods(IDiaSymbol *pGlobal)
{
  wprintf(L"\n\n*** MODULES\n\n");

  // Retrieve all the compiland symbols

  IDiaEnumSymbols *pEnumSymbols;

  if (FAILED(pGlobal->findChildren(SymTagCompiland, NULL, nsNone, &pEnumSymbols))) {
    return false;
  }

  IDiaSymbol *pCompiland;
  ULONG celt = 0;
  ULONG iMod = 1;

  while (SUCCEEDED(pEnumSymbols->Next(1, &pCompiland, &celt)) && (celt == 1)) {
    BSTR bstrName;

    if (pCompiland->get_name(&bstrName) != S_OK) {
      wprintf(L"ERROR - Failed to get the compiland's name\n");

      pCompiland->Release();
      pEnumSymbols->Release();

      return false;
    }

    wprintf(L"%04X %s\n", iMod++, bstrName);

    // Deallocate the string allocated previously by the call to get_name

    SysFreeString(bstrName);

    pCompiland->Release();
  }

  pEnumSymbols->Release();

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// Dump all the public symbols - SymTagPublicSymbol
//
bool DumpAllPublics(IDiaSymbol *pGlobal)
{
  wprintf(L"\n\n*** PUBLICS\n\n");

  // Retrieve all the public symbols

  IDiaEnumSymbols *pEnumSymbols;

  if (FAILED(pGlobal->findChildren(SymTagPublicSymbol, NULL, nsNone, &pEnumSymbols))) {
    return false;
  }

  IDiaSymbol *pSymbol;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumSymbols->Next(1, &pSymbol, &celt)) && (celt == 1)) {
    PrintPublicSymbol(pSymbol);

    pSymbol->Release();
  }

  pEnumSymbols->Release();

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// Dump all the symbol information stored in the compilands
//
bool DumpAllSymbols(IDiaSymbol *pGlobal)
{
  wprintf(L"\n\n*** SYMBOLS\n\n\n");

  // Retrieve the compilands first

  IDiaEnumSymbols *pEnumSymbols;

  if (FAILED(pGlobal->findChildren(SymTagCompiland, NULL, nsNone, &pEnumSymbols))) {
    return false;
  }

  IDiaSymbol *pCompiland;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumSymbols->Next(1, &pCompiland, &celt)) && (celt == 1)) {
    wprintf(L"\n** Module: ");

    // Retrieve the name of the module

    BSTR bstrName;

    if (pCompiland->get_name(&bstrName) != S_OK) {
      wprintf(L"(???)\n\n");
    }

    else {
      wprintf(L"%s\n\n", bstrName);

      SysFreeString(bstrName);
    }

    // Find all the symbols defined in this compiland and print their info

    IDiaEnumSymbols *pEnumChildren;

    if (SUCCEEDED(pCompiland->findChildren(SymTagNull, NULL, nsNone, &pEnumChildren))) {
      IDiaSymbol *pSymbol;
      ULONG celtChildren = 0;

      while (SUCCEEDED(pEnumChildren->Next(1, &pSymbol, &celtChildren)) && (celtChildren == 1)) {
        PrintSymbol(pSymbol, 0);
        pSymbol->Release();
      }

      pEnumChildren->Release();
    }

    pCompiland->Release();
  }

  pEnumSymbols->Release();

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// Dump all the global symbols - SymTagFunction,
//  SymTagThunk and SymTagData
//
bool DumpAllGlobals(IDiaSymbol *pGlobal)
{
  IDiaEnumSymbols *pEnumSymbols;
  IDiaSymbol *pSymbol;
  enum SymTagEnum dwSymTags[] = { SymTagFunction, SymTagThunk, SymTagData };
  ULONG celt = 0;

  wprintf(L"\n\n*** GLOBALS\n\n");

  for (size_t i = 0; i < _countof(dwSymTags); i++, pEnumSymbols = NULL) {
    if (SUCCEEDED(pGlobal->findChildren(dwSymTags[i], NULL, nsNone, &pEnumSymbols))) {
      while (SUCCEEDED(pEnumSymbols->Next(1, &pSymbol, &celt)) && (celt == 1)) {
        PrintGlobalSymbol(pSymbol);

        pSymbol->Release();
      }

      pEnumSymbols->Release();
    }

    else {
      wprintf(L"ERROR - DumpAllGlobals() returned no symbols\n");

      return false;
    }
  }

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// Dump all the types information
//  (type symbols can be UDTs, enums or typedefs)
//
bool DumpAllTypes(IDiaSymbol *pGlobal)
{
  wprintf(L"\n\n*** TYPES\n");

  return DumpAllUDTs(pGlobal) || DumpAllEnums(pGlobal) || DumpAllTypedefs(pGlobal);
}

////////////////////////////////////////////////////////////
// Dump all the user defined types (UDT)
//
bool DumpAllUDTs(IDiaSymbol *pGlobal)
{
  wprintf(L"\n\n** User Defined Types\n\n");

  IDiaEnumSymbols *pEnumSymbols;

  if (FAILED(pGlobal->findChildren(SymTagUDT, NULL, nsNone, &pEnumSymbols))) {
    wprintf(L"ERROR - DumpAllUDTs() returned no symbols\n");

    return false;
  }

  IDiaSymbol *pSymbol;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumSymbols->Next(1, &pSymbol, &celt)) && (celt == 1)) {
    PrintTypeInDetail(pSymbol, 0);

    pSymbol->Release();
  }

  pEnumSymbols->Release();

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// Dump all the enum types from the pdb
//
bool DumpAllEnums(IDiaSymbol *pGlobal)
{
  wprintf(L"\n\n** ENUMS\n\n");

  IDiaEnumSymbols *pEnumSymbols;

  if (FAILED(pGlobal->findChildren(SymTagEnum, NULL, nsNone, &pEnumSymbols))) {
    wprintf(L"ERROR - DumpAllEnums() returned no symbols\n");

    return false;
  }

  IDiaSymbol *pSymbol;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumSymbols->Next(1, &pSymbol, &celt)) && (celt == 1)) {
    PrintTypeInDetail(pSymbol, 0);

    pSymbol->Release();
  }

  pEnumSymbols->Release();

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// Dump all the typedef types from the pdb
//
bool DumpAllTypedefs(IDiaSymbol *pGlobal)
{
  wprintf(L"\n\n** TYPEDEFS\n\n");

  IDiaEnumSymbols *pEnumSymbols;

  if (FAILED(pGlobal->findChildren(SymTagTypedef, NULL, nsNone, &pEnumSymbols))) {
    wprintf(L"ERROR - DumpAllTypedefs() returned no symbols\n");

    return false;
  }

  IDiaSymbol *pSymbol;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumSymbols->Next(1, &pSymbol, &celt)) && (celt == 1)) {
    PrintTypeInDetail(pSymbol, 0);

    pSymbol->Release();
  }

  pEnumSymbols->Release();

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// Dump OEM specific types
//
bool DumpAllOEMs(IDiaSymbol *pGlobal)
{
  wprintf(L"\n\n*** OEM Specific types\n\n");

  IDiaEnumSymbols *pEnumSymbols;

  if (FAILED(pGlobal->findChildren(SymTagCustomType, NULL, nsNone, &pEnumSymbols))) {
    wprintf(L"ERROR - DumpAllOEMs() returned no symbols\n");

    return false;
  }

  IDiaSymbol *pSymbol;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumSymbols->Next(1, &pSymbol, &celt)) && (celt == 1)) {
    PrintTypeInDetail(pSymbol, 0);

    pSymbol->Release();
  }

  pEnumSymbols->Release();

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// For each compiland in the PDB dump all the source files
//
bool DumpAllFiles(IDiaSession *pSession, IDiaSymbol *pGlobal)
{
  wprintf(L"\n\n*** FILES\n\n");

  // In order to find the source files, we have to look at the image's compilands/modules

  IDiaEnumSymbols *pEnumSymbols;

  if (FAILED(pGlobal->findChildren(SymTagCompiland, NULL, nsNone, &pEnumSymbols))) {
    return false;
  }

  IDiaSymbol *pCompiland;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumSymbols->Next(1, &pCompiland, &celt)) && (celt == 1)) {
    BSTR bstrName;

    if (pCompiland->get_name(&bstrName) == S_OK) {
      wprintf(L"\nCompiland = %s\n\n", bstrName);

      SysFreeString(bstrName);
    }

    // Every compiland could contain multiple references to the source files which were used to build it
    // Retrieve all source files by compiland by passing NULL for the name of the source file

    IDiaEnumSourceFiles *pEnumSourceFiles;

    if (SUCCEEDED(pSession->findFile(pCompiland, NULL, nsNone, &pEnumSourceFiles))) {
      IDiaSourceFile *pSourceFile;

      while (SUCCEEDED(pEnumSourceFiles->Next(1, &pSourceFile, &celt)) && (celt == 1)) {
        PrintSourceFile(pSourceFile);
        putwchar(L'\n');

        pSourceFile->Release();
      }

      pEnumSourceFiles->Release();
    }

    putwchar(L'\n');

    pCompiland->Release();
  }

  pEnumSymbols->Release();

  return true;
}

////////////////////////////////////////////////////////////
// Dump all the line numbering information contained in the PDB
//  Only function symbols have corresponding line numbering information
bool DumpAllLines(IDiaSession *pSession, IDiaSymbol *pGlobal)
{
  wprintf(L"\n\n*** LINES\n\n");

  // First retrieve the compilands/modules

  IDiaEnumSymbols *pEnumSymbols;

  if (FAILED(pGlobal->findChildren(SymTagCompiland, NULL, nsNone, &pEnumSymbols))) {
    return false;
  }

  IDiaSymbol *pCompiland;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumSymbols->Next(1, &pCompiland, &celt)) && (celt == 1)) {
    IDiaEnumSymbols *pEnumFunction;

    // For every function symbol defined in the compiland, retrieve and print the line numbering info

    if (SUCCEEDED(pCompiland->findChildren(SymTagFunction, NULL, nsNone, &pEnumFunction))) {
      IDiaSymbol *pFunction;
      ULONG celt = 0;

      while (SUCCEEDED(pEnumFunction->Next(1, &pFunction, &celt)) && (celt == 1)) {
        PrintLines(pSession, pFunction);

        pFunction->Release();
      }

      pEnumFunction->Release();
    }

    pCompiland->Release();
  }

  pEnumSymbols->Release();

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// Dump all the line numbering information for a given RVA
// and a given range
//
bool DumpAllLines(IDiaSession *pSession, DWORD dwRVA, DWORD dwRange)
{
  // Retrieve and print the lines that corresponds to a specified RVA

  IDiaEnumLineNumbers *pLines;

  if (FAILED(pSession->findLinesByRVA(dwRVA, dwRange, &pLines))) {
    return false;
  }

  PrintLines(pLines);

  pLines->Release();

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// Dump all the section contributions from the PDB
//
//  Section contributions are stored in a table which will
//  be retrieved via IDiaSession->getEnumTables through
//  QueryInterface()using the REFIID of the IDiaEnumSectionContribs
//
bool DumpAllSecContribs(IDiaSession *pSession)
{
  wprintf(L"\n\n*** SECTION CONTRIBUTION\n\n");

  IDiaEnumSectionContribs *pEnumSecContribs;

  if (FAILED(GetTable(pSession, __uuidof(IDiaEnumSectionContribs), (void **) &pEnumSecContribs))) {
    return false;
  }

  wprintf(L"    RVA        Address       Size    Module\n");

  IDiaSectionContrib *pSecContrib;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumSecContribs->Next(1, &pSecContrib, &celt)) && (celt == 1)) {
    PrintSecContribs(pSecContrib);

    pSecContrib->Release();
  }

  pEnumSecContribs->Release();

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// Dump all debug data streams contained in the PDB
//
bool DumpAllDebugStreams(IDiaSession *pSession)
{
  IDiaEnumDebugStreams *pEnumStreams;

  wprintf(L"\n\n*** DEBUG STREAMS\n\n");

  // Retrieve an enumerated sequence of debug data streams

  if (FAILED(pSession->getEnumDebugStreams(&pEnumStreams))) {
    return false;
  }

  IDiaEnumDebugStreamData *pStream;
  ULONG celt = 0;

  for (; SUCCEEDED(pEnumStreams->Next(1, &pStream, &celt)) && (celt == 1); pStream = NULL) {
    PrintStreamData(pStream);

    pStream->Release();
  }

  pEnumStreams->Release();

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// Dump all the injected source from the PDB
//
//  Injected sources data is stored in a table which will
//  be retrieved via IDiaSession->getEnumTables through
//  QueryInterface()using the REFIID of the IDiaEnumSectionContribs
//
bool DumpAllInjectedSources(IDiaSession *pSession)
{
  wprintf(L"\n\n*** INJECTED SOURCES TABLE\n\n");

  IDiaEnumInjectedSources *pEnumInjSources;

  if (SUCCEEDED(GetTable(pSession, __uuidof(IDiaEnumInjectedSources), (void **) &pEnumInjSources))) {
    return false;
  }

  IDiaInjectedSource *pInjSource;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumInjSources->Next(1, &pInjSource, &celt)) && (celt == 1)) {
    PrintGeneric(pInjSource);

    pInjSource->Release();
  }

  pEnumInjSources->Release();

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// Dump info corresponing to a given injected source filename
//
bool DumpInjectedSource(IDiaSession *pSession, const wchar_t *szName)
{
  // Retrieve a source that has been placed into the symbol store by attribute providers or
  //  other components of the compilation process

  IDiaEnumInjectedSources *pEnumInjSources;

  if (FAILED(pSession->findInjectedSource(szName, &pEnumInjSources))) {
    wprintf(L"ERROR - DumpInjectedSources() could not find %s\n", szName);

    return false;
  }

  IDiaInjectedSource *pInjSource;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumInjSources->Next(1, &pInjSource, &celt)) && (celt == 1)) {
    PrintGeneric(pInjSource);

    pInjSource->Release();
  }

  pEnumInjSources->Release();

  return true;
}

////////////////////////////////////////////////////////////
// Dump all the source file information stored in the PDB
// We have to go through every compiland in order to retrieve
//   all the information otherwise checksums for instance
//   will not be available
// Compilands can have multiple source files with the same
//   name but different content which produces diffrent
//   checksums
//
bool DumpAllSourceFiles(IDiaSession *pSession, IDiaSymbol *pGlobal)
{
  wprintf(L"\n\n*** SOURCE FILES\n\n");

  // To get the complete source file info we must go through the compiland first
  // by passing NULL instead all the source file names only will be retrieved

  IDiaEnumSymbols *pEnumSymbols;

  if (FAILED(pGlobal->findChildren(SymTagCompiland, NULL, nsNone, &pEnumSymbols))) {
    return false;
  }

  IDiaSymbol *pCompiland;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumSymbols->Next(1, &pCompiland, &celt)) && (celt == 1)) {
    BSTR bstrName;

    if (pCompiland->get_name(&bstrName) == S_OK) {
      wprintf(L"\nCompiland = %s\n\n", bstrName);

      SysFreeString(bstrName);
    }

    // Every compiland could contain multiple references to the source files which were used to build it
    // Retrieve all source files by compiland by passing NULL for the name of the source file

    IDiaEnumSourceFiles *pEnumSourceFiles;

    if (SUCCEEDED(pSession->findFile(pCompiland, NULL, nsNone, &pEnumSourceFiles))) {
      IDiaSourceFile *pSourceFile;

      while (SUCCEEDED(pEnumSourceFiles->Next(1, &pSourceFile, &celt)) && (celt == 1)) {
        PrintSourceFile(pSourceFile);
        putwchar(L'\n');

        pSourceFile->Release();
      }

      pEnumSourceFiles->Release();
    }

    putwchar(L'\n');

    pCompiland->Release();
  }

  pEnumSymbols->Release();

  return true;
}

////////////////////////////////////////////////////////////
// Dump all the FPO info
//
//  FPO data stored in a table which will be retrieved via
//    IDiaSession->getEnumTables through QueryInterface()
//    using the REFIID of the IDiaEnumFrameData
//
bool DumpAllFPO(IDiaSession *pSession)
{
  IDiaEnumFrameData *pEnumFrameData;

  wprintf(L"\n\n*** FPO\n\n");

  if (FAILED(GetTable(pSession, __uuidof(IDiaEnumFrameData), (void **) &pEnumFrameData))) {
    return false;
  }

  IDiaFrameData *pFrameData;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumFrameData->Next(1, &pFrameData, &celt)) && (celt == 1)) {
    PrintFrameData(pFrameData);

    pFrameData->Release();
  }

  pEnumFrameData->Release();

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// Dump FPO info for a function at the specified RVA
//
bool DumpFPO(IDiaSession *pSession, DWORD dwRVA)
{
  IDiaEnumFrameData *pEnumFrameData;

  // Retrieve first the table holding all the FPO info

  if ((dwRVA != 0) && SUCCEEDED(GetTable(pSession, __uuidof(IDiaEnumFrameData), (void **) &pEnumFrameData))) {
    IDiaFrameData *pFrameData;

    // Retrieve the frame data corresponding to the given RVA

    if (SUCCEEDED(pEnumFrameData->frameByRVA(dwRVA, &pFrameData))) {
      PrintGeneric(pFrameData);

      pFrameData->Release();
    }

    else {
      // Some function might not have FPO data available (see ASM funcs like strcpy)

      wprintf(L"ERROR - DumpFPO() frameByRVA invalid RVA: 0x%08X\n", dwRVA);

      pEnumFrameData->Release();

      return false;
    }

    pEnumFrameData->Release();
  }

  else {
    wprintf(L"ERROR - DumpFPO() GetTable\n");

    return false;
  }

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// Dump FPO info for a specified function symbol using its
//  name (a regular expression string is used for the search)
//
bool DumpFPO(IDiaSession *pSession, IDiaSymbol *pGlobal, const wchar_t *szSymbolName)
{
  IDiaEnumSymbols *pEnumSymbols;
  IDiaSymbol *pSymbol;
  ULONG celt = 0;
  DWORD dwRVA;

  // Find first all the function symbols that their names matches the search criteria

  if (FAILED(pGlobal->findChildren(SymTagFunction, szSymbolName, nsRegularExpression, &pEnumSymbols))) {
    wprintf(L"ERROR - DumpFPO() findChildren could not find symol %s\n", szSymbolName);

    return false;
  }

  while (SUCCEEDED(pEnumSymbols->Next(1, &pSymbol, &celt)) && (celt == 1)) {
    if (pSymbol->get_relativeVirtualAddress(&dwRVA) == S_OK) {
      PrintPublicSymbol(pSymbol);

      DumpFPO(pSession, dwRVA);
    }

    pSymbol->Release();
  }

  pEnumSymbols->Release();

  putwchar(L'\n');

  return true;
}

////////////////////////////////////////////////////////////
// Dump a specified compiland and all the symbols defined in it
//
bool DumpCompiland(IDiaSymbol *pGlobal, const wchar_t *szCompName)
{
  IDiaEnumSymbols *pEnumSymbols;

  if (FAILED(pGlobal->findChildren(SymTagCompiland, szCompName, nsCaseInsensitive, &pEnumSymbols))) {
    return false;
  }

  IDiaSymbol *pCompiland;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumSymbols->Next(1, &pCompiland, &celt)) && (celt == 1)) {
    wprintf(L"\n** Module: ");

    // Retrieve the name of the module

    BSTR bstrName;

    if (pCompiland->get_name(&bstrName) != S_OK) {
      wprintf(L"(???)\n\n");
    }

    else {
      wprintf(L"%s\n\n", bstrName);

      SysFreeString(bstrName);
    }

    IDiaEnumSymbols *pEnumChildren;

    if (SUCCEEDED(pCompiland->findChildren(SymTagNull, NULL, nsNone, &pEnumChildren))) {
      IDiaSymbol *pSymbol;
      ULONG celt_ = 0;

      while (SUCCEEDED(pEnumChildren->Next(1, &pSymbol, &celt_)) && (celt_ == 1)) {
        PrintSymbol(pSymbol, 0);

        pSymbol->Release();
      }

      pEnumChildren->Release();
    }

    pCompiland->Release();
  }

  pEnumSymbols->Release();

  return true;
}

////////////////////////////////////////////////////////////
// Dump the line numbering information for a specified RVA
//
bool DumpLines(IDiaSession *pSession, DWORD dwRVA)
{
  IDiaEnumLineNumbers *pLines;

  if (FAILED(pSession->findLinesByRVA(dwRVA, MAX_RVA_LINES_BYTES_RANGE, &pLines))) {
    return false;
  }

  PrintLines(pLines);

  pLines->Release();

  return true;
}

////////////////////////////////////////////////////////////
// Dump the all line numbering information for a specified
//  function symbol name (as a regular expression string)
//
bool DumpLines(IDiaSession *pSession, IDiaSymbol *pGlobal, const wchar_t *szFuncName)
{
  IDiaEnumSymbols *pEnumSymbols;

  if (FAILED(pGlobal->findChildren(SymTagFunction, szFuncName, nsRegularExpression, &pEnumSymbols))) {
    return false;
  }

  IDiaSymbol *pFunction;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumSymbols->Next(1, &pFunction, &celt)) && (celt == 1)) {
    PrintLines(pSession, pFunction);

    pFunction->Release();
  }

  pEnumSymbols->Release();

  return true;
}

////////////////////////////////////////////////////////////
// Dump the symbol information corresponding to a specified RVA
//
bool DumpSymbolWithRVA(IDiaSession *pSession, DWORD dwRVA, const wchar_t *szChildname)
{
  IDiaSymbol *pSymbol;
  LONG lDisplacement;

  if (FAILED(pSession->findSymbolByRVAEx(dwRVA, SymTagNull, &pSymbol, &lDisplacement))) {
    return false;
  }

  wprintf(L"Displacement = 0x%X\n", lDisplacement);

  PrintGeneric(pSymbol);

  bool bReturn = DumpSymbolWithChildren(pSymbol, szChildname);

  while (pSymbol != NULL) {
    IDiaSymbol *pParent;

    if ((pSymbol->get_lexicalParent(&pParent) == S_OK) && pParent) {
      wprintf(L"\nParent\n");

      PrintSymbol(pParent, 0);

      pSymbol->Release();

      pSymbol = pParent;
    }

    else {
      pSymbol->Release();
      break;
    }
  }

  return true;
}

////////////////////////////////////////////////////////////
// Dump the symbols information where their names matches a
//  specified regular expression string
//
bool DumpSymbolsWithRegEx(IDiaSymbol *pGlobal, const wchar_t *szRegEx, const wchar_t *szChildname)
{
  IDiaEnumSymbols *pEnumSymbols;

  if (FAILED(pGlobal->findChildren(SymTagNull, szRegEx, nsRegularExpression, &pEnumSymbols))) {
    return false;
  }

  bool bReturn = true;

  IDiaSymbol *pSymbol;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumSymbols->Next(1, &pSymbol, &celt)) && (celt == 1)) {
    PrintGeneric(pSymbol);

    bReturn = DumpSymbolWithChildren(pSymbol, szChildname);

    pSymbol->Release();
  }

  pEnumSymbols->Release();

  return bReturn;
}

////////////////////////////////////////////////////////////
// Dump the information corresponding to a symbol name which
//  is a children of the specified parent symbol
//
bool DumpSymbolWithChildren(IDiaSymbol *pSymbol, const wchar_t *szChildname)
{
  if (szChildname != NULL) {
    IDiaEnumSymbols *pEnumSyms;

    if (FAILED(pSymbol->findChildren(SymTagNull, szChildname, nsRegularExpression, &pEnumSyms))) {
      return false;
    }

    IDiaSymbol *pChild;
    DWORD celt = 1;

    while (SUCCEEDED(pEnumSyms->Next(celt, &pChild, &celt)) && (celt == 1)) {
      PrintGeneric(pChild);
      PrintSymbol(pChild, 0);

      pChild->Release();
    }

    pEnumSyms->Release();
  }

  else {
    // If the specified name is NULL then only the parent symbol data is displayed

    DWORD dwSymTag;

    if ((pSymbol->get_symTag(&dwSymTag) == S_OK) && (dwSymTag == SymTagPublicSymbol)) {
      PrintPublicSymbol(pSymbol);
    }

    else {
      PrintSymbol(pSymbol, 0);
    }
  }

  return true;
}

////////////////////////////////////////////////////////////
// Dump all the type symbols information that matches their
//  names to a specified regular expression string
//
bool DumpType(IDiaSymbol *pGlobal, const wchar_t *szRegEx)
{
  IDiaEnumSymbols *pEnumSymbols;

  if (FAILED(pGlobal->findChildren(SymTagUDT, szRegEx, nsRegularExpression, &pEnumSymbols))) {
    return false;
  }

  IDiaSymbol *pSymbol;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumSymbols->Next(1, &pSymbol, &celt)) && (celt == 1)) {
    PrintTypeInDetail(pSymbol, 0);

    pSymbol->Release();
  }

  pEnumSymbols->Release();

  return true;
}

////////////////////////////////////////////////////////////
// Dump line numbering information for a given file name and
//  an optional line number
//
bool DumpLinesForSourceFile(IDiaSession *pSession, const wchar_t *szFileName, DWORD dwLine)
{
  IDiaEnumSourceFiles *pEnumSrcFiles;

  if (FAILED(pSession->findFile(NULL, szFileName, nsFNameExt, &pEnumSrcFiles))) {
    return false;
  }

  IDiaSourceFile *pSrcFile;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumSrcFiles->Next(1, &pSrcFile, &celt)) && (celt == 1)) {
    IDiaEnumSymbols *pEnumCompilands;

    if (pSrcFile->get_compilands(&pEnumCompilands) == S_OK) {
      IDiaSymbol *pCompiland;

      celt = 0;
      while (SUCCEEDED(pEnumCompilands->Next(1, &pCompiland, &celt)) && (celt == 1)) {
        BSTR bstrName;

        if (pCompiland->get_name(&bstrName) == S_OK) {
          wprintf(L"Compiland = %s\n", bstrName);

          SysFreeString(bstrName);
        }

        else {
          wprintf(L"Compiland = (???)\n");
        }

        IDiaEnumLineNumbers *pLines;

        if (dwLine != 0) {
          if (SUCCEEDED(pSession->findLinesByLinenum(pCompiland, pSrcFile, dwLine, 0, &pLines))) {
            PrintLines(pLines);

            pLines->Release();
          }
        }

        else {
          if (SUCCEEDED(pSession->findLines(pCompiland, pSrcFile, &pLines))) {
            PrintLines(pLines);

            pLines->Release();
          }
        }

        pCompiland->Release();
      }

      pEnumCompilands->Release();
    }

    pSrcFile->Release();
  }

  pEnumSrcFiles->Release();

  return true;
}

////////////////////////////////////////////////////////////
// Dump public symbol information for a given number of
//  symbols around a given RVA address
//
bool DumpPublicSymbolsSorted(IDiaSession *pSession, DWORD dwRVA, DWORD dwRange, bool bReverse)
{
  IDiaEnumSymbolsByAddr *pEnumSymsByAddr;

  if (FAILED(pSession->getSymbolsByAddr(&pEnumSymsByAddr))) {
    return false;
  }

  IDiaSymbol *pSymbol;

  if (SUCCEEDED(pEnumSymsByAddr->symbolByRVA(dwRVA, &pSymbol))) {
    if (dwRange == 0) {
      PrintPublicSymbol(pSymbol);
    }

    ULONG celt;
    ULONG i;

    if (bReverse) {
      pSymbol->Release();

      i = 0;

      for (pSymbol = NULL; (i < dwRange) && SUCCEEDED(pEnumSymsByAddr->Next(1, &pSymbol, &celt)) && (celt == 1); i++) {
        PrintPublicSymbol(pSymbol);

        pSymbol->Release();
      }
    }

    else {
      PrintPublicSymbol(pSymbol);

      pSymbol->Release();

      i = 1;

      for (pSymbol = NULL; (i < dwRange) && SUCCEEDED(pEnumSymsByAddr->Prev(1, &pSymbol, &celt)) && (celt == 1); i++) {
        PrintPublicSymbol(pSymbol);
      }
    }
  }

  pEnumSymsByAddr->Release();

  return true;
}

////////////////////////////////////////////////////////////
// Dump label symbol information at a given RVA
//
bool DumpLabel(IDiaSession *pSession, DWORD dwRVA)
{
  IDiaSymbol *pSymbol;
  LONG lDisplacement;

  if (FAILED(pSession->findSymbolByRVAEx(dwRVA, SymTagLabel, &pSymbol, &lDisplacement)) || (pSymbol == NULL)) {
    return false;
  }

  wprintf(L"Displacement = 0x%X\n", lDisplacement);

  PrintGeneric(pSymbol);

  pSymbol->Release();

  return true;
}

////////////////////////////////////////////////////////////
// Dump annotation symbol information at a given RVA
//
bool DumpAnnotations(IDiaSession *pSession, DWORD dwRVA)
{
  IDiaSymbol *pSymbol;
  LONG lDisplacement;

  if (FAILED(pSession->findSymbolByRVAEx(dwRVA, SymTagAnnotation, &pSymbol, &lDisplacement)) || (pSymbol == NULL)) {
    return false;
  }

  wprintf(L"Displacement = 0x%X\n", lDisplacement);

  PrintGeneric(pSymbol);

  pSymbol->Release();

  return true;
}

struct OMAP_DATA
{
  DWORD dwRVA;
  DWORD dwRVATo;
};

////////////////////////////////////////////////////////////
//
bool DumpMapToSrc(IDiaSession *pSession, DWORD dwRVA)
{
  IDiaEnumDebugStreams *pEnumStreams;
  IDiaEnumDebugStreamData *pStream;
  ULONG celt;

  if (FAILED(pSession->getEnumDebugStreams(&pEnumStreams))) {
    return false;
  }

  celt = 0;

  for (; SUCCEEDED(pEnumStreams->Next(1, &pStream, &celt)) && (celt == 1); pStream = NULL) {
    BSTR bstrName;

    if (pStream->get_name(&bstrName) != S_OK) {
      bstrName = NULL;
    }

    if (bstrName && wcscmp(bstrName, L"OMAPTO") == 0) {
      OMAP_DATA data, datasav;
      DWORD cbData, celt;
      DWORD dwRVATo = 0;
      unsigned int i;

      datasav.dwRVATo = 0;
      datasav.dwRVA = 0;

      while (SUCCEEDED(pStream->Next(1, sizeof(data), &cbData, (BYTE*) &data, &celt)) && (celt == 1)) {
        if (dwRVA > data.dwRVA) {
          datasav = data;
          continue;
        }

         else if (dwRVA == data.dwRVA) {
          dwRVATo = data.dwRVATo;
        }

        else if (datasav.dwRVATo) {
          dwRVATo = datasav.dwRVATo + (dwRVA - datasav.dwRVA);
        }
        break;
      }

      wprintf(L"image rva = %08X ==> source rva = %08X\n\nRelated OMAP entries:\n", dwRVA, dwRVATo);
      wprintf(L"image rva ==> source rva\n");
      wprintf(L"%08X  ==> %08X\n", datasav.dwRVA, datasav.dwRVATo);

      i = 0;

      do {
        wprintf(L"%08X  ==> %08X\n", data.dwRVA, data.dwRVATo);
      }
      while ((++i) < 5 && SUCCEEDED(pStream->Next(1, sizeof(data), &cbData, (BYTE*) &data, &celt)) && (celt == 1));
    }

    if (bstrName != NULL) {
      SysFreeString(bstrName);
    }

    pStream->Release();
  }

  pEnumStreams->Release();

  return true;
}

////////////////////////////////////////////////////////////
//
bool DumpMapFromSrc(IDiaSession *pSession, DWORD dwRVA)
{
  IDiaEnumDebugStreams *pEnumStreams;

  if (FAILED(pSession->getEnumDebugStreams(&pEnumStreams))) {
    return false;
  }

  IDiaEnumDebugStreamData *pStream;
  ULONG celt = 0;

  for (; SUCCEEDED(pEnumStreams->Next(1, &pStream, &celt)) && (celt == 1); pStream = NULL) {
    BSTR bstrName;

    if (pStream->get_name(&bstrName) != S_OK) {
      bstrName = NULL;
    }

    if (bstrName && wcscmp(bstrName, L"OMAPFROM") == 0) {
      OMAP_DATA data;
      OMAP_DATA datasav;
      DWORD cbData;
      DWORD celt;
      DWORD dwRVATo = 0;
      unsigned int i;

      datasav.dwRVATo = 0;
      datasav.dwRVA = 0;

      while (SUCCEEDED(pStream->Next(1, sizeof(data), &cbData, (BYTE*) &data, &celt)) && (celt == 1)) {
        if (dwRVA > data.dwRVA) {
          datasav = data;
          continue;
        }

        else if (dwRVA == data.dwRVA) {
          dwRVATo = data.dwRVATo;
        }

        else if (datasav.dwRVATo) {
          dwRVATo = datasav.dwRVATo + (dwRVA - datasav.dwRVA);
        }
        break;
      }

      wprintf(L"source rva = %08X ==> image rva = %08X\n\nRelated OMAP entries:\n", dwRVA, dwRVATo);
      wprintf(L"source rva ==> image rva\n");
      wprintf(L"%08X  ==> %08X\n", datasav.dwRVA, datasav.dwRVATo);

      i = 0;

      do {
        wprintf(L"%08X  ==> %08X\n", data.dwRVA, data.dwRVATo);
      }
      while ((++i) < 5 && SUCCEEDED(pStream->Next(1, sizeof(data), &cbData, (BYTE*) &data, &celt)) && (celt == 1));
    }

    if (bstrName != NULL) {
      SysFreeString(bstrName);
    }

    pStream->Release();
  }

  pEnumStreams->Release();

  return true;
}

////////////////////////////////////////////////////////////
// Retreive the table that matches the given iid
//
//  A PDB table could store the section contributions, the frame data,
//  the injected sources
//
HRESULT GetTable(IDiaSession *pSession, REFIID iid, void **ppUnk)
{
  IDiaEnumTables *pEnumTables;

  if (FAILED(pSession->getEnumTables(&pEnumTables))) {
    wprintf(L"ERROR - GetTable() getEnumTables\n");

    return E_FAIL;
  }

  IDiaTable *pTable;
  ULONG celt = 0;

  while (SUCCEEDED(pEnumTables->Next(1, &pTable, &celt)) && (celt == 1)) {
    // There's only one table that matches the given IID

    if (SUCCEEDED(pTable->QueryInterface(iid, (void **) ppUnk))) {
      pTable->Release();
      pEnumTables->Release();

      return S_OK;
    }

    pTable->Release();
  }

  pEnumTables->Release();

  return E_FAIL;
}
