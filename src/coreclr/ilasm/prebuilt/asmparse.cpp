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
#define YYLAST   5505

/* YYNTOKENS -- Number of terminals.  */
#define YYNTOKENS  309
/* YYNNTS -- Number of nonterminals.  */
#define YYNNTS  186
/* YYNRULES -- Number of rules.  */
#define YYNRULES  862
/* YYNSTATES -- Number of states.  */
#define YYNSTATES  1607

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
     230,   231,   234,   235,   238,   239,   241,   246,   248,   249,
     250,   251,   252,   253,   254,   255,   256,   257,   258,   259,
     260,   261,   262,   263,   266,   267,   268,   271,   274,   275,
     278,   279,   280,   284,   285,   286,   287,   288,   293,   294,
     295,   296,   299,   302,   303,   307,   308,   312,   313,   314,
     315,   318,   319,   320,   322,   325,   328,   334,   337,   338,
     342,   348,   349,   351,   354,   355,   361,   364,   365,   368,
     372,   373,   381,   382,   383,   384,   386,   388,   393,   394,
     395,   402,   406,   407,   408,   409,   410,   411,   414,   417,
     421,   424,   427,   433,   436,   437,   438,   439,   440,   441,
     442,   443,   444,   445,   446,   447,   448,   449,   450,   451,
     452,   453,   454,   455,   456,   457,   458,   459,   460,   461,
     462,   465,   466,   469,   470,   473,   474,   477,   478,   482,
     483,   486,   487,   490,   491,   494,   495,   496,   497,   498,
     499,   500,   503,   504,   507,   508,   511,   512,   515,   518,
     519,   522,   526,   530,   531,   532,   533,   534,   535,   536,
     537,   538,   539,   540,   541,   547,   556,   557,   558,   563,
     569,   570,   571,   578,   583,   584,   585,   586,   587,   588,
     589,   590,   602,   604,   605,   606,   607,   608,   609,   610,
     613,   614,   617,   618,   621,   622,   626,   643,   649,   665,
     670,   671,   672,   675,   676,   677,   678,   681,   682,   683,
     684,   685,   686,   687,   688,   691,   694,   699,   703,   707,
     709,   711,   716,   717,   721,   722,   723,   726,   727,   730,
     731,   732,   733,   734,   735,   736,   737,   741,   747,   748,
     749,   752,   753,   757,   758,   759,   760,   761,   762,   763,
     767,   773,   774,   777,   778,   781,   784,   800,   801,   802,
     803,   804,   805,   806,   807,   808,   809,   810,   811,   812,
     813,   814,   815,   816,   817,   818,   819,   820,   823,   826,
     831,   832,   833,   834,   835,   836,   837,   838,   839,   840,
     841,   842,   843,   844,   845,   846,   849,   850,   851,   854,
     855,   856,   857,   858,   861,   862,   863,   864,   865,   866,
     867,   868,   869,   870,   871,   872,   873,   874,   875,   876,
     877,   880,   884,   885,   888,   889,   890,   891,   893,   896,
     897,   898,   899,   900,   901,   902,   903,   904,   905,   906,
     916,   926,   928,   931,   938,   939,   944,   950,   951,   953,
     974,   977,   981,   984,   985,   988,   989,   990,   994,   999,
    1000,  1001,  1002,  1006,  1007,  1009,  1013,  1017,  1022,  1026,
    1030,  1031,  1032,  1037,  1040,  1041,  1044,  1045,  1046,  1049,
    1050,  1053,  1054,  1057,  1058,  1063,  1064,  1065,  1066,  1073,
    1080,  1087,  1094,  1102,  1110,  1111,  1112,  1113,  1114,  1115,
    1119,  1122,  1124,  1126,  1128,  1130,  1132,  1134,  1136,  1138,
    1140,  1142,  1144,  1146,  1148,  1150,  1152,  1154,  1156,  1160,
    1163,  1164,  1167,  1168,  1172,  1173,  1174,  1179,  1180,  1181,
    1183,  1185,  1187,  1188,  1189,  1193,  1197,  1201,  1205,  1209,
    1213,  1217,  1221,  1225,  1229,  1233,  1237,  1241,  1245,  1249,
    1253,  1257,  1261,  1268,  1269,  1271,  1275,  1276,  1278,  1282,
    1283,  1287,  1288,  1291,  1292,  1295,  1296,  1299,  1300,  1304,
    1305,  1306,  1310,  1311,  1312,  1314,  1318,  1319,  1323,  1329,
    1332,  1335,  1338,  1341,  1344,  1347,  1355,  1358,  1361,  1364,
    1367,  1370,  1373,  1377,  1378,  1379,  1380,  1381,  1382,  1383,
    1384,  1393,  1394,  1395,  1402,  1410,  1418,  1424,  1430,  1436,
    1440,  1441,  1443,  1445,  1449,  1455,  1458,  1459,  1460,  1461,
    1462,  1466,  1467,  1470,  1471,  1474,  1475,  1479,  1480,  1483,
    1484,  1487,  1488,  1489,  1493,  1494,  1495,  1496,  1497,  1498,
    1499,  1500,  1503,  1509,  1516,  1517,  1520,  1521,  1522,  1523,
    1527,  1528,  1535,  1541,  1543,  1546,  1548,  1549,  1551,  1553,
    1554,  1555,  1556,  1557,  1558,  1559,  1560,  1561,  1562,  1563,
    1564,  1565,  1566,  1567,  1568,  1569,  1571,  1573,  1578,  1583,
    1586,  1588,  1590,  1591,  1592,  1593,  1594,  1596,  1598,  1600,
    1601,  1603,  1606,  1610,  1611,  1612,  1613,  1615,  1616,  1617,
    1618,  1619,  1620,  1621,  1622,  1625,  1626,  1629,  1630,  1631,
    1632,  1633,  1634,  1635,  1636,  1637,  1638,  1639,  1640,  1641,
    1642,  1643,  1644,  1645,  1646,  1647,  1648,  1649,  1650,  1651,
    1652,  1653,  1654,  1655,  1656,  1657,  1658,  1659,  1660,  1661,
    1662,  1663,  1664,  1665,  1666,  1667,  1668,  1669,  1670,  1671,
    1672,  1673,  1674,  1675,  1676,  1677,  1681,  1687,  1688,  1689,
    1690,  1691,  1692,  1693,  1694,  1695,  1697,  1699,  1706,  1713,
    1719,  1725,  1740,  1755,  1756,  1757,  1758,  1759,  1760,  1761,
    1764,  1765,  1766,  1767,  1768,  1769,  1770,  1771,  1772,  1773,
    1774,  1775,  1776,  1777,  1778,  1779,  1780,  1781,  1784,  1785,
    1788,  1789,  1790,  1791,  1794,  1798,  1800,  1802,  1803,  1804,
    1806,  1815,  1816,  1817,  1820,  1823,  1828,  1829,  1833,  1834,
    1837,  1840,  1841,  1844,  1847,  1850,  1853,  1857,  1863,  1869,
    1875,  1883,  1884,  1885,  1886,  1887,  1888,  1889,  1890,  1891,
    1892,  1893,  1894,  1895,  1896,  1897,  1901,  1902,  1905,  1908,
    1910,  1913,  1915,  1919,  1922,  1926,  1929,  1933,  1936,  1942,
    1944,  1947,  1948,  1951,  1952,  1955,  1958,  1961,  1962,  1963,
    1964,  1965,  1966,  1967,  1968,  1969,  1970,  1973,  1974,  1977,
    1978,  1979,  1982,  1983,  1986,  1987,  1989,  1990,  1991,  1992,
    1995,  1998,  2001,  2004,  2006,  2010,  2011,  2014,  2015,  2016,
    2017,  2020,  2023,  2026,  2027,  2028,  2029,  2030,  2031,  2032,
    2033,  2034,  2035,  2038,  2039,  2042,  2043,  2044,  2045,  2047,
    2049,  2050,  2053,  2054,  2058,  2059,  2060,  2063,  2064,  2067,
    2068,  2069,  2070
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

#define YYPACT_NINF (-1345)

#define yypact_value_is_default(Yyn) \
  ((Yyn) == YYPACT_NINF)

#define YYTABLE_NINF (-575)

#define yytable_value_is_error(Yyn) \
  0

/* YYPACT[STATE-NUM] -- Index in YYTABLE of the portion describing
   STATE-NUM.  */
static const yytype_int16 yypact[] =
{
   -1345,  1188, -1345, -1345,   -80,  5281, -1345,   -75,    61,  2078,
    2078, -1345, -1345,   240,   223,   -67,   -30,   -20,    50, -1345,
    4471,   275,   275,   141,   141,  1282,   -16, -1345,  5281,  5281,
    5281,  5281, -1345, -1345,   293, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345,   290,   290, -1345, -1345, -1345, -1345,   290,    15,
   -1345,   294,    89, -1345, -1345, -1345, -1345,   129, -1345,   290,
     275, -1345, -1345,    97,   107,   130,   135, -1345, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345,   128,   275, -1345,
   -1345, -1345,  4538, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,  2294,    33,
      99, -1345, -1345,   168,   175, -1345, -1345,   451,   132,   132,
    2151,   180, -1345,  4333, -1345, -1345,   207,   275,   275,  4880,
   -1345,  4826,  5140,  5281,   128, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345, -1345,  4333, -1345, -1345, -1345,   614, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345,  3190, -1345,   453,  3190,   164, -1345,  3628, -1345, -1345,
   -1345,   885,   733,   128,   377,   394, -1345,   397,  1446,   415,
     214,   359, -1345,  3190,    64,   128,   128,   128, -1345, -1345,
     261,   553,   274,   281, -1345,  5035,  2294,   537, -1345,  5380,
    3613,   299,    62,    86,    94,   162,   169,   186,   317,   178,
     322, -1345, -1345,   290,   324,    42, -1345, -1345, -1345, -1345,
    5130,  5281,   323,  4163,   334,  2832, -1345,   132, -1345,   -13,
     221, -1345,   360,   -33,   365,   660,   275,   275, -1345, -1345,
   -1345, -1345, -1345, -1345,   374, -1345, -1345,    57,   164,  1535,
   -1345,   382, -1345, -1345,   -45,  4826, -1345, -1345, -1345,    31,
     472, -1345, -1345, -1345, -1345,   128, -1345, -1345,   -53,   128,
     221, -1345, -1345, -1345, -1345, -1345,  3190, -1345,   663, -1345,
   -1345, -1345, -1345,  1843,  5281,   401,  -146,   413,  5238,   128,
   -1345,  5281,  5281,  5281, -1345,  4333,  5281,  5281, -1345,   422,
     424,  5281,    38,  4333, -1345, -1345,   430,  3190,   365, -1345,
   -1345, -1345, -1345,  4271,   434, -1345, -1345, -1345, -1345, -1345,
   -1345,   416, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345,  -102, -1345,  2294, -1345,  4502,
     441, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,   445, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345, -1345,   275, -1345,   275, -1345, -1345, -1345,
     275,   426,   -54,  2452, -1345, -1345, -1345,   439, -1345, -1345,
     -78, -1345, -1345, -1345, -1345,   531,  2286, -1345, -1345,  2587,
     275,   141,   108,  2587,  1446,  3572,  2294,   140,   132,  2151,
     449,   290, -1345, -1345, -1345,   454,   275,   275, -1345,   275,
   -1345,   275, -1345,   141, -1345,   138, -1345,   138, -1345, -1345,
     466,   458,  4538,   464, -1345, -1345, -1345,   275,   275,  1460,
    1144,  1632,  2147, -1345, -1345, -1345,   721,   128,   128, -1345,
     473, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345, -1345,   474,  2431, -1345,  5281,   -86,  4333,
     755,   481, -1345,  2595, -1345,   765,   482,   478,   489,  1446,
   -1345, -1345,   365, -1345, -1345,  2850,    46,   476,   769, -1345,
   -1345,   581,   -51, -1345,  5281, -1345, -1345,    46,   772,   -29,
    5281,  5281,  5281,   128, -1345,   128,   128,   128,  1685,   128,
     128,  2294,  2294,   128, -1345, -1345,   775,   -81, -1345,   490,
     506,   221, -1345, -1345, -1345,   275, -1345, -1345, -1345, -1345,
   -1345, -1345,   187, -1345,   507, -1345,   690, -1345, -1345, -1345,
     275,   275, -1345,   -35,  2753, -1345, -1345, -1345, -1345,   516,
   -1345, -1345,   518,   523, -1345, -1345, -1345, -1345,   525,   275,
     755,  3989, -1345, -1345,   513,   275,   892,  3285,   275,   132,
     804, -1345,   529,    79,  3795, -1345,  2294, -1345, -1345, -1345,
     531,    13,  2286,    13,    13,    13,   768,   776, -1345, -1345,
   -1345, -1345, -1345, -1345,   536,   551, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345,  1843, -1345,   552,   365,   290,
    4333, -1345,  2587,   555,   755,   556,   548,   557,   558,   560,
     571,   573, -1345,   178,   574, -1345,   545,    43,   645,   575,
      29,    34, -1345, -1345, -1345, -1345, -1345, -1345,   290,   290,
   -1345,   577,   578, -1345,   290, -1345,   290, -1345,   582,    59,
    5281,   656, -1345, -1345, -1345, -1345,  5281,   662, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,   275,  4789,
      -1,   -27,  5281,   477,    21,   583,   587, -1345,  3274,   584,
     593,   592, -1345,   878, -1345, -1345,   589,   597,  4479,  4278,
     594,   600,  5184,   633,   290,  5281,   128,  5281,  5281,   214,
     214,   214,   601,   596,   603,   275,   176, -1345, -1345,  4333,
     605,   607, -1345, -1345, -1345, -1345, -1345, -1345,   187,  3976,
     606,  2294,  2294,  1993,   739, -1345, -1345,  5130,  3292,  3387,
     132,   886, -1345, -1345, -1345,  3969, -1345,   611,    -3,   467,
     188,   404,   275,   609,   275,   128,   275,  -100,   612,  4333,
    5184,    79, -1345,  3989,   615,   624, -1345, -1345, -1345, -1345,
    2587, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,  4538,
     275,   275,   141,    46,   900,   755,   625,   460,   627,   629,
     631, -1345,    24,   632, -1345,   632,   632,   632,   632,   632,
   -1345, -1345,   275, -1345,   275,   275,   635, -1345, -1345,   628,
     638,   365,   640,   641,   639,   643,   644,   646,   275,  5281,
   -1345,   128,  5281,    16,  5281,   647, -1345, -1345, -1345,   519,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345,   649,   698,   713, -1345,   702,   670,   -64,   929,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
     649,   649, -1345,  4385, -1345, -1345, -1345, -1345,   661,   290,
      18,  4538,   668,  5281,  2960, -1345,   755,   679,   674,   693,
   -1345,  2595, -1345,    65, -1345,   210,   224,   793,   238,   241,
     254,   264,   265,   300,   311,   312,   338,   353,   354,   355,
     368, -1345,   954, -1345,   290, -1345,   275,   687,    79,    79,
     128,   476, -1345, -1345,  4538, -1345, -1345, -1345,   694,   128,
     128,   214,    79, -1345, -1345, -1345, -1345,   221, -1345,   275,
   -1345,  2294,   -60,  5281, -1345, -1345,   795, -1345, -1345,   105,
    5281, -1345, -1345,  4333,   128,   275,   128,   275,    48,  4333,
    5184,  4554,  1324,  1940, -1345,  2711, -1345,   755,   493,   703,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
     696,   730, -1345,   697,   707,   725,   735,   742,  5184, -1345,
     901,   740,   743,  2294,   668,  1843, -1345,   748,   404, -1345,
    1019,   983,   984, -1345, -1345,   754,   756,  5281,    84, -1345,
      79,  2587,  2587, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345,    44,  1043, -1345, -1345,    29, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345,   757,   214,   128,   275,   128, -1345, -1345,
   -1345, -1345, -1345, -1345,   818, -1345, -1345, -1345, -1345,   755,
     774,   779, -1345, -1345, -1345, -1345, -1345,   706, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345,   143, -1345,    27,    51,
   -1345, -1345,  3441, -1345,   780, -1345, -1345,   365, -1345,   784,
   -1345, -1345, -1345, -1345,   791, -1345, -1345, -1345, -1345,   365,
     378,   275,   275,   275,   384,   385,   390,   396,   275,   275,
     275,   275,   275,   275,   141,   275,   565,   275,   562,   275,
     275,   275,   275,   275,   275,   275,   141,   275,  3528,   275,
     127,   275,  3090,   275, -1345, -1345, -1345,  5362,   778,   782,
   -1345,   787,   788,   796,   797, -1345,   921,   794,   801,   803,
     800, -1345,   187, -1345,   -60,  1446, -1345,   128,  2431,   802,
     805,  2294,  1843,   844, -1345,  1446,  1446,  1446,  1446, -1345,
   -1345, -1345, -1345, -1345, -1345,  1446,  1446,  1446, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345,   365, -1345,   275,   336,   486,
   -1345, -1345, -1345, -1345,  4789,   806,  4538, -1345,   811, -1345,
   -1345,  1087, -1345,  4538, -1345,  4538,   275, -1345, -1345,   128,
   -1345,   813, -1345, -1345, -1345,   275, -1345,   810, -1345, -1345,
     812,   406,   275,   275, -1345, -1345, -1345, -1345, -1345, -1345,
     755,   828, -1345, -1345,   275, -1345,   -71,   821,   843,   816,
     847,   849,   851,   852,   854,   855,   857,   859,   860,   861,
   -1345,   365, -1345, -1345,   275,   233, -1345,   745,   866,   863,
     864,   865,   867,   275,   275,   275,   275,   275,   275,   141,
     275,   869,   872,   870,   873,   879,   874,   880,   877,   887,
     888,   881,   889,   890,   882,   891,   893,   896,   894,   899,
     898,   903,   902,   904,   905,   907,   908,   909,   911,  1152,
     912,   913, -1345,  3111, -1345,  3450, -1345, -1345,   910, -1345,
   -1345,    79,    79, -1345, -1345, -1345, -1345,  2294, -1345, -1345,
     417, -1345,   918, -1345,  1170,   132, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345,   960,   919, -1345, -1345, -1345, -1345,   922,
     868, -1345,  2294,  5184, -1345, -1345, -1345, -1345,  1193,    29,
     275,   755,   917,   920,   365, -1345,   923,   275, -1345,   924,
     926,   927,   931,   928,   925,   930,   939,   934,   975, -1345,
   -1345, -1345,   932, -1345,   936,   950,   947,   952,   949,   956,
     953,   958,   955, -1345,   972, -1345,   973, -1345,   974, -1345,
     976, -1345, -1345,   986, -1345, -1345,   987, -1345,   988, -1345,
     989, -1345,   990, -1345,   991, -1345,   997, -1345, -1345,   998,
   -1345,  1000, -1345,   999,  1242, -1345,   970,   189, -1345,  1001,
    1003, -1345,    79,  2294,  5184,  4333, -1345, -1345, -1345,    79,
   -1345,   895, -1345,  1008,  1005,    76, -1345,  4828, -1345,   979,
   -1345,   275,   275,   275, -1345, -1345, -1345, -1345, -1345,  1012,
   -1345,  1020, -1345,  1026, -1345,  1031, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345,  3528, -1345, -1345,  1032, -1345,   895,  1843,  1033,
    1028,  1036, -1345,    29, -1345,   755, -1345,    18, -1345,  1038,
    1041,  1044,     8,    68, -1345, -1345, -1345, -1345,    78,    81,
      87,    98,    74,    71,    92,    95,   102,   100,  1034,    53,
    2978, -1345,   668,  1037,  1320, -1345,    79, -1345,   414, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345,   106,   121,   122,   104,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345,  1340, -1345, -1345, -1345,    79,  5184,  2000,
    1053,   755, -1345, -1345, -1345, -1345, -1345,  1058,  1062,  1067,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345,   412,  1107,    79,
     275, -1345,  1260,  1072,  1073,   132, -1345, -1345,  4333,  1843,
    1351,  5184,   895,  1077,    79,  1078, -1345
};

/* YYDEFACT[STATE-NUM] -- Default reduction number in state STATE-NUM.
   Performed when YYTABLE does not specify something else to do.  Zero
   means the default is an error.  */
static const yytype_int16 yydefact[] =
{
       2,     0,     1,   101,   121,     0,   280,   224,   406,     0,
       0,   776,   777,     0,   237,     0,     0,   791,   797,   854,
     108,     0,     0,     0,     0,     0,     0,    29,     0,     0,
       0,     0,    73,    74,     0,    76,     3,    25,    26,    27,
      99,   100,   450,   450,    19,    17,    10,     9,   450,     0,
     124,   151,     0,     7,   287,   352,     8,     0,    18,   450,
       0,    11,    12,     0,     0,     0,     0,   833,    37,    55,
      53,    42,    38,    47,    48,    49,    50,    51,    52,    39,
      40,    41,    43,    44,    45,    46,    54,   120,     0,   204,
     407,   408,   405,   761,   762,   763,   764,   765,   766,   767,
     768,   769,   770,   771,   772,   773,   774,   775,     0,     0,
      34,   231,   232,     0,     0,   238,   239,   244,   237,   237,
       0,    77,    87,     0,   235,   230,     0,     0,     0,     0,
     797,     0,     0,     0,   109,    57,    20,    21,    59,    58,
      23,    24,   570,   727,     0,   704,   712,   710,     0,   713,
     714,   715,   716,   717,   718,   723,   724,   725,   726,   687,
     711,     0,   703,     0,     0,    38,   508,     0,   571,   572,
     573,     0,     0,   574,     0,     0,   251,     0,   237,     0,
     568,     0,   708,    30,    68,    70,    71,    72,    75,   452,
       0,   451,     0,     0,     2,     0,     0,   153,   155,   237,
       0,     0,   413,   413,   413,   413,   413,   413,     0,     0,
       0,   403,   410,   450,     0,   779,   807,   825,   843,   857,
       0,     0,     0,     0,     0,     0,   569,   237,   576,   737,
     579,    32,     0,     0,   739,     0,     0,     0,   240,   241,
     242,   243,   233,   234,     0,    89,    88,     0,     0,     0,
     119,     0,    22,   792,   793,     0,   798,   799,   800,   802,
       0,   803,   804,   805,   806,   796,   855,   856,   852,   110,
     709,   719,   720,   721,   722,   686,     0,   689,     0,   705,
     707,   249,   250,     0,     0,     0,     0,     0,     0,   702,
     700,     0,     0,     0,   246,     0,     0,     0,   694,     0,
       0,     0,   730,   553,   693,   692,     0,    30,    69,    80,
     453,    84,   118,     0,     0,   127,   148,   125,   126,   129,
     130,     0,   131,   132,   133,   134,   135,   136,   137,   138,
     128,   147,   140,   139,   149,   163,   152,     0,   123,     0,
       0,   293,   288,   289,   290,   291,   292,   296,   294,   304,
     295,   297,   298,   299,   300,   301,   302,   303,     0,   305,
     329,   509,   510,   511,   512,   513,   514,   515,   516,   517,
     518,   519,   520,   521,     0,   388,     0,   351,   359,   360,
       0,     0,     0,     0,   381,     6,   366,     0,   368,   367,
       0,   353,   374,   352,   355,     0,     0,   361,   523,     0,
       0,     0,     0,     0,   237,     0,     0,     0,   237,     0,
       0,   450,   362,   364,   365,     0,     0,     0,   429,     0,
     428,     0,   427,     0,   426,     0,   424,     0,   425,   449,
       0,   412,     0,     0,   738,   788,   778,     0,     0,     0,
       0,     0,     0,   836,   835,   834,     0,   831,    56,   225,
       0,   211,   205,   206,   207,   208,   213,   214,   215,   216,
     210,   217,   218,   209,     0,     0,   404,     0,     0,     0,
       0,     0,   747,   741,   746,     0,    35,     0,     0,   237,
      91,    85,    78,   326,   327,   730,   328,   551,     0,   112,
     794,   790,   823,   801,     0,   688,   706,   248,     0,     0,
       0,     0,     0,   701,   699,    66,    67,    65,     0,    64,
     575,     0,     0,    63,   731,   690,   732,     0,   728,     0,
     554,   555,    28,    31,     5,     0,   141,   142,   143,   144,
     145,   146,   172,   122,   154,   158,     0,   121,   254,   268,
       0,     0,   833,     0,     0,     4,   196,   197,   190,     0,
     156,   186,     0,     0,   352,   187,   188,   189,     0,     0,
     310,     0,   354,   356,     0,     0,     0,     0,     0,   237,
       0,   363,     0,   329,     0,   398,     0,   396,   399,   382,
     384,     0,     0,     0,     0,     0,     0,     0,   385,   525,
     524,   526,   527,    60,     0,     0,   522,   529,   528,   532,
     531,   533,   537,   538,   536,     0,   539,     0,   540,   450,
       0,   544,   546,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   409,     0,     0,   417,     0,   781,     0,     0,
       0,     0,    13,   819,   818,   810,   808,   811,   450,   450,
     830,     0,     0,    14,   450,   828,   450,   826,     0,     0,
       0,     0,    15,   851,   850,   844,     0,     0,    16,   862,
     861,   858,   837,   838,   839,   840,   841,   842,     0,   580,
     220,     0,   577,     0,     0,     0,   748,    91,     0,     0,
       0,   742,    33,     0,   236,   245,    81,     0,    94,   553,
       0,     0,     0,     0,   450,     0,   853,     0,     0,   566,
     564,   565,   693,     0,     0,   734,   730,   691,   698,     0,
       0,     0,   167,   169,   168,   170,   165,   166,   172,     0,
       0,     0,     0,     0,   237,   191,   192,     0,     0,     0,
     237,     0,   155,   257,   271,     0,   843,     0,   310,     0,
       0,   281,     0,     0,     0,   376,     0,     0,     0,     0,
       0,   329,   561,     0,     0,   558,   559,   380,   397,   383,
       0,   400,   390,   394,   395,   393,   389,   391,   392,     0,
       0,     0,     0,   535,     0,     0,     0,     0,   549,   550,
       0,   530,     0,   413,   414,   413,   413,   413,   413,   413,
     411,   416,     0,   780,     0,     0,     0,   813,   812,     0,
       0,   816,     0,     0,     0,     0,     0,     0,     0,     0,
     849,   845,     0,     0,     0,     0,   634,   588,   589,     0,
     623,   590,   591,   592,   593,   594,   595,   625,   601,   602,
     603,   604,   635,     0,     0,   631,     0,     0,     0,   585,
     586,   587,   610,   611,   612,   629,   613,   614,   615,   616,
     635,   635,   619,   637,   627,   633,   596,   285,     0,     0,
     283,     0,   222,   578,     0,   735,     0,     0,    53,     0,
     740,   741,    36,     0,    79,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,    93,    90,   457,   450,    92,     0,     0,   329,   329,
     328,   551,   113,   114,     0,   115,   116,   117,     0,   824,
     247,   567,   329,   695,   696,   733,   729,   556,   150,     0,
     173,   159,   176,     0,   164,   157,     0,   256,   255,   574,
       0,   270,   269,     0,   832,     0,   199,     0,     0,     0,
       0,     0,     0,     0,   182,     0,   306,     0,     0,     0,
     317,   318,   319,   320,   312,   313,   314,   311,   315,   316,
       0,     0,   309,     0,     0,     0,     0,     0,     0,   371,
     369,     0,     0,     0,   222,     0,   372,     0,   281,   357,
     329,     0,     0,   386,   387,     0,     0,     0,     0,   542,
     329,   546,   546,   545,   415,   423,   422,   421,   420,   418,
     419,   785,   783,   809,   820,     0,   822,   814,   817,   795,
     821,   827,   829,     0,   846,   847,     0,   860,   219,   624,
     597,   598,   599,   600,     0,   620,   626,   628,   632,     0,
       0,     0,   630,   617,   618,   641,   642,     0,   669,   643,
     644,   645,   646,   647,   648,   671,   653,   654,   655,   656,
     639,   640,   661,   662,   663,   664,   665,   666,   667,   668,
     638,   672,   673,   674,   675,   676,   677,   678,   679,   680,
     681,   682,   683,   684,   685,   657,   621,   212,     0,     0,
     605,   221,     0,   203,     0,   751,   752,   756,   754,     0,
     753,   750,   749,   736,     0,    94,   743,    91,    86,    82,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,    97,    98,    96,     0,     0,     0,
     552,     0,     0,     0,     0,   111,   793,     0,     0,     0,
     160,   161,   172,   175,   176,   237,   202,   252,     0,     0,
       0,     0,     0,     0,   183,   237,   237,   237,   237,   184,
     265,   266,   264,   258,   263,   237,   237,   237,   185,   278,
     279,   276,   272,   277,   193,   310,   308,     0,     0,     0,
     330,   331,   332,   333,   580,   163,     0,   375,     0,   378,
     379,     0,   358,   562,   560,     0,     0,    61,    62,   534,
     541,     0,   547,   548,   784,     0,   782,     0,   848,   859,
       0,     0,     0,     0,   670,   649,   650,   651,   652,   659,
       0,     0,   660,   284,     0,   606,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
     456,   455,   454,   223,     0,     0,    94,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   104,     0,   103,     0,   102,   448,     0,   229,
     228,   329,   329,   789,   697,   171,   178,     0,   177,   174,
       0,   198,     0,   201,     0,   237,   259,   260,   261,   262,
     275,   273,   274,     0,     0,   321,   322,   323,   324,     0,
       0,   370,     0,     0,   563,   401,   402,   543,   787,     0,
       0,     0,     0,     0,   622,   658,     0,     0,   607,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,   744,
      83,   447,     0,   446,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   437,     0,   436,     0,   435,     0,   434,
       0,   432,   430,     0,   433,   431,     0,   445,     0,   444,
       0,   443,     0,   442,     0,   463,     0,   459,   458,     0,
     462,     0,   461,     0,     0,   106,     0,     0,   181,     0,
       0,   162,   329,     0,     0,     0,   307,   325,   282,   329,
     377,   179,   786,     0,     0,     0,   583,   580,   609,     0,
     755,     0,     0,     0,   760,   745,   497,   493,   441,     0,
     440,     0,   439,     0,   438,     0,   495,   493,   491,   489,
     483,   486,   495,   493,   491,   489,   506,   499,   460,   502,
     105,   107,     0,   227,   226,     0,   200,   179,     0,     0,
       0,     0,   180,     0,   636,     0,   582,   584,   608,     0,
       0,     0,     0,     0,   495,   493,   491,   489,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,    95,   222,     0,     0,   334,   329,   815,     0,   757,
     758,   759,   479,   498,   478,   494,     0,     0,     0,     0,
     469,   496,   468,   467,   492,   466,   490,   464,   485,   484,
     465,   488,   487,   473,   472,   471,   470,   482,   507,   501,
     500,   480,   503,     0,   481,   505,   267,   329,     0,     0,
       0,     0,   477,   476,   475,   474,   504,     0,     0,     0,
     339,   335,   344,   345,   346,   347,   348,   349,   336,   337,
     338,   340,   341,   342,   343,   286,   373,     0,     0,   329,
       0,   581,     0,     0,     0,   237,   194,   350,     0,     0,
       0,     0,   179,     0,   329,     0,   195
};

/* YYPGOTO[NTERM-NUM].  */
static const yytype_int16 yypgoto[] =
{
   -1345,  1179, -1345,  1081,  -106,    17,   -88,    -5,    10,    22,
    -416, -1345,    32,   -11,  1355, -1345, -1345,   914,   982,  -642,
   -1345,  -966, -1345,    11, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345,  -323, -1345, -1345, -1345,   664, -1345, -1345,
   -1345,   197, -1345,   676,   245,   247, -1345, -1344,  -445, -1345,
    -320, -1345, -1345,  -954, -1345,  -172,  -111, -1345,     3,  1374,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,   425,
     213, -1345,  -317, -1345,  -708,  -687,  1055, -1345, -1345,  -248,
   -1345,  -144, -1345, -1345,   824, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345,  -151,     9, -1345, -1345, -1345,   798,  -112,
    1359,   335,   -44,     0,   559, -1345, -1094, -1345, -1345, -1322,
   -1318, -1304, -1299, -1345, -1345, -1345, -1345,    12, -1345, -1345,
   -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,
   -1345, -1345, -1345,  -264,   521,   731, -1345,  -676, -1345,   443,
      19,  -447,  -107,   -12,  -103, -1345,   -23,   287, -1345,   727,
      20,   563, -1345, -1345,   570, -1345, -1064, -1345,  1427, -1345,
      26, -1345, -1345,   292,   948, -1345,  1310, -1345, -1345,  -976,
    1006, -1345, -1345, -1345, -1345, -1345, -1345, -1345, -1345,   906,
     709, -1345, -1345, -1345, -1345, -1345
};

/* YYDEFGOTO[NTERM-NUM].  */
static const yytype_int16 yydefgoto[] =
{
       0,     1,    36,   306,   674,   386,    86,   173,   798,  1536,
     598,    38,   388,    40,    41,    42,    43,   121,   244,   687,
     688,   892,  1137,   389,  1305,    45,    46,   693,    47,    48,
      49,    50,    51,    52,   195,   197,   338,   339,   534,  1149,
    1150,   533,   718,   719,   720,  1153,   923,  1481,  1482,   550,
      53,   223,   862,  1083,    89,   122,   123,   124,   226,   245,
     552,   723,   942,  1173,   553,   724,   943,  1182,    54,   968,
     858,   859,    55,   199,   739,   487,   753,  1559,   390,   200,
     391,   761,   393,   394,   579,   395,   396,   580,   581,   582,
     583,   584,   585,   762,   397,    57,    92,   211,   430,   418,
     431,   893,   894,   190,   191,  1253,   895,  1502,  1503,  1501,
    1500,  1493,  1498,  1492,  1509,  1510,  1508,   227,   398,   399,
     400,   401,   402,   403,   404,   405,   406,   407,   408,   409,
     410,   411,   412,   780,   691,   519,   520,   754,   755,   756,
     228,   180,   246,   860,  1025,  1076,   230,   182,   517,   518,
     413,   680,   681,    59,   675,   676,  1090,  1091,   108,    60,
     414,    62,   129,   491,   644,    63,   131,   439,   636,   799,
     637,   638,   646,   639,    64,   440,   647,    65,   558,   220,
     441,   655,    66,   132,   442,   661
};

/* YYTABLE[YYPACT[STATE-NUM]] -- What to do in state STATE-NUM.  If
   positive, shift that token.  If negative, reduce the rule whose
   number is the opposite.  If YYTABLE_NINF, syntax error.  */
static const yytype_int16 yytable[] =
{
      87,   229,   181,   234,   224,   901,   294,   242,   243,   620,
      56,   621,    44,   213,   174,   134,   549,   125,    37,   551,
    1200,    58,   554,   184,   185,   186,   187,    61,   176,  1217,
     948,   136,   137,    39,  1293,   867,  1259,   178,   135,   310,
     231,   231,   690,   192,   179,   140,   141,   135,   193,   435,
     436,   793,  1214,   699,   700,   701,   392,   566,   514,   214,
     135,  1549,   861,   976,   231,  -574,   232,   295,   135,   760,
     215,   231,   231,  1085,  1086,   977,   728,   135,   308,  1016,
     138,   139,   593,   135,   494,   593,   695,   135,   360,   336,
     135,   420,   422,   424,   426,   428,   135,  1550,   222,   752,
     249,   135,   594,   595,   135,   594,   595,   138,   139,   138,
     139,   135,   387,   138,   139,   135,   469,   138,   139,   593,
     572,   270,   490,   176,   254,    90,   265,   268,   269,  1257,
     135,   135,   178,  1513,   947,  1295,    67,   251,   252,   594,
     595,   482,   488,  1507,   283,   574,  1506,   231,   221,   593,
     138,   139,   201,  1499,   501,   202,   203,   204,   205,  1505,
     206,   207,   208,  1504,  1030,   213,   286,   289,   -39,   594,
     595,  1296,   113,   208,   287,  1529,   498,   115,  1528,   116,
     275,   125,   290,   277,  1031,   135,   117,    91,   278,   279,
     335,  1527,   221,   973,   221,  1526,   514,   135,   280,   532,
     465,   201,   307,   118,   202,   203,   204,   205,   221,   206,
     207,   208,   706,   433,   672,   447,   448,   573,   119,   707,
     468,  1357,  1141,  1142,    88,   700,   711,   607,   287,  1358,
     535,   126,   601,  1152,   221,   567,  1147,   111,   712,   713,
     112,   221,   135,   221,   486,   568,   477,   478,   110,   221,
     492,   911,   588,  1163,   729,   128,   714,   473,  1603,  1364,
    1365,  1366,   474,   113,   114,   221,   130,   221,   115,   127,
     116,   698,   508,   863,   183,   809,   570,   117,   497,   499,
     521,  1195,   470,   503,   135,   471,   505,   506,   507,   475,
    1368,   509,   510,   221,   118,   495,   513,   610,   504,   606,
     188,   608,   189,   384,  1522,   194,   735,   715,   586,   119,
     221,   589,   516,   475,  1211,   599,   475,  1079,  1233,   864,
     994,   -39,    56,  1080,    44,   -39,   307,   298,   299,   300,
      37,   233,   800,    58,   797,   437,   794,  1215,   515,    61,
     221,  1161,   221,  1234,   624,    39,   438,   480,   555,  1551,
     548,  1235,   481,  -574,   196,  1097,   546,   416,   221,   556,
    1098,   417,  1014,   609,  1524,   557,   679,  1540,   475,  1485,
    1537,   547,  1486,  1433,  1530,  -557,   475,  1532,  1511,   198,
    1210,   419,   605,  1533,   562,   417,   563,   216,  1543,   421,
     564,  1544,   235,   417,  1535,  -253,  1546,   217,  1545,   221,
    1565,   488,  1562,   596,   703,   704,   587,   125,   604,   590,
     591,   613,   176,   600,  1548,  1229,   911,  1563,  1564,   209,
     218,   178,   221,   592,   597,   219,   615,   616,  1523,   617,
     392,   618,   766,   767,   768,   210,  1230,   731,   765,   963,
     964,   965,  1231,  1085,  1086,   619,   673,   626,   627,  1232,
     634,   634,   654,   660,   738,  1258,  1143,   423,   749,   635,
     670,   417,   671,   236,   425,   298,   299,   300,   417,   758,
     237,   633,   633,   653,   659,  1335,  1336,  1333,   247,   716,
     468,   427,   125,   221,   210,   417,   387,  1472,   287,   696,
     717,   526,   527,   528,   763,   516,   301,   238,   774,   239,
     240,   241,   250,   486,   276,  1100,   949,  1517,   782,  1101,
     298,   299,   300,   950,   291,   951,   952,   953,   120,  1102,
     302,   297,   303,  1103,   778,   801,   304,   305,   529,   530,
     531,   292,   949,  1108,   293,   710,  1110,  1109,   741,   950,
    1111,   951,   952,   953,  1019,  1020,  1021,  1022,  1023,  1112,
     725,   726,   296,  1113,   954,   955,   956,   309,  1556,  1114,
    1116,   745,   747,  1115,  1117,   310,   298,   299,   300,   737,
     311,   138,   139,   593,   135,   743,   593,   312,   748,   213,
     954,   955,   956,   298,   299,   300,   575,   777,   576,   577,
     578,   392,   764,   594,   595,  1118,   594,   595,   337,  1119,
     773,   957,   958,   959,   415,   960,  1120,  1122,   961,   776,
    1121,  1123,   429,   933,   925,   926,   930,   432,   549,   939,
     434,   551,   779,   449,   554,  1337,  1338,   957,   958,   959,
     967,   960,   466,  1124,   961,  1419,  1420,  1125,   802,   803,
     271,   272,   273,   274,   806,   811,   807,   387,  1126,  1128,
    1130,   813,  1127,  1129,  1131,   472,  1431,   475,   302,   810,
     303,   902,   903,  1132,   304,   305,   521,  1133,   476,   988,
     479,   995,   981,   996,   997,   998,   999,  1000,   815,  1263,
    1265,   983,   489,  1264,  1266,  1267,   917,   900,   496,  1268,
     909,  1269,   910,   493,   908,  1270,   904,   275,   475,  1351,
    1280,   500,  1283,   302,   475,   303,   475,  1561,  1591,   304,
     305,   221,  1422,   502,   922,   915,   516,   511,   929,   512,
     891,   522,   934,   936,   938,   565,   975,  1212,  1213,   525,
     978,  1224,  1225,  1226,  1227,  1228,   559,  1477,    68,    69,
     560,    70,   135,   571,   612,   900,  1475,  1033,  1034,   614,
     966,   623,   969,  1479,   971,   990,   972,   622,  1087,   302,
     625,   303,   231,   962,   679,   304,   305,  1099,   668,   669,
     982,   677,   682,  1081,   684,   683,   302,   689,   303,   113,
     984,   985,   702,   305,   115,   685,   116,    71,   692,  1186,
     641,   697,   708,   117,   986,   705,   662,   663,   664,   709,
     721,   722,  1001,    72,  1002,  1003,   732,  1015,   733,  1017,
     118,   931,    73,   734,  1151,   736,  1145,   742,  1013,  1104,
    1105,  1106,  1107,   750,   751,   119,    74,    75,    76,    77,
     769,   771,    78,   665,   666,   667,   905,   906,   770,   907,
    1560,  1185,  1359,  1360,  1361,  1362,   772,   775,   784,   792,
    1134,   781,   783,   785,   786,  1135,   787,   932,   795,    79,
      80,    81,    82,    83,    84,    85,  1199,   788,  1201,   789,
     791,  1568,   812,   796,  1088,   804,   805,   808,   814,   865,
     866,  1567,   869,  1089,   870,   871,   872,   873,   874,   898,
      68,    69,   913,    70,  1138,   899,   912,    68,    69,   914,
      70,   918,   919,   778,   778,   940,  1139,   946,   924,   970,
    1158,   979,   974,  1593,  1602,  1156,  1162,   980,  1154,   987,
     991,   989,   992,  1221,  1136,  1157,   114,   993,  1605,  1148,
    1004,   417,  1005,  1006,  1009,   900,  1007,  1008,  1010,    71,
    1011,  1026,  1012,  1018,  1024,  1159,    71,  1160,  1027,  1028,
     555,  1032,   548,  1172,  1181,    72,   654,  1077,   546,  1170,
    1179,   556,    72,   900,    73,  1029,  1082,   557,  1174,  1183,
    1093,    73,  1094,   547,  1171,  1180,  1251,   653,    74,    75,
      76,    77,  1209,  1095,    78,    74,    75,    76,    77,  1140,
    1146,    78,  1155,  1326,  1327,  1328,  1329,  1190,  1187,   949,
    1188,   779,   779,  1330,  1331,  1332,   950,  1191,   951,   952,
     953,    79,    80,    81,    82,    83,    84,    85,    79,    80,
      81,    82,    83,    84,    85,  1192,  1219,    28,    29,    30,
      31,    32,    33,    34,  1189,  1193,  1369,  1194,  1196,   752,
    1197,   288,    35,  1198,  1202,  1205,  1206,   954,   955,   956,
    1207,  1216,  1208,  1218,  1323,  1324,   875,   876,   877,  1134,
     878,   879,   880,   881,  1135,   882,   883,   208,  1220,   884,
     885,   886,   887,  1222,  1307,  1254,   888,   889,  1223,  1255,
    1134,  1256,  1308,  1309,  1310,  1135,  1539,  1542,   490,  1236,
    1314,  1311,  1312,  1317,   957,   958,   959,  1315,   960,  1316,
    1325,   961,  1321,   284,  1342,  1322,  1343,   532,  1341,  1347,
    1350,  1260,  1261,  1262,  1349,  1344,  1100,  1345,  1271,  1272,
    1273,  1274,  1275,  1276,  1354,  1278,  1279,  1281,  1355,  1284,
    1285,  1286,  1287,  1288,  1289,  1290,  1277,  1292,  1102,  1294,
    1282,  1297,  1108,  1301,  1110,   890,  1112,  1114,  1291,  1116,
    1118,  1300,  1120,  1320,  1122,  1124,  1126,  1370,   125,  1371,
    1411,  1373,     3,  1429,  1372,  1383,  1385,  1374,   125,   125,
     125,   125,  1384,  1386,  1388,  1387,  1389,  1390,   125,   125,
     125,  1393,  1396,  1391,  1392,  1394,  1395,  1397,     2,  1424,
     285,   744,  1399,  1398,  1400,  1401,  1480,  1334,  1402,  1403,
    1405,  1432,  1404,  1407,  1408,  1406,     3,  1410,  1412,  1409,
    1421,  1423,  1418,  1413,  1425,  1427,  1346,  1436,  1428,  1263,
    1437,  1265,  1267,  1438,  1440,  1348,  1269,  1446,   640,  1441,
    1444,  1447,  1352,  1353,  1442,  1430,    28,    29,    30,    31,
      32,    33,    34,  1443,  1356,  1435,  1448,  1449,  1450,  1451,
    1470,    35,  1452,  1453,  1454,  1455,  1426,    28,    29,    30,
      31,    32,    33,    34,  1363,  1367,  1445,  1456,  1457,  1458,
    1471,  1459,    35,  1375,  1376,  1377,  1378,  1379,  1380,  1488,
    1382,  1460,  1461,  1462,  1463,  1464,  1465,    68,    69,  1136,
      70,  1381,  1466,  1467,  1469,   142,  1468,  1473,   143,  1474,
    1417,  1484,   144,   145,   146,   147,   148,  1494,   149,   150,
     151,   152,  1483,   153,   154,  1495,  1476,   155,   156,   157,
     158,  1496,  1415,   114,   159,   160,  1497,   896,  1512,  1515,
    1547,  1516,  1557,   161,  1519,   162,    71,  1520,   900,  1558,
    1521,    14,     3,     4,     5,     6,     7,     8,  1566,  1586,
     163,   164,   165,   641,  1588,   629,   642,  1589,   630,   631,
    1434,    73,  1590,  1592,  1595,     9,    10,  1439,  1596,  1597,
    1601,  1514,  1604,   313,  1606,    74,    75,    76,    77,  1518,
     175,    78,    11,    12,    13,    14,   166,   167,   523,    15,
      16,   611,  1340,   686,   920,    17,   941,  1318,    18,   177,
    1136,  1319,  1478,  1203,   759,    19,    20,  1339,    79,    80,
      81,    82,    83,    84,    85,   561,   212,  1252,  1078,   900,
     897,   790,  1144,  1204,  1306,  1487,    28,    29,    30,    31,
      32,    33,    34,   916,  1096,   643,  1092,   109,  1313,   694,
     255,    35,   168,   169,   170,   945,   645,     0,   727,     0,
       0,  1489,  1490,  1491,     0,  1587,     0,     0,     0,     0,
     111,    21,    22,   112,    23,    24,    25,     0,    26,    27,
      28,    29,    30,    31,    32,    33,    34,     0,     3,    14,
       0,     0,     0,     0,  1598,    35,   113,   114,     0,     0,
       0,   115,  1600,   116,  1165,  1166,  1167,  1168,     0,     0,
     117,     0,     0,  1525,     0,     0,     0,     0,  1531,  1525,
    1534,     0,  1538,     0,  1531,  1525,  1534,   118,    11,    12,
      13,    14,     0,     0,     0,  1541,     0,     0,     0,  1555,
       0,     0,   119,     0,     0,     0,  1531,  1525,  1534,     0,
      68,    69,     0,    70,     0,     0,     0,     0,   142,     0,
       0,   143,     0,   900,     0,   144,   145,   146,   147,   148,
       0,   149,   150,   151,   152,     0,   153,   154,     0,     0,
     155,   156,   157,   158,     0,  1599,   114,   159,   160,     0,
       0,   171,     0,     0,     0,     0,   161,     0,   162,    71,
     172,     0,     0,     0,     0,     0,   900,     0,     0,     0,
    1594,     0,     0,   163,   164,   165,    28,    29,    30,    31,
      32,    33,    34,     0,    73,  1169,     0,     0,     0,     0,
       0,    35,     0,     0,     0,     0,     0,     0,    74,    75,
      76,    77,     0,     0,    78,     0,     0,     9,    10,   166,
       0,   298,   299,   300,     0,     0,     0,     0,     0,     0,
       3,     0,     0,     0,     0,   483,   484,    14,     0,     0,
       0,    79,    80,    81,    82,    83,    84,    85,     0,   628,
       0,   629,     0,   648,   630,   631,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
      68,    69,     0,    70,     0,   168,   169,   170,   142,     0,
       0,   143,     0,     0,     0,   144,   145,   146,   147,   148,
       0,   149,   150,   151,   152,     0,   153,   154,     0,     0,
     155,   156,   157,   158,     0,     0,   114,   159,   160,     0,
       0,     0,     0,     0,     0,     0,   161,     0,   162,    71,
       0,     0,    28,    29,    30,    31,    32,    33,    34,     0,
       0,   632,     0,   163,   164,   165,     0,    35,     0,     0,
       0,     0,     0,     0,    73,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,    74,    75,
      76,    77,     0,     0,    78,     0,     0,   649,     0,   166,
       0,   298,   299,   300,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,   483,   484,     0,     0,     0,
       0,    79,    80,    81,    82,    83,    84,    85,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,    14,
       0,     0,     0,     0,   485,     0,   303,     0,     0,   650,
     304,   305,   651,   172,     0,   168,   169,   170,    68,    69,
       0,    70,     0,     0,     0,     0,   142,     0,     0,   143,
       0,     0,     0,   144,   145,   146,   147,   148,     0,   149,
     150,   151,   152,     0,   153,   154,     0,     0,   155,   156,
     157,   158,     0,     0,   114,   159,   160,     0,     0,     0,
       0,     0,     0,     0,   161,     0,   162,    71,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,   163,   164,   165,    28,    29,    30,    31,    32,    33,
      34,     0,    73,   652,     0,     0,     0,     0,     0,    35,
       0,     0,     0,     0,     0,     0,    74,    75,    76,    77,
       0,     0,    78,     0,     0,     0,     0,   166,     0,   298,
     299,   300,     0,     0,     0,     0,     0,     0,     3,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,    79,
      80,    81,    82,    83,    84,    85,     0,     0,     0,     0,
       0,     0,     0,     0,   485,     0,   303,     0,     0,     0,
     702,   305,     0,   172,     0,     0,     0,     0,    68,    69,
       0,    70,     0,   168,   169,   170,   142,     0,     0,   143,
       0,     0,     0,   144,   145,   146,   147,   148,     0,   149,
     150,   151,   152,     0,   153,   154,     0,     0,   155,   156,
     157,   158,     0,     0,   114,   159,   160,     0,     0,  1569,
       0,     0,     0,     0,   161,     0,   162,    71,     0,     0,
       0,     0,     0,     0,  1570,     0,     0,     0,     0,     0,
       0,   163,   164,   165,     0,   927,     0,     0,     0,     0,
    1571,     0,    73,     0,     0,     0,     0,     0,     0,  1572,
       0,     0,     0,     0,     0,     0,    74,    75,    76,    77,
       0,     0,    78,  1573,  1574,  1575,  1576,   166,     0,  1577,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,   928,     0,  1175,     0,  1176,  1177,     0,     0,    79,
      80,    81,    82,    83,    84,    85,  1578,  1579,  1580,  1581,
    1582,  1583,  1584,     0,    11,    12,    13,    14,     0,     0,
       0,     0,   485,     0,   303,     0,     0,     0,   304,   305,
       0,   172,     0,   168,   169,   170,    68,    69,     0,    70,
       0,     0,     0,     0,   142,     3,     0,   143,     0,     0,
       0,   144,   145,   146,   147,   148,     0,   149,   150,   151,
     152,     0,   153,   154,     0,     0,   155,   156,   157,   158,
       0,     0,   114,   159,   160,     0,     0,     0,     0,     0,
       0,     0,   161,     0,   162,    71,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,   163,
     164,   165,    28,    29,    30,    31,    32,    33,    34,     0,
      73,  1178,     0,     0,     0,     0,     0,    35,     0,     0,
       0,     0,     0,     0,    74,    75,    76,    77,     0,     0,
      78,     0,     0,     0,     0,   166,   167,    93,    94,    95,
      96,    97,    98,    99,   100,   101,   102,   103,   104,   105,
     106,   107,     0,     0,     0,     0,     0,    79,    80,    81,
      82,    83,    84,    85,     0,     0,     0,     0,     0,     0,
    1585,    68,   225,     0,    70,   135,     0,     0,     0,    68,
      69,   172,    70,     0,     0,     0,     0,   142,     0,     0,
     143,   168,   169,   170,   144,   145,   146,   147,   148,     0,
     149,   150,   151,   152,     0,   153,   154,     0,     0,   155,
     156,   157,   158,     0,     0,   114,   159,   160,     0,     0,
      71,     0,     0,     0,    14,   161,     0,   162,    71,     0,
       0,     0,     0,     0,   656,     0,    72,   657,     0,     0,
       0,     0,   163,   164,   165,    73,     0,     0,     0,     0,
       0,     0,     0,    73,     0,     0,     0,     0,     0,    74,
      75,    76,    77,     0,     0,    78,     0,    74,    75,    76,
      77,     0,     0,    78,     0,     0,     0,     0,   166,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,    79,    80,    81,    82,    83,    84,    85,     0,
      79,    80,    81,    82,    83,    84,    85,     0,     0,    28,
      29,    30,    31,    32,    33,    34,    68,    69,   658,    70,
       0,     0,     0,     0,    35,     0,     0,     0,     0,     0,
     225,     0,     0,     0,   168,   169,   170,    68,    69,   172,
      70,     0,     0,     0,     0,   142,     0,     0,   143,     0,
       0,     0,   144,   145,   146,   147,   148,     0,   149,   150,
     151,   152,     0,   153,   154,    71,     0,   155,   156,   157,
     158,     0,     0,   114,   159,   160,     0,     0,     0,     0,
       0,    72,     0,   161,     0,   162,    71,     0,     0,     0,
      73,     0,     0,     0,     0,     0,     0,     0,     0,     0,
     163,   164,   165,     0,    74,    75,    76,    77,     0,     0,
      78,    73,     0,     0,     0,     0,     0,   298,   299,   300,
       0,     0,     0,     0,     0,    74,    75,    76,    77,     0,
       0,    78,     0,     0,     0,     0,   569,    79,    80,    81,
      82,    83,    84,    85,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,   384,     0,    79,    80,
      81,    82,    83,    84,    85,     0,     0,     0,     0,     0,
       0,     0,    68,   225,     0,    70,   135,     0,     0,     0,
      68,    69,   172,    70,     0,     0,     0,     0,   142,     0,
       0,   143,   168,   169,   170,   144,   145,   146,   147,   148,
       0,   149,   150,   151,   152,     0,   153,   154,     0,     0,
     155,   156,   157,   158,     0,     0,   114,   159,   160,     0,
       0,    71,     0,     0,     0,     0,   678,     0,   162,    71,
       0,     0,     0,     0,     0,     0,     0,    72,     0,     0,
       0,     0,     0,   163,   164,   165,    73,     0,     0,     0,
       0,     0,     0,     0,    73,     0,     0,     0,     0,     0,
      74,    75,    76,    77,     0,     0,    78,     0,    74,    75,
      76,    77,     0,     0,    78,     0,     0,     0,     0,   166,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,    79,    80,    81,    82,    83,    84,    85,
       0,    79,    80,    81,    82,    83,    84,    85,     0,     3,
     302,     0,   303,     0,     0,     0,   304,   305,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,   225,   648,     0,     0,   168,   169,   170,    68,    69,
     172,    70,     0,     0,     0,     0,   142,     0,     0,   143,
       0,     0,     0,   144,   145,   146,   147,   148,     0,   149,
     150,   151,   152,     0,   153,   154,     0,     0,   155,   156,
     157,   158,     0,     0,   114,   159,   160,     0,     0,     0,
       0,     0,     0,     0,   161,     0,   162,    71,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,   163,   164,   165,     0,     0,     0,     0,     0,     0,
       0,     0,    73,     0,     0,     0,     0,    68,    69,     0,
      70,     0,     0,     0,     0,     0,    74,    75,    76,    77,
       0,     0,    78,     0,     0,    68,    69,   730,    70,   135,
       0,     0,     0,     0,     0,     0,   649,     0,     0,     0,
     514,     0,     0,   114,     0,     0,     0,     0,     0,    79,
      80,    81,    82,    83,    84,    85,    71,     0,     0,     0,
       0,   114,     0,     0,   225,     0,     0,     0,     0,     0,
       0,     0,    72,   172,    71,     0,     0,     0,    14,     0,
       0,    73,     0,   168,   169,   170,     0,     0,   650,     0,
      72,   651,     0,     0,     0,    74,    75,    76,    77,    73,
       0,    78,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,    74,    75,    76,    77,     0,     0,    78,
       0,     0,     0,     0,     0,     0,     0,     0,    79,    80,
      81,    82,    83,    84,    85,    68,    69,   231,    70,   135,
       0,     0,     0,   142,     0,     0,    79,    80,    81,    82,
      83,    84,    85,    68,    69,     0,    70,     0,  1084,     0,
       0,   142,     0,    28,    29,    30,    31,    32,    33,    34,
       0,   114,  1184,     0,     0,     0,     0,     0,    35,     0,
       0,     0,     0,     0,    71,     0,     0,     0,     0,   114,
       0,     0,  1552,     0,     0,  1085,  1086,     0,     0,  1553,
      72,     0,    71,     0,     0,     0,     0,     0,     0,    73,
       0,     0,     0,     0,     0,     0,     0,     0,    72,     0,
     467,     0,   225,    74,    75,    76,    77,    73,     0,    78,
       0,   172,     0,     0,     0,     0,     0,     0,   467,     0,
       0,    74,    75,    76,    77,     0,     0,    78,     0,     0,
       0,     0,     0,     0,     0,     0,    79,    80,    81,    82,
      83,    84,    85,     0,     0,    68,    69,     0,    70,     0,
       0,     0,     0,   142,    79,    80,    81,    82,    83,    84,
      85,     0,     0,     0,     0,     0,    68,    69,     0,    70,
     168,   169,   170,     0,   142,     0,     0,     0,     0,     0,
       0,   114,     0,     0,  1298,     0,     0,   285,   168,   169,
     170,  1299,     0,     0,    71,     0,     0,     0,     0,     0,
     515,     0,   114,     0,     0,   285,     0,     0,     0,     0,
      72,     0,  1414,     0,     0,    71,     0,     0,     0,    73,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,    72,     0,    74,    75,    76,    77,     0,     0,    78,
      73,     0,     0,     0,     0,    68,    69,     0,    70,     0,
       0,     0,     0,   142,    74,    75,    76,    77,     0,     0,
      78,     0,     0,     0,     0,     0,    79,    80,    81,    82,
      83,    84,    85,     0,     0,     0,     0,     0,     0,     0,
       0,   114,     0,     0,     0,     0,     0,    79,    80,    81,
      82,    83,    84,    85,    71,     0,     0,     0,     0,     0,
     168,   169,   170,     0,     0,     0,     0,     0,     0,   171,
      72,     0,     0,     0,     0,     0,     0,     0,     0,    73,
       0,   168,   169,   170,  1554,     0,     0,   171,     0,    68,
      69,     0,   868,    74,    75,    76,    77,   142,     0,    78,
      68,    69,     0,    70,     0,     0,     0,    68,    69,     0,
      70,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,   114,    79,    80,    81,    82,
      83,    84,    85,     0,     0,     0,     0,     0,    71,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,    71,
       0,     0,     0,     0,    72,     0,    71,     0,     0,     0,
     168,   169,   170,    73,     0,    72,     0,     0,     0,     0,
       0,     0,    72,     0,    73,     0,     0,    74,    75,    76,
      77,    73,     0,    78,     0,     0,     0,     0,    74,    75,
      76,    77,     0,     0,    78,    74,    75,    76,    77,   171,
       0,    78,    68,    69,     0,    70,     0,     0,     0,     0,
      79,    80,    81,    82,    83,    84,    85,     0,     0,     0,
     171,    79,    80,    81,    82,    83,    84,    85,    79,    80,
      81,    82,    83,    84,    85,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   168,   169,   170,     0,     0,     0,
       0,    71,     0,     0,     0,     0,     0,     0,   231,     0,
       0,     0,     0,     0,     0,    68,    69,    72,    70,     0,
       0,     0,     0,  1237,  1238,  1239,    73,  1240,  1241,  1242,
    1243,     0,  1244,  1245,   208,     0,  1246,  1247,  1248,  1249,
      74,    75,    76,    77,     0,  1250,    78,     0,     0,   171,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,    71,     0,     0,     0,     0,     0,
       0,     0,     0,    79,    80,    81,    82,    83,    84,    85,
      72,     0,     0,     0,     0,     0,     0,     0,     0,    73,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,    74,    75,    76,    77,     0,     0,    78,
     875,   876,   877,     0,   878,   879,   880,   881,     0,   882,
     883,   208,     0,   884,   885,   886,   887,     0,     0,     0,
     888,   889,     0,   171,     0,     0,    79,    80,    81,    82,
      83,    84,    85,     0,   746,     0,     0,   602,   143,   603,
       0,   935,   144,   145,   146,   147,   148,     0,   149,   150,
     151,   152,     0,   153,   154,     0,     0,   155,   156,   157,
     158,     0,     0,   114,   159,   160,     0,     0,    68,     0,
       0,    70,     0,   161,     0,   162,     0,     0,     0,     0,
       0,     3,     0,     0,     0,     0,     0,     0,     0,   890,
     163,   164,   248,   281,   143,   282,     0,     0,   144,   145,
     146,   147,   148,     0,   149,   150,   151,   152,     0,   153,
     154,     0,     0,   155,   156,   157,   158,    71,     0,     0,
     159,   160,     0,     0,     0,     0,   166,     0,     0,   161,
       0,   162,     0,    72,     0,     0,   937,     0,     0,     0,
       0,     0,    73,     0,     0,     0,   163,   164,   248,     0,
       0,     0,     0,     0,     0,     0,    74,    75,    76,    77,
       0,     0,    78,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   166,     0,     0,     0,     0,     0,     0,    79,
      80,    81,    82,    83,    84,    85,     0,     0,     0,  1416,
       0,     0,     0,     0,     0,   361,   362,   363,   364,   365,
     366,   367,   368,   369,   370,   371,   372,   373,     0,     0,
       0,     0,     8,     0,     0,     0,   374,   375,   376,   377,
     378,   379,     0,     0,     0,     0,     0,     0,     0,     0,
       9,    10,     0,     0,     0,     0,     0,     0,     0,     0,
      68,     0,     0,    70,     0,     0,     0,    11,    12,    13,
      14,     0,     0,     3,     0,     0,     0,     0,   380,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   381,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,    71,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,    72,     0,   382,   383,     0,
       0,     0,     0,     0,    73,     0,     0,     0,     0,     0,
     172,     0,     0,     0,     0,     0,     0,     0,    74,    75,
      76,    77,     0,     0,    78,    28,    29,    30,    31,    32,
      33,    34,     0,   384,   385,     0,     0,     0,     0,     0,
      35,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,    79,    80,    81,    82,    83,    84,    85,     0,     0,
       0,     0,     0,     0,     0,     0,   172,   361,   362,   363,
     364,   365,   366,   367,   368,   369,   370,   371,   372,   373,
       0,     0,     0,     0,     8,     0,     0,     0,   374,   375,
     376,   377,   378,   379,     0,     0,     0,     0,     0,     0,
       0,     0,     9,    10,    68,     0,     0,    70,     0,     0,
       0,    68,    69,     0,    70,     0,     0,     3,     0,    11,
      12,    13,    14,     0,     0,     0,     0,     0,     0,     0,
     380,     0,     0,     0,     0,   143,     0,     0,     0,   144,
     145,   146,   147,   148,   381,   149,   150,   151,   152,     0,
     153,   154,     0,    71,   155,   156,   157,   158,     0,     0,
      71,   159,   160,     0,     0,     0,     0,     0,     0,    72,
     161,     0,   162,     0,     0,     0,    72,     0,    73,   382,
     383,     0,     0,     0,     0,    73,     0,   163,   164,   248,
       0,     0,    74,    75,    76,    77,     0,     0,    78,    74,
      75,    76,    77,     0,     0,    78,     0,    28,    29,    30,
      31,    32,    33,    34,     0,   384,   757,     0,     0,     0,
       0,     0,    35,   166,     0,    79,    80,    81,    82,    83,
      84,    85,    79,    80,    81,    82,    83,    84,    85,     0,
       0,   361,   362,   363,   364,   365,   366,   367,   368,   369,
     370,   371,   372,   373,     0,     0,     0,     0,     8,     0,
       0,     0,   374,   375,   376,   377,   378,   379,     0,     0,
       0,     0,     0,     0,     0,     0,     9,    10,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,    11,    12,    13,    14,     0,     0,     0,
       0,     0,     0,     0,   380,     0,     0,     0,     0,   143,
       0,     0,     0,   144,   145,   146,   147,   148,   381,   149,
     150,   151,   152,     0,   153,   154,     0,     0,   155,   156,
     157,   158,   450,     0,     0,   159,   160,     0,     0,     0,
       0,     0,     0,     0,   161,     0,   162,     0,     0,     0,
       0,     0,     0,   382,   383,     0,     0,     0,     0,     0,
       0,   163,   164,   248,     0,   451,     0,   452,   453,   454,
     455,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,    28,    29,    30,    31,    32,    33,    34,     0,   384,
     944,     0,     0,     0,     0,     0,    35,   166,     0,     0,
       0,   921,     0,     0,     0,   456,   457,   458,   459,     0,
       0,   460,     0,     0,     0,   461,   462,   463,   740,     3,
       0,     0,     0,     0,   143,     0,     0,   172,   144,   145,
     146,   147,   148,     0,   149,   150,   151,   152,     0,   153,
     154,     0,     0,   155,   156,   157,   158,     0,     0,     0,
     159,   160,     0,     0,     0,     0,     0,     0,     0,   161,
       0,   162,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,   163,   164,   248,   143,
       0,     0,     0,   144,   145,   146,   147,   148,     0,   149,
     150,   151,   152,     0,   153,   154,     0,     0,   155,   156,
     157,   158,     0,     0,     0,   159,   160,     0,     0,     0,
       0,     0,   166,     0,   161,     0,   162,     0,     0,   464,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,   163,   164,   248,     0,     0,  1035,  1036,     0,  1037,
    1038,  1039,  1040,  1041,  1042,     0,  1043,  1044,     0,  1045,
    1046,  1047,  1048,  1049,     0,     0,     4,     5,     6,     7,
       8,     0,     0,     0,     0,     0,     0,   166,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     9,    10,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,    11,    12,    13,    14,     0,
       0,   172,    15,    16,     0,     0,    68,    69,    17,    70,
       0,    18,     0,     0,     0,     0,     0,     0,    19,    20,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,   875,   876,   877,     0,   878,   879,   880,   881,     0,
     882,   883,   208,     0,   884,   885,   886,   887,     0,     0,
       3,   888,   889,     0,     0,    71,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,    72,     0,    68,    21,    22,    70,    23,    24,    25,
      73,    26,    27,    28,    29,    30,    31,    32,    33,    34,
       0,     0,   524,     0,    74,    75,    76,    77,    35,   536,
      78,     0,     3,     0,     0,     0,     0,   896,     0,     0,
       0,     0,     0,     0,     0,     0,   172,     0,     0,     0,
     890,     0,    71,     0,     0,     0,     0,    79,    80,    81,
      82,    83,    84,    85,     0,     0,     0,     0,    72,     0,
       0,     0,     0,     0,     0,  1050,  1051,    73,  1052,  1053,
    1054,   536,  1055,  1056,     0,     0,  1057,  1058,     0,  1059,
       0,    74,    75,    76,    77,     0,     0,    78,     0,     0,
       0,   172,  1060,  1061,  1062,  1063,  1064,  1065,  1066,  1067,
    1068,  1069,  1070,  1071,  1072,  1073,  1074,   537,     0,     6,
       7,     8,     0,     0,    79,    80,    81,    82,    83,    84,
      85,   538,     0,     0,     0,     0,   539,     0,     0,     9,
      10,     0,     0,     0,     0,     0,     0,   133,     0,     0,
    1075,     0,     0,     0,     0,     0,    11,    12,    13,    14,
       0,   540,   541,     0,     0,     0,     0,     0,     0,   537,
       0,     6,     7,     8,     0,     0,     0,     0,     0,     0,
       0,   542,     0,   538,     0,     0,     0,     0,   539,     0,
       0,     9,    10,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,    11,    12,
      13,    14,     0,   540,   541,     0,   543,   544,     0,     0,
       0,    28,    29,    30,    31,    32,    33,    34,     0,     0,
       0,     0,     0,   542,     0,     0,    35,     0,     0,     0,
       0,     0,     0,     0,    28,    29,    30,    31,    32,    33,
      34,     0,     0,   545,     0,     0,     0,     0,     0,    35,
       0,     0,     0,     0,     0,   816,     0,     0,   543,   544,
     817,   818,     0,   819,   820,   821,   822,   823,   824,     0,
     825,   826,     0,   827,   828,   829,   830,   831,     0,     0,
       0,    68,    69,     0,    70,     0,    28,    29,    30,    31,
      32,    33,    34,     0,   816,  1164,     0,     0,     0,   817,
     818,    35,   819,   820,   821,   822,   823,   824,     0,   825,
     826,     0,   827,   828,   829,   830,   831,     0,     0,   832,
       0,   833,     0,     0,     0,     0,   834,     0,     0,     0,
      71,     0,     0,     0,     0,    68,    69,     0,    70,     0,
       0,     0,     0,   835,     0,     0,    72,     0,     0,     0,
       0,     0,     0,     0,     0,    73,     0,     0,   832,     0,
     833,     0,     0,     0,     0,   834,     0,     0,     0,    74,
      75,    76,    77,     0,     0,    78,   836,   256,   257,   258,
       0,     0,   835,     0,    71,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
      72,     0,   259,    80,    81,    82,    83,    84,    85,    73,
       0,     0,     0,     0,     0,   836,     0,     0,     0,     0,
       0,     0,     0,    74,    75,    76,    77,     0,     0,    78,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,    79,    80,    81,    82,
      83,    84,    85,     0,     0,     0,   837,     0,   838,   839,
     840,   841,   842,   843,   844,   845,   846,   847,   848,   849,
     850,   851,   852,   853,   854,     0,     0,     0,   855,     0,
      68,    69,     0,    70,     0,     0,   260,   856,   261,   262,
     263,   264,     0,     0,     0,   837,     0,   838,   839,   840,
     841,   842,   843,   844,   845,   846,   847,   848,   849,   850,
     851,   852,   853,   854,   314,     0,     0,   855,     0,   857,
       0,     0,     0,     0,     0,     0,   856,     0,   253,    71,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   315,     0,    72,     0,   316,     0,     0,
     317,   318,     0,     0,    73,   319,   320,   321,   322,   323,
     324,   325,   326,   327,   328,   329,   330,     0,    74,    75,
      76,    77,     0,   331,    78,    68,    69,   332,    70,     0,
       0,     0,     0,     0,   333,    68,    69,     0,    70,     0,
       0,     0,     0,   334,     0,     0,     0,     0,     0,     0,
       0,    79,    80,    81,    82,    83,    84,    85,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,    71,     0,     0,     0,     0,    68,
      69,     0,    70,     0,    71,     0,     0,     0,     0,     0,
      72,     0,     0,   443,     0,   444,   445,     0,     0,    73,
      72,     0,   446,     0,     0,   266,   267,     0,     0,    73,
       0,     0,     0,    74,    75,    76,    77,     0,     0,    78,
       0,     0,     0,    74,    75,    76,    77,     0,    71,    78,
       0,     0,     0,    68,    69,     0,    70,   135,     0,     0,
       0,     0,     0,     0,    72,     0,    79,    80,    81,    82,
      83,    84,    85,    73,     0,     0,    79,    80,    81,    82,
      83,    84,    85,     0,     0,     0,     0,    74,    75,    76,
      77,     0,     0,    78,     0,     0,    68,    69,     0,    70,
       0,     0,    71,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   483,   484,     0,     0,    72,     0,
      79,    80,    81,    82,    83,    84,    85,    73,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,    74,    75,    76,    77,    71,     0,    78,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,    72,     0,     0,     0,     0,     0,     0,     0,     0,
      73,     0,     0,     0,    79,    80,    81,    82,    83,    84,
      85,     0,     0,     0,    74,    75,    76,    77,   143,     0,
      78,     0,     0,     0,   146,   147,   148,     0,   149,   150,
     151,   152,     0,   153,   154,     0,     0,   155,   156,   157,
     158,     0,     0,     0,  1302,   160,     0,    79,    80,    81,
      82,    83,    84,    85,     0,     0,     0,     0,     0,   340,
     113,     0,     0,     0,     0,   115,     0,   116,     0,     0,
       0,     0,     0,     0,   117,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,   118,   341,  1303,   342,   343,   344,   345,   346,     0,
       0,     0,     0,   347,     0,     0,   119,     0,     0,     0,
       0,     0,   348,  1304,     0,     0,     0,   349,     0,     0,
     350,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   351,   352,   353,   354,   355,   356,   357,   358,
       0,     0,     0,     0,     0,   359
};

static const yytype_int16 yycheck[] =
{
       5,   108,    25,   109,    92,   692,   178,   118,   119,   425,
       1,   427,     1,    57,    25,    20,   339,    14,     1,   339,
     974,     1,   339,    28,    29,    30,    31,     1,    25,  1005,
     738,    21,    22,     1,  1128,   677,  1100,    25,     9,    12,
       7,     7,   487,    43,    25,    23,    24,     9,    48,     7,
       8,     8,     8,   500,   501,   502,   200,   111,    20,    59,
       9,     8,    63,   750,     7,    19,    33,   178,     9,    56,
      60,     7,     7,    65,    66,   751,   111,     9,   184,    63,
       9,    10,    11,     9,   137,    11,   137,     9,   199,   196,
       9,   203,   204,   205,   206,   207,     9,    44,    88,    20,
     123,     9,    31,    32,     9,    31,    32,     9,    10,     9,
      10,     9,   200,     9,    10,     9,   227,     9,    10,    11,
     198,   144,   167,   120,   129,    64,   131,   132,   133,  1095,
       9,     9,   120,  1477,   137,     8,   216,   127,   128,    31,
      32,   247,   249,  1465,   167,   393,  1464,     7,   294,    11,
       9,    10,    23,  1457,   300,    26,    27,    28,    29,  1463,
      31,    32,    33,  1462,   228,   209,   171,   172,   137,    31,
      32,    44,    40,    33,   171,  1497,   283,    45,  1496,    47,
     161,   178,   172,   164,   248,     9,    54,   126,    24,    25,
     195,  1495,   294,   293,   294,  1494,    20,     9,    34,   301,
     223,    23,   183,    71,    26,    27,    28,    29,   294,    31,
      32,    33,   293,   213,   300,   220,   221,   295,    86,   300,
     225,   292,   898,   899,   299,   672,    39,    87,   225,   300,
     337,   298,   404,   293,   294,   289,   912,    14,    51,    52,
      17,   294,     9,   294,   249,   299,   236,   237,     8,   294,
     255,   698,   396,   940,   289,   275,    69,   290,  1602,    26,
      27,    28,   295,    40,    41,   294,   216,   294,    45,   299,
      47,   300,   295,   300,   290,   216,   383,    54,   283,   284,
     303,   968,   295,   288,     9,   298,   291,   292,   293,   292,
    1256,   296,   297,   294,    71,   276,   301,   408,   288,   406,
       7,   407,    12,   290,   296,   290,   554,   120,   396,    86,
     294,   399,   302,   292,   990,   403,   292,   299,   291,   298,
     296,   290,   313,   305,   313,   294,   307,   106,   107,   108,
     313,   298,   298,   313,   305,   293,   293,   293,   300,   313,
     294,   293,   294,   292,   432,   313,   304,   290,   339,   296,
     339,   300,   295,   307,    60,   290,   339,   295,   294,   339,
     295,   299,   809,   407,   296,   339,   473,   296,   292,   293,
     296,   339,   296,  1349,   296,   296,   292,   296,  1472,   290,
     296,   295,   405,   296,   374,   299,   376,   290,   296,   295,
     380,   296,   293,   299,   296,   290,   296,   290,   296,   294,
     296,   508,   296,   295,   511,   512,   396,   404,   405,   399,
     400,   411,   409,   403,  1508,   272,   863,   296,   296,   290,
     290,   409,   294,   401,   402,   290,   416,   417,  1492,   419,
     574,   421,   583,   584,   585,   306,   293,   544,   582,   251,
     252,   253,   299,    65,    66,   423,   469,   437,   438,   306,
     439,   440,   441,   442,   560,  1097,   901,   295,   569,   439,
     465,   299,   467,   295,   295,   106,   107,   108,   299,   576,
     295,   439,   440,   441,   442,   139,   140,  1185,   298,   292,
     485,   295,   479,   294,   306,   299,   574,   298,   485,   494,
     303,    75,    76,    77,   582,   485,   137,    46,   605,    48,
      49,    50,   295,   508,    51,   295,    39,  1483,   614,   299,
     106,   107,   108,    46,   137,    48,    49,    50,   295,   295,
     299,   307,   301,   299,   612,   631,   305,   306,   112,   113,
     114,   137,    39,   295,   137,   525,   295,   299,   561,    46,
     299,    48,    49,    50,    25,    26,    27,    28,    29,   295,
     540,   541,   137,   299,    87,    88,    89,   296,  1512,   295,
     295,   566,   567,   299,   299,    12,   106,   107,   108,   559,
     296,     9,    10,    11,     9,   565,    11,   296,   568,   623,
      87,    88,    89,   106,   107,   108,    55,   610,    57,    58,
      59,   735,   582,    31,    32,   295,    31,    32,    61,   299,
     605,   134,   135,   136,   305,   138,   295,   295,   141,   609,
     299,   299,   295,   724,   721,   722,   723,   295,   941,   730,
     296,   941,   612,   300,   941,   139,   140,   134,   135,   136,
     226,   138,   298,   295,   141,  1311,  1312,   299,   638,   639,
      26,    27,    28,    29,   644,   650,   646,   735,   295,   295,
     295,   656,   299,   299,   299,   295,  1343,   292,   299,   649,
     301,    28,    29,   295,   305,   306,   689,   299,     8,   775,
     296,   783,   760,   785,   786,   787,   788,   789,   668,   295,
     295,   769,   300,   299,   299,   295,   709,   692,    25,   299,
     695,   295,   697,   221,   694,   299,    63,   678,   292,   293,
    1116,   300,  1118,   299,   292,   301,   292,   293,   296,   305,
     306,   294,   295,   300,   719,   705,   706,   295,   723,   295,
     688,   291,   727,   728,   729,   299,   749,   991,   992,   295,
     753,    25,    26,    27,    28,    29,   295,  1424,     5,     6,
     295,     8,     9,   304,   295,   750,  1422,   850,   851,   295,
     740,   293,   742,  1429,   744,   295,   746,   291,   864,   299,
     296,   301,     7,   296,   871,   305,   306,   873,   295,   295,
     760,   290,     7,   861,   296,   293,   299,   301,   301,    40,
     770,   771,   305,   306,    45,   296,    47,    54,    19,   296,
     209,    19,   302,    54,   772,    20,    75,    76,    77,   293,
     293,   111,   792,    70,   794,   795,   290,   812,   290,   814,
      71,    72,    79,   290,   921,   290,   904,   304,   808,    26,
      27,    28,    29,    19,   295,    86,    93,    94,    95,    96,
      62,   295,    99,   112,   113,   114,   203,   204,    62,   206,
    1516,   947,    26,    27,    28,    29,   295,   295,   300,   304,
     105,   296,   296,   296,   296,   110,   296,   118,   213,   126,
     127,   128,   129,   130,   131,   132,   973,   296,   975,   296,
     296,  1558,   216,   298,   864,   298,   298,   295,   216,   296,
     293,  1557,   298,   864,   291,   293,     8,   298,   291,   295,
       5,     6,   296,     8,   894,   295,   295,     5,     6,   296,
       8,   296,   295,   991,   992,    19,   896,   296,   302,   300,
     933,   296,   300,  1589,  1601,   926,   939,   293,   923,    19,
     293,   296,   293,  1029,   892,   930,    41,   296,  1604,   919,
     295,   299,   304,   295,   295,   940,   296,   296,   295,    54,
     296,   243,   296,   296,   295,   935,    54,   937,   235,   247,
     941,    22,   941,   942,   943,    70,   945,   296,   941,   942,
     943,   941,    70,   968,    79,   295,   298,   941,   942,   943,
     291,    79,   298,   941,   942,   943,  1082,   945,    93,    94,
      95,    96,   987,   290,    99,    93,    94,    95,    96,   302,
     296,    99,   197,  1165,  1166,  1167,  1168,   300,   295,    39,
     304,   991,   992,  1175,  1176,  1177,    46,   300,    48,    49,
      50,   126,   127,   128,   129,   130,   131,   132,   126,   127,
     128,   129,   130,   131,   132,   300,  1016,   282,   283,   284,
     285,   286,   287,   288,   304,   300,   291,   295,   137,    20,
     300,   308,   297,   300,   296,    62,    62,    87,    88,    89,
     296,     8,   296,   296,  1161,  1162,    22,    23,    24,   105,
      26,    27,    28,    29,   110,    31,    32,    33,   250,    35,
      36,    37,    38,   299,   296,   295,    42,    43,   299,   295,
     105,   290,   300,   296,   296,   110,  1502,  1503,   167,  1079,
     296,   295,   295,   293,   134,   135,   136,   296,   138,   296,
     256,   141,   300,   218,   293,   300,    19,   301,  1196,   296,
     298,  1101,  1102,  1103,   304,  1203,   295,  1205,  1108,  1109,
    1110,  1111,  1112,  1113,  1230,  1115,  1116,  1117,   300,  1119,
    1120,  1121,  1122,  1123,  1124,  1125,  1114,  1127,   295,  1129,
    1118,  1131,   295,  1133,   295,   111,   295,   295,  1126,   295,
     295,  1132,   295,  1158,   295,   295,   295,   291,  1155,   296,
       8,   296,    18,   295,   300,   296,   296,   300,  1165,  1166,
    1167,  1168,   300,   300,   300,   296,   296,   300,  1175,  1176,
    1177,   300,   300,   296,   296,   296,   296,   296,     0,    19,
     305,   299,   296,   300,   300,   296,   301,  1187,   300,   296,
     296,     8,   300,   296,   296,   300,    18,   296,   296,   300,
    1317,   293,   302,   300,  1325,   296,  1206,   300,   296,   295,
     300,   295,   295,   300,   296,  1215,   295,   295,    84,   304,
     296,   295,  1222,  1223,   304,  1342,   282,   283,   284,   285,
     286,   287,   288,   304,  1234,  1351,   296,   300,   296,   300,
       8,   297,   296,   300,   296,   300,   296,   282,   283,   284,
     285,   286,   287,   288,  1254,  1255,   291,   295,   295,   295,
     300,   295,   297,  1263,  1264,  1265,  1266,  1267,  1268,   300,
    1270,   295,   295,   295,   295,   295,   295,     5,     6,  1257,
       8,  1269,   295,   295,   295,    13,   296,   296,    16,   296,
    1305,   296,    20,    21,    22,    23,    24,   295,    26,    27,
      28,    29,   304,    31,    32,   295,  1423,    35,    36,    37,
      38,   295,  1303,    41,    42,    43,   295,   299,   296,   296,
     296,   295,   295,    51,   296,    53,    54,   296,  1343,    19,
     296,   197,    18,   155,   156,   157,   158,   159,     8,   296,
      68,    69,    70,   209,   296,   211,   212,   295,   214,   215,
    1350,    79,   295,   256,   104,   177,   178,  1357,   296,   296,
      19,  1478,   295,   194,   296,    93,    94,    95,    96,  1485,
      25,    99,   194,   195,   196,   197,   104,   105,   307,   201,
     202,   409,  1195,   479,   718,   207,   732,  1152,   210,    25,
    1368,  1154,  1425,   978,   580,   217,   218,  1194,   126,   127,
     128,   129,   130,   131,   132,   360,    57,  1082,   859,  1424,
     689,   623,   901,   980,  1137,  1437,   282,   283,   284,   285,
     286,   287,   288,   706,   871,   291,   866,    10,  1146,   491,
     130,   297,   160,   161,   162,   736,   440,    -1,   542,    -1,
      -1,  1441,  1442,  1443,    -1,  1561,    -1,    -1,    -1,    -1,
      14,   273,   274,    17,   276,   277,   278,    -1,   280,   281,
     282,   283,   284,   285,   286,   287,   288,    -1,    18,   197,
      -1,    -1,    -1,    -1,  1595,   297,    40,    41,    -1,    -1,
      -1,    45,  1599,    47,   170,   171,   172,   173,    -1,    -1,
      54,    -1,    -1,  1493,    -1,    -1,    -1,    -1,  1498,  1499,
    1500,    -1,  1502,    -1,  1504,  1505,  1506,    71,   194,   195,
     196,   197,    -1,    -1,    -1,  1503,    -1,    -1,    -1,  1510,
      -1,    -1,    86,    -1,    -1,    -1,  1526,  1527,  1528,    -1,
       5,     6,    -1,     8,    -1,    -1,    -1,    -1,    13,    -1,
      -1,    16,    -1,  1558,    -1,    20,    21,    22,    23,    24,
      -1,    26,    27,    28,    29,    -1,    31,    32,    -1,    -1,
      35,    36,    37,    38,    -1,  1598,    41,    42,    43,    -1,
      -1,   299,    -1,    -1,    -1,    -1,    51,    -1,    53,    54,
     308,    -1,    -1,    -1,    -1,    -1,  1601,    -1,    -1,    -1,
    1590,    -1,    -1,    68,    69,    70,   282,   283,   284,   285,
     286,   287,   288,    -1,    79,   291,    -1,    -1,    -1,    -1,
      -1,   297,    -1,    -1,    -1,    -1,    -1,    -1,    93,    94,
      95,    96,    -1,    -1,    99,    -1,    -1,   177,   178,   104,
      -1,   106,   107,   108,    -1,    -1,    -1,    -1,    -1,    -1,
      18,    -1,    -1,    -1,    -1,   120,   121,   197,    -1,    -1,
      -1,   126,   127,   128,   129,   130,   131,   132,    -1,   209,
      -1,   211,    -1,    41,   214,   215,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
       5,     6,    -1,     8,    -1,   160,   161,   162,    13,    -1,
      -1,    16,    -1,    -1,    -1,    20,    21,    22,    23,    24,
      -1,    26,    27,    28,    29,    -1,    31,    32,    -1,    -1,
      35,    36,    37,    38,    -1,    -1,    41,    42,    43,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    51,    -1,    53,    54,
      -1,    -1,   282,   283,   284,   285,   286,   287,   288,    -1,
      -1,   291,    -1,    68,    69,    70,    -1,   297,    -1,    -1,
      -1,    -1,    -1,    -1,    79,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    93,    94,
      95,    96,    -1,    -1,    99,    -1,    -1,   155,    -1,   104,
      -1,   106,   107,   108,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   120,   121,    -1,    -1,    -1,
      -1,   126,   127,   128,   129,   130,   131,   132,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   197,
      -1,    -1,    -1,    -1,   299,    -1,   301,    -1,    -1,   207,
     305,   306,   210,   308,    -1,   160,   161,   162,     5,     6,
      -1,     8,    -1,    -1,    -1,    -1,    13,    -1,    -1,    16,
      -1,    -1,    -1,    20,    21,    22,    23,    24,    -1,    26,
      27,    28,    29,    -1,    31,    32,    -1,    -1,    35,    36,
      37,    38,    -1,    -1,    41,    42,    43,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    51,    -1,    53,    54,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    68,    69,    70,   282,   283,   284,   285,   286,   287,
     288,    -1,    79,   291,    -1,    -1,    -1,    -1,    -1,   297,
      -1,    -1,    -1,    -1,    -1,    -1,    93,    94,    95,    96,
      -1,    -1,    99,    -1,    -1,    -1,    -1,   104,    -1,   106,
     107,   108,    -1,    -1,    -1,    -1,    -1,    -1,    18,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   126,
     127,   128,   129,   130,   131,   132,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   299,    -1,   301,    -1,    -1,    -1,
     305,   306,    -1,   308,    -1,    -1,    -1,    -1,     5,     6,
      -1,     8,    -1,   160,   161,   162,    13,    -1,    -1,    16,
      -1,    -1,    -1,    20,    21,    22,    23,    24,    -1,    26,
      27,    28,    29,    -1,    31,    32,    -1,    -1,    35,    36,
      37,    38,    -1,    -1,    41,    42,    43,    -1,    -1,    39,
      -1,    -1,    -1,    -1,    51,    -1,    53,    54,    -1,    -1,
      -1,    -1,    -1,    -1,    54,    -1,    -1,    -1,    -1,    -1,
      -1,    68,    69,    70,    -1,    72,    -1,    -1,    -1,    -1,
      70,    -1,    79,    -1,    -1,    -1,    -1,    -1,    -1,    79,
      -1,    -1,    -1,    -1,    -1,    -1,    93,    94,    95,    96,
      -1,    -1,    99,    93,    94,    95,    96,   104,    -1,    99,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   118,    -1,   173,    -1,   175,   176,    -1,    -1,   126,
     127,   128,   129,   130,   131,   132,   126,   127,   128,   129,
     130,   131,   132,    -1,   194,   195,   196,   197,    -1,    -1,
      -1,    -1,   299,    -1,   301,    -1,    -1,    -1,   305,   306,
      -1,   308,    -1,   160,   161,   162,     5,     6,    -1,     8,
      -1,    -1,    -1,    -1,    13,    18,    -1,    16,    -1,    -1,
      -1,    20,    21,    22,    23,    24,    -1,    26,    27,    28,
      29,    -1,    31,    32,    -1,    -1,    35,    36,    37,    38,
      -1,    -1,    41,    42,    43,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    51,    -1,    53,    54,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    68,
      69,    70,   282,   283,   284,   285,   286,   287,   288,    -1,
      79,   291,    -1,    -1,    -1,    -1,    -1,   297,    -1,    -1,
      -1,    -1,    -1,    -1,    93,    94,    95,    96,    -1,    -1,
      99,    -1,    -1,    -1,    -1,   104,   105,   179,   180,   181,
     182,   183,   184,   185,   186,   187,   188,   189,   190,   191,
     192,   193,    -1,    -1,    -1,    -1,    -1,   126,   127,   128,
     129,   130,   131,   132,    -1,    -1,    -1,    -1,    -1,    -1,
     290,     5,   299,    -1,     8,     9,    -1,    -1,    -1,     5,
       6,   308,     8,    -1,    -1,    -1,    -1,    13,    -1,    -1,
      16,   160,   161,   162,    20,    21,    22,    23,    24,    -1,
      26,    27,    28,    29,    -1,    31,    32,    -1,    -1,    35,
      36,    37,    38,    -1,    -1,    41,    42,    43,    -1,    -1,
      54,    -1,    -1,    -1,   197,    51,    -1,    53,    54,    -1,
      -1,    -1,    -1,    -1,   207,    -1,    70,   210,    -1,    -1,
      -1,    -1,    68,    69,    70,    79,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    79,    -1,    -1,    -1,    -1,    -1,    93,
      94,    95,    96,    -1,    -1,    99,    -1,    93,    94,    95,
      96,    -1,    -1,    99,    -1,    -1,    -1,    -1,   104,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   126,   127,   128,   129,   130,   131,   132,    -1,
     126,   127,   128,   129,   130,   131,   132,    -1,    -1,   282,
     283,   284,   285,   286,   287,   288,     5,     6,   291,     8,
      -1,    -1,    -1,    -1,   297,    -1,    -1,    -1,    -1,    -1,
     299,    -1,    -1,    -1,   160,   161,   162,     5,     6,   308,
       8,    -1,    -1,    -1,    -1,    13,    -1,    -1,    16,    -1,
      -1,    -1,    20,    21,    22,    23,    24,    -1,    26,    27,
      28,    29,    -1,    31,    32,    54,    -1,    35,    36,    37,
      38,    -1,    -1,    41,    42,    43,    -1,    -1,    -1,    -1,
      -1,    70,    -1,    51,    -1,    53,    54,    -1,    -1,    -1,
      79,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      68,    69,    70,    -1,    93,    94,    95,    96,    -1,    -1,
      99,    79,    -1,    -1,    -1,    -1,    -1,   106,   107,   108,
      -1,    -1,    -1,    -1,    -1,    93,    94,    95,    96,    -1,
      -1,    99,    -1,    -1,    -1,    -1,   104,   126,   127,   128,
     129,   130,   131,   132,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   290,    -1,   126,   127,
     128,   129,   130,   131,   132,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,     5,   299,    -1,     8,     9,    -1,    -1,    -1,
       5,     6,   308,     8,    -1,    -1,    -1,    -1,    13,    -1,
      -1,    16,   160,   161,   162,    20,    21,    22,    23,    24,
      -1,    26,    27,    28,    29,    -1,    31,    32,    -1,    -1,
      35,    36,    37,    38,    -1,    -1,    41,    42,    43,    -1,
      -1,    54,    -1,    -1,    -1,    -1,    51,    -1,    53,    54,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    70,    -1,    -1,
      -1,    -1,    -1,    68,    69,    70,    79,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    79,    -1,    -1,    -1,    -1,    -1,
      93,    94,    95,    96,    -1,    -1,    99,    -1,    93,    94,
      95,    96,    -1,    -1,    99,    -1,    -1,    -1,    -1,   104,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   126,   127,   128,   129,   130,   131,   132,
      -1,   126,   127,   128,   129,   130,   131,   132,    -1,    18,
     299,    -1,   301,    -1,    -1,    -1,   305,   306,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   299,    41,    -1,    -1,   160,   161,   162,     5,     6,
     308,     8,    -1,    -1,    -1,    -1,    13,    -1,    -1,    16,
      -1,    -1,    -1,    20,    21,    22,    23,    24,    -1,    26,
      27,    28,    29,    -1,    31,    32,    -1,    -1,    35,    36,
      37,    38,    -1,    -1,    41,    42,    43,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    51,    -1,    53,    54,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    68,    69,    70,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    79,    -1,    -1,    -1,    -1,     5,     6,    -1,
       8,    -1,    -1,    -1,    -1,    -1,    93,    94,    95,    96,
      -1,    -1,    99,    -1,    -1,     5,     6,   104,     8,     9,
      -1,    -1,    -1,    -1,    -1,    -1,   155,    -1,    -1,    -1,
      20,    -1,    -1,    41,    -1,    -1,    -1,    -1,    -1,   126,
     127,   128,   129,   130,   131,   132,    54,    -1,    -1,    -1,
      -1,    41,    -1,    -1,   299,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    70,   308,    54,    -1,    -1,    -1,   197,    -1,
      -1,    79,    -1,   160,   161,   162,    -1,    -1,   207,    -1,
      70,   210,    -1,    -1,    -1,    93,    94,    95,    96,    79,
      -1,    99,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    93,    94,    95,    96,    -1,    -1,    99,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   126,   127,
     128,   129,   130,   131,   132,     5,     6,     7,     8,     9,
      -1,    -1,    -1,    13,    -1,    -1,   126,   127,   128,   129,
     130,   131,   132,     5,     6,    -1,     8,    -1,    28,    -1,
      -1,    13,    -1,   282,   283,   284,   285,   286,   287,   288,
      -1,    41,   291,    -1,    -1,    -1,    -1,    -1,   297,    -1,
      -1,    -1,    -1,    -1,    54,    -1,    -1,    -1,    -1,    41,
      -1,    -1,    44,    -1,    -1,    65,    66,    -1,    -1,    51,
      70,    -1,    54,    -1,    -1,    -1,    -1,    -1,    -1,    79,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    70,    -1,
     218,    -1,   299,    93,    94,    95,    96,    79,    -1,    99,
      -1,   308,    -1,    -1,    -1,    -1,    -1,    -1,   218,    -1,
      -1,    93,    94,    95,    96,    -1,    -1,    99,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   126,   127,   128,   129,
     130,   131,   132,    -1,    -1,     5,     6,    -1,     8,    -1,
      -1,    -1,    -1,    13,   126,   127,   128,   129,   130,   131,
     132,    -1,    -1,    -1,    -1,    -1,     5,     6,    -1,     8,
     160,   161,   162,    -1,    13,    -1,    -1,    -1,    -1,    -1,
      -1,    41,    -1,    -1,    44,    -1,    -1,   305,   160,   161,
     162,    51,    -1,    -1,    54,    -1,    -1,    -1,    -1,    -1,
     300,    -1,    41,    -1,    -1,   305,    -1,    -1,    -1,    -1,
      70,    -1,    51,    -1,    -1,    54,    -1,    -1,    -1,    79,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    70,    -1,    93,    94,    95,    96,    -1,    -1,    99,
      79,    -1,    -1,    -1,    -1,     5,     6,    -1,     8,    -1,
      -1,    -1,    -1,    13,    93,    94,    95,    96,    -1,    -1,
      99,    -1,    -1,    -1,    -1,    -1,   126,   127,   128,   129,
     130,   131,   132,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    41,    -1,    -1,    -1,    -1,    -1,   126,   127,   128,
     129,   130,   131,   132,    54,    -1,    -1,    -1,    -1,    -1,
     160,   161,   162,    -1,    -1,    -1,    -1,    -1,    -1,   299,
      70,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    79,
      -1,   160,   161,   162,   296,    -1,    -1,   299,    -1,     5,
       6,    -1,     8,    93,    94,    95,    96,    13,    -1,    99,
       5,     6,    -1,     8,    -1,    -1,    -1,     5,     6,    -1,
       8,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    41,   126,   127,   128,   129,
     130,   131,   132,    -1,    -1,    -1,    -1,    -1,    54,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    54,
      -1,    -1,    -1,    -1,    70,    -1,    54,    -1,    -1,    -1,
     160,   161,   162,    79,    -1,    70,    -1,    -1,    -1,    -1,
      -1,    -1,    70,    -1,    79,    -1,    -1,    93,    94,    95,
      96,    79,    -1,    99,    -1,    -1,    -1,    -1,    93,    94,
      95,    96,    -1,    -1,    99,    93,    94,    95,    96,   299,
      -1,    99,     5,     6,    -1,     8,    -1,    -1,    -1,    -1,
     126,   127,   128,   129,   130,   131,   132,    -1,    -1,    -1,
     299,   126,   127,   128,   129,   130,   131,   132,   126,   127,
     128,   129,   130,   131,   132,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   160,   161,   162,    -1,    -1,    -1,
      -1,    54,    -1,    -1,    -1,    -1,    -1,    -1,     7,    -1,
      -1,    -1,    -1,    -1,    -1,     5,     6,    70,     8,    -1,
      -1,    -1,    -1,    22,    23,    24,    79,    26,    27,    28,
      29,    -1,    31,    32,    33,    -1,    35,    36,    37,    38,
      93,    94,    95,    96,    -1,    44,    99,    -1,    -1,   299,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    54,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   126,   127,   128,   129,   130,   131,   132,
      70,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    79,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    93,    94,    95,    96,    -1,    -1,    99,
      22,    23,    24,    -1,    26,    27,    28,    29,    -1,    31,
      32,    33,    -1,    35,    36,    37,    38,    -1,    -1,    -1,
      42,    43,    -1,   299,    -1,    -1,   126,   127,   128,   129,
     130,   131,   132,    -1,   299,    -1,    -1,    15,    16,    17,
      -1,   299,    20,    21,    22,    23,    24,    -1,    26,    27,
      28,    29,    -1,    31,    32,    -1,    -1,    35,    36,    37,
      38,    -1,    -1,    41,    42,    43,    -1,    -1,     5,    -1,
      -1,     8,    -1,    51,    -1,    53,    -1,    -1,    -1,    -1,
      -1,    18,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   111,
      68,    69,    70,    15,    16,    17,    -1,    -1,    20,    21,
      22,    23,    24,    -1,    26,    27,    28,    29,    -1,    31,
      32,    -1,    -1,    35,    36,    37,    38,    54,    -1,    -1,
      42,    43,    -1,    -1,    -1,    -1,   104,    -1,    -1,    51,
      -1,    53,    -1,    70,    -1,    -1,   299,    -1,    -1,    -1,
      -1,    -1,    79,    -1,    -1,    -1,    68,    69,    70,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    93,    94,    95,    96,
      -1,    -1,    99,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   104,    -1,    -1,    -1,    -1,    -1,    -1,   126,
     127,   128,   129,   130,   131,   132,    -1,    -1,    -1,   299,
      -1,    -1,    -1,    -1,    -1,   142,   143,   144,   145,   146,
     147,   148,   149,   150,   151,   152,   153,   154,    -1,    -1,
      -1,    -1,   159,    -1,    -1,    -1,   163,   164,   165,   166,
     167,   168,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     177,   178,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
       5,    -1,    -1,     8,    -1,    -1,    -1,   194,   195,   196,
     197,    -1,    -1,    18,    -1,    -1,    -1,    -1,   205,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   219,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    54,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    70,    -1,   254,   255,    -1,
      -1,    -1,    -1,    -1,    79,    -1,    -1,    -1,    -1,    -1,
     308,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    93,    94,
      95,    96,    -1,    -1,    99,   282,   283,   284,   285,   286,
     287,   288,    -1,   290,   291,    -1,    -1,    -1,    -1,    -1,
     297,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   126,   127,   128,   129,   130,   131,   132,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   308,   142,   143,   144,
     145,   146,   147,   148,   149,   150,   151,   152,   153,   154,
      -1,    -1,    -1,    -1,   159,    -1,    -1,    -1,   163,   164,
     165,   166,   167,   168,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   177,   178,     5,    -1,    -1,     8,    -1,    -1,
      -1,     5,     6,    -1,     8,    -1,    -1,    18,    -1,   194,
     195,   196,   197,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     205,    -1,    -1,    -1,    -1,    16,    -1,    -1,    -1,    20,
      21,    22,    23,    24,   219,    26,    27,    28,    29,    -1,
      31,    32,    -1,    54,    35,    36,    37,    38,    -1,    -1,
      54,    42,    43,    -1,    -1,    -1,    -1,    -1,    -1,    70,
      51,    -1,    53,    -1,    -1,    -1,    70,    -1,    79,   254,
     255,    -1,    -1,    -1,    -1,    79,    -1,    68,    69,    70,
      -1,    -1,    93,    94,    95,    96,    -1,    -1,    99,    93,
      94,    95,    96,    -1,    -1,    99,    -1,   282,   283,   284,
     285,   286,   287,   288,    -1,   290,   291,    -1,    -1,    -1,
      -1,    -1,   297,   104,    -1,   126,   127,   128,   129,   130,
     131,   132,   126,   127,   128,   129,   130,   131,   132,    -1,
      -1,   142,   143,   144,   145,   146,   147,   148,   149,   150,
     151,   152,   153,   154,    -1,    -1,    -1,    -1,   159,    -1,
      -1,    -1,   163,   164,   165,   166,   167,   168,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   177,   178,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   194,   195,   196,   197,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   205,    -1,    -1,    -1,    -1,    16,
      -1,    -1,    -1,    20,    21,    22,    23,    24,   219,    26,
      27,    28,    29,    -1,    31,    32,    -1,    -1,    35,    36,
      37,    38,    39,    -1,    -1,    42,    43,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    51,    -1,    53,    -1,    -1,    -1,
      -1,    -1,    -1,   254,   255,    -1,    -1,    -1,    -1,    -1,
      -1,    68,    69,    70,    -1,    72,    -1,    74,    75,    76,
      77,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   282,   283,   284,   285,   286,   287,   288,    -1,   290,
     291,    -1,    -1,    -1,    -1,    -1,   297,   104,    -1,    -1,
      -1,   295,    -1,    -1,    -1,   112,   113,   114,   115,    -1,
      -1,   118,    -1,    -1,    -1,   122,   123,   124,   299,    18,
      -1,    -1,    -1,    -1,    16,    -1,    -1,   308,    20,    21,
      22,    23,    24,    -1,    26,    27,    28,    29,    -1,    31,
      32,    -1,    -1,    35,    36,    37,    38,    -1,    -1,    -1,
      42,    43,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    51,
      -1,    53,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    68,    69,    70,    16,
      -1,    -1,    -1,    20,    21,    22,    23,    24,    -1,    26,
      27,    28,    29,    -1,    31,    32,    -1,    -1,    35,    36,
      37,    38,    -1,    -1,    -1,    42,    43,    -1,    -1,    -1,
      -1,    -1,   104,    -1,    51,    -1,    53,    -1,    -1,   226,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    68,    69,    70,    -1,    -1,    21,    22,    -1,    24,
      25,    26,    27,    28,    29,    -1,    31,    32,    -1,    34,
      35,    36,    37,    38,    -1,    -1,   155,   156,   157,   158,
     159,    -1,    -1,    -1,    -1,    -1,    -1,   104,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   177,   178,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   194,   195,   196,   197,    -1,
      -1,   308,   201,   202,    -1,    -1,     5,     6,   207,     8,
      -1,   210,    -1,    -1,    -1,    -1,    -1,    -1,   217,   218,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    22,    23,    24,    -1,    26,    27,    28,    29,    -1,
      31,    32,    33,    -1,    35,    36,    37,    38,    -1,    -1,
      18,    42,    43,    -1,    -1,    54,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    70,    -1,     5,   273,   274,     8,   276,   277,   278,
      79,   280,   281,   282,   283,   284,   285,   286,   287,   288,
      -1,    -1,   291,    -1,    93,    94,    95,    96,   297,    67,
      99,    -1,    18,    -1,    -1,    -1,    -1,   299,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   308,    -1,    -1,    -1,
     111,    -1,    54,    -1,    -1,    -1,    -1,   126,   127,   128,
     129,   130,   131,   132,    -1,    -1,    -1,    -1,    70,    -1,
      -1,    -1,    -1,    -1,    -1,   230,   231,    79,   233,   234,
     235,    67,   237,   238,    -1,    -1,   241,   242,    -1,   244,
      -1,    93,    94,    95,    96,    -1,    -1,    99,    -1,    -1,
      -1,   308,   257,   258,   259,   260,   261,   262,   263,   264,
     265,   266,   267,   268,   269,   270,   271,   155,    -1,   157,
     158,   159,    -1,    -1,   126,   127,   128,   129,   130,   131,
     132,   169,    -1,    -1,    -1,    -1,   174,    -1,    -1,   177,
     178,    -1,    -1,    -1,    -1,    -1,    -1,   216,    -1,    -1,
     305,    -1,    -1,    -1,    -1,    -1,   194,   195,   196,   197,
      -1,   199,   200,    -1,    -1,    -1,    -1,    -1,    -1,   155,
      -1,   157,   158,   159,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   219,    -1,   169,    -1,    -1,    -1,    -1,   174,    -1,
      -1,   177,   178,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   194,   195,
     196,   197,    -1,   199,   200,    -1,   254,   255,    -1,    -1,
      -1,   282,   283,   284,   285,   286,   287,   288,    -1,    -1,
      -1,    -1,    -1,   219,    -1,    -1,   297,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   282,   283,   284,   285,   286,   287,
     288,    -1,    -1,   291,    -1,    -1,    -1,    -1,    -1,   297,
      -1,    -1,    -1,    -1,    -1,    16,    -1,    -1,   254,   255,
      21,    22,    -1,    24,    25,    26,    27,    28,    29,    -1,
      31,    32,    -1,    34,    35,    36,    37,    38,    -1,    -1,
      -1,     5,     6,    -1,     8,    -1,   282,   283,   284,   285,
     286,   287,   288,    -1,    16,   291,    -1,    -1,    -1,    21,
      22,   297,    24,    25,    26,    27,    28,    29,    -1,    31,
      32,    -1,    34,    35,    36,    37,    38,    -1,    -1,    80,
      -1,    82,    -1,    -1,    -1,    -1,    87,    -1,    -1,    -1,
      54,    -1,    -1,    -1,    -1,     5,     6,    -1,     8,    -1,
      -1,    -1,    -1,   104,    -1,    -1,    70,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    79,    -1,    -1,    80,    -1,
      82,    -1,    -1,    -1,    -1,    87,    -1,    -1,    -1,    93,
      94,    95,    96,    -1,    -1,    99,   137,   101,   102,   103,
      -1,    -1,   104,    -1,    54,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      70,    -1,   126,   127,   128,   129,   130,   131,   132,    79,
      -1,    -1,    -1,    -1,    -1,   137,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    93,    94,    95,    96,    -1,    -1,    99,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   126,   127,   128,   129,
     130,   131,   132,    -1,    -1,    -1,   227,    -1,   229,   230,
     231,   232,   233,   234,   235,   236,   237,   238,   239,   240,
     241,   242,   243,   244,   245,    -1,    -1,    -1,   249,    -1,
       5,     6,    -1,     8,    -1,    -1,   220,   258,   222,   223,
     224,   225,    -1,    -1,    -1,   227,    -1,   229,   230,   231,
     232,   233,   234,   235,   236,   237,   238,   239,   240,   241,
     242,   243,   244,   245,    39,    -1,    -1,   249,    -1,   290,
      -1,    -1,    -1,    -1,    -1,    -1,   258,    -1,   208,    54,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    68,    -1,    70,    -1,    72,    -1,    -1,
      75,    76,    -1,    -1,    79,    80,    81,    82,    83,    84,
      85,    86,    87,    88,    89,    90,    91,    -1,    93,    94,
      95,    96,    -1,    98,    99,     5,     6,   102,     8,    -1,
      -1,    -1,    -1,    -1,   109,     5,     6,    -1,     8,    -1,
      -1,    -1,    -1,   118,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   126,   127,   128,   129,   130,   131,   132,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    54,    -1,    -1,    -1,    -1,     5,
       6,    -1,     8,    -1,    54,    -1,    -1,    -1,    -1,    -1,
      70,    -1,    -1,    73,    -1,    75,    76,    -1,    -1,    79,
      70,    -1,    82,    -1,    -1,    75,    76,    -1,    -1,    79,
      -1,    -1,    -1,    93,    94,    95,    96,    -1,    -1,    99,
      -1,    -1,    -1,    93,    94,    95,    96,    -1,    54,    99,
      -1,    -1,    -1,     5,     6,    -1,     8,     9,    -1,    -1,
      -1,    -1,    -1,    -1,    70,    -1,   126,   127,   128,   129,
     130,   131,   132,    79,    -1,    -1,   126,   127,   128,   129,
     130,   131,   132,    -1,    -1,    -1,    -1,    93,    94,    95,
      96,    -1,    -1,    99,    -1,    -1,     5,     6,    -1,     8,
      -1,    -1,    54,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   120,   121,    -1,    -1,    70,    -1,
     126,   127,   128,   129,   130,   131,   132,    79,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    93,    94,    95,    96,    54,    -1,    99,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    70,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      79,    -1,    -1,    -1,   126,   127,   128,   129,   130,   131,
     132,    -1,    -1,    -1,    93,    94,    95,    96,    16,    -1,
      99,    -1,    -1,    -1,    22,    23,    24,    -1,    26,    27,
      28,    29,    -1,    31,    32,    -1,    -1,    35,    36,    37,
      38,    -1,    -1,    -1,    42,    43,    -1,   126,   127,   128,
     129,   130,   131,   132,    -1,    -1,    -1,    -1,    -1,    39,
      40,    -1,    -1,    -1,    -1,    45,    -1,    47,    -1,    -1,
      -1,    -1,    -1,    -1,    54,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    71,    72,    91,    74,    75,    76,    77,    78,    -1,
      -1,    -1,    -1,    83,    -1,    -1,    86,    -1,    -1,    -1,
      -1,    -1,    92,   111,    -1,    -1,    -1,    97,    -1,    -1,
     100,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   112,   113,   114,   115,   116,   117,   118,   119,
      -1,    -1,    -1,    -1,    -1,   125
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
       8,    54,    70,    79,    93,    94,    95,    96,    99,   126,
     127,   128,   129,   130,   131,   132,   315,   316,   299,   363,
      64,   126,   405,   179,   180,   181,   182,   183,   184,   185,
     186,   187,   188,   189,   190,   191,   192,   193,   467,   467,
       8,    14,    17,    40,    41,    45,    47,    54,    71,    86,
     295,   326,   364,   365,   366,   367,   298,   299,   275,   471,
     216,   475,   492,   216,   316,     9,   317,   317,     9,    10,
     318,   318,    13,    16,    20,    21,    22,    23,    24,    26,
      27,    28,    29,    31,    32,    35,    36,    37,    38,    42,
      43,    51,    53,    68,    69,    70,   104,   105,   160,   161,
     162,   299,   308,   316,   322,   323,   367,   368,   426,   449,
     450,   455,   456,   290,   316,   316,   316,   316,     7,    12,
     412,   413,   412,   412,   290,   343,    60,   344,   290,   382,
     388,    23,    26,    27,    28,    29,    31,    32,    33,   290,
     306,   406,   409,   411,   412,   317,   290,   290,   290,   290,
     488,   294,   317,   360,   315,   299,   367,   426,   449,   451,
     455,     7,    33,   298,   313,   293,   295,   295,    46,    48,
      49,    50,   365,   365,   327,   368,   451,   298,    70,   455,
     295,   317,   317,   208,   316,   475,   101,   102,   103,   126,
     220,   222,   223,   224,   225,   316,    75,    76,   316,   316,
     455,    26,    27,    28,    29,   449,    51,   449,    24,    25,
      34,    15,    17,   455,   218,   305,   316,   367,   308,   316,
     317,   137,   137,   137,   364,   365,   137,   307,   106,   107,
     108,   137,   299,   301,   305,   306,   312,   449,   313,   296,
      12,   296,   296,   310,    39,    68,    72,    75,    76,    80,
      81,    82,    83,    84,    85,    86,    87,    88,    89,    90,
      91,    98,   102,   109,   118,   316,   451,    61,   345,   346,
      39,    72,    74,    75,    76,    77,    78,    83,    92,    97,
     100,   112,   113,   114,   115,   116,   117,   118,   119,   125,
     365,   142,   143,   144,   145,   146,   147,   148,   149,   150,
     151,   152,   153,   154,   163,   164,   165,   166,   167,   168,
     205,   219,   254,   255,   290,   291,   314,   315,   321,   332,
     387,   389,   390,   391,   392,   394,   395,   403,   427,   428,
     429,   430,   431,   432,   433,   434,   435,   436,   437,   438,
     439,   440,   441,   459,   469,   305,   295,   299,   408,   295,
     408,   295,   408,   295,   408,   295,   408,   295,   408,   295,
     407,   409,   295,   412,   296,     7,     8,   293,   304,   476,
     484,   489,   493,    73,    75,    76,    82,   316,   316,   300,
      39,    72,    74,    75,    76,    77,   112,   113,   114,   115,
     118,   122,   123,   124,   226,   455,   298,   218,   316,   365,
     295,   298,   295,   290,   295,   292,     8,   317,   317,   296,
     290,   295,   313,   120,   121,   299,   316,   384,   451,   300,
     167,   472,   316,   221,   137,   449,    25,   316,   451,   316,
     300,   300,   300,   316,   317,   316,   316,   316,   455,   316,
     316,   295,   295,   316,    20,   300,   317,   457,   458,   444,
     445,   455,   291,   312,   291,   295,    75,    76,    77,   112,
     113,   114,   301,   350,   347,   451,    67,   155,   169,   174,
     199,   200,   219,   254,   255,   291,   314,   321,   332,   342,
     358,   359,   369,   373,   381,   403,   459,   469,   487,   295,
     295,   385,   317,   317,   317,   299,   111,   289,   299,   104,
     451,   304,   198,   295,   388,    55,    57,    58,    59,   393,
     396,   397,   398,   399,   400,   401,   315,   317,   390,   315,
     317,   317,   318,    11,    31,    32,   295,   318,   319,   315,
     317,   364,    15,    17,   367,   455,   451,    87,   313,   411,
     365,   327,   295,   412,   295,   317,   317,   317,   317,   318,
     319,   319,   291,   293,   315,   296,   317,   317,   209,   211,
     214,   215,   291,   321,   332,   459,   477,   479,   480,   482,
      84,   209,   212,   291,   473,   479,   481,   485,    41,   155,
     207,   210,   291,   321,   332,   490,   207,   210,   291,   321,
     332,   494,    75,    76,    77,   112,   113,   114,   295,   295,
     316,   316,   300,   455,   313,   463,   464,   290,    51,   451,
     460,   461,     7,   293,   296,   296,   326,   328,   329,   301,
     357,   443,    19,   336,   473,   137,   316,    19,   300,   450,
     450,   450,   305,   451,   451,    20,   293,   300,   302,   293,
     317,    39,    51,    52,    69,   120,   292,   303,   351,   352,
     353,   293,   111,   370,   374,   317,   317,   488,   111,   289,
     104,   451,   290,   290,   290,   388,   290,   317,   313,   383,
     299,   455,   304,   317,   299,   316,   299,   316,   317,   365,
      19,   295,    20,   385,   446,   447,   448,   291,   451,   393,
      56,   390,   402,   315,   317,   390,   402,   402,   402,    62,
      62,   295,   295,   316,   451,   295,   412,   455,   315,   317,
     442,   296,   313,   296,   300,   296,   296,   296,   296,   296,
     407,   296,   304,     8,   293,   213,   298,   305,   317,   478,
     298,   313,   412,   412,   298,   298,   412,   412,   295,   216,
     317,   316,   216,   316,   216,   317,    16,    21,    22,    24,
      25,    26,    27,    28,    29,    31,    32,    34,    35,    36,
      37,    38,    80,    82,    87,   104,   137,   227,   229,   230,
     231,   232,   233,   234,   235,   236,   237,   238,   239,   240,
     241,   242,   243,   244,   245,   249,   258,   290,   379,   380,
     452,    63,   361,   300,   298,   296,   293,   328,     8,   298,
     291,   293,     8,   298,   291,    22,    23,    24,    26,    27,
      28,    29,    31,    32,    35,    36,    37,    38,    42,    43,
     111,   321,   330,   410,   411,   415,   299,   444,   295,   295,
     316,   384,    28,    29,    63,   203,   204,   206,   412,   316,
     316,   450,   295,   296,   296,   317,   458,   455,   296,   295,
     352,   295,   316,   355,   302,   451,   451,    72,   118,   316,
     451,    72,   118,   365,   316,   299,   316,   299,   316,   365,
      19,   346,   371,   375,   291,   489,   296,   137,   383,    39,
      46,    48,    49,    50,    87,    88,    89,   134,   135,   136,
     138,   141,   296,   251,   252,   253,   317,   226,   378,   317,
     300,   317,   317,   293,   300,   455,   384,   446,   455,   296,
     293,   315,   317,   315,   317,   317,   318,    19,   313,   296,
     295,   293,   293,   296,   296,   408,   408,   408,   408,   408,
     408,   317,   317,   317,   295,   304,   295,   296,   296,   295,
     295,   296,   296,   317,   450,   316,    63,   316,   296,    25,
      26,    27,    28,    29,   295,   453,   243,   235,   247,   295,
     228,   248,    22,   453,   453,    21,    22,    24,    25,    26,
      27,    28,    29,    31,    32,    34,    35,    36,    37,    38,
     230,   231,   233,   234,   235,   237,   238,   241,   242,   244,
     257,   258,   259,   260,   261,   262,   263,   264,   265,   266,
     267,   268,   269,   270,   271,   305,   454,   296,   413,   299,
     305,   315,   298,   362,    28,    65,    66,   313,   317,   449,
     465,   466,   463,   291,   298,   290,   460,   290,   295,   313,
     295,   299,   295,   299,    26,    27,    28,    29,   295,   299,
     295,   299,   295,   299,   295,   299,   295,   299,   295,   299,
     295,   299,   295,   299,   295,   299,   295,   299,   295,   299,
     295,   299,   295,   299,   105,   110,   321,   331,   412,   317,
     302,   446,   446,   357,   443,   315,   296,   446,   317,   348,
     349,   451,   293,   354,   316,   197,   322,   316,   455,   317,
     317,   293,   455,   384,   291,   170,   171,   172,   173,   291,
     314,   321,   332,   372,   469,   173,   175,   176,   291,   314,
     321,   332,   376,   469,   291,   313,   296,   295,   304,   304,
     300,   300,   300,   300,   295,   384,   137,   300,   300,   451,
     362,   451,   296,   378,   448,    62,    62,   296,   296,   316,
     296,   446,   442,   442,     8,   293,     8,   478,   296,   317,
     250,   313,   299,   299,    25,    26,    27,    28,    29,   272,
     293,   299,   306,   291,   292,   300,   317,    22,    23,    24,
      26,    27,    28,    29,    31,    32,    35,    36,    37,    38,
      44,   313,   410,   414,   295,   295,   290,   330,   328,   465,
     317,   317,   317,   295,   299,   295,   299,   295,   299,   295,
     299,   317,   317,   317,   317,   317,   317,   318,   317,   317,
     319,   317,   318,   319,   317,   317,   317,   317,   317,   317,
     317,   318,   317,   415,   317,     8,    44,   317,    44,    51,
     449,   317,    42,    91,   111,   333,   456,   296,   300,   296,
     296,   295,   295,   472,   296,   296,   296,   293,   353,   354,
     316,   300,   300,   451,   451,   256,   364,   364,   364,   364,
     364,   364,   364,   383,   317,   139,   140,   139,   140,   379,
     350,   315,   293,    19,   315,   315,   317,   296,   317,   304,
     298,   293,   317,   317,   313,   300,   317,   292,   300,    26,
      27,    28,    29,   317,    26,    27,    28,   317,   330,   291,
     291,   296,   300,   296,   300,   317,   317,   317,   317,   317,
     317,   318,   317,   296,   300,   296,   300,   296,   300,   296,
     300,   296,   296,   300,   296,   296,   300,   296,   300,   296,
     300,   296,   300,   296,   300,   296,   300,   296,   296,   300,
     296,     8,   296,   300,    51,   449,   299,   316,   302,   446,
     446,   451,   295,   293,    19,   365,   296,   296,   296,   295,
     451,   384,     8,   478,   317,   313,   300,   300,   300,   317,
     296,   304,   304,   304,   296,   291,   295,   295,   296,   300,
     296,   300,   296,   300,   296,   300,   295,   295,   295,   295,
     295,   295,   295,   295,   295,   295,   295,   295,   296,   295,
       8,   300,   298,   296,   296,   446,   451,   384,   455,   446,
     301,   356,   357,   304,   296,   293,   296,   452,   300,   317,
     317,   317,   422,   420,   295,   295,   295,   295,   421,   420,
     419,   418,   416,   417,   421,   420,   419,   418,   425,   423,
     424,   415,   296,   356,   451,   296,   295,   478,   313,   296,
     296,   296,   296,   465,   296,   317,   421,   420,   419,   418,
     296,   317,   296,   296,   317,   296,   318,   296,   317,   319,
     296,   318,   319,   296,   296,   296,   296,   296,   415,     8,
      44,   296,    44,    51,   296,   449,   362,   295,    19,   386,
     446,   293,   296,   296,   296,   296,     8,   446,   384,    39,
      54,    70,    79,    93,    94,    95,    96,    99,   126,   127,
     128,   129,   130,   131,   132,   290,   296,   313,   296,   295,
     295,   296,   256,   446,   317,   104,   296,   296,   365,   455,
     451,    19,   384,   356,   295,   446,   296
};

/* YYR1[RULE-NUM] -- Symbol kind of the left-hand side of rule RULE-NUM.  */
static const yytype_int16 yyr1[] =
{
       0,   309,   310,   310,   311,   311,   311,   311,   311,   311,
     311,   311,   311,   311,   311,   311,   311,   311,   311,   311,
     311,   311,   311,   311,   311,   311,   311,   311,   311,   311,
     312,   312,   313,   313,   314,   314,   314,   315,   315,   315,
     315,   315,   315,   315,   315,   315,   315,   315,   315,   315,
     315,   315,   315,   315,   316,   316,   316,   317,   318,   318,
     319,   319,   319,   320,   320,   320,   320,   320,   321,   321,
     321,   321,   321,   321,   321,   321,   321,   322,   322,   322,
     322,   323,   323,   323,   323,   324,   325,   326,   327,   327,
     328,   329,   329,   329,   330,   330,   330,   331,   331,   332,
     332,   332,   333,   333,   333,   333,   333,   333,   334,   334,
     334,   335,   336,   336,   336,   336,   336,   336,   337,   338,
     339,   340,   341,   342,   343,   343,   343,   343,   343,   343,
     343,   343,   343,   343,   343,   343,   343,   343,   343,   343,
     343,   343,   343,   343,   343,   343,   343,   343,   343,   343,
     343,   344,   344,   345,   345,   346,   346,   347,   347,   348,
     348,   349,   349,   350,   350,   351,   351,   351,   351,   351,
     351,   351,   352,   352,   353,   353,   354,   354,   355,   356,
     356,   357,   358,   358,   358,   358,   358,   358,   358,   358,
     358,   358,   358,   358,   358,   358,   358,   358,   358,   358,
     358,   358,   358,   359,   360,   360,   360,   360,   360,   360,
     360,   360,   360,   360,   360,   360,   360,   360,   360,   360,
     361,   361,   362,   362,   363,   363,   364,   364,   364,   364,
     364,   364,   364,   365,   365,   365,   365,   366,   366,   366,
     366,   366,   366,   366,   366,   367,   368,   368,   368,   368,
     368,   368,   369,   369,   370,   370,   370,   371,   371,   372,
     372,   372,   372,   372,   372,   372,   372,   373,   374,   374,
     374,   375,   375,   376,   376,   376,   376,   376,   376,   376,
     377,   378,   378,   379,   379,   380,   381,   382,   382,   382,
     382,   382,   382,   382,   382,   382,   382,   382,   382,   382,
     382,   382,   382,   382,   382,   382,   382,   382,   382,   382,
     383,   383,   383,   383,   383,   383,   383,   383,   383,   383,
     383,   383,   383,   383,   383,   383,   384,   384,   384,   385,
     385,   385,   385,   385,   386,   386,   386,   386,   386,   386,
     386,   386,   386,   386,   386,   386,   386,   386,   386,   386,
     386,   387,   388,   388,   389,   389,   389,   389,   389,   389,
     389,   389,   389,   389,   389,   389,   389,   389,   389,   389,
     389,   389,   389,   389,   389,   389,   389,   389,   389,   389,
     390,   391,   392,   393,   393,   394,   394,   394,   395,   396,
     396,   396,   396,   397,   397,   397,   398,   399,   400,   401,
     402,   402,   402,   403,   404,   404,   405,   405,   405,   406,
     406,   407,   407,   408,   408,   409,   409,   409,   409,   409,
     409,   409,   409,   409,   409,   409,   409,   409,   409,   409,
     410,   410,   410,   410,   410,   410,   410,   410,   410,   410,
     410,   410,   410,   410,   410,   410,   410,   410,   410,   411,
     412,   412,   413,   413,   414,   414,   414,   415,   415,   415,
     415,   415,   415,   415,   415,   415,   415,   415,   415,   415,
     415,   415,   415,   415,   415,   415,   415,   415,   415,   415,
     415,   415,   415,   416,   416,   416,   417,   417,   417,   418,
     418,   419,   419,   420,   420,   421,   421,   422,   422,   423,
     423,   423,   424,   424,   424,   424,   425,   425,   426,   427,
     428,   429,   430,   431,   432,   433,   434,   435,   436,   437,
     438,   439,   440,   441,   441,   441,   441,   441,   441,   441,
     441,   441,   441,   441,   441,   441,   441,   441,   441,   441,
     441,   441,   441,   441,   441,   441,   442,   442,   442,   442,
     442,   443,   443,   444,   444,   445,   445,   446,   446,   447,
     447,   448,   448,   448,   449,   449,   449,   449,   449,   449,
     449,   449,   449,   449,   450,   450,   451,   451,   451,   451,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     452,   452,   452,   452,   452,   453,   453,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   455,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   455,   455,   455,   455,   455,
     456,   456,   456,   456,   456,   456,   456,   456,   456,   456,
     456,   456,   456,   456,   456,   456,   456,   456,   457,   457,
     458,   458,   458,   458,   458,   459,   459,   459,   459,   459,
     459,   460,   460,   460,   461,   461,   462,   462,   463,   463,
     464,   465,   465,   466,   466,   466,   466,   466,   466,   466,
     466,   467,   467,   467,   467,   467,   467,   467,   467,   467,
     467,   467,   467,   467,   467,   467,   468,   468,   469,   469,
     469,   469,   469,   469,   469,   469,   469,   469,   469,   470,
     470,   471,   471,   472,   472,   473,   474,   475,   475,   475,
     475,   475,   475,   475,   475,   475,   475,   476,   476,   477,
     477,   477,   478,   478,   479,   479,   479,   479,   479,   479,
     480,   481,   482,   483,   483,   484,   484,   485,   485,   485,
     485,   486,   487,   488,   488,   488,   488,   488,   488,   488,
     488,   488,   488,   489,   489,   490,   490,   490,   490,   490,
     490,   490,   491,   491,   492,   492,   492,   493,   493,   494,
     494,   494,   494
};

/* YYR2[RULE-NUM] -- Number of symbols on the right-hand side of rule RULE-NUM.  */
static const yytype_int8 yyr2[] =
{
       0,     2,     0,     2,     4,     4,     3,     1,     1,     1,
       1,     1,     1,     4,     4,     4,     4,     1,     1,     1,
       2,     2,     3,     2,     2,     1,     1,     1,     4,     1,
       0,     2,     1,     3,     2,     4,     6,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     3,     1,     1,     1,
       1,     4,     4,     4,     4,     4,     4,     4,     2,     3,
       2,     2,     2,     1,     1,     2,     1,     2,     4,     6,
       3,     5,     7,     9,     3,     4,     7,     1,     1,     1,
       2,     0,     2,     2,     0,     6,     2,     1,     1,     1,
       1,     1,     1,     1,     1,     3,     2,     3,     1,     2,
       3,     7,     0,     2,     2,     2,     2,     2,     3,     3,
       2,     1,     4,     3,     0,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     3,     3,     3,     3,     3,     3,     2,     2,     2,
       5,     0,     2,     0,     2,     0,     2,     3,     1,     0,
       1,     1,     3,     0,     3,     1,     1,     1,     1,     1,
       1,     4,     0,     2,     4,     3,     0,     2,     3,     0,
       1,     5,     3,     4,     4,     4,     1,     1,     1,     1,
       1,     2,     2,     4,    13,    22,     1,     1,     5,     3,
       7,     5,     4,     7,     0,     2,     2,     2,     2,     2,
       2,     2,     5,     2,     2,     2,     2,     2,     2,     5,
       0,     2,     0,     2,     0,     3,     9,     9,     7,     7,
       1,     1,     1,     2,     2,     1,     4,     0,     1,     1,
       2,     2,     2,     2,     1,     4,     2,     5,     3,     2,
       2,     1,     4,     3,     0,     2,     2,     0,     2,     2,
       2,     2,     2,     1,     1,     1,     1,     9,     0,     2,
       2,     0,     2,     2,     2,     2,     1,     1,     1,     1,
       1,     0,     4,     1,     3,     1,    13,     0,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     5,     8,     6,     5,
       0,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     4,     4,     4,     4,     5,     1,     1,     1,     0,
       4,     4,     4,     4,     0,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       5,     1,     0,     2,     2,     1,     2,     4,     5,     1,
       1,     1,     1,     2,     1,     1,     1,     1,     1,     4,
       6,     4,     4,    11,     1,     5,     3,     7,     5,     5,
       3,     1,     2,     2,     1,     2,     4,     4,     1,     2,
       2,     2,     2,     2,     2,     2,     1,     2,     1,     1,
       1,     4,     4,     2,     4,     2,     0,     1,     1,     3,
       1,     3,     1,     0,     3,     5,     4,     3,     5,     5,
       5,     5,     5,     5,     2,     2,     2,     2,     2,     2,
       4,     4,     4,     4,     4,     4,     4,     4,     5,     5,
       5,     5,     4,     4,     4,     4,     4,     4,     3,     2,
       0,     1,     1,     2,     1,     1,     1,     1,     4,     4,
       5,     4,     4,     4,     7,     7,     7,     7,     7,     7,
       7,     7,     7,     7,     8,     8,     8,     8,     7,     7,
       7,     7,     7,     0,     2,     2,     0,     2,     2,     0,
       2,     0,     2,     0,     2,     0,     2,     0,     2,     0,
       2,     2,     0,     2,     3,     2,     0,     2,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     2,     1,     2,     2,     2,     2,     2,     2,
       3,     2,     2,     2,     5,     3,     2,     2,     2,     2,
       2,     5,     4,     6,     2,     4,     0,     3,     3,     1,
       1,     0,     3,     0,     1,     1,     3,     0,     1,     1,
       3,     1,     3,     4,     4,     4,     4,     5,     1,     1,
       1,     1,     1,     1,     1,     3,     1,     3,     4,     1,
       0,    10,     6,     5,     6,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     2,     2,     2,
       2,     1,     1,     1,     1,     2,     3,     4,     6,     5,
       1,     1,     1,     1,     1,     1,     1,     2,     2,     1,
       2,     2,     4,     1,     2,     1,     2,     1,     2,     1,
       2,     1,     2,     1,     1,     0,     5,     0,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     2,
       2,     2,     2,     1,     1,     1,     1,     1,     3,     2,
       2,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       2,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     2,     1,     3,     2,
       3,     4,     2,     2,     2,     5,     5,     7,     4,     3,
       2,     3,     2,     1,     1,     2,     3,     2,     1,     2,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     2,
       2,     2,     2,     1,     1,     1,     1,     1,     1,     3,
       0,     1,     1,     3,     2,     6,     7,     3,     3,     3,
       6,     0,     1,     3,     5,     6,     4,     4,     1,     3,
       3,     1,     1,     1,     1,     4,     1,     6,     6,     6,
       4,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     3,     2,
       5,     4,     7,     6,     7,     6,     9,     8,     3,     8,
       4,     0,     2,     0,     1,     3,     3,     0,     2,     2,
       2,     3,     2,     2,     2,     2,     2,     0,     2,     3,
       1,     1,     1,     1,     3,     8,     2,     3,     1,     1,
       3,     3,     3,     4,     6,     0,     2,     3,     1,     3,
       1,     4,     3,     0,     2,     2,     2,     3,     3,     3,
       3,     3,     3,     0,     2,     2,     3,     3,     4,     2,
       1,     1,     3,     5,     0,     2,     2,     0,     2,     4,
       3,     1,     1
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
#line 3902 "prebuilt\\asmparse.cpp"
    break;

  case 5: /* decl: nameSpaceHead '{' decls '}'  */
#line 194 "asmparse.y"
                                                                                { PASM->EndNameSpace(); }
#line 3908 "prebuilt\\asmparse.cpp"
    break;

  case 6: /* decl: methodHead methodDecls '}'  */
#line 195 "asmparse.y"
                                                                                { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                                                  {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                                     PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                                                  PASM->EndMethod(); }
#line 3917 "prebuilt\\asmparse.cpp"
    break;

  case 13: /* decl: assemblyHead '{' assemblyDecls '}'  */
#line 205 "asmparse.y"
                                                                                { PASMM->EndAssembly(); }
#line 3923 "prebuilt\\asmparse.cpp"
    break;

  case 14: /* decl: assemblyRefHead '{' assemblyRefDecls '}'  */
#line 206 "asmparse.y"
                                                                                { PASMM->EndAssembly(); }
#line 3929 "prebuilt\\asmparse.cpp"
    break;

  case 15: /* decl: exptypeHead '{' exptypeDecls '}'  */
#line 207 "asmparse.y"
                                                                                { PASMM->EndComType(); }
#line 3935 "prebuilt\\asmparse.cpp"
    break;

  case 16: /* decl: manifestResHead '{' manifestResDecls '}'  */
#line 208 "asmparse.y"
                                                                                { PASMM->EndManifestRes(); }
#line 3941 "prebuilt\\asmparse.cpp"
    break;

  case 20: /* decl: _SUBSYSTEM int32  */
#line 212 "asmparse.y"
                                                                                {
                                                                                  PASM->m_dwSubsystem = (yyvsp[0].int32);
                                                                                }
#line 3949 "prebuilt\\asmparse.cpp"
    break;

  case 21: /* decl: _CORFLAGS int32  */
#line 215 "asmparse.y"
                                                                                { PASM->m_dwComImageFlags = (yyvsp[0].int32); }
#line 3955 "prebuilt\\asmparse.cpp"
    break;

  case 22: /* decl: _FILE ALIGNMENT_ int32  */
#line 216 "asmparse.y"
                                                                                { PASM->m_dwFileAlignment = (yyvsp[0].int32);
                                                                                  if(((yyvsp[0].int32) & ((yyvsp[0].int32) - 1))||((yyvsp[0].int32) < 0x200)||((yyvsp[0].int32) > 0x10000))
                                                                                    PASM->report->error("Invalid file alignment, must be power of 2 from 0x200 to 0x10000\n");}
#line 3963 "prebuilt\\asmparse.cpp"
    break;

  case 23: /* decl: _IMAGEBASE int64  */
#line 219 "asmparse.y"
                                                                                { PASM->m_stBaseAddress = (ULONGLONG)(*((yyvsp[0].int64))); delete (yyvsp[0].int64);
                                                                                  if(PASM->m_stBaseAddress & 0xFFFF)
                                                                                    PASM->report->error("Invalid image base, must be 0x10000-aligned\n");}
#line 3971 "prebuilt\\asmparse.cpp"
    break;

  case 24: /* decl: _STACKRESERVE int64  */
#line 222 "asmparse.y"
                                                                                { PASM->m_stSizeOfStackReserve = (size_t)(*((yyvsp[0].int64))); delete (yyvsp[0].int64); }
#line 3977 "prebuilt\\asmparse.cpp"
    break;

  case 29: /* decl: _MSCORLIB  */
#line 227 "asmparse.y"
                                                                                { PASM->m_fIsMscorlib = TRUE; }
#line 3983 "prebuilt\\asmparse.cpp"
    break;

  case 32: /* compQstring: QSTRING  */
#line 234 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[0].binstr); }
#line 3989 "prebuilt\\asmparse.cpp"
    break;

  case 33: /* compQstring: compQstring '+' QSTRING  */
#line 235 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 3995 "prebuilt\\asmparse.cpp"
    break;

  case 34: /* languageDecl: _LANGUAGE SQSTRING  */
#line 238 "asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[0].string),&(PASM->m_guidLang)); }
#line 4001 "prebuilt\\asmparse.cpp"
    break;

  case 35: /* languageDecl: _LANGUAGE SQSTRING ',' SQSTRING  */
#line 239 "asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid((yyvsp[0].string),&(PASM->m_guidLangVendor));}
#line 4008 "prebuilt\\asmparse.cpp"
    break;

  case 36: /* languageDecl: _LANGUAGE SQSTRING ',' SQSTRING ',' SQSTRING  */
#line 241 "asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[-4].string),&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidLangVendor));
                                                                                  LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidDoc));}
#line 4016 "prebuilt\\asmparse.cpp"
    break;

  case 37: /* id: ID  */
#line 246 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 4022 "prebuilt\\asmparse.cpp"
    break;

  case 38: /* id: NATIVE_  */
#line 248 "asmparse.y"
                                                              { (yyval.string) = newString("native"); }
#line 4028 "prebuilt\\asmparse.cpp"
    break;

  case 39: /* id: CIL_  */
#line 249 "asmparse.y"
                                                              { (yyval.string) = newString("cil"); }
#line 4034 "prebuilt\\asmparse.cpp"
    break;

  case 40: /* id: OPTIL_  */
#line 250 "asmparse.y"
                                                              { (yyval.string) = newString("optil"); }
#line 4040 "prebuilt\\asmparse.cpp"
    break;

  case 41: /* id: MANAGED_  */
#line 251 "asmparse.y"
                                                              { (yyval.string) = newString("managed"); }
#line 4046 "prebuilt\\asmparse.cpp"
    break;

  case 42: /* id: UNMANAGED_  */
#line 252 "asmparse.y"
                                                              { (yyval.string) = newString("unmanaged"); }
#line 4052 "prebuilt\\asmparse.cpp"
    break;

  case 43: /* id: FORWARDREF_  */
#line 253 "asmparse.y"
                                                              { (yyval.string) = newString("forwardref"); }
#line 4058 "prebuilt\\asmparse.cpp"
    break;

  case 44: /* id: PRESERVESIG_  */
#line 254 "asmparse.y"
                                                              { (yyval.string) = newString("preservesig"); }
#line 4064 "prebuilt\\asmparse.cpp"
    break;

  case 45: /* id: RUNTIME_  */
#line 255 "asmparse.y"
                                                              { (yyval.string) = newString("runtime"); }
#line 4070 "prebuilt\\asmparse.cpp"
    break;

  case 46: /* id: INTERNALCALL_  */
#line 256 "asmparse.y"
                                                              { (yyval.string) = newString("internalcall"); }
#line 4076 "prebuilt\\asmparse.cpp"
    break;

  case 47: /* id: SYNCHRONIZED_  */
#line 257 "asmparse.y"
                                                              { (yyval.string) = newString("synchronized"); }
#line 4082 "prebuilt\\asmparse.cpp"
    break;

  case 48: /* id: NOINLINING_  */
#line 258 "asmparse.y"
                                                              { (yyval.string) = newString("noinlining"); }
#line 4088 "prebuilt\\asmparse.cpp"
    break;

  case 49: /* id: AGGRESSIVEINLINING_  */
#line 259 "asmparse.y"
                                                              { (yyval.string) = newString("aggressiveinlining"); }
#line 4094 "prebuilt\\asmparse.cpp"
    break;

  case 50: /* id: NOOPTIMIZATION_  */
#line 260 "asmparse.y"
                                                              { (yyval.string) = newString("nooptimization"); }
#line 4100 "prebuilt\\asmparse.cpp"
    break;

  case 51: /* id: AGGRESSIVEOPTIMIZATION_  */
#line 261 "asmparse.y"
                                                              { (yyval.string) = newString("aggressiveoptimization"); }
#line 4106 "prebuilt\\asmparse.cpp"
    break;

  case 52: /* id: ASYNC_  */
#line 262 "asmparse.y"
                                                              { (yyval.string) = newString("async"); }
#line 4112 "prebuilt\\asmparse.cpp"
    break;

  case 53: /* id: SQSTRING  */
#line 263 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 4118 "prebuilt\\asmparse.cpp"
    break;

  case 54: /* dottedName: id  */
#line 266 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 4124 "prebuilt\\asmparse.cpp"
    break;

  case 55: /* dottedName: DOTTEDNAME  */
#line 267 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 4130 "prebuilt\\asmparse.cpp"
    break;

  case 56: /* dottedName: dottedName '.' dottedName  */
#line 268 "asmparse.y"
                                                              { (yyval.string) = newStringWDel((yyvsp[-2].string), '.', (yyvsp[0].string)); }
#line 4136 "prebuilt\\asmparse.cpp"
    break;

  case 57: /* int32: INT32_V  */
#line 271 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 4142 "prebuilt\\asmparse.cpp"
    break;

  case 58: /* int64: INT64_V  */
#line 274 "asmparse.y"
                                                              { (yyval.int64) = (yyvsp[0].int64); }
#line 4148 "prebuilt\\asmparse.cpp"
    break;

  case 59: /* int64: INT32_V  */
#line 275 "asmparse.y"
                                                              { (yyval.int64) = neg ? new int64_t((yyvsp[0].int32)) : new int64_t((unsigned)(yyvsp[0].int32)); }
#line 4154 "prebuilt\\asmparse.cpp"
    break;

  case 60: /* float64: FLOAT64  */
#line 278 "asmparse.y"
                                                              { (yyval.float64) = (yyvsp[0].float64); }
#line 4160 "prebuilt\\asmparse.cpp"
    break;

  case 61: /* float64: FLOAT32_ '(' int32 ')'  */
#line 279 "asmparse.y"
                                                              { float f; *((int32_t*) (&f)) = (yyvsp[-1].int32); (yyval.float64) = new double(f); }
#line 4166 "prebuilt\\asmparse.cpp"
    break;

  case 62: /* float64: FLOAT64_ '(' int64 ')'  */
#line 280 "asmparse.y"
                                                              { (yyval.float64) = (double*) (yyvsp[-1].int64); }
#line 4172 "prebuilt\\asmparse.cpp"
    break;

  case 63: /* typedefDecl: _TYPEDEF type AS_ dottedName  */
#line 284 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].binstr),(yyvsp[0].string)); }
#line 4178 "prebuilt\\asmparse.cpp"
    break;

  case 64: /* typedefDecl: _TYPEDEF className AS_ dottedName  */
#line 285 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].token),(yyvsp[0].string)); }
#line 4184 "prebuilt\\asmparse.cpp"
    break;

  case 65: /* typedefDecl: _TYPEDEF memberRef AS_ dottedName  */
#line 286 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].token),(yyvsp[0].string)); }
#line 4190 "prebuilt\\asmparse.cpp"
    break;

  case 66: /* typedefDecl: _TYPEDEF customDescr AS_ dottedName  */
#line 287 "asmparse.y"
                                                                                { (yyvsp[-2].cad)->tkOwner = 0; PASM->AddTypeDef((yyvsp[-2].cad),(yyvsp[0].string)); }
#line 4196 "prebuilt\\asmparse.cpp"
    break;

  case 67: /* typedefDecl: _TYPEDEF customDescrWithOwner AS_ dottedName  */
#line 288 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].cad),(yyvsp[0].string)); }
#line 4202 "prebuilt\\asmparse.cpp"
    break;

  case 68: /* compControl: P_DEFINE dottedName  */
#line 293 "asmparse.y"
                                                                                { DefineVar((yyvsp[0].string), NULL); }
#line 4208 "prebuilt\\asmparse.cpp"
    break;

  case 69: /* compControl: P_DEFINE dottedName compQstring  */
#line 294 "asmparse.y"
                                                                                { DefineVar((yyvsp[-1].string), (yyvsp[0].binstr)); }
#line 4214 "prebuilt\\asmparse.cpp"
    break;

  case 70: /* compControl: P_UNDEF dottedName  */
#line 295 "asmparse.y"
                                                                                { UndefVar((yyvsp[0].string)); }
#line 4220 "prebuilt\\asmparse.cpp"
    break;

  case 71: /* compControl: P_IFDEF dottedName  */
#line 296 "asmparse.y"
                                                                                { SkipToken = !IsVarDefined((yyvsp[0].string));
                                                                                  IfEndif++;
                                                                                }
#line 4228 "prebuilt\\asmparse.cpp"
    break;

  case 72: /* compControl: P_IFNDEF dottedName  */
#line 299 "asmparse.y"
                                                                                { SkipToken = IsVarDefined((yyvsp[0].string));
                                                                                  IfEndif++;
                                                                                }
#line 4236 "prebuilt\\asmparse.cpp"
    break;

  case 73: /* compControl: P_ELSE  */
#line 302 "asmparse.y"
                                                                                { if(IfEndif == 1) SkipToken = !SkipToken;}
#line 4242 "prebuilt\\asmparse.cpp"
    break;

  case 74: /* compControl: P_ENDIF  */
#line 303 "asmparse.y"
                                                                                { if(IfEndif == 0)
                                                                                    PASM->report->error("Unmatched #endif\n");
                                                                                  else IfEndif--;
                                                                                }
#line 4251 "prebuilt\\asmparse.cpp"
    break;

  case 75: /* compControl: P_INCLUDE QSTRING  */
#line 307 "asmparse.y"
                                                                                { _ASSERTE(!"yylex should have dealt with this"); }
#line 4257 "prebuilt\\asmparse.cpp"
    break;

  case 76: /* compControl: ';'  */
#line 308 "asmparse.y"
                                                                                { }
#line 4263 "prebuilt\\asmparse.cpp"
    break;

  case 77: /* customDescr: _CUSTOM customType  */
#line 312 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[0].token), NULL); }
#line 4269 "prebuilt\\asmparse.cpp"
    break;

  case 78: /* customDescr: _CUSTOM customType '=' compQstring  */
#line 313 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].token), (yyvsp[0].binstr)); }
#line 4275 "prebuilt\\asmparse.cpp"
    break;

  case 79: /* customDescr: _CUSTOM customType '=' '{' customBlobDescr '}'  */
#line 314 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 4281 "prebuilt\\asmparse.cpp"
    break;

  case 80: /* customDescr: customHead bytes ')'  */
#line 315 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].int32), (yyvsp[-1].binstr)); }
#line 4287 "prebuilt\\asmparse.cpp"
    break;

  case 81: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType  */
#line 318 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-2].token), (yyvsp[0].token), NULL); }
#line 4293 "prebuilt\\asmparse.cpp"
    break;

  case 82: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType '=' compQstring  */
#line 319 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-4].token), (yyvsp[-2].token), (yyvsp[0].binstr)); }
#line 4299 "prebuilt\\asmparse.cpp"
    break;

  case 83: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType '=' '{' customBlobDescr '}'  */
#line 321 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-6].token), (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 4305 "prebuilt\\asmparse.cpp"
    break;

  case 84: /* customDescrWithOwner: customHeadWithOwner bytes ')'  */
#line 322 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].int32), (yyvsp[-1].binstr)); }
#line 4311 "prebuilt\\asmparse.cpp"
    break;

  case 85: /* customHead: _CUSTOM customType '=' '('  */
#line 325 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-2].token); bParsingByteArray = TRUE; }
#line 4317 "prebuilt\\asmparse.cpp"
    break;

  case 86: /* customHeadWithOwner: _CUSTOM '(' ownerType ')' customType '=' '('  */
#line 329 "asmparse.y"
                                                                                { PASM->m_pCustomDescrList = NULL;
                                                                                  PASM->m_tkCurrentCVOwner = (yyvsp[-4].token);
                                                                                  (yyval.int32) = (yyvsp[-2].token); bParsingByteArray = TRUE; }
#line 4325 "prebuilt\\asmparse.cpp"
    break;

  case 87: /* customType: methodRef  */
#line 334 "asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 4331 "prebuilt\\asmparse.cpp"
    break;

  case 88: /* ownerType: typeSpec  */
#line 337 "asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 4337 "prebuilt\\asmparse.cpp"
    break;

  case 89: /* ownerType: memberRef  */
#line 338 "asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 4343 "prebuilt\\asmparse.cpp"
    break;

  case 90: /* customBlobDescr: customBlobArgs customBlobNVPairs  */
#line 342 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr);
                                                                                  (yyval.binstr)->appendInt16(VAL16(nCustomBlobNVPairs));
                                                                                  (yyval.binstr)->append((yyvsp[0].binstr));
                                                                                  nCustomBlobNVPairs = 0; }
#line 4352 "prebuilt\\asmparse.cpp"
    break;

  case 91: /* customBlobArgs: %empty  */
#line 348 "asmparse.y"
                                                                                { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt16(VAL16(0x0001)); }
#line 4358 "prebuilt\\asmparse.cpp"
    break;

  case 92: /* customBlobArgs: customBlobArgs serInit  */
#line 349 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr);
                                                                                  AppendFieldToCustomBlob((yyval.binstr),(yyvsp[0].binstr)); }
#line 4365 "prebuilt\\asmparse.cpp"
    break;

  case 93: /* customBlobArgs: customBlobArgs compControl  */
#line 351 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 4371 "prebuilt\\asmparse.cpp"
    break;

  case 94: /* customBlobNVPairs: %empty  */
#line 354 "asmparse.y"
                                                                                { (yyval.binstr) = new BinStr(); }
#line 4377 "prebuilt\\asmparse.cpp"
    break;

  case 95: /* customBlobNVPairs: customBlobNVPairs fieldOrProp serializType dottedName '=' serInit  */
#line 356 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-5].binstr); (yyval.binstr)->appendInt8((yyvsp[-4].int32));
                                                                                  (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                                  AppendStringWithLength((yyval.binstr),(yyvsp[-2].string));
                                                                                  AppendFieldToCustomBlob((yyval.binstr),(yyvsp[0].binstr));
                                                                                  nCustomBlobNVPairs++; }
#line 4387 "prebuilt\\asmparse.cpp"
    break;

  case 96: /* customBlobNVPairs: customBlobNVPairs compControl  */
#line 361 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 4393 "prebuilt\\asmparse.cpp"
    break;

  case 97: /* fieldOrProp: FIELD_  */
#line 364 "asmparse.y"
                                                                                { (yyval.int32) = SERIALIZATION_TYPE_FIELD; }
#line 4399 "prebuilt\\asmparse.cpp"
    break;

  case 98: /* fieldOrProp: PROPERTY_  */
#line 365 "asmparse.y"
                                                                                { (yyval.int32) = SERIALIZATION_TYPE_PROPERTY; }
#line 4405 "prebuilt\\asmparse.cpp"
    break;

  case 99: /* customAttrDecl: customDescr  */
#line 368 "asmparse.y"
                                                                                { if((yyvsp[0].cad)->tkOwner && !(yyvsp[0].cad)->tkInterfacePair)
                                                                                    PASM->DefineCV((yyvsp[0].cad));
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH((yyvsp[0].cad)); }
#line 4414 "prebuilt\\asmparse.cpp"
    break;

  case 100: /* customAttrDecl: customDescrWithOwner  */
#line 372 "asmparse.y"
                                                                                { PASM->DefineCV((yyvsp[0].cad)); }
#line 4420 "prebuilt\\asmparse.cpp"
    break;

  case 101: /* customAttrDecl: TYPEDEF_CA  */
#line 373 "asmparse.y"
                                                                                { CustomDescr* pNew = new CustomDescr((yyvsp[0].tdd)->m_pCA);
                                                                                  if(pNew->tkOwner == 0) pNew->tkOwner = PASM->m_tkCurrentCVOwner;
                                                                                  if(pNew->tkOwner)
                                                                                    PASM->DefineCV(pNew);
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH(pNew); }
#line 4431 "prebuilt\\asmparse.cpp"
    break;

  case 102: /* serializType: simpleType  */
#line 381 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4437 "prebuilt\\asmparse.cpp"
    break;

  case 103: /* serializType: TYPE_  */
#line 382 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE); }
#line 4443 "prebuilt\\asmparse.cpp"
    break;

  case 104: /* serializType: OBJECT_  */
#line 383 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TAGGED_OBJECT); }
#line 4449 "prebuilt\\asmparse.cpp"
    break;

  case 105: /* serializType: ENUM_ CLASS_ SQSTRING  */
#line 384 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); }
#line 4456 "prebuilt\\asmparse.cpp"
    break;

  case 106: /* serializType: ENUM_ className  */
#line 386 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[0].token))); }
#line 4463 "prebuilt\\asmparse.cpp"
    break;

  case 107: /* serializType: serializType '[' ']'  */
#line 388 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 4469 "prebuilt\\asmparse.cpp"
    break;

  case 108: /* moduleHead: _MODULE  */
#line 393 "asmparse.y"
                                                                                { PASMM->SetModuleName(NULL); PASM->m_tkCurrentCVOwner=1; }
#line 4475 "prebuilt\\asmparse.cpp"
    break;

  case 109: /* moduleHead: _MODULE dottedName  */
#line 394 "asmparse.y"
                                                                                { PASMM->SetModuleName((yyvsp[0].string)); PASM->m_tkCurrentCVOwner=1; }
#line 4481 "prebuilt\\asmparse.cpp"
    break;

  case 110: /* moduleHead: _MODULE EXTERN_ dottedName  */
#line 395 "asmparse.y"
                                                                                { BinStr* pbs = new BinStr();
                                                                                  unsigned L = (unsigned)strlen((yyvsp[0].string));
                                                                                  memcpy((char*)(pbs->getBuff(L)),(yyvsp[0].string),L);
                                                                                  PASM->EmitImport(pbs); delete pbs;}
#line 4490 "prebuilt\\asmparse.cpp"
    break;

  case 111: /* vtfixupDecl: _VTFIXUP '[' int32 ']' vtfixupAttr AT_ id  */
#line 402 "asmparse.y"
                                                                                { /*PASM->SetDataSection(); PASM->EmitDataLabel($7);*/
                                                                                  PASM->m_VTFList.PUSH(new VTFEntry((USHORT)(yyvsp[-4].int32), (USHORT)(yyvsp[-2].int32), (yyvsp[0].string))); }
#line 4497 "prebuilt\\asmparse.cpp"
    break;

  case 112: /* vtfixupAttr: %empty  */
#line 406 "asmparse.y"
                                                                                { (yyval.int32) = 0; }
#line 4503 "prebuilt\\asmparse.cpp"
    break;

  case 113: /* vtfixupAttr: vtfixupAttr INT32_  */
#line 407 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_32BIT; }
#line 4509 "prebuilt\\asmparse.cpp"
    break;

  case 114: /* vtfixupAttr: vtfixupAttr INT64_  */
#line 408 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_64BIT; }
#line 4515 "prebuilt\\asmparse.cpp"
    break;

  case 115: /* vtfixupAttr: vtfixupAttr FROMUNMANAGED_  */
#line 409 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_FROM_UNMANAGED; }
#line 4521 "prebuilt\\asmparse.cpp"
    break;

  case 116: /* vtfixupAttr: vtfixupAttr CALLMOSTDERIVED_  */
#line 410 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_CALL_MOST_DERIVED; }
#line 4527 "prebuilt\\asmparse.cpp"
    break;

  case 117: /* vtfixupAttr: vtfixupAttr RETAINAPPDOMAIN_  */
#line 411 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN; }
#line 4533 "prebuilt\\asmparse.cpp"
    break;

  case 118: /* vtableDecl: vtableHead bytes ')'  */
#line 414 "asmparse.y"
                                                                                { PASM->m_pVTable = (yyvsp[-1].binstr); }
#line 4539 "prebuilt\\asmparse.cpp"
    break;

  case 119: /* vtableHead: _VTABLE '=' '('  */
#line 417 "asmparse.y"
                                                                                { bParsingByteArray = TRUE; }
#line 4545 "prebuilt\\asmparse.cpp"
    break;

  case 120: /* nameSpaceHead: _NAMESPACE dottedName  */
#line 421 "asmparse.y"
                                                                                { PASM->StartNameSpace((yyvsp[0].string)); }
#line 4551 "prebuilt\\asmparse.cpp"
    break;

  case 121: /* _class: _CLASS  */
#line 424 "asmparse.y"
                                                                                { newclass = TRUE; }
#line 4557 "prebuilt\\asmparse.cpp"
    break;

  case 122: /* classHeadBegin: _class classAttr dottedName typarsClause  */
#line 427 "asmparse.y"
                                                                                { if((yyvsp[0].typarlist)) FixupConstraints();
                                                                                  PASM->StartClass((yyvsp[-1].string), (yyvsp[-2].classAttr), (yyvsp[0].typarlist));
                                                                                  TyParFixupList.RESET(false);
                                                                                  newclass = FALSE;
                                                                                }
#line 4567 "prebuilt\\asmparse.cpp"
    break;

  case 123: /* classHead: classHeadBegin extendsClause implClause  */
#line 433 "asmparse.y"
                                                                                { PASM->AddClass(); }
#line 4573 "prebuilt\\asmparse.cpp"
    break;

  case 124: /* classAttr: %empty  */
#line 436 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) 0; }
#line 4579 "prebuilt\\asmparse.cpp"
    break;

  case 125: /* classAttr: classAttr PUBLIC_  */
#line 437 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdVisibilityMask) | tdPublic); }
#line 4585 "prebuilt\\asmparse.cpp"
    break;

  case 126: /* classAttr: classAttr PRIVATE_  */
#line 438 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdVisibilityMask) | tdNotPublic); }
#line 4591 "prebuilt\\asmparse.cpp"
    break;

  case 127: /* classAttr: classAttr VALUE_  */
#line 439 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | 0x80000000 | tdSealed); }
#line 4597 "prebuilt\\asmparse.cpp"
    break;

  case 128: /* classAttr: classAttr ENUM_  */
#line 440 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | 0x40000000); }
#line 4603 "prebuilt\\asmparse.cpp"
    break;

  case 129: /* classAttr: classAttr INTERFACE_  */
#line 441 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdInterface | tdAbstract); }
#line 4609 "prebuilt\\asmparse.cpp"
    break;

  case 130: /* classAttr: classAttr SEALED_  */
#line 442 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSealed); }
#line 4615 "prebuilt\\asmparse.cpp"
    break;

  case 131: /* classAttr: classAttr ABSTRACT_  */
#line 443 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdAbstract); }
#line 4621 "prebuilt\\asmparse.cpp"
    break;

  case 132: /* classAttr: classAttr AUTO_  */
#line 444 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdAutoLayout); }
#line 4627 "prebuilt\\asmparse.cpp"
    break;

  case 133: /* classAttr: classAttr SEQUENTIAL_  */
#line 445 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdSequentialLayout); }
#line 4633 "prebuilt\\asmparse.cpp"
    break;

  case 134: /* classAttr: classAttr EXPLICIT_  */
#line 446 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdExplicitLayout); }
#line 4639 "prebuilt\\asmparse.cpp"
    break;

  case 135: /* classAttr: classAttr ANSI_  */
#line 447 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdAnsiClass); }
#line 4645 "prebuilt\\asmparse.cpp"
    break;

  case 136: /* classAttr: classAttr UNICODE_  */
#line 448 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdUnicodeClass); }
#line 4651 "prebuilt\\asmparse.cpp"
    break;

  case 137: /* classAttr: classAttr AUTOCHAR_  */
#line 449 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdAutoClass); }
#line 4657 "prebuilt\\asmparse.cpp"
    break;

  case 138: /* classAttr: classAttr IMPORT_  */
#line 450 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdImport); }
#line 4663 "prebuilt\\asmparse.cpp"
    break;

  case 139: /* classAttr: classAttr SERIALIZABLE_  */
#line 451 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSerializable); }
#line 4669 "prebuilt\\asmparse.cpp"
    break;

  case 140: /* classAttr: classAttr WINDOWSRUNTIME_  */
#line 452 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdWindowsRuntime); }
#line 4675 "prebuilt\\asmparse.cpp"
    break;

  case 141: /* classAttr: classAttr NESTED_ PUBLIC_  */
#line 453 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedPublic); }
#line 4681 "prebuilt\\asmparse.cpp"
    break;

  case 142: /* classAttr: classAttr NESTED_ PRIVATE_  */
#line 454 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedPrivate); }
#line 4687 "prebuilt\\asmparse.cpp"
    break;

  case 143: /* classAttr: classAttr NESTED_ FAMILY_  */
#line 455 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamily); }
#line 4693 "prebuilt\\asmparse.cpp"
    break;

  case 144: /* classAttr: classAttr NESTED_ ASSEMBLY_  */
#line 456 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedAssembly); }
#line 4699 "prebuilt\\asmparse.cpp"
    break;

  case 145: /* classAttr: classAttr NESTED_ FAMANDASSEM_  */
#line 457 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamANDAssem); }
#line 4705 "prebuilt\\asmparse.cpp"
    break;

  case 146: /* classAttr: classAttr NESTED_ FAMORASSEM_  */
#line 458 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamORAssem); }
#line 4711 "prebuilt\\asmparse.cpp"
    break;

  case 147: /* classAttr: classAttr BEFOREFIELDINIT_  */
#line 459 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdBeforeFieldInit); }
#line 4717 "prebuilt\\asmparse.cpp"
    break;

  case 148: /* classAttr: classAttr SPECIALNAME_  */
#line 460 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSpecialName); }
#line 4723 "prebuilt\\asmparse.cpp"
    break;

  case 149: /* classAttr: classAttr RTSPECIALNAME_  */
#line 461 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr)); }
#line 4729 "prebuilt\\asmparse.cpp"
    break;

  case 150: /* classAttr: classAttr FLAGS_ '(' int32 ')'  */
#line 462 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].int32)); }
#line 4735 "prebuilt\\asmparse.cpp"
    break;

  case 152: /* extendsClause: EXTENDS_ typeSpec  */
#line 466 "asmparse.y"
                                                                            { PASM->m_crExtends = (yyvsp[0].token); }
#line 4741 "prebuilt\\asmparse.cpp"
    break;

  case 157: /* implList: implList ',' typeSpec  */
#line 477 "asmparse.y"
                                                            { PASM->AddToImplList((yyvsp[0].token)); }
#line 4747 "prebuilt\\asmparse.cpp"
    break;

  case 158: /* implList: typeSpec  */
#line 478 "asmparse.y"
                                                            { PASM->AddToImplList((yyvsp[0].token)); }
#line 4753 "prebuilt\\asmparse.cpp"
    break;

  case 159: /* typeList: %empty  */
#line 482 "asmparse.y"
                                                            { (yyval.binstr) = new BinStr(); }
#line 4759 "prebuilt\\asmparse.cpp"
    break;

  case 160: /* typeList: typeListNotEmpty  */
#line 483 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4765 "prebuilt\\asmparse.cpp"
    break;

  case 161: /* typeListNotEmpty: typeSpec  */
#line 486 "asmparse.y"
                                                            { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[0].token)); }
#line 4771 "prebuilt\\asmparse.cpp"
    break;

  case 162: /* typeListNotEmpty: typeListNotEmpty ',' typeSpec  */
#line 487 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->appendInt32((yyvsp[0].token)); }
#line 4777 "prebuilt\\asmparse.cpp"
    break;

  case 163: /* typarsClause: %empty  */
#line 490 "asmparse.y"
                                                            { (yyval.typarlist) = NULL; PASM->m_TyParList = NULL;}
#line 4783 "prebuilt\\asmparse.cpp"
    break;

  case 164: /* typarsClause: '<' typars '>'  */
#line 491 "asmparse.y"
                                                            { (yyval.typarlist) = (yyvsp[-1].typarlist);   PASM->m_TyParList = (yyvsp[-1].typarlist);}
#line 4789 "prebuilt\\asmparse.cpp"
    break;

  case 165: /* typarAttrib: '+'  */
#line 494 "asmparse.y"
                                                            { (yyval.int32) = gpCovariant; }
#line 4795 "prebuilt\\asmparse.cpp"
    break;

  case 166: /* typarAttrib: '-'  */
#line 495 "asmparse.y"
                                                            { (yyval.int32) = gpContravariant; }
#line 4801 "prebuilt\\asmparse.cpp"
    break;

  case 167: /* typarAttrib: CLASS_  */
#line 496 "asmparse.y"
                                                            { (yyval.int32) = gpReferenceTypeConstraint; }
#line 4807 "prebuilt\\asmparse.cpp"
    break;

  case 168: /* typarAttrib: VALUETYPE_  */
#line 497 "asmparse.y"
                                                            { (yyval.int32) = gpNotNullableValueTypeConstraint; }
#line 4813 "prebuilt\\asmparse.cpp"
    break;

  case 169: /* typarAttrib: BYREFLIKE_  */
#line 498 "asmparse.y"
                                                            { (yyval.int32) = gpAllowByRefLike; }
#line 4819 "prebuilt\\asmparse.cpp"
    break;

  case 170: /* typarAttrib: _CTOR  */
#line 499 "asmparse.y"
                                                            { (yyval.int32) = gpDefaultConstructorConstraint; }
#line 4825 "prebuilt\\asmparse.cpp"
    break;

  case 171: /* typarAttrib: FLAGS_ '(' int32 ')'  */
#line 500 "asmparse.y"
                                                            { (yyval.int32) = (CorGenericParamAttr)(yyvsp[-1].int32); }
#line 4831 "prebuilt\\asmparse.cpp"
    break;

  case 172: /* typarAttribs: %empty  */
#line 503 "asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 4837 "prebuilt\\asmparse.cpp"
    break;

  case 173: /* typarAttribs: typarAttrib typarAttribs  */
#line 504 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) | (yyvsp[0].int32); }
#line 4843 "prebuilt\\asmparse.cpp"
    break;

  case 174: /* typars: typarAttribs tyBound dottedName typarsRest  */
#line 507 "asmparse.y"
                                                                     {(yyval.typarlist) = new TyParList((yyvsp[-3].int32), (yyvsp[-2].binstr), (yyvsp[-1].string), (yyvsp[0].typarlist));}
#line 4849 "prebuilt\\asmparse.cpp"
    break;

  case 175: /* typars: typarAttribs dottedName typarsRest  */
#line 508 "asmparse.y"
                                                               {(yyval.typarlist) = new TyParList((yyvsp[-2].int32), NULL, (yyvsp[-1].string), (yyvsp[0].typarlist));}
#line 4855 "prebuilt\\asmparse.cpp"
    break;

  case 176: /* typarsRest: %empty  */
#line 511 "asmparse.y"
                                                            { (yyval.typarlist) = NULL; }
#line 4861 "prebuilt\\asmparse.cpp"
    break;

  case 177: /* typarsRest: ',' typars  */
#line 512 "asmparse.y"
                                                            { (yyval.typarlist) = (yyvsp[0].typarlist); }
#line 4867 "prebuilt\\asmparse.cpp"
    break;

  case 178: /* tyBound: '(' typeList ')'  */
#line 515 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 4873 "prebuilt\\asmparse.cpp"
    break;

  case 179: /* genArity: %empty  */
#line 518 "asmparse.y"
                                                            { (yyval.int32)= 0; }
#line 4879 "prebuilt\\asmparse.cpp"
    break;

  case 180: /* genArity: genArityNotEmpty  */
#line 519 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[0].int32); }
#line 4885 "prebuilt\\asmparse.cpp"
    break;

  case 181: /* genArityNotEmpty: '<' '[' int32 ']' '>'  */
#line 522 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-2].int32); }
#line 4891 "prebuilt\\asmparse.cpp"
    break;

  case 182: /* classDecl: methodHead methodDecls '}'  */
#line 526 "asmparse.y"
                                                            { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                              {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                 PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                              PASM->EndMethod(); }
#line 4900 "prebuilt\\asmparse.cpp"
    break;

  case 183: /* classDecl: classHead '{' classDecls '}'  */
#line 530 "asmparse.y"
                                                            { PASM->EndClass(); }
#line 4906 "prebuilt\\asmparse.cpp"
    break;

  case 184: /* classDecl: eventHead '{' eventDecls '}'  */
#line 531 "asmparse.y"
                                                            { PASM->EndEvent(); }
#line 4912 "prebuilt\\asmparse.cpp"
    break;

  case 185: /* classDecl: propHead '{' propDecls '}'  */
#line 532 "asmparse.y"
                                                            { PASM->EndProp(); }
#line 4918 "prebuilt\\asmparse.cpp"
    break;

  case 191: /* classDecl: _SIZE int32  */
#line 538 "asmparse.y"
                                                                { PASM->m_pCurClass->m_ulSize = (yyvsp[0].int32); }
#line 4924 "prebuilt\\asmparse.cpp"
    break;

  case 192: /* classDecl: _PACK int32  */
#line 539 "asmparse.y"
                                                                { PASM->m_pCurClass->m_ulPack = (yyvsp[0].int32); }
#line 4930 "prebuilt\\asmparse.cpp"
    break;

  case 193: /* classDecl: exportHead '{' exptypeDecls '}'  */
#line 540 "asmparse.y"
                                                                { PASMM->EndComType(); }
#line 4936 "prebuilt\\asmparse.cpp"
    break;

  case 194: /* classDecl: _OVERRIDE typeSpec DCOLON methodName WITH_ callConv type typeSpec DCOLON methodName '(' sigArgs0 ')'  */
#line 542 "asmparse.y"
                                                                { BinStr *sig1 = parser->MakeSig((yyvsp[-7].int32), (yyvsp[-6].binstr), (yyvsp[-1].binstr));
                                                                  BinStr *sig2 = new BinStr(); sig2->append(sig1);
                                                                  PASM->AddMethodImpl((yyvsp[-11].token),(yyvsp[-9].string),sig1,(yyvsp[-5].token),(yyvsp[-3].string),sig2);
                                                                  PASM->ResetArgNameList();
                                                                }
#line 4946 "prebuilt\\asmparse.cpp"
    break;

  case 195: /* classDecl: _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')' WITH_ METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'  */
#line 548 "asmparse.y"
                                                                 { PASM->AddMethodImpl((yyvsp[-17].token),(yyvsp[-15].string),
                                                                      ((yyvsp[-14].int32)==0 ? parser->MakeSig((yyvsp[-19].int32),(yyvsp[-18].binstr),(yyvsp[-12].binstr)) :
                                                                      parser->MakeSig((yyvsp[-19].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-18].binstr),(yyvsp[-12].binstr),(yyvsp[-14].int32))),
                                                                      (yyvsp[-6].token),(yyvsp[-4].string),
                                                                      ((yyvsp[-3].int32)==0 ? parser->MakeSig((yyvsp[-8].int32),(yyvsp[-7].binstr),(yyvsp[-1].binstr)) :
                                                                      parser->MakeSig((yyvsp[-8].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-7].binstr),(yyvsp[-1].binstr),(yyvsp[-3].int32))));
                                                                   PASM->ResetArgNameList();
                                                                 }
#line 4959 "prebuilt\\asmparse.cpp"
    break;

  case 198: /* classDecl: _PARAM TYPE_ '[' int32 ']'  */
#line 558 "asmparse.y"
                                                            { if(((yyvsp[-1].int32) > 0) && ((yyvsp[-1].int32) <= (int)PASM->m_pCurClass->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[(yyvsp[-1].int32)-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
#line 4969 "prebuilt\\asmparse.cpp"
    break;

  case 199: /* classDecl: _PARAM TYPE_ dottedName  */
#line 563 "asmparse.y"
                                                            { int n = PASM->m_pCurClass->FindTyPar((yyvsp[0].string));
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",(yyvsp[0].string));
                                                            }
#line 4980 "prebuilt\\asmparse.cpp"
    break;

  case 200: /* classDecl: _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec  */
#line 569 "asmparse.y"
                                                                        { PASM->AddGenericParamConstraint((yyvsp[-3].int32), 0, (yyvsp[0].token)); }
#line 4986 "prebuilt\\asmparse.cpp"
    break;

  case 201: /* classDecl: _PARAM CONSTRAINT_ dottedName ',' typeSpec  */
#line 570 "asmparse.y"
                                                                        { PASM->AddGenericParamConstraint(0, (yyvsp[-2].string), (yyvsp[0].token)); }
#line 4992 "prebuilt\\asmparse.cpp"
    break;

  case 202: /* classDecl: _INTERFACEIMPL TYPE_ typeSpec customDescr  */
#line 571 "asmparse.y"
                                                                      { (yyvsp[0].cad)->tkInterfacePair = (yyvsp[-1].token);
                                                                        if(PASM->m_pCustomDescrList)
                                                                            PASM->m_pCustomDescrList->PUSH((yyvsp[0].cad));
                                                                      }
#line 5001 "prebuilt\\asmparse.cpp"
    break;

  case 203: /* fieldDecl: _FIELD repeatOpt fieldAttr type dottedName atOpt initOpt  */
#line 579 "asmparse.y"
                                                            { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                              PASM->AddField((yyvsp[-2].string), (yyvsp[-3].binstr), (yyvsp[-4].fieldAttr), (yyvsp[-1].string), (yyvsp[0].binstr), (yyvsp[-5].int32)); }
#line 5008 "prebuilt\\asmparse.cpp"
    break;

  case 204: /* fieldAttr: %empty  */
#line 583 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) 0; }
#line 5014 "prebuilt\\asmparse.cpp"
    break;

  case 205: /* fieldAttr: fieldAttr STATIC_  */
#line 584 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdStatic); }
#line 5020 "prebuilt\\asmparse.cpp"
    break;

  case 206: /* fieldAttr: fieldAttr PUBLIC_  */
#line 585 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPublic); }
#line 5026 "prebuilt\\asmparse.cpp"
    break;

  case 207: /* fieldAttr: fieldAttr PRIVATE_  */
#line 586 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPrivate); }
#line 5032 "prebuilt\\asmparse.cpp"
    break;

  case 208: /* fieldAttr: fieldAttr FAMILY_  */
#line 587 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamily); }
#line 5038 "prebuilt\\asmparse.cpp"
    break;

  case 209: /* fieldAttr: fieldAttr INITONLY_  */
#line 588 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdInitOnly); }
#line 5044 "prebuilt\\asmparse.cpp"
    break;

  case 210: /* fieldAttr: fieldAttr RTSPECIALNAME_  */
#line 589 "asmparse.y"
                                                            { (yyval.fieldAttr) = (yyvsp[-1].fieldAttr); }
#line 5050 "prebuilt\\asmparse.cpp"
    break;

  case 211: /* fieldAttr: fieldAttr SPECIALNAME_  */
#line 590 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdSpecialName); }
#line 5056 "prebuilt\\asmparse.cpp"
    break;

  case 212: /* fieldAttr: fieldAttr MARSHAL_ '(' marshalBlob ')'  */
#line 603 "asmparse.y"
                                                            { PASM->m_pMarshal = (yyvsp[-1].binstr); }
#line 5062 "prebuilt\\asmparse.cpp"
    break;

  case 213: /* fieldAttr: fieldAttr ASSEMBLY_  */
#line 604 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdAssembly); }
#line 5068 "prebuilt\\asmparse.cpp"
    break;

  case 214: /* fieldAttr: fieldAttr FAMANDASSEM_  */
#line 605 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamANDAssem); }
#line 5074 "prebuilt\\asmparse.cpp"
    break;

  case 215: /* fieldAttr: fieldAttr FAMORASSEM_  */
#line 606 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamORAssem); }
#line 5080 "prebuilt\\asmparse.cpp"
    break;

  case 216: /* fieldAttr: fieldAttr PRIVATESCOPE_  */
#line 607 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPrivateScope); }
#line 5086 "prebuilt\\asmparse.cpp"
    break;

  case 217: /* fieldAttr: fieldAttr LITERAL_  */
#line 608 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdLiteral); }
#line 5092 "prebuilt\\asmparse.cpp"
    break;

  case 218: /* fieldAttr: fieldAttr NOTSERIALIZED_  */
#line 609 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdNotSerialized); }
#line 5098 "prebuilt\\asmparse.cpp"
    break;

  case 219: /* fieldAttr: fieldAttr FLAGS_ '(' int32 ')'  */
#line 610 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].int32)); }
#line 5104 "prebuilt\\asmparse.cpp"
    break;

  case 220: /* atOpt: %empty  */
#line 613 "asmparse.y"
                                                            { (yyval.string) = 0; }
#line 5110 "prebuilt\\asmparse.cpp"
    break;

  case 221: /* atOpt: AT_ id  */
#line 614 "asmparse.y"
                                                            { (yyval.string) = (yyvsp[0].string); }
#line 5116 "prebuilt\\asmparse.cpp"
    break;

  case 222: /* initOpt: %empty  */
#line 617 "asmparse.y"
                                                            { (yyval.binstr) = NULL; }
#line 5122 "prebuilt\\asmparse.cpp"
    break;

  case 223: /* initOpt: '=' fieldInit  */
#line 618 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 5128 "prebuilt\\asmparse.cpp"
    break;

  case 224: /* repeatOpt: %empty  */
#line 621 "asmparse.y"
                                                            { (yyval.int32) = 0xFFFFFFFF; }
#line 5134 "prebuilt\\asmparse.cpp"
    break;

  case 225: /* repeatOpt: '[' int32 ']'  */
#line 622 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32); }
#line 5140 "prebuilt\\asmparse.cpp"
    break;

  case 226: /* methodRef: callConv type typeSpec DCOLON methodName tyArgs0 '(' sigArgs0 ')'  */
#line 627 "asmparse.y"
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
#line 5161 "prebuilt\\asmparse.cpp"
    break;

  case 227: /* methodRef: callConv type typeSpec DCOLON methodName genArityNotEmpty '(' sigArgs0 ')'  */
#line 644 "asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(((yyvsp[-8].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               (yyval.token) = PASM->MakeMemberRef((yyvsp[-6].token), (yyvsp[-4].string),
                                                                 parser->MakeSig((yyvsp[-8].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-7].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32)));
                                                             }
#line 5171 "prebuilt\\asmparse.cpp"
    break;

  case 228: /* methodRef: callConv type methodName tyArgs0 '(' sigArgs0 ')'  */
#line 650 "asmparse.y"
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
#line 5191 "prebuilt\\asmparse.cpp"
    break;

  case 229: /* methodRef: callConv type methodName genArityNotEmpty '(' sigArgs0 ')'  */
#line 666 "asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(((yyvsp[-6].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               (yyval.token) = PASM->MakeMemberRef(mdTokenNil, (yyvsp[-4].string), parser->MakeSig((yyvsp[-6].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-5].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32)));
                                                             }
#line 5200 "prebuilt\\asmparse.cpp"
    break;

  case 230: /* methodRef: mdtoken  */
#line 670 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token); }
#line 5206 "prebuilt\\asmparse.cpp"
    break;

  case 231: /* methodRef: TYPEDEF_M  */
#line 671 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 5212 "prebuilt\\asmparse.cpp"
    break;

  case 232: /* methodRef: TYPEDEF_MR  */
#line 672 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 5218 "prebuilt\\asmparse.cpp"
    break;

  case 233: /* callConv: INSTANCE_ callConv  */
#line 675 "asmparse.y"
                                                              { (yyval.int32) = ((yyvsp[0].int32) | IMAGE_CEE_CS_CALLCONV_HASTHIS); }
#line 5224 "prebuilt\\asmparse.cpp"
    break;

  case 234: /* callConv: EXPLICIT_ callConv  */
#line 676 "asmparse.y"
                                                              { (yyval.int32) = ((yyvsp[0].int32) | IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS); }
#line 5230 "prebuilt\\asmparse.cpp"
    break;

  case 235: /* callConv: callKind  */
#line 677 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 5236 "prebuilt\\asmparse.cpp"
    break;

  case 236: /* callConv: CALLCONV_ '(' int32 ')'  */
#line 678 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32); }
#line 5242 "prebuilt\\asmparse.cpp"
    break;

  case 237: /* callKind: %empty  */
#line 681 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
#line 5248 "prebuilt\\asmparse.cpp"
    break;

  case 238: /* callKind: DEFAULT_  */
#line 682 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
#line 5254 "prebuilt\\asmparse.cpp"
    break;

  case 239: /* callKind: VARARG_  */
#line 683 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_VARARG; }
#line 5260 "prebuilt\\asmparse.cpp"
    break;

  case 240: /* callKind: UNMANAGED_ CDECL_  */
#line 684 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_C; }
#line 5266 "prebuilt\\asmparse.cpp"
    break;

  case 241: /* callKind: UNMANAGED_ STDCALL_  */
#line 685 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_STDCALL; }
#line 5272 "prebuilt\\asmparse.cpp"
    break;

  case 242: /* callKind: UNMANAGED_ THISCALL_  */
#line 686 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_THISCALL; }
#line 5278 "prebuilt\\asmparse.cpp"
    break;

  case 243: /* callKind: UNMANAGED_ FASTCALL_  */
#line 687 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_FASTCALL; }
#line 5284 "prebuilt\\asmparse.cpp"
    break;

  case 244: /* callKind: UNMANAGED_  */
#line 688 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_UNMANAGED; }
#line 5290 "prebuilt\\asmparse.cpp"
    break;

  case 245: /* mdtoken: MDTOKEN_ '(' int32 ')'  */
#line 691 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[-1].int32); }
#line 5296 "prebuilt\\asmparse.cpp"
    break;

  case 246: /* memberRef: methodSpec methodRef  */
#line 694 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token);
                                                               PASM->delArgNameList(PASM->m_firstArgName);
                                                               PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                               PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                               PASM->SetMemberRefFixup((yyvsp[0].token),iOpcodeLen); }
#line 5306 "prebuilt\\asmparse.cpp"
    break;

  case 247: /* memberRef: FIELD_ type typeSpec DCOLON dottedName  */
#line 700 "asmparse.y"
                                                             { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               (yyval.token) = PASM->MakeMemberRef((yyvsp[-2].token), (yyvsp[0].string), (yyvsp[-3].binstr));
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 5314 "prebuilt\\asmparse.cpp"
    break;

  case 248: /* memberRef: FIELD_ type dottedName  */
#line 704 "asmparse.y"
                                                             { (yyvsp[-1].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               (yyval.token) = PASM->MakeMemberRef(mdTokenNil, (yyvsp[0].string), (yyvsp[-1].binstr));
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 5322 "prebuilt\\asmparse.cpp"
    break;

  case 249: /* memberRef: FIELD_ TYPEDEF_F  */
#line 707 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 5329 "prebuilt\\asmparse.cpp"
    break;

  case 250: /* memberRef: FIELD_ TYPEDEF_MR  */
#line 709 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 5336 "prebuilt\\asmparse.cpp"
    break;

  case 251: /* memberRef: mdtoken  */
#line 711 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token);
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 5343 "prebuilt\\asmparse.cpp"
    break;

  case 252: /* eventHead: _EVENT eventAttr typeSpec dottedName  */
#line 716 "asmparse.y"
                                                                 { PASM->ResetEvent((yyvsp[0].string), (yyvsp[-1].token), (yyvsp[-2].eventAttr)); }
#line 5349 "prebuilt\\asmparse.cpp"
    break;

  case 253: /* eventHead: _EVENT eventAttr dottedName  */
#line 717 "asmparse.y"
                                                                 { PASM->ResetEvent((yyvsp[0].string), mdTypeRefNil, (yyvsp[-1].eventAttr)); }
#line 5355 "prebuilt\\asmparse.cpp"
    break;

  case 254: /* eventAttr: %empty  */
#line 721 "asmparse.y"
                                                            { (yyval.eventAttr) = (CorEventAttr) 0; }
#line 5361 "prebuilt\\asmparse.cpp"
    break;

  case 255: /* eventAttr: eventAttr RTSPECIALNAME_  */
#line 722 "asmparse.y"
                                                            { (yyval.eventAttr) = (yyvsp[-1].eventAttr); }
#line 5367 "prebuilt\\asmparse.cpp"
    break;

  case 256: /* eventAttr: eventAttr SPECIALNAME_  */
#line 723 "asmparse.y"
                                                            { (yyval.eventAttr) = (CorEventAttr) ((yyvsp[-1].eventAttr) | evSpecialName); }
#line 5373 "prebuilt\\asmparse.cpp"
    break;

  case 259: /* eventDecl: _ADDON methodRef  */
#line 730 "asmparse.y"
                                                           { PASM->SetEventMethod(0, (yyvsp[0].token)); }
#line 5379 "prebuilt\\asmparse.cpp"
    break;

  case 260: /* eventDecl: _REMOVEON methodRef  */
#line 731 "asmparse.y"
                                                           { PASM->SetEventMethod(1, (yyvsp[0].token)); }
#line 5385 "prebuilt\\asmparse.cpp"
    break;

  case 261: /* eventDecl: _FIRE methodRef  */
#line 732 "asmparse.y"
                                                           { PASM->SetEventMethod(2, (yyvsp[0].token)); }
#line 5391 "prebuilt\\asmparse.cpp"
    break;

  case 262: /* eventDecl: _OTHER methodRef  */
#line 733 "asmparse.y"
                                                           { PASM->SetEventMethod(3, (yyvsp[0].token)); }
#line 5397 "prebuilt\\asmparse.cpp"
    break;

  case 267: /* propHead: _PROPERTY propAttr callConv type dottedName '(' sigArgs0 ')' initOpt  */
#line 742 "asmparse.y"
                                                            { PASM->ResetProp((yyvsp[-4].string),
                                                              parser->MakeSig((IMAGE_CEE_CS_CALLCONV_PROPERTY |
                                                              ((yyvsp[-6].int32) & IMAGE_CEE_CS_CALLCONV_HASTHIS)),(yyvsp[-5].binstr),(yyvsp[-2].binstr)), (yyvsp[-7].propAttr), (yyvsp[0].binstr));}
#line 5405 "prebuilt\\asmparse.cpp"
    break;

  case 268: /* propAttr: %empty  */
#line 747 "asmparse.y"
                                                            { (yyval.propAttr) = (CorPropertyAttr) 0; }
#line 5411 "prebuilt\\asmparse.cpp"
    break;

  case 269: /* propAttr: propAttr RTSPECIALNAME_  */
#line 748 "asmparse.y"
                                                            { (yyval.propAttr) = (yyvsp[-1].propAttr); }
#line 5417 "prebuilt\\asmparse.cpp"
    break;

  case 270: /* propAttr: propAttr SPECIALNAME_  */
#line 749 "asmparse.y"
                                                            { (yyval.propAttr) = (CorPropertyAttr) ((yyvsp[-1].propAttr) | prSpecialName); }
#line 5423 "prebuilt\\asmparse.cpp"
    break;

  case 273: /* propDecl: _SET methodRef  */
#line 757 "asmparse.y"
                                                            { PASM->SetPropMethod(0, (yyvsp[0].token)); }
#line 5429 "prebuilt\\asmparse.cpp"
    break;

  case 274: /* propDecl: _GET methodRef  */
#line 758 "asmparse.y"
                                                            { PASM->SetPropMethod(1, (yyvsp[0].token)); }
#line 5435 "prebuilt\\asmparse.cpp"
    break;

  case 275: /* propDecl: _OTHER methodRef  */
#line 759 "asmparse.y"
                                                            { PASM->SetPropMethod(2, (yyvsp[0].token)); }
#line 5441 "prebuilt\\asmparse.cpp"
    break;

  case 280: /* methodHeadPart1: _METHOD  */
#line 767 "asmparse.y"
                                                            { PASM->ResetForNextMethod();
                                                              uMethodBeginLine = PASM->m_ulCurLine;
                                                              uMethodBeginColumn=PASM->m_ulCurColumn;
                                                            }
#line 5450 "prebuilt\\asmparse.cpp"
    break;

  case 281: /* marshalClause: %empty  */
#line 773 "asmparse.y"
                                                            { (yyval.binstr) = NULL; }
#line 5456 "prebuilt\\asmparse.cpp"
    break;

  case 282: /* marshalClause: MARSHAL_ '(' marshalBlob ')'  */
#line 774 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 5462 "prebuilt\\asmparse.cpp"
    break;

  case 283: /* marshalBlob: nativeType  */
#line 777 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 5468 "prebuilt\\asmparse.cpp"
    break;

  case 284: /* marshalBlob: marshalBlobHead hexbytes '}'  */
#line 778 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 5474 "prebuilt\\asmparse.cpp"
    break;

  case 285: /* marshalBlobHead: '{'  */
#line 781 "asmparse.y"
                                                             { bParsingByteArray = TRUE; }
#line 5480 "prebuilt\\asmparse.cpp"
    break;

  case 286: /* methodHead: methodHeadPart1 methAttr callConv paramAttr type marshalClause methodName typarsClause '(' sigArgs0 ')' implAttr '{'  */
#line 785 "asmparse.y"
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
#line 5498 "prebuilt\\asmparse.cpp"
    break;

  case 287: /* methAttr: %empty  */
#line 800 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) 0; }
#line 5504 "prebuilt\\asmparse.cpp"
    break;

  case 288: /* methAttr: methAttr STATIC_  */
#line 801 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdStatic); }
#line 5510 "prebuilt\\asmparse.cpp"
    break;

  case 289: /* methAttr: methAttr PUBLIC_  */
#line 802 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPublic); }
#line 5516 "prebuilt\\asmparse.cpp"
    break;

  case 290: /* methAttr: methAttr PRIVATE_  */
#line 803 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPrivate); }
#line 5522 "prebuilt\\asmparse.cpp"
    break;

  case 291: /* methAttr: methAttr FAMILY_  */
#line 804 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamily); }
#line 5528 "prebuilt\\asmparse.cpp"
    break;

  case 292: /* methAttr: methAttr FINAL_  */
#line 805 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdFinal); }
#line 5534 "prebuilt\\asmparse.cpp"
    break;

  case 293: /* methAttr: methAttr SPECIALNAME_  */
#line 806 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdSpecialName); }
#line 5540 "prebuilt\\asmparse.cpp"
    break;

  case 294: /* methAttr: methAttr VIRTUAL_  */
#line 807 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdVirtual); }
#line 5546 "prebuilt\\asmparse.cpp"
    break;

  case 295: /* methAttr: methAttr STRICT_  */
#line 808 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdCheckAccessOnOverride); }
#line 5552 "prebuilt\\asmparse.cpp"
    break;

  case 296: /* methAttr: methAttr ABSTRACT_  */
#line 809 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdAbstract); }
#line 5558 "prebuilt\\asmparse.cpp"
    break;

  case 297: /* methAttr: methAttr ASSEMBLY_  */
#line 810 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdAssem); }
#line 5564 "prebuilt\\asmparse.cpp"
    break;

  case 298: /* methAttr: methAttr FAMANDASSEM_  */
#line 811 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamANDAssem); }
#line 5570 "prebuilt\\asmparse.cpp"
    break;

  case 299: /* methAttr: methAttr FAMORASSEM_  */
#line 812 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamORAssem); }
#line 5576 "prebuilt\\asmparse.cpp"
    break;

  case 300: /* methAttr: methAttr PRIVATESCOPE_  */
#line 813 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPrivateScope); }
#line 5582 "prebuilt\\asmparse.cpp"
    break;

  case 301: /* methAttr: methAttr HIDEBYSIG_  */
#line 814 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdHideBySig); }
#line 5588 "prebuilt\\asmparse.cpp"
    break;

  case 302: /* methAttr: methAttr NEWSLOT_  */
#line 815 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdNewSlot); }
#line 5594 "prebuilt\\asmparse.cpp"
    break;

  case 303: /* methAttr: methAttr RTSPECIALNAME_  */
#line 816 "asmparse.y"
                                                            { (yyval.methAttr) = (yyvsp[-1].methAttr); }
#line 5600 "prebuilt\\asmparse.cpp"
    break;

  case 304: /* methAttr: methAttr UNMANAGEDEXP_  */
#line 817 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdUnmanagedExport); }
#line 5606 "prebuilt\\asmparse.cpp"
    break;

  case 305: /* methAttr: methAttr REQSECOBJ_  */
#line 818 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdRequireSecObject); }
#line 5612 "prebuilt\\asmparse.cpp"
    break;

  case 306: /* methAttr: methAttr FLAGS_ '(' int32 ')'  */
#line 819 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].int32)); }
#line 5618 "prebuilt\\asmparse.cpp"
    break;

  case 307: /* methAttr: methAttr PINVOKEIMPL_ '(' compQstring AS_ compQstring pinvAttr ')'  */
#line 821 "asmparse.y"
                                                            { PASM->SetPinvoke((yyvsp[-4].binstr),0,(yyvsp[-2].binstr),(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-7].methAttr) | mdPinvokeImpl); }
#line 5625 "prebuilt\\asmparse.cpp"
    break;

  case 308: /* methAttr: methAttr PINVOKEIMPL_ '(' compQstring pinvAttr ')'  */
#line 824 "asmparse.y"
                                                            { PASM->SetPinvoke((yyvsp[-2].binstr),0,NULL,(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-5].methAttr) | mdPinvokeImpl); }
#line 5632 "prebuilt\\asmparse.cpp"
    break;

  case 309: /* methAttr: methAttr PINVOKEIMPL_ '(' pinvAttr ')'  */
#line 827 "asmparse.y"
                                                            { PASM->SetPinvoke(new BinStr(),0,NULL,(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-4].methAttr) | mdPinvokeImpl); }
#line 5639 "prebuilt\\asmparse.cpp"
    break;

  case 310: /* pinvAttr: %empty  */
#line 831 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) 0; }
#line 5645 "prebuilt\\asmparse.cpp"
    break;

  case 311: /* pinvAttr: pinvAttr NOMANGLE_  */
#line 832 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmNoMangle); }
#line 5651 "prebuilt\\asmparse.cpp"
    break;

  case 312: /* pinvAttr: pinvAttr ANSI_  */
#line 833 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetAnsi); }
#line 5657 "prebuilt\\asmparse.cpp"
    break;

  case 313: /* pinvAttr: pinvAttr UNICODE_  */
#line 834 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetUnicode); }
#line 5663 "prebuilt\\asmparse.cpp"
    break;

  case 314: /* pinvAttr: pinvAttr AUTOCHAR_  */
#line 835 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetAuto); }
#line 5669 "prebuilt\\asmparse.cpp"
    break;

  case 315: /* pinvAttr: pinvAttr LASTERR_  */
#line 836 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmSupportsLastError); }
#line 5675 "prebuilt\\asmparse.cpp"
    break;

  case 316: /* pinvAttr: pinvAttr WINAPI_  */
#line 837 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvWinapi); }
#line 5681 "prebuilt\\asmparse.cpp"
    break;

  case 317: /* pinvAttr: pinvAttr CDECL_  */
#line 838 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvCdecl); }
#line 5687 "prebuilt\\asmparse.cpp"
    break;

  case 318: /* pinvAttr: pinvAttr STDCALL_  */
#line 839 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvStdcall); }
#line 5693 "prebuilt\\asmparse.cpp"
    break;

  case 319: /* pinvAttr: pinvAttr THISCALL_  */
#line 840 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvThiscall); }
#line 5699 "prebuilt\\asmparse.cpp"
    break;

  case 320: /* pinvAttr: pinvAttr FASTCALL_  */
#line 841 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvFastcall); }
#line 5705 "prebuilt\\asmparse.cpp"
    break;

  case 321: /* pinvAttr: pinvAttr BESTFIT_ ':' ON_  */
#line 842 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmBestFitEnabled); }
#line 5711 "prebuilt\\asmparse.cpp"
    break;

  case 322: /* pinvAttr: pinvAttr BESTFIT_ ':' OFF_  */
#line 843 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmBestFitDisabled); }
#line 5717 "prebuilt\\asmparse.cpp"
    break;

  case 323: /* pinvAttr: pinvAttr CHARMAPERROR_ ':' ON_  */
#line 844 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmThrowOnUnmappableCharEnabled); }
#line 5723 "prebuilt\\asmparse.cpp"
    break;

  case 324: /* pinvAttr: pinvAttr CHARMAPERROR_ ':' OFF_  */
#line 845 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmThrowOnUnmappableCharDisabled); }
#line 5729 "prebuilt\\asmparse.cpp"
    break;

  case 325: /* pinvAttr: pinvAttr FLAGS_ '(' int32 ')'  */
#line 846 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].int32)); }
#line 5735 "prebuilt\\asmparse.cpp"
    break;

  case 326: /* methodName: _CTOR  */
#line 849 "asmparse.y"
                                                            { (yyval.string) = newString(COR_CTOR_METHOD_NAME); }
#line 5741 "prebuilt\\asmparse.cpp"
    break;

  case 327: /* methodName: _CCTOR  */
#line 850 "asmparse.y"
                                                            { (yyval.string) = newString(COR_CCTOR_METHOD_NAME); }
#line 5747 "prebuilt\\asmparse.cpp"
    break;

  case 328: /* methodName: dottedName  */
#line 851 "asmparse.y"
                                                            { (yyval.string) = (yyvsp[0].string); }
#line 5753 "prebuilt\\asmparse.cpp"
    break;

  case 329: /* paramAttr: %empty  */
#line 854 "asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 5759 "prebuilt\\asmparse.cpp"
    break;

  case 330: /* paramAttr: paramAttr '[' IN_ ']'  */
#line 855 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdIn; }
#line 5765 "prebuilt\\asmparse.cpp"
    break;

  case 331: /* paramAttr: paramAttr '[' OUT_ ']'  */
#line 856 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdOut; }
#line 5771 "prebuilt\\asmparse.cpp"
    break;

  case 332: /* paramAttr: paramAttr '[' OPT_ ']'  */
#line 857 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdOptional; }
#line 5777 "prebuilt\\asmparse.cpp"
    break;

  case 333: /* paramAttr: paramAttr '[' int32 ']'  */
#line 858 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) + 1; }
#line 5783 "prebuilt\\asmparse.cpp"
    break;

  case 334: /* implAttr: %empty  */
#line 861 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (miIL | miManaged); }
#line 5789 "prebuilt\\asmparse.cpp"
    break;

  case 335: /* implAttr: implAttr NATIVE_  */
#line 862 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miNative); }
#line 5795 "prebuilt\\asmparse.cpp"
    break;

  case 336: /* implAttr: implAttr CIL_  */
#line 863 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miIL); }
#line 5801 "prebuilt\\asmparse.cpp"
    break;

  case 337: /* implAttr: implAttr OPTIL_  */
#line 864 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miOPTIL); }
#line 5807 "prebuilt\\asmparse.cpp"
    break;

  case 338: /* implAttr: implAttr MANAGED_  */
#line 865 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFFB) | miManaged); }
#line 5813 "prebuilt\\asmparse.cpp"
    break;

  case 339: /* implAttr: implAttr UNMANAGED_  */
#line 866 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFFB) | miUnmanaged); }
#line 5819 "prebuilt\\asmparse.cpp"
    break;

  case 340: /* implAttr: implAttr FORWARDREF_  */
#line 867 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miForwardRef); }
#line 5825 "prebuilt\\asmparse.cpp"
    break;

  case 341: /* implAttr: implAttr PRESERVESIG_  */
#line 868 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miPreserveSig); }
#line 5831 "prebuilt\\asmparse.cpp"
    break;

  case 342: /* implAttr: implAttr RUNTIME_  */
#line 869 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miRuntime); }
#line 5837 "prebuilt\\asmparse.cpp"
    break;

  case 343: /* implAttr: implAttr INTERNALCALL_  */
#line 870 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miInternalCall); }
#line 5843 "prebuilt\\asmparse.cpp"
    break;

  case 344: /* implAttr: implAttr SYNCHRONIZED_  */
#line 871 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miSynchronized); }
#line 5849 "prebuilt\\asmparse.cpp"
    break;

  case 345: /* implAttr: implAttr NOINLINING_  */
#line 872 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miNoInlining); }
#line 5855 "prebuilt\\asmparse.cpp"
    break;

  case 346: /* implAttr: implAttr AGGRESSIVEINLINING_  */
#line 873 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAggressiveInlining); }
#line 5861 "prebuilt\\asmparse.cpp"
    break;

  case 347: /* implAttr: implAttr NOOPTIMIZATION_  */
#line 874 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miNoOptimization); }
#line 5867 "prebuilt\\asmparse.cpp"
    break;

  case 348: /* implAttr: implAttr AGGRESSIVEOPTIMIZATION_  */
#line 875 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAggressiveOptimization); }
#line 5873 "prebuilt\\asmparse.cpp"
    break;

  case 349: /* implAttr: implAttr ASYNC_  */
#line 876 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAsync); }
#line 5879 "prebuilt\\asmparse.cpp"
    break;

  case 350: /* implAttr: implAttr FLAGS_ '(' int32 ')'  */
#line 877 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].int32)); }
#line 5885 "prebuilt\\asmparse.cpp"
    break;

  case 351: /* localsHead: _LOCALS  */
#line 880 "asmparse.y"
                                                            { PASM->delArgNameList(PASM->m_firstArgName); PASM->m_firstArgName = NULL;PASM->m_lastArgName = NULL;
                                                            }
#line 5892 "prebuilt\\asmparse.cpp"
    break;

  case 354: /* methodDecl: _EMITBYTE int32  */
#line 888 "asmparse.y"
                                                            { PASM->EmitByte((yyvsp[0].int32)); }
#line 5898 "prebuilt\\asmparse.cpp"
    break;

  case 355: /* methodDecl: sehBlock  */
#line 889 "asmparse.y"
                                                            { delete PASM->m_SEHD; PASM->m_SEHD = PASM->m_SEHDstack.POP(); }
#line 5904 "prebuilt\\asmparse.cpp"
    break;

  case 356: /* methodDecl: _MAXSTACK int32  */
#line 890 "asmparse.y"
                                                            { PASM->EmitMaxStack((yyvsp[0].int32)); }
#line 5910 "prebuilt\\asmparse.cpp"
    break;

  case 357: /* methodDecl: localsHead '(' sigArgs0 ')'  */
#line 891 "asmparse.y"
                                                            { PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, (yyvsp[-1].binstr)));
                                                            }
#line 5917 "prebuilt\\asmparse.cpp"
    break;

  case 358: /* methodDecl: localsHead INIT_ '(' sigArgs0 ')'  */
#line 893 "asmparse.y"
                                                            { PASM->EmitZeroInit();
                                                              PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, (yyvsp[-1].binstr)));
                                                            }
#line 5925 "prebuilt\\asmparse.cpp"
    break;

  case 359: /* methodDecl: _ENTRYPOINT  */
#line 896 "asmparse.y"
                                                            { PASM->EmitEntryPoint(); }
#line 5931 "prebuilt\\asmparse.cpp"
    break;

  case 360: /* methodDecl: _ZEROINIT  */
#line 897 "asmparse.y"
                                                            { PASM->EmitZeroInit(); }
#line 5937 "prebuilt\\asmparse.cpp"
    break;

  case 363: /* methodDecl: id ':'  */
#line 900 "asmparse.y"
                                                            { PASM->AddLabel(PASM->m_CurPC,(yyvsp[-1].string)); /*PASM->EmitLabel($1);*/ }
#line 5943 "prebuilt\\asmparse.cpp"
    break;

  case 369: /* methodDecl: _EXPORT '[' int32 ']'  */
#line 906 "asmparse.y"
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
#line 5958 "prebuilt\\asmparse.cpp"
    break;

  case 370: /* methodDecl: _EXPORT '[' int32 ']' AS_ id  */
#line 916 "asmparse.y"
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
#line 5973 "prebuilt\\asmparse.cpp"
    break;

  case 371: /* methodDecl: _VTENTRY int32 ':' int32  */
#line 926 "asmparse.y"
                                                            { PASM->m_pCurMethod->m_wVTEntry = (WORD)(yyvsp[-2].int32);
                                                              PASM->m_pCurMethod->m_wVTSlot = (WORD)(yyvsp[0].int32); }
#line 5980 "prebuilt\\asmparse.cpp"
    break;

  case 372: /* methodDecl: _OVERRIDE typeSpec DCOLON methodName  */
#line 929 "asmparse.y"
                                                            { PASM->AddMethodImpl((yyvsp[-2].token),(yyvsp[0].string),NULL,mdTokenNil,NULL,NULL); }
#line 5986 "prebuilt\\asmparse.cpp"
    break;

  case 373: /* methodDecl: _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'  */
#line 932 "asmparse.y"
                                                            { PASM->AddMethodImpl((yyvsp[-6].token),(yyvsp[-4].string),
                                                              ((yyvsp[-3].int32)==0 ? parser->MakeSig((yyvsp[-8].int32),(yyvsp[-7].binstr),(yyvsp[-1].binstr)) :
                                                              parser->MakeSig((yyvsp[-8].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-7].binstr),(yyvsp[-1].binstr),(yyvsp[-3].int32)))
                                                              ,mdTokenNil,NULL,NULL);
                                                              PASM->ResetArgNameList();
                                                            }
#line 5997 "prebuilt\\asmparse.cpp"
    break;

  case 375: /* methodDecl: _PARAM TYPE_ '[' int32 ']'  */
#line 939 "asmparse.y"
                                                            { if(((yyvsp[-1].int32) > 0) && ((yyvsp[-1].int32) <= (int)PASM->m_pCurMethod->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[(yyvsp[-1].int32)-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
#line 6007 "prebuilt\\asmparse.cpp"
    break;

  case 376: /* methodDecl: _PARAM TYPE_ dottedName  */
#line 944 "asmparse.y"
                                                            { int n = PASM->m_pCurMethod->FindTyPar((yyvsp[0].string));
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",(yyvsp[0].string));
                                                            }
#line 6018 "prebuilt\\asmparse.cpp"
    break;

  case 377: /* methodDecl: _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec  */
#line 950 "asmparse.y"
                                                                        { PASM->m_pCurMethod->AddGenericParamConstraint((yyvsp[-3].int32), 0, (yyvsp[0].token)); }
#line 6024 "prebuilt\\asmparse.cpp"
    break;

  case 378: /* methodDecl: _PARAM CONSTRAINT_ dottedName ',' typeSpec  */
#line 951 "asmparse.y"
                                                                        { PASM->m_pCurMethod->AddGenericParamConstraint(0, (yyvsp[-2].string), (yyvsp[0].token)); }
#line 6030 "prebuilt\\asmparse.cpp"
    break;

  case 379: /* methodDecl: _PARAM '[' int32 ']' initOpt  */
#line 954 "asmparse.y"
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
#line 6053 "prebuilt\\asmparse.cpp"
    break;

  case 380: /* scopeBlock: scopeOpen methodDecls '}'  */
#line 974 "asmparse.y"
                                                            { PASM->m_pCurMethod->CloseScope(); }
#line 6059 "prebuilt\\asmparse.cpp"
    break;

  case 381: /* scopeOpen: '{'  */
#line 977 "asmparse.y"
                                                            { PASM->m_pCurMethod->OpenScope(); }
#line 6065 "prebuilt\\asmparse.cpp"
    break;

  case 385: /* tryBlock: tryHead scopeBlock  */
#line 988 "asmparse.y"
                                                            { PASM->m_SEHD->tryTo = PASM->m_CurPC; }
#line 6071 "prebuilt\\asmparse.cpp"
    break;

  case 386: /* tryBlock: tryHead id TO_ id  */
#line 989 "asmparse.y"
                                                            { PASM->SetTryLabels((yyvsp[-2].string), (yyvsp[0].string)); }
#line 6077 "prebuilt\\asmparse.cpp"
    break;

  case 387: /* tryBlock: tryHead int32 TO_ int32  */
#line 990 "asmparse.y"
                                                            { if(PASM->m_SEHD) {PASM->m_SEHD->tryFrom = (yyvsp[-2].int32);
                                                              PASM->m_SEHD->tryTo = (yyvsp[0].int32);} }
#line 6084 "prebuilt\\asmparse.cpp"
    break;

  case 388: /* tryHead: _TRY  */
#line 994 "asmparse.y"
                                                            { PASM->NewSEHDescriptor();
                                                              PASM->m_SEHD->tryFrom = PASM->m_CurPC; }
#line 6091 "prebuilt\\asmparse.cpp"
    break;

  case 389: /* sehClause: catchClause handlerBlock  */
#line 999 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 6097 "prebuilt\\asmparse.cpp"
    break;

  case 390: /* sehClause: filterClause handlerBlock  */
#line 1000 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 6103 "prebuilt\\asmparse.cpp"
    break;

  case 391: /* sehClause: finallyClause handlerBlock  */
#line 1001 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 6109 "prebuilt\\asmparse.cpp"
    break;

  case 392: /* sehClause: faultClause handlerBlock  */
#line 1002 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 6115 "prebuilt\\asmparse.cpp"
    break;

  case 393: /* filterClause: filterHead scopeBlock  */
#line 1006 "asmparse.y"
                                                             { PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6121 "prebuilt\\asmparse.cpp"
    break;

  case 394: /* filterClause: filterHead id  */
#line 1007 "asmparse.y"
                                                             { PASM->SetFilterLabel((yyvsp[0].string));
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6128 "prebuilt\\asmparse.cpp"
    break;

  case 395: /* filterClause: filterHead int32  */
#line 1009 "asmparse.y"
                                                             { PASM->m_SEHD->sehFilter = (yyvsp[0].int32);
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6135 "prebuilt\\asmparse.cpp"
    break;

  case 396: /* filterHead: FILTER_  */
#line 1013 "asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FILTER;
                                                               PASM->m_SEHD->sehFilter = PASM->m_CurPC; }
#line 6142 "prebuilt\\asmparse.cpp"
    break;

  case 397: /* catchClause: CATCH_ typeSpec  */
#line 1017 "asmparse.y"
                                                            {  PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_NONE;
                                                               PASM->SetCatchClass((yyvsp[0].token));
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6150 "prebuilt\\asmparse.cpp"
    break;

  case 398: /* finallyClause: FINALLY_  */
#line 1022 "asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FINALLY;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6157 "prebuilt\\asmparse.cpp"
    break;

  case 399: /* faultClause: FAULT_  */
#line 1026 "asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FAULT;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6164 "prebuilt\\asmparse.cpp"
    break;

  case 400: /* handlerBlock: scopeBlock  */
#line 1030 "asmparse.y"
                                                             { PASM->m_SEHD->sehHandlerTo = PASM->m_CurPC; }
#line 6170 "prebuilt\\asmparse.cpp"
    break;

  case 401: /* handlerBlock: HANDLER_ id TO_ id  */
#line 1031 "asmparse.y"
                                                             { PASM->SetHandlerLabels((yyvsp[-2].string), (yyvsp[0].string)); }
#line 6176 "prebuilt\\asmparse.cpp"
    break;

  case 402: /* handlerBlock: HANDLER_ int32 TO_ int32  */
#line 1032 "asmparse.y"
                                                             { PASM->m_SEHD->sehHandler = (yyvsp[-2].int32);
                                                               PASM->m_SEHD->sehHandlerTo = (yyvsp[0].int32); }
#line 6183 "prebuilt\\asmparse.cpp"
    break;

  case 404: /* ddHead: _DATA tls id '='  */
#line 1040 "asmparse.y"
                                                             { PASM->EmitDataLabel((yyvsp[-1].string)); }
#line 6189 "prebuilt\\asmparse.cpp"
    break;

  case 406: /* tls: %empty  */
#line 1044 "asmparse.y"
                                                             { PASM->SetDataSection(); }
#line 6195 "prebuilt\\asmparse.cpp"
    break;

  case 407: /* tls: TLS_  */
#line 1045 "asmparse.y"
                                                             { PASM->SetTLSSection(); }
#line 6201 "prebuilt\\asmparse.cpp"
    break;

  case 408: /* tls: CIL_  */
#line 1046 "asmparse.y"
                                                             { PASM->SetILSection(); }
#line 6207 "prebuilt\\asmparse.cpp"
    break;

  case 413: /* ddItemCount: %empty  */
#line 1057 "asmparse.y"
                                                             { (yyval.int32) = 1; }
#line 6213 "prebuilt\\asmparse.cpp"
    break;

  case 414: /* ddItemCount: '[' int32 ']'  */
#line 1058 "asmparse.y"
                                                             { (yyval.int32) = (yyvsp[-1].int32);
                                                               if((yyvsp[-1].int32) <= 0) { PASM->report->error("Illegal item count: %d\n",(yyvsp[-1].int32));
                                                                  if(!PASM->OnErrGo) (yyval.int32) = 1; }}
#line 6221 "prebuilt\\asmparse.cpp"
    break;

  case 415: /* ddItem: CHAR_ '*' '(' compQstring ')'  */
#line 1063 "asmparse.y"
                                                             { PASM->EmitDataString((yyvsp[-1].binstr)); }
#line 6227 "prebuilt\\asmparse.cpp"
    break;

  case 416: /* ddItem: '&' '(' id ')'  */
#line 1064 "asmparse.y"
                                                             { PASM->EmitDD((yyvsp[-1].string)); }
#line 6233 "prebuilt\\asmparse.cpp"
    break;

  case 417: /* ddItem: bytearrayhead bytes ')'  */
#line 1065 "asmparse.y"
                                                             { PASM->EmitData((yyvsp[-1].binstr)->ptr(),(yyvsp[-1].binstr)->length()); }
#line 6239 "prebuilt\\asmparse.cpp"
    break;

  case 418: /* ddItem: FLOAT32_ '(' float64 ')' ddItemCount  */
#line 1067 "asmparse.y"
                                                             { float f = (float) (*(yyvsp[-2].float64)); float* p = new (nothrow) float[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i < (yyvsp[0].int32); i++) p[i] = f;
                                                                 PASM->EmitData(p, sizeof(float)*(yyvsp[0].int32)); delete (yyvsp[-2].float64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(float)*(yyvsp[0].int32)); }
#line 6250 "prebuilt\\asmparse.cpp"
    break;

  case 419: /* ddItem: FLOAT64_ '(' float64 ')' ddItemCount  */
#line 1074 "asmparse.y"
                                                             { double* p = new (nothrow) double[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = *((yyvsp[-2].float64));
                                                                 PASM->EmitData(p, sizeof(double)*(yyvsp[0].int32)); delete (yyvsp[-2].float64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(double)*(yyvsp[0].int32)); }
#line 6261 "prebuilt\\asmparse.cpp"
    break;

  case 420: /* ddItem: INT64_ '(' int64 ')' ddItemCount  */
#line 1081 "asmparse.y"
                                                             { int64_t* p = new (nothrow) int64_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = *((yyvsp[-2].int64));
                                                                 PASM->EmitData(p, sizeof(int64_t)*(yyvsp[0].int32)); delete (yyvsp[-2].int64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int64_t)*(yyvsp[0].int32)); }
#line 6272 "prebuilt\\asmparse.cpp"
    break;

  case 421: /* ddItem: INT32_ '(' int32 ')' ddItemCount  */
#line 1088 "asmparse.y"
                                                             { int32_t* p = new (nothrow) int32_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = (yyvsp[-2].int32);
                                                                 PASM->EmitData(p, sizeof(int32_t)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int32_t)*(yyvsp[0].int32)); }
#line 6283 "prebuilt\\asmparse.cpp"
    break;

  case 422: /* ddItem: INT16_ '(' int32 ')' ddItemCount  */
#line 1095 "asmparse.y"
                                                             { int16_t i = (int16_t) (yyvsp[-2].int32); FAIL_UNLESS(i == (yyvsp[-2].int32), ("Value %d too big\n", (yyvsp[-2].int32)));
                                                               int16_t* p = new (nothrow) int16_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<(yyvsp[0].int32); j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(int16_t)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int16_t)*(yyvsp[0].int32)); }
#line 6295 "prebuilt\\asmparse.cpp"
    break;

  case 423: /* ddItem: INT8_ '(' int32 ')' ddItemCount  */
#line 1103 "asmparse.y"
                                                             { int8_t i = (int8_t) (yyvsp[-2].int32); FAIL_UNLESS(i == (yyvsp[-2].int32), ("Value %d too big\n", (yyvsp[-2].int32)));
                                                               int8_t* p = new (nothrow) int8_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<(yyvsp[0].int32); j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(int8_t)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int8_t)*(yyvsp[0].int32)); }
#line 6307 "prebuilt\\asmparse.cpp"
    break;

  case 424: /* ddItem: FLOAT32_ ddItemCount  */
#line 1110 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(float)*(yyvsp[0].int32)); }
#line 6313 "prebuilt\\asmparse.cpp"
    break;

  case 425: /* ddItem: FLOAT64_ ddItemCount  */
#line 1111 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(double)*(yyvsp[0].int32)); }
#line 6319 "prebuilt\\asmparse.cpp"
    break;

  case 426: /* ddItem: INT64_ ddItemCount  */
#line 1112 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int64_t)*(yyvsp[0].int32)); }
#line 6325 "prebuilt\\asmparse.cpp"
    break;

  case 427: /* ddItem: INT32_ ddItemCount  */
#line 1113 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int32_t)*(yyvsp[0].int32)); }
#line 6331 "prebuilt\\asmparse.cpp"
    break;

  case 428: /* ddItem: INT16_ ddItemCount  */
#line 1114 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int16_t)*(yyvsp[0].int32)); }
#line 6337 "prebuilt\\asmparse.cpp"
    break;

  case 429: /* ddItem: INT8_ ddItemCount  */
#line 1115 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int8_t)*(yyvsp[0].int32)); }
#line 6343 "prebuilt\\asmparse.cpp"
    break;

  case 430: /* fieldSerInit: FLOAT32_ '(' float64 ')'  */
#line 1119 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               float f = (float)(*(yyvsp[-1].float64));
                                                               (yyval.binstr)->appendInt32(*((int32_t*)&f)); delete (yyvsp[-1].float64); }
#line 6351 "prebuilt\\asmparse.cpp"
    break;

  case 431: /* fieldSerInit: FLOAT64_ '(' float64 ')'  */
#line 1122 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].float64)); delete (yyvsp[-1].float64); }
#line 6358 "prebuilt\\asmparse.cpp"
    break;

  case 432: /* fieldSerInit: FLOAT32_ '(' int32 ')'  */
#line 1124 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6365 "prebuilt\\asmparse.cpp"
    break;

  case 433: /* fieldSerInit: FLOAT64_ '(' int64 ')'  */
#line 1126 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6372 "prebuilt\\asmparse.cpp"
    break;

  case 434: /* fieldSerInit: INT64_ '(' int64 ')'  */
#line 1128 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6379 "prebuilt\\asmparse.cpp"
    break;

  case 435: /* fieldSerInit: INT32_ '(' int32 ')'  */
#line 1130 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6386 "prebuilt\\asmparse.cpp"
    break;

  case 436: /* fieldSerInit: INT16_ '(' int32 ')'  */
#line 1132 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6393 "prebuilt\\asmparse.cpp"
    break;

  case 437: /* fieldSerInit: INT8_ '(' int32 ')'  */
#line 1134 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6400 "prebuilt\\asmparse.cpp"
    break;

  case 438: /* fieldSerInit: UNSIGNED_ INT64_ '(' int64 ')'  */
#line 1136 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6407 "prebuilt\\asmparse.cpp"
    break;

  case 439: /* fieldSerInit: UNSIGNED_ INT32_ '(' int32 ')'  */
#line 1138 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6414 "prebuilt\\asmparse.cpp"
    break;

  case 440: /* fieldSerInit: UNSIGNED_ INT16_ '(' int32 ')'  */
#line 1140 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6421 "prebuilt\\asmparse.cpp"
    break;

  case 441: /* fieldSerInit: UNSIGNED_ INT8_ '(' int32 ')'  */
#line 1142 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6428 "prebuilt\\asmparse.cpp"
    break;

  case 442: /* fieldSerInit: UINT64_ '(' int64 ')'  */
#line 1144 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6435 "prebuilt\\asmparse.cpp"
    break;

  case 443: /* fieldSerInit: UINT32_ '(' int32 ')'  */
#line 1146 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6442 "prebuilt\\asmparse.cpp"
    break;

  case 444: /* fieldSerInit: UINT16_ '(' int32 ')'  */
#line 1148 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6449 "prebuilt\\asmparse.cpp"
    break;

  case 445: /* fieldSerInit: UINT8_ '(' int32 ')'  */
#line 1150 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6456 "prebuilt\\asmparse.cpp"
    break;

  case 446: /* fieldSerInit: CHAR_ '(' int32 ')'  */
#line 1152 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CHAR);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6463 "prebuilt\\asmparse.cpp"
    break;

  case 447: /* fieldSerInit: BOOL_ '(' truefalse ')'  */
#line 1154 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_BOOLEAN);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32));}
#line 6470 "prebuilt\\asmparse.cpp"
    break;

  case 448: /* fieldSerInit: bytearrayhead bytes ')'  */
#line 1156 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING);
                                                               (yyval.binstr)->append((yyvsp[-1].binstr)); delete (yyvsp[-1].binstr);}
#line 6477 "prebuilt\\asmparse.cpp"
    break;

  case 449: /* bytearrayhead: BYTEARRAY_ '('  */
#line 1160 "asmparse.y"
                                                             { bParsingByteArray = TRUE; }
#line 6483 "prebuilt\\asmparse.cpp"
    break;

  case 450: /* bytes: %empty  */
#line 1163 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6489 "prebuilt\\asmparse.cpp"
    break;

  case 451: /* bytes: hexbytes  */
#line 1164 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6495 "prebuilt\\asmparse.cpp"
    break;

  case 452: /* hexbytes: HEXBYTE  */
#line 1167 "asmparse.y"
                                                             { int8_t i = (int8_t) (yyvsp[0].int32); (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(i); }
#line 6501 "prebuilt\\asmparse.cpp"
    break;

  case 453: /* hexbytes: hexbytes HEXBYTE  */
#line 1168 "asmparse.y"
                                                             { int8_t i = (int8_t) (yyvsp[0].int32); (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(i); }
#line 6507 "prebuilt\\asmparse.cpp"
    break;

  case 454: /* fieldInit: fieldSerInit  */
#line 1172 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6513 "prebuilt\\asmparse.cpp"
    break;

  case 455: /* fieldInit: compQstring  */
#line 1173 "asmparse.y"
                                                             { (yyval.binstr) = BinStrToUnicode((yyvsp[0].binstr),true); (yyval.binstr)->insertInt8(ELEMENT_TYPE_STRING);}
#line 6519 "prebuilt\\asmparse.cpp"
    break;

  case 456: /* fieldInit: NULLREF_  */
#line 1174 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CLASS);
                                                               (yyval.binstr)->appendInt32(0); }
#line 6526 "prebuilt\\asmparse.cpp"
    break;

  case 457: /* serInit: fieldSerInit  */
#line 1179 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6532 "prebuilt\\asmparse.cpp"
    break;

  case 458: /* serInit: STRING_ '(' NULLREF_ ')'  */
#line 1180 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); (yyval.binstr)->appendInt8(0xFF); }
#line 6538 "prebuilt\\asmparse.cpp"
    break;

  case 459: /* serInit: STRING_ '(' SQSTRING ')'  */
#line 1181 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[-1].string)); delete [] (yyvsp[-1].string);}
#line 6545 "prebuilt\\asmparse.cpp"
    break;

  case 460: /* serInit: TYPE_ '(' CLASS_ SQSTRING ')'  */
#line 1183 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[-1].string)); delete [] (yyvsp[-1].string);}
#line 6552 "prebuilt\\asmparse.cpp"
    break;

  case 461: /* serInit: TYPE_ '(' className ')'  */
#line 1185 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[-1].token)));}
#line 6559 "prebuilt\\asmparse.cpp"
    break;

  case 462: /* serInit: TYPE_ '(' NULLREF_ ')'  */
#line 1187 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE); (yyval.binstr)->appendInt8(0xFF); }
#line 6565 "prebuilt\\asmparse.cpp"
    break;

  case 463: /* serInit: OBJECT_ '(' serInit ')'  */
#line 1188 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);}
#line 6571 "prebuilt\\asmparse.cpp"
    break;

  case 464: /* serInit: FLOAT32_ '[' int32 ']' '(' f32seq ')'  */
#line 1190 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_R4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6579 "prebuilt\\asmparse.cpp"
    break;

  case 465: /* serInit: FLOAT64_ '[' int32 ']' '(' f64seq ')'  */
#line 1194 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6587 "prebuilt\\asmparse.cpp"
    break;

  case 466: /* serInit: INT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1198 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6595 "prebuilt\\asmparse.cpp"
    break;

  case 467: /* serInit: INT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1202 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6603 "prebuilt\\asmparse.cpp"
    break;

  case 468: /* serInit: INT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1206 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6611 "prebuilt\\asmparse.cpp"
    break;

  case 469: /* serInit: INT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1210 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6619 "prebuilt\\asmparse.cpp"
    break;

  case 470: /* serInit: UINT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1214 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6627 "prebuilt\\asmparse.cpp"
    break;

  case 471: /* serInit: UINT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1218 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6635 "prebuilt\\asmparse.cpp"
    break;

  case 472: /* serInit: UINT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1222 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6643 "prebuilt\\asmparse.cpp"
    break;

  case 473: /* serInit: UINT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1226 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6651 "prebuilt\\asmparse.cpp"
    break;

  case 474: /* serInit: UNSIGNED_ INT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1230 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6659 "prebuilt\\asmparse.cpp"
    break;

  case 475: /* serInit: UNSIGNED_ INT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1234 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6667 "prebuilt\\asmparse.cpp"
    break;

  case 476: /* serInit: UNSIGNED_ INT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1238 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6675 "prebuilt\\asmparse.cpp"
    break;

  case 477: /* serInit: UNSIGNED_ INT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1242 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6683 "prebuilt\\asmparse.cpp"
    break;

  case 478: /* serInit: CHAR_ '[' int32 ']' '(' i16seq ')'  */
#line 1246 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_CHAR);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6691 "prebuilt\\asmparse.cpp"
    break;

  case 479: /* serInit: BOOL_ '[' int32 ']' '(' boolSeq ')'  */
#line 1250 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_BOOLEAN);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6699 "prebuilt\\asmparse.cpp"
    break;

  case 480: /* serInit: STRING_ '[' int32 ']' '(' sqstringSeq ')'  */
#line 1254 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_STRING);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6707 "prebuilt\\asmparse.cpp"
    break;

  case 481: /* serInit: TYPE_ '[' int32 ']' '(' classSeq ')'  */
#line 1258 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TYPE);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6715 "prebuilt\\asmparse.cpp"
    break;

  case 482: /* serInit: OBJECT_ '[' int32 ']' '(' objSeq ')'  */
#line 1262 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6723 "prebuilt\\asmparse.cpp"
    break;

  case 483: /* f32seq: %empty  */
#line 1268 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6729 "prebuilt\\asmparse.cpp"
    break;

  case 484: /* f32seq: f32seq float64  */
#line 1269 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               float f = (float) (*(yyvsp[0].float64)); (yyval.binstr)->appendInt32(*((int32_t*)&f)); delete (yyvsp[0].float64); }
#line 6736 "prebuilt\\asmparse.cpp"
    break;

  case 485: /* f32seq: f32seq int32  */
#line 1271 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 6743 "prebuilt\\asmparse.cpp"
    break;

  case 486: /* f64seq: %empty  */
#line 1275 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6749 "prebuilt\\asmparse.cpp"
    break;

  case 487: /* f64seq: f64seq float64  */
#line 1276 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[0].float64)); delete (yyvsp[0].float64); }
#line 6756 "prebuilt\\asmparse.cpp"
    break;

  case 488: /* f64seq: f64seq int64  */
#line 1278 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[0].int64)); delete (yyvsp[0].int64); }
#line 6763 "prebuilt\\asmparse.cpp"
    break;

  case 489: /* i64seq: %empty  */
#line 1282 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6769 "prebuilt\\asmparse.cpp"
    break;

  case 490: /* i64seq: i64seq int64  */
#line 1283 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[0].int64)); delete (yyvsp[0].int64); }
#line 6776 "prebuilt\\asmparse.cpp"
    break;

  case 491: /* i32seq: %empty  */
#line 1287 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6782 "prebuilt\\asmparse.cpp"
    break;

  case 492: /* i32seq: i32seq int32  */
#line 1288 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt32((yyvsp[0].int32));}
#line 6788 "prebuilt\\asmparse.cpp"
    break;

  case 493: /* i16seq: %empty  */
#line 1291 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6794 "prebuilt\\asmparse.cpp"
    break;

  case 494: /* i16seq: i16seq int32  */
#line 1292 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt16((yyvsp[0].int32));}
#line 6800 "prebuilt\\asmparse.cpp"
    break;

  case 495: /* i8seq: %empty  */
#line 1295 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6806 "prebuilt\\asmparse.cpp"
    break;

  case 496: /* i8seq: i8seq int32  */
#line 1296 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8((yyvsp[0].int32)); }
#line 6812 "prebuilt\\asmparse.cpp"
    break;

  case 497: /* boolSeq: %empty  */
#line 1299 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6818 "prebuilt\\asmparse.cpp"
    break;

  case 498: /* boolSeq: boolSeq truefalse  */
#line 1300 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt8((yyvsp[0].int32));}
#line 6825 "prebuilt\\asmparse.cpp"
    break;

  case 499: /* sqstringSeq: %empty  */
#line 1304 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6831 "prebuilt\\asmparse.cpp"
    break;

  case 500: /* sqstringSeq: sqstringSeq NULLREF_  */
#line 1305 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(0xFF); }
#line 6837 "prebuilt\\asmparse.cpp"
    break;

  case 501: /* sqstringSeq: sqstringSeq SQSTRING  */
#line 1306 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); delete [] (yyvsp[0].string);}
#line 6844 "prebuilt\\asmparse.cpp"
    break;

  case 502: /* classSeq: %empty  */
#line 1310 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6850 "prebuilt\\asmparse.cpp"
    break;

  case 503: /* classSeq: classSeq NULLREF_  */
#line 1311 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(0xFF); }
#line 6856 "prebuilt\\asmparse.cpp"
    break;

  case 504: /* classSeq: classSeq CLASS_ SQSTRING  */
#line 1312 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); delete [] (yyvsp[0].string);}
#line 6863 "prebuilt\\asmparse.cpp"
    break;

  case 505: /* classSeq: classSeq className  */
#line 1314 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[0].token)));}
#line 6870 "prebuilt\\asmparse.cpp"
    break;

  case 506: /* objSeq: %empty  */
#line 1318 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6876 "prebuilt\\asmparse.cpp"
    break;

  case 507: /* objSeq: objSeq serInit  */
#line 1319 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 6882 "prebuilt\\asmparse.cpp"
    break;

  case 508: /* methodSpec: METHOD_  */
#line 1323 "asmparse.y"
                                                             { parser->m_ANSFirst.PUSH(PASM->m_firstArgName);
                                                               parser->m_ANSLast.PUSH(PASM->m_lastArgName);
                                                               PASM->m_firstArgName = NULL;
                                                               PASM->m_lastArgName = NULL; }
#line 6891 "prebuilt\\asmparse.cpp"
    break;

  case 509: /* instr_none: INSTR_NONE  */
#line 1329 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6897 "prebuilt\\asmparse.cpp"
    break;

  case 510: /* instr_var: INSTR_VAR  */
#line 1332 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6903 "prebuilt\\asmparse.cpp"
    break;

  case 511: /* instr_i: INSTR_I  */
#line 1335 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6909 "prebuilt\\asmparse.cpp"
    break;

  case 512: /* instr_i8: INSTR_I8  */
#line 1338 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6915 "prebuilt\\asmparse.cpp"
    break;

  case 513: /* instr_r: INSTR_R  */
#line 1341 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6921 "prebuilt\\asmparse.cpp"
    break;

  case 514: /* instr_brtarget: INSTR_BRTARGET  */
#line 1344 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6927 "prebuilt\\asmparse.cpp"
    break;

  case 515: /* instr_method: INSTR_METHOD  */
#line 1347 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode));
                                                               if((!PASM->OnErrGo)&&
                                                               (((yyvsp[0].opcode) == CEE_NEWOBJ)||
                                                                ((yyvsp[0].opcode) == CEE_CALLVIRT)))
                                                                  iCallConv = IMAGE_CEE_CS_CALLCONV_HASTHIS;
                                                             }
#line 6938 "prebuilt\\asmparse.cpp"
    break;

  case 516: /* instr_field: INSTR_FIELD  */
#line 1355 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6944 "prebuilt\\asmparse.cpp"
    break;

  case 517: /* instr_type: INSTR_TYPE  */
#line 1358 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6950 "prebuilt\\asmparse.cpp"
    break;

  case 518: /* instr_string: INSTR_STRING  */
#line 1361 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6956 "prebuilt\\asmparse.cpp"
    break;

  case 519: /* instr_sig: INSTR_SIG  */
#line 1364 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6962 "prebuilt\\asmparse.cpp"
    break;

  case 520: /* instr_tok: INSTR_TOK  */
#line 1367 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); iOpcodeLen = PASM->OpcodeLen((yyval.instr)); }
#line 6968 "prebuilt\\asmparse.cpp"
    break;

  case 521: /* instr_switch: INSTR_SWITCH  */
#line 1370 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6974 "prebuilt\\asmparse.cpp"
    break;

  case 522: /* instr_r_head: instr_r '('  */
#line 1373 "asmparse.y"
                                                             { (yyval.instr) = (yyvsp[-1].instr); bParsingByteArray = TRUE; }
#line 6980 "prebuilt\\asmparse.cpp"
    break;

  case 523: /* instr: instr_none  */
#line 1377 "asmparse.y"
                                                             { PASM->EmitOpcode((yyvsp[0].instr)); }
#line 6986 "prebuilt\\asmparse.cpp"
    break;

  case 524: /* instr: instr_var int32  */
#line 1378 "asmparse.y"
                                                             { PASM->EmitInstrVar((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 6992 "prebuilt\\asmparse.cpp"
    break;

  case 525: /* instr: instr_var id  */
#line 1379 "asmparse.y"
                                                             { PASM->EmitInstrVarByName((yyvsp[-1].instr), (yyvsp[0].string)); }
#line 6998 "prebuilt\\asmparse.cpp"
    break;

  case 526: /* instr: instr_i int32  */
#line 1380 "asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 7004 "prebuilt\\asmparse.cpp"
    break;

  case 527: /* instr: instr_i8 int64  */
#line 1381 "asmparse.y"
                                                             { PASM->EmitInstrI8((yyvsp[-1].instr), (yyvsp[0].int64)); }
#line 7010 "prebuilt\\asmparse.cpp"
    break;

  case 528: /* instr: instr_r float64  */
#line 1382 "asmparse.y"
                                                             { PASM->EmitInstrR((yyvsp[-1].instr), (yyvsp[0].float64)); delete ((yyvsp[0].float64));}
#line 7016 "prebuilt\\asmparse.cpp"
    break;

  case 529: /* instr: instr_r int64  */
#line 1383 "asmparse.y"
                                                             { double f = (double) (*(yyvsp[0].int64)); PASM->EmitInstrR((yyvsp[-1].instr), &f); }
#line 7022 "prebuilt\\asmparse.cpp"
    break;

  case 530: /* instr: instr_r_head bytes ')'  */
#line 1384 "asmparse.y"
                                                             { unsigned L = (yyvsp[-1].binstr)->length();
                                                               FAIL_UNLESS(L >= sizeof(float), ("%d hexbytes, must be at least %d\n",
                                                                           L,sizeof(float)));
                                                               if(L < sizeof(float)) {YYERROR; }
                                                               else {
                                                                   double f = (L >= sizeof(double)) ? *((double *)((yyvsp[-1].binstr)->ptr()))
                                                                                    : (double)(*(float *)((yyvsp[-1].binstr)->ptr()));
                                                                   PASM->EmitInstrR((yyvsp[-2].instr),&f); }
                                                               delete (yyvsp[-1].binstr); }
#line 7036 "prebuilt\\asmparse.cpp"
    break;

  case 531: /* instr: instr_brtarget int32  */
#line 1393 "asmparse.y"
                                                             { PASM->EmitInstrBrOffset((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 7042 "prebuilt\\asmparse.cpp"
    break;

  case 532: /* instr: instr_brtarget id  */
#line 1394 "asmparse.y"
                                                             { PASM->EmitInstrBrTarget((yyvsp[-1].instr), (yyvsp[0].string)); }
#line 7048 "prebuilt\\asmparse.cpp"
    break;

  case 533: /* instr: instr_method methodRef  */
#line 1396 "asmparse.y"
                                                             { PASM->SetMemberRefFixup((yyvsp[0].token),PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),(yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iCallConv = 0;
                                                             }
#line 7059 "prebuilt\\asmparse.cpp"
    break;

  case 534: /* instr: instr_field type typeSpec DCOLON dottedName  */
#line 1403 "asmparse.y"
                                                             { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef((yyvsp[-2].token), (yyvsp[0].string), (yyvsp[-3].binstr));
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-4].instr)));
                                                               PASM->EmitInstrI((yyvsp[-4].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7071 "prebuilt\\asmparse.cpp"
    break;

  case 535: /* instr: instr_field type dottedName  */
#line 1411 "asmparse.y"
                                                             { (yyvsp[-1].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef(mdTokenNil, (yyvsp[0].string), (yyvsp[-1].binstr));
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-2].instr)));
                                                               PASM->EmitInstrI((yyvsp[-2].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7083 "prebuilt\\asmparse.cpp"
    break;

  case 536: /* instr: instr_field mdtoken  */
#line 1418 "asmparse.y"
                                                             { mdToken mr = (yyvsp[0].token);
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7094 "prebuilt\\asmparse.cpp"
    break;

  case 537: /* instr: instr_field TYPEDEF_F  */
#line 1424 "asmparse.y"
                                                             { mdToken mr = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7105 "prebuilt\\asmparse.cpp"
    break;

  case 538: /* instr: instr_field TYPEDEF_MR  */
#line 1430 "asmparse.y"
                                                             { mdToken mr = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7116 "prebuilt\\asmparse.cpp"
    break;

  case 539: /* instr: instr_type typeSpec  */
#line 1436 "asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr), (yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7125 "prebuilt\\asmparse.cpp"
    break;

  case 540: /* instr: instr_string compQstring  */
#line 1440 "asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-1].instr), (yyvsp[0].binstr),TRUE); }
#line 7131 "prebuilt\\asmparse.cpp"
    break;

  case 541: /* instr: instr_string ANSI_ '(' compQstring ')'  */
#line 1442 "asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-4].instr), (yyvsp[-1].binstr),FALSE); }
#line 7137 "prebuilt\\asmparse.cpp"
    break;

  case 542: /* instr: instr_string bytearrayhead bytes ')'  */
#line 1444 "asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-3].instr), (yyvsp[-1].binstr),FALSE,TRUE); }
#line 7143 "prebuilt\\asmparse.cpp"
    break;

  case 543: /* instr: instr_sig callConv type '(' sigArgs0 ')'  */
#line 1446 "asmparse.y"
                                                             { PASM->EmitInstrSig((yyvsp[-5].instr), parser->MakeSig((yyvsp[-4].int32), (yyvsp[-3].binstr), (yyvsp[-1].binstr)));
                                                               PASM->ResetArgNameList();
                                                             }
#line 7151 "prebuilt\\asmparse.cpp"
    break;

  case 544: /* instr: instr_tok ownerType  */
#line 1450 "asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr),(yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iOpcodeLen = 0;
                                                             }
#line 7161 "prebuilt\\asmparse.cpp"
    break;

  case 545: /* instr: instr_switch '(' labels ')'  */
#line 1455 "asmparse.y"
                                                             { PASM->EmitInstrSwitch((yyvsp[-3].instr), (yyvsp[-1].labels)); }
#line 7167 "prebuilt\\asmparse.cpp"
    break;

  case 546: /* labels: %empty  */
#line 1458 "asmparse.y"
                                                              { (yyval.labels) = 0; }
#line 7173 "prebuilt\\asmparse.cpp"
    break;

  case 547: /* labels: id ',' labels  */
#line 1459 "asmparse.y"
                                                              { (yyval.labels) = new Labels((yyvsp[-2].string), (yyvsp[0].labels), TRUE); }
#line 7179 "prebuilt\\asmparse.cpp"
    break;

  case 548: /* labels: int32 ',' labels  */
#line 1460 "asmparse.y"
                                                              { (yyval.labels) = new Labels((char *)(UINT_PTR)(yyvsp[-2].int32), (yyvsp[0].labels), FALSE); }
#line 7185 "prebuilt\\asmparse.cpp"
    break;

  case 549: /* labels: id  */
#line 1461 "asmparse.y"
                                                              { (yyval.labels) = new Labels((yyvsp[0].string), NULL, TRUE); }
#line 7191 "prebuilt\\asmparse.cpp"
    break;

  case 550: /* labels: int32  */
#line 1462 "asmparse.y"
                                                              { (yyval.labels) = new Labels((char *)(UINT_PTR)(yyvsp[0].int32), NULL, FALSE); }
#line 7197 "prebuilt\\asmparse.cpp"
    break;

  case 551: /* tyArgs0: %empty  */
#line 1466 "asmparse.y"
                                                             { (yyval.binstr) = NULL; }
#line 7203 "prebuilt\\asmparse.cpp"
    break;

  case 552: /* tyArgs0: '<' tyArgs1 '>'  */
#line 1467 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 7209 "prebuilt\\asmparse.cpp"
    break;

  case 553: /* tyArgs1: %empty  */
#line 1470 "asmparse.y"
                                                             { (yyval.binstr) = NULL; }
#line 7215 "prebuilt\\asmparse.cpp"
    break;

  case 554: /* tyArgs1: tyArgs2  */
#line 1471 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7221 "prebuilt\\asmparse.cpp"
    break;

  case 555: /* tyArgs2: type  */
#line 1474 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7227 "prebuilt\\asmparse.cpp"
    break;

  case 556: /* tyArgs2: tyArgs2 ',' type  */
#line 1475 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 7233 "prebuilt\\asmparse.cpp"
    break;

  case 557: /* sigArgs0: %empty  */
#line 1479 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 7239 "prebuilt\\asmparse.cpp"
    break;

  case 558: /* sigArgs0: sigArgs1  */
#line 1480 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr);}
#line 7245 "prebuilt\\asmparse.cpp"
    break;

  case 559: /* sigArgs1: sigArg  */
#line 1483 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7251 "prebuilt\\asmparse.cpp"
    break;

  case 560: /* sigArgs1: sigArgs1 ',' sigArg  */
#line 1484 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 7257 "prebuilt\\asmparse.cpp"
    break;

  case 561: /* sigArg: ELLIPSIS  */
#line 1487 "asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_SENTINEL); }
#line 7263 "prebuilt\\asmparse.cpp"
    break;

  case 562: /* sigArg: paramAttr type marshalClause  */
#line 1488 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[-1].binstr)); PASM->addArgName(NULL, (yyvsp[-1].binstr), (yyvsp[0].binstr), (yyvsp[-2].int32)); }
#line 7269 "prebuilt\\asmparse.cpp"
    break;

  case 563: /* sigArg: paramAttr type marshalClause id  */
#line 1489 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[-2].binstr)); PASM->addArgName((yyvsp[0].string), (yyvsp[-2].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32));}
#line 7275 "prebuilt\\asmparse.cpp"
    break;

  case 564: /* className: '[' dottedName ']' slashedName  */
#line 1493 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(PASM->GetAsmRef((yyvsp[-2].string)), (yyvsp[0].string), NULL); delete[] (yyvsp[-2].string);}
#line 7281 "prebuilt\\asmparse.cpp"
    break;

  case 565: /* className: '[' mdtoken ']' slashedName  */
#line 1494 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef((yyvsp[-2].token), (yyvsp[0].string), NULL); }
#line 7287 "prebuilt\\asmparse.cpp"
    break;

  case 566: /* className: '[' '*' ']' slashedName  */
#line 1495 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(mdTokenNil, (yyvsp[0].string), NULL); }
#line 7293 "prebuilt\\asmparse.cpp"
    break;

  case 567: /* className: '[' _MODULE dottedName ']' slashedName  */
#line 1496 "asmparse.y"
                                                                   { (yyval.token) = PASM->ResolveClassRef(PASM->GetModRef((yyvsp[-2].string)),(yyvsp[0].string), NULL); delete[] (yyvsp[-2].string);}
#line 7299 "prebuilt\\asmparse.cpp"
    break;

  case 568: /* className: slashedName  */
#line 1497 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(1,(yyvsp[0].string),NULL); }
#line 7305 "prebuilt\\asmparse.cpp"
    break;

  case 569: /* className: mdtoken  */
#line 1498 "asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].token); }
#line 7311 "prebuilt\\asmparse.cpp"
    break;

  case 570: /* className: TYPEDEF_T  */
#line 1499 "asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 7317 "prebuilt\\asmparse.cpp"
    break;

  case 571: /* className: _THIS  */
#line 1500 "asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) (yyval.token) = PASM->m_pCurClass->m_cl;
                                                                else { (yyval.token) = 0; PASM->report->error(".this outside class scope\n"); }
                                                              }
#line 7325 "prebuilt\\asmparse.cpp"
    break;

  case 572: /* className: _BASE  */
#line 1503 "asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) {
                                                                  (yyval.token) = PASM->m_pCurClass->m_crExtends;
                                                                  if(RidFromToken((yyval.token)) == 0)
                                                                    PASM->report->error(".base undefined\n");
                                                                } else { (yyval.token) = 0; PASM->report->error(".base outside class scope\n"); }
                                                              }
#line 7336 "prebuilt\\asmparse.cpp"
    break;

  case 573: /* className: _NESTER  */
#line 1509 "asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) {
                                                                  if(PASM->m_pCurClass->m_pEncloser != NULL) (yyval.token) = PASM->m_pCurClass->m_pEncloser->m_cl;
                                                                  else { (yyval.token) = 0; PASM->report->error(".nester undefined\n"); }
                                                                } else { (yyval.token) = 0; PASM->report->error(".nester outside class scope\n"); }
                                                              }
#line 7346 "prebuilt\\asmparse.cpp"
    break;

  case 574: /* slashedName: dottedName  */
#line 1516 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 7352 "prebuilt\\asmparse.cpp"
    break;

  case 575: /* slashedName: slashedName '/' dottedName  */
#line 1517 "asmparse.y"
                                                              { (yyval.string) = newStringWDel((yyvsp[-2].string), NESTING_SEP, (yyvsp[0].string)); }
#line 7358 "prebuilt\\asmparse.cpp"
    break;

  case 576: /* typeSpec: className  */
#line 1520 "asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].token);}
#line 7364 "prebuilt\\asmparse.cpp"
    break;

  case 577: /* typeSpec: '[' dottedName ']'  */
#line 1521 "asmparse.y"
                                                              { (yyval.token) = PASM->GetAsmRef((yyvsp[-1].string)); delete[] (yyvsp[-1].string);}
#line 7370 "prebuilt\\asmparse.cpp"
    break;

  case 578: /* typeSpec: '[' _MODULE dottedName ']'  */
#line 1522 "asmparse.y"
                                                              { (yyval.token) = PASM->GetModRef((yyvsp[-1].string)); delete[] (yyvsp[-1].string);}
#line 7376 "prebuilt\\asmparse.cpp"
    break;

  case 579: /* typeSpec: type  */
#line 1523 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveTypeSpec((yyvsp[0].binstr)); }
#line 7382 "prebuilt\\asmparse.cpp"
    break;

  case 580: /* nativeType: %empty  */
#line 1527 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); }
#line 7388 "prebuilt\\asmparse.cpp"
    break;

  case 581: /* nativeType: CUSTOM_ '(' compQstring ',' compQstring ',' compQstring ',' compQstring ')'  */
#line 1529 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt((yyval.binstr),(yyvsp[-7].binstr)->length()); (yyval.binstr)->append((yyvsp[-7].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-5].binstr)->length()); (yyval.binstr)->append((yyvsp[-5].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].binstr)->length()); (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].binstr)->length()); (yyval.binstr)->append((yyvsp[-1].binstr));
                                                                PASM->report->warn("Deprecated 4-string form of custom marshaler, first two strings ignored\n");}
#line 7399 "prebuilt\\asmparse.cpp"
    break;

  case 582: /* nativeType: CUSTOM_ '(' compQstring ',' compQstring ')'  */
#line 1536 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].binstr)->length()); (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].binstr)->length()); (yyval.binstr)->append((yyvsp[-1].binstr)); }
#line 7409 "prebuilt\\asmparse.cpp"
    break;

  case 583: /* nativeType: FIXED_ SYSSTRING_ '[' int32 ']'  */
#line 1541 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FIXEDSYSSTRING);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32)); }
#line 7416 "prebuilt\\asmparse.cpp"
    break;

  case 584: /* nativeType: FIXED_ ARRAY_ '[' int32 ']' nativeType  */
#line 1544 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FIXEDARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-2].int32)); (yyval.binstr)->append((yyvsp[0].binstr)); }
#line 7423 "prebuilt\\asmparse.cpp"
    break;

  case 585: /* nativeType: VARIANT_  */
#line 1546 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VARIANT);
                                                                PASM->report->warn("Deprecated native type 'variant'\n"); }
#line 7430 "prebuilt\\asmparse.cpp"
    break;

  case 586: /* nativeType: CURRENCY_  */
#line 1548 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CURRENCY); }
#line 7436 "prebuilt\\asmparse.cpp"
    break;

  case 587: /* nativeType: SYSCHAR_  */
#line 1549 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SYSCHAR);
                                                                PASM->report->warn("Deprecated native type 'syschar'\n"); }
#line 7443 "prebuilt\\asmparse.cpp"
    break;

  case 588: /* nativeType: VOID_  */
#line 1551 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VOID);
                                                                PASM->report->warn("Deprecated native type 'void'\n"); }
#line 7450 "prebuilt\\asmparse.cpp"
    break;

  case 589: /* nativeType: BOOL_  */
#line 1553 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BOOLEAN); }
#line 7456 "prebuilt\\asmparse.cpp"
    break;

  case 590: /* nativeType: INT8_  */
#line 1554 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I1); }
#line 7462 "prebuilt\\asmparse.cpp"
    break;

  case 591: /* nativeType: INT16_  */
#line 1555 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I2); }
#line 7468 "prebuilt\\asmparse.cpp"
    break;

  case 592: /* nativeType: INT32_  */
#line 1556 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I4); }
#line 7474 "prebuilt\\asmparse.cpp"
    break;

  case 593: /* nativeType: INT64_  */
#line 1557 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I8); }
#line 7480 "prebuilt\\asmparse.cpp"
    break;

  case 594: /* nativeType: FLOAT32_  */
#line 1558 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_R4); }
#line 7486 "prebuilt\\asmparse.cpp"
    break;

  case 595: /* nativeType: FLOAT64_  */
#line 1559 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_R8); }
#line 7492 "prebuilt\\asmparse.cpp"
    break;

  case 596: /* nativeType: ERROR_  */
#line 1560 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ERROR); }
#line 7498 "prebuilt\\asmparse.cpp"
    break;

  case 597: /* nativeType: UNSIGNED_ INT8_  */
#line 1561 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U1); }
#line 7504 "prebuilt\\asmparse.cpp"
    break;

  case 598: /* nativeType: UNSIGNED_ INT16_  */
#line 1562 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U2); }
#line 7510 "prebuilt\\asmparse.cpp"
    break;

  case 599: /* nativeType: UNSIGNED_ INT32_  */
#line 1563 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U4); }
#line 7516 "prebuilt\\asmparse.cpp"
    break;

  case 600: /* nativeType: UNSIGNED_ INT64_  */
#line 1564 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U8); }
#line 7522 "prebuilt\\asmparse.cpp"
    break;

  case 601: /* nativeType: UINT8_  */
#line 1565 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U1); }
#line 7528 "prebuilt\\asmparse.cpp"
    break;

  case 602: /* nativeType: UINT16_  */
#line 1566 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U2); }
#line 7534 "prebuilt\\asmparse.cpp"
    break;

  case 603: /* nativeType: UINT32_  */
#line 1567 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U4); }
#line 7540 "prebuilt\\asmparse.cpp"
    break;

  case 604: /* nativeType: UINT64_  */
#line 1568 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U8); }
#line 7546 "prebuilt\\asmparse.cpp"
    break;

  case 605: /* nativeType: nativeType '*'  */
#line 1569 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(NATIVE_TYPE_PTR);
                                                                PASM->report->warn("Deprecated native type '*'\n"); }
#line 7553 "prebuilt\\asmparse.cpp"
    break;

  case 606: /* nativeType: nativeType '[' ']'  */
#line 1571 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY); }
#line 7560 "prebuilt\\asmparse.cpp"
    break;

  case 607: /* nativeType: nativeType '[' int32 ']'  */
#line 1573 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-3].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32));
                                                                corEmitInt((yyval.binstr),0); }
#line 7570 "prebuilt\\asmparse.cpp"
    break;

  case 608: /* nativeType: nativeType '[' int32 '+' int32 ']'  */
#line 1578 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-5].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32));
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].int32));
                                                                corEmitInt((yyval.binstr),ntaSizeParamIndexSpecified); }
#line 7580 "prebuilt\\asmparse.cpp"
    break;

  case 609: /* nativeType: nativeType '[' '+' int32 ']'  */
#line 1583 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-4].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32)); }
#line 7588 "prebuilt\\asmparse.cpp"
    break;

  case 610: /* nativeType: DECIMAL_  */
#line 1586 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_DECIMAL);
                                                                PASM->report->warn("Deprecated native type 'decimal'\n"); }
#line 7595 "prebuilt\\asmparse.cpp"
    break;

  case 611: /* nativeType: DATE_  */
#line 1588 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_DATE);
                                                                PASM->report->warn("Deprecated native type 'date'\n"); }
#line 7602 "prebuilt\\asmparse.cpp"
    break;

  case 612: /* nativeType: BSTR_  */
#line 1590 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BSTR); }
#line 7608 "prebuilt\\asmparse.cpp"
    break;

  case 613: /* nativeType: LPSTR_  */
#line 1591 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPSTR); }
#line 7614 "prebuilt\\asmparse.cpp"
    break;

  case 614: /* nativeType: LPWSTR_  */
#line 1592 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPWSTR); }
#line 7620 "prebuilt\\asmparse.cpp"
    break;

  case 615: /* nativeType: LPTSTR_  */
#line 1593 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPTSTR); }
#line 7626 "prebuilt\\asmparse.cpp"
    break;

  case 616: /* nativeType: OBJECTREF_  */
#line 1594 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_OBJECTREF);
                                                                PASM->report->warn("Deprecated native type 'objectref'\n"); }
#line 7633 "prebuilt\\asmparse.cpp"
    break;

  case 617: /* nativeType: IUNKNOWN_ iidParamIndex  */
#line 1596 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_IUNKNOWN);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7640 "prebuilt\\asmparse.cpp"
    break;

  case 618: /* nativeType: IDISPATCH_ iidParamIndex  */
#line 1598 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_IDISPATCH);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7647 "prebuilt\\asmparse.cpp"
    break;

  case 619: /* nativeType: STRUCT_  */
#line 1600 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_STRUCT); }
#line 7653 "prebuilt\\asmparse.cpp"
    break;

  case 620: /* nativeType: INTERFACE_ iidParamIndex  */
#line 1601 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_INTF);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7660 "prebuilt\\asmparse.cpp"
    break;

  case 621: /* nativeType: SAFEARRAY_ variantType  */
#line 1603 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[0].int32));
                                                                corEmitInt((yyval.binstr),0);}
#line 7668 "prebuilt\\asmparse.cpp"
    break;

  case 622: /* nativeType: SAFEARRAY_ variantType ',' compQstring  */
#line 1606 "asmparse.y"
                                                                 { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-2].int32));
                                                                corEmitInt((yyval.binstr),(yyvsp[0].binstr)->length()); (yyval.binstr)->append((yyvsp[0].binstr)); }
#line 7676 "prebuilt\\asmparse.cpp"
    break;

  case 623: /* nativeType: INT_  */
#line 1610 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_INT); }
#line 7682 "prebuilt\\asmparse.cpp"
    break;

  case 624: /* nativeType: UNSIGNED_ INT_  */
#line 1611 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_UINT); }
#line 7688 "prebuilt\\asmparse.cpp"
    break;

  case 625: /* nativeType: UINT_  */
#line 1612 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_UINT); }
#line 7694 "prebuilt\\asmparse.cpp"
    break;

  case 626: /* nativeType: NESTED_ STRUCT_  */
#line 1613 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_NESTEDSTRUCT);
                                                                PASM->report->warn("Deprecated native type 'nested struct'\n"); }
#line 7701 "prebuilt\\asmparse.cpp"
    break;

  case 627: /* nativeType: BYVALSTR_  */
#line 1615 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BYVALSTR); }
#line 7707 "prebuilt\\asmparse.cpp"
    break;

  case 628: /* nativeType: ANSI_ BSTR_  */
#line 1616 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ANSIBSTR); }
#line 7713 "prebuilt\\asmparse.cpp"
    break;

  case 629: /* nativeType: TBSTR_  */
#line 1617 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_TBSTR); }
#line 7719 "prebuilt\\asmparse.cpp"
    break;

  case 630: /* nativeType: VARIANT_ BOOL_  */
#line 1618 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VARIANTBOOL); }
#line 7725 "prebuilt\\asmparse.cpp"
    break;

  case 631: /* nativeType: METHOD_  */
#line 1619 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FUNC); }
#line 7731 "prebuilt\\asmparse.cpp"
    break;

  case 632: /* nativeType: AS_ ANY_  */
#line 1620 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ASANY); }
#line 7737 "prebuilt\\asmparse.cpp"
    break;

  case 633: /* nativeType: LPSTRUCT_  */
#line 1621 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPSTRUCT); }
#line 7743 "prebuilt\\asmparse.cpp"
    break;

  case 634: /* nativeType: TYPEDEF_TS  */
#line 1622 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[0].tdd)->m_pbsTypeSpec); }
#line 7749 "prebuilt\\asmparse.cpp"
    break;

  case 635: /* iidParamIndex: %empty  */
#line 1625 "asmparse.y"
                                                              { (yyval.int32) = -1; }
#line 7755 "prebuilt\\asmparse.cpp"
    break;

  case 636: /* iidParamIndex: '(' IIDPARAM_ '=' int32 ')'  */
#line 1626 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32); }
#line 7761 "prebuilt\\asmparse.cpp"
    break;

  case 637: /* variantType: %empty  */
#line 1629 "asmparse.y"
                                                              { (yyval.int32) = VT_EMPTY; }
#line 7767 "prebuilt\\asmparse.cpp"
    break;

  case 638: /* variantType: NULL_  */
#line 1630 "asmparse.y"
                                                              { (yyval.int32) = VT_NULL; }
#line 7773 "prebuilt\\asmparse.cpp"
    break;

  case 639: /* variantType: VARIANT_  */
#line 1631 "asmparse.y"
                                                              { (yyval.int32) = VT_VARIANT; }
#line 7779 "prebuilt\\asmparse.cpp"
    break;

  case 640: /* variantType: CURRENCY_  */
#line 1632 "asmparse.y"
                                                              { (yyval.int32) = VT_CY; }
#line 7785 "prebuilt\\asmparse.cpp"
    break;

  case 641: /* variantType: VOID_  */
#line 1633 "asmparse.y"
                                                              { (yyval.int32) = VT_VOID; }
#line 7791 "prebuilt\\asmparse.cpp"
    break;

  case 642: /* variantType: BOOL_  */
#line 1634 "asmparse.y"
                                                              { (yyval.int32) = VT_BOOL; }
#line 7797 "prebuilt\\asmparse.cpp"
    break;

  case 643: /* variantType: INT8_  */
#line 1635 "asmparse.y"
                                                              { (yyval.int32) = VT_I1; }
#line 7803 "prebuilt\\asmparse.cpp"
    break;

  case 644: /* variantType: INT16_  */
#line 1636 "asmparse.y"
                                                              { (yyval.int32) = VT_I2; }
#line 7809 "prebuilt\\asmparse.cpp"
    break;

  case 645: /* variantType: INT32_  */
#line 1637 "asmparse.y"
                                                              { (yyval.int32) = VT_I4; }
#line 7815 "prebuilt\\asmparse.cpp"
    break;

  case 646: /* variantType: INT64_  */
#line 1638 "asmparse.y"
                                                              { (yyval.int32) = VT_I8; }
#line 7821 "prebuilt\\asmparse.cpp"
    break;

  case 647: /* variantType: FLOAT32_  */
#line 1639 "asmparse.y"
                                                              { (yyval.int32) = VT_R4; }
#line 7827 "prebuilt\\asmparse.cpp"
    break;

  case 648: /* variantType: FLOAT64_  */
#line 1640 "asmparse.y"
                                                              { (yyval.int32) = VT_R8; }
#line 7833 "prebuilt\\asmparse.cpp"
    break;

  case 649: /* variantType: UNSIGNED_ INT8_  */
#line 1641 "asmparse.y"
                                                              { (yyval.int32) = VT_UI1; }
#line 7839 "prebuilt\\asmparse.cpp"
    break;

  case 650: /* variantType: UNSIGNED_ INT16_  */
#line 1642 "asmparse.y"
                                                              { (yyval.int32) = VT_UI2; }
#line 7845 "prebuilt\\asmparse.cpp"
    break;

  case 651: /* variantType: UNSIGNED_ INT32_  */
#line 1643 "asmparse.y"
                                                              { (yyval.int32) = VT_UI4; }
#line 7851 "prebuilt\\asmparse.cpp"
    break;

  case 652: /* variantType: UNSIGNED_ INT64_  */
#line 1644 "asmparse.y"
                                                              { (yyval.int32) = VT_UI8; }
#line 7857 "prebuilt\\asmparse.cpp"
    break;

  case 653: /* variantType: UINT8_  */
#line 1645 "asmparse.y"
                                                              { (yyval.int32) = VT_UI1; }
#line 7863 "prebuilt\\asmparse.cpp"
    break;

  case 654: /* variantType: UINT16_  */
#line 1646 "asmparse.y"
                                                              { (yyval.int32) = VT_UI2; }
#line 7869 "prebuilt\\asmparse.cpp"
    break;

  case 655: /* variantType: UINT32_  */
#line 1647 "asmparse.y"
                                                              { (yyval.int32) = VT_UI4; }
#line 7875 "prebuilt\\asmparse.cpp"
    break;

  case 656: /* variantType: UINT64_  */
#line 1648 "asmparse.y"
                                                              { (yyval.int32) = VT_UI8; }
#line 7881 "prebuilt\\asmparse.cpp"
    break;

  case 657: /* variantType: '*'  */
#line 1649 "asmparse.y"
                                                              { (yyval.int32) = VT_PTR; }
#line 7887 "prebuilt\\asmparse.cpp"
    break;

  case 658: /* variantType: variantType '[' ']'  */
#line 1650 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-2].int32) | VT_ARRAY; }
#line 7893 "prebuilt\\asmparse.cpp"
    break;

  case 659: /* variantType: variantType VECTOR_  */
#line 1651 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32) | VT_VECTOR; }
#line 7899 "prebuilt\\asmparse.cpp"
    break;

  case 660: /* variantType: variantType '&'  */
#line 1652 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32) | VT_BYREF; }
#line 7905 "prebuilt\\asmparse.cpp"
    break;

  case 661: /* variantType: DECIMAL_  */
#line 1653 "asmparse.y"
                                                              { (yyval.int32) = VT_DECIMAL; }
#line 7911 "prebuilt\\asmparse.cpp"
    break;

  case 662: /* variantType: DATE_  */
#line 1654 "asmparse.y"
                                                              { (yyval.int32) = VT_DATE; }
#line 7917 "prebuilt\\asmparse.cpp"
    break;

  case 663: /* variantType: BSTR_  */
#line 1655 "asmparse.y"
                                                              { (yyval.int32) = VT_BSTR; }
#line 7923 "prebuilt\\asmparse.cpp"
    break;

  case 664: /* variantType: LPSTR_  */
#line 1656 "asmparse.y"
                                                              { (yyval.int32) = VT_LPSTR; }
#line 7929 "prebuilt\\asmparse.cpp"
    break;

  case 665: /* variantType: LPWSTR_  */
#line 1657 "asmparse.y"
                                                              { (yyval.int32) = VT_LPWSTR; }
#line 7935 "prebuilt\\asmparse.cpp"
    break;

  case 666: /* variantType: IUNKNOWN_  */
#line 1658 "asmparse.y"
                                                              { (yyval.int32) = VT_UNKNOWN; }
#line 7941 "prebuilt\\asmparse.cpp"
    break;

  case 667: /* variantType: IDISPATCH_  */
#line 1659 "asmparse.y"
                                                              { (yyval.int32) = VT_DISPATCH; }
#line 7947 "prebuilt\\asmparse.cpp"
    break;

  case 668: /* variantType: SAFEARRAY_  */
#line 1660 "asmparse.y"
                                                              { (yyval.int32) = VT_SAFEARRAY; }
#line 7953 "prebuilt\\asmparse.cpp"
    break;

  case 669: /* variantType: INT_  */
#line 1661 "asmparse.y"
                                                              { (yyval.int32) = VT_INT; }
#line 7959 "prebuilt\\asmparse.cpp"
    break;

  case 670: /* variantType: UNSIGNED_ INT_  */
#line 1662 "asmparse.y"
                                                              { (yyval.int32) = VT_UINT; }
#line 7965 "prebuilt\\asmparse.cpp"
    break;

  case 671: /* variantType: UINT_  */
#line 1663 "asmparse.y"
                                                              { (yyval.int32) = VT_UINT; }
#line 7971 "prebuilt\\asmparse.cpp"
    break;

  case 672: /* variantType: ERROR_  */
#line 1664 "asmparse.y"
                                                              { (yyval.int32) = VT_ERROR; }
#line 7977 "prebuilt\\asmparse.cpp"
    break;

  case 673: /* variantType: HRESULT_  */
#line 1665 "asmparse.y"
                                                              { (yyval.int32) = VT_HRESULT; }
#line 7983 "prebuilt\\asmparse.cpp"
    break;

  case 674: /* variantType: CARRAY_  */
#line 1666 "asmparse.y"
                                                              { (yyval.int32) = VT_CARRAY; }
#line 7989 "prebuilt\\asmparse.cpp"
    break;

  case 675: /* variantType: USERDEFINED_  */
#line 1667 "asmparse.y"
                                                              { (yyval.int32) = VT_USERDEFINED; }
#line 7995 "prebuilt\\asmparse.cpp"
    break;

  case 676: /* variantType: RECORD_  */
#line 1668 "asmparse.y"
                                                              { (yyval.int32) = VT_RECORD; }
#line 8001 "prebuilt\\asmparse.cpp"
    break;

  case 677: /* variantType: FILETIME_  */
#line 1669 "asmparse.y"
                                                              { (yyval.int32) = VT_FILETIME; }
#line 8007 "prebuilt\\asmparse.cpp"
    break;

  case 678: /* variantType: BLOB_  */
#line 1670 "asmparse.y"
                                                              { (yyval.int32) = VT_BLOB; }
#line 8013 "prebuilt\\asmparse.cpp"
    break;

  case 679: /* variantType: STREAM_  */
#line 1671 "asmparse.y"
                                                              { (yyval.int32) = VT_STREAM; }
#line 8019 "prebuilt\\asmparse.cpp"
    break;

  case 680: /* variantType: STORAGE_  */
#line 1672 "asmparse.y"
                                                              { (yyval.int32) = VT_STORAGE; }
#line 8025 "prebuilt\\asmparse.cpp"
    break;

  case 681: /* variantType: STREAMED_OBJECT_  */
#line 1673 "asmparse.y"
                                                              { (yyval.int32) = VT_STREAMED_OBJECT; }
#line 8031 "prebuilt\\asmparse.cpp"
    break;

  case 682: /* variantType: STORED_OBJECT_  */
#line 1674 "asmparse.y"
                                                              { (yyval.int32) = VT_STORED_OBJECT; }
#line 8037 "prebuilt\\asmparse.cpp"
    break;

  case 683: /* variantType: BLOB_OBJECT_  */
#line 1675 "asmparse.y"
                                                              { (yyval.int32) = VT_BLOB_OBJECT; }
#line 8043 "prebuilt\\asmparse.cpp"
    break;

  case 684: /* variantType: CF_  */
#line 1676 "asmparse.y"
                                                              { (yyval.int32) = VT_CF; }
#line 8049 "prebuilt\\asmparse.cpp"
    break;

  case 685: /* variantType: CLSID_  */
#line 1677 "asmparse.y"
                                                              { (yyval.int32) = VT_CLSID; }
#line 8055 "prebuilt\\asmparse.cpp"
    break;

  case 686: /* type: CLASS_ className  */
#line 1681 "asmparse.y"
                                                              { if((yyvsp[0].token) == PASM->m_tkSysString)
                                                                {     (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); }
                                                                else if((yyvsp[0].token) == PASM->m_tkSysObject)
                                                                {     (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_OBJECT); }
                                                                else
                                                                 (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CLASS, (yyvsp[0].token)); }
#line 8066 "prebuilt\\asmparse.cpp"
    break;

  case 687: /* type: OBJECT_  */
#line 1687 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_OBJECT); }
#line 8072 "prebuilt\\asmparse.cpp"
    break;

  case 688: /* type: VALUE_ CLASS_ className  */
#line 1688 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, (yyvsp[0].token)); }
#line 8078 "prebuilt\\asmparse.cpp"
    break;

  case 689: /* type: VALUETYPE_ className  */
#line 1689 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, (yyvsp[0].token)); }
#line 8084 "prebuilt\\asmparse.cpp"
    break;

  case 690: /* type: type '[' ']'  */
#line 1690 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 8090 "prebuilt\\asmparse.cpp"
    break;

  case 691: /* type: type '[' bounds1 ']'  */
#line 1691 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeArray(ELEMENT_TYPE_ARRAY, (yyvsp[-3].binstr), (yyvsp[-1].binstr)); }
#line 8096 "prebuilt\\asmparse.cpp"
    break;

  case 692: /* type: type '&'  */
#line 1692 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_BYREF); }
#line 8102 "prebuilt\\asmparse.cpp"
    break;

  case 693: /* type: type '*'  */
#line 1693 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_PTR); }
#line 8108 "prebuilt\\asmparse.cpp"
    break;

  case 694: /* type: type PINNED_  */
#line 1694 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_PINNED); }
#line 8114 "prebuilt\\asmparse.cpp"
    break;

  case 695: /* type: type MODREQ_ '(' typeSpec ')'  */
#line 1695 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_REQD, (yyvsp[-1].token));
                                                                (yyval.binstr)->append((yyvsp[-4].binstr)); }
#line 8121 "prebuilt\\asmparse.cpp"
    break;

  case 696: /* type: type MODOPT_ '(' typeSpec ')'  */
#line 1697 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_OPT, (yyvsp[-1].token));
                                                                (yyval.binstr)->append((yyvsp[-4].binstr)); }
#line 8128 "prebuilt\\asmparse.cpp"
    break;

  case 697: /* type: methodSpec callConv type '*' '(' sigArgs0 ')'  */
#line 1700 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeSig((yyvsp[-5].int32), (yyvsp[-4].binstr), (yyvsp[-1].binstr));
                                                                (yyval.binstr)->insertInt8(ELEMENT_TYPE_FNPTR);
                                                                PASM->delArgNameList(PASM->m_firstArgName);
                                                                PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                                PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                              }
#line 8139 "prebuilt\\asmparse.cpp"
    break;

  case 698: /* type: type '<' tyArgs1 '>'  */
#line 1706 "asmparse.y"
                                                              { if((yyvsp[-1].binstr) == NULL) (yyval.binstr) = (yyvsp[-3].binstr);
                                                                else {
                                                                  (yyval.binstr) = new BinStr();
                                                                  (yyval.binstr)->appendInt8(ELEMENT_TYPE_GENERICINST);
                                                                  (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                  corEmitInt((yyval.binstr), corCountArgs((yyvsp[-1].binstr)));
                                                                  (yyval.binstr)->append((yyvsp[-1].binstr)); delete (yyvsp[-3].binstr); delete (yyvsp[-1].binstr); }}
#line 8151 "prebuilt\\asmparse.cpp"
    break;

  case 699: /* type: '!' '!' int32  */
#line 1713 "asmparse.y"
                                                              { //if(PASM->m_pCurMethod)  {
                                                                //  if(($3 < 0)||((DWORD)$3 >= PASM->m_pCurMethod->m_NumTyPars))
                                                                //    PASM->report->error("Invalid method type parameter '%d'\n",$3);
                                                                  (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_MVAR); corEmitInt((yyval.binstr), (yyvsp[0].int32));
                                                                //} else PASM->report->error("Method type parameter '%d' outside method scope\n",$3);
                                                              }
#line 8162 "prebuilt\\asmparse.cpp"
    break;

  case 700: /* type: '!' int32  */
#line 1719 "asmparse.y"
                                                              { //if(PASM->m_pCurClass)  {
                                                                //  if(($2 < 0)||((DWORD)$2 >= PASM->m_pCurClass->m_NumTyPars))
                                                                //    PASM->report->error("Invalid type parameter '%d'\n",$2);
                                                                  (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_VAR); corEmitInt((yyval.binstr), (yyvsp[0].int32));
                                                                //} else PASM->report->error("Type parameter '%d' outside class scope\n",$2);
                                                              }
#line 8173 "prebuilt\\asmparse.cpp"
    break;

  case 701: /* type: '!' '!' dottedName  */
#line 1725 "asmparse.y"
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
#line 8193 "prebuilt\\asmparse.cpp"
    break;

  case 702: /* type: '!' dottedName  */
#line 1740 "asmparse.y"
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
#line 8213 "prebuilt\\asmparse.cpp"
    break;

  case 703: /* type: TYPEDREF_  */
#line 1755 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_TYPEDBYREF); }
#line 8219 "prebuilt\\asmparse.cpp"
    break;

  case 704: /* type: VOID_  */
#line 1756 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_VOID); }
#line 8225 "prebuilt\\asmparse.cpp"
    break;

  case 705: /* type: NATIVE_ INT_  */
#line 1757 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I); }
#line 8231 "prebuilt\\asmparse.cpp"
    break;

  case 706: /* type: NATIVE_ UNSIGNED_ INT_  */
#line 1758 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U); }
#line 8237 "prebuilt\\asmparse.cpp"
    break;

  case 707: /* type: NATIVE_ UINT_  */
#line 1759 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U); }
#line 8243 "prebuilt\\asmparse.cpp"
    break;

  case 708: /* type: simpleType  */
#line 1760 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 8249 "prebuilt\\asmparse.cpp"
    break;

  case 709: /* type: ELLIPSIS type  */
#line 1761 "asmparse.y"
                                                               { (yyval.binstr) = (yyvsp[0].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SENTINEL); }
#line 8255 "prebuilt\\asmparse.cpp"
    break;

  case 710: /* simpleType: CHAR_  */
#line 1764 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CHAR); }
#line 8261 "prebuilt\\asmparse.cpp"
    break;

  case 711: /* simpleType: STRING_  */
#line 1765 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); }
#line 8267 "prebuilt\\asmparse.cpp"
    break;

  case 712: /* simpleType: BOOL_  */
#line 1766 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_BOOLEAN); }
#line 8273 "prebuilt\\asmparse.cpp"
    break;

  case 713: /* simpleType: INT8_  */
#line 1767 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I1); }
#line 8279 "prebuilt\\asmparse.cpp"
    break;

  case 714: /* simpleType: INT16_  */
#line 1768 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I2); }
#line 8285 "prebuilt\\asmparse.cpp"
    break;

  case 715: /* simpleType: INT32_  */
#line 1769 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I4); }
#line 8291 "prebuilt\\asmparse.cpp"
    break;

  case 716: /* simpleType: INT64_  */
#line 1770 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I8); }
#line 8297 "prebuilt\\asmparse.cpp"
    break;

  case 717: /* simpleType: FLOAT32_  */
#line 1771 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4); }
#line 8303 "prebuilt\\asmparse.cpp"
    break;

  case 718: /* simpleType: FLOAT64_  */
#line 1772 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8); }
#line 8309 "prebuilt\\asmparse.cpp"
    break;

  case 719: /* simpleType: UNSIGNED_ INT8_  */
#line 1773 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1); }
#line 8315 "prebuilt\\asmparse.cpp"
    break;

  case 720: /* simpleType: UNSIGNED_ INT16_  */
#line 1774 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2); }
#line 8321 "prebuilt\\asmparse.cpp"
    break;

  case 721: /* simpleType: UNSIGNED_ INT32_  */
#line 1775 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4); }
#line 8327 "prebuilt\\asmparse.cpp"
    break;

  case 722: /* simpleType: UNSIGNED_ INT64_  */
#line 1776 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8); }
#line 8333 "prebuilt\\asmparse.cpp"
    break;

  case 723: /* simpleType: UINT8_  */
#line 1777 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1); }
#line 8339 "prebuilt\\asmparse.cpp"
    break;

  case 724: /* simpleType: UINT16_  */
#line 1778 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2); }
#line 8345 "prebuilt\\asmparse.cpp"
    break;

  case 725: /* simpleType: UINT32_  */
#line 1779 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4); }
#line 8351 "prebuilt\\asmparse.cpp"
    break;

  case 726: /* simpleType: UINT64_  */
#line 1780 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8); }
#line 8357 "prebuilt\\asmparse.cpp"
    break;

  case 727: /* simpleType: TYPEDEF_TS  */
#line 1781 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[0].tdd)->m_pbsTypeSpec); }
#line 8363 "prebuilt\\asmparse.cpp"
    break;

  case 728: /* bounds1: bound  */
#line 1784 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 8369 "prebuilt\\asmparse.cpp"
    break;

  case 729: /* bounds1: bounds1 ',' bound  */
#line 1785 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyvsp[-2].binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 8375 "prebuilt\\asmparse.cpp"
    break;

  case 730: /* bound: %empty  */
#line 1788 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0x7FFFFFFF); (yyval.binstr)->appendInt32(0x7FFFFFFF);  }
#line 8381 "prebuilt\\asmparse.cpp"
    break;

  case 731: /* bound: ELLIPSIS  */
#line 1789 "asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0x7FFFFFFF); (yyval.binstr)->appendInt32(0x7FFFFFFF);  }
#line 8387 "prebuilt\\asmparse.cpp"
    break;

  case 732: /* bound: int32  */
#line 1790 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0); (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 8393 "prebuilt\\asmparse.cpp"
    break;

  case 733: /* bound: int32 ELLIPSIS int32  */
#line 1791 "asmparse.y"
                                                               { FAIL_UNLESS((yyvsp[-2].int32) <= (yyvsp[0].int32), ("lower bound %d must be <= upper bound %d\n", (yyvsp[-2].int32), (yyvsp[0].int32)));
                                                                if ((yyvsp[-2].int32) > (yyvsp[0].int32)) { YYERROR; };
                                                                (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[-2].int32)); (yyval.binstr)->appendInt32((yyvsp[0].int32)-(yyvsp[-2].int32)+1); }
#line 8401 "prebuilt\\asmparse.cpp"
    break;

  case 734: /* bound: int32 ELLIPSIS  */
#line 1794 "asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[-1].int32)); (yyval.binstr)->appendInt32(0x7FFFFFFF); }
#line 8407 "prebuilt\\asmparse.cpp"
    break;

  case 735: /* secDecl: _PERMISSION secAction typeSpec '(' nameValPairs ')'  */
#line 1799 "asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-4].secAct), (yyvsp[-3].token), (yyvsp[-1].pair)); }
#line 8413 "prebuilt\\asmparse.cpp"
    break;

  case 736: /* secDecl: _PERMISSION secAction typeSpec '=' '{' customBlobDescr '}'  */
#line 1801 "asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-5].secAct), (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 8419 "prebuilt\\asmparse.cpp"
    break;

  case 737: /* secDecl: _PERMISSION secAction typeSpec  */
#line 1802 "asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-1].secAct), (yyvsp[0].token), (NVPair *)NULL); }
#line 8425 "prebuilt\\asmparse.cpp"
    break;

  case 738: /* secDecl: psetHead bytes ')'  */
#line 1803 "asmparse.y"
                                                              { PASM->AddPermissionSetDecl((yyvsp[-2].secAct), (yyvsp[-1].binstr)); }
#line 8431 "prebuilt\\asmparse.cpp"
    break;

  case 739: /* secDecl: _PERMISSIONSET secAction compQstring  */
#line 1805 "asmparse.y"
                                                              { PASM->AddPermissionSetDecl((yyvsp[-1].secAct),BinStrToUnicode((yyvsp[0].binstr),true));}
#line 8437 "prebuilt\\asmparse.cpp"
    break;

  case 740: /* secDecl: _PERMISSIONSET secAction '=' '{' secAttrSetBlob '}'  */
#line 1807 "asmparse.y"
                                                              { BinStr* ret = new BinStr();
                                                                ret->insertInt8('.');
                                                                corEmitInt(ret, nSecAttrBlobs);
                                                                ret->append((yyvsp[-1].binstr));
                                                                PASM->AddPermissionSetDecl((yyvsp[-4].secAct),ret);
                                                                nSecAttrBlobs = 0; }
#line 8448 "prebuilt\\asmparse.cpp"
    break;

  case 741: /* secAttrSetBlob: %empty  */
#line 1815 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); nSecAttrBlobs = 0;}
#line 8454 "prebuilt\\asmparse.cpp"
    break;

  case 742: /* secAttrSetBlob: secAttrBlob  */
#line 1816 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); nSecAttrBlobs = 1; }
#line 8460 "prebuilt\\asmparse.cpp"
    break;

  case 743: /* secAttrSetBlob: secAttrBlob ',' secAttrSetBlob  */
#line 1817 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); nSecAttrBlobs++; }
#line 8466 "prebuilt\\asmparse.cpp"
    break;

  case 744: /* secAttrBlob: typeSpec '=' '{' customBlobNVPairs '}'  */
#line 1821 "asmparse.y"
                                                              { (yyval.binstr) = PASM->EncodeSecAttr(PASM->ReflectionNotation((yyvsp[-4].token)),(yyvsp[-1].binstr),nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
#line 8473 "prebuilt\\asmparse.cpp"
    break;

  case 745: /* secAttrBlob: CLASS_ SQSTRING '=' '{' customBlobNVPairs '}'  */
#line 1824 "asmparse.y"
                                                              { (yyval.binstr) = PASM->EncodeSecAttr((yyvsp[-4].string),(yyvsp[-1].binstr),nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
#line 8480 "prebuilt\\asmparse.cpp"
    break;

  case 746: /* psetHead: _PERMISSIONSET secAction '=' '('  */
#line 1828 "asmparse.y"
                                                              { (yyval.secAct) = (yyvsp[-2].secAct); bParsingByteArray = TRUE; }
#line 8486 "prebuilt\\asmparse.cpp"
    break;

  case 747: /* psetHead: _PERMISSIONSET secAction BYTEARRAY_ '('  */
#line 1830 "asmparse.y"
                                                              { (yyval.secAct) = (yyvsp[-2].secAct); bParsingByteArray = TRUE; }
#line 8492 "prebuilt\\asmparse.cpp"
    break;

  case 748: /* nameValPairs: nameValPair  */
#line 1833 "asmparse.y"
                                                              { (yyval.pair) = (yyvsp[0].pair); }
#line 8498 "prebuilt\\asmparse.cpp"
    break;

  case 749: /* nameValPairs: nameValPair ',' nameValPairs  */
#line 1834 "asmparse.y"
                                                              { (yyval.pair) = (yyvsp[-2].pair)->Concat((yyvsp[0].pair)); }
#line 8504 "prebuilt\\asmparse.cpp"
    break;

  case 750: /* nameValPair: compQstring '=' caValue  */
#line 1837 "asmparse.y"
                                                              { (yyvsp[-2].binstr)->appendInt8(0); (yyval.pair) = new NVPair((yyvsp[-2].binstr), (yyvsp[0].binstr)); }
#line 8510 "prebuilt\\asmparse.cpp"
    break;

  case 751: /* truefalse: TRUE_  */
#line 1840 "asmparse.y"
                                                              { (yyval.int32) = 1; }
#line 8516 "prebuilt\\asmparse.cpp"
    break;

  case 752: /* truefalse: FALSE_  */
#line 1841 "asmparse.y"
                                                              { (yyval.int32) = 0; }
#line 8522 "prebuilt\\asmparse.cpp"
    break;

  case 753: /* caValue: truefalse  */
#line 1844 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_BOOLEAN);
                                                                (yyval.binstr)->appendInt8((yyvsp[0].int32)); }
#line 8530 "prebuilt\\asmparse.cpp"
    break;

  case 754: /* caValue: int32  */
#line 1847 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_I4);
                                                                (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 8538 "prebuilt\\asmparse.cpp"
    break;

  case 755: /* caValue: INT32_ '(' int32 ')'  */
#line 1850 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_I4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8546 "prebuilt\\asmparse.cpp"
    break;

  case 756: /* caValue: compQstring  */
#line 1853 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_STRING);
                                                                (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr);
                                                                (yyval.binstr)->appendInt8(0); }
#line 8555 "prebuilt\\asmparse.cpp"
    break;

  case 757: /* caValue: className '(' INT8_ ':' int32 ')'  */
#line 1857 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(1);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8566 "prebuilt\\asmparse.cpp"
    break;

  case 758: /* caValue: className '(' INT16_ ':' int32 ')'  */
#line 1863 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(2);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8577 "prebuilt\\asmparse.cpp"
    break;

  case 759: /* caValue: className '(' INT32_ ':' int32 ')'  */
#line 1869 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8588 "prebuilt\\asmparse.cpp"
    break;

  case 760: /* caValue: className '(' int32 ')'  */
#line 1875 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-3].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8599 "prebuilt\\asmparse.cpp"
    break;

  case 761: /* secAction: REQUEST_  */
#line 1883 "asmparse.y"
                                                              { (yyval.secAct) = dclRequest; }
#line 8605 "prebuilt\\asmparse.cpp"
    break;

  case 762: /* secAction: DEMAND_  */
#line 1884 "asmparse.y"
                                                              { (yyval.secAct) = dclDemand; }
#line 8611 "prebuilt\\asmparse.cpp"
    break;

  case 763: /* secAction: ASSERT_  */
#line 1885 "asmparse.y"
                                                              { (yyval.secAct) = dclAssert; }
#line 8617 "prebuilt\\asmparse.cpp"
    break;

  case 764: /* secAction: DENY_  */
#line 1886 "asmparse.y"
                                                              { (yyval.secAct) = dclDeny; }
#line 8623 "prebuilt\\asmparse.cpp"
    break;

  case 765: /* secAction: PERMITONLY_  */
#line 1887 "asmparse.y"
                                                              { (yyval.secAct) = dclPermitOnly; }
#line 8629 "prebuilt\\asmparse.cpp"
    break;

  case 766: /* secAction: LINKCHECK_  */
#line 1888 "asmparse.y"
                                                              { (yyval.secAct) = dclLinktimeCheck; }
#line 8635 "prebuilt\\asmparse.cpp"
    break;

  case 767: /* secAction: INHERITCHECK_  */
#line 1889 "asmparse.y"
                                                              { (yyval.secAct) = dclInheritanceCheck; }
#line 8641 "prebuilt\\asmparse.cpp"
    break;

  case 768: /* secAction: REQMIN_  */
#line 1890 "asmparse.y"
                                                              { (yyval.secAct) = dclRequestMinimum; }
#line 8647 "prebuilt\\asmparse.cpp"
    break;

  case 769: /* secAction: REQOPT_  */
#line 1891 "asmparse.y"
                                                              { (yyval.secAct) = dclRequestOptional; }
#line 8653 "prebuilt\\asmparse.cpp"
    break;

  case 770: /* secAction: REQREFUSE_  */
#line 1892 "asmparse.y"
                                                              { (yyval.secAct) = dclRequestRefuse; }
#line 8659 "prebuilt\\asmparse.cpp"
    break;

  case 771: /* secAction: PREJITGRANT_  */
#line 1893 "asmparse.y"
                                                              { (yyval.secAct) = dclPrejitGrant; }
#line 8665 "prebuilt\\asmparse.cpp"
    break;

  case 772: /* secAction: PREJITDENY_  */
#line 1894 "asmparse.y"
                                                              { (yyval.secAct) = dclPrejitDenied; }
#line 8671 "prebuilt\\asmparse.cpp"
    break;

  case 773: /* secAction: NONCASDEMAND_  */
#line 1895 "asmparse.y"
                                                              { (yyval.secAct) = dclNonCasDemand; }
#line 8677 "prebuilt\\asmparse.cpp"
    break;

  case 774: /* secAction: NONCASLINKDEMAND_  */
#line 1896 "asmparse.y"
                                                              { (yyval.secAct) = dclNonCasLinkDemand; }
#line 8683 "prebuilt\\asmparse.cpp"
    break;

  case 775: /* secAction: NONCASINHERITANCE_  */
#line 1897 "asmparse.y"
                                                              { (yyval.secAct) = dclNonCasInheritance; }
#line 8689 "prebuilt\\asmparse.cpp"
    break;

  case 776: /* esHead: _LINE  */
#line 1901 "asmparse.y"
                                                              { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = FALSE; }
#line 8695 "prebuilt\\asmparse.cpp"
    break;

  case 777: /* esHead: P_LINE  */
#line 1902 "asmparse.y"
                                                              { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = TRUE; }
#line 8701 "prebuilt\\asmparse.cpp"
    break;

  case 778: /* extSourceSpec: esHead int32 SQSTRING  */
#line 1905 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-1].int32);
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8709 "prebuilt\\asmparse.cpp"
    break;

  case 779: /* extSourceSpec: esHead int32  */
#line 1908 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[0].int32);
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1); }
#line 8716 "prebuilt\\asmparse.cpp"
    break;

  case 780: /* extSourceSpec: esHead int32 ':' int32 SQSTRING  */
#line 1910 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-3].int32);
                                                                PENV->nExtCol=(yyvsp[-1].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8724 "prebuilt\\asmparse.cpp"
    break;

  case 781: /* extSourceSpec: esHead int32 ':' int32  */
#line 1913 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-2].int32);
                                                                PENV->nExtCol=(yyvsp[0].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);}
#line 8731 "prebuilt\\asmparse.cpp"
    break;

  case 782: /* extSourceSpec: esHead int32 ':' int32 ',' int32 SQSTRING  */
#line 1916 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-5].int32);
                                                                PENV->nExtCol=(yyvsp[-3].int32); PENV->nExtColEnd = (yyvsp[-1].int32);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8739 "prebuilt\\asmparse.cpp"
    break;

  case 783: /* extSourceSpec: esHead int32 ':' int32 ',' int32  */
#line 1920 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-4].int32);
                                                                PENV->nExtCol=(yyvsp[-2].int32); PENV->nExtColEnd = (yyvsp[0].int32); }
#line 8746 "prebuilt\\asmparse.cpp"
    break;

  case 784: /* extSourceSpec: esHead int32 ',' int32 ':' int32 SQSTRING  */
#line 1923 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-5].int32); PENV->nExtLineEnd = (yyvsp[-3].int32);
                                                                PENV->nExtCol=(yyvsp[-1].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8754 "prebuilt\\asmparse.cpp"
    break;

  case 785: /* extSourceSpec: esHead int32 ',' int32 ':' int32  */
#line 1927 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-4].int32); PENV->nExtLineEnd = (yyvsp[-2].int32);
                                                                PENV->nExtCol=(yyvsp[0].int32); PENV->nExtColEnd = static_cast<unsigned>(-1); }
#line 8761 "prebuilt\\asmparse.cpp"
    break;

  case 786: /* extSourceSpec: esHead int32 ',' int32 ':' int32 ',' int32 SQSTRING  */
#line 1930 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-7].int32); PENV->nExtLineEnd = (yyvsp[-5].int32);
                                                                PENV->nExtCol=(yyvsp[-3].int32); PENV->nExtColEnd = (yyvsp[-1].int32);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8769 "prebuilt\\asmparse.cpp"
    break;

  case 787: /* extSourceSpec: esHead int32 ',' int32 ':' int32 ',' int32  */
#line 1934 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-6].int32); PENV->nExtLineEnd = (yyvsp[-4].int32);
                                                                PENV->nExtCol=(yyvsp[-2].int32); PENV->nExtColEnd = (yyvsp[0].int32); }
#line 8776 "prebuilt\\asmparse.cpp"
    break;

  case 788: /* extSourceSpec: esHead int32 QSTRING  */
#line 1936 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-1].int32) - 1;
                                                                PENV->nExtCol = 0; PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].binstr));}
#line 8784 "prebuilt\\asmparse.cpp"
    break;

  case 789: /* fileDecl: _FILE fileAttr dottedName fileEntry hashHead bytes ')' fileEntry  */
#line 1943 "asmparse.y"
                                                              { PASMM->AddFile((yyvsp[-5].string), (yyvsp[-6].fileAttr)|(yyvsp[-4].fileAttr)|(yyvsp[0].fileAttr), (yyvsp[-2].binstr)); }
#line 8790 "prebuilt\\asmparse.cpp"
    break;

  case 790: /* fileDecl: _FILE fileAttr dottedName fileEntry  */
#line 1944 "asmparse.y"
                                                              { PASMM->AddFile((yyvsp[-1].string), (yyvsp[-2].fileAttr)|(yyvsp[0].fileAttr), NULL); }
#line 8796 "prebuilt\\asmparse.cpp"
    break;

  case 791: /* fileAttr: %empty  */
#line 1947 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0; }
#line 8802 "prebuilt\\asmparse.cpp"
    break;

  case 792: /* fileAttr: fileAttr NOMETADATA_  */
#line 1948 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) ((yyvsp[-1].fileAttr) | ffContainsNoMetaData); }
#line 8808 "prebuilt\\asmparse.cpp"
    break;

  case 793: /* fileEntry: %empty  */
#line 1951 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0; }
#line 8814 "prebuilt\\asmparse.cpp"
    break;

  case 794: /* fileEntry: _ENTRYPOINT  */
#line 1952 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0x80000000; }
#line 8820 "prebuilt\\asmparse.cpp"
    break;

  case 795: /* hashHead: _HASH '=' '('  */
#line 1955 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8826 "prebuilt\\asmparse.cpp"
    break;

  case 796: /* assemblyHead: _ASSEMBLY asmAttr dottedName  */
#line 1958 "asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[0].string), NULL, (DWORD)(yyvsp[-1].asmAttr), FALSE); }
#line 8832 "prebuilt\\asmparse.cpp"
    break;

  case 797: /* asmAttr: %empty  */
#line 1961 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) 0; }
#line 8838 "prebuilt\\asmparse.cpp"
    break;

  case 798: /* asmAttr: asmAttr RETARGETABLE_  */
#line 1962 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afRetargetable); }
#line 8844 "prebuilt\\asmparse.cpp"
    break;

  case 799: /* asmAttr: asmAttr WINDOWSRUNTIME_  */
#line 1963 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afContentType_WindowsRuntime); }
#line 8850 "prebuilt\\asmparse.cpp"
    break;

  case 800: /* asmAttr: asmAttr NOPLATFORM_  */
#line 1964 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afPA_NoPlatform); }
#line 8856 "prebuilt\\asmparse.cpp"
    break;

  case 801: /* asmAttr: asmAttr LEGACY_ LIBRARY_  */
#line 1965 "asmparse.y"
                                                              { (yyval.asmAttr) = (yyvsp[-2].asmAttr); }
#line 8862 "prebuilt\\asmparse.cpp"
    break;

  case 802: /* asmAttr: asmAttr CIL_  */
#line 1966 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_MSIL); }
#line 8868 "prebuilt\\asmparse.cpp"
    break;

  case 803: /* asmAttr: asmAttr X86_  */
#line 1967 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_x86); }
#line 8874 "prebuilt\\asmparse.cpp"
    break;

  case 804: /* asmAttr: asmAttr AMD64_  */
#line 1968 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_AMD64); }
#line 8880 "prebuilt\\asmparse.cpp"
    break;

  case 805: /* asmAttr: asmAttr ARM_  */
#line 1969 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_ARM); }
#line 8886 "prebuilt\\asmparse.cpp"
    break;

  case 806: /* asmAttr: asmAttr ARM64_  */
#line 1970 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_ARM64); }
#line 8892 "prebuilt\\asmparse.cpp"
    break;

  case 809: /* assemblyDecl: _HASH ALGORITHM_ int32  */
#line 1977 "asmparse.y"
                                                              { PASMM->SetAssemblyHashAlg((yyvsp[0].int32)); }
#line 8898 "prebuilt\\asmparse.cpp"
    break;

  case 812: /* intOrWildcard: int32  */
#line 1982 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 8904 "prebuilt\\asmparse.cpp"
    break;

  case 813: /* intOrWildcard: '*'  */
#line 1983 "asmparse.y"
                                                              { (yyval.int32) = 0xFFFF; }
#line 8910 "prebuilt\\asmparse.cpp"
    break;

  case 814: /* asmOrRefDecl: publicKeyHead bytes ')'  */
#line 1986 "asmparse.y"
                                                              { PASMM->SetAssemblyPublicKey((yyvsp[-1].binstr)); }
#line 8916 "prebuilt\\asmparse.cpp"
    break;

  case 815: /* asmOrRefDecl: _VER intOrWildcard ':' intOrWildcard ':' intOrWildcard ':' intOrWildcard  */
#line 1988 "asmparse.y"
                                                              { PASMM->SetAssemblyVer((USHORT)(yyvsp[-6].int32), (USHORT)(yyvsp[-4].int32), (USHORT)(yyvsp[-2].int32), (USHORT)(yyvsp[0].int32)); }
#line 8922 "prebuilt\\asmparse.cpp"
    break;

  case 816: /* asmOrRefDecl: _LOCALE compQstring  */
#line 1989 "asmparse.y"
                                                              { (yyvsp[0].binstr)->appendInt8(0); PASMM->SetAssemblyLocale((yyvsp[0].binstr),TRUE); }
#line 8928 "prebuilt\\asmparse.cpp"
    break;

  case 817: /* asmOrRefDecl: localeHead bytes ')'  */
#line 1990 "asmparse.y"
                                                              { PASMM->SetAssemblyLocale((yyvsp[-1].binstr),FALSE); }
#line 8934 "prebuilt\\asmparse.cpp"
    break;

  case 820: /* publicKeyHead: _PUBLICKEY '=' '('  */
#line 1995 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8940 "prebuilt\\asmparse.cpp"
    break;

  case 821: /* publicKeyTokenHead: _PUBLICKEYTOKEN '=' '('  */
#line 1998 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8946 "prebuilt\\asmparse.cpp"
    break;

  case 822: /* localeHead: _LOCALE '=' '('  */
#line 2001 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8952 "prebuilt\\asmparse.cpp"
    break;

  case 823: /* assemblyRefHead: _ASSEMBLY EXTERN_ asmAttr dottedName  */
#line 2005 "asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[0].string), NULL, (yyvsp[-1].asmAttr), TRUE); }
#line 8958 "prebuilt\\asmparse.cpp"
    break;

  case 824: /* assemblyRefHead: _ASSEMBLY EXTERN_ asmAttr dottedName AS_ dottedName  */
#line 2007 "asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[-2].string), (yyvsp[0].string), (yyvsp[-3].asmAttr), TRUE); }
#line 8964 "prebuilt\\asmparse.cpp"
    break;

  case 827: /* assemblyRefDecl: hashHead bytes ')'  */
#line 2014 "asmparse.y"
                                                              { PASMM->SetAssemblyHashBlob((yyvsp[-1].binstr)); }
#line 8970 "prebuilt\\asmparse.cpp"
    break;

  case 829: /* assemblyRefDecl: publicKeyTokenHead bytes ')'  */
#line 2016 "asmparse.y"
                                                              { PASMM->SetAssemblyPublicKeyToken((yyvsp[-1].binstr)); }
#line 8976 "prebuilt\\asmparse.cpp"
    break;

  case 830: /* assemblyRefDecl: AUTO_  */
#line 2017 "asmparse.y"
                                                              { PASMM->SetAssemblyAutodetect(); }
#line 8982 "prebuilt\\asmparse.cpp"
    break;

  case 831: /* exptypeHead: _CLASS EXTERN_ exptAttr dottedName  */
#line 2020 "asmparse.y"
                                                              { PASMM->StartComType((yyvsp[0].string), (yyvsp[-1].exptAttr));}
#line 8988 "prebuilt\\asmparse.cpp"
    break;

  case 832: /* exportHead: _EXPORT exptAttr dottedName  */
#line 2023 "asmparse.y"
                                                                              { PASMM->StartComType((yyvsp[0].string), (yyvsp[-1].exptAttr)); }
#line 8994 "prebuilt\\asmparse.cpp"
    break;

  case 833: /* exptAttr: %empty  */
#line 2026 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) 0; }
#line 9000 "prebuilt\\asmparse.cpp"
    break;

  case 834: /* exptAttr: exptAttr PRIVATE_  */
#line 2027 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdNotPublic); }
#line 9006 "prebuilt\\asmparse.cpp"
    break;

  case 835: /* exptAttr: exptAttr PUBLIC_  */
#line 2028 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdPublic); }
#line 9012 "prebuilt\\asmparse.cpp"
    break;

  case 836: /* exptAttr: exptAttr FORWARDER_  */
#line 2029 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdForwarder); }
#line 9018 "prebuilt\\asmparse.cpp"
    break;

  case 837: /* exptAttr: exptAttr NESTED_ PUBLIC_  */
#line 2030 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedPublic); }
#line 9024 "prebuilt\\asmparse.cpp"
    break;

  case 838: /* exptAttr: exptAttr NESTED_ PRIVATE_  */
#line 2031 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedPrivate); }
#line 9030 "prebuilt\\asmparse.cpp"
    break;

  case 839: /* exptAttr: exptAttr NESTED_ FAMILY_  */
#line 2032 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamily); }
#line 9036 "prebuilt\\asmparse.cpp"
    break;

  case 840: /* exptAttr: exptAttr NESTED_ ASSEMBLY_  */
#line 2033 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedAssembly); }
#line 9042 "prebuilt\\asmparse.cpp"
    break;

  case 841: /* exptAttr: exptAttr NESTED_ FAMANDASSEM_  */
#line 2034 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamANDAssem); }
#line 9048 "prebuilt\\asmparse.cpp"
    break;

  case 842: /* exptAttr: exptAttr NESTED_ FAMORASSEM_  */
#line 2035 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamORAssem); }
#line 9054 "prebuilt\\asmparse.cpp"
    break;

  case 845: /* exptypeDecl: _FILE dottedName  */
#line 2042 "asmparse.y"
                                                              { PASMM->SetComTypeFile((yyvsp[0].string)); }
#line 9060 "prebuilt\\asmparse.cpp"
    break;

  case 846: /* exptypeDecl: _CLASS EXTERN_ slashedName  */
#line 2043 "asmparse.y"
                                                               { PASMM->SetComTypeComType((yyvsp[0].string)); }
#line 9066 "prebuilt\\asmparse.cpp"
    break;

  case 847: /* exptypeDecl: _ASSEMBLY EXTERN_ dottedName  */
#line 2044 "asmparse.y"
                                                              { PASMM->SetComTypeAsmRef((yyvsp[0].string)); }
#line 9072 "prebuilt\\asmparse.cpp"
    break;

  case 848: /* exptypeDecl: MDTOKEN_ '(' int32 ')'  */
#line 2045 "asmparse.y"
                                                              { if(!PASMM->SetComTypeImplementationTok((yyvsp[-1].int32)))
                                                                  PASM->report->error("Invalid implementation of exported type\n"); }
#line 9079 "prebuilt\\asmparse.cpp"
    break;

  case 849: /* exptypeDecl: _CLASS int32  */
#line 2047 "asmparse.y"
                                                              { if(!PASMM->SetComTypeClassTok((yyvsp[0].int32)))
                                                                  PASM->report->error("Invalid TypeDefID of exported type\n"); }
#line 9086 "prebuilt\\asmparse.cpp"
    break;

  case 852: /* manifestResHead: _MRESOURCE manresAttr dottedName  */
#line 2053 "asmparse.y"
                                                              { PASMM->StartManifestRes((yyvsp[0].string), (yyvsp[0].string), (yyvsp[-1].manresAttr)); }
#line 9092 "prebuilt\\asmparse.cpp"
    break;

  case 853: /* manifestResHead: _MRESOURCE manresAttr dottedName AS_ dottedName  */
#line 2055 "asmparse.y"
                                                              { PASMM->StartManifestRes((yyvsp[-2].string), (yyvsp[0].string), (yyvsp[-3].manresAttr)); }
#line 9098 "prebuilt\\asmparse.cpp"
    break;

  case 854: /* manresAttr: %empty  */
#line 2058 "asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) 0; }
#line 9104 "prebuilt\\asmparse.cpp"
    break;

  case 855: /* manresAttr: manresAttr PUBLIC_  */
#line 2059 "asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) ((yyvsp[-1].manresAttr) | mrPublic); }
#line 9110 "prebuilt\\asmparse.cpp"
    break;

  case 856: /* manresAttr: manresAttr PRIVATE_  */
#line 2060 "asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) ((yyvsp[-1].manresAttr) | mrPrivate); }
#line 9116 "prebuilt\\asmparse.cpp"
    break;

  case 859: /* manifestResDecl: _FILE dottedName AT_ int32  */
#line 2067 "asmparse.y"
                                                              { PASMM->SetManifestResFile((yyvsp[-2].string), (ULONG)(yyvsp[0].int32)); }
#line 9122 "prebuilt\\asmparse.cpp"
    break;

  case 860: /* manifestResDecl: _ASSEMBLY EXTERN_ dottedName  */
#line 2068 "asmparse.y"
                                                              { PASMM->SetManifestResAsmRef((yyvsp[0].string)); }
#line 9128 "prebuilt\\asmparse.cpp"
    break;


#line 9132 "prebuilt\\asmparse.cpp"

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

#line 2073 "asmparse.y"


#include "grammar_after.cpp"
