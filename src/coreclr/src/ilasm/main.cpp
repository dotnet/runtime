// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// File:  main.cpp
//

//

#include "ilasmpch.h"

#include "asmparse.h"
#include "ndpversion.h"
#include "shimload.h"

#include "strsafe.h"
#define ASSERTE_ALL_BUILDS(expr) _ASSERTE_ALL_BUILDS(__FILE__, (expr))

WCHAR* EqualOrColon(__in __nullterminated WCHAR* szArg)
{
    WCHAR* pchE = wcschr(szArg,L'=');
    WCHAR* pchC = wcschr(szArg,L':');
    WCHAR* ret;
    if(pchE == NULL) ret = pchC;
    else if(pchC == NULL) ret = pchE;
    else ret = (pchE < pchC)? pchE : pchC;
    return ret;
}

static DWORD    g_dwSubsystem=(DWORD)-1,g_dwComImageFlags=(DWORD)-1,g_dwFileAlignment=0,g_dwTestRepeat=0;
static ULONGLONG   g_stBaseAddress=0;
static size_t   g_stSizeOfStackReserve=0;
extern unsigned int g_uConsoleCP;
#ifdef FEATURE_PAL
char * g_pszExeFile;
#endif

void MakeTestFile(__in __nullterminated char* szFileName)
{
    if(g_dwTestRepeat)
    {
        FILE* pF = NULL;
        if(fopen_s(&pF,szFileName,"wt")==0 && pF != NULL)
        {
            printf("Making test file\n");
            fprintf(pF,".assembly extern mscorlib {}\n");
            fprintf(pF,".assembly test%d {}\n",g_dwTestRepeat);
            fprintf(pF,".module test%d.exe\n",g_dwTestRepeat);
            fprintf(pF,".method public static void Exec() { .entrypoint\n");
            for(unsigned i=0; i<g_dwTestRepeat*1000; i++)
            {
                fprintf(pF,"ldc.i4.1\ncall void [mscorlib]System.Console::WriteLine(int32)\n");
            }
            fprintf(pF,"ret }\n");
            fclose(pF);
        }
    }
}

void MakeProperSourceFileName(__in __nullterminated WCHAR* wzOrigName, 
                              unsigned uCodePage, 
                              __out_ecount(MAX_FILENAME_LENGTH) WCHAR* wzProperName, 
                              __out_ecount(MAX_FILENAME_LENGTH*3) char* szProperName)
{
    wcscpy_s(wzProperName,MAX_FILENAME_LENGTH, wzOrigName);
    size_t j = wcslen(wzProperName);
    do
    {
        j--;
        if(wzProperName[j] == '.') break;
        if((wzProperName[j] == '\\')||(j == 0))
        {
            wcscat_s(wzProperName,MAX_FILENAME_LENGTH,W(".il"));
            break;
        }
    }
    while(j);
    WszWideCharToMultiByte(uCodePage,0,wzProperName,-1,szProperName,MAX_FILENAME_LENGTH*3-1,NULL,NULL);
}

char* FullFileName(__in __nullterminated WCHAR* wzFileName, unsigned uCodePage)
{
    static WCHAR wzFullPath[MAX_FILENAME_LENGTH];
    WCHAR* pwz;
    WszGetFullPathName(wzFileName,MAX_FILENAME_LENGTH,wzFullPath,&pwz);
    char szFullPath[MAX_FILENAME_LENGTH*3];
    WszWideCharToMultiByte(uCodePage,0,wzFullPath,-1,szFullPath,MAX_FILENAME_LENGTH*3-1,NULL,NULL);
    char* sz = new char[strlen(szFullPath)+1];
    if(sz) strcpy_s(sz,strlen(szFullPath)+1,szFullPath);
    return sz;
}

WCHAR       *pwzInputFiles[1024];
WCHAR       *pwzDeltaFiles[1024];

char        szInputFilename[MAX_FILENAME_LENGTH*3];
WCHAR       wzInputFilename[MAX_FILENAME_LENGTH];
WCHAR       wzOutputFilename[MAX_FILENAME_LENGTH];
WCHAR       wzIncludePathBuffer[MAX_FILENAME_LENGTH];

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif

extern "C" int _cdecl wmain(int argc, __in WCHAR **argv)
{
    int         i, NumFiles = 0, NumDeltaFiles = 0;
    bool        IsDLL = false, IsOBJ = false;
    char        szOpt[128];
    Assembler   *pAsm;
    MappedFileStream *pIn;
    AsmParse    *pParser;
    int         exitval=1;
    bool        bLogo = TRUE;
    bool        bReportProgress = TRUE;
    BOOL        bNoDebug = TRUE;
    WCHAR*      wzIncludePath = NULL;
    int exitcode = 0;
    unsigned    uCodePage;

    bool bClock = false;
    Clockwork   cw;

    // SWI has requested that the exact form of the function call below be used. For details
    // see http://swi/SWI%20Docs/Detecting%20Heap%20Corruption.doc
    (void)HeapSetInformation(NULL, HeapEnableTerminationOnCorruption, NULL, 0);

    memset(pwzInputFiles,0,1024*sizeof(WCHAR*));
    memset(pwzDeltaFiles,0,1024*sizeof(WCHAR*));
    memset(&cw,0,sizeof(Clockwork));
    cw.cBegin = GetTickCount();

    g_uConsoleCP = GetConsoleOutputCP();
    memset(wzOutputFilename,0,sizeof(wzOutputFilename));

#ifdef _DEBUG
    DisableThrowCheck();
    //CONTRACT_VIOLATION(ThrowsViolation);
#endif

    if(argc < 2) goto ErrorExit;
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:26000) // "Suppress prefast warning about index overflow"
#endif
    if (! wcscmp(argv[1], W("/?")) || ! wcscmp(argv[1],W("-?")))
#ifdef _PREFAST_
#pragma warning(pop)
#endif    
    {
        printf("\nMicrosoft (R) .NET Framework IL Assembler version " VER_FILEVERSION_STR);
        printf("\n%S\n\n", VER_LEGALCOPYRIGHT_LOGO_STR_L);
        goto PrintUsageAndExit;

    ErrorExit:
      exitcode = 1;
    PrintUsageAndExit:
      printf("\n\nUsage: ilasm [Options] <sourcefile> [Options]");
      printf("\n\nOptions:");
      printf("\n/NOLOGO         Don't type the logo");
      printf("\n/QUIET          Don't report assembly progress");
      printf("\n/NOAUTOINHERIT  Disable inheriting from System.Object by default");
      printf("\n/DLL            Compile to .dll");
      printf("\n/EXE            Compile to .exe (default)");
      printf("\n/PDB            Create the PDB file without enabling debug info tracking");
      printf("\n/APPCONTAINER   Create an AppContainer exe or dll");
      printf("\n/DEBUG          Disable JIT optimization, create PDB file, use sequence points from PDB");
      printf("\n/DEBUG=IMPL     Disable JIT optimization, create PDB file, use implicit sequence points");
      printf("\n/DEBUG=OPT      Enable JIT optimization, create PDB file, use implicit sequence points");
      printf("\n/OPTIMIZE       Optimize long instructions to short");
      printf("\n/FOLD           Fold the identical method bodies into one");
      printf("\n/CLOCK          Measure and report compilation times");
//      printf("\n/ERROR          Try to create .exe or .dll file despite errors reported");
//      printf("\n       Warning! Results are unpredictable, use this option at your own risk!");
      printf("\n/RESOURCE=<res_file>    Link the specified resource file (*.res) \n\t\t\tinto resulting .exe or .dll");
      printf("\n/OUTPUT=<targetfile>    Compile to file with specified name \n\t\t\t(user must provide extension, if any)");
      printf("\n/KEY=<keyfile>      Compile with strong signature \n\t\t\t(<keyfile> contains private key)");
      printf("\n/KEY=@<keysource>   Compile with strong signature \n\t\t\t(<keysource> is the private key source name)");
      printf("\n/INCLUDE=<path>     Set path to search for #include'd files");
      printf("\n/SUBSYSTEM=<int>    Set Subsystem value in the NT Optional header");
      printf("\n/SSVER=<int>.<int>  Set Subsystem version number in the NT Optional header");
      printf("\n/FLAGS=<int>        Set CLR ImageFlags value in the CLR header");
      printf("\n/ALIGNMENT=<int>    Set FileAlignment value in the NT Optional header");
      printf("\n/BASE=<int>     Set ImageBase value in the NT Optional header (max 2GB for 32-bit images)");
      printf("\n/STACK=<int>    Set SizeOfStackReserve value in the NT Optional header");
      printf("\n/MDV=<version_string>   Set Metadata version string");
      printf("\n/MSV=<int>.<int>   Set Metadata stream version (<major>.<minor>)");
      printf("\n/PE64           Create a 64bit image (PE32+)");
      printf("\n/HIGHENTROPYVA  Set High Entropy Virtual Address capable PE32+ images (default for /APPCONTAINER)");
      printf("\n/NOCORSTUB      Suppress generation of CORExeMain stub");
      printf("\n/STRIPRELOC     Indicate that no base relocations are needed");
      printf("\n/ITANIUM        Target processor: Intel Itanium");
      printf("\n/X64            Target processor: 64bit AMD processor");
      printf("\n/ARM            Target processor: ARM processor");
      printf("\n/32BITPREFERRED Create a 32BitPreferred image (PE32)");
      printf("\n/ENC=<file>     Create Edit-and-Continue deltas from specified source file");
      
      printf("\n\nKey may be '-' or '/'\nOptions are recognized by first 3 characters\nDefault source file extension is .il\n");

      printf("\nTarget defaults:");
      printf("\n/PE64      => /PE64 /ITANIUM");
      printf("\n/ITANIUM   => /PE64 /ITANIUM");
      printf("\n/X64       => /PE64 /X64");

      printf("\n\n");
      exit(exitcode);
    }

    uCodePage = CP_UTF8;
    WszSetEnvironmentVariable(W("COMP_ENC_OPENSCOPE"), W(""));
    WszSetEnvironmentVariable(W("COMP_ENC_EMIT"), W(""));
    if((pAsm = new Assembler()))
    {
        pAsm->SetCodePage(uCodePage);
        //if(pAsm->Init())
        {
            pAsm->SetStdMapping(1);
            //-------------------------------------------------
            for (i = 1; i < argc; i++)
            {
#ifdef FEATURE_PAL
                if(argv[i][0] == L'-')
#else
                if((argv[i][0] == L'/') || (argv[i][0] == L'-'))
#endif
                {
                    memset(szOpt,0,sizeof(szOpt));
                    WszWideCharToMultiByte(uCodePage,0,&argv[i][1],-1,szOpt,sizeof(szOpt),NULL,NULL);
                    szOpt[3] = 0;
                    if (!_stricmp(szOpt,"NOA"))
                    {
                        pAsm->m_fAutoInheritFromObject = FALSE;
                    }
                    else if (!_stricmp(szOpt,"QUI"))
                    {
                        pAsm->m_fReportProgress = FALSE;
                        bReportProgress = FALSE;
                        bLogo = FALSE;
                    }
                    else if (!_stricmp(szOpt, "NOL"))
                    {
                        bLogo = FALSE;
                    }
                    else if (!_stricmp(szOpt, "FOL"))
                    {
                      pAsm->m_fFoldCode = TRUE;
                    }
                    else if (!_stricmp(szOpt, "DEB"))
                    {
                      pAsm->m_dwIncludeDebugInfo = 0x101;
#ifdef FEATURE_CORECLR
                      // PDB is ignored under 'DEB' option for ilasm on CoreCLR.
                      // https://github.com/dotnet/coreclr/issues/2982
#else
                      pAsm->m_fGeneratePDB = TRUE;
#endif
                      bNoDebug = FALSE;

                      WCHAR *pStr = EqualOrColon(argv[i]);
                      if(pStr != NULL)
                      {
                          for(pStr++; *pStr == L' '; pStr++); //skip the blanks
                          if(wcslen(pStr)==0) goto InvalidOption; //if no suboption
                          else
                          {
                              WCHAR wzSubOpt[8];
                              wcsncpy_s(wzSubOpt,8,pStr,3);
                              wzSubOpt[3] = 0;
                              if(0 == _wcsicmp(wzSubOpt,W("OPT")))
                                pAsm->m_dwIncludeDebugInfo = 0x3;
                              else if(0 == _wcsicmp(wzSubOpt,W("IMP")))
                                pAsm->m_dwIncludeDebugInfo = 0x103;
                              else 
                              {
                                const WCHAR *pFmt =((*pStr == '0')&&(*(pStr+1) == 'x'))? W("%lx") : W("%ld");
                                if(swscanf_s(pStr,pFmt,&(pAsm->m_dwIncludeDebugInfo))!=1)
                                goto InvalidOption; // bad subooption
                              }
                          }
                      }
                    }
                    else if (!_stricmp(szOpt, "PDB"))
                    {
#ifdef FEATURE_CORECLR
                      // 'PDB' option is ignored for ilasm on CoreCLR.
                      // https://github.com/dotnet/coreclr/issues/2982
#else
                      pAsm->m_fGeneratePDB = TRUE;
#endif
                      bNoDebug = FALSE;
                    }
                    else if (!_stricmp(szOpt, "CLO"))
                    {
                      bClock = true;
                      pAsm->SetClock(&cw);
                    }
                    else if (!_stricmp(szOpt, "DLL"))
                    {
                      IsDLL = true; IsOBJ = false;
                    }
                    else if (!_stricmp(szOpt, "OBJ"))
                    {
                      //IsOBJ = true; IsDLL = false;
                      printf("Option /OBJECT is not supported.\n");
                      goto ErrorExit;
                    }
                    else if (!_stricmp(szOpt, "ERR"))
                    {
                      pAsm->OnErrGo = true;
                    }
                    else if (!_stricmp(szOpt, "EXE"))
                    {
                      IsDLL = false;
                    }
                    else if (!_stricmp(szOpt, "APP"))
                    {
                        pAsm->m_fAppContainer = TRUE;
                    }
                    else if (!_stricmp(szOpt, "HIG"))
                    {
                        pAsm->m_fHighEntropyVA = TRUE;
                    }
                    else if (!_stricmp(szOpt, "OPT"))
                    {
                      pAsm->m_fOptimize = TRUE;
                    }
                    else if (!_stricmp(szOpt, "ITA"))
                    {
                      pAsm->m_dwCeeFileFlags &= ~ICEE_CREATE_MACHINE_MASK;
                      pAsm->m_dwCeeFileFlags |= ICEE_CREATE_MACHINE_IA64;
                    }
                    else if (!_stricmp(szOpt, "X64"))
                    {
                      pAsm->m_dwCeeFileFlags &= ~ICEE_CREATE_MACHINE_MASK;        
                      pAsm->m_dwCeeFileFlags |= ICEE_CREATE_MACHINE_AMD64;
                    }
                    else if (!_stricmp(szOpt, "ARM"))
                    {
                      pAsm->m_dwCeeFileFlags &= ~ICEE_CREATE_MACHINE_MASK;        
                      pAsm->m_dwCeeFileFlags |= ICEE_CREATE_MACHINE_ARM;
                    }
                    else if (!_stricmp(szOpt, "32B"))
                    {
                        if (g_dwComImageFlags == (DWORD)-1)
                            g_dwComImageFlags = pAsm->m_dwComImageFlags;
                        COR_SET_32BIT_PREFERRED(g_dwComImageFlags);
                    }
                    else if (!_stricmp(szOpt, "PE6"))
                    {
                      pAsm->m_dwCeeFileFlags &= ~ICEE_CREATE_FILE_PE32;
                      pAsm->m_dwCeeFileFlags |= ICEE_CREATE_FILE_PE64;
                    }
                    else if (!_stricmp(szOpt, "NOC"))
                    {
                      pAsm->m_dwCeeFileFlags &= ~ICEE_CREATE_FILE_CORMAIN_STUB;
                    }
                    else if (!_stricmp(szOpt, "STR"))
                    {
                      pAsm->m_dwCeeFileFlags |= ICEE_CREATE_FILE_STRIP_RELOCS;
                    }
                    else if (!_stricmp(szOpt, "OPT"))
                    {
                      pAsm->m_fOptimize = TRUE;
                    }
                    else if (!_stricmp(szOpt, "LIS"))
                    {
                        printf("Option /LISTING is not supported, use ILDASM.EXE\n");
                    }
                    else if (!_stricmp(szOpt, "RES"))
                    {
                        if(pAsm->m_wzResourceFile==NULL)
                        {
                            WCHAR *pStr = EqualOrColon(argv[i]);
                            if(pStr == NULL) goto ErrorExit;
                            for(pStr++; *pStr == L' '; pStr++); //skip the blanks
                            if(wcslen(pStr)==0) goto InvalidOption; //if no file name
                            pAsm->m_wzResourceFile = pStr;
                        }
                        else
                            printf("Multiple resource files not allowed. Option %ls skipped\n",argv[i]);
                    }
                    else if (!_stricmp(szOpt, "KEY"))
                    {
                        WCHAR *pStr = EqualOrColon(argv[i]);
                        if(pStr == NULL) goto InvalidOption;
                        for(pStr++; *pStr == L' '; pStr++); //skip the blanks
                        if(wcslen(pStr)==0) goto InvalidOption; //if no file name
                        pAsm->m_wzKeySourceName = pStr;
                    }
                    else if (!_stricmp(szOpt, "INC"))
                    {
                        WCHAR *pStr = EqualOrColon(argv[i]);
                        if(pStr == NULL) goto InvalidOption;
                        for(pStr++; *pStr == L' '; pStr++); //skip the blanks
                        if(wcslen(pStr)==0) goto InvalidOption; //if no file name
                        wzIncludePath = pStr;
                    }
                    else if (!_stricmp(szOpt, "OUT"))
                    {
                        WCHAR *pStr = EqualOrColon(argv[i]);
                        if(pStr == NULL) goto InvalidOption;
                        for(pStr++; *pStr == L' '; pStr++); //skip the blanks
                        if(wcslen(pStr)==0) goto InvalidOption; //if no file name
                        if(wcslen(pStr) >= MAX_FILENAME_LENGTH)
                        {
                            fprintf(stderr,"\nError: Output file name exceeds %d characters\n",MAX_FILENAME_LENGTH-1);
                            goto ErrorExit;
                        }
                        wcscpy_s(wzOutputFilename,MAX_FILENAME_LENGTH,pStr);
                    }
                    else if (!_stricmp(szOpt, "MDV"))
                    {
                        WCHAR *pStr = EqualOrColon(argv[i]);
                        if(pStr == NULL) goto InvalidOption;
                        for(pStr++; *pStr == L' '; pStr++); //skip the blanks
                        if(wcslen(pStr)==0) goto InvalidOption; //if no version string
                        pAsm->m_wzMetadataVersion = pStr;
                    }
                    else if (!_stricmp(szOpt, "MSV"))
                    {
                        WCHAR *pStr = EqualOrColon(argv[i]);
                        if(pStr == NULL) goto InvalidOption;
                        for(pStr++; *pStr == L' '; pStr++); //skip the blanks
                        if(wcslen(pStr)==0) goto InvalidOption; //if no version
                        {
                            int major=-1,minor=-1;
                            if(swscanf_s(pStr,W("%d.%d"),&major, &minor)==2)
                            {
                                if((major >= 0)&&(major < 0xFF))
                                    pAsm->m_wMSVmajor = (WORD)major;
                                if((minor >= 0)&&(minor < 0xFF))
                                    pAsm->m_wMSVminor = (WORD)minor;
                            }
                        }
                    }
                    else if (!_stricmp(szOpt, "ENC"))
                    {
                        WCHAR *pStr = EqualOrColon(argv[i]);
                        if(pStr == NULL) goto InvalidOption;
                        for(pStr++; *pStr == L' '; pStr++); //skip the blanks
                        if(wcslen(pStr)==0) goto InvalidOption; //if no file name
                        pwzDeltaFiles[NumDeltaFiles++] = pStr;
                        pAsm->m_fTolerateDupMethods = TRUE;
                    }
                    else if (!_stricmp(szOpt, "SUB"))
                    {
                        WCHAR *pStr = EqualOrColon(argv[i]);
                        if(pStr == NULL) goto InvalidOption;
                        pStr++;
                        const WCHAR *pFmt = ((*pStr=='0')&&(*(pStr+1) == 'x'))? W("%lx") : W("%ld");
                        if(swscanf_s(pStr,pFmt,&g_dwSubsystem)!=1) goto InvalidOption;
                    }
                    else if (!_stricmp(szOpt, "SSV"))
                    {
                        WCHAR *pStr = EqualOrColon(argv[i]);
                        if(pStr == NULL) goto InvalidOption;
                        for(pStr++; *pStr == L' '; pStr++); //skip the blanks
                        if(wcslen(pStr)==0) goto InvalidOption; //if no version
                        {
                            int major=-1,minor=-1;
                            if(swscanf_s(pStr,W("%d.%d"),&major, &minor)==2)
                            {
                                if((major >= 0)&&(major < 0xFFFF))
                                    pAsm->m_wSSVersionMajor = (WORD)major;
                                if((minor >= 0)&&(minor < 0xFFFF))
                                    pAsm->m_wSSVersionMinor = (WORD)minor;
                            } else 
                                goto InvalidOption;
                        }
                    }
                    else if (!_stricmp(szOpt, "ALI"))
                    {
                        WCHAR *pStr = EqualOrColon(argv[i]);
                        if(pStr == NULL) goto InvalidOption;
                        pStr++;
                        const WCHAR *pFmt = ((*pStr=='0')&&(*(pStr+1) == 'x'))? W("%lx") : W("%ld");
                        if(swscanf_s(pStr,pFmt,&g_dwFileAlignment)!=1) goto InvalidOption;
                        if((g_dwFileAlignment & (g_dwFileAlignment-1))
                           || (g_dwFileAlignment < 0x200) || (g_dwFileAlignment > 0x10000))
                        {
                            fprintf(stderr,"\nFile Alignment must be power of 2 from 0x200 to 0x10000\n");
                            if(!pAsm->OnErrGo) goto InvalidOption;
                        }
                    }
                    else if (!_stricmp(szOpt, "FLA"))
                    {
                        WCHAR *pStr = EqualOrColon(argv[i]);
                        if(pStr == NULL) goto InvalidOption;
                        pStr++;
                        const WCHAR *pFmt = ((*pStr=='0')&&(*(pStr+1) == 'x'))? W("%lx") : W("%ld");
                        if(swscanf_s(pStr,pFmt,&g_dwComImageFlags)!=1) goto InvalidOption;
                    }
                    else if (!_stricmp(szOpt, "BAS"))
                    {
                        WCHAR *pStr = EqualOrColon(argv[i]);
                        if(pStr == NULL) goto InvalidOption;
                        pStr++;
                        const WCHAR *pFmt = ((*pStr=='0')&&(*(pStr+1) == 'x'))? W("%I64x") : W("%I64d");
                        if(swscanf_s(pStr,pFmt,&g_stBaseAddress)!=1) goto InvalidOption;
                        if(g_stBaseAddress & 0xFFFF)
                        {
                            fprintf(stderr,"\nBase address must be 0x10000-aligned\n");
                            if(!pAsm->OnErrGo) goto InvalidOption;
                        }
                    }
                    else if (!_stricmp(szOpt, "STA"))
                    {
                        WCHAR *pStr = EqualOrColon(argv[i]);
                        if(pStr == NULL) goto InvalidOption;
                        pStr++;
                        const WCHAR *pFmt = ((*pStr=='0')&&(*(pStr+1) == 'x'))? W("%lx") : W("%ld");
                        if(swscanf_s(pStr,pFmt,&g_stSizeOfStackReserve)!=1) goto InvalidOption;
                    }
#ifdef _SPECIAL_INTERNAL_USE_ONLY
                    else if (!_stricmp(szOpt, "TES"))
                    {
                        WCHAR *pStr = EqualOrColon(argv[i]);
                        if(pStr == NULL) goto InvalidOption;
                        pStr++;
                        WCHAR *pFmt = ((*pStr=='0')&&(*(pStr+1) == 'x'))? L"%lx" : L"%ld";
                        if(swscanf_s(pStr,pFmt,&g_dwTestRepeat)!=1) goto InvalidOption;
                    }
#endif
                    else
                    {
                    InvalidOption:
                        fprintf(stderr, "Error : Invalid Option: %LS\n", argv[i]);
                        goto ErrorExit;
                    }
                }
                else
                {
                    if(wcslen(argv[i]) >= MAX_FILENAME_LENGTH)
                    {
                        printf("\nError: Input file name exceeds %d characters\n",MAX_FILENAME_LENGTH-1);
                        goto ErrorExit;
                    }
                    pwzInputFiles[NumFiles++] = argv[i];
                    if(NumFiles == 1)
                    {
                        MakeProperSourceFileName(argv[i], uCodePage, wzInputFilename, szInputFilename);
                    }
                }

            }
            if(NumFiles == 0)
            {
                delete pAsm;
                goto ErrorExit;
            }
            if(pAsm->m_dwCeeFileFlags & ICEE_CREATE_FILE_PE64)
            {
                if((pAsm->m_dwCeeFileFlags & ICEE_CREATE_MACHINE_I386)
                   ||(pAsm->m_dwCeeFileFlags & ICEE_CREATE_MACHINE_ARM))
                {
                    printf("\nMachine type /ITANIUM or /X64 must be specified for 64 bit targets.");
                    if(!pAsm->OnErrGo)
                    {
                        pAsm->m_dwCeeFileFlags &= ~ICEE_CREATE_MACHINE_MASK;
                        pAsm->m_dwCeeFileFlags |= ICEE_CREATE_MACHINE_IA64;
                        printf(" Type set to ITANIUM.");
                    }
                    printf("\n");
                }
            }
            else
            {
                if((pAsm->m_dwCeeFileFlags & ICEE_CREATE_MACHINE_IA64)
                  ||(pAsm->m_dwCeeFileFlags & ICEE_CREATE_MACHINE_AMD64))
                {
                    printf("\n64 bit target must be specified for machine type /ITANIUM or /X64.");
                    if(!pAsm->OnErrGo)
                    {
                        pAsm->m_dwCeeFileFlags &= ~ICEE_CREATE_FILE_PE32;
                        pAsm->m_dwCeeFileFlags |= ICEE_CREATE_FILE_PE64;
                        printf(" Target set to 64 bit.");
                    }
                    printf("\n");
                }
            }
            if((pAsm->m_dwCeeFileFlags & ICEE_CREATE_MACHINE_IA64))
            {
                pAsm->m_dwComImageFlags &= ~COMIMAGE_FLAGS_ILONLY;
            }
            if(pAsm->m_dwCeeFileFlags & ICEE_CREATE_FILE_PE32)
            {
                if(g_stBaseAddress > 0x80000000)
                {
                    fprintf(stderr,"Invalid Image Base specified for 32-bit target\n");
                    delete pAsm;
                    goto ErrorExit;
                }
            }
            if (COR_IS_32BIT_PREFERRED(pAsm->m_dwComImageFlags) &&
                ((pAsm->m_dwCeeFileFlags & ICEE_CREATE_FILE_PE64) ||
                 ((pAsm->m_dwCeeFileFlags & ICEE_CREATE_FILE_PE32) == 0) ||
                 ((pAsm->m_dwCeeFileFlags & ICEE_CREATE_MACHINE_I386) == 0) ||
                 ((pAsm->m_dwComImageFlags & COMIMAGE_FLAGS_ILONLY) == 0)))
            {
                fprintf(stderr,"/32BITPREFERRED valid only with PE32/X86/ILONLY images\n");
                delete pAsm;
                goto ErrorExit;
            }
            if(!pAsm->Init())
            {
                fprintf(stderr,"Failed to initialize Assembler\n");
                delete pAsm;
                goto ErrorExit;
            }
            if(g_dwTestRepeat)
                MakeTestFile(szInputFilename);

            if(wzOutputFilename[0] == 0)
            {
                wcscpy_s(wzOutputFilename,MAX_FILENAME_LENGTH,pwzInputFiles[0]);
                size_t j = wcslen(wzOutputFilename);
                do
                {
                    j--;
                    if(wzOutputFilename[j] == L'.')
                    {
                        wzOutputFilename[j] = 0;
                        break;
                    }
                }
                while(j);
                wcscat_s(wzOutputFilename, MAX_FILENAME_LENGTH,(IsDLL ? W(".dll") : (IsOBJ ? W(".obj") : W(".exe"))));
            }
            if(wzIncludePath == NULL)
            {
                if(0!=WszGetEnvironmentVariable(W("ILASM_INCLUDE"),wzIncludePathBuffer,MAX_FILENAME_LENGTH))
                    wzIncludePath = wzIncludePathBuffer;
            }
            //------------ Assembler initialization done. Now, to business -----------------------
            if((pParser = new AsmParse(NULL, pAsm)))
            {
                uCodePage = CP_UTF8;
                pAsm->SetCodePage(uCodePage);
                pParser->SetIncludePath(wzIncludePath);
                //======================================================================
                if(bLogo)
                {
                    printf("\nMicrosoft (R) .NET Framework IL Assembler.  Version " VER_FILEVERSION_STR);
                    printf("\n%S", VER_LEGALCOPYRIGHT_LOGO_STR_L);
                }

                pAsm->SetDLL(IsDLL);
                pAsm->SetOBJ(IsOBJ);
                wcscpy_s(pAsm->m_wzOutputFileName,MAX_FILENAME_LENGTH,wzOutputFilename);
                strcpy_s(pAsm->m_szSourceFileName,MAX_FILENAME_LENGTH*3+1,szInputFilename);

                if (SUCCEEDED(pAsm->InitMetaData()))
                {
                    int iFile;
                    BOOL fAllFilesPresent = TRUE;
                    if(bClock) cw.cParsBegin = GetTickCount();
                    for(iFile = 0; iFile < NumFiles; iFile++)
                    {
                        uCodePage = CP_UTF8;
                        pAsm->SetCodePage(uCodePage);
                        if(iFile) // for the first file, it's already done
                        {
                            MakeProperSourceFileName(pwzInputFiles[iFile], uCodePage, wzInputFilename, szInputFilename);
                        }
                        if(pAsm->m_fReportProgress)
                        {
                            pParser->msg("\nAssembling '%s' ", szInputFilename);
                            if(pAsm->m_fCPlusPlus)  pParser->msg(" C++");
                            if(pAsm->m_fWindowsCE)  pParser->msg(" WINCE");
                            if(!pAsm->m_fAutoInheritFromObject) pParser->msg(" NOAUTOINHERIT");
                            pParser->msg(IsDLL ? " to DLL" : (IsOBJ? " to OBJ" : " to EXE"));
                            //======================================================================
                            if (pAsm->m_fStdMapping == FALSE)
                                pParser->msg(", with REFERENCE mapping");

                            {
                                char szOutputFilename[MAX_FILENAME_LENGTH*3];
                                memset(szOutputFilename,0,sizeof(szOutputFilename));
                                WszWideCharToMultiByte(uCodePage,0,wzOutputFilename,-1,szOutputFilename,MAX_FILENAME_LENGTH*3-1,NULL,NULL);
                                pParser->msg(" --> '%s'\n", szOutputFilename);
                            }
                        }
                            
                        pIn = new MappedFileStream(wzInputFilename);

                        if ((!pIn) || !(pIn->IsValid()))
                        {
                            pParser->msg("Could not open %s\n", szInputFilename);
                            fAllFilesPresent = FALSE;
                        }
                        else
                        {
#ifndef FEATURE_PAL
                            DWORD dwBinType;
                            if(GetBinaryTypeA(szInputFilename,&dwBinType))
                            {
                                pParser->msg("%s is not a text file\n",szInputFilename);
                                fAllFilesPresent = FALSE;
                            }
                            else
#endif
                            {
                                pAsm->SetSourceFileName(FullFileName(wzInputFilename,uCodePage)); // deletes the argument!

                                pParser->ParseFile(pIn);
                            }
                        }
                        if(pIn)
                        {
                            pIn->set_namew(NULL);
                            delete pIn;
                        }
                    } // end for(iFile)
                    if(bClock) cw.cParsEnd = GetTickCount();
                    if ((pParser->Success() && fAllFilesPresent) || pAsm->OnErrGo)
                    {
                        HRESULT hr;
                        if(g_dwSubsystem  != (DWORD)-1)      pAsm->m_dwSubsystem = g_dwSubsystem;
                        if(g_dwComImageFlags != (DWORD)-1)   pAsm->m_dwComImageFlags = g_dwComImageFlags;
                        if(g_dwFileAlignment)   pAsm->m_dwFileAlignment = g_dwFileAlignment;
                        if(g_stBaseAddress)     pAsm->m_stBaseAddress = g_stBaseAddress;
                        if(g_stSizeOfStackReserve)     pAsm->m_stSizeOfStackReserve = g_stSizeOfStackReserve;
                        if(FAILED(hr=pAsm->CreatePEFile(wzOutputFilename)))
                            pParser->msg("Could not create output file, error code=0x%08X\n",hr);
                        else
                        {
                            if(pAsm->m_fFoldCode && pAsm->m_fReportProgress)
                                pParser->msg("%d methods folded\n",pAsm->m_dwMethodsFolded);
                            if(pParser->Success() && fAllFilesPresent) exitval = 0;
                            else
                            {
                                pParser->msg("Output file contains errors\n");
                                if(pAsm->OnErrGo) exitval = 0;
                            }
                            if(exitval == 0) // Write the output file
                            {
                                if(bClock) cw.cFilegenEnd = GetTickCount();
                                if(pAsm->m_fReportProgress) pParser->msg("Writing %s file\n", pAsm->m_fOBJ ? "COFF" : "PE");
                                // Generate the file
                                if (FAILED(hr = pAsm->m_pCeeFileGen->GenerateCeeFile(pAsm->m_pCeeFile)))
                                {
                                    exitval = 1;
                                    pParser->msg("Failed to write output file, error code=0x%08X\n",hr);
                                }
#ifndef FEATURE_CORECLR
                                else if (pAsm->m_pManifest->m_sStrongName.m_fFullSign)
                                {
                                    // Strong name sign the resultant assembly.
                                    if(pAsm->m_fReportProgress) pParser->msg("Signing file with strong name\n");
                                    if (FAILED(hr=pAsm->StrongNameSign()))
                                    {
                                        exitval = 1;
                                        pParser->msg("Failed to strong name sign output file, error code=0x%08X\n",hr);
                                    }
                                }
#endif
                                if(bClock) cw.cEnd = GetTickCount();
#define ENC_ENABLED
#ifdef ENC_ENABLED
                                if(exitval==0)
                                {
                                    pAsm->m_fENCMode = TRUE;
                                    WCHAR wzNewOutputFilename[MAX_FILENAME_LENGTH+16];
                                    for(iFile = 0; iFile < NumDeltaFiles; iFile++)
                                    {
                                        wcscpy_s(wzNewOutputFilename,MAX_FILENAME_LENGTH+16,wzOutputFilename);
                                        exitval = (int)StringCchPrintfW(&wzNewOutputFilename[wcslen(wzNewOutputFilename)], 32,
                                                 W(".%d"),iFile+1);
                                        MakeProperSourceFileName(pwzDeltaFiles[iFile], uCodePage, wzInputFilename, szInputFilename);
                                        if(pAsm->m_fReportProgress)
                                        {
                                            pParser->msg("\nAssembling delta '%s' ", szInputFilename);
                                            if(pAsm->m_fCPlusPlus)  pParser->msg(" C++");
                                            if(pAsm->m_fWindowsCE)  pParser->msg(" WINCE");
                                            if(!pAsm->m_fAutoInheritFromObject) pParser->msg(" NOAUTOINHERIT");
                                            pParser->msg(" to DMETA,DIL");
                                            //======================================================================
                                            if (pAsm->m_fStdMapping == FALSE)
                                                pParser->msg(", with REFERENCE mapping");

                                            pParser->msg(" --> '%S.*'\n", wzNewOutputFilename);
                                        }
                                        exitval = 0;
                                        pIn = new MappedFileStream(wzInputFilename);

                                        if ((!pIn) || !(pIn->IsValid()))
                                        {
                                            pParser->msg("Could not open %s\n", szInputFilename);
                                            fAllFilesPresent = FALSE;
                                        }
                                        else
                                        {
#ifndef FEATURE_PAL
                                            DWORD dwBinType;
                                            if(GetBinaryTypeA(szInputFilename,&dwBinType))
                                            {
                                                pParser->msg("%s is not a text file\n",szInputFilename);
                                                fAllFilesPresent = FALSE;
                                            }
                                            else
#endif
                                            if (SUCCEEDED(pAsm->InitMetaDataForENC(wzNewOutputFilename)))
                                            {
                                                pAsm->SetSourceFileName(FullFileName(wzInputFilename,uCodePage)); // deletes the argument!

                                                pParser->ParseFile(pIn);
                                                if (pParser->Success() || pAsm->OnErrGo)
                                                {
                                                    exitval = 1;
                                                    if(FAILED(hr=pAsm->CreateDeltaFiles(wzNewOutputFilename)))
                                                        pParser->msg("Could not create output delta files, error code=0x%08X\n",hr);
                                                    else
                                                    {
                                                        if(pAsm->m_fFoldCode && pAsm->m_fReportProgress)
                                                            pParser->msg("%d methods folded\n",pAsm->m_dwMethodsFolded);
                                                        if(pParser->Success()) exitval = 0;
                                                        else    pParser->msg("Output delta files contain errors\n");

#ifdef GENERATE_SUMMARY_PE_FILE
                                                        if(pAsm->OnErrGo) exitval = 0;

                                                        //if(FAILED(hr=pAsm->CreatePEFile(wzOutputFilename)))
                                                        //    pParser->msg("Could not create output file, error code=0x%08X\n",hr);
                                                        //else
                                                        {
                                                            if(pAsm->m_fReportProgress) pParser->msg("Writing %s file\n", pAsm->m_fOBJ ? "COFF" : "PE");
                                                            // Generate the file
                                                            if (FAILED(hr = pAsm->m_pCeeFileGen->GenerateCeeFile(pAsm->m_pCeeFile)))
                                                            {
                                                                exitval = 1;
                                                                pParser->msg("Failed to write output file, error code=0x%08X\n",hr);
                                                            }
                                                            else if (pAsm->m_pManifest->m_sStrongName.m_fFullSign)
                                                            {
                                                                // Strong name sign the resultant assembly.
                                                                if(pAsm->m_fReportProgress) pParser->msg("Signing file with strong name\n");
                                                                if (FAILED(hr=pAsm->StrongNameSign()))
                                                                {
                                                                    exitval = 1;
                                                                    pParser->msg("Failed to strong name sign output file, error code=0x%08X\n",hr);
                                                                }
                                                            }
                                                        }
#endif
                                                    }
                                                } // end if (pParser->Success() || pAsm->OnErrGo)
                                            } //end if (SUCCEEDED(pAsm->InitMetaDataForENC()))
                                        } // end if ((!pIn) || !(pIn->IsValid())) -- else
                                        if(pIn)
                                        {
                                            pIn->set_namew(NULL);
                                            delete pIn;
                                        }
                                    } // end for(iFile)
                                } // end if(exitval==0)
#endif
                            }

                        }
                    }
                }
                else pParser->msg("Failed to initialize Meta Data\n");
                delete pParser;
            }
            else printf("Could not create parser\n");
        }
        //else printf("Failed to initialize Assembler\n");
        delete pAsm;
    }
    else printf("Insufficient memory\n");

    WszSetEnvironmentVariable(W("COMP_ENC_OPENSCOPE"), W(""));
    WszSetEnvironmentVariable(W("COMP_ENC_EMIT"), W(""));

    if(exitval || bNoDebug)
    {
        // PE file was not created, or no debug info required. Kill PDB if any
        WCHAR* pc = wcsrchr(wzOutputFilename,L'.');
        if(pc==NULL)
        {
            pc = &wzOutputFilename[wcslen(wzOutputFilename)];
            *pc = L'.';
        }
        wcscpy_s(pc+1,4,W("PDB"));
#undef DeleteFileW
        DeleteFileW(wzOutputFilename);
    }
    if (exitval == 0)
    {
        if(bReportProgress) printf("Operation completed successfully\n");
        if(bClock)
        {
            printf("Timing (msec): Total run                 %d\n",(cw.cEnd-cw.cBegin));
            printf("               Startup                   %d\n",(cw.cParsBegin-cw.cBegin));
            printf("               - MD initialization       %d\n",(cw.cMDInitEnd - cw.cMDInitBegin));
            printf("               Parsing                   %d\n",(cw.cParsEnd - cw.cParsBegin));
            printf("               Emitting MD               %d\n",(cw.cMDEmitEnd - cw.cRef2DefEnd)+(cw.cRef2DefBegin - cw.cMDEmitBegin));
            //printf("                - global fixups         %d\n",(cw.cMDEmit1 - cw.cMDEmitBegin));
            printf("                - SN sig alloc           %d\n",(cw.cMDEmit2 - cw.cMDEmitBegin));
            printf("                - Classes,Methods,Fields %d\n",(cw.cRef2DefBegin - cw.cMDEmit2));
            printf("                - Events,Properties      %d\n",(cw.cMDEmit3 - cw.cRef2DefEnd));
            printf("                - MethodImpls            %d\n",(cw.cMDEmit4 - cw.cMDEmit3));
            printf("                - Manifest,CAs           %d\n",(cw.cMDEmitEnd - cw.cMDEmit4));
            printf("               Ref to Def resolution     %d\n",(cw.cRef2DefEnd - cw.cRef2DefBegin));
            printf("               Fixup and linking         %d\n",(cw.cFilegenBegin - cw.cMDEmitEnd));
            printf("               CEE file generation       %d\n",(cw.cFilegenEnd - cw.cFilegenBegin));
            printf("               PE file writing           %d\n",(cw.cEnd - cw.cFilegenEnd));
        }
    }
    else
    {
        printf("\n***** FAILURE ***** \n");
    }
    exit(exitval);
    return exitval;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

#ifndef FEATURE_CORECLR
HINSTANCE GetModuleInst()
{
    return (NULL);
}
#endif // !FEATURE_CORECLR

#ifdef FEATURE_PAL
int main(int argc, char* str[])
{
    g_pszExeFile = str[0];
    if (0 != PAL_Initialize(argc, str))
    {
        fprintf(stderr,"Error: Fail to PAL_Initialize\n");
        exit(1);
    }

    WCHAR **argv = new WCHAR*[argc];
    for (int i = 0; i < argc; i++) {
        int length = MultiByteToWideChar(CP_ACP, 0, str[i], -1, NULL, 0);
        ASSERTE_ALL_BUILDS(length != 0);

        LPWSTR result = new (nothrow) WCHAR[length];
        ASSERTE_ALL_BUILDS(result != NULL);

        length = MultiByteToWideChar(CP_ACP, 0, str[i], -1, result, length);
        ASSERTE_ALL_BUILDS (length != 0);

        argv[i] = result;
    }

    int ret = wmain(argc, argv);

    for (int i = 0 ; i < argc; i++) {
        delete[] argv[i];
    }
    delete[] argv;

    return ret;
}
#endif // FEATURE_PAL

