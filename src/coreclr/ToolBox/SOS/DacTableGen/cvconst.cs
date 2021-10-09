// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// cvconst.h - codeview constant definitions

//      Enumeration for function call type

namespace Dia
{

public enum NameSearchOptions
{
    nsNone = 0,

    nsfCaseSensitive = 0x1,         // apply a case sensitive match
    nsfCaseInsensitive = 0x2,       // apply a case insensitive match
    nsfFNameExt = 0x4,              // treat names as paths and apply a filename.ext match
    nsfRegularExpression = 0x8,     // regular expression
    nsfUndecoratedName = 0x10,      // applies only to symbols that have both undecorated and decorated names

    // predefined names for backward source compatibility

    nsCaseSensitive = nsfCaseSensitive,             // apply a case sensitive match
    nsCaseInsensitive = nsfCaseInsensitive,         // apply a case insensitive match
    nsFNameExt = nsfCaseInsensitive | nsfFNameExt,  // treat names as paths and apply a filename.ext match
    nsRegularExpression = nsfRegularExpression | nsfCaseSensitive,      // regular expression (using only '*' and '?')
    nsCaseInRegularExpression = nsfRegularExpression | nsfCaseInsensitive,  // case insensitive regular expression
}


// the following are error HRESULTS returned by an IDiaDataSource they
// are based on the FACILITY_VISUALCPP (0x6d) defined in delayimp.h

public enum E_PDB
{
    E_PDB_OK= unchecked((int)(((long)(1)<<31) | ((long)(((long)0x6d))<<16) | ((long)(1)))),
    E_PDB_USAGE                 ,
    E_PDB_OUT_OF_MEMORY         , // not used, use E_OUTOFMEMORY
    E_PDB_FILE_SYSTEM           ,
    E_PDB_NOT_FOUND             ,
    E_PDB_INVALID_SIG           ,
    E_PDB_INVALID_AGE           ,
    E_PDB_PRECOMP_REQUIRED      ,
    E_PDB_OUT_OF_TI             ,
    E_PDB_NOT_IMPLEMENTED       ,   // use E_NOTIMPL
    E_PDB_V1_PDB                ,
    E_PDB_FORMAT                ,
    E_PDB_LIMIT                 ,
    E_PDB_CORRUPT               ,
    E_PDB_TI16                  ,
    E_PDB_ACCESS_DENIED         ,  // use E_ACCESSDENIED
    E_PDB_ILLEGAL_TYPE_EDIT     ,
    E_PDB_INVALID_EXECUTABLE    ,
    E_PDB_DBG_NOT_FOUND         ,
    E_PDB_NO_DEBUG_INFO         ,
    E_PDB_INVALID_EXE_TIMESTAMP ,
    E_PDB_RESERVED              ,
    E_PDB_DEBUG_INFO_NOT_IN_PDB ,
    E_PDB_MAX
}

//
// Errors in finding dynamically loaded dlls or functions.
//
public enum DIA_E
{
    DIA_E_MODNOTFOUND = E_PDB.E_PDB_MAX+1,
    DIA_E_PROCNOTFOUND,
}

public enum CV_call_e {
    CV_CALL_NEAR_C      = 0x00, // near right to left push, caller pops stack
    CV_CALL_FAR_C       = 0x01, // far right to left push, caller pops stack
    CV_CALL_NEAR_PASCAL = 0x02, // near left to right push, callee pops stack
    CV_CALL_FAR_PASCAL  = 0x03, // far left to right push, callee pops stack
    CV_CALL_NEAR_FAST   = 0x04, // near left to right push with regs, callee pops stack
    CV_CALL_FAR_FAST    = 0x05, // far left to right push with regs, callee pops stack
    CV_CALL_SKIPPED     = 0x06, // skipped (unused) call index
    CV_CALL_NEAR_STD    = 0x07, // near standard call
    CV_CALL_FAR_STD     = 0x08, // far standard call
    CV_CALL_NEAR_SYS    = 0x09, // near sys call
    CV_CALL_FAR_SYS     = 0x0a, // far sys call
    CV_CALL_THISCALL    = 0x0b, // this call (this passed in register)
    CV_CALL_MIPSCALL    = 0x0c, // Mips call
    CV_CALL_GENERIC     = 0x0d, // Generic call sequence
    CV_CALL_ALPHACALL   = 0x0e, // Alpha call
    CV_CALL_PPCCALL     = 0x0f, // PPC call
    CV_CALL_SHCALL      = 0x10, // Hitachi SuperH call
    CV_CALL_ARMCALL     = 0x11, // ARM call
    CV_CALL_AM33CALL    = 0x12, // AM33 call
    CV_CALL_TRICALL     = 0x13, // TriCore Call
    CV_CALL_SH5CALL     = 0x14, // Hitachi SuperH-5 call
    CV_CALL_M32RCALL    = 0x15, // M32R Call
    CV_CALL_RESERVED    = 0x16  // first unused call enumeration
}




//      Values for the access protection of class attributes


public enum CV_access_e {
    CV_private   = 1,
    CV_protected = 2,
    CV_public    = 3
}

public enum THUNK_ORDINAL {
    THUNK_ORDINAL_NOTYPE,       // standard thunk
    THUNK_ORDINAL_ADJUSTOR,     // "this" adjustor thunk
    THUNK_ORDINAL_VCALL,        // virtual call thunk
    THUNK_ORDINAL_PCODE,        // pcode thunk
    THUNK_ORDINAL_LOAD,         // thunk which loads the address to jump to
                                //  via unknown means...

 // trampoline thunk ordinals   - only for use in Trampoline thunk symbols
    THUNK_ORDINAL_TRAMP_INCREMENTAL,
    THUNK_ORDINAL_TRAMP_BRANCHISLAND,

}


public enum CV_SourceChksum_t {
    CHKSUM_TYPE_NONE = 0,        // indicates no checksum is available
    CHKSUM_TYPE_MD5
}

//
// DIA enums
//

public enum LocationType
{
    LocIsNull,
    LocIsStatic,
    LocIsTLS,
    LocIsRegRel,
    LocIsThisRel,
    LocIsEnregistered,
    LocIsBitField,
    LocIsSlot,
    LocIsIlRel,
    LocInMetaData,
    LocIsConstant,
    LocTypeMax
};

public enum DataKind
{
    DataIsUnknown,
    DataIsLocal,
    DataIsStaticLocal,
    DataIsParam,
    DataIsObjectPtr,
    DataIsFileStatic,
    DataIsGlobal,
    DataIsMember,
    DataIsStaticMember,
    DataIsConstant
};

public enum UdtKind
{
    UdtStruct,
    UdtClass,
    UdtUnion
};

public enum BasicType
{
    btNoType = 0,
    btVoid = 1,
    btChar = 2,
    btWChar = 3,
    btInt = 6,
    btUInt = 7,
    btFloat = 8,
    btBCD = 9,
    btBool = 10,
    btLong = 13,
    btULong = 14,
    btCurrency = 25,
    btDate = 26,
    btVariant = 27,
    btComplex = 28,
    btBit = 29,
    btBSTR = 30,
    btHresult = 31
};


//  enum describing the compile flag source language


public enum CV_CFL_LANG {
    CV_CFL_C        = 0x00,
    CV_CFL_CXX      = 0x01,
    CV_CFL_FORTRAN  = 0x02,
    CV_CFL_MASM     = 0x03,
    CV_CFL_PASCAL   = 0x04,
    CV_CFL_BASIC    = 0x05,
    CV_CFL_COBOL    = 0x06,
    CV_CFL_LINK     = 0x07,
    CV_CFL_CVTRES   = 0x08,
    CV_CFL_CVTPGD   = 0x09,
}


//  enum describing target processor


public enum CV_CPU_TYPE_e {
    CV_CFL_8080         = 0x00,
    CV_CFL_8086         = 0x01,
    CV_CFL_80286        = 0x02,
    CV_CFL_80386        = 0x03,
    CV_CFL_80486        = 0x04,
    CV_CFL_PENTIUM      = 0x05,
    CV_CFL_PENTIUMII    = 0x06,
    CV_CFL_PENTIUMPRO   = CV_CFL_PENTIUMII,
    CV_CFL_PENTIUMIII   = 0x07,
    CV_CFL_MIPS         = 0x10,
    CV_CFL_MIPSR4000    = CV_CFL_MIPS,  // don't break current code
    CV_CFL_MIPS16       = 0x11,
    CV_CFL_MIPS32       = 0x12,
    CV_CFL_MIPS64       = 0x13,
    CV_CFL_MIPSI        = 0x14,
    CV_CFL_MIPSII       = 0x15,
    CV_CFL_MIPSIII      = 0x16,
    CV_CFL_MIPSIV       = 0x17,
    CV_CFL_MIPSV        = 0x18,
    CV_CFL_M68000       = 0x20,
    CV_CFL_M68010       = 0x21,
    CV_CFL_M68020       = 0x22,
    CV_CFL_M68030       = 0x23,
    CV_CFL_M68040       = 0x24,
    CV_CFL_ALPHA        = 0x30,
    CV_CFL_ALPHA_21064  = 0x30,
    CV_CFL_ALPHA_21164  = 0x31,
    CV_CFL_ALPHA_21164A = 0x32,
    CV_CFL_ALPHA_21264  = 0x33,
    CV_CFL_ALPHA_21364  = 0x34,
    CV_CFL_PPC601       = 0x40,
    CV_CFL_PPC603       = 0x41,
    CV_CFL_PPC604       = 0x42,
    CV_CFL_PPC620       = 0x43,
    CV_CFL_PPCFP        = 0x44,
    CV_CFL_SH3          = 0x50,
    CV_CFL_SH3E         = 0x51,
    CV_CFL_SH3DSP       = 0x52,
    CV_CFL_SH4          = 0x53,
    CV_CFL_SHMEDIA      = 0x54,
    CV_CFL_ARM3         = 0x60,
    CV_CFL_ARM4         = 0x61,
    CV_CFL_ARM4T        = 0x62,
    CV_CFL_ARM5         = 0x63,
    CV_CFL_ARM5T        = 0x64,
    CV_CFL_OMNI         = 0x70,
    CV_CFL_IA64         = 0x80,
    CV_CFL_IA64_1       = 0x80,
    CV_CFL_IA64_2       = 0x81,
    CV_CFL_CEE          = 0x90,
    CV_CFL_AM33         = 0xA0,
    CV_CFL_M32R         = 0xB0,
    CV_CFL_TRICORE      = 0xC0,
    CV_CFL_RESERVED1    = 0xD0,
    CV_CFL_EBC          = 0xE0,
    CV_CFL_THUMB        = 0xF0,
}

public enum CV_HREG_e {
    // Register subset shared by all processor types,
    // must not overlap with any of the ranges below, hence the high values

    CV_ALLREG_ERR   =   30000,
    CV_ALLREG_TEB   =   30001,
    CV_ALLREG_TIMER =   30002,
    CV_ALLREG_EFAD1 =   30003,
    CV_ALLREG_EFAD2 =   30004,
    CV_ALLREG_EFAD3 =   30005,
    CV_ALLREG_VFRAME=   30006,
    CV_ALLREG_HANDLE=   30007,
    CV_ALLREG_PARAMS=   30008,
    CV_ALLREG_LOCALS=   30009,


    //  Register set for the Intel 80x86 and ix86 processor series
    //  (plus PCODE registers)

    CV_REG_NONE     =   0,
    CV_REG_AL       =   1,
    CV_REG_CL       =   2,
    CV_REG_DL       =   3,
    CV_REG_BL       =   4,
    CV_REG_AH       =   5,
    CV_REG_CH       =   6,
    CV_REG_DH       =   7,
    CV_REG_BH       =   8,
    CV_REG_AX       =   9,
    CV_REG_CX       =  10,
    CV_REG_DX       =  11,
    CV_REG_BX       =  12,
    CV_REG_SP       =  13,
    CV_REG_BP       =  14,
    CV_REG_SI       =  15,
    CV_REG_DI       =  16,
    CV_REG_EAX      =  17,
    CV_REG_ECX      =  18,
    CV_REG_EDX      =  19,
    CV_REG_EBX      =  20,
    CV_REG_ESP      =  21,
    CV_REG_EBP      =  22,
    CV_REG_ESI      =  23,
    CV_REG_EDI      =  24,
    CV_REG_ES       =  25,
    CV_REG_CS       =  26,
    CV_REG_SS       =  27,
    CV_REG_DS       =  28,
    CV_REG_FS       =  29,
    CV_REG_GS       =  30,
    CV_REG_IP       =  31,
    CV_REG_FLAGS    =  32,
    CV_REG_EIP      =  33,
    CV_REG_EFLAGS   =  34,
    CV_REG_TEMP     =  40,          // PCODE Temp
    CV_REG_TEMPH    =  41,          // PCODE TempH
    CV_REG_QUOTE    =  42,          // PCODE Quote
    CV_REG_PCDR3    =  43,          // PCODE reserved
    CV_REG_PCDR4    =  44,          // PCODE reserved
    CV_REG_PCDR5    =  45,          // PCODE reserved
    CV_REG_PCDR6    =  46,          // PCODE reserved
    CV_REG_PCDR7    =  47,          // PCODE reserved
    CV_REG_CR0      =  80,          // CR0 -- control registers
    CV_REG_CR1      =  81,
    CV_REG_CR2      =  82,
    CV_REG_CR3      =  83,
    CV_REG_CR4      =  84,          // Pentium
    CV_REG_DR0      =  90,          // Debug register
    CV_REG_DR1      =  91,
    CV_REG_DR2      =  92,
    CV_REG_DR3      =  93,
    CV_REG_DR4      =  94,
    CV_REG_DR5      =  95,
    CV_REG_DR6      =  96,
    CV_REG_DR7      =  97,
    CV_REG_GDTR     =  110,
    CV_REG_GDTL     =  111,
    CV_REG_IDTR     =  112,
    CV_REG_IDTL     =  113,
    CV_REG_LDTR     =  114,
    CV_REG_TR       =  115,

    CV_REG_PSEUDO1  =  116,
    CV_REG_PSEUDO2  =  117,
    CV_REG_PSEUDO3  =  118,
    CV_REG_PSEUDO4  =  119,
    CV_REG_PSEUDO5  =  120,
    CV_REG_PSEUDO6  =  121,
    CV_REG_PSEUDO7  =  122,
    CV_REG_PSEUDO8  =  123,
    CV_REG_PSEUDO9  =  124,

    CV_REG_ST0      =  128,
    CV_REG_ST1      =  129,
    CV_REG_ST2      =  130,
    CV_REG_ST3      =  131,
    CV_REG_ST4      =  132,
    CV_REG_ST5      =  133,
    CV_REG_ST6      =  134,
    CV_REG_ST7      =  135,
    CV_REG_CTRL     =  136,
    CV_REG_STAT     =  137,
    CV_REG_TAG      =  138,
    CV_REG_FPIP     =  139,
    CV_REG_FPCS     =  140,
    CV_REG_FPDO     =  141,
    CV_REG_FPDS     =  142,
    CV_REG_ISEM     =  143,
    CV_REG_FPEIP    =  144,
    CV_REG_FPEDO    =  145,

    CV_REG_MM0      =  146,
    CV_REG_MM1      =  147,
    CV_REG_MM2      =  148,
    CV_REG_MM3      =  149,
    CV_REG_MM4      =  150,
    CV_REG_MM5      =  151,
    CV_REG_MM6      =  152,
    CV_REG_MM7      =  153,

    CV_REG_XMM0     =  154, // KATMAI registers
    CV_REG_XMM1     =  155,
    CV_REG_XMM2     =  156,
    CV_REG_XMM3     =  157,
    CV_REG_XMM4     =  158,
    CV_REG_XMM5     =  159,
    CV_REG_XMM6     =  160,
    CV_REG_XMM7     =  161,

    CV_REG_XMM00    =  162, // KATMAI sub-registers
    CV_REG_XMM01    =  163,
    CV_REG_XMM02    =  164,
    CV_REG_XMM03    =  165,
    CV_REG_XMM10    =  166,
    CV_REG_XMM11    =  167,
    CV_REG_XMM12    =  168,
    CV_REG_XMM13    =  169,
    CV_REG_XMM20    =  170,
    CV_REG_XMM21    =  171,
    CV_REG_XMM22    =  172,
    CV_REG_XMM23    =  173,
    CV_REG_XMM30    =  174,
    CV_REG_XMM31    =  175,
    CV_REG_XMM32    =  176,
    CV_REG_XMM33    =  177,
    CV_REG_XMM40    =  178,
    CV_REG_XMM41    =  179,
    CV_REG_XMM42    =  180,
    CV_REG_XMM43    =  181,
    CV_REG_XMM50    =  182,
    CV_REG_XMM51    =  183,
    CV_REG_XMM52    =  184,
    CV_REG_XMM53    =  185,
    CV_REG_XMM60    =  186,
    CV_REG_XMM61    =  187,
    CV_REG_XMM62    =  188,
    CV_REG_XMM63    =  189,
    CV_REG_XMM70    =  190,
    CV_REG_XMM71    =  191,
    CV_REG_XMM72    =  192,
    CV_REG_XMM73    =  193,

    CV_REG_XMM0L    =  194,
    CV_REG_XMM1L    =  195,
    CV_REG_XMM2L    =  196,
    CV_REG_XMM3L    =  197,
    CV_REG_XMM4L    =  198,
    CV_REG_XMM5L    =  199,
    CV_REG_XMM6L    =  200,
    CV_REG_XMM7L    =  201,

    CV_REG_XMM0H    =  202,
    CV_REG_XMM1H    =  203,
    CV_REG_XMM2H    =  204,
    CV_REG_XMM3H    =  205,
    CV_REG_XMM4H    =  206,
    CV_REG_XMM5H    =  207,
    CV_REG_XMM6H    =  208,
    CV_REG_XMM7H    =  209,

    CV_REG_MXCSR    =  211, // XMM status register

    CV_REG_EDXEAX   =  212, // EDX:EAX pair

    CV_REG_EMM0L    =  220, // XMM sub-registers (WNI integer)
    CV_REG_EMM1L    =  221,
    CV_REG_EMM2L    =  222,
    CV_REG_EMM3L    =  223,
    CV_REG_EMM4L    =  224,
    CV_REG_EMM5L    =  225,
    CV_REG_EMM6L    =  226,
    CV_REG_EMM7L    =  227,

    CV_REG_EMM0H    =  228,
    CV_REG_EMM1H    =  229,
    CV_REG_EMM2H    =  230,
    CV_REG_EMM3H    =  231,
    CV_REG_EMM4H    =  232,
    CV_REG_EMM5H    =  233,
    CV_REG_EMM6H    =  234,
    CV_REG_EMM7H    =  235,

    // do not change the order of these regs, first one must be even too
    CV_REG_MM00     =  236,
    CV_REG_MM01     =  237,
    CV_REG_MM10     =  238,
    CV_REG_MM11     =  239,
    CV_REG_MM20     =  240,
    CV_REG_MM21     =  241,
    CV_REG_MM30     =  242,
    CV_REG_MM31     =  243,
    CV_REG_MM40     =  244,
    CV_REG_MM41     =  245,
    CV_REG_MM50     =  246,
    CV_REG_MM51     =  247,
    CV_REG_MM60     =  248,
    CV_REG_MM61     =  249,
    CV_REG_MM70     =  250,
    CV_REG_MM71     =  251,

    // registers for the 68K processors

    CV_R68_D0       =    0,
    CV_R68_D1       =    1,
    CV_R68_D2       =    2,
    CV_R68_D3       =    3,
    CV_R68_D4       =    4,
    CV_R68_D5       =    5,
    CV_R68_D6       =    6,
    CV_R68_D7       =    7,
    CV_R68_A0       =    8,
    CV_R68_A1       =    9,
    CV_R68_A2       =   10,
    CV_R68_A3       =   11,
    CV_R68_A4       =   12,
    CV_R68_A5       =   13,
    CV_R68_A6       =   14,
    CV_R68_A7       =   15,
    CV_R68_CCR      =   16,
    CV_R68_SR       =   17,
    CV_R68_USP      =   18,
    CV_R68_MSP      =   19,
    CV_R68_SFC      =   20,
    CV_R68_DFC      =   21,
    CV_R68_CACR     =   22,
    CV_R68_VBR      =   23,
    CV_R68_CAAR     =   24,
    CV_R68_ISP      =   25,
    CV_R68_PC       =   26,
    //reserved  27
    CV_R68_FPCR     =   28,
    CV_R68_FPSR     =   29,
    CV_R68_FPIAR    =   30,
    //reserved  31
    CV_R68_FP0      =   32,
    CV_R68_FP1      =   33,
    CV_R68_FP2      =   34,
    CV_R68_FP3      =   35,
    CV_R68_FP4      =   36,
    CV_R68_FP5      =   37,
    CV_R68_FP6      =   38,
    CV_R68_FP7      =   39,
    //reserved  40
    CV_R68_MMUSR030 =   41,
    CV_R68_MMUSR    =   42,
    CV_R68_URP      =   43,
    CV_R68_DTT0     =   44,
    CV_R68_DTT1     =   45,
    CV_R68_ITT0     =   46,
    CV_R68_ITT1     =   47,
    //reserved  50
    CV_R68_PSR      =   51,
    CV_R68_PCSR     =   52,
    CV_R68_VAL      =   53,
    CV_R68_CRP      =   54,
    CV_R68_SRP      =   55,
    CV_R68_DRP      =   56,
    CV_R68_TC       =   57,
    CV_R68_AC       =   58,
    CV_R68_SCC      =   59,
    CV_R68_CAL      =   60,
    CV_R68_TT0      =   61,
    CV_R68_TT1      =   62,
    //reserved  63
    CV_R68_BAD0     =   64,
    CV_R68_BAD1     =   65,
    CV_R68_BAD2     =   66,
    CV_R68_BAD3     =   67,
    CV_R68_BAD4     =   68,
    CV_R68_BAD5     =   69,
    CV_R68_BAD6     =   70,
    CV_R68_BAD7     =   71,
    CV_R68_BAC0     =   72,
    CV_R68_BAC1     =   73,
    CV_R68_BAC2     =   74,
    CV_R68_BAC3     =   75,
    CV_R68_BAC4     =   76,
    CV_R68_BAC5     =   77,
    CV_R68_BAC6     =   78,
    CV_R68_BAC7     =   79,

     // Register set for the MIPS 4000

    CV_M4_NOREG     =   CV_REG_NONE,

    CV_M4_IntZERO   =   10,      /* CPU REGISTER */
    CV_M4_IntAT     =   11,
    CV_M4_IntV0     =   12,
    CV_M4_IntV1     =   13,
    CV_M4_IntA0     =   14,
    CV_M4_IntA1     =   15,
    CV_M4_IntA2     =   16,
    CV_M4_IntA3     =   17,
    CV_M4_IntT0     =   18,
    CV_M4_IntT1     =   19,
    CV_M4_IntT2     =   20,
    CV_M4_IntT3     =   21,
    CV_M4_IntT4     =   22,
    CV_M4_IntT5     =   23,
    CV_M4_IntT6     =   24,
    CV_M4_IntT7     =   25,
    CV_M4_IntS0     =   26,
    CV_M4_IntS1     =   27,
    CV_M4_IntS2     =   28,
    CV_M4_IntS3     =   29,
    CV_M4_IntS4     =   30,
    CV_M4_IntS5     =   31,
    CV_M4_IntS6     =   32,
    CV_M4_IntS7     =   33,
    CV_M4_IntT8     =   34,
    CV_M4_IntT9     =   35,
    CV_M4_IntKT0    =   36,
    CV_M4_IntKT1    =   37,
    CV_M4_IntGP     =   38,
    CV_M4_IntSP     =   39,
    CV_M4_IntS8     =   40,
    CV_M4_IntRA     =   41,
    CV_M4_IntLO     =   42,
    CV_M4_IntHI     =   43,

    CV_M4_Fir       =   50,
    CV_M4_Psr       =   51,

    CV_M4_FltF0     =   60,      /* Floating point registers */
    CV_M4_FltF1     =   61,
    CV_M4_FltF2     =   62,
    CV_M4_FltF3     =   63,
    CV_M4_FltF4     =   64,
    CV_M4_FltF5     =   65,
    CV_M4_FltF6     =   66,
    CV_M4_FltF7     =   67,
    CV_M4_FltF8     =   68,
    CV_M4_FltF9     =   69,
    CV_M4_FltF10    =   70,
    CV_M4_FltF11    =   71,
    CV_M4_FltF12    =   72,
    CV_M4_FltF13    =   73,
    CV_M4_FltF14    =   74,
    CV_M4_FltF15    =   75,
    CV_M4_FltF16    =   76,
    CV_M4_FltF17    =   77,
    CV_M4_FltF18    =   78,
    CV_M4_FltF19    =   79,
    CV_M4_FltF20    =   80,
    CV_M4_FltF21    =   81,
    CV_M4_FltF22    =   82,
    CV_M4_FltF23    =   83,
    CV_M4_FltF24    =   84,
    CV_M4_FltF25    =   85,
    CV_M4_FltF26    =   86,
    CV_M4_FltF27    =   87,
    CV_M4_FltF28    =   88,
    CV_M4_FltF29    =   89,
    CV_M4_FltF30    =   90,
    CV_M4_FltF31    =   91,
    CV_M4_FltFsr    =   92,


    // Register set for the ALPHA AXP

    CV_ALPHA_NOREG  = CV_REG_NONE,

    CV_ALPHA_FltF0  =   10,   // Floating point registers
    CV_ALPHA_FltF1  =   11,
    CV_ALPHA_FltF2  =   12,
    CV_ALPHA_FltF3  =   13,
    CV_ALPHA_FltF4  =   14,
    CV_ALPHA_FltF5  =   15,
    CV_ALPHA_FltF6  =   16,
    CV_ALPHA_FltF7  =   17,
    CV_ALPHA_FltF8  =   18,
    CV_ALPHA_FltF9  =   19,
    CV_ALPHA_FltF10 =   20,
    CV_ALPHA_FltF11 =   21,
    CV_ALPHA_FltF12 =   22,
    CV_ALPHA_FltF13 =   23,
    CV_ALPHA_FltF14 =   24,
    CV_ALPHA_FltF15 =   25,
    CV_ALPHA_FltF16 =   26,
    CV_ALPHA_FltF17 =   27,
    CV_ALPHA_FltF18 =   28,
    CV_ALPHA_FltF19 =   29,
    CV_ALPHA_FltF20 =   30,
    CV_ALPHA_FltF21 =   31,
    CV_ALPHA_FltF22 =   32,
    CV_ALPHA_FltF23 =   33,
    CV_ALPHA_FltF24 =   34,
    CV_ALPHA_FltF25 =   35,
    CV_ALPHA_FltF26 =   36,
    CV_ALPHA_FltF27 =   37,
    CV_ALPHA_FltF28 =   38,
    CV_ALPHA_FltF29 =   39,
    CV_ALPHA_FltF30 =   40,
    CV_ALPHA_FltF31 =   41,

    CV_ALPHA_IntV0  =   42,   // Integer registers
    CV_ALPHA_IntT0  =   43,
    CV_ALPHA_IntT1  =   44,
    CV_ALPHA_IntT2  =   45,
    CV_ALPHA_IntT3  =   46,
    CV_ALPHA_IntT4  =   47,
    CV_ALPHA_IntT5  =   48,
    CV_ALPHA_IntT6  =   49,
    CV_ALPHA_IntT7  =   50,
    CV_ALPHA_IntS0  =   51,
    CV_ALPHA_IntS1  =   52,
    CV_ALPHA_IntS2  =   53,
    CV_ALPHA_IntS3  =   54,
    CV_ALPHA_IntS4  =   55,
    CV_ALPHA_IntS5  =   56,
    CV_ALPHA_IntFP  =   57,
    CV_ALPHA_IntA0  =   58,
    CV_ALPHA_IntA1  =   59,
    CV_ALPHA_IntA2  =   60,
    CV_ALPHA_IntA3  =   61,
    CV_ALPHA_IntA4  =   62,
    CV_ALPHA_IntA5  =   63,
    CV_ALPHA_IntT8  =   64,
    CV_ALPHA_IntT9  =   65,
    CV_ALPHA_IntT10 =   66,
    CV_ALPHA_IntT11 =   67,
    CV_ALPHA_IntRA  =   68,
    CV_ALPHA_IntT12 =   69,
    CV_ALPHA_IntAT  =   70,
    CV_ALPHA_IntGP  =   71,
    CV_ALPHA_IntSP  =   72,
    CV_ALPHA_IntZERO =  73,


    CV_ALPHA_Fpcr   =   74,   // Control registers
    CV_ALPHA_Fir    =   75,
    CV_ALPHA_Psr    =   76,
    CV_ALPHA_FltFsr =   77,
    CV_ALPHA_SoftFpcr =   78,

    // Register Set for Motorola/IBM PowerPC

    /*
    ** PowerPC General Registers ( User Level )
    */
    CV_PPC_GPR0     =  1,
    CV_PPC_GPR1     =  2,
    CV_PPC_GPR2     =  3,
    CV_PPC_GPR3     =  4,
    CV_PPC_GPR4     =  5,
    CV_PPC_GPR5     =  6,
    CV_PPC_GPR6     =  7,
    CV_PPC_GPR7     =  8,
    CV_PPC_GPR8     =  9,
    CV_PPC_GPR9     = 10,
    CV_PPC_GPR10    = 11,
    CV_PPC_GPR11    = 12,
    CV_PPC_GPR12    = 13,
    CV_PPC_GPR13    = 14,
    CV_PPC_GPR14    = 15,
    CV_PPC_GPR15    = 16,
    CV_PPC_GPR16    = 17,
    CV_PPC_GPR17    = 18,
    CV_PPC_GPR18    = 19,
    CV_PPC_GPR19    = 20,
    CV_PPC_GPR20    = 21,
    CV_PPC_GPR21    = 22,
    CV_PPC_GPR22    = 23,
    CV_PPC_GPR23    = 24,
    CV_PPC_GPR24    = 25,
    CV_PPC_GPR25    = 26,
    CV_PPC_GPR26    = 27,
    CV_PPC_GPR27    = 28,
    CV_PPC_GPR28    = 29,
    CV_PPC_GPR29    = 30,
    CV_PPC_GPR30    = 31,
    CV_PPC_GPR31    = 32,

    /*
    ** PowerPC Condition Register ( User Level )
    */
    CV_PPC_CR       = 33,
    CV_PPC_CR0      = 34,
    CV_PPC_CR1      = 35,
    CV_PPC_CR2      = 36,
    CV_PPC_CR3      = 37,
    CV_PPC_CR4      = 38,
    CV_PPC_CR5      = 39,
    CV_PPC_CR6      = 40,
    CV_PPC_CR7      = 41,

    /*
    ** PowerPC Floating Point Registers ( User Level )
    */
    CV_PPC_FPR0     = 42,
    CV_PPC_FPR1     = 43,
    CV_PPC_FPR2     = 44,
    CV_PPC_FPR3     = 45,
    CV_PPC_FPR4     = 46,
    CV_PPC_FPR5     = 47,
    CV_PPC_FPR6     = 48,
    CV_PPC_FPR7     = 49,
    CV_PPC_FPR8     = 50,
    CV_PPC_FPR9     = 51,
    CV_PPC_FPR10    = 52,
    CV_PPC_FPR11    = 53,
    CV_PPC_FPR12    = 54,
    CV_PPC_FPR13    = 55,
    CV_PPC_FPR14    = 56,
    CV_PPC_FPR15    = 57,
    CV_PPC_FPR16    = 58,
    CV_PPC_FPR17    = 59,
    CV_PPC_FPR18    = 60,
    CV_PPC_FPR19    = 61,
    CV_PPC_FPR20    = 62,
    CV_PPC_FPR21    = 63,
    CV_PPC_FPR22    = 64,
    CV_PPC_FPR23    = 65,
    CV_PPC_FPR24    = 66,
    CV_PPC_FPR25    = 67,
    CV_PPC_FPR26    = 68,
    CV_PPC_FPR27    = 69,
    CV_PPC_FPR28    = 70,
    CV_PPC_FPR29    = 71,
    CV_PPC_FPR30    = 72,
    CV_PPC_FPR31    = 73,

    /*
    ** PowerPC Floating Point Status and Control Register ( User Level )
    */
    CV_PPC_FPSCR    = 74,

    /*
    ** PowerPC Machine State Register ( Supervisor Level )
    */
    CV_PPC_MSR      = 75,

    /*
    ** PowerPC Segment Registers ( Supervisor Level )
    */
    CV_PPC_SR0      = 76,
    CV_PPC_SR1      = 77,
    CV_PPC_SR2      = 78,
    CV_PPC_SR3      = 79,
    CV_PPC_SR4      = 80,
    CV_PPC_SR5      = 81,
    CV_PPC_SR6      = 82,
    CV_PPC_SR7      = 83,
    CV_PPC_SR8      = 84,
    CV_PPC_SR9      = 85,
    CV_PPC_SR10     = 86,
    CV_PPC_SR11     = 87,
    CV_PPC_SR12     = 88,
    CV_PPC_SR13     = 89,
    CV_PPC_SR14     = 90,
    CV_PPC_SR15     = 91,

    /*
    ** For all of the special purpose registers add 100 to the SPR# that the
    ** Motorola/IBM documentation gives with the exception of any imaginary
    ** registers.
    */

    /*
    ** PowerPC Special Purpose Registers ( User Level )
    */
    CV_PPC_PC       = 99,     // PC (imaginary register)

    CV_PPC_MQ       = 100,    // MPC601
    CV_PPC_XER      = 101,
    CV_PPC_RTCU     = 104,    // MPC601
    CV_PPC_RTCL     = 105,    // MPC601
    CV_PPC_LR       = 108,
    CV_PPC_CTR      = 109,

    CV_PPC_COMPARE  = 110,    // part of XER (internal to the debugger only)
    CV_PPC_COUNT    = 111,    // part of XER (internal to the debugger only)

    /*
    ** PowerPC Special Purpose Registers ( Supervisor Level )
    */
    CV_PPC_DSISR    = 118,
    CV_PPC_DAR      = 119,
    CV_PPC_DEC      = 122,
    CV_PPC_SDR1     = 125,
    CV_PPC_SRR0     = 126,
    CV_PPC_SRR1     = 127,
    CV_PPC_SPRG0    = 372,
    CV_PPC_SPRG1    = 373,
    CV_PPC_SPRG2    = 374,
    CV_PPC_SPRG3    = 375,
    CV_PPC_ASR      = 280,    // 64-bit implementations only
    CV_PPC_EAR      = 382,
    CV_PPC_PVR      = 287,
    CV_PPC_BAT0U    = 628,
    CV_PPC_BAT0L    = 629,
    CV_PPC_BAT1U    = 630,
    CV_PPC_BAT1L    = 631,
    CV_PPC_BAT2U    = 632,
    CV_PPC_BAT2L    = 633,
    CV_PPC_BAT3U    = 634,
    CV_PPC_BAT3L    = 635,
    CV_PPC_DBAT0U   = 636,
    CV_PPC_DBAT0L   = 637,
    CV_PPC_DBAT1U   = 638,
    CV_PPC_DBAT1L   = 639,
    CV_PPC_DBAT2U   = 640,
    CV_PPC_DBAT2L   = 641,
    CV_PPC_DBAT3U   = 642,
    CV_PPC_DBAT3L   = 643,

    /*
    ** PowerPC Special Purpose Registers Implementation Dependent ( Supervisor Level )
    */

    /*
    ** Doesn't appear that IBM/Motorola has finished defining these.
    */

    CV_PPC_PMR0     = 1044,   // MPC620,
    CV_PPC_PMR1     = 1045,   // MPC620,
    CV_PPC_PMR2     = 1046,   // MPC620,
    CV_PPC_PMR3     = 1047,   // MPC620,
    CV_PPC_PMR4     = 1048,   // MPC620,
    CV_PPC_PMR5     = 1049,   // MPC620,
    CV_PPC_PMR6     = 1050,   // MPC620,
    CV_PPC_PMR7     = 1051,   // MPC620,
    CV_PPC_PMR8     = 1052,   // MPC620,
    CV_PPC_PMR9     = 1053,   // MPC620,
    CV_PPC_PMR10    = 1054,   // MPC620,
    CV_PPC_PMR11    = 1055,   // MPC620,
    CV_PPC_PMR12    = 1056,   // MPC620,
    CV_PPC_PMR13    = 1057,   // MPC620,
    CV_PPC_PMR14    = 1058,   // MPC620,
    CV_PPC_PMR15    = 1059,   // MPC620,

    CV_PPC_DMISS    = 1076,   // MPC603
    CV_PPC_DCMP     = 1077,   // MPC603
    CV_PPC_HASH1    = 1078,   // MPC603
    CV_PPC_HASH2    = 1079,   // MPC603
    CV_PPC_IMISS    = 1080,   // MPC603
    CV_PPC_ICMP     = 1081,   // MPC603
    CV_PPC_RPA      = 1082,   // MPC603

    CV_PPC_HID0     = 1108,   // MPC601, MPC603, MPC620
    CV_PPC_HID1     = 1109,   // MPC601
    CV_PPC_HID2     = 1110,   // MPC601, MPC603, MPC620 ( IABR )
    CV_PPC_HID3     = 1111,   // Not Defined
    CV_PPC_HID4     = 1112,   // Not Defined
    CV_PPC_HID5     = 1113,   // MPC601, MPC604, MPC620 ( DABR )
    CV_PPC_HID6     = 1114,   // Not Defined
    CV_PPC_HID7     = 1115,   // Not Defined
    CV_PPC_HID8     = 1116,   // MPC620 ( BUSCSR )
    CV_PPC_HID9     = 1117,   // MPC620 ( L2CSR )
    CV_PPC_HID10    = 1118,   // Not Defined
    CV_PPC_HID11    = 1119,   // Not Defined
    CV_PPC_HID12    = 1120,   // Not Defined
    CV_PPC_HID13    = 1121,   // MPC604 ( HCR )
    CV_PPC_HID14    = 1122,   // Not Defined
    CV_PPC_HID15    = 1123,   // MPC601, MPC604, MPC620 ( PIR )

    //
    // JAVA VM registers
    //

    CV_JAVA_PC      = 1,

    //
    // Register set for the Hitachi SH3
    //

    CV_SH3_NOREG    =   CV_REG_NONE,

    CV_SH3_IntR0    =   10,   // CPU REGISTER
    CV_SH3_IntR1    =   11,
    CV_SH3_IntR2    =   12,
    CV_SH3_IntR3    =   13,
    CV_SH3_IntR4    =   14,
    CV_SH3_IntR5    =   15,
    CV_SH3_IntR6    =   16,
    CV_SH3_IntR7    =   17,
    CV_SH3_IntR8    =   18,
    CV_SH3_IntR9    =   19,
    CV_SH3_IntR10   =   20,
    CV_SH3_IntR11   =   21,
    CV_SH3_IntR12   =   22,
    CV_SH3_IntR13   =   23,
    CV_SH3_IntFp    =   24,
    CV_SH3_IntSp    =   25,
    CV_SH3_Gbr      =   38,
    CV_SH3_Pr       =   39,
    CV_SH3_Mach     =   40,
    CV_SH3_Macl     =   41,

    CV_SH3_Pc       =   50,
    CV_SH3_Sr       =   51,

    CV_SH3_BarA     =   60,
    CV_SH3_BasrA    =   61,
    CV_SH3_BamrA    =   62,
    CV_SH3_BbrA     =   63,
    CV_SH3_BarB     =   64,
    CV_SH3_BasrB    =   65,
    CV_SH3_BamrB    =   66,
    CV_SH3_BbrB     =   67,
    CV_SH3_BdrB     =   68,
    CV_SH3_BdmrB    =   69,
    CV_SH3_Brcr     =   70,

    //
    // Additional registers for Hitachi SH processors
    //

    CV_SH_Fpscr    =   75,    // floating point status/control register
    CV_SH_Fpul     =   76,    // floating point communication register

    CV_SH_FpR0     =   80,    // Floating point registers
    CV_SH_FpR1     =   81,
    CV_SH_FpR2     =   82,
    CV_SH_FpR3     =   83,
    CV_SH_FpR4     =   84,
    CV_SH_FpR5     =   85,
    CV_SH_FpR6     =   86,
    CV_SH_FpR7     =   87,
    CV_SH_FpR8     =   88,
    CV_SH_FpR9     =   89,
    CV_SH_FpR10    =   90,
    CV_SH_FpR11    =   91,
    CV_SH_FpR12    =   92,
    CV_SH_FpR13    =   93,
    CV_SH_FpR14    =   94,
    CV_SH_FpR15    =   95,

    CV_SH_XFpR0    =   96,
    CV_SH_XFpR1    =   97,
    CV_SH_XFpR2    =   98,
    CV_SH_XFpR3    =   99,
    CV_SH_XFpR4    =  100,
    CV_SH_XFpR5    =  101,
    CV_SH_XFpR6    =  102,
    CV_SH_XFpR7    =  103,
    CV_SH_XFpR8    =  104,
    CV_SH_XFpR9    =  105,
    CV_SH_XFpR10   =  106,
    CV_SH_XFpR11   =  107,
    CV_SH_XFpR12   =  108,
    CV_SH_XFpR13   =  109,
    CV_SH_XFpR14   =  110,
    CV_SH_XFpR15   =  111,

    //
    // Register set for the ARM processor.
    //

    CV_ARM_NOREG    =   CV_REG_NONE,

    CV_ARM_R0       =   10,
    CV_ARM_R1       =   11,
    CV_ARM_R2       =   12,
    CV_ARM_R3       =   13,
    CV_ARM_R4       =   14,
    CV_ARM_R5       =   15,
    CV_ARM_R6       =   16,
    CV_ARM_R7       =   17,
    CV_ARM_R8       =   18,
    CV_ARM_R9       =   19,
    CV_ARM_R10      =   20,
    CV_ARM_R11      =   21, // Frame pointer, if allocated
    CV_ARM_R12      =   22,
    CV_ARM_SP       =   23, // Stack pointer
    CV_ARM_LR       =   24, // Link Register
    CV_ARM_PC       =   25, // Program counter
    CV_ARM_CPSR     =   26, // Current program status register

    //
    // Register set for Intel IA64
    //

    CV_IA64_NOREG   =   CV_REG_NONE,

    // Branch Registers

    CV_IA64_Br0     =   512,
    CV_IA64_Br1     =   513,
    CV_IA64_Br2     =   514,
    CV_IA64_Br3     =   515,
    CV_IA64_Br4     =   516,
    CV_IA64_Br5     =   517,
    CV_IA64_Br6     =   518,
    CV_IA64_Br7     =   519,

    // Predicate Registers

    CV_IA64_P0    =   704,
    CV_IA64_P1    =   705,
    CV_IA64_P2    =   706,
    CV_IA64_P3    =   707,
    CV_IA64_P4    =   708,
    CV_IA64_P5    =   709,
    CV_IA64_P6    =   710,
    CV_IA64_P7    =   711,
    CV_IA64_P8    =   712,
    CV_IA64_P9    =   713,
    CV_IA64_P10   =   714,
    CV_IA64_P11   =   715,
    CV_IA64_P12   =   716,
    CV_IA64_P13   =   717,
    CV_IA64_P14   =   718,
    CV_IA64_P15   =   719,
    CV_IA64_P16   =   720,
    CV_IA64_P17   =   721,
    CV_IA64_P18   =   722,
    CV_IA64_P19   =   723,
    CV_IA64_P20   =   724,
    CV_IA64_P21   =   725,
    CV_IA64_P22   =   726,
    CV_IA64_P23   =   727,
    CV_IA64_P24   =   728,
    CV_IA64_P25   =   729,
    CV_IA64_P26   =   730,
    CV_IA64_P27   =   731,
    CV_IA64_P28   =   732,
    CV_IA64_P29   =   733,
    CV_IA64_P30   =   734,
    CV_IA64_P31   =   735,
    CV_IA64_P32   =   736,
    CV_IA64_P33   =   737,
    CV_IA64_P34   =   738,
    CV_IA64_P35   =   739,
    CV_IA64_P36   =   740,
    CV_IA64_P37   =   741,
    CV_IA64_P38   =   742,
    CV_IA64_P39   =   743,
    CV_IA64_P40   =   744,
    CV_IA64_P41   =   745,
    CV_IA64_P42   =   746,
    CV_IA64_P43   =   747,
    CV_IA64_P44   =   748,
    CV_IA64_P45   =   749,
    CV_IA64_P46   =   750,
    CV_IA64_P47   =   751,
    CV_IA64_P48   =   752,
    CV_IA64_P49   =   753,
    CV_IA64_P50   =   754,
    CV_IA64_P51   =   755,
    CV_IA64_P52   =   756,
    CV_IA64_P53   =   757,
    CV_IA64_P54   =   758,
    CV_IA64_P55   =   759,
    CV_IA64_P56   =   760,
    CV_IA64_P57   =   761,
    CV_IA64_P58   =   762,
    CV_IA64_P59   =   763,
    CV_IA64_P60   =   764,
    CV_IA64_P61   =   765,
    CV_IA64_P62   =   766,
    CV_IA64_P63   =   767,

    CV_IA64_Preds   =   768,

    // Banked General Registers

    CV_IA64_IntH0   =   832,
    CV_IA64_IntH1   =   833,
    CV_IA64_IntH2   =   834,
    CV_IA64_IntH3   =   835,
    CV_IA64_IntH4   =   836,
    CV_IA64_IntH5   =   837,
    CV_IA64_IntH6   =   838,
    CV_IA64_IntH7   =   839,
    CV_IA64_IntH8   =   840,
    CV_IA64_IntH9   =   841,
    CV_IA64_IntH10  =   842,
    CV_IA64_IntH11  =   843,
    CV_IA64_IntH12  =   844,
    CV_IA64_IntH13  =   845,
    CV_IA64_IntH14  =   846,
    CV_IA64_IntH15  =   847,

    // Special Registers

    CV_IA64_Ip      =   1016,
    CV_IA64_Umask   =   1017,
    CV_IA64_Cfm     =   1018,
    CV_IA64_Psr     =   1019,

    // Banked General Registers

    CV_IA64_Nats    =   1020,
    CV_IA64_Nats2   =   1021,
    CV_IA64_Nats3   =   1022,

    // General-Purpose Registers

    // Integer registers
    CV_IA64_IntR0   =   1024,
    CV_IA64_IntR1   =   1025,
    CV_IA64_IntR2   =   1026,
    CV_IA64_IntR3   =   1027,
    CV_IA64_IntR4   =   1028,
    CV_IA64_IntR5   =   1029,
    CV_IA64_IntR6   =   1030,
    CV_IA64_IntR7   =   1031,
    CV_IA64_IntR8   =   1032,
    CV_IA64_IntR9   =   1033,
    CV_IA64_IntR10  =   1034,
    CV_IA64_IntR11  =   1035,
    CV_IA64_IntR12  =   1036,
    CV_IA64_IntR13  =   1037,
    CV_IA64_IntR14  =   1038,
    CV_IA64_IntR15  =   1039,
    CV_IA64_IntR16  =   1040,
    CV_IA64_IntR17  =   1041,
    CV_IA64_IntR18  =   1042,
    CV_IA64_IntR19  =   1043,
    CV_IA64_IntR20  =   1044,
    CV_IA64_IntR21  =   1045,
    CV_IA64_IntR22  =   1046,
    CV_IA64_IntR23  =   1047,
    CV_IA64_IntR24  =   1048,
    CV_IA64_IntR25  =   1049,
    CV_IA64_IntR26  =   1050,
    CV_IA64_IntR27  =   1051,
    CV_IA64_IntR28  =   1052,
    CV_IA64_IntR29  =   1053,
    CV_IA64_IntR30  =   1054,
    CV_IA64_IntR31  =   1055,

    // Register Stack
    CV_IA64_IntR32  =   1056,
    CV_IA64_IntR33  =   1057,
    CV_IA64_IntR34  =   1058,
    CV_IA64_IntR35  =   1059,
    CV_IA64_IntR36  =   1060,
    CV_IA64_IntR37  =   1061,
    CV_IA64_IntR38  =   1062,
    CV_IA64_IntR39  =   1063,
    CV_IA64_IntR40  =   1064,
    CV_IA64_IntR41  =   1065,
    CV_IA64_IntR42  =   1066,
    CV_IA64_IntR43  =   1067,
    CV_IA64_IntR44  =   1068,
    CV_IA64_IntR45  =   1069,
    CV_IA64_IntR46  =   1070,
    CV_IA64_IntR47  =   1071,
    CV_IA64_IntR48  =   1072,
    CV_IA64_IntR49  =   1073,
    CV_IA64_IntR50  =   1074,
    CV_IA64_IntR51  =   1075,
    CV_IA64_IntR52  =   1076,
    CV_IA64_IntR53  =   1077,
    CV_IA64_IntR54  =   1078,
    CV_IA64_IntR55  =   1079,
    CV_IA64_IntR56  =   1080,
    CV_IA64_IntR57  =   1081,
    CV_IA64_IntR58  =   1082,
    CV_IA64_IntR59  =   1083,
    CV_IA64_IntR60  =   1084,
    CV_IA64_IntR61  =   1085,
    CV_IA64_IntR62  =   1086,
    CV_IA64_IntR63  =   1087,
    CV_IA64_IntR64  =   1088,
    CV_IA64_IntR65  =   1089,
    CV_IA64_IntR66  =   1090,
    CV_IA64_IntR67  =   1091,
    CV_IA64_IntR68  =   1092,
    CV_IA64_IntR69  =   1093,
    CV_IA64_IntR70  =   1094,
    CV_IA64_IntR71  =   1095,
    CV_IA64_IntR72  =   1096,
    CV_IA64_IntR73  =   1097,
    CV_IA64_IntR74  =   1098,
    CV_IA64_IntR75  =   1099,
    CV_IA64_IntR76  =   1100,
    CV_IA64_IntR77  =   1101,
    CV_IA64_IntR78  =   1102,
    CV_IA64_IntR79  =   1103,
    CV_IA64_IntR80  =   1104,
    CV_IA64_IntR81  =   1105,
    CV_IA64_IntR82  =   1106,
    CV_IA64_IntR83  =   1107,
    CV_IA64_IntR84  =   1108,
    CV_IA64_IntR85  =   1109,
    CV_IA64_IntR86  =   1110,
    CV_IA64_IntR87  =   1111,
    CV_IA64_IntR88  =   1112,
    CV_IA64_IntR89  =   1113,
    CV_IA64_IntR90  =   1114,
    CV_IA64_IntR91  =   1115,
    CV_IA64_IntR92  =   1116,
    CV_IA64_IntR93  =   1117,
    CV_IA64_IntR94  =   1118,
    CV_IA64_IntR95  =   1119,
    CV_IA64_IntR96  =   1120,
    CV_IA64_IntR97  =   1121,
    CV_IA64_IntR98  =   1122,
    CV_IA64_IntR99  =   1123,
    CV_IA64_IntR100 =   1124,
    CV_IA64_IntR101 =   1125,
    CV_IA64_IntR102 =   1126,
    CV_IA64_IntR103 =   1127,
    CV_IA64_IntR104 =   1128,
    CV_IA64_IntR105 =   1129,
    CV_IA64_IntR106 =   1130,
    CV_IA64_IntR107 =   1131,
    CV_IA64_IntR108 =   1132,
    CV_IA64_IntR109 =   1133,
    CV_IA64_IntR110 =   1134,
    CV_IA64_IntR111 =   1135,
    CV_IA64_IntR112 =   1136,
    CV_IA64_IntR113 =   1137,
    CV_IA64_IntR114 =   1138,
    CV_IA64_IntR115 =   1139,
    CV_IA64_IntR116 =   1140,
    CV_IA64_IntR117 =   1141,
    CV_IA64_IntR118 =   1142,
    CV_IA64_IntR119 =   1143,
    CV_IA64_IntR120 =   1144,
    CV_IA64_IntR121 =   1145,
    CV_IA64_IntR122 =   1146,
    CV_IA64_IntR123 =   1147,
    CV_IA64_IntR124 =   1148,
    CV_IA64_IntR125 =   1149,
    CV_IA64_IntR126 =   1150,
    CV_IA64_IntR127 =   1151,

    // Floating-Point Registers

    // Low Floating Point Registers
    CV_IA64_FltF0   =   2048,
    CV_IA64_FltF1   =   2049,
    CV_IA64_FltF2   =   2050,
    CV_IA64_FltF3   =   2051,
    CV_IA64_FltF4   =   2052,
    CV_IA64_FltF5   =   2053,
    CV_IA64_FltF6   =   2054,
    CV_IA64_FltF7   =   2055,
    CV_IA64_FltF8   =   2056,
    CV_IA64_FltF9   =   2057,
    CV_IA64_FltF10  =   2058,
    CV_IA64_FltF11  =   2059,
    CV_IA64_FltF12  =   2060,
    CV_IA64_FltF13  =   2061,
    CV_IA64_FltF14  =   2062,
    CV_IA64_FltF15  =   2063,
    CV_IA64_FltF16  =   2064,
    CV_IA64_FltF17  =   2065,
    CV_IA64_FltF18  =   2066,
    CV_IA64_FltF19  =   2067,
    CV_IA64_FltF20  =   2068,
    CV_IA64_FltF21  =   2069,
    CV_IA64_FltF22  =   2070,
    CV_IA64_FltF23  =   2071,
    CV_IA64_FltF24  =   2072,
    CV_IA64_FltF25  =   2073,
    CV_IA64_FltF26  =   2074,
    CV_IA64_FltF27  =   2075,
    CV_IA64_FltF28  =   2076,
    CV_IA64_FltF29  =   2077,
    CV_IA64_FltF30  =   2078,
    CV_IA64_FltF31  =   2079,

    // High Floating Point Registers
    CV_IA64_FltF32  =   2080,
    CV_IA64_FltF33  =   2081,
    CV_IA64_FltF34  =   2082,
    CV_IA64_FltF35  =   2083,
    CV_IA64_FltF36  =   2084,
    CV_IA64_FltF37  =   2085,
    CV_IA64_FltF38  =   2086,
    CV_IA64_FltF39  =   2087,
    CV_IA64_FltF40  =   2088,
    CV_IA64_FltF41  =   2089,
    CV_IA64_FltF42  =   2090,
    CV_IA64_FltF43  =   2091,
    CV_IA64_FltF44  =   2092,
    CV_IA64_FltF45  =   2093,
    CV_IA64_FltF46  =   2094,
    CV_IA64_FltF47  =   2095,
    CV_IA64_FltF48  =   2096,
    CV_IA64_FltF49  =   2097,
    CV_IA64_FltF50  =   2098,
    CV_IA64_FltF51  =   2099,
    CV_IA64_FltF52  =   2100,
    CV_IA64_FltF53  =   2101,
    CV_IA64_FltF54  =   2102,
    CV_IA64_FltF55  =   2103,
    CV_IA64_FltF56  =   2104,
    CV_IA64_FltF57  =   2105,
    CV_IA64_FltF58  =   2106,
    CV_IA64_FltF59  =   2107,
    CV_IA64_FltF60  =   2108,
    CV_IA64_FltF61  =   2109,
    CV_IA64_FltF62  =   2110,
    CV_IA64_FltF63  =   2111,
    CV_IA64_FltF64  =   2112,
    CV_IA64_FltF65  =   2113,
    CV_IA64_FltF66  =   2114,
    CV_IA64_FltF67  =   2115,
    CV_IA64_FltF68  =   2116,
    CV_IA64_FltF69  =   2117,
    CV_IA64_FltF70  =   2118,
    CV_IA64_FltF71  =   2119,
    CV_IA64_FltF72  =   2120,
    CV_IA64_FltF73  =   2121,
    CV_IA64_FltF74  =   2122,
    CV_IA64_FltF75  =   2123,
    CV_IA64_FltF76  =   2124,
    CV_IA64_FltF77  =   2125,
    CV_IA64_FltF78  =   2126,
    CV_IA64_FltF79  =   2127,
    CV_IA64_FltF80  =   2128,
    CV_IA64_FltF81  =   2129,
    CV_IA64_FltF82  =   2130,
    CV_IA64_FltF83  =   2131,
    CV_IA64_FltF84  =   2132,
    CV_IA64_FltF85  =   2133,
    CV_IA64_FltF86  =   2134,
    CV_IA64_FltF87  =   2135,
    CV_IA64_FltF88  =   2136,
    CV_IA64_FltF89  =   2137,
    CV_IA64_FltF90  =   2138,
    CV_IA64_FltF91  =   2139,
    CV_IA64_FltF92  =   2140,
    CV_IA64_FltF93  =   2141,
    CV_IA64_FltF94  =   2142,
    CV_IA64_FltF95  =   2143,
    CV_IA64_FltF96  =   2144,
    CV_IA64_FltF97  =   2145,
    CV_IA64_FltF98  =   2146,
    CV_IA64_FltF99  =   2147,
    CV_IA64_FltF100 =   2148,
    CV_IA64_FltF101 =   2149,
    CV_IA64_FltF102 =   2150,
    CV_IA64_FltF103 =   2151,
    CV_IA64_FltF104 =   2152,
    CV_IA64_FltF105 =   2153,
    CV_IA64_FltF106 =   2154,
    CV_IA64_FltF107 =   2155,
    CV_IA64_FltF108 =   2156,
    CV_IA64_FltF109 =   2157,
    CV_IA64_FltF110 =   2158,
    CV_IA64_FltF111 =   2159,
    CV_IA64_FltF112 =   2160,
    CV_IA64_FltF113 =   2161,
    CV_IA64_FltF114 =   2162,
    CV_IA64_FltF115 =   2163,
    CV_IA64_FltF116 =   2164,
    CV_IA64_FltF117 =   2165,
    CV_IA64_FltF118 =   2166,
    CV_IA64_FltF119 =   2167,
    CV_IA64_FltF120 =   2168,
    CV_IA64_FltF121 =   2169,
    CV_IA64_FltF122 =   2170,
    CV_IA64_FltF123 =   2171,
    CV_IA64_FltF124 =   2172,
    CV_IA64_FltF125 =   2173,
    CV_IA64_FltF126 =   2174,
    CV_IA64_FltF127 =   2175,

    // Application Registers

    CV_IA64_ApKR0   =   3072,
    CV_IA64_ApKR1   =   3073,
    CV_IA64_ApKR2   =   3074,
    CV_IA64_ApKR3   =   3075,
    CV_IA64_ApKR4   =   3076,
    CV_IA64_ApKR5   =   3077,
    CV_IA64_ApKR6   =   3078,
    CV_IA64_ApKR7   =   3079,
    CV_IA64_AR8     =   3080,
    CV_IA64_AR9     =   3081,
    CV_IA64_AR10    =   3082,
    CV_IA64_AR11    =   3083,
    CV_IA64_AR12    =   3084,
    CV_IA64_AR13    =   3085,
    CV_IA64_AR14    =   3086,
    CV_IA64_AR15    =   3087,
    CV_IA64_RsRSC   =   3088,
    CV_IA64_RsBSP   =   3089,
    CV_IA64_RsBSPSTORE  =   3090,
    CV_IA64_RsRNAT  =   3091,
    CV_IA64_AR20    =   3092,
    CV_IA64_StFCR   =   3093,
    CV_IA64_AR22    =   3094,
    CV_IA64_AR23    =   3095,
    CV_IA64_EFLAG   =   3096,
    CV_IA64_CSD     =   3097,
    CV_IA64_SSD     =   3098,
    CV_IA64_CFLG    =   3099,
    CV_IA64_StFSR   =   3100,
    CV_IA64_StFIR   =   3101,
    CV_IA64_StFDR   =   3102,
    CV_IA64_AR31    =   3103,
    CV_IA64_ApCCV   =   3104,
    CV_IA64_AR33    =   3105,
    CV_IA64_AR34    =   3106,
    CV_IA64_AR35    =   3107,
    CV_IA64_ApUNAT  =   3108,
    CV_IA64_AR37    =   3109,
    CV_IA64_AR38    =   3110,
    CV_IA64_AR39    =   3111,
    CV_IA64_StFPSR  =   3112,
    CV_IA64_AR41    =   3113,
    CV_IA64_AR42    =   3114,
    CV_IA64_AR43    =   3115,
    CV_IA64_ApITC   =   3116,
    CV_IA64_AR45    =   3117,
    CV_IA64_AR46    =   3118,
    CV_IA64_AR47    =   3119,
    CV_IA64_AR48    =   3120,
    CV_IA64_AR49    =   3121,
    CV_IA64_AR50    =   3122,
    CV_IA64_AR51    =   3123,
    CV_IA64_AR52    =   3124,
    CV_IA64_AR53    =   3125,
    CV_IA64_AR54    =   3126,
    CV_IA64_AR55    =   3127,
    CV_IA64_AR56    =   3128,
    CV_IA64_AR57    =   3129,
    CV_IA64_AR58    =   3130,
    CV_IA64_AR59    =   3131,
    CV_IA64_AR60    =   3132,
    CV_IA64_AR61    =   3133,
    CV_IA64_AR62    =   3134,
    CV_IA64_AR63    =   3135,
    CV_IA64_RsPFS   =   3136,
    CV_IA64_ApLC    =   3137,
    CV_IA64_ApEC    =   3138,
    CV_IA64_AR67    =   3139,
    CV_IA64_AR68    =   3140,
    CV_IA64_AR69    =   3141,
    CV_IA64_AR70    =   3142,
    CV_IA64_AR71    =   3143,
    CV_IA64_AR72    =   3144,
    CV_IA64_AR73    =   3145,
    CV_IA64_AR74    =   3146,
    CV_IA64_AR75    =   3147,
    CV_IA64_AR76    =   3148,
    CV_IA64_AR77    =   3149,
    CV_IA64_AR78    =   3150,
    CV_IA64_AR79    =   3151,
    CV_IA64_AR80    =   3152,
    CV_IA64_AR81    =   3153,
    CV_IA64_AR82    =   3154,
    CV_IA64_AR83    =   3155,
    CV_IA64_AR84    =   3156,
    CV_IA64_AR85    =   3157,
    CV_IA64_AR86    =   3158,
    CV_IA64_AR87    =   3159,
    CV_IA64_AR88    =   3160,
    CV_IA64_AR89    =   3161,
    CV_IA64_AR90    =   3162,
    CV_IA64_AR91    =   3163,
    CV_IA64_AR92    =   3164,
    CV_IA64_AR93    =   3165,
    CV_IA64_AR94    =   3166,
    CV_IA64_AR95    =   3167,
    CV_IA64_AR96    =   3168,
    CV_IA64_AR97    =   3169,
    CV_IA64_AR98    =   3170,
    CV_IA64_AR99    =   3171,
    CV_IA64_AR100   =   3172,
    CV_IA64_AR101   =   3173,
    CV_IA64_AR102   =   3174,
    CV_IA64_AR103   =   3175,
    CV_IA64_AR104   =   3176,
    CV_IA64_AR105   =   3177,
    CV_IA64_AR106   =   3178,
    CV_IA64_AR107   =   3179,
    CV_IA64_AR108   =   3180,
    CV_IA64_AR109   =   3181,
    CV_IA64_AR110   =   3182,
    CV_IA64_AR111   =   3183,
    CV_IA64_AR112   =   3184,
    CV_IA64_AR113   =   3185,
    CV_IA64_AR114   =   3186,
    CV_IA64_AR115   =   3187,
    CV_IA64_AR116   =   3188,
    CV_IA64_AR117   =   3189,
    CV_IA64_AR118   =   3190,
    CV_IA64_AR119   =   3191,
    CV_IA64_AR120   =   3192,
    CV_IA64_AR121   =   3193,
    CV_IA64_AR122   =   3194,
    CV_IA64_AR123   =   3195,
    CV_IA64_AR124   =   3196,
    CV_IA64_AR125   =   3197,
    CV_IA64_AR126   =   3198,
    CV_IA64_AR127   =   3199,

    // CPUID Registers

    CV_IA64_CPUID0  =   3328,
    CV_IA64_CPUID1  =   3329,
    CV_IA64_CPUID2  =   3330,
    CV_IA64_CPUID3  =   3331,
    CV_IA64_CPUID4  =   3332,

    // Control Registers

    CV_IA64_ApDCR   =   4096,
    CV_IA64_ApITM   =   4097,
    CV_IA64_ApIVA   =   4098,
    CV_IA64_CR3     =   4099,
    CV_IA64_CR4     =   4100,
    CV_IA64_CR5     =   4101,
    CV_IA64_CR6     =   4102,
    CV_IA64_CR7     =   4103,
    CV_IA64_ApPTA   =   4104,
    CV_IA64_ApGPTA  =   4105,
    CV_IA64_CR10    =   4106,
    CV_IA64_CR11    =   4107,
    CV_IA64_CR12    =   4108,
    CV_IA64_CR13    =   4109,
    CV_IA64_CR14    =   4110,
    CV_IA64_CR15    =   4111,
    CV_IA64_StIPSR  =   4112,
    CV_IA64_StISR   =   4113,
    CV_IA64_CR18    =   4114,
    CV_IA64_StIIP   =   4115,
    CV_IA64_StIFA   =   4116,
    CV_IA64_StITIR  =   4117,
    CV_IA64_StIIPA  =   4118,
    CV_IA64_StIFS   =   4119,
    CV_IA64_StIIM   =   4120,
    CV_IA64_StIHA   =   4121,
    CV_IA64_CR26    =   4122,
    CV_IA64_CR27    =   4123,
    CV_IA64_CR28    =   4124,
    CV_IA64_CR29    =   4125,
    CV_IA64_CR30    =   4126,
    CV_IA64_CR31    =   4127,
    CV_IA64_CR32    =   4128,
    CV_IA64_CR33    =   4129,
    CV_IA64_CR34    =   4130,
    CV_IA64_CR35    =   4131,
    CV_IA64_CR36    =   4132,
    CV_IA64_CR37    =   4133,
    CV_IA64_CR38    =   4134,
    CV_IA64_CR39    =   4135,
    CV_IA64_CR40    =   4136,
    CV_IA64_CR41    =   4137,
    CV_IA64_CR42    =   4138,
    CV_IA64_CR43    =   4139,
    CV_IA64_CR44    =   4140,
    CV_IA64_CR45    =   4141,
    CV_IA64_CR46    =   4142,
    CV_IA64_CR47    =   4143,
    CV_IA64_CR48    =   4144,
    CV_IA64_CR49    =   4145,
    CV_IA64_CR50    =   4146,
    CV_IA64_CR51    =   4147,
    CV_IA64_CR52    =   4148,
    CV_IA64_CR53    =   4149,
    CV_IA64_CR54    =   4150,
    CV_IA64_CR55    =   4151,
    CV_IA64_CR56    =   4152,
    CV_IA64_CR57    =   4153,
    CV_IA64_CR58    =   4154,
    CV_IA64_CR59    =   4155,
    CV_IA64_CR60    =   4156,
    CV_IA64_CR61    =   4157,
    CV_IA64_CR62    =   4158,
    CV_IA64_CR63    =   4159,
    CV_IA64_SaLID   =   4160,
    CV_IA64_SaIVR   =   4161,
    CV_IA64_SaTPR   =   4162,
    CV_IA64_SaEOI   =   4163,
    CV_IA64_SaIRR0  =   4164,
    CV_IA64_SaIRR1  =   4165,
    CV_IA64_SaIRR2  =   4166,
    CV_IA64_SaIRR3  =   4167,
    CV_IA64_SaITV   =   4168,
    CV_IA64_SaPMV   =   4169,
    CV_IA64_SaCMCV  =   4170,
    CV_IA64_CR75    =   4171,
    CV_IA64_CR76    =   4172,
    CV_IA64_CR77    =   4173,
    CV_IA64_CR78    =   4174,
    CV_IA64_CR79    =   4175,
    CV_IA64_SaLRR0  =   4176,
    CV_IA64_SaLRR1  =   4177,
    CV_IA64_CR82    =   4178,
    CV_IA64_CR83    =   4179,
    CV_IA64_CR84    =   4180,
    CV_IA64_CR85    =   4181,
    CV_IA64_CR86    =   4182,
    CV_IA64_CR87    =   4183,
    CV_IA64_CR88    =   4184,
    CV_IA64_CR89    =   4185,
    CV_IA64_CR90    =   4186,
    CV_IA64_CR91    =   4187,
    CV_IA64_CR92    =   4188,
    CV_IA64_CR93    =   4189,
    CV_IA64_CR94    =   4190,
    CV_IA64_CR95    =   4191,
    CV_IA64_CR96    =   4192,
    CV_IA64_CR97    =   4193,
    CV_IA64_CR98    =   4194,
    CV_IA64_CR99    =   4195,
    CV_IA64_CR100   =   4196,
    CV_IA64_CR101   =   4197,
    CV_IA64_CR102   =   4198,
    CV_IA64_CR103   =   4199,
    CV_IA64_CR104   =   4200,
    CV_IA64_CR105   =   4201,
    CV_IA64_CR106   =   4202,
    CV_IA64_CR107   =   4203,
    CV_IA64_CR108   =   4204,
    CV_IA64_CR109   =   4205,
    CV_IA64_CR110   =   4206,
    CV_IA64_CR111   =   4207,
    CV_IA64_CR112   =   4208,
    CV_IA64_CR113   =   4209,
    CV_IA64_CR114   =   4210,
    CV_IA64_CR115   =   4211,
    CV_IA64_CR116   =   4212,
    CV_IA64_CR117   =   4213,
    CV_IA64_CR118   =   4214,
    CV_IA64_CR119   =   4215,
    CV_IA64_CR120   =   4216,
    CV_IA64_CR121   =   4217,
    CV_IA64_CR122   =   4218,
    CV_IA64_CR123   =   4219,
    CV_IA64_CR124   =   4220,
    CV_IA64_CR125   =   4221,
    CV_IA64_CR126   =   4222,
    CV_IA64_CR127   =   4223,

    // Protection Key Registers

    CV_IA64_Pkr0    =   5120,
    CV_IA64_Pkr1    =   5121,
    CV_IA64_Pkr2    =   5122,
    CV_IA64_Pkr3    =   5123,
    CV_IA64_Pkr4    =   5124,
    CV_IA64_Pkr5    =   5125,
    CV_IA64_Pkr6    =   5126,
    CV_IA64_Pkr7    =   5127,
    CV_IA64_Pkr8    =   5128,
    CV_IA64_Pkr9    =   5129,
    CV_IA64_Pkr10   =   5130,
    CV_IA64_Pkr11   =   5131,
    CV_IA64_Pkr12   =   5132,
    CV_IA64_Pkr13   =   5133,
    CV_IA64_Pkr14   =   5134,
    CV_IA64_Pkr15   =   5135,

    // Region Registers

    CV_IA64_Rr0     =   6144,
    CV_IA64_Rr1     =   6145,
    CV_IA64_Rr2     =   6146,
    CV_IA64_Rr3     =   6147,
    CV_IA64_Rr4     =   6148,
    CV_IA64_Rr5     =   6149,
    CV_IA64_Rr6     =   6150,
    CV_IA64_Rr7     =   6151,

    // Performance Monitor Data Registers

    CV_IA64_PFD0    =   7168,
    CV_IA64_PFD1    =   7169,
    CV_IA64_PFD2    =   7170,
    CV_IA64_PFD3    =   7171,
    CV_IA64_PFD4    =   7172,
    CV_IA64_PFD5    =   7173,
    CV_IA64_PFD6    =   7174,
    CV_IA64_PFD7    =   7175,

    // Performance Monitor Config Registers

    CV_IA64_PFC0    =   7424,
    CV_IA64_PFC1    =   7425,
    CV_IA64_PFC2    =   7426,
    CV_IA64_PFC3    =   7427,
    CV_IA64_PFC4    =   7428,
    CV_IA64_PFC5    =   7429,
    CV_IA64_PFC6    =   7430,
    CV_IA64_PFC7    =   7431,

    // Instruction Translation Registers

    CV_IA64_TrI0    =   8192,
    CV_IA64_TrI1    =   8193,
    CV_IA64_TrI2    =   8194,
    CV_IA64_TrI3    =   8195,
    CV_IA64_TrI4    =   8196,
    CV_IA64_TrI5    =   8197,
    CV_IA64_TrI6    =   8198,
    CV_IA64_TrI7    =   8199,

    // Data Translation Registers

    CV_IA64_TrD0    =   8320,
    CV_IA64_TrD1    =   8321,
    CV_IA64_TrD2    =   8322,
    CV_IA64_TrD3    =   8323,
    CV_IA64_TrD4    =   8324,
    CV_IA64_TrD5    =   8325,
    CV_IA64_TrD6    =   8326,
    CV_IA64_TrD7    =   8327,

    // Instruction Breakpoint Registers

    CV_IA64_DbI0    =   8448,
    CV_IA64_DbI1    =   8449,
    CV_IA64_DbI2    =   8450,
    CV_IA64_DbI3    =   8451,
    CV_IA64_DbI4    =   8452,
    CV_IA64_DbI5    =   8453,
    CV_IA64_DbI6    =   8454,
    CV_IA64_DbI7    =   8455,

    // Data Breakpoint Registers

    CV_IA64_DbD0    =   8576,
    CV_IA64_DbD1    =   8577,
    CV_IA64_DbD2    =   8578,
    CV_IA64_DbD3    =   8579,
    CV_IA64_DbD4    =   8580,
    CV_IA64_DbD5    =   8581,
    CV_IA64_DbD6    =   8582,
    CV_IA64_DbD7    =   8583,

    //
    // Register set for the TriCore processor.
    //

    CV_TRI_NOREG    =   CV_REG_NONE,

    // General Purpose Data Registers

    CV_TRI_D0   =   10,
    CV_TRI_D1   =   11,
    CV_TRI_D2   =   12,
    CV_TRI_D3   =   13,
    CV_TRI_D4   =   14,
    CV_TRI_D5   =   15,
    CV_TRI_D6   =   16,
    CV_TRI_D7   =   17,
    CV_TRI_D8   =   18,
    CV_TRI_D9   =   19,
    CV_TRI_D10  =   20,
    CV_TRI_D11  =   21,
    CV_TRI_D12  =   22,
    CV_TRI_D13  =   23,
    CV_TRI_D14  =   24,
    CV_TRI_D15  =   25,

    // General Purpose Address Registers

    CV_TRI_A0   =   26,
    CV_TRI_A1   =   27,
    CV_TRI_A2   =   28,
    CV_TRI_A3   =   29,
    CV_TRI_A4   =   30,
    CV_TRI_A5   =   31,
    CV_TRI_A6   =   32,
    CV_TRI_A7   =   33,
    CV_TRI_A8   =   34,
    CV_TRI_A9   =   35,
    CV_TRI_A10  =   36,
    CV_TRI_A11  =   37,
    CV_TRI_A12  =   38,
    CV_TRI_A13  =   39,
    CV_TRI_A14  =   40,
    CV_TRI_A15  =   41,

    // Extended (64-bit) data registers

    CV_TRI_E0   =   42,
    CV_TRI_E2   =   43,
    CV_TRI_E4   =   44,
    CV_TRI_E6   =   45,
    CV_TRI_E8   =   46,
    CV_TRI_E10  =   47,
    CV_TRI_E12  =   48,
    CV_TRI_E14  =   49,

    // Extended (64-bit) address registers

    CV_TRI_EA0  =   50,
    CV_TRI_EA2  =   51,
    CV_TRI_EA4  =   52,
    CV_TRI_EA6  =   53,
    CV_TRI_EA8  =   54,
    CV_TRI_EA10 =   55,
    CV_TRI_EA12 =   56,
    CV_TRI_EA14 =   57,

    CV_TRI_PSW  =   58,
    CV_TRI_PCXI =   59,
    CV_TRI_PC   =   60,
    CV_TRI_FCX  =   61,
    CV_TRI_LCX  =   62,
    CV_TRI_ISP  =   63,
    CV_TRI_ICR  =   64,
    CV_TRI_BIV  =   65,
    CV_TRI_BTV  =   66,
    CV_TRI_SYSCON   =   67,
    CV_TRI_DPRx_0   =   68,
    CV_TRI_DPRx_1   =   69,
    CV_TRI_DPRx_2   =   70,
    CV_TRI_DPRx_3   =   71,
    CV_TRI_CPRx_0   =   68,
    CV_TRI_CPRx_1   =   69,
    CV_TRI_CPRx_2   =   70,
    CV_TRI_CPRx_3   =   71,
    CV_TRI_DPMx_0   =   68,
    CV_TRI_DPMx_1   =   69,
    CV_TRI_DPMx_2   =   70,
    CV_TRI_DPMx_3   =   71,
    CV_TRI_CPMx_0   =   68,
    CV_TRI_CPMx_1   =   69,
    CV_TRI_CPMx_2   =   70,
    CV_TRI_CPMx_3   =   71,
    CV_TRI_DBGSSR   =   72,
    CV_TRI_EXEVT    =   73,
    CV_TRI_SWEVT    =   74,
    CV_TRI_CREVT    =   75,
    CV_TRI_TRnEVT   =   76,
    CV_TRI_MMUCON   =   77,
    CV_TRI_ASI      =   78,
    CV_TRI_TVA      =   79,
    CV_TRI_TPA      =   80,
    CV_TRI_TPX      =   81,
    CV_TRI_TFA      =   82,

    //
    // Register set for the AM33 and related processors.
    //

    CV_AM33_NOREG   =   CV_REG_NONE,

    // "Extended" (general purpose integer) registers
    CV_AM33_E0      =   10,
    CV_AM33_E1      =   11,
    CV_AM33_E2      =   12,
    CV_AM33_E3      =   13,
    CV_AM33_E4      =   14,
    CV_AM33_E5      =   15,
    CV_AM33_E6      =   16,
    CV_AM33_E7      =   17,

    // Address registers
    CV_AM33_A0      =   20,
    CV_AM33_A1      =   21,
    CV_AM33_A2      =   22,
    CV_AM33_A3      =   23,

    // Integer data registers
    CV_AM33_D0      =   30,
    CV_AM33_D1      =   31,
    CV_AM33_D2      =   32,
    CV_AM33_D3      =   33,

    // (Single-precision) floating-point registers
    CV_AM33_FS0     =   40,
    CV_AM33_FS1     =   41,
    CV_AM33_FS2     =   42,
    CV_AM33_FS3     =   43,
    CV_AM33_FS4     =   44,
    CV_AM33_FS5     =   45,
    CV_AM33_FS6     =   46,
    CV_AM33_FS7     =   47,
    CV_AM33_FS8     =   48,
    CV_AM33_FS9     =   49,
    CV_AM33_FS10    =   50,
    CV_AM33_FS11    =   51,
    CV_AM33_FS12    =   52,
    CV_AM33_FS13    =   53,
    CV_AM33_FS14    =   54,
    CV_AM33_FS15    =   55,
    CV_AM33_FS16    =   56,
    CV_AM33_FS17    =   57,
    CV_AM33_FS18    =   58,
    CV_AM33_FS19    =   59,
    CV_AM33_FS20    =   60,
    CV_AM33_FS21    =   61,
    CV_AM33_FS22    =   62,
    CV_AM33_FS23    =   63,
    CV_AM33_FS24    =   64,
    CV_AM33_FS25    =   65,
    CV_AM33_FS26    =   66,
    CV_AM33_FS27    =   67,
    CV_AM33_FS28    =   68,
    CV_AM33_FS29    =   69,
    CV_AM33_FS30    =   70,
    CV_AM33_FS31    =   71,

    // Special purpose registers

    // Stack pointer
    CV_AM33_SP      =   80,

    // Program counter
    CV_AM33_PC      =   81,

    // Multiply-divide/accumulate registers
    CV_AM33_MDR     =   82,
    CV_AM33_MDRQ    =   83,
    CV_AM33_MCRH    =   84,
    CV_AM33_MCRL    =   85,
    CV_AM33_MCVF    =   86,

    // CPU status words
    CV_AM33_EPSW    =   87,
    CV_AM33_FPCR    =   88,

    // Loop buffer registers
    CV_AM33_LIR     =   89,
    CV_AM33_LAR     =   90,

    //
    // Register set for the Mitsubishi M32R
    //

    CV_M32R_NOREG    =   CV_REG_NONE,

    CV_M32R_R0    =   10,
    CV_M32R_R1    =   11,
    CV_M32R_R2    =   12,
    CV_M32R_R3    =   13,
    CV_M32R_R4    =   14,
    CV_M32R_R5    =   15,
    CV_M32R_R6    =   16,
    CV_M32R_R7    =   17,
    CV_M32R_R8    =   18,
    CV_M32R_R9    =   19,
    CV_M32R_R10   =   20,
    CV_M32R_R11   =   21,
    CV_M32R_R12   =   22,   // Gloabal Pointer, if used
    CV_M32R_R13   =   23,   // Frame Pointer, if allocated
    CV_M32R_R14   =   24,   // Link Register
    CV_M32R_R15   =   25,   // Stack Pointer
    CV_M32R_PSW   =   26,   // Preocessor Status Register
    CV_M32R_CBR   =   27,   // Condition Bit Register
    CV_M32R_SPI   =   28,   // Interrupt Stack Pointer
    CV_M32R_SPU   =   29,   // User Stack Pointer
    CV_M32R_SPO   =   30,   // OS Stack Pointer
    CV_M32R_BPC   =   31,   // Backup Program Counter
    CV_M32R_ACHI  =   32,   // Accumulator High
    CV_M32R_ACLO  =   33,   // Accumulator Low
    CV_M32R_PC    =   34,   // Program Counter

}

} // Namespace Dia
