// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// PrintSymbol.cpp : Defines the printing procedures for the symbols
//
// This is a part of the Debug Interface Access SDK 
//
// This source code is only intended as a supplement to the
// Debug Interface Access SDK and related electronic documentation 
// provided with the library.
// See these sources for detailed information regarding the
// Debug Interface Access SDK API.
//

#include "stdafx.h"

#include <malloc.h>

#include "dia2.h"
#include "regs.h"
#include "PrintSymbol.h"
#include <comdef.h>


// Basic types
const wchar_t * const rgBaseType[] =
{
  L"<NoType>",                         // btNoType = 0,
  L"void",                             // btVoid = 1,
  L"char",                             // btChar = 2,
  L"wchar_t",                          // btWChar = 3,
  L"signed char",
  L"unsigned char",
  L"int",                              // btInt = 6,
  L"unsigned int",                     // btUInt = 7,
  L"float",                            // btFloat = 8,
  L"<BCD>",                            // btBCD = 9,
  L"bool",                             // btBool = 10,
  L"short",
  L"unsigned short",
  L"long",                             // btLong = 13,
  L"unsigned long",                    // btULong = 14,
  L"__int8",
  L"__int16",
  L"__int32",
  L"__int64",
  L"__int128",
  L"unsigned __int8",
  L"unsigned __int16",
  L"unsigned __int32",
  L"unsigned __int64",
  L"unsigned __int128",
  L"<currency>",                       // btCurrency = 25,
  L"<date>",                           // btDate = 26,
  L"VARIANT",                          // btVariant = 27,
  L"<complex>",                        // btComplex = 28,
  L"<bit>",                            // btBit = 29,
  L"BSTR",                             // btBSTR = 30,
  L"HRESULT"                           // btHresult = 31
};

// Tags returned by Dia
const wchar_t * const rgTags[] =
{
  L"(SymTagNull)",                     // SymTagNull
  L"Executable (Global)",              // SymTagExe
  L"Compiland",                        // SymTagCompiland
  L"CompilandDetails",                 // SymTagCompilandDetails
  L"CompilandEnv",                     // SymTagCompilandEnv
  L"Function",                         // SymTagFunction
  L"Block",                            // SymTagBlock
  L"Data",                             // SymTagData
  L"Annotation",                       // SymTagAnnotation
  L"Label",                            // SymTagLabel
  L"PublicSymbol",                     // SymTagPublicSymbol
  L"UserDefinedType",                  // SymTagUDT
  L"Enum",                             // SymTagEnum
  L"FunctionType",                     // SymTagFunctionType
  L"PointerType",                      // SymTagPointerType
  L"ArrayType",                        // SymTagArrayType
  L"BaseType",                         // SymTagBaseType
  L"Typedef",                          // SymTagTypedef
  L"BaseClass",                        // SymTagBaseClass
  L"Friend",                           // SymTagFriend
  L"FunctionArgType",                  // SymTagFunctionArgType
  L"FuncDebugStart",                   // SymTagFuncDebugStart
  L"FuncDebugEnd",                     // SymTagFuncDebugEnd
  L"UsingNamespace",                   // SymTagUsingNamespace
  L"VTableShape",                      // SymTagVTableShape
  L"VTable",                           // SymTagVTable
  L"Custom",                           // SymTagCustom
  L"Thunk",                            // SymTagThunk
  L"CustomType",                       // SymTagCustomType
  L"ManagedType",                      // SymTagManagedType
  L"Dimension",                        // SymTagDimension
  L"CallSite",                         // SymTagCallSite
};


// Processors
const wchar_t * const rgFloatPackageStrings[] =
{
  L"hardware processor (80x87 for Intel processors)",    // CV_CFL_NDP
  L"emulator",                                           // CV_CFL_EMU
  L"altmath",                                            // CV_CFL_ALT
  L"???"
};

const wchar_t * const rgProcessorStrings[] =
{
  L"8080",                             //  CV_CFL_8080
  L"8086",                             //  CV_CFL_8086
  L"80286",                            //  CV_CFL_80286
  L"80386",                            //  CV_CFL_80386
  L"80486",                            //  CV_CFL_80486
  L"Pentium",                          //  CV_CFL_PENTIUM
  L"Pentium Pro/Pentium II",           //  CV_CFL_PENTIUMII/CV_CFL_PENTIUMPRO
  L"Pentium III",                      //  CV_CFL_PENTIUMIII
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"MIPS (Generic)",                   //  CV_CFL_MIPSR4000
  L"MIPS16",                           //  CV_CFL_MIPS16
  L"MIPS32",                           //  CV_CFL_MIPS32
  L"MIPS64",                           //  CV_CFL_MIPS64
  L"MIPS I",                           //  CV_CFL_MIPSI
  L"MIPS II",                          //  CV_CFL_MIPSII
  L"MIPS III",                         //  CV_CFL_MIPSIII
  L"MIPS IV",                          //  CV_CFL_MIPSIV
  L"MIPS V",                           //  CV_CFL_MIPSV
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"M68000",                           //  CV_CFL_M68000
  L"M68010",                           //  CV_CFL_M68010
  L"M68020",                           //  CV_CFL_M68020
  L"M68030",                           //  CV_CFL_M68030
  L"M68040",                           //  CV_CFL_M68040
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"Alpha 21064",                      // CV_CFL_ALPHA, CV_CFL_ALPHA_21064
  L"Alpha 21164",                      // CV_CFL_ALPHA_21164
  L"Alpha 21164A",                     // CV_CFL_ALPHA_21164A
  L"Alpha 21264",                      // CV_CFL_ALPHA_21264
  L"Alpha 21364",                      // CV_CFL_ALPHA_21364
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"PPC 601",                          // CV_CFL_PPC601
  L"PPC 603",                          // CV_CFL_PPC603
  L"PPC 604",                          // CV_CFL_PPC604
  L"PPC 620",                          // CV_CFL_PPC620
  L"PPC w/FP",                         // CV_CFL_PPCFP
  L"PPC (Big Endian)",                 // CV_CFL_PPCBE
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"SH3",                              // CV_CFL_SH3
  L"SH3E",                             // CV_CFL_SH3E
  L"SH3DSP",                           // CV_CFL_SH3DSP
  L"SH4",                              // CV_CFL_SH4
  L"SHmedia",                          // CV_CFL_SHMEDIA
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"ARM3",                             // CV_CFL_ARM3
  L"ARM4",                             // CV_CFL_ARM4
  L"ARM4T",                            // CV_CFL_ARM4T
  L"ARM5",                             // CV_CFL_ARM5
  L"ARM5T",                            // CV_CFL_ARM5T
  L"ARM6",                             // CV_CFL_ARM6
  L"ARM (XMAC)",                       // CV_CFL_ARM_XMAC
  L"ARM (WMMX)",                       // CV_CFL_ARM_WMMX
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"Omni",                             // CV_CFL_OMNI
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"Itanium",                          // CV_CFL_IA64, CV_CFL_IA64_1
  L"Itanium (McKinley)",               // CV_CFL_IA64_2
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"CEE",                              // CV_CFL_CEE
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"AM33",                             // CV_CFL_AM33
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"M32R",                             // CV_CFL_M32R
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"TriCore",                          // CV_CFL_TRICORE
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"x64",                              // CV_CFL_X64
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"EBC",                              // CV_CFL_EBC
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"???",
  L"Thumb",                            // CV_CFL_THUMB
};

const wchar_t * const rgDataKind[] =
{
  L"Unknown",
  L"Local",
  L"Static Local",
  L"Param",
  L"Object Ptr",
  L"File Static",
  L"Global",
  L"Member",
  L"Static Member",
  L"Constant",
};

const wchar_t * const rgUdtKind[] =
{
  L"struct",
  L"class",
  L"union",
  L"enum",
};

const wchar_t * const rgAccess[] =
{
  L"",                     // No access specifier
  L"private",
  L"protected",
  L"public"
};

const wchar_t * const rgCallingConvention[] =
{
  L"CV_CALL_NEAR_C      ",
  L"CV_CALL_FAR_C       ",
  L"CV_CALL_NEAR_PASCAL ",
  L"CV_CALL_FAR_PASCAL  ",
  L"CV_CALL_NEAR_FAST   ",
  L"CV_CALL_FAR_FAST    ",
  L"CV_CALL_SKIPPED     ",
  L"CV_CALL_NEAR_STD    ",
  L"CV_CALL_FAR_STD     ",
  L"CV_CALL_NEAR_SYS    ",
  L"CV_CALL_FAR_SYS     ",
  L"CV_CALL_THISCALL    ",
  L"CV_CALL_MIPSCALL    ",
  L"CV_CALL_GENERIC     ",
  L"CV_CALL_ALPHACALL   ",
  L"CV_CALL_PPCCALL     ",
  L"CV_CALL_SHCALL      ",
  L"CV_CALL_ARMCALL     ",
  L"CV_CALL_AM33CALL    ",
  L"CV_CALL_TRICALL     ",
  L"CV_CALL_SH5CALL     ",
  L"CV_CALL_M32RCALL    ",
  L"CV_CALL_RESERVED    "
};

const wchar_t * const rgLanguage[] =
{
  L"C",                                // CV_CFL_C
  L"C++",                              // CV_CFL_CXX
  L"FORTRAN",                          // CV_CFL_FORTRAN
  L"MASM",                             // CV_CFL_MASM
  L"Pascal",                           // CV_CFL_PASCAL
  L"Basic",                            // CV_CFL_BASIC
  L"COBOL",                            // CV_CFL_COBOL
  L"LINK",                             // CV_CFL_LINK
  L"CVTRES",                           // CV_CFL_CVTRES
  L"CVTPGD",                           // CV_CFL_CVTPGD
  L"C#",                               // CV_CFL_CSHARP
  L"Visual Basic",                     // CV_CFL_VB
  L"ILASM",                            // CV_CFL_ILASM
  L"Java",                             // CV_CFL_JAVA
  L"JScript",                          // CV_CFL_JSCRIPT
  L"MSIL",                             // CV_CFL_MSIL
};

const wchar_t * const rgLocationTypeString[] =
{
  L"NULL",
  L"static",
  L"TLS",
  L"RegRel",
  L"ThisRel",
  L"Enregistered",
  L"BitField",
  L"Slot",
  L"IL Relative",
  L"In MetaData",
  L"Constant"
};


////////////////////////////////////////////////////////////
// Print a public symbol info: name, VA, RVA, SEG:OFF
//
void PrintPublicSymbol(IDiaSymbol *pSymbol)
{
  DWORD dwSymTag;
  DWORD dwRVA;
  DWORD dwSeg;
  DWORD dwOff;
  BSTR bstrName;
  
  if (pSymbol->get_symTag(&dwSymTag) != S_OK) {
    return;
  }

  if (pSymbol->get_relativeVirtualAddress(&dwRVA) != S_OK) {
    dwRVA = 0xFFFFFFFF;
  }

  pSymbol->get_addressSection(&dwSeg);
  pSymbol->get_addressOffset(&dwOff);

  wprintf(L"%s: [%08X][%04X:%08X] ", rgTags[dwSymTag], dwRVA, dwSeg, dwOff);
  
  if (dwSymTag == SymTagThunk) {
    if (pSymbol->get_name(&bstrName) == S_OK) {
      wprintf(L"%s\n", bstrName);

      SysFreeString(bstrName);
    }

    else {
      if (pSymbol->get_targetRelativeVirtualAddress(&dwRVA) != S_OK) {
        dwRVA = 0xFFFFFFFF;
      }

      pSymbol->get_targetSection(&dwSeg);
      pSymbol->get_targetOffset(&dwOff);

      wprintf(L"target -> [%08X][%04X:%08X]\n", dwRVA, dwSeg, dwOff);
    }
  }

  else {
    // must be a function or a data symbol

    BSTR bstrUndname;

    if (pSymbol->get_name(&bstrName) == S_OK) {
      if (pSymbol->get_undecoratedName(&bstrUndname) == S_OK) {
        wprintf(L"%s(%s)\n", bstrName, bstrUndname);

        SysFreeString(bstrUndname);
      }

      else {
        wprintf(L"%s\n", bstrName);
      }

      SysFreeString(bstrName);
    }
  }
}

////////////////////////////////////////////////////////////
// Print a global symbol info: name, VA, RVA, SEG:OFF
//
void PrintGlobalSymbol(IDiaSymbol *pSymbol)
{
  DWORD dwSymTag;
  DWORD dwRVA;
  DWORD dwSeg;
  DWORD dwOff;
  
  if (pSymbol->get_symTag(&dwSymTag) != S_OK) {
    return;
  }

  if (pSymbol->get_relativeVirtualAddress(&dwRVA) != S_OK) {
    dwRVA = 0xFFFFFFFF;
  }

  pSymbol->get_addressSection(&dwSeg);
  pSymbol->get_addressOffset(&dwOff);

  wprintf(L"%s: [%08X][%04X:%08X] ", rgTags[dwSymTag], dwRVA, dwSeg, dwOff);
  
  if (dwSymTag == SymTagThunk) {
    BSTR bstrName;

    if (pSymbol->get_name(&bstrName) == S_OK) {
      wprintf(L"%s\n", bstrName);

      SysFreeString(bstrName);
    }

    else {
      if (pSymbol->get_targetRelativeVirtualAddress(&dwRVA) != S_OK) {
        dwRVA = 0xFFFFFFFF;
      }

      pSymbol->get_targetSection(&dwSeg);
      pSymbol->get_targetOffset(&dwOff);
      wprintf(L"target -> [%08X][%04X:%08X]\n", dwRVA, dwSeg, dwOff);
    }
  }

  else {
    BSTR bstrName;
    BSTR bstrUndname;

    if (pSymbol->get_name(&bstrName) == S_OK) {
      if (pSymbol->get_undecoratedName(&bstrUndname) == S_OK) {
        wprintf(L"%s(%s)\n", bstrName, bstrUndname);

        SysFreeString(bstrUndname);
      }

      else {
        wprintf(L"%s\n", bstrName);
      }

      SysFreeString(bstrName);
    }
  }
}

////////////////////////////////////////////////////////////
// Print a callsite symbol info: SEG:OFF, RVA, type
//
void PrintCallSiteInfo(IDiaSymbol *pSymbol)
{
  DWORD dwISect, dwOffset;
  if (pSymbol->get_addressSection(&dwISect) == S_OK &&
    pSymbol->get_addressOffset(&dwOffset) == S_OK) {
    wprintf(L"[0x%04x:0x%08x]  ", dwISect, dwOffset);
  }

  DWORD rva;
  if (pSymbol->get_relativeVirtualAddress(&rva) == S_OK) {
    wprintf(L"0x%08X  ", rva);
  }

  IDiaSymbol *pFuncType;
  if (pSymbol->get_type(&pFuncType) == S_OK) {
    DWORD tag;
    if (pFuncType->get_symTag(&tag) == S_OK) {
      switch(tag)
      {
        case SymTagFunctionType:
          PrintFunctionType(pSymbol);
          break;
        case SymTagPointerType:
          PrintFunctionType(pFuncType);
          break;
        default:
          wprintf(L"???\n");
          break;
      }
    }
    pFuncType->Release();
  }
}

////////////////////////////////////////////////////////////
// Print a symbol info: name, type etc.
//
void PrintSymbol(IDiaSymbol *pSymbol, DWORD dwIndent)
{
  IDiaSymbol *pType;
  DWORD dwSymTag;
  ULONGLONG ulLen;
  
  if (pSymbol->get_symTag(&dwSymTag) != S_OK) {
    wprintf(L"ERROR - PrintSymbol get_symTag() failed\n");
    return;
  }

  if (dwSymTag == SymTagFunction) {
    putwchar(L'\n');
  }

  PrintSymTag(dwSymTag);

  for (DWORD i = 0; i < dwIndent; i++) {
    putwchar(L' ');
  }

  switch (dwSymTag) {
    case SymTagCompilandDetails:
      PrintCompilandDetails(pSymbol);
      break;

    case SymTagCompilandEnv:
      PrintCompilandEnv(pSymbol);
      break;

    case SymTagData:
      PrintData(pSymbol, dwIndent + 2);
      break;

    case SymTagFunction:
    case SymTagBlock:
      PrintLocation(pSymbol);

      if (pSymbol->get_length(&ulLen) == S_OK) {
        wprintf(L", len = %08X, ", ulLen);
      }

      if (dwSymTag == SymTagFunction) {
        DWORD dwCall;

        if (pSymbol->get_callingConvention(&dwCall) == S_OK) {
          wprintf(L", %s", SafeDRef(rgCallingConvention, dwCall));
        }
      }

      PrintUndName(pSymbol);
      putwchar(L'\n');

      if (dwSymTag == SymTagFunction)
      {
        BOOL f;

        for (DWORD i = 0; i < dwIndent; i++) {
          putwchar(L' ');
        }
        wprintf(L"                 Function attribute:");

        if ((pSymbol->get_isCxxReturnUdt(&f) == S_OK) && f) {
          wprintf(L" return user defined type (C++ style)");
        }
        if ((pSymbol->get_constructor(&f) == S_OK) && f) {
          wprintf(L" instance constructor");
        }
        if ((pSymbol->get_isConstructorVirtualBase(&f) == S_OK) && f) {
          wprintf(L" instance constructor of a class with virtual base");
        }
        putwchar (L'\n');

        for (DWORD i = 0; i < dwIndent; i++) {
          putwchar(L' ');
        }
        wprintf(L"                 Function info:");

        if ((pSymbol->get_hasAlloca(&f) == S_OK) && f) {
          wprintf(L" alloca");
        }

        if ((pSymbol->get_hasSetJump(&f) == S_OK) && f) {
          wprintf(L" setjmp");
        }

        if ((pSymbol->get_hasLongJump(&f) == S_OK) && f) {
          wprintf(L" longjmp");
        }

        if ((pSymbol->get_hasInlAsm(&f) == S_OK) && f) {
          wprintf(L" inlasm");
        }

        if ((pSymbol->get_hasEH(&f) == S_OK) && f) {
          wprintf(L" eh");
        }

        if ((pSymbol->get_inlSpec(&f) == S_OK) && f) {
          wprintf(L" inl_specified");
        }

        if ((pSymbol->get_hasSEH(&f) == S_OK) && f) {
          wprintf(L" seh");
        }

        if ((pSymbol->get_isNaked(&f) == S_OK) && f) {
          wprintf(L" naked");
        }

        if ((pSymbol->get_hasSecurityChecks(&f) == S_OK) && f) {
          wprintf(L" gschecks");
        }

        if ((pSymbol->get_isSafeBuffers(&f) == S_OK) && f) {
          wprintf(L" safebuffers");
        }

        if ((pSymbol->get_hasEHa(&f) == S_OK) && f) {
          wprintf(L" asyncheh");
        }

        if ((pSymbol->get_noStackOrdering(&f) == S_OK) && f) {
          wprintf(L" gsnostackordering");
        }

        if ((pSymbol->get_wasInlined(&f) == S_OK) && f) {
          wprintf(L" wasinlined");
        }

        if ((pSymbol->get_strictGSCheck(&f) == S_OK) && f) {
          wprintf(L" strict_gs_check");
        }

        putwchar(L'\n');
      }

      IDiaEnumSymbols *pEnumChildren;

      if (SUCCEEDED(pSymbol->findChildren(SymTagNull, NULL, nsNone, &pEnumChildren))) {
        IDiaSymbol *pChild;
        ULONG celt = 0;

        while (SUCCEEDED(pEnumChildren->Next(1, &pChild, &celt)) && (celt == 1)) {
          PrintSymbol(pChild, dwIndent + 2);
          pChild->Release();
        }

        pEnumChildren->Release();
      }
      return;

    case SymTagAnnotation:
      PrintLocation(pSymbol);
      putwchar(L'\n');
      break;

    case SymTagLabel:
      PrintLocation(pSymbol);
      wprintf(L", ");
      PrintName(pSymbol);
      break;

    case SymTagEnum:
    case SymTagTypedef:
    case SymTagUDT:
    case SymTagBaseClass:
      PrintUDT(pSymbol);
      break;

    case SymTagFuncDebugStart:
    case SymTagFuncDebugEnd:
      PrintLocation(pSymbol);
      break;

    case SymTagFunctionArgType:
    case SymTagFunctionType:
    case SymTagPointerType:
    case SymTagArrayType:
    case SymTagBaseType:
      if (pSymbol->get_type(&pType) == S_OK) {
        PrintType(pType);
        pType->Release();
      }

      putwchar(L'\n');
      break;

    case SymTagThunk:
      PrintThunk(pSymbol);
      break;

    case SymTagCallSite:
      PrintCallSiteInfo(pSymbol);
      break;
        
    default:
      PrintName(pSymbol);

      IDiaSymbol *pType;

      if (pSymbol->get_type(&pType) == S_OK) {
        wprintf(L" has type ");
        PrintType(pType);
        pType->Release();
      }
  }

  if ((dwSymTag == SymTagUDT) || (dwSymTag == SymTagAnnotation)) {
    IDiaEnumSymbols *pEnumChildren;

    putwchar(L'\n');

    if (SUCCEEDED(pSymbol->findChildren(SymTagNull, NULL, nsNone, &pEnumChildren))) {
      IDiaSymbol *pChild;
      ULONG celt = 0;

      while (SUCCEEDED(pEnumChildren->Next(1, &pChild, &celt)) && (celt == 1)) {
        PrintSymbol(pChild, dwIndent + 2);
        pChild->Release();
      }

      pEnumChildren->Release();
    }
  }
  putwchar(L'\n');
}

////////////////////////////////////////////////////////////
// Print the string coresponding to the symbol's tag property
//
void PrintSymTag(DWORD dwSymTag)
{
  wprintf(L"%-15s: ", SafeDRef(rgTags, dwSymTag));
}

////////////////////////////////////////////////////////////
// Print the name of the symbol
//
void PrintName(IDiaSymbol *pSymbol)
{
  BSTR bstrName;
  BSTR bstrUndName;

  if (pSymbol->get_name(&bstrName) != S_OK) {
    wprintf(L"(none)");
    return;
  }

  if (pSymbol->get_undecoratedName(&bstrUndName) == S_OK) {
    if (wcscmp(bstrName, bstrUndName) == 0) {
      wprintf(L"%s", bstrName);
    }

    else {
      wprintf(L"%s(%s)", bstrUndName, bstrName);
    }

    SysFreeString(bstrUndName);
  }

  else {
    wprintf(L"%s", bstrName);
  }

  SysFreeString(bstrName);
}

void GetSymbolName(std::wstring& symbolName, IDiaSymbol *pSymbol)
{
    BSTR bstrName;
    BSTR bstrUndName;

    if (pSymbol->get_name(&bstrName) != S_OK) {
    symbolName.clear();
    return;
    }

    if (pSymbol->get_undecoratedName(&bstrUndName) == S_OK) {
    if (wcscmp(bstrName, bstrUndName) == 0) 
    {
        symbolName= _bstr_t(bstrName);
    }
    else
    {
        symbolName= _bstr_t(bstrUndName);
    }

        SysFreeString(bstrUndName);
    }

    else 
    {
        symbolName= _bstr_t(bstrName);
    }

    SysFreeString(bstrName);
}

////////////////////////////////////////////////////////////
// Print the undecorated name of the symbol
//  - only SymTagFunction, SymTagData and SymTagPublicSymbol
//    can have this property set
//
void PrintUndName(IDiaSymbol *pSymbol)
{
  BSTR bstrName;

  if (pSymbol->get_undecoratedName(&bstrName) != S_OK) {
    if (pSymbol->get_name(&bstrName) == S_OK) {
      // Print the name of the symbol instead

      wprintf(L"%s", (bstrName[0] != L'\0') ? bstrName : L"(none)");

      SysFreeString(bstrName);
    }

    else {
      wprintf(L"(none)");
    }

    return;
  }

  if (bstrName[0] != L'\0') {
    wprintf(L"%s", bstrName);
  }

  SysFreeString(bstrName);
}

////////////////////////////////////////////////////////////
// Print a SymTagThunk symbol's info
//
void PrintThunk(IDiaSymbol *pSymbol)
{
  DWORD dwRVA;
  DWORD dwISect;
  DWORD dwOffset;

  if ((pSymbol->get_relativeVirtualAddress(&dwRVA) == S_OK) &&
      (pSymbol->get_addressSection(&dwISect) == S_OK) &&
      (pSymbol->get_addressOffset(&dwOffset) == S_OK)) {
    wprintf(L"[%08X][%04X:%08X]", dwRVA, dwISect, dwOffset);
  }

  if ((pSymbol->get_targetSection(&dwISect) == S_OK) &&
      (pSymbol->get_targetOffset(&dwOffset) == S_OK) &&
      (pSymbol->get_targetRelativeVirtualAddress(&dwRVA) == S_OK)) {
    wprintf(L", target [%08X][%04X:%08X] ", dwRVA, dwISect, dwOffset);
  }

  else {
    wprintf(L", target ");

    PrintName(pSymbol);
  }
}

////////////////////////////////////////////////////////////
// Print the compiland/module details: language, platform...
//
void PrintCompilandDetails(IDiaSymbol *pSymbol)
{
  DWORD dwLanguage;

  if (pSymbol->get_language(&dwLanguage) == S_OK) {
    wprintf(L"\n\tLanguage: %s\n", SafeDRef(rgLanguage, dwLanguage));
  }

  DWORD dwPlatform;

  if (pSymbol->get_platform(&dwPlatform) == S_OK) {
    wprintf(L"\tTarget processor: %s\n", SafeDRef(rgProcessorStrings, dwPlatform));
  }

  BOOL fEC;

  if (pSymbol->get_editAndContinueEnabled(&fEC) == S_OK) {
    if (fEC) {
      wprintf(L"\tCompiled for edit and continue: yes\n");
    }

    else {
      wprintf(L"\tCompiled for edit and continue: no\n");
    }
  }

  BOOL fDbgInfo;

  if (pSymbol->get_hasDebugInfo(&fDbgInfo) == S_OK) {
    if (fDbgInfo) {
      wprintf(L"\tCompiled without debugging info: no\n");
    }

    else {
      wprintf(L"\tCompiled without debugging info: yes\n");
    }
  }

  BOOL fLTCG;

  if (pSymbol->get_isLTCG(&fLTCG) == S_OK) {
    if (fLTCG) {
      wprintf(L"\tCompiled with LTCG: yes\n");
    }

    else {
      wprintf(L"\tCompiled with LTCG: no\n");
    }
  }

  BOOL fDataAlign;

  if (pSymbol->get_isDataAligned(&fDataAlign) == S_OK) {
    if (fDataAlign) {
      wprintf(L"\tCompiled with /bzalign: no\n");
    }

    else {
      wprintf(L"\tCompiled with /bzalign: yes\n");
    }
  }

  BOOL fManagedPresent;

  if (pSymbol->get_hasManagedCode(&fManagedPresent) == S_OK) {
    if (fManagedPresent) {
      wprintf(L"\tManaged code present: yes\n");
    }

    else {
      wprintf(L"\tManaged code present: no\n");
    }
  }

  BOOL fSecurityChecks;

  if (pSymbol->get_hasSecurityChecks(&fSecurityChecks) == S_OK) {
    if (fSecurityChecks) {
      wprintf(L"\tCompiled with /GS: yes\n");
    }

    else {
      wprintf(L"\tCompiled with /GS: no\n");
    }
  }

  BOOL fHotPatch;

  if (pSymbol->get_isHotpatchable(&fHotPatch) == S_OK) {
    if (fHotPatch) {
      wprintf(L"\tCompiled with /hotpatch: yes\n");
    }

    else {
      wprintf(L"\tCompiled with /hotpatch: no\n");
    }
  }

  BOOL fCVTCIL;

  if (pSymbol->get_isCVTCIL(&fCVTCIL) == S_OK) {
    if (fCVTCIL) {
      wprintf(L"\tConverted by CVTCIL: yes\n");
    }

    else {
      wprintf(L"\tConverted by CVTCIL: no\n");
    }
  }

  BOOL fMSILModule;

  if (pSymbol->get_isMSILNetmodule(&fMSILModule) == S_OK) {
    if (fMSILModule) {
      wprintf(L"\tMSIL module: yes\n");
    }

    else {
      wprintf(L"\tMSIL module: no\n");
    }
  }

  DWORD dwVerMajor;
  DWORD dwVerMinor;
  DWORD dwVerBuild;
  DWORD dwVerQFE;

  if ((pSymbol->get_frontEndMajor(&dwVerMajor) == S_OK) &&
      (pSymbol->get_frontEndMinor(&dwVerMinor) == S_OK) &&
      (pSymbol->get_frontEndBuild(&dwVerBuild) == S_OK)) {
    wprintf(L"\tFrontend Version: Major = %u, Minor = %u, Build = %u",
            dwVerMajor,
            dwVerMinor,
            dwVerBuild);

    if (pSymbol->get_frontEndQFE(&dwVerQFE) == S_OK) {
      wprintf(L", QFE = %u", dwVerQFE);
    }

    putwchar(L'\n');
  }

  if ((pSymbol->get_backEndMajor(&dwVerMajor) == S_OK) &&
      (pSymbol->get_backEndMinor(&dwVerMinor) == S_OK) &&
      (pSymbol->get_backEndBuild(&dwVerBuild) == S_OK)) {
    wprintf(L"\tBackend Version: Major = %u, Minor = %u, Build = %u",
            dwVerMajor,
            dwVerMinor,
            dwVerBuild);

    if (pSymbol->get_backEndQFE(&dwVerQFE) == S_OK) {
      wprintf(L", QFE = %u", dwVerQFE);
    }

    putwchar(L'\n');
  }

  BSTR bstrCompilerName;

  if (pSymbol->get_compilerName(&bstrCompilerName) == S_OK) {
    if (bstrCompilerName != NULL) {
      wprintf(L"\tVersion string: %s", bstrCompilerName);

      SysFreeString(bstrCompilerName);
    }
  }

  putwchar(L'\n');
}

////////////////////////////////////////////////////////////
// Print the compilan/module env
//
void PrintCompilandEnv(IDiaSymbol *pSymbol)
{
  PrintName(pSymbol);
  wprintf(L" =");

  VARIANT vt = { VT_EMPTY };

  if (pSymbol->get_value(&vt) == S_OK) {
    PrintVariant(vt);
    VariantClear((VARIANTARG *) &vt);
  }
}

////////////////////////////////////////////////////////////
// Print a string corespondig to a location type
//
void PrintLocation(IDiaSymbol *pSymbol)
{
  DWORD dwLocType;
  DWORD dwRVA, dwSect, dwOff, dwReg, dwBitPos, dwSlot;
  LONG lOffset;
  ULONGLONG ulLen;
  VARIANT vt = { VT_EMPTY };

  if (pSymbol->get_locationType(&dwLocType) != S_OK) {
    // It must be a symbol in optimized code

    wprintf(L"symbol in optmized code");
    return;
  }

  switch (dwLocType) {
    case LocIsStatic:
      if ((pSymbol->get_relativeVirtualAddress(&dwRVA) == S_OK) &&
          (pSymbol->get_addressSection(&dwSect) == S_OK) &&
          (pSymbol->get_addressOffset(&dwOff) == S_OK)) {
        wprintf(L"%s, [%08X][%04X:%08X]", SafeDRef(rgLocationTypeString, dwLocType), dwRVA, dwSect, dwOff);
      }
      break;

    case LocIsTLS:
    case LocInMetaData:
    case LocIsIlRel:
      if ((pSymbol->get_relativeVirtualAddress(&dwRVA) == S_OK) &&
          (pSymbol->get_addressSection(&dwSect) == S_OK) &&
          (pSymbol->get_addressOffset(&dwOff) == S_OK)) {
        wprintf(L"%s, [%08X][%04X:%08X]", SafeDRef(rgLocationTypeString, dwLocType), dwRVA, dwSect, dwOff);
      }
      break;

    case LocIsRegRel:
      if ((pSymbol->get_registerId(&dwReg) == S_OK) &&
          (pSymbol->get_offset(&lOffset) == S_OK)) {
        wprintf(L"%s Relative, [%08X]", SzNameC7Reg((USHORT) dwReg), lOffset);
      }
      break;

    case LocIsThisRel:
      if (pSymbol->get_offset(&lOffset) == S_OK) {
        wprintf(L"this+0x%X", lOffset);
      }
      break;

    case LocIsBitField:
      if ((pSymbol->get_offset(&lOffset) == S_OK) &&
          (pSymbol->get_bitPosition(&dwBitPos) == S_OK) &&
          (pSymbol->get_length(&ulLen) == S_OK)) {
        wprintf(L"this(bf)+0x%X:0x%X len(0x%X)", lOffset, dwBitPos, ulLen);
      }
      break;

    case LocIsEnregistered:
      if (pSymbol->get_registerId(&dwReg) == S_OK) {
        wprintf(L"enregistered %s", SzNameC7Reg((USHORT) dwReg));
      }
      break;

    case LocIsSlot:
      if (pSymbol->get_slot(&dwSlot) == S_OK) {
        wprintf(L"%s, [%08X]", SafeDRef(rgLocationTypeString, dwLocType), dwSlot);
      }
      break;

    case LocIsConstant:
      wprintf(L"constant");

      if (pSymbol->get_value(&vt) == S_OK) {
        PrintVariant(vt);
        VariantClear((VARIANTARG *) &vt);
      }
      break;

    case LocIsNull:
      break;

    default :
      wprintf(L"Error - invalid location type: 0x%X", dwLocType);
      break;
    }
}

////////////////////////////////////////////////////////////
// Print the type, value and the name of a const symbol
//
void PrintConst(IDiaSymbol *pSymbol)
{
  PrintSymbolType(pSymbol);

  VARIANT vt = { VT_EMPTY };

  if (pSymbol->get_value(&vt) == S_OK) {
    PrintVariant(vt);
    VariantClear((VARIANTARG *) &vt);
  }

  PrintName(pSymbol);
}

////////////////////////////////////////////////////////////
// Print the name and the type of an user defined type
//
void PrintUDT(IDiaSymbol *pSymbol)
{
  PrintName(pSymbol);
  PrintSymbolType(pSymbol);
}

////////////////////////////////////////////////////////////
// Print a string representing the type of a symbol
//
void PrintSymbolType(IDiaSymbol *pSymbol)
{
  IDiaSymbol *pType;

  if (pSymbol->get_type(&pType) == S_OK) {
    wprintf(L", Type: ");
    PrintType(pType);
    pType->Release();
  }
}

////////////////////////////////////////////////////////////
// Print the information details for a type symbol
//
void PrintType(IDiaSymbol *pSymbol)
{
  IDiaSymbol *pBaseType;
  IDiaEnumSymbols *pEnumSym;
  IDiaSymbol *pSym;
  DWORD dwTag;
  BSTR bstrName;
  DWORD dwInfo;
  BOOL bSet;
  DWORD dwRank;
  LONG lCount = 0;
  ULONG celt = 1;

  if (pSymbol->get_symTag(&dwTag) != S_OK) {
    wprintf(L"ERROR - can't retrieve the symbol's SymTag\n");
    return;
  }

  if (pSymbol->get_name(&bstrName) != S_OK) {
    bstrName = NULL;
  }

  if (dwTag != SymTagPointerType) {
    if ((pSymbol->get_constType(&bSet) == S_OK) && bSet) {
      wprintf(L"const ");
    }

    if ((pSymbol->get_volatileType(&bSet) == S_OK) && bSet) {
       wprintf(L"volatile ");
    }

    if ((pSymbol->get_unalignedType(&bSet) == S_OK) && bSet) {
      wprintf(L"__unaligned ");
    }
  }

  ULONGLONG ulLen;

  pSymbol->get_length(&ulLen);

  switch (dwTag) {
    case SymTagUDT:
      PrintUdtKind(pSymbol);
      PrintName(pSymbol);
      break;

    case SymTagEnum:
      wprintf(L"enum ");
      PrintName(pSymbol);
      break;

    case SymTagFunctionType:
      wprintf(L"function ");
      break;

    case SymTagPointerType:
      if (pSymbol->get_type(&pBaseType) != S_OK) {
        wprintf(L"ERROR - SymTagPointerType get_type");
        if (bstrName != NULL) {
          SysFreeString(bstrName);
        }
        return;
      }

      PrintType(pBaseType);
      pBaseType->Release();

      if ((pSymbol->get_reference(&bSet) == S_OK) && bSet) {
        wprintf(L" &");
      }

      else {
        wprintf(L" *");
      }

      if ((pSymbol->get_constType(&bSet) == S_OK) && bSet) {
        wprintf(L" const");
      }

      if ((pSymbol->get_volatileType(&bSet) == S_OK) && bSet) {
        wprintf(L" volatile");
      }

      if ((pSymbol->get_unalignedType(&bSet) == S_OK) && bSet) {
        wprintf(L" __unaligned");
      }
      break;

    case SymTagArrayType:
      if (pSymbol->get_type(&pBaseType) == S_OK) {
        PrintType(pBaseType);

        if (pSymbol->get_rank(&dwRank) == S_OK) {
          if (SUCCEEDED(pSymbol->findChildren(SymTagDimension, NULL, nsNone, &pEnumSym)) && (pEnumSym != NULL)) {
            while (SUCCEEDED(pEnumSym->Next(1, &pSym, &celt)) && (celt == 1)) {
              IDiaSymbol *pBound;

              wprintf(L"[");

              if (pSym->get_lowerBound(&pBound) == S_OK) {
                PrintBound(pBound);

                wprintf(L"..");

                pBound->Release();
              }

              pBound = NULL;

              if (pSym->get_upperBound(&pBound) == S_OK) {
                PrintBound(pBound);

                pBound->Release();
              }

              pSym->Release();
              pSym = NULL;

              wprintf(L"]");
            }

            pEnumSym->Release();
          }
        }

        else if (SUCCEEDED(pSymbol->findChildren(SymTagCustomType, NULL, nsNone, &pEnumSym)) &&
                 (pEnumSym != NULL) &&
                 (pEnumSym->get_Count(&lCount) == S_OK) &&
                 (lCount > 0)) {
          while (SUCCEEDED(pEnumSym->Next(1, &pSym, &celt)) && (celt == 1)) {
            wprintf(L"[");
            PrintType(pSym);
            wprintf(L"]");

            pSym->Release();
          }

          pEnumSym->Release();
        }

        else {
          DWORD dwCountElems;
          ULONGLONG ulLenArray;
          ULONGLONG ulLenElem;

          if (pSymbol->get_count(&dwCountElems) == S_OK) {
            wprintf(L"[0x%X]", dwCountElems);
          }

          else if ((pSymbol->get_length(&ulLenArray) == S_OK) &&
                   (pBaseType->get_length(&ulLenElem) == S_OK)) {
            if (ulLenElem == 0) {
              wprintf(L"[0x%lX]", ulLenArray);
            }

            else {
              wprintf(L"[0x%lX]", ulLenArray/ulLenElem);
            }
          }
        }

        pBaseType->Release();
      }

      else {
        wprintf(L"ERROR - SymTagArrayType get_type\n");
        if (bstrName != NULL) {
          SysFreeString(bstrName);
        }
        return;
      }
      break;

    case SymTagBaseType:
      if (pSymbol->get_baseType(&dwInfo) != S_OK) {
        wprintf(L"SymTagBaseType get_baseType\n");
        if (bstrName != NULL) {
          SysFreeString(bstrName);
        }
        return;
      }

      switch (dwInfo) {
        case btUInt :
          wprintf(L"unsigned ");

        // Fall through

        case btInt :
          switch (ulLen) {
            case 1:
              if (dwInfo == btInt) {
                wprintf(L"signed ");
              }

              wprintf(L"char");
              break;

            case 2:
              wprintf(L"short");
              break;

            case 4:
              wprintf(L"int");
              break;

            case 8:
              wprintf(L"__int64");
              break;
          }

          dwInfo = 0xFFFFFFFF;
          break;

        case btFloat :
          switch (ulLen) {
            case 4:
              wprintf(L"float");
              break;

            case 8:
              wprintf(L"double");
              break;
          }

          dwInfo = 0xFFFFFFFF;
          break;
      }

      if (dwInfo == 0xFFFFFFFF) {
         break;
      }

      wprintf(L"%s", rgBaseType[dwInfo]);
      break;

    case SymTagTypedef:
      PrintName(pSymbol);
      break;

    case SymTagCustomType:
      {
        DWORD idOEM, idOEMSym;
        DWORD cbData = 0;
        DWORD count;

        if (pSymbol->get_oemId(&idOEM) == S_OK) {
          wprintf(L"OEMId = %X, ", idOEM);
        }

        if (pSymbol->get_oemSymbolId(&idOEMSym) == S_OK) {
          wprintf(L"SymbolId = %X, ", idOEMSym);
        }

        if (pSymbol->get_types(0, &count, NULL) == S_OK) {
          IDiaSymbol** rgpDiaSymbols = (IDiaSymbol**) _alloca(sizeof(IDiaSymbol *) * count);

          if (pSymbol->get_types(count, &count, rgpDiaSymbols) == S_OK) {
            for (ULONG i = 0; i < count; i++) {
              PrintType(rgpDiaSymbols[i]);
              rgpDiaSymbols[i]->Release();
            }
          }
        }

        // print custom data

        if ((pSymbol->get_dataBytes(cbData, &cbData, NULL) == S_OK) && (cbData != 0)) {
          wprintf(L", Data: ");

          BYTE *pbData = new BYTE[cbData];

          pSymbol->get_dataBytes(cbData, &cbData, pbData);

          for (ULONG i = 0; i < cbData; i++) {
            wprintf(L"0x%02X ", pbData[i]);
          }

          delete [] pbData;
        }
      }
      break;

    case SymTagData: // This really is member data, just print its location
      PrintLocation(pSymbol);
      break;
  }

  if (bstrName != NULL) {
    SysFreeString(bstrName);
  }
}

////////////////////////////////////////////////////////////
// Print bound information
//
void PrintBound(IDiaSymbol *pSymbol)
{
  DWORD dwTag = 0;
  DWORD dwKind;

  if (pSymbol->get_symTag(&dwTag) != S_OK) {
    wprintf(L"ERROR - PrintBound() get_symTag");
    return;
  }

  if (pSymbol->get_locationType(&dwKind) != S_OK) {
    wprintf(L"ERROR - PrintBound() get_locationType");
    return;
  }

  if (dwTag == SymTagData && dwKind == LocIsConstant) {
    VARIANT v;

    if (pSymbol->get_value(&v) == S_OK) {
      PrintVariant(v);
      VariantClear((VARIANTARG *) &v);
    }
  }

  else {
    PrintName(pSymbol);
  }
}

////////////////////////////////////////////////////////////
//
void PrintData(IDiaSymbol *pSymbol, DWORD dwIndent)
{
  PrintLocation(pSymbol);

  DWORD dwDataKind;
  if (pSymbol->get_dataKind(&dwDataKind) != S_OK) {
    wprintf(L"ERROR - PrintData() get_dataKind");
    return;
  }

  wprintf(L", %s", SafeDRef(rgDataKind, dwDataKind));
  PrintSymbolType(pSymbol);

  wprintf(L", ");
  PrintName(pSymbol);
}

////////////////////////////////////////////////////////////
// Print a VARIANT
//
void PrintVariant(VARIANT var)
{
  switch (var.vt) {
    case VT_UI1:
    case VT_I1:
      wprintf(L" 0x%X", var.bVal);
      break;

    case VT_I2:
    case VT_UI2:
    case VT_BOOL:
      wprintf(L" 0x%X", var.iVal);
      break;

    case VT_I4:
    case VT_UI4:
    case VT_INT:
    case VT_UINT:
    case VT_ERROR:
      wprintf(L" 0x%X", var.lVal);
      break;

    case VT_R4:
      wprintf(L" %g", var.fltVal);
      break;

    case VT_R8:
      wprintf(L" %g", var.dblVal);
      break;

    case VT_BSTR:
      wprintf(L" \"%s\"", var.bstrVal);
      break;

    default:
      wprintf(L" ??");
    }
}

////////////////////////////////////////////////////////////
// Print a string corresponding to a UDT kind
//
void PrintUdtKind(IDiaSymbol *pSymbol)
{
  DWORD dwKind = 0;

  if (pSymbol->get_udtKind(&dwKind) == S_OK) {
    wprintf(L"%s ", rgUdtKind[dwKind]);
  }
}

////////////////////////////////////////////////////////////
// Print type informations is details
//
void PrintTypeInDetail(IDiaSymbol *pSymbol, DWORD dwIndent)
{
  IDiaEnumSymbols *pEnumChildren;
  IDiaSymbol *pType;
  IDiaSymbol *pChild;
  DWORD dwSymTag;
  DWORD dwSymTagType;
  ULONG celt = 0;
  BOOL bFlag;

  if (dwIndent > MAX_TYPE_IN_DETAIL) {
    return;
  }

  if (pSymbol->get_symTag(&dwSymTag) != S_OK) {
    wprintf(L"ERROR - PrintTypeInDetail() get_symTag\n");
    return;
  }

  PrintSymTag(dwSymTag);

  for (DWORD i = 0;i < dwIndent; i++) {
    putwchar(L' ');
  }

  switch (dwSymTag) {
    case SymTagData:
      PrintData(pSymbol, dwIndent);

      if (pSymbol->get_type(&pType) == S_OK) {
        if (pType->get_symTag(&dwSymTagType) == S_OK) {
          if (dwSymTagType == SymTagUDT) {
            putwchar(L'\n');
            PrintTypeInDetail(pType, dwIndent + 2);
          }
        }
        pType->Release();
      }
      break;

    case SymTagTypedef:
    case SymTagVTable:
      PrintSymbolType(pSymbol);
      break;

    case SymTagEnum:
    case SymTagUDT:
      PrintUDT(pSymbol);
      putwchar(L'\n');

      if (SUCCEEDED(pSymbol->findChildren(SymTagNull, NULL, nsNone, &pEnumChildren))) {
        while (SUCCEEDED(pEnumChildren->Next(1, &pChild, &celt)) && (celt == 1)) {
          PrintTypeInDetail(pChild, dwIndent + 2);

          pChild->Release();
        }

        pEnumChildren->Release();
      }
      return;
      break;

    case SymTagFunction:
      PrintFunctionType(pSymbol);
      return;
      break;

    case SymTagPointerType:
      PrintName(pSymbol);
      wprintf(L" has type ");
      PrintType(pSymbol);
      break;

    case SymTagArrayType:
    case SymTagBaseType:
    case SymTagFunctionArgType:
    case SymTagUsingNamespace:
    case SymTagCustom:
    case SymTagFriend:
      PrintName(pSymbol);
      PrintSymbolType(pSymbol);
      break;

    case SymTagVTableShape:
    case SymTagBaseClass:
      PrintName(pSymbol);

      if ((pSymbol->get_virtualBaseClass(&bFlag) == S_OK) && bFlag) {
        IDiaSymbol *pVBTableType;
        LONG ptrOffset;
        DWORD dispIndex;

        if ((pSymbol->get_virtualBaseDispIndex(&dispIndex) == S_OK) &&
            (pSymbol->get_virtualBasePointerOffset(&ptrOffset) == S_OK)) {
          wprintf(L" virtual, offset = 0x%X, pointer offset = %ld, virtual base pointer type = ", dispIndex, ptrOffset);

          if (pSymbol->get_virtualBaseTableType(&pVBTableType) == S_OK) {
            PrintType(pVBTableType);
            pVBTableType->Release();
          }

          else {
            wprintf(L"(unknown)");
          }
        }
      }

      else {
        LONG offset;

        if (pSymbol->get_offset(&offset) == S_OK) {
          wprintf(L", offset = 0x%X", offset);
        }
      }

      putwchar(L'\n');

      if (SUCCEEDED(pSymbol->findChildren(SymTagNull, NULL, nsNone, &pEnumChildren))) {
        while (SUCCEEDED(pEnumChildren->Next(1, &pChild, &celt)) && (celt == 1)) {
          PrintTypeInDetail(pChild, dwIndent + 2);
          pChild->Release();
        }

        pEnumChildren->Release();
      }
      break;

    case SymTagFunctionType:
      if (pSymbol->get_type(&pType) == S_OK) {
        PrintType(pType);
      }
      break;

    case SymTagThunk:
      // Happens for functions which only have S_PROCREF
      PrintThunk(pSymbol);
      break;

    default:
      wprintf(L"ERROR - PrintTypeInDetail() invalid SymTag\n");
    }

    putwchar(L'\n');
}

////////////////////////////////////////////////////////////
// Print a function type
//
void PrintFunctionType(IDiaSymbol *pSymbol)
{
  DWORD dwAccess = 0;

  if (pSymbol->get_access(&dwAccess) == S_OK) {
    wprintf(L"%s ", SafeDRef(rgAccess, dwAccess));
  }

  BOOL bIsStatic = FALSE;

  if ((pSymbol->get_isStatic(&bIsStatic) == S_OK) && bIsStatic) {
    wprintf(L"static ");
  }

  IDiaSymbol *pFuncType;

  if (pSymbol->get_type(&pFuncType) == S_OK) {
    IDiaSymbol *pReturnType;

    if (pFuncType->get_type(&pReturnType) == S_OK) {
      PrintType(pReturnType);
      putwchar(L' ');

      BSTR bstrName;

      if (pSymbol->get_name(&bstrName) == S_OK) {
        wprintf(L"%s", bstrName);

        SysFreeString(bstrName);
      }

      IDiaEnumSymbols *pEnumChildren;

      if (SUCCEEDED(pFuncType->findChildren(SymTagNull, NULL, nsNone, &pEnumChildren))) {
        IDiaSymbol *pChild;
        ULONG celt = 0;
        ULONG nParam = 0;

        wprintf(L"(");

        while (SUCCEEDED(pEnumChildren->Next(1, &pChild, &celt)) && (celt == 1)) {
          IDiaSymbol *pType;

          if (pChild->get_type(&pType) == S_OK) {
            if (nParam++) {
              wprintf(L", ");
            }

            PrintType(pType);
            pType->Release();
          }

          pChild->Release();
        }

        pEnumChildren->Release();

        wprintf(L")\n");
      }

      pReturnType->Release();
    }

    pFuncType->Release();
  }
}

////////////////////////////////////////////////////////////
//
void PrintSourceFile(IDiaSourceFile *pSource)
{
  BSTR bstrSourceName;

  if (pSource->get_fileName(&bstrSourceName) == S_OK) {
    wprintf(L"\t%s", bstrSourceName);

    SysFreeString(bstrSourceName);
  }

  else {
    wprintf(L"ERROR - PrintSourceFile() get_fileName");
    return;
  }

  BYTE checksum[256];
  DWORD cbChecksum = sizeof(checksum);

  if (pSource->get_checksum(cbChecksum, &cbChecksum, checksum) == S_OK) {
    wprintf(L" (");

    DWORD checksumType;

    if (pSource->get_checksumType(&checksumType) == S_OK) {
      switch (checksumType) {
        case CHKSUM_TYPE_NONE :
          wprintf(L"None");
          break;

        case CHKSUM_TYPE_MD5 :
          wprintf(L"MD5");
          break;

        case CHKSUM_TYPE_SHA1 :
          wprintf(L"SHA1");
          break;

        default :
          wprintf(L"0x%X", checksumType);
          break;
      }

      if (cbChecksum != 0) {
        wprintf(L": ");
      }
    }

    for (DWORD ib = 0; ib < cbChecksum; ib++) {
      wprintf(L"%02X", checksum[ib]);
    }

    wprintf(L")");
  }
}

////////////////////////////////////////////////////////////
//
void PrintLines(IDiaSession *pSession, IDiaSymbol *pFunction)
{
  DWORD dwSymTag;

  if ((pFunction->get_symTag(&dwSymTag) != S_OK) || (dwSymTag != SymTagFunction)) {
    wprintf(L"ERROR - PrintLines() dwSymTag != SymTagFunction");
    return;
  }

  BSTR bstrName;

  if (pFunction->get_name(&bstrName) == S_OK) {
    wprintf(L"\n** %s\n\n", bstrName);

    SysFreeString(bstrName);
  }

  ULONGLONG ulLength;

  if (pFunction->get_length(&ulLength) != S_OK) {
    wprintf(L"ERROR - PrintLines() get_length");
    return;
  }

  DWORD dwRVA;
  IDiaEnumLineNumbers *pLines;

  if (pFunction->get_relativeVirtualAddress(&dwRVA) == S_OK) {
    if (SUCCEEDED(pSession->findLinesByRVA(dwRVA, static_cast<DWORD>(ulLength), &pLines))) {
      PrintLines(pLines);
      pLines->Release();
    }
  }

  else {
    DWORD dwSect;
    DWORD dwOffset;

    if ((pFunction->get_addressSection(&dwSect) == S_OK) &&
        (pFunction->get_addressOffset(&dwOffset) == S_OK)) {
      if (SUCCEEDED(pSession->findLinesByAddr(dwSect, dwOffset, static_cast<DWORD>(ulLength), &pLines))) {
        PrintLines(pLines);
        pLines->Release();
      }
    }
  }
}

////////////////////////////////////////////////////////////
//
void PrintLines(IDiaEnumLineNumbers *pLines)
{
  IDiaLineNumber *pLine;
  DWORD celt;
  DWORD dwRVA;
  DWORD dwSeg;
  DWORD dwOffset;
  DWORD dwLinenum;
  DWORD dwSrcId;
  DWORD dwLength;

  DWORD dwSrcIdLast = (DWORD)(-1);

  while (SUCCEEDED(pLines->Next(1, &pLine, &celt)) && (celt == 1)) {
    if ((pLine->get_relativeVirtualAddress(&dwRVA) == S_OK) &&
        (pLine->get_addressSection(&dwSeg) == S_OK) &&
        (pLine->get_addressOffset(&dwOffset) == S_OK) &&
        (pLine->get_lineNumber(&dwLinenum) == S_OK) &&
        (pLine->get_sourceFileId(&dwSrcId) == S_OK) &&
        (pLine->get_length(&dwLength) == S_OK)) {
      wprintf(L"\tline %u at [%08X][%04X:%08X], len = 0x%X", dwLinenum, dwRVA, dwSeg, dwOffset, dwLength);

      if (dwSrcId != dwSrcIdLast) {
        IDiaSourceFile *pSource;

        if (pLine->get_sourceFile(&pSource) == S_OK) {
          PrintSourceFile(pSource);

          dwSrcIdLast = dwSrcId;

          pSource->Release();
        }
      }

      pLine->Release();

      putwchar(L'\n');
    }
  }
}

////////////////////////////////////////////////////////////
// Print the section contribution data: name, Sec::Off, length
void PrintSecContribs(IDiaSectionContrib *pSegment)
{
  DWORD dwRVA;
  DWORD dwSect;
  DWORD dwOffset;
  DWORD dwLen;
  IDiaSymbol *pCompiland;
  BSTR bstrName;

  if ((pSegment->get_relativeVirtualAddress(&dwRVA) == S_OK) &&
      (pSegment->get_addressSection(&dwSect) == S_OK) &&
      (pSegment->get_addressOffset(&dwOffset) == S_OK) &&
      (pSegment->get_length(&dwLen) == S_OK) &&
      (pSegment->get_compiland(&pCompiland) == S_OK) &&
      (pCompiland->get_name(&bstrName) == S_OK)) {
    wprintf(L"  %08X  %04X:%08X  %08X  %s\n", dwRVA, dwSect, dwOffset, dwLen, bstrName);

    pCompiland->Release();

    SysFreeString(bstrName);
  }
}

////////////////////////////////////////////////////////////
// Print a debug stream data
//
void PrintStreamData(IDiaEnumDebugStreamData *pStream)
{
  BSTR bstrName;

  if (pStream->get_name(&bstrName) != S_OK) {
    wprintf(L"ERROR - PrintStreamData() get_name\n");
  }

  else {
    wprintf(L"Stream: %s", bstrName);

    SysFreeString(bstrName);
  }

  LONG dwElem;

  if (pStream->get_Count(&dwElem) != S_OK) {
      wprintf(L"ERROR - PrintStreamData() get_Count\n");
  }

  else {
    wprintf(L"(%u)\n", dwElem);
  }

  DWORD cbTotal = 0;

  BYTE data[1024];
  DWORD cbData;
  ULONG celt = 0;

  while (SUCCEEDED(pStream->Next(1, sizeof(data), &cbData, (BYTE *) &data, &celt)) && (celt == 1)) {
    DWORD i;

    for (i = 0; i < cbData; i++) {
      wprintf(L"%02X ", data[i]);

      if (i && (i % 8 == 7) && (i+1 < cbData)) {
        wprintf(L"- ");
      }
    }

    wprintf(L"| ");

    for (i = 0; i < cbData; i++) {
      wprintf(L"%c", iswprint(data[i]) ? data[i] : '.');
    }

    putwchar(L'\n');

    cbTotal += cbData;
  }

  wprintf(L"Summary :\n\tNo of Elems = %u\n", dwElem);
  if (dwElem != 0) {
    wprintf(L"\tSizeof(Elem) = %u\n", cbTotal / dwElem);
  }
  putwchar(L'\n');
}

////////////////////////////////////////////////////////////
// Print the FPO info for a given symbol;
//
void PrintFrameData(IDiaFrameData *pFrameData)
{
  DWORD dwSect;
  DWORD dwOffset;
  DWORD cbBlock;
  DWORD cbLocals;                      // Number of bytes reserved for the function locals
  DWORD cbParams;                      // Number of bytes reserved for the function arguments
  DWORD cbMaxStack;
  DWORD cbProlog;
  DWORD cbSavedRegs;
  BOOL bSEH;
  BOOL bEH;
  BOOL bStart;

  if ((pFrameData->get_addressSection(&dwSect) == S_OK) &&
      (pFrameData->get_addressOffset(&dwOffset) == S_OK) &&
      (pFrameData->get_lengthBlock(&cbBlock) == S_OK) &&
      (pFrameData->get_lengthLocals(&cbLocals) == S_OK) &&
      (pFrameData->get_lengthParams(&cbParams) == S_OK) &&
      (pFrameData->get_maxStack(&cbMaxStack) == S_OK) &&
      (pFrameData->get_lengthProlog(&cbProlog) == S_OK) &&
      (pFrameData->get_lengthSavedRegisters(&cbSavedRegs) == S_OK) &&
      (pFrameData->get_systemExceptionHandling(&bSEH) == S_OK) &&
      (pFrameData->get_cplusplusExceptionHandling(&bEH) == S_OK) &&
      (pFrameData->get_functionStart(&bStart) == S_OK)) {
    wprintf(L"%04X:%08X   %8X %8X %8X %8X %8X %8X %c   %c   %c",
            dwSect, dwOffset, cbBlock, cbLocals, cbParams, cbMaxStack, cbProlog, cbSavedRegs,
            bSEH ? L'Y' : L'N',
            bEH ? L'Y' : L'N',
            bStart ? L'Y' : L'N');

    BSTR bstrProgram;

    if (pFrameData->get_program(&bstrProgram) == S_OK) {
      wprintf(L" %s", bstrProgram);

      SysFreeString(bstrProgram);
    }

    putwchar(L'\n');
  }
}

////////////////////////////////////////////////////////////
// Print all the valid properties associated to a symbol
//
void PrintPropertyStorage(IDiaPropertyStorage *pPropertyStorage)
{
  IEnumSTATPROPSTG *pEnumProps;

  if (SUCCEEDED(pPropertyStorage->Enum(&pEnumProps))) {
    STATPROPSTG prop;
    DWORD celt = 1;

    while (SUCCEEDED(pEnumProps->Next(celt, &prop, &celt)) && (celt == 1)) {
      PROPSPEC pspec = { PRSPEC_PROPID, prop.propid };
      PROPVARIANT vt = { VT_EMPTY };

      if (SUCCEEDED(pPropertyStorage->ReadMultiple(1, &pspec, &vt))) {
        switch (vt.vt) {
          case VT_BOOL:
            wprintf(L"%32s:\t %s\n", prop.lpwstrName, vt.bVal ? L"true" : L"false");
            break;

          case VT_I2:
            wprintf(L"%32s:\t %d\n", prop.lpwstrName, vt.iVal);
            break;

          case VT_UI2:
            wprintf(L"%32s:\t %u\n", prop.lpwstrName, vt.uiVal);
            break;

          case VT_I4:
            wprintf(L"%32s:\t %d\n", prop.lpwstrName, vt.intVal);
            break;

          case VT_UI4:
            wprintf(L"%32s:\t 0x%0X\n", prop.lpwstrName, vt.uintVal);
            break;

          case VT_UI8:
            wprintf(L"%32s:\t 0x%X\n", prop.lpwstrName, vt.uhVal.QuadPart);
            break;

          case VT_BSTR:
            wprintf(L"%32s:\t %s\n", prop.lpwstrName, vt.bstrVal);
            break;

          case VT_UNKNOWN:
            wprintf(L"%32s:\t %p\n", prop.lpwstrName, vt.punkVal);
            break;

          case VT_SAFEARRAY:
            break;
        }

        VariantClear((VARIANTARG *) &vt);
      }

      SysFreeString( prop.lpwstrName );
    }

    pEnumProps->Release();
  }
}
