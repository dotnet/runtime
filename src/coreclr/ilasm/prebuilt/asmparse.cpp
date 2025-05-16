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
    UNMANAGEDEXP_ = 352,           /* UNMANAGEDEXP_  */
    BEFOREFIELDINIT_ = 353,        /* BEFOREFIELDINIT_  */
    ASYNC_ = 354,                  /* ASYNC_  */
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
  YYSYMBOL_UNMANAGEDEXP_ = 97,             /* UNMANAGEDEXP_  */
  YYSYMBOL_BEFOREFIELDINIT_ = 98,          /* BEFOREFIELDINIT_  */
  YYSYMBOL_ASYNC_ = 99,                    /* ASYNC_  */
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
#define YYLAST   3802

/* YYNTOKENS -- Number of terminals.  */
#define YYNTOKENS  309
/* YYNNTS -- Number of nonterminals.  */
#define YYNNTS  186
/* YYNRULES -- Number of rules.  */
#define YYNRULES  848
/* YYNSTATES -- Number of states.  */
#define YYNSTATES  1592

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
     230,   231,   234,   235,   238,   239,   241,   246,   247,   248,
     251,   252,   253,   256,   259,   260,   263,   264,   265,   269,
     270,   271,   272,   273,   278,   279,   280,   281,   284,   287,
     288,   292,   293,   297,   298,   299,   300,   303,   304,   305,
     307,   310,   313,   319,   322,   323,   327,   333,   334,   336,
     339,   340,   346,   349,   350,   353,   357,   358,   366,   367,
     368,   369,   371,   373,   378,   379,   380,   387,   391,   392,
     393,   394,   395,   396,   399,   402,   406,   409,   412,   418,
     421,   422,   423,   424,   425,   426,   427,   428,   429,   430,
     431,   432,   433,   434,   435,   436,   437,   438,   439,   440,
     441,   442,   443,   444,   445,   446,   447,   450,   451,   454,
     455,   458,   459,   462,   463,   467,   468,   471,   472,   475,
     476,   479,   480,   481,   482,   483,   484,   485,   488,   489,
     492,   493,   496,   497,   500,   503,   504,   507,   511,   515,
     516,   517,   518,   519,   520,   521,   522,   523,   524,   525,
     526,   532,   541,   542,   543,   548,   554,   555,   556,   563,
     568,   569,   570,   571,   572,   573,   574,   575,   587,   589,
     590,   591,   592,   593,   594,   595,   598,   599,   602,   603,
     606,   607,   611,   628,   634,   650,   655,   656,   657,   660,
     661,   662,   663,   666,   667,   668,   669,   670,   671,   672,
     673,   676,   679,   684,   688,   692,   694,   696,   701,   702,
     706,   707,   708,   711,   712,   715,   716,   717,   718,   719,
     720,   721,   722,   726,   732,   733,   734,   737,   738,   742,
     743,   744,   745,   746,   747,   748,   752,   758,   759,   762,
     763,   766,   769,   785,   786,   787,   788,   789,   790,   791,
     792,   793,   794,   795,   796,   797,   798,   799,   800,   801,
     802,   803,   804,   805,   808,   811,   816,   817,   818,   819,
     820,   821,   822,   823,   824,   825,   826,   827,   828,   829,
     830,   831,   834,   835,   836,   839,   840,   841,   842,   843,
     846,   847,   848,   849,   850,   851,   852,   853,   854,   855,
     856,   857,   858,   859,   860,   861,   862,   865,   869,   870,
     873,   874,   875,   876,   878,   881,   882,   883,   884,   885,
     886,   887,   888,   889,   890,   891,   901,   911,   913,   916,
     923,   924,   929,   935,   936,   938,   959,   962,   966,   969,
     970,   973,   974,   975,   979,   984,   985,   986,   987,   991,
     992,   994,   998,  1002,  1007,  1011,  1015,  1016,  1017,  1022,
    1025,  1026,  1029,  1030,  1031,  1034,  1035,  1038,  1039,  1042,
    1043,  1048,  1049,  1050,  1051,  1058,  1065,  1072,  1079,  1087,
    1095,  1096,  1097,  1098,  1099,  1100,  1104,  1107,  1109,  1111,
    1113,  1115,  1117,  1119,  1121,  1123,  1125,  1127,  1129,  1131,
    1133,  1135,  1137,  1139,  1141,  1145,  1148,  1149,  1152,  1153,
    1157,  1158,  1159,  1164,  1165,  1166,  1168,  1170,  1172,  1173,
    1174,  1178,  1182,  1186,  1190,  1194,  1198,  1202,  1206,  1210,
    1214,  1218,  1222,  1226,  1230,  1234,  1238,  1242,  1246,  1253,
    1254,  1256,  1260,  1261,  1263,  1267,  1268,  1272,  1273,  1276,
    1277,  1280,  1281,  1284,  1285,  1289,  1290,  1291,  1295,  1296,
    1297,  1299,  1303,  1304,  1308,  1314,  1317,  1320,  1323,  1326,
    1329,  1332,  1340,  1343,  1346,  1349,  1352,  1355,  1358,  1362,
    1363,  1364,  1365,  1366,  1367,  1368,  1369,  1378,  1379,  1380,
    1387,  1395,  1403,  1409,  1415,  1421,  1425,  1426,  1428,  1430,
    1434,  1440,  1443,  1444,  1445,  1446,  1447,  1451,  1452,  1455,
    1456,  1459,  1460,  1464,  1465,  1468,  1469,  1472,  1473,  1474,
    1478,  1479,  1480,  1481,  1482,  1483,  1484,  1485,  1488,  1494,
    1501,  1502,  1505,  1506,  1507,  1508,  1512,  1513,  1520,  1526,
    1528,  1531,  1533,  1534,  1536,  1538,  1539,  1540,  1541,  1542,
    1543,  1544,  1545,  1546,  1547,  1548,  1549,  1550,  1551,  1552,
    1553,  1554,  1556,  1558,  1563,  1568,  1571,  1573,  1575,  1576,
    1577,  1578,  1579,  1581,  1583,  1585,  1586,  1588,  1591,  1595,
    1596,  1597,  1598,  1600,  1601,  1602,  1603,  1604,  1605,  1606,
    1607,  1610,  1611,  1614,  1615,  1616,  1617,  1618,  1619,  1620,
    1621,  1622,  1623,  1624,  1625,  1626,  1627,  1628,  1629,  1630,
    1631,  1632,  1633,  1634,  1635,  1636,  1637,  1638,  1639,  1640,
    1641,  1642,  1643,  1644,  1645,  1646,  1647,  1648,  1649,  1650,
    1651,  1652,  1653,  1654,  1655,  1656,  1657,  1658,  1659,  1660,
    1661,  1662,  1666,  1672,  1673,  1674,  1675,  1676,  1677,  1678,
    1679,  1680,  1682,  1684,  1691,  1698,  1704,  1710,  1725,  1740,
    1741,  1742,  1743,  1744,  1745,  1746,  1749,  1750,  1751,  1752,
    1753,  1754,  1755,  1756,  1757,  1758,  1759,  1760,  1761,  1762,
    1763,  1764,  1765,  1766,  1769,  1770,  1773,  1774,  1775,  1776,
    1779,  1783,  1785,  1787,  1788,  1789,  1791,  1800,  1801,  1802,
    1805,  1808,  1813,  1814,  1818,  1819,  1822,  1825,  1826,  1829,
    1832,  1835,  1838,  1842,  1848,  1854,  1860,  1868,  1869,  1870,
    1871,  1872,  1873,  1874,  1875,  1876,  1877,  1878,  1879,  1880,
    1881,  1882,  1886,  1887,  1890,  1893,  1895,  1898,  1900,  1904,
    1907,  1911,  1914,  1918,  1921,  1927,  1929,  1932,  1933,  1936,
    1937,  1940,  1943,  1946,  1947,  1948,  1949,  1950,  1951,  1952,
    1953,  1954,  1955,  1958,  1959,  1962,  1963,  1964,  1967,  1968,
    1971,  1972,  1974,  1975,  1976,  1977,  1980,  1983,  1986,  1989,
    1991,  1995,  1996,  1999,  2000,  2001,  2002,  2005,  2008,  2011,
    2012,  2013,  2014,  2015,  2016,  2017,  2018,  2019,  2020,  2023,
    2024,  2027,  2028,  2029,  2030,  2032,  2034,  2035,  2038,  2039,
    2043,  2044,  2045,  2048,  2049,  2052,  2053,  2054,  2055
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
  "AGGRESSIVEOPTIMIZATION_", "UNMANAGEDEXP_", "BEFOREFIELDINIT_", "ASYNC_",
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

#define YYPACT_NINF (-1319)

#define yypact_value_is_default(Yyn) \
  ((Yyn) == YYPACT_NINF)

#define YYTABLE_NINF (-561)

#define yytable_value_is_error(Yyn) \
  0

/* YYPACT[STATE-NUM] -- Index in YYTABLE of the portion describing
   STATE-NUM.  */
static const yytype_int16 yypact[] =
{
   -1319,  2171, -1319, -1319,   -94,   493, -1319,  -127,   119,  3116,
    3116, -1319, -1319,   210,   734,   -57,   -50,    25,    66, -1319,
     595,   308,   308,   298,   298,  1699,    53, -1319,   493,   493,
     493,   493, -1319, -1319,   341, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319,   346,   346, -1319, -1319, -1319, -1319,   346,    82,
   -1319,   324,   109, -1319, -1319, -1319, -1319,   501, -1319,   346,
     308, -1319, -1319,   111,   125,   130,   140, -1319, -1319, -1319,
   -1319, -1319, -1319,   157,   308, -1319, -1319, -1319,   632, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319,  2003,    39,    97, -1319, -1319,   175,
     201, -1319, -1319,   844,   731,   731,  1901,   216, -1319,  2959,
   -1319, -1319,   231,   308,   308,   487, -1319,   907,   550,   493,
     157, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,
    2959, -1319, -1319, -1319,  1020, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319,    61, -1319,   480,
      61,   270, -1319,  2449, -1319, -1319, -1319,   185,    34,   157,
     403,   405, -1319,   416,  1804,   424,   257,   559, -1319,    61,
      85,   157,   157,   157, -1319, -1319,   286,   590,   327,   333,
   -1319,  3478,  2003,   505, -1319,  3677,  2391,   292,   176,   183,
     208,   313,   353,   381,   336,   542,   344, -1319, -1319,   346,
     350,    43, -1319, -1319, -1319, -1319,  1380,   493,   362,  2848,
     355,   203, -1319,   731, -1319,    30,   717, -1319,   369,   -42,
     387,   665,   308,   308, -1319, -1319, -1319, -1319, -1319, -1319,
     385, -1319, -1319,    78,  1290, -1319,   398, -1319, -1319,   -47,
     907, -1319, -1319, -1319, -1319,   484, -1319, -1319, -1319, -1319,
     157, -1319, -1319,    -2,   157,   717, -1319, -1319, -1319, -1319,
   -1319,    61, -1319,   707, -1319, -1319, -1319, -1319,  1594,   493,
     438,   110,   446,   575,   157, -1319,   493,   493,   493, -1319,
    2959,   493,   493, -1319,   459,   469,   493,    45,  2959, -1319,
   -1319,   486,    61,   387, -1319, -1319, -1319, -1319,  2895,   526,
   -1319, -1319, -1319, -1319, -1319, -1319,   436, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,
     -96, -1319,  2003, -1319,  3039,   552, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319,   561, -1319, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,   308,
   -1319,   308, -1319, -1319, -1319,   308,   511,   -14,  2062, -1319,
   -1319, -1319,   530, -1319, -1319,   -40, -1319, -1319, -1319, -1319,
     915,   251, -1319, -1319,   325,   308,   298,   506,   325,  1804,
    1152,  2003,   254,   731,  1901,   574,   346, -1319, -1319, -1319,
     576,   308,   308, -1319,   308, -1319,   308, -1319,   298, -1319,
     288, -1319,   288, -1319, -1319,   563,   596,   632,   600, -1319,
   -1319, -1319,   308,   308,  1397,   754,  1972,  1222, -1319, -1319,
   -1319,   913,   157,   157, -1319,   616, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,   645,
      56, -1319,   493,   134,  2959,   940,   662, -1319,  2175, -1319,
     952,   671,   708,   709,  1804, -1319, -1319,   387, -1319, -1319,
     179,    52,   675,   988, -1319, -1319,   803,     2, -1319,   493,
   -1319, -1319,    52,   995,   189,   493,   493,   493,   157, -1319,
     157,   157,   157,  1535,   157,   157,  2003,  2003,   157, -1319,
   -1319,   997,   -62, -1319,   713,   738,   717, -1319, -1319, -1319,
     308, -1319, -1319, -1319, -1319, -1319, -1319,   470, -1319,   739,
   -1319,   933, -1319, -1319, -1319,   308,   308, -1319,   -10,  2272,
   -1319, -1319, -1319, -1319,   760, -1319, -1319,   766,   767, -1319,
   -1319, -1319, -1319,   779,   308,   940,  2117, -1319, -1319,   720,
     308,    75,   143,   308,   731,  1024, -1319,   776,    33,  2490,
   -1319,  2003, -1319, -1319, -1319,   915,    73,   251,    73,    73,
      73,  1010,  1013, -1319, -1319, -1319, -1319, -1319, -1319,   788,
     789, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,
    1594, -1319,   791,   387,   346,  2959, -1319,   325,   796,   940,
     797,   787,   798,   804,   805,   806,   813, -1319,   542,   817,
   -1319,   786,    92,   886,   823,    27,    41, -1319, -1319, -1319,
   -1319, -1319, -1319,   346,   346, -1319,   825,   827, -1319,   346,
   -1319,   346, -1319,   839,    46,   493,   920, -1319, -1319, -1319,
   -1319,   493,   921, -1319, -1319, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319,   308,  3395,    19,   225,   493,   729,   259,
     845,   847, -1319,   637,   850,   851,   857, -1319,  1143, -1319,
   -1319,   854,   872,  3103,  2690,   869,   870,   753,   515,   346,
     493,   157,   493,   493,   257,   257,   257,   871,   874,   875,
     308,   158, -1319, -1319,  2959,   881,   890, -1319, -1319, -1319,
   -1319, -1319, -1319,   470,    83,   884,  2003,  2003,  1758,  1606,
   -1319, -1319,  1380,   207,   211,   731,  1173, -1319, -1319, -1319,
    2654, -1319,   904,   -26,   932,   234,   580,   308,   906,   308,
     157,   308,   -99,   908,  2959,   753,    33, -1319,  2117,   911,
     916, -1319, -1319, -1319, -1319,   325, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319,   632,   308,   308,   298,    52,  1185,
     940,   917,   562,   918,   919,   923, -1319,   418,   924, -1319,
     924,   924,   924,   924,   924, -1319, -1319,   308, -1319,   308,
     308,   929, -1319, -1319,   912,   930,   387,   931,   934,   936,
     937,   938,   939,   308,   493, -1319,   157,   493,    55,   493,
     941, -1319, -1319, -1319,   880, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319,   943,   972,   991,
   -1319,   989,   946,   -83,  1220, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319,   943,   943, -1319,  3011, -1319,
   -1319, -1319, -1319,   947,   346,   294,   632,   949,   493,   684,
   -1319,   940,   961,   956,   965, -1319,  2175, -1319,    98, -1319,
     429,   434,  1026,   458,   461,   468,   485,   514,   517,   520,
     527,   544,   554,   567,   585,   636, -1319,  1052, -1319,   346,
   -1319,   308,   955,    33,    33,   157,   675, -1319, -1319,   632,
   -1319, -1319, -1319,   968,   157,   157,   257,    33, -1319, -1319,
   -1319, -1319,   717, -1319,   308, -1319,  2003,   146,   493, -1319,
   -1319,  1070, -1319, -1319,   492,   493, -1319, -1319,  2959,   157,
     308,   157,   308,   301,  2959,   753,  3183,  1184,  1690, -1319,
    2542, -1319,   940,  1110,   973, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319,   966,   967, -1319,   974,   975,
     977,   979,   978,   753, -1319,  1135,   980,   981,  2003,   949,
    1594, -1319,   987,   580, -1319,  1264,  1224,  1225, -1319, -1319,
     992,   993,   493,   504, -1319,    33,   325,   325, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319,   113,  1283, -1319, -1319,
      27, -1319, -1319, -1319, -1319, -1319, -1319, -1319,   996,   257,
     157,   308,   157, -1319, -1319, -1319, -1319, -1319, -1319,  1043,
   -1319, -1319, -1319, -1319,   940,   998,  1000, -1319, -1319, -1319,
   -1319, -1319,   896, -1319, -1319, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,
   -1319,   441, -1319,    32,    64, -1319, -1319,  3126, -1319,   999,
   -1319, -1319,   387, -1319,  1005, -1319, -1319, -1319, -1319,  1011,
   -1319, -1319, -1319, -1319,   387,   551,   308,   308,   308,   650,
     651,   658,   680,   308,   308,   308,   308,   308,   308,   298,
     308,   866,   308,   819,   308,   308,   308,   308,   308,   308,
     308,   298,   308,  3619,   308,   163,   308,   453,   308, -1319,
   -1319, -1319,  3492,  1006,  1004, -1319,  1009,  1012,  1014,  1029,
   -1319,  1140,  1019,  1033,  1046,  1037, -1319,   470, -1319,   146,
    1804, -1319,   157,    56,  1044,  1047,  2003,  1594,  1090, -1319,
    1804,  1804,  1804,  1804, -1319, -1319, -1319, -1319, -1319, -1319,
    1804,  1804,  1804, -1319, -1319, -1319, -1319, -1319, -1319, -1319,
     387, -1319,   308,   577,   630, -1319, -1319, -1319, -1319,  3395,
    1049,   632, -1319,  1055, -1319, -1319,  1332, -1319,   632, -1319,
     632,   308, -1319, -1319,   157, -1319,  1057, -1319, -1319, -1319,
     308, -1319,  1058, -1319, -1319,  1063,   539,   308,   308, -1319,
   -1319, -1319, -1319, -1319, -1319,   940,  1065, -1319, -1319,   308,
   -1319,  -100,  1068,  1071,  1034,  1072,  1081,  1087,  1089,  1095,
    1096,  1097,  1098,  1100,  1104, -1319,   387, -1319, -1319,   308,
     578, -1319,   794,  1109,  1105,  1102,  1107,  1108,   308,   308,
     308,   308,   308,   308,   298,   308,  1111,  1112,  1113,  1114,
    1117,  1118,  1121,  1120,  1125,  1126,  1123,  1128,  1129,  1127,
    1130,  1131,  1132,  1133,  1134,  1139,  1144,  1142,  1150,  1147,
    1153,  1158,  1148,  1161,  1427,  1162,  1159, -1319,   583, -1319,
     219, -1319, -1319,  1103, -1319, -1319,    33,    33, -1319, -1319,
   -1319, -1319,  2003, -1319, -1319,   546, -1319,  1168, -1319,  1444,
     731, -1319, -1319, -1319, -1319, -1319, -1319, -1319,  1395,  1169,
   -1319, -1319, -1319, -1319,  1178,  1181, -1319,  2003,   753, -1319,
   -1319, -1319, -1319,  1456,    27,   308,   940,  1177,  1180,   387,
   -1319,  1186,   308, -1319,  1183,  1190,  1192,  1194,  1195,  1198,
    1207,  1208,  1202,  1086, -1319, -1319, -1319,  1219, -1319,  1221,
    1229,  1215,  1230,  1217,  1231,  1218,  1232,  1234, -1319,  1237,
   -1319,  1240, -1319,  1242, -1319,  1244, -1319, -1319,  1247, -1319,
   -1319,  1249, -1319,  1250, -1319,  1251, -1319,  1252, -1319,  1254,
   -1319,  1255, -1319, -1319,  1257, -1319,  1258, -1319,  1270,  1482,
   -1319,  1253,   701, -1319,  1272,  1273, -1319,    33,  2003,   753,
    2959, -1319, -1319, -1319,    33, -1319,  1278, -1319,  1216,  1284,
     430, -1319,  3469, -1319,  1282, -1319,   308,   308,   308, -1319,
   -1319, -1319, -1319, -1319,  1288, -1319,  1289, -1319,  1292, -1319,
    1295, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319,  3619, -1319, -1319,
    1296, -1319,  1278,  1594,  1297,  1298,  1306, -1319,    27, -1319,
     940, -1319,   294, -1319,  1313,  1317,  1323,   171,    81, -1319,
   -1319, -1319, -1319,    84,   100,   147,   117,   106,   280,   150,
     152,   164,   133,  2585,    96,    90, -1319,   949,  1329,  1608,
   -1319,    33, -1319,   669, -1319, -1319, -1319, -1319, -1319, -1319,
   -1319,   166,   168,   172,   160, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,  1620, -1319,
   -1319, -1319,    33,   753,  3314,  1337,   940, -1319, -1319, -1319,
   -1319, -1319,  1342,  1345,  1349, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319,   704,  1392,    33,   308, -1319,  1545,  1354,  1356,
     731, -1319, -1319,  2959,  1594,  1635,   753,  1278,  1362,    33,
    1363, -1319
};

/* YYDEFACT[STATE-NUM] -- Default reduction number in state STATE-NUM.
   Performed when YYTABLE does not specify something else to do.  Zero
   means the default is an error.  */
static const yytype_int16 yydefact[] =
{
       2,     0,     1,    87,   107,     0,   266,   210,   392,     0,
       0,   762,   763,     0,   223,     0,     0,   777,   783,   840,
      94,     0,     0,     0,     0,     0,     0,    29,     0,     0,
       0,     0,    59,    60,     0,    62,     3,    25,    26,    27,
      85,    86,   436,   436,    19,    17,    10,     9,   436,     0,
     110,   137,     0,     7,   273,   338,     8,     0,    18,   436,
       0,    11,    12,     0,     0,     0,     0,   819,    37,    41,
      39,    38,    40,   106,     0,   190,   393,   394,   391,   747,
     748,   749,   750,   751,   752,   753,   754,   755,   756,   757,
     758,   759,   760,   761,     0,     0,    34,   217,   218,     0,
       0,   224,   225,   230,   223,   223,     0,    63,    73,     0,
     221,   216,     0,     0,     0,     0,   783,     0,     0,     0,
      95,    43,    20,    21,    45,    44,    23,    24,   556,   713,
       0,   690,   698,   696,     0,   699,   700,   701,   702,   703,
     704,   709,   710,   711,   712,   673,   697,     0,   689,     0,
       0,     0,   494,     0,   557,   558,   559,     0,     0,   560,
       0,     0,   237,     0,   223,     0,   554,     0,   694,    30,
      54,    56,    57,    58,    61,   438,     0,   437,     0,     0,
       2,     0,     0,   139,   141,   223,     0,     0,   399,   399,
     399,   399,   399,   399,     0,     0,     0,   389,   396,   436,
       0,   765,   793,   811,   829,   843,     0,     0,     0,     0,
       0,     0,   555,   223,   562,   723,   565,    32,     0,     0,
     725,     0,     0,     0,   226,   227,   228,   229,   219,   220,
       0,    75,    74,     0,     0,   105,     0,    22,   778,   779,
       0,   784,   785,   786,   788,     0,   789,   790,   791,   792,
     782,   841,   842,   838,    96,   695,   705,   706,   707,   708,
     672,     0,   675,     0,   691,   693,   235,   236,     0,     0,
       0,     0,     0,     0,   688,   686,     0,     0,     0,   232,
       0,     0,     0,   680,     0,     0,     0,   716,   539,   679,
     678,     0,    30,    55,    66,   439,    70,   104,     0,     0,
     113,   134,   111,   112,   115,   116,     0,   117,   118,   119,
     120,   121,   122,   123,   124,   114,   133,   126,   125,   135,
     149,   138,     0,   109,     0,     0,   279,   274,   275,   276,
     277,   278,   282,   280,   290,   281,   283,   284,   285,   286,
     287,   288,   289,     0,   291,   315,   495,   496,   497,   498,
     499,   500,   501,   502,   503,   504,   505,   506,   507,     0,
     374,     0,   337,   345,   346,     0,     0,     0,     0,   367,
       6,   352,     0,   354,   353,     0,   339,   360,   338,   341,
       0,     0,   347,   509,     0,     0,     0,     0,     0,   223,
       0,     0,     0,   223,     0,     0,   436,   348,   350,   351,
       0,     0,     0,   415,     0,   414,     0,   413,     0,   412,
       0,   410,     0,   411,   435,     0,   398,     0,     0,   724,
     774,   764,     0,     0,     0,     0,     0,     0,   822,   821,
     820,     0,   817,    42,   211,     0,   197,   191,   192,   193,
     194,   199,   200,   201,   202,   196,   203,   204,   195,     0,
       0,   390,     0,     0,     0,     0,     0,   733,   727,   732,
       0,    35,     0,     0,   223,    77,    71,    64,   312,   313,
     716,   314,   537,     0,    98,   780,   776,   809,   787,     0,
     674,   692,   234,     0,     0,     0,     0,     0,   687,   685,
      52,    53,    51,     0,    50,   561,     0,     0,    49,   717,
     676,   718,     0,   714,     0,   540,   541,    28,    31,     5,
       0,   127,   128,   129,   130,   131,   132,   158,   108,   140,
     144,     0,   107,   240,   254,     0,     0,   819,     0,     0,
       4,   182,   183,   176,     0,   142,   172,     0,     0,   338,
     173,   174,   175,     0,     0,   296,     0,   340,   342,     0,
       0,     0,     0,     0,   223,     0,   349,     0,   315,     0,
     384,     0,   382,   385,   368,   370,     0,     0,     0,     0,
       0,     0,     0,   371,   511,   510,   512,   513,    46,     0,
       0,   508,   515,   514,   518,   517,   519,   523,   524,   522,
       0,   525,     0,   526,   436,     0,   530,   532,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   395,     0,     0,
     403,     0,   767,     0,     0,     0,     0,    13,   805,   804,
     796,   794,   797,   436,   436,   816,     0,     0,    14,   436,
     814,   436,   812,     0,     0,     0,     0,    15,   837,   836,
     830,     0,     0,    16,   848,   847,   844,   823,   824,   825,
     826,   827,   828,     0,   566,   206,     0,   563,     0,     0,
       0,   734,    77,     0,     0,     0,   728,    33,     0,   222,
     231,    67,     0,    80,   539,     0,     0,     0,     0,   436,
       0,   839,     0,     0,   552,   550,   551,   679,     0,     0,
     720,   716,   677,   684,     0,     0,     0,   153,   155,   154,
     156,   151,   152,   158,     0,     0,     0,     0,     0,   223,
     177,   178,     0,     0,     0,   223,     0,   141,   243,   257,
       0,   829,     0,   296,     0,     0,   267,     0,     0,     0,
     362,     0,     0,     0,     0,     0,   315,   547,     0,     0,
     544,   545,   366,   383,   369,     0,   386,   376,   380,   381,
     379,   375,   377,   378,     0,     0,     0,     0,   521,     0,
       0,     0,     0,   535,   536,     0,   516,     0,   399,   400,
     399,   399,   399,   399,   399,   397,   402,     0,   766,     0,
       0,     0,   799,   798,     0,     0,   802,     0,     0,     0,
       0,     0,     0,     0,     0,   835,   831,     0,     0,     0,
       0,   620,   574,   575,     0,   609,   576,   577,   578,   579,
     580,   581,   611,   587,   588,   589,   590,   621,     0,     0,
     617,     0,     0,     0,   571,   572,   573,   596,   597,   598,
     615,   599,   600,   601,   602,   621,   621,   605,   623,   613,
     619,   582,   271,     0,     0,   269,     0,   208,   564,     0,
     721,     0,     0,    39,     0,   726,   727,    36,     0,    65,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,    79,    76,   443,   436,
      78,     0,     0,   315,   315,   314,   537,    99,   100,     0,
     101,   102,   103,     0,   810,   233,   553,   315,   681,   682,
     719,   715,   542,   136,     0,   159,   145,   162,     0,   150,
     143,     0,   242,   241,   560,     0,   256,   255,     0,   818,
       0,   185,     0,     0,     0,     0,     0,     0,     0,   168,
       0,   292,     0,     0,     0,   303,   304,   305,   306,   298,
     299,   300,   297,   301,   302,     0,     0,   295,     0,     0,
       0,     0,     0,     0,   357,   355,     0,     0,     0,   208,
       0,   358,     0,   267,   343,   315,     0,     0,   372,   373,
       0,     0,     0,     0,   528,   315,   532,   532,   531,   401,
     409,   408,   407,   406,   404,   405,   771,   769,   795,   806,
       0,   808,   800,   803,   781,   807,   813,   815,     0,   832,
     833,     0,   846,   205,   610,   583,   584,   585,   586,     0,
     606,   612,   614,   618,     0,     0,     0,   616,   603,   604,
     627,   628,     0,   655,   629,   630,   631,   632,   633,   634,
     657,   639,   640,   641,   642,   625,   626,   647,   648,   649,
     650,   651,   652,   653,   654,   624,   658,   659,   660,   661,
     662,   663,   664,   665,   666,   667,   668,   669,   670,   671,
     643,   607,   198,     0,     0,   591,   207,     0,   189,     0,
     737,   738,   742,   740,     0,   739,   736,   735,   722,     0,
      80,   729,    77,    72,    68,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,    83,
      84,    82,     0,     0,     0,   538,     0,     0,     0,     0,
      97,   779,     0,     0,     0,   146,   147,   158,   161,   162,
     223,   188,   238,     0,     0,     0,     0,     0,     0,   169,
     223,   223,   223,   223,   170,   251,   252,   250,   244,   249,
     223,   223,   223,   171,   264,   265,   262,   258,   263,   179,
     296,   294,     0,     0,     0,   316,   317,   318,   319,   566,
     149,     0,   361,     0,   364,   365,     0,   344,   548,   546,
       0,     0,    47,    48,   520,   527,     0,   533,   534,   770,
       0,   768,     0,   834,   845,     0,     0,     0,     0,   656,
     635,   636,   637,   638,   645,     0,     0,   646,   270,     0,
     592,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,   442,   441,   440,   209,     0,
       0,    80,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,    90,     0,    89,
       0,    88,   434,     0,   215,   214,   315,   315,   775,   683,
     157,   164,     0,   163,   160,     0,   184,     0,   187,     0,
     223,   245,   246,   247,   248,   261,   259,   260,     0,     0,
     307,   308,   309,   310,     0,     0,   356,     0,     0,   549,
     387,   388,   529,   773,     0,     0,     0,     0,     0,   608,
     644,     0,     0,   593,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   730,    69,   433,     0,   432,     0,
       0,     0,     0,     0,     0,     0,     0,     0,   423,     0,
     422,     0,   421,     0,   420,     0,   418,   416,     0,   419,
     417,     0,   431,     0,   430,     0,   429,     0,   428,     0,
     449,     0,   445,   444,     0,   448,     0,   447,     0,     0,
      92,     0,     0,   167,     0,     0,   148,   315,     0,     0,
       0,   293,   311,   268,   315,   363,   165,   772,     0,     0,
       0,   569,   566,   595,     0,   741,     0,     0,     0,   746,
     731,   483,   479,   427,     0,   426,     0,   425,     0,   424,
       0,   481,   479,   477,   475,   469,   472,   481,   479,   477,
     475,   492,   485,   446,   488,    91,    93,     0,   213,   212,
       0,   186,   165,     0,     0,     0,     0,   166,     0,   622,
       0,   568,   570,   594,     0,     0,     0,     0,     0,   481,
     479,   477,   475,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,    81,   208,     0,     0,
     320,   315,   801,     0,   743,   744,   745,   465,   484,   464,
     480,     0,     0,     0,     0,   455,   482,   454,   453,   478,
     452,   476,   450,   471,   470,   451,   474,   473,   459,   458,
     457,   456,   468,   493,   487,   486,   466,   489,     0,   467,
     491,   253,   315,     0,     0,     0,     0,   463,   462,   461,
     460,   490,     0,     0,     0,   325,   321,   330,   331,   332,
     333,   334,   335,   322,   323,   324,   326,   327,   328,   329,
     272,   359,     0,     0,   315,     0,   567,     0,     0,     0,
     223,   180,   336,     0,     0,     0,     0,   165,     0,   315,
       0,   181
};

/* YYPGOTO[NTERM-NUM].  */
static const yytype_int16 yypgoto[] =
{
   -1319,  1478, -1319,  1369,   -54,     0,    88,    -5,    10,    36,
    -399, -1319,    11,   -11,  1640, -1319, -1319,  1203,  1274,  -632,
   -1319,  -974, -1319,    28, -1319, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319,  -315, -1319, -1319, -1319,   953, -1319, -1319,
   -1319,   489, -1319,   963,   534,   533, -1319, -1318,  -437, -1319,
    -305, -1319, -1319,  -938, -1319,  -159,   -77, -1319,    -7,  1648,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,   711,
     496, -1319,  -304, -1319,  -689,  -667,  1331, -1319, -1319,  -246,
   -1319,  -141, -1319, -1319,  1122, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319,   348,     7, -1319, -1319, -1319,  1078,  -114,
    1632,   623,   -41,     4,   855, -1319, -1076, -1319, -1319, -1236,
   -1105, -1127, -1125, -1319, -1319, -1319, -1319,    13, -1319, -1319,
   -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,
   -1319, -1319, -1319,     8,   820,  1035, -1319,  -687, -1319,   745,
     -22,  -429,   -88,   281,   162, -1319,   -23,   589, -1319,  1022,
       3,   858, -1319, -1319,   865, -1319, -1052, -1319,  1707, -1319,
      16, -1319, -1319,   587,  1256, -1319,  1613, -1319, -1319,  -968,
    1308, -1319, -1319, -1319, -1319, -1319, -1319, -1319, -1319,  1211,
    1018, -1319, -1319, -1319, -1319, -1319
};

/* YYDEFGOTO[NTERM-NUM].  */
static const yytype_int16 yydefgoto[] =
{
       0,     1,    36,   291,   659,   371,    72,   159,   783,  1521,
     583,    38,   373,    40,    41,    42,    43,   107,   230,   672,
     673,   877,  1122,   374,  1290,    45,    46,   678,    47,    48,
      49,    50,    51,    52,   181,   183,   323,   324,   519,  1134,
    1135,   518,   703,   704,   705,  1138,   908,  1466,  1467,   535,
      53,   209,   847,  1068,    75,   108,   109,   110,   212,   231,
     537,   708,   927,  1158,   538,   709,   928,  1167,    54,   953,
     843,   844,    55,   185,   724,   472,   738,  1544,   375,   186,
     376,   746,   378,   379,   564,   380,   381,   565,   566,   567,
     568,   569,   570,   747,   382,    57,    78,   197,   415,   403,
     416,   878,   879,   176,   177,  1238,   880,  1487,  1488,  1486,
    1485,  1478,  1483,  1477,  1494,  1495,  1493,   213,   383,   384,
     385,   386,   387,   388,   389,   390,   391,   392,   393,   394,
     395,   396,   397,   765,   676,   504,   505,   739,   740,   741,
     214,   166,   232,   845,  1010,  1061,   216,   168,   502,   503,
     398,   665,   666,    59,   660,   661,  1075,  1076,    94,    60,
     399,    62,   115,   476,   629,    63,   117,   424,   621,   784,
     622,   623,   631,   624,    64,   425,   632,    65,   543,   206,
     426,   640,    66,   118,   427,   646
};

/* YYTABLE[YYPACT[STATE-NUM]] -- What to do in state STATE-NUM.  If
   positive, shift that token.  If negative, reduce the rule whose
   number is the opposite.  If YYTABLE_NINF, syntax error.  */
static const yytype_int16 yytable[] =
{
      73,    37,   167,   165,    58,   279,   215,   111,    56,   534,
     886,   605,    39,   606,   160,   120,   199,    61,   162,   536,
     539,  1185,  1202,   170,   171,   172,   173,   228,   229,    44,
     852,   122,   123,  1244,   933,   675,   121,  1278,   164,    68,
      69,   220,    70,   121,   295,   377,   217,   178,   217,   962,
     420,   421,   179,   737,   121,   121,   684,   685,   686,   126,
     127,    68,    69,   200,    70,   499,    68,    69,   961,    70,
     201,  -560,   218,   121,   128,   405,   407,   409,   411,   413,
      68,    69,   846,    70,   208,   217,   234,   280,    68,    69,
     121,    70,   217,   121,   321,    68,    69,   551,    70,   162,
     778,   713,   100,   128,  1534,   217,  1242,   255,   345,   121,
     239,   932,   250,   253,   254,   121,   293,   578,  1001,   164,
     475,  1199,    67,   236,   237,   260,   124,   125,   262,   745,
     268,   100,   559,    71,  1537,   479,   454,   579,   580,   680,
    1535,  1538,   124,   125,  1498,  1015,   473,   292,    68,    69,
     272,    70,   271,   274,   199,    71,   121,   111,   557,   121,
      71,   121,   283,   284,   285,  1016,   210,   121,   275,   124,
     125,  1280,    74,   121,    71,   121,   320,   121,   499,   467,
     483,   121,    71,    76,    68,    69,   450,    70,   121,    71,
      68,    69,  1342,    70,   958,   207,  1126,  1127,   207,   499,
    1343,   432,   433,   418,   272,   517,   453,  1281,    68,    69,
    1132,    70,    68,    69,  1492,    70,    68,    69,    96,    70,
     100,   154,   155,   156,    68,    69,   100,    70,   685,   471,
     586,   691,   462,   463,   520,   477,  1070,  1071,   692,   480,
     573,   112,    71,   121,   100,    77,  1514,   207,   458,   113,
     154,   155,   156,   459,   896,   558,    68,   493,  1148,    70,
     121,   217,   794,   482,   484,   506,   460,  1353,   488,  1588,
     292,   490,   491,   492,   372,   552,   494,   495,    71,   714,
     555,   498,   116,   489,    71,   553,  1180,   194,  1196,   124,
     125,   578,   207,   720,   263,   264,   207,   501,    37,   578,
     114,    58,    71,   591,   265,    56,    71,   124,   125,    39,
      71,   579,   580,   207,    61,  1484,   595,   121,    71,   579,
     580,  1490,  1489,  1218,   531,   455,    44,   541,   456,  -543,
      68,   540,   782,    70,   121,   532,   422,   219,   593,   785,
     542,   592,   273,   169,  1491,   500,   207,   423,   174,   207,
      71,   594,   533,  1512,  1511,   287,  1219,   288,   175,  -560,
     157,   289,   290,   369,  1220,   999,  1418,   590,   465,   547,
     664,   548,   180,   466,   729,   549,  1513,  1509,   906,   207,
    1515,  1496,   111,   589,   182,   779,  1539,   162,  1082,   157,
     221,   572,  1536,  1083,   575,   576,  1517,   452,   585,   184,
     598,   202,  1522,   269,   207,   473,  1200,   164,   688,   689,
     486,   600,   601,  1520,   602,   203,   603,  1533,   377,   896,
     204,   452,   577,   582,    71,  1508,   750,   620,   207,  1531,
     205,   658,   611,   612,   657,   618,   618,   638,   644,  1137,
     207,   716,   731,  1518,   604,   655,  1528,   656,  1529,  1128,
    1243,   207,   619,   619,   639,   645,  1550,   111,    68,    69,
    1530,    70,  1547,   272,  1548,   453,   128,  1507,  1549,   571,
     222,   401,   574,   743,   681,   402,   584,   734,   404,   500,
     501,  1318,   402,   207,   270,   948,   949,   950,   471,   683,
     270,   723,    68,    69,   100,    70,   223,  1283,    68,    69,
    1502,    70,   759,   406,  1284,   609,   920,   402,   270,   696,
     922,   511,   512,   513,   233,   124,   125,   578,  1401,   207,
     695,   697,   698,   726,   187,   848,   235,   188,   189,   190,
     191,   261,   192,   193,   194,   710,   711,   579,   580,   699,
     276,   369,   277,   887,   888,   767,   730,   732,   514,   515,
     516,   460,    71,   278,   722,    68,    69,   849,    70,  1541,
     728,   281,   786,   733,   282,   187,   322,   199,   188,   189,
     190,   191,   762,   192,   193,   194,  1525,   749,   889,   377,
      68,    69,   294,    70,   121,   758,    71,   121,    68,    69,
     700,    70,    71,  1064,  1146,   207,   128,   400,   761,  1065,
      68,    69,   295,    70,  1349,  1350,  1351,   764,   408,  1404,
    1405,   534,   402,   154,   155,   156,  1070,  1071,   910,   911,
     915,   536,   539,   296,   100,   251,   252,   787,   788,   297,
     796,   414,   918,   791,  1399,   792,   798,    68,   924,   417,
      70,   260,    68,    69,   795,   853,   419,   372,   410,    71,
     128,   506,   402,   451,   980,   748,   981,   982,   983,   984,
     985,  1416,   434,   800,   457,   283,   284,   285,   283,   284,
     285,   902,   885,   461,    71,   894,   412,   895,   100,   460,
     402,   464,    71,   893,   876,   763,   283,   284,   285,    68,
      69,   217,    70,   121,    71,   238,   286,   128,   474,   907,
     900,   501,  1265,   914,  1268,   478,   973,   919,   921,   923,
     460,   960,  1069,  1214,   979,   963,  1320,  1321,   890,   891,
    1460,   892,   460,  1470,  1085,   100,  1471,  1464,  1086,  1087,
     885,    71,   481,  1088,  1215,   951,    71,   954,   485,   956,
    1216,   957,  1462,   154,   155,   156,   487,  1217,    97,  1070,
    1071,    98,   157,  1093,   496,   967,  1095,  1094,    68,    69,
    1096,    70,   701,  1097,   497,   969,   970,  1098,   664,  1322,
    1323,    99,     3,   702,    99,   100,   101,   507,   102,   101,
    1099,   102,  -239,    71,  1100,   103,   207,   986,   103,   987,
     988,   195,  1000,   971,  1002,  1072,   460,   154,   155,   156,
    1195,   581,   104,   998,  1084,   104,   952,   196,   372,  1101,
     550,   119,  1103,  1102,  1545,  1105,  1104,   105,  1136,  1106,
     105,   510,  1107,   283,   284,   285,  1108,  1074,   124,   125,
     578,   460,  1336,   966,   556,   283,   284,   285,   625,  1109,
     207,  1407,   968,  1110,   154,   155,   156,   544,   196,  1111,
     579,   580,    71,  1112,   607,  1552,   545,   975,   287,  1073,
     288,   287,  1113,   288,   289,   290,  1114,   289,   290,   597,
    1184,   599,  1186,   468,   469,   121,  1553,   578,  1170,   287,
    1115,   288,   157,  1123,  1116,   289,   290,  1578,  1121,   608,
     224,  1124,   225,   226,   227,  1143,   610,   579,   580,  1119,
    1141,  1147,  1590,  1139,  1120,  1004,  1005,  1006,  1007,  1008,
    1142,   653,    68,    69,  1133,    70,   751,   752,   753,  1587,
     885,  1209,  1210,  1211,  1212,  1213,   531,  1155,  1164,   541,
    1144,  1117,  1145,   540,  1066,  1118,   157,   532,  1156,  1165,
     654,   638,   542,  1159,  1168,  1248,  1250,   217,   885,  1249,
    1251,    14,   662,  1252,   533,  1157,  1166,  1253,   639,   667,
    1206,   460,  1546,   626,   668,   614,   627,  1194,   615,   616,
     560,   934,   561,   562,   563,  1254,   674,  1130,   935,  1255,
     936,   937,   938,   157,  1197,  1198,   764,   764,   647,   648,
     649,  1311,  1312,  1313,  1314,   207,   460,  1018,  1019,  1457,
    1576,  1315,  1316,  1317,   669,   670,    71,   677,   241,   242,
     243,  1204,   626,  1236,   682,   693,   287,   690,   288,   939,
     940,   941,   289,   290,   727,   650,   651,   652,   287,   106,
     288,   694,   706,   244,   687,   290,    28,    29,    30,    31,
      32,    33,    34,   735,   707,   628,   256,   257,   258,   259,
     717,    35,  1089,  1090,  1091,  1092,   718,   719,  1308,  1309,
    1344,  1345,  1346,  1347,   763,   763,   942,   943,   944,   721,
     945,   736,   754,   946,  1221,   755,    28,    29,    30,    31,
      32,    33,    34,   756,   757,  1354,   760,   769,  1524,  1527,
     777,    35,   766,   768,   770,  1285,  1245,  1246,  1247,   780,
     771,   772,   773,  1256,  1257,  1258,  1259,  1260,  1261,   774,
    1263,  1264,  1266,   776,  1269,  1270,  1271,  1272,  1273,  1274,
    1275,   781,  1277,   789,  1279,   790,  1282,   245,  1286,   246,
     247,   248,   249,   111,   793,  1262,   797,   799,  1305,  1267,
     851,   850,   855,   111,   111,   111,   111,  1276,   854,   934,
     856,   857,   858,   111,   111,   111,   935,  1119,   936,   937,
     938,  1339,  1120,   859,   883,   884,   897,   587,   129,   588,
     898,   899,   130,   131,   132,   133,   134,   903,   135,   136,
     137,   138,  1319,   139,   140,   904,   909,   141,   142,   143,
     144,  1119,   925,   100,   145,   146,  1120,   939,   940,   941,
     931,  1331,     3,   147,   972,   148,   955,   964,   959,   965,
    1333,   976,   977,   974,  1406,  1011,   990,  1337,  1338,   978,
     149,   150,   151,   402,   989,   991,  1012,   992,   947,  1341,
     993,   994,   995,  1410,   996,   997,  1013,  1003,  1009,  1415,
       3,  1014,  1017,  1062,   942,   943,   944,  1067,   945,  1348,
    1352,   946,  1078,  1121,  1079,  1080,   152,  1125,  1360,  1361,
    1362,  1363,  1364,  1365,  1131,  1367,  1400,  1140,  1172,  1326,
    1173,  1174,  1181,  1179,  1175,  1176,  1329,  1177,  1330,  1178,
    1182,  1183,  1420,  1187,   737,  1402,  1190,  1191,  1192,  1193,
    1366,  1201,  1203,  1205,  1239,    68,    69,  1207,    70,  1208,
    1240,  1241,  1292,   128,  1293,  1294,   129,   475,  1295,  1296,
     130,   131,   132,   133,   134,  1299,   135,   136,   137,   138,
    1461,   139,   140,   885,  1297,   141,   142,   143,   144,  1300,
    1302,   100,   145,   146,    28,    29,    30,    31,    32,    33,
      34,   147,  1301,   148,  1306,  1419,  1310,  1307,  1327,    35,
     517,  1328,  1424,  1332,  1150,  1151,  1152,  1153,   149,   150,
     151,  1335,  1334,  1085,  1121,  1340,  1087,  1093,    28,    29,
      30,    31,    32,    33,    34,  1499,  1095,  1430,    11,    12,
      13,    14,  1097,    35,  1099,    68,    69,  1463,    70,    71,
    1101,  1103,  1105,  1107,   152,  1109,   283,   284,   285,  1111,
    1355,  1356,  1357,  1358,   885,  1403,  1171,  1368,  1359,  1370,
     468,   469,  1369,  1372,  1371,     3,  1503,  1374,  1373,    14,
    1375,  1376,  1377,  1378,  1379,  1380,  1382,  1381,  1384,   641,
    1386,  1383,   642,  1385,   934,  1396,  1474,  1475,  1476,  1387,
    1388,   935,  1389,   936,   937,   938,  1390,  1391,  1394,  1392,
     154,   155,   156,   428,  1393,   429,   430,  1395,  1397,  1398,
     158,  1408,   431,  1409,  1417,  1412,    28,    29,    30,    31,
      32,    33,    34,  1540,  1413,  1154,  1414,  1421,  1248,    71,
    1422,    35,   939,   940,   941,  1250,  1423,  1252,  1510,  1254,
    1455,  1425,  1572,  1516,  1510,  1519,  1585,  1523,  1429,  1516,
    1510,  1519,  1426,  1583,    28,    29,    30,    31,    32,    33,
      34,  1427,  1428,   643,  1431,  1434,  1432,  1436,  1438,    35,
    1468,  1516,  1510,  1519,  1526,  1433,  1435,  1437,  1439,   942,
     943,   944,  1441,   945,  1440,  1442,   946,  1443,   885,  1444,
      68,    69,  1445,    70,  1446,  1447,  1448,  1449,   128,  1450,
    1451,   129,  1452,  1456,  1453,   130,   131,   132,   133,   134,
    1584,   135,   136,   137,   138,  1454,   139,   140,  1458,  1459,
     141,   142,   143,   144,     9,    10,   100,   145,   146,  1465,
    1469,   885,  1473,  1479,  1480,  1579,   147,  1481,   148,   470,
    1482,   288,  1497,  1500,    14,   289,   290,   881,   158,    68,
      69,  1501,    70,   149,   150,   151,   613,   128,   614,  1504,
     129,   615,   616,  1505,   130,   131,   132,   133,   134,  1506,
     135,   136,   137,   138,  1542,   139,   140,  1543,  1551,   141,
     142,   143,   144,  1571,    71,   100,   145,   146,  1573,   152,
    1574,   283,   284,   285,  1575,   147,    99,   148,  1577,  1580,
    1581,   101,  1582,   102,  1586,   468,   469,  1589,   298,  1591,
     103,   508,   149,   150,   151,   161,   905,   671,   596,  1325,
     926,  1303,  1304,   163,  1188,  1324,   546,   104,   916,    28,
      29,    30,    31,    32,    33,    34,   775,   744,   617,   198,
    1237,  1411,   105,    71,    35,   154,   155,   156,   152,  1063,
     283,   284,   285,  1472,    68,    69,  1129,    70,     3,   882,
    1189,  1291,   128,   901,  1081,   129,  1077,    95,  1298,   130,
     131,   132,   133,   134,   917,   135,   136,   137,   138,   240,
     139,   140,   679,   630,   141,   142,   143,   144,   712,   930,
     100,   145,   146,     0,     0,     0,     0,     0,     0,     0,
     147,     0,   148,     0,   154,   155,   156,     0,     0,     0,
       0,     0,     0,    68,    69,     0,    70,   149,   150,   151,
       0,   128,     0,     0,   129,     0,     0,     0,   130,   131,
     132,   133,   134,     0,   135,   136,   137,   138,     0,   139,
     140,     0,     0,   141,   142,   143,   144,     0,    71,   100,
     145,   146,     0,   152,   153,     0,     0,     0,     0,   147,
       0,   148,     0,     0,     0,     0,     0,     0,    97,     0,
       0,    98,     0,     0,     0,     0,   149,   150,   151,     0,
     912,     0,     0,     0,   470,     0,   288,     0,     0,     0,
     687,   290,     0,   158,    99,   100,     0,     0,     0,   101,
       0,   102,     0,     0,     0,     0,     0,    71,   103,   154,
     155,   156,   152,  1160,     0,  1161,  1162,     0,     0,     0,
       0,     0,     0,     0,     0,   104,   913,     0,     0,     0,
       0,     0,     0,     0,    11,    12,    13,    14,     0,     0,
     105,     0,     0,   470,     0,   288,    14,     0,     0,   289,
     290,     0,   158,     0,     0,     0,    68,    69,     0,    70,
       0,     0,     0,     0,   128,     0,     0,   129,   154,   155,
     156,   130,   131,   132,   133,   134,     0,   135,   136,   137,
     138,     0,   139,   140,     0,     0,   141,   142,   143,   144,
       0,     0,   100,   145,   146,     0,     0,     0,     0,     0,
       0,     0,   147,     0,   148,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,   149,
     150,   151,    28,    29,    30,    31,    32,    33,    34,     0,
       0,  1163,     0,     0,     0,     0,     0,    35,     0,     0,
       3,     0,     0,     0,     0,     0,     0,     0,   157,     0,
      71,     0,     0,     0,     0,   152,   153,   158,    68,    69,
       0,    70,     0,   633,     0,     0,   128,     0,     0,   129,
       0,     0,     0,   130,   131,   132,   133,   134,     0,   135,
     136,   137,   138,     0,   139,   140,     0,     0,   141,   142,
     143,   144,     0,     0,   100,   145,   146,     0,     0,     0,
       0,     0,     0,     0,   147,     0,   148,   211,     0,     0,
       0,   154,   155,   156,     0,     0,   158,    68,    69,     0,
      70,   149,   150,   151,     0,   128,     0,     0,   129,     0,
       0,     0,   130,   131,   132,   133,   134,     0,   135,   136,
     137,   138,     0,   139,   140,     0,     0,   141,   142,   143,
     144,     0,    71,   100,   145,   146,     0,   152,     0,     0,
       0,     0,     0,   147,     0,   148,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   634,     0,     0,
     149,   150,   151,   129,     0,     0,     0,   130,   131,   132,
     133,   134,     0,   135,   136,   137,   138,     0,   139,   140,
       0,     0,   141,   142,   143,   144,     0,     0,     0,   145,
     146,    71,     0,   154,   155,   156,   554,     0,   147,    14,
     148,     2,     0,     0,     0,     0,     0,     0,     0,   635,
      68,    69,   636,    70,     0,   149,   150,   151,   128,     3,
       0,   129,     0,     0,     0,   130,   131,   132,   133,   134,
     211,   135,   136,   137,   138,     0,   139,   140,     0,   158,
     141,   142,   143,   144,     0,     0,   100,   145,   146,     0,
       0,   152,   154,   155,   156,     0,   663,     0,   148,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   149,   150,   151,     0,     0,     0,     0,
       0,     0,     0,     0,    28,    29,    30,    31,    32,    33,
      34,     0,     0,   637,     0,     0,     0,     0,     0,    35,
       0,     0,     0,     0,    71,     0,     0,    68,    69,   152,
      70,     0,     0,     0,     0,   128,     0,     0,   129,     0,
       0,     0,   130,   131,   132,   133,   134,     0,   135,   136,
     137,   138,   211,   139,   140,     0,     0,   141,   142,   143,
     144,   158,     0,   100,   145,   146,     0,     0,     0,     0,
       0,     0,     0,   147,     0,   148,     4,     5,     6,     7,
       8,     0,     0,     0,     0,   154,   155,   156,     0,     0,
     149,   150,   151,     0,     0,     0,     0,     0,     9,    10,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,   211,     0,     0,     0,    11,    12,    13,    14,     0,
     158,    71,    15,    16,     0,     0,   715,     0,    17,     0,
       0,    18,     0,     0,     0,     0,     0,     0,    19,    20,
       0,     0,     0,     0,     0,     0,    68,     0,     0,    70,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     3,
       0,     0,     0,     0,     0,     0,   725,     0,     0,     0,
       0,     0,     0,     0,     0,   158,     0,     0,     0,     0,
       0,     0,   154,   155,   156,     0,     0,     0,     0,     0,
       0,     0,     0,     0,    21,    22,     0,    23,    24,    25,
       0,    26,    27,    28,    29,    30,    31,    32,    33,    34,
       0,     0,     0,     0,   266,   129,   267,     0,    35,   130,
     131,   132,   133,   134,   211,   135,   136,   137,   138,     0,
     139,   140,     0,   158,   141,   142,   143,   144,     0,     0,
      71,   145,   146,     0,     0,    68,     0,     0,    70,     0,
     147,     0,   148,     0,     0,     0,     0,     0,     3,     0,
       0,     0,     0,     0,     0,     0,     0,   149,   150,   151,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   346,   347,   348,   349,   350,   351,   352,
     353,   354,   355,   356,   357,   358,     0,     0,     0,     0,
       8,     0,     0,   152,   359,   360,   361,   362,   363,   364,
       3,     0,     0,     0,     0,     0,     0,     0,     9,    10,
       0,   211,     0,     0,     0,     0,     0,     0,     0,     0,
     158,     0,     0,   633,     0,    11,    12,    13,    14,    71,
       0,     0,     0,     0,     0,     0,   365,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   860,   861,   862,
     366,   863,   864,   865,   866,     0,   867,   868,   194,     0,
     869,   870,   871,   872,     0,     0,     0,   873,   874,     0,
       0,     0,   346,   347,   348,   349,   350,   351,   352,   353,
     354,   355,   356,   357,   358,   367,   368,     0,     0,     8,
       0,     0,     0,   359,   360,   361,   362,   363,   364,    68,
       0,     0,    70,     0,     0,     0,     0,     9,    10,     0,
       0,     0,     3,    28,    29,    30,    31,    32,    33,    34,
       0,   369,   370,     0,    11,    12,    13,    14,    35,     0,
       0,     0,     0,     0,     0,   365,   875,   634,     0,     0,
       0,     0,     0,     0,     0,     0,   129,     0,     0,   366,
     130,   131,   132,   133,   134,     0,   135,   136,   137,   138,
       0,   139,   140,     0,     0,   141,   142,   143,   144,     0,
       0,     0,   145,   146,     0,     0,     0,     0,     0,    14,
       0,   147,     0,   148,   367,   368,     0,     0,     0,   635,
       0,     0,   636,    71,     0,     0,     0,   158,   149,   150,
     151,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,    28,    29,    30,    31,    32,    33,    34,     0,
     369,   742,     0,     0,     0,     0,     0,    35,     0,     0,
       0,     0,     0,     0,   152,     0,   346,   347,   348,   349,
     350,   351,   352,   353,   354,   355,   356,   357,   358,     0,
       0,     0,     0,     8,     0,     0,     0,   359,   360,   361,
     362,   363,   364,     0,    28,    29,    30,    31,    32,    33,
      34,     9,    10,  1169,     0,     0,     0,     0,     0,    35,
       0,     0,     0,     0,     0,     0,     0,     0,    11,    12,
      13,    14,     0,     0,     0,     0,     0,     0,     0,   365,
       0,     0,     0,     0,   129,     0,     0,     0,   130,   131,
     132,   133,   134,   366,   135,   136,   137,   138,     0,   139,
     140,  1532,     0,   141,   142,   143,   144,   435,     0,     0,
     145,   146,     0,     0,     0,     0,     0,     0,     0,   147,
       0,   148,     0,     0,     0,     0,     0,     0,   367,   368,
       0,     0,     0,     3,     0,     0,   149,   150,   151,     0,
     436,     0,   437,   438,   439,   440,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,    28,    29,    30,    31,
      32,    33,    34,     0,   369,   929,     0,     0,     0,     0,
       0,    35,   152,     0,     0,     0,     0,     0,     0,     0,
     441,   442,   443,   444,     0,     0,   445,     0,     0,     0,
     446,   447,   448,     0,     0,   129,     0,     0,     0,   130,
     131,   132,   133,   134,     0,   135,   136,   137,   138,   881,
     139,   140,     0,     0,   141,   142,   143,   144,   158,     0,
       0,   145,   146,     0,     0,     0,     0,     0,     0,     0,
     147,     0,   148,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   149,   150,   151,
       0,     0,  1020,  1021,     0,  1022,  1023,  1024,  1025,  1026,
    1027,     0,  1028,  1029,     0,  1030,  1031,  1032,  1033,  1034,
       4,     5,     6,     7,     8,     0,     0,     3,     0,     0,
       0,     0,     0,   152,     0,     0,     0,     0,     0,     0,
       0,     0,     9,    10,   449,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,    11,
      12,    13,    14,     0,     0,     0,    15,    16,     0,     0,
       0,     0,    17,     0,     0,    18,   521,     0,     0,     0,
       0,     0,    19,    20,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,   860,   861,   862,     0,   863,
     864,   865,   866,   217,   867,   868,   194,     0,   869,   870,
     871,   872,     0,     0,     0,   873,   874,     0,  1222,  1223,
    1224,     0,  1225,  1226,  1227,  1228,   158,  1229,  1230,   194,
       0,  1231,  1232,  1233,  1234,     0,     0,     0,    21,    22,
    1235,    23,    24,    25,     0,    26,    27,    28,    29,    30,
      31,    32,    33,    34,     0,     0,   509,     0,     0,     0,
       0,     0,    35,     0,   522,     0,     6,     7,     8,     0,
       0,     3,     0,     0,     0,     0,     0,     0,   523,     0,
       0,     0,     0,   524,   875,     0,     9,    10,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,    11,    12,    13,    14,     0,   525,   526,
       0,  1035,  1036,     0,  1037,  1038,  1039,     0,  1040,  1041,
     521,     0,  1042,  1043,     0,  1044,     0,     0,   527,     0,
       0,     0,     0,     0,     0,     0,     0,   158,  1045,  1046,
    1047,  1048,  1049,  1050,  1051,  1052,  1053,  1054,  1055,  1056,
    1057,  1058,  1059,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   528,   529,    79,    80,    81,    82,    83,
      84,    85,    86,    87,    88,    89,    90,    91,    92,    93,
       0,     0,     0,     0,     0,     0,  1060,     0,     0,     0,
       0,    28,    29,    30,    31,    32,    33,    34,     0,     0,
     530,     0,     0,     0,     0,     0,    35,     0,   522,     0,
       6,     7,     8,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   523,  1554,     0,     0,     0,   524,     0,     0,
       9,    10,     0,     0,     0,     0,     0,     0,  1555,     0,
       0,     0,     0,     0,     0,     0,     0,    11,    12,    13,
      14,     0,   525,   526,  1556,    28,    29,    30,    31,    32,
      33,    34,     0,  1557,     0,     0,     0,     0,     0,     0,
      35,     0,   527,     0,     0,     0,     0,  1558,  1559,  1560,
    1561,   801,     0,  1562,     0,     0,   802,   803,     0,   804,
     805,   806,   807,   808,   809,     0,   810,   811,     0,   812,
     813,   814,   815,   816,     0,     0,     0,   528,   529,     0,
    1563,  1564,  1565,  1566,  1567,  1568,  1569,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,    28,    29,    30,    31,    32,
      33,    34,     0,     0,  1149,   817,     0,   818,     0,     0,
      35,     0,   819,    68,    69,   801,    70,     0,     0,     0,
     802,   803,     0,   804,   805,   806,   807,   808,   809,   820,
     810,   811,     0,   812,   813,   814,   815,   816,   129,     0,
       0,     0,     0,     0,   132,   133,   134,   299,   135,   136,
     137,   138,     0,   139,   140,     0,     0,   141,   142,   143,
     144,     0,   821,     0,  1287,   146,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,   300,     0,     0,   817,
     301,   818,     0,   302,   303,     0,   819,     0,   304,   305,
     306,   307,   308,   309,   310,   311,   312,   313,   314,   315,
       0,     0,     0,   820,     0,     0,   316,    71,     0,     0,
     317,     0,     0,  1288,     0,     0,     0,   318,     0,     0,
       0,     0,     0,     0,     0,     0,   319,     0,     0,     0,
       0,     0,     0,  1289,  1570,     0,   821,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   822,     0,   823,   824,   825,   826,   827,   828,
     829,   830,   831,   832,   833,   834,   835,   836,   837,   838,
     839,   860,   861,   862,   840,   863,   864,   865,   866,     0,
     867,   868,   194,   841,   869,   870,   871,   872,     0,     0,
       0,   873,   874,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,   842,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,   822,     0,   823,   824,
     825,   826,   827,   828,   829,   830,   831,   832,   833,   834,
     835,   836,   837,   838,   839,     0,   325,    99,   840,     0,
       0,     0,   101,     0,   102,     0,     0,   841,     0,     0,
     875,   103,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,   104,   326,
       0,   327,   328,   329,   330,   331,     0,     0,     0,     0,
     332,     0,     0,   105,     0,     0,     0,     0,     0,   333,
       0,     0,     0,     0,   334,     0,     0,   335,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,   336,
     337,   338,   339,   340,   341,   342,   343,     0,     0,     0,
       0,     0,   344
};

static const yytype_int16 yycheck[] =
{
       5,     1,    25,    25,     1,   164,    94,    14,     1,   324,
     677,   410,     1,   412,    25,    20,    57,     1,    25,   324,
     324,   959,   990,    28,    29,    30,    31,   104,   105,     1,
     662,    21,    22,  1085,   723,   472,     9,  1113,    25,     5,
       6,    95,     8,     9,    12,   186,     7,    43,     7,   736,
       7,     8,    48,    20,     9,     9,   485,   486,   487,    23,
      24,     5,     6,    59,     8,    20,     5,     6,   735,     8,
      60,    19,    33,     9,    13,   189,   190,   191,   192,   193,
       5,     6,    63,     8,    74,     7,   109,   164,     5,     6,
       9,     8,     7,     9,   182,     5,     6,   111,     8,   106,
       8,   111,    41,    13,     8,     7,  1080,   130,   185,     9,
     115,   137,   117,   118,   119,     9,   170,    11,    63,   106,
     167,     8,   216,   113,   114,   147,     9,    10,   150,    56,
     153,    41,   378,    99,    44,   137,   213,    31,    32,   137,
      44,    51,     9,    10,  1462,   228,   234,   169,     5,     6,
     157,     8,   157,   158,   195,    99,     9,   164,   198,     9,
      99,     9,   106,   107,   108,   248,    78,     9,   158,     9,
      10,     8,   299,     9,    99,     9,   181,     9,    20,   233,
     268,     9,    99,    64,     5,     6,   209,     8,     9,    99,
       5,     6,   292,     8,   293,   294,   883,   884,   294,    20,
     300,   206,   207,   199,   211,   301,   211,    44,     5,     6,
     897,     8,     5,     6,  1450,     8,     5,     6,     8,     8,
      41,   160,   161,   162,     5,     6,    41,     8,   657,   234,
     389,   293,   222,   223,   322,   240,    65,    66,   300,   261,
     381,   298,    99,     9,    41,   126,  1482,   294,   290,   299,
     160,   161,   162,   295,   683,   295,     5,   280,   925,     8,
       9,     7,   216,   268,   269,   288,   292,  1241,   273,  1587,
     292,   276,   277,   278,   186,   289,   281,   282,    99,   289,
     368,   286,   216,   273,    99,   299,   953,    33,   975,     9,
      10,    11,   294,   539,    24,    25,   294,   287,   298,    11,
     275,   298,    99,   391,    34,   298,    99,     9,    10,   298,
      99,    31,    32,   294,   298,  1442,   393,     9,    99,    31,
      32,  1448,  1447,   291,   324,   295,   298,   324,   298,   296,
       5,   324,   305,     8,     9,   324,   293,   298,   392,   298,
     324,    87,   308,   290,  1449,   300,   294,   304,     7,   294,
      99,   392,   324,  1480,  1479,   299,   292,   301,    12,   307,
     299,   305,   306,   290,   300,   794,  1334,   390,   290,   359,
     458,   361,   290,   295,   299,   365,  1481,   296,   295,   294,
     296,  1457,   389,   390,    60,   293,   296,   394,   290,   299,
     293,   381,   296,   295,   384,   385,   296,   218,   388,   290,
     396,   290,   296,   218,   294,   493,   293,   394,   496,   497,
     300,   401,   402,   296,   404,   290,   406,  1493,   559,   848,
     290,   218,   386,   387,    99,  1477,   567,   424,   294,   296,
     290,   454,   422,   423,   300,   424,   425,   426,   427,   293,
     294,   529,   299,   296,   408,   450,   296,   452,   296,   886,
    1082,   294,   424,   425,   426,   427,   296,   464,     5,     6,
     296,     8,   296,   470,   296,   470,    13,   296,   296,   381,
     295,   295,   384,   561,   479,   299,   388,   554,   295,   300,
     470,  1170,   299,   294,   305,   251,   252,   253,   493,   300,
     305,   545,     5,     6,    41,     8,   295,    44,     5,     6,
    1468,     8,   590,   295,    51,   417,   299,   299,   305,    39,
     299,    75,    76,    77,   298,     9,    10,    11,   299,   294,
     510,    51,    52,   546,    23,   300,   295,    26,    27,    28,
      29,    51,    31,    32,    33,   525,   526,    31,    32,    69,
     137,   290,   137,    28,    29,   599,   551,   552,   112,   113,
     114,   292,    99,   137,   544,     5,     6,   298,     8,  1497,
     550,   137,   616,   553,   307,    23,    61,   608,    26,    27,
      28,    29,   595,    31,    32,    33,   296,   567,    63,   720,
       5,     6,   296,     8,     9,   590,    99,     9,     5,     6,
     120,     8,    99,   299,   293,   294,    13,   305,   594,   305,
       5,     6,    12,     8,    26,    27,    28,   597,   295,  1296,
    1297,   926,   299,   160,   161,   162,    65,    66,   706,   707,
     708,   926,   926,   296,    41,    75,    76,   623,   624,   296,
     635,   295,   709,   629,    51,   631,   641,     5,   715,   295,
       8,   663,     5,     6,   634,     8,   296,   559,   295,    99,
      13,   674,   299,   298,   768,   567,   770,   771,   772,   773,
     774,  1328,   300,   653,   295,   106,   107,   108,   106,   107,
     108,   694,   677,     8,    99,   680,   295,   682,    41,   292,
     299,   296,    99,   679,   673,   597,   106,   107,   108,     5,
       6,     7,     8,     9,    99,   208,   137,    13,   300,   704,
     690,   691,  1101,   708,  1103,   221,   760,   712,   713,   714,
     292,   734,    28,   272,   296,   738,   139,   140,   203,   204,
    1407,   206,   292,   293,   295,    41,   296,  1414,   299,   295,
     735,    99,    25,   299,   293,   725,    99,   727,   300,   729,
     299,   731,  1409,   160,   161,   162,   300,   306,    14,    65,
      66,    17,   299,   295,   295,   745,   295,   299,     5,     6,
     299,     8,   292,   295,   295,   755,   756,   299,   856,   139,
     140,    40,    18,   303,    40,    41,    45,   291,    47,    45,
     295,    47,   290,    99,   299,    54,   294,   777,    54,   779,
     780,   290,   797,   757,   799,   849,   292,   160,   161,   162,
     296,   295,    71,   793,   858,    71,   226,   306,   720,   295,
     299,   216,   295,   299,  1501,   295,   299,    86,   906,   299,
      86,   295,   295,   106,   107,   108,   299,   849,     9,    10,
      11,   292,   293,   745,   304,   106,   107,   108,    84,   295,
     294,   295,   754,   299,   160,   161,   162,   295,   306,   295,
      31,    32,    99,   299,   291,  1542,   295,   295,   299,   849,
     301,   299,   295,   301,   305,   306,   299,   305,   306,   295,
     958,   295,   960,   120,   121,     9,  1543,    11,   932,   299,
     295,   301,   299,   879,   299,   305,   306,  1574,   877,   293,
      46,   881,    48,    49,    50,   918,   296,    31,    32,   105,
     911,   924,  1589,   908,   110,    25,    26,    27,    28,    29,
     915,   295,     5,     6,   904,     8,   568,   569,   570,  1586,
     925,    25,    26,    27,    28,    29,   926,   927,   928,   926,
     920,   295,   922,   926,   846,   299,   299,   926,   927,   928,
     295,   930,   926,   927,   928,   295,   295,     7,   953,   299,
     299,   197,   290,   295,   926,   927,   928,   299,   930,     7,
    1014,   292,   293,   209,   293,   211,   212,   972,   214,   215,
      55,    39,    57,    58,    59,   295,   301,   889,    46,   299,
      48,    49,    50,   299,   976,   977,   976,   977,    75,    76,
      77,  1150,  1151,  1152,  1153,   294,   292,   835,   836,   298,
     296,  1160,  1161,  1162,   296,   296,    99,    19,   101,   102,
     103,  1001,   209,  1067,    19,   302,   299,    20,   301,    87,
      88,    89,   305,   306,   304,   112,   113,   114,   299,   295,
     301,   293,   293,   126,   305,   306,   282,   283,   284,   285,
     286,   287,   288,    19,   111,   291,    26,    27,    28,    29,
     290,   297,    26,    27,    28,    29,   290,   290,  1146,  1147,
      26,    27,    28,    29,   976,   977,   134,   135,   136,   290,
     138,   295,    62,   141,  1064,    62,   282,   283,   284,   285,
     286,   287,   288,   295,   295,   291,   295,   300,  1487,  1488,
     304,   297,   296,   296,   296,  1117,  1086,  1087,  1088,   213,
     296,   296,   296,  1093,  1094,  1095,  1096,  1097,  1098,   296,
    1100,  1101,  1102,   296,  1104,  1105,  1106,  1107,  1108,  1109,
    1110,   298,  1112,   298,  1114,   298,  1116,   220,  1118,   222,
     223,   224,   225,  1140,   295,  1099,   216,   216,  1143,  1103,
     293,   296,   291,  1150,  1151,  1152,  1153,  1111,   298,    39,
     293,     8,   298,  1160,  1161,  1162,    46,   105,    48,    49,
      50,  1215,   110,   291,   295,   295,   295,    15,    16,    17,
     296,   296,    20,    21,    22,    23,    24,   296,    26,    27,
      28,    29,  1172,    31,    32,   295,   302,    35,    36,    37,
      38,   105,    19,    41,    42,    43,   110,    87,    88,    89,
     296,  1191,    18,    51,    19,    53,   300,   296,   300,   293,
    1200,   293,   293,   296,  1302,   243,   304,  1207,  1208,   296,
      68,    69,    70,   299,   295,   295,   235,   296,   296,  1219,
     296,   295,   295,  1310,   296,   296,   247,   296,   295,  1327,
      18,   295,    22,   296,   134,   135,   136,   298,   138,  1239,
    1240,   141,   291,  1242,   298,   290,   104,   302,  1248,  1249,
    1250,  1251,  1252,  1253,   296,  1255,  1288,   197,   295,  1181,
     304,   304,   137,   295,   300,   300,  1188,   300,  1190,   300,
     300,   300,  1336,   296,    20,  1290,    62,    62,   296,   296,
    1254,     8,   296,   250,   295,     5,     6,   299,     8,   299,
     295,   290,   296,    13,   300,   296,    16,   167,   296,   295,
      20,    21,    22,    23,    24,   296,    26,    27,    28,    29,
    1408,    31,    32,  1328,   295,    35,    36,    37,    38,   296,
     293,    41,    42,    43,   282,   283,   284,   285,   286,   287,
     288,    51,   296,    53,   300,  1335,   256,   300,   293,   297,
     301,    19,  1342,   296,   170,   171,   172,   173,    68,    69,
      70,   298,   304,   295,  1353,   300,   295,   295,   282,   283,
     284,   285,   286,   287,   288,  1463,   295,   291,   194,   195,
     196,   197,   295,   297,   295,     5,     6,  1410,     8,    99,
     295,   295,   295,   295,   104,   295,   106,   107,   108,   295,
     291,   296,   300,   296,  1409,   302,   296,   296,   300,   296,
     120,   121,   300,   296,   300,    18,  1470,   296,   300,   197,
     300,   296,   296,   300,   296,   296,   296,   300,   296,   207,
     296,   300,   210,   300,    39,     8,  1426,  1427,  1428,   300,
     296,    46,   300,    48,    49,    50,   296,   300,   300,   296,
     160,   161,   162,    73,   296,    75,    76,   296,   296,   300,
     308,   293,    82,    19,     8,   296,   282,   283,   284,   285,
     286,   287,   288,  1495,   296,   291,   295,   300,   295,    99,
     300,   297,    87,    88,    89,   295,   300,   295,  1478,   295,
       8,   296,  1546,  1483,  1484,  1485,  1584,  1487,   296,  1489,
    1490,  1491,   304,  1580,   282,   283,   284,   285,   286,   287,
     288,   304,   304,   291,   295,   300,   295,   300,   300,   297,
     304,  1511,  1512,  1513,  1488,   296,   296,   296,   296,   134,
     135,   136,   295,   138,   300,   295,   141,   295,  1543,   295,
       5,     6,   295,     8,   295,   295,   295,   295,    13,   295,
     295,    16,   295,   300,   296,    20,    21,    22,    23,    24,
    1583,    26,    27,    28,    29,   295,    31,    32,   296,   296,
      35,    36,    37,    38,   177,   178,    41,    42,    43,   301,
     296,  1586,   300,   295,   295,  1575,    51,   295,    53,   299,
     295,   301,   296,   296,   197,   305,   306,   299,   308,     5,
       6,   295,     8,    68,    69,    70,   209,    13,   211,   296,
      16,   214,   215,   296,    20,    21,    22,    23,    24,   296,
      26,    27,    28,    29,   295,    31,    32,    19,     8,    35,
      36,    37,    38,   296,    99,    41,    42,    43,   296,   104,
     295,   106,   107,   108,   295,    51,    40,    53,   256,   104,
     296,    45,   296,    47,    19,   120,   121,   295,   180,   296,
      54,   292,    68,    69,    70,    25,   703,   464,   394,  1180,
     717,  1137,  1139,    25,   963,  1179,   345,    71,    72,   282,
     283,   284,   285,   286,   287,   288,   608,   565,   291,    57,
    1067,   296,    86,    99,   297,   160,   161,   162,   104,   844,
     106,   107,   108,  1422,     5,     6,   886,     8,    18,   674,
     965,  1122,    13,   691,   856,    16,   851,    10,  1131,    20,
      21,    22,    23,    24,   118,    26,    27,    28,    29,   116,
      31,    32,   476,   425,    35,    36,    37,    38,   527,   721,
      41,    42,    43,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      51,    -1,    53,    -1,   160,   161,   162,    -1,    -1,    -1,
      -1,    -1,    -1,     5,     6,    -1,     8,    68,    69,    70,
      -1,    13,    -1,    -1,    16,    -1,    -1,    -1,    20,    21,
      22,    23,    24,    -1,    26,    27,    28,    29,    -1,    31,
      32,    -1,    -1,    35,    36,    37,    38,    -1,    99,    41,
      42,    43,    -1,   104,   105,    -1,    -1,    -1,    -1,    51,
      -1,    53,    -1,    -1,    -1,    -1,    -1,    -1,    14,    -1,
      -1,    17,    -1,    -1,    -1,    -1,    68,    69,    70,    -1,
      72,    -1,    -1,    -1,   299,    -1,   301,    -1,    -1,    -1,
     305,   306,    -1,   308,    40,    41,    -1,    -1,    -1,    45,
      -1,    47,    -1,    -1,    -1,    -1,    -1,    99,    54,   160,
     161,   162,   104,   173,    -1,   175,   176,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    71,   118,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   194,   195,   196,   197,    -1,    -1,
      86,    -1,    -1,   299,    -1,   301,   197,    -1,    -1,   305,
     306,    -1,   308,    -1,    -1,    -1,     5,     6,    -1,     8,
      -1,    -1,    -1,    -1,    13,    -1,    -1,    16,   160,   161,
     162,    20,    21,    22,    23,    24,    -1,    26,    27,    28,
      29,    -1,    31,    32,    -1,    -1,    35,    36,    37,    38,
      -1,    -1,    41,    42,    43,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    51,    -1,    53,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    68,
      69,    70,   282,   283,   284,   285,   286,   287,   288,    -1,
      -1,   291,    -1,    -1,    -1,    -1,    -1,   297,    -1,    -1,
      18,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   299,    -1,
      99,    -1,    -1,    -1,    -1,   104,   105,   308,     5,     6,
      -1,     8,    -1,    41,    -1,    -1,    13,    -1,    -1,    16,
      -1,    -1,    -1,    20,    21,    22,    23,    24,    -1,    26,
      27,    28,    29,    -1,    31,    32,    -1,    -1,    35,    36,
      37,    38,    -1,    -1,    41,    42,    43,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    51,    -1,    53,   299,    -1,    -1,
      -1,   160,   161,   162,    -1,    -1,   308,     5,     6,    -1,
       8,    68,    69,    70,    -1,    13,    -1,    -1,    16,    -1,
      -1,    -1,    20,    21,    22,    23,    24,    -1,    26,    27,
      28,    29,    -1,    31,    32,    -1,    -1,    35,    36,    37,
      38,    -1,    99,    41,    42,    43,    -1,   104,    -1,    -1,
      -1,    -1,    -1,    51,    -1,    53,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   155,    -1,    -1,
      68,    69,    70,    16,    -1,    -1,    -1,    20,    21,    22,
      23,    24,    -1,    26,    27,    28,    29,    -1,    31,    32,
      -1,    -1,    35,    36,    37,    38,    -1,    -1,    -1,    42,
      43,    99,    -1,   160,   161,   162,   104,    -1,    51,   197,
      53,     0,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   207,
       5,     6,   210,     8,    -1,    68,    69,    70,    13,    18,
      -1,    16,    -1,    -1,    -1,    20,    21,    22,    23,    24,
     299,    26,    27,    28,    29,    -1,    31,    32,    -1,   308,
      35,    36,    37,    38,    -1,    -1,    41,    42,    43,    -1,
      -1,   104,   160,   161,   162,    -1,    51,    -1,    53,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    68,    69,    70,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   282,   283,   284,   285,   286,   287,
     288,    -1,    -1,   291,    -1,    -1,    -1,    -1,    -1,   297,
      -1,    -1,    -1,    -1,    99,    -1,    -1,     5,     6,   104,
       8,    -1,    -1,    -1,    -1,    13,    -1,    -1,    16,    -1,
      -1,    -1,    20,    21,    22,    23,    24,    -1,    26,    27,
      28,    29,   299,    31,    32,    -1,    -1,    35,    36,    37,
      38,   308,    -1,    41,    42,    43,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    51,    -1,    53,   155,   156,   157,   158,
     159,    -1,    -1,    -1,    -1,   160,   161,   162,    -1,    -1,
      68,    69,    70,    -1,    -1,    -1,    -1,    -1,   177,   178,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   299,    -1,    -1,    -1,   194,   195,   196,   197,    -1,
     308,    99,   201,   202,    -1,    -1,   104,    -1,   207,    -1,
      -1,   210,    -1,    -1,    -1,    -1,    -1,    -1,   217,   218,
      -1,    -1,    -1,    -1,    -1,    -1,     5,    -1,    -1,     8,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    18,
      -1,    -1,    -1,    -1,    -1,    -1,   299,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   308,    -1,    -1,    -1,    -1,
      -1,    -1,   160,   161,   162,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   273,   274,    -1,   276,   277,   278,
      -1,   280,   281,   282,   283,   284,   285,   286,   287,   288,
      -1,    -1,    -1,    -1,    15,    16,    17,    -1,   297,    20,
      21,    22,    23,    24,   299,    26,    27,    28,    29,    -1,
      31,    32,    -1,   308,    35,    36,    37,    38,    -1,    -1,
      99,    42,    43,    -1,    -1,     5,    -1,    -1,     8,    -1,
      51,    -1,    53,    -1,    -1,    -1,    -1,    -1,    18,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    68,    69,    70,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   142,   143,   144,   145,   146,   147,   148,
     149,   150,   151,   152,   153,   154,    -1,    -1,    -1,    -1,
     159,    -1,    -1,   104,   163,   164,   165,   166,   167,   168,
      18,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   177,   178,
      -1,   299,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     308,    -1,    -1,    41,    -1,   194,   195,   196,   197,    99,
      -1,    -1,    -1,    -1,    -1,    -1,   205,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    22,    23,    24,
     219,    26,    27,    28,    29,    -1,    31,    32,    33,    -1,
      35,    36,    37,    38,    -1,    -1,    -1,    42,    43,    -1,
      -1,    -1,   142,   143,   144,   145,   146,   147,   148,   149,
     150,   151,   152,   153,   154,   254,   255,    -1,    -1,   159,
      -1,    -1,    -1,   163,   164,   165,   166,   167,   168,     5,
      -1,    -1,     8,    -1,    -1,    -1,    -1,   177,   178,    -1,
      -1,    -1,    18,   282,   283,   284,   285,   286,   287,   288,
      -1,   290,   291,    -1,   194,   195,   196,   197,   297,    -1,
      -1,    -1,    -1,    -1,    -1,   205,   111,   155,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    16,    -1,    -1,   219,
      20,    21,    22,    23,    24,    -1,    26,    27,    28,    29,
      -1,    31,    32,    -1,    -1,    35,    36,    37,    38,    -1,
      -1,    -1,    42,    43,    -1,    -1,    -1,    -1,    -1,   197,
      -1,    51,    -1,    53,   254,   255,    -1,    -1,    -1,   207,
      -1,    -1,   210,    99,    -1,    -1,    -1,   308,    68,    69,
      70,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   282,   283,   284,   285,   286,   287,   288,    -1,
     290,   291,    -1,    -1,    -1,    -1,    -1,   297,    -1,    -1,
      -1,    -1,    -1,    -1,   104,    -1,   142,   143,   144,   145,
     146,   147,   148,   149,   150,   151,   152,   153,   154,    -1,
      -1,    -1,    -1,   159,    -1,    -1,    -1,   163,   164,   165,
     166,   167,   168,    -1,   282,   283,   284,   285,   286,   287,
     288,   177,   178,   291,    -1,    -1,    -1,    -1,    -1,   297,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   194,   195,
     196,   197,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   205,
      -1,    -1,    -1,    -1,    16,    -1,    -1,    -1,    20,    21,
      22,    23,    24,   219,    26,    27,    28,    29,    -1,    31,
      32,   296,    -1,    35,    36,    37,    38,    39,    -1,    -1,
      42,    43,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    51,
      -1,    53,    -1,    -1,    -1,    -1,    -1,    -1,   254,   255,
      -1,    -1,    -1,    18,    -1,    -1,    68,    69,    70,    -1,
      72,    -1,    74,    75,    76,    77,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   282,   283,   284,   285,
     286,   287,   288,    -1,   290,   291,    -1,    -1,    -1,    -1,
      -1,   297,   104,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     112,   113,   114,   115,    -1,    -1,   118,    -1,    -1,    -1,
     122,   123,   124,    -1,    -1,    16,    -1,    -1,    -1,    20,
      21,    22,    23,    24,    -1,    26,    27,    28,    29,   299,
      31,    32,    -1,    -1,    35,    36,    37,    38,   308,    -1,
      -1,    42,    43,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      51,    -1,    53,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    68,    69,    70,
      -1,    -1,    21,    22,    -1,    24,    25,    26,    27,    28,
      29,    -1,    31,    32,    -1,    34,    35,    36,    37,    38,
     155,   156,   157,   158,   159,    -1,    -1,    18,    -1,    -1,
      -1,    -1,    -1,   104,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   177,   178,   226,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   194,
     195,   196,   197,    -1,    -1,    -1,   201,   202,    -1,    -1,
      -1,    -1,   207,    -1,    -1,   210,    67,    -1,    -1,    -1,
      -1,    -1,   217,   218,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    22,    23,    24,    -1,    26,
      27,    28,    29,     7,    31,    32,    33,    -1,    35,    36,
      37,    38,    -1,    -1,    -1,    42,    43,    -1,    22,    23,
      24,    -1,    26,    27,    28,    29,   308,    31,    32,    33,
      -1,    35,    36,    37,    38,    -1,    -1,    -1,   273,   274,
      44,   276,   277,   278,    -1,   280,   281,   282,   283,   284,
     285,   286,   287,   288,    -1,    -1,   291,    -1,    -1,    -1,
      -1,    -1,   297,    -1,   155,    -1,   157,   158,   159,    -1,
      -1,    18,    -1,    -1,    -1,    -1,    -1,    -1,   169,    -1,
      -1,    -1,    -1,   174,   111,    -1,   177,   178,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   194,   195,   196,   197,    -1,   199,   200,
      -1,   230,   231,    -1,   233,   234,   235,    -1,   237,   238,
      67,    -1,   241,   242,    -1,   244,    -1,    -1,   219,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   308,   257,   258,
     259,   260,   261,   262,   263,   264,   265,   266,   267,   268,
     269,   270,   271,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   254,   255,   179,   180,   181,   182,   183,
     184,   185,   186,   187,   188,   189,   190,   191,   192,   193,
      -1,    -1,    -1,    -1,    -1,    -1,   305,    -1,    -1,    -1,
      -1,   282,   283,   284,   285,   286,   287,   288,    -1,    -1,
     291,    -1,    -1,    -1,    -1,    -1,   297,    -1,   155,    -1,
     157,   158,   159,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   169,    39,    -1,    -1,    -1,   174,    -1,    -1,
     177,   178,    -1,    -1,    -1,    -1,    -1,    -1,    54,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   194,   195,   196,
     197,    -1,   199,   200,    70,   282,   283,   284,   285,   286,
     287,   288,    -1,    79,    -1,    -1,    -1,    -1,    -1,    -1,
     297,    -1,   219,    -1,    -1,    -1,    -1,    93,    94,    95,
      96,    16,    -1,    99,    -1,    -1,    21,    22,    -1,    24,
      25,    26,    27,    28,    29,    -1,    31,    32,    -1,    34,
      35,    36,    37,    38,    -1,    -1,    -1,   254,   255,    -1,
     126,   127,   128,   129,   130,   131,   132,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   282,   283,   284,   285,   286,
     287,   288,    -1,    -1,   291,    80,    -1,    82,    -1,    -1,
     297,    -1,    87,     5,     6,    16,     8,    -1,    -1,    -1,
      21,    22,    -1,    24,    25,    26,    27,    28,    29,   104,
      31,    32,    -1,    34,    35,    36,    37,    38,    16,    -1,
      -1,    -1,    -1,    -1,    22,    23,    24,    39,    26,    27,
      28,    29,    -1,    31,    32,    -1,    -1,    35,    36,    37,
      38,    -1,   137,    -1,    42,    43,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    68,    -1,    -1,    80,
      72,    82,    -1,    75,    76,    -1,    87,    -1,    80,    81,
      82,    83,    84,    85,    86,    87,    88,    89,    90,    91,
      -1,    -1,    -1,   104,    -1,    -1,    98,    99,    -1,    -1,
     102,    -1,    -1,    91,    -1,    -1,    -1,   109,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   118,    -1,    -1,    -1,
      -1,    -1,    -1,   111,   290,    -1,   137,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   227,    -1,   229,   230,   231,   232,   233,   234,
     235,   236,   237,   238,   239,   240,   241,   242,   243,   244,
     245,    22,    23,    24,   249,    26,    27,    28,    29,    -1,
      31,    32,    33,   258,    35,    36,    37,    38,    -1,    -1,
      -1,    42,    43,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   290,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   227,    -1,   229,   230,
     231,   232,   233,   234,   235,   236,   237,   238,   239,   240,
     241,   242,   243,   244,   245,    -1,    39,    40,   249,    -1,
      -1,    -1,    45,    -1,    47,    -1,    -1,   258,    -1,    -1,
     111,    54,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    71,    72,
      -1,    74,    75,    76,    77,    78,    -1,    -1,    -1,    -1,
      83,    -1,    -1,    86,    -1,    -1,    -1,    -1,    -1,    92,
      -1,    -1,    -1,    -1,    97,    -1,    -1,   100,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   112,
     113,   114,   115,   116,   117,   118,   119,    -1,    -1,    -1,
      -1,    -1,   125
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
       8,    99,   315,   316,   299,   363,    64,   126,   405,   179,
     180,   181,   182,   183,   184,   185,   186,   187,   188,   189,
     190,   191,   192,   193,   467,   467,     8,    14,    17,    40,
      41,    45,    47,    54,    71,    86,   295,   326,   364,   365,
     366,   367,   298,   299,   275,   471,   216,   475,   492,   216,
     316,     9,   317,   317,     9,    10,   318,   318,    13,    16,
      20,    21,    22,    23,    24,    26,    27,    28,    29,    31,
      32,    35,    36,    37,    38,    42,    43,    51,    53,    68,
      69,    70,   104,   105,   160,   161,   162,   299,   308,   316,
     322,   323,   367,   368,   426,   449,   450,   455,   456,   290,
     316,   316,   316,   316,     7,    12,   412,   413,   412,   412,
     290,   343,    60,   344,   290,   382,   388,    23,    26,    27,
      28,    29,    31,    32,    33,   290,   306,   406,   409,   411,
     412,   317,   290,   290,   290,   290,   488,   294,   317,   360,
     315,   299,   367,   426,   449,   451,   455,     7,    33,   298,
     313,   293,   295,   295,    46,    48,    49,    50,   365,   365,
     327,   368,   451,   298,   455,   295,   317,   317,   208,   316,
     475,   101,   102,   103,   126,   220,   222,   223,   224,   225,
     316,    75,    76,   316,   316,   455,    26,    27,    28,    29,
     449,    51,   449,    24,    25,    34,    15,    17,   455,   218,
     305,   316,   367,   308,   316,   317,   137,   137,   137,   364,
     365,   137,   307,   106,   107,   108,   137,   299,   301,   305,
     306,   312,   449,   313,   296,    12,   296,   296,   310,    39,
      68,    72,    75,    76,    80,    81,    82,    83,    84,    85,
      86,    87,    88,    89,    90,    91,    98,   102,   109,   118,
     316,   451,    61,   345,   346,    39,    72,    74,    75,    76,
      77,    78,    83,    92,    97,   100,   112,   113,   114,   115,
     116,   117,   118,   119,   125,   365,   142,   143,   144,   145,
     146,   147,   148,   149,   150,   151,   152,   153,   154,   163,
     164,   165,   166,   167,   168,   205,   219,   254,   255,   290,
     291,   314,   315,   321,   332,   387,   389,   390,   391,   392,
     394,   395,   403,   427,   428,   429,   430,   431,   432,   433,
     434,   435,   436,   437,   438,   439,   440,   441,   459,   469,
     305,   295,   299,   408,   295,   408,   295,   408,   295,   408,
     295,   408,   295,   408,   295,   407,   409,   295,   412,   296,
       7,     8,   293,   304,   476,   484,   489,   493,    73,    75,
      76,    82,   316,   316,   300,    39,    72,    74,    75,    76,
      77,   112,   113,   114,   115,   118,   122,   123,   124,   226,
     455,   298,   218,   316,   365,   295,   298,   295,   290,   295,
     292,     8,   317,   317,   296,   290,   295,   313,   120,   121,
     299,   316,   384,   451,   300,   167,   472,   316,   221,   137,
     449,    25,   316,   451,   316,   300,   300,   300,   316,   317,
     316,   316,   316,   455,   316,   316,   295,   295,   316,    20,
     300,   317,   457,   458,   444,   445,   455,   291,   312,   291,
     295,    75,    76,    77,   112,   113,   114,   301,   350,   347,
     451,    67,   155,   169,   174,   199,   200,   219,   254,   255,
     291,   314,   321,   332,   342,   358,   359,   369,   373,   381,
     403,   459,   469,   487,   295,   295,   385,   317,   317,   317,
     299,   111,   289,   299,   104,   451,   304,   198,   295,   388,
      55,    57,    58,    59,   393,   396,   397,   398,   399,   400,
     401,   315,   317,   390,   315,   317,   317,   318,    11,    31,
      32,   295,   318,   319,   315,   317,   364,    15,    17,   367,
     455,   451,    87,   313,   411,   365,   327,   295,   412,   295,
     317,   317,   317,   317,   318,   319,   319,   291,   293,   315,
     296,   317,   317,   209,   211,   214,   215,   291,   321,   332,
     459,   477,   479,   480,   482,    84,   209,   212,   291,   473,
     479,   481,   485,    41,   155,   207,   210,   291,   321,   332,
     490,   207,   210,   291,   321,   332,   494,    75,    76,    77,
     112,   113,   114,   295,   295,   316,   316,   300,   455,   313,
     463,   464,   290,    51,   451,   460,   461,     7,   293,   296,
     296,   326,   328,   329,   301,   357,   443,    19,   336,   473,
     137,   316,    19,   300,   450,   450,   450,   305,   451,   451,
      20,   293,   300,   302,   293,   317,    39,    51,    52,    69,
     120,   292,   303,   351,   352,   353,   293,   111,   370,   374,
     317,   317,   488,   111,   289,   104,   451,   290,   290,   290,
     388,   290,   317,   313,   383,   299,   455,   304,   317,   299,
     316,   299,   316,   317,   365,    19,   295,    20,   385,   446,
     447,   448,   291,   451,   393,    56,   390,   402,   315,   317,
     390,   402,   402,   402,    62,    62,   295,   295,   316,   451,
     295,   412,   455,   315,   317,   442,   296,   313,   296,   300,
     296,   296,   296,   296,   296,   407,   296,   304,     8,   293,
     213,   298,   305,   317,   478,   298,   313,   412,   412,   298,
     298,   412,   412,   295,   216,   317,   316,   216,   316,   216,
     317,    16,    21,    22,    24,    25,    26,    27,    28,    29,
      31,    32,    34,    35,    36,    37,    38,    80,    82,    87,
     104,   137,   227,   229,   230,   231,   232,   233,   234,   235,
     236,   237,   238,   239,   240,   241,   242,   243,   244,   245,
     249,   258,   290,   379,   380,   452,    63,   361,   300,   298,
     296,   293,   328,     8,   298,   291,   293,     8,   298,   291,
      22,    23,    24,    26,    27,    28,    29,    31,    32,    35,
      36,    37,    38,    42,    43,   111,   321,   330,   410,   411,
     415,   299,   444,   295,   295,   316,   384,    28,    29,    63,
     203,   204,   206,   412,   316,   316,   450,   295,   296,   296,
     317,   458,   455,   296,   295,   352,   295,   316,   355,   302,
     451,   451,    72,   118,   316,   451,    72,   118,   365,   316,
     299,   316,   299,   316,   365,    19,   346,   371,   375,   291,
     489,   296,   137,   383,    39,    46,    48,    49,    50,    87,
      88,    89,   134,   135,   136,   138,   141,   296,   251,   252,
     253,   317,   226,   378,   317,   300,   317,   317,   293,   300,
     455,   384,   446,   455,   296,   293,   315,   317,   315,   317,
     317,   318,    19,   313,   296,   295,   293,   293,   296,   296,
     408,   408,   408,   408,   408,   408,   317,   317,   317,   295,
     304,   295,   296,   296,   295,   295,   296,   296,   317,   450,
     316,    63,   316,   296,    25,    26,    27,    28,    29,   295,
     453,   243,   235,   247,   295,   228,   248,    22,   453,   453,
      21,    22,    24,    25,    26,    27,    28,    29,    31,    32,
      34,    35,    36,    37,    38,   230,   231,   233,   234,   235,
     237,   238,   241,   242,   244,   257,   258,   259,   260,   261,
     262,   263,   264,   265,   266,   267,   268,   269,   270,   271,
     305,   454,   296,   413,   299,   305,   315,   298,   362,    28,
      65,    66,   313,   317,   449,   465,   466,   463,   291,   298,
     290,   460,   290,   295,   313,   295,   299,   295,   299,    26,
      27,    28,    29,   295,   299,   295,   299,   295,   299,   295,
     299,   295,   299,   295,   299,   295,   299,   295,   299,   295,
     299,   295,   299,   295,   299,   295,   299,   295,   299,   105,
     110,   321,   331,   412,   317,   302,   446,   446,   357,   443,
     315,   296,   446,   317,   348,   349,   451,   293,   354,   316,
     197,   322,   316,   455,   317,   317,   293,   455,   384,   291,
     170,   171,   172,   173,   291,   314,   321,   332,   372,   469,
     173,   175,   176,   291,   314,   321,   332,   376,   469,   291,
     313,   296,   295,   304,   304,   300,   300,   300,   300,   295,
     384,   137,   300,   300,   451,   362,   451,   296,   378,   448,
      62,    62,   296,   296,   316,   296,   446,   442,   442,     8,
     293,     8,   478,   296,   317,   250,   313,   299,   299,    25,
      26,    27,    28,    29,   272,   293,   299,   306,   291,   292,
     300,   317,    22,    23,    24,    26,    27,    28,    29,    31,
      32,    35,    36,    37,    38,    44,   313,   410,   414,   295,
     295,   290,   330,   328,   465,   317,   317,   317,   295,   299,
     295,   299,   295,   299,   295,   299,   317,   317,   317,   317,
     317,   317,   318,   317,   317,   319,   317,   318,   319,   317,
     317,   317,   317,   317,   317,   317,   318,   317,   415,   317,
       8,    44,   317,    44,    51,   449,   317,    42,    91,   111,
     333,   456,   296,   300,   296,   296,   295,   295,   472,   296,
     296,   296,   293,   353,   354,   316,   300,   300,   451,   451,
     256,   364,   364,   364,   364,   364,   364,   364,   383,   317,
     139,   140,   139,   140,   379,   350,   315,   293,    19,   315,
     315,   317,   296,   317,   304,   298,   293,   317,   317,   313,
     300,   317,   292,   300,    26,    27,    28,    29,   317,    26,
      27,    28,   317,   330,   291,   291,   296,   300,   296,   300,
     317,   317,   317,   317,   317,   317,   318,   317,   296,   300,
     296,   300,   296,   300,   296,   300,   296,   296,   300,   296,
     296,   300,   296,   300,   296,   300,   296,   300,   296,   300,
     296,   300,   296,   296,   300,   296,     8,   296,   300,    51,
     449,   299,   316,   302,   446,   446,   451,   295,   293,    19,
     365,   296,   296,   296,   295,   451,   384,     8,   478,   317,
     313,   300,   300,   300,   317,   296,   304,   304,   304,   296,
     291,   295,   295,   296,   300,   296,   300,   296,   300,   296,
     300,   295,   295,   295,   295,   295,   295,   295,   295,   295,
     295,   295,   295,   296,   295,     8,   300,   298,   296,   296,
     446,   451,   384,   455,   446,   301,   356,   357,   304,   296,
     293,   296,   452,   300,   317,   317,   317,   422,   420,   295,
     295,   295,   295,   421,   420,   419,   418,   416,   417,   421,
     420,   419,   418,   425,   423,   424,   415,   296,   356,   451,
     296,   295,   478,   313,   296,   296,   296,   296,   465,   296,
     317,   421,   420,   419,   418,   296,   317,   296,   296,   317,
     296,   318,   296,   317,   319,   296,   318,   319,   296,   296,
     296,   296,   296,   415,     8,    44,   296,    44,    51,   296,
     449,   362,   295,    19,   386,   446,   293,   296,   296,   296,
     296,     8,   446,   384,    39,    54,    70,    79,    93,    94,
      95,    96,    99,   126,   127,   128,   129,   130,   131,   132,
     290,   296,   313,   296,   295,   295,   296,   256,   446,   317,
     104,   296,   296,   365,   455,   451,    19,   384,   356,   295,
     446,   296
};

/* YYR1[RULE-NUM] -- Symbol kind of the left-hand side of rule RULE-NUM.  */
static const yytype_int16 yyr1[] =
{
       0,   309,   310,   310,   311,   311,   311,   311,   311,   311,
     311,   311,   311,   311,   311,   311,   311,   311,   311,   311,
     311,   311,   311,   311,   311,   311,   311,   311,   311,   311,
     312,   312,   313,   313,   314,   314,   314,   315,   315,   315,
     316,   316,   316,   317,   318,   318,   319,   319,   319,   320,
     320,   320,   320,   320,   321,   321,   321,   321,   321,   321,
     321,   321,   321,   322,   322,   322,   322,   323,   323,   323,
     323,   324,   325,   326,   327,   327,   328,   329,   329,   329,
     330,   330,   330,   331,   331,   332,   332,   332,   333,   333,
     333,   333,   333,   333,   334,   334,   334,   335,   336,   336,
     336,   336,   336,   336,   337,   338,   339,   340,   341,   342,
     343,   343,   343,   343,   343,   343,   343,   343,   343,   343,
     343,   343,   343,   343,   343,   343,   343,   343,   343,   343,
     343,   343,   343,   343,   343,   343,   343,   344,   344,   345,
     345,   346,   346,   347,   347,   348,   348,   349,   349,   350,
     350,   351,   351,   351,   351,   351,   351,   351,   352,   352,
     353,   353,   354,   354,   355,   356,   356,   357,   358,   358,
     358,   358,   358,   358,   358,   358,   358,   358,   358,   358,
     358,   358,   358,   358,   358,   358,   358,   358,   358,   359,
     360,   360,   360,   360,   360,   360,   360,   360,   360,   360,
     360,   360,   360,   360,   360,   360,   361,   361,   362,   362,
     363,   363,   364,   364,   364,   364,   364,   364,   364,   365,
     365,   365,   365,   366,   366,   366,   366,   366,   366,   366,
     366,   367,   368,   368,   368,   368,   368,   368,   369,   369,
     370,   370,   370,   371,   371,   372,   372,   372,   372,   372,
     372,   372,   372,   373,   374,   374,   374,   375,   375,   376,
     376,   376,   376,   376,   376,   376,   377,   378,   378,   379,
     379,   380,   381,   382,   382,   382,   382,   382,   382,   382,
     382,   382,   382,   382,   382,   382,   382,   382,   382,   382,
     382,   382,   382,   382,   382,   382,   383,   383,   383,   383,
     383,   383,   383,   383,   383,   383,   383,   383,   383,   383,
     383,   383,   384,   384,   384,   385,   385,   385,   385,   385,
     386,   386,   386,   386,   386,   386,   386,   386,   386,   386,
     386,   386,   386,   386,   386,   386,   386,   387,   388,   388,
     389,   389,   389,   389,   389,   389,   389,   389,   389,   389,
     389,   389,   389,   389,   389,   389,   389,   389,   389,   389,
     389,   389,   389,   389,   389,   389,   390,   391,   392,   393,
     393,   394,   394,   394,   395,   396,   396,   396,   396,   397,
     397,   397,   398,   399,   400,   401,   402,   402,   402,   403,
     404,   404,   405,   405,   405,   406,   406,   407,   407,   408,
     408,   409,   409,   409,   409,   409,   409,   409,   409,   409,
     409,   409,   409,   409,   409,   409,   410,   410,   410,   410,
     410,   410,   410,   410,   410,   410,   410,   410,   410,   410,
     410,   410,   410,   410,   410,   411,   412,   412,   413,   413,
     414,   414,   414,   415,   415,   415,   415,   415,   415,   415,
     415,   415,   415,   415,   415,   415,   415,   415,   415,   415,
     415,   415,   415,   415,   415,   415,   415,   415,   415,   416,
     416,   416,   417,   417,   417,   418,   418,   419,   419,   420,
     420,   421,   421,   422,   422,   423,   423,   423,   424,   424,
     424,   424,   425,   425,   426,   427,   428,   429,   430,   431,
     432,   433,   434,   435,   436,   437,   438,   439,   440,   441,
     441,   441,   441,   441,   441,   441,   441,   441,   441,   441,
     441,   441,   441,   441,   441,   441,   441,   441,   441,   441,
     441,   441,   442,   442,   442,   442,   442,   443,   443,   444,
     444,   445,   445,   446,   446,   447,   447,   448,   448,   448,
     449,   449,   449,   449,   449,   449,   449,   449,   449,   449,
     450,   450,   451,   451,   451,   451,   452,   452,   452,   452,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     452,   453,   453,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   455,   455,   455,   455,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   455,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   455,   456,   456,   456,   456,
     456,   456,   456,   456,   456,   456,   456,   456,   456,   456,
     456,   456,   456,   456,   457,   457,   458,   458,   458,   458,
     458,   459,   459,   459,   459,   459,   459,   460,   460,   460,
     461,   461,   462,   462,   463,   463,   464,   465,   465,   466,
     466,   466,   466,   466,   466,   466,   466,   467,   467,   467,
     467,   467,   467,   467,   467,   467,   467,   467,   467,   467,
     467,   467,   468,   468,   469,   469,   469,   469,   469,   469,
     469,   469,   469,   469,   469,   470,   470,   471,   471,   472,
     472,   473,   474,   475,   475,   475,   475,   475,   475,   475,
     475,   475,   475,   476,   476,   477,   477,   477,   478,   478,
     479,   479,   479,   479,   479,   479,   480,   481,   482,   483,
     483,   484,   484,   485,   485,   485,   485,   486,   487,   488,
     488,   488,   488,   488,   488,   488,   488,   488,   488,   489,
     489,   490,   490,   490,   490,   490,   490,   490,   491,   491,
     492,   492,   492,   493,   493,   494,   494,   494,   494
};

/* YYR2[RULE-NUM] -- Number of symbols on the right-hand side of rule RULE-NUM.  */
static const yytype_int8 yyr2[] =
{
       0,     2,     0,     2,     4,     4,     3,     1,     1,     1,
       1,     1,     1,     4,     4,     4,     4,     1,     1,     1,
       2,     2,     3,     2,     2,     1,     1,     1,     4,     1,
       0,     2,     1,     3,     2,     4,     6,     1,     1,     1,
       1,     1,     3,     1,     1,     1,     1,     4,     4,     4,
       4,     4,     4,     4,     2,     3,     2,     2,     2,     1,
       1,     2,     1,     2,     4,     6,     3,     5,     7,     9,
       3,     4,     7,     1,     1,     1,     2,     0,     2,     2,
       0,     6,     2,     1,     1,     1,     1,     1,     1,     1,
       1,     3,     2,     3,     1,     2,     3,     7,     0,     2,
       2,     2,     2,     2,     3,     3,     2,     1,     4,     3,
       0,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     3,     3,     3,
       3,     3,     3,     2,     2,     2,     5,     0,     2,     0,
       2,     0,     2,     3,     1,     0,     1,     1,     3,     0,
       3,     1,     1,     1,     1,     1,     1,     4,     0,     2,
       4,     3,     0,     2,     3,     0,     1,     5,     3,     4,
       4,     4,     1,     1,     1,     1,     1,     2,     2,     4,
      13,    22,     1,     1,     5,     3,     7,     5,     4,     7,
       0,     2,     2,     2,     2,     2,     2,     2,     5,     2,
       2,     2,     2,     2,     2,     5,     0,     2,     0,     2,
       0,     3,     9,     9,     7,     7,     1,     1,     1,     2,
       2,     1,     4,     0,     1,     1,     2,     2,     2,     2,
       1,     4,     2,     5,     3,     2,     2,     1,     4,     3,
       0,     2,     2,     0,     2,     2,     2,     2,     2,     1,
       1,     1,     1,     9,     0,     2,     2,     0,     2,     2,
       2,     2,     1,     1,     1,     1,     1,     0,     4,     1,
       3,     1,    13,     0,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     5,     8,     6,     5,     0,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     4,     4,     4,
       4,     5,     1,     1,     1,     0,     4,     4,     4,     4,
       0,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     5,     1,     0,     2,
       2,     1,     2,     4,     5,     1,     1,     1,     1,     2,
       1,     1,     1,     1,     1,     4,     6,     4,     4,    11,
       1,     5,     3,     7,     5,     5,     3,     1,     2,     2,
       1,     2,     4,     4,     1,     2,     2,     2,     2,     2,
       2,     2,     1,     2,     1,     1,     1,     4,     4,     2,
       4,     2,     0,     1,     1,     3,     1,     3,     1,     0,
       3,     5,     4,     3,     5,     5,     5,     5,     5,     5,
       2,     2,     2,     2,     2,     2,     4,     4,     4,     4,
       4,     4,     4,     4,     5,     5,     5,     5,     4,     4,
       4,     4,     4,     4,     3,     2,     0,     1,     1,     2,
       1,     1,     1,     1,     4,     4,     5,     4,     4,     4,
       7,     7,     7,     7,     7,     7,     7,     7,     7,     7,
       8,     8,     8,     8,     7,     7,     7,     7,     7,     0,
       2,     2,     0,     2,     2,     0,     2,     0,     2,     0,
       2,     0,     2,     0,     2,     0,     2,     2,     0,     2,
       3,     2,     0,     2,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     2,     1,
       2,     2,     2,     2,     2,     2,     3,     2,     2,     2,
       5,     3,     2,     2,     2,     2,     2,     5,     4,     6,
       2,     4,     0,     3,     3,     1,     1,     0,     3,     0,
       1,     1,     3,     0,     1,     1,     3,     1,     3,     4,
       4,     4,     4,     5,     1,     1,     1,     1,     1,     1,
       1,     3,     1,     3,     4,     1,     0,    10,     6,     5,
       6,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     2,     2,     2,     2,     1,     1,     1,
       1,     2,     3,     4,     6,     5,     1,     1,     1,     1,
       1,     1,     1,     2,     2,     1,     2,     2,     4,     1,
       2,     1,     2,     1,     2,     1,     2,     1,     2,     1,
       1,     0,     5,     0,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     2,     2,     2,     2,     1,
       1,     1,     1,     1,     3,     2,     2,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     2,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     2,     1,     3,     2,     3,     4,     2,     2,
       2,     5,     5,     7,     4,     3,     2,     3,     2,     1,
       1,     2,     3,     2,     1,     2,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     2,     2,     2,     2,     1,
       1,     1,     1,     1,     1,     3,     0,     1,     1,     3,
       2,     6,     7,     3,     3,     3,     6,     0,     1,     3,
       5,     6,     4,     4,     1,     3,     3,     1,     1,     1,
       1,     4,     1,     6,     6,     6,     4,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     3,     2,     5,     4,     7,     6,
       7,     6,     9,     8,     3,     8,     4,     0,     2,     0,
       1,     3,     3,     0,     2,     2,     2,     3,     2,     2,
       2,     2,     2,     0,     2,     3,     1,     1,     1,     1,
       3,     8,     2,     3,     1,     1,     3,     3,     3,     4,
       6,     0,     2,     3,     1,     3,     1,     4,     3,     0,
       2,     2,     2,     3,     3,     3,     3,     3,     3,     0,
       2,     2,     3,     3,     4,     2,     1,     1,     3,     5,
       0,     2,     2,     0,     2,     4,     3,     1,     1
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
#line 3553 "prebuilt\\asmparse.cpp"
    break;

  case 5: /* decl: nameSpaceHead '{' decls '}'  */
#line 194 "asmparse.y"
                                                                                { PASM->EndNameSpace(); }
#line 3559 "prebuilt\\asmparse.cpp"
    break;

  case 6: /* decl: methodHead methodDecls '}'  */
#line 195 "asmparse.y"
                                                                                { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                                                  {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                                     PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                                                  PASM->EndMethod(); }
#line 3568 "prebuilt\\asmparse.cpp"
    break;

  case 13: /* decl: assemblyHead '{' assemblyDecls '}'  */
#line 205 "asmparse.y"
                                                                                { PASMM->EndAssembly(); }
#line 3574 "prebuilt\\asmparse.cpp"
    break;

  case 14: /* decl: assemblyRefHead '{' assemblyRefDecls '}'  */
#line 206 "asmparse.y"
                                                                                { PASMM->EndAssembly(); }
#line 3580 "prebuilt\\asmparse.cpp"
    break;

  case 15: /* decl: exptypeHead '{' exptypeDecls '}'  */
#line 207 "asmparse.y"
                                                                                { PASMM->EndComType(); }
#line 3586 "prebuilt\\asmparse.cpp"
    break;

  case 16: /* decl: manifestResHead '{' manifestResDecls '}'  */
#line 208 "asmparse.y"
                                                                                { PASMM->EndManifestRes(); }
#line 3592 "prebuilt\\asmparse.cpp"
    break;

  case 20: /* decl: _SUBSYSTEM int32  */
#line 212 "asmparse.y"
                                                                                {
                                                                                  PASM->m_dwSubsystem = (yyvsp[0].int32);
                                                                                }
#line 3600 "prebuilt\\asmparse.cpp"
    break;

  case 21: /* decl: _CORFLAGS int32  */
#line 215 "asmparse.y"
                                                                                { PASM->m_dwComImageFlags = (yyvsp[0].int32); }
#line 3606 "prebuilt\\asmparse.cpp"
    break;

  case 22: /* decl: _FILE ALIGNMENT_ int32  */
#line 216 "asmparse.y"
                                                                                { PASM->m_dwFileAlignment = (yyvsp[0].int32);
                                                                                  if(((yyvsp[0].int32) & ((yyvsp[0].int32) - 1))||((yyvsp[0].int32) < 0x200)||((yyvsp[0].int32) > 0x10000))
                                                                                    PASM->report->error("Invalid file alignment, must be power of 2 from 0x200 to 0x10000\n");}
#line 3614 "prebuilt\\asmparse.cpp"
    break;

  case 23: /* decl: _IMAGEBASE int64  */
#line 219 "asmparse.y"
                                                                                { PASM->m_stBaseAddress = (ULONGLONG)(*((yyvsp[0].int64))); delete (yyvsp[0].int64);
                                                                                  if(PASM->m_stBaseAddress & 0xFFFF)
                                                                                    PASM->report->error("Invalid image base, must be 0x10000-aligned\n");}
#line 3622 "prebuilt\\asmparse.cpp"
    break;

  case 24: /* decl: _STACKRESERVE int64  */
#line 222 "asmparse.y"
                                                                                { PASM->m_stSizeOfStackReserve = (size_t)(*((yyvsp[0].int64))); delete (yyvsp[0].int64); }
#line 3628 "prebuilt\\asmparse.cpp"
    break;

  case 29: /* decl: _MSCORLIB  */
#line 227 "asmparse.y"
                                                                                { PASM->m_fIsMscorlib = TRUE; }
#line 3634 "prebuilt\\asmparse.cpp"
    break;

  case 32: /* compQstring: QSTRING  */
#line 234 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[0].binstr); }
#line 3640 "prebuilt\\asmparse.cpp"
    break;

  case 33: /* compQstring: compQstring '+' QSTRING  */
#line 235 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 3646 "prebuilt\\asmparse.cpp"
    break;

  case 34: /* languageDecl: _LANGUAGE SQSTRING  */
#line 238 "asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[0].string),&(PASM->m_guidLang)); }
#line 3652 "prebuilt\\asmparse.cpp"
    break;

  case 35: /* languageDecl: _LANGUAGE SQSTRING ',' SQSTRING  */
#line 239 "asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid((yyvsp[0].string),&(PASM->m_guidLangVendor));}
#line 3659 "prebuilt\\asmparse.cpp"
    break;

  case 36: /* languageDecl: _LANGUAGE SQSTRING ',' SQSTRING ',' SQSTRING  */
#line 241 "asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[-4].string),&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidLangVendor));
                                                                                  LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidDoc));}
#line 3667 "prebuilt\\asmparse.cpp"
    break;

  case 37: /* id: ID  */
#line 246 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3673 "prebuilt\\asmparse.cpp"
    break;

  case 38: /* id: ASYNC_  */
#line 247 "asmparse.y"
                                                              { (yyval.string) = new char[] { 'a', 's', 'y', 'n', 'c', '\0' }; }
#line 3679 "prebuilt\\asmparse.cpp"
    break;

  case 39: /* id: SQSTRING  */
#line 248 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3685 "prebuilt\\asmparse.cpp"
    break;

  case 40: /* dottedName: id  */
#line 251 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3691 "prebuilt\\asmparse.cpp"
    break;

  case 41: /* dottedName: DOTTEDNAME  */
#line 252 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3697 "prebuilt\\asmparse.cpp"
    break;

  case 42: /* dottedName: dottedName '.' dottedName  */
#line 253 "asmparse.y"
                                                              { (yyval.string) = newStringWDel((yyvsp[-2].string), '.', (yyvsp[0].string)); }
#line 3703 "prebuilt\\asmparse.cpp"
    break;

  case 43: /* int32: INT32_V  */
#line 256 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 3709 "prebuilt\\asmparse.cpp"
    break;

  case 44: /* int64: INT64_V  */
#line 259 "asmparse.y"
                                                              { (yyval.int64) = (yyvsp[0].int64); }
#line 3715 "prebuilt\\asmparse.cpp"
    break;

  case 45: /* int64: INT32_V  */
#line 260 "asmparse.y"
                                                              { (yyval.int64) = neg ? new int64_t((yyvsp[0].int32)) : new int64_t((unsigned)(yyvsp[0].int32)); }
#line 3721 "prebuilt\\asmparse.cpp"
    break;

  case 46: /* float64: FLOAT64  */
#line 263 "asmparse.y"
                                                              { (yyval.float64) = (yyvsp[0].float64); }
#line 3727 "prebuilt\\asmparse.cpp"
    break;

  case 47: /* float64: FLOAT32_ '(' int32 ')'  */
#line 264 "asmparse.y"
                                                              { float f; *((int32_t*) (&f)) = (yyvsp[-1].int32); (yyval.float64) = new double(f); }
#line 3733 "prebuilt\\asmparse.cpp"
    break;

  case 48: /* float64: FLOAT64_ '(' int64 ')'  */
#line 265 "asmparse.y"
                                                              { (yyval.float64) = (double*) (yyvsp[-1].int64); }
#line 3739 "prebuilt\\asmparse.cpp"
    break;

  case 49: /* typedefDecl: _TYPEDEF type AS_ dottedName  */
#line 269 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].binstr),(yyvsp[0].string)); }
#line 3745 "prebuilt\\asmparse.cpp"
    break;

  case 50: /* typedefDecl: _TYPEDEF className AS_ dottedName  */
#line 270 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].token),(yyvsp[0].string)); }
#line 3751 "prebuilt\\asmparse.cpp"
    break;

  case 51: /* typedefDecl: _TYPEDEF memberRef AS_ dottedName  */
#line 271 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].token),(yyvsp[0].string)); }
#line 3757 "prebuilt\\asmparse.cpp"
    break;

  case 52: /* typedefDecl: _TYPEDEF customDescr AS_ dottedName  */
#line 272 "asmparse.y"
                                                                                { (yyvsp[-2].cad)->tkOwner = 0; PASM->AddTypeDef((yyvsp[-2].cad),(yyvsp[0].string)); }
#line 3763 "prebuilt\\asmparse.cpp"
    break;

  case 53: /* typedefDecl: _TYPEDEF customDescrWithOwner AS_ dottedName  */
#line 273 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].cad),(yyvsp[0].string)); }
#line 3769 "prebuilt\\asmparse.cpp"
    break;

  case 54: /* compControl: P_DEFINE dottedName  */
#line 278 "asmparse.y"
                                                                                { DefineVar((yyvsp[0].string), NULL); }
#line 3775 "prebuilt\\asmparse.cpp"
    break;

  case 55: /* compControl: P_DEFINE dottedName compQstring  */
#line 279 "asmparse.y"
                                                                                { DefineVar((yyvsp[-1].string), (yyvsp[0].binstr)); }
#line 3781 "prebuilt\\asmparse.cpp"
    break;

  case 56: /* compControl: P_UNDEF dottedName  */
#line 280 "asmparse.y"
                                                                                { UndefVar((yyvsp[0].string)); }
#line 3787 "prebuilt\\asmparse.cpp"
    break;

  case 57: /* compControl: P_IFDEF dottedName  */
#line 281 "asmparse.y"
                                                                                { SkipToken = !IsVarDefined((yyvsp[0].string));
                                                                                  IfEndif++;
                                                                                }
#line 3795 "prebuilt\\asmparse.cpp"
    break;

  case 58: /* compControl: P_IFNDEF dottedName  */
#line 284 "asmparse.y"
                                                                                { SkipToken = IsVarDefined((yyvsp[0].string));
                                                                                  IfEndif++;
                                                                                }
#line 3803 "prebuilt\\asmparse.cpp"
    break;

  case 59: /* compControl: P_ELSE  */
#line 287 "asmparse.y"
                                                                                { if(IfEndif == 1) SkipToken = !SkipToken;}
#line 3809 "prebuilt\\asmparse.cpp"
    break;

  case 60: /* compControl: P_ENDIF  */
#line 288 "asmparse.y"
                                                                                { if(IfEndif == 0)
                                                                                    PASM->report->error("Unmatched #endif\n");
                                                                                  else IfEndif--;
                                                                                }
#line 3818 "prebuilt\\asmparse.cpp"
    break;

  case 61: /* compControl: P_INCLUDE QSTRING  */
#line 292 "asmparse.y"
                                                                                { _ASSERTE(!"yylex should have dealt with this"); }
#line 3824 "prebuilt\\asmparse.cpp"
    break;

  case 62: /* compControl: ';'  */
#line 293 "asmparse.y"
                                                                                { }
#line 3830 "prebuilt\\asmparse.cpp"
    break;

  case 63: /* customDescr: _CUSTOM customType  */
#line 297 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[0].token), NULL); }
#line 3836 "prebuilt\\asmparse.cpp"
    break;

  case 64: /* customDescr: _CUSTOM customType '=' compQstring  */
#line 298 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].token), (yyvsp[0].binstr)); }
#line 3842 "prebuilt\\asmparse.cpp"
    break;

  case 65: /* customDescr: _CUSTOM customType '=' '{' customBlobDescr '}'  */
#line 299 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 3848 "prebuilt\\asmparse.cpp"
    break;

  case 66: /* customDescr: customHead bytes ')'  */
#line 300 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].int32), (yyvsp[-1].binstr)); }
#line 3854 "prebuilt\\asmparse.cpp"
    break;

  case 67: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType  */
#line 303 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-2].token), (yyvsp[0].token), NULL); }
#line 3860 "prebuilt\\asmparse.cpp"
    break;

  case 68: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType '=' compQstring  */
#line 304 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-4].token), (yyvsp[-2].token), (yyvsp[0].binstr)); }
#line 3866 "prebuilt\\asmparse.cpp"
    break;

  case 69: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType '=' '{' customBlobDescr '}'  */
#line 306 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-6].token), (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 3872 "prebuilt\\asmparse.cpp"
    break;

  case 70: /* customDescrWithOwner: customHeadWithOwner bytes ')'  */
#line 307 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].int32), (yyvsp[-1].binstr)); }
#line 3878 "prebuilt\\asmparse.cpp"
    break;

  case 71: /* customHead: _CUSTOM customType '=' '('  */
#line 310 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-2].token); bParsingByteArray = TRUE; }
#line 3884 "prebuilt\\asmparse.cpp"
    break;

  case 72: /* customHeadWithOwner: _CUSTOM '(' ownerType ')' customType '=' '('  */
#line 314 "asmparse.y"
                                                                                { PASM->m_pCustomDescrList = NULL;
                                                                                  PASM->m_tkCurrentCVOwner = (yyvsp[-4].token);
                                                                                  (yyval.int32) = (yyvsp[-2].token); bParsingByteArray = TRUE; }
#line 3892 "prebuilt\\asmparse.cpp"
    break;

  case 73: /* customType: methodRef  */
#line 319 "asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 3898 "prebuilt\\asmparse.cpp"
    break;

  case 74: /* ownerType: typeSpec  */
#line 322 "asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 3904 "prebuilt\\asmparse.cpp"
    break;

  case 75: /* ownerType: memberRef  */
#line 323 "asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 3910 "prebuilt\\asmparse.cpp"
    break;

  case 76: /* customBlobDescr: customBlobArgs customBlobNVPairs  */
#line 327 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr);
                                                                                  (yyval.binstr)->appendInt16(VAL16(nCustomBlobNVPairs));
                                                                                  (yyval.binstr)->append((yyvsp[0].binstr));
                                                                                  nCustomBlobNVPairs = 0; }
#line 3919 "prebuilt\\asmparse.cpp"
    break;

  case 77: /* customBlobArgs: %empty  */
#line 333 "asmparse.y"
                                                                                { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt16(VAL16(0x0001)); }
#line 3925 "prebuilt\\asmparse.cpp"
    break;

  case 78: /* customBlobArgs: customBlobArgs serInit  */
#line 334 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr);
                                                                                  AppendFieldToCustomBlob((yyval.binstr),(yyvsp[0].binstr)); }
#line 3932 "prebuilt\\asmparse.cpp"
    break;

  case 79: /* customBlobArgs: customBlobArgs compControl  */
#line 336 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 3938 "prebuilt\\asmparse.cpp"
    break;

  case 80: /* customBlobNVPairs: %empty  */
#line 339 "asmparse.y"
                                                                                { (yyval.binstr) = new BinStr(); }
#line 3944 "prebuilt\\asmparse.cpp"
    break;

  case 81: /* customBlobNVPairs: customBlobNVPairs fieldOrProp serializType dottedName '=' serInit  */
#line 341 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-5].binstr); (yyval.binstr)->appendInt8((yyvsp[-4].int32));
                                                                                  (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                                  AppendStringWithLength((yyval.binstr),(yyvsp[-2].string));
                                                                                  AppendFieldToCustomBlob((yyval.binstr),(yyvsp[0].binstr));
                                                                                  nCustomBlobNVPairs++; }
#line 3954 "prebuilt\\asmparse.cpp"
    break;

  case 82: /* customBlobNVPairs: customBlobNVPairs compControl  */
#line 346 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 3960 "prebuilt\\asmparse.cpp"
    break;

  case 83: /* fieldOrProp: FIELD_  */
#line 349 "asmparse.y"
                                                                                { (yyval.int32) = SERIALIZATION_TYPE_FIELD; }
#line 3966 "prebuilt\\asmparse.cpp"
    break;

  case 84: /* fieldOrProp: PROPERTY_  */
#line 350 "asmparse.y"
                                                                                { (yyval.int32) = SERIALIZATION_TYPE_PROPERTY; }
#line 3972 "prebuilt\\asmparse.cpp"
    break;

  case 85: /* customAttrDecl: customDescr  */
#line 353 "asmparse.y"
                                                                                { if((yyvsp[0].cad)->tkOwner && !(yyvsp[0].cad)->tkInterfacePair)
                                                                                    PASM->DefineCV((yyvsp[0].cad));
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH((yyvsp[0].cad)); }
#line 3981 "prebuilt\\asmparse.cpp"
    break;

  case 86: /* customAttrDecl: customDescrWithOwner  */
#line 357 "asmparse.y"
                                                                                { PASM->DefineCV((yyvsp[0].cad)); }
#line 3987 "prebuilt\\asmparse.cpp"
    break;

  case 87: /* customAttrDecl: TYPEDEF_CA  */
#line 358 "asmparse.y"
                                                                                { CustomDescr* pNew = new CustomDescr((yyvsp[0].tdd)->m_pCA);
                                                                                  if(pNew->tkOwner == 0) pNew->tkOwner = PASM->m_tkCurrentCVOwner;
                                                                                  if(pNew->tkOwner)
                                                                                    PASM->DefineCV(pNew);
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH(pNew); }
#line 3998 "prebuilt\\asmparse.cpp"
    break;

  case 88: /* serializType: simpleType  */
#line 366 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4004 "prebuilt\\asmparse.cpp"
    break;

  case 89: /* serializType: TYPE_  */
#line 367 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE); }
#line 4010 "prebuilt\\asmparse.cpp"
    break;

  case 90: /* serializType: OBJECT_  */
#line 368 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TAGGED_OBJECT); }
#line 4016 "prebuilt\\asmparse.cpp"
    break;

  case 91: /* serializType: ENUM_ CLASS_ SQSTRING  */
#line 369 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); }
#line 4023 "prebuilt\\asmparse.cpp"
    break;

  case 92: /* serializType: ENUM_ className  */
#line 371 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[0].token))); }
#line 4030 "prebuilt\\asmparse.cpp"
    break;

  case 93: /* serializType: serializType '[' ']'  */
#line 373 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 4036 "prebuilt\\asmparse.cpp"
    break;

  case 94: /* moduleHead: _MODULE  */
#line 378 "asmparse.y"
                                                                                { PASMM->SetModuleName(NULL); PASM->m_tkCurrentCVOwner=1; }
#line 4042 "prebuilt\\asmparse.cpp"
    break;

  case 95: /* moduleHead: _MODULE dottedName  */
#line 379 "asmparse.y"
                                                                                { PASMM->SetModuleName((yyvsp[0].string)); PASM->m_tkCurrentCVOwner=1; }
#line 4048 "prebuilt\\asmparse.cpp"
    break;

  case 96: /* moduleHead: _MODULE EXTERN_ dottedName  */
#line 380 "asmparse.y"
                                                                                { BinStr* pbs = new BinStr();
                                                                                  unsigned L = (unsigned)strlen((yyvsp[0].string));
                                                                                  memcpy((char*)(pbs->getBuff(L)),(yyvsp[0].string),L);
                                                                                  PASM->EmitImport(pbs); delete pbs;}
#line 4057 "prebuilt\\asmparse.cpp"
    break;

  case 97: /* vtfixupDecl: _VTFIXUP '[' int32 ']' vtfixupAttr AT_ id  */
#line 387 "asmparse.y"
                                                                                { /*PASM->SetDataSection(); PASM->EmitDataLabel($7);*/
                                                                                  PASM->m_VTFList.PUSH(new VTFEntry((USHORT)(yyvsp[-4].int32), (USHORT)(yyvsp[-2].int32), (yyvsp[0].string))); }
#line 4064 "prebuilt\\asmparse.cpp"
    break;

  case 98: /* vtfixupAttr: %empty  */
#line 391 "asmparse.y"
                                                                                { (yyval.int32) = 0; }
#line 4070 "prebuilt\\asmparse.cpp"
    break;

  case 99: /* vtfixupAttr: vtfixupAttr INT32_  */
#line 392 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_32BIT; }
#line 4076 "prebuilt\\asmparse.cpp"
    break;

  case 100: /* vtfixupAttr: vtfixupAttr INT64_  */
#line 393 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_64BIT; }
#line 4082 "prebuilt\\asmparse.cpp"
    break;

  case 101: /* vtfixupAttr: vtfixupAttr FROMUNMANAGED_  */
#line 394 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_FROM_UNMANAGED; }
#line 4088 "prebuilt\\asmparse.cpp"
    break;

  case 102: /* vtfixupAttr: vtfixupAttr CALLMOSTDERIVED_  */
#line 395 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_CALL_MOST_DERIVED; }
#line 4094 "prebuilt\\asmparse.cpp"
    break;

  case 103: /* vtfixupAttr: vtfixupAttr RETAINAPPDOMAIN_  */
#line 396 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN; }
#line 4100 "prebuilt\\asmparse.cpp"
    break;

  case 104: /* vtableDecl: vtableHead bytes ')'  */
#line 399 "asmparse.y"
                                                                                { PASM->m_pVTable = (yyvsp[-1].binstr); }
#line 4106 "prebuilt\\asmparse.cpp"
    break;

  case 105: /* vtableHead: _VTABLE '=' '('  */
#line 402 "asmparse.y"
                                                                                { bParsingByteArray = TRUE; }
#line 4112 "prebuilt\\asmparse.cpp"
    break;

  case 106: /* nameSpaceHead: _NAMESPACE dottedName  */
#line 406 "asmparse.y"
                                                                                { PASM->StartNameSpace((yyvsp[0].string)); }
#line 4118 "prebuilt\\asmparse.cpp"
    break;

  case 107: /* _class: _CLASS  */
#line 409 "asmparse.y"
                                                                                { newclass = TRUE; }
#line 4124 "prebuilt\\asmparse.cpp"
    break;

  case 108: /* classHeadBegin: _class classAttr dottedName typarsClause  */
#line 412 "asmparse.y"
                                                                                { if((yyvsp[0].typarlist)) FixupConstraints();
                                                                                  PASM->StartClass((yyvsp[-1].string), (yyvsp[-2].classAttr), (yyvsp[0].typarlist));
                                                                                  TyParFixupList.RESET(false);
                                                                                  newclass = FALSE;
                                                                                }
#line 4134 "prebuilt\\asmparse.cpp"
    break;

  case 109: /* classHead: classHeadBegin extendsClause implClause  */
#line 418 "asmparse.y"
                                                                                { PASM->AddClass(); }
#line 4140 "prebuilt\\asmparse.cpp"
    break;

  case 110: /* classAttr: %empty  */
#line 421 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) 0; }
#line 4146 "prebuilt\\asmparse.cpp"
    break;

  case 111: /* classAttr: classAttr PUBLIC_  */
#line 422 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdVisibilityMask) | tdPublic); }
#line 4152 "prebuilt\\asmparse.cpp"
    break;

  case 112: /* classAttr: classAttr PRIVATE_  */
#line 423 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdVisibilityMask) | tdNotPublic); }
#line 4158 "prebuilt\\asmparse.cpp"
    break;

  case 113: /* classAttr: classAttr VALUE_  */
#line 424 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | 0x80000000 | tdSealed); }
#line 4164 "prebuilt\\asmparse.cpp"
    break;

  case 114: /* classAttr: classAttr ENUM_  */
#line 425 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | 0x40000000); }
#line 4170 "prebuilt\\asmparse.cpp"
    break;

  case 115: /* classAttr: classAttr INTERFACE_  */
#line 426 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdInterface | tdAbstract); }
#line 4176 "prebuilt\\asmparse.cpp"
    break;

  case 116: /* classAttr: classAttr SEALED_  */
#line 427 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSealed); }
#line 4182 "prebuilt\\asmparse.cpp"
    break;

  case 117: /* classAttr: classAttr ABSTRACT_  */
#line 428 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdAbstract); }
#line 4188 "prebuilt\\asmparse.cpp"
    break;

  case 118: /* classAttr: classAttr AUTO_  */
#line 429 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdAutoLayout); }
#line 4194 "prebuilt\\asmparse.cpp"
    break;

  case 119: /* classAttr: classAttr SEQUENTIAL_  */
#line 430 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdSequentialLayout); }
#line 4200 "prebuilt\\asmparse.cpp"
    break;

  case 120: /* classAttr: classAttr EXPLICIT_  */
#line 431 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdExplicitLayout); }
#line 4206 "prebuilt\\asmparse.cpp"
    break;

  case 121: /* classAttr: classAttr ANSI_  */
#line 432 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdAnsiClass); }
#line 4212 "prebuilt\\asmparse.cpp"
    break;

  case 122: /* classAttr: classAttr UNICODE_  */
#line 433 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdUnicodeClass); }
#line 4218 "prebuilt\\asmparse.cpp"
    break;

  case 123: /* classAttr: classAttr AUTOCHAR_  */
#line 434 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdAutoClass); }
#line 4224 "prebuilt\\asmparse.cpp"
    break;

  case 124: /* classAttr: classAttr IMPORT_  */
#line 435 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdImport); }
#line 4230 "prebuilt\\asmparse.cpp"
    break;

  case 125: /* classAttr: classAttr SERIALIZABLE_  */
#line 436 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSerializable); }
#line 4236 "prebuilt\\asmparse.cpp"
    break;

  case 126: /* classAttr: classAttr WINDOWSRUNTIME_  */
#line 437 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdWindowsRuntime); }
#line 4242 "prebuilt\\asmparse.cpp"
    break;

  case 127: /* classAttr: classAttr NESTED_ PUBLIC_  */
#line 438 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedPublic); }
#line 4248 "prebuilt\\asmparse.cpp"
    break;

  case 128: /* classAttr: classAttr NESTED_ PRIVATE_  */
#line 439 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedPrivate); }
#line 4254 "prebuilt\\asmparse.cpp"
    break;

  case 129: /* classAttr: classAttr NESTED_ FAMILY_  */
#line 440 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamily); }
#line 4260 "prebuilt\\asmparse.cpp"
    break;

  case 130: /* classAttr: classAttr NESTED_ ASSEMBLY_  */
#line 441 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedAssembly); }
#line 4266 "prebuilt\\asmparse.cpp"
    break;

  case 131: /* classAttr: classAttr NESTED_ FAMANDASSEM_  */
#line 442 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamANDAssem); }
#line 4272 "prebuilt\\asmparse.cpp"
    break;

  case 132: /* classAttr: classAttr NESTED_ FAMORASSEM_  */
#line 443 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamORAssem); }
#line 4278 "prebuilt\\asmparse.cpp"
    break;

  case 133: /* classAttr: classAttr BEFOREFIELDINIT_  */
#line 444 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdBeforeFieldInit); }
#line 4284 "prebuilt\\asmparse.cpp"
    break;

  case 134: /* classAttr: classAttr SPECIALNAME_  */
#line 445 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSpecialName); }
#line 4290 "prebuilt\\asmparse.cpp"
    break;

  case 135: /* classAttr: classAttr RTSPECIALNAME_  */
#line 446 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr)); }
#line 4296 "prebuilt\\asmparse.cpp"
    break;

  case 136: /* classAttr: classAttr FLAGS_ '(' int32 ')'  */
#line 447 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].int32)); }
#line 4302 "prebuilt\\asmparse.cpp"
    break;

  case 138: /* extendsClause: EXTENDS_ typeSpec  */
#line 451 "asmparse.y"
                                                                            { PASM->m_crExtends = (yyvsp[0].token); }
#line 4308 "prebuilt\\asmparse.cpp"
    break;

  case 143: /* implList: implList ',' typeSpec  */
#line 462 "asmparse.y"
                                                            { PASM->AddToImplList((yyvsp[0].token)); }
#line 4314 "prebuilt\\asmparse.cpp"
    break;

  case 144: /* implList: typeSpec  */
#line 463 "asmparse.y"
                                                            { PASM->AddToImplList((yyvsp[0].token)); }
#line 4320 "prebuilt\\asmparse.cpp"
    break;

  case 145: /* typeList: %empty  */
#line 467 "asmparse.y"
                                                            { (yyval.binstr) = new BinStr(); }
#line 4326 "prebuilt\\asmparse.cpp"
    break;

  case 146: /* typeList: typeListNotEmpty  */
#line 468 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4332 "prebuilt\\asmparse.cpp"
    break;

  case 147: /* typeListNotEmpty: typeSpec  */
#line 471 "asmparse.y"
                                                            { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[0].token)); }
#line 4338 "prebuilt\\asmparse.cpp"
    break;

  case 148: /* typeListNotEmpty: typeListNotEmpty ',' typeSpec  */
#line 472 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->appendInt32((yyvsp[0].token)); }
#line 4344 "prebuilt\\asmparse.cpp"
    break;

  case 149: /* typarsClause: %empty  */
#line 475 "asmparse.y"
                                                            { (yyval.typarlist) = NULL; PASM->m_TyParList = NULL;}
#line 4350 "prebuilt\\asmparse.cpp"
    break;

  case 150: /* typarsClause: '<' typars '>'  */
#line 476 "asmparse.y"
                                                            { (yyval.typarlist) = (yyvsp[-1].typarlist);   PASM->m_TyParList = (yyvsp[-1].typarlist);}
#line 4356 "prebuilt\\asmparse.cpp"
    break;

  case 151: /* typarAttrib: '+'  */
#line 479 "asmparse.y"
                                                            { (yyval.int32) = gpCovariant; }
#line 4362 "prebuilt\\asmparse.cpp"
    break;

  case 152: /* typarAttrib: '-'  */
#line 480 "asmparse.y"
                                                            { (yyval.int32) = gpContravariant; }
#line 4368 "prebuilt\\asmparse.cpp"
    break;

  case 153: /* typarAttrib: CLASS_  */
#line 481 "asmparse.y"
                                                            { (yyval.int32) = gpReferenceTypeConstraint; }
#line 4374 "prebuilt\\asmparse.cpp"
    break;

  case 154: /* typarAttrib: VALUETYPE_  */
#line 482 "asmparse.y"
                                                            { (yyval.int32) = gpNotNullableValueTypeConstraint; }
#line 4380 "prebuilt\\asmparse.cpp"
    break;

  case 155: /* typarAttrib: BYREFLIKE_  */
#line 483 "asmparse.y"
                                                            { (yyval.int32) = gpAllowByRefLike; }
#line 4386 "prebuilt\\asmparse.cpp"
    break;

  case 156: /* typarAttrib: _CTOR  */
#line 484 "asmparse.y"
                                                            { (yyval.int32) = gpDefaultConstructorConstraint; }
#line 4392 "prebuilt\\asmparse.cpp"
    break;

  case 157: /* typarAttrib: FLAGS_ '(' int32 ')'  */
#line 485 "asmparse.y"
                                                            { (yyval.int32) = (CorGenericParamAttr)(yyvsp[-1].int32); }
#line 4398 "prebuilt\\asmparse.cpp"
    break;

  case 158: /* typarAttribs: %empty  */
#line 488 "asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 4404 "prebuilt\\asmparse.cpp"
    break;

  case 159: /* typarAttribs: typarAttrib typarAttribs  */
#line 489 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) | (yyvsp[0].int32); }
#line 4410 "prebuilt\\asmparse.cpp"
    break;

  case 160: /* typars: typarAttribs tyBound dottedName typarsRest  */
#line 492 "asmparse.y"
                                                                     {(yyval.typarlist) = new TyParList((yyvsp[-3].int32), (yyvsp[-2].binstr), (yyvsp[-1].string), (yyvsp[0].typarlist));}
#line 4416 "prebuilt\\asmparse.cpp"
    break;

  case 161: /* typars: typarAttribs dottedName typarsRest  */
#line 493 "asmparse.y"
                                                               {(yyval.typarlist) = new TyParList((yyvsp[-2].int32), NULL, (yyvsp[-1].string), (yyvsp[0].typarlist));}
#line 4422 "prebuilt\\asmparse.cpp"
    break;

  case 162: /* typarsRest: %empty  */
#line 496 "asmparse.y"
                                                            { (yyval.typarlist) = NULL; }
#line 4428 "prebuilt\\asmparse.cpp"
    break;

  case 163: /* typarsRest: ',' typars  */
#line 497 "asmparse.y"
                                                            { (yyval.typarlist) = (yyvsp[0].typarlist); }
#line 4434 "prebuilt\\asmparse.cpp"
    break;

  case 164: /* tyBound: '(' typeList ')'  */
#line 500 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 4440 "prebuilt\\asmparse.cpp"
    break;

  case 165: /* genArity: %empty  */
#line 503 "asmparse.y"
                                                            { (yyval.int32)= 0; }
#line 4446 "prebuilt\\asmparse.cpp"
    break;

  case 166: /* genArity: genArityNotEmpty  */
#line 504 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[0].int32); }
#line 4452 "prebuilt\\asmparse.cpp"
    break;

  case 167: /* genArityNotEmpty: '<' '[' int32 ']' '>'  */
#line 507 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-2].int32); }
#line 4458 "prebuilt\\asmparse.cpp"
    break;

  case 168: /* classDecl: methodHead methodDecls '}'  */
#line 511 "asmparse.y"
                                                            { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                              {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                 PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                              PASM->EndMethod(); }
#line 4467 "prebuilt\\asmparse.cpp"
    break;

  case 169: /* classDecl: classHead '{' classDecls '}'  */
#line 515 "asmparse.y"
                                                            { PASM->EndClass(); }
#line 4473 "prebuilt\\asmparse.cpp"
    break;

  case 170: /* classDecl: eventHead '{' eventDecls '}'  */
#line 516 "asmparse.y"
                                                            { PASM->EndEvent(); }
#line 4479 "prebuilt\\asmparse.cpp"
    break;

  case 171: /* classDecl: propHead '{' propDecls '}'  */
#line 517 "asmparse.y"
                                                            { PASM->EndProp(); }
#line 4485 "prebuilt\\asmparse.cpp"
    break;

  case 177: /* classDecl: _SIZE int32  */
#line 523 "asmparse.y"
                                                                { PASM->m_pCurClass->m_ulSize = (yyvsp[0].int32); }
#line 4491 "prebuilt\\asmparse.cpp"
    break;

  case 178: /* classDecl: _PACK int32  */
#line 524 "asmparse.y"
                                                                { PASM->m_pCurClass->m_ulPack = (yyvsp[0].int32); }
#line 4497 "prebuilt\\asmparse.cpp"
    break;

  case 179: /* classDecl: exportHead '{' exptypeDecls '}'  */
#line 525 "asmparse.y"
                                                                { PASMM->EndComType(); }
#line 4503 "prebuilt\\asmparse.cpp"
    break;

  case 180: /* classDecl: _OVERRIDE typeSpec DCOLON methodName WITH_ callConv type typeSpec DCOLON methodName '(' sigArgs0 ')'  */
#line 527 "asmparse.y"
                                                                { BinStr *sig1 = parser->MakeSig((yyvsp[-7].int32), (yyvsp[-6].binstr), (yyvsp[-1].binstr));
                                                                  BinStr *sig2 = new BinStr(); sig2->append(sig1);
                                                                  PASM->AddMethodImpl((yyvsp[-11].token),(yyvsp[-9].string),sig1,(yyvsp[-5].token),(yyvsp[-3].string),sig2);
                                                                  PASM->ResetArgNameList();
                                                                }
#line 4513 "prebuilt\\asmparse.cpp"
    break;

  case 181: /* classDecl: _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')' WITH_ METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'  */
#line 533 "asmparse.y"
                                                                 { PASM->AddMethodImpl((yyvsp[-17].token),(yyvsp[-15].string),
                                                                      ((yyvsp[-14].int32)==0 ? parser->MakeSig((yyvsp[-19].int32),(yyvsp[-18].binstr),(yyvsp[-12].binstr)) :
                                                                      parser->MakeSig((yyvsp[-19].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-18].binstr),(yyvsp[-12].binstr),(yyvsp[-14].int32))),
                                                                      (yyvsp[-6].token),(yyvsp[-4].string),
                                                                      ((yyvsp[-3].int32)==0 ? parser->MakeSig((yyvsp[-8].int32),(yyvsp[-7].binstr),(yyvsp[-1].binstr)) :
                                                                      parser->MakeSig((yyvsp[-8].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-7].binstr),(yyvsp[-1].binstr),(yyvsp[-3].int32))));
                                                                   PASM->ResetArgNameList();
                                                                 }
#line 4526 "prebuilt\\asmparse.cpp"
    break;

  case 184: /* classDecl: _PARAM TYPE_ '[' int32 ']'  */
#line 543 "asmparse.y"
                                                            { if(((yyvsp[-1].int32) > 0) && ((yyvsp[-1].int32) <= (int)PASM->m_pCurClass->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[(yyvsp[-1].int32)-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
#line 4536 "prebuilt\\asmparse.cpp"
    break;

  case 185: /* classDecl: _PARAM TYPE_ dottedName  */
#line 548 "asmparse.y"
                                                            { int n = PASM->m_pCurClass->FindTyPar((yyvsp[0].string));
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",(yyvsp[0].string));
                                                            }
#line 4547 "prebuilt\\asmparse.cpp"
    break;

  case 186: /* classDecl: _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec  */
#line 554 "asmparse.y"
                                                                        { PASM->AddGenericParamConstraint((yyvsp[-3].int32), 0, (yyvsp[0].token)); }
#line 4553 "prebuilt\\asmparse.cpp"
    break;

  case 187: /* classDecl: _PARAM CONSTRAINT_ dottedName ',' typeSpec  */
#line 555 "asmparse.y"
                                                                        { PASM->AddGenericParamConstraint(0, (yyvsp[-2].string), (yyvsp[0].token)); }
#line 4559 "prebuilt\\asmparse.cpp"
    break;

  case 188: /* classDecl: _INTERFACEIMPL TYPE_ typeSpec customDescr  */
#line 556 "asmparse.y"
                                                                      { (yyvsp[0].cad)->tkInterfacePair = (yyvsp[-1].token);
                                                                        if(PASM->m_pCustomDescrList)
                                                                            PASM->m_pCustomDescrList->PUSH((yyvsp[0].cad));
                                                                      }
#line 4568 "prebuilt\\asmparse.cpp"
    break;

  case 189: /* fieldDecl: _FIELD repeatOpt fieldAttr type dottedName atOpt initOpt  */
#line 564 "asmparse.y"
                                                            { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                              PASM->AddField((yyvsp[-2].string), (yyvsp[-3].binstr), (yyvsp[-4].fieldAttr), (yyvsp[-1].string), (yyvsp[0].binstr), (yyvsp[-5].int32)); }
#line 4575 "prebuilt\\asmparse.cpp"
    break;

  case 190: /* fieldAttr: %empty  */
#line 568 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) 0; }
#line 4581 "prebuilt\\asmparse.cpp"
    break;

  case 191: /* fieldAttr: fieldAttr STATIC_  */
#line 569 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdStatic); }
#line 4587 "prebuilt\\asmparse.cpp"
    break;

  case 192: /* fieldAttr: fieldAttr PUBLIC_  */
#line 570 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPublic); }
#line 4593 "prebuilt\\asmparse.cpp"
    break;

  case 193: /* fieldAttr: fieldAttr PRIVATE_  */
#line 571 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPrivate); }
#line 4599 "prebuilt\\asmparse.cpp"
    break;

  case 194: /* fieldAttr: fieldAttr FAMILY_  */
#line 572 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamily); }
#line 4605 "prebuilt\\asmparse.cpp"
    break;

  case 195: /* fieldAttr: fieldAttr INITONLY_  */
#line 573 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdInitOnly); }
#line 4611 "prebuilt\\asmparse.cpp"
    break;

  case 196: /* fieldAttr: fieldAttr RTSPECIALNAME_  */
#line 574 "asmparse.y"
                                                            { (yyval.fieldAttr) = (yyvsp[-1].fieldAttr); }
#line 4617 "prebuilt\\asmparse.cpp"
    break;

  case 197: /* fieldAttr: fieldAttr SPECIALNAME_  */
#line 575 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdSpecialName); }
#line 4623 "prebuilt\\asmparse.cpp"
    break;

  case 198: /* fieldAttr: fieldAttr MARSHAL_ '(' marshalBlob ')'  */
#line 588 "asmparse.y"
                                                            { PASM->m_pMarshal = (yyvsp[-1].binstr); }
#line 4629 "prebuilt\\asmparse.cpp"
    break;

  case 199: /* fieldAttr: fieldAttr ASSEMBLY_  */
#line 589 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdAssembly); }
#line 4635 "prebuilt\\asmparse.cpp"
    break;

  case 200: /* fieldAttr: fieldAttr FAMANDASSEM_  */
#line 590 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamANDAssem); }
#line 4641 "prebuilt\\asmparse.cpp"
    break;

  case 201: /* fieldAttr: fieldAttr FAMORASSEM_  */
#line 591 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamORAssem); }
#line 4647 "prebuilt\\asmparse.cpp"
    break;

  case 202: /* fieldAttr: fieldAttr PRIVATESCOPE_  */
#line 592 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPrivateScope); }
#line 4653 "prebuilt\\asmparse.cpp"
    break;

  case 203: /* fieldAttr: fieldAttr LITERAL_  */
#line 593 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdLiteral); }
#line 4659 "prebuilt\\asmparse.cpp"
    break;

  case 204: /* fieldAttr: fieldAttr NOTSERIALIZED_  */
#line 594 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdNotSerialized); }
#line 4665 "prebuilt\\asmparse.cpp"
    break;

  case 205: /* fieldAttr: fieldAttr FLAGS_ '(' int32 ')'  */
#line 595 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].int32)); }
#line 4671 "prebuilt\\asmparse.cpp"
    break;

  case 206: /* atOpt: %empty  */
#line 598 "asmparse.y"
                                                            { (yyval.string) = 0; }
#line 4677 "prebuilt\\asmparse.cpp"
    break;

  case 207: /* atOpt: AT_ id  */
#line 599 "asmparse.y"
                                                            { (yyval.string) = (yyvsp[0].string); }
#line 4683 "prebuilt\\asmparse.cpp"
    break;

  case 208: /* initOpt: %empty  */
#line 602 "asmparse.y"
                                                            { (yyval.binstr) = NULL; }
#line 4689 "prebuilt\\asmparse.cpp"
    break;

  case 209: /* initOpt: '=' fieldInit  */
#line 603 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4695 "prebuilt\\asmparse.cpp"
    break;

  case 210: /* repeatOpt: %empty  */
#line 606 "asmparse.y"
                                                            { (yyval.int32) = 0xFFFFFFFF; }
#line 4701 "prebuilt\\asmparse.cpp"
    break;

  case 211: /* repeatOpt: '[' int32 ']'  */
#line 607 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32); }
#line 4707 "prebuilt\\asmparse.cpp"
    break;

  case 212: /* methodRef: callConv type typeSpec DCOLON methodName tyArgs0 '(' sigArgs0 ')'  */
#line 612 "asmparse.y"
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
#line 4728 "prebuilt\\asmparse.cpp"
    break;

  case 213: /* methodRef: callConv type typeSpec DCOLON methodName genArityNotEmpty '(' sigArgs0 ')'  */
#line 629 "asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(((yyvsp[-8].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               (yyval.token) = PASM->MakeMemberRef((yyvsp[-6].token), (yyvsp[-4].string),
                                                                 parser->MakeSig((yyvsp[-8].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-7].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32)));
                                                             }
#line 4738 "prebuilt\\asmparse.cpp"
    break;

  case 214: /* methodRef: callConv type methodName tyArgs0 '(' sigArgs0 ')'  */
#line 635 "asmparse.y"
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
#line 4758 "prebuilt\\asmparse.cpp"
    break;

  case 215: /* methodRef: callConv type methodName genArityNotEmpty '(' sigArgs0 ')'  */
#line 651 "asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(((yyvsp[-6].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               (yyval.token) = PASM->MakeMemberRef(mdTokenNil, (yyvsp[-4].string), parser->MakeSig((yyvsp[-6].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-5].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32)));
                                                             }
#line 4767 "prebuilt\\asmparse.cpp"
    break;

  case 216: /* methodRef: mdtoken  */
#line 655 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token); }
#line 4773 "prebuilt\\asmparse.cpp"
    break;

  case 217: /* methodRef: TYPEDEF_M  */
#line 656 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 4779 "prebuilt\\asmparse.cpp"
    break;

  case 218: /* methodRef: TYPEDEF_MR  */
#line 657 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 4785 "prebuilt\\asmparse.cpp"
    break;

  case 219: /* callConv: INSTANCE_ callConv  */
#line 660 "asmparse.y"
                                                              { (yyval.int32) = ((yyvsp[0].int32) | IMAGE_CEE_CS_CALLCONV_HASTHIS); }
#line 4791 "prebuilt\\asmparse.cpp"
    break;

  case 220: /* callConv: EXPLICIT_ callConv  */
#line 661 "asmparse.y"
                                                              { (yyval.int32) = ((yyvsp[0].int32) | IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS); }
#line 4797 "prebuilt\\asmparse.cpp"
    break;

  case 221: /* callConv: callKind  */
#line 662 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 4803 "prebuilt\\asmparse.cpp"
    break;

  case 222: /* callConv: CALLCONV_ '(' int32 ')'  */
#line 663 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32); }
#line 4809 "prebuilt\\asmparse.cpp"
    break;

  case 223: /* callKind: %empty  */
#line 666 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
#line 4815 "prebuilt\\asmparse.cpp"
    break;

  case 224: /* callKind: DEFAULT_  */
#line 667 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
#line 4821 "prebuilt\\asmparse.cpp"
    break;

  case 225: /* callKind: VARARG_  */
#line 668 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_VARARG; }
#line 4827 "prebuilt\\asmparse.cpp"
    break;

  case 226: /* callKind: UNMANAGED_ CDECL_  */
#line 669 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_C; }
#line 4833 "prebuilt\\asmparse.cpp"
    break;

  case 227: /* callKind: UNMANAGED_ STDCALL_  */
#line 670 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_STDCALL; }
#line 4839 "prebuilt\\asmparse.cpp"
    break;

  case 228: /* callKind: UNMANAGED_ THISCALL_  */
#line 671 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_THISCALL; }
#line 4845 "prebuilt\\asmparse.cpp"
    break;

  case 229: /* callKind: UNMANAGED_ FASTCALL_  */
#line 672 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_FASTCALL; }
#line 4851 "prebuilt\\asmparse.cpp"
    break;

  case 230: /* callKind: UNMANAGED_  */
#line 673 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_UNMANAGED; }
#line 4857 "prebuilt\\asmparse.cpp"
    break;

  case 231: /* mdtoken: MDTOKEN_ '(' int32 ')'  */
#line 676 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[-1].int32); }
#line 4863 "prebuilt\\asmparse.cpp"
    break;

  case 232: /* memberRef: methodSpec methodRef  */
#line 679 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token);
                                                               PASM->delArgNameList(PASM->m_firstArgName);
                                                               PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                               PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                               PASM->SetMemberRefFixup((yyvsp[0].token),iOpcodeLen); }
#line 4873 "prebuilt\\asmparse.cpp"
    break;

  case 233: /* memberRef: FIELD_ type typeSpec DCOLON dottedName  */
#line 685 "asmparse.y"
                                                             { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               (yyval.token) = PASM->MakeMemberRef((yyvsp[-2].token), (yyvsp[0].string), (yyvsp[-3].binstr));
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4881 "prebuilt\\asmparse.cpp"
    break;

  case 234: /* memberRef: FIELD_ type dottedName  */
#line 689 "asmparse.y"
                                                             { (yyvsp[-1].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               (yyval.token) = PASM->MakeMemberRef(mdTokenNil, (yyvsp[0].string), (yyvsp[-1].binstr));
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4889 "prebuilt\\asmparse.cpp"
    break;

  case 235: /* memberRef: FIELD_ TYPEDEF_F  */
#line 692 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4896 "prebuilt\\asmparse.cpp"
    break;

  case 236: /* memberRef: FIELD_ TYPEDEF_MR  */
#line 694 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4903 "prebuilt\\asmparse.cpp"
    break;

  case 237: /* memberRef: mdtoken  */
#line 696 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token);
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4910 "prebuilt\\asmparse.cpp"
    break;

  case 238: /* eventHead: _EVENT eventAttr typeSpec dottedName  */
#line 701 "asmparse.y"
                                                                 { PASM->ResetEvent((yyvsp[0].string), (yyvsp[-1].token), (yyvsp[-2].eventAttr)); }
#line 4916 "prebuilt\\asmparse.cpp"
    break;

  case 239: /* eventHead: _EVENT eventAttr dottedName  */
#line 702 "asmparse.y"
                                                                 { PASM->ResetEvent((yyvsp[0].string), mdTypeRefNil, (yyvsp[-1].eventAttr)); }
#line 4922 "prebuilt\\asmparse.cpp"
    break;

  case 240: /* eventAttr: %empty  */
#line 706 "asmparse.y"
                                                            { (yyval.eventAttr) = (CorEventAttr) 0; }
#line 4928 "prebuilt\\asmparse.cpp"
    break;

  case 241: /* eventAttr: eventAttr RTSPECIALNAME_  */
#line 707 "asmparse.y"
                                                            { (yyval.eventAttr) = (yyvsp[-1].eventAttr); }
#line 4934 "prebuilt\\asmparse.cpp"
    break;

  case 242: /* eventAttr: eventAttr SPECIALNAME_  */
#line 708 "asmparse.y"
                                                            { (yyval.eventAttr) = (CorEventAttr) ((yyvsp[-1].eventAttr) | evSpecialName); }
#line 4940 "prebuilt\\asmparse.cpp"
    break;

  case 245: /* eventDecl: _ADDON methodRef  */
#line 715 "asmparse.y"
                                                           { PASM->SetEventMethod(0, (yyvsp[0].token)); }
#line 4946 "prebuilt\\asmparse.cpp"
    break;

  case 246: /* eventDecl: _REMOVEON methodRef  */
#line 716 "asmparse.y"
                                                           { PASM->SetEventMethod(1, (yyvsp[0].token)); }
#line 4952 "prebuilt\\asmparse.cpp"
    break;

  case 247: /* eventDecl: _FIRE methodRef  */
#line 717 "asmparse.y"
                                                           { PASM->SetEventMethod(2, (yyvsp[0].token)); }
#line 4958 "prebuilt\\asmparse.cpp"
    break;

  case 248: /* eventDecl: _OTHER methodRef  */
#line 718 "asmparse.y"
                                                           { PASM->SetEventMethod(3, (yyvsp[0].token)); }
#line 4964 "prebuilt\\asmparse.cpp"
    break;

  case 253: /* propHead: _PROPERTY propAttr callConv type dottedName '(' sigArgs0 ')' initOpt  */
#line 727 "asmparse.y"
                                                            { PASM->ResetProp((yyvsp[-4].string),
                                                              parser->MakeSig((IMAGE_CEE_CS_CALLCONV_PROPERTY |
                                                              ((yyvsp[-6].int32) & IMAGE_CEE_CS_CALLCONV_HASTHIS)),(yyvsp[-5].binstr),(yyvsp[-2].binstr)), (yyvsp[-7].propAttr), (yyvsp[0].binstr));}
#line 4972 "prebuilt\\asmparse.cpp"
    break;

  case 254: /* propAttr: %empty  */
#line 732 "asmparse.y"
                                                            { (yyval.propAttr) = (CorPropertyAttr) 0; }
#line 4978 "prebuilt\\asmparse.cpp"
    break;

  case 255: /* propAttr: propAttr RTSPECIALNAME_  */
#line 733 "asmparse.y"
                                                            { (yyval.propAttr) = (yyvsp[-1].propAttr); }
#line 4984 "prebuilt\\asmparse.cpp"
    break;

  case 256: /* propAttr: propAttr SPECIALNAME_  */
#line 734 "asmparse.y"
                                                            { (yyval.propAttr) = (CorPropertyAttr) ((yyvsp[-1].propAttr) | prSpecialName); }
#line 4990 "prebuilt\\asmparse.cpp"
    break;

  case 259: /* propDecl: _SET methodRef  */
#line 742 "asmparse.y"
                                                            { PASM->SetPropMethod(0, (yyvsp[0].token)); }
#line 4996 "prebuilt\\asmparse.cpp"
    break;

  case 260: /* propDecl: _GET methodRef  */
#line 743 "asmparse.y"
                                                            { PASM->SetPropMethod(1, (yyvsp[0].token)); }
#line 5002 "prebuilt\\asmparse.cpp"
    break;

  case 261: /* propDecl: _OTHER methodRef  */
#line 744 "asmparse.y"
                                                            { PASM->SetPropMethod(2, (yyvsp[0].token)); }
#line 5008 "prebuilt\\asmparse.cpp"
    break;

  case 266: /* methodHeadPart1: _METHOD  */
#line 752 "asmparse.y"
                                                            { PASM->ResetForNextMethod();
                                                              uMethodBeginLine = PASM->m_ulCurLine;
                                                              uMethodBeginColumn=PASM->m_ulCurColumn;
                                                            }
#line 5017 "prebuilt\\asmparse.cpp"
    break;

  case 267: /* marshalClause: %empty  */
#line 758 "asmparse.y"
                                                            { (yyval.binstr) = NULL; }
#line 5023 "prebuilt\\asmparse.cpp"
    break;

  case 268: /* marshalClause: MARSHAL_ '(' marshalBlob ')'  */
#line 759 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 5029 "prebuilt\\asmparse.cpp"
    break;

  case 269: /* marshalBlob: nativeType  */
#line 762 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 5035 "prebuilt\\asmparse.cpp"
    break;

  case 270: /* marshalBlob: marshalBlobHead hexbytes '}'  */
#line 763 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 5041 "prebuilt\\asmparse.cpp"
    break;

  case 271: /* marshalBlobHead: '{'  */
#line 766 "asmparse.y"
                                                             { bParsingByteArray = TRUE; }
#line 5047 "prebuilt\\asmparse.cpp"
    break;

  case 272: /* methodHead: methodHeadPart1 methAttr callConv paramAttr type marshalClause methodName typarsClause '(' sigArgs0 ')' implAttr '{'  */
#line 770 "asmparse.y"
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
#line 5065 "prebuilt\\asmparse.cpp"
    break;

  case 273: /* methAttr: %empty  */
#line 785 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) 0; }
#line 5071 "prebuilt\\asmparse.cpp"
    break;

  case 274: /* methAttr: methAttr STATIC_  */
#line 786 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdStatic); }
#line 5077 "prebuilt\\asmparse.cpp"
    break;

  case 275: /* methAttr: methAttr PUBLIC_  */
#line 787 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPublic); }
#line 5083 "prebuilt\\asmparse.cpp"
    break;

  case 276: /* methAttr: methAttr PRIVATE_  */
#line 788 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPrivate); }
#line 5089 "prebuilt\\asmparse.cpp"
    break;

  case 277: /* methAttr: methAttr FAMILY_  */
#line 789 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamily); }
#line 5095 "prebuilt\\asmparse.cpp"
    break;

  case 278: /* methAttr: methAttr FINAL_  */
#line 790 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdFinal); }
#line 5101 "prebuilt\\asmparse.cpp"
    break;

  case 279: /* methAttr: methAttr SPECIALNAME_  */
#line 791 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdSpecialName); }
#line 5107 "prebuilt\\asmparse.cpp"
    break;

  case 280: /* methAttr: methAttr VIRTUAL_  */
#line 792 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdVirtual); }
#line 5113 "prebuilt\\asmparse.cpp"
    break;

  case 281: /* methAttr: methAttr STRICT_  */
#line 793 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdCheckAccessOnOverride); }
#line 5119 "prebuilt\\asmparse.cpp"
    break;

  case 282: /* methAttr: methAttr ABSTRACT_  */
#line 794 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdAbstract); }
#line 5125 "prebuilt\\asmparse.cpp"
    break;

  case 283: /* methAttr: methAttr ASSEMBLY_  */
#line 795 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdAssem); }
#line 5131 "prebuilt\\asmparse.cpp"
    break;

  case 284: /* methAttr: methAttr FAMANDASSEM_  */
#line 796 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamANDAssem); }
#line 5137 "prebuilt\\asmparse.cpp"
    break;

  case 285: /* methAttr: methAttr FAMORASSEM_  */
#line 797 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamORAssem); }
#line 5143 "prebuilt\\asmparse.cpp"
    break;

  case 286: /* methAttr: methAttr PRIVATESCOPE_  */
#line 798 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPrivateScope); }
#line 5149 "prebuilt\\asmparse.cpp"
    break;

  case 287: /* methAttr: methAttr HIDEBYSIG_  */
#line 799 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdHideBySig); }
#line 5155 "prebuilt\\asmparse.cpp"
    break;

  case 288: /* methAttr: methAttr NEWSLOT_  */
#line 800 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdNewSlot); }
#line 5161 "prebuilt\\asmparse.cpp"
    break;

  case 289: /* methAttr: methAttr RTSPECIALNAME_  */
#line 801 "asmparse.y"
                                                            { (yyval.methAttr) = (yyvsp[-1].methAttr); }
#line 5167 "prebuilt\\asmparse.cpp"
    break;

  case 290: /* methAttr: methAttr UNMANAGEDEXP_  */
#line 802 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdUnmanagedExport); }
#line 5173 "prebuilt\\asmparse.cpp"
    break;

  case 291: /* methAttr: methAttr REQSECOBJ_  */
#line 803 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdRequireSecObject); }
#line 5179 "prebuilt\\asmparse.cpp"
    break;

  case 292: /* methAttr: methAttr FLAGS_ '(' int32 ')'  */
#line 804 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].int32)); }
#line 5185 "prebuilt\\asmparse.cpp"
    break;

  case 293: /* methAttr: methAttr PINVOKEIMPL_ '(' compQstring AS_ compQstring pinvAttr ')'  */
#line 806 "asmparse.y"
                                                            { PASM->SetPinvoke((yyvsp[-4].binstr),0,(yyvsp[-2].binstr),(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-7].methAttr) | mdPinvokeImpl); }
#line 5192 "prebuilt\\asmparse.cpp"
    break;

  case 294: /* methAttr: methAttr PINVOKEIMPL_ '(' compQstring pinvAttr ')'  */
#line 809 "asmparse.y"
                                                            { PASM->SetPinvoke((yyvsp[-2].binstr),0,NULL,(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-5].methAttr) | mdPinvokeImpl); }
#line 5199 "prebuilt\\asmparse.cpp"
    break;

  case 295: /* methAttr: methAttr PINVOKEIMPL_ '(' pinvAttr ')'  */
#line 812 "asmparse.y"
                                                            { PASM->SetPinvoke(new BinStr(),0,NULL,(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-4].methAttr) | mdPinvokeImpl); }
#line 5206 "prebuilt\\asmparse.cpp"
    break;

  case 296: /* pinvAttr: %empty  */
#line 816 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) 0; }
#line 5212 "prebuilt\\asmparse.cpp"
    break;

  case 297: /* pinvAttr: pinvAttr NOMANGLE_  */
#line 817 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmNoMangle); }
#line 5218 "prebuilt\\asmparse.cpp"
    break;

  case 298: /* pinvAttr: pinvAttr ANSI_  */
#line 818 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetAnsi); }
#line 5224 "prebuilt\\asmparse.cpp"
    break;

  case 299: /* pinvAttr: pinvAttr UNICODE_  */
#line 819 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetUnicode); }
#line 5230 "prebuilt\\asmparse.cpp"
    break;

  case 300: /* pinvAttr: pinvAttr AUTOCHAR_  */
#line 820 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetAuto); }
#line 5236 "prebuilt\\asmparse.cpp"
    break;

  case 301: /* pinvAttr: pinvAttr LASTERR_  */
#line 821 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmSupportsLastError); }
#line 5242 "prebuilt\\asmparse.cpp"
    break;

  case 302: /* pinvAttr: pinvAttr WINAPI_  */
#line 822 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvWinapi); }
#line 5248 "prebuilt\\asmparse.cpp"
    break;

  case 303: /* pinvAttr: pinvAttr CDECL_  */
#line 823 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvCdecl); }
#line 5254 "prebuilt\\asmparse.cpp"
    break;

  case 304: /* pinvAttr: pinvAttr STDCALL_  */
#line 824 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvStdcall); }
#line 5260 "prebuilt\\asmparse.cpp"
    break;

  case 305: /* pinvAttr: pinvAttr THISCALL_  */
#line 825 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvThiscall); }
#line 5266 "prebuilt\\asmparse.cpp"
    break;

  case 306: /* pinvAttr: pinvAttr FASTCALL_  */
#line 826 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvFastcall); }
#line 5272 "prebuilt\\asmparse.cpp"
    break;

  case 307: /* pinvAttr: pinvAttr BESTFIT_ ':' ON_  */
#line 827 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmBestFitEnabled); }
#line 5278 "prebuilt\\asmparse.cpp"
    break;

  case 308: /* pinvAttr: pinvAttr BESTFIT_ ':' OFF_  */
#line 828 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmBestFitDisabled); }
#line 5284 "prebuilt\\asmparse.cpp"
    break;

  case 309: /* pinvAttr: pinvAttr CHARMAPERROR_ ':' ON_  */
#line 829 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmThrowOnUnmappableCharEnabled); }
#line 5290 "prebuilt\\asmparse.cpp"
    break;

  case 310: /* pinvAttr: pinvAttr CHARMAPERROR_ ':' OFF_  */
#line 830 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmThrowOnUnmappableCharDisabled); }
#line 5296 "prebuilt\\asmparse.cpp"
    break;

  case 311: /* pinvAttr: pinvAttr FLAGS_ '(' int32 ')'  */
#line 831 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].int32)); }
#line 5302 "prebuilt\\asmparse.cpp"
    break;

  case 312: /* methodName: _CTOR  */
#line 834 "asmparse.y"
                                                            { (yyval.string) = newString(COR_CTOR_METHOD_NAME); }
#line 5308 "prebuilt\\asmparse.cpp"
    break;

  case 313: /* methodName: _CCTOR  */
#line 835 "asmparse.y"
                                                            { (yyval.string) = newString(COR_CCTOR_METHOD_NAME); }
#line 5314 "prebuilt\\asmparse.cpp"
    break;

  case 314: /* methodName: dottedName  */
#line 836 "asmparse.y"
                                                            { (yyval.string) = (yyvsp[0].string); }
#line 5320 "prebuilt\\asmparse.cpp"
    break;

  case 315: /* paramAttr: %empty  */
#line 839 "asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 5326 "prebuilt\\asmparse.cpp"
    break;

  case 316: /* paramAttr: paramAttr '[' IN_ ']'  */
#line 840 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdIn; }
#line 5332 "prebuilt\\asmparse.cpp"
    break;

  case 317: /* paramAttr: paramAttr '[' OUT_ ']'  */
#line 841 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdOut; }
#line 5338 "prebuilt\\asmparse.cpp"
    break;

  case 318: /* paramAttr: paramAttr '[' OPT_ ']'  */
#line 842 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdOptional; }
#line 5344 "prebuilt\\asmparse.cpp"
    break;

  case 319: /* paramAttr: paramAttr '[' int32 ']'  */
#line 843 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) + 1; }
#line 5350 "prebuilt\\asmparse.cpp"
    break;

  case 320: /* implAttr: %empty  */
#line 846 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (miIL | miManaged); }
#line 5356 "prebuilt\\asmparse.cpp"
    break;

  case 321: /* implAttr: implAttr NATIVE_  */
#line 847 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miNative); }
#line 5362 "prebuilt\\asmparse.cpp"
    break;

  case 322: /* implAttr: implAttr CIL_  */
#line 848 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miIL); }
#line 5368 "prebuilt\\asmparse.cpp"
    break;

  case 323: /* implAttr: implAttr OPTIL_  */
#line 849 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miOPTIL); }
#line 5374 "prebuilt\\asmparse.cpp"
    break;

  case 324: /* implAttr: implAttr MANAGED_  */
#line 850 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFFB) | miManaged); }
#line 5380 "prebuilt\\asmparse.cpp"
    break;

  case 325: /* implAttr: implAttr UNMANAGED_  */
#line 851 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFFB) | miUnmanaged); }
#line 5386 "prebuilt\\asmparse.cpp"
    break;

  case 326: /* implAttr: implAttr FORWARDREF_  */
#line 852 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miForwardRef); }
#line 5392 "prebuilt\\asmparse.cpp"
    break;

  case 327: /* implAttr: implAttr PRESERVESIG_  */
#line 853 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miPreserveSig); }
#line 5398 "prebuilt\\asmparse.cpp"
    break;

  case 328: /* implAttr: implAttr RUNTIME_  */
#line 854 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miRuntime); }
#line 5404 "prebuilt\\asmparse.cpp"
    break;

  case 329: /* implAttr: implAttr INTERNALCALL_  */
#line 855 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miInternalCall); }
#line 5410 "prebuilt\\asmparse.cpp"
    break;

  case 330: /* implAttr: implAttr SYNCHRONIZED_  */
#line 856 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miSynchronized); }
#line 5416 "prebuilt\\asmparse.cpp"
    break;

  case 331: /* implAttr: implAttr NOINLINING_  */
#line 857 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miNoInlining); }
#line 5422 "prebuilt\\asmparse.cpp"
    break;

  case 332: /* implAttr: implAttr AGGRESSIVEINLINING_  */
#line 858 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAggressiveInlining); }
#line 5428 "prebuilt\\asmparse.cpp"
    break;

  case 333: /* implAttr: implAttr NOOPTIMIZATION_  */
#line 859 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miNoOptimization); }
#line 5434 "prebuilt\\asmparse.cpp"
    break;

  case 334: /* implAttr: implAttr AGGRESSIVEOPTIMIZATION_  */
#line 860 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAggressiveOptimization); }
#line 5440 "prebuilt\\asmparse.cpp"
    break;

  case 335: /* implAttr: implAttr ASYNC_  */
#line 861 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAsync); }
#line 5446 "prebuilt\\asmparse.cpp"
    break;

  case 336: /* implAttr: implAttr FLAGS_ '(' int32 ')'  */
#line 862 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].int32)); }
#line 5452 "prebuilt\\asmparse.cpp"
    break;

  case 337: /* localsHead: _LOCALS  */
#line 865 "asmparse.y"
                                                            { PASM->delArgNameList(PASM->m_firstArgName); PASM->m_firstArgName = NULL;PASM->m_lastArgName = NULL;
                                                            }
#line 5459 "prebuilt\\asmparse.cpp"
    break;

  case 340: /* methodDecl: _EMITBYTE int32  */
#line 873 "asmparse.y"
                                                            { PASM->EmitByte((yyvsp[0].int32)); }
#line 5465 "prebuilt\\asmparse.cpp"
    break;

  case 341: /* methodDecl: sehBlock  */
#line 874 "asmparse.y"
                                                            { delete PASM->m_SEHD; PASM->m_SEHD = PASM->m_SEHDstack.POP(); }
#line 5471 "prebuilt\\asmparse.cpp"
    break;

  case 342: /* methodDecl: _MAXSTACK int32  */
#line 875 "asmparse.y"
                                                            { PASM->EmitMaxStack((yyvsp[0].int32)); }
#line 5477 "prebuilt\\asmparse.cpp"
    break;

  case 343: /* methodDecl: localsHead '(' sigArgs0 ')'  */
#line 876 "asmparse.y"
                                                            { PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, (yyvsp[-1].binstr)));
                                                            }
#line 5484 "prebuilt\\asmparse.cpp"
    break;

  case 344: /* methodDecl: localsHead INIT_ '(' sigArgs0 ')'  */
#line 878 "asmparse.y"
                                                            { PASM->EmitZeroInit();
                                                              PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, (yyvsp[-1].binstr)));
                                                            }
#line 5492 "prebuilt\\asmparse.cpp"
    break;

  case 345: /* methodDecl: _ENTRYPOINT  */
#line 881 "asmparse.y"
                                                            { PASM->EmitEntryPoint(); }
#line 5498 "prebuilt\\asmparse.cpp"
    break;

  case 346: /* methodDecl: _ZEROINIT  */
#line 882 "asmparse.y"
                                                            { PASM->EmitZeroInit(); }
#line 5504 "prebuilt\\asmparse.cpp"
    break;

  case 349: /* methodDecl: id ':'  */
#line 885 "asmparse.y"
                                                            { PASM->AddLabel(PASM->m_CurPC,(yyvsp[-1].string)); /*PASM->EmitLabel($1);*/ }
#line 5510 "prebuilt\\asmparse.cpp"
    break;

  case 355: /* methodDecl: _EXPORT '[' int32 ']'  */
#line 891 "asmparse.y"
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
#line 5525 "prebuilt\\asmparse.cpp"
    break;

  case 356: /* methodDecl: _EXPORT '[' int32 ']' AS_ id  */
#line 901 "asmparse.y"
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
#line 5540 "prebuilt\\asmparse.cpp"
    break;

  case 357: /* methodDecl: _VTENTRY int32 ':' int32  */
#line 911 "asmparse.y"
                                                            { PASM->m_pCurMethod->m_wVTEntry = (WORD)(yyvsp[-2].int32);
                                                              PASM->m_pCurMethod->m_wVTSlot = (WORD)(yyvsp[0].int32); }
#line 5547 "prebuilt\\asmparse.cpp"
    break;

  case 358: /* methodDecl: _OVERRIDE typeSpec DCOLON methodName  */
#line 914 "asmparse.y"
                                                            { PASM->AddMethodImpl((yyvsp[-2].token),(yyvsp[0].string),NULL,mdTokenNil,NULL,NULL); }
#line 5553 "prebuilt\\asmparse.cpp"
    break;

  case 359: /* methodDecl: _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'  */
#line 917 "asmparse.y"
                                                            { PASM->AddMethodImpl((yyvsp[-6].token),(yyvsp[-4].string),
                                                              ((yyvsp[-3].int32)==0 ? parser->MakeSig((yyvsp[-8].int32),(yyvsp[-7].binstr),(yyvsp[-1].binstr)) :
                                                              parser->MakeSig((yyvsp[-8].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-7].binstr),(yyvsp[-1].binstr),(yyvsp[-3].int32)))
                                                              ,mdTokenNil,NULL,NULL);
                                                              PASM->ResetArgNameList();
                                                            }
#line 5564 "prebuilt\\asmparse.cpp"
    break;

  case 361: /* methodDecl: _PARAM TYPE_ '[' int32 ']'  */
#line 924 "asmparse.y"
                                                            { if(((yyvsp[-1].int32) > 0) && ((yyvsp[-1].int32) <= (int)PASM->m_pCurMethod->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[(yyvsp[-1].int32)-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
#line 5574 "prebuilt\\asmparse.cpp"
    break;

  case 362: /* methodDecl: _PARAM TYPE_ dottedName  */
#line 929 "asmparse.y"
                                                            { int n = PASM->m_pCurMethod->FindTyPar((yyvsp[0].string));
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",(yyvsp[0].string));
                                                            }
#line 5585 "prebuilt\\asmparse.cpp"
    break;

  case 363: /* methodDecl: _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec  */
#line 935 "asmparse.y"
                                                                        { PASM->m_pCurMethod->AddGenericParamConstraint((yyvsp[-3].int32), 0, (yyvsp[0].token)); }
#line 5591 "prebuilt\\asmparse.cpp"
    break;

  case 364: /* methodDecl: _PARAM CONSTRAINT_ dottedName ',' typeSpec  */
#line 936 "asmparse.y"
                                                                        { PASM->m_pCurMethod->AddGenericParamConstraint(0, (yyvsp[-2].string), (yyvsp[0].token)); }
#line 5597 "prebuilt\\asmparse.cpp"
    break;

  case 365: /* methodDecl: _PARAM '[' int32 ']' initOpt  */
#line 939 "asmparse.y"
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
#line 5620 "prebuilt\\asmparse.cpp"
    break;

  case 366: /* scopeBlock: scopeOpen methodDecls '}'  */
#line 959 "asmparse.y"
                                                            { PASM->m_pCurMethod->CloseScope(); }
#line 5626 "prebuilt\\asmparse.cpp"
    break;

  case 367: /* scopeOpen: '{'  */
#line 962 "asmparse.y"
                                                            { PASM->m_pCurMethod->OpenScope(); }
#line 5632 "prebuilt\\asmparse.cpp"
    break;

  case 371: /* tryBlock: tryHead scopeBlock  */
#line 973 "asmparse.y"
                                                            { PASM->m_SEHD->tryTo = PASM->m_CurPC; }
#line 5638 "prebuilt\\asmparse.cpp"
    break;

  case 372: /* tryBlock: tryHead id TO_ id  */
#line 974 "asmparse.y"
                                                            { PASM->SetTryLabels((yyvsp[-2].string), (yyvsp[0].string)); }
#line 5644 "prebuilt\\asmparse.cpp"
    break;

  case 373: /* tryBlock: tryHead int32 TO_ int32  */
#line 975 "asmparse.y"
                                                            { if(PASM->m_SEHD) {PASM->m_SEHD->tryFrom = (yyvsp[-2].int32);
                                                              PASM->m_SEHD->tryTo = (yyvsp[0].int32);} }
#line 5651 "prebuilt\\asmparse.cpp"
    break;

  case 374: /* tryHead: _TRY  */
#line 979 "asmparse.y"
                                                            { PASM->NewSEHDescriptor();
                                                              PASM->m_SEHD->tryFrom = PASM->m_CurPC; }
#line 5658 "prebuilt\\asmparse.cpp"
    break;

  case 375: /* sehClause: catchClause handlerBlock  */
#line 984 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5664 "prebuilt\\asmparse.cpp"
    break;

  case 376: /* sehClause: filterClause handlerBlock  */
#line 985 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5670 "prebuilt\\asmparse.cpp"
    break;

  case 377: /* sehClause: finallyClause handlerBlock  */
#line 986 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5676 "prebuilt\\asmparse.cpp"
    break;

  case 378: /* sehClause: faultClause handlerBlock  */
#line 987 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5682 "prebuilt\\asmparse.cpp"
    break;

  case 379: /* filterClause: filterHead scopeBlock  */
#line 991 "asmparse.y"
                                                             { PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5688 "prebuilt\\asmparse.cpp"
    break;

  case 380: /* filterClause: filterHead id  */
#line 992 "asmparse.y"
                                                             { PASM->SetFilterLabel((yyvsp[0].string));
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5695 "prebuilt\\asmparse.cpp"
    break;

  case 381: /* filterClause: filterHead int32  */
#line 994 "asmparse.y"
                                                             { PASM->m_SEHD->sehFilter = (yyvsp[0].int32);
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5702 "prebuilt\\asmparse.cpp"
    break;

  case 382: /* filterHead: FILTER_  */
#line 998 "asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FILTER;
                                                               PASM->m_SEHD->sehFilter = PASM->m_CurPC; }
#line 5709 "prebuilt\\asmparse.cpp"
    break;

  case 383: /* catchClause: CATCH_ typeSpec  */
#line 1002 "asmparse.y"
                                                            {  PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_NONE;
                                                               PASM->SetCatchClass((yyvsp[0].token));
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5717 "prebuilt\\asmparse.cpp"
    break;

  case 384: /* finallyClause: FINALLY_  */
#line 1007 "asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FINALLY;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5724 "prebuilt\\asmparse.cpp"
    break;

  case 385: /* faultClause: FAULT_  */
#line 1011 "asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FAULT;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5731 "prebuilt\\asmparse.cpp"
    break;

  case 386: /* handlerBlock: scopeBlock  */
#line 1015 "asmparse.y"
                                                             { PASM->m_SEHD->sehHandlerTo = PASM->m_CurPC; }
#line 5737 "prebuilt\\asmparse.cpp"
    break;

  case 387: /* handlerBlock: HANDLER_ id TO_ id  */
#line 1016 "asmparse.y"
                                                             { PASM->SetHandlerLabels((yyvsp[-2].string), (yyvsp[0].string)); }
#line 5743 "prebuilt\\asmparse.cpp"
    break;

  case 388: /* handlerBlock: HANDLER_ int32 TO_ int32  */
#line 1017 "asmparse.y"
                                                             { PASM->m_SEHD->sehHandler = (yyvsp[-2].int32);
                                                               PASM->m_SEHD->sehHandlerTo = (yyvsp[0].int32); }
#line 5750 "prebuilt\\asmparse.cpp"
    break;

  case 390: /* ddHead: _DATA tls id '='  */
#line 1025 "asmparse.y"
                                                             { PASM->EmitDataLabel((yyvsp[-1].string)); }
#line 5756 "prebuilt\\asmparse.cpp"
    break;

  case 392: /* tls: %empty  */
#line 1029 "asmparse.y"
                                                             { PASM->SetDataSection(); }
#line 5762 "prebuilt\\asmparse.cpp"
    break;

  case 393: /* tls: TLS_  */
#line 1030 "asmparse.y"
                                                             { PASM->SetTLSSection(); }
#line 5768 "prebuilt\\asmparse.cpp"
    break;

  case 394: /* tls: CIL_  */
#line 1031 "asmparse.y"
                                                             { PASM->SetILSection(); }
#line 5774 "prebuilt\\asmparse.cpp"
    break;

  case 399: /* ddItemCount: %empty  */
#line 1042 "asmparse.y"
                                                             { (yyval.int32) = 1; }
#line 5780 "prebuilt\\asmparse.cpp"
    break;

  case 400: /* ddItemCount: '[' int32 ']'  */
#line 1043 "asmparse.y"
                                                             { (yyval.int32) = (yyvsp[-1].int32);
                                                               if((yyvsp[-1].int32) <= 0) { PASM->report->error("Illegal item count: %d\n",(yyvsp[-1].int32));
                                                                  if(!PASM->OnErrGo) (yyval.int32) = 1; }}
#line 5788 "prebuilt\\asmparse.cpp"
    break;

  case 401: /* ddItem: CHAR_ '*' '(' compQstring ')'  */
#line 1048 "asmparse.y"
                                                             { PASM->EmitDataString((yyvsp[-1].binstr)); }
#line 5794 "prebuilt\\asmparse.cpp"
    break;

  case 402: /* ddItem: '&' '(' id ')'  */
#line 1049 "asmparse.y"
                                                             { PASM->EmitDD((yyvsp[-1].string)); }
#line 5800 "prebuilt\\asmparse.cpp"
    break;

  case 403: /* ddItem: bytearrayhead bytes ')'  */
#line 1050 "asmparse.y"
                                                             { PASM->EmitData((yyvsp[-1].binstr)->ptr(),(yyvsp[-1].binstr)->length()); }
#line 5806 "prebuilt\\asmparse.cpp"
    break;

  case 404: /* ddItem: FLOAT32_ '(' float64 ')' ddItemCount  */
#line 1052 "asmparse.y"
                                                             { float f = (float) (*(yyvsp[-2].float64)); float* p = new (nothrow) float[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i < (yyvsp[0].int32); i++) p[i] = f;
                                                                 PASM->EmitData(p, sizeof(float)*(yyvsp[0].int32)); delete (yyvsp[-2].float64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(float)*(yyvsp[0].int32)); }
#line 5817 "prebuilt\\asmparse.cpp"
    break;

  case 405: /* ddItem: FLOAT64_ '(' float64 ')' ddItemCount  */
#line 1059 "asmparse.y"
                                                             { double* p = new (nothrow) double[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = *((yyvsp[-2].float64));
                                                                 PASM->EmitData(p, sizeof(double)*(yyvsp[0].int32)); delete (yyvsp[-2].float64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(double)*(yyvsp[0].int32)); }
#line 5828 "prebuilt\\asmparse.cpp"
    break;

  case 406: /* ddItem: INT64_ '(' int64 ')' ddItemCount  */
#line 1066 "asmparse.y"
                                                             { int64_t* p = new (nothrow) int64_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = *((yyvsp[-2].int64));
                                                                 PASM->EmitData(p, sizeof(int64_t)*(yyvsp[0].int32)); delete (yyvsp[-2].int64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int64_t)*(yyvsp[0].int32)); }
#line 5839 "prebuilt\\asmparse.cpp"
    break;

  case 407: /* ddItem: INT32_ '(' int32 ')' ddItemCount  */
#line 1073 "asmparse.y"
                                                             { int32_t* p = new (nothrow) int32_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = (yyvsp[-2].int32);
                                                                 PASM->EmitData(p, sizeof(int32_t)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int32_t)*(yyvsp[0].int32)); }
#line 5850 "prebuilt\\asmparse.cpp"
    break;

  case 408: /* ddItem: INT16_ '(' int32 ')' ddItemCount  */
#line 1080 "asmparse.y"
                                                             { int16_t i = (int16_t) (yyvsp[-2].int32); FAIL_UNLESS(i == (yyvsp[-2].int32), ("Value %d too big\n", (yyvsp[-2].int32)));
                                                               int16_t* p = new (nothrow) int16_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<(yyvsp[0].int32); j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(int16_t)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int16_t)*(yyvsp[0].int32)); }
#line 5862 "prebuilt\\asmparse.cpp"
    break;

  case 409: /* ddItem: INT8_ '(' int32 ')' ddItemCount  */
#line 1088 "asmparse.y"
                                                             { int8_t i = (int8_t) (yyvsp[-2].int32); FAIL_UNLESS(i == (yyvsp[-2].int32), ("Value %d too big\n", (yyvsp[-2].int32)));
                                                               int8_t* p = new (nothrow) int8_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<(yyvsp[0].int32); j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(int8_t)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int8_t)*(yyvsp[0].int32)); }
#line 5874 "prebuilt\\asmparse.cpp"
    break;

  case 410: /* ddItem: FLOAT32_ ddItemCount  */
#line 1095 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(float)*(yyvsp[0].int32)); }
#line 5880 "prebuilt\\asmparse.cpp"
    break;

  case 411: /* ddItem: FLOAT64_ ddItemCount  */
#line 1096 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(double)*(yyvsp[0].int32)); }
#line 5886 "prebuilt\\asmparse.cpp"
    break;

  case 412: /* ddItem: INT64_ ddItemCount  */
#line 1097 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int64_t)*(yyvsp[0].int32)); }
#line 5892 "prebuilt\\asmparse.cpp"
    break;

  case 413: /* ddItem: INT32_ ddItemCount  */
#line 1098 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int32_t)*(yyvsp[0].int32)); }
#line 5898 "prebuilt\\asmparse.cpp"
    break;

  case 414: /* ddItem: INT16_ ddItemCount  */
#line 1099 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int16_t)*(yyvsp[0].int32)); }
#line 5904 "prebuilt\\asmparse.cpp"
    break;

  case 415: /* ddItem: INT8_ ddItemCount  */
#line 1100 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int8_t)*(yyvsp[0].int32)); }
#line 5910 "prebuilt\\asmparse.cpp"
    break;

  case 416: /* fieldSerInit: FLOAT32_ '(' float64 ')'  */
#line 1104 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               float f = (float)(*(yyvsp[-1].float64));
                                                               (yyval.binstr)->appendInt32(*((int32_t*)&f)); delete (yyvsp[-1].float64); }
#line 5918 "prebuilt\\asmparse.cpp"
    break;

  case 417: /* fieldSerInit: FLOAT64_ '(' float64 ')'  */
#line 1107 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].float64)); delete (yyvsp[-1].float64); }
#line 5925 "prebuilt\\asmparse.cpp"
    break;

  case 418: /* fieldSerInit: FLOAT32_ '(' int32 ')'  */
#line 1109 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 5932 "prebuilt\\asmparse.cpp"
    break;

  case 419: /* fieldSerInit: FLOAT64_ '(' int64 ')'  */
#line 1111 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 5939 "prebuilt\\asmparse.cpp"
    break;

  case 420: /* fieldSerInit: INT64_ '(' int64 ')'  */
#line 1113 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 5946 "prebuilt\\asmparse.cpp"
    break;

  case 421: /* fieldSerInit: INT32_ '(' int32 ')'  */
#line 1115 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 5953 "prebuilt\\asmparse.cpp"
    break;

  case 422: /* fieldSerInit: INT16_ '(' int32 ')'  */
#line 1117 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 5960 "prebuilt\\asmparse.cpp"
    break;

  case 423: /* fieldSerInit: INT8_ '(' int32 ')'  */
#line 1119 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 5967 "prebuilt\\asmparse.cpp"
    break;

  case 424: /* fieldSerInit: UNSIGNED_ INT64_ '(' int64 ')'  */
#line 1121 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 5974 "prebuilt\\asmparse.cpp"
    break;

  case 425: /* fieldSerInit: UNSIGNED_ INT32_ '(' int32 ')'  */
#line 1123 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 5981 "prebuilt\\asmparse.cpp"
    break;

  case 426: /* fieldSerInit: UNSIGNED_ INT16_ '(' int32 ')'  */
#line 1125 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 5988 "prebuilt\\asmparse.cpp"
    break;

  case 427: /* fieldSerInit: UNSIGNED_ INT8_ '(' int32 ')'  */
#line 1127 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 5995 "prebuilt\\asmparse.cpp"
    break;

  case 428: /* fieldSerInit: UINT64_ '(' int64 ')'  */
#line 1129 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6002 "prebuilt\\asmparse.cpp"
    break;

  case 429: /* fieldSerInit: UINT32_ '(' int32 ')'  */
#line 1131 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6009 "prebuilt\\asmparse.cpp"
    break;

  case 430: /* fieldSerInit: UINT16_ '(' int32 ')'  */
#line 1133 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6016 "prebuilt\\asmparse.cpp"
    break;

  case 431: /* fieldSerInit: UINT8_ '(' int32 ')'  */
#line 1135 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6023 "prebuilt\\asmparse.cpp"
    break;

  case 432: /* fieldSerInit: CHAR_ '(' int32 ')'  */
#line 1137 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CHAR);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6030 "prebuilt\\asmparse.cpp"
    break;

  case 433: /* fieldSerInit: BOOL_ '(' truefalse ')'  */
#line 1139 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_BOOLEAN);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32));}
#line 6037 "prebuilt\\asmparse.cpp"
    break;

  case 434: /* fieldSerInit: bytearrayhead bytes ')'  */
#line 1141 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING);
                                                               (yyval.binstr)->append((yyvsp[-1].binstr)); delete (yyvsp[-1].binstr);}
#line 6044 "prebuilt\\asmparse.cpp"
    break;

  case 435: /* bytearrayhead: BYTEARRAY_ '('  */
#line 1145 "asmparse.y"
                                                             { bParsingByteArray = TRUE; }
#line 6050 "prebuilt\\asmparse.cpp"
    break;

  case 436: /* bytes: %empty  */
#line 1148 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6056 "prebuilt\\asmparse.cpp"
    break;

  case 437: /* bytes: hexbytes  */
#line 1149 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6062 "prebuilt\\asmparse.cpp"
    break;

  case 438: /* hexbytes: HEXBYTE  */
#line 1152 "asmparse.y"
                                                             { int8_t i = (int8_t) (yyvsp[0].int32); (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(i); }
#line 6068 "prebuilt\\asmparse.cpp"
    break;

  case 439: /* hexbytes: hexbytes HEXBYTE  */
#line 1153 "asmparse.y"
                                                             { int8_t i = (int8_t) (yyvsp[0].int32); (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(i); }
#line 6074 "prebuilt\\asmparse.cpp"
    break;

  case 440: /* fieldInit: fieldSerInit  */
#line 1157 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6080 "prebuilt\\asmparse.cpp"
    break;

  case 441: /* fieldInit: compQstring  */
#line 1158 "asmparse.y"
                                                             { (yyval.binstr) = BinStrToUnicode((yyvsp[0].binstr),true); (yyval.binstr)->insertInt8(ELEMENT_TYPE_STRING);}
#line 6086 "prebuilt\\asmparse.cpp"
    break;

  case 442: /* fieldInit: NULLREF_  */
#line 1159 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CLASS);
                                                               (yyval.binstr)->appendInt32(0); }
#line 6093 "prebuilt\\asmparse.cpp"
    break;

  case 443: /* serInit: fieldSerInit  */
#line 1164 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6099 "prebuilt\\asmparse.cpp"
    break;

  case 444: /* serInit: STRING_ '(' NULLREF_ ')'  */
#line 1165 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); (yyval.binstr)->appendInt8(0xFF); }
#line 6105 "prebuilt\\asmparse.cpp"
    break;

  case 445: /* serInit: STRING_ '(' SQSTRING ')'  */
#line 1166 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[-1].string)); delete [] (yyvsp[-1].string);}
#line 6112 "prebuilt\\asmparse.cpp"
    break;

  case 446: /* serInit: TYPE_ '(' CLASS_ SQSTRING ')'  */
#line 1168 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[-1].string)); delete [] (yyvsp[-1].string);}
#line 6119 "prebuilt\\asmparse.cpp"
    break;

  case 447: /* serInit: TYPE_ '(' className ')'  */
#line 1170 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[-1].token)));}
#line 6126 "prebuilt\\asmparse.cpp"
    break;

  case 448: /* serInit: TYPE_ '(' NULLREF_ ')'  */
#line 1172 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE); (yyval.binstr)->appendInt8(0xFF); }
#line 6132 "prebuilt\\asmparse.cpp"
    break;

  case 449: /* serInit: OBJECT_ '(' serInit ')'  */
#line 1173 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);}
#line 6138 "prebuilt\\asmparse.cpp"
    break;

  case 450: /* serInit: FLOAT32_ '[' int32 ']' '(' f32seq ')'  */
#line 1175 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_R4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6146 "prebuilt\\asmparse.cpp"
    break;

  case 451: /* serInit: FLOAT64_ '[' int32 ']' '(' f64seq ')'  */
#line 1179 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6154 "prebuilt\\asmparse.cpp"
    break;

  case 452: /* serInit: INT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1183 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6162 "prebuilt\\asmparse.cpp"
    break;

  case 453: /* serInit: INT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1187 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6170 "prebuilt\\asmparse.cpp"
    break;

  case 454: /* serInit: INT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1191 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6178 "prebuilt\\asmparse.cpp"
    break;

  case 455: /* serInit: INT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1195 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6186 "prebuilt\\asmparse.cpp"
    break;

  case 456: /* serInit: UINT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1199 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6194 "prebuilt\\asmparse.cpp"
    break;

  case 457: /* serInit: UINT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1203 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6202 "prebuilt\\asmparse.cpp"
    break;

  case 458: /* serInit: UINT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1207 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6210 "prebuilt\\asmparse.cpp"
    break;

  case 459: /* serInit: UINT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1211 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6218 "prebuilt\\asmparse.cpp"
    break;

  case 460: /* serInit: UNSIGNED_ INT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1215 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6226 "prebuilt\\asmparse.cpp"
    break;

  case 461: /* serInit: UNSIGNED_ INT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1219 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6234 "prebuilt\\asmparse.cpp"
    break;

  case 462: /* serInit: UNSIGNED_ INT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1223 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6242 "prebuilt\\asmparse.cpp"
    break;

  case 463: /* serInit: UNSIGNED_ INT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1227 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6250 "prebuilt\\asmparse.cpp"
    break;

  case 464: /* serInit: CHAR_ '[' int32 ']' '(' i16seq ')'  */
#line 1231 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_CHAR);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6258 "prebuilt\\asmparse.cpp"
    break;

  case 465: /* serInit: BOOL_ '[' int32 ']' '(' boolSeq ')'  */
#line 1235 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_BOOLEAN);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6266 "prebuilt\\asmparse.cpp"
    break;

  case 466: /* serInit: STRING_ '[' int32 ']' '(' sqstringSeq ')'  */
#line 1239 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_STRING);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6274 "prebuilt\\asmparse.cpp"
    break;

  case 467: /* serInit: TYPE_ '[' int32 ']' '(' classSeq ')'  */
#line 1243 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TYPE);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6282 "prebuilt\\asmparse.cpp"
    break;

  case 468: /* serInit: OBJECT_ '[' int32 ']' '(' objSeq ')'  */
#line 1247 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6290 "prebuilt\\asmparse.cpp"
    break;

  case 469: /* f32seq: %empty  */
#line 1253 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6296 "prebuilt\\asmparse.cpp"
    break;

  case 470: /* f32seq: f32seq float64  */
#line 1254 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               float f = (float) (*(yyvsp[0].float64)); (yyval.binstr)->appendInt32(*((int32_t*)&f)); delete (yyvsp[0].float64); }
#line 6303 "prebuilt\\asmparse.cpp"
    break;

  case 471: /* f32seq: f32seq int32  */
#line 1256 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 6310 "prebuilt\\asmparse.cpp"
    break;

  case 472: /* f64seq: %empty  */
#line 1260 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6316 "prebuilt\\asmparse.cpp"
    break;

  case 473: /* f64seq: f64seq float64  */
#line 1261 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[0].float64)); delete (yyvsp[0].float64); }
#line 6323 "prebuilt\\asmparse.cpp"
    break;

  case 474: /* f64seq: f64seq int64  */
#line 1263 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[0].int64)); delete (yyvsp[0].int64); }
#line 6330 "prebuilt\\asmparse.cpp"
    break;

  case 475: /* i64seq: %empty  */
#line 1267 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6336 "prebuilt\\asmparse.cpp"
    break;

  case 476: /* i64seq: i64seq int64  */
#line 1268 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[0].int64)); delete (yyvsp[0].int64); }
#line 6343 "prebuilt\\asmparse.cpp"
    break;

  case 477: /* i32seq: %empty  */
#line 1272 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6349 "prebuilt\\asmparse.cpp"
    break;

  case 478: /* i32seq: i32seq int32  */
#line 1273 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt32((yyvsp[0].int32));}
#line 6355 "prebuilt\\asmparse.cpp"
    break;

  case 479: /* i16seq: %empty  */
#line 1276 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6361 "prebuilt\\asmparse.cpp"
    break;

  case 480: /* i16seq: i16seq int32  */
#line 1277 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt16((yyvsp[0].int32));}
#line 6367 "prebuilt\\asmparse.cpp"
    break;

  case 481: /* i8seq: %empty  */
#line 1280 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6373 "prebuilt\\asmparse.cpp"
    break;

  case 482: /* i8seq: i8seq int32  */
#line 1281 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8((yyvsp[0].int32)); }
#line 6379 "prebuilt\\asmparse.cpp"
    break;

  case 483: /* boolSeq: %empty  */
#line 1284 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6385 "prebuilt\\asmparse.cpp"
    break;

  case 484: /* boolSeq: boolSeq truefalse  */
#line 1285 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt8((yyvsp[0].int32));}
#line 6392 "prebuilt\\asmparse.cpp"
    break;

  case 485: /* sqstringSeq: %empty  */
#line 1289 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6398 "prebuilt\\asmparse.cpp"
    break;

  case 486: /* sqstringSeq: sqstringSeq NULLREF_  */
#line 1290 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(0xFF); }
#line 6404 "prebuilt\\asmparse.cpp"
    break;

  case 487: /* sqstringSeq: sqstringSeq SQSTRING  */
#line 1291 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); delete [] (yyvsp[0].string);}
#line 6411 "prebuilt\\asmparse.cpp"
    break;

  case 488: /* classSeq: %empty  */
#line 1295 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6417 "prebuilt\\asmparse.cpp"
    break;

  case 489: /* classSeq: classSeq NULLREF_  */
#line 1296 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(0xFF); }
#line 6423 "prebuilt\\asmparse.cpp"
    break;

  case 490: /* classSeq: classSeq CLASS_ SQSTRING  */
#line 1297 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); delete [] (yyvsp[0].string);}
#line 6430 "prebuilt\\asmparse.cpp"
    break;

  case 491: /* classSeq: classSeq className  */
#line 1299 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[0].token)));}
#line 6437 "prebuilt\\asmparse.cpp"
    break;

  case 492: /* objSeq: %empty  */
#line 1303 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6443 "prebuilt\\asmparse.cpp"
    break;

  case 493: /* objSeq: objSeq serInit  */
#line 1304 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 6449 "prebuilt\\asmparse.cpp"
    break;

  case 494: /* methodSpec: METHOD_  */
#line 1308 "asmparse.y"
                                                             { parser->m_ANSFirst.PUSH(PASM->m_firstArgName);
                                                               parser->m_ANSLast.PUSH(PASM->m_lastArgName);
                                                               PASM->m_firstArgName = NULL;
                                                               PASM->m_lastArgName = NULL; }
#line 6458 "prebuilt\\asmparse.cpp"
    break;

  case 495: /* instr_none: INSTR_NONE  */
#line 1314 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6464 "prebuilt\\asmparse.cpp"
    break;

  case 496: /* instr_var: INSTR_VAR  */
#line 1317 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6470 "prebuilt\\asmparse.cpp"
    break;

  case 497: /* instr_i: INSTR_I  */
#line 1320 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6476 "prebuilt\\asmparse.cpp"
    break;

  case 498: /* instr_i8: INSTR_I8  */
#line 1323 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6482 "prebuilt\\asmparse.cpp"
    break;

  case 499: /* instr_r: INSTR_R  */
#line 1326 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6488 "prebuilt\\asmparse.cpp"
    break;

  case 500: /* instr_brtarget: INSTR_BRTARGET  */
#line 1329 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6494 "prebuilt\\asmparse.cpp"
    break;

  case 501: /* instr_method: INSTR_METHOD  */
#line 1332 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode));
                                                               if((!PASM->OnErrGo)&&
                                                               (((yyvsp[0].opcode) == CEE_NEWOBJ)||
                                                                ((yyvsp[0].opcode) == CEE_CALLVIRT)))
                                                                  iCallConv = IMAGE_CEE_CS_CALLCONV_HASTHIS;
                                                             }
#line 6505 "prebuilt\\asmparse.cpp"
    break;

  case 502: /* instr_field: INSTR_FIELD  */
#line 1340 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6511 "prebuilt\\asmparse.cpp"
    break;

  case 503: /* instr_type: INSTR_TYPE  */
#line 1343 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6517 "prebuilt\\asmparse.cpp"
    break;

  case 504: /* instr_string: INSTR_STRING  */
#line 1346 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6523 "prebuilt\\asmparse.cpp"
    break;

  case 505: /* instr_sig: INSTR_SIG  */
#line 1349 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6529 "prebuilt\\asmparse.cpp"
    break;

  case 506: /* instr_tok: INSTR_TOK  */
#line 1352 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); iOpcodeLen = PASM->OpcodeLen((yyval.instr)); }
#line 6535 "prebuilt\\asmparse.cpp"
    break;

  case 507: /* instr_switch: INSTR_SWITCH  */
#line 1355 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6541 "prebuilt\\asmparse.cpp"
    break;

  case 508: /* instr_r_head: instr_r '('  */
#line 1358 "asmparse.y"
                                                             { (yyval.instr) = (yyvsp[-1].instr); bParsingByteArray = TRUE; }
#line 6547 "prebuilt\\asmparse.cpp"
    break;

  case 509: /* instr: instr_none  */
#line 1362 "asmparse.y"
                                                             { PASM->EmitOpcode((yyvsp[0].instr)); }
#line 6553 "prebuilt\\asmparse.cpp"
    break;

  case 510: /* instr: instr_var int32  */
#line 1363 "asmparse.y"
                                                             { PASM->EmitInstrVar((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 6559 "prebuilt\\asmparse.cpp"
    break;

  case 511: /* instr: instr_var id  */
#line 1364 "asmparse.y"
                                                             { PASM->EmitInstrVarByName((yyvsp[-1].instr), (yyvsp[0].string)); }
#line 6565 "prebuilt\\asmparse.cpp"
    break;

  case 512: /* instr: instr_i int32  */
#line 1365 "asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 6571 "prebuilt\\asmparse.cpp"
    break;

  case 513: /* instr: instr_i8 int64  */
#line 1366 "asmparse.y"
                                                             { PASM->EmitInstrI8((yyvsp[-1].instr), (yyvsp[0].int64)); }
#line 6577 "prebuilt\\asmparse.cpp"
    break;

  case 514: /* instr: instr_r float64  */
#line 1367 "asmparse.y"
                                                             { PASM->EmitInstrR((yyvsp[-1].instr), (yyvsp[0].float64)); delete ((yyvsp[0].float64));}
#line 6583 "prebuilt\\asmparse.cpp"
    break;

  case 515: /* instr: instr_r int64  */
#line 1368 "asmparse.y"
                                                             { double f = (double) (*(yyvsp[0].int64)); PASM->EmitInstrR((yyvsp[-1].instr), &f); }
#line 6589 "prebuilt\\asmparse.cpp"
    break;

  case 516: /* instr: instr_r_head bytes ')'  */
#line 1369 "asmparse.y"
                                                             { unsigned L = (yyvsp[-1].binstr)->length();
                                                               FAIL_UNLESS(L >= sizeof(float), ("%d hexbytes, must be at least %d\n",
                                                                           L,sizeof(float)));
                                                               if(L < sizeof(float)) {YYERROR; }
                                                               else {
                                                                   double f = (L >= sizeof(double)) ? *((double *)((yyvsp[-1].binstr)->ptr()))
                                                                                    : (double)(*(float *)((yyvsp[-1].binstr)->ptr()));
                                                                   PASM->EmitInstrR((yyvsp[-2].instr),&f); }
                                                               delete (yyvsp[-1].binstr); }
#line 6603 "prebuilt\\asmparse.cpp"
    break;

  case 517: /* instr: instr_brtarget int32  */
#line 1378 "asmparse.y"
                                                             { PASM->EmitInstrBrOffset((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 6609 "prebuilt\\asmparse.cpp"
    break;

  case 518: /* instr: instr_brtarget id  */
#line 1379 "asmparse.y"
                                                             { PASM->EmitInstrBrTarget((yyvsp[-1].instr), (yyvsp[0].string)); }
#line 6615 "prebuilt\\asmparse.cpp"
    break;

  case 519: /* instr: instr_method methodRef  */
#line 1381 "asmparse.y"
                                                             { PASM->SetMemberRefFixup((yyvsp[0].token),PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),(yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iCallConv = 0;
                                                             }
#line 6626 "prebuilt\\asmparse.cpp"
    break;

  case 520: /* instr: instr_field type typeSpec DCOLON dottedName  */
#line 1388 "asmparse.y"
                                                             { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef((yyvsp[-2].token), (yyvsp[0].string), (yyvsp[-3].binstr));
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-4].instr)));
                                                               PASM->EmitInstrI((yyvsp[-4].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6638 "prebuilt\\asmparse.cpp"
    break;

  case 521: /* instr: instr_field type dottedName  */
#line 1396 "asmparse.y"
                                                             { (yyvsp[-1].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef(mdTokenNil, (yyvsp[0].string), (yyvsp[-1].binstr));
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-2].instr)));
                                                               PASM->EmitInstrI((yyvsp[-2].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6650 "prebuilt\\asmparse.cpp"
    break;

  case 522: /* instr: instr_field mdtoken  */
#line 1403 "asmparse.y"
                                                             { mdToken mr = (yyvsp[0].token);
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6661 "prebuilt\\asmparse.cpp"
    break;

  case 523: /* instr: instr_field TYPEDEF_F  */
#line 1409 "asmparse.y"
                                                             { mdToken mr = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6672 "prebuilt\\asmparse.cpp"
    break;

  case 524: /* instr: instr_field TYPEDEF_MR  */
#line 1415 "asmparse.y"
                                                             { mdToken mr = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6683 "prebuilt\\asmparse.cpp"
    break;

  case 525: /* instr: instr_type typeSpec  */
#line 1421 "asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr), (yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6692 "prebuilt\\asmparse.cpp"
    break;

  case 526: /* instr: instr_string compQstring  */
#line 1425 "asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-1].instr), (yyvsp[0].binstr),TRUE); }
#line 6698 "prebuilt\\asmparse.cpp"
    break;

  case 527: /* instr: instr_string ANSI_ '(' compQstring ')'  */
#line 1427 "asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-4].instr), (yyvsp[-1].binstr),FALSE); }
#line 6704 "prebuilt\\asmparse.cpp"
    break;

  case 528: /* instr: instr_string bytearrayhead bytes ')'  */
#line 1429 "asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-3].instr), (yyvsp[-1].binstr),FALSE,TRUE); }
#line 6710 "prebuilt\\asmparse.cpp"
    break;

  case 529: /* instr: instr_sig callConv type '(' sigArgs0 ')'  */
#line 1431 "asmparse.y"
                                                             { PASM->EmitInstrSig((yyvsp[-5].instr), parser->MakeSig((yyvsp[-4].int32), (yyvsp[-3].binstr), (yyvsp[-1].binstr)));
                                                               PASM->ResetArgNameList();
                                                             }
#line 6718 "prebuilt\\asmparse.cpp"
    break;

  case 530: /* instr: instr_tok ownerType  */
#line 1435 "asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr),(yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iOpcodeLen = 0;
                                                             }
#line 6728 "prebuilt\\asmparse.cpp"
    break;

  case 531: /* instr: instr_switch '(' labels ')'  */
#line 1440 "asmparse.y"
                                                             { PASM->EmitInstrSwitch((yyvsp[-3].instr), (yyvsp[-1].labels)); }
#line 6734 "prebuilt\\asmparse.cpp"
    break;

  case 532: /* labels: %empty  */
#line 1443 "asmparse.y"
                                                              { (yyval.labels) = 0; }
#line 6740 "prebuilt\\asmparse.cpp"
    break;

  case 533: /* labels: id ',' labels  */
#line 1444 "asmparse.y"
                                                              { (yyval.labels) = new Labels((yyvsp[-2].string), (yyvsp[0].labels), TRUE); }
#line 6746 "prebuilt\\asmparse.cpp"
    break;

  case 534: /* labels: int32 ',' labels  */
#line 1445 "asmparse.y"
                                                              { (yyval.labels) = new Labels((char *)(UINT_PTR)(yyvsp[-2].int32), (yyvsp[0].labels), FALSE); }
#line 6752 "prebuilt\\asmparse.cpp"
    break;

  case 535: /* labels: id  */
#line 1446 "asmparse.y"
                                                              { (yyval.labels) = new Labels((yyvsp[0].string), NULL, TRUE); }
#line 6758 "prebuilt\\asmparse.cpp"
    break;

  case 536: /* labels: int32  */
#line 1447 "asmparse.y"
                                                              { (yyval.labels) = new Labels((char *)(UINT_PTR)(yyvsp[0].int32), NULL, FALSE); }
#line 6764 "prebuilt\\asmparse.cpp"
    break;

  case 537: /* tyArgs0: %empty  */
#line 1451 "asmparse.y"
                                                             { (yyval.binstr) = NULL; }
#line 6770 "prebuilt\\asmparse.cpp"
    break;

  case 538: /* tyArgs0: '<' tyArgs1 '>'  */
#line 1452 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 6776 "prebuilt\\asmparse.cpp"
    break;

  case 539: /* tyArgs1: %empty  */
#line 1455 "asmparse.y"
                                                             { (yyval.binstr) = NULL; }
#line 6782 "prebuilt\\asmparse.cpp"
    break;

  case 540: /* tyArgs1: tyArgs2  */
#line 1456 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6788 "prebuilt\\asmparse.cpp"
    break;

  case 541: /* tyArgs2: type  */
#line 1459 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6794 "prebuilt\\asmparse.cpp"
    break;

  case 542: /* tyArgs2: tyArgs2 ',' type  */
#line 1460 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 6800 "prebuilt\\asmparse.cpp"
    break;

  case 543: /* sigArgs0: %empty  */
#line 1464 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6806 "prebuilt\\asmparse.cpp"
    break;

  case 544: /* sigArgs0: sigArgs1  */
#line 1465 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr);}
#line 6812 "prebuilt\\asmparse.cpp"
    break;

  case 545: /* sigArgs1: sigArg  */
#line 1468 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6818 "prebuilt\\asmparse.cpp"
    break;

  case 546: /* sigArgs1: sigArgs1 ',' sigArg  */
#line 1469 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 6824 "prebuilt\\asmparse.cpp"
    break;

  case 547: /* sigArg: ELLIPSIS  */
#line 1472 "asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_SENTINEL); }
#line 6830 "prebuilt\\asmparse.cpp"
    break;

  case 548: /* sigArg: paramAttr type marshalClause  */
#line 1473 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[-1].binstr)); PASM->addArgName(NULL, (yyvsp[-1].binstr), (yyvsp[0].binstr), (yyvsp[-2].int32)); }
#line 6836 "prebuilt\\asmparse.cpp"
    break;

  case 549: /* sigArg: paramAttr type marshalClause id  */
#line 1474 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[-2].binstr)); PASM->addArgName((yyvsp[0].string), (yyvsp[-2].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32));}
#line 6842 "prebuilt\\asmparse.cpp"
    break;

  case 550: /* className: '[' dottedName ']' slashedName  */
#line 1478 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(PASM->GetAsmRef((yyvsp[-2].string)), (yyvsp[0].string), NULL); delete[] (yyvsp[-2].string);}
#line 6848 "prebuilt\\asmparse.cpp"
    break;

  case 551: /* className: '[' mdtoken ']' slashedName  */
#line 1479 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef((yyvsp[-2].token), (yyvsp[0].string), NULL); }
#line 6854 "prebuilt\\asmparse.cpp"
    break;

  case 552: /* className: '[' '*' ']' slashedName  */
#line 1480 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(mdTokenNil, (yyvsp[0].string), NULL); }
#line 6860 "prebuilt\\asmparse.cpp"
    break;

  case 553: /* className: '[' _MODULE dottedName ']' slashedName  */
#line 1481 "asmparse.y"
                                                                   { (yyval.token) = PASM->ResolveClassRef(PASM->GetModRef((yyvsp[-2].string)),(yyvsp[0].string), NULL); delete[] (yyvsp[-2].string);}
#line 6866 "prebuilt\\asmparse.cpp"
    break;

  case 554: /* className: slashedName  */
#line 1482 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(1,(yyvsp[0].string),NULL); }
#line 6872 "prebuilt\\asmparse.cpp"
    break;

  case 555: /* className: mdtoken  */
#line 1483 "asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].token); }
#line 6878 "prebuilt\\asmparse.cpp"
    break;

  case 556: /* className: TYPEDEF_T  */
#line 1484 "asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 6884 "prebuilt\\asmparse.cpp"
    break;

  case 557: /* className: _THIS  */
#line 1485 "asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) (yyval.token) = PASM->m_pCurClass->m_cl;
                                                                else { (yyval.token) = 0; PASM->report->error(".this outside class scope\n"); }
                                                              }
#line 6892 "prebuilt\\asmparse.cpp"
    break;

  case 558: /* className: _BASE  */
#line 1488 "asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) {
                                                                  (yyval.token) = PASM->m_pCurClass->m_crExtends;
                                                                  if(RidFromToken((yyval.token)) == 0)
                                                                    PASM->report->error(".base undefined\n");
                                                                } else { (yyval.token) = 0; PASM->report->error(".base outside class scope\n"); }
                                                              }
#line 6903 "prebuilt\\asmparse.cpp"
    break;

  case 559: /* className: _NESTER  */
#line 1494 "asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) {
                                                                  if(PASM->m_pCurClass->m_pEncloser != NULL) (yyval.token) = PASM->m_pCurClass->m_pEncloser->m_cl;
                                                                  else { (yyval.token) = 0; PASM->report->error(".nester undefined\n"); }
                                                                } else { (yyval.token) = 0; PASM->report->error(".nester outside class scope\n"); }
                                                              }
#line 6913 "prebuilt\\asmparse.cpp"
    break;

  case 560: /* slashedName: dottedName  */
#line 1501 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 6919 "prebuilt\\asmparse.cpp"
    break;

  case 561: /* slashedName: slashedName '/' dottedName  */
#line 1502 "asmparse.y"
                                                              { (yyval.string) = newStringWDel((yyvsp[-2].string), NESTING_SEP, (yyvsp[0].string)); }
#line 6925 "prebuilt\\asmparse.cpp"
    break;

  case 562: /* typeSpec: className  */
#line 1505 "asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].token);}
#line 6931 "prebuilt\\asmparse.cpp"
    break;

  case 563: /* typeSpec: '[' dottedName ']'  */
#line 1506 "asmparse.y"
                                                              { (yyval.token) = PASM->GetAsmRef((yyvsp[-1].string)); delete[] (yyvsp[-1].string);}
#line 6937 "prebuilt\\asmparse.cpp"
    break;

  case 564: /* typeSpec: '[' _MODULE dottedName ']'  */
#line 1507 "asmparse.y"
                                                              { (yyval.token) = PASM->GetModRef((yyvsp[-1].string)); delete[] (yyvsp[-1].string);}
#line 6943 "prebuilt\\asmparse.cpp"
    break;

  case 565: /* typeSpec: type  */
#line 1508 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveTypeSpec((yyvsp[0].binstr)); }
#line 6949 "prebuilt\\asmparse.cpp"
    break;

  case 566: /* nativeType: %empty  */
#line 1512 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); }
#line 6955 "prebuilt\\asmparse.cpp"
    break;

  case 567: /* nativeType: CUSTOM_ '(' compQstring ',' compQstring ',' compQstring ',' compQstring ')'  */
#line 1514 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt((yyval.binstr),(yyvsp[-7].binstr)->length()); (yyval.binstr)->append((yyvsp[-7].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-5].binstr)->length()); (yyval.binstr)->append((yyvsp[-5].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].binstr)->length()); (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].binstr)->length()); (yyval.binstr)->append((yyvsp[-1].binstr));
                                                                PASM->report->warn("Deprecated 4-string form of custom marshaler, first two strings ignored\n");}
#line 6966 "prebuilt\\asmparse.cpp"
    break;

  case 568: /* nativeType: CUSTOM_ '(' compQstring ',' compQstring ')'  */
#line 1521 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].binstr)->length()); (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].binstr)->length()); (yyval.binstr)->append((yyvsp[-1].binstr)); }
#line 6976 "prebuilt\\asmparse.cpp"
    break;

  case 569: /* nativeType: FIXED_ SYSSTRING_ '[' int32 ']'  */
#line 1526 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FIXEDSYSSTRING);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32)); }
#line 6983 "prebuilt\\asmparse.cpp"
    break;

  case 570: /* nativeType: FIXED_ ARRAY_ '[' int32 ']' nativeType  */
#line 1529 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FIXEDARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-2].int32)); (yyval.binstr)->append((yyvsp[0].binstr)); }
#line 6990 "prebuilt\\asmparse.cpp"
    break;

  case 571: /* nativeType: VARIANT_  */
#line 1531 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VARIANT);
                                                                PASM->report->warn("Deprecated native type 'variant'\n"); }
#line 6997 "prebuilt\\asmparse.cpp"
    break;

  case 572: /* nativeType: CURRENCY_  */
#line 1533 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CURRENCY); }
#line 7003 "prebuilt\\asmparse.cpp"
    break;

  case 573: /* nativeType: SYSCHAR_  */
#line 1534 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SYSCHAR);
                                                                PASM->report->warn("Deprecated native type 'syschar'\n"); }
#line 7010 "prebuilt\\asmparse.cpp"
    break;

  case 574: /* nativeType: VOID_  */
#line 1536 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VOID);
                                                                PASM->report->warn("Deprecated native type 'void'\n"); }
#line 7017 "prebuilt\\asmparse.cpp"
    break;

  case 575: /* nativeType: BOOL_  */
#line 1538 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BOOLEAN); }
#line 7023 "prebuilt\\asmparse.cpp"
    break;

  case 576: /* nativeType: INT8_  */
#line 1539 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I1); }
#line 7029 "prebuilt\\asmparse.cpp"
    break;

  case 577: /* nativeType: INT16_  */
#line 1540 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I2); }
#line 7035 "prebuilt\\asmparse.cpp"
    break;

  case 578: /* nativeType: INT32_  */
#line 1541 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I4); }
#line 7041 "prebuilt\\asmparse.cpp"
    break;

  case 579: /* nativeType: INT64_  */
#line 1542 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I8); }
#line 7047 "prebuilt\\asmparse.cpp"
    break;

  case 580: /* nativeType: FLOAT32_  */
#line 1543 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_R4); }
#line 7053 "prebuilt\\asmparse.cpp"
    break;

  case 581: /* nativeType: FLOAT64_  */
#line 1544 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_R8); }
#line 7059 "prebuilt\\asmparse.cpp"
    break;

  case 582: /* nativeType: ERROR_  */
#line 1545 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ERROR); }
#line 7065 "prebuilt\\asmparse.cpp"
    break;

  case 583: /* nativeType: UNSIGNED_ INT8_  */
#line 1546 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U1); }
#line 7071 "prebuilt\\asmparse.cpp"
    break;

  case 584: /* nativeType: UNSIGNED_ INT16_  */
#line 1547 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U2); }
#line 7077 "prebuilt\\asmparse.cpp"
    break;

  case 585: /* nativeType: UNSIGNED_ INT32_  */
#line 1548 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U4); }
#line 7083 "prebuilt\\asmparse.cpp"
    break;

  case 586: /* nativeType: UNSIGNED_ INT64_  */
#line 1549 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U8); }
#line 7089 "prebuilt\\asmparse.cpp"
    break;

  case 587: /* nativeType: UINT8_  */
#line 1550 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U1); }
#line 7095 "prebuilt\\asmparse.cpp"
    break;

  case 588: /* nativeType: UINT16_  */
#line 1551 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U2); }
#line 7101 "prebuilt\\asmparse.cpp"
    break;

  case 589: /* nativeType: UINT32_  */
#line 1552 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U4); }
#line 7107 "prebuilt\\asmparse.cpp"
    break;

  case 590: /* nativeType: UINT64_  */
#line 1553 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U8); }
#line 7113 "prebuilt\\asmparse.cpp"
    break;

  case 591: /* nativeType: nativeType '*'  */
#line 1554 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(NATIVE_TYPE_PTR);
                                                                PASM->report->warn("Deprecated native type '*'\n"); }
#line 7120 "prebuilt\\asmparse.cpp"
    break;

  case 592: /* nativeType: nativeType '[' ']'  */
#line 1556 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY); }
#line 7127 "prebuilt\\asmparse.cpp"
    break;

  case 593: /* nativeType: nativeType '[' int32 ']'  */
#line 1558 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-3].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32));
                                                                corEmitInt((yyval.binstr),0); }
#line 7137 "prebuilt\\asmparse.cpp"
    break;

  case 594: /* nativeType: nativeType '[' int32 '+' int32 ']'  */
#line 1563 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-5].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32));
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].int32));
                                                                corEmitInt((yyval.binstr),ntaSizeParamIndexSpecified); }
#line 7147 "prebuilt\\asmparse.cpp"
    break;

  case 595: /* nativeType: nativeType '[' '+' int32 ']'  */
#line 1568 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-4].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32)); }
#line 7155 "prebuilt\\asmparse.cpp"
    break;

  case 596: /* nativeType: DECIMAL_  */
#line 1571 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_DECIMAL);
                                                                PASM->report->warn("Deprecated native type 'decimal'\n"); }
#line 7162 "prebuilt\\asmparse.cpp"
    break;

  case 597: /* nativeType: DATE_  */
#line 1573 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_DATE);
                                                                PASM->report->warn("Deprecated native type 'date'\n"); }
#line 7169 "prebuilt\\asmparse.cpp"
    break;

  case 598: /* nativeType: BSTR_  */
#line 1575 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BSTR); }
#line 7175 "prebuilt\\asmparse.cpp"
    break;

  case 599: /* nativeType: LPSTR_  */
#line 1576 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPSTR); }
#line 7181 "prebuilt\\asmparse.cpp"
    break;

  case 600: /* nativeType: LPWSTR_  */
#line 1577 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPWSTR); }
#line 7187 "prebuilt\\asmparse.cpp"
    break;

  case 601: /* nativeType: LPTSTR_  */
#line 1578 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPTSTR); }
#line 7193 "prebuilt\\asmparse.cpp"
    break;

  case 602: /* nativeType: OBJECTREF_  */
#line 1579 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_OBJECTREF);
                                                                PASM->report->warn("Deprecated native type 'objectref'\n"); }
#line 7200 "prebuilt\\asmparse.cpp"
    break;

  case 603: /* nativeType: IUNKNOWN_ iidParamIndex  */
#line 1581 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_IUNKNOWN);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7207 "prebuilt\\asmparse.cpp"
    break;

  case 604: /* nativeType: IDISPATCH_ iidParamIndex  */
#line 1583 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_IDISPATCH);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7214 "prebuilt\\asmparse.cpp"
    break;

  case 605: /* nativeType: STRUCT_  */
#line 1585 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_STRUCT); }
#line 7220 "prebuilt\\asmparse.cpp"
    break;

  case 606: /* nativeType: INTERFACE_ iidParamIndex  */
#line 1586 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_INTF);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7227 "prebuilt\\asmparse.cpp"
    break;

  case 607: /* nativeType: SAFEARRAY_ variantType  */
#line 1588 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[0].int32));
                                                                corEmitInt((yyval.binstr),0);}
#line 7235 "prebuilt\\asmparse.cpp"
    break;

  case 608: /* nativeType: SAFEARRAY_ variantType ',' compQstring  */
#line 1591 "asmparse.y"
                                                                 { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-2].int32));
                                                                corEmitInt((yyval.binstr),(yyvsp[0].binstr)->length()); (yyval.binstr)->append((yyvsp[0].binstr)); }
#line 7243 "prebuilt\\asmparse.cpp"
    break;

  case 609: /* nativeType: INT_  */
#line 1595 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_INT); }
#line 7249 "prebuilt\\asmparse.cpp"
    break;

  case 610: /* nativeType: UNSIGNED_ INT_  */
#line 1596 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_UINT); }
#line 7255 "prebuilt\\asmparse.cpp"
    break;

  case 611: /* nativeType: UINT_  */
#line 1597 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_UINT); }
#line 7261 "prebuilt\\asmparse.cpp"
    break;

  case 612: /* nativeType: NESTED_ STRUCT_  */
#line 1598 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_NESTEDSTRUCT);
                                                                PASM->report->warn("Deprecated native type 'nested struct'\n"); }
#line 7268 "prebuilt\\asmparse.cpp"
    break;

  case 613: /* nativeType: BYVALSTR_  */
#line 1600 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BYVALSTR); }
#line 7274 "prebuilt\\asmparse.cpp"
    break;

  case 614: /* nativeType: ANSI_ BSTR_  */
#line 1601 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ANSIBSTR); }
#line 7280 "prebuilt\\asmparse.cpp"
    break;

  case 615: /* nativeType: TBSTR_  */
#line 1602 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_TBSTR); }
#line 7286 "prebuilt\\asmparse.cpp"
    break;

  case 616: /* nativeType: VARIANT_ BOOL_  */
#line 1603 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VARIANTBOOL); }
#line 7292 "prebuilt\\asmparse.cpp"
    break;

  case 617: /* nativeType: METHOD_  */
#line 1604 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FUNC); }
#line 7298 "prebuilt\\asmparse.cpp"
    break;

  case 618: /* nativeType: AS_ ANY_  */
#line 1605 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ASANY); }
#line 7304 "prebuilt\\asmparse.cpp"
    break;

  case 619: /* nativeType: LPSTRUCT_  */
#line 1606 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPSTRUCT); }
#line 7310 "prebuilt\\asmparse.cpp"
    break;

  case 620: /* nativeType: TYPEDEF_TS  */
#line 1607 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[0].tdd)->m_pbsTypeSpec); }
#line 7316 "prebuilt\\asmparse.cpp"
    break;

  case 621: /* iidParamIndex: %empty  */
#line 1610 "asmparse.y"
                                                              { (yyval.int32) = -1; }
#line 7322 "prebuilt\\asmparse.cpp"
    break;

  case 622: /* iidParamIndex: '(' IIDPARAM_ '=' int32 ')'  */
#line 1611 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32); }
#line 7328 "prebuilt\\asmparse.cpp"
    break;

  case 623: /* variantType: %empty  */
#line 1614 "asmparse.y"
                                                              { (yyval.int32) = VT_EMPTY; }
#line 7334 "prebuilt\\asmparse.cpp"
    break;

  case 624: /* variantType: NULL_  */
#line 1615 "asmparse.y"
                                                              { (yyval.int32) = VT_NULL; }
#line 7340 "prebuilt\\asmparse.cpp"
    break;

  case 625: /* variantType: VARIANT_  */
#line 1616 "asmparse.y"
                                                              { (yyval.int32) = VT_VARIANT; }
#line 7346 "prebuilt\\asmparse.cpp"
    break;

  case 626: /* variantType: CURRENCY_  */
#line 1617 "asmparse.y"
                                                              { (yyval.int32) = VT_CY; }
#line 7352 "prebuilt\\asmparse.cpp"
    break;

  case 627: /* variantType: VOID_  */
#line 1618 "asmparse.y"
                                                              { (yyval.int32) = VT_VOID; }
#line 7358 "prebuilt\\asmparse.cpp"
    break;

  case 628: /* variantType: BOOL_  */
#line 1619 "asmparse.y"
                                                              { (yyval.int32) = VT_BOOL; }
#line 7364 "prebuilt\\asmparse.cpp"
    break;

  case 629: /* variantType: INT8_  */
#line 1620 "asmparse.y"
                                                              { (yyval.int32) = VT_I1; }
#line 7370 "prebuilt\\asmparse.cpp"
    break;

  case 630: /* variantType: INT16_  */
#line 1621 "asmparse.y"
                                                              { (yyval.int32) = VT_I2; }
#line 7376 "prebuilt\\asmparse.cpp"
    break;

  case 631: /* variantType: INT32_  */
#line 1622 "asmparse.y"
                                                              { (yyval.int32) = VT_I4; }
#line 7382 "prebuilt\\asmparse.cpp"
    break;

  case 632: /* variantType: INT64_  */
#line 1623 "asmparse.y"
                                                              { (yyval.int32) = VT_I8; }
#line 7388 "prebuilt\\asmparse.cpp"
    break;

  case 633: /* variantType: FLOAT32_  */
#line 1624 "asmparse.y"
                                                              { (yyval.int32) = VT_R4; }
#line 7394 "prebuilt\\asmparse.cpp"
    break;

  case 634: /* variantType: FLOAT64_  */
#line 1625 "asmparse.y"
                                                              { (yyval.int32) = VT_R8; }
#line 7400 "prebuilt\\asmparse.cpp"
    break;

  case 635: /* variantType: UNSIGNED_ INT8_  */
#line 1626 "asmparse.y"
                                                              { (yyval.int32) = VT_UI1; }
#line 7406 "prebuilt\\asmparse.cpp"
    break;

  case 636: /* variantType: UNSIGNED_ INT16_  */
#line 1627 "asmparse.y"
                                                              { (yyval.int32) = VT_UI2; }
#line 7412 "prebuilt\\asmparse.cpp"
    break;

  case 637: /* variantType: UNSIGNED_ INT32_  */
#line 1628 "asmparse.y"
                                                              { (yyval.int32) = VT_UI4; }
#line 7418 "prebuilt\\asmparse.cpp"
    break;

  case 638: /* variantType: UNSIGNED_ INT64_  */
#line 1629 "asmparse.y"
                                                              { (yyval.int32) = VT_UI8; }
#line 7424 "prebuilt\\asmparse.cpp"
    break;

  case 639: /* variantType: UINT8_  */
#line 1630 "asmparse.y"
                                                              { (yyval.int32) = VT_UI1; }
#line 7430 "prebuilt\\asmparse.cpp"
    break;

  case 640: /* variantType: UINT16_  */
#line 1631 "asmparse.y"
                                                              { (yyval.int32) = VT_UI2; }
#line 7436 "prebuilt\\asmparse.cpp"
    break;

  case 641: /* variantType: UINT32_  */
#line 1632 "asmparse.y"
                                                              { (yyval.int32) = VT_UI4; }
#line 7442 "prebuilt\\asmparse.cpp"
    break;

  case 642: /* variantType: UINT64_  */
#line 1633 "asmparse.y"
                                                              { (yyval.int32) = VT_UI8; }
#line 7448 "prebuilt\\asmparse.cpp"
    break;

  case 643: /* variantType: '*'  */
#line 1634 "asmparse.y"
                                                              { (yyval.int32) = VT_PTR; }
#line 7454 "prebuilt\\asmparse.cpp"
    break;

  case 644: /* variantType: variantType '[' ']'  */
#line 1635 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-2].int32) | VT_ARRAY; }
#line 7460 "prebuilt\\asmparse.cpp"
    break;

  case 645: /* variantType: variantType VECTOR_  */
#line 1636 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32) | VT_VECTOR; }
#line 7466 "prebuilt\\asmparse.cpp"
    break;

  case 646: /* variantType: variantType '&'  */
#line 1637 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32) | VT_BYREF; }
#line 7472 "prebuilt\\asmparse.cpp"
    break;

  case 647: /* variantType: DECIMAL_  */
#line 1638 "asmparse.y"
                                                              { (yyval.int32) = VT_DECIMAL; }
#line 7478 "prebuilt\\asmparse.cpp"
    break;

  case 648: /* variantType: DATE_  */
#line 1639 "asmparse.y"
                                                              { (yyval.int32) = VT_DATE; }
#line 7484 "prebuilt\\asmparse.cpp"
    break;

  case 649: /* variantType: BSTR_  */
#line 1640 "asmparse.y"
                                                              { (yyval.int32) = VT_BSTR; }
#line 7490 "prebuilt\\asmparse.cpp"
    break;

  case 650: /* variantType: LPSTR_  */
#line 1641 "asmparse.y"
                                                              { (yyval.int32) = VT_LPSTR; }
#line 7496 "prebuilt\\asmparse.cpp"
    break;

  case 651: /* variantType: LPWSTR_  */
#line 1642 "asmparse.y"
                                                              { (yyval.int32) = VT_LPWSTR; }
#line 7502 "prebuilt\\asmparse.cpp"
    break;

  case 652: /* variantType: IUNKNOWN_  */
#line 1643 "asmparse.y"
                                                              { (yyval.int32) = VT_UNKNOWN; }
#line 7508 "prebuilt\\asmparse.cpp"
    break;

  case 653: /* variantType: IDISPATCH_  */
#line 1644 "asmparse.y"
                                                              { (yyval.int32) = VT_DISPATCH; }
#line 7514 "prebuilt\\asmparse.cpp"
    break;

  case 654: /* variantType: SAFEARRAY_  */
#line 1645 "asmparse.y"
                                                              { (yyval.int32) = VT_SAFEARRAY; }
#line 7520 "prebuilt\\asmparse.cpp"
    break;

  case 655: /* variantType: INT_  */
#line 1646 "asmparse.y"
                                                              { (yyval.int32) = VT_INT; }
#line 7526 "prebuilt\\asmparse.cpp"
    break;

  case 656: /* variantType: UNSIGNED_ INT_  */
#line 1647 "asmparse.y"
                                                              { (yyval.int32) = VT_UINT; }
#line 7532 "prebuilt\\asmparse.cpp"
    break;

  case 657: /* variantType: UINT_  */
#line 1648 "asmparse.y"
                                                              { (yyval.int32) = VT_UINT; }
#line 7538 "prebuilt\\asmparse.cpp"
    break;

  case 658: /* variantType: ERROR_  */
#line 1649 "asmparse.y"
                                                              { (yyval.int32) = VT_ERROR; }
#line 7544 "prebuilt\\asmparse.cpp"
    break;

  case 659: /* variantType: HRESULT_  */
#line 1650 "asmparse.y"
                                                              { (yyval.int32) = VT_HRESULT; }
#line 7550 "prebuilt\\asmparse.cpp"
    break;

  case 660: /* variantType: CARRAY_  */
#line 1651 "asmparse.y"
                                                              { (yyval.int32) = VT_CARRAY; }
#line 7556 "prebuilt\\asmparse.cpp"
    break;

  case 661: /* variantType: USERDEFINED_  */
#line 1652 "asmparse.y"
                                                              { (yyval.int32) = VT_USERDEFINED; }
#line 7562 "prebuilt\\asmparse.cpp"
    break;

  case 662: /* variantType: RECORD_  */
#line 1653 "asmparse.y"
                                                              { (yyval.int32) = VT_RECORD; }
#line 7568 "prebuilt\\asmparse.cpp"
    break;

  case 663: /* variantType: FILETIME_  */
#line 1654 "asmparse.y"
                                                              { (yyval.int32) = VT_FILETIME; }
#line 7574 "prebuilt\\asmparse.cpp"
    break;

  case 664: /* variantType: BLOB_  */
#line 1655 "asmparse.y"
                                                              { (yyval.int32) = VT_BLOB; }
#line 7580 "prebuilt\\asmparse.cpp"
    break;

  case 665: /* variantType: STREAM_  */
#line 1656 "asmparse.y"
                                                              { (yyval.int32) = VT_STREAM; }
#line 7586 "prebuilt\\asmparse.cpp"
    break;

  case 666: /* variantType: STORAGE_  */
#line 1657 "asmparse.y"
                                                              { (yyval.int32) = VT_STORAGE; }
#line 7592 "prebuilt\\asmparse.cpp"
    break;

  case 667: /* variantType: STREAMED_OBJECT_  */
#line 1658 "asmparse.y"
                                                              { (yyval.int32) = VT_STREAMED_OBJECT; }
#line 7598 "prebuilt\\asmparse.cpp"
    break;

  case 668: /* variantType: STORED_OBJECT_  */
#line 1659 "asmparse.y"
                                                              { (yyval.int32) = VT_STORED_OBJECT; }
#line 7604 "prebuilt\\asmparse.cpp"
    break;

  case 669: /* variantType: BLOB_OBJECT_  */
#line 1660 "asmparse.y"
                                                              { (yyval.int32) = VT_BLOB_OBJECT; }
#line 7610 "prebuilt\\asmparse.cpp"
    break;

  case 670: /* variantType: CF_  */
#line 1661 "asmparse.y"
                                                              { (yyval.int32) = VT_CF; }
#line 7616 "prebuilt\\asmparse.cpp"
    break;

  case 671: /* variantType: CLSID_  */
#line 1662 "asmparse.y"
                                                              { (yyval.int32) = VT_CLSID; }
#line 7622 "prebuilt\\asmparse.cpp"
    break;

  case 672: /* type: CLASS_ className  */
#line 1666 "asmparse.y"
                                                              { if((yyvsp[0].token) == PASM->m_tkSysString)
                                                                {     (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); }
                                                                else if((yyvsp[0].token) == PASM->m_tkSysObject)
                                                                {     (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_OBJECT); }
                                                                else
                                                                 (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CLASS, (yyvsp[0].token)); }
#line 7633 "prebuilt\\asmparse.cpp"
    break;

  case 673: /* type: OBJECT_  */
#line 1672 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_OBJECT); }
#line 7639 "prebuilt\\asmparse.cpp"
    break;

  case 674: /* type: VALUE_ CLASS_ className  */
#line 1673 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, (yyvsp[0].token)); }
#line 7645 "prebuilt\\asmparse.cpp"
    break;

  case 675: /* type: VALUETYPE_ className  */
#line 1674 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, (yyvsp[0].token)); }
#line 7651 "prebuilt\\asmparse.cpp"
    break;

  case 676: /* type: type '[' ']'  */
#line 1675 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 7657 "prebuilt\\asmparse.cpp"
    break;

  case 677: /* type: type '[' bounds1 ']'  */
#line 1676 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeArray(ELEMENT_TYPE_ARRAY, (yyvsp[-3].binstr), (yyvsp[-1].binstr)); }
#line 7663 "prebuilt\\asmparse.cpp"
    break;

  case 678: /* type: type '&'  */
#line 1677 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_BYREF); }
#line 7669 "prebuilt\\asmparse.cpp"
    break;

  case 679: /* type: type '*'  */
#line 1678 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_PTR); }
#line 7675 "prebuilt\\asmparse.cpp"
    break;

  case 680: /* type: type PINNED_  */
#line 1679 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_PINNED); }
#line 7681 "prebuilt\\asmparse.cpp"
    break;

  case 681: /* type: type MODREQ_ '(' typeSpec ')'  */
#line 1680 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_REQD, (yyvsp[-1].token));
                                                                (yyval.binstr)->append((yyvsp[-4].binstr)); }
#line 7688 "prebuilt\\asmparse.cpp"
    break;

  case 682: /* type: type MODOPT_ '(' typeSpec ')'  */
#line 1682 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_OPT, (yyvsp[-1].token));
                                                                (yyval.binstr)->append((yyvsp[-4].binstr)); }
#line 7695 "prebuilt\\asmparse.cpp"
    break;

  case 683: /* type: methodSpec callConv type '*' '(' sigArgs0 ')'  */
#line 1685 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeSig((yyvsp[-5].int32), (yyvsp[-4].binstr), (yyvsp[-1].binstr));
                                                                (yyval.binstr)->insertInt8(ELEMENT_TYPE_FNPTR);
                                                                PASM->delArgNameList(PASM->m_firstArgName);
                                                                PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                                PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                              }
#line 7706 "prebuilt\\asmparse.cpp"
    break;

  case 684: /* type: type '<' tyArgs1 '>'  */
#line 1691 "asmparse.y"
                                                              { if((yyvsp[-1].binstr) == NULL) (yyval.binstr) = (yyvsp[-3].binstr);
                                                                else {
                                                                  (yyval.binstr) = new BinStr();
                                                                  (yyval.binstr)->appendInt8(ELEMENT_TYPE_GENERICINST);
                                                                  (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                  corEmitInt((yyval.binstr), corCountArgs((yyvsp[-1].binstr)));
                                                                  (yyval.binstr)->append((yyvsp[-1].binstr)); delete (yyvsp[-3].binstr); delete (yyvsp[-1].binstr); }}
#line 7718 "prebuilt\\asmparse.cpp"
    break;

  case 685: /* type: '!' '!' int32  */
#line 1698 "asmparse.y"
                                                              { //if(PASM->m_pCurMethod)  {
                                                                //  if(($3 < 0)||((DWORD)$3 >= PASM->m_pCurMethod->m_NumTyPars))
                                                                //    PASM->report->error("Invalid method type parameter '%d'\n",$3);
                                                                  (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_MVAR); corEmitInt((yyval.binstr), (yyvsp[0].int32));
                                                                //} else PASM->report->error("Method type parameter '%d' outside method scope\n",$3);
                                                              }
#line 7729 "prebuilt\\asmparse.cpp"
    break;

  case 686: /* type: '!' int32  */
#line 1704 "asmparse.y"
                                                              { //if(PASM->m_pCurClass)  {
                                                                //  if(($2 < 0)||((DWORD)$2 >= PASM->m_pCurClass->m_NumTyPars))
                                                                //    PASM->report->error("Invalid type parameter '%d'\n",$2);
                                                                  (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_VAR); corEmitInt((yyval.binstr), (yyvsp[0].int32));
                                                                //} else PASM->report->error("Type parameter '%d' outside class scope\n",$2);
                                                              }
#line 7740 "prebuilt\\asmparse.cpp"
    break;

  case 687: /* type: '!' '!' dottedName  */
#line 1710 "asmparse.y"
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
#line 7760 "prebuilt\\asmparse.cpp"
    break;

  case 688: /* type: '!' dottedName  */
#line 1725 "asmparse.y"
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
#line 7780 "prebuilt\\asmparse.cpp"
    break;

  case 689: /* type: TYPEDREF_  */
#line 1740 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_TYPEDBYREF); }
#line 7786 "prebuilt\\asmparse.cpp"
    break;

  case 690: /* type: VOID_  */
#line 1741 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_VOID); }
#line 7792 "prebuilt\\asmparse.cpp"
    break;

  case 691: /* type: NATIVE_ INT_  */
#line 1742 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I); }
#line 7798 "prebuilt\\asmparse.cpp"
    break;

  case 692: /* type: NATIVE_ UNSIGNED_ INT_  */
#line 1743 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U); }
#line 7804 "prebuilt\\asmparse.cpp"
    break;

  case 693: /* type: NATIVE_ UINT_  */
#line 1744 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U); }
#line 7810 "prebuilt\\asmparse.cpp"
    break;

  case 694: /* type: simpleType  */
#line 1745 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7816 "prebuilt\\asmparse.cpp"
    break;

  case 695: /* type: ELLIPSIS type  */
#line 1746 "asmparse.y"
                                                               { (yyval.binstr) = (yyvsp[0].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SENTINEL); }
#line 7822 "prebuilt\\asmparse.cpp"
    break;

  case 696: /* simpleType: CHAR_  */
#line 1749 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CHAR); }
#line 7828 "prebuilt\\asmparse.cpp"
    break;

  case 697: /* simpleType: STRING_  */
#line 1750 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); }
#line 7834 "prebuilt\\asmparse.cpp"
    break;

  case 698: /* simpleType: BOOL_  */
#line 1751 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_BOOLEAN); }
#line 7840 "prebuilt\\asmparse.cpp"
    break;

  case 699: /* simpleType: INT8_  */
#line 1752 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I1); }
#line 7846 "prebuilt\\asmparse.cpp"
    break;

  case 700: /* simpleType: INT16_  */
#line 1753 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I2); }
#line 7852 "prebuilt\\asmparse.cpp"
    break;

  case 701: /* simpleType: INT32_  */
#line 1754 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I4); }
#line 7858 "prebuilt\\asmparse.cpp"
    break;

  case 702: /* simpleType: INT64_  */
#line 1755 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I8); }
#line 7864 "prebuilt\\asmparse.cpp"
    break;

  case 703: /* simpleType: FLOAT32_  */
#line 1756 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4); }
#line 7870 "prebuilt\\asmparse.cpp"
    break;

  case 704: /* simpleType: FLOAT64_  */
#line 1757 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8); }
#line 7876 "prebuilt\\asmparse.cpp"
    break;

  case 705: /* simpleType: UNSIGNED_ INT8_  */
#line 1758 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1); }
#line 7882 "prebuilt\\asmparse.cpp"
    break;

  case 706: /* simpleType: UNSIGNED_ INT16_  */
#line 1759 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2); }
#line 7888 "prebuilt\\asmparse.cpp"
    break;

  case 707: /* simpleType: UNSIGNED_ INT32_  */
#line 1760 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4); }
#line 7894 "prebuilt\\asmparse.cpp"
    break;

  case 708: /* simpleType: UNSIGNED_ INT64_  */
#line 1761 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8); }
#line 7900 "prebuilt\\asmparse.cpp"
    break;

  case 709: /* simpleType: UINT8_  */
#line 1762 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1); }
#line 7906 "prebuilt\\asmparse.cpp"
    break;

  case 710: /* simpleType: UINT16_  */
#line 1763 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2); }
#line 7912 "prebuilt\\asmparse.cpp"
    break;

  case 711: /* simpleType: UINT32_  */
#line 1764 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4); }
#line 7918 "prebuilt\\asmparse.cpp"
    break;

  case 712: /* simpleType: UINT64_  */
#line 1765 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8); }
#line 7924 "prebuilt\\asmparse.cpp"
    break;

  case 713: /* simpleType: TYPEDEF_TS  */
#line 1766 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[0].tdd)->m_pbsTypeSpec); }
#line 7930 "prebuilt\\asmparse.cpp"
    break;

  case 714: /* bounds1: bound  */
#line 1769 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7936 "prebuilt\\asmparse.cpp"
    break;

  case 715: /* bounds1: bounds1 ',' bound  */
#line 1770 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyvsp[-2].binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 7942 "prebuilt\\asmparse.cpp"
    break;

  case 716: /* bound: %empty  */
#line 1773 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0x7FFFFFFF); (yyval.binstr)->appendInt32(0x7FFFFFFF);  }
#line 7948 "prebuilt\\asmparse.cpp"
    break;

  case 717: /* bound: ELLIPSIS  */
#line 1774 "asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0x7FFFFFFF); (yyval.binstr)->appendInt32(0x7FFFFFFF);  }
#line 7954 "prebuilt\\asmparse.cpp"
    break;

  case 718: /* bound: int32  */
#line 1775 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0); (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 7960 "prebuilt\\asmparse.cpp"
    break;

  case 719: /* bound: int32 ELLIPSIS int32  */
#line 1776 "asmparse.y"
                                                               { FAIL_UNLESS((yyvsp[-2].int32) <= (yyvsp[0].int32), ("lower bound %d must be <= upper bound %d\n", (yyvsp[-2].int32), (yyvsp[0].int32)));
                                                                if ((yyvsp[-2].int32) > (yyvsp[0].int32)) { YYERROR; };
                                                                (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[-2].int32)); (yyval.binstr)->appendInt32((yyvsp[0].int32)-(yyvsp[-2].int32)+1); }
#line 7968 "prebuilt\\asmparse.cpp"
    break;

  case 720: /* bound: int32 ELLIPSIS  */
#line 1779 "asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[-1].int32)); (yyval.binstr)->appendInt32(0x7FFFFFFF); }
#line 7974 "prebuilt\\asmparse.cpp"
    break;

  case 721: /* secDecl: _PERMISSION secAction typeSpec '(' nameValPairs ')'  */
#line 1784 "asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-4].secAct), (yyvsp[-3].token), (yyvsp[-1].pair)); }
#line 7980 "prebuilt\\asmparse.cpp"
    break;

  case 722: /* secDecl: _PERMISSION secAction typeSpec '=' '{' customBlobDescr '}'  */
#line 1786 "asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-5].secAct), (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 7986 "prebuilt\\asmparse.cpp"
    break;

  case 723: /* secDecl: _PERMISSION secAction typeSpec  */
#line 1787 "asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-1].secAct), (yyvsp[0].token), (NVPair *)NULL); }
#line 7992 "prebuilt\\asmparse.cpp"
    break;

  case 724: /* secDecl: psetHead bytes ')'  */
#line 1788 "asmparse.y"
                                                              { PASM->AddPermissionSetDecl((yyvsp[-2].secAct), (yyvsp[-1].binstr)); }
#line 7998 "prebuilt\\asmparse.cpp"
    break;

  case 725: /* secDecl: _PERMISSIONSET secAction compQstring  */
#line 1790 "asmparse.y"
                                                              { PASM->AddPermissionSetDecl((yyvsp[-1].secAct),BinStrToUnicode((yyvsp[0].binstr),true));}
#line 8004 "prebuilt\\asmparse.cpp"
    break;

  case 726: /* secDecl: _PERMISSIONSET secAction '=' '{' secAttrSetBlob '}'  */
#line 1792 "asmparse.y"
                                                              { BinStr* ret = new BinStr();
                                                                ret->insertInt8('.');
                                                                corEmitInt(ret, nSecAttrBlobs);
                                                                ret->append((yyvsp[-1].binstr));
                                                                PASM->AddPermissionSetDecl((yyvsp[-4].secAct),ret);
                                                                nSecAttrBlobs = 0; }
#line 8015 "prebuilt\\asmparse.cpp"
    break;

  case 727: /* secAttrSetBlob: %empty  */
#line 1800 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); nSecAttrBlobs = 0;}
#line 8021 "prebuilt\\asmparse.cpp"
    break;

  case 728: /* secAttrSetBlob: secAttrBlob  */
#line 1801 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); nSecAttrBlobs = 1; }
#line 8027 "prebuilt\\asmparse.cpp"
    break;

  case 729: /* secAttrSetBlob: secAttrBlob ',' secAttrSetBlob  */
#line 1802 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); nSecAttrBlobs++; }
#line 8033 "prebuilt\\asmparse.cpp"
    break;

  case 730: /* secAttrBlob: typeSpec '=' '{' customBlobNVPairs '}'  */
#line 1806 "asmparse.y"
                                                              { (yyval.binstr) = PASM->EncodeSecAttr(PASM->ReflectionNotation((yyvsp[-4].token)),(yyvsp[-1].binstr),nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
#line 8040 "prebuilt\\asmparse.cpp"
    break;

  case 731: /* secAttrBlob: CLASS_ SQSTRING '=' '{' customBlobNVPairs '}'  */
#line 1809 "asmparse.y"
                                                              { (yyval.binstr) = PASM->EncodeSecAttr((yyvsp[-4].string),(yyvsp[-1].binstr),nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
#line 8047 "prebuilt\\asmparse.cpp"
    break;

  case 732: /* psetHead: _PERMISSIONSET secAction '=' '('  */
#line 1813 "asmparse.y"
                                                              { (yyval.secAct) = (yyvsp[-2].secAct); bParsingByteArray = TRUE; }
#line 8053 "prebuilt\\asmparse.cpp"
    break;

  case 733: /* psetHead: _PERMISSIONSET secAction BYTEARRAY_ '('  */
#line 1815 "asmparse.y"
                                                              { (yyval.secAct) = (yyvsp[-2].secAct); bParsingByteArray = TRUE; }
#line 8059 "prebuilt\\asmparse.cpp"
    break;

  case 734: /* nameValPairs: nameValPair  */
#line 1818 "asmparse.y"
                                                              { (yyval.pair) = (yyvsp[0].pair); }
#line 8065 "prebuilt\\asmparse.cpp"
    break;

  case 735: /* nameValPairs: nameValPair ',' nameValPairs  */
#line 1819 "asmparse.y"
                                                              { (yyval.pair) = (yyvsp[-2].pair)->Concat((yyvsp[0].pair)); }
#line 8071 "prebuilt\\asmparse.cpp"
    break;

  case 736: /* nameValPair: compQstring '=' caValue  */
#line 1822 "asmparse.y"
                                                              { (yyvsp[-2].binstr)->appendInt8(0); (yyval.pair) = new NVPair((yyvsp[-2].binstr), (yyvsp[0].binstr)); }
#line 8077 "prebuilt\\asmparse.cpp"
    break;

  case 737: /* truefalse: TRUE_  */
#line 1825 "asmparse.y"
                                                              { (yyval.int32) = 1; }
#line 8083 "prebuilt\\asmparse.cpp"
    break;

  case 738: /* truefalse: FALSE_  */
#line 1826 "asmparse.y"
                                                              { (yyval.int32) = 0; }
#line 8089 "prebuilt\\asmparse.cpp"
    break;

  case 739: /* caValue: truefalse  */
#line 1829 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_BOOLEAN);
                                                                (yyval.binstr)->appendInt8((yyvsp[0].int32)); }
#line 8097 "prebuilt\\asmparse.cpp"
    break;

  case 740: /* caValue: int32  */
#line 1832 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_I4);
                                                                (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 8105 "prebuilt\\asmparse.cpp"
    break;

  case 741: /* caValue: INT32_ '(' int32 ')'  */
#line 1835 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_I4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8113 "prebuilt\\asmparse.cpp"
    break;

  case 742: /* caValue: compQstring  */
#line 1838 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_STRING);
                                                                (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr);
                                                                (yyval.binstr)->appendInt8(0); }
#line 8122 "prebuilt\\asmparse.cpp"
    break;

  case 743: /* caValue: className '(' INT8_ ':' int32 ')'  */
#line 1842 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(1);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8133 "prebuilt\\asmparse.cpp"
    break;

  case 744: /* caValue: className '(' INT16_ ':' int32 ')'  */
#line 1848 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(2);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8144 "prebuilt\\asmparse.cpp"
    break;

  case 745: /* caValue: className '(' INT32_ ':' int32 ')'  */
#line 1854 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8155 "prebuilt\\asmparse.cpp"
    break;

  case 746: /* caValue: className '(' int32 ')'  */
#line 1860 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-3].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8166 "prebuilt\\asmparse.cpp"
    break;

  case 747: /* secAction: REQUEST_  */
#line 1868 "asmparse.y"
                                                              { (yyval.secAct) = dclRequest; }
#line 8172 "prebuilt\\asmparse.cpp"
    break;

  case 748: /* secAction: DEMAND_  */
#line 1869 "asmparse.y"
                                                              { (yyval.secAct) = dclDemand; }
#line 8178 "prebuilt\\asmparse.cpp"
    break;

  case 749: /* secAction: ASSERT_  */
#line 1870 "asmparse.y"
                                                              { (yyval.secAct) = dclAssert; }
#line 8184 "prebuilt\\asmparse.cpp"
    break;

  case 750: /* secAction: DENY_  */
#line 1871 "asmparse.y"
                                                              { (yyval.secAct) = dclDeny; }
#line 8190 "prebuilt\\asmparse.cpp"
    break;

  case 751: /* secAction: PERMITONLY_  */
#line 1872 "asmparse.y"
                                                              { (yyval.secAct) = dclPermitOnly; }
#line 8196 "prebuilt\\asmparse.cpp"
    break;

  case 752: /* secAction: LINKCHECK_  */
#line 1873 "asmparse.y"
                                                              { (yyval.secAct) = dclLinktimeCheck; }
#line 8202 "prebuilt\\asmparse.cpp"
    break;

  case 753: /* secAction: INHERITCHECK_  */
#line 1874 "asmparse.y"
                                                              { (yyval.secAct) = dclInheritanceCheck; }
#line 8208 "prebuilt\\asmparse.cpp"
    break;

  case 754: /* secAction: REQMIN_  */
#line 1875 "asmparse.y"
                                                              { (yyval.secAct) = dclRequestMinimum; }
#line 8214 "prebuilt\\asmparse.cpp"
    break;

  case 755: /* secAction: REQOPT_  */
#line 1876 "asmparse.y"
                                                              { (yyval.secAct) = dclRequestOptional; }
#line 8220 "prebuilt\\asmparse.cpp"
    break;

  case 756: /* secAction: REQREFUSE_  */
#line 1877 "asmparse.y"
                                                              { (yyval.secAct) = dclRequestRefuse; }
#line 8226 "prebuilt\\asmparse.cpp"
    break;

  case 757: /* secAction: PREJITGRANT_  */
#line 1878 "asmparse.y"
                                                              { (yyval.secAct) = dclPrejitGrant; }
#line 8232 "prebuilt\\asmparse.cpp"
    break;

  case 758: /* secAction: PREJITDENY_  */
#line 1879 "asmparse.y"
                                                              { (yyval.secAct) = dclPrejitDenied; }
#line 8238 "prebuilt\\asmparse.cpp"
    break;

  case 759: /* secAction: NONCASDEMAND_  */
#line 1880 "asmparse.y"
                                                              { (yyval.secAct) = dclNonCasDemand; }
#line 8244 "prebuilt\\asmparse.cpp"
    break;

  case 760: /* secAction: NONCASLINKDEMAND_  */
#line 1881 "asmparse.y"
                                                              { (yyval.secAct) = dclNonCasLinkDemand; }
#line 8250 "prebuilt\\asmparse.cpp"
    break;

  case 761: /* secAction: NONCASINHERITANCE_  */
#line 1882 "asmparse.y"
                                                              { (yyval.secAct) = dclNonCasInheritance; }
#line 8256 "prebuilt\\asmparse.cpp"
    break;

  case 762: /* esHead: _LINE  */
#line 1886 "asmparse.y"
                                                              { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = FALSE; }
#line 8262 "prebuilt\\asmparse.cpp"
    break;

  case 763: /* esHead: P_LINE  */
#line 1887 "asmparse.y"
                                                              { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = TRUE; }
#line 8268 "prebuilt\\asmparse.cpp"
    break;

  case 764: /* extSourceSpec: esHead int32 SQSTRING  */
#line 1890 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-1].int32);
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8276 "prebuilt\\asmparse.cpp"
    break;

  case 765: /* extSourceSpec: esHead int32  */
#line 1893 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[0].int32);
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1); }
#line 8283 "prebuilt\\asmparse.cpp"
    break;

  case 766: /* extSourceSpec: esHead int32 ':' int32 SQSTRING  */
#line 1895 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-3].int32);
                                                                PENV->nExtCol=(yyvsp[-1].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8291 "prebuilt\\asmparse.cpp"
    break;

  case 767: /* extSourceSpec: esHead int32 ':' int32  */
#line 1898 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-2].int32);
                                                                PENV->nExtCol=(yyvsp[0].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);}
#line 8298 "prebuilt\\asmparse.cpp"
    break;

  case 768: /* extSourceSpec: esHead int32 ':' int32 ',' int32 SQSTRING  */
#line 1901 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-5].int32);
                                                                PENV->nExtCol=(yyvsp[-3].int32); PENV->nExtColEnd = (yyvsp[-1].int32);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8306 "prebuilt\\asmparse.cpp"
    break;

  case 769: /* extSourceSpec: esHead int32 ':' int32 ',' int32  */
#line 1905 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-4].int32);
                                                                PENV->nExtCol=(yyvsp[-2].int32); PENV->nExtColEnd = (yyvsp[0].int32); }
#line 8313 "prebuilt\\asmparse.cpp"
    break;

  case 770: /* extSourceSpec: esHead int32 ',' int32 ':' int32 SQSTRING  */
#line 1908 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-5].int32); PENV->nExtLineEnd = (yyvsp[-3].int32);
                                                                PENV->nExtCol=(yyvsp[-1].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8321 "prebuilt\\asmparse.cpp"
    break;

  case 771: /* extSourceSpec: esHead int32 ',' int32 ':' int32  */
#line 1912 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-4].int32); PENV->nExtLineEnd = (yyvsp[-2].int32);
                                                                PENV->nExtCol=(yyvsp[0].int32); PENV->nExtColEnd = static_cast<unsigned>(-1); }
#line 8328 "prebuilt\\asmparse.cpp"
    break;

  case 772: /* extSourceSpec: esHead int32 ',' int32 ':' int32 ',' int32 SQSTRING  */
#line 1915 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-7].int32); PENV->nExtLineEnd = (yyvsp[-5].int32);
                                                                PENV->nExtCol=(yyvsp[-3].int32); PENV->nExtColEnd = (yyvsp[-1].int32);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8336 "prebuilt\\asmparse.cpp"
    break;

  case 773: /* extSourceSpec: esHead int32 ',' int32 ':' int32 ',' int32  */
#line 1919 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-6].int32); PENV->nExtLineEnd = (yyvsp[-4].int32);
                                                                PENV->nExtCol=(yyvsp[-2].int32); PENV->nExtColEnd = (yyvsp[0].int32); }
#line 8343 "prebuilt\\asmparse.cpp"
    break;

  case 774: /* extSourceSpec: esHead int32 QSTRING  */
#line 1921 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-1].int32) - 1;
                                                                PENV->nExtCol = 0; PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].binstr));}
#line 8351 "prebuilt\\asmparse.cpp"
    break;

  case 775: /* fileDecl: _FILE fileAttr dottedName fileEntry hashHead bytes ')' fileEntry  */
#line 1928 "asmparse.y"
                                                              { PASMM->AddFile((yyvsp[-5].string), (yyvsp[-6].fileAttr)|(yyvsp[-4].fileAttr)|(yyvsp[0].fileAttr), (yyvsp[-2].binstr)); }
#line 8357 "prebuilt\\asmparse.cpp"
    break;

  case 776: /* fileDecl: _FILE fileAttr dottedName fileEntry  */
#line 1929 "asmparse.y"
                                                              { PASMM->AddFile((yyvsp[-1].string), (yyvsp[-2].fileAttr)|(yyvsp[0].fileAttr), NULL); }
#line 8363 "prebuilt\\asmparse.cpp"
    break;

  case 777: /* fileAttr: %empty  */
#line 1932 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0; }
#line 8369 "prebuilt\\asmparse.cpp"
    break;

  case 778: /* fileAttr: fileAttr NOMETADATA_  */
#line 1933 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) ((yyvsp[-1].fileAttr) | ffContainsNoMetaData); }
#line 8375 "prebuilt\\asmparse.cpp"
    break;

  case 779: /* fileEntry: %empty  */
#line 1936 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0; }
#line 8381 "prebuilt\\asmparse.cpp"
    break;

  case 780: /* fileEntry: _ENTRYPOINT  */
#line 1937 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0x80000000; }
#line 8387 "prebuilt\\asmparse.cpp"
    break;

  case 781: /* hashHead: _HASH '=' '('  */
#line 1940 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8393 "prebuilt\\asmparse.cpp"
    break;

  case 782: /* assemblyHead: _ASSEMBLY asmAttr dottedName  */
#line 1943 "asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[0].string), NULL, (DWORD)(yyvsp[-1].asmAttr), FALSE); }
#line 8399 "prebuilt\\asmparse.cpp"
    break;

  case 783: /* asmAttr: %empty  */
#line 1946 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) 0; }
#line 8405 "prebuilt\\asmparse.cpp"
    break;

  case 784: /* asmAttr: asmAttr RETARGETABLE_  */
#line 1947 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afRetargetable); }
#line 8411 "prebuilt\\asmparse.cpp"
    break;

  case 785: /* asmAttr: asmAttr WINDOWSRUNTIME_  */
#line 1948 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afContentType_WindowsRuntime); }
#line 8417 "prebuilt\\asmparse.cpp"
    break;

  case 786: /* asmAttr: asmAttr NOPLATFORM_  */
#line 1949 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afPA_NoPlatform); }
#line 8423 "prebuilt\\asmparse.cpp"
    break;

  case 787: /* asmAttr: asmAttr LEGACY_ LIBRARY_  */
#line 1950 "asmparse.y"
                                                              { (yyval.asmAttr) = (yyvsp[-2].asmAttr); }
#line 8429 "prebuilt\\asmparse.cpp"
    break;

  case 788: /* asmAttr: asmAttr CIL_  */
#line 1951 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_MSIL); }
#line 8435 "prebuilt\\asmparse.cpp"
    break;

  case 789: /* asmAttr: asmAttr X86_  */
#line 1952 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_x86); }
#line 8441 "prebuilt\\asmparse.cpp"
    break;

  case 790: /* asmAttr: asmAttr AMD64_  */
#line 1953 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_AMD64); }
#line 8447 "prebuilt\\asmparse.cpp"
    break;

  case 791: /* asmAttr: asmAttr ARM_  */
#line 1954 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_ARM); }
#line 8453 "prebuilt\\asmparse.cpp"
    break;

  case 792: /* asmAttr: asmAttr ARM64_  */
#line 1955 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_ARM64); }
#line 8459 "prebuilt\\asmparse.cpp"
    break;

  case 795: /* assemblyDecl: _HASH ALGORITHM_ int32  */
#line 1962 "asmparse.y"
                                                              { PASMM->SetAssemblyHashAlg((yyvsp[0].int32)); }
#line 8465 "prebuilt\\asmparse.cpp"
    break;

  case 798: /* intOrWildcard: int32  */
#line 1967 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 8471 "prebuilt\\asmparse.cpp"
    break;

  case 799: /* intOrWildcard: '*'  */
#line 1968 "asmparse.y"
                                                              { (yyval.int32) = 0xFFFF; }
#line 8477 "prebuilt\\asmparse.cpp"
    break;

  case 800: /* asmOrRefDecl: publicKeyHead bytes ')'  */
#line 1971 "asmparse.y"
                                                              { PASMM->SetAssemblyPublicKey((yyvsp[-1].binstr)); }
#line 8483 "prebuilt\\asmparse.cpp"
    break;

  case 801: /* asmOrRefDecl: _VER intOrWildcard ':' intOrWildcard ':' intOrWildcard ':' intOrWildcard  */
#line 1973 "asmparse.y"
                                                              { PASMM->SetAssemblyVer((USHORT)(yyvsp[-6].int32), (USHORT)(yyvsp[-4].int32), (USHORT)(yyvsp[-2].int32), (USHORT)(yyvsp[0].int32)); }
#line 8489 "prebuilt\\asmparse.cpp"
    break;

  case 802: /* asmOrRefDecl: _LOCALE compQstring  */
#line 1974 "asmparse.y"
                                                              { (yyvsp[0].binstr)->appendInt8(0); PASMM->SetAssemblyLocale((yyvsp[0].binstr),TRUE); }
#line 8495 "prebuilt\\asmparse.cpp"
    break;

  case 803: /* asmOrRefDecl: localeHead bytes ')'  */
#line 1975 "asmparse.y"
                                                              { PASMM->SetAssemblyLocale((yyvsp[-1].binstr),FALSE); }
#line 8501 "prebuilt\\asmparse.cpp"
    break;

  case 806: /* publicKeyHead: _PUBLICKEY '=' '('  */
#line 1980 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8507 "prebuilt\\asmparse.cpp"
    break;

  case 807: /* publicKeyTokenHead: _PUBLICKEYTOKEN '=' '('  */
#line 1983 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8513 "prebuilt\\asmparse.cpp"
    break;

  case 808: /* localeHead: _LOCALE '=' '('  */
#line 1986 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8519 "prebuilt\\asmparse.cpp"
    break;

  case 809: /* assemblyRefHead: _ASSEMBLY EXTERN_ asmAttr dottedName  */
#line 1990 "asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[0].string), NULL, (yyvsp[-1].asmAttr), TRUE); }
#line 8525 "prebuilt\\asmparse.cpp"
    break;

  case 810: /* assemblyRefHead: _ASSEMBLY EXTERN_ asmAttr dottedName AS_ dottedName  */
#line 1992 "asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[-2].string), (yyvsp[0].string), (yyvsp[-3].asmAttr), TRUE); }
#line 8531 "prebuilt\\asmparse.cpp"
    break;

  case 813: /* assemblyRefDecl: hashHead bytes ')'  */
#line 1999 "asmparse.y"
                                                              { PASMM->SetAssemblyHashBlob((yyvsp[-1].binstr)); }
#line 8537 "prebuilt\\asmparse.cpp"
    break;

  case 815: /* assemblyRefDecl: publicKeyTokenHead bytes ')'  */
#line 2001 "asmparse.y"
                                                              { PASMM->SetAssemblyPublicKeyToken((yyvsp[-1].binstr)); }
#line 8543 "prebuilt\\asmparse.cpp"
    break;

  case 816: /* assemblyRefDecl: AUTO_  */
#line 2002 "asmparse.y"
                                                              { PASMM->SetAssemblyAutodetect(); }
#line 8549 "prebuilt\\asmparse.cpp"
    break;

  case 817: /* exptypeHead: _CLASS EXTERN_ exptAttr dottedName  */
#line 2005 "asmparse.y"
                                                              { PASMM->StartComType((yyvsp[0].string), (yyvsp[-1].exptAttr));}
#line 8555 "prebuilt\\asmparse.cpp"
    break;

  case 818: /* exportHead: _EXPORT exptAttr dottedName  */
#line 2008 "asmparse.y"
                                                                              { PASMM->StartComType((yyvsp[0].string), (yyvsp[-1].exptAttr)); }
#line 8561 "prebuilt\\asmparse.cpp"
    break;

  case 819: /* exptAttr: %empty  */
#line 2011 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) 0; }
#line 8567 "prebuilt\\asmparse.cpp"
    break;

  case 820: /* exptAttr: exptAttr PRIVATE_  */
#line 2012 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdNotPublic); }
#line 8573 "prebuilt\\asmparse.cpp"
    break;

  case 821: /* exptAttr: exptAttr PUBLIC_  */
#line 2013 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdPublic); }
#line 8579 "prebuilt\\asmparse.cpp"
    break;

  case 822: /* exptAttr: exptAttr FORWARDER_  */
#line 2014 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdForwarder); }
#line 8585 "prebuilt\\asmparse.cpp"
    break;

  case 823: /* exptAttr: exptAttr NESTED_ PUBLIC_  */
#line 2015 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedPublic); }
#line 8591 "prebuilt\\asmparse.cpp"
    break;

  case 824: /* exptAttr: exptAttr NESTED_ PRIVATE_  */
#line 2016 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedPrivate); }
#line 8597 "prebuilt\\asmparse.cpp"
    break;

  case 825: /* exptAttr: exptAttr NESTED_ FAMILY_  */
#line 2017 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamily); }
#line 8603 "prebuilt\\asmparse.cpp"
    break;

  case 826: /* exptAttr: exptAttr NESTED_ ASSEMBLY_  */
#line 2018 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedAssembly); }
#line 8609 "prebuilt\\asmparse.cpp"
    break;

  case 827: /* exptAttr: exptAttr NESTED_ FAMANDASSEM_  */
#line 2019 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamANDAssem); }
#line 8615 "prebuilt\\asmparse.cpp"
    break;

  case 828: /* exptAttr: exptAttr NESTED_ FAMORASSEM_  */
#line 2020 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamORAssem); }
#line 8621 "prebuilt\\asmparse.cpp"
    break;

  case 831: /* exptypeDecl: _FILE dottedName  */
#line 2027 "asmparse.y"
                                                              { PASMM->SetComTypeFile((yyvsp[0].string)); }
#line 8627 "prebuilt\\asmparse.cpp"
    break;

  case 832: /* exptypeDecl: _CLASS EXTERN_ slashedName  */
#line 2028 "asmparse.y"
                                                               { PASMM->SetComTypeComType((yyvsp[0].string)); }
#line 8633 "prebuilt\\asmparse.cpp"
    break;

  case 833: /* exptypeDecl: _ASSEMBLY EXTERN_ dottedName  */
#line 2029 "asmparse.y"
                                                              { PASMM->SetComTypeAsmRef((yyvsp[0].string)); }
#line 8639 "prebuilt\\asmparse.cpp"
    break;

  case 834: /* exptypeDecl: MDTOKEN_ '(' int32 ')'  */
#line 2030 "asmparse.y"
                                                              { if(!PASMM->SetComTypeImplementationTok((yyvsp[-1].int32)))
                                                                  PASM->report->error("Invalid implementation of exported type\n"); }
#line 8646 "prebuilt\\asmparse.cpp"
    break;

  case 835: /* exptypeDecl: _CLASS int32  */
#line 2032 "asmparse.y"
                                                              { if(!PASMM->SetComTypeClassTok((yyvsp[0].int32)))
                                                                  PASM->report->error("Invalid TypeDefID of exported type\n"); }
#line 8653 "prebuilt\\asmparse.cpp"
    break;

  case 838: /* manifestResHead: _MRESOURCE manresAttr dottedName  */
#line 2038 "asmparse.y"
                                                              { PASMM->StartManifestRes((yyvsp[0].string), (yyvsp[0].string), (yyvsp[-1].manresAttr)); }
#line 8659 "prebuilt\\asmparse.cpp"
    break;

  case 839: /* manifestResHead: _MRESOURCE manresAttr dottedName AS_ dottedName  */
#line 2040 "asmparse.y"
                                                              { PASMM->StartManifestRes((yyvsp[-2].string), (yyvsp[0].string), (yyvsp[-3].manresAttr)); }
#line 8665 "prebuilt\\asmparse.cpp"
    break;

  case 840: /* manresAttr: %empty  */
#line 2043 "asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) 0; }
#line 8671 "prebuilt\\asmparse.cpp"
    break;

  case 841: /* manresAttr: manresAttr PUBLIC_  */
#line 2044 "asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) ((yyvsp[-1].manresAttr) | mrPublic); }
#line 8677 "prebuilt\\asmparse.cpp"
    break;

  case 842: /* manresAttr: manresAttr PRIVATE_  */
#line 2045 "asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) ((yyvsp[-1].manresAttr) | mrPrivate); }
#line 8683 "prebuilt\\asmparse.cpp"
    break;

  case 845: /* manifestResDecl: _FILE dottedName AT_ int32  */
#line 2052 "asmparse.y"
                                                              { PASMM->SetManifestResFile((yyvsp[-2].string), (ULONG)(yyvsp[0].int32)); }
#line 8689 "prebuilt\\asmparse.cpp"
    break;

  case 846: /* manifestResDecl: _ASSEMBLY EXTERN_ dottedName  */
#line 2053 "asmparse.y"
                                                              { PASMM->SetManifestResAsmRef((yyvsp[0].string)); }
#line 8695 "prebuilt\\asmparse.cpp"
    break;


#line 8699 "prebuilt\\asmparse.cpp"

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

#line 2058 "asmparse.y"


#include "grammar_after.cpp"
