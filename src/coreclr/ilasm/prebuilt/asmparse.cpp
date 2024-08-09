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
    STRICT_ = 354,                 /* STRICT_  */
    RETARGETABLE_ = 355,           /* RETARGETABLE_  */
    WINDOWSRUNTIME_ = 356,         /* WINDOWSRUNTIME_  */
    NOPLATFORM_ = 357,             /* NOPLATFORM_  */
    METHOD_ = 358,                 /* METHOD_  */
    FIELD_ = 359,                  /* FIELD_  */
    PINNED_ = 360,                 /* PINNED_  */
    MODREQ_ = 361,                 /* MODREQ_  */
    MODOPT_ = 362,                 /* MODOPT_  */
    SERIALIZABLE_ = 363,           /* SERIALIZABLE_  */
    PROPERTY_ = 364,               /* PROPERTY_  */
    TYPE_ = 365,                   /* TYPE_  */
    ASSEMBLY_ = 366,               /* ASSEMBLY_  */
    FAMANDASSEM_ = 367,            /* FAMANDASSEM_  */
    FAMORASSEM_ = 368,             /* FAMORASSEM_  */
    PRIVATESCOPE_ = 369,           /* PRIVATESCOPE_  */
    HIDEBYSIG_ = 370,              /* HIDEBYSIG_  */
    NEWSLOT_ = 371,                /* NEWSLOT_  */
    RTSPECIALNAME_ = 372,          /* RTSPECIALNAME_  */
    PINVOKEIMPL_ = 373,            /* PINVOKEIMPL_  */
    _CTOR = 374,                   /* _CTOR  */
    _CCTOR = 375,                  /* _CCTOR  */
    LITERAL_ = 376,                /* LITERAL_  */
    NOTSERIALIZED_ = 377,          /* NOTSERIALIZED_  */
    INITONLY_ = 378,               /* INITONLY_  */
    REQSECOBJ_ = 379,              /* REQSECOBJ_  */
    CIL_ = 380,                    /* CIL_  */
    OPTIL_ = 381,                  /* OPTIL_  */
    MANAGED_ = 382,                /* MANAGED_  */
    FORWARDREF_ = 383,             /* FORWARDREF_  */
    PRESERVESIG_ = 384,            /* PRESERVESIG_  */
    RUNTIME_ = 385,                /* RUNTIME_  */
    INTERNALCALL_ = 386,           /* INTERNALCALL_  */
    _IMPORT = 387,                 /* _IMPORT  */
    NOMANGLE_ = 388,               /* NOMANGLE_  */
    LASTERR_ = 389,                /* LASTERR_  */
    WINAPI_ = 390,                 /* WINAPI_  */
    AS_ = 391,                     /* AS_  */
    BESTFIT_ = 392,                /* BESTFIT_  */
    ON_ = 393,                     /* ON_  */
    OFF_ = 394,                    /* OFF_  */
    CHARMAPERROR_ = 395,           /* CHARMAPERROR_  */
    INSTR_NONE = 396,              /* INSTR_NONE  */
    INSTR_VAR = 397,               /* INSTR_VAR  */
    INSTR_I = 398,                 /* INSTR_I  */
    INSTR_I8 = 399,                /* INSTR_I8  */
    INSTR_R = 400,                 /* INSTR_R  */
    INSTR_BRTARGET = 401,          /* INSTR_BRTARGET  */
    INSTR_METHOD = 402,            /* INSTR_METHOD  */
    INSTR_FIELD = 403,             /* INSTR_FIELD  */
    INSTR_TYPE = 404,              /* INSTR_TYPE  */
    INSTR_STRING = 405,            /* INSTR_STRING  */
    INSTR_SIG = 406,               /* INSTR_SIG  */
    INSTR_TOK = 407,               /* INSTR_TOK  */
    INSTR_SWITCH = 408,            /* INSTR_SWITCH  */
    _CLASS = 409,                  /* _CLASS  */
    _NAMESPACE = 410,              /* _NAMESPACE  */
    _METHOD = 411,                 /* _METHOD  */
    _FIELD = 412,                  /* _FIELD  */
    _DATA = 413,                   /* _DATA  */
    _THIS = 414,                   /* _THIS  */
    _BASE = 415,                   /* _BASE  */
    _NESTER = 416,                 /* _NESTER  */
    _EMITBYTE = 417,               /* _EMITBYTE  */
    _TRY = 418,                    /* _TRY  */
    _MAXSTACK = 419,               /* _MAXSTACK  */
    _LOCALS = 420,                 /* _LOCALS  */
    _ENTRYPOINT = 421,             /* _ENTRYPOINT  */
    _ZEROINIT = 422,               /* _ZEROINIT  */
    _EVENT = 423,                  /* _EVENT  */
    _ADDON = 424,                  /* _ADDON  */
    _REMOVEON = 425,               /* _REMOVEON  */
    _FIRE = 426,                   /* _FIRE  */
    _OTHER = 427,                  /* _OTHER  */
    _PROPERTY = 428,               /* _PROPERTY  */
    _SET = 429,                    /* _SET  */
    _GET = 430,                    /* _GET  */
    _PERMISSION = 431,             /* _PERMISSION  */
    _PERMISSIONSET = 432,          /* _PERMISSIONSET  */
    REQUEST_ = 433,                /* REQUEST_  */
    DEMAND_ = 434,                 /* DEMAND_  */
    ASSERT_ = 435,                 /* ASSERT_  */
    DENY_ = 436,                   /* DENY_  */
    PERMITONLY_ = 437,             /* PERMITONLY_  */
    LINKCHECK_ = 438,              /* LINKCHECK_  */
    INHERITCHECK_ = 439,           /* INHERITCHECK_  */
    REQMIN_ = 440,                 /* REQMIN_  */
    REQOPT_ = 441,                 /* REQOPT_  */
    REQREFUSE_ = 442,              /* REQREFUSE_  */
    PREJITGRANT_ = 443,            /* PREJITGRANT_  */
    PREJITDENY_ = 444,             /* PREJITDENY_  */
    NONCASDEMAND_ = 445,           /* NONCASDEMAND_  */
    NONCASLINKDEMAND_ = 446,       /* NONCASLINKDEMAND_  */
    NONCASINHERITANCE_ = 447,      /* NONCASINHERITANCE_  */
    _LINE = 448,                   /* _LINE  */
    P_LINE = 449,                  /* P_LINE  */
    _LANGUAGE = 450,               /* _LANGUAGE  */
    _CUSTOM = 451,                 /* _CUSTOM  */
    INIT_ = 452,                   /* INIT_  */
    _SIZE = 453,                   /* _SIZE  */
    _PACK = 454,                   /* _PACK  */
    _VTABLE = 455,                 /* _VTABLE  */
    _VTFIXUP = 456,                /* _VTFIXUP  */
    FROMUNMANAGED_ = 457,          /* FROMUNMANAGED_  */
    CALLMOSTDERIVED_ = 458,        /* CALLMOSTDERIVED_  */
    _VTENTRY = 459,                /* _VTENTRY  */
    RETAINAPPDOMAIN_ = 460,        /* RETAINAPPDOMAIN_  */
    _FILE = 461,                   /* _FILE  */
    NOMETADATA_ = 462,             /* NOMETADATA_  */
    _HASH = 463,                   /* _HASH  */
    _ASSEMBLY = 464,               /* _ASSEMBLY  */
    _PUBLICKEY = 465,              /* _PUBLICKEY  */
    _PUBLICKEYTOKEN = 466,         /* _PUBLICKEYTOKEN  */
    ALGORITHM_ = 467,              /* ALGORITHM_  */
    _VER = 468,                    /* _VER  */
    _LOCALE = 469,                 /* _LOCALE  */
    EXTERN_ = 470,                 /* EXTERN_  */
    _MRESOURCE = 471,              /* _MRESOURCE  */
    _MODULE = 472,                 /* _MODULE  */
    _EXPORT = 473,                 /* _EXPORT  */
    LEGACY_ = 474,                 /* LEGACY_  */
    LIBRARY_ = 475,                /* LIBRARY_  */
    X86_ = 476,                    /* X86_  */
    AMD64_ = 477,                  /* AMD64_  */
    ARM_ = 478,                    /* ARM_  */
    ARM64_ = 479,                  /* ARM64_  */
    MARSHAL_ = 480,                /* MARSHAL_  */
    CUSTOM_ = 481,                 /* CUSTOM_  */
    SYSSTRING_ = 482,              /* SYSSTRING_  */
    FIXED_ = 483,                  /* FIXED_  */
    VARIANT_ = 484,                /* VARIANT_  */
    CURRENCY_ = 485,               /* CURRENCY_  */
    SYSCHAR_ = 486,                /* SYSCHAR_  */
    DECIMAL_ = 487,                /* DECIMAL_  */
    DATE_ = 488,                   /* DATE_  */
    BSTR_ = 489,                   /* BSTR_  */
    TBSTR_ = 490,                  /* TBSTR_  */
    LPSTR_ = 491,                  /* LPSTR_  */
    LPWSTR_ = 492,                 /* LPWSTR_  */
    LPTSTR_ = 493,                 /* LPTSTR_  */
    OBJECTREF_ = 494,              /* OBJECTREF_  */
    IUNKNOWN_ = 495,               /* IUNKNOWN_  */
    IDISPATCH_ = 496,              /* IDISPATCH_  */
    STRUCT_ = 497,                 /* STRUCT_  */
    SAFEARRAY_ = 498,              /* SAFEARRAY_  */
    BYVALSTR_ = 499,               /* BYVALSTR_  */
    LPVOID_ = 500,                 /* LPVOID_  */
    ANY_ = 501,                    /* ANY_  */
    ARRAY_ = 502,                  /* ARRAY_  */
    LPSTRUCT_ = 503,               /* LPSTRUCT_  */
    IIDPARAM_ = 504,               /* IIDPARAM_  */
    IN_ = 505,                     /* IN_  */
    OUT_ = 506,                    /* OUT_  */
    OPT_ = 507,                    /* OPT_  */
    _PARAM = 508,                  /* _PARAM  */
    _OVERRIDE = 509,               /* _OVERRIDE  */
    WITH_ = 510,                   /* WITH_  */
    NULL_ = 511,                   /* NULL_  */
    ERROR_ = 512,                  /* ERROR_  */
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
    CONSTRAINT_ = 543              /* CONSTRAINT_  */
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

#line 449 "asmparse.cpp"

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
  YYSYMBOL_STRICT_ = 99,                   /* STRICT_  */
  YYSYMBOL_RETARGETABLE_ = 100,            /* RETARGETABLE_  */
  YYSYMBOL_WINDOWSRUNTIME_ = 101,          /* WINDOWSRUNTIME_  */
  YYSYMBOL_NOPLATFORM_ = 102,              /* NOPLATFORM_  */
  YYSYMBOL_METHOD_ = 103,                  /* METHOD_  */
  YYSYMBOL_FIELD_ = 104,                   /* FIELD_  */
  YYSYMBOL_PINNED_ = 105,                  /* PINNED_  */
  YYSYMBOL_MODREQ_ = 106,                  /* MODREQ_  */
  YYSYMBOL_MODOPT_ = 107,                  /* MODOPT_  */
  YYSYMBOL_SERIALIZABLE_ = 108,            /* SERIALIZABLE_  */
  YYSYMBOL_PROPERTY_ = 109,                /* PROPERTY_  */
  YYSYMBOL_TYPE_ = 110,                    /* TYPE_  */
  YYSYMBOL_ASSEMBLY_ = 111,                /* ASSEMBLY_  */
  YYSYMBOL_FAMANDASSEM_ = 112,             /* FAMANDASSEM_  */
  YYSYMBOL_FAMORASSEM_ = 113,              /* FAMORASSEM_  */
  YYSYMBOL_PRIVATESCOPE_ = 114,            /* PRIVATESCOPE_  */
  YYSYMBOL_HIDEBYSIG_ = 115,               /* HIDEBYSIG_  */
  YYSYMBOL_NEWSLOT_ = 116,                 /* NEWSLOT_  */
  YYSYMBOL_RTSPECIALNAME_ = 117,           /* RTSPECIALNAME_  */
  YYSYMBOL_PINVOKEIMPL_ = 118,             /* PINVOKEIMPL_  */
  YYSYMBOL__CTOR = 119,                    /* _CTOR  */
  YYSYMBOL__CCTOR = 120,                   /* _CCTOR  */
  YYSYMBOL_LITERAL_ = 121,                 /* LITERAL_  */
  YYSYMBOL_NOTSERIALIZED_ = 122,           /* NOTSERIALIZED_  */
  YYSYMBOL_INITONLY_ = 123,                /* INITONLY_  */
  YYSYMBOL_REQSECOBJ_ = 124,               /* REQSECOBJ_  */
  YYSYMBOL_CIL_ = 125,                     /* CIL_  */
  YYSYMBOL_OPTIL_ = 126,                   /* OPTIL_  */
  YYSYMBOL_MANAGED_ = 127,                 /* MANAGED_  */
  YYSYMBOL_FORWARDREF_ = 128,              /* FORWARDREF_  */
  YYSYMBOL_PRESERVESIG_ = 129,             /* PRESERVESIG_  */
  YYSYMBOL_RUNTIME_ = 130,                 /* RUNTIME_  */
  YYSYMBOL_INTERNALCALL_ = 131,            /* INTERNALCALL_  */
  YYSYMBOL__IMPORT = 132,                  /* _IMPORT  */
  YYSYMBOL_NOMANGLE_ = 133,                /* NOMANGLE_  */
  YYSYMBOL_LASTERR_ = 134,                 /* LASTERR_  */
  YYSYMBOL_WINAPI_ = 135,                  /* WINAPI_  */
  YYSYMBOL_AS_ = 136,                      /* AS_  */
  YYSYMBOL_BESTFIT_ = 137,                 /* BESTFIT_  */
  YYSYMBOL_ON_ = 138,                      /* ON_  */
  YYSYMBOL_OFF_ = 139,                     /* OFF_  */
  YYSYMBOL_CHARMAPERROR_ = 140,            /* CHARMAPERROR_  */
  YYSYMBOL_INSTR_NONE = 141,               /* INSTR_NONE  */
  YYSYMBOL_INSTR_VAR = 142,                /* INSTR_VAR  */
  YYSYMBOL_INSTR_I = 143,                  /* INSTR_I  */
  YYSYMBOL_INSTR_I8 = 144,                 /* INSTR_I8  */
  YYSYMBOL_INSTR_R = 145,                  /* INSTR_R  */
  YYSYMBOL_INSTR_BRTARGET = 146,           /* INSTR_BRTARGET  */
  YYSYMBOL_INSTR_METHOD = 147,             /* INSTR_METHOD  */
  YYSYMBOL_INSTR_FIELD = 148,              /* INSTR_FIELD  */
  YYSYMBOL_INSTR_TYPE = 149,               /* INSTR_TYPE  */
  YYSYMBOL_INSTR_STRING = 150,             /* INSTR_STRING  */
  YYSYMBOL_INSTR_SIG = 151,                /* INSTR_SIG  */
  YYSYMBOL_INSTR_TOK = 152,                /* INSTR_TOK  */
  YYSYMBOL_INSTR_SWITCH = 153,             /* INSTR_SWITCH  */
  YYSYMBOL__CLASS = 154,                   /* _CLASS  */
  YYSYMBOL__NAMESPACE = 155,               /* _NAMESPACE  */
  YYSYMBOL__METHOD = 156,                  /* _METHOD  */
  YYSYMBOL__FIELD = 157,                   /* _FIELD  */
  YYSYMBOL__DATA = 158,                    /* _DATA  */
  YYSYMBOL__THIS = 159,                    /* _THIS  */
  YYSYMBOL__BASE = 160,                    /* _BASE  */
  YYSYMBOL__NESTER = 161,                  /* _NESTER  */
  YYSYMBOL__EMITBYTE = 162,                /* _EMITBYTE  */
  YYSYMBOL__TRY = 163,                     /* _TRY  */
  YYSYMBOL__MAXSTACK = 164,                /* _MAXSTACK  */
  YYSYMBOL__LOCALS = 165,                  /* _LOCALS  */
  YYSYMBOL__ENTRYPOINT = 166,              /* _ENTRYPOINT  */
  YYSYMBOL__ZEROINIT = 167,                /* _ZEROINIT  */
  YYSYMBOL__EVENT = 168,                   /* _EVENT  */
  YYSYMBOL__ADDON = 169,                   /* _ADDON  */
  YYSYMBOL__REMOVEON = 170,                /* _REMOVEON  */
  YYSYMBOL__FIRE = 171,                    /* _FIRE  */
  YYSYMBOL__OTHER = 172,                   /* _OTHER  */
  YYSYMBOL__PROPERTY = 173,                /* _PROPERTY  */
  YYSYMBOL__SET = 174,                     /* _SET  */
  YYSYMBOL__GET = 175,                     /* _GET  */
  YYSYMBOL__PERMISSION = 176,              /* _PERMISSION  */
  YYSYMBOL__PERMISSIONSET = 177,           /* _PERMISSIONSET  */
  YYSYMBOL_REQUEST_ = 178,                 /* REQUEST_  */
  YYSYMBOL_DEMAND_ = 179,                  /* DEMAND_  */
  YYSYMBOL_ASSERT_ = 180,                  /* ASSERT_  */
  YYSYMBOL_DENY_ = 181,                    /* DENY_  */
  YYSYMBOL_PERMITONLY_ = 182,              /* PERMITONLY_  */
  YYSYMBOL_LINKCHECK_ = 183,               /* LINKCHECK_  */
  YYSYMBOL_INHERITCHECK_ = 184,            /* INHERITCHECK_  */
  YYSYMBOL_REQMIN_ = 185,                  /* REQMIN_  */
  YYSYMBOL_REQOPT_ = 186,                  /* REQOPT_  */
  YYSYMBOL_REQREFUSE_ = 187,               /* REQREFUSE_  */
  YYSYMBOL_PREJITGRANT_ = 188,             /* PREJITGRANT_  */
  YYSYMBOL_PREJITDENY_ = 189,              /* PREJITDENY_  */
  YYSYMBOL_NONCASDEMAND_ = 190,            /* NONCASDEMAND_  */
  YYSYMBOL_NONCASLINKDEMAND_ = 191,        /* NONCASLINKDEMAND_  */
  YYSYMBOL_NONCASINHERITANCE_ = 192,       /* NONCASINHERITANCE_  */
  YYSYMBOL__LINE = 193,                    /* _LINE  */
  YYSYMBOL_P_LINE = 194,                   /* P_LINE  */
  YYSYMBOL__LANGUAGE = 195,                /* _LANGUAGE  */
  YYSYMBOL__CUSTOM = 196,                  /* _CUSTOM  */
  YYSYMBOL_INIT_ = 197,                    /* INIT_  */
  YYSYMBOL__SIZE = 198,                    /* _SIZE  */
  YYSYMBOL__PACK = 199,                    /* _PACK  */
  YYSYMBOL__VTABLE = 200,                  /* _VTABLE  */
  YYSYMBOL__VTFIXUP = 201,                 /* _VTFIXUP  */
  YYSYMBOL_FROMUNMANAGED_ = 202,           /* FROMUNMANAGED_  */
  YYSYMBOL_CALLMOSTDERIVED_ = 203,         /* CALLMOSTDERIVED_  */
  YYSYMBOL__VTENTRY = 204,                 /* _VTENTRY  */
  YYSYMBOL_RETAINAPPDOMAIN_ = 205,         /* RETAINAPPDOMAIN_  */
  YYSYMBOL__FILE = 206,                    /* _FILE  */
  YYSYMBOL_NOMETADATA_ = 207,              /* NOMETADATA_  */
  YYSYMBOL__HASH = 208,                    /* _HASH  */
  YYSYMBOL__ASSEMBLY = 209,                /* _ASSEMBLY  */
  YYSYMBOL__PUBLICKEY = 210,               /* _PUBLICKEY  */
  YYSYMBOL__PUBLICKEYTOKEN = 211,          /* _PUBLICKEYTOKEN  */
  YYSYMBOL_ALGORITHM_ = 212,               /* ALGORITHM_  */
  YYSYMBOL__VER = 213,                     /* _VER  */
  YYSYMBOL__LOCALE = 214,                  /* _LOCALE  */
  YYSYMBOL_EXTERN_ = 215,                  /* EXTERN_  */
  YYSYMBOL__MRESOURCE = 216,               /* _MRESOURCE  */
  YYSYMBOL__MODULE = 217,                  /* _MODULE  */
  YYSYMBOL__EXPORT = 218,                  /* _EXPORT  */
  YYSYMBOL_LEGACY_ = 219,                  /* LEGACY_  */
  YYSYMBOL_LIBRARY_ = 220,                 /* LIBRARY_  */
  YYSYMBOL_X86_ = 221,                     /* X86_  */
  YYSYMBOL_AMD64_ = 222,                   /* AMD64_  */
  YYSYMBOL_ARM_ = 223,                     /* ARM_  */
  YYSYMBOL_ARM64_ = 224,                   /* ARM64_  */
  YYSYMBOL_MARSHAL_ = 225,                 /* MARSHAL_  */
  YYSYMBOL_CUSTOM_ = 226,                  /* CUSTOM_  */
  YYSYMBOL_SYSSTRING_ = 227,               /* SYSSTRING_  */
  YYSYMBOL_FIXED_ = 228,                   /* FIXED_  */
  YYSYMBOL_VARIANT_ = 229,                 /* VARIANT_  */
  YYSYMBOL_CURRENCY_ = 230,                /* CURRENCY_  */
  YYSYMBOL_SYSCHAR_ = 231,                 /* SYSCHAR_  */
  YYSYMBOL_DECIMAL_ = 232,                 /* DECIMAL_  */
  YYSYMBOL_DATE_ = 233,                    /* DATE_  */
  YYSYMBOL_BSTR_ = 234,                    /* BSTR_  */
  YYSYMBOL_TBSTR_ = 235,                   /* TBSTR_  */
  YYSYMBOL_LPSTR_ = 236,                   /* LPSTR_  */
  YYSYMBOL_LPWSTR_ = 237,                  /* LPWSTR_  */
  YYSYMBOL_LPTSTR_ = 238,                  /* LPTSTR_  */
  YYSYMBOL_OBJECTREF_ = 239,               /* OBJECTREF_  */
  YYSYMBOL_IUNKNOWN_ = 240,                /* IUNKNOWN_  */
  YYSYMBOL_IDISPATCH_ = 241,               /* IDISPATCH_  */
  YYSYMBOL_STRUCT_ = 242,                  /* STRUCT_  */
  YYSYMBOL_SAFEARRAY_ = 243,               /* SAFEARRAY_  */
  YYSYMBOL_BYVALSTR_ = 244,                /* BYVALSTR_  */
  YYSYMBOL_LPVOID_ = 245,                  /* LPVOID_  */
  YYSYMBOL_ANY_ = 246,                     /* ANY_  */
  YYSYMBOL_ARRAY_ = 247,                   /* ARRAY_  */
  YYSYMBOL_LPSTRUCT_ = 248,                /* LPSTRUCT_  */
  YYSYMBOL_IIDPARAM_ = 249,                /* IIDPARAM_  */
  YYSYMBOL_IN_ = 250,                      /* IN_  */
  YYSYMBOL_OUT_ = 251,                     /* OUT_  */
  YYSYMBOL_OPT_ = 252,                     /* OPT_  */
  YYSYMBOL__PARAM = 253,                   /* _PARAM  */
  YYSYMBOL__OVERRIDE = 254,                /* _OVERRIDE  */
  YYSYMBOL_WITH_ = 255,                    /* WITH_  */
  YYSYMBOL_NULL_ = 256,                    /* NULL_  */
  YYSYMBOL_ERROR_ = 257,                   /* ERROR_  */
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
  YYSYMBOL_289_ = 289,                     /* '{'  */
  YYSYMBOL_290_ = 290,                     /* '}'  */
  YYSYMBOL_291_ = 291,                     /* '+'  */
  YYSYMBOL_292_ = 292,                     /* ','  */
  YYSYMBOL_293_ = 293,                     /* '.'  */
  YYSYMBOL_294_ = 294,                     /* '('  */
  YYSYMBOL_295_ = 295,                     /* ')'  */
  YYSYMBOL_296_ = 296,                     /* ';'  */
  YYSYMBOL_297_ = 297,                     /* '='  */
  YYSYMBOL_298_ = 298,                     /* '['  */
  YYSYMBOL_299_ = 299,                     /* ']'  */
  YYSYMBOL_300_ = 300,                     /* '<'  */
  YYSYMBOL_301_ = 301,                     /* '>'  */
  YYSYMBOL_302_ = 302,                     /* '-'  */
  YYSYMBOL_303_ = 303,                     /* ':'  */
  YYSYMBOL_304_ = 304,                     /* '*'  */
  YYSYMBOL_305_ = 305,                     /* '&'  */
  YYSYMBOL_306_ = 306,                     /* '/'  */
  YYSYMBOL_307_ = 307,                     /* '!'  */
  YYSYMBOL_YYACCEPT = 308,                 /* $accept  */
  YYSYMBOL_decls = 309,                    /* decls  */
  YYSYMBOL_decl = 310,                     /* decl  */
  YYSYMBOL_classNameSeq = 311,             /* classNameSeq  */
  YYSYMBOL_compQstring = 312,              /* compQstring  */
  YYSYMBOL_languageDecl = 313,             /* languageDecl  */
  YYSYMBOL_id = 314,                       /* id  */
  YYSYMBOL_dottedName = 315,               /* dottedName  */
  YYSYMBOL_int32 = 316,                    /* int32  */
  YYSYMBOL_int64 = 317,                    /* int64  */
  YYSYMBOL_float64 = 318,                  /* float64  */
  YYSYMBOL_typedefDecl = 319,              /* typedefDecl  */
  YYSYMBOL_compControl = 320,              /* compControl  */
  YYSYMBOL_customDescr = 321,              /* customDescr  */
  YYSYMBOL_customDescrWithOwner = 322,     /* customDescrWithOwner  */
  YYSYMBOL_customHead = 323,               /* customHead  */
  YYSYMBOL_customHeadWithOwner = 324,      /* customHeadWithOwner  */
  YYSYMBOL_customType = 325,               /* customType  */
  YYSYMBOL_ownerType = 326,                /* ownerType  */
  YYSYMBOL_customBlobDescr = 327,          /* customBlobDescr  */
  YYSYMBOL_customBlobArgs = 328,           /* customBlobArgs  */
  YYSYMBOL_customBlobNVPairs = 329,        /* customBlobNVPairs  */
  YYSYMBOL_fieldOrProp = 330,              /* fieldOrProp  */
  YYSYMBOL_customAttrDecl = 331,           /* customAttrDecl  */
  YYSYMBOL_serializType = 332,             /* serializType  */
  YYSYMBOL_moduleHead = 333,               /* moduleHead  */
  YYSYMBOL_vtfixupDecl = 334,              /* vtfixupDecl  */
  YYSYMBOL_vtfixupAttr = 335,              /* vtfixupAttr  */
  YYSYMBOL_vtableDecl = 336,               /* vtableDecl  */
  YYSYMBOL_vtableHead = 337,               /* vtableHead  */
  YYSYMBOL_nameSpaceHead = 338,            /* nameSpaceHead  */
  YYSYMBOL__class = 339,                   /* _class  */
  YYSYMBOL_classHeadBegin = 340,           /* classHeadBegin  */
  YYSYMBOL_classHead = 341,                /* classHead  */
  YYSYMBOL_classAttr = 342,                /* classAttr  */
  YYSYMBOL_extendsClause = 343,            /* extendsClause  */
  YYSYMBOL_implClause = 344,               /* implClause  */
  YYSYMBOL_classDecls = 345,               /* classDecls  */
  YYSYMBOL_implList = 346,                 /* implList  */
  YYSYMBOL_typeList = 347,                 /* typeList  */
  YYSYMBOL_typeListNotEmpty = 348,         /* typeListNotEmpty  */
  YYSYMBOL_typarsClause = 349,             /* typarsClause  */
  YYSYMBOL_typarAttrib = 350,              /* typarAttrib  */
  YYSYMBOL_typarAttribs = 351,             /* typarAttribs  */
  YYSYMBOL_typars = 352,                   /* typars  */
  YYSYMBOL_typarsRest = 353,               /* typarsRest  */
  YYSYMBOL_tyBound = 354,                  /* tyBound  */
  YYSYMBOL_genArity = 355,                 /* genArity  */
  YYSYMBOL_genArityNotEmpty = 356,         /* genArityNotEmpty  */
  YYSYMBOL_classDecl = 357,                /* classDecl  */
  YYSYMBOL_fieldDecl = 358,                /* fieldDecl  */
  YYSYMBOL_fieldAttr = 359,                /* fieldAttr  */
  YYSYMBOL_atOpt = 360,                    /* atOpt  */
  YYSYMBOL_initOpt = 361,                  /* initOpt  */
  YYSYMBOL_repeatOpt = 362,                /* repeatOpt  */
  YYSYMBOL_methodRef = 363,                /* methodRef  */
  YYSYMBOL_callConv = 364,                 /* callConv  */
  YYSYMBOL_callKind = 365,                 /* callKind  */
  YYSYMBOL_mdtoken = 366,                  /* mdtoken  */
  YYSYMBOL_memberRef = 367,                /* memberRef  */
  YYSYMBOL_eventHead = 368,                /* eventHead  */
  YYSYMBOL_eventAttr = 369,                /* eventAttr  */
  YYSYMBOL_eventDecls = 370,               /* eventDecls  */
  YYSYMBOL_eventDecl = 371,                /* eventDecl  */
  YYSYMBOL_propHead = 372,                 /* propHead  */
  YYSYMBOL_propAttr = 373,                 /* propAttr  */
  YYSYMBOL_propDecls = 374,                /* propDecls  */
  YYSYMBOL_propDecl = 375,                 /* propDecl  */
  YYSYMBOL_methodHeadPart1 = 376,          /* methodHeadPart1  */
  YYSYMBOL_marshalClause = 377,            /* marshalClause  */
  YYSYMBOL_marshalBlob = 378,              /* marshalBlob  */
  YYSYMBOL_marshalBlobHead = 379,          /* marshalBlobHead  */
  YYSYMBOL_methodHead = 380,               /* methodHead  */
  YYSYMBOL_methAttr = 381,                 /* methAttr  */
  YYSYMBOL_pinvAttr = 382,                 /* pinvAttr  */
  YYSYMBOL_methodName = 383,               /* methodName  */
  YYSYMBOL_paramAttr = 384,                /* paramAttr  */
  YYSYMBOL_implAttr = 385,                 /* implAttr  */
  YYSYMBOL_localsHead = 386,               /* localsHead  */
  YYSYMBOL_methodDecls = 387,              /* methodDecls  */
  YYSYMBOL_methodDecl = 388,               /* methodDecl  */
  YYSYMBOL_scopeBlock = 389,               /* scopeBlock  */
  YYSYMBOL_scopeOpen = 390,                /* scopeOpen  */
  YYSYMBOL_sehBlock = 391,                 /* sehBlock  */
  YYSYMBOL_sehClauses = 392,               /* sehClauses  */
  YYSYMBOL_tryBlock = 393,                 /* tryBlock  */
  YYSYMBOL_tryHead = 394,                  /* tryHead  */
  YYSYMBOL_sehClause = 395,                /* sehClause  */
  YYSYMBOL_filterClause = 396,             /* filterClause  */
  YYSYMBOL_filterHead = 397,               /* filterHead  */
  YYSYMBOL_catchClause = 398,              /* catchClause  */
  YYSYMBOL_finallyClause = 399,            /* finallyClause  */
  YYSYMBOL_faultClause = 400,              /* faultClause  */
  YYSYMBOL_handlerBlock = 401,             /* handlerBlock  */
  YYSYMBOL_dataDecl = 402,                 /* dataDecl  */
  YYSYMBOL_ddHead = 403,                   /* ddHead  */
  YYSYMBOL_tls = 404,                      /* tls  */
  YYSYMBOL_ddBody = 405,                   /* ddBody  */
  YYSYMBOL_ddItemList = 406,               /* ddItemList  */
  YYSYMBOL_ddItemCount = 407,              /* ddItemCount  */
  YYSYMBOL_ddItem = 408,                   /* ddItem  */
  YYSYMBOL_fieldSerInit = 409,             /* fieldSerInit  */
  YYSYMBOL_bytearrayhead = 410,            /* bytearrayhead  */
  YYSYMBOL_bytes = 411,                    /* bytes  */
  YYSYMBOL_hexbytes = 412,                 /* hexbytes  */
  YYSYMBOL_fieldInit = 413,                /* fieldInit  */
  YYSYMBOL_serInit = 414,                  /* serInit  */
  YYSYMBOL_f32seq = 415,                   /* f32seq  */
  YYSYMBOL_f64seq = 416,                   /* f64seq  */
  YYSYMBOL_i64seq = 417,                   /* i64seq  */
  YYSYMBOL_i32seq = 418,                   /* i32seq  */
  YYSYMBOL_i16seq = 419,                   /* i16seq  */
  YYSYMBOL_i8seq = 420,                    /* i8seq  */
  YYSYMBOL_boolSeq = 421,                  /* boolSeq  */
  YYSYMBOL_sqstringSeq = 422,              /* sqstringSeq  */
  YYSYMBOL_classSeq = 423,                 /* classSeq  */
  YYSYMBOL_objSeq = 424,                   /* objSeq  */
  YYSYMBOL_methodSpec = 425,               /* methodSpec  */
  YYSYMBOL_instr_none = 426,               /* instr_none  */
  YYSYMBOL_instr_var = 427,                /* instr_var  */
  YYSYMBOL_instr_i = 428,                  /* instr_i  */
  YYSYMBOL_instr_i8 = 429,                 /* instr_i8  */
  YYSYMBOL_instr_r = 430,                  /* instr_r  */
  YYSYMBOL_instr_brtarget = 431,           /* instr_brtarget  */
  YYSYMBOL_instr_method = 432,             /* instr_method  */
  YYSYMBOL_instr_field = 433,              /* instr_field  */
  YYSYMBOL_instr_type = 434,               /* instr_type  */
  YYSYMBOL_instr_string = 435,             /* instr_string  */
  YYSYMBOL_instr_sig = 436,                /* instr_sig  */
  YYSYMBOL_instr_tok = 437,                /* instr_tok  */
  YYSYMBOL_instr_switch = 438,             /* instr_switch  */
  YYSYMBOL_instr_r_head = 439,             /* instr_r_head  */
  YYSYMBOL_instr = 440,                    /* instr  */
  YYSYMBOL_labels = 441,                   /* labels  */
  YYSYMBOL_tyArgs0 = 442,                  /* tyArgs0  */
  YYSYMBOL_tyArgs1 = 443,                  /* tyArgs1  */
  YYSYMBOL_tyArgs2 = 444,                  /* tyArgs2  */
  YYSYMBOL_sigArgs0 = 445,                 /* sigArgs0  */
  YYSYMBOL_sigArgs1 = 446,                 /* sigArgs1  */
  YYSYMBOL_sigArg = 447,                   /* sigArg  */
  YYSYMBOL_className = 448,                /* className  */
  YYSYMBOL_slashedName = 449,              /* slashedName  */
  YYSYMBOL_typeSpec = 450,                 /* typeSpec  */
  YYSYMBOL_nativeType = 451,               /* nativeType  */
  YYSYMBOL_iidParamIndex = 452,            /* iidParamIndex  */
  YYSYMBOL_variantType = 453,              /* variantType  */
  YYSYMBOL_type = 454,                     /* type  */
  YYSYMBOL_simpleType = 455,               /* simpleType  */
  YYSYMBOL_bounds1 = 456,                  /* bounds1  */
  YYSYMBOL_bound = 457,                    /* bound  */
  YYSYMBOL_secDecl = 458,                  /* secDecl  */
  YYSYMBOL_secAttrSetBlob = 459,           /* secAttrSetBlob  */
  YYSYMBOL_secAttrBlob = 460,              /* secAttrBlob  */
  YYSYMBOL_psetHead = 461,                 /* psetHead  */
  YYSYMBOL_nameValPairs = 462,             /* nameValPairs  */
  YYSYMBOL_nameValPair = 463,              /* nameValPair  */
  YYSYMBOL_truefalse = 464,                /* truefalse  */
  YYSYMBOL_caValue = 465,                  /* caValue  */
  YYSYMBOL_secAction = 466,                /* secAction  */
  YYSYMBOL_esHead = 467,                   /* esHead  */
  YYSYMBOL_extSourceSpec = 468,            /* extSourceSpec  */
  YYSYMBOL_fileDecl = 469,                 /* fileDecl  */
  YYSYMBOL_fileAttr = 470,                 /* fileAttr  */
  YYSYMBOL_fileEntry = 471,                /* fileEntry  */
  YYSYMBOL_hashHead = 472,                 /* hashHead  */
  YYSYMBOL_assemblyHead = 473,             /* assemblyHead  */
  YYSYMBOL_asmAttr = 474,                  /* asmAttr  */
  YYSYMBOL_assemblyDecls = 475,            /* assemblyDecls  */
  YYSYMBOL_assemblyDecl = 476,             /* assemblyDecl  */
  YYSYMBOL_intOrWildcard = 477,            /* intOrWildcard  */
  YYSYMBOL_asmOrRefDecl = 478,             /* asmOrRefDecl  */
  YYSYMBOL_publicKeyHead = 479,            /* publicKeyHead  */
  YYSYMBOL_publicKeyTokenHead = 480,       /* publicKeyTokenHead  */
  YYSYMBOL_localeHead = 481,               /* localeHead  */
  YYSYMBOL_assemblyRefHead = 482,          /* assemblyRefHead  */
  YYSYMBOL_assemblyRefDecls = 483,         /* assemblyRefDecls  */
  YYSYMBOL_assemblyRefDecl = 484,          /* assemblyRefDecl  */
  YYSYMBOL_exptypeHead = 485,              /* exptypeHead  */
  YYSYMBOL_exportHead = 486,               /* exportHead  */
  YYSYMBOL_exptAttr = 487,                 /* exptAttr  */
  YYSYMBOL_exptypeDecls = 488,             /* exptypeDecls  */
  YYSYMBOL_exptypeDecl = 489,              /* exptypeDecl  */
  YYSYMBOL_manifestResHead = 490,          /* manifestResHead  */
  YYSYMBOL_manresAttr = 491,               /* manresAttr  */
  YYSYMBOL_manifestResDecls = 492,         /* manifestResDecls  */
  YYSYMBOL_manifestResDecl = 493           /* manifestResDecl  */
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
#define YYLAST   3777

/* YYNTOKENS -- Number of terminals.  */
#define YYNTOKENS  308
/* YYNNTS -- Number of nonterminals.  */
#define YYNNTS  186
/* YYNRULES -- Number of rules.  */
#define YYNRULES  846
/* YYNSTATES -- Number of states.  */
#define YYNSTATES  1590

/* YYMAXUTOK -- Last valid token kind.  */
#define YYMAXUTOK   543


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
       2,     2,     2,   307,     2,     2,     2,     2,   305,     2,
     294,   295,   304,   291,   292,   302,   293,   306,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,   303,   296,
     300,   297,   301,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,   298,     2,   299,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,   289,     2,   290,     2,     2,     2,     2,
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
     285,   286,   287,   288
};

#if YYDEBUG
/* YYRLINE[YYN] -- Source line where rule number YYN was defined.  */
static const yytype_int16 yyrline[] =
{
       0,   189,   189,   190,   193,   194,   195,   199,   200,   201,
     202,   203,   204,   205,   206,   207,   208,   209,   210,   211,
     212,   222,   223,   226,   229,   230,   231,   232,   233,   234,
     237,   238,   241,   242,   245,   246,   248,   253,   254,   257,
     258,   259,   262,   265,   266,   269,   270,   271,   275,   276,
     277,   278,   279,   284,   285,   286,   287,   290,   293,   294,
     298,   299,   303,   304,   305,   306,   309,   310,   311,   313,
     316,   319,   325,   328,   329,   333,   339,   340,   342,   345,
     346,   352,   355,   356,   359,   363,   364,   372,   373,   374,
     375,   377,   379,   384,   385,   386,   393,   397,   398,   399,
     400,   401,   402,   405,   408,   412,   415,   418,   424,   427,
     428,   429,   430,   431,   432,   433,   434,   435,   436,   437,
     438,   439,   440,   441,   442,   443,   444,   445,   446,   447,
     448,   449,   450,   451,   452,   453,   456,   457,   460,   461,
     464,   465,   468,   469,   473,   474,   477,   478,   481,   482,
     485,   486,   487,   488,   489,   490,   491,   494,   495,   498,
     499,   502,   503,   506,   509,   510,   513,   517,   521,   522,
     523,   524,   525,   526,   527,   528,   529,   530,   531,   532,
     538,   547,   548,   549,   554,   560,   561,   562,   569,   574,
     575,   576,   577,   578,   579,   580,   581,   593,   595,   596,
     597,   598,   599,   600,   601,   604,   605,   608,   609,   612,
     613,   617,   634,   640,   656,   661,   662,   663,   666,   667,
     668,   669,   672,   673,   674,   675,   676,   677,   678,   679,
     682,   685,   690,   694,   698,   700,   702,   707,   708,   712,
     713,   714,   717,   718,   721,   722,   723,   724,   725,   726,
     727,   728,   732,   738,   739,   740,   743,   744,   748,   749,
     750,   751,   752,   753,   754,   758,   764,   765,   768,   769,
     772,   775,   791,   792,   793,   794,   795,   796,   797,   798,
     799,   800,   801,   802,   803,   804,   805,   806,   807,   808,
     809,   810,   811,   814,   817,   822,   823,   824,   825,   826,
     827,   828,   829,   830,   831,   832,   833,   834,   835,   836,
     837,   840,   841,   842,   845,   846,   847,   848,   849,   852,
     853,   854,   855,   856,   857,   858,   859,   860,   861,   862,
     863,   864,   865,   866,   867,   870,   874,   875,   878,   879,
     880,   881,   883,   886,   887,   888,   889,   890,   891,   892,
     893,   894,   895,   896,   906,   916,   918,   921,   928,   929,
     934,   940,   941,   943,   964,   967,   971,   974,   975,   978,
     979,   980,   984,   989,   990,   991,   992,   996,   997,   999,
    1003,  1007,  1012,  1016,  1020,  1021,  1022,  1027,  1030,  1031,
    1034,  1035,  1036,  1039,  1040,  1043,  1044,  1047,  1048,  1053,
    1054,  1055,  1056,  1063,  1070,  1077,  1084,  1092,  1100,  1101,
    1102,  1103,  1104,  1105,  1109,  1112,  1114,  1116,  1118,  1120,
    1122,  1124,  1126,  1128,  1130,  1132,  1134,  1136,  1138,  1140,
    1142,  1144,  1146,  1150,  1153,  1154,  1157,  1158,  1162,  1163,
    1164,  1169,  1170,  1171,  1173,  1175,  1177,  1178,  1179,  1183,
    1187,  1191,  1195,  1199,  1203,  1207,  1211,  1215,  1219,  1223,
    1227,  1231,  1235,  1239,  1243,  1247,  1251,  1258,  1259,  1261,
    1265,  1266,  1268,  1272,  1273,  1277,  1278,  1281,  1282,  1285,
    1286,  1289,  1290,  1294,  1295,  1296,  1300,  1301,  1302,  1304,
    1308,  1309,  1313,  1319,  1322,  1325,  1328,  1331,  1334,  1337,
    1345,  1348,  1351,  1354,  1357,  1360,  1363,  1367,  1368,  1369,
    1370,  1371,  1372,  1373,  1374,  1383,  1384,  1385,  1392,  1400,
    1408,  1414,  1420,  1426,  1430,  1431,  1433,  1435,  1439,  1445,
    1448,  1449,  1450,  1451,  1452,  1456,  1457,  1460,  1461,  1464,
    1465,  1469,  1470,  1473,  1474,  1477,  1478,  1479,  1483,  1484,
    1485,  1486,  1487,  1488,  1489,  1490,  1493,  1499,  1506,  1507,
    1510,  1511,  1512,  1513,  1517,  1518,  1525,  1531,  1533,  1536,
    1538,  1539,  1541,  1543,  1544,  1545,  1546,  1547,  1548,  1549,
    1550,  1551,  1552,  1553,  1554,  1555,  1556,  1557,  1558,  1559,
    1561,  1563,  1568,  1573,  1576,  1578,  1580,  1581,  1582,  1583,
    1584,  1586,  1588,  1590,  1591,  1593,  1596,  1600,  1601,  1602,
    1603,  1605,  1606,  1607,  1608,  1609,  1610,  1611,  1612,  1615,
    1616,  1619,  1620,  1621,  1622,  1623,  1624,  1625,  1626,  1627,
    1628,  1629,  1630,  1631,  1632,  1633,  1634,  1635,  1636,  1637,
    1638,  1639,  1640,  1641,  1642,  1643,  1644,  1645,  1646,  1647,
    1648,  1649,  1650,  1651,  1652,  1653,  1654,  1655,  1656,  1657,
    1658,  1659,  1660,  1661,  1662,  1663,  1664,  1665,  1666,  1667,
    1671,  1677,  1678,  1679,  1680,  1681,  1682,  1683,  1684,  1685,
    1687,  1689,  1696,  1703,  1709,  1715,  1730,  1745,  1746,  1747,
    1748,  1749,  1750,  1751,  1754,  1755,  1756,  1757,  1758,  1759,
    1760,  1761,  1762,  1763,  1764,  1765,  1766,  1767,  1768,  1769,
    1770,  1771,  1774,  1775,  1778,  1779,  1780,  1781,  1784,  1788,
    1790,  1792,  1793,  1794,  1796,  1805,  1806,  1807,  1810,  1813,
    1818,  1819,  1823,  1824,  1827,  1830,  1831,  1834,  1837,  1840,
    1843,  1847,  1853,  1859,  1865,  1873,  1874,  1875,  1876,  1877,
    1878,  1879,  1880,  1881,  1882,  1883,  1884,  1885,  1886,  1887,
    1891,  1892,  1895,  1898,  1900,  1903,  1905,  1909,  1912,  1916,
    1919,  1923,  1926,  1932,  1934,  1937,  1938,  1941,  1942,  1945,
    1948,  1951,  1952,  1953,  1954,  1955,  1956,  1957,  1958,  1959,
    1960,  1963,  1964,  1967,  1968,  1969,  1972,  1973,  1976,  1977,
    1979,  1980,  1981,  1982,  1985,  1988,  1991,  1994,  1996,  2000,
    2001,  2004,  2005,  2006,  2007,  2010,  2013,  2016,  2017,  2018,
    2019,  2020,  2021,  2022,  2023,  2024,  2025,  2028,  2029,  2032,
    2033,  2034,  2035,  2037,  2039,  2040,  2043,  2044,  2048,  2049,
    2050,  2053,  2054,  2057,  2058,  2059,  2060
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

#define YYPACT_NINF (-1367)

#define yypact_value_is_default(Yyn) \
  ((Yyn) == YYPACT_NINF)

#define YYTABLE_NINF (-559)

#define yytable_value_is_error(Yyn) \
  0

/* YYPACT[STATE-NUM] -- Index in YYTABLE of the portion describing
   STATE-NUM.  */
static const yytype_int16 yypact[] =
{
   -1367,  2062, -1367, -1367,   -51,   987, -1367,   -86,   123,  2317,
    2317, -1367, -1367,   246,   182,   -31,   -19,    16,   105, -1367,
     133,   272,   272,   215,   215,  1641,     9, -1367,   987,   987,
     987,   987, -1367, -1367,   315, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367,   320,   320, -1367, -1367, -1367, -1367,   320,    74,
   -1367,   285,   103, -1367, -1367, -1367, -1367,   729, -1367,   320,
     272, -1367, -1367,   116,   144,   167,   169, -1367, -1367, -1367,
   -1367, -1367,   191,   272, -1367, -1367, -1367,   368, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367,  1929,    43,    58, -1367, -1367,   181,   195,
   -1367, -1367,   824,   502,   502,  1825,   166, -1367,  2925, -1367,
   -1367,   202,   272,   272,   238, -1367,   620,   849,   987,   191,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,  2925,
   -1367, -1367, -1367,   894, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367, -1367,   589, -1367,   418,   589,
     378, -1367,  2339, -1367, -1367, -1367,    67,    50,   191,   373,
     387, -1367,   404,  1377,   414,   254,   745, -1367,   589,    45,
     191,   191,   191, -1367, -1367,   282,   579,   301,   314, -1367,
    3481,  1929,   557, -1367,  3653,  2269,   335,    17,    93,   276,
     281,   291,   317,   347,   782,   358, -1367, -1367,   320,   359,
      61, -1367, -1367, -1367, -1367,  1130,   987,   385,  2715,   380,
      95, -1367,   502, -1367,   330,   926, -1367,   402,   -11,   413,
     664,   272,   272, -1367, -1367, -1367, -1367, -1367, -1367,   432,
   -1367, -1367,    91,  1273, -1367,   447, -1367, -1367,    69,   620,
   -1367, -1367, -1367, -1367,   533, -1367, -1367, -1367, -1367,   191,
   -1367, -1367,   -34,   191,   926, -1367, -1367, -1367, -1367, -1367,
     589, -1367,   741, -1367, -1367, -1367, -1367,  1582,   987,   483,
       4,   523,   472,   191, -1367,   987,   987,   987, -1367,  2925,
     987,   987, -1367,   507,   536,   987,    68,  2925, -1367, -1367,
     490,   589,   413, -1367, -1367, -1367, -1367,  2862,   539, -1367,
   -1367, -1367, -1367, -1367, -1367,   803, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,  -135,
   -1367,  1929, -1367,  3003,   543, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367,   562, -1367, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,   272, -1367,
     272, -1367, -1367, -1367,   272,   529,    11,  1999, -1367, -1367,
   -1367,   537, -1367, -1367,   -44, -1367, -1367, -1367, -1367,   546,
     208, -1367, -1367,   503,   272,   215,   296,   503,  1377,   985,
    1929,   171,   502,  1825,   582,   320, -1367, -1367, -1367,   588,
     272,   272, -1367,   272, -1367,   272, -1367,   215, -1367,   303,
   -1367,   303, -1367, -1367,   559,   594,   368,   596, -1367, -1367,
   -1367,   272,   272,   954,  3164,  1071,   581, -1367, -1367, -1367,
     868,   191,   191, -1367,   599, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,   607,    56,
   -1367,   987,    44,  2925,   905,   629, -1367,  2093, -1367,   923,
     641,   651,   655,  1377, -1367, -1367,   413, -1367, -1367,    85,
      38,   654,   937, -1367, -1367,   763,    -4, -1367,   987, -1367,
   -1367,    38,   955,   107,   987,   987,   987,   191, -1367,   191,
     191,   191,  1433,   191,   191,  1929,  1929,   191, -1367, -1367,
     940,   -62, -1367,   674,   690,   926, -1367, -1367, -1367,   272,
   -1367, -1367, -1367, -1367, -1367, -1367,   222, -1367,   691, -1367,
     874, -1367, -1367, -1367,   272,   272, -1367,    25,  2162, -1367,
   -1367, -1367, -1367,   702, -1367, -1367,   707,   714, -1367, -1367,
   -1367, -1367,   715,   272,   905,  2819, -1367, -1367,   712,   272,
     111,   137,   272,   502,  1000, -1367,   735,   100,  2432, -1367,
    1929, -1367, -1367, -1367,   546,    28,   208,    28,    28,    28,
     968,   973, -1367, -1367, -1367, -1367, -1367, -1367,   743,   750,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,  1582,
   -1367,   753,   413,   320,  2925, -1367,   503,   751,   905,   756,
     749,   757,   761,   762,   765,   766, -1367,   782,   767, -1367,
     755,    55,   862,   785,    21,    82, -1367, -1367, -1367, -1367,
   -1367, -1367,   320,   320, -1367,   786,   788, -1367,   320, -1367,
     320, -1367,   792,    73,   987,   876, -1367, -1367, -1367, -1367,
     987,   877, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367,   272,  3377,     8,   121,   987,  1085,    27,   798,
     806, -1367,   675,   802,   810,   809, -1367,  1100, -1367, -1367,
     825,   836,  3058,  2874,   839,   840,   575,   996,   320,   987,
     191,   987,   987,   254,   254,   254,   846,   848,   850,   272,
     146, -1367, -1367,  2925,   854,   847, -1367, -1367, -1367, -1367,
   -1367, -1367,   222,   125,   843,  1929,  1929,  1741,   752, -1367,
   -1367,  1130,   142,   164,   502,  1139, -1367, -1367, -1367,  2516,
   -1367,   864,     1,  2005,   209,   426,   272,   873,   272,   191,
     272,   237,   878,  2925,   575,   100, -1367,  2819,   866,   881,
   -1367, -1367, -1367, -1367,   503, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367,   368,   272,   272,   215,    38,  1144,   905,
     883,   871,   887,   888,   893, -1367,   225,   884, -1367,   884,
     884,   884,   884,   884, -1367, -1367,   272, -1367,   272,   272,
     889, -1367, -1367,   886,   899,   413,   902,   907,   910,   913,
     915,   918,   272,   987, -1367,   191,   987,    15,   987,   919,
   -1367, -1367, -1367,   791, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367, -1367,   914,   976,   981, -1367,
     974,   925,    -7,  1199, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367,   914,   914, -1367,  2977, -1367, -1367,
   -1367, -1367,   927,   320,   149,   368,   932,   987,   486, -1367,
     905,   933,   935,   944, -1367,  2093, -1367,    92, -1367,   355,
     365,   941,   375,   381,   388,   395,   403,   411,   417,   419,
     425,   434,   439,   441,   449, -1367,  1230, -1367,   320, -1367,
     272,   942,   100,   100,   191,   654, -1367, -1367,   368, -1367,
   -1367, -1367,   939,   191,   191,   254,   100, -1367, -1367, -1367,
   -1367,   926, -1367,   272, -1367,  1929,   374,   987, -1367, -1367,
    1046, -1367, -1367,   470,   987, -1367, -1367,  2925,   191,   272,
     191,   272,   481,  2925,   575,  3138,   870,  1533, -1367,  1129,
   -1367,   905,  2196,   951, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367,   943,   948, -1367,   956,   957,   967,
     969,   953,   575, -1367,  1117,   970,   971,  1929,   932,  1582,
   -1367,   977,   426, -1367,  1251,  1211,  1212, -1367, -1367,   990,
     992,   987,   476, -1367,   100,   503,   503, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367,    66,  1268, -1367, -1367,    21,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367,   993,   254,   191,
     272,   191, -1367, -1367, -1367, -1367, -1367, -1367,  1033, -1367,
   -1367, -1367, -1367,   905,  1005,  1008, -1367, -1367, -1367, -1367,
   -1367,   879, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,
     363, -1367,    31,    78, -1367, -1367,  2281, -1367,   997, -1367,
   -1367,   413, -1367,  1004, -1367, -1367, -1367, -1367,  1001, -1367,
   -1367, -1367, -1367,   413,   780,   272,   272,   272,   485,   500,
     509,   527,   272,   272,   272,   272,   272,   272,   215,   272,
     633,   272,   555,   272,   272,   272,   272,   272,   272,   272,
     215,   272,  3468,   272,   189,   272,   497,   272, -1367, -1367,
   -1367,  3236,  1012,  1013, -1367,  1018,  1022,  1024,  1025, -1367,
    1154,  1026,  1028,  1032,  1036, -1367,   222, -1367,   374,  1377,
   -1367,   191,    56,  1030,  1031,  1929,  1582,  1076, -1367,  1377,
    1377,  1377,  1377, -1367, -1367, -1367, -1367, -1367, -1367,  1377,
    1377,  1377, -1367, -1367, -1367, -1367, -1367, -1367, -1367,   413,
   -1367,   272,   430,   722, -1367, -1367, -1367, -1367,  3377,  1037,
     368, -1367,  1040, -1367, -1367,  1317, -1367,   368, -1367,   368,
     272, -1367, -1367,   191, -1367,  1045, -1367, -1367, -1367,   272,
   -1367,  1042, -1367, -1367,  1049,   619,   272,   272, -1367, -1367,
   -1367, -1367, -1367, -1367,   905,  1048, -1367, -1367,   272, -1367,
     -39,  1054,  1055,  1041,  1056,  1065,  1066,  1068,  1069,  1072,
    1074,  1077,  1078,  1079, -1367,   413, -1367, -1367,   272,   742,
   -1367,   794,  1080,  1082,  1075,  1086,  1083,   272,   272,   272,
     272,   272,   272,   215,   272,  1089,  1088,  1101,  1096,  1103,
    1102,  1104,  1105,  1107,  1110,  1108,  1111,  1113,  1121,  1114,
    1122,  1128,  1127,  1132,  1131,  1133,  1141,  1134,  1143,  1148,
    1149,  1146,  1152,  1367,  1155,  1153, -1367,   531, -1367,   168,
   -1367, -1367,  1099, -1367, -1367,   100,   100, -1367, -1367, -1367,
   -1367,  1929, -1367, -1367,   643, -1367,  1159, -1367,  1439,   502,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367,  2405,  1171, -1367,
   -1367, -1367, -1367,  1172,  1183, -1367,  1929,   575, -1367, -1367,
   -1367, -1367,  1470,    21,   272,   905,  1180,  1182,   413, -1367,
    1184,   272, -1367,  1188,  1191,  1194,  1195,  1196,  1187,  1192,
    1201,  1202,  1260, -1367, -1367, -1367,  1213, -1367,  1216,  1210,
    1207,  1223,  1220,  1228,  1225,  1232,  1226, -1367,  1234, -1367,
    1235, -1367,  1236, -1367,  1237, -1367, -1367,  1238, -1367, -1367,
    1239, -1367,  1240, -1367,  1241, -1367,  1254, -1367,  1255, -1367,
    1261, -1367, -1367,  1263, -1367,  1259, -1367,  1265,  1552, -1367,
    1262,   535, -1367,  1267,  1269, -1367,   100,  1929,   575,  2925,
   -1367, -1367, -1367,   100, -1367,  1266, -1367,  1264,  1270,   266,
   -1367,  3447, -1367,  1271, -1367,   272,   272,   272, -1367, -1367,
   -1367, -1367, -1367,  1274, -1367,  1275, -1367,  1278, -1367,  1280,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367, -1367,  3468, -1367, -1367,  1281,
   -1367,  1266,  1582,  1286,  1277,  1288, -1367,    21, -1367,   905,
   -1367,   149, -1367,  1289,  1290,  1291,   176,    57, -1367, -1367,
   -1367, -1367,    83,    87,   101,   170,   175,   179,   106,   109,
     162,   173,  1881,   148,   477, -1367,   932,  1295,  1544, -1367,
     100, -1367,   635, -1367, -1367, -1367, -1367, -1367, -1367, -1367,
     205,   206,   212,   193, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367,  1583, -1367, -1367,
   -1367,   100,   575,  2387,  1301,   905, -1367, -1367, -1367, -1367,
   -1367,  1302,  1305,  1306, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,
     505,  1346,   100,   272, -1367,  1504,  1320,  1321,   502, -1367,
   -1367,  2925,  1582,  1593,   575,  1266,  1327,   100,  1331, -1367
};

/* YYDEFACT[STATE-NUM] -- Default reduction number in state STATE-NUM.
   Performed when YYTABLE does not specify something else to do.  Zero
   means the default is an error.  */
static const yytype_int16 yydefact[] =
{
       2,     0,     1,    86,   106,     0,   265,   209,   390,     0,
       0,   760,   761,     0,   222,     0,     0,   775,   781,   838,
      93,     0,     0,     0,     0,     0,     0,    29,     0,     0,
       0,     0,    58,    59,     0,    61,     3,    25,    26,    27,
      84,    85,   434,   434,    19,    17,    10,     9,   434,     0,
     109,   136,     0,     7,   272,   336,     8,     0,    18,   434,
       0,    11,    12,     0,     0,     0,     0,   817,    37,    40,
      38,    39,   105,     0,   189,   391,   392,   389,   745,   746,
     747,   748,   749,   750,   751,   752,   753,   754,   755,   756,
     757,   758,   759,     0,     0,    34,   216,   217,     0,     0,
     223,   224,   229,   222,   222,     0,    62,    72,     0,   220,
     215,     0,     0,     0,     0,   781,     0,     0,     0,    94,
      42,    20,    21,    44,    43,    23,    24,   554,   711,     0,
     688,   696,   694,     0,   697,   698,   699,   700,   701,   702,
     707,   708,   709,   710,   671,   695,     0,   687,     0,     0,
       0,   492,     0,   555,   556,   557,     0,     0,   558,     0,
       0,   236,     0,   222,     0,   552,     0,   692,    30,    53,
      55,    56,    57,    60,   436,     0,   435,     0,     0,     2,
       0,     0,   138,   140,   222,     0,     0,   397,   397,   397,
     397,   397,   397,     0,     0,     0,   387,   394,   434,     0,
     763,   791,   809,   827,   841,     0,     0,     0,     0,     0,
       0,   553,   222,   560,   721,   563,    32,     0,     0,   723,
       0,     0,     0,   225,   226,   227,   228,   218,   219,     0,
      74,    73,     0,     0,   104,     0,    22,   776,   777,     0,
     782,   783,   784,   786,     0,   787,   788,   789,   790,   780,
     839,   840,   836,    95,   693,   703,   704,   705,   706,   670,
       0,   673,     0,   689,   691,   234,   235,     0,     0,     0,
       0,     0,     0,   686,   684,     0,     0,     0,   231,     0,
       0,     0,   678,     0,     0,     0,   714,   537,   677,   676,
       0,    30,    54,    65,   437,    69,   103,     0,     0,   112,
     133,   110,   111,   114,   115,     0,   116,   117,   118,   119,
     120,   121,   122,   123,   113,   132,   125,   124,   134,   148,
     137,     0,   108,     0,     0,   278,   273,   274,   275,   276,
     277,   281,   279,   289,   280,   282,   283,   284,   285,   286,
     287,   288,     0,   290,   314,   493,   494,   495,   496,   497,
     498,   499,   500,   501,   502,   503,   504,   505,     0,   372,
       0,   335,   343,   344,     0,     0,     0,     0,   365,     6,
     350,     0,   352,   351,     0,   337,   358,   336,   339,     0,
       0,   345,   507,     0,     0,     0,     0,     0,   222,     0,
       0,     0,   222,     0,     0,   434,   346,   348,   349,     0,
       0,     0,   413,     0,   412,     0,   411,     0,   410,     0,
     408,     0,   409,   433,     0,   396,     0,     0,   722,   772,
     762,     0,     0,     0,     0,     0,     0,   820,   819,   818,
       0,   815,    41,   210,     0,   196,   190,   191,   192,   193,
     198,   199,   200,   201,   195,   202,   203,   194,     0,     0,
     388,     0,     0,     0,     0,     0,   731,   725,   730,     0,
      35,     0,     0,   222,    76,    70,    63,   311,   312,   714,
     313,   535,     0,    97,   778,   774,   807,   785,     0,   672,
     690,   233,     0,     0,     0,     0,     0,   685,   683,    51,
      52,    50,     0,    49,   559,     0,     0,    48,   715,   674,
     716,     0,   712,     0,   538,   539,    28,    31,     5,     0,
     126,   127,   128,   129,   130,   131,   157,   107,   139,   143,
       0,   106,   239,   253,     0,     0,   817,     0,     0,     4,
     181,   182,   175,     0,   141,   171,     0,     0,   336,   172,
     173,   174,     0,     0,   295,     0,   338,   340,     0,     0,
       0,     0,     0,   222,     0,   347,     0,   314,     0,   382,
       0,   380,   383,   366,   368,     0,     0,     0,     0,     0,
       0,     0,   369,   509,   508,   510,   511,    45,     0,     0,
     506,   513,   512,   516,   515,   517,   521,   522,   520,     0,
     523,     0,   524,   434,     0,   528,   530,     0,     0,     0,
       0,     0,     0,     0,     0,     0,   393,     0,     0,   401,
       0,   765,     0,     0,     0,     0,    13,   803,   802,   794,
     792,   795,   434,   434,   814,     0,     0,    14,   434,   812,
     434,   810,     0,     0,     0,     0,    15,   835,   834,   828,
       0,     0,    16,   846,   845,   842,   821,   822,   823,   824,
     825,   826,     0,   564,   205,     0,   561,     0,     0,     0,
     732,    76,     0,     0,     0,   726,    33,     0,   221,   230,
      66,     0,    79,   537,     0,     0,     0,     0,   434,     0,
     837,     0,     0,   550,   548,   549,   677,     0,     0,   718,
     714,   675,   682,     0,     0,     0,   152,   154,   153,   155,
     150,   151,   157,     0,     0,     0,     0,     0,   222,   176,
     177,     0,     0,     0,   222,     0,   140,   242,   256,     0,
     827,     0,   295,     0,     0,   266,     0,     0,     0,   360,
       0,     0,     0,     0,     0,   314,   545,     0,     0,   542,
     543,   364,   381,   367,     0,   384,   374,   378,   379,   377,
     373,   375,   376,     0,     0,     0,     0,   519,     0,     0,
       0,     0,   533,   534,     0,   514,     0,   397,   398,   397,
     397,   397,   397,   397,   395,   400,     0,   764,     0,     0,
       0,   797,   796,     0,     0,   800,     0,     0,     0,     0,
       0,     0,     0,     0,   833,   829,     0,     0,     0,     0,
     618,   572,   573,     0,   607,   574,   575,   576,   577,   578,
     579,   609,   585,   586,   587,   588,   619,     0,     0,   615,
       0,     0,     0,   569,   570,   571,   594,   595,   596,   613,
     597,   598,   599,   600,   619,   619,   603,   621,   611,   617,
     580,   270,     0,     0,   268,     0,   207,   562,     0,   719,
       0,     0,    38,     0,   724,   725,    36,     0,    64,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,    78,    75,   441,   434,    77,
       0,     0,   314,   314,   313,   535,    98,    99,     0,   100,
     101,   102,     0,   808,   232,   551,   314,   679,   680,   717,
     713,   540,   135,     0,   158,   144,   161,     0,   149,   142,
       0,   241,   240,   558,     0,   255,   254,     0,   816,     0,
     184,     0,     0,     0,     0,     0,     0,     0,   167,     0,
     291,     0,     0,     0,   302,   303,   304,   305,   297,   298,
     299,   296,   300,   301,     0,     0,   294,     0,     0,     0,
       0,     0,     0,   355,   353,     0,     0,     0,   207,     0,
     356,     0,   266,   341,   314,     0,     0,   370,   371,     0,
       0,     0,     0,   526,   314,   530,   530,   529,   399,   407,
     406,   405,   404,   402,   403,   769,   767,   793,   804,     0,
     806,   798,   801,   779,   805,   811,   813,     0,   830,   831,
       0,   844,   204,   608,   581,   582,   583,   584,     0,   604,
     610,   612,   616,     0,     0,     0,   614,   601,   602,   625,
     626,     0,   653,   627,   628,   629,   630,   631,   632,   655,
     637,   638,   639,   640,   623,   624,   645,   646,   647,   648,
     649,   650,   651,   652,   622,   656,   657,   658,   659,   660,
     661,   662,   663,   664,   665,   666,   667,   668,   669,   641,
     605,   197,     0,     0,   589,   206,     0,   188,     0,   735,
     736,   740,   738,     0,   737,   734,   733,   720,     0,    79,
     727,    76,    71,    67,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,    82,    83,
      81,     0,     0,     0,   536,     0,     0,     0,     0,    96,
     777,     0,     0,     0,   145,   146,   157,   160,   161,   222,
     187,   237,     0,     0,     0,     0,     0,     0,   168,   222,
     222,   222,   222,   169,   250,   251,   249,   243,   248,   222,
     222,   222,   170,   263,   264,   261,   257,   262,   178,   295,
     293,     0,     0,     0,   315,   316,   317,   318,   564,   148,
       0,   359,     0,   362,   363,     0,   342,   546,   544,     0,
       0,    46,    47,   518,   525,     0,   531,   532,   768,     0,
     766,     0,   832,   843,     0,     0,     0,     0,   654,   633,
     634,   635,   636,   643,     0,     0,   644,   269,     0,   590,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   440,   439,   438,   208,     0,     0,
      79,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,    89,     0,    88,     0,
      87,   432,     0,   214,   213,   314,   314,   773,   681,   156,
     163,     0,   162,   159,     0,   183,     0,   186,     0,   222,
     244,   245,   246,   247,   260,   258,   259,     0,     0,   306,
     307,   308,   309,     0,     0,   354,     0,     0,   547,   385,
     386,   527,   771,     0,     0,     0,     0,     0,   606,   642,
       0,     0,   591,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   728,    68,   431,     0,   430,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   421,     0,   420,
       0,   419,     0,   418,     0,   416,   414,     0,   417,   415,
       0,   429,     0,   428,     0,   427,     0,   426,     0,   447,
       0,   443,   442,     0,   446,     0,   445,     0,     0,    91,
       0,     0,   166,     0,     0,   147,   314,     0,     0,     0,
     292,   310,   267,   314,   361,   164,   770,     0,     0,     0,
     567,   564,   593,     0,   739,     0,     0,     0,   744,   729,
     481,   477,   425,     0,   424,     0,   423,     0,   422,     0,
     479,   477,   475,   473,   467,   470,   479,   477,   475,   473,
     490,   483,   444,   486,    90,    92,     0,   212,   211,     0,
     185,   164,     0,     0,     0,     0,   165,     0,   620,     0,
     566,   568,   592,     0,     0,     0,     0,     0,   479,   477,
     475,   473,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,    80,   207,     0,     0,   319,
     314,   799,     0,   741,   742,   743,   463,   482,   462,   478,
       0,     0,     0,     0,   453,   480,   452,   451,   476,   450,
     474,   448,   469,   468,   449,   472,   471,   457,   456,   455,
     454,   466,   491,   485,   484,   464,   487,     0,   465,   489,
     252,   314,     0,     0,     0,     0,   461,   460,   459,   458,
     488,     0,     0,     0,   324,   320,   329,   330,   331,   332,
     333,   321,   322,   323,   325,   326,   327,   328,   271,   357,
       0,     0,   314,     0,   565,     0,     0,     0,   222,   179,
     334,     0,     0,     0,     0,   164,     0,   314,     0,   180
};

/* YYPGOTO[NTERM-NUM].  */
static const yytype_int16 yypgoto[] =
{
   -1367,  1443, -1367,  1336,   -72,    32,   -41,    -5,    10,    22,
    -358, -1367,    13,   -18,  1603, -1367, -1367,  1166,  1243,  -640,
   -1367,  -975, -1367,    26, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367,  -313, -1367, -1367, -1367,   916, -1367, -1367,
   -1367,   451, -1367,   929,   498,   499, -1367, -1366,  -437, -1367,
    -312, -1367, -1367,  -942, -1367,  -162,   -98, -1367,    35,  1613,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,   677,
     462, -1367,  -311, -1367,  -702,  -667,  1297, -1367, -1367,  -243,
   -1367,  -141, -1367, -1367,  1081, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367,   328,     7, -1367, -1367, -1367,  1035,  -150,
    1586,   578,   -40,   -30,   805, -1367, -1058, -1367, -1367, -1324,
   -1299, -1192, -1269, -1367, -1367, -1367, -1367,    23, -1367, -1367,
   -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,
   -1367, -1367, -1367,   -27,   768,   982, -1367,  -688, -1367,   692,
     -22,  -405,   -74,   239,   130, -1367,   -23,   538, -1367,   984,
       3,   811, -1367, -1367,   808, -1367, -1049, -1367,  1661, -1367,
      36, -1367, -1367,   545,  1205, -1367,  1566, -1367, -1367,  -961,
    1272, -1367, -1367, -1367, -1367, -1367, -1367, -1367, -1367,  1160,
     975, -1367, -1367, -1367, -1367, -1367
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
      72,   278,   166,   164,    58,   227,   228,   159,    56,   885,
     533,   535,   538,   177,    39,   119,  1184,   198,   178,   214,
     932,   851,   219,   169,   170,   171,   172,    44,  1201,   199,
     120,   121,   122,    37,   674,  1243,   209,    61,   404,   406,
     408,   410,   412,   294,   376,   125,   126,   961,   163,   110,
     216,   604,   216,   605,  1277,    68,    69,  -558,    70,   120,
     161,    68,    69,   777,    70,   279,   120,   960,   419,   420,
     200,   845,    68,    69,  1198,    70,   217,   120,  1000,   683,
     684,   685,   120,   207,   744,   233,   344,   120,   498,   216,
      68,    69,   120,    70,   120,  1497,   120,   292,   216,   216,
      68,    69,   478,    70,  1241,   498,   254,   320,    99,   238,
     120,   249,   252,   253,   453,   120,    68,    69,   120,    70,
     736,   550,   235,   236,   259,  1491,    99,   261,   163,   267,
      68,    69,   679,    70,   558,   712,    99,   931,    68,    69,
     161,    70,    68,    69,   371,    70,   291,    68,    69,  1490,
      70,   270,   273,   556,   198,   120,  1533,  1513,   206,   472,
     466,   282,   283,   284,    67,   516,   498,   274,   417,    68,
      69,   120,    70,    68,    69,   319,    70,  1488,   216,   123,
     124,  1512,   123,   124,   120,   449,   577,    75,   123,   124,
     577,   271,  1534,   482,  1125,  1126,    96,  1279,   110,    97,
     431,   432,   123,   124,   193,   452,   578,   579,  1131,  1510,
     578,   579,    73,    68,   120,   120,    70,   120,   120,  1586,
    1014,   120,    98,    99,   123,   124,   585,   100,   470,   101,
     690,   461,   462,  1280,   476,   474,   102,   691,   479,   572,
    1015,  1069,  1070,    68,    69,   271,    70,   519,    76,  1483,
     557,   684,  1341,   103,    95,  1489,   492,  1147,   591,   206,
    1342,   695,   481,   483,   505,  1352,   111,   487,   104,   291,
     489,   490,   491,   696,   697,   493,   494,   895,   457,   112,
     497,   120,   488,   458,   268,  1179,  1195,  1511,   793,   206,
     113,   698,   459,   554,   594,   719,   500,   206,   168,   551,
      58,   206,   451,   485,    56,   123,   124,   577,   206,   552,
      39,   400,   451,   713,   577,   401,   590,   368,   459,   592,
     115,  1217,   173,    44,   848,   781,   540,   578,   579,    37,
     539,   206,   174,    61,   578,   579,   531,   206,   206,   570,
     218,   699,   573,   656,  -558,   181,   583,   778,   118,   532,
     220,   593,  1508,   421,   286,   530,   287,   272,  1199,   541,
     288,   289,   206,   179,   422,   597,   589,   499,   546,  1218,
     547,   269,  1417,    68,   548,   608,    70,  1219,  1514,   784,
     464,  1081,  1516,   663,   499,   465,  1082,   403,   998,   269,
     571,   401,   183,   574,   575,  -541,  1517,   584,  1495,   269,
     206,  1527,   262,   263,  1528,   201,   682,   576,   581,   728,
     599,   600,   264,   601,   206,   602,   163,   376,   472,   905,
     847,   687,   688,   110,   588,   749,   619,  1507,   161,   603,
     657,   610,   611,   202,  1532,   730,   617,   617,   637,   643,
     919,  1242,   895,  1535,   654,   237,   655,  1063,  1127,   618,
     618,   638,   644,  1064,   715,   733,   203,  1529,   204,   947,
     948,   949,   921,   232,   452,  1519,  1400,  1317,  1530,   260,
    1521,  1506,   722,   680,  1524,   221,   105,    68,    69,   500,
      70,   120,    68,    69,   206,    70,   742,   470,  1549,   222,
     127,    68,    69,   216,    70,   120,   234,   368,   110,   127,
    1546,  1547,    68,    69,   271,    70,  1501,  1548,    68,   275,
     127,    70,   120,   700,  1068,   758,   459,   371,    99,   694,
     978,  1536,   725,   276,   701,   747,   766,    99,  1537,   957,
     206,   282,   283,   284,   709,   710,    68,    69,    99,    70,
     277,  1282,    98,   785,   127,   729,   731,   100,  1283,   101,
     280,  1069,  1070,   721,  1540,   762,   102,   459,  1469,   727,
     281,  1470,   732,   760,   123,   124,   577,   198,  1319,  1320,
     405,   761,    99,   103,   401,   407,   748,   293,   376,   401,
      68,    69,  1398,    70,   757,   409,   578,   579,   104,   401,
     580,   294,   786,   787,    68,    69,   295,    70,   790,     3,
     791,   559,   127,   560,   561,   562,   763,  1403,  1404,   296,
     917,   411,   533,   535,   538,   401,   923,   979,   321,   980,
     981,   982,   983,   984,   454,    68,    69,   455,    70,   795,
      99,   909,   910,   914,  1213,   797,   153,   154,   155,   399,
     259,   413,   120,   794,   577,   153,   154,   155,   892,  1084,
     505,   951,   416,  1085,   418,  1214,   153,   154,   155,  1086,
    1415,  1215,   799,  1087,   578,   579,  1136,   206,  1216,  1092,
     901,   884,   460,  1093,   893,  1094,   894,   450,   371,  1095,
      68,    69,  1096,   852,   433,   875,  1097,   972,   127,  1098,
     153,   154,   155,  1099,   467,   468,   456,  1100,   906,   899,
     500,  1101,   913,   965,   459,  1102,   918,   920,   922,  1103,
     959,  1104,   967,  1106,   962,  1105,    99,  1107,  1459,  1108,
     240,   241,   242,  1109,   286,  1463,   287,   463,  1110,   884,
     288,   289,  1111,  1112,   950,  1114,   953,  1113,   955,  1115,
     956,  1461,  1264,  1116,  1267,   243,   473,  1117,   153,   154,
     155,   120,   186,   477,   966,   187,   188,   189,   190,  -238,
     191,   192,   193,   206,   968,   969,   480,   459,  1348,  1349,
    1350,  1194,  1538,  1145,   206,   156,  1071,    14,   970,  1247,
     506,   663,   484,  1248,   156,  1083,   985,   640,   986,   987,
     641,   999,    98,  1001,  1249,   156,   459,   100,  1250,   101,
    1574,   495,   997,  1251,  1065,   186,   102,  1252,   187,   188,
     189,   190,  1544,   191,   192,   193,  1003,  1004,  1005,  1006,
    1007,  1253,   486,   103,   915,  1254,  1073,   549,   206,   156,
     496,  1135,  1456,   509,   153,   154,   155,   543,   104,   244,
     555,   245,   246,   247,   248,  1069,  1070,  1129,  1122,   606,
     282,   283,   284,  1551,    68,    69,   544,    70,  1072,  1169,
    1321,  1322,    28,    29,    30,    31,    32,    33,    34,   916,
     223,   642,   224,   225,   226,  1552,   596,    35,   510,   511,
     512,   285,   598,  1183,  1576,  1185,   607,   156,     3,  1120,
    1123,   609,  1140,   652,  1142,   750,   751,   752,  1118,  1588,
    1146,   653,  1138,  1119,  1208,  1209,  1210,  1211,  1212,  1141,
     459,  1335,   216,  1132,   513,   514,   515,  1585,   661,   884,
     255,   256,   257,   258,   250,   251,   459,  1545,   540,  1143,
     666,  1144,   539,   667,   762,   762,   206,  1406,   531,  1155,
    1164,  1205,   637,   646,   647,   648,   668,   884,  1196,  1197,
     669,   532,  1156,  1165,   673,   638,   676,   530,  1154,  1163,
     689,   541,  1158,  1167,  1017,  1018,  1193,  1088,  1089,  1090,
    1091,   625,     3,   156,   681,   692,   282,   283,   284,   649,
     650,   651,   693,   705,   706,   763,   763,  1310,  1311,  1312,
    1313,   716,    68,    69,  1235,    70,   717,  1314,  1315,  1316,
     586,   128,   587,   718,   720,   129,   130,   131,   132,   133,
    1203,   134,   135,   136,   137,   726,   138,   139,   194,   734,
     140,   141,   142,   143,   886,   887,    99,   144,   145,   735,
     753,   282,   283,   284,   195,   754,   146,   755,   147,  1149,
    1150,  1151,  1152,   286,   756,   287,   765,   759,   768,   288,
     289,   767,   769,   148,   149,   150,   770,   771,   776,   888,
     772,   773,   775,    11,    12,    13,    14,  1343,  1344,  1345,
    1346,  1307,  1308,  1220,   779,    28,    29,    30,    31,    32,
      33,    34,   780,   788,  1353,   789,   792,   195,   151,     3,
      35,   796,   798,   849,  1284,  1244,  1245,  1246,   850,   853,
     854,   855,  1255,  1256,  1257,  1258,  1259,  1260,   856,  1262,
    1263,  1265,   632,  1268,  1269,  1270,  1271,  1272,  1273,  1274,
    1261,  1276,   857,  1278,  1266,  1281,   858,  1285,  1523,  1526,
       9,    10,  1275,   882,   883,    68,    69,  1304,    70,  1325,
     896,   903,  1338,   897,   908,   898,  1328,     3,  1329,   902,
      14,    28,    29,    30,    31,    32,    33,    34,   924,   930,
    1153,   963,   612,   971,   613,   974,    35,   614,   615,   286,
     632,   287,   954,   964,   110,   288,   289,   958,   973,   975,
     976,  1318,   401,   988,   110,   110,   110,   110,   977,   989,
     282,   283,   284,   990,   110,   110,   110,   991,   889,   890,
    1330,   891,   992,   427,   993,   428,   429,   994,  1008,  1332,
     995,  1409,   430,   996,  1002,  1011,  1336,  1337,  1010,  1013,
    1012,  1016,  1061,  1077,   286,   633,   287,  1405,  1340,  1066,
     288,   289,  1078,  1079,  1130,    28,    29,    30,    31,    32,
      33,    34,  1139,  1124,   616,  1171,  1172,  1178,  1347,  1351,
      35,  1173,  1414,  1180,  1120,  1174,  1175,  1359,  1360,  1361,
    1362,  1363,  1364,  1419,  1366,  1399,  1176,    14,  1177,  1181,
    1182,   736,  1186,  1189,  1190,  1365,  1200,   634,    68,    69,
     635,    70,  1204,   633,  1401,  1191,   127,  1192,  1202,   128,
    1240,  1238,   157,   129,   130,   131,   132,   133,  1239,   134,
     135,   136,   137,  1206,   138,   139,  1207,  1291,   140,   141,
     142,   143,  1292,  1293,    99,   144,   145,  1294,  1295,  1296,
     474,  1298,   884,  1299,   146,    14,   147,  1300,  1301,  1305,
    1306,  1309,  1326,  1460,  1118,   634,  1327,   516,   635,  1119,
    1331,   148,   149,   150,  1418,  1333,  1334,  1339,  1084,  1086,
    1092,  1423,    28,    29,    30,    31,    32,    33,    34,  1094,
    1096,   636,  1098,  1100,  1118,  1120,  1102,    35,  1104,  1119,
    1354,  1106,  1108,  1110,  1356,  1395,   151,  1355,   282,   283,
     284,  1357,  1358,   286,  1367,   287,  1462,  1368,  1498,   686,
     289,    96,   467,   468,    97,  1370,  1369,  1502,  1371,  1373,
    1402,  1372,  1375,   884,  1374,  1376,  1378,  1377,  1379,  1381,
      28,    29,    30,    31,    32,    33,    34,    98,    99,  1168,
    1380,  1382,   100,  1383,   101,    35,  1384,  1385,  1387,  1389,
    1386,   102,   153,   154,   155,  1473,  1474,  1475,    68,    69,
    1388,    70,  1390,  1391,  1392,  1393,   127,  1394,   103,   128,
    1396,  1407,  1397,   129,   130,   131,   132,   133,  1408,   134,
     135,   136,   137,   104,   138,   139,  1411,  1412,   140,   141,
     142,   143,  1539,  1570,    99,   144,   145,  1413,  1416,  1420,
    1581,  1421,  1247,  1422,   146,  1249,   147,  1509,  1251,  1253,
    1425,  1424,  1515,  1509,  1518,  1426,  1522,  1428,  1515,  1509,
    1518,   148,   149,   150,  1427,  1432,  1433,  1430,  1583,  1525,
    1431,    28,    29,    30,    31,    32,    33,    34,  1434,  1435,
    1515,  1509,  1518,  1436,  1437,  1439,    35,  1438,  1440,  1441,
    1442,  1443,  1444,  1445,  1446,  1447,   151,   884,   282,   283,
     284,    28,    29,    30,    31,    32,    33,    34,  1448,  1449,
    1429,     3,   467,   468,  1452,  1450,    35,  1451,  1582,  1453,
    1454,  1455,  1457,  1542,  1458,  1468,  1464,  1467,  1478,  1479,
    1472,   469,  1480,   287,  1481,   880,  1496,   288,   289,   884,
     157,  1499,  1500,  1577,  1503,  1504,  1505,    68,    69,  1541,
      70,  1550,   153,   154,   155,   127,  1569,  1571,   128,  1572,
    1573,  1575,   129,   130,   131,   132,   133,  1578,   134,   135,
     136,   137,  1584,   138,   139,  1579,  1580,   140,   141,   142,
     143,  1587,   297,    99,   144,   145,  1589,   507,   160,   670,
    1324,   904,   925,   146,  1302,   147,   595,  1303,   162,  1187,
    1323,   545,   774,   197,  1236,   743,    68,    69,  1062,    70,
     148,   149,   150,  1128,   127,   881,  1188,   128,  1076,  1290,
    1471,   129,   130,   131,   132,   133,  1080,   134,   135,   136,
     137,    94,   138,   139,   900,  1297,   140,   141,   142,   143,
     678,   239,    99,   144,   145,   151,   711,   282,   283,   284,
       0,     0,   146,     0,   147,   929,   629,     0,     0,     0,
       0,     0,     0,     0,     0,  1159,     0,  1160,  1161,   148,
     149,   150,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,    11,    12,    13,    14,
       0,   469,     0,   287,     0,     0,     0,   686,   289,     0,
     157,   153,   154,   155,   151,   152,    68,    69,     0,    70,
       0,     0,     0,     0,   127,     0,     0,   128,     0,     0,
       0,   129,   130,   131,   132,   133,     0,   134,   135,   136,
     137,     0,   138,   139,     0,     0,   140,   141,   142,   143,
       0,     0,    99,   144,   145,     0,     0,     0,     0,     0,
       0,     0,   146,     0,   147,     0,     0,     0,     0,     0,
     153,   154,   155,     0,     0,     0,     0,     0,     0,   148,
     149,   150,     0,   911,    28,    29,    30,    31,    32,    33,
      34,     0,     0,  1162,     0,     0,     0,     0,     0,    35,
      68,    69,     0,    70,     0,     0,     0,    14,   127,     0,
       0,   128,     0,     0,   151,   129,   130,   131,   132,   133,
       0,   134,   135,   136,   137,     0,   138,   139,   912,     0,
     140,   141,   142,   143,     0,     0,    99,   144,   145,     0,
       0,     0,     0,     0,     0,     0,   146,     0,   147,     0,
     469,     0,   287,     0,     0,     0,   288,   289,     0,   157,
       0,     0,     0,   148,   149,   150,     0,     0,     0,     0,
     153,   154,   155,   859,   860,   861,     0,   862,   863,   864,
     865,     0,   866,   867,   193,     0,   868,   869,   870,   871,
       0,     0,     0,   872,   873,     0,     0,     0,   151,   152,
       0,     0,     0,     0,    68,    69,     0,    70,     0,   156,
       0,     0,   127,     0,     0,   128,     0,     0,   157,   129,
     130,   131,   132,   133,     0,   134,   135,   136,   137,     0,
     138,   139,     0,     0,   140,   141,   142,   143,     0,     0,
      99,   144,   145,     0,     0,     0,     0,     0,     0,     0,
     146,     0,   147,     0,   153,   154,   155,     0,     0,     0,
       0,   874,     0,     0,     0,     0,     0,   148,   149,   150,
       0,     0,     0,     0,    68,    69,     0,    70,     0,     0,
       0,     0,   127,     0,     0,   128,     0,     0,     0,   129,
     130,   131,   132,   133,     0,   134,   135,   136,   137,     0,
     138,   139,   151,     0,   140,   141,   142,   143,     0,   210,
      99,   144,   145,     0,   933,     0,     0,     0,   157,     0,
     146,   934,   147,   935,   936,   937,     0,     0,     0,     0,
       0,     0,     2,     0,     0,     0,     0,   148,   149,   150,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       3,     0,     0,     0,     0,     0,     0,     0,   153,   154,
     155,     0,   938,   939,   940,     0,     0,     0,    68,    69,
       0,    70,   553,     0,     0,     0,   127,     0,     0,   128,
       0,     0,     0,   129,   130,   131,   132,   133,     0,   134,
     135,   136,   137,   210,   138,   139,     0,     0,   140,   141,
     142,   143,   157,     0,    99,   144,   145,     0,   941,   942,
     943,     0,   944,     0,   662,   945,   147,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,   153,   154,
     155,   148,   149,   150,     0,     0,     0,    68,    69,     0,
      70,     0,     0,     0,     0,   127,  1531,     0,   128,     0,
       0,     0,   129,   130,   131,   132,   133,     0,   134,   135,
     136,   137,     0,   138,   139,     0,   151,   140,   141,   142,
     143,     0,     0,    99,   144,   145,     0,     0,     0,     0,
       0,     0,     0,   146,     0,   147,     4,     5,     6,     7,
       8,     0,     0,     0,     0,     0,     0,   210,     0,     0,
     148,   149,   150,     0,     0,   933,   157,     0,     9,    10,
       0,     0,   934,     0,   935,   936,   937,     0,     0,     0,
       0,     0,   153,   154,   155,    11,    12,    13,    14,     0,
       0,     0,    15,    16,     0,   714,     0,     0,    17,     0,
       0,    18,     0,     0,    68,     0,     0,    70,    19,    20,
       0,     0,     0,   938,   939,   940,     0,     3,   216,     0,
       0,     0,     0,     0,     0,     0,     0,   210,     0,     0,
     946,     0,     0,  1221,  1222,  1223,   157,  1224,  1225,  1226,
    1227,     0,  1228,  1229,   193,     0,  1230,  1231,  1232,  1233,
       0,   153,   154,   155,     0,  1234,     0,     0,     0,   941,
     942,   943,     0,   944,    21,    22,   945,    23,    24,    25,
       0,    26,    27,    28,    29,    30,    31,    32,    33,    34,
       0,     0,     0,     0,   265,   128,   266,     0,    35,   129,
     130,   131,   132,   133,     0,   134,   135,   136,   137,     0,
     138,   139,     0,     0,   140,   141,   142,   143,     0,     0,
       0,   144,   145,     0,     0,     0,     0,     0,     0,     0,
     146,   210,   147,     0,     0,     0,     0,     0,     0,     0,
     157,     0,     0,     0,     0,     0,     0,   148,   149,   150,
     345,   346,   347,   348,   349,   350,   351,   352,   353,   354,
     355,   356,   357,     0,     0,     0,  1553,     8,     0,     0,
       0,   358,   359,   360,   361,   362,   363,    68,     0,     0,
      70,  1554,   151,     0,   933,     9,    10,     0,     0,     0,
       3,   934,     0,   935,   936,   937,     0,  1555,     0,     0,
     210,     0,    11,    12,    13,    14,  1556,     0,     0,   157,
       0,     0,     0,   364,     0,     0,     0,     0,     0,     0,
    1557,  1558,  1559,  1560,     0,     0,     0,   365,     0,     0,
       0,  1170,   938,   939,   940,    78,    79,    80,    81,    82,
      83,    84,    85,    86,    87,    88,    89,    90,    91,    92,
       0,     0,  1561,  1562,  1563,  1564,  1565,  1566,  1567,     0,
       0,    68,   366,   367,    70,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     3,     0,     0,     0,   941,   942,
     943,     0,   944,     0,     0,   945,     0,     0,     0,     0,
      28,    29,    30,    31,    32,    33,    34,     0,   368,   369,
       0,     0,     0,     0,     0,    35,     0,     0,     0,     0,
       0,     0,     0,   345,   346,   347,   348,   349,   350,   351,
     352,   353,   354,   355,   356,   357,     0,     0,     0,     0,
       8,     0,     0,     0,   358,   359,   360,   361,   362,   363,
       0,     0,     0,     0,     0,     0,     0,     0,     9,    10,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,    11,    12,    13,    14,     0,
       0,     0,     0,     0,     0,     0,   364,     0,     0,     0,
       0,     0,     0,     0,     0,     0,   157,     0,     0,     0,
     365,     0,     0,     0,     0,     0,     0,   345,   346,   347,
     348,   349,   350,   351,   352,   353,   354,   355,   356,   357,
       0,     0,     0,     0,     8,     0,  1568,     0,   358,   359,
     360,   361,   362,   363,     0,   366,   367,     0,     0,     0,
       0,     0,     9,    10,     0,     0,     0,     0,     0,     0,
    1410,     0,     0,     0,     0,     0,     0,     0,     0,    11,
      12,    13,    14,    28,    29,    30,    31,    32,    33,    34,
     364,   368,   741,     0,     0,     0,     0,     0,    35,     0,
       0,   128,     0,     0,   365,   129,   130,   131,   132,   133,
       0,   134,   135,   136,   137,     0,   138,   139,     0,     0,
     140,   141,   142,   143,   434,     0,     0,   144,   145,     0,
       0,     0,     0,     0,     0,     0,   146,     0,   147,   366,
     367,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   148,   149,   150,     0,   435,     0,   436,
     437,   438,   439,     0,     0,     0,     0,    28,    29,    30,
      31,    32,    33,    34,     0,   368,   928,     0,     0,     0,
       0,     0,    35,     0,     0,     0,     0,     0,   151,     0,
       0,     0,     0,     0,     0,     0,   440,   441,   442,   443,
       0,     0,   444,     0,     0,   128,   445,   446,   447,   129,
     130,   131,   132,   133,     0,   134,   135,   136,   137,     0,
     138,   139,     0,     0,   140,   141,   142,   143,     0,     0,
       0,   144,   145,     0,     0,     0,     0,     0,     0,     0,
     146,     0,   147,     0,     0,     0,     0,     0,     0,     0,
       3,     0,     0,     0,     0,     0,     0,   148,   149,   150,
     128,     0,     0,     0,   129,   130,   131,   132,   133,     0,
     134,   135,   136,   137,     0,   138,   139,     0,     0,   140,
     141,   142,   143,     0,     0,     0,   144,   145,     0,     0,
       0,     0,   151,     0,     0,   146,     0,   147,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
     448,   128,   148,   149,   150,   129,   130,   131,   132,   133,
       0,   134,   135,   136,   137,     0,   138,   139,     0,     0,
     140,   141,   142,   143,     0,     0,     0,   144,   145,     0,
       0,     0,     0,     0,     0,     0,   146,   151,   147,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   148,   149,   150,     0,     0,  1019,  1020,
       0,  1021,  1022,  1023,  1024,  1025,  1026,     0,  1027,  1028,
       0,  1029,  1030,  1031,  1032,  1033,     4,     5,     6,     7,
       8,     3,   157,     0,     0,     0,     0,     0,   151,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     9,    10,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,    11,    12,    13,    14,     0,
       0,     0,    15,    16,     0,     0,     0,     0,    17,     0,
     520,    18,     0,     0,     0,     0,     0,     0,    19,    20,
     859,   860,   861,     0,   862,   863,   864,   865,     0,   866,
     867,   193,     0,   868,   869,   870,   871,     0,     0,     0,
     872,   873,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   724,     0,     0,
       0,     0,     0,     0,     0,     0,   157,     0,     0,     0,
       0,     0,     0,     0,    21,    22,     0,    23,    24,    25,
       0,    26,    27,    28,    29,    30,    31,    32,    33,    34,
       0,     0,   508,     0,     0,     0,     3,   521,    35,     6,
       7,     8,     0,     0,     0,     0,     0,     0,   874,     0,
       0,   522,   880,     0,     0,     0,   523,     0,     0,     9,
      10,   157,     3,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,    11,    12,    13,    14,
       0,   524,   525,     0,     0,   520,  1034,  1035,     0,  1036,
    1037,  1038,     0,  1039,  1040,     0,     0,  1041,  1042,     0,
    1043,   526,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   157,  1044,  1045,  1046,  1047,  1048,  1049,  1050,
    1051,  1052,  1053,  1054,  1055,  1056,  1057,  1058,   624,     0,
       0,     0,   128,     0,     0,     0,   527,   528,   131,   132,
     133,     0,   134,   135,   136,   137,     0,   138,   139,     0,
       0,   140,   141,   142,   143,     0,     0,     0,  1286,   145,
       0,  1059,     0,     0,    28,    29,    30,    31,    32,    33,
      34,     0,   521,   529,     6,     7,     8,     0,     0,    35,
       0,     0,     0,     0,     0,     0,   522,     0,     0,     0,
       0,   523,     0,     0,     9,    10,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,  1287,     0,     0,
       0,    11,    12,    13,    14,     0,   524,   525,     0,    28,
      29,    30,    31,    32,    33,    34,  1288,     0,     0,     0,
       0,     0,     0,     0,    35,     0,   526,     0,     0,     0,
      14,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   625,     0,   613,   626,     0,   614,   615,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,   527,   528,   800,     0,     0,     0,     0,   801,   802,
       0,   803,   804,   805,   806,   807,   808,     0,   809,   810,
       0,   811,   812,   813,   814,   815,     0,     0,     0,    28,
      29,    30,    31,    32,    33,    34,     0,     0,  1148,     0,
       0,     0,     0,     0,    35,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,    28,    29,    30,    31,    32,
      33,    34,     0,     0,   627,     0,     0,   816,     0,   817,
      35,     0,     0,   800,   818,     0,     0,     0,   801,   802,
       0,   803,   804,   805,   806,   807,   808,     0,   809,   810,
     819,   811,   812,   813,   814,   815,    68,    69,     0,    70,
     859,   860,   861,     0,   862,   863,   864,   865,     0,   866,
     867,   193,     0,   868,   869,   870,   871,     0,     0,     0,
     872,   873,     0,   820,     0,     0,     0,     0,     0,     0,
     298,     0,     0,     0,     0,     0,     0,   816,     0,   817,
       0,     0,     0,     0,   818,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,   299,
     819,     0,     0,   300,     0,     0,   301,   302,     0,     0,
       0,   303,   304,   305,   306,   307,   308,   309,   310,   311,
     312,   313,   314,     0,     0,     0,     0,     0,   874,   315,
       0,     0,   316,   820,     0,     0,     0,     0,     0,   317,
       0,     0,     0,     0,     0,     0,     0,     0,   318,     0,
       0,     0,     0,   821,     0,   822,   823,   824,   825,   826,
     827,   828,   829,   830,   831,   832,   833,   834,   835,   836,
     837,   838,     0,     0,     0,   839,     0,     0,     0,     0,
       0,     0,     0,     0,   840,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,   841,     0,     0,     0,
       0,     0,     0,   821,     0,   822,   823,   824,   825,   826,
     827,   828,   829,   830,   831,   832,   833,   834,   835,   836,
     837,   838,   324,    98,     0,   839,     0,     0,   100,     0,
     101,     0,     0,     0,   840,     0,     0,   102,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   103,   325,     0,   326,   327,   328,
     329,   330,     0,     0,     0,     0,   331,     0,     0,   104,
       0,     0,     0,     0,     0,   332,     0,     0,     0,     0,
     333,     0,   334,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   335,   336,   337,   338,   339,   340,
     341,   342,     0,     0,     0,     0,     0,   343
};

static const yytype_int16 yycheck[] =
{
       5,   163,    25,    25,     1,   103,   104,    25,     1,   676,
     323,   323,   323,    43,     1,    20,   958,    57,    48,    93,
     722,   661,    94,    28,    29,    30,    31,     1,   989,    59,
       9,    21,    22,     1,   471,  1084,    77,     1,   188,   189,
     190,   191,   192,    12,   185,    23,    24,   735,    25,    14,
       7,   409,     7,   411,  1112,     5,     6,    19,     8,     9,
      25,     5,     6,     8,     8,   163,     9,   734,     7,     8,
      60,    63,     5,     6,     8,     8,    33,     9,    63,   484,
     485,   486,     9,    73,    56,   108,   184,     9,    20,     7,
       5,     6,     9,     8,     9,  1461,     9,   169,     7,     7,
       5,     6,   136,     8,  1079,    20,   129,   181,    41,   114,
       9,   116,   117,   118,   212,     9,     5,     6,     9,     8,
      20,   110,   112,   113,   146,  1449,    41,   149,   105,   152,
       5,     6,   136,     8,   377,   110,    41,   136,     5,     6,
     105,     8,     5,     6,   185,     8,   168,     5,     6,  1448,
       8,   156,   157,   197,   194,     9,     8,  1481,   293,   233,
     232,   105,   106,   107,   215,   300,    20,   157,   198,     5,
       6,     9,     8,     5,     6,   180,     8,  1446,     7,     9,
      10,  1480,     9,    10,     9,   208,    11,    64,     9,    10,
      11,   156,    44,   267,   882,   883,    14,     8,   163,    17,
     205,   206,     9,    10,    33,   210,    31,    32,   896,  1478,
      31,    32,   298,     5,     9,     9,     8,     9,     9,  1585,
     227,     9,    40,    41,     9,    10,   388,    45,   233,    47,
     292,   221,   222,    44,   239,   166,    54,   299,   260,   380,
     247,    65,    66,     5,     6,   210,     8,   321,   125,  1441,
     294,   656,   291,    71,     8,  1447,   279,   924,    87,   293,
     299,    39,   267,   268,   287,  1240,   297,   272,    86,   291,
     275,   276,   277,    51,    52,   280,   281,   682,   289,   298,
     285,     9,   272,   294,   217,   952,   974,  1479,   215,   293,
     274,    69,   291,   367,   392,   538,   286,   293,   289,   288,
     297,   293,   217,   299,   297,     9,    10,    11,   293,   298,
     297,   294,   217,   288,    11,   298,   390,   289,   291,   391,
     215,   290,     7,   297,   297,   304,   323,    31,    32,   297,
     323,   293,    12,   297,    31,    32,   323,   293,   293,   380,
     297,   119,   383,   299,   306,    60,   387,   292,   215,   323,
     292,   391,   295,   292,   298,   323,   300,   307,   292,   323,
     304,   305,   293,   289,   303,   395,   389,   299,   358,   291,
     360,   304,  1333,     5,   364,   416,     8,   299,   295,   297,
     289,   289,   295,   457,   299,   294,   294,   294,   793,   304,
     380,   298,   289,   383,   384,   295,   295,   387,  1456,   304,
     293,   295,    24,    25,   295,   289,   299,   385,   386,   298,
     400,   401,    34,   403,   293,   405,   393,   558,   492,   294,
     299,   495,   496,   388,   389,   566,   423,  1476,   393,   407,
     453,   421,   422,   289,  1492,   298,   423,   424,   425,   426,
     298,  1081,   847,   295,   449,   207,   451,   298,   885,   423,
     424,   425,   426,   304,   528,   553,   289,   295,   289,   250,
     251,   252,   298,   297,   469,   295,   298,  1169,   295,    51,
     295,   295,   544,   478,   295,   294,   294,     5,     6,   469,
       8,     9,     5,     6,   293,     8,   560,   492,   295,   294,
      13,     5,     6,     7,     8,     9,   294,   289,   463,    13,
     295,   295,     5,     6,   469,     8,  1467,   295,     5,   136,
      13,     8,     9,   291,    28,   589,   291,   558,    41,   509,
     295,    44,   545,   136,   302,   566,   598,    41,    51,   292,
     293,   105,   106,   107,   524,   525,     5,     6,    41,     8,
     136,    44,    40,   615,    13,   550,   551,    45,    51,    47,
     136,    65,    66,   543,  1496,   596,    54,   291,   292,   549,
     306,   295,   552,   593,     9,    10,    11,   607,   138,   139,
     294,   594,    41,    71,   298,   294,   566,   295,   719,   298,
       5,     6,    51,     8,   589,   294,    31,    32,    86,   298,
     294,    12,   622,   623,     5,     6,   295,     8,   628,    18,
     630,    55,    13,    57,    58,    59,   596,  1295,  1296,   295,
     708,   294,   925,   925,   925,   298,   714,   767,    61,   769,
     770,   771,   772,   773,   294,     5,     6,   297,     8,   634,
      41,   705,   706,   707,   271,   640,   159,   160,   161,   304,
     662,   294,     9,   633,    11,   159,   160,   161,   678,   294,
     673,   225,   294,   298,   295,   292,   159,   160,   161,   294,
    1327,   298,   652,   298,    31,    32,   292,   293,   305,   294,
     693,   676,     8,   298,   679,   294,   681,   297,   719,   298,
       5,     6,   294,     8,   299,   672,   298,   759,    13,   294,
     159,   160,   161,   298,   119,   120,   294,   294,   703,   689,
     690,   298,   707,   744,   291,   294,   711,   712,   713,   298,
     733,   294,   753,   294,   737,   298,    41,   298,  1406,   294,
     100,   101,   102,   298,   298,  1413,   300,   295,   294,   734,
     304,   305,   298,   294,   724,   294,   726,   298,   728,   298,
     730,  1408,  1100,   294,  1102,   125,   299,   298,   159,   160,
     161,     9,    23,   220,   744,    26,    27,    28,    29,   289,
      31,    32,    33,   293,   754,   755,    25,   291,    26,    27,
      28,   295,   295,   292,   293,   298,   848,   196,   756,   294,
     290,   855,   299,   298,   298,   857,   776,   206,   778,   779,
     209,   796,    40,   798,   294,   298,   291,    45,   298,    47,
     295,   294,   792,   294,   845,    23,    54,   298,    26,    27,
      28,    29,  1500,    31,    32,    33,    25,    26,    27,    28,
      29,   294,   299,    71,    72,   298,   848,   298,   293,   298,
     294,   905,   297,   294,   159,   160,   161,   294,    86,   219,
     303,   221,   222,   223,   224,    65,    66,   888,   878,   290,
     105,   106,   107,  1541,     5,     6,   294,     8,   848,   931,
     138,   139,   281,   282,   283,   284,   285,   286,   287,   117,
      46,   290,    48,    49,    50,  1542,   294,   296,    75,    76,
      77,   136,   294,   957,  1572,   959,   292,   298,    18,   876,
     880,   295,   910,   294,   917,   567,   568,   569,   104,  1587,
     923,   294,   907,   109,    25,    26,    27,    28,    29,   914,
     291,   292,     7,   903,   111,   112,   113,  1584,   289,   924,
      26,    27,    28,    29,    75,    76,   291,   292,   925,   919,
       7,   921,   925,   292,   975,   976,   293,   294,   925,   926,
     927,  1013,   929,    75,    76,    77,   295,   952,   975,   976,
     295,   925,   926,   927,   300,   929,    19,   925,   926,   927,
      20,   925,   926,   927,   834,   835,   971,    26,    27,    28,
      29,   208,    18,   298,    19,   301,   105,   106,   107,   111,
     112,   113,   292,   292,   110,   975,   976,  1149,  1150,  1151,
    1152,   289,     5,     6,  1066,     8,   289,  1159,  1160,  1161,
      15,    16,    17,   289,   289,    20,    21,    22,    23,    24,
    1000,    26,    27,    28,    29,   303,    31,    32,   289,    19,
      35,    36,    37,    38,    28,    29,    41,    42,    43,   294,
      62,   105,   106,   107,   305,    62,    51,   294,    53,   169,
     170,   171,   172,   298,   294,   300,   295,   294,   299,   304,
     305,   295,   295,    68,    69,    70,   295,   295,   303,    63,
     295,   295,   295,   193,   194,   195,   196,    26,    27,    28,
      29,  1145,  1146,  1063,   212,   281,   282,   283,   284,   285,
     286,   287,   297,   297,   290,   297,   294,   305,   103,    18,
     296,   215,   215,   295,  1116,  1085,  1086,  1087,   292,   297,
     290,   292,  1092,  1093,  1094,  1095,  1096,  1097,     8,  1099,
    1100,  1101,    41,  1103,  1104,  1105,  1106,  1107,  1108,  1109,
    1098,  1111,   297,  1113,  1102,  1115,   290,  1117,  1486,  1487,
     176,   177,  1110,   294,   294,     5,     6,  1142,     8,  1180,
     294,   294,  1214,   295,   301,   295,  1187,    18,  1189,   295,
     196,   281,   282,   283,   284,   285,   286,   287,    19,   295,
     290,   295,   208,    19,   210,   294,   296,   213,   214,   298,
      41,   300,   299,   292,  1139,   304,   305,   299,   295,   292,
     292,  1171,   298,   294,  1149,  1150,  1151,  1152,   295,   303,
     105,   106,   107,   294,  1159,  1160,  1161,   295,   202,   203,
    1190,   205,   295,    73,   294,    75,    76,   294,   294,  1199,
     295,  1309,    82,   295,   295,   234,  1206,  1207,   242,   294,
     246,    22,   295,   290,   298,   154,   300,  1301,  1218,   297,
     304,   305,   297,   289,   295,   281,   282,   283,   284,   285,
     286,   287,   196,   301,   290,   294,   303,   294,  1238,  1239,
     296,   303,  1326,   136,  1241,   299,   299,  1247,  1248,  1249,
    1250,  1251,  1252,  1335,  1254,  1287,   299,   196,   299,   299,
     299,    20,   295,    62,    62,  1253,     8,   206,     5,     6,
     209,     8,   249,   154,  1289,   295,    13,   295,   295,    16,
     289,   294,   307,    20,    21,    22,    23,    24,   294,    26,
      27,    28,    29,   298,    31,    32,   298,   295,    35,    36,
      37,    38,   299,   295,    41,    42,    43,   295,   294,   294,
     166,   295,  1327,   295,    51,   196,    53,   295,   292,   299,
     299,   255,   292,  1407,   104,   206,    19,   300,   209,   109,
     295,    68,    69,    70,  1334,   303,   297,   299,   294,   294,
     294,  1341,   281,   282,   283,   284,   285,   286,   287,   294,
     294,   290,   294,   294,   104,  1352,   294,   296,   294,   109,
     290,   294,   294,   294,   299,     8,   103,   295,   105,   106,
     107,   295,   299,   298,   295,   300,  1409,   299,  1462,   304,
     305,    14,   119,   120,    17,   299,   295,  1469,   295,   295,
     301,   299,   295,  1408,   299,   295,   295,   299,   295,   295,
     281,   282,   283,   284,   285,   286,   287,    40,    41,   290,
     299,   299,    45,   295,    47,   296,   299,   295,   295,   295,
     299,    54,   159,   160,   161,  1425,  1426,  1427,     5,     6,
     299,     8,   299,   295,   295,   299,    13,   295,    71,    16,
     295,   292,   299,    20,    21,    22,    23,    24,    19,    26,
      27,    28,    29,    86,    31,    32,   295,   295,    35,    36,
      37,    38,  1494,  1545,    41,    42,    43,   294,     8,   299,
    1578,   299,   294,   299,    51,   294,    53,  1477,   294,   294,
     303,   295,  1482,  1483,  1484,   303,  1486,   295,  1488,  1489,
    1490,    68,    69,    70,   303,   295,   299,   294,  1582,  1487,
     294,   281,   282,   283,   284,   285,   286,   287,   295,   299,
    1510,  1511,  1512,   295,   299,   299,   296,   295,   294,   294,
     294,   294,   294,   294,   294,   294,   103,  1542,   105,   106,
     107,   281,   282,   283,   284,   285,   286,   287,   294,   294,
     290,    18,   119,   120,   295,   294,   296,   294,  1581,   294,
       8,   299,   295,    19,   295,   295,   300,   303,   294,   294,
     299,   298,   294,   300,   294,   298,   295,   304,   305,  1584,
     307,   295,   294,  1573,   295,   295,   295,     5,     6,   294,
       8,     8,   159,   160,   161,    13,   295,   295,    16,   294,
     294,   255,    20,    21,    22,    23,    24,   103,    26,    27,
      28,    29,    19,    31,    32,   295,   295,    35,    36,    37,
      38,   294,   179,    41,    42,    43,   295,   291,    25,   463,
    1179,   702,   716,    51,  1136,    53,   393,  1138,    25,   962,
    1178,   344,   607,    57,  1066,   564,     5,     6,   843,     8,
      68,    69,    70,   885,    13,   673,   964,    16,   850,  1121,
    1421,    20,    21,    22,    23,    24,   855,    26,    27,    28,
      29,    10,    31,    32,   690,  1130,    35,    36,    37,    38,
     475,   115,    41,    42,    43,   103,   526,   105,   106,   107,
      -1,    -1,    51,    -1,    53,   720,   424,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   172,    -1,   174,   175,    68,
      69,    70,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   193,   194,   195,   196,
      -1,   298,    -1,   300,    -1,    -1,    -1,   304,   305,    -1,
     307,   159,   160,   161,   103,   104,     5,     6,    -1,     8,
      -1,    -1,    -1,    -1,    13,    -1,    -1,    16,    -1,    -1,
      -1,    20,    21,    22,    23,    24,    -1,    26,    27,    28,
      29,    -1,    31,    32,    -1,    -1,    35,    36,    37,    38,
      -1,    -1,    41,    42,    43,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    51,    -1,    53,    -1,    -1,    -1,    -1,    -1,
     159,   160,   161,    -1,    -1,    -1,    -1,    -1,    -1,    68,
      69,    70,    -1,    72,   281,   282,   283,   284,   285,   286,
     287,    -1,    -1,   290,    -1,    -1,    -1,    -1,    -1,   296,
       5,     6,    -1,     8,    -1,    -1,    -1,   196,    13,    -1,
      -1,    16,    -1,    -1,   103,    20,    21,    22,    23,    24,
      -1,    26,    27,    28,    29,    -1,    31,    32,   117,    -1,
      35,    36,    37,    38,    -1,    -1,    41,    42,    43,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    51,    -1,    53,    -1,
     298,    -1,   300,    -1,    -1,    -1,   304,   305,    -1,   307,
      -1,    -1,    -1,    68,    69,    70,    -1,    -1,    -1,    -1,
     159,   160,   161,    22,    23,    24,    -1,    26,    27,    28,
      29,    -1,    31,    32,    33,    -1,    35,    36,    37,    38,
      -1,    -1,    -1,    42,    43,    -1,    -1,    -1,   103,   104,
      -1,    -1,    -1,    -1,     5,     6,    -1,     8,    -1,   298,
      -1,    -1,    13,    -1,    -1,    16,    -1,    -1,   307,    20,
      21,    22,    23,    24,    -1,    26,    27,    28,    29,    -1,
      31,    32,    -1,    -1,    35,    36,    37,    38,    -1,    -1,
      41,    42,    43,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      51,    -1,    53,    -1,   159,   160,   161,    -1,    -1,    -1,
      -1,   110,    -1,    -1,    -1,    -1,    -1,    68,    69,    70,
      -1,    -1,    -1,    -1,     5,     6,    -1,     8,    -1,    -1,
      -1,    -1,    13,    -1,    -1,    16,    -1,    -1,    -1,    20,
      21,    22,    23,    24,    -1,    26,    27,    28,    29,    -1,
      31,    32,   103,    -1,    35,    36,    37,    38,    -1,   298,
      41,    42,    43,    -1,    39,    -1,    -1,    -1,   307,    -1,
      51,    46,    53,    48,    49,    50,    -1,    -1,    -1,    -1,
      -1,    -1,     0,    -1,    -1,    -1,    -1,    68,    69,    70,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      18,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   159,   160,
     161,    -1,    87,    88,    89,    -1,    -1,    -1,     5,     6,
      -1,     8,   103,    -1,    -1,    -1,    13,    -1,    -1,    16,
      -1,    -1,    -1,    20,    21,    22,    23,    24,    -1,    26,
      27,    28,    29,   298,    31,    32,    -1,    -1,    35,    36,
      37,    38,   307,    -1,    41,    42,    43,    -1,   133,   134,
     135,    -1,   137,    -1,    51,   140,    53,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   159,   160,
     161,    68,    69,    70,    -1,    -1,    -1,     5,     6,    -1,
       8,    -1,    -1,    -1,    -1,    13,   295,    -1,    16,    -1,
      -1,    -1,    20,    21,    22,    23,    24,    -1,    26,    27,
      28,    29,    -1,    31,    32,    -1,   103,    35,    36,    37,
      38,    -1,    -1,    41,    42,    43,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    51,    -1,    53,   154,   155,   156,   157,
     158,    -1,    -1,    -1,    -1,    -1,    -1,   298,    -1,    -1,
      68,    69,    70,    -1,    -1,    39,   307,    -1,   176,   177,
      -1,    -1,    46,    -1,    48,    49,    50,    -1,    -1,    -1,
      -1,    -1,   159,   160,   161,   193,   194,   195,   196,    -1,
      -1,    -1,   200,   201,    -1,   103,    -1,    -1,   206,    -1,
      -1,   209,    -1,    -1,     5,    -1,    -1,     8,   216,   217,
      -1,    -1,    -1,    87,    88,    89,    -1,    18,     7,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   298,    -1,    -1,
     295,    -1,    -1,    22,    23,    24,   307,    26,    27,    28,
      29,    -1,    31,    32,    33,    -1,    35,    36,    37,    38,
      -1,   159,   160,   161,    -1,    44,    -1,    -1,    -1,   133,
     134,   135,    -1,   137,   272,   273,   140,   275,   276,   277,
      -1,   279,   280,   281,   282,   283,   284,   285,   286,   287,
      -1,    -1,    -1,    -1,    15,    16,    17,    -1,   296,    20,
      21,    22,    23,    24,    -1,    26,    27,    28,    29,    -1,
      31,    32,    -1,    -1,    35,    36,    37,    38,    -1,    -1,
      -1,    42,    43,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      51,   298,    53,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     307,    -1,    -1,    -1,    -1,    -1,    -1,    68,    69,    70,
     141,   142,   143,   144,   145,   146,   147,   148,   149,   150,
     151,   152,   153,    -1,    -1,    -1,    39,   158,    -1,    -1,
      -1,   162,   163,   164,   165,   166,   167,     5,    -1,    -1,
       8,    54,   103,    -1,    39,   176,   177,    -1,    -1,    -1,
      18,    46,    -1,    48,    49,    50,    -1,    70,    -1,    -1,
     298,    -1,   193,   194,   195,   196,    79,    -1,    -1,   307,
      -1,    -1,    -1,   204,    -1,    -1,    -1,    -1,    -1,    -1,
      93,    94,    95,    96,    -1,    -1,    -1,   218,    -1,    -1,
      -1,   295,    87,    88,    89,   178,   179,   180,   181,   182,
     183,   184,   185,   186,   187,   188,   189,   190,   191,   192,
      -1,    -1,   125,   126,   127,   128,   129,   130,   131,    -1,
      -1,     5,   253,   254,     8,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    18,    -1,    -1,    -1,   133,   134,
     135,    -1,   137,    -1,    -1,   140,    -1,    -1,    -1,    -1,
     281,   282,   283,   284,   285,   286,   287,    -1,   289,   290,
      -1,    -1,    -1,    -1,    -1,   296,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   141,   142,   143,   144,   145,   146,   147,
     148,   149,   150,   151,   152,   153,    -1,    -1,    -1,    -1,
     158,    -1,    -1,    -1,   162,   163,   164,   165,   166,   167,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   176,   177,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   193,   194,   195,   196,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   204,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   307,    -1,    -1,    -1,
     218,    -1,    -1,    -1,    -1,    -1,    -1,   141,   142,   143,
     144,   145,   146,   147,   148,   149,   150,   151,   152,   153,
      -1,    -1,    -1,    -1,   158,    -1,   289,    -1,   162,   163,
     164,   165,   166,   167,    -1,   253,   254,    -1,    -1,    -1,
      -1,    -1,   176,   177,    -1,    -1,    -1,    -1,    -1,    -1,
     295,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   193,
     194,   195,   196,   281,   282,   283,   284,   285,   286,   287,
     204,   289,   290,    -1,    -1,    -1,    -1,    -1,   296,    -1,
      -1,    16,    -1,    -1,   218,    20,    21,    22,    23,    24,
      -1,    26,    27,    28,    29,    -1,    31,    32,    -1,    -1,
      35,    36,    37,    38,    39,    -1,    -1,    42,    43,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    51,    -1,    53,   253,
     254,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    68,    69,    70,    -1,    72,    -1,    74,
      75,    76,    77,    -1,    -1,    -1,    -1,   281,   282,   283,
     284,   285,   286,   287,    -1,   289,   290,    -1,    -1,    -1,
      -1,    -1,   296,    -1,    -1,    -1,    -1,    -1,   103,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   111,   112,   113,   114,
      -1,    -1,   117,    -1,    -1,    16,   121,   122,   123,    20,
      21,    22,    23,    24,    -1,    26,    27,    28,    29,    -1,
      31,    32,    -1,    -1,    35,    36,    37,    38,    -1,    -1,
      -1,    42,    43,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      51,    -1,    53,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      18,    -1,    -1,    -1,    -1,    -1,    -1,    68,    69,    70,
      16,    -1,    -1,    -1,    20,    21,    22,    23,    24,    -1,
      26,    27,    28,    29,    -1,    31,    32,    -1,    -1,    35,
      36,    37,    38,    -1,    -1,    -1,    42,    43,    -1,    -1,
      -1,    -1,   103,    -1,    -1,    51,    -1,    53,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     225,    16,    68,    69,    70,    20,    21,    22,    23,    24,
      -1,    26,    27,    28,    29,    -1,    31,    32,    -1,    -1,
      35,    36,    37,    38,    -1,    -1,    -1,    42,    43,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    51,   103,    53,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    68,    69,    70,    -1,    -1,    21,    22,
      -1,    24,    25,    26,    27,    28,    29,    -1,    31,    32,
      -1,    34,    35,    36,    37,    38,   154,   155,   156,   157,
     158,    18,   307,    -1,    -1,    -1,    -1,    -1,   103,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   176,   177,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   193,   194,   195,   196,    -1,
      -1,    -1,   200,   201,    -1,    -1,    -1,    -1,   206,    -1,
      67,   209,    -1,    -1,    -1,    -1,    -1,    -1,   216,   217,
      22,    23,    24,    -1,    26,    27,    28,    29,    -1,    31,
      32,    33,    -1,    35,    36,    37,    38,    -1,    -1,    -1,
      42,    43,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   298,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   307,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   272,   273,    -1,   275,   276,   277,
      -1,   279,   280,   281,   282,   283,   284,   285,   286,   287,
      -1,    -1,   290,    -1,    -1,    -1,    18,   154,   296,   156,
     157,   158,    -1,    -1,    -1,    -1,    -1,    -1,   110,    -1,
      -1,   168,   298,    -1,    -1,    -1,   173,    -1,    -1,   176,
     177,   307,    18,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   193,   194,   195,   196,
      -1,   198,   199,    -1,    -1,    67,   229,   230,    -1,   232,
     233,   234,    -1,   236,   237,    -1,    -1,   240,   241,    -1,
     243,   218,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   307,   256,   257,   258,   259,   260,   261,   262,
     263,   264,   265,   266,   267,   268,   269,   270,    84,    -1,
      -1,    -1,    16,    -1,    -1,    -1,   253,   254,    22,    23,
      24,    -1,    26,    27,    28,    29,    -1,    31,    32,    -1,
      -1,    35,    36,    37,    38,    -1,    -1,    -1,    42,    43,
      -1,   304,    -1,    -1,   281,   282,   283,   284,   285,   286,
     287,    -1,   154,   290,   156,   157,   158,    -1,    -1,   296,
      -1,    -1,    -1,    -1,    -1,    -1,   168,    -1,    -1,    -1,
      -1,   173,    -1,    -1,   176,   177,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    91,    -1,    -1,
      -1,   193,   194,   195,   196,    -1,   198,   199,    -1,   281,
     282,   283,   284,   285,   286,   287,   110,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   296,    -1,   218,    -1,    -1,    -1,
     196,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   208,    -1,   210,   211,    -1,   213,   214,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   253,   254,    16,    -1,    -1,    -1,    -1,    21,    22,
      -1,    24,    25,    26,    27,    28,    29,    -1,    31,    32,
      -1,    34,    35,    36,    37,    38,    -1,    -1,    -1,   281,
     282,   283,   284,   285,   286,   287,    -1,    -1,   290,    -1,
      -1,    -1,    -1,    -1,   296,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   281,   282,   283,   284,   285,
     286,   287,    -1,    -1,   290,    -1,    -1,    80,    -1,    82,
     296,    -1,    -1,    16,    87,    -1,    -1,    -1,    21,    22,
      -1,    24,    25,    26,    27,    28,    29,    -1,    31,    32,
     103,    34,    35,    36,    37,    38,     5,     6,    -1,     8,
      22,    23,    24,    -1,    26,    27,    28,    29,    -1,    31,
      32,    33,    -1,    35,    36,    37,    38,    -1,    -1,    -1,
      42,    43,    -1,   136,    -1,    -1,    -1,    -1,    -1,    -1,
      39,    -1,    -1,    -1,    -1,    -1,    -1,    80,    -1,    82,
      -1,    -1,    -1,    -1,    87,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    68,
     103,    -1,    -1,    72,    -1,    -1,    75,    76,    -1,    -1,
      -1,    80,    81,    82,    83,    84,    85,    86,    87,    88,
      89,    90,    91,    -1,    -1,    -1,    -1,    -1,   110,    98,
      -1,    -1,   101,   136,    -1,    -1,    -1,    -1,    -1,   108,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   117,    -1,
      -1,    -1,    -1,   226,    -1,   228,   229,   230,   231,   232,
     233,   234,   235,   236,   237,   238,   239,   240,   241,   242,
     243,   244,    -1,    -1,    -1,   248,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   257,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   289,    -1,    -1,    -1,
      -1,    -1,    -1,   226,    -1,   228,   229,   230,   231,   232,
     233,   234,   235,   236,   237,   238,   239,   240,   241,   242,
     243,   244,    39,    40,    -1,   248,    -1,    -1,    45,    -1,
      47,    -1,    -1,    -1,   257,    -1,    -1,    54,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    71,    72,    -1,    74,    75,    76,
      77,    78,    -1,    -1,    -1,    -1,    83,    -1,    -1,    86,
      -1,    -1,    -1,    -1,    -1,    92,    -1,    -1,    -1,    -1,
      97,    -1,    99,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   111,   112,   113,   114,   115,   116,
     117,   118,    -1,    -1,    -1,    -1,    -1,   124
};

/* YYSTOS[STATE-NUM] -- The symbol kind of the accessing symbol of
   state STATE-NUM.  */
static const yytype_int16 yystos[] =
{
       0,   309,     0,    18,   154,   155,   156,   157,   158,   176,
     177,   193,   194,   195,   196,   200,   201,   206,   209,   216,
     217,   272,   273,   275,   276,   277,   279,   280,   281,   282,
     283,   284,   285,   286,   287,   296,   310,   313,   319,   320,
     321,   322,   323,   324,   331,   333,   334,   336,   337,   338,
     339,   340,   341,   358,   376,   380,   402,   403,   458,   461,
     467,   468,   469,   473,   482,   485,   490,   215,     5,     6,
       8,   314,   315,   298,   362,    64,   125,   404,   178,   179,
     180,   181,   182,   183,   184,   185,   186,   187,   188,   189,
     190,   191,   192,   466,   466,     8,    14,    17,    40,    41,
      45,    47,    54,    71,    86,   294,   325,   363,   364,   365,
     366,   297,   298,   274,   470,   215,   474,   491,   215,   315,
       9,   316,   316,     9,    10,   317,   317,    13,    16,    20,
      21,    22,    23,    24,    26,    27,    28,    29,    31,    32,
      35,    36,    37,    38,    42,    43,    51,    53,    68,    69,
      70,   103,   104,   159,   160,   161,   298,   307,   315,   321,
     322,   366,   367,   425,   448,   449,   454,   455,   289,   315,
     315,   315,   315,     7,    12,   411,   412,   411,   411,   289,
     342,    60,   343,   289,   381,   387,    23,    26,    27,    28,
      29,    31,    32,    33,   289,   305,   405,   408,   410,   411,
     316,   289,   289,   289,   289,   487,   293,   316,   359,   314,
     298,   366,   425,   448,   450,   454,     7,    33,   297,   312,
     292,   294,   294,    46,    48,    49,    50,   364,   364,   326,
     367,   450,   297,   454,   294,   316,   316,   207,   315,   474,
     100,   101,   102,   125,   219,   221,   222,   223,   224,   315,
      75,    76,   315,   315,   454,    26,    27,    28,    29,   448,
      51,   448,    24,    25,    34,    15,    17,   454,   217,   304,
     315,   366,   307,   315,   316,   136,   136,   136,   363,   364,
     136,   306,   105,   106,   107,   136,   298,   300,   304,   305,
     311,   448,   312,   295,    12,   295,   295,   309,    39,    68,
      72,    75,    76,    80,    81,    82,    83,    84,    85,    86,
      87,    88,    89,    90,    91,    98,   101,   108,   117,   315,
     450,    61,   344,   345,    39,    72,    74,    75,    76,    77,
      78,    83,    92,    97,    99,   111,   112,   113,   114,   115,
     116,   117,   118,   124,   364,   141,   142,   143,   144,   145,
     146,   147,   148,   149,   150,   151,   152,   153,   162,   163,
     164,   165,   166,   167,   204,   218,   253,   254,   289,   290,
     313,   314,   320,   331,   386,   388,   389,   390,   391,   393,
     394,   402,   426,   427,   428,   429,   430,   431,   432,   433,
     434,   435,   436,   437,   438,   439,   440,   458,   468,   304,
     294,   298,   407,   294,   407,   294,   407,   294,   407,   294,
     407,   294,   407,   294,   406,   408,   294,   411,   295,     7,
       8,   292,   303,   475,   483,   488,   492,    73,    75,    76,
      82,   315,   315,   299,    39,    72,    74,    75,    76,    77,
     111,   112,   113,   114,   117,   121,   122,   123,   225,   454,
     297,   217,   315,   364,   294,   297,   294,   289,   294,   291,
       8,   316,   316,   295,   289,   294,   312,   119,   120,   298,
     315,   383,   450,   299,   166,   471,   315,   220,   136,   448,
      25,   315,   450,   315,   299,   299,   299,   315,   316,   315,
     315,   315,   454,   315,   315,   294,   294,   315,    20,   299,
     316,   456,   457,   443,   444,   454,   290,   311,   290,   294,
      75,    76,    77,   111,   112,   113,   300,   349,   346,   450,
      67,   154,   168,   173,   198,   199,   218,   253,   254,   290,
     313,   320,   331,   341,   357,   358,   368,   372,   380,   402,
     458,   468,   486,   294,   294,   384,   316,   316,   316,   298,
     110,   288,   298,   103,   450,   303,   197,   294,   387,    55,
      57,    58,    59,   392,   395,   396,   397,   398,   399,   400,
     314,   316,   389,   314,   316,   316,   317,    11,    31,    32,
     294,   317,   318,   314,   316,   363,    15,    17,   366,   454,
     450,    87,   312,   410,   364,   326,   294,   411,   294,   316,
     316,   316,   316,   317,   318,   318,   290,   292,   314,   295,
     316,   316,   208,   210,   213,   214,   290,   320,   331,   458,
     476,   478,   479,   481,    84,   208,   211,   290,   472,   478,
     480,   484,    41,   154,   206,   209,   290,   320,   331,   489,
     206,   209,   290,   320,   331,   493,    75,    76,    77,   111,
     112,   113,   294,   294,   315,   315,   299,   454,   312,   462,
     463,   289,    51,   450,   459,   460,     7,   292,   295,   295,
     325,   327,   328,   300,   356,   442,    19,   335,   472,   136,
     315,    19,   299,   449,   449,   449,   304,   450,   450,    20,
     292,   299,   301,   292,   316,    39,    51,    52,    69,   119,
     291,   302,   350,   351,   352,   292,   110,   369,   373,   316,
     316,   487,   110,   288,   103,   450,   289,   289,   289,   387,
     289,   316,   312,   382,   298,   454,   303,   316,   298,   315,
     298,   315,   316,   364,    19,   294,    20,   384,   445,   446,
     447,   290,   450,   392,    56,   389,   401,   314,   316,   389,
     401,   401,   401,    62,    62,   294,   294,   315,   450,   294,
     411,   454,   314,   316,   441,   295,   312,   295,   299,   295,
     295,   295,   295,   295,   406,   295,   303,     8,   292,   212,
     297,   304,   316,   477,   297,   312,   411,   411,   297,   297,
     411,   411,   294,   215,   316,   315,   215,   315,   215,   316,
      16,    21,    22,    24,    25,    26,    27,    28,    29,    31,
      32,    34,    35,    36,    37,    38,    80,    82,    87,   103,
     136,   226,   228,   229,   230,   231,   232,   233,   234,   235,
     236,   237,   238,   239,   240,   241,   242,   243,   244,   248,
     257,   289,   378,   379,   451,    63,   360,   299,   297,   295,
     292,   327,     8,   297,   290,   292,     8,   297,   290,    22,
      23,    24,    26,    27,    28,    29,    31,    32,    35,    36,
      37,    38,    42,    43,   110,   320,   329,   409,   410,   414,
     298,   443,   294,   294,   315,   383,    28,    29,    63,   202,
     203,   205,   411,   315,   315,   449,   294,   295,   295,   316,
     457,   454,   295,   294,   351,   294,   315,   354,   301,   450,
     450,    72,   117,   315,   450,    72,   117,   364,   315,   298,
     315,   298,   315,   364,    19,   345,   370,   374,   290,   488,
     295,   136,   382,    39,    46,    48,    49,    50,    87,    88,
      89,   133,   134,   135,   137,   140,   295,   250,   251,   252,
     316,   225,   377,   316,   299,   316,   316,   292,   299,   454,
     383,   445,   454,   295,   292,   314,   316,   314,   316,   316,
     317,    19,   312,   295,   294,   292,   292,   295,   295,   407,
     407,   407,   407,   407,   407,   316,   316,   316,   294,   303,
     294,   295,   295,   294,   294,   295,   295,   316,   449,   315,
      63,   315,   295,    25,    26,    27,    28,    29,   294,   452,
     242,   234,   246,   294,   227,   247,    22,   452,   452,    21,
      22,    24,    25,    26,    27,    28,    29,    31,    32,    34,
      35,    36,    37,    38,   229,   230,   232,   233,   234,   236,
     237,   240,   241,   243,   256,   257,   258,   259,   260,   261,
     262,   263,   264,   265,   266,   267,   268,   269,   270,   304,
     453,   295,   412,   298,   304,   314,   297,   361,    28,    65,
      66,   312,   316,   448,   464,   465,   462,   290,   297,   289,
     459,   289,   294,   312,   294,   298,   294,   298,    26,    27,
      28,    29,   294,   298,   294,   298,   294,   298,   294,   298,
     294,   298,   294,   298,   294,   298,   294,   298,   294,   298,
     294,   298,   294,   298,   294,   298,   294,   298,   104,   109,
     320,   330,   411,   316,   301,   445,   445,   356,   442,   314,
     295,   445,   316,   347,   348,   450,   292,   353,   315,   196,
     321,   315,   454,   316,   316,   292,   454,   383,   290,   169,
     170,   171,   172,   290,   313,   320,   331,   371,   468,   172,
     174,   175,   290,   313,   320,   331,   375,   468,   290,   312,
     295,   294,   303,   303,   299,   299,   299,   299,   294,   383,
     136,   299,   299,   450,   361,   450,   295,   377,   447,    62,
      62,   295,   295,   315,   295,   445,   441,   441,     8,   292,
       8,   477,   295,   316,   249,   312,   298,   298,    25,    26,
      27,    28,    29,   271,   292,   298,   305,   290,   291,   299,
     316,    22,    23,    24,    26,    27,    28,    29,    31,    32,
      35,    36,    37,    38,    44,   312,   409,   413,   294,   294,
     289,   329,   327,   464,   316,   316,   316,   294,   298,   294,
     298,   294,   298,   294,   298,   316,   316,   316,   316,   316,
     316,   317,   316,   316,   318,   316,   317,   318,   316,   316,
     316,   316,   316,   316,   316,   317,   316,   414,   316,     8,
      44,   316,    44,    51,   448,   316,    42,    91,   110,   332,
     455,   295,   299,   295,   295,   294,   294,   471,   295,   295,
     295,   292,   352,   353,   315,   299,   299,   450,   450,   255,
     363,   363,   363,   363,   363,   363,   363,   382,   316,   138,
     139,   138,   139,   378,   349,   314,   292,    19,   314,   314,
     316,   295,   316,   303,   297,   292,   316,   316,   312,   299,
     316,   291,   299,    26,    27,    28,    29,   316,    26,    27,
      28,   316,   329,   290,   290,   295,   299,   295,   299,   316,
     316,   316,   316,   316,   316,   317,   316,   295,   299,   295,
     299,   295,   299,   295,   299,   295,   295,   299,   295,   295,
     299,   295,   299,   295,   299,   295,   299,   295,   299,   295,
     299,   295,   295,   299,   295,     8,   295,   299,    51,   448,
     298,   315,   301,   445,   445,   450,   294,   292,    19,   364,
     295,   295,   295,   294,   450,   383,     8,   477,   316,   312,
     299,   299,   299,   316,   295,   303,   303,   303,   295,   290,
     294,   294,   295,   299,   295,   299,   295,   299,   295,   299,
     294,   294,   294,   294,   294,   294,   294,   294,   294,   294,
     294,   294,   295,   294,     8,   299,   297,   295,   295,   445,
     450,   383,   454,   445,   300,   355,   356,   303,   295,   292,
     295,   451,   299,   316,   316,   316,   421,   419,   294,   294,
     294,   294,   420,   419,   418,   417,   415,   416,   420,   419,
     418,   417,   424,   422,   423,   414,   295,   355,   450,   295,
     294,   477,   312,   295,   295,   295,   295,   464,   295,   316,
     420,   419,   418,   417,   295,   316,   295,   295,   316,   295,
     317,   295,   316,   318,   295,   317,   318,   295,   295,   295,
     295,   295,   414,     8,    44,   295,    44,    51,   295,   448,
     361,   294,    19,   385,   445,   292,   295,   295,   295,   295,
       8,   445,   383,    39,    54,    70,    79,    93,    94,    95,
      96,   125,   126,   127,   128,   129,   130,   131,   289,   295,
     312,   295,   294,   294,   295,   255,   445,   316,   103,   295,
     295,   364,   454,   450,    19,   383,   355,   294,   445,   295
};

/* YYR1[RULE-NUM] -- Symbol kind of the left-hand side of rule RULE-NUM.  */
static const yytype_int16 yyr1[] =
{
       0,   308,   309,   309,   310,   310,   310,   310,   310,   310,
     310,   310,   310,   310,   310,   310,   310,   310,   310,   310,
     310,   310,   310,   310,   310,   310,   310,   310,   310,   310,
     311,   311,   312,   312,   313,   313,   313,   314,   314,   315,
     315,   315,   316,   317,   317,   318,   318,   318,   319,   319,
     319,   319,   319,   320,   320,   320,   320,   320,   320,   320,
     320,   320,   321,   321,   321,   321,   322,   322,   322,   322,
     323,   324,   325,   326,   326,   327,   328,   328,   328,   329,
     329,   329,   330,   330,   331,   331,   331,   332,   332,   332,
     332,   332,   332,   333,   333,   333,   334,   335,   335,   335,
     335,   335,   335,   336,   337,   338,   339,   340,   341,   342,
     342,   342,   342,   342,   342,   342,   342,   342,   342,   342,
     342,   342,   342,   342,   342,   342,   342,   342,   342,   342,
     342,   342,   342,   342,   342,   342,   343,   343,   344,   344,
     345,   345,   346,   346,   347,   347,   348,   348,   349,   349,
     350,   350,   350,   350,   350,   350,   350,   351,   351,   352,
     352,   353,   353,   354,   355,   355,   356,   357,   357,   357,
     357,   357,   357,   357,   357,   357,   357,   357,   357,   357,
     357,   357,   357,   357,   357,   357,   357,   357,   358,   359,
     359,   359,   359,   359,   359,   359,   359,   359,   359,   359,
     359,   359,   359,   359,   359,   360,   360,   361,   361,   362,
     362,   363,   363,   363,   363,   363,   363,   363,   364,   364,
     364,   364,   365,   365,   365,   365,   365,   365,   365,   365,
     366,   367,   367,   367,   367,   367,   367,   368,   368,   369,
     369,   369,   370,   370,   371,   371,   371,   371,   371,   371,
     371,   371,   372,   373,   373,   373,   374,   374,   375,   375,
     375,   375,   375,   375,   375,   376,   377,   377,   378,   378,
     379,   380,   381,   381,   381,   381,   381,   381,   381,   381,
     381,   381,   381,   381,   381,   381,   381,   381,   381,   381,
     381,   381,   381,   381,   381,   382,   382,   382,   382,   382,
     382,   382,   382,   382,   382,   382,   382,   382,   382,   382,
     382,   383,   383,   383,   384,   384,   384,   384,   384,   385,
     385,   385,   385,   385,   385,   385,   385,   385,   385,   385,
     385,   385,   385,   385,   385,   386,   387,   387,   388,   388,
     388,   388,   388,   388,   388,   388,   388,   388,   388,   388,
     388,   388,   388,   388,   388,   388,   388,   388,   388,   388,
     388,   388,   388,   388,   389,   390,   391,   392,   392,   393,
     393,   393,   394,   395,   395,   395,   395,   396,   396,   396,
     397,   398,   399,   400,   401,   401,   401,   402,   403,   403,
     404,   404,   404,   405,   405,   406,   406,   407,   407,   408,
     408,   408,   408,   408,   408,   408,   408,   408,   408,   408,
     408,   408,   408,   408,   409,   409,   409,   409,   409,   409,
     409,   409,   409,   409,   409,   409,   409,   409,   409,   409,
     409,   409,   409,   410,   411,   411,   412,   412,   413,   413,
     413,   414,   414,   414,   414,   414,   414,   414,   414,   414,
     414,   414,   414,   414,   414,   414,   414,   414,   414,   414,
     414,   414,   414,   414,   414,   414,   414,   415,   415,   415,
     416,   416,   416,   417,   417,   418,   418,   419,   419,   420,
     420,   421,   421,   422,   422,   422,   423,   423,   423,   423,
     424,   424,   425,   426,   427,   428,   429,   430,   431,   432,
     433,   434,   435,   436,   437,   438,   439,   440,   440,   440,
     440,   440,   440,   440,   440,   440,   440,   440,   440,   440,
     440,   440,   440,   440,   440,   440,   440,   440,   440,   440,
     441,   441,   441,   441,   441,   442,   442,   443,   443,   444,
     444,   445,   445,   446,   446,   447,   447,   447,   448,   448,
     448,   448,   448,   448,   448,   448,   448,   448,   449,   449,
     450,   450,   450,   450,   451,   451,   451,   451,   451,   451,
     451,   451,   451,   451,   451,   451,   451,   451,   451,   451,
     451,   451,   451,   451,   451,   451,   451,   451,   451,   451,
     451,   451,   451,   451,   451,   451,   451,   451,   451,   451,
     451,   451,   451,   451,   451,   451,   451,   451,   451,   451,
     451,   451,   451,   451,   451,   451,   451,   451,   451,   452,
     452,   453,   453,   453,   453,   453,   453,   453,   453,   453,
     453,   453,   453,   453,   453,   453,   453,   453,   453,   453,
     453,   453,   453,   453,   453,   453,   453,   453,   453,   453,
     453,   453,   453,   453,   453,   453,   453,   453,   453,   453,
     453,   453,   453,   453,   453,   453,   453,   453,   453,   453,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   454,   454,   454,   454,   454,   454,
     454,   454,   454,   454,   455,   455,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   455,   455,   455,   455,   455,
     455,   455,   456,   456,   457,   457,   457,   457,   457,   458,
     458,   458,   458,   458,   458,   459,   459,   459,   460,   460,
     461,   461,   462,   462,   463,   464,   464,   465,   465,   465,
     465,   465,   465,   465,   465,   466,   466,   466,   466,   466,
     466,   466,   466,   466,   466,   466,   466,   466,   466,   466,
     467,   467,   468,   468,   468,   468,   468,   468,   468,   468,
     468,   468,   468,   469,   469,   470,   470,   471,   471,   472,
     473,   474,   474,   474,   474,   474,   474,   474,   474,   474,
     474,   475,   475,   476,   476,   476,   477,   477,   478,   478,
     478,   478,   478,   478,   479,   480,   481,   482,   482,   483,
     483,   484,   484,   484,   484,   485,   486,   487,   487,   487,
     487,   487,   487,   487,   487,   487,   487,   488,   488,   489,
     489,   489,   489,   489,   489,   489,   490,   490,   491,   491,
     491,   492,   492,   493,   493,   493,   493
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
       2,     2,     2,     2,     5,     1,     0,     2,     2,     1,
       2,     4,     5,     1,     1,     1,     1,     2,     1,     1,
       1,     1,     1,     4,     6,     4,     4,    11,     1,     5,
       3,     7,     5,     5,     3,     1,     2,     2,     1,     2,
       4,     4,     1,     2,     2,     2,     2,     2,     2,     2,
       1,     2,     1,     1,     1,     4,     4,     2,     4,     2,
       0,     1,     1,     3,     1,     3,     1,     0,     3,     5,
       4,     3,     5,     5,     5,     5,     5,     5,     2,     2,
       2,     2,     2,     2,     4,     4,     4,     4,     4,     4,
       4,     4,     5,     5,     5,     5,     4,     4,     4,     4,
       4,     4,     3,     2,     0,     1,     1,     2,     1,     1,
       1,     1,     4,     4,     5,     4,     4,     4,     7,     7,
       7,     7,     7,     7,     7,     7,     7,     7,     8,     8,
       8,     8,     7,     7,     7,     7,     7,     0,     2,     2,
       0,     2,     2,     0,     2,     0,     2,     0,     2,     0,
       2,     0,     2,     0,     2,     2,     0,     2,     3,     2,
       0,     2,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     2,     1,     2,     2,
       2,     2,     2,     2,     3,     2,     2,     2,     5,     3,
       2,     2,     2,     2,     2,     5,     4,     6,     2,     4,
       0,     3,     3,     1,     1,     0,     3,     0,     1,     1,
       3,     0,     1,     1,     3,     1,     3,     4,     4,     4,
       4,     5,     1,     1,     1,     1,     1,     1,     1,     3,
       1,     3,     4,     1,     0,    10,     6,     5,     6,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     2,     2,     2,     2,     1,     1,     1,     1,     2,
       3,     4,     6,     5,     1,     1,     1,     1,     1,     1,
       1,     2,     2,     1,     2,     2,     4,     1,     2,     1,
       2,     1,     2,     1,     2,     1,     2,     1,     1,     0,
       5,     0,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     2,     2,     2,     2,     1,     1,     1,
       1,     1,     3,     2,     2,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     2,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       2,     1,     3,     2,     3,     4,     2,     2,     2,     5,
       5,     7,     4,     3,     2,     3,     2,     1,     1,     2,
       3,     2,     1,     2,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     2,     2,     2,     2,     1,     1,     1,
       1,     1,     1,     3,     0,     1,     1,     3,     2,     6,
       7,     3,     3,     3,     6,     0,     1,     3,     5,     6,
       4,     4,     1,     3,     3,     1,     1,     1,     1,     4,
       1,     6,     6,     6,     4,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     3,     2,     5,     4,     7,     6,     7,     6,
       9,     8,     3,     8,     4,     0,     2,     0,     1,     3,
       3,     0,     2,     2,     2,     3,     2,     2,     2,     2,
       2,     0,     2,     3,     1,     1,     1,     1,     3,     8,
       2,     3,     1,     1,     3,     3,     3,     4,     6,     0,
       2,     3,     1,     3,     1,     4,     3,     0,     2,     2,
       2,     3,     3,     3,     3,     3,     3,     0,     2,     2,
       3,     3,     4,     2,     1,     1,     3,     5,     0,     2,
       2,     0,     2,     4,     3,     1,     1
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
#line 3542 "asmparse.cpp"
    break;

  case 5: /* decl: nameSpaceHead '{' decls '}'  */
#line 194 "asmparse.y"
                                                                                { PASM->EndNameSpace(); }
#line 3548 "asmparse.cpp"
    break;

  case 6: /* decl: methodHead methodDecls '}'  */
#line 195 "asmparse.y"
                                                                                { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                                                  {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                                     PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                                                  PASM->EndMethod(); }
#line 3557 "asmparse.cpp"
    break;

  case 13: /* decl: assemblyHead '{' assemblyDecls '}'  */
#line 205 "asmparse.y"
                                                                                { PASMM->EndAssembly(); }
#line 3563 "asmparse.cpp"
    break;

  case 14: /* decl: assemblyRefHead '{' assemblyRefDecls '}'  */
#line 206 "asmparse.y"
                                                                                { PASMM->EndAssembly(); }
#line 3569 "asmparse.cpp"
    break;

  case 15: /* decl: exptypeHead '{' exptypeDecls '}'  */
#line 207 "asmparse.y"
                                                                                { PASMM->EndComType(); }
#line 3575 "asmparse.cpp"
    break;

  case 16: /* decl: manifestResHead '{' manifestResDecls '}'  */
#line 208 "asmparse.y"
                                                                                { PASMM->EndManifestRes(); }
#line 3581 "asmparse.cpp"
    break;

  case 20: /* decl: _SUBSYSTEM int32  */
#line 212 "asmparse.y"
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
#line 3596 "asmparse.cpp"
    break;

  case 21: /* decl: _CORFLAGS int32  */
#line 222 "asmparse.y"
                                                                                { PASM->m_dwComImageFlags = (yyvsp[0].int32); }
#line 3602 "asmparse.cpp"
    break;

  case 22: /* decl: _FILE ALIGNMENT_ int32  */
#line 223 "asmparse.y"
                                                                                { PASM->m_dwFileAlignment = (yyvsp[0].int32);
                                                                                  if(((yyvsp[0].int32) & ((yyvsp[0].int32) - 1))||((yyvsp[0].int32) < 0x200)||((yyvsp[0].int32) > 0x10000))
                                                                                    PASM->report->error("Invalid file alignment, must be power of 2 from 0x200 to 0x10000\n");}
#line 3610 "asmparse.cpp"
    break;

  case 23: /* decl: _IMAGEBASE int64  */
#line 226 "asmparse.y"
                                                                                { PASM->m_stBaseAddress = (ULONGLONG)(*((yyvsp[0].int64))); delete (yyvsp[0].int64);
                                                                                  if(PASM->m_stBaseAddress & 0xFFFF)
                                                                                    PASM->report->error("Invalid image base, must be 0x10000-aligned\n");}
#line 3618 "asmparse.cpp"
    break;

  case 24: /* decl: _STACKRESERVE int64  */
#line 229 "asmparse.y"
                                                                                { PASM->m_stSizeOfStackReserve = (size_t)(*((yyvsp[0].int64))); delete (yyvsp[0].int64); }
#line 3624 "asmparse.cpp"
    break;

  case 29: /* decl: _MSCORLIB  */
#line 234 "asmparse.y"
                                                                                { PASM->m_fIsMscorlib = TRUE; }
#line 3630 "asmparse.cpp"
    break;

  case 32: /* compQstring: QSTRING  */
#line 241 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[0].binstr); }
#line 3636 "asmparse.cpp"
    break;

  case 33: /* compQstring: compQstring '+' QSTRING  */
#line 242 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 3642 "asmparse.cpp"
    break;

  case 34: /* languageDecl: _LANGUAGE SQSTRING  */
#line 245 "asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[0].string),&(PASM->m_guidLang)); }
#line 3648 "asmparse.cpp"
    break;

  case 35: /* languageDecl: _LANGUAGE SQSTRING ',' SQSTRING  */
#line 246 "asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid((yyvsp[0].string),&(PASM->m_guidLangVendor));}
#line 3655 "asmparse.cpp"
    break;

  case 36: /* languageDecl: _LANGUAGE SQSTRING ',' SQSTRING ',' SQSTRING  */
#line 248 "asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[-4].string),&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidLangVendor));
                                                                                  LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidDoc));}
#line 3663 "asmparse.cpp"
    break;

  case 37: /* id: ID  */
#line 253 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3669 "asmparse.cpp"
    break;

  case 38: /* id: SQSTRING  */
#line 254 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3675 "asmparse.cpp"
    break;

  case 39: /* dottedName: id  */
#line 257 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3681 "asmparse.cpp"
    break;

  case 40: /* dottedName: DOTTEDNAME  */
#line 258 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 3687 "asmparse.cpp"
    break;

  case 41: /* dottedName: dottedName '.' dottedName  */
#line 259 "asmparse.y"
                                                              { (yyval.string) = newStringWDel((yyvsp[-2].string), '.', (yyvsp[0].string)); }
#line 3693 "asmparse.cpp"
    break;

  case 42: /* int32: INT32_V  */
#line 262 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 3699 "asmparse.cpp"
    break;

  case 43: /* int64: INT64_V  */
#line 265 "asmparse.y"
                                                              { (yyval.int64) = (yyvsp[0].int64); }
#line 3705 "asmparse.cpp"
    break;

  case 44: /* int64: INT32_V  */
#line 266 "asmparse.y"
                                                              { (yyval.int64) = neg ? new int64_t((yyvsp[0].int32)) : new int64_t((unsigned)(yyvsp[0].int32)); }
#line 3711 "asmparse.cpp"
    break;

  case 45: /* float64: FLOAT64  */
#line 269 "asmparse.y"
                                                              { (yyval.float64) = (yyvsp[0].float64); }
#line 3717 "asmparse.cpp"
    break;

  case 46: /* float64: FLOAT32_ '(' int32 ')'  */
#line 270 "asmparse.y"
                                                              { float f; *((int32_t*) (&f)) = (yyvsp[-1].int32); (yyval.float64) = new double(f); }
#line 3723 "asmparse.cpp"
    break;

  case 47: /* float64: FLOAT64_ '(' int64 ')'  */
#line 271 "asmparse.y"
                                                              { (yyval.float64) = (double*) (yyvsp[-1].int64); }
#line 3729 "asmparse.cpp"
    break;

  case 48: /* typedefDecl: _TYPEDEF type AS_ dottedName  */
#line 275 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].binstr),(yyvsp[0].string)); }
#line 3735 "asmparse.cpp"
    break;

  case 49: /* typedefDecl: _TYPEDEF className AS_ dottedName  */
#line 276 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].token),(yyvsp[0].string)); }
#line 3741 "asmparse.cpp"
    break;

  case 50: /* typedefDecl: _TYPEDEF memberRef AS_ dottedName  */
#line 277 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].token),(yyvsp[0].string)); }
#line 3747 "asmparse.cpp"
    break;

  case 51: /* typedefDecl: _TYPEDEF customDescr AS_ dottedName  */
#line 278 "asmparse.y"
                                                                                { (yyvsp[-2].cad)->tkOwner = 0; PASM->AddTypeDef((yyvsp[-2].cad),(yyvsp[0].string)); }
#line 3753 "asmparse.cpp"
    break;

  case 52: /* typedefDecl: _TYPEDEF customDescrWithOwner AS_ dottedName  */
#line 279 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].cad),(yyvsp[0].string)); }
#line 3759 "asmparse.cpp"
    break;

  case 53: /* compControl: P_DEFINE dottedName  */
#line 284 "asmparse.y"
                                                                                { DefineVar((yyvsp[0].string), NULL); }
#line 3765 "asmparse.cpp"
    break;

  case 54: /* compControl: P_DEFINE dottedName compQstring  */
#line 285 "asmparse.y"
                                                                                { DefineVar((yyvsp[-1].string), (yyvsp[0].binstr)); }
#line 3771 "asmparse.cpp"
    break;

  case 55: /* compControl: P_UNDEF dottedName  */
#line 286 "asmparse.y"
                                                                                { UndefVar((yyvsp[0].string)); }
#line 3777 "asmparse.cpp"
    break;

  case 56: /* compControl: P_IFDEF dottedName  */
#line 287 "asmparse.y"
                                                                                { SkipToken = !IsVarDefined((yyvsp[0].string));
                                                                                  IfEndif++;
                                                                                }
#line 3785 "asmparse.cpp"
    break;

  case 57: /* compControl: P_IFNDEF dottedName  */
#line 290 "asmparse.y"
                                                                                { SkipToken = IsVarDefined((yyvsp[0].string));
                                                                                  IfEndif++;
                                                                                }
#line 3793 "asmparse.cpp"
    break;

  case 58: /* compControl: P_ELSE  */
#line 293 "asmparse.y"
                                                                                { if(IfEndif == 1) SkipToken = !SkipToken;}
#line 3799 "asmparse.cpp"
    break;

  case 59: /* compControl: P_ENDIF  */
#line 294 "asmparse.y"
                                                                                { if(IfEndif == 0)
                                                                                    PASM->report->error("Unmatched #endif\n");
                                                                                  else IfEndif--;
                                                                                }
#line 3808 "asmparse.cpp"
    break;

  case 60: /* compControl: P_INCLUDE QSTRING  */
#line 298 "asmparse.y"
                                                                                { _ASSERTE(!"yylex should have dealt with this"); }
#line 3814 "asmparse.cpp"
    break;

  case 61: /* compControl: ';'  */
#line 299 "asmparse.y"
                                                                                { }
#line 3820 "asmparse.cpp"
    break;

  case 62: /* customDescr: _CUSTOM customType  */
#line 303 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[0].token), NULL); }
#line 3826 "asmparse.cpp"
    break;

  case 63: /* customDescr: _CUSTOM customType '=' compQstring  */
#line 304 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].token), (yyvsp[0].binstr)); }
#line 3832 "asmparse.cpp"
    break;

  case 64: /* customDescr: _CUSTOM customType '=' '{' customBlobDescr '}'  */
#line 305 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 3838 "asmparse.cpp"
    break;

  case 65: /* customDescr: customHead bytes ')'  */
#line 306 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].int32), (yyvsp[-1].binstr)); }
#line 3844 "asmparse.cpp"
    break;

  case 66: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType  */
#line 309 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-2].token), (yyvsp[0].token), NULL); }
#line 3850 "asmparse.cpp"
    break;

  case 67: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType '=' compQstring  */
#line 310 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-4].token), (yyvsp[-2].token), (yyvsp[0].binstr)); }
#line 3856 "asmparse.cpp"
    break;

  case 68: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType '=' '{' customBlobDescr '}'  */
#line 312 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-6].token), (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 3862 "asmparse.cpp"
    break;

  case 69: /* customDescrWithOwner: customHeadWithOwner bytes ')'  */
#line 313 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].int32), (yyvsp[-1].binstr)); }
#line 3868 "asmparse.cpp"
    break;

  case 70: /* customHead: _CUSTOM customType '=' '('  */
#line 316 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-2].token); bParsingByteArray = TRUE; }
#line 3874 "asmparse.cpp"
    break;

  case 71: /* customHeadWithOwner: _CUSTOM '(' ownerType ')' customType '=' '('  */
#line 320 "asmparse.y"
                                                                                { PASM->m_pCustomDescrList = NULL;
                                                                                  PASM->m_tkCurrentCVOwner = (yyvsp[-4].token);
                                                                                  (yyval.int32) = (yyvsp[-2].token); bParsingByteArray = TRUE; }
#line 3882 "asmparse.cpp"
    break;

  case 72: /* customType: methodRef  */
#line 325 "asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 3888 "asmparse.cpp"
    break;

  case 73: /* ownerType: typeSpec  */
#line 328 "asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 3894 "asmparse.cpp"
    break;

  case 74: /* ownerType: memberRef  */
#line 329 "asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 3900 "asmparse.cpp"
    break;

  case 75: /* customBlobDescr: customBlobArgs customBlobNVPairs  */
#line 333 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr);
                                                                                  (yyval.binstr)->appendInt16(VAL16(nCustomBlobNVPairs));
                                                                                  (yyval.binstr)->append((yyvsp[0].binstr));
                                                                                  nCustomBlobNVPairs = 0; }
#line 3909 "asmparse.cpp"
    break;

  case 76: /* customBlobArgs: %empty  */
#line 339 "asmparse.y"
                                                                                { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt16(VAL16(0x0001)); }
#line 3915 "asmparse.cpp"
    break;

  case 77: /* customBlobArgs: customBlobArgs serInit  */
#line 340 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr);
                                                                                  AppendFieldToCustomBlob((yyval.binstr),(yyvsp[0].binstr)); }
#line 3922 "asmparse.cpp"
    break;

  case 78: /* customBlobArgs: customBlobArgs compControl  */
#line 342 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 3928 "asmparse.cpp"
    break;

  case 79: /* customBlobNVPairs: %empty  */
#line 345 "asmparse.y"
                                                                                { (yyval.binstr) = new BinStr(); }
#line 3934 "asmparse.cpp"
    break;

  case 80: /* customBlobNVPairs: customBlobNVPairs fieldOrProp serializType dottedName '=' serInit  */
#line 347 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-5].binstr); (yyval.binstr)->appendInt8((yyvsp[-4].int32));
                                                                                  (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                                  AppendStringWithLength((yyval.binstr),(yyvsp[-2].string));
                                                                                  AppendFieldToCustomBlob((yyval.binstr),(yyvsp[0].binstr));
                                                                                  nCustomBlobNVPairs++; }
#line 3944 "asmparse.cpp"
    break;

  case 81: /* customBlobNVPairs: customBlobNVPairs compControl  */
#line 352 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 3950 "asmparse.cpp"
    break;

  case 82: /* fieldOrProp: FIELD_  */
#line 355 "asmparse.y"
                                                                                { (yyval.int32) = SERIALIZATION_TYPE_FIELD; }
#line 3956 "asmparse.cpp"
    break;

  case 83: /* fieldOrProp: PROPERTY_  */
#line 356 "asmparse.y"
                                                                                { (yyval.int32) = SERIALIZATION_TYPE_PROPERTY; }
#line 3962 "asmparse.cpp"
    break;

  case 84: /* customAttrDecl: customDescr  */
#line 359 "asmparse.y"
                                                                                { if((yyvsp[0].cad)->tkOwner && !(yyvsp[0].cad)->tkInterfacePair)
                                                                                    PASM->DefineCV((yyvsp[0].cad));
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH((yyvsp[0].cad)); }
#line 3971 "asmparse.cpp"
    break;

  case 85: /* customAttrDecl: customDescrWithOwner  */
#line 363 "asmparse.y"
                                                                                { PASM->DefineCV((yyvsp[0].cad)); }
#line 3977 "asmparse.cpp"
    break;

  case 86: /* customAttrDecl: TYPEDEF_CA  */
#line 364 "asmparse.y"
                                                                                { CustomDescr* pNew = new CustomDescr((yyvsp[0].tdd)->m_pCA);
                                                                                  if(pNew->tkOwner == 0) pNew->tkOwner = PASM->m_tkCurrentCVOwner;
                                                                                  if(pNew->tkOwner)
                                                                                    PASM->DefineCV(pNew);
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH(pNew); }
#line 3988 "asmparse.cpp"
    break;

  case 87: /* serializType: simpleType  */
#line 372 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 3994 "asmparse.cpp"
    break;

  case 88: /* serializType: TYPE_  */
#line 373 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE); }
#line 4000 "asmparse.cpp"
    break;

  case 89: /* serializType: OBJECT_  */
#line 374 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TAGGED_OBJECT); }
#line 4006 "asmparse.cpp"
    break;

  case 90: /* serializType: ENUM_ CLASS_ SQSTRING  */
#line 375 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); }
#line 4013 "asmparse.cpp"
    break;

  case 91: /* serializType: ENUM_ className  */
#line 377 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[0].token))); }
#line 4020 "asmparse.cpp"
    break;

  case 92: /* serializType: serializType '[' ']'  */
#line 379 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 4026 "asmparse.cpp"
    break;

  case 93: /* moduleHead: _MODULE  */
#line 384 "asmparse.y"
                                                                                { PASMM->SetModuleName(NULL); PASM->m_tkCurrentCVOwner=1; }
#line 4032 "asmparse.cpp"
    break;

  case 94: /* moduleHead: _MODULE dottedName  */
#line 385 "asmparse.y"
                                                                                { PASMM->SetModuleName((yyvsp[0].string)); PASM->m_tkCurrentCVOwner=1; }
#line 4038 "asmparse.cpp"
    break;

  case 95: /* moduleHead: _MODULE EXTERN_ dottedName  */
#line 386 "asmparse.y"
                                                                                { BinStr* pbs = new BinStr();
                                                                                  unsigned L = (unsigned)strlen((yyvsp[0].string));
                                                                                  memcpy((char*)(pbs->getBuff(L)),(yyvsp[0].string),L);
                                                                                  PASM->EmitImport(pbs); delete pbs;}
#line 4047 "asmparse.cpp"
    break;

  case 96: /* vtfixupDecl: _VTFIXUP '[' int32 ']' vtfixupAttr AT_ id  */
#line 393 "asmparse.y"
                                                                                { /*PASM->SetDataSection(); PASM->EmitDataLabel($7);*/
                                                                                  PASM->m_VTFList.PUSH(new VTFEntry((USHORT)(yyvsp[-4].int32), (USHORT)(yyvsp[-2].int32), (yyvsp[0].string))); }
#line 4054 "asmparse.cpp"
    break;

  case 97: /* vtfixupAttr: %empty  */
#line 397 "asmparse.y"
                                                                                { (yyval.int32) = 0; }
#line 4060 "asmparse.cpp"
    break;

  case 98: /* vtfixupAttr: vtfixupAttr INT32_  */
#line 398 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_32BIT; }
#line 4066 "asmparse.cpp"
    break;

  case 99: /* vtfixupAttr: vtfixupAttr INT64_  */
#line 399 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_64BIT; }
#line 4072 "asmparse.cpp"
    break;

  case 100: /* vtfixupAttr: vtfixupAttr FROMUNMANAGED_  */
#line 400 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_FROM_UNMANAGED; }
#line 4078 "asmparse.cpp"
    break;

  case 101: /* vtfixupAttr: vtfixupAttr CALLMOSTDERIVED_  */
#line 401 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_CALL_MOST_DERIVED; }
#line 4084 "asmparse.cpp"
    break;

  case 102: /* vtfixupAttr: vtfixupAttr RETAINAPPDOMAIN_  */
#line 402 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN; }
#line 4090 "asmparse.cpp"
    break;

  case 103: /* vtableDecl: vtableHead bytes ')'  */
#line 405 "asmparse.y"
                                                                                { PASM->m_pVTable = (yyvsp[-1].binstr); }
#line 4096 "asmparse.cpp"
    break;

  case 104: /* vtableHead: _VTABLE '=' '('  */
#line 408 "asmparse.y"
                                                                                { bParsingByteArray = TRUE; }
#line 4102 "asmparse.cpp"
    break;

  case 105: /* nameSpaceHead: _NAMESPACE dottedName  */
#line 412 "asmparse.y"
                                                                                { PASM->StartNameSpace((yyvsp[0].string)); }
#line 4108 "asmparse.cpp"
    break;

  case 106: /* _class: _CLASS  */
#line 415 "asmparse.y"
                                                                                { newclass = TRUE; }
#line 4114 "asmparse.cpp"
    break;

  case 107: /* classHeadBegin: _class classAttr dottedName typarsClause  */
#line 418 "asmparse.y"
                                                                                { if((yyvsp[0].typarlist)) FixupConstraints();
                                                                                  PASM->StartClass((yyvsp[-1].string), (yyvsp[-2].classAttr), (yyvsp[0].typarlist));
                                                                                  TyParFixupList.RESET(false);
                                                                                  newclass = FALSE;
                                                                                }
#line 4124 "asmparse.cpp"
    break;

  case 108: /* classHead: classHeadBegin extendsClause implClause  */
#line 424 "asmparse.y"
                                                                                { PASM->AddClass(); }
#line 4130 "asmparse.cpp"
    break;

  case 109: /* classAttr: %empty  */
#line 427 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) 0; }
#line 4136 "asmparse.cpp"
    break;

  case 110: /* classAttr: classAttr PUBLIC_  */
#line 428 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdVisibilityMask) | tdPublic); }
#line 4142 "asmparse.cpp"
    break;

  case 111: /* classAttr: classAttr PRIVATE_  */
#line 429 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdVisibilityMask) | tdNotPublic); }
#line 4148 "asmparse.cpp"
    break;

  case 112: /* classAttr: classAttr VALUE_  */
#line 430 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | 0x80000000 | tdSealed); }
#line 4154 "asmparse.cpp"
    break;

  case 113: /* classAttr: classAttr ENUM_  */
#line 431 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | 0x40000000); }
#line 4160 "asmparse.cpp"
    break;

  case 114: /* classAttr: classAttr INTERFACE_  */
#line 432 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdInterface | tdAbstract); }
#line 4166 "asmparse.cpp"
    break;

  case 115: /* classAttr: classAttr SEALED_  */
#line 433 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSealed); }
#line 4172 "asmparse.cpp"
    break;

  case 116: /* classAttr: classAttr ABSTRACT_  */
#line 434 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdAbstract); }
#line 4178 "asmparse.cpp"
    break;

  case 117: /* classAttr: classAttr AUTO_  */
#line 435 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdAutoLayout); }
#line 4184 "asmparse.cpp"
    break;

  case 118: /* classAttr: classAttr SEQUENTIAL_  */
#line 436 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdSequentialLayout); }
#line 4190 "asmparse.cpp"
    break;

  case 119: /* classAttr: classAttr EXPLICIT_  */
#line 437 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdExplicitLayout); }
#line 4196 "asmparse.cpp"
    break;

  case 120: /* classAttr: classAttr ANSI_  */
#line 438 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdAnsiClass); }
#line 4202 "asmparse.cpp"
    break;

  case 121: /* classAttr: classAttr UNICODE_  */
#line 439 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdUnicodeClass); }
#line 4208 "asmparse.cpp"
    break;

  case 122: /* classAttr: classAttr AUTOCHAR_  */
#line 440 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdAutoClass); }
#line 4214 "asmparse.cpp"
    break;

  case 123: /* classAttr: classAttr IMPORT_  */
#line 441 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdImport); }
#line 4220 "asmparse.cpp"
    break;

  case 124: /* classAttr: classAttr SERIALIZABLE_  */
#line 442 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSerializable); }
#line 4226 "asmparse.cpp"
    break;

  case 125: /* classAttr: classAttr WINDOWSRUNTIME_  */
#line 443 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdWindowsRuntime); }
#line 4232 "asmparse.cpp"
    break;

  case 126: /* classAttr: classAttr NESTED_ PUBLIC_  */
#line 444 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedPublic); }
#line 4238 "asmparse.cpp"
    break;

  case 127: /* classAttr: classAttr NESTED_ PRIVATE_  */
#line 445 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedPrivate); }
#line 4244 "asmparse.cpp"
    break;

  case 128: /* classAttr: classAttr NESTED_ FAMILY_  */
#line 446 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamily); }
#line 4250 "asmparse.cpp"
    break;

  case 129: /* classAttr: classAttr NESTED_ ASSEMBLY_  */
#line 447 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedAssembly); }
#line 4256 "asmparse.cpp"
    break;

  case 130: /* classAttr: classAttr NESTED_ FAMANDASSEM_  */
#line 448 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamANDAssem); }
#line 4262 "asmparse.cpp"
    break;

  case 131: /* classAttr: classAttr NESTED_ FAMORASSEM_  */
#line 449 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamORAssem); }
#line 4268 "asmparse.cpp"
    break;

  case 132: /* classAttr: classAttr BEFOREFIELDINIT_  */
#line 450 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdBeforeFieldInit); }
#line 4274 "asmparse.cpp"
    break;

  case 133: /* classAttr: classAttr SPECIALNAME_  */
#line 451 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSpecialName); }
#line 4280 "asmparse.cpp"
    break;

  case 134: /* classAttr: classAttr RTSPECIALNAME_  */
#line 452 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr)); }
#line 4286 "asmparse.cpp"
    break;

  case 135: /* classAttr: classAttr FLAGS_ '(' int32 ')'  */
#line 453 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].int32)); }
#line 4292 "asmparse.cpp"
    break;

  case 137: /* extendsClause: EXTENDS_ typeSpec  */
#line 457 "asmparse.y"
                                                                            { PASM->m_crExtends = (yyvsp[0].token); }
#line 4298 "asmparse.cpp"
    break;

  case 142: /* implList: implList ',' typeSpec  */
#line 468 "asmparse.y"
                                                            { PASM->AddToImplList((yyvsp[0].token)); }
#line 4304 "asmparse.cpp"
    break;

  case 143: /* implList: typeSpec  */
#line 469 "asmparse.y"
                                                            { PASM->AddToImplList((yyvsp[0].token)); }
#line 4310 "asmparse.cpp"
    break;

  case 144: /* typeList: %empty  */
#line 473 "asmparse.y"
                                                            { (yyval.binstr) = new BinStr(); }
#line 4316 "asmparse.cpp"
    break;

  case 145: /* typeList: typeListNotEmpty  */
#line 474 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4322 "asmparse.cpp"
    break;

  case 146: /* typeListNotEmpty: typeSpec  */
#line 477 "asmparse.y"
                                                            { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[0].token)); }
#line 4328 "asmparse.cpp"
    break;

  case 147: /* typeListNotEmpty: typeListNotEmpty ',' typeSpec  */
#line 478 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->appendInt32((yyvsp[0].token)); }
#line 4334 "asmparse.cpp"
    break;

  case 148: /* typarsClause: %empty  */
#line 481 "asmparse.y"
                                                            { (yyval.typarlist) = NULL; PASM->m_TyParList = NULL;}
#line 4340 "asmparse.cpp"
    break;

  case 149: /* typarsClause: '<' typars '>'  */
#line 482 "asmparse.y"
                                                            { (yyval.typarlist) = (yyvsp[-1].typarlist);   PASM->m_TyParList = (yyvsp[-1].typarlist);}
#line 4346 "asmparse.cpp"
    break;

  case 150: /* typarAttrib: '+'  */
#line 485 "asmparse.y"
                                                            { (yyval.int32) = gpCovariant; }
#line 4352 "asmparse.cpp"
    break;

  case 151: /* typarAttrib: '-'  */
#line 486 "asmparse.y"
                                                            { (yyval.int32) = gpContravariant; }
#line 4358 "asmparse.cpp"
    break;

  case 152: /* typarAttrib: CLASS_  */
#line 487 "asmparse.y"
                                                            { (yyval.int32) = gpReferenceTypeConstraint; }
#line 4364 "asmparse.cpp"
    break;

  case 153: /* typarAttrib: VALUETYPE_  */
#line 488 "asmparse.y"
                                                            { (yyval.int32) = gpNotNullableValueTypeConstraint; }
#line 4370 "asmparse.cpp"
    break;

  case 154: /* typarAttrib: BYREFLIKE_  */
#line 489 "asmparse.y"
                                                            { (yyval.int32) = gpAllowByRefLike; }
#line 4376 "asmparse.cpp"
    break;

  case 155: /* typarAttrib: _CTOR  */
#line 490 "asmparse.y"
                                                            { (yyval.int32) = gpDefaultConstructorConstraint; }
#line 4382 "asmparse.cpp"
    break;

  case 156: /* typarAttrib: FLAGS_ '(' int32 ')'  */
#line 491 "asmparse.y"
                                                            { (yyval.int32) = (CorGenericParamAttr)(yyvsp[-1].int32); }
#line 4388 "asmparse.cpp"
    break;

  case 157: /* typarAttribs: %empty  */
#line 494 "asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 4394 "asmparse.cpp"
    break;

  case 158: /* typarAttribs: typarAttrib typarAttribs  */
#line 495 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) | (yyvsp[0].int32); }
#line 4400 "asmparse.cpp"
    break;

  case 159: /* typars: typarAttribs tyBound dottedName typarsRest  */
#line 498 "asmparse.y"
                                                                     {(yyval.typarlist) = new TyParList((yyvsp[-3].int32), (yyvsp[-2].binstr), (yyvsp[-1].string), (yyvsp[0].typarlist));}
#line 4406 "asmparse.cpp"
    break;

  case 160: /* typars: typarAttribs dottedName typarsRest  */
#line 499 "asmparse.y"
                                                               {(yyval.typarlist) = new TyParList((yyvsp[-2].int32), NULL, (yyvsp[-1].string), (yyvsp[0].typarlist));}
#line 4412 "asmparse.cpp"
    break;

  case 161: /* typarsRest: %empty  */
#line 502 "asmparse.y"
                                                            { (yyval.typarlist) = NULL; }
#line 4418 "asmparse.cpp"
    break;

  case 162: /* typarsRest: ',' typars  */
#line 503 "asmparse.y"
                                                            { (yyval.typarlist) = (yyvsp[0].typarlist); }
#line 4424 "asmparse.cpp"
    break;

  case 163: /* tyBound: '(' typeList ')'  */
#line 506 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 4430 "asmparse.cpp"
    break;

  case 164: /* genArity: %empty  */
#line 509 "asmparse.y"
                                                            { (yyval.int32)= 0; }
#line 4436 "asmparse.cpp"
    break;

  case 165: /* genArity: genArityNotEmpty  */
#line 510 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[0].int32); }
#line 4442 "asmparse.cpp"
    break;

  case 166: /* genArityNotEmpty: '<' '[' int32 ']' '>'  */
#line 513 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-2].int32); }
#line 4448 "asmparse.cpp"
    break;

  case 167: /* classDecl: methodHead methodDecls '}'  */
#line 517 "asmparse.y"
                                                            { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                              {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                 PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                              PASM->EndMethod(); }
#line 4457 "asmparse.cpp"
    break;

  case 168: /* classDecl: classHead '{' classDecls '}'  */
#line 521 "asmparse.y"
                                                            { PASM->EndClass(); }
#line 4463 "asmparse.cpp"
    break;

  case 169: /* classDecl: eventHead '{' eventDecls '}'  */
#line 522 "asmparse.y"
                                                            { PASM->EndEvent(); }
#line 4469 "asmparse.cpp"
    break;

  case 170: /* classDecl: propHead '{' propDecls '}'  */
#line 523 "asmparse.y"
                                                            { PASM->EndProp(); }
#line 4475 "asmparse.cpp"
    break;

  case 176: /* classDecl: _SIZE int32  */
#line 529 "asmparse.y"
                                                                { PASM->m_pCurClass->m_ulSize = (yyvsp[0].int32); }
#line 4481 "asmparse.cpp"
    break;

  case 177: /* classDecl: _PACK int32  */
#line 530 "asmparse.y"
                                                                { PASM->m_pCurClass->m_ulPack = (yyvsp[0].int32); }
#line 4487 "asmparse.cpp"
    break;

  case 178: /* classDecl: exportHead '{' exptypeDecls '}'  */
#line 531 "asmparse.y"
                                                                { PASMM->EndComType(); }
#line 4493 "asmparse.cpp"
    break;

  case 179: /* classDecl: _OVERRIDE typeSpec DCOLON methodName WITH_ callConv type typeSpec DCOLON methodName '(' sigArgs0 ')'  */
#line 533 "asmparse.y"
                                                                { BinStr *sig1 = parser->MakeSig((yyvsp[-7].int32), (yyvsp[-6].binstr), (yyvsp[-1].binstr));
                                                                  BinStr *sig2 = new BinStr(); sig2->append(sig1);
                                                                  PASM->AddMethodImpl((yyvsp[-11].token),(yyvsp[-9].string),sig1,(yyvsp[-5].token),(yyvsp[-3].string),sig2);
                                                                  PASM->ResetArgNameList();
                                                                }
#line 4503 "asmparse.cpp"
    break;

  case 180: /* classDecl: _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')' WITH_ METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'  */
#line 539 "asmparse.y"
                                                                 { PASM->AddMethodImpl((yyvsp[-17].token),(yyvsp[-15].string),
                                                                      ((yyvsp[-14].int32)==0 ? parser->MakeSig((yyvsp[-19].int32),(yyvsp[-18].binstr),(yyvsp[-12].binstr)) :
                                                                      parser->MakeSig((yyvsp[-19].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-18].binstr),(yyvsp[-12].binstr),(yyvsp[-14].int32))),
                                                                      (yyvsp[-6].token),(yyvsp[-4].string),
                                                                      ((yyvsp[-3].int32)==0 ? parser->MakeSig((yyvsp[-8].int32),(yyvsp[-7].binstr),(yyvsp[-1].binstr)) :
                                                                      parser->MakeSig((yyvsp[-8].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-7].binstr),(yyvsp[-1].binstr),(yyvsp[-3].int32))));
                                                                   PASM->ResetArgNameList();
                                                                 }
#line 4516 "asmparse.cpp"
    break;

  case 183: /* classDecl: _PARAM TYPE_ '[' int32 ']'  */
#line 549 "asmparse.y"
                                                            { if(((yyvsp[-1].int32) > 0) && ((yyvsp[-1].int32) <= (int)PASM->m_pCurClass->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[(yyvsp[-1].int32)-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
#line 4526 "asmparse.cpp"
    break;

  case 184: /* classDecl: _PARAM TYPE_ dottedName  */
#line 554 "asmparse.y"
                                                            { int n = PASM->m_pCurClass->FindTyPar((yyvsp[0].string));
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",(yyvsp[0].string));
                                                            }
#line 4537 "asmparse.cpp"
    break;

  case 185: /* classDecl: _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec  */
#line 560 "asmparse.y"
                                                                        { PASM->AddGenericParamConstraint((yyvsp[-3].int32), 0, (yyvsp[0].token)); }
#line 4543 "asmparse.cpp"
    break;

  case 186: /* classDecl: _PARAM CONSTRAINT_ dottedName ',' typeSpec  */
#line 561 "asmparse.y"
                                                                        { PASM->AddGenericParamConstraint(0, (yyvsp[-2].string), (yyvsp[0].token)); }
#line 4549 "asmparse.cpp"
    break;

  case 187: /* classDecl: _INTERFACEIMPL TYPE_ typeSpec customDescr  */
#line 562 "asmparse.y"
                                                                      { (yyvsp[0].cad)->tkInterfacePair = (yyvsp[-1].token);
                                                                        if(PASM->m_pCustomDescrList)
                                                                            PASM->m_pCustomDescrList->PUSH((yyvsp[0].cad));
                                                                      }
#line 4558 "asmparse.cpp"
    break;

  case 188: /* fieldDecl: _FIELD repeatOpt fieldAttr type dottedName atOpt initOpt  */
#line 570 "asmparse.y"
                                                            { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                              PASM->AddField((yyvsp[-2].string), (yyvsp[-3].binstr), (yyvsp[-4].fieldAttr), (yyvsp[-1].string), (yyvsp[0].binstr), (yyvsp[-5].int32)); }
#line 4565 "asmparse.cpp"
    break;

  case 189: /* fieldAttr: %empty  */
#line 574 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) 0; }
#line 4571 "asmparse.cpp"
    break;

  case 190: /* fieldAttr: fieldAttr STATIC_  */
#line 575 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdStatic); }
#line 4577 "asmparse.cpp"
    break;

  case 191: /* fieldAttr: fieldAttr PUBLIC_  */
#line 576 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPublic); }
#line 4583 "asmparse.cpp"
    break;

  case 192: /* fieldAttr: fieldAttr PRIVATE_  */
#line 577 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPrivate); }
#line 4589 "asmparse.cpp"
    break;

  case 193: /* fieldAttr: fieldAttr FAMILY_  */
#line 578 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamily); }
#line 4595 "asmparse.cpp"
    break;

  case 194: /* fieldAttr: fieldAttr INITONLY_  */
#line 579 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdInitOnly); }
#line 4601 "asmparse.cpp"
    break;

  case 195: /* fieldAttr: fieldAttr RTSPECIALNAME_  */
#line 580 "asmparse.y"
                                                            { (yyval.fieldAttr) = (yyvsp[-1].fieldAttr); }
#line 4607 "asmparse.cpp"
    break;

  case 196: /* fieldAttr: fieldAttr SPECIALNAME_  */
#line 581 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdSpecialName); }
#line 4613 "asmparse.cpp"
    break;

  case 197: /* fieldAttr: fieldAttr MARSHAL_ '(' marshalBlob ')'  */
#line 594 "asmparse.y"
                                                            { PASM->m_pMarshal = (yyvsp[-1].binstr); }
#line 4619 "asmparse.cpp"
    break;

  case 198: /* fieldAttr: fieldAttr ASSEMBLY_  */
#line 595 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdAssembly); }
#line 4625 "asmparse.cpp"
    break;

  case 199: /* fieldAttr: fieldAttr FAMANDASSEM_  */
#line 596 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamANDAssem); }
#line 4631 "asmparse.cpp"
    break;

  case 200: /* fieldAttr: fieldAttr FAMORASSEM_  */
#line 597 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamORAssem); }
#line 4637 "asmparse.cpp"
    break;

  case 201: /* fieldAttr: fieldAttr PRIVATESCOPE_  */
#line 598 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPrivateScope); }
#line 4643 "asmparse.cpp"
    break;

  case 202: /* fieldAttr: fieldAttr LITERAL_  */
#line 599 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdLiteral); }
#line 4649 "asmparse.cpp"
    break;

  case 203: /* fieldAttr: fieldAttr NOTSERIALIZED_  */
#line 600 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdNotSerialized); }
#line 4655 "asmparse.cpp"
    break;

  case 204: /* fieldAttr: fieldAttr FLAGS_ '(' int32 ')'  */
#line 601 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].int32)); }
#line 4661 "asmparse.cpp"
    break;

  case 205: /* atOpt: %empty  */
#line 604 "asmparse.y"
                                                            { (yyval.string) = 0; }
#line 4667 "asmparse.cpp"
    break;

  case 206: /* atOpt: AT_ id  */
#line 605 "asmparse.y"
                                                            { (yyval.string) = (yyvsp[0].string); }
#line 4673 "asmparse.cpp"
    break;

  case 207: /* initOpt: %empty  */
#line 608 "asmparse.y"
                                                            { (yyval.binstr) = NULL; }
#line 4679 "asmparse.cpp"
    break;

  case 208: /* initOpt: '=' fieldInit  */
#line 609 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4685 "asmparse.cpp"
    break;

  case 209: /* repeatOpt: %empty  */
#line 612 "asmparse.y"
                                                            { (yyval.int32) = 0xFFFFFFFF; }
#line 4691 "asmparse.cpp"
    break;

  case 210: /* repeatOpt: '[' int32 ']'  */
#line 613 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32); }
#line 4697 "asmparse.cpp"
    break;

  case 211: /* methodRef: callConv type typeSpec DCOLON methodName tyArgs0 '(' sigArgs0 ')'  */
#line 618 "asmparse.y"
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
#line 4718 "asmparse.cpp"
    break;

  case 212: /* methodRef: callConv type typeSpec DCOLON methodName genArityNotEmpty '(' sigArgs0 ')'  */
#line 635 "asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(((yyvsp[-8].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               (yyval.token) = PASM->MakeMemberRef((yyvsp[-6].token), (yyvsp[-4].string),
                                                                 parser->MakeSig((yyvsp[-8].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-7].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32)));
                                                             }
#line 4728 "asmparse.cpp"
    break;

  case 213: /* methodRef: callConv type methodName tyArgs0 '(' sigArgs0 ')'  */
#line 641 "asmparse.y"
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
#line 4748 "asmparse.cpp"
    break;

  case 214: /* methodRef: callConv type methodName genArityNotEmpty '(' sigArgs0 ')'  */
#line 657 "asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(((yyvsp[-6].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               (yyval.token) = PASM->MakeMemberRef(mdTokenNil, (yyvsp[-4].string), parser->MakeSig((yyvsp[-6].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-5].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32)));
                                                             }
#line 4757 "asmparse.cpp"
    break;

  case 215: /* methodRef: mdtoken  */
#line 661 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token); }
#line 4763 "asmparse.cpp"
    break;

  case 216: /* methodRef: TYPEDEF_M  */
#line 662 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 4769 "asmparse.cpp"
    break;

  case 217: /* methodRef: TYPEDEF_MR  */
#line 663 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 4775 "asmparse.cpp"
    break;

  case 218: /* callConv: INSTANCE_ callConv  */
#line 666 "asmparse.y"
                                                              { (yyval.int32) = ((yyvsp[0].int32) | IMAGE_CEE_CS_CALLCONV_HASTHIS); }
#line 4781 "asmparse.cpp"
    break;

  case 219: /* callConv: EXPLICIT_ callConv  */
#line 667 "asmparse.y"
                                                              { (yyval.int32) = ((yyvsp[0].int32) | IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS); }
#line 4787 "asmparse.cpp"
    break;

  case 220: /* callConv: callKind  */
#line 668 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 4793 "asmparse.cpp"
    break;

  case 221: /* callConv: CALLCONV_ '(' int32 ')'  */
#line 669 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32); }
#line 4799 "asmparse.cpp"
    break;

  case 222: /* callKind: %empty  */
#line 672 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
#line 4805 "asmparse.cpp"
    break;

  case 223: /* callKind: DEFAULT_  */
#line 673 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
#line 4811 "asmparse.cpp"
    break;

  case 224: /* callKind: VARARG_  */
#line 674 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_VARARG; }
#line 4817 "asmparse.cpp"
    break;

  case 225: /* callKind: UNMANAGED_ CDECL_  */
#line 675 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_C; }
#line 4823 "asmparse.cpp"
    break;

  case 226: /* callKind: UNMANAGED_ STDCALL_  */
#line 676 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_STDCALL; }
#line 4829 "asmparse.cpp"
    break;

  case 227: /* callKind: UNMANAGED_ THISCALL_  */
#line 677 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_THISCALL; }
#line 4835 "asmparse.cpp"
    break;

  case 228: /* callKind: UNMANAGED_ FASTCALL_  */
#line 678 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_FASTCALL; }
#line 4841 "asmparse.cpp"
    break;

  case 229: /* callKind: UNMANAGED_  */
#line 679 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_UNMANAGED; }
#line 4847 "asmparse.cpp"
    break;

  case 230: /* mdtoken: MDTOKEN_ '(' int32 ')'  */
#line 682 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[-1].int32); }
#line 4853 "asmparse.cpp"
    break;

  case 231: /* memberRef: methodSpec methodRef  */
#line 685 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token);
                                                               PASM->delArgNameList(PASM->m_firstArgName);
                                                               PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                               PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                               PASM->SetMemberRefFixup((yyvsp[0].token),iOpcodeLen); }
#line 4863 "asmparse.cpp"
    break;

  case 232: /* memberRef: FIELD_ type typeSpec DCOLON dottedName  */
#line 691 "asmparse.y"
                                                             { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               (yyval.token) = PASM->MakeMemberRef((yyvsp[-2].token), (yyvsp[0].string), (yyvsp[-3].binstr));
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4871 "asmparse.cpp"
    break;

  case 233: /* memberRef: FIELD_ type dottedName  */
#line 695 "asmparse.y"
                                                             { (yyvsp[-1].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               (yyval.token) = PASM->MakeMemberRef(mdTokenNil, (yyvsp[0].string), (yyvsp[-1].binstr));
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4879 "asmparse.cpp"
    break;

  case 234: /* memberRef: FIELD_ TYPEDEF_F  */
#line 698 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4886 "asmparse.cpp"
    break;

  case 235: /* memberRef: FIELD_ TYPEDEF_MR  */
#line 700 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4893 "asmparse.cpp"
    break;

  case 236: /* memberRef: mdtoken  */
#line 702 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token);
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 4900 "asmparse.cpp"
    break;

  case 237: /* eventHead: _EVENT eventAttr typeSpec dottedName  */
#line 707 "asmparse.y"
                                                                 { PASM->ResetEvent((yyvsp[0].string), (yyvsp[-1].token), (yyvsp[-2].eventAttr)); }
#line 4906 "asmparse.cpp"
    break;

  case 238: /* eventHead: _EVENT eventAttr dottedName  */
#line 708 "asmparse.y"
                                                                 { PASM->ResetEvent((yyvsp[0].string), mdTypeRefNil, (yyvsp[-1].eventAttr)); }
#line 4912 "asmparse.cpp"
    break;

  case 239: /* eventAttr: %empty  */
#line 712 "asmparse.y"
                                                            { (yyval.eventAttr) = (CorEventAttr) 0; }
#line 4918 "asmparse.cpp"
    break;

  case 240: /* eventAttr: eventAttr RTSPECIALNAME_  */
#line 713 "asmparse.y"
                                                            { (yyval.eventAttr) = (yyvsp[-1].eventAttr); }
#line 4924 "asmparse.cpp"
    break;

  case 241: /* eventAttr: eventAttr SPECIALNAME_  */
#line 714 "asmparse.y"
                                                            { (yyval.eventAttr) = (CorEventAttr) ((yyvsp[-1].eventAttr) | evSpecialName); }
#line 4930 "asmparse.cpp"
    break;

  case 244: /* eventDecl: _ADDON methodRef  */
#line 721 "asmparse.y"
                                                           { PASM->SetEventMethod(0, (yyvsp[0].token)); }
#line 4936 "asmparse.cpp"
    break;

  case 245: /* eventDecl: _REMOVEON methodRef  */
#line 722 "asmparse.y"
                                                           { PASM->SetEventMethod(1, (yyvsp[0].token)); }
#line 4942 "asmparse.cpp"
    break;

  case 246: /* eventDecl: _FIRE methodRef  */
#line 723 "asmparse.y"
                                                           { PASM->SetEventMethod(2, (yyvsp[0].token)); }
#line 4948 "asmparse.cpp"
    break;

  case 247: /* eventDecl: _OTHER methodRef  */
#line 724 "asmparse.y"
                                                           { PASM->SetEventMethod(3, (yyvsp[0].token)); }
#line 4954 "asmparse.cpp"
    break;

  case 252: /* propHead: _PROPERTY propAttr callConv type dottedName '(' sigArgs0 ')' initOpt  */
#line 733 "asmparse.y"
                                                            { PASM->ResetProp((yyvsp[-4].string),
                                                              parser->MakeSig((IMAGE_CEE_CS_CALLCONV_PROPERTY |
                                                              ((yyvsp[-6].int32) & IMAGE_CEE_CS_CALLCONV_HASTHIS)),(yyvsp[-5].binstr),(yyvsp[-2].binstr)), (yyvsp[-7].propAttr), (yyvsp[0].binstr));}
#line 4962 "asmparse.cpp"
    break;

  case 253: /* propAttr: %empty  */
#line 738 "asmparse.y"
                                                            { (yyval.propAttr) = (CorPropertyAttr) 0; }
#line 4968 "asmparse.cpp"
    break;

  case 254: /* propAttr: propAttr RTSPECIALNAME_  */
#line 739 "asmparse.y"
                                                            { (yyval.propAttr) = (yyvsp[-1].propAttr); }
#line 4974 "asmparse.cpp"
    break;

  case 255: /* propAttr: propAttr SPECIALNAME_  */
#line 740 "asmparse.y"
                                                            { (yyval.propAttr) = (CorPropertyAttr) ((yyvsp[-1].propAttr) | prSpecialName); }
#line 4980 "asmparse.cpp"
    break;

  case 258: /* propDecl: _SET methodRef  */
#line 748 "asmparse.y"
                                                            { PASM->SetPropMethod(0, (yyvsp[0].token)); }
#line 4986 "asmparse.cpp"
    break;

  case 259: /* propDecl: _GET methodRef  */
#line 749 "asmparse.y"
                                                            { PASM->SetPropMethod(1, (yyvsp[0].token)); }
#line 4992 "asmparse.cpp"
    break;

  case 260: /* propDecl: _OTHER methodRef  */
#line 750 "asmparse.y"
                                                            { PASM->SetPropMethod(2, (yyvsp[0].token)); }
#line 4998 "asmparse.cpp"
    break;

  case 265: /* methodHeadPart1: _METHOD  */
#line 758 "asmparse.y"
                                                            { PASM->ResetForNextMethod();
                                                              uMethodBeginLine = PASM->m_ulCurLine;
                                                              uMethodBeginColumn=PASM->m_ulCurColumn;
                                                            }
#line 5007 "asmparse.cpp"
    break;

  case 266: /* marshalClause: %empty  */
#line 764 "asmparse.y"
                                                            { (yyval.binstr) = NULL; }
#line 5013 "asmparse.cpp"
    break;

  case 267: /* marshalClause: MARSHAL_ '(' marshalBlob ')'  */
#line 765 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 5019 "asmparse.cpp"
    break;

  case 268: /* marshalBlob: nativeType  */
#line 768 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 5025 "asmparse.cpp"
    break;

  case 269: /* marshalBlob: marshalBlobHead hexbytes '}'  */
#line 769 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 5031 "asmparse.cpp"
    break;

  case 270: /* marshalBlobHead: '{'  */
#line 772 "asmparse.y"
                                                             { bParsingByteArray = TRUE; }
#line 5037 "asmparse.cpp"
    break;

  case 271: /* methodHead: methodHeadPart1 methAttr callConv paramAttr type marshalClause methodName typarsClause '(' sigArgs0 ')' implAttr '{'  */
#line 776 "asmparse.y"
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
#line 5055 "asmparse.cpp"
    break;

  case 272: /* methAttr: %empty  */
#line 791 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) 0; }
#line 5061 "asmparse.cpp"
    break;

  case 273: /* methAttr: methAttr STATIC_  */
#line 792 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdStatic); }
#line 5067 "asmparse.cpp"
    break;

  case 274: /* methAttr: methAttr PUBLIC_  */
#line 793 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPublic); }
#line 5073 "asmparse.cpp"
    break;

  case 275: /* methAttr: methAttr PRIVATE_  */
#line 794 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPrivate); }
#line 5079 "asmparse.cpp"
    break;

  case 276: /* methAttr: methAttr FAMILY_  */
#line 795 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamily); }
#line 5085 "asmparse.cpp"
    break;

  case 277: /* methAttr: methAttr FINAL_  */
#line 796 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdFinal); }
#line 5091 "asmparse.cpp"
    break;

  case 278: /* methAttr: methAttr SPECIALNAME_  */
#line 797 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdSpecialName); }
#line 5097 "asmparse.cpp"
    break;

  case 279: /* methAttr: methAttr VIRTUAL_  */
#line 798 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdVirtual); }
#line 5103 "asmparse.cpp"
    break;

  case 280: /* methAttr: methAttr STRICT_  */
#line 799 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdCheckAccessOnOverride); }
#line 5109 "asmparse.cpp"
    break;

  case 281: /* methAttr: methAttr ABSTRACT_  */
#line 800 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdAbstract); }
#line 5115 "asmparse.cpp"
    break;

  case 282: /* methAttr: methAttr ASSEMBLY_  */
#line 801 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdAssem); }
#line 5121 "asmparse.cpp"
    break;

  case 283: /* methAttr: methAttr FAMANDASSEM_  */
#line 802 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamANDAssem); }
#line 5127 "asmparse.cpp"
    break;

  case 284: /* methAttr: methAttr FAMORASSEM_  */
#line 803 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamORAssem); }
#line 5133 "asmparse.cpp"
    break;

  case 285: /* methAttr: methAttr PRIVATESCOPE_  */
#line 804 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPrivateScope); }
#line 5139 "asmparse.cpp"
    break;

  case 286: /* methAttr: methAttr HIDEBYSIG_  */
#line 805 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdHideBySig); }
#line 5145 "asmparse.cpp"
    break;

  case 287: /* methAttr: methAttr NEWSLOT_  */
#line 806 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdNewSlot); }
#line 5151 "asmparse.cpp"
    break;

  case 288: /* methAttr: methAttr RTSPECIALNAME_  */
#line 807 "asmparse.y"
                                                            { (yyval.methAttr) = (yyvsp[-1].methAttr); }
#line 5157 "asmparse.cpp"
    break;

  case 289: /* methAttr: methAttr UNMANAGEDEXP_  */
#line 808 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdUnmanagedExport); }
#line 5163 "asmparse.cpp"
    break;

  case 290: /* methAttr: methAttr REQSECOBJ_  */
#line 809 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdRequireSecObject); }
#line 5169 "asmparse.cpp"
    break;

  case 291: /* methAttr: methAttr FLAGS_ '(' int32 ')'  */
#line 810 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].int32)); }
#line 5175 "asmparse.cpp"
    break;

  case 292: /* methAttr: methAttr PINVOKEIMPL_ '(' compQstring AS_ compQstring pinvAttr ')'  */
#line 812 "asmparse.y"
                                                            { PASM->SetPinvoke((yyvsp[-4].binstr),0,(yyvsp[-2].binstr),(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-7].methAttr) | mdPinvokeImpl); }
#line 5182 "asmparse.cpp"
    break;

  case 293: /* methAttr: methAttr PINVOKEIMPL_ '(' compQstring pinvAttr ')'  */
#line 815 "asmparse.y"
                                                            { PASM->SetPinvoke((yyvsp[-2].binstr),0,NULL,(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-5].methAttr) | mdPinvokeImpl); }
#line 5189 "asmparse.cpp"
    break;

  case 294: /* methAttr: methAttr PINVOKEIMPL_ '(' pinvAttr ')'  */
#line 818 "asmparse.y"
                                                            { PASM->SetPinvoke(new BinStr(),0,NULL,(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-4].methAttr) | mdPinvokeImpl); }
#line 5196 "asmparse.cpp"
    break;

  case 295: /* pinvAttr: %empty  */
#line 822 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) 0; }
#line 5202 "asmparse.cpp"
    break;

  case 296: /* pinvAttr: pinvAttr NOMANGLE_  */
#line 823 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmNoMangle); }
#line 5208 "asmparse.cpp"
    break;

  case 297: /* pinvAttr: pinvAttr ANSI_  */
#line 824 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetAnsi); }
#line 5214 "asmparse.cpp"
    break;

  case 298: /* pinvAttr: pinvAttr UNICODE_  */
#line 825 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetUnicode); }
#line 5220 "asmparse.cpp"
    break;

  case 299: /* pinvAttr: pinvAttr AUTOCHAR_  */
#line 826 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetAuto); }
#line 5226 "asmparse.cpp"
    break;

  case 300: /* pinvAttr: pinvAttr LASTERR_  */
#line 827 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmSupportsLastError); }
#line 5232 "asmparse.cpp"
    break;

  case 301: /* pinvAttr: pinvAttr WINAPI_  */
#line 828 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvWinapi); }
#line 5238 "asmparse.cpp"
    break;

  case 302: /* pinvAttr: pinvAttr CDECL_  */
#line 829 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvCdecl); }
#line 5244 "asmparse.cpp"
    break;

  case 303: /* pinvAttr: pinvAttr STDCALL_  */
#line 830 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvStdcall); }
#line 5250 "asmparse.cpp"
    break;

  case 304: /* pinvAttr: pinvAttr THISCALL_  */
#line 831 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvThiscall); }
#line 5256 "asmparse.cpp"
    break;

  case 305: /* pinvAttr: pinvAttr FASTCALL_  */
#line 832 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvFastcall); }
#line 5262 "asmparse.cpp"
    break;

  case 306: /* pinvAttr: pinvAttr BESTFIT_ ':' ON_  */
#line 833 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmBestFitEnabled); }
#line 5268 "asmparse.cpp"
    break;

  case 307: /* pinvAttr: pinvAttr BESTFIT_ ':' OFF_  */
#line 834 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmBestFitDisabled); }
#line 5274 "asmparse.cpp"
    break;

  case 308: /* pinvAttr: pinvAttr CHARMAPERROR_ ':' ON_  */
#line 835 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmThrowOnUnmappableCharEnabled); }
#line 5280 "asmparse.cpp"
    break;

  case 309: /* pinvAttr: pinvAttr CHARMAPERROR_ ':' OFF_  */
#line 836 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmThrowOnUnmappableCharDisabled); }
#line 5286 "asmparse.cpp"
    break;

  case 310: /* pinvAttr: pinvAttr FLAGS_ '(' int32 ')'  */
#line 837 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].int32)); }
#line 5292 "asmparse.cpp"
    break;

  case 311: /* methodName: _CTOR  */
#line 840 "asmparse.y"
                                                            { (yyval.string) = newString(COR_CTOR_METHOD_NAME); }
#line 5298 "asmparse.cpp"
    break;

  case 312: /* methodName: _CCTOR  */
#line 841 "asmparse.y"
                                                            { (yyval.string) = newString(COR_CCTOR_METHOD_NAME); }
#line 5304 "asmparse.cpp"
    break;

  case 313: /* methodName: dottedName  */
#line 842 "asmparse.y"
                                                            { (yyval.string) = (yyvsp[0].string); }
#line 5310 "asmparse.cpp"
    break;

  case 314: /* paramAttr: %empty  */
#line 845 "asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 5316 "asmparse.cpp"
    break;

  case 315: /* paramAttr: paramAttr '[' IN_ ']'  */
#line 846 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdIn; }
#line 5322 "asmparse.cpp"
    break;

  case 316: /* paramAttr: paramAttr '[' OUT_ ']'  */
#line 847 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdOut; }
#line 5328 "asmparse.cpp"
    break;

  case 317: /* paramAttr: paramAttr '[' OPT_ ']'  */
#line 848 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdOptional; }
#line 5334 "asmparse.cpp"
    break;

  case 318: /* paramAttr: paramAttr '[' int32 ']'  */
#line 849 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) + 1; }
#line 5340 "asmparse.cpp"
    break;

  case 319: /* implAttr: %empty  */
#line 852 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (miIL | miManaged); }
#line 5346 "asmparse.cpp"
    break;

  case 320: /* implAttr: implAttr NATIVE_  */
#line 853 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miNative); }
#line 5352 "asmparse.cpp"
    break;

  case 321: /* implAttr: implAttr CIL_  */
#line 854 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miIL); }
#line 5358 "asmparse.cpp"
    break;

  case 322: /* implAttr: implAttr OPTIL_  */
#line 855 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miOPTIL); }
#line 5364 "asmparse.cpp"
    break;

  case 323: /* implAttr: implAttr MANAGED_  */
#line 856 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFFB) | miManaged); }
#line 5370 "asmparse.cpp"
    break;

  case 324: /* implAttr: implAttr UNMANAGED_  */
#line 857 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFFB) | miUnmanaged); }
#line 5376 "asmparse.cpp"
    break;

  case 325: /* implAttr: implAttr FORWARDREF_  */
#line 858 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miForwardRef); }
#line 5382 "asmparse.cpp"
    break;

  case 326: /* implAttr: implAttr PRESERVESIG_  */
#line 859 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miPreserveSig); }
#line 5388 "asmparse.cpp"
    break;

  case 327: /* implAttr: implAttr RUNTIME_  */
#line 860 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miRuntime); }
#line 5394 "asmparse.cpp"
    break;

  case 328: /* implAttr: implAttr INTERNALCALL_  */
#line 861 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miInternalCall); }
#line 5400 "asmparse.cpp"
    break;

  case 329: /* implAttr: implAttr SYNCHRONIZED_  */
#line 862 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miSynchronized); }
#line 5406 "asmparse.cpp"
    break;

  case 330: /* implAttr: implAttr NOINLINING_  */
#line 863 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miNoInlining); }
#line 5412 "asmparse.cpp"
    break;

  case 331: /* implAttr: implAttr AGGRESSIVEINLINING_  */
#line 864 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAggressiveInlining); }
#line 5418 "asmparse.cpp"
    break;

  case 332: /* implAttr: implAttr NOOPTIMIZATION_  */
#line 865 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miNoOptimization); }
#line 5424 "asmparse.cpp"
    break;

  case 333: /* implAttr: implAttr AGGRESSIVEOPTIMIZATION_  */
#line 866 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAggressiveOptimization); }
#line 5430 "asmparse.cpp"
    break;

  case 334: /* implAttr: implAttr FLAGS_ '(' int32 ')'  */
#line 867 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].int32)); }
#line 5436 "asmparse.cpp"
    break;

  case 335: /* localsHead: _LOCALS  */
#line 870 "asmparse.y"
                                                            { PASM->delArgNameList(PASM->m_firstArgName); PASM->m_firstArgName = NULL;PASM->m_lastArgName = NULL;
                                                            }
#line 5443 "asmparse.cpp"
    break;

  case 338: /* methodDecl: _EMITBYTE int32  */
#line 878 "asmparse.y"
                                                            { PASM->EmitByte((yyvsp[0].int32)); }
#line 5449 "asmparse.cpp"
    break;

  case 339: /* methodDecl: sehBlock  */
#line 879 "asmparse.y"
                                                            { delete PASM->m_SEHD; PASM->m_SEHD = PASM->m_SEHDstack.POP(); }
#line 5455 "asmparse.cpp"
    break;

  case 340: /* methodDecl: _MAXSTACK int32  */
#line 880 "asmparse.y"
                                                            { PASM->EmitMaxStack((yyvsp[0].int32)); }
#line 5461 "asmparse.cpp"
    break;

  case 341: /* methodDecl: localsHead '(' sigArgs0 ')'  */
#line 881 "asmparse.y"
                                                            { PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, (yyvsp[-1].binstr)));
                                                            }
#line 5468 "asmparse.cpp"
    break;

  case 342: /* methodDecl: localsHead INIT_ '(' sigArgs0 ')'  */
#line 883 "asmparse.y"
                                                            { PASM->EmitZeroInit();
                                                              PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, (yyvsp[-1].binstr)));
                                                            }
#line 5476 "asmparse.cpp"
    break;

  case 343: /* methodDecl: _ENTRYPOINT  */
#line 886 "asmparse.y"
                                                            { PASM->EmitEntryPoint(); }
#line 5482 "asmparse.cpp"
    break;

  case 344: /* methodDecl: _ZEROINIT  */
#line 887 "asmparse.y"
                                                            { PASM->EmitZeroInit(); }
#line 5488 "asmparse.cpp"
    break;

  case 347: /* methodDecl: id ':'  */
#line 890 "asmparse.y"
                                                            { PASM->AddLabel(PASM->m_CurPC,(yyvsp[-1].string)); /*PASM->EmitLabel($1);*/ }
#line 5494 "asmparse.cpp"
    break;

  case 353: /* methodDecl: _EXPORT '[' int32 ']'  */
#line 896 "asmparse.y"
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
#line 5509 "asmparse.cpp"
    break;

  case 354: /* methodDecl: _EXPORT '[' int32 ']' AS_ id  */
#line 906 "asmparse.y"
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
#line 5524 "asmparse.cpp"
    break;

  case 355: /* methodDecl: _VTENTRY int32 ':' int32  */
#line 916 "asmparse.y"
                                                            { PASM->m_pCurMethod->m_wVTEntry = (WORD)(yyvsp[-2].int32);
                                                              PASM->m_pCurMethod->m_wVTSlot = (WORD)(yyvsp[0].int32); }
#line 5531 "asmparse.cpp"
    break;

  case 356: /* methodDecl: _OVERRIDE typeSpec DCOLON methodName  */
#line 919 "asmparse.y"
                                                            { PASM->AddMethodImpl((yyvsp[-2].token),(yyvsp[0].string),NULL,mdTokenNil,NULL,NULL); }
#line 5537 "asmparse.cpp"
    break;

  case 357: /* methodDecl: _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'  */
#line 922 "asmparse.y"
                                                            { PASM->AddMethodImpl((yyvsp[-6].token),(yyvsp[-4].string),
                                                              ((yyvsp[-3].int32)==0 ? parser->MakeSig((yyvsp[-8].int32),(yyvsp[-7].binstr),(yyvsp[-1].binstr)) :
                                                              parser->MakeSig((yyvsp[-8].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-7].binstr),(yyvsp[-1].binstr),(yyvsp[-3].int32)))
                                                              ,mdTokenNil,NULL,NULL);
                                                              PASM->ResetArgNameList();
                                                            }
#line 5548 "asmparse.cpp"
    break;

  case 359: /* methodDecl: _PARAM TYPE_ '[' int32 ']'  */
#line 929 "asmparse.y"
                                                            { if(((yyvsp[-1].int32) > 0) && ((yyvsp[-1].int32) <= (int)PASM->m_pCurMethod->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[(yyvsp[-1].int32)-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
#line 5558 "asmparse.cpp"
    break;

  case 360: /* methodDecl: _PARAM TYPE_ dottedName  */
#line 934 "asmparse.y"
                                                            { int n = PASM->m_pCurMethod->FindTyPar((yyvsp[0].string));
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",(yyvsp[0].string));
                                                            }
#line 5569 "asmparse.cpp"
    break;

  case 361: /* methodDecl: _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec  */
#line 940 "asmparse.y"
                                                                        { PASM->m_pCurMethod->AddGenericParamConstraint((yyvsp[-3].int32), 0, (yyvsp[0].token)); }
#line 5575 "asmparse.cpp"
    break;

  case 362: /* methodDecl: _PARAM CONSTRAINT_ dottedName ',' typeSpec  */
#line 941 "asmparse.y"
                                                                        { PASM->m_pCurMethod->AddGenericParamConstraint(0, (yyvsp[-2].string), (yyvsp[0].token)); }
#line 5581 "asmparse.cpp"
    break;

  case 363: /* methodDecl: _PARAM '[' int32 ']' initOpt  */
#line 944 "asmparse.y"
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
#line 5604 "asmparse.cpp"
    break;

  case 364: /* scopeBlock: scopeOpen methodDecls '}'  */
#line 964 "asmparse.y"
                                                            { PASM->m_pCurMethod->CloseScope(); }
#line 5610 "asmparse.cpp"
    break;

  case 365: /* scopeOpen: '{'  */
#line 967 "asmparse.y"
                                                            { PASM->m_pCurMethod->OpenScope(); }
#line 5616 "asmparse.cpp"
    break;

  case 369: /* tryBlock: tryHead scopeBlock  */
#line 978 "asmparse.y"
                                                            { PASM->m_SEHD->tryTo = PASM->m_CurPC; }
#line 5622 "asmparse.cpp"
    break;

  case 370: /* tryBlock: tryHead id TO_ id  */
#line 979 "asmparse.y"
                                                            { PASM->SetTryLabels((yyvsp[-2].string), (yyvsp[0].string)); }
#line 5628 "asmparse.cpp"
    break;

  case 371: /* tryBlock: tryHead int32 TO_ int32  */
#line 980 "asmparse.y"
                                                            { if(PASM->m_SEHD) {PASM->m_SEHD->tryFrom = (yyvsp[-2].int32);
                                                              PASM->m_SEHD->tryTo = (yyvsp[0].int32);} }
#line 5635 "asmparse.cpp"
    break;

  case 372: /* tryHead: _TRY  */
#line 984 "asmparse.y"
                                                            { PASM->NewSEHDescriptor();
                                                              PASM->m_SEHD->tryFrom = PASM->m_CurPC; }
#line 5642 "asmparse.cpp"
    break;

  case 373: /* sehClause: catchClause handlerBlock  */
#line 989 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5648 "asmparse.cpp"
    break;

  case 374: /* sehClause: filterClause handlerBlock  */
#line 990 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5654 "asmparse.cpp"
    break;

  case 375: /* sehClause: finallyClause handlerBlock  */
#line 991 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5660 "asmparse.cpp"
    break;

  case 376: /* sehClause: faultClause handlerBlock  */
#line 992 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 5666 "asmparse.cpp"
    break;

  case 377: /* filterClause: filterHead scopeBlock  */
#line 996 "asmparse.y"
                                                             { PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5672 "asmparse.cpp"
    break;

  case 378: /* filterClause: filterHead id  */
#line 997 "asmparse.y"
                                                             { PASM->SetFilterLabel((yyvsp[0].string));
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5679 "asmparse.cpp"
    break;

  case 379: /* filterClause: filterHead int32  */
#line 999 "asmparse.y"
                                                             { PASM->m_SEHD->sehFilter = (yyvsp[0].int32);
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5686 "asmparse.cpp"
    break;

  case 380: /* filterHead: FILTER_  */
#line 1003 "asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FILTER;
                                                               PASM->m_SEHD->sehFilter = PASM->m_CurPC; }
#line 5693 "asmparse.cpp"
    break;

  case 381: /* catchClause: CATCH_ typeSpec  */
#line 1007 "asmparse.y"
                                                            {  PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_NONE;
                                                               PASM->SetCatchClass((yyvsp[0].token));
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5701 "asmparse.cpp"
    break;

  case 382: /* finallyClause: FINALLY_  */
#line 1012 "asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FINALLY;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5708 "asmparse.cpp"
    break;

  case 383: /* faultClause: FAULT_  */
#line 1016 "asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FAULT;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 5715 "asmparse.cpp"
    break;

  case 384: /* handlerBlock: scopeBlock  */
#line 1020 "asmparse.y"
                                                             { PASM->m_SEHD->sehHandlerTo = PASM->m_CurPC; }
#line 5721 "asmparse.cpp"
    break;

  case 385: /* handlerBlock: HANDLER_ id TO_ id  */
#line 1021 "asmparse.y"
                                                             { PASM->SetHandlerLabels((yyvsp[-2].string), (yyvsp[0].string)); }
#line 5727 "asmparse.cpp"
    break;

  case 386: /* handlerBlock: HANDLER_ int32 TO_ int32  */
#line 1022 "asmparse.y"
                                                             { PASM->m_SEHD->sehHandler = (yyvsp[-2].int32);
                                                               PASM->m_SEHD->sehHandlerTo = (yyvsp[0].int32); }
#line 5734 "asmparse.cpp"
    break;

  case 388: /* ddHead: _DATA tls id '='  */
#line 1030 "asmparse.y"
                                                             { PASM->EmitDataLabel((yyvsp[-1].string)); }
#line 5740 "asmparse.cpp"
    break;

  case 390: /* tls: %empty  */
#line 1034 "asmparse.y"
                                                             { PASM->SetDataSection(); }
#line 5746 "asmparse.cpp"
    break;

  case 391: /* tls: TLS_  */
#line 1035 "asmparse.y"
                                                             { PASM->SetTLSSection(); }
#line 5752 "asmparse.cpp"
    break;

  case 392: /* tls: CIL_  */
#line 1036 "asmparse.y"
                                                             { PASM->SetILSection(); }
#line 5758 "asmparse.cpp"
    break;

  case 397: /* ddItemCount: %empty  */
#line 1047 "asmparse.y"
                                                             { (yyval.int32) = 1; }
#line 5764 "asmparse.cpp"
    break;

  case 398: /* ddItemCount: '[' int32 ']'  */
#line 1048 "asmparse.y"
                                                             { (yyval.int32) = (yyvsp[-1].int32);
                                                               if((yyvsp[-1].int32) <= 0) { PASM->report->error("Illegal item count: %d\n",(yyvsp[-1].int32));
                                                                  if(!PASM->OnErrGo) (yyval.int32) = 1; }}
#line 5772 "asmparse.cpp"
    break;

  case 399: /* ddItem: CHAR_ '*' '(' compQstring ')'  */
#line 1053 "asmparse.y"
                                                             { PASM->EmitDataString((yyvsp[-1].binstr)); }
#line 5778 "asmparse.cpp"
    break;

  case 400: /* ddItem: '&' '(' id ')'  */
#line 1054 "asmparse.y"
                                                             { PASM->EmitDD((yyvsp[-1].string)); }
#line 5784 "asmparse.cpp"
    break;

  case 401: /* ddItem: bytearrayhead bytes ')'  */
#line 1055 "asmparse.y"
                                                             { PASM->EmitData((yyvsp[-1].binstr)->ptr(),(yyvsp[-1].binstr)->length()); }
#line 5790 "asmparse.cpp"
    break;

  case 402: /* ddItem: FLOAT32_ '(' float64 ')' ddItemCount  */
#line 1057 "asmparse.y"
                                                             { float f = (float) (*(yyvsp[-2].float64)); float* p = new (nothrow) float[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i < (yyvsp[0].int32); i++) p[i] = f;
                                                                 PASM->EmitData(p, sizeof(float)*(yyvsp[0].int32)); delete (yyvsp[-2].float64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(float)*(yyvsp[0].int32)); }
#line 5801 "asmparse.cpp"
    break;

  case 403: /* ddItem: FLOAT64_ '(' float64 ')' ddItemCount  */
#line 1064 "asmparse.y"
                                                             { double* p = new (nothrow) double[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = *((yyvsp[-2].float64));
                                                                 PASM->EmitData(p, sizeof(double)*(yyvsp[0].int32)); delete (yyvsp[-2].float64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(double)*(yyvsp[0].int32)); }
#line 5812 "asmparse.cpp"
    break;

  case 404: /* ddItem: INT64_ '(' int64 ')' ddItemCount  */
#line 1071 "asmparse.y"
                                                             { int64_t* p = new (nothrow) int64_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = *((yyvsp[-2].int64));
                                                                 PASM->EmitData(p, sizeof(int64_t)*(yyvsp[0].int32)); delete (yyvsp[-2].int64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int64_t)*(yyvsp[0].int32)); }
#line 5823 "asmparse.cpp"
    break;

  case 405: /* ddItem: INT32_ '(' int32 ')' ddItemCount  */
#line 1078 "asmparse.y"
                                                             { int32_t* p = new (nothrow) int32_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = (yyvsp[-2].int32);
                                                                 PASM->EmitData(p, sizeof(int32_t)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int32_t)*(yyvsp[0].int32)); }
#line 5834 "asmparse.cpp"
    break;

  case 406: /* ddItem: INT16_ '(' int32 ')' ddItemCount  */
#line 1085 "asmparse.y"
                                                             { int16_t i = (int16_t) (yyvsp[-2].int32); FAIL_UNLESS(i == (yyvsp[-2].int32), ("Value %d too big\n", (yyvsp[-2].int32)));
                                                               int16_t* p = new (nothrow) int16_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<(yyvsp[0].int32); j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(int16_t)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int16_t)*(yyvsp[0].int32)); }
#line 5846 "asmparse.cpp"
    break;

  case 407: /* ddItem: INT8_ '(' int32 ')' ddItemCount  */
#line 1093 "asmparse.y"
                                                             { int8_t i = (int8_t) (yyvsp[-2].int32); FAIL_UNLESS(i == (yyvsp[-2].int32), ("Value %d too big\n", (yyvsp[-2].int32)));
                                                               int8_t* p = new (nothrow) int8_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<(yyvsp[0].int32); j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(int8_t)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int8_t)*(yyvsp[0].int32)); }
#line 5858 "asmparse.cpp"
    break;

  case 408: /* ddItem: FLOAT32_ ddItemCount  */
#line 1100 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(float)*(yyvsp[0].int32)); }
#line 5864 "asmparse.cpp"
    break;

  case 409: /* ddItem: FLOAT64_ ddItemCount  */
#line 1101 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(double)*(yyvsp[0].int32)); }
#line 5870 "asmparse.cpp"
    break;

  case 410: /* ddItem: INT64_ ddItemCount  */
#line 1102 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int64_t)*(yyvsp[0].int32)); }
#line 5876 "asmparse.cpp"
    break;

  case 411: /* ddItem: INT32_ ddItemCount  */
#line 1103 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int32_t)*(yyvsp[0].int32)); }
#line 5882 "asmparse.cpp"
    break;

  case 412: /* ddItem: INT16_ ddItemCount  */
#line 1104 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int16_t)*(yyvsp[0].int32)); }
#line 5888 "asmparse.cpp"
    break;

  case 413: /* ddItem: INT8_ ddItemCount  */
#line 1105 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int8_t)*(yyvsp[0].int32)); }
#line 5894 "asmparse.cpp"
    break;

  case 414: /* fieldSerInit: FLOAT32_ '(' float64 ')'  */
#line 1109 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               float f = (float)(*(yyvsp[-1].float64));
                                                               (yyval.binstr)->appendInt32(*((int32_t*)&f)); delete (yyvsp[-1].float64); }
#line 5902 "asmparse.cpp"
    break;

  case 415: /* fieldSerInit: FLOAT64_ '(' float64 ')'  */
#line 1112 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].float64)); delete (yyvsp[-1].float64); }
#line 5909 "asmparse.cpp"
    break;

  case 416: /* fieldSerInit: FLOAT32_ '(' int32 ')'  */
#line 1114 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 5916 "asmparse.cpp"
    break;

  case 417: /* fieldSerInit: FLOAT64_ '(' int64 ')'  */
#line 1116 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 5923 "asmparse.cpp"
    break;

  case 418: /* fieldSerInit: INT64_ '(' int64 ')'  */
#line 1118 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 5930 "asmparse.cpp"
    break;

  case 419: /* fieldSerInit: INT32_ '(' int32 ')'  */
#line 1120 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 5937 "asmparse.cpp"
    break;

  case 420: /* fieldSerInit: INT16_ '(' int32 ')'  */
#line 1122 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 5944 "asmparse.cpp"
    break;

  case 421: /* fieldSerInit: INT8_ '(' int32 ')'  */
#line 1124 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 5951 "asmparse.cpp"
    break;

  case 422: /* fieldSerInit: UNSIGNED_ INT64_ '(' int64 ')'  */
#line 1126 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 5958 "asmparse.cpp"
    break;

  case 423: /* fieldSerInit: UNSIGNED_ INT32_ '(' int32 ')'  */
#line 1128 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 5965 "asmparse.cpp"
    break;

  case 424: /* fieldSerInit: UNSIGNED_ INT16_ '(' int32 ')'  */
#line 1130 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 5972 "asmparse.cpp"
    break;

  case 425: /* fieldSerInit: UNSIGNED_ INT8_ '(' int32 ')'  */
#line 1132 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 5979 "asmparse.cpp"
    break;

  case 426: /* fieldSerInit: UINT64_ '(' int64 ')'  */
#line 1134 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 5986 "asmparse.cpp"
    break;

  case 427: /* fieldSerInit: UINT32_ '(' int32 ')'  */
#line 1136 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 5993 "asmparse.cpp"
    break;

  case 428: /* fieldSerInit: UINT16_ '(' int32 ')'  */
#line 1138 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6000 "asmparse.cpp"
    break;

  case 429: /* fieldSerInit: UINT8_ '(' int32 ')'  */
#line 1140 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6007 "asmparse.cpp"
    break;

  case 430: /* fieldSerInit: CHAR_ '(' int32 ')'  */
#line 1142 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CHAR);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6014 "asmparse.cpp"
    break;

  case 431: /* fieldSerInit: BOOL_ '(' truefalse ')'  */
#line 1144 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_BOOLEAN);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32));}
#line 6021 "asmparse.cpp"
    break;

  case 432: /* fieldSerInit: bytearrayhead bytes ')'  */
#line 1146 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING);
                                                               (yyval.binstr)->append((yyvsp[-1].binstr)); delete (yyvsp[-1].binstr);}
#line 6028 "asmparse.cpp"
    break;

  case 433: /* bytearrayhead: BYTEARRAY_ '('  */
#line 1150 "asmparse.y"
                                                             { bParsingByteArray = TRUE; }
#line 6034 "asmparse.cpp"
    break;

  case 434: /* bytes: %empty  */
#line 1153 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6040 "asmparse.cpp"
    break;

  case 435: /* bytes: hexbytes  */
#line 1154 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6046 "asmparse.cpp"
    break;

  case 436: /* hexbytes: HEXBYTE  */
#line 1157 "asmparse.y"
                                                             { int8_t i = (int8_t) (yyvsp[0].int32); (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(i); }
#line 6052 "asmparse.cpp"
    break;

  case 437: /* hexbytes: hexbytes HEXBYTE  */
#line 1158 "asmparse.y"
                                                             { int8_t i = (int8_t) (yyvsp[0].int32); (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(i); }
#line 6058 "asmparse.cpp"
    break;

  case 438: /* fieldInit: fieldSerInit  */
#line 1162 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6064 "asmparse.cpp"
    break;

  case 439: /* fieldInit: compQstring  */
#line 1163 "asmparse.y"
                                                             { (yyval.binstr) = BinStrToUnicode((yyvsp[0].binstr),true); (yyval.binstr)->insertInt8(ELEMENT_TYPE_STRING);}
#line 6070 "asmparse.cpp"
    break;

  case 440: /* fieldInit: NULLREF_  */
#line 1164 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CLASS);
                                                               (yyval.binstr)->appendInt32(0); }
#line 6077 "asmparse.cpp"
    break;

  case 441: /* serInit: fieldSerInit  */
#line 1169 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6083 "asmparse.cpp"
    break;

  case 442: /* serInit: STRING_ '(' NULLREF_ ')'  */
#line 1170 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); (yyval.binstr)->appendInt8(0xFF); }
#line 6089 "asmparse.cpp"
    break;

  case 443: /* serInit: STRING_ '(' SQSTRING ')'  */
#line 1171 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[-1].string)); delete [] (yyvsp[-1].string);}
#line 6096 "asmparse.cpp"
    break;

  case 444: /* serInit: TYPE_ '(' CLASS_ SQSTRING ')'  */
#line 1173 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[-1].string)); delete [] (yyvsp[-1].string);}
#line 6103 "asmparse.cpp"
    break;

  case 445: /* serInit: TYPE_ '(' className ')'  */
#line 1175 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[-1].token)));}
#line 6110 "asmparse.cpp"
    break;

  case 446: /* serInit: TYPE_ '(' NULLREF_ ')'  */
#line 1177 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE); (yyval.binstr)->appendInt8(0xFF); }
#line 6116 "asmparse.cpp"
    break;

  case 447: /* serInit: OBJECT_ '(' serInit ')'  */
#line 1178 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);}
#line 6122 "asmparse.cpp"
    break;

  case 448: /* serInit: FLOAT32_ '[' int32 ']' '(' f32seq ')'  */
#line 1180 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_R4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6130 "asmparse.cpp"
    break;

  case 449: /* serInit: FLOAT64_ '[' int32 ']' '(' f64seq ')'  */
#line 1184 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6138 "asmparse.cpp"
    break;

  case 450: /* serInit: INT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1188 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6146 "asmparse.cpp"
    break;

  case 451: /* serInit: INT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1192 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6154 "asmparse.cpp"
    break;

  case 452: /* serInit: INT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1196 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6162 "asmparse.cpp"
    break;

  case 453: /* serInit: INT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1200 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6170 "asmparse.cpp"
    break;

  case 454: /* serInit: UINT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1204 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6178 "asmparse.cpp"
    break;

  case 455: /* serInit: UINT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1208 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6186 "asmparse.cpp"
    break;

  case 456: /* serInit: UINT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1212 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6194 "asmparse.cpp"
    break;

  case 457: /* serInit: UINT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1216 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6202 "asmparse.cpp"
    break;

  case 458: /* serInit: UNSIGNED_ INT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1220 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6210 "asmparse.cpp"
    break;

  case 459: /* serInit: UNSIGNED_ INT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1224 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6218 "asmparse.cpp"
    break;

  case 460: /* serInit: UNSIGNED_ INT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1228 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6226 "asmparse.cpp"
    break;

  case 461: /* serInit: UNSIGNED_ INT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1232 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6234 "asmparse.cpp"
    break;

  case 462: /* serInit: CHAR_ '[' int32 ']' '(' i16seq ')'  */
#line 1236 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_CHAR);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6242 "asmparse.cpp"
    break;

  case 463: /* serInit: BOOL_ '[' int32 ']' '(' boolSeq ')'  */
#line 1240 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_BOOLEAN);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6250 "asmparse.cpp"
    break;

  case 464: /* serInit: STRING_ '[' int32 ']' '(' sqstringSeq ')'  */
#line 1244 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_STRING);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6258 "asmparse.cpp"
    break;

  case 465: /* serInit: TYPE_ '[' int32 ']' '(' classSeq ')'  */
#line 1248 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TYPE);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6266 "asmparse.cpp"
    break;

  case 466: /* serInit: OBJECT_ '[' int32 ']' '(' objSeq ')'  */
#line 1252 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6274 "asmparse.cpp"
    break;

  case 467: /* f32seq: %empty  */
#line 1258 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6280 "asmparse.cpp"
    break;

  case 468: /* f32seq: f32seq float64  */
#line 1259 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               float f = (float) (*(yyvsp[0].float64)); (yyval.binstr)->appendInt32(*((int32_t*)&f)); delete (yyvsp[0].float64); }
#line 6287 "asmparse.cpp"
    break;

  case 469: /* f32seq: f32seq int32  */
#line 1261 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 6294 "asmparse.cpp"
    break;

  case 470: /* f64seq: %empty  */
#line 1265 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6300 "asmparse.cpp"
    break;

  case 471: /* f64seq: f64seq float64  */
#line 1266 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[0].float64)); delete (yyvsp[0].float64); }
#line 6307 "asmparse.cpp"
    break;

  case 472: /* f64seq: f64seq int64  */
#line 1268 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[0].int64)); delete (yyvsp[0].int64); }
#line 6314 "asmparse.cpp"
    break;

  case 473: /* i64seq: %empty  */
#line 1272 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6320 "asmparse.cpp"
    break;

  case 474: /* i64seq: i64seq int64  */
#line 1273 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[0].int64)); delete (yyvsp[0].int64); }
#line 6327 "asmparse.cpp"
    break;

  case 475: /* i32seq: %empty  */
#line 1277 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6333 "asmparse.cpp"
    break;

  case 476: /* i32seq: i32seq int32  */
#line 1278 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt32((yyvsp[0].int32));}
#line 6339 "asmparse.cpp"
    break;

  case 477: /* i16seq: %empty  */
#line 1281 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6345 "asmparse.cpp"
    break;

  case 478: /* i16seq: i16seq int32  */
#line 1282 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt16((yyvsp[0].int32));}
#line 6351 "asmparse.cpp"
    break;

  case 479: /* i8seq: %empty  */
#line 1285 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6357 "asmparse.cpp"
    break;

  case 480: /* i8seq: i8seq int32  */
#line 1286 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8((yyvsp[0].int32)); }
#line 6363 "asmparse.cpp"
    break;

  case 481: /* boolSeq: %empty  */
#line 1289 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6369 "asmparse.cpp"
    break;

  case 482: /* boolSeq: boolSeq truefalse  */
#line 1290 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt8((yyvsp[0].int32));}
#line 6376 "asmparse.cpp"
    break;

  case 483: /* sqstringSeq: %empty  */
#line 1294 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6382 "asmparse.cpp"
    break;

  case 484: /* sqstringSeq: sqstringSeq NULLREF_  */
#line 1295 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(0xFF); }
#line 6388 "asmparse.cpp"
    break;

  case 485: /* sqstringSeq: sqstringSeq SQSTRING  */
#line 1296 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); delete [] (yyvsp[0].string);}
#line 6395 "asmparse.cpp"
    break;

  case 486: /* classSeq: %empty  */
#line 1300 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6401 "asmparse.cpp"
    break;

  case 487: /* classSeq: classSeq NULLREF_  */
#line 1301 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(0xFF); }
#line 6407 "asmparse.cpp"
    break;

  case 488: /* classSeq: classSeq CLASS_ SQSTRING  */
#line 1302 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); delete [] (yyvsp[0].string);}
#line 6414 "asmparse.cpp"
    break;

  case 489: /* classSeq: classSeq className  */
#line 1304 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[0].token)));}
#line 6421 "asmparse.cpp"
    break;

  case 490: /* objSeq: %empty  */
#line 1308 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6427 "asmparse.cpp"
    break;

  case 491: /* objSeq: objSeq serInit  */
#line 1309 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 6433 "asmparse.cpp"
    break;

  case 492: /* methodSpec: METHOD_  */
#line 1313 "asmparse.y"
                                                             { parser->m_ANSFirst.PUSH(PASM->m_firstArgName);
                                                               parser->m_ANSLast.PUSH(PASM->m_lastArgName);
                                                               PASM->m_firstArgName = NULL;
                                                               PASM->m_lastArgName = NULL; }
#line 6442 "asmparse.cpp"
    break;

  case 493: /* instr_none: INSTR_NONE  */
#line 1319 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6448 "asmparse.cpp"
    break;

  case 494: /* instr_var: INSTR_VAR  */
#line 1322 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6454 "asmparse.cpp"
    break;

  case 495: /* instr_i: INSTR_I  */
#line 1325 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6460 "asmparse.cpp"
    break;

  case 496: /* instr_i8: INSTR_I8  */
#line 1328 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6466 "asmparse.cpp"
    break;

  case 497: /* instr_r: INSTR_R  */
#line 1331 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6472 "asmparse.cpp"
    break;

  case 498: /* instr_brtarget: INSTR_BRTARGET  */
#line 1334 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6478 "asmparse.cpp"
    break;

  case 499: /* instr_method: INSTR_METHOD  */
#line 1337 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode));
                                                               if((!PASM->OnErrGo)&&
                                                               (((yyvsp[0].opcode) == CEE_NEWOBJ)||
                                                                ((yyvsp[0].opcode) == CEE_CALLVIRT)))
                                                                  iCallConv = IMAGE_CEE_CS_CALLCONV_HASTHIS;
                                                             }
#line 6489 "asmparse.cpp"
    break;

  case 500: /* instr_field: INSTR_FIELD  */
#line 1345 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6495 "asmparse.cpp"
    break;

  case 501: /* instr_type: INSTR_TYPE  */
#line 1348 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6501 "asmparse.cpp"
    break;

  case 502: /* instr_string: INSTR_STRING  */
#line 1351 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6507 "asmparse.cpp"
    break;

  case 503: /* instr_sig: INSTR_SIG  */
#line 1354 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6513 "asmparse.cpp"
    break;

  case 504: /* instr_tok: INSTR_TOK  */
#line 1357 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); iOpcodeLen = PASM->OpcodeLen((yyval.instr)); }
#line 6519 "asmparse.cpp"
    break;

  case 505: /* instr_switch: INSTR_SWITCH  */
#line 1360 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6525 "asmparse.cpp"
    break;

  case 506: /* instr_r_head: instr_r '('  */
#line 1363 "asmparse.y"
                                                             { (yyval.instr) = (yyvsp[-1].instr); bParsingByteArray = TRUE; }
#line 6531 "asmparse.cpp"
    break;

  case 507: /* instr: instr_none  */
#line 1367 "asmparse.y"
                                                             { PASM->EmitOpcode((yyvsp[0].instr)); }
#line 6537 "asmparse.cpp"
    break;

  case 508: /* instr: instr_var int32  */
#line 1368 "asmparse.y"
                                                             { PASM->EmitInstrVar((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 6543 "asmparse.cpp"
    break;

  case 509: /* instr: instr_var id  */
#line 1369 "asmparse.y"
                                                             { PASM->EmitInstrVarByName((yyvsp[-1].instr), (yyvsp[0].string)); }
#line 6549 "asmparse.cpp"
    break;

  case 510: /* instr: instr_i int32  */
#line 1370 "asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 6555 "asmparse.cpp"
    break;

  case 511: /* instr: instr_i8 int64  */
#line 1371 "asmparse.y"
                                                             { PASM->EmitInstrI8((yyvsp[-1].instr), (yyvsp[0].int64)); }
#line 6561 "asmparse.cpp"
    break;

  case 512: /* instr: instr_r float64  */
#line 1372 "asmparse.y"
                                                             { PASM->EmitInstrR((yyvsp[-1].instr), (yyvsp[0].float64)); delete ((yyvsp[0].float64));}
#line 6567 "asmparse.cpp"
    break;

  case 513: /* instr: instr_r int64  */
#line 1373 "asmparse.y"
                                                             { double f = (double) (*(yyvsp[0].int64)); PASM->EmitInstrR((yyvsp[-1].instr), &f); }
#line 6573 "asmparse.cpp"
    break;

  case 514: /* instr: instr_r_head bytes ')'  */
#line 1374 "asmparse.y"
                                                             { unsigned L = (yyvsp[-1].binstr)->length();
                                                               FAIL_UNLESS(L >= sizeof(float), ("%d hexbytes, must be at least %d\n",
                                                                           L,sizeof(float)));
                                                               if(L < sizeof(float)) {YYERROR; }
                                                               else {
                                                                   double f = (L >= sizeof(double)) ? *((double *)((yyvsp[-1].binstr)->ptr()))
                                                                                    : (double)(*(float *)((yyvsp[-1].binstr)->ptr()));
                                                                   PASM->EmitInstrR((yyvsp[-2].instr),&f); }
                                                               delete (yyvsp[-1].binstr); }
#line 6587 "asmparse.cpp"
    break;

  case 515: /* instr: instr_brtarget int32  */
#line 1383 "asmparse.y"
                                                             { PASM->EmitInstrBrOffset((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 6593 "asmparse.cpp"
    break;

  case 516: /* instr: instr_brtarget id  */
#line 1384 "asmparse.y"
                                                             { PASM->EmitInstrBrTarget((yyvsp[-1].instr), (yyvsp[0].string)); }
#line 6599 "asmparse.cpp"
    break;

  case 517: /* instr: instr_method methodRef  */
#line 1386 "asmparse.y"
                                                             { PASM->SetMemberRefFixup((yyvsp[0].token),PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),(yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iCallConv = 0;
                                                             }
#line 6610 "asmparse.cpp"
    break;

  case 518: /* instr: instr_field type typeSpec DCOLON dottedName  */
#line 1393 "asmparse.y"
                                                             { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef((yyvsp[-2].token), (yyvsp[0].string), (yyvsp[-3].binstr));
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-4].instr)));
                                                               PASM->EmitInstrI((yyvsp[-4].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6622 "asmparse.cpp"
    break;

  case 519: /* instr: instr_field type dottedName  */
#line 1401 "asmparse.y"
                                                             { (yyvsp[-1].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef(mdTokenNil, (yyvsp[0].string), (yyvsp[-1].binstr));
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-2].instr)));
                                                               PASM->EmitInstrI((yyvsp[-2].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6634 "asmparse.cpp"
    break;

  case 520: /* instr: instr_field mdtoken  */
#line 1408 "asmparse.y"
                                                             { mdToken mr = (yyvsp[0].token);
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6645 "asmparse.cpp"
    break;

  case 521: /* instr: instr_field TYPEDEF_F  */
#line 1414 "asmparse.y"
                                                             { mdToken mr = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6656 "asmparse.cpp"
    break;

  case 522: /* instr: instr_field TYPEDEF_MR  */
#line 1420 "asmparse.y"
                                                             { mdToken mr = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6667 "asmparse.cpp"
    break;

  case 523: /* instr: instr_type typeSpec  */
#line 1426 "asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr), (yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 6676 "asmparse.cpp"
    break;

  case 524: /* instr: instr_string compQstring  */
#line 1430 "asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-1].instr), (yyvsp[0].binstr),TRUE); }
#line 6682 "asmparse.cpp"
    break;

  case 525: /* instr: instr_string ANSI_ '(' compQstring ')'  */
#line 1432 "asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-4].instr), (yyvsp[-1].binstr),FALSE); }
#line 6688 "asmparse.cpp"
    break;

  case 526: /* instr: instr_string bytearrayhead bytes ')'  */
#line 1434 "asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-3].instr), (yyvsp[-1].binstr),FALSE,TRUE); }
#line 6694 "asmparse.cpp"
    break;

  case 527: /* instr: instr_sig callConv type '(' sigArgs0 ')'  */
#line 1436 "asmparse.y"
                                                             { PASM->EmitInstrSig((yyvsp[-5].instr), parser->MakeSig((yyvsp[-4].int32), (yyvsp[-3].binstr), (yyvsp[-1].binstr)));
                                                               PASM->ResetArgNameList();
                                                             }
#line 6702 "asmparse.cpp"
    break;

  case 528: /* instr: instr_tok ownerType  */
#line 1440 "asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr),(yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iOpcodeLen = 0;
                                                             }
#line 6712 "asmparse.cpp"
    break;

  case 529: /* instr: instr_switch '(' labels ')'  */
#line 1445 "asmparse.y"
                                                             { PASM->EmitInstrSwitch((yyvsp[-3].instr), (yyvsp[-1].labels)); }
#line 6718 "asmparse.cpp"
    break;

  case 530: /* labels: %empty  */
#line 1448 "asmparse.y"
                                                              { (yyval.labels) = 0; }
#line 6724 "asmparse.cpp"
    break;

  case 531: /* labels: id ',' labels  */
#line 1449 "asmparse.y"
                                                              { (yyval.labels) = new Labels((yyvsp[-2].string), (yyvsp[0].labels), TRUE); }
#line 6730 "asmparse.cpp"
    break;

  case 532: /* labels: int32 ',' labels  */
#line 1450 "asmparse.y"
                                                              { (yyval.labels) = new Labels((char *)(UINT_PTR)(yyvsp[-2].int32), (yyvsp[0].labels), FALSE); }
#line 6736 "asmparse.cpp"
    break;

  case 533: /* labels: id  */
#line 1451 "asmparse.y"
                                                              { (yyval.labels) = new Labels((yyvsp[0].string), NULL, TRUE); }
#line 6742 "asmparse.cpp"
    break;

  case 534: /* labels: int32  */
#line 1452 "asmparse.y"
                                                              { (yyval.labels) = new Labels((char *)(UINT_PTR)(yyvsp[0].int32), NULL, FALSE); }
#line 6748 "asmparse.cpp"
    break;

  case 535: /* tyArgs0: %empty  */
#line 1456 "asmparse.y"
                                                             { (yyval.binstr) = NULL; }
#line 6754 "asmparse.cpp"
    break;

  case 536: /* tyArgs0: '<' tyArgs1 '>'  */
#line 1457 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 6760 "asmparse.cpp"
    break;

  case 537: /* tyArgs1: %empty  */
#line 1460 "asmparse.y"
                                                             { (yyval.binstr) = NULL; }
#line 6766 "asmparse.cpp"
    break;

  case 538: /* tyArgs1: tyArgs2  */
#line 1461 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6772 "asmparse.cpp"
    break;

  case 539: /* tyArgs2: type  */
#line 1464 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6778 "asmparse.cpp"
    break;

  case 540: /* tyArgs2: tyArgs2 ',' type  */
#line 1465 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 6784 "asmparse.cpp"
    break;

  case 541: /* sigArgs0: %empty  */
#line 1469 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6790 "asmparse.cpp"
    break;

  case 542: /* sigArgs0: sigArgs1  */
#line 1470 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr);}
#line 6796 "asmparse.cpp"
    break;

  case 543: /* sigArgs1: sigArg  */
#line 1473 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6802 "asmparse.cpp"
    break;

  case 544: /* sigArgs1: sigArgs1 ',' sigArg  */
#line 1474 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 6808 "asmparse.cpp"
    break;

  case 545: /* sigArg: ELLIPSIS  */
#line 1477 "asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_SENTINEL); }
#line 6814 "asmparse.cpp"
    break;

  case 546: /* sigArg: paramAttr type marshalClause  */
#line 1478 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[-1].binstr)); PASM->addArgName(NULL, (yyvsp[-1].binstr), (yyvsp[0].binstr), (yyvsp[-2].int32)); }
#line 6820 "asmparse.cpp"
    break;

  case 547: /* sigArg: paramAttr type marshalClause id  */
#line 1479 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[-2].binstr)); PASM->addArgName((yyvsp[0].string), (yyvsp[-2].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32));}
#line 6826 "asmparse.cpp"
    break;

  case 548: /* className: '[' dottedName ']' slashedName  */
#line 1483 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(PASM->GetAsmRef((yyvsp[-2].string)), (yyvsp[0].string), NULL); delete[] (yyvsp[-2].string);}
#line 6832 "asmparse.cpp"
    break;

  case 549: /* className: '[' mdtoken ']' slashedName  */
#line 1484 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef((yyvsp[-2].token), (yyvsp[0].string), NULL); }
#line 6838 "asmparse.cpp"
    break;

  case 550: /* className: '[' '*' ']' slashedName  */
#line 1485 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(mdTokenNil, (yyvsp[0].string), NULL); }
#line 6844 "asmparse.cpp"
    break;

  case 551: /* className: '[' _MODULE dottedName ']' slashedName  */
#line 1486 "asmparse.y"
                                                                   { (yyval.token) = PASM->ResolveClassRef(PASM->GetModRef((yyvsp[-2].string)),(yyvsp[0].string), NULL); delete[] (yyvsp[-2].string);}
#line 6850 "asmparse.cpp"
    break;

  case 552: /* className: slashedName  */
#line 1487 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(1,(yyvsp[0].string),NULL); }
#line 6856 "asmparse.cpp"
    break;

  case 553: /* className: mdtoken  */
#line 1488 "asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].token); }
#line 6862 "asmparse.cpp"
    break;

  case 554: /* className: TYPEDEF_T  */
#line 1489 "asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 6868 "asmparse.cpp"
    break;

  case 555: /* className: _THIS  */
#line 1490 "asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) (yyval.token) = PASM->m_pCurClass->m_cl;
                                                                else { (yyval.token) = 0; PASM->report->error(".this outside class scope\n"); }
                                                              }
#line 6876 "asmparse.cpp"
    break;

  case 556: /* className: _BASE  */
#line 1493 "asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) {
                                                                  (yyval.token) = PASM->m_pCurClass->m_crExtends;
                                                                  if(RidFromToken((yyval.token)) == 0)
                                                                    PASM->report->error(".base undefined\n");
                                                                } else { (yyval.token) = 0; PASM->report->error(".base outside class scope\n"); }
                                                              }
#line 6887 "asmparse.cpp"
    break;

  case 557: /* className: _NESTER  */
#line 1499 "asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) {
                                                                  if(PASM->m_pCurClass->m_pEncloser != NULL) (yyval.token) = PASM->m_pCurClass->m_pEncloser->m_cl;
                                                                  else { (yyval.token) = 0; PASM->report->error(".nester undefined\n"); }
                                                                } else { (yyval.token) = 0; PASM->report->error(".nester outside class scope\n"); }
                                                              }
#line 6897 "asmparse.cpp"
    break;

  case 558: /* slashedName: dottedName  */
#line 1506 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 6903 "asmparse.cpp"
    break;

  case 559: /* slashedName: slashedName '/' dottedName  */
#line 1507 "asmparse.y"
                                                              { (yyval.string) = newStringWDel((yyvsp[-2].string), NESTING_SEP, (yyvsp[0].string)); }
#line 6909 "asmparse.cpp"
    break;

  case 560: /* typeSpec: className  */
#line 1510 "asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].token);}
#line 6915 "asmparse.cpp"
    break;

  case 561: /* typeSpec: '[' dottedName ']'  */
#line 1511 "asmparse.y"
                                                              { (yyval.token) = PASM->GetAsmRef((yyvsp[-1].string)); delete[] (yyvsp[-1].string);}
#line 6921 "asmparse.cpp"
    break;

  case 562: /* typeSpec: '[' _MODULE dottedName ']'  */
#line 1512 "asmparse.y"
                                                              { (yyval.token) = PASM->GetModRef((yyvsp[-1].string)); delete[] (yyvsp[-1].string);}
#line 6927 "asmparse.cpp"
    break;

  case 563: /* typeSpec: type  */
#line 1513 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveTypeSpec((yyvsp[0].binstr)); }
#line 6933 "asmparse.cpp"
    break;

  case 564: /* nativeType: %empty  */
#line 1517 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); }
#line 6939 "asmparse.cpp"
    break;

  case 565: /* nativeType: CUSTOM_ '(' compQstring ',' compQstring ',' compQstring ',' compQstring ')'  */
#line 1519 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt((yyval.binstr),(yyvsp[-7].binstr)->length()); (yyval.binstr)->append((yyvsp[-7].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-5].binstr)->length()); (yyval.binstr)->append((yyvsp[-5].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].binstr)->length()); (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].binstr)->length()); (yyval.binstr)->append((yyvsp[-1].binstr));
                                                                PASM->report->warn("Deprecated 4-string form of custom marshaler, first two strings ignored\n");}
#line 6950 "asmparse.cpp"
    break;

  case 566: /* nativeType: CUSTOM_ '(' compQstring ',' compQstring ')'  */
#line 1526 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].binstr)->length()); (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].binstr)->length()); (yyval.binstr)->append((yyvsp[-1].binstr)); }
#line 6960 "asmparse.cpp"
    break;

  case 567: /* nativeType: FIXED_ SYSSTRING_ '[' int32 ']'  */
#line 1531 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FIXEDSYSSTRING);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32)); }
#line 6967 "asmparse.cpp"
    break;

  case 568: /* nativeType: FIXED_ ARRAY_ '[' int32 ']' nativeType  */
#line 1534 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FIXEDARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-2].int32)); (yyval.binstr)->append((yyvsp[0].binstr)); }
#line 6974 "asmparse.cpp"
    break;

  case 569: /* nativeType: VARIANT_  */
#line 1536 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VARIANT);
                                                                PASM->report->warn("Deprecated native type 'variant'\n"); }
#line 6981 "asmparse.cpp"
    break;

  case 570: /* nativeType: CURRENCY_  */
#line 1538 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CURRENCY); }
#line 6987 "asmparse.cpp"
    break;

  case 571: /* nativeType: SYSCHAR_  */
#line 1539 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SYSCHAR);
                                                                PASM->report->warn("Deprecated native type 'syschar'\n"); }
#line 6994 "asmparse.cpp"
    break;

  case 572: /* nativeType: VOID_  */
#line 1541 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VOID);
                                                                PASM->report->warn("Deprecated native type 'void'\n"); }
#line 7001 "asmparse.cpp"
    break;

  case 573: /* nativeType: BOOL_  */
#line 1543 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BOOLEAN); }
#line 7007 "asmparse.cpp"
    break;

  case 574: /* nativeType: INT8_  */
#line 1544 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I1); }
#line 7013 "asmparse.cpp"
    break;

  case 575: /* nativeType: INT16_  */
#line 1545 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I2); }
#line 7019 "asmparse.cpp"
    break;

  case 576: /* nativeType: INT32_  */
#line 1546 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I4); }
#line 7025 "asmparse.cpp"
    break;

  case 577: /* nativeType: INT64_  */
#line 1547 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I8); }
#line 7031 "asmparse.cpp"
    break;

  case 578: /* nativeType: FLOAT32_  */
#line 1548 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_R4); }
#line 7037 "asmparse.cpp"
    break;

  case 579: /* nativeType: FLOAT64_  */
#line 1549 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_R8); }
#line 7043 "asmparse.cpp"
    break;

  case 580: /* nativeType: ERROR_  */
#line 1550 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ERROR); }
#line 7049 "asmparse.cpp"
    break;

  case 581: /* nativeType: UNSIGNED_ INT8_  */
#line 1551 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U1); }
#line 7055 "asmparse.cpp"
    break;

  case 582: /* nativeType: UNSIGNED_ INT16_  */
#line 1552 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U2); }
#line 7061 "asmparse.cpp"
    break;

  case 583: /* nativeType: UNSIGNED_ INT32_  */
#line 1553 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U4); }
#line 7067 "asmparse.cpp"
    break;

  case 584: /* nativeType: UNSIGNED_ INT64_  */
#line 1554 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U8); }
#line 7073 "asmparse.cpp"
    break;

  case 585: /* nativeType: UINT8_  */
#line 1555 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U1); }
#line 7079 "asmparse.cpp"
    break;

  case 586: /* nativeType: UINT16_  */
#line 1556 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U2); }
#line 7085 "asmparse.cpp"
    break;

  case 587: /* nativeType: UINT32_  */
#line 1557 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U4); }
#line 7091 "asmparse.cpp"
    break;

  case 588: /* nativeType: UINT64_  */
#line 1558 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U8); }
#line 7097 "asmparse.cpp"
    break;

  case 589: /* nativeType: nativeType '*'  */
#line 1559 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(NATIVE_TYPE_PTR);
                                                                PASM->report->warn("Deprecated native type '*'\n"); }
#line 7104 "asmparse.cpp"
    break;

  case 590: /* nativeType: nativeType '[' ']'  */
#line 1561 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY); }
#line 7111 "asmparse.cpp"
    break;

  case 591: /* nativeType: nativeType '[' int32 ']'  */
#line 1563 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-3].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32));
                                                                corEmitInt((yyval.binstr),0); }
#line 7121 "asmparse.cpp"
    break;

  case 592: /* nativeType: nativeType '[' int32 '+' int32 ']'  */
#line 1568 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-5].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32));
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].int32));
                                                                corEmitInt((yyval.binstr),ntaSizeParamIndexSpecified); }
#line 7131 "asmparse.cpp"
    break;

  case 593: /* nativeType: nativeType '[' '+' int32 ']'  */
#line 1573 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-4].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32)); }
#line 7139 "asmparse.cpp"
    break;

  case 594: /* nativeType: DECIMAL_  */
#line 1576 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_DECIMAL);
                                                                PASM->report->warn("Deprecated native type 'decimal'\n"); }
#line 7146 "asmparse.cpp"
    break;

  case 595: /* nativeType: DATE_  */
#line 1578 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_DATE);
                                                                PASM->report->warn("Deprecated native type 'date'\n"); }
#line 7153 "asmparse.cpp"
    break;

  case 596: /* nativeType: BSTR_  */
#line 1580 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BSTR); }
#line 7159 "asmparse.cpp"
    break;

  case 597: /* nativeType: LPSTR_  */
#line 1581 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPSTR); }
#line 7165 "asmparse.cpp"
    break;

  case 598: /* nativeType: LPWSTR_  */
#line 1582 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPWSTR); }
#line 7171 "asmparse.cpp"
    break;

  case 599: /* nativeType: LPTSTR_  */
#line 1583 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPTSTR); }
#line 7177 "asmparse.cpp"
    break;

  case 600: /* nativeType: OBJECTREF_  */
#line 1584 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_OBJECTREF);
                                                                PASM->report->warn("Deprecated native type 'objectref'\n"); }
#line 7184 "asmparse.cpp"
    break;

  case 601: /* nativeType: IUNKNOWN_ iidParamIndex  */
#line 1586 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_IUNKNOWN);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7191 "asmparse.cpp"
    break;

  case 602: /* nativeType: IDISPATCH_ iidParamIndex  */
#line 1588 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_IDISPATCH);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7198 "asmparse.cpp"
    break;

  case 603: /* nativeType: STRUCT_  */
#line 1590 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_STRUCT); }
#line 7204 "asmparse.cpp"
    break;

  case 604: /* nativeType: INTERFACE_ iidParamIndex  */
#line 1591 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_INTF);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7211 "asmparse.cpp"
    break;

  case 605: /* nativeType: SAFEARRAY_ variantType  */
#line 1593 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[0].int32));
                                                                corEmitInt((yyval.binstr),0);}
#line 7219 "asmparse.cpp"
    break;

  case 606: /* nativeType: SAFEARRAY_ variantType ',' compQstring  */
#line 1596 "asmparse.y"
                                                                 { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-2].int32));
                                                                corEmitInt((yyval.binstr),(yyvsp[0].binstr)->length()); (yyval.binstr)->append((yyvsp[0].binstr)); }
#line 7227 "asmparse.cpp"
    break;

  case 607: /* nativeType: INT_  */
#line 1600 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_INT); }
#line 7233 "asmparse.cpp"
    break;

  case 608: /* nativeType: UNSIGNED_ INT_  */
#line 1601 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_UINT); }
#line 7239 "asmparse.cpp"
    break;

  case 609: /* nativeType: UINT_  */
#line 1602 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_UINT); }
#line 7245 "asmparse.cpp"
    break;

  case 610: /* nativeType: NESTED_ STRUCT_  */
#line 1603 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_NESTEDSTRUCT);
                                                                PASM->report->warn("Deprecated native type 'nested struct'\n"); }
#line 7252 "asmparse.cpp"
    break;

  case 611: /* nativeType: BYVALSTR_  */
#line 1605 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BYVALSTR); }
#line 7258 "asmparse.cpp"
    break;

  case 612: /* nativeType: ANSI_ BSTR_  */
#line 1606 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ANSIBSTR); }
#line 7264 "asmparse.cpp"
    break;

  case 613: /* nativeType: TBSTR_  */
#line 1607 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_TBSTR); }
#line 7270 "asmparse.cpp"
    break;

  case 614: /* nativeType: VARIANT_ BOOL_  */
#line 1608 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VARIANTBOOL); }
#line 7276 "asmparse.cpp"
    break;

  case 615: /* nativeType: METHOD_  */
#line 1609 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FUNC); }
#line 7282 "asmparse.cpp"
    break;

  case 616: /* nativeType: AS_ ANY_  */
#line 1610 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ASANY); }
#line 7288 "asmparse.cpp"
    break;

  case 617: /* nativeType: LPSTRUCT_  */
#line 1611 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPSTRUCT); }
#line 7294 "asmparse.cpp"
    break;

  case 618: /* nativeType: TYPEDEF_TS  */
#line 1612 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[0].tdd)->m_pbsTypeSpec); }
#line 7300 "asmparse.cpp"
    break;

  case 619: /* iidParamIndex: %empty  */
#line 1615 "asmparse.y"
                                                              { (yyval.int32) = -1; }
#line 7306 "asmparse.cpp"
    break;

  case 620: /* iidParamIndex: '(' IIDPARAM_ '=' int32 ')'  */
#line 1616 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32); }
#line 7312 "asmparse.cpp"
    break;

  case 621: /* variantType: %empty  */
#line 1619 "asmparse.y"
                                                              { (yyval.int32) = VT_EMPTY; }
#line 7318 "asmparse.cpp"
    break;

  case 622: /* variantType: NULL_  */
#line 1620 "asmparse.y"
                                                              { (yyval.int32) = VT_NULL; }
#line 7324 "asmparse.cpp"
    break;

  case 623: /* variantType: VARIANT_  */
#line 1621 "asmparse.y"
                                                              { (yyval.int32) = VT_VARIANT; }
#line 7330 "asmparse.cpp"
    break;

  case 624: /* variantType: CURRENCY_  */
#line 1622 "asmparse.y"
                                                              { (yyval.int32) = VT_CY; }
#line 7336 "asmparse.cpp"
    break;

  case 625: /* variantType: VOID_  */
#line 1623 "asmparse.y"
                                                              { (yyval.int32) = VT_VOID; }
#line 7342 "asmparse.cpp"
    break;

  case 626: /* variantType: BOOL_  */
#line 1624 "asmparse.y"
                                                              { (yyval.int32) = VT_BOOL; }
#line 7348 "asmparse.cpp"
    break;

  case 627: /* variantType: INT8_  */
#line 1625 "asmparse.y"
                                                              { (yyval.int32) = VT_I1; }
#line 7354 "asmparse.cpp"
    break;

  case 628: /* variantType: INT16_  */
#line 1626 "asmparse.y"
                                                              { (yyval.int32) = VT_I2; }
#line 7360 "asmparse.cpp"
    break;

  case 629: /* variantType: INT32_  */
#line 1627 "asmparse.y"
                                                              { (yyval.int32) = VT_I4; }
#line 7366 "asmparse.cpp"
    break;

  case 630: /* variantType: INT64_  */
#line 1628 "asmparse.y"
                                                              { (yyval.int32) = VT_I8; }
#line 7372 "asmparse.cpp"
    break;

  case 631: /* variantType: FLOAT32_  */
#line 1629 "asmparse.y"
                                                              { (yyval.int32) = VT_R4; }
#line 7378 "asmparse.cpp"
    break;

  case 632: /* variantType: FLOAT64_  */
#line 1630 "asmparse.y"
                                                              { (yyval.int32) = VT_R8; }
#line 7384 "asmparse.cpp"
    break;

  case 633: /* variantType: UNSIGNED_ INT8_  */
#line 1631 "asmparse.y"
                                                              { (yyval.int32) = VT_UI1; }
#line 7390 "asmparse.cpp"
    break;

  case 634: /* variantType: UNSIGNED_ INT16_  */
#line 1632 "asmparse.y"
                                                              { (yyval.int32) = VT_UI2; }
#line 7396 "asmparse.cpp"
    break;

  case 635: /* variantType: UNSIGNED_ INT32_  */
#line 1633 "asmparse.y"
                                                              { (yyval.int32) = VT_UI4; }
#line 7402 "asmparse.cpp"
    break;

  case 636: /* variantType: UNSIGNED_ INT64_  */
#line 1634 "asmparse.y"
                                                              { (yyval.int32) = VT_UI8; }
#line 7408 "asmparse.cpp"
    break;

  case 637: /* variantType: UINT8_  */
#line 1635 "asmparse.y"
                                                              { (yyval.int32) = VT_UI1; }
#line 7414 "asmparse.cpp"
    break;

  case 638: /* variantType: UINT16_  */
#line 1636 "asmparse.y"
                                                              { (yyval.int32) = VT_UI2; }
#line 7420 "asmparse.cpp"
    break;

  case 639: /* variantType: UINT32_  */
#line 1637 "asmparse.y"
                                                              { (yyval.int32) = VT_UI4; }
#line 7426 "asmparse.cpp"
    break;

  case 640: /* variantType: UINT64_  */
#line 1638 "asmparse.y"
                                                              { (yyval.int32) = VT_UI8; }
#line 7432 "asmparse.cpp"
    break;

  case 641: /* variantType: '*'  */
#line 1639 "asmparse.y"
                                                              { (yyval.int32) = VT_PTR; }
#line 7438 "asmparse.cpp"
    break;

  case 642: /* variantType: variantType '[' ']'  */
#line 1640 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-2].int32) | VT_ARRAY; }
#line 7444 "asmparse.cpp"
    break;

  case 643: /* variantType: variantType VECTOR_  */
#line 1641 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32) | VT_VECTOR; }
#line 7450 "asmparse.cpp"
    break;

  case 644: /* variantType: variantType '&'  */
#line 1642 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32) | VT_BYREF; }
#line 7456 "asmparse.cpp"
    break;

  case 645: /* variantType: DECIMAL_  */
#line 1643 "asmparse.y"
                                                              { (yyval.int32) = VT_DECIMAL; }
#line 7462 "asmparse.cpp"
    break;

  case 646: /* variantType: DATE_  */
#line 1644 "asmparse.y"
                                                              { (yyval.int32) = VT_DATE; }
#line 7468 "asmparse.cpp"
    break;

  case 647: /* variantType: BSTR_  */
#line 1645 "asmparse.y"
                                                              { (yyval.int32) = VT_BSTR; }
#line 7474 "asmparse.cpp"
    break;

  case 648: /* variantType: LPSTR_  */
#line 1646 "asmparse.y"
                                                              { (yyval.int32) = VT_LPSTR; }
#line 7480 "asmparse.cpp"
    break;

  case 649: /* variantType: LPWSTR_  */
#line 1647 "asmparse.y"
                                                              { (yyval.int32) = VT_LPWSTR; }
#line 7486 "asmparse.cpp"
    break;

  case 650: /* variantType: IUNKNOWN_  */
#line 1648 "asmparse.y"
                                                              { (yyval.int32) = VT_UNKNOWN; }
#line 7492 "asmparse.cpp"
    break;

  case 651: /* variantType: IDISPATCH_  */
#line 1649 "asmparse.y"
                                                              { (yyval.int32) = VT_DISPATCH; }
#line 7498 "asmparse.cpp"
    break;

  case 652: /* variantType: SAFEARRAY_  */
#line 1650 "asmparse.y"
                                                              { (yyval.int32) = VT_SAFEARRAY; }
#line 7504 "asmparse.cpp"
    break;

  case 653: /* variantType: INT_  */
#line 1651 "asmparse.y"
                                                              { (yyval.int32) = VT_INT; }
#line 7510 "asmparse.cpp"
    break;

  case 654: /* variantType: UNSIGNED_ INT_  */
#line 1652 "asmparse.y"
                                                              { (yyval.int32) = VT_UINT; }
#line 7516 "asmparse.cpp"
    break;

  case 655: /* variantType: UINT_  */
#line 1653 "asmparse.y"
                                                              { (yyval.int32) = VT_UINT; }
#line 7522 "asmparse.cpp"
    break;

  case 656: /* variantType: ERROR_  */
#line 1654 "asmparse.y"
                                                              { (yyval.int32) = VT_ERROR; }
#line 7528 "asmparse.cpp"
    break;

  case 657: /* variantType: HRESULT_  */
#line 1655 "asmparse.y"
                                                              { (yyval.int32) = VT_HRESULT; }
#line 7534 "asmparse.cpp"
    break;

  case 658: /* variantType: CARRAY_  */
#line 1656 "asmparse.y"
                                                              { (yyval.int32) = VT_CARRAY; }
#line 7540 "asmparse.cpp"
    break;

  case 659: /* variantType: USERDEFINED_  */
#line 1657 "asmparse.y"
                                                              { (yyval.int32) = VT_USERDEFINED; }
#line 7546 "asmparse.cpp"
    break;

  case 660: /* variantType: RECORD_  */
#line 1658 "asmparse.y"
                                                              { (yyval.int32) = VT_RECORD; }
#line 7552 "asmparse.cpp"
    break;

  case 661: /* variantType: FILETIME_  */
#line 1659 "asmparse.y"
                                                              { (yyval.int32) = VT_FILETIME; }
#line 7558 "asmparse.cpp"
    break;

  case 662: /* variantType: BLOB_  */
#line 1660 "asmparse.y"
                                                              { (yyval.int32) = VT_BLOB; }
#line 7564 "asmparse.cpp"
    break;

  case 663: /* variantType: STREAM_  */
#line 1661 "asmparse.y"
                                                              { (yyval.int32) = VT_STREAM; }
#line 7570 "asmparse.cpp"
    break;

  case 664: /* variantType: STORAGE_  */
#line 1662 "asmparse.y"
                                                              { (yyval.int32) = VT_STORAGE; }
#line 7576 "asmparse.cpp"
    break;

  case 665: /* variantType: STREAMED_OBJECT_  */
#line 1663 "asmparse.y"
                                                              { (yyval.int32) = VT_STREAMED_OBJECT; }
#line 7582 "asmparse.cpp"
    break;

  case 666: /* variantType: STORED_OBJECT_  */
#line 1664 "asmparse.y"
                                                              { (yyval.int32) = VT_STORED_OBJECT; }
#line 7588 "asmparse.cpp"
    break;

  case 667: /* variantType: BLOB_OBJECT_  */
#line 1665 "asmparse.y"
                                                              { (yyval.int32) = VT_BLOB_OBJECT; }
#line 7594 "asmparse.cpp"
    break;

  case 668: /* variantType: CF_  */
#line 1666 "asmparse.y"
                                                              { (yyval.int32) = VT_CF; }
#line 7600 "asmparse.cpp"
    break;

  case 669: /* variantType: CLSID_  */
#line 1667 "asmparse.y"
                                                              { (yyval.int32) = VT_CLSID; }
#line 7606 "asmparse.cpp"
    break;

  case 670: /* type: CLASS_ className  */
#line 1671 "asmparse.y"
                                                              { if((yyvsp[0].token) == PASM->m_tkSysString)
                                                                {     (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); }
                                                                else if((yyvsp[0].token) == PASM->m_tkSysObject)
                                                                {     (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_OBJECT); }
                                                                else
                                                                 (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CLASS, (yyvsp[0].token)); }
#line 7617 "asmparse.cpp"
    break;

  case 671: /* type: OBJECT_  */
#line 1677 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_OBJECT); }
#line 7623 "asmparse.cpp"
    break;

  case 672: /* type: VALUE_ CLASS_ className  */
#line 1678 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, (yyvsp[0].token)); }
#line 7629 "asmparse.cpp"
    break;

  case 673: /* type: VALUETYPE_ className  */
#line 1679 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, (yyvsp[0].token)); }
#line 7635 "asmparse.cpp"
    break;

  case 674: /* type: type '[' ']'  */
#line 1680 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 7641 "asmparse.cpp"
    break;

  case 675: /* type: type '[' bounds1 ']'  */
#line 1681 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeArray(ELEMENT_TYPE_ARRAY, (yyvsp[-3].binstr), (yyvsp[-1].binstr)); }
#line 7647 "asmparse.cpp"
    break;

  case 676: /* type: type '&'  */
#line 1682 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_BYREF); }
#line 7653 "asmparse.cpp"
    break;

  case 677: /* type: type '*'  */
#line 1683 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_PTR); }
#line 7659 "asmparse.cpp"
    break;

  case 678: /* type: type PINNED_  */
#line 1684 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_PINNED); }
#line 7665 "asmparse.cpp"
    break;

  case 679: /* type: type MODREQ_ '(' typeSpec ')'  */
#line 1685 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_REQD, (yyvsp[-1].token));
                                                                (yyval.binstr)->append((yyvsp[-4].binstr)); }
#line 7672 "asmparse.cpp"
    break;

  case 680: /* type: type MODOPT_ '(' typeSpec ')'  */
#line 1687 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_OPT, (yyvsp[-1].token));
                                                                (yyval.binstr)->append((yyvsp[-4].binstr)); }
#line 7679 "asmparse.cpp"
    break;

  case 681: /* type: methodSpec callConv type '*' '(' sigArgs0 ')'  */
#line 1690 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeSig((yyvsp[-5].int32), (yyvsp[-4].binstr), (yyvsp[-1].binstr));
                                                                (yyval.binstr)->insertInt8(ELEMENT_TYPE_FNPTR);
                                                                PASM->delArgNameList(PASM->m_firstArgName);
                                                                PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                                PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                              }
#line 7690 "asmparse.cpp"
    break;

  case 682: /* type: type '<' tyArgs1 '>'  */
#line 1696 "asmparse.y"
                                                              { if((yyvsp[-1].binstr) == NULL) (yyval.binstr) = (yyvsp[-3].binstr);
                                                                else {
                                                                  (yyval.binstr) = new BinStr();
                                                                  (yyval.binstr)->appendInt8(ELEMENT_TYPE_GENERICINST);
                                                                  (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                  corEmitInt((yyval.binstr), corCountArgs((yyvsp[-1].binstr)));
                                                                  (yyval.binstr)->append((yyvsp[-1].binstr)); delete (yyvsp[-3].binstr); delete (yyvsp[-1].binstr); }}
#line 7702 "asmparse.cpp"
    break;

  case 683: /* type: '!' '!' int32  */
#line 1703 "asmparse.y"
                                                              { //if(PASM->m_pCurMethod)  {
                                                                //  if(($3 < 0)||((DWORD)$3 >= PASM->m_pCurMethod->m_NumTyPars))
                                                                //    PASM->report->error("Invalid method type parameter '%d'\n",$3);
                                                                  (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_MVAR); corEmitInt((yyval.binstr), (yyvsp[0].int32));
                                                                //} else PASM->report->error("Method type parameter '%d' outside method scope\n",$3);
                                                              }
#line 7713 "asmparse.cpp"
    break;

  case 684: /* type: '!' int32  */
#line 1709 "asmparse.y"
                                                              { //if(PASM->m_pCurClass)  {
                                                                //  if(($2 < 0)||((DWORD)$2 >= PASM->m_pCurClass->m_NumTyPars))
                                                                //    PASM->report->error("Invalid type parameter '%d'\n",$2);
                                                                  (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_VAR); corEmitInt((yyval.binstr), (yyvsp[0].int32));
                                                                //} else PASM->report->error("Type parameter '%d' outside class scope\n",$2);
                                                              }
#line 7724 "asmparse.cpp"
    break;

  case 685: /* type: '!' '!' dottedName  */
#line 1715 "asmparse.y"
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
#line 7744 "asmparse.cpp"
    break;

  case 686: /* type: '!' dottedName  */
#line 1730 "asmparse.y"
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
#line 7764 "asmparse.cpp"
    break;

  case 687: /* type: TYPEDREF_  */
#line 1745 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_TYPEDBYREF); }
#line 7770 "asmparse.cpp"
    break;

  case 688: /* type: VOID_  */
#line 1746 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_VOID); }
#line 7776 "asmparse.cpp"
    break;

  case 689: /* type: NATIVE_ INT_  */
#line 1747 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I); }
#line 7782 "asmparse.cpp"
    break;

  case 690: /* type: NATIVE_ UNSIGNED_ INT_  */
#line 1748 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U); }
#line 7788 "asmparse.cpp"
    break;

  case 691: /* type: NATIVE_ UINT_  */
#line 1749 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U); }
#line 7794 "asmparse.cpp"
    break;

  case 692: /* type: simpleType  */
#line 1750 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7800 "asmparse.cpp"
    break;

  case 693: /* type: ELLIPSIS type  */
#line 1751 "asmparse.y"
                                                               { (yyval.binstr) = (yyvsp[0].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SENTINEL); }
#line 7806 "asmparse.cpp"
    break;

  case 694: /* simpleType: CHAR_  */
#line 1754 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CHAR); }
#line 7812 "asmparse.cpp"
    break;

  case 695: /* simpleType: STRING_  */
#line 1755 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); }
#line 7818 "asmparse.cpp"
    break;

  case 696: /* simpleType: BOOL_  */
#line 1756 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_BOOLEAN); }
#line 7824 "asmparse.cpp"
    break;

  case 697: /* simpleType: INT8_  */
#line 1757 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I1); }
#line 7830 "asmparse.cpp"
    break;

  case 698: /* simpleType: INT16_  */
#line 1758 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I2); }
#line 7836 "asmparse.cpp"
    break;

  case 699: /* simpleType: INT32_  */
#line 1759 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I4); }
#line 7842 "asmparse.cpp"
    break;

  case 700: /* simpleType: INT64_  */
#line 1760 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I8); }
#line 7848 "asmparse.cpp"
    break;

  case 701: /* simpleType: FLOAT32_  */
#line 1761 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4); }
#line 7854 "asmparse.cpp"
    break;

  case 702: /* simpleType: FLOAT64_  */
#line 1762 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8); }
#line 7860 "asmparse.cpp"
    break;

  case 703: /* simpleType: UNSIGNED_ INT8_  */
#line 1763 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1); }
#line 7866 "asmparse.cpp"
    break;

  case 704: /* simpleType: UNSIGNED_ INT16_  */
#line 1764 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2); }
#line 7872 "asmparse.cpp"
    break;

  case 705: /* simpleType: UNSIGNED_ INT32_  */
#line 1765 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4); }
#line 7878 "asmparse.cpp"
    break;

  case 706: /* simpleType: UNSIGNED_ INT64_  */
#line 1766 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8); }
#line 7884 "asmparse.cpp"
    break;

  case 707: /* simpleType: UINT8_  */
#line 1767 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1); }
#line 7890 "asmparse.cpp"
    break;

  case 708: /* simpleType: UINT16_  */
#line 1768 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2); }
#line 7896 "asmparse.cpp"
    break;

  case 709: /* simpleType: UINT32_  */
#line 1769 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4); }
#line 7902 "asmparse.cpp"
    break;

  case 710: /* simpleType: UINT64_  */
#line 1770 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8); }
#line 7908 "asmparse.cpp"
    break;

  case 711: /* simpleType: TYPEDEF_TS  */
#line 1771 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[0].tdd)->m_pbsTypeSpec); }
#line 7914 "asmparse.cpp"
    break;

  case 712: /* bounds1: bound  */
#line 1774 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7920 "asmparse.cpp"
    break;

  case 713: /* bounds1: bounds1 ',' bound  */
#line 1775 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyvsp[-2].binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 7926 "asmparse.cpp"
    break;

  case 714: /* bound: %empty  */
#line 1778 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0x7FFFFFFF); (yyval.binstr)->appendInt32(0x7FFFFFFF);  }
#line 7932 "asmparse.cpp"
    break;

  case 715: /* bound: ELLIPSIS  */
#line 1779 "asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0x7FFFFFFF); (yyval.binstr)->appendInt32(0x7FFFFFFF);  }
#line 7938 "asmparse.cpp"
    break;

  case 716: /* bound: int32  */
#line 1780 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0); (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 7944 "asmparse.cpp"
    break;

  case 717: /* bound: int32 ELLIPSIS int32  */
#line 1781 "asmparse.y"
                                                               { FAIL_UNLESS((yyvsp[-2].int32) <= (yyvsp[0].int32), ("lower bound %d must be <= upper bound %d\n", (yyvsp[-2].int32), (yyvsp[0].int32)));
                                                                if ((yyvsp[-2].int32) > (yyvsp[0].int32)) { YYERROR; };
                                                                (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[-2].int32)); (yyval.binstr)->appendInt32((yyvsp[0].int32)-(yyvsp[-2].int32)+1); }
#line 7952 "asmparse.cpp"
    break;

  case 718: /* bound: int32 ELLIPSIS  */
#line 1784 "asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[-1].int32)); (yyval.binstr)->appendInt32(0x7FFFFFFF); }
#line 7958 "asmparse.cpp"
    break;

  case 719: /* secDecl: _PERMISSION secAction typeSpec '(' nameValPairs ')'  */
#line 1789 "asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-4].secAct), (yyvsp[-3].token), (yyvsp[-1].pair)); }
#line 7964 "asmparse.cpp"
    break;

  case 720: /* secDecl: _PERMISSION secAction typeSpec '=' '{' customBlobDescr '}'  */
#line 1791 "asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-5].secAct), (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 7970 "asmparse.cpp"
    break;

  case 721: /* secDecl: _PERMISSION secAction typeSpec  */
#line 1792 "asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-1].secAct), (yyvsp[0].token), (NVPair *)NULL); }
#line 7976 "asmparse.cpp"
    break;

  case 722: /* secDecl: psetHead bytes ')'  */
#line 1793 "asmparse.y"
                                                              { PASM->AddPermissionSetDecl((yyvsp[-2].secAct), (yyvsp[-1].binstr)); }
#line 7982 "asmparse.cpp"
    break;

  case 723: /* secDecl: _PERMISSIONSET secAction compQstring  */
#line 1795 "asmparse.y"
                                                              { PASM->AddPermissionSetDecl((yyvsp[-1].secAct),BinStrToUnicode((yyvsp[0].binstr),true));}
#line 7988 "asmparse.cpp"
    break;

  case 724: /* secDecl: _PERMISSIONSET secAction '=' '{' secAttrSetBlob '}'  */
#line 1797 "asmparse.y"
                                                              { BinStr* ret = new BinStr();
                                                                ret->insertInt8('.');
                                                                corEmitInt(ret, nSecAttrBlobs);
                                                                ret->append((yyvsp[-1].binstr));
                                                                PASM->AddPermissionSetDecl((yyvsp[-4].secAct),ret);
                                                                nSecAttrBlobs = 0; }
#line 7999 "asmparse.cpp"
    break;

  case 725: /* secAttrSetBlob: %empty  */
#line 1805 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); nSecAttrBlobs = 0;}
#line 8005 "asmparse.cpp"
    break;

  case 726: /* secAttrSetBlob: secAttrBlob  */
#line 1806 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); nSecAttrBlobs = 1; }
#line 8011 "asmparse.cpp"
    break;

  case 727: /* secAttrSetBlob: secAttrBlob ',' secAttrSetBlob  */
#line 1807 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); nSecAttrBlobs++; }
#line 8017 "asmparse.cpp"
    break;

  case 728: /* secAttrBlob: typeSpec '=' '{' customBlobNVPairs '}'  */
#line 1811 "asmparse.y"
                                                              { (yyval.binstr) = PASM->EncodeSecAttr(PASM->ReflectionNotation((yyvsp[-4].token)),(yyvsp[-1].binstr),nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
#line 8024 "asmparse.cpp"
    break;

  case 729: /* secAttrBlob: CLASS_ SQSTRING '=' '{' customBlobNVPairs '}'  */
#line 1814 "asmparse.y"
                                                              { (yyval.binstr) = PASM->EncodeSecAttr((yyvsp[-4].string),(yyvsp[-1].binstr),nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
#line 8031 "asmparse.cpp"
    break;

  case 730: /* psetHead: _PERMISSIONSET secAction '=' '('  */
#line 1818 "asmparse.y"
                                                              { (yyval.secAct) = (yyvsp[-2].secAct); bParsingByteArray = TRUE; }
#line 8037 "asmparse.cpp"
    break;

  case 731: /* psetHead: _PERMISSIONSET secAction BYTEARRAY_ '('  */
#line 1820 "asmparse.y"
                                                              { (yyval.secAct) = (yyvsp[-2].secAct); bParsingByteArray = TRUE; }
#line 8043 "asmparse.cpp"
    break;

  case 732: /* nameValPairs: nameValPair  */
#line 1823 "asmparse.y"
                                                              { (yyval.pair) = (yyvsp[0].pair); }
#line 8049 "asmparse.cpp"
    break;

  case 733: /* nameValPairs: nameValPair ',' nameValPairs  */
#line 1824 "asmparse.y"
                                                              { (yyval.pair) = (yyvsp[-2].pair)->Concat((yyvsp[0].pair)); }
#line 8055 "asmparse.cpp"
    break;

  case 734: /* nameValPair: compQstring '=' caValue  */
#line 1827 "asmparse.y"
                                                              { (yyvsp[-2].binstr)->appendInt8(0); (yyval.pair) = new NVPair((yyvsp[-2].binstr), (yyvsp[0].binstr)); }
#line 8061 "asmparse.cpp"
    break;

  case 735: /* truefalse: TRUE_  */
#line 1830 "asmparse.y"
                                                              { (yyval.int32) = 1; }
#line 8067 "asmparse.cpp"
    break;

  case 736: /* truefalse: FALSE_  */
#line 1831 "asmparse.y"
                                                              { (yyval.int32) = 0; }
#line 8073 "asmparse.cpp"
    break;

  case 737: /* caValue: truefalse  */
#line 1834 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_BOOLEAN);
                                                                (yyval.binstr)->appendInt8((yyvsp[0].int32)); }
#line 8081 "asmparse.cpp"
    break;

  case 738: /* caValue: int32  */
#line 1837 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_I4);
                                                                (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 8089 "asmparse.cpp"
    break;

  case 739: /* caValue: INT32_ '(' int32 ')'  */
#line 1840 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_I4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8097 "asmparse.cpp"
    break;

  case 740: /* caValue: compQstring  */
#line 1843 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_STRING);
                                                                (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr);
                                                                (yyval.binstr)->appendInt8(0); }
#line 8106 "asmparse.cpp"
    break;

  case 741: /* caValue: className '(' INT8_ ':' int32 ')'  */
#line 1847 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(1);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8117 "asmparse.cpp"
    break;

  case 742: /* caValue: className '(' INT16_ ':' int32 ')'  */
#line 1853 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(2);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8128 "asmparse.cpp"
    break;

  case 743: /* caValue: className '(' INT32_ ':' int32 ')'  */
#line 1859 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8139 "asmparse.cpp"
    break;

  case 744: /* caValue: className '(' int32 ')'  */
#line 1865 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-3].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8150 "asmparse.cpp"
    break;

  case 745: /* secAction: REQUEST_  */
#line 1873 "asmparse.y"
                                                              { (yyval.secAct) = dclRequest; }
#line 8156 "asmparse.cpp"
    break;

  case 746: /* secAction: DEMAND_  */
#line 1874 "asmparse.y"
                                                              { (yyval.secAct) = dclDemand; }
#line 8162 "asmparse.cpp"
    break;

  case 747: /* secAction: ASSERT_  */
#line 1875 "asmparse.y"
                                                              { (yyval.secAct) = dclAssert; }
#line 8168 "asmparse.cpp"
    break;

  case 748: /* secAction: DENY_  */
#line 1876 "asmparse.y"
                                                              { (yyval.secAct) = dclDeny; }
#line 8174 "asmparse.cpp"
    break;

  case 749: /* secAction: PERMITONLY_  */
#line 1877 "asmparse.y"
                                                              { (yyval.secAct) = dclPermitOnly; }
#line 8180 "asmparse.cpp"
    break;

  case 750: /* secAction: LINKCHECK_  */
#line 1878 "asmparse.y"
                                                              { (yyval.secAct) = dclLinktimeCheck; }
#line 8186 "asmparse.cpp"
    break;

  case 751: /* secAction: INHERITCHECK_  */
#line 1879 "asmparse.y"
                                                              { (yyval.secAct) = dclInheritanceCheck; }
#line 8192 "asmparse.cpp"
    break;

  case 752: /* secAction: REQMIN_  */
#line 1880 "asmparse.y"
                                                              { (yyval.secAct) = dclRequestMinimum; }
#line 8198 "asmparse.cpp"
    break;

  case 753: /* secAction: REQOPT_  */
#line 1881 "asmparse.y"
                                                              { (yyval.secAct) = dclRequestOptional; }
#line 8204 "asmparse.cpp"
    break;

  case 754: /* secAction: REQREFUSE_  */
#line 1882 "asmparse.y"
                                                              { (yyval.secAct) = dclRequestRefuse; }
#line 8210 "asmparse.cpp"
    break;

  case 755: /* secAction: PREJITGRANT_  */
#line 1883 "asmparse.y"
                                                              { (yyval.secAct) = dclPrejitGrant; }
#line 8216 "asmparse.cpp"
    break;

  case 756: /* secAction: PREJITDENY_  */
#line 1884 "asmparse.y"
                                                              { (yyval.secAct) = dclPrejitDenied; }
#line 8222 "asmparse.cpp"
    break;

  case 757: /* secAction: NONCASDEMAND_  */
#line 1885 "asmparse.y"
                                                              { (yyval.secAct) = dclNonCasDemand; }
#line 8228 "asmparse.cpp"
    break;

  case 758: /* secAction: NONCASLINKDEMAND_  */
#line 1886 "asmparse.y"
                                                              { (yyval.secAct) = dclNonCasLinkDemand; }
#line 8234 "asmparse.cpp"
    break;

  case 759: /* secAction: NONCASINHERITANCE_  */
#line 1887 "asmparse.y"
                                                              { (yyval.secAct) = dclNonCasInheritance; }
#line 8240 "asmparse.cpp"
    break;

  case 760: /* esHead: _LINE  */
#line 1891 "asmparse.y"
                                                              { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = FALSE; }
#line 8246 "asmparse.cpp"
    break;

  case 761: /* esHead: P_LINE  */
#line 1892 "asmparse.y"
                                                              { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = TRUE; }
#line 8252 "asmparse.cpp"
    break;

  case 762: /* extSourceSpec: esHead int32 SQSTRING  */
#line 1895 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-1].int32);
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8260 "asmparse.cpp"
    break;

  case 763: /* extSourceSpec: esHead int32  */
#line 1898 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[0].int32);
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1); }
#line 8267 "asmparse.cpp"
    break;

  case 764: /* extSourceSpec: esHead int32 ':' int32 SQSTRING  */
#line 1900 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-3].int32);
                                                                PENV->nExtCol=(yyvsp[-1].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8275 "asmparse.cpp"
    break;

  case 765: /* extSourceSpec: esHead int32 ':' int32  */
#line 1903 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-2].int32);
                                                                PENV->nExtCol=(yyvsp[0].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);}
#line 8282 "asmparse.cpp"
    break;

  case 766: /* extSourceSpec: esHead int32 ':' int32 ',' int32 SQSTRING  */
#line 1906 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-5].int32);
                                                                PENV->nExtCol=(yyvsp[-3].int32); PENV->nExtColEnd = (yyvsp[-1].int32);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8290 "asmparse.cpp"
    break;

  case 767: /* extSourceSpec: esHead int32 ':' int32 ',' int32  */
#line 1910 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-4].int32);
                                                                PENV->nExtCol=(yyvsp[-2].int32); PENV->nExtColEnd = (yyvsp[0].int32); }
#line 8297 "asmparse.cpp"
    break;

  case 768: /* extSourceSpec: esHead int32 ',' int32 ':' int32 SQSTRING  */
#line 1913 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-5].int32); PENV->nExtLineEnd = (yyvsp[-3].int32);
                                                                PENV->nExtCol=(yyvsp[-1].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8305 "asmparse.cpp"
    break;

  case 769: /* extSourceSpec: esHead int32 ',' int32 ':' int32  */
#line 1917 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-4].int32); PENV->nExtLineEnd = (yyvsp[-2].int32);
                                                                PENV->nExtCol=(yyvsp[0].int32); PENV->nExtColEnd = static_cast<unsigned>(-1); }
#line 8312 "asmparse.cpp"
    break;

  case 770: /* extSourceSpec: esHead int32 ',' int32 ':' int32 ',' int32 SQSTRING  */
#line 1920 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-7].int32); PENV->nExtLineEnd = (yyvsp[-5].int32);
                                                                PENV->nExtCol=(yyvsp[-3].int32); PENV->nExtColEnd = (yyvsp[-1].int32);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8320 "asmparse.cpp"
    break;

  case 771: /* extSourceSpec: esHead int32 ',' int32 ':' int32 ',' int32  */
#line 1924 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-6].int32); PENV->nExtLineEnd = (yyvsp[-4].int32);
                                                                PENV->nExtCol=(yyvsp[-2].int32); PENV->nExtColEnd = (yyvsp[0].int32); }
#line 8327 "asmparse.cpp"
    break;

  case 772: /* extSourceSpec: esHead int32 QSTRING  */
#line 1926 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-1].int32) - 1;
                                                                PENV->nExtCol = 0; PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].binstr));}
#line 8335 "asmparse.cpp"
    break;

  case 773: /* fileDecl: _FILE fileAttr dottedName fileEntry hashHead bytes ')' fileEntry  */
#line 1933 "asmparse.y"
                                                              { PASMM->AddFile((yyvsp[-5].string), (yyvsp[-6].fileAttr)|(yyvsp[-4].fileAttr)|(yyvsp[0].fileAttr), (yyvsp[-2].binstr)); }
#line 8341 "asmparse.cpp"
    break;

  case 774: /* fileDecl: _FILE fileAttr dottedName fileEntry  */
#line 1934 "asmparse.y"
                                                              { PASMM->AddFile((yyvsp[-1].string), (yyvsp[-2].fileAttr)|(yyvsp[0].fileAttr), NULL); }
#line 8347 "asmparse.cpp"
    break;

  case 775: /* fileAttr: %empty  */
#line 1937 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0; }
#line 8353 "asmparse.cpp"
    break;

  case 776: /* fileAttr: fileAttr NOMETADATA_  */
#line 1938 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) ((yyvsp[-1].fileAttr) | ffContainsNoMetaData); }
#line 8359 "asmparse.cpp"
    break;

  case 777: /* fileEntry: %empty  */
#line 1941 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0; }
#line 8365 "asmparse.cpp"
    break;

  case 778: /* fileEntry: _ENTRYPOINT  */
#line 1942 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0x80000000; }
#line 8371 "asmparse.cpp"
    break;

  case 779: /* hashHead: _HASH '=' '('  */
#line 1945 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8377 "asmparse.cpp"
    break;

  case 780: /* assemblyHead: _ASSEMBLY asmAttr dottedName  */
#line 1948 "asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[0].string), NULL, (DWORD)(yyvsp[-1].asmAttr), FALSE); }
#line 8383 "asmparse.cpp"
    break;

  case 781: /* asmAttr: %empty  */
#line 1951 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) 0; }
#line 8389 "asmparse.cpp"
    break;

  case 782: /* asmAttr: asmAttr RETARGETABLE_  */
#line 1952 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afRetargetable); }
#line 8395 "asmparse.cpp"
    break;

  case 783: /* asmAttr: asmAttr WINDOWSRUNTIME_  */
#line 1953 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afContentType_WindowsRuntime); }
#line 8401 "asmparse.cpp"
    break;

  case 784: /* asmAttr: asmAttr NOPLATFORM_  */
#line 1954 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afPA_NoPlatform); }
#line 8407 "asmparse.cpp"
    break;

  case 785: /* asmAttr: asmAttr LEGACY_ LIBRARY_  */
#line 1955 "asmparse.y"
                                                              { (yyval.asmAttr) = (yyvsp[-2].asmAttr); }
#line 8413 "asmparse.cpp"
    break;

  case 786: /* asmAttr: asmAttr CIL_  */
#line 1956 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_MSIL); }
#line 8419 "asmparse.cpp"
    break;

  case 787: /* asmAttr: asmAttr X86_  */
#line 1957 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_x86); }
#line 8425 "asmparse.cpp"
    break;

  case 788: /* asmAttr: asmAttr AMD64_  */
#line 1958 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_AMD64); }
#line 8431 "asmparse.cpp"
    break;

  case 789: /* asmAttr: asmAttr ARM_  */
#line 1959 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_ARM); }
#line 8437 "asmparse.cpp"
    break;

  case 790: /* asmAttr: asmAttr ARM64_  */
#line 1960 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_ARM64); }
#line 8443 "asmparse.cpp"
    break;

  case 793: /* assemblyDecl: _HASH ALGORITHM_ int32  */
#line 1967 "asmparse.y"
                                                              { PASMM->SetAssemblyHashAlg((yyvsp[0].int32)); }
#line 8449 "asmparse.cpp"
    break;

  case 796: /* intOrWildcard: int32  */
#line 1972 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 8455 "asmparse.cpp"
    break;

  case 797: /* intOrWildcard: '*'  */
#line 1973 "asmparse.y"
                                                              { (yyval.int32) = 0xFFFF; }
#line 8461 "asmparse.cpp"
    break;

  case 798: /* asmOrRefDecl: publicKeyHead bytes ')'  */
#line 1976 "asmparse.y"
                                                              { PASMM->SetAssemblyPublicKey((yyvsp[-1].binstr)); }
#line 8467 "asmparse.cpp"
    break;

  case 799: /* asmOrRefDecl: _VER intOrWildcard ':' intOrWildcard ':' intOrWildcard ':' intOrWildcard  */
#line 1978 "asmparse.y"
                                                              { PASMM->SetAssemblyVer((USHORT)(yyvsp[-6].int32), (USHORT)(yyvsp[-4].int32), (USHORT)(yyvsp[-2].int32), (USHORT)(yyvsp[0].int32)); }
#line 8473 "asmparse.cpp"
    break;

  case 800: /* asmOrRefDecl: _LOCALE compQstring  */
#line 1979 "asmparse.y"
                                                              { (yyvsp[0].binstr)->appendInt8(0); PASMM->SetAssemblyLocale((yyvsp[0].binstr),TRUE); }
#line 8479 "asmparse.cpp"
    break;

  case 801: /* asmOrRefDecl: localeHead bytes ')'  */
#line 1980 "asmparse.y"
                                                              { PASMM->SetAssemblyLocale((yyvsp[-1].binstr),FALSE); }
#line 8485 "asmparse.cpp"
    break;

  case 804: /* publicKeyHead: _PUBLICKEY '=' '('  */
#line 1985 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8491 "asmparse.cpp"
    break;

  case 805: /* publicKeyTokenHead: _PUBLICKEYTOKEN '=' '('  */
#line 1988 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8497 "asmparse.cpp"
    break;

  case 806: /* localeHead: _LOCALE '=' '('  */
#line 1991 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8503 "asmparse.cpp"
    break;

  case 807: /* assemblyRefHead: _ASSEMBLY EXTERN_ asmAttr dottedName  */
#line 1995 "asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[0].string), NULL, (yyvsp[-1].asmAttr), TRUE); }
#line 8509 "asmparse.cpp"
    break;

  case 808: /* assemblyRefHead: _ASSEMBLY EXTERN_ asmAttr dottedName AS_ dottedName  */
#line 1997 "asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[-2].string), (yyvsp[0].string), (yyvsp[-3].asmAttr), TRUE); }
#line 8515 "asmparse.cpp"
    break;

  case 811: /* assemblyRefDecl: hashHead bytes ')'  */
#line 2004 "asmparse.y"
                                                              { PASMM->SetAssemblyHashBlob((yyvsp[-1].binstr)); }
#line 8521 "asmparse.cpp"
    break;

  case 813: /* assemblyRefDecl: publicKeyTokenHead bytes ')'  */
#line 2006 "asmparse.y"
                                                              { PASMM->SetAssemblyPublicKeyToken((yyvsp[-1].binstr)); }
#line 8527 "asmparse.cpp"
    break;

  case 814: /* assemblyRefDecl: AUTO_  */
#line 2007 "asmparse.y"
                                                              { PASMM->SetAssemblyAutodetect(); }
#line 8533 "asmparse.cpp"
    break;

  case 815: /* exptypeHead: _CLASS EXTERN_ exptAttr dottedName  */
#line 2010 "asmparse.y"
                                                              { PASMM->StartComType((yyvsp[0].string), (yyvsp[-1].exptAttr));}
#line 8539 "asmparse.cpp"
    break;

  case 816: /* exportHead: _EXPORT exptAttr dottedName  */
#line 2013 "asmparse.y"
                                                                              { PASMM->StartComType((yyvsp[0].string), (yyvsp[-1].exptAttr)); }
#line 8545 "asmparse.cpp"
    break;

  case 817: /* exptAttr: %empty  */
#line 2016 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) 0; }
#line 8551 "asmparse.cpp"
    break;

  case 818: /* exptAttr: exptAttr PRIVATE_  */
#line 2017 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdNotPublic); }
#line 8557 "asmparse.cpp"
    break;

  case 819: /* exptAttr: exptAttr PUBLIC_  */
#line 2018 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdPublic); }
#line 8563 "asmparse.cpp"
    break;

  case 820: /* exptAttr: exptAttr FORWARDER_  */
#line 2019 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdForwarder); }
#line 8569 "asmparse.cpp"
    break;

  case 821: /* exptAttr: exptAttr NESTED_ PUBLIC_  */
#line 2020 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedPublic); }
#line 8575 "asmparse.cpp"
    break;

  case 822: /* exptAttr: exptAttr NESTED_ PRIVATE_  */
#line 2021 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedPrivate); }
#line 8581 "asmparse.cpp"
    break;

  case 823: /* exptAttr: exptAttr NESTED_ FAMILY_  */
#line 2022 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamily); }
#line 8587 "asmparse.cpp"
    break;

  case 824: /* exptAttr: exptAttr NESTED_ ASSEMBLY_  */
#line 2023 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedAssembly); }
#line 8593 "asmparse.cpp"
    break;

  case 825: /* exptAttr: exptAttr NESTED_ FAMANDASSEM_  */
#line 2024 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamANDAssem); }
#line 8599 "asmparse.cpp"
    break;

  case 826: /* exptAttr: exptAttr NESTED_ FAMORASSEM_  */
#line 2025 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamORAssem); }
#line 8605 "asmparse.cpp"
    break;

  case 829: /* exptypeDecl: _FILE dottedName  */
#line 2032 "asmparse.y"
                                                              { PASMM->SetComTypeFile((yyvsp[0].string)); }
#line 8611 "asmparse.cpp"
    break;

  case 830: /* exptypeDecl: _CLASS EXTERN_ slashedName  */
#line 2033 "asmparse.y"
                                                               { PASMM->SetComTypeComType((yyvsp[0].string)); }
#line 8617 "asmparse.cpp"
    break;

  case 831: /* exptypeDecl: _ASSEMBLY EXTERN_ dottedName  */
#line 2034 "asmparse.y"
                                                              { PASMM->SetComTypeAsmRef((yyvsp[0].string)); }
#line 8623 "asmparse.cpp"
    break;

  case 832: /* exptypeDecl: MDTOKEN_ '(' int32 ')'  */
#line 2035 "asmparse.y"
                                                              { if(!PASMM->SetComTypeImplementationTok((yyvsp[-1].int32)))
                                                                  PASM->report->error("Invalid implementation of exported type\n"); }
#line 8630 "asmparse.cpp"
    break;

  case 833: /* exptypeDecl: _CLASS int32  */
#line 2037 "asmparse.y"
                                                              { if(!PASMM->SetComTypeClassTok((yyvsp[0].int32)))
                                                                  PASM->report->error("Invalid TypeDefID of exported type\n"); }
#line 8637 "asmparse.cpp"
    break;

  case 836: /* manifestResHead: _MRESOURCE manresAttr dottedName  */
#line 2043 "asmparse.y"
                                                              { PASMM->StartManifestRes((yyvsp[0].string), (yyvsp[0].string), (yyvsp[-1].manresAttr)); }
#line 8643 "asmparse.cpp"
    break;

  case 837: /* manifestResHead: _MRESOURCE manresAttr dottedName AS_ dottedName  */
#line 2045 "asmparse.y"
                                                              { PASMM->StartManifestRes((yyvsp[-2].string), (yyvsp[0].string), (yyvsp[-3].manresAttr)); }
#line 8649 "asmparse.cpp"
    break;

  case 838: /* manresAttr: %empty  */
#line 2048 "asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) 0; }
#line 8655 "asmparse.cpp"
    break;

  case 839: /* manresAttr: manresAttr PUBLIC_  */
#line 2049 "asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) ((yyvsp[-1].manresAttr) | mrPublic); }
#line 8661 "asmparse.cpp"
    break;

  case 840: /* manresAttr: manresAttr PRIVATE_  */
#line 2050 "asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) ((yyvsp[-1].manresAttr) | mrPrivate); }
#line 8667 "asmparse.cpp"
    break;

  case 843: /* manifestResDecl: _FILE dottedName AT_ int32  */
#line 2057 "asmparse.y"
                                                              { PASMM->SetManifestResFile((yyvsp[-2].string), (ULONG)(yyvsp[0].int32)); }
#line 8673 "asmparse.cpp"
    break;

  case 844: /* manifestResDecl: _ASSEMBLY EXTERN_ dottedName  */
#line 2058 "asmparse.y"
                                                              { PASMM->SetManifestResAsmRef((yyvsp[0].string)); }
#line 8679 "asmparse.cpp"
    break;


#line 8683 "asmparse.cpp"

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

#line 2063 "asmparse.y"


#include "grammar_after.cpp"
