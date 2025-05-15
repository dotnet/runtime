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
#line 1 "asmparse.y"


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// File asmparse.y
//
#include "ilasmpch.h"

#include "grammar_before.cpp"


#line 85 "prebuilt\\asmparse.cpp"

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
    BAD_COMMENT_ = 258,            /* BAD_COMMENT_  */
    BAD_LITERAL_ = 259,            /* BAD_LITERAL_  */
    ID = 260,                      /* ID  */
    DOTTEDNAME = 261,              /* DOTTEDNAME  */
    QSTRING = 262,                 /* QSTRING  */
    SQSTRING = 263,                /* SQSTRING  */
    INT32_V = 264,                 /* INT32_V  */
    INT64_V = 265,                 /* INT64_V  */
    FLOAT64 = 266,                 /* FLOAT64  */
    HEXBYTE = 267,                 /* HEXBYTE  */
    TYPEDEF_T = 268,               /* TYPEDEF_T  */
    TYPEDEF_M = 269,               /* TYPEDEF_M  */
    TYPEDEF_F = 270,               /* TYPEDEF_F  */
    TYPEDEF_TS = 271,              /* TYPEDEF_TS  */
    TYPEDEF_MR = 272,              /* TYPEDEF_MR  */
    TYPEDEF_CA = 273,              /* TYPEDEF_CA  */
    DCOLON = 274,                  /* DCOLON  */
    ELLIPSIS = 275,                /* ELLIPSIS  */
    VOID_ = 276,                   /* VOID_  */
    BOOL_ = 277,                   /* BOOL_  */
    CHAR_ = 278,                   /* CHAR_  */
    UNSIGNED_ = 279,               /* UNSIGNED_  */
    INT_ = 280,                    /* INT_  */
    INT8_ = 281,                   /* INT8_  */
    INT16_ = 282,                  /* INT16_  */
    INT32_ = 283,                  /* INT32_  */
    INT64_ = 284,                  /* INT64_  */
    FLOAT_ = 285,                  /* FLOAT_  */
    FLOAT32_ = 286,                /* FLOAT32_  */
    FLOAT64_ = 287,                /* FLOAT64_  */
    BYTEARRAY_ = 288,              /* BYTEARRAY_  */
    UINT_ = 289,                   /* UINT_  */
    UINT8_ = 290,                  /* UINT8_  */
    UINT16_ = 291,                 /* UINT16_  */
    UINT32_ = 292,                 /* UINT32_  */
    UINT64_ = 293,                 /* UINT64_  */
    FLAGS_ = 294,                  /* FLAGS_  */
    CALLCONV_ = 295,               /* CALLCONV_  */
    MDTOKEN_ = 296,                /* MDTOKEN_  */
    OBJECT_ = 297,                 /* OBJECT_  */
    STRING_ = 298,                 /* STRING_  */
    NULLREF_ = 299,                /* NULLREF_  */
    DEFAULT_ = 300,                /* DEFAULT_  */
    CDECL_ = 301,                  /* CDECL_  */
    VARARG_ = 302,                 /* VARARG_  */
    STDCALL_ = 303,                /* STDCALL_  */
    THISCALL_ = 304,               /* THISCALL_  */
    FASTCALL_ = 305,               /* FASTCALL_  */
    CLASS_ = 306,                  /* CLASS_  */
    BYREFLIKE_ = 307,              /* BYREFLIKE_  */
    TYPEDREF_ = 308,               /* TYPEDREF_  */
    UNMANAGED_ = 309,              /* UNMANAGED_  */
    FINALLY_ = 310,                /* FINALLY_  */
    HANDLER_ = 311,                /* HANDLER_  */
    CATCH_ = 312,                  /* CATCH_  */
    FILTER_ = 313,                 /* FILTER_  */
    FAULT_ = 314,                  /* FAULT_  */
    EXTENDS_ = 315,                /* EXTENDS_  */
    IMPLEMENTS_ = 316,             /* IMPLEMENTS_  */
    TO_ = 317,                     /* TO_  */
    AT_ = 318,                     /* AT_  */
    TLS_ = 319,                    /* TLS_  */
    TRUE_ = 320,                   /* TRUE_  */
    FALSE_ = 321,                  /* FALSE_  */
    _INTERFACEIMPL = 322,          /* _INTERFACEIMPL  */
    VALUE_ = 323,                  /* VALUE_  */
    VALUETYPE_ = 324,              /* VALUETYPE_  */
    NATIVE_ = 325,                 /* NATIVE_  */
    INSTANCE_ = 326,               /* INSTANCE_  */
    SPECIALNAME_ = 327,            /* SPECIALNAME_  */
    FORWARDER_ = 328,              /* FORWARDER_  */
    STATIC_ = 329,                 /* STATIC_  */
    PUBLIC_ = 330,                 /* PUBLIC_  */
    PRIVATE_ = 331,                /* PRIVATE_  */
    FAMILY_ = 332,                 /* FAMILY_  */
    FINAL_ = 333,                  /* FINAL_  */
    SYNCHRONIZED_ = 334,           /* SYNCHRONIZED_  */
    INTERFACE_ = 335,              /* INTERFACE_  */
    SEALED_ = 336,                 /* SEALED_  */
    NESTED_ = 337,                 /* NESTED_  */
    ABSTRACT_ = 338,               /* ABSTRACT_  */
    AUTO_ = 339,                   /* AUTO_  */
    SEQUENTIAL_ = 340,             /* SEQUENTIAL_  */
    EXPLICIT_ = 341,               /* EXPLICIT_  */
    ANSI_ = 342,                   /* ANSI_  */
    UNICODE_ = 343,                /* UNICODE_  */
    AUTOCHAR_ = 344,               /* AUTOCHAR_  */
    IMPORT_ = 345,                 /* IMPORT_  */
    ENUM_ = 346,                   /* ENUM_  */
    VIRTUAL_ = 347,                /* VIRTUAL_  */
    NOINLINING_ = 348,             /* NOINLINING_  */
    AGGRESSIVEINLINING_ = 349,     /* AGGRESSIVEINLINING_  */
    NOOPTIMIZATION_ = 350,         /* NOOPTIMIZATION_  */
    AGGRESSIVEOPTIMIZATION_ = 351, /* AGGRESSIVEOPTIMIZATION_  */
    ASYNC_ = 352,                  /* ASYNC_  */
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
    ERROR_ = 513,                  /* ERROR_  */
    HRESULT_ = 514,                /* HRESULT_  */
    CARRAY_ = 515,                 /* CARRAY_  */
    USERDEFINED_ = 516,            /* USERDEFINED_  */
    RECORD_ = 517,                 /* RECORD_  */
    FILETIME_ = 518,               /* FILETIME_  */
    BLOB_ = 519,                   /* BLOB_  */
    STREAM_ = 520,                 /* STREAM_  */
    STORAGE_ = 521,                /* STORAGE_  */
    STREAMED_OBJECT_ = 522,        /* STREAMED_OBJECT_  */
    STORED_OBJECT_ = 523,          /* STORED_OBJECT_  */
    BLOB_OBJECT_ = 524,            /* BLOB_OBJECT_  */
    CF_ = 525,                     /* CF_  */
    CLSID_ = 526,                  /* CLSID_  */
    VECTOR_ = 527,                 /* VECTOR_  */
    _SUBSYSTEM = 528,              /* _SUBSYSTEM  */
    _CORFLAGS = 529,               /* _CORFLAGS  */
    ALIGNMENT_ = 530,              /* ALIGNMENT_  */
    _IMAGEBASE = 531,              /* _IMAGEBASE  */
    _STACKRESERVE = 532,           /* _STACKRESERVE  */
    _TYPEDEF = 533,                /* _TYPEDEF  */
    _TEMPLATE = 534,               /* _TEMPLATE  */
    _TYPELIST = 535,               /* _TYPELIST  */
    _MSCORLIB = 536,               /* _MSCORLIB  */
    P_DEFINE = 537,                /* P_DEFINE  */
    P_UNDEF = 538,                 /* P_UNDEF  */
    P_IFDEF = 539,                 /* P_IFDEF  */
    P_IFNDEF = 540,                /* P_IFNDEF  */
    P_ELSE = 541,                  /* P_ELSE  */
    P_ENDIF = 542,                 /* P_ENDIF  */
    P_INCLUDE = 543,               /* P_INCLUDE  */
    CONSTRAINT_ = 544              /* CONSTRAINT_  */
  };
  typedef enum yytokentype yytoken_kind_t;
#endif

/* Value type.  */
#if ! defined YYSTYPE && ! defined YYSTYPE_IS_DECLARED
union YYSTYPE
{
#line 15 "asmparse.y"

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
        int64_t* int64;
        int32_t  int32;
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

#line 450 "prebuilt\\asmparse.cpp"

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
  YYSYMBOL_BAD_COMMENT_ = 3,               /* BAD_COMMENT_  */
  YYSYMBOL_BAD_LITERAL_ = 4,               /* BAD_LITERAL_  */
  YYSYMBOL_ID = 5,                         /* ID  */
  YYSYMBOL_DOTTEDNAME = 6,                 /* DOTTEDNAME  */
  YYSYMBOL_QSTRING = 7,                    /* QSTRING  */
  YYSYMBOL_SQSTRING = 8,                   /* SQSTRING  */
  YYSYMBOL_INT32_V = 9,                    /* INT32_V  */
  YYSYMBOL_INT64_V = 10,                   /* INT64_V  */
  YYSYMBOL_FLOAT64 = 11,                   /* FLOAT64  */
  YYSYMBOL_HEXBYTE = 12,                   /* HEXBYTE  */
  YYSYMBOL_TYPEDEF_T = 13,                 /* TYPEDEF_T  */
  YYSYMBOL_TYPEDEF_M = 14,                 /* TYPEDEF_M  */
  YYSYMBOL_TYPEDEF_F = 15,                 /* TYPEDEF_F  */
  YYSYMBOL_TYPEDEF_TS = 16,                /* TYPEDEF_TS  */
  YYSYMBOL_TYPEDEF_MR = 17,                /* TYPEDEF_MR  */
  YYSYMBOL_TYPEDEF_CA = 18,                /* TYPEDEF_CA  */
  YYSYMBOL_DCOLON = 19,                    /* DCOLON  */
  YYSYMBOL_ELLIPSIS = 20,                  /* ELLIPSIS  */
  YYSYMBOL_VOID_ = 21,                     /* VOID_  */
  YYSYMBOL_BOOL_ = 22,                     /* BOOL_  */
  YYSYMBOL_CHAR_ = 23,                     /* CHAR_  */
  YYSYMBOL_UNSIGNED_ = 24,                 /* UNSIGNED_  */
  YYSYMBOL_INT_ = 25,                      /* INT_  */
  YYSYMBOL_INT8_ = 26,                     /* INT8_  */
  YYSYMBOL_INT16_ = 27,                    /* INT16_  */
  YYSYMBOL_INT32_ = 28,                    /* INT32_  */
  YYSYMBOL_INT64_ = 29,                    /* INT64_  */
  YYSYMBOL_FLOAT_ = 30,                    /* FLOAT_  */
  YYSYMBOL_FLOAT32_ = 31,                  /* FLOAT32_  */
  YYSYMBOL_FLOAT64_ = 32,                  /* FLOAT64_  */
  YYSYMBOL_BYTEARRAY_ = 33,                /* BYTEARRAY_  */
  YYSYMBOL_UINT_ = 34,                     /* UINT_  */
  YYSYMBOL_UINT8_ = 35,                    /* UINT8_  */
  YYSYMBOL_UINT16_ = 36,                   /* UINT16_  */
  YYSYMBOL_UINT32_ = 37,                   /* UINT32_  */
  YYSYMBOL_UINT64_ = 38,                   /* UINT64_  */
  YYSYMBOL_FLAGS_ = 39,                    /* FLAGS_  */
  YYSYMBOL_CALLCONV_ = 40,                 /* CALLCONV_  */
  YYSYMBOL_MDTOKEN_ = 41,                  /* MDTOKEN_  */
  YYSYMBOL_OBJECT_ = 42,                   /* OBJECT_  */
  YYSYMBOL_STRING_ = 43,                   /* STRING_  */
  YYSYMBOL_NULLREF_ = 44,                  /* NULLREF_  */
  YYSYMBOL_DEFAULT_ = 45,                  /* DEFAULT_  */
  YYSYMBOL_CDECL_ = 46,                    /* CDECL_  */
  YYSYMBOL_VARARG_ = 47,                   /* VARARG_  */
  YYSYMBOL_STDCALL_ = 48,                  /* STDCALL_  */
  YYSYMBOL_THISCALL_ = 49,                 /* THISCALL_  */
  YYSYMBOL_FASTCALL_ = 50,                 /* FASTCALL_  */
  YYSYMBOL_CLASS_ = 51,                    /* CLASS_  */
  YYSYMBOL_BYREFLIKE_ = 52,                /* BYREFLIKE_  */
  YYSYMBOL_TYPEDREF_ = 53,                 /* TYPEDREF_  */
  YYSYMBOL_UNMANAGED_ = 54,                /* UNMANAGED_  */
  YYSYMBOL_FINALLY_ = 55,                  /* FINALLY_  */
  YYSYMBOL_HANDLER_ = 56,                  /* HANDLER_  */
  YYSYMBOL_CATCH_ = 57,                    /* CATCH_  */
  YYSYMBOL_FILTER_ = 58,                   /* FILTER_  */
  YYSYMBOL_FAULT_ = 59,                    /* FAULT_  */
  YYSYMBOL_EXTENDS_ = 60,                  /* EXTENDS_  */
  YYSYMBOL_IMPLEMENTS_ = 61,               /* IMPLEMENTS_  */
  YYSYMBOL_TO_ = 62,                       /* TO_  */
  YYSYMBOL_AT_ = 63,                       /* AT_  */
  YYSYMBOL_TLS_ = 64,                      /* TLS_  */
  YYSYMBOL_TRUE_ = 65,                     /* TRUE_  */
  YYSYMBOL_FALSE_ = 66,                    /* FALSE_  */
  YYSYMBOL__INTERFACEIMPL = 67,            /* _INTERFACEIMPL  */
  YYSYMBOL_VALUE_ = 68,                    /* VALUE_  */
  YYSYMBOL_VALUETYPE_ = 69,                /* VALUETYPE_  */
  YYSYMBOL_NATIVE_ = 70,                   /* NATIVE_  */
  YYSYMBOL_INSTANCE_ = 71,                 /* INSTANCE_  */
  YYSYMBOL_SPECIALNAME_ = 72,              /* SPECIALNAME_  */
  YYSYMBOL_FORWARDER_ = 73,                /* FORWARDER_  */
  YYSYMBOL_STATIC_ = 74,                   /* STATIC_  */
  YYSYMBOL_PUBLIC_ = 75,                   /* PUBLIC_  */
  YYSYMBOL_PRIVATE_ = 76,                  /* PRIVATE_  */
  YYSYMBOL_FAMILY_ = 77,                   /* FAMILY_  */
  YYSYMBOL_FINAL_ = 78,                    /* FINAL_  */
  YYSYMBOL_SYNCHRONIZED_ = 79,             /* SYNCHRONIZED_  */
  YYSYMBOL_INTERFACE_ = 80,                /* INTERFACE_  */
  YYSYMBOL_SEALED_ = 81,                   /* SEALED_  */
  YYSYMBOL_NESTED_ = 82,                   /* NESTED_  */
  YYSYMBOL_ABSTRACT_ = 83,                 /* ABSTRACT_  */
  YYSYMBOL_AUTO_ = 84,                     /* AUTO_  */
  YYSYMBOL_SEQUENTIAL_ = 85,               /* SEQUENTIAL_  */
  YYSYMBOL_EXPLICIT_ = 86,                 /* EXPLICIT_  */
  YYSYMBOL_ANSI_ = 87,                     /* ANSI_  */
  YYSYMBOL_UNICODE_ = 88,                  /* UNICODE_  */
  YYSYMBOL_AUTOCHAR_ = 89,                 /* AUTOCHAR_  */
  YYSYMBOL_IMPORT_ = 90,                   /* IMPORT_  */
  YYSYMBOL_ENUM_ = 91,                     /* ENUM_  */
  YYSYMBOL_VIRTUAL_ = 92,                  /* VIRTUAL_  */
  YYSYMBOL_NOINLINING_ = 93,               /* NOINLINING_  */
  YYSYMBOL_AGGRESSIVEINLINING_ = 94,       /* AGGRESSIVEINLINING_  */
  YYSYMBOL_NOOPTIMIZATION_ = 95,           /* NOOPTIMIZATION_  */
  YYSYMBOL_AGGRESSIVEOPTIMIZATION_ = 96,   /* AGGRESSIVEOPTIMIZATION_  */
  YYSYMBOL_ASYNC_ = 97,                    /* ASYNC_  */
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
  YYSYMBOL_ERROR_ = 258,                   /* ERROR_  */
  YYSYMBOL_HRESULT_ = 259,                 /* HRESULT_  */
  YYSYMBOL_CARRAY_ = 260,                  /* CARRAY_  */
  YYSYMBOL_USERDEFINED_ = 261,             /* USERDEFINED_  */
  YYSYMBOL_RECORD_ = 262,                  /* RECORD_  */
  YYSYMBOL_FILETIME_ = 263,                /* FILETIME_  */
  YYSYMBOL_BLOB_ = 264,                    /* BLOB_  */
  YYSYMBOL_STREAM_ = 265,                  /* STREAM_  */
  YYSYMBOL_STORAGE_ = 266,                 /* STORAGE_  */
  YYSYMBOL_STREAMED_OBJECT_ = 267,         /* STREAMED_OBJECT_  */
  YYSYMBOL_STORED_OBJECT_ = 268,           /* STORED_OBJECT_  */
  YYSYMBOL_BLOB_OBJECT_ = 269,             /* BLOB_OBJECT_  */
  YYSYMBOL_CF_ = 270,                      /* CF_  */
  YYSYMBOL_CLSID_ = 271,                   /* CLSID_  */
  YYSYMBOL_VECTOR_ = 272,                  /* VECTOR_  */
  YYSYMBOL__SUBSYSTEM = 273,               /* _SUBSYSTEM  */
  YYSYMBOL__CORFLAGS = 274,                /* _CORFLAGS  */
  YYSYMBOL_ALIGNMENT_ = 275,               /* ALIGNMENT_  */
  YYSYMBOL__IMAGEBASE = 276,               /* _IMAGEBASE  */
  YYSYMBOL__STACKRESERVE = 277,            /* _STACKRESERVE  */
  YYSYMBOL__TYPEDEF = 278,                 /* _TYPEDEF  */
  YYSYMBOL__TEMPLATE = 279,                /* _TEMPLATE  */
  YYSYMBOL__TYPELIST = 280,                /* _TYPELIST  */
  YYSYMBOL__MSCORLIB = 281,                /* _MSCORLIB  */
  YYSYMBOL_P_DEFINE = 282,                 /* P_DEFINE  */
  YYSYMBOL_P_UNDEF = 283,                  /* P_UNDEF  */
  YYSYMBOL_P_IFDEF = 284,                  /* P_IFDEF  */
  YYSYMBOL_P_IFNDEF = 285,                 /* P_IFNDEF  */
  YYSYMBOL_P_ELSE = 286,                   /* P_ELSE  */
  YYSYMBOL_P_ENDIF = 287,                  /* P_ENDIF  */
  YYSYMBOL_P_INCLUDE = 288,                /* P_INCLUDE  */
  YYSYMBOL_CONSTRAINT_ = 289,              /* CONSTRAINT_  */
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
  YYSYMBOL_typars = 353,                   /* typars  */
  YYSYMBOL_typarsRest = 354,               /* typarsRest  */
  YYSYMBOL_tyBound = 355,                  /* tyBound  */
  YYSYMBOL_genArity = 356,                 /* genArity  */
  YYSYMBOL_genArityNotEmpty = 357,         /* genArityNotEmpty  */
  YYSYMBOL_classDecl = 358,                /* classDecl  */
  YYSYMBOL_fieldDecl = 359,                /* fieldDecl  */
  YYSYMBOL_fieldAttr = 360,                /* fieldAttr  */
  YYSYMBOL_atOpt = 361,                    /* atOpt  */
  YYSYMBOL_initOpt = 362,                  /* initOpt  */
  YYSYMBOL_repeatOpt = 363,                /* repeatOpt  */
  YYSYMBOL_methodRef = 364,                /* methodRef  */
  YYSYMBOL_callConv = 365,                 /* callConv  */
  YYSYMBOL_callKind = 366,                 /* callKind  */
  YYSYMBOL_mdtoken = 367,                  /* mdtoken  */
  YYSYMBOL_memberRef = 368,                /* memberRef  */
  YYSYMBOL_eventHead = 369,                /* eventHead  */
  YYSYMBOL_eventAttr = 370,                /* eventAttr  */
  YYSYMBOL_eventDecls = 371,               /* eventDecls  */
  YYSYMBOL_eventDecl = 372,                /* eventDecl  */
  YYSYMBOL_propHead = 373,                 /* propHead  */
  YYSYMBOL_propAttr = 374,                 /* propAttr  */
  YYSYMBOL_propDecls = 375,                /* propDecls  */
  YYSYMBOL_propDecl = 376,                 /* propDecl  */
  YYSYMBOL_methodHeadPart1 = 377,          /* methodHeadPart1  */
  YYSYMBOL_marshalClause = 378,            /* marshalClause  */
  YYSYMBOL_marshalBlob = 379,              /* marshalBlob  */
  YYSYMBOL_marshalBlobHead = 380,          /* marshalBlobHead  */
  YYSYMBOL_methodHead = 381,               /* methodHead  */
  YYSYMBOL_methAttr = 382,                 /* methAttr  */
  YYSYMBOL_pinvAttr = 383,                 /* pinvAttr  */
  YYSYMBOL_methodName = 384,               /* methodName  */
  YYSYMBOL_paramAttr = 385,                /* paramAttr  */
  YYSYMBOL_implAttr = 386,                 /* implAttr  */
  YYSYMBOL_localsHead = 387,               /* localsHead  */
  YYSYMBOL_methodDecls = 388,              /* methodDecls  */
  YYSYMBOL_methodDecl = 389,               /* methodDecl  */
  YYSYMBOL_scopeBlock = 390,               /* scopeBlock  */
  YYSYMBOL_scopeOpen = 391,                /* scopeOpen  */
  YYSYMBOL_sehBlock = 392,                 /* sehBlock  */
  YYSYMBOL_sehClauses = 393,               /* sehClauses  */
  YYSYMBOL_tryBlock = 394,                 /* tryBlock  */
  YYSYMBOL_tryHead = 395,                  /* tryHead  */
  YYSYMBOL_sehClause = 396,                /* sehClause  */
  YYSYMBOL_filterClause = 397,             /* filterClause  */
  YYSYMBOL_filterHead = 398,               /* filterHead  */
  YYSYMBOL_catchClause = 399,              /* catchClause  */
  YYSYMBOL_finallyClause = 400,            /* finallyClause  */
  YYSYMBOL_faultClause = 401,              /* faultClause  */
  YYSYMBOL_handlerBlock = 402,             /* handlerBlock  */
  YYSYMBOL_dataDecl = 403,                 /* dataDecl  */
  YYSYMBOL_ddHead = 404,                   /* ddHead  */
  YYSYMBOL_tls = 405,                      /* tls  */
  YYSYMBOL_ddBody = 406,                   /* ddBody  */
  YYSYMBOL_ddItemList = 407,               /* ddItemList  */
  YYSYMBOL_ddItemCount = 408,              /* ddItemCount  */
  YYSYMBOL_ddItem = 409,                   /* ddItem  */
  YYSYMBOL_fieldSerInit = 410,             /* fieldSerInit  */
  YYSYMBOL_bytearrayhead = 411,            /* bytearrayhead  */
  YYSYMBOL_bytes = 412,                    /* bytes  */
  YYSYMBOL_hexbytes = 413,                 /* hexbytes  */
  YYSYMBOL_fieldInit = 414,                /* fieldInit  */
  YYSYMBOL_serInit = 415,                  /* serInit  */
  YYSYMBOL_f32seq = 416,                   /* f32seq  */
  YYSYMBOL_f64seq = 417,                   /* f64seq  */
  YYSYMBOL_i64seq = 418,                   /* i64seq  */
  YYSYMBOL_i32seq = 419,                   /* i32seq  */
  YYSYMBOL_i16seq = 420,                   /* i16seq  */
  YYSYMBOL_i8seq = 421,                    /* i8seq  */
  YYSYMBOL_boolSeq = 422,                  /* boolSeq  */
  YYSYMBOL_sqstringSeq = 423,              /* sqstringSeq  */
  YYSYMBOL_classSeq = 424,                 /* classSeq  */
  YYSYMBOL_objSeq = 425,                   /* objSeq  */
  YYSYMBOL_methodSpec = 426,               /* methodSpec  */
  YYSYMBOL_instr_none = 427,               /* instr_none  */
  YYSYMBOL_instr_var = 428,                /* instr_var  */
  YYSYMBOL_instr_i = 429,                  /* instr_i  */
  YYSYMBOL_instr_i8 = 430,                 /* instr_i8  */
  YYSYMBOL_instr_r = 431,                  /* instr_r  */
  YYSYMBOL_instr_brtarget = 432,           /* instr_brtarget  */
  YYSYMBOL_instr_method = 433,             /* instr_method  */
  YYSYMBOL_instr_field = 434,              /* instr_field  */
  YYSYMBOL_instr_type = 435,               /* instr_type  */
  YYSYMBOL_instr_string = 436,             /* instr_string  */
  YYSYMBOL_instr_sig = 437,                /* instr_sig  */
  YYSYMBOL_instr_tok = 438,                /* instr_tok  */
  YYSYMBOL_instr_switch = 439,             /* instr_switch  */
  YYSYMBOL_instr_r_head = 440,             /* instr_r_head  */
  YYSYMBOL_instr = 441,                    /* instr  */
  YYSYMBOL_labels = 442,                   /* labels  */
  YYSYMBOL_tyArgs0 = 443,                  /* tyArgs0  */
  YYSYMBOL_tyArgs1 = 444,                  /* tyArgs1  */
  YYSYMBOL_tyArgs2 = 445,                  /* tyArgs2  */
  YYSYMBOL_sigArgs0 = 446,                 /* sigArgs0  */
  YYSYMBOL_sigArgs1 = 447,                 /* sigArgs1  */
  YYSYMBOL_sigArg = 448,                   /* sigArg  */
  YYSYMBOL_className = 449,                /* className  */
  YYSYMBOL_slashedName = 450,              /* slashedName  */
  YYSYMBOL_typeSpec = 451,                 /* typeSpec  */
  YYSYMBOL_nativeType = 452,               /* nativeType  */
  YYSYMBOL_iidParamIndex = 453,            /* iidParamIndex  */
  YYSYMBOL_variantType = 454,              /* variantType  */
  YYSYMBOL_type = 455,                     /* type  */
  YYSYMBOL_simpleType = 456,               /* simpleType  */
  YYSYMBOL_bounds1 = 457,                  /* bounds1  */
  YYSYMBOL_bound = 458,                    /* bound  */
  YYSYMBOL_secDecl = 459,                  /* secDecl  */
  YYSYMBOL_secAttrSetBlob = 460,           /* secAttrSetBlob  */
  YYSYMBOL_secAttrBlob = 461,              /* secAttrBlob  */
  YYSYMBOL_psetHead = 462,                 /* psetHead  */
  YYSYMBOL_nameValPairs = 463,             /* nameValPairs  */
  YYSYMBOL_nameValPair = 464,              /* nameValPair  */
  YYSYMBOL_truefalse = 465,                /* truefalse  */
  YYSYMBOL_caValue = 466,                  /* caValue  */
  YYSYMBOL_secAction = 467,                /* secAction  */
  YYSYMBOL_esHead = 468,                   /* esHead  */
  YYSYMBOL_extSourceSpec = 469,            /* extSourceSpec  */
  YYSYMBOL_fileDecl = 470,                 /* fileDecl  */
  YYSYMBOL_fileAttr = 471,                 /* fileAttr  */
  YYSYMBOL_fileEntry = 472,                /* fileEntry  */
  YYSYMBOL_hashHead = 473,                 /* hashHead  */
  YYSYMBOL_assemblyHead = 474,             /* assemblyHead  */
  YYSYMBOL_asmAttr = 475,                  /* asmAttr  */
  YYSYMBOL_assemblyDecls = 476,            /* assemblyDecls  */
  YYSYMBOL_assemblyDecl = 477,             /* assemblyDecl  */
  YYSYMBOL_intOrWildcard = 478,            /* intOrWildcard  */
  YYSYMBOL_asmOrRefDecl = 479,             /* asmOrRefDecl  */
  YYSYMBOL_publicKeyHead = 480,            /* publicKeyHead  */
  YYSYMBOL_publicKeyTokenHead = 481,       /* publicKeyTokenHead  */
  YYSYMBOL_localeHead = 482,               /* localeHead  */
  YYSYMBOL_assemblyRefHead = 483,          /* assemblyRefHead  */
  YYSYMBOL_assemblyRefDecls = 484,         /* assemblyRefDecls  */
  YYSYMBOL_assemblyRefDecl = 485,          /* assemblyRefDecl  */
  YYSYMBOL_exptypeHead = 486,              /* exptypeHead  */
  YYSYMBOL_exportHead = 487,               /* exportHead  */
  YYSYMBOL_exptAttr = 488,                 /* exptAttr  */
  YYSYMBOL_exptypeDecls = 489,             /* exptypeDecls  */
  YYSYMBOL_exptypeDecl = 490,              /* exptypeDecl  */
  YYSYMBOL_manifestResHead = 491,          /* manifestResHead  */
  YYSYMBOL_manresAttr = 492,               /* manresAttr  */
  YYSYMBOL_manifestResDecls = 493,         /* manifestResDecls  */
  YYSYMBOL_manifestResDecl = 494           /* manifestResDecl  */
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
#define YYLAST   3765

/* YYNTOKENS -- Number of terminals.  */
#define YYNTOKENS  309
/* YYNNTS -- Number of nonterminals.  */
#define YYNNTS  186
/* YYNRULES -- Number of rules.  */
#define YYNRULES  847
/* YYNSTATES -- Number of states.  */
#define YYNSTATES  1591

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
       0,   189,   189,   190,   193,   194,   195,   199,   200,   201,
     202,   203,   204,   205,   206,   207,   208,   209,   210,   211,
     212,   215,   216,   219,   222,   223,   224,   225,   226,   227,
     230,   231,   234,   235,   238,   239,   241,   246,   247,   250,
     251,   252,   255,   258,   259,   262,   263,   264,   268,   269,
     270,   271,   272,   277,   278,   279,   280,   283,   286,   287,
     291,   292,   296,   297,   298,   299,   302,   303,   304,   306,
     309,   312,   318,   321,   322,   326,   332,   333,   335,   338,
     339,   345,   348,   349,   352,   356,   357,   365,   366,   367,
     368,   370,   372,   377,   378,   379,   386,   390,   391,   392,
     393,   394,   395,   398,   401,   405,   408,   411,   417,   420,
     421,   422,   423,   424,   425,   426,   427,   428,   429,   430,
     431,   432,   433,   434,   435,   436,   437,   438,   439,   440,
     441,   442,   443,   444,   445,   446,   449,   450,   453,   454,
     457,   458,   461,   462,   466,   467,   470,   471,   474,   475,
     478,   479,   480,   481,   482,   483,   484,   487,   488,   491,
     492,   495,   496,   499,   502,   503,   506,   510,   514,   515,
     516,   517,   518,   519,   520,   521,   522,   523,   524,   525,
     531,   540,   541,   542,   547,   553,   554,   555,   562,   567,
     568,   569,   570,   571,   572,   573,   574,   586,   588,   589,
     590,   591,   592,   593,   594,   597,   598,   601,   602,   605,
     606,   610,   627,   633,   649,   654,   655,   656,   659,   660,
     661,   662,   665,   666,   667,   668,   669,   670,   671,   672,
     675,   678,   683,   687,   691,   693,   695,   700,   701,   705,
     706,   707,   710,   711,   714,   715,   716,   717,   718,   719,
     720,   721,   725,   731,   732,   733,   736,   737,   741,   742,
     743,   744,   745,   746,   747,   751,   757,   758,   761,   762,
     765,   768,   784,   785,   786,   787,   788,   789,   790,   791,
     792,   793,   794,   795,   796,   797,   798,   799,   800,   801,
     802,   803,   804,   807,   810,   815,   816,   817,   818,   819,
     820,   821,   822,   823,   824,   825,   826,   827,   828,   829,
     830,   833,   834,   835,   838,   839,   840,   841,   842,   845,
     846,   847,   848,   849,   850,   851,   852,   853,   854,   855,
     856,   857,   858,   859,   860,   861,   864,   868,   869,   872,
     873,   874,   875,   877,   880,   881,   882,   883,   884,   885,
     886,   887,   888,   889,   890,   900,   910,   912,   915,   922,
     923,   928,   934,   935,   937,   958,   961,   965,   968,   969,
     972,   973,   974,   978,   983,   984,   985,   986,   990,   991,
     993,   997,  1001,  1006,  1010,  1014,  1015,  1016,  1021,  1024,
    1025,  1028,  1029,  1030,  1033,  1034,  1037,  1038,  1041,  1042,
    1047,  1048,  1049,  1050,  1057,  1064,  1071,  1078,  1086,  1094,
    1095,  1096,  1097,  1098,  1099,  1103,  1106,  1108,  1110,  1112,
    1114,  1116,  1118,  1120,  1122,  1124,  1126,  1128,  1130,  1132,
    1134,  1136,  1138,  1140,  1144,  1147,  1148,  1151,  1152,  1156,
    1157,  1158,  1163,  1164,  1165,  1167,  1169,  1171,  1172,  1173,
    1177,  1181,  1185,  1189,  1193,  1197,  1201,  1205,  1209,  1213,
    1217,  1221,  1225,  1229,  1233,  1237,  1241,  1245,  1252,  1253,
    1255,  1259,  1260,  1262,  1266,  1267,  1271,  1272,  1275,  1276,
    1279,  1280,  1283,  1284,  1288,  1289,  1290,  1294,  1295,  1296,
    1298,  1302,  1303,  1307,  1313,  1316,  1319,  1322,  1325,  1328,
    1331,  1339,  1342,  1345,  1348,  1351,  1354,  1357,  1361,  1362,
    1363,  1364,  1365,  1366,  1367,  1368,  1377,  1378,  1379,  1386,
    1394,  1402,  1408,  1414,  1420,  1424,  1425,  1427,  1429,  1433,
    1439,  1442,  1443,  1444,  1445,  1446,  1450,  1451,  1454,  1455,
    1458,  1459,  1463,  1464,  1467,  1468,  1471,  1472,  1473,  1477,
    1478,  1479,  1480,  1481,  1482,  1483,  1484,  1487,  1493,  1500,
    1501,  1504,  1505,  1506,  1507,  1511,  1512,  1519,  1525,  1527,
    1530,  1532,  1533,  1535,  1537,  1538,  1539,  1540,  1541,  1542,
    1543,  1544,  1545,  1546,  1547,  1548,  1549,  1550,  1551,  1552,
    1553,  1555,  1557,  1562,  1567,  1570,  1572,  1574,  1575,  1576,
    1577,  1578,  1580,  1582,  1584,  1585,  1587,  1590,  1594,  1595,
    1596,  1597,  1599,  1600,  1601,  1602,  1603,  1604,  1605,  1606,
    1609,  1610,  1613,  1614,  1615,  1616,  1617,  1618,  1619,  1620,
    1621,  1622,  1623,  1624,  1625,  1626,  1627,  1628,  1629,  1630,
    1631,  1632,  1633,  1634,  1635,  1636,  1637,  1638,  1639,  1640,
    1641,  1642,  1643,  1644,  1645,  1646,  1647,  1648,  1649,  1650,
    1651,  1652,  1653,  1654,  1655,  1656,  1657,  1658,  1659,  1660,
    1661,  1665,  1671,  1672,  1673,  1674,  1675,  1676,  1677,  1678,
    1679,  1681,  1683,  1690,  1697,  1703,  1709,  1724,  1739,  1740,
    1741,  1742,  1743,  1744,  1745,  1748,  1749,  1750,  1751,  1752,
    1753,  1754,  1755,  1756,  1757,  1758,  1759,  1760,  1761,  1762,
    1763,  1764,  1765,  1768,  1769,  1772,  1773,  1774,  1775,  1778,
    1782,  1784,  1786,  1787,  1788,  1790,  1799,  1800,  1801,  1804,
    1807,  1812,  1813,  1817,  1818,  1821,  1824,  1825,  1828,  1831,
    1834,  1837,  1841,  1847,  1853,  1859,  1867,  1868,  1869,  1870,
    1871,  1872,  1873,  1874,  1875,  1876,  1877,  1878,  1879,  1880,
    1881,  1885,  1886,  1889,  1892,  1894,  1897,  1899,  1903,  1906,
    1910,  1913,  1917,  1920,  1926,  1928,  1931,  1932,  1935,  1936,
    1939,  1942,  1945,  1946,  1947,  1948,  1949,  1950,  1951,  1952,
    1953,  1954,  1957,  1958,  1961,  1962,  1963,  1966,  1967,  1970,
    1971,  1973,  1974,  1975,  1976,  1979,  1982,  1985,  1988,  1990,
    1994,  1995,  1998,  1999,  2000,  2001,  2004,  2007,  2010,  2011,
    2012,  2013,  2014,  2015,  2016,  2017,  2018,  2019,  2022,  2023,
    2026,  2027,  2028,  2029,  2031,  2033,  2034,  2037,  2038,  2042,
    2043,  2044,  2047,  2048,  2051,  2052,  2053,  2054
};
#endif

/** Accessing symbol of state STATE.  */
#define YY_ACCESSING_SYMBOL(State) YY_CAST (yysymbol_kind_t, yystos[State])

#if YYDEBUG || 0
/* The user-facing name of the symbol whose (internal) number is
   YYSYMBOL.  No bounds checking.  */
static const char *yysymbol_name (yysymbol_kind_t yysymbol) YY_ATTRIBUTE_UNUSED;

/* YYTNAME[SYMBOL-NUM] -- String name of the symbol SYMBOL-NUM.
   First, the terminals, then, starting at YYNTOKENS, nonterminals.  */
static const char *const yytname[] =
{
  "\"end of file\"", "error", "\"invalid token\"", "BAD_COMMENT_",
  "BAD_LITERAL_", "ID", "DOTTEDNAME", "QSTRING", "SQSTRING", "INT32_V",
  "INT64_V", "FLOAT64", "HEXBYTE", "TYPEDEF_T", "TYPEDEF_M", "TYPEDEF_F",
  "TYPEDEF_TS", "TYPEDEF_MR", "TYPEDEF_CA", "DCOLON", "ELLIPSIS", "VOID_",
  "BOOL_", "CHAR_", "UNSIGNED_", "INT_", "INT8_", "INT16_", "INT32_",
  "INT64_", "FLOAT_", "FLOAT32_", "FLOAT64_", "BYTEARRAY_", "UINT_",
  "UINT8_", "UINT16_", "UINT32_", "UINT64_", "FLAGS_", "CALLCONV_",
  "MDTOKEN_", "OBJECT_", "STRING_", "NULLREF_", "DEFAULT_", "CDECL_",
  "VARARG_", "STDCALL_", "THISCALL_", "FASTCALL_", "CLASS_", "BYREFLIKE_",
  "TYPEDREF_", "UNMANAGED_", "FINALLY_", "HANDLER_", "CATCH_", "FILTER_",
  "FAULT_", "EXTENDS_", "IMPLEMENTS_", "TO_", "AT_", "TLS_", "TRUE_",
  "FALSE_", "_INTERFACEIMPL", "VALUE_", "VALUETYPE_", "NATIVE_",
  "INSTANCE_", "SPECIALNAME_", "FORWARDER_", "STATIC_", "PUBLIC_",
  "PRIVATE_", "FAMILY_", "FINAL_", "SYNCHRONIZED_", "INTERFACE_",
  "SEALED_", "NESTED_", "ABSTRACT_", "AUTO_", "SEQUENTIAL_", "EXPLICIT_",
  "ANSI_", "UNICODE_", "AUTOCHAR_", "IMPORT_", "ENUM_", "VIRTUAL_",
  "NOINLINING_", "AGGRESSIVEINLINING_", "NOOPTIMIZATION_",
  "AGGRESSIVEOPTIMIZATION_", "ASYNC_", "UNMANAGEDEXP_", "BEFOREFIELDINIT_",
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
  "NULL_", "ERROR_", "HRESULT_", "CARRAY_", "USERDEFINED_", "RECORD_",
  "FILETIME_", "BLOB_", "STREAM_", "STORAGE_", "STREAMED_OBJECT_",
  "STORED_OBJECT_", "BLOB_OBJECT_", "CF_", "CLSID_", "VECTOR_",
  "_SUBSYSTEM", "_CORFLAGS", "ALIGNMENT_", "_IMAGEBASE", "_STACKRESERVE",
  "_TYPEDEF", "_TEMPLATE", "_TYPELIST", "_MSCORLIB", "P_DEFINE", "P_UNDEF",
  "P_IFDEF", "P_IFNDEF", "P_ELSE", "P_ENDIF", "P_INCLUDE", "CONSTRAINT_",
  "'{'", "'}'", "'+'", "','", "'.'", "'('", "')'", "';'", "'='", "'['",
  "']'", "'<'", "'>'", "'-'", "':'", "'*'", "'&'", "'/'", "'!'", "$accept",
  "decls", "decl", "classNameSeq", "compQstring", "languageDecl", "id",
  "dottedName", "int32", "int64", "float64", "typedefDecl", "compControl",
  "customDescr", "customDescrWithOwner", "customHead",
  "customHeadWithOwner", "customType", "ownerType", "customBlobDescr",
  "customBlobArgs", "customBlobNVPairs", "fieldOrProp", "customAttrDecl",
  "serializType", "moduleHead", "vtfixupDecl", "vtfixupAttr", "vtableDecl",
  "vtableHead", "nameSpaceHead", "_class", "classHeadBegin", "classHead",
  "classAttr", "extendsClause", "implClause", "classDecls", "implList",
  "typeList", "typeListNotEmpty", "typarsClause", "typarAttrib",
  "typarAttribs", "typars", "typarsRest", "tyBound", "genArity",
  "genArityNotEmpty", "classDecl", "fieldDecl", "fieldAttr", "atOpt",
  "initOpt", "repeatOpt", "methodRef", "callConv", "callKind", "mdtoken",
  "memberRef", "eventHead", "eventAttr", "eventDecls", "eventDecl",
  "propHead", "propAttr", "propDecls", "propDecl", "methodHeadPart1",
  "marshalClause", "marshalBlob", "marshalBlobHead", "methodHead",
  "methAttr", "pinvAttr", "methodName", "paramAttr", "implAttr",
  "localsHead", "methodDecls", "methodDecl", "scopeBlock", "scopeOpen",
  "sehBlock", "sehClauses", "tryBlock", "tryHead", "sehClause",
  "filterClause", "filterHead", "catchClause", "finallyClause",
  "faultClause", "handlerBlock", "dataDecl", "ddHead", "tls", "ddBody",
  "ddItemList", "ddItemCount", "ddItem", "fieldSerInit", "bytearrayhead",
  "bytes", "hexbytes", "fieldInit", "serInit", "f32seq", "f64seq",
  "i64seq", "i32seq", "i16seq", "i8seq", "boolSeq", "sqstringSeq",
  "classSeq", "objSeq", "methodSpec", "instr_none", "instr_var", "instr_i",
  "instr_i8", "instr_r", "instr_brtarget", "instr_method", "instr_field",
  "instr_type", "instr_string", "instr_sig", "instr_tok", "instr_switch",
  "instr_r_head", "instr", "labels", "tyArgs0", "tyArgs1", "tyArgs2",
  "sigArgs0", "sigArgs1", "sigArg", "className", "slashedName", "typeSpec",
  "nativeType", "iidParamIndex", "variantType", "type", "simpleType",
  "bounds1", "bound", "secDecl", "secAttrSetBlob", "secAttrBlob",
  "psetHead", "nameValPairs", "nameValPair", "truefalse", "caValue",
  "secAction", "esHead", "extSourceSpec", "fileDecl", "fileAttr",
  "fileEntry", "hashHead", "assemblyHead", "asmAttr", "assemblyDecls",
  "assemblyDecl", "intOrWildcard", "asmOrRefDecl", "publicKeyHead",
  "publicKeyTokenHead", "localeHead", "assemblyRefHead",
  "assemblyRefDecls", "assemblyRefDecl", "exptypeHead", "exportHead",
  "exptAttr", "exptypeDecls", "exptypeDecl", "manifestResHead",
  "manresAttr", "manifestResDecls", "manifestResDecl", YY_NULLPTR
};

static const char *
yysymbol_name (yysymbol_kind_t yysymbol)
{
  return yytname[yysymbol];
}
#endif

#define YYPACT_NINF (-1352)

#define yypact_value_is_default(Yyn) \
  ((Yyn) == YYPACT_NINF)

#define YYTABLE_NINF (-560)

#define yytable_value_is_error(Yyn) \
  0

/* YYPACT[STATE-NUM] -- Index in YYTABLE of the portion describing
   STATE-NUM.  */
static const yytype_int16 yypact[] =
{
   -1352,  2148, -1352, -1352,   -95,   727, -1352,  -143,   135,  3093,
    3093, -1352, -1352,   202,   756,  -126,   -59,    -9,    62, -1352,
     323,   284,   284,   501,   501,  1698,     5, -1352,   727,   727,
     727,   727, -1352, -1352,   332, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352,   341,   341, -1352, -1352, -1352, -1352,   341,    68,
   -1352,   328,   185, -1352, -1352, -1352, -1352,   564, -1352,   341,
     284, -1352, -1352,   195,   200,   235,   246, -1352, -1352, -1352,
   -1352, -1352,   118,   284, -1352, -1352, -1352,   536, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352,  2002,    61,   140, -1352, -1352,   152,   266,
   -1352, -1352,   631,  1125,  1125,  1900,   194, -1352,  3016, -1352,
   -1352,   268,   284,   284,   301, -1352,   681,   524,   727,   118,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,  3016,
   -1352, -1352, -1352,  1149, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352, -1352,    58, -1352,   551,    58,
     489, -1352,  2426, -1352, -1352, -1352,    50,    37,   118,   452,
     468, -1352,   470,  2276,   473,   314,   538, -1352,    58,    93,
     118,   118,   118, -1352, -1352,   335,   627,   365,   377, -1352,
    2509,  2002,   635, -1352,  3521,  2359,   383,   143,   282,   299,
     329,   353,   505,   395,   720,   406, -1352, -1352,   341,   408,
      40, -1352, -1352, -1352, -1352,   803,   727,   420,  2801,   443,
      74, -1352,  1125, -1352,   274,   587, -1352,   455,   -54,   479,
     766,   284,   284, -1352, -1352, -1352, -1352, -1352, -1352,   491,
   -1352, -1352,    47,  1289, -1352,   485, -1352, -1352,   -17,   681,
   -1352, -1352, -1352, -1352,   578, -1352, -1352, -1352, -1352,   118,
   -1352, -1352,   -29,   118,   587, -1352, -1352, -1352, -1352, -1352,
      58, -1352,   798, -1352, -1352, -1352, -1352,  1593,   727,   541,
     224,   550,   771,   118, -1352,   727,   727,   727, -1352,  3016,
     727,   727, -1352,   543,   570,   727,    83,  3016, -1352, -1352,
     568,    58,   479, -1352, -1352, -1352, -1352,  2955,   585, -1352,
   -1352, -1352, -1352, -1352, -1352,   875, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,  -108,
   -1352,  2002, -1352,  3186,   594, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352,   617, -1352, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,   284, -1352,
     284, -1352, -1352, -1352,   284,   618,   206,  2061, -1352, -1352,
   -1352,   614, -1352, -1352,   -44, -1352, -1352, -1352, -1352,   654,
     207, -1352, -1352,   722,   284,   501,   290,   722,  2276,   996,
    2002,   197,  1125,  1900,   633,   341, -1352, -1352, -1352,   700,
     284,   284, -1352,   284, -1352,   284, -1352,   501, -1352,   467,
   -1352,   467, -1352, -1352,   586,   637,   536,   653, -1352, -1352,
   -1352,   284,   284,  1156,  1219,   475,   959, -1352, -1352, -1352,
     928,   118,   118, -1352,   701, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,   702,    90,
   -1352,   727,   249,  3016,   969,   692, -1352,  2174, -1352,  1000,
     715,   719,   725,  2276, -1352, -1352,   479, -1352, -1352,   183,
     122,   728,  1011, -1352, -1352,   826,    53, -1352,   727, -1352,
   -1352,   122,  1017,   257,   727,   727,   727,   118, -1352,   118,
     118,   118,  1534,   118,   118,  2002,  2002,   118, -1352, -1352,
    1023,   -43, -1352,   743,   753,   587, -1352, -1352, -1352,   284,
   -1352, -1352, -1352, -1352, -1352, -1352,   234, -1352,   760, -1352,
     949, -1352, -1352, -1352,   284,   284, -1352,    38,  2244, -1352,
   -1352, -1352, -1352,   779, -1352, -1352,   809,   811, -1352, -1352,
   -1352, -1352,   843,   284,   969,  2906, -1352, -1352,   758,   284,
      44,    99,   284,  1125,  1111, -1352,   844,   127,  2523, -1352,
    2002, -1352, -1352, -1352,   654,    87,   207,    87,    87,    87,
    1078,  1087, -1352, -1352, -1352, -1352, -1352, -1352,   855,   859,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,  1593,
   -1352,   860,   479,   341,  3016, -1352,   722,   861,   969,   866,
     863,   871,   872,   884,   897,   898, -1352,   720,   901, -1352,
     906,    80,   986,   910,    27,    77, -1352, -1352, -1352, -1352,
   -1352, -1352,   341,   341, -1352,   914,   916, -1352,   341, -1352,
     341, -1352,   907,    72,   727,  1002, -1352, -1352, -1352, -1352,
     727,  1003, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352,   284,  3468,   115,   258,   727,   609,   -39,   931,
     941, -1352,   201,   942,   945,   946, -1352,  1243, -1352, -1352,
     955,   963,  3160,  2961,   960,   968,   713,   777,   341,   727,
     118,   727,   727,   314,   314,   314,   971,   973,   981,   284,
     487, -1352, -1352,  3016,   982,   972, -1352, -1352, -1352, -1352,
   -1352, -1352,   234,    81,   978,  2002,  2002,  1757,   493, -1352,
   -1352,   803,   120,   125,  1125,  1262, -1352, -1352, -1352,  2607,
   -1352,   987,    43,  1823,   251,   519,   284,   990,   284,   118,
     284,   289,   991,  3016,   713,   127, -1352,  2906,   992,   989,
   -1352, -1352, -1352, -1352,   722, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352,   536,   284,   284,   501,   122,  1267,   969,
     997,   749,   994,   999,  1004, -1352,    20,  1007, -1352,  1007,
    1007,  1007,  1007,  1007, -1352, -1352,   284, -1352,   284,   284,
    1001, -1352, -1352,   995,  1006,   479,  1012,  1018,  1028,  1033,
    1039,  1040,   284,   727, -1352,   118,   727,   145,   727,  1041,
   -1352, -1352, -1352,   820, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352, -1352,  1034,  1055,  1072, -1352,
    1091,  1044,    26,  1319, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352,  1034,  1034, -1352,  3068, -1352, -1352,
   -1352, -1352,  1047,   341,   157,   536,  1048,   727,   129, -1352,
     969,  1056,  1050,  1059, -1352,  2174, -1352,    70, -1352,   517,
     533,  1178,   596,   612,   640,   646,   649,   663,   666,   668,
     669,   699,   757,   764,   772, -1352,   381, -1352,   341, -1352,
     284,  1043,   127,   127,   118,   728, -1352, -1352,   536, -1352,
   -1352, -1352,  1058,   118,   118,   314,   127, -1352, -1352, -1352,
   -1352,   587, -1352,   284, -1352,  2002,   366,   727, -1352, -1352,
    1153, -1352, -1352,   778,   727, -1352, -1352,  3016,   118,   284,
     118,   284,   432,  3016,   713,  3233,   988,   687, -1352,   938,
   -1352,   969,  2096,  1057, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352,  1051,  1052, -1352,  1060,  1061,  1062,
    1064,  1071,   713, -1352,  1231,  1069,  1073,  2002,  1048,  1593,
   -1352,  1076,   519, -1352,  1362,  1321,  1323, -1352, -1352,  1092,
    1095,   727,   783, -1352,   127,   722,   722, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352,   106,  1379, -1352, -1352,    27,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352,  1096,   314,   118,
     284,   118, -1352, -1352, -1352, -1352, -1352, -1352,  1139, -1352,
   -1352, -1352, -1352,   969,  1099,  1100, -1352, -1352, -1352, -1352,
   -1352,   895, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,
     222, -1352,    45,    92, -1352, -1352,  1054, -1352,  1105, -1352,
   -1352,   479, -1352,  1106, -1352, -1352, -1352, -1352,  1104, -1352,
   -1352, -1352, -1352,   479,   690,   284,   284,   284,   775,   789,
     813,   825,   284,   284,   284,   284,   284,   284,   501,   284,
     577,   284,   804,   284,   284,   284,   284,   284,   284,   284,
     501,   284,  1645,   284,   205,   284,   476,   284, -1352, -1352,
   -1352,  1957,  1108,  1107, -1352,  1109,  1110,  1113,  1116, -1352,
    1245,  1117,  1118,  1119,  1124, -1352,   234, -1352,   366,  2276,
   -1352,   118,    90,  1120,  1121,  2002,  1593,  1162, -1352,  2276,
    2276,  2276,  2276, -1352, -1352, -1352, -1352, -1352, -1352,  2276,
    2276,  2276, -1352, -1352, -1352, -1352, -1352, -1352, -1352,   479,
   -1352,   284,   629,   682, -1352, -1352, -1352, -1352,  3468,  1122,
     536, -1352,  1126, -1352, -1352,  1403, -1352,   536, -1352,   536,
     284, -1352, -1352,   118, -1352,  1128, -1352, -1352, -1352,   284,
   -1352,  1123, -1352, -1352,  1127,   560,   284,   284, -1352, -1352,
   -1352, -1352, -1352, -1352,   969,  1129, -1352, -1352,   284, -1352,
      86,  1131,  1137,  1204,  1150,  1151,  1157,  1167,  1171,  1176,
    1181,  1182,  1184,  1185, -1352,   479, -1352, -1352,   284,   606,
   -1352,   904,  1166,  1152,  1173,  1186,  1183,   284,   284,   284,
     284,   284,   284,   501,   284,  1188,  1189,  1190,  1191,  1192,
    1197,  1194,  1208,  1213,  1215,  1212,  1217,  1218,  1224,  1221,
    1225,  1222,  1226,  1223,  1227,  1232,  1229,  1234,  1233,  1235,
    1236,  1238,  1239,  1473,  1240,  1241, -1352,   562, -1352,   158,
   -1352, -1352,  1242, -1352, -1352,   127,   127, -1352, -1352, -1352,
   -1352,  2002, -1352, -1352,   573, -1352,  1252, -1352,  1496,  1125,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352,  2250,  1250, -1352,
   -1352, -1352, -1352,  1253,  1256, -1352,  2002,   713, -1352, -1352,
   -1352, -1352,  1526,    27,   284,   969,  1248,  1264,   479, -1352,
    1268,   284, -1352,  1257,  1258,  1272,  1278,  1282,  1270,  1275,
    1277,  1286,  1093, -1352, -1352, -1352,  1288, -1352,  1291,  1293,
    1292,  1295,  1296,  1297,  1300,  1309,  1307, -1352,  1313, -1352,
    1315, -1352,  1316, -1352,  1317, -1352, -1352,  1328, -1352, -1352,
    1331, -1352,  1332, -1352,  1337, -1352,  1338, -1352,  1342, -1352,
    1344, -1352, -1352,  1348, -1352,  1322, -1352,  1350,  1639, -1352,
    1349,   828, -1352,  1352,  1354, -1352,   127,  2002,   713,  3016,
   -1352, -1352, -1352,   127, -1352,  1351, -1352,  1347,  1357,   502,
   -1352,  3507, -1352,  1356, -1352,   284,   284,   284, -1352, -1352,
   -1352, -1352, -1352,  1363, -1352,  1364, -1352,  1365, -1352,  1369,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352, -1352,  1645, -1352, -1352,  1361,
   -1352,  1351,  1593,  1370,  1366,  1375, -1352,    27, -1352,   969,
   -1352,   157, -1352,  1383,  1388,  1389,   180,   107, -1352, -1352,
   -1352, -1352,   153,   162,   164,   110,   216,   212,   167,   172,
     173,   130,  1432,   124,   305, -1352,  1048,  1380,  1667, -1352,
     127, -1352,   582, -1352, -1352, -1352, -1352, -1352, -1352, -1352,
     174,   175,   178,   149, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352,  1681, -1352, -1352,
   -1352,   127,   713,  1751,  1394,   969, -1352, -1352, -1352, -1352,
   -1352,  1395,  1397,  1398, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352,   840,  1442,   127,   284, -1352,  1598,  1409,  1411,  1125,
   -1352, -1352,  3016,  1593,  1689,   713,  1351,  1414,   127,  1416,
   -1352
};

/* YYDEFACT[STATE-NUM] -- Default reduction number in state STATE-NUM.
   Performed when YYTABLE does not specify something else to do.  Zero
   means the default is an error.  */
static const yytype_int16 yydefact[] =
{
       2,     0,     1,    86,   106,     0,   265,   209,   391,     0,
       0,   761,   762,     0,   222,     0,     0,   776,   782,   839,
      93,     0,     0,     0,     0,     0,     0,    29,     0,     0,
       0,     0,    58,    59,     0,    61,     3,    25,    26,    27,
      84,    85,   435,   435,    19,    17,    10,     9,   435,     0,
     109,   136,     0,     7,   272,   337,     8,     0,    18,   435,
       0,    11,    12,     0,     0,     0,     0,   818,    37,    40,
      38,    39,   105,     0,   189,   392,   393,   390,   746,   747,
     748,   749,   750,   751,   752,   753,   754,   755,   756,   757,
     758,   759,   760,     0,     0,    34,   216,   217,     0,     0,
     223,   224,   229,   222,   222,     0,    62,    72,     0,   220,
     215,     0,     0,     0,     0,   782,     0,     0,     0,    94,
      42,    20,    21,    44,    43,    23,    24,   555,   712,     0,
     689,   697,   695,     0,   698,   699,   700,   701,   702,   703,
     708,   709,   710,   711,   672,   696,     0,   688,     0,     0,
       0,   493,     0,   556,   557,   558,     0,     0,   559,     0,
       0,   236,     0,   222,     0,   553,     0,   693,    30,    53,
      55,    56,    57,    60,   437,     0,   436,     0,     0,     2,
       0,     0,   138,   140,   222,     0,     0,   398,   398,   398,
     398,   398,   398,     0,     0,     0,   388,   395,   435,     0,
     764,   792,   810,   828,   842,     0,     0,     0,     0,     0,
       0,   554,   222,   561,   722,   564,    32,     0,     0,   724,
       0,     0,     0,   225,   226,   227,   228,   218,   219,     0,
      74,    73,     0,     0,   104,     0,    22,   777,   778,     0,
     783,   784,   785,   787,     0,   788,   789,   790,   791,   781,
     840,   841,   837,    95,   694,   704,   705,   706,   707,   671,
       0,   674,     0,   690,   692,   234,   235,     0,     0,     0,
       0,     0,     0,   687,   685,     0,     0,     0,   231,     0,
       0,     0,   679,     0,     0,     0,   715,   538,   678,   677,
       0,    30,    54,    65,   438,    69,   103,     0,     0,   112,
     133,   110,   111,   114,   115,     0,   116,   117,   118,   119,
     120,   121,   122,   123,   113,   132,   125,   124,   134,   148,
     137,     0,   108,     0,     0,   278,   273,   274,   275,   276,
     277,   281,   279,   289,   280,   282,   283,   284,   285,   286,
     287,   288,     0,   290,   314,   494,   495,   496,   497,   498,
     499,   500,   501,   502,   503,   504,   505,   506,     0,   373,
       0,   336,   344,   345,     0,     0,     0,     0,   366,     6,
     351,     0,   353,   352,     0,   338,   359,   337,   340,     0,
       0,   346,   508,     0,     0,     0,     0,     0,   222,     0,
       0,     0,   222,     0,     0,   435,   347,   349,   350,     0,
       0,     0,   414,     0,   413,     0,   412,     0,   411,     0,
     409,     0,   410,   434,     0,   397,     0,     0,   723,   773,
     763,     0,     0,     0,     0,     0,     0,   821,   820,   819,
       0,   816,    41,   210,     0,   196,   190,   191,   192,   193,
     198,   199,   200,   201,   195,   202,   203,   194,     0,     0,
     389,     0,     0,     0,     0,     0,   732,   726,   731,     0,
      35,     0,     0,   222,    76,    70,    63,   311,   312,   715,
     313,   536,     0,    97,   779,   775,   808,   786,     0,   673,
     691,   233,     0,     0,     0,     0,     0,   686,   684,    51,
      52,    50,     0,    49,   560,     0,     0,    48,   716,   675,
     717,     0,   713,     0,   539,   540,    28,    31,     5,     0,
     126,   127,   128,   129,   130,   131,   157,   107,   139,   143,
       0,   106,   239,   253,     0,     0,   818,     0,     0,     4,
     181,   182,   175,     0,   141,   171,     0,     0,   337,   172,
     173,   174,     0,     0,   295,     0,   339,   341,     0,     0,
       0,     0,     0,   222,     0,   348,     0,   314,     0,   383,
       0,   381,   384,   367,   369,     0,     0,     0,     0,     0,
       0,     0,   370,   510,   509,   511,   512,    45,     0,     0,
     507,   514,   513,   517,   516,   518,   522,   523,   521,     0,
     524,     0,   525,   435,     0,   529,   531,     0,     0,     0,
       0,     0,     0,     0,     0,     0,   394,     0,     0,   402,
       0,   766,     0,     0,     0,     0,    13,   804,   803,   795,
     793,   796,   435,   435,   815,     0,     0,    14,   435,   813,
     435,   811,     0,     0,     0,     0,    15,   836,   835,   829,
       0,     0,    16,   847,   846,   843,   822,   823,   824,   825,
     826,   827,     0,   565,   205,     0,   562,     0,     0,     0,
     733,    76,     0,     0,     0,   727,    33,     0,   221,   230,
      66,     0,    79,   538,     0,     0,     0,     0,   435,     0,
     838,     0,     0,   551,   549,   550,   678,     0,     0,   719,
     715,   676,   683,     0,     0,     0,   152,   154,   153,   155,
     150,   151,   157,     0,     0,     0,     0,     0,   222,   176,
     177,     0,     0,     0,   222,     0,   140,   242,   256,     0,
     828,     0,   295,     0,     0,   266,     0,     0,     0,   361,
       0,     0,     0,     0,     0,   314,   546,     0,     0,   543,
     544,   365,   382,   368,     0,   385,   375,   379,   380,   378,
     374,   376,   377,     0,     0,     0,     0,   520,     0,     0,
       0,     0,   534,   535,     0,   515,     0,   398,   399,   398,
     398,   398,   398,   398,   396,   401,     0,   765,     0,     0,
       0,   798,   797,     0,     0,   801,     0,     0,     0,     0,
       0,     0,     0,     0,   834,   830,     0,     0,     0,     0,
     619,   573,   574,     0,   608,   575,   576,   577,   578,   579,
     580,   610,   586,   587,   588,   589,   620,     0,     0,   616,
       0,     0,     0,   570,   571,   572,   595,   596,   597,   614,
     598,   599,   600,   601,   620,   620,   604,   622,   612,   618,
     581,   270,     0,     0,   268,     0,   207,   563,     0,   720,
       0,     0,    38,     0,   725,   726,    36,     0,    64,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,    78,    75,   442,   435,    77,
       0,     0,   314,   314,   313,   536,    98,    99,     0,   100,
     101,   102,     0,   809,   232,   552,   314,   680,   681,   718,
     714,   541,   135,     0,   158,   144,   161,     0,   149,   142,
       0,   241,   240,   559,     0,   255,   254,     0,   817,     0,
     184,     0,     0,     0,     0,     0,     0,     0,   167,     0,
     291,     0,     0,     0,   302,   303,   304,   305,   297,   298,
     299,   296,   300,   301,     0,     0,   294,     0,     0,     0,
       0,     0,     0,   356,   354,     0,     0,     0,   207,     0,
     357,     0,   266,   342,   314,     0,     0,   371,   372,     0,
       0,     0,     0,   527,   314,   531,   531,   530,   400,   408,
     407,   406,   405,   403,   404,   770,   768,   794,   805,     0,
     807,   799,   802,   780,   806,   812,   814,     0,   831,   832,
       0,   845,   204,   609,   582,   583,   584,   585,     0,   605,
     611,   613,   617,     0,     0,     0,   615,   602,   603,   626,
     627,     0,   654,   628,   629,   630,   631,   632,   633,   656,
     638,   639,   640,   641,   624,   625,   646,   647,   648,   649,
     650,   651,   652,   653,   623,   657,   658,   659,   660,   661,
     662,   663,   664,   665,   666,   667,   668,   669,   670,   642,
     606,   197,     0,     0,   590,   206,     0,   188,     0,   736,
     737,   741,   739,     0,   738,   735,   734,   721,     0,    79,
     728,    76,    71,    67,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,    82,    83,
      81,     0,     0,     0,   537,     0,     0,     0,     0,    96,
     778,     0,     0,     0,   145,   146,   157,   160,   161,   222,
     187,   237,     0,     0,     0,     0,     0,     0,   168,   222,
     222,   222,   222,   169,   250,   251,   249,   243,   248,   222,
     222,   222,   170,   263,   264,   261,   257,   262,   178,   295,
     293,     0,     0,     0,   315,   316,   317,   318,   565,   148,
       0,   360,     0,   363,   364,     0,   343,   547,   545,     0,
       0,    46,    47,   519,   526,     0,   532,   533,   769,     0,
     767,     0,   833,   844,     0,     0,     0,     0,   655,   634,
     635,   636,   637,   644,     0,     0,   645,   269,     0,   591,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   441,   440,   439,   208,     0,     0,
      79,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,    89,     0,    88,     0,
      87,   433,     0,   214,   213,   314,   314,   774,   682,   156,
     163,     0,   162,   159,     0,   183,     0,   186,     0,   222,
     244,   245,   246,   247,   260,   258,   259,     0,     0,   306,
     307,   308,   309,     0,     0,   355,     0,     0,   548,   386,
     387,   528,   772,     0,     0,     0,     0,     0,   607,   643,
       0,     0,   592,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   729,    68,   432,     0,   431,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   422,     0,   421,
       0,   420,     0,   419,     0,   417,   415,     0,   418,   416,
       0,   430,     0,   429,     0,   428,     0,   427,     0,   448,
       0,   444,   443,     0,   447,     0,   446,     0,     0,    91,
       0,     0,   166,     0,     0,   147,   314,     0,     0,     0,
     292,   310,   267,   314,   362,   164,   771,     0,     0,     0,
     568,   565,   594,     0,   740,     0,     0,     0,   745,   730,
     482,   478,   426,     0,   425,     0,   424,     0,   423,     0,
     480,   478,   476,   474,   468,   471,   480,   478,   476,   474,
     491,   484,   445,   487,    90,    92,     0,   212,   211,     0,
     185,   164,     0,     0,     0,     0,   165,     0,   621,     0,
     567,   569,   593,     0,     0,     0,     0,     0,   480,   478,
     476,   474,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,    80,   207,     0,     0,   319,
     314,   800,     0,   742,   743,   744,   464,   483,   463,   479,
       0,     0,     0,     0,   454,   481,   453,   452,   477,   451,
     475,   449,   470,   469,   450,   473,   472,   458,   457,   456,
     455,   467,   492,   486,   485,   465,   488,     0,   466,   490,
     252,   314,     0,     0,     0,     0,   462,   461,   460,   459,
     489,     0,     0,     0,   324,   320,   329,   330,   331,   332,
     333,   334,   321,   322,   323,   325,   326,   327,   328,   271,
     358,     0,     0,   314,     0,   566,     0,     0,     0,   222,
     179,   335,     0,     0,     0,     0,   164,     0,   314,     0,
     180
};

/* YYPGOTO[NTERM-NUM].  */
static const yytype_int16 yypgoto[] =
{
   -1352,  1531, -1352,  1422,   -67,     0,   -16,    -5,    10,    36,
    -358, -1352,    11,   -14,  1690, -1352, -1352,  1254,  1330,  -631,
   -1352,  -935, -1352,    28, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352,  -309, -1352, -1352, -1352,  1015, -1352, -1352,
   -1352,   537, -1352,  1030,   601,   600, -1352, -1351,  -437, -1352,
    -303, -1352, -1352,  -936, -1352,  -159,   -94, -1352,    -8,  1717,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,   781,
     566, -1352,  -302, -1352,  -689,  -669,  1401, -1352, -1352,  -200,
   -1352,  -141, -1352, -1352,  1193, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352,   -13,    17, -1352, -1352, -1352,  1140,  -116,
    1691,   680,   -41,    19,   909, -1352, -1074, -1352, -1352, -1223,
   -1154, -1160, -1126, -1352, -1352, -1352, -1352,    12, -1352, -1352,
   -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,
   -1352, -1352, -1352,  -145,   865,  1085, -1352,  -722, -1352,   795,
     -22,  -445,   -88,   339,    64, -1352,   -23,   643, -1352,  1079,
      18,   917, -1352, -1352,   911, -1352, -1049, -1352,  1761, -1352,
       7, -1352, -1352,   644,  1301, -1352,  1660, -1352, -1352,  -961,
    1358, -1352, -1352, -1352, -1352, -1352, -1352, -1352, -1352,  1261,
    1077, -1352, -1352, -1352, -1352, -1352
};

/* YYDEFGOTO[NTERM-NUM].  */
static const yytype_int16 yydefgoto[] =
{
       0,     1,    36,   290,   658,   370,    71,   158,   782,  1520,
     582,    38,   372,    40,    41,    42,    43,   106,   229,   671,
     672,   876,  1121,   373,  1289,    45,    46,   677,    47,    48,
      49,    50,    51,    52,   180,   182,   322,   323,   518,  1133,
    1134,   517,   702,   703,   704,  1137,   907,  1465,  1466,   534,
      53,   208,   846,  1067,    74,   107,   108,   109,   211,   230,
     536,   707,   926,  1157,   537,   708,   927,  1166,    54,   952,
     842,   843,    55,   184,   723,   471,   737,  1543,   374,   185,
     375,   745,   377,   378,   563,   379,   380,   564,   565,   566,
     567,   568,   569,   746,   381,    57,    77,   196,   414,   402,
     415,   877,   878,   175,   176,  1237,   879,  1486,  1487,  1485,
    1484,  1477,  1482,  1476,  1493,  1494,  1492,   212,   382,   383,
     384,   385,   386,   387,   388,   389,   390,   391,   392,   393,
     394,   395,   396,   764,   675,   503,   504,   738,   739,   740,
     213,   165,   231,   844,  1009,  1060,   215,   167,   501,   502,
     397,   664,   665,    59,   659,   660,  1074,  1075,    93,    60,
     398,    62,   114,   475,   628,    63,   116,   423,   620,   783,
     621,   622,   630,   623,    64,   424,   631,    65,   542,   205,
     425,   639,    66,   117,   426,   645
};

/* YYTABLE[YYPACT[STATE-NUM]] -- What to do in state STATE-NUM.  If
   positive, shift that token.  If negative, reduce the rule whose
   number is the opposite.  If YYTABLE_NINF, syntax error.  */
static const yytype_int16 yytable[] =
{
      72,    37,   166,   164,   278,   214,   110,   885,    61,   227,
     228,   159,    39,   961,   533,   119,   198,   161,    56,    58,
     535,   538,  1184,   169,   170,   171,   172,   219,  1201,    44,
     851,   121,   122,   932,   674,  1243,   120,   163,  1277,   683,
     684,   685,    68,    69,   376,    70,   120,   419,   420,    68,
      69,   604,    70,   605,   216,    68,    69,   294,    70,   125,
     126,   209,   177,    68,    69,   960,    70,   178,   216,   279,
     200,   127,   404,   406,   408,   410,   412,   216,   199,    68,
      69,   120,    70,   207,   216,   233,    68,    69,   777,    70,
     344,    99,   120,   320,   217,    68,    69,   161,    70,    99,
     216,   120,   292,   498,    68,    69,   254,    70,   478,   238,
    1497,   249,   252,   253,  1198,    99,   120,   163,   453,   123,
     124,    67,   235,   236,   259,    68,    69,   261,    70,   267,
      68,    69,  1533,    70,    68,    69,   216,    70,   120,   123,
     124,  -559,   127,   744,  1241,   472,   291,   736,   271,   712,
     474,   270,   273,   198,   556,   110,    73,  1068,   123,   124,
    1125,  1126,   120,    68,    69,   466,    70,   274,  1534,   371,
      99,   120,   111,   120,  1131,   319,   120,   558,   845,   482,
     931,   120,   120,   120,   120,   449,   206,   120,    68,    69,
     679,    70,   120,   516,  1069,  1070,   282,   283,   284,    75,
     431,   432,   271,   498,   216,   452,    68,    69,  1000,   852,
      95,   684,    68,  1279,   127,    70,   120,   417,   153,   154,
     155,   123,   124,   577,    99,   120,  1491,   577,   470,   585,
     193,   461,   462,   519,   476,  1587,   457,   895,   479,   572,
     112,   458,    99,   578,   579,  1069,  1070,   578,   579,  1280,
     690,   557,  1195,   459,  1014,  1147,   492,   691,  1513,   848,
     120,    76,   481,   483,   505,   206,   113,   487,   268,   291,
     489,   490,   491,   695,  1015,   493,   494,   206,   115,   554,
     497,  1483,   488,  1179,   591,   696,   697,  1489,   793,   153,
     154,   155,   451,   120,  1490,   168,   500,    37,   594,   123,
     124,   577,   590,   698,    61,  1352,    68,    69,    39,    70,
      68,    69,   459,    70,    56,    58,   978,   550,   127,  1511,
    1488,   578,   579,   530,   592,    44,  1512,   713,    68,    69,
     541,    70,   781,   421,   531,   459,  1217,   464,   719,   173,
     539,   540,   465,   728,   422,   272,    99,   206,   998,  1536,
     593,   532,  1510,   174,   699,   269,  1537,   156,   179,   218,
    1081,   153,   154,   155,   570,  1082,   589,   573,   546,   663,
     547,   583,  1417,   778,   548,   784,   905,   368,  1341,   269,
     110,   588,  1495,   499,  1218,   161,  1342,   206,   181,   286,
     571,   287,  1219,   574,   575,   288,   289,   584,   730,  1199,
     608,   451,   895,  1508,   472,   163,  1519,   687,   688,   206,
     599,   600,   206,   601,   597,   602,   206,   376,  1532,   919,
    1535,   576,   581,  -542,   921,   749,  1530,  1507,   156,  -559,
     657,   610,   611,   220,   617,   617,   637,   643,   400,   206,
     715,   619,   401,   603,   654,  1549,   655,   221,  1127,  1514,
    1242,   618,   618,   638,   644,   110,  1063,  1400,  1516,   733,
    1517,   271,  1064,  1527,   452,   153,   154,   155,  1528,  1529,
    1546,  1547,   742,   680,  1548,   183,  1506,   722,   577,   500,
    1317,    68,    69,   499,    70,   201,  1118,   470,   269,   127,
     202,  1119,   232,     3,  1213,   551,   120,   368,   578,   579,
     156,   758,   947,   948,   949,   552,  1501,   498,  1524,   237,
     123,   124,  1521,   262,   263,  1214,   632,    99,   206,   694,
    1282,  1215,   725,   264,   485,   203,   700,  1283,  1216,    68,
      69,   766,    70,    98,   709,   710,   204,   701,   100,   118,
     101,    68,   371,   206,    70,   729,   731,   102,   785,   656,
     747,   206,   206,   721,   750,   751,   752,   682,   847,   727,
    1540,   222,   732,   234,   103,   915,   198,    68,    69,   454,
      70,   761,   455,  1403,  1404,   127,   748,   403,   376,   104,
     762,   401,   957,   206,   757,   580,   120,   186,   577,   275,
     187,   188,   189,   190,   405,   191,   192,   193,   401,   250,
     251,  1538,   260,    99,   156,   276,   763,   277,   578,   579,
     280,   916,   760,  1398,   917,   120,   533,   909,   910,   914,
     923,   281,   535,   538,   407,   282,   283,   284,   401,   795,
     633,   293,  1348,  1349,  1350,   797,   153,   154,   155,   294,
     259,   786,   787,   794,   282,   283,   284,   790,   409,   791,
     505,   979,   401,   980,   981,   982,   983,   984,  1415,  1136,
     206,   295,   799,    28,    29,    30,    31,    32,    33,    34,
     901,   884,    14,   296,   893,   285,   894,   223,    35,   224,
     225,   226,   634,   875,  1459,   635,    68,    69,   399,    70,
     413,  1463,   972,   282,   283,   284,   321,   892,   906,   899,
     500,   416,   913,   371,   418,     3,   918,   920,   922,   559,
     959,   560,   561,   562,   962,   282,   283,   284,    68,    69,
     433,    70,   153,   154,   155,  1145,   206,    68,   965,   884,
      70,   120,    68,    69,   950,    70,   953,   967,   955,  1461,
     956,   450,  1264,   186,  1267,   951,   187,   188,   189,   190,
     456,   191,   192,   193,   966,  1069,  1070,    28,    29,    30,
      31,    32,    33,    34,   968,   969,   636,   663,  1319,  1320,
      96,   459,    35,    97,   460,   156,    68,    69,  1544,    70,
     120,  1071,   240,   241,   242,   473,   985,   463,   986,   987,
    1083,   999,   970,  1001,   459,  1469,    98,    99,  1470,   477,
     411,   100,   997,   101,   401,   886,   887,   243,    68,    69,
     102,    70,  1084,   123,   124,   577,  1085,  1135,   286,  1551,
     287,  1321,  1322,   480,   288,   289,  1073,   103,  1086,  1065,
    1196,  1197,  1087,   467,   468,   578,   579,   286,   495,   287,
     888,   484,   104,   288,   289,  1003,  1004,  1005,  1006,  1007,
     486,  1577,   459,  1335,   194,   282,   283,   284,  1072,   506,
    1159,   156,  1160,  1161,  1169,   496,  1589,   206,  1406,  1183,
     195,  1185,  1129,  1552,   459,  1545,   427,   606,   428,   429,
     509,    11,    12,    13,    14,   430,   286,  1120,   287,   543,
    1123,  1092,   288,   289,  1142,  1093,  1140,  1122,  1017,  1018,
    1146,   244,  1138,   245,   246,   247,   248,  1094,   286,  1141,
     287,  1095,   544,  1132,   686,   289,  1586,   549,   555,   884,
    1208,  1209,  1210,  1211,  1212,   530,  1154,  1163,   596,  1143,
     607,  1144,   541,  1158,  1167,  1096,   531,  1155,  1164,  1097,
     637,  1098,   539,   540,  1100,  1099,  1205,   884,  1101,   609,
     510,   511,   512,   532,  1156,  1165,     3,   638,  1102,   762,
     762,  1104,  1103,  1106,  1108,  1105,  1193,  1107,  1109,    28,
      29,    30,    31,    32,    33,    34,   216,     3,  1162,   632,
     889,   890,   661,   891,    35,   763,   763,   513,   514,   515,
    1310,  1311,  1312,  1313,  1110,   598,   652,   653,  1111,  1235,
    1314,  1315,  1316,   646,   647,   648,     3,   666,   667,  1118,
    1203,   586,   128,   587,  1119,   668,   129,   130,   131,   132,
     133,   669,   134,   135,   136,   137,   195,   138,   139,   673,
     676,   140,   141,   142,   143,   625,   681,    99,   144,   145,
     649,   650,   651,   689,   974,   692,   693,   146,   286,   147,
     287,   105,  1112,   705,   288,   289,  1113,  1307,  1308,  1114,
     706,   216,   726,  1115,   148,   149,   150,  1116,  -238,   716,
    1247,  1117,   206,  1220,  1248,   459,  1221,  1222,  1223,  1194,
    1224,  1225,  1226,  1227,  1249,  1228,  1229,   193,  1250,  1230,
    1231,  1232,  1233,   633,  1284,  1244,  1245,  1246,  1234,   717,
     151,   718,  1255,  1256,  1257,  1258,  1259,  1260,  1251,  1262,
    1263,  1265,  1252,  1268,  1269,  1270,  1271,  1272,  1273,  1274,
    1253,  1276,   206,  1278,  1254,  1281,  1456,  1285,  1523,  1526,
     734,   110,   459,   720,  1261,    14,  1575,  1304,  1266,   735,
     753,   110,   110,   110,   110,   634,  1275,  1338,   635,   754,
     755,   110,   110,   110,   756,   759,    14,   765,  1149,  1150,
    1151,  1152,   767,   768,  1325,    98,   640,   769,   770,   641,
     100,  1328,   101,  1329,     3,   255,   256,   257,   258,   102,
     771,  1318,    11,    12,    13,    14,    28,    29,    30,    31,
      32,    33,    34,   772,   773,  1353,   103,   775,  1118,   779,
    1330,    35,   792,  1119,  1088,  1089,  1090,  1091,   780,  1332,
     776,   104,   788,  1405,   789,  1409,  1336,  1337,   796,   798,
      28,    29,    30,    31,    32,    33,    34,   849,  1340,  1168,
    1343,  1344,  1345,  1346,   850,    35,   854,     3,  1414,   855,
     853,    28,    29,    30,    31,    32,    33,    34,  1347,  1351,
     642,   856,  1120,   857,   858,   882,    35,  1359,  1360,  1361,
    1362,  1363,  1364,   883,  1366,  1399,   896,   903,  1419,   897,
      28,    29,    30,    31,    32,    33,    34,   898,   902,  1153,
     908,   924,   964,   930,  1401,    35,   971,   975,   963,  1365,
     954,   958,   976,   973,    68,    69,   988,    70,  1010,   989,
     977,   990,   127,   624,   157,   128,   401,  1011,   991,   129,
     130,   131,   132,   133,   992,   134,   135,   136,   137,  1460,
     138,   139,   884,   993,   140,   141,   142,   143,   994,  1008,
      99,   144,   145,     9,    10,   995,   996,  1002,  1012,  1013,
     146,  1016,   147,  1061,  1418,  1124,  1066,  1077,  1078,  1079,
    1139,  1423,  1171,    14,  1130,  1172,  1173,   148,   149,   150,
    1174,  1175,  1176,  1120,  1177,   612,  1178,   613,  1180,  1181,
     614,   615,  1186,  1182,  1498,    28,    29,    30,    31,    32,
      33,    34,   736,  1189,  1429,  1190,  1462,  1200,  1191,  1204,
      35,  1192,  1202,   151,  1240,   282,   283,   284,  1206,  1207,
    1238,  1239,  1502,   884,  1291,  1293,  1294,  1292,  1295,   467,
     468,  1296,   474,  1298,  1299,  1300,    14,  1301,  1309,  1326,
    1305,  1306,  1327,   516,  1331,  1334,  1084,  1333,   625,  1339,
     613,   626,  1086,   614,   615,  1473,  1474,  1475,    28,    29,
      30,    31,    32,    33,    34,  1092,  1094,   616,  1355,   153,
     154,   155,  1096,    35,   859,   860,   861,  1354,   862,   863,
     864,   865,  1098,   866,   867,   193,  1100,   868,   869,   870,
     871,  1102,  1539,  1356,   872,   873,  1104,  1106,  1571,  1108,
    1110,  1395,  1357,  1358,  1367,  1582,  1369,  1509,  1371,  1368,
    1373,  1370,  1515,  1509,  1518,  1584,  1522,  1372,  1515,  1509,
    1518,    28,    29,    30,    31,    32,    33,    34,  1374,  1375,
     627,  1376,  1377,  1378,  1379,  1408,    35,  1381,  1383,  1385,
    1515,  1509,  1518,  1525,  1380,  1382,  1384,  1386,  1387,  1388,
    1389,  1391,  1392,  1390,  1416,  1394,  1396,   884,  1393,    68,
      69,  1397,    70,   874,  1402,  1407,  1411,   127,  1420,  1412,
     128,  1413,  1247,  1249,   129,   130,   131,   132,   133,  1583,
     134,   135,   136,   137,  1421,   138,   139,  1251,  1422,   140,
     141,   142,   143,  1253,  1425,    99,   144,   145,  1424,  1426,
     884,  1427,  1428,  1430,  1578,   146,  1431,   147,   469,  1432,
     287,  1434,  1433,  1436,   288,   289,  1435,   157,    68,    69,
    1437,    70,   148,   149,   150,  1438,   127,  1439,  1440,   128,
    1441,  1442,  1443,   129,   130,   131,   132,   133,  1452,   134,
     135,   136,   137,  1444,   138,   139,  1445,  1446,   140,   141,
     142,   143,  1447,  1448,    99,   144,   145,  1449,   151,  1450,
     282,   283,   284,  1451,   146,  1453,   147,  1454,  1457,  1455,
    1458,  1467,  1464,  1468,   467,   468,  1472,  1496,  1478,  1479,
    1480,   148,   149,   150,  1481,   880,  1499,   859,   860,   861,
    1500,   862,   863,   864,   865,  1541,   866,   867,   193,  1503,
     868,   869,   870,   871,  1504,  1505,  1542,   872,   873,  1550,
    1570,  1572,  1573,  1574,   153,   154,   155,   151,  1576,   282,
     283,   284,  1579,    68,    69,  1580,    70,  1581,  1585,  1588,
     297,   127,  1590,   507,   128,   160,  1324,   670,   129,   130,
     131,   132,   133,   595,   134,   135,   136,   137,  1531,   138,
     139,   925,   904,   140,   141,   142,   143,  1302,  1303,    99,
     144,   145,   162,  1187,  1323,   545,  1236,   774,   197,   146,
    1128,   147,  1062,   153,   154,   155,   874,   743,   881,  1188,
    1471,  1076,    68,    69,  1290,    70,   148,   149,   150,   900,
     127,    94,  1080,   128,  1297,   239,   678,   129,   130,   131,
     132,   133,   629,   134,   135,   136,   137,   711,   138,   139,
    1553,     0,   140,   141,   142,   143,     0,   929,    99,   144,
     145,     0,   151,   152,     0,  1554,     0,     0,   146,     0,
     147,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,  1555,     0,     0,     0,   148,   149,   150,     0,   911,
    1556,     0,     0,   469,     0,   287,     0,     0,     0,   686,
     289,     0,   157,     0,  1557,  1558,  1559,  1560,  1561,     0,
       0,     0,     0,     0,     0,     0,     0,     0,   153,   154,
     155,   151,   933,     0,     0,     0,     0,     0,     0,   934,
       0,   935,   936,   937,     0,   912,     0,  1562,  1563,  1564,
    1565,  1566,  1567,  1568,     0,     0,     0,     0,     0,     0,
       0,     0,   469,     0,   287,    14,     0,     0,   288,   289,
       0,   157,     0,     0,     0,    68,    69,     0,    70,     0,
     938,   939,   940,   127,     0,     0,   128,   153,   154,   155,
     129,   130,   131,   132,   133,     0,   134,   135,   136,   137,
       0,   138,   139,     0,     0,   140,   141,   142,   143,     0,
       0,    99,   144,   145,     0,     0,     0,     0,     0,     0,
       0,   146,     0,   147,     0,     0,     0,   941,   942,   943,
       0,   944,     0,     0,   945,     0,     0,     0,   148,   149,
     150,     0,     0,   128,     0,     0,     0,     0,     0,   131,
     132,   133,     0,   134,   135,   136,   137,     0,   138,   139,
       0,     0,   140,   141,   142,   143,     0,   156,     0,  1286,
     145,     0,     0,     0,   151,   152,   157,    68,    69,     0,
      70,     0,     0,     0,     0,   127,     0,     0,   128,     0,
       0,     0,   129,   130,   131,   132,   133,     0,   134,   135,
     136,   137,     0,   138,   139,     0,     0,   140,   141,   142,
     143,  1569,     0,    99,   144,   145,     0,     0,  1287,     0,
       0,     0,     0,   146,     0,   147,   210,     0,     0,     0,
     153,   154,   155,     0,     0,   157,    68,    69,  1288,    70,
     148,   149,   150,     0,   127,     0,     0,   128,     0,     0,
       0,   129,   130,   131,   132,   133,     0,   134,   135,   136,
     137,     0,   138,   139,     0,     0,   140,   141,   142,   143,
       0,     0,    99,   144,   145,     0,   151,     0,     0,     0,
       0,     0,   146,     0,   147,     0,     0,     0,     0,   946,
       0,     0,     0,     0,     0,     0,     0,     0,     0,   148,
     149,   150,     0,     0,     0,   933,     0,     0,     0,     0,
       0,     0,   934,     0,   935,   936,   937,     0,     2,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   153,   154,   155,   553,     3,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,    68,
      69,     0,    70,   938,   939,   940,     0,   127,     0,     0,
     128,     0,     0,     0,   129,   130,   131,   132,   133,   210,
     134,   135,   136,   137,     0,   138,   139,     0,   157,   140,
     141,   142,   143,     0,     0,    99,   144,   145,     0,     0,
       0,   153,   154,   155,     0,   662,     0,   147,     0,     0,
     941,   942,   943,     0,   944,     0,     0,   945,     0,     0,
       0,     0,   148,   149,   150,     0,     0,     0,     0,    68,
      69,     0,    70,     0,     0,     0,     0,   127,     0,     0,
     128,     0,     0,     0,   129,   130,   131,   132,   133,     0,
     134,   135,   136,   137,     0,   138,   139,     0,   151,   140,
     141,   142,   143,     0,     0,    99,   144,   145,     0,   933,
      96,     0,     0,    97,     0,   146,   934,   147,   935,   936,
     937,   210,     0,     4,     5,     6,     7,     8,     0,     0,
     157,     0,   148,   149,   150,     0,    98,    99,     0,     0,
       0,   100,     0,   101,     0,     9,    10,     0,     0,     0,
     102,     0,     0,     0,   153,   154,   155,   938,   939,   940,
       0,     0,    11,    12,    13,    14,     0,   103,   714,    15,
      16,     0,     0,     0,     0,    17,     0,     0,    18,     0,
     210,     0,   104,     0,    68,    19,    20,    70,     0,   157,
       0,     0,     0,     0,     0,     0,     0,     3,     0,     0,
       0,     0,     0,     0,   941,   942,   943,     0,   944,     0,
       0,   945,  1170,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   153,   154,   155,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,    21,    22,     0,    23,    24,    25,     0,    26,    27,
      28,    29,    30,    31,    32,    33,    34,     0,     0,     0,
       0,   265,   128,   266,     0,    35,   129,   130,   131,   132,
     133,     0,   134,   135,   136,   137,     0,   138,   139,     0,
       0,   140,   141,   142,   143,     0,     0,     0,   144,   145,
       0,     0,     0,   210,     0,     0,     0,   146,     0,   147,
       0,     0,   157,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   148,   149,   150,     0,     0,     0,
       0,   345,   346,   347,   348,   349,   350,   351,   352,   353,
     354,   355,   356,   357,    68,    69,     0,    70,     8,     0,
       0,     0,   358,   359,   360,   361,   362,   363,    68,     0,
     151,    70,     0,     0,     0,     0,     9,    10,     0,     0,
       0,     3,     0,   210,     0,     0,  1410,     0,   298,     0,
       0,     0,   157,    11,    12,    13,    14,     0,     0,     0,
       0,     0,     0,     0,   364,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   299,   365,     0,
       0,   300,     0,     0,   301,   302,     0,     0,     0,   303,
     304,   305,   306,   307,   308,   309,   310,   311,   312,   313,
     314,     0,     0,     0,     0,     0,     0,     0,   315,     0,
       0,   316,    68,   366,   367,    70,     0,     0,   317,     0,
       0,     0,     0,     0,     0,     3,     0,   318,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,    28,    29,    30,    31,    32,    33,    34,     0,   368,
     369,     0,     0,     0,     0,     0,    35,     0,     0,     0,
       0,     0,     0,     0,     0,   345,   346,   347,   348,   349,
     350,   351,   352,   353,   354,   355,   356,   357,     0,     0,
       0,     0,     8,     0,     0,     0,   358,   359,   360,   361,
     362,   363,     0,     0,     0,     0,     0,     0,     0,     0,
       9,    10,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,    11,    12,    13,
      14,     0,     0,     0,     0,     0,     0,     0,   364,     0,
       0,     0,     0,     0,   157,     0,     0,     0,     0,     0,
       0,     0,   365,     0,     0,     0,     0,     0,     0,   345,
     346,   347,   348,   349,   350,   351,   352,   353,   354,   355,
     356,   357,     0,     0,     0,     0,     8,     0,     0,     0,
     358,   359,   360,   361,   362,   363,     0,   366,   367,     0,
       0,     0,     0,     0,     9,    10,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,    11,    12,    13,    14,    28,    29,    30,    31,    32,
      33,    34,   364,   368,   741,     0,     0,   128,     0,     0,
      35,   129,   130,   131,   132,   133,   365,   134,   135,   136,
     137,     0,   138,   139,     0,     0,   140,   141,   142,   143,
     434,     0,     0,   144,   145,     0,     0,     0,     0,     0,
       0,     0,   146,     0,   147,     0,     0,     0,     0,     0,
       0,   366,   367,     0,     0,     0,     0,     0,     0,   148,
     149,   150,     0,   435,     0,   436,   437,   438,   439,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,    28,
      29,    30,    31,    32,    33,    34,     0,   368,   928,     0,
       0,     0,     0,     0,    35,   151,     0,     0,     0,     0,
       0,     0,     0,   440,   441,   442,   443,     0,     0,   444,
       0,     0,   128,   445,   446,   447,   129,   130,   131,   132,
     133,     0,   134,   135,   136,   137,     0,   138,   139,     0,
       0,   140,   141,   142,   143,     0,     0,     0,   144,   145,
       0,     0,     0,     0,     0,     0,     0,   146,     0,   147,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     3,   148,   149,   150,   128,     0,     0,
       0,   129,   130,   131,   132,   133,     0,   134,   135,   136,
     137,     0,   138,   139,     0,     0,   140,   141,   142,   143,
       0,     0,     0,   144,   145,     0,     0,     0,     0,     0,
     151,     0,   146,     0,   147,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   448,     0,   148,
     149,   150,   128,     0,     0,     0,   129,   130,   131,   132,
     133,     0,   134,   135,   136,   137,     0,   138,   139,     0,
       0,   140,   141,   142,   143,     0,     0,     0,   144,   145,
       0,     0,     0,     0,     0,   151,     0,   146,     0,   147,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   148,   149,   150,     0,     0,  1019,
    1020,     0,  1021,  1022,  1023,  1024,  1025,  1026,     0,  1027,
    1028,     0,  1029,  1030,  1031,  1032,  1033,     0,     0,   157,
       4,     5,     6,     7,     8,     0,     0,     0,     0,     0,
     151,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     9,    10,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,    11,
      12,    13,    14,     0,     0,     0,    15,    16,     0,     0,
       0,     0,    17,     0,     0,    18,     0,     0,     0,     0,
       0,     0,    19,    20,     0,     0,     0,     0,     0,     0,
       0,     0,   859,   860,   861,     0,   862,   863,   864,   865,
       0,   866,   867,   193,     0,   868,   869,   870,   871,     0,
       0,     0,   872,   873,     3,   724,     0,     0,     0,     0,
       0,     0,     0,     0,   157,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,    21,    22,
       0,    23,    24,    25,     0,    26,    27,    28,    29,    30,
      31,    32,    33,    34,     0,     0,   508,     0,     0,     0,
       0,     3,    35,   520,     0,     0,     0,     0,     0,     0,
     880,     0,     0,     0,     0,     0,     0,     0,     0,   157,
       0,   874,    78,    79,    80,    81,    82,    83,    84,    85,
      86,    87,    88,    89,    90,    91,    92,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,  1034,  1035,
     520,  1036,  1037,  1038,     0,  1039,  1040,     0,     0,  1041,
    1042,     0,  1043,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   157,  1044,  1045,  1046,  1047,  1048,
    1049,  1050,  1051,  1052,  1053,  1054,  1055,  1056,  1057,  1058,
       0,   521,     0,     6,     7,     8,     0,     0,     0,     0,
       0,     0,     0,     0,     0,   522,     0,     0,     0,     0,
     523,     0,     0,     9,    10,     0,     0,     0,     0,     0,
       0,     0,     0,  1059,     0,     0,     0,     0,     0,     0,
      11,    12,    13,    14,     0,   524,   525,     0,   521,     0,
       6,     7,     8,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   522,     0,     0,   526,     0,   523,     0,     0,
       9,    10,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,    11,    12,    13,
      14,     0,   524,   525,     0,     0,     0,     0,     0,     0,
     527,   528,    28,    29,    30,    31,    32,    33,    34,     0,
       0,     0,   526,     0,     0,     0,     0,    35,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,    28,    29,
      30,    31,    32,    33,    34,     0,     0,   529,     0,     0,
       0,     0,     0,    35,   800,     0,     0,   527,   528,   801,
     802,     0,   803,   804,   805,   806,   807,   808,     0,   809,
     810,     0,   811,   812,   813,   814,   815,     0,     0,     0,
       0,     0,     0,     0,     0,    28,    29,    30,    31,    32,
      33,    34,     0,   800,  1148,     0,     0,     0,   801,   802,
      35,   803,   804,   805,   806,   807,   808,     0,   809,   810,
       0,   811,   812,   813,   814,   815,     0,     0,   816,     0,
     817,     0,     0,     0,     0,   818,     0,     0,     0,     0,
     324,    98,     0,     0,     0,     0,   100,     0,   101,     0,
       0,     0,   819,     0,     0,   102,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   816,     0,   817,
       0,     0,   103,   325,   818,   326,   327,   328,   329,   330,
       0,     0,     0,     0,   331,   820,     0,   104,     0,     0,
       0,   819,     0,   332,     0,     0,     0,     0,     0,   333,
       0,   334,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   335,   336,   337,   338,   339,   340,   341,
     342,     0,     0,     0,   820,     0,   343,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,   821,     0,   822,   823,   824,
     825,   826,   827,   828,   829,   830,   831,   832,   833,   834,
     835,   836,   837,   838,     0,     0,     0,   839,     0,     0,
       0,     0,     0,     0,     0,     0,   840,     0,     0,     0,
       0,     0,     0,     0,   821,     0,   822,   823,   824,   825,
     826,   827,   828,   829,   830,   831,   832,   833,   834,   835,
     836,   837,   838,     0,     0,     0,   839,     0,   841,     0,
       0,     0,     0,     0,     0,   840
};

static const yytype_int16 yycheck[] =
{
       5,     1,    25,    25,   163,    93,    14,   676,     1,   103,
     104,    25,     1,   735,   323,    20,    57,    25,     1,     1,
     323,   323,   958,    28,    29,    30,    31,    94,   989,     1,
     661,    21,    22,   722,   471,  1084,     9,    25,  1112,   484,
     485,   486,     5,     6,   185,     8,     9,     7,     8,     5,
       6,   409,     8,   411,     7,     5,     6,    12,     8,    23,
      24,    77,    43,     5,     6,   734,     8,    48,     7,   163,
      60,    13,   188,   189,   190,   191,   192,     7,    59,     5,
       6,     9,     8,    73,     7,   108,     5,     6,     8,     8,
     184,    41,     9,   181,    33,     5,     6,   105,     8,    41,
       7,     9,   169,    20,     5,     6,   129,     8,   137,   114,
    1461,   116,   117,   118,     8,    41,     9,   105,   212,     9,
      10,   216,   112,   113,   146,     5,     6,   149,     8,   152,
       5,     6,     8,     8,     5,     6,     7,     8,     9,     9,
      10,    19,    13,    56,  1079,   233,   168,    20,   156,   111,
     167,   156,   157,   194,   198,   163,   299,    28,     9,    10,
     882,   883,     9,     5,     6,   232,     8,   157,    44,   185,
      41,     9,   298,     9,   896,   180,     9,   377,    63,   267,
     137,     9,     9,     9,     9,   208,   294,     9,     5,     6,
     137,     8,     9,   301,    65,    66,   106,   107,   108,    64,
     205,   206,   210,    20,     7,   210,     5,     6,    63,     8,
       8,   656,     5,     8,    13,     8,     9,   198,   160,   161,
     162,     9,    10,    11,    41,     9,  1449,    11,   233,   388,
      33,   221,   222,   321,   239,  1586,   290,   682,   260,   380,
     299,   295,    41,    31,    32,    65,    66,    31,    32,    44,
     293,   295,   974,   292,   228,   924,   279,   300,  1481,   298,
       9,   126,   267,   268,   287,   294,   275,   272,   218,   291,
     275,   276,   277,    39,   248,   280,   281,   294,   216,   367,
     285,  1441,   272,   952,    87,    51,    52,  1447,   216,   160,
     161,   162,   218,     9,  1448,   290,   286,   297,   392,     9,
      10,    11,   390,    69,   297,  1240,     5,     6,   297,     8,
       5,     6,   292,     8,   297,   297,   296,   111,    13,  1479,
    1446,    31,    32,   323,   391,   297,  1480,   289,     5,     6,
     323,     8,   305,   293,   323,   292,   291,   290,   538,     7,
     323,   323,   295,   299,   304,   308,    41,   294,   793,    44,
     391,   323,  1478,    12,   120,   305,    51,   299,   290,   298,
     290,   160,   161,   162,   380,   295,   389,   383,   358,   457,
     360,   387,  1333,   293,   364,   298,   295,   290,   292,   305,
     388,   389,  1456,   300,   292,   393,   300,   294,    60,   299,
     380,   301,   300,   383,   384,   305,   306,   387,   299,   293,
     416,   218,   847,   296,   492,   393,   296,   495,   496,   294,
     400,   401,   294,   403,   395,   405,   294,   558,  1492,   299,
     296,   385,   386,   296,   299,   566,   296,  1476,   299,   307,
     453,   421,   422,   293,   423,   424,   425,   426,   295,   294,
     528,   423,   299,   407,   449,   296,   451,   295,   885,   296,
    1081,   423,   424,   425,   426,   463,   299,   299,   296,   553,
     296,   469,   305,   296,   469,   160,   161,   162,   296,   296,
     296,   296,   560,   478,   296,   290,   296,   544,    11,   469,
    1169,     5,     6,   300,     8,   290,   105,   492,   305,    13,
     290,   110,   298,    18,   272,   289,     9,   290,    31,    32,
     299,   589,   251,   252,   253,   299,  1467,    20,   296,   208,
       9,    10,   296,    24,    25,   293,    41,    41,   294,   509,
      44,   299,   545,    34,   300,   290,   292,    51,   306,     5,
       6,   598,     8,    40,   524,   525,   290,   303,    45,   216,
      47,     5,   558,   294,     8,   550,   551,    54,   615,   300,
     566,   294,   294,   543,   567,   568,   569,   300,   300,   549,
    1496,   295,   552,   295,    71,    72,   607,     5,     6,   295,
       8,   594,   298,  1295,  1296,    13,   566,   295,   719,    86,
     596,   299,   293,   294,   589,   295,     9,    23,    11,   137,
      26,    27,    28,    29,   295,    31,    32,    33,   299,    75,
      76,   296,    51,    41,   299,   137,   596,   137,    31,    32,
     137,   118,   593,    51,   708,     9,   925,   705,   706,   707,
     714,   307,   925,   925,   295,   106,   107,   108,   299,   634,
     155,   296,    26,    27,    28,   640,   160,   161,   162,    12,
     662,   622,   623,   633,   106,   107,   108,   628,   295,   630,
     673,   767,   299,   769,   770,   771,   772,   773,  1327,   293,
     294,   296,   652,   282,   283,   284,   285,   286,   287,   288,
     693,   676,   197,   296,   679,   137,   681,    46,   297,    48,
      49,    50,   207,   672,  1406,   210,     5,     6,   305,     8,
     295,  1413,   759,   106,   107,   108,    61,   678,   703,   689,
     690,   295,   707,   719,   296,    18,   711,   712,   713,    55,
     733,    57,    58,    59,   737,   106,   107,   108,     5,     6,
     300,     8,   160,   161,   162,   293,   294,     5,   744,   734,
       8,     9,     5,     6,   724,     8,   726,   753,   728,  1408,
     730,   298,  1100,    23,  1102,   226,    26,    27,    28,    29,
     295,    31,    32,    33,   744,    65,    66,   282,   283,   284,
     285,   286,   287,   288,   754,   755,   291,   855,   139,   140,
      14,   292,   297,    17,     8,   299,     5,     6,  1500,     8,
       9,   848,   101,   102,   103,   300,   776,   296,   778,   779,
     857,   796,   756,   798,   292,   293,    40,    41,   296,   221,
     295,    45,   792,    47,   299,    28,    29,   126,     5,     6,
      54,     8,   295,     9,    10,    11,   299,   905,   299,  1541,
     301,   139,   140,    25,   305,   306,   848,    71,   295,   845,
     975,   976,   299,   120,   121,    31,    32,   299,   295,   301,
      63,   300,    86,   305,   306,    25,    26,    27,    28,    29,
     300,  1573,   292,   293,   290,   106,   107,   108,   848,   291,
     173,   299,   175,   176,   931,   295,  1588,   294,   295,   957,
     306,   959,   888,  1542,   292,   293,    73,   291,    75,    76,
     295,   194,   195,   196,   197,    82,   299,   876,   301,   295,
     880,   295,   305,   306,   917,   299,   910,   878,   834,   835,
     923,   220,   907,   222,   223,   224,   225,   295,   299,   914,
     301,   299,   295,   903,   305,   306,  1585,   299,   304,   924,
      25,    26,    27,    28,    29,   925,   926,   927,   295,   919,
     293,   921,   925,   926,   927,   295,   925,   926,   927,   299,
     929,   295,   925,   925,   295,   299,  1013,   952,   299,   296,
      75,    76,    77,   925,   926,   927,    18,   929,   295,   975,
     976,   295,   299,   295,   295,   299,   971,   299,   299,   282,
     283,   284,   285,   286,   287,   288,     7,    18,   291,    41,
     203,   204,   290,   206,   297,   975,   976,   112,   113,   114,
    1149,  1150,  1151,  1152,   295,   295,   295,   295,   299,  1066,
    1159,  1160,  1161,    75,    76,    77,    18,     7,   293,   105,
    1000,    15,    16,    17,   110,   296,    20,    21,    22,    23,
      24,   296,    26,    27,    28,    29,   306,    31,    32,   301,
      19,    35,    36,    37,    38,   209,    19,    41,    42,    43,
     112,   113,   114,    20,   295,   302,   293,    51,   299,    53,
     301,   295,   295,   293,   305,   306,   299,  1145,  1146,   295,
     111,     7,   304,   299,    68,    69,    70,   295,   290,   290,
     295,   299,   294,  1063,   299,   292,    22,    23,    24,   296,
      26,    27,    28,    29,   295,    31,    32,    33,   299,    35,
      36,    37,    38,   155,  1116,  1085,  1086,  1087,    44,   290,
     104,   290,  1092,  1093,  1094,  1095,  1096,  1097,   295,  1099,
    1100,  1101,   299,  1103,  1104,  1105,  1106,  1107,  1108,  1109,
     295,  1111,   294,  1113,   299,  1115,   298,  1117,  1486,  1487,
      19,  1139,   292,   290,  1098,   197,   296,  1142,  1102,   295,
      62,  1149,  1150,  1151,  1152,   207,  1110,  1214,   210,    62,
     295,  1159,  1160,  1161,   295,   295,   197,   296,   170,   171,
     172,   173,   296,   300,  1180,    40,   207,   296,   296,   210,
      45,  1187,    47,  1189,    18,    26,    27,    28,    29,    54,
     296,  1171,   194,   195,   196,   197,   282,   283,   284,   285,
     286,   287,   288,   296,   296,   291,    71,   296,   105,   213,
    1190,   297,   295,   110,    26,    27,    28,    29,   298,  1199,
     304,    86,   298,  1301,   298,  1309,  1206,  1207,   216,   216,
     282,   283,   284,   285,   286,   287,   288,   296,  1218,   291,
      26,    27,    28,    29,   293,   297,   291,    18,  1326,   293,
     298,   282,   283,   284,   285,   286,   287,   288,  1238,  1239,
     291,     8,  1241,   298,   291,   295,   297,  1247,  1248,  1249,
    1250,  1251,  1252,   295,  1254,  1287,   295,   295,  1335,   296,
     282,   283,   284,   285,   286,   287,   288,   296,   296,   291,
     302,    19,   293,   296,  1289,   297,    19,   293,   296,  1253,
     300,   300,   293,   296,     5,     6,   295,     8,   243,   304,
     296,   295,    13,    84,   308,    16,   299,   235,   296,    20,
      21,    22,    23,    24,   296,    26,    27,    28,    29,  1407,
      31,    32,  1327,   295,    35,    36,    37,    38,   295,   295,
      41,    42,    43,   177,   178,   296,   296,   296,   247,   295,
      51,    22,    53,   296,  1334,   302,   298,   291,   298,   290,
     197,  1341,   295,   197,   296,   304,   304,    68,    69,    70,
     300,   300,   300,  1352,   300,   209,   295,   211,   137,   300,
     214,   215,   296,   300,  1462,   282,   283,   284,   285,   286,
     287,   288,    20,    62,   291,    62,  1409,     8,   296,   250,
     297,   296,   296,   104,   290,   106,   107,   108,   299,   299,
     295,   295,  1469,  1408,   296,   296,   296,   300,   295,   120,
     121,   295,   167,   296,   296,   296,   197,   293,   256,   293,
     300,   300,    19,   301,   296,   298,   295,   304,   209,   300,
     211,   212,   295,   214,   215,  1425,  1426,  1427,   282,   283,
     284,   285,   286,   287,   288,   295,   295,   291,   296,   160,
     161,   162,   295,   297,    22,    23,    24,   291,    26,    27,
      28,    29,   295,    31,    32,    33,   295,    35,    36,    37,
      38,   295,  1494,   300,    42,    43,   295,   295,  1545,   295,
     295,     8,   296,   300,   296,  1579,   296,  1477,   296,   300,
     296,   300,  1482,  1483,  1484,  1583,  1486,   300,  1488,  1489,
    1490,   282,   283,   284,   285,   286,   287,   288,   300,   296,
     291,   296,   300,   296,   296,    19,   297,   296,   296,   296,
    1510,  1511,  1512,  1487,   300,   300,   300,   300,   296,   300,
     296,   296,   296,   300,     8,   296,   296,  1542,   300,     5,
       6,   300,     8,   111,   302,   293,   296,    13,   300,   296,
      16,   295,   295,   295,    20,    21,    22,    23,    24,  1582,
      26,    27,    28,    29,   300,    31,    32,   295,   300,    35,
      36,    37,    38,   295,   304,    41,    42,    43,   296,   304,
    1585,   304,   296,   295,  1574,    51,   295,    53,   299,   296,
     301,   296,   300,   296,   305,   306,   300,   308,     5,     6,
     300,     8,    68,    69,    70,   296,    13,   300,   295,    16,
     295,   295,   295,    20,    21,    22,    23,    24,   296,    26,
      27,    28,    29,   295,    31,    32,   295,   295,    35,    36,
      37,    38,   295,   295,    41,    42,    43,   295,   104,   295,
     106,   107,   108,   295,    51,   295,    53,     8,   296,   300,
     296,   304,   301,   296,   120,   121,   300,   296,   295,   295,
     295,    68,    69,    70,   295,   299,   296,    22,    23,    24,
     295,    26,    27,    28,    29,   295,    31,    32,    33,   296,
      35,    36,    37,    38,   296,   296,    19,    42,    43,     8,
     296,   296,   295,   295,   160,   161,   162,   104,   256,   106,
     107,   108,   104,     5,     6,   296,     8,   296,    19,   295,
     179,    13,   296,   291,    16,    25,  1179,   463,    20,    21,
      22,    23,    24,   393,    26,    27,    28,    29,   296,    31,
      32,   716,   702,    35,    36,    37,    38,  1136,  1138,    41,
      42,    43,    25,   962,  1178,   344,  1066,   607,    57,    51,
     885,    53,   843,   160,   161,   162,   111,   564,   673,   964,
    1421,   850,     5,     6,  1121,     8,    68,    69,    70,   690,
      13,    10,   855,    16,  1130,   115,   475,    20,    21,    22,
      23,    24,   424,    26,    27,    28,    29,   526,    31,    32,
      39,    -1,    35,    36,    37,    38,    -1,   720,    41,    42,
      43,    -1,   104,   105,    -1,    54,    -1,    -1,    51,    -1,
      53,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    70,    -1,    -1,    -1,    68,    69,    70,    -1,    72,
      79,    -1,    -1,   299,    -1,   301,    -1,    -1,    -1,   305,
     306,    -1,   308,    -1,    93,    94,    95,    96,    97,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   160,   161,
     162,   104,    39,    -1,    -1,    -1,    -1,    -1,    -1,    46,
      -1,    48,    49,    50,    -1,   118,    -1,   126,   127,   128,
     129,   130,   131,   132,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   299,    -1,   301,   197,    -1,    -1,   305,   306,
      -1,   308,    -1,    -1,    -1,     5,     6,    -1,     8,    -1,
      87,    88,    89,    13,    -1,    -1,    16,   160,   161,   162,
      20,    21,    22,    23,    24,    -1,    26,    27,    28,    29,
      -1,    31,    32,    -1,    -1,    35,    36,    37,    38,    -1,
      -1,    41,    42,    43,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    51,    -1,    53,    -1,    -1,    -1,   134,   135,   136,
      -1,   138,    -1,    -1,   141,    -1,    -1,    -1,    68,    69,
      70,    -1,    -1,    16,    -1,    -1,    -1,    -1,    -1,    22,
      23,    24,    -1,    26,    27,    28,    29,    -1,    31,    32,
      -1,    -1,    35,    36,    37,    38,    -1,   299,    -1,    42,
      43,    -1,    -1,    -1,   104,   105,   308,     5,     6,    -1,
       8,    -1,    -1,    -1,    -1,    13,    -1,    -1,    16,    -1,
      -1,    -1,    20,    21,    22,    23,    24,    -1,    26,    27,
      28,    29,    -1,    31,    32,    -1,    -1,    35,    36,    37,
      38,   290,    -1,    41,    42,    43,    -1,    -1,    91,    -1,
      -1,    -1,    -1,    51,    -1,    53,   299,    -1,    -1,    -1,
     160,   161,   162,    -1,    -1,   308,     5,     6,   111,     8,
      68,    69,    70,    -1,    13,    -1,    -1,    16,    -1,    -1,
      -1,    20,    21,    22,    23,    24,    -1,    26,    27,    28,
      29,    -1,    31,    32,    -1,    -1,    35,    36,    37,    38,
      -1,    -1,    41,    42,    43,    -1,   104,    -1,    -1,    -1,
      -1,    -1,    51,    -1,    53,    -1,    -1,    -1,    -1,   296,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    68,
      69,    70,    -1,    -1,    -1,    39,    -1,    -1,    -1,    -1,
      -1,    -1,    46,    -1,    48,    49,    50,    -1,     0,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   160,   161,   162,   104,    18,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,     5,
       6,    -1,     8,    87,    88,    89,    -1,    13,    -1,    -1,
      16,    -1,    -1,    -1,    20,    21,    22,    23,    24,   299,
      26,    27,    28,    29,    -1,    31,    32,    -1,   308,    35,
      36,    37,    38,    -1,    -1,    41,    42,    43,    -1,    -1,
      -1,   160,   161,   162,    -1,    51,    -1,    53,    -1,    -1,
     134,   135,   136,    -1,   138,    -1,    -1,   141,    -1,    -1,
      -1,    -1,    68,    69,    70,    -1,    -1,    -1,    -1,     5,
       6,    -1,     8,    -1,    -1,    -1,    -1,    13,    -1,    -1,
      16,    -1,    -1,    -1,    20,    21,    22,    23,    24,    -1,
      26,    27,    28,    29,    -1,    31,    32,    -1,   104,    35,
      36,    37,    38,    -1,    -1,    41,    42,    43,    -1,    39,
      14,    -1,    -1,    17,    -1,    51,    46,    53,    48,    49,
      50,   299,    -1,   155,   156,   157,   158,   159,    -1,    -1,
     308,    -1,    68,    69,    70,    -1,    40,    41,    -1,    -1,
      -1,    45,    -1,    47,    -1,   177,   178,    -1,    -1,    -1,
      54,    -1,    -1,    -1,   160,   161,   162,    87,    88,    89,
      -1,    -1,   194,   195,   196,   197,    -1,    71,   104,   201,
     202,    -1,    -1,    -1,    -1,   207,    -1,    -1,   210,    -1,
     299,    -1,    86,    -1,     5,   217,   218,     8,    -1,   308,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    18,    -1,    -1,
      -1,    -1,    -1,    -1,   134,   135,   136,    -1,   138,    -1,
      -1,   141,   296,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   160,   161,   162,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   273,   274,    -1,   276,   277,   278,    -1,   280,   281,
     282,   283,   284,   285,   286,   287,   288,    -1,    -1,    -1,
      -1,    15,    16,    17,    -1,   297,    20,    21,    22,    23,
      24,    -1,    26,    27,    28,    29,    -1,    31,    32,    -1,
      -1,    35,    36,    37,    38,    -1,    -1,    -1,    42,    43,
      -1,    -1,    -1,   299,    -1,    -1,    -1,    51,    -1,    53,
      -1,    -1,   308,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    68,    69,    70,    -1,    -1,    -1,
      -1,   142,   143,   144,   145,   146,   147,   148,   149,   150,
     151,   152,   153,   154,     5,     6,    -1,     8,   159,    -1,
      -1,    -1,   163,   164,   165,   166,   167,   168,     5,    -1,
     104,     8,    -1,    -1,    -1,    -1,   177,   178,    -1,    -1,
      -1,    18,    -1,   299,    -1,    -1,   296,    -1,    39,    -1,
      -1,    -1,   308,   194,   195,   196,   197,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   205,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    68,   219,    -1,
      -1,    72,    -1,    -1,    75,    76,    -1,    -1,    -1,    80,
      81,    82,    83,    84,    85,    86,    87,    88,    89,    90,
      91,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    99,    -1,
      -1,   102,     5,   254,   255,     8,    -1,    -1,   109,    -1,
      -1,    -1,    -1,    -1,    -1,    18,    -1,   118,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   282,   283,   284,   285,   286,   287,   288,    -1,   290,
     291,    -1,    -1,    -1,    -1,    -1,   297,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   142,   143,   144,   145,   146,
     147,   148,   149,   150,   151,   152,   153,   154,    -1,    -1,
      -1,    -1,   159,    -1,    -1,    -1,   163,   164,   165,   166,
     167,   168,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     177,   178,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   194,   195,   196,
     197,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   205,    -1,
      -1,    -1,    -1,    -1,   308,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   219,    -1,    -1,    -1,    -1,    -1,    -1,   142,
     143,   144,   145,   146,   147,   148,   149,   150,   151,   152,
     153,   154,    -1,    -1,    -1,    -1,   159,    -1,    -1,    -1,
     163,   164,   165,   166,   167,   168,    -1,   254,   255,    -1,
      -1,    -1,    -1,    -1,   177,   178,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   194,   195,   196,   197,   282,   283,   284,   285,   286,
     287,   288,   205,   290,   291,    -1,    -1,    16,    -1,    -1,
     297,    20,    21,    22,    23,    24,   219,    26,    27,    28,
      29,    -1,    31,    32,    -1,    -1,    35,    36,    37,    38,
      39,    -1,    -1,    42,    43,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    51,    -1,    53,    -1,    -1,    -1,    -1,    -1,
      -1,   254,   255,    -1,    -1,    -1,    -1,    -1,    -1,    68,
      69,    70,    -1,    72,    -1,    74,    75,    76,    77,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   282,
     283,   284,   285,   286,   287,   288,    -1,   290,   291,    -1,
      -1,    -1,    -1,    -1,   297,   104,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   112,   113,   114,   115,    -1,    -1,   118,
      -1,    -1,    16,   122,   123,   124,    20,    21,    22,    23,
      24,    -1,    26,    27,    28,    29,    -1,    31,    32,    -1,
      -1,    35,    36,    37,    38,    -1,    -1,    -1,    42,    43,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    51,    -1,    53,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    18,    68,    69,    70,    16,    -1,    -1,
      -1,    20,    21,    22,    23,    24,    -1,    26,    27,    28,
      29,    -1,    31,    32,    -1,    -1,    35,    36,    37,    38,
      -1,    -1,    -1,    42,    43,    -1,    -1,    -1,    -1,    -1,
     104,    -1,    51,    -1,    53,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   226,    -1,    68,
      69,    70,    16,    -1,    -1,    -1,    20,    21,    22,    23,
      24,    -1,    26,    27,    28,    29,    -1,    31,    32,    -1,
      -1,    35,    36,    37,    38,    -1,    -1,    -1,    42,    43,
      -1,    -1,    -1,    -1,    -1,   104,    -1,    51,    -1,    53,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    68,    69,    70,    -1,    -1,    21,
      22,    -1,    24,    25,    26,    27,    28,    29,    -1,    31,
      32,    -1,    34,    35,    36,    37,    38,    -1,    -1,   308,
     155,   156,   157,   158,   159,    -1,    -1,    -1,    -1,    -1,
     104,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   177,   178,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   194,
     195,   196,   197,    -1,    -1,    -1,   201,   202,    -1,    -1,
      -1,    -1,   207,    -1,    -1,   210,    -1,    -1,    -1,    -1,
      -1,    -1,   217,   218,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    22,    23,    24,    -1,    26,    27,    28,    29,
      -1,    31,    32,    33,    -1,    35,    36,    37,    38,    -1,
      -1,    -1,    42,    43,    18,   299,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   308,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   273,   274,
      -1,   276,   277,   278,    -1,   280,   281,   282,   283,   284,
     285,   286,   287,   288,    -1,    -1,   291,    -1,    -1,    -1,
      -1,    18,   297,    67,    -1,    -1,    -1,    -1,    -1,    -1,
     299,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   308,
      -1,   111,   179,   180,   181,   182,   183,   184,   185,   186,
     187,   188,   189,   190,   191,   192,   193,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   230,   231,
      67,   233,   234,   235,    -1,   237,   238,    -1,    -1,   241,
     242,    -1,   244,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   308,   257,   258,   259,   260,   261,
     262,   263,   264,   265,   266,   267,   268,   269,   270,   271,
      -1,   155,    -1,   157,   158,   159,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   169,    -1,    -1,    -1,    -1,
     174,    -1,    -1,   177,   178,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   305,    -1,    -1,    -1,    -1,    -1,    -1,
     194,   195,   196,   197,    -1,   199,   200,    -1,   155,    -1,
     157,   158,   159,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   169,    -1,    -1,   219,    -1,   174,    -1,    -1,
     177,   178,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   194,   195,   196,
     197,    -1,   199,   200,    -1,    -1,    -1,    -1,    -1,    -1,
     254,   255,   282,   283,   284,   285,   286,   287,   288,    -1,
      -1,    -1,   219,    -1,    -1,    -1,    -1,   297,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   282,   283,
     284,   285,   286,   287,   288,    -1,    -1,   291,    -1,    -1,
      -1,    -1,    -1,   297,    16,    -1,    -1,   254,   255,    21,
      22,    -1,    24,    25,    26,    27,    28,    29,    -1,    31,
      32,    -1,    34,    35,    36,    37,    38,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   282,   283,   284,   285,   286,
     287,   288,    -1,    16,   291,    -1,    -1,    -1,    21,    22,
     297,    24,    25,    26,    27,    28,    29,    -1,    31,    32,
      -1,    34,    35,    36,    37,    38,    -1,    -1,    80,    -1,
      82,    -1,    -1,    -1,    -1,    87,    -1,    -1,    -1,    -1,
      39,    40,    -1,    -1,    -1,    -1,    45,    -1,    47,    -1,
      -1,    -1,   104,    -1,    -1,    54,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    80,    -1,    82,
      -1,    -1,    71,    72,    87,    74,    75,    76,    77,    78,
      -1,    -1,    -1,    -1,    83,   137,    -1,    86,    -1,    -1,
      -1,   104,    -1,    92,    -1,    -1,    -1,    -1,    -1,    98,
      -1,   100,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   112,   113,   114,   115,   116,   117,   118,
     119,    -1,    -1,    -1,   137,    -1,   125,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   227,    -1,   229,   230,   231,
     232,   233,   234,   235,   236,   237,   238,   239,   240,   241,
     242,   243,   244,   245,    -1,    -1,    -1,   249,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   258,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   227,    -1,   229,   230,   231,   232,
     233,   234,   235,   236,   237,   238,   239,   240,   241,   242,
     243,   244,   245,    -1,    -1,    -1,   249,    -1,   290,    -1,
      -1,    -1,    -1,    -1,    -1,   258
};

/* YYSTOS[STATE-NUM] -- The symbol kind of the accessing symbol of
   state STATE-NUM.  */
static const yytype_int16 yystos[] =
{
       0,   310,     0,    18,   155,   156,   157,   158,   159,   177,
     178,   194,   195,   196,   197,   201,   202,   207,   210,   217,
     218,   273,   274,   276,   277,   278,   280,   281,   282,   283,
     284,   285,   286,   287,   288,   297,   311,   314,   320,   321,
     322,   323,   324,   325,   332,   334,   335,   337,   338,   339,
     340,   341,   342,   359,   377,   381,   403,   404,   459,   462,
     468,   469,   470,   474,   483,   486,   491,   216,     5,     6,
       8,   315,   316,   299,   363,    64,   126,   405,   179,   180,
     181,   182,   183,   184,   185,   186,   187,   188,   189,   190,
     191,   192,   193,   467,   467,     8,    14,    17,    40,    41,
      45,    47,    54,    71,    86,   295,   326,   364,   365,   366,
     367,   298,   299,   275,   471,   216,   475,   492,   216,   316,
       9,   317,   317,     9,    10,   318,   318,    13,    16,    20,
      21,    22,    23,    24,    26,    27,    28,    29,    31,    32,
      35,    36,    37,    38,    42,    43,    51,    53,    68,    69,
      70,   104,   105,   160,   161,   162,   299,   308,   316,   322,
     323,   367,   368,   426,   449,   450,   455,   456,   290,   316,
     316,   316,   316,     7,    12,   412,   413,   412,   412,   290,
     343,    60,   344,   290,   382,   388,    23,    26,    27,    28,
      29,    31,    32,    33,   290,   306,   406,   409,   411,   412,
     317,   290,   290,   290,   290,   488,   294,   317,   360,   315,
     299,   367,   426,   449,   451,   455,     7,    33,   298,   313,
     293,   295,   295,    46,    48,    49,    50,   365,   365,   327,
     368,   451,   298,   455,   295,   317,   317,   208,   316,   475,
     101,   102,   103,   126,   220,   222,   223,   224,   225,   316,
      75,    76,   316,   316,   455,    26,    27,    28,    29,   449,
      51,   449,    24,    25,    34,    15,    17,   455,   218,   305,
     316,   367,   308,   316,   317,   137,   137,   137,   364,   365,
     137,   307,   106,   107,   108,   137,   299,   301,   305,   306,
     312,   449,   313,   296,    12,   296,   296,   310,    39,    68,
      72,    75,    76,    80,    81,    82,    83,    84,    85,    86,
      87,    88,    89,    90,    91,    99,   102,   109,   118,   316,
     451,    61,   345,   346,    39,    72,    74,    75,    76,    77,
      78,    83,    92,    98,   100,   112,   113,   114,   115,   116,
     117,   118,   119,   125,   365,   142,   143,   144,   145,   146,
     147,   148,   149,   150,   151,   152,   153,   154,   163,   164,
     165,   166,   167,   168,   205,   219,   254,   255,   290,   291,
     314,   315,   321,   332,   387,   389,   390,   391,   392,   394,
     395,   403,   427,   428,   429,   430,   431,   432,   433,   434,
     435,   436,   437,   438,   439,   440,   441,   459,   469,   305,
     295,   299,   408,   295,   408,   295,   408,   295,   408,   295,
     408,   295,   408,   295,   407,   409,   295,   412,   296,     7,
       8,   293,   304,   476,   484,   489,   493,    73,    75,    76,
      82,   316,   316,   300,    39,    72,    74,    75,    76,    77,
     112,   113,   114,   115,   118,   122,   123,   124,   226,   455,
     298,   218,   316,   365,   295,   298,   295,   290,   295,   292,
       8,   317,   317,   296,   290,   295,   313,   120,   121,   299,
     316,   384,   451,   300,   167,   472,   316,   221,   137,   449,
      25,   316,   451,   316,   300,   300,   300,   316,   317,   316,
     316,   316,   455,   316,   316,   295,   295,   316,    20,   300,
     317,   457,   458,   444,   445,   455,   291,   312,   291,   295,
      75,    76,    77,   112,   113,   114,   301,   350,   347,   451,
      67,   155,   169,   174,   199,   200,   219,   254,   255,   291,
     314,   321,   332,   342,   358,   359,   369,   373,   381,   403,
     459,   469,   487,   295,   295,   385,   317,   317,   317,   299,
     111,   289,   299,   104,   451,   304,   198,   295,   388,    55,
      57,    58,    59,   393,   396,   397,   398,   399,   400,   401,
     315,   317,   390,   315,   317,   317,   318,    11,    31,    32,
     295,   318,   319,   315,   317,   364,    15,    17,   367,   455,
     451,    87,   313,   411,   365,   327,   295,   412,   295,   317,
     317,   317,   317,   318,   319,   319,   291,   293,   315,   296,
     317,   317,   209,   211,   214,   215,   291,   321,   332,   459,
     477,   479,   480,   482,    84,   209,   212,   291,   473,   479,
     481,   485,    41,   155,   207,   210,   291,   321,   332,   490,
     207,   210,   291,   321,   332,   494,    75,    76,    77,   112,
     113,   114,   295,   295,   316,   316,   300,   455,   313,   463,
     464,   290,    51,   451,   460,   461,     7,   293,   296,   296,
     326,   328,   329,   301,   357,   443,    19,   336,   473,   137,
     316,    19,   300,   450,   450,   450,   305,   451,   451,    20,
     293,   300,   302,   293,   317,    39,    51,    52,    69,   120,
     292,   303,   351,   352,   353,   293,   111,   370,   374,   317,
     317,   488,   111,   289,   104,   451,   290,   290,   290,   388,
     290,   317,   313,   383,   299,   455,   304,   317,   299,   316,
     299,   316,   317,   365,    19,   295,    20,   385,   446,   447,
     448,   291,   451,   393,    56,   390,   402,   315,   317,   390,
     402,   402,   402,    62,    62,   295,   295,   316,   451,   295,
     412,   455,   315,   317,   442,   296,   313,   296,   300,   296,
     296,   296,   296,   296,   407,   296,   304,     8,   293,   213,
     298,   305,   317,   478,   298,   313,   412,   412,   298,   298,
     412,   412,   295,   216,   317,   316,   216,   316,   216,   317,
      16,    21,    22,    24,    25,    26,    27,    28,    29,    31,
      32,    34,    35,    36,    37,    38,    80,    82,    87,   104,
     137,   227,   229,   230,   231,   232,   233,   234,   235,   236,
     237,   238,   239,   240,   241,   242,   243,   244,   245,   249,
     258,   290,   379,   380,   452,    63,   361,   300,   298,   296,
     293,   328,     8,   298,   291,   293,     8,   298,   291,    22,
      23,    24,    26,    27,    28,    29,    31,    32,    35,    36,
      37,    38,    42,    43,   111,   321,   330,   410,   411,   415,
     299,   444,   295,   295,   316,   384,    28,    29,    63,   203,
     204,   206,   412,   316,   316,   450,   295,   296,   296,   317,
     458,   455,   296,   295,   352,   295,   316,   355,   302,   451,
     451,    72,   118,   316,   451,    72,   118,   365,   316,   299,
     316,   299,   316,   365,    19,   346,   371,   375,   291,   489,
     296,   137,   383,    39,    46,    48,    49,    50,    87,    88,
      89,   134,   135,   136,   138,   141,   296,   251,   252,   253,
     317,   226,   378,   317,   300,   317,   317,   293,   300,   455,
     384,   446,   455,   296,   293,   315,   317,   315,   317,   317,
     318,    19,   313,   296,   295,   293,   293,   296,   296,   408,
     408,   408,   408,   408,   408,   317,   317,   317,   295,   304,
     295,   296,   296,   295,   295,   296,   296,   317,   450,   316,
      63,   316,   296,    25,    26,    27,    28,    29,   295,   453,
     243,   235,   247,   295,   228,   248,    22,   453,   453,    21,
      22,    24,    25,    26,    27,    28,    29,    31,    32,    34,
      35,    36,    37,    38,   230,   231,   233,   234,   235,   237,
     238,   241,   242,   244,   257,   258,   259,   260,   261,   262,
     263,   264,   265,   266,   267,   268,   269,   270,   271,   305,
     454,   296,   413,   299,   305,   315,   298,   362,    28,    65,
      66,   313,   317,   449,   465,   466,   463,   291,   298,   290,
     460,   290,   295,   313,   295,   299,   295,   299,    26,    27,
      28,    29,   295,   299,   295,   299,   295,   299,   295,   299,
     295,   299,   295,   299,   295,   299,   295,   299,   295,   299,
     295,   299,   295,   299,   295,   299,   295,   299,   105,   110,
     321,   331,   412,   317,   302,   446,   446,   357,   443,   315,
     296,   446,   317,   348,   349,   451,   293,   354,   316,   197,
     322,   316,   455,   317,   317,   293,   455,   384,   291,   170,
     171,   172,   173,   291,   314,   321,   332,   372,   469,   173,
     175,   176,   291,   314,   321,   332,   376,   469,   291,   313,
     296,   295,   304,   304,   300,   300,   300,   300,   295,   384,
     137,   300,   300,   451,   362,   451,   296,   378,   448,    62,
      62,   296,   296,   316,   296,   446,   442,   442,     8,   293,
       8,   478,   296,   317,   250,   313,   299,   299,    25,    26,
      27,    28,    29,   272,   293,   299,   306,   291,   292,   300,
     317,    22,    23,    24,    26,    27,    28,    29,    31,    32,
      35,    36,    37,    38,    44,   313,   410,   414,   295,   295,
     290,   330,   328,   465,   317,   317,   317,   295,   299,   295,
     299,   295,   299,   295,   299,   317,   317,   317,   317,   317,
     317,   318,   317,   317,   319,   317,   318,   319,   317,   317,
     317,   317,   317,   317,   317,   318,   317,   415,   317,     8,
      44,   317,    44,    51,   449,   317,    42,    91,   111,   333,
     456,   296,   300,   296,   296,   295,   295,   472,   296,   296,
     296,   293,   353,   354,   316,   300,   300,   451,   451,   256,
     364,   364,   364,   364,   364,   364,   364,   383,   317,   139,
     140,   139,   140,   379,   350,   315,   293,    19,   315,   315,
     317,   296,   317,   304,   298,   293,   317,   317,   313,   300,
     317,   292,   300,    26,    27,    28,    29,   317,    26,    27,
      28,   317,   330,   291,   291,   296,   300,   296,   300,   317,
     317,   317,   317,   317,   317,   318,   317,   296,   300,   296,
     300,   296,   300,   296,   300,   296,   296,   300,   296,   296,
     300,   296,   300,   296,   300,   296,   300,   296,   300,   296,
     300,   296,   296,   300,   296,     8,   296,   300,    51,   449,
     299,   316,   302,   446,   446,   451,   295,   293,    19,   365,
     296,   296,   296,   295,   451,   384,     8,   478,   317,   313,
     300,   300,   300,   317,   296,   304,   304,   304,   296,   291,
     295,   295,   296,   300,   296,   300,   296,   300,   296,   300,
     295,   295,   295,   295,   295,   295,   295,   295,   295,   295,
     295,   295,   296,   295,     8,   300,   298,   296,   296,   446,
     451,   384,   455,   446,   301,   356,   357,   304,   296,   293,
     296,   452,   300,   317,   317,   317,   422,   420,   295,   295,
     295,   295,   421,   420,   419,   418,   416,   417,   421,   420,
     419,   418,   425,   423,   424,   415,   296,   356,   451,   296,
     295,   478,   313,   296,   296,   296,   296,   465,   296,   317,
     421,   420,   419,   418,   296,   317,   296,   296,   317,   296,
     318,   296,   317,   319,   296,   318,   319,   296,   296,   296,
     296,   296,   415,     8,    44,   296,    44,    51,   296,   449,
     362,   295,    19,   386,   446,   293,   296,   296,   296,   296,
       8,   446,   384,    39,    54,    70,    79,    93,    94,    95,
      96,    97,   126,   127,   128,   129,   130,   131,   132,   290,
     296,   313,   296,   295,   295,   296,   256,   446,   317,   104,
     296,   296,   365,   455,   451,    19,   384,   356,   295,   446,
     296
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
     353,   354,   354,   355,   356,   356,   357,   358,   358,   358,
     358,   358,   358,   358,   358,   358,   358,   358,   358,   358,
     358,   358,   358,   358,   358,   358,   358,   358,   359,   360,
     360,   360,   360,   360,   360,   360,   360,   360,   360,   360,
     360,   360,   360,   360,   360,   361,   361,   362,   362,   363,
     363,   364,   364,   364,   364,   364,   364,   364,   365,   365,
     365,   365,   366,   366,   366,   366,   366,   366,   366,   366,
     367,   368,   368,   368,   368,   368,   368,   369,   369,   370,
     370,   370,   371,   371,   372,   372,   372,   372,   372,   372,
     372,   372,   373,   374,   374,   374,   375,   375,   376,   376,
     376,   376,   376,   376,   376,   377,   378,   378,   379,   379,
     380,   381,   382,   382,   382,   382,   382,   382,   382,   382,
     382,   382,   382,   382,   382,   382,   382,   382,   382,   382,
     382,   382,   382,   382,   382,   383,   383,   383,   383,   383,
     383,   383,   383,   383,   383,   383,   383,   383,   383,   383,
     383,   384,   384,   384,   385,   385,   385,   385,   385,   386,
     386,   386,   386,   386,   386,   386,   386,   386,   386,   386,
     386,   386,   386,   386,   386,   386,   387,   388,   388,   389,
     389,   389,   389,   389,   389,   389,   389,   389,   389,   389,
     389,   389,   389,   389,   389,   389,   389,   389,   389,   389,
     389,   389,   389,   389,   389,   390,   391,   392,   393,   393,
     394,   394,   394,   395,   396,   396,   396,   396,   397,   397,
     397,   398,   399,   400,   401,   402,   402,   402,   403,   404,
     404,   405,   405,   405,   406,   406,   407,   407,   408,   408,
     409,   409,   409,   409,   409,   409,   409,   409,   409,   409,
     409,   409,   409,   409,   409,   410,   410,   410,   410,   410,
     410,   410,   410,   410,   410,   410,   410,   410,   410,   410,
     410,   410,   410,   410,   411,   412,   412,   413,   413,   414,
     414,   414,   415,   415,   415,   415,   415,   415,   415,   415,
     415,   415,   415,   415,   415,   415,   415,   415,   415,   415,
     415,   415,   415,   415,   415,   415,   415,   415,   416,   416,
     416,   417,   417,   417,   418,   418,   419,   419,   420,   420,
     421,   421,   422,   422,   423,   423,   423,   424,   424,   424,
     424,   425,   425,   426,   427,   428,   429,   430,   431,   432,
     433,   434,   435,   436,   437,   438,   439,   440,   441,   441,
     441,   441,   441,   441,   441,   441,   441,   441,   441,   441,
     441,   441,   441,   441,   441,   441,   441,   441,   441,   441,
     441,   442,   442,   442,   442,   442,   443,   443,   444,   444,
     445,   445,   446,   446,   447,   447,   448,   448,   448,   449,
     449,   449,   449,   449,   449,   449,   449,   449,   449,   450,
     450,   451,   451,   451,   451,   452,   452,   452,   452,   452,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     453,   453,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   455,   455,   455,   455,   455,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   455,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   456,   456,   456,   456,   456,
     456,   456,   456,   456,   456,   456,   456,   456,   456,   456,
     456,   456,   456,   457,   457,   458,   458,   458,   458,   458,
     459,   459,   459,   459,   459,   459,   460,   460,   460,   461,
     461,   462,   462,   463,   463,   464,   465,   465,   466,   466,
     466,   466,   466,   466,   466,   466,   467,   467,   467,   467,
     467,   467,   467,   467,   467,   467,   467,   467,   467,   467,
     467,   468,   468,   469,   469,   469,   469,   469,   469,   469,
     469,   469,   469,   469,   470,   470,   471,   471,   472,   472,
     473,   474,   475,   475,   475,   475,   475,   475,   475,   475,
     475,   475,   476,   476,   477,   477,   477,   478,   478,   479,
     479,   479,   479,   479,   479,   480,   481,   482,   483,   483,
     484,   484,   485,   485,   485,   485,   486,   487,   488,   488,
     488,   488,   488,   488,   488,   488,   488,   488,   489,   489,
     490,   490,   490,   490,   490,   490,   490,   491,   491,   492,
     492,   492,   493,   493,   494,   494,   494,   494
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
       3,     0,     2,     3,     0,     1,     5,     3,     4,     4,
       4,     1,     1,     1,     1,     1,     2,     2,     4,    13,
      22,     1,     1,     5,     3,     7,     5,     4,     7,     0,
       2,     2,     2,     2,     2,     2,     2,     5,     2,     2,
       2,     2,     2,     2,     5,     0,     2,     0,     2,     0,
       3,     9,     9,     7,     7,     1,     1,     1,     2,     2,
       1,     4,     0,     1,     1,     2,     2,     2,     2,     1,
       4,     2,     5,     3,     2,     2,     1,     4,     3,     0,
       2,     2,     0,     2,     2,     2,     2,     2,     1,     1,
       1,     1,     9,     0,     2,     2,     0,     2,     2,     2,
       2,     1,     1,     1,     1,     1,     0,     4,     1,     3,
       1,    13,     0,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     5,     8,     6,     5,     0,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     4,     4,     4,     4,
       5,     1,     1,     1,     0,     4,     4,     4,     4,     0,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     5,     1,     0,     2,     2,
       1,     2,     4,     5,     1,     1,     1,     1,     2,     1,
       1,     1,     1,     1,     4,     6,     4,     4,    11,     1,
       5,     3,     7,     5,     5,     3,     1,     2,     2,     1,
       2,     4,     4,     1,     2,     2,     2,     2,     2,     2,
       2,     1,     2,     1,     1,     1,     4,     4,     2,     4,
       2,     0,     1,     1,     3,     1,     3,     1,     0,     3,
       5,     4,     3,     5,     5,     5,     5,     5,     5,     2,
       2,     2,     2,     2,     2,     4,     4,     4,     4,     4,
       4,     4,     4,     5,     5,     5,     5,     4,     4,     4,
       4,     4,     4,     3,     2,     0,     1,     1,     2,     1,
       1,     1,     1,     4,     4,     5,     4,     4,     4,     7,
       7,     7,     7,     7,     7,     7,     7,     7,     7,     8,
       8,     8,     8,     7,     7,     7,     7,     7,     0,     2,
       2,     0,     2,     2,     0,     2,     0,     2,     0,     2,
       0,     2,     0,     2,     0,     2,     2,     0,     2,     3,
       2,     0,     2,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     2,     1,     2,
       2,     2,     2,     2,     2,     3,     2,     2,     2,     5,
       3,     2,     2,     2,     2,     2,     5,     4,     6,     2,
       4,     0,     3,     3,     1,     1,     0,     3,     0,     1,
       1,     3,     0,     1,     1,     3,     1,     3,     4,     4,
       4,     4,     5,     1,     1,     1,     1,     1,     1,     1,
       3,     1,     3,     4,     1,     0,    10,     6,     5,     6,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     2,     2,     2,     2,     1,     1,     1,     1,
       2,     3,     4,     6,     5,     1,     1,     1,     1,     1,
       1,     1,     2,     2,     1,     2,     2,     4,     1,     2,
       1,     2,     1,     2,     1,     2,     1,     2,     1,     1,
       0,     5,     0,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     2,     2,     2,     2,     1,     1,
       1,     1,     1,     3,     2,     2,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     2,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     2,     1,     3,     2,     3,     4,     2,     2,     2,
       5,     5,     7,     4,     3,     2,     3,     2,     1,     1,
       2,     3,     2,     1,     2,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     2,     2,     2,     2,     1,     1,
       1,     1,     1,     1,     3,     0,     1,     1,     3,     2,
       6,     7,     3,     3,     3,     6,     0,     1,     3,     5,
       6,     4,     4,     1,     3,     3,     1,     1,     1,     1,
       4,     1,     6,     6,     6,     4,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     3,     2,     5,     4,     7,     6,     7,
       6,     9,     8,     3,     8,     4,     0,     2,     0,     1,
       3,     3,     0,     2,     2,     2,     3,     2,     2,     2,
       2,     2,     0,     2,     3,     1,     1,     1,     1,     3,
       8,     2,     3,     1,     1,     3,     3,     3,     4,     6,
       0,     2,     3,     1,     3,     1,     4,     3,     0,     2,
       2,     2,     3,     3,     3,     3,     3,     3,     0,     2,
       2,     3,     3,     4,     2,     1,     1,     3,     5,     0,
       2,     2,     0,     2,     4,     3,     1,     1
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
#line 193 "asmparse.y"
                                                                                { PASM->EndClass(); }
#line 3545 "prebuilt\\asmparse.cpp"
    break;

  case 5: /* decl: nameSpaceHead '{' decls '}'  */
#line 194 "asmparse.y"
                                                                                { PASM->EndNameSpace(); }
#line 3551 "prebuilt\\asmparse.cpp"
    break;

  case 6: /* decl: methodHead methodDecls '}'  */
#line 195 "asmparse.y"
                                                                                { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                                                  {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                                     PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                                                  PASM->EndMethod(); }
#line 3560 "prebuilt\\asmparse.cpp"
    break;

  case 13: /* decl: assemblyHead '{' assemblyDecls '}'  */
#line 205 "asmparse.y"
                                                                                { PASMM->EndAssembly(); }
#line 3566 "prebuilt\\asmparse.cpp"
    break;

  case 14: /* decl: assemblyRefHead '{' assemblyRefDecls '}'  */
#line 206 "asmparse.y"
                                                                                { PASMM->EndAssembly(); }
#line 3572 "prebuilt\\asmparse.cpp"
    break;

  case 15: /* decl: exptypeHead '{' exptypeDecls '}'  */
#line 207 "asmparse.y"
                                                                                { PASMM->EndComType(); }
#line 3578 "prebuilt\\asmparse.cpp"
    break;

  case 16: /* decl: manifestResHead '{' manifestResDecls '}'  */
#line 208 "asmparse.y"
                                                                                { PASMM->EndManifestRes(); }
#line 3584 "prebuilt\\asmparse.cpp"
    break;

  case 20: /* decl: _SUBSYSTEM int32  */
#line 212 "asmparse.y"
                                                                                {
                                                                                  PASM->m_dwSubsystem = (yyvsp[0].int32);
                                                                                }
#line 3592 "prebuilt\\asmparse.cpp"
    break;

  case 21: /* decl: _CORFLAGS int32  */
#line 215 "asmparse.y"
                                                                                { PASM->m_dwComImageFlags = (yyvsp[0].int32); }
#line 3598 "prebuilt\\asmparse.cpp"
    break;

  case 22: /* decl: _FILE ALIGNMENT_ int32  */
#line 216 "asmparse.y"
                                                                                { PASM->m_dwFileAlignment = (yyvsp[0].int32);
                                                                                  if(((yyvsp[0].int32) & ((yyvsp[0].int32) - 1))||((yyvsp[0].int32) < 0x200)||((yyvsp[0].int32) > 0x10000))
                                                                                    PASM->report->error("Invalid file alignment, must be power of 2 from 0x200 to 0x10000\n");}
#line 3606 "prebuilt\\asmparse.cpp"
    break;

  case 23: /* decl: _IMAGEBASE int64  */
#line 219 "asmparse.y"
                                                                                { PASM->m_stBaseAddress = (ULONGLONG)(*((yyvsp[0].int64))); delete (yyvsp[0].int64);
                                                                                  if(PASM->m_stBaseAddress & 0xFFFF)
                                                                                    PASM->report->error("Invalid image base, must be 0x10000-aligned\n");}
#line 3614 "prebuilt\\asmparse.cpp"
    break;

  case 24: /* decl: _STACKRESERVE int64  */
#line 222 "asmparse.y"
                                                                                { PASM->m_stSizeOfStackReserve = (size_t)(*((yyvsp[0].int64))); delete (yyvsp[0].int64); }
#line 3620 "prebuilt\\asmparse.cpp"
    break;

  case 29: /* decl: _MSCORLIB  */
#line 227 "asmparse.y"
                                                                                { PASM->m_fIsMscorlib = TRUE; }
#line 3626 "prebuilt\\asmparse.cpp"
    break;

  case 32: /* compQstring: QSTRING  */
#line 234 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[0].binstr); }
#line 3632 "prebuilt\\asmparse.cpp"
    break;

  case 33: /* compQstring: compQstring '+' QSTRING  */
#line 235 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 3638 "prebuilt\\asmparse.cpp"
    break;

  case 34: /* languageDecl: _LANGUAGE SQSTRING  */
#line 238 "asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[0].string),&(PASM->m_guidLang)); }
#line 3644 "prebuilt\\asmparse.cpp"
    break;

  case 35: /* languageDecl: _LANGUAGE SQSTRING ',' SQSTRING  */
#line 239 "asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid((yyvsp[0].string),&(PASM->m_guidLangVendor));}
#line 3651 "prebuilt\\asmparse.cpp"
    break;

  case 36: /* languageDecl: _LANGUAGE SQSTRING ',' SQSTRING ',' SQSTRING  */
#line 241 "asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[-4].string),&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidLangVendor));
                                                                                  LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidDoc));}
#line 3659 "prebuilt\\asmparse.cpp"
    break;

  case 37: /* id: ID  */
#line 246 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3665 "prebuilt\\asmparse.cpp"
    break;

  case 38: /* id: SQSTRING  */
#line 247 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3671 "prebuilt\\asmparse.cpp"
    break;

  case 39: /* dottedName: id  */
#line 250 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3677 "prebuilt\\asmparse.cpp"
    break;

  case 40: /* dottedName: DOTTEDNAME  */
#line 251 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3683 "prebuilt\\asmparse.cpp"
    break;

  case 41: /* dottedName: dottedName '.' dottedName  */
#line 252 "asmparse.y"
                                                              { (yyval.string) = newStringWDel((yyvsp[-2].string), '.', (yyvsp[0].string)); }
#line 3689 "prebuilt\\asmparse.cpp"
    break;

  case 42: /* int32: INT32_V  */
#line 255 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 3695 "prebuilt\\asmparse.cpp"
    break;

  case 43: /* int64: INT64_V  */
#line 258 "asmparse.y"
                                                              { (yyval.int64) = (yyvsp[0].int64); }
#line 3701 "prebuilt\\asmparse.cpp"
    break;

  case 44: /* int64: INT32_V  */
#line 259 "asmparse.y"
                                                              { (yyval.int64) = neg ? new int64_t((yyvsp[0].int32)) : new int64_t((unsigned)(yyvsp[0].int32)); }
#line 3707 "prebuilt\\asmparse.cpp"
    break;

  case 45: /* float64: FLOAT64  */
#line 262 "asmparse.y"
                                                              { (yyval.float64) = (yyvsp[0].float64); }
#line 3713 "prebuilt\\asmparse.cpp"
    break;

  case 46: /* float64: FLOAT32_ '(' int32 ')'  */
#line 263 "asmparse.y"
                                                              { float f; *((int32_t*) (&f)) = (yyvsp[-1].int32); (yyval.float64) = new double(f); }
#line 3719 "prebuilt\\asmparse.cpp"
    break;

  case 47: /* float64: FLOAT64_ '(' int64 ')'  */
#line 264 "asmparse.y"
                                                              { (yyval.float64) = (double*) (yyvsp[-1].int64); }
#line 3725 "prebuilt\\asmparse.cpp"
    break;

  case 48: /* typedefDecl: _TYPEDEF type AS_ dottedName  */
#line 268 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].binstr),(yyvsp[0].string)); }
#line 3731 "prebuilt\\asmparse.cpp"
    break;

  case 49: /* typedefDecl: _TYPEDEF className AS_ dottedName  */
#line 269 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].token),(yyvsp[0].string)); }
#line 3737 "prebuilt\\asmparse.cpp"
    break;

  case 50: /* typedefDecl: _TYPEDEF memberRef AS_ dottedName  */
#line 270 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].token),(yyvsp[0].string)); }
#line 3743 "prebuilt\\asmparse.cpp"
    break;

  case 51: /* typedefDecl: _TYPEDEF customDescr AS_ dottedName  */
#line 271 "asmparse.y"
                                                                                { (yyvsp[-2].cad)->tkOwner = 0; PASM->AddTypeDef((yyvsp[-2].cad),(yyvsp[0].string)); }
#line 3749 "prebuilt\\asmparse.cpp"
    break;

  case 52: /* typedefDecl: _TYPEDEF customDescrWithOwner AS_ dottedName  */
#line 272 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].cad),(yyvsp[0].string)); }
#line 3755 "prebuilt\\asmparse.cpp"
    break;

  case 53: /* compControl: P_DEFINE dottedName  */
#line 277 "asmparse.y"
                                                                                { DefineVar((yyvsp[0].string), NULL); }
#line 3761 "prebuilt\\asmparse.cpp"
    break;

  case 54: /* compControl: P_DEFINE dottedName compQstring  */
#line 278 "asmparse.y"
                                                                                { DefineVar((yyvsp[-1].string), (yyvsp[0].binstr)); }
#line 3767 "prebuilt\\asmparse.cpp"
    break;

  case 55: /* compControl: P_UNDEF dottedName  */
#line 279 "asmparse.y"
                                                                                { UndefVar((yyvsp[0].string)); }
#line 3773 "prebuilt\\asmparse.cpp"
    break;

  case 56: /* compControl: P_IFDEF dottedName  */
#line 280 "asmparse.y"
                                                                                { SkipToken = !IsVarDefined((yyvsp[0].string));
                                                                                  IfEndif++;
                                                                                }
#line 3781 "prebuilt\\asmparse.cpp"
    break;

  case 57: /* compControl: P_IFNDEF dottedName  */
#line 283 "asmparse.y"
                                                                                { SkipToken = IsVarDefined((yyvsp[0].string));
                                                                                  IfEndif++;
                                                                                }
#line 3789 "prebuilt\\asmparse.cpp"
    break;

  case 58: /* compControl: P_ELSE  */
#line 286 "asmparse.y"
                                                                                { if(IfEndif == 1) SkipToken = !SkipToken;}
#line 3795 "prebuilt\\asmparse.cpp"
    break;

  case 59: /* compControl: P_ENDIF  */
#line 287 "asmparse.y"
                                                                                { if(IfEndif == 0)
                                                                                    PASM->report->error("Unmatched #endif\n");
                                                                                  else IfEndif--;
                                                                                }
#line 3804 "prebuilt\\asmparse.cpp"
    break;

  case 60: /* compControl: P_INCLUDE QSTRING  */
#line 291 "asmparse.y"
                                                                                { _ASSERTE(!"yylex should have dealt with this"); }
#line 3810 "prebuilt\\asmparse.cpp"
    break;

  case 61: /* compControl: ';'  */
#line 292 "asmparse.y"
                                                                                { }
#line 3816 "prebuilt\\asmparse.cpp"
    break;

  case 62: /* customDescr: _CUSTOM customType  */
#line 296 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[0].token), NULL); }
#line 3822 "prebuilt\\asmparse.cpp"
    break;

  case 63: /* customDescr: _CUSTOM customType '=' compQstring  */
#line 297 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].token), (yyvsp[0].binstr)); }
#line 3828 "prebuilt\\asmparse.cpp"
    break;

  case 64: /* customDescr: _CUSTOM customType '=' '{' customBlobDescr '}'  */
#line 298 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 3834 "prebuilt\\asmparse.cpp"
    break;

  case 65: /* customDescr: customHead bytes ')'  */
#line 299 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].int32), (yyvsp[-1].binstr)); }
#line 3840 "prebuilt\\asmparse.cpp"
    break;

  case 66: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType  */
#line 302 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-2].token), (yyvsp[0].token), NULL); }
#line 3846 "prebuilt\\asmparse.cpp"
    break;

  case 67: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType '=' compQstring  */
#line 303 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-4].token), (yyvsp[-2].token), (yyvsp[0].binstr)); }
#line 3852 "prebuilt\\asmparse.cpp"
    break;

  case 68: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType '=' '{' customBlobDescr '}'  */
#line 305 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-6].token), (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 3858 "prebuilt\\asmparse.cpp"
    break;

  case 69: /* customDescrWithOwner: customHeadWithOwner bytes ')'  */
#line 306 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].int32), (yyvsp[-1].binstr)); }
#line 3864 "prebuilt\\asmparse.cpp"
    break;

  case 70: /* customHead: _CUSTOM customType '=' '('  */
#line 309 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-2].token); bParsingByteArray = TRUE; }
#line 3870 "prebuilt\\asmparse.cpp"
    break;

  case 71: /* customHeadWithOwner: _CUSTOM '(' ownerType ')' customType '=' '('  */
#line 313 "asmparse.y"
                                                                                { PASM->m_pCustomDescrList = NULL;
                                                                                  PASM->m_tkCurrentCVOwner = (yyvsp[-4].token);
                                                                                  (yyval.int32) = (yyvsp[-2].token); bParsingByteArray = TRUE; }
#line 3878 "prebuilt\\asmparse.cpp"
    break;

  case 72: /* customType: methodRef  */
#line 318 "asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 3884 "prebuilt\\asmparse.cpp"
    break;

  case 73: /* ownerType: typeSpec  */
#line 321 "asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 3890 "prebuilt\\asmparse.cpp"
    break;

  case 74: /* ownerType: memberRef  */
#line 322 "asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 3896 "prebuilt\\asmparse.cpp"
    break;

  case 75: /* customBlobDescr: customBlobArgs customBlobNVPairs  */
#line 326 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr);
                                                                                  (yyval.binstr)->appendInt16(VAL16(nCustomBlobNVPairs));
                                                                                  (yyval.binstr)->append((yyvsp[0].binstr));
                                                                                  nCustomBlobNVPairs = 0; }
#line 3905 "prebuilt\\asmparse.cpp"
    break;

  case 76: /* customBlobArgs: %empty  */
#line 332 "asmparse.y"
                                                                                { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt16(VAL16(0x0001)); }
#line 3911 "prebuilt\\asmparse.cpp"
    break;

  case 77: /* customBlobArgs: customBlobArgs serInit  */
#line 333 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr);
                                                                                  AppendFieldToCustomBlob((yyval.binstr),(yyvsp[0].binstr)); }
#line 3918 "prebuilt\\asmparse.cpp"
    break;

  case 78: /* customBlobArgs: customBlobArgs compControl  */
#line 335 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 3924 "prebuilt\\asmparse.cpp"
    break;

  case 79: /* customBlobNVPairs: %empty  */
#line 338 "asmparse.y"
                                                                                { (yyval.binstr) = new BinStr(); }
#line 3930 "prebuilt\\asmparse.cpp"
    break;

  case 80: /* customBlobNVPairs: customBlobNVPairs fieldOrProp serializType dottedName '=' serInit  */
#line 340 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-5].binstr); (yyval.binstr)->appendInt8((yyvsp[-4].int32));
                                                                                  (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                                  AppendStringWithLength((yyval.binstr),(yyvsp[-2].string));
                                                                                  AppendFieldToCustomBlob((yyval.binstr),(yyvsp[0].binstr));
                                                                                  nCustomBlobNVPairs++; }
#line 3940 "prebuilt\\asmparse.cpp"
    break;

  case 81: /* customBlobNVPairs: customBlobNVPairs compControl  */
#line 345 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 3946 "prebuilt\\asmparse.cpp"
    break;

  case 82: /* fieldOrProp: FIELD_  */
#line 348 "asmparse.y"
                                                                                { (yyval.int32) = SERIALIZATION_TYPE_FIELD; }
#line 3952 "prebuilt\\asmparse.cpp"
    break;

  case 83: /* fieldOrProp: PROPERTY_  */
#line 349 "asmparse.y"
                                                                                { (yyval.int32) = SERIALIZATION_TYPE_PROPERTY; }
#line 3958 "prebuilt\\asmparse.cpp"
    break;

  case 84: /* customAttrDecl: customDescr  */
#line 352 "asmparse.y"
                                                                                { if((yyvsp[0].cad)->tkOwner && !(yyvsp[0].cad)->tkInterfacePair)
                                                                                    PASM->DefineCV((yyvsp[0].cad));
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH((yyvsp[0].cad)); }
#line 3967 "prebuilt\\asmparse.cpp"
    break;

  case 85: /* customAttrDecl: customDescrWithOwner  */
#line 356 "asmparse.y"
                                                                                { PASM->DefineCV((yyvsp[0].cad)); }
#line 3973 "prebuilt\\asmparse.cpp"
    break;

  case 86: /* customAttrDecl: TYPEDEF_CA  */
#line 357 "asmparse.y"
                                                                                { CustomDescr* pNew = new CustomDescr((yyvsp[0].tdd)->m_pCA);
                                                                                  if(pNew->tkOwner == 0) pNew->tkOwner = PASM->m_tkCurrentCVOwner;
                                                                                  if(pNew->tkOwner)
                                                                                    PASM->DefineCV(pNew);
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH(pNew); }
#line 3984 "prebuilt\\asmparse.cpp"
    break;

  case 87: /* serializType: simpleType  */
#line 365 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 3990 "prebuilt\\asmparse.cpp"
    break;

  case 88: /* serializType: TYPE_  */
#line 366 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE); }
#line 3996 "prebuilt\\asmparse.cpp"
    break;

  case 89: /* serializType: OBJECT_  */
#line 367 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TAGGED_OBJECT); }
#line 4002 "prebuilt\\asmparse.cpp"
    break;

  case 90: /* serializType: ENUM_ CLASS_ SQSTRING  */
#line 368 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); }
#line 4009 "prebuilt\\asmparse.cpp"
    break;

  case 91: /* serializType: ENUM_ className  */
#line 370 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[0].token))); }
#line 4016 "prebuilt\\asmparse.cpp"
    break;

  case 92: /* serializType: serializType '[' ']'  */
#line 372 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 4022 "prebuilt\\asmparse.cpp"
    break;

  case 93: /* moduleHead: _MODULE  */
#line 377 "asmparse.y"
                                                                                { PASMM->SetModuleName(NULL); PASM->m_tkCurrentCVOwner=1; }
#line 4028 "prebuilt\\asmparse.cpp"
    break;

  case 94: /* moduleHead: _MODULE dottedName  */
#line 378 "asmparse.y"
                                                                                { PASMM->SetModuleName((yyvsp[0].string)); PASM->m_tkCurrentCVOwner=1; }
#line 4034 "prebuilt\\asmparse.cpp"
    break;

  case 95: /* moduleHead: _MODULE EXTERN_ dottedName  */
#line 379 "asmparse.y"
                                                                                { BinStr* pbs = new BinStr();
                                                                                  unsigned L = (unsigned)strlen((yyvsp[0].string));
                                                                                  memcpy((char*)(pbs->getBuff(L)),(yyvsp[0].string),L);
                                                                                  PASM->EmitImport(pbs); delete pbs;}
#line 4043 "prebuilt\\asmparse.cpp"
    break;

  case 96: /* vtfixupDecl: _VTFIXUP '[' int32 ']' vtfixupAttr AT_ id  */
#line 386 "asmparse.y"
                                                                                { /*PASM->SetDataSection(); PASM->EmitDataLabel($7);*/
                                                                                  PASM->m_VTFList.PUSH(new VTFEntry((USHORT)(yyvsp[-4].int32), (USHORT)(yyvsp[-2].int32), (yyvsp[0].string))); }
#line 4050 "prebuilt\\asmparse.cpp"
    break;

  case 97: /* vtfixupAttr: %empty  */
#line 390 "asmparse.y"
                                                                                { (yyval.int32) = 0; }
#line 4056 "prebuilt\\asmparse.cpp"
    break;

  case 98: /* vtfixupAttr: vtfixupAttr INT32_  */
#line 391 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_32BIT; }
#line 4062 "prebuilt\\asmparse.cpp"
    break;

  case 99: /* vtfixupAttr: vtfixupAttr INT64_  */
#line 392 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_64BIT; }
#line 4068 "prebuilt\\asmparse.cpp"
    break;

  case 100: /* vtfixupAttr: vtfixupAttr FROMUNMANAGED_  */
#line 393 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_FROM_UNMANAGED; }
#line 4074 "prebuilt\\asmparse.cpp"
    break;

  case 101: /* vtfixupAttr: vtfixupAttr CALLMOSTDERIVED_  */
#line 394 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_CALL_MOST_DERIVED; }
#line 4080 "prebuilt\\asmparse.cpp"
    break;

  case 102: /* vtfixupAttr: vtfixupAttr RETAINAPPDOMAIN_  */
#line 395 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN; }
#line 4086 "prebuilt\\asmparse.cpp"
    break;

  case 103: /* vtableDecl: vtableHead bytes ')'  */
#line 398 "asmparse.y"
                                                                                { PASM->m_pVTable = (yyvsp[-1].binstr); }
#line 4092 "prebuilt\\asmparse.cpp"
    break;

  case 104: /* vtableHead: _VTABLE '=' '('  */
#line 401 "asmparse.y"
                                                                                { bParsingByteArray = TRUE; }
#line 4098 "prebuilt\\asmparse.cpp"
    break;

  case 105: /* nameSpaceHead: _NAMESPACE dottedName  */
#line 405 "asmparse.y"
                                                                                { PASM->StartNameSpace((yyvsp[0].string)); }
#line 4104 "prebuilt\\asmparse.cpp"
    break;

  case 106: /* _class: _CLASS  */
#line 408 "asmparse.y"
                                                                                { newclass = TRUE; }
#line 4110 "prebuilt\\asmparse.cpp"
    break;

  case 107: /* classHeadBegin: _class classAttr dottedName typarsClause  */
#line 411 "asmparse.y"
                                                                                { if((yyvsp[0].typarlist)) FixupConstraints();
                                                                                  PASM->StartClass((yyvsp[-1].string), (yyvsp[-2].classAttr), (yyvsp[0].typarlist));
                                                                                  TyParFixupList.RESET(false);
                                                                                  newclass = FALSE;
                                                                                }
#line 4120 "prebuilt\\asmparse.cpp"
    break;

  case 108: /* classHead: classHeadBegin extendsClause implClause  */
#line 417 "asmparse.y"
                                                                                { PASM->AddClass(); }
#line 4126 "prebuilt\\asmparse.cpp"
    break;

  case 109: /* classAttr: %empty  */
#line 420 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) 0; }
#line 4132 "prebuilt\\asmparse.cpp"
    break;

  case 110: /* classAttr: classAttr PUBLIC_  */
#line 421 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdVisibilityMask) | tdPublic); }
#line 4138 "prebuilt\\asmparse.cpp"
    break;

  case 111: /* classAttr: classAttr PRIVATE_  */
#line 422 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdVisibilityMask) | tdNotPublic); }
#line 4144 "prebuilt\\asmparse.cpp"
    break;

  case 112: /* classAttr: classAttr VALUE_  */
#line 423 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | 0x80000000 | tdSealed); }
#line 4150 "prebuilt\\asmparse.cpp"
    break;

  case 113: /* classAttr: classAttr ENUM_  */
#line 424 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | 0x40000000); }
#line 4156 "prebuilt\\asmparse.cpp"
    break;

  case 114: /* classAttr: classAttr INTERFACE_  */
#line 425 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdInterface | tdAbstract); }
#line 4162 "prebuilt\\asmparse.cpp"
    break;

  case 115: /* classAttr: classAttr SEALED_  */
#line 426 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSealed); }
#line 4168 "prebuilt\\asmparse.cpp"
    break;

  case 116: /* classAttr: classAttr ABSTRACT_  */
#line 427 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdAbstract); }
#line 4174 "prebuilt\\asmparse.cpp"
    break;

  case 117: /* classAttr: classAttr AUTO_  */
#line 428 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdAutoLayout); }
#line 4180 "prebuilt\\asmparse.cpp"
    break;

  case 118: /* classAttr: classAttr SEQUENTIAL_  */
#line 429 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdSequentialLayout); }
#line 4186 "prebuilt\\asmparse.cpp"
    break;

  case 119: /* classAttr: classAttr EXPLICIT_  */
#line 430 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdExplicitLayout); }
#line 4192 "prebuilt\\asmparse.cpp"
    break;

  case 120: /* classAttr: classAttr ANSI_  */
#line 431 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdAnsiClass); }
#line 4198 "prebuilt\\asmparse.cpp"
    break;

  case 121: /* classAttr: classAttr UNICODE_  */
#line 432 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdUnicodeClass); }
#line 4204 "prebuilt\\asmparse.cpp"
    break;

  case 122: /* classAttr: classAttr AUTOCHAR_  */
#line 433 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdAutoClass); }
#line 4210 "prebuilt\\asmparse.cpp"
    break;

  case 123: /* classAttr: classAttr IMPORT_  */
#line 434 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdImport); }
#line 4216 "prebuilt\\asmparse.cpp"
    break;

  case 124: /* classAttr: classAttr SERIALIZABLE_  */
#line 435 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSerializable); }
#line 4222 "prebuilt\\asmparse.cpp"
    break;

  case 125: /* classAttr: classAttr WINDOWSRUNTIME_  */
#line 436 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdWindowsRuntime); }
#line 4228 "prebuilt\\asmparse.cpp"
    break;

  case 126: /* classAttr: classAttr NESTED_ PUBLIC_  */
#line 437 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedPublic); }
#line 4234 "prebuilt\\asmparse.cpp"
    break;

  case 127: /* classAttr: classAttr NESTED_ PRIVATE_  */
#line 438 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedPrivate); }
#line 4240 "prebuilt\\asmparse.cpp"
    break;

  case 128: /* classAttr: classAttr NESTED_ FAMILY_  */
#line 439 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamily); }
#line 4246 "prebuilt\\asmparse.cpp"
    break;

  case 129: /* classAttr: classAttr NESTED_ ASSEMBLY_  */
#line 440 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedAssembly); }
#line 4252 "prebuilt\\asmparse.cpp"
    break;

  case 130: /* classAttr: classAttr NESTED_ FAMANDASSEM_  */
#line 441 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamANDAssem); }
#line 4258 "prebuilt\\asmparse.cpp"
    break;

  case 131: /* classAttr: classAttr NESTED_ FAMORASSEM_  */
#line 442 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamORAssem); }
#line 4264 "prebuilt\\asmparse.cpp"
    break;

  case 132: /* classAttr: classAttr BEFOREFIELDINIT_  */
#line 443 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdBeforeFieldInit); }
#line 4270 "prebuilt\\asmparse.cpp"
    break;

  case 133: /* classAttr: classAttr SPECIALNAME_  */
#line 444 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSpecialName); }
#line 4276 "prebuilt\\asmparse.cpp"
    break;

  case 134: /* classAttr: classAttr RTSPECIALNAME_  */
#line 445 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr)); }
#line 4282 "prebuilt\\asmparse.cpp"
    break;

  case 135: /* classAttr: classAttr FLAGS_ '(' int32 ')'  */
#line 446 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].int32)); }
#line 4288 "prebuilt\\asmparse.cpp"
    break;

  case 137: /* extendsClause: EXTENDS_ typeSpec  */
#line 450 "asmparse.y"
                                                                            { PASM->m_crExtends = (yyvsp[0].token); }
#line 4294 "prebuilt\\asmparse.cpp"
    break;

  case 142: /* implList: implList ',' typeSpec  */
#line 461 "asmparse.y"
                                                            { PASM->AddToImplList((yyvsp[0].token)); }
#line 4300 "prebuilt\\asmparse.cpp"
    break;

  case 143: /* implList: typeSpec  */
#line 462 "asmparse.y"
                                                            { PASM->AddToImplList((yyvsp[0].token)); }
#line 4306 "prebuilt\\asmparse.cpp"
    break;

  case 144: /* typeList: %empty  */
#line 466 "asmparse.y"
                                                            { (yyval.binstr) = new BinStr(); }
#line 4312 "prebuilt\\asmparse.cpp"
    break;

  case 145: /* typeList: typeListNotEmpty  */
#line 467 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4318 "prebuilt\\asmparse.cpp"
    break;

  case 146: /* typeListNotEmpty: typeSpec  */
#line 470 "asmparse.y"
                                                            { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[0].token)); }
#line 4324 "prebuilt\\asmparse.cpp"
    break;

  case 147: /* typeListNotEmpty: typeListNotEmpty ',' typeSpec  */
#line 471 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->appendInt32((yyvsp[0].token)); }
#line 4330 "prebuilt\\asmparse.cpp"
    break;

  case 148: /* typarsClause: %empty  */
#line 474 "asmparse.y"
                                                            { (yyval.typarlist) = NULL; PASM->m_TyParList = NULL;}
#line 4336 "prebuilt\\asmparse.cpp"
    break;

  case 149: /* typarsClause: '<' typars '>'  */
#line 475 "asmparse.y"
                                                            { (yyval.typarlist) = (yyvsp[-1].typarlist);   PASM->m_TyParList = (yyvsp[-1].typarlist);}
#line 4342 "prebuilt\\asmparse.cpp"
    break;

  case 150: /* typarAttrib: '+'  */
#line 478 "asmparse.y"
                                                            { (yyval.int32) = gpCovariant; }
#line 4348 "prebuilt\\asmparse.cpp"
    break;

  case 151: /* typarAttrib: '-'  */
#line 479 "asmparse.y"
                                                            { (yyval.int32) = gpContravariant; }
#line 4354 "prebuilt\\asmparse.cpp"
    break;

  case 152: /* typarAttrib: CLASS_  */
#line 480 "asmparse.y"
                                                            { (yyval.int32) = gpReferenceTypeConstraint; }
#line 4360 "prebuilt\\asmparse.cpp"
    break;

  case 153: /* typarAttrib: VALUETYPE_  */
#line 481 "asmparse.y"
                                                            { (yyval.int32) = gpNotNullableValueTypeConstraint; }
#line 4366 "prebuilt\\asmparse.cpp"
    break;

  case 154: /* typarAttrib: BYREFLIKE_  */
#line 482 "asmparse.y"
                                                            { (yyval.int32) = gpAllowByRefLike; }
#line 4372 "prebuilt\\asmparse.cpp"
    break;

  case 155: /* typarAttrib: _CTOR  */
#line 483 "asmparse.y"
                                                            { (yyval.int32) = gpDefaultConstructorConstraint; }
#line 4378 "prebuilt\\asmparse.cpp"
    break;

  case 156: /* typarAttrib: FLAGS_ '(' int32 ')'  */
#line 484 "asmparse.y"
                                                            { (yyval.int32) = (CorGenericParamAttr)(yyvsp[-1].int32); }
#line 4384 "prebuilt\\asmparse.cpp"
    break;

  case 157: /* typarAttribs: %empty  */
#line 487 "asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 4390 "prebuilt\\asmparse.cpp"
    break;

  case 158: /* typarAttribs: typarAttrib typarAttribs  */
#line 488 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) | (yyvsp[0].int32); }
#line 4396 "prebuilt\\asmparse.cpp"
    break;

  case 159: /* typars: typarAttribs tyBound dottedName typarsRest  */
#line 491 "asmparse.y"
                                                                     {(yyval.typarlist) = new TyParList((yyvsp[-3].int32), (yyvsp[-2].binstr), (yyvsp[-1].string), (yyvsp[0].typarlist));}
#line 4402 "prebuilt\\asmparse.cpp"
    break;

  case 160: /* typars: typarAttribs dottedName typarsRest  */
#line 492 "asmparse.y"
                                                               {(yyval.typarlist) = new TyParList((yyvsp[-2].int32), NULL, (yyvsp[-1].string), (yyvsp[0].typarlist));}
#line 4408 "prebuilt\\asmparse.cpp"
    break;

  case 161: /* typarsRest: %empty  */
#line 495 "asmparse.y"
                                                            { (yyval.typarlist) = NULL; }
#line 4414 "prebuilt\\asmparse.cpp"
    break;

  case 162: /* typarsRest: ',' typars  */
#line 496 "asmparse.y"
                                                            { (yyval.typarlist) = (yyvsp[0].typarlist); }
#line 4420 "prebuilt\\asmparse.cpp"
    break;

  case 163: /* tyBound: '(' typeList ')'  */
#line 499 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 4426 "prebuilt\\asmparse.cpp"
    break;

  case 164: /* genArity: %empty  */
#line 502 "asmparse.y"
                                                            { (yyval.int32)= 0; }
#line 4432 "prebuilt\\asmparse.cpp"
    break;

  case 165: /* genArity: genArityNotEmpty  */
#line 503 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[0].int32); }
#line 4438 "prebuilt\\asmparse.cpp"
    break;

  case 166: /* genArityNotEmpty: '<' '[' int32 ']' '>'  */
#line 506 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-2].int32); }
#line 4444 "prebuilt\\asmparse.cpp"
    break;

  case 167: /* classDecl: methodHead methodDecls '}'  */
#line 510 "asmparse.y"
                                                            { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                              {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                 PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                              PASM->EndMethod(); }
#line 4453 "prebuilt\\asmparse.cpp"
    break;

  case 168: /* classDecl: classHead '{' classDecls '}'  */
#line 514 "asmparse.y"
                                                            { PASM->EndClass(); }
#line 4459 "prebuilt\\asmparse.cpp"
    break;

  case 169: /* classDecl: eventHead '{' eventDecls '}'  */
#line 515 "asmparse.y"
                                                            { PASM->EndEvent(); }
#line 4465 "prebuilt\\asmparse.cpp"
    break;

  case 170: /* classDecl: propHead '{' propDecls '}'  */
#line 516 "asmparse.y"
                                                            { PASM->EndProp(); }
#line 4471 "prebuilt\\asmparse.cpp"
    break;

  case 176: /* classDecl: _SIZE int32  */
#line 522 "asmparse.y"
                                                                { PASM->m_pCurClass->m_ulSize = (yyvsp[0].int32); }
#line 4477 "prebuilt\\asmparse.cpp"
    break;

  case 177: /* classDecl: _PACK int32  */
#line 523 "asmparse.y"
                                                                { PASM->m_pCurClass->m_ulPack = (yyvsp[0].int32); }
#line 4483 "prebuilt\\asmparse.cpp"
    break;

  case 178: /* classDecl: exportHead '{' exptypeDecls '}'  */
#line 524 "asmparse.y"
                                                                { PASMM->EndComType(); }
#line 4489 "prebuilt\\asmparse.cpp"
    break;

  case 179: /* classDecl: _OVERRIDE typeSpec DCOLON methodName WITH_ callConv type typeSpec DCOLON methodName '(' sigArgs0 ')'  */
#line 526 "asmparse.y"
                                                                { BinStr *sig1 = parser->MakeSig((yyvsp[-7].int32), (yyvsp[-6].binstr), (yyvsp[-1].binstr));
                                                                  BinStr *sig2 = new BinStr(); sig2->append(sig1);
                                                                  PASM->AddMethodImpl((yyvsp[-11].token),(yyvsp[-9].string),sig1,(yyvsp[-5].token),(yyvsp[-3].string),sig2);
                                                                  PASM->ResetArgNameList();
                                                                }
#line 4499 "prebuilt\\asmparse.cpp"
    break;

  case 180: /* classDecl: _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')' WITH_ METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'  */
#line 532 "asmparse.y"
                                                                 { PASM->AddMethodImpl((yyvsp[-17].token),(yyvsp[-15].string),
                                                                      ((yyvsp[-14].int32)==0 ? parser->MakeSig((yyvsp[-19].int32),(yyvsp[-18].binstr),(yyvsp[-12].binstr)) :
                                                                      parser->MakeSig((yyvsp[-19].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-18].binstr),(yyvsp[-12].binstr),(yyvsp[-14].int32))),
                                                                      (yyvsp[-6].token),(yyvsp[-4].string),
                                                                      ((yyvsp[-3].int32)==0 ? parser->MakeSig((yyvsp[-8].int32),(yyvsp[-7].binstr),(yyvsp[-1].binstr)) :
                                                                      parser->MakeSig((yyvsp[-8].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-7].binstr),(yyvsp[-1].binstr),(yyvsp[-3].int32))));
                                                                   PASM->ResetArgNameList();
                                                                 }
#line 4512 "prebuilt\\asmparse.cpp"
    break;

  case 183: /* classDecl: _PARAM TYPE_ '[' int32 ']'  */
#line 542 "asmparse.y"
                                                            { if(((yyvsp[-1].int32) > 0) && ((yyvsp[-1].int32) <= (int)PASM->m_pCurClass->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[(yyvsp[-1].int32)-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
#line 4522 "prebuilt\\asmparse.cpp"
    break;

  case 184: /* classDecl: _PARAM TYPE_ dottedName  */
#line 547 "asmparse.y"
                                                            { int n = PASM->m_pCurClass->FindTyPar((yyvsp[0].string));
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",(yyvsp[0].string));
                                                            }
#line 4533 "prebuilt\\asmparse.cpp"
    break;

  case 185: /* classDecl: _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec  */
#line 553 "asmparse.y"
                                                                        { PASM->AddGenericParamConstraint((yyvsp[-3].int32), 0, (yyvsp[0].token)); }
#line 4539 "prebuilt\\asmparse.cpp"
    break;

  case 186: /* classDecl: _PARAM CONSTRAINT_ dottedName ',' typeSpec  */
#line 554 "asmparse.y"
                                                                        { PASM->AddGenericParamConstraint(0, (yyvsp[-2].string), (yyvsp[0].token)); }
#line 4545 "prebuilt\\asmparse.cpp"
    break;

  case 187: /* classDecl: _INTERFACEIMPL TYPE_ typeSpec customDescr  */
#line 555 "asmparse.y"
                                                                      { (yyvsp[0].cad)->tkInterfacePair = (yyvsp[-1].token);
                                                                        if(PASM->m_pCustomDescrList)
                                                                            PASM->m_pCustomDescrList->PUSH((yyvsp[0].cad));
                                                                      }
#line 4554 "prebuilt\\asmparse.cpp"
    break;

  case 188: /* fieldDecl: _FIELD repeatOpt fieldAttr type dottedName atOpt initOpt  */
#line 563 "asmparse.y"
                                                            { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                              PASM->AddField((yyvsp[-2].string), (yyvsp[-3].binstr), (yyvsp[-4].fieldAttr), (yyvsp[-1].string), (yyvsp[0].binstr), (yyvsp[-5].int32)); }
#line 4561 "prebuilt\\asmparse.cpp"
    break;

  case 189: /* fieldAttr: %empty  */
#line 567 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) 0; }
#line 4567 "prebuilt\\asmparse.cpp"
    break;

  case 190: /* fieldAttr: fieldAttr STATIC_  */
#line 568 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdStatic); }
#line 4573 "prebuilt\\asmparse.cpp"
    break;

  case 191: /* fieldAttr: fieldAttr PUBLIC_  */
#line 569 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPublic); }
#line 4579 "prebuilt\\asmparse.cpp"
    break;

  case 192: /* fieldAttr: fieldAttr PRIVATE_  */
#line 570 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPrivate); }
#line 4585 "prebuilt\\asmparse.cpp"
    break;

  case 193: /* fieldAttr: fieldAttr FAMILY_  */
#line 571 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamily); }
#line 4591 "prebuilt\\asmparse.cpp"
    break;

  case 194: /* fieldAttr: fieldAttr INITONLY_  */
#line 572 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdInitOnly); }
#line 4597 "prebuilt\\asmparse.cpp"
    break;

  case 195: /* fieldAttr: fieldAttr RTSPECIALNAME_  */
#line 573 "asmparse.y"
                                                            { (yyval.fieldAttr) = (yyvsp[-1].fieldAttr); }
#line 4603 "prebuilt\\asmparse.cpp"
    break;

  case 196: /* fieldAttr: fieldAttr SPECIALNAME_  */
#line 574 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdSpecialName); }
#line 4609 "prebuilt\\asmparse.cpp"
    break;

  case 197: /* fieldAttr: fieldAttr MARSHAL_ '(' marshalBlob ')'  */
#line 587 "asmparse.y"
                                                            { PASM->m_pMarshal = (yyvsp[-1].binstr); }
#line 4615 "prebuilt\\asmparse.cpp"
    break;

  case 198: /* fieldAttr: fieldAttr ASSEMBLY_  */
#line 588 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdAssembly); }
#line 4621 "prebuilt\\asmparse.cpp"
    break;

  case 199: /* fieldAttr: fieldAttr FAMANDASSEM_  */
#line 589 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamANDAssem); }
#line 4627 "prebuilt\\asmparse.cpp"
    break;

  case 200: /* fieldAttr: fieldAttr FAMORASSEM_  */
#line 590 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamORAssem); }
#line 4633 "prebuilt\\asmparse.cpp"
    break;

  case 201: /* fieldAttr: fieldAttr PRIVATESCOPE_  */
#line 591 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPrivateScope); }
#line 4639 "prebuilt\\asmparse.cpp"
    break;

  case 202: /* fieldAttr: fieldAttr LITERAL_  */
#line 592 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdLiteral); }
#line 4645 "prebuilt\\asmparse.cpp"
    break;

  case 203: /* fieldAttr: fieldAttr NOTSERIALIZED_  */
#line 593 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdNotSerialized); }
#line 4651 "prebuilt\\asmparse.cpp"
    break;

  case 204: /* fieldAttr: fieldAttr FLAGS_ '(' int32 ')'  */
#line 594 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].int32)); }
#line 4657 "prebuilt\\asmparse.cpp"
    break;

  case 205: /* atOpt: %empty  */
#line 597 "asmparse.y"
                                                            { (yyval.string) = 0; }
#line 4663 "prebuilt\\asmparse.cpp"
    break;

  case 206: /* atOpt: AT_ id  */
#line 598 "asmparse.y"
                                                            { (yyval.string) = (yyvsp[0].string); }
#line 4669 "prebuilt\\asmparse.cpp"
    break;

  case 207: /* initOpt: %empty  */
#line 601 "asmparse.y"
                                                            { (yyval.binstr) = NULL; }
#line 4675 "prebuilt\\asmparse.cpp"
    break;

  case 208: /* initOpt: '=' fieldInit  */
#line 602 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4681 "prebuilt\\asmparse.cpp"
    break;

  case 209: /* repeatOpt: %empty  */
#line 605 "asmparse.y"
                                                            { (yyval.int32) = 0xFFFFFFFF; }
#line 4687 "prebuilt\\asmparse.cpp"
    break;

  case 210: /* repeatOpt: '[' int32 ']'  */
#line 606 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32); }
#line 4693 "prebuilt\\asmparse.cpp"
    break;

  case 211: /* methodRef: callConv type typeSpec DCOLON methodName tyArgs0 '(' sigArgs0 ')'  */
#line 611 "asmparse.y"
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
#line 4714 "prebuilt\\asmparse.cpp"
    break;

  case 212: /* methodRef: callConv type typeSpec DCOLON methodName genArityNotEmpty '(' sigArgs0 ')'  */
#line 628 "asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(((yyvsp[-8].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               (yyval.token) = PASM->MakeMemberRef((yyvsp[-6].token), (yyvsp[-4].string),
                                                                 parser->MakeSig((yyvsp[-8].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-7].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32)));
                                                             }
#line 4724 "prebuilt\\asmparse.cpp"
    break;

  case 213: /* methodRef: callConv type methodName tyArgs0 '(' sigArgs0 ')'  */
#line 634 "asmparse.y"
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
#line 4744 "prebuilt\\asmparse.cpp"
    break;

  case 214: /* methodRef: callConv type methodName genArityNotEmpty '(' sigArgs0 ')'  */
#line 650 "asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(((yyvsp[-6].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               (yyval.token) = PASM->MakeMemberRef(mdTokenNil, (yyvsp[-4].string), parser->MakeSig((yyvsp[-6].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-5].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32)));
                                                             }
#line 4753 "prebuilt\\asmparse.cpp"
    break;

  case 215: /* methodRef: mdtoken  */
#line 654 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token); }
#line 4759 "prebuilt\\asmparse.cpp"
    break;

  case 216: /* methodRef: TYPEDEF_M  */
#line 655 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 4765 "prebuilt\\asmparse.cpp"
    break;

  case 217: /* methodRef: TYPEDEF_MR  */
#line 656 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 4771 "prebuilt\\asmparse.cpp"
    break;

  case 218: /* callConv: INSTANCE_ callConv  */
#line 659 "asmparse.y"
                                                              { (yyval.int32) = ((yyvsp[0].int32) | IMAGE_CEE_CS_CALLCONV_HASTHIS); }
#line 4777 "prebuilt\\asmparse.cpp"
    break;

  case 219: /* callConv: EXPLICIT_ callConv  */
#line 660 "asmparse.y"
                                                              { (yyval.int32) = ((yyvsp[0].int32) | IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS); }
#line 4783 "prebuilt\\asmparse.cpp"
    break;

  case 220: /* callConv: callKind  */
#line 661 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 4789 "prebuilt\\asmparse.cpp"
    break;

  case 221: /* callConv: CALLCONV_ '(' int32 ')'  */
#line 662 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32); }
#line 4795 "prebuilt\\asmparse.cpp"
    break;

  case 222: /* callKind: %empty  */
#line 665 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
#line 4801 "prebuilt\\asmparse.cpp"
    break;

  case 223: /* callKind: DEFAULT_  */
#line 666 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
#line 4807 "prebuilt\\asmparse.cpp"
    break;

  case 224: /* callKind: VARARG_  */
#line 667 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_VARARG; }
#line 4813 "prebuilt\\asmparse.cpp"
    break;

  case 225: /* callKind: UNMANAGED_ CDECL_  */
#line 668 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_C; }
#line 4819 "prebuilt\\asmparse.cpp"
    break;

  case 226: /* callKind: UNMANAGED_ STDCALL_  */
#line 669 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_STDCALL; }
#line 4825 "prebuilt\\asmparse.cpp"
    break;

  case 227: /* callKind: UNMANAGED_ THISCALL_  */
#line 670 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_THISCALL; }
#line 4831 "prebuilt\\asmparse.cpp"
    break;

  case 228: /* callKind: UNMANAGED_ FASTCALL_  */
#line 671 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_FASTCALL; }
#line 4837 "prebuilt\\asmparse.cpp"
    break;

  case 229: /* callKind: UNMANAGED_  */
#line 672 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_UNMANAGED; }
#line 4843 "prebuilt\\asmparse.cpp"
    break;

  case 230: /* mdtoken: MDTOKEN_ '(' int32 ')'  */
#line 675 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[-1].int32); }
#line 4849 "prebuilt\\asmparse.cpp"
    break;

  case 231: /* memberRef: methodSpec methodRef  */
#line 678 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token);
                                                               PASM->delArgNameList(PASM->m_firstArgName);
                                                               PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                               PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                               PASM->SetMemberRefFixup((yyvsp[0].token),iOpcodeLen); }
#line 4859 "prebuilt\\asmparse.cpp"
    break;

  case 232: /* memberRef: FIELD_ type typeSpec DCOLON dottedName  */
#line 684 "asmparse.y"
                                                             { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               (yyval.token) = PASM->MakeMemberRef((yyvsp[-2].token), (yyvsp[0].string), (yyvsp[-3].binstr));
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4867 "prebuilt\\asmparse.cpp"
    break;

  case 233: /* memberRef: FIELD_ type dottedName  */
#line 688 "asmparse.y"
                                                             { (yyvsp[-1].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               (yyval.token) = PASM->MakeMemberRef(mdTokenNil, (yyvsp[0].string), (yyvsp[-1].binstr));
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4875 "prebuilt\\asmparse.cpp"
    break;

  case 234: /* memberRef: FIELD_ TYPEDEF_F  */
#line 691 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4882 "prebuilt\\asmparse.cpp"
    break;

  case 235: /* memberRef: FIELD_ TYPEDEF_MR  */
#line 693 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4889 "prebuilt\\asmparse.cpp"
    break;

  case 236: /* memberRef: mdtoken  */
#line 695 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token);
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4896 "prebuilt\\asmparse.cpp"
    break;

  case 237: /* eventHead: _EVENT eventAttr typeSpec dottedName  */
#line 700 "asmparse.y"
                                                                 { PASM->ResetEvent((yyvsp[0].string), (yyvsp[-1].token), (yyvsp[-2].eventAttr)); }
#line 4902 "prebuilt\\asmparse.cpp"
    break;

  case 238: /* eventHead: _EVENT eventAttr dottedName  */
#line 701 "asmparse.y"
                                                                 { PASM->ResetEvent((yyvsp[0].string), mdTypeRefNil, (yyvsp[-1].eventAttr)); }
#line 4908 "prebuilt\\asmparse.cpp"
    break;

  case 239: /* eventAttr: %empty  */
#line 705 "asmparse.y"
                                                            { (yyval.eventAttr) = (CorEventAttr) 0; }
#line 4914 "prebuilt\\asmparse.cpp"
    break;

  case 240: /* eventAttr: eventAttr RTSPECIALNAME_  */
#line 706 "asmparse.y"
                                                            { (yyval.eventAttr) = (yyvsp[-1].eventAttr); }
#line 4920 "prebuilt\\asmparse.cpp"
    break;

  case 241: /* eventAttr: eventAttr SPECIALNAME_  */
#line 707 "asmparse.y"
                                                            { (yyval.eventAttr) = (CorEventAttr) ((yyvsp[-1].eventAttr) | evSpecialName); }
#line 4926 "prebuilt\\asmparse.cpp"
    break;

  case 244: /* eventDecl: _ADDON methodRef  */
#line 714 "asmparse.y"
                                                           { PASM->SetEventMethod(0, (yyvsp[0].token)); }
#line 4932 "prebuilt\\asmparse.cpp"
    break;

  case 245: /* eventDecl: _REMOVEON methodRef  */
#line 715 "asmparse.y"
                                                           { PASM->SetEventMethod(1, (yyvsp[0].token)); }
#line 4938 "prebuilt\\asmparse.cpp"
    break;

  case 246: /* eventDecl: _FIRE methodRef  */
#line 716 "asmparse.y"
                                                           { PASM->SetEventMethod(2, (yyvsp[0].token)); }
#line 4944 "prebuilt\\asmparse.cpp"
    break;

  case 247: /* eventDecl: _OTHER methodRef  */
#line 717 "asmparse.y"
                                                           { PASM->SetEventMethod(3, (yyvsp[0].token)); }
#line 4950 "prebuilt\\asmparse.cpp"
    break;

  case 252: /* propHead: _PROPERTY propAttr callConv type dottedName '(' sigArgs0 ')' initOpt  */
#line 726 "asmparse.y"
                                                            { PASM->ResetProp((yyvsp[-4].string),
                                                              parser->MakeSig((IMAGE_CEE_CS_CALLCONV_PROPERTY |
                                                              ((yyvsp[-6].int32) & IMAGE_CEE_CS_CALLCONV_HASTHIS)),(yyvsp[-5].binstr),(yyvsp[-2].binstr)), (yyvsp[-7].propAttr), (yyvsp[0].binstr));}
#line 4958 "prebuilt\\asmparse.cpp"
    break;

  case 253: /* propAttr: %empty  */
#line 731 "asmparse.y"
                                                            { (yyval.propAttr) = (CorPropertyAttr) 0; }
#line 4964 "prebuilt\\asmparse.cpp"
    break;

  case 254: /* propAttr: propAttr RTSPECIALNAME_  */
#line 732 "asmparse.y"
                                                            { (yyval.propAttr) = (yyvsp[-1].propAttr); }
#line 4970 "prebuilt\\asmparse.cpp"
    break;

  case 255: /* propAttr: propAttr SPECIALNAME_  */
#line 733 "asmparse.y"
                                                            { (yyval.propAttr) = (CorPropertyAttr) ((yyvsp[-1].propAttr) | prSpecialName); }
#line 4976 "prebuilt\\asmparse.cpp"
    break;

  case 258: /* propDecl: _SET methodRef  */
#line 741 "asmparse.y"
                                                            { PASM->SetPropMethod(0, (yyvsp[0].token)); }
#line 4982 "prebuilt\\asmparse.cpp"
    break;

  case 259: /* propDecl: _GET methodRef  */
#line 742 "asmparse.y"
                                                            { PASM->SetPropMethod(1, (yyvsp[0].token)); }
#line 4988 "prebuilt\\asmparse.cpp"
    break;

  case 260: /* propDecl: _OTHER methodRef  */
#line 743 "asmparse.y"
                                                            { PASM->SetPropMethod(2, (yyvsp[0].token)); }
#line 4994 "prebuilt\\asmparse.cpp"
    break;

  case 265: /* methodHeadPart1: _METHOD  */
#line 751 "asmparse.y"
                                                            { PASM->ResetForNextMethod();
                                                              uMethodBeginLine = PASM->m_ulCurLine;
                                                              uMethodBeginColumn=PASM->m_ulCurColumn;
                                                            }
#line 5003 "prebuilt\\asmparse.cpp"
    break;

  case 266: /* marshalClause: %empty  */
#line 757 "asmparse.y"
                                                            { (yyval.binstr) = NULL; }
#line 5009 "prebuilt\\asmparse.cpp"
    break;

  case 267: /* marshalClause: MARSHAL_ '(' marshalBlob ')'  */
#line 758 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 5015 "prebuilt\\asmparse.cpp"
    break;

  case 268: /* marshalBlob: nativeType  */
#line 761 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 5021 "prebuilt\\asmparse.cpp"
    break;

  case 269: /* marshalBlob: marshalBlobHead hexbytes '}'  */
#line 762 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 5027 "prebuilt\\asmparse.cpp"
    break;

  case 270: /* marshalBlobHead: '{'  */
#line 765 "asmparse.y"
                                                             { bParsingByteArray = TRUE; }
#line 5033 "prebuilt\\asmparse.cpp"
    break;

  case 271: /* methodHead: methodHeadPart1 methAttr callConv paramAttr type marshalClause methodName typarsClause '(' sigArgs0 ')' implAttr '{'  */
#line 769 "asmparse.y"
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
#line 5051 "prebuilt\\asmparse.cpp"
    break;

  case 272: /* methAttr: %empty  */
#line 784 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) 0; }
#line 5057 "prebuilt\\asmparse.cpp"
    break;

  case 273: /* methAttr: methAttr STATIC_  */
#line 785 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdStatic); }
#line 5063 "prebuilt\\asmparse.cpp"
    break;

  case 274: /* methAttr: methAttr PUBLIC_  */
#line 786 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPublic); }
#line 5069 "prebuilt\\asmparse.cpp"
    break;

  case 275: /* methAttr: methAttr PRIVATE_  */
#line 787 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPrivate); }
#line 5075 "prebuilt\\asmparse.cpp"
    break;

  case 276: /* methAttr: methAttr FAMILY_  */
#line 788 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamily); }
#line 5081 "prebuilt\\asmparse.cpp"
    break;

  case 277: /* methAttr: methAttr FINAL_  */
#line 789 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdFinal); }
#line 5087 "prebuilt\\asmparse.cpp"
    break;

  case 278: /* methAttr: methAttr SPECIALNAME_  */
#line 790 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdSpecialName); }
#line 5093 "prebuilt\\asmparse.cpp"
    break;

  case 279: /* methAttr: methAttr VIRTUAL_  */
#line 791 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdVirtual); }
#line 5099 "prebuilt\\asmparse.cpp"
    break;

  case 280: /* methAttr: methAttr STRICT_  */
#line 792 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdCheckAccessOnOverride); }
#line 5105 "prebuilt\\asmparse.cpp"
    break;

  case 281: /* methAttr: methAttr ABSTRACT_  */
#line 793 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdAbstract); }
#line 5111 "prebuilt\\asmparse.cpp"
    break;

  case 282: /* methAttr: methAttr ASSEMBLY_  */
#line 794 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdAssem); }
#line 5117 "prebuilt\\asmparse.cpp"
    break;

  case 283: /* methAttr: methAttr FAMANDASSEM_  */
#line 795 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamANDAssem); }
#line 5123 "prebuilt\\asmparse.cpp"
    break;

  case 284: /* methAttr: methAttr FAMORASSEM_  */
#line 796 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamORAssem); }
#line 5129 "prebuilt\\asmparse.cpp"
    break;

  case 285: /* methAttr: methAttr PRIVATESCOPE_  */
#line 797 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPrivateScope); }
#line 5135 "prebuilt\\asmparse.cpp"
    break;

  case 286: /* methAttr: methAttr HIDEBYSIG_  */
#line 798 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdHideBySig); }
#line 5141 "prebuilt\\asmparse.cpp"
    break;

  case 287: /* methAttr: methAttr NEWSLOT_  */
#line 799 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdNewSlot); }
#line 5147 "prebuilt\\asmparse.cpp"
    break;

  case 288: /* methAttr: methAttr RTSPECIALNAME_  */
#line 800 "asmparse.y"
                                                            { (yyval.methAttr) = (yyvsp[-1].methAttr); }
#line 5153 "prebuilt\\asmparse.cpp"
    break;

  case 289: /* methAttr: methAttr UNMANAGEDEXP_  */
#line 801 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdUnmanagedExport); }
#line 5159 "prebuilt\\asmparse.cpp"
    break;

  case 290: /* methAttr: methAttr REQSECOBJ_  */
#line 802 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdRequireSecObject); }
#line 5165 "prebuilt\\asmparse.cpp"
    break;

  case 291: /* methAttr: methAttr FLAGS_ '(' int32 ')'  */
#line 803 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].int32)); }
#line 5171 "prebuilt\\asmparse.cpp"
    break;

  case 292: /* methAttr: methAttr PINVOKEIMPL_ '(' compQstring AS_ compQstring pinvAttr ')'  */
#line 805 "asmparse.y"
                                                            { PASM->SetPinvoke((yyvsp[-4].binstr),0,(yyvsp[-2].binstr),(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-7].methAttr) | mdPinvokeImpl); }
#line 5178 "prebuilt\\asmparse.cpp"
    break;

  case 293: /* methAttr: methAttr PINVOKEIMPL_ '(' compQstring pinvAttr ')'  */
#line 808 "asmparse.y"
                                                            { PASM->SetPinvoke((yyvsp[-2].binstr),0,NULL,(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-5].methAttr) | mdPinvokeImpl); }
#line 5185 "prebuilt\\asmparse.cpp"
    break;

  case 294: /* methAttr: methAttr PINVOKEIMPL_ '(' pinvAttr ')'  */
#line 811 "asmparse.y"
                                                            { PASM->SetPinvoke(new BinStr(),0,NULL,(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-4].methAttr) | mdPinvokeImpl); }
#line 5192 "prebuilt\\asmparse.cpp"
    break;

  case 295: /* pinvAttr: %empty  */
#line 815 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) 0; }
#line 5198 "prebuilt\\asmparse.cpp"
    break;

  case 296: /* pinvAttr: pinvAttr NOMANGLE_  */
#line 816 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmNoMangle); }
#line 5204 "prebuilt\\asmparse.cpp"
    break;

  case 297: /* pinvAttr: pinvAttr ANSI_  */
#line 817 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetAnsi); }
#line 5210 "prebuilt\\asmparse.cpp"
    break;

  case 298: /* pinvAttr: pinvAttr UNICODE_  */
#line 818 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetUnicode); }
#line 5216 "prebuilt\\asmparse.cpp"
    break;

  case 299: /* pinvAttr: pinvAttr AUTOCHAR_  */
#line 819 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetAuto); }
#line 5222 "prebuilt\\asmparse.cpp"
    break;

  case 300: /* pinvAttr: pinvAttr LASTERR_  */
#line 820 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmSupportsLastError); }
#line 5228 "prebuilt\\asmparse.cpp"
    break;

  case 301: /* pinvAttr: pinvAttr WINAPI_  */
#line 821 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvWinapi); }
#line 5234 "prebuilt\\asmparse.cpp"
    break;

  case 302: /* pinvAttr: pinvAttr CDECL_  */
#line 822 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvCdecl); }
#line 5240 "prebuilt\\asmparse.cpp"
    break;

  case 303: /* pinvAttr: pinvAttr STDCALL_  */
#line 823 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvStdcall); }
#line 5246 "prebuilt\\asmparse.cpp"
    break;

  case 304: /* pinvAttr: pinvAttr THISCALL_  */
#line 824 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvThiscall); }
#line 5252 "prebuilt\\asmparse.cpp"
    break;

  case 305: /* pinvAttr: pinvAttr FASTCALL_  */
#line 825 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvFastcall); }
#line 5258 "prebuilt\\asmparse.cpp"
    break;

  case 306: /* pinvAttr: pinvAttr BESTFIT_ ':' ON_  */
#line 826 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmBestFitEnabled); }
#line 5264 "prebuilt\\asmparse.cpp"
    break;

  case 307: /* pinvAttr: pinvAttr BESTFIT_ ':' OFF_  */
#line 827 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmBestFitDisabled); }
#line 5270 "prebuilt\\asmparse.cpp"
    break;

  case 308: /* pinvAttr: pinvAttr CHARMAPERROR_ ':' ON_  */
#line 828 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmThrowOnUnmappableCharEnabled); }
#line 5276 "prebuilt\\asmparse.cpp"
    break;

  case 309: /* pinvAttr: pinvAttr CHARMAPERROR_ ':' OFF_  */
#line 829 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmThrowOnUnmappableCharDisabled); }
#line 5282 "prebuilt\\asmparse.cpp"
    break;

  case 310: /* pinvAttr: pinvAttr FLAGS_ '(' int32 ')'  */
#line 830 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].int32)); }
#line 5288 "prebuilt\\asmparse.cpp"
    break;

  case 311: /* methodName: _CTOR  */
#line 833 "asmparse.y"
                                                            { (yyval.string) = newString(COR_CTOR_METHOD_NAME); }
#line 5294 "prebuilt\\asmparse.cpp"
    break;

  case 312: /* methodName: _CCTOR  */
#line 834 "asmparse.y"
                                                            { (yyval.string) = newString(COR_CCTOR_METHOD_NAME); }
#line 5300 "prebuilt\\asmparse.cpp"
    break;

  case 313: /* methodName: dottedName  */
#line 835 "asmparse.y"
                                                            { (yyval.string) = (yyvsp[0].string); }
#line 5306 "prebuilt\\asmparse.cpp"
    break;

  case 314: /* paramAttr: %empty  */
#line 838 "asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 5312 "prebuilt\\asmparse.cpp"
    break;

  case 315: /* paramAttr: paramAttr '[' IN_ ']'  */
#line 839 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdIn; }
#line 5318 "prebuilt\\asmparse.cpp"
    break;

  case 316: /* paramAttr: paramAttr '[' OUT_ ']'  */
#line 840 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdOut; }
#line 5324 "prebuilt\\asmparse.cpp"
    break;

  case 317: /* paramAttr: paramAttr '[' OPT_ ']'  */
#line 841 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdOptional; }
#line 5330 "prebuilt\\asmparse.cpp"
    break;

  case 318: /* paramAttr: paramAttr '[' int32 ']'  */
#line 842 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) + 1; }
#line 5336 "prebuilt\\asmparse.cpp"
    break;

  case 319: /* implAttr: %empty  */
#line 845 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (miIL | miManaged); }
#line 5342 "prebuilt\\asmparse.cpp"
    break;

  case 320: /* implAttr: implAttr NATIVE_  */
#line 846 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miNative); }
#line 5348 "prebuilt\\asmparse.cpp"
    break;

  case 321: /* implAttr: implAttr CIL_  */
#line 847 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miIL); }
#line 5354 "prebuilt\\asmparse.cpp"
    break;

  case 322: /* implAttr: implAttr OPTIL_  */
#line 848 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miOPTIL); }
#line 5360 "prebuilt\\asmparse.cpp"
    break;

  case 323: /* implAttr: implAttr MANAGED_  */
#line 849 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFFB) | miManaged); }
#line 5366 "prebuilt\\asmparse.cpp"
    break;

  case 324: /* implAttr: implAttr UNMANAGED_  */
#line 850 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFFB) | miUnmanaged); }
#line 5372 "prebuilt\\asmparse.cpp"
    break;

  case 325: /* implAttr: implAttr FORWARDREF_  */
#line 851 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miForwardRef); }
#line 5378 "prebuilt\\asmparse.cpp"
    break;

  case 326: /* implAttr: implAttr PRESERVESIG_  */
#line 852 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miPreserveSig); }
#line 5384 "prebuilt\\asmparse.cpp"
    break;

  case 327: /* implAttr: implAttr RUNTIME_  */
#line 853 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miRuntime); }
#line 5390 "prebuilt\\asmparse.cpp"
    break;

  case 328: /* implAttr: implAttr INTERNALCALL_  */
#line 854 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miInternalCall); }
#line 5396 "prebuilt\\asmparse.cpp"
    break;

  case 329: /* implAttr: implAttr SYNCHRONIZED_  */
#line 855 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miSynchronized); }
#line 5402 "prebuilt\\asmparse.cpp"
    break;

  case 330: /* implAttr: implAttr NOINLINING_  */
#line 856 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miNoInlining); }
#line 5408 "prebuilt\\asmparse.cpp"
    break;

  case 331: /* implAttr: implAttr AGGRESSIVEINLINING_  */
#line 857 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAggressiveInlining); }
#line 5414 "prebuilt\\asmparse.cpp"
    break;

  case 332: /* implAttr: implAttr NOOPTIMIZATION_  */
#line 858 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miNoOptimization); }
#line 5420 "prebuilt\\asmparse.cpp"
    break;

  case 333: /* implAttr: implAttr AGGRESSIVEOPTIMIZATION_  */
#line 859 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAggressiveOptimization); }
#line 5426 "prebuilt\\asmparse.cpp"
    break;

  case 334: /* implAttr: implAttr ASYNC_  */
#line 860 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAsync); }
#line 5432 "prebuilt\\asmparse.cpp"
    break;

  case 335: /* implAttr: implAttr FLAGS_ '(' int32 ')'  */
#line 861 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].int32)); }
#line 5438 "prebuilt\\asmparse.cpp"
    break;

  case 336: /* localsHead: _LOCALS  */
#line 864 "asmparse.y"
                                                            { PASM->delArgNameList(PASM->m_firstArgName); PASM->m_firstArgName = NULL;PASM->m_lastArgName = NULL;
                                                            }
#line 5445 "prebuilt\\asmparse.cpp"
    break;

  case 339: /* methodDecl: _EMITBYTE int32  */
#line 872 "asmparse.y"
                                                            { PASM->EmitByte((yyvsp[0].int32)); }
#line 5451 "prebuilt\\asmparse.cpp"
    break;

  case 340: /* methodDecl: sehBlock  */
#line 873 "asmparse.y"
                                                            { delete PASM->m_SEHD; PASM->m_SEHD = PASM->m_SEHDstack.POP(); }
#line 5457 "prebuilt\\asmparse.cpp"
    break;

  case 341: /* methodDecl: _MAXSTACK int32  */
#line 874 "asmparse.y"
                                                            { PASM->EmitMaxStack((yyvsp[0].int32)); }
#line 5463 "prebuilt\\asmparse.cpp"
    break;

  case 342: /* methodDecl: localsHead '(' sigArgs0 ')'  */
#line 875 "asmparse.y"
                                                            { PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, (yyvsp[-1].binstr)));
                                                            }
#line 5470 "prebuilt\\asmparse.cpp"
    break;

  case 343: /* methodDecl: localsHead INIT_ '(' sigArgs0 ')'  */
#line 877 "asmparse.y"
                                                            { PASM->EmitZeroInit();
                                                              PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, (yyvsp[-1].binstr)));
                                                            }
#line 5478 "prebuilt\\asmparse.cpp"
    break;

  case 344: /* methodDecl: _ENTRYPOINT  */
#line 880 "asmparse.y"
                                                            { PASM->EmitEntryPoint(); }
#line 5484 "prebuilt\\asmparse.cpp"
    break;

  case 345: /* methodDecl: _ZEROINIT  */
#line 881 "asmparse.y"
                                                            { PASM->EmitZeroInit(); }
#line 5490 "prebuilt\\asmparse.cpp"
    break;

  case 348: /* methodDecl: id ':'  */
#line 884 "asmparse.y"
                                                            { PASM->AddLabel(PASM->m_CurPC,(yyvsp[-1].string)); /*PASM->EmitLabel($1);*/ }
#line 5496 "prebuilt\\asmparse.cpp"
    break;

  case 354: /* methodDecl: _EXPORT '[' int32 ']'  */
#line 890 "asmparse.y"
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
#line 5511 "prebuilt\\asmparse.cpp"
    break;

  case 355: /* methodDecl: _EXPORT '[' int32 ']' AS_ id  */
#line 900 "asmparse.y"
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
#line 5526 "prebuilt\\asmparse.cpp"
    break;

  case 356: /* methodDecl: _VTENTRY int32 ':' int32  */
#line 910 "asmparse.y"
                                                            { PASM->m_pCurMethod->m_wVTEntry = (WORD)(yyvsp[-2].int32);
                                                              PASM->m_pCurMethod->m_wVTSlot = (WORD)(yyvsp[0].int32); }
#line 5533 "prebuilt\\asmparse.cpp"
    break;

  case 357: /* methodDecl: _OVERRIDE typeSpec DCOLON methodName  */
#line 913 "asmparse.y"
                                                            { PASM->AddMethodImpl((yyvsp[-2].token),(yyvsp[0].string),NULL,mdTokenNil,NULL,NULL); }
#line 5539 "prebuilt\\asmparse.cpp"
    break;

  case 358: /* methodDecl: _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'  */
#line 916 "asmparse.y"
                                                            { PASM->AddMethodImpl((yyvsp[-6].token),(yyvsp[-4].string),
                                                              ((yyvsp[-3].int32)==0 ? parser->MakeSig((yyvsp[-8].int32),(yyvsp[-7].binstr),(yyvsp[-1].binstr)) :
                                                              parser->MakeSig((yyvsp[-8].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-7].binstr),(yyvsp[-1].binstr),(yyvsp[-3].int32)))
                                                              ,mdTokenNil,NULL,NULL);
                                                              PASM->ResetArgNameList();
                                                            }
#line 5550 "prebuilt\\asmparse.cpp"
    break;

  case 360: /* methodDecl: _PARAM TYPE_ '[' int32 ']'  */
#line 923 "asmparse.y"
                                                            { if(((yyvsp[-1].int32) > 0) && ((yyvsp[-1].int32) <= (int)PASM->m_pCurMethod->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[(yyvsp[-1].int32)-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
#line 5560 "prebuilt\\asmparse.cpp"
    break;

  case 361: /* methodDecl: _PARAM TYPE_ dottedName  */
#line 928 "asmparse.y"
                                                            { int n = PASM->m_pCurMethod->FindTyPar((yyvsp[0].string));
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",(yyvsp[0].string));
                                                            }
#line 5571 "prebuilt\\asmparse.cpp"
    break;

  case 362: /* methodDecl: _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec  */
#line 934 "asmparse.y"
                                                                        { PASM->m_pCurMethod->AddGenericParamConstraint((yyvsp[-3].int32), 0, (yyvsp[0].token)); }
#line 5577 "prebuilt\\asmparse.cpp"
    break;

  case 363: /* methodDecl: _PARAM CONSTRAINT_ dottedName ',' typeSpec  */
#line 935 "asmparse.y"
                                                                        { PASM->m_pCurMethod->AddGenericParamConstraint(0, (yyvsp[-2].string), (yyvsp[0].token)); }
#line 5583 "prebuilt\\asmparse.cpp"
    break;

  case 364: /* methodDecl: _PARAM '[' int32 ']' initOpt  */
#line 938 "asmparse.y"
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
#line 5606 "prebuilt\\asmparse.cpp"
    break;

  case 365: /* scopeBlock: scopeOpen methodDecls '}'  */
#line 958 "asmparse.y"
                                                            { PASM->m_pCurMethod->CloseScope(); }
#line 5612 "prebuilt\\asmparse.cpp"
    break;

  case 366: /* scopeOpen: '{'  */
#line 961 "asmparse.y"
                                                            { PASM->m_pCurMethod->OpenScope(); }
#line 5618 "prebuilt\\asmparse.cpp"
    break;

  case 370: /* tryBlock: tryHead scopeBlock  */
#line 972 "asmparse.y"
                                                            { PASM->m_SEHD->tryTo = PASM->m_CurPC; }
#line 5624 "prebuilt\\asmparse.cpp"
    break;

  case 371: /* tryBlock: tryHead id TO_ id  */
#line 973 "asmparse.y"
                                                            { PASM->SetTryLabels((yyvsp[-2].string), (yyvsp[0].string)); }
#line 5630 "prebuilt\\asmparse.cpp"
    break;

  case 372: /* tryBlock: tryHead int32 TO_ int32  */
#line 974 "asmparse.y"
                                                            { if(PASM->m_SEHD) {PASM->m_SEHD->tryFrom = (yyvsp[-2].int32);
                                                              PASM->m_SEHD->tryTo = (yyvsp[0].int32);} }
#line 5637 "prebuilt\\asmparse.cpp"
    break;

  case 373: /* tryHead: _TRY  */
#line 978 "asmparse.y"
                                                            { PASM->NewSEHDescriptor();
                                                              PASM->m_SEHD->tryFrom = PASM->m_CurPC; }
#line 5644 "prebuilt\\asmparse.cpp"
    break;

  case 374: /* sehClause: catchClause handlerBlock  */
#line 983 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5650 "prebuilt\\asmparse.cpp"
    break;

  case 375: /* sehClause: filterClause handlerBlock  */
#line 984 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5656 "prebuilt\\asmparse.cpp"
    break;

  case 376: /* sehClause: finallyClause handlerBlock  */
#line 985 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5662 "prebuilt\\asmparse.cpp"
    break;

  case 377: /* sehClause: faultClause handlerBlock  */
#line 986 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5668 "prebuilt\\asmparse.cpp"
    break;

  case 378: /* filterClause: filterHead scopeBlock  */
#line 990 "asmparse.y"
                                                             { PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5674 "prebuilt\\asmparse.cpp"
    break;

  case 379: /* filterClause: filterHead id  */
#line 991 "asmparse.y"
                                                             { PASM->SetFilterLabel((yyvsp[0].string));
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5681 "prebuilt\\asmparse.cpp"
    break;

  case 380: /* filterClause: filterHead int32  */
#line 993 "asmparse.y"
                                                             { PASM->m_SEHD->sehFilter = (yyvsp[0].int32);
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5688 "prebuilt\\asmparse.cpp"
    break;

  case 381: /* filterHead: FILTER_  */
#line 997 "asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FILTER;
                                                               PASM->m_SEHD->sehFilter = PASM->m_CurPC; }
#line 5695 "prebuilt\\asmparse.cpp"
    break;

  case 382: /* catchClause: CATCH_ typeSpec  */
#line 1001 "asmparse.y"
                                                            {  PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_NONE;
                                                               PASM->SetCatchClass((yyvsp[0].token));
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5703 "prebuilt\\asmparse.cpp"
    break;

  case 383: /* finallyClause: FINALLY_  */
#line 1006 "asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FINALLY;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5710 "prebuilt\\asmparse.cpp"
    break;

  case 384: /* faultClause: FAULT_  */
#line 1010 "asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FAULT;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5717 "prebuilt\\asmparse.cpp"
    break;

  case 385: /* handlerBlock: scopeBlock  */
#line 1014 "asmparse.y"
                                                             { PASM->m_SEHD->sehHandlerTo = PASM->m_CurPC; }
#line 5723 "prebuilt\\asmparse.cpp"
    break;

  case 386: /* handlerBlock: HANDLER_ id TO_ id  */
#line 1015 "asmparse.y"
                                                             { PASM->SetHandlerLabels((yyvsp[-2].string), (yyvsp[0].string)); }
#line 5729 "prebuilt\\asmparse.cpp"
    break;

  case 387: /* handlerBlock: HANDLER_ int32 TO_ int32  */
#line 1016 "asmparse.y"
                                                             { PASM->m_SEHD->sehHandler = (yyvsp[-2].int32);
                                                               PASM->m_SEHD->sehHandlerTo = (yyvsp[0].int32); }
#line 5736 "prebuilt\\asmparse.cpp"
    break;

  case 389: /* ddHead: _DATA tls id '='  */
#line 1024 "asmparse.y"
                                                             { PASM->EmitDataLabel((yyvsp[-1].string)); }
#line 5742 "prebuilt\\asmparse.cpp"
    break;

  case 391: /* tls: %empty  */
#line 1028 "asmparse.y"
                                                             { PASM->SetDataSection(); }
#line 5748 "prebuilt\\asmparse.cpp"
    break;

  case 392: /* tls: TLS_  */
#line 1029 "asmparse.y"
                                                             { PASM->SetTLSSection(); }
#line 5754 "prebuilt\\asmparse.cpp"
    break;

  case 393: /* tls: CIL_  */
#line 1030 "asmparse.y"
                                                             { PASM->SetILSection(); }
#line 5760 "prebuilt\\asmparse.cpp"
    break;

  case 398: /* ddItemCount: %empty  */
#line 1041 "asmparse.y"
                                                             { (yyval.int32) = 1; }
#line 5766 "prebuilt\\asmparse.cpp"
    break;

  case 399: /* ddItemCount: '[' int32 ']'  */
#line 1042 "asmparse.y"
                                                             { (yyval.int32) = (yyvsp[-1].int32);
                                                               if((yyvsp[-1].int32) <= 0) { PASM->report->error("Illegal item count: %d\n",(yyvsp[-1].int32));
                                                                  if(!PASM->OnErrGo) (yyval.int32) = 1; }}
#line 5774 "prebuilt\\asmparse.cpp"
    break;

  case 400: /* ddItem: CHAR_ '*' '(' compQstring ')'  */
#line 1047 "asmparse.y"
                                                             { PASM->EmitDataString((yyvsp[-1].binstr)); }
#line 5780 "prebuilt\\asmparse.cpp"
    break;

  case 401: /* ddItem: '&' '(' id ')'  */
#line 1048 "asmparse.y"
                                                             { PASM->EmitDD((yyvsp[-1].string)); }
#line 5786 "prebuilt\\asmparse.cpp"
    break;

  case 402: /* ddItem: bytearrayhead bytes ')'  */
#line 1049 "asmparse.y"
                                                             { PASM->EmitData((yyvsp[-1].binstr)->ptr(),(yyvsp[-1].binstr)->length()); }
#line 5792 "prebuilt\\asmparse.cpp"
    break;

  case 403: /* ddItem: FLOAT32_ '(' float64 ')' ddItemCount  */
#line 1051 "asmparse.y"
                                                             { float f = (float) (*(yyvsp[-2].float64)); float* p = new (nothrow) float[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i < (yyvsp[0].int32); i++) p[i] = f;
                                                                 PASM->EmitData(p, sizeof(float)*(yyvsp[0].int32)); delete (yyvsp[-2].float64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(float)*(yyvsp[0].int32)); }
#line 5803 "prebuilt\\asmparse.cpp"
    break;

  case 404: /* ddItem: FLOAT64_ '(' float64 ')' ddItemCount  */
#line 1058 "asmparse.y"
                                                             { double* p = new (nothrow) double[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = *((yyvsp[-2].float64));
                                                                 PASM->EmitData(p, sizeof(double)*(yyvsp[0].int32)); delete (yyvsp[-2].float64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(double)*(yyvsp[0].int32)); }
#line 5814 "prebuilt\\asmparse.cpp"
    break;

  case 405: /* ddItem: INT64_ '(' int64 ')' ddItemCount  */
#line 1065 "asmparse.y"
                                                             { int64_t* p = new (nothrow) int64_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = *((yyvsp[-2].int64));
                                                                 PASM->EmitData(p, sizeof(int64_t)*(yyvsp[0].int32)); delete (yyvsp[-2].int64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int64_t)*(yyvsp[0].int32)); }
#line 5825 "prebuilt\\asmparse.cpp"
    break;

  case 406: /* ddItem: INT32_ '(' int32 ')' ddItemCount  */
#line 1072 "asmparse.y"
                                                             { int32_t* p = new (nothrow) int32_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = (yyvsp[-2].int32);
                                                                 PASM->EmitData(p, sizeof(int32_t)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int32_t)*(yyvsp[0].int32)); }
#line 5836 "prebuilt\\asmparse.cpp"
    break;

  case 407: /* ddItem: INT16_ '(' int32 ')' ddItemCount  */
#line 1079 "asmparse.y"
                                                             { int16_t i = (int16_t) (yyvsp[-2].int32); FAIL_UNLESS(i == (yyvsp[-2].int32), ("Value %d too big\n", (yyvsp[-2].int32)));
                                                               int16_t* p = new (nothrow) int16_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<(yyvsp[0].int32); j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(int16_t)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int16_t)*(yyvsp[0].int32)); }
#line 5848 "prebuilt\\asmparse.cpp"
    break;

  case 408: /* ddItem: INT8_ '(' int32 ')' ddItemCount  */
#line 1087 "asmparse.y"
                                                             { int8_t i = (int8_t) (yyvsp[-2].int32); FAIL_UNLESS(i == (yyvsp[-2].int32), ("Value %d too big\n", (yyvsp[-2].int32)));
                                                               int8_t* p = new (nothrow) int8_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<(yyvsp[0].int32); j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(int8_t)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int8_t)*(yyvsp[0].int32)); }
#line 5860 "prebuilt\\asmparse.cpp"
    break;

  case 409: /* ddItem: FLOAT32_ ddItemCount  */
#line 1094 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(float)*(yyvsp[0].int32)); }
#line 5866 "prebuilt\\asmparse.cpp"
    break;

  case 410: /* ddItem: FLOAT64_ ddItemCount  */
#line 1095 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(double)*(yyvsp[0].int32)); }
#line 5872 "prebuilt\\asmparse.cpp"
    break;

  case 411: /* ddItem: INT64_ ddItemCount  */
#line 1096 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int64_t)*(yyvsp[0].int32)); }
#line 5878 "prebuilt\\asmparse.cpp"
    break;

  case 412: /* ddItem: INT32_ ddItemCount  */
#line 1097 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int32_t)*(yyvsp[0].int32)); }
#line 5884 "prebuilt\\asmparse.cpp"
    break;

  case 413: /* ddItem: INT16_ ddItemCount  */
#line 1098 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int16_t)*(yyvsp[0].int32)); }
#line 5890 "prebuilt\\asmparse.cpp"
    break;

  case 414: /* ddItem: INT8_ ddItemCount  */
#line 1099 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int8_t)*(yyvsp[0].int32)); }
#line 5896 "prebuilt\\asmparse.cpp"
    break;

  case 415: /* fieldSerInit: FLOAT32_ '(' float64 ')'  */
#line 1103 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               float f = (float)(*(yyvsp[-1].float64));
                                                               (yyval.binstr)->appendInt32(*((int32_t*)&f)); delete (yyvsp[-1].float64); }
#line 5904 "prebuilt\\asmparse.cpp"
    break;

  case 416: /* fieldSerInit: FLOAT64_ '(' float64 ')'  */
#line 1106 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].float64)); delete (yyvsp[-1].float64); }
#line 5911 "prebuilt\\asmparse.cpp"
    break;

  case 417: /* fieldSerInit: FLOAT32_ '(' int32 ')'  */
#line 1108 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 5918 "prebuilt\\asmparse.cpp"
    break;

  case 418: /* fieldSerInit: FLOAT64_ '(' int64 ')'  */
#line 1110 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 5925 "prebuilt\\asmparse.cpp"
    break;

  case 419: /* fieldSerInit: INT64_ '(' int64 ')'  */
#line 1112 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 5932 "prebuilt\\asmparse.cpp"
    break;

  case 420: /* fieldSerInit: INT32_ '(' int32 ')'  */
#line 1114 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 5939 "prebuilt\\asmparse.cpp"
    break;

  case 421: /* fieldSerInit: INT16_ '(' int32 ')'  */
#line 1116 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 5946 "prebuilt\\asmparse.cpp"
    break;

  case 422: /* fieldSerInit: INT8_ '(' int32 ')'  */
#line 1118 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 5953 "prebuilt\\asmparse.cpp"
    break;

  case 423: /* fieldSerInit: UNSIGNED_ INT64_ '(' int64 ')'  */
#line 1120 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 5960 "prebuilt\\asmparse.cpp"
    break;

  case 424: /* fieldSerInit: UNSIGNED_ INT32_ '(' int32 ')'  */
#line 1122 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 5967 "prebuilt\\asmparse.cpp"
    break;

  case 425: /* fieldSerInit: UNSIGNED_ INT16_ '(' int32 ')'  */
#line 1124 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 5974 "prebuilt\\asmparse.cpp"
    break;

  case 426: /* fieldSerInit: UNSIGNED_ INT8_ '(' int32 ')'  */
#line 1126 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 5981 "prebuilt\\asmparse.cpp"
    break;

  case 427: /* fieldSerInit: UINT64_ '(' int64 ')'  */
#line 1128 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 5988 "prebuilt\\asmparse.cpp"
    break;

  case 428: /* fieldSerInit: UINT32_ '(' int32 ')'  */
#line 1130 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 5995 "prebuilt\\asmparse.cpp"
    break;

  case 429: /* fieldSerInit: UINT16_ '(' int32 ')'  */
#line 1132 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6002 "prebuilt\\asmparse.cpp"
    break;

  case 430: /* fieldSerInit: UINT8_ '(' int32 ')'  */
#line 1134 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6009 "prebuilt\\asmparse.cpp"
    break;

  case 431: /* fieldSerInit: CHAR_ '(' int32 ')'  */
#line 1136 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CHAR);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6016 "prebuilt\\asmparse.cpp"
    break;

  case 432: /* fieldSerInit: BOOL_ '(' truefalse ')'  */
#line 1138 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_BOOLEAN);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32));}
#line 6023 "prebuilt\\asmparse.cpp"
    break;

  case 433: /* fieldSerInit: bytearrayhead bytes ')'  */
#line 1140 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING);
                                                               (yyval.binstr)->append((yyvsp[-1].binstr)); delete (yyvsp[-1].binstr);}
#line 6030 "prebuilt\\asmparse.cpp"
    break;

  case 434: /* bytearrayhead: BYTEARRAY_ '('  */
#line 1144 "asmparse.y"
                                                             { bParsingByteArray = TRUE; }
#line 6036 "prebuilt\\asmparse.cpp"
    break;

  case 435: /* bytes: %empty  */
#line 1147 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6042 "prebuilt\\asmparse.cpp"
    break;

  case 436: /* bytes: hexbytes  */
#line 1148 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6048 "prebuilt\\asmparse.cpp"
    break;

  case 437: /* hexbytes: HEXBYTE  */
#line 1151 "asmparse.y"
                                                             { int8_t i = (int8_t) (yyvsp[0].int32); (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(i); }
#line 6054 "prebuilt\\asmparse.cpp"
    break;

  case 438: /* hexbytes: hexbytes HEXBYTE  */
#line 1152 "asmparse.y"
                                                             { int8_t i = (int8_t) (yyvsp[0].int32); (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(i); }
#line 6060 "prebuilt\\asmparse.cpp"
    break;

  case 439: /* fieldInit: fieldSerInit  */
#line 1156 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6066 "prebuilt\\asmparse.cpp"
    break;

  case 440: /* fieldInit: compQstring  */
#line 1157 "asmparse.y"
                                                             { (yyval.binstr) = BinStrToUnicode((yyvsp[0].binstr),true); (yyval.binstr)->insertInt8(ELEMENT_TYPE_STRING);}
#line 6072 "prebuilt\\asmparse.cpp"
    break;

  case 441: /* fieldInit: NULLREF_  */
#line 1158 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CLASS);
                                                               (yyval.binstr)->appendInt32(0); }
#line 6079 "prebuilt\\asmparse.cpp"
    break;

  case 442: /* serInit: fieldSerInit  */
#line 1163 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6085 "prebuilt\\asmparse.cpp"
    break;

  case 443: /* serInit: STRING_ '(' NULLREF_ ')'  */
#line 1164 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); (yyval.binstr)->appendInt8(0xFF); }
#line 6091 "prebuilt\\asmparse.cpp"
    break;

  case 444: /* serInit: STRING_ '(' SQSTRING ')'  */
#line 1165 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[-1].string)); delete [] (yyvsp[-1].string);}
#line 6098 "prebuilt\\asmparse.cpp"
    break;

  case 445: /* serInit: TYPE_ '(' CLASS_ SQSTRING ')'  */
#line 1167 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[-1].string)); delete [] (yyvsp[-1].string);}
#line 6105 "prebuilt\\asmparse.cpp"
    break;

  case 446: /* serInit: TYPE_ '(' className ')'  */
#line 1169 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[-1].token)));}
#line 6112 "prebuilt\\asmparse.cpp"
    break;

  case 447: /* serInit: TYPE_ '(' NULLREF_ ')'  */
#line 1171 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE); (yyval.binstr)->appendInt8(0xFF); }
#line 6118 "prebuilt\\asmparse.cpp"
    break;

  case 448: /* serInit: OBJECT_ '(' serInit ')'  */
#line 1172 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);}
#line 6124 "prebuilt\\asmparse.cpp"
    break;

  case 449: /* serInit: FLOAT32_ '[' int32 ']' '(' f32seq ')'  */
#line 1174 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_R4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6132 "prebuilt\\asmparse.cpp"
    break;

  case 450: /* serInit: FLOAT64_ '[' int32 ']' '(' f64seq ')'  */
#line 1178 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6140 "prebuilt\\asmparse.cpp"
    break;

  case 451: /* serInit: INT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1182 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6148 "prebuilt\\asmparse.cpp"
    break;

  case 452: /* serInit: INT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1186 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6156 "prebuilt\\asmparse.cpp"
    break;

  case 453: /* serInit: INT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1190 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6164 "prebuilt\\asmparse.cpp"
    break;

  case 454: /* serInit: INT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1194 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6172 "prebuilt\\asmparse.cpp"
    break;

  case 455: /* serInit: UINT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1198 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6180 "prebuilt\\asmparse.cpp"
    break;

  case 456: /* serInit: UINT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1202 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6188 "prebuilt\\asmparse.cpp"
    break;

  case 457: /* serInit: UINT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1206 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6196 "prebuilt\\asmparse.cpp"
    break;

  case 458: /* serInit: UINT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1210 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6204 "prebuilt\\asmparse.cpp"
    break;

  case 459: /* serInit: UNSIGNED_ INT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1214 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6212 "prebuilt\\asmparse.cpp"
    break;

  case 460: /* serInit: UNSIGNED_ INT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1218 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6220 "prebuilt\\asmparse.cpp"
    break;

  case 461: /* serInit: UNSIGNED_ INT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1222 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6228 "prebuilt\\asmparse.cpp"
    break;

  case 462: /* serInit: UNSIGNED_ INT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1226 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6236 "prebuilt\\asmparse.cpp"
    break;

  case 463: /* serInit: CHAR_ '[' int32 ']' '(' i16seq ')'  */
#line 1230 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_CHAR);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6244 "prebuilt\\asmparse.cpp"
    break;

  case 464: /* serInit: BOOL_ '[' int32 ']' '(' boolSeq ')'  */
#line 1234 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_BOOLEAN);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6252 "prebuilt\\asmparse.cpp"
    break;

  case 465: /* serInit: STRING_ '[' int32 ']' '(' sqstringSeq ')'  */
#line 1238 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_STRING);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6260 "prebuilt\\asmparse.cpp"
    break;

  case 466: /* serInit: TYPE_ '[' int32 ']' '(' classSeq ')'  */
#line 1242 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TYPE);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6268 "prebuilt\\asmparse.cpp"
    break;

  case 467: /* serInit: OBJECT_ '[' int32 ']' '(' objSeq ')'  */
#line 1246 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6276 "prebuilt\\asmparse.cpp"
    break;

  case 468: /* f32seq: %empty  */
#line 1252 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6282 "prebuilt\\asmparse.cpp"
    break;

  case 469: /* f32seq: f32seq float64  */
#line 1253 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               float f = (float) (*(yyvsp[0].float64)); (yyval.binstr)->appendInt32(*((int32_t*)&f)); delete (yyvsp[0].float64); }
#line 6289 "prebuilt\\asmparse.cpp"
    break;

  case 470: /* f32seq: f32seq int32  */
#line 1255 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 6296 "prebuilt\\asmparse.cpp"
    break;

  case 471: /* f64seq: %empty  */
#line 1259 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6302 "prebuilt\\asmparse.cpp"
    break;

  case 472: /* f64seq: f64seq float64  */
#line 1260 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[0].float64)); delete (yyvsp[0].float64); }
#line 6309 "prebuilt\\asmparse.cpp"
    break;

  case 473: /* f64seq: f64seq int64  */
#line 1262 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[0].int64)); delete (yyvsp[0].int64); }
#line 6316 "prebuilt\\asmparse.cpp"
    break;

  case 474: /* i64seq: %empty  */
#line 1266 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6322 "prebuilt\\asmparse.cpp"
    break;

  case 475: /* i64seq: i64seq int64  */
#line 1267 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[0].int64)); delete (yyvsp[0].int64); }
#line 6329 "prebuilt\\asmparse.cpp"
    break;

  case 476: /* i32seq: %empty  */
#line 1271 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6335 "prebuilt\\asmparse.cpp"
    break;

  case 477: /* i32seq: i32seq int32  */
#line 1272 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt32((yyvsp[0].int32));}
#line 6341 "prebuilt\\asmparse.cpp"
    break;

  case 478: /* i16seq: %empty  */
#line 1275 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6347 "prebuilt\\asmparse.cpp"
    break;

  case 479: /* i16seq: i16seq int32  */
#line 1276 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt16((yyvsp[0].int32));}
#line 6353 "prebuilt\\asmparse.cpp"
    break;

  case 480: /* i8seq: %empty  */
#line 1279 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6359 "prebuilt\\asmparse.cpp"
    break;

  case 481: /* i8seq: i8seq int32  */
#line 1280 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8((yyvsp[0].int32)); }
#line 6365 "prebuilt\\asmparse.cpp"
    break;

  case 482: /* boolSeq: %empty  */
#line 1283 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6371 "prebuilt\\asmparse.cpp"
    break;

  case 483: /* boolSeq: boolSeq truefalse  */
#line 1284 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt8((yyvsp[0].int32));}
#line 6378 "prebuilt\\asmparse.cpp"
    break;

  case 484: /* sqstringSeq: %empty  */
#line 1288 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6384 "prebuilt\\asmparse.cpp"
    break;

  case 485: /* sqstringSeq: sqstringSeq NULLREF_  */
#line 1289 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(0xFF); }
#line 6390 "prebuilt\\asmparse.cpp"
    break;

  case 486: /* sqstringSeq: sqstringSeq SQSTRING  */
#line 1290 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); delete [] (yyvsp[0].string);}
#line 6397 "prebuilt\\asmparse.cpp"
    break;

  case 487: /* classSeq: %empty  */
#line 1294 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6403 "prebuilt\\asmparse.cpp"
    break;

  case 488: /* classSeq: classSeq NULLREF_  */
#line 1295 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(0xFF); }
#line 6409 "prebuilt\\asmparse.cpp"
    break;

  case 489: /* classSeq: classSeq CLASS_ SQSTRING  */
#line 1296 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); delete [] (yyvsp[0].string);}
#line 6416 "prebuilt\\asmparse.cpp"
    break;

  case 490: /* classSeq: classSeq className  */
#line 1298 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[0].token)));}
#line 6423 "prebuilt\\asmparse.cpp"
    break;

  case 491: /* objSeq: %empty  */
#line 1302 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6429 "prebuilt\\asmparse.cpp"
    break;

  case 492: /* objSeq: objSeq serInit  */
#line 1303 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 6435 "prebuilt\\asmparse.cpp"
    break;

  case 493: /* methodSpec: METHOD_  */
#line 1307 "asmparse.y"
                                                             { parser->m_ANSFirst.PUSH(PASM->m_firstArgName);
                                                               parser->m_ANSLast.PUSH(PASM->m_lastArgName);
                                                               PASM->m_firstArgName = NULL;
                                                               PASM->m_lastArgName = NULL; }
#line 6444 "prebuilt\\asmparse.cpp"
    break;

  case 494: /* instr_none: INSTR_NONE  */
#line 1313 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6450 "prebuilt\\asmparse.cpp"
    break;

  case 495: /* instr_var: INSTR_VAR  */
#line 1316 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6456 "prebuilt\\asmparse.cpp"
    break;

  case 496: /* instr_i: INSTR_I  */
#line 1319 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6462 "prebuilt\\asmparse.cpp"
    break;

  case 497: /* instr_i8: INSTR_I8  */
#line 1322 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6468 "prebuilt\\asmparse.cpp"
    break;

  case 498: /* instr_r: INSTR_R  */
#line 1325 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6474 "prebuilt\\asmparse.cpp"
    break;

  case 499: /* instr_brtarget: INSTR_BRTARGET  */
#line 1328 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6480 "prebuilt\\asmparse.cpp"
    break;

  case 500: /* instr_method: INSTR_METHOD  */
#line 1331 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode));
                                                               if((!PASM->OnErrGo)&&
                                                               (((yyvsp[0].opcode) == CEE_NEWOBJ)||
                                                                ((yyvsp[0].opcode) == CEE_CALLVIRT)))
                                                                  iCallConv = IMAGE_CEE_CS_CALLCONV_HASTHIS;
                                                             }
#line 6491 "prebuilt\\asmparse.cpp"
    break;

  case 501: /* instr_field: INSTR_FIELD  */
#line 1339 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6497 "prebuilt\\asmparse.cpp"
    break;

  case 502: /* instr_type: INSTR_TYPE  */
#line 1342 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6503 "prebuilt\\asmparse.cpp"
    break;

  case 503: /* instr_string: INSTR_STRING  */
#line 1345 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6509 "prebuilt\\asmparse.cpp"
    break;

  case 504: /* instr_sig: INSTR_SIG  */
#line 1348 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6515 "prebuilt\\asmparse.cpp"
    break;

  case 505: /* instr_tok: INSTR_TOK  */
#line 1351 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); iOpcodeLen = PASM->OpcodeLen((yyval.instr)); }
#line 6521 "prebuilt\\asmparse.cpp"
    break;

  case 506: /* instr_switch: INSTR_SWITCH  */
#line 1354 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6527 "prebuilt\\asmparse.cpp"
    break;

  case 507: /* instr_r_head: instr_r '('  */
#line 1357 "asmparse.y"
                                                             { (yyval.instr) = (yyvsp[-1].instr); bParsingByteArray = TRUE; }
#line 6533 "prebuilt\\asmparse.cpp"
    break;

  case 508: /* instr: instr_none  */
#line 1361 "asmparse.y"
                                                             { PASM->EmitOpcode((yyvsp[0].instr)); }
#line 6539 "prebuilt\\asmparse.cpp"
    break;

  case 509: /* instr: instr_var int32  */
#line 1362 "asmparse.y"
                                                             { PASM->EmitInstrVar((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 6545 "prebuilt\\asmparse.cpp"
    break;

  case 510: /* instr: instr_var id  */
#line 1363 "asmparse.y"
                                                             { PASM->EmitInstrVarByName((yyvsp[-1].instr), (yyvsp[0].string)); }
#line 6551 "prebuilt\\asmparse.cpp"
    break;

  case 511: /* instr: instr_i int32  */
#line 1364 "asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 6557 "prebuilt\\asmparse.cpp"
    break;

  case 512: /* instr: instr_i8 int64  */
#line 1365 "asmparse.y"
                                                             { PASM->EmitInstrI8((yyvsp[-1].instr), (yyvsp[0].int64)); }
#line 6563 "prebuilt\\asmparse.cpp"
    break;

  case 513: /* instr: instr_r float64  */
#line 1366 "asmparse.y"
                                                             { PASM->EmitInstrR((yyvsp[-1].instr), (yyvsp[0].float64)); delete ((yyvsp[0].float64));}
#line 6569 "prebuilt\\asmparse.cpp"
    break;

  case 514: /* instr: instr_r int64  */
#line 1367 "asmparse.y"
                                                             { double f = (double) (*(yyvsp[0].int64)); PASM->EmitInstrR((yyvsp[-1].instr), &f); }
#line 6575 "prebuilt\\asmparse.cpp"
    break;

  case 515: /* instr: instr_r_head bytes ')'  */
#line 1368 "asmparse.y"
                                                             { unsigned L = (yyvsp[-1].binstr)->length();
                                                               FAIL_UNLESS(L >= sizeof(float), ("%d hexbytes, must be at least %d\n",
                                                                           L,sizeof(float)));
                                                               if(L < sizeof(float)) {YYERROR; }
                                                               else {
                                                                   double f = (L >= sizeof(double)) ? *((double *)((yyvsp[-1].binstr)->ptr()))
                                                                                    : (double)(*(float *)((yyvsp[-1].binstr)->ptr()));
                                                                   PASM->EmitInstrR((yyvsp[-2].instr),&f); }
                                                               delete (yyvsp[-1].binstr); }
#line 6589 "prebuilt\\asmparse.cpp"
    break;

  case 516: /* instr: instr_brtarget int32  */
#line 1377 "asmparse.y"
                                                             { PASM->EmitInstrBrOffset((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 6595 "prebuilt\\asmparse.cpp"
    break;

  case 517: /* instr: instr_brtarget id  */
#line 1378 "asmparse.y"
                                                             { PASM->EmitInstrBrTarget((yyvsp[-1].instr), (yyvsp[0].string)); }
#line 6601 "prebuilt\\asmparse.cpp"
    break;

  case 518: /* instr: instr_method methodRef  */
#line 1380 "asmparse.y"
                                                             { PASM->SetMemberRefFixup((yyvsp[0].token),PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),(yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iCallConv = 0;
                                                             }
#line 6612 "prebuilt\\asmparse.cpp"
    break;

  case 519: /* instr: instr_field type typeSpec DCOLON dottedName  */
#line 1387 "asmparse.y"
                                                             { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef((yyvsp[-2].token), (yyvsp[0].string), (yyvsp[-3].binstr));
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-4].instr)));
                                                               PASM->EmitInstrI((yyvsp[-4].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6624 "prebuilt\\asmparse.cpp"
    break;

  case 520: /* instr: instr_field type dottedName  */
#line 1395 "asmparse.y"
                                                             { (yyvsp[-1].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef(mdTokenNil, (yyvsp[0].string), (yyvsp[-1].binstr));
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-2].instr)));
                                                               PASM->EmitInstrI((yyvsp[-2].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6636 "prebuilt\\asmparse.cpp"
    break;

  case 521: /* instr: instr_field mdtoken  */
#line 1402 "asmparse.y"
                                                             { mdToken mr = (yyvsp[0].token);
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6647 "prebuilt\\asmparse.cpp"
    break;

  case 522: /* instr: instr_field TYPEDEF_F  */
#line 1408 "asmparse.y"
                                                             { mdToken mr = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6658 "prebuilt\\asmparse.cpp"
    break;

  case 523: /* instr: instr_field TYPEDEF_MR  */
#line 1414 "asmparse.y"
                                                             { mdToken mr = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6669 "prebuilt\\asmparse.cpp"
    break;

  case 524: /* instr: instr_type typeSpec  */
#line 1420 "asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr), (yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6678 "prebuilt\\asmparse.cpp"
    break;

  case 525: /* instr: instr_string compQstring  */
#line 1424 "asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-1].instr), (yyvsp[0].binstr),TRUE); }
#line 6684 "prebuilt\\asmparse.cpp"
    break;

  case 526: /* instr: instr_string ANSI_ '(' compQstring ')'  */
#line 1426 "asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-4].instr), (yyvsp[-1].binstr),FALSE); }
#line 6690 "prebuilt\\asmparse.cpp"
    break;

  case 527: /* instr: instr_string bytearrayhead bytes ')'  */
#line 1428 "asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-3].instr), (yyvsp[-1].binstr),FALSE,TRUE); }
#line 6696 "prebuilt\\asmparse.cpp"
    break;

  case 528: /* instr: instr_sig callConv type '(' sigArgs0 ')'  */
#line 1430 "asmparse.y"
                                                             { PASM->EmitInstrSig((yyvsp[-5].instr), parser->MakeSig((yyvsp[-4].int32), (yyvsp[-3].binstr), (yyvsp[-1].binstr)));
                                                               PASM->ResetArgNameList();
                                                             }
#line 6704 "prebuilt\\asmparse.cpp"
    break;

  case 529: /* instr: instr_tok ownerType  */
#line 1434 "asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr),(yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iOpcodeLen = 0;
                                                             }
#line 6714 "prebuilt\\asmparse.cpp"
    break;

  case 530: /* instr: instr_switch '(' labels ')'  */
#line 1439 "asmparse.y"
                                                             { PASM->EmitInstrSwitch((yyvsp[-3].instr), (yyvsp[-1].labels)); }
#line 6720 "prebuilt\\asmparse.cpp"
    break;

  case 531: /* labels: %empty  */
#line 1442 "asmparse.y"
                                                              { (yyval.labels) = 0; }
#line 6726 "prebuilt\\asmparse.cpp"
    break;

  case 532: /* labels: id ',' labels  */
#line 1443 "asmparse.y"
                                                              { (yyval.labels) = new Labels((yyvsp[-2].string), (yyvsp[0].labels), TRUE); }
#line 6732 "prebuilt\\asmparse.cpp"
    break;

  case 533: /* labels: int32 ',' labels  */
#line 1444 "asmparse.y"
                                                              { (yyval.labels) = new Labels((char *)(UINT_PTR)(yyvsp[-2].int32), (yyvsp[0].labels), FALSE); }
#line 6738 "prebuilt\\asmparse.cpp"
    break;

  case 534: /* labels: id  */
#line 1445 "asmparse.y"
                                                              { (yyval.labels) = new Labels((yyvsp[0].string), NULL, TRUE); }
#line 6744 "prebuilt\\asmparse.cpp"
    break;

  case 535: /* labels: int32  */
#line 1446 "asmparse.y"
                                                              { (yyval.labels) = new Labels((char *)(UINT_PTR)(yyvsp[0].int32), NULL, FALSE); }
#line 6750 "prebuilt\\asmparse.cpp"
    break;

  case 536: /* tyArgs0: %empty  */
#line 1450 "asmparse.y"
                                                             { (yyval.binstr) = NULL; }
#line 6756 "prebuilt\\asmparse.cpp"
    break;

  case 537: /* tyArgs0: '<' tyArgs1 '>'  */
#line 1451 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 6762 "prebuilt\\asmparse.cpp"
    break;

  case 538: /* tyArgs1: %empty  */
#line 1454 "asmparse.y"
                                                             { (yyval.binstr) = NULL; }
#line 6768 "prebuilt\\asmparse.cpp"
    break;

  case 539: /* tyArgs1: tyArgs2  */
#line 1455 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6774 "prebuilt\\asmparse.cpp"
    break;

  case 540: /* tyArgs2: type  */
#line 1458 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6780 "prebuilt\\asmparse.cpp"
    break;

  case 541: /* tyArgs2: tyArgs2 ',' type  */
#line 1459 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 6786 "prebuilt\\asmparse.cpp"
    break;

  case 542: /* sigArgs0: %empty  */
#line 1463 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6792 "prebuilt\\asmparse.cpp"
    break;

  case 543: /* sigArgs0: sigArgs1  */
#line 1464 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr);}
#line 6798 "prebuilt\\asmparse.cpp"
    break;

  case 544: /* sigArgs1: sigArg  */
#line 1467 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6804 "prebuilt\\asmparse.cpp"
    break;

  case 545: /* sigArgs1: sigArgs1 ',' sigArg  */
#line 1468 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 6810 "prebuilt\\asmparse.cpp"
    break;

  case 546: /* sigArg: ELLIPSIS  */
#line 1471 "asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_SENTINEL); }
#line 6816 "prebuilt\\asmparse.cpp"
    break;

  case 547: /* sigArg: paramAttr type marshalClause  */
#line 1472 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[-1].binstr)); PASM->addArgName(NULL, (yyvsp[-1].binstr), (yyvsp[0].binstr), (yyvsp[-2].int32)); }
#line 6822 "prebuilt\\asmparse.cpp"
    break;

  case 548: /* sigArg: paramAttr type marshalClause id  */
#line 1473 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[-2].binstr)); PASM->addArgName((yyvsp[0].string), (yyvsp[-2].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32));}
#line 6828 "prebuilt\\asmparse.cpp"
    break;

  case 549: /* className: '[' dottedName ']' slashedName  */
#line 1477 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(PASM->GetAsmRef((yyvsp[-2].string)), (yyvsp[0].string), NULL); delete[] (yyvsp[-2].string);}
#line 6834 "prebuilt\\asmparse.cpp"
    break;

  case 550: /* className: '[' mdtoken ']' slashedName  */
#line 1478 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef((yyvsp[-2].token), (yyvsp[0].string), NULL); }
#line 6840 "prebuilt\\asmparse.cpp"
    break;

  case 551: /* className: '[' '*' ']' slashedName  */
#line 1479 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(mdTokenNil, (yyvsp[0].string), NULL); }
#line 6846 "prebuilt\\asmparse.cpp"
    break;

  case 552: /* className: '[' _MODULE dottedName ']' slashedName  */
#line 1480 "asmparse.y"
                                                                   { (yyval.token) = PASM->ResolveClassRef(PASM->GetModRef((yyvsp[-2].string)),(yyvsp[0].string), NULL); delete[] (yyvsp[-2].string);}
#line 6852 "prebuilt\\asmparse.cpp"
    break;

  case 553: /* className: slashedName  */
#line 1481 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(1,(yyvsp[0].string),NULL); }
#line 6858 "prebuilt\\asmparse.cpp"
    break;

  case 554: /* className: mdtoken  */
#line 1482 "asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].token); }
#line 6864 "prebuilt\\asmparse.cpp"
    break;

  case 555: /* className: TYPEDEF_T  */
#line 1483 "asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 6870 "prebuilt\\asmparse.cpp"
    break;

  case 556: /* className: _THIS  */
#line 1484 "asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) (yyval.token) = PASM->m_pCurClass->m_cl;
                                                                else { (yyval.token) = 0; PASM->report->error(".this outside class scope\n"); }
                                                              }
#line 6878 "prebuilt\\asmparse.cpp"
    break;

  case 557: /* className: _BASE  */
#line 1487 "asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) {
                                                                  (yyval.token) = PASM->m_pCurClass->m_crExtends;
                                                                  if(RidFromToken((yyval.token)) == 0)
                                                                    PASM->report->error(".base undefined\n");
                                                                } else { (yyval.token) = 0; PASM->report->error(".base outside class scope\n"); }
                                                              }
#line 6889 "prebuilt\\asmparse.cpp"
    break;

  case 558: /* className: _NESTER  */
#line 1493 "asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) {
                                                                  if(PASM->m_pCurClass->m_pEncloser != NULL) (yyval.token) = PASM->m_pCurClass->m_pEncloser->m_cl;
                                                                  else { (yyval.token) = 0; PASM->report->error(".nester undefined\n"); }
                                                                } else { (yyval.token) = 0; PASM->report->error(".nester outside class scope\n"); }
                                                              }
#line 6899 "prebuilt\\asmparse.cpp"
    break;

  case 559: /* slashedName: dottedName  */
#line 1500 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 6905 "prebuilt\\asmparse.cpp"
    break;

  case 560: /* slashedName: slashedName '/' dottedName  */
#line 1501 "asmparse.y"
                                                              { (yyval.string) = newStringWDel((yyvsp[-2].string), NESTING_SEP, (yyvsp[0].string)); }
#line 6911 "prebuilt\\asmparse.cpp"
    break;

  case 561: /* typeSpec: className  */
#line 1504 "asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].token);}
#line 6917 "prebuilt\\asmparse.cpp"
    break;

  case 562: /* typeSpec: '[' dottedName ']'  */
#line 1505 "asmparse.y"
                                                              { (yyval.token) = PASM->GetAsmRef((yyvsp[-1].string)); delete[] (yyvsp[-1].string);}
#line 6923 "prebuilt\\asmparse.cpp"
    break;

  case 563: /* typeSpec: '[' _MODULE dottedName ']'  */
#line 1506 "asmparse.y"
                                                              { (yyval.token) = PASM->GetModRef((yyvsp[-1].string)); delete[] (yyvsp[-1].string);}
#line 6929 "prebuilt\\asmparse.cpp"
    break;

  case 564: /* typeSpec: type  */
#line 1507 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveTypeSpec((yyvsp[0].binstr)); }
#line 6935 "prebuilt\\asmparse.cpp"
    break;

  case 565: /* nativeType: %empty  */
#line 1511 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); }
#line 6941 "prebuilt\\asmparse.cpp"
    break;

  case 566: /* nativeType: CUSTOM_ '(' compQstring ',' compQstring ',' compQstring ',' compQstring ')'  */
#line 1513 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt((yyval.binstr),(yyvsp[-7].binstr)->length()); (yyval.binstr)->append((yyvsp[-7].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-5].binstr)->length()); (yyval.binstr)->append((yyvsp[-5].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].binstr)->length()); (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].binstr)->length()); (yyval.binstr)->append((yyvsp[-1].binstr));
                                                                PASM->report->warn("Deprecated 4-string form of custom marshaler, first two strings ignored\n");}
#line 6952 "prebuilt\\asmparse.cpp"
    break;

  case 567: /* nativeType: CUSTOM_ '(' compQstring ',' compQstring ')'  */
#line 1520 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].binstr)->length()); (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].binstr)->length()); (yyval.binstr)->append((yyvsp[-1].binstr)); }
#line 6962 "prebuilt\\asmparse.cpp"
    break;

  case 568: /* nativeType: FIXED_ SYSSTRING_ '[' int32 ']'  */
#line 1525 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FIXEDSYSSTRING);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32)); }
#line 6969 "prebuilt\\asmparse.cpp"
    break;

  case 569: /* nativeType: FIXED_ ARRAY_ '[' int32 ']' nativeType  */
#line 1528 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FIXEDARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-2].int32)); (yyval.binstr)->append((yyvsp[0].binstr)); }
#line 6976 "prebuilt\\asmparse.cpp"
    break;

  case 570: /* nativeType: VARIANT_  */
#line 1530 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VARIANT);
                                                                PASM->report->warn("Deprecated native type 'variant'\n"); }
#line 6983 "prebuilt\\asmparse.cpp"
    break;

  case 571: /* nativeType: CURRENCY_  */
#line 1532 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CURRENCY); }
#line 6989 "prebuilt\\asmparse.cpp"
    break;

  case 572: /* nativeType: SYSCHAR_  */
#line 1533 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SYSCHAR);
                                                                PASM->report->warn("Deprecated native type 'syschar'\n"); }
#line 6996 "prebuilt\\asmparse.cpp"
    break;

  case 573: /* nativeType: VOID_  */
#line 1535 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VOID);
                                                                PASM->report->warn("Deprecated native type 'void'\n"); }
#line 7003 "prebuilt\\asmparse.cpp"
    break;

  case 574: /* nativeType: BOOL_  */
#line 1537 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BOOLEAN); }
#line 7009 "prebuilt\\asmparse.cpp"
    break;

  case 575: /* nativeType: INT8_  */
#line 1538 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I1); }
#line 7015 "prebuilt\\asmparse.cpp"
    break;

  case 576: /* nativeType: INT16_  */
#line 1539 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I2); }
#line 7021 "prebuilt\\asmparse.cpp"
    break;

  case 577: /* nativeType: INT32_  */
#line 1540 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I4); }
#line 7027 "prebuilt\\asmparse.cpp"
    break;

  case 578: /* nativeType: INT64_  */
#line 1541 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I8); }
#line 7033 "prebuilt\\asmparse.cpp"
    break;

  case 579: /* nativeType: FLOAT32_  */
#line 1542 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_R4); }
#line 7039 "prebuilt\\asmparse.cpp"
    break;

  case 580: /* nativeType: FLOAT64_  */
#line 1543 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_R8); }
#line 7045 "prebuilt\\asmparse.cpp"
    break;

  case 581: /* nativeType: ERROR_  */
#line 1544 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ERROR); }
#line 7051 "prebuilt\\asmparse.cpp"
    break;

  case 582: /* nativeType: UNSIGNED_ INT8_  */
#line 1545 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U1); }
#line 7057 "prebuilt\\asmparse.cpp"
    break;

  case 583: /* nativeType: UNSIGNED_ INT16_  */
#line 1546 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U2); }
#line 7063 "prebuilt\\asmparse.cpp"
    break;

  case 584: /* nativeType: UNSIGNED_ INT32_  */
#line 1547 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U4); }
#line 7069 "prebuilt\\asmparse.cpp"
    break;

  case 585: /* nativeType: UNSIGNED_ INT64_  */
#line 1548 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U8); }
#line 7075 "prebuilt\\asmparse.cpp"
    break;

  case 586: /* nativeType: UINT8_  */
#line 1549 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U1); }
#line 7081 "prebuilt\\asmparse.cpp"
    break;

  case 587: /* nativeType: UINT16_  */
#line 1550 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U2); }
#line 7087 "prebuilt\\asmparse.cpp"
    break;

  case 588: /* nativeType: UINT32_  */
#line 1551 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U4); }
#line 7093 "prebuilt\\asmparse.cpp"
    break;

  case 589: /* nativeType: UINT64_  */
#line 1552 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U8); }
#line 7099 "prebuilt\\asmparse.cpp"
    break;

  case 590: /* nativeType: nativeType '*'  */
#line 1553 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(NATIVE_TYPE_PTR);
                                                                PASM->report->warn("Deprecated native type '*'\n"); }
#line 7106 "prebuilt\\asmparse.cpp"
    break;

  case 591: /* nativeType: nativeType '[' ']'  */
#line 1555 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY); }
#line 7113 "prebuilt\\asmparse.cpp"
    break;

  case 592: /* nativeType: nativeType '[' int32 ']'  */
#line 1557 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-3].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32));
                                                                corEmitInt((yyval.binstr),0); }
#line 7123 "prebuilt\\asmparse.cpp"
    break;

  case 593: /* nativeType: nativeType '[' int32 '+' int32 ']'  */
#line 1562 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-5].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32));
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].int32));
                                                                corEmitInt((yyval.binstr),ntaSizeParamIndexSpecified); }
#line 7133 "prebuilt\\asmparse.cpp"
    break;

  case 594: /* nativeType: nativeType '[' '+' int32 ']'  */
#line 1567 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-4].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32)); }
#line 7141 "prebuilt\\asmparse.cpp"
    break;

  case 595: /* nativeType: DECIMAL_  */
#line 1570 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_DECIMAL);
                                                                PASM->report->warn("Deprecated native type 'decimal'\n"); }
#line 7148 "prebuilt\\asmparse.cpp"
    break;

  case 596: /* nativeType: DATE_  */
#line 1572 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_DATE);
                                                                PASM->report->warn("Deprecated native type 'date'\n"); }
#line 7155 "prebuilt\\asmparse.cpp"
    break;

  case 597: /* nativeType: BSTR_  */
#line 1574 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BSTR); }
#line 7161 "prebuilt\\asmparse.cpp"
    break;

  case 598: /* nativeType: LPSTR_  */
#line 1575 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPSTR); }
#line 7167 "prebuilt\\asmparse.cpp"
    break;

  case 599: /* nativeType: LPWSTR_  */
#line 1576 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPWSTR); }
#line 7173 "prebuilt\\asmparse.cpp"
    break;

  case 600: /* nativeType: LPTSTR_  */
#line 1577 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPTSTR); }
#line 7179 "prebuilt\\asmparse.cpp"
    break;

  case 601: /* nativeType: OBJECTREF_  */
#line 1578 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_OBJECTREF);
                                                                PASM->report->warn("Deprecated native type 'objectref'\n"); }
#line 7186 "prebuilt\\asmparse.cpp"
    break;

  case 602: /* nativeType: IUNKNOWN_ iidParamIndex  */
#line 1580 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_IUNKNOWN);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7193 "prebuilt\\asmparse.cpp"
    break;

  case 603: /* nativeType: IDISPATCH_ iidParamIndex  */
#line 1582 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_IDISPATCH);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7200 "prebuilt\\asmparse.cpp"
    break;

  case 604: /* nativeType: STRUCT_  */
#line 1584 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_STRUCT); }
#line 7206 "prebuilt\\asmparse.cpp"
    break;

  case 605: /* nativeType: INTERFACE_ iidParamIndex  */
#line 1585 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_INTF);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7213 "prebuilt\\asmparse.cpp"
    break;

  case 606: /* nativeType: SAFEARRAY_ variantType  */
#line 1587 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[0].int32));
                                                                corEmitInt((yyval.binstr),0);}
#line 7221 "prebuilt\\asmparse.cpp"
    break;

  case 607: /* nativeType: SAFEARRAY_ variantType ',' compQstring  */
#line 1590 "asmparse.y"
                                                                 { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-2].int32));
                                                                corEmitInt((yyval.binstr),(yyvsp[0].binstr)->length()); (yyval.binstr)->append((yyvsp[0].binstr)); }
#line 7229 "prebuilt\\asmparse.cpp"
    break;

  case 608: /* nativeType: INT_  */
#line 1594 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_INT); }
#line 7235 "prebuilt\\asmparse.cpp"
    break;

  case 609: /* nativeType: UNSIGNED_ INT_  */
#line 1595 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_UINT); }
#line 7241 "prebuilt\\asmparse.cpp"
    break;

  case 610: /* nativeType: UINT_  */
#line 1596 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_UINT); }
#line 7247 "prebuilt\\asmparse.cpp"
    break;

  case 611: /* nativeType: NESTED_ STRUCT_  */
#line 1597 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_NESTEDSTRUCT);
                                                                PASM->report->warn("Deprecated native type 'nested struct'\n"); }
#line 7254 "prebuilt\\asmparse.cpp"
    break;

  case 612: /* nativeType: BYVALSTR_  */
#line 1599 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BYVALSTR); }
#line 7260 "prebuilt\\asmparse.cpp"
    break;

  case 613: /* nativeType: ANSI_ BSTR_  */
#line 1600 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ANSIBSTR); }
#line 7266 "prebuilt\\asmparse.cpp"
    break;

  case 614: /* nativeType: TBSTR_  */
#line 1601 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_TBSTR); }
#line 7272 "prebuilt\\asmparse.cpp"
    break;

  case 615: /* nativeType: VARIANT_ BOOL_  */
#line 1602 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VARIANTBOOL); }
#line 7278 "prebuilt\\asmparse.cpp"
    break;

  case 616: /* nativeType: METHOD_  */
#line 1603 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FUNC); }
#line 7284 "prebuilt\\asmparse.cpp"
    break;

  case 617: /* nativeType: AS_ ANY_  */
#line 1604 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ASANY); }
#line 7290 "prebuilt\\asmparse.cpp"
    break;

  case 618: /* nativeType: LPSTRUCT_  */
#line 1605 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPSTRUCT); }
#line 7296 "prebuilt\\asmparse.cpp"
    break;

  case 619: /* nativeType: TYPEDEF_TS  */
#line 1606 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[0].tdd)->m_pbsTypeSpec); }
#line 7302 "prebuilt\\asmparse.cpp"
    break;

  case 620: /* iidParamIndex: %empty  */
#line 1609 "asmparse.y"
                                                              { (yyval.int32) = -1; }
#line 7308 "prebuilt\\asmparse.cpp"
    break;

  case 621: /* iidParamIndex: '(' IIDPARAM_ '=' int32 ')'  */
#line 1610 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32); }
#line 7314 "prebuilt\\asmparse.cpp"
    break;

  case 622: /* variantType: %empty  */
#line 1613 "asmparse.y"
                                                              { (yyval.int32) = VT_EMPTY; }
#line 7320 "prebuilt\\asmparse.cpp"
    break;

  case 623: /* variantType: NULL_  */
#line 1614 "asmparse.y"
                                                              { (yyval.int32) = VT_NULL; }
#line 7326 "prebuilt\\asmparse.cpp"
    break;

  case 624: /* variantType: VARIANT_  */
#line 1615 "asmparse.y"
                                                              { (yyval.int32) = VT_VARIANT; }
#line 7332 "prebuilt\\asmparse.cpp"
    break;

  case 625: /* variantType: CURRENCY_  */
#line 1616 "asmparse.y"
                                                              { (yyval.int32) = VT_CY; }
#line 7338 "prebuilt\\asmparse.cpp"
    break;

  case 626: /* variantType: VOID_  */
#line 1617 "asmparse.y"
                                                              { (yyval.int32) = VT_VOID; }
#line 7344 "prebuilt\\asmparse.cpp"
    break;

  case 627: /* variantType: BOOL_  */
#line 1618 "asmparse.y"
                                                              { (yyval.int32) = VT_BOOL; }
#line 7350 "prebuilt\\asmparse.cpp"
    break;

  case 628: /* variantType: INT8_  */
#line 1619 "asmparse.y"
                                                              { (yyval.int32) = VT_I1; }
#line 7356 "prebuilt\\asmparse.cpp"
    break;

  case 629: /* variantType: INT16_  */
#line 1620 "asmparse.y"
                                                              { (yyval.int32) = VT_I2; }
#line 7362 "prebuilt\\asmparse.cpp"
    break;

  case 630: /* variantType: INT32_  */
#line 1621 "asmparse.y"
                                                              { (yyval.int32) = VT_I4; }
#line 7368 "prebuilt\\asmparse.cpp"
    break;

  case 631: /* variantType: INT64_  */
#line 1622 "asmparse.y"
                                                              { (yyval.int32) = VT_I8; }
#line 7374 "prebuilt\\asmparse.cpp"
    break;

  case 632: /* variantType: FLOAT32_  */
#line 1623 "asmparse.y"
                                                              { (yyval.int32) = VT_R4; }
#line 7380 "prebuilt\\asmparse.cpp"
    break;

  case 633: /* variantType: FLOAT64_  */
#line 1624 "asmparse.y"
                                                              { (yyval.int32) = VT_R8; }
#line 7386 "prebuilt\\asmparse.cpp"
    break;

  case 634: /* variantType: UNSIGNED_ INT8_  */
#line 1625 "asmparse.y"
                                                              { (yyval.int32) = VT_UI1; }
#line 7392 "prebuilt\\asmparse.cpp"
    break;

  case 635: /* variantType: UNSIGNED_ INT16_  */
#line 1626 "asmparse.y"
                                                              { (yyval.int32) = VT_UI2; }
#line 7398 "prebuilt\\asmparse.cpp"
    break;

  case 636: /* variantType: UNSIGNED_ INT32_  */
#line 1627 "asmparse.y"
                                                              { (yyval.int32) = VT_UI4; }
#line 7404 "prebuilt\\asmparse.cpp"
    break;

  case 637: /* variantType: UNSIGNED_ INT64_  */
#line 1628 "asmparse.y"
                                                              { (yyval.int32) = VT_UI8; }
#line 7410 "prebuilt\\asmparse.cpp"
    break;

  case 638: /* variantType: UINT8_  */
#line 1629 "asmparse.y"
                                                              { (yyval.int32) = VT_UI1; }
#line 7416 "prebuilt\\asmparse.cpp"
    break;

  case 639: /* variantType: UINT16_  */
#line 1630 "asmparse.y"
                                                              { (yyval.int32) = VT_UI2; }
#line 7422 "prebuilt\\asmparse.cpp"
    break;

  case 640: /* variantType: UINT32_  */
#line 1631 "asmparse.y"
                                                              { (yyval.int32) = VT_UI4; }
#line 7428 "prebuilt\\asmparse.cpp"
    break;

  case 641: /* variantType: UINT64_  */
#line 1632 "asmparse.y"
                                                              { (yyval.int32) = VT_UI8; }
#line 7434 "prebuilt\\asmparse.cpp"
    break;

  case 642: /* variantType: '*'  */
#line 1633 "asmparse.y"
                                                              { (yyval.int32) = VT_PTR; }
#line 7440 "prebuilt\\asmparse.cpp"
    break;

  case 643: /* variantType: variantType '[' ']'  */
#line 1634 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-2].int32) | VT_ARRAY; }
#line 7446 "prebuilt\\asmparse.cpp"
    break;

  case 644: /* variantType: variantType VECTOR_  */
#line 1635 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32) | VT_VECTOR; }
#line 7452 "prebuilt\\asmparse.cpp"
    break;

  case 645: /* variantType: variantType '&'  */
#line 1636 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32) | VT_BYREF; }
#line 7458 "prebuilt\\asmparse.cpp"
    break;

  case 646: /* variantType: DECIMAL_  */
#line 1637 "asmparse.y"
                                                              { (yyval.int32) = VT_DECIMAL; }
#line 7464 "prebuilt\\asmparse.cpp"
    break;

  case 647: /* variantType: DATE_  */
#line 1638 "asmparse.y"
                                                              { (yyval.int32) = VT_DATE; }
#line 7470 "prebuilt\\asmparse.cpp"
    break;

  case 648: /* variantType: BSTR_  */
#line 1639 "asmparse.y"
                                                              { (yyval.int32) = VT_BSTR; }
#line 7476 "prebuilt\\asmparse.cpp"
    break;

  case 649: /* variantType: LPSTR_  */
#line 1640 "asmparse.y"
                                                              { (yyval.int32) = VT_LPSTR; }
#line 7482 "prebuilt\\asmparse.cpp"
    break;

  case 650: /* variantType: LPWSTR_  */
#line 1641 "asmparse.y"
                                                              { (yyval.int32) = VT_LPWSTR; }
#line 7488 "prebuilt\\asmparse.cpp"
    break;

  case 651: /* variantType: IUNKNOWN_  */
#line 1642 "asmparse.y"
                                                              { (yyval.int32) = VT_UNKNOWN; }
#line 7494 "prebuilt\\asmparse.cpp"
    break;

  case 652: /* variantType: IDISPATCH_  */
#line 1643 "asmparse.y"
                                                              { (yyval.int32) = VT_DISPATCH; }
#line 7500 "prebuilt\\asmparse.cpp"
    break;

  case 653: /* variantType: SAFEARRAY_  */
#line 1644 "asmparse.y"
                                                              { (yyval.int32) = VT_SAFEARRAY; }
#line 7506 "prebuilt\\asmparse.cpp"
    break;

  case 654: /* variantType: INT_  */
#line 1645 "asmparse.y"
                                                              { (yyval.int32) = VT_INT; }
#line 7512 "prebuilt\\asmparse.cpp"
    break;

  case 655: /* variantType: UNSIGNED_ INT_  */
#line 1646 "asmparse.y"
                                                              { (yyval.int32) = VT_UINT; }
#line 7518 "prebuilt\\asmparse.cpp"
    break;

  case 656: /* variantType: UINT_  */
#line 1647 "asmparse.y"
                                                              { (yyval.int32) = VT_UINT; }
#line 7524 "prebuilt\\asmparse.cpp"
    break;

  case 657: /* variantType: ERROR_  */
#line 1648 "asmparse.y"
                                                              { (yyval.int32) = VT_ERROR; }
#line 7530 "prebuilt\\asmparse.cpp"
    break;

  case 658: /* variantType: HRESULT_  */
#line 1649 "asmparse.y"
                                                              { (yyval.int32) = VT_HRESULT; }
#line 7536 "prebuilt\\asmparse.cpp"
    break;

  case 659: /* variantType: CARRAY_  */
#line 1650 "asmparse.y"
                                                              { (yyval.int32) = VT_CARRAY; }
#line 7542 "prebuilt\\asmparse.cpp"
    break;

  case 660: /* variantType: USERDEFINED_  */
#line 1651 "asmparse.y"
                                                              { (yyval.int32) = VT_USERDEFINED; }
#line 7548 "prebuilt\\asmparse.cpp"
    break;

  case 661: /* variantType: RECORD_  */
#line 1652 "asmparse.y"
                                                              { (yyval.int32) = VT_RECORD; }
#line 7554 "prebuilt\\asmparse.cpp"
    break;

  case 662: /* variantType: FILETIME_  */
#line 1653 "asmparse.y"
                                                              { (yyval.int32) = VT_FILETIME; }
#line 7560 "prebuilt\\asmparse.cpp"
    break;

  case 663: /* variantType: BLOB_  */
#line 1654 "asmparse.y"
                                                              { (yyval.int32) = VT_BLOB; }
#line 7566 "prebuilt\\asmparse.cpp"
    break;

  case 664: /* variantType: STREAM_  */
#line 1655 "asmparse.y"
                                                              { (yyval.int32) = VT_STREAM; }
#line 7572 "prebuilt\\asmparse.cpp"
    break;

  case 665: /* variantType: STORAGE_  */
#line 1656 "asmparse.y"
                                                              { (yyval.int32) = VT_STORAGE; }
#line 7578 "prebuilt\\asmparse.cpp"
    break;

  case 666: /* variantType: STREAMED_OBJECT_  */
#line 1657 "asmparse.y"
                                                              { (yyval.int32) = VT_STREAMED_OBJECT; }
#line 7584 "prebuilt\\asmparse.cpp"
    break;

  case 667: /* variantType: STORED_OBJECT_  */
#line 1658 "asmparse.y"
                                                              { (yyval.int32) = VT_STORED_OBJECT; }
#line 7590 "prebuilt\\asmparse.cpp"
    break;

  case 668: /* variantType: BLOB_OBJECT_  */
#line 1659 "asmparse.y"
                                                              { (yyval.int32) = VT_BLOB_OBJECT; }
#line 7596 "prebuilt\\asmparse.cpp"
    break;

  case 669: /* variantType: CF_  */
#line 1660 "asmparse.y"
                                                              { (yyval.int32) = VT_CF; }
#line 7602 "prebuilt\\asmparse.cpp"
    break;

  case 670: /* variantType: CLSID_  */
#line 1661 "asmparse.y"
                                                              { (yyval.int32) = VT_CLSID; }
#line 7608 "prebuilt\\asmparse.cpp"
    break;

  case 671: /* type: CLASS_ className  */
#line 1665 "asmparse.y"
                                                              { if((yyvsp[0].token) == PASM->m_tkSysString)
                                                                {     (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); }
                                                                else if((yyvsp[0].token) == PASM->m_tkSysObject)
                                                                {     (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_OBJECT); }
                                                                else
                                                                 (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CLASS, (yyvsp[0].token)); }
#line 7619 "prebuilt\\asmparse.cpp"
    break;

  case 672: /* type: OBJECT_  */
#line 1671 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_OBJECT); }
#line 7625 "prebuilt\\asmparse.cpp"
    break;

  case 673: /* type: VALUE_ CLASS_ className  */
#line 1672 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, (yyvsp[0].token)); }
#line 7631 "prebuilt\\asmparse.cpp"
    break;

  case 674: /* type: VALUETYPE_ className  */
#line 1673 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, (yyvsp[0].token)); }
#line 7637 "prebuilt\\asmparse.cpp"
    break;

  case 675: /* type: type '[' ']'  */
#line 1674 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 7643 "prebuilt\\asmparse.cpp"
    break;

  case 676: /* type: type '[' bounds1 ']'  */
#line 1675 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeArray(ELEMENT_TYPE_ARRAY, (yyvsp[-3].binstr), (yyvsp[-1].binstr)); }
#line 7649 "prebuilt\\asmparse.cpp"
    break;

  case 677: /* type: type '&'  */
#line 1676 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_BYREF); }
#line 7655 "prebuilt\\asmparse.cpp"
    break;

  case 678: /* type: type '*'  */
#line 1677 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_PTR); }
#line 7661 "prebuilt\\asmparse.cpp"
    break;

  case 679: /* type: type PINNED_  */
#line 1678 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_PINNED); }
#line 7667 "prebuilt\\asmparse.cpp"
    break;

  case 680: /* type: type MODREQ_ '(' typeSpec ')'  */
#line 1679 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_REQD, (yyvsp[-1].token));
                                                                (yyval.binstr)->append((yyvsp[-4].binstr)); }
#line 7674 "prebuilt\\asmparse.cpp"
    break;

  case 681: /* type: type MODOPT_ '(' typeSpec ')'  */
#line 1681 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_OPT, (yyvsp[-1].token));
                                                                (yyval.binstr)->append((yyvsp[-4].binstr)); }
#line 7681 "prebuilt\\asmparse.cpp"
    break;

  case 682: /* type: methodSpec callConv type '*' '(' sigArgs0 ')'  */
#line 1684 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeSig((yyvsp[-5].int32), (yyvsp[-4].binstr), (yyvsp[-1].binstr));
                                                                (yyval.binstr)->insertInt8(ELEMENT_TYPE_FNPTR);
                                                                PASM->delArgNameList(PASM->m_firstArgName);
                                                                PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                                PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                              }
#line 7692 "prebuilt\\asmparse.cpp"
    break;

  case 683: /* type: type '<' tyArgs1 '>'  */
#line 1690 "asmparse.y"
                                                              { if((yyvsp[-1].binstr) == NULL) (yyval.binstr) = (yyvsp[-3].binstr);
                                                                else {
                                                                  (yyval.binstr) = new BinStr();
                                                                  (yyval.binstr)->appendInt8(ELEMENT_TYPE_GENERICINST);
                                                                  (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                  corEmitInt((yyval.binstr), corCountArgs((yyvsp[-1].binstr)));
                                                                  (yyval.binstr)->append((yyvsp[-1].binstr)); delete (yyvsp[-3].binstr); delete (yyvsp[-1].binstr); }}
#line 7704 "prebuilt\\asmparse.cpp"
    break;

  case 684: /* type: '!' '!' int32  */
#line 1697 "asmparse.y"
                                                              { //if(PASM->m_pCurMethod)  {
                                                                //  if(($3 < 0)||((DWORD)$3 >= PASM->m_pCurMethod->m_NumTyPars))
                                                                //    PASM->report->error("Invalid method type parameter '%d'\n",$3);
                                                                  (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_MVAR); corEmitInt((yyval.binstr), (yyvsp[0].int32));
                                                                //} else PASM->report->error("Method type parameter '%d' outside method scope\n",$3);
                                                              }
#line 7715 "prebuilt\\asmparse.cpp"
    break;

  case 685: /* type: '!' int32  */
#line 1703 "asmparse.y"
                                                              { //if(PASM->m_pCurClass)  {
                                                                //  if(($2 < 0)||((DWORD)$2 >= PASM->m_pCurClass->m_NumTyPars))
                                                                //    PASM->report->error("Invalid type parameter '%d'\n",$2);
                                                                  (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_VAR); corEmitInt((yyval.binstr), (yyvsp[0].int32));
                                                                //} else PASM->report->error("Type parameter '%d' outside class scope\n",$2);
                                                              }
#line 7726 "prebuilt\\asmparse.cpp"
    break;

  case 686: /* type: '!' '!' dottedName  */
#line 1709 "asmparse.y"
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
#line 7746 "prebuilt\\asmparse.cpp"
    break;

  case 687: /* type: '!' dottedName  */
#line 1724 "asmparse.y"
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
#line 7766 "prebuilt\\asmparse.cpp"
    break;

  case 688: /* type: TYPEDREF_  */
#line 1739 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_TYPEDBYREF); }
#line 7772 "prebuilt\\asmparse.cpp"
    break;

  case 689: /* type: VOID_  */
#line 1740 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_VOID); }
#line 7778 "prebuilt\\asmparse.cpp"
    break;

  case 690: /* type: NATIVE_ INT_  */
#line 1741 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I); }
#line 7784 "prebuilt\\asmparse.cpp"
    break;

  case 691: /* type: NATIVE_ UNSIGNED_ INT_  */
#line 1742 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U); }
#line 7790 "prebuilt\\asmparse.cpp"
    break;

  case 692: /* type: NATIVE_ UINT_  */
#line 1743 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U); }
#line 7796 "prebuilt\\asmparse.cpp"
    break;

  case 693: /* type: simpleType  */
#line 1744 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7802 "prebuilt\\asmparse.cpp"
    break;

  case 694: /* type: ELLIPSIS type  */
#line 1745 "asmparse.y"
                                                               { (yyval.binstr) = (yyvsp[0].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SENTINEL); }
#line 7808 "prebuilt\\asmparse.cpp"
    break;

  case 695: /* simpleType: CHAR_  */
#line 1748 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CHAR); }
#line 7814 "prebuilt\\asmparse.cpp"
    break;

  case 696: /* simpleType: STRING_  */
#line 1749 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); }
#line 7820 "prebuilt\\asmparse.cpp"
    break;

  case 697: /* simpleType: BOOL_  */
#line 1750 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_BOOLEAN); }
#line 7826 "prebuilt\\asmparse.cpp"
    break;

  case 698: /* simpleType: INT8_  */
#line 1751 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I1); }
#line 7832 "prebuilt\\asmparse.cpp"
    break;

  case 699: /* simpleType: INT16_  */
#line 1752 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I2); }
#line 7838 "prebuilt\\asmparse.cpp"
    break;

  case 700: /* simpleType: INT32_  */
#line 1753 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I4); }
#line 7844 "prebuilt\\asmparse.cpp"
    break;

  case 701: /* simpleType: INT64_  */
#line 1754 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I8); }
#line 7850 "prebuilt\\asmparse.cpp"
    break;

  case 702: /* simpleType: FLOAT32_  */
#line 1755 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4); }
#line 7856 "prebuilt\\asmparse.cpp"
    break;

  case 703: /* simpleType: FLOAT64_  */
#line 1756 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8); }
#line 7862 "prebuilt\\asmparse.cpp"
    break;

  case 704: /* simpleType: UNSIGNED_ INT8_  */
#line 1757 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1); }
#line 7868 "prebuilt\\asmparse.cpp"
    break;

  case 705: /* simpleType: UNSIGNED_ INT16_  */
#line 1758 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2); }
#line 7874 "prebuilt\\asmparse.cpp"
    break;

  case 706: /* simpleType: UNSIGNED_ INT32_  */
#line 1759 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4); }
#line 7880 "prebuilt\\asmparse.cpp"
    break;

  case 707: /* simpleType: UNSIGNED_ INT64_  */
#line 1760 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8); }
#line 7886 "prebuilt\\asmparse.cpp"
    break;

  case 708: /* simpleType: UINT8_  */
#line 1761 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1); }
#line 7892 "prebuilt\\asmparse.cpp"
    break;

  case 709: /* simpleType: UINT16_  */
#line 1762 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2); }
#line 7898 "prebuilt\\asmparse.cpp"
    break;

  case 710: /* simpleType: UINT32_  */
#line 1763 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4); }
#line 7904 "prebuilt\\asmparse.cpp"
    break;

  case 711: /* simpleType: UINT64_  */
#line 1764 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8); }
#line 7910 "prebuilt\\asmparse.cpp"
    break;

  case 712: /* simpleType: TYPEDEF_TS  */
#line 1765 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[0].tdd)->m_pbsTypeSpec); }
#line 7916 "prebuilt\\asmparse.cpp"
    break;

  case 713: /* bounds1: bound  */
#line 1768 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7922 "prebuilt\\asmparse.cpp"
    break;

  case 714: /* bounds1: bounds1 ',' bound  */
#line 1769 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyvsp[-2].binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 7928 "prebuilt\\asmparse.cpp"
    break;

  case 715: /* bound: %empty  */
#line 1772 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0x7FFFFFFF); (yyval.binstr)->appendInt32(0x7FFFFFFF);  }
#line 7934 "prebuilt\\asmparse.cpp"
    break;

  case 716: /* bound: ELLIPSIS  */
#line 1773 "asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0x7FFFFFFF); (yyval.binstr)->appendInt32(0x7FFFFFFF);  }
#line 7940 "prebuilt\\asmparse.cpp"
    break;

  case 717: /* bound: int32  */
#line 1774 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0); (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 7946 "prebuilt\\asmparse.cpp"
    break;

  case 718: /* bound: int32 ELLIPSIS int32  */
#line 1775 "asmparse.y"
                                                               { FAIL_UNLESS((yyvsp[-2].int32) <= (yyvsp[0].int32), ("lower bound %d must be <= upper bound %d\n", (yyvsp[-2].int32), (yyvsp[0].int32)));
                                                                if ((yyvsp[-2].int32) > (yyvsp[0].int32)) { YYERROR; };
                                                                (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[-2].int32)); (yyval.binstr)->appendInt32((yyvsp[0].int32)-(yyvsp[-2].int32)+1); }
#line 7954 "prebuilt\\asmparse.cpp"
    break;

  case 719: /* bound: int32 ELLIPSIS  */
#line 1778 "asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[-1].int32)); (yyval.binstr)->appendInt32(0x7FFFFFFF); }
#line 7960 "prebuilt\\asmparse.cpp"
    break;

  case 720: /* secDecl: _PERMISSION secAction typeSpec '(' nameValPairs ')'  */
#line 1783 "asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-4].secAct), (yyvsp[-3].token), (yyvsp[-1].pair)); }
#line 7966 "prebuilt\\asmparse.cpp"
    break;

  case 721: /* secDecl: _PERMISSION secAction typeSpec '=' '{' customBlobDescr '}'  */
#line 1785 "asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-5].secAct), (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 7972 "prebuilt\\asmparse.cpp"
    break;

  case 722: /* secDecl: _PERMISSION secAction typeSpec  */
#line 1786 "asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-1].secAct), (yyvsp[0].token), (NVPair *)NULL); }
#line 7978 "prebuilt\\asmparse.cpp"
    break;

  case 723: /* secDecl: psetHead bytes ')'  */
#line 1787 "asmparse.y"
                                                              { PASM->AddPermissionSetDecl((yyvsp[-2].secAct), (yyvsp[-1].binstr)); }
#line 7984 "prebuilt\\asmparse.cpp"
    break;

  case 724: /* secDecl: _PERMISSIONSET secAction compQstring  */
#line 1789 "asmparse.y"
                                                              { PASM->AddPermissionSetDecl((yyvsp[-1].secAct),BinStrToUnicode((yyvsp[0].binstr),true));}
#line 7990 "prebuilt\\asmparse.cpp"
    break;

  case 725: /* secDecl: _PERMISSIONSET secAction '=' '{' secAttrSetBlob '}'  */
#line 1791 "asmparse.y"
                                                              { BinStr* ret = new BinStr();
                                                                ret->insertInt8('.');
                                                                corEmitInt(ret, nSecAttrBlobs);
                                                                ret->append((yyvsp[-1].binstr));
                                                                PASM->AddPermissionSetDecl((yyvsp[-4].secAct),ret);
                                                                nSecAttrBlobs = 0; }
#line 8001 "prebuilt\\asmparse.cpp"
    break;

  case 726: /* secAttrSetBlob: %empty  */
#line 1799 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); nSecAttrBlobs = 0;}
#line 8007 "prebuilt\\asmparse.cpp"
    break;

  case 727: /* secAttrSetBlob: secAttrBlob  */
#line 1800 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); nSecAttrBlobs = 1; }
#line 8013 "prebuilt\\asmparse.cpp"
    break;

  case 728: /* secAttrSetBlob: secAttrBlob ',' secAttrSetBlob  */
#line 1801 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); nSecAttrBlobs++; }
#line 8019 "prebuilt\\asmparse.cpp"
    break;

  case 729: /* secAttrBlob: typeSpec '=' '{' customBlobNVPairs '}'  */
#line 1805 "asmparse.y"
                                                              { (yyval.binstr) = PASM->EncodeSecAttr(PASM->ReflectionNotation((yyvsp[-4].token)),(yyvsp[-1].binstr),nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
#line 8026 "prebuilt\\asmparse.cpp"
    break;

  case 730: /* secAttrBlob: CLASS_ SQSTRING '=' '{' customBlobNVPairs '}'  */
#line 1808 "asmparse.y"
                                                              { (yyval.binstr) = PASM->EncodeSecAttr((yyvsp[-4].string),(yyvsp[-1].binstr),nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
#line 8033 "prebuilt\\asmparse.cpp"
    break;

  case 731: /* psetHead: _PERMISSIONSET secAction '=' '('  */
#line 1812 "asmparse.y"
                                                              { (yyval.secAct) = (yyvsp[-2].secAct); bParsingByteArray = TRUE; }
#line 8039 "prebuilt\\asmparse.cpp"
    break;

  case 732: /* psetHead: _PERMISSIONSET secAction BYTEARRAY_ '('  */
#line 1814 "asmparse.y"
                                                              { (yyval.secAct) = (yyvsp[-2].secAct); bParsingByteArray = TRUE; }
#line 8045 "prebuilt\\asmparse.cpp"
    break;

  case 733: /* nameValPairs: nameValPair  */
#line 1817 "asmparse.y"
                                                              { (yyval.pair) = (yyvsp[0].pair); }
#line 8051 "prebuilt\\asmparse.cpp"
    break;

  case 734: /* nameValPairs: nameValPair ',' nameValPairs  */
#line 1818 "asmparse.y"
                                                              { (yyval.pair) = (yyvsp[-2].pair)->Concat((yyvsp[0].pair)); }
#line 8057 "prebuilt\\asmparse.cpp"
    break;

  case 735: /* nameValPair: compQstring '=' caValue  */
#line 1821 "asmparse.y"
                                                              { (yyvsp[-2].binstr)->appendInt8(0); (yyval.pair) = new NVPair((yyvsp[-2].binstr), (yyvsp[0].binstr)); }
#line 8063 "prebuilt\\asmparse.cpp"
    break;

  case 736: /* truefalse: TRUE_  */
#line 1824 "asmparse.y"
                                                              { (yyval.int32) = 1; }
#line 8069 "prebuilt\\asmparse.cpp"
    break;

  case 737: /* truefalse: FALSE_  */
#line 1825 "asmparse.y"
                                                              { (yyval.int32) = 0; }
#line 8075 "prebuilt\\asmparse.cpp"
    break;

  case 738: /* caValue: truefalse  */
#line 1828 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_BOOLEAN);
                                                                (yyval.binstr)->appendInt8((yyvsp[0].int32)); }
#line 8083 "prebuilt\\asmparse.cpp"
    break;

  case 739: /* caValue: int32  */
#line 1831 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_I4);
                                                                (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 8091 "prebuilt\\asmparse.cpp"
    break;

  case 740: /* caValue: INT32_ '(' int32 ')'  */
#line 1834 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_I4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8099 "prebuilt\\asmparse.cpp"
    break;

  case 741: /* caValue: compQstring  */
#line 1837 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_STRING);
                                                                (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr);
                                                                (yyval.binstr)->appendInt8(0); }
#line 8108 "prebuilt\\asmparse.cpp"
    break;

  case 742: /* caValue: className '(' INT8_ ':' int32 ')'  */
#line 1841 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(1);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8119 "prebuilt\\asmparse.cpp"
    break;

  case 743: /* caValue: className '(' INT16_ ':' int32 ')'  */
#line 1847 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(2);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8130 "prebuilt\\asmparse.cpp"
    break;

  case 744: /* caValue: className '(' INT32_ ':' int32 ')'  */
#line 1853 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8141 "prebuilt\\asmparse.cpp"
    break;

  case 745: /* caValue: className '(' int32 ')'  */
#line 1859 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-3].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8152 "prebuilt\\asmparse.cpp"
    break;

  case 746: /* secAction: REQUEST_  */
#line 1867 "asmparse.y"
                                                              { (yyval.secAct) = dclRequest; }
#line 8158 "prebuilt\\asmparse.cpp"
    break;

  case 747: /* secAction: DEMAND_  */
#line 1868 "asmparse.y"
                                                              { (yyval.secAct) = dclDemand; }
#line 8164 "prebuilt\\asmparse.cpp"
    break;

  case 748: /* secAction: ASSERT_  */
#line 1869 "asmparse.y"
                                                              { (yyval.secAct) = dclAssert; }
#line 8170 "prebuilt\\asmparse.cpp"
    break;

  case 749: /* secAction: DENY_  */
#line 1870 "asmparse.y"
                                                              { (yyval.secAct) = dclDeny; }
#line 8176 "prebuilt\\asmparse.cpp"
    break;

  case 750: /* secAction: PERMITONLY_  */
#line 1871 "asmparse.y"
                                                              { (yyval.secAct) = dclPermitOnly; }
#line 8182 "prebuilt\\asmparse.cpp"
    break;

  case 751: /* secAction: LINKCHECK_  */
#line 1872 "asmparse.y"
                                                              { (yyval.secAct) = dclLinktimeCheck; }
#line 8188 "prebuilt\\asmparse.cpp"
    break;

  case 752: /* secAction: INHERITCHECK_  */
#line 1873 "asmparse.y"
                                                              { (yyval.secAct) = dclInheritanceCheck; }
#line 8194 "prebuilt\\asmparse.cpp"
    break;

  case 753: /* secAction: REQMIN_  */
#line 1874 "asmparse.y"
                                                              { (yyval.secAct) = dclRequestMinimum; }
#line 8200 "prebuilt\\asmparse.cpp"
    break;

  case 754: /* secAction: REQOPT_  */
#line 1875 "asmparse.y"
                                                              { (yyval.secAct) = dclRequestOptional; }
#line 8206 "prebuilt\\asmparse.cpp"
    break;

  case 755: /* secAction: REQREFUSE_  */
#line 1876 "asmparse.y"
                                                              { (yyval.secAct) = dclRequestRefuse; }
#line 8212 "prebuilt\\asmparse.cpp"
    break;

  case 756: /* secAction: PREJITGRANT_  */
#line 1877 "asmparse.y"
                                                              { (yyval.secAct) = dclPrejitGrant; }
#line 8218 "prebuilt\\asmparse.cpp"
    break;

  case 757: /* secAction: PREJITDENY_  */
#line 1878 "asmparse.y"
                                                              { (yyval.secAct) = dclPrejitDenied; }
#line 8224 "prebuilt\\asmparse.cpp"
    break;

  case 758: /* secAction: NONCASDEMAND_  */
#line 1879 "asmparse.y"
                                                              { (yyval.secAct) = dclNonCasDemand; }
#line 8230 "prebuilt\\asmparse.cpp"
    break;

  case 759: /* secAction: NONCASLINKDEMAND_  */
#line 1880 "asmparse.y"
                                                              { (yyval.secAct) = dclNonCasLinkDemand; }
#line 8236 "prebuilt\\asmparse.cpp"
    break;

  case 760: /* secAction: NONCASINHERITANCE_  */
#line 1881 "asmparse.y"
                                                              { (yyval.secAct) = dclNonCasInheritance; }
#line 8242 "prebuilt\\asmparse.cpp"
    break;

  case 761: /* esHead: _LINE  */
#line 1885 "asmparse.y"
                                                              { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = FALSE; }
#line 8248 "prebuilt\\asmparse.cpp"
    break;

  case 762: /* esHead: P_LINE  */
#line 1886 "asmparse.y"
                                                              { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = TRUE; }
#line 8254 "prebuilt\\asmparse.cpp"
    break;

  case 763: /* extSourceSpec: esHead int32 SQSTRING  */
#line 1889 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-1].int32);
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8262 "prebuilt\\asmparse.cpp"
    break;

  case 764: /* extSourceSpec: esHead int32  */
#line 1892 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[0].int32);
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1); }
#line 8269 "prebuilt\\asmparse.cpp"
    break;

  case 765: /* extSourceSpec: esHead int32 ':' int32 SQSTRING  */
#line 1894 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-3].int32);
                                                                PENV->nExtCol=(yyvsp[-1].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8277 "prebuilt\\asmparse.cpp"
    break;

  case 766: /* extSourceSpec: esHead int32 ':' int32  */
#line 1897 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-2].int32);
                                                                PENV->nExtCol=(yyvsp[0].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);}
#line 8284 "prebuilt\\asmparse.cpp"
    break;

  case 767: /* extSourceSpec: esHead int32 ':' int32 ',' int32 SQSTRING  */
#line 1900 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-5].int32);
                                                                PENV->nExtCol=(yyvsp[-3].int32); PENV->nExtColEnd = (yyvsp[-1].int32);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8292 "prebuilt\\asmparse.cpp"
    break;

  case 768: /* extSourceSpec: esHead int32 ':' int32 ',' int32  */
#line 1904 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-4].int32);
                                                                PENV->nExtCol=(yyvsp[-2].int32); PENV->nExtColEnd = (yyvsp[0].int32); }
#line 8299 "prebuilt\\asmparse.cpp"
    break;

  case 769: /* extSourceSpec: esHead int32 ',' int32 ':' int32 SQSTRING  */
#line 1907 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-5].int32); PENV->nExtLineEnd = (yyvsp[-3].int32);
                                                                PENV->nExtCol=(yyvsp[-1].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8307 "prebuilt\\asmparse.cpp"
    break;

  case 770: /* extSourceSpec: esHead int32 ',' int32 ':' int32  */
#line 1911 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-4].int32); PENV->nExtLineEnd = (yyvsp[-2].int32);
                                                                PENV->nExtCol=(yyvsp[0].int32); PENV->nExtColEnd = static_cast<unsigned>(-1); }
#line 8314 "prebuilt\\asmparse.cpp"
    break;

  case 771: /* extSourceSpec: esHead int32 ',' int32 ':' int32 ',' int32 SQSTRING  */
#line 1914 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-7].int32); PENV->nExtLineEnd = (yyvsp[-5].int32);
                                                                PENV->nExtCol=(yyvsp[-3].int32); PENV->nExtColEnd = (yyvsp[-1].int32);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8322 "prebuilt\\asmparse.cpp"
    break;

  case 772: /* extSourceSpec: esHead int32 ',' int32 ':' int32 ',' int32  */
#line 1918 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-6].int32); PENV->nExtLineEnd = (yyvsp[-4].int32);
                                                                PENV->nExtCol=(yyvsp[-2].int32); PENV->nExtColEnd = (yyvsp[0].int32); }
#line 8329 "prebuilt\\asmparse.cpp"
    break;

  case 773: /* extSourceSpec: esHead int32 QSTRING  */
#line 1920 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-1].int32) - 1;
                                                                PENV->nExtCol = 0; PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].binstr));}
#line 8337 "prebuilt\\asmparse.cpp"
    break;

  case 774: /* fileDecl: _FILE fileAttr dottedName fileEntry hashHead bytes ')' fileEntry  */
#line 1927 "asmparse.y"
                                                              { PASMM->AddFile((yyvsp[-5].string), (yyvsp[-6].fileAttr)|(yyvsp[-4].fileAttr)|(yyvsp[0].fileAttr), (yyvsp[-2].binstr)); }
#line 8343 "prebuilt\\asmparse.cpp"
    break;

  case 775: /* fileDecl: _FILE fileAttr dottedName fileEntry  */
#line 1928 "asmparse.y"
                                                              { PASMM->AddFile((yyvsp[-1].string), (yyvsp[-2].fileAttr)|(yyvsp[0].fileAttr), NULL); }
#line 8349 "prebuilt\\asmparse.cpp"
    break;

  case 776: /* fileAttr: %empty  */
#line 1931 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0; }
#line 8355 "prebuilt\\asmparse.cpp"
    break;

  case 777: /* fileAttr: fileAttr NOMETADATA_  */
#line 1932 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) ((yyvsp[-1].fileAttr) | ffContainsNoMetaData); }
#line 8361 "prebuilt\\asmparse.cpp"
    break;

  case 778: /* fileEntry: %empty  */
#line 1935 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0; }
#line 8367 "prebuilt\\asmparse.cpp"
    break;

  case 779: /* fileEntry: _ENTRYPOINT  */
#line 1936 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0x80000000; }
#line 8373 "prebuilt\\asmparse.cpp"
    break;

  case 780: /* hashHead: _HASH '=' '('  */
#line 1939 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8379 "prebuilt\\asmparse.cpp"
    break;

  case 781: /* assemblyHead: _ASSEMBLY asmAttr dottedName  */
#line 1942 "asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[0].string), NULL, (DWORD)(yyvsp[-1].asmAttr), FALSE); }
#line 8385 "prebuilt\\asmparse.cpp"
    break;

  case 782: /* asmAttr: %empty  */
#line 1945 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) 0; }
#line 8391 "prebuilt\\asmparse.cpp"
    break;

  case 783: /* asmAttr: asmAttr RETARGETABLE_  */
#line 1946 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afRetargetable); }
#line 8397 "prebuilt\\asmparse.cpp"
    break;

  case 784: /* asmAttr: asmAttr WINDOWSRUNTIME_  */
#line 1947 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afContentType_WindowsRuntime); }
#line 8403 "prebuilt\\asmparse.cpp"
    break;

  case 785: /* asmAttr: asmAttr NOPLATFORM_  */
#line 1948 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afPA_NoPlatform); }
#line 8409 "prebuilt\\asmparse.cpp"
    break;

  case 786: /* asmAttr: asmAttr LEGACY_ LIBRARY_  */
#line 1949 "asmparse.y"
                                                              { (yyval.asmAttr) = (yyvsp[-2].asmAttr); }
#line 8415 "prebuilt\\asmparse.cpp"
    break;

  case 787: /* asmAttr: asmAttr CIL_  */
#line 1950 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_MSIL); }
#line 8421 "prebuilt\\asmparse.cpp"
    break;

  case 788: /* asmAttr: asmAttr X86_  */
#line 1951 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_x86); }
#line 8427 "prebuilt\\asmparse.cpp"
    break;

  case 789: /* asmAttr: asmAttr AMD64_  */
#line 1952 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_AMD64); }
#line 8433 "prebuilt\\asmparse.cpp"
    break;

  case 790: /* asmAttr: asmAttr ARM_  */
#line 1953 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_ARM); }
#line 8439 "prebuilt\\asmparse.cpp"
    break;

  case 791: /* asmAttr: asmAttr ARM64_  */
#line 1954 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_ARM64); }
#line 8445 "prebuilt\\asmparse.cpp"
    break;

  case 794: /* assemblyDecl: _HASH ALGORITHM_ int32  */
#line 1961 "asmparse.y"
                                                              { PASMM->SetAssemblyHashAlg((yyvsp[0].int32)); }
#line 8451 "prebuilt\\asmparse.cpp"
    break;

  case 797: /* intOrWildcard: int32  */
#line 1966 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 8457 "prebuilt\\asmparse.cpp"
    break;

  case 798: /* intOrWildcard: '*'  */
#line 1967 "asmparse.y"
                                                              { (yyval.int32) = 0xFFFF; }
#line 8463 "prebuilt\\asmparse.cpp"
    break;

  case 799: /* asmOrRefDecl: publicKeyHead bytes ')'  */
#line 1970 "asmparse.y"
                                                              { PASMM->SetAssemblyPublicKey((yyvsp[-1].binstr)); }
#line 8469 "prebuilt\\asmparse.cpp"
    break;

  case 800: /* asmOrRefDecl: _VER intOrWildcard ':' intOrWildcard ':' intOrWildcard ':' intOrWildcard  */
#line 1972 "asmparse.y"
                                                              { PASMM->SetAssemblyVer((USHORT)(yyvsp[-6].int32), (USHORT)(yyvsp[-4].int32), (USHORT)(yyvsp[-2].int32), (USHORT)(yyvsp[0].int32)); }
#line 8475 "prebuilt\\asmparse.cpp"
    break;

  case 801: /* asmOrRefDecl: _LOCALE compQstring  */
#line 1973 "asmparse.y"
                                                              { (yyvsp[0].binstr)->appendInt8(0); PASMM->SetAssemblyLocale((yyvsp[0].binstr),TRUE); }
#line 8481 "prebuilt\\asmparse.cpp"
    break;

  case 802: /* asmOrRefDecl: localeHead bytes ')'  */
#line 1974 "asmparse.y"
                                                              { PASMM->SetAssemblyLocale((yyvsp[-1].binstr),FALSE); }
#line 8487 "prebuilt\\asmparse.cpp"
    break;

  case 805: /* publicKeyHead: _PUBLICKEY '=' '('  */
#line 1979 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8493 "prebuilt\\asmparse.cpp"
    break;

  case 806: /* publicKeyTokenHead: _PUBLICKEYTOKEN '=' '('  */
#line 1982 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8499 "prebuilt\\asmparse.cpp"
    break;

  case 807: /* localeHead: _LOCALE '=' '('  */
#line 1985 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8505 "prebuilt\\asmparse.cpp"
    break;

  case 808: /* assemblyRefHead: _ASSEMBLY EXTERN_ asmAttr dottedName  */
#line 1989 "asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[0].string), NULL, (yyvsp[-1].asmAttr), TRUE); }
#line 8511 "prebuilt\\asmparse.cpp"
    break;

  case 809: /* assemblyRefHead: _ASSEMBLY EXTERN_ asmAttr dottedName AS_ dottedName  */
#line 1991 "asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[-2].string), (yyvsp[0].string), (yyvsp[-3].asmAttr), TRUE); }
#line 8517 "prebuilt\\asmparse.cpp"
    break;

  case 812: /* assemblyRefDecl: hashHead bytes ')'  */
#line 1998 "asmparse.y"
                                                              { PASMM->SetAssemblyHashBlob((yyvsp[-1].binstr)); }
#line 8523 "prebuilt\\asmparse.cpp"
    break;

  case 814: /* assemblyRefDecl: publicKeyTokenHead bytes ')'  */
#line 2000 "asmparse.y"
                                                              { PASMM->SetAssemblyPublicKeyToken((yyvsp[-1].binstr)); }
#line 8529 "prebuilt\\asmparse.cpp"
    break;

  case 815: /* assemblyRefDecl: AUTO_  */
#line 2001 "asmparse.y"
                                                              { PASMM->SetAssemblyAutodetect(); }
#line 8535 "prebuilt\\asmparse.cpp"
    break;

  case 816: /* exptypeHead: _CLASS EXTERN_ exptAttr dottedName  */
#line 2004 "asmparse.y"
                                                              { PASMM->StartComType((yyvsp[0].string), (yyvsp[-1].exptAttr));}
#line 8541 "prebuilt\\asmparse.cpp"
    break;

  case 817: /* exportHead: _EXPORT exptAttr dottedName  */
#line 2007 "asmparse.y"
                                                                              { PASMM->StartComType((yyvsp[0].string), (yyvsp[-1].exptAttr)); }
#line 8547 "prebuilt\\asmparse.cpp"
    break;

  case 818: /* exptAttr: %empty  */
#line 2010 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) 0; }
#line 8553 "prebuilt\\asmparse.cpp"
    break;

  case 819: /* exptAttr: exptAttr PRIVATE_  */
#line 2011 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdNotPublic); }
#line 8559 "prebuilt\\asmparse.cpp"
    break;

  case 820: /* exptAttr: exptAttr PUBLIC_  */
#line 2012 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdPublic); }
#line 8565 "prebuilt\\asmparse.cpp"
    break;

  case 821: /* exptAttr: exptAttr FORWARDER_  */
#line 2013 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdForwarder); }
#line 8571 "prebuilt\\asmparse.cpp"
    break;

  case 822: /* exptAttr: exptAttr NESTED_ PUBLIC_  */
#line 2014 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedPublic); }
#line 8577 "prebuilt\\asmparse.cpp"
    break;

  case 823: /* exptAttr: exptAttr NESTED_ PRIVATE_  */
#line 2015 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedPrivate); }
#line 8583 "prebuilt\\asmparse.cpp"
    break;

  case 824: /* exptAttr: exptAttr NESTED_ FAMILY_  */
#line 2016 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamily); }
#line 8589 "prebuilt\\asmparse.cpp"
    break;

  case 825: /* exptAttr: exptAttr NESTED_ ASSEMBLY_  */
#line 2017 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedAssembly); }
#line 8595 "prebuilt\\asmparse.cpp"
    break;

  case 826: /* exptAttr: exptAttr NESTED_ FAMANDASSEM_  */
#line 2018 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamANDAssem); }
#line 8601 "prebuilt\\asmparse.cpp"
    break;

  case 827: /* exptAttr: exptAttr NESTED_ FAMORASSEM_  */
#line 2019 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamORAssem); }
#line 8607 "prebuilt\\asmparse.cpp"
    break;

  case 830: /* exptypeDecl: _FILE dottedName  */
#line 2026 "asmparse.y"
                                                              { PASMM->SetComTypeFile((yyvsp[0].string)); }
#line 8613 "prebuilt\\asmparse.cpp"
    break;

  case 831: /* exptypeDecl: _CLASS EXTERN_ slashedName  */
#line 2027 "asmparse.y"
                                                               { PASMM->SetComTypeComType((yyvsp[0].string)); }
#line 8619 "prebuilt\\asmparse.cpp"
    break;

  case 832: /* exptypeDecl: _ASSEMBLY EXTERN_ dottedName  */
#line 2028 "asmparse.y"
                                                              { PASMM->SetComTypeAsmRef((yyvsp[0].string)); }
#line 8625 "prebuilt\\asmparse.cpp"
    break;

  case 833: /* exptypeDecl: MDTOKEN_ '(' int32 ')'  */
#line 2029 "asmparse.y"
                                                              { if(!PASMM->SetComTypeImplementationTok((yyvsp[-1].int32)))
                                                                  PASM->report->error("Invalid implementation of exported type\n"); }
#line 8632 "prebuilt\\asmparse.cpp"
    break;

  case 834: /* exptypeDecl: _CLASS int32  */
#line 2031 "asmparse.y"
                                                              { if(!PASMM->SetComTypeClassTok((yyvsp[0].int32)))
                                                                  PASM->report->error("Invalid TypeDefID of exported type\n"); }
#line 8639 "prebuilt\\asmparse.cpp"
    break;

  case 837: /* manifestResHead: _MRESOURCE manresAttr dottedName  */
#line 2037 "asmparse.y"
                                                              { PASMM->StartManifestRes((yyvsp[0].string), (yyvsp[0].string), (yyvsp[-1].manresAttr)); }
#line 8645 "prebuilt\\asmparse.cpp"
    break;

  case 838: /* manifestResHead: _MRESOURCE manresAttr dottedName AS_ dottedName  */
#line 2039 "asmparse.y"
                                                              { PASMM->StartManifestRes((yyvsp[-2].string), (yyvsp[0].string), (yyvsp[-3].manresAttr)); }
#line 8651 "prebuilt\\asmparse.cpp"
    break;

  case 839: /* manresAttr: %empty  */
#line 2042 "asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) 0; }
#line 8657 "prebuilt\\asmparse.cpp"
    break;

  case 840: /* manresAttr: manresAttr PUBLIC_  */
#line 2043 "asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) ((yyvsp[-1].manresAttr) | mrPublic); }
#line 8663 "prebuilt\\asmparse.cpp"
    break;

  case 841: /* manresAttr: manresAttr PRIVATE_  */
#line 2044 "asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) ((yyvsp[-1].manresAttr) | mrPrivate); }
#line 8669 "prebuilt\\asmparse.cpp"
    break;

  case 844: /* manifestResDecl: _FILE dottedName AT_ int32  */
#line 2051 "asmparse.y"
                                                              { PASMM->SetManifestResFile((yyvsp[-2].string), (ULONG)(yyvsp[0].int32)); }
#line 8675 "prebuilt\\asmparse.cpp"
    break;

  case 845: /* manifestResDecl: _ASSEMBLY EXTERN_ dottedName  */
#line 2052 "asmparse.y"
                                                              { PASMM->SetManifestResAsmRef((yyvsp[0].string)); }
#line 8681 "prebuilt\\asmparse.cpp"
    break;


#line 8685 "prebuilt\\asmparse.cpp"

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

#line 2057 "asmparse.y"


#include "grammar_after.cpp"
