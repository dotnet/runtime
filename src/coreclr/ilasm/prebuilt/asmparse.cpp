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
#line 1 ".\\src\\coreclr\\ilasm\\asmparse.y"


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// File asmparse.y
//
#include "ilasmpch.h"

#include "grammar_before.cpp"


#line 85 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"

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
    EXTENDED_ = 342,               /* EXTENDED_  */
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
    ASYNC_ = 355,                  /* ASYNC_  */
    STRICT_ = 356,                 /* STRICT_  */
    RETARGETABLE_ = 357,           /* RETARGETABLE_  */
    WINDOWSRUNTIME_ = 358,         /* WINDOWSRUNTIME_  */
    NOPLATFORM_ = 359,             /* NOPLATFORM_  */
    METHOD_ = 360,                 /* METHOD_  */
    FIELD_ = 361,                  /* FIELD_  */
    PINNED_ = 362,                 /* PINNED_  */
    MODREQ_ = 363,                 /* MODREQ_  */
    MODOPT_ = 364,                 /* MODOPT_  */
    SERIALIZABLE_ = 365,           /* SERIALIZABLE_  */
    PROPERTY_ = 366,               /* PROPERTY_  */
    TYPE_ = 367,                   /* TYPE_  */
    ASSEMBLY_ = 368,               /* ASSEMBLY_  */
    FAMANDASSEM_ = 369,            /* FAMANDASSEM_  */
    FAMORASSEM_ = 370,             /* FAMORASSEM_  */
    PRIVATESCOPE_ = 371,           /* PRIVATESCOPE_  */
    HIDEBYSIG_ = 372,              /* HIDEBYSIG_  */
    NEWSLOT_ = 373,                /* NEWSLOT_  */
    RTSPECIALNAME_ = 374,          /* RTSPECIALNAME_  */
    PINVOKEIMPL_ = 375,            /* PINVOKEIMPL_  */
    _CTOR = 376,                   /* _CTOR  */
    _CCTOR = 377,                  /* _CCTOR  */
    LITERAL_ = 378,                /* LITERAL_  */
    NOTSERIALIZED_ = 379,          /* NOTSERIALIZED_  */
    INITONLY_ = 380,               /* INITONLY_  */
    REQSECOBJ_ = 381,              /* REQSECOBJ_  */
    CIL_ = 382,                    /* CIL_  */
    OPTIL_ = 383,                  /* OPTIL_  */
    MANAGED_ = 384,                /* MANAGED_  */
    FORWARDREF_ = 385,             /* FORWARDREF_  */
    PRESERVESIG_ = 386,            /* PRESERVESIG_  */
    RUNTIME_ = 387,                /* RUNTIME_  */
    INTERNALCALL_ = 388,           /* INTERNALCALL_  */
    _IMPORT = 389,                 /* _IMPORT  */
    NOMANGLE_ = 390,               /* NOMANGLE_  */
    LASTERR_ = 391,                /* LASTERR_  */
    WINAPI_ = 392,                 /* WINAPI_  */
    AS_ = 393,                     /* AS_  */
    BESTFIT_ = 394,                /* BESTFIT_  */
    ON_ = 395,                     /* ON_  */
    OFF_ = 396,                    /* OFF_  */
    CHARMAPERROR_ = 397,           /* CHARMAPERROR_  */
    INSTR_NONE = 398,              /* INSTR_NONE  */
    INSTR_VAR = 399,               /* INSTR_VAR  */
    INSTR_I = 400,                 /* INSTR_I  */
    INSTR_I8 = 401,                /* INSTR_I8  */
    INSTR_R = 402,                 /* INSTR_R  */
    INSTR_BRTARGET = 403,          /* INSTR_BRTARGET  */
    INSTR_METHOD = 404,            /* INSTR_METHOD  */
    INSTR_FIELD = 405,             /* INSTR_FIELD  */
    INSTR_TYPE = 406,              /* INSTR_TYPE  */
    INSTR_STRING = 407,            /* INSTR_STRING  */
    INSTR_SIG = 408,               /* INSTR_SIG  */
    INSTR_TOK = 409,               /* INSTR_TOK  */
    INSTR_SWITCH = 410,            /* INSTR_SWITCH  */
    _CLASS = 411,                  /* _CLASS  */
    _NAMESPACE = 412,              /* _NAMESPACE  */
    _METHOD = 413,                 /* _METHOD  */
    _FIELD = 414,                  /* _FIELD  */
    _DATA = 415,                   /* _DATA  */
    _THIS = 416,                   /* _THIS  */
    _BASE = 417,                   /* _BASE  */
    _NESTER = 418,                 /* _NESTER  */
    _EMITBYTE = 419,               /* _EMITBYTE  */
    _TRY = 420,                    /* _TRY  */
    _MAXSTACK = 421,               /* _MAXSTACK  */
    _LOCALS = 422,                 /* _LOCALS  */
    _ENTRYPOINT = 423,             /* _ENTRYPOINT  */
    _ZEROINIT = 424,               /* _ZEROINIT  */
    _EVENT = 425,                  /* _EVENT  */
    _ADDON = 426,                  /* _ADDON  */
    _REMOVEON = 427,               /* _REMOVEON  */
    _FIRE = 428,                   /* _FIRE  */
    _OTHER = 429,                  /* _OTHER  */
    _PROPERTY = 430,               /* _PROPERTY  */
    _SET = 431,                    /* _SET  */
    _GET = 432,                    /* _GET  */
    _PERMISSION = 433,             /* _PERMISSION  */
    _PERMISSIONSET = 434,          /* _PERMISSIONSET  */
    REQUEST_ = 435,                /* REQUEST_  */
    DEMAND_ = 436,                 /* DEMAND_  */
    ASSERT_ = 437,                 /* ASSERT_  */
    DENY_ = 438,                   /* DENY_  */
    PERMITONLY_ = 439,             /* PERMITONLY_  */
    LINKCHECK_ = 440,              /* LINKCHECK_  */
    INHERITCHECK_ = 441,           /* INHERITCHECK_  */
    REQMIN_ = 442,                 /* REQMIN_  */
    REQOPT_ = 443,                 /* REQOPT_  */
    REQREFUSE_ = 444,              /* REQREFUSE_  */
    PREJITGRANT_ = 445,            /* PREJITGRANT_  */
    PREJITDENY_ = 446,             /* PREJITDENY_  */
    NONCASDEMAND_ = 447,           /* NONCASDEMAND_  */
    NONCASLINKDEMAND_ = 448,       /* NONCASLINKDEMAND_  */
    NONCASINHERITANCE_ = 449,      /* NONCASINHERITANCE_  */
    _LINE = 450,                   /* _LINE  */
    P_LINE = 451,                  /* P_LINE  */
    _LANGUAGE = 452,               /* _LANGUAGE  */
    _CUSTOM = 453,                 /* _CUSTOM  */
    INIT_ = 454,                   /* INIT_  */
    _SIZE = 455,                   /* _SIZE  */
    _PACK = 456,                   /* _PACK  */
    _VTABLE = 457,                 /* _VTABLE  */
    _VTFIXUP = 458,                /* _VTFIXUP  */
    FROMUNMANAGED_ = 459,          /* FROMUNMANAGED_  */
    CALLMOSTDERIVED_ = 460,        /* CALLMOSTDERIVED_  */
    _VTENTRY = 461,                /* _VTENTRY  */
    RETAINAPPDOMAIN_ = 462,        /* RETAINAPPDOMAIN_  */
    _FILE = 463,                   /* _FILE  */
    NOMETADATA_ = 464,             /* NOMETADATA_  */
    _HASH = 465,                   /* _HASH  */
    _ASSEMBLY = 466,               /* _ASSEMBLY  */
    _PUBLICKEY = 467,              /* _PUBLICKEY  */
    _PUBLICKEYTOKEN = 468,         /* _PUBLICKEYTOKEN  */
    ALGORITHM_ = 469,              /* ALGORITHM_  */
    _VER = 470,                    /* _VER  */
    _LOCALE = 471,                 /* _LOCALE  */
    EXTERN_ = 472,                 /* EXTERN_  */
    _MRESOURCE = 473,              /* _MRESOURCE  */
    _MODULE = 474,                 /* _MODULE  */
    _EXPORT = 475,                 /* _EXPORT  */
    LEGACY_ = 476,                 /* LEGACY_  */
    LIBRARY_ = 477,                /* LIBRARY_  */
    X86_ = 478,                    /* X86_  */
    AMD64_ = 479,                  /* AMD64_  */
    ARM_ = 480,                    /* ARM_  */
    ARM64_ = 481,                  /* ARM64_  */
    MARSHAL_ = 482,                /* MARSHAL_  */
    CUSTOM_ = 483,                 /* CUSTOM_  */
    SYSSTRING_ = 484,              /* SYSSTRING_  */
    FIXED_ = 485,                  /* FIXED_  */
    VARIANT_ = 486,                /* VARIANT_  */
    CURRENCY_ = 487,               /* CURRENCY_  */
    SYSCHAR_ = 488,                /* SYSCHAR_  */
    DECIMAL_ = 489,                /* DECIMAL_  */
    DATE_ = 490,                   /* DATE_  */
    BSTR_ = 491,                   /* BSTR_  */
    TBSTR_ = 492,                  /* TBSTR_  */
    LPSTR_ = 493,                  /* LPSTR_  */
    LPWSTR_ = 494,                 /* LPWSTR_  */
    LPTSTR_ = 495,                 /* LPTSTR_  */
    OBJECTREF_ = 496,              /* OBJECTREF_  */
    IUNKNOWN_ = 497,               /* IUNKNOWN_  */
    IDISPATCH_ = 498,              /* IDISPATCH_  */
    STRUCT_ = 499,                 /* STRUCT_  */
    SAFEARRAY_ = 500,              /* SAFEARRAY_  */
    BYVALSTR_ = 501,               /* BYVALSTR_  */
    LPVOID_ = 502,                 /* LPVOID_  */
    ANY_ = 503,                    /* ANY_  */
    ARRAY_ = 504,                  /* ARRAY_  */
    LPSTRUCT_ = 505,               /* LPSTRUCT_  */
    IIDPARAM_ = 506,               /* IIDPARAM_  */
    IN_ = 507,                     /* IN_  */
    OUT_ = 508,                    /* OUT_  */
    OPT_ = 509,                    /* OPT_  */
    _PARAM = 510,                  /* _PARAM  */
    _OVERRIDE = 511,               /* _OVERRIDE  */
    WITH_ = 512,                   /* WITH_  */
    NULL_ = 513,                   /* NULL_  */
    ERROR_ = 514,                  /* ERROR_  */
    HRESULT_ = 515,                /* HRESULT_  */
    CARRAY_ = 516,                 /* CARRAY_  */
    USERDEFINED_ = 517,            /* USERDEFINED_  */
    RECORD_ = 518,                 /* RECORD_  */
    FILETIME_ = 519,               /* FILETIME_  */
    BLOB_ = 520,                   /* BLOB_  */
    STREAM_ = 521,                 /* STREAM_  */
    STORAGE_ = 522,                /* STORAGE_  */
    STREAMED_OBJECT_ = 523,        /* STREAMED_OBJECT_  */
    STORED_OBJECT_ = 524,          /* STORED_OBJECT_  */
    BLOB_OBJECT_ = 525,            /* BLOB_OBJECT_  */
    CF_ = 526,                     /* CF_  */
    CLSID_ = 527,                  /* CLSID_  */
    VECTOR_ = 528,                 /* VECTOR_  */
    _SUBSYSTEM = 529,              /* _SUBSYSTEM  */
    _CORFLAGS = 530,               /* _CORFLAGS  */
    ALIGNMENT_ = 531,              /* ALIGNMENT_  */
    _IMAGEBASE = 532,              /* _IMAGEBASE  */
    _STACKRESERVE = 533,           /* _STACKRESERVE  */
    _TYPEDEF = 534,                /* _TYPEDEF  */
    _TEMPLATE = 535,               /* _TEMPLATE  */
    _TYPELIST = 536,               /* _TYPELIST  */
    _MSCORLIB = 537,               /* _MSCORLIB  */
    P_DEFINE = 538,                /* P_DEFINE  */
    P_UNDEF = 539,                 /* P_UNDEF  */
    P_IFDEF = 540,                 /* P_IFDEF  */
    P_IFNDEF = 541,                /* P_IFNDEF  */
    P_ELSE = 542,                  /* P_ELSE  */
    P_ENDIF = 543,                 /* P_ENDIF  */
    P_INCLUDE = 544,               /* P_INCLUDE  */
    CONSTRAINT_ = 545              /* CONSTRAINT_  */
  };
  typedef enum yytokentype yytoken_kind_t;
#endif

/* Value type.  */
#if ! defined YYSTYPE && ! defined YYSTYPE_IS_DECLARED
union YYSTYPE
{
#line 15 ".\\src\\coreclr\\ilasm\\asmparse.y"

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

#line 451 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"

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
  YYSYMBOL_EXTENDED_ = 87,                 /* EXTENDED_  */
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
  YYSYMBOL_ASYNC_ = 100,                   /* ASYNC_  */
  YYSYMBOL_STRICT_ = 101,                  /* STRICT_  */
  YYSYMBOL_RETARGETABLE_ = 102,            /* RETARGETABLE_  */
  YYSYMBOL_WINDOWSRUNTIME_ = 103,          /* WINDOWSRUNTIME_  */
  YYSYMBOL_NOPLATFORM_ = 104,              /* NOPLATFORM_  */
  YYSYMBOL_METHOD_ = 105,                  /* METHOD_  */
  YYSYMBOL_FIELD_ = 106,                   /* FIELD_  */
  YYSYMBOL_PINNED_ = 107,                  /* PINNED_  */
  YYSYMBOL_MODREQ_ = 108,                  /* MODREQ_  */
  YYSYMBOL_MODOPT_ = 109,                  /* MODOPT_  */
  YYSYMBOL_SERIALIZABLE_ = 110,            /* SERIALIZABLE_  */
  YYSYMBOL_PROPERTY_ = 111,                /* PROPERTY_  */
  YYSYMBOL_TYPE_ = 112,                    /* TYPE_  */
  YYSYMBOL_ASSEMBLY_ = 113,                /* ASSEMBLY_  */
  YYSYMBOL_FAMANDASSEM_ = 114,             /* FAMANDASSEM_  */
  YYSYMBOL_FAMORASSEM_ = 115,              /* FAMORASSEM_  */
  YYSYMBOL_PRIVATESCOPE_ = 116,            /* PRIVATESCOPE_  */
  YYSYMBOL_HIDEBYSIG_ = 117,               /* HIDEBYSIG_  */
  YYSYMBOL_NEWSLOT_ = 118,                 /* NEWSLOT_  */
  YYSYMBOL_RTSPECIALNAME_ = 119,           /* RTSPECIALNAME_  */
  YYSYMBOL_PINVOKEIMPL_ = 120,             /* PINVOKEIMPL_  */
  YYSYMBOL__CTOR = 121,                    /* _CTOR  */
  YYSYMBOL__CCTOR = 122,                   /* _CCTOR  */
  YYSYMBOL_LITERAL_ = 123,                 /* LITERAL_  */
  YYSYMBOL_NOTSERIALIZED_ = 124,           /* NOTSERIALIZED_  */
  YYSYMBOL_INITONLY_ = 125,                /* INITONLY_  */
  YYSYMBOL_REQSECOBJ_ = 126,               /* REQSECOBJ_  */
  YYSYMBOL_CIL_ = 127,                     /* CIL_  */
  YYSYMBOL_OPTIL_ = 128,                   /* OPTIL_  */
  YYSYMBOL_MANAGED_ = 129,                 /* MANAGED_  */
  YYSYMBOL_FORWARDREF_ = 130,              /* FORWARDREF_  */
  YYSYMBOL_PRESERVESIG_ = 131,             /* PRESERVESIG_  */
  YYSYMBOL_RUNTIME_ = 132,                 /* RUNTIME_  */
  YYSYMBOL_INTERNALCALL_ = 133,            /* INTERNALCALL_  */
  YYSYMBOL__IMPORT = 134,                  /* _IMPORT  */
  YYSYMBOL_NOMANGLE_ = 135,                /* NOMANGLE_  */
  YYSYMBOL_LASTERR_ = 136,                 /* LASTERR_  */
  YYSYMBOL_WINAPI_ = 137,                  /* WINAPI_  */
  YYSYMBOL_AS_ = 138,                      /* AS_  */
  YYSYMBOL_BESTFIT_ = 139,                 /* BESTFIT_  */
  YYSYMBOL_ON_ = 140,                      /* ON_  */
  YYSYMBOL_OFF_ = 141,                     /* OFF_  */
  YYSYMBOL_CHARMAPERROR_ = 142,            /* CHARMAPERROR_  */
  YYSYMBOL_INSTR_NONE = 143,               /* INSTR_NONE  */
  YYSYMBOL_INSTR_VAR = 144,                /* INSTR_VAR  */
  YYSYMBOL_INSTR_I = 145,                  /* INSTR_I  */
  YYSYMBOL_INSTR_I8 = 146,                 /* INSTR_I8  */
  YYSYMBOL_INSTR_R = 147,                  /* INSTR_R  */
  YYSYMBOL_INSTR_BRTARGET = 148,           /* INSTR_BRTARGET  */
  YYSYMBOL_INSTR_METHOD = 149,             /* INSTR_METHOD  */
  YYSYMBOL_INSTR_FIELD = 150,              /* INSTR_FIELD  */
  YYSYMBOL_INSTR_TYPE = 151,               /* INSTR_TYPE  */
  YYSYMBOL_INSTR_STRING = 152,             /* INSTR_STRING  */
  YYSYMBOL_INSTR_SIG = 153,                /* INSTR_SIG  */
  YYSYMBOL_INSTR_TOK = 154,                /* INSTR_TOK  */
  YYSYMBOL_INSTR_SWITCH = 155,             /* INSTR_SWITCH  */
  YYSYMBOL__CLASS = 156,                   /* _CLASS  */
  YYSYMBOL__NAMESPACE = 157,               /* _NAMESPACE  */
  YYSYMBOL__METHOD = 158,                  /* _METHOD  */
  YYSYMBOL__FIELD = 159,                   /* _FIELD  */
  YYSYMBOL__DATA = 160,                    /* _DATA  */
  YYSYMBOL__THIS = 161,                    /* _THIS  */
  YYSYMBOL__BASE = 162,                    /* _BASE  */
  YYSYMBOL__NESTER = 163,                  /* _NESTER  */
  YYSYMBOL__EMITBYTE = 164,                /* _EMITBYTE  */
  YYSYMBOL__TRY = 165,                     /* _TRY  */
  YYSYMBOL__MAXSTACK = 166,                /* _MAXSTACK  */
  YYSYMBOL__LOCALS = 167,                  /* _LOCALS  */
  YYSYMBOL__ENTRYPOINT = 168,              /* _ENTRYPOINT  */
  YYSYMBOL__ZEROINIT = 169,                /* _ZEROINIT  */
  YYSYMBOL__EVENT = 170,                   /* _EVENT  */
  YYSYMBOL__ADDON = 171,                   /* _ADDON  */
  YYSYMBOL__REMOVEON = 172,                /* _REMOVEON  */
  YYSYMBOL__FIRE = 173,                    /* _FIRE  */
  YYSYMBOL__OTHER = 174,                   /* _OTHER  */
  YYSYMBOL__PROPERTY = 175,                /* _PROPERTY  */
  YYSYMBOL__SET = 176,                     /* _SET  */
  YYSYMBOL__GET = 177,                     /* _GET  */
  YYSYMBOL__PERMISSION = 178,              /* _PERMISSION  */
  YYSYMBOL__PERMISSIONSET = 179,           /* _PERMISSIONSET  */
  YYSYMBOL_REQUEST_ = 180,                 /* REQUEST_  */
  YYSYMBOL_DEMAND_ = 181,                  /* DEMAND_  */
  YYSYMBOL_ASSERT_ = 182,                  /* ASSERT_  */
  YYSYMBOL_DENY_ = 183,                    /* DENY_  */
  YYSYMBOL_PERMITONLY_ = 184,              /* PERMITONLY_  */
  YYSYMBOL_LINKCHECK_ = 185,               /* LINKCHECK_  */
  YYSYMBOL_INHERITCHECK_ = 186,            /* INHERITCHECK_  */
  YYSYMBOL_REQMIN_ = 187,                  /* REQMIN_  */
  YYSYMBOL_REQOPT_ = 188,                  /* REQOPT_  */
  YYSYMBOL_REQREFUSE_ = 189,               /* REQREFUSE_  */
  YYSYMBOL_PREJITGRANT_ = 190,             /* PREJITGRANT_  */
  YYSYMBOL_PREJITDENY_ = 191,              /* PREJITDENY_  */
  YYSYMBOL_NONCASDEMAND_ = 192,            /* NONCASDEMAND_  */
  YYSYMBOL_NONCASLINKDEMAND_ = 193,        /* NONCASLINKDEMAND_  */
  YYSYMBOL_NONCASINHERITANCE_ = 194,       /* NONCASINHERITANCE_  */
  YYSYMBOL__LINE = 195,                    /* _LINE  */
  YYSYMBOL_P_LINE = 196,                   /* P_LINE  */
  YYSYMBOL__LANGUAGE = 197,                /* _LANGUAGE  */
  YYSYMBOL__CUSTOM = 198,                  /* _CUSTOM  */
  YYSYMBOL_INIT_ = 199,                    /* INIT_  */
  YYSYMBOL__SIZE = 200,                    /* _SIZE  */
  YYSYMBOL__PACK = 201,                    /* _PACK  */
  YYSYMBOL__VTABLE = 202,                  /* _VTABLE  */
  YYSYMBOL__VTFIXUP = 203,                 /* _VTFIXUP  */
  YYSYMBOL_FROMUNMANAGED_ = 204,           /* FROMUNMANAGED_  */
  YYSYMBOL_CALLMOSTDERIVED_ = 205,         /* CALLMOSTDERIVED_  */
  YYSYMBOL__VTENTRY = 206,                 /* _VTENTRY  */
  YYSYMBOL_RETAINAPPDOMAIN_ = 207,         /* RETAINAPPDOMAIN_  */
  YYSYMBOL__FILE = 208,                    /* _FILE  */
  YYSYMBOL_NOMETADATA_ = 209,              /* NOMETADATA_  */
  YYSYMBOL__HASH = 210,                    /* _HASH  */
  YYSYMBOL__ASSEMBLY = 211,                /* _ASSEMBLY  */
  YYSYMBOL__PUBLICKEY = 212,               /* _PUBLICKEY  */
  YYSYMBOL__PUBLICKEYTOKEN = 213,          /* _PUBLICKEYTOKEN  */
  YYSYMBOL_ALGORITHM_ = 214,               /* ALGORITHM_  */
  YYSYMBOL__VER = 215,                     /* _VER  */
  YYSYMBOL__LOCALE = 216,                  /* _LOCALE  */
  YYSYMBOL_EXTERN_ = 217,                  /* EXTERN_  */
  YYSYMBOL__MRESOURCE = 218,               /* _MRESOURCE  */
  YYSYMBOL__MODULE = 219,                  /* _MODULE  */
  YYSYMBOL__EXPORT = 220,                  /* _EXPORT  */
  YYSYMBOL_LEGACY_ = 221,                  /* LEGACY_  */
  YYSYMBOL_LIBRARY_ = 222,                 /* LIBRARY_  */
  YYSYMBOL_X86_ = 223,                     /* X86_  */
  YYSYMBOL_AMD64_ = 224,                   /* AMD64_  */
  YYSYMBOL_ARM_ = 225,                     /* ARM_  */
  YYSYMBOL_ARM64_ = 226,                   /* ARM64_  */
  YYSYMBOL_MARSHAL_ = 227,                 /* MARSHAL_  */
  YYSYMBOL_CUSTOM_ = 228,                  /* CUSTOM_  */
  YYSYMBOL_SYSSTRING_ = 229,               /* SYSSTRING_  */
  YYSYMBOL_FIXED_ = 230,                   /* FIXED_  */
  YYSYMBOL_VARIANT_ = 231,                 /* VARIANT_  */
  YYSYMBOL_CURRENCY_ = 232,                /* CURRENCY_  */
  YYSYMBOL_SYSCHAR_ = 233,                 /* SYSCHAR_  */
  YYSYMBOL_DECIMAL_ = 234,                 /* DECIMAL_  */
  YYSYMBOL_DATE_ = 235,                    /* DATE_  */
  YYSYMBOL_BSTR_ = 236,                    /* BSTR_  */
  YYSYMBOL_TBSTR_ = 237,                   /* TBSTR_  */
  YYSYMBOL_LPSTR_ = 238,                   /* LPSTR_  */
  YYSYMBOL_LPWSTR_ = 239,                  /* LPWSTR_  */
  YYSYMBOL_LPTSTR_ = 240,                  /* LPTSTR_  */
  YYSYMBOL_OBJECTREF_ = 241,               /* OBJECTREF_  */
  YYSYMBOL_IUNKNOWN_ = 242,                /* IUNKNOWN_  */
  YYSYMBOL_IDISPATCH_ = 243,               /* IDISPATCH_  */
  YYSYMBOL_STRUCT_ = 244,                  /* STRUCT_  */
  YYSYMBOL_SAFEARRAY_ = 245,               /* SAFEARRAY_  */
  YYSYMBOL_BYVALSTR_ = 246,                /* BYVALSTR_  */
  YYSYMBOL_LPVOID_ = 247,                  /* LPVOID_  */
  YYSYMBOL_ANY_ = 248,                     /* ANY_  */
  YYSYMBOL_ARRAY_ = 249,                   /* ARRAY_  */
  YYSYMBOL_LPSTRUCT_ = 250,                /* LPSTRUCT_  */
  YYSYMBOL_IIDPARAM_ = 251,                /* IIDPARAM_  */
  YYSYMBOL_IN_ = 252,                      /* IN_  */
  YYSYMBOL_OUT_ = 253,                     /* OUT_  */
  YYSYMBOL_OPT_ = 254,                     /* OPT_  */
  YYSYMBOL__PARAM = 255,                   /* _PARAM  */
  YYSYMBOL__OVERRIDE = 256,                /* _OVERRIDE  */
  YYSYMBOL_WITH_ = 257,                    /* WITH_  */
  YYSYMBOL_NULL_ = 258,                    /* NULL_  */
  YYSYMBOL_ERROR_ = 259,                   /* ERROR_  */
  YYSYMBOL_HRESULT_ = 260,                 /* HRESULT_  */
  YYSYMBOL_CARRAY_ = 261,                  /* CARRAY_  */
  YYSYMBOL_USERDEFINED_ = 262,             /* USERDEFINED_  */
  YYSYMBOL_RECORD_ = 263,                  /* RECORD_  */
  YYSYMBOL_FILETIME_ = 264,                /* FILETIME_  */
  YYSYMBOL_BLOB_ = 265,                    /* BLOB_  */
  YYSYMBOL_STREAM_ = 266,                  /* STREAM_  */
  YYSYMBOL_STORAGE_ = 267,                 /* STORAGE_  */
  YYSYMBOL_STREAMED_OBJECT_ = 268,         /* STREAMED_OBJECT_  */
  YYSYMBOL_STORED_OBJECT_ = 269,           /* STORED_OBJECT_  */
  YYSYMBOL_BLOB_OBJECT_ = 270,             /* BLOB_OBJECT_  */
  YYSYMBOL_CF_ = 271,                      /* CF_  */
  YYSYMBOL_CLSID_ = 272,                   /* CLSID_  */
  YYSYMBOL_VECTOR_ = 273,                  /* VECTOR_  */
  YYSYMBOL__SUBSYSTEM = 274,               /* _SUBSYSTEM  */
  YYSYMBOL__CORFLAGS = 275,                /* _CORFLAGS  */
  YYSYMBOL_ALIGNMENT_ = 276,               /* ALIGNMENT_  */
  YYSYMBOL__IMAGEBASE = 277,               /* _IMAGEBASE  */
  YYSYMBOL__STACKRESERVE = 278,            /* _STACKRESERVE  */
  YYSYMBOL__TYPEDEF = 279,                 /* _TYPEDEF  */
  YYSYMBOL__TEMPLATE = 280,                /* _TEMPLATE  */
  YYSYMBOL__TYPELIST = 281,                /* _TYPELIST  */
  YYSYMBOL__MSCORLIB = 282,                /* _MSCORLIB  */
  YYSYMBOL_P_DEFINE = 283,                 /* P_DEFINE  */
  YYSYMBOL_P_UNDEF = 284,                  /* P_UNDEF  */
  YYSYMBOL_P_IFDEF = 285,                  /* P_IFDEF  */
  YYSYMBOL_P_IFNDEF = 286,                 /* P_IFNDEF  */
  YYSYMBOL_P_ELSE = 287,                   /* P_ELSE  */
  YYSYMBOL_P_ENDIF = 288,                  /* P_ENDIF  */
  YYSYMBOL_P_INCLUDE = 289,                /* P_INCLUDE  */
  YYSYMBOL_CONSTRAINT_ = 290,              /* CONSTRAINT_  */
  YYSYMBOL_291_ = 291,                     /* '{'  */
  YYSYMBOL_292_ = 292,                     /* '}'  */
  YYSYMBOL_293_ = 293,                     /* '+'  */
  YYSYMBOL_294_ = 294,                     /* ','  */
  YYSYMBOL_295_ = 295,                     /* '.'  */
  YYSYMBOL_296_ = 296,                     /* '('  */
  YYSYMBOL_297_ = 297,                     /* ')'  */
  YYSYMBOL_298_ = 298,                     /* ';'  */
  YYSYMBOL_299_ = 299,                     /* '='  */
  YYSYMBOL_300_ = 300,                     /* '['  */
  YYSYMBOL_301_ = 301,                     /* ']'  */
  YYSYMBOL_302_ = 302,                     /* '<'  */
  YYSYMBOL_303_ = 303,                     /* '>'  */
  YYSYMBOL_304_ = 304,                     /* '-'  */
  YYSYMBOL_305_ = 305,                     /* ':'  */
  YYSYMBOL_306_ = 306,                     /* '*'  */
  YYSYMBOL_307_ = 307,                     /* '&'  */
  YYSYMBOL_308_ = 308,                     /* '/'  */
  YYSYMBOL_309_ = 309,                     /* '!'  */
  YYSYMBOL_YYACCEPT = 310,                 /* $accept  */
  YYSYMBOL_decls = 311,                    /* decls  */
  YYSYMBOL_decl = 312,                     /* decl  */
  YYSYMBOL_classNameSeq = 313,             /* classNameSeq  */
  YYSYMBOL_compQstring = 314,              /* compQstring  */
  YYSYMBOL_languageDecl = 315,             /* languageDecl  */
  YYSYMBOL_id = 316,                       /* id  */
  YYSYMBOL_dottedName = 317,               /* dottedName  */
  YYSYMBOL_int32 = 318,                    /* int32  */
  YYSYMBOL_int64 = 319,                    /* int64  */
  YYSYMBOL_float64 = 320,                  /* float64  */
  YYSYMBOL_typedefDecl = 321,              /* typedefDecl  */
  YYSYMBOL_compControl = 322,              /* compControl  */
  YYSYMBOL_customDescr = 323,              /* customDescr  */
  YYSYMBOL_customDescrWithOwner = 324,     /* customDescrWithOwner  */
  YYSYMBOL_customHead = 325,               /* customHead  */
  YYSYMBOL_customHeadWithOwner = 326,      /* customHeadWithOwner  */
  YYSYMBOL_customType = 327,               /* customType  */
  YYSYMBOL_ownerType = 328,                /* ownerType  */
  YYSYMBOL_customBlobDescr = 329,          /* customBlobDescr  */
  YYSYMBOL_customBlobArgs = 330,           /* customBlobArgs  */
  YYSYMBOL_customBlobNVPairs = 331,        /* customBlobNVPairs  */
  YYSYMBOL_fieldOrProp = 332,              /* fieldOrProp  */
  YYSYMBOL_customAttrDecl = 333,           /* customAttrDecl  */
  YYSYMBOL_serializType = 334,             /* serializType  */
  YYSYMBOL_moduleHead = 335,               /* moduleHead  */
  YYSYMBOL_vtfixupDecl = 336,              /* vtfixupDecl  */
  YYSYMBOL_vtfixupAttr = 337,              /* vtfixupAttr  */
  YYSYMBOL_vtableDecl = 338,               /* vtableDecl  */
  YYSYMBOL_vtableHead = 339,               /* vtableHead  */
  YYSYMBOL_nameSpaceHead = 340,            /* nameSpaceHead  */
  YYSYMBOL__class = 341,                   /* _class  */
  YYSYMBOL_classHeadBegin = 342,           /* classHeadBegin  */
  YYSYMBOL_classHead = 343,                /* classHead  */
  YYSYMBOL_classAttr = 344,                /* classAttr  */
  YYSYMBOL_extendsClause = 345,            /* extendsClause  */
  YYSYMBOL_implClause = 346,               /* implClause  */
  YYSYMBOL_classDecls = 347,               /* classDecls  */
  YYSYMBOL_implList = 348,                 /* implList  */
  YYSYMBOL_typeList = 349,                 /* typeList  */
  YYSYMBOL_typeListNotEmpty = 350,         /* typeListNotEmpty  */
  YYSYMBOL_typarsClause = 351,             /* typarsClause  */
  YYSYMBOL_typarAttrib = 352,              /* typarAttrib  */
  YYSYMBOL_typarAttribs = 353,             /* typarAttribs  */
  YYSYMBOL_typars = 354,                   /* typars  */
  YYSYMBOL_typarsRest = 355,               /* typarsRest  */
  YYSYMBOL_tyBound = 356,                  /* tyBound  */
  YYSYMBOL_genArity = 357,                 /* genArity  */
  YYSYMBOL_genArityNotEmpty = 358,         /* genArityNotEmpty  */
  YYSYMBOL_classDecl = 359,                /* classDecl  */
  YYSYMBOL_fieldDecl = 360,                /* fieldDecl  */
  YYSYMBOL_fieldAttr = 361,                /* fieldAttr  */
  YYSYMBOL_atOpt = 362,                    /* atOpt  */
  YYSYMBOL_initOpt = 363,                  /* initOpt  */
  YYSYMBOL_repeatOpt = 364,                /* repeatOpt  */
  YYSYMBOL_methodRef = 365,                /* methodRef  */
  YYSYMBOL_callConv = 366,                 /* callConv  */
  YYSYMBOL_callKind = 367,                 /* callKind  */
  YYSYMBOL_mdtoken = 368,                  /* mdtoken  */
  YYSYMBOL_memberRef = 369,                /* memberRef  */
  YYSYMBOL_eventHead = 370,                /* eventHead  */
  YYSYMBOL_eventAttr = 371,                /* eventAttr  */
  YYSYMBOL_eventDecls = 372,               /* eventDecls  */
  YYSYMBOL_eventDecl = 373,                /* eventDecl  */
  YYSYMBOL_propHead = 374,                 /* propHead  */
  YYSYMBOL_propAttr = 375,                 /* propAttr  */
  YYSYMBOL_propDecls = 376,                /* propDecls  */
  YYSYMBOL_propDecl = 377,                 /* propDecl  */
  YYSYMBOL_methodHeadPart1 = 378,          /* methodHeadPart1  */
  YYSYMBOL_marshalClause = 379,            /* marshalClause  */
  YYSYMBOL_marshalBlob = 380,              /* marshalBlob  */
  YYSYMBOL_marshalBlobHead = 381,          /* marshalBlobHead  */
  YYSYMBOL_methodHead = 382,               /* methodHead  */
  YYSYMBOL_methAttr = 383,                 /* methAttr  */
  YYSYMBOL_pinvAttr = 384,                 /* pinvAttr  */
  YYSYMBOL_methodName = 385,               /* methodName  */
  YYSYMBOL_paramAttr = 386,                /* paramAttr  */
  YYSYMBOL_implAttr = 387,                 /* implAttr  */
  YYSYMBOL_localsHead = 388,               /* localsHead  */
  YYSYMBOL_methodDecls = 389,              /* methodDecls  */
  YYSYMBOL_methodDecl = 390,               /* methodDecl  */
  YYSYMBOL_scopeBlock = 391,               /* scopeBlock  */
  YYSYMBOL_scopeOpen = 392,                /* scopeOpen  */
  YYSYMBOL_sehBlock = 393,                 /* sehBlock  */
  YYSYMBOL_sehClauses = 394,               /* sehClauses  */
  YYSYMBOL_tryBlock = 395,                 /* tryBlock  */
  YYSYMBOL_tryHead = 396,                  /* tryHead  */
  YYSYMBOL_sehClause = 397,                /* sehClause  */
  YYSYMBOL_filterClause = 398,             /* filterClause  */
  YYSYMBOL_filterHead = 399,               /* filterHead  */
  YYSYMBOL_catchClause = 400,              /* catchClause  */
  YYSYMBOL_finallyClause = 401,            /* finallyClause  */
  YYSYMBOL_faultClause = 402,              /* faultClause  */
  YYSYMBOL_handlerBlock = 403,             /* handlerBlock  */
  YYSYMBOL_dataDecl = 404,                 /* dataDecl  */
  YYSYMBOL_ddHead = 405,                   /* ddHead  */
  YYSYMBOL_tls = 406,                      /* tls  */
  YYSYMBOL_ddBody = 407,                   /* ddBody  */
  YYSYMBOL_ddItemList = 408,               /* ddItemList  */
  YYSYMBOL_ddItemCount = 409,              /* ddItemCount  */
  YYSYMBOL_ddItem = 410,                   /* ddItem  */
  YYSYMBOL_fieldSerInit = 411,             /* fieldSerInit  */
  YYSYMBOL_bytearrayhead = 412,            /* bytearrayhead  */
  YYSYMBOL_bytes = 413,                    /* bytes  */
  YYSYMBOL_hexbytes = 414,                 /* hexbytes  */
  YYSYMBOL_fieldInit = 415,                /* fieldInit  */
  YYSYMBOL_serInit = 416,                  /* serInit  */
  YYSYMBOL_f32seq = 417,                   /* f32seq  */
  YYSYMBOL_f64seq = 418,                   /* f64seq  */
  YYSYMBOL_i64seq = 419,                   /* i64seq  */
  YYSYMBOL_i32seq = 420,                   /* i32seq  */
  YYSYMBOL_i16seq = 421,                   /* i16seq  */
  YYSYMBOL_i8seq = 422,                    /* i8seq  */
  YYSYMBOL_boolSeq = 423,                  /* boolSeq  */
  YYSYMBOL_sqstringSeq = 424,              /* sqstringSeq  */
  YYSYMBOL_classSeq = 425,                 /* classSeq  */
  YYSYMBOL_objSeq = 426,                   /* objSeq  */
  YYSYMBOL_methodSpec = 427,               /* methodSpec  */
  YYSYMBOL_instr_none = 428,               /* instr_none  */
  YYSYMBOL_instr_var = 429,                /* instr_var  */
  YYSYMBOL_instr_i = 430,                  /* instr_i  */
  YYSYMBOL_instr_i8 = 431,                 /* instr_i8  */
  YYSYMBOL_instr_r = 432,                  /* instr_r  */
  YYSYMBOL_instr_brtarget = 433,           /* instr_brtarget  */
  YYSYMBOL_instr_method = 434,             /* instr_method  */
  YYSYMBOL_instr_field = 435,              /* instr_field  */
  YYSYMBOL_instr_type = 436,               /* instr_type  */
  YYSYMBOL_instr_string = 437,             /* instr_string  */
  YYSYMBOL_instr_sig = 438,                /* instr_sig  */
  YYSYMBOL_instr_tok = 439,                /* instr_tok  */
  YYSYMBOL_instr_switch = 440,             /* instr_switch  */
  YYSYMBOL_instr_r_head = 441,             /* instr_r_head  */
  YYSYMBOL_instr = 442,                    /* instr  */
  YYSYMBOL_labels = 443,                   /* labels  */
  YYSYMBOL_tyArgs0 = 444,                  /* tyArgs0  */
  YYSYMBOL_tyArgs1 = 445,                  /* tyArgs1  */
  YYSYMBOL_tyArgs2 = 446,                  /* tyArgs2  */
  YYSYMBOL_sigArgs0 = 447,                 /* sigArgs0  */
  YYSYMBOL_sigArgs1 = 448,                 /* sigArgs1  */
  YYSYMBOL_sigArg = 449,                   /* sigArg  */
  YYSYMBOL_className = 450,                /* className  */
  YYSYMBOL_slashedName = 451,              /* slashedName  */
  YYSYMBOL_typeSpec = 452,                 /* typeSpec  */
  YYSYMBOL_nativeType = 453,               /* nativeType  */
  YYSYMBOL_iidParamIndex = 454,            /* iidParamIndex  */
  YYSYMBOL_variantType = 455,              /* variantType  */
  YYSYMBOL_type = 456,                     /* type  */
  YYSYMBOL_simpleType = 457,               /* simpleType  */
  YYSYMBOL_bounds1 = 458,                  /* bounds1  */
  YYSYMBOL_bound = 459,                    /* bound  */
  YYSYMBOL_secDecl = 460,                  /* secDecl  */
  YYSYMBOL_secAttrSetBlob = 461,           /* secAttrSetBlob  */
  YYSYMBOL_secAttrBlob = 462,              /* secAttrBlob  */
  YYSYMBOL_psetHead = 463,                 /* psetHead  */
  YYSYMBOL_nameValPairs = 464,             /* nameValPairs  */
  YYSYMBOL_nameValPair = 465,              /* nameValPair  */
  YYSYMBOL_truefalse = 466,                /* truefalse  */
  YYSYMBOL_caValue = 467,                  /* caValue  */
  YYSYMBOL_secAction = 468,                /* secAction  */
  YYSYMBOL_esHead = 469,                   /* esHead  */
  YYSYMBOL_extSourceSpec = 470,            /* extSourceSpec  */
  YYSYMBOL_fileDecl = 471,                 /* fileDecl  */
  YYSYMBOL_fileAttr = 472,                 /* fileAttr  */
  YYSYMBOL_fileEntry = 473,                /* fileEntry  */
  YYSYMBOL_hashHead = 474,                 /* hashHead  */
  YYSYMBOL_assemblyHead = 475,             /* assemblyHead  */
  YYSYMBOL_asmAttr = 476,                  /* asmAttr  */
  YYSYMBOL_assemblyDecls = 477,            /* assemblyDecls  */
  YYSYMBOL_assemblyDecl = 478,             /* assemblyDecl  */
  YYSYMBOL_intOrWildcard = 479,            /* intOrWildcard  */
  YYSYMBOL_asmOrRefDecl = 480,             /* asmOrRefDecl  */
  YYSYMBOL_publicKeyHead = 481,            /* publicKeyHead  */
  YYSYMBOL_publicKeyTokenHead = 482,       /* publicKeyTokenHead  */
  YYSYMBOL_localeHead = 483,               /* localeHead  */
  YYSYMBOL_assemblyRefHead = 484,          /* assemblyRefHead  */
  YYSYMBOL_assemblyRefDecls = 485,         /* assemblyRefDecls  */
  YYSYMBOL_assemblyRefDecl = 486,          /* assemblyRefDecl  */
  YYSYMBOL_exptypeHead = 487,              /* exptypeHead  */
  YYSYMBOL_exportHead = 488,               /* exportHead  */
  YYSYMBOL_exptAttr = 489,                 /* exptAttr  */
  YYSYMBOL_exptypeDecls = 490,             /* exptypeDecls  */
  YYSYMBOL_exptypeDecl = 491,              /* exptypeDecl  */
  YYSYMBOL_manifestResHead = 492,          /* manifestResHead  */
  YYSYMBOL_manresAttr = 493,               /* manresAttr  */
  YYSYMBOL_manifestResDecls = 494,         /* manifestResDecls  */
  YYSYMBOL_manifestResDecl = 495           /* manifestResDecl  */
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
#define YYLAST   5609

/* YYNTOKENS -- Number of terminals.  */
#define YYNTOKENS  310
/* YYNNTS -- Number of nonterminals.  */
#define YYNNTS  186
/* YYNRULES -- Number of rules.  */
#define YYNRULES  864
/* YYNSTATES -- Number of states.  */
#define YYNSTATES  1609

/* YYMAXUTOK -- Last valid token kind.  */
#define YYMAXUTOK   545


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
       2,     2,     2,   309,     2,     2,     2,     2,   307,     2,
     296,   297,   306,   293,   294,   304,   295,   308,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,   305,   298,
     302,   299,   303,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,   300,     2,   301,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,   291,     2,   292,     2,     2,     2,     2,
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
     285,   286,   287,   288,   289,   290
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
     260,   261,   262,   263,   264,   267,   268,   269,   272,   275,
     276,   279,   280,   281,   285,   286,   287,   288,   289,   294,
     295,   296,   297,   300,   303,   304,   308,   309,   313,   314,
     315,   316,   319,   320,   321,   323,   326,   329,   335,   338,
     339,   343,   349,   350,   352,   355,   356,   362,   365,   366,
     369,   373,   374,   382,   383,   384,   385,   387,   389,   394,
     395,   396,   403,   407,   408,   409,   410,   411,   412,   415,
     418,   422,   425,   428,   434,   437,   438,   439,   440,   441,
     442,   443,   444,   445,   446,   447,   448,   449,   450,   451,
     452,   453,   454,   455,   456,   457,   458,   459,   460,   461,
     462,   463,   464,   467,   468,   471,   472,   475,   476,   479,
     480,   484,   485,   488,   489,   492,   493,   496,   497,   498,
     499,   500,   501,   502,   505,   506,   509,   510,   513,   514,
     517,   520,   521,   524,   528,   532,   533,   534,   535,   536,
     537,   538,   539,   540,   541,   542,   543,   549,   558,   559,
     560,   565,   571,   572,   573,   580,   585,   586,   587,   588,
     589,   590,   591,   592,   604,   606,   607,   608,   609,   610,
     611,   612,   615,   616,   619,   620,   623,   624,   628,   645,
     651,   667,   672,   673,   674,   677,   678,   679,   680,   683,
     684,   685,   686,   687,   688,   689,   690,   693,   696,   701,
     705,   709,   711,   713,   718,   719,   723,   724,   725,   728,
     729,   732,   733,   734,   735,   736,   737,   738,   739,   743,
     749,   750,   751,   754,   755,   759,   760,   761,   762,   763,
     764,   765,   769,   775,   776,   779,   780,   783,   786,   802,
     803,   804,   805,   806,   807,   808,   809,   810,   811,   812,
     813,   814,   815,   816,   817,   818,   819,   820,   821,   822,
     825,   828,   833,   834,   835,   836,   837,   838,   839,   840,
     841,   842,   843,   844,   845,   846,   847,   848,   851,   852,
     853,   856,   857,   858,   859,   860,   863,   864,   865,   866,
     867,   868,   869,   870,   871,   872,   873,   874,   875,   876,
     877,   878,   879,   882,   886,   887,   890,   891,   892,   893,
     895,   898,   899,   900,   901,   902,   903,   904,   905,   906,
     907,   908,   918,   928,   930,   933,   940,   941,   946,   952,
     953,   955,   976,   979,   983,   986,   987,   990,   991,   992,
     996,  1001,  1002,  1003,  1004,  1008,  1009,  1011,  1015,  1019,
    1024,  1028,  1032,  1033,  1034,  1039,  1042,  1043,  1046,  1047,
    1048,  1051,  1052,  1055,  1056,  1059,  1060,  1065,  1066,  1067,
    1068,  1075,  1082,  1089,  1096,  1104,  1112,  1113,  1114,  1115,
    1116,  1117,  1121,  1124,  1126,  1128,  1130,  1132,  1134,  1136,
    1138,  1140,  1142,  1144,  1146,  1148,  1150,  1152,  1154,  1156,
    1158,  1162,  1165,  1166,  1169,  1170,  1174,  1175,  1176,  1181,
    1182,  1183,  1185,  1187,  1189,  1190,  1191,  1195,  1199,  1203,
    1207,  1211,  1215,  1219,  1223,  1227,  1231,  1235,  1239,  1243,
    1247,  1251,  1255,  1259,  1263,  1270,  1271,  1273,  1277,  1278,
    1280,  1284,  1285,  1289,  1290,  1293,  1294,  1297,  1298,  1301,
    1302,  1306,  1307,  1308,  1312,  1313,  1314,  1316,  1320,  1321,
    1325,  1331,  1334,  1337,  1340,  1343,  1346,  1349,  1357,  1360,
    1363,  1366,  1369,  1372,  1375,  1379,  1380,  1381,  1382,  1383,
    1384,  1385,  1386,  1395,  1396,  1397,  1404,  1412,  1420,  1426,
    1432,  1438,  1442,  1443,  1445,  1447,  1451,  1457,  1460,  1461,
    1462,  1463,  1464,  1468,  1469,  1472,  1473,  1476,  1477,  1481,
    1482,  1485,  1486,  1489,  1490,  1491,  1495,  1496,  1497,  1498,
    1499,  1500,  1501,  1502,  1505,  1511,  1518,  1519,  1522,  1523,
    1524,  1525,  1529,  1530,  1537,  1543,  1545,  1548,  1550,  1551,
    1553,  1555,  1556,  1557,  1558,  1559,  1560,  1561,  1562,  1563,
    1564,  1565,  1566,  1567,  1568,  1569,  1570,  1571,  1573,  1575,
    1580,  1585,  1588,  1590,  1592,  1593,  1594,  1595,  1596,  1598,
    1600,  1602,  1603,  1605,  1608,  1612,  1613,  1614,  1615,  1617,
    1618,  1619,  1620,  1621,  1622,  1623,  1624,  1627,  1628,  1631,
    1632,  1633,  1634,  1635,  1636,  1637,  1638,  1639,  1640,  1641,
    1642,  1643,  1644,  1645,  1646,  1647,  1648,  1649,  1650,  1651,
    1652,  1653,  1654,  1655,  1656,  1657,  1658,  1659,  1660,  1661,
    1662,  1663,  1664,  1665,  1666,  1667,  1668,  1669,  1670,  1671,
    1672,  1673,  1674,  1675,  1676,  1677,  1678,  1679,  1683,  1689,
    1690,  1691,  1692,  1693,  1694,  1695,  1696,  1697,  1699,  1701,
    1708,  1715,  1721,  1727,  1742,  1757,  1758,  1759,  1760,  1761,
    1762,  1763,  1766,  1767,  1768,  1769,  1770,  1771,  1772,  1773,
    1774,  1775,  1776,  1777,  1778,  1779,  1780,  1781,  1782,  1783,
    1786,  1787,  1790,  1791,  1792,  1793,  1796,  1800,  1802,  1804,
    1805,  1806,  1808,  1817,  1818,  1819,  1822,  1825,  1830,  1831,
    1835,  1836,  1839,  1842,  1843,  1846,  1849,  1852,  1855,  1859,
    1865,  1871,  1877,  1885,  1886,  1887,  1888,  1889,  1890,  1891,
    1892,  1893,  1894,  1895,  1896,  1897,  1898,  1899,  1903,  1904,
    1907,  1910,  1912,  1915,  1917,  1921,  1924,  1928,  1931,  1935,
    1938,  1944,  1946,  1949,  1950,  1953,  1954,  1957,  1960,  1963,
    1964,  1965,  1966,  1967,  1968,  1969,  1970,  1971,  1972,  1975,
    1976,  1979,  1980,  1981,  1984,  1985,  1988,  1989,  1991,  1992,
    1993,  1994,  1997,  2000,  2003,  2006,  2008,  2012,  2013,  2016,
    2017,  2018,  2019,  2022,  2025,  2028,  2029,  2030,  2031,  2032,
    2033,  2034,  2035,  2036,  2037,  2040,  2041,  2044,  2045,  2046,
    2047,  2049,  2051,  2052,  2055,  2056,  2060,  2061,  2062,  2065,
    2066,  2069,  2070,  2071,  2072
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
  "EXTENDED_", "ANSI_", "UNICODE_", "AUTOCHAR_", "IMPORT_", "ENUM_",
  "VIRTUAL_", "NOINLINING_", "AGGRESSIVEINLINING_", "NOOPTIMIZATION_",
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

#define YYPACT_NINF (-1347)

#define yypact_value_is_default(Yyn) \
  ((Yyn) == YYPACT_NINF)

#define YYTABLE_NINF (-577)

#define yytable_value_is_error(Yyn) \
  0

/* YYPACT[STATE-NUM] -- Index in YYTABLE of the portion describing
   STATE-NUM.  */
static const yytype_int16 yypact[] =
{
   -1347,  2869, -1347, -1347,   -98,  5309, -1347,  -165,    87,  3866,
    3866, -1347, -1347,   136,   418,  -143,  -135,   -97,    -2, -1347,
    4897,   185,   185,   361,   361,  1847,   -68, -1347,  5309,  5309,
    5309,  5309, -1347, -1347,   223, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347,   226,   226, -1347, -1347, -1347, -1347,   226,   -50,
   -1347,   196,     4, -1347, -1347, -1347, -1347,   176, -1347,   226,
     185, -1347, -1347,    24,    38,    47,    50, -1347, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,    26,   185,
   -1347, -1347, -1347,  4553, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,  2300,
      41,    14, -1347, -1347,    93,   122, -1347, -1347,   526,   539,
     539,  2149,    27, -1347,  4347, -1347, -1347,   140,   185,   185,
    5048, -1347,  4842,  5224,  5309,    26, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347,  4347, -1347, -1347, -1347,   759,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347,  3377, -1347,   302,  3377,   225, -1347,  3652, -1347,
   -1347, -1347,  1218,   697,    26,   242,   301, -1347,   313,  2033,
     333,   177,   194, -1347,  3377,    56,    26,    26,    26, -1347,
   -1347,   201,   483,   212,   235, -1347,  5114,  2300,   489, -1347,
    5483,  3637,   233,    91,   120,   207,   219,   230,   255,   289,
     237,   291, -1347, -1347,   226,   293,    42, -1347, -1347, -1347,
   -1347,  5210,  5309,   319,  4186,   324,  2891, -1347,   539, -1347,
    -119,   379, -1347,   305,   -39,   341,   631,   185,   185, -1347,
   -1347, -1347, -1347, -1347, -1347,   348, -1347, -1347,    37,   225,
    1537, -1347,   351, -1347, -1347,   -63,  4842, -1347, -1347, -1347,
      25,   435, -1347, -1347, -1347, -1347,    26, -1347, -1347,   -20,
      26,   379, -1347, -1347, -1347, -1347, -1347,  3377, -1347,   635,
   -1347, -1347, -1347, -1347,  1371,  5309,   362,   -67,   368,  5356,
      26, -1347,  5309,  5309,  5309, -1347,  4347,  5309,  5309, -1347,
     386,   388,  5309,    44,  4347, -1347, -1347,   398,  3377,   341,
   -1347, -1347, -1347, -1347,  4285,   403, -1347, -1347, -1347, -1347,
   -1347, -1347,    73, -1347, -1347, -1347, -1347,   211, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347,  -105, -1347,  2300,
   -1347,  4517,   426, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
     440, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347, -1347,   185, -1347,   185, -1347,
   -1347, -1347,   185,   413,   123,  2451, -1347, -1347, -1347,   429,
   -1347, -1347,   -76, -1347, -1347, -1347, -1347,   578,  1674, -1347,
   -1347,  5420,   185,   361,   121,  5420,  2033,  3596,  2300,   148,
     539,  2149,   454,   226, -1347, -1347, -1347,   464,   185,   185,
   -1347,   185, -1347,   185, -1347,   361, -1347,   195, -1347,   195,
   -1347, -1347,   471,   501,  4553,   451, -1347, -1347, -1347,   185,
     185,  1144,  2986,  1773,   570, -1347, -1347, -1347,   452,    26,
      26, -1347,   481, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347, -1347,   507,   937, -1347,  5309,
     -34,  4347,   798,   517, -1347,  2602, -1347,   803,   520,   524,
     534,  2033, -1347, -1347,   341, -1347, -1347,  2825,    35,   521,
     813, -1347, -1347,   623,     9, -1347,  5309, -1347, -1347,    35,
     815,   -10,  5309,  5309,  5309,    26, -1347,    26,    26,    26,
    1688,    26,    26,  2300,  2300,    26, -1347, -1347,   826,   -83,
   -1347,   532,   554,   379, -1347, -1347, -1347,   185, -1347, -1347,
   -1347, -1347, -1347, -1347,   173, -1347,   555, -1347,   738, -1347,
   -1347, -1347,   185,   185, -1347,   -36,  2753, -1347, -1347, -1347,
   -1347,   561, -1347, -1347,   569,   572, -1347, -1347, -1347, -1347,
     573,   185,   798,  3530, -1347, -1347,   556,   185,  2135,  2286,
     185,   539,   847, -1347,   574,    80,  3819, -1347,  2300, -1347,
   -1347, -1347,   578,    23,  1674,    23,    23,    23,   809,   810,
   -1347, -1347, -1347, -1347, -1347, -1347,   577,   581, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347,  1371, -1347,   582,
     341,   226,  4347, -1347,  5420,   584,   798,   585,   583,   586,
     588,   589,   590,   591, -1347,   237,   594, -1347,   587,    65,
     660,   580,    31,    55, -1347, -1347, -1347, -1347, -1347, -1347,
     226,   226, -1347,   596,   598, -1347,   226, -1347,   226, -1347,
     597,    66,  5309,   677, -1347, -1347, -1347, -1347,  5309,   684,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
     185,  4805,    53,   139,  5309,   438,   108,   605,   609, -1347,
    3447,   607,   617,   616, -1347,   905, -1347, -1347,   612,   622,
    4494,  4292,   619,   620,  5290,   505,   226,  5309,    26,  5309,
    5309,   177,   177,   177,   621,   625,   627,   185,   150, -1347,
   -1347,  4347,   629,   632, -1347, -1347, -1347, -1347, -1347, -1347,
     173,  1827,   630,  2300,  2300,  1998,   124, -1347, -1347,  5210,
    2437,  2588,   539,   900, -1347, -1347, -1347,  3991, -1347,   637,
     -60,   472,    57,   210,   185,   634,   185,    26,   185,   104,
     640,  4347,  5290,    80, -1347,  3530,   639,   638, -1347, -1347,
   -1347, -1347,  5420, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347,  4553,   185,   185,   361,    35,   925,   798,   649,   371,
     654,   656,   662, -1347,   -19,   651, -1347,   651,   651,   651,
     651,   651, -1347, -1347,   185, -1347,   185,   185,   661, -1347,
   -1347,   663,   670,   341,   672,   673,   671,   678,   681,   683,
     185,  5309, -1347,    26,  5309,    54,  5309,   686, -1347, -1347,
   -1347,   417, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347, -1347,   685,   744,   754, -1347,   745,   698,
     -75,   973, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347,   685,   685, -1347,  4399, -1347, -1347, -1347, -1347,
     699,   226,     6,  4553,   703,  5309,  3095, -1347,   798,   705,
     706,   720, -1347,  2602, -1347,    48, -1347,   282,   300,   772,
     303,   350,   355,   366,   374,   376,   387,   395,   397,   411,
     423,   431,   433, -1347,  1043, -1347,   226, -1347,   185,   709,
      80,    80,    26,   521, -1347, -1347,  4553, -1347, -1347, -1347,
     716,    26,    26,   177,    80, -1347, -1347, -1347, -1347,   379,
   -1347,   185, -1347,  2300,   174,  5309, -1347, -1347,   816, -1347,
   -1347,   444,  5309, -1347, -1347,  4347,    26,   185,    26,   185,
     198,  4347,  5290,  4569,   903,  3564, -1347,  3056, -1347,   798,
     850,   719, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347,   712,   713, -1347,   718,   721,   722,   724,   725,
    5290, -1347,   882,   726,   728,  2300,   703,  1371, -1347,   729,
     210, -1347,  1010,   974,   976, -1347, -1347,   742,   743,  5309,
     456, -1347,    80,  5420,  5420, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347,    96,  1027, -1347, -1347,    31, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347,   746,   177,    26,   185,    26,
   -1347, -1347, -1347, -1347, -1347, -1347,   791, -1347, -1347, -1347,
   -1347,   798,   741,   747, -1347, -1347, -1347, -1347, -1347,   603,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,   -54, -1347,
      86,    62, -1347, -1347,  3384, -1347,   752, -1347, -1347,   341,
   -1347,   753, -1347, -1347, -1347, -1347,   760, -1347, -1347, -1347,
   -1347,   341,   476,   185,   185,   185,   441,   459,   461,   466,
     185,   185,   185,   185,   185,   185,   361,   185,   527,   185,
     595,   185,   185,   185,   185,   185,   185,   185,   361,   185,
    2970,   185,   128,   185,  3235,   185, -1347, -1347, -1347,  3848,
     755,   749, -1347,   756,   757,   761,   762, -1347,   891,   763,
     764,   765,   769, -1347,   173, -1347,   174,  2033, -1347,    26,
     937,   770,   771,  2300,  1371,   821, -1347,  2033,  2033,  2033,
    2033, -1347, -1347, -1347, -1347, -1347, -1347,  2033,  2033,  2033,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347,   341, -1347,   185,
     408,   416, -1347, -1347, -1347, -1347,  4805,   777,  4553, -1347,
     779, -1347, -1347,  1061, -1347,  4553, -1347,  4553,   185, -1347,
   -1347,    26, -1347,   784, -1347, -1347, -1347,   185, -1347,   778,
   -1347, -1347,   783,   276,   185,   185, -1347, -1347, -1347, -1347,
   -1347, -1347,   798,   785, -1347, -1347,   185, -1347,  -101,   792,
     793,   790,   794,   796,   797,   799,   800,   801,   814,   820,
     822,   823, -1347,   341, -1347, -1347,   185,   296, -1347,   553,
     795,   812,   825,   843,   845,   185,   185,   185,   185,   185,
     185,   361,   185,   851,   852,   854,   855,   862,   860,   866,
     873,   878,   879,   876,   881,   886,   883,   888,   892,   897,
     895,   901,   896,   906,   899,   907,   904,   909,   913,   911,
     917,  1086,   918,   915, -1347,  3305, -1347,  2963, -1347, -1347,
     841, -1347, -1347,    80,    80, -1347, -1347, -1347, -1347,  2300,
   -1347, -1347,   447, -1347,   858, -1347,  1160,   539, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347,  1119,   920, -1347, -1347, -1347,
   -1347,   922,   924, -1347,  2300,  5290, -1347, -1347, -1347, -1347,
    1194,    31,   185,   798,   921,   927,   341, -1347,   928,   185,
   -1347,   929,   934,   935,   936,   941,   916,   940,   942,   943,
     819, -1347, -1347, -1347,   945, -1347,   946,   951,   932,   952,
     949,   955,   956,   963,   961, -1347,   957, -1347,   967, -1347,
     968, -1347,   969, -1347, -1347,   972, -1347, -1347,   975, -1347,
     977, -1347,   978, -1347,   985, -1347,   988, -1347,   989, -1347,
   -1347,   990, -1347,   992, -1347,   991,  1261, -1347,   993,   475,
   -1347,   994,   995, -1347,    80,  2300,  5290,  4347, -1347, -1347,
   -1347,    80, -1347,   996, -1347,   998,   999,   298, -1347,  4844,
   -1347,  1000, -1347,   185,   185,   185, -1347, -1347, -1347, -1347,
   -1347,  1003, -1347,  1004, -1347,  1008, -1347,  1011, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347, -1347,  2970, -1347, -1347,  1009, -1347,   996,
    1371,  1012,  1016,  1014, -1347,    31, -1347,   798, -1347,     6,
   -1347,  1022,  1023,  1024,    30,    78, -1347, -1347, -1347, -1347,
      84,    85,    88,    59,    79,    71,    97,    98,    99,    75,
    4000,    69,  3165, -1347,   703,  1015,  1271, -1347,    80, -1347,
     486, -1347, -1347, -1347, -1347, -1347, -1347, -1347,   100,   103,
     106,    82, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347,  1287, -1347, -1347, -1347,    80,
    5290,  1557,  1028,   798, -1347, -1347, -1347, -1347, -1347,  1036,
    1038,  1039, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,   478,
    1051,    80,   185, -1347,  1219,  1040,  1041,   539, -1347, -1347,
    4347,  1371,  1317,  5290,   996,  1047,    80,  1042, -1347
};

/* YYDEFACT[STATE-NUM] -- Default reduction number in state STATE-NUM.
   Performed when YYTABLE does not specify something else to do.  Zero
   means the default is an error.  */
static const yytype_int16 yydefact[] =
{
       2,     0,     1,   102,   122,     0,   282,   226,   408,     0,
       0,   778,   779,     0,   239,     0,     0,   793,   799,   856,
     109,     0,     0,     0,     0,     0,     0,    29,     0,     0,
       0,     0,    74,    75,     0,    77,     3,    25,    26,    27,
     100,   101,   452,   452,    19,    17,    10,     9,   452,     0,
     125,   153,     0,     7,   289,   354,     8,     0,    18,   452,
       0,    11,    12,     0,     0,     0,     0,   835,    37,    56,
      54,    42,    38,    47,    53,    48,    49,    50,    51,    52,
      39,    40,    41,    43,    44,    45,    46,    55,   121,     0,
     206,   409,   410,   407,   763,   764,   765,   766,   767,   768,
     769,   770,   771,   772,   773,   774,   775,   776,   777,     0,
       0,    34,   233,   234,     0,     0,   240,   241,   246,   239,
     239,     0,    78,    88,     0,   237,   232,     0,     0,     0,
       0,   799,     0,     0,     0,   110,    58,    20,    21,    60,
      59,    23,    24,   572,   729,     0,   706,   714,   712,     0,
     715,   716,   717,   718,   719,   720,   725,   726,   727,   728,
     689,   713,     0,   705,     0,     0,    38,   510,     0,   573,
     574,   575,     0,     0,   576,     0,     0,   253,     0,   239,
       0,   570,     0,   710,    30,    69,    71,    72,    73,    76,
     454,     0,   453,     0,     0,     2,     0,     0,   155,   157,
     239,     0,     0,   415,   415,   415,   415,   415,   415,     0,
       0,     0,   405,   412,   452,     0,   781,   809,   827,   845,
     859,     0,     0,     0,     0,     0,     0,   571,   239,   578,
     739,   581,    32,     0,     0,   741,     0,     0,     0,   242,
     243,   244,   245,   235,   236,     0,    90,    89,     0,     0,
       0,   120,     0,    22,   794,   795,     0,   800,   801,   802,
     804,     0,   805,   806,   807,   808,   798,   857,   858,   854,
     111,   711,   721,   722,   723,   724,   688,     0,   691,     0,
     707,   709,   251,   252,     0,     0,     0,     0,     0,     0,
     704,   702,     0,     0,     0,   248,     0,     0,     0,   696,
       0,     0,     0,   732,   555,   695,   694,     0,    30,    70,
      81,   455,    85,   119,     0,     0,   128,   150,   126,   127,
     130,   131,     0,   132,   133,   134,   135,   136,   137,   138,
     139,   140,   129,   149,   142,   141,   151,   165,   154,     0,
     124,     0,     0,   295,   290,   291,   292,   293,   294,   298,
     296,   306,   297,   299,   300,   301,   302,   303,   304,   305,
       0,   307,   331,   511,   512,   513,   514,   515,   516,   517,
     518,   519,   520,   521,   522,   523,     0,   390,     0,   353,
     361,   362,     0,     0,     0,     0,   383,     6,   368,     0,
     370,   369,     0,   355,   376,   354,   357,     0,     0,   363,
     525,     0,     0,     0,     0,     0,   239,     0,     0,     0,
     239,     0,     0,   452,   364,   366,   367,     0,     0,     0,
     431,     0,   430,     0,   429,     0,   428,     0,   426,     0,
     427,   451,     0,   414,     0,     0,   740,   790,   780,     0,
       0,     0,     0,     0,     0,   838,   837,   836,     0,   833,
      57,   227,     0,   213,   207,   208,   209,   210,   215,   216,
     217,   218,   212,   219,   220,   211,     0,     0,   406,     0,
       0,     0,     0,     0,   749,   743,   748,     0,    35,     0,
       0,   239,    92,    86,    79,   328,   329,   732,   330,   553,
       0,   113,   796,   792,   825,   803,     0,   690,   708,   250,
       0,     0,     0,     0,     0,   703,   701,    67,    68,    66,
       0,    65,   577,     0,     0,    64,   733,   692,   734,     0,
     730,     0,   556,   557,    28,    31,     5,     0,   143,   144,
     145,   146,   147,   148,   174,   123,   156,   160,     0,   122,
     256,   270,     0,     0,   835,     0,     0,     4,   198,   199,
     192,     0,   158,   188,     0,     0,   354,   189,   190,   191,
       0,     0,   312,     0,   356,   358,     0,     0,     0,     0,
       0,   239,     0,   365,     0,   331,     0,   400,     0,   398,
     401,   384,   386,     0,     0,     0,     0,     0,     0,     0,
     387,   527,   526,   528,   529,    61,     0,     0,   524,   531,
     530,   534,   533,   535,   539,   540,   538,     0,   541,     0,
     542,   452,     0,   546,   548,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   411,     0,     0,   419,     0,   783,
       0,     0,     0,     0,    13,   821,   820,   812,   810,   813,
     452,   452,   832,     0,     0,    14,   452,   830,   452,   828,
       0,     0,     0,     0,    15,   853,   852,   846,     0,     0,
      16,   864,   863,   860,   839,   840,   841,   842,   843,   844,
       0,   582,   222,     0,   579,     0,     0,     0,   750,    92,
       0,     0,     0,   744,    33,     0,   238,   247,    82,     0,
      95,   555,     0,     0,     0,     0,   452,     0,   855,     0,
       0,   568,   566,   567,   695,     0,     0,   736,   732,   693,
     700,     0,     0,     0,   169,   171,   170,   172,   167,   168,
     174,     0,     0,     0,     0,     0,   239,   193,   194,     0,
       0,     0,   239,     0,   157,   259,   273,     0,   845,     0,
     312,     0,     0,   283,     0,     0,     0,   378,     0,     0,
       0,     0,     0,   331,   563,     0,     0,   560,   561,   382,
     399,   385,     0,   402,   392,   396,   397,   395,   391,   393,
     394,     0,     0,     0,     0,   537,     0,     0,     0,     0,
     551,   552,     0,   532,     0,   415,   416,   415,   415,   415,
     415,   415,   413,   418,     0,   782,     0,     0,     0,   815,
     814,     0,     0,   818,     0,     0,     0,     0,     0,     0,
       0,     0,   851,   847,     0,     0,     0,     0,   636,   590,
     591,     0,   625,   592,   593,   594,   595,   596,   597,   627,
     603,   604,   605,   606,   637,     0,     0,   633,     0,     0,
       0,   587,   588,   589,   612,   613,   614,   631,   615,   616,
     617,   618,   637,   637,   621,   639,   629,   635,   598,   287,
       0,     0,   285,     0,   224,   580,     0,   737,     0,     0,
      54,     0,   742,   743,    36,     0,    80,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,    94,    91,   459,   452,    93,     0,     0,
     331,   331,   330,   553,   114,   115,     0,   116,   117,   118,
       0,   826,   249,   569,   331,   697,   698,   735,   731,   558,
     152,     0,   175,   161,   178,     0,   166,   159,     0,   258,
     257,   576,     0,   272,   271,     0,   834,     0,   201,     0,
       0,     0,     0,     0,     0,     0,   184,     0,   308,     0,
       0,     0,   319,   320,   321,   322,   314,   315,   316,   313,
     317,   318,     0,     0,   311,     0,     0,     0,     0,     0,
       0,   373,   371,     0,     0,     0,   224,     0,   374,     0,
     283,   359,   331,     0,     0,   388,   389,     0,     0,     0,
       0,   544,   331,   548,   548,   547,   417,   425,   424,   423,
     422,   420,   421,   787,   785,   811,   822,     0,   824,   816,
     819,   797,   823,   829,   831,     0,   848,   849,     0,   862,
     221,   626,   599,   600,   601,   602,     0,   622,   628,   630,
     634,     0,     0,     0,   632,   619,   620,   643,   644,     0,
     671,   645,   646,   647,   648,   649,   650,   673,   655,   656,
     657,   658,   641,   642,   663,   664,   665,   666,   667,   668,
     669,   670,   640,   674,   675,   676,   677,   678,   679,   680,
     681,   682,   683,   684,   685,   686,   687,   659,   623,   214,
       0,     0,   607,   223,     0,   205,     0,   753,   754,   758,
     756,     0,   755,   752,   751,   738,     0,    95,   745,    92,
      87,    83,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,    98,    99,    97,     0,
       0,     0,   554,     0,     0,     0,     0,   112,   795,     0,
       0,     0,   162,   163,   174,   177,   178,   239,   204,   254,
       0,     0,     0,     0,     0,     0,   185,   239,   239,   239,
     239,   186,   267,   268,   266,   260,   265,   239,   239,   239,
     187,   280,   281,   278,   274,   279,   195,   312,   310,     0,
       0,     0,   332,   333,   334,   335,   582,   165,     0,   377,
       0,   380,   381,     0,   360,   564,   562,     0,     0,    62,
      63,   536,   543,     0,   549,   550,   786,     0,   784,     0,
     850,   861,     0,     0,     0,     0,   672,   651,   652,   653,
     654,   661,     0,     0,   662,   286,     0,   608,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   458,   457,   456,   225,     0,     0,    95,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   105,     0,   104,     0,   103,   450,
       0,   231,   230,   331,   331,   791,   699,   173,   180,     0,
     179,   176,     0,   200,     0,   203,     0,   239,   261,   262,
     263,   264,   277,   275,   276,     0,     0,   323,   324,   325,
     326,     0,     0,   372,     0,     0,   565,   403,   404,   545,
     789,     0,     0,     0,     0,     0,   624,   660,     0,     0,
     609,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,   746,    84,   449,     0,   448,     0,     0,     0,     0,
       0,     0,     0,     0,     0,   439,     0,   438,     0,   437,
       0,   436,     0,   434,   432,     0,   435,   433,     0,   447,
       0,   446,     0,   445,     0,   444,     0,   465,     0,   461,
     460,     0,   464,     0,   463,     0,     0,   107,     0,     0,
     183,     0,     0,   164,   331,     0,     0,     0,   309,   327,
     284,   331,   379,   181,   788,     0,     0,     0,   585,   582,
     611,     0,   757,     0,     0,     0,   762,   747,   499,   495,
     443,     0,   442,     0,   441,     0,   440,     0,   497,   495,
     493,   491,   485,   488,   497,   495,   493,   491,   508,   501,
     462,   504,   106,   108,     0,   229,   228,     0,   202,   181,
       0,     0,     0,     0,   182,     0,   638,     0,   584,   586,
     610,     0,     0,     0,     0,     0,   497,   495,   493,   491,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,    96,   224,     0,     0,   336,   331,   817,
       0,   759,   760,   761,   481,   500,   480,   496,     0,     0,
       0,     0,   471,   498,   470,   469,   494,   468,   492,   466,
     487,   486,   467,   490,   489,   475,   474,   473,   472,   484,
     509,   503,   502,   482,   505,     0,   483,   507,   269,   331,
       0,     0,     0,     0,   479,   478,   477,   476,   506,     0,
       0,     0,   341,   337,   346,   347,   348,   349,   350,   351,
     338,   339,   340,   342,   343,   344,   345,   288,   375,     0,
       0,   331,     0,   583,     0,     0,     0,   239,   196,   352,
       0,     0,     0,     0,   181,     0,   331,     0,   197
};

/* YYPGOTO[NTERM-NUM].  */
static const yytype_int16 yypgoto[] =
{
   -1347,  1149, -1347,  1044,  -102,    17,   -41,    -5,    10,    22,
    -420, -1347,    11,   -21,  1328, -1347, -1347,   874,   947,  -643,
   -1347,  -976, -1347,    32, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347,  -330, -1347, -1347, -1347,   633, -1347, -1347,
   -1347,   160, -1347,   641,   209,   208, -1347, -1346,  -462, -1347,
    -322, -1347, -1347,  -960, -1347,  -169,  -114, -1347,     3,  1340,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,   390,
     170, -1347,  -321, -1347,  -706,  -680,  1006, -1347, -1347,  -258,
   -1347,  -154, -1347, -1347,   789, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347,   -62,    21, -1347, -1347, -1347,   748,  -147,
    1318,   290,   -44,     8,   519, -1347, -1091, -1347, -1347, -1326,
   -1309, -1339, -1303, -1347, -1347, -1347, -1347,    13, -1347, -1347,
   -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,
   -1347, -1347, -1347,  -204,   479,   687, -1347,  -716, -1347,   401,
     -22,  -461,  -108,   -53,   -40, -1347,   -23,   249, -1347,   682,
      20,   516, -1347, -1347,   528, -1347, -1067, -1347,  1391, -1347,
      28, -1347, -1347,   257,   926, -1347,  1279, -1347, -1347,  -977,
     981, -1347, -1347, -1347, -1347, -1347, -1347, -1347, -1347,   867,
     679, -1347, -1347, -1347, -1347, -1347
};

/* YYDEFGOTO[NTERM-NUM].  */
static const yytype_int16 yydefgoto[] =
{
       0,     1,    36,   307,   676,   388,    87,   174,   800,  1538,
     600,    38,   390,    40,    41,    42,    43,   122,   245,   689,
     690,   894,  1139,   391,  1307,    45,    46,   695,    47,    48,
      49,    50,    51,    52,   196,   198,   340,   341,   536,  1151,
    1152,   535,   720,   721,   722,  1155,   925,  1483,  1484,   552,
      53,   224,   864,  1085,    90,   123,   124,   125,   227,   246,
     554,   725,   944,  1175,   555,   726,   945,  1184,    54,   970,
     860,   861,    55,   200,   741,   489,   755,  1561,   392,   201,
     393,   763,   395,   396,   581,   397,   398,   582,   583,   584,
     585,   586,   587,   764,   399,    57,    93,   212,   432,   420,
     433,   895,   896,   191,   192,  1255,   897,  1504,  1505,  1503,
    1502,  1495,  1500,  1494,  1511,  1512,  1510,   228,   400,   401,
     402,   403,   404,   405,   406,   407,   408,   409,   410,   411,
     412,   413,   414,   782,   693,   521,   522,   756,   757,   758,
     229,   181,   247,   862,  1027,  1078,   231,   183,   519,   520,
     415,   682,   683,    59,   677,   678,  1092,  1093,   109,    60,
     416,    62,   130,   493,   646,    63,   132,   441,   638,   801,
     639,   640,   648,   641,    64,   442,   649,    65,   560,   221,
     443,   657,    66,   133,   444,   663
};

/* YYTABLE[YYPACT[STATE-NUM]] -- What to do in state STATE-NUM.  If
   positive, shift that token.  If negative, reduce the rule whose
   number is the opposite.  If YYTABLE_NINF, syntax error.  */
static const yytype_int16 yytable[] =
{
      88,   230,   182,   180,   175,   243,   244,   622,   235,   623,
     295,   551,    39,   214,   903,   135,  1202,   126,    37,   553,
     556,    58,    56,   185,   186,   187,   188,   692,   177,    61,
    1219,   137,   138,    44,   950,  1261,   869,   979,   179,  1295,
     136,   701,   702,   703,   232,   141,   142,   394,   232,   437,
     438,   193,   225,   136,  -576,   232,   194,   422,   424,   426,
     428,   430,   232,   232,   516,   296,   136,   215,   139,   140,
     216,   136,   978,   795,   233,   136,   730,  1551,   949,   762,
     139,   140,   595,   309,   139,   140,   362,   136,   136,   338,
     595,   139,   140,   136,   136,  1087,  1088,   136,   311,   223,
     754,   250,   596,   597,  1216,   492,   136,   136,   136,   136,
     596,   597,   136,  1552,   471,   136,   863,  1018,   496,    67,
    1501,  1259,   271,   574,   177,   255,  1507,   266,   269,   270,
     139,   140,   595,  1515,   179,    89,  1297,   576,   252,   253,
     276,  1509,   490,   278,   111,   284,   484,   697,   528,   529,
     530,    91,   596,   597,  1032,   232,   127,  1508,  1529,   136,
     389,  1506,   308,   -39,   114,   128,   214,   287,   290,   116,
     516,   117,  1298,  1531,  1033,   288,   500,   472,   118,   129,
     473,   209,   126,   291,  1143,  1144,   531,   532,   533,  1530,
     222,   337,  1359,  1528,   136,   119,   933,   534,  1149,   202,
    1360,   467,   203,   204,   205,   206,   595,   207,   208,   209,
     120,   708,   713,   702,    92,   131,   449,   450,   709,  1231,
     575,   470,   435,   184,   714,   715,   596,   597,   222,   288,
     189,   537,   222,   477,   503,   568,   609,   603,   190,   913,
    1232,   195,   716,   934,   590,   488,  1233,   479,   480,   279,
     280,   494,   475,  1234,   731,   497,   197,   476,  1605,   281,
     202,   222,  1165,   203,   204,   205,   206,   674,   207,   208,
     209,   -53,   -53,   510,   477,   222,  1213,   572,   996,   499,
     501,   523,  1370,   811,   505,   222,   308,   507,   508,   509,
    1197,   700,   511,   512,   717,   199,   612,   515,   737,   506,
     608,   299,   300,   301,   222,   136,  1081,   610,   236,   965,
     966,   967,  1082,   518,   386,   217,   -39,   299,   300,   301,
     -39,   222,  1366,  1367,  1368,    39,   248,  1524,   482,   218,
     222,    37,   302,   483,    58,    56,   439,   799,   219,  1099,
     234,   220,    61,  -576,  1100,   517,    44,   440,   222,   222,
    1016,   222,   549,   277,   802,  1236,  1537,   588,   548,   796,
     591,   558,   557,  1237,   601,   611,  1553,   681,  1542,   559,
     139,   140,  1548,   550,  1435,  1526,  1539,  -559,  1235,  1567,
     292,  1532,  1534,  1513,   607,  1535,   564,   418,   565,   237,
    1217,   419,   566,   626,  1545,  1546,  1547,  1564,   975,   222,
    1565,   477,   490,  1566,   913,   705,   706,   866,   589,   126,
     606,   592,   593,   569,   177,   602,   421,   598,   238,  1550,
     419,   615,   394,   570,   179,   594,   599,  1525,   617,   618,
     767,   619,   112,   620,   222,   113,   251,   969,   733,   293,
     865,  1145,  1021,  1022,  1023,  1024,  1025,   621,   675,   628,
     629,   294,   635,   635,   655,   661,  1260,   751,   114,   115,
     740,   637,   672,   116,   673,   117,   718,   210,  1154,   222,
     760,   297,   118,   636,   636,   656,   662,   719,   299,   300,
     301,  1335,   470,   211,   126,   298,   299,   300,   301,   119,
     288,   698,  1163,   222,   303,   311,   304,   518,   310,   776,
     305,   306,   -53,   423,   120,   488,   -53,   419,  1519,   312,
     303,   951,   304,   -53,   784,   425,   305,   306,   952,   419,
     953,   954,   955,   768,   769,   770,   427,   664,   665,   666,
     419,   803,   313,   904,   905,   389,   136,   712,   595,   417,
     743,  1087,  1088,   765,   211,   299,   300,   301,  1337,  1338,
     339,   429,   727,   728,  1558,   419,  1339,  1340,   596,   597,
     956,   957,   958,   747,   749,   667,   668,   669,   906,   477,
    1353,   739,   239,   780,   240,   241,   242,   745,  1102,   114,
     750,   214,  1103,   394,   116,   431,   117,   434,     3,   779,
     436,   477,  1487,   118,   766,  1488,  1104,  1421,  1422,  1110,
    1105,   474,   775,  1111,   139,   140,   595,   959,   960,   961,
     119,   962,   935,   551,   963,   927,   928,   932,   941,   778,
     451,   553,   556,   468,   781,   120,   596,   597,  1226,  1227,
    1228,  1229,  1230,   577,   477,   578,   579,   580,   997,   478,
     998,   999,  1000,  1001,  1002,   481,  1112,   813,   804,   805,
    1113,  1114,   491,   815,   808,  1115,   809,   495,   276,  1136,
     498,   812,  1116,   502,  1137,  1433,  1117,   992,   523,   504,
    1118,   303,  1120,   304,  1119,   990,  1121,   305,   306,   303,
     817,   304,   513,  1122,   514,   305,   306,  1123,   919,   902,
     524,  1124,   911,  1126,   912,  1125,   389,  1127,  1282,   527,
    1285,   893,    68,    69,   910,    70,   136,  1128,  1477,   907,
     908,  1129,   909,   567,   121,  1481,   924,   917,   518,  1130,
     931,   983,   561,  1131,   936,   938,   940,  1132,   977,  1134,
     985,  1133,   980,  1135,   573,  -255,   562,  1265,   303,   222,
     304,  1266,   222,  1424,   704,   306,  1479,   902,   627,   477,
     614,    71,   968,  1212,   971,  1267,   973,  1269,   974,  1268,
     616,  1270,  1271,   624,  1089,   681,  1272,    72,    14,   964,
     222,   477,   984,  1101,  1474,  1593,    73,   670,   658,   477,
    1563,   659,   986,   987,    74,   272,   273,   274,   275,  1214,
    1215,    75,    76,    77,    78,   625,   988,    79,  1106,  1107,
    1108,  1109,  1562,   671,  1003,   232,  1004,  1005,   679,  1017,
     684,  1019,  1035,  1036,   685,  1153,  1361,  1362,  1363,  1364,
    1015,   686,  1083,   691,    80,    81,    82,    83,    84,    85,
      86,   687,   694,   643,   699,   710,    28,    29,    30,    31,
      32,    33,    34,  1569,  1091,  1371,   707,  1187,   711,   723,
     724,    35,   734,    28,    29,    30,    31,    32,    33,    34,
     735,   744,   660,   736,   738,  1147,   752,  1201,    35,  1203,
     753,   771,   772,   773,   797,  1595,  1090,   774,   777,   798,
    1570,   783,   785,   787,   786,   788,   789,   790,   791,   951,
    1607,   793,   794,   810,   814,   806,   952,   807,   953,   954,
     955,   816,   867,   868,  1140,  1138,   871,  1158,  1141,   872,
     873,   875,  1160,   874,   876,   900,   901,   914,  1164,   942,
    1156,     3,   915,  1604,   916,  1136,   920,  1159,   921,  1223,
    1137,  1150,   982,   926,   948,   972,   981,   902,   956,   957,
     958,   976,    68,    69,   989,    70,   991,  1161,   993,  1162,
     994,   419,   780,   780,   549,  1173,  1182,  1006,   655,   995,
     548,  1172,  1181,   558,   557,   902,  1008,  1011,  1007,  1009,
    1010,   559,  1176,  1185,  1012,   550,  1174,  1183,  1013,   656,
    1014,  1026,  1253,  1020,  1211,   959,   960,   961,  1028,   962,
    1029,    71,   963,  1030,  1031,  1034,  1079,  1095,  1328,  1329,
    1330,  1331,  1084,   781,   781,  1096,   289,    72,  1332,  1333,
    1334,  1097,  1142,  1148,  1157,  1189,    73,  1190,  1191,  1192,
    1198,  1196,  1193,  1194,    74,  1195,  1204,  1199,  1221,  1200,
     754,    75,    76,    77,    78,  1218,  1207,    79,  1208,  1209,
    1210,  1224,  1222,  1220,   299,   300,   301,  1225,  1256,  1257,
    1310,  1258,  1309,  1311,  1312,  1325,  1326,  1313,  1314,   492,
    1316,  1317,  1318,  1319,    80,    81,    82,    83,    84,    85,
      86,  1323,  1324,  1344,  1167,  1168,  1169,  1170,  1327,   534,
    1345,  1349,  1352,  1351,  1541,  1544,  1357,  1372,  1102,  1104,
    1110,  1238,  1112,  1114,  1413,  1116,  1118,  1120,    11,    12,
      13,    14,    28,    29,    30,    31,    32,    33,    34,  1373,
    1122,  1447,  1302,  1262,  1263,  1264,  1124,    35,  1126,  1128,
    1273,  1274,  1275,  1276,  1277,  1278,  1374,  1280,  1281,  1283,
    1356,  1286,  1287,  1288,  1289,  1290,  1291,  1292,  1279,  1294,
    1375,  1296,  1284,  1299,  1420,  1303,  1376,  1188,  1385,  1136,
    1293,  1387,  1425,  1386,  1137,  1322,  1388,  1343,   951,  1389,
     126,  1390,     3,  1391,  1346,   952,  1347,   953,   954,   955,
     126,   126,   126,   126,  1392,  1393,  1394,  1395,  1396,  1426,
     126,   126,   126,  1397,  1398,  1399,    28,    29,    30,    31,
      32,    33,    34,  1400,  1401,  1171,  1402,  1404,  1403,  1336,
    1406,    35,  1434,  1405,  1407,  1408,  1409,   956,   957,   958,
    1410,  1423,  1411,  1427,  1412,  1414,  1415,  1429,  1348,  1430,
    1431,  1443,  1438,    68,    69,  1265,    70,  1350,  1439,  1440,
    1267,  1269,  1271,  1451,  1354,  1355,  1432,   303,  1442,   304,
    1446,  1448,  1449,   305,   306,  1444,  1358,  1445,  1450,  1452,
    1453,  1437,  1454,  1458,   959,   960,   961,  1455,   962,   115,
    1456,   963,  1457,  1459,  1460,  1461,  1365,  1369,  1462,  1472,
    1138,  1463,    71,  1464,  1465,  1377,  1378,  1379,  1380,  1381,
    1382,  1466,  1384,  1417,  1467,  1468,  1469,  1471,    72,  1470,
    1560,  1475,  1476,  1383,  1473,  1568,  1486,    73,  1482,  1496,
    1497,  1490,  1419,  1485,  1498,    74,  1514,  1499,  1594,  1517,
    1518,  1559,    75,    76,    77,    78,   898,  1478,    79,  1521,
    1522,  1523,     9,    10,  1597,  1588,    28,    29,    30,    31,
      32,    33,    34,  1590,  1591,  1592,  1603,  1598,  1599,  1608,
     902,    35,    14,  1606,   314,    80,    81,    82,    83,    84,
      85,    86,   525,   176,   630,   688,   631,  1342,   613,   632,
     633,   922,  1436,  1320,  1321,   178,  1341,   943,   563,  1441,
    1205,   761,  1516,   792,  1254,   213,    68,    69,   899,    70,
    1080,  1138,  1146,  1206,   143,  1520,  1489,   144,  1308,  1098,
     918,   145,   146,   147,   148,   149,  1094,   150,   151,   152,
     153,   110,   154,   155,  1480,  1315,   156,   157,   158,   159,
     256,   729,   115,   160,   161,     0,  1428,   947,     0,   696,
       0,   902,   162,   647,   163,    71,     0,    28,    29,    30,
      31,    32,    33,    34,     0,     0,   634,   285,     0,   164,
     165,   166,    35,     0,     0,     0,     0,     0,     0,     0,
      73,     0,     0,  1491,  1492,  1493,     0,     0,    74,     0,
       0,  1589,     0,     0,     0,    75,    76,    77,    78,     0,
       0,    79,     0,     0,     0,     0,   167,     0,   299,   300,
     301,     0,     0,  1600,     0,     0,     0,     0,     0,     0,
    1557,     0,     0,  1602,     0,     0,     0,     0,    80,    81,
      82,    83,    84,    85,    86,  1527,     0,     0,     0,     0,
    1533,  1527,  1536,     0,  1540,     0,  1533,  1527,  1536,     0,
       0,     0,     0,     0,   286,     0,     0,  1543,     0,     0,
       0,     0,   169,   170,   171,     0,     0,     0,  1533,  1527,
    1536,     0,    68,    69,     0,    70,     0,     0,     0,     0,
     143,     0,     0,   144,     0,   902,     0,   145,   146,   147,
     148,   149,     0,   150,   151,   152,   153,     0,   154,   155,
       0,     0,   156,   157,   158,   159,     0,  1601,   115,   160,
     161,     0,     0,     0,     0,     0,     0,     0,   162,     0,
     163,    71,     0,     0,     0,     0,  1571,     0,   902,     0,
       0,     0,  1596,     0,     0,   164,   165,   166,     0,     0,
       0,  1572,     0,     0,     0,     0,    73,     0,     0,     0,
       0,     0,     0,     0,    74,     0,     0,  1573,     0,     0,
       0,    75,    76,    77,    78,     0,  1574,    79,     0,     0,
       0,     0,   167,     0,   299,   300,   301,     0,     0,     0,
       0,  1575,  1576,  1577,  1578,     0,     0,  1579,   485,   486,
       0,     0,     0,     0,    80,    81,    82,    83,    84,    85,
      86,   487,     0,   304,     0,     0,     0,   305,   306,    68,
     173,     0,    70,   136,  1580,  1581,  1582,  1583,  1584,  1585,
    1586,     0,     0,    68,    69,     0,    70,     0,   169,   170,
     171,   143,     0,     0,   144,     0,     0,     0,   145,   146,
     147,   148,   149,     0,   150,   151,   152,   153,     0,   154,
     155,     0,     0,   156,   157,   158,   159,     0,    71,   115,
     160,   161,     0,     0,     0,     0,     0,     0,     0,   162,
       0,   163,    71,     0,    72,     0,     0,     0,     0,     0,
       0,     0,     0,    73,     0,     0,   164,   165,   166,     0,
       0,    74,     0,     0,     0,     0,     0,    73,    75,    76,
      77,    78,     0,     0,    79,    74,     0,     0,     0,     0,
       0,     0,    75,    76,    77,    78,     0,     0,    79,     0,
       0,     3,     0,   167,     0,   299,   300,   301,     0,     0,
       0,    80,    81,    82,    83,    84,    85,    86,     0,   485,
     486,     0,     0,     0,   650,    80,    81,    82,    83,    84,
      85,    86,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,    68,    69,     0,    70,     0,   487,     0,   304,
       0,     0,     0,   305,   306,     0,   173,     0,  1587,   169,
     170,   171,    68,    69,     0,    70,     0,     0,     0,     0,
     143,     0,     0,   144,     0,     0,     0,   145,   146,   147,
     148,   149,     0,   150,   151,   152,   153,     0,   154,   155,
       0,    71,   156,   157,   158,   159,     0,     0,   115,   160,
     161,     0,     0,     0,     0,     0,     0,    72,   162,     0,
     163,    71,     0,     0,     0,     0,    73,     0,     0,     0,
       0,     0,     0,     0,    74,   164,   165,   166,     0,     0,
       0,    75,    76,    77,    78,     0,    73,    79,     0,   651,
       0,     0,     0,     0,    74,     0,     0,     0,     0,     0,
       0,    75,    76,    77,    78,     0,     0,    79,     0,     0,
       0,     0,   167,   168,    80,    81,    82,    83,    84,    85,
      86,     0,     0,     0,     0,   386,     0,     0,     0,     0,
       0,    14,     0,     0,    80,    81,    82,    83,    84,    85,
      86,   652,     0,     0,   653,     0,     0,     0,   487,     0,
     304,     0,     0,     0,   704,   306,     0,   173,     0,     0,
       0,     0,     0,    68,    69,     0,    70,     0,   169,   170,
     171,   143,     0,     0,   144,     0,     0,     0,   145,   146,
     147,   148,   149,     0,   150,   151,   152,   153,     0,   154,
     155,     0,     0,   156,   157,   158,   159,     0,     0,   115,
     160,   161,     0,     0,     0,    14,     0,   112,     0,   162,
     113,   163,    71,     0,     0,     0,    28,    29,    30,    31,
      32,    33,    34,     0,     0,   654,   164,   165,   166,     0,
     929,    35,     0,   114,   115,     0,     0,    73,   116,     0,
     117,     0,     0,     0,     0,    74,     0,   118,     0,     0,
       0,     0,    75,    76,    77,    78,     0,     0,    79,     0,
       0,     0,     0,   167,   119,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   930,     0,   120,
       0,     0,     0,   923,     0,    80,    81,    82,    83,    84,
      85,    86,     0,     0,     0,     0,     0,     0,     0,     0,
      68,    69,     0,    70,     0,     0,     0,   172,     0,     0,
       0,     0,     0,     0,    68,    69,   173,    70,     0,   169,
     170,   171,   143,     0,     0,   144,     0,     0,     0,   145,
     146,   147,   148,   149,     0,   150,   151,   152,   153,     0,
     154,   155,     0,     0,   156,   157,   158,   159,     0,    71,
     115,   160,   161,     0,     0,     0,     0,     0,     0,     0,
     162,     0,   163,    71,     0,    72,     0,     0,     0,     0,
       0,     0,     0,     0,    73,     0,     0,   164,   165,   166,
       0,     0,    74,     0,     0,     0,     0,     0,    73,    75,
      76,    77,    78,     0,     0,    79,    74,     0,     0,     0,
       0,     0,     0,    75,    76,    77,    78,     0,     0,    79,
       0,     0,     0,     0,   167,   168,     0,     0,     0,     0,
       0,     0,    80,    81,    82,    83,    84,    85,    86,     0,
       0,     0,     0,     0,     0,     0,    80,    81,    82,    83,
      84,    85,    86,     0,     0,     0,     0,     0,     0,     0,
       0,    68,    69,     0,    70,     0,     0,     0,   226,     0,
       0,     0,     0,     0,     0,    68,    69,   173,    70,     0,
     169,   170,   171,   143,     0,     0,   144,     0,     0,     0,
     145,   146,   147,   148,   149,     0,   150,   151,   152,   153,
       0,   154,   155,     0,     0,   156,   157,   158,   159,     0,
      71,   115,   160,   161,     0,     0,     0,     0,     0,     0,
       0,   162,     0,   163,    71,     0,    72,     0,     0,     0,
       0,     0,     0,     0,     0,    73,     0,     0,   164,   165,
     166,     0,     0,    74,     0,     0,     0,     0,     0,    73,
      75,    76,    77,    78,     0,     0,    79,    74,     0,     0,
       0,     0,     0,     0,    75,    76,    77,    78,     0,     0,
      79,     0,     0,     0,     0,   167,     0,     0,     0,     0,
       0,     0,     0,    80,    81,    82,    83,    84,    85,    86,
       0,     0,     0,     0,     0,     0,     0,    80,    81,    82,
      83,    84,    85,    86,     0,   746,     0,     0,     0,     0,
       0,     0,    68,    69,     0,    70,     0,     0,     0,   226,
       0,     0,     0,     0,     0,     0,    68,    69,   173,    70,
       0,   169,   170,   171,   143,     0,     0,   144,     0,     0,
       0,   145,   146,   147,   148,   149,     0,   150,   151,   152,
     153,     0,   154,   155,     0,     0,   156,   157,   158,   159,
       0,    71,   115,   160,   161,     0,     0,     0,     0,     0,
       0,     0,   162,     0,   163,    71,     0,    72,     0,     0,
       0,     0,     0,     0,     0,     0,    73,     0,     0,   164,
     165,   166,     0,     0,    74,     0,     0,     0,     0,     0,
      73,    75,    76,    77,    78,     0,     0,    79,    74,     0,
       0,     0,     0,     0,     0,    75,    76,    77,    78,     0,
       0,    79,     0,     0,     0,     0,   571,     0,     0,     0,
       0,     0,     0,     0,    80,    81,    82,    83,    84,    85,
      86,     0,     0,     0,     0,     0,     0,     0,    80,    81,
      82,    83,    84,    85,    86,     0,   748,     0,     0,     0,
       0,     0,     0,    68,    69,     0,    70,     0,     0,     0,
     226,     0,     0,     0,     0,     0,     0,    68,    69,   173,
      70,     0,   169,   170,   171,   143,     0,     0,   144,     0,
       0,     0,   145,   146,   147,   148,   149,     0,   150,   151,
     152,   153,     0,   154,   155,     0,     0,   156,   157,   158,
     159,     0,    71,   115,   160,   161,     0,     0,     0,     0,
       0,     0,     0,   680,     0,   163,    71,     0,    72,     0,
       0,     0,     0,     0,     0,     0,     0,    73,     0,     0,
     164,   165,   166,     0,     0,    74,     0,     0,     0,     0,
       0,    73,    75,    76,    77,    78,     0,     0,    79,    74,
       0,     0,     0,     0,     0,     0,    75,    76,    77,    78,
       0,     0,    79,     0,     0,     0,     0,   167,     0,     0,
       0,     0,     0,     0,     0,    80,    81,    82,    83,    84,
      85,    86,     0,     0,     0,     0,     0,     0,     0,    80,
      81,    82,    83,    84,    85,    86,     0,   937,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,   226,     0,     0,     0,     0,     0,     0,    68,    69,
     173,    70,     0,   169,   170,   171,   143,     0,     0,   144,
       0,     0,     0,   145,   146,   147,   148,   149,     0,   150,
     151,   152,   153,     0,   154,   155,     0,     0,   156,   157,
     158,   159,     0,     0,   115,   160,   161,     0,     0,     0,
       0,     0,     0,     0,   162,     0,   163,    71,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,   164,   165,   166,     0,     0,     0,     0,     0,     0,
      68,    69,    73,    70,   136,     0,     0,     0,     0,     0,
      74,     0,     0,     0,     0,   516,     0,    75,    76,    77,
      78,     0,     0,    79,     0,     0,     0,     0,   732,     0,
       0,     0,     0,     0,     0,     0,   115,     0,     0,     2,
       0,     0,     0,     0,     0,     0,     0,     0,     0,    71,
      80,    81,    82,    83,    84,    85,    86,     3,   939,     0,
       0,     0,     0,     0,     0,    72,    68,    69,     0,    70,
       0,     0,   226,     0,    73,     0,     0,     0,     0,     0,
       0,   173,    74,     0,   169,   170,   171,     0,     0,    75,
      76,    77,    78,     0,     0,    79,     0,     0,     0,     0,
       0,     0,   115,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,    71,     0,     0,     0,     0,
       0,     0,    80,    81,    82,    83,    84,    85,    86,     0,
       0,    72,     0,     0,     0,     0,     0,     0,    68,    69,
      73,    70,     0,     0,     0,     0,     0,     0,    74,     0,
       0,     0,     0,     0,     0,    75,    76,    77,    78,     0,
       0,    79,   877,   878,   879,     0,   880,   881,   882,   883,
       0,   884,   885,   209,     3,   886,   887,   888,   889,     0,
       0,     0,   890,   891,     0,     0,     0,    71,    80,    81,
      82,    83,    84,    85,    86,     4,     5,     6,     7,     8,
       0,     0,     0,    72,     0,     0,     0,     0,     0,     0,
       0,     0,    73,     0,   469,     0,     0,     9,    10,     0,
      74,     0,     0,   226,     0,     0,     0,    75,    76,    77,
      78,     0,   173,    79,    11,    12,    13,    14,     0,     0,
     642,    15,    16,     0,     3,     0,     0,    17,     0,     0,
      18,     0,   892,     0,     0,     0,     0,    19,    20,     0,
      80,    81,    82,    83,    84,    85,    86,   650,     0,     0,
      68,    69,   232,    70,   136,     0,     0,     0,   143,     0,
     469,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,  1086,     0,     0,   517,     0,     0,     0,
       0,   286,     0,     0,     0,     0,   115,     0,     0,     0,
       0,     0,     0,    21,    22,     0,    23,    24,    25,    71,
      26,    27,    28,    29,    30,    31,    32,    33,    34,     0,
    1087,  1088,     0,     0,     0,    72,     0,    35,     0,     0,
      68,    69,     0,    70,    73,     0,     0,     0,   143,     0,
       0,     0,    74,     0,    14,     0,     0,     0,     0,    75,
      76,    77,    78,     0,     0,    79,   643,   286,   631,   644,
       0,   632,   633,     0,     0,     0,   115,     0,     0,  1554,
       0,     0,   651,     0,     0,     0,  1555,     0,     0,    71,
       0,     0,    80,    81,    82,    83,    84,    85,    86,     0,
       0,     0,     0,     0,     0,    72,     0,     0,     0,     0,
      68,    69,     0,    70,    73,     0,     0,     0,   143,     0,
       0,     0,    74,     0,    14,     0,   169,   170,   171,    75,
      76,    77,    78,  1418,   652,    79,     0,   653,     0,    28,
      29,    30,    31,    32,    33,    34,   115,     0,   645,  1300,
       0,     0,     0,     0,    35,     0,  1301,     0,     0,    71,
       0,     0,    80,    81,    82,    83,    84,    85,    86,     0,
       0,     0,     0,     0,     0,    72,     0,     0,     0,     0,
      68,    69,     0,    70,    73,     0,     0,     0,   143,     0,
       0,     0,    74,     0,     0,     0,   169,   170,   171,    75,
      76,    77,    78,     0,     0,    79,     0,     0,     0,    28,
      29,    30,    31,    32,    33,    34,   115,     0,  1186,     0,
       0,     0,     0,     0,    35,     0,  1416,     0,     0,    71,
       0,     0,    80,    81,    82,    83,    84,    85,    86,     0,
       0,     0,     0,     0,     0,    72,     0,     0,     0,     0,
       0,     0,    68,    69,    73,    70,     0,     0,     0,     0,
     143,   232,    74,     0,     0,   172,   169,   170,   171,    75,
      76,    77,    78,     0,     0,    79,  1239,  1240,  1241,     0,
    1242,  1243,  1244,  1245,     0,  1246,  1247,   209,   115,  1248,
    1249,  1250,  1251,     0,     0,     0,     0,     0,  1252,     0,
       0,    71,    80,    81,    82,    83,    84,    85,    86,     0,
       0,     0,     0,     0,     0,     0,     0,    72,     0,     0,
       0,     0,    68,    69,     0,   870,    73,     0,     0,     0,
     143,     0,  1556,     0,    74,   172,   169,   170,   171,     0,
       0,    75,    76,    77,    78,     0,     0,    79,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,   115,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,    71,     0,     0,    80,    81,    82,    83,    84,    85,
      86,     0,     0,     0,     0,     0,     0,    72,     0,     0,
       0,     0,     0,     0,     0,     0,    73,     0,     0,     0,
       0,     0,     0,     0,    74,   172,     0,     0,   169,   170,
     171,    75,    76,    77,    78,     0,   144,    79,     0,     0,
     145,   146,   147,   148,   149,     0,   150,   151,   152,   153,
       0,   154,   155,     0,     0,   156,   157,   158,   159,     0,
       0,     0,   160,   161,    80,    81,    82,    83,    84,    85,
      86,   162,     3,   163,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,   164,   165,
     249,     0,     0,     0,     0,   172,     0,     0,   169,   170,
     171,   604,   144,   605,     0,     0,   145,   146,   147,   148,
     149,     0,   150,   151,   152,   153,     0,   154,   155,     0,
       0,   156,   157,   158,   159,   167,     0,   115,   160,   161,
       0,     0,    68,     0,     0,    70,     0,   162,     0,   163,
       0,     0,     0,     0,     0,     3,     0,     0,     0,     0,
       0,     0,     0,     0,   164,   165,   249,   282,   144,   283,
       0,     0,   145,   146,   147,   148,   149,   172,   150,   151,
     152,   153,     0,   154,   155,     0,     0,   156,   157,   158,
     159,    71,     0,     0,   160,   161,     0,     0,     0,     0,
       0,   167,     0,   162,     0,   163,     0,    72,     0,     0,
       0,     0,     0,     0,     0,     0,    73,     0,     0,     0,
     164,   165,   249,     0,    74,     0,     0,     0,     0,     0,
       0,    75,    76,    77,    78,     0,     0,    79,  1177,     0,
    1178,  1179,     0,     0,     0,     0,     0,   172,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   167,     0,    11,
      12,    13,    14,     0,    80,    81,    82,    83,    84,    85,
      86,     0,     0,     0,     0,     0,     0,     0,     0,     0,
     363,   364,   365,   366,   367,   368,   369,   370,   371,   372,
     373,   374,   375,     0,     0,     0,     0,     8,     0,     0,
       0,   376,   377,   378,   379,   380,   381,     0,     0,     0,
       0,     0,     0,     0,     0,     9,    10,     0,     0,     0,
       0,     0,     0,     0,    68,     0,     0,    70,     0,     0,
     742,     0,    11,    12,    13,    14,     0,     3,     0,   173,
       0,     0,     0,   382,     0,     0,     0,    28,    29,    30,
      31,    32,    33,    34,     0,     0,  1180,   383,     0,     0,
       0,     0,    35,     0,   144,     0,     0,     0,     0,     0,
     147,   148,   149,    71,   150,   151,   152,   153,     0,   154,
     155,     0,     0,   156,   157,   158,   159,     0,     0,    72,
    1304,   161,   384,   385,     0,     0,     0,     0,    73,     0,
       0,     0,     0,     0,     0,   173,    74,     0,     0,     0,
       0,     0,     0,    75,    76,    77,    78,     0,     0,    79,
      28,    29,    30,    31,    32,    33,    34,     0,   386,   387,
       0,     0,     0,     0,     0,    35,     0,     0,     0,     0,
    1305,     0,     0,     0,     0,     0,    80,    81,    82,    83,
      84,    85,    86,     0,     0,     0,     0,     0,     0,     0,
    1306,   173,   363,   364,   365,   366,   367,   368,   369,   370,
     371,   372,   373,   374,   375,     0,     0,     0,     0,     8,
       0,     0,     0,   376,   377,   378,   379,   380,   381,     0,
       0,     0,     0,     0,     0,     0,    68,     9,    10,    70,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     3,
       0,     0,     0,     0,    11,    12,    13,    14,     0,     0,
       0,     0,   877,   878,   879,   382,   880,   881,   882,   883,
       0,   884,   885,   209,     0,   886,   887,   888,   889,   383,
       0,     0,   890,   891,     0,    71,    94,    95,    96,    97,
      98,    99,   100,   101,   102,   103,   104,   105,   106,   107,
     108,    72,     0,     0,     0,     0,     0,     0,     0,     0,
      73,     0,     0,     0,   384,   385,     0,     0,    74,     0,
       0,     0,     0,     0,     0,    75,    76,    77,    78,     0,
       0,    79,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,    28,    29,    30,    31,    32,    33,    34,     0,
     386,   759,   892,     0,     0,     0,     0,    35,    80,    81,
      82,    83,    84,    85,    86,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   363,   364,   365,   366,   367,   368,
     369,   370,   371,   372,   373,   374,   375,     0,     0,     0,
       0,     8,     0,     0,     0,   376,   377,   378,   379,   380,
     381,     0,     0,     0,     0,     0,     0,     0,     0,     9,
      10,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,    11,    12,    13,    14,
       0,     0,     0,     0,     0,     0,     0,   382,     0,     0,
       0,     0,   144,     0,     0,     0,   145,   146,   147,   148,
     149,   383,   150,   151,   152,   153,     0,   154,   155,     0,
       0,   156,   157,   158,   159,   452,     0,     0,   160,   161,
       0,     0,     0,     0,     0,     0,     0,   162,     0,   163,
       0,     0,     0,     0,     0,     0,   384,   385,     0,     0,
       0,     0,     0,     0,   164,   165,   249,     0,   453,     0,
     454,   455,   456,   457,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,    28,    29,    30,    31,    32,    33,
      34,     0,   386,   946,     0,     0,     0,     0,     0,    35,
       0,   167,     0,     0,     0,     0,     0,  1549,     0,   458,
     459,   460,   461,     3,     0,   462,     0,     0,   144,   463,
     464,   465,   145,   146,   147,   148,   149,     0,   150,   151,
     152,   153,     0,   154,   155,     0,     0,   156,   157,   158,
     159,     0,     0,     0,   160,   161,     0,     0,     0,     0,
       0,     0,     0,   162,     0,   163,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
     164,   165,   249,   144,     0,     0,     0,   145,   146,   147,
     148,   149,     0,   150,   151,   152,   153,     0,   154,   155,
       0,     0,   156,   157,   158,   159,     0,     0,     0,   160,
     161,     0,     0,     0,     0,     0,     0,   167,   162,     0,
     163,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   466,     0,   164,   165,   249,     0,     0,
    1037,  1038,     0,  1039,  1040,  1041,  1042,  1043,  1044,     0,
    1045,  1046,     0,  1047,  1048,  1049,  1050,  1051,     0,     0,
       0,     4,     5,     6,     7,     8,     0,     0,     0,     0,
       0,     0,   167,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     9,    10,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
      11,    12,    13,    14,     0,     0,     0,    15,    16,     0,
       0,     0,     0,    17,     0,   173,    18,     0,     0,     0,
       0,     0,     0,    19,    20,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,   877,   878,   879,     0,
     880,   881,   882,   883,     0,   884,   885,   209,     0,   886,
     887,   888,   889,     0,     0,     3,   890,   891,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,    68,    21,
      22,    70,    23,    24,    25,     0,    26,    27,    28,    29,
      30,    31,    32,    33,    34,     0,     0,   526,     0,     0,
       0,     0,     0,    35,   538,     0,     0,     3,     0,     0,
       0,     0,   898,     0,     0,     0,     0,     0,     0,     0,
       0,   173,     0,     0,     0,     0,   892,    71,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,    72,     0,     0,     0,     0,     0,     0,
    1052,  1053,    73,  1054,  1055,  1056,   538,  1057,  1058,     0,
      74,  1059,  1060,     0,  1061,     0,     0,    75,    76,    77,
      78,     0,     0,    79,     0,     0,   173,  1062,  1063,  1064,
    1065,  1066,  1067,  1068,  1069,  1070,  1071,  1072,  1073,  1074,
    1075,  1076,     0,   539,     0,     6,     7,     8,     0,     0,
      80,    81,    82,    83,    84,    85,    86,   540,     0,     0,
       0,     0,   541,     0,     0,     9,    10,     0,     0,     0,
       0,     0,     0,     0,     0,  1077,     0,     0,     0,     0,
       0,     0,    11,    12,    13,    14,     0,   542,   543,     0,
       0,     0,     0,     0,     0,   539,     0,     6,     7,     8,
       0,     0,     0,     0,     0,     0,     0,   544,     0,   540,
       0,     0,     0,     0,   541,     0,     0,     9,    10,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,    11,    12,    13,    14,     0,   542,
     543,     0,   545,   546,     0,     0,     0,    28,    29,    30,
      31,    32,    33,    34,     0,     0,     0,     0,     0,   544,
       0,     0,    35,     0,     0,     0,     0,     0,     0,     0,
      28,    29,    30,    31,    32,    33,    34,     0,     0,   547,
       0,     0,     0,     0,     0,    35,     0,     0,     0,     0,
       0,   818,     0,     0,   545,   546,   819,   820,     0,   821,
     822,   823,   824,   825,   826,     0,   827,   828,     0,   829,
     830,   831,   832,   833,     0,     0,     0,    68,    69,     0,
      70,     0,    28,    29,    30,    31,    32,    33,    34,     0,
     818,  1166,     0,     0,     0,   819,   820,    35,   821,   822,
     823,   824,   825,   826,     0,   827,   828,     0,   829,   830,
     831,   832,   833,     0,     0,   834,     0,   835,     0,     0,
       0,     0,     0,   836,     0,     0,    71,     0,     0,     0,
       0,     0,    68,    69,     0,    70,     0,     0,     0,     0,
     837,     0,    72,     0,     0,     0,     0,     0,     0,     0,
       0,    73,     0,     0,   834,     0,   835,     0,     0,    74,
       0,     0,   836,     0,     0,     0,    75,    76,    77,    78,
       0,     0,    79,   838,   257,   258,   259,     0,     0,   837,
       0,    71,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,    72,     0,   260,
      81,    82,    83,    84,    85,    86,    73,     0,     0,     0,
       0,     0,   838,     0,    74,     0,     0,     0,     0,     0,
       0,    75,    76,    77,    78,     0,     0,    79,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,    80,    81,    82,    83,    84,    85,
      86,     0,     0,   839,     0,   840,   841,   842,   843,   844,
     845,   846,   847,   848,   849,   850,   851,   852,   853,   854,
     855,   856,     0,    68,    69,   857,    70,     0,     0,     0,
       0,     0,     0,   261,   858,   262,   263,   264,   265,     0,
       0,     0,   839,     0,   840,   841,   842,   843,   844,   845,
     846,   847,   848,   849,   850,   851,   852,   853,   854,   855,
     856,     0,     0,     0,   857,     0,   859,     0,     0,     0,
       0,     0,    71,   858,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   134,     0,     0,     0,    72,    68,
      69,     0,    70,     0,     0,     0,     0,    73,     0,     0,
       0,     0,     0,     0,     0,    74,     0,     0,     0,     0,
       0,     0,    75,    76,    77,    78,     0,     0,    79,     0,
       0,     0,     0,   315,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,    71,     0,
       0,     0,     0,     0,     0,    80,    81,    82,    83,    84,
      85,    86,   316,     0,    72,     0,   317,     0,     0,   318,
     319,     0,     0,    73,   320,   321,   322,   323,   324,   325,
     326,   327,   328,   329,   330,   331,   332,     0,    75,    76,
      77,    78,     0,   333,    79,    68,    69,   334,    70,     0,
       0,     0,     0,     0,   335,     0,     0,     0,     0,    68,
      69,     0,    70,   336,     0,     0,     0,     0,     0,     0,
       0,    80,    81,    82,    83,    84,    85,    86,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   254,     0,     0,
       0,     0,     0,     0,    71,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,    71,     0,
      72,     0,     0,   445,     0,   446,   447,     0,     0,    73,
       0,     0,   448,     0,    72,    68,    69,    74,    70,   267,
     268,     0,     0,    73,    75,    76,    77,    78,     0,     0,
      79,    74,     0,     0,    68,    69,     0,    70,    75,    76,
      77,    78,     0,     0,    79,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,    80,    81,    82,
      83,    84,    85,    86,    71,     0,     0,     0,     0,     0,
       0,    80,    81,    82,    83,    84,    85,    86,     0,     0,
      72,    68,    69,    71,    70,   136,     0,     0,     0,    73,
       0,     0,     0,     0,     0,     0,     0,    74,     0,    72,
       0,     0,     0,     0,    75,    76,    77,    78,    73,     0,
      79,     0,     0,     0,     0,     0,    74,     0,     0,     0,
       0,     0,     0,    75,    76,    77,    78,     0,     0,    79,
      71,   485,   486,     0,     0,     0,     0,    80,    81,    82,
      83,    84,    85,    86,     0,    68,    72,     0,    70,   136,
       0,     0,     0,     0,     0,    73,    80,    81,    82,    83,
      84,    85,    86,    74,     0,     0,     0,     0,     0,     0,
      75,    76,    77,    78,     0,     0,    79,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,    71,     0,     0,     0,     0,     0,
       0,     0,     0,    80,    81,    82,    83,    84,    85,    86,
      72,     0,     0,     0,     0,     0,     0,     0,     0,    73,
       0,     0,     0,     0,     0,     0,     0,    74,     0,     0,
       0,     0,     0,     0,    75,    76,    77,    78,     0,     0,
      79,     0,   342,   114,     0,     0,     0,     0,   116,     0,
     117,     0,     0,     0,     0,     0,     0,   118,     0,     0,
       0,     0,     0,     0,     0,     0,     0,    80,    81,    82,
      83,    84,    85,    86,   119,   343,     0,   344,   345,   346,
     347,   348,     0,     0,     0,     0,   349,     0,     0,   120,
       0,     0,     0,     0,     0,     0,   350,     0,     0,     0,
       0,   351,     0,     0,   352,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,   353,   354,   355,   356,
     357,   358,   359,   360,     0,     0,     0,     0,     0,   361
};

static const yytype_int16 yycheck[] =
{
       5,   109,    25,    25,    25,   119,   120,   427,   110,   429,
     179,   341,     1,    57,   694,    20,   976,    14,     1,   341,
     341,     1,     1,    28,    29,    30,    31,   489,    25,     1,
    1007,    21,    22,     1,   740,  1102,   679,   753,    25,  1130,
       9,   502,   503,   504,     7,    23,    24,   201,     7,     7,
       8,    43,    93,     9,    19,     7,    48,   204,   205,   206,
     207,   208,     7,     7,    20,   179,     9,    59,     9,    10,
      60,     9,   752,     8,    33,     9,   112,     8,   138,    56,
       9,    10,    11,   185,     9,    10,   200,     9,     9,   197,
      11,     9,    10,     9,     9,    65,    66,     9,    12,    89,
      20,   124,    31,    32,     8,   168,     9,     9,     9,     9,
      31,    32,     9,    44,   228,     9,    63,    63,   138,   217,
    1459,  1097,   145,   199,   121,   130,  1465,   132,   133,   134,
       9,    10,    11,  1479,   121,   300,     8,   395,   128,   129,
     162,  1467,   250,   165,     8,   168,   248,   138,    75,    76,
      77,    64,    31,    32,   229,     7,   299,  1466,  1497,     9,
     201,  1464,   184,   138,    40,   300,   210,   172,   173,    45,
      20,    47,    44,  1499,   249,   172,   284,   296,    54,   276,
     299,    33,   179,   173,   900,   901,   113,   114,   115,  1498,
     295,   196,   293,  1496,     9,    71,    72,   302,   914,    23,
     301,   224,    26,    27,    28,    29,    11,    31,    32,    33,
      86,   294,    39,   674,   127,   217,   221,   222,   301,   273,
     296,   226,   214,   291,    51,    52,    31,    32,   295,   226,
       7,   339,   295,   293,   301,   112,    88,   406,    12,   700,
     294,   291,    69,   119,   398,   250,   300,   237,   238,    24,
      25,   256,   291,   307,   290,   277,    60,   296,  1604,    34,
      23,   295,   942,    26,    27,    28,    29,   301,    31,    32,
      33,    60,    61,   296,   293,   295,   992,   385,   297,   284,
     285,   304,  1258,   217,   289,   295,   308,   292,   293,   294,
     970,   301,   297,   298,   121,   291,   410,   302,   556,   289,
     408,   107,   108,   109,   295,     9,   300,   409,   294,   252,
     253,   254,   306,   303,   291,   291,   291,   107,   108,   109,
     295,   295,    26,    27,    28,   314,   299,   297,   291,   291,
     295,   314,   138,   296,   314,   314,   294,   306,   291,   291,
     299,   291,   314,   308,   296,   301,   314,   305,   295,   295,
     811,   295,   341,    51,   299,   293,   297,   398,   341,   294,
     401,   341,   341,   301,   405,   409,   297,   475,   297,   341,
       9,    10,   297,   341,  1351,   297,   297,   297,   292,   297,
     138,   297,   297,  1474,   407,   297,   376,   296,   378,   296,
     294,   300,   382,   434,   297,   297,   297,   297,   294,   295,
     297,   293,   510,   297,   865,   513,   514,   299,   398,   406,
     407,   401,   402,   290,   411,   405,   296,   296,   296,  1510,
     300,   413,   576,   300,   411,   403,   404,  1494,   418,   419,
     584,   421,    14,   423,   295,    17,   296,   227,   546,   138,
     301,   903,    25,    26,    27,    28,    29,   425,   471,   439,
     440,   138,   441,   442,   443,   444,  1099,   571,    40,    41,
     562,   441,   467,    45,   469,    47,   293,   291,   294,   295,
     578,   138,    54,   441,   442,   443,   444,   304,   107,   108,
     109,  1187,   487,   307,   481,   308,   107,   108,   109,    71,
     487,   496,   294,   295,   300,    12,   302,   487,   297,   607,
     306,   307,   291,   296,    86,   510,   295,   300,  1485,   297,
     300,    39,   302,   302,   616,   296,   306,   307,    46,   300,
      48,    49,    50,   585,   586,   587,   296,    75,    76,    77,
     300,   633,   297,    28,    29,   576,     9,   527,    11,   306,
     563,    65,    66,   584,   307,   107,   108,   109,   140,   141,
      61,   296,   542,   543,  1514,   300,   140,   141,    31,    32,
      88,    89,    90,   568,   569,   113,   114,   115,    63,   293,
     294,   561,    46,   614,    48,    49,    50,   567,   296,    40,
     570,   625,   300,   737,    45,   296,    47,   296,    18,   612,
     297,   293,   294,    54,   584,   297,   296,  1313,  1314,   296,
     300,   296,   607,   300,     9,    10,    11,   135,   136,   137,
      71,   139,   726,   943,   142,   723,   724,   725,   732,   611,
     301,   943,   943,   299,   614,    86,    31,    32,    25,    26,
      27,    28,    29,    55,   293,    57,    58,    59,   785,     8,
     787,   788,   789,   790,   791,   297,   296,   652,   640,   641,
     300,   296,   301,   658,   646,   300,   648,   222,   680,   106,
      25,   651,   296,   301,   111,  1345,   300,   296,   691,   301,
     296,   300,   296,   302,   300,   777,   300,   306,   307,   300,
     670,   302,   296,   296,   296,   306,   307,   300,   711,   694,
     292,   296,   697,   296,   699,   300,   737,   300,  1118,   296,
    1120,   690,     5,     6,   696,     8,     9,   296,  1424,   204,
     205,   300,   207,   300,   296,  1431,   721,   707,   708,   296,
     725,   762,   296,   300,   729,   730,   731,   296,   751,   296,
     771,   300,   755,   300,   305,   291,   296,   296,   300,   295,
     302,   300,   295,   296,   306,   307,  1426,   752,   297,   293,
     296,    54,   742,   297,   744,   296,   746,   296,   748,   300,
     296,   300,   296,   292,   866,   873,   300,    70,   198,   297,
     295,   293,   762,   875,   299,   297,    79,   296,   208,   293,
     294,   211,   772,   773,    87,    26,    27,    28,    29,   993,
     994,    94,    95,    96,    97,   294,   774,   100,    26,    27,
      28,    29,  1518,   296,   794,     7,   796,   797,   291,   814,
       7,   816,   852,   853,   294,   923,    26,    27,    28,    29,
     810,   297,   863,   302,   127,   128,   129,   130,   131,   132,
     133,   297,    19,   210,    19,   303,   283,   284,   285,   286,
     287,   288,   289,  1559,   866,   292,    20,   949,   294,   294,
     112,   298,   291,   283,   284,   285,   286,   287,   288,   289,
     291,   305,   292,   291,   291,   906,    19,   975,   298,   977,
     296,    62,    62,   296,   214,  1591,   866,   296,   296,   299,
    1560,   297,   297,   297,   301,   297,   297,   297,   297,    39,
    1606,   297,   305,   296,   217,   299,    46,   299,    48,    49,
      50,   217,   297,   294,   896,   894,   299,   928,   898,   292,
     294,   299,   935,     8,   292,   296,   296,   296,   941,    19,
     925,    18,   297,  1603,   297,   106,   297,   932,   296,  1031,
     111,   921,   294,   303,   297,   301,   297,   942,    88,    89,
      90,   301,     5,     6,    19,     8,   297,   937,   294,   939,
     294,   300,   993,   994,   943,   944,   945,   296,   947,   297,
     943,   944,   945,   943,   943,   970,   296,   296,   305,   297,
     297,   943,   944,   945,   296,   943,   944,   945,   297,   947,
     297,   296,  1084,   297,   989,   135,   136,   137,   244,   139,
     236,    54,   142,   248,   296,    22,   297,   292,  1167,  1168,
    1169,  1170,   299,   993,   994,   299,   309,    70,  1177,  1178,
    1179,   291,   303,   297,   198,   296,    79,   305,   305,   301,
     138,   296,   301,   301,    87,   301,   297,   301,  1018,   301,
      20,    94,    95,    96,    97,     8,    62,   100,    62,   297,
     297,   300,   251,   297,   107,   108,   109,   300,   296,   296,
     301,   291,   297,   297,   297,  1163,  1164,   296,   296,   168,
     297,   297,   297,   294,   127,   128,   129,   130,   131,   132,
     133,   301,   301,   294,   171,   172,   173,   174,   257,   302,
      19,   297,   299,   305,  1504,  1505,   301,   292,   296,   296,
     296,  1081,   296,   296,     8,   296,   296,   296,   195,   196,
     197,   198,   283,   284,   285,   286,   287,   288,   289,   297,
     296,   292,  1134,  1103,  1104,  1105,   296,   298,   296,   296,
    1110,  1111,  1112,  1113,  1114,  1115,   301,  1117,  1118,  1119,
    1232,  1121,  1122,  1123,  1124,  1125,  1126,  1127,  1116,  1129,
     297,  1131,  1120,  1133,   303,  1135,   301,   297,   297,   106,
    1128,   297,   294,   301,   111,  1160,   301,  1198,    39,   297,
    1157,   301,    18,   297,  1205,    46,  1207,    48,    49,    50,
    1167,  1168,  1169,  1170,   301,   297,   297,   301,   297,    19,
    1177,  1178,  1179,   297,   301,   297,   283,   284,   285,   286,
     287,   288,   289,   301,   297,   292,   301,   301,   297,  1189,
     301,   298,     8,   297,   297,   301,   297,    88,    89,    90,
     297,  1319,   301,  1327,   297,   297,   301,   297,  1208,   297,
     296,   305,   301,     5,     6,   296,     8,  1217,   301,   301,
     296,   296,   296,   301,  1224,  1225,  1344,   300,   297,   302,
     297,   296,   296,   306,   307,   305,  1236,   305,   297,   297,
     301,  1353,   297,   296,   135,   136,   137,   301,   139,    41,
     297,   142,   301,   296,   296,   296,  1256,  1257,   296,     8,
    1259,   296,    54,   296,   296,  1265,  1266,  1267,  1268,  1269,
    1270,   296,  1272,  1305,   296,   296,   296,   296,    70,   297,
      19,   297,   297,  1271,   301,     8,   297,    79,   302,   296,
     296,   301,  1307,   305,   296,    87,   297,   296,   257,   297,
     296,   296,    94,    95,    96,    97,   300,  1425,   100,   297,
     297,   297,   178,   179,   105,   297,   283,   284,   285,   286,
     287,   288,   289,   297,   296,   296,    19,   297,   297,   297,
    1345,   298,   198,   296,   195,   127,   128,   129,   130,   131,
     132,   133,   308,    25,   210,   481,   212,  1197,   411,   215,
     216,   720,  1352,  1154,  1156,    25,  1196,   734,   362,  1359,
     980,   582,  1480,   625,  1084,    57,     5,     6,   691,     8,
     861,  1370,   903,   982,    13,  1487,  1439,    16,  1139,   873,
     708,    20,    21,    22,    23,    24,   868,    26,    27,    28,
      29,    10,    31,    32,  1427,  1148,    35,    36,    37,    38,
     131,   544,    41,    42,    43,    -1,   297,   738,    -1,   493,
      -1,  1426,    51,   442,    53,    54,    -1,   283,   284,   285,
     286,   287,   288,   289,    -1,    -1,   292,   219,    -1,    68,
      69,    70,   298,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      79,    -1,    -1,  1443,  1444,  1445,    -1,    -1,    87,    -1,
      -1,  1563,    -1,    -1,    -1,    94,    95,    96,    97,    -1,
      -1,   100,    -1,    -1,    -1,    -1,   105,    -1,   107,   108,
     109,    -1,    -1,  1597,    -1,    -1,    -1,    -1,    -1,    -1,
    1512,    -1,    -1,  1601,    -1,    -1,    -1,    -1,   127,   128,
     129,   130,   131,   132,   133,  1495,    -1,    -1,    -1,    -1,
    1500,  1501,  1502,    -1,  1504,    -1,  1506,  1507,  1508,    -1,
      -1,    -1,    -1,    -1,   306,    -1,    -1,  1505,    -1,    -1,
      -1,    -1,   161,   162,   163,    -1,    -1,    -1,  1528,  1529,
    1530,    -1,     5,     6,    -1,     8,    -1,    -1,    -1,    -1,
      13,    -1,    -1,    16,    -1,  1560,    -1,    20,    21,    22,
      23,    24,    -1,    26,    27,    28,    29,    -1,    31,    32,
      -1,    -1,    35,    36,    37,    38,    -1,  1600,    41,    42,
      43,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    51,    -1,
      53,    54,    -1,    -1,    -1,    -1,    39,    -1,  1603,    -1,
      -1,    -1,  1592,    -1,    -1,    68,    69,    70,    -1,    -1,
      -1,    54,    -1,    -1,    -1,    -1,    79,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    87,    -1,    -1,    70,    -1,    -1,
      -1,    94,    95,    96,    97,    -1,    79,   100,    -1,    -1,
      -1,    -1,   105,    -1,   107,   108,   109,    -1,    -1,    -1,
      -1,    94,    95,    96,    97,    -1,    -1,   100,   121,   122,
      -1,    -1,    -1,    -1,   127,   128,   129,   130,   131,   132,
     133,   300,    -1,   302,    -1,    -1,    -1,   306,   307,     5,
     309,    -1,     8,     9,   127,   128,   129,   130,   131,   132,
     133,    -1,    -1,     5,     6,    -1,     8,    -1,   161,   162,
     163,    13,    -1,    -1,    16,    -1,    -1,    -1,    20,    21,
      22,    23,    24,    -1,    26,    27,    28,    29,    -1,    31,
      32,    -1,    -1,    35,    36,    37,    38,    -1,    54,    41,
      42,    43,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    51,
      -1,    53,    54,    -1,    70,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    79,    -1,    -1,    68,    69,    70,    -1,
      -1,    87,    -1,    -1,    -1,    -1,    -1,    79,    94,    95,
      96,    97,    -1,    -1,   100,    87,    -1,    -1,    -1,    -1,
      -1,    -1,    94,    95,    96,    97,    -1,    -1,   100,    -1,
      -1,    18,    -1,   105,    -1,   107,   108,   109,    -1,    -1,
      -1,   127,   128,   129,   130,   131,   132,   133,    -1,   121,
     122,    -1,    -1,    -1,    41,   127,   128,   129,   130,   131,
     132,   133,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,     5,     6,    -1,     8,    -1,   300,    -1,   302,
      -1,    -1,    -1,   306,   307,    -1,   309,    -1,   291,   161,
     162,   163,     5,     6,    -1,     8,    -1,    -1,    -1,    -1,
      13,    -1,    -1,    16,    -1,    -1,    -1,    20,    21,    22,
      23,    24,    -1,    26,    27,    28,    29,    -1,    31,    32,
      -1,    54,    35,    36,    37,    38,    -1,    -1,    41,    42,
      43,    -1,    -1,    -1,    -1,    -1,    -1,    70,    51,    -1,
      53,    54,    -1,    -1,    -1,    -1,    79,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    87,    68,    69,    70,    -1,    -1,
      -1,    94,    95,    96,    97,    -1,    79,   100,    -1,   156,
      -1,    -1,    -1,    -1,    87,    -1,    -1,    -1,    -1,    -1,
      -1,    94,    95,    96,    97,    -1,    -1,   100,    -1,    -1,
      -1,    -1,   105,   106,   127,   128,   129,   130,   131,   132,
     133,    -1,    -1,    -1,    -1,   291,    -1,    -1,    -1,    -1,
      -1,   198,    -1,    -1,   127,   128,   129,   130,   131,   132,
     133,   208,    -1,    -1,   211,    -1,    -1,    -1,   300,    -1,
     302,    -1,    -1,    -1,   306,   307,    -1,   309,    -1,    -1,
      -1,    -1,    -1,     5,     6,    -1,     8,    -1,   161,   162,
     163,    13,    -1,    -1,    16,    -1,    -1,    -1,    20,    21,
      22,    23,    24,    -1,    26,    27,    28,    29,    -1,    31,
      32,    -1,    -1,    35,    36,    37,    38,    -1,    -1,    41,
      42,    43,    -1,    -1,    -1,   198,    -1,    14,    -1,    51,
      17,    53,    54,    -1,    -1,    -1,   283,   284,   285,   286,
     287,   288,   289,    -1,    -1,   292,    68,    69,    70,    -1,
      72,   298,    -1,    40,    41,    -1,    -1,    79,    45,    -1,
      47,    -1,    -1,    -1,    -1,    87,    -1,    54,    -1,    -1,
      -1,    -1,    94,    95,    96,    97,    -1,    -1,   100,    -1,
      -1,    -1,    -1,   105,    71,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   119,    -1,    86,
      -1,    -1,    -1,   296,    -1,   127,   128,   129,   130,   131,
     132,   133,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
       5,     6,    -1,     8,    -1,    -1,    -1,   300,    -1,    -1,
      -1,    -1,    -1,    -1,     5,     6,   309,     8,    -1,   161,
     162,   163,    13,    -1,    -1,    16,    -1,    -1,    -1,    20,
      21,    22,    23,    24,    -1,    26,    27,    28,    29,    -1,
      31,    32,    -1,    -1,    35,    36,    37,    38,    -1,    54,
      41,    42,    43,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      51,    -1,    53,    54,    -1,    70,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    79,    -1,    -1,    68,    69,    70,
      -1,    -1,    87,    -1,    -1,    -1,    -1,    -1,    79,    94,
      95,    96,    97,    -1,    -1,   100,    87,    -1,    -1,    -1,
      -1,    -1,    -1,    94,    95,    96,    97,    -1,    -1,   100,
      -1,    -1,    -1,    -1,   105,   106,    -1,    -1,    -1,    -1,
      -1,    -1,   127,   128,   129,   130,   131,   132,   133,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   127,   128,   129,   130,
     131,   132,   133,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,     5,     6,    -1,     8,    -1,    -1,    -1,   300,    -1,
      -1,    -1,    -1,    -1,    -1,     5,     6,   309,     8,    -1,
     161,   162,   163,    13,    -1,    -1,    16,    -1,    -1,    -1,
      20,    21,    22,    23,    24,    -1,    26,    27,    28,    29,
      -1,    31,    32,    -1,    -1,    35,    36,    37,    38,    -1,
      54,    41,    42,    43,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    51,    -1,    53,    54,    -1,    70,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    79,    -1,    -1,    68,    69,
      70,    -1,    -1,    87,    -1,    -1,    -1,    -1,    -1,    79,
      94,    95,    96,    97,    -1,    -1,   100,    87,    -1,    -1,
      -1,    -1,    -1,    -1,    94,    95,    96,    97,    -1,    -1,
     100,    -1,    -1,    -1,    -1,   105,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   127,   128,   129,   130,   131,   132,   133,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   127,   128,   129,
     130,   131,   132,   133,    -1,   300,    -1,    -1,    -1,    -1,
      -1,    -1,     5,     6,    -1,     8,    -1,    -1,    -1,   300,
      -1,    -1,    -1,    -1,    -1,    -1,     5,     6,   309,     8,
      -1,   161,   162,   163,    13,    -1,    -1,    16,    -1,    -1,
      -1,    20,    21,    22,    23,    24,    -1,    26,    27,    28,
      29,    -1,    31,    32,    -1,    -1,    35,    36,    37,    38,
      -1,    54,    41,    42,    43,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    51,    -1,    53,    54,    -1,    70,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    79,    -1,    -1,    68,
      69,    70,    -1,    -1,    87,    -1,    -1,    -1,    -1,    -1,
      79,    94,    95,    96,    97,    -1,    -1,   100,    87,    -1,
      -1,    -1,    -1,    -1,    -1,    94,    95,    96,    97,    -1,
      -1,   100,    -1,    -1,    -1,    -1,   105,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   127,   128,   129,   130,   131,   132,
     133,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   127,   128,
     129,   130,   131,   132,   133,    -1,   300,    -1,    -1,    -1,
      -1,    -1,    -1,     5,     6,    -1,     8,    -1,    -1,    -1,
     300,    -1,    -1,    -1,    -1,    -1,    -1,     5,     6,   309,
       8,    -1,   161,   162,   163,    13,    -1,    -1,    16,    -1,
      -1,    -1,    20,    21,    22,    23,    24,    -1,    26,    27,
      28,    29,    -1,    31,    32,    -1,    -1,    35,    36,    37,
      38,    -1,    54,    41,    42,    43,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    51,    -1,    53,    54,    -1,    70,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    79,    -1,    -1,
      68,    69,    70,    -1,    -1,    87,    -1,    -1,    -1,    -1,
      -1,    79,    94,    95,    96,    97,    -1,    -1,   100,    87,
      -1,    -1,    -1,    -1,    -1,    -1,    94,    95,    96,    97,
      -1,    -1,   100,    -1,    -1,    -1,    -1,   105,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   127,   128,   129,   130,   131,
     132,   133,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   127,
     128,   129,   130,   131,   132,   133,    -1,   300,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   300,    -1,    -1,    -1,    -1,    -1,    -1,     5,     6,
     309,     8,    -1,   161,   162,   163,    13,    -1,    -1,    16,
      -1,    -1,    -1,    20,    21,    22,    23,    24,    -1,    26,
      27,    28,    29,    -1,    31,    32,    -1,    -1,    35,    36,
      37,    38,    -1,    -1,    41,    42,    43,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    51,    -1,    53,    54,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    68,    69,    70,    -1,    -1,    -1,    -1,    -1,    -1,
       5,     6,    79,     8,     9,    -1,    -1,    -1,    -1,    -1,
      87,    -1,    -1,    -1,    -1,    20,    -1,    94,    95,    96,
      97,    -1,    -1,   100,    -1,    -1,    -1,    -1,   105,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    41,    -1,    -1,     0,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    54,
     127,   128,   129,   130,   131,   132,   133,    18,   300,    -1,
      -1,    -1,    -1,    -1,    -1,    70,     5,     6,    -1,     8,
      -1,    -1,   300,    -1,    79,    -1,    -1,    -1,    -1,    -1,
      -1,   309,    87,    -1,   161,   162,   163,    -1,    -1,    94,
      95,    96,    97,    -1,    -1,   100,    -1,    -1,    -1,    -1,
      -1,    -1,    41,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    54,    -1,    -1,    -1,    -1,
      -1,    -1,   127,   128,   129,   130,   131,   132,   133,    -1,
      -1,    70,    -1,    -1,    -1,    -1,    -1,    -1,     5,     6,
      79,     8,    -1,    -1,    -1,    -1,    -1,    -1,    87,    -1,
      -1,    -1,    -1,    -1,    -1,    94,    95,    96,    97,    -1,
      -1,   100,    22,    23,    24,    -1,    26,    27,    28,    29,
      -1,    31,    32,    33,    18,    35,    36,    37,    38,    -1,
      -1,    -1,    42,    43,    -1,    -1,    -1,    54,   127,   128,
     129,   130,   131,   132,   133,   156,   157,   158,   159,   160,
      -1,    -1,    -1,    70,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    79,    -1,   219,    -1,    -1,   178,   179,    -1,
      87,    -1,    -1,   300,    -1,    -1,    -1,    94,    95,    96,
      97,    -1,   309,   100,   195,   196,   197,   198,    -1,    -1,
      84,   202,   203,    -1,    18,    -1,    -1,   208,    -1,    -1,
     211,    -1,   112,    -1,    -1,    -1,    -1,   218,   219,    -1,
     127,   128,   129,   130,   131,   132,   133,    41,    -1,    -1,
       5,     6,     7,     8,     9,    -1,    -1,    -1,    13,    -1,
     219,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    28,    -1,    -1,   301,    -1,    -1,    -1,
      -1,   306,    -1,    -1,    -1,    -1,    41,    -1,    -1,    -1,
      -1,    -1,    -1,   274,   275,    -1,   277,   278,   279,    54,
     281,   282,   283,   284,   285,   286,   287,   288,   289,    -1,
      65,    66,    -1,    -1,    -1,    70,    -1,   298,    -1,    -1,
       5,     6,    -1,     8,    79,    -1,    -1,    -1,    13,    -1,
      -1,    -1,    87,    -1,   198,    -1,    -1,    -1,    -1,    94,
      95,    96,    97,    -1,    -1,   100,   210,   306,   212,   213,
      -1,   215,   216,    -1,    -1,    -1,    41,    -1,    -1,    44,
      -1,    -1,   156,    -1,    -1,    -1,    51,    -1,    -1,    54,
      -1,    -1,   127,   128,   129,   130,   131,   132,   133,    -1,
      -1,    -1,    -1,    -1,    -1,    70,    -1,    -1,    -1,    -1,
       5,     6,    -1,     8,    79,    -1,    -1,    -1,    13,    -1,
      -1,    -1,    87,    -1,   198,    -1,   161,   162,   163,    94,
      95,    96,    97,   300,   208,   100,    -1,   211,    -1,   283,
     284,   285,   286,   287,   288,   289,    41,    -1,   292,    44,
      -1,    -1,    -1,    -1,   298,    -1,    51,    -1,    -1,    54,
      -1,    -1,   127,   128,   129,   130,   131,   132,   133,    -1,
      -1,    -1,    -1,    -1,    -1,    70,    -1,    -1,    -1,    -1,
       5,     6,    -1,     8,    79,    -1,    -1,    -1,    13,    -1,
      -1,    -1,    87,    -1,    -1,    -1,   161,   162,   163,    94,
      95,    96,    97,    -1,    -1,   100,    -1,    -1,    -1,   283,
     284,   285,   286,   287,   288,   289,    41,    -1,   292,    -1,
      -1,    -1,    -1,    -1,   298,    -1,    51,    -1,    -1,    54,
      -1,    -1,   127,   128,   129,   130,   131,   132,   133,    -1,
      -1,    -1,    -1,    -1,    -1,    70,    -1,    -1,    -1,    -1,
      -1,    -1,     5,     6,    79,     8,    -1,    -1,    -1,    -1,
      13,     7,    87,    -1,    -1,   300,   161,   162,   163,    94,
      95,    96,    97,    -1,    -1,   100,    22,    23,    24,    -1,
      26,    27,    28,    29,    -1,    31,    32,    33,    41,    35,
      36,    37,    38,    -1,    -1,    -1,    -1,    -1,    44,    -1,
      -1,    54,   127,   128,   129,   130,   131,   132,   133,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    70,    -1,    -1,
      -1,    -1,     5,     6,    -1,     8,    79,    -1,    -1,    -1,
      13,    -1,   297,    -1,    87,   300,   161,   162,   163,    -1,
      -1,    94,    95,    96,    97,    -1,    -1,   100,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    41,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    54,    -1,    -1,   127,   128,   129,   130,   131,   132,
     133,    -1,    -1,    -1,    -1,    -1,    -1,    70,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    79,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    87,   300,    -1,    -1,   161,   162,
     163,    94,    95,    96,    97,    -1,    16,   100,    -1,    -1,
      20,    21,    22,    23,    24,    -1,    26,    27,    28,    29,
      -1,    31,    32,    -1,    -1,    35,    36,    37,    38,    -1,
      -1,    -1,    42,    43,   127,   128,   129,   130,   131,   132,
     133,    51,    18,    53,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    68,    69,
      70,    -1,    -1,    -1,    -1,   300,    -1,    -1,   161,   162,
     163,    15,    16,    17,    -1,    -1,    20,    21,    22,    23,
      24,    -1,    26,    27,    28,    29,    -1,    31,    32,    -1,
      -1,    35,    36,    37,    38,   105,    -1,    41,    42,    43,
      -1,    -1,     5,    -1,    -1,     8,    -1,    51,    -1,    53,
      -1,    -1,    -1,    -1,    -1,    18,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    68,    69,    70,    15,    16,    17,
      -1,    -1,    20,    21,    22,    23,    24,   300,    26,    27,
      28,    29,    -1,    31,    32,    -1,    -1,    35,    36,    37,
      38,    54,    -1,    -1,    42,    43,    -1,    -1,    -1,    -1,
      -1,   105,    -1,    51,    -1,    53,    -1,    70,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    79,    -1,    -1,    -1,
      68,    69,    70,    -1,    87,    -1,    -1,    -1,    -1,    -1,
      -1,    94,    95,    96,    97,    -1,    -1,   100,   174,    -1,
     176,   177,    -1,    -1,    -1,    -1,    -1,   300,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   105,    -1,   195,
     196,   197,   198,    -1,   127,   128,   129,   130,   131,   132,
     133,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     143,   144,   145,   146,   147,   148,   149,   150,   151,   152,
     153,   154,   155,    -1,    -1,    -1,    -1,   160,    -1,    -1,
      -1,   164,   165,   166,   167,   168,   169,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   178,   179,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,     5,    -1,    -1,     8,    -1,    -1,
     300,    -1,   195,   196,   197,   198,    -1,    18,    -1,   309,
      -1,    -1,    -1,   206,    -1,    -1,    -1,   283,   284,   285,
     286,   287,   288,   289,    -1,    -1,   292,   220,    -1,    -1,
      -1,    -1,   298,    -1,    16,    -1,    -1,    -1,    -1,    -1,
      22,    23,    24,    54,    26,    27,    28,    29,    -1,    31,
      32,    -1,    -1,    35,    36,    37,    38,    -1,    -1,    70,
      42,    43,   255,   256,    -1,    -1,    -1,    -1,    79,    -1,
      -1,    -1,    -1,    -1,    -1,   309,    87,    -1,    -1,    -1,
      -1,    -1,    -1,    94,    95,    96,    97,    -1,    -1,   100,
     283,   284,   285,   286,   287,   288,   289,    -1,   291,   292,
      -1,    -1,    -1,    -1,    -1,   298,    -1,    -1,    -1,    -1,
      92,    -1,    -1,    -1,    -1,    -1,   127,   128,   129,   130,
     131,   132,   133,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     112,   309,   143,   144,   145,   146,   147,   148,   149,   150,
     151,   152,   153,   154,   155,    -1,    -1,    -1,    -1,   160,
      -1,    -1,    -1,   164,   165,   166,   167,   168,   169,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,     5,   178,   179,     8,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    18,
      -1,    -1,    -1,    -1,   195,   196,   197,   198,    -1,    -1,
      -1,    -1,    22,    23,    24,   206,    26,    27,    28,    29,
      -1,    31,    32,    33,    -1,    35,    36,    37,    38,   220,
      -1,    -1,    42,    43,    -1,    54,   180,   181,   182,   183,
     184,   185,   186,   187,   188,   189,   190,   191,   192,   193,
     194,    70,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      79,    -1,    -1,    -1,   255,   256,    -1,    -1,    87,    -1,
      -1,    -1,    -1,    -1,    -1,    94,    95,    96,    97,    -1,
      -1,   100,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   283,   284,   285,   286,   287,   288,   289,    -1,
     291,   292,   112,    -1,    -1,    -1,    -1,   298,   127,   128,
     129,   130,   131,   132,   133,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   143,   144,   145,   146,   147,   148,
     149,   150,   151,   152,   153,   154,   155,    -1,    -1,    -1,
      -1,   160,    -1,    -1,    -1,   164,   165,   166,   167,   168,
     169,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   178,
     179,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   195,   196,   197,   198,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   206,    -1,    -1,
      -1,    -1,    16,    -1,    -1,    -1,    20,    21,    22,    23,
      24,   220,    26,    27,    28,    29,    -1,    31,    32,    -1,
      -1,    35,    36,    37,    38,    39,    -1,    -1,    42,    43,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    51,    -1,    53,
      -1,    -1,    -1,    -1,    -1,    -1,   255,   256,    -1,    -1,
      -1,    -1,    -1,    -1,    68,    69,    70,    -1,    72,    -1,
      74,    75,    76,    77,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   283,   284,   285,   286,   287,   288,
     289,    -1,   291,   292,    -1,    -1,    -1,    -1,    -1,   298,
      -1,   105,    -1,    -1,    -1,    -1,    -1,   297,    -1,   113,
     114,   115,   116,    18,    -1,   119,    -1,    -1,    16,   123,
     124,   125,    20,    21,    22,    23,    24,    -1,    26,    27,
      28,    29,    -1,    31,    32,    -1,    -1,    35,    36,    37,
      38,    -1,    -1,    -1,    42,    43,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    51,    -1,    53,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      68,    69,    70,    16,    -1,    -1,    -1,    20,    21,    22,
      23,    24,    -1,    26,    27,    28,    29,    -1,    31,    32,
      -1,    -1,    35,    36,    37,    38,    -1,    -1,    -1,    42,
      43,    -1,    -1,    -1,    -1,    -1,    -1,   105,    51,    -1,
      53,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   227,    -1,    68,    69,    70,    -1,    -1,
      21,    22,    -1,    24,    25,    26,    27,    28,    29,    -1,
      31,    32,    -1,    34,    35,    36,    37,    38,    -1,    -1,
      -1,   156,   157,   158,   159,   160,    -1,    -1,    -1,    -1,
      -1,    -1,   105,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   178,   179,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     195,   196,   197,   198,    -1,    -1,    -1,   202,   203,    -1,
      -1,    -1,    -1,   208,    -1,   309,   211,    -1,    -1,    -1,
      -1,    -1,    -1,   218,   219,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    22,    23,    24,    -1,
      26,    27,    28,    29,    -1,    31,    32,    33,    -1,    35,
      36,    37,    38,    -1,    -1,    18,    42,    43,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,     5,   274,
     275,     8,   277,   278,   279,    -1,   281,   282,   283,   284,
     285,   286,   287,   288,   289,    -1,    -1,   292,    -1,    -1,
      -1,    -1,    -1,   298,    67,    -1,    -1,    18,    -1,    -1,
      -1,    -1,   300,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   309,    -1,    -1,    -1,    -1,   112,    54,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    70,    -1,    -1,    -1,    -1,    -1,    -1,
     231,   232,    79,   234,   235,   236,    67,   238,   239,    -1,
      87,   242,   243,    -1,   245,    -1,    -1,    94,    95,    96,
      97,    -1,    -1,   100,    -1,    -1,   309,   258,   259,   260,
     261,   262,   263,   264,   265,   266,   267,   268,   269,   270,
     271,   272,    -1,   156,    -1,   158,   159,   160,    -1,    -1,
     127,   128,   129,   130,   131,   132,   133,   170,    -1,    -1,
      -1,    -1,   175,    -1,    -1,   178,   179,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   306,    -1,    -1,    -1,    -1,
      -1,    -1,   195,   196,   197,   198,    -1,   200,   201,    -1,
      -1,    -1,    -1,    -1,    -1,   156,    -1,   158,   159,   160,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   220,    -1,   170,
      -1,    -1,    -1,    -1,   175,    -1,    -1,   178,   179,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   195,   196,   197,   198,    -1,   200,
     201,    -1,   255,   256,    -1,    -1,    -1,   283,   284,   285,
     286,   287,   288,   289,    -1,    -1,    -1,    -1,    -1,   220,
      -1,    -1,   298,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     283,   284,   285,   286,   287,   288,   289,    -1,    -1,   292,
      -1,    -1,    -1,    -1,    -1,   298,    -1,    -1,    -1,    -1,
      -1,    16,    -1,    -1,   255,   256,    21,    22,    -1,    24,
      25,    26,    27,    28,    29,    -1,    31,    32,    -1,    34,
      35,    36,    37,    38,    -1,    -1,    -1,     5,     6,    -1,
       8,    -1,   283,   284,   285,   286,   287,   288,   289,    -1,
      16,   292,    -1,    -1,    -1,    21,    22,   298,    24,    25,
      26,    27,    28,    29,    -1,    31,    32,    -1,    34,    35,
      36,    37,    38,    -1,    -1,    80,    -1,    82,    -1,    -1,
      -1,    -1,    -1,    88,    -1,    -1,    54,    -1,    -1,    -1,
      -1,    -1,     5,     6,    -1,     8,    -1,    -1,    -1,    -1,
     105,    -1,    70,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    79,    -1,    -1,    80,    -1,    82,    -1,    -1,    87,
      -1,    -1,    88,    -1,    -1,    -1,    94,    95,    96,    97,
      -1,    -1,   100,   138,   102,   103,   104,    -1,    -1,   105,
      -1,    54,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    70,    -1,   127,
     128,   129,   130,   131,   132,   133,    79,    -1,    -1,    -1,
      -1,    -1,   138,    -1,    87,    -1,    -1,    -1,    -1,    -1,
      -1,    94,    95,    96,    97,    -1,    -1,   100,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   127,   128,   129,   130,   131,   132,
     133,    -1,    -1,   228,    -1,   230,   231,   232,   233,   234,
     235,   236,   237,   238,   239,   240,   241,   242,   243,   244,
     245,   246,    -1,     5,     6,   250,     8,    -1,    -1,    -1,
      -1,    -1,    -1,   221,   259,   223,   224,   225,   226,    -1,
      -1,    -1,   228,    -1,   230,   231,   232,   233,   234,   235,
     236,   237,   238,   239,   240,   241,   242,   243,   244,   245,
     246,    -1,    -1,    -1,   250,    -1,   291,    -1,    -1,    -1,
      -1,    -1,    54,   259,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   217,    -1,    -1,    -1,    70,     5,
       6,    -1,     8,    -1,    -1,    -1,    -1,    79,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    87,    -1,    -1,    -1,    -1,
      -1,    -1,    94,    95,    96,    97,    -1,    -1,   100,    -1,
      -1,    -1,    -1,    39,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    54,    -1,
      -1,    -1,    -1,    -1,    -1,   127,   128,   129,   130,   131,
     132,   133,    68,    -1,    70,    -1,    72,    -1,    -1,    75,
      76,    -1,    -1,    79,    80,    81,    82,    83,    84,    85,
      86,    87,    88,    89,    90,    91,    92,    -1,    94,    95,
      96,    97,    -1,    99,   100,     5,     6,   103,     8,    -1,
      -1,    -1,    -1,    -1,   110,    -1,    -1,    -1,    -1,     5,
       6,    -1,     8,   119,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,   127,   128,   129,   130,   131,   132,   133,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   209,    -1,    -1,
      -1,    -1,    -1,    -1,    54,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    54,    -1,
      70,    -1,    -1,    73,    -1,    75,    76,    -1,    -1,    79,
      -1,    -1,    82,    -1,    70,     5,     6,    87,     8,    75,
      76,    -1,    -1,    79,    94,    95,    96,    97,    -1,    -1,
     100,    87,    -1,    -1,     5,     6,    -1,     8,    94,    95,
      96,    97,    -1,    -1,   100,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   127,   128,   129,
     130,   131,   132,   133,    54,    -1,    -1,    -1,    -1,    -1,
      -1,   127,   128,   129,   130,   131,   132,   133,    -1,    -1,
      70,     5,     6,    54,     8,     9,    -1,    -1,    -1,    79,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    87,    -1,    70,
      -1,    -1,    -1,    -1,    94,    95,    96,    97,    79,    -1,
     100,    -1,    -1,    -1,    -1,    -1,    87,    -1,    -1,    -1,
      -1,    -1,    -1,    94,    95,    96,    97,    -1,    -1,   100,
      54,   121,   122,    -1,    -1,    -1,    -1,   127,   128,   129,
     130,   131,   132,   133,    -1,     5,    70,    -1,     8,     9,
      -1,    -1,    -1,    -1,    -1,    79,   127,   128,   129,   130,
     131,   132,   133,    87,    -1,    -1,    -1,    -1,    -1,    -1,
      94,    95,    96,    97,    -1,    -1,   100,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    54,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   127,   128,   129,   130,   131,   132,   133,
      70,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    79,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    87,    -1,    -1,
      -1,    -1,    -1,    -1,    94,    95,    96,    97,    -1,    -1,
     100,    -1,    39,    40,    -1,    -1,    -1,    -1,    45,    -1,
      47,    -1,    -1,    -1,    -1,    -1,    -1,    54,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   127,   128,   129,
     130,   131,   132,   133,    71,    72,    -1,    74,    75,    76,
      77,    78,    -1,    -1,    -1,    -1,    83,    -1,    -1,    86,
      -1,    -1,    -1,    -1,    -1,    -1,    93,    -1,    -1,    -1,
      -1,    98,    -1,    -1,   101,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   113,   114,   115,   116,
     117,   118,   119,   120,    -1,    -1,    -1,    -1,    -1,   126
};

/* YYSTOS[STATE-NUM] -- The symbol kind of the accessing symbol of
   state STATE-NUM.  */
static const yytype_int16 yystos[] =
{
       0,   311,     0,    18,   156,   157,   158,   159,   160,   178,
     179,   195,   196,   197,   198,   202,   203,   208,   211,   218,
     219,   274,   275,   277,   278,   279,   281,   282,   283,   284,
     285,   286,   287,   288,   289,   298,   312,   315,   321,   322,
     323,   324,   325,   326,   333,   335,   336,   338,   339,   340,
     341,   342,   343,   360,   378,   382,   404,   405,   460,   463,
     469,   470,   471,   475,   484,   487,   492,   217,     5,     6,
       8,    54,    70,    79,    87,    94,    95,    96,    97,   100,
     127,   128,   129,   130,   131,   132,   133,   316,   317,   300,
     364,    64,   127,   406,   180,   181,   182,   183,   184,   185,
     186,   187,   188,   189,   190,   191,   192,   193,   194,   468,
     468,     8,    14,    17,    40,    41,    45,    47,    54,    71,
      86,   296,   327,   365,   366,   367,   368,   299,   300,   276,
     472,   217,   476,   493,   217,   317,     9,   318,   318,     9,
      10,   319,   319,    13,    16,    20,    21,    22,    23,    24,
      26,    27,    28,    29,    31,    32,    35,    36,    37,    38,
      42,    43,    51,    53,    68,    69,    70,   105,   106,   161,
     162,   163,   300,   309,   317,   323,   324,   368,   369,   427,
     450,   451,   456,   457,   291,   317,   317,   317,   317,     7,
      12,   413,   414,   413,   413,   291,   344,    60,   345,   291,
     383,   389,    23,    26,    27,    28,    29,    31,    32,    33,
     291,   307,   407,   410,   412,   413,   318,   291,   291,   291,
     291,   489,   295,   318,   361,   316,   300,   368,   427,   450,
     452,   456,     7,    33,   299,   314,   294,   296,   296,    46,
      48,    49,    50,   366,   366,   328,   369,   452,   299,    70,
     456,   296,   318,   318,   209,   317,   476,   102,   103,   104,
     127,   221,   223,   224,   225,   226,   317,    75,    76,   317,
     317,   456,    26,    27,    28,    29,   450,    51,   450,    24,
      25,    34,    15,    17,   456,   219,   306,   317,   368,   309,
     317,   318,   138,   138,   138,   365,   366,   138,   308,   107,
     108,   109,   138,   300,   302,   306,   307,   313,   450,   314,
     297,    12,   297,   297,   311,    39,    68,    72,    75,    76,
      80,    81,    82,    83,    84,    85,    86,    87,    88,    89,
      90,    91,    92,    99,   103,   110,   119,   317,   452,    61,
     346,   347,    39,    72,    74,    75,    76,    77,    78,    83,
      93,    98,   101,   113,   114,   115,   116,   117,   118,   119,
     120,   126,   366,   143,   144,   145,   146,   147,   148,   149,
     150,   151,   152,   153,   154,   155,   164,   165,   166,   167,
     168,   169,   206,   220,   255,   256,   291,   292,   315,   316,
     322,   333,   388,   390,   391,   392,   393,   395,   396,   404,
     428,   429,   430,   431,   432,   433,   434,   435,   436,   437,
     438,   439,   440,   441,   442,   460,   470,   306,   296,   300,
     409,   296,   409,   296,   409,   296,   409,   296,   409,   296,
     409,   296,   408,   410,   296,   413,   297,     7,     8,   294,
     305,   477,   485,   490,   494,    73,    75,    76,    82,   317,
     317,   301,    39,    72,    74,    75,    76,    77,   113,   114,
     115,   116,   119,   123,   124,   125,   227,   456,   299,   219,
     317,   366,   296,   299,   296,   291,   296,   293,     8,   318,
     318,   297,   291,   296,   314,   121,   122,   300,   317,   385,
     452,   301,   168,   473,   317,   222,   138,   450,    25,   317,
     452,   317,   301,   301,   301,   317,   318,   317,   317,   317,
     456,   317,   317,   296,   296,   317,    20,   301,   318,   458,
     459,   445,   446,   456,   292,   313,   292,   296,    75,    76,
      77,   113,   114,   115,   302,   351,   348,   452,    67,   156,
     170,   175,   200,   201,   220,   255,   256,   292,   315,   322,
     333,   343,   359,   360,   370,   374,   382,   404,   460,   470,
     488,   296,   296,   386,   318,   318,   318,   300,   112,   290,
     300,   105,   452,   305,   199,   296,   389,    55,    57,    58,
      59,   394,   397,   398,   399,   400,   401,   402,   316,   318,
     391,   316,   318,   318,   319,    11,    31,    32,   296,   319,
     320,   316,   318,   365,    15,    17,   368,   456,   452,    88,
     314,   412,   366,   328,   296,   413,   296,   318,   318,   318,
     318,   319,   320,   320,   292,   294,   316,   297,   318,   318,
     210,   212,   215,   216,   292,   322,   333,   460,   478,   480,
     481,   483,    84,   210,   213,   292,   474,   480,   482,   486,
      41,   156,   208,   211,   292,   322,   333,   491,   208,   211,
     292,   322,   333,   495,    75,    76,    77,   113,   114,   115,
     296,   296,   317,   317,   301,   456,   314,   464,   465,   291,
      51,   452,   461,   462,     7,   294,   297,   297,   327,   329,
     330,   302,   358,   444,    19,   337,   474,   138,   317,    19,
     301,   451,   451,   451,   306,   452,   452,    20,   294,   301,
     303,   294,   318,    39,    51,    52,    69,   121,   293,   304,
     352,   353,   354,   294,   112,   371,   375,   318,   318,   489,
     112,   290,   105,   452,   291,   291,   291,   389,   291,   318,
     314,   384,   300,   456,   305,   318,   300,   317,   300,   317,
     318,   366,    19,   296,    20,   386,   447,   448,   449,   292,
     452,   394,    56,   391,   403,   316,   318,   391,   403,   403,
     403,    62,    62,   296,   296,   317,   452,   296,   413,   456,
     316,   318,   443,   297,   314,   297,   301,   297,   297,   297,
     297,   297,   408,   297,   305,     8,   294,   214,   299,   306,
     318,   479,   299,   314,   413,   413,   299,   299,   413,   413,
     296,   217,   318,   317,   217,   317,   217,   318,    16,    21,
      22,    24,    25,    26,    27,    28,    29,    31,    32,    34,
      35,    36,    37,    38,    80,    82,    88,   105,   138,   228,
     230,   231,   232,   233,   234,   235,   236,   237,   238,   239,
     240,   241,   242,   243,   244,   245,   246,   250,   259,   291,
     380,   381,   453,    63,   362,   301,   299,   297,   294,   329,
       8,   299,   292,   294,     8,   299,   292,    22,    23,    24,
      26,    27,    28,    29,    31,    32,    35,    36,    37,    38,
      42,    43,   112,   322,   331,   411,   412,   416,   300,   445,
     296,   296,   317,   385,    28,    29,    63,   204,   205,   207,
     413,   317,   317,   451,   296,   297,   297,   318,   459,   456,
     297,   296,   353,   296,   317,   356,   303,   452,   452,    72,
     119,   317,   452,    72,   119,   366,   317,   300,   317,   300,
     317,   366,    19,   347,   372,   376,   292,   490,   297,   138,
     384,    39,    46,    48,    49,    50,    88,    89,    90,   135,
     136,   137,   139,   142,   297,   252,   253,   254,   318,   227,
     379,   318,   301,   318,   318,   294,   301,   456,   385,   447,
     456,   297,   294,   316,   318,   316,   318,   318,   319,    19,
     314,   297,   296,   294,   294,   297,   297,   409,   409,   409,
     409,   409,   409,   318,   318,   318,   296,   305,   296,   297,
     297,   296,   296,   297,   297,   318,   451,   317,    63,   317,
     297,    25,    26,    27,    28,    29,   296,   454,   244,   236,
     248,   296,   229,   249,    22,   454,   454,    21,    22,    24,
      25,    26,    27,    28,    29,    31,    32,    34,    35,    36,
      37,    38,   231,   232,   234,   235,   236,   238,   239,   242,
     243,   245,   258,   259,   260,   261,   262,   263,   264,   265,
     266,   267,   268,   269,   270,   271,   272,   306,   455,   297,
     414,   300,   306,   316,   299,   363,    28,    65,    66,   314,
     318,   450,   466,   467,   464,   292,   299,   291,   461,   291,
     296,   314,   296,   300,   296,   300,    26,    27,    28,    29,
     296,   300,   296,   300,   296,   300,   296,   300,   296,   300,
     296,   300,   296,   300,   296,   300,   296,   300,   296,   300,
     296,   300,   296,   300,   296,   300,   106,   111,   322,   332,
     413,   318,   303,   447,   447,   358,   444,   316,   297,   447,
     318,   349,   350,   452,   294,   355,   317,   198,   323,   317,
     456,   318,   318,   294,   456,   385,   292,   171,   172,   173,
     174,   292,   315,   322,   333,   373,   470,   174,   176,   177,
     292,   315,   322,   333,   377,   470,   292,   314,   297,   296,
     305,   305,   301,   301,   301,   301,   296,   385,   138,   301,
     301,   452,   363,   452,   297,   379,   449,    62,    62,   297,
     297,   317,   297,   447,   443,   443,     8,   294,     8,   479,
     297,   318,   251,   314,   300,   300,    25,    26,    27,    28,
      29,   273,   294,   300,   307,   292,   293,   301,   318,    22,
      23,    24,    26,    27,    28,    29,    31,    32,    35,    36,
      37,    38,    44,   314,   411,   415,   296,   296,   291,   331,
     329,   466,   318,   318,   318,   296,   300,   296,   300,   296,
     300,   296,   300,   318,   318,   318,   318,   318,   318,   319,
     318,   318,   320,   318,   319,   320,   318,   318,   318,   318,
     318,   318,   318,   319,   318,   416,   318,     8,    44,   318,
      44,    51,   450,   318,    42,    92,   112,   334,   457,   297,
     301,   297,   297,   296,   296,   473,   297,   297,   297,   294,
     354,   355,   317,   301,   301,   452,   452,   257,   365,   365,
     365,   365,   365,   365,   365,   384,   318,   140,   141,   140,
     141,   380,   351,   316,   294,    19,   316,   316,   318,   297,
     318,   305,   299,   294,   318,   318,   314,   301,   318,   293,
     301,    26,    27,    28,    29,   318,    26,    27,    28,   318,
     331,   292,   292,   297,   301,   297,   301,   318,   318,   318,
     318,   318,   318,   319,   318,   297,   301,   297,   301,   297,
     301,   297,   301,   297,   297,   301,   297,   297,   301,   297,
     301,   297,   301,   297,   301,   297,   301,   297,   301,   297,
     297,   301,   297,     8,   297,   301,    51,   450,   300,   317,
     303,   447,   447,   452,   296,   294,    19,   366,   297,   297,
     297,   296,   452,   385,     8,   479,   318,   314,   301,   301,
     301,   318,   297,   305,   305,   305,   297,   292,   296,   296,
     297,   301,   297,   301,   297,   301,   297,   301,   296,   296,
     296,   296,   296,   296,   296,   296,   296,   296,   296,   296,
     297,   296,     8,   301,   299,   297,   297,   447,   452,   385,
     456,   447,   302,   357,   358,   305,   297,   294,   297,   453,
     301,   318,   318,   318,   423,   421,   296,   296,   296,   296,
     422,   421,   420,   419,   417,   418,   422,   421,   420,   419,
     426,   424,   425,   416,   297,   357,   452,   297,   296,   479,
     314,   297,   297,   297,   297,   466,   297,   318,   422,   421,
     420,   419,   297,   318,   297,   297,   318,   297,   319,   297,
     318,   320,   297,   319,   320,   297,   297,   297,   297,   297,
     416,     8,    44,   297,    44,    51,   297,   450,   363,   296,
      19,   387,   447,   294,   297,   297,   297,   297,     8,   447,
     385,    39,    54,    70,    79,    94,    95,    96,    97,   100,
     127,   128,   129,   130,   131,   132,   133,   291,   297,   314,
     297,   296,   296,   297,   257,   447,   318,   105,   297,   297,
     366,   456,   452,    19,   385,   357,   296,   447,   297
};

/* YYR1[RULE-NUM] -- Symbol kind of the left-hand side of rule RULE-NUM.  */
static const yytype_int16 yyr1[] =
{
       0,   310,   311,   311,   312,   312,   312,   312,   312,   312,
     312,   312,   312,   312,   312,   312,   312,   312,   312,   312,
     312,   312,   312,   312,   312,   312,   312,   312,   312,   312,
     313,   313,   314,   314,   315,   315,   315,   316,   316,   316,
     316,   316,   316,   316,   316,   316,   316,   316,   316,   316,
     316,   316,   316,   316,   316,   317,   317,   317,   318,   319,
     319,   320,   320,   320,   321,   321,   321,   321,   321,   322,
     322,   322,   322,   322,   322,   322,   322,   322,   323,   323,
     323,   323,   324,   324,   324,   324,   325,   326,   327,   328,
     328,   329,   330,   330,   330,   331,   331,   331,   332,   332,
     333,   333,   333,   334,   334,   334,   334,   334,   334,   335,
     335,   335,   336,   337,   337,   337,   337,   337,   337,   338,
     339,   340,   341,   342,   343,   344,   344,   344,   344,   344,
     344,   344,   344,   344,   344,   344,   344,   344,   344,   344,
     344,   344,   344,   344,   344,   344,   344,   344,   344,   344,
     344,   344,   344,   345,   345,   346,   346,   347,   347,   348,
     348,   349,   349,   350,   350,   351,   351,   352,   352,   352,
     352,   352,   352,   352,   353,   353,   354,   354,   355,   355,
     356,   357,   357,   358,   359,   359,   359,   359,   359,   359,
     359,   359,   359,   359,   359,   359,   359,   359,   359,   359,
     359,   359,   359,   359,   359,   360,   361,   361,   361,   361,
     361,   361,   361,   361,   361,   361,   361,   361,   361,   361,
     361,   361,   362,   362,   363,   363,   364,   364,   365,   365,
     365,   365,   365,   365,   365,   366,   366,   366,   366,   367,
     367,   367,   367,   367,   367,   367,   367,   368,   369,   369,
     369,   369,   369,   369,   370,   370,   371,   371,   371,   372,
     372,   373,   373,   373,   373,   373,   373,   373,   373,   374,
     375,   375,   375,   376,   376,   377,   377,   377,   377,   377,
     377,   377,   378,   379,   379,   380,   380,   381,   382,   383,
     383,   383,   383,   383,   383,   383,   383,   383,   383,   383,
     383,   383,   383,   383,   383,   383,   383,   383,   383,   383,
     383,   383,   384,   384,   384,   384,   384,   384,   384,   384,
     384,   384,   384,   384,   384,   384,   384,   384,   385,   385,
     385,   386,   386,   386,   386,   386,   387,   387,   387,   387,
     387,   387,   387,   387,   387,   387,   387,   387,   387,   387,
     387,   387,   387,   388,   389,   389,   390,   390,   390,   390,
     390,   390,   390,   390,   390,   390,   390,   390,   390,   390,
     390,   390,   390,   390,   390,   390,   390,   390,   390,   390,
     390,   390,   391,   392,   393,   394,   394,   395,   395,   395,
     396,   397,   397,   397,   397,   398,   398,   398,   399,   400,
     401,   402,   403,   403,   403,   404,   405,   405,   406,   406,
     406,   407,   407,   408,   408,   409,   409,   410,   410,   410,
     410,   410,   410,   410,   410,   410,   410,   410,   410,   410,
     410,   410,   411,   411,   411,   411,   411,   411,   411,   411,
     411,   411,   411,   411,   411,   411,   411,   411,   411,   411,
     411,   412,   413,   413,   414,   414,   415,   415,   415,   416,
     416,   416,   416,   416,   416,   416,   416,   416,   416,   416,
     416,   416,   416,   416,   416,   416,   416,   416,   416,   416,
     416,   416,   416,   416,   416,   417,   417,   417,   418,   418,
     418,   419,   419,   420,   420,   421,   421,   422,   422,   423,
     423,   424,   424,   424,   425,   425,   425,   425,   426,   426,
     427,   428,   429,   430,   431,   432,   433,   434,   435,   436,
     437,   438,   439,   440,   441,   442,   442,   442,   442,   442,
     442,   442,   442,   442,   442,   442,   442,   442,   442,   442,
     442,   442,   442,   442,   442,   442,   442,   442,   443,   443,
     443,   443,   443,   444,   444,   445,   445,   446,   446,   447,
     447,   448,   448,   449,   449,   449,   450,   450,   450,   450,
     450,   450,   450,   450,   450,   450,   451,   451,   452,   452,
     452,   452,   453,   453,   453,   453,   453,   453,   453,   453,
     453,   453,   453,   453,   453,   453,   453,   453,   453,   453,
     453,   453,   453,   453,   453,   453,   453,   453,   453,   453,
     453,   453,   453,   453,   453,   453,   453,   453,   453,   453,
     453,   453,   453,   453,   453,   453,   453,   453,   453,   453,
     453,   453,   453,   453,   453,   453,   453,   454,   454,   455,
     455,   455,   455,   455,   455,   455,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   455,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   455,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   455,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   455,   455,   455,   456,   456,
     456,   456,   456,   456,   456,   456,   456,   456,   456,   456,
     456,   456,   456,   456,   456,   456,   456,   456,   456,   456,
     456,   456,   457,   457,   457,   457,   457,   457,   457,   457,
     457,   457,   457,   457,   457,   457,   457,   457,   457,   457,
     458,   458,   459,   459,   459,   459,   459,   460,   460,   460,
     460,   460,   460,   461,   461,   461,   462,   462,   463,   463,
     464,   464,   465,   466,   466,   467,   467,   467,   467,   467,
     467,   467,   467,   468,   468,   468,   468,   468,   468,   468,
     468,   468,   468,   468,   468,   468,   468,   468,   469,   469,
     470,   470,   470,   470,   470,   470,   470,   470,   470,   470,
     470,   471,   471,   472,   472,   473,   473,   474,   475,   476,
     476,   476,   476,   476,   476,   476,   476,   476,   476,   477,
     477,   478,   478,   478,   479,   479,   480,   480,   480,   480,
     480,   480,   481,   482,   483,   484,   484,   485,   485,   486,
     486,   486,   486,   487,   488,   489,   489,   489,   489,   489,
     489,   489,   489,   489,   489,   490,   490,   491,   491,   491,
     491,   491,   491,   491,   492,   492,   493,   493,   493,   494,
     494,   495,   495,   495,   495
};

/* YYR2[RULE-NUM] -- Number of symbols on the right-hand side of rule RULE-NUM.  */
static const yytype_int8 yyr2[] =
{
       0,     2,     0,     2,     4,     4,     3,     1,     1,     1,
       1,     1,     1,     4,     4,     4,     4,     1,     1,     1,
       2,     2,     3,     2,     2,     1,     1,     1,     4,     1,
       0,     2,     1,     3,     2,     4,     6,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     3,     1,     1,
       1,     1,     4,     4,     4,     4,     4,     4,     4,     2,
       3,     2,     2,     2,     1,     1,     2,     1,     2,     4,
       6,     3,     5,     7,     9,     3,     4,     7,     1,     1,
       1,     2,     0,     2,     2,     0,     6,     2,     1,     1,
       1,     1,     1,     1,     1,     1,     3,     2,     3,     1,
       2,     3,     7,     0,     2,     2,     2,     2,     2,     3,
       3,     2,     1,     4,     3,     0,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     3,     3,     3,     3,     3,     3,     2,
       2,     2,     5,     0,     2,     0,     2,     0,     2,     3,
       1,     0,     1,     1,     3,     0,     3,     1,     1,     1,
       1,     1,     1,     4,     0,     2,     4,     3,     0,     2,
       3,     0,     1,     5,     3,     4,     4,     4,     1,     1,
       1,     1,     1,     2,     2,     4,    13,    22,     1,     1,
       5,     3,     7,     5,     4,     7,     0,     2,     2,     2,
       2,     2,     2,     2,     5,     2,     2,     2,     2,     2,
       2,     5,     0,     2,     0,     2,     0,     3,     9,     9,
       7,     7,     1,     1,     1,     2,     2,     1,     4,     0,
       1,     1,     2,     2,     2,     2,     1,     4,     2,     5,
       3,     2,     2,     1,     4,     3,     0,     2,     2,     0,
       2,     2,     2,     2,     2,     1,     1,     1,     1,     9,
       0,     2,     2,     0,     2,     2,     2,     2,     1,     1,
       1,     1,     1,     0,     4,     1,     3,     1,    13,     0,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     5,     8,
       6,     5,     0,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     2,     4,     4,     4,     4,     5,     1,     1,
       1,     0,     4,     4,     4,     4,     0,     2,     2,     2,
       2,     2,     2,     2,     2,     2,     2,     2,     2,     2,
       2,     2,     5,     1,     0,     2,     2,     1,     2,     4,
       5,     1,     1,     1,     1,     2,     1,     1,     1,     1,
       1,     4,     6,     4,     4,    11,     1,     5,     3,     7,
       5,     5,     3,     1,     2,     2,     1,     2,     4,     4,
       1,     2,     2,     2,     2,     2,     2,     2,     1,     2,
       1,     1,     1,     4,     4,     2,     4,     2,     0,     1,
       1,     3,     1,     3,     1,     0,     3,     5,     4,     3,
       5,     5,     5,     5,     5,     5,     2,     2,     2,     2,
       2,     2,     4,     4,     4,     4,     4,     4,     4,     4,
       5,     5,     5,     5,     4,     4,     4,     4,     4,     4,
       3,     2,     0,     1,     1,     2,     1,     1,     1,     1,
       4,     4,     5,     4,     4,     4,     7,     7,     7,     7,
       7,     7,     7,     7,     7,     7,     8,     8,     8,     8,
       7,     7,     7,     7,     7,     0,     2,     2,     0,     2,
       2,     0,     2,     0,     2,     0,     2,     0,     2,     0,
       2,     0,     2,     2,     0,     2,     3,     2,     0,     2,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     2,     1,     2,     2,     2,     2,
       2,     2,     3,     2,     2,     2,     5,     3,     2,     2,
       2,     2,     2,     5,     4,     6,     2,     4,     0,     3,
       3,     1,     1,     0,     3,     0,     1,     1,     3,     0,
       1,     1,     3,     1,     3,     4,     4,     4,     4,     5,
       1,     1,     1,     1,     1,     1,     1,     3,     1,     3,
       4,     1,     0,    10,     6,     5,     6,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     2,
       2,     2,     2,     1,     1,     1,     1,     2,     3,     4,
       6,     5,     1,     1,     1,     1,     1,     1,     1,     2,
       2,     1,     2,     2,     4,     1,     2,     1,     2,     1,
       2,     1,     2,     1,     2,     1,     1,     0,     5,     0,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     2,     2,     2,     2,     1,     1,     1,     1,     1,
       3,     2,     2,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     2,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     2,     1,
       3,     2,     3,     4,     2,     2,     2,     5,     5,     7,
       4,     3,     2,     3,     2,     1,     1,     2,     3,     2,
       1,     2,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     2,     2,     2,     2,     1,     1,     1,     1,     1,
       1,     3,     0,     1,     1,     3,     2,     6,     7,     3,
       3,     3,     6,     0,     1,     3,     5,     6,     4,     4,
       1,     3,     3,     1,     1,     1,     1,     4,     1,     6,
       6,     6,     4,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       3,     2,     5,     4,     7,     6,     7,     6,     9,     8,
       3,     8,     4,     0,     2,     0,     1,     3,     3,     0,
       2,     2,     2,     3,     2,     2,     2,     2,     2,     0,
       2,     3,     1,     1,     1,     1,     3,     8,     2,     3,
       1,     1,     3,     3,     3,     4,     6,     0,     2,     3,
       1,     3,     1,     4,     3,     0,     2,     2,     2,     3,
       3,     3,     3,     3,     3,     0,     2,     2,     3,     3,
       4,     2,     1,     1,     3,     5,     0,     2,     2,     0,
       2,     4,     3,     1,     1
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
#line 193 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->EndClass(); }
#line 3924 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 5: /* decl: nameSpaceHead '{' decls '}'  */
#line 194 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->EndNameSpace(); }
#line 3930 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 6: /* decl: methodHead methodDecls '}'  */
#line 195 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                                                  {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                                     PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                                                  PASM->EndMethod(); }
#line 3939 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 13: /* decl: assemblyHead '{' assemblyDecls '}'  */
#line 205 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASMM->EndAssembly(); }
#line 3945 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 14: /* decl: assemblyRefHead '{' assemblyRefDecls '}'  */
#line 206 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASMM->EndAssembly(); }
#line 3951 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 15: /* decl: exptypeHead '{' exptypeDecls '}'  */
#line 207 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASMM->EndComType(); }
#line 3957 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 16: /* decl: manifestResHead '{' manifestResDecls '}'  */
#line 208 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASMM->EndManifestRes(); }
#line 3963 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 20: /* decl: _SUBSYSTEM int32  */
#line 212 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                {
                                                                                  PASM->m_dwSubsystem = (yyvsp[0].int32);
                                                                                }
#line 3971 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 21: /* decl: _CORFLAGS int32  */
#line 215 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->m_dwComImageFlags = (yyvsp[0].int32); }
#line 3977 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 22: /* decl: _FILE ALIGNMENT_ int32  */
#line 216 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->m_dwFileAlignment = (yyvsp[0].int32);
                                                                                  if(((yyvsp[0].int32) & ((yyvsp[0].int32) - 1))||((yyvsp[0].int32) < 0x200)||((yyvsp[0].int32) > 0x10000))
                                                                                    PASM->report->error("Invalid file alignment, must be power of 2 from 0x200 to 0x10000\n");}
#line 3985 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 23: /* decl: _IMAGEBASE int64  */
#line 219 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->m_stBaseAddress = (ULONGLONG)(*((yyvsp[0].int64))); delete (yyvsp[0].int64);
                                                                                  if(PASM->m_stBaseAddress & 0xFFFF)
                                                                                    PASM->report->error("Invalid image base, must be 0x10000-aligned\n");}
#line 3993 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 24: /* decl: _STACKRESERVE int64  */
#line 222 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->m_stSizeOfStackReserve = (size_t)(*((yyvsp[0].int64))); delete (yyvsp[0].int64); }
#line 3999 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 29: /* decl: _MSCORLIB  */
#line 227 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->m_fIsMscorlib = TRUE; }
#line 4005 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 32: /* compQstring: QSTRING  */
#line 234 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4011 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 33: /* compQstring: compQstring '+' QSTRING  */
#line 235 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 4017 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 34: /* languageDecl: _LANGUAGE SQSTRING  */
#line 238 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[0].string),&(PASM->m_guidLang)); }
#line 4023 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 35: /* languageDecl: _LANGUAGE SQSTRING ',' SQSTRING  */
#line 239 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid((yyvsp[0].string),&(PASM->m_guidLangVendor));}
#line 4030 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 36: /* languageDecl: _LANGUAGE SQSTRING ',' SQSTRING ',' SQSTRING  */
#line 241 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[-4].string),&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidLangVendor));
                                                                                  LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidDoc));}
#line 4038 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 37: /* id: ID  */
#line 246 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 4044 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 38: /* id: NATIVE_  */
#line 248 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("native"); }
#line 4050 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 39: /* id: CIL_  */
#line 249 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("cil"); }
#line 4056 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 40: /* id: OPTIL_  */
#line 250 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("optil"); }
#line 4062 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 41: /* id: MANAGED_  */
#line 251 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("managed"); }
#line 4068 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 42: /* id: UNMANAGED_  */
#line 252 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("unmanaged"); }
#line 4074 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 43: /* id: FORWARDREF_  */
#line 253 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("forwardref"); }
#line 4080 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 44: /* id: PRESERVESIG_  */
#line 254 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("preservesig"); }
#line 4086 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 45: /* id: RUNTIME_  */
#line 255 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("runtime"); }
#line 4092 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 46: /* id: INTERNALCALL_  */
#line 256 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("internalcall"); }
#line 4098 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 47: /* id: SYNCHRONIZED_  */
#line 257 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("synchronized"); }
#line 4104 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 48: /* id: NOINLINING_  */
#line 258 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("noinlining"); }
#line 4110 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 49: /* id: AGGRESSIVEINLINING_  */
#line 259 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("aggressiveinlining"); }
#line 4116 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 50: /* id: NOOPTIMIZATION_  */
#line 260 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("nooptimization"); }
#line 4122 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 51: /* id: AGGRESSIVEOPTIMIZATION_  */
#line 261 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("aggressiveoptimization"); }
#line 4128 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 52: /* id: ASYNC_  */
#line 262 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("async"); }
#line 4134 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 53: /* id: EXTENDED_  */
#line 263 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newString("extended"); }
#line 4140 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 54: /* id: SQSTRING  */
#line 264 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 4146 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 55: /* dottedName: id  */
#line 267 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 4152 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 56: /* dottedName: DOTTEDNAME  */
#line 268 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 4158 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 57: /* dottedName: dottedName '.' dottedName  */
#line 269 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newStringWDel((yyvsp[-2].string), '.', (yyvsp[0].string)); }
#line 4164 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 58: /* int32: INT32_V  */
#line 272 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 4170 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 59: /* int64: INT64_V  */
#line 275 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int64) = (yyvsp[0].int64); }
#line 4176 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 60: /* int64: INT32_V  */
#line 276 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int64) = neg ? new int64_t((yyvsp[0].int32)) : new int64_t((unsigned)(yyvsp[0].int32)); }
#line 4182 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 61: /* float64: FLOAT64  */
#line 279 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.float64) = (yyvsp[0].float64); }
#line 4188 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 62: /* float64: FLOAT32_ '(' int32 ')'  */
#line 280 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { float f; *((int32_t*) (&f)) = (yyvsp[-1].int32); (yyval.float64) = new double(f); }
#line 4194 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 63: /* float64: FLOAT64_ '(' int64 ')'  */
#line 281 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.float64) = (double*) (yyvsp[-1].int64); }
#line 4200 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 64: /* typedefDecl: _TYPEDEF type AS_ dottedName  */
#line 285 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].binstr),(yyvsp[0].string)); }
#line 4206 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 65: /* typedefDecl: _TYPEDEF className AS_ dottedName  */
#line 286 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].token),(yyvsp[0].string)); }
#line 4212 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 66: /* typedefDecl: _TYPEDEF memberRef AS_ dottedName  */
#line 287 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].token),(yyvsp[0].string)); }
#line 4218 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 67: /* typedefDecl: _TYPEDEF customDescr AS_ dottedName  */
#line 288 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyvsp[-2].cad)->tkOwner = 0; PASM->AddTypeDef((yyvsp[-2].cad),(yyvsp[0].string)); }
#line 4224 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 68: /* typedefDecl: _TYPEDEF customDescrWithOwner AS_ dottedName  */
#line 289 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].cad),(yyvsp[0].string)); }
#line 4230 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 69: /* compControl: P_DEFINE dottedName  */
#line 294 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { DefineVar((yyvsp[0].string), NULL); }
#line 4236 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 70: /* compControl: P_DEFINE dottedName compQstring  */
#line 295 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { DefineVar((yyvsp[-1].string), (yyvsp[0].binstr)); }
#line 4242 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 71: /* compControl: P_UNDEF dottedName  */
#line 296 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { UndefVar((yyvsp[0].string)); }
#line 4248 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 72: /* compControl: P_IFDEF dottedName  */
#line 297 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { SkipToken = !IsVarDefined((yyvsp[0].string));
                                                                                  IfEndif++;
                                                                                }
#line 4256 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 73: /* compControl: P_IFNDEF dottedName  */
#line 300 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { SkipToken = IsVarDefined((yyvsp[0].string));
                                                                                  IfEndif++;
                                                                                }
#line 4264 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 74: /* compControl: P_ELSE  */
#line 303 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { if(IfEndif == 1) SkipToken = !SkipToken;}
#line 4270 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 75: /* compControl: P_ENDIF  */
#line 304 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { if(IfEndif == 0)
                                                                                    PASM->report->error("Unmatched #endif\n");
                                                                                  else IfEndif--;
                                                                                }
#line 4279 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 76: /* compControl: P_INCLUDE QSTRING  */
#line 308 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { _ASSERTE(!"yylex should have dealt with this"); }
#line 4285 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 77: /* compControl: ';'  */
#line 309 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { }
#line 4291 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 78: /* customDescr: _CUSTOM customType  */
#line 313 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[0].token), NULL); }
#line 4297 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 79: /* customDescr: _CUSTOM customType '=' compQstring  */
#line 314 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].token), (yyvsp[0].binstr)); }
#line 4303 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 80: /* customDescr: _CUSTOM customType '=' '{' customBlobDescr '}'  */
#line 315 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 4309 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 81: /* customDescr: customHead bytes ')'  */
#line 316 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].int32), (yyvsp[-1].binstr)); }
#line 4315 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 82: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType  */
#line 319 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-2].token), (yyvsp[0].token), NULL); }
#line 4321 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 83: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType '=' compQstring  */
#line 320 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-4].token), (yyvsp[-2].token), (yyvsp[0].binstr)); }
#line 4327 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 84: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType '=' '{' customBlobDescr '}'  */
#line 322 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-6].token), (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 4333 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 85: /* customDescrWithOwner: customHeadWithOwner bytes ')'  */
#line 323 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].int32), (yyvsp[-1].binstr)); }
#line 4339 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 86: /* customHead: _CUSTOM customType '=' '('  */
#line 326 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-2].token); bParsingByteArray = TRUE; }
#line 4345 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 87: /* customHeadWithOwner: _CUSTOM '(' ownerType ')' customType '=' '('  */
#line 330 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->m_pCustomDescrList = NULL;
                                                                                  PASM->m_tkCurrentCVOwner = (yyvsp[-4].token);
                                                                                  (yyval.int32) = (yyvsp[-2].token); bParsingByteArray = TRUE; }
#line 4353 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 88: /* customType: methodRef  */
#line 335 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 4359 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 89: /* ownerType: typeSpec  */
#line 338 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 4365 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 90: /* ownerType: memberRef  */
#line 339 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 4371 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 91: /* customBlobDescr: customBlobArgs customBlobNVPairs  */
#line 343 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr);
                                                                                  (yyval.binstr)->appendInt16(VAL16(nCustomBlobNVPairs));
                                                                                  (yyval.binstr)->append((yyvsp[0].binstr));
                                                                                  nCustomBlobNVPairs = 0; }
#line 4380 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 92: /* customBlobArgs: %empty  */
#line 349 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt16(VAL16(0x0001)); }
#line 4386 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 93: /* customBlobArgs: customBlobArgs serInit  */
#line 350 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr);
                                                                                  AppendFieldToCustomBlob((yyval.binstr),(yyvsp[0].binstr)); }
#line 4393 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 94: /* customBlobArgs: customBlobArgs compControl  */
#line 352 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 4399 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 95: /* customBlobNVPairs: %empty  */
#line 355 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.binstr) = new BinStr(); }
#line 4405 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 96: /* customBlobNVPairs: customBlobNVPairs fieldOrProp serializType dottedName '=' serInit  */
#line 357 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-5].binstr); (yyval.binstr)->appendInt8((yyvsp[-4].int32));
                                                                                  (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                                  AppendStringWithLength((yyval.binstr),(yyvsp[-2].string));
                                                                                  AppendFieldToCustomBlob((yyval.binstr),(yyvsp[0].binstr));
                                                                                  nCustomBlobNVPairs++; }
#line 4415 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 97: /* customBlobNVPairs: customBlobNVPairs compControl  */
#line 362 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 4421 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 98: /* fieldOrProp: FIELD_  */
#line 365 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.int32) = SERIALIZATION_TYPE_FIELD; }
#line 4427 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 99: /* fieldOrProp: PROPERTY_  */
#line 366 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.int32) = SERIALIZATION_TYPE_PROPERTY; }
#line 4433 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 100: /* customAttrDecl: customDescr  */
#line 369 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { if((yyvsp[0].cad)->tkOwner && !(yyvsp[0].cad)->tkInterfacePair)
                                                                                    PASM->DefineCV((yyvsp[0].cad));
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH((yyvsp[0].cad)); }
#line 4442 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 101: /* customAttrDecl: customDescrWithOwner  */
#line 373 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->DefineCV((yyvsp[0].cad)); }
#line 4448 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 102: /* customAttrDecl: TYPEDEF_CA  */
#line 374 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { CustomDescr* pNew = new CustomDescr((yyvsp[0].tdd)->m_pCA);
                                                                                  if(pNew->tkOwner == 0) pNew->tkOwner = PASM->m_tkCurrentCVOwner;
                                                                                  if(pNew->tkOwner)
                                                                                    PASM->DefineCV(pNew);
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH(pNew); }
#line 4459 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 103: /* serializType: simpleType  */
#line 382 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4465 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 104: /* serializType: TYPE_  */
#line 383 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE); }
#line 4471 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 105: /* serializType: OBJECT_  */
#line 384 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TAGGED_OBJECT); }
#line 4477 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 106: /* serializType: ENUM_ CLASS_ SQSTRING  */
#line 385 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); }
#line 4484 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 107: /* serializType: ENUM_ className  */
#line 387 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[0].token))); }
#line 4491 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 108: /* serializType: serializType '[' ']'  */
#line 389 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 4497 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 109: /* moduleHead: _MODULE  */
#line 394 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASMM->SetModuleName(NULL); PASM->m_tkCurrentCVOwner=1; }
#line 4503 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 110: /* moduleHead: _MODULE dottedName  */
#line 395 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASMM->SetModuleName((yyvsp[0].string)); PASM->m_tkCurrentCVOwner=1; }
#line 4509 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 111: /* moduleHead: _MODULE EXTERN_ dottedName  */
#line 396 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { BinStr* pbs = new BinStr();
                                                                                  unsigned L = (unsigned)strlen((yyvsp[0].string));
                                                                                  memcpy((char*)(pbs->getBuff(L)),(yyvsp[0].string),L);
                                                                                  PASM->EmitImport(pbs); delete pbs;}
#line 4518 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 112: /* vtfixupDecl: _VTFIXUP '[' int32 ']' vtfixupAttr AT_ id  */
#line 403 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { /*PASM->SetDataSection(); PASM->EmitDataLabel($7);*/
                                                                                  PASM->m_VTFList.PUSH(new VTFEntry((USHORT)(yyvsp[-4].int32), (USHORT)(yyvsp[-2].int32), (yyvsp[0].string))); }
#line 4525 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 113: /* vtfixupAttr: %empty  */
#line 407 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.int32) = 0; }
#line 4531 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 114: /* vtfixupAttr: vtfixupAttr INT32_  */
#line 408 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_32BIT; }
#line 4537 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 115: /* vtfixupAttr: vtfixupAttr INT64_  */
#line 409 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_64BIT; }
#line 4543 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 116: /* vtfixupAttr: vtfixupAttr FROMUNMANAGED_  */
#line 410 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_FROM_UNMANAGED; }
#line 4549 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 117: /* vtfixupAttr: vtfixupAttr CALLMOSTDERIVED_  */
#line 411 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_CALL_MOST_DERIVED; }
#line 4555 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 118: /* vtfixupAttr: vtfixupAttr RETAINAPPDOMAIN_  */
#line 412 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN; }
#line 4561 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 119: /* vtableDecl: vtableHead bytes ')'  */
#line 415 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->m_pVTable = (yyvsp[-1].binstr); }
#line 4567 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 120: /* vtableHead: _VTABLE '=' '('  */
#line 418 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { bParsingByteArray = TRUE; }
#line 4573 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 121: /* nameSpaceHead: _NAMESPACE dottedName  */
#line 422 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->StartNameSpace((yyvsp[0].string)); }
#line 4579 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 122: /* _class: _CLASS  */
#line 425 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { newclass = TRUE; }
#line 4585 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 123: /* classHeadBegin: _class classAttr dottedName typarsClause  */
#line 428 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { if((yyvsp[0].typarlist)) FixupConstraints();
                                                                                  PASM->StartClass((yyvsp[-1].string), (yyvsp[-2].classAttr), (yyvsp[0].typarlist));
                                                                                  TyParFixupList.RESET(false);
                                                                                  newclass = FALSE;
                                                                                }
#line 4595 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 124: /* classHead: classHeadBegin extendsClause implClause  */
#line 434 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                                { PASM->AddClass(); }
#line 4601 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 125: /* classAttr: %empty  */
#line 437 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) 0; }
#line 4607 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 126: /* classAttr: classAttr PUBLIC_  */
#line 438 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdVisibilityMask) | tdPublic); }
#line 4613 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 127: /* classAttr: classAttr PRIVATE_  */
#line 439 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdVisibilityMask) | tdNotPublic); }
#line 4619 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 128: /* classAttr: classAttr VALUE_  */
#line 440 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | 0x80000000 | tdSealed); }
#line 4625 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 129: /* classAttr: classAttr ENUM_  */
#line 441 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | 0x40000000); }
#line 4631 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 130: /* classAttr: classAttr INTERFACE_  */
#line 442 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdInterface | tdAbstract); }
#line 4637 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 131: /* classAttr: classAttr SEALED_  */
#line 443 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSealed); }
#line 4643 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 132: /* classAttr: classAttr ABSTRACT_  */
#line 444 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdAbstract); }
#line 4649 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 133: /* classAttr: classAttr AUTO_  */
#line 445 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdAutoLayout); }
#line 4655 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 134: /* classAttr: classAttr SEQUENTIAL_  */
#line 446 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdSequentialLayout); }
#line 4661 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 135: /* classAttr: classAttr EXPLICIT_  */
#line 447 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdExplicitLayout); }
#line 4667 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 136: /* classAttr: classAttr EXTENDED_  */
#line 448 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdExtendedLayout); }
#line 4673 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 137: /* classAttr: classAttr ANSI_  */
#line 449 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdAnsiClass); }
#line 4679 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 138: /* classAttr: classAttr UNICODE_  */
#line 450 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdUnicodeClass); }
#line 4685 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 139: /* classAttr: classAttr AUTOCHAR_  */
#line 451 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdAutoClass); }
#line 4691 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 140: /* classAttr: classAttr IMPORT_  */
#line 452 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdImport); }
#line 4697 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 141: /* classAttr: classAttr SERIALIZABLE_  */
#line 453 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSerializable); }
#line 4703 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 142: /* classAttr: classAttr WINDOWSRUNTIME_  */
#line 454 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdWindowsRuntime); }
#line 4709 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 143: /* classAttr: classAttr NESTED_ PUBLIC_  */
#line 455 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedPublic); }
#line 4715 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 144: /* classAttr: classAttr NESTED_ PRIVATE_  */
#line 456 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedPrivate); }
#line 4721 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 145: /* classAttr: classAttr NESTED_ FAMILY_  */
#line 457 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamily); }
#line 4727 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 146: /* classAttr: classAttr NESTED_ ASSEMBLY_  */
#line 458 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedAssembly); }
#line 4733 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 147: /* classAttr: classAttr NESTED_ FAMANDASSEM_  */
#line 459 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamANDAssem); }
#line 4739 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 148: /* classAttr: classAttr NESTED_ FAMORASSEM_  */
#line 460 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamORAssem); }
#line 4745 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 149: /* classAttr: classAttr BEFOREFIELDINIT_  */
#line 461 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdBeforeFieldInit); }
#line 4751 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 150: /* classAttr: classAttr SPECIALNAME_  */
#line 462 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSpecialName); }
#line 4757 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 151: /* classAttr: classAttr RTSPECIALNAME_  */
#line 463 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr)); }
#line 4763 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 152: /* classAttr: classAttr FLAGS_ '(' int32 ')'  */
#line 464 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].int32)); }
#line 4769 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 154: /* extendsClause: EXTENDS_ typeSpec  */
#line 468 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                            { PASM->m_crExtends = (yyvsp[0].token); }
#line 4775 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 159: /* implList: implList ',' typeSpec  */
#line 479 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->AddToImplList((yyvsp[0].token)); }
#line 4781 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 160: /* implList: typeSpec  */
#line 480 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->AddToImplList((yyvsp[0].token)); }
#line 4787 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 161: /* typeList: %empty  */
#line 484 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.binstr) = new BinStr(); }
#line 4793 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 162: /* typeList: typeListNotEmpty  */
#line 485 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4799 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 163: /* typeListNotEmpty: typeSpec  */
#line 488 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[0].token)); }
#line 4805 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 164: /* typeListNotEmpty: typeListNotEmpty ',' typeSpec  */
#line 489 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->appendInt32((yyvsp[0].token)); }
#line 4811 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 165: /* typarsClause: %empty  */
#line 492 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.typarlist) = NULL; PASM->m_TyParList = NULL;}
#line 4817 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 166: /* typarsClause: '<' typars '>'  */
#line 493 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.typarlist) = (yyvsp[-1].typarlist);   PASM->m_TyParList = (yyvsp[-1].typarlist);}
#line 4823 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 167: /* typarAttrib: '+'  */
#line 496 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = gpCovariant; }
#line 4829 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 168: /* typarAttrib: '-'  */
#line 497 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = gpContravariant; }
#line 4835 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 169: /* typarAttrib: CLASS_  */
#line 498 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = gpReferenceTypeConstraint; }
#line 4841 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 170: /* typarAttrib: VALUETYPE_  */
#line 499 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = gpNotNullableValueTypeConstraint; }
#line 4847 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 171: /* typarAttrib: BYREFLIKE_  */
#line 500 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = gpAllowByRefLike; }
#line 4853 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 172: /* typarAttrib: _CTOR  */
#line 501 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = gpDefaultConstructorConstraint; }
#line 4859 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 173: /* typarAttrib: FLAGS_ '(' int32 ')'  */
#line 502 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = (CorGenericParamAttr)(yyvsp[-1].int32); }
#line 4865 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 174: /* typarAttribs: %empty  */
#line 505 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 4871 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 175: /* typarAttribs: typarAttrib typarAttribs  */
#line 506 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) | (yyvsp[0].int32); }
#line 4877 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 176: /* typars: typarAttribs tyBound dottedName typarsRest  */
#line 509 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                     {(yyval.typarlist) = new TyParList((yyvsp[-3].int32), (yyvsp[-2].binstr), (yyvsp[-1].string), (yyvsp[0].typarlist));}
#line 4883 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 177: /* typars: typarAttribs dottedName typarsRest  */
#line 510 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                               {(yyval.typarlist) = new TyParList((yyvsp[-2].int32), NULL, (yyvsp[-1].string), (yyvsp[0].typarlist));}
#line 4889 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 178: /* typarsRest: %empty  */
#line 513 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.typarlist) = NULL; }
#line 4895 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 179: /* typarsRest: ',' typars  */
#line 514 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.typarlist) = (yyvsp[0].typarlist); }
#line 4901 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 180: /* tyBound: '(' typeList ')'  */
#line 517 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 4907 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 181: /* genArity: %empty  */
#line 520 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32)= 0; }
#line 4913 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 182: /* genArity: genArityNotEmpty  */
#line 521 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = (yyvsp[0].int32); }
#line 4919 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 183: /* genArityNotEmpty: '<' '[' int32 ']' '>'  */
#line 524 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-2].int32); }
#line 4925 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 184: /* classDecl: methodHead methodDecls '}'  */
#line 528 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                              {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                 PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                              PASM->EndMethod(); }
#line 4934 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 185: /* classDecl: classHead '{' classDecls '}'  */
#line 532 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->EndClass(); }
#line 4940 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 186: /* classDecl: eventHead '{' eventDecls '}'  */
#line 533 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->EndEvent(); }
#line 4946 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 187: /* classDecl: propHead '{' propDecls '}'  */
#line 534 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->EndProp(); }
#line 4952 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 193: /* classDecl: _SIZE int32  */
#line 540 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                { PASM->m_pCurClass->m_ulSize = (yyvsp[0].int32); }
#line 4958 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 194: /* classDecl: _PACK int32  */
#line 541 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                { PASM->m_pCurClass->m_ulPack = (yyvsp[0].int32); }
#line 4964 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 195: /* classDecl: exportHead '{' exptypeDecls '}'  */
#line 542 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                { PASMM->EndComType(); }
#line 4970 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 196: /* classDecl: _OVERRIDE typeSpec DCOLON methodName WITH_ callConv type typeSpec DCOLON methodName '(' sigArgs0 ')'  */
#line 544 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                { BinStr *sig1 = parser->MakeSig((yyvsp[-7].int32), (yyvsp[-6].binstr), (yyvsp[-1].binstr));
                                                                  BinStr *sig2 = new BinStr(); sig2->append(sig1);
                                                                  PASM->AddMethodImpl((yyvsp[-11].token),(yyvsp[-9].string),sig1,(yyvsp[-5].token),(yyvsp[-3].string),sig2);
                                                                  PASM->ResetArgNameList();
                                                                }
#line 4980 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 197: /* classDecl: _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')' WITH_ METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'  */
#line 550 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                 { PASM->AddMethodImpl((yyvsp[-17].token),(yyvsp[-15].string),
                                                                      ((yyvsp[-14].int32)==0 ? parser->MakeSig((yyvsp[-19].int32),(yyvsp[-18].binstr),(yyvsp[-12].binstr)) :
                                                                      parser->MakeSig((yyvsp[-19].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-18].binstr),(yyvsp[-12].binstr),(yyvsp[-14].int32))),
                                                                      (yyvsp[-6].token),(yyvsp[-4].string),
                                                                      ((yyvsp[-3].int32)==0 ? parser->MakeSig((yyvsp[-8].int32),(yyvsp[-7].binstr),(yyvsp[-1].binstr)) :
                                                                      parser->MakeSig((yyvsp[-8].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-7].binstr),(yyvsp[-1].binstr),(yyvsp[-3].int32))));
                                                                   PASM->ResetArgNameList();
                                                                 }
#line 4993 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 200: /* classDecl: _PARAM TYPE_ '[' int32 ']'  */
#line 560 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { if(((yyvsp[-1].int32) > 0) && ((yyvsp[-1].int32) <= (int)PASM->m_pCurClass->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[(yyvsp[-1].int32)-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
#line 5003 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 201: /* classDecl: _PARAM TYPE_ dottedName  */
#line 565 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { int n = PASM->m_pCurClass->FindTyPar((yyvsp[0].string));
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",(yyvsp[0].string));
                                                            }
#line 5014 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 202: /* classDecl: _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec  */
#line 571 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                        { PASM->AddGenericParamConstraint((yyvsp[-3].int32), 0, (yyvsp[0].token)); }
#line 5020 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 203: /* classDecl: _PARAM CONSTRAINT_ dottedName ',' typeSpec  */
#line 572 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                        { PASM->AddGenericParamConstraint(0, (yyvsp[-2].string), (yyvsp[0].token)); }
#line 5026 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 204: /* classDecl: _INTERFACEIMPL TYPE_ typeSpec customDescr  */
#line 573 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                      { (yyvsp[0].cad)->tkInterfacePair = (yyvsp[-1].token);
                                                                        if(PASM->m_pCustomDescrList)
                                                                            PASM->m_pCustomDescrList->PUSH((yyvsp[0].cad));
                                                                      }
#line 5035 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 205: /* fieldDecl: _FIELD repeatOpt fieldAttr type dottedName atOpt initOpt  */
#line 581 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                              PASM->AddField((yyvsp[-2].string), (yyvsp[-3].binstr), (yyvsp[-4].fieldAttr), (yyvsp[-1].string), (yyvsp[0].binstr), (yyvsp[-5].int32)); }
#line 5042 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 206: /* fieldAttr: %empty  */
#line 585 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) 0; }
#line 5048 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 207: /* fieldAttr: fieldAttr STATIC_  */
#line 586 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdStatic); }
#line 5054 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 208: /* fieldAttr: fieldAttr PUBLIC_  */
#line 587 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPublic); }
#line 5060 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 209: /* fieldAttr: fieldAttr PRIVATE_  */
#line 588 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPrivate); }
#line 5066 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 210: /* fieldAttr: fieldAttr FAMILY_  */
#line 589 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamily); }
#line 5072 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 211: /* fieldAttr: fieldAttr INITONLY_  */
#line 590 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdInitOnly); }
#line 5078 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 212: /* fieldAttr: fieldAttr RTSPECIALNAME_  */
#line 591 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.fieldAttr) = (yyvsp[-1].fieldAttr); }
#line 5084 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 213: /* fieldAttr: fieldAttr SPECIALNAME_  */
#line 592 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdSpecialName); }
#line 5090 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 214: /* fieldAttr: fieldAttr MARSHAL_ '(' marshalBlob ')'  */
#line 605 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->m_pMarshal = (yyvsp[-1].binstr); }
#line 5096 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 215: /* fieldAttr: fieldAttr ASSEMBLY_  */
#line 606 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdAssembly); }
#line 5102 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 216: /* fieldAttr: fieldAttr FAMANDASSEM_  */
#line 607 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamANDAssem); }
#line 5108 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 217: /* fieldAttr: fieldAttr FAMORASSEM_  */
#line 608 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamORAssem); }
#line 5114 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 218: /* fieldAttr: fieldAttr PRIVATESCOPE_  */
#line 609 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPrivateScope); }
#line 5120 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 219: /* fieldAttr: fieldAttr LITERAL_  */
#line 610 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdLiteral); }
#line 5126 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 220: /* fieldAttr: fieldAttr NOTSERIALIZED_  */
#line 611 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdNotSerialized); }
#line 5132 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 221: /* fieldAttr: fieldAttr FLAGS_ '(' int32 ')'  */
#line 612 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].int32)); }
#line 5138 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 222: /* atOpt: %empty  */
#line 615 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.string) = 0; }
#line 5144 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 223: /* atOpt: AT_ id  */
#line 616 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.string) = (yyvsp[0].string); }
#line 5150 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 224: /* initOpt: %empty  */
#line 619 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.binstr) = NULL; }
#line 5156 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 225: /* initOpt: '=' fieldInit  */
#line 620 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 5162 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 226: /* repeatOpt: %empty  */
#line 623 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = 0xFFFFFFFF; }
#line 5168 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 227: /* repeatOpt: '[' int32 ']'  */
#line 624 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32); }
#line 5174 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 228: /* methodRef: callConv type typeSpec DCOLON methodName tyArgs0 '(' sigArgs0 ')'  */
#line 629 ".\\src\\coreclr\\ilasm\\asmparse.y"
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
#line 5195 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 229: /* methodRef: callConv type typeSpec DCOLON methodName genArityNotEmpty '(' sigArgs0 ')'  */
#line 646 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(((yyvsp[-8].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               (yyval.token) = PASM->MakeMemberRef((yyvsp[-6].token), (yyvsp[-4].string),
                                                                 parser->MakeSig((yyvsp[-8].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-7].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32)));
                                                             }
#line 5205 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 230: /* methodRef: callConv type methodName tyArgs0 '(' sigArgs0 ')'  */
#line 652 ".\\src\\coreclr\\ilasm\\asmparse.y"
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
#line 5225 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 231: /* methodRef: callConv type methodName genArityNotEmpty '(' sigArgs0 ')'  */
#line 668 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(((yyvsp[-6].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               (yyval.token) = PASM->MakeMemberRef(mdTokenNil, (yyvsp[-4].string), parser->MakeSig((yyvsp[-6].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-5].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32)));
                                                             }
#line 5234 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 232: /* methodRef: mdtoken  */
#line 672 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token); }
#line 5240 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 233: /* methodRef: TYPEDEF_M  */
#line 673 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 5246 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 234: /* methodRef: TYPEDEF_MR  */
#line 674 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 5252 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 235: /* callConv: INSTANCE_ callConv  */
#line 677 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = ((yyvsp[0].int32) | IMAGE_CEE_CS_CALLCONV_HASTHIS); }
#line 5258 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 236: /* callConv: EXPLICIT_ callConv  */
#line 678 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = ((yyvsp[0].int32) | IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS); }
#line 5264 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 237: /* callConv: callKind  */
#line 679 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 5270 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 238: /* callConv: CALLCONV_ '(' int32 ')'  */
#line 680 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32); }
#line 5276 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 239: /* callKind: %empty  */
#line 683 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
#line 5282 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 240: /* callKind: DEFAULT_  */
#line 684 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
#line 5288 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 241: /* callKind: VARARG_  */
#line 685 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_VARARG; }
#line 5294 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 242: /* callKind: UNMANAGED_ CDECL_  */
#line 686 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_C; }
#line 5300 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 243: /* callKind: UNMANAGED_ STDCALL_  */
#line 687 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_STDCALL; }
#line 5306 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 244: /* callKind: UNMANAGED_ THISCALL_  */
#line 688 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_THISCALL; }
#line 5312 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 245: /* callKind: UNMANAGED_ FASTCALL_  */
#line 689 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_FASTCALL; }
#line 5318 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 246: /* callKind: UNMANAGED_  */
#line 690 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_UNMANAGED; }
#line 5324 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 247: /* mdtoken: MDTOKEN_ '(' int32 ')'  */
#line 693 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.token) = (yyvsp[-1].int32); }
#line 5330 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 248: /* memberRef: methodSpec methodRef  */
#line 696 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token);
                                                               PASM->delArgNameList(PASM->m_firstArgName);
                                                               PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                               PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                               PASM->SetMemberRefFixup((yyvsp[0].token),iOpcodeLen); }
#line 5340 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 249: /* memberRef: FIELD_ type typeSpec DCOLON dottedName  */
#line 702 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               (yyval.token) = PASM->MakeMemberRef((yyvsp[-2].token), (yyvsp[0].string), (yyvsp[-3].binstr));
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 5348 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 250: /* memberRef: FIELD_ type dottedName  */
#line 706 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyvsp[-1].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               (yyval.token) = PASM->MakeMemberRef(mdTokenNil, (yyvsp[0].string), (yyvsp[-1].binstr));
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 5356 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 251: /* memberRef: FIELD_ TYPEDEF_F  */
#line 709 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 5363 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 252: /* memberRef: FIELD_ TYPEDEF_MR  */
#line 711 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 5370 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 253: /* memberRef: mdtoken  */
#line 713 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token);
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 5377 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 254: /* eventHead: _EVENT eventAttr typeSpec dottedName  */
#line 718 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                 { PASM->ResetEvent((yyvsp[0].string), (yyvsp[-1].token), (yyvsp[-2].eventAttr)); }
#line 5383 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 255: /* eventHead: _EVENT eventAttr dottedName  */
#line 719 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                 { PASM->ResetEvent((yyvsp[0].string), mdTypeRefNil, (yyvsp[-1].eventAttr)); }
#line 5389 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 256: /* eventAttr: %empty  */
#line 723 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.eventAttr) = (CorEventAttr) 0; }
#line 5395 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 257: /* eventAttr: eventAttr RTSPECIALNAME_  */
#line 724 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.eventAttr) = (yyvsp[-1].eventAttr); }
#line 5401 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 258: /* eventAttr: eventAttr SPECIALNAME_  */
#line 725 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.eventAttr) = (CorEventAttr) ((yyvsp[-1].eventAttr) | evSpecialName); }
#line 5407 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 261: /* eventDecl: _ADDON methodRef  */
#line 732 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                           { PASM->SetEventMethod(0, (yyvsp[0].token)); }
#line 5413 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 262: /* eventDecl: _REMOVEON methodRef  */
#line 733 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                           { PASM->SetEventMethod(1, (yyvsp[0].token)); }
#line 5419 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 263: /* eventDecl: _FIRE methodRef  */
#line 734 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                           { PASM->SetEventMethod(2, (yyvsp[0].token)); }
#line 5425 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 264: /* eventDecl: _OTHER methodRef  */
#line 735 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                           { PASM->SetEventMethod(3, (yyvsp[0].token)); }
#line 5431 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 269: /* propHead: _PROPERTY propAttr callConv type dottedName '(' sigArgs0 ')' initOpt  */
#line 744 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->ResetProp((yyvsp[-4].string),
                                                              parser->MakeSig((IMAGE_CEE_CS_CALLCONV_PROPERTY |
                                                              ((yyvsp[-6].int32) & IMAGE_CEE_CS_CALLCONV_HASTHIS)),(yyvsp[-5].binstr),(yyvsp[-2].binstr)), (yyvsp[-7].propAttr), (yyvsp[0].binstr));}
#line 5439 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 270: /* propAttr: %empty  */
#line 749 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.propAttr) = (CorPropertyAttr) 0; }
#line 5445 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 271: /* propAttr: propAttr RTSPECIALNAME_  */
#line 750 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.propAttr) = (yyvsp[-1].propAttr); }
#line 5451 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 272: /* propAttr: propAttr SPECIALNAME_  */
#line 751 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.propAttr) = (CorPropertyAttr) ((yyvsp[-1].propAttr) | prSpecialName); }
#line 5457 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 275: /* propDecl: _SET methodRef  */
#line 759 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->SetPropMethod(0, (yyvsp[0].token)); }
#line 5463 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 276: /* propDecl: _GET methodRef  */
#line 760 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->SetPropMethod(1, (yyvsp[0].token)); }
#line 5469 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 277: /* propDecl: _OTHER methodRef  */
#line 761 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->SetPropMethod(2, (yyvsp[0].token)); }
#line 5475 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 282: /* methodHeadPart1: _METHOD  */
#line 769 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->ResetForNextMethod();
                                                              uMethodBeginLine = PASM->m_ulCurLine;
                                                              uMethodBeginColumn=PASM->m_ulCurColumn;
                                                            }
#line 5484 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 283: /* marshalClause: %empty  */
#line 775 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.binstr) = NULL; }
#line 5490 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 284: /* marshalClause: MARSHAL_ '(' marshalBlob ')'  */
#line 776 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 5496 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 285: /* marshalBlob: nativeType  */
#line 779 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 5502 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 286: /* marshalBlob: marshalBlobHead hexbytes '}'  */
#line 780 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 5508 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 287: /* marshalBlobHead: '{'  */
#line 783 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { bParsingByteArray = TRUE; }
#line 5514 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 288: /* methodHead: methodHeadPart1 methAttr callConv paramAttr type marshalClause methodName typarsClause '(' sigArgs0 ')' implAttr '{'  */
#line 787 ".\\src\\coreclr\\ilasm\\asmparse.y"
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
#line 5532 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 289: /* methAttr: %empty  */
#line 802 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) 0; }
#line 5538 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 290: /* methAttr: methAttr STATIC_  */
#line 803 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdStatic); }
#line 5544 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 291: /* methAttr: methAttr PUBLIC_  */
#line 804 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPublic); }
#line 5550 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 292: /* methAttr: methAttr PRIVATE_  */
#line 805 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPrivate); }
#line 5556 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 293: /* methAttr: methAttr FAMILY_  */
#line 806 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamily); }
#line 5562 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 294: /* methAttr: methAttr FINAL_  */
#line 807 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdFinal); }
#line 5568 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 295: /* methAttr: methAttr SPECIALNAME_  */
#line 808 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdSpecialName); }
#line 5574 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 296: /* methAttr: methAttr VIRTUAL_  */
#line 809 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdVirtual); }
#line 5580 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 297: /* methAttr: methAttr STRICT_  */
#line 810 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdCheckAccessOnOverride); }
#line 5586 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 298: /* methAttr: methAttr ABSTRACT_  */
#line 811 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdAbstract); }
#line 5592 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 299: /* methAttr: methAttr ASSEMBLY_  */
#line 812 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdAssem); }
#line 5598 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 300: /* methAttr: methAttr FAMANDASSEM_  */
#line 813 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamANDAssem); }
#line 5604 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 301: /* methAttr: methAttr FAMORASSEM_  */
#line 814 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamORAssem); }
#line 5610 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 302: /* methAttr: methAttr PRIVATESCOPE_  */
#line 815 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPrivateScope); }
#line 5616 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 303: /* methAttr: methAttr HIDEBYSIG_  */
#line 816 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdHideBySig); }
#line 5622 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 304: /* methAttr: methAttr NEWSLOT_  */
#line 817 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdNewSlot); }
#line 5628 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 305: /* methAttr: methAttr RTSPECIALNAME_  */
#line 818 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (yyvsp[-1].methAttr); }
#line 5634 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 306: /* methAttr: methAttr UNMANAGEDEXP_  */
#line 819 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdUnmanagedExport); }
#line 5640 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 307: /* methAttr: methAttr REQSECOBJ_  */
#line 820 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdRequireSecObject); }
#line 5646 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 308: /* methAttr: methAttr FLAGS_ '(' int32 ')'  */
#line 821 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].int32)); }
#line 5652 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 309: /* methAttr: methAttr PINVOKEIMPL_ '(' compQstring AS_ compQstring pinvAttr ')'  */
#line 823 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->SetPinvoke((yyvsp[-4].binstr),0,(yyvsp[-2].binstr),(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-7].methAttr) | mdPinvokeImpl); }
#line 5659 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 310: /* methAttr: methAttr PINVOKEIMPL_ '(' compQstring pinvAttr ')'  */
#line 826 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->SetPinvoke((yyvsp[-2].binstr),0,NULL,(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-5].methAttr) | mdPinvokeImpl); }
#line 5666 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 311: /* methAttr: methAttr PINVOKEIMPL_ '(' pinvAttr ')'  */
#line 829 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->SetPinvoke(new BinStr(),0,NULL,(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-4].methAttr) | mdPinvokeImpl); }
#line 5673 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 312: /* pinvAttr: %empty  */
#line 833 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) 0; }
#line 5679 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 313: /* pinvAttr: pinvAttr NOMANGLE_  */
#line 834 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmNoMangle); }
#line 5685 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 314: /* pinvAttr: pinvAttr ANSI_  */
#line 835 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetAnsi); }
#line 5691 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 315: /* pinvAttr: pinvAttr UNICODE_  */
#line 836 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetUnicode); }
#line 5697 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 316: /* pinvAttr: pinvAttr AUTOCHAR_  */
#line 837 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetAuto); }
#line 5703 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 317: /* pinvAttr: pinvAttr LASTERR_  */
#line 838 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmSupportsLastError); }
#line 5709 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 318: /* pinvAttr: pinvAttr WINAPI_  */
#line 839 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvWinapi); }
#line 5715 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 319: /* pinvAttr: pinvAttr CDECL_  */
#line 840 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvCdecl); }
#line 5721 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 320: /* pinvAttr: pinvAttr STDCALL_  */
#line 841 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvStdcall); }
#line 5727 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 321: /* pinvAttr: pinvAttr THISCALL_  */
#line 842 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvThiscall); }
#line 5733 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 322: /* pinvAttr: pinvAttr FASTCALL_  */
#line 843 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvFastcall); }
#line 5739 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 323: /* pinvAttr: pinvAttr BESTFIT_ ':' ON_  */
#line 844 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmBestFitEnabled); }
#line 5745 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 324: /* pinvAttr: pinvAttr BESTFIT_ ':' OFF_  */
#line 845 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmBestFitDisabled); }
#line 5751 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 325: /* pinvAttr: pinvAttr CHARMAPERROR_ ':' ON_  */
#line 846 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmThrowOnUnmappableCharEnabled); }
#line 5757 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 326: /* pinvAttr: pinvAttr CHARMAPERROR_ ':' OFF_  */
#line 847 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmThrowOnUnmappableCharDisabled); }
#line 5763 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 327: /* pinvAttr: pinvAttr FLAGS_ '(' int32 ')'  */
#line 848 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].int32)); }
#line 5769 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 328: /* methodName: _CTOR  */
#line 851 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.string) = newString(COR_CTOR_METHOD_NAME); }
#line 5775 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 329: /* methodName: _CCTOR  */
#line 852 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.string) = newString(COR_CCTOR_METHOD_NAME); }
#line 5781 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 330: /* methodName: dottedName  */
#line 853 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.string) = (yyvsp[0].string); }
#line 5787 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 331: /* paramAttr: %empty  */
#line 856 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 5793 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 332: /* paramAttr: paramAttr '[' IN_ ']'  */
#line 857 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdIn; }
#line 5799 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 333: /* paramAttr: paramAttr '[' OUT_ ']'  */
#line 858 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdOut; }
#line 5805 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 334: /* paramAttr: paramAttr '[' OPT_ ']'  */
#line 859 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdOptional; }
#line 5811 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 335: /* paramAttr: paramAttr '[' int32 ']'  */
#line 860 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) + 1; }
#line 5817 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 336: /* implAttr: %empty  */
#line 863 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (miIL | miManaged); }
#line 5823 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 337: /* implAttr: implAttr NATIVE_  */
#line 864 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miNative); }
#line 5829 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 338: /* implAttr: implAttr CIL_  */
#line 865 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miIL); }
#line 5835 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 339: /* implAttr: implAttr OPTIL_  */
#line 866 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miOPTIL); }
#line 5841 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 340: /* implAttr: implAttr MANAGED_  */
#line 867 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFFB) | miManaged); }
#line 5847 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 341: /* implAttr: implAttr UNMANAGED_  */
#line 868 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFFB) | miUnmanaged); }
#line 5853 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 342: /* implAttr: implAttr FORWARDREF_  */
#line 869 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miForwardRef); }
#line 5859 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 343: /* implAttr: implAttr PRESERVESIG_  */
#line 870 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miPreserveSig); }
#line 5865 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 344: /* implAttr: implAttr RUNTIME_  */
#line 871 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miRuntime); }
#line 5871 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 345: /* implAttr: implAttr INTERNALCALL_  */
#line 872 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miInternalCall); }
#line 5877 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 346: /* implAttr: implAttr SYNCHRONIZED_  */
#line 873 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miSynchronized); }
#line 5883 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 347: /* implAttr: implAttr NOINLINING_  */
#line 874 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miNoInlining); }
#line 5889 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 348: /* implAttr: implAttr AGGRESSIVEINLINING_  */
#line 875 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAggressiveInlining); }
#line 5895 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 349: /* implAttr: implAttr NOOPTIMIZATION_  */
#line 876 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miNoOptimization); }
#line 5901 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 350: /* implAttr: implAttr AGGRESSIVEOPTIMIZATION_  */
#line 877 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAggressiveOptimization); }
#line 5907 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 351: /* implAttr: implAttr ASYNC_  */
#line 878 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAsync); }
#line 5913 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 352: /* implAttr: implAttr FLAGS_ '(' int32 ')'  */
#line 879 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].int32)); }
#line 5919 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 353: /* localsHead: _LOCALS  */
#line 882 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->delArgNameList(PASM->m_firstArgName); PASM->m_firstArgName = NULL;PASM->m_lastArgName = NULL;
                                                            }
#line 5926 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 356: /* methodDecl: _EMITBYTE int32  */
#line 890 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->EmitByte((yyvsp[0].int32)); }
#line 5932 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 357: /* methodDecl: sehBlock  */
#line 891 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { delete PASM->m_SEHD; PASM->m_SEHD = PASM->m_SEHDstack.POP(); }
#line 5938 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 358: /* methodDecl: _MAXSTACK int32  */
#line 892 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->EmitMaxStack((yyvsp[0].int32)); }
#line 5944 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 359: /* methodDecl: localsHead '(' sigArgs0 ')'  */
#line 893 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, (yyvsp[-1].binstr)));
                                                            }
#line 5951 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 360: /* methodDecl: localsHead INIT_ '(' sigArgs0 ')'  */
#line 895 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->EmitZeroInit();
                                                              PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, (yyvsp[-1].binstr)));
                                                            }
#line 5959 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 361: /* methodDecl: _ENTRYPOINT  */
#line 898 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->EmitEntryPoint(); }
#line 5965 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 362: /* methodDecl: _ZEROINIT  */
#line 899 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->EmitZeroInit(); }
#line 5971 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 365: /* methodDecl: id ':'  */
#line 902 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->AddLabel(PASM->m_CurPC,(yyvsp[-1].string)); /*PASM->EmitLabel($1);*/ }
#line 5977 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 371: /* methodDecl: _EXPORT '[' int32 ']'  */
#line 908 ".\\src\\coreclr\\ilasm\\asmparse.y"
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
#line 5992 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 372: /* methodDecl: _EXPORT '[' int32 ']' AS_ id  */
#line 918 ".\\src\\coreclr\\ilasm\\asmparse.y"
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
#line 6007 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 373: /* methodDecl: _VTENTRY int32 ':' int32  */
#line 928 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->m_pCurMethod->m_wVTEntry = (WORD)(yyvsp[-2].int32);
                                                              PASM->m_pCurMethod->m_wVTSlot = (WORD)(yyvsp[0].int32); }
#line 6014 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 374: /* methodDecl: _OVERRIDE typeSpec DCOLON methodName  */
#line 931 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->AddMethodImpl((yyvsp[-2].token),(yyvsp[0].string),NULL,mdTokenNil,NULL,NULL); }
#line 6020 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 375: /* methodDecl: _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'  */
#line 934 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->AddMethodImpl((yyvsp[-6].token),(yyvsp[-4].string),
                                                              ((yyvsp[-3].int32)==0 ? parser->MakeSig((yyvsp[-8].int32),(yyvsp[-7].binstr),(yyvsp[-1].binstr)) :
                                                              parser->MakeSig((yyvsp[-8].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-7].binstr),(yyvsp[-1].binstr),(yyvsp[-3].int32)))
                                                              ,mdTokenNil,NULL,NULL);
                                                              PASM->ResetArgNameList();
                                                            }
#line 6031 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 377: /* methodDecl: _PARAM TYPE_ '[' int32 ']'  */
#line 941 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { if(((yyvsp[-1].int32) > 0) && ((yyvsp[-1].int32) <= (int)PASM->m_pCurMethod->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[(yyvsp[-1].int32)-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
#line 6041 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 378: /* methodDecl: _PARAM TYPE_ dottedName  */
#line 946 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { int n = PASM->m_pCurMethod->FindTyPar((yyvsp[0].string));
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",(yyvsp[0].string));
                                                            }
#line 6052 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 379: /* methodDecl: _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec  */
#line 952 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                        { PASM->m_pCurMethod->AddGenericParamConstraint((yyvsp[-3].int32), 0, (yyvsp[0].token)); }
#line 6058 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 380: /* methodDecl: _PARAM CONSTRAINT_ dottedName ',' typeSpec  */
#line 953 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                        { PASM->m_pCurMethod->AddGenericParamConstraint(0, (yyvsp[-2].string), (yyvsp[0].token)); }
#line 6064 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 381: /* methodDecl: _PARAM '[' int32 ']' initOpt  */
#line 956 ".\\src\\coreclr\\ilasm\\asmparse.y"
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
#line 6087 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 382: /* scopeBlock: scopeOpen methodDecls '}'  */
#line 976 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->m_pCurMethod->CloseScope(); }
#line 6093 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 383: /* scopeOpen: '{'  */
#line 979 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->m_pCurMethod->OpenScope(); }
#line 6099 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 387: /* tryBlock: tryHead scopeBlock  */
#line 990 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->m_SEHD->tryTo = PASM->m_CurPC; }
#line 6105 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 388: /* tryBlock: tryHead id TO_ id  */
#line 991 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->SetTryLabels((yyvsp[-2].string), (yyvsp[0].string)); }
#line 6111 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 389: /* tryBlock: tryHead int32 TO_ int32  */
#line 992 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { if(PASM->m_SEHD) {PASM->m_SEHD->tryFrom = (yyvsp[-2].int32);
                                                              PASM->m_SEHD->tryTo = (yyvsp[0].int32);} }
#line 6118 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 390: /* tryHead: _TRY  */
#line 996 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            { PASM->NewSEHDescriptor();
                                                              PASM->m_SEHD->tryFrom = PASM->m_CurPC; }
#line 6125 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 391: /* sehClause: catchClause handlerBlock  */
#line 1001 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitTry(); }
#line 6131 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 392: /* sehClause: filterClause handlerBlock  */
#line 1002 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitTry(); }
#line 6137 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 393: /* sehClause: finallyClause handlerBlock  */
#line 1003 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitTry(); }
#line 6143 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 394: /* sehClause: faultClause handlerBlock  */
#line 1004 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitTry(); }
#line 6149 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 395: /* filterClause: filterHead scopeBlock  */
#line 1008 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6155 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 396: /* filterClause: filterHead id  */
#line 1009 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->SetFilterLabel((yyvsp[0].string));
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6162 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 397: /* filterClause: filterHead int32  */
#line 1011 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->m_SEHD->sehFilter = (yyvsp[0].int32);
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6169 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 398: /* filterHead: FILTER_  */
#line 1015 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FILTER;
                                                               PASM->m_SEHD->sehFilter = PASM->m_CurPC; }
#line 6176 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 399: /* catchClause: CATCH_ typeSpec  */
#line 1019 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                            {  PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_NONE;
                                                               PASM->SetCatchClass((yyvsp[0].token));
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6184 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 400: /* finallyClause: FINALLY_  */
#line 1024 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FINALLY;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6191 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 401: /* faultClause: FAULT_  */
#line 1028 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FAULT;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6198 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 402: /* handlerBlock: scopeBlock  */
#line 1032 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->m_SEHD->sehHandlerTo = PASM->m_CurPC; }
#line 6204 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 403: /* handlerBlock: HANDLER_ id TO_ id  */
#line 1033 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->SetHandlerLabels((yyvsp[-2].string), (yyvsp[0].string)); }
#line 6210 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 404: /* handlerBlock: HANDLER_ int32 TO_ int32  */
#line 1034 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->m_SEHD->sehHandler = (yyvsp[-2].int32);
                                                               PASM->m_SEHD->sehHandlerTo = (yyvsp[0].int32); }
#line 6217 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 406: /* ddHead: _DATA tls id '='  */
#line 1042 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitDataLabel((yyvsp[-1].string)); }
#line 6223 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 408: /* tls: %empty  */
#line 1046 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->SetDataSection(); }
#line 6229 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 409: /* tls: TLS_  */
#line 1047 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->SetTLSSection(); }
#line 6235 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 410: /* tls: CIL_  */
#line 1048 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->SetILSection(); }
#line 6241 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 415: /* ddItemCount: %empty  */
#line 1059 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.int32) = 1; }
#line 6247 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 416: /* ddItemCount: '[' int32 ']'  */
#line 1060 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.int32) = (yyvsp[-1].int32);
                                                               if((yyvsp[-1].int32) <= 0) { PASM->report->error("Illegal item count: %d\n",(yyvsp[-1].int32));
                                                                  if(!PASM->OnErrGo) (yyval.int32) = 1; }}
#line 6255 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 417: /* ddItem: CHAR_ '*' '(' compQstring ')'  */
#line 1065 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitDataString((yyvsp[-1].binstr)); }
#line 6261 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 418: /* ddItem: '&' '(' id ')'  */
#line 1066 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitDD((yyvsp[-1].string)); }
#line 6267 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 419: /* ddItem: bytearrayhead bytes ')'  */
#line 1067 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitData((yyvsp[-1].binstr)->ptr(),(yyvsp[-1].binstr)->length()); }
#line 6273 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 420: /* ddItem: FLOAT32_ '(' float64 ')' ddItemCount  */
#line 1069 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { float f = (float) (*(yyvsp[-2].float64)); float* p = new (nothrow) float[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i < (yyvsp[0].int32); i++) p[i] = f;
                                                                 PASM->EmitData(p, sizeof(float)*(yyvsp[0].int32)); delete (yyvsp[-2].float64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(float)*(yyvsp[0].int32)); }
#line 6284 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 421: /* ddItem: FLOAT64_ '(' float64 ')' ddItemCount  */
#line 1076 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { double* p = new (nothrow) double[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = *((yyvsp[-2].float64));
                                                                 PASM->EmitData(p, sizeof(double)*(yyvsp[0].int32)); delete (yyvsp[-2].float64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(double)*(yyvsp[0].int32)); }
#line 6295 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 422: /* ddItem: INT64_ '(' int64 ')' ddItemCount  */
#line 1083 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { int64_t* p = new (nothrow) int64_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = *((yyvsp[-2].int64));
                                                                 PASM->EmitData(p, sizeof(int64_t)*(yyvsp[0].int32)); delete (yyvsp[-2].int64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int64_t)*(yyvsp[0].int32)); }
#line 6306 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 423: /* ddItem: INT32_ '(' int32 ')' ddItemCount  */
#line 1090 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { int32_t* p = new (nothrow) int32_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = (yyvsp[-2].int32);
                                                                 PASM->EmitData(p, sizeof(int32_t)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int32_t)*(yyvsp[0].int32)); }
#line 6317 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 424: /* ddItem: INT16_ '(' int32 ')' ddItemCount  */
#line 1097 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { int16_t i = (int16_t) (yyvsp[-2].int32); FAIL_UNLESS(i == (yyvsp[-2].int32), ("Value %d too big\n", (yyvsp[-2].int32)));
                                                               int16_t* p = new (nothrow) int16_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<(yyvsp[0].int32); j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(int16_t)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int16_t)*(yyvsp[0].int32)); }
#line 6329 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 425: /* ddItem: INT8_ '(' int32 ')' ddItemCount  */
#line 1105 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { int8_t i = (int8_t) (yyvsp[-2].int32); FAIL_UNLESS(i == (yyvsp[-2].int32), ("Value %d too big\n", (yyvsp[-2].int32)));
                                                               int8_t* p = new (nothrow) int8_t[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<(yyvsp[0].int32); j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(int8_t)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(int8_t)*(yyvsp[0].int32)); }
#line 6341 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 426: /* ddItem: FLOAT32_ ddItemCount  */
#line 1112 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(float)*(yyvsp[0].int32)); }
#line 6347 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 427: /* ddItem: FLOAT64_ ddItemCount  */
#line 1113 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(double)*(yyvsp[0].int32)); }
#line 6353 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 428: /* ddItem: INT64_ ddItemCount  */
#line 1114 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int64_t)*(yyvsp[0].int32)); }
#line 6359 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 429: /* ddItem: INT32_ ddItemCount  */
#line 1115 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int32_t)*(yyvsp[0].int32)); }
#line 6365 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 430: /* ddItem: INT16_ ddItemCount  */
#line 1116 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int16_t)*(yyvsp[0].int32)); }
#line 6371 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 431: /* ddItem: INT8_ ddItemCount  */
#line 1117 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(int8_t)*(yyvsp[0].int32)); }
#line 6377 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 432: /* fieldSerInit: FLOAT32_ '(' float64 ')'  */
#line 1121 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               float f = (float)(*(yyvsp[-1].float64));
                                                               (yyval.binstr)->appendInt32(*((int32_t*)&f)); delete (yyvsp[-1].float64); }
#line 6385 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 433: /* fieldSerInit: FLOAT64_ '(' float64 ')'  */
#line 1124 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].float64)); delete (yyvsp[-1].float64); }
#line 6392 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 434: /* fieldSerInit: FLOAT32_ '(' int32 ')'  */
#line 1126 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6399 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 435: /* fieldSerInit: FLOAT64_ '(' int64 ')'  */
#line 1128 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6406 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 436: /* fieldSerInit: INT64_ '(' int64 ')'  */
#line 1130 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6413 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 437: /* fieldSerInit: INT32_ '(' int32 ')'  */
#line 1132 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6420 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 438: /* fieldSerInit: INT16_ '(' int32 ')'  */
#line 1134 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6427 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 439: /* fieldSerInit: INT8_ '(' int32 ')'  */
#line 1136 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6434 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 440: /* fieldSerInit: UNSIGNED_ INT64_ '(' int64 ')'  */
#line 1138 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6441 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 441: /* fieldSerInit: UNSIGNED_ INT32_ '(' int32 ')'  */
#line 1140 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6448 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 442: /* fieldSerInit: UNSIGNED_ INT16_ '(' int32 ')'  */
#line 1142 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6455 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 443: /* fieldSerInit: UNSIGNED_ INT8_ '(' int32 ')'  */
#line 1144 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6462 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 444: /* fieldSerInit: UINT64_ '(' int64 ')'  */
#line 1146 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6469 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 445: /* fieldSerInit: UINT32_ '(' int32 ')'  */
#line 1148 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6476 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 446: /* fieldSerInit: UINT16_ '(' int32 ')'  */
#line 1150 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6483 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 447: /* fieldSerInit: UINT8_ '(' int32 ')'  */
#line 1152 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6490 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 448: /* fieldSerInit: CHAR_ '(' int32 ')'  */
#line 1154 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CHAR);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6497 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 449: /* fieldSerInit: BOOL_ '(' truefalse ')'  */
#line 1156 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_BOOLEAN);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32));}
#line 6504 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 450: /* fieldSerInit: bytearrayhead bytes ')'  */
#line 1158 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING);
                                                               (yyval.binstr)->append((yyvsp[-1].binstr)); delete (yyvsp[-1].binstr);}
#line 6511 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 451: /* bytearrayhead: BYTEARRAY_ '('  */
#line 1162 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { bParsingByteArray = TRUE; }
#line 6517 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 452: /* bytes: %empty  */
#line 1165 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6523 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 453: /* bytes: hexbytes  */
#line 1166 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6529 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 454: /* hexbytes: HEXBYTE  */
#line 1169 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { int8_t i = (int8_t) (yyvsp[0].int32); (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(i); }
#line 6535 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 455: /* hexbytes: hexbytes HEXBYTE  */
#line 1170 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { int8_t i = (int8_t) (yyvsp[0].int32); (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(i); }
#line 6541 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 456: /* fieldInit: fieldSerInit  */
#line 1174 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6547 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 457: /* fieldInit: compQstring  */
#line 1175 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = BinStrToUnicode((yyvsp[0].binstr),true); (yyval.binstr)->insertInt8(ELEMENT_TYPE_STRING);}
#line 6553 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 458: /* fieldInit: NULLREF_  */
#line 1176 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CLASS);
                                                               (yyval.binstr)->appendInt32(0); }
#line 6560 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 459: /* serInit: fieldSerInit  */
#line 1181 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6566 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 460: /* serInit: STRING_ '(' NULLREF_ ')'  */
#line 1182 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); (yyval.binstr)->appendInt8(0xFF); }
#line 6572 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 461: /* serInit: STRING_ '(' SQSTRING ')'  */
#line 1183 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[-1].string)); delete [] (yyvsp[-1].string);}
#line 6579 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 462: /* serInit: TYPE_ '(' CLASS_ SQSTRING ')'  */
#line 1185 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[-1].string)); delete [] (yyvsp[-1].string);}
#line 6586 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 463: /* serInit: TYPE_ '(' className ')'  */
#line 1187 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[-1].token)));}
#line 6593 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 464: /* serInit: TYPE_ '(' NULLREF_ ')'  */
#line 1189 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE); (yyval.binstr)->appendInt8(0xFF); }
#line 6599 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 465: /* serInit: OBJECT_ '(' serInit ')'  */
#line 1190 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);}
#line 6605 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 466: /* serInit: FLOAT32_ '[' int32 ']' '(' f32seq ')'  */
#line 1192 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_R4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6613 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 467: /* serInit: FLOAT64_ '[' int32 ']' '(' f64seq ')'  */
#line 1196 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6621 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 468: /* serInit: INT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1200 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6629 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 469: /* serInit: INT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1204 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6637 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 470: /* serInit: INT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1208 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6645 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 471: /* serInit: INT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1212 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6653 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 472: /* serInit: UINT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1216 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6661 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 473: /* serInit: UINT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1220 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6669 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 474: /* serInit: UINT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1224 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6677 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 475: /* serInit: UINT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1228 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6685 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 476: /* serInit: UNSIGNED_ INT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1232 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6693 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 477: /* serInit: UNSIGNED_ INT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1236 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6701 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 478: /* serInit: UNSIGNED_ INT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1240 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6709 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 479: /* serInit: UNSIGNED_ INT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1244 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6717 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 480: /* serInit: CHAR_ '[' int32 ']' '(' i16seq ')'  */
#line 1248 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_CHAR);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6725 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 481: /* serInit: BOOL_ '[' int32 ']' '(' boolSeq ')'  */
#line 1252 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_BOOLEAN);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6733 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 482: /* serInit: STRING_ '[' int32 ']' '(' sqstringSeq ')'  */
#line 1256 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_STRING);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6741 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 483: /* serInit: TYPE_ '[' int32 ']' '(' classSeq ')'  */
#line 1260 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TYPE);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6749 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 484: /* serInit: OBJECT_ '[' int32 ']' '(' objSeq ')'  */
#line 1264 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6757 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 485: /* f32seq: %empty  */
#line 1270 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6763 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 486: /* f32seq: f32seq float64  */
#line 1271 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               float f = (float) (*(yyvsp[0].float64)); (yyval.binstr)->appendInt32(*((int32_t*)&f)); delete (yyvsp[0].float64); }
#line 6770 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 487: /* f32seq: f32seq int32  */
#line 1273 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 6777 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 488: /* f64seq: %empty  */
#line 1277 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6783 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 489: /* f64seq: f64seq float64  */
#line 1278 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[0].float64)); delete (yyvsp[0].float64); }
#line 6790 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 490: /* f64seq: f64seq int64  */
#line 1280 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[0].int64)); delete (yyvsp[0].int64); }
#line 6797 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 491: /* i64seq: %empty  */
#line 1284 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6803 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 492: /* i64seq: i64seq int64  */
#line 1285 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((int64_t *)(yyvsp[0].int64)); delete (yyvsp[0].int64); }
#line 6810 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 493: /* i32seq: %empty  */
#line 1289 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6816 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 494: /* i32seq: i32seq int32  */
#line 1290 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt32((yyvsp[0].int32));}
#line 6822 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 495: /* i16seq: %empty  */
#line 1293 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6828 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 496: /* i16seq: i16seq int32  */
#line 1294 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt16((yyvsp[0].int32));}
#line 6834 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 497: /* i8seq: %empty  */
#line 1297 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6840 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 498: /* i8seq: i8seq int32  */
#line 1298 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8((yyvsp[0].int32)); }
#line 6846 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 499: /* boolSeq: %empty  */
#line 1301 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6852 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 500: /* boolSeq: boolSeq truefalse  */
#line 1302 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt8((yyvsp[0].int32));}
#line 6859 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 501: /* sqstringSeq: %empty  */
#line 1306 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6865 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 502: /* sqstringSeq: sqstringSeq NULLREF_  */
#line 1307 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(0xFF); }
#line 6871 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 503: /* sqstringSeq: sqstringSeq SQSTRING  */
#line 1308 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); delete [] (yyvsp[0].string);}
#line 6878 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 504: /* classSeq: %empty  */
#line 1312 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6884 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 505: /* classSeq: classSeq NULLREF_  */
#line 1313 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(0xFF); }
#line 6890 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 506: /* classSeq: classSeq CLASS_ SQSTRING  */
#line 1314 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); delete [] (yyvsp[0].string);}
#line 6897 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 507: /* classSeq: classSeq className  */
#line 1316 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[0].token)));}
#line 6904 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 508: /* objSeq: %empty  */
#line 1320 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6910 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 509: /* objSeq: objSeq serInit  */
#line 1321 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 6916 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 510: /* methodSpec: METHOD_  */
#line 1325 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { parser->m_ANSFirst.PUSH(PASM->m_firstArgName);
                                                               parser->m_ANSLast.PUSH(PASM->m_lastArgName);
                                                               PASM->m_firstArgName = NULL;
                                                               PASM->m_lastArgName = NULL; }
#line 6925 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 511: /* instr_none: INSTR_NONE  */
#line 1331 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6931 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 512: /* instr_var: INSTR_VAR  */
#line 1334 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6937 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 513: /* instr_i: INSTR_I  */
#line 1337 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6943 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 514: /* instr_i8: INSTR_I8  */
#line 1340 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6949 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 515: /* instr_r: INSTR_R  */
#line 1343 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6955 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 516: /* instr_brtarget: INSTR_BRTARGET  */
#line 1346 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6961 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 517: /* instr_method: INSTR_METHOD  */
#line 1349 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode));
                                                               if((!PASM->OnErrGo)&&
                                                               (((yyvsp[0].opcode) == CEE_NEWOBJ)||
                                                                ((yyvsp[0].opcode) == CEE_CALLVIRT)))
                                                                  iCallConv = IMAGE_CEE_CS_CALLCONV_HASTHIS;
                                                             }
#line 6972 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 518: /* instr_field: INSTR_FIELD  */
#line 1357 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6978 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 519: /* instr_type: INSTR_TYPE  */
#line 1360 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6984 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 520: /* instr_string: INSTR_STRING  */
#line 1363 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6990 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 521: /* instr_sig: INSTR_SIG  */
#line 1366 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6996 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 522: /* instr_tok: INSTR_TOK  */
#line 1369 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); iOpcodeLen = PASM->OpcodeLen((yyval.instr)); }
#line 7002 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 523: /* instr_switch: INSTR_SWITCH  */
#line 1372 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 7008 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 524: /* instr_r_head: instr_r '('  */
#line 1375 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.instr) = (yyvsp[-1].instr); bParsingByteArray = TRUE; }
#line 7014 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 525: /* instr: instr_none  */
#line 1379 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitOpcode((yyvsp[0].instr)); }
#line 7020 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 526: /* instr: instr_var int32  */
#line 1380 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitInstrVar((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 7026 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 527: /* instr: instr_var id  */
#line 1381 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitInstrVarByName((yyvsp[-1].instr), (yyvsp[0].string)); }
#line 7032 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 528: /* instr: instr_i int32  */
#line 1382 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 7038 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 529: /* instr: instr_i8 int64  */
#line 1383 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitInstrI8((yyvsp[-1].instr), (yyvsp[0].int64)); }
#line 7044 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 530: /* instr: instr_r float64  */
#line 1384 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitInstrR((yyvsp[-1].instr), (yyvsp[0].float64)); delete ((yyvsp[0].float64));}
#line 7050 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 531: /* instr: instr_r int64  */
#line 1385 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { double f = (double) (*(yyvsp[0].int64)); PASM->EmitInstrR((yyvsp[-1].instr), &f); }
#line 7056 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 532: /* instr: instr_r_head bytes ')'  */
#line 1386 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { unsigned L = (yyvsp[-1].binstr)->length();
                                                               FAIL_UNLESS(L >= sizeof(float), ("%d hexbytes, must be at least %d\n",
                                                                           L,sizeof(float)));
                                                               if(L < sizeof(float)) {YYERROR; }
                                                               else {
                                                                   double f = (L >= sizeof(double)) ? *((double *)((yyvsp[-1].binstr)->ptr()))
                                                                                    : (double)(*(float *)((yyvsp[-1].binstr)->ptr()));
                                                                   PASM->EmitInstrR((yyvsp[-2].instr),&f); }
                                                               delete (yyvsp[-1].binstr); }
#line 7070 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 533: /* instr: instr_brtarget int32  */
#line 1395 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitInstrBrOffset((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 7076 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 534: /* instr: instr_brtarget id  */
#line 1396 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitInstrBrTarget((yyvsp[-1].instr), (yyvsp[0].string)); }
#line 7082 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 535: /* instr: instr_method methodRef  */
#line 1398 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->SetMemberRefFixup((yyvsp[0].token),PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),(yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iCallConv = 0;
                                                             }
#line 7093 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 536: /* instr: instr_field type typeSpec DCOLON dottedName  */
#line 1405 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef((yyvsp[-2].token), (yyvsp[0].string), (yyvsp[-3].binstr));
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-4].instr)));
                                                               PASM->EmitInstrI((yyvsp[-4].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7105 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 537: /* instr: instr_field type dottedName  */
#line 1413 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyvsp[-1].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef(mdTokenNil, (yyvsp[0].string), (yyvsp[-1].binstr));
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-2].instr)));
                                                               PASM->EmitInstrI((yyvsp[-2].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7117 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 538: /* instr: instr_field mdtoken  */
#line 1420 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { mdToken mr = (yyvsp[0].token);
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7128 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 539: /* instr: instr_field TYPEDEF_F  */
#line 1426 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { mdToken mr = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7139 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 540: /* instr: instr_field TYPEDEF_MR  */
#line 1432 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { mdToken mr = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7150 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 541: /* instr: instr_type typeSpec  */
#line 1438 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr), (yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7159 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 542: /* instr: instr_string compQstring  */
#line 1442 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-1].instr), (yyvsp[0].binstr),TRUE); }
#line 7165 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 543: /* instr: instr_string ANSI_ '(' compQstring ')'  */
#line 1444 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-4].instr), (yyvsp[-1].binstr),FALSE); }
#line 7171 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 544: /* instr: instr_string bytearrayhead bytes ')'  */
#line 1446 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-3].instr), (yyvsp[-1].binstr),FALSE,TRUE); }
#line 7177 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 545: /* instr: instr_sig callConv type '(' sigArgs0 ')'  */
#line 1448 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitInstrSig((yyvsp[-5].instr), parser->MakeSig((yyvsp[-4].int32), (yyvsp[-3].binstr), (yyvsp[-1].binstr)));
                                                               PASM->ResetArgNameList();
                                                             }
#line 7185 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 546: /* instr: instr_tok ownerType  */
#line 1452 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr),(yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iOpcodeLen = 0;
                                                             }
#line 7195 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 547: /* instr: instr_switch '(' labels ')'  */
#line 1457 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { PASM->EmitInstrSwitch((yyvsp[-3].instr), (yyvsp[-1].labels)); }
#line 7201 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 548: /* labels: %empty  */
#line 1460 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.labels) = 0; }
#line 7207 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 549: /* labels: id ',' labels  */
#line 1461 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.labels) = new Labels((yyvsp[-2].string), (yyvsp[0].labels), TRUE); }
#line 7213 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 550: /* labels: int32 ',' labels  */
#line 1462 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.labels) = new Labels((char *)(UINT_PTR)(yyvsp[-2].int32), (yyvsp[0].labels), FALSE); }
#line 7219 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 551: /* labels: id  */
#line 1463 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.labels) = new Labels((yyvsp[0].string), NULL, TRUE); }
#line 7225 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 552: /* labels: int32  */
#line 1464 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.labels) = new Labels((char *)(UINT_PTR)(yyvsp[0].int32), NULL, FALSE); }
#line 7231 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 553: /* tyArgs0: %empty  */
#line 1468 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = NULL; }
#line 7237 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 554: /* tyArgs0: '<' tyArgs1 '>'  */
#line 1469 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 7243 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 555: /* tyArgs1: %empty  */
#line 1472 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = NULL; }
#line 7249 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 556: /* tyArgs1: tyArgs2  */
#line 1473 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7255 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 557: /* tyArgs2: type  */
#line 1476 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7261 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 558: /* tyArgs2: tyArgs2 ',' type  */
#line 1477 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 7267 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 559: /* sigArgs0: %empty  */
#line 1481 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 7273 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 560: /* sigArgs0: sigArgs1  */
#line 1482 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr);}
#line 7279 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 561: /* sigArgs1: sigArg  */
#line 1485 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7285 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 562: /* sigArgs1: sigArgs1 ',' sigArg  */
#line 1486 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 7291 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 563: /* sigArg: ELLIPSIS  */
#line 1489 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_SENTINEL); }
#line 7297 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 564: /* sigArg: paramAttr type marshalClause  */
#line 1490 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[-1].binstr)); PASM->addArgName(NULL, (yyvsp[-1].binstr), (yyvsp[0].binstr), (yyvsp[-2].int32)); }
#line 7303 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 565: /* sigArg: paramAttr type marshalClause id  */
#line 1491 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[-2].binstr)); PASM->addArgName((yyvsp[0].string), (yyvsp[-2].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32));}
#line 7309 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 566: /* className: '[' dottedName ']' slashedName  */
#line 1495 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(PASM->GetAsmRef((yyvsp[-2].string)), (yyvsp[0].string), NULL); delete[] (yyvsp[-2].string);}
#line 7315 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 567: /* className: '[' mdtoken ']' slashedName  */
#line 1496 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef((yyvsp[-2].token), (yyvsp[0].string), NULL); }
#line 7321 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 568: /* className: '[' '*' ']' slashedName  */
#line 1497 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(mdTokenNil, (yyvsp[0].string), NULL); }
#line 7327 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 569: /* className: '[' _MODULE dottedName ']' slashedName  */
#line 1498 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                   { (yyval.token) = PASM->ResolveClassRef(PASM->GetModRef((yyvsp[-2].string)),(yyvsp[0].string), NULL); delete[] (yyvsp[-2].string);}
#line 7333 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 570: /* className: slashedName  */
#line 1499 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(1,(yyvsp[0].string),NULL); }
#line 7339 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 571: /* className: mdtoken  */
#line 1500 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].token); }
#line 7345 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 572: /* className: TYPEDEF_T  */
#line 1501 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 7351 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 573: /* className: _THIS  */
#line 1502 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) (yyval.token) = PASM->m_pCurClass->m_cl;
                                                                else { (yyval.token) = 0; PASM->report->error(".this outside class scope\n"); }
                                                              }
#line 7359 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 574: /* className: _BASE  */
#line 1505 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) {
                                                                  (yyval.token) = PASM->m_pCurClass->m_crExtends;
                                                                  if(RidFromToken((yyval.token)) == 0)
                                                                    PASM->report->error(".base undefined\n");
                                                                } else { (yyval.token) = 0; PASM->report->error(".base outside class scope\n"); }
                                                              }
#line 7370 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 575: /* className: _NESTER  */
#line 1511 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) {
                                                                  if(PASM->m_pCurClass->m_pEncloser != NULL) (yyval.token) = PASM->m_pCurClass->m_pEncloser->m_cl;
                                                                  else { (yyval.token) = 0; PASM->report->error(".nester undefined\n"); }
                                                                } else { (yyval.token) = 0; PASM->report->error(".nester outside class scope\n"); }
                                                              }
#line 7380 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 576: /* slashedName: dottedName  */
#line 1518 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 7386 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 577: /* slashedName: slashedName '/' dottedName  */
#line 1519 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.string) = newStringWDel((yyvsp[-2].string), NESTING_SEP, (yyvsp[0].string)); }
#line 7392 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 578: /* typeSpec: className  */
#line 1522 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].token);}
#line 7398 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 579: /* typeSpec: '[' dottedName ']'  */
#line 1523 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.token) = PASM->GetAsmRef((yyvsp[-1].string)); delete[] (yyvsp[-1].string);}
#line 7404 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 580: /* typeSpec: '[' _MODULE dottedName ']'  */
#line 1524 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.token) = PASM->GetModRef((yyvsp[-1].string)); delete[] (yyvsp[-1].string);}
#line 7410 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 581: /* typeSpec: type  */
#line 1525 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.token) = PASM->ResolveTypeSpec((yyvsp[0].binstr)); }
#line 7416 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 582: /* nativeType: %empty  */
#line 1529 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); }
#line 7422 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 583: /* nativeType: CUSTOM_ '(' compQstring ',' compQstring ',' compQstring ',' compQstring ')'  */
#line 1531 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt((yyval.binstr),(yyvsp[-7].binstr)->length()); (yyval.binstr)->append((yyvsp[-7].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-5].binstr)->length()); (yyval.binstr)->append((yyvsp[-5].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].binstr)->length()); (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].binstr)->length()); (yyval.binstr)->append((yyvsp[-1].binstr));
                                                                PASM->report->warn("Deprecated 4-string form of custom marshaler, first two strings ignored\n");}
#line 7433 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 584: /* nativeType: CUSTOM_ '(' compQstring ',' compQstring ')'  */
#line 1538 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].binstr)->length()); (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].binstr)->length()); (yyval.binstr)->append((yyvsp[-1].binstr)); }
#line 7443 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 585: /* nativeType: FIXED_ SYSSTRING_ '[' int32 ']'  */
#line 1543 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FIXEDSYSSTRING);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32)); }
#line 7450 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 586: /* nativeType: FIXED_ ARRAY_ '[' int32 ']' nativeType  */
#line 1546 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FIXEDARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-2].int32)); (yyval.binstr)->append((yyvsp[0].binstr)); }
#line 7457 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 587: /* nativeType: VARIANT_  */
#line 1548 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VARIANT);
                                                                PASM->report->warn("Deprecated native type 'variant'\n"); }
#line 7464 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 588: /* nativeType: CURRENCY_  */
#line 1550 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CURRENCY); }
#line 7470 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 589: /* nativeType: SYSCHAR_  */
#line 1551 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SYSCHAR);
                                                                PASM->report->warn("Deprecated native type 'syschar'\n"); }
#line 7477 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 590: /* nativeType: VOID_  */
#line 1553 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VOID);
                                                                PASM->report->warn("Deprecated native type 'void'\n"); }
#line 7484 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 591: /* nativeType: BOOL_  */
#line 1555 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BOOLEAN); }
#line 7490 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 592: /* nativeType: INT8_  */
#line 1556 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I1); }
#line 7496 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 593: /* nativeType: INT16_  */
#line 1557 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I2); }
#line 7502 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 594: /* nativeType: INT32_  */
#line 1558 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I4); }
#line 7508 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 595: /* nativeType: INT64_  */
#line 1559 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I8); }
#line 7514 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 596: /* nativeType: FLOAT32_  */
#line 1560 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_R4); }
#line 7520 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 597: /* nativeType: FLOAT64_  */
#line 1561 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_R8); }
#line 7526 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 598: /* nativeType: ERROR_  */
#line 1562 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ERROR); }
#line 7532 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 599: /* nativeType: UNSIGNED_ INT8_  */
#line 1563 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U1); }
#line 7538 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 600: /* nativeType: UNSIGNED_ INT16_  */
#line 1564 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U2); }
#line 7544 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 601: /* nativeType: UNSIGNED_ INT32_  */
#line 1565 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U4); }
#line 7550 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 602: /* nativeType: UNSIGNED_ INT64_  */
#line 1566 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U8); }
#line 7556 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 603: /* nativeType: UINT8_  */
#line 1567 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U1); }
#line 7562 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 604: /* nativeType: UINT16_  */
#line 1568 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U2); }
#line 7568 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 605: /* nativeType: UINT32_  */
#line 1569 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U4); }
#line 7574 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 606: /* nativeType: UINT64_  */
#line 1570 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U8); }
#line 7580 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 607: /* nativeType: nativeType '*'  */
#line 1571 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(NATIVE_TYPE_PTR);
                                                                PASM->report->warn("Deprecated native type '*'\n"); }
#line 7587 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 608: /* nativeType: nativeType '[' ']'  */
#line 1573 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY); }
#line 7594 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 609: /* nativeType: nativeType '[' int32 ']'  */
#line 1575 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-3].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32));
                                                                corEmitInt((yyval.binstr),0); }
#line 7604 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 610: /* nativeType: nativeType '[' int32 '+' int32 ']'  */
#line 1580 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-5].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32));
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].int32));
                                                                corEmitInt((yyval.binstr),ntaSizeParamIndexSpecified); }
#line 7614 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 611: /* nativeType: nativeType '[' '+' int32 ']'  */
#line 1585 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-4].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32)); }
#line 7622 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 612: /* nativeType: DECIMAL_  */
#line 1588 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_DECIMAL);
                                                                PASM->report->warn("Deprecated native type 'decimal'\n"); }
#line 7629 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 613: /* nativeType: DATE_  */
#line 1590 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_DATE);
                                                                PASM->report->warn("Deprecated native type 'date'\n"); }
#line 7636 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 614: /* nativeType: BSTR_  */
#line 1592 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BSTR); }
#line 7642 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 615: /* nativeType: LPSTR_  */
#line 1593 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPSTR); }
#line 7648 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 616: /* nativeType: LPWSTR_  */
#line 1594 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPWSTR); }
#line 7654 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 617: /* nativeType: LPTSTR_  */
#line 1595 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPTSTR); }
#line 7660 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 618: /* nativeType: OBJECTREF_  */
#line 1596 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_OBJECTREF);
                                                                PASM->report->warn("Deprecated native type 'objectref'\n"); }
#line 7667 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 619: /* nativeType: IUNKNOWN_ iidParamIndex  */
#line 1598 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_IUNKNOWN);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7674 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 620: /* nativeType: IDISPATCH_ iidParamIndex  */
#line 1600 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_IDISPATCH);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7681 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 621: /* nativeType: STRUCT_  */
#line 1602 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_STRUCT); }
#line 7687 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 622: /* nativeType: INTERFACE_ iidParamIndex  */
#line 1603 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_INTF);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7694 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 623: /* nativeType: SAFEARRAY_ variantType  */
#line 1605 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[0].int32));
                                                                corEmitInt((yyval.binstr),0);}
#line 7702 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 624: /* nativeType: SAFEARRAY_ variantType ',' compQstring  */
#line 1608 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                 { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-2].int32));
                                                                corEmitInt((yyval.binstr),(yyvsp[0].binstr)->length()); (yyval.binstr)->append((yyvsp[0].binstr)); }
#line 7710 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 625: /* nativeType: INT_  */
#line 1612 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_INT); }
#line 7716 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 626: /* nativeType: UNSIGNED_ INT_  */
#line 1613 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_UINT); }
#line 7722 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 627: /* nativeType: UINT_  */
#line 1614 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_UINT); }
#line 7728 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 628: /* nativeType: NESTED_ STRUCT_  */
#line 1615 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_NESTEDSTRUCT);
                                                                PASM->report->warn("Deprecated native type 'nested struct'\n"); }
#line 7735 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 629: /* nativeType: BYVALSTR_  */
#line 1617 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BYVALSTR); }
#line 7741 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 630: /* nativeType: ANSI_ BSTR_  */
#line 1618 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ANSIBSTR); }
#line 7747 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 631: /* nativeType: TBSTR_  */
#line 1619 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_TBSTR); }
#line 7753 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 632: /* nativeType: VARIANT_ BOOL_  */
#line 1620 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VARIANTBOOL); }
#line 7759 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 633: /* nativeType: METHOD_  */
#line 1621 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FUNC); }
#line 7765 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 634: /* nativeType: AS_ ANY_  */
#line 1622 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ASANY); }
#line 7771 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 635: /* nativeType: LPSTRUCT_  */
#line 1623 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPSTRUCT); }
#line 7777 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 636: /* nativeType: TYPEDEF_TS  */
#line 1624 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[0].tdd)->m_pbsTypeSpec); }
#line 7783 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 637: /* iidParamIndex: %empty  */
#line 1627 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = -1; }
#line 7789 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 638: /* iidParamIndex: '(' IIDPARAM_ '=' int32 ')'  */
#line 1628 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32); }
#line 7795 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 639: /* variantType: %empty  */
#line 1631 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_EMPTY; }
#line 7801 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 640: /* variantType: NULL_  */
#line 1632 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_NULL; }
#line 7807 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 641: /* variantType: VARIANT_  */
#line 1633 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_VARIANT; }
#line 7813 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 642: /* variantType: CURRENCY_  */
#line 1634 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_CY; }
#line 7819 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 643: /* variantType: VOID_  */
#line 1635 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_VOID; }
#line 7825 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 644: /* variantType: BOOL_  */
#line 1636 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_BOOL; }
#line 7831 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 645: /* variantType: INT8_  */
#line 1637 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_I1; }
#line 7837 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 646: /* variantType: INT16_  */
#line 1638 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_I2; }
#line 7843 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 647: /* variantType: INT32_  */
#line 1639 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_I4; }
#line 7849 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 648: /* variantType: INT64_  */
#line 1640 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_I8; }
#line 7855 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 649: /* variantType: FLOAT32_  */
#line 1641 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_R4; }
#line 7861 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 650: /* variantType: FLOAT64_  */
#line 1642 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_R8; }
#line 7867 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 651: /* variantType: UNSIGNED_ INT8_  */
#line 1643 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_UI1; }
#line 7873 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 652: /* variantType: UNSIGNED_ INT16_  */
#line 1644 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_UI2; }
#line 7879 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 653: /* variantType: UNSIGNED_ INT32_  */
#line 1645 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_UI4; }
#line 7885 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 654: /* variantType: UNSIGNED_ INT64_  */
#line 1646 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_UI8; }
#line 7891 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 655: /* variantType: UINT8_  */
#line 1647 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_UI1; }
#line 7897 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 656: /* variantType: UINT16_  */
#line 1648 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_UI2; }
#line 7903 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 657: /* variantType: UINT32_  */
#line 1649 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_UI4; }
#line 7909 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 658: /* variantType: UINT64_  */
#line 1650 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_UI8; }
#line 7915 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 659: /* variantType: '*'  */
#line 1651 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_PTR; }
#line 7921 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 660: /* variantType: variantType '[' ']'  */
#line 1652 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-2].int32) | VT_ARRAY; }
#line 7927 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 661: /* variantType: variantType VECTOR_  */
#line 1653 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32) | VT_VECTOR; }
#line 7933 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 662: /* variantType: variantType '&'  */
#line 1654 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32) | VT_BYREF; }
#line 7939 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 663: /* variantType: DECIMAL_  */
#line 1655 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_DECIMAL; }
#line 7945 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 664: /* variantType: DATE_  */
#line 1656 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_DATE; }
#line 7951 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 665: /* variantType: BSTR_  */
#line 1657 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_BSTR; }
#line 7957 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 666: /* variantType: LPSTR_  */
#line 1658 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_LPSTR; }
#line 7963 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 667: /* variantType: LPWSTR_  */
#line 1659 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_LPWSTR; }
#line 7969 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 668: /* variantType: IUNKNOWN_  */
#line 1660 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_UNKNOWN; }
#line 7975 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 669: /* variantType: IDISPATCH_  */
#line 1661 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_DISPATCH; }
#line 7981 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 670: /* variantType: SAFEARRAY_  */
#line 1662 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_SAFEARRAY; }
#line 7987 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 671: /* variantType: INT_  */
#line 1663 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_INT; }
#line 7993 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 672: /* variantType: UNSIGNED_ INT_  */
#line 1664 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_UINT; }
#line 7999 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 673: /* variantType: UINT_  */
#line 1665 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_UINT; }
#line 8005 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 674: /* variantType: ERROR_  */
#line 1666 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_ERROR; }
#line 8011 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 675: /* variantType: HRESULT_  */
#line 1667 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_HRESULT; }
#line 8017 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 676: /* variantType: CARRAY_  */
#line 1668 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_CARRAY; }
#line 8023 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 677: /* variantType: USERDEFINED_  */
#line 1669 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_USERDEFINED; }
#line 8029 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 678: /* variantType: RECORD_  */
#line 1670 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_RECORD; }
#line 8035 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 679: /* variantType: FILETIME_  */
#line 1671 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_FILETIME; }
#line 8041 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 680: /* variantType: BLOB_  */
#line 1672 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_BLOB; }
#line 8047 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 681: /* variantType: STREAM_  */
#line 1673 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_STREAM; }
#line 8053 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 682: /* variantType: STORAGE_  */
#line 1674 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_STORAGE; }
#line 8059 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 683: /* variantType: STREAMED_OBJECT_  */
#line 1675 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_STREAMED_OBJECT; }
#line 8065 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 684: /* variantType: STORED_OBJECT_  */
#line 1676 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_STORED_OBJECT; }
#line 8071 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 685: /* variantType: BLOB_OBJECT_  */
#line 1677 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_BLOB_OBJECT; }
#line 8077 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 686: /* variantType: CF_  */
#line 1678 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_CF; }
#line 8083 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 687: /* variantType: CLSID_  */
#line 1679 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = VT_CLSID; }
#line 8089 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 688: /* type: CLASS_ className  */
#line 1683 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { if((yyvsp[0].token) == PASM->m_tkSysString)
                                                                {     (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); }
                                                                else if((yyvsp[0].token) == PASM->m_tkSysObject)
                                                                {     (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_OBJECT); }
                                                                else
                                                                 (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CLASS, (yyvsp[0].token)); }
#line 8100 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 689: /* type: OBJECT_  */
#line 1689 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_OBJECT); }
#line 8106 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 690: /* type: VALUE_ CLASS_ className  */
#line 1690 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, (yyvsp[0].token)); }
#line 8112 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 691: /* type: VALUETYPE_ className  */
#line 1691 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, (yyvsp[0].token)); }
#line 8118 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 692: /* type: type '[' ']'  */
#line 1692 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 8124 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 693: /* type: type '[' bounds1 ']'  */
#line 1693 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeArray(ELEMENT_TYPE_ARRAY, (yyvsp[-3].binstr), (yyvsp[-1].binstr)); }
#line 8130 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 694: /* type: type '&'  */
#line 1694 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_BYREF); }
#line 8136 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 695: /* type: type '*'  */
#line 1695 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_PTR); }
#line 8142 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 696: /* type: type PINNED_  */
#line 1696 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_PINNED); }
#line 8148 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 697: /* type: type MODREQ_ '(' typeSpec ')'  */
#line 1697 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_REQD, (yyvsp[-1].token));
                                                                (yyval.binstr)->append((yyvsp[-4].binstr)); }
#line 8155 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 698: /* type: type MODOPT_ '(' typeSpec ')'  */
#line 1699 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_OPT, (yyvsp[-1].token));
                                                                (yyval.binstr)->append((yyvsp[-4].binstr)); }
#line 8162 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 699: /* type: methodSpec callConv type '*' '(' sigArgs0 ')'  */
#line 1702 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = parser->MakeSig((yyvsp[-5].int32), (yyvsp[-4].binstr), (yyvsp[-1].binstr));
                                                                (yyval.binstr)->insertInt8(ELEMENT_TYPE_FNPTR);
                                                                PASM->delArgNameList(PASM->m_firstArgName);
                                                                PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                                PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                              }
#line 8173 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 700: /* type: type '<' tyArgs1 '>'  */
#line 1708 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { if((yyvsp[-1].binstr) == NULL) (yyval.binstr) = (yyvsp[-3].binstr);
                                                                else {
                                                                  (yyval.binstr) = new BinStr();
                                                                  (yyval.binstr)->appendInt8(ELEMENT_TYPE_GENERICINST);
                                                                  (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                  corEmitInt((yyval.binstr), corCountArgs((yyvsp[-1].binstr)));
                                                                  (yyval.binstr)->append((yyvsp[-1].binstr)); delete (yyvsp[-3].binstr); delete (yyvsp[-1].binstr); }}
#line 8185 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 701: /* type: '!' '!' int32  */
#line 1715 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { //if(PASM->m_pCurMethod)  {
                                                                //  if(($3 < 0)||((DWORD)$3 >= PASM->m_pCurMethod->m_NumTyPars))
                                                                //    PASM->report->error("Invalid method type parameter '%d'\n",$3);
                                                                  (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_MVAR); corEmitInt((yyval.binstr), (yyvsp[0].int32));
                                                                //} else PASM->report->error("Method type parameter '%d' outside method scope\n",$3);
                                                              }
#line 8196 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 702: /* type: '!' int32  */
#line 1721 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { //if(PASM->m_pCurClass)  {
                                                                //  if(($2 < 0)||((DWORD)$2 >= PASM->m_pCurClass->m_NumTyPars))
                                                                //    PASM->report->error("Invalid type parameter '%d'\n",$2);
                                                                  (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_VAR); corEmitInt((yyval.binstr), (yyvsp[0].int32));
                                                                //} else PASM->report->error("Type parameter '%d' outside class scope\n",$2);
                                                              }
#line 8207 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 703: /* type: '!' '!' dottedName  */
#line 1727 ".\\src\\coreclr\\ilasm\\asmparse.y"
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
#line 8227 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 704: /* type: '!' dottedName  */
#line 1742 ".\\src\\coreclr\\ilasm\\asmparse.y"
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
#line 8247 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 705: /* type: TYPEDREF_  */
#line 1757 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_TYPEDBYREF); }
#line 8253 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 706: /* type: VOID_  */
#line 1758 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_VOID); }
#line 8259 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 707: /* type: NATIVE_ INT_  */
#line 1759 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I); }
#line 8265 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 708: /* type: NATIVE_ UNSIGNED_ INT_  */
#line 1760 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U); }
#line 8271 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 709: /* type: NATIVE_ UINT_  */
#line 1761 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U); }
#line 8277 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 710: /* type: simpleType  */
#line 1762 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 8283 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 711: /* type: ELLIPSIS type  */
#line 1763 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                               { (yyval.binstr) = (yyvsp[0].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SENTINEL); }
#line 8289 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 712: /* simpleType: CHAR_  */
#line 1766 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CHAR); }
#line 8295 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 713: /* simpleType: STRING_  */
#line 1767 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); }
#line 8301 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 714: /* simpleType: BOOL_  */
#line 1768 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_BOOLEAN); }
#line 8307 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 715: /* simpleType: INT8_  */
#line 1769 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I1); }
#line 8313 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 716: /* simpleType: INT16_  */
#line 1770 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I2); }
#line 8319 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 717: /* simpleType: INT32_  */
#line 1771 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I4); }
#line 8325 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 718: /* simpleType: INT64_  */
#line 1772 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I8); }
#line 8331 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 719: /* simpleType: FLOAT32_  */
#line 1773 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4); }
#line 8337 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 720: /* simpleType: FLOAT64_  */
#line 1774 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8); }
#line 8343 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 721: /* simpleType: UNSIGNED_ INT8_  */
#line 1775 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1); }
#line 8349 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 722: /* simpleType: UNSIGNED_ INT16_  */
#line 1776 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2); }
#line 8355 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 723: /* simpleType: UNSIGNED_ INT32_  */
#line 1777 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4); }
#line 8361 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 724: /* simpleType: UNSIGNED_ INT64_  */
#line 1778 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8); }
#line 8367 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 725: /* simpleType: UINT8_  */
#line 1779 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1); }
#line 8373 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 726: /* simpleType: UINT16_  */
#line 1780 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2); }
#line 8379 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 727: /* simpleType: UINT32_  */
#line 1781 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4); }
#line 8385 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 728: /* simpleType: UINT64_  */
#line 1782 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8); }
#line 8391 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 729: /* simpleType: TYPEDEF_TS  */
#line 1783 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[0].tdd)->m_pbsTypeSpec); }
#line 8397 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 730: /* bounds1: bound  */
#line 1786 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 8403 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 731: /* bounds1: bounds1 ',' bound  */
#line 1787 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyvsp[-2].binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 8409 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 732: /* bound: %empty  */
#line 1790 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0x7FFFFFFF); (yyval.binstr)->appendInt32(0x7FFFFFFF);  }
#line 8415 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 733: /* bound: ELLIPSIS  */
#line 1791 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0x7FFFFFFF); (yyval.binstr)->appendInt32(0x7FFFFFFF);  }
#line 8421 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 734: /* bound: int32  */
#line 1792 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0); (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 8427 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 735: /* bound: int32 ELLIPSIS int32  */
#line 1793 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                               { FAIL_UNLESS((yyvsp[-2].int32) <= (yyvsp[0].int32), ("lower bound %d must be <= upper bound %d\n", (yyvsp[-2].int32), (yyvsp[0].int32)));
                                                                if ((yyvsp[-2].int32) > (yyvsp[0].int32)) { YYERROR; };
                                                                (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[-2].int32)); (yyval.binstr)->appendInt32((yyvsp[0].int32)-(yyvsp[-2].int32)+1); }
#line 8435 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 736: /* bound: int32 ELLIPSIS  */
#line 1796 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[-1].int32)); (yyval.binstr)->appendInt32(0x7FFFFFFF); }
#line 8441 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 737: /* secDecl: _PERMISSION secAction typeSpec '(' nameValPairs ')'  */
#line 1801 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-4].secAct), (yyvsp[-3].token), (yyvsp[-1].pair)); }
#line 8447 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 738: /* secDecl: _PERMISSION secAction typeSpec '=' '{' customBlobDescr '}'  */
#line 1803 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-5].secAct), (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 8453 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 739: /* secDecl: _PERMISSION secAction typeSpec  */
#line 1804 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-1].secAct), (yyvsp[0].token), (NVPair *)NULL); }
#line 8459 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 740: /* secDecl: psetHead bytes ')'  */
#line 1805 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASM->AddPermissionSetDecl((yyvsp[-2].secAct), (yyvsp[-1].binstr)); }
#line 8465 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 741: /* secDecl: _PERMISSIONSET secAction compQstring  */
#line 1807 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASM->AddPermissionSetDecl((yyvsp[-1].secAct),BinStrToUnicode((yyvsp[0].binstr),true));}
#line 8471 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 742: /* secDecl: _PERMISSIONSET secAction '=' '{' secAttrSetBlob '}'  */
#line 1809 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { BinStr* ret = new BinStr();
                                                                ret->insertInt8('.');
                                                                corEmitInt(ret, nSecAttrBlobs);
                                                                ret->append((yyvsp[-1].binstr));
                                                                PASM->AddPermissionSetDecl((yyvsp[-4].secAct),ret);
                                                                nSecAttrBlobs = 0; }
#line 8482 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 743: /* secAttrSetBlob: %empty  */
#line 1817 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); nSecAttrBlobs = 0;}
#line 8488 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 744: /* secAttrSetBlob: secAttrBlob  */
#line 1818 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); nSecAttrBlobs = 1; }
#line 8494 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 745: /* secAttrSetBlob: secAttrBlob ',' secAttrSetBlob  */
#line 1819 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); nSecAttrBlobs++; }
#line 8500 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 746: /* secAttrBlob: typeSpec '=' '{' customBlobNVPairs '}'  */
#line 1823 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = PASM->EncodeSecAttr(PASM->ReflectionNotation((yyvsp[-4].token)),(yyvsp[-1].binstr),nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
#line 8507 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 747: /* secAttrBlob: CLASS_ SQSTRING '=' '{' customBlobNVPairs '}'  */
#line 1826 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = PASM->EncodeSecAttr((yyvsp[-4].string),(yyvsp[-1].binstr),nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
#line 8514 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 748: /* psetHead: _PERMISSIONSET secAction '=' '('  */
#line 1830 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = (yyvsp[-2].secAct); bParsingByteArray = TRUE; }
#line 8520 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 749: /* psetHead: _PERMISSIONSET secAction BYTEARRAY_ '('  */
#line 1832 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = (yyvsp[-2].secAct); bParsingByteArray = TRUE; }
#line 8526 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 750: /* nameValPairs: nameValPair  */
#line 1835 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.pair) = (yyvsp[0].pair); }
#line 8532 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 751: /* nameValPairs: nameValPair ',' nameValPairs  */
#line 1836 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.pair) = (yyvsp[-2].pair)->Concat((yyvsp[0].pair)); }
#line 8538 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 752: /* nameValPair: compQstring '=' caValue  */
#line 1839 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyvsp[-2].binstr)->appendInt8(0); (yyval.pair) = new NVPair((yyvsp[-2].binstr), (yyvsp[0].binstr)); }
#line 8544 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 753: /* truefalse: TRUE_  */
#line 1842 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = 1; }
#line 8550 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 754: /* truefalse: FALSE_  */
#line 1843 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = 0; }
#line 8556 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 755: /* caValue: truefalse  */
#line 1846 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_BOOLEAN);
                                                                (yyval.binstr)->appendInt8((yyvsp[0].int32)); }
#line 8564 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 756: /* caValue: int32  */
#line 1849 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_I4);
                                                                (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 8572 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 757: /* caValue: INT32_ '(' int32 ')'  */
#line 1852 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_I4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8580 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 758: /* caValue: compQstring  */
#line 1855 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_STRING);
                                                                (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr);
                                                                (yyval.binstr)->appendInt8(0); }
#line 8589 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 759: /* caValue: className '(' INT8_ ':' int32 ')'  */
#line 1859 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(1);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8600 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 760: /* caValue: className '(' INT16_ ':' int32 ')'  */
#line 1865 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(2);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8611 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 761: /* caValue: className '(' INT32_ ':' int32 ')'  */
#line 1871 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8622 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 762: /* caValue: className '(' int32 ')'  */
#line 1877 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-3].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8633 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 763: /* secAction: REQUEST_  */
#line 1885 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = dclRequest; }
#line 8639 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 764: /* secAction: DEMAND_  */
#line 1886 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = dclDemand; }
#line 8645 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 765: /* secAction: ASSERT_  */
#line 1887 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = dclAssert; }
#line 8651 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 766: /* secAction: DENY_  */
#line 1888 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = dclDeny; }
#line 8657 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 767: /* secAction: PERMITONLY_  */
#line 1889 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = dclPermitOnly; }
#line 8663 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 768: /* secAction: LINKCHECK_  */
#line 1890 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = dclLinktimeCheck; }
#line 8669 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 769: /* secAction: INHERITCHECK_  */
#line 1891 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = dclInheritanceCheck; }
#line 8675 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 770: /* secAction: REQMIN_  */
#line 1892 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = dclRequestMinimum; }
#line 8681 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 771: /* secAction: REQOPT_  */
#line 1893 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = dclRequestOptional; }
#line 8687 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 772: /* secAction: REQREFUSE_  */
#line 1894 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = dclRequestRefuse; }
#line 8693 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 773: /* secAction: PREJITGRANT_  */
#line 1895 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = dclPrejitGrant; }
#line 8699 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 774: /* secAction: PREJITDENY_  */
#line 1896 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = dclPrejitDenied; }
#line 8705 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 775: /* secAction: NONCASDEMAND_  */
#line 1897 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = dclNonCasDemand; }
#line 8711 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 776: /* secAction: NONCASLINKDEMAND_  */
#line 1898 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = dclNonCasLinkDemand; }
#line 8717 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 777: /* secAction: NONCASINHERITANCE_  */
#line 1899 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.secAct) = dclNonCasInheritance; }
#line 8723 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 778: /* esHead: _LINE  */
#line 1903 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = FALSE; }
#line 8729 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 779: /* esHead: P_LINE  */
#line 1904 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = TRUE; }
#line 8735 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 780: /* extSourceSpec: esHead int32 SQSTRING  */
#line 1907 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-1].int32);
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8743 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 781: /* extSourceSpec: esHead int32  */
#line 1910 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[0].int32);
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1); }
#line 8750 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 782: /* extSourceSpec: esHead int32 ':' int32 SQSTRING  */
#line 1912 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-3].int32);
                                                                PENV->nExtCol=(yyvsp[-1].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8758 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 783: /* extSourceSpec: esHead int32 ':' int32  */
#line 1915 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-2].int32);
                                                                PENV->nExtCol=(yyvsp[0].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);}
#line 8765 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 784: /* extSourceSpec: esHead int32 ':' int32 ',' int32 SQSTRING  */
#line 1918 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-5].int32);
                                                                PENV->nExtCol=(yyvsp[-3].int32); PENV->nExtColEnd = (yyvsp[-1].int32);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8773 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 785: /* extSourceSpec: esHead int32 ':' int32 ',' int32  */
#line 1922 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-4].int32);
                                                                PENV->nExtCol=(yyvsp[-2].int32); PENV->nExtColEnd = (yyvsp[0].int32); }
#line 8780 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 786: /* extSourceSpec: esHead int32 ',' int32 ':' int32 SQSTRING  */
#line 1925 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-5].int32); PENV->nExtLineEnd = (yyvsp[-3].int32);
                                                                PENV->nExtCol=(yyvsp[-1].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8788 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 787: /* extSourceSpec: esHead int32 ',' int32 ':' int32  */
#line 1929 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-4].int32); PENV->nExtLineEnd = (yyvsp[-2].int32);
                                                                PENV->nExtCol=(yyvsp[0].int32); PENV->nExtColEnd = static_cast<unsigned>(-1); }
#line 8795 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 788: /* extSourceSpec: esHead int32 ',' int32 ':' int32 ',' int32 SQSTRING  */
#line 1932 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-7].int32); PENV->nExtLineEnd = (yyvsp[-5].int32);
                                                                PENV->nExtCol=(yyvsp[-3].int32); PENV->nExtColEnd = (yyvsp[-1].int32);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8803 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 789: /* extSourceSpec: esHead int32 ',' int32 ':' int32 ',' int32  */
#line 1936 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-6].int32); PENV->nExtLineEnd = (yyvsp[-4].int32);
                                                                PENV->nExtCol=(yyvsp[-2].int32); PENV->nExtColEnd = (yyvsp[0].int32); }
#line 8810 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 790: /* extSourceSpec: esHead int32 QSTRING  */
#line 1938 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-1].int32) - 1;
                                                                PENV->nExtCol = 0; PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].binstr));}
#line 8818 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 791: /* fileDecl: _FILE fileAttr dottedName fileEntry hashHead bytes ')' fileEntry  */
#line 1945 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->AddFile((yyvsp[-5].string), (yyvsp[-6].fileAttr)|(yyvsp[-4].fileAttr)|(yyvsp[0].fileAttr), (yyvsp[-2].binstr)); }
#line 8824 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 792: /* fileDecl: _FILE fileAttr dottedName fileEntry  */
#line 1946 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->AddFile((yyvsp[-1].string), (yyvsp[-2].fileAttr)|(yyvsp[0].fileAttr), NULL); }
#line 8830 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 793: /* fileAttr: %empty  */
#line 1949 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0; }
#line 8836 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 794: /* fileAttr: fileAttr NOMETADATA_  */
#line 1950 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) ((yyvsp[-1].fileAttr) | ffContainsNoMetaData); }
#line 8842 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 795: /* fileEntry: %empty  */
#line 1953 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0; }
#line 8848 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 796: /* fileEntry: _ENTRYPOINT  */
#line 1954 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0x80000000; }
#line 8854 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 797: /* hashHead: _HASH '=' '('  */
#line 1957 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8860 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 798: /* assemblyHead: _ASSEMBLY asmAttr dottedName  */
#line 1960 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[0].string), NULL, (DWORD)(yyvsp[-1].asmAttr), FALSE); }
#line 8866 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 799: /* asmAttr: %empty  */
#line 1963 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) 0; }
#line 8872 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 800: /* asmAttr: asmAttr RETARGETABLE_  */
#line 1964 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afRetargetable); }
#line 8878 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 801: /* asmAttr: asmAttr WINDOWSRUNTIME_  */
#line 1965 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afContentType_WindowsRuntime); }
#line 8884 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 802: /* asmAttr: asmAttr NOPLATFORM_  */
#line 1966 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afPA_NoPlatform); }
#line 8890 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 803: /* asmAttr: asmAttr LEGACY_ LIBRARY_  */
#line 1967 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.asmAttr) = (yyvsp[-2].asmAttr); }
#line 8896 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 804: /* asmAttr: asmAttr CIL_  */
#line 1968 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_MSIL); }
#line 8902 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 805: /* asmAttr: asmAttr X86_  */
#line 1969 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_x86); }
#line 8908 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 806: /* asmAttr: asmAttr AMD64_  */
#line 1970 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_AMD64); }
#line 8914 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 807: /* asmAttr: asmAttr ARM_  */
#line 1971 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_ARM); }
#line 8920 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 808: /* asmAttr: asmAttr ARM64_  */
#line 1972 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_ARM64); }
#line 8926 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 811: /* assemblyDecl: _HASH ALGORITHM_ int32  */
#line 1979 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->SetAssemblyHashAlg((yyvsp[0].int32)); }
#line 8932 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 814: /* intOrWildcard: int32  */
#line 1984 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 8938 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 815: /* intOrWildcard: '*'  */
#line 1985 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.int32) = 0xFFFF; }
#line 8944 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 816: /* asmOrRefDecl: publicKeyHead bytes ')'  */
#line 1988 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->SetAssemblyPublicKey((yyvsp[-1].binstr)); }
#line 8950 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 817: /* asmOrRefDecl: _VER intOrWildcard ':' intOrWildcard ':' intOrWildcard ':' intOrWildcard  */
#line 1990 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->SetAssemblyVer((USHORT)(yyvsp[-6].int32), (USHORT)(yyvsp[-4].int32), (USHORT)(yyvsp[-2].int32), (USHORT)(yyvsp[0].int32)); }
#line 8956 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 818: /* asmOrRefDecl: _LOCALE compQstring  */
#line 1991 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyvsp[0].binstr)->appendInt8(0); PASMM->SetAssemblyLocale((yyvsp[0].binstr),TRUE); }
#line 8962 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 819: /* asmOrRefDecl: localeHead bytes ')'  */
#line 1992 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->SetAssemblyLocale((yyvsp[-1].binstr),FALSE); }
#line 8968 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 822: /* publicKeyHead: _PUBLICKEY '=' '('  */
#line 1997 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8974 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 823: /* publicKeyTokenHead: _PUBLICKEYTOKEN '=' '('  */
#line 2000 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8980 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 824: /* localeHead: _LOCALE '=' '('  */
#line 2003 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8986 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 825: /* assemblyRefHead: _ASSEMBLY EXTERN_ asmAttr dottedName  */
#line 2007 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[0].string), NULL, (yyvsp[-1].asmAttr), TRUE); }
#line 8992 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 826: /* assemblyRefHead: _ASSEMBLY EXTERN_ asmAttr dottedName AS_ dottedName  */
#line 2009 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[-2].string), (yyvsp[0].string), (yyvsp[-3].asmAttr), TRUE); }
#line 8998 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 829: /* assemblyRefDecl: hashHead bytes ')'  */
#line 2016 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->SetAssemblyHashBlob((yyvsp[-1].binstr)); }
#line 9004 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 831: /* assemblyRefDecl: publicKeyTokenHead bytes ')'  */
#line 2018 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->SetAssemblyPublicKeyToken((yyvsp[-1].binstr)); }
#line 9010 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 832: /* assemblyRefDecl: AUTO_  */
#line 2019 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->SetAssemblyAutodetect(); }
#line 9016 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 833: /* exptypeHead: _CLASS EXTERN_ exptAttr dottedName  */
#line 2022 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->StartComType((yyvsp[0].string), (yyvsp[-1].exptAttr));}
#line 9022 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 834: /* exportHead: _EXPORT exptAttr dottedName  */
#line 2025 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                                              { PASMM->StartComType((yyvsp[0].string), (yyvsp[-1].exptAttr)); }
#line 9028 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 835: /* exptAttr: %empty  */
#line 2028 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) 0; }
#line 9034 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 836: /* exptAttr: exptAttr PRIVATE_  */
#line 2029 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdNotPublic); }
#line 9040 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 837: /* exptAttr: exptAttr PUBLIC_  */
#line 2030 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdPublic); }
#line 9046 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 838: /* exptAttr: exptAttr FORWARDER_  */
#line 2031 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdForwarder); }
#line 9052 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 839: /* exptAttr: exptAttr NESTED_ PUBLIC_  */
#line 2032 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedPublic); }
#line 9058 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 840: /* exptAttr: exptAttr NESTED_ PRIVATE_  */
#line 2033 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedPrivate); }
#line 9064 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 841: /* exptAttr: exptAttr NESTED_ FAMILY_  */
#line 2034 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamily); }
#line 9070 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 842: /* exptAttr: exptAttr NESTED_ ASSEMBLY_  */
#line 2035 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedAssembly); }
#line 9076 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 843: /* exptAttr: exptAttr NESTED_ FAMANDASSEM_  */
#line 2036 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamANDAssem); }
#line 9082 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 844: /* exptAttr: exptAttr NESTED_ FAMORASSEM_  */
#line 2037 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamORAssem); }
#line 9088 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 847: /* exptypeDecl: _FILE dottedName  */
#line 2044 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->SetComTypeFile((yyvsp[0].string)); }
#line 9094 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 848: /* exptypeDecl: _CLASS EXTERN_ slashedName  */
#line 2045 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                               { PASMM->SetComTypeComType((yyvsp[0].string)); }
#line 9100 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 849: /* exptypeDecl: _ASSEMBLY EXTERN_ dottedName  */
#line 2046 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->SetComTypeAsmRef((yyvsp[0].string)); }
#line 9106 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 850: /* exptypeDecl: MDTOKEN_ '(' int32 ')'  */
#line 2047 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { if(!PASMM->SetComTypeImplementationTok((yyvsp[-1].int32)))
                                                                  PASM->report->error("Invalid implementation of exported type\n"); }
#line 9113 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 851: /* exptypeDecl: _CLASS int32  */
#line 2049 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { if(!PASMM->SetComTypeClassTok((yyvsp[0].int32)))
                                                                  PASM->report->error("Invalid TypeDefID of exported type\n"); }
#line 9120 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 854: /* manifestResHead: _MRESOURCE manresAttr dottedName  */
#line 2055 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->StartManifestRes((yyvsp[0].string), (yyvsp[0].string), (yyvsp[-1].manresAttr)); }
#line 9126 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 855: /* manifestResHead: _MRESOURCE manresAttr dottedName AS_ dottedName  */
#line 2057 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->StartManifestRes((yyvsp[-2].string), (yyvsp[0].string), (yyvsp[-3].manresAttr)); }
#line 9132 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 856: /* manresAttr: %empty  */
#line 2060 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) 0; }
#line 9138 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 857: /* manresAttr: manresAttr PUBLIC_  */
#line 2061 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) ((yyvsp[-1].manresAttr) | mrPublic); }
#line 9144 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 858: /* manresAttr: manresAttr PRIVATE_  */
#line 2062 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) ((yyvsp[-1].manresAttr) | mrPrivate); }
#line 9150 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 861: /* manifestResDecl: _FILE dottedName AT_ int32  */
#line 2069 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->SetManifestResFile((yyvsp[-2].string), (ULONG)(yyvsp[0].int32)); }
#line 9156 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;

  case 862: /* manifestResDecl: _ASSEMBLY EXTERN_ dottedName  */
#line 2070 ".\\src\\coreclr\\ilasm\\asmparse.y"
                                                              { PASMM->SetManifestResAsmRef((yyvsp[0].string)); }
#line 9162 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"
    break;


#line 9166 ".\\src\\coreclr\\ilasm\\prebuilt\\asmparse.cpp"

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

#line 2075 ".\\src\\coreclr\\ilasm\\asmparse.y"


#include "grammar_after.cpp"
