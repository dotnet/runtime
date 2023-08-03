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
/* Token kinds.  */
#define YYEMPTY -2
#define YYEOF 0
#define YYerror 256
#define YYUNDEF 257
#define ERROR_ 258
#define BAD_COMMENT_ 259
#define BAD_LITERAL_ 260
#define ID 261
#define DOTTEDNAME 262
#define QSTRING 263
#define SQSTRING 264
#define INT32_T 265
#define INT64_T 266
#define FLOAT64 267
#define HEXBYTE 268
#define TYPEDEF_T 269
#define TYPEDEF_M 270
#define TYPEDEF_F 271
#define TYPEDEF_TS 272
#define TYPEDEF_MR 273
#define TYPEDEF_CA 274
#define DCOLON 275
#define ELLIPSIS 276
#define VOID_ 277
#define BOOL_ 278
#define CHAR_ 279
#define UNSIGNED_ 280
#define INT_ 281
#define INT8_ 282
#define INT16_ 283
#define INT32_ 284
#define INT64_ 285
#define FLOAT_ 286
#define FLOAT32_ 287
#define FLOAT64_ 288
#define BYTEARRAY_ 289
#define UINT_ 290
#define UINT8_ 291
#define UINT16_ 292
#define UINT32_ 293
#define UINT64_ 294
#define FLAGS_ 295
#define CALLCONV_ 296
#define MDTOKEN_ 297
#define OBJECT_ 298
#define STRING_ 299
#define NULLREF_ 300
#define DEFAULT_ 301
#define CDECL_ 302
#define VARARG_ 303
#define STDCALL_ 304
#define THISCALL_ 305
#define FASTCALL_ 306
#define CLASS_ 307
#define BYREFLIKE_ 308
#define TYPEDREF_ 309
#define UNMANAGED_ 310
#define FINALLY_ 311
#define HANDLER_ 312
#define CATCH_ 313
#define FILTER_ 314
#define FAULT_ 315
#define EXTENDS_ 316
#define IMPLEMENTS_ 317
#define TO_ 318
#define AT_ 319
#define TLS_ 320
#define TRUE_ 321
#define FALSE_ 322
#define _INTERFACEIMPL 323
#define VALUE_ 324
#define VALUETYPE_ 325
#define NATIVE_ 326
#define INSTANCE_ 327
#define SPECIALNAME_ 328
#define FORWARDER_ 329
#define STATIC_ 330
#define PUBLIC_ 331
#define PRIVATE_ 332
#define FAMILY_ 333
#define FINAL_ 334
#define SYNCHRONIZED_ 335
#define INTERFACE_ 336
#define SEALED_ 337
#define NESTED_ 338
#define ABSTRACT_ 339
#define AUTO_ 340
#define SEQUENTIAL_ 341
#define EXPLICIT_ 342
#define ANSI_ 343
#define UNICODE_ 344
#define AUTOCHAR_ 345
#define IMPORT_ 346
#define ENUM_ 347
#define VIRTUAL_ 348
#define NOINLINING_ 349
#define AGGRESSIVEINLINING_ 350
#define NOOPTIMIZATION_ 351
#define AGGRESSIVEOPTIMIZATION_ 352
#define UNMANAGEDEXP_ 353
#define BEFOREFIELDINIT_ 354
#define STRICT_ 355
#define RETARGETABLE_ 356
#define WINDOWSRUNTIME_ 357
#define NOPLATFORM_ 358
#define METHOD_ 359
#define FIELD_ 360
#define PINNED_ 361
#define MODREQ_ 362
#define MODOPT_ 363
#define SERIALIZABLE_ 364
#define PROPERTY_ 365
#define TYPE_ 366
#define ASSEMBLY_ 367
#define FAMANDASSEM_ 368
#define FAMORASSEM_ 369
#define PRIVATESCOPE_ 370
#define HIDEBYSIG_ 371
#define NEWSLOT_ 372
#define RTSPECIALNAME_ 373
#define PINVOKEIMPL_ 374
#define _CTOR 375
#define _CCTOR 376
#define LITERAL_ 377
#define NOTSERIALIZED_ 378
#define INITONLY_ 379
#define REQSECOBJ_ 380
#define CIL_ 381
#define OPTIL_ 382
#define MANAGED_ 383
#define FORWARDREF_ 384
#define PRESERVESIG_ 385
#define RUNTIME_ 386
#define INTERNALCALL_ 387
#define _IMPORT 388
#define NOMANGLE_ 389
#define LASTERR_ 390
#define WINAPI_ 391
#define AS_ 392
#define BESTFIT_ 393
#define ON_ 394
#define OFF_ 395
#define CHARMAPERROR_ 396
#define INSTR_NONE 397
#define INSTR_VAR 398
#define INSTR_I 399
#define INSTR_I8 400
#define INSTR_R 401
#define INSTR_BRTARGET 402
#define INSTR_METHOD 403
#define INSTR_FIELD 404
#define INSTR_TYPE 405
#define INSTR_STRING 406
#define INSTR_SIG 407
#define INSTR_TOK 408
#define INSTR_SWITCH 409
#define _CLASS 410
#define _NAMESPACE 411
#define _METHOD 412
#define _FIELD 413
#define _DATA 414
#define _THIS 415
#define _BASE 416
#define _NESTER 417
#define _EMITBYTE 418
#define _TRY 419
#define _MAXSTACK 420
#define _LOCALS 421
#define _ENTRYPOINT 422
#define _ZEROINIT 423
#define _EVENT 424
#define _ADDON 425
#define _REMOVEON 426
#define _FIRE 427
#define _OTHER 428
#define _PROPERTY 429
#define _SET 430
#define _GET 431
#define _PERMISSION 432
#define _PERMISSIONSET 433
#define REQUEST_ 434
#define DEMAND_ 435
#define ASSERT_ 436
#define DENY_ 437
#define PERMITONLY_ 438
#define LINKCHECK_ 439
#define INHERITCHECK_ 440
#define REQMIN_ 441
#define REQOPT_ 442
#define REQREFUSE_ 443
#define PREJITGRANT_ 444
#define PREJITDENY_ 445
#define NONCASDEMAND_ 446
#define NONCASLINKDEMAND_ 447
#define NONCASINHERITANCE_ 448
#define _LINE 449
#define P_LINE 450
#define _LANGUAGE 451
#define _CUSTOM 452
#define INIT_ 453
#define _SIZE 454
#define _PACK 455
#define _VTABLE 456
#define _VTFIXUP 457
#define FROMUNMANAGED_ 458
#define CALLMOSTDERIVED_ 459
#define _VTENTRY 460
#define RETAINAPPDOMAIN_ 461
#define _FILE 462
#define NOMETADATA_ 463
#define _HASH 464
#define _ASSEMBLY 465
#define _PUBLICKEY 466
#define _PUBLICKEYTOKEN 467
#define ALGORITHM_ 468
#define _VER 469
#define _LOCALE 470
#define EXTERN_ 471
#define _MRESOURCE 472
#define _MODULE 473
#define _EXPORT 474
#define LEGACY_ 475
#define LIBRARY_ 476
#define X86_ 477
#define AMD64_ 478
#define ARM_ 479
#define ARM64_ 480
#define MARSHAL_ 481
#define CUSTOM_ 482
#define SYSSTRING_ 483
#define FIXED_ 484
#define VARIANT_ 485
#define CURRENCY_ 486
#define SYSCHAR_ 487
#define DECIMAL_ 488
#define DATE_ 489
#define BSTR_ 490
#define TBSTR_ 491
#define LPSTR_ 492
#define LPWSTR_ 493
#define LPTSTR_ 494
#define OBJECTREF_ 495
#define IUNKNOWN_ 496
#define IDISPATCH_ 497
#define STRUCT_ 498
#define SAFEARRAY_ 499
#define BYVALSTR_ 500
#define LPVOID_ 501
#define ANY_ 502
#define ARRAY_ 503
#define LPSTRUCT_ 504
#define IIDPARAM_ 505
#define IN_ 506
#define OUT_ 507
#define OPT_ 508
#define _PARAM 509
#define _OVERRIDE 510
#define WITH_ 511
#define NULL_ 512
#define HRESULT_ 513
#define CARRAY_ 514
#define USERDEFINED_ 515
#define RECORD_ 516
#define FILETIME_ 517
#define BLOB_ 518
#define STREAM_ 519
#define STORAGE_ 520
#define STREAMED_OBJECT_ 521
#define STORED_OBJECT_ 522
#define BLOB_OBJECT_ 523
#define CF_ 524
#define CLSID_ 525
#define VECTOR_ 526
#define _SUBSYSTEM 527
#define _CORFLAGS 528
#define ALIGNMENT_ 529
#define _IMAGEBASE 530
#define _STACKRESERVE 531
#define _TYPEDEF 532
#define _TEMPLATE 533
#define _TYPELIST 534
#define _MSCORLIB 535
#define P_DEFINE 536
#define P_UNDEF 537
#define P_IFDEF 538
#define P_IFNDEF 539
#define P_ELSE 540
#define P_ENDIF 541
#define P_INCLUDE 542
#define CONSTRAINT_ 543
#define CONST_ 544

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

#line 742 "asmparse.cpp"

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
  YYSYMBOL_constTypeArg = 418,             /* constTypeArg  */
  YYSYMBOL_f32seq = 419,                   /* f32seq  */
  YYSYMBOL_f64seq = 420,                   /* f64seq  */
  YYSYMBOL_i64seq = 421,                   /* i64seq  */
  YYSYMBOL_i32seq = 422,                   /* i32seq  */
  YYSYMBOL_i16seq = 423,                   /* i16seq  */
  YYSYMBOL_i8seq = 424,                    /* i8seq  */
  YYSYMBOL_boolSeq = 425,                  /* boolSeq  */
  YYSYMBOL_sqstringSeq = 426,              /* sqstringSeq  */
  YYSYMBOL_classSeq = 427,                 /* classSeq  */
  YYSYMBOL_objSeq = 428,                   /* objSeq  */
  YYSYMBOL_methodSpec = 429,               /* methodSpec  */
  YYSYMBOL_instr_none = 430,               /* instr_none  */
  YYSYMBOL_instr_var = 431,                /* instr_var  */
  YYSYMBOL_instr_i = 432,                  /* instr_i  */
  YYSYMBOL_instr_i8 = 433,                 /* instr_i8  */
  YYSYMBOL_instr_r = 434,                  /* instr_r  */
  YYSYMBOL_instr_brtarget = 435,           /* instr_brtarget  */
  YYSYMBOL_instr_method = 436,             /* instr_method  */
  YYSYMBOL_instr_field = 437,              /* instr_field  */
  YYSYMBOL_instr_type = 438,               /* instr_type  */
  YYSYMBOL_instr_string = 439,             /* instr_string  */
  YYSYMBOL_instr_sig = 440,                /* instr_sig  */
  YYSYMBOL_instr_tok = 441,                /* instr_tok  */
  YYSYMBOL_instr_switch = 442,             /* instr_switch  */
  YYSYMBOL_instr_r_head = 443,             /* instr_r_head  */
  YYSYMBOL_instr = 444,                    /* instr  */
  YYSYMBOL_labels = 445,                   /* labels  */
  YYSYMBOL_tyArgs0 = 446,                  /* tyArgs0  */
  YYSYMBOL_tyArgs1 = 447,                  /* tyArgs1  */
  YYSYMBOL_tyArgs2 = 448,                  /* tyArgs2  */
  YYSYMBOL_sigArgs0 = 449,                 /* sigArgs0  */
  YYSYMBOL_sigArgs1 = 450,                 /* sigArgs1  */
  YYSYMBOL_sigArg = 451,                   /* sigArg  */
  YYSYMBOL_className = 452,                /* className  */
  YYSYMBOL_slashedName = 453,              /* slashedName  */
  YYSYMBOL_typeSpec = 454,                 /* typeSpec  */
  YYSYMBOL_nativeType = 455,               /* nativeType  */
  YYSYMBOL_iidParamIndex = 456,            /* iidParamIndex  */
  YYSYMBOL_variantType = 457,              /* variantType  */
  YYSYMBOL_type = 458,                     /* type  */
  YYSYMBOL_simpleType = 459,               /* simpleType  */
  YYSYMBOL_bounds1 = 460,                  /* bounds1  */
  YYSYMBOL_bound = 461,                    /* bound  */
  YYSYMBOL_secDecl = 462,                  /* secDecl  */
  YYSYMBOL_secAttrSetBlob = 463,           /* secAttrSetBlob  */
  YYSYMBOL_secAttrBlob = 464,              /* secAttrBlob  */
  YYSYMBOL_psetHead = 465,                 /* psetHead  */
  YYSYMBOL_nameValPairs = 466,             /* nameValPairs  */
  YYSYMBOL_nameValPair = 467,              /* nameValPair  */
  YYSYMBOL_truefalse = 468,                /* truefalse  */
  YYSYMBOL_caValue = 469,                  /* caValue  */
  YYSYMBOL_secAction = 470,                /* secAction  */
  YYSYMBOL_esHead = 471,                   /* esHead  */
  YYSYMBOL_extSourceSpec = 472,            /* extSourceSpec  */
  YYSYMBOL_fileDecl = 473,                 /* fileDecl  */
  YYSYMBOL_fileAttr = 474,                 /* fileAttr  */
  YYSYMBOL_fileEntry = 475,                /* fileEntry  */
  YYSYMBOL_hashHead = 476,                 /* hashHead  */
  YYSYMBOL_assemblyHead = 477,             /* assemblyHead  */
  YYSYMBOL_asmAttr = 478,                  /* asmAttr  */
  YYSYMBOL_assemblyDecls = 479,            /* assemblyDecls  */
  YYSYMBOL_assemblyDecl = 480,             /* assemblyDecl  */
  YYSYMBOL_intOrWildcard = 481,            /* intOrWildcard  */
  YYSYMBOL_asmOrRefDecl = 482,             /* asmOrRefDecl  */
  YYSYMBOL_publicKeyHead = 483,            /* publicKeyHead  */
  YYSYMBOL_publicKeyTokenHead = 484,       /* publicKeyTokenHead  */
  YYSYMBOL_localeHead = 485,               /* localeHead  */
  YYSYMBOL_assemblyRefHead = 486,          /* assemblyRefHead  */
  YYSYMBOL_assemblyRefDecls = 487,         /* assemblyRefDecls  */
  YYSYMBOL_assemblyRefDecl = 488,          /* assemblyRefDecl  */
  YYSYMBOL_exptypeHead = 489,              /* exptypeHead  */
  YYSYMBOL_exportHead = 490,               /* exportHead  */
  YYSYMBOL_exptAttr = 491,                 /* exptAttr  */
  YYSYMBOL_exptypeDecls = 492,             /* exptypeDecls  */
  YYSYMBOL_exptypeDecl = 493,              /* exptypeDecl  */
  YYSYMBOL_manifestResHead = 494,          /* manifestResHead  */
  YYSYMBOL_manresAttr = 495,               /* manresAttr  */
  YYSYMBOL_manifestResDecls = 496,         /* manifestResDecls  */
  YYSYMBOL_manifestResDecl = 497           /* manifestResDecl  */
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
#define YYLAST   3784

/* YYNTOKENS -- Number of terminals.  */
#define YYNTOKENS  309
/* YYNNTS -- Number of nonterminals.  */
#define YYNNTS  189
/* YYNRULES -- Number of rules.  */
#define YYNRULES  869
/* YYNSTATES -- Number of states.  */
#define YYNSTATES  1672

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
       0,   191,   191,   192,   195,   196,   197,   201,   202,   203,
     204,   205,   206,   207,   208,   209,   210,   211,   212,   213,
     214,   224,   225,   228,   231,   232,   233,   234,   235,   236,
     239,   240,   243,   244,   247,   248,   250,   255,   256,   259,
     260,   261,   264,   267,   268,   271,   272,   273,   277,   278,
     279,   280,   281,   286,   287,   288,   289,   292,   295,   296,
     300,   301,   305,   306,   307,   308,   311,   312,   313,   315,
     318,   321,   327,   330,   331,   335,   341,   342,   344,   347,
     348,   354,   357,   358,   361,   365,   366,   374,   375,   376,
     377,   379,   381,   386,   387,   388,   395,   399,   400,   401,
     402,   403,   404,   407,   410,   414,   417,   420,   426,   429,
     430,   431,   432,   433,   434,   435,   436,   437,   438,   439,
     440,   441,   442,   443,   444,   445,   446,   447,   448,   449,
     450,   451,   452,   453,   454,   455,   458,   459,   462,   463,
     466,   467,   470,   471,   475,   476,   479,   480,   483,   484,
     487,   488,   489,   490,   491,   492,   493,   496,   497,   500,
     503,   504,   507,   508,   509,   512,   513,   516,   519,   520,
     523,   527,   531,   532,   533,   534,   535,   536,   537,   538,
     539,   540,   541,   542,   548,   557,   558,   559,   564,   570,
     571,   572,   579,   584,   585,   586,   587,   588,   589,   590,
     591,   603,   605,   606,   607,   608,   609,   610,   611,   614,
     615,   618,   619,   622,   623,   627,   644,   650,   666,   671,
     672,   673,   676,   677,   678,   679,   682,   683,   684,   685,
     686,   687,   688,   689,   692,   695,   700,   704,   708,   710,
     712,   717,   718,   722,   723,   724,   727,   728,   731,   732,
     733,   734,   735,   736,   737,   738,   742,   748,   749,   750,
     753,   754,   758,   759,   760,   761,   762,   763,   764,   768,
     774,   775,   778,   779,   782,   785,   801,   802,   803,   804,
     805,   806,   807,   808,   809,   810,   811,   812,   813,   814,
     815,   816,   817,   818,   819,   820,   821,   824,   827,   832,
     833,   834,   835,   836,   837,   838,   839,   840,   841,   842,
     843,   844,   845,   846,   847,   850,   851,   852,   855,   856,
     857,   858,   859,   862,   863,   864,   865,   866,   867,   868,
     869,   870,   871,   872,   873,   874,   875,   876,   877,   880,
     884,   885,   888,   889,   890,   891,   893,   896,   897,   898,
     899,   900,   901,   902,   903,   904,   905,   906,   916,   926,
     928,   931,   938,   939,   944,   950,   951,   953,   974,   977,
     981,   984,   985,   988,   989,   990,   994,   999,  1000,  1001,
    1002,  1006,  1007,  1009,  1013,  1017,  1022,  1026,  1030,  1031,
    1032,  1037,  1040,  1041,  1044,  1045,  1046,  1049,  1050,  1053,
    1054,  1057,  1058,  1063,  1064,  1065,  1066,  1073,  1080,  1087,
    1094,  1102,  1110,  1111,  1112,  1113,  1114,  1115,  1119,  1122,
    1124,  1126,  1128,  1130,  1132,  1134,  1136,  1138,  1140,  1142,
    1144,  1146,  1148,  1150,  1152,  1154,  1156,  1160,  1163,  1164,
    1167,  1168,  1172,  1173,  1174,  1179,  1180,  1181,  1183,  1185,
    1187,  1188,  1189,  1193,  1197,  1201,  1205,  1209,  1213,  1217,
    1221,  1225,  1229,  1233,  1237,  1241,  1245,  1249,  1253,  1257,
    1261,  1267,  1270,  1272,  1274,  1276,  1278,  1280,  1282,  1284,
    1286,  1288,  1290,  1292,  1294,  1296,  1298,  1300,  1302,  1307,
    1308,  1310,  1314,  1315,  1317,  1321,  1322,  1326,  1327,  1330,
    1331,  1334,  1335,  1338,  1339,  1343,  1344,  1345,  1349,  1350,
    1351,  1353,  1357,  1358,  1362,  1368,  1371,  1374,  1377,  1380,
    1383,  1386,  1394,  1397,  1400,  1403,  1406,  1409,  1412,  1416,
    1417,  1418,  1419,  1420,  1421,  1422,  1423,  1432,  1433,  1434,
    1441,  1449,  1457,  1463,  1469,  1475,  1479,  1480,  1482,  1484,
    1488,  1494,  1497,  1498,  1499,  1500,  1501,  1505,  1506,  1509,
    1510,  1513,  1514,  1518,  1519,  1522,  1523,  1526,  1527,  1528,
    1532,  1533,  1534,  1535,  1536,  1537,  1538,  1539,  1542,  1548,
    1555,  1556,  1559,  1560,  1561,  1562,  1566,  1567,  1574,  1580,
    1582,  1585,  1587,  1588,  1590,  1592,  1593,  1594,  1595,  1596,
    1597,  1598,  1599,  1600,  1601,  1602,  1603,  1604,  1605,  1606,
    1607,  1608,  1610,  1612,  1617,  1622,  1625,  1627,  1629,  1630,
    1631,  1632,  1633,  1635,  1637,  1639,  1640,  1642,  1645,  1649,
    1650,  1651,  1652,  1654,  1655,  1656,  1657,  1658,  1659,  1660,
    1661,  1664,  1665,  1668,  1669,  1670,  1671,  1672,  1673,  1674,
    1675,  1676,  1677,  1678,  1679,  1680,  1681,  1682,  1683,  1684,
    1685,  1686,  1687,  1688,  1689,  1690,  1691,  1692,  1693,  1694,
    1695,  1696,  1697,  1698,  1699,  1700,  1701,  1702,  1703,  1704,
    1705,  1706,  1707,  1708,  1709,  1710,  1711,  1712,  1713,  1714,
    1715,  1716,  1720,  1726,  1727,  1728,  1729,  1730,  1731,  1732,
    1733,  1734,  1735,  1737,  1739,  1746,  1753,  1759,  1765,  1780,
    1795,  1796,  1797,  1798,  1799,  1800,  1801,  1804,  1805,  1806,
    1807,  1808,  1809,  1810,  1811,  1812,  1813,  1814,  1815,  1816,
    1817,  1818,  1819,  1820,  1821,  1824,  1825,  1828,  1829,  1830,
    1831,  1834,  1838,  1840,  1842,  1843,  1844,  1846,  1855,  1856,
    1857,  1860,  1863,  1868,  1869,  1873,  1874,  1877,  1880,  1881,
    1884,  1887,  1890,  1893,  1897,  1903,  1909,  1915,  1923,  1924,
    1925,  1926,  1927,  1928,  1929,  1930,  1931,  1932,  1933,  1934,
    1935,  1936,  1937,  1941,  1942,  1945,  1948,  1950,  1953,  1955,
    1959,  1962,  1966,  1969,  1973,  1976,  1982,  1984,  1987,  1988,
    1991,  1992,  1995,  1998,  2001,  2002,  2003,  2004,  2005,  2006,
    2007,  2008,  2009,  2010,  2013,  2014,  2017,  2018,  2019,  2022,
    2023,  2026,  2027,  2029,  2030,  2031,  2032,  2035,  2038,  2041,
    2044,  2046,  2050,  2051,  2054,  2055,  2056,  2057,  2060,  2063,
    2066,  2067,  2068,  2069,  2070,  2071,  2072,  2073,  2074,  2075,
    2078,  2079,  2082,  2083,  2084,  2085,  2087,  2089,  2090,  2093,
    2094,  2098,  2099,  2100,  2103,  2104,  2107,  2108,  2109,  2110
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
  "bytearrayhead", "bytes", "hexbytes", "fieldInit", "serInit",
  "constTypeArg", "f32seq", "f64seq", "i64seq", "i32seq", "i16seq",
  "i8seq", "boolSeq", "sqstringSeq", "classSeq", "objSeq", "methodSpec",
  "instr_none", "instr_var", "instr_i", "instr_i8", "instr_r",
  "instr_brtarget", "instr_method", "instr_field", "instr_type",
  "instr_string", "instr_sig", "instr_tok", "instr_switch", "instr_r_head",
  "instr", "labels", "tyArgs0", "tyArgs1", "tyArgs2", "sigArgs0",
  "sigArgs1", "sigArg", "className", "slashedName", "typeSpec",
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

#define YYPACT_NINF (-1398)

#define yypact_value_is_default(Yyn) \
  ((Yyn) == YYPACT_NINF)

#define YYTABLE_NINF (-581)

#define yytable_value_is_error(Yyn) \
  0

/* YYPACT[STATE-NUM] -- Index in YYTABLE of the portion describing
   STATE-NUM.  */
static const yytype_int16 yypact[] =
{
   -1398,  2337, -1398, -1398,  -166,   656, -1398,  -196,    98,  2579,
    2579, -1398, -1398,   127,   819,  -164,  -124,   -66,    37, -1398,
     308,   245,   245,   319,   319,  1896,    32, -1398,   656,   656,
     656,   656, -1398, -1398,   349, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398,   392,   392, -1398, -1398, -1398, -1398,   392,    74,
   -1398,   322,   128, -1398, -1398, -1398, -1398,   682, -1398,   392,
     245, -1398, -1398,   131,   146,   149,   174, -1398, -1398, -1398,
   -1398, -1398,   140,   245, -1398, -1398, -1398,   346, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398,  2222,    40,   201, -1398, -1398,   147,   180,
   -1398, -1398,   694,   524,   524,  2128,   198, -1398,  3124, -1398,
   -1398,   251,   245,   245,   152, -1398,   921,   555,   656,   140,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,  3124,
   -1398, -1398, -1398,  1041, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398, -1398,   142, -1398,   474,   142,
     284, -1398,  1810, -1398, -1398, -1398,  2811,    54,    36,   140,
     411,   422, -1398,   431,  1304,   438,   277,   527, -1398,   142,
      81,   140,   140,   140, -1398, -1398,   291,   586,   311,   334,
   -1398,  3572,  2222,   621, -1398,  3627,  2534,   313,   181,   283,
     306,   330,   344,   355,   373,   743,   379, -1398, -1398,   392,
     424,    68, -1398, -1398, -1398, -1398,   521,   656,   456,  2914,
     420,   132, -1398,   524, -1398,    78,   640, -1398,   445,   214,
     469,   760,   245,   245, -1398, -1398, -1398, -1398, -1398, -1398,
     468, -1398, -1398,    75,  1376, -1398,   480, -1398, -1398,   -27,
     921, -1398, -1398, -1398, -1398,   567, -1398, -1398, -1398, -1398,
     140, -1398, -1398,    62,   140,   640, -1398, -1398, -1398, -1398,
   -1398,   142, -1398,   780, -1398, -1398, -1398, -1398,  1698,   526,
     534,  1106,   540,   543,   559,   595,   607,   615,   627,   634,
     645,   647, -1398,   656,   516,   120,   578,   583,   140, -1398,
     656,   656,   656, -1398,  3124,   656,   656, -1398,   652,   654,
     656,    47,  3124, -1398, -1398,   597,   142,   469, -1398, -1398,
   -1398, -1398,  3117,   659, -1398, -1398, -1398, -1398, -1398, -1398,
     823, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398,   -55, -1398,  2222, -1398,  3301,   668,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398,   674, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398,   245, -1398,   245, -1398, -1398, -1398,   245,
     658,   -11,  2291, -1398, -1398, -1398,   667, -1398, -1398,     5,
   -1398, -1398, -1398, -1398,   790,   173, -1398, -1398,   604,   245,
     319,   248,   604,  1304,  1088,  2222,   404,   524,  2128,   681,
     392, -1398, -1398, -1398,   691,   245,   245, -1398,   245, -1398,
     245, -1398,   319, -1398,   224, -1398,   224, -1398, -1398,   699,
     704,   346,   706, -1398, -1398, -1398,   245,   245,  1244,  1263,
    1479,   598, -1398, -1398, -1398,   980,   140,   140, -1398,   709,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398,   714,    88, -1398,   656,   125,  3124,  1009,
     729, -1398,  2364, -1398,  1013,   732,   733,   741,  1304, -1398,
   -1398,   469, -1398, -1398,    95,    42,   737,  1020, -1398, -1398,
     832,   105, -1398,   656, -1398, -1398,    42,  1022,   446,   245,
     749,   750,   751,   753,   245,   245,   245,   319,   370,   882,
     245,   245,   245,   319,   193,   656,   656,   656,   140, -1398,
     140,   140,   140,  1593,   140,   140,  2222,  2222,   140, -1398,
   -1398,  1034,     6, -1398,   759,   769,   640, -1398, -1398, -1398,
     245, -1398, -1398, -1398, -1398, -1398, -1398,   271, -1398,   770,
   -1398,   961, -1398, -1398, -1398,   245,   245, -1398,    -4,  2433,
   -1398, -1398, -1398, -1398,   783, -1398, -1398,   787,   791, -1398,
   -1398, -1398, -1398,   792,   245,  1009,  3018, -1398, -1398,   779,
     245,   124,   136,   245,   524,  1077, -1398,   803,    71,  2632,
   -1398,  2222, -1398, -1398, -1398,   790,    58,   173,    58,    58,
      58,  1039,  1040, -1398, -1398, -1398, -1398, -1398, -1398,   812,
     813, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
    1698, -1398,   824,   469,   392,  3124, -1398,   604,   826,  1009,
     827,   828,   833,   841,   842,   843,   852, -1398,   743,   853,
   -1398,   848,    49,   940,   857,    30,    41, -1398, -1398, -1398,
   -1398, -1398, -1398,   392,   392, -1398,   858,   866, -1398,   392,
   -1398,   392, -1398,   870,    89,   656,   950, -1398, -1398, -1398,
   -1398,   656,   954, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398,   245,  3204,     0,   231,   656,   972,    -9,
     875,   879, -1398,   531,   886,   889,   901, -1398,  1187, -1398,
   -1398,   900,   909,  1218,  3073,   906,   907,   535,   593,   392,
     656,   140,   656, -1398, -1398,   915,   918,   245,   245,   245,
     319,   922,   929,   930,   939,   941,   942,   943,   944,   962,
     963,   968,   969,   656,   277,   277,   277,   971,   976,   978,
     245,   205, -1398, -1398,  3124,   979,   981, -1398, -1398, -1398,
   -1398,  1177, -1398, -1398,   606,    63,   934,  2222,  2222,  2059,
     878, -1398, -1398,   521,   161,   203,   524,  1260, -1398, -1398,
   -1398,  2715, -1398,   985,   -17,  2667,   221,   928,   245,   983,
     245,   140,   245,   343,   984,  3124,   535,    71, -1398,  3018,
     989,   993, -1398, -1398, -1398, -1398,   604, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398,   346,   245,   245,   319,    42,
    1267,  1009,   995,   563,   996,   999,   997, -1398,   412,   998,
   -1398,   998,   998,   998,   998,   998, -1398, -1398,   245, -1398,
     245,   245,  1003, -1398, -1398,   990,  1011,   469,  1012,  1014,
    1016,  1017,  1018,  1019,   245,   656, -1398,   140,   656,    24,
     656,  1021, -1398, -1398, -1398, -1398,   774, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,  1025,
    1070,  1086, -1398,  1076,  1029,   -57,  1302, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398,  1025,  1025, -1398,
    1785, -1398, -1398, -1398,  1030,   392,   250,   346,  1032,   656,
     491, -1398,  1009,  1042,  1033,  1044, -1398,  2364, -1398,    79,
   -1398,   418,   432,  1133,   479,   499,   525,   532,   545,   564,
     576,   577,   608,   617,   618,   626,   639, -1398,  1371, -1398,
     392, -1398,   245,  1045,    71,    71,   140,   737, -1398, -1398,
     346, -1398, -1398, -1398,  1036,   140,   140, -1398, -1398,  1046,
    1048,  1053,  1057, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398,   277,    71, -1398, -1398, -1398,
   -1398,   640, -1398,   245,  1059,  1177,  3124, -1398,  2222,   351,
     656, -1398, -1398,  1158, -1398, -1398,   662,   656, -1398, -1398,
    3124,   140,   245,   140,   245,   396,  3124,   535,  3347,   528,
     804, -1398,  1986, -1398,  1009,  3165,  1061, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398,  1054,  1056, -1398,
    1063,  1064,  1065,  1066,  1062,   535, -1398,  1230,  1068,  1069,
    2222,  1032,  1698, -1398,  1074,   928, -1398,  1350,  1310,  1312,
   -1398, -1398,  1082,  1083,   656,   663, -1398,    71,   604,   604,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,    50,  1372,
   -1398, -1398,    30, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
    1084,   277,   140,   245,   140, -1398, -1398, -1398, -1398, -1398,
   -1398,  1134, -1398, -1398, -1398, -1398,  1009,  1089,  1090, -1398,
   -1398, -1398, -1398, -1398, -1398,  1024, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398,   486, -1398,    97,    34, -1398, -1398,  2178,
   -1398,  1092,   469, -1398,  1097, -1398, -1398, -1398, -1398,  1104,
   -1398, -1398, -1398, -1398,   469,   446,   245,   245,   245,   665,
     671,   679,   686,   245,   245,   245,   245,   245,   245,   319,
     245,   370,   245,   882,   245,   245,   245,   245,   245,   245,
     245,   319,   245,  3315,   245,   223,   245,   171,   245, -1398,
   -1398, -1398,  2533,  1099,  1107, -1398,  1114,  1115,  1121,  1122,
   -1398,  1256, -1398, -1398, -1398, -1398,  1128,  1130,   245, -1398,
      88,  1131,  1136, -1398,   271, -1398,   351,  1304, -1398,   140,
      88,  1135,  1137,  2222,  1698,  1175, -1398,  1304,  1304,  1304,
    1304, -1398, -1398, -1398, -1398, -1398, -1398,  1304,  1304,  1304,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398,   469, -1398,   245,
     556,   713, -1398, -1398, -1398, -1398,  3204,  1132,   346, -1398,
    1141, -1398, -1398,  1416, -1398,   346, -1398,   346,   245, -1398,
   -1398,   140, -1398,  1142, -1398, -1398, -1398,   245, -1398,  1138,
   -1398, -1398,  1145,   440,   245,   245, -1398, -1398, -1398, -1398,
   -1398, -1398,  1009,  1139, -1398, -1398,   245, -1398,  -119,  1149,
    1154,  1179,  1155,  1156,  1157,  1159,  1161,  1162,  1166,  1167,
    1169,  1170, -1398,   469, -1398, -1398,   245,   755, -1398,  1496,
    1176,  1144,  1171,  1174,  1186,   245,   245,   245,   245,   245,
     245,   319,   245,  1183,  1188,  1191,  1189,  1194,  1192,  1195,
    1193,  1198,  1199,  1206,  1211,  1213,  1210,  1215,  1212,  1217,
    1214,  1220,  1222,  1221,  1223,  1236,  1233,  1238,  1243,  1240,
    1246,  1457,  1247,  1251, -1398,   255, -1398,   212, -1398, -1398,
    1250, -1398, -1398,    71,    71, -1398, -1398, -1398,  1257,   351,
   -1398,  2222, -1398, -1398,   602, -1398,  1180, -1398,  1535,   524,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398,  3328,  1261, -1398,
   -1398, -1398, -1398,  1262,  1264, -1398,  2222,   535, -1398, -1398,
   -1398, -1398,  1547,    30,   245,  1009,  1265,  1266,   469, -1398,
    1268,   245, -1398,  1269,  1272,  1274,  1276,  1277,  1259,  1270,
    1281,  1282,  1586, -1398, -1398, -1398,  1285, -1398,  1291,  1292,
    1287,  1293,  1290,  1295,  1294,  1296,  1297, -1398,  1298, -1398,
    1300, -1398,  1301, -1398,  1303, -1398, -1398,  1313, -1398, -1398,
    1314, -1398,  1316, -1398,  1318, -1398,  1329, -1398,  1332, -1398,
    1333, -1398, -1398,  1338, -1398,  1342, -1398,  1344,  1563, -1398,
    1341,   689, -1398,  1346,  1347, -1398, -1398, -1398,    71,  2222,
     535,  3124, -1398, -1398, -1398,    71, -1398,  1345, -1398,  1355,
    1352,   328, -1398,  3535, -1398,  1349, -1398,   245,   245,   245,
   -1398, -1398, -1398, -1398, -1398,  1356, -1398,  1365, -1398,  1374,
   -1398,  1375, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,  3315, -1398,
   -1398,  1354, -1398,  1345,  1698,  1377,  1368,  1379, -1398,    30,
   -1398,  1009, -1398,   250, -1398,  1382,  1383,  1384,   185,    83,
   -1398, -1398, -1398, -1398,   108,   134,   137,   111,   195,   233,
     144,   162,   182,   115,  1952,    72,   574, -1398,  1032,  1388,
    1651, -1398,    71, -1398,   612, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398,   187,   188,   190,   154, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,  1663,
   -1398, -1398, -1398,    71,   535,  1173,  1389,  1009, -1398, -1398,
   -1398, -1398, -1398,  1391,  1393,  1395, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398,   697,  1436,    71,   245, -1398,  1589,  1398,  1399,
     524, -1398, -1398,  3124,  1698,  1678,   535,  1345,  1407,    71,
    1410, -1398
};

/* YYDEFACT[STATE-NUM] -- Default reduction number in state STATE-NUM.
   Performed when YYTABLE does not specify something else to do.  Zero
   means the default is an error.  */
static const yytype_int16 yydefact[] =
{
       2,     0,     1,    86,   106,     0,   269,   213,   394,     0,
       0,   783,   784,     0,   226,     0,     0,   798,   804,   861,
      93,     0,     0,     0,     0,     0,     0,    29,     0,     0,
       0,     0,    58,    59,     0,    61,     3,    25,    26,    27,
      84,    85,   438,   438,    19,    17,    10,     9,   438,     0,
     109,   136,     0,     7,   276,   340,     8,     0,    18,   438,
       0,    11,    12,     0,     0,     0,     0,   840,    37,    40,
      38,    39,   105,     0,   193,   395,   396,   393,   768,   769,
     770,   771,   772,   773,   774,   775,   776,   777,   778,   779,
     780,   781,   782,     0,     0,    34,   220,   221,     0,     0,
     227,   228,   233,   226,   226,     0,    62,    72,     0,   224,
     219,     0,     0,     0,     0,   804,     0,     0,     0,    94,
      42,    20,    21,    44,    43,    23,    24,   576,   734,     0,
     711,   719,   717,     0,   720,   721,   722,   723,   724,   725,
     730,   731,   732,   733,   693,   718,     0,   710,     0,     0,
       0,   514,     0,   577,   578,   579,     0,     0,     0,   580,
       0,     0,   240,     0,   226,     0,   574,     0,   715,    30,
      53,    55,    56,    57,    60,   440,     0,   439,     0,     0,
       2,     0,     0,   138,   140,   226,     0,     0,   401,   401,
     401,   401,   401,   401,     0,     0,     0,   391,   398,   438,
       0,   786,   814,   832,   850,   864,     0,     0,     0,     0,
       0,     0,   575,   226,   582,   744,   585,    32,     0,     0,
     746,     0,     0,     0,   229,   230,   231,   232,   222,   223,
       0,    74,    73,     0,     0,   104,     0,    22,   799,   800,
       0,   805,   806,   807,   809,     0,   810,   811,   812,   813,
     803,   862,   863,   859,    95,   716,   726,   727,   728,   729,
     692,     0,   695,     0,   712,   714,   238,   239,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   696,     0,     0,     0,     0,     0,   709,   707,
       0,     0,     0,   235,     0,     0,     0,   701,     0,     0,
       0,   737,   559,   700,   699,     0,    30,    54,    65,   441,
      69,   103,     0,     0,   112,   133,   110,   111,   114,   115,
       0,   116,   117,   118,   119,   120,   121,   122,   123,   113,
     132,   125,   124,   134,   148,   137,     0,   108,     0,     0,
     282,   277,   278,   279,   280,   281,   285,   283,   293,   284,
     286,   287,   288,   289,   290,   291,   292,     0,   294,   318,
     515,   516,   517,   518,   519,   520,   521,   522,   523,   524,
     525,   526,   527,     0,   376,     0,   339,   347,   348,     0,
       0,     0,     0,   369,     6,   354,     0,   356,   355,     0,
     341,   362,   340,   343,     0,     0,   349,   529,     0,     0,
       0,     0,     0,   226,     0,     0,     0,   226,     0,     0,
     438,   350,   352,   353,     0,     0,     0,   417,     0,   416,
       0,   415,     0,   414,     0,   412,     0,   413,   437,     0,
     400,     0,     0,   745,   795,   785,     0,     0,     0,     0,
       0,     0,   843,   842,   841,     0,   838,    41,   214,     0,
     200,   194,   195,   196,   197,   202,   203,   204,   205,   199,
     206,   207,   198,     0,     0,   392,     0,     0,     0,     0,
       0,   754,   748,   753,     0,    35,     0,     0,   226,    76,
      70,    63,   315,   316,   737,   317,   557,     0,    97,   801,
     797,   830,   808,     0,   694,   713,   237,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,   708,   706,
      51,    52,    50,     0,    49,   581,     0,     0,    48,   738,
     697,   739,     0,   735,     0,   560,   561,    28,    31,     5,
       0,   126,   127,   128,   129,   130,   131,   157,   107,   139,
     143,     0,   106,   243,   257,     0,     0,   840,     0,     0,
       4,   185,   186,   179,     0,   141,   175,     0,     0,   340,
     176,   177,   178,     0,     0,   299,     0,   342,   344,     0,
       0,     0,     0,     0,   226,     0,   351,     0,   318,     0,
     386,     0,   384,   387,   370,   372,     0,     0,     0,     0,
       0,     0,     0,   373,   531,   530,   532,   533,    45,     0,
       0,   528,   535,   534,   538,   537,   539,   543,   544,   542,
       0,   545,     0,   546,   438,     0,   550,   552,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   397,     0,     0,
     405,     0,   788,     0,     0,     0,     0,    13,   826,   825,
     817,   815,   818,   438,   438,   837,     0,     0,    14,   438,
     835,   438,   833,     0,     0,     0,     0,    15,   858,   857,
     851,     0,     0,    16,   869,   868,   865,   844,   845,   846,
     847,   848,   849,     0,   586,   209,     0,   583,     0,     0,
       0,   755,    76,     0,     0,     0,   749,    33,     0,   225,
     234,    66,     0,    79,   559,     0,     0,     0,     0,   438,
       0,   860,     0,   758,   759,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   572,   570,   571,   700,     0,     0,
     741,   737,   698,   705,     0,     0,     0,   152,   154,   153,
     155,   160,   150,   151,   157,     0,     0,     0,     0,     0,
     226,   180,   181,     0,     0,     0,   226,     0,   140,   246,
     260,     0,   850,     0,   299,     0,     0,   270,     0,     0,
       0,   364,     0,     0,     0,     0,     0,   318,   567,     0,
       0,   564,   565,   368,   385,   371,     0,   388,   378,   382,
     383,   381,   377,   379,   380,     0,     0,     0,     0,   541,
       0,     0,     0,     0,   555,   556,     0,   536,     0,   401,
     402,   401,   401,   401,   401,   401,   399,   404,     0,   787,
       0,     0,     0,   820,   819,     0,     0,   823,     0,     0,
       0,     0,     0,     0,     0,     0,   856,   852,     0,     0,
       0,     0,   602,   640,   594,   595,     0,   629,   596,   597,
     598,   599,   600,   601,   631,   607,   608,   609,   610,   641,
       0,     0,   637,     0,     0,     0,   591,   592,   593,   616,
     617,   618,   635,   619,   620,   621,   622,   641,   641,   625,
     643,   633,   639,   274,     0,     0,   272,     0,   211,   584,
       0,   742,     0,     0,    38,     0,   747,   748,    36,     0,
      64,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,    78,    75,   445,
     438,    77,     0,     0,   318,   318,   317,   557,    98,    99,
       0,   100,   101,   102,     0,   831,   236,   488,   487,     0,
       0,     0,     0,   478,   477,   476,   475,   473,   471,   474,
     472,   486,   485,   484,   483,   573,   318,   702,   703,   740,
     736,   562,   135,     0,     0,   160,     0,   158,   144,   165,
       0,   149,   142,     0,   245,   244,   580,     0,   259,   258,
       0,   839,     0,   188,     0,     0,     0,     0,     0,     0,
       0,   171,     0,   295,     0,     0,     0,   306,   307,   308,
     309,   301,   302,   303,   300,   304,   305,     0,     0,   298,
       0,     0,     0,     0,     0,     0,   359,   357,     0,     0,
       0,   211,     0,   360,     0,   270,   345,   318,     0,     0,
     374,   375,     0,     0,     0,     0,   548,   318,   552,   552,
     551,   403,   411,   410,   409,   408,   406,   407,   792,   790,
     816,   827,     0,   829,   821,   824,   802,   828,   834,   836,
       0,   853,   854,     0,   867,   208,   630,   603,   604,   605,
     606,     0,   626,   632,   634,   638,     0,     0,     0,   636,
     623,   624,   678,   647,   648,     0,   675,   649,   650,   651,
     652,   653,   654,   677,   659,   660,   661,   662,   645,   646,
     667,   668,   669,   670,   671,   672,   673,   674,   644,   679,
     680,   681,   682,   683,   684,   685,   686,   687,   688,   689,
     690,   691,   663,   627,   201,     0,     0,   611,   210,     0,
     192,     0,   763,   761,     0,   760,   757,   756,   743,     0,
      79,   750,    76,    71,    67,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,    82,
      83,    81,     0,     0,     0,   558,     0,     0,     0,     0,
      96,   800,   482,   481,   480,   479,     0,     0,     0,   161,
       0,     0,   145,   146,   157,   164,   165,   226,   191,   241,
       0,     0,     0,     0,     0,     0,   172,   226,   226,   226,
     226,   173,   254,   255,   253,   247,   252,   226,   226,   226,
     174,   267,   268,   265,   261,   266,   182,   299,   297,     0,
       0,     0,   319,   320,   321,   322,   586,   148,     0,   363,
       0,   366,   367,     0,   346,   568,   566,     0,     0,    46,
      47,   540,   547,     0,   553,   554,   791,     0,   789,     0,
     855,   866,     0,     0,     0,     0,   676,   655,   656,   657,
     658,   665,     0,     0,   666,   273,     0,   612,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   444,   443,   442,   212,     0,     0,    79,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,    89,     0,    88,     0,    87,   436,
       0,   218,   217,   318,   318,   796,   704,   156,     0,   165,
     167,     0,   166,   163,     0,   187,     0,   190,     0,   226,
     248,   249,   250,   251,   264,   262,   263,     0,     0,   310,
     311,   312,   313,     0,     0,   358,     0,     0,   569,   389,
     390,   549,   794,     0,     0,     0,     0,     0,   628,   664,
       0,     0,   613,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   751,    68,   435,     0,   434,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   425,     0,   424,
       0,   423,     0,   422,     0,   420,   418,     0,   421,   419,
       0,   433,     0,   432,     0,   431,     0,   430,     0,   451,
       0,   447,   446,     0,   450,     0,   449,     0,     0,    91,
       0,     0,   170,     0,     0,   159,   162,   147,   318,     0,
       0,     0,   296,   314,   271,   318,   365,   168,   793,     0,
       0,     0,   589,   586,   615,     0,   762,     0,     0,     0,
     767,   752,   503,   499,   429,     0,   428,     0,   427,     0,
     426,     0,   501,   499,   497,   495,   489,   492,   501,   499,
     497,   495,   512,   505,   448,   508,    90,    92,     0,   216,
     215,     0,   189,   168,     0,     0,     0,     0,   169,     0,
     642,     0,   588,   590,   614,     0,     0,     0,     0,     0,
     501,   499,   497,   495,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,    80,   211,     0,
       0,   323,   318,   822,     0,   764,   765,   766,   467,   504,
     466,   500,     0,     0,     0,     0,   457,   502,   456,   455,
     498,   454,   496,   452,   491,   490,   453,   494,   493,   461,
     460,   459,   458,   470,   513,   507,   506,   468,   509,     0,
     469,   511,   256,   318,     0,     0,     0,     0,   465,   464,
     463,   462,   510,     0,     0,     0,   328,   324,   333,   334,
     335,   336,   337,   325,   326,   327,   329,   330,   331,   332,
     275,   361,     0,     0,   318,     0,   587,     0,     0,     0,
     226,   183,   338,     0,     0,     0,     0,   168,     0,   318,
       0,   184
};

/* YYPGOTO[NTERM-NUM].  */
static const yytype_int16 yypgoto[] =
{
   -1398,  1523, -1398,  1402,   -43,     8,    51,    -5,    11,    43,
    -418, -1398,    16,   -15,  1684, -1398, -1398,  1232,  1308,  -662,
   -1398, -1042, -1398,    28, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398,  -326, -1398, -1398, -1398,   949, -1398, -1398,
   -1398,   454, -1398,   964, -1398,   754,   510, -1141, -1398, -1397,
    -448, -1398,  -325, -1398, -1398, -1009, -1398,  -163,   -99, -1398,
      -7,  1707, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398,   698,   482, -1398,  -318, -1398,  -739,  -704,  1380, -1398,
   -1398,  -232, -1398,  -165, -1398, -1398,  1148, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398,   -64,    15, -1398, -1398, -1398,
    1108,  -137,  1687,   609,   -29,   -12,   850, -1398, -1146, -1398,
   -1398, -1398, -1338, -1308, -1312, -1287, -1398, -1398, -1398, -1398,
      14, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398, -1398, -1398, -1398, -1398,  -117,   810,  1047, -1398,
    -746, -1398,   712,   -14,  -437,   -20,   253,   107, -1398,   -23,
     565, -1398,  1031,    13,   851, -1398, -1398,   869, -1398,  -881,
   -1398,  1749, -1398,    33, -1398, -1398,   572,  1284, -1398,  1660,
   -1398, -1398, -1035,  1351, -1398, -1398, -1398, -1398, -1398, -1398,
   -1398, -1398,  1227,  1023, -1398, -1398, -1398, -1398, -1398
};

/* YYDEFGOTO[NTERM-NUM].  */
static const yytype_int16 yydefgoto[] =
{
       0,     1,    36,   305,   689,   385,    71,   159,   834,  1602,
     613,    38,   387,    40,    41,    42,    43,   106,   230,   702,
     703,   928,  1192,   388,  1367,    45,    46,   708,    47,    48,
      49,    50,    51,    52,   181,   183,   337,   338,   549,  1211,
    1212,   548,   754,   755,   975,   976,   756,  1215,   980,  1547,
    1548,   565,    53,   209,   898,  1140,    74,   107,   108,   109,
     212,   231,   567,   759,   999,  1235,   568,   760,  1000,  1244,
      54,  1025,   894,   895,    55,   185,   775,   486,   789,  1625,
     389,   186,   390,   797,   392,   393,   594,   394,   395,   595,
     596,   597,   598,   599,   600,   798,   396,    57,    77,   197,
     429,   417,   430,   929,   930,   176,   177,  1315,   931,   282,
    1568,  1569,  1567,  1566,  1559,  1564,  1558,  1575,  1576,  1574,
     213,   397,   398,   399,   400,   401,   402,   403,   404,   405,
     406,   407,   408,   409,   410,   411,   816,   706,   534,   535,
     790,   791,   792,   214,   166,   232,   896,  1082,  1133,   216,
     168,   532,   533,   412,   695,   696,    59,   690,   691,   715,
    1146,    93,    60,   413,    62,   114,   490,   659,    63,   116,
     438,   651,   835,   652,   653,   661,   654,    64,   439,   662,
      65,   573,   206,   440,   670,    66,   117,   441,   676
};

/* YYTABLE[YYPACT[STATE-NUM]] -- What to do in state STATE-NUM.  If
   positive, shift that token.  If negative, reduce the rule whose
   number is the opposite.  If YYTABLE_NINF, syntax error.  */
static const yytype_int16 yytable[] =
{
      72,   293,   167,   937,   228,   229,   635,   110,   636,    37,
     160,   165,   564,   566,    58,   119,    56,    39,   162,  1145,
     569,   391,  1262,   170,   171,   172,   173,  1279,   199,    44,
     903,   178,   121,   122,    61,  1005,   179,  1355,   705,   164,
     120,  1034,    68,    69,   120,    70,   120,   200,   217,   217,
      67,   220,   419,   421,   423,   425,   427,   120,   829,  1276,
      68,    69,  -580,    70,   897,   294,   125,   126,   529,    68,
      69,   201,    70,   215,   218,  1383,   434,   435,   734,   735,
     736,  1615,  1033,   217,   208,   234,   359,   217,  1073,   217,
     726,   728,   788,   120,    68,    69,    99,    70,   162,   120,
     581,    68,    69,    73,    70,   120,   255,   764,  1319,   239,
     309,   250,   253,   254,   468,   796,   529,  1616,   120,   164,
    1004,   123,   124,   236,   237,   123,   124,   307,   210,   268,
      68,    69,   260,    70,   111,   262,    95,    99,    68,    69,
     489,    70,    68,    69,   120,    70,  1579,   120,    68,    69,
     286,    70,   285,   288,   120,   306,   127,   110,    68,    69,
     589,    70,   335,    75,   123,   124,   199,    68,    69,   289,
      70,  1087,   120,  1421,    99,   112,   334,    68,    69,    68,
      70,  1422,    70,   120,    99,   127,   464,   432,  1196,  1197,
     481,  1088,   120,  1573,   297,   298,   299,   120,   120,   493,
     120,   446,   447,   587,   286,   120,   467,   608,   113,    68,
      69,  1565,    70,    99,   487,   120,  1360,  1571,    68,    69,
    1206,    70,  1572,  1361,    76,  1595,   529,   609,   610,   485,
     603,   120,  1357,   476,   477,   491,   608,   386,  1486,   207,
     616,  1570,   710,   123,   124,   608,   547,   494,   497,  1593,
     735,   713,   714,   115,  1594,   120,   609,   610,   123,   124,
     608,    68,    69,   496,    70,   609,   610,   207,  1358,   127,
    1668,   523,   283,  1592,  1321,   474,  1432,   582,   514,   536,
     609,   610,   518,   474,   765,   520,   521,   522,   583,   900,
     524,   525,   306,  1225,   207,   528,   965,    99,   519,   741,
     588,  1273,   153,   154,   155,   845,   742,  1478,   625,   263,
     264,   746,   531,   466,    68,    69,   550,    70,   207,   265,
      37,  1257,   169,   747,   748,    58,  1296,    56,    39,   123,
     124,   153,   154,   155,  1297,   833,   207,   771,   219,   836,
      44,   749,   830,  1277,   287,    61,   561,   530,   383,  -580,
     466,   571,    68,   570,   562,    70,   207,   174,   978,   284,
     238,   436,   585,   623,   180,   479,   563,  -563,  1617,  1152,
     480,   572,   437,   469,  1153,   207,   470,   624,  1499,  1590,
     120,   620,   608,   182,   577,   621,   578,   301,  1295,   302,
     579,   750,  1577,   303,   304,   530,   110,   619,   628,   207,
     284,   162,   609,   610,  1596,   175,   602,  1601,  1071,   605,
     606,  1612,   217,   615,   207,   153,   154,   155,   184,   207,
     516,   202,   164,   780,   391,   687,   630,   631,  1614,   632,
    1598,   633,   801,  1599,   207,   782,   203,   284,   194,   204,
    1609,   157,   222,   607,   612,   688,   601,   641,   642,   604,
    1631,   650,   694,   614,   648,   648,   668,   674,  1610,   685,
     992,   686,   965,   383,   205,   634,   649,   649,   669,   675,
     157,   110,  1020,  1021,  1022,   223,   415,   286,  1611,   467,
     416,  1588,   639,  1628,  1629,   785,  1630,   207,   711,  1198,
    1320,  1603,   622,   733,   221,   531,   233,    68,    69,   217,
      70,   120,   994,   487,   472,   127,   738,   739,  1397,   473,
     716,  1480,   713,   714,  1583,   721,   722,   723,   485,   725,
    1141,   729,   730,   731,   118,   207,   261,    68,    69,  1606,
      70,   899,   774,    99,   802,   803,   804,    68,    69,   767,
     904,    68,    69,   611,    70,   127,   235,     3,   290,  1136,
     724,   745,   727,   777,   157,  1137,   732,   713,   714,   291,
     751,    68,    69,   752,    70,    98,   761,   762,   292,  1622,
     100,   794,   101,    99,   753,   295,   781,   783,   418,   102,
      68,    69,   416,    70,   296,   773,   818,   308,   127,    68,
      69,   779,    70,   120,   784,   442,   103,   443,   444,   309,
     810,   420,   813,   837,   445,   416,   391,   310,   800,   199,
      68,   104,   812,    70,   120,   809,    99,     3,   414,  1618,
     474,  1551,   938,   939,  1552,   422,  1619,  1483,  1484,   416,
     311,   251,   252,   297,   298,   299,  1030,   207,   815,   424,
     386,   838,   839,   416,  1214,   207,   746,   842,   799,   843,
     426,   153,   154,   155,   416,   482,   483,   940,   747,   748,
     847,   990,    68,    69,   300,    70,   849,   996,   428,   297,
     298,   299,   564,   566,   431,   846,   749,  1589,   814,   260,
     569,   536,  1052,   336,  1053,  1054,  1055,  1056,  1057,  1223,
     207,   153,   154,   155,   851,  1399,  1400,   944,  1227,  1228,
    1229,  1230,   936,  1497,   474,   945,   187,   946,  1051,   188,
     189,   190,   191,  1155,   192,   193,   194,  1156,   465,   927,
     433,   971,    11,    12,    13,    14,   750,  1157,   949,   950,
     951,  1158,   474,  1415,   153,   154,   155,   982,   983,   987,
     471,   224,  1541,   225,   226,   227,   297,   298,   299,  1545,
     979,   969,   531,  1342,   986,  1345,   448,  1291,   991,   993,
     995,   474,  1032,   952,   478,   120,  1035,   187,  1045,   475,
     188,   189,   190,   191,  1163,   192,   193,   194,  1164,  1292,
     488,   936,  1428,  1429,  1430,  1293,  1543,  1023,   492,  1026,
     157,  1028,  1294,  1029,  1165,    14,   941,   942,  1166,   943,
    1076,  1077,  1078,  1079,  1080,   671,   495,  1039,   672,    28,
      29,    30,    31,    32,    33,    34,   515,  1041,  1042,  1231,
    1167,   498,   386,     3,  1168,    35,   301,  1169,   302,   499,
     157,  1170,   303,   304,    96,   504,  1626,    97,   505,  1058,
    1171,  1059,  1060,  1072,  1172,  1074,   590,  1038,   591,   592,
     593,  1043,  1401,  1402,   506,  1070,  1040,  1142,  1047,  1173,
      98,    99,   301,  1174,   302,   100,  1154,   101,   303,   304,
    1620,  1175,  1177,   157,   102,  1176,  1178,  1633,   517,    28,
      29,    30,    31,    32,    33,    34,  1144,   694,   537,   673,
     507,   103,   123,   124,   608,    35,   207,  1488,   752,   541,
     542,   543,   508,  1179,   474,  1627,   104,  1180,  1658,   753,
     509,  1143,  1181,  1183,   609,   610,  1182,  1184,  1193,    98,
    1634,  1185,   510,  1670,   100,  1186,   101,    68,    69,   511,
      70,  1274,  1275,   102,  1187,   544,   545,   546,  1188,   301,
     512,   302,   513,  1194,  1191,   303,   304,   526,  1138,   527,
     103,   988,  -242,  1210,   540,   474,   207,   580,  1213,  1272,
    1325,  1247,  1667,   574,  1326,   104,  1327,  1220,  1218,   575,
    1328,   586,   195,  1224,  1329,  1216,   627,  1237,  1330,  1238,
    1239,  1331,  1219,   207,  1207,  1332,   629,  1538,   196,   474,
     637,  1200,   936,  1656,  1090,  1091,   989,   638,    11,    12,
      13,    14,   640,  1221,   683,  1222,   561,  1232,  1241,   684,
    1261,   571,  1263,   570,   562,  1233,  1242,   217,   668,   692,
     936,   697,   241,   242,   243,   698,   563,  1234,  1243,   699,
     669,   572,  1236,  1245,   297,   298,   299,   700,   704,  1271,
     707,   656,   712,  1283,   717,   718,   719,   244,   720,   196,
    1286,  1287,  1288,  1289,  1290,   740,   677,   678,   679,   815,
     815,   743,   744,   757,  1390,  1391,  1392,  1393,   256,   257,
     258,   259,   758,   768,  1394,  1395,  1396,   769,   297,   298,
     299,   770,   772,   778,  1281,    28,    29,    30,    31,    32,
      33,    34,   680,   681,   682,  1240,  1313,   786,   787,   814,
     814,    35,   805,   806,   617,   128,   618,   807,   808,   129,
     130,   131,   132,   133,   105,   134,   135,   136,   137,   811,
     138,   139,   817,   819,   140,   141,   142,   143,   820,   821,
      99,   144,   145,   500,   501,   502,   503,   822,   823,   824,
     146,   245,   147,   246,   247,   248,   249,  1298,   825,   827,
    1605,  1608,   828,   831,  1024,   832,   840,   148,   149,   150,
    1159,  1160,  1161,  1162,   841,   844,   848,  1322,  1323,  1324,
     850,   901,   902,  1362,  1333,  1334,  1335,  1336,  1337,  1338,
     906,  1340,  1341,  1343,   905,  1346,  1347,  1348,  1349,  1350,
    1351,  1352,   151,  1354,   907,  1356,   908,  1359,   909,  1363,
     910,   934,   935,  1387,  1388,  1379,  1423,  1424,  1425,  1426,
     110,   947,  1339,  1635,   948,  1384,  1344,   974,   953,  1378,
     110,   110,   110,   110,  1353,   954,   955,   301,  1636,   302,
     110,   110,   110,   303,   304,   956,   981,   957,   958,   959,
     960,   911,   912,   913,  1637,   914,   915,   916,   917,  1418,
     918,   919,   194,  1638,   920,   921,   922,   923,   961,   962,
    1398,   924,   925,     3,   963,   964,   966,  1639,  1640,  1641,
    1642,   301,   967,   302,   968,   972,   973,   737,   304,  1410,
     997,  1003,     3,  1027,  1031,  1036,  1037,  1044,  1412,  1048,
    1491,  1046,  1049,  1050,  1062,  1416,  1417,   416,  1061,  1643,
    1644,  1645,  1646,  1647,  1648,  1649,  1063,  1420,  1064,  1405,
    1065,  1066,  1067,  1083,  1068,  1069,  1408,  1075,  1409,    96,
    1081,  1084,    97,  1085,  1086,  1089,  1134,  1427,  1431,   926,
    1139,  1149,  1201,  1148,  1150,  1191,  1439,  1440,  1441,  1442,
    1443,  1444,  1202,  1446,  1203,    98,    99,  1195,   655,  1204,
     100,  1479,   101,  1205,  1208,  1217,  1249,  1256,  1250,   102,
    1251,  1487,  1481,  1252,  1253,  1254,  1255,  1258,  1259,  1260,
    1264,   788,  1501,  1267,  1445,  1268,   103,   156,  1269,  1270,
    1280,  1278,    68,    69,  1282,    70,  1496,  1316,  1284,  1285,
     127,   104,  1317,   128,  1318,  1369,   158,   129,   130,   131,
     132,   133,   936,   134,   135,   136,   137,  1370,   138,   139,
    1371,  1372,   140,   141,   142,   143,  1373,  1374,    99,   144,
     145,     9,    10,   489,  1376,  1500,  1377,  1380,   146,  1381,
     147,  1389,  1505,   547,  1406,  1385,  1407,  1386,  1411,  1419,
    1435,    14,  1413,  1414,  1155,   148,   149,   150,  1191,  1157,
    1163,  1165,  1167,   643,  1169,   644,  1171,  1173,   645,   646,
      14,  1175,  1177,  1650,  1179,  1181,  1475,  1434,  1544,  1542,
    1437,  1436,   656,  1489,   644,   657,  1189,   645,   646,  1447,
     151,  1190,   297,   298,   299,   936,  1438,  1449,  1448,  1450,
    1451,  1453,  1452,  1454,  1455,  1456,   482,   483,     3,    28,
      29,    30,    31,    32,    33,    34,  1457,  1458,  1584,  1459,
    1460,  1461,  1462,  1463,  1464,    35,  1465,  1467,  1555,  1556,
    1557,   663,  1466,  1468,  1580,    28,    29,    30,    31,    32,
      33,    34,  1469,  1470,  1471,   647,   153,   154,   155,  1472,
    1473,    35,  1474,  1476,    28,    29,    30,    31,    32,    33,
      34,  1477,  1482,  1485,   658,  1490,  1498,  1493,  1494,  1495,
      35,  1663,  1621,  1507,  1325,  1502,  1503,  1327,  1504,  1329,
    1591,  1331,  1536,  1506,  1508,  1597,  1591,  1600,  1510,  1604,
    1512,  1597,  1591,  1600,  1652,  1509,  1513,  1515,  1514,  1516,
    1517,  1518,  1520,  1522,  1519,  1523,  1524,  1521,  1525,    68,
      69,  1189,    70,  1597,  1591,  1600,  1190,   127,  1526,  1527,
     128,  1528,  1607,  1529,   129,   130,   131,   132,   133,   936,
     134,   135,   136,   137,  1530,   138,   139,  1531,  1532,   140,
     141,   142,   143,  1533,   664,    99,   144,   145,  1534,  1535,
    1664,  1537,  1539,  1540,  1665,   146,  1546,   147,  1550,  1554,
    1578,  1560,    28,    29,    30,    31,    32,    33,    34,  1549,
    1561,   936,   148,   149,   150,   156,  1659,   932,    35,  1562,
    1563,  1624,  1632,  1581,  1582,   484,    14,   302,  1585,  1586,
    1587,   303,   304,  1623,   158,  1651,   665,  1653,  1654,   666,
    1655,  1189,  1657,  1660,  1661,  1662,  1190,   151,  1666,   297,
     298,   299,  1669,   312,    68,    69,  1671,    70,   538,   161,
     701,  1404,   127,   482,   483,   128,   626,   998,   977,   129,
     130,   131,   132,   133,  1382,   134,   135,   136,   137,  1209,
     138,   139,   163,  1265,   140,   141,   142,   143,  1403,   576,
      99,   144,   145,   795,   198,  1135,   826,  1199,  1314,  1266,
     146,   933,   147,   153,   154,   155,  1553,  1368,  1151,    94,
      28,    29,    30,    31,    32,    33,    34,   148,   149,   150,
     667,  1147,   970,  1375,   709,   240,    35,    28,    29,    30,
      31,    32,    33,    34,   763,     0,     0,  1433,  1092,     0,
     660,     0,     0,    35,     0,  1002,     0,     0,     0,     0,
       0,     0,   151,     0,   297,   298,   299,  1093,  1094,     0,
    1095,  1096,  1097,  1098,  1099,  1100,     0,  1101,  1102,     0,
    1103,  1104,  1105,  1106,  1107,     0,   266,   128,   267,     0,
       0,   129,   130,   131,   132,   133,     0,   134,   135,   136,
     137,     0,   138,   139,     0,     0,   140,   141,   142,   143,
       0,     0,     0,   144,   145,     0,     0,     0,   153,   154,
     155,     0,   146,     0,   147,     0,     0,    28,    29,    30,
      31,    32,    33,    34,     0,     0,     0,  1511,     0,   148,
     149,   150,   156,    35,     0,     0,     0,     0,     0,     0,
       0,     0,   484,     0,   302,     0,     0,     0,   737,   304,
       0,   158,    68,    69,     0,    70,     0,     0,     0,     0,
     127,     0,     0,   128,   151,     0,     0,   129,   130,   131,
     132,   133,     0,   134,   135,   136,   137,     0,   138,   139,
       0,     0,   140,   141,   142,   143,     0,     0,    99,   144,
     145,     0,     0,     0,     0,     0,     0,     0,   146,     0,
     147,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,   148,   149,   150,     0,     0,
       0,     0,     0,     0,     0,   911,   912,   913,     0,   914,
     915,   916,   917,     0,   918,   919,   194,   156,   920,   921,
     922,   923,     0,     0,     0,   924,   925,   484,     0,   302,
     151,   152,     0,   303,   304,     3,   158,     0,     0,     0,
       0,     0,     0,     0,     0,  1108,  1109,     0,  1110,  1111,
    1112,     0,  1113,  1114,     0,     0,  1115,  1116,   663,  1117,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,  1118,  1119,  1120,  1121,  1122,  1123,  1124,  1125,
    1126,  1127,  1128,  1129,  1130,  1131,   153,   154,   155,     0,
       0,     0,     0,   926,     0,    68,    69,     0,    70,     0,
       0,     0,     0,   127,     0,     0,   128,     0,     0,     0,
     129,   130,   131,   132,   133,     0,   134,   135,   136,   137,
    1132,   138,   139,    14,     0,   140,   141,   142,   143,   156,
       0,    99,   144,   145,     0,     0,     0,     0,     0,     0,
       0,   146,     0,   147,     0,     0,     0,     0,   158,     0,
       0,     0,     0,     0,     0,     0,     0,     0,   148,   149,
     150,     0,   984,     0,    68,    69,     0,    70,     0,     0,
       0,   664,   127,     0,     0,   128,     0,     0,     0,   129,
     130,   131,   132,   133,     0,   134,   135,   136,   137,     0,
     138,   139,     0,   151,   140,   141,   142,   143,     0,     0,
      99,   144,   145,     0,     0,     0,     0,   985,     0,     0,
     146,     0,   147,    14,     0,   156,   217,     0,     0,     0,
       0,     0,     0,   665,     0,   157,   666,   148,   149,   150,
       0,  1299,  1300,  1301,   158,  1302,  1303,  1304,  1305,     0,
    1306,  1307,   194,     0,  1308,  1309,  1310,  1311,     0,   153,
     154,   155,     0,  1312,     0,     0,     0,     0,    68,    69,
       0,    70,   151,   152,     0,     0,   127,     0,     0,   128,
       0,     0,     0,   129,   130,   131,   132,   133,  1613,   134,
     135,   136,   137,     0,   138,   139,     0,     0,   140,   141,
     142,   143,     0,     0,    99,   144,   145,    28,    29,    30,
      31,    32,    33,    34,   146,     0,   147,  1246,     0,     0,
       0,     0,     0,    35,     0,     0,     0,     0,   153,   154,
     155,   148,   149,   150,     0,     0,     0,    68,    69,     0,
      70,     0,     0,     0,     0,   127,     0,     0,   128,     0,
       0,     0,   129,   130,   131,   132,   133,     0,   134,   135,
     136,   137,     0,   138,   139,     0,   151,   140,   141,   142,
     143,     0,     0,    99,   144,   145,     0,     2,     0,     0,
       0,     0,     0,   146,     0,   147,     0,     0,   156,     0,
       0,     0,     0,     0,     0,     0,     3,     0,   211,     0,
     148,   149,   150,     0,     0,     0,     0,   158,     0,     0,
      68,    69,     0,    70,     0,     0,     0,     0,   127,     0,
       0,   128,   153,   154,   155,   129,   130,   131,   132,   133,
       0,   134,   135,   136,   137,   584,   138,   139,     0,     0,
     140,   141,   142,   143,     0,     0,    99,   144,   145,     0,
       0,     0,     0,     0,     0,     0,   693,   156,   147,     0,
       0,     0,     0,     0,     0,     0,     0,   211,     0,     0,
       0,     0,     0,   148,   149,   150,   158,     0,     0,    68,
      69,     0,    70,     0,     0,     0,     0,   127,     0,     0,
     128,   153,   154,   155,   129,   130,   131,   132,   133,     0,
     134,   135,   136,   137,     0,   138,   139,     0,   151,   140,
     141,   142,   143,     0,     0,    99,   144,   145,     0,     0,
       0,     0,     0,     0,     0,   146,     0,   147,     0,     0,
       0,     0,     4,     5,     6,     7,     8,     0,     0,     0,
       0,     0,   148,   149,   150,     0,     0,     0,     0,     0,
       0,   156,     0,     0,     9,    10,     0,     0,     0,     0,
       0,   211,     0,     0,   153,   154,   155,     0,     0,     0,
     158,    11,    12,    13,    14,     0,     0,   766,    15,    16,
      68,     0,     0,    70,    17,     0,     0,    18,     0,     0,
     128,     0,     0,     3,    19,    20,   131,   132,   133,     0,
     134,   135,   136,   137,     0,   138,   139,     0,     0,   140,
     141,   142,   143,     0,     0,     0,  1364,   145,     0,     0,
     156,     0,     0,     0,     0,     0,     0,     0,     0,     0,
     211,     0,     0,   153,   154,   155,     0,     0,     0,   158,
       0,     0,     0,     0,     0,     0,     0,     0,     0,    21,
      22,     0,    23,    24,    25,     0,    26,    27,    28,    29,
      30,    31,    32,    33,    34,  1365,     0,     0,     0,     0,
       0,     0,     0,     0,    35,     0,     0,     0,    68,     0,
       0,    70,     0,     0,  1366,     0,     0,     0,     0,     0,
       0,     3,     0,   156,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   211,     0,     0,     0,     0,     0,     0,
       0,     0,   158,     0,     0,     0,   360,   361,   362,   363,
     364,   365,   366,   367,   368,   369,   370,   371,   372,     0,
       0,     0,     0,     8,     0,     0,     0,   373,   374,   375,
     376,   377,   378,     0,     0,     0,     0,  1006,     0,     0,
       0,     9,    10,     0,  1007,     0,  1008,  1009,  1010,     0,
       0,    68,   156,     0,    70,     0,     0,     0,    11,    12,
      13,    14,   211,     0,     3,     0,     0,     0,     0,   379,
       0,   158,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   380,     0,  1011,  1012,  1013,    78,    79,
      80,    81,    82,    83,    84,    85,    86,    87,    88,    89,
      90,    91,    92,     0,   360,   361,   362,   363,   364,   365,
     366,   367,   368,   369,   370,   371,   372,     0,   381,   382,
       0,     8,     0,     0,     0,   373,   374,   375,   376,   377,
     378,  1014,  1015,  1016,     0,  1017,     0,     0,  1018,     9,
      10,     0,     0,     0,     0,    28,    29,    30,    31,    32,
      33,    34,     0,     0,   383,   384,    11,    12,    13,    14,
       0,    35,     0,     0,   269,   270,   271,   379,   272,   273,
     274,   275,     0,   276,   277,     0,     0,   278,   279,   280,
     281,   380,     0,     0,     0,     0,     0,   360,   361,   362,
     363,   364,   365,   366,   367,   368,   369,   370,   371,   372,
       0,     0,     0,     0,     8,     0,     0,     0,   373,   374,
     375,   376,   377,   378,     0,     0,   381,   382,     0,     0,
       0,     0,     9,    10,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,    11,
      12,    13,    14,    28,    29,    30,    31,    32,    33,    34,
     379,     0,   383,   793,     0,     0,     0,     0,     0,    35,
       0,   128,     0,     0,   380,   129,   130,   131,   132,   133,
       0,   134,   135,   136,   137,     0,   138,   139,     0,     0,
     140,   141,   142,   143,   449,     0,     0,   144,   145,     0,
       0,     0,     0,  1019,     0,     0,   146,     0,   147,   381,
     382,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   148,   149,   150,     0,   450,     0,   451,
     452,   453,   454,     0,     0,     0,    28,    29,    30,    31,
      32,    33,    34,     0,     0,   383,  1001,     0,     0,     0,
       0,     0,    35,     0,     0,     0,     0,     0,   151,     0,
       0,     0,     0,     0,     0,     0,   455,   456,   457,   458,
       0,     0,   459,     0,     0,   128,   460,   461,   462,   129,
     130,   131,   132,   133,     0,   134,   135,   136,   137,     0,
     138,   139,     0,     0,   140,   141,   142,   143,     0,     0,
       0,   144,   145,     0,     0,     0,     0,     0,     0,     0,
     146,     0,   147,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,   148,   149,   150,
     128,     0,     0,     0,   129,   130,   131,   132,   133,     0,
     134,   135,   136,   137,     0,   138,   139,     0,     0,   140,
     141,   142,   143,     0,     0,     0,   144,   145,     0,     0,
       0,     0,   151,     0,     0,   146,     0,   147,     0,     0,
       0,     0,     0,     0,     0,     0,     3,     0,     0,     0,
     463,   128,   148,   149,   150,   129,   130,   131,   132,   133,
       0,   134,   135,   136,   137,     0,   138,   139,     0,     0,
     140,   141,   142,   143,     0,     0,     0,   144,   145,     0,
       0,     0,     0,     0,     0,     0,   146,   151,   147,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,   148,   149,   150,     0,     0,     0,     0,
       0,     0,     0,   156,     0,  1006,     0,   852,     0,     0,
       0,     0,  1007,     0,  1008,  1009,  1010,     0,     0,     0,
       0,   853,   158,     0,     0,     0,   854,   855,   151,   856,
     857,   858,   859,   860,   861,     0,   862,   863,     0,   864,
     865,   866,   867,   868,     0,     0,     0,     0,     0,     0,
       0,     0,     0,  1011,  1012,  1013,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     4,     5,     6,     7,     8,     0,     0,     0,
       0,     0,     0,     0,     0,   869,     0,   870,     0,     0,
       0,     0,   871,     0,     9,    10,     0,     0,     0,  1014,
    1015,  1016,     0,  1017,     0,     0,  1018,   156,   872,     0,
       0,    11,    12,    13,    14,     0,     0,   776,    15,    16,
       3,     0,     0,     0,    17,     0,   158,    18,     0,     0,
       0,     0,     0,     0,    19,    20,     0,     0,   911,   912,
     913,   873,   914,   915,   916,   917,     0,   918,   919,   194,
       0,   920,   921,   922,   923,     0,     0,     0,   924,   925,
       0,     0,   156,     0,     0,     0,     3,     0,  1006,   551,
       0,     0,   932,     0,     0,  1007,     0,  1008,  1009,  1010,
       0,   158,     0,     0,     0,     0,     0,     0,     0,    21,
      22,     0,    23,    24,    25,     0,    26,    27,    28,    29,
      30,    31,    32,    33,    34,     0,     0,     0,   539,     0,
       0,     0,     0,   156,    35,   551,  1011,  1012,  1013,     0,
       0,     0,     0,     0,     0,     0,   926,     0,     0,     0,
       0,   874,   158,   875,   876,   877,   878,   879,   880,   881,
     882,   883,   884,   885,   886,   887,   888,   889,   890,   891,
       0,     0,     0,   892,     0,     0,   552,     0,     6,     7,
       8,  1248,  1014,  1015,  1016,     0,  1017,     0,     0,  1018,
     553,     0,     0,     0,     0,   554,     0,     0,     9,    10,
       0,     0,     0,     0,     0,     0,     0,     0,     0,     0,
       0,     0,     0,     0,   893,    11,    12,    13,    14,     0,
     555,   556,   552,     0,     6,     7,     8,     0,     0,     0,
       0,     0,     0,     0,     0,     0,   553,     0,     0,     0,
     557,   554,     0,     0,     9,    10,     0,     0,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,   852,     0,
       0,    11,    12,    13,    14,     0,   555,   556,     0,     0,
       0,     0,   853,     0,     0,   558,   559,   854,   855,     0,
     856,   857,   858,   859,   860,   861,   557,   862,   863,     0,
     864,   865,   866,   867,   868,     0,     0,     0,    68,    69,
       0,    70,    28,    29,    30,    31,    32,    33,    34,     0,
       0,     0,   560,     0,     0,     0,     0,     0,    35,     0,
       0,   558,   559,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   313,     0,     0,     0,   869,     0,   870,     0,
       0,     0,     0,   871,  1492,     0,     0,     0,    28,    29,
      30,    31,    32,    33,    34,     0,     0,     0,  1226,   872,
       0,   314,     0,     0,    35,   315,     0,     0,   316,   317,
       0,     0,     0,   318,   319,   320,   321,   322,   323,   324,
     325,   326,   327,   328,   329,     0,     0,   339,    98,     0,
       0,   330,   873,   100,   331,   101,     0,     0,     0,     0,
       0,   332,   102,     0,     0,     0,     0,     0,     0,     0,
     333,     0,     0,     0,     0,     0,     0,     0,     0,   103,
     340,     0,   341,   342,   343,   344,   345,     0,     0,     0,
       0,   346,     0,     0,   104,     0,     0,     0,     0,     0,
     347,     0,     0,     0,     0,   348,     0,   349,     0,     0,
       0,     0,     0,     0,     0,     0,     0,     0,     0,   350,
     351,   352,   353,   354,   355,   356,   357,     0,     0,     0,
       0,     0,   358,     0,     0,     0,     0,     0,     0,     0,
       0,     0,   874,     0,   875,   876,   877,   878,   879,   880,
     881,   882,   883,   884,   885,   886,   887,   888,   889,   890,
     891,     0,     0,     0,   892
};

static const yytype_int16 yycheck[] =
{
       5,   164,    25,   707,   103,   104,   424,    14,   426,     1,
      25,    25,   338,   338,     1,    20,     1,     1,    25,   900,
     338,   186,  1031,    28,    29,    30,    31,  1062,    57,     1,
     692,    43,    21,    22,     1,   774,    48,  1183,   486,    25,
      10,   787,     6,     7,    10,     9,    10,    59,     8,     8,
     216,    94,   189,   190,   191,   192,   193,    10,     9,     9,
       6,     7,    20,     9,    64,   164,    23,    24,    21,     6,
       7,    60,     9,    93,    34,  1216,     8,     9,   515,   516,
     517,     9,   786,     8,    73,   108,   185,     8,    64,     8,
     508,   509,    21,    10,     6,     7,    42,     9,   105,    10,
     111,     6,     7,   299,     9,    10,   129,   111,  1150,   114,
      13,   116,   117,   118,   213,    57,    21,    45,    10,   105,
     137,    10,    11,   112,   113,    10,    11,   170,    77,   152,
       6,     7,   146,     9,   298,   149,     9,    42,     6,     7,
     167,     9,     6,     7,    10,     9,  1543,    10,     6,     7,
     157,     9,   157,   158,    10,   169,    14,   164,     6,     7,
     392,     9,   182,    65,    10,    11,   195,     6,     7,   158,
       9,   228,    10,   292,    42,   299,   181,     6,     7,     6,
       9,   300,     9,    10,    42,    14,   209,   199,   934,   935,
     233,   248,    10,  1531,   106,   107,   108,    10,    10,   137,
      10,   206,   207,   198,   211,    10,   211,    12,   274,     6,
       7,  1523,     9,    42,   234,    10,    45,  1529,     6,     7,
     966,     9,  1530,    52,   126,  1563,    21,    32,    33,   234,
     395,    10,     9,   222,   223,   240,    12,   186,  1379,   294,
     403,  1528,   137,    10,    11,    12,   301,   261,   268,  1561,
     687,    66,    67,   216,  1562,    10,    32,    33,    10,    11,
      12,     6,     7,   268,     9,    32,    33,   294,    45,    14,
    1667,   294,   218,  1560,  1155,   292,  1318,   288,   283,   302,
      32,    33,   287,   292,   288,   290,   291,   292,   299,   298,
     295,   296,   306,   997,   294,   300,   733,    42,   287,   293,
     295,  1047,   160,   161,   162,   216,   300,    52,   407,    25,
      26,    40,   301,   218,     6,     7,   336,     9,   294,    35,
     312,  1025,   290,    52,    53,   312,   292,   312,   312,    10,
      11,   160,   161,   162,   300,   305,   294,   569,   298,   298,
     312,    70,   293,   293,   308,   312,   338,   300,   290,   307,
     218,   338,     6,   338,   338,     9,   294,     8,   295,   305,
     208,   293,   382,   406,   290,   290,   338,   296,   296,   290,
     295,   338,   304,   295,   295,   294,   298,   406,  1413,   296,
      10,   404,    12,    61,   373,   405,   375,   299,   291,   301,
     379,   120,  1538,   305,   306,   300,   403,   404,   410,   294,
     305,   408,    32,    33,   296,    13,   395,   296,   845,   398,
     399,   296,     8,   402,   294,   160,   161,   162,   290,   294,
     300,   290,   408,   299,   589,   300,   415,   416,  1574,   418,
     296,   420,   597,   296,   294,   299,   290,   305,    34,   290,
     296,   299,   295,   400,   401,   468,   395,   436,   437,   398,
     296,   438,   472,   402,   438,   439,   440,   441,   296,   464,
     299,   466,   899,   290,   290,   422,   438,   439,   440,   441,
     299,   478,   251,   252,   253,   295,   295,   484,   296,   484,
     299,   296,   431,   296,   296,   584,   296,   294,   493,   937,
    1152,   296,    88,   300,   293,   484,   298,     6,     7,     8,
       9,    10,   299,   523,   290,    14,   526,   527,  1247,   295,
     499,   299,    66,    67,  1549,   504,   505,   506,   523,   508,
      29,   510,   511,   512,   216,   294,    52,     6,     7,   296,
       9,   300,   575,    42,   598,   599,   600,     6,     7,   559,
       9,     6,     7,   295,     9,    14,   295,    19,   137,   299,
     507,   540,   509,   576,   299,   305,   513,    66,    67,   137,
     289,     6,     7,   292,     9,    41,   555,   556,   137,  1578,
      46,   591,    48,    42,   303,   137,   581,   582,   295,    55,
       6,     7,   299,     9,   307,   574,   629,   296,    14,     6,
       7,   580,     9,    10,   583,    74,    72,    76,    77,    13,
     620,   295,   625,   646,    83,   299,   771,   296,   597,   638,
       6,    87,   624,     9,    10,   620,    42,    19,   305,    45,
     292,   293,    29,    30,   296,   295,    52,  1373,  1374,   299,
     296,    76,    77,   106,   107,   108,   293,   294,   627,   295,
     589,   653,   654,   299,   293,   294,    40,   659,   597,   661,
     295,   160,   161,   162,   299,   120,   121,    64,    52,    53,
     665,   760,     6,     7,   137,     9,   671,   766,   295,   106,
     107,   108,   998,   998,   295,   664,    70,  1558,   627,   693,
     998,   704,   819,    62,   821,   822,   823,   824,   825,   293,
     294,   160,   161,   162,   683,   139,   140,   709,   170,   171,
     172,   173,   707,  1407,   292,   710,    24,   712,   296,    27,
      28,    29,    30,   295,    32,    33,    34,   299,   298,   703,
     296,   744,   194,   195,   196,   197,   120,   295,   717,   718,
     719,   299,   292,   293,   160,   161,   162,   757,   758,   759,
     295,    47,  1488,    49,    50,    51,   106,   107,   108,  1495,
     755,   740,   741,  1171,   759,  1173,   300,   271,   763,   764,
     765,   292,   785,   720,   296,    10,   789,    24,   811,     9,
      27,    28,    29,    30,   295,    32,    33,    34,   299,   293,
     300,   786,    27,    28,    29,   299,  1490,   776,   221,   778,
     299,   780,   306,   782,   295,   197,   203,   204,   299,   206,
      26,    27,    28,    29,    30,   207,    26,   796,   210,   281,
     282,   283,   284,   285,   286,   287,   300,   806,   807,   291,
     295,   295,   771,    19,   299,   297,   299,   295,   301,   295,
     299,   299,   305,   306,    15,   295,  1582,    18,   295,   828,
     295,   830,   831,   848,   299,   850,    56,   796,    58,    59,
      60,   808,   139,   140,   295,   844,   805,   900,   295,   295,
      41,    42,   299,   299,   301,    46,   909,    48,   305,   306,
     296,   295,   295,   299,    55,   299,   299,  1623,   300,   281,
     282,   283,   284,   285,   286,   287,   900,   907,   291,   291,
     295,    72,    10,    11,    12,   297,   294,   295,   292,    76,
      77,    78,   295,   295,   292,   293,    87,   299,  1654,   303,
     295,   900,   295,   295,    32,    33,   299,   299,   930,    41,
    1624,   295,   295,  1669,    46,   299,    48,     6,     7,   295,
       9,  1048,  1049,    55,   295,   112,   113,   114,   299,   299,
     295,   301,   295,   932,   928,   305,   306,   295,   897,   295,
      72,    73,   290,   976,   295,   292,   294,   299,   978,   296,
     295,  1004,  1666,   295,   299,    87,   295,   990,   983,   295,
     299,   304,   290,   996,   295,   980,   295,   173,   299,   175,
     176,   295,   987,   294,   973,   299,   295,   298,   306,   292,
     291,   940,   997,   296,   887,   888,   118,   293,   194,   195,
     196,   197,   296,   992,   295,   994,   998,   999,  1000,   295,
    1030,   998,  1032,   998,   998,   999,  1000,     8,  1002,   290,
    1025,     8,   101,   102,   103,   293,   998,   999,  1000,   296,
    1002,   998,   999,  1000,   106,   107,   108,   296,   301,  1044,
      20,   209,    20,  1086,   295,   295,   295,   126,   295,   306,
      26,    27,    28,    29,    30,    21,    76,    77,    78,  1048,
    1049,   302,   293,   293,  1227,  1228,  1229,  1230,    27,    28,
      29,    30,   111,   290,  1237,  1238,  1239,   290,   106,   107,
     108,   290,   290,   304,  1073,   281,   282,   283,   284,   285,
     286,   287,   112,   113,   114,   291,  1139,    20,   295,  1048,
    1049,   297,    63,    63,    16,    17,    18,   295,   295,    21,
      22,    23,    24,    25,   295,    27,    28,    29,    30,   295,
      32,    33,   296,   296,    36,    37,    38,    39,   300,   296,
      42,    43,    44,    27,    28,    29,    30,   296,   296,   296,
      52,   220,    54,   222,   223,   224,   225,  1136,   296,   296,
    1568,  1569,   304,   213,   226,   298,   298,    69,    70,    71,
      27,    28,    29,    30,   298,   295,   216,  1156,  1157,  1158,
     216,   296,   293,  1187,  1163,  1164,  1165,  1166,  1167,  1168,
     291,  1170,  1171,  1172,   298,  1174,  1175,  1176,  1177,  1178,
    1179,  1180,   104,  1182,   293,  1184,     9,  1186,   298,  1188,
     291,   295,   295,  1223,  1224,  1210,    27,    28,    29,    30,
    1217,   296,  1169,    40,   296,  1220,  1173,    40,   296,  1208,
    1227,  1228,  1229,  1230,  1181,   296,   296,   299,    55,   301,
    1237,  1238,  1239,   305,   306,   296,   302,   296,   296,   296,
     296,    23,    24,    25,    71,    27,    28,    29,    30,  1292,
      32,    33,    34,    80,    36,    37,    38,    39,   296,   296,
    1249,    43,    44,    19,   296,   296,   295,    94,    95,    96,
      97,   299,   296,   301,   296,   296,   295,   305,   306,  1268,
      20,   296,    19,   300,   300,   296,   293,    20,  1277,   293,
    1389,   296,   293,   296,   304,  1284,  1285,   299,   295,   126,
     127,   128,   129,   130,   131,   132,   295,  1296,   296,  1258,
     296,   295,   295,   243,   296,   296,  1265,   296,  1267,    15,
     295,   235,    18,   247,   295,    23,   296,  1316,  1317,   111,
     298,   298,   296,   291,   290,  1319,  1325,  1326,  1327,  1328,
    1329,  1330,   296,  1332,   296,    41,    42,   302,    85,   296,
      46,  1365,    48,   296,   295,   197,   295,   295,   304,    55,
     304,  1381,  1367,   300,   300,   300,   300,   137,   300,   300,
     296,    21,  1415,    63,  1331,    63,    72,   289,   296,   296,
     296,     9,     6,     7,   250,     9,  1406,   295,   299,   299,
      14,    87,   295,    17,   290,   296,   308,    21,    22,    23,
      24,    25,  1407,    27,    28,    29,    30,   300,    32,    33,
     296,   296,    36,    37,    38,    39,   295,   295,    42,    43,
      44,   177,   178,   167,   296,  1414,   296,   296,    52,   293,
      54,   256,  1421,   301,   293,   300,    20,   300,   296,   300,
     296,   197,   304,   298,   295,    69,    70,    71,  1432,   295,
     295,   295,   295,   209,   295,   211,   295,   295,   214,   215,
     197,   295,   295,   290,   295,   295,     9,   291,  1491,  1489,
     296,   300,   209,   293,   211,   212,   105,   214,   215,   296,
     104,   110,   106,   107,   108,  1490,   300,   296,   300,   300,
     296,   296,   300,   300,   296,   296,   120,   121,    19,   281,
     282,   283,   284,   285,   286,   287,   300,   296,  1551,   296,
     300,   296,   300,   296,   300,   297,   296,   296,  1507,  1508,
    1509,    42,   300,   300,  1544,   281,   282,   283,   284,   285,
     286,   287,   296,   300,   296,   291,   160,   161,   162,   296,
     300,   297,   296,   296,   281,   282,   283,   284,   285,   286,
     287,   300,   302,   296,   291,    20,     9,   296,   296,   295,
     297,  1660,  1576,   304,   295,   300,   300,   295,   300,   295,
    1559,   295,     9,   296,   304,  1564,  1565,  1566,   296,  1568,
     295,  1570,  1571,  1572,  1627,   304,   295,   300,   296,   296,
     300,   296,   296,   295,   300,   295,   295,   300,   295,     6,
       7,   105,     9,  1592,  1593,  1594,   110,    14,   295,   295,
      17,   295,  1569,   295,    21,    22,    23,    24,    25,  1624,
      27,    28,    29,    30,   295,    32,    33,   295,   295,    36,
      37,    38,    39,   295,   155,    42,    43,    44,   296,   295,
    1663,   300,   296,   296,  1664,    52,   301,    54,   296,   300,
     296,   295,   281,   282,   283,   284,   285,   286,   287,   304,
     295,  1666,    69,    70,    71,   289,  1655,   299,   297,   295,
     295,    20,     9,   296,   295,   299,   197,   301,   296,   296,
     296,   305,   306,   295,   308,   296,   207,   296,   295,   210,
     295,   105,   256,   104,   296,   296,   110,   104,    20,   106,
     107,   108,   295,   180,     6,     7,   296,     9,   306,    25,
     478,  1257,    14,   120,   121,    17,   408,   768,   754,    21,
      22,    23,    24,    25,  1214,    27,    28,    29,    30,   975,
      32,    33,    25,  1035,    36,    37,    38,    39,  1256,   359,
      42,    43,    44,   595,    57,   895,   638,   937,  1139,  1037,
      52,   704,    54,   160,   161,   162,  1503,  1192,   907,    10,
     281,   282,   283,   284,   285,   286,   287,    69,    70,    71,
     291,   902,   741,  1201,   490,   115,   297,   281,   282,   283,
     284,   285,   286,   287,   557,    -1,    -1,   291,     3,    -1,
     439,    -1,    -1,   297,    -1,   772,    -1,    -1,    -1,    -1,
      -1,    -1,   104,    -1,   106,   107,   108,    22,    23,    -1,
      25,    26,    27,    28,    29,    30,    -1,    32,    33,    -1,
      35,    36,    37,    38,    39,    -1,    16,    17,    18,    -1,
      -1,    21,    22,    23,    24,    25,    -1,    27,    28,    29,
      30,    -1,    32,    33,    -1,    -1,    36,    37,    38,    39,
      -1,    -1,    -1,    43,    44,    -1,    -1,    -1,   160,   161,
     162,    -1,    52,    -1,    54,    -1,    -1,   281,   282,   283,
     284,   285,   286,   287,    -1,    -1,    -1,   291,    -1,    69,
      70,    71,   289,   297,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   299,    -1,   301,    -1,    -1,    -1,   305,   306,
      -1,   308,     6,     7,    -1,     9,    -1,    -1,    -1,    -1,
      14,    -1,    -1,    17,   104,    -1,    -1,    21,    22,    23,
      24,    25,    -1,    27,    28,    29,    30,    -1,    32,    33,
      -1,    -1,    36,    37,    38,    39,    -1,    -1,    42,    43,
      44,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    52,    -1,
      54,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    69,    70,    71,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    23,    24,    25,    -1,    27,
      28,    29,    30,    -1,    32,    33,    34,   289,    36,    37,
      38,    39,    -1,    -1,    -1,    43,    44,   299,    -1,   301,
     104,   105,    -1,   305,   306,    19,   308,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,   230,   231,    -1,   233,   234,
     235,    -1,   237,   238,    -1,    -1,   241,   242,    42,   244,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   257,   258,   259,   260,   261,   262,   263,   264,
     265,   266,   267,   268,   269,   270,   160,   161,   162,    -1,
      -1,    -1,    -1,   111,    -1,     6,     7,    -1,     9,    -1,
      -1,    -1,    -1,    14,    -1,    -1,    17,    -1,    -1,    -1,
      21,    22,    23,    24,    25,    -1,    27,    28,    29,    30,
     305,    32,    33,   197,    -1,    36,    37,    38,    39,   289,
      -1,    42,    43,    44,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    52,    -1,    54,    -1,    -1,    -1,    -1,   308,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    69,    70,
      71,    -1,    73,    -1,     6,     7,    -1,     9,    -1,    -1,
      -1,   155,    14,    -1,    -1,    17,    -1,    -1,    -1,    21,
      22,    23,    24,    25,    -1,    27,    28,    29,    30,    -1,
      32,    33,    -1,   104,    36,    37,    38,    39,    -1,    -1,
      42,    43,    44,    -1,    -1,    -1,    -1,   118,    -1,    -1,
      52,    -1,    54,   197,    -1,   289,     8,    -1,    -1,    -1,
      -1,    -1,    -1,   207,    -1,   299,   210,    69,    70,    71,
      -1,    23,    24,    25,   308,    27,    28,    29,    30,    -1,
      32,    33,    34,    -1,    36,    37,    38,    39,    -1,   160,
     161,   162,    -1,    45,    -1,    -1,    -1,    -1,     6,     7,
      -1,     9,   104,   105,    -1,    -1,    14,    -1,    -1,    17,
      -1,    -1,    -1,    21,    22,    23,    24,    25,   296,    27,
      28,    29,    30,    -1,    32,    33,    -1,    -1,    36,    37,
      38,    39,    -1,    -1,    42,    43,    44,   281,   282,   283,
     284,   285,   286,   287,    52,    -1,    54,   291,    -1,    -1,
      -1,    -1,    -1,   297,    -1,    -1,    -1,    -1,   160,   161,
     162,    69,    70,    71,    -1,    -1,    -1,     6,     7,    -1,
       9,    -1,    -1,    -1,    -1,    14,    -1,    -1,    17,    -1,
      -1,    -1,    21,    22,    23,    24,    25,    -1,    27,    28,
      29,    30,    -1,    32,    33,    -1,   104,    36,    37,    38,
      39,    -1,    -1,    42,    43,    44,    -1,     0,    -1,    -1,
      -1,    -1,    -1,    52,    -1,    54,    -1,    -1,   289,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    19,    -1,   299,    -1,
      69,    70,    71,    -1,    -1,    -1,    -1,   308,    -1,    -1,
       6,     7,    -1,     9,    -1,    -1,    -1,    -1,    14,    -1,
      -1,    17,   160,   161,   162,    21,    22,    23,    24,    25,
      -1,    27,    28,    29,    30,   104,    32,    33,    -1,    -1,
      36,    37,    38,    39,    -1,    -1,    42,    43,    44,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    52,   289,    54,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,   299,    -1,    -1,
      -1,    -1,    -1,    69,    70,    71,   308,    -1,    -1,     6,
       7,    -1,     9,    -1,    -1,    -1,    -1,    14,    -1,    -1,
      17,   160,   161,   162,    21,    22,    23,    24,    25,    -1,
      27,    28,    29,    30,    -1,    32,    33,    -1,   104,    36,
      37,    38,    39,    -1,    -1,    42,    43,    44,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    52,    -1,    54,    -1,    -1,
      -1,    -1,   155,   156,   157,   158,   159,    -1,    -1,    -1,
      -1,    -1,    69,    70,    71,    -1,    -1,    -1,    -1,    -1,
      -1,   289,    -1,    -1,   177,   178,    -1,    -1,    -1,    -1,
      -1,   299,    -1,    -1,   160,   161,   162,    -1,    -1,    -1,
     308,   194,   195,   196,   197,    -1,    -1,   104,   201,   202,
       6,    -1,    -1,     9,   207,    -1,    -1,   210,    -1,    -1,
      17,    -1,    -1,    19,   217,   218,    23,    24,    25,    -1,
      27,    28,    29,    30,    -1,    32,    33,    -1,    -1,    36,
      37,    38,    39,    -1,    -1,    -1,    43,    44,    -1,    -1,
     289,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     299,    -1,    -1,   160,   161,   162,    -1,    -1,    -1,   308,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   272,
     273,    -1,   275,   276,   277,    -1,   279,   280,   281,   282,
     283,   284,   285,   286,   287,    92,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   297,    -1,    -1,    -1,     6,    -1,
      -1,     9,    -1,    -1,   111,    -1,    -1,    -1,    -1,    -1,
      -1,    19,    -1,   289,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   299,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   308,    -1,    -1,    -1,   142,   143,   144,   145,
     146,   147,   148,   149,   150,   151,   152,   153,   154,    -1,
      -1,    -1,    -1,   159,    -1,    -1,    -1,   163,   164,   165,
     166,   167,   168,    -1,    -1,    -1,    -1,    40,    -1,    -1,
      -1,   177,   178,    -1,    47,    -1,    49,    50,    51,    -1,
      -1,     6,   289,    -1,     9,    -1,    -1,    -1,   194,   195,
     196,   197,   299,    -1,    19,    -1,    -1,    -1,    -1,   205,
      -1,   308,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   219,    -1,    88,    89,    90,   179,   180,
     181,   182,   183,   184,   185,   186,   187,   188,   189,   190,
     191,   192,   193,    -1,   142,   143,   144,   145,   146,   147,
     148,   149,   150,   151,   152,   153,   154,    -1,   254,   255,
      -1,   159,    -1,    -1,    -1,   163,   164,   165,   166,   167,
     168,   134,   135,   136,    -1,   138,    -1,    -1,   141,   177,
     178,    -1,    -1,    -1,    -1,   281,   282,   283,   284,   285,
     286,   287,    -1,    -1,   290,   291,   194,   195,   196,   197,
      -1,   297,    -1,    -1,    23,    24,    25,   205,    27,    28,
      29,    30,    -1,    32,    33,    -1,    -1,    36,    37,    38,
      39,   219,    -1,    -1,    -1,    -1,    -1,   142,   143,   144,
     145,   146,   147,   148,   149,   150,   151,   152,   153,   154,
      -1,    -1,    -1,    -1,   159,    -1,    -1,    -1,   163,   164,
     165,   166,   167,   168,    -1,    -1,   254,   255,    -1,    -1,
      -1,    -1,   177,   178,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   194,
     195,   196,   197,   281,   282,   283,   284,   285,   286,   287,
     205,    -1,   290,   291,    -1,    -1,    -1,    -1,    -1,   297,
      -1,    17,    -1,    -1,   219,    21,    22,    23,    24,    25,
      -1,    27,    28,    29,    30,    -1,    32,    33,    -1,    -1,
      36,    37,    38,    39,    40,    -1,    -1,    43,    44,    -1,
      -1,    -1,    -1,   296,    -1,    -1,    52,    -1,    54,   254,
     255,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    69,    70,    71,    -1,    73,    -1,    75,
      76,    77,    78,    -1,    -1,    -1,   281,   282,   283,   284,
     285,   286,   287,    -1,    -1,   290,   291,    -1,    -1,    -1,
      -1,    -1,   297,    -1,    -1,    -1,    -1,    -1,   104,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   112,   113,   114,   115,
      -1,    -1,   118,    -1,    -1,    17,   122,   123,   124,    21,
      22,    23,    24,    25,    -1,    27,    28,    29,    30,    -1,
      32,    33,    -1,    -1,    36,    37,    38,    39,    -1,    -1,
      -1,    43,    44,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      52,    -1,    54,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    69,    70,    71,
      17,    -1,    -1,    -1,    21,    22,    23,    24,    25,    -1,
      27,    28,    29,    30,    -1,    32,    33,    -1,    -1,    36,
      37,    38,    39,    -1,    -1,    -1,    43,    44,    -1,    -1,
      -1,    -1,   104,    -1,    -1,    52,    -1,    54,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    19,    -1,    -1,    -1,
     226,    17,    69,    70,    71,    21,    22,    23,    24,    25,
      -1,    27,    28,    29,    30,    -1,    32,    33,    -1,    -1,
      36,    37,    38,    39,    -1,    -1,    -1,    43,    44,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    52,   104,    54,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    69,    70,    71,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,   289,    -1,    40,    -1,     3,    -1,    -1,
      -1,    -1,    47,    -1,    49,    50,    51,    -1,    -1,    -1,
      -1,    17,   308,    -1,    -1,    -1,    22,    23,   104,    25,
      26,    27,    28,    29,    30,    -1,    32,    33,    -1,    35,
      36,    37,    38,    39,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    88,    89,    90,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   155,   156,   157,   158,   159,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    81,    -1,    83,    -1,    -1,
      -1,    -1,    88,    -1,   177,   178,    -1,    -1,    -1,   134,
     135,   136,    -1,   138,    -1,    -1,   141,   289,   104,    -1,
      -1,   194,   195,   196,   197,    -1,    -1,   299,   201,   202,
      19,    -1,    -1,    -1,   207,    -1,   308,   210,    -1,    -1,
      -1,    -1,    -1,    -1,   217,   218,    -1,    -1,    23,    24,
      25,   137,    27,    28,    29,    30,    -1,    32,    33,    34,
      -1,    36,    37,    38,    39,    -1,    -1,    -1,    43,    44,
      -1,    -1,   289,    -1,    -1,    -1,    19,    -1,    40,    68,
      -1,    -1,   299,    -1,    -1,    47,    -1,    49,    50,    51,
      -1,   308,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   272,
     273,    -1,   275,   276,   277,    -1,   279,   280,   281,   282,
     283,   284,   285,   286,   287,    -1,    -1,    -1,   291,    -1,
      -1,    -1,    -1,   289,   297,    68,    88,    89,    90,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   111,    -1,    -1,    -1,
      -1,   227,   308,   229,   230,   231,   232,   233,   234,   235,
     236,   237,   238,   239,   240,   241,   242,   243,   244,   245,
      -1,    -1,    -1,   249,    -1,    -1,   155,    -1,   157,   158,
     159,   296,   134,   135,   136,    -1,   138,    -1,    -1,   141,
     169,    -1,    -1,    -1,    -1,   174,    -1,    -1,   177,   178,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,   290,   194,   195,   196,   197,    -1,
     199,   200,   155,    -1,   157,   158,   159,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,   169,    -1,    -1,    -1,
     219,   174,    -1,    -1,   177,   178,    -1,    -1,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,     3,    -1,
      -1,   194,   195,   196,   197,    -1,   199,   200,    -1,    -1,
      -1,    -1,    17,    -1,    -1,   254,   255,    22,    23,    -1,
      25,    26,    27,    28,    29,    30,   219,    32,    33,    -1,
      35,    36,    37,    38,    39,    -1,    -1,    -1,     6,     7,
      -1,     9,   281,   282,   283,   284,   285,   286,   287,    -1,
      -1,    -1,   291,    -1,    -1,    -1,    -1,    -1,   297,    -1,
      -1,   254,   255,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,    40,    -1,    -1,    -1,    81,    -1,    83,    -1,
      -1,    -1,    -1,    88,   296,    -1,    -1,    -1,   281,   282,
     283,   284,   285,   286,   287,    -1,    -1,    -1,   291,   104,
      -1,    69,    -1,    -1,   297,    73,    -1,    -1,    76,    77,
      -1,    -1,    -1,    81,    82,    83,    84,    85,    86,    87,
      88,    89,    90,    91,    92,    -1,    -1,    40,    41,    -1,
      -1,    99,   137,    46,   102,    48,    -1,    -1,    -1,    -1,
      -1,   109,    55,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
     118,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    72,
      73,    -1,    75,    76,    77,    78,    79,    -1,    -1,    -1,
      -1,    84,    -1,    -1,    87,    -1,    -1,    -1,    -1,    -1,
      93,    -1,    -1,    -1,    -1,    98,    -1,   100,    -1,    -1,
      -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,    -1,   112,
     113,   114,   115,   116,   117,   118,   119,    -1,    -1,    -1,
      -1,    -1,   125,    -1,    -1,    -1,    -1,    -1,    -1,    -1,
      -1,    -1,   227,    -1,   229,   230,   231,   232,   233,   234,
     235,   236,   237,   238,   239,   240,   241,   242,   243,   244,
     245,    -1,    -1,    -1,   249
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
     340,   341,   342,   361,   379,   383,   405,   406,   462,   465,
     471,   472,   473,   477,   486,   489,   494,   216,     6,     7,
       9,   315,   316,   299,   365,    65,   126,   407,   179,   180,
     181,   182,   183,   184,   185,   186,   187,   188,   189,   190,
     191,   192,   193,   470,   470,     9,    15,    18,    41,    42,
      46,    48,    55,    72,    87,   295,   326,   366,   367,   368,
     369,   298,   299,   274,   474,   216,   478,   495,   216,   316,
      10,   317,   317,    10,    11,   318,   318,    14,    17,    21,
      22,    23,    24,    25,    27,    28,    29,    30,    32,    33,
      36,    37,    38,    39,    43,    44,    52,    54,    69,    70,
      71,   104,   105,   160,   161,   162,   289,   299,   308,   316,
     322,   323,   369,   370,   429,   452,   453,   458,   459,   290,
     316,   316,   316,   316,     8,    13,   414,   415,   414,   414,
     290,   343,    61,   344,   290,   384,   390,    24,    27,    28,
      29,    30,    32,    33,    34,   290,   306,   408,   411,   413,
     414,   317,   290,   290,   290,   290,   491,   294,   317,   362,
     315,   299,   369,   429,   452,   454,   458,     8,    34,   298,
     313,   293,   295,   295,    47,    49,    50,    51,   367,   367,
     327,   370,   454,   298,   458,   295,   317,   317,   208,   316,
     478,   101,   102,   103,   126,   220,   222,   223,   224,   225,
     316,    76,    77,   316,   316,   458,    27,    28,    29,    30,
     452,    52,   452,    25,    26,    35,    16,    18,   458,    23,
      24,    25,    27,    28,    29,    30,    32,    33,    36,    37,
      38,    39,   418,   218,   305,   316,   369,   308,   316,   317,
     137,   137,   137,   366,   367,   137,   307,   106,   107,   108,
     137,   299,   301,   305,   306,   312,   452,   313,   296,    13,
     296,   296,   310,    40,    69,    73,    76,    77,    81,    82,
      83,    84,    85,    86,    87,    88,    89,    90,    91,    92,
      99,   102,   109,   118,   316,   454,    62,   345,   346,    40,
      73,    75,    76,    77,    78,    79,    84,    93,    98,   100,
     112,   113,   114,   115,   116,   117,   118,   119,   125,   367,
     142,   143,   144,   145,   146,   147,   148,   149,   150,   151,
     152,   153,   154,   163,   164,   165,   166,   167,   168,   205,
     219,   254,   255,   290,   291,   314,   315,   321,   332,   389,
     391,   392,   393,   394,   396,   397,   405,   430,   431,   432,
     433,   434,   435,   436,   437,   438,   439,   440,   441,   442,
     443,   444,   462,   472,   305,   295,   299,   410,   295,   410,
     295,   410,   295,   410,   295,   410,   295,   410,   295,   409,
     411,   295,   414,   296,     8,     9,   293,   304,   479,   487,
     492,   496,    74,    76,    77,    83,   316,   316,   300,    40,
      73,    75,    76,    77,    78,   112,   113,   114,   115,   118,
     122,   123,   124,   226,   458,   298,   218,   316,   367,   295,
     298,   295,   290,   295,   292,     9,   317,   317,   296,   290,
     295,   313,   120,   121,   299,   316,   386,   454,   300,   167,
     475,   316,   221,   137,   452,    26,   316,   454,   295,   295,
      27,    28,    29,    30,   295,   295,   295,   295,   295,   295,
     295,   295,   295,   295,   316,   300,   300,   300,   316,   317,
     316,   316,   316,   458,   316,   316,   295,   295,   316,    21,
     300,   317,   460,   461,   447,   448,   458,   291,   312,   291,
     295,    76,    77,    78,   112,   113,   114,   301,   350,   347,
     454,    68,   155,   169,   174,   199,   200,   219,   254,   255,
     291,   314,   321,   332,   342,   360,   361,   371,   375,   383,
     405,   462,   472,   490,   295,   295,   387,   317,   317,   317,
     299,   111,   288,   299,   104,   454,   304,   198,   295,   390,
      56,    58,    59,    60,   395,   398,   399,   400,   401,   402,
     403,   315,   317,   392,   315,   317,   317,   318,    12,    32,
      33,   295,   318,   319,   315,   317,   366,    16,    18,   369,
     458,   454,    88,   313,   413,   367,   327,   295,   414,   295,
     317,   317,   317,   317,   318,   319,   319,   291,   293,   315,
     296,   317,   317,   209,   211,   214,   215,   291,   321,   332,
     462,   480,   482,   483,   485,    85,   209,   212,   291,   476,
     482,   484,   488,    42,   155,   207,   210,   291,   321,   332,
     493,   207,   210,   291,   321,   332,   497,    76,    77,    78,
     112,   113,   114,   295,   295,   316,   316,   300,   458,   313,
     466,   467,   290,    52,   454,   463,   464,     8,   293,   296,
     296,   326,   328,   329,   301,   359,   446,    20,   336,   476,
     137,   316,    20,    66,    67,   468,   317,   295,   295,   295,
     295,   317,   317,   317,   318,   317,   319,   318,   319,   317,
     317,   317,   318,   300,   453,   453,   453,   305,   454,   454,
      21,   293,   300,   302,   293,   317,    40,    52,    53,    70,
     120,   289,   292,   303,   351,   352,   355,   293,   111,   372,
     376,   317,   317,   491,   111,   288,   104,   454,   290,   290,
     290,   390,   290,   317,   313,   385,   299,   458,   304,   317,
     299,   316,   299,   316,   317,   367,    20,   295,    21,   387,
     449,   450,   451,   291,   454,   395,    57,   392,   404,   315,
     317,   392,   404,   404,   404,    63,    63,   295,   295,   316,
     454,   295,   414,   458,   315,   317,   445,   296,   313,   296,
     300,   296,   296,   296,   296,   296,   409,   296,   304,     9,
     293,   213,   298,   305,   317,   481,   298,   313,   414,   414,
     298,   298,   414,   414,   295,   216,   317,   316,   216,   316,
     216,   317,     3,    17,    22,    23,    25,    26,    27,    28,
      29,    30,    32,    33,    35,    36,    37,    38,    39,    81,
      83,    88,   104,   137,   227,   229,   230,   231,   232,   233,
     234,   235,   236,   237,   238,   239,   240,   241,   242,   243,
     244,   245,   249,   290,   381,   382,   455,    64,   363,   300,
     298,   296,   293,   328,     9,   298,   291,   293,     9,   298,
     291,    23,    24,    25,    27,    28,    29,    30,    32,    33,
      36,    37,    38,    39,    43,    44,   111,   321,   330,   412,
     413,   417,   299,   447,   295,   295,   316,   386,    29,    30,
      64,   203,   204,   206,   414,   316,   316,   296,   296,   317,
     317,   317,   318,   296,   296,   296,   296,   296,   296,   296,
     296,   296,   296,   296,   296,   453,   295,   296,   296,   317,
     461,   458,   296,   295,    40,   353,   354,   352,   295,   316,
     357,   302,   454,   454,    73,   118,   316,   454,    73,   118,
     367,   316,   299,   316,   299,   316,   367,    20,   346,   373,
     377,   291,   492,   296,   137,   385,    40,    47,    49,    50,
      51,    88,    89,    90,   134,   135,   136,   138,   141,   296,
     251,   252,   253,   317,   226,   380,   317,   300,   317,   317,
     293,   300,   458,   386,   449,   458,   296,   293,   315,   317,
     315,   317,   317,   318,    20,   313,   296,   295,   293,   293,
     296,   296,   410,   410,   410,   410,   410,   410,   317,   317,
     317,   295,   304,   295,   296,   296,   295,   295,   296,   296,
     317,   453,   316,    64,   316,   296,    26,    27,    28,    29,
      30,   295,   456,   243,   235,   247,   295,   228,   248,    23,
     456,   456,     3,    22,    23,    25,    26,    27,    28,    29,
      30,    32,    33,    35,    36,    37,    38,    39,   230,   231,
     233,   234,   235,   237,   238,   241,   242,   244,   257,   258,
     259,   260,   261,   262,   263,   264,   265,   266,   267,   268,
     269,   270,   305,   457,   296,   415,   299,   305,   315,   298,
     364,    29,   313,   317,   452,   468,   469,   466,   291,   298,
     290,   463,   290,   295,   313,   295,   299,   295,   299,    27,
      28,    29,    30,   295,   299,   295,   299,   295,   299,   295,
     299,   295,   299,   295,   299,   295,   299,   295,   299,   295,
     299,   295,   299,   295,   299,   295,   299,   295,   299,   105,
     110,   321,   331,   414,   317,   302,   449,   449,   359,   446,
     315,   296,   296,   296,   296,   296,   449,   317,   295,   354,
     458,   348,   349,   454,   293,   356,   316,   197,   322,   316,
     458,   317,   317,   293,   458,   386,   291,   170,   171,   172,
     173,   291,   314,   321,   332,   374,   472,   173,   175,   176,
     291,   314,   321,   332,   378,   472,   291,   313,   296,   295,
     304,   304,   300,   300,   300,   300,   295,   386,   137,   300,
     300,   454,   364,   454,   296,   380,   451,    63,    63,   296,
     296,   316,   296,   449,   445,   445,     9,   293,     9,   481,
     296,   317,   250,   313,   299,   299,    26,    27,    28,    29,
      30,   271,   293,   299,   306,   291,   292,   300,   317,    23,
      24,    25,    27,    28,    29,    30,    32,    33,    36,    37,
      38,    39,    45,   313,   412,   416,   295,   295,   290,   330,
     328,   468,   317,   317,   317,   295,   299,   295,   299,   295,
     299,   295,   299,   317,   317,   317,   317,   317,   317,   318,
     317,   317,   319,   317,   318,   319,   317,   317,   317,   317,
     317,   317,   317,   318,   317,   417,   317,     9,    45,   317,
      45,    52,   452,   317,    43,    92,   111,   333,   459,   296,
     300,   296,   296,   295,   295,   475,   296,   296,   317,   316,
     296,   293,   355,   356,   316,   300,   300,   454,   454,   256,
     366,   366,   366,   366,   366,   366,   366,   385,   317,   139,
     140,   139,   140,   381,   350,   315,   293,    20,   315,   315,
     317,   296,   317,   304,   298,   293,   317,   317,   313,   300,
     317,   292,   300,    27,    28,    29,    30,   317,    27,    28,
      29,   317,   330,   291,   291,   296,   300,   296,   300,   317,
     317,   317,   317,   317,   317,   318,   317,   296,   300,   296,
     300,   296,   300,   296,   300,   296,   296,   300,   296,   296,
     300,   296,   300,   296,   300,   296,   300,   296,   300,   296,
     300,   296,   296,   300,   296,     9,   296,   300,    52,   452,
     299,   316,   302,   449,   449,   296,   356,   454,   295,   293,
      20,   367,   296,   296,   296,   295,   454,   386,     9,   481,
     317,   313,   300,   300,   300,   317,   296,   304,   304,   304,
     296,   291,   295,   295,   296,   300,   296,   300,   296,   300,
     296,   300,   295,   295,   295,   295,   295,   295,   295,   295,
     295,   295,   295,   295,   296,   295,     9,   300,   298,   296,
     296,   449,   454,   386,   458,   449,   301,   358,   359,   304,
     296,   293,   296,   455,   300,   317,   317,   317,   425,   423,
     295,   295,   295,   295,   424,   423,   422,   421,   419,   420,
     424,   423,   422,   421,   428,   426,   427,   417,   296,   358,
     454,   296,   295,   481,   313,   296,   296,   296,   296,   468,
     296,   317,   424,   423,   422,   421,   296,   317,   296,   296,
     317,   296,   318,   296,   317,   319,   296,   318,   319,   296,
     296,   296,   296,   296,   417,     9,    45,   296,    45,    52,
     296,   452,   364,   295,    20,   388,   449,   293,   296,   296,
     296,   296,     9,   449,   386,    40,    55,    71,    80,    94,
      95,    96,    97,   126,   127,   128,   129,   130,   131,   132,
     290,   296,   313,   296,   295,   295,   296,   256,   449,   317,
     104,   296,   296,   367,   458,   454,    20,   386,   358,   295,
     449,   296
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
     417,   418,   418,   418,   418,   418,   418,   418,   418,   418,
     418,   418,   418,   418,   418,   418,   418,   418,   418,   419,
     419,   419,   420,   420,   420,   421,   421,   422,   422,   423,
     423,   424,   424,   425,   425,   426,   426,   426,   427,   427,
     427,   427,   428,   428,   429,   430,   431,   432,   433,   434,
     435,   436,   437,   438,   439,   440,   441,   442,   443,   444,
     444,   444,   444,   444,   444,   444,   444,   444,   444,   444,
     444,   444,   444,   444,   444,   444,   444,   444,   444,   444,
     444,   444,   445,   445,   445,   445,   445,   446,   446,   447,
     447,   448,   448,   449,   449,   450,   450,   451,   451,   451,
     452,   452,   452,   452,   452,   452,   452,   452,   452,   452,
     453,   453,   454,   454,   454,   454,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   455,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   455,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   455,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   455,   455,   455,   455,   455,
     455,   455,   455,   455,   455,   455,   455,   455,   455,   455,
     455,   456,   456,   457,   457,   457,   457,   457,   457,   457,
     457,   457,   457,   457,   457,   457,   457,   457,   457,   457,
     457,   457,   457,   457,   457,   457,   457,   457,   457,   457,
     457,   457,   457,   457,   457,   457,   457,   457,   457,   457,
     457,   457,   457,   457,   457,   457,   457,   457,   457,   457,
     457,   457,   458,   458,   458,   458,   458,   458,   458,   458,
     458,   458,   458,   458,   458,   458,   458,   458,   458,   458,
     458,   458,   458,   458,   458,   458,   458,   459,   459,   459,
     459,   459,   459,   459,   459,   459,   459,   459,   459,   459,
     459,   459,   459,   459,   459,   460,   460,   461,   461,   461,
     461,   461,   462,   462,   462,   462,   462,   462,   463,   463,
     463,   464,   464,   465,   465,   466,   466,   467,   468,   468,
     469,   469,   469,   469,   469,   469,   469,   469,   470,   470,
     470,   470,   470,   470,   470,   470,   470,   470,   470,   470,
     470,   470,   470,   471,   471,   472,   472,   472,   472,   472,
     472,   472,   472,   472,   472,   472,   473,   473,   474,   474,
     475,   475,   476,   477,   478,   478,   478,   478,   478,   478,
     478,   478,   478,   478,   479,   479,   480,   480,   480,   481,
     481,   482,   482,   482,   482,   482,   482,   483,   484,   485,
     486,   486,   487,   487,   488,   488,   488,   488,   489,   490,
     491,   491,   491,   491,   491,   491,   491,   491,   491,   491,
     492,   492,   493,   493,   493,   493,   493,   493,   493,   494,
     494,   495,   495,   495,   496,   496,   497,   497,   497,   497
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
       7,     4,     4,     4,     4,     4,     4,     4,     4,     5,
       5,     5,     5,     4,     4,     4,     4,     4,     4,     0,
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
       1,     1,     2,     1,     3,     2,     2,     3,     4,     2,
       2,     2,     5,     5,     7,     4,     3,     2,     3,     2,
       1,     1,     2,     3,     2,     1,     2,     1,     1,     1,
       1,     1,     1,     1,     1,     1,     2,     2,     2,     2,
       1,     1,     1,     1,     1,     1,     3,     0,     1,     1,
       3,     2,     6,     7,     3,     3,     3,     6,     0,     1,
       3,     5,     6,     4,     4,     1,     3,     3,     1,     1,
       1,     1,     4,     1,     6,     6,     6,     4,     1,     1,
       1,     1,     1,     1,     1,     1,     1,     1,     1,     1,
       1,     1,     1,     1,     1,     3,     2,     5,     4,     7,
       6,     7,     6,     9,     8,     3,     8,     4,     0,     2,
       0,     1,     3,     3,     0,     2,     2,     2,     3,     2,
       2,     2,     2,     2,     0,     2,     3,     1,     1,     1,
       1,     3,     8,     2,     3,     1,     1,     3,     3,     3,
       4,     6,     0,     2,     3,     1,     3,     1,     4,     3,
       0,     2,     2,     2,     3,     3,     3,     3,     3,     3,
       0,     2,     2,     3,     3,     4,     2,     1,     1,     3,
       5,     0,     2,     2,     0,     2,     4,     3,     1,     1
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
#line 195 "asmparse.y"
                                                                                { PASM->EndClass(); }
#line 3875 "asmparse.cpp"
    break;

  case 5: /* decl: nameSpaceHead '{' decls '}'  */
#line 196 "asmparse.y"
                                                                                { PASM->EndNameSpace(); }
#line 3881 "asmparse.cpp"
    break;

  case 6: /* decl: methodHead methodDecls '}'  */
#line 197 "asmparse.y"
                                                                                { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                                                  {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                                     PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                                                  PASM->EndMethod(); }
#line 3890 "asmparse.cpp"
    break;

  case 13: /* decl: assemblyHead '{' assemblyDecls '}'  */
#line 207 "asmparse.y"
                                                                                { PASMM->EndAssembly(); }
#line 3896 "asmparse.cpp"
    break;

  case 14: /* decl: assemblyRefHead '{' assemblyRefDecls '}'  */
#line 208 "asmparse.y"
                                                                                { PASMM->EndAssembly(); }
#line 3902 "asmparse.cpp"
    break;

  case 15: /* decl: exptypeHead '{' exptypeDecls '}'  */
#line 209 "asmparse.y"
                                                                                { PASMM->EndComType(); }
#line 3908 "asmparse.cpp"
    break;

  case 16: /* decl: manifestResHead '{' manifestResDecls '}'  */
#line 210 "asmparse.y"
                                                                                { PASMM->EndManifestRes(); }
#line 3914 "asmparse.cpp"
    break;

  case 20: /* decl: _SUBSYSTEM int32  */
#line 214 "asmparse.y"
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
#line 3929 "asmparse.cpp"
    break;

  case 21: /* decl: _CORFLAGS int32  */
#line 224 "asmparse.y"
                                                                                { PASM->m_dwComImageFlags = (yyvsp[0].int32); }
#line 3935 "asmparse.cpp"
    break;

  case 22: /* decl: _FILE ALIGNMENT_ int32  */
#line 225 "asmparse.y"
                                                                                { PASM->m_dwFileAlignment = (yyvsp[0].int32);
                                                                                  if(((yyvsp[0].int32) & ((yyvsp[0].int32) - 1))||((yyvsp[0].int32) < 0x200)||((yyvsp[0].int32) > 0x10000))
                                                                                    PASM->report->error("Invalid file alignment, must be power of 2 from 0x200 to 0x10000\n");}
#line 3943 "asmparse.cpp"
    break;

  case 23: /* decl: _IMAGEBASE int64  */
#line 228 "asmparse.y"
                                                                                { PASM->m_stBaseAddress = (ULONGLONG)(*((yyvsp[0].int64))); delete (yyvsp[0].int64);
                                                                                  if(PASM->m_stBaseAddress & 0xFFFF)
                                                                                    PASM->report->error("Invalid image base, must be 0x10000-aligned\n");}
#line 3951 "asmparse.cpp"
    break;

  case 24: /* decl: _STACKRESERVE int64  */
#line 231 "asmparse.y"
                                                                                { PASM->m_stSizeOfStackReserve = (size_t)(*((yyvsp[0].int64))); delete (yyvsp[0].int64); }
#line 3957 "asmparse.cpp"
    break;

  case 29: /* decl: _MSCORLIB  */
#line 236 "asmparse.y"
                                                                                { PASM->m_fIsMscorlib = TRUE; }
#line 3963 "asmparse.cpp"
    break;

  case 32: /* compQstring: QSTRING  */
#line 243 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[0].binstr); }
#line 3969 "asmparse.cpp"
    break;

  case 33: /* compQstring: compQstring '+' QSTRING  */
#line 244 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 3975 "asmparse.cpp"
    break;

  case 34: /* languageDecl: _LANGUAGE SQSTRING  */
#line 247 "asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[0].string),&(PASM->m_guidLang)); }
#line 3981 "asmparse.cpp"
    break;

  case 35: /* languageDecl: _LANGUAGE SQSTRING ',' SQSTRING  */
#line 248 "asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid((yyvsp[0].string),&(PASM->m_guidLangVendor));}
#line 3988 "asmparse.cpp"
    break;

  case 36: /* languageDecl: _LANGUAGE SQSTRING ',' SQSTRING ',' SQSTRING  */
#line 250 "asmparse.y"
                                                                                { LPCSTRToGuid((yyvsp[-4].string),&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidLangVendor));
                                                                                  LPCSTRToGuid((yyvsp[-2].string),&(PASM->m_guidDoc));}
#line 3996 "asmparse.cpp"
    break;

  case 37: /* id: ID  */
#line 255 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 4002 "asmparse.cpp"
    break;

  case 38: /* id: SQSTRING  */
#line 256 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 4008 "asmparse.cpp"
    break;

  case 39: /* dottedName: id  */
#line 259 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 4014 "asmparse.cpp"
    break;

  case 40: /* dottedName: DOTTEDNAME  */
#line 260 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 4020 "asmparse.cpp"
    break;

  case 41: /* dottedName: dottedName '.' dottedName  */
#line 261 "asmparse.y"
                                                              { (yyval.string) = newStringWDel((yyvsp[-2].string), '.', (yyvsp[0].string)); }
#line 4026 "asmparse.cpp"
    break;

  case 42: /* int32: INT32_T  */
#line 264 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 4032 "asmparse.cpp"
    break;

  case 43: /* int64: INT64_T  */
#line 267 "asmparse.y"
                                                              { (yyval.int64) = (yyvsp[0].int64); }
#line 4038 "asmparse.cpp"
    break;

  case 44: /* int64: INT32_T  */
#line 268 "asmparse.y"
                                                              { (yyval.int64) = neg ? new __int64((yyvsp[0].int32)) : new __int64((unsigned)(yyvsp[0].int32)); }
#line 4044 "asmparse.cpp"
    break;

  case 45: /* float64: FLOAT64  */
#line 271 "asmparse.y"
                                                              { (yyval.float64) = (yyvsp[0].float64); }
#line 4050 "asmparse.cpp"
    break;

  case 46: /* float64: FLOAT32_ '(' int32 ')'  */
#line 272 "asmparse.y"
                                                              { float f; *((__int32*) (&f)) = (yyvsp[-1].int32); (yyval.float64) = new double(f); }
#line 4056 "asmparse.cpp"
    break;

  case 47: /* float64: FLOAT64_ '(' int64 ')'  */
#line 273 "asmparse.y"
                                                              { (yyval.float64) = (double*) (yyvsp[-1].int64); }
#line 4062 "asmparse.cpp"
    break;

  case 48: /* typedefDecl: _TYPEDEF type AS_ dottedName  */
#line 277 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].binstr),(yyvsp[0].string)); }
#line 4068 "asmparse.cpp"
    break;

  case 49: /* typedefDecl: _TYPEDEF className AS_ dottedName  */
#line 278 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].token),(yyvsp[0].string)); }
#line 4074 "asmparse.cpp"
    break;

  case 50: /* typedefDecl: _TYPEDEF memberRef AS_ dottedName  */
#line 279 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].token),(yyvsp[0].string)); }
#line 4080 "asmparse.cpp"
    break;

  case 51: /* typedefDecl: _TYPEDEF customDescr AS_ dottedName  */
#line 280 "asmparse.y"
                                                                                { (yyvsp[-2].cad)->tkOwner = 0; PASM->AddTypeDef((yyvsp[-2].cad),(yyvsp[0].string)); }
#line 4086 "asmparse.cpp"
    break;

  case 52: /* typedefDecl: _TYPEDEF customDescrWithOwner AS_ dottedName  */
#line 281 "asmparse.y"
                                                                                { PASM->AddTypeDef((yyvsp[-2].cad),(yyvsp[0].string)); }
#line 4092 "asmparse.cpp"
    break;

  case 53: /* compControl: P_DEFINE dottedName  */
#line 286 "asmparse.y"
                                                                                { DefineVar((yyvsp[0].string), NULL); }
#line 4098 "asmparse.cpp"
    break;

  case 54: /* compControl: P_DEFINE dottedName compQstring  */
#line 287 "asmparse.y"
                                                                                { DefineVar((yyvsp[-1].string), (yyvsp[0].binstr)); }
#line 4104 "asmparse.cpp"
    break;

  case 55: /* compControl: P_UNDEF dottedName  */
#line 288 "asmparse.y"
                                                                                { UndefVar((yyvsp[0].string)); }
#line 4110 "asmparse.cpp"
    break;

  case 56: /* compControl: P_IFDEF dottedName  */
#line 289 "asmparse.y"
                                                                                { SkipToken = !IsVarDefined((yyvsp[0].string));
                                                                                  IfEndif++;
                                                                                }
#line 4118 "asmparse.cpp"
    break;

  case 57: /* compControl: P_IFNDEF dottedName  */
#line 292 "asmparse.y"
                                                                                { SkipToken = IsVarDefined((yyvsp[0].string));
                                                                                  IfEndif++;
                                                                                }
#line 4126 "asmparse.cpp"
    break;

  case 58: /* compControl: P_ELSE  */
#line 295 "asmparse.y"
                                                                                { if(IfEndif == 1) SkipToken = !SkipToken;}
#line 4132 "asmparse.cpp"
    break;

  case 59: /* compControl: P_ENDIF  */
#line 296 "asmparse.y"
                                                                                { if(IfEndif == 0)
                                                                                    PASM->report->error("Unmatched #endif\n");
                                                                                  else IfEndif--;
                                                                                }
#line 4141 "asmparse.cpp"
    break;

  case 60: /* compControl: P_INCLUDE QSTRING  */
#line 300 "asmparse.y"
                                                                                { _ASSERTE(!"yylex should have dealt with this"); }
#line 4147 "asmparse.cpp"
    break;

  case 61: /* compControl: ';'  */
#line 301 "asmparse.y"
                                                                                { }
#line 4153 "asmparse.cpp"
    break;

  case 62: /* customDescr: _CUSTOM customType  */
#line 305 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[0].token), NULL); }
#line 4159 "asmparse.cpp"
    break;

  case 63: /* customDescr: _CUSTOM customType '=' compQstring  */
#line 306 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].token), (yyvsp[0].binstr)); }
#line 4165 "asmparse.cpp"
    break;

  case 64: /* customDescr: _CUSTOM customType '=' '{' customBlobDescr '}'  */
#line 307 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 4171 "asmparse.cpp"
    break;

  case 65: /* customDescr: customHead bytes ')'  */
#line 308 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].int32), (yyvsp[-1].binstr)); }
#line 4177 "asmparse.cpp"
    break;

  case 66: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType  */
#line 311 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-2].token), (yyvsp[0].token), NULL); }
#line 4183 "asmparse.cpp"
    break;

  case 67: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType '=' compQstring  */
#line 312 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-4].token), (yyvsp[-2].token), (yyvsp[0].binstr)); }
#line 4189 "asmparse.cpp"
    break;

  case 68: /* customDescrWithOwner: _CUSTOM '(' ownerType ')' customType '=' '{' customBlobDescr '}'  */
#line 314 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr((yyvsp[-6].token), (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 4195 "asmparse.cpp"
    break;

  case 69: /* customDescrWithOwner: customHeadWithOwner bytes ')'  */
#line 315 "asmparse.y"
                                                                                { (yyval.cad) = new CustomDescr(PASM->m_tkCurrentCVOwner, (yyvsp[-2].int32), (yyvsp[-1].binstr)); }
#line 4201 "asmparse.cpp"
    break;

  case 70: /* customHead: _CUSTOM customType '=' '('  */
#line 318 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-2].token); bParsingByteArray = TRUE; }
#line 4207 "asmparse.cpp"
    break;

  case 71: /* customHeadWithOwner: _CUSTOM '(' ownerType ')' customType '=' '('  */
#line 322 "asmparse.y"
                                                                                { PASM->m_pCustomDescrList = NULL;
                                                                                  PASM->m_tkCurrentCVOwner = (yyvsp[-4].token);
                                                                                  (yyval.int32) = (yyvsp[-2].token); bParsingByteArray = TRUE; }
#line 4215 "asmparse.cpp"
    break;

  case 72: /* customType: methodRef  */
#line 327 "asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 4221 "asmparse.cpp"
    break;

  case 73: /* ownerType: typeSpec  */
#line 330 "asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 4227 "asmparse.cpp"
    break;

  case 74: /* ownerType: memberRef  */
#line 331 "asmparse.y"
                                                            { (yyval.token) = (yyvsp[0].token); }
#line 4233 "asmparse.cpp"
    break;

  case 75: /* customBlobDescr: customBlobArgs customBlobNVPairs  */
#line 335 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr);
                                                                                  (yyval.binstr)->appendInt16(VAL16(nCustomBlobNVPairs));
                                                                                  (yyval.binstr)->append((yyvsp[0].binstr));
                                                                                  nCustomBlobNVPairs = 0; }
#line 4242 "asmparse.cpp"
    break;

  case 76: /* customBlobArgs: %empty  */
#line 341 "asmparse.y"
                                                                                { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt16(VAL16(0x0001)); }
#line 4248 "asmparse.cpp"
    break;

  case 77: /* customBlobArgs: customBlobArgs serInit  */
#line 342 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr);
                                                                                  AppendFieldToCustomBlob((yyval.binstr),(yyvsp[0].binstr)); }
#line 4255 "asmparse.cpp"
    break;

  case 78: /* customBlobArgs: customBlobArgs compControl  */
#line 344 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 4261 "asmparse.cpp"
    break;

  case 79: /* customBlobNVPairs: %empty  */
#line 347 "asmparse.y"
                                                                                { (yyval.binstr) = new BinStr(); }
#line 4267 "asmparse.cpp"
    break;

  case 80: /* customBlobNVPairs: customBlobNVPairs fieldOrProp serializType dottedName '=' serInit  */
#line 349 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-5].binstr); (yyval.binstr)->appendInt8((yyvsp[-4].int32));
                                                                                  (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                                  AppendStringWithLength((yyval.binstr),(yyvsp[-2].string));
                                                                                  AppendFieldToCustomBlob((yyval.binstr),(yyvsp[0].binstr));
                                                                                  nCustomBlobNVPairs++; }
#line 4277 "asmparse.cpp"
    break;

  case 81: /* customBlobNVPairs: customBlobNVPairs compControl  */
#line 354 "asmparse.y"
                                                                                { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 4283 "asmparse.cpp"
    break;

  case 82: /* fieldOrProp: FIELD_  */
#line 357 "asmparse.y"
                                                                                { (yyval.int32) = SERIALIZATION_TYPE_FIELD; }
#line 4289 "asmparse.cpp"
    break;

  case 83: /* fieldOrProp: PROPERTY_  */
#line 358 "asmparse.y"
                                                                                { (yyval.int32) = SERIALIZATION_TYPE_PROPERTY; }
#line 4295 "asmparse.cpp"
    break;

  case 84: /* customAttrDecl: customDescr  */
#line 361 "asmparse.y"
                                                                                { if((yyvsp[0].cad)->tkOwner && !(yyvsp[0].cad)->tkInterfacePair)
                                                                                    PASM->DefineCV((yyvsp[0].cad));
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH((yyvsp[0].cad)); }
#line 4304 "asmparse.cpp"
    break;

  case 85: /* customAttrDecl: customDescrWithOwner  */
#line 365 "asmparse.y"
                                                                                { PASM->DefineCV((yyvsp[0].cad)); }
#line 4310 "asmparse.cpp"
    break;

  case 86: /* customAttrDecl: TYPEDEF_CA  */
#line 366 "asmparse.y"
                                                                                { CustomDescr* pNew = new CustomDescr((yyvsp[0].tdd)->m_pCA);
                                                                                  if(pNew->tkOwner == 0) pNew->tkOwner = PASM->m_tkCurrentCVOwner;
                                                                                  if(pNew->tkOwner)
                                                                                    PASM->DefineCV(pNew);
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH(pNew); }
#line 4321 "asmparse.cpp"
    break;

  case 87: /* serializType: simpleType  */
#line 374 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4327 "asmparse.cpp"
    break;

  case 88: /* serializType: TYPE_  */
#line 375 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE); }
#line 4333 "asmparse.cpp"
    break;

  case 89: /* serializType: OBJECT_  */
#line 376 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TAGGED_OBJECT); }
#line 4339 "asmparse.cpp"
    break;

  case 90: /* serializType: ENUM_ CLASS_ SQSTRING  */
#line 377 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); }
#line 4346 "asmparse.cpp"
    break;

  case 91: /* serializType: ENUM_ className  */
#line 379 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[0].token))); }
#line 4353 "asmparse.cpp"
    break;

  case 92: /* serializType: serializType '[' ']'  */
#line 381 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 4359 "asmparse.cpp"
    break;

  case 93: /* moduleHead: _MODULE  */
#line 386 "asmparse.y"
                                                                                { PASMM->SetModuleName(NULL); PASM->m_tkCurrentCVOwner=1; }
#line 4365 "asmparse.cpp"
    break;

  case 94: /* moduleHead: _MODULE dottedName  */
#line 387 "asmparse.y"
                                                                                { PASMM->SetModuleName((yyvsp[0].string)); PASM->m_tkCurrentCVOwner=1; }
#line 4371 "asmparse.cpp"
    break;

  case 95: /* moduleHead: _MODULE EXTERN_ dottedName  */
#line 388 "asmparse.y"
                                                                                { BinStr* pbs = new BinStr();
                                                                                  unsigned L = (unsigned)strlen((yyvsp[0].string));
                                                                                  memcpy((char*)(pbs->getBuff(L)),(yyvsp[0].string),L);
                                                                                  PASM->EmitImport(pbs); delete pbs;}
#line 4380 "asmparse.cpp"
    break;

  case 96: /* vtfixupDecl: _VTFIXUP '[' int32 ']' vtfixupAttr AT_ id  */
#line 395 "asmparse.y"
                                                                                { /*PASM->SetDataSection(); PASM->EmitDataLabel($7);*/
                                                                                  PASM->m_VTFList.PUSH(new VTFEntry((USHORT)(yyvsp[-4].int32), (USHORT)(yyvsp[-2].int32), (yyvsp[0].string))); }
#line 4387 "asmparse.cpp"
    break;

  case 97: /* vtfixupAttr: %empty  */
#line 399 "asmparse.y"
                                                                                { (yyval.int32) = 0; }
#line 4393 "asmparse.cpp"
    break;

  case 98: /* vtfixupAttr: vtfixupAttr INT32_  */
#line 400 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_32BIT; }
#line 4399 "asmparse.cpp"
    break;

  case 99: /* vtfixupAttr: vtfixupAttr INT64_  */
#line 401 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_64BIT; }
#line 4405 "asmparse.cpp"
    break;

  case 100: /* vtfixupAttr: vtfixupAttr FROMUNMANAGED_  */
#line 402 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_FROM_UNMANAGED; }
#line 4411 "asmparse.cpp"
    break;

  case 101: /* vtfixupAttr: vtfixupAttr CALLMOSTDERIVED_  */
#line 403 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_CALL_MOST_DERIVED; }
#line 4417 "asmparse.cpp"
    break;

  case 102: /* vtfixupAttr: vtfixupAttr RETAINAPPDOMAIN_  */
#line 404 "asmparse.y"
                                                                                { (yyval.int32) = (yyvsp[-1].int32) | COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN; }
#line 4423 "asmparse.cpp"
    break;

  case 103: /* vtableDecl: vtableHead bytes ')'  */
#line 407 "asmparse.y"
                                                                                { PASM->m_pVTable = (yyvsp[-1].binstr); }
#line 4429 "asmparse.cpp"
    break;

  case 104: /* vtableHead: _VTABLE '=' '('  */
#line 410 "asmparse.y"
                                                                                { bParsingByteArray = TRUE; }
#line 4435 "asmparse.cpp"
    break;

  case 105: /* nameSpaceHead: _NAMESPACE dottedName  */
#line 414 "asmparse.y"
                                                                                { PASM->StartNameSpace((yyvsp[0].string)); }
#line 4441 "asmparse.cpp"
    break;

  case 106: /* _class: _CLASS  */
#line 417 "asmparse.y"
                                                                                { newclass = TRUE; }
#line 4447 "asmparse.cpp"
    break;

  case 107: /* classHeadBegin: _class classAttr dottedName typarsClause  */
#line 420 "asmparse.y"
                                                                                { if((yyvsp[0].typarlist)) FixupConstraints();
                                                                                  PASM->StartClass((yyvsp[-1].string), (yyvsp[-2].classAttr), (yyvsp[0].typarlist));
                                                                                  TyParFixupList.RESET(false);
                                                                                  newclass = FALSE;
                                                                                }
#line 4457 "asmparse.cpp"
    break;

  case 108: /* classHead: classHeadBegin extendsClause implClause  */
#line 426 "asmparse.y"
                                                                                { PASM->AddClass(); }
#line 4463 "asmparse.cpp"
    break;

  case 109: /* classAttr: %empty  */
#line 429 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) 0; }
#line 4469 "asmparse.cpp"
    break;

  case 110: /* classAttr: classAttr PUBLIC_  */
#line 430 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdVisibilityMask) | tdPublic); }
#line 4475 "asmparse.cpp"
    break;

  case 111: /* classAttr: classAttr PRIVATE_  */
#line 431 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdVisibilityMask) | tdNotPublic); }
#line 4481 "asmparse.cpp"
    break;

  case 112: /* classAttr: classAttr VALUE_  */
#line 432 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | 0x80000000 | tdSealed); }
#line 4487 "asmparse.cpp"
    break;

  case 113: /* classAttr: classAttr ENUM_  */
#line 433 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | 0x40000000); }
#line 4493 "asmparse.cpp"
    break;

  case 114: /* classAttr: classAttr INTERFACE_  */
#line 434 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdInterface | tdAbstract); }
#line 4499 "asmparse.cpp"
    break;

  case 115: /* classAttr: classAttr SEALED_  */
#line 435 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSealed); }
#line 4505 "asmparse.cpp"
    break;

  case 116: /* classAttr: classAttr ABSTRACT_  */
#line 436 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdAbstract); }
#line 4511 "asmparse.cpp"
    break;

  case 117: /* classAttr: classAttr AUTO_  */
#line 437 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdAutoLayout); }
#line 4517 "asmparse.cpp"
    break;

  case 118: /* classAttr: classAttr SEQUENTIAL_  */
#line 438 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdSequentialLayout); }
#line 4523 "asmparse.cpp"
    break;

  case 119: /* classAttr: classAttr EXPLICIT_  */
#line 439 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdLayoutMask) | tdExplicitLayout); }
#line 4529 "asmparse.cpp"
    break;

  case 120: /* classAttr: classAttr ANSI_  */
#line 440 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdAnsiClass); }
#line 4535 "asmparse.cpp"
    break;

  case 121: /* classAttr: classAttr UNICODE_  */
#line 441 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdUnicodeClass); }
#line 4541 "asmparse.cpp"
    break;

  case 122: /* classAttr: classAttr AUTOCHAR_  */
#line 442 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-1].classAttr) & ~tdStringFormatMask) | tdAutoClass); }
#line 4547 "asmparse.cpp"
    break;

  case 123: /* classAttr: classAttr IMPORT_  */
#line 443 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdImport); }
#line 4553 "asmparse.cpp"
    break;

  case 124: /* classAttr: classAttr SERIALIZABLE_  */
#line 444 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSerializable); }
#line 4559 "asmparse.cpp"
    break;

  case 125: /* classAttr: classAttr WINDOWSRUNTIME_  */
#line 445 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdWindowsRuntime); }
#line 4565 "asmparse.cpp"
    break;

  case 126: /* classAttr: classAttr NESTED_ PUBLIC_  */
#line 446 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedPublic); }
#line 4571 "asmparse.cpp"
    break;

  case 127: /* classAttr: classAttr NESTED_ PRIVATE_  */
#line 447 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedPrivate); }
#line 4577 "asmparse.cpp"
    break;

  case 128: /* classAttr: classAttr NESTED_ FAMILY_  */
#line 448 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamily); }
#line 4583 "asmparse.cpp"
    break;

  case 129: /* classAttr: classAttr NESTED_ ASSEMBLY_  */
#line 449 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedAssembly); }
#line 4589 "asmparse.cpp"
    break;

  case 130: /* classAttr: classAttr NESTED_ FAMANDASSEM_  */
#line 450 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamANDAssem); }
#line 4595 "asmparse.cpp"
    break;

  case 131: /* classAttr: classAttr NESTED_ FAMORASSEM_  */
#line 451 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) (((yyvsp[-2].classAttr) & ~tdVisibilityMask) | tdNestedFamORAssem); }
#line 4601 "asmparse.cpp"
    break;

  case 132: /* classAttr: classAttr BEFOREFIELDINIT_  */
#line 452 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdBeforeFieldInit); }
#line 4607 "asmparse.cpp"
    break;

  case 133: /* classAttr: classAttr SPECIALNAME_  */
#line 453 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr) | tdSpecialName); }
#line 4613 "asmparse.cpp"
    break;

  case 134: /* classAttr: classAttr RTSPECIALNAME_  */
#line 454 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].classAttr)); }
#line 4619 "asmparse.cpp"
    break;

  case 135: /* classAttr: classAttr FLAGS_ '(' int32 ')'  */
#line 455 "asmparse.y"
                                                            { (yyval.classAttr) = (CorRegTypeAttr) ((yyvsp[-1].int32)); }
#line 4625 "asmparse.cpp"
    break;

  case 137: /* extendsClause: EXTENDS_ typeSpec  */
#line 459 "asmparse.y"
                                                                            { PASM->m_crExtends = (yyvsp[0].token); }
#line 4631 "asmparse.cpp"
    break;

  case 142: /* implList: implList ',' typeSpec  */
#line 470 "asmparse.y"
                                                            { PASM->AddToImplList((yyvsp[0].token)); }
#line 4637 "asmparse.cpp"
    break;

  case 143: /* implList: typeSpec  */
#line 471 "asmparse.y"
                                                            { PASM->AddToImplList((yyvsp[0].token)); }
#line 4643 "asmparse.cpp"
    break;

  case 144: /* typeList: %empty  */
#line 475 "asmparse.y"
                                                            { (yyval.binstr) = new BinStr(); }
#line 4649 "asmparse.cpp"
    break;

  case 145: /* typeList: typeListNotEmpty  */
#line 476 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 4655 "asmparse.cpp"
    break;

  case 146: /* typeListNotEmpty: typeSpec  */
#line 479 "asmparse.y"
                                                            { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[0].token)); }
#line 4661 "asmparse.cpp"
    break;

  case 147: /* typeListNotEmpty: typeListNotEmpty ',' typeSpec  */
#line 480 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->appendInt32((yyvsp[0].token)); }
#line 4667 "asmparse.cpp"
    break;

  case 148: /* typarsClause: %empty  */
#line 483 "asmparse.y"
                                                            { (yyval.typarlist) = NULL; PASM->m_TyParList = NULL;}
#line 4673 "asmparse.cpp"
    break;

  case 149: /* typarsClause: '<' typars '>'  */
#line 484 "asmparse.y"
                                                            { PASM->m_TyParList = (yyvsp[-1].typarlist); ResolveTyParList(PASM->m_TyParList); (yyval.typarlist) = PASM->m_TyParList; }
#line 4679 "asmparse.cpp"
    break;

  case 150: /* typarAttrib: '+'  */
#line 487 "asmparse.y"
                                                            { (yyval.int32) = gpCovariant; }
#line 4685 "asmparse.cpp"
    break;

  case 151: /* typarAttrib: '-'  */
#line 488 "asmparse.y"
                                                            { (yyval.int32) = gpContravariant; }
#line 4691 "asmparse.cpp"
    break;

  case 152: /* typarAttrib: CLASS_  */
#line 489 "asmparse.y"
                                                            { (yyval.int32) = gpReferenceTypeConstraint; }
#line 4697 "asmparse.cpp"
    break;

  case 153: /* typarAttrib: VALUETYPE_  */
#line 490 "asmparse.y"
                                                            { (yyval.int32) = gpNotNullableValueTypeConstraint; }
#line 4703 "asmparse.cpp"
    break;

  case 154: /* typarAttrib: BYREFLIKE_  */
#line 491 "asmparse.y"
                                                            { (yyval.int32) = gpAcceptByRefLike; }
#line 4709 "asmparse.cpp"
    break;

  case 155: /* typarAttrib: _CTOR  */
#line 492 "asmparse.y"
                                                            { (yyval.int32) = gpDefaultConstructorConstraint; }
#line 4715 "asmparse.cpp"
    break;

  case 156: /* typarAttrib: FLAGS_ '(' int32 ')'  */
#line 493 "asmparse.y"
                                                            { (yyval.int32) = (CorGenericParamAttr)(yyvsp[-1].int32); }
#line 4721 "asmparse.cpp"
    break;

  case 157: /* typarAttribs: %empty  */
#line 496 "asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 4727 "asmparse.cpp"
    break;

  case 158: /* typarAttribs: typarAttrib typarAttribs  */
#line 497 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) | (yyvsp[0].int32); }
#line 4733 "asmparse.cpp"
    break;

  case 159: /* conTyparAttrib: FLAGS_ '(' int32 ')'  */
#line 500 "asmparse.y"
                                                            { (yyval.int32) = (CorGenericParamAttr)(yyvsp[-1].int32); }
#line 4739 "asmparse.cpp"
    break;

  case 160: /* conTyparAttribs: %empty  */
#line 503 "asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 4745 "asmparse.cpp"
    break;

  case 161: /* conTyparAttribs: conTyparAttrib conTyparAttribs  */
#line 504 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) | (yyvsp[0].int32); }
#line 4751 "asmparse.cpp"
    break;

  case 162: /* typars: CONST_ conTyparAttribs type dottedName typarsRest  */
#line 507 "asmparse.y"
                                                                            {(yyval.typarlist) = new TyParList((yyvsp[-3].int32), (yyvsp[-2].binstr), NULL, (yyvsp[-1].string), (yyvsp[0].typarlist)); }
#line 4757 "asmparse.cpp"
    break;

  case 163: /* typars: typarAttribs tyBound dottedName typarsRest  */
#line 508 "asmparse.y"
                                                                     {(yyval.typarlist) = new TyParList((yyvsp[-3].int32), (yyvsp[-2].binstr), (yyvsp[-1].string), (yyvsp[0].typarlist));}
#line 4763 "asmparse.cpp"
    break;

  case 164: /* typars: typarAttribs dottedName typarsRest  */
#line 509 "asmparse.y"
                                                               {(yyval.typarlist) = new TyParList((yyvsp[-2].int32), NULL, (yyvsp[-1].string), (yyvsp[0].typarlist));}
#line 4769 "asmparse.cpp"
    break;

  case 165: /* typarsRest: %empty  */
#line 512 "asmparse.y"
                                                            { (yyval.typarlist) = NULL; }
#line 4775 "asmparse.cpp"
    break;

  case 166: /* typarsRest: ',' typars  */
#line 513 "asmparse.y"
                                                            { (yyval.typarlist) = (yyvsp[0].typarlist); }
#line 4781 "asmparse.cpp"
    break;

  case 167: /* tyBound: '(' typeList ')'  */
#line 516 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 4787 "asmparse.cpp"
    break;

  case 168: /* genArity: %empty  */
#line 519 "asmparse.y"
                                                            { (yyval.int32)= 0; }
#line 4793 "asmparse.cpp"
    break;

  case 169: /* genArity: genArityNotEmpty  */
#line 520 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[0].int32); }
#line 4799 "asmparse.cpp"
    break;

  case 170: /* genArityNotEmpty: '<' '[' int32 ']' '>'  */
#line 523 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-2].int32); }
#line 4805 "asmparse.cpp"
    break;

  case 171: /* classDecl: methodHead methodDecls '}'  */
#line 527 "asmparse.y"
                                                            { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                              {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                 PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                              PASM->EndMethod(); }
#line 4814 "asmparse.cpp"
    break;

  case 172: /* classDecl: classHead '{' classDecls '}'  */
#line 531 "asmparse.y"
                                                            { PASM->EndClass(); }
#line 4820 "asmparse.cpp"
    break;

  case 173: /* classDecl: eventHead '{' eventDecls '}'  */
#line 532 "asmparse.y"
                                                            { PASM->EndEvent(); }
#line 4826 "asmparse.cpp"
    break;

  case 174: /* classDecl: propHead '{' propDecls '}'  */
#line 533 "asmparse.y"
                                                            { PASM->EndProp(); }
#line 4832 "asmparse.cpp"
    break;

  case 180: /* classDecl: _SIZE int32  */
#line 539 "asmparse.y"
                                                                { PASM->m_pCurClass->m_ulSize = (yyvsp[0].int32); }
#line 4838 "asmparse.cpp"
    break;

  case 181: /* classDecl: _PACK int32  */
#line 540 "asmparse.y"
                                                                { PASM->m_pCurClass->m_ulPack = (yyvsp[0].int32); }
#line 4844 "asmparse.cpp"
    break;

  case 182: /* classDecl: exportHead '{' exptypeDecls '}'  */
#line 541 "asmparse.y"
                                                                { PASMM->EndComType(); }
#line 4850 "asmparse.cpp"
    break;

  case 183: /* classDecl: _OVERRIDE typeSpec DCOLON methodName WITH_ callConv type typeSpec DCOLON methodName '(' sigArgs0 ')'  */
#line 543 "asmparse.y"
                                                                { BinStr *sig1 = parser->MakeSig((yyvsp[-7].int32), (yyvsp[-6].binstr), (yyvsp[-1].binstr));
                                                                  BinStr *sig2 = new BinStr(); sig2->append(sig1);
                                                                  PASM->AddMethodImpl((yyvsp[-11].token),(yyvsp[-9].string),sig1,(yyvsp[-5].token),(yyvsp[-3].string),sig2);
                                                                  PASM->ResetArgNameList();
                                                                }
#line 4860 "asmparse.cpp"
    break;

  case 184: /* classDecl: _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')' WITH_ METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'  */
#line 549 "asmparse.y"
                                                                 { PASM->AddMethodImpl((yyvsp[-17].token),(yyvsp[-15].string),
                                                                      ((yyvsp[-14].int32)==0 ? parser->MakeSig((yyvsp[-19].int32),(yyvsp[-18].binstr),(yyvsp[-12].binstr)) :
                                                                      parser->MakeSig((yyvsp[-19].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-18].binstr),(yyvsp[-12].binstr),(yyvsp[-14].int32))),
                                                                      (yyvsp[-6].token),(yyvsp[-4].string),
                                                                      ((yyvsp[-3].int32)==0 ? parser->MakeSig((yyvsp[-8].int32),(yyvsp[-7].binstr),(yyvsp[-1].binstr)) :
                                                                      parser->MakeSig((yyvsp[-8].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-7].binstr),(yyvsp[-1].binstr),(yyvsp[-3].int32))));
                                                                   PASM->ResetArgNameList();
                                                                 }
#line 4873 "asmparse.cpp"
    break;

  case 187: /* classDecl: _PARAM TYPE_ '[' int32 ']'  */
#line 559 "asmparse.y"
                                                            { if(((yyvsp[-1].int32) > 0) && ((yyvsp[-1].int32) <= (int)PASM->m_pCurClass->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[(yyvsp[-1].int32)-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
#line 4883 "asmparse.cpp"
    break;

  case 188: /* classDecl: _PARAM TYPE_ dottedName  */
#line 564 "asmparse.y"
                                                            { int n = PASM->m_pCurClass->FindTyPar((yyvsp[0].string));
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",(yyvsp[0].string));
                                                            }
#line 4894 "asmparse.cpp"
    break;

  case 189: /* classDecl: _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec  */
#line 570 "asmparse.y"
                                                                        { PASM->AddGenericParamConstraint((yyvsp[-3].int32), 0, (yyvsp[0].token)); }
#line 4900 "asmparse.cpp"
    break;

  case 190: /* classDecl: _PARAM CONSTRAINT_ dottedName ',' typeSpec  */
#line 571 "asmparse.y"
                                                                        { PASM->AddGenericParamConstraint(0, (yyvsp[-2].string), (yyvsp[0].token)); }
#line 4906 "asmparse.cpp"
    break;

  case 191: /* classDecl: _INTERFACEIMPL TYPE_ typeSpec customDescr  */
#line 572 "asmparse.y"
                                                                      { (yyvsp[0].cad)->tkInterfacePair = (yyvsp[-1].token);
                                                                        if(PASM->m_pCustomDescrList)
                                                                            PASM->m_pCustomDescrList->PUSH((yyvsp[0].cad));
                                                                      }
#line 4915 "asmparse.cpp"
    break;

  case 192: /* fieldDecl: _FIELD repeatOpt fieldAttr type dottedName atOpt initOpt  */
#line 580 "asmparse.y"
                                                            { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                              PASM->AddField((yyvsp[-2].string), (yyvsp[-3].binstr), (yyvsp[-4].fieldAttr), (yyvsp[-1].string), (yyvsp[0].binstr), (yyvsp[-5].int32)); }
#line 4922 "asmparse.cpp"
    break;

  case 193: /* fieldAttr: %empty  */
#line 584 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) 0; }
#line 4928 "asmparse.cpp"
    break;

  case 194: /* fieldAttr: fieldAttr STATIC_  */
#line 585 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdStatic); }
#line 4934 "asmparse.cpp"
    break;

  case 195: /* fieldAttr: fieldAttr PUBLIC_  */
#line 586 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPublic); }
#line 4940 "asmparse.cpp"
    break;

  case 196: /* fieldAttr: fieldAttr PRIVATE_  */
#line 587 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPrivate); }
#line 4946 "asmparse.cpp"
    break;

  case 197: /* fieldAttr: fieldAttr FAMILY_  */
#line 588 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamily); }
#line 4952 "asmparse.cpp"
    break;

  case 198: /* fieldAttr: fieldAttr INITONLY_  */
#line 589 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdInitOnly); }
#line 4958 "asmparse.cpp"
    break;

  case 199: /* fieldAttr: fieldAttr RTSPECIALNAME_  */
#line 590 "asmparse.y"
                                                            { (yyval.fieldAttr) = (yyvsp[-1].fieldAttr); }
#line 4964 "asmparse.cpp"
    break;

  case 200: /* fieldAttr: fieldAttr SPECIALNAME_  */
#line 591 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdSpecialName); }
#line 4970 "asmparse.cpp"
    break;

  case 201: /* fieldAttr: fieldAttr MARSHAL_ '(' marshalBlob ')'  */
#line 604 "asmparse.y"
                                                            { PASM->m_pMarshal = (yyvsp[-1].binstr); }
#line 4976 "asmparse.cpp"
    break;

  case 202: /* fieldAttr: fieldAttr ASSEMBLY_  */
#line 605 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdAssembly); }
#line 4982 "asmparse.cpp"
    break;

  case 203: /* fieldAttr: fieldAttr FAMANDASSEM_  */
#line 606 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamANDAssem); }
#line 4988 "asmparse.cpp"
    break;

  case 204: /* fieldAttr: fieldAttr FAMORASSEM_  */
#line 607 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdFamORAssem); }
#line 4994 "asmparse.cpp"
    break;

  case 205: /* fieldAttr: fieldAttr PRIVATESCOPE_  */
#line 608 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) (((yyvsp[-1].fieldAttr) & ~mdMemberAccessMask) | fdPrivateScope); }
#line 5000 "asmparse.cpp"
    break;

  case 206: /* fieldAttr: fieldAttr LITERAL_  */
#line 609 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdLiteral); }
#line 5006 "asmparse.cpp"
    break;

  case 207: /* fieldAttr: fieldAttr NOTSERIALIZED_  */
#line 610 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].fieldAttr) | fdNotSerialized); }
#line 5012 "asmparse.cpp"
    break;

  case 208: /* fieldAttr: fieldAttr FLAGS_ '(' int32 ')'  */
#line 611 "asmparse.y"
                                                            { (yyval.fieldAttr) = (CorFieldAttr) ((yyvsp[-1].int32)); }
#line 5018 "asmparse.cpp"
    break;

  case 209: /* atOpt: %empty  */
#line 614 "asmparse.y"
                                                            { (yyval.string) = 0; }
#line 5024 "asmparse.cpp"
    break;

  case 210: /* atOpt: AT_ id  */
#line 615 "asmparse.y"
                                                            { (yyval.string) = (yyvsp[0].string); }
#line 5030 "asmparse.cpp"
    break;

  case 211: /* initOpt: %empty  */
#line 618 "asmparse.y"
                                                            { (yyval.binstr) = NULL; }
#line 5036 "asmparse.cpp"
    break;

  case 212: /* initOpt: '=' fieldInit  */
#line 619 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 5042 "asmparse.cpp"
    break;

  case 213: /* repeatOpt: %empty  */
#line 622 "asmparse.y"
                                                            { (yyval.int32) = 0xFFFFFFFF; }
#line 5048 "asmparse.cpp"
    break;

  case 214: /* repeatOpt: '[' int32 ']'  */
#line 623 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32); }
#line 5054 "asmparse.cpp"
    break;

  case 215: /* methodRef: callConv type typeSpec DCOLON methodName tyArgs0 '(' sigArgs0 ')'  */
#line 628 "asmparse.y"
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
#line 5075 "asmparse.cpp"
    break;

  case 216: /* methodRef: callConv type typeSpec DCOLON methodName genArityNotEmpty '(' sigArgs0 ')'  */
#line 645 "asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(((yyvsp[-8].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               (yyval.token) = PASM->MakeMemberRef((yyvsp[-6].token), (yyvsp[-4].string),
                                                                 parser->MakeSig((yyvsp[-8].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-7].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32)));
                                                             }
#line 5085 "asmparse.cpp"
    break;

  case 217: /* methodRef: callConv type methodName tyArgs0 '(' sigArgs0 ')'  */
#line 651 "asmparse.y"
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
#line 5105 "asmparse.cpp"
    break;

  case 218: /* methodRef: callConv type methodName genArityNotEmpty '(' sigArgs0 ')'  */
#line 667 "asmparse.y"
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(((yyvsp[-6].int32) & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               (yyval.token) = PASM->MakeMemberRef(mdTokenNil, (yyvsp[-4].string), parser->MakeSig((yyvsp[-6].int32) | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, (yyvsp[-5].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32)));
                                                             }
#line 5114 "asmparse.cpp"
    break;

  case 219: /* methodRef: mdtoken  */
#line 671 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token); }
#line 5120 "asmparse.cpp"
    break;

  case 220: /* methodRef: TYPEDEF_M  */
#line 672 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 5126 "asmparse.cpp"
    break;

  case 221: /* methodRef: TYPEDEF_MR  */
#line 673 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 5132 "asmparse.cpp"
    break;

  case 222: /* callConv: INSTANCE_ callConv  */
#line 676 "asmparse.y"
                                                              { (yyval.int32) = ((yyvsp[0].int32) | IMAGE_CEE_CS_CALLCONV_HASTHIS); }
#line 5138 "asmparse.cpp"
    break;

  case 223: /* callConv: EXPLICIT_ callConv  */
#line 677 "asmparse.y"
                                                              { (yyval.int32) = ((yyvsp[0].int32) | IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS); }
#line 5144 "asmparse.cpp"
    break;

  case 224: /* callConv: callKind  */
#line 678 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 5150 "asmparse.cpp"
    break;

  case 225: /* callConv: CALLCONV_ '(' int32 ')'  */
#line 679 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32); }
#line 5156 "asmparse.cpp"
    break;

  case 226: /* callKind: %empty  */
#line 682 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
#line 5162 "asmparse.cpp"
    break;

  case 227: /* callKind: DEFAULT_  */
#line 683 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
#line 5168 "asmparse.cpp"
    break;

  case 228: /* callKind: VARARG_  */
#line 684 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_VARARG; }
#line 5174 "asmparse.cpp"
    break;

  case 229: /* callKind: UNMANAGED_ CDECL_  */
#line 685 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_C; }
#line 5180 "asmparse.cpp"
    break;

  case 230: /* callKind: UNMANAGED_ STDCALL_  */
#line 686 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_STDCALL; }
#line 5186 "asmparse.cpp"
    break;

  case 231: /* callKind: UNMANAGED_ THISCALL_  */
#line 687 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_THISCALL; }
#line 5192 "asmparse.cpp"
    break;

  case 232: /* callKind: UNMANAGED_ FASTCALL_  */
#line 688 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_FASTCALL; }
#line 5198 "asmparse.cpp"
    break;

  case 233: /* callKind: UNMANAGED_  */
#line 689 "asmparse.y"
                                                              { (yyval.int32) = IMAGE_CEE_CS_CALLCONV_UNMANAGED; }
#line 5204 "asmparse.cpp"
    break;

  case 234: /* mdtoken: MDTOKEN_ '(' int32 ')'  */
#line 692 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[-1].int32); }
#line 5210 "asmparse.cpp"
    break;

  case 235: /* memberRef: methodSpec methodRef  */
#line 695 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token);
                                                               PASM->delArgNameList(PASM->m_firstArgName);
                                                               PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                               PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                               PASM->SetMemberRefFixup((yyvsp[0].token),iOpcodeLen); }
#line 5220 "asmparse.cpp"
    break;

  case 236: /* memberRef: FIELD_ type typeSpec DCOLON dottedName  */
#line 701 "asmparse.y"
                                                             { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               (yyval.token) = PASM->MakeMemberRef((yyvsp[-2].token), (yyvsp[0].string), (yyvsp[-3].binstr));
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 5228 "asmparse.cpp"
    break;

  case 237: /* memberRef: FIELD_ type dottedName  */
#line 705 "asmparse.y"
                                                             { (yyvsp[-1].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               (yyval.token) = PASM->MakeMemberRef(NULL, (yyvsp[0].string), (yyvsp[-1].binstr));
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 5236 "asmparse.cpp"
    break;

  case 238: /* memberRef: FIELD_ TYPEDEF_F  */
#line 708 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 5243 "asmparse.cpp"
    break;

  case 239: /* memberRef: FIELD_ TYPEDEF_MR  */
#line 710 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 5250 "asmparse.cpp"
    break;

  case 240: /* memberRef: mdtoken  */
#line 712 "asmparse.y"
                                                             { (yyval.token) = (yyvsp[0].token);
                                                               PASM->SetMemberRefFixup((yyval.token),iOpcodeLen); }
#line 5257 "asmparse.cpp"
    break;

  case 241: /* eventHead: _EVENT eventAttr typeSpec dottedName  */
#line 717 "asmparse.y"
                                                                 { PASM->ResetEvent((yyvsp[0].string), (yyvsp[-1].token), (yyvsp[-2].eventAttr)); }
#line 5263 "asmparse.cpp"
    break;

  case 242: /* eventHead: _EVENT eventAttr dottedName  */
#line 718 "asmparse.y"
                                                                 { PASM->ResetEvent((yyvsp[0].string), mdTypeRefNil, (yyvsp[-1].eventAttr)); }
#line 5269 "asmparse.cpp"
    break;

  case 243: /* eventAttr: %empty  */
#line 722 "asmparse.y"
                                                            { (yyval.eventAttr) = (CorEventAttr) 0; }
#line 5275 "asmparse.cpp"
    break;

  case 244: /* eventAttr: eventAttr RTSPECIALNAME_  */
#line 723 "asmparse.y"
                                                            { (yyval.eventAttr) = (yyvsp[-1].eventAttr); }
#line 5281 "asmparse.cpp"
    break;

  case 245: /* eventAttr: eventAttr SPECIALNAME_  */
#line 724 "asmparse.y"
                                                            { (yyval.eventAttr) = (CorEventAttr) ((yyvsp[-1].eventAttr) | evSpecialName); }
#line 5287 "asmparse.cpp"
    break;

  case 248: /* eventDecl: _ADDON methodRef  */
#line 731 "asmparse.y"
                                                           { PASM->SetEventMethod(0, (yyvsp[0].token)); }
#line 5293 "asmparse.cpp"
    break;

  case 249: /* eventDecl: _REMOVEON methodRef  */
#line 732 "asmparse.y"
                                                           { PASM->SetEventMethod(1, (yyvsp[0].token)); }
#line 5299 "asmparse.cpp"
    break;

  case 250: /* eventDecl: _FIRE methodRef  */
#line 733 "asmparse.y"
                                                           { PASM->SetEventMethod(2, (yyvsp[0].token)); }
#line 5305 "asmparse.cpp"
    break;

  case 251: /* eventDecl: _OTHER methodRef  */
#line 734 "asmparse.y"
                                                           { PASM->SetEventMethod(3, (yyvsp[0].token)); }
#line 5311 "asmparse.cpp"
    break;

  case 256: /* propHead: _PROPERTY propAttr callConv type dottedName '(' sigArgs0 ')' initOpt  */
#line 743 "asmparse.y"
                                                            { PASM->ResetProp((yyvsp[-4].string),
                                                              parser->MakeSig((IMAGE_CEE_CS_CALLCONV_PROPERTY |
                                                              ((yyvsp[-6].int32) & IMAGE_CEE_CS_CALLCONV_HASTHIS)),(yyvsp[-5].binstr),(yyvsp[-2].binstr)), (yyvsp[-7].propAttr), (yyvsp[0].binstr));}
#line 5319 "asmparse.cpp"
    break;

  case 257: /* propAttr: %empty  */
#line 748 "asmparse.y"
                                                            { (yyval.propAttr) = (CorPropertyAttr) 0; }
#line 5325 "asmparse.cpp"
    break;

  case 258: /* propAttr: propAttr RTSPECIALNAME_  */
#line 749 "asmparse.y"
                                                            { (yyval.propAttr) = (yyvsp[-1].propAttr); }
#line 5331 "asmparse.cpp"
    break;

  case 259: /* propAttr: propAttr SPECIALNAME_  */
#line 750 "asmparse.y"
                                                            { (yyval.propAttr) = (CorPropertyAttr) ((yyvsp[-1].propAttr) | prSpecialName); }
#line 5337 "asmparse.cpp"
    break;

  case 262: /* propDecl: _SET methodRef  */
#line 758 "asmparse.y"
                                                            { PASM->SetPropMethod(0, (yyvsp[0].token)); }
#line 5343 "asmparse.cpp"
    break;

  case 263: /* propDecl: _GET methodRef  */
#line 759 "asmparse.y"
                                                            { PASM->SetPropMethod(1, (yyvsp[0].token)); }
#line 5349 "asmparse.cpp"
    break;

  case 264: /* propDecl: _OTHER methodRef  */
#line 760 "asmparse.y"
                                                            { PASM->SetPropMethod(2, (yyvsp[0].token)); }
#line 5355 "asmparse.cpp"
    break;

  case 269: /* methodHeadPart1: _METHOD  */
#line 768 "asmparse.y"
                                                            { PASM->ResetForNextMethod();
                                                              uMethodBeginLine = PASM->m_ulCurLine;
                                                              uMethodBeginColumn=PASM->m_ulCurColumn;
                                                            }
#line 5364 "asmparse.cpp"
    break;

  case 270: /* marshalClause: %empty  */
#line 774 "asmparse.y"
                                                            { (yyval.binstr) = NULL; }
#line 5370 "asmparse.cpp"
    break;

  case 271: /* marshalClause: MARSHAL_ '(' marshalBlob ')'  */
#line 775 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 5376 "asmparse.cpp"
    break;

  case 272: /* marshalBlob: nativeType  */
#line 778 "asmparse.y"
                                                            { (yyval.binstr) = (yyvsp[0].binstr); }
#line 5382 "asmparse.cpp"
    break;

  case 273: /* marshalBlob: marshalBlobHead hexbytes '}'  */
#line 779 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 5388 "asmparse.cpp"
    break;

  case 274: /* marshalBlobHead: '{'  */
#line 782 "asmparse.y"
                                                             { bParsingByteArray = TRUE; }
#line 5394 "asmparse.cpp"
    break;

  case 275: /* methodHead: methodHeadPart1 methAttr callConv paramAttr type marshalClause methodName typarsClause '(' sigArgs0 ')' implAttr '{'  */
#line 786 "asmparse.y"
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
#line 5412 "asmparse.cpp"
    break;

  case 276: /* methAttr: %empty  */
#line 801 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) 0; }
#line 5418 "asmparse.cpp"
    break;

  case 277: /* methAttr: methAttr STATIC_  */
#line 802 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdStatic); }
#line 5424 "asmparse.cpp"
    break;

  case 278: /* methAttr: methAttr PUBLIC_  */
#line 803 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPublic); }
#line 5430 "asmparse.cpp"
    break;

  case 279: /* methAttr: methAttr PRIVATE_  */
#line 804 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPrivate); }
#line 5436 "asmparse.cpp"
    break;

  case 280: /* methAttr: methAttr FAMILY_  */
#line 805 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamily); }
#line 5442 "asmparse.cpp"
    break;

  case 281: /* methAttr: methAttr FINAL_  */
#line 806 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdFinal); }
#line 5448 "asmparse.cpp"
    break;

  case 282: /* methAttr: methAttr SPECIALNAME_  */
#line 807 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdSpecialName); }
#line 5454 "asmparse.cpp"
    break;

  case 283: /* methAttr: methAttr VIRTUAL_  */
#line 808 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdVirtual); }
#line 5460 "asmparse.cpp"
    break;

  case 284: /* methAttr: methAttr STRICT_  */
#line 809 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdCheckAccessOnOverride); }
#line 5466 "asmparse.cpp"
    break;

  case 285: /* methAttr: methAttr ABSTRACT_  */
#line 810 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdAbstract); }
#line 5472 "asmparse.cpp"
    break;

  case 286: /* methAttr: methAttr ASSEMBLY_  */
#line 811 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdAssem); }
#line 5478 "asmparse.cpp"
    break;

  case 287: /* methAttr: methAttr FAMANDASSEM_  */
#line 812 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamANDAssem); }
#line 5484 "asmparse.cpp"
    break;

  case 288: /* methAttr: methAttr FAMORASSEM_  */
#line 813 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdFamORAssem); }
#line 5490 "asmparse.cpp"
    break;

  case 289: /* methAttr: methAttr PRIVATESCOPE_  */
#line 814 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) (((yyvsp[-1].methAttr) & ~mdMemberAccessMask) | mdPrivateScope); }
#line 5496 "asmparse.cpp"
    break;

  case 290: /* methAttr: methAttr HIDEBYSIG_  */
#line 815 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdHideBySig); }
#line 5502 "asmparse.cpp"
    break;

  case 291: /* methAttr: methAttr NEWSLOT_  */
#line 816 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdNewSlot); }
#line 5508 "asmparse.cpp"
    break;

  case 292: /* methAttr: methAttr RTSPECIALNAME_  */
#line 817 "asmparse.y"
                                                            { (yyval.methAttr) = (yyvsp[-1].methAttr); }
#line 5514 "asmparse.cpp"
    break;

  case 293: /* methAttr: methAttr UNMANAGEDEXP_  */
#line 818 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdUnmanagedExport); }
#line 5520 "asmparse.cpp"
    break;

  case 294: /* methAttr: methAttr REQSECOBJ_  */
#line 819 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].methAttr) | mdRequireSecObject); }
#line 5526 "asmparse.cpp"
    break;

  case 295: /* methAttr: methAttr FLAGS_ '(' int32 ')'  */
#line 820 "asmparse.y"
                                                            { (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-1].int32)); }
#line 5532 "asmparse.cpp"
    break;

  case 296: /* methAttr: methAttr PINVOKEIMPL_ '(' compQstring AS_ compQstring pinvAttr ')'  */
#line 822 "asmparse.y"
                                                            { PASM->SetPinvoke((yyvsp[-4].binstr),0,(yyvsp[-2].binstr),(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-7].methAttr) | mdPinvokeImpl); }
#line 5539 "asmparse.cpp"
    break;

  case 297: /* methAttr: methAttr PINVOKEIMPL_ '(' compQstring pinvAttr ')'  */
#line 825 "asmparse.y"
                                                            { PASM->SetPinvoke((yyvsp[-2].binstr),0,NULL,(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-5].methAttr) | mdPinvokeImpl); }
#line 5546 "asmparse.cpp"
    break;

  case 298: /* methAttr: methAttr PINVOKEIMPL_ '(' pinvAttr ')'  */
#line 828 "asmparse.y"
                                                            { PASM->SetPinvoke(new BinStr(),0,NULL,(yyvsp[-1].pinvAttr));
                                                              (yyval.methAttr) = (CorMethodAttr) ((yyvsp[-4].methAttr) | mdPinvokeImpl); }
#line 5553 "asmparse.cpp"
    break;

  case 299: /* pinvAttr: %empty  */
#line 832 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) 0; }
#line 5559 "asmparse.cpp"
    break;

  case 300: /* pinvAttr: pinvAttr NOMANGLE_  */
#line 833 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmNoMangle); }
#line 5565 "asmparse.cpp"
    break;

  case 301: /* pinvAttr: pinvAttr ANSI_  */
#line 834 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetAnsi); }
#line 5571 "asmparse.cpp"
    break;

  case 302: /* pinvAttr: pinvAttr UNICODE_  */
#line 835 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetUnicode); }
#line 5577 "asmparse.cpp"
    break;

  case 303: /* pinvAttr: pinvAttr AUTOCHAR_  */
#line 836 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCharSetAuto); }
#line 5583 "asmparse.cpp"
    break;

  case 304: /* pinvAttr: pinvAttr LASTERR_  */
#line 837 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmSupportsLastError); }
#line 5589 "asmparse.cpp"
    break;

  case 305: /* pinvAttr: pinvAttr WINAPI_  */
#line 838 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvWinapi); }
#line 5595 "asmparse.cpp"
    break;

  case 306: /* pinvAttr: pinvAttr CDECL_  */
#line 839 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvCdecl); }
#line 5601 "asmparse.cpp"
    break;

  case 307: /* pinvAttr: pinvAttr STDCALL_  */
#line 840 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvStdcall); }
#line 5607 "asmparse.cpp"
    break;

  case 308: /* pinvAttr: pinvAttr THISCALL_  */
#line 841 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvThiscall); }
#line 5613 "asmparse.cpp"
    break;

  case 309: /* pinvAttr: pinvAttr FASTCALL_  */
#line 842 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].pinvAttr) | pmCallConvFastcall); }
#line 5619 "asmparse.cpp"
    break;

  case 310: /* pinvAttr: pinvAttr BESTFIT_ ':' ON_  */
#line 843 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmBestFitEnabled); }
#line 5625 "asmparse.cpp"
    break;

  case 311: /* pinvAttr: pinvAttr BESTFIT_ ':' OFF_  */
#line 844 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmBestFitDisabled); }
#line 5631 "asmparse.cpp"
    break;

  case 312: /* pinvAttr: pinvAttr CHARMAPERROR_ ':' ON_  */
#line 845 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmThrowOnUnmappableCharEnabled); }
#line 5637 "asmparse.cpp"
    break;

  case 313: /* pinvAttr: pinvAttr CHARMAPERROR_ ':' OFF_  */
#line 846 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-3].pinvAttr) | pmThrowOnUnmappableCharDisabled); }
#line 5643 "asmparse.cpp"
    break;

  case 314: /* pinvAttr: pinvAttr FLAGS_ '(' int32 ')'  */
#line 847 "asmparse.y"
                                                            { (yyval.pinvAttr) = (CorPinvokeMap) ((yyvsp[-1].int32)); }
#line 5649 "asmparse.cpp"
    break;

  case 315: /* methodName: _CTOR  */
#line 850 "asmparse.y"
                                                            { (yyval.string) = newString(COR_CTOR_METHOD_NAME); }
#line 5655 "asmparse.cpp"
    break;

  case 316: /* methodName: _CCTOR  */
#line 851 "asmparse.y"
                                                            { (yyval.string) = newString(COR_CCTOR_METHOD_NAME); }
#line 5661 "asmparse.cpp"
    break;

  case 317: /* methodName: dottedName  */
#line 852 "asmparse.y"
                                                            { (yyval.string) = (yyvsp[0].string); }
#line 5667 "asmparse.cpp"
    break;

  case 318: /* paramAttr: %empty  */
#line 855 "asmparse.y"
                                                            { (yyval.int32) = 0; }
#line 5673 "asmparse.cpp"
    break;

  case 319: /* paramAttr: paramAttr '[' IN_ ']'  */
#line 856 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdIn; }
#line 5679 "asmparse.cpp"
    break;

  case 320: /* paramAttr: paramAttr '[' OUT_ ']'  */
#line 857 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdOut; }
#line 5685 "asmparse.cpp"
    break;

  case 321: /* paramAttr: paramAttr '[' OPT_ ']'  */
#line 858 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-3].int32) | pdOptional; }
#line 5691 "asmparse.cpp"
    break;

  case 322: /* paramAttr: paramAttr '[' int32 ']'  */
#line 859 "asmparse.y"
                                                            { (yyval.int32) = (yyvsp[-1].int32) + 1; }
#line 5697 "asmparse.cpp"
    break;

  case 323: /* implAttr: %empty  */
#line 862 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (miIL | miManaged); }
#line 5703 "asmparse.cpp"
    break;

  case 324: /* implAttr: implAttr NATIVE_  */
#line 863 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miNative); }
#line 5709 "asmparse.cpp"
    break;

  case 325: /* implAttr: implAttr CIL_  */
#line 864 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miIL); }
#line 5715 "asmparse.cpp"
    break;

  case 326: /* implAttr: implAttr OPTIL_  */
#line 865 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFF4) | miOPTIL); }
#line 5721 "asmparse.cpp"
    break;

  case 327: /* implAttr: implAttr MANAGED_  */
#line 866 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFFB) | miManaged); }
#line 5727 "asmparse.cpp"
    break;

  case 328: /* implAttr: implAttr UNMANAGED_  */
#line 867 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) (((yyvsp[-1].implAttr) & 0xFFFB) | miUnmanaged); }
#line 5733 "asmparse.cpp"
    break;

  case 329: /* implAttr: implAttr FORWARDREF_  */
#line 868 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miForwardRef); }
#line 5739 "asmparse.cpp"
    break;

  case 330: /* implAttr: implAttr PRESERVESIG_  */
#line 869 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miPreserveSig); }
#line 5745 "asmparse.cpp"
    break;

  case 331: /* implAttr: implAttr RUNTIME_  */
#line 870 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miRuntime); }
#line 5751 "asmparse.cpp"
    break;

  case 332: /* implAttr: implAttr INTERNALCALL_  */
#line 871 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miInternalCall); }
#line 5757 "asmparse.cpp"
    break;

  case 333: /* implAttr: implAttr SYNCHRONIZED_  */
#line 872 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miSynchronized); }
#line 5763 "asmparse.cpp"
    break;

  case 334: /* implAttr: implAttr NOINLINING_  */
#line 873 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miNoInlining); }
#line 5769 "asmparse.cpp"
    break;

  case 335: /* implAttr: implAttr AGGRESSIVEINLINING_  */
#line 874 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAggressiveInlining); }
#line 5775 "asmparse.cpp"
    break;

  case 336: /* implAttr: implAttr NOOPTIMIZATION_  */
#line 875 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miNoOptimization); }
#line 5781 "asmparse.cpp"
    break;

  case 337: /* implAttr: implAttr AGGRESSIVEOPTIMIZATION_  */
#line 876 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].implAttr) | miAggressiveOptimization); }
#line 5787 "asmparse.cpp"
    break;

  case 338: /* implAttr: implAttr FLAGS_ '(' int32 ')'  */
#line 877 "asmparse.y"
                                                            { (yyval.implAttr) = (CorMethodImpl) ((yyvsp[-1].int32)); }
#line 5793 "asmparse.cpp"
    break;

  case 339: /* localsHead: _LOCALS  */
#line 880 "asmparse.y"
                                                            { PASM->delArgNameList(PASM->m_firstArgName); PASM->m_firstArgName = NULL;PASM->m_lastArgName = NULL;
                                                            }
#line 5800 "asmparse.cpp"
    break;

  case 342: /* methodDecl: _EMITBYTE int32  */
#line 888 "asmparse.y"
                                                            { PASM->EmitByte((yyvsp[0].int32)); }
#line 5806 "asmparse.cpp"
    break;

  case 343: /* methodDecl: sehBlock  */
#line 889 "asmparse.y"
                                                            { delete PASM->m_SEHD; PASM->m_SEHD = PASM->m_SEHDstack.POP(); }
#line 5812 "asmparse.cpp"
    break;

  case 344: /* methodDecl: _MAXSTACK int32  */
#line 890 "asmparse.y"
                                                            { PASM->EmitMaxStack((yyvsp[0].int32)); }
#line 5818 "asmparse.cpp"
    break;

  case 345: /* methodDecl: localsHead '(' sigArgs0 ')'  */
#line 891 "asmparse.y"
                                                            { PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, (yyvsp[-1].binstr)));
                                                            }
#line 5825 "asmparse.cpp"
    break;

  case 346: /* methodDecl: localsHead INIT_ '(' sigArgs0 ')'  */
#line 893 "asmparse.y"
                                                            { PASM->EmitZeroInit();
                                                              PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, (yyvsp[-1].binstr)));
                                                            }
#line 5833 "asmparse.cpp"
    break;

  case 347: /* methodDecl: _ENTRYPOINT  */
#line 896 "asmparse.y"
                                                            { PASM->EmitEntryPoint(); }
#line 5839 "asmparse.cpp"
    break;

  case 348: /* methodDecl: _ZEROINIT  */
#line 897 "asmparse.y"
                                                            { PASM->EmitZeroInit(); }
#line 5845 "asmparse.cpp"
    break;

  case 351: /* methodDecl: id ':'  */
#line 900 "asmparse.y"
                                                            { PASM->AddLabel(PASM->m_CurPC,(yyvsp[-1].string)); /*PASM->EmitLabel($1);*/ }
#line 5851 "asmparse.cpp"
    break;

  case 357: /* methodDecl: _EXPORT '[' int32 ']'  */
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
#line 5866 "asmparse.cpp"
    break;

  case 358: /* methodDecl: _EXPORT '[' int32 ']' AS_ id  */
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
#line 5881 "asmparse.cpp"
    break;

  case 359: /* methodDecl: _VTENTRY int32 ':' int32  */
#line 926 "asmparse.y"
                                                            { PASM->m_pCurMethod->m_wVTEntry = (WORD)(yyvsp[-2].int32);
                                                              PASM->m_pCurMethod->m_wVTSlot = (WORD)(yyvsp[0].int32); }
#line 5888 "asmparse.cpp"
    break;

  case 360: /* methodDecl: _OVERRIDE typeSpec DCOLON methodName  */
#line 929 "asmparse.y"
                                                            { PASM->AddMethodImpl((yyvsp[-2].token),(yyvsp[0].string),NULL,NULL,NULL,NULL); }
#line 5894 "asmparse.cpp"
    break;

  case 361: /* methodDecl: _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'  */
#line 932 "asmparse.y"
                                                            { PASM->AddMethodImpl((yyvsp[-6].token),(yyvsp[-4].string),
                                                              ((yyvsp[-3].int32)==0 ? parser->MakeSig((yyvsp[-8].int32),(yyvsp[-7].binstr),(yyvsp[-1].binstr)) :
                                                              parser->MakeSig((yyvsp[-8].int32)| IMAGE_CEE_CS_CALLCONV_GENERIC,(yyvsp[-7].binstr),(yyvsp[-1].binstr),(yyvsp[-3].int32)))
                                                              ,NULL,NULL,NULL);
                                                              PASM->ResetArgNameList();
                                                            }
#line 5905 "asmparse.cpp"
    break;

  case 363: /* methodDecl: _PARAM TYPE_ '[' int32 ']'  */
#line 939 "asmparse.y"
                                                            { if(((yyvsp[-1].int32) > 0) && ((yyvsp[-1].int32) <= (int)PASM->m_pCurMethod->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[(yyvsp[-1].int32)-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
#line 5915 "asmparse.cpp"
    break;

  case 364: /* methodDecl: _PARAM TYPE_ dottedName  */
#line 944 "asmparse.y"
                                                            { int n = PASM->m_pCurMethod->FindTyPar((yyvsp[0].string));
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",(yyvsp[0].string));
                                                            }
#line 5926 "asmparse.cpp"
    break;

  case 365: /* methodDecl: _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec  */
#line 950 "asmparse.y"
                                                                        { PASM->m_pCurMethod->AddGenericParamConstraint((yyvsp[-3].int32), 0, (yyvsp[0].token)); }
#line 5932 "asmparse.cpp"
    break;

  case 366: /* methodDecl: _PARAM CONSTRAINT_ dottedName ',' typeSpec  */
#line 951 "asmparse.y"
                                                                        { PASM->m_pCurMethod->AddGenericParamConstraint(0, (yyvsp[-2].string), (yyvsp[0].token)); }
#line 5938 "asmparse.cpp"
    break;

  case 367: /* methodDecl: _PARAM '[' int32 ']' initOpt  */
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
#line 5961 "asmparse.cpp"
    break;

  case 368: /* scopeBlock: scopeOpen methodDecls '}'  */
#line 974 "asmparse.y"
                                                            { PASM->m_pCurMethod->CloseScope(); }
#line 5967 "asmparse.cpp"
    break;

  case 369: /* scopeOpen: '{'  */
#line 977 "asmparse.y"
                                                            { PASM->m_pCurMethod->OpenScope(); }
#line 5973 "asmparse.cpp"
    break;

  case 373: /* tryBlock: tryHead scopeBlock  */
#line 988 "asmparse.y"
                                                            { PASM->m_SEHD->tryTo = PASM->m_CurPC; }
#line 5979 "asmparse.cpp"
    break;

  case 374: /* tryBlock: tryHead id TO_ id  */
#line 989 "asmparse.y"
                                                            { PASM->SetTryLabels((yyvsp[-2].string), (yyvsp[0].string)); }
#line 5985 "asmparse.cpp"
    break;

  case 375: /* tryBlock: tryHead int32 TO_ int32  */
#line 990 "asmparse.y"
                                                            { if(PASM->m_SEHD) {PASM->m_SEHD->tryFrom = (yyvsp[-2].int32);
                                                              PASM->m_SEHD->tryTo = (yyvsp[0].int32);} }
#line 5992 "asmparse.cpp"
    break;

  case 376: /* tryHead: _TRY  */
#line 994 "asmparse.y"
                                                            { PASM->NewSEHDescriptor();
                                                              PASM->m_SEHD->tryFrom = PASM->m_CurPC; }
#line 5999 "asmparse.cpp"
    break;

  case 377: /* sehClause: catchClause handlerBlock  */
#line 999 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 6005 "asmparse.cpp"
    break;

  case 378: /* sehClause: filterClause handlerBlock  */
#line 1000 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 6011 "asmparse.cpp"
    break;

  case 379: /* sehClause: finallyClause handlerBlock  */
#line 1001 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 6017 "asmparse.cpp"
    break;

  case 380: /* sehClause: faultClause handlerBlock  */
#line 1002 "asmparse.y"
                                                             { PASM->EmitTry(); }
#line 6023 "asmparse.cpp"
    break;

  case 381: /* filterClause: filterHead scopeBlock  */
#line 1006 "asmparse.y"
                                                             { PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6029 "asmparse.cpp"
    break;

  case 382: /* filterClause: filterHead id  */
#line 1007 "asmparse.y"
                                                             { PASM->SetFilterLabel((yyvsp[0].string));
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6036 "asmparse.cpp"
    break;

  case 383: /* filterClause: filterHead int32  */
#line 1009 "asmparse.y"
                                                             { PASM->m_SEHD->sehFilter = (yyvsp[0].int32);
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6043 "asmparse.cpp"
    break;

  case 384: /* filterHead: FILTER_  */
#line 1013 "asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FILTER;
                                                               PASM->m_SEHD->sehFilter = PASM->m_CurPC; }
#line 6050 "asmparse.cpp"
    break;

  case 385: /* catchClause: CATCH_ typeSpec  */
#line 1017 "asmparse.y"
                                                            {  PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_NONE;
                                                               PASM->SetCatchClass((yyvsp[0].token));
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6058 "asmparse.cpp"
    break;

  case 386: /* finallyClause: FINALLY_  */
#line 1022 "asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FINALLY;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6065 "asmparse.cpp"
    break;

  case 387: /* faultClause: FAULT_  */
#line 1026 "asmparse.y"
                                                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FAULT;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
#line 6072 "asmparse.cpp"
    break;

  case 388: /* handlerBlock: scopeBlock  */
#line 1030 "asmparse.y"
                                                             { PASM->m_SEHD->sehHandlerTo = PASM->m_CurPC; }
#line 6078 "asmparse.cpp"
    break;

  case 389: /* handlerBlock: HANDLER_ id TO_ id  */
#line 1031 "asmparse.y"
                                                             { PASM->SetHandlerLabels((yyvsp[-2].string), (yyvsp[0].string)); }
#line 6084 "asmparse.cpp"
    break;

  case 390: /* handlerBlock: HANDLER_ int32 TO_ int32  */
#line 1032 "asmparse.y"
                                                             { PASM->m_SEHD->sehHandler = (yyvsp[-2].int32);
                                                               PASM->m_SEHD->sehHandlerTo = (yyvsp[0].int32); }
#line 6091 "asmparse.cpp"
    break;

  case 392: /* ddHead: _DATA tls id '='  */
#line 1040 "asmparse.y"
                                                             { PASM->EmitDataLabel((yyvsp[-1].string)); }
#line 6097 "asmparse.cpp"
    break;

  case 394: /* tls: %empty  */
#line 1044 "asmparse.y"
                                                             { PASM->SetDataSection(); }
#line 6103 "asmparse.cpp"
    break;

  case 395: /* tls: TLS_  */
#line 1045 "asmparse.y"
                                                             { PASM->SetTLSSection(); }
#line 6109 "asmparse.cpp"
    break;

  case 396: /* tls: CIL_  */
#line 1046 "asmparse.y"
                                                             { PASM->SetILSection(); }
#line 6115 "asmparse.cpp"
    break;

  case 401: /* ddItemCount: %empty  */
#line 1057 "asmparse.y"
                                                             { (yyval.int32) = 1; }
#line 6121 "asmparse.cpp"
    break;

  case 402: /* ddItemCount: '[' int32 ']'  */
#line 1058 "asmparse.y"
                                                             { (yyval.int32) = (yyvsp[-1].int32);
                                                               if((yyvsp[-1].int32) <= 0) { PASM->report->error("Illegal item count: %d\n",(yyvsp[-1].int32));
                                                                  if(!PASM->OnErrGo) (yyval.int32) = 1; }}
#line 6129 "asmparse.cpp"
    break;

  case 403: /* ddItem: CHAR_ '*' '(' compQstring ')'  */
#line 1063 "asmparse.y"
                                                             { PASM->EmitDataString((yyvsp[-1].binstr)); }
#line 6135 "asmparse.cpp"
    break;

  case 404: /* ddItem: '&' '(' id ')'  */
#line 1064 "asmparse.y"
                                                             { PASM->EmitDD((yyvsp[-1].string)); }
#line 6141 "asmparse.cpp"
    break;

  case 405: /* ddItem: bytearrayhead bytes ')'  */
#line 1065 "asmparse.y"
                                                             { PASM->EmitData((yyvsp[-1].binstr)->ptr(),(yyvsp[-1].binstr)->length()); }
#line 6147 "asmparse.cpp"
    break;

  case 406: /* ddItem: FLOAT32_ '(' float64 ')' ddItemCount  */
#line 1067 "asmparse.y"
                                                             { float f = (float) (*(yyvsp[-2].float64)); float* p = new (nothrow) float[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i < (yyvsp[0].int32); i++) p[i] = f;
                                                                 PASM->EmitData(p, sizeof(float)*(yyvsp[0].int32)); delete (yyvsp[-2].float64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(float)*(yyvsp[0].int32)); }
#line 6158 "asmparse.cpp"
    break;

  case 407: /* ddItem: FLOAT64_ '(' float64 ')' ddItemCount  */
#line 1074 "asmparse.y"
                                                             { double* p = new (nothrow) double[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = *((yyvsp[-2].float64));
                                                                 PASM->EmitData(p, sizeof(double)*(yyvsp[0].int32)); delete (yyvsp[-2].float64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(double)*(yyvsp[0].int32)); }
#line 6169 "asmparse.cpp"
    break;

  case 408: /* ddItem: INT64_ '(' int64 ')' ddItemCount  */
#line 1081 "asmparse.y"
                                                             { __int64* p = new (nothrow) __int64[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = *((yyvsp[-2].int64));
                                                                 PASM->EmitData(p, sizeof(__int64)*(yyvsp[0].int32)); delete (yyvsp[-2].int64); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(__int64)*(yyvsp[0].int32)); }
#line 6180 "asmparse.cpp"
    break;

  case 409: /* ddItem: INT32_ '(' int32 ')' ddItemCount  */
#line 1088 "asmparse.y"
                                                             { __int32* p = new (nothrow) __int32[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<(yyvsp[0].int32); i++) p[i] = (yyvsp[-2].int32);
                                                                 PASM->EmitData(p, sizeof(__int32)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(__int32)*(yyvsp[0].int32)); }
#line 6191 "asmparse.cpp"
    break;

  case 410: /* ddItem: INT16_ '(' int32 ')' ddItemCount  */
#line 1095 "asmparse.y"
                                                             { __int16 i = (__int16) (yyvsp[-2].int32); FAIL_UNLESS(i == (yyvsp[-2].int32), ("Value %d too big\n", (yyvsp[-2].int32)));
                                                               __int16* p = new (nothrow) __int16[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<(yyvsp[0].int32); j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(__int16)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(__int16)*(yyvsp[0].int32)); }
#line 6203 "asmparse.cpp"
    break;

  case 411: /* ddItem: INT8_ '(' int32 ')' ddItemCount  */
#line 1103 "asmparse.y"
                                                             { __int8 i = (__int8) (yyvsp[-2].int32); FAIL_UNLESS(i == (yyvsp[-2].int32), ("Value %d too big\n", (yyvsp[-2].int32)));
                                                               __int8* p = new (nothrow) __int8[(yyvsp[0].int32)];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<(yyvsp[0].int32); j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(__int8)*(yyvsp[0].int32)); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(__int8)*(yyvsp[0].int32)); }
#line 6215 "asmparse.cpp"
    break;

  case 412: /* ddItem: FLOAT32_ ddItemCount  */
#line 1110 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(float)*(yyvsp[0].int32)); }
#line 6221 "asmparse.cpp"
    break;

  case 413: /* ddItem: FLOAT64_ ddItemCount  */
#line 1111 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(double)*(yyvsp[0].int32)); }
#line 6227 "asmparse.cpp"
    break;

  case 414: /* ddItem: INT64_ ddItemCount  */
#line 1112 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(__int64)*(yyvsp[0].int32)); }
#line 6233 "asmparse.cpp"
    break;

  case 415: /* ddItem: INT32_ ddItemCount  */
#line 1113 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(__int32)*(yyvsp[0].int32)); }
#line 6239 "asmparse.cpp"
    break;

  case 416: /* ddItem: INT16_ ddItemCount  */
#line 1114 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(__int16)*(yyvsp[0].int32)); }
#line 6245 "asmparse.cpp"
    break;

  case 417: /* ddItem: INT8_ ddItemCount  */
#line 1115 "asmparse.y"
                                                             { PASM->EmitData(NULL, sizeof(__int8)*(yyvsp[0].int32)); }
#line 6251 "asmparse.cpp"
    break;

  case 418: /* fieldSerInit: FLOAT32_ '(' float64 ')'  */
#line 1119 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               float f = (float)(*(yyvsp[-1].float64));
                                                               (yyval.binstr)->appendInt32(*((__int32*)&f)); delete (yyvsp[-1].float64); }
#line 6259 "asmparse.cpp"
    break;

  case 419: /* fieldSerInit: FLOAT64_ '(' float64 ')'  */
#line 1122 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[-1].float64)); delete (yyvsp[-1].float64); }
#line 6266 "asmparse.cpp"
    break;

  case 420: /* fieldSerInit: FLOAT32_ '(' int32 ')'  */
#line 1124 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6273 "asmparse.cpp"
    break;

  case 421: /* fieldSerInit: FLOAT64_ '(' int64 ')'  */
#line 1126 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6280 "asmparse.cpp"
    break;

  case 422: /* fieldSerInit: INT64_ '(' int64 ')'  */
#line 1128 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I8);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6287 "asmparse.cpp"
    break;

  case 423: /* fieldSerInit: INT32_ '(' int32 ')'  */
#line 1130 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6294 "asmparse.cpp"
    break;

  case 424: /* fieldSerInit: INT16_ '(' int32 ')'  */
#line 1132 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6301 "asmparse.cpp"
    break;

  case 425: /* fieldSerInit: INT8_ '(' int32 ')'  */
#line 1134 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6308 "asmparse.cpp"
    break;

  case 426: /* fieldSerInit: UNSIGNED_ INT64_ '(' int64 ')'  */
#line 1136 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6315 "asmparse.cpp"
    break;

  case 427: /* fieldSerInit: UNSIGNED_ INT32_ '(' int32 ')'  */
#line 1138 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6322 "asmparse.cpp"
    break;

  case 428: /* fieldSerInit: UNSIGNED_ INT16_ '(' int32 ')'  */
#line 1140 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6329 "asmparse.cpp"
    break;

  case 429: /* fieldSerInit: UNSIGNED_ INT8_ '(' int32 ')'  */
#line 1142 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6336 "asmparse.cpp"
    break;

  case 430: /* fieldSerInit: UINT64_ '(' int64 ')'  */
#line 1144 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6343 "asmparse.cpp"
    break;

  case 431: /* fieldSerInit: UINT32_ '(' int32 ')'  */
#line 1146 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6350 "asmparse.cpp"
    break;

  case 432: /* fieldSerInit: UINT16_ '(' int32 ')'  */
#line 1148 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6357 "asmparse.cpp"
    break;

  case 433: /* fieldSerInit: UINT8_ '(' int32 ')'  */
#line 1150 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6364 "asmparse.cpp"
    break;

  case 434: /* fieldSerInit: CHAR_ '(' int32 ')'  */
#line 1152 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CHAR);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6371 "asmparse.cpp"
    break;

  case 435: /* fieldSerInit: BOOL_ '(' truefalse ')'  */
#line 1154 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_BOOLEAN);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32));}
#line 6378 "asmparse.cpp"
    break;

  case 436: /* fieldSerInit: bytearrayhead bytes ')'  */
#line 1156 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING);
                                                               (yyval.binstr)->append((yyvsp[-1].binstr)); delete (yyvsp[-1].binstr);}
#line 6385 "asmparse.cpp"
    break;

  case 437: /* bytearrayhead: BYTEARRAY_ '('  */
#line 1160 "asmparse.y"
                                                             { bParsingByteArray = TRUE; }
#line 6391 "asmparse.cpp"
    break;

  case 438: /* bytes: %empty  */
#line 1163 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6397 "asmparse.cpp"
    break;

  case 439: /* bytes: hexbytes  */
#line 1164 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6403 "asmparse.cpp"
    break;

  case 440: /* hexbytes: HEXBYTE  */
#line 1167 "asmparse.y"
                                                             { __int8 i = (__int8) (yyvsp[0].int32); (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(i); }
#line 6409 "asmparse.cpp"
    break;

  case 441: /* hexbytes: hexbytes HEXBYTE  */
#line 1168 "asmparse.y"
                                                             { __int8 i = (__int8) (yyvsp[0].int32); (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(i); }
#line 6415 "asmparse.cpp"
    break;

  case 442: /* fieldInit: fieldSerInit  */
#line 1172 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6421 "asmparse.cpp"
    break;

  case 443: /* fieldInit: compQstring  */
#line 1173 "asmparse.y"
                                                             { (yyval.binstr) = BinStrToUnicode((yyvsp[0].binstr),true); (yyval.binstr)->insertInt8(ELEMENT_TYPE_STRING);}
#line 6427 "asmparse.cpp"
    break;

  case 444: /* fieldInit: NULLREF_  */
#line 1174 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CLASS);
                                                               (yyval.binstr)->appendInt32(0); }
#line 6434 "asmparse.cpp"
    break;

  case 445: /* serInit: fieldSerInit  */
#line 1179 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 6440 "asmparse.cpp"
    break;

  case 446: /* serInit: STRING_ '(' NULLREF_ ')'  */
#line 1180 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); (yyval.binstr)->appendInt8(0xFF); }
#line 6446 "asmparse.cpp"
    break;

  case 447: /* serInit: STRING_ '(' SQSTRING ')'  */
#line 1181 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[-1].string)); delete [] (yyvsp[-1].string);}
#line 6453 "asmparse.cpp"
    break;

  case 448: /* serInit: TYPE_ '(' CLASS_ SQSTRING ')'  */
#line 1183 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[-1].string)); delete [] (yyvsp[-1].string);}
#line 6460 "asmparse.cpp"
    break;

  case 449: /* serInit: TYPE_ '(' className ')'  */
#line 1185 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[-1].token)));}
#line 6467 "asmparse.cpp"
    break;

  case 450: /* serInit: TYPE_ '(' NULLREF_ ')'  */
#line 1187 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_TYPE); (yyval.binstr)->appendInt8(0xFF); }
#line 6473 "asmparse.cpp"
    break;

  case 451: /* serInit: OBJECT_ '(' serInit ')'  */
#line 1188 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);}
#line 6479 "asmparse.cpp"
    break;

  case 452: /* serInit: FLOAT32_ '[' int32 ']' '(' f32seq ')'  */
#line 1190 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_R4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6487 "asmparse.cpp"
    break;

  case 453: /* serInit: FLOAT64_ '[' int32 ']' '(' f64seq ')'  */
#line 1194 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6495 "asmparse.cpp"
    break;

  case 454: /* serInit: INT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1198 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6503 "asmparse.cpp"
    break;

  case 455: /* serInit: INT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1202 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6511 "asmparse.cpp"
    break;

  case 456: /* serInit: INT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1206 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6519 "asmparse.cpp"
    break;

  case 457: /* serInit: INT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1210 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_I1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6527 "asmparse.cpp"
    break;

  case 458: /* serInit: UINT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1214 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6535 "asmparse.cpp"
    break;

  case 459: /* serInit: UINT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1218 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6543 "asmparse.cpp"
    break;

  case 460: /* serInit: UINT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1222 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6551 "asmparse.cpp"
    break;

  case 461: /* serInit: UINT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1226 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6559 "asmparse.cpp"
    break;

  case 462: /* serInit: UNSIGNED_ INT64_ '[' int32 ']' '(' i64seq ')'  */
#line 1230 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6567 "asmparse.cpp"
    break;

  case 463: /* serInit: UNSIGNED_ INT32_ '[' int32 ']' '(' i32seq ')'  */
#line 1234 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6575 "asmparse.cpp"
    break;

  case 464: /* serInit: UNSIGNED_ INT16_ '[' int32 ']' '(' i16seq ')'  */
#line 1238 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6583 "asmparse.cpp"
    break;

  case 465: /* serInit: UNSIGNED_ INT8_ '[' int32 ']' '(' i8seq ')'  */
#line 1242 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6591 "asmparse.cpp"
    break;

  case 466: /* serInit: CHAR_ '[' int32 ']' '(' i16seq ')'  */
#line 1246 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_CHAR);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6599 "asmparse.cpp"
    break;

  case 467: /* serInit: BOOL_ '[' int32 ']' '(' boolSeq ')'  */
#line 1250 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_BOOLEAN);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6607 "asmparse.cpp"
    break;

  case 468: /* serInit: STRING_ '[' int32 ']' '(' sqstringSeq ')'  */
#line 1254 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_STRING);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6615 "asmparse.cpp"
    break;

  case 469: /* serInit: TYPE_ '[' int32 ']' '(' classSeq ')'  */
#line 1258 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TYPE);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6623 "asmparse.cpp"
    break;

  case 470: /* serInit: OBJECT_ '[' int32 ']' '(' objSeq ')'  */
#line 1262 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt32((yyvsp[-4].int32));
                                                               (yyval.binstr)->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);
                                                               (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 6631 "asmparse.cpp"
    break;

  case 471: /* constTypeArg: FLOAT32_ '(' float64 ')'  */
#line 1267 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               float f = (float)(*(yyvsp[-1].float64));
                                                               (yyval.binstr)->appendInt32(*((__int32*)&f)); delete (yyvsp[-1].float64); }
#line 6639 "asmparse.cpp"
    break;

  case 472: /* constTypeArg: FLOAT64_ '(' float64 ')'  */
#line 1270 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[-1].float64)); delete (yyvsp[-1].float64); }
#line 6646 "asmparse.cpp"
    break;

  case 473: /* constTypeArg: FLOAT32_ '(' int32 ')'  */
#line 1272 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6653 "asmparse.cpp"
    break;

  case 474: /* constTypeArg: FLOAT64_ '(' int64 ')'  */
#line 1274 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6660 "asmparse.cpp"
    break;

  case 475: /* constTypeArg: INT64_ '(' int64 ')'  */
#line 1276 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I8);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6667 "asmparse.cpp"
    break;

  case 476: /* constTypeArg: INT32_ '(' int32 ')'  */
#line 1278 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6674 "asmparse.cpp"
    break;

  case 477: /* constTypeArg: INT16_ '(' int32 ')'  */
#line 1280 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6681 "asmparse.cpp"
    break;

  case 478: /* constTypeArg: INT8_ '(' int32 ')'  */
#line 1282 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6688 "asmparse.cpp"
    break;

  case 479: /* constTypeArg: UNSIGNED_ INT64_ '(' int64 ')'  */
#line 1284 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6695 "asmparse.cpp"
    break;

  case 480: /* constTypeArg: UNSIGNED_ INT32_ '(' int32 ')'  */
#line 1286 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6702 "asmparse.cpp"
    break;

  case 481: /* constTypeArg: UNSIGNED_ INT16_ '(' int32 ')'  */
#line 1288 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6709 "asmparse.cpp"
    break;

  case 482: /* constTypeArg: UNSIGNED_ INT8_ '(' int32 ')'  */
#line 1290 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6716 "asmparse.cpp"
    break;

  case 483: /* constTypeArg: UINT64_ '(' int64 ')'  */
#line 1292 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[-1].int64)); delete (yyvsp[-1].int64); }
#line 6723 "asmparse.cpp"
    break;

  case 484: /* constTypeArg: UINT32_ '(' int32 ')'  */
#line 1294 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4);
                                                               (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 6730 "asmparse.cpp"
    break;

  case 485: /* constTypeArg: UINT16_ '(' int32 ')'  */
#line 1296 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6737 "asmparse.cpp"
    break;

  case 486: /* constTypeArg: UINT8_ '(' int32 ')'  */
#line 1298 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32)); }
#line 6744 "asmparse.cpp"
    break;

  case 487: /* constTypeArg: CHAR_ '(' int32 ')'  */
#line 1300 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CHAR);
                                                               (yyval.binstr)->appendInt16((yyvsp[-1].int32)); }
#line 6751 "asmparse.cpp"
    break;

  case 488: /* constTypeArg: BOOL_ '(' truefalse ')'  */
#line 1302 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_BOOLEAN);
                                                               (yyval.binstr)->appendInt8((yyvsp[-1].int32));}
#line 6758 "asmparse.cpp"
    break;

  case 489: /* f32seq: %empty  */
#line 1307 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6764 "asmparse.cpp"
    break;

  case 490: /* f32seq: f32seq float64  */
#line 1308 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               float f = (float) (*(yyvsp[0].float64)); (yyval.binstr)->appendInt32(*((__int32*)&f)); delete (yyvsp[0].float64); }
#line 6771 "asmparse.cpp"
    break;

  case 491: /* f32seq: f32seq int32  */
#line 1310 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 6778 "asmparse.cpp"
    break;

  case 492: /* f64seq: %empty  */
#line 1314 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6784 "asmparse.cpp"
    break;

  case 493: /* f64seq: f64seq float64  */
#line 1315 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[0].float64)); delete (yyvsp[0].float64); }
#line 6791 "asmparse.cpp"
    break;

  case 494: /* f64seq: f64seq int64  */
#line 1317 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[0].int64)); delete (yyvsp[0].int64); }
#line 6798 "asmparse.cpp"
    break;

  case 495: /* i64seq: %empty  */
#line 1321 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6804 "asmparse.cpp"
    break;

  case 496: /* i64seq: i64seq int64  */
#line 1322 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt64((__int64 *)(yyvsp[0].int64)); delete (yyvsp[0].int64); }
#line 6811 "asmparse.cpp"
    break;

  case 497: /* i32seq: %empty  */
#line 1326 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6817 "asmparse.cpp"
    break;

  case 498: /* i32seq: i32seq int32  */
#line 1327 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt32((yyvsp[0].int32));}
#line 6823 "asmparse.cpp"
    break;

  case 499: /* i16seq: %empty  */
#line 1330 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6829 "asmparse.cpp"
    break;

  case 500: /* i16seq: i16seq int32  */
#line 1331 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt16((yyvsp[0].int32));}
#line 6835 "asmparse.cpp"
    break;

  case 501: /* i8seq: %empty  */
#line 1334 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6841 "asmparse.cpp"
    break;

  case 502: /* i8seq: i8seq int32  */
#line 1335 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8((yyvsp[0].int32)); }
#line 6847 "asmparse.cpp"
    break;

  case 503: /* boolSeq: %empty  */
#line 1338 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6853 "asmparse.cpp"
    break;

  case 504: /* boolSeq: boolSeq truefalse  */
#line 1339 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               (yyval.binstr)->appendInt8((yyvsp[0].int32));}
#line 6860 "asmparse.cpp"
    break;

  case 505: /* sqstringSeq: %empty  */
#line 1343 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6866 "asmparse.cpp"
    break;

  case 506: /* sqstringSeq: sqstringSeq NULLREF_  */
#line 1344 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(0xFF); }
#line 6872 "asmparse.cpp"
    break;

  case 507: /* sqstringSeq: sqstringSeq SQSTRING  */
#line 1345 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); delete [] (yyvsp[0].string);}
#line 6879 "asmparse.cpp"
    break;

  case 508: /* classSeq: %empty  */
#line 1349 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6885 "asmparse.cpp"
    break;

  case 509: /* classSeq: classSeq NULLREF_  */
#line 1350 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->appendInt8(0xFF); }
#line 6891 "asmparse.cpp"
    break;

  case 510: /* classSeq: classSeq CLASS_ SQSTRING  */
#line 1351 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr);
                                                               AppendStringWithLength((yyval.binstr),(yyvsp[0].string)); delete [] (yyvsp[0].string);}
#line 6898 "asmparse.cpp"
    break;

  case 511: /* classSeq: classSeq className  */
#line 1353 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr);
                                                               AppendStringWithLength((yyval.binstr),PASM->ReflectionNotation((yyvsp[0].token)));}
#line 6905 "asmparse.cpp"
    break;

  case 512: /* objSeq: %empty  */
#line 1357 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 6911 "asmparse.cpp"
    break;

  case 513: /* objSeq: objSeq serInit  */
#line 1358 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 6917 "asmparse.cpp"
    break;

  case 514: /* methodSpec: METHOD_  */
#line 1362 "asmparse.y"
                                                             { parser->m_ANSFirst.PUSH(PASM->m_firstArgName);
                                                               parser->m_ANSLast.PUSH(PASM->m_lastArgName);
                                                               PASM->m_firstArgName = NULL;
                                                               PASM->m_lastArgName = NULL; }
#line 6926 "asmparse.cpp"
    break;

  case 515: /* instr_none: INSTR_NONE  */
#line 1368 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6932 "asmparse.cpp"
    break;

  case 516: /* instr_var: INSTR_VAR  */
#line 1371 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6938 "asmparse.cpp"
    break;

  case 517: /* instr_i: INSTR_I  */
#line 1374 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6944 "asmparse.cpp"
    break;

  case 518: /* instr_i8: INSTR_I8  */
#line 1377 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6950 "asmparse.cpp"
    break;

  case 519: /* instr_r: INSTR_R  */
#line 1380 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6956 "asmparse.cpp"
    break;

  case 520: /* instr_brtarget: INSTR_BRTARGET  */
#line 1383 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6962 "asmparse.cpp"
    break;

  case 521: /* instr_method: INSTR_METHOD  */
#line 1386 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode));
                                                               if((!PASM->OnErrGo)&&
                                                               (((yyvsp[0].opcode) == CEE_NEWOBJ)||
                                                                ((yyvsp[0].opcode) == CEE_CALLVIRT)))
                                                                  iCallConv = IMAGE_CEE_CS_CALLCONV_HASTHIS;
                                                             }
#line 6973 "asmparse.cpp"
    break;

  case 522: /* instr_field: INSTR_FIELD  */
#line 1394 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6979 "asmparse.cpp"
    break;

  case 523: /* instr_type: INSTR_TYPE  */
#line 1397 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6985 "asmparse.cpp"
    break;

  case 524: /* instr_string: INSTR_STRING  */
#line 1400 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6991 "asmparse.cpp"
    break;

  case 525: /* instr_sig: INSTR_SIG  */
#line 1403 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 6997 "asmparse.cpp"
    break;

  case 526: /* instr_tok: INSTR_TOK  */
#line 1406 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); iOpcodeLen = PASM->OpcodeLen((yyval.instr)); }
#line 7003 "asmparse.cpp"
    break;

  case 527: /* instr_switch: INSTR_SWITCH  */
#line 1409 "asmparse.y"
                                                             { (yyval.instr) = SetupInstr((yyvsp[0].opcode)); }
#line 7009 "asmparse.cpp"
    break;

  case 528: /* instr_r_head: instr_r '('  */
#line 1412 "asmparse.y"
                                                             { (yyval.instr) = (yyvsp[-1].instr); bParsingByteArray = TRUE; }
#line 7015 "asmparse.cpp"
    break;

  case 529: /* instr: instr_none  */
#line 1416 "asmparse.y"
                                                             { PASM->EmitOpcode((yyvsp[0].instr)); }
#line 7021 "asmparse.cpp"
    break;

  case 530: /* instr: instr_var int32  */
#line 1417 "asmparse.y"
                                                             { PASM->EmitInstrVar((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 7027 "asmparse.cpp"
    break;

  case 531: /* instr: instr_var id  */
#line 1418 "asmparse.y"
                                                             { PASM->EmitInstrVarByName((yyvsp[-1].instr), (yyvsp[0].string)); }
#line 7033 "asmparse.cpp"
    break;

  case 532: /* instr: instr_i int32  */
#line 1419 "asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 7039 "asmparse.cpp"
    break;

  case 533: /* instr: instr_i8 int64  */
#line 1420 "asmparse.y"
                                                             { PASM->EmitInstrI8((yyvsp[-1].instr), (yyvsp[0].int64)); }
#line 7045 "asmparse.cpp"
    break;

  case 534: /* instr: instr_r float64  */
#line 1421 "asmparse.y"
                                                             { PASM->EmitInstrR((yyvsp[-1].instr), (yyvsp[0].float64)); delete ((yyvsp[0].float64));}
#line 7051 "asmparse.cpp"
    break;

  case 535: /* instr: instr_r int64  */
#line 1422 "asmparse.y"
                                                             { double f = (double) (*(yyvsp[0].int64)); PASM->EmitInstrR((yyvsp[-1].instr), &f); }
#line 7057 "asmparse.cpp"
    break;

  case 536: /* instr: instr_r_head bytes ')'  */
#line 1423 "asmparse.y"
                                                             { unsigned L = (yyvsp[-1].binstr)->length();
                                                               FAIL_UNLESS(L >= sizeof(float), ("%d hexbytes, must be at least %d\n",
                                                                           L,sizeof(float)));
                                                               if(L < sizeof(float)) {YYERROR; }
                                                               else {
                                                                   double f = (L >= sizeof(double)) ? *((double *)((yyvsp[-1].binstr)->ptr()))
                                                                                    : (double)(*(float *)((yyvsp[-1].binstr)->ptr()));
                                                                   PASM->EmitInstrR((yyvsp[-2].instr),&f); }
                                                               delete (yyvsp[-1].binstr); }
#line 7071 "asmparse.cpp"
    break;

  case 537: /* instr: instr_brtarget int32  */
#line 1432 "asmparse.y"
                                                             { PASM->EmitInstrBrOffset((yyvsp[-1].instr), (yyvsp[0].int32)); }
#line 7077 "asmparse.cpp"
    break;

  case 538: /* instr: instr_brtarget id  */
#line 1433 "asmparse.y"
                                                             { PASM->EmitInstrBrTarget((yyvsp[-1].instr), (yyvsp[0].string)); }
#line 7083 "asmparse.cpp"
    break;

  case 539: /* instr: instr_method methodRef  */
#line 1435 "asmparse.y"
                                                             { PASM->SetMemberRefFixup((yyvsp[0].token),PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),(yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iCallConv = 0;
                                                             }
#line 7094 "asmparse.cpp"
    break;

  case 540: /* instr: instr_field type typeSpec DCOLON dottedName  */
#line 1442 "asmparse.y"
                                                             { (yyvsp[-3].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef((yyvsp[-2].token), (yyvsp[0].string), (yyvsp[-3].binstr));
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-4].instr)));
                                                               PASM->EmitInstrI((yyvsp[-4].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7106 "asmparse.cpp"
    break;

  case 541: /* instr: instr_field type dottedName  */
#line 1450 "asmparse.y"
                                                             { (yyvsp[-1].binstr)->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef(mdTokenNil, (yyvsp[0].string), (yyvsp[-1].binstr));
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-2].instr)));
                                                               PASM->EmitInstrI((yyvsp[-2].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7118 "asmparse.cpp"
    break;

  case 542: /* instr: instr_field mdtoken  */
#line 1457 "asmparse.y"
                                                             { mdToken mr = (yyvsp[0].token);
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7129 "asmparse.cpp"
    break;

  case 543: /* instr: instr_field TYPEDEF_F  */
#line 1463 "asmparse.y"
                                                             { mdToken mr = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7140 "asmparse.cpp"
    break;

  case 544: /* instr: instr_field TYPEDEF_MR  */
#line 1469 "asmparse.y"
                                                             { mdToken mr = (yyvsp[0].tdd)->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen((yyvsp[-1].instr)));
                                                               PASM->EmitInstrI((yyvsp[-1].instr),mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7151 "asmparse.cpp"
    break;

  case 545: /* instr: instr_type typeSpec  */
#line 1475 "asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr), (yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
#line 7160 "asmparse.cpp"
    break;

  case 546: /* instr: instr_string compQstring  */
#line 1479 "asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-1].instr), (yyvsp[0].binstr),TRUE); }
#line 7166 "asmparse.cpp"
    break;

  case 547: /* instr: instr_string ANSI_ '(' compQstring ')'  */
#line 1481 "asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-4].instr), (yyvsp[-1].binstr),FALSE); }
#line 7172 "asmparse.cpp"
    break;

  case 548: /* instr: instr_string bytearrayhead bytes ')'  */
#line 1483 "asmparse.y"
                                                             { PASM->EmitInstrStringLiteral((yyvsp[-3].instr), (yyvsp[-1].binstr),FALSE,TRUE); }
#line 7178 "asmparse.cpp"
    break;

  case 549: /* instr: instr_sig callConv type '(' sigArgs0 ')'  */
#line 1485 "asmparse.y"
                                                             { PASM->EmitInstrSig((yyvsp[-5].instr), parser->MakeSig((yyvsp[-4].int32), (yyvsp[-3].binstr), (yyvsp[-1].binstr)));
                                                               PASM->ResetArgNameList();
                                                             }
#line 7186 "asmparse.cpp"
    break;

  case 550: /* instr: instr_tok ownerType  */
#line 1489 "asmparse.y"
                                                             { PASM->EmitInstrI((yyvsp[-1].instr),(yyvsp[0].token));
                                                               PASM->m_tkCurrentCVOwner = (yyvsp[0].token);
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iOpcodeLen = 0;
                                                             }
#line 7196 "asmparse.cpp"
    break;

  case 551: /* instr: instr_switch '(' labels ')'  */
#line 1494 "asmparse.y"
                                                             { PASM->EmitInstrSwitch((yyvsp[-3].instr), (yyvsp[-1].labels)); }
#line 7202 "asmparse.cpp"
    break;

  case 552: /* labels: %empty  */
#line 1497 "asmparse.y"
                                                              { (yyval.labels) = 0; }
#line 7208 "asmparse.cpp"
    break;

  case 553: /* labels: id ',' labels  */
#line 1498 "asmparse.y"
                                                              { (yyval.labels) = new Labels((yyvsp[-2].string), (yyvsp[0].labels), TRUE); }
#line 7214 "asmparse.cpp"
    break;

  case 554: /* labels: int32 ',' labels  */
#line 1499 "asmparse.y"
                                                              { (yyval.labels) = new Labels((char *)(UINT_PTR)(yyvsp[-2].int32), (yyvsp[0].labels), FALSE); }
#line 7220 "asmparse.cpp"
    break;

  case 555: /* labels: id  */
#line 1500 "asmparse.y"
                                                              { (yyval.labels) = new Labels((yyvsp[0].string), NULL, TRUE); }
#line 7226 "asmparse.cpp"
    break;

  case 556: /* labels: int32  */
#line 1501 "asmparse.y"
                                                              { (yyval.labels) = new Labels((char *)(UINT_PTR)(yyvsp[0].int32), NULL, FALSE); }
#line 7232 "asmparse.cpp"
    break;

  case 557: /* tyArgs0: %empty  */
#line 1505 "asmparse.y"
                                                             { (yyval.binstr) = NULL; }
#line 7238 "asmparse.cpp"
    break;

  case 558: /* tyArgs0: '<' tyArgs1 '>'  */
#line 1506 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-1].binstr); }
#line 7244 "asmparse.cpp"
    break;

  case 559: /* tyArgs1: %empty  */
#line 1509 "asmparse.y"
                                                             { (yyval.binstr) = NULL; }
#line 7250 "asmparse.cpp"
    break;

  case 560: /* tyArgs1: tyArgs2  */
#line 1510 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7256 "asmparse.cpp"
    break;

  case 561: /* tyArgs2: type  */
#line 1513 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7262 "asmparse.cpp"
    break;

  case 562: /* tyArgs2: tyArgs2 ',' type  */
#line 1514 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 7268 "asmparse.cpp"
    break;

  case 563: /* sigArgs0: %empty  */
#line 1518 "asmparse.y"
                                                             { (yyval.binstr) = new BinStr(); }
#line 7274 "asmparse.cpp"
    break;

  case 564: /* sigArgs0: sigArgs1  */
#line 1519 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr);}
#line 7280 "asmparse.cpp"
    break;

  case 565: /* sigArgs1: sigArg  */
#line 1522 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[0].binstr); }
#line 7286 "asmparse.cpp"
    break;

  case 566: /* sigArgs1: sigArgs1 ',' sigArg  */
#line 1523 "asmparse.y"
                                                             { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 7292 "asmparse.cpp"
    break;

  case 567: /* sigArg: ELLIPSIS  */
#line 1526 "asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_SENTINEL); }
#line 7298 "asmparse.cpp"
    break;

  case 568: /* sigArg: paramAttr type marshalClause  */
#line 1527 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[-1].binstr)); PASM->addArgName(NULL, (yyvsp[-1].binstr), (yyvsp[0].binstr), (yyvsp[-2].int32)); }
#line 7304 "asmparse.cpp"
    break;

  case 569: /* sigArg: paramAttr type marshalClause id  */
#line 1528 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[-2].binstr)); PASM->addArgName((yyvsp[0].string), (yyvsp[-2].binstr), (yyvsp[-1].binstr), (yyvsp[-3].int32));}
#line 7310 "asmparse.cpp"
    break;

  case 570: /* className: '[' dottedName ']' slashedName  */
#line 1532 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(PASM->GetAsmRef((yyvsp[-2].string)), (yyvsp[0].string), NULL); delete[] (yyvsp[-2].string);}
#line 7316 "asmparse.cpp"
    break;

  case 571: /* className: '[' mdtoken ']' slashedName  */
#line 1533 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef((yyvsp[-2].token), (yyvsp[0].string), NULL); }
#line 7322 "asmparse.cpp"
    break;

  case 572: /* className: '[' '*' ']' slashedName  */
#line 1534 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(mdTokenNil, (yyvsp[0].string), NULL); }
#line 7328 "asmparse.cpp"
    break;

  case 573: /* className: '[' _MODULE dottedName ']' slashedName  */
#line 1535 "asmparse.y"
                                                                   { (yyval.token) = PASM->ResolveClassRef(PASM->GetModRef((yyvsp[-2].string)),(yyvsp[0].string), NULL); delete[] (yyvsp[-2].string);}
#line 7334 "asmparse.cpp"
    break;

  case 574: /* className: slashedName  */
#line 1536 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveClassRef(1,(yyvsp[0].string),NULL); }
#line 7340 "asmparse.cpp"
    break;

  case 575: /* className: mdtoken  */
#line 1537 "asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].token); }
#line 7346 "asmparse.cpp"
    break;

  case 576: /* className: TYPEDEF_T  */
#line 1538 "asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].tdd)->m_tkTypeSpec; }
#line 7352 "asmparse.cpp"
    break;

  case 577: /* className: _THIS  */
#line 1539 "asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) (yyval.token) = PASM->m_pCurClass->m_cl;
                                                                else { (yyval.token) = 0; PASM->report->error(".this outside class scope\n"); }
                                                              }
#line 7360 "asmparse.cpp"
    break;

  case 578: /* className: _BASE  */
#line 1542 "asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) {
                                                                  (yyval.token) = PASM->m_pCurClass->m_crExtends;
                                                                  if(RidFromToken((yyval.token)) == 0)
                                                                    PASM->report->error(".base undefined\n");
                                                                } else { (yyval.token) = 0; PASM->report->error(".base outside class scope\n"); }
                                                              }
#line 7371 "asmparse.cpp"
    break;

  case 579: /* className: _NESTER  */
#line 1548 "asmparse.y"
                                                              { if(PASM->m_pCurClass != NULL) {
                                                                  if(PASM->m_pCurClass->m_pEncloser != NULL) (yyval.token) = PASM->m_pCurClass->m_pEncloser->m_cl;
                                                                  else { (yyval.token) = 0; PASM->report->error(".nester undefined\n"); }
                                                                } else { (yyval.token) = 0; PASM->report->error(".nester outside class scope\n"); }
                                                              }
#line 7381 "asmparse.cpp"
    break;

  case 580: /* slashedName: dottedName  */
#line 1555 "asmparse.y"
                                                              { (yyval.string) = (yyvsp[0].string); }
#line 7387 "asmparse.cpp"
    break;

  case 581: /* slashedName: slashedName '/' dottedName  */
#line 1556 "asmparse.y"
                                                              { (yyval.string) = newStringWDel((yyvsp[-2].string), NESTING_SEP, (yyvsp[0].string)); }
#line 7393 "asmparse.cpp"
    break;

  case 582: /* typeSpec: className  */
#line 1559 "asmparse.y"
                                                              { (yyval.token) = (yyvsp[0].token);}
#line 7399 "asmparse.cpp"
    break;

  case 583: /* typeSpec: '[' dottedName ']'  */
#line 1560 "asmparse.y"
                                                              { (yyval.token) = PASM->GetAsmRef((yyvsp[-1].string)); delete[] (yyvsp[-1].string);}
#line 7405 "asmparse.cpp"
    break;

  case 584: /* typeSpec: '[' _MODULE dottedName ']'  */
#line 1561 "asmparse.y"
                                                              { (yyval.token) = PASM->GetModRef((yyvsp[-1].string)); delete[] (yyvsp[-1].string);}
#line 7411 "asmparse.cpp"
    break;

  case 585: /* typeSpec: type  */
#line 1562 "asmparse.y"
                                                              { (yyval.token) = PASM->ResolveTypeSpec((yyvsp[0].binstr)); }
#line 7417 "asmparse.cpp"
    break;

  case 586: /* nativeType: %empty  */
#line 1566 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); }
#line 7423 "asmparse.cpp"
    break;

  case 587: /* nativeType: CUSTOM_ '(' compQstring ',' compQstring ',' compQstring ',' compQstring ')'  */
#line 1568 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt((yyval.binstr),(yyvsp[-7].binstr)->length()); (yyval.binstr)->append((yyvsp[-7].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-5].binstr)->length()); (yyval.binstr)->append((yyvsp[-5].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].binstr)->length()); (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].binstr)->length()); (yyval.binstr)->append((yyvsp[-1].binstr));
                                                                PASM->report->warn("Deprecated 4-string form of custom marshaler, first two strings ignored\n");}
#line 7434 "asmparse.cpp"
    break;

  case 588: /* nativeType: CUSTOM_ '(' compQstring ',' compQstring ')'  */
#line 1575 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].binstr)->length()); (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].binstr)->length()); (yyval.binstr)->append((yyvsp[-1].binstr)); }
#line 7444 "asmparse.cpp"
    break;

  case 589: /* nativeType: FIXED_ SYSSTRING_ '[' int32 ']'  */
#line 1580 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FIXEDSYSSTRING);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32)); }
#line 7451 "asmparse.cpp"
    break;

  case 590: /* nativeType: FIXED_ ARRAY_ '[' int32 ']' nativeType  */
#line 1583 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FIXEDARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-2].int32)); (yyval.binstr)->append((yyvsp[0].binstr)); }
#line 7458 "asmparse.cpp"
    break;

  case 591: /* nativeType: VARIANT_  */
#line 1585 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VARIANT);
                                                                PASM->report->warn("Deprecated native type 'variant'\n"); }
#line 7465 "asmparse.cpp"
    break;

  case 592: /* nativeType: CURRENCY_  */
#line 1587 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_CURRENCY); }
#line 7471 "asmparse.cpp"
    break;

  case 593: /* nativeType: SYSCHAR_  */
#line 1588 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SYSCHAR);
                                                                PASM->report->warn("Deprecated native type 'syschar'\n"); }
#line 7478 "asmparse.cpp"
    break;

  case 594: /* nativeType: VOID_  */
#line 1590 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VOID);
                                                                PASM->report->warn("Deprecated native type 'void'\n"); }
#line 7485 "asmparse.cpp"
    break;

  case 595: /* nativeType: BOOL_  */
#line 1592 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BOOLEAN); }
#line 7491 "asmparse.cpp"
    break;

  case 596: /* nativeType: INT8_  */
#line 1593 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I1); }
#line 7497 "asmparse.cpp"
    break;

  case 597: /* nativeType: INT16_  */
#line 1594 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I2); }
#line 7503 "asmparse.cpp"
    break;

  case 598: /* nativeType: INT32_  */
#line 1595 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I4); }
#line 7509 "asmparse.cpp"
    break;

  case 599: /* nativeType: INT64_  */
#line 1596 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_I8); }
#line 7515 "asmparse.cpp"
    break;

  case 600: /* nativeType: FLOAT32_  */
#line 1597 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_R4); }
#line 7521 "asmparse.cpp"
    break;

  case 601: /* nativeType: FLOAT64_  */
#line 1598 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_R8); }
#line 7527 "asmparse.cpp"
    break;

  case 602: /* nativeType: ERROR_  */
#line 1599 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ERROR); }
#line 7533 "asmparse.cpp"
    break;

  case 603: /* nativeType: UNSIGNED_ INT8_  */
#line 1600 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U1); }
#line 7539 "asmparse.cpp"
    break;

  case 604: /* nativeType: UNSIGNED_ INT16_  */
#line 1601 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U2); }
#line 7545 "asmparse.cpp"
    break;

  case 605: /* nativeType: UNSIGNED_ INT32_  */
#line 1602 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U4); }
#line 7551 "asmparse.cpp"
    break;

  case 606: /* nativeType: UNSIGNED_ INT64_  */
#line 1603 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U8); }
#line 7557 "asmparse.cpp"
    break;

  case 607: /* nativeType: UINT8_  */
#line 1604 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U1); }
#line 7563 "asmparse.cpp"
    break;

  case 608: /* nativeType: UINT16_  */
#line 1605 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U2); }
#line 7569 "asmparse.cpp"
    break;

  case 609: /* nativeType: UINT32_  */
#line 1606 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U4); }
#line 7575 "asmparse.cpp"
    break;

  case 610: /* nativeType: UINT64_  */
#line 1607 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_U8); }
#line 7581 "asmparse.cpp"
    break;

  case 611: /* nativeType: nativeType '*'  */
#line 1608 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(NATIVE_TYPE_PTR);
                                                                PASM->report->warn("Deprecated native type '*'\n"); }
#line 7588 "asmparse.cpp"
    break;

  case 612: /* nativeType: nativeType '[' ']'  */
#line 1610 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY); }
#line 7595 "asmparse.cpp"
    break;

  case 613: /* nativeType: nativeType '[' int32 ']'  */
#line 1612 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-3].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),0);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32));
                                                                corEmitInt((yyval.binstr),0); }
#line 7605 "asmparse.cpp"
    break;

  case 614: /* nativeType: nativeType '[' int32 '+' int32 ']'  */
#line 1617 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-5].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32));
                                                                corEmitInt((yyval.binstr),(yyvsp[-3].int32));
                                                                corEmitInt((yyval.binstr),ntaSizeParamIndexSpecified); }
#line 7615 "asmparse.cpp"
    break;

  case 615: /* nativeType: nativeType '[' '+' int32 ']'  */
#line 1622 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-4].binstr); if((yyval.binstr)->length()==0) (yyval.binstr)->appendInt8(NATIVE_TYPE_MAX);
                                                                (yyval.binstr)->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-1].int32)); }
#line 7623 "asmparse.cpp"
    break;

  case 616: /* nativeType: DECIMAL_  */
#line 1625 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_DECIMAL);
                                                                PASM->report->warn("Deprecated native type 'decimal'\n"); }
#line 7630 "asmparse.cpp"
    break;

  case 617: /* nativeType: DATE_  */
#line 1627 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_DATE);
                                                                PASM->report->warn("Deprecated native type 'date'\n"); }
#line 7637 "asmparse.cpp"
    break;

  case 618: /* nativeType: BSTR_  */
#line 1629 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BSTR); }
#line 7643 "asmparse.cpp"
    break;

  case 619: /* nativeType: LPSTR_  */
#line 1630 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPSTR); }
#line 7649 "asmparse.cpp"
    break;

  case 620: /* nativeType: LPWSTR_  */
#line 1631 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPWSTR); }
#line 7655 "asmparse.cpp"
    break;

  case 621: /* nativeType: LPTSTR_  */
#line 1632 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPTSTR); }
#line 7661 "asmparse.cpp"
    break;

  case 622: /* nativeType: OBJECTREF_  */
#line 1633 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_OBJECTREF);
                                                                PASM->report->warn("Deprecated native type 'objectref'\n"); }
#line 7668 "asmparse.cpp"
    break;

  case 623: /* nativeType: IUNKNOWN_ iidParamIndex  */
#line 1635 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_IUNKNOWN);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7675 "asmparse.cpp"
    break;

  case 624: /* nativeType: IDISPATCH_ iidParamIndex  */
#line 1637 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_IDISPATCH);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7682 "asmparse.cpp"
    break;

  case 625: /* nativeType: STRUCT_  */
#line 1639 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_STRUCT); }
#line 7688 "asmparse.cpp"
    break;

  case 626: /* nativeType: INTERFACE_ iidParamIndex  */
#line 1640 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_INTF);
                                                                if((yyvsp[0].int32) != -1) corEmitInt((yyval.binstr),(yyvsp[0].int32)); }
#line 7695 "asmparse.cpp"
    break;

  case 627: /* nativeType: SAFEARRAY_ variantType  */
#line 1642 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[0].int32));
                                                                corEmitInt((yyval.binstr),0);}
#line 7703 "asmparse.cpp"
    break;

  case 628: /* nativeType: SAFEARRAY_ variantType ',' compQstring  */
#line 1645 "asmparse.y"
                                                                 { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt((yyval.binstr),(yyvsp[-2].int32));
                                                                corEmitInt((yyval.binstr),(yyvsp[0].binstr)->length()); (yyval.binstr)->append((yyvsp[0].binstr)); }
#line 7711 "asmparse.cpp"
    break;

  case 629: /* nativeType: INT_  */
#line 1649 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_INT); }
#line 7717 "asmparse.cpp"
    break;

  case 630: /* nativeType: UNSIGNED_ INT_  */
#line 1650 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_UINT); }
#line 7723 "asmparse.cpp"
    break;

  case 631: /* nativeType: UINT_  */
#line 1651 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_UINT); }
#line 7729 "asmparse.cpp"
    break;

  case 632: /* nativeType: NESTED_ STRUCT_  */
#line 1652 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_NESTEDSTRUCT);
                                                                PASM->report->warn("Deprecated native type 'nested struct'\n"); }
#line 7736 "asmparse.cpp"
    break;

  case 633: /* nativeType: BYVALSTR_  */
#line 1654 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_BYVALSTR); }
#line 7742 "asmparse.cpp"
    break;

  case 634: /* nativeType: ANSI_ BSTR_  */
#line 1655 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ANSIBSTR); }
#line 7748 "asmparse.cpp"
    break;

  case 635: /* nativeType: TBSTR_  */
#line 1656 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_TBSTR); }
#line 7754 "asmparse.cpp"
    break;

  case 636: /* nativeType: VARIANT_ BOOL_  */
#line 1657 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_VARIANTBOOL); }
#line 7760 "asmparse.cpp"
    break;

  case 637: /* nativeType: METHOD_  */
#line 1658 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_FUNC); }
#line 7766 "asmparse.cpp"
    break;

  case 638: /* nativeType: AS_ ANY_  */
#line 1659 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_ASANY); }
#line 7772 "asmparse.cpp"
    break;

  case 639: /* nativeType: LPSTRUCT_  */
#line 1660 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(NATIVE_TYPE_LPSTRUCT); }
#line 7778 "asmparse.cpp"
    break;

  case 640: /* nativeType: TYPEDEF_TS  */
#line 1661 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[0].tdd)->m_pbsTypeSpec); }
#line 7784 "asmparse.cpp"
    break;

  case 641: /* iidParamIndex: %empty  */
#line 1664 "asmparse.y"
                                                              { (yyval.int32) = -1; }
#line 7790 "asmparse.cpp"
    break;

  case 642: /* iidParamIndex: '(' IIDPARAM_ '=' int32 ')'  */
#line 1665 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32); }
#line 7796 "asmparse.cpp"
    break;

  case 643: /* variantType: %empty  */
#line 1668 "asmparse.y"
                                                              { (yyval.int32) = VT_EMPTY; }
#line 7802 "asmparse.cpp"
    break;

  case 644: /* variantType: NULL_  */
#line 1669 "asmparse.y"
                                                              { (yyval.int32) = VT_NULL; }
#line 7808 "asmparse.cpp"
    break;

  case 645: /* variantType: VARIANT_  */
#line 1670 "asmparse.y"
                                                              { (yyval.int32) = VT_VARIANT; }
#line 7814 "asmparse.cpp"
    break;

  case 646: /* variantType: CURRENCY_  */
#line 1671 "asmparse.y"
                                                              { (yyval.int32) = VT_CY; }
#line 7820 "asmparse.cpp"
    break;

  case 647: /* variantType: VOID_  */
#line 1672 "asmparse.y"
                                                              { (yyval.int32) = VT_VOID; }
#line 7826 "asmparse.cpp"
    break;

  case 648: /* variantType: BOOL_  */
#line 1673 "asmparse.y"
                                                              { (yyval.int32) = VT_BOOL; }
#line 7832 "asmparse.cpp"
    break;

  case 649: /* variantType: INT8_  */
#line 1674 "asmparse.y"
                                                              { (yyval.int32) = VT_I1; }
#line 7838 "asmparse.cpp"
    break;

  case 650: /* variantType: INT16_  */
#line 1675 "asmparse.y"
                                                              { (yyval.int32) = VT_I2; }
#line 7844 "asmparse.cpp"
    break;

  case 651: /* variantType: INT32_  */
#line 1676 "asmparse.y"
                                                              { (yyval.int32) = VT_I4; }
#line 7850 "asmparse.cpp"
    break;

  case 652: /* variantType: INT64_  */
#line 1677 "asmparse.y"
                                                              { (yyval.int32) = VT_I8; }
#line 7856 "asmparse.cpp"
    break;

  case 653: /* variantType: FLOAT32_  */
#line 1678 "asmparse.y"
                                                              { (yyval.int32) = VT_R4; }
#line 7862 "asmparse.cpp"
    break;

  case 654: /* variantType: FLOAT64_  */
#line 1679 "asmparse.y"
                                                              { (yyval.int32) = VT_R8; }
#line 7868 "asmparse.cpp"
    break;

  case 655: /* variantType: UNSIGNED_ INT8_  */
#line 1680 "asmparse.y"
                                                              { (yyval.int32) = VT_UI1; }
#line 7874 "asmparse.cpp"
    break;

  case 656: /* variantType: UNSIGNED_ INT16_  */
#line 1681 "asmparse.y"
                                                              { (yyval.int32) = VT_UI2; }
#line 7880 "asmparse.cpp"
    break;

  case 657: /* variantType: UNSIGNED_ INT32_  */
#line 1682 "asmparse.y"
                                                              { (yyval.int32) = VT_UI4; }
#line 7886 "asmparse.cpp"
    break;

  case 658: /* variantType: UNSIGNED_ INT64_  */
#line 1683 "asmparse.y"
                                                              { (yyval.int32) = VT_UI8; }
#line 7892 "asmparse.cpp"
    break;

  case 659: /* variantType: UINT8_  */
#line 1684 "asmparse.y"
                                                              { (yyval.int32) = VT_UI1; }
#line 7898 "asmparse.cpp"
    break;

  case 660: /* variantType: UINT16_  */
#line 1685 "asmparse.y"
                                                              { (yyval.int32) = VT_UI2; }
#line 7904 "asmparse.cpp"
    break;

  case 661: /* variantType: UINT32_  */
#line 1686 "asmparse.y"
                                                              { (yyval.int32) = VT_UI4; }
#line 7910 "asmparse.cpp"
    break;

  case 662: /* variantType: UINT64_  */
#line 1687 "asmparse.y"
                                                              { (yyval.int32) = VT_UI8; }
#line 7916 "asmparse.cpp"
    break;

  case 663: /* variantType: '*'  */
#line 1688 "asmparse.y"
                                                              { (yyval.int32) = VT_PTR; }
#line 7922 "asmparse.cpp"
    break;

  case 664: /* variantType: variantType '[' ']'  */
#line 1689 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-2].int32) | VT_ARRAY; }
#line 7928 "asmparse.cpp"
    break;

  case 665: /* variantType: variantType VECTOR_  */
#line 1690 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32) | VT_VECTOR; }
#line 7934 "asmparse.cpp"
    break;

  case 666: /* variantType: variantType '&'  */
#line 1691 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[-1].int32) | VT_BYREF; }
#line 7940 "asmparse.cpp"
    break;

  case 667: /* variantType: DECIMAL_  */
#line 1692 "asmparse.y"
                                                              { (yyval.int32) = VT_DECIMAL; }
#line 7946 "asmparse.cpp"
    break;

  case 668: /* variantType: DATE_  */
#line 1693 "asmparse.y"
                                                              { (yyval.int32) = VT_DATE; }
#line 7952 "asmparse.cpp"
    break;

  case 669: /* variantType: BSTR_  */
#line 1694 "asmparse.y"
                                                              { (yyval.int32) = VT_BSTR; }
#line 7958 "asmparse.cpp"
    break;

  case 670: /* variantType: LPSTR_  */
#line 1695 "asmparse.y"
                                                              { (yyval.int32) = VT_LPSTR; }
#line 7964 "asmparse.cpp"
    break;

  case 671: /* variantType: LPWSTR_  */
#line 1696 "asmparse.y"
                                                              { (yyval.int32) = VT_LPWSTR; }
#line 7970 "asmparse.cpp"
    break;

  case 672: /* variantType: IUNKNOWN_  */
#line 1697 "asmparse.y"
                                                              { (yyval.int32) = VT_UNKNOWN; }
#line 7976 "asmparse.cpp"
    break;

  case 673: /* variantType: IDISPATCH_  */
#line 1698 "asmparse.y"
                                                              { (yyval.int32) = VT_DISPATCH; }
#line 7982 "asmparse.cpp"
    break;

  case 674: /* variantType: SAFEARRAY_  */
#line 1699 "asmparse.y"
                                                              { (yyval.int32) = VT_SAFEARRAY; }
#line 7988 "asmparse.cpp"
    break;

  case 675: /* variantType: INT_  */
#line 1700 "asmparse.y"
                                                              { (yyval.int32) = VT_INT; }
#line 7994 "asmparse.cpp"
    break;

  case 676: /* variantType: UNSIGNED_ INT_  */
#line 1701 "asmparse.y"
                                                              { (yyval.int32) = VT_UINT; }
#line 8000 "asmparse.cpp"
    break;

  case 677: /* variantType: UINT_  */
#line 1702 "asmparse.y"
                                                              { (yyval.int32) = VT_UINT; }
#line 8006 "asmparse.cpp"
    break;

  case 678: /* variantType: ERROR_  */
#line 1703 "asmparse.y"
                                                              { (yyval.int32) = VT_ERROR; }
#line 8012 "asmparse.cpp"
    break;

  case 679: /* variantType: HRESULT_  */
#line 1704 "asmparse.y"
                                                              { (yyval.int32) = VT_HRESULT; }
#line 8018 "asmparse.cpp"
    break;

  case 680: /* variantType: CARRAY_  */
#line 1705 "asmparse.y"
                                                              { (yyval.int32) = VT_CARRAY; }
#line 8024 "asmparse.cpp"
    break;

  case 681: /* variantType: USERDEFINED_  */
#line 1706 "asmparse.y"
                                                              { (yyval.int32) = VT_USERDEFINED; }
#line 8030 "asmparse.cpp"
    break;

  case 682: /* variantType: RECORD_  */
#line 1707 "asmparse.y"
                                                              { (yyval.int32) = VT_RECORD; }
#line 8036 "asmparse.cpp"
    break;

  case 683: /* variantType: FILETIME_  */
#line 1708 "asmparse.y"
                                                              { (yyval.int32) = VT_FILETIME; }
#line 8042 "asmparse.cpp"
    break;

  case 684: /* variantType: BLOB_  */
#line 1709 "asmparse.y"
                                                              { (yyval.int32) = VT_BLOB; }
#line 8048 "asmparse.cpp"
    break;

  case 685: /* variantType: STREAM_  */
#line 1710 "asmparse.y"
                                                              { (yyval.int32) = VT_STREAM; }
#line 8054 "asmparse.cpp"
    break;

  case 686: /* variantType: STORAGE_  */
#line 1711 "asmparse.y"
                                                              { (yyval.int32) = VT_STORAGE; }
#line 8060 "asmparse.cpp"
    break;

  case 687: /* variantType: STREAMED_OBJECT_  */
#line 1712 "asmparse.y"
                                                              { (yyval.int32) = VT_STREAMED_OBJECT; }
#line 8066 "asmparse.cpp"
    break;

  case 688: /* variantType: STORED_OBJECT_  */
#line 1713 "asmparse.y"
                                                              { (yyval.int32) = VT_STORED_OBJECT; }
#line 8072 "asmparse.cpp"
    break;

  case 689: /* variantType: BLOB_OBJECT_  */
#line 1714 "asmparse.y"
                                                              { (yyval.int32) = VT_BLOB_OBJECT; }
#line 8078 "asmparse.cpp"
    break;

  case 690: /* variantType: CF_  */
#line 1715 "asmparse.y"
                                                              { (yyval.int32) = VT_CF; }
#line 8084 "asmparse.cpp"
    break;

  case 691: /* variantType: CLSID_  */
#line 1716 "asmparse.y"
                                                              { (yyval.int32) = VT_CLSID; }
#line 8090 "asmparse.cpp"
    break;

  case 692: /* type: CLASS_ className  */
#line 1720 "asmparse.y"
                                                              { if((yyvsp[0].token) == PASM->m_tkSysString)
                                                                {     (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); }
                                                                else if((yyvsp[0].token) == PASM->m_tkSysObject)
                                                                {     (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_OBJECT); }
                                                                else
                                                                 (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CLASS, (yyvsp[0].token)); }
#line 8101 "asmparse.cpp"
    break;

  case 693: /* type: OBJECT_  */
#line 1726 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_OBJECT); }
#line 8107 "asmparse.cpp"
    break;

  case 694: /* type: VALUE_ CLASS_ className  */
#line 1727 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, (yyvsp[0].token)); }
#line 8113 "asmparse.cpp"
    break;

  case 695: /* type: VALUETYPE_ className  */
#line 1728 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, (yyvsp[0].token)); }
#line 8119 "asmparse.cpp"
    break;

  case 696: /* type: CONST_ constTypeArg  */
#line 1729 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_CTARG); }
#line 8125 "asmparse.cpp"
    break;

  case 697: /* type: type '[' ']'  */
#line 1730 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SZARRAY); }
#line 8131 "asmparse.cpp"
    break;

  case 698: /* type: type '[' bounds1 ']'  */
#line 1731 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeArray(ELEMENT_TYPE_ARRAY, (yyvsp[-3].binstr), (yyvsp[-1].binstr)); }
#line 8137 "asmparse.cpp"
    break;

  case 699: /* type: type '&'  */
#line 1732 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_BYREF); }
#line 8143 "asmparse.cpp"
    break;

  case 700: /* type: type '*'  */
#line 1733 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_PTR); }
#line 8149 "asmparse.cpp"
    break;

  case 701: /* type: type PINNED_  */
#line 1734 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-1].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_PINNED); }
#line 8155 "asmparse.cpp"
    break;

  case 702: /* type: type MODREQ_ '(' typeSpec ')'  */
#line 1735 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_REQD, (yyvsp[-1].token));
                                                                (yyval.binstr)->append((yyvsp[-4].binstr)); }
#line 8162 "asmparse.cpp"
    break;

  case 703: /* type: type MODOPT_ '(' typeSpec ')'  */
#line 1737 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_OPT, (yyvsp[-1].token));
                                                                (yyval.binstr)->append((yyvsp[-4].binstr)); }
#line 8169 "asmparse.cpp"
    break;

  case 704: /* type: methodSpec callConv type '*' '(' sigArgs0 ')'  */
#line 1740 "asmparse.y"
                                                              { (yyval.binstr) = parser->MakeSig((yyvsp[-5].int32), (yyvsp[-4].binstr), (yyvsp[-1].binstr));
                                                                (yyval.binstr)->insertInt8(ELEMENT_TYPE_FNPTR);
                                                                PASM->delArgNameList(PASM->m_firstArgName);
                                                                PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                                PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                              }
#line 8180 "asmparse.cpp"
    break;

  case 705: /* type: type '<' tyArgs1 '>'  */
#line 1746 "asmparse.y"
                                                              { if((yyvsp[-1].binstr) == NULL) (yyval.binstr) = (yyvsp[-3].binstr);
                                                                else {
                                                                  (yyval.binstr) = new BinStr();
                                                                  (yyval.binstr)->appendInt8(ELEMENT_TYPE_GENERICINST);
                                                                  (yyval.binstr)->append((yyvsp[-3].binstr));
                                                                  corEmitInt((yyval.binstr), corCountArgs((yyvsp[-1].binstr)));
                                                                  (yyval.binstr)->append((yyvsp[-1].binstr)); delete (yyvsp[-3].binstr); delete (yyvsp[-1].binstr); }}
#line 8192 "asmparse.cpp"
    break;

  case 706: /* type: '!' '!' int32  */
#line 1753 "asmparse.y"
                                                              { //if(PASM->m_pCurMethod)  {
                                                                //  if(($3 < 0)||((DWORD)$3 >= PASM->m_pCurMethod->m_NumTyPars))
                                                                //    PASM->report->error("Invalid method type parameter '%d'\n",$3);
                                                                  (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_MVAR); corEmitInt((yyval.binstr), (yyvsp[0].int32));
                                                                //} else PASM->report->error("Method type parameter '%d' outside method scope\n",$3);
                                                              }
#line 8203 "asmparse.cpp"
    break;

  case 707: /* type: '!' int32  */
#line 1759 "asmparse.y"
                                                              { //if(PASM->m_pCurClass)  {
                                                                //  if(($2 < 0)||((DWORD)$2 >= PASM->m_pCurClass->m_NumTyPars))
                                                                //    PASM->report->error("Invalid type parameter '%d'\n",$2);
                                                                  (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_VAR); corEmitInt((yyval.binstr), (yyvsp[0].int32));
                                                                //} else PASM->report->error("Type parameter '%d' outside class scope\n",$2);
                                                              }
#line 8214 "asmparse.cpp"
    break;

  case 708: /* type: '!' '!' dottedName  */
#line 1765 "asmparse.y"
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
#line 8234 "asmparse.cpp"
    break;

  case 709: /* type: '!' dottedName  */
#line 1780 "asmparse.y"
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
#line 8254 "asmparse.cpp"
    break;

  case 710: /* type: TYPEDREF_  */
#line 1795 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_TYPEDBYREF); }
#line 8260 "asmparse.cpp"
    break;

  case 711: /* type: VOID_  */
#line 1796 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_VOID); }
#line 8266 "asmparse.cpp"
    break;

  case 712: /* type: NATIVE_ INT_  */
#line 1797 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I); }
#line 8272 "asmparse.cpp"
    break;

  case 713: /* type: NATIVE_ UNSIGNED_ INT_  */
#line 1798 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U); }
#line 8278 "asmparse.cpp"
    break;

  case 714: /* type: NATIVE_ UINT_  */
#line 1799 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U); }
#line 8284 "asmparse.cpp"
    break;

  case 715: /* type: simpleType  */
#line 1800 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 8290 "asmparse.cpp"
    break;

  case 716: /* type: ELLIPSIS type  */
#line 1801 "asmparse.y"
                                                               { (yyval.binstr) = (yyvsp[0].binstr); (yyval.binstr)->insertInt8(ELEMENT_TYPE_SENTINEL); }
#line 8296 "asmparse.cpp"
    break;

  case 717: /* simpleType: CHAR_  */
#line 1804 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_CHAR); }
#line 8302 "asmparse.cpp"
    break;

  case 718: /* simpleType: STRING_  */
#line 1805 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_STRING); }
#line 8308 "asmparse.cpp"
    break;

  case 719: /* simpleType: BOOL_  */
#line 1806 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_BOOLEAN); }
#line 8314 "asmparse.cpp"
    break;

  case 720: /* simpleType: INT8_  */
#line 1807 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I1); }
#line 8320 "asmparse.cpp"
    break;

  case 721: /* simpleType: INT16_  */
#line 1808 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I2); }
#line 8326 "asmparse.cpp"
    break;

  case 722: /* simpleType: INT32_  */
#line 1809 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I4); }
#line 8332 "asmparse.cpp"
    break;

  case 723: /* simpleType: INT64_  */
#line 1810 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_I8); }
#line 8338 "asmparse.cpp"
    break;

  case 724: /* simpleType: FLOAT32_  */
#line 1811 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R4); }
#line 8344 "asmparse.cpp"
    break;

  case 725: /* simpleType: FLOAT64_  */
#line 1812 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_R8); }
#line 8350 "asmparse.cpp"
    break;

  case 726: /* simpleType: UNSIGNED_ INT8_  */
#line 1813 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1); }
#line 8356 "asmparse.cpp"
    break;

  case 727: /* simpleType: UNSIGNED_ INT16_  */
#line 1814 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2); }
#line 8362 "asmparse.cpp"
    break;

  case 728: /* simpleType: UNSIGNED_ INT32_  */
#line 1815 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4); }
#line 8368 "asmparse.cpp"
    break;

  case 729: /* simpleType: UNSIGNED_ INT64_  */
#line 1816 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8); }
#line 8374 "asmparse.cpp"
    break;

  case 730: /* simpleType: UINT8_  */
#line 1817 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U1); }
#line 8380 "asmparse.cpp"
    break;

  case 731: /* simpleType: UINT16_  */
#line 1818 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U2); }
#line 8386 "asmparse.cpp"
    break;

  case 732: /* simpleType: UINT32_  */
#line 1819 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U4); }
#line 8392 "asmparse.cpp"
    break;

  case 733: /* simpleType: UINT64_  */
#line 1820 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt8(ELEMENT_TYPE_U8); }
#line 8398 "asmparse.cpp"
    break;

  case 734: /* simpleType: TYPEDEF_TS  */
#line 1821 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->append((yyvsp[0].tdd)->m_pbsTypeSpec); }
#line 8404 "asmparse.cpp"
    break;

  case 735: /* bounds1: bound  */
#line 1824 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); }
#line 8410 "asmparse.cpp"
    break;

  case 736: /* bounds1: bounds1 ',' bound  */
#line 1825 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyvsp[-2].binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr); }
#line 8416 "asmparse.cpp"
    break;

  case 737: /* bound: %empty  */
#line 1828 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0x7FFFFFFF); (yyval.binstr)->appendInt32(0x7FFFFFFF);  }
#line 8422 "asmparse.cpp"
    break;

  case 738: /* bound: ELLIPSIS  */
#line 1829 "asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0x7FFFFFFF); (yyval.binstr)->appendInt32(0x7FFFFFFF);  }
#line 8428 "asmparse.cpp"
    break;

  case 739: /* bound: int32  */
#line 1830 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32(0); (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 8434 "asmparse.cpp"
    break;

  case 740: /* bound: int32 ELLIPSIS int32  */
#line 1831 "asmparse.y"
                                                               { FAIL_UNLESS((yyvsp[-2].int32) <= (yyvsp[0].int32), ("lower bound %d must be <= upper bound %d\n", (yyvsp[-2].int32), (yyvsp[0].int32)));
                                                                if ((yyvsp[-2].int32) > (yyvsp[0].int32)) { YYERROR; };
                                                                (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[-2].int32)); (yyval.binstr)->appendInt32((yyvsp[0].int32)-(yyvsp[-2].int32)+1); }
#line 8442 "asmparse.cpp"
    break;

  case 741: /* bound: int32 ELLIPSIS  */
#line 1834 "asmparse.y"
                                                               { (yyval.binstr) = new BinStr(); (yyval.binstr)->appendInt32((yyvsp[-1].int32)); (yyval.binstr)->appendInt32(0x7FFFFFFF); }
#line 8448 "asmparse.cpp"
    break;

  case 742: /* secDecl: _PERMISSION secAction typeSpec '(' nameValPairs ')'  */
#line 1839 "asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-4].secAct), (yyvsp[-3].token), (yyvsp[-1].pair)); }
#line 8454 "asmparse.cpp"
    break;

  case 743: /* secDecl: _PERMISSION secAction typeSpec '=' '{' customBlobDescr '}'  */
#line 1841 "asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-5].secAct), (yyvsp[-4].token), (yyvsp[-1].binstr)); }
#line 8460 "asmparse.cpp"
    break;

  case 744: /* secDecl: _PERMISSION secAction typeSpec  */
#line 1842 "asmparse.y"
                                                              { PASM->AddPermissionDecl((yyvsp[-1].secAct), (yyvsp[0].token), (NVPair *)NULL); }
#line 8466 "asmparse.cpp"
    break;

  case 745: /* secDecl: psetHead bytes ')'  */
#line 1843 "asmparse.y"
                                                              { PASM->AddPermissionSetDecl((yyvsp[-2].secAct), (yyvsp[-1].binstr)); }
#line 8472 "asmparse.cpp"
    break;

  case 746: /* secDecl: _PERMISSIONSET secAction compQstring  */
#line 1845 "asmparse.y"
                                                              { PASM->AddPermissionSetDecl((yyvsp[-1].secAct),BinStrToUnicode((yyvsp[0].binstr),true));}
#line 8478 "asmparse.cpp"
    break;

  case 747: /* secDecl: _PERMISSIONSET secAction '=' '{' secAttrSetBlob '}'  */
#line 1847 "asmparse.y"
                                                              { BinStr* ret = new BinStr();
                                                                ret->insertInt8('.');
                                                                corEmitInt(ret, nSecAttrBlobs);
                                                                ret->append((yyvsp[-1].binstr));
                                                                PASM->AddPermissionSetDecl((yyvsp[-4].secAct),ret);
                                                                nSecAttrBlobs = 0; }
#line 8489 "asmparse.cpp"
    break;

  case 748: /* secAttrSetBlob: %empty  */
#line 1855 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr(); nSecAttrBlobs = 0;}
#line 8495 "asmparse.cpp"
    break;

  case 749: /* secAttrSetBlob: secAttrBlob  */
#line 1856 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[0].binstr); nSecAttrBlobs = 1; }
#line 8501 "asmparse.cpp"
    break;

  case 750: /* secAttrSetBlob: secAttrBlob ',' secAttrSetBlob  */
#line 1857 "asmparse.y"
                                                              { (yyval.binstr) = (yyvsp[-2].binstr); (yyval.binstr)->append((yyvsp[0].binstr)); nSecAttrBlobs++; }
#line 8507 "asmparse.cpp"
    break;

  case 751: /* secAttrBlob: typeSpec '=' '{' customBlobNVPairs '}'  */
#line 1861 "asmparse.y"
                                                              { (yyval.binstr) = PASM->EncodeSecAttr(PASM->ReflectionNotation((yyvsp[-4].token)),(yyvsp[-1].binstr),nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
#line 8514 "asmparse.cpp"
    break;

  case 752: /* secAttrBlob: CLASS_ SQSTRING '=' '{' customBlobNVPairs '}'  */
#line 1864 "asmparse.y"
                                                              { (yyval.binstr) = PASM->EncodeSecAttr((yyvsp[-4].string),(yyvsp[-1].binstr),nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
#line 8521 "asmparse.cpp"
    break;

  case 753: /* psetHead: _PERMISSIONSET secAction '=' '('  */
#line 1868 "asmparse.y"
                                                              { (yyval.secAct) = (yyvsp[-2].secAct); bParsingByteArray = TRUE; }
#line 8527 "asmparse.cpp"
    break;

  case 754: /* psetHead: _PERMISSIONSET secAction BYTEARRAY_ '('  */
#line 1870 "asmparse.y"
                                                              { (yyval.secAct) = (yyvsp[-2].secAct); bParsingByteArray = TRUE; }
#line 8533 "asmparse.cpp"
    break;

  case 755: /* nameValPairs: nameValPair  */
#line 1873 "asmparse.y"
                                                              { (yyval.pair) = (yyvsp[0].pair); }
#line 8539 "asmparse.cpp"
    break;

  case 756: /* nameValPairs: nameValPair ',' nameValPairs  */
#line 1874 "asmparse.y"
                                                              { (yyval.pair) = (yyvsp[-2].pair)->Concat((yyvsp[0].pair)); }
#line 8545 "asmparse.cpp"
    break;

  case 757: /* nameValPair: compQstring '=' caValue  */
#line 1877 "asmparse.y"
                                                              { (yyvsp[-2].binstr)->appendInt8(0); (yyval.pair) = new NVPair((yyvsp[-2].binstr), (yyvsp[0].binstr)); }
#line 8551 "asmparse.cpp"
    break;

  case 758: /* truefalse: TRUE_  */
#line 1880 "asmparse.y"
                                                              { (yyval.int32) = 1; }
#line 8557 "asmparse.cpp"
    break;

  case 759: /* truefalse: FALSE_  */
#line 1881 "asmparse.y"
                                                              { (yyval.int32) = 0; }
#line 8563 "asmparse.cpp"
    break;

  case 760: /* caValue: truefalse  */
#line 1884 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_BOOLEAN);
                                                                (yyval.binstr)->appendInt8((yyvsp[0].int32)); }
#line 8571 "asmparse.cpp"
    break;

  case 761: /* caValue: int32  */
#line 1887 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_I4);
                                                                (yyval.binstr)->appendInt32((yyvsp[0].int32)); }
#line 8579 "asmparse.cpp"
    break;

  case 762: /* caValue: INT32_ '(' int32 ')'  */
#line 1890 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_I4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8587 "asmparse.cpp"
    break;

  case 763: /* caValue: compQstring  */
#line 1893 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_STRING);
                                                                (yyval.binstr)->append((yyvsp[0].binstr)); delete (yyvsp[0].binstr);
                                                                (yyval.binstr)->appendInt8(0); }
#line 8596 "asmparse.cpp"
    break;

  case 764: /* caValue: className '(' INT8_ ':' int32 ')'  */
#line 1897 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(1);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8607 "asmparse.cpp"
    break;

  case 765: /* caValue: className '(' INT16_ ':' int32 ')'  */
#line 1903 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(2);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8618 "asmparse.cpp"
    break;

  case 766: /* caValue: className '(' INT32_ ':' int32 ')'  */
#line 1909 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-5].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8629 "asmparse.cpp"
    break;

  case 767: /* caValue: className '(' int32 ')'  */
#line 1915 "asmparse.y"
                                                              { (yyval.binstr) = new BinStr();
                                                                (yyval.binstr)->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation((yyvsp[-3].token));
                                                                strcpy_s((char *)(yyval.binstr)->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                (yyval.binstr)->appendInt8(4);
                                                                (yyval.binstr)->appendInt32((yyvsp[-1].int32)); }
#line 8640 "asmparse.cpp"
    break;

  case 768: /* secAction: REQUEST_  */
#line 1923 "asmparse.y"
                                                              { (yyval.secAct) = dclRequest; }
#line 8646 "asmparse.cpp"
    break;

  case 769: /* secAction: DEMAND_  */
#line 1924 "asmparse.y"
                                                              { (yyval.secAct) = dclDemand; }
#line 8652 "asmparse.cpp"
    break;

  case 770: /* secAction: ASSERT_  */
#line 1925 "asmparse.y"
                                                              { (yyval.secAct) = dclAssert; }
#line 8658 "asmparse.cpp"
    break;

  case 771: /* secAction: DENY_  */
#line 1926 "asmparse.y"
                                                              { (yyval.secAct) = dclDeny; }
#line 8664 "asmparse.cpp"
    break;

  case 772: /* secAction: PERMITONLY_  */
#line 1927 "asmparse.y"
                                                              { (yyval.secAct) = dclPermitOnly; }
#line 8670 "asmparse.cpp"
    break;

  case 773: /* secAction: LINKCHECK_  */
#line 1928 "asmparse.y"
                                                              { (yyval.secAct) = dclLinktimeCheck; }
#line 8676 "asmparse.cpp"
    break;

  case 774: /* secAction: INHERITCHECK_  */
#line 1929 "asmparse.y"
                                                              { (yyval.secAct) = dclInheritanceCheck; }
#line 8682 "asmparse.cpp"
    break;

  case 775: /* secAction: REQMIN_  */
#line 1930 "asmparse.y"
                                                              { (yyval.secAct) = dclRequestMinimum; }
#line 8688 "asmparse.cpp"
    break;

  case 776: /* secAction: REQOPT_  */
#line 1931 "asmparse.y"
                                                              { (yyval.secAct) = dclRequestOptional; }
#line 8694 "asmparse.cpp"
    break;

  case 777: /* secAction: REQREFUSE_  */
#line 1932 "asmparse.y"
                                                              { (yyval.secAct) = dclRequestRefuse; }
#line 8700 "asmparse.cpp"
    break;

  case 778: /* secAction: PREJITGRANT_  */
#line 1933 "asmparse.y"
                                                              { (yyval.secAct) = dclPrejitGrant; }
#line 8706 "asmparse.cpp"
    break;

  case 779: /* secAction: PREJITDENY_  */
#line 1934 "asmparse.y"
                                                              { (yyval.secAct) = dclPrejitDenied; }
#line 8712 "asmparse.cpp"
    break;

  case 780: /* secAction: NONCASDEMAND_  */
#line 1935 "asmparse.y"
                                                              { (yyval.secAct) = dclNonCasDemand; }
#line 8718 "asmparse.cpp"
    break;

  case 781: /* secAction: NONCASLINKDEMAND_  */
#line 1936 "asmparse.y"
                                                              { (yyval.secAct) = dclNonCasLinkDemand; }
#line 8724 "asmparse.cpp"
    break;

  case 782: /* secAction: NONCASINHERITANCE_  */
#line 1937 "asmparse.y"
                                                              { (yyval.secAct) = dclNonCasInheritance; }
#line 8730 "asmparse.cpp"
    break;

  case 783: /* esHead: _LINE  */
#line 1941 "asmparse.y"
                                                              { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = FALSE; }
#line 8736 "asmparse.cpp"
    break;

  case 784: /* esHead: P_LINE  */
#line 1942 "asmparse.y"
                                                              { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = TRUE; }
#line 8742 "asmparse.cpp"
    break;

  case 785: /* extSourceSpec: esHead int32 SQSTRING  */
#line 1945 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-1].int32);
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8750 "asmparse.cpp"
    break;

  case 786: /* extSourceSpec: esHead int32  */
#line 1948 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[0].int32);
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1); }
#line 8757 "asmparse.cpp"
    break;

  case 787: /* extSourceSpec: esHead int32 ':' int32 SQSTRING  */
#line 1950 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-3].int32);
                                                                PENV->nExtCol=(yyvsp[-1].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8765 "asmparse.cpp"
    break;

  case 788: /* extSourceSpec: esHead int32 ':' int32  */
#line 1953 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-2].int32);
                                                                PENV->nExtCol=(yyvsp[0].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);}
#line 8772 "asmparse.cpp"
    break;

  case 789: /* extSourceSpec: esHead int32 ':' int32 ',' int32 SQSTRING  */
#line 1956 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-5].int32);
                                                                PENV->nExtCol=(yyvsp[-3].int32); PENV->nExtColEnd = (yyvsp[-1].int32);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8780 "asmparse.cpp"
    break;

  case 790: /* extSourceSpec: esHead int32 ':' int32 ',' int32  */
#line 1960 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-4].int32);
                                                                PENV->nExtCol=(yyvsp[-2].int32); PENV->nExtColEnd = (yyvsp[0].int32); }
#line 8787 "asmparse.cpp"
    break;

  case 791: /* extSourceSpec: esHead int32 ',' int32 ':' int32 SQSTRING  */
#line 1963 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-5].int32); PENV->nExtLineEnd = (yyvsp[-3].int32);
                                                                PENV->nExtCol=(yyvsp[-1].int32); PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8795 "asmparse.cpp"
    break;

  case 792: /* extSourceSpec: esHead int32 ',' int32 ':' int32  */
#line 1967 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-4].int32); PENV->nExtLineEnd = (yyvsp[-2].int32);
                                                                PENV->nExtCol=(yyvsp[0].int32); PENV->nExtColEnd = static_cast<unsigned>(-1); }
#line 8802 "asmparse.cpp"
    break;

  case 793: /* extSourceSpec: esHead int32 ',' int32 ':' int32 ',' int32 SQSTRING  */
#line 1970 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-7].int32); PENV->nExtLineEnd = (yyvsp[-5].int32);
                                                                PENV->nExtCol=(yyvsp[-3].int32); PENV->nExtColEnd = (yyvsp[-1].int32);
                                                                PASM->SetSourceFileName((yyvsp[0].string));}
#line 8810 "asmparse.cpp"
    break;

  case 794: /* extSourceSpec: esHead int32 ',' int32 ':' int32 ',' int32  */
#line 1974 "asmparse.y"
                                                              { PENV->nExtLine = (yyvsp[-6].int32); PENV->nExtLineEnd = (yyvsp[-4].int32);
                                                                PENV->nExtCol=(yyvsp[-2].int32); PENV->nExtColEnd = (yyvsp[0].int32); }
#line 8817 "asmparse.cpp"
    break;

  case 795: /* extSourceSpec: esHead int32 QSTRING  */
#line 1976 "asmparse.y"
                                                              { PENV->nExtLine = PENV->nExtLineEnd = (yyvsp[-1].int32) - 1;
                                                                PENV->nExtCol = 0; PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName((yyvsp[0].binstr));}
#line 8825 "asmparse.cpp"
    break;

  case 796: /* fileDecl: _FILE fileAttr dottedName fileEntry hashHead bytes ')' fileEntry  */
#line 1983 "asmparse.y"
                                                              { PASMM->AddFile((yyvsp[-5].string), (yyvsp[-6].fileAttr)|(yyvsp[-4].fileAttr)|(yyvsp[0].fileAttr), (yyvsp[-2].binstr)); }
#line 8831 "asmparse.cpp"
    break;

  case 797: /* fileDecl: _FILE fileAttr dottedName fileEntry  */
#line 1984 "asmparse.y"
                                                              { PASMM->AddFile((yyvsp[-1].string), (yyvsp[-2].fileAttr)|(yyvsp[0].fileAttr), NULL); }
#line 8837 "asmparse.cpp"
    break;

  case 798: /* fileAttr: %empty  */
#line 1987 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0; }
#line 8843 "asmparse.cpp"
    break;

  case 799: /* fileAttr: fileAttr NOMETADATA_  */
#line 1988 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) ((yyvsp[-1].fileAttr) | ffContainsNoMetaData); }
#line 8849 "asmparse.cpp"
    break;

  case 800: /* fileEntry: %empty  */
#line 1991 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0; }
#line 8855 "asmparse.cpp"
    break;

  case 801: /* fileEntry: _ENTRYPOINT  */
#line 1992 "asmparse.y"
                                                              { (yyval.fileAttr) = (CorFileFlags) 0x80000000; }
#line 8861 "asmparse.cpp"
    break;

  case 802: /* hashHead: _HASH '=' '('  */
#line 1995 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8867 "asmparse.cpp"
    break;

  case 803: /* assemblyHead: _ASSEMBLY asmAttr dottedName  */
#line 1998 "asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[0].string), NULL, (DWORD)(yyvsp[-1].asmAttr), FALSE); }
#line 8873 "asmparse.cpp"
    break;

  case 804: /* asmAttr: %empty  */
#line 2001 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) 0; }
#line 8879 "asmparse.cpp"
    break;

  case 805: /* asmAttr: asmAttr RETARGETABLE_  */
#line 2002 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afRetargetable); }
#line 8885 "asmparse.cpp"
    break;

  case 806: /* asmAttr: asmAttr WINDOWSRUNTIME_  */
#line 2003 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afContentType_WindowsRuntime); }
#line 8891 "asmparse.cpp"
    break;

  case 807: /* asmAttr: asmAttr NOPLATFORM_  */
#line 2004 "asmparse.y"
                                                              { (yyval.asmAttr) = (CorAssemblyFlags) ((yyvsp[-1].asmAttr) | afPA_NoPlatform); }
#line 8897 "asmparse.cpp"
    break;

  case 808: /* asmAttr: asmAttr LEGACY_ LIBRARY_  */
#line 2005 "asmparse.y"
                                                              { (yyval.asmAttr) = (yyvsp[-2].asmAttr); }
#line 8903 "asmparse.cpp"
    break;

  case 809: /* asmAttr: asmAttr CIL_  */
#line 2006 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_MSIL); }
#line 8909 "asmparse.cpp"
    break;

  case 810: /* asmAttr: asmAttr X86_  */
#line 2007 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_x86); }
#line 8915 "asmparse.cpp"
    break;

  case 811: /* asmAttr: asmAttr AMD64_  */
#line 2008 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_AMD64); }
#line 8921 "asmparse.cpp"
    break;

  case 812: /* asmAttr: asmAttr ARM_  */
#line 2009 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_ARM); }
#line 8927 "asmparse.cpp"
    break;

  case 813: /* asmAttr: asmAttr ARM64_  */
#line 2010 "asmparse.y"
                                                              { SET_PA((yyval.asmAttr),(yyvsp[-1].asmAttr),afPA_ARM64); }
#line 8933 "asmparse.cpp"
    break;

  case 816: /* assemblyDecl: _HASH ALGORITHM_ int32  */
#line 2017 "asmparse.y"
                                                              { PASMM->SetAssemblyHashAlg((yyvsp[0].int32)); }
#line 8939 "asmparse.cpp"
    break;

  case 819: /* intOrWildcard: int32  */
#line 2022 "asmparse.y"
                                                              { (yyval.int32) = (yyvsp[0].int32); }
#line 8945 "asmparse.cpp"
    break;

  case 820: /* intOrWildcard: '*'  */
#line 2023 "asmparse.y"
                                                              { (yyval.int32) = 0xFFFF; }
#line 8951 "asmparse.cpp"
    break;

  case 821: /* asmOrRefDecl: publicKeyHead bytes ')'  */
#line 2026 "asmparse.y"
                                                              { PASMM->SetAssemblyPublicKey((yyvsp[-1].binstr)); }
#line 8957 "asmparse.cpp"
    break;

  case 822: /* asmOrRefDecl: _VER intOrWildcard ':' intOrWildcard ':' intOrWildcard ':' intOrWildcard  */
#line 2028 "asmparse.y"
                                                              { PASMM->SetAssemblyVer((USHORT)(yyvsp[-6].int32), (USHORT)(yyvsp[-4].int32), (USHORT)(yyvsp[-2].int32), (USHORT)(yyvsp[0].int32)); }
#line 8963 "asmparse.cpp"
    break;

  case 823: /* asmOrRefDecl: _LOCALE compQstring  */
#line 2029 "asmparse.y"
                                                              { (yyvsp[0].binstr)->appendInt8(0); PASMM->SetAssemblyLocale((yyvsp[0].binstr),TRUE); }
#line 8969 "asmparse.cpp"
    break;

  case 824: /* asmOrRefDecl: localeHead bytes ')'  */
#line 2030 "asmparse.y"
                                                              { PASMM->SetAssemblyLocale((yyvsp[-1].binstr),FALSE); }
#line 8975 "asmparse.cpp"
    break;

  case 827: /* publicKeyHead: _PUBLICKEY '=' '('  */
#line 2035 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8981 "asmparse.cpp"
    break;

  case 828: /* publicKeyTokenHead: _PUBLICKEYTOKEN '=' '('  */
#line 2038 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8987 "asmparse.cpp"
    break;

  case 829: /* localeHead: _LOCALE '=' '('  */
#line 2041 "asmparse.y"
                                                              { bParsingByteArray = TRUE; }
#line 8993 "asmparse.cpp"
    break;

  case 830: /* assemblyRefHead: _ASSEMBLY EXTERN_ asmAttr dottedName  */
#line 2045 "asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[0].string), NULL, (yyvsp[-1].asmAttr), TRUE); }
#line 8999 "asmparse.cpp"
    break;

  case 831: /* assemblyRefHead: _ASSEMBLY EXTERN_ asmAttr dottedName AS_ dottedName  */
#line 2047 "asmparse.y"
                                                              { PASMM->StartAssembly((yyvsp[-2].string), (yyvsp[0].string), (yyvsp[-3].asmAttr), TRUE); }
#line 9005 "asmparse.cpp"
    break;

  case 834: /* assemblyRefDecl: hashHead bytes ')'  */
#line 2054 "asmparse.y"
                                                              { PASMM->SetAssemblyHashBlob((yyvsp[-1].binstr)); }
#line 9011 "asmparse.cpp"
    break;

  case 836: /* assemblyRefDecl: publicKeyTokenHead bytes ')'  */
#line 2056 "asmparse.y"
                                                              { PASMM->SetAssemblyPublicKeyToken((yyvsp[-1].binstr)); }
#line 9017 "asmparse.cpp"
    break;

  case 837: /* assemblyRefDecl: AUTO_  */
#line 2057 "asmparse.y"
                                                              { PASMM->SetAssemblyAutodetect(); }
#line 9023 "asmparse.cpp"
    break;

  case 838: /* exptypeHead: _CLASS EXTERN_ exptAttr dottedName  */
#line 2060 "asmparse.y"
                                                              { PASMM->StartComType((yyvsp[0].string), (yyvsp[-1].exptAttr));}
#line 9029 "asmparse.cpp"
    break;

  case 839: /* exportHead: _EXPORT exptAttr dottedName  */
#line 2063 "asmparse.y"
                                                                              { PASMM->StartComType((yyvsp[0].string), (yyvsp[-1].exptAttr)); }
#line 9035 "asmparse.cpp"
    break;

  case 840: /* exptAttr: %empty  */
#line 2066 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) 0; }
#line 9041 "asmparse.cpp"
    break;

  case 841: /* exptAttr: exptAttr PRIVATE_  */
#line 2067 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdNotPublic); }
#line 9047 "asmparse.cpp"
    break;

  case 842: /* exptAttr: exptAttr PUBLIC_  */
#line 2068 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdPublic); }
#line 9053 "asmparse.cpp"
    break;

  case 843: /* exptAttr: exptAttr FORWARDER_  */
#line 2069 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-1].exptAttr) | tdForwarder); }
#line 9059 "asmparse.cpp"
    break;

  case 844: /* exptAttr: exptAttr NESTED_ PUBLIC_  */
#line 2070 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedPublic); }
#line 9065 "asmparse.cpp"
    break;

  case 845: /* exptAttr: exptAttr NESTED_ PRIVATE_  */
#line 2071 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedPrivate); }
#line 9071 "asmparse.cpp"
    break;

  case 846: /* exptAttr: exptAttr NESTED_ FAMILY_  */
#line 2072 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamily); }
#line 9077 "asmparse.cpp"
    break;

  case 847: /* exptAttr: exptAttr NESTED_ ASSEMBLY_  */
#line 2073 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedAssembly); }
#line 9083 "asmparse.cpp"
    break;

  case 848: /* exptAttr: exptAttr NESTED_ FAMANDASSEM_  */
#line 2074 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamANDAssem); }
#line 9089 "asmparse.cpp"
    break;

  case 849: /* exptAttr: exptAttr NESTED_ FAMORASSEM_  */
#line 2075 "asmparse.y"
                                                              { (yyval.exptAttr) = (CorTypeAttr) ((yyvsp[-2].exptAttr) | tdNestedFamORAssem); }
#line 9095 "asmparse.cpp"
    break;

  case 852: /* exptypeDecl: _FILE dottedName  */
#line 2082 "asmparse.y"
                                                              { PASMM->SetComTypeFile((yyvsp[0].string)); }
#line 9101 "asmparse.cpp"
    break;

  case 853: /* exptypeDecl: _CLASS EXTERN_ slashedName  */
#line 2083 "asmparse.y"
                                                               { PASMM->SetComTypeComType((yyvsp[0].string)); }
#line 9107 "asmparse.cpp"
    break;

  case 854: /* exptypeDecl: _ASSEMBLY EXTERN_ dottedName  */
#line 2084 "asmparse.y"
                                                              { PASMM->SetComTypeAsmRef((yyvsp[0].string)); }
#line 9113 "asmparse.cpp"
    break;

  case 855: /* exptypeDecl: MDTOKEN_ '(' int32 ')'  */
#line 2085 "asmparse.y"
                                                              { if(!PASMM->SetComTypeImplementationTok((yyvsp[-1].int32)))
                                                                  PASM->report->error("Invalid implementation of exported type\n"); }
#line 9120 "asmparse.cpp"
    break;

  case 856: /* exptypeDecl: _CLASS int32  */
#line 2087 "asmparse.y"
                                                              { if(!PASMM->SetComTypeClassTok((yyvsp[0].int32)))
                                                                  PASM->report->error("Invalid TypeDefID of exported type\n"); }
#line 9127 "asmparse.cpp"
    break;

  case 859: /* manifestResHead: _MRESOURCE manresAttr dottedName  */
#line 2093 "asmparse.y"
                                                              { PASMM->StartManifestRes((yyvsp[0].string), (yyvsp[0].string), (yyvsp[-1].manresAttr)); }
#line 9133 "asmparse.cpp"
    break;

  case 860: /* manifestResHead: _MRESOURCE manresAttr dottedName AS_ dottedName  */
#line 2095 "asmparse.y"
                                                              { PASMM->StartManifestRes((yyvsp[-2].string), (yyvsp[0].string), (yyvsp[-3].manresAttr)); }
#line 9139 "asmparse.cpp"
    break;

  case 861: /* manresAttr: %empty  */
#line 2098 "asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) 0; }
#line 9145 "asmparse.cpp"
    break;

  case 862: /* manresAttr: manresAttr PUBLIC_  */
#line 2099 "asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) ((yyvsp[-1].manresAttr) | mrPublic); }
#line 9151 "asmparse.cpp"
    break;

  case 863: /* manresAttr: manresAttr PRIVATE_  */
#line 2100 "asmparse.y"
                                                              { (yyval.manresAttr) = (CorManifestResourceFlags) ((yyvsp[-1].manresAttr) | mrPrivate); }
#line 9157 "asmparse.cpp"
    break;

  case 866: /* manifestResDecl: _FILE dottedName AT_ int32  */
#line 2107 "asmparse.y"
                                                              { PASMM->SetManifestResFile((yyvsp[-2].string), (ULONG)(yyvsp[0].int32)); }
#line 9163 "asmparse.cpp"
    break;

  case 867: /* manifestResDecl: _ASSEMBLY EXTERN_ dottedName  */
#line 2108 "asmparse.y"
                                                              { PASMM->SetManifestResAsmRef((yyvsp[0].string)); }
#line 9169 "asmparse.cpp"
    break;


#line 9173 "asmparse.cpp"

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

#line 2113 "asmparse.y"


#include "grammar_after.cpp"
