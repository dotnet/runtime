// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/************************************************************************************************
 *                                                                                              *
 *  File:    winmain.cpp                                                                        *
 *                                                         *
 *  Purpose: Main program for graphic COM+ 2.0 disassembler ILDASM.exe                          *
 *                                                                                              *
 ************************************************************************************************/
#include "ildasmpch.h"

#include "dynamicarray.h"

#include "dasmenum.hpp"
#include "dis.h"
#include <clrversion.h>
#include "resource.h"

#include "new.hpp"

#define MODE_DUMP_ALL               0
#define MODE_DUMP_CLASS             1
#define MODE_DUMP_CLASS_METHOD      2
#define MODE_DUMP_CLASS_METHOD_SIG  3

// All externs are defined in DASM.CPP
extern BOOL                    g_fDumpIL;
extern BOOL                    g_fDumpHeader;
extern BOOL                    g_fDumpAsmCode;
extern BOOL                    g_fDumpTokens;
extern BOOL                    g_fDumpStats;
extern BOOL                    g_fDumpClassList;
extern BOOL                    g_fDumpTypeList;
extern BOOL                    g_fDumpSummary;
extern BOOL                    g_fDecompile; // still in progress
extern BOOL                    g_fShowRefs;

extern BOOL                    g_fDumpToPerfWriter;

extern BOOL                    g_fShowBytes;
extern BOOL                    g_fShowSource;
extern BOOL                    g_fInsertSourceLines;
extern BOOL                    g_fTryInCode;
extern BOOL                    g_fQuoteAllNames;
extern BOOL                    g_fTDC;
extern BOOL                    g_fShowCA;
extern BOOL                    g_fCAVerbal;

extern char                    g_pszClassToDump[];
extern char                    g_pszMethodToDump[];
extern char                    g_pszSigToDump[];

extern char                    g_szAsmCodeIndent[];

extern DWORD                   g_Mode;

extern char*                    g_pszExeFile;
extern char                     g_szInputFile[]; // in UTF-8
extern WCHAR                    g_wszFullInputFile[]; // in UTF-16
extern char                     g_szOutputFile[]; // in UTF-8
extern char*                    g_pszObjFileName;
extern FILE*                    g_pFile;

extern BOOL                 g_fLimitedVisibility;
extern BOOL                 g_fR2RNativeManifestMetadata;
extern BOOL                 g_fHidePub;
extern BOOL                 g_fHidePriv;
extern BOOL                 g_fHideFam;
extern BOOL                 g_fHideAsm;
extern BOOL                 g_fHideFAA;
extern BOOL                 g_fHideFOA;
extern BOOL                 g_fHidePrivScope;
extern BOOL                 g_fProject;

extern unsigned             g_uCodePage;
extern unsigned             g_uConsoleCP;
extern BOOL                 g_fForwardDecl;
extern BOOL                 g_fUseProperName;

#include "../tools/metainfo/mdinfo.h"
extern BOOL                 g_fDumpMetaInfo;
extern ULONG                g_ulMetaInfoFilter;
HINSTANCE                   g_hInstance;
HINSTANCE                   g_hResources;
HANDLE                      hConsoleOut=NULL;
HANDLE                      hConsoleErr=NULL;
// These are implemented in DASM.CPP:
BOOL Init();
void Uninit();
void Cleanup();
void DumpMetaInfo(_In_ __nullterminated const WCHAR* pszFileName, _In_ __nullterminated const char* pszObjFileName, void* GUICookie);
FILE* OpenOutput(_In_ __nullterminated const char* szFileName);

void PrintLogo()
{
    printf("Microsoft (R) .NET IL Disassembler.  Version " CLR_PRODUCT_VERSION);
    printf("\n%S\n\n", VER_LEGALCOPYRIGHT_LOGO_STR_L);
}

void SyntaxCon()
{
    DWORD l;

    for(l=IDS_USAGE_01; l<= IDS_USAGE_23; l++) printf(RstrANSI(l));
    if(g_fTDC)
    {
        for(l=IDS_USAGE_24; l<= IDS_USAGE_32; l++) printf(RstrANSI(l));
        for(l=IDS_USAGE_34; l<= IDS_USAGE_36; l++) printf(RstrANSI(l));
        for(l=IDS_USAGE_37; l<= IDS_USAGE_39; l++) printf(RstrANSI(l));
    }
    else printf(RstrANSI(IDS_USAGE_40));
    for(l=IDS_USAGE_41; l<= IDS_USAGE_42; l++) printf(RstrANSI(l));

}

char* CheckForDQuotes(__inout __nullterminated char* sz)
{
    char* ret = sz;
    if(*sz == '"')
    {
        ret++;
        sz[strlen(sz)-1] = 0;
    }
    return ret;
}

char* EqualOrColon(_In_ __nullterminated char* szArg)
{
    char* pchE = strchr(szArg,'=');
    char* pchC = strchr(szArg,':');
    char* ret;
    if(pchE == NULL) ret = pchC;
    else if(pchC == NULL) ret = pchE;
    else ret = (pchE < pchC)? pchE : pchC;
    return ret;
}

void GetInputFileFullPath()
{
    // We need the input file's full path to make uses of it later, despite changing CurrentDirectory

    // First, convert back up to UTF16
    DWORD len = (DWORD) strlen(g_szInputFile) + 16;
    WCHAR* wzArg = new WCHAR[len];
    memset(wzArg, 0, len * sizeof(WCHAR));
    WszMultiByteToWideChar(g_uConsoleCP, 0, g_szInputFile, -1, wzArg, len);

    // Get the full path
    len = WszGetFullPathName(wzArg, MAX_PATH, g_wszFullInputFile, NULL);
    VDELETE(wzArg);
}

int ProcessOneArg(_In_ __nullterminated char* szArg, _Out_ char** ppszObjFileName)
{
    char        szOpt[128];
    if(strlen(szArg) == 0) return 0;
    if ((strcmp(szArg, "/?") == 0) || (strcmp(szArg, "-?") == 0)) return 1;

#ifdef TARGET_UNIX
    if(szArg[0] == '-')
#else
    if((szArg[0] == '/') || (szArg[0] == '-'))
#endif
    {
        strncpy_s(szOpt,128, &szArg[1],10);
        szOpt[3] = 0;
        if (_stricmp(szOpt, "dec") == 0)
        {
            g_fDecompile = TRUE;
        }
        else if (_stricmp(szOpt, "hea") == 0)
        {
            g_fDumpHeader = TRUE;
        }
        else if (_stricmp(szOpt, "adv") == 0)
        {
            g_fTDC = TRUE;
        }
        else if (_stricmp(szOpt, "tok") == 0)
        {
            g_fDumpTokens = TRUE;
        }
        else if (_stricmp(szOpt, "noi") == 0)
        {
            g_fDumpAsmCode = FALSE;
        }
        else if (_stricmp(szOpt, "noc") == 0)
        {
            g_fShowCA = FALSE;
        }
        else if (_stricmp(szOpt, "cav") == 0)
        {
            g_fCAVerbal = TRUE;
        }
        else if (_stricmp(szOpt, "not") == 0)
        {
            g_fTryInCode = FALSE;
        }
        else if (_stricmp(szOpt, "raw") == 0)
        {
            g_fTryInCode = FALSE;
        }
        else if (_stricmp(szOpt, "byt") == 0)
        {
            g_fShowBytes = TRUE;
        }
        else if (_stricmp(szOpt, "sou") == 0)
        {
            printf("Warning: 'SOURCE' option is ignored for ildasm on CoreCLR.\n");
        }
        else if (_stricmp(szOpt, "lin") == 0)
        {
            g_fInsertSourceLines = TRUE;
        }
        else if ((_stricmp(szOpt, "sta") == 0)&&g_fTDC)
        {
            g_fDumpStats = g_fTDC;
        }
        else if ((_stricmp(szOpt, "cla") == 0)&&g_fTDC)
        {
            g_fDumpClassList = g_fTDC;
        }
        else if (_stricmp(szOpt, "typ") == 0)
        {
            g_fDumpTypeList = TRUE;
        }
        else if (_stricmp(szOpt, "sum") == 0)
        {
            g_fDumpSummary = TRUE;
        }
        else if (_stricmp(szOpt, "per") == 0)
        {
            g_fDumpToPerfWriter = TRUE;
        }
        else if (_stricmp(szOpt, "for") == 0)
        {
            g_fForwardDecl = TRUE;
        }
        else if (_stricmp(szOpt, "ref") == 0)
        {
            g_fShowRefs = TRUE;
        }
        else if (_stricmp(szOpt, "pub") == 0)
        {
            g_fLimitedVisibility = TRUE;
            g_fHidePub = FALSE;
        }
        else if (_stricmp(szOpt, "r2r") == 0)
        {
             g_fR2RNativeManifestMetadata = TRUE;
             g_fDumpMetaInfo = TRUE;
        }
        else if (_stricmp(szOpt, "pre") == 0)
        {
            //g_fPrettyPrint = TRUE;
        }
        else if (_stricmp(szOpt, "pro") == 0)
        {
            g_fProject = TRUE;
        }
        else if (_stricmp(szOpt, "vis") == 0)
        {
            char *pc = EqualOrColon(szArg);
            char *pStr;
            if(pc == NULL) return -1;
            do {
                pStr = pc+1;
                pStr = CheckForDQuotes(pStr);
                if((pc = strchr(pStr,'+'))) *pc=0;
                if     (!_stricmp(pStr,"pub")) g_fHidePub = FALSE;
                else if(!_stricmp(pStr,"pri")) g_fHidePriv = FALSE;
                else if(!_stricmp(pStr,"fam")) g_fHideFam = FALSE;
                else if(!_stricmp(pStr,"asm")) g_fHideAsm = FALSE;
                else if(!_stricmp(pStr,"faa")) g_fHideFAA = FALSE;
                else if(!_stricmp(pStr,"foa")) g_fHideFOA = FALSE;
                else if(!_stricmp(pStr,"psc")) g_fHidePrivScope = FALSE;
            } while(pc);
            g_fLimitedVisibility = g_fHidePub  ||
                                   g_fHidePriv ||
                                   g_fHideFam  ||
                                   g_fHideAsm  ||
                                   g_fHideFAA  ||
                                   g_fHideFOA  ||
                                   g_fHidePrivScope;
        }
        else if (_stricmp(szOpt, "quo") == 0)
        {
            g_fQuoteAllNames = TRUE;
        }
        else if (_stricmp(szOpt, "utf") == 0)
        {
            g_uCodePage = CP_UTF8;
        }
        else if (_stricmp(szOpt, "uni") == 0)
        {
            g_uCodePage = 0xFFFFFFFF;
        }
        else if (_stricmp(szOpt, "rtf") == 0)
        {
            g_fDumpRTF = TRUE;
            g_fDumpHTML = FALSE;
        }
        else if (_stricmp(szOpt, "htm") == 0)
        {
            g_fDumpRTF = FALSE;
            g_fDumpHTML = TRUE;
        }
        else if (_stricmp(szOpt, "all") == 0)
        {
            g_fDumpStats = g_fTDC;
            g_fDumpHeader = TRUE;
            g_fShowBytes = TRUE;
            g_fDumpClassList = g_fTDC;
            g_fDumpTokens = TRUE;
        }
        else if (_stricmp(szOpt, "ite") == 0)
        {
            char *pStr = EqualOrColon(szArg);
            char *p, *q;
            if(pStr == NULL) return -1;
            pStr++;
            pStr = CheckForDQuotes(pStr);
            // treat it as meaning "dump only class X" or "class X method Y"
            p = strchr(pStr, ':');

            if (p == NULL)
            {
                // dump one class
                g_Mode = MODE_DUMP_CLASS;
                strcpy_s(g_pszClassToDump, MAX_CLASSNAME_LENGTH, pStr);
            }
            else
            {
                *p++ = '\0';
                if (*p != ':') return -1;

                strcpy_s(g_pszClassToDump, MAX_CLASSNAME_LENGTH, pStr);

                p++;

                q = strchr(p, '(');
                if (q == NULL)
                {
                    // dump class::method
                    g_Mode = MODE_DUMP_CLASS_METHOD;
                    strcpy_s(g_pszMethodToDump, MAX_MEMBER_LENGTH, p);
                }
                else
                {
                    // dump class::method(sig)
                    g_Mode = MODE_DUMP_CLASS_METHOD_SIG;
                    *q = '\0';
                    strcpy_s(g_pszMethodToDump, MAX_MEMBER_LENGTH, p);
                    // get rid of external parentheses:
                    q++;
                    strcpy_s(g_pszSigToDump, MAX_SIGNATURE_LENGTH, q);
                }
            }
        }
        else if ((_stricmp(szOpt, "met") == 0)&&g_fTDC)
        {
            char *pStr = EqualOrColon(szArg);
            g_fDumpMetaInfo = TRUE;
            if(pStr)
            {
                char szOptn[64];
                strncpy_s(szOptn, 64, pStr+1,10);
                szOptn[3] = 0; // recognize metainfo specifier by first 3 chars
                if     (_stricmp(szOptn, "hex") == 0) g_ulMetaInfoFilter |= MDInfo::dumpMoreHex;
                else if(_stricmp(szOptn, "csv") == 0) g_ulMetaInfoFilter |= MDInfo::dumpCSV;
                else if(_stricmp(szOptn, "mdh") == 0) g_ulMetaInfoFilter |= MDInfo::dumpHeader;
                else if(_stricmp(szOptn, "raw") == 0) g_ulMetaInfoFilter |= MDInfo::dumpRaw;
                else if(_stricmp(szOptn, "hea") == 0) g_ulMetaInfoFilter |= MDInfo::dumpRawHeaps;
                else if(_stricmp(szOptn, "sch") == 0) g_ulMetaInfoFilter |= MDInfo::dumpSchema;
                else if(_stricmp(szOptn, "unr") == 0) g_ulMetaInfoFilter |= MDInfo::dumpUnsat;
                else if(_stricmp(szOptn, "val") == 0) g_ulMetaInfoFilter |= MDInfo::dumpValidate;
                else if(_stricmp(szOptn, "sta") == 0) g_ulMetaInfoFilter |= MDInfo::dumpStats;
                else return -1;
            }
        }
        else if (_stricmp(szOpt, "obj") == 0)
        {
            char *pStr = EqualOrColon(szArg);
            if(pStr == NULL) return -1;
            pStr++;
            pStr = CheckForDQuotes(pStr);
            *ppszObjFileName = new char[strlen(pStr)+1];
            strcpy_s(*ppszObjFileName,strlen(pStr)+1,pStr);
        }
        else if (_stricmp(szOpt, "out") == 0)
        {
            char *pStr = EqualOrColon(szArg);
            if(pStr == NULL) return -1;
            pStr++;
            pStr = CheckForDQuotes(pStr);
            if(*pStr == 0) return -1;
            if(_stricmp(pStr,"con"))
            {
                strncpy_s(g_szOutputFile, MAX_FILENAME_LENGTH, pStr,MAX_FILENAME_LENGTH-1);
                g_szOutputFile[MAX_FILENAME_LENGTH-1] = 0;
            }
        }
        else
        {
            PrintLogo();
            printf(RstrANSI(IDS_E_INVALIDOPTION),szArg); //"INVALID COMMAND LINE OPTION: %s\n\n",szArg);
            return -1;
        }
    }
    else
    {
        if(g_szInputFile[0])
        {
            PrintLogo();
            printf(RstrANSI(IDS_E_MULTIPLEINPUT)); //"MULTIPLE INPUT FILES SPECIFIED\n\n");
            return -1; // check if it was already specified
        }
        szArg = CheckForDQuotes(szArg);
        strncpy_s(g_szInputFile, MAX_FILENAME_LENGTH,szArg,MAX_FILENAME_LENGTH-1);
        g_szInputFile[MAX_FILENAME_LENGTH-1] = 0;
        GetInputFileFullPath();
    }
    return 0;
}

char* UTF8toANSI(_In_ __nullterminated char* szUTF)
{
    ULONG32 L = (ULONG32) strlen(szUTF)+16;
    WCHAR* wzUnicode = new WCHAR[L];
    memset(wzUnicode,0,L*sizeof(WCHAR));
    WszMultiByteToWideChar(CP_UTF8,0,szUTF,-1,wzUnicode,L);
    L <<= 2;
    char* szANSI = new char[L];
    memset(szANSI,0,L);
    WszWideCharToMultiByte(g_uConsoleCP,0,wzUnicode,-1,szANSI,L,NULL,NULL);
    VDELETE(wzUnicode);
    return szANSI;
}
char* ANSItoUTF8(_In_ __nullterminated char* szANSI)
{
    ULONG32 L = (ULONG32) strlen(szANSI)+16;
    WCHAR* wzUnicode = new WCHAR[L];
    memset(wzUnicode,0,L*sizeof(WCHAR));
    WszMultiByteToWideChar(g_uConsoleCP,0,szANSI,-1,wzUnicode,L);
    L *= 3;
    char* szUTF = new char[L];
    memset(szUTF,0,L);
    WszWideCharToMultiByte(CP_UTF8,0,wzUnicode,-1,szUTF,L,NULL,NULL);
    VDELETE(wzUnicode);
    return szUTF;
}

int ParseCmdLineW(_In_ __nullterminated WCHAR* wzCmdLine, _Out_ char** ppszObjFileName)
{
    int     argc,ret=0;
    LPWSTR* argv= SegmentCommandLine(wzCmdLine, (DWORD*)&argc);
    char*   szArg = new char[2048];
    for(int i=1; i < argc; i++)
    {
        memset(szArg,0,2048);
        WszWideCharToMultiByte(CP_UTF8,0,argv[i],-1,szArg,2048,NULL,NULL);
        if((ret = ProcessOneArg(szArg,ppszObjFileName)) != 0) break;
    }
    VDELETE(szArg);
    return ret;
}

int ParseCmdLineA(_In_ __nullterminated char* szCmdLine, _Out_ char** ppszObjFileName)
{
    if((szCmdLine == NULL)||(*szCmdLine == 0)) return 0;

    // ANSI to UTF-8
    char*       szCmdLineUTF = ANSItoUTF8(szCmdLine);

    // Split into argv[]
    int argc=0, ret = 0;
    DynamicArray<char*> argv;
    char*       pch;
    char*       pchend;
    bool        bUnquoted = true;

    pch = szCmdLineUTF;
    pchend = pch+strlen(szCmdLineUTF);
    while(pch)
    {
        for(; *pch == ' '; pch++); // skip the blanks
        argv[argc++] = pch;
        for(; pch < pchend; pch++)
        {
            if(*pch == '"') bUnquoted = !bUnquoted;
            else if((*pch == ' ')&&bUnquoted) break;
        }

        if(pch < pchend) *pch++ = 0;
        else break;
    }

    for(int i=1; i < argc; i++)
    {
        if((ret = ProcessOneArg(argv[i],ppszObjFileName)) != 0) break;
    }
    VDELETE(szCmdLineUTF);
    return ret;
}

int __cdecl main(int nCmdShow, char* lpCmdLine[])
{
#if defined(TARGET_UNIX)
    if (0 != PAL_Initialize(nCmdShow, lpCmdLine))
    {
        printError(g_pFile, "Error: Fail to PAL_Initialize\n");
        exit(1);
    }
    g_pszExeFile = lpCmdLine[0];
#endif

#ifdef HOST_WINDOWS
    // SWI has requested that the exact form of the function call below be used. For details see http://swi/SWI%20Docs/Detecting%20Heap%20Corruption.doc
    (void)HeapSetInformation(NULL, HeapEnableTerminationOnCorruption, NULL, 0);
#endif

#ifdef _DEBUG
    DisableThrowCheck();
#endif

    int     iCommandLineParsed = 0;
    WCHAR*  wzCommandLine = NULL;
    char*   szCommandLine = NULL;

    g_fUseProperName = TRUE;

    g_pszClassToDump[0]=0;
    g_pszMethodToDump[0]=0;
    g_pszSigToDump[0]=0;
    memset(g_szInputFile,0,MAX_FILENAME_LENGTH);
    memset(g_szOutputFile,0,MAX_FILENAME_LENGTH);
#if defined(_DEBUG)
    g_fTDC = TRUE;
#endif

#undef GetCommandLineW
#undef CreateProcessW
    g_pszObjFileName = NULL;

    g_szAsmCodeIndent[0] = 0;
    hConsoleOut = GetStdHandle(STD_OUTPUT_HANDLE);
    hConsoleErr = GetStdHandle(STD_ERROR_HANDLE);

#ifndef TARGET_UNIX
    // Dev11 #5320 - pull the localized resource loader up so if ParseCmdLineW need resources, they're already loaded
    g_hResources = WszGetModuleHandle(NULL);
#endif

    iCommandLineParsed = ParseCmdLineW((wzCommandLine = GetCommandLineW()),&g_pszObjFileName);

    if(!g_fLimitedVisibility)
    {
        g_fHidePub = FALSE;
        g_fHidePriv = FALSE;
        g_fHideFam = FALSE;
        g_fHideAsm = FALSE;
        g_fHideFAA = FALSE;
        g_fHideFOA = FALSE;
        g_fHidePrivScope = FALSE;
    }
    if(hConsoleOut != INVALID_HANDLE_VALUE) //First pass: console
    {
        g_uConsoleCP = GetConsoleOutputCP();

        if(iCommandLineParsed)
        {
            if(iCommandLineParsed > 0) PrintLogo();
            SyntaxCon();
            exit((iCommandLineParsed == 1) ? 0 : 1);
        }

        {
            DWORD   exitCode = 1;
            if(g_szInputFile[0] == 0)
            {
                SyntaxCon();
                exit(1);
            }
            g_pFile = NULL;
            if(g_szOutputFile[0])
            {
                g_pFile = OpenOutput(g_szOutputFile);
                if(g_pFile == NULL)
                {
                    char sz[4096];
                    sprintf_s(sz,4096,RstrUTF(IDS_E_CANTOPENOUT)/*"Unable to open '%s' for output."*/,   g_szOutputFile);
                    g_uCodePage = CP_ACP;
                    printError(NULL,sz);
                    exit(1);
                }
            }
            else // console output -- force the code page to ANSI
            {
                g_uCodePage = g_uConsoleCP;
                g_fDumpRTF = FALSE;
                g_fDumpHTML = FALSE;
            }
            if (Init() == TRUE)
            {
                exitCode = DumpFile() ? 0 : 1;
                Cleanup();
            }
            Uninit();
            exit(exitCode);
        }
    }
    return 0;
}

