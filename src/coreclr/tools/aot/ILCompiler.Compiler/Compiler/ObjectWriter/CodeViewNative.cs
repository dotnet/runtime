// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILCompiler.ObjectWriter
{
    internal static class CodeViewNative
    {
        public const uint T_NOTYPE        = 0x0000;   // uncharacterized type (no type)
        public const uint T_ABS           = 0x0001;   // absolute symbol
        public const uint T_SEGMENT       = 0x0002;   // segment type
        public const uint T_VOID          = 0x0003;   // void
        public const uint T_HRESULT       = 0x0008;   // OLE/COM HRESULT
        public const uint T_32PHRESULT    = 0x0408;   // OLE/COM HRESULT __ptr32 *
        public const uint T_64PHRESULT    = 0x0608;   // OLE/COM HRESULT __ptr64 *

        public const uint T_PVOID         = 0x0103;   // near pointer to void
        public const uint T_PFVOID        = 0x0203;   // far pointer to void
        public const uint T_PHVOID        = 0x0303;   // huge pointer to void
        public const uint T_32PVOID       = 0x0403;   // 32 bit pointer to void
        public const uint T_32PFVOID      = 0x0503;   // 16:32 pointer to void
        public const uint T_64PVOID       = 0x0603;   // 64 bit pointer to void
        public const uint T_CURRENCY      = 0x0004;   // BASIC 8 byte currency value
        public const uint T_NBASICSTR     = 0x0005;   // Near BASIC string
        public const uint T_FBASICSTR     = 0x0006;   // Far BASIC string
        public const uint T_NOTTRANS      = 0x0007;   // type not translated by cvpack
        public const uint T_BIT           = 0x0060;   // bit
        public const uint T_PASCHAR       = 0x0061;   // Pascal CHAR
        public const uint T_BOOL32FF      = 0x0062;   // 32-bit BOOL where true is 0xffffffff

        // Character types
        public const uint T_CHAR          = 0x0010;   // 8 bit signed
        public const uint T_PCHAR         = 0x0110;   // 16 bit pointer to 8 bit signed
        public const uint T_PFCHAR        = 0x0210;   // 16:16 far pointer to 8 bit signed
        public const uint T_PHCHAR        = 0x0310;   // 16:16 huge pointer to 8 bit signed
        public const uint T_32PCHAR       = 0x0410;   // 32 bit pointer to 8 bit signed
        public const uint T_32PFCHAR      = 0x0510;   // 16:32 pointer to 8 bit signed
        public const uint T_64PCHAR       = 0x0610;   // 64 bit pointer to 8 bit signed

        public const uint T_UCHAR         = 0x0020;   // 8 bit unsigned
        public const uint T_PUCHAR        = 0x0120;   // 16 bit pointer to 8 bit unsigned
        public const uint T_PFUCHAR       = 0x0220;   // 16:16 far pointer to 8 bit unsigned
        public const uint T_PHUCHAR       = 0x0320;   // 16:16 huge pointer to 8 bit unsigned
        public const uint T_32PUCHAR      = 0x0420;   // 32 bit pointer to 8 bit unsigned
        public const uint T_32PFUCHAR     = 0x0520;   // 16:32 pointer to 8 bit unsigned
        public const uint T_64PUCHAR      = 0x0620;   // 64 bit pointer to 8 bit unsigned

        // Really a character types
        public const uint T_RCHAR         = 0x0070;   // really a char
        public const uint T_PRCHAR        = 0x0170;   // 16 bit pointer to a real char
        public const uint T_PFRCHAR       = 0x0270;   // 16:16 far pointer to a real char
        public const uint T_PHRCHAR       = 0x0370;   // 16:16 huge pointer to a real char
        public const uint T_32PRCHAR      = 0x0470;   // 32 bit pointer to a real char
        public const uint T_32PFRCHAR     = 0x0570;   // 16:32 pointer to a real char
        public const uint T_64PRCHAR      = 0x0670;   // 64 bit pointer to a real char

        // really a wide character types
        public const uint T_WCHAR         = 0x0071;   // wide char
        public const uint T_PWCHAR        = 0x0171;   // 16 bit pointer to a wide char
        public const uint T_PFWCHAR       = 0x0271;   // 16:16 far pointer to a wide char
        public const uint T_PHWCHAR       = 0x0371;   // 16:16 huge pointer to a wide char
        public const uint T_32PWCHAR      = 0x0471;   // 32 bit pointer to a wide char
        public const uint T_32PFWCHAR     = 0x0571;   // 16:32 pointer to a wide char
        public const uint T_64PWCHAR      = 0x0671;   // 64 bit pointer to a wide char

        // really a 16-bit unicode char
        public const uint T_CHAR16         = 0x007a;   // 16-bit unicode char
        public const uint T_PCHAR16        = 0x017a;   // 16 bit pointer to a 16-bit unicode char
        public const uint T_PFCHAR16       = 0x027a;   // 16:16 far pointer to a 16-bit unicode char
        public const uint T_PHCHAR16       = 0x037a;   // 16:16 huge pointer to a 16-bit unicode char
        public const uint T_32PCHAR16      = 0x047a;   // 32 bit pointer to a 16-bit unicode char
        public const uint T_32PFCHAR16     = 0x057a;   // 16:32 pointer to a 16-bit unicode char
        public const uint T_64PCHAR16      = 0x067a;   // 64 bit pointer to a 16-bit unicode char

        // really a 32-bit unicode char
        public const uint T_CHAR32         = 0x007b;   // 32-bit unicode char
        public const uint T_PCHAR32        = 0x017b;   // 16 bit pointer to a 32-bit unicode char
        public const uint T_PFCHAR32       = 0x027b;   // 16:16 far pointer to a 32-bit unicode char
        public const uint T_PHCHAR32       = 0x037b;   // 16:16 huge pointer to a 32-bit unicode char
        public const uint T_32PCHAR32      = 0x047b;   // 32 bit pointer to a 32-bit unicode char
        public const uint T_32PFCHAR32     = 0x057b;   // 16:32 pointer to a 32-bit unicode char
        public const uint T_64PCHAR32      = 0x067b;   // 64 bit pointer to a 32-bit unicode char

        // 8 bit int types
        public const uint T_INT1          = 0x0068;   // 8 bit signed int
        public const uint T_PINT1         = 0x0168;   // 16 bit pointer to 8 bit signed int
        public const uint T_PFINT1        = 0x0268;   // 16:16 far pointer to 8 bit signed int
        public const uint T_PHINT1        = 0x0368;   // 16:16 huge pointer to 8 bit signed int
        public const uint T_32PINT1       = 0x0468;   // 32 bit pointer to 8 bit signed int
        public const uint T_32PFINT1      = 0x0568;   // 16:32 pointer to 8 bit signed int
        public const uint T_64PINT1       = 0x0668;   // 64 bit pointer to 8 bit signed int

        public const uint T_UINT1         = 0x0069;   // 8 bit unsigned int
        public const uint T_PUINT1        = 0x0169;   // 16 bit pointer to 8 bit unsigned int
        public const uint T_PFUINT1       = 0x0269;   // 16:16 far pointer to 8 bit unsigned int
        public const uint T_PHUINT1       = 0x0369;   // 16:16 huge pointer to 8 bit unsigned int
        public const uint T_32PUINT1      = 0x0469;   // 32 bit pointer to 8 bit unsigned int
        public const uint T_32PFUINT1     = 0x0569;   // 16:32 pointer to 8 bit unsigned int
        public const uint T_64PUINT1      = 0x0669;   // 64 bit pointer to 8 bit unsigned int

        // 16 bit short types
        public const uint T_SHORT         = 0x0011;   // 16 bit signed
        public const uint T_PSHORT        = 0x0111;   // 16 bit pointer to 16 bit signed
        public const uint T_PFSHORT       = 0x0211;   // 16:16 far pointer to 16 bit signed
        public const uint T_PHSHORT       = 0x0311;   // 16:16 huge pointer to 16 bit signed
        public const uint T_32PSHORT      = 0x0411;   // 32 bit pointer to 16 bit signed
        public const uint T_32PFSHORT     = 0x0511;   // 16:32 pointer to 16 bit signed
        public const uint T_64PSHORT      = 0x0611;   // 64 bit pointer to 16 bit signed

        public const uint T_USHORT        = 0x0021;   // 16 bit unsigned
        public const uint T_PUSHORT       = 0x0121;   // 16 bit pointer to 16 bit unsigned
        public const uint T_PFUSHORT      = 0x0221;   // 16:16 far pointer to 16 bit unsigned
        public const uint T_PHUSHORT      = 0x0321;   // 16:16 huge pointer to 16 bit unsigned
        public const uint T_32PUSHORT     = 0x0421;   // 32 bit pointer to 16 bit unsigned
        public const uint T_32PFUSHORT    = 0x0521;   // 16:32 pointer to 16 bit unsigned
        public const uint T_64PUSHORT     = 0x0621;   // 64 bit pointer to 16 bit unsigned

        // 16 bit int types
        public const uint T_INT2          = 0x0072;   // 16 bit signed int
        public const uint T_PINT2         = 0x0172;   // 16 bit pointer to 16 bit signed int
        public const uint T_PFINT2        = 0x0272;   // 16:16 far pointer to 16 bit signed int
        public const uint T_PHINT2        = 0x0372;   // 16:16 huge pointer to 16 bit signed int
        public const uint T_32PINT2       = 0x0472;   // 32 bit pointer to 16 bit signed int
        public const uint T_32PFINT2      = 0x0572;   // 16:32 pointer to 16 bit signed int
        public const uint T_64PINT2       = 0x0672;   // 64 bit pointer to 16 bit signed int

        public const uint T_UINT2         = 0x0073;   // 16 bit unsigned int
        public const uint T_PUINT2        = 0x0173;   // 16 bit pointer to 16 bit unsigned int
        public const uint T_PFUINT2       = 0x0273;   // 16:16 far pointer to 16 bit unsigned int
        public const uint T_PHUINT2       = 0x0373;   // 16:16 huge pointer to 16 bit unsigned int
        public const uint T_32PUINT2      = 0x0473;   // 32 bit pointer to 16 bit unsigned int
        public const uint T_32PFUINT2     = 0x0573;   // 16:32 pointer to 16 bit unsigned int
        public const uint T_64PUINT2      = 0x0673;   // 64 bit pointer to 16 bit unsigned int

        // 32 bit long types
        public const uint T_LONG          = 0x0012;   // 32 bit signed
        public const uint T_ULONG         = 0x0022;   // 32 bit unsigned
        public const uint T_PLONG         = 0x0112;   // 16 bit pointer to 32 bit signed
        public const uint T_PULONG        = 0x0122;   // 16 bit pointer to 32 bit unsigned
        public const uint T_PFLONG        = 0x0212;   // 16:16 far pointer to 32 bit signed
        public const uint T_PFULONG       = 0x0222;   // 16:16 far pointer to 32 bit unsigned
        public const uint T_PHLONG        = 0x0312;   // 16:16 huge pointer to 32 bit signed
        public const uint T_PHULONG       = 0x0322;   // 16:16 huge pointer to 32 bit unsigned

        public const uint T_32PLONG       = 0x0412;   // 32 bit pointer to 32 bit signed
        public const uint T_32PULONG      = 0x0422;   // 32 bit pointer to 32 bit unsigned
        public const uint T_32PFLONG      = 0x0512;   // 16:32 pointer to 32 bit signed
        public const uint T_32PFULONG     = 0x0522;   // 16:32 pointer to 32 bit unsigned
        public const uint T_64PLONG       = 0x0612;   // 64 bit pointer to 32 bit signed
        public const uint T_64PULONG      = 0x0622;   // 64 bit pointer to 32 bit unsigned

        // 32 bit int types
        public const uint T_INT4          = 0x0074;   // 32 bit signed int
        public const uint T_PINT4         = 0x0174;   // 16 bit pointer to 32 bit signed int
        public const uint T_PFINT4        = 0x0274;   // 16:16 far pointer to 32 bit signed int
        public const uint T_PHINT4        = 0x0374;   // 16:16 huge pointer to 32 bit signed int
        public const uint T_32PINT4       = 0x0474;   // 32 bit pointer to 32 bit signed int
        public const uint T_32PFINT4      = 0x0574;   // 16:32 pointer to 32 bit signed int
        public const uint T_64PINT4       = 0x0674;   // 64 bit pointer to 32 bit signed int

        public const uint T_UINT4         = 0x0075;   // 32 bit unsigned int
        public const uint T_PUINT4        = 0x0175;   // 16 bit pointer to 32 bit unsigned int
        public const uint T_PFUINT4       = 0x0275;   // 16:16 far pointer to 32 bit unsigned int
        public const uint T_PHUINT4       = 0x0375;   // 16:16 huge pointer to 32 bit unsigned int
        public const uint T_32PUINT4      = 0x0475;   // 32 bit pointer to 32 bit unsigned int
        public const uint T_32PFUINT4     = 0x0575;   // 16:32 pointer to 32 bit unsigned int
        public const uint T_64PUINT4      = 0x0675;   // 64 bit pointer to 32 bit unsigned int

        // 64 bit quad types
        public const uint T_QUAD          = 0x0013;   // 64 bit signed
        public const uint T_PQUAD         = 0x0113;   // 16 bit pointer to 64 bit signed
        public const uint T_PFQUAD        = 0x0213;   // 16:16 far pointer to 64 bit signed
        public const uint T_PHQUAD        = 0x0313;   // 16:16 huge pointer to 64 bit signed
        public const uint T_32PQUAD       = 0x0413;   // 32 bit pointer to 64 bit signed
        public const uint T_32PFQUAD      = 0x0513;   // 16:32 pointer to 64 bit signed
        public const uint T_64PQUAD       = 0x0613;   // 64 bit pointer to 64 bit signed

        public const uint T_UQUAD         = 0x0023;   // 64 bit unsigned
        public const uint T_PUQUAD        = 0x0123;   // 16 bit pointer to 64 bit unsigned
        public const uint T_PFUQUAD       = 0x0223;   // 16:16 far pointer to 64 bit unsigned
        public const uint T_PHUQUAD       = 0x0323;   // 16:16 huge pointer to 64 bit unsigned
        public const uint T_32PUQUAD      = 0x0423;   // 32 bit pointer to 64 bit unsigned
        public const uint T_32PFUQUAD     = 0x0523;   // 16:32 pointer to 64 bit unsigned
        public const uint T_64PUQUAD      = 0x0623;   // 64 bit pointer to 64 bit unsigned

        // 64 bit int types
        public const uint T_INT8          = 0x0076;   // 64 bit signed int
        public const uint T_PINT8         = 0x0176;   // 16 bit pointer to 64 bit signed int
        public const uint T_PFINT8        = 0x0276;   // 16:16 far pointer to 64 bit signed int
        public const uint T_PHINT8        = 0x0376;   // 16:16 huge pointer to 64 bit signed int
        public const uint T_32PINT8       = 0x0476;   // 32 bit pointer to 64 bit signed int
        public const uint T_32PFINT8      = 0x0576;   // 16:32 pointer to 64 bit signed int
        public const uint T_64PINT8       = 0x0676;   // 64 bit pointer to 64 bit signed int

        public const uint T_UINT8         = 0x0077;   // 64 bit unsigned int
        public const uint T_PUINT8        = 0x0177;   // 16 bit pointer to 64 bit unsigned int
        public const uint T_PFUINT8       = 0x0277;   // 16:16 far pointer to 64 bit unsigned int
        public const uint T_PHUINT8       = 0x0377;   // 16:16 huge pointer to 64 bit unsigned int
        public const uint T_32PUINT8      = 0x0477;   // 32 bit pointer to 64 bit unsigned int
        public const uint T_32PFUINT8     = 0x0577;   // 16:32 pointer to 64 bit unsigned int
        public const uint T_64PUINT8      = 0x0677;   // 64 bit pointer to 64 bit unsigned int

        // 128 bit octet types
        public const uint T_OCT           = 0x0014;   // 128 bit signed
        public const uint T_POCT          = 0x0114;   // 16 bit pointer to 128 bit signed
        public const uint T_PFOCT         = 0x0214;   // 16:16 far pointer to 128 bit signed
        public const uint T_PHOCT         = 0x0314;   // 16:16 huge pointer to 128 bit signed
        public const uint T_32POCT        = 0x0414;   // 32 bit pointer to 128 bit signed
        public const uint T_32PFOCT       = 0x0514;   // 16:32 pointer to 128 bit signed
        public const uint T_64POCT        = 0x0614;   // 64 bit pointer to 128 bit signed

        public const uint T_UOCT          = 0x0024;   // 128 bit unsigned
        public const uint T_PUOCT         = 0x0124;   // 16 bit pointer to 128 bit unsigned
        public const uint T_PFUOCT        = 0x0224;   // 16:16 far pointer to 128 bit unsigned
        public const uint T_PHUOCT        = 0x0324;   // 16:16 huge pointer to 128 bit unsigned
        public const uint T_32PUOCT       = 0x0424;   // 32 bit pointer to 128 bit unsigned
        public const uint T_32PFUOCT      = 0x0524;   // 16:32 pointer to 128 bit unsigned
        public const uint T_64PUOCT       = 0x0624;   // 64 bit pointer to 128 bit unsigned

        // 128 bit int types
        public const uint T_INT16         = 0x0078;   // 128 bit signed int
        public const uint T_PINT16        = 0x0178;   // 16 bit pointer to 128 bit signed int
        public const uint T_PFINT16       = 0x0278;   // 16:16 far pointer to 128 bit signed int
        public const uint T_PHINT16       = 0x0378;   // 16:16 huge pointer to 128 bit signed int
        public const uint T_32PINT16      = 0x0478;   // 32 bit pointer to 128 bit signed int
        public const uint T_32PFINT16     = 0x0578;   // 16:32 pointer to 128 bit signed int
        public const uint T_64PINT16      = 0x0678;   // 64 bit pointer to 128 bit signed int

        public const uint T_UINT16        = 0x0079;   // 128 bit unsigned int
        public const uint T_PUINT16       = 0x0179;   // 16 bit pointer to 128 bit unsigned int
        public const uint T_PFUINT16      = 0x0279;   // 16:16 far pointer to 128 bit unsigned int
        public const uint T_PHUINT16      = 0x0379;   // 16:16 huge pointer to 128 bit unsigned int
        public const uint T_32PUINT16     = 0x0479;   // 32 bit pointer to 128 bit unsigned int
        public const uint T_32PFUINT16    = 0x0579;   // 16:32 pointer to 128 bit unsigned int
        public const uint T_64PUINT16     = 0x0679;   // 64 bit pointer to 128 bit unsigned int

        // 16 bit real types
        public const uint T_REAL16        = 0x0046;   // 16 bit real
        public const uint T_PREAL16       = 0x0146;   // 16 bit pointer to 16 bit real
        public const uint T_PFREAL16      = 0x0246;   // 16:16 far pointer to 16 bit real
        public const uint T_PHREAL16      = 0x0346;   // 16:16 huge pointer to 16 bit real
        public const uint T_32PREAL16     = 0x0446;   // 32 bit pointer to 16 bit real
        public const uint T_32PFREAL16    = 0x0546;   // 16:32 pointer to 16 bit real
        public const uint T_64PREAL16     = 0x0646;   // 64 bit pointer to 16 bit real

        // 32 bit real types
        public const uint T_REAL32        = 0x0040;   // 32 bit real
        public const uint T_PREAL32       = 0x0140;   // 16 bit pointer to 32 bit real
        public const uint T_PFREAL32      = 0x0240;   // 16:16 far pointer to 32 bit real
        public const uint T_PHREAL32      = 0x0340;   // 16:16 huge pointer to 32 bit real
        public const uint T_32PREAL32     = 0x0440;   // 32 bit pointer to 32 bit real
        public const uint T_32PFREAL32    = 0x0540;   // 16:32 pointer to 32 bit real
        public const uint T_64PREAL32     = 0x0640;   // 64 bit pointer to 32 bit real

        // 32 bit partial-precision real types
        public const uint T_REAL32PP      = 0x0045;   // 32 bit PP real
        public const uint T_PREAL32PP     = 0x0145;   // 16 bit pointer to 32 bit PP real
        public const uint T_PFREAL32PP    = 0x0245;   // 16:16 far pointer to 32 bit PP real
        public const uint T_PHREAL32PP    = 0x0345;   // 16:16 huge pointer to 32 bit PP real
        public const uint T_32PREAL32PP   = 0x0445;   // 32 bit pointer to 32 bit PP real
        public const uint T_32PFREAL32PP  = 0x0545;   // 16:32 pointer to 32 bit PP real
        public const uint T_64PREAL32PP   = 0x0645;   // 64 bit pointer to 32 bit PP real


        // 48 bit real types
        public const uint T_REAL48        = 0x0044;   // 48 bit real
        public const uint T_PREAL48       = 0x0144;   // 16 bit pointer to 48 bit real
        public const uint T_PFREAL48      = 0x0244;   // 16:16 far pointer to 48 bit real
        public const uint T_PHREAL48      = 0x0344;   // 16:16 huge pointer to 48 bit real
        public const uint T_32PREAL48     = 0x0444;   // 32 bit pointer to 48 bit real
        public const uint T_32PFREAL48    = 0x0544;   // 16:32 pointer to 48 bit real
        public const uint T_64PREAL48     = 0x0644;   // 64 bit pointer to 48 bit real

        // 64 bit real types
        public const uint T_REAL64        = 0x0041;   // 64 bit real
        public const uint T_PREAL64       = 0x0141;   // 16 bit pointer to 64 bit real
        public const uint T_PFREAL64      = 0x0241;   // 16:16 far pointer to 64 bit real
        public const uint T_PHREAL64      = 0x0341;   // 16:16 huge pointer to 64 bit real
        public const uint T_32PREAL64     = 0x0441;   // 32 bit pointer to 64 bit real
        public const uint T_32PFREAL64    = 0x0541;   // 16:32 pointer to 64 bit real
        public const uint T_64PREAL64     = 0x0641;   // 64 bit pointer to 64 bit real

        // 80 bit real types
        public const uint T_REAL80        = 0x0042;   // 80 bit real
        public const uint T_PREAL80       = 0x0142;   // 16 bit pointer to 80 bit real
        public const uint T_PFREAL80      = 0x0242;   // 16:16 far pointer to 80 bit real
        public const uint T_PHREAL80      = 0x0342;   // 16:16 huge pointer to 80 bit real
        public const uint T_32PREAL80     = 0x0442;   // 32 bit pointer to 80 bit real
        public const uint T_32PFREAL80    = 0x0542;   // 16:32 pointer to 80 bit real
        public const uint T_64PREAL80     = 0x0642;   // 64 bit pointer to 80 bit real

        // 128 bit real types
        public const uint T_REAL128       = 0x0043;   // 128 bit real
        public const uint T_PREAL128      = 0x0143;   // 16 bit pointer to 128 bit real
        public const uint T_PFREAL128     = 0x0243;   // 16:16 far pointer to 128 bit real
        public const uint T_PHREAL128     = 0x0343;   // 16:16 huge pointer to 128 bit real
        public const uint T_32PREAL128    = 0x0443;   // 32 bit pointer to 128 bit real
        public const uint T_32PFREAL128   = 0x0543;   // 16:32 pointer to 128 bit real
        public const uint T_64PREAL128    = 0x0643;   // 64 bit pointer to 128 bit real

        // 32 bit complex types
        public const uint T_CPLX32        = 0x0050;   // 32 bit complex
        public const uint T_PCPLX32       = 0x0150;   // 16 bit pointer to 32 bit complex
        public const uint T_PFCPLX32      = 0x0250;   // 16:16 far pointer to 32 bit complex
        public const uint T_PHCPLX32      = 0x0350;   // 16:16 huge pointer to 32 bit complex
        public const uint T_32PCPLX32     = 0x0450;   // 32 bit pointer to 32 bit complex
        public const uint T_32PFCPLX32    = 0x0550;   // 16:32 pointer to 32 bit complex
        public const uint T_64PCPLX32     = 0x0650;   // 64 bit pointer to 32 bit complex

        // 64 bit complex types
        public const uint T_CPLX64        = 0x0051;   // 64 bit complex
        public const uint T_PCPLX64       = 0x0151;   // 16 bit pointer to 64 bit complex
        public const uint T_PFCPLX64      = 0x0251;   // 16:16 far pointer to 64 bit complex
        public const uint T_PHCPLX64      = 0x0351;   // 16:16 huge pointer to 64 bit complex
        public const uint T_32PCPLX64     = 0x0451;   // 32 bit pointer to 64 bit complex
        public const uint T_32PFCPLX64    = 0x0551;   // 16:32 pointer to 64 bit complex
        public const uint T_64PCPLX64     = 0x0651;   // 64 bit pointer to 64 bit complex

        // 80 bit complex types
        public const uint T_CPLX80        = 0x0052;   // 80 bit complex
        public const uint T_PCPLX80       = 0x0152;   // 16 bit pointer to 80 bit complex
        public const uint T_PFCPLX80      = 0x0252;   // 16:16 far pointer to 80 bit complex
        public const uint T_PHCPLX80      = 0x0352;   // 16:16 huge pointer to 80 bit complex
        public const uint T_32PCPLX80     = 0x0452;   // 32 bit pointer to 80 bit complex
        public const uint T_32PFCPLX80    = 0x0552;   // 16:32 pointer to 80 bit complex
        public const uint T_64PCPLX80     = 0x0652;   // 64 bit pointer to 80 bit complex

        // 128 bit complex types
        public const uint T_CPLX128       = 0x0053;   // 128 bit complex
        public const uint T_PCPLX128      = 0x0153;   // 16 bit pointer to 128 bit complex
        public const uint T_PFCPLX128     = 0x0253;   // 16:16 far pointer to 128 bit complex
        public const uint T_PHCPLX128     = 0x0353;   // 16:16 huge pointer to 128 bit real
        public const uint T_32PCPLX128    = 0x0453;   // 32 bit pointer to 128 bit complex
        public const uint T_32PFCPLX128   = 0x0553;   // 16:32 pointer to 128 bit complex
        public const uint T_64PCPLX128    = 0x0653;   // 64 bit pointer to 128 bit complex

        // Boolean types
        public const uint T_BOOL08        = 0x0030;   // 8 bit boolean
        public const uint T_PBOOL08       = 0x0130;   // 16 bit pointer to  8 bit boolean
        public const uint T_PFBOOL08      = 0x0230;   // 16:16 far pointer to  8 bit boolean
        public const uint T_PHBOOL08      = 0x0330;   // 16:16 huge pointer to  8 bit boolean
        public const uint T_32PBOOL08     = 0x0430;   // 32 bit pointer to 8 bit boolean
        public const uint T_32PFBOOL08    = 0x0530;   // 16:32 pointer to 8 bit boolean
        public const uint T_64PBOOL08     = 0x0630;   // 64 bit pointer to 8 bit boolean

        public const uint T_BOOL16        = 0x0031;   // 16 bit boolean
        public const uint T_PBOOL16       = 0x0131;   // 16 bit pointer to 16 bit boolean
        public const uint T_PFBOOL16      = 0x0231;   // 16:16 far pointer to 16 bit boolean
        public const uint T_PHBOOL16      = 0x0331;   // 16:16 huge pointer to 16 bit boolean
        public const uint T_32PBOOL16     = 0x0431;   // 32 bit pointer to 18 bit boolean
        public const uint T_32PFBOOL16    = 0x0531;   // 16:32 pointer to 16 bit boolean
        public const uint T_64PBOOL16     = 0x0631;   // 64 bit pointer to 18 bit boolean

        public const uint T_BOOL32        = 0x0032;   // 32 bit boolean
        public const uint T_PBOOL32       = 0x0132;   // 16 bit pointer to 32 bit boolean
        public const uint T_PFBOOL32      = 0x0232;   // 16:16 far pointer to 32 bit boolean
        public const uint T_PHBOOL32      = 0x0332;   // 16:16 huge pointer to 32 bit boolean
        public const uint T_32PBOOL32     = 0x0432;   // 32 bit pointer to 32 bit boolean
        public const uint T_32PFBOOL32    = 0x0532;   // 16:32 pointer to 32 bit boolean
        public const uint T_64PBOOL32     = 0x0632;   // 64 bit pointer to 32 bit boolean

        public const uint T_BOOL64        = 0x0033;   // 64 bit boolean
        public const uint T_PBOOL64       = 0x0133;   // 16 bit pointer to 64 bit boolean
        public const uint T_PFBOOL64      = 0x0233;   // 16:16 far pointer to 64 bit boolean
        public const uint T_PHBOOL64      = 0x0333;   // 16:16 huge pointer to 64 bit boolean
        public const uint T_32PBOOL64     = 0x0433;   // 32 bit pointer to 64 bit boolean
        public const uint T_32PFBOOL64    = 0x0533;   // 16:32 pointer to 64 bit boolean
        public const uint T_64PBOOL64     = 0x0633;   // 64 bit pointer to 64 bit boolean

        // ???
        public const uint T_NCVPTR        = 0x01f0;   // CV Internal type for created near pointers
        public const uint T_FCVPTR        = 0x02f0;   // CV Internal type for created far pointers
        public const uint T_HCVPTR        = 0x03f0;   // CV Internal type for created huge pointers
        public const uint T_32NCVPTR      = 0x04f0;   // CV Internal type for created near 32-bit pointers
        public const uint T_32FCVPTR      = 0x05f0;   // CV Internal type for created far 32-bit pointers
        public const uint T_64NCVPTR      = 0x06f0;   // CV Internal type for created near 64-bit pointers

        // Type enum for pointer records
        // Pointers can be one of the following types
        public const uint CV_PTR_NEAR         = 0x00; // 16 bit pointer
        public const uint CV_PTR_FAR          = 0x01; // 16:16 far pointer
        public const uint CV_PTR_HUGE         = 0x02; // 16:16 huge pointer
        public const uint CV_PTR_BASE_SEG     = 0x03; // based on segment
        public const uint CV_PTR_BASE_VAL     = 0x04; // based on value of base
        public const uint CV_PTR_BASE_SEGVAL  = 0x05; // based on segment value of base
        public const uint CV_PTR_BASE_ADDR    = 0x06; // based on address of base
        public const uint CV_PTR_BASE_SEGADDR = 0x07; // based on segment address of base
        public const uint CV_PTR_BASE_TYPE    = 0x08; // based on type
        public const uint CV_PTR_BASE_SELF    = 0x09; // based on self
        public const uint CV_PTR_NEAR32       = 0x0a; // 32 bit pointer
        public const uint CV_PTR_FAR32        = 0x0b; // 16:32 pointer
        public const uint CV_PTR_64           = 0x0c; // 64 bit pointer
        public const uint CV_PTR_UNUSEDPTR    = 0x0d; // first unused pointer type

        public const uint CV_PTR_MODE_PTR     = 0x00; // "normal" pointer
        public const uint CV_PTR_MODE_REF     = 0x01; // "old" reference
        public const uint CV_PTR_MODE_LVREF   = 0x01; // l-value reference
        public const uint CV_PTR_MODE_PMEM    = 0x02; // pointer to data member
        public const uint CV_PTR_MODE_PMFUNC  = 0x03; // pointer to member function
        public const uint CV_PTR_MODE_RVREF   = 0x04; // r-value reference
        public const uint CV_PTR_MODE_RESERVED= 0x05; // first unused pointer mode

        public const ushort MOD_none = 0;
        public const ushort MOD_const = 1;
        public const ushort MOD_volatile = 2;
        public const ushort MOD_unaligned = 4;

        public const ushort CV_PROP_NONE = 0;
        public const ushort CV_PROP_PACKED = 0x0001;
        public const ushort CV_PROP_HAS_CONSTURUCTOR_OR_DESTRUCTOR = 0x0002;
        public const ushort CV_PROP_HAS_OVERLOADED_OPERATOR = 0x0004;
        public const ushort CV_PROP_NESTED = 0x0008;
        public const ushort CV_PROP_CONTAINS_NESTED_CLASS = 0x0010;
        public const ushort CV_PROP_HAS_OVERLOADED_ASSIGNMENT_OPERATOR = 0x0020;
        public const ushort CV_PROP_HAS_CONVERSION_OPERATOR = 0x0040;
        public const ushort CV_PROP_FORWARD_REFERENCE = 0x0080;
        public const ushort CV_PROP_SCOPED = 0x0100;
        public const ushort CV_PROP_HAS_UNIQUE_NAME = 0x0200;
        public const ushort CV_PROP_SEALED = 0x0400;
        public const ushort CV_PROP_INTRINSIC = 0x2000;

        public const ushort CV_REG_NONE = 0;

        public const ushort CV_AMD64_RAX      =  328;
        public const ushort CV_AMD64_RBX      =  329;
        public const ushort CV_AMD64_RCX      =  330;
        public const ushort CV_AMD64_RDX      =  331;
        public const ushort CV_AMD64_RSI      =  332;
        public const ushort CV_AMD64_RDI      =  333;
        public const ushort CV_AMD64_RBP      =  334;
        public const ushort CV_AMD64_RSP      =  335;

        // 64-bit integer registers with 8-, 16-, and 32-bit forms (B, W, and D)
        public const ushort CV_AMD64_R8       =  336;
        public const ushort CV_AMD64_R9       =  337;
        public const ushort CV_AMD64_R10      =  338;
        public const ushort CV_AMD64_R11      =  339;
        public const ushort CV_AMD64_R12      =  340;
        public const ushort CV_AMD64_R13      =  341;
        public const ushort CV_AMD64_R14      =  342;
        public const ushort CV_AMD64_R15      =  343;

        // Matches DEBUG_S_SUBSECTION_TYPE in cvinfo.h
        public enum DebugSymbolsSubsectionType : uint
        {
            Symbols = 0xf1,
            Lines,
            StringTable,
            FileChecksums,
            FrameData,
            InlineeLines,
            CrossScopeImports,
            CrossScopeExports,

            ILLines,
            FunctionMDTokenMap,
            TypeMDTokenMap,
            MergedAssemblyInput,

            CoffSymbolRva,
        }

        // Matches LEAF_ENUM_e in cvinfo.h
        // 32-bit type index versions of leaves have the 0x1000 bit set; most 16-bit
        // leaves are omitted since they are unused
        public enum LeafRecordType
        {
            // Leaf indices starting records referenced from symbol records
            VTShape = 0x000a,
            Label = 0x000e,
            Null = 0x000f,
            NotTran = 0x0010,
            EndPrecomp = 0x0014, // not referenced from symbol
            TypeServerST = 0x0016, // not referenced from symbol
            Modifier = 0x1001,
            Pointer = 0x1002,
            ArrayST = 0x1003,
            ClassST = 0x1004,
            StructureST = 0x1005,
            UnionST = 0x1006,
            EnumST = 0x1007,
            Procedure = 0x1008,
            MemberFunction = 0x1009,
            Cobol0 = 0x100a,
            BArray = 0x100b,
            DimArrayST = 0x100c,
            VFTPath = 0x100d,
            PrecompST = 0x100e, // not referenced from symbol
            Oem = 0x100f, // oem definable type string
            AliasST = 0x1010, // alias (typedef) type
            Oem2 = 0x1011, // oem definable type string

            // Leaf indices starting records but referenced only from type records
            RefSym = 0x020c,
            EnumerateST = 0x0403,

            Skip = 0x1200,
            ArgList = 0x1201,
            DefArgST = 0x1202,
            FieldList = 0x1203,
            Derived = 0x1204,
            BitField = 0x1205,
            MethodList = 0x1206,
            DimConU = 0x1207,
            DimConLU = 0x1208,
            DimVatU = 0x1209,
            DimVarLU = 0x120a,

            BaseClass = 0x1400,
            VBaseClass = 0x1401,
            IVBaseClass = 0x1402,
            FriendFunctionST = 0x1403,
            Index = 0x1404,
            MemberST = 0x1405,
            StaticMemberST = 0x1406,
            MethodST = 0x1407,
            NestTypeST = 0x1408,
            VFunctionTable = 0x1409,
            FriendClass = 0x140a,
            OneMethodST = 0x140b,
            VFunctionOffset = 0x140c,
            NestTypeExST = 0x140d,
            MemberModifyST = 0x140e,
            ManagedST = 0x140f,

            // Types w/ SZ names
            TypeServer = 0x1501, // not referenced from symbol
            Enumerate = 0x1502,
            Array = 0x1503,
            Class = 0x1504,
            Structure = 0x1505,
            Union = 0x1506,
            Enum = 0x1507,
            DimArray = 0x1508,
            Precomp = 0x1509, // not referenced from symbol
            Alias = 0x150a, // alias (typedef) type
            DefArg = 0x150b,
            FriendFunction = 0x150c,
            Member = 0x150d,
            StaticMember = 0x150e,
            Method = 0x150f,
            NestType = 0x1510,
            OneMethod = 0x1511,
            NestTypeEx = 0x1512,
            MemberModify = 0x1513,
            Managed = 0x1514,
            TypeServer2 = 0x1515,

            StridedArray = 0x1516, // same as Array, but with stride between adjacent elements
            HLSL = 0x1517,
            ModifierEx = 0x1518,
            Interface = 0x1519,
            BInterface = 0x151a,
            Vector = 0x151b,
            Matrix = 0x151c,

            VFTable = 0x151d,

            FunctionId = 0x1601,
            MemberFunctionId = 0x1602,
            BuildInfo = 0x1603,
            SubstringList = 0x1604,
            StringId = 0x1605,

            UdtSrcLine = 0x1606, // source and line on where an UDT is defined (only generated by compiler)
            UdtModSrcLine = 0x1607, // module, source and line on where an UDT is defined (only generated by linker)

            // Size prefixes
            Numeric = 0x8000,
            Char = 0x8000,
            Short = 0x8001,
            UShort = 0x8002,
            Long = 0x8003,
            ULong = 0x8004,
            Real32 = 0x8005,
            Real64 = 0x8006,
            Real80 = 0x8007,
            Real128 = 0x8008,
            QuadWord = 0x8009,
            UQuadWord = 0x800a,
            Real48 = 0x800b,
            Complex32 = 0x800c,
            Complex64 = 0x800d,
            Complex80 = 0x800e,
            Complex128 = 0x800f,
            VarString = 0x8010,

            OctWord = 0x8017,
            UOctWord = 0x8018,
            Decimal = 0x8019,
            Date = 0x801a,
            Utf8String = 0x801b,
            Real = 0x801c,

            // List padding
            Pad0 = 0xf0,
            Pad1 = 0xf1,
            Pad2 = 0xf2,
            Pad3 = 0xf3,
            Pad4 = 0xf4,
            Pad5 = 0xf5,
            Pad6 = 0xf6,
            Pad7 = 0xf7,
            Pad8 = 0xf8,
            Pad9 = 0xf9,
            Pad10 = 0xfa,
            Pad11 = 0xfb,
            Pad12 = 0xfc,
            Pad13 = 0xfd,
            Pad14 = 0xfe,
            Pad15 = 0xff,
        }
    }
}
