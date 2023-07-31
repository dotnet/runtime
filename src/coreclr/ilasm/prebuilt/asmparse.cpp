/* A Bison parser, made by GNU Bison 3.8.2.  */

/* Bison implementation for Yacc-like parsers in C

   Copyright (C) 1984, 1989-1990, 2000-2015, 2018-2021 Free Software Foundation,
   Inc.

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with this program.  If not, see <https://www.gnu.org/licenses/>.  */

/* As a special exception, you may create a larger work that contains
   part or all of the Bison parser skeleton and distribute that work
   under terms of your choice, so long as that work isn't itself a
   parser generator using the skeleton or a modified version thereof
   as a parser skeleton.  Alternatively, if you modify or redistribute
   the parser skeleton itself, you may (at your option) remove this
   special exception, which will cause the skeleton and the resulting
   Bison output files to be licensed under the GNU General Public
   License without this special exception.

   This special exception was added by the Free Software Foundation in
   version 2.2 of Bison.  */

/* C LALR(1) parser skeleton written by Richard Stallman, by
   simplifying the original so-called "semantic" parser.  */

/* DO NOT RELY ON FEATURES THAT ARE NOT DOCUMENTED in the manual,
   especially those whose name start with YY_ or yy_.  They are
   private implementation details that can be changed or removed.  */

/* All symbols defined below should begin with yy or YY, to avoid
   infringing on user name space.  This should be done even for local
   variables, as they might otherwise be expanded by user macros.
   There are some unavoidable exceptions within include files to
   define necessary library symbols; they are noted "INFRINGES ON
   USER NAME SPACE" below.  */

/* Identify Bison output, and Bison version.  */
#define YYBISON 30802

/* Bison version string.  */
#define YYBISON_VERSION "3.8.2"

/* Skeleton name.  */
#define YYSKELETON_NAME "yacc.c"

/* Pure parsers.  */
#define YYPURE 0

/* Push parsers.  */
#define YYPUSH 0

/* Pull parsers.  */
#define YYPULL 1




/* First part of user prologue.  */
#line 1 "./asmparse.y"


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// File asmparse.y
//
#include "ilasmpch.h"

#include "grammar_before.cpp"


#line 85 "asmparse.cpp"

# ifndef YY_CAST
#  ifdef __cplusplus
#   define YY_CAST(Type, Val) static_cast<Type> (Val)
#   define YY_REINTERPRET_CAST(Type, Val) reinterpret_cast<Type> (Val)
#  else
#   define YY_CAST(Type, Val) ((Type) (Val))
#   define YY_REINTERPRET_CAST(Type, Val) ((Type) (Val))
#  endif
# endif
# ifndef YY_NULLPTR
#  if defined __cplusplus
#   if 201103L <= __cplusplus
#    define YY_NULLPTR nullptr
#   else
#    define YY_NULLPTR 0
#   endif
#  else
#   define YY_NULLPTR ((void*)0)
#  endif
# endif


/* Debug traces.  */
#ifndef YYDEBUG
# define YYDEBUG 0
#endif
#if YYDEBUG
extern int yydebug;
#endif

/* Token kinds.  */
#ifndef YYTOKENTYPE
# define YYTOKENTYPE
  enum yytokentype
  {
    YYEMPTY = -2,
    YYEOF = 0,                     /* "end of file"  */
    YYerror = 256,                 /* error  */
    YYUNDEF = 257,                 /* "invalid token"  */
    ERROR_ = 258,                  /* ERROR_  */
    BAD_COMMENT_ = 259,            /* BAD_COMMENT_  */
    BAD_LITERAL_ = 260,            /* BAD_LITERAL_  */
    ID = 261,                      /* ID  */
    DOTTEDNAME = 262,              /* DOTTEDNAME  */
    QSTRING = 263,                 /* QSTRING  */
    SQSTRING = 264,                /* SQSTRING  */
    INT32_T = 265,                 /* INT32_T  */
    INT64_T = 266,                 /* INT64_T  */
    FLOAT64 = 267,                 /* FLOAT64  */
    HEXBYTE = 268,                 /* HEXBYTE  */
    TYPEDEF_T = 269,               /* TYPEDEF_T  */
    TYPEDEF_M = 270,               /* TYPEDEF_M  */
    TYPEDEF_F = 271,               /* TYPEDEF_F  */
    TYPEDEF_TS = 272,              /* TYPEDEF_TS  */
    TYPEDEF_MR = 273,              /* TYPEDEF_MR  */
    TYPEDEF_CA = 274,              /* TYPEDEF_CA  */
    DCOLON = 275,                  /* DCOLON  */
    ELLIPSIS = 276,                /* ELLIPSIS  */
    VOID_ = 277,                   /* VOID_  */
    BOOL_ = 278,                   /* BOOL_  */
    CHAR_ = 279,                   /* CHAR_  */
    UNSIGNED_ = 280,               /* UNSIGNED_  */
    INT_ = 281,                    /* INT_  */
    INT8_ = 282,                   /* INT8_  */
    INT16_ = 283,                  /* INT16_  */
    INT32_ = 284,                  /* INT32_  */
    INT64_ = 285,                  /* INT64_  */
    FLOAT_ = 286,                  /* FLOAT_  */
    FLOAT32_ = 287,                /* FLOAT32_  */
    FLOAT64_ = 288,                /* FLOAT64_  */
    BYTEARRAY_ = 289,              /* BYTEARRAY_  */
    UINT_ = 290,                   /* UINT_  */
    UINT8_ = 291,                  /* UINT8_  */
    UINT16_ = 292,                 /* UINT16_  */
    UINT32_ = 293,                 /* UINT32_  */
    UINT64_ = 294,                 /* UINT64_  */
    FLAGS_ = 295,                  /* FLAGS_  */
    CALLCONV_ = 296,               /* CALLCONV_  */
    MDTOKEN_ = 297,                /* MDTOKEN_  */
    OBJECT_ = 298,                 /* OBJECT_  */
    STRING_ = 299,                 /* STRING_  */
    NULLREF_ = 300,                /* NULLREF_  */
    DEFAULT_ = 301,                /* DEFAULT_  */
    CDECL_ = 302,                  /* CDECL_  */
    VARARG_ = 303,                 /* VARARG_  */
    STDCALL_ = 304,                /* STDCALL_  */
    THISCALL_ = 305,               /* THISCALL_  */
    FASTCALL_ = 306,               /* FASTCALL_  */
    CLASS_ = 307,                  /* CLASS_  */
    BYREFLIKE_ = 308,              /* BYREFLIKE_  */
    TYPEDREF_ = 309,               /* TYPEDREF_  */
    UNMANAGED_ = 310,              /* UNMANAGED_  */
    FINALLY_ = 311,                /* FINALLY_  */
    HANDLER_ = 312,                /* HANDLER_  */
    CATCH_ = 313,                  /* CATCH_  */
    FILTER_ = 314,                 /* FILTER_  */
    FAULT_ = 315,                  /* FAULT_  */
    EXTENDS_ = 316,                /* EXTENDS_  */
    IMPLEMENTS_ = 317,             /* IMPLEMENTS_  */
    TO_ = 318,                     /* TO_  */
    AT_ = 319,                     /* AT_  */
    TLS_ = 320,                    /* TLS_  */
    TRUE_ = 321,                   /* TRUE_  */
    FALSE_ = 322,                  /* FALSE_  */
    _INTERFACEIMPL = 323,          /* _INTERFACEIMPL  */
    VALUE_ = 324,                  /* VALUE_  */
    VALUETYPE_ = 325,              /* VALUETYPE_  */
    NATIVE_ = 326,                 /* NATIVE_  */
    INSTANCE_ = 327,               /* INSTANCE_  */
    SPECIALNAME_ = 328,            /* SPECIALNAME_  */
    FORWARDER_ = 329,              /* FORWARDER_  */
    STATIC_ = 330,                 /* STATIC_  */
    PUBLIC_ = 331,                 /* PUBLIC_  */
    PRIVATE_ = 332,                /* PRIVATE_  */
    FAMILY_ = 333,                 /* FAMILY_  */
    FINAL_ = 334,                  /* FINAL_  */
    SYNCHRONIZED_ = 335,           /* SYNCHRONIZED_  */
    INTERFACE_ = 336,              /* INTERFACE_  */
    SEALED_ = 337,                 /* SEALED_  */
    NESTED_ = 338,                 /* NESTED_  */
    ABSTRACT_ = 339,               /* ABSTRACT_  */
    AUTO_ = 340,                   /* AUTO_  */
    SEQUENTIAL_ = 341,             /* SEQUENTIAL_  */
    EXPLICIT_ = 342,               /* EXPLICIT_  */
    ANSI_ = 343,                   /* ANSI_  */
    UNICODE_ = 344,                /* UNICODE_  */
    AUTOCHAR_ = 345,               /* AUTOCHAR_  */
    IMPORT_ = 346,                 /* IMPORT_  */
    ENUM_ = 347,                   /* ENUM_  */
    VIRTUAL_ = 348,                /* VIRTUAL_  */
    NOINLINING_ = 349,             /* NOINLINING_  */
    AGGRESSIVEINLINING_ = 350,     /* AGGRESSIVEINLINING_  */
    NOOPTIMIZATION_ = 351,         /* NOOPTIMIZATION_  */
    AGGRESSIVEOPTIMIZATION_ = 352, /* AGGRESSIVEOPTIMIZATION_  */
    UNMANAGEDEXP_ = 353,           /* UNMANAGEDEXP_  */
    BEFOREFIELDINIT_ = 354,        /* BEFOREFIELDINIT_  */
    STRICT_ = 355,                 /* STRICT_  */
    RETARGETABLE_ = 356,           /* RETARGETABLE_  */
    WINDOWSRUNTIME_ = 357,         /* WINDOWSRUNTIME_  */
    NOPLATFORM_ = 358,             /* NOPLATFORM_  */
    METHOD_ = 359,                 /* METHOD_  */
    FIELD_ = 360,                  /* FIELD_  */
    PINNED_ = 361,                 /* PINNED_  */
    MODREQ_ = 362,                 /* MODREQ_  */
    MODOPT_ = 363,                 /* MODOPT_  */
    SERIALIZABLE_ = 364,           /* SERIALIZABLE_  */
    PROPERTY_ = 365,               /* PROPERTY_  */
    TYPE_ = 366,                   /* TYPE_  */
    ASSEMBLY_ = 367,               /* ASSEMBLY_  */
    FAMANDASSEM_ = 368,            /* FAMANDASSEM_  */
    FAMORASSEM_ = 369,             /* FAMORASSEM_  */
    PRIVATESCOPE_ = 370,           /* PRIVATESCOPE_  */
    HIDEBYSIG_ = 371,              /* HIDEBYSIG_  */
    NEWSLOT_ = 372,                /* NEWSLOT_  */
    RTSPECIALNAME_ = 373,          /* RTSPECIALNAME_  */
    PINVOKEIMPL_ = 374,            /* PINVOKEIMPL_  */
    _CTOR = 375,                   /* _CTOR  */
    _CCTOR = 376,                  /* _CCTOR  */
    LITERAL_ = 377,                /* LITERAL_  */
    NOTSERIALIZED_ = 378,          /* NOTSERIALIZED_  */
    INITONLY_ = 379,               /* INITONLY_  */
    REQSECOBJ_ = 380,              /* REQSECOBJ_  */
    CIL_ = 381,                    /* CIL_  */
    OPTIL_ = 382,                  /* OPTIL_  */
    MANAGED_ = 383,                /* MANAGED_  */
    FORWARDREF_ = 384,             /* FORWARDREF_  */
    PRESERVESIG_ = 385,            /* PRESERVESIG_  */
    RUNTIME_ = 386,                /* RUNTIME_  */
    INTERNALCALL_ = 387,           /* INTERNALCALL_  */
    _IMPORT = 388,                 /* _IMPORT  */
    NOMANGLE_ = 389,               /* NOMANGLE_  */
    LASTERR_ = 390,                /* LASTERR_  */
    WINAPI_ = 391,                 /* WINAPI_  */
    AS_ = 392,                     /* AS_  */
    BESTFIT_ = 393,                /* BESTFIT_  */
    ON_ = 394,                     /* ON_  */
    OFF_ = 395,                    /* OFF_  */
    CHARMAPERROR_ = 396,           /* CHARMAPERROR_  */
    INSTR_NONE = 397,              /* INSTR_NONE  */
    INSTR_VAR = 398,               /* INSTR_VAR  */
    INSTR_I = 399,                 /* INSTR_I  */
    INSTR_I8 = 400,                /* INSTR_I8  */
    INSTR_R = 401,                 /* INSTR_R  */
    INSTR_BRTARGET = 402,          /* INSTR_BRTARGET  */
    INSTR_METHOD = 403,            /* INSTR_METHOD  */
    INSTR_FIELD = 404,             /* INSTR_FIELD  */
    INSTR_TYPE = 405,              /* INSTR_TYPE  */
    INSTR_STRING = 406,            /* INSTR_STRING  */
    INSTR_SIG = 407,               /* INSTR_SIG  */
    INSTR_TOK = 408,               /* INSTR_TOK  */
    INSTR_SWITCH = 409,            /* INSTR_SWITCH  */
    _CLASS = 410,                  /* _CLASS  */
    _NAMESPACE = 411,              /* _NAMESPACE  */
    _METHOD = 412,                 /* _METHOD  */
    _FIELD = 413,                  /* _FIELD  */
    _DATA = 414,                   /* _DATA  */
    _THIS = 415,                   /* _THIS  */
    _BASE = 416,                   /* _BASE  */
    _NESTER = 417,                 /* _NESTER  */
    _EMITBYTE = 418,               /* _EMITBYTE  */
    _TRY = 419,                    /* _TRY  */
    _MAXSTACK = 420,               /* _MAXSTACK  */
    _LOCALS = 421,                 /* _LOCALS  */
    _ENTRYPOINT = 422,             /* _ENTRYPOINT  */
    _ZEROINIT = 423,               /* _ZEROINIT  */
    _EVENT = 424,                  /* _EVENT  */
    _ADDON = 425,                  /* _ADDON  */
    _REMOVEON = 426,               /* _REMOVEON  */
    _FIRE = 427,                   /* _FIRE  */
    _OTHER = 428,                  /* _OTHER  */
    _PROPERTY = 429,               /* _PROPERTY  */
    _SET = 430,                    /* _SET  */
    _GET = 431,                    /* _GET  */
    _PERMISSION = 432,             /* _PERMISSION  */
    _PERMISSIONSET = 433,          /* _PERMISSIONSET  */
    REQUEST_ = 434,                /* REQUEST_  */
    DEMAND_ = 435,                 /* DEMAND_  */
    ASSERT_ = 436,                 /* ASSERT_  */
    DENY_ = 437,                   /* DENY_  */
    PERMITONLY_ = 438,             /* PERMITONLY_  */
    LINKCHECK_ = 439,              /* LINKCHECK_  */
    INHERITCHECK_ = 440,           /* INHERITCHECK_  */
    REQMIN_ = 441,                 /* REQMIN_  */
    REQOPT_ = 442,                 /* REQOPT_  */
    REQREFUSE_ = 443,              /* REQREFUSE_  */
    PREJITGRANT_ = 444,            /* PREJITGRANT_  */
    PREJITDENY_ = 445,             /* PREJITDENY_  */
    NONCASDEMAND_ = 446,           /* NONCASDEMAND_  */
    NONCASLINKDEMAND_ = 447,       /* NONCASLINKDEMAND_  */
    NONCASINHERITANCE_ = 448,      /* NONCASINHERITANCE_  */
    _LINE = 449,                   /* _LINE  */
    P_LINE = 450,                  /* P_LINE  */
    _LANGUAGE = 451,               /* _LANGUAGE  */
    _CUSTOM = 452,                 /* _CUSTOM  */
    INIT_ = 453,                   /* INIT_  */
    _SIZE = 454,                   /* _SIZE  */
    _PACK = 455,                   /* _PACK  */
    _VTABLE = 456,                 /* _VTABLE  */
    _VTFIXUP = 457,                /* _VTFIXUP  */
    FROMUNMANAGED_ = 458,          /* FROMUNMANAGED_  */
    CALLMOSTDERIVED_ = 459,        /* CALLMOSTDERIVED_  */
    _VTENTRY = 460,                /* _VTENTRY  */
    RETAINAPPDOMAIN_ = 461,        /* RETAINAPPDOMAIN_  */
    _FILE = 462,                   /* _FILE  */
    NOMETADATA_ = 463,             /* NOMETADATA_  */
    _HASH = 464,                   /* _HASH  */
    _ASSEMBLY = 465,               /* _ASSEMBLY  */
    _PUBLICKEY = 466,              /* _PUBLICKEY  */
    _PUBLICKEYTOKEN = 467,         /* _PUBLICKEYTOKEN  */
    ALGORITHM_ = 468,              /* ALGORITHM_  */
    _VER = 469,                    /* _VER  */
    _LOCALE = 470,                 /* _LOCALE  */
    EXTERN_ = 471,                 /* EXTERN_  */
    _MRESOURCE = 472,              /* _MRESOURCE  */
    _MODULE = 473,                 /* _MODULE  */
    _EXPORT = 474,                 /* _EXPORT  */
    LEGACY_ = 475,                 /* LEGACY_  */
    LIBRARY_ = 476,                /* LIBRARY_  */
    X86_ = 477,                    /* X86_  */
    AMD64_ = 478,                  /* AMD64_  */
    ARM_ = 479,                    /* ARM_  */
    ARM64_ = 480,                  /* ARM64_  */
    MARSHAL_ = 481,                /* MARSHAL_  */
    CUSTOM_ = 482,                 /* CUSTOM_  */
    SYSSTRING_ = 483,              /* SYSSTRING_  */
    FIXED_ = 484,                  /* FIXED_  */
    VARIANT_ = 485,                /* VARIANT_  */
    CURRENCY_ = 486,               /* CURRENCY_  */
    SYSCHAR_ = 487,                /* SYSCHAR_  */
    DECIMAL_ = 488,                /* DECIMAL_  */
    DATE_ = 489,                   /* DATE_  */
    BSTR_ = 490,                   /* BSTR_  */
    TBSTR_ = 491,                  /* TBSTR_  */
    LPSTR_ = 492,                  /* LPSTR_  */
    LPWSTR_ = 493,                 /* LPWSTR_  */
    LPTSTR_ = 494,                 /* LPTSTR_  */
    OBJECTREF_ = 495,              /* OBJECTREF_  */
    IUNKNOWN_ = 496,               /* IUNKNOWN_  */
    IDISPATCH_ = 497,              /* IDISPATCH_  */
    STRUCT_ = 498,                 /* STRUCT_  */
    SAFEARRAY_ = 499,              /* SAFEARRAY_  */
    BYVALSTR_ = 500,               /* BYVALSTR_  */
    LPVOID_ = 501,                 /* LPVOID_  */
    ANY_ = 502,                    /* ANY_  */
    ARRAY_ = 503,                  /* ARRAY_  */
    LPSTRUCT_ = 504,               /* LPSTRUCT_  */
    IIDPARAM_ = 505,               /* IIDPARAM_  */
    IN_ = 506,                     /* IN_  */
    OUT_ = 507,                    /* OUT_  */
    OPT_ = 508,                    /* OPT_  */
    _PARAM = 509,                  /* _PARAM  */
    _OVERRIDE = 510,               /* _OVERRIDE  */
    WITH_ = 511,                   /* WITH_  */
    NULL_ = 512,                   /* NULL_  */
    HRESULT_ = 513,                /* HRESULT_  */
    CARRAY_ = 514,                 /* CARRAY_  */
    USERDEFINED_ = 515,            /* USERDEFINED_  */
    RECORD_ = 516,                 /* RECORD_  */
    FILETIME_ = 517,               /* FILETIME_  */
    BLOB_ = 518,                   /* BLOB_  */
    STREAM_ = 519,                 /* STREAM_  */
    STORAGE_ = 520,                /* STORAGE_  */
    STREAMED_OBJECT_ = 521,        /* STREAMED_OBJECT_  */
    STORED_OBJECT_ = 522,          /* STORED_OBJECT_  */
    BLOB_OBJECT_ = 523,            /* BLOB_OBJECT_  */
    CF_ = 524,                     /* CF_  */
    CLSID_ = 525,                  /* CLSID_  */
    VECTOR_ = 526,                 /* VECTOR_  */
    _SUBSYSTEM = 527,              /* _SUBSYSTEM  */
    _CORFLAGS = 528,               /* _CORFLAGS  */
    ALIGNMENT_ = 529,              /* ALIGNMENT_  */
    _IMAGEBASE = 530,              /* _IMAGEBASE  */
    _STACKRESERVE = 531,           /* _STACKRESERVE  */
    _TYPEDEF = 532,                /* _TYPEDEF  */
    _TEMPLATE = 533,               /* _TEMPLATE  */
    _TYPELIST = 534,               /* _TYPELIST  */
    _MSCORLIB = 535,               /* _MSCORLIB  */
    P_DEFINE = 536,                /* P_DEFINE  */
    P_UNDEF = 537,                 /* P_UNDEF  */
    P_IFDEF = 538,                 /* P_IFDEF  */
    P_IFNDEF = 539,                /* P_IFNDEF  */
    P_ELSE = 540,                  /* P_ELSE  */
    P_ENDIF = 541,                 /* P_ENDIF  */
    P_INCLUDE = 542,               /* P_INCLUDE  */
    CONSTRAINT_ = 543,             /* CONSTRAINT_  */
    CONST_ = 544                   /* CONST_  */
  };
  typedef enum yytokentype yytoken_kind_t;
#endif

/* Value type.  */
#if ! defined YYSTYPE && ! defined YYSTYPE_IS_DECLARED
union YYSTYPE
{
#line 15 "./asmparse.y"

        CorRegTypeAttr classAttr;
        CorMethodAttr methAttr;
        CorFieldAttr fieldAttr;
        CorMethodImpl implAttr;
        CorEventAttr  eventAttr;
        CorPropertyAttr propAttr;
        CorPinvokeMap pinvAttr;
        CorDeclSecurity secAct;
        CorFileFlags fileAttr;
        CorAssemblyFlags asmAttr;
        CorAssemblyFlags asmRefAttr;
        CorTypeAttr exptAttr;
        CorManifestResourceFlags manresAttr;
        double*  float64;
        __int64* int64;
        __int32  int32;
        char*    string;
        BinStr*  binstr;
        Labels*  labels;
        Instr*   instr;         // instruction opcode
        NVPair*  pair;
        pTyParList typarlist;
        mdToken token;
        TypeDefDescr* tdd;
        CustomDescr*  cad;
        unsigned short opcode;

#line 450 "asmparse.cpp"

};
typedef union YYSTYPE YYSTYPE;
# define YYSTYPE_IS_TRIVIAL 1
# define YYSTYPE_IS_DECLARED 1
#endif


extern YYSTYPE yylval;


int yyparse (void);



/* Symbol kind.  */
enum yysymbol_kind_t
{
  YYSYMBOL_YYEMPTY = -2,
  YYSYMBOL_YYEOF = 0,                      /* "end of file"  */
  YYSYMBOL_YYerror = 1,                    /* error  */
  YYSYMBOL_YYUNDEF = 2,                    /* "invalid token"  */
  YYSYMBOL_ERROR_ = 3,                     /* ERROR_  */
  YYSYMBOL_BAD_COMMENT_ = 4,               /* BAD_COMMENT_  */
  YYSYMBOL_BAD_LITERAL_ = 5,               /* BAD_LITERAL_  */
  YYSYMBOL_ID = 6,                         /* ID  */
  YYSYMBOL_DOTTEDNAME = 7,                 /* DOTTEDNAME  */
  YYSYMBOL_QSTRING = 8,                    /* QSTRING  */
  YYSYMBOL_SQSTRING = 9,                   /* SQSTRING  */
  YYSYMBOL_INT32_T = 10,                   /* INT32_T  */
  YYSYMBOL_INT64_T = 11,                   /* INT64_T  */
  YYSYMBOL_FLOAT64 = 12,                   /* FLOAT64  */
  YYSYMBOL_HEXBYTE = 13,                   /* HEXBYTE  */
  YYSYMBOL_TYPEDEF_T = 14,                 /* TYPEDEF_T  */
  YYSYMBOL_TYPEDEF_M = 15,                 /* TYPEDEF_M  */
  YYSYMBOL_TYPEDEF_F = 16,                 /* TYPEDEF_F  */
  YYSYMBOL_TYPEDEF_TS = 17,                /* TYPEDEF_TS  */
  YYSYMBOL_TYPEDEF_MR = 18,                /* TYPEDEF_MR  */
  YYSYMBOL_TYPEDEF_CA = 19,                /* TYPEDEF_CA  */
  YYSYMBOL_DCOLON = 20,                    /* DCOLON  */
  YYSYMBOL_ELLIPSIS = 21,                  /* ELLIPSIS  */
  YYSYMBOL_VOID_ = 22,                     /* VOID_  */
  YYSYMBOL_BOOL_ = 23,                     /* BOOL_  */
  YYSYMBOL_CHAR_ = 24,                     /* CHAR_  */
  YYSYMBOL_UNSIGNED_ = 25,                 /* UNSIGNED_  */
  YYSYMBOL_INT_ = 26,                      /* INT_  */
  YYSYMBOL_INT8_ = 27,                     /* INT8_  */
  YYSYMBOL_INT16_ = 28,                    /* INT16_  */
  YYSYMBOL_INT32_ = 29,                    /* INT32_  */
  YYSYMBOL_INT64_ = 30,                    /* INT64_  */
  YYSYMBOL_FLOAT_ = 31,                    /* FLOAT_  */
  YYSYMBOL_FLOAT32_ = 32,                  /* FLOAT32_  */
  YYSYMBOL_FLOAT64_ = 33,                  /* FLOAT64_  */
  YYSYMBOL_BYTEARRAY_ = 34,                /* BYTEARRAY_  */
  YYSYMBOL_UINT_ = 35,                     /* UINT_  */
  YYSYMBOL_UINT8_ = 36,                    /* UINT8_  */
  YYSYMBOL_UINT16_ = 37,                   /* UINT16_  */
  YYSYMBOL_UINT32_ = 38,                   /* UINT32_  */
  YYSYMBOL_UINT64_ = 39,                   /* UINT64_  */
  YYSYMBOL_FLAGS_ = 40,                    /* FLAGS_  */
  YYSYMBOL_CALLCONV_ = 41,                 /* CALLCONV_  */
  YYSYMBOL_MDTOKEN_ = 42,                  /* MDTOKEN_  */
  YYSYMBOL_OBJECT_ = 43,                   /* OBJECT_  */
  YYSYMBOL_STRING_ = 44,                   /* STRING_  */
  YYSYMBOL_NULLREF_ = 45,                  /* NULLREF_  */
  YYSYMBOL_DEFAULT_ = 46,                  /* DEFAULT_  */
  YYSYMBOL_CDECL_ = 47,                    /* CDECL_  */
  YYSYMBOL_VARARG_ = 48,                   /* VARARG_  */
  YYSYMBOL_STDCALL_ = 49,                  /* STDCALL_  */
  YYSYMBOL_THISCALL_ = 50,                 /* THISCALL_  */
  YYSYMBOL_FASTCALL_ = 51,                 /* FASTCALL_  */
  YYSYMBOL_CLASS_ = 52,                    /* CLASS_  */
  YYSYMBOL_BYREFLIKE_ = 53,                /* BYREFLIKE_  */
  YYSYMBOL_TYPEDREF_ = 54,                 /* TYPEDREF_  */
  YYSYMBOL_UNMANAGED_ = 55,                /* UNMANAGED_  */
  YYSYMBOL_FINALLY_ = 56,                  /* FINALLY_  */
  YYSYMBOL_HANDLER_ = 57,                  /* HANDLER_  */
  YYSYMBOL_CATCH_ = 58,                    /* CATCH_  */
  YYSYMBOL_FILTER_ = 59,                   /* FILTER_  */
  YYSYMBOL_FAULT_ = 60,                    /* FAULT_  */
  YYSYMBOL_EXTENDS_ = 61,                  /* EXTENDS_  */
  YYSYMBOL_IMPLEMENTS_ = 62,               /* IMPLEMENTS_  */
  YYSYMBOL_TO_ = 63,                       /* TO_  */
  YYSYMBOL_AT_ = 64,                       /* AT_  */
  YYSYMBOL_TLS_ = 65,                      /* TLS_  */
  YYSYMBOL_TRUE_ = 66,                     /* TRUE_  */
  YYSYMBOL_FALSE_ = 67,                    /* FALSE_  */
  YYSYMBOL__INTERFACEIMPL = 68,            /* _INTERFACEIMPL  */
  YYSYMBOL_VALUE_ = 69,                    /* VALUE_  */
  YYSYMBOL_VALUETYPE_ = 70,                /* VALUETYPE_  */
  YYSYMBOL_NATIVE_ = 71,                   /* NATIVE_  */
  YYSYMBOL_INSTANCE_ = 72,                 /* INSTANCE_  */
  YYSYMBOL_SPECIALNAME_ = 73,              /* SPECIALNAME_  */
  YYSYMBOL_FORWARDER_ = 74,                /* FORWARDER_  */
  YYSYMBOL_STATIC_ = 75,                   /* STATIC_  */
  YYSYMBOL_PUBLIC_ = 76,                   /* PUBLIC_  */
  YYSYMBOL_PRIVATE_ = 77,                  /* PRIVATE_  */
  YYSYMBOL_FAMILY_ = 78,                   /* FAMILY_  */
  YYSYMBOL_FINAL_ = 79,                    /* FINAL_  */
  YYSYMBOL_SYNCHRONIZED_ = 80,             /* SYNCHRONIZED_  */
  YYSYMBOL_INTERFACE_ = 81,                /* INTERFACE_  */
  YYSYMBOL_SEALED_ = 82,                   /* SEALED_  */
  YYSYMBOL_NESTED_ = 83,                   /* NESTED_  */
  YYSYMBOL_ABSTRACT_ = 84,                 /* ABSTRACT_  */
  YYSYMBOL_AUTO_ = 85,                     /* AUTO_  */
  YYSYMBOL_SEQUENTIAL_ = 86,               /* SEQUENTIAL_  */
  YYSYMBOL_EXPLICIT_ = 87,                 /* EXPLICIT_  */
  YYSYMBOL_ANSI_ = 88,                     /* ANSI_  */
  YYSYMBOL_UNICODE_ = 89,                  /* UNICODE_  */
  YYSYMBOL_AUTOCHAR_ = 90,                 /* AUTOCHAR_  */
  YYSYMBOL_IMPORT_ = 91,                   /* IMPORT_  */
  YYSYMBOL_ENUM_ = 92,                     /* ENUM_  */
  YYSYMBOL_VIRTUAL_ = 93,                  /* VIRTUAL_  */
  YYSYMBOL_NOINLINING_ = 94,               /* NOINLINING_  */
  YYSYMBOL_AGGRESSIVEINLINING_ = 95,       /* AGGRESSIVEINLINING_  */
  YYSYMBOL_NOOPTIMIZATION_ = 96,           /* NOOPTIMIZATION_  */
  YYSYMBOL_AGGRESSIVEOPTIMIZATION_ = 97,   /* AGGRESSIVEOPTIMIZATION_  */
  YYSYMBOL_UNMANAGEDEXP_ = 98,             /* UNMANAGEDEXP_  */
  YYSYMBOL_BEFOREFIELDINIT_ = 99,          /* BEFOREFIELDINIT_  */
  YYSYMBOL_STRICT_ = 100,                  /* STRICT_  */
  YYSYMBOL_RETARGETABLE_ = 101,            /* RETARGETABLE_  */
  YYSYMBOL_WINDOWSRUNTIME_ = 102,          /* WINDOWSRUNTIME_  */
  YYSYMBOL_NOPLATFORM_ = 103,              /* NOPLATFORM_  */
  YYSYMBOL_METHOD_ = 104,                  /* METHOD_  */
  YYSYMBOL_FIELD_ = 105,                   /* FIELD_  */
  YYSYMBOL_PINNED_ = 106,                  /* PINNED_  */
  YYSYMBOL_MODREQ_ = 107,                  /* MODREQ_  */
  YYSYMBOL_MODOPT_ = 108,                  /* MODOPT_  */
  YYSYMBOL_SERIALIZABLE_ = 109,            /* SERIALIZABLE_  */
  YYSYMBOL_PROPERTY_ = 110,                /* PROPERTY_  */
  YYSYMBOL_TYPE_ = 111,                    /* TYPE_  */
  YYSYMBOL_ASSEMBLY_ = 112,                /* ASSEMBLY_  */
  YYSYMBOL_FAMANDASSEM_ = 113,             /* FAMANDASSEM_  */
  YYSYMBOL_FAMORASSEM_ = 114,              /* FAMORASSEM_  */
  YYSYMBOL_PRIVATESCOPE_ = 115,            /* PRIVATESCOPE_  */
  YYSYMBOL_HIDEBYSIG_ = 116,               /* HIDEBYSIG_  */
  YYSYMBOL_NEWSLOT_ = 117,                 /* NEWSLOT_  */
  YYSYMBOL_RTSPECIALNAME_ = 118,           /* RTSPECIALNAME_  */
  YYSYMBOL_PINVOKEIMPL_ = 119,             /* PINVOKEIMPL_  */
  YYSYMBOL__CTOR = 120,                    /* _CTOR  */
  YYSYMBOL__CCTOR = 121,                   /* _CCTOR  */
  YYSYMBOL_LITERAL_ = 122,                 /* LITERAL_  */
  YYSYMBOL_NOTSERIALIZED_ = 123,           /* NOTSERIALIZED_  */
  YYSYMBOL_INITONLY_ = 124,                /* INITONLY_  */
  YYSYMBOL_REQSECOBJ_ = 125,               /* REQSECOBJ_  */
  YYSYMBOL_CIL_ = 126,                     /* CIL_  */
  YYSYMBOL_OPTIL_ = 127,                   /* OPTIL_  */
  YYSYMBOL_MANAGED_ = 128,                 /* MANAGED_  */
  YYSYMBOL_FORWARDREF_ = 129,              /* FORWARDREF_  */
  YYSYMBOL_PRESERVESIG_ = 130,             /* PRESERVESIG_  */
  YYSYMBOL_RUNTIME_ = 131,                 /* RUNTIME_  */
  YYSYMBOL_INTERNALCALL_ = 132,            /* INTERNALCALL_  */
  YYSYMBOL__IMPORT = 133,                  /* _IMPORT  */
  YYSYMBOL_NOMANGLE_ = 134,                /* NOMANGLE_  */
  YYSYMBOL_LASTERR_ = 135,                 /* LASTERR_  */
  YYSYMBOL_WINAPI_ = 136,                  /* WINAPI_  */
  YYSYMBOL_AS_ = 137,                      /* AS_  */
  YYSYMBOL_BESTFIT_ = 138,                 /* BESTFIT_  */
  YYSYMBOL_ON_ = 139,                      /* ON_  */
  YYSYMBOL_OFF_ = 140,                     /* OFF_  */
  YYSYMBOL_CHARMAPERROR_ = 141,            /* CHARMAPERROR_  */
  YYSYMBOL_INSTR_NONE = 142,               /* INSTR_NONE  */
  YYSYMBOL_INSTR_VAR = 143,                /* INSTR_VAR  */
  YYSYMBOL_INSTR_I = 144,                  /* INSTR_I  */
  YYSYMBOL_INSTR_I8 = 145,                 /* INSTR_I8  */
  YYSYMBOL_INSTR_R = 146,                  /* INSTR_R  */
  YYSYMBOL_INSTR_BRTARGET = 147,           /* INSTR_BRTARGET  */
  YYSYMBOL_INSTR_METHOD = 148,             /* INSTR_METHOD  */
  YYSYMBOL_INSTR_FIELD = 149,              /* INSTR_FIELD  */
  YYSYMBOL_INSTR_TYPE = 150,               /* INSTR_TYPE  */
  YYSYMBOL_INSTR_STRING = 151,             /* INSTR_STRING  */
  YYSYMBOL_INSTR_SIG = 152,                /* INSTR_SIG  */
  YYSYMBOL_INSTR_TOK = 153,                /* INSTR_TOK  */
  YYSYMBOL_INSTR_SWITCH = 154,             /* INSTR_SWITCH  */
  YYSYMBOL__CLASS = 155,                   /* _CLASS  */
  YYSYMBOL__NAMESPACE = 156,               /* _NAMESPACE  */
  YYSYMBOL__METHOD = 157,                  /* _METHOD  */
  YYSYMBOL__FIELD = 158,                   /* _FIELD  */
  YYSYMBOL__DATA = 159,                    /* _DATA  */
  YYSYMBOL__THIS = 160,                    /* _THIS  */
  YYSYMBOL__BASE = 161,                    /* _BASE  */
  YYSYMBOL__NESTER = 162,                  /* _NESTER  */
  YYSYMBOL__EMITBYTE = 163,                /* _EMITBYTE  */
  YYSYMBOL__TRY = 164,                     /* _TRY  */
  YYSYMBOL__MAXSTACK = 165,                /* _MAXSTACK  */
  YYSYMBOL__LOCALS = 166,                  /* _LOCALS  */
  YYSYMBOL__ENTRYPOINT = 167,              /* _ENTRYPOINT  */
  YYSYMBOL__ZEROINIT = 168,                /* _ZEROINIT  */
  YYSYMBOL__EVENT = 169,                   /* _EVENT  */
  YYSYMBOL__ADDON = 170,                   /* _ADDON  */
  YYSYMBOL__REMOVEON = 171,                /* _REMOVEON  */
  YYSYMBOL__FIRE = 172,                    /* _FIRE  */
  YYSYMBOL__OTHER = 173,                   /* _OTHER  */
  YYSYMBOL__PROPERTY = 174,                /* _PROPERTY  */
  YYSYMBOL__SET = 175,                     /* _SET  */
  YYSYMBOL__GET = 176,                     /* _GET  */
  YYSYMBOL__PERMISSION = 177,              /* _PERMISSION  */
  YYSYMBOL__PERMISSIONSET = 178,           /* _PERMISSIONSET  */
  YYSYMBOL_REQUEST_ = 179,                 /* REQUEST_  */
  YYSYMBOL_DEMAND_ = 180,                  /* DEMAND_  */
  YYSYMBOL_ASSERT_ = 181,                  /* ASSERT_  */
  YYSYMBOL_DENY_ = 182,                    /* DENY_  */
  YYSYMBOL_PERMITONLY_ = 183,              /* PERMITONLY_  */
  YYSYMBOL_LINKCHECK_ = 184,               /* LINKCHECK_  */
  YYSYMBOL_INHERITCHECK_ = 185,            /* INHERITCHECK_  */
  YYSYMBOL_REQMIN_ = 186,                  /* REQMIN_  */
  YYSYMBOL_REQOPT_ = 187,                  /* REQOPT_  */
  YYSYMBOL_REQREFUSE_ = 188,               /* REQREFUSE_  */
  YYSYMBOL_PREJITGRANT_ = 189,             /* PREJITGRANT_  */
  YYSYMBOL_PREJITDENY_ = 190,              /* PREJITDENY_  */
  YYSYMBOL_NONCASDEMAND_ = 191,            /* NONCASDEMAND_  */
  YYSYMBOL_NONCASLINKDEMAND_ = 192,        /* NONCASLINKDEMAND_  */
  YYSYMBOL_NONCASINHERITANCE_ = 193,       /* NONCASINHERITANCE_  */
  YYSYMBOL__LINE = 194,                    /* _LINE  */
  YYSYMBOL_P_LINE = 195,                   /* P_LINE  */
  YYSYMBOL__LANGUAGE = 196,                /* _LANGUAGE  */
  YYSYMBOL__CUSTOM = 197,                  /* _CUSTOM  */
  YYSYMBOL_INIT_ = 198,                    /* INIT_  */
  YYSYMBOL__SIZE = 199,                    /* _SIZE  */
  YYSYMBOL__PACK = 200,                    /* _PACK  */
  YYSYMBOL__VTABLE = 201,                  /* _VTABLE  */
  YYSYMBOL__VTFIXUP = 202,                 /* _VTFIXUP  */
  YYSYMBOL_FROMUNMANAGED_ = 203,           /* FROMUNMANAGED_  */
  YYSYMBOL_CALLMOSTDERIVED_ = 204,         /* CALLMOSTDERIVED_  */
  YYSYMBOL__VTENTRY = 205,                 /* _VTENTRY  */
  YYSYMBOL_RETAINAPPDOMAIN_ = 206,         /* RETAINAPPDOMAIN_  */
  YYSYMBOL__FILE = 207,                    /* _FILE  */
  YYSYMBOL_NOMETADATA_ = 208,              /* NOMETADATA_  */
  YYSYMBOL__HASH = 209,                    /* _HASH  */
  YYSYMBOL__ASSEMBLY = 210,                /* _ASSEMBLY  */
  YYSYMBOL__PUBLICKEY = 211,               /* _PUBLICKEY  */
  YYSYMBOL__PUBLICKEYTOKEN = 212,          /* _PUBLICKEYTOKEN  */
  YYSYMBOL_ALGORITHM_ = 213,               /* ALGORITHM_  */
  YYSYMBOL__VER = 214,                     /* _VER  */
  YYSYMBOL__LOCALE = 215,                  /* _LOCALE  */
  YYSYMBOL_EXTERN_ = 216,                  /* EXTERN_  */
  YYSYMBOL__MRESOURCE = 217,               /* _MRESOURCE  */
  YYSYMBOL__MODULE = 218,                  /* _MODULE  */
  YYSYMBOL__EXPORT = 219,                  /* _EXPORT  */
  YYSYMBOL_LEGACY_ = 220,                  /* LEGACY_  */
  YYSYMBOL_LIBRARY_ = 221,                 /* LIBRARY_  */
  YYSYMBOL_X86_ = 222,                     /* X86_  */
  YYSYMBOL_AMD64_ = 223,                   /* AMD64_  */
  YYSYMBOL_ARM_ = 224,                     /* ARM_  */
  YYSYMBOL_ARM64_ = 225,                   /* ARM64_  */
  YYSYMBOL_MARSHAL_ = 226,                 /* MARSHAL_  */
  YYSYMBOL_CUSTOM_ = 227,                  /* CUSTOM_  */
  YYSYMBOL_SYSSTRING_ = 228,               /* SYSSTRING_  */
  YYSYMBOL_FIXED_ = 229,                   /* FIXED_  */
  YYSYMBOL_VARIANT_ = 230,                 /* VARIANT_  */
  YYSYMBOL_CURRENCY_ = 231,                /* CURRENCY_  */
  YYSYMBOL_SYSCHAR_ = 232,                 /* SYSCHAR_  */
  YYSYMBOL_DECIMAL_ = 233,                 /* DECIMAL_  */
  YYSYMBOL_DATE_ = 234,                    /* DATE_  */
  YYSYMBOL_BSTR_ = 235,                    /* BSTR_  */
  YYSYMBOL_TBSTR_ = 236,                   /* TBSTR_  */
  YYSYMBOL_LPSTR_ = 237,                   /* LPSTR_  */
  YYSYMBOL_LPWSTR_ = 238,                  /* LPWSTR_  */
  YYSYMBOL_LPTSTR_ = 239,                  /* LPTSTR_  */
  YYSYMBOL_OBJECTREF_ = 240,               /* OBJECTREF_  */
  YYSYMBOL_IUNKNOWN_ = 241,                /* IUNKNOWN_  */
  YYSYMBOL_IDISPATCH_ = 242,               /* IDISPATCH_  */
  YYSYMBOL_STRUCT_ = 243,                  /* STRUCT_  */
  YYSYMBOL_SAFEARRAY_ = 244,               /* SAFEARRAY_  */
  YYSYMBOL_BYVALSTR_ = 245,                /* BYVALSTR_  */
  YYSYMBOL_LPVOID_ = 246,                  /* LPVOID_  */
  YYSYMBOL_ANY_ = 247,                     /* ANY_  */
  YYSYMBOL_ARRAY_ = 248,                   /* ARRAY_  */
  YYSYMBOL_LPSTRUCT_ = 249,                /* LPSTRUCT_  */
  YYSYMBOL_IIDPARAM_ = 250,                /* IIDPARAM_  */
  YYSYMBOL_IN_ = 251,                      /* IN_  */
  YYSYMBOL_OUT_ = 252,                     /* OUT_  */
  YYSYMBOL_OPT_ = 253,                     /* OPT_  */
  YYSYMBOL__PARAM = 254,                   /* _PARAM  */
  YYSYMBOL__OVERRIDE = 255,                /* _OVERRIDE  */
  YYSYMBOL_WITH_ = 256,                    /* WITH_  */
  YYSYMBOL_NULL_ = 257,                    /* NULL_  */
  YYSYMBOL_HRESULT_ = 258,                 /* HRESULT_  */
  YYSYMBOL_CARRAY_ = 259,                  /* CARRAY_  */
  YYSYMBOL_USERDEFINED_ = 260,             /* USERDEFINED_  */
  YYSYMBOL_RECORD_ = 261,                  /* RECORD_  */
  YYSYMBOL_FILETIME_ = 262,                /* FILETIME_  */
  YYSYMBOL_BLOB_ = 263,                    /* BLOB_  */
  YYSYMBOL_STREAM_ = 264,                  /* STREAM_  */
  YYSYMBOL_STORAGE_ = 265,                 /* STORAGE_  */
  YYSYMBOL_STREAMED_OBJECT_ = 266,         /* STREAMED_OBJECT_  */
  YYSYMBOL_STORED_OBJECT_ = 267,           /* STORED_OBJECT_  */
  YYSYMBOL_BLOB_OBJECT_ = 268,             /* BLOB_OBJECT_  */
  YYSYMBOL_CF_ = 269,                      /* CF_  */
  YYSYMBOL_CLSID_ = 270,                   /* CLSID_  */
  YYSYMBOL_VECTOR_ = 271,                  /* VECTOR_  */
  YYSYMBOL__SUBSYSTEM = 272,               /* _SUBSYSTEM  */
  YYSYMBOL__CORFLAGS = 273,                /* _CORFLAGS  */
  YYSYMBOL_ALIGNMENT_ = 274,               /* ALIGNMENT_  */
  YYSYMBOL__IMAGEBASE = 275,               /* _IMAGEBASE  */
  YYSYMBOL__STACKRESERVE = 276,            /* _STACKRESERVE  */
  YYSYMBOL__TYPEDEF = 277,                 /* _TYPEDEF  */
  YYSYMBOL__TEMPLATE = 278,                /* _TEMPLATE  */
  YYSYMBOL__TYPELIST = 279,                /* _TYPELIST  */
  YYSYMBOL__MSCORLIB = 280,                /* _MSCORLIB  */
  YYSYMBOL_P_DEFINE = 281,                 /* P_DEFINE  */
  YYSYMBOL_P_UNDEF = 282,                  /* P_UNDEF  */
  YYSYMBOL_P_IFDEF = 283,                  /* P_IFDEF  */
  YYSYMBOL_P_IFNDEF = 284,                 /* P_IFNDEF  */
  YYSYMBOL_P_ELSE = 285,                   /* P_ELSE  */
  YYSYMBOL_P_ENDIF = 286,                  /* P_ENDIF  */
  YYSYMBOL_P_INCLUDE = 287,                /* P_INCLUDE  */
  YYSYMBOL_CONSTRAINT_ = 288,              /* CONSTRAINT_  */
  YYSYMBOL_CONST_ = 289,                   /* CONST_  */
  YYSYMBOL_290_ = 290,                     /* '{'  */
  YYSYMBOL_291_ = 291,                     /* '}'  */
  YYSYMBOL_292_ = 292,                     /* '+'  */
  YYSYMBOL_293_ = 293,                     /* ','  */
  YYSYMBOL_294_ = 294,                     /* '.'  */
  YYSYMBOL_295_ = 295,                     /* '('  */
  YYSYMBOL_296_ = 296,                     /* ')'  */
  YYSYMBOL_297_ = 297,                     /* ';'  */
  YYSYMBOL_298_ = 298,                     /* '='  */
  YYSYMBOL_299_ = 299,                     /* '['  */
  YYSYMBOL_300_ = 300,                     /* ']'  */
  YYSYMBOL_301_ = 301,                     /* '<'  */
  YYSYMBOL_302_ = 302,                     /* '>'  */
  YYSYMBOL_303_ = 303,                     /* '-'  */
  YYSYMBOL_304_ = 304,                     /* ':'  */
  YYSYMBOL_305_ = 305,                     /* '*'  */
  YYSYMBOL_306_ = 306,                     /* '&'  */
  YYSYMBOL_307_ = 307,                     /* '/'  */
  YYSYMBOL_308_ = 308,                     /* '!'  */
  YYSYMBOL_YYACCEPT = 309,                 /* $accept  */
  YYSYMBOL_decls = 310,                    /* decls  */
  YYSYMBOL_decl = 311,                     /* decl  */
  YYSYMBOL_classNameSeq = 312,             /* classNameSeq  */
  YYSYMBOL_compQstring = 313,              /* compQstring  */
  YYSYMBOL_languageDecl = 314,             /* languageDecl  */
  YYSYMBOL_id = 315,                       /* id  */
  YYSYMBOL_dottedName = 316,               /* dottedName  */
  YYSYMBOL_int32 = 317,                    /* int32  */
  YYSYMBOL_int64 = 318,                    /* int64  */
  YYSYMBOL_float64 = 319,                  /* float64  */
  YYSYMBOL_typedefDecl = 320,              /* typedefDecl  */
  YYSYMBOL_compControl = 321,              /* compControl  */
  YYSYMBOL_customDescr = 322,              /* customDescr  */
  YYSYMBOL_customDescrWithOwner = 323,     /* customDescrWithOwner  */
  YYSYMBOL_customHead = 324,               /* customHead  */
  YYSYMBOL_customHeadWithOwner = 325,      /* customHeadWithOwner  */
  YYSYMBOL_customType = 326,               /* customType  */
  YYSYMBOL_ownerType = 327,                /* ownerType  */
  YYSYMBOL_customBlobDescr = 328,          /* customBlobDescr  */
  YYSYMBOL_customBlobArgs = 329,           /* customBlobArgs  */
  YYSYMBOL_customBlobNVPairs = 330,        /* customBlobNVPairs  */
  YYSYMBOL_fieldOrProp = 331,              /* fieldOrProp  */
  YYSYMBOL_customAttrDecl = 332,           /* customAttrDecl  */
  YYSYMBOL_serializType = 333,             /* serializType  */
  YYSYMBOL_moduleHead = 334,               /* moduleHead  */
  YYSYMBOL_vtfixupDecl = 335,              /* vtfixupDecl  */
  YYSYMBOL_vtfixupAttr = 336,              /* vtfixupAttr  */
  YYSYMBOL_vtableDecl = 337,               /* vtableDecl  */
  YYSYMBOL_vtableHead = 338,               /* vtableHead  */
  YYSYMBOL_nameSpaceHead = 339,            /* nameSpaceHead  */
  YYSYMBOL__class = 340,                   /* _class  */
  YYSYMBOL_classHeadBegin = 341,           /* classHeadBegin  */
  YYSYMBOL_classHead = 342,                /* classHead  */
  YYSYMBOL_classAttr = 343,                /* classAttr  */
  YYSYMBOL_extendsClause = 344,            /* extendsClause  */
  YYSYMBOL_implClause = 345,               /* implClause  */
  YYSYMBOL_classDecls = 346,               /* classDecls  */
  YYSYMBOL_implList = 347,                 /* implList  */
  YYSYMBOL_typeList = 348,                 /* typeList  */
  YYSYMBOL_typeListNotEmpty = 349,         /* typeListNotEmpty  */
  YYSYMBOL_typarsClause = 350,             /* typarsClause  */
  YYSYMBOL_typarAttrib = 351,              /* typarAttrib  */
  YYSYMBOL_typarAttribs = 352,             /* typarAttribs  */
  YYSYMBOL_conTyparAttrib = 353,           /* conTyparAttrib  */
  YYSYMBOL_conTyparAttribs = 354,          /* conTyparAttribs  */
  YYSYMBOL_typars = 355,                   /* typars  */
  YYSYMBOL_typarsRest = 356,               /* typarsRest  */
  YYSYMBOL_tyBound = 357,                  /* tyBound  */
  YYSYMBOL_genArity = 358,                 /* genArity  */
  YYSYMBOL_genArityNotEmpty = 359,         /* genArityNotEmpty  */
  YYSYMBOL_classDecl = 360,                /* classDecl  */
  YYSYMBOL_fieldDecl = 361,                /* fieldDecl  */
  YYSYMBOL_fieldAttr = 362,                /* fieldAttr  */
  YYSYMBOL_atOpt = 363,                    /* atOpt  */
  YYSYMBOL_initOpt = 364,                  /* initOpt  */
  YYSYMBOL_repeatOpt = 365,                /* repeatOpt  */
  YYSYMBOL_methodRef = 366,                /* methodRef  */
  YYSYMBOL_callConv = 367,                 /* callConv  */
  YYSYMBOL_callKind = 368,                 /* callKind  */
  YYSYMBOL_mdtoken = 369,                  /* mdtoken  */
  YYSYMBOL_memberRef = 370,                /* memberRef  */
  YYSYMBOL_eventHead = 371,                /* eventHead  */
  YYSYMBOL_eventAttr = 372,                /* eventAttr  */
  YYSYMBOL_eventDecls = 373,               /* eventDecls  */
  YYSYMBOL_eventDecl = 374,                /* eventDecl  */
  YYSYMBOL_propHead = 375,                 /* propHead  */
  YYSYMBOL_propAttr = 376,                 /* propAttr  */
  YYSYMBOL_propDecls = 377,                /* propDecls  */
  YYSYMBOL_propDecl = 378,                 /* propDecl  */
  YYSYMBOL_methodHeadPart1 = 379,          /* methodHeadPart1  */
  YYSYMBOL_marshalClause = 380,            /* marshalClause  */
  YYSYMBOL_marshalBlob = 381,              /* marshalBlob  */
  YYSYMBOL_marshalBlobHead = 382,          /* marshalBlobHead  */
  YYSYMBOL_methodHead = 383,               /* methodHead  */
  YYSYMBOL_methAttr = 384,                 /* methAttr  */
  YYSYMBOL_pinvAttr = 385,                 /* pinvAttr  */
  YYSYMBOL_methodName = 386,               /* methodName  */
  YYSYMBOL_paramAttr = 387,                /* paramAttr  */
  YYSYMBOL_implAttr = 388,                 /* implAttr  */
  YYSYMBOL_localsHead = 389,               /* localsHead  */
  YYSYMBOL_methodDecls = 390,              /* methodDecls  */
  YYSYMBOL_methodDecl = 391,               /* methodDecl  */
  YYSYMBOL_scopeBlock = 392,               /* scopeBlock  */
  YYSYMBOL_scopeOpen = 393,                /* scopeOpen  */
  YYSYMBOL_sehBlock = 394,                 /* sehBlock  */
  YYSYMBOL_sehClauses = 395,               /* sehClauses  */
  YYSYMBOL_tryBlock = 396,                 /* tryBlock  */
  YYSYMBOL_tryHead = 397,                  /* tryHead  */
  YYSYMBOL_sehClause = 398,                /* sehClause  */
  YYSYMBOL_filterClause = 399,             /* filterClause  */
  YYSYMBOL_filterHead = 400,               /* filterHead  */
  YYSYMBOL_catchClause = 401,              /* catchClause  */
  YYSYMBOL_finallyClause = 402,            /* finallyClause  */
  YYSYMBOL_faultClause = 403,              /* faultClause  */
  YYSYMBOL_handlerBlock = 404,             /* handlerBlock  */
  YYSYMBOL_dataDecl = 405,                 /* dataDecl  */
  YYSYMBOL_ddHead = 406,                   /* ddHead  */
  YYSYMBOL_tls = 407,                      /* tls  */
  YYSYMBOL_ddBody = 408,                   /* ddBody  */
  YYSYMBOL_ddItemList = 409,               /* ddItemList  */
  YYSYMBOL_ddItemCount = 410,              /* ddItemCount  */
  YYSYMBOL_ddItem = 411,                   /* ddItem  */
  YYSYMBOL_fieldSerInit = 412,             /* fieldSerInit  */
  YYSYMBOL_bytearrayhead = 413,            /* bytearrayhead  */
  YYSYMBOL_bytes = 414,                    /* bytes  */
  YYSYMBOL_hexbytes = 415,                 /* hexbytes  */
  YYSYMBOL_fieldInit = 416,                /* fieldInit  */
  YYSYMBOL_serInit = 417,                  /* serInit  */
  YYSYMBOL_f32seq = 418,                   /* f32seq  */
  YYSYMBOL_f64seq = 419,                   /* f64seq  */
  YYSYMBOL_i64seq = 420,                   /* i64seq  */
  YYSYMBOL_i32seq = 421,                   /* i32seq  */
  YYSYMBOL_i16seq = 422,                   /* i16seq  */
  YYSYMBOL_i8seq = 423,                    /* i8seq  */
  YYSYMBOL_boolSeq = 424,                  /* boolSeq  */
  YYSYMBOL_sqstringSeq = 425,              /* sqstringSeq  */
  YYSYMBOL_classSeq = 426,                 /* classSeq  */
  YYSYMBOL_objSeq = 427,                   /* objSeq  */
  YYSYMBOL_methodSpec = 428,               /* methodSpec  */
  YYSYMBOL_instr_none = 429,               /* instr_none  */
  YYSYMBOL_instr_var = 430,                /* instr_var  */
  YYSYMBOL_instr_i = 431,                  /* instr_i  */
  YYSYMBOL_instr_i8 = 432,                 /* instr_i8  */
  YYSYMBOL_instr_r = 433,                  /* instr_r  */
  YYSYMBOL_instr_brtarget = 434,           /* instr_brtarget  */
  YYSYMBOL_instr_method = 435,             /* instr_method  */
  YYSYMBOL_instr_field = 436,              /* instr_field  */
  YYSYMBOL_instr_type = 437,               /* instr_type  */
  YYSYMBOL_instr_string = 438,             /* instr_string  */
  YYSYMBOL_instr_sig = 439,                /* instr_sig  */
  YYSYMBOL_instr_tok = 440,                /* instr_tok  */
  YYSYMBOL_instr_switch = 441,             /* instr_switch  */
  YYSYMBOL_instr_r_head = 442,             /* instr_r_head  */
  YYSYMBOL_instr = 443,                    /* instr  */
  YYSYMBOL_labels = 444,                   /* labels  */
  YYSYMBOL_tyArgs0 = 445,                  /* tyArgs0  */
  YYSYMBOL_tyArgs1 = 446,                  /* tyArgs1  */
  YYSYMBOL_tyArgs2 = 447,                  /* tyArgs2  */
  YYSYMBOL_sigArgs0 = 448,                 /* sigArgs0  */
  YYSYMBOL_sigArgs1 = 449,                 /* sigArgs1  */
  YYSYMBOL_sigArg = 450,                   /* sigArg  */
  YYSYMBOL_className = 451,                /* className  */
  YYSYMBOL_slashedName = 452,              /* slashedName  */
  YYSYMBOL_typeSpec = 453,                 /* typeSpec  */
  YYSYMBOL_nativeType = 454,               /* nativeType  */
  YYSYMBOL_iidParamIndex = 455,            /* iidParamIndex  */
  YYSYMBOL_variantType = 456,              /* variantType  */
  YYSYMBOL_type = 457,                     /* type  */
  YYSYMBOL_simpleType = 458,               /* simpleType  */
  YYSYMBOL_bounds1 = 459,                  /* bounds1  */
  YYSYMBOL_bound = 460,                    /* bound  */
  YYSYMBOL_secDecl = 461,                  /* secDecl  */
  YYSYMBOL_secAttrSetBlob = 462,           /* secAttrSetBlob  */
  YYSYMBOL_secAttrBlob = 463,              /* secAttrBlob  */
  YYSYMBOL_psetHead = 464,                 /* psetHead  */
  YYSYMBOL_nameValPairs = 465,             /* nameValPairs  */
  YYSYMBOL_nameValPair = 466,              /* nameValPair  */
  YYSYMBOL_truefalse = 467,                /* truefalse  */
  YYSYMBOL_caValue = 468,                  /* caValue  */
  YYSYMBOL_secAction = 469,                /* secAction  */
  YYSYMBOL_esHead = 470,                   /* esHead  */
  YYSYMBOL_extSourceSpec = 471,            /* extSourceSpec  */
  YYSYMBOL_fileDecl = 472,                 /* fileDecl  */
  YYSYMBOL_fileAttr = 473,                 /* fileAttr  */
  YYSYMBOL_fileEntry = 474,                /* fileEntry  */
  YYSYMBOL_hashHead = 475,                 /* hashHead  */
  YYSYMBOL_assemblyHead = 476,             /* assemblyHead  */
  YYSYMBOL_asmAttr = 477,                  /* asmAttr  */
  YYSYMBOL_assemblyDecls = 478,            /* assemblyDecls  */
  YYSYMBOL_assemblyDecl = 479,             /* assemblyDecl  */
  YYSYMBOL_intOrWildcard = 480,            /* intOrWildcard  */
  YYSYMBOL_asmOrRefDecl = 481,             /* asmOrRefDecl  */
  YYSYMBOL_publicKeyHead = 482,            /* publicKeyHead  */
  YYSYMBOL_publicKeyTokenHead = 483,       /* publicKeyTokenHead  */
  YYSYMBOL_localeHead = 484,               /* localeHead  */
  YYSYMBOL_assemblyRefHead = 485,          /* assemblyRefHead  */
  YYSYMBOL_assemblyRefDecls = 486,         /* assemblyRefDecls  */
  YYSYMBOL_assemblyRefDecl = 487,          /* assemblyRefDecl  */
  YYSYMBOL_exptypeHead = 488,              /* exptypeHead  */
  YYSYMBOL_exportHead = 489,               /* exportHead  */
  YYSYMBOL_exptAttr = 490,                 /* exptAttr  */
  YYSYMBOL_exptypeDecls = 491,             /* exptypeDecls  */
  YYSYMBOL_exptypeDecl = 492,              /* exptypeDecl  */
  YYSYMBOL_manifestResHead = 493,          /* manifestResHead  */
  YYSYMBOL_manresAttr = 494,               /* manresAttr  */
  YYSYMBOL_manifestResDecls = 495,         /* manifestResDecls  */
  YYSYMBOL_manifestResDecl = 496           /* manifestResDecl  */
};
typedef enum yysymbol_kind_t yysymbol_kind_t;




#ifdef short
# undef short
#endif

/* On compilers that do not define __PTRDIFF_MAX__ etc., make sure
   <limits.h> and (if available) <stdint.h> are included
   so that the code can choose integer types of a good width.  */

#ifndef __PTRDIFF_MAX__
# include <limits.h> /* INFRINGES ON USER NAME SPACE */
# if defined __STDC_VERSION__ && 199901 <= __STDC_VERSION__
#  include <stdint.h> /* INFRINGES ON USER NAME SPACE */
#  define YY_STDINT_H
# endif
#endif

/* Narrow types that promote to a signed type and that can represent a
   signed or unsigned integer of at least N bits.  In tables they can
   save space and decrease cache pressure.  Promoting to a signed type
   helps avoid bugs in integer arithmetic.  */

#ifdef __INT_LEAST8_MAX__
typedef __INT_LEAST8_TYPE__ yytype_int8;
#elif defined YY_STDINT_H
typedef int_least8_t yytype_int8;
#else
typedef signed char yytype_int8;
#endif

#ifdef __INT_LEAST16_MAX__
typedef __INT_LEAST16_TYPE__ yytype_int16;
#elif defined YY_STDINT_H
typedef int_least16_t yytype_int16;
#else
typedef short yytype_int16;
#endif

/* Work around bug in HP-UX 11.23, which defines these macros
   incorrectly for preprocessor constants.  This workaround can likely
   be removed in 2023, as HPE has promised support for HP-UX 11.23
   (aka HP-UX 11i v2) only through the end of 2022; see Table 2 of
   <https://h20195.www2.hpe.com/V2/getpdf.aspx/4AA4-7673ENW.pdf>.  */
#ifdef __hpux
# undef UINT_LEAST8_MAX
# undef UINT_LEAST16_MAX
# define UINT_LEAST8_MAX 255
# define UINT_LEAST16_MAX 65535
#endif

#if defined __UINT_LEAST8_MAX__ && __UINT_LEAST8_MAX__ <= __INT_MAX__
typedef __UINT_LEAST8_TYPE__ yytype_uint8;
#elif (!defined __UINT_LEAST8_MAX__ && defined YY_STDINT_H \
       && UINT_LEAST8_MAX <= INT_MAX)
typedef uint_least8_t yytype_uint8;
#elif !defined __UINT_LEAST8_MAX__ && UCHAR_MAX <= INT_MAX
typedef unsigned char yytype_uint8;
#else
typedef short yytype_uint8;
#endif

#if defined __UINT_LEAST16_MAX__ && __UINT_LEAST16_MAX__ <= __INT_MAX__
typedef __UINT_LEAST16_TYPE__ yytype_uint16;
#elif (!defined __UINT_LEAST16_MAX__ && defined YY_STDINT_H \
       && UINT_LEAST16_MAX <= INT_MAX)
typedef uint_least16_t yytype_uint16;
#elif !defined __UINT_LEAST16_MAX__ && USHRT_MAX <= INT_MAX
typedef unsigned short yytype_uint16;
#else
typedef int yytype_uint16;
#endif

#ifndef YYPTRDIFF_T
# if defined __PTRDIFF_TYPE__ && defined __PTRDIFF_MAX__
#  define YYPTRDIFF_T __PTRDIFF_TYPE__
#  define YYPTRDIFF_MAXIMUM __PTRDIFF_MAX__
# elif defined PTRDIFF_MAX
#  ifndef ptrdiff_t
#   include <stddef.h> /* INFRINGES ON USER NAME SPACE */
#  endif
#  define YYPTRDIFF_T ptrdiff_t
#  define YYPTRDIFF_MAXIMUM PTRDIFF_MAX
# else
#  define YYPTRDIFF_T long
#  define YYPTRDIFF_MAXIMUM LONG_MAX
# endif
#endif

#ifndef YYSIZE_T
# ifdef __SIZE_TYPE__
#  define YYSIZE_T __SIZE_TYPE__
# elif defined size_t
#  define YYSIZE_T size_t
# elif defined __STDC_VERSION__ && 199901 <= __STDC_VERSION__
#  include <stddef.h> /* INFRINGES ON USER NAME SPACE */
#  define YYSIZE_T size_t
# else
#  define YYSIZE_T unsigned
# endif
#endif

#define YYSIZE_MAXIMUM                                  \
  YY_CAST (YYPTRDIFF_T,                                 \
           (YYPTRDIFF_MAXIMUM < YY_CAST (YYSIZE_T, -1)  \
            ? YYPTRDIFF_MAXIMUM                         \
            : YY_CAST (YYSIZE_T, -1)))

#define YYSIZEOF(X) YY_CAST (YYPTRDIFF_T, sizeof (X))


/* Stored state numbers (used for stacks). */
typedef yytype_int16 yy_state_t;

/* State numbers in computations.  */
typedef int yy_state_fast_t;

#ifndef YY_
# if defined YYENABLE_NLS && YYENABLE_NLS
#  if ENABLE_NLS
#   include <libintl.h> /* INFRINGES ON USER NAME SPACE */
#   define YY_(Msgid) dgettext ("bison-runtime", Msgid)
#  endif
# endif
# ifndef YY_
#  define YY_(Msgid) Msgid
# endif
#endif


#ifndef YY_ATTRIBUTE_PURE
# if defined __GNUC__ && 2 < __GNUC__ + (96 <= __GNUC_MINOR__)
#  define YY_ATTRIBUTE_PURE __attribute__ ((__pure__))
# else
#  define YY_ATTRIBUTE_PURE
# endif
#endif

#ifndef YY_ATTRIBUTE_UNUSED
# if defined __GNUC__ && 2 < __GNUC__ + (7 <= __GNUC_MINOR__)
#  define YY_ATTRIBUTE_UNUSED __attribute__ ((__unused__))
# else
#  define YY_ATTRIBUTE_UNUSED
# endif
#endif

/* Suppress unused-variable warnings by "using" E.  */
#if ! defined lint || defined __GNUC__
# define YY_USE(E) ((void) (E))
#else
# define YY_USE(E) /* empty */
#endif

/* Suppress an incorrect diagnostic about yylval being uninitialized.  */
#if defined __GNUC__ && ! defined __ICC && 406 <= __GNUC__ * 100 + __GNUC_MINOR__
# if __GNUC__ * 100 + __GNUC_MINOR__ < 407
#  define YY_IGNORE_MAYBE_UNINITIALIZED_BEGIN                           \
    _Pragma ("GCC diagnostic push")                                     \
    _Pragma ("GCC diagnostic ignored \"-Wuninitialized\"")
# else
#  define YY_IGNORE_MAYBE_UNINITIALIZED_BEGIN                           \
    _Pragma ("GCC diagnostic push")                                     \
    _Pragma ("GCC diagnostic ignored \"-Wuninitialized\"")              \
    _Pragma ("GCC diagnostic ignored \"-Wmaybe-uninitialized\"")
# endif
# define YY_IGNORE_MAYBE_UNINITIALIZED_END      \
    _Pragma ("GCC diagnostic pop")
#else
# define YY_INITIAL_VALUE(Value) Value
#endif
#ifndef YY_IGNORE_MAYBE_UNINITIALIZED_BEGIN
# define YY_IGNORE_MAYBE_UNINITIALIZED_BEGIN
# define YY_IGNORE_MAYBE_UNINITIALIZED_END
#endif
#ifndef YY_INITIAL_VALUE
# define YY_INITIAL_VALUE(Value) /* Nothing. */
#endif

#if defined __cplusplus && defined __GNUC__ && ! defined __ICC && 6 <= __GNUC__
# define YY_IGNORE_USELESS_CAST_BEGIN                          \
    _Pragma ("GCC diagnostic push")                            \
    _Pragma ("GCC diagnostic ignored \"-Wuseless-cast\"")
# define YY_IGNORE_USELESS_CAST_END            \
    _Pragma ("GCC diagnostic pop")
#endif
#ifndef YY_IGNORE_USELESS_CAST_BEGIN
# define YY_IGNORE_USELESS_CAST_BEGIN
# define YY_IGNORE_USELESS_CAST_END
#endif


#define YY_ASSERT(E) ((void) (0 && (E)))

#if !defined yyoverflow

/* The parser invokes alloca or malloc; define the necessary symbols.  */

# ifdef YYSTACK_USE_ALLOCA
#  if YYSTACK_USE_ALLOCA
#   ifdef __GNUC__
#    define YYSTACK_ALLOC __builtin_alloca
#   elif defined __BUILTIN_VA_ARG_INCR
#    include <alloca.h> /* INFRINGES ON USER NAME SPACE */
#   elif defined _AIX
#    define YYSTACK_ALLOC __alloca
#   elif defined _MSC_VER
#    include <malloc.h> /* INFRINGES ON USER NAME SPACE */
#    define alloca _alloca
#   else
#    define YYSTACK_ALLOC alloca
#    if ! defined _ALLOCA_H && ! defined EXIT_SUCCESS
#     include <stdlib.h> /* INFRINGES ON USER NAME SPACE */
      /* Use EXIT_SUCCESS as a witness for stdlib.h.  */
#     ifndef EXIT_SUCCESS
#      define EXIT_SUCCESS 0
#     endif
#    endif
#   endif
#  endif
# endif

# ifdef YYSTACK_ALLOC
   /* Pacify GCC's 'empty if-body' warning.  */
#  define YYSTACK_FREE(Ptr) do { /* empty */; } while (0)
#  ifndef YYSTACK_ALLOC_MAXIMUM
    /* The OS might guarantee only one guard page at the bottom of the stack,
       and a page size can be as small as 4096 bytes.  So we cannot safely
       invoke alloca (N) if N exceeds 4096.  Use a slightly smaller number
       to allow for a few compiler-allocated temporary stack slots.  */
#   define YYSTACK_ALLOC_MAXIMUM 4032 /* reasonable circa 2006 */
#  endif
# else
#  define YYSTACK_ALLOC YYMALLOC
#  define YYSTACK_FREE YYFREE
#  ifndef YYSTACK_ALLOC_MAXIMUM
#   define YYSTACK_ALLOC_MAXIMUM YYSIZE_MAXIMUM
#  endif
#  if (defined __cplusplus && ! defined EXIT_SUCCESS \
       && ! ((defined YYMALLOC || defined malloc) \
             && (defined YYFREE || defined free)))
#   include <stdlib.h> /* INFRINGES ON USER NAME SPACE */
#   ifndef EXIT_SUCCESS
#    define EXIT_SUCCESS 0
#   endif
#  endif
#  ifndef YYMALLOC
#   define YYMALLOC malloc
#   if ! defined malloc && ! defined EXIT_SUCCESS
void *malloc (YYSIZE_T); /* INFRINGES ON USER NAME SPACE */
#   endif
#  endif
#  ifndef YYFREE
#   define YYFREE free
#   if ! defined free && ! defined EXIT_SUCCESS
void free (void *); /* INFRINGES ON USER NAME SPACE */
#   endif
#  endif
# endif
#endif /* !defined yyoverflow */

#if (! defined yyoverflow \
     && (! defined __cplusplus \
         || (defined YYSTYPE_IS_TRIVIAL && YYSTYPE_IS_TRIVIAL)))

/* A type that is properly aligned for any stack member.  */
union yyalloc
{
  yy_state_t yyss_alloc;
  YYSTYPE yyvs_alloc;
};

/* The size of the maximum gap between one aligned stack and the next.  */
# define YYSTACK_GAP_MAXIMUM (YYSIZEOF (union yyalloc) - 1)

/* The size of an array large to enough to hold all stacks, each with
   N elements.  */
# define YYSTACK_BYTES(N) \
     ((N) * (YYSIZEOF (yy_state_t) + YYSIZEOF (YYSTYPE)) \
      + YYSTACK_GAP_MAXIMUM)

# define YYCOPY_NEEDED 1

/* Relocate STACK from its old location to the new one.  The
   local variables YYSIZE and YYSTACKSIZE give the old and new number of
   elements in the stack, and YYPTR gives the new location of the
   stack.  Advance YYPTR to a properly aligned location for the next
   stack.  */
# define YYSTACK_RELOCATE(Stack_alloc, Stack)                           \
    do                                                                  \
      {                                                                 \
        YYPTRDIFF_T yynewbytes;                                         \
        YYCOPY (&yyptr->Stack_alloc, Stack, yysize);                    \
        Stack = &yyptr->Stack_alloc;                                    \
        yynewbytes = yystacksize * YYSIZEOF (*Stack) + YYSTACK_GAP_MAXIMUM; \
        yyptr += yynewbytes / YYSIZEOF (*yyptr);                        \
      }                                                                 \
    while (0)

#endif

#if defined YYCOPY_NEEDED && YYCOPY_NEEDED
/* Copy COUNT objects from SRC to DST.  The source and destination do
   not overlap.  */
# ifndef YYCOPY
#  if defined __GNUC__ && 1 < __GNUC__
#   define YYCOPY(Dst, Src, Count) \
      __builtin_memcpy (Dst, Src, YY_CAST (YYSIZE_T, (Count)) * sizeof (*(Src)))
#  else
#   define YYCOPY(Dst, Src, Count)              \
      do                                        \
        {                                       \
          YYPTRDIFF_T yyi;                      \
          for (yyi = 0; yyi < (Count); yyi++)   \
            (Dst)[yyi] = (Src)[yyi];            \
        }                                       \
      while (0)
#  endif
# endif
#endif /* !YYCOPY_NEEDED */

/* YYFINAL -- State number of the termination state.  */
#define YYFINAL  2
/* YYLAST -- Last index in YYTABLE.  */
#define YYLAST   3842

/* YYNTOKENS -- Number of terminals.  */
#define YYNTOKENS  309
/* YYNNTS -- Number of nonterminals.  */
#define YYNNTS  188
/* YYNRULES -- Number of rules.  */
#define YYNRULES  851
/* YYNSTATES -- Number of states.  */
#define YYNSTATES  1603

/* YYMAXUTOK -- Last valid token kind.  */
#define YYMAXUTOK   544


/* YYTRANSLATE(TOKEN-NUM) -- Symbol number corresponding to TOKEN-NUM
   as returned by yylex, with out-of-bounds checking.  */
#define YYTRANSLATE(YYX)                                \
  (0 <= (YYX) && (YYX) <= YYMAXUTOK                     \
   ? YY_CAST (yysymbol_kind_t, yytranslate[YYX])        \
   : YYSYMBOL_YYUNDEF)

/* YYTRANSLATE[TOKEN-NUM] -- Symbol number corresponding to TOKEN-NUM
   as returned by yylex.  */
static const yytype_int16 yytranslate[] =
{
       0,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,   308,     2,     2,     2,     2,   306,     2,
     295,   296,   305,   292,   293,   303,   294,   307,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,   304,   297,
     301,   298,   302,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,   299,     2,   300,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,   290,     2,   291,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     1,     2,     3,     4,
       5,     6,     7,     8,     9,    10,    11,    12,    13,    14,
      15,    16,    17,    18,    19,    20,    21,    22,    23,    24,
      25,    26,    27,    28,    29,    30,    31,    32,    33,    34,
      35,    36,    37,    38,    39,    40,    41,    42,    43,    44,
      45,    46,    47,    48,    49,    50,    51,    52,    53,    54,
      55,    56,    57,    58,    59,    60,    61,    62,    63,    64,
      65,    66,    67,    68,    69,    70,    71,    72,    73,    74,
      75,    76,    77,    78,    79,    80,    81,    82,    83,    84,
      85,    86,    87,    88,    89,    90,    91,    92,    93,    94,
      95,    96,    97,    98,    99,   100,   101,   102,   103,   104,
     105,   106,   107,   108,   109,   110,   111,   112,   113,   114,
     115,   116,   117,   118,   119,   120,   121,   122,   123,   124,
     125,   126,   127,   128,   129,   130,   131,   132,   133,   134,
     135,   136,   137,   138,   139,   140,   141,   142,   143,   144,
     145,   146,   147,   148,   149,   150,   151,   152,   153,   154,
     155,   156,   157,   158,   159,   160,   161,   162,   163,   164,
     165,   166,   167,   168,   169,   170,   171,   172,   173,   174,
     175,   176,   177,   178,   179,   180,   181,   182,   183,   184,
     185,   186,   187,   188,   189,   190,   191,   192,   193,   194,
     195,   196,   197,   198,   199,   200,   201,   202,   203,   204,
     205,   206,   207,   208,   209,   210,   211,   212,   213,   214,
     215,   216,   217,   218,   219,   220,   221,   222,   223,   224,
     225,   226,   227,   228,   229,   230,   231,   232,   233,   234,
     235,   236,   237,   238,   239,   240,   241,   242,   243,   244,
     245,   246,   247,   248,   249,   250,   251,   252,   253,   254,
     255,   256,   257,   258,   259,   260,   261,   262,   263,   264,
     265,   266,   267,   268,   269,   270,   271,   272,   273,   274,
     275,   276,   277,   278,   279,   280,   281,   282,   283,   284,
     285,   286,   287,   288,   289
};

#if YYDEBUG
/* YYRLINE[YYN] -- Source line where rule number YYN was defined.  */
static const yytype_int16 yyrline[] =
{
       0,   190,   190,   191,   194,   195,   196,   200,   201,   202,
     203,   204,   205,   206,   207,   208,   209,   210,   211,   212,
     213,   223,   224,   227,   230,   231,   232,   233,   234,   235,
     238,   239,   242,   243,   246,   247,   249,   254,   255,   258,
     259,   260,   263,   266,   267,   270,   271,   272,   276,   277,
     278,   279,   280,   285,   286,   287,   288,   291,   294,   295,
     299,   300,   304,   305,   306,   307,   310,   311,   312,   314,
     317,   320,   326,   329,   330,   334,   340,   341,   343,   346,
     347,   353,   356,   357,   360,   364,   365,   373,   374,   375,
     376,   378,   380,   385,   386,   387,   394,   398,   399,   400,
     401,   402,   403,   406,   409,   413,   416,   419,   425,   428,
     429,   430,   431,   432,   433,   434,   435,   436,   437,   438,
     439,   440,   441,   442,   443,   444,   445,   446,   447,   448,
     449,   450,   451,   452,   453,   454,   457,   458,   461,   462,
     465,   466,   469,   470,   474,   475,   478,   479,   482,   483,
     486,   487,   488,   489,   490,   491,   492,   495,   496,   499,
     502,   503,   506,   507,   508,   511,   512,   515,   518,   519,
     522,   526,   530,   531,   532,   533,   534,   535,   536,   537,
     538,   539,   540,   541,   547,   556,   557,   558,   563,   569,
     570,   571,   578,   583,   584,   585,   586,   587,   588,   589,
     590,   602,   604,   605,   606,   607,   608,   609,   610,   613,
     614,   617,   618,   621,   622,   626,   643,   649,   665,   670,
     671,   672,   675,   676,   677,   678,   681,   682,   683,   684,
     685,   686,   687,   688,   691,   694,   699,   703,   707,   709,
     711,   716,   717,   721,   722,   723,   726,   727,   730,   731,
     732,   733,   734,   735,   736,   737,   741,   747,   748,   749,
     752,   753,   757,   758,   759,   760,   761,   762,   763,   767,
     773,   774,   777,   778,   781,   784,   800,   801,   802,   803,
     804,   805,   806,   807,   808,   809,   810,   811,   812,   813,
     814,   815,   816,   817,   818,   819,   820,   823,   826,   831,
     832,   833,   834,   835,   836,   837,   838,   839,   840,   841,
     842,   843,   844,   845,   846,   849,   850,   851,   854,   855,
     856,   857,   858,   861,   862,   863,   864,   865,   866,   867,
     868,   869,   870,   871,   872,   873,   874,   875,   876,   879,
     883,   884,   887,   888,   889,   890,   892,   895,   896,   897,
     898,   899,   900,   901,   902,   903,   904,   905,   915,   925,
     927,   930,   937,   938,   943,   949,   950,   952,   973,   976,
     980,   983,   984,   987,   988,   989,   993,   998,   999,  1000,
    1001,  1005,  1006,  1008,  1012,  1016,  1021,  1025,  1029,  1030,
    1031,  1036,  1039,  1040,  1043,  1044,  1045,  1048,  1049,  1052,
    1053,  1056,  1057,  1062,  1063,  1064,  1065,  1072,  1079,  1086,
    1093,  1101,  1109,  1110,  1111,  1112,  1113,  1114,  1118,  1121,
    1123,  1125,  1127,  1129,  1131,  1133,  1135,  1137,  1139,  1141,
    1143,  1145,  1147,  1149,  1151,  1153,  1155,  1159,  1162,  1163,
    1166,  1167,  1171,  1172,  1173,  1178,  1179,  1180,  1182,  1184,
    1186,  1187,  1188,  1192,  1196,  1200,  1204,  1208,  1212,  1216,
    1220,  1224,  1228,  1232,  1236,  1240,  1244,  1248,  1252,  1256,
    1260,  1267,  1268,  1270,  1274,  1275,  1277,  1281,  1282,  1286,
    1287,  1290,  1291,  1294,  1295,  1298,  1299,  1303,  1304,  1305,
    1309,  1310,  1311,  1313,  1317,  1318,  1322,  1328,  1331,  1334,
    1337,  1340,  1343,  1346,  1354,  1357,  1360,  1363,  1366,  1369,
    1372,  1376,  1377,  1378,  1379,  1380,  1381,  1382,  1383,  1392,
    1393,  1394,  1401,  1409,  1417,  1423,  1429,  1435,  1439,  1440,
    1442,  1444,  1448,  1454,  1457,  1458,  1459,  1460,  1461,  1465,
    1466,  1469,  1470,  1473,  1474,  1478,  1479,  1482,  1483,  1486,
    1487,  1488,  1492,  1493,  1494,  1495,  1496,  1497,  1498,  1499,
    1502,  1508,  1515,  1516,  1519,  1520,  1521,  1522,  1526,  1527,
    1534,  1540,  1542,  1545,  1547,  1548,  1550,  1552,  1553,  1554,
    1555,  1556,  1557,  1558,  1559,  1560,  1561,  1562,  1563,  1564,
    1565,  1566,  1567,  1568,  1570,  1572,  1577,  1582,  1585,  1587,
    1589,  1590,  1591,  1592,  1593,  1595,  1597,  1599,  1600,  1602,
    1605,  1609,  1610,  1611,  1612,  1614,  1615,  1616,  1617,  1618,
    1619,  1620,  1621,  1624,  1625,  1628,  1629,  1630,  1631,  1632,
    1633,  1634,  1635,  1636,  1637,  1638,  1639,  1640,  1641,  1642,
    1643,  1644,  1645,  1646,  1647,  1648,  1649,  1650,  1651,  1652,
    1653,  1654,  1655,  1656,  1657,  1658,  1659,  1660,  1661,  1662,
    1663,  1664,  1665,  1666,  1667,  1668,  1669,  1670,  1671,  1672,
    1673,  1674,  1675,  1676,  1680,  1686,  1687,  1688,  1689,  1690,
    1691,  1692,  1693,  1694,  1695,  1697,  1699,  1706,  1713,  1719,
    1725,  1740,  1755,  1756,  1757,  1758,  1759,  1760,  1761,  1764,
    1765,  1766,  1767,  1768,  1769,  1770,  1771,  1772,  1773,  1774,
    1775,  1776,  1777,  1778,  1779,  1780,  1781,  1784,  1785,  1788,
    1789,  1790,  1791,  1794,  1798,  1800,  1802,  1803,  1804,  1806,
    1815,  1816,  1817,  1820,  1823,  1828,  1829,  1833,  1834,  1837,
    1840,  1841,  1844,  1847,  1850,  1853,  1857,  1863,  1869,  1875,
    1883,  1884,  1885,  1886,  1887,  1888,  1889,  1890,  1891,  1892,
    1893,  1894,  1895,  1896,  1897,  1901,  1902,  1905,  1908,  1910,
    1913,  1915,  1919,  1922,  1926,  1929,  1933,  1936,  1942,  1944,
    1947,  1948,  1951,  1952,  1955,  1958,  1961,  1962,  1963,  1964,
    1965,  1966,  1967,  1968,  1969,  1970,  1973,  1974,  1977,  1978,
    1979,  1982,  1983,  1986,  1987,  1989,  1990,  1991,  1992,  1995,
    1998,  2001,  2004,  2006,  2010,  2011,  2014,  2015,  2016,  2017,
    2020,  2023,  2026,  2027,  2028,  2029,  2030,  2031,  2032,  2033,
    2034,  2035,  2038,  2039,  2042,  2043,  2044,  2045,  2047,  2049,
    2050,  2053,  2054,  2058,  2059,  2060,  2063,  2064,  2067,  2068,
    2069,  2070
};
#endif

/** Accessing symbol of state STATE.  */
#define YY_ACCESSING_SYMBOL(State) YY_CAST (yysymbol_kind_t, yystos[State])

#if YYDEBUG || 1
/* The user-facing name of the symbol whose (internal) number is
   YYSYMBOL.  No bounds checking.  */
static const char *yysymbol_name (yysymbol_kind_t yysymbol) YY_ATTRIBUTE_UNUSED;

/* YYTNAME[SYMBOL-NUM] -- String name of the symbol SYMBOL-NUM.
   First, the terminals, then, starting at YYNTOKENS, nonterminals.  */
static const char *const yytname[] =
{
  "\"end of file\"", "error", "\"invalid token\"", "ERROR_",
  "BAD_COMMENT_", "BAD_LITERAL_", "ID", "DOTTEDNAME", "QSTRING",
  "SQSTRING", "INT32_T", "INT64_T", "FLOAT64", "HEXBYTE", "TYPEDEF_T",
  "TYPEDEF_M", "TYPEDEF_F", "TYPEDEF_TS", "TYPEDEF_MR", "TYPEDEF_CA",
  "DCOLON", "ELLIPSIS", "VOID_", "BOOL_", "CHAR_", "UNSIGNED_", "INT_",
  "INT8_", "INT16_", "INT32_", "INT64_", "FLOAT_", "FLOAT32_", "FLOAT64_",
  "BYTEARRAY_", "UINT_", "UINT8_", "UINT16_", "UINT32_", "UINT64_",
  "FLAGS_", "CALLCONV_", "MDTOKEN_", "OBJECT_", "STRING_", "NULLREF_",
  "DEFAULT_", "CDECL_", "VARARG_", "STDCALL_", "THISCALL_", "FASTCALL_",
  "CLASS_", "BYREFLIKE_", "TYPEDREF_", "UNMANAGED_", "FINALLY_",
  "HANDLER_", "CATCH_", "FILTER_", "FAULT_", "EXTENDS_", "IMPLEMENTS_",
  "TO_", "AT_", "TLS_", "TRUE_", "FALSE_", "_INTERFACEIMPL", "VALUE_",
  "VALUETYPE_", "NATIVE_", "INSTANCE_", "SPECIALNAME_", "FORWARDER_",
  "STATIC_", "PUBLIC_", "PRIVATE_", "FAMILY_", "FINAL_", "SYNCHRONIZED_",
  "INTERFACE_", "SEALED_", "NESTED_", "ABSTRACT_", "AUTO_", "SEQUENTIAL_",
  "EXPLICIT_", "ANSI_", "UNICODE_", "AUTOCHAR_", "IMPORT_", "ENUM_",
  "VIRTUAL_", "NOINLINING_", "AGGRESSIVEINLINING_", "NOOPTIMIZATION_",
  "AGGRESSIVEOPTIMIZATION_", "UNMANAGEDEXP_", "BEFOREFIELDINIT_",
  "STRICT_", "RETARGETABLE_", "WINDOWSRUNTIME_", "NOPLATFORM_", "METHOD_",
  "FIELD_", "PINNED_", "MODREQ_", "MODOPT_", "SERIALIZABLE_", "PROPERTY_",
  "TYPE_", "ASSEMBLY_", "FAMANDASSEM_", "FAMORASSEM_", "PRIVATESCOPE_",
  "HIDEBYSIG_", "NEWSLOT_", "RTSPECIALNAME_", "PINVOKEIMPL_", "_CTOR",
  "_CCTOR", "LITERAL_", "NOTSERIALIZED_", "INITONLY_", "REQSECOBJ_",
  "CIL_", "OPTIL_", "MANAGED_", "FORWARDREF_", "PRESERVESIG_", "RUNTIME_",
  "INTERNALCALL_", "_IMPORT", "NOMANGLE_", "LASTERR_", "WINAPI_", "AS_",
  "BESTFIT_", "ON_", "OFF_", "CHARMAPERROR_", "INSTR_NONE", "INSTR_VAR",
  "INSTR_I", "INSTR_I8", "INSTR_R", "INSTR_BRTARGET", "INSTR_METHOD",
  "INSTR_FIELD", "INSTR_TYPE", "INSTR_STRING", "INSTR_SIG", "INSTR_TOK",
  "INSTR_SWITCH", "_CLASS", "_NAMESPACE", "_METHOD", "_FIELD", "_DATA",
  "_THIS", "_BASE", "_NESTER", "_EMITBYTE", "_TRY", "_MAXSTACK", "_LOCALS",
  "_ENTRYPOINT", "_ZEROINIT", "_EVENT", "_ADDON", "_REMOVEON", "_FIRE",
  "_OTHER", "_PROPERTY", "_SET", "_GET", "_PERMISSION", "_PERMISSIONSET",
  "REQUEST_", "DEMAND_", "ASSERT_", "DENY_", "PERMITONLY_", "LINKCHECK_",
  "INHERITCHECK_", "REQMIN_", "REQOPT_", "REQREFUSE_", "PREJITGRANT_",
  "PREJITDENY_", "NONCASDEMAND_", "NONCASLINKDEMAND_",
  "NONCASINHERITANCE_", "_LINE", "P_LINE", "_LANGUAGE", "_CUSTOM", "INIT_",
  "_SIZE", "_PACK", "_VTABLE", "_VTFIXUP", "FROMUNMANAGED_",
  "CALLMOSTDERIVED_", "_VTENTRY", "RETAINAPPDOMAIN_", "_FILE",
  "NOMETADATA_", "_HASH", "_ASSEMBLY", "_PUBLICKEY", "_PUBLICKEYTOKEN",
  "ALGORITHM_", "_VER", "_LOCALE", "EXTERN_", "_MRESOURCE", "_MODULE",
  "_EXPORT", "LEGACY_", "LIBRARY_", "X86_", "AMD64_", "ARM_", "ARM64_",
  "MARSHAL_", "CUSTOM_", "SYSSTRING_", "FIXED_", "VARIANT_", "CURRENCY_",
  "SYSCHAR_", "DECIMAL_", "DATE_", "BSTR_", "TBSTR_", "LPSTR_", "LPWSTR_",
  "LPTSTR_", "OBJECTREF_", "IUNKNOWN_", "IDISPATCH_", "STRUCT_",
  "SAFEARRAY_", "BYVALSTR_", "LPVOID_", "ANY_", "ARRAY_", "LPSTRUCT_",
  "IIDPARAM_", "IN_", "OUT_", "OPT_", "_PARAM", "_OVERRIDE", "WITH_",
  "NULL_", "HRESULT_", "CARRAY_", "USERDEFINED_", "RECORD_", "FILETIME_",
  "BLOB_", "STREAM_", "STORAGE_", "STREAMED_OBJECT_", "STORED_OBJECT_",
  "BLOB_OBJECT_", "CF_", "CLSID_", "VECTOR_", "_SUBSYSTEM", "_CORFLAGS",
  "ALIGNMENT_", "_IMAGEBASE", "_STACKRESERVE", "_TYPEDEF", "_TEMPLATE",
  "_TYPELIST", "_MSCORLIB", "P_DEFINE", "P_UNDEF", "P_IFDEF", "P_IFNDEF",
  "P_ELSE", "P_ENDIF", "P_INCLUDE", "CONSTRAINT_", "CONST_", "'{'", "'}'",
  "'+'", "','", "'.'", "'('", "')'", "';'", "'='", "'['", "']'", "'<'",
  "'>'", "'-'", "':'", "'*'", "'&'", "'/'", "'!'", "$accept", "decls",
  "decl", "classNameSeq", "compQstring", "languageDecl", "id",
  "dottedName", "int32", "int64", "float64", "typedefDecl", "compControl",
  "customDescr", "customDescrWithOwner", "customHead",
  "customHeadWithOwner", "customType", "ownerType", "customBlobDescr",
  "customBlobArgs", "customBlobNVPairs", "fieldOrProp", "customAttrDecl",
  "serializType", "moduleHead", "vtfixupDecl", "vtfixupAttr", "vtableDecl",
  "vtableHead", "nameSpaceHead", "_class", "classHeadBegin", "classHead",
  "classAttr", "extendsClause", "implClause", "classDecls", "implList",
  "typeList", "typeListNotEmpty", "typarsClause", "typarAttrib",
  "typarAttribs", "conTyparAttrib", "conTyparAttribs", "typars",
  "typarsRest", "tyBound", "genArity", "genArityNotEmpty", "classDecl",
  "fieldDecl", "fieldAttr", "atOpt", "initOpt", "repeatOpt", "methodRef",
  "callConv", "callKind", "mdtoken", "memberRef", "eventHead", "eventAttr",
  "eventDecls", "eventDecl", "propHead", "propAttr", "propDecls",
  "propDecl", "methodHeadPart1", "marshalClause", "marshalBlob",
  "marshalBlobHead", "methodHead", "methAttr", "pinvAttr", "methodName",
  "paramAttr", "implAttr", "localsHead", "methodDecls", "methodDecl",
  "scopeBlock", "scopeOpen", "sehBlock", "sehClauses", "tryBlock",
  "tryHead", "sehClause", "filterClause", "filterHead", "catchClause",
  "finallyClause", "faultClause", "handlerBlock", "dataDecl", "ddHead",
  "tls", "ddBody", "ddItemList", "ddItemCount", "ddItem", "fieldSerInit",
  "bytearrayhead", "bytes", "hexbytes", "fieldInit", "serInit", "f32seq",
  "f64seq", "i64seq", "i32seq", "i16seq", "i8seq", "boolSeq",
  "sqstringSeq", "classSeq", "objSeq", "methodSpec", "instr_none",
  "instr_var", "instr_i", "instr_i8", "instr_r", "instr_brtarget",
  "instr_method", "instr_field", "instr_type", "instr_string", "instr_sig",
  "instr_tok", "instr_switch", "instr_r_head", "instr", "labels",
  "tyArgs0", "tyArgs1", "tyArgs2", "sigArgs0", "sigArgs1", "sigArg",
  "className", "slashedName", "typeSpec", "nativeType", "iidParamIndex",
  "variantType", "type", "simpleType", "bounds1", "bound", "secDecl",
  "secAttrSetBlob", "secAttrBlob", "psetHead", "nameValPairs",
  "nameValPair", "truefalse", "caValue", "secAction", "esHead",
  "extSourceSpec", "fileDecl", "fileAttr", "fileEntry", "hashHead",
  "assemblyHead", "asmAttr", "assemblyDecls", "assemblyDecl",
  "intOrWildcard", "asmOrRefDecl", "publicKeyHead", "publicKeyTokenHead",
  "localeHead", "assemblyRefHead", "assemblyRefDecls", "assemblyRefDecl",
  "exptypeHead", "exportHead", "exptAttr", "exptypeDecls", "exptypeDecl",
  "manifestResHead", "manresAttr", "manifestResDecls", "manifestResDecl", YY_NULLPTR
};

static const char *
yysymbol_name (yysymbol_kind_t yysymbol)
{
  return yytname[yysymbol];
}
#endif

#define YYPACT_NINF (-1314)

#define yypact_value_is_default(Yyn) \
  ((Yyn) == YYPACT_NINF)

#define YYTABLE_NINF (-563)

#define yytable_value_is_error(Yyn) \
  0

/* YYPACT[STATE-NUM] -- Index in YYTABLE of the portion describing
   STATE-NUM.  */
static const yytype_int16 yypact[] =
{
   -1314,   962, -1314, -1314,  -114,   512, -1314,  -140,   123,  3127,
    3127, -1314, -1314,   164,   703,   -50,   -49,    -3,    84, -1314,
     216,   319,   319,   197,   197,  1929,    54, -1314,   512,   512,
     512,   512, -1314, -1314,   266, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314,   341,   341, -1314, -1314, -1314, -1314,   341,    80,
   -1314,   301,   125, -1314, -1314, -1314, -1314,   498, -1314,   341,
     319, -1314, -1314,   158,   181,   190,   211, -1314, -1314, -1314,
   -1314, -1314,   230,   319, -1314, -1314, -1314,   253, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314,  2255,    69,   244, -1314, -1314,   234,   247,
   -1314, -1314,   502,   753,   753,  2161,   131, -1314,  3201, -1314,
   -1314,   249,   319,   319,   251, -1314,   683,   559,   512,   230,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,  3201,
   -1314, -1314, -1314,   839, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314, -1314,   534, -1314,   504,   534,
     387, -1314,  2550, -1314, -1314, -1314,  1781,    50,    42,   230,
     427,   435, -1314,   438,  1460,   448,   270,   472, -1314,   534,
      47,   230,   230,   230, -1314, -1314,   300,   591,   314,   336,
   -1314,  3319,  2255,   572, -1314,  3677,  2513,   334,   136,   302,
     304,   326,   332,   352,   346,   628,   351, -1314, -1314,   341,
     358,    45, -1314, -1314, -1314, -1314,   660,   512,   370,  3040,
     381,    66, -1314,   753, -1314,   105,   921, -1314,   390,   215,
     396,   688,   319,   319, -1314, -1314, -1314, -1314, -1314, -1314,
     415, -1314, -1314,    85,  1419, -1314,   417, -1314, -1314,   -18,
     683, -1314, -1314, -1314, -1314,   499, -1314, -1314, -1314, -1314,
     230, -1314, -1314,   -43,   230,   921, -1314, -1314, -1314, -1314,
   -1314,   534, -1314,   704, -1314, -1314, -1314, -1314,  1731,   466,
     467,  1027,   474,   479,   481,   500,   503,   510,   517,   529,
     539,   551, -1314,   396, -1314,   341, -1314,   512,   507,    83,
     547,   694,   230, -1314,   512,   512,   512, -1314,  3201,   512,
     512, -1314,   558,   560,   512,    48,  3201, -1314, -1314,   444,
     534,   396, -1314, -1314, -1314, -1314,  3180,   561, -1314, -1314,
   -1314, -1314, -1314, -1314,   751, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,   -63, -1314,
    2255, -1314,  3359,   600, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314,   602, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314,   319, -1314,   319,
   -1314, -1314, -1314,   319,   540,   -32,  2324, -1314, -1314, -1314,
     570, -1314, -1314,  -132, -1314, -1314, -1314, -1314,   873,   224,
   -1314, -1314,   312,   319,   197,   209,   312,  1460,  1843,  2255,
     236,   753,  2161,   609,   341, -1314, -1314, -1314,   614,   319,
     319, -1314,   319, -1314,   319, -1314,   197, -1314,   269, -1314,
     269, -1314, -1314,   557,   627,   253,   629, -1314, -1314, -1314,
     319,   319,  1183,  1302,  1066,   902, -1314, -1314, -1314,   964,
     230,   230, -1314,   616, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314,   633,    58, -1314,
     512,    95,  3201,   932,   651, -1314,  2397, -1314,   941,   662,
     665,   667,  1460, -1314, -1314,   396, -1314, -1314,    74,   103,
     675,   948, -1314, -1314,   770,   -33, -1314,   512, -1314, -1314,
     103,   968,   241,   319,   707,   713,   714,   716,   319,   319,
     319,   197,   747,   579,   319,   319,   319,   197,   721,   173,
     512,   512,   512,   230, -1314,   230,   230,   230,  1626,   230,
     230,  2255,  2255,   230, -1314, -1314,  1015,     4, -1314,   736,
     757,   921, -1314, -1314, -1314,   319, -1314, -1314, -1314, -1314,
   -1314, -1314,   792, -1314,   759, -1314,   947, -1314, -1314, -1314,
     319,   319, -1314,   -35,  2466, -1314, -1314, -1314, -1314,   773,
   -1314, -1314,   774,   777, -1314, -1314, -1314, -1314,   778,   319,
     932,  2877, -1314, -1314,   769,   319,    91,   108,   319,   753,
    1059, -1314,   787,    39,  2678, -1314,  2255, -1314, -1314, -1314,
     873,     6,   224,     6,     6,     6,  1020,  1023, -1314, -1314,
   -1314, -1314, -1314, -1314,   798,   799, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314,  1731, -1314,   801,   396,   341,
    3201, -1314,   312,   802,   932,   804,   788,   814,   818,   819,
     820,   826, -1314,   628,   827, -1314,   821,   111,   884,   828,
      51,    70, -1314, -1314, -1314, -1314, -1314, -1314,   341,   341,
   -1314,   829,   833, -1314,   341, -1314,   341, -1314,   855,    72,
     512,   908, -1314, -1314, -1314, -1314,   512,   925, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,   319,  3262,
      26,   208,   512,   995,    41,   847,   853, -1314,   631,   857,
     862,   867, -1314,  1152, -1314, -1314,   864,   874,  2606,  3150,
     871,   875,   463,   544,   341,   512,   230,   512, -1314, -1314,
     881,   886,   319,   319,   319,   197,   898,   899,   900,   904,
     905,   910,   912,   913,   914,   915,   916,   922, -1314,   512,
     270,   270,   270,   876,   933,   934,   319,   146, -1314, -1314,
    3201,   935,   903, -1314, -1314, -1314, -1314,  1164, -1314, -1314,
     258,   139,   926,  2255,  2255,  2092,   919, -1314, -1314,   660,
     124,   134,   753,  1185, -1314, -1314, -1314,  2841, -1314,   936,
      73,  1040,    61,   846,   319,   917,   319,   230,   319,    88,
     940,  3201,   463,    39, -1314,  2877,   937,   943, -1314, -1314,
   -1314, -1314,   312, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314,   253,   319,   319,   197,   103,  1230,   932,   955,   618,
     960,   961,   965, -1314,   168,   957, -1314,   957,   957,   957,
     957,   957, -1314, -1314,   319, -1314,   319,   319,   963, -1314,
   -1314,   953,   969,   396,   970,   971,   973,   975,   976,   981,
     319,   512, -1314,   230,   512,    57,   512,   982, -1314, -1314,
   -1314, -1314,   909, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314,   984,  1017,  1030, -1314,  1043,
     997,    18,  1274, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314,   984,   984, -1314,  1818, -1314, -1314, -1314,
    1002,   341,   121,   253,  1001,   512,   118, -1314,   932,  1011,
    1005,  1014, -1314,  2397, -1314,   126, -1314,   354,   468,  1077,
     501,   511,   531,   536,   584,   594,   595,   615,   623,   647,
     648,   671,   674, -1314,  1279, -1314, -1314,   319,  1003,    39,
      39,   230,   675, -1314, -1314,   253, -1314, -1314, -1314,  1012,
     230,   230, -1314, -1314,  1013,  1016,  1019,  1021, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
     270,    39, -1314, -1314, -1314, -1314,   921, -1314,   319,  1024,
    1164,  2255, -1314,  2255,   194,   512, -1314, -1314,  1113, -1314,
   -1314,   564,   512, -1314, -1314,  3201,   230,   319,   230,   319,
     218,  3201,   463,  3405,   851,  1725, -1314,  1339, -1314,   932,
    2014,  1028, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314,  1007,  1018, -1314,  1032,  1033,  1034,  1037,  1029,
     463, -1314,  1181,  1039,  1042,  2255,  1001,  1731, -1314,  1047,
     846, -1314,  1319,  1281,  1282, -1314, -1314,  1050,  1058,   512,
     679, -1314,    39,   312,   312, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314,   135,  1347, -1314, -1314,    51, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314,  1068,   270,   230,   319,   230,
   -1314, -1314, -1314, -1314, -1314, -1314,  1112, -1314, -1314, -1314,
   -1314,   932,  1067,  1069, -1314, -1314, -1314, -1314, -1314, -1314,
     956, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,   264, -1314,
      49,    31, -1314, -1314,  1781, -1314,  1070,   396, -1314,  1072,
   -1314, -1314, -1314, -1314,  1079, -1314, -1314, -1314, -1314,   396,
     319,   319,   695,   700,   701,   744,   319,   319,   319,   319,
     319,   319,   319,   319,   319,   319,  3676,   319,   207,   319,
     553,   319, -1314, -1314, -1314,  2697,  1071, -1314,  1074,  1076,
    1078,  1080, -1314,  1209, -1314, -1314, -1314, -1314,  1081,  1082,
     319, -1314,   512,  1083,  1089, -1314,   792, -1314,   194,  1460,
   -1314,   230,    58,  1085,  1086,  2255,  1731,  1132, -1314,  1460,
    1460,  1460,  1460, -1314, -1314, -1314, -1314, -1314, -1314,  1460,
    1460,  1460, -1314, -1314, -1314, -1314, -1314, -1314, -1314,   396,
   -1314,   319,   476,   524, -1314, -1314, -1314, -1314,  3262,  1092,
     253, -1314,  1102, -1314, -1314,  1380, -1314,   253, -1314,   253,
     319, -1314, -1314,   230, -1314,  1105, -1314, -1314, -1314,   319,
   -1314,  1100, -1314, -1314,  1107,   384,   319,   319, -1314, -1314,
   -1314, -1314, -1314, -1314,   932,  1108, -1314, -1314,   319, -1314,
     -68, -1314,   319,   793, -1314,  1044,  1118,  1110,  1111,   319,
     319,   319,   319,  1119,  1121,  1122,  1123,  1124,  1127,  1129,
    1130,  1131,  1134,  1116,  1135,  1136,  1141,  1138,  1143,  1409,
    1149,  1153, -1314,   385, -1314,   163, -1314,  1157, -1314, -1314,
      39,    39, -1314, -1314, -1314,  1158,   194, -1314,  2255, -1314,
   -1314,   428, -1314,  1167, -1314,  1452,   753, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314,  2729,  1180, -1314, -1314, -1314, -1314,
    1186,  1182, -1314,  2255,   463, -1314, -1314, -1314, -1314,  1470,
      51,   319,   932,  1184,  1187,   396, -1314,  1191,   319, -1314,
    1190,  1177,  1192,  1194,  1199,  1402, -1314, -1314,  1208,  1210,
    1204,  1218,  1219,  1220,  1214,  1215,  1226,  1227,  1229,  1233,
    1234,  1235,  1236,  1238, -1314,  1239, -1314, -1314,  1240, -1314,
    1241, -1314,  1243,  1532, -1314,  1242,   755, -1314,  1247,  1248,
   -1314, -1314, -1314,    39,  2255,   463,  3201, -1314, -1314, -1314,
      39, -1314,  1244, -1314,  1249,  1252,   413, -1314,  3593, -1314,
    1251, -1314,   319,   319,   319, -1314, -1314, -1314, -1314,  1257,
    1259,  1260,  1264, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,  3676,
   -1314, -1314,  1271, -1314,  1244,  1731,  1272,  1270,  1277, -1314,
      51, -1314,   932, -1314,   121, -1314,  1278,  1286,  1294,   170,
      64, -1314, -1314, -1314, -1314,    97,   100,   128,   144,   182,
     179,   140,   165,   172,   189,   848,    77,   432, -1314,  1001,
    1280,  1553, -1314,    39, -1314,   440, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314,   183,   185,   203,   193, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
    1568, -1314, -1314, -1314,    39,   463,  2902,  1295,   932, -1314,
   -1314, -1314, -1314, -1314,  1298,  1283,  1300, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314,   697,  1340,    39,   319, -1314,  1493,  1304,
    1305,   753, -1314, -1314,  3201,  1731,  1578,   463,  1244,  1307,
      39,  1309, -1314
};

/* YYDEFACT[STATE-NUM] -- Default reduction number in state STATE-NUM.
   Performed when YYTABLE does not specify something else to do.  Zero
   means the default is an error.  */
static const yytype_int16 yydefact[] =
{
       2,     0,     1,    86,   106,     0,   269,   213,   394,     0,
       0,   765,   766,     0,   226,     0,     0,   780,   786,   843,
      93,     0,     0,     0,     0,     0,     0,    29,     0,     0,
       0,     0,    58,    59,     0,    61,     3,    25,    26,    27,
      84,    85,   438,   438,    19,    17,    10,     9,   438,     0,
     109,   136,     0,     7,   276,   340,     8,     0,    18,   438,
       0,    11,    12,     0,     0,     0,     0,   822,    37,    40,
      38,    39,   105,     0,   193,   395,   396,   393,   750,   751,
     752,   753,   754,   755,   756,   757,   758,   759,   760,   761,
     762,   763,   764,     0,     0,    34,   220,   221,     0,     0,
     227,   228,   233,   226,   226,     0,    62,    72,     0,   224,
     219,     0,     0,     0,     0,   786,     0,     0,     0,    94,
      42,    20,    21,    44,    43,    23,    24,   558,   716,     0,
     693,   701,   699,     0,   702,   703,   704,   705,   706,   707,
     712,   713,   714,   715,   675,   700,     0,   692,     0,     0,
       0,   496,     0,   559,   560,   561,     0,     0,     0,   562,
       0,     0,   240,     0,   226,     0,   556,     0,   697,    30,
      53,    55,    56,    57,    60,   440,     0,   439,     0,     0,
       2,     0,     0,   138,   140,   226,     0,     0,   401,   401,
     401,   401,   401,   401,     0,     0,     0,   391,   398,   438,
       0,   768,   796,   814,   832,   846,     0,     0,     0,     0,
       0,     0,   557,   226,   564,   726,   567,    32,     0,     0,
     728,     0,     0,     0,   229,   230,   231,   232,   222,   223,
       0,    74,    73,     0,     0,   104,     0,    22,   781,   782,
       0,   787,   788,   789,   791,     0,   792,   793,   794,   795,
     785,   844,   845,   841,    95,   698,   708,   709,   710,   711,
     674,     0,   677,     0,   694,   696,   238,   239,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   444,   443,   442,   438,   678,     0,     0,     0,
       0,     0,   691,   689,     0,     0,     0,   235,     0,     0,
       0,   683,     0,     0,     0,   719,   541,   682,   681,     0,
      30,    54,    65,   441,    69,   103,     0,     0,   112,   133,
     110,   111,   114,   115,     0,   116,   117,   118,   119,   120,
     121,   122,   123,   113,   132,   125,   124,   134,   148,   137,
       0,   108,     0,     0,   282,   277,   278,   279,   280,   281,
     285,   283,   293,   284,   286,   287,   288,   289,   290,   291,
     292,     0,   294,   318,   497,   498,   499,   500,   501,   502,
     503,   504,   505,   506,   507,   508,   509,     0,   376,     0,
     339,   347,   348,     0,     0,     0,     0,   369,     6,   354,
       0,   356,   355,     0,   341,   362,   340,   343,     0,     0,
     349,   511,     0,     0,     0,     0,     0,   226,     0,     0,
       0,   226,     0,     0,   438,   350,   352,   353,     0,     0,
       0,   417,     0,   416,     0,   415,     0,   414,     0,   412,
       0,   413,   437,     0,   400,     0,     0,   727,   777,   767,
       0,     0,     0,     0,     0,     0,   825,   824,   823,     0,
     820,    41,   214,     0,   200,   194,   195,   196,   197,   202,
     203,   204,   205,   199,   206,   207,   198,     0,     0,   392,
       0,     0,     0,     0,     0,   736,   730,   735,     0,    35,
       0,     0,   226,    76,    70,    63,   315,   316,   719,   317,
     539,     0,    97,   783,   779,   812,   790,     0,   676,   695,
     237,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   690,   688,    51,    52,    50,     0,    49,
     563,     0,     0,    48,   720,   679,   721,     0,   717,     0,
     542,   543,    28,    31,     5,     0,   126,   127,   128,   129,
     130,   131,   157,   107,   139,   143,     0,   106,   243,   257,
       0,     0,   822,     0,     0,     4,   185,   186,   179,     0,
     141,   175,     0,     0,   340,   176,   177,   178,     0,     0,
     299,     0,   342,   344,     0,     0,     0,     0,     0,   226,
       0,   351,     0,   318,     0,   386,     0,   384,   387,   370,
     372,     0,     0,     0,     0,     0,     0,     0,   373,   513,
     512,   514,   515,    45,     0,     0,   510,   517,   516,   520,
     519,   521,   525,   526,   524,     0,   527,     0,   528,   438,
       0,   532,   534,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   397,     0,     0,   405,     0,   770,     0,     0,
       0,     0,    13,   808,   807,   799,   797,   800,   438,   438,
     819,     0,     0,    14,   438,   817,   438,   815,     0,     0,
       0,     0,    15,   840,   839,   833,     0,     0,    16,   851,
     850,   847,   826,   827,   828,   829,   830,   831,     0,   568,
     209,     0,   565,     0,     0,     0,   737,    76,     0,     0,
       0,   731,    33,     0,   225,   234,    66,     0,    79,   541,
       0,     0,     0,     0,   438,     0,   842,     0,   740,   741,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,   436,     0,
     554,   552,   553,   682,     0,     0,   723,   719,   680,   687,
       0,     0,     0,   152,   154,   153,   155,   160,   150,   151,
     157,     0,     0,     0,     0,     0,   226,   180,   181,     0,
       0,     0,   226,     0,   140,   246,   260,     0,   832,     0,
     299,     0,     0,   270,     0,     0,     0,   364,     0,     0,
       0,     0,     0,   318,   549,     0,     0,   546,   547,   368,
     385,   371,     0,   388,   378,   382,   383,   381,   377,   379,
     380,     0,     0,     0,     0,   523,     0,     0,     0,     0,
     537,   538,     0,   518,     0,   401,   402,   401,   401,   401,
     401,   401,   399,   404,     0,   769,     0,     0,     0,   802,
     801,     0,     0,   805,     0,     0,     0,     0,     0,     0,
       0,     0,   838,   834,     0,     0,     0,     0,   584,   622,
     576,   577,     0,   611,   578,   579,   580,   581,   582,   583,
     613,   589,   590,   591,   592,   623,     0,     0,   619,     0,
       0,     0,   573,   574,   575,   598,   599,   600,   617,   601,
     602,   603,   604,   623,   623,   607,   625,   615,   621,   274,
       0,     0,   272,     0,   211,   566,     0,   724,     0,     0,
      38,     0,   729,   730,    36,     0,    64,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,    78,    75,   445,    77,     0,     0,   318,
     318,   317,   539,    98,    99,     0,   100,   101,   102,     0,
     813,   236,   435,   434,     0,     0,     0,     0,   425,   424,
     423,   422,   420,   418,   421,   419,   433,   432,   431,   430,
     555,   318,   684,   685,   722,   718,   544,   135,     0,     0,
     160,     0,   158,   144,   165,     0,   149,   142,     0,   245,
     244,   562,     0,   259,   258,     0,   821,     0,   188,     0,
       0,     0,     0,     0,     0,     0,   171,     0,   295,     0,
       0,     0,   306,   307,   308,   309,   301,   302,   303,   300,
     304,   305,     0,     0,   298,     0,     0,     0,     0,     0,
       0,   359,   357,     0,     0,     0,   211,     0,   360,     0,
     270,   345,   318,     0,     0,   374,   375,     0,     0,     0,
       0,   530,   318,   534,   534,   533,   403,   411,   410,   409,
     408,   406,   407,   774,   772,   798,   809,     0,   811,   803,
     806,   784,   810,   816,   818,     0,   835,   836,     0,   849,
     208,   612,   585,   586,   587,   588,     0,   608,   614,   616,
     620,     0,     0,     0,   618,   605,   606,   660,   629,   630,
       0,   657,   631,   632,   633,   634,   635,   636,   659,   641,
     642,   643,   644,   627,   628,   649,   650,   651,   652,   653,
     654,   655,   656,   626,   661,   662,   663,   664,   665,   666,
     667,   668,   669,   670,   671,   672,   673,   645,   609,   201,
       0,     0,   593,   210,     0,   192,     0,   745,   743,     0,
     742,   739,   738,   725,     0,    79,   732,    76,    71,    67,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,    82,    83,    81,     0,     0,   540,     0,     0,
       0,     0,    96,   782,   429,   428,   427,   426,     0,     0,
       0,   161,     0,     0,   145,   146,   157,   164,   165,   226,
     191,   241,     0,     0,     0,     0,     0,     0,   172,   226,
     226,   226,   226,   173,   254,   255,   253,   247,   252,   226,
     226,   226,   174,   267,   268,   265,   261,   266,   182,   299,
     297,     0,     0,     0,   319,   320,   321,   322,   568,   148,
       0,   363,     0,   366,   367,     0,   346,   550,   548,     0,
       0,    46,    47,   522,   529,     0,   535,   536,   773,     0,
     771,     0,   837,   848,     0,     0,     0,     0,   658,   637,
     638,   639,   640,   647,     0,     0,   648,   273,     0,   594,
       0,   212,     0,     0,    79,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,    89,     0,    88,     0,    87,     0,   218,   217,
     318,   318,   778,   686,   156,     0,   165,   167,     0,   166,
     163,     0,   187,     0,   190,     0,   226,   248,   249,   250,
     251,   264,   262,   263,     0,     0,   310,   311,   312,   313,
       0,     0,   358,     0,     0,   551,   389,   390,   531,   776,
       0,     0,     0,     0,     0,   610,   646,     0,     0,   595,
       0,     0,     0,     0,     0,     0,   733,    68,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   451,     0,   447,   446,     0,   450,
       0,   449,     0,     0,    91,     0,     0,   170,     0,     0,
     159,   162,   147,   318,     0,     0,     0,   296,   314,   271,
     318,   365,   168,   775,     0,     0,     0,   571,   568,   597,
       0,   744,     0,     0,     0,   749,   734,   485,   481,     0,
       0,     0,     0,   483,   481,   479,   477,   471,   474,   483,
     481,   479,   477,   494,   487,   448,   490,    90,    92,     0,
     216,   215,     0,   189,   168,     0,     0,     0,     0,   169,
       0,   624,     0,   570,   572,   596,     0,     0,     0,     0,
       0,   483,   481,   479,   477,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,    80,   211,
       0,     0,   323,   318,   804,     0,   746,   747,   748,   467,
     486,   466,   482,     0,     0,     0,     0,   457,   484,   456,
     455,   480,   454,   478,   452,   473,   472,   453,   476,   475,
     461,   460,   459,   458,   470,   495,   489,   488,   468,   491,
       0,   469,   493,   256,   318,     0,     0,     0,     0,   465,
     464,   463,   462,   492,     0,     0,     0,   328,   324,   333,
     334,   335,   336,   337,   325,   326,   327,   329,   330,   331,
     332,   275,   361,     0,     0,   318,     0,   569,     0,     0,
       0,   226,   183,   338,     0,     0,     0,     0,   168,     0,
     318,     0,   184
};

/* YYPGOTO[NTERM-NUM].  */
static const yytype_int16 yypgoto[] =
{
   -1314,  1423, -1314,  1296,   -65,    27,    12,    -5,   114,   -17,
    -425, -1314,    11,   -11,  1582, -1314, -1314,  1126,  1200,  -667,
   -1314, -1054, -1314,     0, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314,  -322, -1314, -1314, -1314,   840, -1314, -1314,
   -1314,   369, -1314,   859, -1314,   649,   421, -1071, -1314, -1313,
    -456, -1314,  -321, -1314, -1314, -1000, -1314,  -160,   -94, -1314,
      -6,  1603, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314,   601,   383, -1314,  -320, -1314,  -741,  -696,  1289, -1314,
   -1314,  -356, -1314,  -144, -1314, -1314,  1045, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314,   150,    16, -1314, -1314, -1314,
     991,   -12,  1585,  -143,   -24,   -16,   743,   513, -1139, -1314,
   -1314, -1189, -1174, -1215, -1166, -1314, -1314, -1314, -1314,    13,
   -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314, -1314, -1314, -1314, -1314,  -312,   718,   952, -1314,  -743,
   -1314,   604,    -7,  -475,   -25,   228,  -112, -1314,   -23,   482,
   -1314,   924,    10,   760, -1314, -1314,   764, -1314,  -871, -1314,
    1664, -1314,    30, -1314, -1314,   483,  1188, -1314,  1560, -1314,
   -1314, -1023,  1255, -1314, -1314, -1314, -1314, -1314, -1314, -1314,
   -1314,  1115,   901, -1314, -1314, -1314, -1314, -1314
};

/* YYDEFGOTO[NTERM-NUM].  */
static const yytype_int16 yydefgoto[] =
{
       0,     1,    36,   309,   283,   389,    71,   159,   840,  1533,
     618,    38,   391,    40,    41,    42,    43,   106,   230,   707,
     708,   934,  1185,   392,  1325,    45,    46,   713,    47,    48,
      49,    50,    51,    52,   181,   183,   341,   342,   554,  1203,
    1204,   553,   760,   761,   980,   981,   762,  1207,   985,  1478,
    1479,   570,    53,   209,   904,  1145,    74,   107,   108,   109,
     212,   231,   572,   765,  1004,  1227,   573,   766,  1005,  1236,
      54,  1030,   900,   901,    55,   185,   781,   490,   795,  1556,
     393,   186,   394,   803,   396,   397,   599,   398,   399,   600,
     601,   602,   603,   604,   605,   804,   400,    57,    77,   197,
     433,   421,   434,   935,   285,   176,   177,   286,   936,  1499,
    1500,  1498,  1497,  1490,  1495,  1489,  1506,  1507,  1505,   213,
     401,   402,   403,   404,   405,   406,   407,   408,   409,   410,
     411,   412,   413,   414,   415,   822,   711,   539,   540,   796,
     797,   798,   214,   166,   232,   902,  1087,  1138,   216,   168,
     537,   538,   416,   700,   701,    59,   695,   696,   720,  1151,
      93,    60,   417,    62,   114,   494,   664,    63,   116,   442,
     656,   841,   657,   658,   666,   659,    64,   443,   667,    65,
     578,   206,   444,   675,    66,   117,   445,   681
};

/* YYTABLE[YYPACT[STATE-NUM]] -- What to do in state STATE-NUM.  If
   positive, shift that token.  If negative, reduce the rule whose
   number is the opposite.  If YYTABLE_NINF, syntax error.  */
static const yytype_int16 yytable[] =
{
      72,    44,   167,   640,   297,   641,   125,   126,   110,   228,
     229,    58,    39,   284,   160,   119,   942,    56,   165,   162,
     569,   571,   574,   170,   171,   172,   173,   178,    37,   220,
     909,    61,   179,   199,   710,  1150,  1254,  1313,   164,  1010,
     594,   120,   395,   200,  1271,   740,   741,   742,    68,    69,
    1039,    70,   120,   438,   439,   217,    68,    69,   120,    70,
     794,   120,   313,   802,    68,    69,   592,    70,   215,   534,
     298,   120,    68,    69,   120,    70,   770,   217,   217,   586,
      68,    69,   120,    70,   120,   234,  1546,   731,   733,   210,
     903,   363,    99,   217,   497,   534,  1038,    68,    69,   162,
      70,  1295,    67,   218,   715,   311,   255,   120,    99,   239,
     120,   250,   253,   254,    68,    69,    99,    70,   164,   472,
     835,  1078,  1547,  -562,    68,    69,   217,    70,   120,   268,
      68,    69,   127,    70,   217,   121,   122,  1340,   120,   260,
      68,    69,   262,    70,  1268,    68,    69,  1146,    70,   493,
     120,   290,   289,   292,   123,   124,   120,   339,   110,    73,
      99,  1510,   310,   593,   301,   302,   303,   534,   485,    68,
      69,   199,    70,    95,   201,   120,   338,   423,   425,   427,
     429,   431,   120,   436,   718,   719,   468,   208,    75,   123,
     124,   613,   120,   120,   613,   120,  1188,  1189,   390,   123,
     124,   450,   451,   123,   124,   290,   471,   123,   124,   491,
    1009,   614,   615,   120,   614,   615,  1315,   741,   777,   123,
     124,   613,    68,    69,  1378,    70,   236,   237,  1198,   489,
      68,   207,  1379,    70,   120,   495,   718,   719,   552,  1496,
    1385,   614,   615,   501,   217,  1502,  1092,   621,   111,    76,
     112,   207,  1316,   771,   498,   608,   587,    68,    69,    68,
      70,   207,    70,   500,   970,  1421,  1093,   588,   287,   518,
     194,   113,   293,  1504,   174,   528,   207,  1524,   153,   154,
     155,   613,   519,   541,   470,  1599,   523,  1503,   851,   525,
     526,   527,   470,  1501,   529,   530,   387,   747,   752,   533,
     115,   614,   615,   310,   748,  1526,  1217,   718,   719,  1265,
     753,   754,  1025,  1026,  1027,   555,    44,   630,    68,  1525,
     207,    70,   120,  1288,   627,  1523,    58,    39,   755,   120,
    1508,  1289,    56,   478,  1249,  -545,   480,   481,   440,   906,
    1287,   207,   568,    37,   169,   628,    61,  1434,   535,   441,
     291,   207,   576,   567,   175,   288,   839,   305,   575,   306,
    1521,   590,   182,   307,   308,   478,  1545,   219,   842,   566,
     180,   288,   577,  1548,   535,   483,  1076,   207,   756,   288,
     484,  1035,   207,   521,   626,   625,   629,   612,   617,   207,
     786,    68,    69,  1527,    70,   692,  1529,   207,   633,   127,
     473,   110,   624,   474,   836,   524,   162,   788,   694,   639,
    -562,   606,   263,   264,   609,   184,  1157,   157,   619,   536,
    1141,  1158,   265,   997,  1530,   164,  1142,    99,  1269,   233,
     970,   419,   118,   999,   983,   420,  1540,  1413,    68,    69,
    1532,    70,   654,   654,   674,   680,   127,   644,   202,   693,
     395,   699,   655,   653,   653,   673,   679,  1514,   807,   238,
     478,  1541,  1415,   690,  1056,   691,  1519,   207,  1542,    68,
      69,   203,    70,   739,    99,  1537,   110,  1549,  1534,  1559,
     204,  1560,   290,   471,  1550,  1543,  1190,  1206,   207,  1562,
    1296,   582,   716,   583,   729,   791,   732,   584,  1354,  1561,
     737,   205,   207,   491,   616,   476,   744,   745,   905,  1553,
     477,  1215,   207,   607,   387,   780,   610,   611,    68,    69,
     620,    70,   187,   489,   207,   188,   189,   190,   191,   222,
     192,   193,   194,   635,   636,  1283,   637,   221,   638,   773,
      68,    69,   223,    70,   235,   153,   154,   155,   127,   224,
     758,   225,   226,   227,   646,   647,   261,  1284,   783,    68,
      69,   759,    70,  1285,   294,    68,    69,   127,    70,   824,
    1286,   800,   295,   943,   944,   296,    99,   300,   301,   302,
     303,   787,   789,   486,   487,   299,   843,  1418,  1419,   123,
     124,   613,   153,   154,   155,    99,   312,   422,  1318,   424,
     816,   420,   536,   420,   313,  1319,   390,   819,   945,   304,
     314,   614,   615,   818,   805,  1356,  1357,   721,  1520,   199,
     815,   426,   726,   727,   728,   420,   730,   428,   734,   735,
     736,   420,   315,   395,   340,   251,   252,    68,    69,   418,
     910,   432,   844,   845,   820,   127,   435,   430,   848,   502,
     849,   420,   187,  1160,   437,   188,   189,   190,   191,   751,
     192,   193,   194,  1358,  1359,   853,    68,    69,  1432,    70,
     452,   855,   995,    99,   767,   768,   478,  1372,  1001,   469,
    1472,   569,   571,   574,   157,   475,   541,  1476,   478,    68,
      69,   260,    70,   779,   153,   154,   155,   479,   949,   785,
      68,    69,   790,    70,   120,   478,  1482,   941,   957,  1483,
     950,   482,   951,   153,   154,   155,   806,   492,    96,   933,
     496,    97,   207,  1423,   301,   302,   303,   976,  1551,  1474,
     499,   157,   478,  1558,   446,   542,   447,   448,   987,   988,
     992,  1266,  1267,   449,    98,    99,   821,   946,   947,   100,
     948,   101,  1050,   808,   809,   810,   984,   120,   102,   613,
     991,   502,   503,   503,   996,   998,  1000,  1161,  1037,   508,
    1557,   305,  1040,   306,   509,   103,   510,   307,   308,   614,
     615,  1095,  1096,   852,   241,   242,   243,   941,   195,   390,
     104,   153,   154,   155,    98,   511,   508,  1048,   512,   100,
    1166,   101,   857,   120,   196,   513,   509,   520,   102,   244,
    1167,  1564,   514,  1057,  1043,  1058,  1059,  1060,  1061,  1062,
    1381,  1382,  1383,  1045,   515,   103,   510,   546,   547,   548,
    1168,   511,   752,   157,   516,  1169,   954,   955,   956,   585,
     104,  1147,  1589,   694,   753,   754,   517,   522,   642,  1077,
    1159,  1079,   157,   531,  -242,   532,   545,  1601,   207,  1565,
     974,   536,   755,   549,   550,   551,   256,   257,   258,   259,
       3,   917,   918,   919,   591,   920,   921,   922,   923,   512,
     924,   925,   194,  1170,   926,   927,   928,   929,   699,   513,
     514,   930,   931,  1171,  1172,   579,  1028,   580,  1031,  1149,
    1033,  1598,  1034,   245,   632,   246,   247,   248,   249,   634,
     515,   688,   756,  1052,  1173,  1143,  1044,   305,   516,   306,
     643,     3,  1174,   307,   308,   645,  1046,  1047,   689,   595,
     157,   596,   597,   598,   196,  1081,  1082,  1083,  1084,  1085,
     217,   697,   517,  1176,  1239,  1184,  1175,  1177,  1063,   702,
    1064,  1065,   301,   302,   303,   703,  1202,  1192,  1205,   932,
      98,   704,     2,   705,  1075,   100,  1178,   101,   712,  1180,
    1179,   478,  1212,  1181,   102,  1264,   709,  1210,  1216,   661,
    1208,     3,  1278,  1279,  1280,  1281,  1282,  1211,   717,   478,
     722,   103,   993,  1587,  1299,   723,   724,   941,   105,  1300,
    1301,   284,   722,   568,  1226,  1235,   104,   674,   723,   724,
    1253,   725,  1255,   576,   567,  1225,  1234,   738,   673,   575,
    1148,  1219,  1220,  1221,  1222,   941,  1275,   301,   302,   303,
     566,  1224,  1233,   577,  1228,  1237,   746,   994,   749,   725,
     682,   683,   684,  1302,  1263,    11,    12,    13,    14,   207,
     750,  1186,   763,  1469,   504,   505,   506,   507,   764,  1347,
    1348,  1349,  1350,   774,   775,   820,   820,   776,   778,  1351,
    1352,  1353,  1029,   784,  1536,  1539,   685,   686,   687,   792,
    1011,   757,   793,   811,   758,     3,   812,  1012,   826,  1013,
    1014,  1015,  1199,   813,   814,   759,   817,   837,   823,    14,
     825,   301,   302,   303,  1162,  1163,  1164,  1165,   668,   676,
     827,  1213,   677,  1214,   828,   829,   830,     4,     5,     6,
       7,     8,   831,   833,   854,   834,   838,   846,  1016,  1017,
    1018,   847,    28,    29,    30,    31,    32,    33,    34,     9,
      10,   856,  1223,   907,  1544,   305,   908,   306,    35,  1182,
     850,   307,   308,   912,  1183,   911,    11,    12,    13,    14,
     913,   914,   915,    15,    16,   916,   939,   821,   821,    17,
     940,   971,    18,  1320,  1019,  1020,  1021,   952,  1022,    19,
      20,  1023,   953,    28,    29,    30,    31,    32,    33,    34,
    1344,  1345,  1273,   678,   958,   959,   960,  1336,   978,    35,
     961,   962,     3,   110,   979,  1002,   963,  1341,   964,   965,
     966,   967,   968,   110,   110,   110,   110,  1032,   969,  1375,
     305,   669,   306,   110,   110,   110,   307,   308,   986,   972,
     973,   977,  1008,  1041,    21,    22,  1042,    23,    24,    25,
    1036,    26,    27,    28,    29,    30,    31,    32,    33,    34,
    1049,  1051,  1426,  1053,  1054,  1290,   420,  1067,  1066,    35,
    1088,  1055,  1362,    14,  1068,  1089,  1069,  1070,  1071,  1365,
    1072,  1366,  1073,   670,  1297,  1298,   671,  1074,  1080,  1086,
    1303,  1304,  1305,  1306,  1307,  1308,  1309,  1310,  1311,  1312,
    1090,  1314,  1091,  1317,   305,  1321,   306,  1094,  1139,  1144,
     743,   308,  1153,  1154,  1155,  1187,  1184,  1436,  1193,  1194,
    1209,  1242,  1195,  1422,  1335,  1196,  1414,  1197,  1250,  1200,
    1416,     3,  1243,  1241,  1248,    28,    29,    30,    31,    32,
      33,    34,  1244,  1245,  1246,  1386,  1024,  1247,  1431,  1251,
     794,    35,  1252,  1256,  1259,  1260,  1261,    28,    29,    30,
      31,    32,    33,    34,  1262,  1355,  1270,   672,     3,   941,
       9,    10,  1274,    35,  1272,  1292,  1276,  1293,  1277,  1294,
    1328,  1327,  1329,  1330,  1367,  1331,   493,  1333,  1334,  1337,
      14,   668,  1338,  1369,  1182,  1342,  1343,   660,  1346,  1183,
    1373,  1374,   648,   552,   649,  1363,  1184,   650,   651,  1473,
    1364,  1368,  1377,  1475,  1370,  1371,  1380,  1384,  1376,  1387,
    1388,  1389,  1404,  1390,  1391,  1392,  1393,  1515,  1410,  1394,
     941,  1395,  1396,  1397,  1398,    68,    69,  1399,    70,  1400,
    1401,  1402,  1406,   127,  1403,  1405,   128,  1407,  1408,  1409,
     129,   130,   131,   132,   133,  1411,   134,   135,   136,   137,
    1511,   138,   139,  1412,  1420,   140,   141,   142,   143,  1417,
    1424,    99,   144,   145,    28,    29,    30,    31,    32,    33,
      34,   146,  1425,   147,   652,    96,  1428,  1430,    97,  1433,
      35,  1442,  1429,  1538,  1437,  1435,  1441,  1438,   148,   149,
     150,  1439,  1440,  1583,   669,  1445,  1443,  1594,  1444,    14,
    1552,    98,    99,  1447,  1449,  1448,   100,  1182,   101,  1453,
    1454,   661,  1183,   649,   662,   102,   650,   651,  1450,  1451,
    1452,  1455,  1456,   151,  1457,   301,   302,   303,  1458,  1459,
    1460,  1461,   103,  1462,  1463,  1464,    14,  1465,  1466,   486,
     487,  1467,  1468,  1470,  1471,  1477,   670,   104,  1481,   671,
     941,  1485,  1491,  1480,  1492,  1493,  1486,  1487,  1488,  1494,
      28,    29,    30,    31,    32,    33,    34,  1509,  1512,   937,
    1596,  1595,  1513,  1555,  1516,  1554,    35,  1563,  1585,   153,
     154,   155,  1517,    28,    29,    30,    31,    32,    33,    34,
    1518,  1582,   941,   663,  1584,  1586,  1588,  1591,  1597,    35,
    1592,  1593,  1600,   316,  1522,  1602,   543,   161,   706,  1528,
    1522,  1531,   631,  1535,  1003,  1528,  1522,  1531,  1361,   982,
      28,    29,    30,    31,    32,    33,    34,  1339,   163,  1201,
    1238,  1360,    68,    69,   832,    70,    35,  1528,  1522,  1531,
     127,  1257,   198,   128,  1140,   801,  1258,   129,   130,   131,
     132,   133,   581,   134,   135,   136,   137,  1291,   138,   139,
    1191,   938,   140,   141,   142,   143,  1484,  1326,    99,   144,
     145,   975,  1152,  1156,    94,   240,  1332,   769,   146,  1007,
     147,     0,   714,    28,    29,    30,    31,    32,    33,    34,
       0,     0,     0,  1446,     0,   148,   149,   150,   665,    35,
    1590,     0,     0,     0,     0,     0,     0,     0,   156,     0,
       0,     0,     0,     0,     0,     0,     0,     0,   488,     0,
     306,     0,     0,     0,   307,   308,     0,   158,     0,     0,
     151,     0,   301,   302,   303,     0,     0,    68,    69,     0,
      70,     0,     0,     0,     3,   127,   486,   487,   128,     0,
       0,     0,   129,   130,   131,   132,   133,     0,   134,   135,
     136,   137,     0,   138,   139,     0,     0,   140,   141,   142,
     143,     0,     0,    99,   144,   145,     0,     0,     0,     0,
       0,     0,     0,   146,     0,   147,   153,   154,   155,   217,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
     148,   149,   150,     0,   269,   270,   271,     0,   272,   273,
     274,   275,     0,   276,   277,   194,     0,   278,   279,   280,
     281,  1097,     0,     0,     0,     0,   282,     0,     0,     0,
       0,     0,     0,     0,     0,   151,     0,   301,   302,   303,
    1098,  1099,     0,  1100,  1101,  1102,  1103,  1104,  1105,     0,
    1106,  1107,     0,  1108,  1109,  1110,  1111,  1112,     0,   622,
     128,   623,     0,     0,   129,   130,   131,   132,   133,     0,
     134,   135,   136,   137,     0,   138,   139,     0,     0,   140,
     141,   142,   143,     0,     0,    99,   144,   145,     0,     0,
       0,   153,   154,   155,     0,   146,     0,   147,  1229,     0,
    1230,  1231,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   148,   149,   150,   156,     0,     0,     0,    11,
      12,    13,    14,     0,     0,   488,     0,   306,     0,     0,
       0,   743,   308,     0,   158,    68,    69,     0,    70,     0,
       0,     0,     0,   127,     0,     0,   128,   151,     0,     0,
     129,   130,   131,   132,   133,     0,   134,   135,   136,   137,
       0,   138,   139,     0,     0,   140,   141,   142,   143,     0,
       0,    99,   144,   145,     0,     0,     0,     0,     0,     0,
       0,   146,     0,   147,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,   148,   149,
     150,     0,     0,     0,     0,     0,    28,    29,    30,    31,
      32,    33,    34,     0,     0,     0,  1232,     0,     0,     0,
     156,     0,    35,     0,     0,     0,     0,     0,     0,     0,
     488,     0,   306,   151,   152,     0,   307,   308,     0,   158,
       0,     0,     0,     0,     0,     0,     0,     0,  1113,  1114,
       0,  1115,  1116,  1117,  1011,  1118,  1119,     0,     0,  1120,
    1121,  1012,  1122,  1013,  1014,  1015,     0,     0,     0,     0,
       0,     0,     0,     0,     0,  1123,  1124,  1125,  1126,  1127,
    1128,  1129,  1130,  1131,  1132,  1133,  1134,  1135,  1136,   153,
     154,   155,     0,     0,     0,     0,     0,     0,    68,    69,
       0,    70,  1016,  1017,  1018,     0,   127,     0,     0,   128,
       0,     0,     0,   129,   130,   131,   132,   133,     0,   134,
     135,   136,   137,  1137,   138,   139,    14,     0,   140,   141,
     142,   143,   156,     0,    99,   144,   145,     0,     0,     0,
       0,     0,     0,     0,   146,     0,   147,     0,  1019,  1020,
    1021,   158,  1022,     0,     0,  1023,     0,     0,     0,     0,
       0,   148,   149,   150,     0,   989,     0,    68,    69,     0,
      70,     0,     0,     0,     0,   127,     0,     0,   128,     0,
       0,     0,   129,   130,   131,   132,   133,     0,   134,   135,
     136,   137,     0,   138,   139,     0,   151,   140,   141,   142,
     143,     0,     0,    99,   144,   145,     0,     0,     0,     0,
     990,     0,     0,   146,     0,   147,     0,     0,   156,     0,
       0,     0,     0,     0,     0,     0,     0,     0,   157,     0,
     148,   149,   150,     0,     0,     0,     0,   158,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   153,   154,   155,     0,     0,     0,     0,     0,
       0,    68,    69,     0,    70,   151,   152,     0,     0,   127,
       0,     0,   128,     0,     0,     0,   129,   130,   131,   132,
     133,     0,   134,   135,   136,   137,     0,   138,   139,     0,
       0,   140,   141,   142,   143,     0,     0,    99,   144,   145,
       0,     0,     0,     0,     0,     0,     0,   146,     0,   147,
    1240,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,   153,   154,   155,   148,   149,   150,     0,     0,     0,
      68,    69,     0,    70,     0,     0,     0,     0,   127,     0,
       0,   128,     0,     0,     0,   129,   130,   131,   132,   133,
       0,   134,   135,   136,   137,     0,   138,   139,     0,   151,
     140,   141,   142,   143,     0,     0,    99,   144,   145,     0,
       0,     0,     0,     0,     0,     0,   146,     0,   147,     0,
       0,   156,     0,     0,     0,     0,     0,     0,     0,     0,
       0,   211,     0,   148,   149,   150,     0,     0,     0,     0,
     158,     0,     0,    68,    69,     0,    70,     0,     0,     0,
       0,   127,     0,     0,   128,   153,   154,   155,   129,   130,
     131,   132,   133,     0,   134,   135,   136,   137,   589,   138,
     139,     0,     0,   140,   141,   142,   143,     0,     0,    99,
     144,   145,     0,     0,     0,     0,     0,     0,     0,   698,
     156,   147,     0,     0,     0,     0,     0,     0,     0,     0,
     211,     0,     0,     0,     0,     0,   148,   149,   150,   158,
       0,     0,    68,    69,     0,    70,     0,     0,     0,     0,
     127,     0,     0,   128,   153,   154,   155,   129,   130,   131,
     132,   133,     0,   134,   135,   136,   137,     0,   138,   139,
       0,   151,   140,   141,   142,   143,     0,     0,    99,   144,
     145,     0,     0,     0,     0,     0,     0,     0,   146,    68,
     147,     0,    70,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     3,     0,     0,   148,   149,   150,     0,     0,
       0,     0,     0,     0,   156,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   211,     0,     0,   153,   154,   155,
       0,     0,     0,   158,     0,     0,   266,   128,   267,     0,
     772,   129,   130,   131,   132,   133,     0,   134,   135,   136,
     137,     0,   138,   139,     0,     0,   140,   141,   142,   143,
       0,     0,     0,   144,   145,     0,     0,     0,     0,     0,
       0,     0,   146,     0,   147,     0,     0,     0,     0,     0,
       0,     0,     0,   156,     0,     0,     0,     0,     0,   148,
     149,   150,     0,   211,     0,     0,   153,   154,   155,   917,
     918,   919,   158,   920,   921,   922,   923,     0,   924,   925,
     194,     0,   926,   927,   928,   929,     0,     0,     0,   930,
     931,     0,     0,     0,   151,   364,   365,   366,   367,   368,
     369,   370,   371,   372,   373,   374,   375,   376,     0,     0,
       0,     0,     8,     0,     0,     0,   377,   378,   379,   380,
     381,   382,     0,     0,    68,     0,   156,    70,     0,     0,
       9,    10,     0,     0,     0,     0,   211,     3,     0,     0,
       0,     0,     0,     0,     0,   158,     0,    11,    12,    13,
      14,     0,     0,     0,   128,     0,     0,   932,   383,     0,
     131,   132,   133,     0,   134,   135,   136,   137,     0,   138,
     139,     0,   384,   140,   141,   142,   143,     0,     0,     0,
    1322,   145,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,   156,     0,     0,     0,     0,
       0,     0,     0,     0,     0,   211,     0,   385,   386,  1011,
       0,     0,     0,     0,   158,     0,  1012,     0,  1013,  1014,
    1015,     0,     0,     0,     0,     0,     0,     0,     0,  1323,
       0,     0,     0,     0,    28,    29,    30,    31,    32,    33,
      34,     0,     0,   387,   388,     0,     0,     0,  1324,     0,
      35,     0,     0,     0,     0,     0,     0,  1016,  1017,  1018,
     364,   365,   366,   367,   368,   369,   370,   371,   372,   373,
     374,   375,   376,     0,     0,     0,     0,     8,     0,   156,
       0,   377,   378,   379,   380,   381,   382,    68,     0,     0,
      70,     0,     0,     0,     0,     9,    10,     0,   158,     0,
       3,     0,     0,  1019,  1020,  1021,     0,  1022,     0,     0,
    1023,     0,    11,    12,    13,    14,     0,     0,     0,     0,
       0,     0,     0,   383,     0,     0,     0,    28,    29,    30,
      31,    32,    33,    34,   128,     0,     0,   384,   129,   130,
     131,   132,   133,    35,   134,   135,   136,   137,     0,   138,
     139,     0,     0,   140,   141,   142,   143,     0,     0,     0,
     144,   145,     0,     0,     0,     0,     0,     0,     0,   146,
       0,   147,   385,   386,     0,     0,     0,     0,     0,     0,
       0,     0,  1566,     0,     0,     0,   148,   149,   150,     0,
       0,     0,     0,     0,     0,     0,     0,  1567,     0,    28,
      29,    30,    31,    32,    33,    34,     0,     0,   387,   799,
       0,     0,     0,  1568,     0,    35,     0,     0,     0,     0,
       0,   151,  1569,   364,   365,   366,   367,   368,   369,   370,
     371,   372,   373,   374,   375,   376,  1570,  1571,  1572,  1573,
       8,     0,     0,     0,   377,   378,   379,   380,   381,   382,
       0,     0,     0,     0,     0,     0,     0,     0,     9,    10,
       0,     0,     0,     0,     0,  1427,     0,     0,  1574,  1575,
    1576,  1577,  1578,  1579,  1580,    11,    12,    13,    14,     0,
       0,     0,     0,     0,     0,     0,   383,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   128,     0,     0,
     384,   129,   130,   131,   132,   133,     0,   134,   135,   136,
     137,     0,   138,   139,     0,     0,   140,   141,   142,   143,
     453,     0,     0,   144,   145,     0,     0,     0,     0,     0,
       0,     0,   146,     0,   147,   385,   386,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,   148,
     149,   150,     0,   454,     0,   455,   456,   457,   458,     0,
       0,     0,    28,    29,    30,    31,    32,    33,    34,     0,
       0,   387,  1006,     0,     0,     0,     0,     0,    35,     0,
       0,     0,     0,     0,   151,     0,     0,     0,     0,     0,
       0,     0,   459,   460,   461,   462,     0,     0,   463,     0,
       0,     0,   464,   465,   466,     0,   156,   128,     0,     0,
       0,   129,   130,   131,   132,   133,   782,   134,   135,   136,
     137,     0,   138,   139,     0,   158,   140,   141,   142,   143,
       0,     0,  1581,   144,   145,     0,     0,     0,     0,     3,
       0,     0,   146,     0,   147,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,   128,   148,
     149,   150,   129,   130,   131,   132,   133,     0,   134,   135,
     136,   137,     0,   138,   139,     0,     0,   140,   141,   142,
     143,     0,     0,     0,   144,   145,     0,     0,     0,     0,
       0,     0,     0,   146,   151,   147,     0,     0,     0,     0,
       0,     0,     0,     0,     0,   858,   467,     0,     0,     0,
     148,   149,   150,     0,     0,     0,     0,     0,     0,   859,
       0,     0,     0,     0,   860,   861,     0,   862,   863,   864,
     865,   866,   867,     0,   868,   869,     0,   870,   871,   872,
     873,   874,     0,     0,     0,   151,    78,    79,    80,    81,
      82,    83,    84,    85,    86,    87,    88,    89,    90,    91,
      92,     0,     0,     0,     0,    68,    69,     0,    70,   156,
       0,     0,     0,     0,     0,     4,     5,     6,     7,     8,
       0,     0,     0,   875,     0,   876,     0,     0,   158,     0,
     877,     0,     0,     0,     0,     0,     0,     9,    10,   317,
       0,     0,     0,     0,     0,     0,   878,     0,     0,     0,
       0,     0,     0,     0,    11,    12,    13,    14,     3,     0,
       0,    15,    16,     0,     0,     0,     0,    17,   318,     0,
      18,     0,   319,     0,     0,   320,   321,    19,    20,   879,
     322,   323,   324,   325,   326,   327,   328,   329,   330,   331,
     332,   333,     0,     0,     0,     0,     0,     0,   334,     0,
       0,   335,     0,     0,     3,     0,     0,   556,   336,     0,
       0,     0,     0,     0,     0,     0,     0,   337,     0,   156,
       0,     0,     0,     0,     0,     0,     0,     0,     0,   937,
       0,     0,    21,    22,     0,    23,    24,    25,   158,    26,
      27,    28,    29,    30,    31,    32,    33,    34,     0,     0,
       0,   544,     0,   556,     0,     0,     0,    35,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,   880,
     156,   881,   882,   883,   884,   885,   886,   887,   888,   889,
     890,   891,   892,   893,   894,   895,   896,   897,     0,   158,
       0,   898,     0,     0,   557,     0,     6,     7,     8,     0,
       0,     0,     0,     0,     0,     0,     0,     0,   558,     0,
       0,     0,     0,   559,     0,     0,     9,    10,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   899,    11,    12,    13,    14,     0,   560,   561,
     557,     0,     6,     7,     8,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   558,     0,     0,     0,   562,   559,
       0,     0,     9,    10,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,   858,     0,     0,    11,
      12,    13,    14,     0,   560,   561,     0,     0,     0,     0,
     859,     0,     0,   563,   564,   860,   861,     0,   862,   863,
     864,   865,   866,   867,   562,   868,   869,     0,   870,   871,
     872,   873,   874,     0,     0,     0,     0,     0,     0,     0,
      28,    29,    30,    31,    32,    33,    34,     0,     0,     0,
     565,     0,     0,     0,     0,     0,    35,     0,     0,   563,
     564,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   875,     0,   876,     0,     0,     0,
       0,   877,     0,     0,     0,     0,    28,    29,    30,    31,
      32,    33,    34,     0,     0,     0,  1218,   878,     0,   917,
     918,   919,    35,   920,   921,   922,   923,     0,   924,   925,
     194,     0,   926,   927,   928,   929,     0,   343,    98,   930,
     931,     0,     0,   100,     0,   101,     0,     0,     0,     0,
     879,     0,   102,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,   103,
     344,     0,   345,   346,   347,   348,   349,     0,     0,     0,
       0,   350,     0,     0,   104,     0,     0,     0,     0,     0,
     351,     0,     0,     0,     0,   352,     0,   353,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   932,     0,   354,
     355,   356,   357,   358,   359,   360,   361,     0,     0,     0,
       0,     0,   362,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
     880,     0,   881,   882,   883,   884,   885,   886,   887,   888,
     889,   890,   891,   892,   893,   894,   895,   896,   897,     0,
       0,     0,   898
};

static const yytype_int16 yycheck[] =
{
       5,     1,    25,   428,   164,   430,    23,    24,    14,   103,
     104,     1,     1,   156,    25,    20,   712,     1,    25,    25,
     342,   342,   342,    28,    29,    30,    31,    43,     1,    94,
     697,     1,    48,    57,   490,   906,  1036,  1176,    25,   780,
     396,    10,   186,    59,  1067,   520,   521,   522,     6,     7,
     793,     9,    10,     8,     9,     8,     6,     7,    10,     9,
      21,    10,    13,    57,     6,     7,   198,     9,    93,    21,
     164,    10,     6,     7,    10,     9,   111,     8,     8,   111,
       6,     7,    10,     9,    10,   108,     9,   512,   513,    77,
      64,   185,    42,     8,   137,    21,   792,     6,     7,   105,
       9,  1155,   216,    34,   137,   170,   129,    10,    42,   114,
      10,   116,   117,   118,     6,     7,    42,     9,   105,   213,
       9,    64,    45,    20,     6,     7,     8,     9,    10,   152,
       6,     7,    14,     9,     8,    21,    22,  1208,    10,   146,
       6,     7,   149,     9,     9,     6,     7,    29,     9,   167,
      10,   157,   157,   158,    10,    11,    10,   182,   164,   299,
      42,  1474,   169,   295,   106,   107,   108,    21,   233,     6,
       7,   195,     9,     9,    60,    10,   181,   189,   190,   191,
     192,   193,    10,   199,    66,    67,   209,    73,    65,    10,
      11,    12,    10,    10,    12,    10,   939,   940,   186,    10,
      11,   206,   207,    10,    11,   211,   211,    10,    11,   234,
     137,    32,    33,    10,    32,    33,     9,   692,   574,    10,
      11,    12,     6,     7,   292,     9,   112,   113,   971,   234,
       6,   294,   300,     9,    10,   240,    66,    67,   301,  1454,
    1294,    32,    33,   268,     8,  1460,   228,   407,   298,   126,
     299,   294,    45,   288,   261,   399,   288,     6,     7,     6,
       9,   294,     9,   268,   739,  1336,   248,   299,   218,   285,
      34,   274,   158,  1462,     8,   298,   294,  1492,   160,   161,
     162,    12,   287,   306,   218,  1598,   291,  1461,   216,   294,
     295,   296,   218,  1459,   299,   300,   290,   293,    40,   304,
     216,    32,    33,   310,   300,  1494,  1002,    66,    67,  1052,
      52,    53,   251,   252,   253,   340,   316,   411,     6,  1493,
     294,     9,    10,   292,    88,  1491,   316,   316,    70,    10,
    1469,   300,   316,   292,  1030,   296,   222,   223,   293,   298,
     291,   294,   342,   316,   290,   410,   316,  1370,   300,   304,
     308,   294,   342,   342,    13,   305,   305,   299,   342,   301,
     296,   386,    61,   305,   306,   292,  1505,   298,   298,   342,
     290,   305,   342,   296,   300,   290,   851,   294,   120,   305,
     295,   293,   294,   300,   409,   408,   410,   404,   405,   294,
     299,     6,     7,   296,     9,   300,   296,   294,   414,    14,
     295,   407,   408,   298,   293,   291,   412,   299,   473,   426,
     307,   399,    25,    26,   402,   290,   290,   299,   406,   305,
     299,   295,    35,   299,   296,   412,   305,    42,   293,   298,
     905,   295,   216,   299,   295,   299,   296,    52,     6,     7,
     296,     9,   442,   443,   444,   445,    14,   435,   290,   472,
     594,   476,   442,   442,   443,   444,   445,  1480,   602,   208,
     292,   296,   299,   468,   296,   470,   296,   294,   296,     6,
       7,   290,     9,   300,    42,   296,   482,    45,   296,   296,
     290,   296,   488,   488,    52,   296,   942,   293,   294,   296,
    1157,   377,   497,   379,   511,   589,   513,   383,  1239,   296,
     517,   290,   294,   528,   295,   290,   531,   532,   300,  1509,
     295,   293,   294,   399,   290,   580,   402,   403,     6,     7,
     406,     9,    24,   528,   294,    27,    28,    29,    30,   295,
      32,    33,    34,   419,   420,   271,   422,   293,   424,   564,
       6,     7,   295,     9,   295,   160,   161,   162,    14,    47,
     292,    49,    50,    51,   440,   441,    52,   293,   581,     6,
       7,   303,     9,   299,   137,     6,     7,    14,     9,   634,
     306,   596,   137,    29,    30,   137,    42,   307,   106,   107,
     108,   586,   587,   120,   121,   137,   651,  1330,  1331,    10,
      11,    12,   160,   161,   162,    42,   296,   295,    45,   295,
     625,   299,   488,   299,    13,    52,   594,   630,    64,   137,
     296,    32,    33,   629,   602,   139,   140,   503,  1489,   643,
     625,   295,   508,   509,   510,   299,   512,   295,   514,   515,
     516,   299,   296,   777,    62,    76,    77,     6,     7,   305,
       9,   295,   658,   659,   632,    14,   295,   295,   664,   295,
     666,   299,    24,   299,   296,    27,    28,    29,    30,   545,
      32,    33,    34,   139,   140,   670,     6,     7,  1364,     9,
     300,   676,   766,    42,   560,   561,   292,   293,   772,   298,
    1423,  1003,  1003,  1003,   299,   295,   709,  1430,   292,     6,
       7,   698,     9,   579,   160,   161,   162,     9,   714,   585,
       6,     7,   588,     9,    10,   292,   293,   712,   725,   296,
     715,   296,   717,   160,   161,   162,   602,   300,    15,   708,
     221,    18,   294,   295,   106,   107,   108,   750,   296,  1425,
      26,   299,   292,   293,    74,   291,    76,    77,   763,   764,
     765,  1053,  1054,    83,    41,    42,   632,   203,   204,    46,
     206,    48,   817,   603,   604,   605,   761,    10,    55,    12,
     765,   295,   295,   295,   769,   770,   771,   299,   791,   295,
    1513,   299,   795,   301,   295,    72,   295,   305,   306,    32,
      33,   893,   894,   669,   101,   102,   103,   792,   290,   777,
      87,   160,   161,   162,    41,   295,   295,   814,   295,    46,
     299,    48,   688,    10,   306,   295,   295,   300,    55,   126,
     299,  1554,   295,   825,   802,   827,   828,   829,   830,   831,
      27,    28,    29,   811,   295,    72,   295,    76,    77,    78,
     299,   295,    40,   299,   295,   299,   722,   723,   724,   299,
      87,   906,  1585,   908,    52,    53,   295,   300,   291,   854,
     915,   856,   299,   295,   290,   295,   295,  1600,   294,  1555,
     746,   747,    70,   112,   113,   114,    27,    28,    29,    30,
      19,    23,    24,    25,   304,    27,    28,    29,    30,   295,
      32,    33,    34,   299,    36,    37,    38,    39,   913,   295,
     295,    43,    44,   299,   299,   295,   782,   295,   784,   906,
     786,  1597,   788,   220,   295,   222,   223,   224,   225,   295,
     295,   295,   120,   295,   299,   903,   802,   299,   295,   301,
     293,    19,   299,   305,   306,   296,   812,   813,   295,    56,
     299,    58,    59,    60,   306,    26,    27,    28,    29,    30,
       8,   290,   295,   295,  1009,   934,   299,   299,   834,     8,
     836,   837,   106,   107,   108,   293,   981,   945,   983,   111,
      41,   296,     0,   296,   850,    46,   295,    48,    20,   295,
     299,   292,   995,   299,    55,   296,   301,   988,  1001,   209,
     985,    19,    26,    27,    28,    29,    30,   992,    20,   292,
     295,    72,    73,   296,   299,   295,   295,  1002,   295,   299,
     299,  1144,   295,  1003,  1004,  1005,    87,  1007,   295,   295,
    1035,   295,  1037,  1003,  1003,  1004,  1005,   296,  1007,  1003,
     906,   170,   171,   172,   173,  1030,  1091,   106,   107,   108,
    1003,  1004,  1005,  1003,  1004,  1005,    21,   118,   302,   295,
      76,    77,    78,   299,  1049,   194,   195,   196,   197,   294,
     293,   937,   293,   298,    27,    28,    29,    30,   111,  1219,
    1220,  1221,  1222,   290,   290,  1053,  1054,   290,   290,  1229,
    1230,  1231,   226,   304,  1499,  1500,   112,   113,   114,    20,
      40,   289,   295,    63,   292,    19,    63,    47,   300,    49,
      50,    51,   978,   295,   295,   303,   295,   213,   296,   197,
     296,   106,   107,   108,    27,    28,    29,    30,    42,   207,
     296,   997,   210,   999,   296,   296,   296,   155,   156,   157,
     158,   159,   296,   296,   216,   304,   298,   298,    88,    89,
      90,   298,   281,   282,   283,   284,   285,   286,   287,   177,
     178,   216,   291,   296,   296,   299,   293,   301,   297,   105,
     295,   305,   306,   291,   110,   298,   194,   195,   196,   197,
     293,     9,   298,   201,   202,   291,   295,  1053,  1054,   207,
     295,   295,   210,  1180,   134,   135,   136,   296,   138,   217,
     218,   141,   296,   281,   282,   283,   284,   285,   286,   287,
    1215,  1216,  1078,   291,   296,   296,   296,  1202,   295,   297,
     296,   296,    19,  1209,    40,    20,   296,  1212,   296,   296,
     296,   296,   296,  1219,  1220,  1221,  1222,   300,   296,  1284,
     299,   155,   301,  1229,  1230,  1231,   305,   306,   302,   296,
     296,   296,   296,   296,   272,   273,   293,   275,   276,   277,
     300,   279,   280,   281,   282,   283,   284,   285,   286,   287,
      20,   296,  1346,   293,   293,  1141,   299,   304,   295,   297,
     243,   296,  1250,   197,   295,   235,   296,   296,   295,  1257,
     295,  1259,   296,   207,  1160,  1161,   210,   296,   296,   295,
    1166,  1167,  1168,  1169,  1170,  1171,  1172,  1173,  1174,  1175,
     247,  1177,   295,  1179,   299,  1181,   301,    23,   296,   298,
     305,   306,   291,   298,   290,   302,  1295,  1372,   296,   296,
     197,   304,   296,  1338,  1200,   296,  1323,   296,   137,   295,
    1325,    19,   304,   295,   295,   281,   282,   283,   284,   285,
     286,   287,   300,   300,   300,   291,   296,   300,  1363,   300,
      21,   297,   300,   296,    63,    63,   296,   281,   282,   283,
     284,   285,   286,   287,   296,  1241,     9,   291,    19,  1364,
     177,   178,   250,   297,   296,   295,   299,   295,   299,   290,
     296,   300,   296,   295,  1260,   295,   167,   296,   296,   296,
     197,    42,   293,  1269,   105,   300,   300,    85,   256,   110,
    1276,  1277,   209,   301,   211,   293,  1385,   214,   215,  1424,
      20,   296,  1288,  1426,   304,   298,  1292,  1293,   300,   291,
     300,   300,   296,  1299,  1300,  1301,  1302,  1482,     9,   300,
    1425,   300,   300,   300,   300,     6,     7,   300,     9,   300,
     300,   300,   296,    14,   300,   300,    17,   296,   300,   296,
      21,    22,    23,    24,    25,   296,    27,    28,    29,    30,
    1475,    32,    33,   300,   296,    36,    37,    38,    39,   302,
     293,    42,    43,    44,   281,   282,   283,   284,   285,   286,
     287,    52,    20,    54,   291,    15,   296,   295,    18,     9,
     297,   304,   296,  1500,   300,  1371,   296,   300,    69,    70,
      71,   300,  1378,  1558,   155,   296,   304,  1591,   304,   197,
    1507,    41,    42,   295,   300,   295,    46,   105,    48,   295,
     295,   209,   110,   211,   212,    55,   214,   215,   300,   300,
     300,   295,   295,   104,   295,   106,   107,   108,   295,   295,
     295,   295,    72,   295,   295,   295,   197,   296,   295,   120,
     121,     9,   300,   296,   296,   301,   207,    87,   296,   210,
    1555,   300,   295,   304,   295,   295,  1442,  1443,  1444,   295,
     281,   282,   283,   284,   285,   286,   287,   296,   296,   299,
    1595,  1594,   295,    20,   296,   295,   297,     9,   295,   160,
     161,   162,   296,   281,   282,   283,   284,   285,   286,   287,
     296,   296,  1597,   291,   296,   295,   256,   104,    20,   297,
     296,   296,   295,   180,  1490,   296,   310,    25,   482,  1495,
    1496,  1497,   412,  1499,   774,  1501,  1502,  1503,  1249,   760,
     281,   282,   283,   284,   285,   286,   287,  1206,    25,   980,
     291,  1248,     6,     7,   643,     9,   297,  1523,  1524,  1525,
      14,  1040,    57,    17,   901,   600,  1042,    21,    22,    23,
      24,    25,   363,    27,    28,    29,    30,  1144,    32,    33,
     942,   709,    36,    37,    38,    39,  1438,  1185,    42,    43,
      44,   747,   908,   913,    10,   115,  1193,   562,    52,   778,
      54,    -1,   494,   281,   282,   283,   284,   285,   286,   287,
      -1,    -1,    -1,   291,    -1,    69,    70,    71,   443,   297,
    1586,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   289,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   299,    -1,
     301,    -1,    -1,    -1,   305,   306,    -1,   308,    -1,    -1,
     104,    -1,   106,   107,   108,    -1,    -1,     6,     7,    -1,
       9,    -1,    -1,    -1,    19,    14,   120,   121,    17,    -1,
      -1,    -1,    21,    22,    23,    24,    25,    -1,    27,    28,
      29,    30,    -1,    32,    33,    -1,    -1,    36,    37,    38,
      39,    -1,    -1,    42,    43,    44,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    52,    -1,    54,   160,   161,   162,     8,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      69,    70,    71,    -1,    23,    24,    25,    -1,    27,    28,
      29,    30,    -1,    32,    33,    34,    -1,    36,    37,    38,
      39,     3,    -1,    -1,    -1,    -1,    45,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   104,    -1,   106,   107,   108,
      22,    23,    -1,    25,    26,    27,    28,    29,    30,    -1,
      32,    33,    -1,    35,    36,    37,    38,    39,    -1,    16,
      17,    18,    -1,    -1,    21,    22,    23,    24,    25,    -1,
      27,    28,    29,    30,    -1,    32,    33,    -1,    -1,    36,
      37,    38,    39,    -1,    -1,    42,    43,    44,    -1,    -1,
      -1,   160,   161,   162,    -1,    52,    -1,    54,   173,    -1,
     175,   176,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    69,    70,    71,   289,    -1,    -1,    -1,   194,
     195,   196,   197,    -1,    -1,   299,    -1,   301,    -1,    -1,
      -1,   305,   306,    -1,   308,     6,     7,    -1,     9,    -1,
      -1,    -1,    -1,    14,    -1,    -1,    17,   104,    -1,    -1,
      21,    22,    23,    24,    25,    -1,    27,    28,    29,    30,
      -1,    32,    33,    -1,    -1,    36,    37,    38,    39,    -1,
      -1,    42,    43,    44,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    52,    -1,    54,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    69,    70,
      71,    -1,    -1,    -1,    -1,    -1,   281,   282,   283,   284,
     285,   286,   287,    -1,    -1,    -1,   291,    -1,    -1,    -1,
     289,    -1,   297,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     299,    -1,   301,   104,   105,    -1,   305,   306,    -1,   308,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   230,   231,
      -1,   233,   234,   235,    40,   237,   238,    -1,    -1,   241,
     242,    47,   244,    49,    50,    51,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   257,   258,   259,   260,   261,
     262,   263,   264,   265,   266,   267,   268,   269,   270,   160,
     161,   162,    -1,    -1,    -1,    -1,    -1,    -1,     6,     7,
      -1,     9,    88,    89,    90,    -1,    14,    -1,    -1,    17,
      -1,    -1,    -1,    21,    22,    23,    24,    25,    -1,    27,
      28,    29,    30,   305,    32,    33,   197,    -1,    36,    37,
      38,    39,   289,    -1,    42,    43,    44,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    52,    -1,    54,    -1,   134,   135,
     136,   308,   138,    -1,    -1,   141,    -1,    -1,    -1,    -1,
      -1,    69,    70,    71,    -1,    73,    -1,     6,     7,    -1,
       9,    -1,    -1,    -1,    -1,    14,    -1,    -1,    17,    -1,
      -1,    -1,    21,    22,    23,    24,    25,    -1,    27,    28,
      29,    30,    -1,    32,    33,    -1,   104,    36,    37,    38,
      39,    -1,    -1,    42,    43,    44,    -1,    -1,    -1,    -1,
     118,    -1,    -1,    52,    -1,    54,    -1,    -1,   289,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   299,    -1,
      69,    70,    71,    -1,    -1,    -1,    -1,   308,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   160,   161,   162,    -1,    -1,    -1,    -1,    -1,
      -1,     6,     7,    -1,     9,   104,   105,    -1,    -1,    14,
      -1,    -1,    17,    -1,    -1,    -1,    21,    22,    23,    24,
      25,    -1,    27,    28,    29,    30,    -1,    32,    33,    -1,
      -1,    36,    37,    38,    39,    -1,    -1,    42,    43,    44,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    52,    -1,    54,
     296,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   160,   161,   162,    69,    70,    71,    -1,    -1,    -1,
       6,     7,    -1,     9,    -1,    -1,    -1,    -1,    14,    -1,
      -1,    17,    -1,    -1,    -1,    21,    22,    23,    24,    25,
      -1,    27,    28,    29,    30,    -1,    32,    33,    -1,   104,
      36,    37,    38,    39,    -1,    -1,    42,    43,    44,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    52,    -1,    54,    -1,
      -1,   289,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   299,    -1,    69,    70,    71,    -1,    -1,    -1,    -1,
     308,    -1,    -1,     6,     7,    -1,     9,    -1,    -1,    -1,
      -1,    14,    -1,    -1,    17,   160,   161,   162,    21,    22,
      23,    24,    25,    -1,    27,    28,    29,    30,   104,    32,
      33,    -1,    -1,    36,    37,    38,    39,    -1,    -1,    42,
      43,    44,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    52,
     289,    54,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     299,    -1,    -1,    -1,    -1,    -1,    69,    70,    71,   308,
      -1,    -1,     6,     7,    -1,     9,    -1,    -1,    -1,    -1,
      14,    -1,    -1,    17,   160,   161,   162,    21,    22,    23,
      24,    25,    -1,    27,    28,    29,    30,    -1,    32,    33,
      -1,   104,    36,    37,    38,    39,    -1,    -1,    42,    43,
      44,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    52,     6,
      54,    -1,     9,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    19,    -1,    -1,    69,    70,    71,    -1,    -1,
      -1,    -1,    -1,    -1,   289,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   299,    -1,    -1,   160,   161,   162,
      -1,    -1,    -1,   308,    -1,    -1,    16,    17,    18,    -1,
     104,    21,    22,    23,    24,    25,    -1,    27,    28,    29,
      30,    -1,    32,    33,    -1,    -1,    36,    37,    38,    39,
      -1,    -1,    -1,    43,    44,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    52,    -1,    54,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   289,    -1,    -1,    -1,    -1,    -1,    69,
      70,    71,    -1,   299,    -1,    -1,   160,   161,   162,    23,
      24,    25,   308,    27,    28,    29,    30,    -1,    32,    33,
      34,    -1,    36,    37,    38,    39,    -1,    -1,    -1,    43,
      44,    -1,    -1,    -1,   104,   142,   143,   144,   145,   146,
     147,   148,   149,   150,   151,   152,   153,   154,    -1,    -1,
      -1,    -1,   159,    -1,    -1,    -1,   163,   164,   165,   166,
     167,   168,    -1,    -1,     6,    -1,   289,     9,    -1,    -1,
     177,   178,    -1,    -1,    -1,    -1,   299,    19,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   308,    -1,   194,   195,   196,
     197,    -1,    -1,    -1,    17,    -1,    -1,   111,   205,    -1,
      23,    24,    25,    -1,    27,    28,    29,    30,    -1,    32,
      33,    -1,   219,    36,    37,    38,    39,    -1,    -1,    -1,
      43,    44,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   289,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   299,    -1,   254,   255,    40,
      -1,    -1,    -1,    -1,   308,    -1,    47,    -1,    49,    50,
      51,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    92,
      -1,    -1,    -1,    -1,   281,   282,   283,   284,   285,   286,
     287,    -1,    -1,   290,   291,    -1,    -1,    -1,   111,    -1,
     297,    -1,    -1,    -1,    -1,    -1,    -1,    88,    89,    90,
     142,   143,   144,   145,   146,   147,   148,   149,   150,   151,
     152,   153,   154,    -1,    -1,    -1,    -1,   159,    -1,   289,
      -1,   163,   164,   165,   166,   167,   168,     6,    -1,    -1,
       9,    -1,    -1,    -1,    -1,   177,   178,    -1,   308,    -1,
      19,    -1,    -1,   134,   135,   136,    -1,   138,    -1,    -1,
     141,    -1,   194,   195,   196,   197,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   205,    -1,    -1,    -1,   281,   282,   283,
     284,   285,   286,   287,    17,    -1,    -1,   219,    21,    22,
      23,    24,    25,   297,    27,    28,    29,    30,    -1,    32,
      33,    -1,    -1,    36,    37,    38,    39,    -1,    -1,    -1,
      43,    44,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    52,
      -1,    54,   254,   255,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    40,    -1,    -1,    -1,    69,    70,    71,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    55,    -1,   281,
     282,   283,   284,   285,   286,   287,    -1,    -1,   290,   291,
      -1,    -1,    -1,    71,    -1,   297,    -1,    -1,    -1,    -1,
      -1,   104,    80,   142,   143,   144,   145,   146,   147,   148,
     149,   150,   151,   152,   153,   154,    94,    95,    96,    97,
     159,    -1,    -1,    -1,   163,   164,   165,   166,   167,   168,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   177,   178,
      -1,    -1,    -1,    -1,    -1,   296,    -1,    -1,   126,   127,
     128,   129,   130,   131,   132,   194,   195,   196,   197,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   205,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    17,    -1,    -1,
     219,    21,    22,    23,    24,    25,    -1,    27,    28,    29,
      30,    -1,    32,    33,    -1,    -1,    36,    37,    38,    39,
      40,    -1,    -1,    43,    44,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    52,    -1,    54,   254,   255,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    69,
      70,    71,    -1,    73,    -1,    75,    76,    77,    78,    -1,
      -1,    -1,   281,   282,   283,   284,   285,   286,   287,    -1,
      -1,   290,   291,    -1,    -1,    -1,    -1,    -1,   297,    -1,
      -1,    -1,    -1,    -1,   104,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   112,   113,   114,   115,    -1,    -1,   118,    -1,
      -1,    -1,   122,   123,   124,    -1,   289,    17,    -1,    -1,
      -1,    21,    22,    23,    24,    25,   299,    27,    28,    29,
      30,    -1,    32,    33,    -1,   308,    36,    37,    38,    39,
      -1,    -1,   290,    43,    44,    -1,    -1,    -1,    -1,    19,
      -1,    -1,    52,    -1,    54,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    17,    69,
      70,    71,    21,    22,    23,    24,    25,    -1,    27,    28,
      29,    30,    -1,    32,    33,    -1,    -1,    36,    37,    38,
      39,    -1,    -1,    -1,    43,    44,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    52,   104,    54,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,     3,   226,    -1,    -1,    -1,
      69,    70,    71,    -1,    -1,    -1,    -1,    -1,    -1,    17,
      -1,    -1,    -1,    -1,    22,    23,    -1,    25,    26,    27,
      28,    29,    30,    -1,    32,    33,    -1,    35,    36,    37,
      38,    39,    -1,    -1,    -1,   104,   179,   180,   181,   182,
     183,   184,   185,   186,   187,   188,   189,   190,   191,   192,
     193,    -1,    -1,    -1,    -1,     6,     7,    -1,     9,   289,
      -1,    -1,    -1,    -1,    -1,   155,   156,   157,   158,   159,
      -1,    -1,    -1,    81,    -1,    83,    -1,    -1,   308,    -1,
      88,    -1,    -1,    -1,    -1,    -1,    -1,   177,   178,    40,
      -1,    -1,    -1,    -1,    -1,    -1,   104,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   194,   195,   196,   197,    19,    -1,
      -1,   201,   202,    -1,    -1,    -1,    -1,   207,    69,    -1,
     210,    -1,    73,    -1,    -1,    76,    77,   217,   218,   137,
      81,    82,    83,    84,    85,    86,    87,    88,    89,    90,
      91,    92,    -1,    -1,    -1,    -1,    -1,    -1,    99,    -1,
      -1,   102,    -1,    -1,    19,    -1,    -1,    68,   109,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   118,    -1,   289,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   299,
      -1,    -1,   272,   273,    -1,   275,   276,   277,   308,   279,
     280,   281,   282,   283,   284,   285,   286,   287,    -1,    -1,
      -1,   291,    -1,    68,    -1,    -1,    -1,   297,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   227,
     289,   229,   230,   231,   232,   233,   234,   235,   236,   237,
     238,   239,   240,   241,   242,   243,   244,   245,    -1,   308,
      -1,   249,    -1,    -1,   155,    -1,   157,   158,   159,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   169,    -1,
      -1,    -1,    -1,   174,    -1,    -1,   177,   178,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   290,   194,   195,   196,   197,    -1,   199,   200,
     155,    -1,   157,   158,   159,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   169,    -1,    -1,    -1,   219,   174,
      -1,    -1,   177,   178,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,     3,    -1,    -1,   194,
     195,   196,   197,    -1,   199,   200,    -1,    -1,    -1,    -1,
      17,    -1,    -1,   254,   255,    22,    23,    -1,    25,    26,
      27,    28,    29,    30,   219,    32,    33,    -1,    35,    36,
      37,    38,    39,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     281,   282,   283,   284,   285,   286,   287,    -1,    -1,    -1,
     291,    -1,    -1,    -1,    -1,    -1,   297,    -1,    -1,   254,
     255,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    81,    -1,    83,    -1,    -1,    -1,
      -1,    88,    -1,    -1,    -1,    -1,   281,   282,   283,   284,
     285,   286,   287,    -1,    -1,    -1,   291,   104,    -1,    23,
      24,    25,   297,    27,    28,    29,    30,    -1,    32,    33,
      34,    -1,    36,    37,    38,    39,    -1,    40,    41,    43,
      44,    -1,    -1,    46,    -1,    48,    -1,    -1,    -1,    -1,
     137,    -1,    55,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    72,
      73,    -1,    75,    76,    77,    78,    79,    -1,    -1,    -1,
      -1,    84,    -1,    -1,    87,    -1,    -1,    -1,    -1,    -1,
      93,    -1,    -1,    -1,    -1,    98,    -1,   100,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   111,    -1,   112,
     113,   114,   115,   116,   117,   118,   119,    -1,    -1,    -1,
      -1,    -1,   125,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     227,    -1,   229,   230,   231,   232,   233,   234,   235,   236,
     237,   238,   239,   240,   241,   242,   243,   244,   245,    -1,
      -1,    -1,   249
};

/* YYSTOS[STATE-NUM] -- The symbol kind of the accessing symbol of
   state STATE-NUM.  */
static const yytype_int16 yystos[] =
{
       0,   310,     0,    19,   155,   156,   157,   158,   159,   177,
     178,   194,   195,   196,   197,   201,   202,   207,   210,   217,
     218,   272,   273,   275,   276,   277,   279,   280,   281,   282,
     283,   284,   285,   286,   287,   297,   311,   314,   320,   321,
     322,   323,   324,   325,   332,   334,   335,   337,   338,   339,
     340,   341,   342,   361,   379,   383,   405,   406,   461,   464,
     470,   471,   472,   476,   485,   488,   493,   216,     6,     7,
       9,   315,   316,   299,   365,    65,   126,   407,   179,   180,
     181,   182,   183,   184,   185,   186,   187,   188,   189,   190,
     191,   192,   193,   469,   469,     9,    15,    18,    41,    42,
      46,    48,    55,    72,    87,   295,   326,   366,   367,   368,
     369,   298,   299,   274,   473,   216,   477,   494,   216,   316,
      10,   317,   317,    10,    11,   318,   318,    14,    17,    21,
      22,    23,    24,    25,    27,    28,    29,    30,    32,    33,
      36,    37,    38,    39,    43,    44,    52,    54,    69,    70,
      71,   104,   105,   160,   161,   162,   289,   299,   308,   316,
     322,   323,   369,   370,   428,   451,   452,   457,   458,   290,
     316,   316,   316,   316,     8,    13,   414,   415,   414,   414,
     290,   343,    61,   344,   290,   384,   390,    24,    27,    28,
      29,    30,    32,    33,    34,   290,   306,   408,   411,   413,
     414,   317,   290,   290,   290,   290,   490,   294,   317,   362,
     315,   299,   369,   428,   451,   453,   457,     8,    34,   298,
     313,   293,   295,   295,    47,    49,    50,    51,   367,   367,
     327,   370,   453,   298,   457,   295,   317,   317,   208,   316,
     477,   101,   102,   103,   126,   220,   222,   223,   224,   225,
     316,    76,    77,   316,   316,   457,    27,    28,    29,    30,
     451,    52,   451,    25,    26,    35,    16,    18,   457,    23,
      24,    25,    27,    28,    29,    30,    32,    33,    36,    37,
      38,    39,    45,   313,   412,   413,   416,   218,   305,   316,
     369,   308,   316,   317,   137,   137,   137,   366,   367,   137,
     307,   106,   107,   108,   137,   299,   301,   305,   306,   312,
     451,   313,   296,    13,   296,   296,   310,    40,    69,    73,
      76,    77,    81,    82,    83,    84,    85,    86,    87,    88,
      89,    90,    91,    92,    99,   102,   109,   118,   316,   453,
      62,   345,   346,    40,    73,    75,    76,    77,    78,    79,
      84,    93,    98,   100,   112,   113,   114,   115,   116,   117,
     118,   119,   125,   367,   142,   143,   144,   145,   146,   147,
     148,   149,   150,   151,   152,   153,   154,   163,   164,   165,
     166,   167,   168,   205,   219,   254,   255,   290,   291,   314,
     315,   321,   332,   389,   391,   392,   393,   394,   396,   397,
     405,   429,   430,   431,   432,   433,   434,   435,   436,   437,
     438,   439,   440,   441,   442,   443,   461,   471,   305,   295,
     299,   410,   295,   410,   295,   410,   295,   410,   295,   410,
     295,   410,   295,   409,   411,   295,   414,   296,     8,     9,
     293,   304,   478,   486,   491,   495,    74,    76,    77,    83,
     316,   316,   300,    40,    73,    75,    76,    77,    78,   112,
     113,   114,   115,   118,   122,   123,   124,   226,   457,   298,
     218,   316,   367,   295,   298,   295,   290,   295,   292,     9,
     317,   317,   296,   290,   295,   313,   120,   121,   299,   316,
     386,   453,   300,   167,   474,   316,   221,   137,   451,    26,
     316,   453,   295,   295,    27,    28,    29,    30,   295,   295,
     295,   295,   295,   295,   295,   295,   295,   295,   414,   316,
     300,   300,   300,   316,   317,   316,   316,   316,   457,   316,
     316,   295,   295,   316,    21,   300,   317,   459,   460,   446,
     447,   457,   291,   312,   291,   295,    76,    77,    78,   112,
     113,   114,   301,   350,   347,   453,    68,   155,   169,   174,
     199,   200,   219,   254,   255,   291,   314,   321,   332,   342,
     360,   361,   371,   375,   383,   405,   461,   471,   489,   295,
     295,   387,   317,   317,   317,   299,   111,   288,   299,   104,
     453,   304,   198,   295,   390,    56,    58,    59,    60,   395,
     398,   399,   400,   401,   402,   403,   315,   317,   392,   315,
     317,   317,   318,    12,    32,    33,   295,   318,   319,   315,
     317,   366,    16,    18,   369,   457,   453,    88,   313,   413,
     367,   327,   295,   414,   295,   317,   317,   317,   317,   318,
     319,   319,   291,   293,   315,   296,   317,   317,   209,   211,
     214,   215,   291,   321,   332,   461,   479,   481,   482,   484,
      85,   209,   212,   291,   475,   481,   483,   487,    42,   155,
     207,   210,   291,   321,   332,   492,   207,   210,   291,   321,
     332,   496,    76,    77,    78,   112,   113,   114,   295,   295,
     316,   316,   300,   457,   313,   465,   466,   290,    52,   453,
     462,   463,     8,   293,   296,   296,   326,   328,   329,   301,
     359,   445,    20,   336,   475,   137,   316,    20,    66,    67,
     467,   317,   295,   295,   295,   295,   317,   317,   317,   318,
     317,   319,   318,   319,   317,   317,   317,   318,   296,   300,
     452,   452,   452,   305,   453,   453,    21,   293,   300,   302,
     293,   317,    40,    52,    53,    70,   120,   289,   292,   303,
     351,   352,   355,   293,   111,   372,   376,   317,   317,   490,
     111,   288,   104,   453,   290,   290,   290,   390,   290,   317,
     313,   385,   299,   457,   304,   317,   299,   316,   299,   316,
     317,   367,    20,   295,    21,   387,   448,   449,   450,   291,
     453,   395,    57,   392,   404,   315,   317,   392,   404,   404,
     404,    63,    63,   295,   295,   316,   453,   295,   414,   457,
     315,   317,   444,   296,   313,   296,   300,   296,   296,   296,
     296,   296,   409,   296,   304,     9,   293,   213,   298,   305,
     317,   480,   298,   313,   414,   414,   298,   298,   414,   414,
     295,   216,   317,   316,   216,   316,   216,   317,     3,    17,
      22,    23,    25,    26,    27,    28,    29,    30,    32,    33,
      35,    36,    37,    38,    39,    81,    83,    88,   104,   137,
     227,   229,   230,   231,   232,   233,   234,   235,   236,   237,
     238,   239,   240,   241,   242,   243,   244,   245,   249,   290,
     381,   382,   454,    64,   363,   300,   298,   296,   293,   328,
       9,   298,   291,   293,     9,   298,   291,    23,    24,    25,
      27,    28,    29,    30,    32,    33,    36,    37,    38,    39,
      43,    44,   111,   321,   330,   412,   417,   299,   446,   295,
     295,   316,   386,    29,    30,    64,   203,   204,   206,   414,
     316,   316,   296,   296,   317,   317,   317,   318,   296,   296,
     296,   296,   296,   296,   296,   296,   296,   296,   296,   296,
     452,   295,   296,   296,   317,   460,   457,   296,   295,    40,
     353,   354,   352,   295,   316,   357,   302,   453,   453,    73,
     118,   316,   453,    73,   118,   367,   316,   299,   316,   299,
     316,   367,    20,   346,   373,   377,   291,   491,   296,   137,
     385,    40,    47,    49,    50,    51,    88,    89,    90,   134,
     135,   136,   138,   141,   296,   251,   252,   253,   317,   226,
     380,   317,   300,   317,   317,   293,   300,   457,   386,   448,
     457,   296,   293,   315,   317,   315,   317,   317,   318,    20,
     313,   296,   295,   293,   293,   296,   296,   410,   410,   410,
     410,   410,   410,   317,   317,   317,   295,   304,   295,   296,
     296,   295,   295,   296,   296,   317,   452,   316,    64,   316,
     296,    26,    27,    28,    29,    30,   295,   455,   243,   235,
     247,   295,   228,   248,    23,   455,   455,     3,    22,    23,
      25,    26,    27,    28,    29,    30,    32,    33,    35,    36,
      37,    38,    39,   230,   231,   233,   234,   235,   237,   238,
     241,   242,   244,   257,   258,   259,   260,   261,   262,   263,
     264,   265,   266,   267,   268,   269,   270,   305,   456,   296,
     415,   299,   305,   315,   298,   364,    29,   313,   317,   451,
     467,   468,   465,   291,   298,   290,   462,   290,   295,   313,
     299,   299,    27,    28,    29,    30,   299,   299,   299,   299,
     299,   299,   299,   299,   299,   299,   295,   299,   295,   299,
     295,   299,   105,   110,   321,   331,   317,   302,   448,   448,
     359,   445,   315,   296,   296,   296,   296,   296,   448,   317,
     295,   354,   453,   348,   349,   453,   293,   356,   316,   197,
     322,   316,   457,   317,   317,   293,   457,   386,   291,   170,
     171,   172,   173,   291,   314,   321,   332,   374,   471,   173,
     175,   176,   291,   314,   321,   332,   378,   471,   291,   313,
     296,   295,   304,   304,   300,   300,   300,   300,   295,   386,
     137,   300,   300,   453,   364,   453,   296,   380,   450,    63,
      63,   296,   296,   316,   296,   448,   444,   444,     9,   293,
       9,   480,   296,   317,   250,   313,   299,   299,    26,    27,
      28,    29,    30,   271,   293,   299,   306,   291,   292,   300,
     317,   416,   295,   295,   290,   330,   328,   317,   317,   299,
     299,   299,   299,   317,   317,   317,   317,   317,   317,   317,
     317,   317,   317,   417,   317,     9,    45,   317,    45,    52,
     451,   317,    43,    92,   111,   333,   458,   300,   296,   296,
     295,   295,   474,   296,   296,   317,   316,   296,   293,   355,
     356,   316,   300,   300,   453,   453,   256,   366,   366,   366,
     366,   366,   366,   366,   385,   317,   139,   140,   139,   140,
     381,   350,   315,   293,    20,   315,   315,   317,   296,   317,
     304,   298,   293,   317,   317,   313,   300,   317,   292,   300,
     317,    27,    28,    29,   317,   330,   291,   291,   300,   300,
     317,   317,   317,   317,   300,   300,   300,   300,   300,   300,
     300,   300,   300,   300,   296,   300,   296,   296,   300,   296,
       9,   296,   300,    52,   451,   299,   316,   302,   448,   448,
     296,   356,   453,   295,   293,    20,   367,   296,   296,   296,
     295,   453,   386,     9,   480,   317,   313,   300,   300,   300,
     317,   296,   304,   304,   304,   296,   291,   295,   295,   300,
     300,   300,   300,   295,   295,   295,   295,   295,   295,   295,
     295,   295,   295,   295,   295,   296,   295,     9,   300,   298,
     296,   296,   448,   453,   386,   457,   448,   301,   358,   359,
     304,   296,   293,   296,   454,   300,   317,   317,   317,   424,
     422,   295,   295,   295,   295,   423,   422,   421,   420,   418,
     419,   423,   422,   421,   420,   427,   425,   426,   417,   296,
     358,   453,   296,   295,   480,   313,   296,   296,   296,   296,
     467,   296,   317,   423,   422,   421,   420,   296,   317,   296,
     296,   317,   296,   318,   296,   317,   319,   296,   318,   319,
     296,   296,   296,   296,   296,   417,     9,    45,   296,    45,
      52,   296,   451,   364,   295,    20,   388,   448,   293,   296,
     296,   296,   296,     9,   448,   386,    40,    55,    71,    80,
      94,    95,    96,    97,   126,   127,   128,   129,   130,   131,
     132,   290,   296,   313,   296,   295,   295,   296,   256,   448,
     317,   104,   296,   296,   367,   457,   453,    20,   386,   358,
     295,   448,   296
};

/* YYR1[RULE-NUM] -- Symbol kind of the left-hand side of rule RULE-NUM.  */
static const yytype_int16 yyr1[] =
{
       0,   309,   310,   310,   311,   311,   311,   311,   311,   311,
     311,   311,   311,   311,   311,   311,   311,   311,   311,   311,
     311,   311,   311,   311,   311,   311,   311,   311,   311,   311,
     312,   312,   313,   313,   314,   314,   314,   315,   315,   316,
     316,   316,   317,   318,   318,   319,   319,   319,   320,   320,
     320,   320,   320,   321,   321,   321,   321,   321,   321,   321,
     321,   321,   322,   322,   322,   322,   323,   323,   323,   323,
     324,   325,   326,   327,   327,   328,   329,   329,   329,   330,
     330,   330,   331,   331,   332,   332,   332,   333,   333,   333,
     333,   333,   333,   334,   334,   334,   335,   336,   336,   336,
     336,   336,   336,   337,   338,   339,   340,   341,   342,   343,
     343,   343,   343,   343,   343,   343,   343,   343,   343,   343,
     343,   343,   343,   343,   343,   343,   343,   343,   343,   343,
     343,   343,   343,   343,   343,   343,   344,   344,   345,   345,
     346,   346,   347,   347,   348,   348,   349,   349,   350,   350,
     351,   351,   351,   351,   351,   351,   351,   352,   352,   353,
     354,   354,   355,   355,   355,   356,   356,   357,   358,   358,
     359,   360,   360,   360,   360,   360,   360,   360,   360,   360,
     360,   360,   360,   360,   360,   360,   360,   360,   360,   360,
     360,   360,   361,   362,   362,   362,   362,   362,   362,   362,
     362,   362,   362,   362,   362,   362,   362,   362,   362,   363,
     363,   364,   364,   365,   365,   366,   366,   366,   366,   366,
     366,   366,   367,   367,   367,   367,   368,   368,   368,   368,
     368,   368,   368,   368,   369,   370,   370,   370,   370,   370,
     370,   371,   371,   372,   372,   372,   373,   373,   374,   374,
     374,   374,   374,   374,   374,   374,   375,   376,   376,   376,
     377,   377,   378,   378,   378,   378,   378,   378,   378,   379,
     380,   380,   381,   381,   382,   383,   384,   384,   384,   384,
     384,   384,   384,   384,   384,   384,   384,   384,   384,   384,
     384,   384,   384,   384,   384,   384,   384,   384,   384,   385,
     385,   385,   385,   385,   385,   385,   385,   385,   385,   385,
     385,   385,   385,   385,   385,   386,   386,   386,   387,   387,
     387,   387,   387,   388,   388,   388,   388,   388,   388,   388,
     388,   388,   388,   388,   388,   388,   388,   388,   388,   389,
     390,   390,   391,   391,   391,   391,   391,   391,   391,   391,
     391,   391,   391,   391,   391,   391,   391,   391,   391,   391,
     391,   391,   391,   391,   391,   391,   391,   391,   392,   393,
     394,   395,   395,   396,   396,   396,   397,   398,   398,   398,
     398,   399,   399,   399,   400,   401,   402,   403,   404,   404,
     404,   405,   406,   406,   407,   407,   407,   408,   408,   409,
     409,   410,   410,   411,   411,   411,   411,   411,   411,   411,
     411,   411,   411,   411,   411,   411,   411,   411,   412,   412,
     412,   412,   412,   412,   412,   412,   412,   412,   412,   412,
     412,   412,   412,   412,   412,   412,   412,   413,   414,   414,
     415,   415,   416,   416,   416,   417,   417,   417,   417,   417,
     417,   417,   417,   417,   417,   417,   417,   417,   417,   417,
     417,   417,   417,   417,   417,   417,   417,   417,   417,   417,
     417,   418,   418,   418,   419,   419,   419,   420,   420,   421,
     421,   422,   422,   423,   423,   424,   424,   425,   425,   425,
     426,   426,   426,   426,   427,   427,   428,   429,   430,   431,
     432,   433,   434,   435,   436,   437,   438,   439,   440,   441,
     442,   443,   443,   443,   443,   443,   443,   443,   443,   443,
     443,   443,   443,   443,   443,   443,   443,   443,   443,   443,
     443,   443,   443,   443,   444,   444,   444,   444,   444,   445,
     445,   446,   446,   447,   447,   448,   448,   449,   449,   450,
     450,   450,   451,   451,   451,   451,   451,   451,   451,   451,
     451,   451,   452,   452,   453,   453,   453,   453,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   455,   455,   456,   456,   456,   456,   456,
     456,   456,   456,   456,   456,   456,   456,   456,   456,   456,
     456,   456,   456,   456,   456,   456,   456,   456,   456,   456,
     456,   456,   456,   456,   456,   456,   456,   456,   456,   456,
     456,   456,   456,   456,   456,   456,   456,   456,   456,   456,
     456,   456,   456,   456,   457,   457,   457,   457,   457,   457,
     457,   457,   457,   457,   457,   457,   457,   457,   457,   457,
     457,   457,   457,   457,   457,   457,   457,   457,   457,   458,
     458,   458,   458,   458,   458,   458,   458,   458,   458,   458,
     458,   458,   458,   458,   458,   458,   458,   459,   459,   460,
     460,   460,   460,   460,   461,   461,   461,   461,   461,   461,
     462,   462,   462,   463,   463,   464,   464,   465,   465,   466,
     467,   467,   468,   468,   468,   468,   468,   468,   468,   468,
     469,   469,   469,   469,   469,   469,   469,   469,   469,   469,
     469,   469,   469,   469,   469,   470,   470,   471,   471,   471,
     471,   471,   471,   471,   471,   471,   471,   471,   472,   472,
     473,   473,   474,   474,   475,   476,   477,   477,   477,   477,
     477,   477,   477,   477,   477,   477,   478,   478,   479,   479,
     479,   480,   480,   481,   481,   481,   481,   481,   481,   482,
     483,   484,   485,   485,   486,   486,   487,   487,   487,   487,
     488,   489,   490,   490,   490,   490,   490,   490,   490,   490,
     490,   490,   491,   491,   492,   492,   492,   492,   492,   492,
     492,   493,   493,   494,   494,   494,   495,   495,   496,   496,
     496,   496
};

/* YYR2[RULE-NUM] -- Number of symbols on the right-hand side of rule RULE-NUM.  */
static const yytype_int8 yyr2[] =
{
       0,     2,     0,     2,     4,     4,     3,     1,     1,     1,
       1,     1,     1,     4,     4,     4,     4,     1,     1,     1,
       2,     2,     3,     2,     2,     1,     1,     1,     4,     1,
       0,     2,     1,     3,     2,     4,     6,     1,     1,     1,
       1,     3,     1,     1,     1,     1,     4,     4,     4,     4,
       4,     4,     4,     2,     3,     2,     2,     2,     1,     1,
       2,     1,     2,     4,     6,     3,     5,     7,     9,     3,
       4,     7,     1,     1,     1,     2,     0,     2,     2,     0,
       6,     2,     1,     1,     1,     1,     1,     1,     1,     1,
       3,     2,     3,     1,     2,     3,     7,     0,     2,     2,
       2,     2,     2,     3,     3,     2,     1,     4,     3,     0,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     3,     3,     3,     3,
       3,     3,     2,     2,     2,     5,     0,     2,     0,     2,
       0,     2,     3,     1,     0,     1,     1,     3,     0,     3,
       1,     1,     1,     1,     1,     1,     4,     0,     2,     4,
       0,     2,     5,     4,     3,     0,     2,     3,     0,     1,
       5,     3,     4,     4,     4,     1,     1,     1,     1,     1,
       2,     2,     4,    13,    22,     1,     1,     5,     3,     7,
       5,     4,     7,     0,     2,     2,     2,     2,     2,     2,
       2,     5,     2,     2,     2,     2,     2,     2,     5,     0,
       2,     0,     2,     0,     3,     9,     9,     7,     7,     1,
       1,     1,     2,     2,     1,     4,     0,     1,     1,     2,
       2,     2,     2,     1,     4,     2,     5,     3,     2,     2,
       1,     4,     3,     0,     2,     2,     0,     2,     2,     2,
       2,     2,     1,     1,     1,     1,     9,     0,     2,     2,
       0,     2,     2,     2,     2,     1,     1,     1,     1,     1,
       0,     4,     1,     3,     1,    13,     0,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     5,     8,     6,     5,     0,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       4,     4,     4,     4,     5,     1,     1,     1,     0,     4,
       4,     4,     4,     0,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     5,     1,
       0,     2,     2,     1,     2,     4,     5,     1,     1,     1,
       1,     2,     1,     1,     1,     1,     1,     4,     6,     4,
       4,    11,     1,     5,     3,     7,     5,     5,     3,     1,
       2,     2,     1,     2,     4,     4,     1,     2,     2,     2,
       2,     2,     2,     2,     1,     2,     1,     1,     1,     4,
       4,     2,     4,     2,     0,     1,     1,     3,     1,     3,
       1,     0,     3,     5,     4,     3,     5,     5,     5,     5,
       5,     5,     2,     2,     2,     2,     2,     2,     4,     4,
       4,     4,     4,     4,     4,     4,     5,     5,     5,     5,
       4,     4,     4,     4,     4,     4,     3,     2,     0,     1,
       1,     2,     1,     1,     1,     1,     4,     4,     5,     4,
       4,     4,     7,     7,     7,     7,     7,     7,     7,     7,
       7,     7,     8,     8,     8,     8,     7,     7,     7,     7,
       7,     0,     2,     2,     0,     2,     2,     0,     2,     0,
       2,     0,     2,     0,     2,     0,     2,     0,     2,     2,
       0,     2,     3,     2,     0,     2,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       2,     1,     2,     2,     2,     2,     2,     2,     3,     2,
       2,     2,     5,     3,     2,     2,     2,     2,     2,     5,
       4,     6,     2,     4,     0,     3,     3,     1,     1,     0,
       3,     0,     1,     1,     3,     0,     1,     1,     3,     1,
       3,     4,     4,     4,     4,     5,     1,     1,     1,     1,
       1,     1,     1,     3,     1,     3,     4,     1,     0,    10,
       6,     5,     6,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     2,     2,     2,     2,     1,
       1,     1,     1,     2,     3,     4,     6,     5,     1,     1,
       1,     1,     1,     1,     1,     2,     2,     1,     2,     2,
       4,     1,     2,     1,     2,     1,     2,     1,     2,     1,
       2,     1,     1,     0,     5,     0,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     2,     2,     2,
       2,     1,     1,     1,     1,     1,     3,     2,     2,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     2,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     2,     1,     3,     2,     2,     3,
       4,     2,     2,     2,     5,     5,     7,     4,     3,     2,
       3,     2,     1,     1,     2,     3,     2,     1,     2,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     2,     2,
       2,     2,     1,     1,     1,     1,     1,     1,     3,     0,
       1,     1,     3,     2,     6,     7,     3,     3,     3,     6,
       0,     1,     3,     5,     6,     4,     4,     1,     3,     3,
       1,     1,     1,     1,     4,     1,     6,     6,     6,     4,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     3,     2,     5,
       4,     7,     6,     7,     6,     9,     8,     3,     8,     4,
       0,     2,     0,     1,     3,     3,     0,     2,     2,     2,
       3,     2,     2,     2,     2,     2,     0,     2,     3,     1,
       1,     1,     1,     3,     8,     2,     3,     1,     1,     3,
       3,     3,     4,     6,     0,     2,     3,     1,     3,     1,
       4,     3,     0,     2,     2,     2,     3,     3,     3,     3,
       3,     3,     0,     2,     2,     3,     3,     4,     2,     1,
       1,     3,     5,     0,     2,     2,     0,     2,     4,     3,
       1,     1
};


enum { YYENOMEM = -2 };

#define yyerrok         (yyerrstatus = 0)
#define yyclearin       (yychar = YYEMPTY)

#define YYACCEPT        goto yyacceptlab
#define YYABORT         goto yyabortlab
#define YYERROR         goto yyerrorlab
#define YYNOMEM         goto yyexhaustedlab


#define YYRECOVERING()  (!!yyerrstatus)

#define YYBACKUP(Token, Value)                                    \
  do                                                              \
    if (yychar == YYEMPTY)                                        \
      {                                                           \
        yychar = (Token);                                         \
        yylval = (Value);                                         \
        YYPOPSTACK (yylen);                                       \
        yystate = *yyssp;                                         \
        goto yybackup;                                            \
      }                                                           \
    else                                                          \
      {                                                           \
        yyerror (YY_("syntax error: cannot back up")); \
        YYERROR;                                                  \
      }                                                           \
  while (0)

/* Backward compatibility with an undocumented macro.
   Use YYerror or YYUNDEF. */
#define YYERRCODE YYUNDEF


/* Enable debugging if requested.  */
#if YYDEBUG

# ifndef YYFPRINTF
#  include <stdio.h> /* INFRINGES ON USER NAME SPACE */
#  define YYFPRINTF fprintf
# endif

# define YYDPRINTF(Args)                        \
do {                                            \
  if (yydebug)                                  \
    YYFPRINTF Args;                             \
} while (0)




# define YY_SYMBOL_PRINT(Title, Kind, Value, Location)                    \
do {                                                                      \
  if (yydebug)                                                            \
    {                                                                     \
      YYFPRINTF (stderr, "%s ", Title);                                   \
      yy_symbol_print (stderr,                                            \
                  Kind, Value); \
      YYFPRINTF (stderr, "\n");                                           \
    }                                                                     \
} while (0)


/*-----------------------------------.
| Print this symbol's value on YYO.  |
`-----------------------------------*/

static void
yy_symbol_value_print (FILE *yyo,
                       yysymbol_kind_t yykind, YYSTYPE const * const yyvaluep)
{
  FILE *yyoutput = yyo;
  YY_USE (yyoutput);
  if (!yyvaluep)
    return;
  YY_IGNORE_MAYBE_UNINITIALIZED_BEGIN
  YY_USE (yykind);
  YY_IGNORE_MAYBE_UNINITIALIZED_END
}


/*---------------------------.
| Print this symbol on YYO.  |
`---------------------------*/

static void
yy_symbol_print (FILE *yyo,
                 yysymbol_kind_t yykind, YYSTYPE const * const yyvaluep)
{
  YYFPRINTF (yyo, "%s %s (",
             yykind < YYNTOKENS ? "token" : "nterm", yysymbol_name (yykind));

  yy_symbol_value_print (yyo, yykind, yyvaluep);
  YYFPRINTF (yyo, ")");
}

/*------------------------------------------------------------------.
| yy_stack_print -- Print the state stack from its BOTTOM up to its |
| TOP (included).                                                   |
`------------------------------------------------------------------*/

static void
yy_stack_print (yy_state_t *yybottom, yy_state_t *yytop)
{
  YYFPRINTF (stderr, "Stack now");
  for (; yybottom <= yytop; yybottom++)
    {
      int yybot = *yybottom;
      YYFPRINTF (stderr, " %d", yybot);
    }
  YYFPRINTF (stderr, "\n");
}

# define YY_STACK_PRINT(Bottom, Top)                            \
do {                                                            \
  if (yydebug)                                                  \
    yy_stack_print ((Bottom), (Top));                           \
} while (0)


/*------------------------------------------------.
| Report that the YYRULE is going to be reduced.  |
`------------------------------------------------*/

static void
yy_reduce_print (yy_state_t *yyssp, YYSTYPE *yyvsp,
                 int yyrule)
{
  int yylno = yyrline[yyrule];
  int yynrhs = yyr2[yyrule];
  int yyi;
  YYFPRINTF (stderr, "Reducing stack by rule %d (line %d):\n",
             yyrule - 1, yylno);
  /* The symbols being reduced.  */
  for (yyi = 0; yyi < yynrhs; yyi++)
    {
      YYFPRINTF (stderr, "   $%d = ", yyi + 1);
      yy_symbol_print (stderr,
                       YY_ACCESSING_SYMBOL (+yyssp[yyi + 1 - yynrhs]),
                       &yyvsp[(yyi + 1) - (yynrhs)]);
      YYFPRINTF (stderr, "\n");
    }
}

# define YY_REDUCE_PRINT(Rule)          \
do {                                    \
  if (yydebug)                          \
    yy_reduce_print (yyssp, yyvsp, Rule); \
} while (0)

/* Nonzero means print parse trace.  It is left uninitialized so that
   multiple parsers can coexist.  */
int yydebug;
#else /* !YYDEBUG */
# define YYDPRINTF(Args) ((void) 0)
# define YY_SYMBOL_PRINT(Title, Kind, Value, Location)
# define YY_STACK_PRINT(Bottom, Top)
# define YY_REDUCE_PRINT(Rule)
#endif /* !YYDEBUG */


/* YYINITDEPTH -- initial size of the parser's stacks.  */
#ifndef YYINITDEPTH
# define YYINITDEPTH 200
#endif

/* YYMAXDEPTH -- maximum size the stacks can grow to (effective only
   if the built-in stack extension method is used).

   Do not make this value too large; the results are undefined if
   YYSTACK_ALLOC_MAXIMUM < YYSTACK_BYTES (YYMAXDEPTH)
   evaluated with infinite-precision integer arithmetic.  */

#ifndef YYMAXDEPTH
# define YYMAXDEPTH 10000
#endif






/*-----------------------------------------------.
| Release the memory associated to this symbol.  |
`-----------------------------------------------*/

static void
yydestruct (const char *yymsg,
            yysymbol_kind_t yykind, YYSTYPE *yyvaluep)
{
  YY_USE (yyvaluep);
  if (!yymsg)
    yymsg = "Deleting";
  YY_SYMBOL_PRINT (yymsg, yykind, yyvaluep, yylocationp);

  YY_IGNORE_MAYBE_UNINITIALIZED_BEGIN
  YY_USE (yykind);
  YY_IGNORE_MAYBE_UNINITIALIZED_END
}


/* Lookahead token kind.  */
int yychar;

/* The semantic value of the lookahead symbol.  */
YYSTYPE yylval;
/* Number of syntax errors so far.  */
int yynerrs;




/*----------.
| yyparse.  |
`----------*/

int
yyparse (void)
{
    yy_state_fast_t yystate = 0;
    /* Number of tokens to shift before error messages enabled.  */
    int yyerrstatus = 0;

    /* Refer to the stacks through separate pointers, to allow yyoverflow
       to reallocate them elsewhere.  */

    /* Their size.  */
    YYPTRDIFF_T yystacksize = YYINITDEPTH;

    /* The state stack: array, bottom, top.  */
    yy_state_t yyssa[YYINITDEPTH];
    yy_state_t *yyss = yyssa;
    yy_state_t *yyssp = yyss;

    /* The semantic value stack: array, bottom, top.  */
    YYSTYPE yyvsa[YYINITDEPTH];
    YYSTYPE *yyvs = yyvsa;
    YYSTYPE *yyvsp = yyvs;

  int yyn;
  /* The return value of yyparse.  */
  int yyresult;
  /* Lookahead symbol kind.  */
  yysymbol_kind_t yytoken = YYSYMBOL_YYEMPTY;
  /* The variables used to return semantic value and location from the
     action routines.  */
  YYSTYPE yyval;



#define YYPOPSTACK(N)   (yyvsp -= (N), yyssp -= (N))

  /* The number of symbols on the RHS of the reduced rule.
     Keep to zero when no symbol should be popped.  */
  int yylen = 0;

  YYDPRINTF ((stderr, "Starting parse\n"));

  yychar = YYEMPTY; /* Cause a token to be read.  */

  goto yysetstate;


/*------------------------------------------------------------.
| yynewstate -- push a new state, which is found in yystate.  |
`------------------------------------------------------------*/
yynewstate:
  /* In all cases, when you get here, the value and location stacks
     have just been pushed.  So pushing a state here evens the stacks.  */
  yyssp++;


/*--------------------------------------------------------------------.
| yysetstate -- set current state (the top of the stack) to yystate.  |
`--------------------------------------------------------------------*/
yysetstate:
  YYDPRINTF ((stderr, "Entering state %d\n", yystate));
  YY_ASSERT (0 <= yystate && yystate < YYNSTATES);
  YY_IGNORE_USELESS_CAST_BEGIN
  *yyssp = YY_CAST (yy_state_t, yystate);
  YY_IGNORE_USELESS_CAST_END
  YY_STACK_PRINT (yyss, yyssp);

  if (yyss + yystacksize - 1 <= yyssp)
#if !defined yyoverflow && !defined YYSTACK_RELOCATE
    YYNOMEM;
#else
    {
      /* Get the current used size of the three stacks, in elements.  */
      YYPTRDIFF_T yysize = yyssp - yyss + 1;

# if defined yyoverflow
      {
        /* Give user a chance to reallocate the stack.  Use copies of
           these so that the &'s don't force the real ones into
           memory.  */
        yy_state_t *yyss1 = yyss;
        YYSTYPE *yyvs1 = yyvs;

        /* Each stack pointer address is followed by the size of the
           data in use in that stack, in bytes.  This used to be a
           conditional around just the two extra args, but that might
           be undefined if yyoverflow is a macro.  */
        yyoverflow (YY_("memory exhausted"),
                    &yyss1, yysize * YYSIZEOF (*yyssp),
                    &yyvs1, yysize * YYSIZEOF (*yyvsp),
                    &yystacksize);
        yyss = yyss1;
        yyvs = yyvs1;
      }
# else /* defined YYSTACK_RELOCATE */
      /* Extend the stack our own way.  */
      if (YYMAXDEPTH <= yystacksize)
        YYNOMEM;
      yystacksize *= 2;
      if (YYMAXDEPTH < yystacksize)
        yystacksize = YYMAXDEPTH;

      {
        yy_state_t *yyss1 = yyss;
        union yyalloc *yyptr =
          YY_CAST (union yyalloc *,
                   YYSTACK_ALLOC (YY_CAST (YYSIZE_T, YYSTACK_BYTES (yystacksize))));
        if (! yyptr)
          YYNOMEM;
        YYSTACK_RELOCATE (yyss_alloc, yyss);
        YYSTACK_RELOCATE (yyvs_alloc, yyvs);
#  undef YYSTACK_RELOCATE
        if (yyss1 != yyssa)
          YYSTACK_FREE (yyss1);
      }
# endif

      yyssp = yyss + yysize - 1;
      yyvsp = yyvs + yysize - 1;

      YY_IGNORE_USELESS_CAST_BEGIN
      YYDPRINTF ((stderr, "Stack size increased to %ld\n",
                  YY_CAST (long, yystacksize)));
      YY_IGNORE_USELESS_CAST_END

      if (yyss + yystacksize - 1 <= yyssp)
        YYABORT;
    }
#endif /* !defined yyoverflow && !defined YYSTACK_RELOCATE */


  if (yystate == YYFINAL)
    YYACCEPT;

  goto yybackup;


/*-----------.
| yybackup.  |
`-----------*/
yybackup:
  /* Do appropriate processing given the current state.  Read a
     lookahead token if we need one and don't already have one.  */

  /* First try to decide what to do without reference to lookahead token.  */
  yyn = yypact[yystate];
  if (yypact_value_is_default (yyn))
    goto yydefault;

  /* Not known => get a lookahead token if don't already have one.  */

  /* YYCHAR is either empty, or end-of-input, or a valid lookahead.  */
  if (yychar == YYEMPTY)
    {
      YYDPRINTF ((stderr, "Reading a token\n"));
      yychar = yylex ();
    }

  if (yychar <= YYEOF)
    {
      yychar = YYEOF;
      yytoken = YYSYMBOL_YYEOF;
      YYDPRINTF ((stderr, "Now at end of input.\n"));
    }
  else if (yychar == YYerror)
    {
      /* The scanner already issued an error message, process directly
         to error recovery.  But do not keep the error token as
         lookahead, it is too special and may lead us to an endless
         loop in error recovery. */
      yychar = YYUNDEF;
      yytoken = YYSYMBOL_YYerror;
      goto yyerrlab1;
    }
  else
    {
      yytoken = YYTRANSLATE (yychar);
      YY_SYMBOL_PRINT ("Next token is", yytoken, &yylval, &yylloc);
    }

  /* If the proper action on seeing token YYTOKEN is to reduce or to
     detect an error, take that action.  */
  yyn += yytoken;
  if (yyn < 0 || YYLAST < yyn || yycheck[yyn] != yytoken)
    goto yydefault;
  yyn = yytable[yyn];
  if (yyn <= 0)
    {
      if (yytable_value_is_error (yyn))
        goto yyerrlab;
      yyn = -yyn;
      goto yyreduce;
    }

  /* Count tokens shifted since error; after three, turn off error
     status.  */
  if (yyerrstatus)
    yyerrstatus--;

  /* Shift the lookahead token.  */
  YY_SYMBOL_PRINT ("Shifting", yytoken, &yylval, &yylloc);
  yystate = yyn;
  YY_IGNORE_MAYBE_UNINITIALIZED_BEGIN
  *++yyvsp = yylval;
  YY_IGNORE_MAYBE_UNINITIALIZED_END

  /* Discard the shifted token.  */
  yychar = YYEMPTY;
  goto yynewstate;


/*-----------------------------------------------------------.
| yydefault -- do the default action for the current state.  |
`-----------------------------------------------------------*/
yydefault:
  yyn = yydefact[yystate];
  if (yyn == 0)
    goto yyerrlab;
  goto yyreduce;


/*-----------------------------.
| yyreduce -- do a reduction.  |
`-----------------------------*/
yyreduce:
  /* yyn is the number of a rule to reduce with.  */
  yylen = yyr2[yyn];

  /* If YYLEN is nonzero, implement the default value of the action:
     '$$ = $1'.

     Otherwise, the following line sets YYVAL to garbage.
     This behavior is undocumented and Bison
     users should not rely upon it.  Assigning to YYVAL
     unconditionally makes the parser a bit smaller, and it avoids a
     GCC warning that YYVAL may be used uninitialized.  */
  yyval = yyvsp[1-yylen];


  YY_REDUCE_PRINT (yyn);
  switch (yyn)
    {
  case 4: /* decl: classHead '{' classDecls '}'  */
#line 194 "./asmparse.y"
                                                                                { PASM->EndClass(); }
#line 3569 "asmparse.cpp"
    break;

  case 5: /* decl: nameSpaceHead '{' decls '}'  */
#line 195 "./asmparse.y"
                                                                                { PASM->EndNameSpace(); }
#line 3575 "asmparse.cpp"
    break;

  case 6: /* decl: methodHead methodDecls '}'  */
#line 196 "./asmparse.y"
                                                                                { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                                                  {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                                     PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                                                  PASM->EndMethod(); }
#line 3584 "asmparse.cpp"
    break;

  case 13: /* decl: assemblyHead '{' assemblyDecls '}'  */
#line 206 "./asmparse.y"
                                                                                { PASMM->EndAssembly(); }
#line 3590 "asmparse.cpp"
    break;

  case 14: /* decl: assemblyRefHead '{' assemblyRefDecls '}'  */
#line 207 "./asmparse.y"
                                                                                { PASMM->EndAssembly(); }
#line 3596 "asmparse.cpp"
    break;

  case 15: /* decl: exptypeHead '{' exptypeDecls '}'  */
#line 208 "./asmparse.y"
                                                                                { PASMM->EndComType(); }
#line 3602 "asmparse.cpp"
    break;

  case 16: /* decl: manifestResHead '{' manifestResDecls '}'  */
#line 209 "./asmparse.y"
                                                                                { PASMM->EndManifestRes(); }
#line 3608 "asmparse.cpp"
    break;

  case 20: /* decl: _SUBSYSTEM int32  */
#line 213 "./asmparse.y"
                                                                                {
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:22011) // Suppress PREFast warning about integer overflow/underflow
#endif
                                                                                  PASM->m_dwSubsystem = (yyvsp[0].int32);
#ifdef _PREFAST_
#pragma warning(pop)
#endif
                                                                                }
#line 3623 "asmparse.cpp"
    break;

  case 21: /* decl: _CORFLAGS int32  */
#line 223 "./asmparse.y"
                                                                                { PASM->m_dwComImageFlags = (yyvsp[0].int32); }
#line 3629 "asmparse.cpp"
    break;

  case 22: /* decl: _FILE ALIGNMENT_ int32  */
#line 224 "./asmparse.y"
                                                                                { PASM->m_dwFileAlignment = (yyvsp[0].int32);
                                                                                  if(((yyvsp[0].int32) & ((yyvsp[0].int32) - 1))||((yyvsp[0].int32) < 0x200)||((yyvsp[0].int32) > 0x10000))
                                                                                    PASM->report->error("Invalid file alignment, must be power of 2 from 0x200 to 0x10000\n");}
#line 3637 "asmparse.cpp"
    break;

  case 23: /* decl: _IMAGEBASE int64  */
#line 227 "./asmparse.y"
                                                                                { PASM->m_stBaseAddress = (ULONGLONG)(*((yyvsp[0].int64))); delete (yyvsp[0].int64);
                                                                                  if(PASM->m_stBaseAddress & 0xFFFF)
                                                                                    PASM->report->error("Invalid image base, must be 0x10000-aligned\n");}
#line 3645 "asmparse.cpp"
    break;

  case 24: /* decl: _STACKRESERVE int64  */
#line 230 "./asmparse.y"
                                                                                { PASM->m_stSizeOfStackReserve = (size_t)(*((yyvsp[0].int64))); delete (yyvsp[0].int64); }
#line 3651 "asmparse.cpp"
    break;

  case 29: /* decl: _MSCORLIB  */
#line 235 "./asmparse.y"
                                                                                { PASM->m_fIsMscorlib = TRUE; }
#line 3657 "asmparse.cpp"
    break;

  case 32: /* compQstring: QSTRING  */
#line 242 "./asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[0].binstr); }
#line 3663 "asmparse.cpp"
    break;

  case 33: /* compQstring: compQstring '+' QSTRING  */
#line 243 "./asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 3669 "asmparse.cpp"
    break;

  case 34: /* languageDecl: _LANGUAGE SQSTRING  */
#line 246 "./asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[0].string),&(PASM->m_guidLang)); }
#line 3675 "asmparse.cpp"
    break;

  case 35: /* languageDecl: _LANGUAGE SQSTRING ',' SQSTRING  */
#line 247 "./asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid((yyvsp[0].string),&(PASM->m_guidLangVendor));}
#line 3682 "asmparse.cpp"
    break;

  case 36: /* languageDecl: _LANGUAGE SQSTRING ',' SQSTRING ',' SQSTRING  */
#line 249 "./asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[-4].string),&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidLangVendor));
                                                                                  LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidDoc));}
#line 3690 "asmparse.cpp"
    break;

  case 37: /* id: ID  */
#line 254 "./asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3696 "asmparse.cpp"
    break;

  case 38: /* id: SQSTRING  */
#line 255 "./asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3702 "asmparse.cpp"
    break;

  case 39: /* dottedName: id  */
#line 258 "./asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3708 "asmparse.cpp"
    break;

  case 40: /* dottedName: DOTTEDNAME  */
#line 259 "./asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3714 "asmparse.cpp"
    break;

  case 41: /* dottedName: dottedName '.' dottedName  */
#line 260 "./asmparse.y"
                                                              { (yyval.string) = newStringWDel((yyvsp[-2].string), '.', (yyvsp[0].string)); }
#line 3720 "asmparse.cpp"
    break;

  case 42: /* int32: INT32_T  */
#line 263 "./asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 3726 "asmparse.cpp"
    break;

  case 43: /* int64: INT64_T  */
#line 266 "./asmparse.y"
                                                              { (yyval.int64) = (yyvsp[0].int64); }
#line 3732 "asmparse.cpp"
    break;

  case 44: /* int64: INT32_T  */
#line 267 "./asmparse.y"
                                                              { (yyval.int64) = neg ? new __int64((yyvsp[0].int32)) : new __int64((unsigned)(yyvsp[0].int32)); }
#line 3738 "asmparse.cpp"
    break;

  case 45: /* float64: FLOAT64  */
#line 270 "./asmparse.y"
                                                              { (yyval.float64) = (yyvsp[0].float64); }
#line 3744 "asmparse.cpp"
    break;

  case 46: /* float64: FLOAT32_ '(' int32 ')'  */
#line 271 "./asmparse.y"
                                                              { float f; *((__int32*) (&f)) = (yyvsp[-1].int32); (yyval.float64) = new double(f); }
#line 3750 "asmparse.cpp"
    break;

  case 47: /* float64: FLOAT64_ '(' int64 ')'  */
#line 272 "./asmparse.y"
                                                              { (yyval.float64) = (double*) (yyvsp[-1].int64); }
#line 3756 "asmparse.cpp"
    break;

  case 48: /* typedefDecl: _TYPEDEF type AS_ dottedName  */
#line 276 "./asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].binstr),(yyvsp[0].string)); }
#line 3762 "asmparse.cpp"
    break;

  case 49: /* typedefDecl: _TYPEDEF className AS_ dottedName  */
#line 277 "./asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].token),(yyvsp[0].string)); }
#line 3768 "asmparse.cpp"
    break;

  case 50: /* typedefDecl: _TYPEDEF memberRef AS_ dottedName  */
#line 278 "./asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].token),(yyvsp[0].string)); }
#line 3774 "asmparse.cpp"
    break;

  case 51: /* typedefDecl: _TYPEDEF customDescr AS_ dottedName  */
#line 279 "./asmparse.y"
                                                                                { (yyvsp[-2].cad)->tkOwner = 0; PASM->AddTypeDef((yyvsp[-2].cad),(yyvsp[0].string)); }
#line 3780 "asmparse.cpp"
    break;

  case 52: /* typedefDecl: _TYPEDEF customDescrWithOwner AS_ dottedName  */
#line 280 "./asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].cad),(yyvsp[0].string)); }
#line 3786 "asmparse.cpp"
    break;

  case 53: /* compControl: P_DEFINE dottedName  */
#line 285 "./asmparse.y"
                                                                                { DefineVar((yyvsp[0].string), NULL); }
#line 3792 "asmparse.cpp"
    break;

  case 54: /* compControl: P_DEFINE dottedName compQstring  */
#line 286 "./asmparse.y"
                                                                                { DefineVar((yyvsp[-1].string), (yyvsp[0].binstr)); }
#line 3798 "asmparse.cpp"
    break;

  case 55: /* compControl: P_UNDEF dottedName  */
#line 287 "./asmparse.y"
                                                                                { UndefVar((yyvsp[0].string)); }
#line 3804 "asmparse.cpp"
    break;

  case 56: /* compControl: P_IFDEF dottedName  */
#line 288 "./asmparse.y"
                                                                                { SkipToken = !IsVarDefined((yyvsp[0].string));
                                                                                  IfEndif++;
                                                                                }
#line 3812 "asmparse.cpp"
    break;

  case 57: /* compControl: P_IFNDEF dottedName  */
#line 291 "./asmparse.y"
                                                                                { SkipToken = IsVarDefined((yyvsp[0].string));
                                                                                  IfEndif++;
                                                                                }
#line 3820 "asmparse.cpp"
    break;

  case 58: /* compControl: P_ELSE  */
#line 294 "./asmparse.y"
                                                                                { if(IfEndif == 1) SkipToken = !SkipToken;}
#line 3826 "asmparse.cpp"
    break;

  case 59: /* compControl: P_ENDIF  */
#line 295 "./asmparse.y"
                                                                                { if(IfEndif == 0)
                                                                                    PASM->report->error("Unmatched #endif\n");
                                                                                  else IfEndif--;
                                                                                }
#line 3835 "asmparse.cpp"
    break;

  case 60: /* compControl: P_INCLUDE QSTRING  */
#line 299 "./asmparse.y"
                                                                                { _ASSERTE(!"yylex should have dealt with this"); }
#line 3841 "asmparse.cpp"
    break;

  case 61: /* compControl: ';'  */
#line 300 "./asmparse.y"
                                                                                { }
#line 3847 "asmparse.cpp"
    break;

  case 62: /* customDescr: _CUSTOM customType  */
#line 304 "./asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[0].token), NULL); }
#line 3853 "asmparse.cpp"
    break;

  case 63: /* customDescr: _CUSTOM customType '=' compQstring  */
#line 305 "./asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].token), (yyvsp[0].binstr)); }
#line 3859 "asmparse.cpp"
    break;

  case 64: /* customDescr: _CUSTOM customType '=' '{' customBlobDescr '}'  */
#line 306 "./asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 3865 "asmparse.cpp"
    break;

  case 65: /* customDescr: customHead bytes ')'  */
#line 307 "./asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].int32), (yyvsp[-1].binstr)); }
#line 3871 "asmparse.cpp"
    break;

  case 66: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType  */
#line 310 "./asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-2].token), (yyvsp[0].token), NULL); }
#line 3877 "asmparse.cpp"
    break;

  case 67: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType '=' compQstring  */
#line 311 "./asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-4].token), (yyvsp[-2].token), (yyvsp[0].binstr)); }
#line 3883 "asmparse.cpp"
    break;

  case 68: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType '=' '{' customBlobDescr '}'  */
#line 313 "./asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-6].token), (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 3889 "asmparse.cpp"
    break;

  case 69: /* customDescrWithOwner: customHeadWithOwner bytes ')'  */
#line 314 "./asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].int32), (yyvsp[-1].binstr)); }
#line 3895 "asmparse.cpp"
    break;

  case 70: /* customHead: _CUSTOM customType '=' '('  */
#line 317 "./asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-2].token); bParsingByteArray = TRUE; }
#line 3901 "asmparse.cpp"
    break;

  case 71: /* customHeadWithOwner: _CUSTOM '(' ownerType ')' customType '=' '('  */
#line 321 "./asmparse.y"
                                                                                { PASM->m_pCustomDescrList = NULL;
                                                                                  PASM->m_tkCurrentCVOwner = (yyvsp[-4].token);
                                                                                  (yyval.int32) = (yyvsp[-2].token); bParsingByteArray = TRUE; }
#line 3909 "asmparse.cpp"
    break;

  case 72: /* customType: methodRef  */
#line 326 "./asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 3915 "asmparse.cpp"
    break;

  case 73: /* ownerType: typeSpec  */
#line 329 "./asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 3921 "asmparse.cpp"
    break;

  case 74: /* ownerType: memberRef  */
#line 330 "./asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 3927 "asmparse.cpp"
    break;

  case 75: /* customBlobDescr: customBlobArgs customBlobNVPairs  */
#line 334 "./asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr);
                                                                                  (yyval.binstr)->appendInt16(VAL16(nCustomBlobNVPairs));
                                                                                  (yyval.binstr)->append((yyvsp[0].binstr));
                                                                                  nCustomBlobNVPairs = 0; }
#line 3936 "asmparse.cpp"
    break;

  case 76: /* customBlobArgs: %empty  */
#line 340 "./asmparse.y"
                                                                                { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt16(VAL16(0x0001)); }
#line 3942 "asmparse.cpp"
    break;

  case 77: /* customBlobArgs: customBlobArgs serInit  */
#line 341 "./asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr);
                                                                                  AppendFieldToCustomBlob((yyval.binstr),(yyvsp[0].binstr)); }
#line 3949 "asmparse.cpp"
    break;

  case 78: /* customBlobArgs: customBlobArgs compControl  */
#line 343 "./asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 3955 "asmparse.cpp"
    break;

  case 79: /* customBlobNVPairs: %empty  */
#line 346 "./asmparse.y"
                                                                                { (yyval.binstr) = new BinStr(); }
#line 3961 "asmparse.cpp"
    break;

  case 80: /* customBlobNVPairs: customBlobNVPairs fieldOrProp serializType dottedName '=' serInit  */
#line 348 "./asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-5].binstr); (yyval.binstr)->appendInt8((yyvsp[-4].int32));
                                                                                  (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                                  AppendStringWithLength((yyval.binstr),(yyvsp[-2].string));
                                                                                  AppendFieldToCustomBlob((yyval.binstr),(yyvsp[0].binstr));
                                                                                  nCustomBlobNVPairs++; }
#line 3971 "asmparse.cpp"
    break;

  case 81: /* customBlobNVPairs: customBlobNVPairs compControl  */
#line 353 "./asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 3977 "asmparse.cpp"
    break;

  case 82: /* fieldOrProp: FIELD_  */
#line 356 "./asmparse.y"
                                                                                { (yyval.int32) = SERIALIZATION_TYPE_FIELD; }
#line 3983 "asmparse.cpp"
    break;

  case 83: /* fieldOrProp: PROPERTY_  */
#line 357 "./asmparse.y"
                                                                                { (yyval.int32) = SERIALIZATION_TYPE_PROPERTY; }
#line 3989 "asmparse.cpp"
    break;

  case 84: /* customAttrDecl: customDescr  */
#line 360 "./asmparse.y"
                                                                                { if((yyvsp[0].cad)->tkOwner && !(yyvsp[0].cad)->tkInterfacePair)
                                                                                    PASM->DefineCV((yyvsp[0].cad));
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH((yyvsp[0].cad)); }
#line 3998 "asmparse.cpp"
    break;

  case 85: /* customAttrDecl: customDescrWithOwner  */
#line 364 "./asmparse.y"
                                                                                { PASM->DefineCV((yyvsp[0].cad)); }
#line 4004 "asmparse.cpp"
    break;

  case 86: /* customAttrDecl: TYPEDEF_CA  */
#line 365 "./asmparse.y"
                                                                                { CustomDescr* pNew = new CustomDescr((yyvsp[0].tdd)->m_pCA);
                                                                                  if(pNew->tkOwner == 0) pNew->tkOwner = PASM->m_tkCurrentCVOwner;
                                                                                  if(pNew->tkOwner)
                                                                                    PASM->DefineCV(pNew);
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH(pNew); }
#line 4015 "asmparse.cpp"
    break;

  case 87: /* serializType: simpleType  */
#line 373 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4021 "asmparse.cpp"
    break;

  case 88: /* serializType: TYPE_  */
#line 374 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE); }
#line 4027 "asmparse.cpp"
    break;

  case 89: /* serializType: OBJECT_  */
#line 375 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TAGGED_OBJECT); }
#line 4033 "asmparse.cpp"
    break;

  case 90: /* serializType: ENUM_ CLASS_ SQSTRING  */
#line 376 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); }
#line 4040 "asmparse.cpp"
    break;

  case 91: /* serializType: ENUM_ className  */
#line 378 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[0].token))); }
#line 4047 "asmparse.cpp"
    break;

  case 92: /* serializType: serializType '[' ']'  */
#line 380 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 4053 "asmparse.cpp"
    break;

  case 93: /* moduleHead: _MODULE  */
#line 385 "./asmparse.y"
                                                                                { PASMM->SetModuleName(NULL); PASM->m_tkCurrentCVOwner=1; }
#line 4059 "asmparse.cpp"
    break;

  case 94: /* moduleHead: _MODULE dottedName  */
#line 386 "./asmparse.y"
                                                                                { PASMM->SetModuleName((yyvsp[0].string)); PASM->m_tkCurrentCVOwner=1; }
#line 4065 "asmparse.cpp"
    break;

  case 95: /* moduleHead: _MODULE EXTERN_ dottedName  */
#line 387 "./asmparse.y"
                                                                                { BinStr* pbs = new BinStr();
                                                                                  unsigned L = (unsigned)strlen((yyvsp[0].string));
                                                                                  memcpy((char*)(pbs->getBuff(L)),(yyvsp[0].string),L);
                                                                                  PASM->EmitImport(pbs); delete pbs;}
#line 4074 "asmparse.cpp"
    break;

  case 96: /* vtfixupDecl: _VTFIXUP '[' int32 ']' vtfixupAttr AT_ id  */
#line 394 "./asmparse.y"
                                                                                { /*PASM->SetDataSection(); PASM->EmitDataLabel($7);*/
                                                                                  PASM->m_VTFList.PUSH(new VTFEntry((USHORT)(yyvsp[-4].int32), (USHORT)(yyvsp[-2].int32), (yyvsp[0].string))); }
#line 4081 "asmparse.cpp"
    break;

  case 97: /* vtfixupAttr: %empty  */
#line 398 "./asmparse.y"
                                                                                { (yyval.int32) = 0; }
#line 4087 "asmparse.cpp"
    break;

  case 98: /* vtfixupAttr: vtfixupAttr INT32_  */
#line 399 "./asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_32BIT; }
#line 4093 "asmparse.cpp"
    break;

  case 99: /* vtfixupAttr: vtfixupAttr INT64_  */
#line 400 "./asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_64BIT; }
#line 4099 "asmparse.cpp"
    break;

  case 100: /* vtfixupAttr: vtfixupAttr FROMUNMANAGED_  */
#line 401 "./asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_FROM_UNMANAGED; }
#line 4105 "asmparse.cpp"
    break;

  case 101: /* vtfixupAttr: vtfixupAttr CALLMOSTDERIVED_  */
#line 402 "./asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_CALL_MOST_DERIVED; }
#line 4111 "asmparse.cpp"
    break;

  case 102: /* vtfixupAttr: vtfixupAttr RETAINAPPDOMAIN_  */
#line 403 "./asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN; }
#line 4117 "asmparse.cpp"
    break;

  case 103: /* vtableDecl: vtableHead bytes ')'  */
#line 406 "./asmparse.y"
                                                                                { PASM->m_pVTable = (yyvsp[-1].binstr); }
#line 4123 "asmparse.cpp"
    break;

  case 104: /* vtableHead: _VTABLE '=' '('  */
#line 409 "./asmparse.y"
                                                                                { bParsingByteArray = TRUE; }
#line 4129 "asmparse.cpp"
    break;

  case 105: /* nameSpaceHead: _NAMESPACE dottedName  */
#line 413 "./asmparse.y"
                                                                                { PASM->StartNameSpace((yyvsp[0].string)); }
#line 4135 "asmparse.cpp"
    break;

  case 106: /* _class: _CLASS  */
#line 416 "./asmparse.y"
                                                                                { newclass = TRUE; }
#line 4141 "asmparse.cpp"
    break;

  case 107: /* classHeadBegin: _class classAttr dottedName typarsClause  */
#line 419 "./asmparse.y"
                                                                                { if((yyvsp[0].typarlist)) FixupConstraints();
                                                                                  PASM->StartClass((yyvsp[-1].string), (yyvsp[-2].classAttr), (yyvsp[0].typarlist));
                                                                                  TyParFixupList.RESET(false);
                                                                                  newclass = FALSE;
                                                                                }
#line 4151 "asmparse.cpp"
    break;

  case 108: /* classHead: classHeadBegin extendsClause implClause  */
#line 425 "./asmparse.y"
                                                                                { PASM->AddClass(); }
#line 4157 "asmparse.cpp"
    break;

  case 109: /* classAttr: %empty  */
#line 428 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) 0; }
#line 4163 "asmparse.cpp"
    break;

  case 110: /* classAttr: classAttr PUBLIC_  */
#line 429 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdVisibilityMask) | tdPublic); }
#line 4169 "asmparse.cpp"
    break;

  case 111: /* classAttr: classAttr PRIVATE_  */
#line 430 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdVisibilityMask) | tdNotPublic); }
#line 4175 "asmparse.cpp"
    break;

  case 112: /* classAttr: classAttr VALUE_  */
#line 431 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | 0x80000000 | tdSealed); }
#line 4181 "asmparse.cpp"
    break;

  case 113: /* classAttr: classAttr ENUM_  */
#line 432 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | 0x40000000); }
#line 4187 "asmparse.cpp"
    break;

  case 114: /* classAttr: classAttr INTERFACE_  */
#line 433 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdInterface | tdAbstract); }
#line 4193 "asmparse.cpp"
    break;

  case 115: /* classAttr: classAttr SEALED_  */
#line 434 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSealed); }
#line 4199 "asmparse.cpp"
    break;

  case 116: /* classAttr: classAttr ABSTRACT_  */
#line 435 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdAbstract); }
#line 4205 "asmparse.cpp"
    break;

  case 117: /* classAttr: classAttr AUTO_  */
#line 436 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdAutoLayout); }
#line 4211 "asmparse.cpp"
    break;

  case 118: /* classAttr: classAttr SEQUENTIAL_  */
#line 437 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdSequentialLayout); }
#line 4217 "asmparse.cpp"
    break;

  case 119: /* classAttr: classAttr EXPLICIT_  */
#line 438 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdExplicitLayout); }
#line 4223 "asmparse.cpp"
    break;

  case 120: /* classAttr: classAttr ANSI_  */
#line 439 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdAnsiClass); }
#line 4229 "asmparse.cpp"
    break;

  case 121: /* classAttr: classAttr UNICODE_  */
#line 440 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdUnicodeClass); }
#line 4235 "asmparse.cpp"
    break;

  case 122: /* classAttr: classAttr AUTOCHAR_  */
#line 441 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdAutoClass); }
#line 4241 "asmparse.cpp"
    break;

  case 123: /* classAttr: classAttr IMPORT_  */
#line 442 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdImport); }
#line 4247 "asmparse.cpp"
    break;

  case 124: /* classAttr: classAttr SERIALIZABLE_  */
#line 443 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSerializable); }
#line 4253 "asmparse.cpp"
    break;

  case 125: /* classAttr: classAttr WINDOWSRUNTIME_  */
#line 444 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdWindowsRuntime); }
#line 4259 "asmparse.cpp"
    break;

  case 126: /* classAttr: classAttr NESTED_ PUBLIC_  */
#line 445 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedPublic); }
#line 4265 "asmparse.cpp"
    break;

  case 127: /* classAttr: classAttr NESTED_ PRIVATE_  */
#line 446 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedPrivate); }
#line 4271 "asmparse.cpp"
    break;

  case 128: /* classAttr: classAttr NESTED_ FAMILY_  */
#line 447 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamily); }
#line 4277 "asmparse.cpp"
    break;

  case 129: /* classAttr: classAttr NESTED_ ASSEMBLY_  */
#line 448 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedAssembly); }
#line 4283 "asmparse.cpp"
    break;

  case 130: /* classAttr: classAttr NESTED_ FAMANDASSEM_  */
#line 449 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamANDAssem); }
#line 4289 "asmparse.cpp"
    break;

  case 131: /* classAttr: classAttr NESTED_ FAMORASSEM_  */
#line 450 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamORAssem); }
#line 4295 "asmparse.cpp"
    break;

  case 132: /* classAttr: classAttr BEFOREFIELDINIT_  */
#line 451 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdBeforeFieldInit); }
#line 4301 "asmparse.cpp"
    break;

  case 133: /* classAttr: classAttr SPECIALNAME_  */
#line 452 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSpecialName); }
#line 4307 "asmparse.cpp"
    break;

  case 134: /* classAttr: classAttr RTSPECIALNAME_  */
#line 453 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr)); }
#line 4313 "asmparse.cpp"
    break;

  case 135: /* classAttr: classAttr FLAGS_ '(' int32 ')'  */
#line 454 "./asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].int32)); }
#line 4319 "asmparse.cpp"
    break;

  case 137: /* extendsClause: EXTENDS_ typeSpec  */
#line 458 "./asmparse.y"
                                                                            { PASM->m_crExtends = (yyvsp[0].token); }
#line 4325 "asmparse.cpp"
    break;

  case 142: /* implList: implList ',' typeSpec  */
#line 469 "./asmparse.y"
                                                            { PASM->AddToImplList((yyvsp[0].token)); }
#line 4331 "asmparse.cpp"
    break;

  case 143: /* implList: typeSpec  */
#line 470 "./asmparse.y"
                                                            { PASM->AddToImplList((yyvsp[0].token)); }
#line 4337 "asmparse.cpp"
    break;

  case 144: /* typeList: %empty  */
#line 474 "./asmparse.y"
                                                            { (yyval.binstr) = new BinStr(); }
#line 4343 "asmparse.cpp"
    break;

  case 145: /* typeList: typeListNotEmpty  */
#line 475 "./asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4349 "asmparse.cpp"
    break;

  case 146: /* typeListNotEmpty: typeSpec  */
#line 478 "./asmparse.y"
                                                            { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[0].token)); }
#line 4355 "asmparse.cpp"
    break;

  case 147: /* typeListNotEmpty: typeListNotEmpty ',' typeSpec  */
#line 479 "./asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->appendInt32((yyvsp[0].token)); }
#line 4361 "asmparse.cpp"
    break;

  case 148: /* typarsClause: %empty  */
#line 482 "./asmparse.y"
                                                            { (yyval.typarlist) = NULL; PASM->m_TyParList = NULL;}
#line 4367 "asmparse.cpp"
    break;

  case 149: /* typarsClause: '<' typars '>'  */
#line 483 "./asmparse.y"
                                                            { (yyval.typarlist) = (yyvsp[-1].typarlist);   PASM->m_TyParList = (yyvsp[-1].typarlist);}
#line 4373 "asmparse.cpp"
    break;

  case 150: /* typarAttrib: '+'  */
#line 486 "./asmparse.y"
                                                            { (yyval.int32) = gpCovariant; }
#line 4379 "asmparse.cpp"
    break;

  case 151: /* typarAttrib: '-'  */
#line 487 "./asmparse.y"
                                                            { (yyval.int32) = gpContravariant; }
#line 4385 "asmparse.cpp"
    break;

  case 152: /* typarAttrib: CLASS_  */
#line 488 "./asmparse.y"
                                                            { (yyval.int32) = gpReferenceTypeConstraint; }
#line 4391 "asmparse.cpp"
    break;

  case 153: /* typarAttrib: VALUETYPE_  */
#line 489 "./asmparse.y"
                                                            { (yyval.int32) = gpNotNullableValueTypeConstraint; }
#line 4397 "asmparse.cpp"
    break;

  case 154: /* typarAttrib: BYREFLIKE_  */
#line 490 "./asmparse.y"
                                                            { (yyval.int32) = gpAcceptByRefLike; }
#line 4403 "asmparse.cpp"
    break;

  case 155: /* typarAttrib: _CTOR  */
#line 491 "./asmparse.y"
                                                            { (yyval.int32) = gpDefaultConstructorConstraint; }
#line 4409 "asmparse.cpp"
    break;

  case 156: /* typarAttrib: FLAGS_ '(' int32 ')'  */
#line 492 "./asmparse.y"
                                                            { (yyval.int32) = (CorGenericParamAttr)(yyvsp[-1].int32); }
#line 4415 "asmparse.cpp"
    break;

  case 157: /* typarAttribs: %empty  */
#line 495 "./asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 4421 "asmparse.cpp"
    break;

  case 158: /* typarAttribs: typarAttrib typarAttribs  */
#line 496 "./asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) | (yyvsp[0].int32); }
#line 4427 "asmparse.cpp"
    break;

  case 159: /* conTyparAttrib: FLAGS_ '(' int32 ')'  */
#line 499 "./asmparse.y"
                                                            { (yyval.int32) = (CorGenericParamAttr)(yyvsp[-1].int32); }
#line 4433 "asmparse.cpp"
    break;

  case 160: /* conTyparAttribs: %empty  */
#line 502 "./asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 4439 "asmparse.cpp"
    break;

  case 161: /* conTyparAttribs: conTyparAttrib conTyparAttribs  */
#line 503 "./asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) | (yyvsp[0].int32); }
#line 4445 "asmparse.cpp"
    break;

  case 162: /* typars: CONST_ conTyparAttribs typeSpec dottedName typarsRest  */
#line 506 "./asmparse.y"
                                                                                {(yyval.typarlist) = new TyParList((yyvsp[-3].int32), (yyvsp[-2].token), NULL, (yyvsp[-1].string), (yyvsp[0].typarlist)); }
#line 4451 "asmparse.cpp"
    break;

  case 163: /* typars: typarAttribs tyBound dottedName typarsRest  */
#line 507 "./asmparse.y"
                                                                     {(yyval.typarlist) = new TyParList((yyvsp[-3].int32), (yyvsp[-2].binstr), (yyvsp[-1].string), (yyvsp[0].typarlist));}
#line 4457 "asmparse.cpp"
    break;

  case 164: /* typars: typarAttribs dottedName typarsRest  */
#line 508 "./asmparse.y"
                                                               {(yyval.typarlist) = new TyParList((yyvsp[-2].int32), NULL, (yyvsp[-1].string), (yyvsp[0].typarlist));}
#line 4463 "asmparse.cpp"
    break;

  case 165: /* typarsRest: %empty  */
#line 511 "./asmparse.y"
                                                            { (yyval.typarlist) = NULL; }
#line 4469 "asmparse.cpp"
    break;

  case 166: /* typarsRest: ',' typars  */
#line 512 "./asmparse.y"
                                                            { (yyval.typarlist) = (yyvsp[0].typarlist); }
#line 4475 "asmparse.cpp"
    break;

  case 167: /* tyBound: '(' typeList ')'  */
#line 515 "./asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 4481 "asmparse.cpp"
    break;

  case 168: /* genArity: %empty  */
#line 518 "./asmparse.y"
                                                            { (yyval.int32)= 0; }
#line 4487 "asmparse.cpp"
    break;

  case 169: /* genArity: genArityNotEmpty  */
#line 519 "./asmparse.y"
                                                            { (yyval.int32) = (yyvsp[0].int32); }
#line 4493 "asmparse.cpp"
    break;

  case 170: /* genArityNotEmpty: '<' '[' int32 ']' '>'  */
#line 522 "./asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-2].int32); }
#line 4499 "asmparse.cpp"
    break;

  case 171: /* classDecl: methodHead methodDecls '}'  */
#line 526 "./asmparse.y"
                                                            { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                              {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                 PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                              PASM->EndMethod(); }
#line 4508 "asmparse.cpp"
    break;

  case 172: /* classDecl: classHead '{' classDecls '}'  */
#line 530 "./asmparse.y"
                                                            { PASM->EndClass(); }
#line 4514 "asmparse.cpp"
    break;

  case 173: /* classDecl: eventHead '{' eventDecls '}'  */
#line 531 "./asmparse.y"
                                                            { PASM->EndEvent(); }
#line 4520 "asmparse.cpp"
    break;

  case 174: /* classDecl: propHead '{' propDecls '}'  */
#line 532 "./asmparse.y"
                                                            { PASM->EndProp(); }
#line 4526 "asmparse.cpp"
    break;

  case 180: /* classDecl: _SIZE int32  */
#line 538 "./asmparse.y"
                                                                { PASM->m_pCurClass->m_ulSize = (yyvsp[0].int32); }
#line 4532 "asmparse.cpp"
    break;

  case 181: /* classDecl: _PACK int32  */
#line 539 "./asmparse.y"
                                                                { PASM->m_pCurClass->m_ulPack = (yyvsp[0].int32); }
#line 4538 "asmparse.cpp"
    break;

  case 182: /* classDecl: exportHead '{' exptypeDecls '}'  */
#line 540 "./asmparse.y"
                                                                { PASMM->EndComType(); }
#line 4544 "asmparse.cpp"
    break;

  case 183: /* classDecl: _OVERRIDE typeSpec DCOLON methodName WITH_ callConv type typeSpec DCOLON methodName '(' sigArgs0 ')'  */
#line 542 "./asmparse.y"
                                                                { BinStr *sig1 = parser->MakeSig((yyvsp[-7].int32), (yyvsp[-6].binstr), (yyvsp[-1].binstr));
                                                                  BinStr *sig2 = new BinStr(); sig2->append(sig1);
                                                                  PASM->AddMethodImpl((yyvsp[-11].token),(yyvsp[-9].string),sig1,(yyvsp[-5].token),(yyvsp[-3].string),sig2);
                                                                  PASM->ResetArgNameList();
                                                                }
#line 4554 "asmparse.cpp"
    break;

  case 184: /* classDecl: _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')' WITH_ METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'  */
#line 548 "./asmparse.y"
                                                                 { PASM->AddMethodImpl((yyvsp[-17].token),(yyvsp[-15].string),
                                                                      ((yyvsp[-14].int32)==0 ? parser->MakeSig((yyvsp[-19].int32),(yyvsp[-18].binstr),(yyvsp[-12].binstr)) :
                                                                      parser->MakeSig((yyvsp[-19].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-18].binstr),(yyvsp[-12].binstr),(yyvsp[-14].int32))),
                                                                      (yyvsp[-6].token),(yyvsp[-4].string),
                                                                      ((yyvsp[-3].int32)==0 ? parser->MakeSig((yyvsp[-8].int32),(yyvsp[-7].binstr),(yyvsp[-1].binstr)) :
                                                                      parser->MakeSig((yyvsp[-8].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-7].binstr),(yyvsp[-1].binstr),(yyvsp[-3].int32))));
                                                                   PASM->ResetArgNameList();
                                                                 }
#line 4567 "asmparse.cpp"
    break;

  case 187: /* classDecl: _PARAM TYPE_ '[' int32 ']'  */
#line 558 "./asmparse.y"
                                                            { if(((yyvsp[-1].int32) > 0) && ((yyvsp[-1].int32) <= (int)PASM->m_pCurClass->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[(yyvsp[-1].int32)-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
#line 4577 "asmparse.cpp"
    break;

  case 188: /* classDecl: _PARAM TYPE_ dottedName  */
#line 563 "./asmparse.y"
                                                            { int n = PASM->m_pCurClass->FindTyPar((yyvsp[0].string));
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",(yyvsp[0].string));
                                                            }
#line 4588 "asmparse.cpp"
    break;

  case 189: /* classDecl: _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec  */
#line 569 "./asmparse.y"
                                                                        { PASM->AddGenericParamConstraint((yyvsp[-3].int32), 0, (yyvsp[0].token)); }
#line 4594 "asmparse.cpp"
    break;

  case 190: /* classDecl: _PARAM CONSTRAINT_ dottedName ',' typeSpec  */
#line 570 "./asmparse.y"
                                                                        { PASM->AddGenericParamConstraint(0, (yyvsp[-2].string), (yyvsp[0].token)); }
#line 4600 "asmparse.cpp"
    break;

  case 191: /* classDecl: _INTERFACEIMPL TYPE_ typeSpec customDescr  */
#line 571 "./asmparse.y"
                                                                      { (yyvsp[0].cad)->tkInterfacePair = (yyvsp[-1].token);
                                                                        if(PASM->m_pCustomDescrList)
                                                                            PASM->m_pCustomDescrList->PUSH((yyvsp[0].cad));
                                                                      }
#line 4609 "asmparse.cpp"
    break;

  case 192: /* fieldDecl: _FIELD repeatOpt fieldAttr type dottedName atOpt initOpt  */
#line 579 "./asmparse.y"
                                                            { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                              PASM->AddField((yyvsp[-2].string), (yyvsp[-3].binstr), (yyvsp[-4].fieldAttr), (yyvsp[-1].string), (yyvsp[0].binstr), (yyvsp[-5].int32)); }
#line 4616 "asmparse.cpp"
    break;

  case 193: /* fieldAttr: %empty  */
#line 583 "./asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) 0; }
#line 4622 "asmparse.cpp"
    break;

  case 194: /* fieldAttr: fieldAttr STATIC_  */
#line 584 "./asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdStatic); }
#line 4628 "asmparse.cpp"
    break;

  case 195: /* fieldAttr: fieldAttr PUBLIC_  */
#line 585 "./asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPublic); }
#line 4634 "asmparse.cpp"
    break;

  case 196: /* fieldAttr: fieldAttr PRIVATE_  */
#line 586 "./asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPrivate); }
#line 4640 "asmparse.cpp"
    break;

  case 197: /* fieldAttr: fieldAttr FAMILY_  */
#line 587 "./asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamily); }
#line 4646 "asmparse.cpp"
    break;

  case 198: /* fieldAttr: fieldAttr INITONLY_  */
#line 588 "./asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdInitOnly); }
#line 4652 "asmparse.cpp"
    break;

  case 199: /* fieldAttr: fieldAttr RTSPECIALNAME_  */
#line 589 "./asmparse.y"
                                                            { (yyval.fieldAttr) = (yyvsp[-1].fieldAttr); }
#line 4658 "asmparse.cpp"
    break;

  case 200: /* fieldAttr: fieldAttr SPECIALNAME_  */
#line 590 "./asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdSpecialName); }
#line 4664 "asmparse.cpp"
    break;

  case 201: /* fieldAttr: fieldAttr MARSHAL_ '(' marshalBlob ')'  */
#line 603 "./asmparse.y"
                                                            { PASM->m_pMarshal = (yyvsp[-1].binstr); }
#line 4670 "asmparse.cpp"
    break;

  case 202: /* fieldAttr: fieldAttr ASSEMBLY_  */
#line 604 "./asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdAssembly); }
#line 4676 "asmparse.cpp"
    break;

  case 203: /* fieldAttr: fieldAttr FAMANDASSEM_  */
#line 605 "./asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamANDAssem); }
#line 4682 "asmparse.cpp"
    break;

  case 204: /* fieldAttr: fieldAttr FAMORASSEM_  */
#line 606 "./asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamORAssem); }
#line 4688 "asmparse.cpp"
    break;

  case 205: /* fieldAttr: fieldAttr PRIVATESCOPE_  */
#line 607 "./asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPrivateScope); }
#line 4694 "asmparse.cpp"
    break;

  case 206: /* fieldAttr: fieldAttr LITERAL_  */
#line 608 "./asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdLiteral); }
#line 4700 "asmparse.cpp"
    break;

  case 207: /* fieldAttr: fieldAttr NOTSERIALIZED_  */
#line 609 "./asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdNotSerialized); }
#line 4706 "asmparse.cpp"
    break;

  case 208: /* fieldAttr: fieldAttr FLAGS_ '(' int32 ')'  */
#line 610 "./asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].int32)); }
#line 4712 "asmparse.cpp"
    break;

  case 209: /* atOpt: %empty  */
#line 613 "./asmparse.y"
                                                            { (yyval.string) = 0; }
#line 4718 "asmparse.cpp"
    break;

  case 210: /* atOpt: AT_ id  */
#line 614 "./asmparse.y"
                                                            { (yyval.string) = (yyvsp[0].string); }
#line 4724 "asmparse.cpp"
    break;

  case 211: /* initOpt: %empty  */
#line 617 "./asmparse.y"
                                                            { (yyval.binstr) = NULL; }
#line 4730 "asmparse.cpp"
    break;

  case 212: /* initOpt: '=' fieldInit  */
#line 618 "./asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4736 "asmparse.cpp"
    break;

  case 213: /* repeatOpt: %empty  */
#line 621 "./asmparse.y"
                                                            { (yyval.int32) = 0xFFFFFFFF; }
#line 4742 "asmparse.cpp"
    break;

  case 214: /* repeatOpt: '[' int32 ']'  */
#line 622 "./asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32); }
#line 4748 "asmparse.cpp"
    break;

  case 215: /* methodRef: callConv type typeSpec DCOLON methodName tyArgs0 '(' sigArgs0 ')'  */
#line 627 "./asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if ((yyvsp[-3].binstr) == NULL)
                                                               {
                                                                 if((iCallConv)&&(((yyvsp[-8].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                                 (yyval.token) = PASM->MakeMemberRef((yyvsp[-6].token), (yyvsp[-4].string), parser->MakeSig((yyvsp[-8].int32)|iCallConv, (yyvsp[-7].binstr), (yyvsp[-1].binstr)));
                                                               }
                                                               else
                                                               {
                                                                 mdToken mr;
                                                                 if((iCallConv)&&(((yyvsp[-8].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                                 mr = PASM->MakeMemberRef((yyvsp[-6].token), (yyvsp[-4].string),
                                                                   parser->MakeSig((yyvsp[-8].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-7].binstr), (yyvsp[-1].binstr), corCountArgs((yyvsp[-3].binstr))));
                                                                 (yyval.token) = PASM->MakeMethodSpec(mr,
                                                                   parser->MakeSig(IMAGE_CEE_CS_CALLCONV_INSTANTIATION, 0, (yyvsp[-3].binstr)));
                                                               }
                                                             }
#line 4769 "asmparse.cpp"
    break;

  case 216: /* methodRef: callConv type typeSpec DCOLON methodName genArityNotEmpty '(' sigArgs0 ')'  */
#line 644 "./asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(((yyvsp[-8].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               (yyval.token) = PASM->MakeMemberRef((yyvsp[-6].token), (yyvsp[-4].string),
                                                                 parser->MakeSig((yyvsp[-8].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-7].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32)));
                                                             }
#line 4779 "asmparse.cpp"
    break;

  case 217: /* methodRef: callConv type methodName tyArgs0 '(' sigArgs0 ')'  */
#line 650 "./asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if ((yyvsp[-3].binstr) == NULL)
                                                               {
                                                                 if((iCallConv)&&(((yyvsp[-6].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                                 (yyval.token) = PASM->MakeMemberRef(mdTokenNil, (yyvsp[-4].string), parser->MakeSig((yyvsp[-6].int32)|iCallConv, (yyvsp[-5].binstr), (yyvsp[-1].binstr)));
                                                               }
                                                               else
                                                               {
                                                                 mdToken mr;
                                                                 if((iCallConv)&&(((yyvsp[-6].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                                 mr = PASM->MakeMemberRef(mdTokenNil, (yyvsp[-4].string), parser->MakeSig((yyvsp[-6].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-5].binstr), (yyvsp[-1].binstr), corCountArgs((yyvsp[-3].binstr))));
                                                                 (yyval.token) = PASM->MakeMethodSpec(mr,
                                                                   parser->MakeSig(IMAGE_CEE_CS_CALLCONV_INSTANTIATION, 0, (yyvsp[-3].binstr)));
                                                               }
                                                             }
#line 4799 "asmparse.cpp"
    break;

  case 218: /* methodRef: callConv type methodName genArityNotEmpty '(' sigArgs0 ')'  */
#line 666 "./asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(((yyvsp[-6].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               (yyval.token) = PASM->MakeMemberRef(mdTokenNil, (yyvsp[-4].string), parser->MakeSig((yyvsp[-6].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-5].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32)));
                                                             }
#line 4808 "asmparse.cpp"
    break;

  case 219: /* methodRef: mdtoken  */
#line 670 "./asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token); }
#line 4814 "asmparse.cpp"
    break;

  case 220: /* methodRef: TYPEDEF_M  */
#line 671 "./asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 4820 "asmparse.cpp"
    break;

  case 221: /* methodRef: TYPEDEF_MR  */
#line 672 "./asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 4826 "asmparse.cpp"
    break;

  case 222: /* callConv: INSTANCE_ callConv  */
#line 675 "./asmparse.y"
                                                              { (yyval.int32) = ((yyvsp[0].int32) | IMAGE_CEE_CS_CALLCONV_HASTHIS); }
#line 4832 "asmparse.cpp"
    break;

  case 223: /* callConv: EXPLICIT_ callConv  */
#line 676 "./asmparse.y"
                                                              { (yyval.int32) = ((yyvsp[0].int32) | IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS); }
#line 4838 "asmparse.cpp"
    break;

  case 224: /* callConv: callKind  */
#line 677 "./asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 4844 "asmparse.cpp"
    break;

  case 225: /* callConv: CALLCONV_ '(' int32 ')'  */
#line 678 "./asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32); }
#line 4850 "asmparse.cpp"
    break;

  case 226: /* callKind: %empty  */
#line 681 "./asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
#line 4856 "asmparse.cpp"
    break;

  case 227: /* callKind: DEFAULT_  */
#line 682 "./asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
#line 4862 "asmparse.cpp"
    break;

  case 228: /* callKind: VARARG_  */
#line 683 "./asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_VARARG; }
#line 4868 "asmparse.cpp"
    break;

  case 229: /* callKind: UNMANAGED_ CDECL_  */
#line 684 "./asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_C; }
#line 4874 "asmparse.cpp"
    break;

  case 230: /* callKind: UNMANAGED_ STDCALL_  */
#line 685 "./asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_STDCALL; }
#line 4880 "asmparse.cpp"
    break;

  case 231: /* callKind: UNMANAGED_ THISCALL_  */
#line 686 "./asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_THISCALL; }
#line 4886 "asmparse.cpp"
    break;

  case 232: /* callKind: UNMANAGED_ FASTCALL_  */
#line 687 "./asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_FASTCALL; }
#line 4892 "asmparse.cpp"
    break;

  case 233: /* callKind: UNMANAGED_  */
#line 688 "./asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_UNMANAGED; }
#line 4898 "asmparse.cpp"
    break;

  case 234: /* mdtoken: MDTOKEN_ '(' int32 ')'  */
#line 691 "./asmparse.y"
                                                             { (yyval.token) = (yyvsp[-1].int32); }
#line 4904 "asmparse.cpp"
    break;

  case 235: /* memberRef: methodSpec methodRef  */
#line 694 "./asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token);
                                                               PASM->delArgNameList(PASM->m_firstArgName);
                                                               PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                               PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                               PASM->SetMemberRefFixup((yyvsp[0].token),iOpcodeLen); }
#line 4914 "asmparse.cpp"
    break;

  case 236: /* memberRef: FIELD_ type typeSpec DCOLON dottedName  */
#line 700 "./asmparse.y"
                                                             { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               (yyval.token) = PASM->MakeMemberRef((yyvsp[-2].token), (yyvsp[0].string), (yyvsp[-3].binstr));
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4922 "asmparse.cpp"
    break;

  case 237: /* memberRef: FIELD_ type dottedName  */
#line 704 "./asmparse.y"
                                                             { (yyvsp[-1].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               (yyval.token) = PASM->MakeMemberRef(NULL, (yyvsp[0].string), (yyvsp[-1].binstr));
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4930 "asmparse.cpp"
    break;

  case 238: /* memberRef: FIELD_ TYPEDEF_F  */
#line 707 "./asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4937 "asmparse.cpp"
    break;

  case 239: /* memberRef: FIELD_ TYPEDEF_MR  */
#line 709 "./asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4944 "asmparse.cpp"
    break;

  case 240: /* memberRef: mdtoken  */
#line 711 "./asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token);
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4951 "asmparse.cpp"
    break;

  case 241: /* eventHead: _EVENT eventAttr typeSpec dottedName  */
#line 716 "./asmparse.y"
                                                                 { PASM->ResetEvent((yyvsp[0].string), (yyvsp[-1].token), (yyvsp[-2].eventAttr)); }
#line 4957 "asmparse.cpp"
    break;

  case 242: /* eventHead: _EVENT eventAttr dottedName  */
#line 717 "./asmparse.y"
                                                                 { PASM->ResetEvent((yyvsp[0].string), mdTypeRefNil, (yyvsp[-1].eventAttr)); }
#line 4963 "asmparse.cpp"
    break;

  case 243: /* eventAttr: %empty  */
#line 721 "./asmparse.y"
                                                            { (yyval.eventAttr) = (CorEventAttr) 0; }
#line 4969 "asmparse.cpp"
    break;

  case 244: /* eventAttr: eventAttr RTSPECIALNAME_  */
#line 722 "./asmparse.y"
                                                            { (yyval.eventAttr) = (yyvsp[-1].eventAttr); }
#line 4975 "asmparse.cpp"
    break;

  case 245: /* eventAttr: eventAttr SPECIALNAME_  */
#line 723 "./asmparse.y"
                                                            { (yyval.eventAttr) = (CorEventAttr) ((yyvsp[-1].eventAttr) | evSpecialName); }
#line 4981 "asmparse.cpp"
    break;

  case 248: /* eventDecl: _ADDON methodRef  */
#line 730 "./asmparse.y"
                                                           { PASM->SetEventMethod(0, (yyvsp[0].token)); }
#line 4987 "asmparse.cpp"
    break;

  case 249: /* eventDecl: _REMOVEON methodRef  */
#line 731 "./asmparse.y"
                                                           { PASM->SetEventMethod(1, (yyvsp[0].token)); }
#line 4993 "asmparse.cpp"
    break;

  case 250: /* eventDecl: _FIRE methodRef  */
#line 732 "./asmparse.y"
                                                           { PASM->SetEventMethod(2, (yyvsp[0].token)); }
#line 4999 "asmparse.cpp"
    break;

  case 251: /* eventDecl: _OTHER methodRef  */
#line 733 "./asmparse.y"
                                                           { PASM->SetEventMethod(3, (yyvsp[0].token)); }
#line 5005 "asmparse.cpp"
    break;

  case 256: /* propHead: _PROPERTY propAttr callConv type dottedName '(' sigArgs0 ')' initOpt  */
#line 742 "./asmparse.y"
                                                            { PASM->ResetProp((yyvsp[-4].string),
                                                              parser->MakeSig((IMAGE_CEE_CS_CALLCONV_PROPERTY |
                                                              ((yyvsp[-6].int32) & IMAGE_CEE_CS_CALLCONV_HASTHIS)),(yyvsp[-5].binstr),(yyvsp[-2].binstr)), (yyvsp[-7].propAttr), (yyvsp[0].binstr));}
#line 5013 "asmparse.cpp"
    break;

  case 257: /* propAttr: %empty  */
#line 747 "./asmparse.y"
                                                            { (yyval.propAttr) = (CorPropertyAttr) 0; }
#line 5019 "asmparse.cpp"
    break;

  case 258: /* propAttr: propAttr RTSPECIALNAME_  */
#line 748 "./asmparse.y"
                                                            { (yyval.propAttr) = (yyvsp[-1].propAttr); }
#line 5025 "asmparse.cpp"
    break;

  case 259: /* propAttr: propAttr SPECIALNAME_  */
#line 749 "./asmparse.y"
                                                            { (yyval.propAttr) = (CorPropertyAttr) ((yyvsp[-1].propAttr) | prSpecialName); }
#line 5031 "asmparse.cpp"
    break;

  case 262: /* propDecl: _SET methodRef  */
#line 757 "./asmparse.y"
                                                            { PASM->SetPropMethod(0, (yyvsp[0].token)); }
#line 5037 "asmparse.cpp"
    break;

  case 263: /* propDecl: _GET methodRef  */
#line 758 "./asmparse.y"
                                                            { PASM->SetPropMethod(1, (yyvsp[0].token)); }
#line 5043 "asmparse.cpp"
    break;

  case 264: /* propDecl: _OTHER methodRef  */
#line 759 "./asmparse.y"
                                                            { PASM->SetPropMethod(2, (yyvsp[0].token)); }
#line 5049 "asmparse.cpp"
    break;

  case 269: /* methodHeadPart1: _METHOD  */
#line 767 "./asmparse.y"
                                                            { PASM->ResetForNextMethod();
                                                              uMethodBeginLine = PASM->m_ulCurLine;
                                                              uMethodBeginColumn=PASM->m_ulCurColumn;
                                                            }
#line 5058 "asmparse.cpp"
    break;

  case 270: /* marshalClause: %empty  */
#line 773 "./asmparse.y"
                                                            { (yyval.binstr) = NULL; }
#line 5064 "asmparse.cpp"
    break;

  case 271: /* marshalClause: MARSHAL_ '(' marshalBlob ')'  */
#line 774 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 5070 "asmparse.cpp"
    break;

  case 272: /* marshalBlob: nativeType  */
#line 777 "./asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 5076 "asmparse.cpp"
    break;

  case 273: /* marshalBlob: marshalBlobHead hexbytes '}'  */
#line 778 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 5082 "asmparse.cpp"
    break;

  case 274: /* marshalBlobHead: '{'  */
#line 781 "./asmparse.y"
                                                             { bParsingByteArray = TRUE; }
#line 5088 "asmparse.cpp"
    break;

  case 275: /* methodHead: methodHeadPart1 methAttr callConv paramAttr type marshalClause methodName typarsClause '(' sigArgs0 ')' implAttr '{'  */
#line 785 "./asmparse.y"
                                                            { BinStr* sig;
                                                              if ((yyvsp[-5].typarlist) == NULL) sig = parser->MakeSig((yyvsp[-10].int32), (yyvsp[-8].binstr), (yyvsp[-3].binstr));
                                                              else {
                                                               FixupTyPars((yyvsp[-8].binstr));
                                                               sig = parser->MakeSig((yyvsp[-10].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC, (yyvsp[-8].binstr), (yyvsp[-3].binstr), (yyvsp[-5].typarlist)->Count());
                                                               FixupConstraints();
                                                              }
                                                              PASM->StartMethod((yyvsp[-6].string), sig, (yyvsp[-11].methAttr), (yyvsp[-7].binstr), (yyvsp[-9].int32), (yyvsp[-5].typarlist));
                                                              TyParFixupList.RESET(false);
                                                              PASM->SetImplAttr((USHORT)(yyvsp[-1].implAttr));
                                                              PASM->m_pCurMethod->m_ulLines[0] = uMethodBeginLine;
                                                              PASM->m_pCurMethod->m_ulColumns[0]=uMethodBeginColumn;
                                                            }
#line 5106 "asmparse.cpp"
    break;

  case 276: /* methAttr: %empty  */
#line 800 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) 0; }
#line 5112 "asmparse.cpp"
    break;

  case 277: /* methAttr: methAttr STATIC_  */
#line 801 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdStatic); }
#line 5118 "asmparse.cpp"
    break;

  case 278: /* methAttr: methAttr PUBLIC_  */
#line 802 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPublic); }
#line 5124 "asmparse.cpp"
    break;

  case 279: /* methAttr: methAttr PRIVATE_  */
#line 803 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPrivate); }
#line 5130 "asmparse.cpp"
    break;

  case 280: /* methAttr: methAttr FAMILY_  */
#line 804 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamily); }
#line 5136 "asmparse.cpp"
    break;

  case 281: /* methAttr: methAttr FINAL_  */
#line 805 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdFinal); }
#line 5142 "asmparse.cpp"
    break;

  case 282: /* methAttr: methAttr SPECIALNAME_  */
#line 806 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdSpecialName); }
#line 5148 "asmparse.cpp"
    break;

  case 283: /* methAttr: methAttr VIRTUAL_  */
#line 807 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdVirtual); }
#line 5154 "asmparse.cpp"
    break;

  case 284: /* methAttr: methAttr STRICT_  */
#line 808 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdCheckAccessOnOverride); }
#line 5160 "asmparse.cpp"
    break;

  case 285: /* methAttr: methAttr ABSTRACT_  */
#line 809 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdAbstract); }
#line 5166 "asmparse.cpp"
    break;

  case 286: /* methAttr: methAttr ASSEMBLY_  */
#line 810 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdAssem); }
#line 5172 "asmparse.cpp"
    break;

  case 287: /* methAttr: methAttr FAMANDASSEM_  */
#line 811 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamANDAssem); }
#line 5178 "asmparse.cpp"
    break;

  case 288: /* methAttr: methAttr FAMORASSEM_  */
#line 812 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamORAssem); }
#line 5184 "asmparse.cpp"
    break;

  case 289: /* methAttr: methAttr PRIVATESCOPE_  */
#line 813 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPrivateScope); }
#line 5190 "asmparse.cpp"
    break;

  case 290: /* methAttr: methAttr HIDEBYSIG_  */
#line 814 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdHideBySig); }
#line 5196 "asmparse.cpp"
    break;

  case 291: /* methAttr: methAttr NEWSLOT_  */
#line 815 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdNewSlot); }
#line 5202 "asmparse.cpp"
    break;

  case 292: /* methAttr: methAttr RTSPECIALNAME_  */
#line 816 "./asmparse.y"
                                                            { (yyval.methAttr) = (yyvsp[-1].methAttr); }
#line 5208 "asmparse.cpp"
    break;

  case 293: /* methAttr: methAttr UNMANAGEDEXP_  */
#line 817 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdUnmanagedExport); }
#line 5214 "asmparse.cpp"
    break;

  case 294: /* methAttr: methAttr REQSECOBJ_  */
#line 818 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdRequireSecObject); }
#line 5220 "asmparse.cpp"
    break;

  case 295: /* methAttr: methAttr FLAGS_ '(' int32 ')'  */
#line 819 "./asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].int32)); }
#line 5226 "asmparse.cpp"
    break;

  case 296: /* methAttr: methAttr PINVOKEIMPL_ '(' compQstring AS_ compQstring pinvAttr ')'  */
#line 821 "./asmparse.y"
                                                            { PASM->SetPinvoke((yyvsp[-4].binstr),0,(yyvsp[-2].binstr),(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-7].methAttr) | mdPinvokeImpl); }
#line 5233 "asmparse.cpp"
    break;

  case 297: /* methAttr: methAttr PINVOKEIMPL_ '(' compQstring pinvAttr ')'  */
#line 824 "./asmparse.y"
                                                            { PASM->SetPinvoke((yyvsp[-2].binstr),0,NULL,(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-5].methAttr) | mdPinvokeImpl); }
#line 5240 "asmparse.cpp"
    break;

  case 298: /* methAttr: methAttr PINVOKEIMPL_ '(' pinvAttr ')'  */
#line 827 "./asmparse.y"
                                                            { PASM->SetPinvoke(new BinStr(),0,NULL,(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-4].methAttr) | mdPinvokeImpl); }
#line 5247 "asmparse.cpp"
    break;

  case 299: /* pinvAttr: %empty  */
#line 831 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) 0; }
#line 5253 "asmparse.cpp"
    break;

  case 300: /* pinvAttr: pinvAttr NOMANGLE_  */
#line 832 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmNoMangle); }
#line 5259 "asmparse.cpp"
    break;

  case 301: /* pinvAttr: pinvAttr ANSI_  */
#line 833 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetAnsi); }
#line 5265 "asmparse.cpp"
    break;

  case 302: /* pinvAttr: pinvAttr UNICODE_  */
#line 834 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetUnicode); }
#line 5271 "asmparse.cpp"
    break;

  case 303: /* pinvAttr: pinvAttr AUTOCHAR_  */
#line 835 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetAuto); }
#line 5277 "asmparse.cpp"
    break;

  case 304: /* pinvAttr: pinvAttr LASTERR_  */
#line 836 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmSupportsLastError); }
#line 5283 "asmparse.cpp"
    break;

  case 305: /* pinvAttr: pinvAttr WINAPI_  */
#line 837 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvWinapi); }
#line 5289 "asmparse.cpp"
    break;

  case 306: /* pinvAttr: pinvAttr CDECL_  */
#line 838 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvCdecl); }
#line 5295 "asmparse.cpp"
    break;

  case 307: /* pinvAttr: pinvAttr STDCALL_  */
#line 839 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvStdcall); }
#line 5301 "asmparse.cpp"
    break;

  case 308: /* pinvAttr: pinvAttr THISCALL_  */
#line 840 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvThiscall); }
#line 5307 "asmparse.cpp"
    break;

  case 309: /* pinvAttr: pinvAttr FASTCALL_  */
#line 841 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvFastcall); }
#line 5313 "asmparse.cpp"
    break;

  case 310: /* pinvAttr: pinvAttr BESTFIT_ ':' ON_  */
#line 842 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmBestFitEnabled); }
#line 5319 "asmparse.cpp"
    break;

  case 311: /* pinvAttr: pinvAttr BESTFIT_ ':' OFF_  */
#line 843 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmBestFitDisabled); }
#line 5325 "asmparse.cpp"
    break;

  case 312: /* pinvAttr: pinvAttr CHARMAPERROR_ ':' ON_  */
#line 844 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmThrowOnUnmappableCharEnabled); }
#line 5331 "asmparse.cpp"
    break;

  case 313: /* pinvAttr: pinvAttr CHARMAPERROR_ ':' OFF_  */
#line 845 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmThrowOnUnmappableCharDisabled); }
#line 5337 "asmparse.cpp"
    break;

  case 314: /* pinvAttr: pinvAttr FLAGS_ '(' int32 ')'  */
#line 846 "./asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].int32)); }
#line 5343 "asmparse.cpp"
    break;

  case 315: /* methodName: _CTOR  */
#line 849 "./asmparse.y"
                                                            { (yyval.string) = newString(COR_CTOR_METHOD_NAME); }
#line 5349 "asmparse.cpp"
    break;

  case 316: /* methodName: _CCTOR  */
#line 850 "./asmparse.y"
                                                            { (yyval.string) = newString(COR_CCTOR_METHOD_NAME); }
#line 5355 "asmparse.cpp"
    break;

  case 317: /* methodName: dottedName  */
#line 851 "./asmparse.y"
                                                            { (yyval.string) = (yyvsp[0].string); }
#line 5361 "asmparse.cpp"
    break;

  case 318: /* paramAttr: %empty  */
#line 854 "./asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 5367 "asmparse.cpp"
    break;

  case 319: /* paramAttr: paramAttr '[' IN_ ']'  */
#line 855 "./asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdIn; }
#line 5373 "asmparse.cpp"
    break;

  case 320: /* paramAttr: paramAttr '[' OUT_ ']'  */
#line 856 "./asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdOut; }
#line 5379 "asmparse.cpp"
    break;

  case 321: /* paramAttr: paramAttr '[' OPT_ ']'  */
#line 857 "./asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdOptional; }
#line 5385 "asmparse.cpp"
    break;

  case 322: /* paramAttr: paramAttr '[' int32 ']'  */
#line 858 "./asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) + 1; }
#line 5391 "asmparse.cpp"
    break;

  case 323: /* implAttr: %empty  */
#line 861 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (miIL | miManaged); }
#line 5397 "asmparse.cpp"
    break;

  case 324: /* implAttr: implAttr NATIVE_  */
#line 862 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miNative); }
#line 5403 "asmparse.cpp"
    break;

  case 325: /* implAttr: implAttr CIL_  */
#line 863 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miIL); }
#line 5409 "asmparse.cpp"
    break;

  case 326: /* implAttr: implAttr OPTIL_  */
#line 864 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miOPTIL); }
#line 5415 "asmparse.cpp"
    break;

  case 327: /* implAttr: implAttr MANAGED_  */
#line 865 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFFB) | miManaged); }
#line 5421 "asmparse.cpp"
    break;

  case 328: /* implAttr: implAttr UNMANAGED_  */
#line 866 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFFB) | miUnmanaged); }
#line 5427 "asmparse.cpp"
    break;

  case 329: /* implAttr: implAttr FORWARDREF_  */
#line 867 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miForwardRef); }
#line 5433 "asmparse.cpp"
    break;

  case 330: /* implAttr: implAttr PRESERVESIG_  */
#line 868 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miPreserveSig); }
#line 5439 "asmparse.cpp"
    break;

  case 331: /* implAttr: implAttr RUNTIME_  */
#line 869 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miRuntime); }
#line 5445 "asmparse.cpp"
    break;

  case 332: /* implAttr: implAttr INTERNALCALL_  */
#line 870 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miInternalCall); }
#line 5451 "asmparse.cpp"
    break;

  case 333: /* implAttr: implAttr SYNCHRONIZED_  */
#line 871 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miSynchronized); }
#line 5457 "asmparse.cpp"
    break;

  case 334: /* implAttr: implAttr NOINLINING_  */
#line 872 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miNoInlining); }
#line 5463 "asmparse.cpp"
    break;

  case 335: /* implAttr: implAttr AGGRESSIVEINLINING_  */
#line 873 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAggressiveInlining); }
#line 5469 "asmparse.cpp"
    break;

  case 336: /* implAttr: implAttr NOOPTIMIZATION_  */
#line 874 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miNoOptimization); }
#line 5475 "asmparse.cpp"
    break;

  case 337: /* implAttr: implAttr AGGRESSIVEOPTIMIZATION_  */
#line 875 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAggressiveOptimization); }
#line 5481 "asmparse.cpp"
    break;

  case 338: /* implAttr: implAttr FLAGS_ '(' int32 ')'  */
#line 876 "./asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].int32)); }
#line 5487 "asmparse.cpp"
    break;

  case 339: /* localsHead: _LOCALS  */
#line 879 "./asmparse.y"
                                                            { PASM->delArgNameList(PASM->m_firstArgName); PASM->m_firstArgName = NULL;PASM->m_lastArgName = NULL;
                                                            }
#line 5494 "asmparse.cpp"
    break;

  case 342: /* methodDecl: _EMITBYTE int32  */
#line 887 "./asmparse.y"
                                                            { PASM->EmitByte((yyvsp[0].int32)); }
#line 5500 "asmparse.cpp"
    break;

  case 343: /* methodDecl: sehBlock  */
#line 888 "./asmparse.y"
                                                            { delete PASM->m_SEHD; PASM->m_SEHD = PASM->m_SEHDstack.POP(); }
#line 5506 "asmparse.cpp"
    break;

  case 344: /* methodDecl: _MAXSTACK int32  */
#line 889 "./asmparse.y"
                                                            { PASM->EmitMaxStack((yyvsp[0].int32)); }
#line 5512 "asmparse.cpp"
    break;

  case 345: /* methodDecl: localsHead '(' sigArgs0 ')'  */
#line 890 "./asmparse.y"
                                                            { PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, (yyvsp[-1].binstr)));
                                                            }
#line 5519 "asmparse.cpp"
    break;

  case 346: /* methodDecl: localsHead INIT_ '(' sigArgs0 ')'  */
#line 892 "./asmparse.y"
                                                            { PASM->EmitZeroInit();
                                                              PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, (yyvsp[-1].binstr)));
                                                            }
#line 5527 "asmparse.cpp"
    break;

  case 347: /* methodDecl: _ENTRYPOINT  */
#line 895 "./asmparse.y"
                                                            { PASM->EmitEntryPoint(); }
#line 5533 "asmparse.cpp"
    break;

  case 348: /* methodDecl: _ZEROINIT  */
#line 896 "./asmparse.y"
                                                            { PASM->EmitZeroInit(); }
#line 5539 "asmparse.cpp"
    break;

  case 351: /* methodDecl: id ':'  */
#line 899 "./asmparse.y"
                                                            { PASM->AddLabel(PASM->m_CurPC,(yyvsp[-1].string)); /*PASM->EmitLabel($1);*/ }
#line 5545 "asmparse.cpp"
    break;

  case 357: /* methodDecl: _EXPORT '[' int32 ']'  */
#line 905 "./asmparse.y"
                                                            { if(PASM->m_pCurMethod->m_dwExportOrdinal == 0xFFFFFFFF)
                                                              {
                                                                PASM->m_pCurMethod->m_dwExportOrdinal = (yyvsp[-1].int32);
                                                                PASM->m_pCurMethod->m_szExportAlias = NULL;
                                                                if(PASM->m_pCurMethod->m_wVTEntry == 0) PASM->m_pCurMethod->m_wVTEntry = 1;
                                                                if(PASM->m_pCurMethod->m_wVTSlot  == 0) PASM->m_pCurMethod->m_wVTSlot = (WORD)((yyvsp[-1].int32) + 0x8000);
                                                              }
                                                              else
                                                                PASM->report->warn("Duplicate .export directive, ignored\n");
                                                            }
#line 5560 "asmparse.cpp"
    break;

  case 358: /* methodDecl: _EXPORT '[' int32 ']' AS_ id  */
#line 915 "./asmparse.y"
                                                            { if(PASM->m_pCurMethod->m_dwExportOrdinal == 0xFFFFFFFF)
                                                              {
                                                                PASM->m_pCurMethod->m_dwExportOrdinal = (yyvsp[-3].int32);
                                                                PASM->m_pCurMethod->m_szExportAlias = (yyvsp[0].string);
                                                                if(PASM->m_pCurMethod->m_wVTEntry == 0) PASM->m_pCurMethod->m_wVTEntry = 1;
                                                                if(PASM->m_pCurMethod->m_wVTSlot  == 0) PASM->m_pCurMethod->m_wVTSlot = (WORD)((yyvsp[-3].int32) + 0x8000);
                                                              }
                                                              else
                                                                PASM->report->warn("Duplicate .export directive, ignored\n");
                                                            }
#line 5575 "asmparse.cpp"
    break;

  case 359: /* methodDecl: _VTENTRY int32 ':' int32  */
#line 925 "./asmparse.y"
                                                            { PASM->m_pCurMethod->m_wVTEntry = (WORD)(yyvsp[-2].int32);
                                                              PASM->m_pCurMethod->m_wVTSlot = (WORD)(yyvsp[0].int32); }
#line 5582 "asmparse.cpp"
    break;

  case 360: /* methodDecl: _OVERRIDE typeSpec DCOLON methodName  */
#line 928 "./asmparse.y"
                                                            { PASM->AddMethodImpl((yyvsp[-2].token),(yyvsp[0].string),NULL,NULL,NULL,NULL); }
#line 5588 "asmparse.cpp"
    break;

  case 361: /* methodDecl: _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'  */
#line 931 "./asmparse.y"
                                                            { PASM->AddMethodImpl((yyvsp[-6].token),(yyvsp[-4].string),
                                                              ((yyvsp[-3].int32)==0 ? parser->MakeSig((yyvsp[-8].int32),(yyvsp[-7].binstr),(yyvsp[-1].binstr)) :
                                                              parser->MakeSig((yyvsp[-8].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-7].binstr),(yyvsp[-1].binstr),(yyvsp[-3].int32)))
                                                              ,NULL,NULL,NULL);
                                                              PASM->ResetArgNameList();
                                                            }
#line 5599 "asmparse.cpp"
    break;

  case 363: /* methodDecl: _PARAM TYPE_ '[' int32 ']'  */
#line 938 "./asmparse.y"
                                                            { if(((yyvsp[-1].int32) > 0) && ((yyvsp[-1].int32) <= (int)PASM->m_pCurMethod->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[(yyvsp[-1].int32)-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
#line 5609 "asmparse.cpp"
    break;

  case 364: /* methodDecl: _PARAM TYPE_ dottedName  */
#line 943 "./asmparse.y"
                                                            { int n = PASM->m_pCurMethod->FindTyPar((yyvsp[0].string));
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",(yyvsp[0].string));
                                                            }
#line 5620 "asmparse.cpp"
    break;

  case 365: /* methodDecl: _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec  */
#line 949 "./asmparse.y"
                                                                        { PASM->m_pCurMethod->AddGenericParamConstraint((yyvsp[-3].int32), 0, (yyvsp[0].token)); }
#line 5626 "asmparse.cpp"
    break;

  case 366: /* methodDecl: _PARAM CONSTRAINT_ dottedName ',' typeSpec  */
#line 950 "./asmparse.y"
                                                                        { PASM->m_pCurMethod->AddGenericParamConstraint(0, (yyvsp[-2].string), (yyvsp[0].token)); }
#line 5632 "asmparse.cpp"
    break;

  case 367: /* methodDecl: _PARAM '[' int32 ']' initOpt  */
#line 953 "./asmparse.y"
                                                            { if( (yyvsp[-2].int32) ) {
                                                                ARG_NAME_LIST* pAN=PASM->findArg(PASM->m_pCurMethod->m_firstArgName, (yyvsp[-2].int32) - 1);
                                                                if(pAN)
                                                                {
                                                                    PASM->m_pCustomDescrList = &(pAN->CustDList);
                                                                    pAN->pValue = (yyvsp[0].binstr);
                                                                }
                                                                else
                                                                {
                                                                    PASM->m_pCustomDescrList = NULL;
                                                                    if((yyvsp[0].binstr)) delete (yyvsp[0].binstr);
                                                                }
                                                              } else {
                                                                PASM->m_pCustomDescrList = &(PASM->m_pCurMethod->m_RetCustDList);
                                                                PASM->m_pCurMethod->m_pRetValue = (yyvsp[0].binstr);
                                                              }
                                                              PASM->m_tkCurrentCVOwner = 0;
                                                            }
#line 5655 "asmparse.cpp"
    break;

  case 368: /* scopeBlock: scopeOpen methodDecls '}'  */
#line 973 "./asmparse.y"
                                                            { PASM->m_pCurMethod->CloseScope(); }
#line 5661 "asmparse.cpp"
    break;

  case 369: /* scopeOpen: '{'  */
#line 976 "./asmparse.y"
                                                            { PASM->m_pCurMethod->OpenScope(); }
#line 5667 "asmparse.cpp"
    break;

  case 373: /* tryBlock: tryHead scopeBlock  */
#line 987 "./asmparse.y"
                                                            { PASM->m_SEHD->tryTo = PASM->m_CurPC; }
#line 5673 "asmparse.cpp"
    break;

  case 374: /* tryBlock: tryHead id TO_ id  */
#line 988 "./asmparse.y"
                                                            { PASM->SetTryLabels((yyvsp[-2].string), (yyvsp[0].string)); }
#line 5679 "asmparse.cpp"
    break;

  case 375: /* tryBlock: tryHead int32 TO_ int32  */
#line 989 "./asmparse.y"
                                                            { if(PASM->m_SEHD) {PASM->m_SEHD->tryFrom = (yyvsp[-2].int32);
                                                              PASM->m_SEHD->tryTo = (yyvsp[0].int32);} }
#line 5686 "asmparse.cpp"
    break;

  case 376: /* tryHead: _TRY  */
#line 993 "./asmparse.y"
                                                            { PASM->NewSEHDescriptor();
                                                              PASM->m_SEHD->tryFrom = PASM->m_CurPC; }
#line 5693 "asmparse.cpp"
    break;

  case 377: /* sehClause: catchClause handlerBlock  */
#line 998 "./asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5699 "asmparse.cpp"
    break;

  case 378: /* sehClause: filterClause handlerBlock  */
#line 999 "./asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5705 "asmparse.cpp"
    break;

  case 379: /* sehClause: finallyClause handlerBlock  */
#line 1000 "./asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5711 "asmparse.cpp"
    break;

  case 380: /* sehClause: faultClause handlerBlock  */
#line 1001 "./asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5717 "asmparse.cpp"
    break;

  case 381: /* filterClause: filterHead scopeBlock  */
#line 1005 "./asmparse.y"
                                                             { PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5723 "asmparse.cpp"
    break;

  case 382: /* filterClause: filterHead id  */
#line 1006 "./asmparse.y"
                                                             { PASM->SetFilterLabel((yyvsp[0].string));
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5730 "asmparse.cpp"
    break;

  case 383: /* filterClause: filterHead int32  */
#line 1008 "./asmparse.y"
                                                             { PASM->m_SEHD->sehFilter = (yyvsp[0].int32);
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5737 "asmparse.cpp"
    break;

  case 384: /* filterHead: FILTER_  */
#line 1012 "./asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FILTER;
                                                               PASM->m_SEHD->sehFilter = PASM->m_CurPC; }
#line 5744 "asmparse.cpp"
    break;

  case 385: /* catchClause: CATCH_ typeSpec  */
#line 1016 "./asmparse.y"
                                                            {  PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_NONE;
                                                               PASM->SetCatchClass((yyvsp[0].token));
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5752 "asmparse.cpp"
    break;

  case 386: /* finallyClause: FINALLY_  */
#line 1021 "./asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FINALLY;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5759 "asmparse.cpp"
    break;

  case 387: /* faultClause: FAULT_  */
#line 1025 "./asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FAULT;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5766 "asmparse.cpp"
    break;

  case 388: /* handlerBlock: scopeBlock  */
#line 1029 "./asmparse.y"
                                                             { PASM->m_SEHD->sehHandlerTo = PASM->m_CurPC; }
#line 5772 "asmparse.cpp"
    break;

  case 389: /* handlerBlock: HANDLER_ id TO_ id  */
#line 1030 "./asmparse.y"
                                                             { PASM->SetHandlerLabels((yyvsp[-2].string), (yyvsp[0].string)); }
#line 5778 "asmparse.cpp"
    break;

  case 390: /* handlerBlock: HANDLER_ int32 TO_ int32  */
#line 1031 "./asmparse.y"
                                                             { PASM->m_SEHD->sehHandler = (yyvsp[-2].int32);
                                                               PASM->m_SEHD->sehHandlerTo = (yyvsp[0].int32); }
#line 5785 "asmparse.cpp"
    break;

  case 392: /* ddHead: _DATA tls id '='  */
#line 1039 "./asmparse.y"
                                                             { PASM->EmitDataLabel((yyvsp[-1].string)); }
#line 5791 "asmparse.cpp"
    break;

  case 394: /* tls: %empty  */
#line 1043 "./asmparse.y"
                                                             { PASM->SetDataSection(); }
#line 5797 "asmparse.cpp"
    break;

  case 395: /* tls: TLS_  */
#line 1044 "./asmparse.y"
                                                             { PASM->SetTLSSection(); }
#line 5803 "asmparse.cpp"
    break;

  case 396: /* tls: CIL_  */
#line 1045 "./asmparse.y"
                                                             { PASM->SetILSection(); }
#line 5809 "asmparse.cpp"
    break;

  case 401: /* ddItemCount: %empty  */
#line 1056 "./asmparse.y"
                                                             { (yyval.int32) = 1; }
#line 5815 "asmparse.cpp"
    break;

  case 402: /* ddItemCount: '[' int32 ']'  */
#line 1057 "./asmparse.y"
                                                             { (yyval.int32) = (yyvsp[-1].int32);
                                                               if((yyvsp[-1].int32) <= 0) { PASM->report->error("Illegal item count: %d\n",(yyvsp[-1].int32));
                                                                  if(!PASM->OnErrGo) (yyval.int32) = 1; }}
#line 5823 "asmparse.cpp"
    break;

  case 403: /* ddItem: CHAR_ '*' '(' compQstring ')'  */
#line 1062 "./asmparse.y"
                                                             { PASM->EmitDataString((yyvsp[-1].binstr)); }
#line 5829 "asmparse.cpp"
    break;

  case 404: /* ddItem: '&' '(' id ')'  */
#line 1063 "./asmparse.y"
                                                             { PASM->EmitDD((yyvsp[-1].string)); }
#line 5835 "asmparse.cpp"
    break;

  case 405: /* ddItem: bytearrayhead bytes ')'  */
#line 1064 "./asmparse.y"
                                                             { PASM->EmitData((yyvsp[-1].binstr)->ptr(),(yyvsp[-1].binstr)->length()); }
#line 5841 "asmparse.cpp"
    break;

  case 406: /* ddItem: FLOAT32_ '(' float64 ')' ddItemCount  */
#line 1066 "./asmparse.y"
                                                             { float f = (float) (*(yyvsp[-2].float64)); float* p = new (nothrow) float[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i < (yyvsp[0].int32); i++) p[i] = f;
                                                                 PASM->EmitData(p, sizeof(float)*(yyvsp[0].int32)); delete (yyvsp[-2].float64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(float)*(yyvsp[0].int32)); }
#line 5852 "asmparse.cpp"
    break;

  case 407: /* ddItem: FLOAT64_ '(' float64 ')' ddItemCount  */
#line 1073 "./asmparse.y"
                                                             { double* p = new (nothrow) double[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = *((yyvsp[-2].float64));
                                                                 PASM->EmitData(p, sizeof(double)*(yyvsp[0].int32)); delete (yyvsp[-2].float64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(double)*(yyvsp[0].int32)); }
#line 5863 "asmparse.cpp"
    break;

  case 408: /* ddItem: INT64_ '(' int64 ')' ddItemCount  */
#line 1080 "./asmparse.y"
                                                             { __int64* p = new (nothrow) __int64[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = *((yyvsp[-2].int64));
                                                                 PASM->EmitData(p, sizeof(__int64)*(yyvsp[0].int32)); delete (yyvsp[-2].int64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(__int64)*(yyvsp[0].int32)); }
#line 5874 "asmparse.cpp"
    break;

  case 409: /* ddItem: INT32_ '(' int32 ')' ddItemCount  */
#line 1087 "./asmparse.y"
                                                             { __int32* p = new (nothrow) __int32[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = (yyvsp[-2].int32);
                                                                 PASM->EmitData(p, sizeof(__int32)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(__int32)*(yyvsp[0].int32)); }
#line 5885 "asmparse.cpp"
    break;

  case 410: /* ddItem: INT16_ '(' int32 ')' ddItemCount  */
#line 1094 "./asmparse.y"
                                                             { __int16 i = (__int16) (yyvsp[-2].int32); FAIL_UNLESS(i == (yyvsp[-2].int32), ("Value %d too big\n", (yyvsp[-2].int32)));
                                                               __int16* p = new (nothrow) __int16[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<(yyvsp[0].int32); j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(__int16)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(__int16)*(yyvsp[0].int32)); }
#line 5897 "asmparse.cpp"
    break;

  case 411: /* ddItem: INT8_ '(' int32 ')' ddItemCount  */
#line 1102 "./asmparse.y"
                                                             { __int8 i = (__int8) (yyvsp[-2].int32); FAIL_UNLESS(i == (yyvsp[-2].int32), ("Value %d too big\n", (yyvsp[-2].int32)));
                                                               __int8* p = new (nothrow) __int8[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<(yyvsp[0].int32); j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(__int8)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(__int8)*(yyvsp[0].int32)); }
#line 5909 "asmparse.cpp"
    break;

  case 412: /* ddItem: FLOAT32_ ddItemCount  */
#line 1109 "./asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(float)*(yyvsp[0].int32)); }
#line 5915 "asmparse.cpp"
    break;

  case 413: /* ddItem: FLOAT64_ ddItemCount  */
#line 1110 "./asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(double)*(yyvsp[0].int32)); }
#line 5921 "asmparse.cpp"
    break;

  case 414: /* ddItem: INT64_ ddItemCount  */
#line 1111 "./asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(__int64)*(yyvsp[0].int32)); }
#line 5927 "asmparse.cpp"
    break;

  case 415: /* ddItem: INT32_ ddItemCount  */
#line 1112 "./asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(__int32)*(yyvsp[0].int32)); }
#line 5933 "asmparse.cpp"
    break;

  case 416: /* ddItem: INT16_ ddItemCount  */
#line 1113 "./asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(__int16)*(yyvsp[0].int32)); }
#line 5939 "asmparse.cpp"
    break;

  case 417: /* ddItem: INT8_ ddItemCount  */
#line 1114 "./asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(__int8)*(yyvsp[0].int32)); }
#line 5945 "asmparse.cpp"
    break;

  case 418: /* fieldSerInit: FLOAT32_ '(' float64 ')'  */
#line 1118 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               float f = (float)(*(yyvsp[-1].float64));
                                                               (yyval.binstr)->appendInt32(*((__int32*)&f)); delete (yyvsp[-1].float64); }
#line 5953 "asmparse.cpp"
    break;

  case 419: /* fieldSerInit: FLOAT64_ '(' float64 ')'  */
#line 1121 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[-1].float64)); delete (yyvsp[-1].float64); }
#line 5960 "asmparse.cpp"
    break;

  case 420: /* fieldSerInit: FLOAT32_ '(' int32 ')'  */
#line 1123 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 5967 "asmparse.cpp"
    break;

  case 421: /* fieldSerInit: FLOAT64_ '(' int64 ')'  */
#line 1125 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 5974 "asmparse.cpp"
    break;

  case 422: /* fieldSerInit: INT64_ '(' int64 ')'  */
#line 1127 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I8);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 5981 "asmparse.cpp"
    break;

  case 423: /* fieldSerInit: INT32_ '(' int32 ')'  */
#line 1129 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 5988 "asmparse.cpp"
    break;

  case 424: /* fieldSerInit: INT16_ '(' int32 ')'  */
#line 1131 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 5995 "asmparse.cpp"
    break;

  case 425: /* fieldSerInit: INT8_ '(' int32 ')'  */
#line 1133 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6002 "asmparse.cpp"
    break;

  case 426: /* fieldSerInit: UNSIGNED_ INT64_ '(' int64 ')'  */
#line 1135 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6009 "asmparse.cpp"
    break;

  case 427: /* fieldSerInit: UNSIGNED_ INT32_ '(' int32 ')'  */
#line 1137 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6016 "asmparse.cpp"
    break;

  case 428: /* fieldSerInit: UNSIGNED_ INT16_ '(' int32 ')'  */
#line 1139 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6023 "asmparse.cpp"
    break;

  case 429: /* fieldSerInit: UNSIGNED_ INT8_ '(' int32 ')'  */
#line 1141 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6030 "asmparse.cpp"
    break;

  case 430: /* fieldSerInit: UINT64_ '(' int64 ')'  */
#line 1143 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6037 "asmparse.cpp"
    break;

  case 431: /* fieldSerInit: UINT32_ '(' int32 ')'  */
#line 1145 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6044 "asmparse.cpp"
    break;

  case 432: /* fieldSerInit: UINT16_ '(' int32 ')'  */
#line 1147 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6051 "asmparse.cpp"
    break;

  case 433: /* fieldSerInit: UINT8_ '(' int32 ')'  */
#line 1149 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6058 "asmparse.cpp"
    break;

  case 434: /* fieldSerInit: CHAR_ '(' int32 ')'  */
#line 1151 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CHAR);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6065 "asmparse.cpp"
    break;

  case 435: /* fieldSerInit: BOOL_ '(' truefalse ')'  */
#line 1153 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_BOOLEAN);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32));}
#line 6072 "asmparse.cpp"
    break;

  case 436: /* fieldSerInit: bytearrayhead bytes ')'  */
#line 1155 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING);
                                                               (yyval.binstr)->append((yyvsp[-1].binstr)); delete (yyvsp[-1].binstr);}
#line 6079 "asmparse.cpp"
    break;

  case 437: /* bytearrayhead: BYTEARRAY_ '('  */
#line 1159 "./asmparse.y"
                                                             { bParsingByteArray = TRUE; }
#line 6085 "asmparse.cpp"
    break;

  case 438: /* bytes: %empty  */
#line 1162 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6091 "asmparse.cpp"
    break;

  case 439: /* bytes: hexbytes  */
#line 1163 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6097 "asmparse.cpp"
    break;

  case 440: /* hexbytes: HEXBYTE  */
#line 1166 "./asmparse.y"
                                                             { __int8 i = (__int8) (yyvsp[0].int32); (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(i); }
#line 6103 "asmparse.cpp"
    break;

  case 441: /* hexbytes: hexbytes HEXBYTE  */
#line 1167 "./asmparse.y"
                                                             { __int8 i = (__int8) (yyvsp[0].int32); (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(i); }
#line 6109 "asmparse.cpp"
    break;

  case 442: /* fieldInit: fieldSerInit  */
#line 1171 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6115 "asmparse.cpp"
    break;

  case 443: /* fieldInit: compQstring  */
#line 1172 "./asmparse.y"
                                                             { (yyval.binstr) = BinStrToUnicode((yyvsp[0].binstr),true); (yyval.binstr)->insertInt8(ELEMENT_TYPE_STRING);}
#line 6121 "asmparse.cpp"
    break;

  case 444: /* fieldInit: NULLREF_  */
#line 1173 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CLASS);
                                                               (yyval.binstr)->appendInt32(0); }
#line 6128 "asmparse.cpp"
    break;

  case 445: /* serInit: fieldSerInit  */
#line 1178 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6134 "asmparse.cpp"
    break;

  case 446: /* serInit: STRING_ '(' NULLREF_ ')'  */
#line 1179 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); (yyval.binstr)->appendInt8(0xFF); }
#line 6140 "asmparse.cpp"
    break;

  case 447: /* serInit: STRING_ '(' SQSTRING ')'  */
#line 1180 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[-1].string)); delete [] (yyvsp[-1].string);}
#line 6147 "asmparse.cpp"
    break;

  case 448: /* serInit: TYPE_ '(' CLASS_ SQSTRING ')'  */
#line 1182 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[-1].string)); delete [] (yyvsp[-1].string);}
#line 6154 "asmparse.cpp"
    break;

  case 449: /* serInit: TYPE_ '(' className ')'  */
#line 1184 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[-1].token)));}
#line 6161 "asmparse.cpp"
    break;

  case 450: /* serInit: TYPE_ '(' NULLREF_ ')'  */
#line 1186 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE); (yyval.binstr)->appendInt8(0xFF); }
#line 6167 "asmparse.cpp"
    break;

  case 451: /* serInit: OBJECT_ '(' serInit ')'  */
#line 1187 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);}
#line 6173 "asmparse.cpp"
    break;

  case 452: /* serInit: FLOAT32_ '[' int32 ']' '(' f32seq ')'  */
#line 1189 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_R4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6181 "asmparse.cpp"
    break;

  case 453: /* serInit: FLOAT64_ '[' int32 ']' '(' f64seq ')'  */
#line 1193 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6189 "asmparse.cpp"
    break;

  case 454: /* serInit: INT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1197 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6197 "asmparse.cpp"
    break;

  case 455: /* serInit: INT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1201 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6205 "asmparse.cpp"
    break;

  case 456: /* serInit: INT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1205 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6213 "asmparse.cpp"
    break;

  case 457: /* serInit: INT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1209 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6221 "asmparse.cpp"
    break;

  case 458: /* serInit: UINT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1213 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6229 "asmparse.cpp"
    break;

  case 459: /* serInit: UINT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1217 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6237 "asmparse.cpp"
    break;

  case 460: /* serInit: UINT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1221 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6245 "asmparse.cpp"
    break;

  case 461: /* serInit: UINT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1225 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6253 "asmparse.cpp"
    break;

  case 462: /* serInit: UNSIGNED_ INT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1229 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6261 "asmparse.cpp"
    break;

  case 463: /* serInit: UNSIGNED_ INT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1233 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6269 "asmparse.cpp"
    break;

  case 464: /* serInit: UNSIGNED_ INT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1237 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6277 "asmparse.cpp"
    break;

  case 465: /* serInit: UNSIGNED_ INT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1241 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6285 "asmparse.cpp"
    break;

  case 466: /* serInit: CHAR_ '[' int32 ']' '(' i16seq ')'  */
#line 1245 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_CHAR);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6293 "asmparse.cpp"
    break;

  case 467: /* serInit: BOOL_ '[' int32 ']' '(' boolSeq ')'  */
#line 1249 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_BOOLEAN);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6301 "asmparse.cpp"
    break;

  case 468: /* serInit: STRING_ '[' int32 ']' '(' sqstringSeq ')'  */
#line 1253 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_STRING);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6309 "asmparse.cpp"
    break;

  case 469: /* serInit: TYPE_ '[' int32 ']' '(' classSeq ')'  */
#line 1257 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TYPE);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6317 "asmparse.cpp"
    break;

  case 470: /* serInit: OBJECT_ '[' int32 ']' '(' objSeq ')'  */
#line 1261 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6325 "asmparse.cpp"
    break;

  case 471: /* f32seq: %empty  */
#line 1267 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6331 "asmparse.cpp"
    break;

  case 472: /* f32seq: f32seq float64  */
#line 1268 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               float f = (float) (*(yyvsp[0].float64)); (yyval.binstr)->appendInt32(*((__int32*)&f)); delete (yyvsp[0].float64); }
#line 6338 "asmparse.cpp"
    break;

  case 473: /* f32seq: f32seq int32  */
#line 1270 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 6345 "asmparse.cpp"
    break;

  case 474: /* f64seq: %empty  */
#line 1274 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6351 "asmparse.cpp"
    break;

  case 475: /* f64seq: f64seq float64  */
#line 1275 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[0].float64)); delete (yyvsp[0].float64); }
#line 6358 "asmparse.cpp"
    break;

  case 476: /* f64seq: f64seq int64  */
#line 1277 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[0].int64)); delete (yyvsp[0].int64); }
#line 6365 "asmparse.cpp"
    break;

  case 477: /* i64seq: %empty  */
#line 1281 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6371 "asmparse.cpp"
    break;

  case 478: /* i64seq: i64seq int64  */
#line 1282 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[0].int64)); delete (yyvsp[0].int64); }
#line 6378 "asmparse.cpp"
    break;

  case 479: /* i32seq: %empty  */
#line 1286 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6384 "asmparse.cpp"
    break;

  case 480: /* i32seq: i32seq int32  */
#line 1287 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt32((yyvsp[0].int32));}
#line 6390 "asmparse.cpp"
    break;

  case 481: /* i16seq: %empty  */
#line 1290 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6396 "asmparse.cpp"
    break;

  case 482: /* i16seq: i16seq int32  */
#line 1291 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt16((yyvsp[0].int32));}
#line 6402 "asmparse.cpp"
    break;

  case 483: /* i8seq: %empty  */
#line 1294 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6408 "asmparse.cpp"
    break;

  case 484: /* i8seq: i8seq int32  */
#line 1295 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8((yyvsp[0].int32)); }
#line 6414 "asmparse.cpp"
    break;

  case 485: /* boolSeq: %empty  */
#line 1298 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6420 "asmparse.cpp"
    break;

  case 486: /* boolSeq: boolSeq truefalse  */
#line 1299 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt8((yyvsp[0].int32));}
#line 6427 "asmparse.cpp"
    break;

  case 487: /* sqstringSeq: %empty  */
#line 1303 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6433 "asmparse.cpp"
    break;

  case 488: /* sqstringSeq: sqstringSeq NULLREF_  */
#line 1304 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(0xFF); }
#line 6439 "asmparse.cpp"
    break;

  case 489: /* sqstringSeq: sqstringSeq SQSTRING  */
#line 1305 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); delete [] (yyvsp[0].string);}
#line 6446 "asmparse.cpp"
    break;

  case 490: /* classSeq: %empty  */
#line 1309 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6452 "asmparse.cpp"
    break;

  case 491: /* classSeq: classSeq NULLREF_  */
#line 1310 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(0xFF); }
#line 6458 "asmparse.cpp"
    break;

  case 492: /* classSeq: classSeq CLASS_ SQSTRING  */
#line 1311 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); delete [] (yyvsp[0].string);}
#line 6465 "asmparse.cpp"
    break;

  case 493: /* classSeq: classSeq className  */
#line 1313 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[0].token)));}
#line 6472 "asmparse.cpp"
    break;

  case 494: /* objSeq: %empty  */
#line 1317 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6478 "asmparse.cpp"
    break;

  case 495: /* objSeq: objSeq serInit  */
#line 1318 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 6484 "asmparse.cpp"
    break;

  case 496: /* methodSpec: METHOD_  */
#line 1322 "./asmparse.y"
                                                             { parser->m_ANSFirst.PUSH(PASM->m_firstArgName);
                                                               parser->m_ANSLast.PUSH(PASM->m_lastArgName);
                                                               PASM->m_firstArgName = NULL;
                                                               PASM->m_lastArgName = NULL; }
#line 6493 "asmparse.cpp"
    break;

  case 497: /* instr_none: INSTR_NONE  */
#line 1328 "./asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6499 "asmparse.cpp"
    break;

  case 498: /* instr_var: INSTR_VAR  */
#line 1331 "./asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6505 "asmparse.cpp"
    break;

  case 499: /* instr_i: INSTR_I  */
#line 1334 "./asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6511 "asmparse.cpp"
    break;

  case 500: /* instr_i8: INSTR_I8  */
#line 1337 "./asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6517 "asmparse.cpp"
    break;

  case 501: /* instr_r: INSTR_R  */
#line 1340 "./asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6523 "asmparse.cpp"
    break;

  case 502: /* instr_brtarget: INSTR_BRTARGET  */
#line 1343 "./asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6529 "asmparse.cpp"
    break;

  case 503: /* instr_method: INSTR_METHOD  */
#line 1346 "./asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode));
                                                               if((!PASM->OnErrGo)&&
                                                               (((yyvsp[0].opcode) == CEE_NEWOBJ)||
                                                                ((yyvsp[0].opcode) == CEE_CALLVIRT)))
                                                                  iCallConv = IMAGE_CEE_CS_CALLCONV_HASTHIS;
                                                             }
#line 6540 "asmparse.cpp"
    break;

  case 504: /* instr_field: INSTR_FIELD  */
#line 1354 "./asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6546 "asmparse.cpp"
    break;

  case 505: /* instr_type: INSTR_TYPE  */
#line 1357 "./asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6552 "asmparse.cpp"
    break;

  case 506: /* instr_string: INSTR_STRING  */
#line 1360 "./asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6558 "asmparse.cpp"
    break;

  case 507: /* instr_sig: INSTR_SIG  */
#line 1363 "./asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6564 "asmparse.cpp"
    break;

  case 508: /* instr_tok: INSTR_TOK  */
#line 1366 "./asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); iOpcodeLen = PASM->OpcodeLen((yyval.instr)); }
#line 6570 "asmparse.cpp"
    break;

  case 509: /* instr_switch: INSTR_SWITCH  */
#line 1369 "./asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6576 "asmparse.cpp"
    break;

  case 510: /* instr_r_head: instr_r '('  */
#line 1372 "./asmparse.y"
                                                             { (yyval.instr) = (yyvsp[-1].instr); bParsingByteArray = TRUE; }
#line 6582 "asmparse.cpp"
    break;

  case 511: /* instr: instr_none  */
#line 1376 "./asmparse.y"
                                                             { PASM->EmitOpcode((yyvsp[0].instr)); }
#line 6588 "asmparse.cpp"
    break;

  case 512: /* instr: instr_var int32  */
#line 1377 "./asmparse.y"
                                                             { PASM->EmitInstrVar((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 6594 "asmparse.cpp"
    break;

  case 513: /* instr: instr_var id  */
#line 1378 "./asmparse.y"
                                                             { PASM->EmitInstrVarByName((yyvsp[-1].instr), (yyvsp[0].string)); }
#line 6600 "asmparse.cpp"
    break;

  case 514: /* instr: instr_i int32  */
#line 1379 "./asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 6606 "asmparse.cpp"
    break;

  case 515: /* instr: instr_i8 int64  */
#line 1380 "./asmparse.y"
                                                             { PASM->EmitInstrI8((yyvsp[-1].instr), (yyvsp[0].int64)); }
#line 6612 "asmparse.cpp"
    break;

  case 516: /* instr: instr_r float64  */
#line 1381 "./asmparse.y"
                                                             { PASM->EmitInstrR((yyvsp[-1].instr), (yyvsp[0].float64)); delete ((yyvsp[0].float64));}
#line 6618 "asmparse.cpp"
    break;

  case 517: /* instr: instr_r int64  */
#line 1382 "./asmparse.y"
                                                             { double f = (double) (*(yyvsp[0].int64)); PASM->EmitInstrR((yyvsp[-1].instr), &f); }
#line 6624 "asmparse.cpp"
    break;

  case 518: /* instr: instr_r_head bytes ')'  */
#line 1383 "./asmparse.y"
                                                             { unsigned L = (yyvsp[-1].binstr)->length();
                                                               FAIL_UNLESS(L >= sizeof(float), ("%d hexbytes, must be at least %d\n",
                                                                           L,sizeof(float)));
                                                               if(L < sizeof(float)) {YYERROR; }
                                                               else {
                                                                   double f = (L >= sizeof(double)) ? *((double *)((yyvsp[-1].binstr)->ptr()))
                                                                                    : (double)(*(float *)((yyvsp[-1].binstr)->ptr()));
                                                                   PASM->EmitInstrR((yyvsp[-2].instr),&f); }
                                                               delete (yyvsp[-1].binstr); }
#line 6638 "asmparse.cpp"
    break;

  case 519: /* instr: instr_brtarget int32  */
#line 1392 "./asmparse.y"
                                                             { PASM->EmitInstrBrOffset((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 6644 "asmparse.cpp"
    break;

  case 520: /* instr: instr_brtarget id  */
#line 1393 "./asmparse.y"
                                                             { PASM->EmitInstrBrTarget((yyvsp[-1].instr), (yyvsp[0].string)); }
#line 6650 "asmparse.cpp"
    break;

  case 521: /* instr: instr_method methodRef  */
#line 1395 "./asmparse.y"
                                                             { PASM->SetMemberRefFixup((yyvsp[0].token),PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),(yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iCallConv = 0;
                                                             }
#line 6661 "asmparse.cpp"
    break;

  case 522: /* instr: instr_field type typeSpec DCOLON dottedName  */
#line 1402 "./asmparse.y"
                                                             { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef((yyvsp[-2].token), (yyvsp[0].string), (yyvsp[-3].binstr));
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-4].instr)));
                                                               PASM->EmitInstrI((yyvsp[-4].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6673 "asmparse.cpp"
    break;

  case 523: /* instr: instr_field type dottedName  */
#line 1410 "./asmparse.y"
                                                             { (yyvsp[-1].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef(mdTokenNil, (yyvsp[0].string), (yyvsp[-1].binstr));
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-2].instr)));
                                                               PASM->EmitInstrI((yyvsp[-2].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6685 "asmparse.cpp"
    break;

  case 524: /* instr: instr_field mdtoken  */
#line 1417 "./asmparse.y"
                                                             { mdToken mr = (yyvsp[0].token);
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6696 "asmparse.cpp"
    break;

  case 525: /* instr: instr_field TYPEDEF_F  */
#line 1423 "./asmparse.y"
                                                             { mdToken mr = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6707 "asmparse.cpp"
    break;

  case 526: /* instr: instr_field TYPEDEF_MR  */
#line 1429 "./asmparse.y"
                                                             { mdToken mr = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6718 "asmparse.cpp"
    break;

  case 527: /* instr: instr_type typeSpec  */
#line 1435 "./asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr), (yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6727 "asmparse.cpp"
    break;

  case 528: /* instr: instr_string compQstring  */
#line 1439 "./asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-1].instr), (yyvsp[0].binstr),TRUE); }
#line 6733 "asmparse.cpp"
    break;

  case 529: /* instr: instr_string ANSI_ '(' compQstring ')'  */
#line 1441 "./asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-4].instr), (yyvsp[-1].binstr),FALSE); }
#line 6739 "asmparse.cpp"
    break;

  case 530: /* instr: instr_string bytearrayhead bytes ')'  */
#line 1443 "./asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-3].instr), (yyvsp[-1].binstr),FALSE,TRUE); }
#line 6745 "asmparse.cpp"
    break;

  case 531: /* instr: instr_sig callConv type '(' sigArgs0 ')'  */
#line 1445 "./asmparse.y"
                                                             { PASM->EmitInstrSig((yyvsp[-5].instr), parser->MakeSig((yyvsp[-4].int32), (yyvsp[-3].binstr), (yyvsp[-1].binstr)));
                                                               PASM->ResetArgNameList();
                                                             }
#line 6753 "asmparse.cpp"
    break;

  case 532: /* instr: instr_tok ownerType  */
#line 1449 "./asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr),(yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iOpcodeLen = 0;
                                                             }
#line 6763 "asmparse.cpp"
    break;

  case 533: /* instr: instr_switch '(' labels ')'  */
#line 1454 "./asmparse.y"
                                                             { PASM->EmitInstrSwitch((yyvsp[-3].instr), (yyvsp[-1].labels)); }
#line 6769 "asmparse.cpp"
    break;

  case 534: /* labels: %empty  */
#line 1457 "./asmparse.y"
                                                              { (yyval.labels) = 0; }
#line 6775 "asmparse.cpp"
    break;

  case 535: /* labels: id ',' labels  */
#line 1458 "./asmparse.y"
                                                              { (yyval.labels) = new Labels((yyvsp[-2].string), (yyvsp[0].labels), TRUE); }
#line 6781 "asmparse.cpp"
    break;

  case 536: /* labels: int32 ',' labels  */
#line 1459 "./asmparse.y"
                                                              { (yyval.labels) = new Labels((char *)(UINT_PTR)(yyvsp[-2].int32), (yyvsp[0].labels), FALSE); }
#line 6787 "asmparse.cpp"
    break;

  case 537: /* labels: id  */
#line 1460 "./asmparse.y"
                                                              { (yyval.labels) = new Labels((yyvsp[0].string), NULL, TRUE); }
#line 6793 "asmparse.cpp"
    break;

  case 538: /* labels: int32  */
#line 1461 "./asmparse.y"
                                                              { (yyval.labels) = new Labels((char *)(UINT_PTR)(yyvsp[0].int32), NULL, FALSE); }
#line 6799 "asmparse.cpp"
    break;

  case 539: /* tyArgs0: %empty  */
#line 1465 "./asmparse.y"
                                                             { (yyval.binstr) = NULL; }
#line 6805 "asmparse.cpp"
    break;

  case 540: /* tyArgs0: '<' tyArgs1 '>'  */
#line 1466 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 6811 "asmparse.cpp"
    break;

  case 541: /* tyArgs1: %empty  */
#line 1469 "./asmparse.y"
                                                             { (yyval.binstr) = NULL; }
#line 6817 "asmparse.cpp"
    break;

  case 542: /* tyArgs1: tyArgs2  */
#line 1470 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6823 "asmparse.cpp"
    break;

  case 543: /* tyArgs2: type  */
#line 1473 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6829 "asmparse.cpp"
    break;

  case 544: /* tyArgs2: tyArgs2 ',' type  */
#line 1474 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 6835 "asmparse.cpp"
    break;

  case 545: /* sigArgs0: %empty  */
#line 1478 "./asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6841 "asmparse.cpp"
    break;

  case 546: /* sigArgs0: sigArgs1  */
#line 1479 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr);}
#line 6847 "asmparse.cpp"
    break;

  case 547: /* sigArgs1: sigArg  */
#line 1482 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6853 "asmparse.cpp"
    break;

  case 548: /* sigArgs1: sigArgs1 ',' sigArg  */
#line 1483 "./asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 6859 "asmparse.cpp"
    break;

  case 549: /* sigArg: ELLIPSIS  */
#line 1486 "./asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_SENTINEL); }
#line 6865 "asmparse.cpp"
    break;

  case 550: /* sigArg: paramAttr type marshalClause  */
#line 1487 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[-1].binstr)); PASM->addArgName(NULL, (yyvsp[-1].binstr), (yyvsp[0].binstr), (yyvsp[-2].int32)); }
#line 6871 "asmparse.cpp"
    break;

  case 551: /* sigArg: paramAttr type marshalClause id  */
#line 1488 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[-2].binstr)); PASM->addArgName((yyvsp[0].string), (yyvsp[-2].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32));}
#line 6877 "asmparse.cpp"
    break;

  case 552: /* className: '[' dottedName ']' slashedName  */
#line 1492 "./asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(PASM->GetAsmRef((yyvsp[-2].string)), (yyvsp[0].string), NULL); delete[] (yyvsp[-2].string);}
#line 6883 "asmparse.cpp"
    break;

  case 553: /* className: '[' mdtoken ']' slashedName  */
#line 1493 "./asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef((yyvsp[-2].token), (yyvsp[0].string), NULL); }
#line 6889 "asmparse.cpp"
    break;

  case 554: /* className: '[' '*' ']' slashedName  */
#line 1494 "./asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(mdTokenNil, (yyvsp[0].string), NULL); }
#line 6895 "asmparse.cpp"
    break;

  case 555: /* className: '[' _MODULE dottedName ']' slashedName  */
#line 1495 "./asmparse.y"
                                                                   { (yyval.token) = PASM->ResolveClassRef(PASM->GetModRef((yyvsp[-2].string)),(yyvsp[0].string), NULL); delete[] (yyvsp[-2].string);}
#line 6901 "asmparse.cpp"
    break;

  case 556: /* className: slashedName  */
#line 1496 "./asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(1,(yyvsp[0].string),NULL); }
#line 6907 "asmparse.cpp"
    break;

  case 557: /* className: mdtoken  */
#line 1497 "./asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].token); }
#line 6913 "asmparse.cpp"
    break;

  case 558: /* className: TYPEDEF_T  */
#line 1498 "./asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 6919 "asmparse.cpp"
    break;

  case 559: /* className: _THIS  */
#line 1499 "./asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) (yyval.token) = PASM->m_pCurClass->m_cl;
                                                                else { (yyval.token) = 0; PASM->report->error(".this outside class scope\n"); }
                                                              }
#line 6927 "asmparse.cpp"
    break;

  case 560: /* className: _BASE  */
#line 1502 "./asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) {
                                                                  (yyval.token) = PASM->m_pCurClass->m_crExtends;
                                                                  if(RidFromToken((yyval.token)) == 0)
                                                                    PASM->report->error(".base undefined\n");
                                                                } else { (yyval.token) = 0; PASM->report->error(".base outside class scope\n"); }
                                                              }
#line 6938 "asmparse.cpp"
    break;

  case 561: /* className: _NESTER  */
#line 1508 "./asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) {
                                                                  if(PASM->m_pCurClass->m_pEncloser != NULL) (yyval.token) = PASM->m_pCurClass->m_pEncloser->m_cl;
                                                                  else { (yyval.token) = 0; PASM->report->error(".nester undefined\n"); }
                                                                } else { (yyval.token) = 0; PASM->report->error(".nester outside class scope\n"); }
                                                              }
#line 6948 "asmparse.cpp"
    break;

  case 562: /* slashedName: dottedName  */
#line 1515 "./asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 6954 "asmparse.cpp"
    break;

  case 563: /* slashedName: slashedName '/' dottedName  */
#line 1516 "./asmparse.y"
                                                              { (yyval.string) = newStringWDel((yyvsp[-2].string), NESTING_SEP, (yyvsp[0].string)); }
#line 6960 "asmparse.cpp"
    break;

  case 564: /* typeSpec: className  */
#line 1519 "./asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].token);}
#line 6966 "asmparse.cpp"
    break;

  case 565: /* typeSpec: '[' dottedName ']'  */
#line 1520 "./asmparse.y"
                                                              { (yyval.token) = PASM->GetAsmRef((yyvsp[-1].string)); delete[] (yyvsp[-1].string);}
#line 6972 "asmparse.cpp"
    break;

  case 566: /* typeSpec: '[' _MODULE dottedName ']'  */
#line 1521 "./asmparse.y"
                                                              { (yyval.token) = PASM->GetModRef((yyvsp[-1].string)); delete[] (yyvsp[-1].string);}
#line 6978 "asmparse.cpp"
    break;

  case 567: /* typeSpec: type  */
#line 1522 "./asmparse.y"
                                                              { (yyval.token) = PASM->ResolveTypeSpec((yyvsp[0].binstr)); }
#line 6984 "asmparse.cpp"
    break;

  case 568: /* nativeType: %empty  */
#line 1526 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); }
#line 6990 "asmparse.cpp"
    break;

  case 569: /* nativeType: CUSTOM_ '(' compQstring ',' compQstring ',' compQstring ',' compQstring ')'  */
#line 1528 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt((yyval.binstr),(yyvsp[-7].binstr)->length()); (yyval.binstr)->append((yyvsp[-7].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-5].binstr)->length()); (yyval.binstr)->append((yyvsp[-5].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].binstr)->length()); (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].binstr)->length()); (yyval.binstr)->append((yyvsp[-1].binstr));
                                                                PASM->report->warn("Deprecated 4-string form of custom marshaler, first two strings ignored\n");}
#line 7001 "asmparse.cpp"
    break;

  case 570: /* nativeType: CUSTOM_ '(' compQstring ',' compQstring ')'  */
#line 1535 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].binstr)->length()); (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].binstr)->length()); (yyval.binstr)->append((yyvsp[-1].binstr)); }
#line 7011 "asmparse.cpp"
    break;

  case 571: /* nativeType: FIXED_ SYSSTRING_ '[' int32 ']'  */
#line 1540 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FIXEDSYSSTRING);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32)); }
#line 7018 "asmparse.cpp"
    break;

  case 572: /* nativeType: FIXED_ ARRAY_ '[' int32 ']' nativeType  */
#line 1543 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FIXEDARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-2].int32)); (yyval.binstr)->append((yyvsp[0].binstr)); }
#line 7025 "asmparse.cpp"
    break;

  case 573: /* nativeType: VARIANT_  */
#line 1545 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VARIANT);
                                                                PASM->report->warn("Deprecated native type 'variant'\n"); }
#line 7032 "asmparse.cpp"
    break;

  case 574: /* nativeType: CURRENCY_  */
#line 1547 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CURRENCY); }
#line 7038 "asmparse.cpp"
    break;

  case 575: /* nativeType: SYSCHAR_  */
#line 1548 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SYSCHAR);
                                                                PASM->report->warn("Deprecated native type 'syschar'\n"); }
#line 7045 "asmparse.cpp"
    break;

  case 576: /* nativeType: VOID_  */
#line 1550 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VOID);
                                                                PASM->report->warn("Deprecated native type 'void'\n"); }
#line 7052 "asmparse.cpp"
    break;

  case 577: /* nativeType: BOOL_  */
#line 1552 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BOOLEAN); }
#line 7058 "asmparse.cpp"
    break;

  case 578: /* nativeType: INT8_  */
#line 1553 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I1); }
#line 7064 "asmparse.cpp"
    break;

  case 579: /* nativeType: INT16_  */
#line 1554 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I2); }
#line 7070 "asmparse.cpp"
    break;

  case 580: /* nativeType: INT32_  */
#line 1555 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I4); }
#line 7076 "asmparse.cpp"
    break;

  case 581: /* nativeType: INT64_  */
#line 1556 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I8); }
#line 7082 "asmparse.cpp"
    break;

  case 582: /* nativeType: FLOAT32_  */
#line 1557 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_R4); }
#line 7088 "asmparse.cpp"
    break;

  case 583: /* nativeType: FLOAT64_  */
#line 1558 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_R8); }
#line 7094 "asmparse.cpp"
    break;

  case 584: /* nativeType: ERROR_  */
#line 1559 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ERROR); }
#line 7100 "asmparse.cpp"
    break;

  case 585: /* nativeType: UNSIGNED_ INT8_  */
#line 1560 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U1); }
#line 7106 "asmparse.cpp"
    break;

  case 586: /* nativeType: UNSIGNED_ INT16_  */
#line 1561 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U2); }
#line 7112 "asmparse.cpp"
    break;

  case 587: /* nativeType: UNSIGNED_ INT32_  */
#line 1562 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U4); }
#line 7118 "asmparse.cpp"
    break;

  case 588: /* nativeType: UNSIGNED_ INT64_  */
#line 1563 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U8); }
#line 7124 "asmparse.cpp"
    break;

  case 589: /* nativeType: UINT8_  */
#line 1564 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U1); }
#line 7130 "asmparse.cpp"
    break;

  case 590: /* nativeType: UINT16_  */
#line 1565 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U2); }
#line 7136 "asmparse.cpp"
    break;

  case 591: /* nativeType: UINT32_  */
#line 1566 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U4); }
#line 7142 "asmparse.cpp"
    break;

  case 592: /* nativeType: UINT64_  */
#line 1567 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U8); }
#line 7148 "asmparse.cpp"
    break;

  case 593: /* nativeType: nativeType '*'  */
#line 1568 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(NATIVE_TYPE_PTR);
                                                                PASM->report->warn("Deprecated native type '*'\n"); }
#line 7155 "asmparse.cpp"
    break;

  case 594: /* nativeType: nativeType '[' ']'  */
#line 1570 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY); }
#line 7162 "asmparse.cpp"
    break;

  case 595: /* nativeType: nativeType '[' int32 ']'  */
#line 1572 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-3].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32));
                                                                corEmitInt((yyval.binstr),0); }
#line 7172 "asmparse.cpp"
    break;

  case 596: /* nativeType: nativeType '[' int32 '+' int32 ']'  */
#line 1577 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-5].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32));
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].int32));
                                                                corEmitInt((yyval.binstr),ntaSizeParamIndexSpecified); }
#line 7182 "asmparse.cpp"
    break;

  case 597: /* nativeType: nativeType '[' '+' int32 ']'  */
#line 1582 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-4].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32)); }
#line 7190 "asmparse.cpp"
    break;

  case 598: /* nativeType: DECIMAL_  */
#line 1585 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_DECIMAL);
                                                                PASM->report->warn("Deprecated native type 'decimal'\n"); }
#line 7197 "asmparse.cpp"
    break;

  case 599: /* nativeType: DATE_  */
#line 1587 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_DATE);
                                                                PASM->report->warn("Deprecated native type 'date'\n"); }
#line 7204 "asmparse.cpp"
    break;

  case 600: /* nativeType: BSTR_  */
#line 1589 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BSTR); }
#line 7210 "asmparse.cpp"
    break;

  case 601: /* nativeType: LPSTR_  */
#line 1590 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPSTR); }
#line 7216 "asmparse.cpp"
    break;

  case 602: /* nativeType: LPWSTR_  */
#line 1591 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPWSTR); }
#line 7222 "asmparse.cpp"
    break;

  case 603: /* nativeType: LPTSTR_  */
#line 1592 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPTSTR); }
#line 7228 "asmparse.cpp"
    break;

  case 604: /* nativeType: OBJECTREF_  */
#line 1593 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_OBJECTREF);
                                                                PASM->report->warn("Deprecated native type 'objectref'\n"); }
#line 7235 "asmparse.cpp"
    break;

  case 605: /* nativeType: IUNKNOWN_ iidParamIndex  */
#line 1595 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_IUNKNOWN);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7242 "asmparse.cpp"
    break;

  case 606: /* nativeType: IDISPATCH_ iidParamIndex  */
#line 1597 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_IDISPATCH);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7249 "asmparse.cpp"
    break;

  case 607: /* nativeType: STRUCT_  */
#line 1599 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_STRUCT); }
#line 7255 "asmparse.cpp"
    break;

  case 608: /* nativeType: INTERFACE_ iidParamIndex  */
#line 1600 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_INTF);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7262 "asmparse.cpp"
    break;

  case 609: /* nativeType: SAFEARRAY_ variantType  */
#line 1602 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[0].int32));
                                                                corEmitInt((yyval.binstr),0);}
#line 7270 "asmparse.cpp"
    break;

  case 610: /* nativeType: SAFEARRAY_ variantType ',' compQstring  */
#line 1605 "./asmparse.y"
                                                                 { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-2].int32));
                                                                corEmitInt((yyval.binstr),(yyvsp[0].binstr)->length()); (yyval.binstr)->append((yyvsp[0].binstr)); }
#line 7278 "asmparse.cpp"
    break;

  case 611: /* nativeType: INT_  */
#line 1609 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_INT); }
#line 7284 "asmparse.cpp"
    break;

  case 612: /* nativeType: UNSIGNED_ INT_  */
#line 1610 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_UINT); }
#line 7290 "asmparse.cpp"
    break;

  case 613: /* nativeType: UINT_  */
#line 1611 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_UINT); }
#line 7296 "asmparse.cpp"
    break;

  case 614: /* nativeType: NESTED_ STRUCT_  */
#line 1612 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_NESTEDSTRUCT);
                                                                PASM->report->warn("Deprecated native type 'nested struct'\n"); }
#line 7303 "asmparse.cpp"
    break;

  case 615: /* nativeType: BYVALSTR_  */
#line 1614 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BYVALSTR); }
#line 7309 "asmparse.cpp"
    break;

  case 616: /* nativeType: ANSI_ BSTR_  */
#line 1615 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ANSIBSTR); }
#line 7315 "asmparse.cpp"
    break;

  case 617: /* nativeType: TBSTR_  */
#line 1616 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_TBSTR); }
#line 7321 "asmparse.cpp"
    break;

  case 618: /* nativeType: VARIANT_ BOOL_  */
#line 1617 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VARIANTBOOL); }
#line 7327 "asmparse.cpp"
    break;

  case 619: /* nativeType: METHOD_  */
#line 1618 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FUNC); }
#line 7333 "asmparse.cpp"
    break;

  case 620: /* nativeType: AS_ ANY_  */
#line 1619 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ASANY); }
#line 7339 "asmparse.cpp"
    break;

  case 621: /* nativeType: LPSTRUCT_  */
#line 1620 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPSTRUCT); }
#line 7345 "asmparse.cpp"
    break;

  case 622: /* nativeType: TYPEDEF_TS  */
#line 1621 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[0].tdd)->m_pbsTypeSpec); }
#line 7351 "asmparse.cpp"
    break;

  case 623: /* iidParamIndex: %empty  */
#line 1624 "./asmparse.y"
                                                              { (yyval.int32) = -1; }
#line 7357 "asmparse.cpp"
    break;

  case 624: /* iidParamIndex: '(' IIDPARAM_ '=' int32 ')'  */
#line 1625 "./asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32); }
#line 7363 "asmparse.cpp"
    break;

  case 625: /* variantType: %empty  */
#line 1628 "./asmparse.y"
                                                              { (yyval.int32) = VT_EMPTY; }
#line 7369 "asmparse.cpp"
    break;

  case 626: /* variantType: NULL_  */
#line 1629 "./asmparse.y"
                                                              { (yyval.int32) = VT_NULL; }
#line 7375 "asmparse.cpp"
    break;

  case 627: /* variantType: VARIANT_  */
#line 1630 "./asmparse.y"
                                                              { (yyval.int32) = VT_VARIANT; }
#line 7381 "asmparse.cpp"
    break;

  case 628: /* variantType: CURRENCY_  */
#line 1631 "./asmparse.y"
                                                              { (yyval.int32) = VT_CY; }
#line 7387 "asmparse.cpp"
    break;

  case 629: /* variantType: VOID_  */
#line 1632 "./asmparse.y"
                                                              { (yyval.int32) = VT_VOID; }
#line 7393 "asmparse.cpp"
    break;

  case 630: /* variantType: BOOL_  */
#line 1633 "./asmparse.y"
                                                              { (yyval.int32) = VT_BOOL; }
#line 7399 "asmparse.cpp"
    break;

  case 631: /* variantType: INT8_  */
#line 1634 "./asmparse.y"
                                                              { (yyval.int32) = VT_I1; }
#line 7405 "asmparse.cpp"
    break;

  case 632: /* variantType: INT16_  */
#line 1635 "./asmparse.y"
                                                              { (yyval.int32) = VT_I2; }
#line 7411 "asmparse.cpp"
    break;

  case 633: /* variantType: INT32_  */
#line 1636 "./asmparse.y"
                                                              { (yyval.int32) = VT_I4; }
#line 7417 "asmparse.cpp"
    break;

  case 634: /* variantType: INT64_  */
#line 1637 "./asmparse.y"
                                                              { (yyval.int32) = VT_I8; }
#line 7423 "asmparse.cpp"
    break;

  case 635: /* variantType: FLOAT32_  */
#line 1638 "./asmparse.y"
                                                              { (yyval.int32) = VT_R4; }
#line 7429 "asmparse.cpp"
    break;

  case 636: /* variantType: FLOAT64_  */
#line 1639 "./asmparse.y"
                                                              { (yyval.int32) = VT_R8; }
#line 7435 "asmparse.cpp"
    break;

  case 637: /* variantType: UNSIGNED_ INT8_  */
#line 1640 "./asmparse.y"
                                                              { (yyval.int32) = VT_UI1; }
#line 7441 "asmparse.cpp"
    break;

  case 638: /* variantType: UNSIGNED_ INT16_  */
#line 1641 "./asmparse.y"
                                                              { (yyval.int32) = VT_UI2; }
#line 7447 "asmparse.cpp"
    break;

  case 639: /* variantType: UNSIGNED_ INT32_  */
#line 1642 "./asmparse.y"
                                                              { (yyval.int32) = VT_UI4; }
#line 7453 "asmparse.cpp"
    break;

  case 640: /* variantType: UNSIGNED_ INT64_  */
#line 1643 "./asmparse.y"
                                                              { (yyval.int32) = VT_UI8; }
#line 7459 "asmparse.cpp"
    break;

  case 641: /* variantType: UINT8_  */
#line 1644 "./asmparse.y"
                                                              { (yyval.int32) = VT_UI1; }
#line 7465 "asmparse.cpp"
    break;

  case 642: /* variantType: UINT16_  */
#line 1645 "./asmparse.y"
                                                              { (yyval.int32) = VT_UI2; }
#line 7471 "asmparse.cpp"
    break;

  case 643: /* variantType: UINT32_  */
#line 1646 "./asmparse.y"
                                                              { (yyval.int32) = VT_UI4; }
#line 7477 "asmparse.cpp"
    break;

  case 644: /* variantType: UINT64_  */
#line 1647 "./asmparse.y"
                                                              { (yyval.int32) = VT_UI8; }
#line 7483 "asmparse.cpp"
    break;

  case 645: /* variantType: '*'  */
#line 1648 "./asmparse.y"
                                                              { (yyval.int32) = VT_PTR; }
#line 7489 "asmparse.cpp"
    break;

  case 646: /* variantType: variantType '[' ']'  */
#line 1649 "./asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-2].int32) | VT_ARRAY; }
#line 7495 "asmparse.cpp"
    break;

  case 647: /* variantType: variantType VECTOR_  */
#line 1650 "./asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32) | VT_VECTOR; }
#line 7501 "asmparse.cpp"
    break;

  case 648: /* variantType: variantType '&'  */
#line 1651 "./asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32) | VT_BYREF; }
#line 7507 "asmparse.cpp"
    break;

  case 649: /* variantType: DECIMAL_  */
#line 1652 "./asmparse.y"
                                                              { (yyval.int32) = VT_DECIMAL; }
#line 7513 "asmparse.cpp"
    break;

  case 650: /* variantType: DATE_  */
#line 1653 "./asmparse.y"
                                                              { (yyval.int32) = VT_DATE; }
#line 7519 "asmparse.cpp"
    break;

  case 651: /* variantType: BSTR_  */
#line 1654 "./asmparse.y"
                                                              { (yyval.int32) = VT_BSTR; }
#line 7525 "asmparse.cpp"
    break;

  case 652: /* variantType: LPSTR_  */
#line 1655 "./asmparse.y"
                                                              { (yyval.int32) = VT_LPSTR; }
#line 7531 "asmparse.cpp"
    break;

  case 653: /* variantType: LPWSTR_  */
#line 1656 "./asmparse.y"
                                                              { (yyval.int32) = VT_LPWSTR; }
#line 7537 "asmparse.cpp"
    break;

  case 654: /* variantType: IUNKNOWN_  */
#line 1657 "./asmparse.y"
                                                              { (yyval.int32) = VT_UNKNOWN; }
#line 7543 "asmparse.cpp"
    break;

  case 655: /* variantType: IDISPATCH_  */
#line 1658 "./asmparse.y"
                                                              { (yyval.int32) = VT_DISPATCH; }
#line 7549 "asmparse.cpp"
    break;

  case 656: /* variantType: SAFEARRAY_  */
#line 1659 "./asmparse.y"
                                                              { (yyval.int32) = VT_SAFEARRAY; }
#line 7555 "asmparse.cpp"
    break;

  case 657: /* variantType: INT_  */
#line 1660 "./asmparse.y"
                                                              { (yyval.int32) = VT_INT; }
#line 7561 "asmparse.cpp"
    break;

  case 658: /* variantType: UNSIGNED_ INT_  */
#line 1661 "./asmparse.y"
                                                              { (yyval.int32) = VT_UINT; }
#line 7567 "asmparse.cpp"
    break;

  case 659: /* variantType: UINT_  */
#line 1662 "./asmparse.y"
                                                              { (yyval.int32) = VT_UINT; }
#line 7573 "asmparse.cpp"
    break;

  case 660: /* variantType: ERROR_  */
#line 1663 "./asmparse.y"
                                                              { (yyval.int32) = VT_ERROR; }
#line 7579 "asmparse.cpp"
    break;

  case 661: /* variantType: HRESULT_  */
#line 1664 "./asmparse.y"
                                                              { (yyval.int32) = VT_HRESULT; }
#line 7585 "asmparse.cpp"
    break;

  case 662: /* variantType: CARRAY_  */
#line 1665 "./asmparse.y"
                                                              { (yyval.int32) = VT_CARRAY; }
#line 7591 "asmparse.cpp"
    break;

  case 663: /* variantType: USERDEFINED_  */
#line 1666 "./asmparse.y"
                                                              { (yyval.int32) = VT_USERDEFINED; }
#line 7597 "asmparse.cpp"
    break;

  case 664: /* variantType: RECORD_  */
#line 1667 "./asmparse.y"
                                                              { (yyval.int32) = VT_RECORD; }
#line 7603 "asmparse.cpp"
    break;

  case 665: /* variantType: FILETIME_  */
#line 1668 "./asmparse.y"
                                                              { (yyval.int32) = VT_FILETIME; }
#line 7609 "asmparse.cpp"
    break;

  case 666: /* variantType: BLOB_  */
#line 1669 "./asmparse.y"
                                                              { (yyval.int32) = VT_BLOB; }
#line 7615 "asmparse.cpp"
    break;

  case 667: /* variantType: STREAM_  */
#line 1670 "./asmparse.y"
                                                              { (yyval.int32) = VT_STREAM; }
#line 7621 "asmparse.cpp"
    break;

  case 668: /* variantType: STORAGE_  */
#line 1671 "./asmparse.y"
                                                              { (yyval.int32) = VT_STORAGE; }
#line 7627 "asmparse.cpp"
    break;

  case 669: /* variantType: STREAMED_OBJECT_  */
#line 1672 "./asmparse.y"
                                                              { (yyval.int32) = VT_STREAMED_OBJECT; }
#line 7633 "asmparse.cpp"
    break;

  case 670: /* variantType: STORED_OBJECT_  */
#line 1673 "./asmparse.y"
                                                              { (yyval.int32) = VT_STORED_OBJECT; }
#line 7639 "asmparse.cpp"
    break;

  case 671: /* variantType: BLOB_OBJECT_  */
#line 1674 "./asmparse.y"
                                                              { (yyval.int32) = VT_BLOB_OBJECT; }
#line 7645 "asmparse.cpp"
    break;

  case 672: /* variantType: CF_  */
#line 1675 "./asmparse.y"
                                                              { (yyval.int32) = VT_CF; }
#line 7651 "asmparse.cpp"
    break;

  case 673: /* variantType: CLSID_  */
#line 1676 "./asmparse.y"
                                                              { (yyval.int32) = VT_CLSID; }
#line 7657 "asmparse.cpp"
    break;

  case 674: /* type: CLASS_ className  */
#line 1680 "./asmparse.y"
                                                              { if((yyvsp[0].token) == PASM->m_tkSysString)
                                                                {     (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); }
                                                                else if((yyvsp[0].token) == PASM->m_tkSysObject)
                                                                {     (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_OBJECT); }
                                                                else
                                                                 (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CLASS, (yyvsp[0].token)); }
#line 7668 "asmparse.cpp"
    break;

  case 675: /* type: OBJECT_  */
#line 1686 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_OBJECT); }
#line 7674 "asmparse.cpp"
    break;

  case 676: /* type: VALUE_ CLASS_ className  */
#line 1687 "./asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, (yyvsp[0].token)); }
#line 7680 "asmparse.cpp"
    break;

  case 677: /* type: VALUETYPE_ className  */
#line 1688 "./asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, (yyvsp[0].token)); }
#line 7686 "asmparse.cpp"
    break;

  case 678: /* type: CONST_ fieldInit  */
#line 1689 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_CTARG); }
#line 7692 "asmparse.cpp"
    break;

  case 679: /* type: type '[' ']'  */
#line 1690 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 7698 "asmparse.cpp"
    break;

  case 680: /* type: type '[' bounds1 ']'  */
#line 1691 "./asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeArray(ELEMENT_TYPE_ARRAY, (yyvsp[-3].binstr), (yyvsp[-1].binstr)); }
#line 7704 "asmparse.cpp"
    break;

  case 681: /* type: type '&'  */
#line 1692 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_BYREF); }
#line 7710 "asmparse.cpp"
    break;

  case 682: /* type: type '*'  */
#line 1693 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_PTR); }
#line 7716 "asmparse.cpp"
    break;

  case 683: /* type: type PINNED_  */
#line 1694 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_PINNED); }
#line 7722 "asmparse.cpp"
    break;

  case 684: /* type: type MODREQ_ '(' typeSpec ')'  */
#line 1695 "./asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_REQD, (yyvsp[-1].token));
                                                                (yyval.binstr)->append((yyvsp[-4].binstr)); }
#line 7729 "asmparse.cpp"
    break;

  case 685: /* type: type MODOPT_ '(' typeSpec ')'  */
#line 1697 "./asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_OPT, (yyvsp[-1].token));
                                                                (yyval.binstr)->append((yyvsp[-4].binstr)); }
#line 7736 "asmparse.cpp"
    break;

  case 686: /* type: methodSpec callConv type '*' '(' sigArgs0 ')'  */
#line 1700 "./asmparse.y"
                                                              { (yyval.binstr) = parser->MakeSig((yyvsp[-5].int32), (yyvsp[-4].binstr), (yyvsp[-1].binstr));
                                                                (yyval.binstr)->insertInt8(ELEMENT_TYPE_FNPTR);
                                                                PASM->delArgNameList(PASM->m_firstArgName);
                                                                PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                                PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                              }
#line 7747 "asmparse.cpp"
    break;

  case 687: /* type: type '<' tyArgs1 '>'  */
#line 1706 "./asmparse.y"
                                                              { if((yyvsp[-1].binstr) == NULL) (yyval.binstr) = (yyvsp[-3].binstr);
                                                                else {
                                                                  (yyval.binstr) = new BinStr();
                                                                  (yyval.binstr)->appendInt8(ELEMENT_TYPE_GENERICINST);
                                                                  (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                  corEmitInt((yyval.binstr), corCountArgs((yyvsp[-1].binstr)));
                                                                  (yyval.binstr)->append((yyvsp[-1].binstr)); delete (yyvsp[-3].binstr); delete (yyvsp[-1].binstr); }}
#line 7759 "asmparse.cpp"
    break;

  case 688: /* type: '!' '!' int32  */
#line 1713 "./asmparse.y"
                                                              { //if(PASM->m_pCurMethod)  {
                                                                //  if(($3 < 0)||((DWORD)$3 >= PASM->m_pCurMethod->m_NumTyPars))
                                                                //    PASM->report->error("Invalid method type parameter '%d'\n",$3);
                                                                  (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_MVAR); corEmitInt((yyval.binstr), (yyvsp[0].int32));
                                                                //} else PASM->report->error("Method type parameter '%d' outside method scope\n",$3);
                                                              }
#line 7770 "asmparse.cpp"
    break;

  case 689: /* type: '!' int32  */
#line 1719 "./asmparse.y"
                                                              { //if(PASM->m_pCurClass)  {
                                                                //  if(($2 < 0)||((DWORD)$2 >= PASM->m_pCurClass->m_NumTyPars))
                                                                //    PASM->report->error("Invalid type parameter '%d'\n",$2);
                                                                  (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_VAR); corEmitInt((yyval.binstr), (yyvsp[0].int32));
                                                                //} else PASM->report->error("Type parameter '%d' outside class scope\n",$2);
                                                              }
#line 7781 "asmparse.cpp"
    break;

  case 690: /* type: '!' '!' dottedName  */
#line 1725 "./asmparse.y"
                                                              { int eltype = ELEMENT_TYPE_MVAR;
                                                                int n=-1;
                                                                if(PASM->m_pCurMethod) n = PASM->m_pCurMethod->FindTyPar((yyvsp[0].string));
                                                                else {
                                                                  if(PASM->m_TyParList) n = PASM->m_TyParList->IndexOf((yyvsp[0].string));
                                                                  if(n == -1)
                                                                  { n = TyParFixupList.COUNT();
                                                                    TyParFixupList.PUSH((yyvsp[0].string));
                                                                    eltype = ELEMENT_TYPE_MVARFIXUP;
                                                                  }
                                                                }
                                                                if(n == -1) { PASM->report->error("Invalid method type parameter '%s'\n",(yyvsp[0].string));
                                                                n = 0x1FFFFFFF; }
                                                                (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(eltype); corEmitInt((yyval.binstr),n);
                                                              }
#line 7801 "asmparse.cpp"
    break;

  case 691: /* type: '!' dottedName  */
#line 1740 "./asmparse.y"
                                                              { int eltype = ELEMENT_TYPE_VAR;
                                                                int n=-1;
                                                                if(PASM->m_pCurClass && !newclass) n = PASM->m_pCurClass->FindTyPar((yyvsp[0].string));
                                                                else {
                                                                  if(PASM->m_TyParList) n = PASM->m_TyParList->IndexOf((yyvsp[0].string));
                                                                  if(n == -1)
                                                                  { n = TyParFixupList.COUNT();
                                                                    TyParFixupList.PUSH((yyvsp[0].string));
                                                                    eltype = ELEMENT_TYPE_VARFIXUP;
                                                                  }
                                                                }
                                                                if(n == -1) { PASM->report->error("Invalid type parameter '%s'\n",(yyvsp[0].string));
                                                                n = 0x1FFFFFFF; }
                                                                (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(eltype); corEmitInt((yyval.binstr),n);
                                                              }
#line 7821 "asmparse.cpp"
    break;

  case 692: /* type: TYPEDREF_  */
#line 1755 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_TYPEDBYREF); }
#line 7827 "asmparse.cpp"
    break;

  case 693: /* type: VOID_  */
#line 1756 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_VOID); }
#line 7833 "asmparse.cpp"
    break;

  case 694: /* type: NATIVE_ INT_  */
#line 1757 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I); }
#line 7839 "asmparse.cpp"
    break;

  case 695: /* type: NATIVE_ UNSIGNED_ INT_  */
#line 1758 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U); }
#line 7845 "asmparse.cpp"
    break;

  case 696: /* type: NATIVE_ UINT_  */
#line 1759 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U); }
#line 7851 "asmparse.cpp"
    break;

  case 697: /* type: simpleType  */
#line 1760 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7857 "asmparse.cpp"
    break;

  case 698: /* type: ELLIPSIS type  */
#line 1761 "./asmparse.y"
                                                               { (yyval.binstr) = (yyvsp[0].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SENTINEL); }
#line 7863 "asmparse.cpp"
    break;

  case 699: /* simpleType: CHAR_  */
#line 1764 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CHAR); }
#line 7869 "asmparse.cpp"
    break;

  case 700: /* simpleType: STRING_  */
#line 1765 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); }
#line 7875 "asmparse.cpp"
    break;

  case 701: /* simpleType: BOOL_  */
#line 1766 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_BOOLEAN); }
#line 7881 "asmparse.cpp"
    break;

  case 702: /* simpleType: INT8_  */
#line 1767 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I1); }
#line 7887 "asmparse.cpp"
    break;

  case 703: /* simpleType: INT16_  */
#line 1768 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I2); }
#line 7893 "asmparse.cpp"
    break;

  case 704: /* simpleType: INT32_  */
#line 1769 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I4); }
#line 7899 "asmparse.cpp"
    break;

  case 705: /* simpleType: INT64_  */
#line 1770 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I8); }
#line 7905 "asmparse.cpp"
    break;

  case 706: /* simpleType: FLOAT32_  */
#line 1771 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4); }
#line 7911 "asmparse.cpp"
    break;

  case 707: /* simpleType: FLOAT64_  */
#line 1772 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8); }
#line 7917 "asmparse.cpp"
    break;

  case 708: /* simpleType: UNSIGNED_ INT8_  */
#line 1773 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1); }
#line 7923 "asmparse.cpp"
    break;

  case 709: /* simpleType: UNSIGNED_ INT16_  */
#line 1774 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2); }
#line 7929 "asmparse.cpp"
    break;

  case 710: /* simpleType: UNSIGNED_ INT32_  */
#line 1775 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4); }
#line 7935 "asmparse.cpp"
    break;

  case 711: /* simpleType: UNSIGNED_ INT64_  */
#line 1776 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8); }
#line 7941 "asmparse.cpp"
    break;

  case 712: /* simpleType: UINT8_  */
#line 1777 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1); }
#line 7947 "asmparse.cpp"
    break;

  case 713: /* simpleType: UINT16_  */
#line 1778 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2); }
#line 7953 "asmparse.cpp"
    break;

  case 714: /* simpleType: UINT32_  */
#line 1779 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4); }
#line 7959 "asmparse.cpp"
    break;

  case 715: /* simpleType: UINT64_  */
#line 1780 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8); }
#line 7965 "asmparse.cpp"
    break;

  case 716: /* simpleType: TYPEDEF_TS  */
#line 1781 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[0].tdd)->m_pbsTypeSpec); }
#line 7971 "asmparse.cpp"
    break;

  case 717: /* bounds1: bound  */
#line 1784 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7977 "asmparse.cpp"
    break;

  case 718: /* bounds1: bounds1 ',' bound  */
#line 1785 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyvsp[-2].binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 7983 "asmparse.cpp"
    break;

  case 719: /* bound: %empty  */
#line 1788 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0x7FFFFFFF); (yyval.binstr)->appendInt32(0x7FFFFFFF);  }
#line 7989 "asmparse.cpp"
    break;

  case 720: /* bound: ELLIPSIS  */
#line 1789 "./asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0x7FFFFFFF); (yyval.binstr)->appendInt32(0x7FFFFFFF);  }
#line 7995 "asmparse.cpp"
    break;

  case 721: /* bound: int32  */
#line 1790 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0); (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 8001 "asmparse.cpp"
    break;

  case 722: /* bound: int32 ELLIPSIS int32  */
#line 1791 "./asmparse.y"
                                                               { FAIL_UNLESS((yyvsp[-2].int32) <= (yyvsp[0].int32), ("lower bound %d must be <= upper bound %d\n", (yyvsp[-2].int32), (yyvsp[0].int32)));
                                                                if ((yyvsp[-2].int32) > (yyvsp[0].int32)) { YYERROR; };
                                                                (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[-2].int32)); (yyval.binstr)->appendInt32((yyvsp[0].int32)-(yyvsp[-2].int32)+1); }
#line 8009 "asmparse.cpp"
    break;

  case 723: /* bound: int32 ELLIPSIS  */
#line 1794 "./asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[-1].int32)); (yyval.binstr)->appendInt32(0x7FFFFFFF); }
#line 8015 "asmparse.cpp"
    break;

  case 724: /* secDecl: _PERMISSION secAction typeSpec '(' nameValPairs ')'  */
#line 1799 "./asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-4].secAct), (yyvsp[-3].token), (yyvsp[-1].pair)); }
#line 8021 "asmparse.cpp"
    break;

  case 725: /* secDecl: _PERMISSION secAction typeSpec '=' '{' customBlobDescr '}'  */
#line 1801 "./asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-5].secAct), (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 8027 "asmparse.cpp"
    break;

  case 726: /* secDecl: _PERMISSION secAction typeSpec  */
#line 1802 "./asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-1].secAct), (yyvsp[0].token), (NVPair *)NULL); }
#line 8033 "asmparse.cpp"
    break;

  case 727: /* secDecl: psetHead bytes ')'  */
#line 1803 "./asmparse.y"
                                                              { PASM->AddPermissionSetDecl((yyvsp[-2].secAct), (yyvsp[-1].binstr)); }
#line 8039 "asmparse.cpp"
    break;

  case 728: /* secDecl: _PERMISSIONSET secAction compQstring  */
#line 1805 "./asmparse.y"
                                                              { PASM->AddPermissionSetDecl((yyvsp[-1].secAct),BinStrToUnicode((yyvsp[0].binstr),true));}
#line 8045 "asmparse.cpp"
    break;

  case 729: /* secDecl: _PERMISSIONSET secAction '=' '{' secAttrSetBlob '}'  */
#line 1807 "./asmparse.y"
                                                              { BinStr* ret = new BinStr();
                                                                ret->insertInt8('.');
                                                                corEmitInt(ret, nSecAttrBlobs);
                                                                ret->append((yyvsp[-1].binstr));
                                                                PASM->AddPermissionSetDecl((yyvsp[-4].secAct),ret);
                                                                nSecAttrBlobs = 0; }
#line 8056 "asmparse.cpp"
    break;

  case 730: /* secAttrSetBlob: %empty  */
#line 1815 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); nSecAttrBlobs = 0;}
#line 8062 "asmparse.cpp"
    break;

  case 731: /* secAttrSetBlob: secAttrBlob  */
#line 1816 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); nSecAttrBlobs = 1; }
#line 8068 "asmparse.cpp"
    break;

  case 732: /* secAttrSetBlob: secAttrBlob ',' secAttrSetBlob  */
#line 1817 "./asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); nSecAttrBlobs++; }
#line 8074 "asmparse.cpp"
    break;

  case 733: /* secAttrBlob: typeSpec '=' '{' customBlobNVPairs '}'  */
#line 1821 "./asmparse.y"
                                                              { (yyval.binstr) = PASM->EncodeSecAttr(PASM->ReflectionNotation((yyvsp[-4].token)),(yyvsp[-1].binstr),nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
#line 8081 "asmparse.cpp"
    break;

  case 734: /* secAttrBlob: CLASS_ SQSTRING '=' '{' customBlobNVPairs '}'  */
#line 1824 "./asmparse.y"
                                                              { (yyval.binstr) = PASM->EncodeSecAttr((yyvsp[-4].string),(yyvsp[-1].binstr),nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
#line 8088 "asmparse.cpp"
    break;

  case 735: /* psetHead: _PERMISSIONSET secAction '=' '('  */
#line 1828 "./asmparse.y"
                                                              { (yyval.secAct) = (yyvsp[-2].secAct); bParsingByteArray = TRUE; }
#line 8094 "asmparse.cpp"
    break;

  case 736: /* psetHead: _PERMISSIONSET secAction BYTEARRAY_ '('  */
#line 1830 "./asmparse.y"
                                                              { (yyval.secAct) = (yyvsp[-2].secAct); bParsingByteArray = TRUE; }
#line 8100 "asmparse.cpp"
    break;

  case 737: /* nameValPairs: nameValPair  */
#line 1833 "./asmparse.y"
                                                              { (yyval.pair) = (yyvsp[0].pair); }
#line 8106 "asmparse.cpp"
    break;

  case 738: /* nameValPairs: nameValPair ',' nameValPairs  */
#line 1834 "./asmparse.y"
                                                              { (yyval.pair) = (yyvsp[-2].pair)->Concat((yyvsp[0].pair)); }
#line 8112 "asmparse.cpp"
    break;

  case 739: /* nameValPair: compQstring '=' caValue  */
#line 1837 "./asmparse.y"
                                                              { (yyvsp[-2].binstr)->appendInt8(0); (yyval.pair) = new NVPair((yyvsp[-2].binstr), (yyvsp[0].binstr)); }
#line 8118 "asmparse.cpp"
    break;

  case 740: /* truefalse: TRUE_  */
#line 1840 "./asmparse.y"
                                                              { (yyval.int32) = 1; }
#line 8124 "asmparse.cpp"
    break;

  case 741: /* truefalse: FALSE_  */
#line 1841 "./asmparse.y"
                                                              { (yyval.int32) = 0; }
#line 8130 "asmparse.cpp"
    break;

  case 742: /* caValue: truefalse  */
#line 1844 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_BOOLEAN);
                                                                (yyval.binstr)->appendInt8((yyvsp[0].int32)); }
#line 8138 "asmparse.cpp"
    break;

  case 743: /* caValue: int32  */
#line 1847 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_I4);
                                                                (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 8146 "asmparse.cpp"
    break;

  case 744: /* caValue: INT32_ '(' int32 ')'  */
#line 1850 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_I4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8154 "asmparse.cpp"
    break;

  case 745: /* caValue: compQstring  */
#line 1853 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_STRING);
                                                                (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr);
                                                                (yyval.binstr)->appendInt8(0); }
#line 8163 "asmparse.cpp"
    break;

  case 746: /* caValue: className '(' INT8_ ':' int32 ')'  */
#line 1857 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(1);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8174 "asmparse.cpp"
    break;

  case 747: /* caValue: className '(' INT16_ ':' int32 ')'  */
#line 1863 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(2);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8185 "asmparse.cpp"
    break;

  case 748: /* caValue: className '(' INT32_ ':' int32 ')'  */
#line 1869 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8196 "asmparse.cpp"
    break;

  case 749: /* caValue: className '(' int32 ')'  */
#line 1875 "./asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-3].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8207 "asmparse.cpp"
    break;

  case 750: /* secAction: REQUEST_  */
#line 1883 "./asmparse.y"
                                                              { (yyval.secAct) = dclRequest; }
#line 8213 "asmparse.cpp"
    break;

  case 751: /* secAction: DEMAND_  */
#line 1884 "./asmparse.y"
                                                              { (yyval.secAct) = dclDemand; }
#line 8219 "asmparse.cpp"
    break;

  case 752: /* secAction: ASSERT_  */
#line 1885 "./asmparse.y"
                                                              { (yyval.secAct) = dclAssert; }
#line 8225 "asmparse.cpp"
    break;

  case 753: /* secAction: DENY_  */
#line 1886 "./asmparse.y"
                                                              { (yyval.secAct) = dclDeny; }
#line 8231 "asmparse.cpp"
    break;

  case 754: /* secAction: PERMITONLY_  */
#line 1887 "./asmparse.y"
                                                              { (yyval.secAct) = dclPermitOnly; }
#line 8237 "asmparse.cpp"
    break;

  case 755: /* secAction: LINKCHECK_  */
#line 1888 "./asmparse.y"
                                                              { (yyval.secAct) = dclLinktimeCheck; }
#line 8243 "asmparse.cpp"
    break;

  case 756: /* secAction: INHERITCHECK_  */
#line 1889 "./asmparse.y"
                                                              { (yyval.secAct) = dclInheritanceCheck; }
#line 8249 "asmparse.cpp"
    break;

  case 757: /* secAction: REQMIN_  */
#line 1890 "./asmparse.y"
                                                              { (yyval.secAct) = dclRequestMinimum; }
#line 8255 "asmparse.cpp"
    break;

  case 758: /* secAction: REQOPT_  */
#line 1891 "./asmparse.y"
                                                              { (yyval.secAct) = dclRequestOptional; }
#line 8261 "asmparse.cpp"
    break;

  case 759: /* secAction: REQREFUSE_  */
#line 1892 "./asmparse.y"
                                                              { (yyval.secAct) = dclRequestRefuse; }
#line 8267 "asmparse.cpp"
    break;

  case 760: /* secAction: PREJITGRANT_  */
#line 1893 "./asmparse.y"
                                                              { (yyval.secAct) = dclPrejitGrant; }
#line 8273 "asmparse.cpp"
    break;

  case 761: /* secAction: PREJITDENY_  */
#line 1894 "./asmparse.y"
                                                              { (yyval.secAct) = dclPrejitDenied; }
#line 8279 "asmparse.cpp"
    break;

  case 762: /* secAction: NONCASDEMAND_  */
#line 1895 "./asmparse.y"
                                                              { (yyval.secAct) = dclNonCasDemand; }
#line 8285 "asmparse.cpp"
    break;

  case 763: /* secAction: NONCASLINKDEMAND_  */
#line 1896 "./asmparse.y"
                                                              { (yyval.secAct) = dclNonCasLinkDemand; }
#line 8291 "asmparse.cpp"
    break;

  case 764: /* secAction: NONCASINHERITANCE_  */
#line 1897 "./asmparse.y"
                                                              { (yyval.secAct) = dclNonCasInheritance; }
#line 8297 "asmparse.cpp"
    break;

  case 765: /* esHead: _LINE  */
#line 1901 "./asmparse.y"
                                                              { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = FALSE; }
#line 8303 "asmparse.cpp"
    break;

  case 766: /* esHead: P_LINE  */
#line 1902 "./asmparse.y"
                                                              { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = TRUE; }
#line 8309 "asmparse.cpp"
    break;

  case 767: /* extSourceSpec: esHead int32 SQSTRING  */
#line 1905 "./asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-1].int32);
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8317 "asmparse.cpp"
    break;

  case 768: /* extSourceSpec: esHead int32  */
#line 1908 "./asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[0].int32);
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1); }
#line 8324 "asmparse.cpp"
    break;

  case 769: /* extSourceSpec: esHead int32 ':' int32 SQSTRING  */
#line 1910 "./asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-3].int32);
                                                                PENV->nExtCol=(yyvsp[-1].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8332 "asmparse.cpp"
    break;

  case 770: /* extSourceSpec: esHead int32 ':' int32  */
#line 1913 "./asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-2].int32);
                                                                PENV->nExtCol=(yyvsp[0].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);}
#line 8339 "asmparse.cpp"
    break;

  case 771: /* extSourceSpec: esHead int32 ':' int32 ',' int32 SQSTRING  */
#line 1916 "./asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-5].int32);
                                                                PENV->nExtCol=(yyvsp[-3].int32); PENV->nExtColEnd = (yyvsp[-1].int32);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8347 "asmparse.cpp"
    break;

  case 772: /* extSourceSpec: esHead int32 ':' int32 ',' int32  */
#line 1920 "./asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-4].int32);
                                                                PENV->nExtCol=(yyvsp[-2].int32); PENV->nExtColEnd = (yyvsp[0].int32); }
#line 8354 "asmparse.cpp"
    break;

  case 773: /* extSourceSpec: esHead int32 ',' int32 ':' int32 SQSTRING  */
#line 1923 "./asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-5].int32); PENV->nExtLineEnd = (yyvsp[-3].int32);
                                                                PENV->nExtCol=(yyvsp[-1].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8362 "asmparse.cpp"
    break;

  case 774: /* extSourceSpec: esHead int32 ',' int32 ':' int32  */
#line 1927 "./asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-4].int32); PENV->nExtLineEnd = (yyvsp[-2].int32);
                                                                PENV->nExtCol=(yyvsp[0].int32); PENV->nExtColEnd = static_cast<unsigned>(-1); }
#line 8369 "asmparse.cpp"
    break;

  case 775: /* extSourceSpec: esHead int32 ',' int32 ':' int32 ',' int32 SQSTRING  */
#line 1930 "./asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-7].int32); PENV->nExtLineEnd = (yyvsp[-5].int32);
                                                                PENV->nExtCol=(yyvsp[-3].int32); PENV->nExtColEnd = (yyvsp[-1].int32);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8377 "asmparse.cpp"
    break;

  case 776: /* extSourceSpec: esHead int32 ',' int32 ':' int32 ',' int32  */
#line 1934 "./asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-6].int32); PENV->nExtLineEnd = (yyvsp[-4].int32);
                                                                PENV->nExtCol=(yyvsp[-2].int32); PENV->nExtColEnd = (yyvsp[0].int32); }
#line 8384 "asmparse.cpp"
    break;

  case 777: /* extSourceSpec: esHead int32 QSTRING  */
#line 1936 "./asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-1].int32) - 1;
                                                                PENV->nExtCol = 0; PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].binstr));}
#line 8392 "asmparse.cpp"
    break;

  case 778: /* fileDecl: _FILE fileAttr dottedName fileEntry hashHead bytes ')' fileEntry  */
#line 1943 "./asmparse.y"
                                                              { PASMM->AddFile((yyvsp[-5].string), (yyvsp[-6].fileAttr)|(yyvsp[-4].fileAttr)|(yyvsp[0].fileAttr), (yyvsp[-2].binstr)); }
#line 8398 "asmparse.cpp"
    break;

  case 779: /* fileDecl: _FILE fileAttr dottedName fileEntry  */
#line 1944 "./asmparse.y"
                                                              { PASMM->AddFile((yyvsp[-1].string), (yyvsp[-2].fileAttr)|(yyvsp[0].fileAttr), NULL); }
#line 8404 "asmparse.cpp"
    break;

  case 780: /* fileAttr: %empty  */
#line 1947 "./asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0; }
#line 8410 "asmparse.cpp"
    break;

  case 781: /* fileAttr: fileAttr NOMETADATA_  */
#line 1948 "./asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) ((yyvsp[-1].fileAttr) | ffContainsNoMetaData); }
#line 8416 "asmparse.cpp"
    break;

  case 782: /* fileEntry: %empty  */
#line 1951 "./asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0; }
#line 8422 "asmparse.cpp"
    break;

  case 783: /* fileEntry: _ENTRYPOINT  */
#line 1952 "./asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0x80000000; }
#line 8428 "asmparse.cpp"
    break;

  case 784: /* hashHead: _HASH '=' '('  */
#line 1955 "./asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8434 "asmparse.cpp"
    break;

  case 785: /* assemblyHead: _ASSEMBLY asmAttr dottedName  */
#line 1958 "./asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[0].string), NULL, (DWORD)(yyvsp[-1].asmAttr), FALSE); }
#line 8440 "asmparse.cpp"
    break;

  case 786: /* asmAttr: %empty  */
#line 1961 "./asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) 0; }
#line 8446 "asmparse.cpp"
    break;

  case 787: /* asmAttr: asmAttr RETARGETABLE_  */
#line 1962 "./asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afRetargetable); }
#line 8452 "asmparse.cpp"
    break;

  case 788: /* asmAttr: asmAttr WINDOWSRUNTIME_  */
#line 1963 "./asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afContentType_WindowsRuntime); }
#line 8458 "asmparse.cpp"
    break;

  case 789: /* asmAttr: asmAttr NOPLATFORM_  */
#line 1964 "./asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afPA_NoPlatform); }
#line 8464 "asmparse.cpp"
    break;

  case 790: /* asmAttr: asmAttr LEGACY_ LIBRARY_  */
#line 1965 "./asmparse.y"
                                                              { (yyval.asmAttr) = (yyvsp[-2].asmAttr); }
#line 8470 "asmparse.cpp"
    break;

  case 791: /* asmAttr: asmAttr CIL_  */
#line 1966 "./asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_MSIL); }
#line 8476 "asmparse.cpp"
    break;

  case 792: /* asmAttr: asmAttr X86_  */
#line 1967 "./asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_x86); }
#line 8482 "asmparse.cpp"
    break;

  case 793: /* asmAttr: asmAttr AMD64_  */
#line 1968 "./asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_AMD64); }
#line 8488 "asmparse.cpp"
    break;

  case 794: /* asmAttr: asmAttr ARM_  */
#line 1969 "./asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_ARM); }
#line 8494 "asmparse.cpp"
    break;

  case 795: /* asmAttr: asmAttr ARM64_  */
#line 1970 "./asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_ARM64); }
#line 8500 "asmparse.cpp"
    break;

  case 798: /* assemblyDecl: _HASH ALGORITHM_ int32  */
#line 1977 "./asmparse.y"
                                                              { PASMM->SetAssemblyHashAlg((yyvsp[0].int32)); }
#line 8506 "asmparse.cpp"
    break;

  case 801: /* intOrWildcard: int32  */
#line 1982 "./asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 8512 "asmparse.cpp"
    break;

  case 802: /* intOrWildcard: '*'  */
#line 1983 "./asmparse.y"
                                                              { (yyval.int32) = 0xFFFF; }
#line 8518 "asmparse.cpp"
    break;

  case 803: /* asmOrRefDecl: publicKeyHead bytes ')'  */
#line 1986 "./asmparse.y"
                                                              { PASMM->SetAssemblyPublicKey((yyvsp[-1].binstr)); }
#line 8524 "asmparse.cpp"
    break;

  case 804: /* asmOrRefDecl: _VER intOrWildcard ':' intOrWildcard ':' intOrWildcard ':' intOrWildcard  */
#line 1988 "./asmparse.y"
                                                              { PASMM->SetAssemblyVer((USHORT)(yyvsp[-6].int32), (USHORT)(yyvsp[-4].int32), (USHORT)(yyvsp[-2].int32), (USHORT)(yyvsp[0].int32)); }
#line 8530 "asmparse.cpp"
    break;

  case 805: /* asmOrRefDecl: _LOCALE compQstring  */
#line 1989 "./asmparse.y"
                                                              { (yyvsp[0].binstr)->appendInt8(0); PASMM->SetAssemblyLocale((yyvsp[0].binstr),TRUE); }
#line 8536 "asmparse.cpp"
    break;

  case 806: /* asmOrRefDecl: localeHead bytes ')'  */
#line 1990 "./asmparse.y"
                                                              { PASMM->SetAssemblyLocale((yyvsp[-1].binstr),FALSE); }
#line 8542 "asmparse.cpp"
    break;

  case 809: /* publicKeyHead: _PUBLICKEY '=' '('  */
#line 1995 "./asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8548 "asmparse.cpp"
    break;

  case 810: /* publicKeyTokenHead: _PUBLICKEYTOKEN '=' '('  */
#line 1998 "./asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8554 "asmparse.cpp"
    break;

  case 811: /* localeHead: _LOCALE '=' '('  */
#line 2001 "./asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8560 "asmparse.cpp"
    break;

  case 812: /* assemblyRefHead: _ASSEMBLY EXTERN_ asmAttr dottedName  */
#line 2005 "./asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[0].string), NULL, (yyvsp[-1].asmAttr), TRUE); }
#line 8566 "asmparse.cpp"
    break;

  case 813: /* assemblyRefHead: _ASSEMBLY EXTERN_ asmAttr dottedName AS_ dottedName  */
#line 2007 "./asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[-2].string), (yyvsp[0].string), (yyvsp[-3].asmAttr), TRUE); }
#line 8572 "asmparse.cpp"
    break;

  case 816: /* assemblyRefDecl: hashHead bytes ')'  */
#line 2014 "./asmparse.y"
                                                              { PASMM->SetAssemblyHashBlob((yyvsp[-1].binstr)); }
#line 8578 "asmparse.cpp"
    break;

  case 818: /* assemblyRefDecl: publicKeyTokenHead bytes ')'  */
#line 2016 "./asmparse.y"
                                                              { PASMM->SetAssemblyPublicKeyToken((yyvsp[-1].binstr)); }
#line 8584 "asmparse.cpp"
    break;

  case 819: /* assemblyRefDecl: AUTO_  */
#line 2017 "./asmparse.y"
                                                              { PASMM->SetAssemblyAutodetect(); }
#line 8590 "asmparse.cpp"
    break;

  case 820: /* exptypeHead: _CLASS EXTERN_ exptAttr dottedName  */
#line 2020 "./asmparse.y"
                                                              { PASMM->StartComType((yyvsp[0].string), (yyvsp[-1].exptAttr));}
#line 8596 "asmparse.cpp"
    break;

  case 821: /* exportHead: _EXPORT exptAttr dottedName  */
#line 2023 "./asmparse.y"
                                                                              { PASMM->StartComType((yyvsp[0].string), (yyvsp[-1].exptAttr)); }
#line 8602 "asmparse.cpp"
    break;

  case 822: /* exptAttr: %empty  */
#line 2026 "./asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) 0; }
#line 8608 "asmparse.cpp"
    break;

  case 823: /* exptAttr: exptAttr PRIVATE_  */
#line 2027 "./asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdNotPublic); }
#line 8614 "asmparse.cpp"
    break;

  case 824: /* exptAttr: exptAttr PUBLIC_  */
#line 2028 "./asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdPublic); }
#line 8620 "asmparse.cpp"
    break;

  case 825: /* exptAttr: exptAttr FORWARDER_  */
#line 2029 "./asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdForwarder); }
#line 8626 "asmparse.cpp"
    break;

  case 826: /* exptAttr: exptAttr NESTED_ PUBLIC_  */
#line 2030 "./asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedPublic); }
#line 8632 "asmparse.cpp"
    break;

  case 827: /* exptAttr: exptAttr NESTED_ PRIVATE_  */
#line 2031 "./asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedPrivate); }
#line 8638 "asmparse.cpp"
    break;

  case 828: /* exptAttr: exptAttr NESTED_ FAMILY_  */
#line 2032 "./asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamily); }
#line 8644 "asmparse.cpp"
    break;

  case 829: /* exptAttr: exptAttr NESTED_ ASSEMBLY_  */
#line 2033 "./asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedAssembly); }
#line 8650 "asmparse.cpp"
    break;

  case 830: /* exptAttr: exptAttr NESTED_ FAMANDASSEM_  */
#line 2034 "./asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamANDAssem); }
#line 8656 "asmparse.cpp"
    break;

  case 831: /* exptAttr: exptAttr NESTED_ FAMORASSEM_  */
#line 2035 "./asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamORAssem); }
#line 8662 "asmparse.cpp"
    break;

  case 834: /* exptypeDecl: _FILE dottedName  */
#line 2042 "./asmparse.y"
                                                              { PASMM->SetComTypeFile((yyvsp[0].string)); }
#line 8668 "asmparse.cpp"
    break;

  case 835: /* exptypeDecl: _CLASS EXTERN_ slashedName  */
#line 2043 "./asmparse.y"
                                                               { PASMM->SetComTypeComType((yyvsp[0].string)); }
#line 8674 "asmparse.cpp"
    break;

  case 836: /* exptypeDecl: _ASSEMBLY EXTERN_ dottedName  */
#line 2044 "./asmparse.y"
                                                              { PASMM->SetComTypeAsmRef((yyvsp[0].string)); }
#line 8680 "asmparse.cpp"
    break;

  case 837: /* exptypeDecl: MDTOKEN_ '(' int32 ')'  */
#line 2045 "./asmparse.y"
                                                              { if(!PASMM->SetComTypeImplementationTok((yyvsp[-1].int32)))
                                                                  PASM->report->error("Invalid implementation of exported type\n"); }
#line 8687 "asmparse.cpp"
    break;

  case 838: /* exptypeDecl: _CLASS int32  */
#line 2047 "./asmparse.y"
                                                              { if(!PASMM->SetComTypeClassTok((yyvsp[0].int32)))
                                                                  PASM->report->error("Invalid TypeDefID of exported type\n"); }
#line 8694 "asmparse.cpp"
    break;

  case 841: /* manifestResHead: _MRESOURCE manresAttr dottedName  */
#line 2053 "./asmparse.y"
                                                              { PASMM->StartManifestRes((yyvsp[0].string), (yyvsp[0].string), (yyvsp[-1].manresAttr)); }
#line 8700 "asmparse.cpp"
    break;

  case 842: /* manifestResHead: _MRESOURCE manresAttr dottedName AS_ dottedName  */
#line 2055 "./asmparse.y"
                                                              { PASMM->StartManifestRes((yyvsp[-2].string), (yyvsp[0].string), (yyvsp[-3].manresAttr)); }
#line 8706 "asmparse.cpp"
    break;

  case 843: /* manresAttr: %empty  */
#line 2058 "./asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) 0; }
#line 8712 "asmparse.cpp"
    break;

  case 844: /* manresAttr: manresAttr PUBLIC_  */
#line 2059 "./asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) ((yyvsp[-1].manresAttr) | mrPublic); }
#line 8718 "asmparse.cpp"
    break;

  case 845: /* manresAttr: manresAttr PRIVATE_  */
#line 2060 "./asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) ((yyvsp[-1].manresAttr) | mrPrivate); }
#line 8724 "asmparse.cpp"
    break;

  case 848: /* manifestResDecl: _FILE dottedName AT_ int32  */
#line 2067 "./asmparse.y"
                                                              { PASMM->SetManifestResFile((yyvsp[-2].string), (ULONG)(yyvsp[0].int32)); }
#line 8730 "asmparse.cpp"
    break;

  case 849: /* manifestResDecl: _ASSEMBLY EXTERN_ dottedName  */
#line 2068 "./asmparse.y"
                                                              { PASMM->SetManifestResAsmRef((yyvsp[0].string)); }
#line 8736 "asmparse.cpp"
    break;


#line 8740 "asmparse.cpp"

      default: break;
    }
  /* User semantic actions sometimes alter yychar, and that requires
     that yytoken be updated with the new translation.  We take the
     approach of translating immediately before every use of yytoken.
     One alternative is translating here after every semantic action,
     but that translation would be missed if the semantic action invokes
     YYABORT, YYACCEPT, or YYERROR immediately after altering yychar or
     if it invokes YYBACKUP.  In the case of YYABORT or YYACCEPT, an
     incorrect destructor might then be invoked immediately.  In the
     case of YYERROR or YYBACKUP, subsequent parser actions might lead
     to an incorrect destructor call or verbose syntax error message
     before the lookahead is translated.  */
  YY_SYMBOL_PRINT ("-> $$ =", YY_CAST (yysymbol_kind_t, yyr1[yyn]), &yyval, &yyloc);

  YYPOPSTACK (yylen);
  yylen = 0;

  *++yyvsp = yyval;

  /* Now 'shift' the result of the reduction.  Determine what state
     that goes to, based on the state we popped back to and the rule
     number reduced by.  */
  {
    const int yylhs = yyr1[yyn] - YYNTOKENS;
    const int yyi = yypgoto[yylhs] + *yyssp;
    yystate = (0 <= yyi && yyi <= YYLAST && yycheck[yyi] == *yyssp
               ? yytable[yyi]
               : yydefgoto[yylhs]);
  }

  goto yynewstate;


/*--------------------------------------.
| yyerrlab -- here on detecting error.  |
`--------------------------------------*/
yyerrlab:
  /* Make sure we have latest lookahead translation.  See comments at
     user semantic actions for why this is necessary.  */
  yytoken = yychar == YYEMPTY ? YYSYMBOL_YYEMPTY : YYTRANSLATE (yychar);
  /* If not already recovering from an error, report this error.  */
  if (!yyerrstatus)
    {
      ++yynerrs;
      yyerror (YY_("syntax error"));
    }

  if (yyerrstatus == 3)
    {
      /* If just tried and failed to reuse lookahead token after an
         error, discard it.  */

      if (yychar <= YYEOF)
        {
          /* Return failure if at end of input.  */
          if (yychar == YYEOF)
            YYABORT;
        }
      else
        {
          yydestruct ("Error: discarding",
                      yytoken, &yylval);
          yychar = YYEMPTY;
        }
    }

  /* Else will try to reuse lookahead token after shifting the error
     token.  */
  goto yyerrlab1;


/*---------------------------------------------------.
| yyerrorlab -- error raised explicitly by YYERROR.  |
`---------------------------------------------------*/
yyerrorlab:
  /* Pacify compilers when the user code never invokes YYERROR and the
     label yyerrorlab therefore never appears in user code.  */
  if (0)
    YYERROR;
  ++yynerrs;

  /* Do not reclaim the symbols of the rule whose action triggered
     this YYERROR.  */
  YYPOPSTACK (yylen);
  yylen = 0;
  YY_STACK_PRINT (yyss, yyssp);
  yystate = *yyssp;
  goto yyerrlab1;


/*-------------------------------------------------------------.
| yyerrlab1 -- common code for both syntax error and YYERROR.  |
`-------------------------------------------------------------*/
yyerrlab1:
  yyerrstatus = 3;      /* Each real token shifted decrements this.  */

  /* Pop stack until we find a state that shifts the error token.  */
  for (;;)
    {
      yyn = yypact[yystate];
      if (!yypact_value_is_default (yyn))
        {
          yyn += YYSYMBOL_YYerror;
          if (0 <= yyn && yyn <= YYLAST && yycheck[yyn] == YYSYMBOL_YYerror)
            {
              yyn = yytable[yyn];
              if (0 < yyn)
                break;
            }
        }

      /* Pop the current state because it cannot handle the error token.  */
      if (yyssp == yyss)
        YYABORT;


      yydestruct ("Error: popping",
                  YY_ACCESSING_SYMBOL (yystate), yyvsp);
      YYPOPSTACK (1);
      yystate = *yyssp;
      YY_STACK_PRINT (yyss, yyssp);
    }

  YY_IGNORE_MAYBE_UNINITIALIZED_BEGIN
  *++yyvsp = yylval;
  YY_IGNORE_MAYBE_UNINITIALIZED_END


  /* Shift the error token.  */
  YY_SYMBOL_PRINT ("Shifting", YY_ACCESSING_SYMBOL (yyn), yyvsp, yylsp);

  yystate = yyn;
  goto yynewstate;


/*-------------------------------------.
| yyacceptlab -- YYACCEPT comes here.  |
`-------------------------------------*/
yyacceptlab:
  yyresult = 0;
  goto yyreturnlab;


/*-----------------------------------.
| yyabortlab -- YYABORT comes here.  |
`-----------------------------------*/
yyabortlab:
  yyresult = 1;
  goto yyreturnlab;


/*-----------------------------------------------------------.
| yyexhaustedlab -- YYNOMEM (memory exhaustion) comes here.  |
`-----------------------------------------------------------*/
yyexhaustedlab:
  yyerror (YY_("memory exhausted"));
  yyresult = 2;
  goto yyreturnlab;


/*----------------------------------------------------------.
| yyreturnlab -- parsing is finished, clean up and return.  |
`----------------------------------------------------------*/
yyreturnlab:
  if (yychar != YYEMPTY)
    {
      /* Make sure we have latest lookahead translation.  See comments at
         user semantic actions for why this is necessary.  */
      yytoken = YYTRANSLATE (yychar);
      yydestruct ("Cleanup: discarding lookahead",
                  yytoken, &yylval);
    }
  /* Do not reclaim the symbols of the rule whose action triggered
     this YYABORT or YYACCEPT.  */
  YYPOPSTACK (yylen);
  YY_STACK_PRINT (yyss, yyssp);
  while (yyssp != yyss)
    {
      yydestruct ("Cleanup: popping",
                  YY_ACCESSING_SYMBOL (+*yyssp), yyvsp);
      YYPOPSTACK (1);
    }
#ifndef yyoverflow
  if (yyss != yyssa)
    YYSTACK_FREE (yyss);
#endif

  return yyresult;
}

#line 2073 "./asmparse.y"


#include "grammar_after.cpp"
