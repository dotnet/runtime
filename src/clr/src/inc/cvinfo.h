// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***    cvinfo.h - Generic CodeView information definitions
 *
 *      Structures, constants, etc. for accessing and interpreting
 *      CodeView information.
 *
 */


/***    The master copy of this file resides in the langapi project.
 *      All Microsoft projects are required to use the master copy without
 *      modification.  Modification of the master version or a copy
 *      without consultation with all parties concerned is extremely
 *      risky.
 *
 */

#pragma once

#include "cvconst.h"

#ifndef _CV_INFO_INCLUDED
#define _CV_INFO_INCLUDED

#ifdef  __cplusplus
#pragma warning ( disable: 4200 )
#endif

#ifndef __INLINE
#ifdef  __cplusplus
#define __INLINE inline
#else
#define __INLINE __inline
#endif
#endif

#pragma pack ( push, 1 )
typedef unsigned long   CV_uoff32_t;
typedef          long   CV_off32_t;
typedef unsigned short  CV_uoff16_t;
typedef          short  CV_off16_t;
typedef unsigned short  CV_typ16_t;
typedef unsigned long   CV_typ_t;
typedef unsigned long   CV_pubsymflag_t;    // must be same as CV_typ_t.
typedef unsigned short  _2BYTEPAD;
typedef unsigned long   CV_tkn_t;

#if !defined (CV_ZEROLEN)
#define CV_ZEROLEN
#endif

#if !defined (FLOAT10)
#if defined(_M_I86)                    // 16 bit x86 supporting long double
typedef long double FLOAT10;
#else                                  // 32 bit w/o long double support
typedef struct FLOAT10
{
    char b[10];
} FLOAT10;
#endif
#endif


#define CV_SIGNATURE_C6         0L  // Actual signature is >64K
#define CV_SIGNATURE_C7         1L  // First explicit signature
#define CV_SIGNATURE_C11        2L  // C11 (vc5.x) 32-bit types
#define CV_SIGNATURE_C13        4L  // C13 (vc7.x) zero terminated names
#define CV_SIGNATURE_RESERVED   5L  // All signatures from 5 to 64K are reserved

#define CV_MAXOFFSET   0xffffffff

#ifndef GUID_DEFINED
#define GUID_DEFINED

typedef struct _GUID {          // size is 16
    unsigned long   Data1;
    unsigned short  Data2;
    unsigned short  Data3;
    unsigned char   Data4[8];
} GUID;

#endif // !GUID_DEFINED

typedef GUID            SIG70;      // new to 7.0 are 16-byte guid-like signatures
typedef SIG70 *         PSIG70;
typedef const SIG70 *   PCSIG70;



/**     CodeView Symbol and Type OMF type information is broken up into two
 *      ranges.  Type indices less than 0x1000 describe type information
 *      that is frequently used.  Type indices above 0x1000 are used to
 *      describe more complex features such as functions, arrays and
 *      structures.
 */




/**     Primitive types have predefined meaning that is encoded in the
 *      values of the various bit fields in the value.
 *
 *      A CodeView primitive type is defined as:
 *
 *      1 1
 *      1 089  7654  3  210
 *      r mode type  r  sub
 *
 *      Where
 *          mode is the pointer mode
 *          type is a type indicator
 *          sub  is a subtype enumeration
 *          r    is a reserved field
 *
 *      See Microsoft Symbol and Type OMF (Version 4.0) for more
 *      information.
 */


#define CV_MMASK        0x700       // mode mask
#define CV_TMASK        0x0f0       // type mask

// can we use the reserved bit ??
#define CV_SMASK        0x00f       // subtype mask

#define CV_MSHIFT       8           // primitive mode right shift count
#define CV_TSHIFT       4           // primitive type right shift count
#define CV_SSHIFT       0           // primitive subtype right shift count

// macros to extract primitive mode, type and size

#define CV_MODE(typ)    (((typ) & CV_MMASK) >> CV_MSHIFT)
#define CV_TYPE(typ)    (((typ) & CV_TMASK) >> CV_TSHIFT)
#define CV_SUBT(typ)    (((typ) & CV_SMASK) >> CV_SSHIFT)

// macros to insert new primitive mode, type and size

#define CV_NEWMODE(typ, nm)     ((CV_typ_t)(((typ) & ~CV_MMASK) | ((nm) << CV_MSHIFT)))
#define CV_NEWTYPE(typ, nt)     (((typ) & ~CV_TMASK) | ((nt) << CV_TSHIFT))
#define CV_NEWSUBT(typ, ns)     (((typ) & ~CV_SMASK) | ((ns) << CV_SSHIFT))



//     pointer mode enumeration values

typedef enum CV_prmode_e {
    CV_TM_DIRECT = 0,       // mode is not a pointer
    CV_TM_NPTR   = 1,       // mode is a near pointer
    CV_TM_FPTR   = 2,       // mode is a far pointer
    CV_TM_HPTR   = 3,       // mode is a huge pointer
    CV_TM_NPTR32 = 4,       // mode is a 32 bit near pointer
    CV_TM_FPTR32 = 5,       // mode is a 32 bit far pointer
    CV_TM_NPTR64 = 6,       // mode is a 64 bit near pointer
    CV_TM_NPTR128 = 7,      // mode is a 128 bit near pointer
} CV_prmode_e;




//      type enumeration values


typedef enum CV_type_e {
    CV_SPECIAL      = 0x00,         // special type size values
    CV_SIGNED       = 0x01,         // signed integral size values
    CV_UNSIGNED     = 0x02,         // unsigned integral size values
    CV_BOOLEAN      = 0x03,         // Boolean size values
    CV_REAL         = 0x04,         // real number size values
    CV_COMPLEX      = 0x05,         // complex number size values
    CV_SPECIAL2     = 0x06,         // second set of special types
    CV_INT          = 0x07,         // integral (int) values
    CV_CVRESERVED   = 0x0f,
} CV_type_e;




//      subtype enumeration values for CV_SPECIAL


typedef enum CV_special_e {
    CV_SP_NOTYPE    = 0x00,
    CV_SP_ABS       = 0x01,
    CV_SP_SEGMENT   = 0x02,
    CV_SP_VOID      = 0x03,
    CV_SP_CURRENCY  = 0x04,
    CV_SP_NBASICSTR = 0x05,
    CV_SP_FBASICSTR = 0x06,
    CV_SP_NOTTRANS  = 0x07,
    CV_SP_HRESULT   = 0x08,
} CV_special_e;




//      subtype enumeration values for CV_SPECIAL2


typedef enum CV_special2_e {
    CV_S2_BIT       = 0x00,
    CV_S2_PASCHAR   = 0x01,         // Pascal CHAR
    CV_S2_BOOL32FF  = 0x02,         // 32-bit BOOL where true is 0xffffffff
} CV_special2_e;





//      subtype enumeration values for CV_SIGNED, CV_UNSIGNED and CV_BOOLEAN


typedef enum CV_integral_e {
    CV_IN_1BYTE     = 0x00,
    CV_IN_2BYTE     = 0x01,
    CV_IN_4BYTE     = 0x02,
    CV_IN_8BYTE     = 0x03,
    CV_IN_16BYTE    = 0x04
} CV_integral_e;





//      subtype enumeration values for CV_REAL and CV_COMPLEX


typedef enum CV_real_e {
    CV_RC_REAL32    = 0x00,
    CV_RC_REAL64    = 0x01,
    CV_RC_REAL80    = 0x02,
    CV_RC_REAL128   = 0x03,
    CV_RC_REAL48    = 0x04,
    CV_RC_REAL32PP  = 0x05,   // 32-bit partial precision real
    CV_RC_REAL16    = 0x06,
} CV_real_e;




//      subtype enumeration values for CV_INT (really int)


typedef enum CV_int_e {
    CV_RI_CHAR      = 0x00,
    CV_RI_INT1      = 0x00,
    CV_RI_WCHAR     = 0x01,
    CV_RI_UINT1     = 0x01,
    CV_RI_INT2      = 0x02,
    CV_RI_UINT2     = 0x03,
    CV_RI_INT4      = 0x04,
    CV_RI_UINT4     = 0x05,
    CV_RI_INT8      = 0x06,
    CV_RI_UINT8     = 0x07,
    CV_RI_INT16     = 0x08,
    CV_RI_UINT16    = 0x09,
    CV_RI_CHAR16    = 0x0a,  // char16_t
    CV_RI_CHAR32    = 0x0b,  // char32_t
} CV_int_e;



// macros to check the type of a primitive

#define CV_TYP_IS_DIRECT(typ)   (CV_MODE(typ) == CV_TM_DIRECT)
#define CV_TYP_IS_PTR(typ)      (CV_MODE(typ) != CV_TM_DIRECT)
#define CV_TYP_IS_NPTR(typ)     (CV_MODE(typ) == CV_TM_NPTR)
#define CV_TYP_IS_FPTR(typ)     (CV_MODE(typ) == CV_TM_FPTR)
#define CV_TYP_IS_HPTR(typ)     (CV_MODE(typ) == CV_TM_HPTR)
#define CV_TYP_IS_NPTR32(typ)   (CV_MODE(typ) == CV_TM_NPTR32)
#define CV_TYP_IS_FPTR32(typ)   (CV_MODE(typ) == CV_TM_FPTR32)

#define CV_TYP_IS_SIGNED(typ)   (((CV_TYPE(typ) == CV_SIGNED) && CV_TYP_IS_DIRECT(typ)) || \
                                 (typ == T_INT1)  || \
                                 (typ == T_INT2)  || \
                                 (typ == T_INT4)  || \
                                 (typ == T_INT8)  || \
                                 (typ == T_INT16) || \
                                 (typ == T_RCHAR))

#define CV_TYP_IS_UNSIGNED(typ) (((CV_TYPE(typ) == CV_UNSIGNED) && CV_TYP_IS_DIRECT(typ)) || \
                                 (typ == T_UINT1) || \
                                 (typ == T_UINT2) || \
                                 (typ == T_UINT4) || \
                                 (typ == T_UINT8) || \
                                 (typ == T_UINT16))

#define CV_TYP_IS_REAL(typ)     ((CV_TYPE(typ) == CV_REAL)  && CV_TYP_IS_DIRECT(typ))

#define CV_FIRST_NONPRIM 0x1000
#define CV_IS_PRIMITIVE(typ)    ((typ) < CV_FIRST_NONPRIM)
#define CV_TYP_IS_COMPLEX(typ)  ((CV_TYPE(typ) == CV_COMPLEX)   && CV_TYP_IS_DIRECT(typ))
#define CV_IS_INTERNAL_PTR(typ) (CV_IS_PRIMITIVE(typ) && \
                                 CV_TYPE(typ) == CV_CVRESERVED && \
                                 CV_TYP_IS_PTR(typ))






// selected values for type_index - for a more complete definition, see
// Microsoft Symbol and Type OMF document




//      Special Types

typedef enum TYPE_ENUM_e {
//      Special Types

    T_NOTYPE        = 0x0000,   // uncharacterized type (no type)
    T_ABS           = 0x0001,   // absolute symbol
    T_SEGMENT       = 0x0002,   // segment type
    T_VOID          = 0x0003,   // void
    T_HRESULT       = 0x0008,   // OLE/COM HRESULT
    T_32PHRESULT    = 0x0408,   // OLE/COM HRESULT __ptr32 *
    T_64PHRESULT    = 0x0608,   // OLE/COM HRESULT __ptr64 *

    T_PVOID         = 0x0103,   // near pointer to void
    T_PFVOID        = 0x0203,   // far pointer to void
    T_PHVOID        = 0x0303,   // huge pointer to void
    T_32PVOID       = 0x0403,   // 32 bit pointer to void
    T_32PFVOID      = 0x0503,   // 16:32 pointer to void
    T_64PVOID       = 0x0603,   // 64 bit pointer to void
    T_CURRENCY      = 0x0004,   // BASIC 8 byte currency value
    T_NBASICSTR     = 0x0005,   // Near BASIC string
    T_FBASICSTR     = 0x0006,   // Far BASIC string
    T_NOTTRANS      = 0x0007,   // type not translated by cvpack
    T_BIT           = 0x0060,   // bit
    T_PASCHAR       = 0x0061,   // Pascal CHAR
    T_BOOL32FF      = 0x0062,   // 32-bit BOOL where true is 0xffffffff


//      Character types

    T_CHAR          = 0x0010,   // 8 bit signed
    T_PCHAR         = 0x0110,   // 16 bit pointer to 8 bit signed
    T_PFCHAR        = 0x0210,   // 16:16 far pointer to 8 bit signed
    T_PHCHAR        = 0x0310,   // 16:16 huge pointer to 8 bit signed
    T_32PCHAR       = 0x0410,   // 32 bit pointer to 8 bit signed
    T_32PFCHAR      = 0x0510,   // 16:32 pointer to 8 bit signed
    T_64PCHAR       = 0x0610,   // 64 bit pointer to 8 bit signed

    T_UCHAR         = 0x0020,   // 8 bit unsigned
    T_PUCHAR        = 0x0120,   // 16 bit pointer to 8 bit unsigned
    T_PFUCHAR       = 0x0220,   // 16:16 far pointer to 8 bit unsigned
    T_PHUCHAR       = 0x0320,   // 16:16 huge pointer to 8 bit unsigned
    T_32PUCHAR      = 0x0420,   // 32 bit pointer to 8 bit unsigned
    T_32PFUCHAR     = 0x0520,   // 16:32 pointer to 8 bit unsigned
    T_64PUCHAR      = 0x0620,   // 64 bit pointer to 8 bit unsigned


//      really a character types

    T_RCHAR         = 0x0070,   // really a char
    T_PRCHAR        = 0x0170,   // 16 bit pointer to a real char
    T_PFRCHAR       = 0x0270,   // 16:16 far pointer to a real char
    T_PHRCHAR       = 0x0370,   // 16:16 huge pointer to a real char
    T_32PRCHAR      = 0x0470,   // 32 bit pointer to a real char
    T_32PFRCHAR     = 0x0570,   // 16:32 pointer to a real char
    T_64PRCHAR      = 0x0670,   // 64 bit pointer to a real char


//      really a wide character types

    T_WCHAR         = 0x0071,   // wide char
    T_PWCHAR        = 0x0171,   // 16 bit pointer to a wide char
    T_PFWCHAR       = 0x0271,   // 16:16 far pointer to a wide char
    T_PHWCHAR       = 0x0371,   // 16:16 huge pointer to a wide char
    T_32PWCHAR      = 0x0471,   // 32 bit pointer to a wide char
    T_32PFWCHAR     = 0x0571,   // 16:32 pointer to a wide char
    T_64PWCHAR      = 0x0671,   // 64 bit pointer to a wide char

//      really a 16-bit unicode char

    T_CHAR16         = 0x007a,   // 16-bit unicode char
    T_PCHAR16        = 0x017a,   // 16 bit pointer to a 16-bit unicode char
    T_PFCHAR16       = 0x027a,   // 16:16 far pointer to a 16-bit unicode char
    T_PHCHAR16       = 0x037a,   // 16:16 huge pointer to a 16-bit unicode char
    T_32PCHAR16      = 0x047a,   // 32 bit pointer to a 16-bit unicode char
    T_32PFCHAR16     = 0x057a,   // 16:32 pointer to a 16-bit unicode char
    T_64PCHAR16      = 0x067a,   // 64 bit pointer to a 16-bit unicode char

//      really a 32-bit unicode char

    T_CHAR32         = 0x007b,   // 32-bit unicode char
    T_PCHAR32        = 0x017b,   // 16 bit pointer to a 32-bit unicode char
    T_PFCHAR32       = 0x027b,   // 16:16 far pointer to a 32-bit unicode char
    T_PHCHAR32       = 0x037b,   // 16:16 huge pointer to a 32-bit unicode char
    T_32PCHAR32      = 0x047b,   // 32 bit pointer to a 32-bit unicode char
    T_32PFCHAR32     = 0x057b,   // 16:32 pointer to a 32-bit unicode char
    T_64PCHAR32      = 0x067b,   // 64 bit pointer to a 32-bit unicode char

//      8 bit int types

    T_INT1          = 0x0068,   // 8 bit signed int
    T_PINT1         = 0x0168,   // 16 bit pointer to 8 bit signed int
    T_PFINT1        = 0x0268,   // 16:16 far pointer to 8 bit signed int
    T_PHINT1        = 0x0368,   // 16:16 huge pointer to 8 bit signed int
    T_32PINT1       = 0x0468,   // 32 bit pointer to 8 bit signed int
    T_32PFINT1      = 0x0568,   // 16:32 pointer to 8 bit signed int
    T_64PINT1       = 0x0668,   // 64 bit pointer to 8 bit signed int

    T_UINT1         = 0x0069,   // 8 bit unsigned int
    T_PUINT1        = 0x0169,   // 16 bit pointer to 8 bit unsigned int
    T_PFUINT1       = 0x0269,   // 16:16 far pointer to 8 bit unsigned int
    T_PHUINT1       = 0x0369,   // 16:16 huge pointer to 8 bit unsigned int
    T_32PUINT1      = 0x0469,   // 32 bit pointer to 8 bit unsigned int
    T_32PFUINT1     = 0x0569,   // 16:32 pointer to 8 bit unsigned int
    T_64PUINT1      = 0x0669,   // 64 bit pointer to 8 bit unsigned int


//      16 bit short types

    T_SHORT         = 0x0011,   // 16 bit signed
    T_PSHORT        = 0x0111,   // 16 bit pointer to 16 bit signed
    T_PFSHORT       = 0x0211,   // 16:16 far pointer to 16 bit signed
    T_PHSHORT       = 0x0311,   // 16:16 huge pointer to 16 bit signed
    T_32PSHORT      = 0x0411,   // 32 bit pointer to 16 bit signed
    T_32PFSHORT     = 0x0511,   // 16:32 pointer to 16 bit signed
    T_64PSHORT      = 0x0611,   // 64 bit pointer to 16 bit signed

    T_USHORT        = 0x0021,   // 16 bit unsigned
    T_PUSHORT       = 0x0121,   // 16 bit pointer to 16 bit unsigned
    T_PFUSHORT      = 0x0221,   // 16:16 far pointer to 16 bit unsigned
    T_PHUSHORT      = 0x0321,   // 16:16 huge pointer to 16 bit unsigned
    T_32PUSHORT     = 0x0421,   // 32 bit pointer to 16 bit unsigned
    T_32PFUSHORT    = 0x0521,   // 16:32 pointer to 16 bit unsigned
    T_64PUSHORT     = 0x0621,   // 64 bit pointer to 16 bit unsigned


//      16 bit int types

    T_INT2          = 0x0072,   // 16 bit signed int
    T_PINT2         = 0x0172,   // 16 bit pointer to 16 bit signed int
    T_PFINT2        = 0x0272,   // 16:16 far pointer to 16 bit signed int
    T_PHINT2        = 0x0372,   // 16:16 huge pointer to 16 bit signed int
    T_32PINT2       = 0x0472,   // 32 bit pointer to 16 bit signed int
    T_32PFINT2      = 0x0572,   // 16:32 pointer to 16 bit signed int
    T_64PINT2       = 0x0672,   // 64 bit pointer to 16 bit signed int

    T_UINT2         = 0x0073,   // 16 bit unsigned int
    T_PUINT2        = 0x0173,   // 16 bit pointer to 16 bit unsigned int
    T_PFUINT2       = 0x0273,   // 16:16 far pointer to 16 bit unsigned int
    T_PHUINT2       = 0x0373,   // 16:16 huge pointer to 16 bit unsigned int
    T_32PUINT2      = 0x0473,   // 32 bit pointer to 16 bit unsigned int
    T_32PFUINT2     = 0x0573,   // 16:32 pointer to 16 bit unsigned int
    T_64PUINT2      = 0x0673,   // 64 bit pointer to 16 bit unsigned int


//      32 bit long types

    T_LONG          = 0x0012,   // 32 bit signed
    T_ULONG         = 0x0022,   // 32 bit unsigned
    T_PLONG         = 0x0112,   // 16 bit pointer to 32 bit signed
    T_PULONG        = 0x0122,   // 16 bit pointer to 32 bit unsigned
    T_PFLONG        = 0x0212,   // 16:16 far pointer to 32 bit signed
    T_PFULONG       = 0x0222,   // 16:16 far pointer to 32 bit unsigned
    T_PHLONG        = 0x0312,   // 16:16 huge pointer to 32 bit signed
    T_PHULONG       = 0x0322,   // 16:16 huge pointer to 32 bit unsigned

    T_32PLONG       = 0x0412,   // 32 bit pointer to 32 bit signed
    T_32PULONG      = 0x0422,   // 32 bit pointer to 32 bit unsigned
    T_32PFLONG      = 0x0512,   // 16:32 pointer to 32 bit signed
    T_32PFULONG     = 0x0522,   // 16:32 pointer to 32 bit unsigned
    T_64PLONG       = 0x0612,   // 64 bit pointer to 32 bit signed
    T_64PULONG      = 0x0622,   // 64 bit pointer to 32 bit unsigned


//      32 bit int types

    T_INT4          = 0x0074,   // 32 bit signed int
    T_PINT4         = 0x0174,   // 16 bit pointer to 32 bit signed int
    T_PFINT4        = 0x0274,   // 16:16 far pointer to 32 bit signed int
    T_PHINT4        = 0x0374,   // 16:16 huge pointer to 32 bit signed int
    T_32PINT4       = 0x0474,   // 32 bit pointer to 32 bit signed int
    T_32PFINT4      = 0x0574,   // 16:32 pointer to 32 bit signed int
    T_64PINT4       = 0x0674,   // 64 bit pointer to 32 bit signed int

    T_UINT4         = 0x0075,   // 32 bit unsigned int
    T_PUINT4        = 0x0175,   // 16 bit pointer to 32 bit unsigned int
    T_PFUINT4       = 0x0275,   // 16:16 far pointer to 32 bit unsigned int
    T_PHUINT4       = 0x0375,   // 16:16 huge pointer to 32 bit unsigned int
    T_32PUINT4      = 0x0475,   // 32 bit pointer to 32 bit unsigned int
    T_32PFUINT4     = 0x0575,   // 16:32 pointer to 32 bit unsigned int
    T_64PUINT4      = 0x0675,   // 64 bit pointer to 32 bit unsigned int


//      64 bit quad types

    T_QUAD          = 0x0013,   // 64 bit signed
    T_PQUAD         = 0x0113,   // 16 bit pointer to 64 bit signed
    T_PFQUAD        = 0x0213,   // 16:16 far pointer to 64 bit signed
    T_PHQUAD        = 0x0313,   // 16:16 huge pointer to 64 bit signed
    T_32PQUAD       = 0x0413,   // 32 bit pointer to 64 bit signed
    T_32PFQUAD      = 0x0513,   // 16:32 pointer to 64 bit signed
    T_64PQUAD       = 0x0613,   // 64 bit pointer to 64 bit signed

    T_UQUAD         = 0x0023,   // 64 bit unsigned
    T_PUQUAD        = 0x0123,   // 16 bit pointer to 64 bit unsigned
    T_PFUQUAD       = 0x0223,   // 16:16 far pointer to 64 bit unsigned
    T_PHUQUAD       = 0x0323,   // 16:16 huge pointer to 64 bit unsigned
    T_32PUQUAD      = 0x0423,   // 32 bit pointer to 64 bit unsigned
    T_32PFUQUAD     = 0x0523,   // 16:32 pointer to 64 bit unsigned
    T_64PUQUAD      = 0x0623,   // 64 bit pointer to 64 bit unsigned


//      64 bit int types

    T_INT8          = 0x0076,   // 64 bit signed int
    T_PINT8         = 0x0176,   // 16 bit pointer to 64 bit signed int
    T_PFINT8        = 0x0276,   // 16:16 far pointer to 64 bit signed int
    T_PHINT8        = 0x0376,   // 16:16 huge pointer to 64 bit signed int
    T_32PINT8       = 0x0476,   // 32 bit pointer to 64 bit signed int
    T_32PFINT8      = 0x0576,   // 16:32 pointer to 64 bit signed int
    T_64PINT8       = 0x0676,   // 64 bit pointer to 64 bit signed int

    T_UINT8         = 0x0077,   // 64 bit unsigned int
    T_PUINT8        = 0x0177,   // 16 bit pointer to 64 bit unsigned int
    T_PFUINT8       = 0x0277,   // 16:16 far pointer to 64 bit unsigned int
    T_PHUINT8       = 0x0377,   // 16:16 huge pointer to 64 bit unsigned int
    T_32PUINT8      = 0x0477,   // 32 bit pointer to 64 bit unsigned int
    T_32PFUINT8     = 0x0577,   // 16:32 pointer to 64 bit unsigned int
    T_64PUINT8      = 0x0677,   // 64 bit pointer to 64 bit unsigned int


//      128 bit octet types

    T_OCT           = 0x0014,   // 128 bit signed
    T_POCT          = 0x0114,   // 16 bit pointer to 128 bit signed
    T_PFOCT         = 0x0214,   // 16:16 far pointer to 128 bit signed
    T_PHOCT         = 0x0314,   // 16:16 huge pointer to 128 bit signed
    T_32POCT        = 0x0414,   // 32 bit pointer to 128 bit signed
    T_32PFOCT       = 0x0514,   // 16:32 pointer to 128 bit signed
    T_64POCT        = 0x0614,   // 64 bit pointer to 128 bit signed

    T_UOCT          = 0x0024,   // 128 bit unsigned
    T_PUOCT         = 0x0124,   // 16 bit pointer to 128 bit unsigned
    T_PFUOCT        = 0x0224,   // 16:16 far pointer to 128 bit unsigned
    T_PHUOCT        = 0x0324,   // 16:16 huge pointer to 128 bit unsigned
    T_32PUOCT       = 0x0424,   // 32 bit pointer to 128 bit unsigned
    T_32PFUOCT      = 0x0524,   // 16:32 pointer to 128 bit unsigned
    T_64PUOCT       = 0x0624,   // 64 bit pointer to 128 bit unsigned


//      128 bit int types

    T_INT16         = 0x0078,   // 128 bit signed int
    T_PINT16        = 0x0178,   // 16 bit pointer to 128 bit signed int
    T_PFINT16       = 0x0278,   // 16:16 far pointer to 128 bit signed int
    T_PHINT16       = 0x0378,   // 16:16 huge pointer to 128 bit signed int
    T_32PINT16      = 0x0478,   // 32 bit pointer to 128 bit signed int
    T_32PFINT16     = 0x0578,   // 16:32 pointer to 128 bit signed int
    T_64PINT16      = 0x0678,   // 64 bit pointer to 128 bit signed int

    T_UINT16        = 0x0079,   // 128 bit unsigned int
    T_PUINT16       = 0x0179,   // 16 bit pointer to 128 bit unsigned int
    T_PFUINT16      = 0x0279,   // 16:16 far pointer to 128 bit unsigned int
    T_PHUINT16      = 0x0379,   // 16:16 huge pointer to 128 bit unsigned int
    T_32PUINT16     = 0x0479,   // 32 bit pointer to 128 bit unsigned int
    T_32PFUINT16    = 0x0579,   // 16:32 pointer to 128 bit unsigned int
    T_64PUINT16     = 0x0679,   // 64 bit pointer to 128 bit unsigned int


//      16 bit real types

    T_REAL16        = 0x0046,   // 16 bit real
    T_PREAL16       = 0x0146,   // 16 bit pointer to 16 bit real
    T_PFREAL16      = 0x0246,   // 16:16 far pointer to 16 bit real
    T_PHREAL16      = 0x0346,   // 16:16 huge pointer to 16 bit real
    T_32PREAL16     = 0x0446,   // 32 bit pointer to 16 bit real
    T_32PFREAL16    = 0x0546,   // 16:32 pointer to 16 bit real
    T_64PREAL16     = 0x0646,   // 64 bit pointer to 16 bit real


//      32 bit real types

    T_REAL32        = 0x0040,   // 32 bit real
    T_PREAL32       = 0x0140,   // 16 bit pointer to 32 bit real
    T_PFREAL32      = 0x0240,   // 16:16 far pointer to 32 bit real
    T_PHREAL32      = 0x0340,   // 16:16 huge pointer to 32 bit real
    T_32PREAL32     = 0x0440,   // 32 bit pointer to 32 bit real
    T_32PFREAL32    = 0x0540,   // 16:32 pointer to 32 bit real
    T_64PREAL32     = 0x0640,   // 64 bit pointer to 32 bit real


//      32 bit partial-precision real types

    T_REAL32PP      = 0x0045,   // 32 bit PP real
    T_PREAL32PP     = 0x0145,   // 16 bit pointer to 32 bit PP real
    T_PFREAL32PP    = 0x0245,   // 16:16 far pointer to 32 bit PP real
    T_PHREAL32PP    = 0x0345,   // 16:16 huge pointer to 32 bit PP real
    T_32PREAL32PP   = 0x0445,   // 32 bit pointer to 32 bit PP real
    T_32PFREAL32PP  = 0x0545,   // 16:32 pointer to 32 bit PP real
    T_64PREAL32PP   = 0x0645,   // 64 bit pointer to 32 bit PP real


//      48 bit real types

    T_REAL48        = 0x0044,   // 48 bit real
    T_PREAL48       = 0x0144,   // 16 bit pointer to 48 bit real
    T_PFREAL48      = 0x0244,   // 16:16 far pointer to 48 bit real
    T_PHREAL48      = 0x0344,   // 16:16 huge pointer to 48 bit real
    T_32PREAL48     = 0x0444,   // 32 bit pointer to 48 bit real
    T_32PFREAL48    = 0x0544,   // 16:32 pointer to 48 bit real
    T_64PREAL48     = 0x0644,   // 64 bit pointer to 48 bit real


//      64 bit real types

    T_REAL64        = 0x0041,   // 64 bit real
    T_PREAL64       = 0x0141,   // 16 bit pointer to 64 bit real
    T_PFREAL64      = 0x0241,   // 16:16 far pointer to 64 bit real
    T_PHREAL64      = 0x0341,   // 16:16 huge pointer to 64 bit real
    T_32PREAL64     = 0x0441,   // 32 bit pointer to 64 bit real
    T_32PFREAL64    = 0x0541,   // 16:32 pointer to 64 bit real
    T_64PREAL64     = 0x0641,   // 64 bit pointer to 64 bit real


//      80 bit real types

    T_REAL80        = 0x0042,   // 80 bit real
    T_PREAL80       = 0x0142,   // 16 bit pointer to 80 bit real
    T_PFREAL80      = 0x0242,   // 16:16 far pointer to 80 bit real
    T_PHREAL80      = 0x0342,   // 16:16 huge pointer to 80 bit real
    T_32PREAL80     = 0x0442,   // 32 bit pointer to 80 bit real
    T_32PFREAL80    = 0x0542,   // 16:32 pointer to 80 bit real
    T_64PREAL80     = 0x0642,   // 64 bit pointer to 80 bit real


//      128 bit real types

    T_REAL128       = 0x0043,   // 128 bit real
    T_PREAL128      = 0x0143,   // 16 bit pointer to 128 bit real
    T_PFREAL128     = 0x0243,   // 16:16 far pointer to 128 bit real
    T_PHREAL128     = 0x0343,   // 16:16 huge pointer to 128 bit real
    T_32PREAL128    = 0x0443,   // 32 bit pointer to 128 bit real
    T_32PFREAL128   = 0x0543,   // 16:32 pointer to 128 bit real
    T_64PREAL128    = 0x0643,   // 64 bit pointer to 128 bit real


//      32 bit complex types

    T_CPLX32        = 0x0050,   // 32 bit complex
    T_PCPLX32       = 0x0150,   // 16 bit pointer to 32 bit complex
    T_PFCPLX32      = 0x0250,   // 16:16 far pointer to 32 bit complex
    T_PHCPLX32      = 0x0350,   // 16:16 huge pointer to 32 bit complex
    T_32PCPLX32     = 0x0450,   // 32 bit pointer to 32 bit complex
    T_32PFCPLX32    = 0x0550,   // 16:32 pointer to 32 bit complex
    T_64PCPLX32     = 0x0650,   // 64 bit pointer to 32 bit complex


//      64 bit complex types

    T_CPLX64        = 0x0051,   // 64 bit complex
    T_PCPLX64       = 0x0151,   // 16 bit pointer to 64 bit complex
    T_PFCPLX64      = 0x0251,   // 16:16 far pointer to 64 bit complex
    T_PHCPLX64      = 0x0351,   // 16:16 huge pointer to 64 bit complex
    T_32PCPLX64     = 0x0451,   // 32 bit pointer to 64 bit complex
    T_32PFCPLX64    = 0x0551,   // 16:32 pointer to 64 bit complex
    T_64PCPLX64     = 0x0651,   // 64 bit pointer to 64 bit complex


//      80 bit complex types

    T_CPLX80        = 0x0052,   // 80 bit complex
    T_PCPLX80       = 0x0152,   // 16 bit pointer to 80 bit complex
    T_PFCPLX80      = 0x0252,   // 16:16 far pointer to 80 bit complex
    T_PHCPLX80      = 0x0352,   // 16:16 huge pointer to 80 bit complex
    T_32PCPLX80     = 0x0452,   // 32 bit pointer to 80 bit complex
    T_32PFCPLX80    = 0x0552,   // 16:32 pointer to 80 bit complex
    T_64PCPLX80     = 0x0652,   // 64 bit pointer to 80 bit complex


//      128 bit complex types

    T_CPLX128       = 0x0053,   // 128 bit complex
    T_PCPLX128      = 0x0153,   // 16 bit pointer to 128 bit complex
    T_PFCPLX128     = 0x0253,   // 16:16 far pointer to 128 bit complex
    T_PHCPLX128     = 0x0353,   // 16:16 huge pointer to 128 bit real
    T_32PCPLX128    = 0x0453,   // 32 bit pointer to 128 bit complex
    T_32PFCPLX128   = 0x0553,   // 16:32 pointer to 128 bit complex
    T_64PCPLX128    = 0x0653,   // 64 bit pointer to 128 bit complex


//      boolean types

    T_BOOL08        = 0x0030,   // 8 bit boolean
    T_PBOOL08       = 0x0130,   // 16 bit pointer to  8 bit boolean
    T_PFBOOL08      = 0x0230,   // 16:16 far pointer to  8 bit boolean
    T_PHBOOL08      = 0x0330,   // 16:16 huge pointer to  8 bit boolean
    T_32PBOOL08     = 0x0430,   // 32 bit pointer to 8 bit boolean
    T_32PFBOOL08    = 0x0530,   // 16:32 pointer to 8 bit boolean
    T_64PBOOL08     = 0x0630,   // 64 bit pointer to 8 bit boolean

    T_BOOL16        = 0x0031,   // 16 bit boolean
    T_PBOOL16       = 0x0131,   // 16 bit pointer to 16 bit boolean
    T_PFBOOL16      = 0x0231,   // 16:16 far pointer to 16 bit boolean
    T_PHBOOL16      = 0x0331,   // 16:16 huge pointer to 16 bit boolean
    T_32PBOOL16     = 0x0431,   // 32 bit pointer to 18 bit boolean
    T_32PFBOOL16    = 0x0531,   // 16:32 pointer to 16 bit boolean
    T_64PBOOL16     = 0x0631,   // 64 bit pointer to 18 bit boolean

    T_BOOL32        = 0x0032,   // 32 bit boolean
    T_PBOOL32       = 0x0132,   // 16 bit pointer to 32 bit boolean
    T_PFBOOL32      = 0x0232,   // 16:16 far pointer to 32 bit boolean
    T_PHBOOL32      = 0x0332,   // 16:16 huge pointer to 32 bit boolean
    T_32PBOOL32     = 0x0432,   // 32 bit pointer to 32 bit boolean
    T_32PFBOOL32    = 0x0532,   // 16:32 pointer to 32 bit boolean
    T_64PBOOL32     = 0x0632,   // 64 bit pointer to 32 bit boolean

    T_BOOL64        = 0x0033,   // 64 bit boolean
    T_PBOOL64       = 0x0133,   // 16 bit pointer to 64 bit boolean
    T_PFBOOL64      = 0x0233,   // 16:16 far pointer to 64 bit boolean
    T_PHBOOL64      = 0x0333,   // 16:16 huge pointer to 64 bit boolean
    T_32PBOOL64     = 0x0433,   // 32 bit pointer to 64 bit boolean
    T_32PFBOOL64    = 0x0533,   // 16:32 pointer to 64 bit boolean
    T_64PBOOL64     = 0x0633,   // 64 bit pointer to 64 bit boolean


//      ???

    T_NCVPTR        = 0x01f0,   // CV Internal type for created near pointers
    T_FCVPTR        = 0x02f0,   // CV Internal type for created far pointers
    T_HCVPTR        = 0x03f0,   // CV Internal type for created huge pointers
    T_32NCVPTR      = 0x04f0,   // CV Internal type for created near 32-bit pointers
    T_32FCVPTR      = 0x05f0,   // CV Internal type for created far 32-bit pointers
    T_64NCVPTR      = 0x06f0,   // CV Internal type for created near 64-bit pointers

} TYPE_ENUM_e;

/**     No leaf index can have a value of 0x0000.  The leaf indices are
 *      separated into ranges depending upon the use of the type record.
 *      The second range is for the type records that are directly referenced
 *      in symbols. The first range is for type records that are not
 *      referenced by symbols but instead are referenced by other type
 *      records.  All type records must have a starting leaf index in these
 *      first two ranges.  The third range of leaf indices are used to build
 *      up complex lists such as the field list of a class type record.  No
 *      type record can begin with one of the leaf indices. The fourth ranges
 *      of type indices are used to represent numeric data in a symbol or
 *      type record. These leaf indices are greater than 0x8000.  At the
 *      point that type or symbol processor is expecting a numeric field, the
 *      next two bytes in the type record are examined.  If the value is less
 *      than 0x8000, then the two bytes contain the numeric value.  If the
 *      value is greater than 0x8000, then the data follows the leaf index in
 *      a format specified by the leaf index. The final range of leaf indices
 *      are used to force alignment of subfields within a complex type record..
 */


typedef enum LEAF_ENUM_e {
    // leaf indices starting records but referenced from symbol records

    LF_MODIFIER_16t     = 0x0001,
    LF_POINTER_16t      = 0x0002,
    LF_ARRAY_16t        = 0x0003,
    LF_CLASS_16t        = 0x0004,
    LF_STRUCTURE_16t    = 0x0005,
    LF_UNION_16t        = 0x0006,
    LF_ENUM_16t         = 0x0007,
    LF_PROCEDURE_16t    = 0x0008,
    LF_MFUNCTION_16t    = 0x0009,
    LF_VTSHAPE          = 0x000a,
    LF_COBOL0_16t       = 0x000b,
    LF_COBOL1           = 0x000c,
    LF_BARRAY_16t       = 0x000d,
    LF_LABEL            = 0x000e,
    LF_NULL             = 0x000f,
    LF_NOTTRAN          = 0x0010,
    LF_DIMARRAY_16t     = 0x0011,
    LF_VFTPATH_16t      = 0x0012,
    LF_PRECOMP_16t      = 0x0013,       // not referenced from symbol
    LF_ENDPRECOMP       = 0x0014,       // not referenced from symbol
    LF_OEM_16t          = 0x0015,       // oem definable type string
    LF_TYPESERVER_ST    = 0x0016,       // not referenced from symbol

    // leaf indices starting records but referenced only from type records

    LF_SKIP_16t         = 0x0200,
    LF_ARGLIST_16t      = 0x0201,
    LF_DEFARG_16t       = 0x0202,
    LF_LIST             = 0x0203,
    LF_FIELDLIST_16t    = 0x0204,
    LF_DERIVED_16t      = 0x0205,
    LF_BITFIELD_16t     = 0x0206,
    LF_METHODLIST_16t   = 0x0207,
    LF_DIMCONU_16t      = 0x0208,
    LF_DIMCONLU_16t     = 0x0209,
    LF_DIMVARU_16t      = 0x020a,
    LF_DIMVARLU_16t     = 0x020b,
    LF_REFSYM           = 0x020c,

    LF_BCLASS_16t       = 0x0400,
    LF_VBCLASS_16t      = 0x0401,
    LF_IVBCLASS_16t     = 0x0402,
    LF_ENUMERATE_ST     = 0x0403,
    LF_FRIENDFCN_16t    = 0x0404,
    LF_INDEX_16t        = 0x0405,
    LF_MEMBER_16t       = 0x0406,
    LF_STMEMBER_16t     = 0x0407,
    LF_METHOD_16t       = 0x0408,
    LF_NESTTYPE_16t     = 0x0409,
    LF_VFUNCTAB_16t     = 0x040a,
    LF_FRIENDCLS_16t    = 0x040b,
    LF_ONEMETHOD_16t    = 0x040c,
    LF_VFUNCOFF_16t     = 0x040d,

// 32-bit type index versions of leaves, all have the 0x1000 bit set
//
    LF_TI16_MAX         = 0x1000,

    LF_MODIFIER         = 0x1001,
    LF_POINTER          = 0x1002,
    LF_ARRAY_ST         = 0x1003,
    LF_CLASS_ST         = 0x1004,
    LF_STRUCTURE_ST     = 0x1005,
    LF_UNION_ST         = 0x1006,
    LF_ENUM_ST          = 0x1007,
    LF_PROCEDURE        = 0x1008,
    LF_MFUNCTION        = 0x1009,
    LF_COBOL0           = 0x100a,
    LF_BARRAY           = 0x100b,
    LF_DIMARRAY_ST      = 0x100c,
    LF_VFTPATH          = 0x100d,
    LF_PRECOMP_ST       = 0x100e,       // not referenced from symbol
    LF_OEM              = 0x100f,       // oem definable type string
    LF_ALIAS_ST         = 0x1010,       // alias (typedef) type
    LF_OEM2             = 0x1011,       // oem definable type string

    // leaf indices starting records but referenced only from type records

    LF_SKIP             = 0x1200,
    LF_ARGLIST          = 0x1201,
    LF_DEFARG_ST        = 0x1202,
    LF_FIELDLIST        = 0x1203,
    LF_DERIVED          = 0x1204,
    LF_BITFIELD         = 0x1205,
    LF_METHODLIST       = 0x1206,
    LF_DIMCONU          = 0x1207,
    LF_DIMCONLU         = 0x1208,
    LF_DIMVARU          = 0x1209,
    LF_DIMVARLU         = 0x120a,

    LF_BCLASS           = 0x1400,
    LF_VBCLASS          = 0x1401,
    LF_IVBCLASS         = 0x1402,
    LF_FRIENDFCN_ST     = 0x1403,
    LF_INDEX            = 0x1404,
    LF_MEMBER_ST        = 0x1405,
    LF_STMEMBER_ST      = 0x1406,
    LF_METHOD_ST        = 0x1407,
    LF_NESTTYPE_ST      = 0x1408,
    LF_VFUNCTAB         = 0x1409,
    LF_FRIENDCLS        = 0x140a,
    LF_ONEMETHOD_ST     = 0x140b,
    LF_VFUNCOFF         = 0x140c,
    LF_NESTTYPEEX_ST    = 0x140d,
    LF_MEMBERMODIFY_ST  = 0x140e,
    LF_MANAGED_ST       = 0x140f,

    // Types w/ SZ names

    LF_ST_MAX           = 0x1500,

    LF_TYPESERVER       = 0x1501,       // not referenced from symbol
    LF_ENUMERATE        = 0x1502,
    LF_ARRAY            = 0x1503,
    LF_CLASS            = 0x1504,
    LF_STRUCTURE        = 0x1505,
    LF_UNION            = 0x1506,
    LF_ENUM             = 0x1507,
    LF_DIMARRAY         = 0x1508,
    LF_PRECOMP          = 0x1509,       // not referenced from symbol
    LF_ALIAS            = 0x150a,       // alias (typedef) type
    LF_DEFARG           = 0x150b,
    LF_FRIENDFCN        = 0x150c,
    LF_MEMBER           = 0x150d,
    LF_STMEMBER         = 0x150e,
    LF_METHOD           = 0x150f,
    LF_NESTTYPE         = 0x1510,
    LF_ONEMETHOD        = 0x1511,
    LF_NESTTYPEEX       = 0x1512,
    LF_MEMBERMODIFY     = 0x1513,
    LF_MANAGED          = 0x1514,
    LF_TYPESERVER2      = 0x1515,

    LF_STRIDED_ARRAY    = 0x1516,    // same as LF_ARRAY, but with stride between adjacent elements
    LF_HLSL             = 0x1517,
    LF_MODIFIER_EX      = 0x1518,
    LF_INTERFACE        = 0x1519,
    LF_BINTERFACE       = 0x151a,
    LF_VECTOR           = 0x151b,
    LF_MATRIX           = 0x151c,

    LF_VFTABLE          = 0x151d,      // a virtual function table
    LF_ENDOFLEAFRECORD  = LF_VFTABLE,

    LF_TYPE_LAST,                    // one greater than the last type record
    LF_TYPE_MAX         = LF_TYPE_LAST - 1,

    LF_FUNC_ID          = 0x1601,    // global func ID
    LF_MFUNC_ID         = 0x1602,    // member func ID
    LF_BUILDINFO        = 0x1603,    // build info: tool, version, command line, src/pdb file
    LF_SUBSTR_LIST      = 0x1604,    // similar to LF_ARGLIST, for list of sub strings
    LF_STRING_ID        = 0x1605,    // string ID

    LF_UDT_SRC_LINE     = 0x1606,    // source and line on where an UDT is defined
                                     // only generated by compiler

    LF_UDT_MOD_SRC_LINE = 0x1607,    // module, source and line on where an UDT is defined
                                     // only generated by linker

    LF_ID_LAST,                      // one greater than the last ID record
    LF_ID_MAX           = LF_ID_LAST - 1,

    LF_NUMERIC          = 0x8000,
    LF_CHAR             = 0x8000,
    LF_SHORT            = 0x8001,
    LF_USHORT           = 0x8002,
    LF_LONG             = 0x8003,
    LF_ULONG            = 0x8004,
    LF_REAL32           = 0x8005,
    LF_REAL64           = 0x8006,
    LF_REAL80           = 0x8007,
    LF_REAL128          = 0x8008,
    LF_QUADWORD         = 0x8009,
    LF_UQUADWORD        = 0x800a,
    LF_REAL48           = 0x800b,
    LF_COMPLEX32        = 0x800c,
    LF_COMPLEX64        = 0x800d,
    LF_COMPLEX80        = 0x800e,
    LF_COMPLEX128       = 0x800f,
    LF_VARSTRING        = 0x8010,

    LF_OCTWORD          = 0x8017,
    LF_UOCTWORD         = 0x8018,

    LF_DECIMAL          = 0x8019,
    LF_DATE             = 0x801a,
    LF_UTF8STRING       = 0x801b,

    LF_REAL16           = 0x801c,
    
    LF_PAD0             = 0xf0,
    LF_PAD1             = 0xf1,
    LF_PAD2             = 0xf2,
    LF_PAD3             = 0xf3,
    LF_PAD4             = 0xf4,
    LF_PAD5             = 0xf5,
    LF_PAD6             = 0xf6,
    LF_PAD7             = 0xf7,
    LF_PAD8             = 0xf8,
    LF_PAD9             = 0xf9,
    LF_PAD10            = 0xfa,
    LF_PAD11            = 0xfb,
    LF_PAD12            = 0xfc,
    LF_PAD13            = 0xfd,
    LF_PAD14            = 0xfe,
    LF_PAD15            = 0xff,

} LEAF_ENUM_e;

// end of leaf indices




//      Type enum for pointer records
//      Pointers can be one of the following types


typedef enum CV_ptrtype_e {
    CV_PTR_NEAR         = 0x00, // 16 bit pointer
    CV_PTR_FAR          = 0x01, // 16:16 far pointer
    CV_PTR_HUGE         = 0x02, // 16:16 huge pointer
    CV_PTR_BASE_SEG     = 0x03, // based on segment
    CV_PTR_BASE_VAL     = 0x04, // based on value of base
    CV_PTR_BASE_SEGVAL  = 0x05, // based on segment value of base
    CV_PTR_BASE_ADDR    = 0x06, // based on address of base
    CV_PTR_BASE_SEGADDR = 0x07, // based on segment address of base
    CV_PTR_BASE_TYPE    = 0x08, // based on type
    CV_PTR_BASE_SELF    = 0x09, // based on self
    CV_PTR_NEAR32       = 0x0a, // 32 bit pointer
    CV_PTR_FAR32        = 0x0b, // 16:32 pointer
    CV_PTR_64           = 0x0c, // 64 bit pointer
    CV_PTR_UNUSEDPTR    = 0x0d  // first unused pointer type
} CV_ptrtype_e;





//      Mode enum for pointers
//      Pointers can have one of the following modes
//
//  To support for l-value and r-value reference, we added CV_PTR_MODE_LVREF
//  and CV_PTR_MODE_RVREF.  CV_PTR_MODE_REF should be removed at some point.
//  We keep it now so that old code that uses it won't be broken.
//  

typedef enum CV_ptrmode_e {
    CV_PTR_MODE_PTR     = 0x00, // "normal" pointer
    CV_PTR_MODE_REF     = 0x01, // "old" reference
    CV_PTR_MODE_LVREF   = 0x01, // l-value reference
    CV_PTR_MODE_PMEM    = 0x02, // pointer to data member
    CV_PTR_MODE_PMFUNC  = 0x03, // pointer to member function
    CV_PTR_MODE_RVREF   = 0x04, // r-value reference
    CV_PTR_MODE_RESERVED= 0x05  // first unused pointer mode
} CV_ptrmode_e;


//      enumeration for pointer-to-member types

typedef enum CV_pmtype_e {
    CV_PMTYPE_Undef     = 0x00, // not specified (pre VC8)
    CV_PMTYPE_D_Single  = 0x01, // member data, single inheritance
    CV_PMTYPE_D_Multiple= 0x02, // member data, multiple inheritance
    CV_PMTYPE_D_Virtual = 0x03, // member data, virtual inheritance
    CV_PMTYPE_D_General = 0x04, // member data, most general
    CV_PMTYPE_F_Single  = 0x05, // member function, single inheritance
    CV_PMTYPE_F_Multiple= 0x06, // member function, multiple inheritance
    CV_PMTYPE_F_Virtual = 0x07, // member function, virtual inheritance
    CV_PMTYPE_F_General = 0x08, // member function, most general
} CV_pmtype_e;

//      enumeration for method properties

typedef enum CV_methodprop_e {
    CV_MTvanilla        = 0x00,
    CV_MTvirtual        = 0x01,
    CV_MTstatic         = 0x02,
    CV_MTfriend         = 0x03,
    CV_MTintro          = 0x04,
    CV_MTpurevirt       = 0x05,
    CV_MTpureintro      = 0x06
} CV_methodprop_e;




//      enumeration for virtual shape table entries

typedef enum CV_VTS_desc_e {
    CV_VTS_near         = 0x00,
    CV_VTS_far          = 0x01,
    CV_VTS_thin         = 0x02,
    CV_VTS_outer        = 0x03,
    CV_VTS_meta         = 0x04,
    CV_VTS_near32       = 0x05,
    CV_VTS_far32        = 0x06,
    CV_VTS_unused       = 0x07
} CV_VTS_desc_e;




//      enumeration for LF_LABEL address modes

typedef enum CV_LABEL_TYPE_e {
    CV_LABEL_NEAR = 0,       // near return
    CV_LABEL_FAR  = 4        // far return
} CV_LABEL_TYPE_e;



//      enumeration for LF_MODIFIER values


typedef struct CV_modifier_t {
    unsigned short  MOD_const       :1;
    unsigned short  MOD_volatile    :1;
    unsigned short  MOD_unaligned   :1;
    unsigned short  MOD_unused      :13;
} CV_modifier_t;




//  enumeration for HFA kinds

typedef enum CV_HFA_e {
   CV_HFA_none   =  0,
   CV_HFA_float  =  1,
   CV_HFA_double =  2,
   CV_HFA_other  =  3
} CV_HFA_e;

//  enumeration for MoCOM UDT kinds

typedef enum CV_MOCOM_UDT_e {
    CV_MOCOM_UDT_none      = 0,
    CV_MOCOM_UDT_ref       = 1,
    CV_MOCOM_UDT_value     = 2,
    CV_MOCOM_UDT_interface = 3
} CV_MOCOM_UDT_e;

//  bit field structure describing class/struct/union/enum properties

typedef struct CV_prop_t {
    unsigned short  packed      :1;     // true if structure is packed
    unsigned short  ctor        :1;     // true if constructors or destructors present
    unsigned short  ovlops      :1;     // true if overloaded operators present
    unsigned short  isnested    :1;     // true if this is a nested class
    unsigned short  cnested     :1;     // true if this class contains nested types
    unsigned short  opassign    :1;     // true if overloaded assignment (=)
    unsigned short  opcast      :1;     // true if casting methods
    unsigned short  fwdref      :1;     // true if forward reference (incomplete defn)
    unsigned short  scoped      :1;     // scoped definition
    unsigned short  hasuniquename :1;   // true if there is a decorated name following the regular name
    unsigned short  sealed      :1;     // true if class cannot be used as a base class
    unsigned short  hfa         :2;     // CV_HFA_e
    unsigned short  intrinsic   :1;     // true if class is an intrinsic type (e.g. __m128d)
    unsigned short  mocom       :2;     // CV_MOCOM_UDT_e
} CV_prop_t;




//  class field attribute

typedef struct CV_fldattr_t {
    unsigned short  access      :2;     // access protection CV_access_t
    unsigned short  mprop       :3;     // method properties CV_methodprop_t
    unsigned short  pseudo      :1;     // compiler generated fcn and does not exist
    unsigned short  noinherit   :1;     // true if class cannot be inherited
    unsigned short  noconstruct :1;     // true if class cannot be constructed
    unsigned short  compgenx    :1;     // compiler generated fcn and does exist
    unsigned short  sealed      :1;     // true if method cannot be overridden
    unsigned short  unused      :6;     // unused
} CV_fldattr_t;


//  function flags

typedef struct CV_funcattr_t {
    unsigned char  cxxreturnudt :1;  // true if C++ style ReturnUDT
    unsigned char  ctor         :1;  // true if func is an instance constructor
    unsigned char  ctorvbase    :1;  // true if func is an instance constructor of a class with virtual bases
    unsigned char  unused       :5;  // unused
} CV_funcattr_t;


//  matrix flags

typedef struct CV_matrixattr_t {
    unsigned char  row_major   :1;   // true if matrix has row-major layout (column-major is default)
    unsigned char  unused      :7;   // unused
} CV_matrixattr_t;


//  Structures to access to the type records


typedef struct TYPTYPE {
    unsigned short  len;
    unsigned short  leaf;
    unsigned char   data[CV_ZEROLEN];
} TYPTYPE;          // general types record

__INLINE char *NextType ( _In_ char * pType) {
    return (pType + ((TYPTYPE *)pType)->len + sizeof(unsigned short));
}

typedef enum CV_PMEMBER {
    CV_PDM16_NONVIRT    = 0x00, // 16:16 data no virtual fcn or base
    CV_PDM16_VFCN       = 0x01, // 16:16 data with virtual functions
    CV_PDM16_VBASE      = 0x02, // 16:16 data with virtual bases
    CV_PDM32_NVVFCN     = 0x03, // 16:32 data w/wo virtual functions
    CV_PDM32_VBASE      = 0x04, // 16:32 data with virtual bases

    CV_PMF16_NEARNVSA   = 0x05, // 16:16 near method nonvirtual single address point
    CV_PMF16_NEARNVMA   = 0x06, // 16:16 near method nonvirtual multiple address points
    CV_PMF16_NEARVBASE  = 0x07, // 16:16 near method virtual bases
    CV_PMF16_FARNVSA    = 0x08, // 16:16 far method nonvirtual single address point
    CV_PMF16_FARNVMA    = 0x09, // 16:16 far method nonvirtual multiple address points
    CV_PMF16_FARVBASE   = 0x0a, // 16:16 far method virtual bases

    CV_PMF32_NVSA       = 0x0b, // 16:32 method nonvirtual single address point
    CV_PMF32_NVMA       = 0x0c, // 16:32 method nonvirtual multiple address point
    CV_PMF32_VBASE      = 0x0d  // 16:32 method virtual bases
} CV_PMEMBER;



//  memory representation of pointer to member.  These representations are
//  indexed by the enumeration above in the LF_POINTER record




//  representation of a 16:16 pointer to data for a class with no
//  virtual functions or virtual bases


struct CV_PDMR16_NONVIRT {
    CV_off16_t      mdisp;      // displacement to data (NULL = -1)
};




//  representation of a 16:16 pointer to data for a class with virtual
//  functions


struct CV_PMDR16_VFCN {
    CV_off16_t      mdisp;      // displacement to data ( NULL = 0)
};




//  representation of a 16:16 pointer to data for a class with
//  virtual bases


struct CV_PDMR16_VBASE {
    CV_off16_t      mdisp;      // displacement to data
    CV_off16_t      pdisp;      // this pointer displacement to vbptr
    CV_off16_t      vdisp;      // displacement within vbase table
                                // NULL = (,,0xffff)
};




//  representation of a 32 bit pointer to data for a class with
//  or without virtual functions and no virtual bases


struct CV_PDMR32_NVVFCN {
    CV_off32_t      mdisp;      // displacement to data (NULL = 0x80000000)
};




//  representation of a 32 bit pointer to data for a class
//  with virtual bases


struct CV_PDMR32_VBASE {
    CV_off32_t      mdisp;      // displacement to data
    CV_off32_t      pdisp;      // this pointer displacement
    CV_off32_t      vdisp;      // vbase table displacement
                                // NULL = (,,0xffffffff)
};




//  representation of a 16:16 pointer to near member function for a
//  class with no virtual functions or bases and a single address point


struct CV_PMFR16_NEARNVSA {
    CV_uoff16_t     off;        // near address of function (NULL = 0)
};



//  representation of a 16 bit pointer to member functions of a
//  class with no virtual bases and multiple address points


struct CV_PMFR16_NEARNVMA {
    CV_uoff16_t     off;        // offset of function (NULL = 0,x)
    signed short    disp;
};




//  representation of a 16 bit pointer to member function of a
//  class with virtual bases


struct CV_PMFR16_NEARVBASE {
    CV_uoff16_t     off;        // offset of function (NULL = 0,x,x,x)
    CV_off16_t      mdisp;      // displacement to data
    CV_off16_t      pdisp;      // this pointer displacement
    CV_off16_t      vdisp;      // vbase table displacement
};




//  representation of a 16:16 pointer to far member function for a
//  class with no virtual bases and a single address point


struct CV_PMFR16_FARNVSA {
    CV_uoff16_t     off;        // offset of function (NULL = 0:0)
    unsigned short  seg;        // segment of function
};




//  representation of a 16:16 far pointer to member functions of a
//  class with no virtual bases and multiple address points


struct CV_PMFR16_FARNVMA {
    CV_uoff16_t     off;        // offset of function (NULL = 0:0,x)
    unsigned short  seg;
    signed short    disp;
};




//  representation of a 16:16 far pointer to member function of a
//  class with virtual bases


struct CV_PMFR16_FARVBASE {
    CV_uoff16_t     off;        // offset of function (NULL = 0:0,x,x,x)
    unsigned short  seg;
    CV_off16_t      mdisp;      // displacement to data
    CV_off16_t      pdisp;      // this pointer displacement
    CV_off16_t      vdisp;      // vbase table displacement

};




//  representation of a 32 bit pointer to member function for a
//  class with no virtual bases and a single address point


struct CV_PMFR32_NVSA {
    CV_uoff32_t      off;        // near address of function (NULL = 0L)
};




//  representation of a 32 bit pointer to member function for a
//  class with no virtual bases and multiple address points


struct CV_PMFR32_NVMA {
    CV_uoff32_t     off;        // near address of function (NULL = 0L,x)
    CV_off32_t      disp;
};




//  representation of a 32 bit pointer to member function for a
//  class with virtual bases


struct CV_PMFR32_VBASE {
    CV_uoff32_t     off;        // near address of function (NULL = 0L,x,x,x)
    CV_off32_t      mdisp;      // displacement to data
    CV_off32_t      pdisp;      // this pointer displacement
    CV_off32_t      vdisp;      // vbase table displacement
};





//  Easy leaf - used for generic casting to reference leaf field
//  of a subfield of a complex list

typedef struct lfEasy {
    unsigned short  leaf;           // LF_...
} lfEasy;


/**     The following type records are basically variant records of the
 *      above structure.  The "unsigned short leaf" of the above structure and
 *      the "unsigned short leaf" of the following type definitions are the same
 *      symbol.  When the OMF record is locked via the MHOMFLock API
 *      call, the address of the "unsigned short leaf" is returned
 */

/**     Notes on alignment
 *      Alignment of the fields in most of the type records is done on the
 *      basis of the TYPTYPE record base.  That is why in most of the lf*
 *      records that the CV_typ_t (32-bit types) is located on what appears to
 *      be a offset mod 4 == 2 boundary.  The exception to this rule are those
 *      records that are in a list (lfFieldList, lfMethodList), which are
 *      aligned to their own bases since they don't have the length field
 */

/**** Change log for 16-bit to 32-bit type and symbol records

    Record type         Change (f == field arrangement, p = padding added)
    ----------------------------------------------------------------------
    lfModifer           f
    lfPointer           fp
    lfClass             f
    lfStructure         f
    lfUnion             f
    lfEnum              f
    lfVFTPath           p
    lfPreComp           p
    lfOEM               p
    lfArgList           p
    lfDerived           p
    mlMethod            p   (method list member)
    lfBitField          f
    lfDimCon            f
    lfDimVar            p
    lfIndex             p   (field list member)
    lfBClass            f   (field list member)
    lfVBClass           f   (field list member)
    lfFriendCls         p   (field list member)
    lfFriendFcn         p   (field list member)
    lfMember            f   (field list member)
    lfSTMember          f   (field list member)
    lfVFuncTab          p   (field list member)
    lfVFuncOff          p   (field list member)
    lfNestType          p   (field list member)

    DATASYM32           f
    PROCSYM32           f
    VPATHSYM32          f
    REGREL32            f
    THREADSYM32         f
    PROCSYMMIPS         f


*/

//      Type record for LF_MODIFIER

typedef struct lfModifier_16t {
    unsigned short  leaf;           // LF_MODIFIER_16t
    CV_modifier_t   attr;           // modifier attribute modifier_t
    CV_typ16_t      type;           // modified type
} lfModifier_16t;

typedef struct lfModifier {
    unsigned short  leaf;           // LF_MODIFIER
    CV_typ_t        type;           // modified type
    CV_modifier_t   attr;           // modifier attribute modifier_t
} lfModifier;




//      type record for LF_POINTER

#ifndef __cplusplus
typedef struct lfPointer_16t {
#endif
    struct lfPointerBody_16t {
        unsigned short      leaf;           // LF_POINTER_16t
        struct lfPointerAttr_16t {
            unsigned char   ptrtype     :5; // ordinal specifying pointer type (CV_ptrtype_e)
            unsigned char   ptrmode     :3; // ordinal specifying pointer mode (CV_ptrmode_e)
            unsigned char   isflat32    :1; // true if 0:32 pointer
            unsigned char   isvolatile  :1; // TRUE if volatile pointer
            unsigned char   isconst     :1; // TRUE if const pointer
            unsigned char   isunaligned :1; // TRUE if unaligned pointer
            unsigned char   unused      :4;
        } attr;
        CV_typ16_t  utype;          // type index of the underlying type
#if (defined(__cplusplus) || defined(_MSC_VER)) // for C++ and MS compilers that support unnamed unions
    };
#else
    } u;
#endif
#ifdef  __cplusplus
typedef struct lfPointer_16t : public lfPointerBody_16t {
#endif
    union {
        struct {
            CV_typ16_t      pmclass;    // index of containing class for pointer to member
            unsigned short  pmenum;     // enumeration specifying pm format (CV_pmtype_e)
        } pm;
        unsigned short      bseg;       // base segment if PTR_BASE_SEG
        unsigned char       Sym[1];     // copy of base symbol record (including length)
        struct  {
            CV_typ16_t      index;      // type index if CV_PTR_BASE_TYPE
            unsigned char   name[1];    // name of base type
        } btype;
    } pbase;
} lfPointer_16t;

#ifndef __cplusplus
typedef struct lfPointer {
#endif
    struct lfPointerBody {
        unsigned short      leaf;           // LF_POINTER
        CV_typ_t            utype;          // type index of the underlying type
        struct lfPointerAttr {
            unsigned long   ptrtype     :5; // ordinal specifying pointer type (CV_ptrtype_e)
            unsigned long   ptrmode     :3; // ordinal specifying pointer mode (CV_ptrmode_e)
            unsigned long   isflat32    :1; // true if 0:32 pointer
            unsigned long   isvolatile  :1; // TRUE if volatile pointer
            unsigned long   isconst     :1; // TRUE if const pointer
            unsigned long   isunaligned :1; // TRUE if unaligned pointer
            unsigned long   isrestrict  :1; // TRUE if restricted pointer (allow agressive opts)
            unsigned long   size        :6; // size of pointer (in bytes)
            unsigned long   ismocom     :1; // TRUE if it is a MoCOM pointer (^ or %)
            unsigned long   islref      :1; // TRUE if it is this pointer of member function with & ref-qualifier
            unsigned long   isrref      :1; // TRUE if it is this pointer of member function with && ref-qualifier
            unsigned long   unused      :10;// pad out to 32-bits for following cv_typ_t's
        } attr;
#if (defined(__cplusplus) || defined(_MSC_VER)) // for C++ and MS compilers that support unnamed unions
    };
#else
    } u;
#endif
#ifdef  __cplusplus
typedef struct lfPointer : public lfPointerBody {
#endif
    union {
        struct {
            CV_typ_t        pmclass;    // index of containing class for pointer to member
            unsigned short  pmenum;     // enumeration specifying pm format (CV_pmtype_e)
        } pm;
        unsigned short      bseg;       // base segment if PTR_BASE_SEG
        unsigned char       Sym[1];     // copy of base symbol record (including length)
        struct  {
            CV_typ_t        index;      // type index if CV_PTR_BASE_TYPE
            unsigned char   name[1];    // name of base type
        } btype;
    } pbase;
} lfPointer;




//      type record for LF_ARRAY


typedef struct lfArray_16t {
    unsigned short  leaf;           // LF_ARRAY_16t
    CV_typ16_t      elemtype;       // type index of element type
    CV_typ16_t      idxtype;        // type index of indexing type
    unsigned char   data[CV_ZEROLEN];         // variable length data specifying
                                    // size in bytes and name
} lfArray_16t;

typedef struct lfArray {
    unsigned short  leaf;           // LF_ARRAY
    CV_typ_t        elemtype;       // type index of element type
    CV_typ_t        idxtype;        // type index of indexing type
    unsigned char   data[CV_ZEROLEN];         // variable length data specifying
                                    // size in bytes and name
} lfArray;

typedef struct lfStridedArray {
    unsigned short  leaf;           // LF_STRIDED_ARRAY
    CV_typ_t        elemtype;       // type index of element type
    CV_typ_t        idxtype;        // type index of indexing type
    unsigned long   stride;
    unsigned char   data[CV_ZEROLEN];         // variable length data specifying
                                    // size in bytes and name
} lfStridedArray;




//      type record for LF_VECTOR


typedef struct lfVector {
    unsigned short  leaf;           // LF_VECTOR
    CV_typ_t        elemtype;       // type index of element type
    unsigned long   count;          // number of elements in the vector
    unsigned char   data[CV_ZEROLEN];         // variable length data specifying
                                    // size in bytes and name
} lfVector;




//      type record for LF_MATRIX


typedef struct lfMatrix {
    unsigned short  leaf;           // LF_MATRIX
    CV_typ_t        elemtype;       // type index of element type
    unsigned long   rows;           // number of rows
    unsigned long   cols;           // number of columns
    unsigned long   majorStride;
    CV_matrixattr_t matattr;        // attributes
    unsigned char   data[CV_ZEROLEN];         // variable length data specifying
                                    // size in bytes and name
} lfMatrix;




//      type record for LF_CLASS, LF_STRUCTURE


typedef struct lfClass_16t {
    unsigned short  leaf;           // LF_CLASS_16t, LF_STRUCT_16t
    unsigned short  count;          // count of number of elements in class
    CV_typ16_t      field;          // type index of LF_FIELD descriptor list
    CV_prop_t       property;       // property attribute field (prop_t)
    CV_typ16_t      derived;        // type index of derived from list if not zero
    CV_typ16_t      vshape;         // type index of vshape table for this class
    unsigned char   data[CV_ZEROLEN];         // data describing length of structure in
                                    // bytes and name
} lfClass_16t;
typedef lfClass_16t lfStructure_16t;


typedef struct lfClass {
    unsigned short  leaf;           // LF_CLASS, LF_STRUCT, LF_INTERFACE
    unsigned short  count;          // count of number of elements in class
    CV_prop_t       property;       // property attribute field (prop_t)
    CV_typ_t        field;          // type index of LF_FIELD descriptor list
    CV_typ_t        derived;        // type index of derived from list if not zero
    CV_typ_t        vshape;         // type index of vshape table for this class
    unsigned char   data[CV_ZEROLEN];         // data describing length of structure in
                                    // bytes and name
} lfClass;
typedef lfClass lfStructure;
typedef lfClass lfInterface;

//      type record for LF_UNION


typedef struct lfUnion_16t {
    unsigned short  leaf;           // LF_UNION_16t
    unsigned short  count;          // count of number of elements in class
    CV_typ16_t      field;          // type index of LF_FIELD descriptor list
    CV_prop_t       property;       // property attribute field
    unsigned char   data[CV_ZEROLEN];         // variable length data describing length of
                                    // structure and name
} lfUnion_16t;


typedef struct lfUnion {
    unsigned short  leaf;           // LF_UNION
    unsigned short  count;          // count of number of elements in class
    CV_prop_t       property;       // property attribute field
    CV_typ_t        field;          // type index of LF_FIELD descriptor list
    unsigned char   data[CV_ZEROLEN];         // variable length data describing length of
                                    // structure and name
} lfUnion;


//      type record for LF_ALIAS

typedef struct lfAlias {
    unsigned short  leaf;           // LF_ALIAS
    CV_typ_t        utype;          // underlying type
    unsigned char   Name[1];        // alias name
} lfAlias;

// Item Id is a stricter typeindex which may referenced from symbol stream.
// The code item always had a name.

typedef CV_typ_t CV_ItemId;

typedef struct lfFuncId {
    unsigned short  leaf;       // LF_FUNC_ID
    CV_ItemId       scopeId;    // parent scope of the ID, 0 if global
    CV_typ_t        type;       // function type
    unsigned char   name[CV_ZEROLEN]; 
} lfFuncId;

typedef struct lfMFuncId {
    unsigned short  leaf;       // LF_MFUNC_ID
    CV_typ_t        parentType; // type index of parent
    CV_typ_t        type;       // function type
    unsigned char   name[CV_ZEROLEN]; 
} lfMFuncId;

typedef struct lfStringId {
    unsigned short  leaf;       // LF_STRING_ID
    CV_ItemId       id;         // ID to list of sub string IDs
    unsigned char   name[CV_ZEROLEN];
} lfStringId;

typedef struct lfUdtSrcLine {
    unsigned short leaf;        // LF_UDT_SRC_LINE
    CV_typ_t       type;        // UDT's type index
    CV_ItemId      src;         // index to LF_STRING_ID record where source file name is saved
    unsigned long  line;        // line number
} lfUdtSrcLine;

typedef struct lfUdtModSrcLine {
    unsigned short leaf;        // LF_UDT_MOD_SRC_LINE
    CV_typ_t       type;        // UDT's type index
    CV_ItemId      src;         // index into string table where source file name is saved
    unsigned long  line;        // line number
    unsigned short imod;        // module that contributes this UDT definition 
} lfUdtModSrcLine;

typedef enum CV_BuildInfo_e {
    CV_BuildInfo_CurrentDirectory = 0,
    CV_BuildInfo_BuildTool        = 1,    // Cl.exe
    CV_BuildInfo_SourceFile       = 2,    // foo.cpp
    CV_BuildInfo_ProgramDatabaseFile = 3, // foo.pdb
    CV_BuildInfo_CommandArguments = 4,    // -I etc
    CV_BUILDINFO_KNOWN
} CV_BuildInfo_e;

// type record for build information

typedef struct lfBuildInfo {
    unsigned short  leaf;                    // LF_BUILDINFO
    unsigned short  count;                   // number of arguments
    CV_ItemId       arg[CV_BUILDINFO_KNOWN]; // arguments as CodeItemId
} lfBuildInfo;

//      type record for LF_MANAGED

typedef struct lfManaged {
    unsigned short  leaf;           // LF_MANAGED
    unsigned char   Name[1];        // utf8, zero terminated managed type name
} lfManaged;


//      type record for LF_ENUM


typedef struct lfEnum_16t {
    unsigned short  leaf;           // LF_ENUM_16t
    unsigned short  count;          // count of number of elements in class
    CV_typ16_t      utype;          // underlying type of the enum
    CV_typ16_t      field;          // type index of LF_FIELD descriptor list
    CV_prop_t       property;       // property attribute field
    unsigned char   Name[1];        // length prefixed name of enum
} lfEnum_16t;

typedef struct lfEnum {
    unsigned short  leaf;           // LF_ENUM
    unsigned short  count;          // count of number of elements in class
    CV_prop_t       property;       // property attribute field
    CV_typ_t        utype;          // underlying type of the enum
    CV_typ_t        field;          // type index of LF_FIELD descriptor list
    unsigned char   Name[1];        // length prefixed name of enum
} lfEnum;



//      Type record for LF_PROCEDURE


typedef struct lfProc_16t {
    unsigned short  leaf;           // LF_PROCEDURE_16t
    CV_typ16_t      rvtype;         // type index of return value
    unsigned char   calltype;       // calling convention (CV_call_t)
    CV_funcattr_t   funcattr;       // attributes
    unsigned short  parmcount;      // number of parameters
    CV_typ16_t      arglist;        // type index of argument list
} lfProc_16t;

typedef struct lfProc {
    unsigned short  leaf;           // LF_PROCEDURE
    CV_typ_t        rvtype;         // type index of return value
    unsigned char   calltype;       // calling convention (CV_call_t)
    CV_funcattr_t   funcattr;       // attributes
    unsigned short  parmcount;      // number of parameters
    CV_typ_t        arglist;        // type index of argument list
} lfProc;



//      Type record for member function


typedef struct lfMFunc_16t {
    unsigned short  leaf;           // LF_MFUNCTION_16t
    CV_typ16_t      rvtype;         // type index of return value
    CV_typ16_t      classtype;      // type index of containing class
    CV_typ16_t      thistype;       // type index of this pointer (model specific)
    unsigned char   calltype;       // calling convention (call_t)
    CV_funcattr_t   funcattr;       // attributes
    unsigned short  parmcount;      // number of parameters
    CV_typ16_t      arglist;        // type index of argument list
    long            thisadjust;     // this adjuster (long because pad required anyway)
} lfMFunc_16t;

typedef struct lfMFunc {
    unsigned short  leaf;           // LF_MFUNCTION
    CV_typ_t        rvtype;         // type index of return value
    CV_typ_t        classtype;      // type index of containing class
    CV_typ_t        thistype;       // type index of this pointer (model specific)
    unsigned char   calltype;       // calling convention (call_t)
    CV_funcattr_t   funcattr;       // attributes
    unsigned short  parmcount;      // number of parameters
    CV_typ_t        arglist;        // type index of argument list
    long            thisadjust;     // this adjuster (long because pad required anyway)
} lfMFunc;




//     type record for virtual function table shape


typedef struct lfVTShape {
    unsigned short  leaf;       // LF_VTSHAPE
    unsigned short  count;      // number of entries in vfunctable
    unsigned char   desc[CV_ZEROLEN];     // 4 bit (CV_VTS_desc) descriptors
} lfVTShape;

//     type record for a virtual function table
typedef struct lfVftable {
    unsigned short  leaf;             // LF_VFTABLE
    CV_typ_t        type;             // class/structure that owns the vftable
    CV_typ_t        baseVftable;      // vftable from which this vftable is derived
    unsigned long   offsetInObjectLayout; // offset of the vfptr to this table, relative to the start of the object layout.
    unsigned long   len;              // length of the Names array below in bytes.
    unsigned char   Names[1];         // array of names.
                                      // The first is the name of the vtable.
                                      // The others are the names of the methods.
                                      // TS-TODO: replace a name with a NamedCodeItem once Weiping is done, to
                                      //    avoid duplication of method names.
} lfVftable;

//      type record for cobol0


typedef struct lfCobol0_16t {
    unsigned short  leaf;       // LF_COBOL0_16t
    CV_typ16_t      type;       // parent type record index
    unsigned char   data[CV_ZEROLEN];
} lfCobol0_16t;

typedef struct lfCobol0 {
    unsigned short  leaf;       // LF_COBOL0
    CV_typ_t        type;       // parent type record index
    unsigned char   data[CV_ZEROLEN];
} lfCobol0;




//      type record for cobol1


typedef struct lfCobol1 {
    unsigned short  leaf;       // LF_COBOL1
    unsigned char   data[CV_ZEROLEN];
} lfCobol1;




//      type record for basic array


typedef struct lfBArray_16t {
    unsigned short  leaf;       // LF_BARRAY_16t
    CV_typ16_t      utype;      // type index of underlying type
} lfBArray_16t;

typedef struct lfBArray {
    unsigned short  leaf;       // LF_BARRAY
    CV_typ_t        utype;      // type index of underlying type
} lfBArray;

//      type record for assembler labels


typedef struct lfLabel {
    unsigned short  leaf;       // LF_LABEL
    unsigned short  mode;       // addressing mode of label
} lfLabel;



//      type record for dimensioned arrays


typedef struct lfDimArray_16t {
    unsigned short  leaf;       // LF_DIMARRAY_16t
    CV_typ16_t      utype;      // underlying type of the array
    CV_typ16_t      diminfo;    // dimension information
    unsigned char   name[1];    // length prefixed name
} lfDimArray_16t;

typedef struct lfDimArray {
    unsigned short  leaf;       // LF_DIMARRAY
    CV_typ_t        utype;      // underlying type of the array
    CV_typ_t        diminfo;    // dimension information
    unsigned char   name[1];    // length prefixed name
} lfDimArray;



//      type record describing path to virtual function table


typedef struct lfVFTPath_16t {
    unsigned short  leaf;       // LF_VFTPATH_16t
    unsigned short  count;      // count of number of bases in path
    CV_typ16_t      base[1];    // bases from root to leaf
} lfVFTPath_16t;

typedef struct lfVFTPath {
    unsigned short  leaf;       // LF_VFTPATH
    unsigned long   count;      // count of number of bases in path
    CV_typ_t        base[1];    // bases from root to leaf
} lfVFTPath;


//      type record describing inclusion of precompiled types


typedef struct lfPreComp_16t {
    unsigned short  leaf;       // LF_PRECOMP_16t
    unsigned short  start;      // starting type index included
    unsigned short  count;      // number of types in inclusion
    unsigned long   signature;  // signature
    unsigned char   name[CV_ZEROLEN];     // length prefixed name of included type file
} lfPreComp_16t;

typedef struct lfPreComp {
    unsigned short  leaf;       // LF_PRECOMP
    unsigned long   start;      // starting type index included
    unsigned long   count;      // number of types in inclusion
    unsigned long   signature;  // signature
    unsigned char   name[CV_ZEROLEN];     // length prefixed name of included type file
} lfPreComp;



//      type record describing end of precompiled types that can be
//      included by another file


typedef struct lfEndPreComp {
    unsigned short  leaf;       // LF_ENDPRECOMP
    unsigned long   signature;  // signature
} lfEndPreComp;





//      type record for OEM definable type strings


typedef struct lfOEM_16t {
    unsigned short  leaf;       // LF_OEM_16t
    unsigned short  cvOEM;      // MS assigned OEM identified
    unsigned short  recOEM;     // OEM assigned type identifier
    unsigned short  count;      // count of type indices to follow
    CV_typ16_t      index[CV_ZEROLEN];  // array of type indices followed
                                // by OEM defined data
} lfOEM_16t;

typedef struct lfOEM {
    unsigned short  leaf;       // LF_OEM
    unsigned short  cvOEM;      // MS assigned OEM identified
    unsigned short  recOEM;     // OEM assigned type identifier
    unsigned long   count;      // count of type indices to follow
    CV_typ_t        index[CV_ZEROLEN];  // array of type indices followed
                                // by OEM defined data
} lfOEM;

#define OEM_MS_FORTRAN90        0xF090
#define OEM_ODI                 0x0010
#define OEM_THOMSON_SOFTWARE    0x5453
#define OEM_ODI_REC_BASELIST    0x0000

typedef struct lfOEM2 {
    unsigned short  leaf;       // LF_OEM2
    unsigned char   idOem[16];  // an oem ID (GUID)
    unsigned long   count;      // count of type indices to follow
    CV_typ_t        index[CV_ZEROLEN];  // array of type indices followed
                                // by OEM defined data
} lfOEM2;

//      type record describing using of a type server

typedef struct lfTypeServer {
    unsigned short  leaf;       // LF_TYPESERVER
    unsigned long   signature;  // signature
    unsigned long   age;        // age of database used by this module
    unsigned char   name[CV_ZEROLEN];     // length prefixed name of PDB
} lfTypeServer;

//      type record describing using of a type server with v7 (GUID) signatures

typedef struct lfTypeServer2 {
    unsigned short  leaf;       // LF_TYPESERVER2
    SIG70           sig70;      // guid signature
    unsigned long   age;        // age of database used by this module
    unsigned char   name[CV_ZEROLEN];     // length prefixed name of PDB
} lfTypeServer2;

//      description of type records that can be referenced from
//      type records referenced by symbols



//      type record for skip record


typedef struct lfSkip_16t {
    unsigned short  leaf;       // LF_SKIP_16t
    CV_typ16_t      type;       // next valid index
    unsigned char   data[CV_ZEROLEN];     // pad data
} lfSkip_16t;

typedef struct lfSkip {
    unsigned short  leaf;       // LF_SKIP
    CV_typ_t        type;       // next valid index
    unsigned char   data[CV_ZEROLEN];     // pad data
} lfSkip;



//      argument list leaf


typedef struct lfArgList_16t {
    unsigned short  leaf;           // LF_ARGLIST_16t
    unsigned short  count;          // number of arguments
    CV_typ16_t      arg[CV_ZEROLEN];      // number of arguments
} lfArgList_16t;

typedef struct lfArgList {
    unsigned short  leaf;           // LF_ARGLIST, LF_SUBSTR_LIST
    unsigned long   count;          // number of arguments
    CV_typ_t        arg[CV_ZEROLEN];      // number of arguments
} lfArgList;




//      derived class list leaf


typedef struct lfDerived_16t {
    unsigned short  leaf;           // LF_DERIVED_16t
    unsigned short  count;          // number of arguments
    CV_typ16_t      drvdcls[CV_ZEROLEN];      // type indices of derived classes
} lfDerived_16t;

typedef struct lfDerived {
    unsigned short  leaf;           // LF_DERIVED
    unsigned long   count;          // number of arguments
    CV_typ_t        drvdcls[CV_ZEROLEN];      // type indices of derived classes
} lfDerived;




//      leaf for default arguments


typedef struct lfDefArg_16t {
    unsigned short  leaf;               // LF_DEFARG_16t
    CV_typ16_t      type;               // type of resulting expression
    unsigned char   expr[CV_ZEROLEN];   // length prefixed expression string
} lfDefArg_16t;

typedef struct lfDefArg {
    unsigned short  leaf;               // LF_DEFARG
    CV_typ_t        type;               // type of resulting expression
    unsigned char   expr[CV_ZEROLEN];   // length prefixed expression string
} lfDefArg;



//      list leaf
//          This list should no longer be used because the utilities cannot
//          verify the contents of the list without knowing what type of list
//          it is.  New specific leaf indices should be used instead.


typedef struct lfList {
    unsigned short  leaf;           // LF_LIST
    char            data[CV_ZEROLEN];         // data format specified by indexing type
} lfList;




//      field list leaf
//      This is the header leaf for a complex list of class and structure
//      subfields.


typedef struct lfFieldList_16t {
    unsigned short  leaf;           // LF_FIELDLIST_16t
    char            data[CV_ZEROLEN];         // field list sub lists
} lfFieldList_16t;


typedef struct lfFieldList {
    unsigned short  leaf;           // LF_FIELDLIST
    char            data[CV_ZEROLEN];         // field list sub lists
} lfFieldList;







//  type record for non-static methods and friends in overloaded method list

typedef struct mlMethod_16t {
    CV_fldattr_t   attr;           // method attribute
    CV_typ16_t     index;          // index to type record for procedure
    unsigned long  vbaseoff[CV_ZEROLEN];    // offset in vfunctable if intro virtual
} mlMethod_16t;

typedef struct mlMethod {
    CV_fldattr_t    attr;           // method attribute
    _2BYTEPAD       pad0;           // internal padding, must be 0
    CV_typ_t        index;          // index to type record for procedure
    unsigned long   vbaseoff[CV_ZEROLEN];    // offset in vfunctable if intro virtual
} mlMethod;


typedef struct lfMethodList_16t {
    unsigned short leaf;
    unsigned char  mList[CV_ZEROLEN];         // really a mlMethod_16t type
} lfMethodList_16t;

typedef struct lfMethodList {
    unsigned short leaf;
    unsigned char  mList[CV_ZEROLEN];         // really a mlMethod type
} lfMethodList;





//      type record for LF_BITFIELD


typedef struct lfBitfield_16t {
    unsigned short  leaf;           // LF_BITFIELD_16t
    unsigned char   length;
    unsigned char   position;
    CV_typ16_t      type;           // type of bitfield

} lfBitfield_16t;

typedef struct lfBitfield {
    unsigned short  leaf;           // LF_BITFIELD
    CV_typ_t        type;           // type of bitfield
    unsigned char   length;
    unsigned char   position;

} lfBitfield;




//      type record for dimensioned array with constant bounds


typedef struct lfDimCon_16t {
    unsigned short  leaf;           // LF_DIMCONU_16t or LF_DIMCONLU_16t
    unsigned short  rank;           // number of dimensions
    CV_typ16_t      typ;            // type of index
    unsigned char   dim[CV_ZEROLEN];          // array of dimension information with
                                    // either upper bounds or lower/upper bound
} lfDimCon_16t;

typedef struct lfDimCon {
    unsigned short  leaf;           // LF_DIMCONU or LF_DIMCONLU
    CV_typ_t        typ;            // type of index
    unsigned short  rank;           // number of dimensions
    unsigned char   dim[CV_ZEROLEN];          // array of dimension information with
                                    // either upper bounds or lower/upper bound
} lfDimCon;




//      type record for dimensioned array with variable bounds


typedef struct lfDimVar_16t {
    unsigned short  leaf;           // LF_DIMVARU_16t or LF_DIMVARLU_16t
    unsigned short  rank;           // number of dimensions
    CV_typ16_t      typ;            // type of index
    CV_typ16_t      dim[CV_ZEROLEN];          // array of type indices for either
                                    // variable upper bound or variable
                                    // lower/upper bound.  The referenced
                                    // types must be LF_REFSYM or T_VOID
} lfDimVar_16t;

typedef struct lfDimVar {
    unsigned short  leaf;           // LF_DIMVARU or LF_DIMVARLU
    unsigned long   rank;           // number of dimensions
    CV_typ_t        typ;            // type of index
    CV_typ_t        dim[CV_ZEROLEN];          // array of type indices for either
                                    // variable upper bound or variable
                                    // lower/upper bound.  The count of type
                                    // indices is rank or rank*2 depending on
                                    // whether it is LFDIMVARU or LF_DIMVARLU.
                                    // The referenced types must be
                                    // LF_REFSYM or T_VOID
} lfDimVar;




//      type record for referenced symbol


typedef struct lfRefSym {
    unsigned short  leaf;           // LF_REFSYM
    unsigned char   Sym[1];         // copy of referenced symbol record
                                    // (including length)
} lfRefSym;



//      type record for generic HLSL type


typedef struct lfHLSL {
    unsigned short  leaf;                 // LF_HLSL
    CV_typ_t        subtype;              // sub-type index, if any
    unsigned short  kind;                 // kind of built-in type from CV_builtin_e
    unsigned short  numprops :  4;        // number of numeric properties
    unsigned short  unused   : 12;        // padding, must be 0
    unsigned char   data[CV_ZEROLEN];     // variable-length array of numeric properties
                                          // followed by byte size
} lfHLSL;




//      type record for a generalized built-in type modifier


typedef struct lfModifierEx {
    unsigned short  leaf;                 // LF_MODIFIER_EX
    CV_typ_t        type;                 // type being modified
    unsigned short  count;                // count of modifier values
    unsigned short  mods[CV_ZEROLEN];     // modifiers from CV_modifier_e
} lfModifierEx;




/**     the following are numeric leaves.  They are used to indicate the
 *      size of the following variable length data.  When the numeric
 *      data is a single byte less than 0x8000, then the data is output
 *      directly.  If the data is more the 0x8000 or is a negative value,
 *      then the data is preceeded by the proper index.
 */



//      signed character leaf

typedef struct lfChar {
    unsigned short  leaf;           // LF_CHAR
    signed char     val;            // signed 8-bit value
} lfChar;




//      signed short leaf

typedef struct lfShort {
    unsigned short  leaf;           // LF_SHORT
    short           val;            // signed 16-bit value
} lfShort;




//      unsigned short leaf

typedef struct lfUShort {
    unsigned short  leaf;           // LF_unsigned short
    unsigned short  val;            // unsigned 16-bit value
} lfUShort;




//      signed long leaf

typedef struct lfLong {
    unsigned short  leaf;           // LF_LONG
    long            val;            // signed 32-bit value
} lfLong;




//      unsigned long leaf

typedef struct lfULong {
    unsigned short  leaf;           // LF_ULONG
    unsigned long   val;            // unsigned 32-bit value
} lfULong;




//      signed quad leaf

typedef struct lfQuad {
    unsigned short  leaf;           // LF_QUAD
    unsigned char   val[8];         // signed 64-bit value
} lfQuad;




//      unsigned quad leaf

typedef struct lfUQuad {
    unsigned short  leaf;           // LF_UQUAD
    unsigned char   val[8];         // unsigned 64-bit value
} lfUQuad;


//      signed int128 leaf

typedef struct lfOct {
    unsigned short  leaf;           // LF_OCT
    unsigned char   val[16];        // signed 128-bit value
} lfOct;

//      unsigned int128 leaf

typedef struct lfUOct {
    unsigned short  leaf;           // LF_UOCT
    unsigned char   val[16];        // unsigned 128-bit value
} lfUOct;




//      real 16-bit leaf

typedef struct lfReal16 {
    unsigned short  leaf;           // LF_REAL16
    unsigned short  val;            // 16-bit real value
} lfReal16;




//      real 32-bit leaf

typedef struct lfReal32 {
    unsigned short  leaf;           // LF_REAL32
    float           val;            // 32-bit real value
} lfReal32;




//      real 48-bit leaf

typedef struct lfReal48 {
    unsigned short  leaf;           // LF_REAL48
    unsigned char   val[6];         // 48-bit real value
} lfReal48;




//      real 64-bit leaf

typedef struct lfReal64 {
    unsigned short  leaf;           // LF_REAL64
    double          val;            // 64-bit real value
} lfReal64;




//      real 80-bit leaf

typedef struct lfReal80 {
    unsigned short  leaf;           // LF_REAL80
    FLOAT10         val;            // real 80-bit value
} lfReal80;




//      real 128-bit leaf

typedef struct lfReal128 {
    unsigned short  leaf;           // LF_REAL128
    char            val[16];        // real 128-bit value
} lfReal128;




//      complex 32-bit leaf

typedef struct lfCmplx32 {
    unsigned short  leaf;           // LF_COMPLEX32
    float           val_real;       // real component
    float           val_imag;       // imaginary component
} lfCmplx32;




//      complex 64-bit leaf

typedef struct lfCmplx64 {
    unsigned short  leaf;           // LF_COMPLEX64
    double          val_real;       // real component
    double          val_imag;       // imaginary component
} flCmplx64;




//      complex 80-bit leaf

typedef struct lfCmplx80 {
    unsigned short  leaf;           // LF_COMPLEX80
    FLOAT10         val_real;       // real component
    FLOAT10         val_imag;       // imaginary component
} lfCmplx80;




//      complex 128-bit leaf

typedef struct lfCmplx128 {
    unsigned short  leaf;           // LF_COMPLEX128
    char            val_real[16];   // real component
    char            val_imag[16];   // imaginary component
} lfCmplx128;



//  variable length numeric field

typedef struct lfVarString {
    unsigned short  leaf;       // LF_VARSTRING
    unsigned short  len;        // length of value in bytes
    unsigned char   value[CV_ZEROLEN];  // value
} lfVarString;

//***********************************************************************


//      index leaf - contains type index of another leaf
//      a major use of this leaf is to allow the compilers to emit a
//      long complex list (LF_FIELD) in smaller pieces.

typedef struct lfIndex_16t {
    unsigned short  leaf;           // LF_INDEX_16t
    CV_typ16_t      index;          // type index of referenced leaf
} lfIndex_16t;

typedef struct lfIndex {
    unsigned short  leaf;           // LF_INDEX
    _2BYTEPAD       pad0;           // internal padding, must be 0
    CV_typ_t        index;          // type index of referenced leaf
} lfIndex;


//      subfield record for base class field

typedef struct lfBClass_16t {
    unsigned short  leaf;           // LF_BCLASS_16t
    CV_typ16_t      index;          // type index of base class
    CV_fldattr_t    attr;           // attribute
    unsigned char   offset[CV_ZEROLEN];       // variable length offset of base within class
} lfBClass_16t;

typedef struct lfBClass {
    unsigned short  leaf;           // LF_BCLASS, LF_BINTERFACE
    CV_fldattr_t    attr;           // attribute
    CV_typ_t        index;          // type index of base class
    unsigned char   offset[CV_ZEROLEN];       // variable length offset of base within class
} lfBClass;
typedef lfBClass lfBInterface;




//      subfield record for direct and indirect virtual base class field

typedef struct lfVBClass_16t {
    unsigned short  leaf;           // LF_VBCLASS_16t | LV_IVBCLASS_16t
    CV_typ16_t      index;          // type index of direct virtual base class
    CV_typ16_t      vbptr;          // type index of virtual base pointer
    CV_fldattr_t    attr;           // attribute
    unsigned char   vbpoff[CV_ZEROLEN];       // virtual base pointer offset from address point
                                    // followed by virtual base offset from vbtable
} lfVBClass_16t;

typedef struct lfVBClass {
    unsigned short  leaf;           // LF_VBCLASS | LV_IVBCLASS
    CV_fldattr_t    attr;           // attribute
    CV_typ_t        index;          // type index of direct virtual base class
    CV_typ_t        vbptr;          // type index of virtual base pointer
    unsigned char   vbpoff[CV_ZEROLEN];       // virtual base pointer offset from address point
                                    // followed by virtual base offset from vbtable
} lfVBClass;





//      subfield record for friend class


typedef struct lfFriendCls_16t {
    unsigned short  leaf;           // LF_FRIENDCLS_16t
    CV_typ16_t      index;          // index to type record of friend class
} lfFriendCls_16t;

typedef struct lfFriendCls {
    unsigned short  leaf;           // LF_FRIENDCLS
    _2BYTEPAD       pad0;           // internal padding, must be 0
    CV_typ_t        index;          // index to type record of friend class
} lfFriendCls;





//      subfield record for friend function


typedef struct lfFriendFcn_16t {
    unsigned short  leaf;           // LF_FRIENDFCN_16t
    CV_typ16_t      index;          // index to type record of friend function
    unsigned char   Name[1];        // name of friend function
} lfFriendFcn_16t;

typedef struct lfFriendFcn {
    unsigned short  leaf;           // LF_FRIENDFCN
    _2BYTEPAD       pad0;           // internal padding, must be 0
    CV_typ_t        index;          // index to type record of friend function
    unsigned char   Name[1];        // name of friend function
} lfFriendFcn;



//      subfield record for non-static data members

typedef struct lfMember_16t {
    unsigned short  leaf;           // LF_MEMBER_16t
    CV_typ16_t      index;          // index of type record for field
    CV_fldattr_t    attr;           // attribute mask
    unsigned char   offset[CV_ZEROLEN];       // variable length offset of field followed
                                    // by length prefixed name of field
} lfMember_16t;

typedef struct lfMember {
    unsigned short  leaf;           // LF_MEMBER
    CV_fldattr_t    attr;           // attribute mask
    CV_typ_t        index;          // index of type record for field
    unsigned char   offset[CV_ZEROLEN];       // variable length offset of field followed
                                    // by length prefixed name of field
} lfMember;



//  type record for static data members

typedef struct lfSTMember_16t {
    unsigned short  leaf;           // LF_STMEMBER_16t
    CV_typ16_t      index;          // index of type record for field
    CV_fldattr_t    attr;           // attribute mask
    unsigned char   Name[1];        // length prefixed name of field
} lfSTMember_16t;

typedef struct lfSTMember {
    unsigned short  leaf;           // LF_STMEMBER
    CV_fldattr_t    attr;           // attribute mask
    CV_typ_t        index;          // index of type record for field
    unsigned char   Name[1];        // length prefixed name of field
} lfSTMember;



//      subfield record for virtual function table pointer

typedef struct lfVFuncTab_16t {
    unsigned short  leaf;           // LF_VFUNCTAB_16t
    CV_typ16_t      type;           // type index of pointer
} lfVFuncTab_16t;

typedef struct lfVFuncTab {
    unsigned short  leaf;           // LF_VFUNCTAB
    _2BYTEPAD       pad0;           // internal padding, must be 0
    CV_typ_t        type;           // type index of pointer
} lfVFuncTab;



//      subfield record for virtual function table pointer with offset

typedef struct lfVFuncOff_16t {
    unsigned short  leaf;           // LF_VFUNCOFF_16t
    CV_typ16_t      type;           // type index of pointer
    CV_off32_t      offset;         // offset of virtual function table pointer
} lfVFuncOff_16t;

typedef struct lfVFuncOff {
    unsigned short  leaf;           // LF_VFUNCOFF
    _2BYTEPAD       pad0;           // internal padding, must be 0.
    CV_typ_t        type;           // type index of pointer
    CV_off32_t      offset;         // offset of virtual function table pointer
} lfVFuncOff;



//      subfield record for overloaded method list


typedef struct lfMethod_16t {
    unsigned short  leaf;           // LF_METHOD_16t
    unsigned short  count;          // number of occurrences of function
    CV_typ16_t      mList;          // index to LF_METHODLIST record
    unsigned char   Name[1];        // length prefixed name of method
} lfMethod_16t;

typedef struct lfMethod {
    unsigned short  leaf;           // LF_METHOD
    unsigned short  count;          // number of occurrences of function
    CV_typ_t        mList;          // index to LF_METHODLIST record
    unsigned char   Name[1];        // length prefixed name of method
} lfMethod;



//      subfield record for nonoverloaded method


typedef struct lfOneMethod_16t {
    unsigned short leaf;            // LF_ONEMETHOD_16t
    CV_fldattr_t   attr;            // method attribute
    CV_typ16_t     index;           // index to type record for procedure
    unsigned long  vbaseoff[CV_ZEROLEN];    // offset in vfunctable if
                                    // intro virtual followed by
                                    // length prefixed name of method
} lfOneMethod_16t;

typedef struct lfOneMethod {
    unsigned short leaf;            // LF_ONEMETHOD
    CV_fldattr_t   attr;            // method attribute
    CV_typ_t       index;           // index to type record for procedure
    unsigned long  vbaseoff[CV_ZEROLEN];    // offset in vfunctable if
                                    // intro virtual followed by
                                    // length prefixed name of method
} lfOneMethod;


//      subfield record for enumerate

typedef struct lfEnumerate {
    unsigned short  leaf;       // LF_ENUMERATE
    CV_fldattr_t    attr;       // access
    unsigned char   value[CV_ZEROLEN];    // variable length value field followed
                                // by length prefixed name
} lfEnumerate;


//  type record for nested (scoped) type definition

typedef struct lfNestType_16t {
    unsigned short  leaf;       // LF_NESTTYPE_16t
    CV_typ16_t      index;      // index of nested type definition
    unsigned char   Name[1];    // length prefixed type name
} lfNestType_16t;

typedef struct lfNestType {
    unsigned short  leaf;       // LF_NESTTYPE
    _2BYTEPAD       pad0;       // internal padding, must be 0
    CV_typ_t        index;      // index of nested type definition
    unsigned char   Name[1];    // length prefixed type name
} lfNestType;

//  type record for nested (scoped) type definition, with attributes
//  new records for vC v5.0, no need to have 16-bit ti versions.

typedef struct lfNestTypeEx {
    unsigned short  leaf;       // LF_NESTTYPEEX
    CV_fldattr_t    attr;       // member access
    CV_typ_t        index;      // index of nested type definition
    unsigned char   Name[1];    // length prefixed type name
} lfNestTypeEx;

//  type record for modifications to members

typedef struct lfMemberModify {
    unsigned short  leaf;       // LF_MEMBERMODIFY
    CV_fldattr_t    attr;       // the new attributes
    CV_typ_t        index;      // index of base class type definition
    unsigned char   Name[1];    // length prefixed member name
} lfMemberModify;

//  type record for pad leaf

typedef struct lfPad {
    unsigned char   leaf;
} SYM_PAD;



//  Symbol definitions

typedef enum SYM_ENUM_e {
    S_COMPILE       =  0x0001,  // Compile flags symbol
    S_REGISTER_16t  =  0x0002,  // Register variable
    S_CONSTANT_16t  =  0x0003,  // constant symbol
    S_UDT_16t       =  0x0004,  // User defined type
    S_SSEARCH       =  0x0005,  // Start Search
    S_END           =  0x0006,  // Block, procedure, "with" or thunk end
    S_SKIP          =  0x0007,  // Reserve symbol space in $$Symbols table
    S_CVRESERVE     =  0x0008,  // Reserved symbol for CV internal use
    S_OBJNAME_ST    =  0x0009,  // path to object file name
    S_ENDARG        =  0x000a,  // end of argument/return list
    S_COBOLUDT_16t  =  0x000b,  // special UDT for cobol that does not symbol pack
    S_MANYREG_16t   =  0x000c,  // multiple register variable
    S_RETURN        =  0x000d,  // return description symbol
    S_ENTRYTHIS     =  0x000e,  // description of this pointer on entry

    S_BPREL16       =  0x0100,  // BP-relative
    S_LDATA16       =  0x0101,  // Module-local symbol
    S_GDATA16       =  0x0102,  // Global data symbol
    S_PUB16         =  0x0103,  // a public symbol
    S_LPROC16       =  0x0104,  // Local procedure start
    S_GPROC16       =  0x0105,  // Global procedure start
    S_THUNK16       =  0x0106,  // Thunk Start
    S_BLOCK16       =  0x0107,  // block start
    S_WITH16        =  0x0108,  // with start
    S_LABEL16       =  0x0109,  // code label
    S_CEXMODEL16    =  0x010a,  // change execution model
    S_VFTABLE16     =  0x010b,  // address of virtual function table
    S_REGREL16      =  0x010c,  // register relative address

    S_BPREL32_16t   =  0x0200,  // BP-relative
    S_LDATA32_16t   =  0x0201,  // Module-local symbol
    S_GDATA32_16t   =  0x0202,  // Global data symbol
    S_PUB32_16t     =  0x0203,  // a public symbol (CV internal reserved)
    S_LPROC32_16t   =  0x0204,  // Local procedure start
    S_GPROC32_16t   =  0x0205,  // Global procedure start
    S_THUNK32_ST    =  0x0206,  // Thunk Start
    S_BLOCK32_ST    =  0x0207,  // block start
    S_WITH32_ST     =  0x0208,  // with start
    S_LABEL32_ST    =  0x0209,  // code label
    S_CEXMODEL32    =  0x020a,  // change execution model
    S_VFTABLE32_16t =  0x020b,  // address of virtual function table
    S_REGREL32_16t  =  0x020c,  // register relative address
    S_LTHREAD32_16t =  0x020d,  // local thread storage
    S_GTHREAD32_16t =  0x020e,  // global thread storage
    S_SLINK32       =  0x020f,  // static link for MIPS EH implementation

    S_LPROCMIPS_16t =  0x0300,  // Local procedure start
    S_GPROCMIPS_16t =  0x0301,  // Global procedure start

    // if these ref symbols have names following then the names are in ST format
    S_PROCREF_ST    =  0x0400,  // Reference to a procedure
    S_DATAREF_ST    =  0x0401,  // Reference to data
    S_ALIGN         =  0x0402,  // Used for page alignment of symbols

    S_LPROCREF_ST   =  0x0403,  // Local Reference to a procedure
    S_OEM           =  0x0404,  // OEM defined symbol

    // sym records with 32-bit types embedded instead of 16-bit
    // all have 0x1000 bit set for easy identification
    // only do the 32-bit target versions since we don't really
    // care about 16-bit ones anymore.
    S_TI16_MAX          =  0x1000,

    S_REGISTER_ST   =  0x1001,  // Register variable
    S_CONSTANT_ST   =  0x1002,  // constant symbol
    S_UDT_ST        =  0x1003,  // User defined type
    S_COBOLUDT_ST   =  0x1004,  // special UDT for cobol that does not symbol pack
    S_MANYREG_ST    =  0x1005,  // multiple register variable
    S_BPREL32_ST    =  0x1006,  // BP-relative
    S_LDATA32_ST    =  0x1007,  // Module-local symbol
    S_GDATA32_ST    =  0x1008,  // Global data symbol
    S_PUB32_ST      =  0x1009,  // a public symbol (CV internal reserved)
    S_LPROC32_ST    =  0x100a,  // Local procedure start
    S_GPROC32_ST    =  0x100b,  // Global procedure start
    S_VFTABLE32     =  0x100c,  // address of virtual function table
    S_REGREL32_ST   =  0x100d,  // register relative address
    S_LTHREAD32_ST  =  0x100e,  // local thread storage
    S_GTHREAD32_ST  =  0x100f,  // global thread storage

    S_LPROCMIPS_ST  =  0x1010,  // Local procedure start
    S_GPROCMIPS_ST  =  0x1011,  // Global procedure start

    S_FRAMEPROC     =  0x1012,  // extra frame and proc information
    S_COMPILE2_ST   =  0x1013,  // extended compile flags and info

    // new symbols necessary for 16-bit enumerates of IA64 registers
    // and IA64 specific symbols

    S_MANYREG2_ST   =  0x1014,  // multiple register variable
    S_LPROCIA64_ST  =  0x1015,  // Local procedure start (IA64)
    S_GPROCIA64_ST  =  0x1016,  // Global procedure start (IA64)

    // Local symbols for IL
    S_LOCALSLOT_ST  =  0x1017,  // local IL sym with field for local slot index
    S_PARAMSLOT_ST  =  0x1018,  // local IL sym with field for parameter slot index

    S_ANNOTATION    =  0x1019,  // Annotation string literals

    // symbols to support managed code debugging
    S_GMANPROC_ST   =  0x101a,  // Global proc
    S_LMANPROC_ST   =  0x101b,  // Local proc
    S_RESERVED1     =  0x101c,  // reserved
    S_RESERVED2     =  0x101d,  // reserved
    S_RESERVED3     =  0x101e,  // reserved
    S_RESERVED4     =  0x101f,  // reserved
    S_LMANDATA_ST   =  0x1020,
    S_GMANDATA_ST   =  0x1021,
    S_MANFRAMEREL_ST=  0x1022,
    S_MANREGISTER_ST=  0x1023,
    S_MANSLOT_ST    =  0x1024,
    S_MANMANYREG_ST =  0x1025,
    S_MANREGREL_ST  =  0x1026,
    S_MANMANYREG2_ST=  0x1027,
    S_MANTYPREF     =  0x1028,  // Index for type referenced by name from metadata
    S_UNAMESPACE_ST =  0x1029,  // Using namespace

    // Symbols w/ SZ name fields. All name fields contain utf8 encoded strings.
    S_ST_MAX        =  0x1100,  // starting point for SZ name symbols

    S_OBJNAME       =  0x1101,  // path to object file name
    S_THUNK32       =  0x1102,  // Thunk Start
    S_BLOCK32       =  0x1103,  // block start
    S_WITH32        =  0x1104,  // with start
    S_LABEL32       =  0x1105,  // code label
    S_REGISTER      =  0x1106,  // Register variable
    S_CONSTANT      =  0x1107,  // constant symbol
    S_UDT           =  0x1108,  // User defined type
    S_COBOLUDT      =  0x1109,  // special UDT for cobol that does not symbol pack
    S_MANYREG       =  0x110a,  // multiple register variable
    S_BPREL32       =  0x110b,  // BP-relative
    S_LDATA32       =  0x110c,  // Module-local symbol
    S_GDATA32       =  0x110d,  // Global data symbol
    S_PUB32         =  0x110e,  // a public symbol (CV internal reserved)
    S_LPROC32       =  0x110f,  // Local procedure start
    S_GPROC32       =  0x1110,  // Global procedure start
    S_REGREL32      =  0x1111,  // register relative address
    S_LTHREAD32     =  0x1112,  // local thread storage
    S_GTHREAD32     =  0x1113,  // global thread storage

    S_LPROCMIPS     =  0x1114,  // Local procedure start
    S_GPROCMIPS     =  0x1115,  // Global procedure start
    S_COMPILE2      =  0x1116,  // extended compile flags and info
    S_MANYREG2      =  0x1117,  // multiple register variable
    S_LPROCIA64     =  0x1118,  // Local procedure start (IA64)
    S_GPROCIA64     =  0x1119,  // Global procedure start (IA64)
    S_LOCALSLOT     =  0x111a,  // local IL sym with field for local slot index
    S_SLOT          = S_LOCALSLOT,  // alias for LOCALSLOT
    S_PARAMSLOT     =  0x111b,  // local IL sym with field for parameter slot index

    // symbols to support managed code debugging
    S_LMANDATA      =  0x111c,
    S_GMANDATA      =  0x111d,
    S_MANFRAMEREL   =  0x111e,
    S_MANREGISTER   =  0x111f,
    S_MANSLOT       =  0x1120,
    S_MANMANYREG    =  0x1121,
    S_MANREGREL     =  0x1122,
    S_MANMANYREG2   =  0x1123,
    S_UNAMESPACE    =  0x1124,  // Using namespace

    // ref symbols with name fields
    S_PROCREF       =  0x1125,  // Reference to a procedure
    S_DATAREF       =  0x1126,  // Reference to data
    S_LPROCREF      =  0x1127,  // Local Reference to a procedure
    S_ANNOTATIONREF =  0x1128,  // Reference to an S_ANNOTATION symbol
    S_TOKENREF      =  0x1129,  // Reference to one of the many MANPROCSYM's

    // continuation of managed symbols
    S_GMANPROC      =  0x112a,  // Global proc
    S_LMANPROC      =  0x112b,  // Local proc

    // short, light-weight thunks
    S_TRAMPOLINE    =  0x112c,  // trampoline thunks
    S_MANCONSTANT   =  0x112d,  // constants with metadata type info

    // native attributed local/parms
    S_ATTR_FRAMEREL =  0x112e,  // relative to virtual frame ptr
    S_ATTR_REGISTER =  0x112f,  // stored in a register
    S_ATTR_REGREL   =  0x1130,  // relative to register (alternate frame ptr)
    S_ATTR_MANYREG  =  0x1131,  // stored in >1 register

    // Separated code (from the compiler) support
    S_SEPCODE       =  0x1132,

    S_LOCAL_2005    =  0x1133,  // defines a local symbol in optimized code
    S_DEFRANGE_2005 =  0x1134,  // defines a single range of addresses in which symbol can be evaluated
    S_DEFRANGE2_2005 =  0x1135,  // defines ranges of addresses in which symbol can be evaluated

    S_SECTION       =  0x1136,  // A COFF section in a PE executable
    S_COFFGROUP     =  0x1137,  // A COFF group
    S_EXPORT        =  0x1138,  // A export

    S_CALLSITEINFO  =  0x1139,  // Indirect call site information
    S_FRAMECOOKIE   =  0x113a,  // Security cookie information

    S_DISCARDED     =  0x113b,  // Discarded by LINK /OPT:REF (experimental, see richards)

    S_COMPILE3      =  0x113c,  // Replacement for S_COMPILE2
    S_ENVBLOCK      =  0x113d,  // Environment block split off from S_COMPILE2

    S_LOCAL         =  0x113e,  // defines a local symbol in optimized code
    S_DEFRANGE      =  0x113f,  // defines a single range of addresses in which symbol can be evaluated
    S_DEFRANGE_SUBFIELD =  0x1140,           // ranges for a subfield

    S_DEFRANGE_REGISTER =  0x1141,           // ranges for en-registered symbol
    S_DEFRANGE_FRAMEPOINTER_REL =  0x1142,   // range for stack symbol.
    S_DEFRANGE_SUBFIELD_REGISTER =  0x1143,  // ranges for en-registered field of symbol
    S_DEFRANGE_FRAMEPOINTER_REL_FULL_SCOPE =  0x1144, // range for stack symbol span valid full scope of function body, gap might apply.
    S_DEFRANGE_REGISTER_REL =  0x1145, // range for symbol address as register + offset.

    // S_PROC symbols that reference ID instead of type
    S_LPROC32_ID     =  0x1146,
    S_GPROC32_ID     =  0x1147,
    S_LPROCMIPS_ID   =  0x1148,
    S_GPROCMIPS_ID   =  0x1149,
    S_LPROCIA64_ID   =  0x114a,
    S_GPROCIA64_ID   =  0x114b,

    S_BUILDINFO      = 0x114c, // build information.
    S_INLINESITE     = 0x114d, // inlined function callsite.
    S_INLINESITE_END = 0x114e,
    S_PROC_ID_END    = 0x114f,

    S_DEFRANGE_HLSL  = 0x1150,
    S_GDATA_HLSL     = 0x1151,
    S_LDATA_HLSL     = 0x1152,

    S_FILESTATIC     = 0x1153,

#if defined(CC_DP_CXX) && CC_DP_CXX

    S_LOCAL_DPC_GROUPSHARED = 0x1154, // DPC groupshared variable
    S_LPROC32_DPC = 0x1155, // DPC local procedure start
    S_LPROC32_DPC_ID =  0x1156,
    S_DEFRANGE_DPC_PTR_TAG =  0x1157, // DPC pointer tag definition range
    S_DPC_SYM_TAG_MAP = 0x1158, // DPC pointer tag value to symbol record map

#endif // CC_DP_CXX
    
    S_ARMSWITCHTABLE  = 0x1159,
    S_CALLEES = 0x115a,
    S_CALLERS = 0x115b,
    S_POGODATA = 0x115c,
    S_INLINESITE2 = 0x115d,      // extended inline site information

    S_HEAPALLOCSITE = 0x115e,    // heap allocation site

    S_MOD_TYPEREF = 0x115f,      // only generated at link time

    S_REF_MINIPDB = 0x1160,      // only generated at link time for mini PDB
    S_PDBMAP      = 0x1161,      // only generated at link time for mini PDB

    S_GDATA_HLSL32 = 0x1162,
    S_LDATA_HLSL32 = 0x1163,

    S_GDATA_HLSL32_EX = 0x1164,
    S_LDATA_HLSL32_EX = 0x1165,

    S_RECTYPE_MAX,               // one greater than last
    S_RECTYPE_LAST  = S_RECTYPE_MAX - 1,
    S_RECTYPE_PAD   = S_RECTYPE_MAX + 0x100 // Used *only* to verify symbol record types so that current PDB code can potentially read
                                // future PDBs (assuming no format change, etc).

} SYM_ENUM_e;


//  enum describing compile flag ambient data model


typedef enum CV_CFL_DATA {
    CV_CFL_DNEAR    = 0x00,
    CV_CFL_DFAR     = 0x01,
    CV_CFL_DHUGE    = 0x02
} CV_CFL_DATA;




//  enum describing compile flag ambiant code model


typedef enum CV_CFL_CODE_e {
    CV_CFL_CNEAR    = 0x00,
    CV_CFL_CFAR     = 0x01,
    CV_CFL_CHUGE    = 0x02
} CV_CFL_CODE_e;




//  enum describing compile flag target floating point package

typedef enum CV_CFL_FPKG_e {
    CV_CFL_NDP      = 0x00,
    CV_CFL_EMU      = 0x01,
    CV_CFL_ALT      = 0x02
} CV_CFL_FPKG_e;


// enum describing function return method


typedef struct CV_PROCFLAGS {
    union {
        unsigned char   bAll;
        unsigned char   grfAll;
        struct {
            unsigned char CV_PFLAG_NOFPO     :1; // frame pointer present
            unsigned char CV_PFLAG_INT       :1; // interrupt return
            unsigned char CV_PFLAG_FAR       :1; // far return
            unsigned char CV_PFLAG_NEVER     :1; // function does not return
            unsigned char CV_PFLAG_NOTREACHED:1; // label isn't fallen into
            unsigned char CV_PFLAG_CUST_CALL :1; // custom calling convention
            unsigned char CV_PFLAG_NOINLINE  :1; // function marked as noinline
            unsigned char CV_PFLAG_OPTDBGINFO:1; // function has debug information for optimized code
        };
    };
} CV_PROCFLAGS;

// Extended proc flags
//
typedef struct CV_EXPROCFLAGS {
    CV_PROCFLAGS cvpf;
    union {
        unsigned char   grfAll;
        struct {
            unsigned char   __reserved_byte      :8; // must be zero
        };
    };
} CV_EXPROCFLAGS;

// local variable flags
typedef struct CV_LVARFLAGS {
    unsigned short fIsParam          :1; // variable is a parameter
    unsigned short fAddrTaken        :1; // address is taken
    unsigned short fCompGenx         :1; // variable is compiler generated
    unsigned short fIsAggregate      :1; // the symbol is splitted in temporaries,
                                         // which are treated by compiler as 
                                         // independent entities
    unsigned short fIsAggregated     :1; // Counterpart of fIsAggregate - tells
                                         // that it is a part of a fIsAggregate symbol
    unsigned short fIsAliased        :1; // variable has multiple simultaneous lifetimes
    unsigned short fIsAlias          :1; // represents one of the multiple simultaneous lifetimes
    unsigned short fIsRetValue       :1; // represents a function return value
    unsigned short fIsOptimizedOut   :1; // variable has no lifetimes
    unsigned short fIsEnregGlob      :1; // variable is an enregistered global
    unsigned short fIsEnregStat      :1; // variable is an enregistered static

    unsigned short unused            :5; // must be zero

} CV_LVARFLAGS;

// extended attributes common to all local variables
typedef struct CV_lvar_attr {
    CV_uoff32_t     off;        // first code address where var is live
    unsigned short  seg;
    CV_LVARFLAGS    flags;      // local var flags
} CV_lvar_attr;

// This is max length of a lexical linear IP range.
// The upper number are reserved for seeded and flow based range

#define CV_LEXICAL_RANGE_MAX  0xF000

// represents an address range, used for optimized code debug info

typedef struct CV_LVAR_ADDR_RANGE {       // defines a range of addresses
    CV_uoff32_t     offStart;
    unsigned short  isectStart;
    unsigned short  cbRange;
} CV_LVAR_ADDR_RANGE;

// Represents the holes in overall address range, all address is pre-bbt. 
// it is for compress and reduce the amount of relocations need.

typedef struct CV_LVAR_ADDR_GAP {
    unsigned short  gapStartOffset;   // relative offset from the beginning of the live range.
    unsigned short  cbRange;          // length of this gap.
} CV_LVAR_ADDR_GAP;

#if defined(CC_DP_CXX) && CC_DP_CXX

// Represents a mapping from a DPC pointer tag value to the corresponding symbol record
typedef struct CV_DPC_SYM_TAG_MAP_ENTRY {
    unsigned int tagValue;       // address taken symbol's pointer tag value.
    CV_off32_t  symRecordOffset; // offset of the symbol record from the S_LPROC32_DPC record it is nested within
} CV_DPC_SYM_TAG_MAP_ENTRY;

#endif // CC_DP_CXX

// enum describing function data return method

typedef enum CV_GENERIC_STYLE_e {
    CV_GENERIC_VOID   = 0x00,       // void return type
    CV_GENERIC_REG    = 0x01,       // return data is in registers
    CV_GENERIC_ICAN   = 0x02,       // indirect caller allocated near
    CV_GENERIC_ICAF   = 0x03,       // indirect caller allocated far
    CV_GENERIC_IRAN   = 0x04,       // indirect returnee allocated near
    CV_GENERIC_IRAF   = 0x05,       // indirect returnee allocated far
    CV_GENERIC_UNUSED = 0x06        // first unused
} CV_GENERIC_STYLE_e;


typedef struct CV_GENERIC_FLAG {
    unsigned short  cstyle  :1;     // true push varargs right to left
    unsigned short  rsclean :1;     // true if returnee stack cleanup
    unsigned short  unused  :14;    // unused
} CV_GENERIC_FLAG;


// flag bitfields for separated code attributes

typedef struct CV_SEPCODEFLAGS {
    unsigned long fIsLexicalScope : 1;     // S_SEPCODE doubles as lexical scope
    unsigned long fReturnsToParent : 1;    // code frag returns to parent
    unsigned long pad : 30;                // must be zero
} CV_SEPCODEFLAGS;

// Generic layout for symbol records

typedef struct SYMTYPE {
    unsigned short      reclen;     // Record length
    unsigned short      rectyp;     // Record type
    char                data[CV_ZEROLEN];
} SYMTYPE;

__INLINE SYMTYPE *NextSym (SYMTYPE * pSym) {
    return (SYMTYPE *) ((char *)pSym + pSym->reclen + sizeof(unsigned short));
}

//      non-model specific symbol types



typedef struct REGSYM_16t {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_REGISTER_16t
    CV_typ16_t      typind;     // Type index
    unsigned short  reg;        // register enumerate
    unsigned char   name[1];    // Length-prefixed name
} REGSYM_16t;

typedef struct REGSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_REGISTER
    CV_typ_t        typind;     // Type index or Metadata token
    unsigned short  reg;        // register enumerate
    unsigned char   name[1];    // Length-prefixed name
} REGSYM;

typedef struct ATTRREGSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_MANREGISTER | S_ATTR_REGISTER
    CV_typ_t        typind;     // Type index or Metadata token
    CV_lvar_attr    attr;       // local var attributes
    unsigned short  reg;        // register enumerate
    unsigned char   name[1];    // Length-prefixed name
} ATTRREGSYM;

typedef struct MANYREGSYM_16t {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_MANYREG_16t
    CV_typ16_t      typind;     // Type index
    unsigned char   count;      // count of number of registers
    unsigned char   reg[1];     // count register enumerates followed by
                                // length-prefixed name.  Registers are
                                // most significant first.
} MANYREGSYM_16t;

typedef struct MANYREGSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_MANYREG
    CV_typ_t        typind;     // Type index or metadata token
    unsigned char   count;      // count of number of registers
    unsigned char   reg[1];     // count register enumerates followed by
                                // length-prefixed name.  Registers are
                                // most significant first.
} MANYREGSYM;

typedef struct MANYREGSYM2 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_MANYREG2
    CV_typ_t        typind;     // Type index or metadata token
    unsigned short  count;      // count of number of registers
    unsigned short  reg[1];     // count register enumerates followed by
                                // length-prefixed name.  Registers are
                                // most significant first.
} MANYREGSYM2;

typedef struct ATTRMANYREGSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_MANMANYREG
    CV_typ_t        typind;     // Type index or metadata token
    CV_lvar_attr    attr;       // local var attributes
    unsigned char   count;      // count of number of registers
    unsigned char   reg[1];     // count register enumerates followed by
                                // length-prefixed name.  Registers are
                                // most significant first.
    unsigned char   name[CV_ZEROLEN];   // utf-8 encoded zero terminate name
} ATTRMANYREGSYM;

typedef struct ATTRMANYREGSYM2 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_MANMANYREG2 | S_ATTR_MANYREG
    CV_typ_t        typind;     // Type index or metadata token
    CV_lvar_attr    attr;       // local var attributes
    unsigned short  count;      // count of number of registers
    unsigned short  reg[1];     // count register enumerates followed by
                                // length-prefixed name.  Registers are
                                // most significant first.
    unsigned char   name[CV_ZEROLEN];   // utf-8 encoded zero terminate name
} ATTRMANYREGSYM2;

typedef struct CONSTSYM_16t {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_CONSTANT_16t
    CV_typ16_t      typind;     // Type index (containing enum if enumerate)
    unsigned short  value;      // numeric leaf containing value
    unsigned char   name[CV_ZEROLEN];     // Length-prefixed name
} CONSTSYM_16t;

typedef struct CONSTSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_CONSTANT or S_MANCONSTANT
    CV_typ_t        typind;     // Type index (containing enum if enumerate) or metadata token
    unsigned short  value;      // numeric leaf containing value
    unsigned char   name[CV_ZEROLEN];     // Length-prefixed name
} CONSTSYM;


typedef struct UDTSYM_16t {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_UDT_16t | S_COBOLUDT_16t
    CV_typ16_t      typind;     // Type index
    unsigned char   name[1];    // Length-prefixed name
} UDTSYM_16t;


typedef struct UDTSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_UDT | S_COBOLUDT
    CV_typ_t        typind;     // Type index
    unsigned char   name[1];    // Length-prefixed name
} UDTSYM;

typedef struct MANTYPREF {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_MANTYPREF
    CV_typ_t        typind;     // Type index
} MANTYPREF;

typedef struct SEARCHSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_SSEARCH
    unsigned long   startsym;   // offset of the procedure
    unsigned short  seg;        // segment of symbol
} SEARCHSYM;


typedef struct CFLAGSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_COMPILE
    unsigned char   machine;    // target processor
    struct  {
        unsigned char   language    :8; // language index
        unsigned char   pcode       :1; // true if pcode present
        unsigned char   floatprec   :2; // floating precision
        unsigned char   floatpkg    :2; // float package
        unsigned char   ambdata     :3; // ambient data model
        unsigned char   ambcode     :3; // ambient code model
        unsigned char   mode32      :1; // true if compiled 32 bit mode
        unsigned char   pad         :4; // reserved
    } flags;
    unsigned char       ver[1];     // Length-prefixed compiler version string
} CFLAGSYM;


typedef struct COMPILESYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_COMPILE2
    struct {
        unsigned long   iLanguage       :  8;   // language index
        unsigned long   fEC             :  1;   // compiled for E/C
        unsigned long   fNoDbgInfo      :  1;   // not compiled with debug info
        unsigned long   fLTCG           :  1;   // compiled with LTCG
        unsigned long   fNoDataAlign    :  1;   // compiled with -Bzalign
        unsigned long   fManagedPresent :  1;   // managed code/data present
        unsigned long   fSecurityChecks :  1;   // compiled with /GS
        unsigned long   fHotPatch       :  1;   // compiled with /hotpatch
        unsigned long   fCVTCIL         :  1;   // converted with CVTCIL
        unsigned long   fMSILModule     :  1;   // MSIL netmodule
        unsigned long   pad             : 15;   // reserved, must be 0
    } flags;
    unsigned short  machine;    // target processor
    unsigned short  verFEMajor; // front end major version #
    unsigned short  verFEMinor; // front end minor version #
    unsigned short  verFEBuild; // front end build version #
    unsigned short  verMajor;   // back end major version #
    unsigned short  verMinor;   // back end minor version #
    unsigned short  verBuild;   // back end build version #
    unsigned char   verSt[1];   // Length-prefixed compiler version string, followed
                                //  by an optional block of zero terminated strings
                                //  terminated with a double zero.
} COMPILESYM;

typedef struct COMPILESYM3 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_COMPILE3
    struct {
        unsigned long   iLanguage       :  8;   // language index
        unsigned long   fEC             :  1;   // compiled for E/C
        unsigned long   fNoDbgInfo      :  1;   // not compiled with debug info
        unsigned long   fLTCG           :  1;   // compiled with LTCG
        unsigned long   fNoDataAlign    :  1;   // compiled with -Bzalign
        unsigned long   fManagedPresent :  1;   // managed code/data present
        unsigned long   fSecurityChecks :  1;   // compiled with /GS
        unsigned long   fHotPatch       :  1;   // compiled with /hotpatch
        unsigned long   fCVTCIL         :  1;   // converted with CVTCIL
        unsigned long   fMSILModule     :  1;   // MSIL netmodule
        unsigned long   fSdl            :  1;   // compiled with /sdl
        unsigned long   fPGO            :  1;   // compiled with /ltcg:pgo or pgu
        unsigned long   fExp            :  1;   // .exp module
        unsigned long   pad             : 12;   // reserved, must be 0
    } flags;
    unsigned short  machine;    // target processor
    unsigned short  verFEMajor; // front end major version #
    unsigned short  verFEMinor; // front end minor version #
    unsigned short  verFEBuild; // front end build version #
    unsigned short  verFEQFE;   // front end QFE version #
    unsigned short  verMajor;   // back end major version #
    unsigned short  verMinor;   // back end minor version #
    unsigned short  verBuild;   // back end build version #
    unsigned short  verQFE;     // back end QFE version #
    char            verSz[1];   // Zero terminated compiler version string
} COMPILESYM3;

typedef struct ENVBLOCKSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_ENVBLOCK
    struct {
        unsigned char  rev              : 1;    // reserved
        unsigned char  pad              : 7;    // reserved, must be 0
    } flags;
    unsigned char   rgsz[1];    // Sequence of zero-terminated strings
} ENVBLOCKSYM;

typedef struct OBJNAMESYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_OBJNAME
    unsigned long   signature;  // signature
    unsigned char   name[1];    // Length-prefixed name
} OBJNAMESYM;


typedef struct ENDARGSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_ENDARG
} ENDARGSYM;


typedef struct RETURNSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_RETURN
    CV_GENERIC_FLAG flags;      // flags
    unsigned char   style;      // CV_GENERIC_STYLE_e return style
                                // followed by return method data
} RETURNSYM;


typedef struct ENTRYTHISSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_ENTRYTHIS
    unsigned char   thissym;    // symbol describing this pointer on entry
} ENTRYTHISSYM;


//      symbol types for 16:16 memory model


typedef struct BPRELSYM16 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_BPREL16
    CV_off16_t      off;        // BP-relative offset
    CV_typ16_t      typind;     // Type index
    unsigned char   name[1];    // Length-prefixed name
} BPRELSYM16;


typedef struct DATASYM16 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_LDATA or S_GDATA
    CV_uoff16_t     off;        // offset of symbol
    unsigned short  seg;        // segment of symbol
    CV_typ16_t      typind;     // Type index
    unsigned char   name[1];    // Length-prefixed name
} DATASYM16;
typedef DATASYM16 PUBSYM16;


typedef struct PROCSYM16 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_GPROC16 or S_LPROC16
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned long   pNext;      // pointer to next symbol
    unsigned short  len;        // Proc length
    unsigned short  DbgStart;   // Debug start offset
    unsigned short  DbgEnd;     // Debug end offset
    CV_uoff16_t     off;        // offset of symbol
    unsigned short  seg;        // segment of symbol
    CV_typ16_t      typind;     // Type index
    CV_PROCFLAGS    flags;      // Proc flags
    unsigned char   name[1];    // Length-prefixed name
} PROCSYM16;


typedef struct THUNKSYM16 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_THUNK
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned long   pNext;      // pointer to next symbol
    CV_uoff16_t     off;        // offset of symbol
    unsigned short  seg;        // segment of symbol
    unsigned short  len;        // length of thunk
    unsigned char   ord;        // THUNK_ORDINAL specifying type of thunk
    unsigned char   name[1];    // name of thunk
    unsigned char   variant[CV_ZEROLEN]; // variant portion of thunk
} THUNKSYM16;

typedef struct LABELSYM16 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_LABEL16
    CV_uoff16_t     off;        // offset of symbol
    unsigned short  seg;        // segment of symbol
    CV_PROCFLAGS    flags;      // flags
    unsigned char   name[1];    // Length-prefixed name
} LABELSYM16;


typedef struct BLOCKSYM16 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_BLOCK16
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned short  len;        // Block length
    CV_uoff16_t     off;        // offset of symbol
    unsigned short  seg;        // segment of symbol
    unsigned char   name[1];    // Length-prefixed name
} BLOCKSYM16;


typedef struct WITHSYM16 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_WITH16
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned short  len;        // Block length
    CV_uoff16_t     off;        // offset of symbol
    unsigned short  seg;        // segment of symbol
    unsigned char   expr[1];    // Length-prefixed expression
} WITHSYM16;


typedef enum CEXM_MODEL_e {
    CEXM_MDL_table          = 0x00, // not executable
    CEXM_MDL_jumptable      = 0x01, // Compiler generated jump table
    CEXM_MDL_datapad        = 0x02, // Data padding for alignment
    CEXM_MDL_native         = 0x20, // native (actually not-pcode)
    CEXM_MDL_cobol          = 0x21, // cobol
    CEXM_MDL_codepad        = 0x22, // Code padding for alignment
    CEXM_MDL_code           = 0x23, // code
    CEXM_MDL_sql            = 0x30, // sql
    CEXM_MDL_pcode          = 0x40, // pcode
    CEXM_MDL_pcode32Mac     = 0x41, // macintosh 32 bit pcode
    CEXM_MDL_pcode32MacNep  = 0x42, // macintosh 32 bit pcode native entry point
    CEXM_MDL_javaInt        = 0x50,
    CEXM_MDL_unknown        = 0xff
} CEXM_MODEL_e;

// use the correct enumerate name
#define CEXM_MDL_SQL CEXM_MDL_sql

typedef enum CV_COBOL_e {
    CV_COBOL_dontstop,
    CV_COBOL_pfm,
    CV_COBOL_false,
    CV_COBOL_extcall
} CV_COBOL_e;

typedef struct CEXMSYM16 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_CEXMODEL16
    CV_uoff16_t     off;        // offset of symbol
    unsigned short  seg;        // segment of symbol
    unsigned short  model;      // execution model
    union {
        struct  {
            CV_uoff16_t pcdtable;   // offset to pcode function table
            CV_uoff16_t pcdspi;     // offset to segment pcode information
        } pcode;
        struct {
            unsigned short  subtype;   // see CV_COBOL_e above
            unsigned short  flag;
        } cobol;
    };
} CEXMSYM16;


typedef struct VPATHSYM16 {
    unsigned short  reclen;     // record length
    unsigned short  rectyp;     // S_VFTPATH16
    CV_uoff16_t     off;        // offset of virtual function table
    unsigned short  seg;        // segment of virtual function table
    CV_typ16_t      root;       // type index of the root of path
    CV_typ16_t      path;       // type index of the path record
} VPATHSYM16;


typedef struct REGREL16 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_REGREL16
    CV_uoff16_t     off;        // offset of symbol
    unsigned short  reg;        // register index
    CV_typ16_t      typind;     // Type index
    unsigned char   name[1];    // Length-prefixed name
} REGREL16;


typedef struct BPRELSYM32_16t {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_BPREL32_16t
    CV_off32_t      off;        // BP-relative offset
    CV_typ16_t      typind;     // Type index
    unsigned char   name[1];    // Length-prefixed name
} BPRELSYM32_16t;

typedef struct BPRELSYM32 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_BPREL32
    CV_off32_t      off;        // BP-relative offset
    CV_typ_t        typind;     // Type index or Metadata token
    unsigned char   name[1];    // Length-prefixed name
} BPRELSYM32;

typedef struct FRAMERELSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_MANFRAMEREL | S_ATTR_FRAMEREL
    CV_off32_t      off;        // Frame relative offset
    CV_typ_t        typind;     // Type index or Metadata token
    CV_lvar_attr    attr;       // local var attributes
    unsigned char   name[1];    // Length-prefixed name
} FRAMERELSYM;

typedef FRAMERELSYM ATTRFRAMERELSYM;


typedef struct SLOTSYM32 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_LOCALSLOT or S_PARAMSLOT
    unsigned long   iSlot;      // slot index
    CV_typ_t        typind;     // Type index or Metadata token
    unsigned char   name[1];    // Length-prefixed name
} SLOTSYM32;

typedef struct ATTRSLOTSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_MANSLOT
    unsigned long   iSlot;      // slot index
    CV_typ_t        typind;     // Type index or Metadata token
    CV_lvar_attr    attr;       // local var attributes
    unsigned char   name[1];    // Length-prefixed name
} ATTRSLOTSYM;

typedef struct ANNOTATIONSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_ANNOTATION
    CV_uoff32_t     off;
    unsigned short  seg;
    unsigned short  csz;        // Count of zero terminated annotation strings
    unsigned char   rgsz[1];    // Sequence of zero terminated annotation strings
} ANNOTATIONSYM;

typedef struct DATASYM32_16t {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_LDATA32_16t, S_GDATA32_16t or S_PUB32_16t
    CV_uoff32_t     off;
    unsigned short  seg;
    CV_typ16_t      typind;     // Type index
    unsigned char   name[1];    // Length-prefixed name
} DATASYM32_16t;
typedef DATASYM32_16t PUBSYM32_16t;

typedef struct DATASYM32 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_LDATA32, S_GDATA32, S_LMANDATA, S_GMANDATA
    CV_typ_t        typind;     // Type index, or Metadata token if a managed symbol
    CV_uoff32_t     off;
    unsigned short  seg;
    unsigned char   name[1];    // Length-prefixed name
} DATASYM32;

typedef struct DATASYMHLSL {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_GDATA_HLSL, S_LDATA_HLSL
    CV_typ_t        typind;     // Type index
    unsigned short  regType;    // register type from CV_HLSLREG_e
    unsigned short  dataslot;   // Base data (cbuffer, groupshared, etc.) slot
    unsigned short  dataoff;    // Base data byte offset start
    unsigned short  texslot;    // Texture slot start
    unsigned short  sampslot;   // Sampler slot start
    unsigned short  uavslot;    // UAV slot start
    unsigned char   name[1];    // name
} DATASYMHLSL;

typedef struct DATASYMHLSL32 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_GDATA_HLSL32, S_LDATA_HLSL32
    CV_typ_t        typind;     // Type index
    unsigned long   dataslot;   // Base data (cbuffer, groupshared, etc.) slot
    unsigned long   dataoff;    // Base data byte offset start
    unsigned long   texslot;    // Texture slot start
    unsigned long   sampslot;   // Sampler slot start
    unsigned long   uavslot;    // UAV slot start
    unsigned short  regType;    // register type from CV_HLSLREG_e
    unsigned char   name[1];    // name
} DATASYMHLSL32;

typedef struct DATASYMHLSL32_EX {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_GDATA_HLSL32_EX, S_LDATA_HLSL32_EX
    CV_typ_t        typind;     // Type index
    unsigned long   regID;      // Register index
    unsigned long   dataoff;    // Base data byte offset start
    unsigned long   bindSpace;  // Binding space
    unsigned long   bindSlot;   // Lower bound in binding space
    unsigned short  regType;    // register type from CV_HLSLREG_e
    unsigned char   name[1];    // name
} DATASYMHLSL32_EX;

typedef enum CV_PUBSYMFLAGS_e
 {
    cvpsfNone     = 0,
    cvpsfCode     = 0x00000001,
    cvpsfFunction = 0x00000002,
    cvpsfManaged  = 0x00000004,
    cvpsfMSIL     = 0x00000008,
} CV_PUBSYMFLAGS_e;

typedef union CV_PUBSYMFLAGS {
    CV_pubsymflag_t grfFlags;
    struct {
        CV_pubsymflag_t fCode       :  1;    // set if public symbol refers to a code address
        CV_pubsymflag_t fFunction   :  1;    // set if public symbol is a function
        CV_pubsymflag_t fManaged    :  1;    // set if managed code (native or IL)
        CV_pubsymflag_t fMSIL       :  1;    // set if managed IL code
        CV_pubsymflag_t __unused    : 28;    // must be zero
    };
} CV_PUBSYMFLAGS;

typedef struct PUBSYM32 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_PUB32
    CV_PUBSYMFLAGS  pubsymflags;
    CV_uoff32_t     off;
    unsigned short  seg;
    unsigned char   name[1];    // Length-prefixed name
} PUBSYM32;


typedef struct PROCSYM32_16t {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_GPROC32_16t or S_LPROC32_16t
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned long   pNext;      // pointer to next symbol
    unsigned long   len;        // Proc length
    unsigned long   DbgStart;   // Debug start offset
    unsigned long   DbgEnd;     // Debug end offset
    CV_uoff32_t     off;
    unsigned short  seg;
    CV_typ16_t      typind;     // Type index
    CV_PROCFLAGS    flags;      // Proc flags
    unsigned char   name[1];    // Length-prefixed name
} PROCSYM32_16t;

typedef struct PROCSYM32 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_GPROC32, S_LPROC32, S_GPROC32_ID, S_LPROC32_ID, S_LPROC32_DPC or S_LPROC32_DPC_ID
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned long   pNext;      // pointer to next symbol
    unsigned long   len;        // Proc length
    unsigned long   DbgStart;   // Debug start offset
    unsigned long   DbgEnd;     // Debug end offset
    CV_typ_t        typind;     // Type index or ID
    CV_uoff32_t     off;
    unsigned short  seg;
    CV_PROCFLAGS    flags;      // Proc flags
    unsigned char   name[1];    // Length-prefixed name
} PROCSYM32;

typedef struct MANPROCSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_GMANPROC, S_LMANPROC, S_GMANPROCIA64 or S_LMANPROCIA64
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned long   pNext;      // pointer to next symbol
    unsigned long   len;        // Proc length
    unsigned long   DbgStart;   // Debug start offset
    unsigned long   DbgEnd;     // Debug end offset
    CV_tkn_t        token;      // COM+ metadata token for method
    CV_uoff32_t     off;
    unsigned short  seg;
    CV_PROCFLAGS    flags;      // Proc flags
    unsigned short  retReg;     // Register return value is in (may not be used for all archs)
    unsigned char   name[1];    // optional name field
} MANPROCSYM;

typedef struct MANPROCSYMMIPS {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_GMANPROCMIPS or S_LMANPROCMIPS
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned long   pNext;      // pointer to next symbol
    unsigned long   len;        // Proc length
    unsigned long   DbgStart;   // Debug start offset
    unsigned long   DbgEnd;     // Debug end offset
    unsigned long   regSave;    // int register save mask
    unsigned long   fpSave;     // fp register save mask
    CV_uoff32_t     intOff;     // int register save offset
    CV_uoff32_t     fpOff;      // fp register save offset
    CV_tkn_t        token;      // COM+ token type
    CV_uoff32_t     off;
    unsigned short  seg;
    unsigned char   retReg;     // Register return value is in
    unsigned char   frameReg;   // Frame pointer register
    unsigned char   name[1];    // optional name field
} MANPROCSYMMIPS;

typedef struct THUNKSYM32 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_THUNK32
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned long   pNext;      // pointer to next symbol
    CV_uoff32_t     off;
    unsigned short  seg;
    unsigned short  len;        // length of thunk
    unsigned char   ord;        // THUNK_ORDINAL specifying type of thunk
    unsigned char   name[1];    // Length-prefixed name
    unsigned char   variant[CV_ZEROLEN]; // variant portion of thunk
} THUNKSYM32;

typedef enum TRAMP_e {      // Trampoline subtype
    trampIncremental,           // incremental thunks
    trampBranchIsland,          // Branch island thunks
} TRAMP_e;

typedef struct TRAMPOLINESYM {  // Trampoline thunk symbol
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_TRAMPOLINE
    unsigned short  trampType;  // trampoline sym subtype
    unsigned short  cbThunk;    // size of the thunk
    CV_uoff32_t     offThunk;   // offset of the thunk
    CV_uoff32_t     offTarget;  // offset of the target of the thunk
    unsigned short  sectThunk;  // section index of the thunk
    unsigned short  sectTarget; // section index of the target of the thunk
} TRAMPOLINE;

typedef struct LABELSYM32 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_LABEL32
    CV_uoff32_t     off;
    unsigned short  seg;
    CV_PROCFLAGS    flags;      // flags
    unsigned char   name[1];    // Length-prefixed name
} LABELSYM32;


typedef struct BLOCKSYM32 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_BLOCK32
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned long   len;        // Block length
    CV_uoff32_t     off;        // Offset in code segment
    unsigned short  seg;        // segment of label
    unsigned char   name[1];    // Length-prefixed name
} BLOCKSYM32;


typedef struct WITHSYM32 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_WITH32
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned long   len;        // Block length
    CV_uoff32_t     off;        // Offset in code segment
    unsigned short  seg;        // segment of label
    unsigned char   expr[1];    // Length-prefixed expression string
} WITHSYM32;



typedef struct CEXMSYM32 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_CEXMODEL32
    CV_uoff32_t     off;        // offset of symbol
    unsigned short  seg;        // segment of symbol
    unsigned short  model;      // execution model
    union {
        struct  {
            CV_uoff32_t pcdtable;   // offset to pcode function table
            CV_uoff32_t pcdspi;     // offset to segment pcode information
        } pcode;
        struct {
            unsigned short  subtype;   // see CV_COBOL_e above
            unsigned short  flag;
        } cobol;
        struct {
            CV_uoff32_t calltableOff; // offset to function table
            unsigned short calltableSeg; // segment of function table
        } pcode32Mac;
    };
} CEXMSYM32;



typedef struct VPATHSYM32_16t {
    unsigned short  reclen;     // record length
    unsigned short  rectyp;     // S_VFTABLE32_16t
    CV_uoff32_t     off;        // offset of virtual function table
    unsigned short  seg;        // segment of virtual function table
    CV_typ16_t      root;       // type index of the root of path
    CV_typ16_t      path;       // type index of the path record
} VPATHSYM32_16t;

typedef struct VPATHSYM32 {
    unsigned short  reclen;     // record length
    unsigned short  rectyp;     // S_VFTABLE32
    CV_typ_t        root;       // type index of the root of path
    CV_typ_t        path;       // type index of the path record
    CV_uoff32_t     off;        // offset of virtual function table
    unsigned short  seg;        // segment of virtual function table
} VPATHSYM32;





typedef struct REGREL32_16t {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_REGREL32_16t
    CV_uoff32_t     off;        // offset of symbol
    unsigned short  reg;        // register index for symbol
    CV_typ16_t      typind;     // Type index
    unsigned char   name[1];    // Length-prefixed name
} REGREL32_16t;

typedef struct REGREL32 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_REGREL32
    CV_uoff32_t     off;        // offset of symbol
    CV_typ_t        typind;     // Type index or metadata token
    unsigned short  reg;        // register index for symbol
    unsigned char   name[1];    // Length-prefixed name
} REGREL32;

typedef struct ATTRREGREL {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_MANREGREL | S_ATTR_REGREL
    CV_uoff32_t     off;        // offset of symbol
    CV_typ_t        typind;     // Type index or metadata token
    unsigned short  reg;        // register index for symbol
    CV_lvar_attr    attr;       // local var attributes
    unsigned char   name[1];    // Length-prefixed name
} ATTRREGREL;

typedef ATTRREGREL  ATTRREGRELSYM;

typedef struct THREADSYM32_16t {
    unsigned short  reclen;     // record length
    unsigned short  rectyp;     // S_LTHREAD32_16t | S_GTHREAD32_16t
    CV_uoff32_t     off;        // offset into thread storage
    unsigned short  seg;        // segment of thread storage
    CV_typ16_t      typind;     // type index
    unsigned char   name[1];    // length prefixed name
} THREADSYM32_16t;

typedef struct THREADSYM32 {
    unsigned short  reclen;     // record length
    unsigned short  rectyp;     // S_LTHREAD32 | S_GTHREAD32
    CV_typ_t        typind;     // type index
    CV_uoff32_t     off;        // offset into thread storage
    unsigned short  seg;        // segment of thread storage
    unsigned char   name[1];    // length prefixed name
} THREADSYM32;

typedef struct SLINK32 {
    unsigned short  reclen;     // record length
    unsigned short  rectyp;     // S_SLINK32
    unsigned long   framesize;  // frame size of parent procedure
    CV_off32_t      off;        // signed offset where the static link was saved relative to the value of reg
    unsigned short  reg;
} SLINK32;

typedef struct PROCSYMMIPS_16t {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_GPROCMIPS_16t or S_LPROCMIPS_16t
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned long   pNext;      // pointer to next symbol
    unsigned long   len;        // Proc length
    unsigned long   DbgStart;   // Debug start offset
    unsigned long   DbgEnd;     // Debug end offset
    unsigned long   regSave;    // int register save mask
    unsigned long   fpSave;     // fp register save mask
    CV_uoff32_t     intOff;     // int register save offset
    CV_uoff32_t     fpOff;      // fp register save offset
    CV_uoff32_t     off;        // Symbol offset
    unsigned short  seg;        // Symbol segment
    CV_typ16_t      typind;     // Type index
    unsigned char   retReg;     // Register return value is in
    unsigned char   frameReg;   // Frame pointer register
    unsigned char   name[1];    // Length-prefixed name
} PROCSYMMIPS_16t;

typedef struct PROCSYMMIPS {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_GPROCMIPS or S_LPROCMIPS
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned long   pNext;      // pointer to next symbol
    unsigned long   len;        // Proc length
    unsigned long   DbgStart;   // Debug start offset
    unsigned long   DbgEnd;     // Debug end offset
    unsigned long   regSave;    // int register save mask
    unsigned long   fpSave;     // fp register save mask
    CV_uoff32_t     intOff;     // int register save offset
    CV_uoff32_t     fpOff;      // fp register save offset
    CV_typ_t        typind;     // Type index
    CV_uoff32_t     off;        // Symbol offset
    unsigned short  seg;        // Symbol segment
    unsigned char   retReg;     // Register return value is in
    unsigned char   frameReg;   // Frame pointer register
    unsigned char   name[1];    // Length-prefixed name
} PROCSYMMIPS;

typedef struct PROCSYMIA64 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_GPROCIA64 or S_LPROCIA64
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned long   pNext;      // pointer to next symbol
    unsigned long   len;        // Proc length
    unsigned long   DbgStart;   // Debug start offset
    unsigned long   DbgEnd;     // Debug end offset
    CV_typ_t        typind;     // Type index
    CV_uoff32_t     off;        // Symbol offset
    unsigned short  seg;        // Symbol segment
    unsigned short  retReg;     // Register return value is in
    CV_PROCFLAGS    flags;      // Proc flags
    unsigned char   name[1];    // Length-prefixed name
} PROCSYMIA64;

typedef struct REFSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_PROCREF_ST, S_DATAREF_ST, or S_LPROCREF_ST
    unsigned long   sumName;    // SUC of the name
    unsigned long   ibSym;      // Offset of actual symbol in $$Symbols
    unsigned short  imod;       // Module containing the actual symbol
    unsigned short  usFill;     // align this record
} REFSYM;

typedef struct REFSYM2 {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_PROCREF, S_DATAREF, or S_LPROCREF
    unsigned long   sumName;    // SUC of the name
    unsigned long   ibSym;      // Offset of actual symbol in $$Symbols
    unsigned short  imod;       // Module containing the actual symbol
    unsigned char   name[1];    // hidden name made a first class member
} REFSYM2;

typedef struct ALIGNSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_ALIGN
} ALIGNSYM;

typedef struct OEMSYMBOL {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_OEM
    unsigned char   idOem[16];  // an oem ID (GUID)
    CV_typ_t        typind;     // Type index
    unsigned long   rgl[];      // user data, force 4-byte alignment
} OEMSYMBOL;

//  generic block definition symbols
//  these are similar to the equivalent 16:16 or 16:32 symbols but
//  only define the length, type and linkage fields

typedef struct PROCSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_GPROC16 or S_LPROC16
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned long   pNext;      // pointer to next symbol
} PROCSYM;


typedef struct THUNKSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_THUNK
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
    unsigned long   pNext;      // pointer to next symbol
} THUNKSYM;

typedef struct BLOCKSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_BLOCK16
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
} BLOCKSYM;


typedef struct WITHSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_WITH16
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this blocks end
} WITHSYM;

typedef struct FRAMEPROCSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_FRAMEPROC
    unsigned long   cbFrame;    // count of bytes of total frame of procedure
    unsigned long   cbPad;      // count of bytes of padding in the frame
    CV_uoff32_t     offPad;     // offset (relative to frame poniter) to where
                                //  padding starts
    unsigned long   cbSaveRegs; // count of bytes of callee save registers
    CV_uoff32_t     offExHdlr;  // offset of exception handler
    unsigned short  sectExHdlr; // section id of exception handler

    struct {
        unsigned long   fHasAlloca  :  1;   // function uses _alloca()
        unsigned long   fHasSetJmp  :  1;   // function uses setjmp()
        unsigned long   fHasLongJmp :  1;   // function uses longjmp()
        unsigned long   fHasInlAsm  :  1;   // function uses inline asm
        unsigned long   fHasEH      :  1;   // function has EH states
        unsigned long   fInlSpec    :  1;   // function was speced as inline
        unsigned long   fHasSEH     :  1;   // function has SEH
        unsigned long   fNaked      :  1;   // function is __declspec(naked)
        unsigned long   fSecurityChecks :  1;   // function has buffer security check introduced by /GS.
        unsigned long   fAsyncEH    :  1;   // function compiled with /EHa
        unsigned long   fGSNoStackOrdering :  1;   // function has /GS buffer checks, but stack ordering couldn't be done
        unsigned long   fWasInlined :  1;   // function was inlined within another function
        unsigned long   fGSCheck    :  1;   // function is __declspec(strict_gs_check)
        unsigned long   fSafeBuffers : 1;   // function is __declspec(safebuffers)
        unsigned long   encodedLocalBasePointer : 2;  // record function's local pointer explicitly.
        unsigned long   encodedParamBasePointer : 2;  // record function's parameter pointer explicitly.
        unsigned long   fPogoOn      : 1;   // function was compiled with PGO/PGU
        unsigned long   fValidCounts : 1;   // Do we have valid Pogo counts?
        unsigned long   fOptSpeed    : 1;  // Did we optimize for speed?
        unsigned long   fGuardCF    :  1;   // function contains CFG checks (and no write checks)
        unsigned long   fGuardCFW   :  1;   // function contains CFW checks and/or instrumentation
        unsigned long   pad          : 9;   // must be zero
    } flags;
} FRAMEPROCSYM;

#ifdef  __cplusplus
namespace CodeViewInfo 
{
__inline unsigned short ExpandEncodedBasePointerReg(unsigned machineType, unsigned encodedFrameReg) 
{
    static const unsigned short rgFramePointerRegX86[] = {
        CV_REG_NONE, CV_ALLREG_VFRAME, CV_REG_EBP, CV_REG_EBX};
    static const unsigned short rgFramePointerRegX64[] = {
        CV_REG_NONE, CV_AMD64_RSP, CV_AMD64_RBP, CV_AMD64_R13};
    static const unsigned short rgFramePointerRegArm[] = {
        CV_REG_NONE, CV_ARM_SP, CV_ARM_R7, CV_REG_NONE};

    if (encodedFrameReg >= 4) {
        return CV_REG_NONE;
    }
    switch (machineType) {
        case CV_CFL_8080 :
        case CV_CFL_8086 :
        case CV_CFL_80286 :
        case CV_CFL_80386 :
        case CV_CFL_80486 :
        case CV_CFL_PENTIUM :
        case CV_CFL_PENTIUMII :
        case CV_CFL_PENTIUMIII :
            return rgFramePointerRegX86[encodedFrameReg];
        case CV_CFL_AMD64 :
            return rgFramePointerRegX64[encodedFrameReg];
        case CV_CFL_ARMNT :
            return rgFramePointerRegArm[encodedFrameReg];
        default:
            return CV_REG_NONE;
    }
}
}
#endif

typedef struct UNAMESPACE {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_UNAMESPACE
    unsigned char   name[1];    // name
} UNAMESPACE;

typedef struct SEPCODESYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_SEPCODE
    unsigned long   pParent;    // pointer to the parent
    unsigned long   pEnd;       // pointer to this block's end
    unsigned long   length;     // count of bytes of this block
    CV_SEPCODEFLAGS scf;        // flags
    CV_uoff32_t     off;        // sect:off of the separated code
    CV_uoff32_t     offParent;  // sectParent:offParent of the enclosing scope
    unsigned short  sect;       //  (proc, block, or sepcode)
    unsigned short  sectParent;
} SEPCODESYM;

typedef struct BUILDINFOSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_BUILDINFO
    CV_ItemId       id;         // CV_ItemId of Build Info.
} BUILDINFOSYM;

typedef struct INLINESITESYM {
    unsigned short  reclen;    // Record length
    unsigned short  rectyp;    // S_INLINESITE
    unsigned long   pParent;   // pointer to the inliner
    unsigned long   pEnd;      // pointer to this block's end
    CV_ItemId       inlinee;   // CV_ItemId of inlinee
    unsigned char   binaryAnnotations[CV_ZEROLEN];   // an array of compressed binary annotations.
} INLINESITESYM;

typedef struct INLINESITESYM2 {
    unsigned short  reclen;         // Record length
    unsigned short  rectyp;         // S_INLINESITE2
    unsigned long   pParent;        // pointer to the inliner
    unsigned long   pEnd;           // pointer to this block's end
    CV_ItemId       inlinee;        // CV_ItemId of inlinee
    unsigned long   invocations;    // entry count
    unsigned char   binaryAnnotations[CV_ZEROLEN];   // an array of compressed binary annotations.
} INLINESITESYM2;


// Defines a locals and it is live range, how to evaluate.
// S_DEFRANGE modifies previous local S_LOCAL, it has to consecutive.

typedef struct LOCALSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_LOCAL
    CV_typ_t        typind;     // type index   
    CV_LVARFLAGS    flags;      // local var flags

    unsigned char   name[CV_ZEROLEN];   // Name of this symbol, a null terminated array of UTF8 characters.
} LOCALSYM;

typedef struct FILESTATICSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_FILESTATIC
    CV_typ_t        typind;     // type index   
    CV_uoff32_t     modOffset;  // index of mod filename in stringtable
    CV_LVARFLAGS    flags;      // local var flags

    unsigned char   name[CV_ZEROLEN];   // Name of this symbol, a null terminated array of UTF8 characters
} FILESTATICSYM;

typedef struct DEFRANGESYM {    // A live range of sub field of variable
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_DEFRANGE

    CV_uoff32_t     program;    // DIA program to evaluate the value of the symbol

    CV_LVAR_ADDR_RANGE range;   // Range of addresses where this program is valid
    CV_LVAR_ADDR_GAP   gaps[CV_ZEROLEN];  // The value is not available in following gaps. 
} DEFRANGESYM;

typedef struct DEFRANGESYMSUBFIELD { // A live range of sub field of variable. like locala.i
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_DEFRANGE_SUBFIELD

    CV_uoff32_t     program;    // DIA program to evaluate the value of the symbol

    CV_uoff32_t     offParent;  // Offset in parent variable.

    CV_LVAR_ADDR_RANGE range;   // Range of addresses where this program is valid
    CV_LVAR_ADDR_GAP   gaps[CV_ZEROLEN];  // The value is not available in following gaps. 
} DEFRANGESYMSUBFIELD;

typedef struct CV_RANGEATTR {
    unsigned short  maybe : 1;    // May have no user name on one of control flow path.
    unsigned short  padding : 15; // Padding for future use.
} CV_RANGEATTR;

typedef struct DEFRANGESYMREGISTER {    // A live range of en-registed variable
    unsigned short     reclen;     // Record length
    unsigned short     rectyp;     // S_DEFRANGE_REGISTER 
    unsigned short     reg;        // Register to hold the value of the symbol
    CV_RANGEATTR       attr;       // Attribute of the register range.
    CV_LVAR_ADDR_RANGE range;      // Range of addresses where this program is valid
    CV_LVAR_ADDR_GAP   gaps[CV_ZEROLEN];  // The value is not available in following gaps. 
} DEFRANGESYMREGISTER;

typedef struct DEFRANGESYMFRAMEPOINTERREL {    // A live range of frame variable
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_DEFRANGE_FRAMEPOINTER_REL

    CV_off32_t      offFramePointer;  // offset to frame pointer

    CV_LVAR_ADDR_RANGE range;   // Range of addresses where this program is valid
    CV_LVAR_ADDR_GAP   gaps[CV_ZEROLEN];  // The value is not available in following gaps. 
} DEFRANGESYMFRAMEPOINTERREL;

typedef struct DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE { // A frame variable valid in all function scope 
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_DEFRANGE_FRAMEPOINTER_REL

    CV_off32_t      offFramePointer;  // offset to frame pointer
} DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE;

#define CV_OFFSET_PARENT_LENGTH_LIMIT 12

// Note DEFRANGESYMREGISTERREL and DEFRANGESYMSUBFIELDREGISTER had same layout. 
typedef struct DEFRANGESYMSUBFIELDREGISTER { // A live range of sub field of variable. like locala.i
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_DEFRANGE_SUBFIELD_REGISTER 

    unsigned short     reg;        // Register to hold the value of the symbol
    CV_RANGEATTR       attr;       // Attribute of the register range.
    CV_uoff32_t        offParent : CV_OFFSET_PARENT_LENGTH_LIMIT;  // Offset in parent variable.
    CV_uoff32_t        padding   : 20;  // Padding for future use.
    CV_LVAR_ADDR_RANGE range;   // Range of addresses where this program is valid
    CV_LVAR_ADDR_GAP   gaps[CV_ZEROLEN];  // The value is not available in following gaps. 
} DEFRANGESYMSUBFIELDREGISTER;

// Note DEFRANGESYMREGISTERREL and DEFRANGESYMSUBFIELDREGISTER had same layout.
// Used when /GS Copy parameter as local variable or other variable don't cover by FRAMERELATIVE.
typedef struct DEFRANGESYMREGISTERREL {    // A live range of variable related to a register.
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_DEFRANGE_REGISTER_REL

    unsigned short  baseReg;         // Register to hold the base pointer of the symbol
    unsigned short  spilledUdtMember : 1;   // Spilled member for s.i.
    unsigned short  padding          : 3;   // Padding for future use.
    unsigned short  offsetParent     : CV_OFFSET_PARENT_LENGTH_LIMIT;  // Offset in parent variable.
    CV_off32_t      offBasePointer;  // offset to register

    CV_LVAR_ADDR_RANGE range;   // Range of addresses where this program is valid
    CV_LVAR_ADDR_GAP   gaps[CV_ZEROLEN];  // The value is not available in following gaps.
} DEFRANGESYMREGISTERREL;

typedef struct DEFRANGESYMHLSL {    // A live range of variable related to a symbol in HLSL code.
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_DEFRANGE_HLSL or S_DEFRANGE_DPC_PTR_TAG

    unsigned short  regType;    // register type from CV_HLSLREG_e

    unsigned short  regIndices       : 2;   // 0, 1 or 2, dimensionality of register space
    unsigned short  spilledUdtMember : 1;   // this is a spilled member
    unsigned short  memorySpace      : 4;   // memory space
    unsigned short  padding          : 9;   // for future use
    
    unsigned short  offsetParent;           // Offset in parent variable.
    unsigned short  sizeInParent;           // Size of enregistered portion

    CV_LVAR_ADDR_RANGE range;               // Range of addresses where this program is valid
    unsigned char   data[CV_ZEROLEN];       // variable length data specifying gaps where the value is not available
                                            // followed by multi-dimensional offset of variable location in register
                                            // space (see CV_DEFRANGESYMHLSL_* macros below)
} DEFRANGESYMHLSL;

#define CV_DEFRANGESYM_GAPS_COUNT(x) \
    (((x)->reclen + sizeof((x)->reclen) - sizeof(DEFRANGESYM)) / sizeof(CV_LVAR_ADDR_GAP))

#define CV_DEFRANGESYMSUBFIELD_GAPS_COUNT(x) \
    (((x)->reclen + sizeof((x)->reclen) - sizeof(DEFRANGESYMSUBFIELD)) / sizeof(CV_LVAR_ADDR_GAP)) 

#define CV_DEFRANGESYMHLSL_GAPS_COUNT(x) \
    (((x)->reclen + sizeof((x)->reclen) - sizeof(DEFRANGESYMHLSL) - (x)->regIndices * sizeof(CV_uoff32_t)) / sizeof(CV_LVAR_ADDR_GAP))

#define CV_DEFRANGESYMHLSL_GAPS_PTR_BASE(x, t)  reinterpret_cast<t>((x)->data)

#define CV_DEFRANGESYMHLSL_GAPS_CONST_PTR(x) \
    CV_DEFRANGESYMHLSL_GAPS_PTR_BASE(x, const CV_LVAR_ADDR_GAP*)

#define CV_DEFRANGESYMHLSL_GAPS_PTR(x) \
    CV_DEFRANGESYMHLSL_GAPS_PTR_BASE(x, CV_LVAR_ADDR_GAP*)

#define CV_DEFRANGESYMHLSL_OFFSET_PTR_BASE(x, t) \
    reinterpret_cast<t>(((CV_LVAR_ADDR_GAP*)(x)->data) + CV_DEFRANGESYMHLSL_GAPS_COUNT(x))
 
#define CV_DEFRANGESYMHLSL_OFFSET_CONST_PTR(x) \
    CV_DEFRANGESYMHLSL_OFFSET_PTR_BASE(x, const CV_uoff32_t*)
 
#define CV_DEFRANGESYMHLSL_OFFSET_PTR(x) \
    CV_DEFRANGESYMHLSL_OFFSET_PTR_BASE(x, CV_uoff32_t*)

#if defined(CC_DP_CXX) && CC_DP_CXX

// Defines a local DPC group shared variable and its location.
typedef struct LOCALDPCGROUPSHAREDSYM {
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_LOCAL_DPC_GROUPSHARED
    CV_typ_t        typind;     // type index   
    CV_LVARFLAGS    flags;      // local var flags

    unsigned short  dataslot;   // Base data (cbuffer, groupshared, etc.) slot
    unsigned short  dataoff;    // Base data byte offset start
    
    unsigned char   name[CV_ZEROLEN];   // Name of this symbol, a null terminated array of UTF8 characters.
} LOCALDPCGROUPSHAREDSYM;

typedef struct DPCSYMTAGMAP {   // A map for DPC pointer tag values to symbol records.
    unsigned short  reclen;     // Record length
    unsigned short  rectyp;     // S_DPC_SYM_TAG_MAP

    CV_DPC_SYM_TAG_MAP_ENTRY mapEntries[CV_ZEROLEN];  // Array of mappings from DPC pointer tag values to symbol record offsets
} DPCSYMTAGMAP;

#define CV_DPCSYMTAGMAP_COUNT(x) \
    (((x)->reclen + sizeof((x)->reclen) - sizeof(DPCSYMTAGMAP)) / sizeof(CV_DPC_SYM_TAG_MAP_ENTRY))

#endif // CC_DP_CXX

typedef enum CV_armswitchtype {
    CV_SWT_INT1         = 0,
    CV_SWT_UINT1        = 1,
    CV_SWT_INT2         = 2,
    CV_SWT_UINT2        = 3,
    CV_SWT_INT4         = 4,
    CV_SWT_UINT4        = 5,
    CV_SWT_POINTER      = 6,
    CV_SWT_UINT1SHL1    = 7,
    CV_SWT_UINT2SHL1    = 8,
    CV_SWT_INT1SHL1     = 9,
    CV_SWT_INT2SHL1     = 10,
    CV_SWT_TBB          = CV_SWT_UINT1SHL1,
    CV_SWT_TBH          = CV_SWT_UINT2SHL1,
} CV_armswitchtype;

typedef struct FUNCTIONLIST {
    unsigned short  reclen;             // Record length
    unsigned short  rectyp;             // S_CALLERS or S_CALLEES

    unsigned long   count;              // Number of functions
    CV_typ_t        funcs[CV_ZEROLEN];  // List of functions, dim == count
    // unsigned long   invocations[CV_ZEROLEN]; Followed by a parallel array of
    // invocation counts. Counts > reclen are assumed to be zero
} FUNCTIONLIST;

typedef struct POGOINFO {
    unsigned short  reclen;             // Record length
    unsigned short  rectyp;             // S_POGODATA

    unsigned long   invocations;        // Number of times function was called
    __int64         dynCount;           // Dynamic instruction count
    unsigned long   numInstrs;          // Static instruction count
    unsigned long   staInstLive;        // Final static instruction count (post inlining)
} POGOINFO;

typedef struct ARMSWITCHTABLE {
    unsigned short  reclen;             // Record length
    unsigned short  rectyp;             // S_ARMSWITCHTABLE

    CV_uoff32_t     offsetBase;         // Section-relative offset to the base for switch offsets
    unsigned short  sectBase;           // Section index of the base for switch offsets
    unsigned short  switchType;         // type of each entry
    CV_uoff32_t     offsetBranch;       // Section-relative offset to the table branch instruction
    CV_uoff32_t     offsetTable;        // Section-relative offset to the start of the table
    unsigned short  sectBranch;         // Section index of the table branch instruction
    unsigned short  sectTable;          // Section index of the table
    unsigned long   cEntries;           // number of switch table entries
} ARMSWITCHTABLE;

typedef struct MODTYPEREF {
    unsigned short  reclen;             // Record length
    unsigned short  rectyp;             // S_MOD_TYPEREF

    unsigned long   fNone     : 1;      // module doesn't reference any type
    unsigned long   fRefTMPCT : 1;      // reference /Z7 PCH types
    unsigned long   fOwnTMPCT : 1;      // module contains /Z7 PCH types
    unsigned long   fOwnTMR   : 1;      // module contains type info (/Z7)
    unsigned long   fOwnTM    : 1;      // module contains type info (/Zi or /ZI)
    unsigned long   fRefTM    : 1;      // module references type info owned by other module
    unsigned long   reserved  : 9;

    unsigned short  word0;              // these two words contain SN or module index depending
    unsigned short  word1;              // on above flags
} MODTYPEREF;

typedef struct SECTIONSYM {
    unsigned short  reclen;             // Record length
    unsigned short  rectyp;             // S_SECTION

    unsigned short  isec;               // Section number
    unsigned char   align;              // Alignment of this section (power of 2)
    unsigned char   bReserved;          // Reserved.  Must be zero.
    unsigned long   rva;
    unsigned long   cb;
    unsigned long   characteristics;
    unsigned char   name[1];            // name
} SECTIONSYM;

typedef struct COFFGROUPSYM {
    unsigned short  reclen;             // Record length
    unsigned short  rectyp;             // S_COFFGROUP

    unsigned long   cb;
    unsigned long   characteristics;
    CV_uoff32_t     off;                // Symbol offset
    unsigned short  seg;                // Symbol segment
    unsigned char   name[1];            // name
} COFFGROUPSYM;

typedef struct EXPORTSYM {
    unsigned short  reclen;             // Record length
    unsigned short  rectyp;             // S_EXPORT

    unsigned short  ordinal;
    unsigned short  fConstant : 1;      // CONSTANT
    unsigned short  fData : 1;          // DATA
    unsigned short  fPrivate : 1;       // PRIVATE
    unsigned short  fNoName : 1;        // NONAME
    unsigned short  fOrdinal : 1;       // Ordinal was explicitly assigned
    unsigned short  fForwarder : 1;     // This is a forwarder
    unsigned short  reserved : 10;      // Reserved. Must be zero.
    unsigned char   name[1];            // name of
} EXPORTSYM;

//
// Symbol for describing indirect calls when they are using
// a function pointer cast on some other type or temporary.
// Typical content will be an LF_POINTER to an LF_PROCEDURE
// type record that should mimic an actual variable with the
// function pointer type in question.
//
// Since the compiler can sometimes tail-merge a function call
// through a function pointer, there may be more than one
// S_CALLSITEINFO record at an address.  This is similar to what
// you could do in your own code by:
//
//  if (expr)
//      pfn = &function1;
//  else
//      pfn = &function2;
//
//  (*pfn)(arg list);
//

typedef struct CALLSITEINFO {
    unsigned short  reclen;             // Record length
    unsigned short  rectyp;             // S_CALLSITEINFO
    CV_off32_t      off;                // offset of call site
    unsigned short  sect;               // section index of call site
    unsigned short  __reserved_0;       // alignment padding field, must be zero
    CV_typ_t        typind;             // type index describing function signature
} CALLSITEINFO;

typedef struct HEAPALLOCSITE {
    unsigned short  reclen;             // Record length
    unsigned short  rectyp;             // S_HEAPALLOCSITE
    CV_off32_t      off;                // offset of call site
    unsigned short  sect;               // section index of call site
    unsigned short  cbInstr;            // length of heap allocation call instruction
    CV_typ_t        typind;             // type index describing function signature
} HEAPALLOCSITE;

// Frame cookie information

typedef enum CV_cookietype_e
{
   CV_COOKIETYPE_COPY = 0, 
   CV_COOKIETYPE_XOR_SP, 
   CV_COOKIETYPE_XOR_BP,
   CV_COOKIETYPE_XOR_R13,
} CV_cookietype_e;

// Symbol for describing security cookie's position and type 
// (raw, xor'd with esp, xor'd with ebp).

typedef struct FRAMECOOKIE {
    unsigned short  reclen;             // Record length
    unsigned short  rectyp;             // S_FRAMECOOKIE
    CV_off32_t      off;                // Frame relative offset
    unsigned short  reg;                // Register index
    CV_cookietype_e cookietype;         // Type of the cookie
    unsigned char   flags;              // Flags describing this cookie
} FRAMECOOKIE;

typedef enum CV_DISCARDED_e
{
   CV_DISCARDED_UNKNOWN,
   CV_DISCARDED_NOT_SELECTED,
   CV_DISCARDED_NOT_REFERENCED,
} CV_DISCARDED_e;

typedef struct DISCARDEDSYM {
    unsigned short  reclen;             // Record length
    unsigned short  rectyp;             // S_DISCARDED
    unsigned long   discarded : 8;      // CV_DISCARDED_e
    unsigned long   reserved : 24;      // Unused
    unsigned long   fileid;             // First FILEID if line number info present
    unsigned long   linenum;            // First line number
    char            data[CV_ZEROLEN];   // Original record(s) with invalid type indices
} DISCARDEDSYM;

typedef struct REFMINIPDB {
    unsigned short  reclen;             // Record length
    unsigned short  rectyp;             // S_REF_MINIPDB
    union {
        unsigned long  isectCoff;       // coff section
        CV_typ_t       typind;          // type index
    };
    unsigned short  imod;               // mod index
    unsigned short  fLocal   :  1;      // reference to local (vs. global) func or data
    unsigned short  fData    :  1;      // reference to data (vs. func)
    unsigned short  fUDT     :  1;      // reference to UDT
    unsigned short  fLabel   :  1;      // reference to label
    unsigned short  fConst   :  1;      // reference to const
    unsigned short  reserved : 11;      // reserved, must be zero
    unsigned char   name[1];            // zero terminated name string
} REFMINIPDB;

typedef struct PDBMAP {
    unsigned short  reclen;             // Record length
    unsigned short  rectyp;             // S_PDBMAP
    unsigned char   name[CV_ZEROLEN];   // zero terminated source PDB filename followed by zero
                                        // terminated destination PDB filename, both in wchar_t
} PDBMAP;

//
// V7 line number data types
//

enum DEBUG_S_SUBSECTION_TYPE {
    DEBUG_S_IGNORE = 0x80000000,    // if this bit is set in a subsection type then ignore the subsection contents

    DEBUG_S_SYMBOLS = 0xf1,
    DEBUG_S_LINES,
    DEBUG_S_STRINGTABLE,
    DEBUG_S_FILECHKSMS,
    DEBUG_S_FRAMEDATA,
    DEBUG_S_INLINEELINES,
    DEBUG_S_CROSSSCOPEIMPORTS,
    DEBUG_S_CROSSSCOPEEXPORTS,

    DEBUG_S_IL_LINES,
    DEBUG_S_FUNC_MDTOKEN_MAP,
    DEBUG_S_TYPE_MDTOKEN_MAP,
    DEBUG_S_MERGED_ASSEMBLYINPUT,

    DEBUG_S_COFF_SYMBOL_RVA,
};

struct CV_DebugSSubsectionHeader_t {
    enum DEBUG_S_SUBSECTION_TYPE type; 
    CV_off32_t                   cbLen;
};

struct CV_DebugSLinesHeader_t {
    CV_off32_t     offCon;
    unsigned short segCon;
    unsigned short flags;
    CV_off32_t     cbCon;
};

struct CV_DebugSLinesFileBlockHeader_t {
    CV_off32_t     offFile;
    CV_off32_t     nLines;
    CV_off32_t     cbBlock;
    // CV_Line_t      lines[nLines];
    // CV_Column_t    columns[nColumns];
};

//
// Line flags (data present)
//
#define CV_LINES_HAVE_COLUMNS 0x0001

struct CV_Line_t {
        unsigned long   offset;             // Offset to start of code bytes for line number
        unsigned long   linenumStart:24;    // line where statement/expression starts
        unsigned long   deltaLineEnd:7;     // delta to line where statement ends (optional)
        unsigned long   fStatement:1;       // true if a statement linenumber, else an expression line num
};

typedef unsigned short CV_columnpos_t;    // byte offset in a source line

struct CV_Column_t {
    CV_columnpos_t offColumnStart;
    CV_columnpos_t offColumnEnd;
};

struct tagFRAMEDATA {
    unsigned long   ulRvaStart;
    unsigned long   cbBlock;
    unsigned long   cbLocals;
    unsigned long   cbParams;
    unsigned long   cbStkMax;
    unsigned long   frameFunc;
    unsigned short  cbProlog;
    unsigned short  cbSavedRegs;
    unsigned long   fHasSEH:1;
    unsigned long   fHasEH:1;
    unsigned long   fIsFunctionStart:1;
    unsigned long   reserved:29;
};

typedef struct tagFRAMEDATA FRAMEDATA, * PFRAMEDATA;

typedef struct tagXFIXUP_DATA {
   unsigned short wType;
   unsigned short wExtra;
   unsigned long rva;
   unsigned long rvaTarget;
} XFIXUP_DATA;

// Those cross scope IDs are private convention, 
// it used to delay the ID merging for frontend and backend even linker. 
// It is transparent for DIA client. 
// Use those ID will let DIA run a litter slower and but 
// avoid the copy type tree in some scenarios.

#ifdef  __cplusplus
namespace CodeViewInfo 
{

typedef struct ComboID
{
    static const unsigned int IndexBitWidth = 20;
    static const unsigned int ImodBitWidth = 12;

    ComboID(unsigned short imod, unsigned int index)
    {
        m_comboID = (((unsigned int) imod) << IndexBitWidth) | index;
    }

    ComboID(unsigned int comboID)
    {
        m_comboID = comboID;
    }

    operator unsigned int()
    {
        return m_comboID;
    }

    unsigned short GetModIndex()
    {
        return (unsigned short) (m_comboID >> IndexBitWidth);
    }

    unsigned int GetIndex()
    {
        return (m_comboID & ((1 << IndexBitWidth) - 1));
    }

private:

    unsigned int m_comboID;
} ComboID;


typedef struct CrossScopeId
{
    static const unsigned int LocalIdBitWidth = 20;
    static const unsigned int IdScopeBitWidth = 11;
    static const unsigned int StartCrossScopeId = 
        (unsigned int) (1 << (LocalIdBitWidth + IdScopeBitWidth));
    static const unsigned int LocalIdMask = (1 << LocalIdBitWidth) - 1;
    static const unsigned int ScopeIdMask = StartCrossScopeId - (1 << LocalIdBitWidth);

    // Compilation unit at most reference 1M constructed type.
    static const unsigned int MaxLocalId = (1 << LocalIdBitWidth) - 1;

    // Compilation unit at most reference to another 2K compilation units.
    static const unsigned int MaxScopeId = (1 << IdScopeBitWidth) - 1;

    CrossScopeId(unsigned short aIdScopeId, unsigned int aLocalId) 
    {  
        crossScopeId = StartCrossScopeId
               | (aIdScopeId << LocalIdBitWidth)
               | aLocalId;
    }

    operator unsigned int() {
        return crossScopeId;
    }

    unsigned int GetLocalId() {
        return crossScopeId & LocalIdMask;
    }

    unsigned int GetIdScopeId() {
        return (crossScopeId & ScopeIdMask) >> LocalIdBitWidth;
    }

    static bool IsCrossScopeId(unsigned int i) 
    {
        return (StartCrossScopeId & i) != 0;
    }

    static CrossScopeId Decode(unsigned int i) 
    {
        CrossScopeId retval;
        retval.crossScopeId = i;
        return retval;
    }

private:

    CrossScopeId() {}

    unsigned int crossScopeId;

} CrossScopeId; 

// Combined encoding of TI or FuncId, In compiler implementation
// Id prefixed by 1 if it is function ID.

typedef struct DecoratedItemId
{
    DecoratedItemId(bool isFuncId, CV_ItemId inputId) {
        if (isFuncId) {
            decoratedItemId = 0x80000000 | inputId;
        } else {
            decoratedItemId = inputId;
        }
    }

    DecoratedItemId(CV_ItemId encodedId) {
        decoratedItemId = encodedId;
    }

    operator unsigned int() {
        return decoratedItemId;
    }

    bool IsFuncId() 
    {
        return (decoratedItemId & 0x80000000) == 0x80000000;
    }

    CV_ItemId GetItemId() 
    {
        return decoratedItemId & 0x7fffffff;
    }

private:

    unsigned int decoratedItemId;

} DecoratedItemId;

// Compilation Unit object file path include library name
// Or compile time PDB full path

typedef struct tagPdbIdScope {
    CV_off32_t  offObjectFilePath; 
} PdbIdScope;

// An array of all imports by import module.
// List all cross reference for a specific ID scope.
// Format of DEBUG_S_CROSSSCOPEIMPORTS subsection is 
typedef struct tagCrossScopeReferences {
    PdbIdScope    externalScope;              // Module of definition Scope.
    unsigned int  countOfCrossReferences;     // Count of following array. 
    CV_ItemId     referenceIds[CV_ZEROLEN];   // CV_ItemId in another compilation unit.
} CrossScopeReferences;

// An array of all exports in this module.
// Format of DEBUG_S_CROSSSCOPEEXPORTS subsection is 
typedef struct tagLocalIdAndGlobalIdPair {
    CV_ItemId localId;    // local id inside the compile time PDB scope. 0 based
    CV_ItemId globalId;   // global id inside the link time PDB scope, if scope are different.
} LocalIdAndGlobalIdPair;

// Format of DEBUG_S_INLINEELINEINFO subsection
// List start source file information for an inlined function.

#define CV_INLINEE_SOURCE_LINE_SIGNATURE     0x0
#define CV_INLINEE_SOURCE_LINE_SIGNATURE_EX  0x1

typedef struct tagInlineeSourceLine {
    CV_ItemId      inlinee;       // function id.
    CV_off32_t     fileId;        // offset into file table DEBUG_S_FILECHKSMS
    CV_off32_t     sourceLineNum; // definition start line number.
} InlineeSourceLine;

typedef struct tagInlineeSourceLineEx {
    CV_ItemId      inlinee;       // function id
    CV_off32_t     fileId;        // offset into file table DEBUG_S_FILECHKSMS
    CV_off32_t     sourceLineNum; // definition start line number
    unsigned int   countOfExtraFiles;
    CV_off32_t     extraFileId[CV_ZEROLEN];
} InlineeSourceLineEx;

// BinaryAnnotations ::= BinaryAnnotationInstruction+
// BinaryAnnotationInstruction ::= BinaryAnnotationOpcode Operand+
//
// The binary annotation mechanism supports recording a list of annotations
// in an instruction stream.  The X64 unwind code and the DWARF standard have
// similar design.
//
// One annotation contains opcode and a number of 32bits operands.
//
// The initial set of annotation instructions are for line number table
// encoding only.  These annotations append to S_INLINESITE record, and
// operands are unsigned except for BA_OP_ChangeLineOffset.

enum BinaryAnnotationOpcode
{
    BA_OP_Invalid,               // link time pdb contains PADDINGs
    BA_OP_CodeOffset,            // param : start offset 
    BA_OP_ChangeCodeOffsetBase,  // param : nth separated code chunk (main code chunk == 0)
    BA_OP_ChangeCodeOffset,      // param : delta of offset
    BA_OP_ChangeCodeLength,      // param : length of code, default next start
    BA_OP_ChangeFile,            // param : fileId 
    BA_OP_ChangeLineOffset,      // param : line offset (signed)
    BA_OP_ChangeLineEndDelta,    // param : how many lines, default 1
    BA_OP_ChangeRangeKind,       // param : either 1 (default, for statement)
                                 //         or 0 (for expression)

    BA_OP_ChangeColumnStart,     // param : start column number, 0 means no column info
    BA_OP_ChangeColumnEndDelta,  // param : end column number delta (signed)

    // Combo opcodes for smaller encoding size.

    BA_OP_ChangeCodeOffsetAndLineOffset,  // param : ((sourceDelta << 4) | CodeDelta)
    BA_OP_ChangeCodeLengthAndCodeOffset,  // param : codeLength, codeOffset

    BA_OP_ChangeColumnEnd,       // param : end column number
};

inline int BinaryAnnotationInstructionOperandCount(BinaryAnnotationOpcode op)
{
    return (op == BA_OP_ChangeCodeLengthAndCodeOffset) ? 2 : 1;
}

///////////////////////////////////////////////////////////////////////////////
//
// This routine a simplified variant from cor.h.
//
// Compress an unsigned integer (iLen) and store the result into pDataOut.
//
// Return value is the number of bytes that the compressed data occupies.  It
// is caller's responsibilityt to ensure *pDataOut has at least 4 bytes to be
// written to.
//
// Note that this function returns -1 if iLen is too big to be compressed.
// We currently can only encode numbers no larger than 0x1FFFFFFF.
//
///////////////////////////////////////////////////////////////////////////////

typedef unsigned __int8 UInt8;
typedef unsigned __int32 UInt32;

typedef UInt8 CompressedAnnotation;
typedef CompressedAnnotation* PCompressedAnnotation;

inline UInt32 CVCompressData(
    UInt32  iLen,       // [IN]  given uncompressed data
    void *  pDataOut)   // [OUT] buffer for the compressed data
{
    UInt8 *pBytes = reinterpret_cast<UInt8 *>(pDataOut);

    if (iLen <= 0x7F) {
        *pBytes = UInt8(iLen);
        return 1;
    }

    if (iLen <= 0x3FFF) {
        *pBytes     = UInt8((iLen >> 8) | 0x80);
        *(pBytes+1) = UInt8(iLen & 0xff);
        return 2;
    }

    if (iLen <= 0x1FFFFFFF) {
        *pBytes     = UInt8((iLen >> 24) | 0xC0);
        *(pBytes+1) = UInt8((iLen >> 16) & 0xff);
        *(pBytes+2) = UInt8((iLen >> 8)  & 0xff);
        *(pBytes+3) = UInt8(iLen & 0xff);
        return 4;
    }

    return (UInt32) -1;
}

///////////////////////////////////////////////////////////////////////////////
//
// Uncompress the data in pData and store the result into pDataOut.
//
// Return value is the uncompressed unsigned integer.  pData is incremented to
// point to the next piece of uncompressed data.
// 
// Returns -1 if what is passed in is incorrectly compressed data, such as
// (*pBytes & 0xE0) == 0xE0.
//
///////////////////////////////////////////////////////////////////////////////

inline UInt32 CVUncompressData(
    PCompressedAnnotation & pData)    // [IN,OUT] compressed data 
{
    UInt32 res = (UInt32)(-1);

    if ((*pData & 0x80) == 0x00) {
        // 0??? ????

        res = (UInt32)(*pData++);
    }
    else if ((*pData & 0xC0) == 0x80) {
        // 10?? ????

        res = (UInt32)((*pData++ & 0x3f) << 8);
        res |= *pData++;
    }
    else if ((*pData & 0xE0) == 0xC0) {
        // 110? ???? 

        res = (*pData++ & 0x1f) << 24;
        res |= *pData++ << 16;
        res |= *pData++ << 8;
        res |= *pData++;
    }

    return res; 
}

// Encode smaller absolute numbers with smaller buffer.
//
// General compression only work for input < 0x1FFFFFFF 
// algorithm will not work on 0x80000000 

inline unsigned __int32 EncodeSignedInt32(__int32 input)
{
    unsigned __int32 rotatedInput;

    if (input >= 0) {
        rotatedInput = input << 1;
    } else {
        rotatedInput = ((-input) << 1) | 1;
    }

    return rotatedInput;
}

inline __int32 DecodeSignedInt32(unsigned __int32 input)
{
    __int32 rotatedInput;

    if (input & 1) {
        rotatedInput = - (int)(input >> 1);
    } else {
        rotatedInput = input >> 1;
    }

    return rotatedInput;
}

}
#endif
#pragma pack ( pop )

#endif /* CV_INFO_INCLUDED */
